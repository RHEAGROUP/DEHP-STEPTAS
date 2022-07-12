// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstExtractionPreviewModel.cs" company="Open Engineering S.A.">
//     Copyright (c) 2022 Open Engineering S.A.
//
//     Author: Ivan Fontaine, S. Paquay
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
// -

using DEHPSTEPTAS.ViewModel.Interfaces;
using System;

using System.Linq;

namespace DEHPSTEPTAS.ViewModel
{
    using Autofac;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal.Events;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
    using DEHPSTEPTAS.DstController;
    using DEHPSTEPTAS.Extraction;
    using DEHPSTEPTAS.Services.DstHubService;
    using DEHPSTEPTAS.Settings;
    using DEHPSTEPTAS.ViewModel.Dialogs;
    using DEHPSTEPTAS.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPTAS.Views.Dialogs;
    using ReactiveUI;
    using Scriban;
    using Scriban.Runtime;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public class DstExtractionViewModel : ReactiveObject, IDstExtractionViewModel
    {
        private readonly IHubController hubController;

        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        //private readonly IDstHubService dstHubService;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>

        /// </summary>
        public List<ParameterType> ParameterTypes { get; set; } = new();

        public ReactiveList<string> ParameterTypeNames { get; set; } = new();

        public ReactiveList<string> FiniteStateNames { get; set; } = new();

        Dictionary<string, HashSet<string>> StatesForTypes { get; set; } = new();


        //Dictionary<string, Object> EDForScript = new();   // ElementDefintions for scriban script
        //Dictionary<string, Object> EUForScript = new();   // ElementUsages for scriban script
        //Dictionary<int, Object> NodalFromEDForScript = new();
        //Dictionary<int, Object> NodalFromEUForScript = new();

        Dictionary<string, TasDataOnElementBase> EDForScript = new();   // ElementDefintions for scriban script
        Dictionary<string, TasDataOnElementBase> EUForScript = new();   // ElementUsages for scriban script
        Dictionary<string, NodalData> NodalFromEDForScript = new();
        Dictionary<string, NodalData> NodalFromEUForScript = new();



        //public List<CDP4Common.EngineeringModelData.Parameter> ModelData { get; set; } = new();
        //public List<CDP4Common.EngineeringModelData.Parameter> SourceReference { get; set; } = new();



        /// <summary>
        /// Holds the template edition text
        /// </summary>
        private string textvalue;

        public string TextValue { get => textvalue; set => this.RaiseAndSetIfChanged(ref this.textvalue, value); }

        /// <summary>
        /// Used to store the template content loaded from file.
        /// </summary>
        private string TemplateBody { get; set; }

        public bool HasTemplate(string type) { return true; }

        //public bool IsEditorDirty { get => textvalue.Length > 0 && !textvalue.Equals(TemplateBody); }
        
        private string currentTemplateType;
        public string CurrentTemplateType
        {
            get => currentTemplateType;
            set => this.RaiseAndSetIfChanged(ref this.currentTemplateType, value);
        }

        private string currentFiniteState;
        public string CurrentFiniteState
        {
           get => currentFiniteState;
           set => this.RaiseAndSetIfChanged(ref this.currentFiniteState, value);
        }



        public ReactiveCommand<Object> SaveCurrentTemplate { get; set; }

        public ReactiveCommand<Object> ExtractCurrentTemplate { get; set; }

        public ReactiveCommand<Object> PreviewCurrentTemplate { get; set; }// => throw new NotImplementedException();

        //        ReactiveCommand<Unit> IDstExtractionViewModel.ExtractCurrentTemplate => throw new NotImplementedException();

        private List<ElementDefinition> ListOfElements = new();
        public ReactiveCommand<Object> EditTemplate { get; set; }
        //public ReactiveCommand<Unit> EditTemplate { get; set; }


        public ReactiveCommand<Object> LoadTemplate { get; set; }



        private IUserPreferenceService<AppSettings> Preferences { get; set; }

        private INavigationService NavigationService;

        public IDstHubService DstHubService { get; set; }

        //   private Func<T, bool> AreTheseOwnedByTheDomain<T>() where T : IOwnedThing
        //=> x => x.Owner.Iid == this.hubController.CurrentDomainOfExpertise.Iid;

        public DstExtractionViewModel(INavigationService navigationService, IHubController hubController, IStatusBarControlViewModel statusBarControlView,
            IDstHubService dstHubService,
            IDstController dstController, IUserPreferenceService<AppSettings> userPreferenceService)   // SPA: Where this constructor is called? In App.xaml.cs? No. Either in HubDataSourceViewModel.cs or MainWindowViewModel.cs
        {
            this.NavigationService = navigationService;
            this.DstHubService = dstHubService;  
            this.hubController = hubController;
            this.Preferences = userPreferenceService;
            this.dstController = dstController;
            this.statusBar = statusBarControlView;

            this.WhenAnyValue(x => x.hubController.OpenIteration)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateElements());

          

            CDP4Dal.CDPMessageBus.Current.Listen<UpdateObjectBrowserTreeEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.Reset());

