﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDstCompareStepFileViewModel.cs" company="Open Engineering S.A.">
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEHPSTEPTAS.Dialog.Interfaces
{
    public interface IDstCompareStepFilesViewModel
    {
        /** <summary>>Do the main DIff process. 
         * </summary>
         * <returns>true if the diff is successful, false otherwise</returns>
         */
        bool Process();
        /** <summary>
         * Sets the two input files path of the files we want to display the differences and loads them.
         * </summary>
         * <returns>false if an error occured  while reading one of the files. true if they could be read.</returns>
         */
        bool SetFiles(string path1, string path2);
    }
}
