using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;

namespace Editor
{
    public partial class MainWindow : Window
    {
        private static string GeneratorExecutableName = "GeradorV2.exe";
        private static string ViewerExecutableName = "Visualizador.exe";
        private static string BaseConstraintFileContents = "<?xml version='1.0'?>\n<constraints xmlns=\"ConstraintSchema\">\n<!-- Add your constraints sets here -->\n</constraints>";

        private static int MaximumPreviewFloorCount = 10;

        private struct TemporaryFileId
        {
            public bool IsDefinitionFile;
            public string Filename;
        }

        private Dictionary<string, TemporaryFileId> TemporaryFiles;
        public static string WorkingDirectory = "";
        public DataRegistry Registry;

        public MainWindow()
        {
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Registry = new DataRegistry();
            TemporaryFiles = new Dictionary<string, TemporaryFileId>();

            InitializeComponent();
            documentTabControl.AddHandler(DocumentTabItem.CloseTabEvent, new RoutedEventHandler(documentTabControl_CloseTab));

            this.Loaded += delegate
                {
                    if (Registry.LoadData(WorkingDirectory))
                    {
                        UpdateTreeViews();
                    }
                };
        }

        private void CloseTab(string tabTitle)
        {
            object tabItem = null;

            foreach(var tab in documentTabControl.Items)
            {
                if((((TabItem)tab).Header as string) == tabTitle)
                {
                    tabItem = tab;
                    break;
                }
            }

            if (tabItem != null)
                documentTabControl.Items.Remove(tabItem);
        }

        private void SaveActiveFile(string targetFilename = null)
        {
            if ((documentTabControl.Items.Count > 0) && (documentTabControl.SelectedItem != null))
            {
                var selectedItem = (TabItem)documentTabControl.SelectedItem;
                var selectedEntryText = System.IO.Path.GetFileNameWithoutExtension((string)selectedItem.Header);

                // Find out if the root parent is a constraint set or a building descriptor, otherwise do nothing
                if(selectedItem is DocumentTabItem)
                {
                    string originalFilename;

                    if (Registry.DefinitionFiles.TryGetValue(selectedEntryText, out originalFilename))
                    {
                        SaveFile((DocumentTabItem)selectedItem, targetFilename != null ? targetFilename : originalFilename, true);
                    }
                    else if (Registry.ConstraintFiles.TryGetValue(selectedEntryText, out originalFilename))
                    {
                        SaveFile((DocumentTabItem)selectedItem, targetFilename != null ? targetFilename : originalFilename, false);
                    }

                    // If we haven't found the file, check the temporary files
                    TemporaryFileId fileId;
                    if (TemporaryFiles.TryGetValue(selectedEntryText, out fileId))
                    {
                        SaveFile((DocumentTabItem)selectedItem, targetFilename != null ? targetFilename : fileId.Filename, fileId.IsDefinitionFile);

                        CloseTab(System.IO.Path.GetFileName(fileId.Filename));

                        if (targetFilename == null)
                        {
                            if (fileId.IsDefinitionFile)
                                EditDefinitionFile(fileId.Filename);
                            else
                                EditConstraintFile(fileId.Filename);
                        }

                        TemporaryFiles.Remove(selectedEntryText);
                    }
                }

                Registry.LoadData(WorkingDirectory);
                UpdateTreeViews();
            }
        }

        private void SaveFile(DocumentTabItem containingTab, string filename, bool isDefinitionFile)
        {
            try
            {
                if (isDefinitionFile)
                {
                    var editor = (BuildingDescriptorEditor)containingTab.Content;
                    System.IO.File.WriteAllText(filename, editor.Text);
                }
                else
                {
                    var editor = (ConstraintFileEditor)containingTab.Content;
                    System.IO.File.WriteAllText(filename, editor.Text);
                }

                containingTab.DocumentModified = false;
            }
            catch(Exception exc)
            {
                MessageBox.Show("An exception occurred while saving data to the definition file located at " + filename + ":\n" + exc.Message);
            }
        }

