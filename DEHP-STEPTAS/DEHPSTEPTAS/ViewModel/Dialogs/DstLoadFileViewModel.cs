﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstLoadFileViewModel.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
//
//    Part of the code was based on the work performed by RHEA as result
//    of the collaboration in the context of "Digital Engineering Hub Pathfinder"
//    by Sam Gerené, Alex Vorobiev, Alexander van Delft and Nathanael Smiechowski.
// 
//    This file is part of DEHP STEP-TAS adapter project.
// 
//    The DEHP STEP-TAS is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHP STEP-TAS is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPTAS.ViewModel.Dialogs
{
    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPTAS.DstController;
    using DEHPSTEPTAS.Settings;
    using DEHPSTEPTAS.ViewModel.Dialogs.Interfaces;
    using Microsoft.Win32;
    using ReactiveUI;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reactive;
    using System.Threading.Tasks;


    /// <summary>
    /// The view-model for the Login that allows users to open a STEP-TAS file.
    /// </summary>
    public class DstLoadFileViewModel : ReactiveObject, IDstLoadFileViewModel, ICloseWindowViewModel
    {
        #region Private Members

        /// <summary>
        /// The <see cref="IDstController"/> instance
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarControlView;

        /// <summary>
        /// The <see cref="IUserPreferenceService{AppSettings}"/> instance
        /// </summary>
        private readonly IUserPreferenceService<AppSettings> userPreferenceService;

        #endregion

        #region IDstLoadFileViewModel interface

        /// <summary>
        /// Backing field for <see cref="FilePath"/>
        /// </summary>
        private string filePath;

        /// <summary>
        /// Gets or sets the current path to a STEP file.
        /// </summary>
        public string FilePath
        {
            get => filePath;
            set => this.RaiseAndSetIfChanged(ref this.filePath, value);
        }

        /// <summary>
        /// Loads the current <see cref="FilePath"/> and closes the window.
        /// </summary>
        public ReactiveCommand<Unit> LoadFileCommand { get; private set; }

        /// <summary>
        /// Executes the <see cref="LoadFileCommand"/> asynchronously
        /// </summary>
        protected async Task LoadFileCommandExecuteAsync()
        {
            IsLoadingFile = true;
            statusBarControlView.Append($"Loading file: {FilePath}");

            try
            {
                await dstController.LoadAsync(FilePath);

                IsLoadingFile = false;

                if (dstController.IsFileOpen)
                {
                    statusBarControlView.Append("Load successful");

                    AddToRecentFiles(FilePath);
                    SaveRecentFiles();

                    CloseWindowBehavior?.Close();
                }
                else
                {
                    statusBarControlView.Append($"Failure loading file: {FilePath}", StatusBarMessageSeverity.Error);
                }
            }
            catch (InvalidOperationException e)
            {
                statusBarControlView.Append(e.Message, StatusBarMessageSeverity.Error);
            }
            finally
            {
                IsLoadingFile = false;
            }
        }

        #endregion

        #region ICloseWindowViewModel interface

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        #endregion

        #region Public Reactive Properties

        /// <summary>
        /// Backing field for <see cref="MainLabel"/>
        /// </summary>
        private string mainLabel;

        /// <summary>
        /// Gets or sets the main label title.
        /// </summary>
        public string MainLabel
        {
            get => mainLabel;
            private set => this.RaiseAndSetIfChanged(ref this.mainLabel, value);
        }

        /// <summary>
        /// Backing field for <see cref="IsLoadingFile"/>
        /// </summary>
        private bool loadingFile;

        /// <summary>
        /// Gets or sets the current loading task status.
        /// </summary>
        public bool IsLoadingFile
        {
            get => loadingFile;
            private set => this.RaiseAndSetIfChanged(ref this.loadingFile, value);
        }

        /// <summary>
        /// List of recent opened STEP files.
        /// </summary>
        public ReactiveList<string> RecentFiles { get; private set; } = new ReactiveList<string> { ChangeTrackingEnabled = true };

        /// <summary>
        /// Load STEP-TAS file from the local machine.
        /// 
        /// <seealso cref="FilePath"/>
        /// </summary>
        public ReactiveCommand<object> SelectFileCommand { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DstLoadFileViewModel"/> class.
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarControlView">The <see cref="IStatusBarControlViewModel"/></param>
        public DstLoadFileViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlView, IUserPreferenceService<AppSettings> userPreferenceService)
        {
            this.dstController = dstController;
            this.statusBarControlView = statusBarControlView;
            this.userPreferenceService = userPreferenceService;

            InitializeUI();
            InitializeCommands();
        }

        #endregion

        #region Protected/Private Methods

        /// <summary>
        /// Initializes the User Interface components
        /// </summary>
        private void InitializeUI()
        {
            // Initialize main title and subscribe to any change
            UpdateMainLabel();
            this.WhenAnyValue(vm => vm.IsLoadingFile).Subscribe(_ => UpdateMainLabel());

            // Load recent files and initialize with the last opened file
            PopulateRecentFiles();

            if (!RecentFiles.IsEmpty)
            {
                FilePath = RecentFiles[0];
            }
        }

        /// <summary>
        /// Instantiates the commands.
        /// </summary>
        private void InitializeCommands()
        {
            // Select File is only available when no loading task is in progress
            var canSelectFile = this.WhenAnyValue(
                vm => vm.IsLoadingFile,
                (loading) => !loading);

            SelectFileCommand = ReactiveCommand.Create(canSelectFile);
            SelectFileCommand.Subscribe(_ => SelectFileCommandExecute());

            // Load File button is activated when the FilePath points to a existing file
            var canLoadFile = this.WhenAnyValue(
                vm => vm.FilePath,
                vm => vm.IsLoadingFile,
                (fn, loading) => File.Exists(fn) && !loading);

            LoadFileCommand = ReactiveCommand.CreateAsyncTask(canLoadFile, async _ => await this.LoadFileCommandExecuteAsync(), RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Executes the <see cref="SelectFileCommand"/>
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected void SelectFileCommandExecute()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "STEP-TAS|*.step;*.stp|All types|*.*",
                InitialDirectory = Path.GetDirectoryName(FilePath)
            };

            if (dlg.ShowDialog() == true)
            {
                FilePath = dlg.FileName;
            }
        }

        /// <summary>
        /// Updates main title label according to the <see cref="IsLoadingFile"/> status.
        /// </summary>
        private void UpdateMainLabel()
        {
            if (IsLoadingFile)
            {
                MainLabel = "Loading STEP-TAS file... please wait";
            }
            else
            {
                MainLabel = "Load STEP-TAS file";
            }
        }

        /// <summary>
        /// Loads the saved recent files into the <see cref="RecentFiles"/>
        /// </summary>
        private void PopulateRecentFiles()
        {
            userPreferenceService.Read();
            RecentFiles.Clear();
            RecentFiles.AddRange(this.userPreferenceService.UserPreferenceSettings.RecentFiles);
        }

        /// <summary>
        /// Adds a file name to the <see cref="RecentFiles"/> list.
        /// 
        /// It behaves as a stack: the last used in the first in the list (more recent).
        /// 
        /// Duplicated entries are silently ignored.
        /// </summary>
        /// <param name="filename">Full path to a file</param>
        private void AddToRecentFiles(string filename)
        {
            // First files are the latest used
            if (RecentFiles.Contains(filename))
            {
                RecentFiles.Remove(filename);
            }

            RecentFiles.Insert(0, filename);
        }

        /// <summary>
        /// Updates and saves the recent files into the <see cref="RecentFiles"/>
        /// </summary>
        private void SaveRecentFiles()
        {
            userPreferenceService.UserPreferenceSettings.RecentFiles.Clear();
            userPreferenceService.UserPreferenceSettings.RecentFiles.AddRange(RecentFiles);

            userPreferenceService.Save();
        }

        #endregion
    }
}
