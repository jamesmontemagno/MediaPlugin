## Media Plugin for Xamarin and Windows

Simple cross platform plugin to take photos and video or pick them from a gallery from shared code.

Ported from [Xamarin.Mobile](http://www.github.com/xamarin/xamarin.mobile) to a cross platform API.

### Setup
* Available on NuGet: http://www.nuget.org/packages/Xam.Plugin.Media [![NuGet](https://img.shields.io/nuget/v/Xam.Plugin.Media.svg?label=NuGet)](https://www.nuget.org/packages/Xam.Plugin.Media/)
* Install into your PCL project and Client projects.
* Please see the additional setup for each platforms permissions.

Build Status: 
* [![Build status](https://ci.appveyor.com/api/projects/status/872kljawr91vphty?svg=true)](https://ci.appveyor.com/project/JamesMontemagno/mediaplugin)
* CI NuGet Feed: https://ci.appveyor.com/nuget/mediaplugin

**Platform Support**

|Platform|Supported|Version|
| ------------------- | :-----------: | :------------------: |
|Xamarin.iOS|Yes|iOS 7+|
|Xamarin.iOS Unified|Yes|iOS 7+|
|Xamarin.Android|Yes|API 14+|
|Windows Phone Silverlight|Yes|8.0+|
|Windows Phone RT|Yes|8.1+|
|Windows Store RT|Yes|8.1+|
|Windows 10 UWP|Yes|10+|
|Xamarin.Mac|No||


### API Usage

Call **CrossMedia.Current** from any project or PCL to gain access to APIs.

Before taking photos or videos you should check to see if a camera exists and if photos and videos are supported on the device. There are five properties that you can check:

```csharp

/// <summary>
/// Initialize all camera components, must be called before checking properties below
/// </summary>
/// <returns>If success</returns>
Task<bool> Initialize();

/// <summary>
/// Gets if a camera is available on the device
/// </summary>
bool IsCameraAvailable { get; }

/// <summary>
/// Gets if ability to take photos supported on the device
/// </summary>
bool IsTakePhotoSupported { get; }

/// <summary>
/// Gets if the ability to pick photo is supported on the device
/// </summary>
bool IsPickPhotoSupported { get; }

/// <summary>
/// Gets if ability to take video is supported on the device
/// </summary>
bool IsTakeVideoSupported { get; }

/// <summary>
/// Gets if the ability to pick a video is supported on the device
/// </summary>
bool IsPickVideoSupported { get; }
```

### Photos
```csharp
/// <summary>
/// Picks a photo from the default gallery
/// </summary>
/// <param name="options">Pick Photo Media Options</param>
/// <returns>Media file or null if canceled</returns>
Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null);

/// <summary>
/// Take a photo async with specified options
/// </summary>
/// <param name="options">Camera Media Options</param>
/// <returns>Media file of photo or null if canceled</returns>
Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options);
```

### Videos
```csharp
/// <summary>
/// Picks a video from the default gallery
/// </summary>
/// <returns>Media file of video or null if canceled</returns>
Task<MediaFile> PickVideoAsync();

/// <summary>
/// Take a video with specified options
/// </summary>
/// <param name="options">Video Media Options</param>
/// <returns>Media file of new video or null if canceled</returns>
Task<MediaFile> TakeVideoAsync(StoreVideoOptions options);
```

### Usage
Via a Xamarin.Forms project with a Button and Image to take a photo:

```csharp
takePhoto.Clicked += async (sender, args) =>
{
    await CrossMedia.Current.Initialize();
    
    if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
    {
        DisplayAlert("No Camera", ":( No camera available.", "OK");
        return;
    }

    var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
    {
        Directory = "Sample",
        Name = "test.jpg"
    });

    if (file == null)
        return;

    await DisplayAlert("File Location", file.Path, "OK");

    image.Source = ImageSource.FromStream(() =>
    {
        var stream = file.GetStream();
        file.Dispose();
        return stream;
    }); 
};
```

### Directories and File Names
Setting these properties are optional. Any illegal characters will be removed and if the name of the file is a duplicate then a number will be appended to the end. The default implementation is to specify a unique time code to each value. 


## Photo & Video Settings

### Compressing Photos
When calling `TakePhotoAsync` or `PickPhotoAsync` you can specify multiple options to reduce the size and quality of the photo that is taken or picked. These are applied to the `StoreCameraMediaOptions` and `PickMediaOptions`.

#### Resize Photo Size
 By default the photo that is taken/picked is the maxiumum size and quality available. For most applications this is not needed and can be Resized. This can be accomplished by adjusting the `PhotoSize` property on the options. The easiest is to adjust it to `Small, Medium, or Large`, which is 25%, 50%, or 75% or the original.

```csharp
var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
{
    PhotoSize = PhotoSize.Medium,
});
```

Or you can set to a custom percentage:

```csharp
var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
{
    PhotoSize = PhotoSize.Custom,
    CustomPhotoSize = 90 //Resize to 90% of original
});
```

#### Photo Quality
Set the `CompressionQuality`, which is a value from 0 the most compressed all the way to 100, which is no compression. A good setting from testing is around 92.

```csharp
var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
{
    CompressionQuality = 92
});
```


### Saving Photo/Video to Camera Roll/Gallery 
You can now save a photo or video to the camera roll/gallery. When creating the ```StoreCameraMediaOptions``` or ```StoreVideoMediaOptions``` simply set ```SaveToAlbum``` to true. When your user takes a photo it will still store temporary data, but also if needed make a copy to the public gallery (based on platform). In the MediaFile you will now see a AlbumPath that you can query as well.

```csharp
var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
{
    SaveToAlbum = true
});

//Get the public album path
var aPpath = file.AlbumPath; 

//Get private path
var path = file.Path;
```

This will restult in 2 photos being saved for the photo. One in your private folder and one in a public directory that is shown. The value will be returned at `AlbumPath`.

Android: When you set SaveToAlbum this will make it so your photos are public in the Pictures/YourDirectory or Movies/YourDirectory. This is the only way Android can detect the photos.


### Allow Cropping
Both iOS and UWP have crop controls built into the the camera control when taking a photo. On iOS the default is `false` and UWP the default is `true`. You can adjust the `AllowCropping` property when taking a photo to allow your user to crop.

```csharp
var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
{
    AllowCropping = true
});
```

### Default Camera 
By default when you take a photo or video the default system camera will be selected. Simply set the `DefaultCamera` on `StoreCameraMediaOptions`. This option does not guarantee that the actual camera will be selected because each platform is different. It seems to work extremely well on iOS, but not so much on Android. Your mileage may vary.

```csharp
var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
{
    DefaultCamera = Plugin.Media.Abstractions.CameraDevice.Front
});
```

### Take Photo Overlay (iOS Only Preview)
On iOS you are able to specify an overlay on top of the camera. It will show up on the live camera and on the final preview, but it is not saved as part of the photo, which means it is not a filter.

```csharp
//Load an image as an overlay (this is in the iOS Project)
Func<object> func = () =>
{
    var imageView = new UIImageView(UIImage.FromBundle("face-template.png"));
    imageView.ContentMode = UIViewContentMode.ScaleAspectFit;

    var screen = UIScreen.MainScreen.Bounds;
    imageView.Frame = screen;

    return imageView;
};

//Take Photo, could be in iOS Project, or in shared code where there function is passed up via Dependency Services.
var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
{
    OverlayViewProvider = fund
});
```


###  Important Permission Information
Please read these as they must be implemented for all platforms.

#### Android 
The `WRITE_EXTERNAL_STORAGE` & `READ_EXTERNAL_STORAGE` permissions are required, but the library will automatically add this for you. Additionally, if your users are running Marshmallow the Plugin will automatically prompt them for runtime permissions. You must add the Permission Plugin code into your Main or Base Activities:

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

You MUST set your Target version to API 23+ and Compile against API 23+:
![image](https://cloud.githubusercontent.com/assets/1676321/17110560/7279341c-5252-11e6-89be-8c10b38c0ea6.png)

By adding these permissions [Google Play will automatically filter out devices](http://developer.android.com/guide/topics/manifest/uses-feature-element.html#permissions-features) without specific hardward. You can get around this by adding the following to your AssemblyInfo.cs file in your Android project:

```
[assembly: UsesFeature("android.hardware.camera", Required = false)]
[assembly: UsesFeature("android.hardware.camera.autofocus", Required = false)]
```


#### ANDROID N

If your application targets Android N (API 24) or newer, you must use version 2.6.0+.

You must also add a few additional configuration files to adhere to the new strict mode:

1.) Add the following to your AndroidManifest.xml inside the `<application>` tags:
```
<provider android:name="android.support.v4.content.FileProvider" 
				android:authorities="YOUR_APP_PACKAGE_NAME.fileprovider" 
				android:exported="false" 
				android:grantUriPermissions="true">
			<meta-data android:name="android.support.FILE_PROVIDER_PATHS" 
				android:resource="@xml/file_paths"></meta-data>
</provider>
```

**YOUR_APP_PACKAGE_NAME** must be set to your app package name!

2.) Add a new folder called `xml` into your Resources folder and add a new XML file called `file_paths.xml`

Add the following code:
```
<?xml version="1.0" encoding="utf-8"?>
<paths xmlns:android="http://schemas.android.com/apk/res/android">
    <external-path name="my_images" path="Android/data/YOUR_APP_PACKAGE_NAME/files/Pictures" />
    <external-path name="my_movies" path="Android/data/YOUR_APP_PACKAGE_NAME/files/Movies" />
</paths>
```

**YOUR_APP_PACKAGE_NAME** must be set to your app package name!

You can read more at: https://developer.android.com/training/camera/photobasics.html


#### iOS

Your app is required to have keys in your Info.plist for `NSCameraUsageDescription` and `NSPhotoLibraryUsageDescription` in order to access the device's camera and photo/video library. If you are using the Video capabilities of the library then you must also add `NSMicrophoneUsageDescription`.  The string that you provide for each of these keys will be displayed to the user when they are prompted to provide permission to access these device features. You can read me here: https://blog.xamarin.com/new-ios-10-privacy-permission-settings/

Such as:
```
<key>NSCameraUsageDescription</key>
<string>This app needs access to the camera to take photos.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>This app needs access to photos.</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app needs access to microphone.</string>
```


**Windows Phone 8/8.1 Silverlight:**

You must set the `IC_CAP_ISV_CAMERA` permission.

WP 8/8.1 Silverlight only supports photo, not video.

**Windows Phone 8.1 RT:**

Set `Webcam` permission.

In your App.xaml.cs you MUST place the following code inside of the `OnLaunched` method:

```csharp
protected override void OnActivated(IActivatedEventArgs args)
{

    Plugin.Media.MediaImplementation.OnFilesPicked(args);

    base.OnActivated(args);
}
```
**Windows Store:**

Set `Webcam` permission.


#### Windows Phone 8.1 RT Photo Capture
This feature was made possible by Daniel Meixner and his great open source project:  https://diycameracaptureui.codeplex.com/

Made possible under Ms-PL license:  https://diycameracaptureui.codeplex.com/license

### Permission Recommendations
By default, the Media Plugin will attempt to request multiple permissions, but each platform handles this a bit differently, such as iOS which will only pop up permissions once. I recommend adding the [Permissions Plugin](http://github.com/jamesmontemagno/PermissionsPlugin) into your application and before taking any photo or picking photos that you check permissions ahead of time. 

Here is an example:
```csharp
var cameraStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Camera);
var storageStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);

if (cameraStatus != PermissionStatus.Granted || storageStatus != PermissionStatus.Granted)
{
    var results = await CrossPermissions.Current.RequestPermissionsAsync(new[] {Permission.Camera, Permission.Storage});
    cameraStatus = results[Permission.Camera];
    storageStatus = results[Permission.Storage];
}

if (cameraStatus == PermissionStatus.Granted && storageStatus == PermissionStatus.Granted)
{
     var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
    {
        Directory = "Sample",
        Name = "test.jpg"
    });
}
else
{
    await DisplayAlert("Permissions Denied", "Unable to take photos.", "OK");
    //On iOS you may want to send your user to the settings screen.
    //CrossPermissions.Current.OpenAppSettings();
}
```


#### License
Licensed under MIT, see license file. This is a derivative to [Xamarin.Mobile's Media](http://github.com/xamarin/xamarin.mobile) with a cross platform API and other enhancements.
//
//  Copyright 2011-2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
