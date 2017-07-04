using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Net.Chdk.Model.Software;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Net.Chdk.Meta.Providers.CameraModel.Zip
{
    sealed class ZipCameraListProvider : ICameraListProvider
    {
        private ILogger Logger { get; }
        private ICameraMetaProvider CameraProvider { get; }
        private string FileName { get; }

        public ZipCameraListProvider(ICameraMetaProvider cameraProvider, IBootMetaProvider bootProvider, ILogger<ZipCameraListProvider> logger)
        {
            Logger = logger;
            CameraProvider = cameraProvider;
            FileName = bootProvider.FileName;
        }

        public IDictionary<string, IDictionary<string, string>> GetCameraList(Stream stream)
        {
            var cameraList = new SortedDictionary<string, IDictionary<string, string>>();
            var cameras = GetCameras(stream, string.Empty);
            foreach (var camera in cameras)
            {
                if (camera != null)
                {
                    AddCamera(cameraList, camera.Platform, camera.Revision);
                }
            }
            return cameraList;
        }

        private static void AddCamera(IDictionary<string, IDictionary<string, string>> cameraList, string platform, string revision)
        {
            var revisionKey = GetRevisionKey(revision);
            var revisions = GetOrAddRevisions(cameraList, platform);
            revisions.Add(revisionKey, revision);
        }

        private static IDictionary<string, string> GetOrAddRevisions(IDictionary<string, IDictionary<string, string>> cameraList, string platform)
        {
            IDictionary<string, string> revisions;
            if (!cameraList.TryGetValue(platform, out revisions))
            {
                revisions = new SortedDictionary<string, string>();
                cameraList.Add(platform, revisions);
            }
            return revisions;
        }

        private IEnumerable<SoftwareCameraInfo> GetCameras(Stream stream, string name)
        {
            using (var zip = new ZipFile(stream))
            {
                return GetCameras(zip, name).ToArray();
            }
        }

        private IEnumerable<SoftwareCameraInfo> GetCameras(ZipFile zip, string name)
        {
            Logger.LogInformation("Enter {0}", name);
            foreach (ZipEntry entry in zip)
            {
                var items = GetCameras(zip, entry);
                foreach (var item in items)
                    yield return item;
                yield return GetCamera(zip, name, entry);
            }
            Logger.LogInformation("Exit {0}", name);
        }

        private IEnumerable<SoftwareCameraInfo> GetCameras(ZipFile zip, ZipEntry entry)
        {
            if (!entry.IsFile)
                return Enumerable.Empty<SoftwareCameraInfo>();

            var ext = Path.GetExtension(entry.Name);
            if (!".zip".Equals(ext, StringComparison.OrdinalIgnoreCase))
                return Enumerable.Empty<SoftwareCameraInfo>();

            var name = Path.GetFileName(entry.Name);
            using (var stream = zip.GetInputStream(entry))
            {
                return GetCameras(stream, name);
            }
        }

        private SoftwareCameraInfo GetCamera(ZipFile zip, string name, ZipEntry entry)
        {
            if (!entry.IsFile)
                return null;

            if (!FileName.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))
                return null;

            return DoGetCamera(zip, name, entry);
        }

        private SoftwareCameraInfo DoGetCamera(ZipFile zip, string name, ZipEntry entry)
        {
            return CameraProvider.GetCamera(name);
        }

        private static string GetRevisionKey(string revisionStr)
        {
            var revision = GetFirmwareRevision(revisionStr);
            return $"0x{revision:x}";
        }

        private static uint GetFirmwareRevision(string revision)
        {
            if (revision == null)
                return 0;
            return
                (uint)((revision[0] - 0x30) << 24) +
                (uint)((revision[1] - 0x30) << 20) +
                (uint)((revision[2] - 0x30) << 16) +
                (uint)((revision[3] - 0x60) << 8);
        }
    }
}
