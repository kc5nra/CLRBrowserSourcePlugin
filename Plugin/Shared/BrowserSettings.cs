using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

using CLROBS;
using System.Web.Script.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace CLRBrowserSourcePlugin.Shared
{
    [Serializable]
    public abstract class AbstractSettings
    {
        public void MergeWith(AbstractSettings secondary)
        {

            PropertyDescriptorCollection thisPropertyDescriptors = TypeDescriptor.GetProperties(this);
            PropertyDescriptorCollection secondaryPropertyDescriptors = TypeDescriptor.GetProperties(secondary);
            for (int i = 0; i < thisPropertyDescriptors.Count; i++)
            {
                PropertyDescriptor thisDescriptor = thisPropertyDescriptors[i];
                PropertyDescriptor secondaryDescriptor = secondaryPropertyDescriptors[i];

                var defaultValueAttr = thisDescriptor.Attributes.OfType<DefaultValueAttribute>();
                if (defaultValueAttr.Count() >= 0)
                {
                    object defaultValue = defaultValueAttr.First().Value;
                    object thisValue = thisDescriptor.GetValue(this);
                    object secondaryValue = secondaryDescriptor.GetValue(secondary);

                    // our object is default, so copy the secondary value
                    if (Object.Equals(defaultValue, thisValue))
                    {
                        thisDescriptor.SetValue(this, secondaryValue);
                    }
                    else
                    {
                        // the guy we are merging with is NOT default value
                        if (!Object.Equals(defaultValue, secondaryValue))
                        {
                            thisDescriptor.SetValue(this, secondaryValue);
                        }
                    }
                }
            }
        }

        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
    }

    public class BrowserSettings
    {
        private static BrowserSettings instance;

        [Serializable]
        public class SerializedSettings
        {
            public BrowserSourceSettings SourceSettings { get; set; }
            public BrowserInstanceSettings InstanceSettings { get; set; }
            public BrowserRuntimeSettings RuntimeSettings { get; set; }
            public BrowserPluginSettings PluginSettings { get; set; }
        }

        private SerializedSettings serializedSettings;
        private String settingsLocation;

        public BrowserSettings()
        {
            settingsLocation = Path.Combine(API.Instance.GetPluginDataPath(), "browser.json");

            Reload();
        }

        public void Reset()
        {
            // initialize with default
            serializedSettings = new SerializedSettings();
            serializedSettings.SourceSettings = new BrowserSourceSettings();
            serializedSettings.InstanceSettings = new BrowserInstanceSettings();
            serializedSettings.RuntimeSettings = new BrowserRuntimeSettings();
        }

        public void Reload()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            try
            {
                using (var reader = new StreamReader(settingsLocation))
                {
                    serializedSettings = serializer.Deserialize<SerializedSettings>(reader.ReadToEnd());

                };
            }
            catch (Exception)
            {
                API.Instance.Log("Could not find/load browser settings at location {0}", settingsLocation);
            }

            if (serializedSettings == null)
            {
                // initialize with default
                serializedSettings = new SerializedSettings();
            }

            if (serializedSettings.SourceSettings == null)
            {
                serializedSettings.SourceSettings = new BrowserSourceSettings();
            }
            if (serializedSettings.InstanceSettings == null)
            {
                serializedSettings.InstanceSettings = new BrowserInstanceSettings();
            }
            if (serializedSettings.RuntimeSettings == null)
            {
                serializedSettings.RuntimeSettings = new BrowserRuntimeSettings();
            }
            if (serializedSettings.RuntimeSettings.Plugins == null)
            {
                serializedSettings.RuntimeSettings.Plugins = new List<BrowserPlugin>();
            }
            if (serializedSettings.PluginSettings == null)
            {
                serializedSettings.PluginSettings = new BrowserPluginSettings();
            }
            if (serializedSettings.PluginSettings.CustomPluginDirectories == null)
            {
                serializedSettings.PluginSettings.CustomPluginDirectories = new List<String>();
            }
            if (serializedSettings.PluginSettings.CustomPluginPaths == null)
            {
                serializedSettings.PluginSettings.CustomPluginPaths = new List<String>();
            }
            if (serializedSettings.PluginSettings.DisabledPlugins == null)
            {
                serializedSettings.PluginSettings.DisabledPlugins = new List<String>();
            }
            
        }

        public void Save()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            try
            {
                using (var writer = new StreamWriter(settingsLocation))
                {
                    writer.Write(JsonHelper.FormatJson(serializer.Serialize(serializedSettings)));
                }
            }
            catch (Exception)
            {
                API.Instance.Log("Could not save browser settings to location {0}", settingsLocation);
            }
        }

        public BrowserSourceSettings SourceSettings
        {
            get { return serializedSettings.SourceSettings; }
        }

        public BrowserInstanceSettings InstanceSettings
        {
            get { return serializedSettings.InstanceSettings; }
        }

        public BrowserRuntimeSettings RuntimeSettings
        {
            get { return serializedSettings.RuntimeSettings; }
        }

        public BrowserPluginSettings PluginSettings
        {
            get { return serializedSettings.PluginSettings; }
        }

        public static BrowserSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BrowserSettings();
                }

                return instance;
            }
        }
    }

    class JsonHelper
    {
        private const string INDENT_STRING = "    ";
        public static string FormatJson(string str)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
    }

    static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }
    }

    [Serializable]
    public class BrowserSourceSettings
    {
        public BrowserSourceSettings()
        {
            Width = 640;
            Height = 480;
            Url = "http://www.obsproject.com";
            CSS =
                  "::-webkit-scrollbar {\r\n"
                + "  visibility: hidden;\r\n"
                + "}\r\n"
                + "body {\r\n"
                + "  background-color: rgba(0, 0, 0, 0);\r\n"
                + "  margin: 0px auto;\r\n"
                + "}\r\n";
            Template =
                  "<html>\r\n"
                + "  <head>\r\n"
                + " 	  <meta charset='utf-8'/>\r\n"
                + "  </head>\r\n"
                + "  <body>\r\n"
                + "    <object width='$(WIDTH)' height='$(HEIGHT)'>\r\n"
                + "      <param name='movie' value='$(FILE)'></param>\r\n"
                + "      <param name='allowscriptaccess' value='always'></param>\r\n"
                + "      <param name='wmode' value='transparent'></param>\r\n"
                + "      <embed \r\n"
                + "        src='$(FILE)' \r\n"
                + "        type='application/x-shockwave-flash' \r\n"
                + "        allowscriptaccess='always' \r\n"
                + "        width='$(WIDTH)' \r\n"
                + "        height='$(HEIGHT)' \r\n"
                + "        wmode='transparent'>\r\n"
                + "      </embed>\r\n"
                + "    </object>\r\n"
                + "  </body>\r\n"
                + "</html>\r\n";
            IsApplyingTemplate = false;
            IsShowingAdvancedProperties = false;
            Opacity = 1.0f;
        }

        public bool IsApplyingTemplate { get; set; }
        public bool IsShowingAdvancedProperties { get; set; }
        public String Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public String CSS { get; set; }
        public String Template { get; set; }
        public double Opacity { get; set; }
    }

    [Serializable]
    public class BrowserInstanceSettings : AbstractSettings
    {
        // The below values map to WebPreferences settings.
        public BrowserInstanceSettings()
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this);
            for (int i = 0; i < properties.Count; i++)
            {
                DefaultValueAttribute attr = properties[i].Attributes[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
                if (attr == null) continue;
                properties[i].SetValue(this, attr.Value);
            }
        }

        #region Font

        [DefaultValue(null)]
        [Category("Fonts")]
        [Description("Standard font family.")]
        public string StandardFontFamily { get; set; }

        [DefaultValue(null)]
        [Category("Fonts")]
        [Description("Fixed font family.")]
        public string FixedFontFamily { get; set; }

        [DefaultValue(null)]
        [Category("Fonts")]
        [Description("Serif font family.")]
        public string SerifFontFamily { get; set; }

        [DefaultValue(null)]
        [Category("Fonts")]
        [Description("Sans Serif font family.")]
        public string SansSerifFontFamily { get; set; }

        [DefaultValue(null)]
        [Category("Fonts")]
        [Description("Crusive font family.")]
        public string CursiveFontFamily { get; set; }

        [DefaultValue(null)]
        [Category("Fonts")]
        [Description("Fantasy font family.")]
        public string FantasyFontFamily { get; set; }

        [DefaultValue(0)]
        [Category("Fonts")]
        [Description("Default font size.")]
        public int DefaultFontSize { get; set; }

        [DefaultValue(0)]
        [Category("Fonts")]
        [Description("Default fixed font size.")]
        public int DefaultFixedFontSize { get; set; }

        [DefaultValue(0)]
        [Category("Fonts")]
        [Description("Minimum font size.")]
        public int MinimumFontSize { get; set; }

        [DefaultValue(0)]
        [Category("Fonts")]
        [Description("Minimum logical font size.")]
        public int MinimumLogicalFontSize { get; set; }

        /// <summary>
        /// Controls the loading of fonts from remote sources. Also configurable using
        /// the "disable-remote-fonts" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Fonts")]
        [Description("Controls the loading of fonts from remote sources.")]
        public CefState RemoteFonts { get; set; }

        #endregion

        #region Misc
        /// <summary>
        /// Default encoding for Web content. If empty "ISO-8859-1" will be used. Also
        /// configurable using the "default-encoding" command-line switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("Miscellaneous")]
        [Description("Default encoding for Web content. If empty 'ISO-8859-1' will be used.")]
        public string DefaultEncoding { get; set; }

        /// <summary>
        /// Location of the user style sheet that will be used for all pages. This must
        /// be a data URL of the form "data:text/css;charset=utf-8;base64,csscontent"
        /// where "csscontent" is the base64 encoded contents of the CSS file. Also
        /// configurable using the "user-style-sheet-location" command-line switch.
        /// </summary>
        //public string UserStyleSheetLocation { get { return ""; } }

        /// <summary>
        /// Controls whether the tab key can advance focus to links. Also configurable
        /// using the "disable-tab-to-links" command-line switch.
        /// </summary>
        //public CefState TabToLinks { get; set; }

        /// <summary>
        /// Controls whether text areas can be resized. Also configurable using the
        /// "disable-text-area-resize" command-line switch.
        /// </summary>
        //public CefState TextAreaResize { get { return CefState.Default; } }

        /// <summary>
        /// Controls whether style sheets can be used. Also configurable using the
        /// "disable-author-and-user-styles" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Miscellaneous")]
        [Description("Controls whether style sheets can be used.")]
        public CefState AuthorAndUserStyles { get; set; }

        /// <summary>
        /// Controls whether developer tools (WebKit inspector) can be used. Also
        /// configurable using the "disable-developer-tools" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Miscellaneous")]
        [Description("Controls whether developer tools (WebKit inspector) can be used.")]
        public CefState DeveloperTools { get; set; }

        #endregion

        #region JavaScript

        /// <summary>
        /// Controls whether JavaScript can be executed. Also configurable using the
        /// "disable-javascript" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("JavaScript")]
        [Description("Controls whether JavaScript can be executed.")]
        public CefState JavaScript { get; set; }

        /// <summary>
        /// Controls whether JavaScript can be used for opening windows. Also
        /// configurable using the "disable-javascript-open-windows" command-line
        /// switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("JavaScript")]
        [Description("Controls whether JavaScript can be used for opening windows.")]
        public CefState JavaScriptOpenWindows { get; set; }

        /// <summary>
        /// Controls whether JavaScript can be used to close windows that were not
        /// opened via JavaScript. JavaScript can still be used to close windows that
        /// were opened via JavaScript. Also configurable using the
        /// "disable-javascript-close-windows" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("JavaScript")]
        [Description("Controls whether JavaScript can be used to close windows that were not opened via JavaScript. JavaScript can still be used to close windows that were opened via JavaScript.")]
        public CefState JavaScriptCloseWindows { get; set; }

        /// <summary>
        /// Controls whether JavaScript can access the clipboard. Also configurable
        /// using the "disable-javascript-access-clipboard" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("JavaScript")]
        [Description("Controls whether JavaScript can access the clipboard.")]
        public CefState JavaScriptAccessClipboard { get; set; }

        /// <summary>
        /// Controls whether DOM pasting is supported in the editor via
        /// execCommand("paste"). The |javascript_access_clipboard| setting must also
        /// be enabled. Also configurable using the "disable-javascript-dom-paste"
        /// command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("JavaScript")]
        [Description("Controls whether DOM pasting is supported in the editor via execCommand('paste'). The JavaScriptAccessClipboard setting must also be enabled.")]
        public CefState JavaScriptDomPaste { get; set; }

        /// <summary>
        /// Controls whether the caret position will be drawn. Also configurable using
        /// the "enable-caret-browsing" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("JavaScript")]
        [Description("Controls whether the caret position will be drawn.")]
        public CefState CaretBrowsing { get; set; }

        #endregion

        #region Plugin

        /// <summary>
        /// Controls whether any plugins will be loaded. Also configurable using the
        /// "disable-plugins" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Plugins")]
        [Description("Controls whether any plugins will be loaded.")]
        public CefState Plugins { get; set; }

        /// <summary>
        /// Controls whether the Java plugin will be loaded. Also configurable using
        /// the "disable-java" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Plugins")]
        [Description("Controls whether the Java plugin will be loaded.")]
        public CefState Java { get; set; }

        #endregion

        #region Security

        /// <summary>
        /// Controls whether file URLs will have access to all URLs. Also configurable
        /// using the "allow-universal-access-from-files" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Enabled)]
        [Category("Security")]
        [Description("Controls whether file URLs will have access to all URLs.")]
        public CefState UniversalAccessFromFileUrls { get; set; }

        /// <summary>
        /// Controls whether file URLs will have access to other file URLs. Also
        /// configurable using the "allow-access-from-files" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Enabled)]
        [Category("Security")]
        [Description("Controls whether file URLs will have access to other file URLs.")]
        public CefState FileAccessFromFileUrls { get; set; }

        /// <summary>
        /// Controls whether web security restrictions (same-origin policy) will be
        /// enforced. Disabling this setting is not recommend as it will allow risky
        /// security behavior such as cross-site scripting (XSS). Also configurable
        /// using the "disable-web-security" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Disabled)]
        [Category("Security")]
        [Description("Controls whether web security restrictions (same-origin policy) will be enforced. Disabling this setting is not recommend as it will allow risky security behavior such as cross-site scripting (XSS).")]
        public CefState WebSecurity { get; set; }

        #endregion

        #region Images

        /// <summary>
        /// Controls whether image URLs will be loaded from the network. A cached image
        /// will still be rendered if requested. Also configurable using the
        /// "disable-image-loading" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Images")]
        [Description("Controls whether image URLs will be loaded from the network. A cached image will still be rendered if requested.")]
        public CefState ImageLoading { get; set; }

        /// <summary>
        /// Controls whether standalone images will be shrunk to fit the page. Also
        /// configurable using the "image-shrink-standalone-to-fit" command-line
        /// switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Images")]
        [Description("Controls whether standalone images will be shrunk to fit the page.")]
        public CefState ImageShrinkStandaloneToFit { get; set; }

        #endregion

        #region Storage

        /// <summary>
        /// Controls whether local storage can be used. Also configurable using the
        /// "disable-local-storage" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Storage")]
        [Description("Controls whether local storage can be used.")]
        public CefState LocalStorage { get; set; }

        /// <summary>
        /// Controls whether databases can be used. Also configurable using the
        /// "disable-databases" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Storage")]
        [Description("Controls whether databases can be used.")]
        public CefState Databases { get; set; }

        #endregion

        #region Cache

        /// <summary>
        /// Controls whether the fastback (back/forward) page cache will be used. Also
        /// configurable using the "enable-fastback" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Cache")]
        [Description("Controls whether the fastback (back/forward) page cache will be used.")]
        public CefState PageCache { get; set; }

        /// <summary>
        /// Controls whether the application cache can be used. Also configurable using
        /// the "disable-application-cache" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Cache")]
        [Description("Controls whether the application cache can be used.")]
        public CefState ApplicationCache { get; set; }

        #endregion

        #region Hardware Support

        /// <summary>
        /// Controls whether WebGL can be used. Note that WebGL requires hardware
        /// support and may not work on all systems even when enabled. Also
        /// configurable using the "disable-webgl" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Hardware Support")]
        [Description("Controls whether WebGL can be used. Note that WebGL requires hardware support and may not work on all systems even when enabled.")]
        public CefState WebGL { get; set; }

        /// <summary>
        /// Controls whether content that depends on accelerated compositing can be
        /// used. Note that accelerated compositing requires hardware support and may
        /// not work on all systems even when enabled. Also configurable using the
        /// "disable-accelerated-compositing" command-line switch.
        /// </summary>
        [DefaultValue(CefState.Default)]
        [Category("Hardware Support")]
        [Description("Controls whether content that depends on accelerated compositing can be used. Note that accelerated compositing requires hardware support and may not work on all systems even when enabled.")]
        public CefState AcceleratedCompositing { get; set; }

        #endregion

    }

    [Serializable]
    public class BrowserPluginSettings
    {
        public List<String> CustomPluginDirectories;
        public List<String> CustomPluginPaths;
        public List<String> DisabledPlugins;
    }

    [Serializable]
    public class BrowserRuntimeSettings
    {

        private List<BrowserPlugin> plugins;

        public BrowserRuntimeSettings()
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this);
            for(int i = 0; i < properties.Count; i++)
            {
                DefaultValueAttribute attr = properties[i].Attributes[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
                if (attr == null) continue;
                properties[i].SetValue(this, attr.Value);
            }
        }

        #region Core Settings

        /// <summary>
        /// Set to <c>true</c> to use a single process for the browser and renderer. This
        /// run mode is not officially supported by Chromium and is less stable than
        /// the multi-process default. Also configurable using the "single-process"
        /// command-line switch.
        /// </summary>
        [DefaultValue(false)]
        [Category("Core")]
        [Description("Set to true to use a single process for the browser and renderer. This run mode is not officially supported by Chromium and is less stable than the multi-process default.")]
        public bool SingleProcess { get; set; }

        ///// <summary>
        ///// Set to <c>true</c> to have the browser process message loop run in a separate
        ///// thread. If <c>false</c> than the CefDoMessageLoopWork() function must be
        ///// called from your application message loop.
        ///// </summary>
        //public bool MultiThreadedMessageLoop { get; }

        [DefaultValue(new String[] { "--no-proxy-server" })]
        [Category("Core")]
        [Description("Chrome command line arguments. (Advanced!)")]
        public String[] CommandLineArguments { get; set; }

        /// <summary>
        /// Set to <c>true</c> to disable configuration of browser process features using
        /// standard CEF and Chromium command-line arguments. Configuration can still
        /// be specified using CEF data structures or via the
        /// CefApp::OnBeforeCommandLineProcessing() method.
        /// </summary>
        [DefaultValue(false)]
        [Category("Core")]
        [Description("Set to true to disable configuration of browser process features using standard CEF and Chromium command-line arguments.")]
        public bool CommandLineArgsDisabled { get; set; }

        /// <summary>
        /// Set to <c>true</c> to have the browser process message loop run in a separate
        /// thread. If <c>false</c> than the CefDoMessageLoopWork() function must be
        /// called from your application message loop.
        /// </summary>
        [DefaultValue(true)]
        [Category("Core")]
        [Description("Set to true to have the browser process message loop run in a separate thread.")]
        public bool MultiThreadedMessageLoop { get; set; }

        [DefaultValue(5000)]
        [Category("Core")]
        [Description("Maximum amount of time to wait for browsers to gracefully close.  The short amout of time this is, the more likely there might be a crash with beta CEF builds.")]
        public int MaximumBrowserKillWaitTime { get; set; }

        [Browsable(false)]
        public List<BrowserPlugin> Plugins
        {
            get { return this.plugins; }
            internal set { this.plugins = value; }
        }

        // this (below) is the one the PropertyGrid will use
        [DisplayName("Plugins")]
        [Category("Core")]
        [Description("List of all the plugins currently installed and available to browser sources")]
        public ReadOnlyCollection<BrowserPlugin> ReadOnlyPlugins
        {
            get { return this.plugins.AsReadOnly(); }
        }

        #endregion

        #region Resources

        /// <summary>
        /// The location where cache data will be stored on disk. If empty an in-memory
        /// cache will be used. HTML5 databases such as localStorage will only persist
        /// across sessions if a cache path is specified.
        /// </summary>
        [DefaultValue(null)]
        [Category("Resources")]
        [Description("The location where cache data will be stored on disk. If empty an in-memory cache will be used. HTML5 databases such as localStorage will only persist across sessions if a cache path is specified.")]
        public string CachePath { get; set; }

        /// <summary>
        /// To persist session cookies (cookies without an expiry date or validity
        /// interval) by default when using the global cookie manager set this value to
        /// true. Session cookies are generally intended to be transient and most Web
        /// browsers do not persist them. A |cache_path| value must also be specified to
        /// enable this feature. Also configurable using the "persist-session-cookies"
        /// command-line switch.
        /// </summary>
        [DefaultValue(false)]
        [Category("Resources")]
        [Description("To persist session cookies (cookies without an expiry date or validity interval) by default when using the global cookie manager set this value to true. A CachePath value must also be specified to enable this feature.")]
        public bool PersistSessionCookies { get; set; }

        /// <summary>
        /// The fully qualified path for the resources directory. If this value is
        /// empty the cef.pak and/or devtools_resources.pak files must be located in
        /// the module directory on Windows/Linux or the app bundle Resources directory
        /// on Mac OS X. Also configurable using the "resources-dir-path" command-line
        /// switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("Resources")]
        [Description("The fully qualified path for the resources directory. If this value is empty the cef.pak and/or devtools_resources.pak files must be located in the module directory.")]
        public string ResourcesDirPath { get; set; }

        

        #endregion

        #region Locale

        /// <summary>
        /// The locale string that will be passed to WebKit. If empty the default
        /// locale of "en-US" will be used. This value is ignored on Linux where locale
        /// is determined using environment variable parsing with the precedence order:
        /// LANGUAGE, LC_ALL, LC_MESSAGES and LANG. Also configurable using the "lang"
        /// command-line switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("Locale")]
        [Description("The locale string that will be passed to WebKit. If empty the default locale of 'en-US' will be used.")]
        public string Locale { get; set; }

        /// <summary>
        /// The fully qualified path for the locales directory. If this value is empty
        /// the locales directory must be located in the module directory. This value
        /// is ignored on Mac OS X where pack files are always loaded from the app
        /// bundle resource directory. Also configurable using the "locales-dir-path"
        /// command-line switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("Locale")]
        [Description("The fully qualified path for the locales directory. If this value is empty the locales directory must be located in the module directory.")]
        public string LocalesDirPath { get; set; }


        #endregion

        #region Behavior

        /// <summary>
        /// Value that will be returned as the User-Agent HTTP header. If empty the
        /// default User-Agent string will be used. Also configurable using the
        /// "user-agent" command-line switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("Behavior")]
        [Description("Value that will be returned as the User-Agent HTTP header. If empty the default User-Agent string will be used.")]
        public string UserAgent { get; set; }

        /// <summary>
        /// Value that will be inserted as the product portion of the default
        /// User-Agent string. If empty the Chromium product version will be used. If
        /// |userAgent| is specified this value will be ignored. Also configurable
        /// using the "product-version" command-line switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("Behavior")]
        [Description("Value that will be inserted as the product portion of the default User-Agent string. If empty the Chromium product version will be used. If UserAgent is specified this value will be ignored.")]
        public string ProductVersion { get; set; }

        #endregion

        #region Security

        /// <summary>
        /// Set to true (1) to ignore errors related to invalid SSL certificates.
        /// Enabling this setting can lead to potential security vulnerabilities like
        /// "man in the middle" attacks. Applications that load content from the
        /// internet should not enable this setting. Also configurable using the
        /// "ignore-certificate-errors" command-line switch.
        /// </summary>
        [DefaultValue(false)]
        [Category("Security")]
        [Description("Set to true (1) to ignore errors related to invalid SSL certificates. Enabling this setting can lead to potential security vulnerabilities like 'man in the middle' attacks. Applications that load content from the internet should not enable this setting.")]
        public bool IgnoreCertificateErrors { get; set; }

        #endregion

        #region Logging

        /// <summary>
        /// The directory and file name to use for the debug log. If empty, the
        /// default name of "debug.log" will be used and the file will be written
        /// to the application directory. Also configurable using the "log-file"
        /// command-line switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("Logging")]
        [Description("The directory and file name to use for the debug log. If empty, the default name of 'debug.log' will be used and the file will be written to the application directory.")]
        public string LogFile { get; set; }

        /// <summary>
        /// The log severity. Only messages of this severity level or higher will be
        /// logged. Also configurable using the "log-severity" command-line switch with
        /// a value of "verbose", "info", "warning", "error", "error-report" or
        /// "disable".
        /// </summary>
        [DefaultValue(CefLogSeverity.Default)]
        [Category("Logging")]
        [Description("The log severity. Only messages of this severity level or higher will be logged. Verbose < Info < Warning < Error < ErrorReport < Disable")]
        public CefLogSeverity LogSeverity { get; set; }

        #endregion

        #region Debugging

        /// <summary>
        /// Enable DCHECK in release mode to ease debugging.  Also configurable using the
        /// "enable-release-dcheck" command-line switch.
        /// </summary>
        [DefaultValue(false)]
        [Category("Debugging")]
        [Description("Enable DCHECK in release mode to ease debugging.")]
        public bool ReleaseDCheckEnabled { get; set; }

        /// <summary>
        /// Set to a value between 1024 and 65535 to enable remote debugging on the
        /// specified port. For example, if 8080 is specified the remote debugging URL
        /// will be http://localhost:8080. CEF can be remotely debugged from any CEF or
        /// Chrome browser window. Also configurable using the "remote-debugging-port"
        /// command-line switch.
        /// </summary>
        [DefaultValue(0)]
        [Category("Debugging")]
        [Description("Set to a value between 1024 and 65535 to enable remote debugging on the specified port. If 8080 the debugging URL will be http://localhost:8080. Use chrome to view this site.")]
        public int RemoteDebuggingPort { get; set; }

        /// <summary>
        /// The number of stack trace frames to capture for uncaught exceptions.
        /// Specify a positive value to enable the CefV8ContextHandler::
        /// OnUncaughtException() callback. Specify 0 (default value) and
        /// OnUncaughtException() will not be called. Also configurable using the
        /// "uncaught-exception-stack-size" command-line switch.
        /// </summary>
        [DefaultValue(0)]
        [Category("Debugging")]
        [Description("The number of stack trace frames to capture for uncaught exceptions. Specify a positive value to enable the OnUncaughtException() callback. Specify 0 (default value) to disable.")]
        public int UncaughtExceptionStackSize { get; set; }

        #endregion

        #region JavaScript

        /// <summary>
        /// Custom flags that will be used when initializing the V8 JavaScript engine.
        /// The consequences of using custom flags may not be well tested. Also
        /// configurable using the "js-flags" command-line switch.
        /// </summary>
        [DefaultValue(null)]
        [Category("JavaScript")]
        [Description("Custom flags that will be used when initializing the V8 JavaScript engine. The consequences of using custom flags may not be well tested.")]
        public string JavaScriptFlags { get; set; }

        #endregion

    }
}
