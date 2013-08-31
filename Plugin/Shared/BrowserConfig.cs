using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CLROBS;
using System.Web.Script.Serialization;

namespace CLRBrowserSourcePlugin.Shared
{
    class BrowserConfig
    {

        public void Reload(XElement element)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            BrowserSourceSettings = AbstractSettings.DeepClone(BrowserSettings.Instance.SourceSettings);
            BrowserInstanceSettings = AbstractSettings.DeepClone(BrowserSettings.Instance.InstanceSettings);

            String instanceSettingsString = element.GetString("instanceSettings");
            String sourceSettingsString = element.GetString("sourceSettings");

            if (sourceSettingsString != null && sourceSettingsString.Count() > 0)
            {
                try
                {
                    BrowserSourceSettings = serializer.Deserialize<BrowserSourceSettings>(sourceSettingsString);
                }
                catch (ArgumentException e)
                {
                    API.Instance.Log("Failed to deserialized source settings and forced to recreate; {0}", e.Message);
                }
            }

            if (instanceSettingsString != null && instanceSettingsString.Count() > 0)
            {
                BrowserInstanceSettings.MergeWith(serializer.Deserialize<BrowserInstanceSettings>(instanceSettingsString));
            }

        }

        public bool Save(XElement element)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            try
            {
                String sourceSettings = serializer.Serialize(BrowserSourceSettings);
                String instanceSettings = serializer.Serialize(BrowserInstanceSettings);

                element.SetString("sourceSettings", sourceSettings);
                element.SetString("instanceSettings", instanceSettings);
            }
            catch (Exception ex)
            {
                API.Instance.Log("Failed to save browser configuration; {0}", ex);
                return false;
            }

            return true;

        }

        #region Properties

        public BrowserSourceSettings BrowserSourceSettings { get; set; }
        public BrowserInstanceSettings BrowserInstanceSettings { get; set; }

        #endregion
    }
}
