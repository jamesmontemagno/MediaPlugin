## Changelog

### [3.0.0]
* Upgrade to .NET Standard
* Deprecate Windows Phone 8/8.1 and Windows Store
* Update to 25.x Support Libraries on Android

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
