using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Net.Chdk.Meta.Model.CameraList;
using Net.Chdk.Meta.Providers.Zip;
using Net.Chdk.Model.Software;
using System.Collections.Generic;
using System.IO;

namespace Net.Chdk.Meta.Providers.CameraList.Zip
{
    sealed class ZipCameraListProvider : ZipMetaProvider<SoftwareCameraInfo>, ICameraListProvider
    {
        private ICameraMetaProvider CameraProvider { get; }

        public ZipCameraListProvider(ICameraMetaProvider cameraProvider, IBootMetaProvider bootProvider, ILogger<ZipCameraListProvider> logger)
            : base(bootProvider, logger)
        {
            CameraProvider = cameraProvider;
        }

        public IDictionary<string, ListPlatformData> GetCameraList(Stream stream)
        {
            var cameraList = new SortedDictionary<string, ListPlatformData>();
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

        private static void AddCamera(IDictionary<string, ListPlatformData> cameras, string platform, string revision)
        {
            var revisionKey = GetRevisionKey(revision);
            var platformData = GetOrAddPlatform(cameras, platform);
            var revisionData = GetRevisionData(platform, revision, null);
            platformData.Revisions.Add(revisionKey, revisionData);
        }

        private static ListPlatformData GetOrAddPlatform(IDictionary<string, ListPlatformData> cameras, string platform)
        {
            ListPlatformData platformData;
            if (!cameras.TryGetValue(platform, out platformData))
            {
                platformData = GetPlatformData();
                cameras.Add(platform, platformData);
            }
            return platformData;
        }

        private static ListPlatformData GetPlatformData()
        {
            return new ListPlatformData
            {
                Revisions = new SortedDictionary<string, ListRevisionData>()
            };
        }

        private static ListRevisionData GetRevisionData(string platform, string revision, string source)
        {
            return new ListRevisionData
            {
                Source = GetSourceData(platform, revision, source)
            };
        }

        private static ListSourceData GetSourceData(string platform, string revision, string source)
        {
            return new ListSourceData
            {
                Platform = platform,
                Revision = GetRevision(revision, source)
            };
        }

        private static string GetRevision(string revision, string source)
        {
            if (!string.IsNullOrEmpty(source))
                return source;
            return revision;
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
