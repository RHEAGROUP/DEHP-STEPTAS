// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutomatedTest.cs" company="Open Engineering S.A.">
//     Copyright (c) 2022 Open Engineering S.A.
//
//     Author: S. Paquay, C. Henrard
//
//     Part of the code was based on the work performed by RHEA as result of the collaboration in
//     the context of "Digital Engineering Hub Pathfinder" by Sam Gerené, Alex Vorobiev, Alexander
//     van Delft and Nathanael Smiechowski.
//
//     This file is part of DEHP STEP-TAS adapter project.
//
//     The DEHP STEP-TAS is free software; you can redistribute it and/or modify it under the
//     terms of the GNU Lesser General Public License as published by the Free Software Foundation;
//     either version 3 of the License, or (at your option) any later version.
//
//     The DEHP STEP-TAS is distributed in the hope that it will be useful, but WITHOUT ANY
//     WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
//     PURPOSE. See the GNU Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public License along with this
//     program; if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth
//     Floor, Boston, MA 02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Autofac;
using CDP4Common.CommonData;
using CDP4Common.EngineeringModelData;
using CDP4Common.SiteDirectoryData;
using CDP4Common.Types;
using CDP4Dal;
using CDP4Dal.Operations;
using DEHPCommon;
using DEHPCommon.Enumerators;
using DEHPCommon.HubController.Interfaces;
using DEHPCommon.MappingEngine;
using DEHPCommon.Services.ExchangeHistory;
using DEHPCommon.Services.NavigationService;
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPSTEPTAS.Events;
using DEHPSTEPTAS.ViewModel;

using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DEHPSTEPTAS.Services.DstHubService;
using DEHPSTEPTAS.ViewModel.Dialogs.Interfaces;
using DEHPSTEPTAS.Builds.HighLevelRepresentationBuilder;
using DEHPSTEPTAS.Views.Dialogs;
using DEHPSTEPTAS.ViewModel.Dialogs;



namespace DEHPSTEPTAS.AutomatedTests
{
    using DEHPCommon.HubController;
    using DEHPCommon.Services.FileDialogService;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;
    using DEHPCommon.UserPreferenceHandler.Enums;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPTAS.Dialog.Interfaces;
    using DEHPSTEPTAS.Dialogs;
    using DEHPSTEPTAS.DstController;
    using DEHPSTEPTAS.Services.FileStoreService;
    using DEHPSTEPTAS.Settings;
    using DEHPSTEPTAS.ViewModel.Interfaces;
    using DEHPSTEPTAS.ViewModel.NetChangePreview;
    using DEHPSTEPTAS.ViewModel.Rows;
    using ReactiveUI;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reflection;
    using System.Windows;

    //[TestFixture]
    [TestFixture, Apartment(ApartmentState.STA)]
    public class Tests
    {
        private ContainerBuilder containerBuilder = null;
        private App application = null;

        // Credentials
        private Uri uri = new Uri("http://localhost:5000");
        //private Uri uri = new Uri("https://cdp4services-public.rheagroup.com");
        private string userName = "admin";
        private string password = "pass";


        private string pathToStepTasFiles = "D:\\StepTasFiles\\";
        private string pathToCSVFiles = "D:\\CSVFiles\\";


        public void ApplicationOpen()
        {
            if (containerBuilder == null)
            {
                containerBuilder = new ContainerBuilder();

               

                application = new App(containerBuilder);

                DstController dstController = (DstController)AppContainer.Container.Resolve<IDstController>();
                dstController.CodeCoverageState = true;


               
            }

            
            // For each test : rebuild a new container... and activate code coverage state in order to avoid some UI interaction 
            {
                containerBuilder = new ContainerBuilder();
                App.RegisterTypes(containerBuilder);
                App.RegisterViewModels(containerBuilder);

                AppContainer.BuildContainer(containerBuilder);

                DstController dstController = (DstController)AppContainer.Container.Resolve<IDstController>();
                dstController.CodeCoverageState = true;
            }
        }


        public void RemoveModelFromHub(IHubController hubController, string modelName)
        {
            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();

            Console.WriteLine(hubController.IsSessionOpen);
            Console.WriteLine(hubController.Session.Name);

            SiteDirectory siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();

            // Delete the existing engineering model if it exists (support the case if there are several)            
            while (true)
            {
                EngineeringModelSetup modelSetup2 = siteDirectory.Model.Find(x => x.UserFriendlyName == modelName);
                if (modelSetup2 is null)
                    break;

                Console.WriteLine("Deleting Model on HUB");

                SiteDirectory siteDirectoryCloned2 = siteDirectory.Clone(false);
                ThingTransaction transaction2 = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned2), siteDirectoryCloned2);

                EngineeringModelSetup testModelSetupCloned = modelSetup2.Clone(false);
                transaction2.Delete(testModelSetupCloned);

                hubController.Write(transaction2).Wait();

