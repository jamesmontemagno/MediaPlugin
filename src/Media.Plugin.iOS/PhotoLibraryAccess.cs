using System;
using CoreImage;
using Foundation;
using Photos;

namespace Plugin.Media
{
	public static class PhotoLibraryAccess
	{
		public static NSDictionary GetPhotoLibraryMetadata(NSUrl url)
		{
			NSDictionary meta = null;

			var image = PHAsset.FetchAssets(new NSUrl[] { url }, new PHFetchOptions()).firstObject as PHAsset;
			var imageManager = PHImageManager.DefaultManager;
			var requestOptions = new PHImageRequestOptions
			{
				Synchronous = true,
				NetworkAccessAllowed = true,
				DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
			};
			imageManager.RequestImageData(image, requestOptions, (data, dataUti, orientation, info) =>
			{
				try
				{
					var fullimage = CIImage.FromData(data);
					if (fullimage?.Properties != null)
					{
						meta = new NSMutableDictionary();
						meta[ImageIO.CGImageProperties.Orientation] = new NSString(fullimage.Properties.Orientation.ToString());
						meta[ImageIO.CGImageProperties.ExifDictionary] = fullimage.Properties.Exif?.Dictionary ?? new NSDictionary();
						meta[ImageIO.CGImageProperties.TIFFDictionary] = fullimage.Properties.Tiff?.Dictionary ?? new NSDictionary();
						meta[ImageIO.CGImageProperties.GPSDictionary] = fullimage.Properties.Gps?.Dictionary ?? new NSDictionary();
						meta[ImageIO.CGImageProperties.IPTCDictionary] = fullimage.Properties.Iptc?.Dictionary ?? new NSDictionary();
						meta[ImageIO.CGImageProperties.JFIFDictionary] = fullimage.Properties.Jfif?.Dictionary ?? new NSDictionary();
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}

			});

			return meta;
		}
	}
}
