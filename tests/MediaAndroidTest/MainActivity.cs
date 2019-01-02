using Android.App;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Android.Content.PM;
using System;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System.Threading;

namespace MediaAndroidTest
{
    [Activity(Label = "MediaAndroidTest", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            StartActivity(typeof(MainActivity2));
			Finish();
        }
    }
    [Activity(Label = "MediaAndroidTest", Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity2 : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
			Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, bundle);

			// Get our button from the layout resource,
			// and attach an event to it
			var button = FindViewById<Button>(Resource.Id.MyButton);
            var image = FindViewById<ImageView>(Resource.Id.imageView1);

            var switchSize = FindViewById<Switch>(Resource.Id.switch_size);
			var switchSaveToAlbum = FindViewById<Switch>(Resource.Id.switch_save_album);
			var switchCamera = FindViewById<Switch>(Resource.Id.switch_front);
			var switchCancel = FindViewById<Switch>(Resource.Id.switch_cancel);

			button.Click += async delegate
            {
                try
                {

					var cts = new CancellationTokenSource();
					if (switchCancel.Checked)
					{
						cts.CancelAfter(TimeSpan.FromSeconds(10));
					}
                    var size = switchSize.Checked ? PhotoSize.Medium : PhotoSize.Full;
                    var media = new MediaImplementation();
					var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
					{
						Directory = "Sample",
						Name = $"{DateTime.Now}_{size}|\\?*<\":>/'.jpg".Replace(" ", string.Empty),
						SaveToAlbum = switchSaveToAlbum.Checked,
						PhotoSize = switchSize.Checked ? PhotoSize.Small : PhotoSize.Full,
						DefaultCamera = switchCamera.Checked ? CameraDevice.Front : CameraDevice.Rear
                    }, cts.Token);

					if (file == null)
                        return;
                    var path = file.Path;
                    Toast.MakeText(this, path, ToastLength.Long).Show();
                    System.Diagnostics.Debug.WriteLine(path);

                    var bitmap = BitmapFactory.DecodeFile(file.Path);
                    image.SetImageBitmap(bitmap);
                    file.Dispose();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                }
            };

            var pick = FindViewById<Button>(Resource.Id.button1);
            pick.Click += async (sender, args) =>
              {
                  try
                  {
					  var cts = new CancellationTokenSource();
					  if (switchCancel.Checked)
					  {
						  cts.CancelAfter(TimeSpan.FromSeconds(10));
					  }
					  var file = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions
					  {
                          PhotoSize = switchSize.Checked ? PhotoSize.Large : PhotoSize.Full
                      }, cts.Token);
                      if (file == null)
                          return;
                      var path = file.Path;
                      Toast.MakeText(this, path, ToastLength.Long).Show();
                      System.Diagnostics.Debug.WriteLine(path);
                      var bitmap = BitmapFactory.DecodeFile(file.Path);
                      image.SetImageBitmap(bitmap);
                      file.Dispose();
                  }
                  catch (Exception ex)
                  {
                      Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                  }
              };

            FindViewById<Button>(Resource.Id.button2).Click += async (sender, args) =>
              {
                  try
                  {
					  var cts = new CancellationTokenSource();
					  if (switchCancel.Checked)
					  {
						  cts.CancelAfter(TimeSpan.FromSeconds(10));
					  }
					  var size = switchSize.Checked ? VideoQuality.Low : VideoQuality.Medium;
					  var media = new MediaImplementation();

					  /*var options = new Plugin.Media.Abstractions.StoreVideoOptions
					  {
						  Directory = "Sample",
						  Name = $"{DateTime.UtcNow}_{size}|\\?*<\":>/'.mp4".Replace(" ", string.Empty),
						  SaveToAlbum = switchSaveToAlbum.Checked,
						  Quality = size,
						  DefaultCamera = switchCamera.Checked ? Plugin.Media.Abstractions.CameraDevice.Front : CameraDevice.Rear
					  };*/

					  var options = new StoreVideoOptions
					  {
						  Directory = "HCS",
						  Name = $"{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.mp4",
						  SaveToAlbum = true,
						  Quality = VideoQuality.Medium,
						  DesiredSize = 45 * 1000000,
						  CompressionQuality = 0,
					  }; 

					  var file = await CrossMedia.Current.TakeVideoAsync(options, cts.Token);
                      if (file == null)
                          return;
                      var path = file.Path;
                      System.Diagnostics.Debug.WriteLine(path);
                      Toast.MakeText(this, path, ToastLength.Long).Show();


                      file.Dispose();
                  }
                  catch (Exception ex)
                  {
                      Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
                  }
              };


            FindViewById<Button>(Resource.Id.button3).Click += async (sender, args) =>
            {
				var cts = new CancellationTokenSource();
				if (switchCancel.Checked)
				{
					cts.CancelAfter(TimeSpan.FromSeconds(10));
				}
				var media = new MediaImplementation();
                var file = await CrossMedia.Current.PickVideoAsync(cts.Token);
                if (file == null)
                    return;

                var path = file.Path;
                Toast.MakeText(this, path, ToastLength.Long).Show();
                System.Diagnostics.Debug.WriteLine(path);

                file.Dispose();
            };

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

