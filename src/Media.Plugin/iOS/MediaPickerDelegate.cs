using System;
using System.IO;
using System.Threading.Tasks;

using Plugin.Media.Abstractions;

using CoreGraphics;
using AssetsLibrary;
using Foundation;
using UIKit;
using NSAction = global::System.Action;
using System.Globalization;
using ImageIO;
using MobileCoreServices;
using System.Drawing;

namespace Plugin.Media
{
	internal class MediaPickerDelegate : UIImagePickerControllerDelegate
	{
		internal MediaPickerDelegate(UIViewController viewController, UIImagePickerControllerSourceType sourceType, StoreCameraMediaOptions options)
		{
			this.viewController = viewController;
			source = sourceType;
			this.options = options ?? new StoreCameraMediaOptions();

			if (viewController != null)
			{
				UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
				observer = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, DidRotate);
			}
		}

		public UIPopoverController Popover
		{
			get;
			set;
		}

		public UIView View => viewController.View;

		public Task<MediaFile> Task => tcs.Task;

		public override async void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
		{
			RemoveOrientationChangeObserverAndNotifications();

			MediaFile mediaFile;
			switch ((NSString)info[UIImagePickerController.MediaType])
			{
				case MediaImplementation.TypeImage:
					mediaFile = await GetPictureMediaFile(info);
					break;

				case MediaImplementation.TypeMovie:
					mediaFile = await GetMovieMediaFile(info);
					break;

				default:
					throw new NotSupportedException();
			}

			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
			{
				UIApplication.SharedApplication.SetStatusBarStyle(MediaImplementation.StatusBarStyle, false);
			}

			Dismiss(picker, () =>
			{
                if (mediaFile == null)
                    tcs.SetException(new FileNotFoundException());
				else
					tcs.TrySetResult(mediaFile);
			});
		}

		public override void Canceled(UIImagePickerController picker)
		{
			RemoveOrientationChangeObserverAndNotifications();

			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
			{
				UIApplication.SharedApplication.SetStatusBarStyle(MediaImplementation.StatusBarStyle, false);
			}

			Dismiss(picker, () =>
			{
				tcs.SetResult(null);
			});
		}

		public void DisplayPopover(bool hideFirst = false)
		{
			if (Popover == null)
				return;

			var swidth = UIScreen.MainScreen.Bounds.Width;
			var sheight = UIScreen.MainScreen.Bounds.Height;

			nfloat width = 400;
			nfloat height = 300;


			if (orientation == null)
			{
				if (IsValidInterfaceOrientation(UIDevice.CurrentDevice.Orientation))
					orientation = UIDevice.CurrentDevice.Orientation;
				else
					orientation = GetDeviceOrientation(viewController.InterfaceOrientation);
			}

			double x, y;
			if (orientation == UIDeviceOrientation.LandscapeLeft || orientation == UIDeviceOrientation.LandscapeRight)
			{
				x = (Math.Max(swidth, sheight) - width) / 2;
				y = (Math.Min(swidth, sheight) - height) / 2;
			}
			else
			{
				x = (Math.Min(swidth, sheight) - width) / 2;
				y = (Math.Max(swidth, sheight) - height) / 2;
			}

			if (hideFirst && Popover.PopoverVisible)
				Popover.Dismiss(animated: false);

			Popover.PresentFromRect(new CGRect(x, y, width, height), View, 0, animated: true);
		}

		private UIDeviceOrientation? orientation;
		private NSObject observer;
		private readonly UIViewController viewController;
		private readonly UIImagePickerControllerSourceType source;
		private TaskCompletionSource<MediaFile> tcs = new TaskCompletionSource<MediaFile>();
		private readonly StoreCameraMediaOptions options;

		private bool IsCaptured =>
			source == UIImagePickerControllerSourceType.Camera;

