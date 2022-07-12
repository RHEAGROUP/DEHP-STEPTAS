// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Step3DDiffRowViewModel.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
//
//    Authors: Juan Pablo Hernandez Vogt, Ivan Fontaine
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

namespace DEHPSTEPTAS.ViewModel.Rows
{
    using DEHPSTEPTAS.ViewModel.Interfaces;
    using ReactiveUI;
    //using STEP3DAdapter;
    using System;
    using System.Collections.Generic;
   

    /// <summary>
    /// The <see cref="StepTasDiffRowViewModel"/> is the node in the Step comparison tree
    ///
    /// <seealso cref="DstObjectBrowserViewModel"/>
    /// <seealso cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/>
    /// </summary>
    public class StepTasDiffRowViewModel : ReactiveObject
    {
       
        public enum PartOfKind {  BOTH,FIRST,SECOND,SECONDTORELOCATE}

        private readonly StepTasRowData stepRowData;

        #region HLR Tree Indexes

        /// <summary>
        /// Auxiliary index for tree control.
        /// </summary>
        /// <remarks>
        /// It is an unique value in the <see cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/> context.
        /// </remarks>

        #endregion HLR Tree Indexes

        #region Part Fields

        /// <summary>
        /// Gets the part instance name
        /// </summary>
        /// <remarks>
        /// The instance is the part name and the usage id <see cref="STNodeRelation.id"/>
        /// representing a unique string for the part.
        /// </remarks>
        

        /// <summary>
        /// Get full path of compised part instance names
        /// </summary>
        public string  Path { get => stepRowData.Path; }

        /// <summary>
        /// Get Part name.

        public string Signature { get => stepRowData.GetSignature(); }
        /// </summary>
        public string Name { get => stepRowData.Name; }

        /// <summary>
        /// Get short entity type.
        /// </summary>
        public string Type { get => stepRowData.Type; }

        /// <summary>
        /// Get STEP entity type.
        /// </summary>
        public string RepresentationType { get => stepRowData.RepresentationType; }

        /// <summary>
        /// Get STEP entity file Id.
        /// </summary>
        public int StepId { get => stepRowData.ID; }

        public int ID { get => stepRowData.ID; }

        
        public int ParentID { get => stepRowData.ParentID; }

        /// <summary>
        /// Compose a reduced description of the <see cref="STNode"/>
        /// </summary>
        public string Description
        {
            get => $"{stepRowData.Type}#{stepRowData.StepId} '{stepRowData.Name}'";
        }

        /// <summary>
        /// Gets a label of association
        /// </summary>
        public string RelationLabel { get => stepRowData.RelationLabel; }

        /// <summary>
        /// Gets the Get STEP entity file Id of the relation (NAUO)
        /// </summary>
        public string RelationId { get => $"{stepRowData.StepId}"; }

        #endregion Part Fields       
               
        /** <summary>Used to keep track of the node belonging during the comparison process
         * </summary>
         */
        public PartOfKind PartOf { get; set; }
      
        #region Constructor

        public StepTasDiffRowViewModel(StepTasRowData rowdata, PartOfKind partOf)
        {
            this.stepRowData = rowdata;
            this.PartOf = partOf;
          
        }

        #endregion Constructor
    }
}