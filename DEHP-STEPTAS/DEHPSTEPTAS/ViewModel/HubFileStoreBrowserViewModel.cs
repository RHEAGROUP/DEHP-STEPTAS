﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubFileStoreBrowserViewModel.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPTAS.ViewModel
{
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.FileDialogService;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPTAS.Dialog.Interfaces;
    //using DEHPSTEPTAS.Dialogs;
    using DEHPSTEPTAS.DstController;
    using DEHPSTEPTAS.Events;
    using DEHPSTEPTAS.Services.DstHubService;
    using DEHPSTEPTAS.Services.FileStoreService;
    using DEHPSTEPTAS.Settings;
    using DEHPSTEPTAS.ViewModel.Interfaces;
    using DEHPSTEPTAS.Views.Dialogs;
    using NLog;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Wrapper class to display <see cref="FileRevision"/> of a STEP <see cref="File"/>
    /// </summary>
    public class HubFile
    {
        /// <summary>
        /// Gets the <see cref="FileRevision.Path"/>
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets the <see cref="FileRevision.RevisionNumber"/>
        /// </summary>
        public int RevisionNumber { get; private set; }

        /// <summary>
        /// Gets the <see cref="FileRevision.CreatedOn"/>
        /// </summary>
        public DateTime CreatedOn { get; private set; }

        /// <summary>
        /// Gets the <see cref="Person"/> full name creator of this <see cref="FileRevision"/>
        /// </summary>
        public string CreatorFullName { get; private set; }

        /// <summary>
        /// The referenced <see cref="FileRevision"/>
        /// </summary>
        internal FileRevision FileRev { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> of this representation</param>
        public HubFile(FileRevision fileRevision)
        {
            FileRev = fileRevision;

            FilePath = FileRev.Path;
            RevisionNumber = FileRev.RevisionNumber;
            CreatedOn = FileRev.CreatedOn;
            CreatorFullName = $"{FileRev.Creator.Person.GivenName} {FileRev.Creator.Person.Surname.ToUpper()}";
        }
    }

    /// <summary>
    /// ViewModel for all the STEP files in the current <see cref="Iteration"/>
    /// and <see cref="EngineeringModel"/>.
    ///
    /// Only last revisions are shown.
    /// </summary>
    public class HubFileStoreBrowserViewModel : ReactiveObject, IHubFileStoreBrowserViewModel
    {
        #region Private members

        /// <summary>
        /// The <see cref="IDstController"/> instance
        /// </summary>
        
        private readonly IHubController hubController;

        private readonly IDstController dstController;

        
        /// <summary>
        /// The <see cref="IDstHubService"/> instance
        /// </summary>
        private readonly IDstHubService dstHubService;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// The <see cref="IFileStoreService"/> instance
        /// </summary>
        private readonly IFileStoreService fileStoreService;

        /// <summary>
        /// The <see cref="IOpenSaveFileDialogService"/>
        /// </summary>
        private readonly IOpenSaveFileDialogService fileDialogService;

        // <summary>
        /// The <see cref="IDstCompareStepFilesViewModel"/>
        /// </summary>

        private readonly IDstCompareStepFilesViewModel fileCompare;

        // <summary>
        /// The <see cref="IUserPreferenceService"/>
        /// </summary>
        private readonly IUserPreferenceService<AppSettings> userPreferenceService;
        private readonly INavigationService navigationService;

        /// <summary>
        /// Backing field for <see cref="IsBusy"/>
        /// </summary>
        private bool? isBusy;

        /// <summary>
        /// Gets or sets a value indicating whether the browser is busy
        /// </summary>
        public bool? IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        #endregion Private members

        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        #region IHubFileBrowserViewModel interface

        /// <summary>
        /// Gets the collection of STEP file names in the current iteration and active domain
        /// </summary>
        public ReactiveList<HubFile> HubFiles { get; private set; }

        /// <summary>
        /// Backing field for <see cref="CurrentHubFile"/>
        /// </summary>
        private HubFile currentHubFile;

        /// <summary>
        /// Sets and gets selected <see cref="HubFile"/> from <see cref="HubFiles"/> list
        /// </summary>
        public HubFile CurrentHubFile
        {
            get => currentHubFile;
            set => this.RaiseAndSetIfChanged(ref this.currentHubFile, value);
        }

        private string localStepFilePath;

        public string LocalStepFilePath
        {
            get => localStepFilePath;
            set => this.RaiseAndSetIfChanged(ref this.localStepFilePath, value);
        }

        /// <summary>
        /// Uploads one STEP-TAS file to the <see cref="DomainFileStore"/> of the active domain
        /// </summary>
        public ReactiveCommand<object> UploadFileCommand { get; private set; }

        /// <summary>
        /// Downloads one STEP-TAS file from the <see cref="DomainFileStore"/> of active domain into user choosen location
        ///
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<Unit> DownloadFileAsCommand { get; private set; }

        /// <summary>
        /// Downloads one STEP-TAS file from the <see cref="DomainFileStore"/> of active domain into the local storage
        ///
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<Unit> DownloadFileCommand { get; private set; }

        /// <summary>
        /// Loads one STEP-TAS file from the local storage
        ///
        /// If files does not exist, it call <see cref="DownloadFileCommand"/> first.
        ///
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<Unit> LoadFileCommand { get; private set; }

        public ReactiveCommand<Unit> CompareFileCommand { get; private set; }

        #endregion IHubFileBrowserViewModel interface

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hubController"></param>
        /// <param name="statusBarControlView"></param>
        /// <param name="fileStoreService"></param>
        /// <param name="dstHubService"></param>
        /// <param name="fileDialogService"></param>
        /// <param name=userPreferenceService></param>
        public HubFileStoreBrowserViewModel(IHubController hubController, IStatusBarControlViewModel statusBarControlView,
            IFileStoreService fileStoreService, IDstHubService dstHubService, 
            IOpenSaveFileDialogService fileDialogService, IDstController dstController, IDstCompareStepFilesViewModel fileCompare,
            IUserPreferenceService<AppSettings> userPreferenceService, INavigationService navigationService)
        {
            this.hubController = hubController;
            this.statusBar = statusBarControlView;
            this.fileStoreService = fileStoreService;
            this.dstHubService = dstHubService;
            this.fileDialogService = fileDialogService;
            this.dstController = dstController;
            this.fileCompare = fileCompare;
            this.userPreferenceService = userPreferenceService;
            this.navigationService = navigationService;
            HubFiles = new ReactiveList<HubFile>();

            InitializeCommandsAndObservables();
        }

        #endregion Constructor

        #region Private/Protected methods

        /// <summary>
        /// Initializes the commands
        /// </summary>
        private void InitializeCommandsAndObservables()
        {
            // Change on connection
            this.WhenAnyValue(x => x.hubController.OpenIteration).ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => this.UpdateFileList());

            // Refresh/Transfer emits UpdateObjectBrowserTreeEvent (cache changed)
            CDPMessageBus.Current.Listen<UpdateObjectBrowserTreeEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => this.UpdateFileList());

            // Ask for download
            CDPMessageBus.Current.Listen<DownloadFileRevisionEvent>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async x => await this.DownloadFileRevisionIdAs(x.TargetId));

            // Commands on selected FileRevision
            var fileSelected = this.WhenAny(
                vm => vm.CurrentHubFile,
                (x) => x.Value != null);

            var cancompare =  this.WhenAnyValue(vm => vm.dstController.IsFileOpen ).ObserveOn(RxApp.MainThreadScheduler).CombineLatest(fileSelected, (a, b) => a && b).DistinctUntilChanged();
            
            this.CompareFileCommand = ReactiveCommand.CreateAsyncTask(cancompare, async x => await CompareFileCommandExecute());

          

            this.DownloadFileCommand = ReactiveCommand.CreateAsyncTask(fileSelected, async _ => await this.DownloadFileCommandExecute());

            this.DownloadFileAsCommand = ReactiveCommand.CreateAsyncTask(fileSelected, async _ => await this.DownloadFileAsCommandExecute());
        }

        /// <summary>
        /// Fills the list of STEP files
        ///
        /// Files correspond to the current <see cref="Iteration"/> and current <see cref="DomainOfExpertise"/>
        /// and <see cref="EngineeringModel"/>.
        /// </summary>
        private void UpdateFileList()
        {
            if (!this.hubController.IsSessionOpen || this.hubController.OpenIteration is null)
            {
                CurrentHubFile = null;
                HubFiles.Clear();
                return;
            }

            this.IsBusy = true;

            var revisions = dstHubService.GetFileRevisions();

            List<HubFile> hubfiles = new List<HubFile>();

            foreach (var rev in revisions)
            {
                var item = new HubFile(rev);
                hubfiles.Add(item);
            }

            CurrentHubFile = null;
            HubFiles.Clear();
            HubFiles.AddRange(hubfiles);

            this.IsBusy = false;
        }

        /// <summary>
        /// Gets the <see cref="FileRevision"/> of the <see cref="CurrentHubFile"/>
        /// </summary>
        /// <returns>The <see cref="FileRevision"/></returns>
        private FileRevision CurrentFileRevision()
        {
            var frev = HubFiles.FirstOrDefault(x => x.FilePath == CurrentHubFile?.FilePath);

            if (frev is null)
            {
                return null;
            }

            return frev.FileRev;
        }

        /// <summary>
        /// Shows the <see cref="IOpenSaveFileDialogService.GetSaveFileDialog()"/> for a <see cref="FileRevision"/>
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> to be downloaded</param>
        /// <returns>The destination path, or null if cancelled by the user</returns>
        private string GetSaveFileDestination(FileRevision fileRevision)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(fileRevision.Path);
            var extension = System.IO.Path.GetExtension(fileRevision.Path);
            var filter = $"{extension.Replace(".", "").ToUpper()} files|*{extension}|All files (*.*)|*.*";

            var destinationPath = this.fileDialogService.GetSaveFileDialog(fileName, extension, filter, string.Empty, 1);
            return destinationPath;
        }

        /// <summary>
        /// Downloads the <see cref="FileRevision"/> into a file
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> to be downloaded</param>
        /// <param name="destinationPath">Full name path to file</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task DownloadFileRevision(FileRevision fileRevision, string destinationPath)
        {
            if (fileRevision is null)
            {
                return;
            }

            IsBusy = true;
            //Application.Current.Dispatcher.Invoke(() => this.statusBar.Append("Downloading file from Hub..."));
            this.statusBar.Append("Downloading file from Hub...");

            using (var fstream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
            {
                await hubController.Download(fileRevision, fstream);
            }

            this.statusBar.Append($"Downloaded as: {destinationPath}");
            IsBusy = false;
        }

        /// <summary>
        /// Downloads the <see cref="FileRevision"/> into destination choosen by the user
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> to be downloaded</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task DownloadFileRevisionAs(FileRevision fileRevision)
        {
            if (fileRevision is null)
            {
                this.statusBar.Append($"The FileRevision is null", StatusBarMessageSeverity.Error);
                return;
            }

            var destinationPath = this.GetSaveFileDestination(fileRevision);
            if (destinationPath is null)
            {
                return;
            }

            await this.DownloadFileRevision(fileRevision, destinationPath);
        }

        /// <summary>
        /// Downloads the <see cref="FileRevision"/> from its <see cref="System.Guid"/> into destination choosen by the user
        /// </summary>
        /// <param name="guid"><see cref="System.Guid"/> value</param>
        /// <returns>A <see cref="Task"/></returns>
        /// <remarks>
        /// The <paramref name="guid"/> is validated as <see cref="System.Guid"/> pointing to a <see cref="FileRevision"/>
        /// in the current <see cref="DomainOfExpertise"/>
        /// </remarks>
        private async Task DownloadFileRevisionIdAs(string guid)
        {
            var fileRevision = this.dstHubService.FindFileRevision(guid);
            if (fileRevision is null)
            {
                this.statusBar.Append($"The Guid {guid} did not correspond to a valid FileRevision", StatusBarMessageSeverity.Warning);
                return;
            }

            await this.DownloadFileRevisionAs(fileRevision);
        }

        /// <summary>
        /// Executes the <see cref="DownloadFileCommand"/> asynchronously.
        ///
        /// File is downloaded from the Hub into destination choosen by the user.
        /// </summary>
        private async Task DownloadFileAsCommandExecute()
        {
            var fileRevision = CurrentFileRevision();
            if (fileRevision is null)
            {
                statusBar.Append("No current file selected to perform the download");
                return;
            }

            await this.DownloadFileRevisionAs(fileRevision);
        }

        /// <summary>
        /// Executes the <see cref="DownloadFileCommand"/> asynchronously.
        ///
        /// File is downloaded from the Hub and stored locally.
        /// <seealso cref="FileStoreService"/>.
        /// </summary>
        private async Task DownloadFileCommandExecute()
        {
            var fileRevision = CurrentFileRevision();
            if (fileRevision is null)
            {
                statusBar.Append("No current file selected to perform the download");
                return;
            }

            IsBusy = true;

            //Application.Current.Dispatcher.Invoke(() => statusBar.Append("Downloading file from Hub..."));
            statusBar.Append("Downloading file from Hub...");

            using (var fstream = fileStoreService.AddFileStream(fileRevision))
            {
                await hubController.Download(fileRevision, fstream);
            }

            // Remove comment
            statusBar.Append("Download successful");
                       

            IsBusy = false;
        }

       
        /**<summary>
         * The command executable code for comparing two step files.
         * This command manages the different dialog boxes.
         * </summary>
         * */
        private async Task CompareFileCommandExecute()
        {
            await DownloadFileCommandExecute();
            string hubdestinationPath =  fileStoreService.GetPath(CurrentFileRevision());
            string loadedStepFilePath = this.dstController.StepTASFile.FileName;
            logger.Debug("Step comparison: Hub file is located here : {0}", hubdestinationPath);
            logger.Debug("Step comparison: Local file is located here : {0} ", loadedStepFilePath);



            //this.navigationService..Show<UndeterminateProgressBar>();
            
            UndeterminateProgressBar dlg = null;
            if (!dstController.CodeCoverageState)
            {
                dlg = new UndeterminateProgressBar();
                dlg.Show();
            }

            bool isOK = false;
            await Task.Run(() =>
            {
                statusBar.Append("Loading files.");
                isOK = this.fileCompare.SetFiles(loadedStepFilePath, hubdestinationPath);
                statusBar.Append("Comparing the files.");
                isOK = isOK && this.fileCompare.Process();
            });

            //DEHPSTEPTAS.Dialogs.DstCompareStepFilesViewModel compVM = (DEHPSTEPTAS.Dialogs.DstCompareStepFilesViewModel) this.fileCompare;
            //int nbEntries = compVM.Step3DHLR.Count;
            //foreach (var entity in compVM.Step3DHLR)
            //    entity.PartOf=Rows.StepTasDiffRowViewModel.PartOfKind.BOTH;


            if (!dstController.CodeCoverageState) 
                dlg.Close();

            if (!isOK)
            {
                statusBar.Append(string.Format(string.Format("An error occured when comparing\n {0} and\n {1}", loadedStepFilePath, hubdestinationPath)));
                MessageBox.Show(string.Format("An error occured when comparing\n {0} and\n {1}", loadedStepFilePath, hubdestinationPath), "An Error Occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (!dstController.CodeCoverageState)
                {
                    var compareDialog = new DstCompareStepFiles()
                    {
                        DataContext = this.fileCompare
                    };

                    compareDialog.ShowDialog();
                }
                
            }
            statusBar.Append("");
        }

        #endregion Private/Protected methods
    }
}