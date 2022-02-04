## Changelog
### 5.0.1
* Main release with Xamarin.Essentials at the core
* Fix a lot of iOS issues and other misc.

### 4.0.0-pre
* Fix for #496: Special case for Huawei devices on pre-N devices
* Fix for #452: Large images on iPhone X don't save sometimes at 100% (thanks @christophedelanghe)
* Fix for #514: Videos on Android 8.1 stop after short time.
* Upgrade to CurrentActivityPlugin 2.0 and Permissions 3.0
* Ability on iOS to specify pop over style
* Fix for #642: Android dock/undock issue
* Fix for #639: Null reference sometimes when can't accessing file on Android.
* Fix for #553: When not rotating image on android ensure we take max width/height into consideration.
* Fix for #545: iOS error sometimes when saving metadata.
* Fix for #608: Possible null on iOS when picking video sometimes.

### [3.1.3]
* Remove need for Android Target versions (always use File Provider via #442 and @ddobrev)
* Enhancments to Android picking front or rare camera (via @WebDucer)


### [3.1.0]
* Fixes for rotations on iOS (return proper exif)
* Remove permission pop up on pick video/photo on iOS 11+
* Better checks on disposing of controllers
* Tizen Support

### [3.0.2]
* iOS: Fix Lat/Long saving
* Android: Fixup issue with url sharing #300 especially with mobileiron
* iOS: Ensure all properties are set when picking photo #305
* Android/iOS check for permissions before performing actions. Will now throw a MediaPermissionException if invalid permissions.
* Android: Fix potential corrupt metadata: #367
* iOS: Handle memory better when taking photos #336
* iOS: Fix issues when using TabController that may get pushed down


### [3.0.0]
* Upgrade to .NET Standard
* Deprecate Windows Phone 8/8.1 and Windows Store
* Update to 25.x Support Libraries on Android
* iOS: Fix Pop-over position on iPad in landscape
* Add Exif Information to iOS and Android
* Optimize rotations on iOS and Android


### [2.6.3]
* All: No longer delete files when picked. You are in control.
* Windows Phone 8.1 RT: Handle button mashing on photo button better.
* Android: Re-save EXIF on rotation

### [2.6.2]
* Android: Fix issue where Zero byte image was being saved
* Android: Work around to add small delay when closing activity
* Android: Compress image even if size is full
* iOS: Align root pages for modal pages
* iOS: Fix compat with Rg.Plugins.Popup
* iOS: Return album path when picking photo

### [2.6.1]
* Android: Ensure files are resized when picking photo
* Andriid: Explicit grant URI for camera intent

### [2.6.0]
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

* Addition Bug Fixes & Optimizations on all platforms

### [2.3.0]
* Add UWP support

### [2.2.0]
* Android: No longer require camera permission
* Android: Use latest permission plugin for External Storage Permissions
* Android: Try to show front facing camera when parameter is set

### 2.1.2
* Add new "SaveToAlbum" bool to save the photo to public gallery
* Added new "AlbumPath", which is set when you use the "SaveToAlbum" settings
* Supports iOS, Android, WinRT, and UWP
* Add readme.txt
* Update to latest permissions plugin

### 2.0.1
* Breaking changes: New namespace - Plugin.Media
* Automatically Add Android Permissions
* Request Android Permissions on Marshmallow
* Uses new Permissions Plugin : https://www.nuget.org/packages/Plugin.Permissions/
* UWP Support
