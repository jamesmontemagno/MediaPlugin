using System;
using Rg.Plugins.Popup.Extensions;
using Rg.Plugins.Popup.Pages;

namespace PopupMediaCamera
{
    public partial class Page1
    {
        public Page1()
        {
            InitializeComponent();
        }

        private void TapGestureRecognizer_OpenPopupPageTapped(object sender, EventArgs e)
        {
            Navigation.PushPopupAsync(new PagePopup());
        }
    }
}
