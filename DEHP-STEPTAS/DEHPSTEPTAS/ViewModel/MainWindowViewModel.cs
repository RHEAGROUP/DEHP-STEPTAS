﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="Open Engineering S.A.">
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
    using DEHPCommon.Enumerators;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views.ExchangeHistory;
    using DEHPSTEPTAS.DstController;
    using DEHPSTEPTAS.ViewModel.Interfaces;
    using ReactiveUI;
    using System;
    using System.Windows.Input;

    /// <summary>
    /// <see cref="MainWindowViewModel"/> is the view model for <see cref="Views.MainWindow"/>
    /// 
    /// </summary>
    /// <remarks>
    /// From <see cref="DEHPCommon.UserInterfaces.Behaviors.SwitchLayoutPanelOrderBehavior"/>:
    /// The behavior well function relies on the panels to be of type <see cref="LayoutGroup"/> and on their name.
    /// Those needs to match the <see cref="LayoutGroupName"/>, <see cref="DstPanelName"/> and <see cref="HubPanelName"/>
    /// </remarks>
    public class MainWindowViewModel : ReactiveObject, IMainWindowViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// Gets the view model that represents the 10-25 data source
        /// </summary>
        public IHubDataSourceViewModel HubDataSourceViewModel { get; private set; }

        /// <summary>
        /// Gets the view model that represents the STEP-TAS data source
        /// </summary>
        public IDstDataSourceViewModel DstSourceViewModel { get; private set; }

        /// <summary>
        /// Gets the view model that represents the net change preview panel
        /// </summary>
        public IHubNetChangePreviewViewModel HubNetChangePreviewViewModel { get; }

        /// <summary>
        /// Gets the <see cref="ITransferControlViewModel"/>
        /// </summary>
        public ITransferControlViewModel TransferControlViewModel { get; }

        public IDstExtractionViewModel ExtractionViewModel { get; }

        public IUploadCSVViewModel UploadCSVViewModel { get; }

        /// <summary>
        /// Gets the <see cref="IMappingViewModel"/>
        /// </summary>
        public IMappingViewModel MappingViewModel { get; }

        /// <summary>
        /// Gets or sets the <see cref="ISwitchLayoutPanelOrderBehavior"/>
        /// </summary>
        public ISwitchLayoutPanelOrderBehavior SwitchPanelBehavior { get; set; }

        /// <summary>
        /// Gets the view model that represents the status bar
        /// </summary>
        public IStatusBarControlViewModel StatusBarControlViewModel { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> that will change the mapping direction
        /// </summary>
        public ReactiveCommand<object> ChangeMappingDirection { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> that will open the ExchangeHistory window
        /// </summary>
        public ReactiveCommand<object> OpenExchangeHistory { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="MainWindowViewModel"/>
        /// </summary>
        /// <param name="hubHubDataSourceViewModelViewModel">A <see cref="IHubDataSourceViewModel"/></param>
        /// <param name="dstSourceViewModelViewModel">A <see cref="IHubDataSourceViewModel"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="hubNetChangePreviewViewModel">The <see cref="IHubNetChangePreviewViewModel"/></param>
        /// <param name="mappingViewModel">The <see cref="IMappingViewModel"/></param>
        /// <param name="transferControlViewModel">The <see cref="ITransferControlViewModel"/></param>
        /// <param name="statusBarControlViewModel">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        public MainWindowViewModel(IHubDataSourceViewModel hubHubDataSourceViewModelViewModel,
            IDstDataSourceViewModel dstSourceViewModelViewModel,
            IDstController dstController,
            IHubNetChangePreviewViewModel hubNetChangePreviewViewModel,
            ITransferControlViewModel transferControlViewModel,
            IMappingViewModel mappingViewModel,
            IStatusBarControlViewModel statusBarControlViewModel,
            INavigationService navigationService,
            IDstExtractionViewModel extract,
            IUploadCSVViewModel uploadCSV
            )
        {
            this.dstController = dstController;
            this.HubDataSourceViewModel = hubHubDataSourceViewModelViewModel;
            this.DstSourceViewModel = dstSourceViewModelViewModel;
            this.HubNetChangePreviewViewModel = hubNetChangePreviewViewModel;
            this.MappingViewModel = mappingViewModel;
            this.TransferControlViewModel = transferControlViewModel;
            this.StatusBarControlViewModel = statusBarControlViewModel;
            this.navigationService = navigationService;
            this.ExtractionViewModel = extract;
            this.UploadCSVViewModel = uploadCSV;
            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/>
        /// </summary>
        private void InitializeCommands()
        {
            this.ChangeMappingDirection = ReactiveCommand.Create();
            this.ChangeMappingDirection.Subscribe(_ => this.ChangeMappingDirectionExecute());

            this.OpenExchangeHistory = ReactiveCommand.Create();
            this.OpenExchangeHistory.Subscribe(_ => this.navigationService.ShowDialog<ExchangeHistory>());
        }

        /// <summary>
        /// Executes the <see cref="ChangeMappingDirection"/>
        /// </summary>
        private void ChangeMappingDirectionExecute()
        {
            this.SwitchPanelBehavior?.Switch();
            this.dstController.MappingDirection = this.SwitchPanelBehavior?.MappingDirection ?? MappingDirection.FromDstToHub;
        }
    }
}
