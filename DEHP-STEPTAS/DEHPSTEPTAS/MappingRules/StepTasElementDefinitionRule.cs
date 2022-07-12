// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Step3DPartToElementDefinitionRule" company="Open Engineering S.A.">
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

namespace DEHPSTEPTAS.MappingRules
{
    using Autofac;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;
    using DEHPSTEPTAS.DstController;
    using DEHPSTEPTAS.Services.DstHubService;
    using DEHPSTEPTAS.ViewModel.Rows;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.ExceptionServices;

    /// <summary>
    /// The <see cref="StepTasElementDefinitionRule"/> is a <see cref="IMappingRule"/> 
    /// for the <see cref="MappingEngine"/>.
    /// 
    /// That takes a <see cref="List{T}"/> of <see cref="StepTasRowViewModel"/> as input 
    /// and outputs a E-TM-10-25 <see cref="ElementDefinition"/>.
    /// </summary>
    public class StepTasElementDefinitionRule : MappingRule<List<StepTasRowViewModel>, (Dictionary<ParameterOrOverrideBase, MappedParameterValue>, List<ElementBase>)>
    {
        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController = AppContainer.Container.Resolve<IHubController>();

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService = AppContainer.Container.Resolve<IDstHubService>();

        /// <summary>
        /// The <see cref="List{ElementBase}>"/> that needs to be updated 
        /// before the transfer to the Hub.
        /// </summary>
        private readonly List<ElementBase> targetSourceElementBase = new List<ElementBase>();

        /// <summary>
        /// Holds a <see cref="Dictionary{TKey,TValue}"/> of <see cref="ParameterOrOverrideBase"/> and <see cref="NodeId.Identifier"/>
        /// </summary>
        private readonly Dictionary<ParameterOrOverrideBase, MappedParameterValue> parametersMappingInfo = new Dictionary<ParameterOrOverrideBase, MappedParameterValue>();

        /// <summary>
        /// The current <see cref="DomainOfExpertise"/>
        /// </summary>
        private DomainOfExpertise owner;

        /// <summary>
        /// Holds the current processing <see cref="StepTasRowViewModel"/> new element name
        /// </summary>
        private string dstNewElementDefinitionName;

        /// <summary>
        /// Holds the current processing <see cref="StepTasRowViewModel"/> element name
        /// </summary>
        private string dstElementName;

        /// <summary>
        /// Holds the current processing <see cref="StepTasRowViewModel"/> parameter name
        /// </summary>
        private string dstParameterName;

        /// <summary>
        /// Transforms a <see cref="List{T}"/> of <see cref="StepTasRowViewModel"/> into an <see cref="ElementDefinition"/>
        /// </summary>
        /// <param name="input">The <see cref="List{T}"/> of <see cref="StepTasRowViewModel"/> to transform</param>
        /// <returns>An <see cref="List{ElementDefinition}"/> as the top level <see cref="Thing"/> with changes</returns>
        public override (Dictionary<ParameterOrOverrideBase, MappedParameterValue>, List<ElementBase>) Transform(List<StepTasRowViewModel> input)
        {
            try
            {
                this.dstController = AppContainer.Container.Resolve<IDstController>();

                this.targetSourceElementBase.Clear();
                this.parametersMappingInfo.Clear();

                this.owner = this.hubController.CurrentDomainOfExpertise;

                foreach (var part in input)
                {
                    // Transformation is as following:
                    // - The STEP part information is stored in a ComposedParameterType
                    // - Having parameter for:
                    //   + Product Definition (name, id, type)
                    //   + Assembly Usage, or Relation (label, id)
                    //   + Uuid of the file in the DomainFileStore (known only at Transfer time)
                    //
                    this.logger.Info($"Processing MappingRule for: {part.Description}");

                    // Default values
                    this.dstElementName = part.ElementName;
                    this.dstParameterName = part.ParameterName;
                    this.dstNewElementDefinitionName = part.NewElementDefinitionName;
                    if (part.SelectedElementDefinition is null)
                    {
                        part.SelectedElementDefinition = this.GetElementDefinition();

                        if (part.SelectedElementDefinition.Iid != Guid.Empty)
                        {
                            // When the ED was automatically selected from the Rule,
                            // set also the expected parameter (if does exist, it rests as null)
                            part.SelectedParameter = part.SelectedElementDefinition.Parameter
                                .FirstOrDefault(x => this.dstHubService.IsSTEPTasParameterType(x.ParameterType));
                        }
                    }

                    this.UpdateValueSetsFromElementDefinition(part);

                    if (part.SelectedElementUsages.Any())
                    {
                        this.UpdateValueSetsFromElementUsage(part);
                    }
                    

                    part.SetMappedStatus();
                }

                // When changes can be also performed in other things
                // (i.e. EU, Parameters, etc.) only the top thing in the 
                // hierarchy is returned, the update will call
                // CreateOrUpdate for all its related things. Then ElementBase
                // is the only required thing to return.
                //
                // The parametersMappingInfo is the related information to the 
                // target parameter, we need this to remember what whas changed and
                // who produces it. In addition, this DST requires to know the 
                // FileRevision of the uploaded STEP file.
                return (this.parametersMappingInfo, this.targetSourceElementBase);
            }
            catch (Exception exception)
            {
                this.logger.Error(exception);
                this.logger.Error($"Mapping Rule for Step3DRowViewModel failed: {exception.Message}");
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }
        }

