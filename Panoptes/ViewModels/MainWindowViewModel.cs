﻿using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Sessions;
using Panoptes.ViewModels.Charts;
using Panoptes.ViewModels.Panels;
using System;
using System.Threading.Tasks;

namespace Panoptes.ViewModels
{
    public sealed class MainWindowViewModel : ObservableRecipient
    {
        private readonly ISessionService _sessionService;
        //private readonly ILayoutManager _layoutManager;

        private DispatcherTimer _timer = new DispatcherTimer();

        public RelayCommand ExitCommand { get; }
        public RelayCommand OpenSessionCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand ExportCommand { get; }

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }

        /*
        public RelayCommand<DockingManager> SaveLayoutCommand { get; }
        public RelayCommand<DockingManager> ResetLayoutCommand { get; }
        public RelayCommand<DockingManager> RestoreLayoutCommand { get; }
        */

        public LogPanelViewModel LogPane { get; }
        public StatisticsPanelViewModel StatisticsPane { get; }
        public RuntimeStatisticsPanelViewModel RuntimeStatisticsPane { get; }
        public TradesPanelViewModel TradesPane { get; }
        public HoldingsPanelViewModel HoldingsPane { get; }
        public CashBookPanelViewModel CashBookPane { get; }
        public ProfitLossPanelViewModel ProfitLossPane { get; }
        public OxyPlotSelectionViewModel OxyPlotSelectionPane { get; }

        public bool IsSessionActive => _sessionService.IsSessionActive;

        private SessionState _sessionState = SessionState.Unsubscribed;
        public SessionState SessionState
        {
            get { return _sessionState; }
            set
            {
                _sessionState = value;
                OnPropertyChanged();
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public StatusViewModel StatusViewModel { get; }

        public MainWindowViewModel(ISessionService resultService, IMessenger messenger, StatusViewModel statusViewModel,
            LogPanelViewModel logPanelViewModel, StatisticsPanelViewModel statisticsPanelViewModel,
            RuntimeStatisticsPanelViewModel runtimeStatisticsPanelViewModel, ProfitLossPanelViewModel profitLossPanelViewModel,
            TradesPanelViewModel tradesPanelViewModel, HoldingsPanelViewModel holdingsPane, CashBookPanelViewModel cashBookPane,
            OxyPlotSelectionViewModel oxyPlotSelectionViewModel)
            : base(messenger)
        {
            StatusViewModel = statusViewModel;
            _sessionService = resultService;

            //_layoutManager = layoutManager;

            LogPane = logPanelViewModel;
            StatisticsPane = statisticsPanelViewModel;
            RuntimeStatisticsPane = runtimeStatisticsPanelViewModel;
            ProfitLossPane = profitLossPanelViewModel;
            TradesPane = tradesPanelViewModel;
            HoldingsPane = holdingsPane;
            CashBookPane = cashBookPane;
            OxyPlotSelectionPane = oxyPlotSelectionViewModel;

            Title = $"Panoptes - LEAN Algorithm Monitor - {GetVersion()}";

#if DEBUG
            Title = "[DEBUG] " + Title;
#endif

            ExitCommand = new RelayCommand(() =>
            {
                if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow.Close();
                }
            });

            CloseCommand = new RelayCommand(() => _sessionService.ShutdownSession(), () => IsSessionActive);
            OpenSessionCommand = new RelayCommand(() => Messenger.Send(new ShowNewSessionWindowMessage()));
            ExportCommand = new RelayCommand(Export, () => IsSessionActive && false); // deactivate for the moment
            ConnectCommand = new RelayCommand(() => _sessionService.IsSessionSubscribed = true, () => _sessionState != SessionState.Subscribed && _sessionService.CanSubscribe);
            DisconnectCommand = new RelayCommand(() => _sessionService.IsSessionSubscribed = false, () => _sessionState != SessionState.Unsubscribed);

            /*
            SaveLayoutCommand = new RelayCommand<DockingManager>(manager => _layoutManager.SaveLayout(manager));
            RestoreLayoutCommand = new RelayCommand<DockingManager>(manager => _layoutManager.LoadLayout(manager));
            ResetLayoutCommand = new RelayCommand<DockingManager>(manager => _layoutManager.ResetLayout(manager));
            */

            Messenger.Register<MainWindowViewModel, SessionOpenedMessage>(this, async (r, _) => await r.InvalidateCommands().ConfigureAwait(false));
            Messenger.Register<MainWindowViewModel, SessionClosedMessage>(this, async (r, _) =>
            {
                r.SessionState = SessionState.Unsubscribed;
                //r.Documents.Clear();
                await r.InvalidateCommands().ConfigureAwait(false);
            });

            Messenger.Register<MainWindowViewModel, SessionStateChangedMessage>(this, async (r, m) =>
            {
                r.SessionState = m.State;
                await r.InvalidateCommands().ConfigureAwait(false);
            });

            Messenger.Register<MainWindowViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                /*
                try
                {
                    lock (_documents)
                    {
                        r.ParseResult(m.ResultContext.Result);
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
                */
            });

