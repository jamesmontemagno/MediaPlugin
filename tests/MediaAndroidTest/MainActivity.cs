using Android.App;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Android.Content.PM;
using System;
using Plugin.Media;
using Plugin.Media.Abstractions;

namespace MediaAndroidTest
{
    [Activity(Label = "MediaAndroidTest", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            StartActivity(typeof(MainActivity2));
			Finish();
        }
    }
    [Activity(Label = "MediaAndroidTest", Icon = "@drawable/icon", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class MainActivity2 : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.MyButton);
            var image = FindViewById<ImageView>(Resource.Id.imageView1);

            var switchSize = FindViewById<Switch>(Resource.Id.switch_size);
			var switchSaveToAlbum = FindViewById<Switch>(Resource.Id.switch_save_album);
			var switchCamera = FindViewById<Switch>(Resource.Id.switch_front);

			button.Click += async delegate
            {
                try
                {
                    var size = switchSize.Checked ? Plugin.Media.Abstractions.PhotoSize.Medium : Plugin.Media.Abstractions.PhotoSize.Full;
                    var media = new Plugin.Media.MediaImplementation();
					var file = await Plugin.Media.CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
					{
						Directory = "Sample",
						Name = $"{DateTime.Now}_{size}|\\?*<\":>/'.jpg".Replace(" ", string.Empty),
						SaveToAlbum = switchSaveToAlbum.Checked,
						PhotoSize = switchSize.Checked ? Plugin.Media.Abstractions.PhotoSize.Small : Plugin.Media.Abstractions.PhotoSize.Full,
						DefaultCamera = switchCamera.Checked ? Plugin.Media.Abstractions.CameraDevice.Front : CameraDevice.Rear
                    });

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
                      var file = await Plugin.Media.CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions
                      {
                          PhotoSize = switchSize.Checked ? Plugin.Media.Abstractions.PhotoSize.Large : Plugin.Media.Abstractions.PhotoSize.Full
                      });
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
					  var size = switchSize.Checked ? Plugin.Media.Abstractions.VideoQuality.Low : Plugin.Media.Abstractions.VideoQuality.Medium;
					  var media = new Plugin.Media.MediaImplementation();

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

					  var file = await Plugin.Media.CrossMedia.Current.TakeVideoAsync(options);
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
                var media = new Plugin.Media.MediaImplementation();
                var file = await Plugin.Media.CrossMedia.Current.PickVideoAsync();
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

