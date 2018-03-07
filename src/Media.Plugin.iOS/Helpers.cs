using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

namespace Plugin.Media
{
	public static class Helpers
	{
		public static UIViewController GetCurrentUIController()
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			var vc = window.RootViewController;
			while (vc.PresentedViewController != null)
			{
				vc = vc.PresentedViewController;
			}

			var navController = vc as UINavigationController;
			if (navController != null)
			{
				vc = navController.ViewControllers.Last();
			}

			return vc;
		}
	}
}