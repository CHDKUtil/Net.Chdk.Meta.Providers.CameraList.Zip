using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Net.Chdk.Meta.Providers.Zip;
using Net.Chdk.Model.Software;
using System.Collections.Generic;
using System.IO;

namespace Net.Chdk.Meta.Providers.CameraModel.Zip
{
    sealed class ZipCameraListProvider : ZipMetaProvider<SoftwareCameraInfo>, ICameraListProvider
    {
        private ICameraMetaProvider CameraProvider { get; }

        public ZipCameraListProvider(ICameraMetaProvider cameraProvider, IBootMetaProvider bootProvider, ILogger<ZipCameraListProvider> logger)
            : base(bootProvider, logger)
        {
            CameraProvider = cameraProvider;
        }

        public IDictionary<string, IDictionary<string, string>> GetCameraList(Stream stream)
        {
            var cameraList = new SortedDictionary<string, IDictionary<string, string>>();
            var cameras = GetItems(stream, string.Empty);
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

        protected override SoftwareCameraInfo DoGetItem(ZipFile zip, string name, ZipEntry entry)
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
