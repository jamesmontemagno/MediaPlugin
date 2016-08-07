## Changelog

### [2.5.1-betaX]
* All: Ensure you call await CrossMedia.Current.Initialize(); before accessing any APIs
* All: Resize when taking a photo
* All: Save original album location when picking photo
* iOS & Android: Ability to resize when picking photo
* iOS & Android: Set Quality Level when taking photo
* Android: Fix images that get rotated in the wrong direction
* iOS: Fix for rotating device.
* iOS: Added custom overlay method
* Windows RT: Bug fixes & Video Support

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