// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstStepTasFileHeaderViewModel.cs" company="Open Engineering S.A.">
//    Copyright (c) 2021 Open Engineering S.A.
//
//    Author: Ivan Fontaine
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



using DEHPSTEPTAS.DstController;
using DEHPSTEPTAS.StepTas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEHPSTEPTAS.ViewModel
{
    public class DstStepTasFileHeaderViewModel:DstBrowserHeaderViewModel

    {

        public StepTasFile File { set; get; }
        public DstStepTasFileHeaderViewModel(IDstController dstController) : base(dstController) { }

        public  new void UpdateHeader()
        {

            if(File==null || File.HasFailed)

            {
                return;
            }
            StepTasFile step3d = File;


            FilePath = step3d.FileName;

            var hdr = step3d.HeaderInfo;

            
            //Description = hdr.
           // ImplementationLevel = fdesc.implementation_level;

          //  var fname = hdr.file_name;
            //Name = fname.name;
            //TimeStamp = fname.time_stamp;
            //Author = fname.author;
            //Organization = fname.organization;
            //PreprocessorVersion = fname.preprocessor_version;
            //OriginatingSystem = fname.originating_system;
            //Authorization = fname.authorisation; // Note: STEP TAS uses british english name

            //FileSchema = hdr.file_schema;



        }


    }
}
