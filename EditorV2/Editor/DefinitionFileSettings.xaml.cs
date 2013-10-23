using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Editor
{
    public partial class DefinitionFileSettings : Window
    {
        public string ConstraintSet = "";
        public int Seed = 0;
        public float BuildingWidth = 10f;
        public float BuildingHeight = 10f;
        public float BuildingResolution = 0.2f;

        public DefinitionFileSettings()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConstraintSet = ConstraintSetTextBox.Text;
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
            ConstraintSetTextBox.Text = ConstraintSet;
            SeedTextBox.Text = Seed.ToString(CultureInfo.InvariantCulture);
            FloorWidthTextBox.Text = BuildingWidth.ToString(CultureInfo.InvariantCulture);
            FloorHeightTextBox.Text = BuildingHeight.ToString(CultureInfo.InvariantCulture);
            FloorResolutionTextBox.Text = BuildingResolution.ToString(CultureInfo.InvariantCulture);
        }

        private void SeedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsNumeric(e.Text))
                e.Handled = true;
        }

        private bool IsNumeric(string text)
        {
            var regex = new System.Text.RegularExpressions.Regex("[^0-9.-]+");

            return regex.IsMatch(text);
        }
    }
}
