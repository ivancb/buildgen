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
    /// <summary>
    /// Window that provides a preview of the output from the generator by loading the final .bmp files for each floor.
    /// Also allows the user to issue a request to re-generate the previewed building.
    /// </summary>
    public partial class PreviewWindow : Window, IDisposable
    {
        private string BasePath;
        private int TotalFloorCount;

        public PreviewWindow()
        {
            InitializeComponent();

            BasePath = "";
            TotalFloorCount = 0;
        }

        public void Initialize(string basePath, int floorCount)
        {
            BasePath = basePath;
            TotalFloorCount = floorCount;

            Thickness widgetMargin = new Thickness(0f, 0f, 5f, 0f);

            for (int n = 0; n < TotalFloorCount; n++)
            {
                // NOTE: Necessary since the delegate captures by reference instead of by value
                int floorIndex = n;

                Button floorButton = new Button();
                floorButton.Content = n.ToString();
                floorButton.Width = 20;
                floorButton.Margin = widgetMargin;
                floorButton.Click += delegate { ShowPreview(floorIndex); };

                FloorPanel.Children.Add(floorButton);
            }

            ShowPreview(0);
        }

        public void Dispose()
        {
            PreviewImage.Source = null;
        }

        private void ShowPreview(int floorIndex)
        {
            PreviewImage.Source = new BitmapImage(new Uri(BasePath + floorIndex + ".bmp", UriKind.Absolute));
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
