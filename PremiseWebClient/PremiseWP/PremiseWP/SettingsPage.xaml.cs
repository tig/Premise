// Copyright 2012 Charlie Kindel
//   
// This file is part of PremiseWP7
//   

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Controls;
using PremiseWebClient;

namespace PremiseWP {
    public partial class SettingsPage : PhoneApplicationPage {
        public SettingsPage() {
            InitializeComponent();

            DataContext = App.SettingsViewModel;
        }

        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e) {
        }

        private void PhoneApplicationPage_LostFocus(object sender, RoutedEventArgs e) {
        }

        private async void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e) {
            Debug.WriteLine("BackKeyPress");
            var settings = new Settings {
                Host = PremiseServer.Instance.Host, Password = PremiseServer.Instance.Password,
                Port = PremiseServer.Instance.Port, Ssl = PremiseServer.Instance.Ssl,
                Username = PremiseServer.Instance.Username
            };
            Settings.SaveSettings(true, settings);

            PremiseServer.Instance.StopSubscriptions();
        }
    }
}