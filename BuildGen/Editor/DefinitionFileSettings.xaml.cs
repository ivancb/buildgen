using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Editor
{
    public partial class DefinitionFileSettings : Window
    {
        public string ConstraintSet = "";
        public int Seed = 0;
        public float BuildingWidth = 10f;
        public float BuildingHeight = 10f;
        public float BuildingResolution = 0.2f;

        private DataRegistry Registry;

        public DefinitionFileSettings(DataRegistry dataSource)
        {
            Registry = dataSource;

            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConstraintSet = (string)ConstraintSetComboBox.SelectedValue;
                Seed = int.Parse(SeedTextBox.Text, CultureInfo.InvariantCulture);
                BuildingWidth = float.Parse(FloorWidthTextBox.Text, CultureInfo.InvariantCulture);
                BuildingHeight = float.Parse(FloorHeightTextBox.Text, CultureInfo.InvariantCulture);
                BuildingResolution = float.Parse(FloorResolutionTextBox.Text, CultureInfo.InvariantCulture);

                this.DialogResult = true;
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void DefinitionFileSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeConstraintSetsComboBox();

            if ((ConstraintSetComboBox.Items.Count > 0) && (ConstraintSetComboBox.SelectedIndex == -1))
                ConstraintSetComboBox.SelectedIndex = 0;

            SeedTextBox.Text = Seed.ToString(CultureInfo.InvariantCulture);
            FloorWidthTextBox.Text = BuildingWidth.ToString(CultureInfo.InvariantCulture);
            FloorHeightTextBox.Text = BuildingHeight.ToString(CultureInfo.InvariantCulture);
            FloorResolutionTextBox.Text = BuildingResolution.ToString(CultureInfo.InvariantCulture);
        }

        private void InitializeConstraintSetsComboBox()
        {
            ConstraintSetComboBox.DisplayMemberPath = "key";
            ConstraintSetComboBox.SelectedValuePath = "val";

            foreach (var filepath in Registry.ConstraintFiles)
            {
                ConstraintSetComboBox.Items.Add(new { key = Path.GetFileNameWithoutExtension(filepath), val = filepath });
            }

            // Has to be done after populating the combobox, otherwise it gets reset
            for (int n = 0; n < Registry.ConstraintFiles.Count; n++)
            {
                if (Registry.ConstraintFiles[n] == ConstraintSet)
                {
                    ConstraintSetComboBox.SelectedIndex = n;
                    break;
                }
            }
        }

        private void NumericInputTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (IsNumeric(e.Text))
                e.Handled = true;
        }

        private bool IsNumeric(string text)
        {
            var regex = new System.Text.RegularExpressions.Regex("[^0-9.-]+");

            return regex.IsMatch(text);
        }
    }
}
