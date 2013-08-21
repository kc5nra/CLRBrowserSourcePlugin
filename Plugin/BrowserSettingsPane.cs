using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using CLROBS;
using CLRBrowserSourcePlugin.Shared;

namespace CLRBrowserSourcePlugin
{
    class BrowserSettingsPane : AbstractWPFSettingsPane
    {

        private SettingsPane settingsPane;

        public BrowserSettingsPane()
        {
            Category = "Browser";   
        }

        public override void ApplySettings()
        {
            BrowserSettings.Instance.SourceSettings.CSS = settingsPane.CSSEditor.Text;
            BrowserSettings.Instance.SourceSettings.Template = settingsPane.TemplateEditor.Text;
            BrowserSettings.Instance.SourceSettings.IsShowingAdvancedProperties = settingsPane.AdvancedPropertiesCheckBox.IsChecked.GetValueOrDefault();

            BrowserSettings.Instance.Save();
        }

        public override void CancelSettings()
        {
            BrowserSettings.Instance.Reload();
        }

        public override UIElement CreateUIElement()
        {
            return settingsPane = new SettingsPane();
        }

        public override bool HasDefaults()
        {
            return true;
        }

        public override void SetDefaults()
        {
            BrowserSettings.Instance.Reset();
            if (settingsPane != null)
            {
                settingsPane.Reload();
            }
        }
    }
}
