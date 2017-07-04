using Microsoft.Extensions.DependencyInjection;

namespace Net.Chdk.Meta.Providers.CameraModel.Zip
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddZipCameraListProvider(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<ICameraListProvider, ZipCameraListProvider>();
        }
    }
}
