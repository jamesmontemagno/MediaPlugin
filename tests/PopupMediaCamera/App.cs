using Xamarin.Forms;

namespace PopupMediaCamera
{
    public class App : Application
    {
        public App()
        {
            MainPage = new NavigationPage(new Page1());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
        ///// <summary>
        ///// The get navigation.
        ///// </summary>
        ///// <returns>
        ///// The <see cref="NavigationPage"/>.
        ///// </returns>
        public static NavigationPage GetNavigation()
        {
            var page = Current.MainPage as NavigationPage;
            return page;
        }
    }
}
