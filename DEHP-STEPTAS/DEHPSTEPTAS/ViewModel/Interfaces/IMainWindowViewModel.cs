﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMainWindowsViewModel.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPTAS.ViewModel.Interfaces
{
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using ReactiveUI;

    /// <summary>
    /// Interface definitions of methods and properties of <see cref="Views.MainWindow"/>
    /// </summary>
    public interface IMainWindowViewModel : ISwitchLayoutPanelOrderViewModel
    {
        /// <summary>
        /// Gets the view model that represents the 10-25 data source
        /// </summary>
        IHubDataSourceViewModel HubDataSourceViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the EcosimPro data source
        /// </summary>
        IDstDataSourceViewModel DstSourceViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the net change preview panel to the 10-25 data source
        /// </summary>
        IHubNetChangePreviewViewModel HubNetChangePreviewViewModel { get; }

        /// <summary>
        /// Gets the <see cref="ITransferControlViewModel"/>
        /// </summary>
        ITransferControlViewModel TransferControlViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the status bar
        /// </summary>
        IStatusBarControlViewModel StatusBarControlViewModel { get; }

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> that will change the mapping direction
        /// </summary>
        ReactiveCommand<object> ChangeMappingDirection { get; }
    }
}
