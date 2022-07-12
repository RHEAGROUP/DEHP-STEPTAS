// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UploadCSVViewModel.cs" company="Open Engineering S.A.">
//     Copyright (c) 2022 Open Engineering S.A.
//
//     Author: C. Henrard, S. Paquay
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

using DEHPSTEPTAS.ViewModel.Interfaces;
using System;

using System.Linq;

namespace DEHPSTEPTAS.ViewModel
{
    using Autofac;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
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
    using System.Reactive.Linq;
    /// OpenFileDialog
    using System.Windows.Forms;
    ///  StreamReader
    using System.IO;
    using System.Globalization;
    // To push transation to hub
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    public class UploadCSVViewModel : ReactiveObject, IUploadCSVViewModel
    {
        private readonly IHubController hubController;

        private readonly IDstController dstController;

        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// UploadCSV TextBox
        /// </summary>
        private string uploadcsvfilenamebox;
        public string UploadCSVFileNameBox { get => uploadcsvfilenamebox; set => this.RaiseAndSetIfChanged(ref this.uploadcsvfilenamebox, value); }

        /// <summary>
        /// UploadCSV TextBox
        /// </summary>
        private string uploadcsvtextbox;
        public string UploadCSVTextBox { get => uploadcsvtextbox; set => this.RaiseAndSetIfChanged(ref this.uploadcsvtextbox, value); }

        /// <summary>
        /// Used to store the CSV file name.
        /// </summary>
        private string csvfilename;
        public string CSVFileName { get => csvfilename; set => this.RaiseAndSetIfChanged(ref this.csvfilename, value); }

        public ReactiveCommand<Object> Browse { get; set; }
        public ReactiveCommand<Object> Analyze { get; set; }
        public ReactiveCommand<Object> Upload { get; set; }
        //public ReactiveCommand<Unit> Upload { get; set; }
        public ReactiveCommand<Object> CSVTimesField { get; set; }

        private bool analysisDone;
        public bool AnalysisDone { get => analysisDone; set => this.RaiseAndSetIfChanged(ref this.analysisDone, value); }

        //Enable the CSVTimesMin/Max/Inc TextFields?
        private bool bCSVTimesMin;
        public bool BCSVTimesMin { get => bCSVTimesMin; set => this.RaiseAndSetIfChanged(ref this.bCSVTimesMin, value); }
        private bool bCSVTimesMax;
        public bool BCSVTimesMax { get => bCSVTimesMax; set => this.RaiseAndSetIfChanged(ref this.bCSVTimesMax, value); }
        private bool bCSVTimesInc;
        public bool BCSVTimesInc { get => bCSVTimesInc; set => this.RaiseAndSetIfChanged(ref this.bCSVTimesInc, value); }

        //ComoBox Times fields...
        public ReactiveList<string> TimesSelectionNames { get; set; } = new();
        //... and the selected field in ComboBox
        private string timesSelectionType;
        public string TimesSelectionType
        {
            get { return timesSelectionType; }
            set
            {
                timesSelectionType = value;
                UpdCSVTimesField();
            }
        }
        private int timesSelectionTypeIdx;
        public int TimesSelectionTypeIdx { get => timesSelectionTypeIdx; set => this.RaiseAndSetIfChanged(ref this.timesSelectionTypeIdx, value); }

        //ComoBox Finite State...
        public ReactiveList<string> FiniteStateItemsSource { get; set; } = new();
        //... and the selected field in ComboBox
        private string finiteStateSelectedValue;
        public string FiniteStateSelectedValue
        {
            get { return finiteStateSelectedValue; }
            set
            {
                finiteStateSelectedValue = value;
            }
        }
        private int finiteStateSelectedIdx;
        public int FiniteStateSelectedIdx { get => finiteStateSelectedIdx; set => this.RaiseAndSetIfChanged(ref this.finiteStateSelectedIdx, value); }

        //Is "by" checkBox checked?
        private bool bCheckBox;
        public bool BCheckBox
        {
            get { return bCheckBox; }
            set
            {
                this.RaiseAndSetIfChanged(ref this.bCheckBox, value);
                BCSVTimesInc = value;
            }
        }

        private string csvTimesMin;
        public string CSVTimesMin { get => csvTimesMin; set => this.RaiseAndSetIfChanged(ref this.csvTimesMin, value); }
        private string csvTimesMax;
        public string CSVTimesMax { get => csvTimesMax; set => this.RaiseAndSetIfChanged(ref this.csvTimesMax, value); }
        private string csvTimesInc;
        public string CSVTimesInc { get => csvTimesInc; set => this.RaiseAndSetIfChanged(ref this.csvTimesInc, value); }
        private string csvFromTxt;
        public string CSVFromTxt { get => csvFromTxt; set => this.RaiseAndSetIfChanged(ref this.csvFromTxt, value); }
        private bool isVisibleTo;
        public bool IsVisibleTo { get => isVisibleTo; set => this.RaiseAndSetIfChanged(ref this.isVisibleTo, value); }
        private bool isVisibleBy;
        public bool IsVisibleBy { get => isVisibleBy; set => this.RaiseAndSetIfChanged(ref this.isVisibleBy, value); }

        private IUserPreferenceService<AppSettings> Preferences { get; set; }

        private List<ElementDefinition> ListOfElements = new();
        //private List<ElementUsage> ListOfElements = new();
        private int nbTimes = 0;
        private int nbNodes = 0;
        private int[] node_IDs = null;
        private string[] completeNodeIds = null;
        private double[] time_values = null;
        private double[,] csv_content = null;

        private INavigationService NavigationService;

        public IDstHubService DstHubService { get; set; }

        public UploadCSVViewModel(INavigationService navigationService,
                                  IHubController hubController,
                                  IStatusBarControlViewModel statusBarControlView,
                                  IDstHubService dstHubService,
                                  IDstController dstController,
                                  IUserPreferenceService<AppSettings> userPreferenceService)   // SPA: Where this constructor is called? In App.xaml.cs? No. Either in HubDataSourceViewModel.cs or MainWindowViewModel.cs
        {
            this.NavigationService = navigationService;
            this.DstHubService = dstHubService;
            this.hubController = hubController;
            this.Preferences = userPreferenceService;
            this.dstController = dstController;
            this.statusBar = statusBarControlView;

            UploadCSVTextBox = "";
            CSVFileName = "";
            TimesSelectionNames.Clear();
            TimesSelectionType = "";
            FiniteStateItemsSource.Clear();
            FiniteStateItemsSource.Add("-");
            FiniteStateSelectedValue = "-";
            FiniteStateSelectedIdx = 0;
            CSVFromTxt = "from";
            CSVTimesMin = "-";
            IsVisibleTo = true;
            IsVisibleBy = false;
            CSVTimesMax = "-";
            CSVTimesInc = "-";
            AnalysisDone = false;
            BCSVTimesMin = false;
            BCSVTimesMax = false;
            BCheckBox = false; //BCSVTimesInc will follow
            //UploadCSVFileNameBox = "Press the Browse button to select CSV file...";

            this.Browse = ReactiveCommand.Create();
            this.Browse.Subscribe(_ => this.BrowseCmd());

            //var canAnalyze = this.WhenAnyValue(
            //    vm => vm.CSVFileName,
            //    (ct) => !string.IsNullOrEmpty(ct)
            //);

            var canAnalyze = this.WhenAnyValue(
                vm => vm.dstController.IsFileOpen, vm => vm.CSVFileName,
                (fo, tt) => (fo && !string.IsNullOrEmpty(tt)))
             .ObserveOn(RxApp.MainThreadScheduler);

            this.Analyze = ReactiveCommand.Create(canAnalyze);
            this.Analyze.Subscribe(_ => this.AnalyzeCmd());

            //CH: add another criteria to allow upload with
            // this.hubController.OpenIteration != null
            // this.dstController.StepTASFile != null
            var canUpload = this.WhenAnyValue(
                vm => vm.AnalysisDone,
                vm => vm.hubController.OpenIteration,
                vm => vm.dstController.StepTASFile,
                (ad, oi, st) => ad
            //                             && oi != null // This does not seem to work, probably because there is no public object
            //                             && st != null // with RaiseAndSetIfChanged and the private object is readonly
            );
            this.Upload = ReactiveCommand.Create(canUpload);
            this.Upload.Subscribe(_ => this.UploadCmd());


            //this.Upload = ReactiveCommand.CreateAsyncTask(canUpload, x => this.UploadCmd());
        }

        public void UpdCSVTimesField()
        {
            // All {time_values.Count()} times
            // Time range
            // Single time
            if (TimesSelectionType != null)
            {
                if (TimesSelectionType.Contains("All"))
                {
                    BCSVTimesMin = false;
                    BCSVTimesMax = false;
                    CSVFromTxt = "from";
                    IsVisibleTo = true;
                    IsVisibleBy = false;
                    BCSVTimesInc = false;
                    CSVTimesMin = time_values[0].ToString();
                    CSVTimesMax = time_values.Last().ToString();
                }
                else if (TimesSelectionType.Contains("Single"))
                {
                    BCSVTimesMin = true;
                    BCSVTimesMax = false;
                    CSVFromTxt = "at";
                    IsVisibleTo = false;
                    IsVisibleBy = false;
                    BCSVTimesInc = false;
                }
                else if (TimesSelectionType.Contains("range"))
                {
                    BCSVTimesMin = true;
                    BCSVTimesMax = true;
                    CSVFromTxt = "from";
                    IsVisibleTo = true;
                    IsVisibleBy = true;
                    BCSVTimesInc = BCheckBox; //Enable the By field based on checkbox value
                }
            }
        }
        //==============================================================================================================
        /// <summary>
        /// Called when the Browse button is pressed
        /// </summary>
        [ExcludeFromCodeCoverage]
        private void BrowseCmd()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Clicked the browse button");
            NLog.LogManager.GetLogger("CSVUpload_Logger").Info("Clicked the browse button");
            OpenFileDialog openCSVFileDialog = new OpenFileDialog();
            //openCSVFileDialog.InitialDirectory = @"C:\Users\fontaine\Downloads\SC_Model_DP2\SC_Model_DP2\AC_Test_case";
            openCSVFileDialog.Title = "Browse CSV File";
            openCSVFileDialog.DefaultExt = "txt";
            openCSVFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openCSVFileDialog.CheckFileExists = true;
            openCSVFileDialog.CheckPathExists = true;
            openCSVFileDialog.Multiselect = false;
            if (openCSVFileDialog.ShowDialog() == DialogResult.OK)
            {
                openCSVFileDialog.FilterIndex = 0;
                openCSVFileDialog.RestoreDirectory = true;
                CSVFileName = openCSVFileDialog.FileName;
                UploadCSVFileNameBox = CSVFileName;
                //UploadCSVTextBox += "Browse: " + CSVFileName + "\n";
                NLog.LogManager.GetLogger("CSVUpload_Logger").Info("Selected file: " + CSVFileName);
                TimesSelectionNames.Clear();
                TimesSelectionType = "";
                FiniteStateItemsSource.Clear();
                FiniteStateItemsSource.Add("-");
                FiniteStateSelectedValue = "-";
                FiniteStateSelectedIdx = 0;
                CSVTimesMin = "-";
                CSVTimesMax = "-";
                CSVTimesInc = "-";
                AnalysisDone = false;
                //UploadCSVTextBox += "You can now click the Analyze button\n";
            }
            else
            {
                NLog.LogManager.GetLogger("CSVUpload_Logger").Info("Cancel button");
            }
        }

