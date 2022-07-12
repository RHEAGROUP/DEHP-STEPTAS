// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstBrowserHeaderViewModel.cs" company="Open Engineering S.A.">
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
    using DEHPSTEPTAS.DstController;
    using DEHPSTEPTAS.ViewModel.Interfaces;
    using DEHPSTEPTAS.Views;
    using ReactiveUI;
    using DEHPSTEPTAS.StepTas;
    using System;

    /// <summary>
    /// The <see cref="DstBrowserHeaderViewModel"/> is the view model the <see cref="DstBrowserHeader"/>
    /// </summary>
    public class DstBrowserHeaderViewModel : ReactiveObject, IDstBrowserHeaderViewModel
    {
        #region Private Members

        /// <summary>
        /// The <see cref="IDstController"/> instance
        /// </summary>
        private readonly IDstController dstController;

        #endregion

        #region Private Members (STEP 3D Header Information)

        // FILE_DESCRIPTION
        private string description;
        private string implementation_level;

        // FILE_NAME
        private string name;
        private string time_stamp;
        private string author;
        private string organization;
        private string preprocessor_version;
        private string originating_system;
        private string authorization;

        // FILE_SHEMA
        private string file_schema;

        #endregion

        #region Reactive Properties

        /// <summary>
        /// Backing field for <see cref="FilePath"/>
        /// </summary>
        private string filePath = "<unloaded>";
       
        /// <summary>
        /// Gets or sets the current path to a STEP file.
        /// </summary>
        public string FilePath
        {
            get => this.filePath;
            set => this.RaiseAndSetIfChanged(ref this.filePath, value);
        }

        /// <summary>
        /// Gets or sets the FILE_HEADER.description.
        /// </summary>
        public string Description
        {
            get => this.description;
            set => this.RaiseAndSetIfChanged(ref this.description, value);
        }

        /// <summary>
        /// Gets or sets the FILE_HEADER.implementation_level.
        /// </summary>
        public string ImplementationLevel
        {
            get => this.implementation_level;
            set => this.RaiseAndSetIfChanged(ref this.implementation_level, value);
        }

        /// <summary>
        /// Gets or sets the FILE_NAME.name.
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Gets or sets the FILE_NAME.time_stamp.
        /// </summary>
        public string TimeStamp
        {
            get => this.time_stamp;
            set => this.RaiseAndSetIfChanged(ref this.time_stamp, value);
        }

        /// <summary>
        /// Gets or sets the FILE_NAME.author.
        /// </summary>
        public string Author
        {
            get => this.author;
            set => this.RaiseAndSetIfChanged(ref this.author, value);
        }

        /// <summary>
        /// Gets or sets the FILE_NAME.organization.
        /// </summary>
        public string Organization
        {
            get => this.organization;
            set => this.RaiseAndSetIfChanged(ref this.organization, value);
        }

        /// <summary>
        /// Gets or sets the FILE_NAME.preprocessor_version.
        /// </summary>
        public string PreprocessorVersion
        {
            get => this.preprocessor_version;
            set => this.RaiseAndSetIfChanged(ref this.preprocessor_version, value);
        }

        /// <summary>
        /// Gets or sets the FILE_NAME.originating_system.
        /// </summary>
        public string OriginatingSystem
        {
            get => this.originating_system;
            set => this.RaiseAndSetIfChanged(ref this.originating_system, value);
        }

        /// <summary>
        /// Gets or sets the FILE_NAME.authorization.
        /// </summary>
        public string Authorization
        {
            get => this.authorization;
            set => this.RaiseAndSetIfChanged(ref this.authorization, value);
        }

        /// <summary>
        /// Gets or sets the FILE_SCHEMA full string.
        /// </summary>
        public string FileSchema
        {
            get => this.file_schema;
            set => this.RaiseAndSetIfChanged(ref this.file_schema, value);
        }

        #endregion

        #region IDstBrowserHeaderViewModel interface

        /// <summary>
        /// Update all BrowswerHeader values.
        /// </summary>
        /// <param name="step3d"></param>
        public void UpdateHeader()
        {
            if (dstController.IsLoading)
            {
                InitializeValues();
                FilePath = "LOADING A FILE...";

                return;
            }

            StepTasFile step3d = dstController.StepTASFile;

            if (step3d == null || step3d.HasFailed)
            {
                InitializeValues();

                return;
            }

            FilePath = step3d.FileName;

            var hdr = step3d.HeaderInfo;

            var fdesc = hdr.description;
            Description = hdr.description;
            //ImplementationLevel = hdr

            //var fname = hdr.file_name;
            Name = hdr.name;
            TimeStamp = hdr.timeStamp;
            Author = hdr.author;
            Organization = hdr.organization;
            PreprocessorVersion = hdr.preprocessorVersion;
            OriginatingSystem = hdr.preprocessorVersion;
            Authorization = hdr.authorization;

            FileSchema = hdr.schema;
        }

        #endregion

        #region Constructor

        public DstBrowserHeaderViewModel(IDstController dstController)
        {
            this.dstController = dstController;

            this.WhenAnyValue(x => x.dstController.IsLoading).Subscribe(_ => this.UpdateHeader());
        }

        #endregion

        #region Private Methods

        private void InitializeValues()
        {
            FilePath = "<unloaded>";

            Description = "";
            ImplementationLevel = "";

            Name = "";
            TimeStamp = "";
            Author = "";
            Organization = "";
            PreprocessorVersion = "";
            OriginatingSystem = "";
            Authorization = "";

            FileSchema = "";
        }

        #endregion
    }
}