		private void Dismiss(UIImagePickerController picker, NSAction onDismiss)
		{
			if (viewController == null)
			{
				onDismiss();
				tcs = new TaskCompletionSource<MediaFile>();
			}
			else
			{
				if (Popover != null)
				{
					Popover.Dismiss(animated: true);
					try
					{
						Popover.Dispose();
					}
					catch
					{

					}
					Popover = null;

					onDismiss();
				}
				else
				{
					picker.DismissViewController(true, onDismiss);
					try
					{
						picker.Dispose();
					}
					catch
					{

					}
				}
			}
		}

		private void RemoveOrientationChangeObserverAndNotifications()
		{
			if (viewController != null)
			{
				UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
				NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
				observer.Dispose();
			}
		}

		private void DidRotate(NSNotification notice)
		{
			var device = (UIDevice)notice.Object;
			if (!IsValidInterfaceOrientation(device.Orientation) || Popover == null)
				return;
			if (orientation.HasValue && IsSameOrientationKind(orientation.Value, device.Orientation))
				return;

			if (UIDevice.CurrentDevice.CheckSystemVersion(6, 0))
			{
				if (!GetShouldRotate6(device.Orientation))
					return;
			}
			else if (!GetShouldRotate(device.Orientation))
				return;

			var co = orientation;
			orientation = device.Orientation;

			if (co == null)
				return;

			DisplayPopover(hideFirst: true);
		}

		private bool GetShouldRotate(UIDeviceOrientation orientation)
		{
			var iorientation = UIInterfaceOrientation.Portrait;
			switch (orientation)
			{
				case UIDeviceOrientation.LandscapeLeft:
					iorientation = UIInterfaceOrientation.LandscapeLeft;
					break;

				case UIDeviceOrientation.LandscapeRight:
					iorientation = UIInterfaceOrientation.LandscapeRight;
					break;

				case UIDeviceOrientation.Portrait:
					iorientation = UIInterfaceOrientation.Portrait;
					break;

				case UIDeviceOrientation.PortraitUpsideDown:
					iorientation = UIInterfaceOrientation.PortraitUpsideDown;
					break;

				default: return false;
			}

			return viewController.ShouldAutorotateToInterfaceOrientation(iorientation);
		}

		private bool GetShouldRotate6(UIDeviceOrientation orientation)
		{
			if (!viewController.ShouldAutorotate())
				return false;

			UIInterfaceOrientationMask mask = UIInterfaceOrientationMask.Portrait;
			switch (orientation)
			{
				case UIDeviceOrientation.LandscapeLeft:
					mask = UIInterfaceOrientationMask.LandscapeLeft;
					break;

				case UIDeviceOrientation.LandscapeRight:
					mask = UIInterfaceOrientationMask.LandscapeRight;
					break;

				case UIDeviceOrientation.Portrait:
					mask = UIInterfaceOrientationMask.Portrait;
					break;

				case UIDeviceOrientation.PortraitUpsideDown:
					mask = UIInterfaceOrientationMask.PortraitUpsideDown;
					break;

				default: return false;
			}

			return viewController.GetSupportedInterfaceOrientations().HasFlag(mask);
		}


		private async Task<MediaFile> GetPictureMediaFile(NSDictionary info)
		{
			var image = (UIImage)info[UIImagePickerController.EditedImage] ?? (UIImage)info[UIImagePickerController.OriginalImage];

            if (image == null)
                return null;

			var path = GetOutputPath(MediaImplementation.TypeImage,
				options.Directory ?? ((IsCaptured) ? string.Empty : "temp"),
				options.Name);

			var cgImage = image.CGImage;

			var percent = 1.0f;
			float newHeight = image.CGImage.Height;
			float newWidth = image.CGImage.Width;

			if (options.PhotoSize != PhotoSize.Full)
			{
				try
				{
					switch (options.PhotoSize)
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
							percent = (float)options.CustomPhotoSize / 100f;
							break;
					}

					if (options.PhotoSize == PhotoSize.MaxWidthHeight && options.MaxWidthHeight.HasValue)
					{
						var max = Math.Max(image.CGImage.Width, image.CGImage.Height);
						if (max > options.MaxWidthHeight.Value)
						{
							percent = (float)options.MaxWidthHeight.Value / (float)max;
						}
					}

					if (percent < 1.0f)
					{
						//begin resizing image
						image = image.ResizeImageWithAspectRatio(percent);
					}

				}
				catch (Exception ex)
				{
					Console.WriteLine($"Unable to compress image: {ex}");
				}
			}


