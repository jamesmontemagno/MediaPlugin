using FFImageLoading.Forms;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Media.Plugin.Sample
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MediaPage : ContentPage
	{
		ObservableCollection<MediaFile> files = new ObservableCollection<MediaFile>();
		public MediaPage()
		{
			InitializeComponent();

			files.CollectionChanged += Files_CollectionChanged;


			takePhoto.Clicked += async (sender, args) =>
			{
				await CrossMedia.Current.Initialize();
				files.Clear();
				if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
				{
					await DisplayAlert("No Camera", ":( No camera avaialble.", "OK");
					return;
				}

				var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
				{
					PhotoSize = PhotoSize.Medium,
					Directory = "Sample",
					Name = "test.jpg"
				});

				if (file == null)
					return;

				await DisplayAlert("File Location", file.Path, "OK");

				files.Add(file);
			};

			pickPhoto.Clicked += async (sender, args) =>
			{
				await CrossMedia.Current.Initialize();
				files.Clear();
				if (!CrossMedia.Current.IsPickPhotoSupported)
				{
					await DisplayAlert("Photos Not Supported", ":( Permission not granted to photos.", "OK");
					return;
				}
				var file = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions
				{
					PhotoSize = PhotoSize.Full,
					SaveMetaData = true
				});


				if (file == null)
					return;

				files.Add(file);
			};

			pickPhotos.Clicked += async (sender, args) =>
			{
				await CrossMedia.Current.Initialize();
				files.Clear();
				if (!CrossMedia.Current.IsPickPhotoSupported)
				{
					await DisplayAlert("Photos Not Supported", ":( Permission not granted to photos.", "OK");
					return;
				}
				var picked = await CrossMedia.Current.PickPhotosAsync();


				if (picked == null)
					return;
				foreach (var file in picked)
					files.Add(file);
				
			};

			takeVideo.Clicked += async (sender, args) =>
			{
				await CrossMedia.Current.Initialize();
				files.Clear();
				if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakeVideoSupported)
				{
					await DisplayAlert("No Camera", ":( No camera avaialble.", "OK");
					return;
				}

				var file = await CrossMedia.Current.TakeVideoAsync(new StoreVideoOptions
				{
					Name = "video.mp4",
					Directory = "DefaultVideos"
				});

				if (file == null)
					return;

				await DisplayAlert("Video Recorded", "Location: " + file.Path, "OK");

				file.Dispose();
			};

			pickVideo.Clicked += async (sender, args) =>
			{
				await CrossMedia.Current.Initialize();
				files.Clear();
				if (!CrossMedia.Current.IsPickVideoSupported)
				{
					await DisplayAlert("Videos Not Supported", ":( Permission not granted to videos.", "OK");
					return;
				}
				var file = await CrossMedia.Current.PickVideoAsync();

				if (file == null)
					return;

				await DisplayAlert("Video Selected", "Location: " + file.Path, "OK");
				file.Dispose();
			};
		}

		private void Files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if(files.Count == 0)
			{
				ImageList.Children.Clear();
				return;
			}
			if (e.NewItems.Count == 0)
				return;

			var file = e.NewItems[0] as MediaFile;
			var image = new Image { WidthRequest = 300, HeightRequest = 300, Aspect = Aspect.AspectFit };
			image.Source = ImageSource.FromFile(file.Path);
			/*image.Source = ImageSource.FromStream(() =>
			{
				var stream = file.GetStream();
				return stream;
			});*/
			ImageList.Children.Add(image);

			var image2 = new CachedImage { WidthRequest = 300, HeightRequest = 300, Aspect = Aspect.AspectFit };
			image2.Source = ImageSource.FromFile(file.Path);
			ImageList.Children.Add(image2);
		}

		private async void Button_Clicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new ContentPage());
		}
	}
}