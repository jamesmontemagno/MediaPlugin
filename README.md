## Update Novemeber 2020

[Xamarin.Essentials](https://github.com/xamarin/essentials?WT.mc_id=docs-github-jamont) 1.6 introduced official support for [picking/taking photos and videos](https://docs.microsoft.com/xamarin/essentials/media-picker?WT.mc_id=friends-0000-jamont) with the new Media Picker API. 

This library has a lot of legacy code that is extremely hard to maintain and update to support the latest OSes without a major re-write. I will officially be archiving this library in December 2020 unless anyone from the community wants to adopt the project.

## Media Plugin for Xamarin and Windows

Simple cross platform plugin to take photos and video or pick them from a gallery from shared code.

Please read through all of the setup directions below: https://github.com/jamesmontemagno/MediaPlugin#important-permission-information

Ported from [Xamarin.Mobile](http://www.github.com/xamarin/xamarin.mobile) to a cross platform API.

### Setup
* Available on NuGet: http://www.nuget.org/packages/Xam.Plugin.Media [![NuGet](https://img.shields.io/nuget/v/Xam.Plugin.Media.svg?label=NuGet)](https://www.nuget.org/packages/Xam.Plugin.Media/)
* Install into your .NET Standard project and Client projects.
* Please see the additional setup for each platforms permissions.

Build Status: 
* ![](https://jamesmontemagno.visualstudio.com/_apis/public/build/definitions/6b79a378-ddd6-4e31-98ac-a12fcd68644c/24/badge)

**Platform Support**

|Platform|Version|
| ------------------- | :------------------: |
|Xamarin.iOS|iOS 7+|
|Xamarin.Android|API 14+|
|Windows 10 UWP|10+|
|.NET for iOS |iOS 10+|
|.NET for Android |API 21+|
|Windows App SDK (WinUI3)|10+|
|.NET for Mac Catalyst|All|
|Tizen|4+|


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
        return stream;
    }); 
};
```

To see more examples of usage without Xamarin.Forms open up the test folder in this project.

### Directories and File Names
Setting these properties are optional. Any illegal characters will be removed and if the name of the file is a duplicate then a number will be appended to the end. The default implementation is to specify a unique time code to each value. 


## Photo & Video Settings

### Compressing Photos
When calling `TakePhotoAsync` or `PickPhotoAsync` you can specify multiple options to reduce the size and quality of the photo that is taken or picked. These are applied to the `StoreCameraMediaOptions` and `PickMediaOptions`.

#### Resize Photo Size
 By default the photo that is taken/picked is the maxiumum size and quality available. For most applications this is not needed and can be Resized. This can be accomplished by adjusting the `PhotoSize` property on the options. The easiest is to adjust it to `Small, Medium, or Large`, which is 25%, 50%, or 75% or the original. This is only supported in Android & iOS. On UWP there is a different scale that is used based on these numbers to the respected resolutions UWP supports.

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
Set the `CompressionQuality`, which is a value from 0 the most compressed all the way to 100, which is no compression. A good setting from testing is around 92. This is only supported in Android & iOS

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

### Take Photo Overlay (iOS Only)
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
    OverlayViewProvider = func
});
```


###  Important Permission Information
Please read these as they must be implemented for all platforms.

#### Android 
The `WRITE_EXTERNAL_STORAGE` & `READ_EXTERNAL_STORAGE` permissions are required, but the library will automatically add this for you. Additionally, if your users are running Marshmallow the Plugin will automatically prompt them for runtime permissions. You must add `Xamarin.Essentials.Platform.OnRequestPermissionsResult` code into your Main or Base Activities:

Add to Activity:

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
{
    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

## Android Required Setup

This plugin uses the Xamarin.Essentials, please follow the setup guide: http://aka.ms/essentials-getstarted


#### Android File Provider Setup

You must also add a few additional configuration files to adhere to the new strict mode:

1a.) (Non AndroidX) Add the following to your AndroidManifest.xml inside the `<application>` tags:
```xml
<provider android:name="android.support.v4.content.FileProvider" 
          android:authorities="${applicationId}.fileprovider" 
          android:exported="false" 
          android:grantUriPermissions="true">
          
	  <meta-data android:name="android.support.FILE_PROVIDER_PATHS" 
                     android:resource="@xml/file_paths"></meta-data>
</provider>
```

