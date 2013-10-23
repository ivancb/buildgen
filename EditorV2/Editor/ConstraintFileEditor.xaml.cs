using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Editor
{
    public partial class ConstraintFileEditor : UserControl
    {
        public delegate void ContentsModifiedHandler(object sender);
        public event ContentsModifiedHandler ContentsModified;

        public ConstraintFileEditor()
        {
            InitializeComponent();
        }

        public string Text
        {
            get 
            {
                return TextEditor.Text;
            }
            set
            {
                TextEditor.Text = value;
            }
        }

        private void TextEditor_TextInput(object sender, TextCompositionEventArgs e)
        {
            ContentsModified(this);
        }

        private void NewZoneBtn_Click(object sender, RoutedEventArgs e)
        {
            NewZoneForm nform = new NewZoneForm();
            bool? ret = nform.ShowDialog();

            if (ret.HasValue && ret.Value)
            {
                int closingTag = Text.IndexOf("</constraints>");

                if (closingTag == -1)
                {
                    MessageBox.Show("Invalid file structure.");
                }
                else
                {
                    int lastSetClosingTagIndex = Text.LastIndexOf("</floorconstraint>");

                    if (lastSetClosingTagIndex == -1)
                        Text = Text.Insert(closingTag, "<set name=\"GENERATED_SET\">\n<floorconstraint>\n" + nform.ZoneText + "\n</floorconstraint>\n</set>\n");
                    else
                        Text = Text.Insert(lastSetClosingTagIndex, nform.ZoneText + "\n");

                    ContentsModified(this);
                }
            }
        }
    }
}
