using Plugin.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MediaTest.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ButtonTakePhoto_Click(object sender, RoutedEventArgs e)
        {
			var cts = new CancellationTokenSource();
			if (ToggleCancel.IsOn)
			{
				cts.CancelAfter(TimeSpan.FromSeconds(10));
			}
			var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
            {
                Directory = "Sample",
                Name = "test.jpg",
                SaveToAlbum = ToggleSaveToAlbum.IsOn,
                AllowCropping = ToggleCrop.IsOn,
                PhotoSize = ToggleResize.IsOn ? Plugin.Media.Abstractions.PhotoSize.Large : Plugin.Media.Abstractions.PhotoSize.Full,
                CompressionQuality = (int)SliderQuality.Value
            }, cts.Token);
            if (file == null)
                return;
            var path = file.Path;
            System.Diagnostics.Debug.WriteLine(path);

            var dialog = new MessageDialog(path);
            await dialog.ShowAsync();

            Photo.Source = new BitmapImage(new Uri(path));

            file.Dispose();
        }

        private async void ButtonPickPhoto_Click(object sender, RoutedEventArgs e)
        {
			var cts = new CancellationTokenSource();
			if (ToggleCancel.IsOn)
			{
				cts.CancelAfter(TimeSpan.FromSeconds(10));
			}
			var file = await CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions
            {
                PhotoSize = ToggleResize.IsOn ? Plugin.Media.Abstractions.PhotoSize.Medium : Plugin.Media.Abstractions.PhotoSize.Full,
                CompressionQuality = (int)SliderQuality.Value
            }, cts.Token);
            if (file == null)
                return;
            var path = file.Path;
            System.Diagnostics.Debug.WriteLine(path);
            var dialog = new MessageDialog(path);
            await dialog.ShowAsync();

            Photo.Source = new BitmapImage(new Uri(path));

            file.Dispose();
        }

       

        private async void ButtonTakeVIdeo_Click(object sender, RoutedEventArgs e)
        {
			var cts = new CancellationTokenSource();
			if (ToggleCancel.IsOn)
			{
				cts.CancelAfter(TimeSpan.FromSeconds(10));
			}
			var file = await CrossMedia.Current.TakeVideoAsync(new Plugin.Media.Abstractions.StoreVideoOptions
            {
                Directory = "Sample",
                Name = "test.mp4",
                SaveToAlbum = ToggleSaveToAlbum.IsOn,
                AllowCropping = ToggleCrop.IsOn,
                PhotoSize = ToggleResize.IsOn ? Plugin.Media.Abstractions.PhotoSize.Large : Plugin.Media.Abstractions.PhotoSize.Full,
                CompressionQuality = (int)SliderQuality.Value,
                Quality = ToggleResize.IsOn ? Plugin.Media.Abstractions.VideoQuality.High : Plugin.Media.Abstractions.VideoQuality.Low
            }, cts.Token);
            if (file == null)
                return;
            var path = file.Path;
            System.Diagnostics.Debug.WriteLine(path);

            var dialog = new MessageDialog(path);
            await dialog.ShowAsync();

            file.Dispose();
        }

        private async void ButtonPickVideo_Click(object sender, RoutedEventArgs e)
        {
			var cts = new CancellationTokenSource();
			if (ToggleCancel.IsOn)
			{
				cts.CancelAfter(TimeSpan.FromSeconds(10));
			}
			var file = await CrossMedia.Current.PickVideoAsync(cts.Token);
            if (file == null)
                return;
            var path = file.Path;
            System.Diagnostics.Debug.WriteLine(path);
            var dialog = new MessageDialog(path);
            await dialog.ShowAsync();

            //Photo.Source = 

            file.Dispose();
        }
    }
}
