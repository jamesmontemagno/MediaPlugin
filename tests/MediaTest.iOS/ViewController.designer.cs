// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace MediaTest.iOS
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch AlbumSwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch CroppingSwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch FrontSwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIImageView MainImage { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch OverlaySwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton PickPhoto { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton PickVideo { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch SizeSwitch { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISlider SliderQuality { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch SwitchCancel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch SwitchRotate { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton TakePhoto { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton TakeVideo { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (AlbumSwitch != null) {
                AlbumSwitch.Dispose ();
                AlbumSwitch = null;
            }

            if (CroppingSwitch != null) {
                CroppingSwitch.Dispose ();
                CroppingSwitch = null;
            }

            if (FrontSwitch != null) {
                FrontSwitch.Dispose ();
                FrontSwitch = null;
            }

            if (MainImage != null) {
                MainImage.Dispose ();
                MainImage = null;
            }

            if (OverlaySwitch != null) {
                OverlaySwitch.Dispose ();
                OverlaySwitch = null;
            }

            if (PickPhoto != null) {
                PickPhoto.Dispose ();
                PickPhoto = null;
            }

            if (PickVideo != null) {
                PickVideo.Dispose ();
                PickVideo = null;
            }

            if (SizeSwitch != null) {
                SizeSwitch.Dispose ();
                SizeSwitch = null;
            }

            if (SliderQuality != null) {
                SliderQuality.Dispose ();
                SliderQuality = null;
            }

            if (SwitchCancel != null) {
                SwitchCancel.Dispose ();
                SwitchCancel = null;
            }

            if (SwitchRotate != null) {
                SwitchRotate.Dispose ();
                SwitchRotate = null;
            }

            if (TakePhoto != null) {
                TakePhoto.Dispose ();
                TakePhoto = null;
            }

            if (TakeVideo != null) {
                TakeVideo.Dispose ();
                TakeVideo = null;
            }
        }
    }
}