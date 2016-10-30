Media Plugin for Xamarin & Windows

Changelog:
[2.6.0]
* All: Ensure you call await CrossMedia.Current.Initialize(); before accessing any APIs
* All: Resize when taking a photo
* All: Save original album location when picking photo
* iOS & Android: Ability to resize when picking photo
* iOS & Android: Set Quality Level when taking photo
* Android: Fix images that get rotated in the wrong direction
* Android: Updates for Android N Strict Mode, see documentation if you target N+
* iOS & Android: Fix for rotating device.
* iOS: Added custom overlay method (Preview)
* iOS: iOS 10 support for new permissions, please see documentations
* Windows RT: Bug fixes & Video Support

Find the latest at: https://github.com/jamesmontemagno/MediaPlugin

## Additional Required Setup (Please Read!)

## Android 
In  your BaseActivity or MainActivity (for Xamarin.Forms) add this code:

public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}

The `WRITE_EXTERNAL_STORAGE`, `READ_EXTERNAL_STORAGE` permissions are required, but the library will automatically add this for you. Additionally, if your users are running Marshmallow the Plugin will automatically prompt them for runtime permissions.

Additionally, the following has been added for you:
[assembly: UsesFeature("android.hardware.camera", Required = false)]
[assembly: UsesFeature("android.hardware.camera.autofocus", Required = false)]

**ANDROID N**
If your application targets Android N (API 24) or newer, you must use version 2.6.0+.

You must also add a few additional configuration files to adhere to the new strict mode:

1.) Add the following to your AndroidManifest.xml inside the <application> tags:

<provider android:name="android.support.v4.content.FileProvider" 
				android:authorities="YOUR_APP_PACKAGE_NAME.fileprovider" 
				android:exported="false" 
				android:grantUriPermissions="true">
			<meta-data android:name="android.support.FILE_PROVIDER_PATHS" 
				android:resource="@xml/file_paths"></meta-data>
</provider>

YOUR_APP_PACKAGE_NAME must be set to your app package name!

2.) Add a new folder called xml into your Resources folder and add a new XML file called `file_paths.xml`

Add the following code:

<?xml version="1.0" encoding="utf-8"?>
<paths xmlns:android="http://schemas.android.com/apk/res/android">
    <external-path name="my_images" path="Android/data/YOUR_APP_PACKAGE_NAME/files/Pictures" />
    <external-path name="my_movies" path="Android/data/YOUR_APP_PACKAGE_NAME/files/Movies" />
</paths>

YOUR_APP_PACKAGE_NAME must be set to your app package name!

You can read more at: https://developer.android.com/training/camera/photobasics.html


### iOS

Your app is required to have keys in your Info.plist for `NSCameraUsageDescription` and `NSPhotoLibraryUsageDescription` in order to access the device's camera and photo/video library. If you are using the Video capabilities of the library then you must also add `NSMicrophoneUsageDescription`.  The string that you provide for each of these keys will be displayed to the user when they are prompted to provide permission to access these device features. You can read me here: https://blog.xamarin.com/new-ios-10-privacy-permission-settings/

Such as:
<key>NSCameraUsageDescription</key>
<string>This app needs access to the camera to take photos.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>This app needs access to photos.</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app needs access to microphone.</string>

### Windows Phone 8/8.1 Silverlight

You must set the `IC_CAP_ISV_CAMERA` permission.

WP 8/8.1 Silverlight only supports photo, not video.

### Windows Phone 8.1 RT
Set `Webcam` permission.

In your App.xaml.cs you MUST place the following code inside of the `OnLaunched` method:

```csharp
protected override void OnActivated(IActivatedEventArgs args)
{

    Plugin.Media.MediaImplementation.OnFilesPicked(args);

    base.OnActivated(args);
}
```



### Windows Store:
Set `Webcam` permission.
