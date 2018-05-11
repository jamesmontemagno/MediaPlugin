using UIKit;

namespace Plugin.Media
{
    internal class MediaPickerPopoverDelegate
        : UIPopoverControllerDelegate
    {
        internal MediaPickerPopoverDelegate(MediaPickerDelegate pickerDelegate, UIImagePickerController picker)
        {
            this.pickerDelegate = pickerDelegate;
            this.picker = picker;
        }

        public override bool ShouldDismiss(UIPopoverController popoverController) => true;

        public override void DidDismiss(UIPopoverController popoverController) =>
            pickerDelegate.Canceled(picker);

        private readonly MediaPickerDelegate pickerDelegate;
        private readonly UIImagePickerController picker;
    }
}

