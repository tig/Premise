using System;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PremiseWebClient;

namespace PremiseWP {
    public partial class GarageDoors  {
        public GarageDoors() {
            InitializeComponent();

            // Set the data context of the LongListSelector control to the sample data
            DataContext = App.GarageDoorsViewModel;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        // Load data for the ViewModel Items
        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            if (!PremiseServer.Instance.Connected) {
                await PremiseServer.Instance.StartSubscriptionsAsync(new StreamSocketPremiseSocket());
            } 
            App.GarageDoorsViewModel.LoadData();
        }

        private void PinToStart(object sender, EventArgs e) {
            var newTile = new StandardTileData {
                Title = "Garage Doors",
                BackgroundImage = new Uri("/Assets/Tiles/garage_icon_173.png", UriKind.Relative),
            };

            ShellTile.Create(new Uri("/GarageDoors.xaml?state=From a Live Tile", UriKind.Relative), newTile);
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