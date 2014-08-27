// Copyright 2012 Charlie Kindel
//   
// This file is part of PremiseWP7
//   

using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;
using Microsoft.Phone.Shell;

namespace PremiseWP {
    public class Settings {
        public Settings() {
            Host = "home.kindel.net";
            Port = 86;
            Ssl = false;
            Username = "tester";
            Password = "";
        }

        public String Host { get; set; }
        public int Port { get; set; }
        public bool Ssl { get; set; }

        public String Username { get; set; }
        public String Password { get; set; }

        private const string SettingsFileName = "Settings.dat";

        /// <summary>
        ///   Hydrate settings from backing store.
        /// </summary>
        /// <param name="fromIsoStore"> True: Use IsolatedStorage, False: Use PhoneApplicationState </param>
        public static Settings LoadSettings(bool fromIsoStore) {
            Settings connectionSettings = null;

            if (fromIsoStore) {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                    if (isf.FileExists(SettingsFileName)) {
                        using (IsolatedStorageFileStream fs = isf.OpenFile(SettingsFileName, FileMode.Open)) {
                            var ser = new XmlSerializer(typeof (Settings));
                            object obj = ser.Deserialize(fs);

                            if (obj != null)
                                connectionSettings = obj as Settings;
                        }
                    }
                }
            }
            else {
                if (PhoneApplicationService.Current.State.ContainsKey("UnsavedSettings")) {
                    connectionSettings =
                        PhoneApplicationService.Current.State["UnsavedSettings"] as Settings;
                    PhoneApplicationService.Current.State.Remove("UnsavedSettings");
                }
            }

            return connectionSettings ?? (new Settings());
        }

        /// <summary>
        ///   Save settings to backing store.
        /// </summary>
        /// <param name="fromIsoStore"> True: Use IsolatedStorage, False: Use PhoneApplicationState </param>
        /// <param name="connectionSettings"></param>
        public static void SaveSettings(bool fromIsoStore, Settings connectionSettings) {
            if (fromIsoStore) {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                    using (IsolatedStorageFileStream fs = isf.CreateFile(SettingsFileName)) {
                        var ser = new XmlSerializer(typeof (Settings));
                        ser.Serialize(fs, connectionSettings);
                    }
                }
            }
            else
                PhoneApplicationService.Current.State.Add("UnsavedSettings", connectionSettings);
        }
    }
}