            TextValue = "";

            var canEdit = this.WhenAnyValue(vm => vm.CurrentTemplateType, (ct) => !string.IsNullOrEmpty(ct));
            //this.EditTemplate = ReactiveCommand.CreateAsyncTask(canEdit, x=> this.EditTemplateCmd());
            this.EditTemplate = ReactiveCommand.Create(canEdit);
            this.EditTemplate.Subscribe(_ => this.EditTemplateCmd());

            this.LoadTemplate = ReactiveCommand.Create();
            this.LoadTemplate.Subscribe(_ => this.LoadTemplateCmd());



            var canSave = this.WhenAnyValue(vm => vm.TextValue,vm => vm.CurrentTemplateType, 
                (text,currentTemplateType) => !string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(currentTemplateType));
            this.SaveCurrentTemplate = ReactiveCommand.Create(canSave);
            this.SaveCurrentTemplate.Subscribe(_ => this.SaveTemplateCmd());
            //var canPreview = this.WhenAnyValue(vm => vm.TextValue, (ct) => !string.IsNullOrEmpty(ct));

            var canextract = this.WhenAnyValue(vm => vm.dstController.IsFileOpen, vm => vm.CurrentTemplateType, (fo, tt) => (fo && !string.IsNullOrEmpty(tt)))
                .ObserveOn(RxApp.MainThreadScheduler);     // SPA: Why this considering that is does not appear for canEdit qnd canSave (due to call to dstController controller<)

            this.PreviewCurrentTemplate = ReactiveCommand.Create(canextract);
            this.PreviewCurrentTemplate.Subscribe(_ => this.PreviewTemplateCmd());
            // Simplified : PreviewCurrentTemplate.Subscribe(_ => PreviewTemplateCmd());  // SPA Is there a difference with the previous line?

            // this.ExtractCurrentTemplate = ReactiveCommand.Create();   // SPA: this line permits to avoid the Extract button to be greyed and inactive!!
            this.ExtractCurrentTemplate = ReactiveCommand.Create(canextract);
            this.ExtractCurrentTemplate.Subscribe(_ => this.ExtractTemplateCmd());


            // TO Update list of available State
            this.WhenAnyValue(x => x.CurrentTemplateType)
               .ObserveOn(RxApp.MainThreadScheduler)
               .Subscribe(_ => this.UpdateAvailableFiniteStates());



