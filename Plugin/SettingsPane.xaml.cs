using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Xilium.CefGlue;

using CLRBrowserSourcePlugin.Shared;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Folding;

using CLROBS;

namespace CLRBrowserSourcePlugin
{
    /// <summary>
    /// Interaction logic for SettingsPane.xaml
    /// </summary>
    public partial class SettingsPane : UserControl
    {
        private EventHandler dirtyEventHandler;
        private RoutedEventHandler dirtyRoutedHandler;
        private System.Windows.Forms.PropertyValueChangedEventHandler dirtyPropHandler;

        public SettingsPane()
        {
            InitializeComponent();

            dirtyEventHandler = (object o, EventArgs e) =>
            {
                API.Instance.SetChangedSettings(true);
            };

            dirtyRoutedHandler = (object o, RoutedEventArgs e) =>
            {
                API.Instance.SetChangedSettings(true);
            };

            dirtyPropHandler = (object o, System.Windows.Forms.PropertyValueChangedEventArgs e) =>
            {
                API.Instance.SetChangedSettings(true);
            };

            CSSEditor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas"),
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CSS"),
                ShowLineNumbers = true
            };
            
            TemplateEditor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas"),
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("HTML"),
                ShowLineNumbers = true,
            };

            cssGrid.Children.Add(CSSEditor);
            templatesGrid.Children.Add(TemplateEditor);

            AdvancedPropertiesCheckBox = advancedPropertiesCheckBox;

            Reload();

            CSSEditor.TextChanged += dirtyEventHandler;
            TemplateEditor.TextChanged += dirtyEventHandler;
            advancedSettings.PropertyValueChanged += dirtyPropHandler;
            advancedPropertiesCheckBox.Checked += dirtyRoutedHandler;
            advancedPropertiesCheckBox.Unchecked += dirtyRoutedHandler;
        }


        public void Reload()
        {
            CSSEditor.Text = BrowserSettings.Instance.SourceSettings.CSS;
            TemplateEditor.Text = BrowserSettings.Instance.SourceSettings.Template;
            advancedSettings.SelectedObject = BrowserSettings.Instance.InstanceSettings;
            advancedPropertiesCheckBox.IsChecked = BrowserSettings.Instance.SourceSettings.IsShowingAdvancedProperties;
        }

        public TextEditor CSSEditor { get; private set; }
        public TextEditor TemplateEditor { get; private set; }
        public CheckBox AdvancedPropertiesCheckBox { get; private set; }

    }
}