        //==============================================================================================================
        /**
         * <summary>
         * Called when the Analyze button is pressed
         * </summary>
         **/
        private void AnalyzeCmd()
        {
            UploadCSVTextBox += "*** BEGIN Analyzing CSV file ***\n";

            if (this.hubController.OpenIteration == null)
            {
                UploadCSVTextBox += "Error, you must first Connect to the Hub Data Source...\n";
                return;
            }
            if (this.dstController.StepTASFile == null)
            {
                UploadCSVTextBox += "Error, you must load the StepTas file before uploading...\n";
                return;
            }
            //Get list of Finite State --> to be added in drop down menu
            FiniteStateItemsSource.Clear();
            FiniteStateItemsSource.Add("-");
            var afsl = hubController.OpenIteration.ActualFiniteStateList;
            for (int i = 0; i < afsl.Count; i++)
            {
                foreach (var finiteState in afsl[i].ActualState)
                {
                    FiniteStateItemsSource.Add(finiteState.Name);
                }
            }
            FiniteStateSelectedIdx = 0;
            FiniteStateSelectedValue = "-";
            //
            string allTimes;
            NLog.LogManager.GetCurrentClassLogger().Info("Analyzing file: " + CSVFileName);
            NLog.LogManager.GetLogger("CSVUpload_Logger").Info("Analyzing file: " + CSVFileName);
            AnalysisDone = false;
            //
            //UploadCSVTextBox += "Analyzing file '" + CSVFileName + "'\n";
            //
            if (!ReadCSVFile(out nbTimes,
                             out nbNodes,
                             out node_IDs,
                             out completeNodeIds,
                             out time_values,
                             out csv_content)) return;
            TimesSelectionNames.Clear();
            allTimes = $"All {time_values.Count()} times";
            TimesSelectionNames.Add(allTimes);
            TimesSelectionNames.Add("Time range");
            TimesSelectionNames.Add("Single time");
            TimesSelectionType = allTimes;
            TimesSelectionTypeIdx = 0; //Only setting TimesSelectionType does not work
            UpdCSVTimesField();
            BCheckBox = false; //BCSVTimesInc will follow
            CSVTimesMin = time_values[0].ToString();
            CSVTimesMax = time_values.Last().ToString();
            AnalysisDone = true;

            UploadCSVTextBox += "*** END Analyzing CSV file ***\n";
        }

