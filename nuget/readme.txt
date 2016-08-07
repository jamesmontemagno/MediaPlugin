Media Plugin for Xamarin & Windows

Changelog:
[2.5.1-betaX]
* All: Ensure you call await CrossMedia.Current.Initialize(); before accessing any APIs
* All: Resize when taking a photo
* All: Save original album location when picking photo
* iOS & Android: Ability to resize when picking photo
* iOS & Android: Set Quality Level when taking photo
* Android: Fix images that get rotated in the wrong direction
* iOS: Fix for rotating device.
* iOS: Added custom overlay method
* Windows RT: Bug fixes & Video Support

Find the latest at: https://github.com/jamesmontemagno/MediaPlugin

## Additional Setup

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


### iOS
The library will automatically ask for permission when taking photos/videos or access the libraries.

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