            Messenger.Register<MainWindowViewModel, GridRequestMessage>(this, (r, m) =>
            {
                /*
                var chartTableViewModel = new GridPanelViewModel
                {
                    Key = m.Key
                };

                // Calcualte the index for this tab
                var index = Documents.IndexOf(Documents.First(c => c.Key == m.Key));
                r.Documents.Insert(index, chartTableViewModel);

                chartTableViewModel.IsSelected = true;

                // Get the latest data for this tab and inject it
                var chart = _sessionService.LastResult.Charts[m.Key];
                chartTableViewModel.ParseChart(chart);
                */
            });

            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += (s, e) => CurrentDateTimeUtc = DateTime.UtcNow;
            _timer.Start();
        }

        public void Initialize()
        {
            _sessionService.Initialize();
        }

        private DateTime _currentDateTimeUtc;
        public DateTime CurrentDateTimeUtc
        {
            get
            {
                return _currentDateTimeUtc;
            }

            private set
            {
                if (_currentDateTimeUtc == value) return;
                _currentDateTimeUtc = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentDateTimeLocal));
            }
        }

        public DateTime CurrentDateTimeLocal
        {
            get
            {
                return _currentDateTimeUtc.ToLocalTime();
            }
        }

        public void HandleDroppedFileName(string fileName)
        {
            /*
            _sessionService.OpenFile(new FileSessionParameters
            {
                FileName = fileName,
                Watch = true
            });
            */
        }

        internal void ShutdownSession()
        {
            _sessionService.ShutdownSession();
        }

        private void Export()
        {
            /*
            var exportDialog = new SaveFileDialog
            {
                FileName = DateTime.Now.ToString("yyyyMMddHHmm") + "_export",
                DefaultExt = ".json",
                Filter = "Json documents (.json)|*.json"
            };

            var dialogResult = exportDialog.ShowDialog();
            if (dialogResult != true) return;

            var serializer = new ResultSerializer(new ResultConverter());
            var serialized = serializer.Serialize(_sessionService.LastResult);
            File.WriteAllText(exportDialog.FileName, serialized);
            */
        }

        private Task InvalidateCommands()
        {
            OnPropertyChanged(nameof(IsSessionActive));

            // This need to be called from the UI thread,
            // this is not the case anymore since Session open async.
            return Dispatcher.UIThread.InvokeAsync(() =>
            {
                CloseCommand.NotifyCanExecuteChanged();
                ExportCommand.NotifyCanExecuteChanged();
                ConnectCommand.NotifyCanExecuteChanged();
                DisconnectCommand.NotifyCanExecuteChanged();
            });
        }

        private static string GetVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
#pragma warning disable CS8603 // Possible null reference return.
            if (fvi != null)
            {
                if (fvi.FileVersion == fvi.ProductVersion)
                {
                    return fvi.FileVersion;
                }
                else
                {
                    return $"{fvi.FileVersion} ({fvi.ProductVersion})";
                }
            }
            return null;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}