        //==============================================================================================================
        /**
         * <summary>
         * Method to read the CSV file.
         * Called by UploadCmd()
         * Returns:
         *    node_IDs[nbNodes]
         *    time_values[nbTimes]
         *    csv_content[nbTimes, nbNodes]
         * </summary>
         **/
        private bool ReadCSVFile(out int nbTimes,
                             out int nbNodes,
                             out int[] node_IDs,
                             out string[] completeNodeIDs,     // Add by SPA to consider ("model:nodeId")  
                             out double[] time_values,
                             out double[,] csv_content)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Entering ReadCSVFile");
            bool irc = false;
            StreamReader sr = null;
            nbTimes = 0;
            nbNodes = 0;
            node_IDs = null;
            completeNodeIDs = null;
            time_values = null;
            csv_content = null;
            try
            {
                sr = new StreamReader(CSVFileName);
                string currentLine;
                int countCol = 0, countLine = 0;
                // Skip 2 lines of header
                sr.ReadLine();
                sr.ReadLine();
                // Read column names
                currentLine = sr.ReadLine();
                // Count number of columns
                string[] csvLineFields = currentLine.Split(',');
                countCol = csvLineFields.Length;
                bool bDebug = false;
                if (countCol > 1)
                {
                    // Remove 1 for the Time list
                    nbNodes = countCol - 1;
                    if (bDebug)
                        UploadCSVTextBox += $"CSV file contains {countCol} columns with following headers:\n";
                    NLog.LogManager.GetCurrentClassLogger().Info($"CSV file contains {countCol} columns");
                }
                else
                {
                    UploadCSVTextBox += "Error, unable to count number of columns in CSV file\n";
                    NLog.LogManager.GetCurrentClassLogger().Error("Error, unable to count number of columns in CSV file");
                    sr.Close();
                    return irc;
                }
                //Read node IDs from header
                node_IDs = new int[nbNodes];
                completeNodeIDs = new string[nbNodes];

                List<bool> consideredColumns = new List<bool>();

                string[] CSVcolumnSubFields = new string[5];
                int nbIgnoredColumns = 0;
                countCol = 0;
                foreach (string CSVcolumnheader in csvLineFields)
                {
                    if (CSVcolumnheader.Length > 0)
                    {
                        if (bDebug) UploadCSVTextBox += "   " + CSVcolumnheader;
                        CSVcolumnSubFields = CSVcolumnheader.Split(':');

                        if (CSVcolumnSubFields.Length >= 2)
                        {
                            if (bDebug) UploadCSVTextBox += $"; Node ID = {CSVcolumnSubFields[2]}\n";
                            Int32.TryParse(CSVcolumnSubFields[2], out node_IDs[countCol]);
                            completeNodeIDs[countCol] = CSVcolumnSubFields[1] + ":" + CSVcolumnSubFields[2];



                            countCol++;
                        }
                        else if (countCol > 0)
                        {
                            if (bDebug) UploadCSVTextBox += "\n";
                            UploadCSVTextBox += $"Error, unable to find node ID in column header {CSVcolumnheader}\n";
                            UploadCSVTextBox += "Please check that column header contains 'xxx:<modelName>:<nodeID>'\n";
                            UploadCSVTextBox += "Aborting upload...'\n";
                            NLog.LogManager.GetCurrentClassLogger().Error("Error, unable to find node ID in column header");
                            NLog.LogManager.GetCurrentClassLogger().Error("Please check that column header contains 'xxx:xxx:<nodeID>'");
                            NLog.LogManager.GetCurrentClassLogger().Error("Aborting upload...'\n");
                            node_IDs = null;
                            nbNodes = 0;
                            sr.Close();
                            return irc;
                        }
                        else
                        {
                            //First column = time, does not contain node ID
                            if (bDebug) UploadCSVTextBox += "\n";
                        }
                    }
                    else
                    {
                        //UploadCSVTextBox += "<empty>; No node ID...\n";
                        nbIgnoredColumns++;
                        nbNodes--;
                    }
                }
                if (nbIgnoredColumns > 0)
                {
                    UploadCSVTextBox += $"Warning: ignoring {nbIgnoredColumns} empty columns...\n";
                }
                // currentLine will be null when the StreamReader reaches the end of file
                while ((currentLine = sr.ReadLine()) != null)
                {
                    countLine++;
                }
                nbTimes = countLine;
                //UploadCSVTextBox += $"CSV file contains {nbTimes} times\n";
                NLog.LogManager.GetCurrentClassLogger().Info($"CSV file contains {nbTimes} times\n");
                // Rewind CSV file to beginning
                sr.DiscardBufferedData();
                sr.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
                countLine = 0;
                // Skip 2 lines of header + column names (it was read before)
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                // Allocate tables CSVcontent to store all data
                time_values = new double[nbTimes];
                csv_content = new double[nbTimes, nbNodes];
                //UploadCSVTextBox += "Reading and storing CSV file data...\n";
                NLog.LogManager.GetCurrentClassLogger().Info("Reading and storing CSV file data...");
                while ((currentLine = sr.ReadLine()) != null)
                {
                    // Search, case insensitive, if the currentLine contains the searched keyword
                    csvLineFields = currentLine.Split(',');
                    countCol = 0;
                    double csv_one_double;
                    foreach (string CSVoneField in csvLineFields)
                    {
                        if (countCol > nbNodes) break;
                        //UploadCSVTextBox += $"Debug: Line {countLine + 4} Col {countCol}/{nbNodes} Time {time_values[countLine]} Field {CSVoneField}\n";
                        if (!Double.TryParse(CSVoneField,
                                        NumberStyles.Float,
                                        new CultureInfo("en-US"),
                                        out csv_one_double))
                        {
                            UploadCSVTextBox += $"Error: CSV file contains characters at line {countLine + 4} at time={time_values[countLine]})\n";
                            UploadCSVTextBox += $"that could not be converted to a valid number: '{CSVoneField}'.\n";
                            sr.Close();
                            return irc;
                        }
                        if (countCol == 0)
                        {
                            time_values[countLine] = csv_one_double;
                            if (countLine > 0)
                            {
                                if (time_values[countLine] == time_values[countLine - 1])
                                {
                                    UploadCSVTextBox += $"Warning: CSV file contains at line {countLine + 4} the same time value (={time_values[countLine]}) as the previous line.\n";
                                    NLog.LogManager.GetCurrentClassLogger().Warn($"Warning: CSV file contains at line {countLine + 4} the same time value (={time_values[countLine]}) as the previous line.");
                                }
                            }
                        }
                        //Ignore the last columns for whieh there was no node specification in the header
                        else
                        {
                            csv_content[countLine, countCol - 1] = csv_one_double;
                        }
                        countCol++;
                    }
                    countLine++;
                }
                //UploadCSVTextBox += "Finished reading and storing CSV file data...!\n";
                NLog.LogManager.GetCurrentClassLogger().Info("Finished reading and storing CSV file data...!");
                irc = true;
            }
            catch (System.IO.IOException csvReadError)
            {
                UploadCSVTextBox += "Error while reading CSV file. Error message:\n";
                UploadCSVTextBox += csvReadError.Message + "\n";
                NLog.LogManager.GetCurrentClassLogger().Error("Error while reading CSV file. Error message:");
                NLog.LogManager.GetCurrentClassLogger().Error(csvReadError.Message);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }
            return irc;
        }

        //==============================================================================================================
        /**
         * <summary>
         * Called when the Upload button is pressed
         * </summary>
         **/
        private void UploadCmd()
        {
            UploadCSVTextBox += "*** BEGIN Upload temperature results ***\n";

            FindStepTasNodesAndUpload(nbTimes,
                     nbNodes,
                     node_IDs,
                     time_values,
                     csv_content);

            UploadCSVTextBox += "*** END Upload temperature results ***\n";

        }

        //==============================================================================================================
        /**
         * <summary>
         * Main method called by UploadCmd() when user clicks on the Upload button
         * Goal is to:
         * - loop on ElementDefinition's with a StepTas reference
         * - find the ones having nodes in common with the ones declared in the CSV file
         * - compute statistical data on those elements (min/max/average for the group of nodes)
         * - send those to the hub (either ElementDefinition or ElementUsage is StepTas is overridden)
         * Input:
         *    node_IDs[nbNodes]
         *    time_values[nbTimes]
         *    csv_content[nbTimes, nbNodes]
         * Output:
         *    <none>
         * </summary>
         **/
        private void FindStepTasNodesAndUpload(int nbTimes,
                                      int nbNodes,
                                      int[] node_IDs,
                                      double[] time_values,
                                      double[,] csv_content)
        {
            



            //UploadCSVTextBox += "Searching for elements in StepTas file with identical nodes to CSV file...\n";
            NLog.LogManager.GetCurrentClassLogger().Info("Searching for elements in StepTas file with identical nodes to CSV file...");
            ListOfElements.Clear();
            ListOfElements.AddRange(     // SPA: we add here Element Definitions that have a step tas parameter
               this.hubController.OpenIteration.Element.Where(
                   x => x.Parameter.Any(p => this.DstHubService.IsSTEPTasParameterType(p.ParameterType))));

            if (ListOfElements.Count == 0)
            {
                UploadCSVTextBox += "No Element Definition with 'step tas reference' in the Engineering Model... Nothing to upload!\n";
                return;
            }


            bool needRefresh = false;

          
            bool dataResampled = false;    // To make it only once!!!!
            int nb_resampled_times = 0 ;
            double[,] csv_resampled_content = null;
            double[] resampled_time_values = null;

            foreach (ElementDefinition ed in ListOfElements)
            {
                NLog.LogManager.GetCurrentClassLogger().Info($"Considering ElementDefinition  {ed.Name}");


                if (ed is null)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error("Critical error, ElementDefinition ed is null in first loop");
                    UploadCSVTextBox += "Error: ed is null in first loop on ElementDefinitions. Aborting upload...\n";
                    return;
                }
                //Search if the ElementDefinition contains a "step tas reference"
                Parameter edSteptasPrm = ed.Parameter.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");
                if (edSteptasPrm is null)
                {
                    //Skip to next ElementDefinition since the current one does not have a StepTas reference.  <-- It should never happen
                    continue;     
                }



                //-------------------------------------------------
                // Declare/Create temperature results in ED / EU
                //-------------------------------------------------



                bool needToHaveDeclaredTemperatureResultsInED = false;
                bool needToHaveComputedTemperatureResultsInED = false;
                bool needToHaveOveriddenResults = false;
                bool needToSendTemperatures = false;



                //First check in the element definition itself
                {
                    TasDataOnElementBase PrmRefVal = new TasDataOnElementBase(edSteptasPrm, dstController.StepTASFile, null, FiniteStateSelectedValue);
                    UploadCSVTextBox += $"ElementDefinition name '{ed.Name}' with {PrmRefVal.nodes.Count} node(s)\n";
                    MatchingNodesWithSteptas(in PrmRefVal, out int nbNodIdx, out int[] node_indices);
                    if (nbNodIdx > 0)
                    {
                        UploadCSVTextBox += $"   ==> CSV file contains references to following {nbNodIdx} node(s) in element:\n      ";
                        int maxNodePerLine = 4;
                        for (int nodIdx = 0; nodIdx < nbNodIdx; nodIdx++)
                        {
                            UploadCSVTextBox += $"{completeNodeIds[node_indices[nodIdx]]}";
                            if (nodIdx % maxNodePerLine == (maxNodePerLine - 1) && nodIdx < nbNodIdx - 1)
                                UploadCSVTextBox += "\n      ";
                            else
                                UploadCSVTextBox += "  ";
                        }
                        UploadCSVTextBox += "\n";

                        needToHaveDeclaredTemperatureResultsInED = true;
                        needToHaveComputedTemperatureResultsInED = true;
                        needToSendTemperatures = true;
                    }
                }

                NLog.LogManager.GetCurrentClassLogger().Info($"Stage 1");


                // Next we check for related Element Usages
                var elementUsageOfCurrentED = ed.ReferencingElementUsages();
                if (elementUsageOfCurrentED is not null)
                {
                    foreach (ElementUsage eu in elementUsageOfCurrentED)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Info($"Considering ElementUsage  {eu.Name}");

                        ParameterOverride euTasRefParam = eu.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");
                        if (euTasRefParam is not null)
                        {
                            TasDataOnElementBase PrmRefVal = new TasDataOnElementBase(euTasRefParam, dstController.StepTASFile, null, FiniteStateSelectedValue);
                            UploadCSVTextBox += $"ElementUsage name '{eu.Name}' with {PrmRefVal.nodes.Count} node(s)\n";
                            MatchingNodesWithSteptas(in PrmRefVal, out int nbNodIdx, out int[] node_indices);
                            if (nbNodIdx > 0)
                            {
                                UploadCSVTextBox += $"   ==> CSV file contains references to following {nbNodIdx} node(s) in element:\n      ";
                                int maxNodePerLine = 4;
                                for (int nodIdx = 0; nodIdx < nbNodIdx; nodIdx++)
                                {
                                    UploadCSVTextBox += $"{completeNodeIds[node_indices[nodIdx]]}";
                                    if (nodIdx % maxNodePerLine == (maxNodePerLine - 1) && nodIdx < nbNodIdx - 1)
                                        UploadCSVTextBox += "\n      ";
                                    else
                                        UploadCSVTextBox += "  ";
                                }
                                UploadCSVTextBox += "\n";

                                needToHaveDeclaredTemperatureResultsInED = true;
                                needToHaveOveriddenResults = true;
                                needToSendTemperatures = true; 
                            }
                        }
                    }
                }

                NLog.LogManager.GetCurrentClassLogger().Info($"Stage 2");

                // If we need temperature results in ED we check if they exist and we create them if they do not exist 
                if (needToHaveDeclaredTemperatureResultsInED is true)
                {
                    this.UpdateParamIfNeeded(ed, null, needRefresh);
                }


                NLog.LogManager.GetCurrentClassLogger().Info($"Stage 3");

                // After we are sure that the results are created in the ED we can create them in EU
                if (elementUsageOfCurrentED is not null && needToHaveOveriddenResults == true)
                {
                    foreach (ElementUsage eu in elementUsageOfCurrentED)
                    {
                        ParameterOverride euTasRefParam = eu.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");
                        if (euTasRefParam is not null)
                        {
                            TasDataOnElementBase PrmRefVal = new TasDataOnElementBase(euTasRefParam, dstController.StepTASFile, null, FiniteStateSelectedValue);
                            MatchingNodesWithSteptas(in PrmRefVal, out int nbNodIdx, out int[] node_indices);
                            if (nbNodIdx > 0)
                            {
                                this.UpdateParamIfNeeded(null, eu, needRefresh);
                            }
                        }
                    }
                }

                NLog.LogManager.GetCurrentClassLogger().Info($"Stage 4");



                if (!needToSendTemperatures)  // For the considered Element Definition
                {
                    UploadCSVTextBox += $"  Nothing to upload for Element Definition '{ed.Name}'\n";
                    continue;
                }

                NLog.LogManager.GetCurrentClassLogger().Info($"Stage 5");

                //-------------------------------------------------------------------------------------
                // Crop and resample data since they will need to be sent later
                //-------------------------------------------------------------------------------------


                if (dataResampled == false)
                {
                    CropAndResampleData(in nbTimes,
                              in nbNodes,
                              in time_values,
                              in csv_content,
                              out nb_resampled_times,
                              out csv_resampled_content,
                              out resampled_time_values);

                    dataResampled = true;
                }

                NLog.LogManager.GetCurrentClassLogger().Info($"Stage 6");

                //-------------------------------------------------------------------------------------
                // Send temperatures to hub
                //-------------------------------------------------------------------------------------


                if (needToHaveComputedTemperatureResultsInED)
                {
                    // The model with a StepTas reference with state dependence only sends the temperatures if we select the mode that's currently mapped
                    //var PrmRefVal = new ParameterReferenceValue(edSteptasPrm, dstController.StepTASFile);
                    TasDataOnElementBase PrmRefVal = new TasDataOnElementBase(edSteptasPrm, dstController.StepTASFile, null, FiniteStateSelectedValue);
                    
                    MatchingNodesWithSteptas(in PrmRefVal, out int nbNodIdx, out int[] node_indices);
                    if (nbNodIdx > 0)
                    {
                        UploadCSVTextBox += $"Uploading temperatures for ElementDefinition name '{ed.Name}' with {PrmRefVal.nodes.Count} node(s)\n";

                        ComputeAverage(in nb_resampled_times,
                                       in nbNodes,
                                       in nbNodIdx,
                                       in resampled_time_values,
                                       in node_IDs,
                                       in node_indices,
                                       in csv_resampled_content,
                                       out double dmin,
                                       out double dmax,
                                       out double[] davg_t);
                        SendTemperaturesToHub(ed, null, resampled_time_values, dmin, dmax, davg_t);
                    }
                }

                NLog.LogManager.GetCurrentClassLogger().Info($"Stage 7");

                if (elementUsageOfCurrentED is not null && needToHaveOveriddenResults == true)
                {
                    foreach (ElementUsage eu in elementUsageOfCurrentED)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Info($"Considering ElementUsage  {eu.Name}");
                        
                        //Check if the ElementUsage has a "step tas reference" parameter override
                        ParameterOverride euTasRefParam = eu.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == "step tas reference");
                        if (euTasRefParam is not null)
                        {
                            TasDataOnElementBase PrmRefVal = new TasDataOnElementBase(euTasRefParam, dstController.StepTASFile, null, FiniteStateSelectedValue);
                            
                            MatchingNodesWithSteptas(in PrmRefVal, out int nbNodIdx, out int[] node_indices);
                            if (nbNodIdx > 0)
                            {
                                UploadCSVTextBox += $"Uploading temperatures for ElementUsage name '{eu.Name}'\n";
                                
                                ComputeAverage(in nb_resampled_times,
                                               in nbNodes,
                                               in nbNodIdx,
                                               in resampled_time_values,
                                               in node_IDs,
                                               in node_indices,
                                               in csv_resampled_content,
                                               out double dmin,
                                               out double dmax,
                                               out double[] davg_t);
                                SendTemperaturesToHub(null, eu, resampled_time_values, dmin, dmax, davg_t);
                            }
                        }
                    }
                }

                NLog.LogManager.GetCurrentClassLogger().Error($"End of current ED");
            }

        }

        //==============================================================================================================
        private void MatchingNodesWithSteptas(in TasDataOnElementBase PrmRefVal,
                                              out int nbNodIdx,
                                              out int[] node_indices)
        {
            // If the 2 lists are getting large and performance is an issue
            // check if this solution can be applied:
            // https://stackoverflow.com/questions/12795882/quickest-way-to-compare-two-generic-lists-for-differences

            // TO DO: Generalize implementation  with completeNodeIds  (model:nodeId)

            node_indices = new int[completeNodeIds.Count()]; //list of indices in completesNodeIds that have a match with PrmRefVal.nodes (StepTAS nodes)
            nbNodIdx = 0;   //useful length of node_indices (max size = total number of nodes)
            int nodIdx = 0; //iterator index in node_IDs
            foreach(string node1Id in completeNodeIds)
            {
                foreach (var node2 in PrmRefVal.nodes)
                {
                    string node2Id = node2.model + ":" + node2.number;
                    if (node2Id == node1Id) 
                    {
                        node_indices[nbNodIdx] = nodIdx;
                        nbNodIdx++;
                    }
                }
                nodIdx++;
            }
            return;

            /*node_indices = new int[node_IDs.Count()]; //list of indices in node_IDs that have a match in PrmRefVal.nodes (StepTAS nodes)
            nbNodIdx = 0;   //useful length of node_indices (max size = total number of nodes)
            int nodIdx = 0; //iterator index in node_IDs
            foreach (int node1 in node_IDs)
            {
                foreach (var node2 in PrmRefVal.nodes)
                {
                    //[CH] should we also compare the meshedsurface (which seems empty)?
                    //     If so, is this information also contained in CSV file header, e.g., T:SG_DEP:12217001 ==> SG_DEP?
                    int node2num = 0;
                    if (Int32.TryParse(node2.number, out node2num) && node2num == node1)
                    {
                        node_indices[nbNodIdx] = nodIdx;
                        nbNodIdx++;
                    }
                }
                nodIdx++;
            }
            return;
            */
        }

        //==============================================================================================================
        // Inspired from TestHUBMethod method in DstHubService.cs
        private void UpdateParamIfNeeded(ElementDefinition ed, ElementUsage eu, bool needRefresh)
        {
            var rdl = DstHubService.GetReferenceDataLibrary();
            //var rdl = hubController.GetDehpOrModelReferenceDataLibrary().Clone(false);
            var parameters = rdl.QueryParameterTypesFromChainOfRdls();
            ParameterOrOverrideBase param;
            ElementDefinition edCloned = null;
            ElementUsage euCloned = null;
            if (ed is null && eu is null)
                return;
            if (ed is not null)
                edCloned = ed.Clone(true);
            else
                euCloned = eu.Clone(true);

            // Create 3 temperature fields if they do not exist (with the correct StateDependency)
            // ------------------------------------------------
            foreach (string fieldName in new[] { "sampled temperature", "minimum temperature", "maximum temperature" })
            //foreach (string fieldName in new[] { "maximum temperature" })
            {
                if (ed is not null)
                    param = edCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == fieldName);
                else //eu is not null
                    param = euCloned.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == fieldName);
                if (param is null)
                {
                    // one transaction for each parameters
                    CDP4Dal.Operations.ThingTransaction transaction = null;
                    if (ed is not null)
                        transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(edCloned), edCloned);
                    else //eu is not null
                        transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(euCloned), euCloned);
                    ParameterType paramType;
                    if (fieldName != "sampled temperature")
                    {
                        paramType = parameters.OfType<TextParameterType>().FirstOrDefault(x => x.Name == fieldName && !x.IsDeprecated);
                    }
                    else
                    {
                        paramType = parameters.OfType<SampledFunctionParameterType>().FirstOrDefault(x => x.Name == fieldName && !x.IsDeprecated);
                    }

                    if (eu is not null)
                    {
                        Parameter paramED = eu.ElementDefinition.Parameter.FirstOrDefault(x => x.ParameterType.Name == fieldName);

                        ParameterOverride newTemperatureParamOver = new ParameterOverride(Guid.NewGuid(), null, null)
                        {
                            Parameter = paramED,  // It means that the ED parameter should exist before
                            Owner = this.hubController.CurrentDomainOfExpertise
                        };



                        // No need to definie parameter override dependence : exclude also!!!!!

                        /*
                        if (FiniteStateSelectedValue != "-")
                        {
                            var tmp = hubController.OpenIteration.ActualFiniteStateList;
                            var tmp2 = tmp[0];

                            newTemperatureParamOver.StateDependence = this.hubController.OpenIteration.ActualFiniteStateList[0];
                        }
                        */



                        euCloned.ParameterOverride.Add(newTemperatureParamOver);

                        transaction.CreateOrUpdate(newTemperatureParamOver);
                    }
                    else
                    {
                        Parameter newTemperatureParam = new Parameter(Guid.NewGuid(), null, null)
                        {
                            ParameterType = paramType,
                            Owner = this.hubController.CurrentDomainOfExpertise
                        };

                        if (FiniteStateSelectedValue != "-")
                            newTemperatureParam.StateDependence = this.hubController.OpenIteration.ActualFiniteStateList[0];

                        edCloned.Parameter.Add((Parameter)newTemperatureParam);

                        transaction.CreateOrUpdate(newTemperatureParam);
                    }




                    try
                    {
                        needRefresh = true;
                        //transaction.CreateOrUpdate(newTemperatureParam);
                        Task.Run(async () =>
                        {
                            await this.hubController.Write(transaction);
                                //await this.hubController.RefreshReferenceDataLibrary(rdl);
                            }).ContinueWith(task =>
                            {
                                if (!task.IsCompleted)
                                {
                                    NLog.LogManager.GetCurrentClassLogger().Error($"Error creating field {fieldName} because {task.Exception}");
                                }
                                else
                                {
                                    NLog.LogManager.GetCurrentClassLogger().Info($"Created field {fieldName} successfully");
                                }
                            }).Wait();
                    }
                    catch (Exception e)
                    {
                        UploadCSVTextBox += $"  Error creating field {fieldName}. Make sure the parameter is declared in the list of Parameter Types...\n";
                        UploadCSVTextBox += $"  Exception caught: {e}\n";
                        NLog.LogManager.GetCurrentClassLogger().Error($"Error creating field {fieldName} because {e}");
                        return;
                    }
                }
                else
                {
                    if ((param.StateDependence == null && FiniteStateSelectedValue != "-") ||
                        (param.StateDependence != null && FiniteStateSelectedValue == "-"))
                    {
                        var paramCloned = param.Clone(true);   //False?
                        var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);
                        if (FiniteStateSelectedValue != "-")
                        {
                            paramCloned.StateDependence = this.hubController.OpenIteration.ActualFiniteStateList[0];
                        }
                        else
                        {
                            paramCloned.StateDependence = null;
                        }
                        try
                        {
                            needRefresh = true;
                            transaction.CreateOrUpdate(paramCloned);
                            Task.Run(async () =>
                            {
                                await this.hubController.Write(transaction);
                                    //await this.HubController.RefreshReferenceDataLibrary(rdl);
                                }).ContinueWith(task =>
                                {
                                    if (!task.IsCompleted)
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Error($"Error updating field {fieldName} because {task.Exception}");
                                    }
                                    else
                                    {
                                        NLog.LogManager.GetCurrentClassLogger().Info($"Updated field {fieldName} successfully");
                                    }
                                }).Wait();
                        }
                        catch (Exception e)
                        {
                            UploadCSVTextBox += $"  Error during field update {fieldName}. Make sure the parameter is declared in the list of Parameter Types...\n";
                            UploadCSVTextBox += $"  Exception caught: {e}\n";
                            NLog.LogManager.GetCurrentClassLogger().Error($"Error updating field {fieldName} because {e}");
                        }
                    }
                }
            }
        }

        //==============================================================================================================
        /*private void RefreshHub()
        {
            try
            {
                Task.Run(async () =>
                {
                    await this.hubController.Refresh();
                }).ContinueWith(task =>
                {
                    if (!task.IsCompleted)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error($"Error updating element because {task.Exception}");
                    }
                    else
                    {
                        NLog.LogManager.GetCurrentClassLogger().Info($"Updated element");
                    }
                }).Wait();
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error($"Error updating element because {e}");
            }
        }*/

        //==============================================================================================================
        // Inspired from TestHUBMethod method in DstHubService.cs
        private void SendTemperaturesToHub(ElementDefinition ed, ElementUsage eu,
                                                 double[] time_values,
                                                 double dmin,
                                                 double dmax,
                                                 double[] davg_t)
        {
            ParameterOrOverrideBase param;
            ElementDefinition edCloned = null;
            ElementUsage euCloned = null;
            if (ed is null && eu is null)
                return;
            var rdl = DstHubService.GetReferenceDataLibrary();
            //var rdl = hubController.GetDehpOrModelReferenceDataLibrary().Clone(false);
            var parameters = rdl.QueryParameterTypesFromChainOfRdls();
            if (ed is not null)
                edCloned = ed.Clone(true);
            else
                euCloned = eu.Clone(true);

            // Define the values for the 3 temperature types
            foreach (string fieldName in new[] { "sampled temperature", "minimum temperature", "maximum temperature" })
            {
                if (ed is not null)
                    param = edCloned.Parameter.FirstOrDefault(x => x.ParameterType.Name == fieldName);
                else //eu is not null
                    param = euCloned.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == fieldName);
                var paramCloned = param.Clone(true);
                var stateDep = param.StateDependence;
                var transaction = new CDP4Dal.Operations.ThingTransaction(CDP4Dal.Operations.TransactionContextResolver.ResolveContext(paramCloned), paramCloned);

                IValueSet newValue;
                if (stateDep is not null)
                {
                    newValue = paramCloned.QueryParameterBaseValueSet(null, param.StateDependence.ActualState.Find(x => x.Name == finiteStateSelectedValue));
                }
                else
                {
                    newValue = paramCloned.QueryParameterBaseValueSet(null, null);
                }

                newValue.ValueSwitch = ParameterSwitchKind.COMPUTED;
                List<string> values = new();
                if (fieldName == "sampled temperature")
                {
                    var timeAndTemp = time_values.Zip(davg_t, (time, temp) => new { Time = time, Temp = temp });
                    foreach (var tt in timeAndTemp)
                    {
                        values.Add(tt.Time.ToString());    // time i
                        values.Add(tt.Temp.ToString());   // temperature i
                    }
                }
                else if (fieldName == "minimum temperature")
                {
                    values.Add(dmin.ToString());
                }
                else // if (fieldName == "maximum temperature")
                {
                    values.Add(dmax.ToString());
                }

                if (ed is not null)
                {
                    ((ParameterValueSet)newValue).Computed = new CDP4Common.Types.ValueArray<string>(values);
                    transaction.CreateOrUpdate((ParameterValueSet)newValue);
                }
                else
                {
                    ((ParameterOverrideValueSet)newValue).Computed = new CDP4Common.Types.ValueArray<string>(values);
                    transaction.CreateOrUpdate((ParameterOverrideValueSet)newValue);
                }

                try
                {
                    Task.Run(async () =>
                    {
                        await this.hubController.Write(transaction);
                    }).ContinueWith(task =>
                    {
                        if (!task.IsCompleted)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Error($"Error sending {fieldName} to HUB because {task.Exception}");
                        }
                        else
                        {
                            NLog.LogManager.GetCurrentClassLogger().Info($"Sent {fieldName} to HUB");
                        }
                    }).Wait();

                    UploadCSVTextBox += $"  Sent {fieldName} to HUB\n";
                }
                catch (Exception e)
                {
                    UploadCSVTextBox += $"  Error during upload of values...\n";
                    UploadCSVTextBox += $"  Exception caught: {e}\n";
                    NLog.LogManager.GetCurrentClassLogger().Error($"Error sending {fieldName} to HUB because {e}");
                }
            }
        }


        //==============================================================================================================
        /**
         * <summary>
         * Method to analyze the CSV data (that have been populated by ReadCSVFile)
         * Called by FindStepTasNodes()
         * Input:
         *    time_values[nbTimes]
         *    node_IDs[nbNodes]
         *    node_indices[nbNodes] but only nbNodIdx useful values
         *    csv_content[nbTimes, nbNodes]
         * Output:
         *    dmin = absolute minimum for all times and all node indices declared in node_indices[nbNodIdx]
         *    dmax = absolute maximum for all times and all node indices declared in node_indices[nbNodIdx]
         *    davg = average for all times of the average value of all node indices declared in node_indices[nbNodIdx]
         * </summary>
         **/
        private void CropAndResampleData(in int nbTimes,
                                         in int nbNodes,
                                         in double[] time_values,
                                         in double[,] csv_content,
                                         out int nb_resampled_times,
                                         out double[,] csv_resampled_content,
                                         out double[] resampled_time_values)
        {
            //Reading user input (tmin, tmax and tinc)
            //========================================
            //Default values
            double tmin = time_values[0];
            double tmax = time_values.Last();
            double tinc = -1.0;
            if (TimesSelectionType.Contains("All"))
            {
                UploadCSVTextBox += $"Uploading data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax}\n";
                NLog.LogManager.GetCurrentClassLogger().Info($"  Uploading data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax}");
            }
            else if (TimesSelectionType.Contains("range"))
            {
                StringToDouble(csvTimesMin, "minimum time field", time_values[0], "Using absolute minimum time instead", out tmin);
                StringToDouble(csvTimesMax, "maximum time field", time_values.Last(), "Using absolute maximum time instead", out tmax);
                if (BCheckBox)
                {
                    if (StringToDouble(csvTimesInc, "time resampling (by) field", -1.0, "Ignoring time resampling", out tinc))
                    {
                        UploadCSVTextBox += $"Extracting data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax} by step of {CSVTimesInc} \n";
                        NLog.LogManager.GetCurrentClassLogger().Info($"  Extracting data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax} by step of {CSVTimesInc}");
                    }
                    else
                    {
                        CSVTimesInc = "-";
                        BCheckBox = false;
                        UploadCSVTextBox += $"Extracting data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax}\n";
                        NLog.LogManager.GetCurrentClassLogger().Info($"  Extracting data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax}");
                    }
                }
                else
                {
                    UploadCSVTextBox += $"Extracting data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax}\n";
                    NLog.LogManager.GetCurrentClassLogger().Info($"  Extracting data for {TimesSelectionType} from {CSVTimesMin} to {CSVTimesMax}");
                }
                if (tmin > tmax)
                {
                    UploadCSVTextBox += $"Warning: tmin={tmin} > tmax={tmax}.\n";
                    double ttemp = tmin;
                    tmin = tmax;
                    tmax = ttemp;
                    UploadCSVTextBox += $"Swapping values.\n";
                }
                if (tmin < time_values[0])
                {
                    UploadCSVTextBox += $"Error: out-of-bounds minimum time: tmin={tmin}; rangeMin={time_values[0]} rangeMax={time_values.Last()}.\n";
                    UploadCSVTextBox += $"Updating tmin to minimum range: {tmin}==>{time_values[0]}.\n";
                    tmin = time_values[0];
                    CSVTimesMin = tmin.ToString();
                }
                else if (tmin > time_values.Last())
                {
                    UploadCSVTextBox += $"Error: out-of-bounds minimum time: tmin={tmin}; rangeMin={time_values[0]} rangeMax={time_values.Last()}.\n";
                    UploadCSVTextBox += $"Updating tmin to maximum range: {tmin}==>{time_values.Last()}.\n";
                    tmin = time_values.Last();
                    CSVTimesMin = tmin.ToString();
                }
                if (tmax < time_values[0])
                {
                    UploadCSVTextBox += $"Error: out-of-bounds maximum time: tmax={tmax}; rangeMin={time_values[0]} rangeMax={time_values.Last()}.\n";
                    UploadCSVTextBox += $"Updating tmax to minimum range: {tmax}==>{time_values[0]}.\n";
                    tmax = time_values[0];
                    CSVTimesMax = tmax.ToString();
                }
                else if (tmax > time_values.Last())
                {
                    UploadCSVTextBox += $"Error: out-of-bounds minimum time: tmax={tmax}; rangeMin={time_values[0]} rangeMax={time_values.Last()}.\n";
                    UploadCSVTextBox += $"Updating tmax to maximum range: {tmax}==>{time_values.Last()}.\n";
                    tmax = time_values.Last();
                    CSVTimesMax = tmax.ToString();
                }
                if (BCheckBox)
                {
                    bool isErrorTINC = false;
                    if (tinc > (tmax - tmin))
                    {
                        UploadCSVTextBox += $"Error: time resampling (by) field, {tinc}, should not be larger than the full range '{tmax - tmin}'.\n";
                        isErrorTINC = true;
                    }
                    else if (tinc <= 0.0)
                    {
                        UploadCSVTextBox += $"Error: time resampling (by) field, {tinc}, should be greater or equal to zero.\n";
                        isErrorTINC = true;
                    }
                    if (isErrorTINC)
                    {
                        UploadCSVTextBox += "Ignoring time resampling\n";
                        tinc = -1.0;
                        CSVTimesInc = "-";
                        BCheckBox = false;
                    }
                }
            }
            else
            {
                StringToDouble(csvTimesMin, "minimum time field", time_values[0], "Using absolute minimum time instead", out tmin);
                if (tmin < time_values[0])
                {
                    UploadCSVTextBox += $"Error: out-of-bounds single time: tmin={tmin}; rangeMin={time_values[0]} rangeMax={time_values.Last()}.\n";
                    UploadCSVTextBox += $"Updating time to minimum range: {tmin}==>{time_values[0]}.\n";
                    tmin = time_values[0];
                    CSVTimesMin = tmin.ToString();
                }
                else if (tmin > time_values.Last())
                {
                    UploadCSVTextBox += $"Error: out-of-bounds single time: tmin={tmin}; rangeMin={time_values[0]} rangeMax={time_values.Last()}.\n";
                    UploadCSVTextBox += $"Updating time to maximum range: {tmin}==>{time_values.Last()}.\n";
                    tmin = time_values.Last();
                    CSVTimesMin = tmin.ToString();
                }
                tmax = tmin;
                tinc = -1.0;
            }
            //Start extracting data
            //=====================
            int itmin, itmax;
            if (TimesSelectionType.Contains("All"))
            {
                nb_resampled_times = nbTimes;
                csv_resampled_content = csv_content;
                resampled_time_values = time_values;
                return;
            }
            else // Single or range, with or without resampling --> update times and values
            {
                //Generate new list of times
                //--------------------------
                if (TimesSelectionType.Contains("Single"))
                {
                    nb_resampled_times = 1;
                    resampled_time_values = new double[nb_resampled_times];
                    resampled_time_values[0] = tmin;
                }
                else if (tinc > 0)
                { //Range with resampling
                    nb_resampled_times = Convert.ToInt32(Math.Ceiling((tmax - tmin) / tinc));
                    nb_resampled_times++;
                    resampled_time_values = new double[nb_resampled_times];
                    int icount = 0;
                    for (double dtime = tmin; dtime < tmax; dtime += tinc)
                    {
                        resampled_time_values[icount] = dtime;
                        icount++;
                    }
                    resampled_time_values[icount] = tmax;
                }
                else
                { //Range without resampling
                    //Search for the bounds in the initial arrays to know how many points will be needed
                    itmin = 0;
                    for (int itime = 0; itime < nbTimes; itime++)
                    {
                        if (time_values[itime] <= tmin)
                        {
                            itmin = itime;
                        }
                        else
                        {
                            break;
                        }
                    }
                    itmax = nbTimes - 1;
                    for (int itime = nbTimes - 1; itime > itmin; itime--)
                    {
                        if (time_values[itime] >= tmax)
                        {
                            itmax = itime;
                        }
                        else
                        {
                            break;
                        }
                    }
                    nb_resampled_times = itmax - itmin + 1;
                    //if (tmin > (time_values[itmin] + 1.0e-12)) nb_resampled_times -= 1;
                    //if (tmax < (time_values[itmax - 1] - 1.0e-12)) nb_resampled_times -= 1;
                    resampled_time_values = new double[nb_resampled_times];
                    int icount = 0;
                    for (int itime = itmin; itime <= itmax; itime++)
                    {
                        //if (itime == itmin && tmin > time_values[itime])
                        if (itime == itmin)
                        {
                            resampled_time_values[icount] = tmin;
                        }
                        //else if (itime == itmax && tmax < time_values[itime])
                        else if (itime == itmax)
                        {
                            resampled_time_values[icount] = tmax;
                        }
                        else
                        {
                            resampled_time_values[icount] = time_values[itime];
                        }
                        icount++;
                    }
                }
                //Compute new data interpolated at the correct times
                //--------------------------------------------------
                csv_resampled_content = new double[nb_resampled_times, nbNodes];
                double[] data_temp;
                double tratio;
                itmin = 0;
                for (int irst = 0; irst < nb_resampled_times; irst++)
                {
                    //Search for the bounds of resampled_time_values in initial time_values
                    for (int itime = itmin; itime < nbTimes; itime++)
                    {
                        if (time_values[itime] <= resampled_time_values[irst])
                        {
                            itmin = itime;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (itmin < nbTimes - 1)
                    {
                        itmax = itmin + 1;
                    }
                    else
                    {
                        itmax = nbTimes - 1;
                        itmin = itmax - 1;
                    }
                    //Compute interpolation ratio
                    if (Math.Abs(time_values[itmax] - time_values[itmin]) > 1.0e-20)
                    {
                        tratio = (resampled_time_values[irst] - time_values[itmin]) / (time_values[itmax] - time_values[itmin]);
                    }
                    else
                    {
                        tratio = 0.0;
                    }
                    data_temp = InterpolateTime(itmin, itmax, tratio, csv_content);
                    CopyArray1D2D(data_temp, irst, nbNodes, csv_resampled_content);
                }
                return;
            }
            /*
            //Debug print of the resampled data
            UploadCSVTextBox += $"------TIME";
            for (nodIdx = 0; nodIdx < nbNodIdx; nodIdx++)
            {
                UploadCSVTextBox += $"{String.Format("{0,10:d}", node_IDs[node_indices[nodIdx]])}";
            }
            UploadCSVTextBox += "\n";
            for (int i=0; i< nb_resampled_times; i++)
            {
                UploadCSVTextBox += $"{String.Format("{0,10:G}", resampled_time_values[i])}";
                for (nodIdx = 0; nodIdx < nbNodIdx; nodIdx++)
                {
                    UploadCSVTextBox += $"{String.Format("{0,10:G}", csv_resampled_content[i,node_indices[nodIdx]])}";
                }
                UploadCSVTextBox += "\n";
            }
            */
        }
        private void ComputeAverage(in int nbTimes,
                            in int nbNodes,
                            in int nbNodIdx,
                            in double[] time_values,
                            in int[] node_IDs,
                            in int[] node_indices,
                            in double[,] csv_content,
                            out double dmin,
                            out double dmax,
                            out double[] davg_t)
        {
            dmin = Double.MaxValue; //1.7976931348623157E+308
            dmax = Double.MinValue; //-1.7976931348623157E+308
            davg_t = new double[nbTimes];
            double davg = 0.0; //global average (for all nodes and all times); useless for now
            for (int itime = 0; itime < nbTimes; itime++)
            {
                davg_t[itime] = 0.0;
                for (int inode = 0; inode < nbNodIdx; inode++)
                {
                    //UploadCSVTextBox += $"Time {time_values[itime]}; Node {node_IDs[node_indices[inode]]}; value {csv_content[itime, node_indices[inode]]}";
                    if (csv_content[itime, node_indices[inode]] > dmax)
                    {
                        dmax = csv_content[itime, node_indices[inode]];
                    }
                    if (csv_content[itime, node_indices[inode]] < dmin)
                    {
                        dmin = csv_content[itime, node_indices[inode]];
                    }
                    davg_t[itime] += csv_content[itime, node_indices[inode]];
                }
                davg_t[itime] = davg_t[itime] / nbNodIdx;
                //Debug:
                //UploadCSVTextBox += $" time {time_values[itime]} ==> avg {davg_t[itime]}; min {dmin}; max {dmax}\n";
                davg += davg_t[itime];
            }
            davg = davg / nbTimes;
            UploadCSVTextBox += "   Temperature statistics\n";
            UploadCSVTextBox += $"      min {dmin}; avg {davg}; max {dmax}\n";
            NLog.LogManager.GetCurrentClassLogger().Info($" ==> min {dmin}; avg {davg}; max {dmax}");
            return;
        }

        private bool StringToDouble(string mystring, string mylabel, double defval, string deflabel, out double value)
        {
            //UploadCSVTextBox += $"  Extracting data at {mystring}\n";
            //NLog.LogManager.GetCurrentClassLogger().Info($"  Extracting data at {mystring}");
            bool isConvOk = Double.TryParse(mystring,
                                    NumberStyles.Float,
                                    new CultureInfo("en-US"),
                                    out value);
            if (!isConvOk)
            {
                UploadCSVTextBox += $"Error: {mylabel} contains characters that could not be converted to a valid number: '{mystring}'.\n";
                value = defval;
                UploadCSVTextBox += $"{deflabel}: {value}\n";
            }
            return isConvOk;
        }

        private double[] InterpolateTime(int it0, int it1, double tratio, double[,] data)
        {
            //UploadCSVTextBox += $"InterpolateTime it0={it0}; it1={it1}; tratio={tratio}\n";
            int nelem = data.GetLength(1);
            double[] outData = new double[nelem];
            for (int i = 0; i < nelem; i++)
            {
                outData[i] = data[it0, i] + (data[it1, i] - data[it0, i]) * tratio;
            }
            return outData;
        }

        private void CopyArray1D2D(double[] inData, int iline, int nbcol, double[,] outData)
        {
            for (int i = 0; i < nbcol; i++)
            {
                outData[iline, i] = inData[i];
            }
            return;
        }
    }
}