        private void EditDefinitionFile(string filename, BuildGen.Data.Building bld = null)
        {
            try
            {
                string tabHeader = System.IO.Path.GetFileName(filename);

                // Check if a tab exists for this document already
                int tabIndex = -1;
                for (int n = 0; n < documentTabControl.Items.Count; n++)
                {
                    if ((((TabItem)documentTabControl.Items[n]).Header as string) == tabHeader)
                    {
                        tabIndex = n;
                        break;
                    }
                }

                // Create a new tab if it does not exist
                if (tabIndex == -1)
                {
                    string fileContents = ((bld != null) ? "" : System.IO.File.ReadAllText(filename));

                    BuildingDescriptorEditor nEditor = new BuildingDescriptorEditor();
                    TabItem docTab = new DocumentTabItem() { Header = tabHeader };
                    docTab.Content = nEditor;

                    nEditor.ContentsModified += new BuildingDescriptorEditor.ContentsModifiedHandler(this.editor_ContentsModified);
                    documentTabControl.Items.Add(docTab);

                    UpdateEnabledDocumentControls();

                    // Set the last opened document as the active tab, otherwise a blank tab control could be shown to the user.
                    documentTabControl.SelectedIndex = documentTabControl.Items.Count - 1;

                    nEditor.Loaded += delegate
                    {
                        if (bld == null)
                            nEditor.Edit(fileContents);
                        else
                            nEditor.Edit(bld);
                    };
                }
                else
                {
                    documentTabControl.SelectedIndex = tabIndex;
                }
            }
            catch(Exception exc)
            {
                MessageBox.Show("An exception occurred while preparing to edit the definition file " + filename + ":\n" + exc.Message);
            }
        }

