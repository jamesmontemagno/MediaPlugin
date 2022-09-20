#if NET6_0_OR_GREATER
using Microsoft.Maui.ApplicationModel;
using static Microsoft.Maui.ApplicationModel.Permissions;
#else
using Xamarin.Essentials;
using static Xamarin.Essentials.Permissions;
#endif
using Android;

namespace Plugin.Media
{
    public class StoragePermission : BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
                new (string, bool)[] { (Manifest.Permission.WriteExternalStorage, true),
                (Manifest.Permission.ReadExternalStorage, true)};
    }
}