			NSDictionary meta = null;
			try
			{
				if (options.SaveMetaData)
				{
					if (source == UIImagePickerControllerSourceType.Camera)
					{
						meta = info[UIImagePickerController.MediaMetadata] as NSDictionary;
						if (meta != null && meta.ContainsKey(ImageIO.CGImageProperties.Orientation))
						{
							var newMeta = new NSMutableDictionary();
							newMeta.SetValuesForKeysWithDictionary(meta);
							var newTiffDict = new NSMutableDictionary();
							newTiffDict.SetValuesForKeysWithDictionary(meta[ImageIO.CGImageProperties.TIFFDictionary] as NSDictionary);
							newTiffDict.SetValueForKey(meta[ImageIO.CGImageProperties.Orientation], ImageIO.CGImageProperties.TIFFOrientation);
							newMeta[ImageIO.CGImageProperties.TIFFDictionary] = newTiffDict;

							meta = newMeta;
						}
						var location = options.Location;
						if (meta != null && location != null)
						{
							meta = SetGpsLocation(meta, location);
						}
					}
					else
					{
						meta = PhotoLibraryAccess.GetPhotoLibraryMetadata(info[UIImagePickerController.ReferenceUrl] as NSUrl);
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unable to get metadata: {ex}");
			}

			//iOS quality is 0.0-1.0
			var quality = (options.CompressionQuality / 100f);
			var savedImage = false;
			if (meta != null)
				savedImage = SaveImageWithMetadata(image, quality, meta, path);

			if (!savedImage)
			{
				var finalQuality = quality;
				var imageData = image.AsJPEG(finalQuality);

				//continue to move down quality , rare instances
				while (imageData == null && finalQuality > 0)
				{
					finalQuality -= 0.05f;
					imageData = image.AsJPEG(finalQuality);
				}

				if (imageData == null)
					throw new NullReferenceException("Unable to convert image to jpeg, please ensure file exists or lower quality level");


				imageData.Save(path, true);
				imageData.Dispose();
			}


			string aPath = null;
			if (source != UIImagePickerControllerSourceType.Camera)
			{

				//try to get the album path's url
				var url = (NSUrl)info[UIImagePickerController.ReferenceUrl];
				aPath = url?.AbsoluteString;
			}
			else
			{
				if (options.SaveToAlbum)
				{
					try
					{
						var library = new ALAssetsLibrary();
						var albumSave = await library.WriteImageToSavedPhotosAlbumAsync(cgImage, meta);
						aPath = albumSave.AbsoluteString;
					}
					catch (Exception ex)
					{
						Console.WriteLine("unable to save to album:" + ex);
					}
				}

			}

			Func<Stream> getStreamForExternalStorage = () =>
			{
				if (options.RotateImage)
					return RotateImage(image, options.CompressionQuality);
				else
					return File.OpenRead(path);
			};

			return new MediaFile(path, () => File.OpenRead(path), streamGetterForExternalStorage: () => getStreamForExternalStorage(), albumPath: aPath);
		}

		private static NSDictionary SetGpsLocation(NSDictionary meta, Location location)
		{
			var newMeta = new NSMutableDictionary();
			newMeta.SetValuesForKeysWithDictionary(meta);
			var newGpsDict = new NSMutableDictionary();
			newGpsDict.SetValueForKey(new NSNumber(Math.Abs(location.Latitude)), ImageIO.CGImageProperties.GPSLatitude);
			newGpsDict.SetValueForKey(new NSString(location.Latitude > 0 ? "N" : "S"), ImageIO.CGImageProperties.GPSLatitudeRef);
			newGpsDict.SetValueForKey(new NSNumber(Math.Abs(location.Longitude)), ImageIO.CGImageProperties.GPSLongitude);
			newGpsDict.SetValueForKey(new NSString(location.Longitude > 0 ? "E" : "W"), ImageIO.CGImageProperties.GPSLongitudeRef);
			newGpsDict.SetValueForKey(new NSNumber(location.Altitude), ImageIO.CGImageProperties.GPSAltitude);
			newGpsDict.SetValueForKey(new NSNumber(0), ImageIO.CGImageProperties.GPSAltitudeRef);
			newGpsDict.SetValueForKey(new NSNumber(location.Speed), ImageIO.CGImageProperties.GPSSpeed);
			newGpsDict.SetValueForKey(new NSString("K"), ImageIO.CGImageProperties.GPSSpeedRef);
			newGpsDict.SetValueForKey(new NSNumber(location.Direction), ImageIO.CGImageProperties.GPSImgDirection);
			newGpsDict.SetValueForKey(new NSString("T"), ImageIO.CGImageProperties.GPSImgDirectionRef);
			newGpsDict.SetValueForKey(new NSString(location.Timestamp.ToString("hh:mm:ss")), ImageIO.CGImageProperties.GPSTimeStamp);
			newGpsDict.SetValueForKey(new NSString(location.Timestamp.ToString("yyyy:MM:dd")), ImageIO.CGImageProperties.GPSDateStamp);
			newMeta[ImageIO.CGImageProperties.GPSDictionary] = newGpsDict;
			return newMeta;
		}

		private bool SaveImageWithMetadata(UIImage image, float quality, NSDictionary meta, string path)
		{
			try
			{
				var finalQuality = quality;
				var imageData = image.AsJPEG(finalQuality);
				//continue to move down quality , rare instances
				while (imageData == null && finalQuality > 0)
				{
					finalQuality -= 0.05f;
					imageData = image.AsJPEG(finalQuality);
				}

				if (imageData == null)
					throw new NullReferenceException("Unable to convert image to jpeg, please ensure file exists or lower quality level");

				var dataProvider = new CGDataProvider(imageData);
				var cgImageFromJpeg = CGImage.FromJPEG(dataProvider, null, false, CGColorRenderingIntent.Default);
				var imageWithExif = new NSMutableData();
				var destination = CGImageDestination.Create(imageWithExif, UTType.JPEG, 1);
				var cgImageMetadata = new CGMutableImageMetadata();
				var destinationOptions = new CGImageDestinationOptions();

				if (meta.ContainsKey(ImageIO.CGImageProperties.Orientation))
					destinationOptions.Dictionary[ImageIO.CGImageProperties.Orientation] = meta[ImageIO.CGImageProperties.Orientation];

				if (meta.ContainsKey(ImageIO.CGImageProperties.DPIWidth))
					destinationOptions.Dictionary[ImageIO.CGImageProperties.DPIWidth] = meta[ImageIO.CGImageProperties.DPIWidth];

				if (meta.ContainsKey(ImageIO.CGImageProperties.DPIHeight))
					destinationOptions.Dictionary[ImageIO.CGImageProperties.DPIHeight] = meta[ImageIO.CGImageProperties.DPIHeight];


				if (meta.ContainsKey(ImageIO.CGImageProperties.ExifDictionary))
				{

					destinationOptions.ExifDictionary =
										  new CGImagePropertiesExif(meta[ImageIO.CGImageProperties.ExifDictionary] as NSDictionary);

				}


				if (meta.ContainsKey(ImageIO.CGImageProperties.TIFFDictionary))
				{
					destinationOptions.TiffDictionary = new CGImagePropertiesTiff(meta[ImageIO.CGImageProperties.TIFFDictionary] as NSDictionary);

				}
				if (meta.ContainsKey(ImageIO.CGImageProperties.GPSDictionary))
				{
					destinationOptions.GpsDictionary =
						new CGImagePropertiesGps(meta[ImageIO.CGImageProperties.GPSDictionary] as NSDictionary);
				}
				if (meta.ContainsKey(ImageIO.CGImageProperties.JFIFDictionary))
				{
					destinationOptions.JfifDictionary =
						new CGImagePropertiesJfif(meta[ImageIO.CGImageProperties.JFIFDictionary] as NSDictionary);
				}
				if (meta.ContainsKey(ImageIO.CGImageProperties.IPTCDictionary))
				{
					destinationOptions.IptcDictionary =
						new CGImagePropertiesIptc(meta[ImageIO.CGImageProperties.IPTCDictionary] as NSDictionary);
				}
				destination.AddImageAndMetadata(cgImageFromJpeg, cgImageMetadata, destinationOptions);
				var success = destination.Close();
				if (success)
				{
					imageWithExif.Save(path, true);
				}
				return success;

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unable to save image with metadata: {ex}");
			}

			return false;
		}


		private async Task<MediaFile> GetMovieMediaFile(NSDictionary info)
		{
			var url = (NSUrl)info[UIImagePickerController.MediaURL];

			var path = GetOutputPath(MediaImplementation.TypeMovie,
					  options.Directory ?? ((IsCaptured) ? string.Empty : "temp"),
					  options.Name ?? Path.GetFileName(url.Path));

			File.Move(url.Path, path);

			string aPath = null;
			if (source != UIImagePickerControllerSourceType.Camera)
			{
				//try to get the album path's url
				var url2 = (NSUrl)info[UIImagePickerController.ReferenceUrl];
				aPath = url2?.AbsoluteString;
			}
			else
			{
				if (options.SaveToAlbum)
				{
					try
					{
						var library = new ALAssetsLibrary();
						var albumSave = await library.WriteVideoToSavedPhotosAlbumAsync(new NSUrl(path));
						aPath = albumSave.AbsoluteString;
					}
					catch (Exception ex)
					{
						Console.WriteLine("unable to save to album:" + ex);
					}
				}
			}

			return new MediaFile(path, () => File.OpenRead(path), albumPath: aPath);
		}

		private static string GetUniquePath(string type, string path, string name)
		{
			var isPhoto = (type == MediaImplementation.TypeImage);
			var ext = Path.GetExtension(name);
			if (ext == string.Empty)
				ext = ((isPhoto) ? ".jpg" : ".mp4");

			name = Path.GetFileNameWithoutExtension(name);

			var nname = name + ext;
			var i = 1;
			while (File.Exists(Path.Combine(path, nname)))
				nname = name + "_" + (i++) + ext;

			return Path.Combine(path, nname);
		}

		private static string GetOutputPath(string type, string path, string name)
		{
			path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path);
			Directory.CreateDirectory(path);

			if (string.IsNullOrWhiteSpace(name))
			{
				var timestamp = DateTime.Now.ToString("yyyMMdd_HHmmss", CultureInfo.InvariantCulture);
				if (type == MediaImplementation.TypeImage)
					name = "IMG_" + timestamp + ".jpg";
				else
					name = "VID_" + timestamp + ".mp4";
			}

			return Path.Combine(path, GetUniquePath(type, path, name));
		}