        /// <summary>
        /// Updates the parameters overrides from the selected <see cref="ElementUsage"/>s
        /// </summary>
        /// <param name="part">The current <see cref="StepTasRowViewModel"/></param>
        private void UpdateValueSetsFromElementUsage(StepTasRowViewModel part)
        {
            foreach (var elementUsage in part.SelectedElementUsages)
            {
                this.logger.Info($"Processing MappingRule for ElementUsage: {elementUsage.Name} [{elementUsage.Iid}]");

                this.targetSourceElementBase.Add(elementUsage);

                ParameterOverride parameterOverride;
                
                parameterOverride = elementUsage.ParameterOverride.FirstOrDefault(x => this.dstHubService.IsSTEPTasParameterType(x.ParameterType));

                                    
                                   

                if (parameterOverride is { })
                {
                    this.logger.Debug($"parameterOverride {parameterOverride?.Iid}");
                    this.UpdateValueSet(part, parameterOverride);
                }
                else
                {
                    this.logger.Error($"No parameterOverride defined --> UpdateValueSet() not called");
                }

                this.AddToExternalIdentifierMap(elementUsage.ElementDefinition.Iid, this.dstElementName);
                this.AddToExternalIdentifierMap(elementUsage.Iid, this.dstElementName);
            }
        }

        /// <summary>
        /// Gets or Creates an <see cref="ElementDefinition"/> if it does not exist yet
        /// </summary>
        /// <returns>An <see cref="ElementDefinition"/>. New <see cref="ElementDefinition"/> is identified as <see cref="Guid.Empty"/></returns>
        private ElementDefinition GetElementDefinition()
        {
            // Check if already exists in the hub (owned by owned by the current domain of expertise)
            if (this.hubController.OpenIteration.Element
                .Where(x => x.Owner.Iid == this.owner.Iid)
                .FirstOrDefault(x => x.Name == this.dstNewElementDefinitionName) is { } elementDefinition)
            {
                this.logger.Info($"Creating new ElementDefinition '{this.dstNewElementDefinitionName}' found that an element already exists");
                return elementDefinition;
            }

            this.logger.Info($"Creating new ElementDefinition '{this.dstNewElementDefinitionName}'");

            return this.Bake<ElementDefinition>(x =>
            {
                x.Name = this.dstNewElementDefinitionName;
                x.ShortName = this.dstNewElementDefinitionName.Replace(" ", String.Empty);
                x.Owner = this.owner;
                x.Container = this.hubController.OpenIteration;
            });
        }

        /// <summary>
        /// Updates the parameters overrides from the selected <see cref="ElementDefinition"/>s
        /// </summary>
        /// <param name="part">The current <see cref="StepTasRowViewModel"/></param>
        private void UpdateValueSetsFromElementDefinition(StepTasRowViewModel part)
        {
            this.logger.Info($"Processing MappingRule for ElementDefinition: {part.SelectedElementDefinition.Name} [{part.SelectedElementDefinition.Iid}]");

            this.targetSourceElementBase.Add(part.SelectedElementDefinition);

            this.AddsValueSetToTheSelectectedParameter(part);
            this.AddToExternalIdentifierMap(part.SelectedElementDefinition.Iid, this.dstElementName);
        }

        /// <summary>
        /// Adds the selected values to the corresponding valueset of the destination parameter
        /// </summary>
        /// <param name="part">The input part</param>
        private void AddsValueSetToTheSelectectedParameter(StepTasRowViewModel part)
        {
            if (part.SelectedParameter is null)
            {
                if (part.SelectedParameterType is null)
                {
                    part.SelectedParameterType = this.GetStepTasParameterType();
                }

                this.logger.Info($"Creating new Parameter of type '{part.SelectedParameterType.Name}' into ElementDefinition '{part.SelectedElementDefinition.Name}'");

                part.SelectedParameter = this.Bake<Parameter>(x =>
                {
                    x.ParameterType = part.SelectedParameterType;
                    // ask how to add parameter and make it option dependent --> ParameterOverride
                    x.Owner = this.owner;
                });

                var valueSet = this.Bake<ParameterValueSet>(x =>
                {
                });

                part.SelectedParameter.ValueSet.Add(valueSet);
                part.SelectedElementDefinition.Parameter.Add(part.SelectedParameter);
            }

            this.UpdateValueSet(part, part.SelectedParameter);
        }

