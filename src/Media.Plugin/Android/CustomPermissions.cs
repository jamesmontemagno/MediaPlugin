using System;
using System.Collections.Generic;
using System.Text;
using static Xamarin.Essentials.Permissions;
using System.Threading.Tasks;
using Xamarin.Essentials;
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
