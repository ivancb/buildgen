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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Editor
{
    public class DocumentTabItem : TabItem
    {
        private bool documentWasModified = false;

        public string Filename;

        static DocumentTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DocumentTabItem), new FrameworkPropertyMetadata(typeof(DocumentTabItem)));
        }

        public bool DocumentModified
        {
            get { return documentWasModified; }
            set 
            { 
                documentWasModified = value; 
                ToggleDocumentModifiedIconVisibility(); 
            }
        }

        public static readonly RoutedEvent CloseTabEvent = EventManager.RegisterRoutedEvent("CloseTab", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(DocumentTabItem));

        public event RoutedEventHandler Close
        {
            add { AddHandler(CloseTabEvent, value); }
            remove { RemoveHandler(CloseTabEvent, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Setup the event handler for the close button
            Button closeButton = (Button)base.GetTemplateChild("CloseBtn");
            if (closeButton != null)
            {
                closeButton.Click += new RoutedEventHandler(closeButton_Click);
            }

            Canvas documentModifiedIcon = (Canvas)base.GetTemplateChild("DocumentModifiedIcon");
            documentModifiedIcon.Visibility = Visibility.Hidden;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
        }

        private void ToggleDocumentModifiedIconVisibility()
        {
            Canvas documentModifiedIcon = (Canvas)base.GetTemplateChild("DocumentModifiedIcon");
            documentModifiedIcon.Visibility = (documentWasModified ? Visibility.Visible : Visibility.Hidden);
        }
    }
}
