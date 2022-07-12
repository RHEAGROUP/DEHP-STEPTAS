// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHighLevelRepresentationBuilder.cs" company="Open Engineering S.A.">
//    Copyright (c) 2021 Open Engineering S.A.
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

namespace DEHPSTEPTAS.Builds.HighLevelRepresentationBuilder
{
    using DEHPSTEPTAS.ViewModel.Rows;
    using DEHPSTEPTAS.StepTas;
    using System.Collections.Generic;

    /// <summary>
    /// Helper class to create the High Level Representation (HLR) View Model for STEP TAS file
    /// </summary>
    public interface IHighLevelRepresentationBuilder
    {
        /// <summary>
        /// Creates the High Level Representation (HLR) View Model for STEP TAS file
        /// </summary>
        List<StepTasRowData> CreateHLR(StepTasFile steptas,int offset);
    }
}
