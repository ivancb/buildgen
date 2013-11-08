using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Editor
{
    public partial class GeneratorParametersWindow : Window
    {
        public string ConstraintSet;
        public int Seed;
        private DataRegistry Registry;

        public GeneratorParametersWindow(string initialConstraintSet, int initialSeed, DataRegistry registry)
        {
            InitializeComponent();

            ConstraintSet = initialConstraintSet;
            Seed = initialSeed;
            Registry = registry;
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            ConstraintSet = (string)ConstraintSetComboBox.SelectedValue;

            if (!string.IsNullOrEmpty(SeedTextBox.Text))
                Seed = int.Parse(SeedTextBox.Text);

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void GeneratorParametersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeConstraintSetsComboBox();

            if ((ConstraintSetComboBox.Items.Count > 0) && (ConstraintSetComboBox.SelectedIndex == -1))
                ConstraintSetComboBox.SelectedIndex = 0;

            SeedTextBox.Text = Seed.ToString();
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
