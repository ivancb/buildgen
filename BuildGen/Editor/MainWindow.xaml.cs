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
            public string Filename;
            public bool IsDefinitionFile;
        }

        private List<TemporaryFileId> TemporaryFiles;
        public static string WorkingDirectory = "";
        public DataRegistry Registry;

        public MainWindow()
        {
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Registry = new DataRegistry();
            TemporaryFiles = new List<TemporaryFileId>();

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
                var selectedItem = (DocumentTabItem)documentTabControl.SelectedItem;
                var pathActiveFile = (string)selectedItem.Filename;

                // Find out if the root parent is a constraint set or a building descriptor, otherwise do nothing
                if(selectedItem is DocumentTabItem)
                {
                    if (Registry.DefinitionFiles.Contains(pathActiveFile))
                        SaveFile((DocumentTabItem)selectedItem, targetFilename != null ? targetFilename : pathActiveFile, true);
                    else if (Registry.ConstraintFiles.Contains(pathActiveFile))
                        SaveFile((DocumentTabItem)selectedItem, targetFilename != null ? targetFilename : pathActiveFile, false);
                    else
                    {
                        // If we haven't found the file, check the temporary files
                        foreach (var tempfile in TemporaryFiles)
                        {
                            if (tempfile.Filename == pathActiveFile)
                            {
                                SaveFile((DocumentTabItem)selectedItem, targetFilename != null ? targetFilename : tempfile.Filename, tempfile.IsDefinitionFile);

                                CloseTab(System.IO.Path.GetFileName(tempfile.Filename) + (tempfile.IsDefinitionFile ? " (DefFile)" : " (CSet)"));

                                if (targetFilename == null)
                                {
                                    if (tempfile.IsDefinitionFile)
                                        EditDefinitionFile(tempfile.Filename);
                                    else
                                        EditConstraintFile(tempfile.Filename);
                                }

                                TemporaryFiles.Remove(tempfile);
                                break;
                            }
                        }
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
                    if ((string)((DocumentTabItem)documentTabControl.Items[n]).Filename == filename)
                    {
                        tabIndex = n;
                        break;
                    }
                }

                // Create a new tab if it does not exist
                if (tabIndex == -1)
                {
                    string fileContents = ((bld != null) ? "" : System.IO.File.ReadAllText(filename));

                    BuildingDescriptorEditor nEditor = new BuildingDescriptorEditor(Registry);
                    TabItem docTab = new DocumentTabItem() { Header = tabHeader + " (DefFile)", Filename = filename };
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
                    if ((string)((DocumentTabItem)documentTabControl.Items[n]).Filename == filename)
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

                    TabItem docTab = new DocumentTabItem() { Header = tabHeader + " (CSet)", Filename = filename };
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

            foreach (var filepath in Registry.ConstraintFiles)
            {
                TreeViewItem constraintSetItem = new TreeViewItem() { Header = Path.GetFileNameWithoutExtension(filepath) };
                constraintSetsParentItem.Items.Add(constraintSetItem);
            }

            constraintSetsParentItem.IsExpanded = true;
            documentTreeView.Items.Add(constraintSetsParentItem);

            // List the building description files
            TreeViewItem buildingDefinitionParentItem = new TreeViewItem() { Header = "Building Definitions" };

            foreach (var filepath in Registry.DefinitionFiles)
            {
                TreeViewItem buildingDefinitionItem = new TreeViewItem() { Header = Path.GetFileNameWithoutExtension(filepath) };
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

            string pathActiveFile = ((DocumentTabItem)documentTabControl.SelectedItem).Filename;

            if (Registry.DefinitionFiles.Contains(pathActiveFile))
            {
                string noExtPath = Path.GetFileNameWithoutExtension(pathActiveFile);

                string argString = "-in=\"" + pathActiveFile + "\" -out=\"Output\\" + noExtPath + "\"";
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

                        if (proc.ExitCode == 0)
                        {
                            // If the user wishes to rerun the generator, it'll force the usage of a new seed so it gets passed as null
                            ShowOutput("Output\\" + noExtPath, constraintSet, null);
                        }
                        else
                        {
                            MessageBox.Show("An unknown error occurred while executing the generator.");
                        }
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
                    TemporaryFiles.Add(new TemporaryFileId() { IsDefinitionFile = true, Filename = fileDialog.FileName });

                    DefinitionFileSettings settingsDialog = new DefinitionFileSettings(Registry);

                    bool? ret = settingsDialog.ShowDialog();
                    if (ret.HasValue && ret.Value)
                    {
                        TemporaryFileId tempFile = new TemporaryFileId() { Filename = fileDialog.FileName, IsDefinitionFile = true };

                        if (!TemporaryFiles.Contains(tempFile))
                            TemporaryFiles.Add(tempFile);

                        BuildGen.Data.Building nbuilding = new BuildGen.Data.Building(settingsDialog.BuildingWidth, settingsDialog.BuildingHeight, settingsDialog.BuildingResolution);
                        nbuilding.ConstraintSet = settingsDialog.ConstraintSet;
                        nbuilding.Seed = settingsDialog.Seed;
                        nbuilding.AddFloor();

                        EditDefinitionFile(fileDialog.FileName, nbuilding);
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
                    TemporaryFileId tempFile = new TemporaryFileId() { Filename = fileDialog.FileName, IsDefinitionFile = true };

                    if(!TemporaryFiles.Contains(tempFile))
                        TemporaryFiles.Add(tempFile);

                    EditConstraintFile(fileDialog.FileName, true);
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
            if (documentTabControl.SelectedItem != null)
            {
                DocumentTabItem selectedTab = (DocumentTabItem)documentTabControl.SelectedItem;
                string filename = (string)selectedTab.Filename;

                TemporaryFiles.Remove(new TemporaryFileId { Filename = filename, IsDefinitionFile = selectedTab.Content is BuildingDescriptorEditor });
                documentTabControl.Items.Remove(selectedTab);
            }
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
                        GeneratorParametersWindow dialog = new GeneratorParametersWindow(constraintSet, seed, Registry);
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

                GeneratorParametersWindow dialog = new GeneratorParametersWindow(bld.ConstraintSet, bld.Seed, Registry);
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
                var itemHeader = (string)selectedItem.Header;

                // Find out if the root parent is a constraint set or a building descriptor, otherwise do nothing
                var constraintSetItemsParent = documentTreeView.Items[0];
                var definitionSetItemsParent = documentTreeView.Items[1];

                if (selectedItem.Parent == constraintSetItemsParent)
                {
                    foreach(var filepath in Registry.ConstraintFiles)
                    {
                        if (filepath.Contains(itemHeader) && (Path.GetFileNameWithoutExtension(filepath) == itemHeader))
                        {
                            EditConstraintFile(filepath);
                            return;
                        }
                    }

                    MessageBox.Show("Could not edit the specified file");
                }
                else if (selectedItem.Parent == definitionSetItemsParent)
                {
                    foreach (var filepath in Registry.DefinitionFiles)
                    {
                        if (filepath.Contains(itemHeader) && (Path.GetFileNameWithoutExtension(filepath) == itemHeader))
                        {
                            EditDefinitionFile(filepath);
                            return;
                        }
                    }

                    MessageBox.Show("Could not edit the specified file");
                }
            }
        }

        private void documentTabControl_CloseTab(object sender, RoutedEventArgs e)
        {
            var item = e.Source as DocumentTabItem;

            if(item.DocumentModified)
            {
                MessageBoxResult res = MessageBox.Show(this, "Save changes made to " + (string)item.Filename + "?", "Editor", MessageBoxButton.YesNoCancel);

                if (res == MessageBoxResult.Yes)
                {
                    // Save the file first
                    string filepath = (string)item.Filename;

                    if (Registry.ConstraintFiles.Contains(filepath))
                        SaveFile(item, filepath, false);
                    else if (Registry.DefinitionFiles.Contains(filepath))
                        SaveFile(item, filepath, true);
                }
                else if(res == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // Close the current tab and remove it from temporary files if it's there
            string filename = (string)item.Filename;
            TemporaryFiles.Remove(new TemporaryFileId { Filename = filename, IsDefinitionFile = item.Content is BuildingDescriptorEditor });
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
