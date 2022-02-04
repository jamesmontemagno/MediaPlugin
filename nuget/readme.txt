Media Plugin for Xamarin & Windows

Find the latest at: https://github.com/jamesmontemagno/MediaPlugin

Additional Required Setup (Please Read!)

Android 

This library uses Xamarin.Essentials for permissions and other functionality. Please ensure that you have set it up correctly:

https://docs.microsoft.com/xamarin/essentials/get-started

```csharp
protected override void OnCreate(Bundle savedInstanceState) {
    //...
    base.OnCreate(savedInstanceState);
    Xamarin.Essentials.Platform.Init(this, savedInstanceState); // add this line to your code, it may also be called: bundle
    //...
```
And for permissions:

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
{
    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

This method may already exist. If so, add it after the call to Xamarin.Forms.Forms.Init(this, savedInstanceState).

NB: The `WRITE_EXTERNAL_STORAGE`, `READ_EXTERNAL_STORAGE` permissions are required, but the library will automatically add this for you. 
Additionally, if your users are running Marshmallow the Plugin will automatically prompt them for runtime permissions.

The following has also been added for you:
[assembly: UsesFeature("android.hardware.camera", Required = false)]
[assembly: UsesFeature("android.hardware.camera.autofocus", Required = false)]

You must also add a few additional configuration files to adhere to the new strict mode:

1. Add the following to your AndroidManifest.xml inside the <application> tags:

<provider android:name="android.support.v4.content.FileProvider" 
				android:authorities="${applicationId}.fileprovider" 
				android:exported="false" 
				android:grantUriPermissions="true">
			<meta-data android:name="android.support.FILE_PROVIDER_PATHS" 
				android:resource="@xml/file_paths"></meta-data>
</provider>

2. Add a new folder called xml into your Resources folder

3. Add a new XML file called `file_paths.xml` and add the following code:

<?xml version="1.0" encoding="utf-8"?>
<paths xmlns:android="http://schemas.android.com/apk/res/android">
    <external-files-path name="my_images" path="Pictures" />
    <external-files-path name="my_movies" path="Movies" />
</paths>

You can read more at: https://developer.android.com/training/camera/photobasics.html

Android: 

This plugin uses the Xamarin.Essentials, please follow the setup guide.

Xamarin.Essentials.Platform.Init(this, bundle);


iOS

Your app is required to have keys in your Info.plist for `NSCameraUsageDescription` and `NSPhotoLibraryUsageDescription` in order to access the device's camera and photo/video library. 
If you are using the Video capabilities of the library then you must also add `NSMicrophoneUsageDescription`.  
The string that you provide for each of these keys will be displayed to the user when they are prompted to provide permission to access these device features. You can read more here: https://blog.xamarin.com/new-ios-10-privacy-permission-settings/

Such as:
<key>NSCameraUsageDescription</key>
<string>This app needs access to the camera to take photos.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>This app needs access to photos.</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app needs access to microphone.</string>
<key>NSPhotoLibraryAddUsageDescription</key>
<string>This app needs access to the photo gallery.</string>

UWP
Set Webcam, Pictures Library and Videos Library permissions.

To enable the new media implementation, set the flag "UwpUseNewMediaImplementation" in the App.xaml.cs like this:

```c#
CrossMedia.SetFlags("UwpUseNewMediaImplementation");
Xamarin.Forms.Forms.Init(e);
```

Tizen
Please add the following Privileges in tizen-manifest.xml file:

http://tizen.org/privilege/appmanager.launch
http://tizen.org/privilege/mediastorage

See below for additional instructions.
https://developer.tizen.org/development/visual-studio-tools-tizen/tools/tizen-manifest-editor#privileges
