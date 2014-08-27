// Copyright 2012 Charlie Kindel
//   
// This file is part of PremiseWP7
//   

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PremiseWebClient;
using PremiseWP.Annotations;

namespace PremiseWP.ViewModels {
    /// <summary>
    ///   The settings view for Connection Settings binds to this VM.
    ///   This VM talks directly to the Server instance held by MainViewModel.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged {
        /// <summary>
        ///   Initializes a new instance of the SettingsViewModel class.
        /// </summary>
        public SettingsViewModel() {
            Debug.WriteLine("SettingsViewModel()");
        }

     
        ////public override void Cleanup()
        ////{
        ////    // Clean own resources if needed

        ////    base.Cleanup();
        ////}
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        ///   The <see cref="Host" /> property's name.
        /// </summary>
        public const string HostPropertyName = "Host";

        /// <summary>
        ///   The <see cref="Port" /> property's name.
        /// </summary>
        public const string PortPropertyName = "Port";

        /// <summary>
        ///   The <see cref="SSL" /> property's name.
        /// </summary>
        public const string SslPropertyName = "SSL";

        /// <summary>
        ///   The <see cref="Username" /> property's name.
        /// </summary>
        public const string UsernamePropertyName = "Username";

        /// <summary>
        ///   The <see cref="Password" /> property's name.
        /// </summary>
        public const string PasswordPropertyName = "Password";



        /// <summary>
        ///   Gets the Host property.
        ///   TODO Update documentation:
        ///   Changes to that property's value raise the PropertyChanged event. 
        ///   This property's value is broadcasted by the Messenger's default instance when it changes.
        /// </summary>
        public string Host {
            get { return PremiseServer.Instance.Host; }

            set {
                if (PremiseServer.Instance.Host == value) {
                    return;
                }

                var oldValue = PremiseServer.Instance.Host;
                PremiseServer.Instance.Host = value;

                // Update bindings, no broadcast
                OnPropertyChanged(HostPropertyName);
                Debug.WriteLine("Host: {0}", value);
            }
        }

        /// <summary>
        ///   Gets the Port property.
        ///   TODO Update documentation:
        ///   Changes to that property's value raise the PropertyChanged event. 
        ///   This property's value is broadcasted by the Messenger's default instance when it changes.
        /// </summary>
        public int Port {
            get { return PremiseServer.Instance.Port; }

            set {
                if (PremiseServer.Instance.Port == value) {
                    return;
                }

                var oldValue = PremiseServer.Instance.Port;
                PremiseServer.Instance.Port = value;

                // Update bindings, no broadcast
                OnPropertyChanged(PortPropertyName);
                Debug.WriteLine("Port: {0}", value);
            }
        }

        /// <summary>
        ///   Gets the SSL property.
        ///   TODO Update documentation:
        ///   Changes to that property's value raise the PropertyChanged event. 
        ///   This property's value is broadcasted by the Messenger's default instance when it changes.
        /// </summary>
        public bool SSL {
            get { return PremiseServer.Instance.Ssl; }

            set {
                if (PremiseServer.Instance.Ssl == value) {
                    return;
                }

                var oldValue = PremiseServer.Instance.Ssl;
                PremiseServer.Instance.Ssl = value;

                // Update bindings, no broadcast
                OnPropertyChanged(SslPropertyName);
                Debug.WriteLine("SSL: {0}", value);
            }
        }

        /// <summary>
        ///   Gets the Username property.
        ///   TODO Update documentation:
        ///   Changes to that property's value raise the PropertyChanged event. 
        ///   This property's value is broadcasted by the Messenger's default instance when it changes.
        /// </summary>
        public string Username {
            get { return PremiseServer.Instance.Username; }

            set {
                if (PremiseServer.Instance.Username == value) {
                    return;
                }

                var oldValue = PremiseServer.Instance.Username;
                PremiseServer.Instance.Username = value;

                // Update bindings, no broadcast
                OnPropertyChanged(UsernamePropertyName);
                Debug.WriteLine("Username: {0}", value);
            }
        }

        /// <summary>
        ///   Gets the Password property.
        ///   TODO Update documentation:
        ///   Changes to that property's value raise the PropertyChanged event. 
        ///   This property's value is broadcasted by the Messenger's default instance when it changes.
        /// </summary>
        public string Password {
            get { return PremiseServer.Instance.Password; }

            set {
                if (PremiseServer.Instance.Password == value) {
                    return;
                }

                var oldValue = PremiseServer.Instance.Password;
                PremiseServer.Instance.Password = value;

                // Update bindings, no broadcast
                OnPropertyChanged(PasswordPropertyName);
                Debug.WriteLine("Password: {0}", value);
            }
        }
    }
}