        /// <summary>
        /// Gets the existing parameter type or creates a new one
        /// </summary>
        /// <returns>A <see cref="ParameterType"/></returns>
        private ParameterType GetStepTasParameterType()
        {
            var rdl = this.dstHubService.GetReferenceDataLibrary();

            var parameterType = rdl.ParameterType.FirstOrDefault(x => this.dstHubService.IsSTEPTasParameterType(x));

            
            if (parameterType is null)
            {
                // NOTE: this should not happen, the DST creates required types at connection time
                this.logger.Warn("STEP Tas parameter not found, creating a new one!");

                parameterType = this.CreateCompoundParameterTypeForStepTas();
            }
            

            return parameterType;
        }

        /// <summary>
        /// Creates the <see cref="CompoundParameterType"/> for time tagged values
        /// </summary>
        /// <returns>A <see cref="CompoundParameterType"/></returns>
        /// <remarks>This method will not be called because all was created at connection time</remarks>
        [ExcludeFromCodeCoverage]
        private CompoundParameterType CreateCompoundParameterTypeForStepTas()
        {
            this.logger.Warn("STEP Tas compound parameter should be created by HubDstService instance at Connect time");

            return this.Bake<CompoundParameterType>(x =>
            {
                
                string STEP_GEOMETRY_NAME = "step tas";

                x.Name = STEP_GEOMETRY_NAME;
                x.ShortName = "step_tas";
                x.Symbol = "-";

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "name";
                        p.ParameterType = this.Bake<TextParameterType>();
                    }));


                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "path";
                        p.ParameterType = this.Bake<TextParameterType>();
                    }));

               
                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "side";
                        p.ParameterType = this.Bake<TextParameterType>();
                    }));

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "node";
                        p.ParameterType = this.Bake<TextParameterType>();
                    }));

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "type";
                        p.ParameterType = this.Bake<TextParameterType>();
                    }));
                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "description";
                        p.ParameterType = this.Bake<TextParameterType>();
                    }));
                

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                   p =>
                   {
                       p.ShortName = "stepid";
                       p.ParameterType = this.Bake<SimpleQuantityKind>(
                           d =>
                           {
                               d.Symbol = "#";
                               // d.DefaultScale = ?
                               // d.PossibleScale = new List<MeasurementScale> { ? }
                           }
                           );
                   }));


                x.Component.Add(this.Bake<ParameterTypeComponent>(
                   p =>
                   {
                       p.ShortName = "source";
                       p.ParameterType = this.Bake<TextParameterType>();
                   }));


            });
        }
        

        /// <summary>
        /// Initializes a new <see cref="Thing"/> of type <typeparamref name="TThing"/>
        /// </summary>
        /// <typeparam name="TThing">The <see cref="Type"/> from which the constructor is invoked</typeparam>
        /// <returns>A <typeparamref name="TThing"/> instance</returns>
        private TThing Bake<TThing>(Action<TThing> initialize = null) where TThing : Thing, new()
        {
            var tThingInstance = Activator.CreateInstance(typeof(TThing), Guid.Empty, this.hubController.Session.Assembler.Cache, new Uri(this.hubController.Session.DataSourceUri)) as TThing;
            initialize?.Invoke(tThingInstance);
            return tThingInstance;
        }

        /// <summary>
        /// Updates the correct value set
        /// </summary>
        /// <param name="part">The <see cref="VariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Parameter"/></param>
        private void UpdateValueSet(StepTasRowViewModel part, ParameterOrOverrideBase parameter)
        {
            var valueSet = (ParameterValueSetBase)parameter.QueryParameterBaseValueSet(part.SelectedOption, part.SelectedActualFiniteState);

            var optionName = part.SelectedOption is null ? "null" : part.SelectedOption.Name;
            var stateName = part.SelectedActualFiniteState is null ? "null" : part.SelectedActualFiniteState.Name;
            this.logger.Debug($"ParameterValueSet {valueSet.Iid} for Option: {optionName}, State: {stateName}");

            this.UpdateComputedValueSet(part, parameter, valueSet);

        }

        /// <summary>
        /// Updates the Computed <see cref="ParameterValueSetBase"/> <see cref="ValueArray{T}"/>
        /// </summary>
        /// <param name="part">The <see cref="StepTasRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Thing"/> <see cref="Parameter"/> or <see cref="ParameterOverride"/></param>
        /// <param name="valueSet">The <see cref="ParameterValueSetBase"/></param>
        private void UpdateComputedValueSet(StepTasRowViewModel part, ParameterOrOverrideBase parameter, ParameterValueSetBase valueSet)
        {
            this.logger.Debug($"Updating Parameter {parameter.Iid} for ValueSet {valueSet.Iid}");

            ParameterBase paramBase = (ParameterBase)parameter;
            var paramType = paramBase.ParameterType;

            if (paramType is CompoundParameterType p)
            {
                var valuearray = valueSet.Computed;

                if (valuearray.Count == 0)
                {
                    // New parameter does not contain ValueArray with the expected dimmension,
                    // they are filled by the server side, then it is necessary to create the
                    // expected content here.

                    this.logger.Debug($"Computed ValueArray is empty (expected on new parameters) --> initializing to CompoundParameterType.NumberOfValues={p.NumberOfValues}");

                    var values = new List<string>(p.NumberOfValues);
                    foreach (var _ in System.Linq.Enumerable.Range(0, p.NumberOfValues))  // SPA: Why is it soo complex? 
                    {
                        values.Add("-");
                    }

                    valueSet.Computed = new ValueArray<string>(values);
                    valueSet.Manual = new ValueArray<string>(values);
                    valueSet.Reference = new ValueArray<string>(values);
                    valueSet.Formula = new ValueArray<string>(values);

                    valuearray = valueSet.Computed;
                }

                UpdateValueArrayForCompoundParameterType(part, parameter, p, valuearray);

                valueSet.ValueSwitch = ParameterSwitchKind.COMPUTED;

                this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);
                
                if (part.SelectedOption is { } option)
                {
                    this.AddToExternalIdentifierMap(option.Iid, this.dstParameterName);
                }

                if (part.SelectedActualFiniteState is { } state)
                {
                    this.AddToExternalIdentifierMap(state.Iid, this.dstParameterName);
                }
            }
            else
            {
                this.logger.Error($"Parameter is not of CompoundParameterType");
            }
        }

        /// <summary>
        /// Update <see cref="CompoundParameterType"/> <see cref="ValueArray{string}"/>
        /// </summary>
        /// <param name="part">The <see cref="StepTasRowViewModel"/></param>
        /// <param name="compoundParameter">The <see cref="CompoundParameterType"/></param>
        /// <param name="valuearray">The <see cref="ValueArray{string}"/></param>
        /// <remarks>
        /// Creates a <seealso cref="StepTasTargetSourceParameter"/> entry when a
        /// <see cref="ParameterTypeComponent"/> named "source" is present in the <paramref name="compoundParameter"/>.
        /// </remarks>
        private void UpdateValueArrayForCompoundParameterType(StepTasRowViewModel part, ParameterOrOverrideBase parameter, CompoundParameterType compoundParameter, ValueArray<string> valuearray)
        {
            // Component is an OrderedItemList, and the order could be 
            // changed externally by modifyind the ParameterType definition,
            // then do the set the value based on component's name

            int index = 0;
            foreach (ParameterTypeComponent component in compoundParameter.Component)
            {
                switch (component.ShortName)
                {
                    case "name": valuearray[index++] = $"{part.Name}"; break;
                    case "path": valuearray[index++] = $"{part.Path}"; break;
                    case "type": valuearray[index++] = $"{part.Type}"; break;
                    

                    case "side": valuearray[index++] = $"{part.Sides}"; break;
                    case "description": valuearray[index++] = $"{part.Description}"; break;
                    case "stepid": valuearray[index++] = $"{part.StepId}"; break;
                    case "node": valuearray[index++] = $"{part.Nodes}"; break;
                    

                    case "source":
                        {
                            // NOTE: FileRevision.Iid will be known at Transfer time
                            //       store the current index to know which possition corresponds
                            //       to the source (avoid searching it again)
                            //this.parametersMappingInfo[part.SelectedParameter] = new MappedParameterValue(part, valuearray, index);

                            this.logger.Debug($"Updating map parametersMappingInfo[{parameter.Iid}]");
                            this.parametersMappingInfo[parameter] = new MappedParameterValue(part, valuearray, index);
                            valuearray[index++] = "";
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="externalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        private void AddToExternalIdentifierMap(Guid internalId, string externalId)
            => this.dstController.AddToExternalIdentifierMap(internalId, externalId);
    }
}
