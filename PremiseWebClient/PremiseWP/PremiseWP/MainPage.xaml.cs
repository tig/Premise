﻿using System;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using PremiseWebClient;

namespace PremiseWP {
    public partial class MainPage : PhoneApplicationPage {
        // Constructor
        public MainPage() {
            InitializeComponent();

            // Set the data context of the LongListSelector control to the sample data
            DataContext = App.MainViewModel;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        // Load data for the ViewModel Items
        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            if (!PremiseServer.Instance.Connected) {
                await PremiseServer.Instance.StartSubscriptionsAsync(new StreamSocketPremiseSocket());
            }
            App.MainViewModel.LoadData();
        }
        private void Settings_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void Refresh_Click(object sender, EventArgs e) {
            App.MainViewModel.LoadData();
        }

        private void Disconnect_Click(object sender, EventArgs e) {
            App.MainViewModel.Disconnect();
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}