                hubController.Refresh();
            }
            hubController.Close();
        }


        public void ResetEngineeringModel(IHubController hubController, string modelName, bool withStepRef)
        {
            RemoveModelFromHub(hubController, modelName);   // Does nothing if the model does not exist

            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
            Console.WriteLine(hubController.IsSessionOpen);
            Console.WriteLine(hubController.Session.Name);

            SiteDirectory siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();


            // Create the empty model
            ////////////////////////////
            SiteDirectory siteDirectoryCloned = siteDirectory.Clone(false);   // Try false
            ThingTransaction transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned), siteDirectoryCloned);


            ModelReferenceDataLibrary modelRDL = new ModelReferenceDataLibrary(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "EM1_RDL",
                ShortName = "EM1_RDL",
                RequiredRdl = siteDirectory.SiteReferenceDataLibrary[0]
            };
            transaction.CreateOrUpdate(modelRDL);

            EngineeringModelSetup modelSetup = new EngineeringModelSetup(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = modelName,
                ShortName = modelName,
            };

            modelSetup.RequiredRdl.Add(modelRDL);

            EngineeringModel engineeringModel = new EngineeringModel(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                EngineeringModelSetup = modelSetup
            };
            modelSetup.EngineeringModelIid = engineeringModel.Iid;

            transaction.CreateOrUpdate(modelSetup);

            siteDirectoryCloned.Model.Add(modelSetup);

            hubController.Write(transaction).Wait();

            Console.WriteLine("Model created");


            // Open the iteration


            EngineeringModelSetup modelSetup3 = siteDirectory.Model.Find(x => x.UserFriendlyName == modelName);
            DomainOfExpertise domExpertise = modelSetup3.ActiveDomain[0];

            Console.WriteLine(modelSetup3.UserFriendlyName);
            Console.WriteLine(domExpertise.UserFriendlyName);

            var model = new EngineeringModel(modelSetup3.EngineeringModelIid, hubController.Session.Assembler.Cache, uri);
            var itIid = modelSetup3.IterationSetup[0].IterationIid;
            var iteration = new Iteration(itIid, hubController.Session.Assembler.Cache, uri);
            model.Iteration.Add(iteration);
            hubController.GetIteration(iteration, domExpertise).Wait();

            iteration = hubController.OpenIteration;

            var iterationCloned = iteration.Clone(true);

            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(iterationCloned), iterationCloned);

            ElementDefinition edMission = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Mission",
                ShortName = "Mission",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edMission);
            iterationCloned.Element.Add(edMission);

            ElementDefinition edSatellite = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Satellite",
                ShortName = "Satellite",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edSatellite);
            iterationCloned.Element.Add(edSatellite);

            ElementDefinition edOBC = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "OBC",
                ShortName = "OBC",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edOBC);
            iterationCloned.Element.Add(edOBC);

            ElementDefinition edPCDU = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "PCDU",
                ShortName = "PCDU",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edPCDU);
            iterationCloned.Element.Add(edPCDU);

            ElementDefinition edRadiator = new ElementDefinition(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Radiator",
                ShortName = "Radiator",
                Owner = domExpertise,
            };
            transaction.CreateOrUpdate(edRadiator);
            iterationCloned.Element.Add(edRadiator);

            hubController.Write(transaction).Wait();

            // Define EU under Satellite
            var edSatelliteCloned = edSatellite.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edSatelliteCloned), edSatelliteCloned);
            ElementUsage euOBC1 = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "OBC1",
                ShortName = "OBC1",
                Owner = domExpertise,
                ElementDefinition = edOBC
            };
            transaction.CreateOrUpdate(euOBC1);
            edSatelliteCloned.ContainedElement.Add(euOBC1);
            ElementUsage euOBC2 = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "OBC2",
                ShortName = "OBC2",
                Owner = domExpertise,
                ElementDefinition = edOBC
            };
            transaction.CreateOrUpdate(euOBC2);
            edSatelliteCloned.ContainedElement.Add(euOBC2);
            ElementUsage euPCDU = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "PCDU",
                ShortName = "PCDU",
                Owner = domExpertise,
                ElementDefinition = edPCDU
            };
            transaction.CreateOrUpdate(euPCDU);
            edSatelliteCloned.ContainedElement.Add(euPCDU);
            ElementUsage euRadiator = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Radiator",
                ShortName = "Radiator",
                Owner = domExpertise,
                ElementDefinition = edRadiator
            };
            transaction.CreateOrUpdate(euRadiator);
            edSatelliteCloned.ContainedElement.Add(euRadiator);
            hubController.Write(transaction).Wait();


            // Define EU under Mission
            var edMissionCloned = edMission.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edMissionCloned), edMissionCloned);
            ElementUsage euSatellite = new ElementUsage(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Satellite",
                ShortName = "Satellite",
                Owner = domExpertise,
                ElementDefinition = edSatellite
            };
            transaction.CreateOrUpdate(euSatellite);
            edMissionCloned.ContainedElement.Add(euSatellite);
            hubController.Write(transaction).Wait();



            // Define "Mode" PossibleFiniteStateList
            var iterationCloned2 = hubController.OpenIteration.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(iterationCloned2), iterationCloned2);
            PossibleFiniteStateList modeFSList = new PossibleFiniteStateList(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Mode",
                ShortName = "Mode",
                Owner = domExpertise
            };
            iterationCloned2.PossibleFiniteStateList.Add(modeFSList);

            PossibleFiniteState launchFS = new PossibleFiniteState(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Launch mode",
                ShortName = "Launch_mode",
            };
            modeFSList.PossibleState.Add(launchFS);
            transaction.CreateOrUpdate(launchFS);
            PossibleFiniteState nominalFS = new PossibleFiniteState(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Nominal mode",
                ShortName = "Nominal_mode",
            };
            modeFSList.PossibleState.Add(nominalFS);
            transaction.CreateOrUpdate(nominalFS);
            PossibleFiniteState safeFS = new PossibleFiniteState(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = "Safe mode",
                ShortName = "Safe_mode",
            };
            modeFSList.PossibleState.Add(safeFS);
            transaction.CreateOrUpdate(safeFS);

            transaction.CreateOrUpdate(modeFSList);

            hubController.Write(transaction).Wait();


            // Define the actualFS
            var iterationCloned3 = hubController.OpenIteration.Clone(false);
            transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(iterationCloned3), iterationCloned3);
            ActualFiniteStateList actualFSList = new ActualFiniteStateList(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Owner = domExpertise
            };
            actualFSList.PossibleFiniteStateList.Add(modeFSList);
            iterationCloned3.ActualFiniteStateList.Add(actualFSList);
            transaction.CreateOrUpdate(actualFSList);
            hubController.Write(transaction).Wait();


            // mean consumed power
            {
                var model2 = hubController.OpenIteration.GetContainerOfType<EngineeringModel>();
                var modelSetup2 = model2.EngineeringModelSetup;
                var rdls = modelSetup2.RequiredRdl;
                var rdl = rdls.First();
                var parameters = rdl.QueryParameterTypesFromChainOfRdls();
                var scales = rdl.QueryMeasurementScalesFromChainOfRdls();

                var finiteStateList = hubController.OpenIteration.ActualFiniteStateList;
                int nbFinitesStates = finiteStateList.Count;
                var finiteStates = finiteStateList[0];

                ParameterType paramType = parameters.FirstOrDefault(x => x.Name == "mean consumed power" && !x.IsDeprecated);
                MeasurementScale scale = scales.FirstOrDefault(x => x.Name == "watt" && !x.IsDeprecated);

                // consumed mean power for PCDU ED
                {
                    var edPCDUCloned = edPCDU.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edPCDUCloned), edPCDUCloned);
                    Parameter newParam = new Parameter(Guid.NewGuid(), null, null)
                    {
                        ParameterType = paramType,
                        Owner = hubController.CurrentDomainOfExpertise,
                        Scale = scale, // finiteStates
                        StateDependence = null,
                    };
                    edPCDUCloned.Parameter.Add(newParam);
                    transaction.CreateOrUpdate(newParam);
                    hubController.Write(transaction).Wait();
                    edPCDU = hubController.OpenIteration.Element.FirstOrDefault(x => x.Name == "PCDU");
                    Parameter param = edPCDU.Parameter.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    var paramCloned = param.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);
                    var newValue = paramCloned.QueryParameterBaseValueSet(null, null);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "100";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);
                    hubController.Write(transaction).Wait();
                }

                // consumed mean power for OBC ED
                {
                    var edOBCCloned = edOBC.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edOBCCloned), edOBCCloned);
                    Parameter newParam = new Parameter(Guid.NewGuid(), null, null)
                    {
                        ParameterType = paramType,
                        Owner = hubController.CurrentDomainOfExpertise,
                        Scale = scale,
                        StateDependence = finiteStates
                    };
                    edOBCCloned.Parameter.Add(newParam);
                    transaction.CreateOrUpdate(newParam);
                    hubController.Write(transaction).Wait();


                    Console.WriteLine("FS1 -> " + finiteStates.ActualState[0].Name);
                    Console.WriteLine("FS2 -> " + finiteStates.ActualState[1].Name);
                    Console.WriteLine("FS3 -> " + finiteStates.ActualState[2].Name);

                    edOBC = hubController.OpenIteration.Element.FirstOrDefault(x => x.Name == "OBC");
                    Parameter param = edOBC.Parameter.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    var paramCloned = param.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);
                    var newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[0]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "10";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);

                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[1]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "30";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);

                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[2]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterValueSet)newValue).Manual[0] = "50";
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);

                    hubController.Write(transaction).Wait();
                }

                // Override mean consumed power on OBC2
                {
                    var euOBC2Cloned = euOBC2.Clone(false);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(euOBC2Cloned), euOBC2Cloned);
                    Parameter paramED = edOBC.Parameter.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    ParameterOverride paramOver = new ParameterOverride(Guid.NewGuid(), null, null)
                    {
                        Parameter = paramED,
                        Owner = hubController.CurrentDomainOfExpertise
                    };
                    euOBC2Cloned.ParameterOverride.Add(paramOver);

                    transaction.CreateOrUpdate(paramOver);

                    hubController.Write(transaction).Wait();


                    euOBC2 = edOBC.ReferencingElementUsages().FirstOrDefault(x => x.Name == "OBC2");
                    ParameterOverride param = euOBC2.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == "mean consumed power");
                    var paramCloned = param.Clone(true);
                    transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);
                    var newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[0]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterOverrideValueSet)newValue).Manual[0] = "20";
                    transaction.CreateOrUpdate((ParameterOverrideValueSet)newValue);

                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[1]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterOverrideValueSet)newValue).Manual[0] = "40";
                    transaction.CreateOrUpdate((ParameterOverrideValueSet)newValue);

                    newValue = paramCloned.QueryParameterBaseValueSet(null, finiteStates.ActualState[2]);
                    newValue.ValueSwitch = ParameterSwitchKind.MANUAL;
                    ((ParameterOverrideValueSet)newValue).Manual[0] = "60";
                    transaction.CreateOrUpdate((ParameterOverrideValueSet)newValue);

                    hubController.Write(transaction).Wait();
                }
            }

            if (withStepRef)                    // Adding some "step geometry" with finite states
            {
                DstHubService dstHubService = (DstHubService)AppContainer.Container.Resolve<IDstHubService>();
                dstHubService.CheckHubDependencies().Wait();

                var model2 = hubController.OpenIteration.GetContainerOfType<EngineeringModel>();
                var modelSetup2 = model2.EngineeringModelSetup;
                var rdls = modelSetup2.RequiredRdl;
                var rdl = rdls.First();
                var parameters = rdl.ParameterType;

                var finiteStateList = hubController.OpenIteration.ActualFiniteStateList;
                int nbFinitesStates = finiteStateList.Count;
                var finiteStates = finiteStateList[0];

                // one transaction for each parameters
                var edOBCCloned = edOBC.Clone(true);
                transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edOBCCloned), edOBCCloned);
                ParameterType paramType = parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.Name == "step tas reference" && !x.IsDeprecated);
                Parameter newParam = new Parameter(Guid.NewGuid(), null, null)
                {
                    ParameterType = paramType,
                    Owner = hubController.CurrentDomainOfExpertise,
                    StateDependence = finiteStates
                };
                edOBCCloned.Parameter.Add(newParam);
                transaction.CreateOrUpdate(newParam);
                hubController.Write(transaction).Wait();

                // Parameter override

                var euOBC1Cloned = euOBC1.Clone(false);
                transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(euOBC1Cloned), euOBC1Cloned);
                Parameter paramED = edOBCCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");
                ParameterOverride paramOver = new ParameterOverride(Guid.NewGuid(), null, null)
                {
                    Parameter = paramED,
                    Owner = hubController.CurrentDomainOfExpertise
                };
                euOBC1Cloned.ParameterOverride.Add(paramOver);

                transaction.CreateOrUpdate(paramOver);

                hubController.Write(transaction).Wait();
            }

            hubController.Close();

            return;
        }


        // The following method is not used anymore
        public void RestoreHubModelFromTemplateModel(IHubController hubController, string templateModelName, string testModelName)
        {
            Console.WriteLine("BEGIN RestoreHubModelFromTemplateModel");

            RemoveModelFromHub(hubController, testModelName);   // Does nothing if the model does not exist

            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
            Console.WriteLine(hubController.IsSessionOpen);
            Console.WriteLine(hubController.Session.Name);

            SiteDirectory siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();
            EngineeringModelSetup templateModelSetup = siteDirectory.Model.Find(x => x.UserFriendlyName == templateModelName);

            DomainOfExpertise domExpertise = templateModelSetup.ActiveDomain[0];
            Console.WriteLine(templateModelSetup.UserFriendlyName);
            Console.WriteLine(domExpertise.UserFriendlyName);

            SiteDirectory siteDirectoryCloned = siteDirectory.Clone(false);

            ThingTransaction transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned), siteDirectoryCloned);

            EngineeringModelSetup testModelSetup = new EngineeringModelSetup(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = testModelName,
                ShortName = testModelName,
                SourceEngineeringModelSetupIid = templateModelSetup.Iid,
            };

            EngineeringModel engineeringModel = new EngineeringModel(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                EngineeringModelSetup = testModelSetup
            };

            testModelSetup.EngineeringModelIid = engineeringModel.Iid;


            transaction.CreateOrUpdate(testModelSetup);

            siteDirectoryCloned.Model.Add(testModelSetup);

            hubController.Write(transaction).Wait();

            hubController.Close();

            Console.WriteLine("END RestoreHubModelFromTemplateModel");
            return;

        }