		private static bool IsValidInterfaceOrientation(UIDeviceOrientation self)
		{
			return (self != UIDeviceOrientation.FaceUp && self != UIDeviceOrientation.FaceDown && self != UIDeviceOrientation.Unknown);
		}

		private static bool IsSameOrientationKind(UIDeviceOrientation o1, UIDeviceOrientation o2)
		{
			if (o1 == UIDeviceOrientation.FaceDown || o1 == UIDeviceOrientation.FaceUp)
				return (o2 == UIDeviceOrientation.FaceDown || o2 == UIDeviceOrientation.FaceUp);
			if (o1 == UIDeviceOrientation.LandscapeLeft || o1 == UIDeviceOrientation.LandscapeRight)
				return (o2 == UIDeviceOrientation.LandscapeLeft || o2 == UIDeviceOrientation.LandscapeRight);
			if (o1 == UIDeviceOrientation.Portrait || o1 == UIDeviceOrientation.PortraitUpsideDown)
				return (o2 == UIDeviceOrientation.Portrait || o2 == UIDeviceOrientation.PortraitUpsideDown);

			return false;
		}

		private static UIDeviceOrientation GetDeviceOrientation(UIInterfaceOrientation self)
		{
			switch (self)
			{
				case UIInterfaceOrientation.LandscapeLeft:
					return UIDeviceOrientation.LandscapeLeft;
				case UIInterfaceOrientation.LandscapeRight:
					return UIDeviceOrientation.LandscapeRight;
				case UIInterfaceOrientation.Portrait:
					return UIDeviceOrientation.Portrait;
				case UIInterfaceOrientation.PortraitUpsideDown:
					return UIDeviceOrientation.PortraitUpsideDown;
				default:
					throw new InvalidOperationException();
			}
		}

