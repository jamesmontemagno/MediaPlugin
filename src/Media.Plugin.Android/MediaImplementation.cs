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

namespace Plugin.Media
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    [Android.Runtime.Preserve(AllMembers = true)]
    public class MediaImplementation : IMedia
    {
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Intent GetPickPhotoUI()
        {
            int id = GetRequestId();
            return CreateMediaIntent(id, "image/*", Intent.ActionPick, null, tasked: false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Intent GetTakePhotoUI(StoreCameraMediaOptions options)
        {
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            VerifyOptions(options);

            int id = GetRequestId();
            return CreateMediaIntent(id, "image/*", MediaStore.ActionImageCapture, options, tasked: false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Intent GetPickVideoUI()
        {
            int id = GetRequestId();
            return CreateMediaIntent(id, "video/*", Intent.ActionPick, null, tasked: false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Intent GetTakeVideoUI(StoreVideoOptions options)
        {
            if (!IsCameraAvailable)
                throw new NotSupportedException();

            VerifyOptions(options);

            return CreateMediaIntent(GetRequestId(), "video/*", MediaStore.ActionVideoCapture, options, tasked: false);
        }

        /// <summary>
        /// Picks a photo from the default gallery
        /// </summary>
        /// <returns>Media file or null if canceled</returns>
        public async Task<MediaFile> PickPhotoAsync(PickMediaOptions options = null)
        {
            if (!(await RequestStoragePermission()))
            {
                return null;
            }
            var media = await TakeMediaAsync("image/*", Intent.ActionPick, null);

            if (options == null)
                return media;

            //check to see if we need to rotate if success
            if (!string.IsNullOrWhiteSpace(media?.Path) && options.PhotoSize != PhotoSize.Full)
            {
                try
                {
                    await ResizeAsync(media.Path, options.PhotoSize, options.CompressionQuality);
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

            if (!(await RequestStoragePermission()))
            {
                return null;
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
                await FixOrientationAndResizeAsync(media.Path, options.PhotoSize, options.CompressionQuality);
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
                return null;
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

            if (!(await RequestStoragePermission()))
            {
                return null;
            }

            VerifyOptions(options);

            return await TakeMediaAsync("video/*", MediaStore.ActionVideoCapture, options);
        }

        private readonly Context context;
        private int requestId;
        private TaskCompletionSource<MediaFile> completionSource;


        async Task<bool> RequestStoragePermission()
        {
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

        private void VerifyOptions(StoreMediaOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");
            if (System.IO.Path.IsPathRooted(options.Directory))
                throw new ArgumentException("options.Directory must be a relative path", "options");
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
                }
            }
            //pickerIntent.SetFlags(ActivityFlags.ClearTop);
            pickerIntent.SetFlags(ActivityFlags.NewTask);
            return pickerIntent;
        }

        private int GetRequestId()
        {
            int id = this.requestId;
            if (this.requestId == Int32.MaxValue)
                this.requestId = 0;
            else
                this.requestId++;

            return id;
        }

        private Task<MediaFile> TakeMediaAsync(string type, string action, StoreMediaOptions options)
        {
            int id = GetRequestId();

            var ntcs = new TaskCompletionSource<MediaFile>(id);
            if (Interlocked.CompareExchange(ref this.completionSource, ntcs, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

            this.context.StartActivity(CreateMediaIntent(id, type, action, options));

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
        /// <param name="photoSize">Photo size to go to.</param>
        /// <returns>True if rotation or compression occured, else false</returns>
        public Task<bool> FixOrientationAndResizeAsync(string filePath, PhotoSize photoSize, int quality)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(false);

            try
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var rotation = GetRotation(filePath);

                        if (rotation == 0 && photoSize == PhotoSize.Full)
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
                        }

                        using (var originalImage = BitmapFactory.DecodeFile(filePath))
                        {
                           

                            //if we need to rotate then go for it.
                            //then compresse it if needed
                            if (rotation != 0)
                            {
                                var matrix = new Matrix();
                                matrix.PostRotate(rotation);
                                using (var rotatedImage = Bitmap.CreateBitmap(originalImage, 0, 0, originalImage.Width, originalImage.Height, matrix, true))
                                {
                                    if (photoSize != PhotoSize.Full)
                                    {
                                        using (var compressedImage = Bitmap.CreateScaledBitmap(rotatedImage, (int)(rotatedImage.Width * percent), (int)(rotatedImage.Height * percent), false))
                                        {
                                            using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                                            {
                                                compressedImage.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                                                stream.Close();
                                            }
                                            compressedImage.Recycle();
                                        }
                                    }
                                    else
                                    {
                                        using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                                        {
                                            rotatedImage.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                                            stream.Close();
                                        }
                                    }
                                    rotatedImage.Recycle();
                                }
                                originalImage.Recycle();
                                // Dispose of the Java side bitmap.
                                GC.Collect();
                                return true;
                            }


                            using (var compressedImage = Bitmap.CreateScaledBitmap(originalImage, (int)(originalImage.Width * percent), (int)(originalImage.Height * percent), false))
                            {
                                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                                {
                                    compressedImage.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                                    stream.Close();
                                }

                                compressedImage.Recycle();
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

        /// <summary>
        /// Resize Image Async
        /// </summary>
        /// <param name="filePath">The file image path</param>
        /// <param name="photoSize">Photo size to go to.</param>
        /// <returns>True if rotation or compression occured, else false</returns>
        public Task<bool> ResizeAsync(string filePath, PhotoSize photoSize, int quality)
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
                        }

                        using (var originalImage = BitmapFactory.DecodeFile(filePath))
                        {
                            
                            using (var compressedImage = Bitmap.CreateScaledBitmap(originalImage, (int)(originalImage.Width * percent), (int)(originalImage.Height * percent), false))
                            {
                                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                                {
                                   
                                    compressedImage.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                                    stream.Close();
                                }

                                compressedImage.Recycle();

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


        static int GetRotation(string filePath)
        {
            try
            {
                using (var ei = new ExifInterface(filePath))
                {
                    var orientation = (Orientation)ei.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);

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
