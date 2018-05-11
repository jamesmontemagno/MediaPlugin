using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Plugin.Media.Abstractions;
using Plugin.Permissions;
using Android.Media;
using Android.Graphics;
using System.Text.RegularExpressions;
using Plugin.CurrentActivity;
using System.Collections.Generic;
using System.Linq;
using Permission = Plugin.Permissions.Abstractions.Permission;

namespace Plugin.Media
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    [Android.Runtime.Preserve(AllMembers = true)]
    public class MediaImplementation : IMedia
    {
		const string TAG_PIXEL_X_DIMENSION = "PixelXDimension";
		const string TAG_PIXEL_Y_DIMENSION = "PixelYDimension";

		/// <summary>
        /// Implementation
        /// </summary>
        public MediaImplementation()
        {

            this.context = Android.App.Application.Context;
            IsCameraAvailable = context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread)
                IsCameraAvailable |= context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront);
        }

        ///<inheritdoc/>
        public Task<bool> Initialize() => Task.FromResult(true);

        /// <inheritdoc/>
        public bool IsCameraAvailable { get; }
        /// <inheritdoc/>
        public bool IsTakePhotoSupported => true;

        /// <inheritdoc/>
        public bool IsPickPhotoSupported => true;

        /// <inheritdoc/>
        public bool IsTakeVideoSupported => true;
        /// <inheritdoc/>
        public bool IsPickVideoSupported => true;


        bool IsValidExif(ExifInterface exif)
        {
            //if null, then not falid
            if (exif == null)
                return false;

			try
			{
				//if has thumb, but is <= 0, then not valid
				if (exif.HasThumbnail && (exif.GetThumbnail()?.Length ?? 0) <= 0)
					return false;
			}
			catch(Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Unable to get thumbnail exif: " + ex);
				return false;
			}

            return true;
        }

        /// <summary>
        /// Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
        public async Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null)
        {
            if (!(await RequestStoragePermission()))
            {
                throw new MediaPermissionException(Permission.Storage);
            }
            var media = await TakeMediaAsync("image/*", Intent.ActionPick, null);

            if (options == null)
                options = new PickMediaOptions();

            //check to see if we picked a file, and if so then try to fix orientation and resize
            if (!string.IsNullOrWhiteSpace(media?.Path))
            {
                try
                {
                    var originalMetadata = new ExifInterface(media.Path);

                    if (options.RotateImage)
                    {
                        await FixOrientationAndResizeAsync(media.Path, options, originalMetadata);
                    }
                    else
                    {
                        await ResizeAsync(media.Path, options.PhotoSize, options.CompressionQuality, options.CustomPhotoSize, originalMetadata);
                    }
                    if (options.SaveMetaData && IsValidExif(originalMetadata))
					{
						try
						{
                            originalMetadata?.SaveAttributes();
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Unable to save exif {ex}");
						}
					}

					originalMetadata?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to check orientation: " + ex);
                }
            }

            return media;
        }


        /// <summary>
        /// Take a photo async with specified options
        /// </summary>
        /// <param name="options">Camera Media Options</param>
        /// <returns>Media file of photo or null if canceled</returns>
        public async Task<MediaFile> TakePhotoAsync(StoreCameraMediaOptions options)
        {
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            if (!(await RequestCameraPermissions()))
            {
                throw new MediaPermissionException(Permission.Camera);
            }


            VerifyOptions(options);

            var media = await TakeMediaAsync("image/*", MediaStore.ActionImageCapture, options);

            if (string.IsNullOrWhiteSpace(media?.Path))
                return media;

            if (options.SaveToAlbum)
            {
                try
                {
                    var fileName = System.IO.Path.GetFileName(media.Path);
                    var publicUri = MediaPickerActivity.GetOutputMediaFile(context, options.Directory ?? "temp", fileName, true, true);
                    using (System.IO.Stream input = File.OpenRead(media.Path))
                        using (System.IO.Stream output = File.Create(publicUri.Path))
                            input.CopyTo(output);

                    media.AlbumPath = publicUri.Path;

                    var f = new Java.IO.File(publicUri.Path);

                    //MediaStore.Images.Media.InsertImage(context.ContentResolver,
                    //    f.AbsolutePath, f.Name, null);

                    try
                    {
                        Android.Media.MediaScannerConnection.ScanFile(context, new[] { f.AbsolutePath }, null, context as MediaPickerActivity);

                        ContentValues values = new ContentValues();
                        values.Put(MediaStore.Images.Media.InterfaceConsts.Title, System.IO.Path.GetFileNameWithoutExtension(f.AbsolutePath));
                        values.Put(MediaStore.Images.Media.InterfaceConsts.Description, string.Empty);
                        values.Put(MediaStore.Images.Media.InterfaceConsts.DateTaken, Java.Lang.JavaSystem.CurrentTimeMillis());
                        values.Put(MediaStore.Images.ImageColumns.BucketId, f.ToString().ToLowerInvariant().GetHashCode());
                        values.Put(MediaStore.Images.ImageColumns.BucketDisplayName, f.Name.ToLowerInvariant());
                        values.Put("_data", f.AbsolutePath);

                        var cr = context.ContentResolver;
                        cr.Insert(MediaStore.Images.Media.ExternalContentUri, values);
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine("Unable to save to scan file: " + ex1);
                    }

                    var contentUri = Android.Net.Uri.FromFile(f);
                    var mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile, contentUri);
                    context.SendBroadcast(mediaScanIntent);
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("Unable to save to gallery: " + ex2);
                }
            }

            //check to see if we need to rotate if success


            try
            {
                var exif = new ExifInterface(media.Path);
                if (options.RotateImage)
                {
                    await FixOrientationAndResizeAsync(media.Path, options, exif);
                }
                else
                {
                    await ResizeAsync(media.Path, options.PhotoSize, options.CompressionQuality, options.CustomPhotoSize, exif);
                }

                if (options.SaveMetaData && IsValidExif(exif))
				{
					SetMissingMetadata(exif, options.Location);

					try
					{
						exif?.SaveAttributes();
					}
					catch(Exception ex)
					{
						Console.WriteLine($"Unable to save exif {ex}");
					}
				}

				exif?.Dispose();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Unable to check orientation: " + ex);
            }

            return media;
        }

        /// <summary>
        /// Picks a video from the default gallery
        /// </summary>
        /// <returns>Media file of video or null if canceled</returns>
        public async Task<MediaFile> PickVideoAsync()
        {

            if (!(await RequestStoragePermission()))
            {
                throw new MediaPermissionException(Permission.Storage);
            }

            return await TakeMediaAsync("video/*", Intent.ActionPick, null);
        }

        /// <summary>
        /// Take a video with specified options
        /// </summary>
        /// <param name="options">Video Media Options</param>
        /// <returns>Media file of new video or null if canceled</returns>
        public async Task<MediaFile> TakeVideoAsync(StoreVideoOptions options)
        {
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            if (!(await RequestCameraPermissions()))
            {
                throw new MediaPermissionException(Permission.Camera);
            }

            VerifyOptions(options);

            return await TakeMediaAsync("video/*", MediaStore.ActionVideoCapture, options);
        }

        private readonly Context context;
        private int requestId;
        private TaskCompletionSource<MediaFile> completionSource;


		async Task<bool> RequestCameraPermissions()
		{
			//We always have permission on anything lower than marshmallow.
			if ((int)Build.VERSION.SdkInt < 23)
				return true;

            bool checkCamera = HasPermissionInManifest(Android.Manifest.Permission.Camera);

            var hasStoragePermission = await CrossPermissions.Current.CheckPermissionStatusAsync(Permissions.Abstractions.Permission.Storage);
            var hasCameraPermission = Permissions.Abstractions.PermissionStatus.Granted;
            if(checkCamera)
                hasCameraPermission = await CrossPermissions.Current.CheckPermissionStatusAsync(Permissions.Abstractions.Permission.Camera);


            var permissions = new List<Permissions.Abstractions.Permission>();

            if (hasCameraPermission != Permissions.Abstractions.PermissionStatus.Granted)
                permissions.Add(Permissions.Abstractions.Permission.Camera);

            if(hasStoragePermission != Permissions.Abstractions.PermissionStatus.Granted)
                permissions.Add(Permissions.Abstractions.Permission.Storage);

            if (permissions.Count == 0) //good to go!
                return true;

            var results = await CrossPermissions.Current.RequestPermissionsAsync(permissions.ToArray());

			if (results.ContainsKey(Permissions.Abstractions.Permission.Storage) &&
					results[Permissions.Abstractions.Permission.Storage] != Permissions.Abstractions.PermissionStatus.Granted)
			{
				Console.WriteLine("Storage permission Denied.");
				return false;
			}

			if (results.ContainsKey(Permissions.Abstractions.Permission.Camera) &&
					results[Permissions.Abstractions.Permission.Camera] != Permissions.Abstractions.PermissionStatus.Granted)
			{
				Console.WriteLine("Camera permission Denied.");
				return false;
			}

			return true;
		}

        async Task<bool> RequestStoragePermission()
        {
            //We always have permission on anything lower than marshmallow.
            if ((int)Build.VERSION.SdkInt < 23)
                return true;

            var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permissions.Abstractions.Permission.Storage);
            if (status != Permissions.Abstractions.PermissionStatus.Granted)
            {
                Console.WriteLine("Does not have storage permission granted, requesting.");
                var results = await CrossPermissions.Current.RequestPermissionsAsync(Permissions.Abstractions.Permission.Storage);
                if (results.ContainsKey(Permissions.Abstractions.Permission.Storage) &&
                    results[Permissions.Abstractions.Permission.Storage] != Permissions.Abstractions.PermissionStatus.Granted)
                {
                    Console.WriteLine("Storage permission Denied.");
                    return false;
                }
            }

            return true;
        }

        IList<string> requestedPermissions;
		bool HasPermissionInManifest(string permission)
		{
			try
			{
				if (requestedPermissions != null)
					return requestedPermissions.Any(r => r.Equals(permission, StringComparison.InvariantCultureIgnoreCase));

				//try to use current activity else application context
				var permissionContext = CrossCurrentActivity.Current.Activity ?? Android.App.Application.Context;

				if (context == null)
				{
                    System.Diagnostics.Debug.WriteLine("Unable to detect current Activity or App Context. Please ensure Plugin.CurrentActivity is installed in your Android project and your Application class is registering with Application.IActivityLifecycleCallbacks.");
					return false;
				}

				var info = context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);

				if (info == null)
				{
					System.Diagnostics.Debug.WriteLine("Unable to get Package info, will not be able to determine permissions to request.");
					return false;
				}

				requestedPermissions = info.RequestedPermissions;

				if (requestedPermissions == null)
				{
					System.Diagnostics.Debug.WriteLine("There are no requested permissions, please check to ensure you have marked permissions you want to request.");
					return false;
				}

				return requestedPermissions.Any(r => r.Equals(permission, StringComparison.InvariantCultureIgnoreCase));
			}
			catch (Exception ex)
			{
				Console.Write("Unable to check manifest for permission: " + ex);
			}
			return false;
		}


        const string IllegalCharacters = "[|\\?*<\":>/']";
        private void VerifyOptions(StoreMediaOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (System.IO.Path.IsPathRooted(options.Directory))
                throw new ArgumentException("options.Directory must be a relative path", "options");

            if(!string.IsNullOrWhiteSpace(options.Name))
                options.Name = Regex.Replace(options.Name, IllegalCharacters, string.Empty).Replace(@"\", string.Empty);


            if (!string.IsNullOrWhiteSpace(options.Directory))
                options.Directory = Regex.Replace(options.Directory, IllegalCharacters, string.Empty).Replace(@"\", string.Empty);
        }

        private Intent CreateMediaIntent(int id, string type, string action, StoreMediaOptions options, bool tasked = true)
        {
            Intent pickerIntent = new Intent(this.context, typeof(MediaPickerActivity));
            pickerIntent.PutExtra(MediaPickerActivity.ExtraId, id);
            pickerIntent.PutExtra(MediaPickerActivity.ExtraType, type);
            pickerIntent.PutExtra(MediaPickerActivity.ExtraAction, action);
            pickerIntent.PutExtra(MediaPickerActivity.ExtraTasked, tasked);

            if (options != null)
            {
                pickerIntent.PutExtra(MediaPickerActivity.ExtraPath, options.Directory);
                pickerIntent.PutExtra(MediaStore.Images.ImageColumns.Title, options.Name);

                var cameraOptions = (options as StoreCameraMediaOptions);
                if (cameraOptions != null)
                {
                    if (cameraOptions.DefaultCamera == CameraDevice.Front)
                    {
                        pickerIntent.PutExtra("android.intent.extras.CAMERA_FACING", 1);
                    }
                    pickerIntent.PutExtra(MediaPickerActivity.ExtraSaveToAlbum, cameraOptions.SaveToAlbum);
                }
                var vidOptions = (options as StoreVideoOptions);
                if (vidOptions != null)
                {
                    if (vidOptions.DefaultCamera == CameraDevice.Front)
                    {
                        pickerIntent.PutExtra("android.intent.extras.CAMERA_FACING", 1);
                    }
                    pickerIntent.PutExtra(MediaStore.ExtraDurationLimit, (int)vidOptions.DesiredLength.TotalSeconds);
                    pickerIntent.PutExtra(MediaStore.ExtraVideoQuality, (int)vidOptions.Quality);
                    if (vidOptions.DesiredSize != 0)
                    {
                        pickerIntent.PutExtra(MediaStore.ExtraSizeLimit, vidOptions.DesiredSize);
                    }
                }
            }
            //pickerIntent.SetFlags(ActivityFlags.ClearTop);
            pickerIntent.SetFlags(ActivityFlags.NewTask);
            return pickerIntent;
        }

        private int GetRequestId()
        {
            var id = requestId;
            if (requestId == int.MaxValue)
                requestId = 0;
            else
                requestId++;

            return id;
        }

        private Task<MediaFile> TakeMediaAsync(string type, string action, StoreMediaOptions options)
        {
            int id = GetRequestId();

            var ntcs = new TaskCompletionSource<MediaFile>(id);
            if (Interlocked.CompareExchange(ref completionSource, ntcs, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

			context.StartActivity(CreateMediaIntent(id, type, action, options));

            EventHandler<MediaPickedEventArgs> handler = null;
            handler = (s, e) =>
            {
                var tcs = Interlocked.Exchange(ref this.completionSource, null);

                MediaPickerActivity.MediaPicked -= handler;

                if (e.RequestId != id)
                    return;
                
                if(e.IsCanceled)
                    tcs.SetResult(null);
                else if (e.Error != null)
                    tcs.SetException(e.Error);
                else
                    tcs.SetResult(e.Media);
            };

            MediaPickerActivity.MediaPicked += handler;

            return completionSource.Task;
        }

        /// <summary>
        ///  Rotate an image if required and saves it back to disk.
        /// </summary>
        /// <param name="filePath">The file image path</param>
        /// <param name="mediaOptions">The options.</param>
        /// <param name="exif">original metadata</param>
        /// <returns>True if rotation or compression occured, else false</returns>
        public Task<bool> FixOrientationAndResizeAsync(string filePath, PickMediaOptions mediaOptions, ExifInterface exif)
        {
            return FixOrientationAndResizeAsync(
                filePath,
                new StoreCameraMediaOptions
                {
                    PhotoSize = mediaOptions.PhotoSize,
                    CompressionQuality = mediaOptions.CompressionQuality,
                    CustomPhotoSize = mediaOptions.CustomPhotoSize,
                    MaxWidthHeight = mediaOptions.MaxWidthHeight,
                    RotateImage = mediaOptions.RotateImage,
					SaveMetaData = mediaOptions.SaveMetaData
                },
                exif);
        }

        /// <summary>
        ///  Rotate an image if required and saves it back to disk.
        /// </summary>
        /// <param name="filePath">The file image path</param>
        /// <param name="mediaOptions">The options.</param>
        /// <param name="exif">original metadata</param>
        /// <returns>True if rotation or compression occured, else false</returns>
        public Task<bool> FixOrientationAndResizeAsync(string filePath, StoreCameraMediaOptions mediaOptions, ExifInterface exif)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(false);

            try
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var rotation = GetRotation(exif);

                        // if we don't need to rotate, aren't resizing, and aren't adjusting quality then simply return
                        if (rotation == 0 && mediaOptions.PhotoSize == PhotoSize.Full && mediaOptions.CompressionQuality == 100)
                            return false;

                        var percent = 1.0f;
                        switch (mediaOptions.PhotoSize)
                        {
                            case PhotoSize.Large:
                                percent = .75f;
                                break;
                            case PhotoSize.Medium:
                                percent = .5f;
                                break;
                            case PhotoSize.Small:
                                percent = .25f;
                                break;
                            case PhotoSize.Custom:
                                percent = (float)mediaOptions.CustomPhotoSize / 100f;
                                break;
                        }

                        //First decode to just get dimensions
                        var options = new BitmapFactory.Options
                        {
                            InJustDecodeBounds = true
                        };

                        //already on background task
                        BitmapFactory.DecodeFile(filePath, options);

                        if (mediaOptions.PhotoSize == PhotoSize.MaxWidthHeight && mediaOptions.MaxWidthHeight.HasValue)
                        {
                            var max = Math.Max(options.OutWidth, options.OutHeight);
                            if (max > mediaOptions.MaxWidthHeight)
                            {
                                percent = (float)mediaOptions.MaxWidthHeight / (float)max;
                            }
                        }

                        var finalWidth = (int)(options.OutWidth * percent);
                        var finalHeight = (int)(options.OutHeight * percent);

                        //calculate sample size
                        options.InSampleSize = CalculateInSampleSize(options, finalWidth, finalHeight);
                        
                        //turn off decode
                        options.InJustDecodeBounds = false;


                        //this now will return the requested width/height from file, so no longer need to scale
                        var originalImage = BitmapFactory.DecodeFile(filePath, options);

						if (originalImage == null)
							return false;
                        
                        if (finalWidth != originalImage.Width || finalHeight != originalImage.Height)
                        {
                            originalImage = Bitmap.CreateScaledBitmap(originalImage, finalWidth, finalHeight, true);
                        }
                        if (rotation % 180 == 90)
                        {
                            var a = finalWidth;
                            finalWidth = finalHeight;
                            finalHeight = a;
                        }

                        //set scaled and rotated image dimensions
                        exif?.SetAttribute(TAG_PIXEL_X_DIMENSION, Java.Lang.Integer.ToString(finalWidth));
                        exif?.SetAttribute(TAG_PIXEL_Y_DIMENSION, Java.Lang.Integer.ToString(finalHeight));

                        //if we need to rotate then go for it.
                        //then compresse it if needed
                        if (rotation != 0)
                        {
                            var matrix = new Matrix();
                            matrix.PostRotate(rotation);
                            using (var rotatedImage = Bitmap.CreateBitmap(originalImage, 0, 0, originalImage.Width, originalImage.Height, matrix, true))
                            {
                                //always need to compress to save back to disk
                                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                                {
                                    rotatedImage.Compress(Bitmap.CompressFormat.Jpeg, mediaOptions.CompressionQuality, stream);
                                    stream.Close();
                                }
                                rotatedImage.Recycle();
                            }
                            //change the orienation to "not rotated"
                            exif?.SetAttribute(ExifInterface.TagOrientation, Java.Lang.Integer.ToString((int)Orientation.Normal));

                        }
                        else
                        {
                            //always need to compress to save back to disk
                            using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                            {
                                originalImage.Compress(Bitmap.CompressFormat.Jpeg, mediaOptions.CompressionQuality, stream);
                                stream.Close();
                            }
                        }

                        originalImage.Recycle();
                        originalImage.Dispose();
                        // Dispose of the Java side bitmap.
                        GC.Collect();
                        return true;
                        
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        throw ex;
#else
                        return false;
#endif
                    }
                });
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#else
                return Task.FromResult(false);
#endif
            }

        }

        int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            // Raw height and width of image
            var height = options.OutHeight;
            var width = options.OutWidth;
            var inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {

                var halfHeight = height / 2;
                var halfWidth = width / 2;

                // Calculate the largest inSampleSize value that is a power of 2 and keeps both
                // height and width larger than the requested height and width.
                while ((halfHeight / inSampleSize) >= reqHeight
                        && (halfWidth / inSampleSize) >= reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }

        /// <summary>
        /// Resize Image Async
        /// </summary>
        /// <param name="filePath">The file image path</param>
        /// <param name="photoSize">Photo size to go to.</param>
        /// <param name="quality">Image quality (1-100)</param>
        /// <param name="customPhotoSize">Custom size in percent</param>
        /// <param name="exif">original metadata</param>
        /// <returns>True if rotation or compression occured, else false</returns>
        public Task<bool> ResizeAsync(string filePath, PhotoSize photoSize, int quality, int customPhotoSize, ExifInterface exif)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(false);

            try
            {
                return Task.Run(() =>
                {
                    try
                    {
                        if (photoSize == PhotoSize.Full)
                            return false;

                        var percent = 1.0f;
                        switch (photoSize)
                        {
                            case PhotoSize.Large:
                                percent = .75f;
                                break;
                            case PhotoSize.Medium:
                                percent = .5f;
                                break;
                            case PhotoSize.Small:
                                percent = .25f;
                                break;
                            case PhotoSize.Custom:
                                percent = (float)customPhotoSize / 100f;
                                break;
                        }


                        //First decode to just get dimensions
                        var options = new BitmapFactory.Options
                        {
                            InJustDecodeBounds = true
                        };

                        //already on background task
                        BitmapFactory.DecodeFile(filePath, options);

                        var finalWidth = (int)(options.OutWidth * percent);
                        var finalHeight = (int)(options.OutHeight * percent);

                        //set scaled image dimensions
                        exif?.SetAttribute(TAG_PIXEL_X_DIMENSION, Java.Lang.Integer.ToString(finalWidth));
                        exif?.SetAttribute(TAG_PIXEL_Y_DIMENSION, Java.Lang.Integer.ToString(finalHeight));

                        //calculate sample size
                        options.InSampleSize = CalculateInSampleSize(options, finalWidth, finalHeight);

                        //turn off decode
                        options.InJustDecodeBounds = false;


                        //this now will return the requested width/height from file, so no longer need to scale
                        using (var originalImage = BitmapFactory.DecodeFile(filePath, options))
                        {

                            //always need to compress to save back to disk
                            using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                            {

                                originalImage.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                                stream.Close();
                            }
                            
                            originalImage.Recycle();

                            // Dispose of the Java side bitmap.
                            GC.Collect();
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        throw ex;
#else
                        return false;
#endif
                    }
                });
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#else
                return Task.FromResult(false);
#endif
            }
        }

        void SetMissingMetadata(ExifInterface exif, Location location)
        {
            if (exif == null)
                return;
            
            var position = new float[6];
            if (!exif.GetLatLong(position) && location != null)
            {
                exif.SetAttribute(ExifInterface.TagGpsLatitude, CoordinateToRational(location.Latitude));
                exif.SetAttribute(ExifInterface.TagGpsLongitude, CoordinateToRational(location.Longitude));
                exif.SetAttribute(ExifInterface.TagGpsLatitudeRef, location.Latitude > 0 ? "N" : "S");
                exif.SetAttribute(ExifInterface.TagGpsLongitudeRef, location.Longitude > 0 ? "E" : "W");
            }
            if (string.IsNullOrEmpty(exif.GetAttribute(ExifInterface.TagDatetime)))
            {
                exif.SetAttribute(ExifInterface.TagDatetime, DateTime.Now.ToString("yyyy:MM:dd hh:mm:ss"));
            }
            if (string.IsNullOrEmpty(exif.GetAttribute(ExifInterface.TagMake))) {
                exif.SetAttribute(ExifInterface.TagMake, Build.Manufacturer);
            }
            if (string.IsNullOrEmpty(exif.GetAttribute(ExifInterface.TagModel)))
            {
                exif.SetAttribute(ExifInterface.TagModel, Build.Model);
            }
			
			
        }

        private string CoordinateToRational(double coord)
        {
            coord = coord > 0 ? coord : -coord;
            var degrees = (int)coord;
            coord = (coord % 1) * 60;
            var minutes = (int)coord;
            coord = (coord % 1) * 60000;
            var sec = (int)coord;

            return $"{degrees}/1,{minutes}/1,{sec}/1000";
        }

        static int GetRotation(ExifInterface exif)
        {
            if (exif == null)
                return 0;
            try
            {
                var orientation = (Orientation)exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);

                switch (orientation)
                {
                    case Orientation.Rotate90:
                        return 90;
                    case Orientation.Rotate180:
                        return 180;
                    case Orientation.Rotate270:
                        return 270;
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#else
                return 0;
#endif
            }
        }

    }


}
