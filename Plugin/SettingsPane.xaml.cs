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
        private System.Windows.Forms.PropertyValueChangedEventHandler dirtyPropHandler;
        
        TextEditor templateEditor;
        TextEditor cssEditor;

        public SettingsPane()
        {
            InitializeComponent();

            dirtyEventHandler = (object o, EventArgs e) =>
            {
                API.Instance.SetChangedSettings(true);
            };

            dirtyPropHandler = (object o, System.Windows.Forms.PropertyValueChangedEventArgs e) =>
            {
                API.Instance.SetChangedSettings(true);
            };

            cssEditor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas"),
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CSS"),
                ShowLineNumbers = true
            };
            
            templateEditor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas"),
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("HTML"),
                ShowLineNumbers = true,
            };

            cssGrid.Children.Add(cssEditor);
            templatesGrid.Children.Add(templateEditor);

            Reload();

            cssEditor.TextChanged += dirtyEventHandler;
            templateEditor.TextChanged += dirtyEventHandler;
            advancedSettings.PropertyValueChanged += dirtyPropHandler;
        }


        public void Reload()
        {
            cssEditor.Text = BrowserSettings.Instance.SourceSettings.CSS;
            templateEditor.Text = BrowserSettings.Instance.SourceSettings.Template;
            advancedSettings.SelectedObject = BrowserSettings.Instance.InstanceSettings;
        }

    }
}
