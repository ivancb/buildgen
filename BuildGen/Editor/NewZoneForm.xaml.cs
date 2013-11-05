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
    public partial class NewZoneForm : Window
    {
        public string ZoneText;

        public NewZoneForm()
        {
            InitializeComponent();

            ZoneText = null;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(IdTextBox.Text) || string.IsNullOrEmpty(WidthMinTextBox.Text) ||
                    string.IsNullOrEmpty(WidthMaxTextBox.Text) || string.IsNullOrEmpty(HeightMinTextBox.Text) ||
                    string.IsNullOrEmpty(HeightMaxTextBox.Text) || string.IsNullOrEmpty(AmountMinTextBox.Text) ||
                    string.IsNullOrEmpty(AmountMaxTextBox.Text))
                {
                    MessageBox.Show("You must specify values for the id, width, height and amount ranges.");
                }
                else if (TypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("You must select the type for this zone.");
                }
                else
                {
                    ZoneText = "<zone id=\"" + IdTextBox.Text + "\" type=\"" + ((ComboBoxItem)TypeComboBox.SelectedItem).Content + "\"";

                    if (!string.IsNullOrEmpty(ConstraintSetTextBox.Text))
                        ZoneText += " subdivset=\"" + ConstraintSetTextBox.Text + "\"";

                    ZoneText += ">\n<width>";

                    // Width
                    if (WidthMinTextBox.Text == WidthMaxTextBox.Text)
                        ZoneText += "<value>" + WidthMinTextBox.Text + "</value>";
                    else
                        ZoneText += "<range min=\"" + WidthMinTextBox.Text + "\" max=\"" + WidthMaxTextBox.Text + "\"/>";

                    ZoneText += "</width>\n<height>";

                    // Height
                    if (HeightMinTextBox.Text == HeightMaxTextBox.Text)
                        ZoneText += "<value>" + HeightMinTextBox.Text + "</value>";
                    else
                        ZoneText += "<range min=\"" + HeightMinTextBox.Text + "\" max=\"" + HeightMaxTextBox.Text + "\"/>";

                    ZoneText += "</height>\n<amount>";

                    // Amount
                    if (AmountMinTextBox.Text == AmountMaxTextBox.Text)
                        ZoneText += "<value>" + HeightMinTextBox.Text + "</value>";
                    else
                        ZoneText += "<range min=\"" + AmountMinTextBox.Text + "\" max=\"" + AmountMaxTextBox.Text + "\"/>";

                    ZoneText += "</amount>\n</zone>";

                    this.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
