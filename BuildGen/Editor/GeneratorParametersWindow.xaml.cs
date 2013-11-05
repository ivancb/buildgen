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
using System.Windows.Shapes;

namespace Editor
{
    public partial class GeneratorParametersWindow : Window
    {
        public string ConstraintSet;
        public int Seed;

        public GeneratorParametersWindow(string initialConstraintSet, int initialSeed)
        {
            InitializeComponent();

            ConstraintSet = initialConstraintSet;
            Seed = initialSeed;

            if (ConstraintSet != null)
                ConstraintSetTextBox.Text = ConstraintSet;

            SeedTextBox.Text = Seed.ToString();
        }

        /// <summary>
        /// Handles text input in order to only allow numeric inputs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SeedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsNumeric(e.Text))
                e.Handled = true;
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            ConstraintSet = ConstraintSetTextBox.Text;

            if (!string.IsNullOrEmpty(SeedTextBox.Text))
                Seed = int.Parse(SeedTextBox.Text);

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private bool IsNumeric(string text)
        {
            var regex = new System.Text.RegularExpressions.Regex("[^0-9.-]+");

            return regex.IsMatch(text);
        }
    }
}
