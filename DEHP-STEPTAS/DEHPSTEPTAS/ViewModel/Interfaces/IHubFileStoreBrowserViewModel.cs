﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHubFileStoreBrowserViewModel.cs" company="Open Engineering S.A.">
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

    using ReactiveUI;
    using System.Reactive;

    /// <summary>
    /// Definition of methods and properties of <see cref="HubFileStoreBrowserViewModel"/>
    /// </summary>
    public interface IHubFileStoreBrowserViewModel
    {
        /// <summary>
        /// Gets the collection of STEP file names in the current iteration and active domain
        /// </summary>
        public ReactiveList<HubFile> HubFiles { get; }

        /// <summary>
        /// Sets and gets selected <see cref="HubFile"/> from <see cref="HubFiles"/> list
        /// </summary>
        public HubFile CurrentHubFile { get; set; }

        /// <summary>
        /// Uploads one STEP-TAS file to the <see cref="DomainFileStore"/> of the active domain
        /// </summary>
        ReactiveCommand<object> UploadFileCommand { get; }

        /// <summary>
        /// Downloads one STEP-TAS file from the <see cref="DomainFileStore"/> of active domain into user choosen location
        /// </summary>
        ReactiveCommand<Unit> DownloadFileAsCommand { get; }

        /// <summary>
        /// Downloads one STEP-TAS file from the <see cref="DomainFileStore"/> of active domain into the local storage
        /// </summary>
        ReactiveCommand<Unit> DownloadFileCommand { get; }

        /// <summary>
        /// Loads one STEP-TAS file from the local storage
        /// </summary>
        ReactiveCommand<Unit> LoadFileCommand { get; }
    }
}
