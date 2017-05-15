using CoreGraphics;
using System;
using System.Drawing;
using UIKit;

namespace Plugin.Media
{
    /// <summary>
    /// Static mathods for UIImage
    /// </summary>
    public static class UIImageExtensions
    {
        /// <summary>
        /// Resize image to maximum size
        /// keeping the aspect ratio
        /// </summary>
        public static UIImage ResizeImageWithAspectRatio(this UIImage sourceImage, float maxWidth, float maxHeight)
        {
            var sourceSize = sourceImage.Size;
            var maxResizeFactor = Math.Max(maxWidth / sourceSize.Width, maxHeight / sourceSize.Height);
            if (maxResizeFactor > 1) return sourceImage;
            var width = maxResizeFactor * sourceSize.Width;
            var height = maxResizeFactor * sourceSize.Height;
            UIGraphics.BeginImageContext(new CGSize(width, height));
            sourceImage.Draw(new CGRect(0, 0, width, height));
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }

        /// <summary>
        /// Resize image, but ignore the aspect ratio
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static UIImage ResizeImage(this UIImage sourceImage, float width, float height)
        {
            UIGraphics.BeginImageContext(new SizeF(width, height));
            sourceImage.Draw(new RectangleF(0, 0, width, height));
            var resultImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return resultImage;
        }

        /// <summary>
        /// Crop image to specitic size and at specific coordinates
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="crop_x"></param>
        /// <param name="crop_y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static UIImage CropImage(this UIImage sourceImage, int crop_x, int crop_y, int width, int height)
        {
            var imgSize = sourceImage.Size;
            UIGraphics.BeginImageContext(new SizeF(width, height));
            var context = UIGraphics.GetCurrentContext();
            var clippedRect = new RectangleF(0, 0, width, height);
            context.ClipToRect(clippedRect);
            var drawRect = new CGRect(-crop_x, -crop_y, imgSize.Width, imgSize.Height);
            sourceImage.Draw(drawRect);
            var modifiedImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return modifiedImage;
        }

        public static UIImage ScaleImageWithOrientation(this UIImage sourceImage, float width, float height)
        {
		    const float PI_2 = (float)(Math.PI / 2.0);
            UIImage resultImage;

            using (CGImage image = sourceImage.CGImage)
            {
                CGImageAlphaInfo alpha = image.AlphaInfo == CGImageAlphaInfo.None ? CGImageAlphaInfo.NoneSkipLast  : image.AlphaInfo;
                CGColorSpace color = CGColorSpace.CreateDeviceRGB();
                var bitmap = new CGBitmapContext(IntPtr.Zero, (int)width, (int)height, image.BitsPerComponent, image.BytesPerRow, color, alpha);
                bitmap.DrawImage(new Rectangle(0, 0, (int)width, (int)height), image);

                switch (sourceImage.Orientation)
                {
                    case UIImageOrientation.Left:
                        bitmap.RotateCTM(PI_2);
                        bitmap.TranslateCTM(0, -height);
                        break;
                    case UIImageOrientation.Right:
                        bitmap.RotateCTM(-PI_2);
                        bitmap.TranslateCTM(-width, 0);
                        break;
                    case UIImageOrientation.Down:
                        bitmap.TranslateCTM(width, height);
                        bitmap.RotateCTM(-(float)Math.PI);
                        break;
                }

                resultImage = UIImage.FromImage(bitmap.ToImage());
            }

            return resultImage;
        }
    }
}