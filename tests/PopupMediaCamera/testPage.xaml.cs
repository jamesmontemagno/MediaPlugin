using Xamarin.Forms;
using Plugin.Media;
using Plugin.Media.Abstractions;

namespace test
{
	public partial class testPage : ContentPage
	{
		public testPage()
		{
			InitializeComponent();
		}

		async void Handle_Clicked(object sender, System.EventArgs e)
		{
			var response = await DisplayActionSheet("Profile Photo Source", "Cancel", null, "Camera", "Photo Album", "Remove");

			if (response == "Camera")
			{

				if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsPickPhotoSupported)
				{
					await DisplayAlert("No Camera", ":( No camera available.", "Got It");
					return;
				}

				var mediaFile = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
				{
					AllowCropping = true
				});

				profilePicture.Source = ImageSource.FromStream(() => mediaFile.GetStream());

			

			}
			else if (response == "Photo Album")
			{
				var pickerOptions = new PickMediaOptions();

				var file = await CrossMedia.Current.PickPhotoAsync(pickerOptions);

				profilePicture.Source = ImageSource.FromStream(() => file.GetStream());

			}
			else if (response == "Remove")
			{
				
			}
		}
	}
}
