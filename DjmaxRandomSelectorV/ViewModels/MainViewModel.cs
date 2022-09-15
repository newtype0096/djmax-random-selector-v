﻿using Caliburn.Micro;
using DjmaxRandomSelectorV.Models;
using DjmaxRandomSelectorV.Utilities;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using DjmaxRandomSelectorV.RandomSelector;
using System.Threading;

namespace DjmaxRandomSelectorV.ViewModels
{
    public class MainViewModel : Conductor<object>, IHandle<SelectorOption>
    {
        private const int ApplicationVersion = 150;
        private const string ReleasesUrl = "https://github.com/wowvv0w/djmax-random-selector-v/releases";
        private const string VersionsUrl = "https://raw.githubusercontent.com/wowvv0w/djmax-random-selector-v/main/DjmaxRandomSelectorV/Version.txt";
        private const string AllTrackListUrl = "https://raw.githubusercontent.com/wowvv0w/djmax-random-selector-v/main/DjmaxRandomSelectorV/Data/AllTrackList.csv";
        private const string ConfigPath = "Data/Config.json";

        private FilterBaseViewModel _filterViewModel;
        public FilterBaseViewModel FilterViewModel
        {
            get { return _filterViewModel; }
            set
            {
                _filterViewModel = value;
                NotifyOfPropertyChange(() => FilterViewModel);
            }
        }
        public HistoryViewModel HistoryViewModel { get; set; }
        public FilterOptionViewModel FilterOptionViewModel { get; set; }
        public FilterOptionIndicatorViewModel FilterOptionIndicatorViewModel { get; set; }

        private InfoViewModel _infoViewModel;

        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowManager _windowManager;
        private readonly Configuration _configuration;
        private readonly Selector _selector;
        public MainViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.SubscribeOnUIThread(this);
            _windowManager = windowManager;

            _configuration = FileManager.Import<Configuration>(ConfigPath);
            _selector = new Selector(_eventAggregator, _configuration);

            CheckUpdates();
            ChangeFilterView(_configuration.SelectorOption.FilterType);
            HistoryViewModel = new HistoryViewModel(_eventAggregator);
            FilterOptionIndicatorViewModel = new FilterOptionIndicatorViewModel(_eventAggregator);
            FilterOptionViewModel = new FilterOptionViewModel(_eventAggregator, _configuration.FilterOption);
        }

        private void CheckUpdates()
        {
            try
            {
                using var client = new HttpClient();
                string result = client.GetStringAsync(VersionsUrl).Result;
                string[] versions = result.Split(',');
                var lastestAppVersion = int.Parse(versions[0]);
                var lastestTrackVersion = int.Parse(versions[1]);

                bool hasAppUpdate = lastestAppVersion > ApplicationVersion;
                OpenReleasePageVisibility = hasAppUpdate ? Visibility.Visible : Visibility.Hidden;
                bool hasTrackUpdate = lastestTrackVersion != _configuration.AllTrackVersion;
                if (hasTrackUpdate || !File.Exists("Data/AllTrackList.csv"))
                {
                    FileManager.DownloadAllTrackList();
                    _configuration.AllTrackVersion = lastestTrackVersion;
                    FileManager.Export(_configuration, ConfigPath);
                    MessageBox.Show($"All track list is updated to the version {_configuration.AllTrackVersion}.",
                        "DJMAX Random Selector V", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                _infoViewModel = new InfoViewModel(ApplicationVersion, lastestAppVersion,
                    _configuration.AllTrackVersion);
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Cannot check available updates. Check the internet connection.",
                    "Selector Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                _infoViewModel = new InfoViewModel(ApplicationVersion, ApplicationVersion,
                    _configuration.AllTrackVersion);
            }
        }

        public Task HandleAsync(SelectorOption message, CancellationToken cancellationToken)
        {
            ChangeFilterView(message.FilterType);
            return Task.CompletedTask;
        }

        private void ChangeFilterView(string filterType)
        {
            FilterViewModel?.ExportFilter();
            FilterViewModel = filterType switch
            {
                nameof(ConditionalFilter) => new ConditionalFilterViewModel(_eventAggregator, _windowManager, _configuration.Favorites),
                nameof(SelectiveFilter) => new SelectiveFilterViewModel(_eventAggregator),
                _ => throw new NotSupportedException(),
            };
        }

        protected override void OnViewLoaded(object view)
        {
            var window = view as Window;
            _selector.AddHotKey();
            SetPosition(window);
        }

        public void SetPosition(Window window)
        {
            if (_configuration.Position?.Length == 2)
            {
                window.Top = _configuration.Position[0];
                window.Left = _configuration.Position[1];
            }
        }

        #region Window Top Bar
        private Visibility _openReleasePageVisibility;
        public Visibility OpenReleasePageVisibility
        {
            get { return _openReleasePageVisibility; }
            set
            {
                _openReleasePageVisibility = value;
                NotifyOfPropertyChange(() => OpenReleasePageVisibility);
            }
        }
        public void OpenReleasePage()
        {
            System.Diagnostics.Process.Start(ReleasesUrl);
        }
        public void MoveWindow(object view)
        {
            var window = view as Window;
            window.DragMove();
        }
        public void MinimizeWindow(object view)
        {
            var window = view as Window;
            window.WindowState = WindowState.Minimized;
        }
        public void CloseWindow(object view)
        {
            var window = view as Window;
            SaveConfiguration(window);
            window.Close();
        }
        private void SaveConfiguration(Window window)
        {
            FilterViewModel.ExportFilter();
            _configuration.Position = new double[2] { window.Top, window.Left };
            FileManager.Export(_configuration, ConfigPath);
        }
        #endregion

        #region Tab Bar
        private string _currentTab = "FILTER";
        private bool _isFilterTabSelected = true;
        private bool _isHistoryTabSelected = false;
        public string CurrentTab
        {
            get { return _currentTab; }
            set 
            { 
                _currentTab = value; 
                NotifyOfPropertyChange(() => CurrentTab);
            }
        }
        public bool IsFilterTabSelected
        {
            get { return _isFilterTabSelected; }
            set 
            {
                _isFilterTabSelected = value;
                NotifyOfPropertyChange(() => IsFilterTabSelected);
            }
        }
        public bool IsHistoryTabSelected
        {
            get { return _isHistoryTabSelected; }
            set
            {
                _isHistoryTabSelected = value;
                NotifyOfPropertyChange(() => IsHistoryTabSelected);
            }
        }
        public void LoadFilterTab()
        {
            IsFilterTabSelected = true;
            CurrentTab = "FILTER";
        }
        public void LoadHistoryTab()
        {
            IsHistoryTabSelected = true;
            CurrentTab = "HISTORY";
        }
        #endregion

        #region Another Windows
        public void ShowInfo()
        {
            _windowManager.ShowDialogAsync(_infoViewModel);
        }
        public async void ShowSetting()
        {
            bool? result = await _windowManager.ShowDialogAsync(new SettingViewModel(_eventAggregator, _configuration.SelectorOption));
            if (result == true)
            {
                FileManager.Export(_configuration, ConfigPath);
            }
        }
        #endregion
    }
}