**Note:** If you receive the following error, it is because you are using AndroidX. To resolve this error, follow the instructions in Step `1b.)`.
> Unable to get provider android.support.v4.content.FileProvider: java.lang.ClassNotFoundException: Didn't find class "android.support.v4.content.FileProvider" on path: DexPathList


1b.) (AndroidX) Add the following to your AndroidManifest.xml inside the `<application>` tags:
```xml
<provider android:name="androidx.core.content.FileProvider" 
          android:authorities="${applicationId}.fileprovider" 
          android:exported="false" 
          android:grantUriPermissions="true">
          
	  <meta-data android:name="android.support.FILE_PROVIDER_PATHS" 
                     android:resource="@xml/file_paths"></meta-data>
</provider>
```

2.) Add a new folder called `xml` into your Resources folder and add a new XML file called `file_paths.xml`. Make sure that this XML file has a Build Action of: `AndroidResource`.

Add the following code:

```xml
<?xml version="1.0" encoding="utf-8"?>
<paths xmlns:android="http://schemas.android.com/apk/res/android">
    <external-files-path name="my_images" path="Pictures" />
    <external-files-path name="my_movies" path="Movies" />
</paths>
```

You can read more at: https://developer.android.com/training/camera/photobasics.html

## Android Optional Setup

By default, the library adds `android.hardware.camera` and `android.hardware.camera.autofocus` to your apps manifest as optional features. It is your responsbility to check whether your device supports the hardware before using it. If instead you'd like [Google Play to filter out devices](http://developer.android.com/guide/topics/manifest/uses-feature-element.html#permissions-features) without the required hardware, add the following to your AssemblyInfo.cs file in your Android project:

```
[assembly: UsesFeature("android.hardware.camera", Required = true)]
[assembly: UsesFeature("android.hardware.camera.autofocus", Required = true)]
```


#### iOS

Your app is required to have keys in your Info.plist for `NSCameraUsageDescription` and `NSPhotoLibraryUsageDescription` in order to access the device's camera and photo/video library. If you are using the Video capabilities of the library then you must also add `NSMicrophoneUsageDescription`.  If you want to "SaveToGallery" then you must add the `NSPhotoLibraryAddUsageDescription` key into your info.plist. The string that you provide for each of these keys will be displayed to the user when they are prompted to provide permission to access these device features. You can read me here: [New iOS 10 Privacy Permission Settings](https://devblogs.microsoft.com/xamarin/new-ios-10-privacy-permission-settings?WT.mc_id=friends-0000-jamont)

Such as:
```xml
<key>NSCameraUsageDescription</key>
<string>This app needs access to the camera to take photos.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>This app needs access to photos.</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app needs access to microphone.</string>
<key>NSPhotoLibraryAddUsageDescription</key>
<string>This app needs access to the photo gallery.</string>
```

If you want the dialogs to be translated you must support the specific languages in your app. Read the [iOS Localization Guide](https://developer.xamarin.com/guides/ios/advanced_topics/localization_and_internationalization/)

#### UWP

Set `Webcam` permission.



### Permission Recommendations
By default, the Media Plugin will attempt to request multiple permissions, but each platform handles this a bit differently, such as iOS which will only pop up permissions once.

You can use Xamarin.Essentials to request and check permissions manually.

#### FAQ
Here are some common answers to questions:

#### On iOS how do I translate the text on the buttons on the camera?
You need CFBundleLocalizations in your Info.plist.


#### License

Licensed under MIT, see license file. This is a derivative to [Xamarin.Mobile's Media](http://github.com/xamarin/xamarin.mobile) with a cross platform API and other enhancements.
```
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
```

### Want To Support This Project?
All I have ever asked is to be active by submitting bugs, features, and sending those pull requests down! Want to go further? Make sure to subscribe to my weekly development podcast [Merge Conflict](http://mergeconflict.fm), where I talk all about awesome Xamarin goodies and you can optionally support the show by becoming a [supporter on Patreon](https://www.patreon.com/mergeconflictfm).