            // To update when we click on "Refresh" or "Reload" in "Hub data source" frame 
            CDP4Dal.CDPMessageBus.Current.Listen<SessionEvent>().Where(x => x.Status == SessionStatus.EndUpdate)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateElements());

        }

        // Set to public to be usable by tests
        public void UpdateAvailableFiniteStates()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Calling UpdateAvailableFiniteStates()");

            if (!string.IsNullOrWhiteSpace(currentTemplateType))
            {
                this.FiniteStateNames.Clear();
                
                foreach(var stateName in this.StatesForTypes[currentTemplateType])
                {
                    FiniteStateNames.Add(stateName);
                }
            }
        }


        /**
         * <summary>
         * Runs the template engine of the first parameter and displays the result in a dialog box
         * </summary>
         **/
        [ExcludeFromCodeCoverage]
        private void PreviewTemplateCmd()
        {
            Dictionary<string, string> res = ProcessTemplate(true);

            var vm = new DstExtractionPreviewViewModel();
            vm.SetPreviewResult(res.First().Value);
            var dialogResult = this.NavigationService.ShowDialog<DstExtractPreview, IDstExtractionPreviewViewModel>(vm);
        }

        
        private void ExtractTemplateCmd()
        {
            Dictionary<string, string> result = ProcessTemplate();
            foreach (string var in result.Keys)
            {
                if (result[var] == "") continue;

                if (!System.IO.Directory.Exists(Preferences.UserPreferenceSettings.PathToExtractionOutput))
                {
                    System.IO.Directory.CreateDirectory(Preferences.UserPreferenceSettings.PathToExtractionOutput);
                }
                string filetowrite = CreateResultName(var);
                System.IO.File.WriteAllText(filetowrite, result[var]);
            }
        }

        //  private ListElementData() { }

        private string CreateTemplateFileName(string type)
        {
            string templatepath = Preferences.UserPreferenceSettings.PathToExtractionTemplates + "/" + type + ".template";
            return templatepath;
        }

        private string CreateResultName(string type)
        {
            string resultpath = Preferences.UserPreferenceSettings.PathToExtractionOutput + "/" + type + ".ext";

            return resultpath;
        }

        
        private string LoadTemplateFile(string filetoload)
        {
            this.statusBar.Append($"Loading template from {filetoload}");
            return System.IO.File.ReadAllText(filetoload);
        }

        private void EditTemplateCmd()
        {
            string filetoload = CreateTemplateFileName(CurrentTemplateType);
            if (System.IO.File.Exists(filetoload))
            {
                TextValue = System.IO.File.ReadAllText(filetoload);
            }
        }

        [ExcludeFromCodeCoverage]
        private void LoadTemplateCmd()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Clicked on \"Load specialized template\" button");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.InitialDirectory = @"c:\"; // @"C:\Users\fontaine\Downloads\SC_Model_DP2\SC_Model_DP2\AC_Test_case";
            openFileDialog.Title = "Browse Template File";
            openFileDialog.DefaultExt = "template";
            openFileDialog.Filter = "template files (*.template)|*.template|All files (*.*)|*.*";
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                openFileDialog.FilterIndex = 0;
                openFileDialog.RestoreDirectory = true;
                string fileToLoad = openFileDialog.FileName;
                NLog.LogManager.GetCurrentClassLogger().Info("Selected file: " + fileToLoad);

                TextValue = System.IO.File.ReadAllText(fileToLoad);
            }
            else
            {
                NLog.LogManager.GetCurrentClassLogger().Info("Cancel button");
            }
        }


        private void SaveTemplateCmd()
        {
            if (TextValue == "" || CurrentTemplateType == "") return;
            if (!System.IO.Directory.Exists(Preferences.UserPreferenceSettings.PathToExtractionTemplates))
            {
                System.IO.Directory.CreateDirectory(Preferences.UserPreferenceSettings.PathToExtractionTemplates);
            }
            string filetowrite = CreateTemplateFileName(CurrentTemplateType);
            System.IO.File.WriteAllText(filetowrite, TextValue);
            this.statusBar.Append($"Template saved in  {filetowrite}");
        }

        private string ProcessTemplateParameters() //ParameterExtractionValueNew pe, ParameterReferenceValue pr)
        {
            var scriptObject = new ScriptObject();
            scriptObject.Add("stateName", CurrentFiniteState);
            scriptObject.Add("ED", EDForScript);
            scriptObject.Add("EU", EUForScript);
            scriptObject.Add("NodalFromED", NodalFromEDForScript);
            scriptObject.Add("NodalFromEU", NodalFromEUForScript);

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            var template = Template.Parse(TemplateBody);
            string res;
            try
            {
                res = template.Render(context);
            }
            catch (Exception e)
            {
                res = e.ToString();
            }
            this.statusBar.Append($"Template processed...");
            return res;
        }

        /**
         * <summary>
         * Process all the "candidate" parameters of the "CurrentTemplateType"
         * </summary>
         **/

        private Dictionary<string, string> ProcessTemplate(bool OnlyFirst = false)
        {
            if (TextValue.Length > 0)
            {
                TemplateBody = TextValue;
            }
            else
            {
                string filetoload = CreateTemplateFileName(CurrentTemplateType);
                TemplateBody = LoadTemplateFile(filetoload);
            }

            Dictionary<string, string> result = new();

            EDForScript.Clear();
            EUForScript.Clear();

            //var global = ExtractionGlobal.Instance;
            foreach (ElementDefinition elem in ListOfElements)
            {
                Parameter edTasRefParam= elem.Parameter.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");
                Parameter edParam = elem.Parameter.FirstOrDefault(x => x.ParameterType.Name == CurrentTemplateType);

                if (edParam is not null)
                {
                    EDForScript[elem.Name] = new TasDataOnElementBase(edTasRefParam, dstController.StepTASFile, edParam, CurrentFiniteState);
                    
                    var elementUsageOfCurrentED = elem.ReferencingElementUsages();

                    var euContained=elem.ContainedElement;

                    foreach (ElementUsage eu in elementUsageOfCurrentED)
                    {
                        ParameterOverride euParam = eu.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == CurrentTemplateType);
                        ParameterOverride euTasRefParam = eu.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");
                        if (euParam is not null)
                        {
                            if (euTasRefParam is not null)
                            {
                                EUForScript[eu.Name] = new TasDataOnElementBase(euTasRefParam, dstController.StepTASFile, euParam, CurrentFiniteState);
                            }
                            else
                            {
                                EUForScript[eu.Name] = new TasDataOnElementBase(edTasRefParam, dstController.StepTASFile, euParam, CurrentFiniteState);
                            }
                        }
                        else 
                        {
                            if (euTasRefParam is not null)
                            {
                                EUForScript[eu.Name] = new TasDataOnElementBase(euTasRefParam, dstController.StepTASFile, edParam, CurrentFiniteState);
                            }
                            else
                            {
                                EUForScript[eu.Name] = new TasDataOnElementBase(edTasRefParam, dstController.StepTASFile, edParam, CurrentFiniteState);
                            }
                        }
                    }
                }
            }

            // Nodal information from Element Definitions
            NodalFromEDForScript.Clear();
            foreach (var entry in EDForScript)
            {
                var edName = entry.Key;   // No used in the structure
                var data = entry.Value;
                double contribByNode = data.val / data.nodes.Count;
                foreach (var node in data.nodes)
                {
                    string nodeId = node.model + ":" + node.number;

                    if (!NodalFromEDForScript.ContainsKey(nodeId))
                        NodalFromEDForScript[nodeId] = new NodalData(contribByNode, data.name);
                    else
                        NodalFromEDForScript[nodeId].AppendContrib(contribByNode, data.name);
                }
            }

            // Nodal information from Element Usages
            NodalFromEUForScript.Clear();
            foreach (var entry in EUForScript)
            {
                var edName = entry.Key;   // No used in the structure
                var data = entry.Value;
                double contribByNode = data.val / data.nodes.Count;
                foreach (var node in data.nodes)
                {
                    string nodeId = node.model + ":" + node.number;

                    if (!NodalFromEUForScript.ContainsKey(nodeId))
                        NodalFromEUForScript.Add(nodeId, new NodalData(contribByNode, data.name));
                        //NodalFromEUForScript[nodeId] = new NodalData(contribByNode, data.name);
                    else
                        NodalFromEUForScript[nodeId].AppendContrib(contribByNode, data.name);
                }
            }

            result["steptas"] = ProcessTemplateParameters(); // null, null);

            return result;
        }

        private void Reset()
        {
            CurrentTemplateType = "";
            TextValue = "";
            UpdateElements();
        }

        /**
         *<summary>
         *Collects all the elementdefinition that contain a StepTas Parameter, and then create a list of all the  parameter that are members
         *of that element definition.
         *</summary>
          **/
        // Set to public to be usable by tests
        public void UpdateElements()
        {
            if (this.hubController.OpenIteration == null)
            {
                return;
            }

            ParameterTypes = new();   // SPA: Why not a clear instead?
            ParameterTypeNames.Clear();
            ListOfElements.Clear();
            ListOfElements.AddRange(     // SPA: we add here Element Definitions that have a step tas parameter
               this.hubController.OpenIteration.Element.Where(x => x.Parameter.Any(p => this.DstHubService.IsSTEPTasParameterType(p.ParameterType)
               )));




                       


            StatesForTypes.Clear();

            HashSet<ParameterType> setOfTypes = new();     // SPA: To avoid duplicated entries
            foreach (ElementDefinition elem in ListOfElements)      // SPA: Loop also on ElementUsages.......
            {
                //var elemUsagesInED=elem.ContainedElement;   // test SPA to get the Element Usages in the Element Definition
                //var elemUsagesOfED = elem.ReferencingElementUsages(); // test

                foreach (Parameter p in elem.Parameter)
                {
                    if (!this.DstHubService.IsSTEPTasParameterType(p.ParameterType))
                    {
                        setOfTypes.Add(p.ParameterType);

                        if (!StatesForTypes.ContainsKey(p.ParameterType.Name))  // If the key does not exist in the Dictionnary, we create the pair
                        {
                            StatesForTypes.Add(p.ParameterType.Name, new HashSet<string>());
                        }

                        var stateDependence = p.StateDependence;

                        if (stateDependence is not null)
                        {
                            foreach(var state in stateDependence.ActualState)
                            {
                                StatesForTypes[p.ParameterType.Name].Add(state.Name);
                            }
                        }
                    }
                }
            };
            // if there is an empty list for type, we add "-"   
            foreach (var states in StatesForTypes)
            {
                if (states.Value.Count == 0)
                {
                    states.Value.Add("-");
                }
            }


            ParameterTypes.AddRange(setOfTypes.ToList());
            foreach (var typ in setOfTypes.Distinct())
            {
                ParameterTypeNames.Add(typ.Name);
            }
        }
        
    }

}