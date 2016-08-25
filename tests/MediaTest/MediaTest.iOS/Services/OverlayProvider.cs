using System;
using UIKit;
using System.Drawing;
using Foundation;

namespace MediaTest.iOS
{
	public class OverlayProvider
	{
		public object ProvideOverlay()
		{
			return new Overlay();
		}

		private class Overlay : UIView
		{
			private NSObject _deviceOrientationObserver;
			private NSObject _captureItemObserver;
			private NSObject _rejectItemObserver;

			public Overlay() : base()
			{
				UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
				_deviceOrientationObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, HandleDeviceOrientationChange);
				_captureItemObserver = NSNotificationCenter.DefaultCenter.AddObserver(new NSString("_UIImagePickerControllerUserDidCaptureItem"), HandleUserCapturedItem);
				_rejectItemObserver = NSNotificationCenter.DefaultCenter.AddObserver(new NSString("_UIImagePickerControllerUserDidRejectItem"), HandleUserRejectedItem);

				var lbl = new UILabel(new RectangleF(10, (float)((UIScreen.MainScreen.Bounds.Height - 120) / 2), 300, 120));
				lbl.TextColor = UIColor.Red;
				lbl.Text = "Turn the camera around!";
				lbl.LineBreakMode = UILineBreakMode.WordWrap;
				lbl.Lines = 3;
				lbl.TextAlignment = UITextAlignment.Center;

				lbl.Font = lbl.Font.WithSize(24.0f);
				this.Add(lbl);

				Hidden = ShouldHideOverlay();
			}

			private void HandleDeviceOrientationChange(NSNotification notification)
			{
				Hidden = ShouldHideOverlay();
			}

			private bool ShouldHideOverlay()
			{
				bool retval = false;

				switch (UIDevice.CurrentDevice.Orientation)
				{
					case UIDeviceOrientation.Portrait:
					case UIDeviceOrientation.PortraitUpsideDown:
						retval = false;
						break;
					case UIDeviceOrientation.LandscapeLeft:
						retval = true;
						break;
					case UIDeviceOrientation.LandscapeRight:
						retval = false;
						break;
				}

				return retval;
			}

			private void HandleUserCapturedItem(NSNotification notification)
			{
				Hidden = true;
				if (_deviceOrientationObserver != null)
				{
					NSNotificationCenter.DefaultCenter.RemoveObserver(_deviceOrientationObserver);
				}
			}

			private void HandleUserRejectedItem(NSNotification notification)
			{
				_deviceOrientationObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, HandleDeviceOrientationChange);
				Hidden = ShouldHideOverlay();
			}

			protected override void Dispose(bool disposing)
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(_captureItemObserver);
				NSNotificationCenter.DefaultCenter.RemoveObserver(_rejectItemObserver);
				if (_deviceOrientationObserver != null)
				{
					NSNotificationCenter.DefaultCenter.RemoveObserver(_deviceOrientationObserver);
				}
				UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
				base.Dispose(disposing);
			}
		}
	}
}

