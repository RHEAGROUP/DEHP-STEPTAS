// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
//
//    Part of the code was based on the work performed by RHEA as result
//    of the collaboration in the context of "Digital Engineering Hub Pathfinder"
//    by Sam Gerené, Alex Vorobiev, Alexander van Delft and Nathanael Smiechowski.
// 
//    This file is part of DEHP STEP-TAS (STEP 3D CAD) adapter project.
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

namespace DEHPSTEPTAS.DstController
{
    using CDP4Common.EngineeringModelData;
    using CDP4Common.Types;
    using DEHPCommon.Enumerators;
    using DEHPSTEPTAS.ViewModel.Rows;
    using ReactiveUI;
    using DEHPSTEPTAS.StepTas;
    //using STEP3DAdapter;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Helper class which keeps a reference to the <see cref="ValueArray{string}"/> 
    /// that needs to me updated with the new <see cref="FileRevision"/> of the source
    /// STEP 3D file in the Hub.
    /// </summary>
    public class MappedParameterValue
    {
        /// <summary>
        /// The <see cref="StepTasRowViewModel"/> originating the change
        /// </summary>
        public readonly StepTasRowViewModel Part;

        /// <summary>
        /// The <see cref="ValueArray{string}"/> of the <see cref="IValueSet"/> of interest (fields of the compound parameter)
        /// </summary>
        public readonly ValueArray<string> Fields;
        
        /// <summary>
        /// The index in the <see cref="ValueArray{string}"/> for the <see cref="ParameterTypeComponent"/> corresponding to the "source" field
        /// </summary>
        private readonly int componentIndex;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="part">The <see cref="StepTasRowViewModel"/></param>
        /// <param name="fields">The <see cref="ValueArray{string}"/> of the <see cref="IValueSet"/> of interest to be updated</param>
        /// <param name="componentIndex">The index corresponding to the source field</param>
        public MappedParameterValue(StepTasRowViewModel part, ValueArray<string> fields, int componentIndex)
        {
            this.Part = part;
            this.Fields = fields;
            this.componentIndex = componentIndex;
        }

        /// <summary>
        /// Updates the <see cref="ValueArray{string}"/> associated to the source parameter
        /// </summary>
        /// <param name="fileRevision"></param>
        public void UpdateSource(FileRevision fileRevision)
        {
            this.Fields[componentIndex] = fileRevision.Iid.ToString();
        }
    }

    /// <summary>
    /// Interface definition for <see cref="DstController"/>
    /// </summary>
    public interface IDstController
    {
        /// <summary>
        /// Gets this running tool name
        /// </summary>
        string ThisToolName { get; }

        /// <summary>
        /// Returns the status of the last load action.
        /// </summary>
        bool IsFileOpen { get; }

        /// <summary>
        /// Gets or sets the status flag for the load action.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Gets the <see cref="StepTasFile"/> instance.
        /// </summary>
        StepTasFile StepTASFile { get; }

        

        /// <summary>
        /// Loads a STEP-TAS file.
        /// </summary>
        /// <param name="filename">Full path to file</param>
        void Load(string filename);

        /// <summary>
        /// Loads a STEP-TAS file asynchronusly.
        /// </summary>
        /// <param name="filename">Full path to file</param>
        Task LoadAsync(string filename);

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection"/>
        /// </summary>
        MappingDirection MappingDirection { get; set; }


        /// <summary>
        /// Gets or sets the <see cref="CodeCoverageState"/>
        /// </summary>
        bool CodeCoverageState { get; set; }


        /// <summary>
        /// Gets the collection of <see cref="ExternalIdentifierMap"/>s
        /// </summary>
        IEnumerable<ExternalIdentifierMap> AvailablExternalIdentifierMap { get; }

        /// <summary>
        /// Gets the colection of mapped <see cref="ElementDefinition"/>s and <see cref="Parameter"/>s
        /// </summary>
        ReactiveList<ElementBase> MapResult { get; }
        /// <summary>
        /// Contains the list of ElementDeinfinition or ElementUsage that will be transfere
        /// </summary>
        public ReactiveList<ElementBase> SelectedThingsToTransfer { get; }

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of all mapped parameter and the associate <see cref="StepTasRowViewModel.ID"/>
        /// </summary>
        Dictionary<ParameterOrOverrideBase, MappedParameterValue> ParameterNodeIds { get; }

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        ExternalIdentifierMap ExternalIdentifierMap { get; }

        /// <summary>
        /// Gets the collection of <see cref="IdCorrespondences"/>
        /// </summary>
        List<IdCorrespondence> IdCorrespondences { get; }

        /// <summary>
        /// Helper method for debugging in console the content of <see cref="IdCorrespondence"/>
        /// </summary>
        /// <param name="correspondences">The <see cref="IEnumerable{IdCorrespondence}"/> of correspondances</param>
        void ShowCorrespondences(IEnumerable<IdCorrespondence> correspondences);

        /// <summary>
        /// Helper method for debugging in console the content of <see cref="IdCorrespondence"/>
        /// </summary>
        /// <param name="correspondences">The <see cref="IdCorrespondence"/> of correspondances</param>
        void ShowCorrespondence(IdCorrespondence correspondence);

        /// <summary>
        /// Updates the configured mapping
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        void MergeExternalIdentifierMap();

        /// <summary>
        /// Updates the mappings of the current <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="externalIdentifierMap">The <see cref="ExternalIdentifierMap"/></param>
        void SetExternalIdentifierMap(ExternalIdentifierMap externalIdentifierMap);

        /// <summary>
        /// Creates and sets the <see cref="DstController.ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="DstController.ExternalIdentifierMap"/></param>
        /// <returns>A newly created <see cref="DstController.ExternalIdentifierMap"/></returns>
        ExternalIdentifierMap CreateExternalIdentifierMap(string newName);

        /// <summary>
        /// Adds one correspondance to the <see cref="IdCorrespondences"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        void AddToExternalIdentifierMap(Guid internalId, string externalId);

        /// <summary>
        /// Adds mapping configurations used to detect the not used ones in the mapping process
        /// </summary>
        /// <param name="correspondences">The <see cref="IEnumerable{IdCorrespondence}"/> from which store the current mapping</param>
        public void AddPreviousIdCorrespondances(IEnumerable<IdCorrespondence> correspondences);

        /// <summary>
        /// Remove existing mapping information
        /// </summary>
        void CleanCurrentMapping();

        /// <summary>
        /// Remove existing <see cref="ExternalIdentifierMap"/> and <see cref="IdCorrespondences"/> data
        /// </summary>
        void ResetExternalMappingIdentifier();

        /// <summary>
        /// Map the provided object using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dst3DPart">The <see cref="StepTasRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        void Map(StepTasRowViewModel dst3DPart);

        /// <summary>
        /// Time (milliseconds) consumed by the last succesull transfer
        /// </summary>
        long TransferTime { get; }

        /// <summary>
        /// Transfers the mapped parts to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        Task Transfer();
    }
}
