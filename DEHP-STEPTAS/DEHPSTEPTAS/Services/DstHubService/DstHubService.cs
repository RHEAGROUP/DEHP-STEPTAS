// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstHubService.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPTAS.Services.DstHubService
{
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal.Operations;
    using DEHPCommon.HubController.Interfaces;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using File = CDP4Common.EngineeringModelData.File;

    /// <summary>
    /// Helper service supporting the work performed by the <see cref="DstController"/> and
    /// also required by different components.
    /// </summary>
    public class DstHubService : IDstHubService
    {
        // File Constants
        private static readonly string APPLICATION_STEP_NAME = "application/step";
        private static readonly string[] APPLICATION_STEP_EXTENSIONS = { "step", "stp" };

        // Parameter Constants
        private static readonly string STEP_ID_UNIT_NAME = "step id";
        private static readonly string STEP_TAS_ID_NAME = "step id";
        private static readonly string STEP_TAS_LABEL_NAME = "step name";
        private static readonly string STEP_TAS_PATH = "step path";
        private static readonly string STEP_TAS_SIDE = "step side";
        private static readonly string STEP_TAS_NODE="step node";
        
        private static readonly string STEP_TAS_DESCRIPTION = "step tas description";
        private static readonly string STEP_TAS_FILE_REF_NAME = "step file reference";
        private static readonly string STEP_TAS_NAME = "step tas";
        private static readonly string STEP_TAS_SHORTNAME = "step_tas";
        private static readonly string STEP_TAS_REFERENCE_NAME= "step tas reference";

        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="DEHPCommon.IHubController"/> instance
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// Constructor
        /// </summary>
        public DstHubService(IHubController hubController)
        {
            this.hubController = hubController;
        }

        /// <summary>
        /// Checks/creates all the DST required data is in the Hub.
        /// 
        /// Creates any missing data:
        /// - FileTypes
        /// - ParameterTypes
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task CheckHubDependencies()
        {
            if (this.hubController.OpenIteration is null) return;

            await this.CheckDomainFileStore();  // SPA: Creation date was not initialized
            await this.CheckFileTypes();
            await this.CheckParameterTypes();

     


        }

        /// <summary>
        /// Finds the DST <see cref="CDP4Common.EngineeringModelData.File"/> in the Hub
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.File"/> or null if does not exist</returns>
        public File FindFile(string filePath)
        {
            if (filePath is null || this.hubController.OpenIteration is null)
            {
                return null;
            }

            var name = Path.GetFileNameWithoutExtension(filePath);
            
            var currentDomainOfExpertise = hubController.CurrentDomainOfExpertise;
            if (currentDomainOfExpertise is null) return null;
            var dfStore = hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner.Iid == currentDomainOfExpertise.Iid);  // SPA: DomainFileStore seems to be  associated to a domain of expertise

            var file = dfStore?.File.FirstOrDefault(x => this.IsSTEPTasFileType(x.CurrentFileRevision) && x.CurrentFileRevision.Name == name); // SPA: We get the File (CPD4 on the Hub

            return file;
        }

        /// <summary>
        /// Finds the <see cref="CDP4Common.EngineeringModelData.FileRevision"/> from string <see cref="System.Guid"/>
        /// </summary>
        /// <param name="guid">The string value of an <see cref="System.Guid"/></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.FileRevision"/> or null if does not exist</returns>
        public FileRevision FindFileRevision(string guid)
        {
            // NOTE: HubController.GetThingById() does not contemplates file revisions, only FileType.
            //       Local inspection at each File entry is required.

            var currentDomainOfExpertise = this.hubController.CurrentDomainOfExpertise;
            var dfStore = this.hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner.Iid == currentDomainOfExpertise.Iid);

            if (dfStore is null)
            {
                return null;
            }

            var targetIid = new System.Guid(guid);

            foreach (var file in dfStore.File)
            {
                var fileRevision = file.FileRevision.FirstOrDefault(x => x.Iid == targetIid);
                if (fileRevision is { })
                {
                    return fileRevision;
                }
            }

            return null;
        }

        /// <summary>
        /// First compatible STEP <see cref="FileType"/> of a <see cref="FileRevision"/>
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/></param>
        /// <returns>First compatible FileType or null if not</returns>
        public FileType FirstSTEPFileType(FileRevision fileRevision)
        {
            var fileType = fileRevision.FileType.FirstOrDefault(t => (t.Name == APPLICATION_STEP_NAME));
            return fileType;
        }

        /// <summary>
        /// Finds all the revisions for DST files
        /// </summary>
        /// <returns>The <see cref="List{FileRevision}"/> for only current file revision</returns>
        public List<FileRevision> GetFileRevisions()
        {
            if (this.hubController.OpenIteration is null)
            {
                return new List<FileRevision>();
            }

            var currentDomainOfExpertise = hubController.CurrentDomainOfExpertise;
            this.logger.Debug($"Domain of Expertise: { currentDomainOfExpertise.Name }");

            var dfStore = hubController.OpenIteration.DomainFileStore.FirstOrDefault(d => d.Owner.Iid == currentDomainOfExpertise.Iid);
            if( dfStore is null) return new List<FileRevision>();
            this.logger.Debug($"Domain File Store: {dfStore.Name} (Rev: {dfStore.RevisionNumber})");

            var revisions = new List<FileRevision>();

            this.logger.Debug($"Files Count: {dfStore.File.Count}");
            foreach (var f in dfStore.File)
            {
                var cfrev = f.CurrentFileRevision;

                if (this.IsSTEPTasFileType(cfrev))
                {
                    revisions.Add(cfrev);
                }
            }

            return revisions;
        }

        /// <summary>
        /// Checks if it is a STEP file type
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>True if is a STEP file</returns>
        public bool IsSTEPTasFileType(FileRevision fileRevision)
        {
            var fileType = this.FirstSTEPFileType(fileRevision);

            return !(fileType is null);
        }

        /// <summary>
        /// Checks if a parameter is compatible with STEPTas mapping
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>True if it is a candidate for the mapping</returns>
        public bool IsSTEPTasParameterType(ParameterType param)
        {
            if (param is CompoundParameterType &&
                param.ShortName.Equals("step_tas", StringComparison.CurrentCultureIgnoreCase)
                )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the step geometric parameter where to store a STEP-TAS part information
        /// </summary>
        /// <returns>A <see cref="ParameterType"/></returns>
        public ParameterType FindSTEPTasParameterType()
        {
            var rdl = this.GetReferenceDataLibrary();
            var parameters = rdl.ParameterType;

             var ret =parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.ShortName == STEP_TAS_SHORTNAME && !x.IsDeprecated);
            return ret;
        }

        /// <summary>
        /// Gets the <see cref="ParameterTypeComponent"/> corresponding to the source file reference
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>A <see cref="ParameterTypeComponent"/> or null if does not contain the component</returns>
        public ParameterTypeComponent FindSourceParameterType(ParameterType param)
        {
            if (param is CompoundParameterType compountParameter)
            {
                return compountParameter.Component.FirstOrDefault(x => x.ShortName == "source");
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="ReferenceDataLibrary"/> where to add DST content
        /// </summary>
        /// <returns>A <see cref="ReferenceDataLibrary"/></returns>
        public ReferenceDataLibrary GetReferenceDataLibrary()
        {
            // Different RDL could exist in the server:
            // RDL: RDL specific to CDF_generic_template --> contains 0 FileTypes
            // RDL: Generic ECSS-E-TM-10-25 Reference Data Library --> contains 28 FileTypes

            // Search From Model
            // iteration --> contained in EM --> having a EM Setup with 1 RequiredRDL
            var model = this.hubController.OpenIteration.GetContainerOfType<EngineeringModel>();
            var modelSetup = model.EngineeringModelSetup;
            var rdls = modelSetup.RequiredRdl;

            return rdls.First();  // SPA: why the first?
        }

        /// <summary>
        /// Check is DomainFileStore exists in the iteration
        /// If it does not exist, it is created.  
        ///
        /// </summary>
        private async Task CheckDomainFileStore()
        {
            // SPA: BE CAREFUL : the creation date on the Hub is not initialized


            DomainFileStore dfs = this.hubController.OpenIteration.DomainFileStore.FirstOrDefault();

            if(dfs is null)
            {
                Iteration iterCloned = hubController.OpenIteration.Clone(false);

                ThingTransaction transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(iterCloned), iterCloned);

                dfs = new DomainFileStore(Guid.NewGuid(), null, null)
                {
                    Name  = this.hubController.CurrentDomainOfExpertise.Name,
                    Owner = this.hubController.CurrentDomainOfExpertise
                };
                
                iterCloned.DomainFileStore.Add(dfs);

                transaction.CreateOrUpdate(dfs);

                await this.hubController.Write(transaction);
            }
        }


        /// <summary>
        /// Check that STEP <see cref="FileType"/> exists in the RDL
        /// 
        /// Target RDL: Generic_RDL
        /// 
        /// Two <see cref="FileType"/> are used:
        /// - application/step for .step extension
        /// - application/step for .stp extension
        /// 
        /// Adds missing <see cref="FileType"/>
        /// </summary>
        private async Task CheckFileTypes()
        {
            var rdl = this.GetReferenceDataLibrary();

            var missingExtensions = new List<string>();

            // Verify that any known STEP extension is checked
            foreach (var ext in APPLICATION_STEP_EXTENSIONS)
            {
                if (!rdl.FileType.Any(t => t.Extension == ext))
                {
                    missingExtensions.Add(ext);
                }
            }

            if (missingExtensions.Count > 0)
            {
                var thingsToWrite = new List<FileType>();

                foreach (var extension in missingExtensions)
                {
                    var fileType = new FileType(Guid.NewGuid(), null, null)
                    {   // SPA: below, you will find properties of FileType created just upper
                        Name = APPLICATION_STEP_NAME,
                        ShortName = APPLICATION_STEP_NAME,
                        Extension = extension,
                        Container = rdl
                    };

                    this.logger.Info($"Adding missing STEP FileType {APPLICATION_STEP_NAME} for .{extension}");

                    thingsToWrite.Add(fileType);
                }

                await this.hubController.CreateOrUpdate<ReferenceDataLibrary, FileType>(thingsToWrite, (r, t) => r.FileType.Add(t));
            }
            else
            {
                this.logger.Info($"All STEP FileType already available");
            }
        }

        /// <summary>
        /// Checks that STEP TAS parameters exists
        /// 
        /// Things:
        /// - MeasurementScale: Type=OrdinalScale, Name=step id, ShortName=-, Unit=1, NumberSet=NATURAL_NUMBER_SET, MinPermsibleValue=0, MinInclusive=true (indicate not known value)
        /// 
        /// - ParameterType: Type=SimpleQuantityKind, Name=step id, ShortName=step_id, Symbol=#, DefaultScale=step id
        /// - ParameterType: Type=TextParameterType, Name=step label, ShortName=step_label, Symbol=-
        /// - ParameterType: Type=TextParameterType, Name=step file reference, ShortName=step_file_reference, Symbol=-
        /// 
        /// - ParameterType: Type=CompoundParameterType, Name=step 3d geometry, ShortName=step_3d_geom, Symbol=-
        ///      component1: Name=name,           Type=step label
        ///      component2: Name=path,             Type=step label
        ///      component3: Name=type,       Type=step label
        ///      component4: Name=side, Type=step label
        ///      component5: Name=nodes,    Type=step label
        ///      component6: Name=description,    Type=step label
        ///      component7: Name=source,         Type=step file reference
        /// </summary>
        private async Task CheckParameterTypes()
        {


            // Note 1: MeasurementScale represents the VIM concept of "quantity-value scale" 
            // that is defined as "ordered set of quantity values of quantities of a given 
            // kind of quantity used in ranking, according to magnitude, quantities of that kind".
            //
            // Note 2: A MeasurementScale defines how to interpret the numerical value of a quantity 
            // or parameter. In this data model a distinction is made between a measurement scale 
            // and a measurement unit.A measurement unit is a reference quantity that defines 
            // how to interpret an interval of one on a measurement scale. A measurement scale 
            // defines in addition the kind of scale, and where necessary more characteristics 
            // to provide all information needed for mapping quantity values between different scales, 
            // as specified in the specializations of this class.

            var rdl = this.GetReferenceDataLibrary();

            var units = rdl.QueryMeasurementUnitsFromChainOfRdls();
            var scales = rdl.QueryMeasurementScalesFromChainOfRdls();
            var parameters = rdl.QueryParameterTypesFromChainOfRdls();

            

            

            MeasurementUnit oneUnit = units.OfType<SimpleUnit>().FirstOrDefault(u => u.ShortName == "1");
            MeasurementScale stepIdScale = scales.OfType<OrdinalScale>().FirstOrDefault(x => x.Name == STEP_TAS_ID_NAME && !x.IsDeprecated);
            //
            ParameterType stepIdParameter= parameters.OfType<SimpleQuantityKind>().FirstOrDefault(x => x.Name == STEP_TAS_ID_NAME && !x.IsDeprecated);
            ParameterType stepLabelParameter=parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == STEP_TAS_LABEL_NAME && !x.IsDeprecated);
            ParameterType stepFileRefParameter=parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == STEP_TAS_FILE_REF_NAME && !x.IsDeprecated);
            CompoundParameterType stepTasReferenceParameter=parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.Name == STEP_TAS_REFERENCE_NAME && !x.IsDeprecated);


            


            var rdlClone = rdl.Clone(false);  // SPA: Why do we have to work on clone?
            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(rdlClone), rdlClone); // SPA: ???

            if (!(oneUnit is SimpleUnit))
            {
                oneUnit = this.CreateUnit(transaction, rdlClone, STEP_ID_UNIT_NAME, "1");
            }

            if (stepIdScale is null)
            {
                stepIdScale = this.CreateNaturalScale(transaction, rdlClone, STEP_TAS_ID_NAME, "-", oneUnit);
            }

            if (stepIdParameter is null)
            {
                stepIdParameter = this.CreateSimpleQuantityParameterType(transaction, rdlClone, STEP_TAS_ID_NAME, "step_id", stepIdScale);
            }

            if (stepLabelParameter is null)
            {
                stepLabelParameter = this.CreateTextParameterType(transaction, rdlClone, STEP_TAS_LABEL_NAME, "step_label");
            }

            if (stepFileRefParameter is null)
            {
                stepFileRefParameter = this.CreateTextParameterType(transaction, rdlClone, STEP_TAS_FILE_REF_NAME, "step_tas_file_reference");
            }

            // Once all sub-parameters exist, compound parameter can be created
            if (stepTasReferenceParameter is null)
            {
                var entries = new List<KeyValuePair<string, ParameterType>>()
                {
                    // The Id of the step-tas entity is made out of:
                    // if the name is unique within model, it should be enough to 
                    new KeyValuePair<string, ParameterType>("name", stepLabelParameter),
                    new KeyValuePair<string, ParameterType>("path",stepIdParameter),
                    // the information concerning the sides
                      new KeyValuePair<string, ParameterType>("side", stepLabelParameter),
                    // if the nodes are statically selected they will end up here:  
                    new KeyValuePair<string, ParameterType>("node", stepLabelParameter),
                    // The following data is purely mostly informationnal.
                    new KeyValuePair<string, ParameterType>("type", stepLabelParameter),                                    
                    new KeyValuePair<string, ParameterType>("description", stepLabelParameter),
                    new KeyValuePair<string, ParameterType>("stepid", stepIdParameter),
                    // this bear the file reference
                    new KeyValuePair<string, ParameterType>("source", stepFileRefParameter)
                };

                this.CreateCompoundParameter(transaction, rdlClone, STEP_TAS_REFERENCE_NAME, "step_tas", entries);
            }



            
            // ADDED BY SPA:  for temperature results
            
            MeasurementUnit celsiusUnit = rdl.QueryMeasurementUnitsFromChainOfRdls().OfType<DerivedUnit>().FirstOrDefault(u => u.Name == "degree Celsius");
            if (!(celsiusUnit is DerivedUnit))
                celsiusUnit = this.CreateUnit(transaction, rdlClone,"degree Celsius", "°C");

            MeasurementScale celsiusScale = rdl.QueryMeasurementScalesFromChainOfRdls().OfType<RatioScale>().FirstOrDefault(x => x.Name == "degree Celsius" && !x.IsDeprecated);
            if (celsiusScale is null)
                celsiusScale = this.CreateNaturalScale(transaction, rdlClone, "degree Celsius", "°C", celsiusUnit);

            
            // Create "minimum temperature" if necessary 
            ParameterType minimumTemperaturePT = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == "minimum temperature" && !x.IsDeprecated);
            if (minimumTemperaturePT is null)
            {
                this.logger.Info("Creating \"minimum temperature\" parameter type");
                minimumTemperaturePT = this.CreateTextParameterType(transaction, rdlClone, "minimum temperature", "minimum_temperature");
            }
            // Create "maximum temperature" if necessary 
            ParameterType maximumTemperaturePT = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == "maximum temperature" && !x.IsDeprecated);
            if (maximumTemperaturePT is null)
            {
                this.logger.Info("Creating \"maximum temperature\" parameter type");
                maximumTemperaturePT = this.CreateTextParameterType(transaction, rdlClone, "maximum temperature", "maximum_temperature");
            }

            
            // SPA: TO TEST LATER!!!
            ParameterType computedTemperatureParameterSQ = parameters.OfType<SimpleQuantityKind>().FirstOrDefault(x => x.Name == "computed temperature SQ" && !x.IsDeprecated);
            if (computedTemperatureParameterSQ is null)
            {
                this.logger.Info("Creating \"computed temperature SQ\" parameter");
                computedTemperatureParameterSQ =
                    this.CreateSimpleQuantityParameterType(transaction, rdlClone, "computed temperature SQ", "computed_temperature_SQ", celsiusScale);
                computedTemperatureParameterSQ.Symbol = "°C";  // SPA: to adapt with the short name of measurement scalE,,                   
            }
            // END OF TEST


            
            SampledFunctionParameterType sampledTemperaturePT = parameters.OfType<SampledFunctionParameterType>().FirstOrDefault(x => x.Name == "sampled temperature" && !x.IsDeprecated);
            if (sampledTemperaturePT is null)
            {
                this.logger.Info("Creating \"sampled temperature\" parameter type");

                ParameterType timeParameter = parameters.OfType<SimpleQuantityKind>().FirstOrDefault(x => x.Name == "time" && !x.IsDeprecated);
                ParameterType temperatureParameter = parameters.OfType<SimpleQuantityKind>().FirstOrDefault(x => x.Name == "temperature" && !x.IsDeprecated);

                List<string> interPeriodList = new() { "" };
                CDP4Common.Types.ValueArray<string> interPeriod = new(interPeriodList);

                sampledTemperaturePT = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
                {
                    Name = "sampled temperature",
                    ShortName = "sampled_temperature",
                    Symbol = "°C",
                    InterpolationPeriod = interPeriod,
                };
                                
                var timeParamAssign = new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null);
                timeParamAssign.ParameterType = timeParameter;
                sampledTemperaturePT.IndependentParameterType.Add(timeParamAssign);
                transaction.CreateOrUpdate(timeParamAssign);

                var temperatureParamAssign = new DependentParameterTypeAssignment(Guid.NewGuid(), null, null);
                temperatureParamAssign.ParameterType = temperatureParameter;
                sampledTemperaturePT.DependentParameterType.Add(temperatureParamAssign);
                transaction.CreateOrUpdate(temperatureParamAssign);

                rdlClone.ParameterType.Add(sampledTemperaturePT);
                transaction.CreateOrUpdate(sampledTemperaturePT);
            }
            // END OF ADDED BY SPA

            if( transaction.AddedThing.Count() != 0 )   // If there is nothing to add, we do not write the transaction
            {
                try
                {
                    transaction.CreateOrUpdate(rdlClone);

                    await this.hubController.Write(transaction);    // SPA: does await has an effect?
                }
                catch (Exception exception)
                {
                    this.logger.Error(exception);
                    this.logger.Error($"Parameter(s) creation failed: {exception.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="MeasurementUnit"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <returns><see cref="MeasurementUnit"/></returns>
        private SimpleUnit CreateUnit(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName)
        {
            var newUnit = new SimpleUnit(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
            };

            this.logger.Info($"Adding Unit: {newUnit.Name} [{newUnit.ShortName}]");
            rdlClone.Unit.Add(newUnit);
            transaction.CreateOrUpdate(newUnit);

            return newUnit;
        }

        /// <summary>
        /// Creates a <see cref="MeasurementScale"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="unit">The <see cref="MeasurementUnit"/></param>
        /// <returns><see cref="MeasurementScale"/></returns>
        private OrdinalScale CreateNaturalScale(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName, MeasurementUnit unit)
        {
            var theScale = new OrdinalScale(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Unit = unit,
                NumberSet = NumberSetKind.NATURAL_NUMBER_SET,
                MinimumPermissibleValue = "0",
                IsMinimumInclusive = true, // 0 indicates not known value
            };

            this.logger.Info($"Adding Scale: {theScale.Name} [{theScale.ShortName}] Unit={theScale.Unit.Name}");

            rdlClone.Scale.Add(theScale);
            transaction.CreateOrUpdate(theScale);

            return theScale;
        }

        /// <summary>
        /// Creates a <see cref="SimpleQuantityKind"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="scale"><see cref="MeasurementScale"/> set as <see cref="QuantityKind.DefaultScale"/> and <see cref="QuantityKind.PossibleScale"/></param>
        /// <returns><see cref="SimpleQuantityKind"/></returns>
        private SimpleQuantityKind CreateSimpleQuantityParameterType(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName, MeasurementScale scale)
        {
            var theParameter = new SimpleQuantityKind(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Symbol = "#",
                DefaultScale = scale,
                PossibleScale = new List<MeasurementScale> { scale },
            };

            this.logger.Info($"Adding Parameter: {theParameter.Name} [{theParameter.ShortName}]");

            rdlClone.ParameterType.Add(theParameter);
            transaction.CreateOrUpdate(theParameter);

            return theParameter;
        }

        /// <summary>
        /// Creates a <see cref="TextParameterType"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <returns><see cref="TextParameterType"/></returns>
        private TextParameterType CreateTextParameterType(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName)
        {
            var theParameter = new TextParameterType(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Symbol = "-",
            };

            this.logger.Info($"Adding Parameter: {theParameter.Name} [{theParameter.ShortName}]");

            rdlClone.ParameterType.Add(theParameter);
            transaction.CreateOrUpdate(theParameter);

            return theParameter;
        }

        /// <summary>
        /// Creates a <see cref="CompoundParameterType"/>
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/></param>
        /// <param name="rdlClone">The <see cref="ReferenceDataLibrary"/> container</param>
        /// <param name="name">The name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="shortName">The short name of the <see cref="CDP4Common.CommonData.Thing"/></param>
        /// <param name="entries">The <see cref="List{T}"/> with the components to be added</param>
        private void CreateCompoundParameter(ThingTransaction transaction, ReferenceDataLibrary rdlClone, string name, string shortName, List<KeyValuePair<string, ParameterType>> entries)
        {
            // SPA: How are aatached the components to the CompoundParameter

            var theParameter = new CompoundParameterType(Guid.NewGuid(), null, null)
            {
                Name = name,
                ShortName = shortName,
                Symbol = "-",
            };

            foreach (var item in entries)
            {
                var component = new ParameterTypeComponent(Guid.NewGuid(), null, null)
                {
                    ShortName = item.Key,
                    ParameterType = item.Value
                };

                theParameter.Component.Add(component);
                transaction.CreateOrUpdate(component);
            }

            this.logger.Info($"Adding CompoundParameter: {theParameter.Name} [{theParameter.ShortName}]");

            rdlClone.ParameterType.Add(theParameter);
            transaction.CreateOrUpdate(theParameter);
        }


        [ExcludeFromCodeCoverage]
        private async Task TestHUBMethod()
        {
            var ListOfElements = this.hubController.OpenIteration.Element;     // We consider all ElementDefinition : no filtering for the one containing step_tas_reference

            NLog.LogManager.GetCurrentClassLogger().Info("ListOfElements.Count = " + ListOfElements.Count);


            // Loop over all element ED and print parameters names 
            foreach (ElementDefinition elem in ListOfElements)
            {

                NLog.LogManager.GetCurrentClassLogger().Info("ED Name : " + elem.Name);

                foreach (Parameter p in elem.Parameter)
                {
                    int nbValues = p.ParameterType.NumberOfValues;
                    NLog.LogManager.GetCurrentClassLogger().Info(p.ParameterType.Name + ", " + p.ParameterType.ShortName + ", " + nbValues);
                }
            }


            //var elemUsagesInED=elem.ContainedElement;   // test SPA to get the Element Usages in the Element Definition
            //var elemUsagesOfED = elem.ReferencingElementUsages(); // test


            // Loop over all element definition and print related element usage 
            foreach (ElementDefinition elem in ListOfElements)
            {
                NLog.LogManager.GetCurrentClassLogger().Info("----- Considering Element Name  : " + elem.Name);
                var elemUsagesOfED = elem.ReferencingElementUsages(); // test)
                foreach (ElementUsage elemUse in elemUsagesOfED)
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("         ElementUsage Name : " + elemUse.Name);
                }
            }






            // look for ED "OBC" and print content of SampledFunctionParameter
            foreach (ElementDefinition elem in ListOfElements)
            {
                if (elem.Name == "OBC")
                {
                    foreach (Parameter p in elem.Parameter)
                    {
                        if (p.ParameterType is SampledFunctionParameterType)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Info("ElementDefinition Name: " + elem.Name);

                            NLog.LogManager.GetCurrentClassLogger().Info("     " + p.UserFriendlyName + " :");

                            var stateDep = p.StateDependence;

                            if (stateDep is not null)
                            {

                                foreach (var state in stateDep.ActualState)
                                {
                                    NLog.LogManager.GetCurrentClassLogger().Info("       " + state.Name + " :");

                                    var value = p.QueryParameterBaseValueSet(null, state);

                                    if (value is not null)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Info("         value.ToString : " + value.ToString());
                                        NLog.LogManager.GetCurrentClassLogger().Info("         value.Manual : " + value.Manual);
                                        NLog.LogManager.GetCurrentClassLogger().Info("         value.Computed : " + value.Computed);
                                        NLog.LogManager.GetCurrentClassLogger().Info("         value.Reference : " + value.Reference);
                                        int nbValues = value.ActualValue.Count;
                                        NLog.LogManager.GetCurrentClassLogger().Info("                nbValues = " + nbValues);
                                        for (int i = 0; i < nbValues; i++)
                                            NLog.LogManager.GetCurrentClassLogger().Info($"                val[{i}] = {value.ActualValue[i]}");
                                        NLog.LogManager.GetCurrentClassLogger().Info("                ValueSwitch = " + Convert.ToString(value.ValueSwitch));
                                        value.ValueSwitch = ParameterSwitchKind.MANUAL;
                                    }
                                }
                            }
                            else
                            {
                                // To implement...
                            }
                        }
                    }
                }
            }
            /*
            foreach (ElementDefinition elem in ListOfElements)
            { 
                foreach (Parameter p in elem.Parameter)
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("XXXX " + p.ParameterType.Name);

                    if (p.ParameterType.Name == "resistance")
                    //if (p.ParameterType.Name == "power dissipated")
                    {
                        NLog.LogManager.GetCurrentClassLogger().Info("XXXX resistance parameter found");


                        if (p.StateDependence is not null)
                        {
                            var listOfState = p.StateDependence.ActualState;

                            foreach (var actualstate in listOfState)
                            {
                                var value = p.QueryParameterBaseValueSet(null, actualstate);

                                NLog.LogManager.GetCurrentClassLogger().Info("**** " + actualstate.Name + "  " + value.ActualValue[0]);

                                //this.value[actualstate.Name] = Double.Parse(value.ActualValue[0]);
                            }
                        }
                        else
                        {
                            var value = p.QueryParameterBaseValueSet(null, null);

                            int nbValues = value.ActualValue.Count;
                            NLog.LogManager.GetCurrentClassLogger().Info("**** NO STATE: nbValues = " + nbValues);

                            string valueString = value.ActualValue[0];
                            NLog.LogManager.GetCurrentClassLogger().Info("**** NO STATE: val = " + valueString);

                            
                            // V2

                            var paramCloned = p.Clone(true);

                            var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);

                            var newValue = paramCloned.QueryParameterBaseValueSet(null, null);
                            newValue.ActualValue[0] = "2600";

                            transaction.CreateOrUpdate((ParameterValueSet)newValue);

                            //transaction.CreateOrUpdate(paramCloned);

                            this.hubController.Write(transaction);




                        }
                    }
                }
            }
            */



            ElementDefinition obcED = ListOfElements.FirstOrDefault(x => x.Name == "OBC");

            Parameter paramPowDiss = obcED.Parameter.FirstOrDefault(x => x.ParameterType.Name == "Power Dissipation");

            var rdl = this.GetReferenceDataLibrary();
            var parameters = rdl.QueryParameterTypesFromChainOfRdls();

            var elemCloned = obcED.Clone(true);

            // Create minimum temperature if it does not exist                       
            {
                Parameter param = elemCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == "minimum temperature");
                if (param is null)
                {
                    // one transaction for each parameters
                    var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(elemCloned), elemCloned);

                    ParameterType paramType = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == "minimum temperature" && !x.IsDeprecated);
                    Parameter newTemperatureParam = new(Guid.NewGuid(), null, null)
                    {
                        ParameterType = paramType,
                        Owner = this.hubController.CurrentDomainOfExpertise
                    };
                    elemCloned.Parameter.Add((Parameter)newTemperatureParam);
                    transaction.CreateOrUpdate(newTemperatureParam);

                    await this.hubController.Write(transaction);
                }
            }

            // Create maximum temperature if it does not exist                       
            {
                Parameter param = elemCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == "maximum temperature");
                if (param is null)
                {
                    // one transaction for each parameters
                    var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(elemCloned), elemCloned);

                    ParameterType paramType = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == "maximum temperature" && !x.IsDeprecated);
                    Parameter newTemperatureParam = new(Guid.NewGuid(), null, null)
                    {
                        ParameterType = paramType,
                        Owner = this.hubController.CurrentDomainOfExpertise
                    };
                    elemCloned.Parameter.Add((Parameter)newTemperatureParam);
                    transaction.CreateOrUpdate(newTemperatureParam);

                    await this.hubController.Write(transaction);
                }
            }

            // Create sampled temperature if it does not exist
            {
                Parameter param = elemCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == "sampled temperature");
                if (param is null)
                {
                    // one transaction for each parameters
                    var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(elemCloned), elemCloned);

                    ParameterType paramType = parameters.OfType<SampledFunctionParameterType>().FirstOrDefault(x => x.Name == "sampled temperature" && !x.IsDeprecated);
                    Parameter newTemperatureParam = new(Guid.NewGuid(), null, null)
                    {
                        ParameterType = paramType,
                        Owner = this.hubController.CurrentDomainOfExpertise,
                    };
                    if (paramPowDiss is not null)
                    {
                        if (paramPowDiss.StateDependence is not null)
                            newTemperatureParam.StateDependence = paramPowDiss.StateDependence;    // We define the state dependance of PowerDissipated
                    }
                    elemCloned.Parameter.Add((Parameter)newTemperatureParam);
                    transaction.CreateOrUpdate(newTemperatureParam);

                    await this.hubController.Write(transaction);
                }
            }





            // Set the value
            await this.hubController.Refresh();
            ListOfElements = this.hubController.OpenIteration.Element;
            obcED = ListOfElements.FirstOrDefault(x => x.Name == "OBC");    // Maybe not necessary
            elemCloned = obcED.Clone(true);


            //UpdateElements();   // To rebuild the list from the HUB.... It is probably not necessary
            //obcED = ListOfElements.FirstOrDefault(x => x.Name == "OBC");    // Maybe not necessary
            //elemCloned = obcED.Clone(true);  // Maybe not necessary 

            // Define the value for sampled temperature
            {

                Parameter param = elemCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == "sampled temperature");
                var paramCloned = param.Clone(true);

                var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);

                //var newValue = paramCloned.QueryParameterBaseValueSet(null, param.StateDependence.ActualState.FirstOrDefault());
                var newValue = paramCloned.QueryParameterBaseValueSet(null, null);


                newValue.ValueSwitch = ParameterSwitchKind.COMPUTED;

                List<string> values = new();
                values.Add("0");    // time 1
                values.Add("50");   // temperature 1 
                values.Add("10");   // time 2 
                values.Add("300");  // temperature 2

                NLog.LogManager.GetCurrentClassLogger().Info("         values : " + values);

                //var newValueCloned = ((ParameterValueSet)newValue).Clone(true);    // Mabe not necessary because the paramter is cloned with true arg
                ((ParameterValueSet)newValue).Computed = new CDP4Common.Types.ValueArray<string>(values);


                NLog.LogManager.GetCurrentClassLogger().Info("         newValue.Manual : " + newValue.Manual);
                NLog.LogManager.GetCurrentClassLogger().Info("         newValue.Computed : " + newValue.Computed);
                NLog.LogManager.GetCurrentClassLogger().Info("         newValue.Reference : " + newValue.Reference);

                transaction.CreateOrUpdate((ParameterValueSet)newValue);
                //transaction.CreateOrUpdate((ParameterValueSet)newValueCloned); // Maybe not necessary... can use the orignal ValueSet because param Cloned with true argument
                //transaction.CreateOrUpdate(paramCloned);   // NOT SURE!!!

                await this.hubController.Write(transaction);
            }



            //UpdateElements();   // To rebuild the list from the HUB.... It is probably necessary



            await this.hubController.Refresh();
            ListOfElements = this.hubController.OpenIteration.Element;
            obcED = ListOfElements.FirstOrDefault(x => x.Name == "OBC");    // Maybe not necessary
            //elemCloned = obcED.Clone(true);

            // Override ELEMENT USAGE
            {
                //                obcED = ListOfElements.FirstOrDefault(x => x.Name == "OBC");


                var elementUsagesOfOBC = obcED.ReferencingElementUsages();

                ElementUsage obc1EU = elementUsagesOfOBC.FirstOrDefault(x => x.Name == "OBC1");

                Parameter paramTemperatureED = obcED.Parameter.FirstOrDefault(x => x.ParameterType.Name == "maximum temperature");

                ParameterOverride paramOver = new(Guid.NewGuid(), null, null);
                paramOver.Parameter = paramTemperatureED;
                paramOver.Owner = this.hubController.CurrentDomainOfExpertise;
                
                var obc1EUCloned = obc1EU.Clone(true);  // False is probably better here!
                obc1EUCloned.ParameterOverride.Add(paramOver);


                var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(obc1EUCloned), obc1EUCloned);

                transaction.CreateOrUpdate(paramOver);

                //transaction.CreateOrUpdate(obc1EUclone);


                await this.hubController.Write(transaction);
            }
        }


    }
}