        private void EditConstraintFile(string filename, bool temporary = false)
        {
            try
            {
                string tabHeader = System.IO.Path.GetFileName(filename);

                // Check if a tab exists for this document already
                int tabIndex = -1;
                for (int n = 0; n < documentTabControl.Items.Count; n++)
                {
                    if ((((TabItem)documentTabControl.Items[n]).Header as string) == tabHeader)
                    {
                        tabIndex = n;
                        break;
                    }
                }

                // Create a new tab if it does not exist
                if (tabIndex == -1)
                {
                    string fileContents = (temporary ? BaseConstraintFileContents : System.IO.File.ReadAllText(filename));

                    ConstraintFileEditor nEditor = new ConstraintFileEditor();
                    nEditor.Text = fileContents;

                    TabItem docTab = new DocumentTabItem() { Header = tabHeader };
                    docTab.Content = nEditor;

                    nEditor.ContentsModified += new ConstraintFileEditor.ContentsModifiedHandler(this.editor_ContentsModified);
                    documentTabControl.Items.Add(docTab);

                    UpdateEnabledDocumentControls();

                    // Set the last opened document as the active tab, otherwise a blank tab control could be shown to the user.
                    documentTabControl.SelectedIndex = documentTabControl.Items.Count - 1;
                }
                else
                {
                    documentTabControl.SelectedIndex = tabIndex;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("An exception occurred while preparing to edit the constraint sets file " + filename + ":\n" + exc.Message);
            }
        }

        private void UpdateTreeViews()
        {
            // Update the listing for the constraint set tree view
            documentTreeView.Items.Clear();

            // List the constraint set files
            TreeViewItem constraintSetsParentItem = new TreeViewItem() { Header = "Constraint Sets" };

            foreach (var item in Registry.ConstraintFiles)
            {
                TreeViewItem constraintSetItem = new TreeViewItem() { Header = item.Key };
                constraintSetsParentItem.Items.Add(constraintSetItem);
            }

            constraintSetsParentItem.IsExpanded = true;
            documentTreeView.Items.Add(constraintSetsParentItem);

            // List the building description files
            TreeViewItem buildingDefinitionParentItem = new TreeViewItem() { Header = "Building Definitions" };

            foreach (var item in Registry.DefinitionFiles)
            {
                TreeViewItem buildingDefinitionItem = new TreeViewItem() { Header = item.Key };
                buildingDefinitionParentItem.Items.Add(buildingDefinitionItem);
            }

            buildingDefinitionParentItem.IsExpanded = true;
            documentTreeView.Items.Add(buildingDefinitionParentItem);
        }

        /// <summary>
        /// Toggles the enabled status for several controls, including menu options, 
        /// based on the amount of active documents.
        /// </summary>
        private void UpdateEnabledDocumentControls()
        {
            if (documentTabControl.Items.Count == 0)
            {
                documentTabControl.Visibility = Visibility.Hidden;
                fileSaveItem.IsEnabled = false;
                fileSaveAsItem.IsEnabled = false;
                fileCloseItem.IsEnabled = false;
                toolsGenerateItem.IsEnabled = false;
                toolsGenerateWithParamsItem.IsEnabled = false;
            }
            else
            {
                documentTabControl.Visibility = Visibility.Visible;
            }
        }

        private void ShowOutput(string filepath, string constraintSet, int? seed)
        {
            int numFloorsGenerated = 0;

            for (int n = 0; n < MaximumPreviewFloorCount; n++)
            {
                if (System.IO.File.Exists(WorkingDirectory + "\\" + filepath + "_final_" + n + ".bmp"))
                    numFloorsGenerated++;
                else
                    break;
            }

            if (numFloorsGenerated != 0)
            {
                PreviewWindow previewWnd = new PreviewWindow();
                previewWnd.Initialize(WorkingDirectory + "\\" + filepath + "_final_", numFloorsGenerated);

                bool? ret = previewWnd.ShowDialog();
                if (ret.HasValue && ret.Value)
                {
                    previewWnd.Dispose();
                    RunBuildingGenerator(constraintSet, seed);
                }
            }
            else
            {
                MessageBox.Show("The building generation process failed. Please confirm that the specified files exist and that the user has write privileges.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RunBuildingGenerator(string constraintSet, int? seed)
        {
            SaveActiveFile();

            string selectedEntryText = System.IO.Path.GetFileNameWithoutExtension((string)((TabItem)documentTabControl.SelectedItem).Header);
            string originalFilename;

            if (Registry.DefinitionFiles.TryGetValue(selectedEntryText, out originalFilename))
            {
                string argString = "-in=\"" + originalFilename + "\" -out=\"Output\\" + selectedEntryText + "\"";
                if(constraintSet != null)
                    argString += " -constraints=\"" + constraintSet + "\"";
                if(seed.HasValue)
                    argString += " -seed=" + seed.Value;

                ProcessStartInfo geradorStartInfo = new ProcessStartInfo();
                geradorStartInfo.WorkingDirectory = WorkingDirectory;
                geradorStartInfo.FileName = WorkingDirectory + "\\" + GeneratorExecutableName;
                geradorStartInfo.Arguments = argString;

                try
                {
                    using (Process proc = Process.Start(geradorStartInfo))
                    {
                        proc.WaitForExit();

                        // If the user wishes to rerun the generator, it'll force the usage of a new seed so it gets passed as null
                        ShowOutput("Output\\" + selectedEntryText, constraintSet, null);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #region Keyboard Shortcuts
        private void SaveShortcut_CommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            // Calls a method to close the file and release resources.
            SaveActiveFile();
        }
        #endregion
        
        #region Menu Events
        private void fileNewBuildingDefinitionItem_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog())
            {
                fileDialog.Filter = @"XML File|*.xml";
                fileDialog.AddExtension = true;
                fileDialog.DefaultExt = @".xml";
                fileDialog.InitialDirectory = Path.GetFullPath(WorkingDirectory + @"Input");
                fileDialog.RestoreDirectory = true;

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TemporaryFiles.Add(Path.GetFileNameWithoutExtension(fileDialog.FileName), new TemporaryFileId() { IsDefinitionFile = true, Filename = fileDialog.FileName });

                    DefinitionFileSettings settingsDialog = new DefinitionFileSettings();

                    bool? ret = settingsDialog.ShowDialog();
                    if (ret.HasValue && ret.Value)
                    {
                        BuildGen.Data.Building nbuilding = new BuildGen.Data.Building(settingsDialog.BuildingWidth, settingsDialog.BuildingHeight, settingsDialog.BuildingResolution);
                        nbuilding.ConstraintSet = settingsDialog.ConstraintSet;
                        nbuilding.Seed = settingsDialog.Seed;
                        nbuilding.AddFloor();

                        EditDefinitionFile(Path.GetFileName(fileDialog.FileName), nbuilding);
                    }
                }
            }
        }

        private void fileNewConstraintSetItem_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog())
            {
                fileDialog.Filter = @"XML File|*.xml";
                fileDialog.AddExtension = true;
                fileDialog.DefaultExt = @".xml";
                fileDialog.InitialDirectory = Path.GetFullPath(WorkingDirectory + @"Constraints");
                fileDialog.RestoreDirectory = true;

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TemporaryFiles.Add(Path.GetFileNameWithoutExtension(fileDialog.FileName), new TemporaryFileId() { IsDefinitionFile = false, Filename = fileDialog.FileName });
                    EditConstraintFile(Path.GetFileName(fileDialog.FileName), true);
                }
            }
        }

        private void fileSaveItem_Click(object sender, RoutedEventArgs e)
        {
            SaveActiveFile();
        }

        private void fileSaveAsItem_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.SaveFileDialog fileDialog = new System.Windows.Forms.SaveFileDialog())
            {
                bool selectionIsConstraintSet = ((TabItem)documentTabControl.SelectedItem).Content is ConstraintFileEditor;

                fileDialog.Filter = @"XML File|*.xml";
                fileDialog.AddExtension = true;
                fileDialog.DefaultExt = @".xml";
                fileDialog.RestoreDirectory = true;

                if(!selectionIsConstraintSet)
                    fileDialog.InitialDirectory = WorkingDirectory + @"Input";
                else
                    fileDialog.InitialDirectory = WorkingDirectory + @"Constraints";

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SaveActiveFile(fileDialog.FileName);
                    Registry.LoadData(WorkingDirectory);
                    UpdateTreeViews();

                    // Edit the saved file and set it as the active tab
                    if(!selectionIsConstraintSet)
                        EditDefinitionFile(fileDialog.FileName);
                    else
                        EditConstraintFile(fileDialog.FileName);

                    documentTabControl.SelectedIndex = documentTabControl.Items.Count - 1;
                }
            }
        }

        private void fileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void fileCloseItem_Click(object sender, RoutedEventArgs e)
        {
            if(documentTabControl.SelectedItem != null)
                documentTabControl.Items.Remove((DocumentTabItem)documentTabControl.SelectedItem);
        }

        private void toolsGenerateItem_Click(object sender, RoutedEventArgs e)
        {
            if ((documentTabControl.SelectedItem != null) && (documentTabControl.SelectedItem is DocumentTabItem))
            {
                BuildingDescriptorEditor editor = (BuildingDescriptorEditor)((DocumentTabItem)documentTabControl.SelectedItem).Content;
                BuildGen.Data.Building bld = editor.ActiveBuilding;

                string constraintSet = bld.ConstraintSet;
                int seed = bld.Seed;

                // If we don't have a constraint set present the user with a warning message and show the generator parameters window if he wishes.
                if (string.IsNullOrEmpty(constraintSet))
                {
                    MessageBoxResult mboxResult = MessageBox.Show("No constraint set specified in the description file. Do you wish to launch the generator parameters dialog?", 
                        "Missing Data", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                    if (mboxResult == MessageBoxResult.Yes)
                    {
                        GeneratorParametersWindow dialog = new GeneratorParametersWindow(constraintSet, seed);
                        bool? res = dialog.ShowDialog();

                        if (res.HasValue && res.Value)
                        {
                            constraintSet = dialog.ConstraintSet;
                            seed = dialog.Seed;
                        }
                    }
                }

                if (constraintSet != null)
                {
                    if (seed != 0)
                        RunBuildingGenerator(constraintSet, seed);
                    else
                        RunBuildingGenerator(constraintSet, null);
                }
            }
        }

        private void toolsGenerateWithParamsItem_Click(object sender, RoutedEventArgs e)
        {
            if ((documentTabControl.SelectedItem != null) && (documentTabControl.SelectedItem is DocumentTabItem) &&
                (((DocumentTabItem)documentTabControl.SelectedItem).Content is BuildingDescriptorEditor))
            {
                BuildingDescriptorEditor editor = (BuildingDescriptorEditor)((DocumentTabItem)documentTabControl.SelectedItem).Content;
                BuildGen.Data.Building bld = editor.ActiveBuilding;

                GeneratorParametersWindow dialog = new GeneratorParametersWindow(bld.ConstraintSet, bld.Seed);
                bool? res = dialog.ShowDialog();

                if (res.HasValue && res.Value)
                    RunBuildingGenerator(dialog.ConstraintSet, dialog.Seed);
            }
        }

        private void toolsLaunchViewerItem_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo viewerStartInfo = new ProcessStartInfo();
            viewerStartInfo.WorkingDirectory = WorkingDirectory;
            viewerStartInfo.FileName = WorkingDirectory + "\\" + ViewerExecutableName;

            using (Process proc = Process.Start(viewerStartInfo))
            {
                proc.WaitForExit();
            }
        }

        private void settingsSetWorkDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.SelectedPath = WorkingDirectory;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (Registry.LoadData(folderDialog.SelectedPath))
                    {
                        WorkingDirectory = folderDialog.SelectedPath;
                        UpdateTreeViews();
                    }
                    else
                    {
                        // TODO: add a yes/no check if the user wants to create a work directory at the location
                        MessageBox.Show("The specified location is not a valid working directory. Please confirm that there exist Input, Output and Constraint folders at the specified location");
                    }
                }
            }
        }
        #endregion

        #region Main widget events
        private void documentTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(documentTreeView.SelectedItem != null)
            {
                var selectedItem = (TreeViewItem)documentTreeView.SelectedItem;
                var selectedEntryText = (string)selectedItem.Header;

                // Find out if the root parent is a constraint set or a building descriptor, otherwise do nothing
                if(selectedItem.Parent == documentTreeView.Items[0])
                {
                    string targetFilename;
                    
                    if(Registry.ConstraintFiles.TryGetValue(selectedEntryText, out targetFilename))
                    {
                        EditConstraintFile(targetFilename);
                    }
                }
                else if(selectedItem.Parent == documentTreeView.Items[1])
                {
                    string targetFilename;
                    
                    if(Registry.DefinitionFiles.TryGetValue(selectedEntryText, out targetFilename))
                    {
                        EditDefinitionFile(targetFilename);
                    }
                }
            }
        }

        private void documentTabControl_CloseTab(object sender, RoutedEventArgs e)
        {
            var item = e.Source as DocumentTabItem;

            if(item.DocumentModified)
            {
                MessageBoxResult res = MessageBox.Show(this, "Save changes made to " + (string)item.Header + "?", "Editor", MessageBoxButton.YesNoCancel);

                if (res == MessageBoxResult.Yes)
                {
                    // Save the file first
                    string headerFilename = System.IO.Path.GetFileNameWithoutExtension((string)item.Header);
                    string targetFilename;

                    if (Registry.ConstraintFiles.TryGetValue(headerFilename, out targetFilename))
                    {
                        SaveFile(item, targetFilename, false);
                    }
                    else if (Registry.DefinitionFiles.TryGetValue(headerFilename, out targetFilename))
                    {
                        SaveFile(item, targetFilename, true);
                    }
                }
                else if(res == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // Close the current tab
            documentTabControl.Items.Remove(item);
            UpdateEnabledDocumentControls();
        }

        private void documentTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((documentTabControl.SelectedItem != null) && (documentTabControl.SelectedItem is DocumentTabItem))
            {
                fileSaveItem.IsEnabled = true;
                fileSaveAsItem.IsEnabled = true;
                fileCloseItem.IsEnabled = true;

                if (((DocumentTabItem)documentTabControl.SelectedItem).Content is BuildingDescriptorEditor)
                {
                    toolsGenerateItem.IsEnabled = true;
                    toolsGenerateWithParamsItem.IsEnabled = true;
                }
            }
        }

        private void editor_ContentsModified(object sender)
        {
            DocumentTabItem parentTab = (DocumentTabItem)(sender as UserControl).Parent;
            parentTab.DocumentModified = true;
        }
        #endregion

    }
}
