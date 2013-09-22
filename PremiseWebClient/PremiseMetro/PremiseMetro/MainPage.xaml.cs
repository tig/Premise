using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PremiseLib;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PremiseMetro
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

    

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Quit_Click(object sender, RoutedEventArgs e) {

            App.Current.Exit();
        }

        private async void Test_Click(object sender, RoutedEventArgs e) {
            try {
                StreamSocket socket = new StreamSocket();
                await socket.ConnectAsync(new HostName("192.168.0.2"), "86", SocketProtectionLevel.PlainSocket);
                var writer = new DataWriter(socket.OutputStream);
                uint i = writer.WriteString("test\r\n");
                try {
                    await writer.StoreAsync();
                    Debug.WriteLine("StoreAsync was called");
                    Debug.WriteLine("MainPage Wrote {0} bytes", i);
                    await writer.FlushAsync();
                } catch (Exception ex1) {
                    Debug.WriteLine(ex1.ToString());
                }
            } catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