		public static Stream RotateImage(UIImage image, int compressionQuality)
		{
			UIImage imageToReturn = null;
			if (image.Orientation == UIImageOrientation.Up)
			{
				imageToReturn = image;
			}
			else
			{
				CGAffineTransform transform = CGAffineTransform.MakeIdentity();

				switch (image.Orientation)
				{
					case UIImageOrientation.Down:
					case UIImageOrientation.DownMirrored:
						transform.Rotate((float)Math.PI);
						transform.Translate(image.Size.Width, image.Size.Height);
						break;

					case UIImageOrientation.Left:
					case UIImageOrientation.LeftMirrored:
						transform.Rotate((float)Math.PI / 2);
						transform.Translate(image.Size.Width, 0);
						break;

					case UIImageOrientation.Right:
					case UIImageOrientation.RightMirrored:
						transform.Rotate(-(float)Math.PI / 2);
						transform.Translate(0, image.Size.Height);
						break;
					case UIImageOrientation.Up:
					case UIImageOrientation.UpMirrored:
						break;
				}

				switch (image.Orientation)
				{
					case UIImageOrientation.UpMirrored:
					case UIImageOrientation.DownMirrored:
						transform.Translate(image.Size.Width, 0);
						transform.Scale(-1, 1);
						break;

					case UIImageOrientation.LeftMirrored:
					case UIImageOrientation.RightMirrored:
						transform.Translate(image.Size.Height, 0);
						transform.Scale(-1, 1);
						break;
					case UIImageOrientation.Up:
					case UIImageOrientation.Down:
					case UIImageOrientation.Left:
					case UIImageOrientation.Right:
						break;
				}

				using (var context = new CGBitmapContext(IntPtr.Zero,
														(int)image.Size.Width,
														(int)image.Size.Height,
														image.CGImage.BitsPerComponent,
														image.CGImage.BytesPerRow,
														image.CGImage.ColorSpace,
														image.CGImage.BitmapInfo))
				{
					context.ConcatCTM(transform);
					switch (image.Orientation)
					{
						case UIImageOrientation.Left:
						case UIImageOrientation.LeftMirrored:
						case UIImageOrientation.Right:
						case UIImageOrientation.RightMirrored:
							context.DrawImage(new RectangleF(PointF.Empty, new SizeF((float)image.Size.Height, (float)image.Size.Width)), image.CGImage);
							break;
						default:
							context.DrawImage(new RectangleF(PointF.Empty, new SizeF((float)image.Size.Width, (float)image.Size.Height)), image.CGImage);
							break;
					}

					using (var imageRef = context.ToImage())
					{
						imageToReturn = new UIImage(imageRef, 1, UIImageOrientation.Up);
					}
				}
			}

			var finalQuality = compressionQuality / 100f;
			var imageData = imageToReturn.AsJPEG(finalQuality);
			//continue to move down quality , rare instances
			while (imageData == null && finalQuality > 0)
			{
				finalQuality -= 0.05f;
				imageData = imageToReturn.AsJPEG(finalQuality);
			}

			if (imageData == null)
				throw new NullReferenceException("Unable to convert image to jpeg, please ensure file exists or lower quality level");

			var stream = new MemoryStream();
			imageData.AsStream().CopyTo(stream);
			imageData.Dispose();
			return stream;

		}
	}
}
