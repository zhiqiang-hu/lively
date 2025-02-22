﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Lively.Common.Helpers.Pinvoke;
using Lively.Models;
using Lively.UI.WinUI.Factories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Lively.Common.Factories;
using CommunityToolkit.WinUI.Collections;
using Lively.UI.WinUI.Extensions;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class FindMoreAppsViewModel : ObservableObject
    {
        private readonly IApplicationsFactory appFactory;
        private readonly string[] excludedClasses =
        [
            //uwp apps
            "ApplicationFrameWindow",
            //startmeu, taskview (win10), action center etc
            "Windows.UI.Core.CoreWindow",
        ];

        [ObservableProperty]
        private ObservableCollection<ApplicationModel> applications = [];

        [ObservableProperty]
        private AdvancedCollectionView applicationsFiltered;

        [ObservableProperty]
        private ApplicationModel selectedItem;

        public FindMoreAppsViewModel(IApplicationsFactory appFactory)
        {
            this.appFactory = appFactory;
            ApplicationsFiltered = new AdvancedCollectionView(Applications, true);
            ApplicationsFiltered.SortDescriptions.Add(new SortDescription("AppName", SortDirection.Ascending));

            using (ApplicationsFiltered.DeferRefresh())
            {
                foreach (var item in Process.GetProcesses()
                    .Where(x => x.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(x.MainWindowTitle) && !IsExcluded(x.MainWindowHandle)))
                {
                    var app = appFactory.CreateApp(item);
                    if (app is not null)
                        Applications.Add(app);
                }
            }
            SelectedItem = Applications.FirstOrDefault();
        }

        private RelayCommand _browseCommand;
        public RelayCommand BrowseCommand => _browseCommand ??= new RelayCommand(async() => await BrowseApp());

        private async Task BrowseApp()
        {
            var filePicker = new FileOpenPicker();
            filePicker.SetOwnerWindow(App.Services.GetRequiredService<MainWindow>());
            filePicker.FileTypeFilter.Add(".exe");
            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                var app = appFactory.CreateApp(file.Path);
                if (app is not null)
                {
                    Applications.Add(app);
                    SelectedItem = app;
                }
            }
        }

        private bool IsExcluded(IntPtr hwnd)
        {
            const int maxChars = 256;
            StringBuilder className = new StringBuilder(maxChars);
            return NativeMethods.GetClassName((int)hwnd, className, maxChars) > 0 && excludedClasses.Any(x => x.Equals(className.ToString(), StringComparison.Ordinal));
        }
    }
}
