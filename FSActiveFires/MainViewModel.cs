﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FSActiveFires {
    class MainViewModel : NotifyPropertyChanged {
        private SimConnectInstance sc = null;
        private MODISHotspots activeFires;
        private Log log;

        public MainViewModel() {
            log = Log.Instance;
            log.WriteLine(string.Format("FS Active Fires by Orion Lyau\r\nVersion: {0}\r\n", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version));
            activeFires = new MODISHotspots();
            SelectedDatasetUrl = activeFires.datasets["World"];

            sc = new SimConnectInstance();
            sc.PropertyChanged += (sender, args) => base.OnPropertyChanged(args.PropertyName);
        }

        #region Command Bindings

        private ICommand _installCommand;
        public ICommand InstallCommand {
            get {
                if (_installCommand == null) {
                    _installCommand = new RelayCommandAsync(async _ => {
#if !DEBUG
                        try {
#endif
                            log.Info("InstallCommand");
                            await Task.Run(() => {
                                FireEffect.InstallSimObject();
                            });
                            SimObjectTitle = "Fire_Effect";
#if !DEBUG
                        }
                        catch (Exception ex) {
                            string message = string.Format("Type: {0}\r\nMessage: {1}\r\nStack trace:\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
                            log.Error(message);
                            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
#endif
                    });
                }
                return _installCommand;
            }
        }

        private ICommand _downloadCommand;
        public ICommand DownloadCommand {
            get {
                if (_downloadCommand == null) {
                    _downloadCommand = new RelayCommandAsync(async _ => {
#if !DEBUG
                        try {
#endif
                            log.Info("DownloadCommand");
                            await Task.Run(() => {
                                activeFires.LoadData(SelectedDatasetUrl);
                            });
                            OnPropertyChanged("TotalFiresCount");
#if !DEBUG
                        }
                        catch (Exception ex) {
                            string message = string.Format("Type: {0}\r\nMessage: {1}\r\nStack trace:\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
                            log.Error(message);
                            System.Windows.MessageBox.Show(message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        }
#endif
                    });
                }
                return _downloadCommand;
            }
        }

        private ICommand _connectCommand;
        public ICommand ConnectCommand {
            get {
                if (_connectCommand == null) {
                    _connectCommand = new RelayCommandAsync(async _ => {
                        log.Info("ConnectCommand");
                        if (!IsConnected) {
                            log.Info(string.Format("Minimum detection confidence: {0}%", MinimumConfidence));
                            await Task.Run(() => {
                                sc.AddLocations(SimObjectTitle, activeFires.hotspots.Where(x => x.Confidence >= MinimumConfidence));
                                sc.Connect();
                            });
                        }
                        else {
                            await Task.Run(() => {
                                sc.Disconnect();
                            });
                        }
                    });
                }
                return _connectCommand;
            }
        }

        private ICommand _relocateUserCommand;
        public ICommand RelocateUserCommand {
            get {
                if (_relocateUserCommand == null) {
                    _relocateUserCommand = new RelayCommandAsync(async _ => {
                        log.Info("RelocateUserCommand");
                        await Task.Run(() => {
                            sc.RelocateUserRandomly();
                        });
                    });
                }
                return _relocateUserCommand;
            }
        }

        private ICommand _nasaCommand;
        public ICommand NASACommand {
            get {
                if (_nasaCommand == null) {
                    _nasaCommand = new RelayCommand(param => {
                        log.Info("NASACommand");
                        System.Diagnostics.Process.Start("https://earthdata.nasa.gov/firms");
                    });
                }
                return _nasaCommand;
            }
        }

        private ICommand _donateCommand;
        public ICommand DonateCommand {
            get {
                if (_donateCommand == null) {
                    _donateCommand = new RelayCommand(param => {
                        log.Info("DonateCommand");
                        System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=orion%2epublic%40live%2ecom&lc=US&item_name=FS%20Active%20Fires%20Donation&currency_code=USD");
                    });
                }
                return _donateCommand;
            }
        }

        private ICommand _closingCommand;
        public ICommand ClosingCommand {
            get {
                if (_closingCommand == null) {
                    _closingCommand = new RelayCommand(param => {
#if !DEBUG
                        try {
#endif
                            activeFires.RemoveTemporaryDirectory();
#if !DEBUG
                        }
                        catch (Exception ex) {
                            log.Warning(string.Format("Unable to remove temporary directory.\r\nType: {0}\r\nMessage: {1}\r\nStack trace:\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace));
                        }
#endif
                        if (IsConnected) {
                            sc.Disconnect();
                        }
                        log.ConditionalSave();
                    });
                }
                return _closingCommand;
            }
        }

        #endregion

        #region Data Binding

        public bool IsConnected { get { return sc.IsConnected; } }
        public int CreatedSimObjectsCount { get { return sc.CreatedSimObjectsCount; } }
        public Dictionary<string, string> Datasets { get { return activeFires.datasets; } }
        public int TotalFiresCount { get { return activeFires.hotspots.Count; } }

        private string _selectedDatasetUrl;
        public string SelectedDatasetUrl {
            get { return _selectedDatasetUrl; }
            set { SetProperty(ref _selectedDatasetUrl, value); }
        }

        private string _simObjectTitle = "Fire_Effect"; // "Food_pallet"
        public string SimObjectTitle {
            get { return _simObjectTitle; }
            set { SetProperty(ref _simObjectTitle, value); }
        }

        private int _minimumConfidence;
        public int MinimumConfidence {
            get { return _minimumConfidence; }
            set { SetProperty(ref _minimumConfidence, value); }
        }

        #endregion
    }
}