/*
        public void RestoreHubModelFromTemplateModel(IHubController hubController, string templateModelName, string testModelName)
        {
            Console.WriteLine("BEGIN RestoreHubModelFromTemplateModel");


            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
            Console.WriteLine(hubController.IsSessionOpen);
            Console.WriteLine(hubController.Session.Name);

            SiteDirectory siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();
            EngineeringModelSetup templateModelSetup = siteDirectory.Model.Find(x => x.UserFriendlyName == templateModelName);

            DomainOfExpertise domExpertise = templateModelSetup.ActiveDomain[0];
            Console.WriteLine(templateModelSetup.UserFriendlyName);
            Console.WriteLine(domExpertise.UserFriendlyName);

            EngineeringModelSetup testModelSetup = siteDirectory.Model.Find(x => x.UserFriendlyName == testModelName);
            while (testModelSetup != null)     // we consider that several model with the same name can exist....
            {
                Console.WriteLine("Deleting TestModel on HUB");

                SiteDirectory siteDirectoryCloned2 = siteDirectory.Clone(false);   // Try false
                ThingTransaction transaction2 = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned2), siteDirectoryCloned2);

                EngineeringModelSetup testModelSetupCloned = testModelSetup.Clone(false);
                transaction2.Delete(testModelSetupCloned);

                hubController.Write(transaction2).Wait();

                hubController.Refresh();

                testModelSetup = siteDirectory.Model.Find(x => x.UserFriendlyName == testModelName);
            }


            SiteDirectory siteDirectoryCloned = siteDirectory.Clone(false);   // Try false

            ThingTransaction transaction = new ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(siteDirectoryCloned), siteDirectoryCloned);


            //EngineeringModelSetup
            testModelSetup = new EngineeringModelSetup(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                Name = testModelName,
                ShortName = testModelName,
                SourceEngineeringModelSetupIid = templateModelSetup.Iid,
            };

            EngineeringModel engineeringModel = new EngineeringModel(Guid.NewGuid(), hubController.Session.Assembler.Cache, uri)
            {
                EngineeringModelSetup = testModelSetup
            };

            testModelSetup.EngineeringModelIid = engineeringModel.Iid;


            transaction.CreateOrUpdate(testModelSetup);

            siteDirectoryCloned.Model.Add(testModelSetup);

            hubController.Write(transaction).Wait();

            hubController.Close();

            Console.WriteLine("END RestoreHubModelFromTemplateModel");
            return;

        }*/


        public void OpenIterationOnHub(IHubController hubController, string modelName)
        {
            Console.WriteLine("Open iteration on hub");
            CDP4Dal.DAL.Credentials credentials = new CDP4Dal.DAL.Credentials(userName, password, uri);
            hubController.Open(credentials, ServerType.Cdp4WebServices).Wait();
            Console.WriteLine(hubController.Session.Name);

            var siteDirectory = hubController.Session.Assembler.RetrieveSiteDirectory();


            EngineeringModelSetup modelSetup = siteDirectory.Model.Find(x => x.UserFriendlyName == modelName);

            DomainOfExpertise domExpertise = modelSetup.ActiveDomain[0];

            Console.WriteLine(modelSetup.UserFriendlyName);
            Console.WriteLine(domExpertise.UserFriendlyName);
            Console.WriteLine(modelSetup.IterationSetup[0].UserFriendlyName);


            var model = new EngineeringModel(modelSetup.EngineeringModelIid, hubController.Session.Assembler.Cache, uri);

            Console.WriteLine("Model Name " + model.UserFriendlyName);

            var itIid = modelSetup.IterationSetup[0].IterationIid;

            var iteration = new Iteration(itIid, hubController.Session.Assembler.Cache, uri);
            model.Iteration.Add(iteration);

            hubController.GetIteration(iteration, domExpertise).Wait(); // GetAwaiter().GetResult();

                        // What are the Option?
            Console.WriteLine("OPTIONS:");
            foreach (Option option in hubController.OpenIteration.Option)
            {
                Console.WriteLine("  Option " + option.Name);
            }
            if(hubController.OpenIteration.DefaultOption != null)
                Console.WriteLine("   Default Option " + hubController.OpenIteration.DefaultOption.Name);



            // What is the ED list of opened iteration?
            Console.WriteLine("ElementDefintions:");
            foreach (var elem in hubController.OpenIteration.Element)
            {
                Console.WriteLine("  ED " + elem.Name);

                var containedEUList = elem.ContainedElement;

                foreach(var entry in containedEUList)
                {
                    Console.WriteLine("    EU " + entry.Name);
                    foreach (var exclOpt in entry.ExcludeOption)
                    {
                        Console.WriteLine("      ExcludedOption " + exclOpt.Name);
                    }
                }
            }

            Console.WriteLine("END ElementDefintion of Open Model");
        }

        public void DeclareMapping(string path, string name, string EDname,string EUname=null,string FSname=null)
        {
            Console.WriteLine("BEGIN Mapping");

            DstObjectBrowserViewModel dstObjectBrowserViewModel = (DstObjectBrowserViewModel)AppContainer.Container.Resolve<IDstObjectBrowserViewModel>();
            foreach (var entry in dstObjectBrowserViewModel.StepTasTree)
            {
                var Name = entry.Name;
                var Path = entry.Path;
                var InstancePath = entry.InstancePath;
                var ID = entry.ID;
                var ParentID = entry.ParentID;
                var MaterialName = entry.MaterialName;
                var Description = entry.Description;
            }

            StepTasRowViewModel rowVM = null;

            if (name != null)
                rowVM = dstObjectBrowserViewModel.StepTasTree.Find(x => x.Path == path && x.Name == name);
            else
                rowVM = dstObjectBrowserViewModel.StepTasTree.Find(x => x.Path == path);

            if (rowVM is null)
            {
                Assert.Fail("rowVM not found");
                return;
            }

            Console.WriteLine("   rowVW " + path + name + "found!");

            dstObjectBrowserViewModel.SelectedNode = rowVM;
            dstObjectBrowserViewModel.PopulateContextMenu();   // For source code coverage
            dstObjectBrowserViewModel.MapCommand.ExecuteAsyncTask(null).Wait();


            MappingConfigurationDialogViewModel mappingConfigurationDialogViewModel = (MappingConfigurationDialogViewModel)AppContainer.Container.Resolve<IMappingConfigurationDialogViewModel>();

            mappingConfigurationDialogViewModel.SetPart(rowVM);    // SPA : mandatory if MappingConfigurationDialogViewModel is not a SingleInstance()

            var availableElementDefinitions = mappingConfigurationDialogViewModel.AvailableElementDefinitions;

            ElementDefinition elementDefinition = null;
            foreach (var elem in availableElementDefinitions)
            {
                if (elem.Name == EDname)
                {
                    elementDefinition = elem;
                    break;
                }
            }
            if (elementDefinition is null)
            {
                Console.WriteLine("   ED " + EDname + "not found!    Creating a new ED on the HUB");
                mappingConfigurationDialogViewModel.SelectedThing.SelectedElementDefinition = null;
                mappingConfigurationDialogViewModel.SelectedThing.NewElementDefinitionName = EDname;
            }
            else
            {
                Console.WriteLine("   ED " + EDname + " found!");
                mappingConfigurationDialogViewModel.SelectedThing.SelectedElementDefinition = elementDefinition;
                mappingConfigurationDialogViewModel.SelectedThing.NewElementDefinitionName = null;
            }



            //  Specific things 


            if (EUname != null)
            {
                var availableElementUsages = mappingConfigurationDialogViewModel.AvailableElementUsages;
                ElementUsage selectedEU = availableElementUsages.FirstOrDefault(x => x.Name == EUname);

                if (selectedEU != null)
                {
                        Console.WriteLine("    Mapping EU " + selectedEU.Name);
                        mappingConfigurationDialogViewModel.SelectedThing.SelectedElementUsages.Add(selectedEU);
                }
                else
                {
                        Assert.Fail("The EU " + selectedEU.Name + " does not exist");
                }
            }


            if (FSname != null)
            {
                var availableFiniteStates = mappingConfigurationDialogViewModel.AvailableActualFiniteStates;

                var selectedFS = availableFiniteStates.FirstOrDefault(x => x.Name == FSname);

                if (selectedFS != null)
                {
                    Console.WriteLine("    Mapping FS " + selectedFS.Name);
                    mappingConfigurationDialogViewModel.SelectedThing.SelectedActualFiniteState = selectedFS;
                }
                else
                {
                    Assert.Fail("The FS " + selectedFS.Name + " does not exist");
                }
            }

            mappingConfigurationDialogViewModel.ContinueCommand.ExecuteAsyncTask(null).Wait();

            Console.WriteLine("END Mapping");
        }



        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
        }


        //[Test]
        public void TestHubDependencies()
        {
            ApplicationOpen();

            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            // Retore the used model on HUB from template model
            RestoreHubModelFromTemplateModel(hubController, "Template_STEPTAS_FiniteStates", "TestModel_STEPTAS_FiniteStates");
            OpenIterationOnHub(hubController, "TestModel_STEPTAS_FiniteStates");

            // Begin of test
            DstHubService dstHubService = (DstHubService)AppContainer.Container.Resolve<IDstHubService>();

            Console.WriteLine("Checking HUB dependencies");

            dstHubService.CheckHubDependencies().Wait();

            Console.WriteLine("END of Checking HUB dependencies");

            return;
        }
                




        [Test]
        public void EM1_NoConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            //ResetEngineeringModel(hubController, "Template_STEPTAS_FiniteStates", false);

            //Assert.IsFalse(true);

            ResetEngineeringModel(hubController, "EM1_Test_STEPTAS", false);
            OpenIterationOnHub(hubController, "EM1_Test_STEPTAS");

            VerifyALL(1);

            /*
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            //OpenSaveFileDialogService openSaveFileDialogService = new OpenSaveFileDialogService();
            //IHubController hubController = new HubController(openSaveFileDialogService);

            RestoreHubModelFromTemplateModel(hubController, "Template_STEPTAS_FiniteStates", "TestModel_STEPTAS_FiniteStates");

            OpenIterationOnHub(hubController, "TestModel_STEPTAS_FiniteStates");


            //return;

            VerifyALL(1);*/
        }



        [Test]
        public void EM1_WithConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            OpenIterationOnHub(hubController, "EM1_Test_STEPTAS");

            VerifyALL(1);

        }


        [Test]
        public void EM2_NoConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            ResetEngineeringModel(hubController, "EM2_Test_STEPTAS", true);
            OpenIterationOnHub(hubController, "EM2_Test_STEPTAS");

            VerifyALL(2);

            /*
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();


            RestoreHubModelFromTemplateModel(hubController, "Template_STEPTAS_FiniteStates", "TestModel_STEPTAS_FiniteStates_TasRef_FSAndOverride");
            OpenIterationOnHub(hubController, "TestModel_STEPTAS_FiniteStates_TasRef_FSAndOverride");


            // Begin of test
            
            DstHubService dstHubService = (DstHubService)AppContainer.Container.Resolve<IDstHubService>();
            dstHubService.CheckHubDependencies().Wait();

            ///////////////////////////////////////
            // Try to add finitestates tas ref   //
            ///////////////////////////////////////
            var iteration = hubController.OpenIteration;

            var obcED = iteration.Element.Find(x => x.Name == "OBC");

            var obcEDCloned = obcED.Clone(true);

            var model = iteration.GetContainerOfType<EngineeringModel>();
            var modelSetup = model.EngineeringModelSetup;
            var rdls = modelSetup.RequiredRdl;
            var rdl = rdls.First();

            var finiteStateList = iteration.ActualFiniteStateList;
            int nbFinitesStates = finiteStateList.Count;

            var finiteStates = finiteStateList[0];


            var parameters = rdl.ParameterType;  //rdl.QueryParameterTypesFromChainOfRdls();

            // one transaction for each parameters
            var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(obcEDCloned), obcEDCloned);

            ParameterType paramType = parameters.OfType<CompoundParameterType>().FirstOrDefault(x => x.Name == "step tas reference" && !x.IsDeprecated);
            Parameter newParam = new Parameter(Guid.NewGuid(), null, null)
            {
                ParameterType = paramType,
                Owner = hubController.CurrentDomainOfExpertise,
                StateDependence = finiteStates
            };

            obcEDCloned.Parameter.Add((Parameter)newParam);
            transaction.CreateOrUpdate(newParam);
            hubController.Write(transaction).Wait();

            // Parameter override


            var elementUsagesOfOBC = obcED.ReferencingElementUsages();

            ElementUsage obc1EU = elementUsagesOfOBC.FirstOrDefault(x => x.Name == "OBC1");

            Parameter paramED = obcED.Parameter.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");

            ParameterOverride paramOver = new ParameterOverride(Guid.NewGuid(), null, null)
            {
                Parameter = paramED,
                Owner = hubController.CurrentDomainOfExpertise
            };

            var obc1EUCloned = obc1EU.Clone(true);  // False is probably better here!
            obc1EUCloned.ParameterOverride.Add(paramOver);


            var transaction2 = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(obc1EUCloned), obc1EUCloned);

            transaction2.CreateOrUpdate(paramOver);

            hubController.Write(transaction2).Wait();


            //return;

            VerifyALL(2);

            //VerifyALL(true);
            */
        }

        [Test]
        public void EM2_WithConfig()
        {
            ApplicationOpen();
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();

            OpenIterationOnHub(hubController, "EM2_Test_STEPTAS");

            VerifyALL(2); // true);
        }

        
        

        public void VerifyALL(int mappingNumber)
        {
            //IHubController hubController = AppContainer.Container.Resolve<IHubController>();
            //hubController.Refresh();

            // Begin of test
            DstHubService dstHubService = (DstHubService)AppContainer.Container.Resolve<IDstHubService>();
            dstHubService.CheckHubDependencies().Wait();

            // call chechCheckHubDependencies() ofter constructor initialization  : IsSessionOpen
            HubDataSourceViewModel hubDataSourceViewModel = (HubDataSourceViewModel) AppContainer.Container.Resolve<IHubDataSourceViewModel>();


            //DstDataSourceViewModel dstDataSourceViewModel = (DstDataSourceViewModel)AppContainer.Container.Resolve<DstDataSourceViewModel>();


            UserPreferenceService<AppSettings> userPrefService = (UserPreferenceService<AppSettings>)AppContainer.Container.Resolve<IUserPreferenceService<AppSettings>>();
            userPrefService.Read();
            AppSettings settings = userPrefService.UserPreferenceSettings;
            Console.WriteLine(settings.FileStoreDirectoryName);
            Console.WriteLine(settings.PathToExtractionTemplates);
            Console.WriteLine(settings.PathToExtractionOutput);
            Console.WriteLine(settings.FileStoreCleanOnInit);
            Console.WriteLine(settings.MappingUsedByFiles);
            foreach (var entry in settings.MappingUsedByFiles)
                Console.WriteLine("  Configuration: " + entry.Key + "  -->  " + entry.Value);





            // Opening the STEP-TAS file
            DstLoadFileViewModel dstLoadFileViewModel = (DstLoadFileViewModel)AppContainer.Container.Resolve<IDstLoadFileViewModel>();
            //dstLoadFileViewModel.FilePath = "D:\\unexistingFile.stp";
            //dstLoadFileViewModel.LoadFileCommand.ExecuteAsyncTask(null).Wait();    // Load an inexisting file to improve coverage
            dstLoadFileViewModel.FilePath = pathToStepTasFiles+"SCbase.stp";
            dstLoadFileViewModel.LoadFileCommand.ExecuteAsyncTask(null).Wait();

            


            //DstController dstController = (DstController) AppContainer.Container.Resolve<IDstController>();
            //dstController.Load("D:\\SC_only geometry.stp");   // This load is "mandatory" why...



            
            //return;
            //userPrefService.UserPreferenceSettings = new AppSettings();
            //AppSettings settings = userPrefService.UserPreferenceSettings;
            //settings.PathToExtractionTemplates = "c:\\Templates";
            //settings.FileStoreDirectoryName = "c:\\Templates";
            //settings.MappingUsedByFiles.Add("sc_only geometry", "SC_only geometry Configuration");


            



            // Default dehavior for Mapping configuration
            MappingConfigurationManagerDialogViewModel mappingConfigurationManagerDialogViewModel
              = (MappingConfigurationManagerDialogViewModel) AppContainer.Container.Resolve<IMappingConfigurationManagerDialogViewModel>();

            var existingExternalIdentifierMap = mappingConfigurationManagerDialogViewModel.AvailableExternalIdentifierMap;


            bool existingConfiguration = false;

            if (existingExternalIdentifierMap.Count > 0)
            {
                Console.WriteLine("USING an existing Mapping configuration");
                for(int i=0 ; i < existingExternalIdentifierMap.Count; i++)
                    Console.WriteLine("   " + i + " --> " + existingExternalIdentifierMap[i].Name);

                mappingConfigurationManagerDialogViewModel.CreateNewMappingConfigurationChecked = false;
                mappingConfigurationManagerDialogViewModel.SelectedExternalIdentifierMap = existingExternalIdentifierMap[0];

                existingConfiguration = true;
            }
            else
            {
                Console.WriteLine("Creating a NEW Mapping configuration");
                mappingConfigurationManagerDialogViewModel.CreateNewMappingConfigurationChecked = true;
                //mappingConfigurationManagerDialogViewModel.NewExternalIdentifierMapName = "SC_only geometry Configuration";
                mappingConfigurationManagerDialogViewModel.NewExternalIdentifierMapName = "SCbase Configuration";
            }

            mappingConfigurationManagerDialogViewModel.ApplyCommand.ExecuteAsyncTask(null).Wait();


            // Opening the STEP-TAS file
            //DstLoadFileViewModel dstLoadFileViewModel = (DstLoadFileViewModel) AppContainer.Container.Resolve<IDstLoadFileViewModel>();
            //dstLoadFileViewModel.FilePath = "D:\\SC_only geometry.stp";
            //
            //dstLoadFileViewModel.LoadFileCommand.Execute(null);
            //mappingConfigurationManagerDialogViewModel.ApplyCommand.Execute(null);




            // We transfer the mappings that were defined... at the same times....
            DstTransferControlViewModel dstTransferControlViewModel = (DstTransferControlViewModel)AppContainer.Container.Resolve<ITransferControlViewModel>();

            MappingViewModel mappingViewModel = (MappingViewModel)AppContainer.Container.Resolve<IMappingViewModel>();


            if (!existingConfiguration)
            {
                if (mappingNumber==1)
                {
                    DeclareMapping("SC/SC_Body/", "PL_RAD_1", "OBC");
                    DeclareMapping("SC/SC_Body/", "PL_RAD_3", "PCDU");
                    DeclareMapping("SC/SA_DEPL/SA_DEPL_PY/", "SA_PANEL_1", "Radiator");
                }
                else if(mappingNumber==2)
                {
                    DeclareMapping("SC/SC_Body/", "PL_RAD_1", "OBC","OBC1", "Launch mode");
                    DeclareMapping("SC/SC_Body/", "PL_RAD_5", "OBC", "OBC2", "Launch mode");   // To define the mapping on the ED (without affecting the one defined on OBC1 just before
                    DeclareMapping("SC/SC_Body/", "PL_RAD_3", "PCDU");
                    DeclareMapping("SC/SA_DEPL/SA_DEPL_PY/", "SA_PANEL_1", "Radiator");
                }
            }
            else
            {
                if (mappingNumber==1)
                {
                    DeclareMapping("SC/SC_Body/", "PL_RAD_3", "OBC");
                    DeclareMapping("SC/SC_Body/", "PL_RAD_5", "Radiator");
                    DeclareMapping("SC/", "SA_DEPL", "Satellite");
                    DeclareMapping("SC/SA_DEPL/SA_DEPL_PY/", "SA_PANEL_1", "ELEMENT_CREATED_FROM_ADAPTER");
                }
                else if (mappingNumber == 2)
                {
                    DeclareMapping("SC/SC_Body/", "PL_RAD_3", "OBC", "OBC1", "Launch mode");
                    DeclareMapping("SC/SC_Body/", "PL_RAD_9", "OBC", "OBC2", "Launch mode");   // To define the mapping on the ED (without affecting the one defined on OBC1 just before
                    DeclareMapping("SC/SC_Body/", "PL_RAD_1", "PCDU");
                    DeclareMapping("SC/SC_Body/", "PL_RAD_7", "Radiator");   
                    DeclareMapping("SC/SA_DEPL/SA_DEPL_PY/", "SA_PANEL_1", "ELEMENT_CREATED_FROM_ADAPTER");
                }
            }

            // We deselect and select the  mappings  :  --> At the end everything is selected!!!!
            HubNetChangePreviewViewModel hubNetChangePreviewViewModel = (HubNetChangePreviewViewModel)AppContainer.Container.Resolve<IHubNetChangePreviewViewModel>();
            hubNetChangePreviewViewModel.UpdateTree(false);

            hubNetChangePreviewViewModel.DeselectAllCommand.ExecuteAsyncTask(null).Wait();
            hubNetChangePreviewViewModel.SelectAllCommand.ExecuteAsyncTask(null).Wait();


            //hubNetChangePreviewViewModel.ComputeValues();   // For code coverage

            





            // We transfer the mappings that were defined... at the same times....
            //DstTransferControlViewModel dstTransferControlViewModel = (DstTransferControlViewModel)AppContainer.Container.Resolve<ITransferControlViewModel>();
            dstTransferControlViewModel.TransferCommand.ExecuteAsyncTask(null).Wait();


            

            HubFileStoreBrowserViewModel hubFileStoreBrowserViewModel = (HubFileStoreBrowserViewModel) AppContainer.Container.Resolve<IHubFileStoreBrowserViewModel>();

            for(int i=0;i< hubFileStoreBrowserViewModel.HubFiles.Count;i++)
            {
                Console.WriteLine("FilePath   " + hubFileStoreBrowserViewModel.HubFiles[i].FilePath);
                Console.WriteLine("RevisonNumber   " + hubFileStoreBrowserViewModel.HubFiles[i].RevisionNumber);
                 
            }
           

            hubFileStoreBrowserViewModel.CurrentHubFile = hubFileStoreBrowserViewModel.HubFiles[0];
            hubFileStoreBrowserViewModel.DownloadFileCommand.ExecuteAsyncTask(null).Wait(); 
            hubFileStoreBrowserViewModel.CompareFileCommand.ExecuteAsyncTask(null).Wait(); 
            
            Console.WriteLine("After CompareFileCommand");


            


            /////////////////////////////////////////
            // Cover of IHubObjectBrowserViewModel //
            /////////////////////////////////////////
            HubObjectBrowserViewModel hubObjectBrowserViewModel = (HubObjectBrowserViewModel) AppContainer.Container.Resolve<IHubObjectBrowserViewModel>();
            hubObjectBrowserViewModel.BuildTrees();
            hubObjectBrowserViewModel.UpdateTree(true);
            hubObjectBrowserViewModel.Reload();

            Console.WriteLine("Nb entry in tree = " + hubObjectBrowserViewModel.Things.Count);
            foreach(var entity in hubObjectBrowserViewModel.Things)
            {
                //Console.WriteLine("entity.Name = " + entity.Name + "  " + entity);
                if (entity is ElementDefinitionsBrowserViewModel eds)
                {
                    foreach (var subentity in eds.ContainedRows)
                    {
                        hubObjectBrowserViewModel.SelectedThing = subentity;
                        hubObjectBrowserViewModel.SelectedThings.Clear();
                        hubObjectBrowserViewModel.SelectedThings.Add(subentity);
                        //Console.WriteLine("subentity.type = " + subentity);
                        hubObjectBrowserViewModel.PopulateContextMenu();

                        foreach (var subsubentity in subentity.ContainedRows)
                        {
                            hubObjectBrowserViewModel.SelectedThing = subsubentity;
                            hubObjectBrowserViewModel.SelectedThings.Clear();
                            hubObjectBrowserViewModel.SelectedThings.Add(subsubentity);
                            //Console.WriteLine(" subsubentity.type = " + subsubentity);
                            hubObjectBrowserViewModel.PopulateContextMenu();

                            foreach (var subsubsubentity in subsubentity.ContainedRows)
                            {
                                hubObjectBrowserViewModel.SelectedThing = subsubsubentity;
                                hubObjectBrowserViewModel.SelectedThings.Clear();
                                hubObjectBrowserViewModel.SelectedThings.Add(subsubsubentity);
                                //Console.WriteLine("   subsubsubentity.type = " + subsubsubentity);
                                hubObjectBrowserViewModel.PopulateContextMenu();
                            }
                        }
                    }                    
                }
            }

            /////////////////////////////////
            // Step File comparison tools  //     SC.STP leads to a crash
            /////////////////////////////////
            DstCompareStepFilesViewModel compareStepFilesViewModel = (DstCompareStepFilesViewModel)AppContainer.Container.Resolve<IDstCompareStepFilesViewModel>();
            
            compareStepFilesViewModel.SetFiles(pathToStepTasFiles+"SCbase.stp", pathToStepTasFiles + "SCbase.stp");  // Same files
            Assert.IsTrue(compareStepFilesViewModel.Process());

            compareStepFilesViewModel.SetFiles(pathToStepTasFiles + "SCupdated.stp", pathToStepTasFiles + "SCupdated.stp");  // Same files
            Assert.IsTrue(compareStepFilesViewModel.Process());

            compareStepFilesViewModel.SetFiles(pathToStepTasFiles + "SCbase.stp", pathToStepTasFiles + "SCupdated.stp");  // Same files
            Assert.IsFalse(compareStepFilesViewModel.Process());
            

            // Dst Extraction test
            DstExtractionViewModel dstExtractionViewModel = (DstExtractionViewModel)AppContainer.Container.Resolve<IDstExtractionViewModel>();

            dstExtractionViewModel.CurrentTemplateType = "mean consumed power";
            dstExtractionViewModel.CurrentFiniteState = "Launch mode";

            dstExtractionViewModel.EditTemplate.Execute(null); //Cmd();             //  To add code coverage
            dstExtractionViewModel.SaveCurrentTemplate.Execute(null); // Cmd();             //  To add code coverage

            //dstExtractionViewModel.ExtractCurrentTemplate.ExecuteAsyncTask(null).Wait();
            dstExtractionViewModel.ExtractCurrentTemplate.Execute(null);

            dstExtractionViewModel.TextValue = "";                           // To add code coverage
            dstExtractionViewModel.ExtractCurrentTemplate.Execute(null);     // To add code coverage




            // UPLOAD CSV test
            UploadCSVViewModel uploadCSVViewModel = (UploadCSVViewModel)AppContainer.Container.Resolve<IUploadCSVViewModel>();
                        
            
            //==============================================================================
            //Valid CSV file
            //==============================================================================
            uploadCSVViewModel.UploadCSVFileNameBox = "TestName"; //Not necessary but increases CodeCoverage
            uploadCSVViewModel.CSVFileName = pathToCSVFiles + "TEMP_SOLAR_P_fewNodes.csv";
            //
            uploadCSVViewModel.Analyze.Execute(null);
            if (uploadCSVViewModel.AnalysisDone)
            {
                uploadCSVViewModel.TimesSelectionTypeIdx = 0;
                uploadCSVViewModel.TimesSelectionType = "All 15 times";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.FiniteStateSelectedIdx = 0; //No FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "-";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 1;
                uploadCSVViewModel.TimesSelectionType = "Time range";
                uploadCSVViewModel.CSVTimesMin = "0";
                uploadCSVViewModel.CSVTimesMax = "100";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.FiniteStateSelectedIdx = 1; //Adding a FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "Launch mode";
                //uploadCSVViewModel.CSVTimesField.Execute(null);//Not necessary but increases CodeCoverage. Error, does not work
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null);
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 1;
                uploadCSVViewModel.TimesSelectionType = "Time range";
                uploadCSVViewModel.CSVTimesMin = "20";
                uploadCSVViewModel.CSVTimesMax = "100";
                uploadCSVViewModel.CSVTimesInc = "20";
                uploadCSVViewModel.BCheckBox = true;
                uploadCSVViewModel.FiniteStateSelectedIdx = 2; //Changing to a different FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "Nominal mode";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 2;
                uploadCSVViewModel.TimesSelectionType = "Single time";
                uploadCSVViewModel.CSVTimesMin = "20";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.FiniteStateSelectedIdx = 0; //Back to no FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "-";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null);
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 1;
                uploadCSVViewModel.TimesSelectionType = "Time range";
                uploadCSVViewModel.CSVTimesMin = "wrongmin";
                uploadCSVViewModel.CSVTimesMax = "wrongmax";
                uploadCSVViewModel.CSVTimesInc = "wronginc";
                uploadCSVViewModel.BCheckBox = true;
                uploadCSVViewModel.FiniteStateSelectedIdx = 3; //Back to another FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "Safe mode";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 1;
                uploadCSVViewModel.TimesSelectionType = "Time range";
                uploadCSVViewModel.CSVTimesMin = "-10";
                uploadCSVViewModel.CSVTimesMax = "500";
                uploadCSVViewModel.CSVTimesInc = "1000";
                uploadCSVViewModel.BCheckBox = true;
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
            }

            

            //
            //==============================================================================
            //Invalid CSV files
            //==============================================================================
            uploadCSVViewModel.CSVFileName = pathToCSVFiles + "TEMP_SOLAR_P_error.csv";
            uploadCSVViewModel.Analyze.Execute(null);
            //
            if (uploadCSVViewModel.AnalysisDone)
            {
                uploadCSVViewModel.TimesSelectionTypeIdx = 0;
                uploadCSVViewModel.TimesSelectionType = "All 5 times";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
            }
            //------------------------------------------------------------------------------
            uploadCSVViewModel.CSVFileName = pathToCSVFiles + "TEMP_SOLAR_P_error_no_column.csv";
            uploadCSVViewModel.Analyze.Execute(null);
            //------------------------------------------------------------------------------
            uploadCSVViewModel.CSVFileName = pathToCSVFiles + "TEMP_SOLAR_P_error_invalid_data.csv";
            uploadCSVViewModel.Analyze.Execute(null); 
            //------------------------------------------------------------------------------
            uploadCSVViewModel.CSVFileName = pathToCSVFiles + "I_do_not_exist.csv";
            uploadCSVViewModel.Analyze.Execute(null); 
                        
            //==============================================================================
            //CSV file with warnings
            //==============================================================================
            uploadCSVViewModel.CSVFileName = pathToCSVFiles + "TEMP_SOLAR_P_1.csv";
            uploadCSVViewModel.Analyze.Execute(null); 
            //
            if (uploadCSVViewModel.AnalysisDone)
            {
                uploadCSVViewModel.TimesSelectionTypeIdx = 0;
                uploadCSVViewModel.TimesSelectionType = "All 204 times";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
            }
            


            //==============================================================================
            //Other Valid CSV file  --> split on different ED/EU
            //==============================================================================
            uploadCSVViewModel.CSVFileName = pathToCSVFiles + "TEMP_SOLAR_P_1_mod2.csv";
            uploadCSVViewModel.Analyze.Execute(null);
            if (uploadCSVViewModel.AnalysisDone)
            {
                uploadCSVViewModel.TimesSelectionTypeIdx = 0;
                uploadCSVViewModel.TimesSelectionType = "All 204 times";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.FiniteStateSelectedIdx = 0; //No FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "-";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null);
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 1;
                uploadCSVViewModel.TimesSelectionType = "Time range";
                uploadCSVViewModel.CSVTimesMin = "0";
                uploadCSVViewModel.CSVTimesMax = "100";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.FiniteStateSelectedIdx = 1; 
                uploadCSVViewModel.FiniteStateSelectedValue = "Launch mode";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); //ReactiveCommand to call UploadCmd()
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 1;
                uploadCSVViewModel.TimesSelectionType = "Time range";
                uploadCSVViewModel.CSVTimesMin = "20";
                uploadCSVViewModel.CSVTimesMax = "100";
                uploadCSVViewModel.CSVTimesInc = "20";
                uploadCSVViewModel.BCheckBox = true;
                uploadCSVViewModel.FiniteStateSelectedIdx = 2; //Changing to a different FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "Nominal mode";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
                //
                uploadCSVViewModel.TimesSelectionTypeIdx = 2;
                uploadCSVViewModel.TimesSelectionType = "Single time";
                uploadCSVViewModel.CSVTimesMin = "20";
                uploadCSVViewModel.BCheckBox = false;
                uploadCSVViewModel.FiniteStateSelectedIdx = 0; //Back to no FiniteState
                uploadCSVViewModel.FiniteStateSelectedValue = "-";
                uploadCSVViewModel.UpdCSVTimesField();
                uploadCSVViewModel.Upload.Execute(null); 
            }


            Console.WriteLine(uploadCSVViewModel.UploadCSVTextBox);

            Console.WriteLine("... END OF TEST!");
        }


       

        [Test]
        public void WindowMain_ConnectDisconnect()
        {
            ApplicationOpen();
            
            IHubController hubController = AppContainer.Container.Resolve<IHubController>();
            OpenIterationOnHub(hubController, "EM1_Test_STEPTAS");

            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)AppContainer.Container.Resolve<IMainWindowViewModel>();

            /////////////////////////////
            // Disconnect from the hub //
            /////////////////////////////
            HubDataSourceViewModel hubDataSourceViewModel = (HubDataSourceViewModel)AppContainer.Container.Resolve<IHubDataSourceViewModel>();
            hubDataSourceViewModel.ConnectCommand.ExecuteAsyncTask(null).Wait();


            //////////////////////////////////////////////////////////////
            // Clean model files that were created on the HUB for test  //
            //////////////////////////////////////////////////////////////
            RemoveModelFromHub(hubController, "EM1_Test_STEPTAS");
            RemoveModelFromHub(hubController, "EM2_Test_STEPTAS");

        }


    }
}