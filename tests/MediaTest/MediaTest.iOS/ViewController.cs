using Foundation;
using Plugin.Media;
using System;
using System.Linq;

using UIKit;

namespace MediaTest.iOS
{
    public partial class ViewController : UIViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TakePhoto.Enabled = CrossMedia.Current.IsTakePhotoSupported;
            PickPhoto.Enabled = CrossMedia.Current.IsPickPhotoSupported;

            TakeVideo.Enabled = CrossMedia.Current.IsTakeVideoSupported;
            PickVideo.Enabled = CrossMedia.Current.IsPickVideoSupported;

            TakePhoto.TouchUpInside += async (sender, args) =>
            {
                var test = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    Name = "test1.jpg",
                    SaveToAlbum = AlbumSwitch.On,
                    PhotoSize = SizeSwitch.On ? Plugin.Media.Abstractions.PhotoSize.Medium : Plugin.Media.Abstractions.PhotoSize.Full
                });

                if (test == null)
                    return;

                new UIAlertView("Success", test.Path, null, "OK").Show();

                var stream = test.GetStream();
                using (var data = NSData.FromStream(stream))
                    MainImage.Image = UIImage.LoadFromData(data);

                test.Dispose();
            };

            PickPhoto.TouchUpInside += async (sender, args) =>
            {
                var test = await CrossMedia.Current.PickPhotoAsync();
                if (test == null)
                    return;

                new UIAlertView("Success", test.Path, null, "OK").Show();

                var stream = test.GetStream();
                using (var data = NSData.FromStream(stream))
                    MainImage.Image = UIImage.LoadFromData(data);

                test.Dispose();
            };

            TakeVideo.TouchUpInside += async (sender, args) =>
            {
                var test = await CrossMedia.Current.TakeVideoAsync(new Plugin.Media.Abstractions.StoreVideoOptions
                {
                    Name = "test1.mp4",
                    SaveToAlbum = true
                });

                if (test == null)
                    return;

                new UIAlertView("Success", test.Path, null, "OK").Show();

                test.Dispose();
            };

            PickVideo.TouchUpInside += async (sender, args) =>
            {
                var test = await CrossMedia.Current.PickVideoAsync();
                if (test == null)
                    return;

                new UIAlertView("Success", test.Path, null, "OK").Show();

                test.Dispose();
            };
            // Perform any additional setup after loading the view, typically from a nib.
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}