using CLROBS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CLRBrowserSourcePlugin.Shared
{
    internal class BrowserConfig
    {
        public void Reload(XElement element)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            BrowserSourceSettings = AbstractSettings.DeepClone(BrowserSettings.Instance.SourceSettings);
            BrowserInstanceSettings = AbstractSettings.DeepClone(BrowserSettings.Instance.InstanceSettings);

            try
            {
                byte[] instanceSettingsBytes = Convert.FromBase64String(
                    element.GetString("instanceSettings"));
                byte[] sourceSettingsBytes = Convert.FromBase64String(
                    element.GetString("sourceSettings"));

                string instanceSettingsString = Encoding.UTF8.GetString(
                    instanceSettingsBytes);

                string sourceSettingsString = Encoding.UTF8.GetString(
                    sourceSettingsBytes);

                if (sourceSettingsString != null && sourceSettingsString.Count() > 0)
                {
                    MemoryStream stream = new MemoryStream(
                        Encoding.UTF8.GetBytes(sourceSettingsString));

                    DataContractJsonSerializer ser =
                        new DataContractJsonSerializer(
                            typeof(BrowserSourceSettings));

                    BrowserSourceSettings = ser.ReadObject(stream)
                        as BrowserSourceSettings;
                }

                if (instanceSettingsString != null && instanceSettingsString.Count() > 0)
                {
                    BrowserInstanceSettings.MergeWith(serializer.Deserialize<BrowserInstanceSettings>(instanceSettingsString));

                    try
                    {
                        MemoryStream stream = new MemoryStream(
                            Encoding.UTF8.GetBytes(instanceSettingsString));

                        DataContractJsonSerializer ser =
                            new DataContractJsonSerializer(
                                typeof(BrowserInstanceSettings));
                        BrowserInstanceSettings serializedSettings =
                            ser.ReadObject(stream) as BrowserInstanceSettings;

                        BrowserInstanceSettings.MergeWith(serializedSettings);
                    }
                    catch (Exception e)
                    {
                        API.Instance.Log(
                            "Failed to deserialized source settings and forced to recreate");
                        API.Instance.Log("Exception: {0}", e);
                    }
                }
            }
            catch (Exception e)
            {
                API.Instance.Log(
                    "Failed to deserialized source settings and forced to recreate");
                API.Instance.Log("Exception: {0}", e);
            }
        }

        public bool Save(XElement element)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            try
            {
                String sourceSettings;
                String instanceSettings;

                MemoryStream stream = new MemoryStream();

                DataContractJsonSerializer ser = new DataContractJsonSerializer(
                    typeof(BrowserSourceSettings));

                ser.WriteObject(stream, BrowserSourceSettings);

                sourceSettings = Convert.ToBase64String(stream.ToArray());

                stream = new MemoryStream();
                ser = new DataContractJsonSerializer(
                    typeof(BrowserInstanceSettings));

                ser.WriteObject(stream, BrowserInstanceSettings);

                instanceSettings = Convert.ToBase64String(stream.ToArray());

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

        #endregion Properties
    }
}