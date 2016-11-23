using System;
using System.Diagnostics;
using Plugin.Media;
using Plugin.Media.Abstractions;

namespace PopupMediaCamera
{
    public partial class PagePopup
    {
        public PagePopup()
        {
            InitializeComponent();
        }

        private async void TapGestureRecognizer_StartCameraTapped(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                Debug.WriteLine("No Camera", "No camera avaialble.", "OK");
                return;
            }
            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                Directory = "InventoryManagement",
                Name = "item.jpg",
                PhotoSize = PhotoSize.Medium,
                CompressionQuality = 42,
                SaveToAlbum = false,
                DefaultCamera = CameraDevice.Rear,

            });
            if (file == null)
                return;
        }
    }
}
