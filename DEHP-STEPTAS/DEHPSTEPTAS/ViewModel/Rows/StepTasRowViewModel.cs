﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StepTasRowViewModel.cs" company="Open Engineering S.A.">
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
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using ReactiveUI;
    //using STEP3DAdapter;
    using System;

    /// <summary>
    /// The <see cref="StepTasRowViewModel"/> is the node in the HLR tree structure.
    /// 
    /// <seealso cref="DstObjectBrowserViewModel"/>
    /// <seealso cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/>
    /// </summary>
    public class StepTasRowViewModel : ReactiveObject
    {
        

        internal StepTasRowData stepRowData;

        #region HLR Tree Indexes

        /// <summary>
        /// Auxiliary index for tree control.
        /// </summary>
        /// <remarks>
        /// It is an unique value in the <see cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/> context.
        /// </remarks>

        #endregion
        

        #region Step Tas  information Fields

        /// <summary>
        /// Gets the part instance name
        /// </summary>
        /// <remarks>
        /// The instance is the part name and the usage id <see cref="STNodeRelation.id"/>
        /// representing a unique string for the part.
        /// </remarks>
        public string InstancePath { get => stepRowData.Path+"/"+stepRowData.Name; }

        /// <summary>
        /// Get full path of compised part instance names
        /// </summary>
        public string Path { get => stepRowData.Path; }

        
        /// <summary>
        /// Get Part name.
        /// </summary>
        public string Name { get => stepRowData.Name; }

        public string Nodes { get => stepRowData.ThermalNodes; }

        /// <summary>
        /// Get short entity type.
        /// </summary>
        public string Type { get => stepRowData.Type; }

        public string Sides { get => stepRowData.Sides; }

        /// <summary>
        /// Get STEP entity file Id.
        /// </summary>
        public String StepId { get => stepRowData.StepId; }

        public int ID { get => stepRowData.ID; }

        public int ParentID { get => stepRowData.ParentID;}
        public string MaterialName { get=> stepRowData.MaterialName; }
        /// <summary>
        /// Compose a reduced description of the <see cref="STNode"/>
        /// </summary>
        public string Description
        {
            get => $"{stepRowData.Description}";
        }

        /// <summary>
        /// Gets a label of association
        /// </summary>        
        

        /** <summary>return the corresponding Node is the Tasnode is a Surface</summary>
         */
       
       
       

        #endregion

        #region Mapping parameters

        /// <summary>
        /// Backing field for <see cref="SelectedOption"/>
        /// </summary>
        private Option selectedOption;

        /// <summary>
        /// Gets or sets the selected <see cref="Option"/>
        /// </summary>
        public Option SelectedOption
        {
            get => this.selectedOption;
            set => this.RaiseAndSetIfChanged(ref this.selectedOption, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedParameter"/>
        /// </summary>
        private Parameter selectedParameter;

        /// <summary>
        /// Gets or sets the selected <see cref="Parameter"/>
        /// </summary>
        public Parameter SelectedParameter
        {
            get => this.selectedParameter;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameter, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedParameterType"/>
        /// </summary>
        private ParameterType selectedParameterType;

        /// <summary>
        /// Gets or sets the selected <see cref="Parameter"/>
        /// </summary>
        public ParameterType SelectedParameterType
        {
            get => this.selectedParameterType;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameterType, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedElementDefinition"/>
        /// </summary>
        private ElementDefinition selectedElementDefinition;

        /// <summary>
        /// Gets or sets the selected <see cref="ElementDefinition"/>
        /// </summary>
        public ElementDefinition SelectedElementDefinition
        {
            get => this.selectedElementDefinition;
            set => this.RaiseAndSetIfChanged(ref this.selectedElementDefinition, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedActualFiniteState"/>
        /// </summary>
        private ActualFiniteState selectedActualFiniteState;

        /// <summary>
        /// Gets or sets the selected <see cref="ActualFiniteState"/>
        /// </summary>
        public ActualFiniteState SelectedActualFiniteState
        {
            get => this.selectedActualFiniteState;
            set => this.RaiseAndSetIfChanged(ref this.selectedActualFiniteState, value);
        }

        /// <summary>
        /// Gets or sets the collection of selected <see cref="ElementUsage"/>s
        /// </summary>
        public ReactiveList<ElementUsage> SelectedElementUsages { get; set; } = new ReactiveList<ElementUsage>();

        /// <summary>
        /// Gets or sets the mapping configurations
        /// </summary>
        public ReactiveList<IdCorrespondence> MappingConfigurations { get; set; } = new ReactiveList<IdCorrespondence>() { ChangeTrackingEnabled = true };

        /// <summary>
        /// Cleans all the "Selected" fields
        /// </summary>
        public void CleanSelections()
        {
            this.SelectedElementDefinition = null;
            this.SelectedParameter = null;
            this.SelectedParameterType = null; //TODO: remove unsed
            this.SelectedElementUsages.Clear();
            this.SelectedOption = null;
            this.SelectedActualFiniteState = null;
        }

        /// <summary>
        /// Gets or sets the name when creating a new <see cref="ElementDefinition"/>
        /// </summary>
        public string NewElementDefinitionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets this represented ElementName
        /// </summary>
        public string ElementName => this.Name;

        /// <summary>
        /// Gets this reprensented ParameterName
        /// </summary>
        public string ParameterName => $"{this.Name} tas reference";

        /// <summary>
        /// Enumeration of the possible mapping status of the current part
        /// </summary>
        public enum MappingStatusType
        {
            /// <summary>
            /// Noting refers to no <see cref="IdCorrespondence"/> information about mapping for the part
            /// </summary>
            Nothing,

            /// <summary>
            /// WithConfiguration refers to a <see cref="IdCorrespondence"/> entries not yet used for the mapping process
            /// </summary>
            Configured,

            /// <summary>
            /// Mapped refers to an already mapped part, independently of current <see cref="IdCorrespondence"/> defined
            /// </summary>
            Mapped,

            /// <summary>
            /// Transfered refers to an already transfered part, independently of current <see cref="IdCorrespondence"/> defined
            /// </summary>
            Transfered
        }

        /// <summary>
        /// Gets the mapping status code
        /// </summary>
        public MappingStatusType MappingStatus { get; private set; }

        /// <summary>
        /// Backing field for <see cref="MappingStatusMessage"/>
        /// </summary>
        private string mappingStatusMessage;

        /// <summary>
        /// Gets the <see cref="MappingStatus"/> string representation
        /// </summary>
        public string MappingStatusMessage
        {
            get => this.mappingStatusMessage;
            private set => this.RaiseAndSetIfChanged(ref this.mappingStatusMessage, value);
        }

        /// <summary>
        /// Sets the <see cref="MappingStatus"/> and updates the <see cref="MappingStatusMessage"/>
        /// </summary>
        /// <param name="mappingStatusType"></param>
        private void SetMappingStatus(MappingStatusType mappingStatusType)
        {
            this.MappingStatus = mappingStatusType;

            this.MappingStatusMessage = this.MappingStatus switch
            {
                MappingStatusType.Nothing => string.Empty,
                MappingStatusType.Configured => "Configured",
                MappingStatusType.Mapped => "Mapped",
                MappingStatusType.Transfered => "Transfered",
                _ => string.Empty,// Not expected
            };
        }

        /// <summary>
        /// Update the <see cref="MappingStatus"/> according to current situation
        /// </summary>
        /// <remarks>
        /// The <see cref="MappingStatusType.Nothing"/> or <see cref="MappingStatusType.Configured"/> status can
        /// be changed between them at any time using the <see cref="MappingConfigurations"/> content.
        /// 
        /// Once the status is set to <see cref="MappingStatusType.Mapped"/> or <see cref="MappingStatusType.Transfered"/>
        /// the <see cref="MappingStatusType.Nothing"/> or <see cref="MappingStatusType.Configured"/> status cannot be set.
        /// 
        /// The <see cref="MappingStatusType.Mapped"/> status should remain untouchable until the transfer is executed
        /// or the current pending mappings are removed.
        /// 
        /// <seealso cref="ResetMappingStatus"/>
        /// </remarks>
        public void UpdateMappingStatus()
        {
            // On these no change is informed
            if (this.MappingStatus >= MappingStatusType.Mapped)
            {
                return;
            }

            // Only possible change
            if (this.MappingConfigurations.Count == 0)
            {
                this.SetMappingStatus(MappingStatusType.Nothing);
            }
            else
            {
                this.SetMappingStatus(MappingStatusType.Configured);
            }
        }

        /// <summary>
        /// Sets the <see cref="MappingStatusType.Mapped"/> status
        /// </summary>
        public void SetMappedStatus()
        {
            this.SetMappingStatus(MappingStatusType.Mapped);
        }

        /// <summary>
        /// Sets the <see cref="MappingStatusType.Transfered"/> status
        /// </summary>
        public void SetTransferedStatus()
        {
            this.SetMappingStatus(MappingStatusType.Transfered);
        }

        /// <summary>
        /// Sets the <see cref="MappingStatusType.Nothing"/> status
        /// independently of  <see cref="MappingConfigurations"/>
        /// </summary>
        public void ResetMappingStatus()
        {
            this.SetMappingStatus(MappingStatusType.Nothing);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="part">Reference to <see cref="STNode"/> entity in the <see cref="StepTasFile"/></param>
        /// <param name="relation">Reference to <see cref="STNode"/> entity in the <see cref="StepTasFile"/></param>
      /*  public Step3DRowViewModel(STNode part, STNodeRelation relation, string parentPath = "")
        {
            //this.part = part;
            //this.relation = relation;

            this.InstanceName = string.IsNullOrWhiteSpace(this.RelationLabel) ? this.Name : $"{this.Name} ({this.RelationLabel})";
            this.InstancePath = string.IsNullOrWhiteSpace(parentPath) ? this.InstanceName : $"{parentPath}.{this.InstanceName}";

            this.ResetMappingStatus();

            this.MappingConfigurations.ItemChanged.Subscribe(x => this.UpdateMappingStatus());
        }
      */


        public StepTasRowViewModel(StepTasRowData rowdata)
        {
            this.stepRowData = rowdata;
            //this.part = part;
            //this.relation = relation;

            //this.InstanceName = string.IsNullOrWhiteSpace(this.RelationLabel) ? this.Name : $"{this.Name} ({this.RelationLabel})";
            //this.InstancePath = string.IsNullOrWhiteSpace(parentPath) ? this.InstanceName : $"{parentPath}.{this.InstanceName}";

            this.ResetMappingStatus();

            this.MappingConfigurations.ItemChanged.Subscribe(x => this.UpdateMappingStatus());
        }


        #endregion
    }
}
