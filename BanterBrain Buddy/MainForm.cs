using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Speech.Recognition;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Reflection;
using Newtonsoft.Json;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using BanterBrain_Buddy.Properties;
using System.Globalization;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Configuration;
using Windows.Web.UI;
using TwitchLib.Api.Helix.Models.Extensions.ReleasedExtensions;

/// <summary>
/// CODING RULES:
/// •	Local variables, private instance, static fields and method parameters should be camelCase.
/// •	Methods, constants, properties, events and classes should be PascalCase.
/// </summary>

namespace BanterBrain_Buddy
{
    public partial class BBB : Form
    {
        private readonly static string Version = "1.0.11";

        //set logger
        private static log4net.ILog _bBBlog;

        //PTT hotkey hook
        private IKeyboardMouseEvents m_GlobalHook;

        //used for PTT checking
        private bool _hotkeyCalled;
        // check if STT is finished yet
        private bool _sTTDone;
        //check if TTS is finished yet
        private bool _tTSSpeaking;

        [SupportedOSPlatform("windows10.0.10240")]
        //Hotkey Storage
        readonly private List<Keys> _setHotkeys = [];

        //check if the GPT LLM is donestop audio capture
        private bool _gPTDone;

        //error checker for async events. If true, stop execution of whatever you're doing
        private bool _bigError;

        private bool _twitchStarted;
        private bool _twitchAPIVerified;

        //Global Twitch API class
        //we need this for the hourly /validate check
        private TwitchAPIESub _globalTwitchAPI;
        private TwitchAPIESub _globalTwitchBotAPI;
        private bool _twitchValidateBroadcasterCheckStarted;
        private bool _twitchValidateBotCheckStarted;
        private TwitchAPIESub _twitchEventSub;
        private TwitchAPIESub _twitchChatUser;

        //this needs to be public because the settings form needs to know
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static bool isTwitchRunning;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        //this will hold the STT output text
        private string _sTTOutputText;
        private string _gPTOutputText;

        private ElLabs _elevenLabsApi;
        private AzureSpeechAPI _azureSpeech;
        private OllamaLLM _ollamaLLM;
        private OpenAI _openAI;
        private Groq _groqLLM;
        private readonly List<Personas> _personas = [];
        private TwitchLLMResponseLanguage TwitchLLMLanguage = new();


        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        bool ConvertToNewSettings = false;

        private List<TwitchNotableViewerClass> MyTwitchNotableViewers = [];

        [SupportedOSPlatform("windows10.0.10240")]
        public BBB()
        {
            _twitchValidateBroadcasterCheckStarted = false;
            _twitchValidateBotCheckStarted = false;
            InitializeComponent();

            //fire and forget, the task handles all its own exceptions internally
            _ = CheckForNewVersionAsync();
            SetLogger();

            ///update settings to the new format
            UpdateToNewSettingsFormat();
            while (!ConvertToNewSettings)
            {
                _bBBlog.Debug("Waiting for settings to be converted");
                Thread.Sleep(100);
            }

            //LoadSettings();
            _bBBlog.Info("BanterBrain Buddy version: " + Version);
            LoadLanguageStuff();
            SetupConfigFiles();
            //when no personas.json exists yet this only fills the boxes after an await, the
            //first-install path in SetSelectedSTTProvider awaits a reload to cover that
            _ = LoadPersonas();
            LoadTwitchNotableViewers();
            LoadTwitchLLMLanguageFile();
            _bBBlog.Info("Program Starting...");

            UpdateTextLog("Program Starting...\r\n");
            //TODO: verify API token first
            Startup();

        }

        //this will load the notable MyTwitchNotableViewers from the file and load it in the local variable
        [SupportedOSPlatform("windows10.0.10240")]
        private void LoadTwitchNotableViewers()
        {
            _bBBlog.Debug("Loading notable viewers");
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\viewers.json";
            //aight now load the MyTwitchNotableViewers from the file into TwitchNotableViewerClass
            MyTwitchNotableViewers = new();

            if (System.IO.File.Exists(path))
            {
                _bBBlog.Info("Twitch notable viewers file found, loading viewers from json file");
                string json = System.IO.File.ReadAllText(path);
                MyTwitchNotableViewers = JsonConvert.DeserializeObject<List<TwitchNotableViewerClass>>(json) ?? new List<TwitchNotableViewerClass>();
                _bBBlog.Debug("Viewers loaded from file: " + MyTwitchNotableViewers.Count);

                
                var tmpViewers = "";
                foreach (var viewer in MyTwitchNotableViewers)
                {
                    tmpViewers += viewer.ViewerName + ", ";
                }
                tmpViewers = tmpViewers.TrimEnd(',', ' ');
                UpdateTextLog($"Notable Twitch viewers loaded from file: {tmpViewers}\r\n");
            }
            else
            {
                //we create the file if it doesn't exist
                _bBBlog.Info("Twitch notable viewers file not found, creating new file");
                System.IO.File.Create(path).Dispose();
            }
        }

        private const string GitHubApiUrl = "https://api.github.com/repos/{owner}/{repo}/releases/latest";

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task CheckForNewVersionAsync()
        {

            TextLog.AppendText("Checking version...\r\n");

            using (var client = new HttpClient())
            {
                // GitHub API requires a user-agent
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                client.Timeout = TimeSpan.FromSeconds(5);
                try
                {
                    string url = GitHubApiUrl.Replace("{owner}", "WhiskerWeirdo").Replace("{repo}", "BanterBrain-Buddy");
                    var response = await client.GetStringAsync(url);
                    var tmpLatestVersion = JObject.Parse(response)["tag_name"].ToString();

                    //convert latest version minus the _release tag
                    string latestVersion = tmpLatestVersion.Split('_')[0];



                    // Assuming your current version is stored in a similar tag format
                    string currentVersion = Version; // This should be dynamically obtained from your application
                    // Parse the numeric parts into Version objects
                    Version version1 = new(currentVersion);
                    Version version2 = new(latestVersion);

                    if (version1 < version2)
                    {
                        _bBBlog.Info($"NOTE: A new version is available: {latestVersion}. You are currently on {currentVersion}.");
                        TextLog.AppendText($"*NOTE:* A new version is available: {latestVersion}. You are currently on {currentVersion}.\r\n");
                        VersionUpdateLabel.Text = $"Update available!";
                        VersionUpdateLabel.BackColor = Color.Orange;
                        // Here you can add logic to prompt the user to download the new version
                        TwitchMockCheckBox.Visible = false;
                    }
                    else if (version2 < version1)
                    {
                        _bBBlog.Info($"You run a newer version than the newest published version! Remote: {latestVersion}. You are currently on {currentVersion}.");
                        TextLog.AppendText($"You run a newer version than the newest published version! Remote: {latestVersion}. You are currently on {currentVersion}.\r\n");
                        VersionUpdateLabel.Text = $"Hi Dev!";
                        VersionUpdateLabel.BackColor = Color.Yellow;
                        // we need to hide this option from non devs
                        TwitchMockCheckBox.Visible = true;

                    }
                    else if (version1 == version2)
                    {
                        _bBBlog.Debug("You are on the latest version.");
                        TextLog.AppendText("You are on the latest version.\r\n");
                        VersionUpdateLabel.Text = "No update";
                        VersionUpdateLabel.BackColor = Color.Green;
                        TwitchMockCheckBox.Visible = false;
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _bBBlog.Error($"Timeout checking for application update: {ex.Message}");
                    TextLog.AppendText($"Timeout checking for application update: {ex.Message}\r\n");
                }
                catch (Exception ex)
                {
                    _bBBlog.Error($"Error checking for application update: {ex.Message}");
                    TextLog.AppendText($"Error checking for application update: {ex.Message}\r\n");
                }
            }
        }



        [SupportedOSPlatform("windows10.0.10240")]
        private void SetLogger()
        {

            TextLog.AppendText("Checking logs file" + "\r\n");
            //can we write to the logfile folder? which in appdata
            string logDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\logs";
            string logFile = "BanterBrainBuddy.log";

            if (!Directory.Exists(logDir))
            {
                TextLog.AppendText("Creating log directory: " + logDir + "\r\n");
                //if this cannot be created, we need to throw an error!
                try
                {
                    Directory.CreateDirectory(logDir);
                }
                catch (Exception)
                {
                    TextLog.AppendText("Error creating log directory in " + logDir + ". This should not happen.\r\n");
                }

            }
            else
            {
                TextLog.AppendText("Log directory found: " + logDir + "\r\n");
            }

            //if file doesnt exit, try create it, if that fails
            //change the logDir to the appdata folder
            if (!File.Exists(logDir + $"\\{logFile}"))
            {
                TextLog.AppendText($"Creating {logFile} in logs folder because it does not exist" + "\r\n");
                try
                {
                    File.Create(logDir + $"\\{logFile}").Close();
                }
                catch (IOException ex)
                {
                    TextLog.AppendText("IOException access Error creating log file: " + ex.Message + "\r\n");
                    TextLog.AppendText("Error creating log directory in " + logDir + "\r\n");

                }
                catch (Exception ex)
                {
                    TextLog.AppendText("Error creating log file: " + ex.Message + "\r\n");
                    TextLog.AppendText("Error creating log directory in " + logDir + "\r\n");
                }
            }
            else
            {
                TextLog.AppendText("Log file exists and should be in: " + logDir + "\r\n");
            }

            //can we write to the file?
            //again if not, we move to appdata
            try
            {
                using (StreamWriter sw = File.AppendText(logDir + $"\\{logFile}"))
                {
                    sw.WriteLine("Log file created at: " + DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                TextLog.AppendText("Error writing to log file: " + ex.Message + "\r\n");
                TextLog.AppendText("Error creating log directory in " + logDir + "\r\n");
            }

            //set logger file to where we have rights for writing
            log4net.Appender.DebugAppender consoleAppender = new()
            {
                Name = "ConsoleAppender",
                Layout = new log4net.Layout.PatternLayout("%date [%thread] %-5level %logger - %message%newline")
            };
            consoleAppender.ActivateOptions();

            log4net.Appender.FileAppender fileAppender = new()
            {
                Name = "FileAppender",
                File = System.IO.Path.Combine(logDir, logFile),
                AppendToFile = true,
                Layout = new log4net.Layout.PatternLayout("%date [%thread] %-5level %logger - %message%newline")
            };
            fileAppender.ActivateOptions();

            log4net.Repository.Hierarchy.Hierarchy hierarchy =
                (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            hierarchy.Root.AddAppender(consoleAppender);
            hierarchy.Root.AddAppender(fileAppender);
            hierarchy.Configured = true;
            _bBBlog = log4net.LogManager.GetLogger(typeof(BBB));
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void LoadLanguageStuff()
        {
            CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
            _bBBlog.Debug("Current UI Culture: " + currentUICulture);
            UpdateTextLog("Current UI Language: " + currentUICulture + "\r\n");
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private async void Startup()
        {

            //loading the settings, this might take a bit
            _bBBlog.Info("Loading settings...");
            UpdateTextLog("Loading settings...Please wait\r\n");
            BBBTabs.Enabled = false;
            menuStrip1.Enabled = false;
            if (TwitchEnableCheckbox.Checked && UserSettingsManager.Settings.TwitchAccessToken.Length > 1)
                SetTwitchValidateBroadcasterTokenTimer(true);

            await CheckConfiguredSTTProviders();
            await CheckConfiguredLLMProviders();
            await LoadSettings();
            await SetSelectedSTTProvider();
            SetSelectedLLMProvider();
            Subscribe();
            UpdateTextLog("Settings loaded.\r\n\r\n");
            BBBTabs.Enabled = true;
            menuStrip1.Enabled = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void SetSelectedLLMProvider()
        {
            _bBBlog.Debug("Setting selected LLM provider");
            _bBBlog.Debug("LLM count: " + LLMResponseSelecter.Items.Count);
            if (UserSettingsManager.Settings.SelectedLLM.Length > 1)
            {
                _bBBlog.Debug("Setting LLM to saved value: " + UserSettingsManager.Settings.SelectedLLM);
                LLMResponseSelecter.SelectedIndex = LLMResponseSelecter.FindStringExact(UserSettingsManager.Settings.SelectedLLM);
                if (LLMResponseSelecter.SelectedIndex == -1)
                {
                    _bBBlog.Error("Selected LLM not found in list, setting to first in list");
                    if (LLMResponseSelecter.Items.Count > 0)
                    {
                        _bBBlog.Debug("Setting LLM to first in list");
                        LLMResponseSelecter.SelectedIndex = 0;
                    }
                    else
                    {
                        MainRecordingStart.Enabled = false;
                        TwitchStartButton.Enabled = false;
                        LLMGroupSettingsGroupBox.Enabled = false;
                        _bBBlog.Error("No LLM's found. You need at least one. Please check your settings.");
                        UpdateTextLog("No LLM's found. You need at least one. Please check your settings.\r\n");
                        //  MessageBox.Show("No LLM's found. You need at least one. Please check your settings.", "LLM error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                UpdateTextLog("Selected LLM: " + LLMResponseSelecter.Text + "\r\n");
            }
            else
            {
                //ok then we just load the first one we can find in the list!
                if (LLMResponseSelecter.Items.Count > 0)
                {
                    _bBBlog.Debug("Setting LLM to first in list");
                    LLMResponseSelecter.SelectedIndex = 0;
                }
                else
                {
                    MainRecordingStart.Enabled = false;
                    TwitchStartButton.Enabled = false;
                    LLMGroupSettingsGroupBox.Enabled = false;
                    //MessageBox.Show("No LLM's found. You need at least one. Please check your settings.", "LLM error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        //here we check what LLM's are available and add them to the LLMResponseSelecter list 
        private async Task CheckConfiguredLLMProviders()
        {
            LLMResponseSelecter.Items.Clear();
            //check GPT
            if (UserSettingsManager.Settings.GPTAPIKey.Length > 1)
            {
                try
                {
                    _openAI ??= new();

                    if (await _openAI.OpenAICheckAPIKey())
                    {
                        LLMResponseSelecter.Items.Add("ChatGPT");
                        _bBBlog.Info("GPT API setting valid, adding to list");
                    }
                    else
                    {
                        _bBBlog.Error("GPT API setting invalid");
                    }
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Error checking GPT API key: " + ex.Message);
                }
            }
            //check Ollama  
            if (UserSettingsManager.Settings.OllamaURI.Length > 1 && UserSettingsManager.Settings.UseOllamaLLMCheckBox)
            {
                try
                {
                    _ollamaLLM ??= new(UserSettingsManager.Settings.OllamaURI);
                    if (await _ollamaLLM.OllamaVerify())
                    {
                        LLMResponseSelecter.Items.Add("Ollama");
                        _bBBlog.Info("Ollama setting valid, adding to list");
                        UpdateTextLog("Ollama API setting valid.\r\n");
                    }
                    else
                    {
                        _bBBlog.Error("Ollama API setting invalid or not running");
                    }

                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Error checking Ollama API key: " + ex.Message);
                }
            }
            //check Groq  
            if (!string.IsNullOrEmpty(UserSettingsManager.Settings.GroqAPIKey))
            {
                try
                {
                    _groqLLM ??= new();
                    if (await _groqLLM.GroqCheckAPIKey())
                    {
                        LLMResponseSelecter.Items.Add("Groq");
                        _bBBlog.Info("Groq API setting valid, adding to list");
                        UpdateTextLog("Groq API setting valid.\r\n");
                    }
                    else
                    {
                        _bBBlog.Error("Groq API setting invalid");
                    }

                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Error checking Groq API key: " + ex.Message);
                }
            }
        }


        //this is to make sure config files are writable, in the correct %APPDATA%\BanterBrain folder and can be read and write
        [SupportedOSPlatform("windows10.0.10240")]
        private static void SetupConfigFiles()
        {

            //check if the folder exists, if not, create it

            string appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain";
            if (!Directory.Exists(appdataFolder))
            {
                _bBBlog.Info("Creating BanterBrain folder in %APPDATA% because it does not exist");
                Directory.CreateDirectory(appdataFolder);
            }

            //copy from install folder to %APPDATA%\BanterBrain
            string sourcefolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //we always copy the settings.json file to appdata because if the Client ID changes we need to be able to overwrite it.
            //Guard against a missing source file so the app can still start (e.g. fresh installs that only ship the template).
            string tmpFile = appdataFolder + "\\settings.json";
            if (File.Exists(sourcefolder + "\\settings.json"))
            {
                _bBBlog.Debug("Copying settings.json file to appdata");
                File.Copy(sourcefolder + "\\settings.json", tmpFile, true);
            }
            else
            {
                _bBBlog.Warn("settings.json not found in install folder. Falling back to the settings.json.example template.");

                //First-run fallback: seed %APPDATA%\BanterBrain\settings.json from the shipped template so the
                //Settings UI has something to edit instead of the app crashing on startup.
                if (!File.Exists(tmpFile) && File.Exists(sourcefolder + "\\settings.json.example"))
                {
                    _bBBlog.Info("Copying settings.json.example template to appdata as settings.json");
                    File.Copy(sourcefolder + "\\settings.json.example", tmpFile);
                }
                else if (!File.Exists(tmpFile))
                {
                    //Nothing shipped and nothing in appdata yet: create a default settings.json on first run
                    //so the app starts cleanly and the Settings UI has a file to populate the Client ID into.
                    _bBBlog.Warn("No settings.json or settings.json.example available. Creating a default settings.json in appdata.");
                    File.WriteAllText(tmpFile, "{\n  \"TwitchAuthClientId\": \"\"\n}\n");
                }
            }

            //check if the personas file exists, if not, create it
            tmpFile = appdataFolder + "\\personas.json";
            if (!File.Exists(tmpFile) && File.Exists(sourcefolder + "\\personas.json"))
            {
                _bBBlog.Debug("Copying personas.json file appdata");
                File.Copy(sourcefolder + "\\personas.json", tmpFile);
            }
            else
            {
                _bBBlog.Debug("Personas file not found in install folder. It will be created later.");
            }

            //lets check the existence of the bad word filter file and create it if it doesn't exist.
            tmpFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\WordFilter.txt";
            if (!File.Exists(tmpFile))
            {
                _bBBlog.Error($"Word Filter file not found, creating it");
                File.WriteAllText(tmpFile, "fucker,motherfucker,asshole,nigger,nigga,loser,retard,moron,cunt,slut,fag,whore");
            }
            else
            {
                _bBBlog.Debug("Bad word filter file found.");
            }
        }

        //we need to do this so we fill the default saved value only after the API voices are checked
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task SetSelectedSTTProvider()
        {
            //TODO: check if the selected provider is still valid
            await Task.Delay(1000);
            try
            {
                if (UserSettingsManager.Settings.STTSelectedProvider.Length < 1)
                {
                    _bBBlog.Debug("Setting STT provider to default");
                    STTSelectedComboBox.SelectedIndex = 0;
                    //Oh, we in first install we should reload personas then too just to make sure the mainscreen persona dropdown is filled
                    await LoadPersonas();
                    if (BroadcasterSelectedPersonaComboBox.SelectedIndex == -1)
                        BroadcasterSelectedPersonaComboBox.SelectedIndex = 0;
                }
                else
                    STTSelectedComboBox.SelectedIndex = STTSelectedComboBox.FindStringExact(UserSettingsManager.Settings.STTSelectedProvider);
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Error setting STT provider: " + ex.Message);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private Personas GetSelectedPersona(string personaName)
        {
            // Personas tmpPersona = new();
            foreach (var persona in _personas)
            {
                if (persona.Name == personaName)
                {
                    _bBBlog.Debug("Setting selected persona: " + persona.Name);
                    //_broadcasterSelectedPersona = persona;
                    return persona;
                }
            }
            //no match (renamed/deleted persona or stale saved name): fall back to the first available instead of null
            //so callers that dereference the result inside async void handlers do not crash
            if (_personas.Count > 0)
            {
                _bBBlog.Warn($"Selected persona '{personaName}' not found, falling back to '{_personas[0].Name}'");
                return _personas[0];
            }
            _bBBlog.Error($"Selected persona '{personaName}' not found and no personas are loaded");
            return null;
        }

        //this loads the personas from the personas.json file
        //if one doesn't exist, it creates a default one
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task LoadPersonas()
        {

            var tmpFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\personas.json";
            if (!File.Exists(tmpFile))
            {
                _bBBlog.Debug("Personas file not found, creating it");
                //there might be a native voice installed, if so we should add it to the list
                _ = new                //there might be a native voice installed, if so we should add it to the list
                NativeSpeech();
                var nativeRegionVoicesList = await NativeSpeech.TTSNativeGetVoices();
                string tmpNativeVoice = null;
                if (nativeRegionVoicesList.Count > 0)
                {
                    _bBBlog.Debug("Native voices found, adding them to the list");

                    foreach (var voice in nativeRegionVoicesList)
                    {
                        _bBBlog.Debug("Adding voice: " + voice);
                        tmpNativeVoice = voice.Name + "-" + voice.Gender + "-" + voice.Culture;
                        break;
                    }
                }
                else
                {
                    _bBBlog.Debug("No native voices found");
                    tmpNativeVoice = "None";
                }
                var newPersonas = new List<Personas>
                {
                    new() { Name = "Default", RoleText = "You are a cheeky streamer assistant with a silly personality.", VoiceProvider = "Native", VoiceName = tmpNativeVoice, VoiceOptions = [] }
                };
                string tmpString = JsonConvert.SerializeObject(newPersonas);
                using var sw = new StreamWriter(tmpFile, true);
                sw.Write(tmpString);
            }
            //load (also right after creating the file) so the combos are filled in both cases
            _bBBlog.Debug("Loading personas into the main screen comboboxes.");
            _personas.Clear();
            BroadcasterSelectedPersonaComboBox.Items.Clear();
            TwitchChatPersonaComboBox.Items.Clear();
            TwitchChannelPointPersonaComboBox.Items.Clear();
            TwitchCheeringPersonaComboBox.Items.Clear();
            TwitchSubscriptionPersonaComboBox.Items.Clear();
            using var sr = new StreamReader(tmpFile);
            var tmpString2 = sr.ReadToEnd();
            //var tmpPersonas = JsonConvert.DeserializeObject<List<Personas>>(tmpString);
            var tmpPersonas = JsonConvert.DeserializeObject<List<Personas>>(tmpString2) ?? new List<Personas>();
            foreach (var persona in tmpPersonas)
            {
                _bBBlog.Debug("Loading persona into available list: " + persona.Name);
                _personas.Add(persona);
                BroadcasterSelectedPersonaComboBox.Items.Add(persona.Name);
                TwitchChatPersonaComboBox.Items.Add(persona.Name);
                TwitchChannelPointPersonaComboBox.Items.Add(persona.Name);
                TwitchCheeringPersonaComboBox.Items.Add(persona.Name);
                TwitchSubscriptionPersonaComboBox.Items.Add(persona.Name);

            }
        }

        //here we load the language file for the Twitch LLM responses that are intermediary
        [SupportedOSPlatform("windows10.0.10240")]
        private void LoadTwitchLLMLanguageFile()
        {
            try
            {
                string sourcefolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string tmpFile;


                if (UserSettingsManager.Settings.TwitchLLMLanguageComboBox != "Custom")
                    tmpFile = sourcefolder + $"\\TwitchLLMLanguageFiles\\{UserSettingsManager.Settings.TwitchLLMLanguageComboBox}.json";
                else
                {
                    //we load from appdata, create it if it does not exist from English
                    tmpFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\CustomTwitchLLMLanguage.json";
                    if (!File.Exists(tmpFile))
                    {
                        _bBBlog.Debug("Twitch LLM custom language file not found, creating it from English");
                        var englishTemplate = sourcefolder + "\\TwitchLLMLanguageFiles\\English.json";
                        if (File.Exists(englishTemplate))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(tmpFile));
                            File.Copy(englishTemplate, tmpFile);
                        }
                    }
                }
                //a missing or renamed language file must not leave the handlers without any text
                if (!File.Exists(tmpFile))
                {
                    _bBBlog.Error($"Twitch LLM language {UserSettingsManager.Settings.TwitchLLMLanguageComboBox} file not found, falling back to English!");
                    UpdateTextLog($"Twitch LLM language {UserSettingsManager.Settings.TwitchLLMLanguageComboBox} file not found, falling back to English!\r\n");
                    tmpFile = sourcefolder + "\\TwitchLLMLanguageFiles\\English.json";
                }
                if (!File.Exists(tmpFile))
                {
                    _bBBlog.Error("Twitch LLM English language file not found either, using empty defaults!");
                    UpdateTextLog("Twitch LLM English language file not found, using empty defaults!\r\n");
                    return;
                }

                _bBBlog.Debug($"Twitch LLM language file found, loading it: {tmpFile}");
                using var sr = new StreamReader(tmpFile);
                var tmpString = sr.ReadToEnd();
                //keep the current (empty default) language object when the file cannot be parsed
                var tmpLanguage = JsonConvert.DeserializeObject<TwitchLLMResponseLanguage>(tmpString);
                if (tmpLanguage != null)
                    TwitchLLMLanguage = tmpLanguage;
                else
                    _bBBlog.Error("Twitch LLM language file parsed to null, keeping the previous language");
                _bBBlog.Info($"Twitch LLM language file loaded with language: {TwitchLLMLanguage.Language}");
            }
            catch (Exception ex)
            {
                //called from the constructor, a broken language file may never take down startup
                _bBBlog.Error("Error loading Twitch LLM language file: " + ex.Message);
            }
        }



        /// <summary>
        /// We check the available STT providers and add them to the list for the broadcater selection list
        /// </summary>
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task CheckConfiguredSTTProviders()
        {

            //is there already one from loadsettings?
            var tmpProvider = STTSelectedComboBox.Text;
            if (tmpProvider.Length > 0)
            {
                _bBBlog.Debug("STT provider already set: " + tmpProvider);

            }
            else
            {
                //Check if theres a Windows Native voice recognizer installed
                if (SpeechRecognitionEngine.InstalledRecognizers().Count > 0)
                {
                    STTSelectedComboBox.Items.Add("Native");
                    _bBBlog.Debug("Found at least one Windows Native speech recognizer.");
                    UpdateTextLog("Found at least one Windows Native speech recognizer.\r\n");
                }
                else
                {
                    _bBBlog.Debug("No Windows Native speech recognizer found");
                    UpdateTextLog("No Windows Native speech recognizer found. Please install one. Check here: https://github.com/WhiskerWeirdo/BanterBrain-Buddy/wiki/How-do-I#install-windows-native-speech-recognition\r\n");
                }
            }
            //if the Azure API key is set, we verify if the key can be used to synthesize voice
            if (UserSettingsManager.Settings.AzureAPIKeyTextBox.Length > 1)
            {
                //holds the result of the API ShowSettingsForm
                bool APIResult = false;
                _bBBlog.Info("Checking Azure API key");
                UpdateTextLog("Checking Azure API key\r\n");
                _azureSpeech ??= new(UserSettingsManager.Settings.AzureAPIKeyTextBox, UserSettingsManager.Settings.AzureRegionTextBox, UserSettingsManager.Settings.AzureLanguageComboBox);

                try
                {
                    APIResult = await _azureSpeech.AzureVerifyAPI();
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Error checking Azure API: " + ex.Message);
                    UpdateTextLog("Error checking Azure API configuration. See logfile for details\r\n");
                }
                if (!APIResult)
                {
                    _bBBlog.Error("No valid Azure API key found. Value cleared, please fix in settings");
                    UpdateTextLog("No valid Azure API key found. Value cleared, please fix in settings\r\n");
                    UserSettingsManager.Settings.AzureAPIKeyTextBox = "";
                    UserSettingsManager.SaveSettings();
                    MessageBox.Show("No valid Azure API key found. Value cleared, please fix in settings", "Azure TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    STTSelectedComboBox.Items.Add("Azure");
                    _bBBlog.Info("Azure API setting valid");
                    UpdateTextLog("Azure API setting valid.\r\n");
                }
            }
            //ShowSettingsForm if teh API key for ElevenLabs is set and valid
            if (UserSettingsManager.Settings.ElevenLabsAPIkey.Length > 1)
            {
                //call ShowSettingsForm api key for elevenlabs
                _elevenLabsApi ??= new(UserSettingsManager.Settings.ElevenLabsAPIkey);
                if (await _elevenLabsApi.ElevenLabsAPIKeyTest())
                {
                    UpdateTextLog("ElevenLabs API key is valid\r\n");
                    _bBBlog.Info("ElevenLabs API key is valid");
                }
                else
                {
                    _bBBlog.Error("ElevenLabs API key is invalid.Value cleared, please fix in settings.");
                    UpdateTextLog("ElevenLabs API key is invalid. Value cleared, please fix in settings.\r\n");
                    UserSettingsManager.Settings.ElevenLabsAPIkey = "";
                    UserSettingsManager.SaveSettings(); 
                    MessageBox.Show("ElevenLabs API key is invalid. Value cleared, please fix in settings.", "ElevenLabs TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (UserSettingsManager.Settings.GPTAPIKey.Length > 1)
            {
                // we need to check if the OpenAI API key is valid
                _openAI ??= new();
                if (await _openAI.OpenAICheckAPIKey())
                {
                    STTSelectedComboBox.Items.Add("OpenAI Whisper");
                    _bBBlog.Info("OpenAI API setting valid, adding Whisper to list");
                    UpdateTextLog("OpenAI API setting valid\r\n");
                }
                else
                {
                    _bBBlog.Error("OpenAI API setting invalid! Value cleared, please fix in settings.");
                    UpdateTextLog("OpenAI API setting invalid!Value cleared, please fix in settings. \r\n");
                    UserSettingsManager.Settings.GPTAPIKey = "";
                    UserSettingsManager.SaveSettings(); 
                     MessageBox.Show("OpenAI API setting invalid! Value cleared, please fix in settings.", "OpenAI Whisper error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Twitch requires you to validate your access token every hour. This starts this timer when Twitch is enabled.
        /// </summary>
        [SupportedOSPlatform("windows10.0.10240")]
        public async void SetTwitchValidateBotTokenTimer()
        {
            if (!_twitchValidateBotCheckStarted && TwitchEnableCheckbox.Checked && UserSettingsManager.Settings.TwitchBotAuthKey.Length > 0 && UserSettingsManager.Settings.TwitchBotName.Length > 0)
            {
                //only make a new instance if it's null
                _globalTwitchBotAPI ??= new();

                var result = await _globalTwitchBotAPI.ValidateAccessToken(UserSettingsManager.Settings.TwitchBotAuthKey);
                if (!result)
                {
                    _bBBlog.Error("Twitch access bot token is invalid, please re-authenticate");
                    MessageBox.Show("Twitch access bot token is invalid, please re-authenticate", "Twitch Auth error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateTextLog("Twitch access bot token is invalid, please re-authenticate\r\n");
                    _twitchValidateBotCheckStarted = false;
                    _bigError = true;
                }
                else
                {
                    _bBBlog.Info("Twitch access bot token is valid. Starting automated /validate call");
                    UpdateTextLog("Twitch access bot token is valid. Starting automated /validate call\r\n");
                    _twitchValidateBotCheckStarted = true;
                    await _globalTwitchBotAPI.CheckHourlyAccessToken();
                }
            }
        }

        /// <summary>
        /// Twitch requires you to validate your access token every hour. This starts this timer when Twitch is enabled.
        /// </summary>
        [SupportedOSPlatform("windows10.0.10240")]
        /// <summary>
        public async void SetTwitchValidateBroadcasterTokenTimer(bool StartEventSubClient)
        {
            if (!_twitchValidateBroadcasterCheckStarted && TwitchEnableCheckbox.Checked && UserSettingsManager.Settings.TwitchAccessToken.Length > 0 && UserSettingsManager.Settings.TwitchChannel.Length > 0)
            {
                //only make a new instance if it's null
                _globalTwitchAPI ??= new();

                var result = await _globalTwitchAPI.ValidateAccessToken(UserSettingsManager.Settings.TwitchAccessToken);
                if (!result)
                {
                    _bBBlog.Error("Twitch access broadcaster token is invalid, please re-authenticate");
                    MessageBox.Show("Twitch access broadcaster token is invalid, please re-authenticate", "Twitch Auth error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateTextLog("Twitch access broadcaster token is invalid, please re-authenticate\r\n");
                    _twitchValidateBroadcasterCheckStarted = false;
                    TwitchAPIStatusTextBox.Text = "DISABLED";
                    TwitchAPIStatusTextBox.BackColor = Color.Red;
                    _bigError = true;
                }
                else
                {
                    _bBBlog.Info("Twitch access broadcaster token is valid. Starting automated /validate call");
                    UpdateTextLog("Twitch access broadcaster token is valid. Starting automated /validate call\r\n");
                    _twitchValidateBroadcasterCheckStarted = true;
                    TwitchAPIStatusTextBox.Text = "ENABLED";
                    TwitchAPIStatusTextBox.BackColor = Color.Green;

                    //only start if StartEventSubClient = true
                    //ok so all is good, lets start the eventsub client
                    if (StartEventSubClient)
                    {
                        _bBBlog.Info("Starting EventSub client");
                        await EventSubStartWebsocketClient();
                        //if we are good, start the hourly check
                        await _globalTwitchAPI.CheckHourlyAccessToken();
                    }
                    else if (!StartEventSubClient)
                    {
                        _bBBlog.Info("We are in Test. EventSub client not started");
                        await _globalTwitchAPI.CheckHourlyAccessToken();
                    }
                    else
                    {
                        _bBBlog.Error("Error starting EventSub client");
                        MessageBox.Show("Error starting EventSub client", "Twitch EventSub error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        UpdateTextLog("Error starting EventSub client\r\n");
                        _bigError = true;
                    }
                }
            }
        }


        [SupportedOSPlatform("windows10.0.10240")]
        /// <summary>
        /// This uses the Azure Cognitive Services Speech SDK to convert voice to text.
        /// </summary>
        private async void AzureConvertVoicetoText()
        {
            _sTTOutputText = "";
            _azureSpeech ??= new(UserSettingsManager.Settings.AzureAPIKeyTextBox, UserSettingsManager.Settings.AzureRegionTextBox, UserSettingsManager.Settings.AzureLanguageComboBox);
            //call the Azure STT function with the selected input device
            //first initialize the Azure STT class
            _azureSpeech.AzureSTTInit(UserSettingsManager.Settings.VoiceInput);
            _bBBlog.Info("Azure STT microphone start. Language: " + UserSettingsManager.Settings.AzureLanguageComboBox);
            while (MainRecordingStart.Text == "Recording" && !_sTTDone && !_bigError)
            {
                var recognizeResult = await _azureSpeech.RecognizeSpeechAsync();
                if (recognizeResult == "NOMATCH")
                {
                    UpdateTextLog("Azure Speech-To-Text: NOMATCH: Speech could not be recognized.\r\n");
                    //_sTTOutputText += "NOMATCH: Speech could not be recognized.\r\n";
                    _bigError = true;
                    _sTTDone = true;
                }
                else if (recognizeResult == null)
                {
                    _sTTOutputText += "Fail! Speech could not be proccessed. Check log for more info.\r\n";
                    UpdateTextLog("Azure Speech-To-Text: Fail! Speech could not be proccessed. Check log for more info.\r\n");
                    _bigError = true;
                    _sTTDone = true;
                }
                else
                {
                    _sTTOutputText += recognizeResult + "\r\n";
                    TextLog.Text += "Azure Speech-To-Text: " + recognizeResult + "\r\n";
                }
            }
            _sTTDone = true;
        }



        [SupportedOSPlatform("windows10.0.10240")]
        private async Task InputStreamtoWav()
        {
            _bBBlog.Info("STT microphone start.");
            _bBBlog.Debug("Selected audio input device for Audio to Wav: " + UserSettingsManager.Settings.VoiceInput);

            string tmpWavFile = System.IO.Path.GetTempPath() + $"{Guid.NewGuid()}.wav";

            var recordingDevice = 0;
            for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                var tmpEnum = NAudio.Wave.WaveIn.GetCapabilities(i).ProductName;
                _bBBlog.Debug("Checking recording device: " + tmpEnum + "at id: " + i);
                if (tmpEnum.StartsWith(UserSettingsManager.Settings.VoiceInput))
                {
                    recordingDevice = i;
                    break;
                }
            }
            _bBBlog.Debug("Recording device: " + recordingDevice);

            bool writeDone = false;
            var waveIn = new WaveInEvent
            {
                DeviceNumber = recordingDevice,
                WaveFormat = new NAudio.Wave.WaveFormat(16000, 16, 1)
            };
            var writer = new WaveFileWriter(tmpWavFile, waveIn.WaveFormat);
            waveIn.StartRecording();
            waveIn.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);

            };
            waveIn.RecordingStopped += (s, a) =>
            {
                _bBBlog.Debug("Recording stopped. Disposing writer");
                writer?.Dispose();
                writer = null;
                writeDone = true;
            };
            UpdateTextLog("STT microphone start. -- SPEAK NOW -- \r\n");
            _bBBlog.Info("STT microphone start.");
            while (MainRecordingStart.Text == "Recording")
            {
                await Task.Delay(500);
            }
            _bBBlog.Info("STT microphone stop.");
            waveIn.StopRecording();
            //we have to wait till the file is done writing  to disk
            while (!writeDone)
            {
                await Task.Delay(500);
            }
            //now we have the .wav file, lets convert it to text
            //but lets make sure its actually a decent size?
            _bBBlog.Debug("converting to Text from WAV");
            _sTTDone = false;

            //now lets convert the saved .wav to Text
            if (STTSelectedComboBox.Text == "Native")
                NativeSTTfromWAV(tmpWavFile);
            if (STTSelectedComboBox.Text == "OpenAI Whisper")
                WhisperSTTfromWAV(tmpWavFile);
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private async void WhisperSTTfromWAV(string tmpWavFile)
        {
            _sTTOutputText = "";
            _openAI ??= new();
            _bBBlog.Info("Starting OpenAI STT from WAV");
            try
            {
                _sTTOutputText = await _openAI.OpenAISTT(tmpWavFile);
                if (_sTTOutputText == null)
                {
                    _bBBlog.Error("OpenAI STT failed");
                    _bigError = true;
                }
                else
                {
                    _bBBlog.Info($"OpenAI STT done: {_sTTOutputText}");
                }
            }
            catch (Exception ex)
            {
                //async void, nothing may escape. Flag the error so the waiting caller stops
                _bBBlog.Error("OpenAI STT threw: " + ex.Message);
                _bigError = true;
            }
            finally
            {
                _sTTDone = true;
                _bBBlog.Debug($"Deleting temp wav file: {tmpWavFile}");
                try
                {
                    File.Delete(tmpWavFile);
                }
                catch (Exception ex)
                {
                    _bBBlog.Warn("Could not delete temp wav file: " + ex.Message);
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void NativeSTTfromWAV(string tmpWavFile)
        {
            _sTTDone = false;
            _sTTOutputText = "";
            NativeSpeech nativeSpeech = new();
            _bBBlog.Info("Starting Native STT from WAV");
            try
            {
                _sTTOutputText = await nativeSpeech.NativeSpeechRecognizeStart(tmpWavFile);
                if (_sTTOutputText == null)
                {
                    _bBBlog.Error("Native STT failed");
                    _bigError = true;
                }
                else
                {
                    _bBBlog.Info($"Native STT done: {_sTTOutputText}");
                }
            }
            catch (Exception ex)
            {
                //async void, nothing may escape. Flag the error so the waiting caller stops
                _bBBlog.Error("Native STT threw: " + ex.Message);
                _bigError = true;
            }
            finally
            {
                _sTTDone = true;
                //now delete wav file
                _bBBlog.Debug($"Deleting temp wav file: {tmpWavFile}");
                try
                {
                    File.Delete(tmpWavFile);
                }
                catch (Exception ex)
                {
                    _bBBlog.Warn("Could not delete temp wav file: " + ex.Message);
                }
            }
        }

        //play a wav file to the bots selected audio channel
        [SupportedOSPlatform("windows10.0.10240")]
        private static async Task PlayWaveFile(string tmpWavFile)
        {
            _bBBlog.Debug($"Playing wav file: {tmpWavFile}");
            NativeSpeech nativeSpeech = new();
            //get current working directory
            await nativeSpeech.NativePlayWaveFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\sounds\\" + tmpWavFile);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TalkToOllama(String UserInput, string tmpPersonaRoletext)
        {
            _gPTOutputText = "";
            UpdateTextLog("Sending to Ollama: " + UserInput + "\r\n");
            _ollamaLLM ??= new(UserSettingsManager.Settings.OllamaURI);

            var result = await _ollamaLLM.OllamaGetResponse(UserInput, tmpPersonaRoletext);
            if (result == null)
            {
                _bBBlog.Error("Ollama API error. Is there a problem with your API key or subscription?");
                UpdateTextLog("Ollama returned no answer. Check the LLM settings (thinking off, context tokens high enough?).\r\n");
                MessageBox.Show("Ollama API error. Is there a problem with your API key or subscription?", "Ollama API error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bigError = true;
                //make sure waiters on _gPTDone do not spin forever on a failed call
                _gPTDone = true;
            }
            else
            {
                UpdateTextLog("Ollama Response: " + result + "\r\n");
                _gPTOutputText = result;
                _gPTDone = true;
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TalkToOpenAIGPT(String UserInput, string tmpPersonaRoletext)
        {
            _gPTOutputText = "";
            UpdateTextLog("Sending to GPT: " + UserInput + "\r\n");
            _bBBlog.Info("Sending to GPT: " + UserInput);
            _gPTDone = false;

            _openAI ??= new();
            var result = await _openAI.GetOpenAIIGPTResponse(UserInput, tmpPersonaRoletext);
            if (result == null)
            {
                //the wrapper knows what failed, its reason beats guessing at a key problem here
                string reason = _openAI.LastResponseError;
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "GPT returned no answer. Check the Max Tokens setting or the reasoning effort in Settings.";
                }
                _bBBlog.Error("GPT chat failed: " + reason);
                UpdateTextLog("GPT returned no answer: " + reason + "\r\n");
                MessageBox.Show(reason, "GPT API error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bigError = true;
                //make sure waiters on _gPTDone do not spin forever on a failed call
                _gPTDone = true;
            }
            else
            {
                UpdateTextLog("GPT Response: " + result + "\r\n");
                _gPTOutputText = result;
                _gPTDone = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TalkToGroq(String UserInput, string tmpPersonaRoletext)
        {
            _gPTOutputText = "";
            UpdateTextLog("Sending to Groq: " + UserInput + "\r\n");
            _bBBlog.Info("Sending to Groq: " + UserInput);
            _gPTDone = false;

            _groqLLM ??= new();
            var result = await _groqLLM.GetGroqResponse(UserInput, tmpPersonaRoletext);
            if (result == null)
            {
                //the wrapper knows what failed, its reason beats guessing at a key problem here
                string reason = _groqLLM.LastResponseError;
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "Groq returned no answer. Check the Max Tokens setting or the reasoning effort in Settings.";
                }
                _bBBlog.Error("Groq chat failed: " + reason);
                UpdateTextLog("Groq returned no answer: " + reason + "\r\n");
                MessageBox.Show(reason, "Groq API error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bigError = true;
                //make sure waiters on _gPTDone do not spin forever on a failed call
                _gPTDone = true;
            }
            else
            {
                UpdateTextLog("Groq Response: " + result + "\r\n");
                _gPTOutputText = result;
                _gPTDone = true;
            }
        }

        /// <summary>
        /// Holds the list of Azure Voices and their options
        /// </summary>


        [SupportedOSPlatform("windows10.0.10240")]
        //Azure Text-To-Speach
        private async Task TTSAzureSpeakToOutput(string TextToSpeak, Personas tmpPersona)
        {
            if (UserSettingsManager.Settings.AzureAPIKeyTextBox.Length < 1)
            {
                _bBBlog.Error("Azure TTS error. No API key found but this persona uses it. Please check your settings");
                UpdateTextLog("Azure TTS error. No API key found but this persona uses it. Please check your settings\r\n");
                MainRecordingStart.Enabled = true;
                return;
            }

            UpdateTextLog("Saying text with Azure TTS\r\n");
            _bBBlog.Info("Saying text with Azure TTS");
            _azureSpeech ??= new(UserSettingsManager.Settings.AzureAPIKeyTextBox, UserSettingsManager.Settings.AzureRegionTextBox, UserSettingsManager.Settings.AzureLanguageComboBox);

            //set the output voice, gender and locale, and the style
            //this now depends on the selected persona
            await _azureSpeech.AzureTTSInit(tmpPersona.VoiceName, GetVoiceOptionOrDefault(tmpPersona.VoiceOptions, 0, "Default"), UserSettingsManager.Settings.TTSAudioOutput, tmpPersona.Volume, tmpPersona.Rate, tmpPersona.Pitch);
            if (!await _azureSpeech.AzureVerifyAPI())
            {
                _bBBlog.Error("Azure TTS error. Is there a problem with your API key or subscription?");
                UpdateTextLog("Azure TTS error. Is there a problem with your API key or subscription?\r\n");
                _bigError = true;
                MainRecordingStart.Enabled = true;
                return;
            }

            var result = await _azureSpeech.AzureSpeak(TextToSpeak);
            if (!result)
            {
                _bBBlog.Error("Azure TTS error. Is there a problem with your API key or subscription?");
                MessageBox.Show("Azure TTS error. Is there a problem with your API key or subscription?", "Azure TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bigError = true;
            }
            _bBBlog.Info("Azure TTS done");
            UpdateTextLog("Azure TTS done\r\n");
            MainRecordingStart.Enabled = true;
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSNativeSpeakToOutput(String TTSText, Personas tmpPersona)
        {
            UpdateTextLog("Saying text with Native TTS\r\n");
            _bBBlog.Info("Saying text with Native TTS");
            NativeSpeech nativeSpeech = new();
            //this now depends on the selected persona
            await nativeSpeech.NativeTTSInit(tmpPersona.VoiceName, UserSettingsManager.Settings.TTSAudioOutput, tmpPersona.Volume, tmpPersona.Rate, tmpPersona.Pitch);
            await nativeSpeech.NativeSpeak(TTSText);
            UpdateTextLog("Native TTS done\r\n");
            _bBBlog.Info("Native TTS done");
            MainRecordingStart.Enabled = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //this is the generic caller for TTS function that makes sure the TTS is done before continuing
        //we make this public so we can call it to ShowSettingsForm
        private async Task SayText(string TextToSay, int DelayWhenDone, Personas tmpPersona)
        {
            while (_tTSSpeaking)
            {
                await Task.Delay(500);
            }
            await DoSayText(TextToSay, DelayWhenDone, tmpPersona);
        }
        //agnostic TTS function
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task DoSayText(string TextToSay, int DelayWhenDone, Personas tmpPersona)
        {
            //an empty response (e.g. a failed LLM call) must not be handed to the TTS engines
            if (string.IsNullOrWhiteSpace(TextToSay))
            {
                _bBBlog.Error("DoSayText called with empty text, skipping TTS");
                UpdateTextLog("Nothing to say, skipping TTS\r\n");
                _tTSSpeaking = false;
                MainRecordingStart.Enabled = true;
                return;
            }
            //guard against a null persona (e.g. no personas loaded) so we never dereference null in an async void handler
            if (tmpPersona == null)
            {
                _bBBlog.Error("DoSayText called with no persona, skipping TTS");
                UpdateTextLog("No persona available for TTS, skipping\r\n");
                _tTSSpeaking = false;
                MainRecordingStart.Enabled = true;
                return;
            }
            _bBBlog.Info("Saying text: " + TextToSay + " with " + tmpPersona.Name);
            //this depends on the selected TTS provider from the persona
            _tTSSpeaking = true;
            if (tmpPersona.VoiceProvider == "Native")
            {
                await TTSNativeSpeakToOutput(TextToSay, tmpPersona);
            }
            else if (tmpPersona.VoiceProvider == "Azure")
            {
                await TTSAzureSpeakToOutput(TextToSay, tmpPersona);
            }
            else if (tmpPersona.VoiceProvider == "OpenAI Whisper")
            {
                await TTSOpenAISpeakToOutput(TextToSay, tmpPersona);
            }
            else if (tmpPersona.VoiceProvider == "ElevenLabs")
            {
                await TTSElevenLabsSpeakToOutput(TextToSay, tmpPersona);
            }

            await Task.Delay(DelayWhenDone);
            _tTSSpeaking = false;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSElevenLabsSpeakToOutput(string TextToSay, Personas tmpPersona)
        {
            UpdateTextLog("Saying text with ElevenLabs TTS\r\n");
            _bBBlog.Info("Saying text with ElevenLabs TTS");

            if (UserSettingsManager.Settings.ElevenLabsAPIkey.Length < 1)
            {
                _bBBlog.Error("ElevenLabs TTS error. No API key found but this persona uses it. Please check your settings");
                UpdateTextLog("Elevenlans TTS error. No API key found but this persona uses it. Please check your settings\r\n");
                MainRecordingStart.Enabled = true;
                return;
            }

            if (_elevenLabsApi == null)
            {
                _elevenLabsApi = new(UserSettingsManager.Settings.ElevenLabsAPIkey);
                _ = await _elevenLabsApi.TTSGetElevenLabsVoices();
            }
            else
            {
                //we check if length is > 1 and if the key is different
                //if so we need to update the key
                if (UserSettingsManager.Settings.ElevenLabsAPIkey != _elevenLabsApi.ElevelLabsAPIKey)
                {
                    _elevenLabsApi.ElevelLabsAPIKey = UserSettingsManager.Settings.ElevenLabsAPIkey;
                }
            }

            //ShowSettingsForm the current key
            if (!await _elevenLabsApi.ElevenLabsAPIKeyTest())
            {
                _bBBlog.Error("ElevenLabs TTS error. Is there a problem with your API key or subscription?");
                MessageBox.Show("ElevenLabs TTS error. Is there a problem with your API key or subscription?", "ElevenLabs TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MainRecordingStart.Enabled = true;
                return;
            }
            var result = await _elevenLabsApi.ElevenLabsTTS(TextToSay, UserSettingsManager.Settings.TTSAudioOutput, tmpPersona.VoiceName, ParseVoiceOptionOrDefault(tmpPersona.VoiceOptions, 0, 0), ParseVoiceOptionOrDefault(tmpPersona.VoiceOptions, 1, 0), ParseVoiceOptionOrDefault(tmpPersona.VoiceOptions, 2, 0), tmpPersona.Volume);

            if (!result)
            {
                _bBBlog.Error("ElevenLabs TTS error. Is there a problem with your API key or subscription?");
                MessageBox.Show("ElevenLabs TTS error. Is there a problem with your API key or subscription?", "ElevenLabs TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            _bBBlog.Info("ElevenLabs TTS done");
            UpdateTextLog("ElevenLabs TTS done\r\n");
            MainRecordingStart.Enabled = true;

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSOpenAISpeakToOutput(string TextToSpeak, Personas tmpPersona)
        {
            if (UserSettingsManager.Settings.GPTAPIKey.Length < 1)
            {
                _bBBlog.Error("OpenAI TTS error. No API key found but this persona uses it. Please check your settings");
                UpdateTextLog("OpenAI TTS error. No API key found but this persona uses it. Please check your settings\r\n");
                MainRecordingStart.Enabled = true;
                return;
            }
            UpdateTextLog("Saying text with OpenAI TTS\r\n");
            _bBBlog.Info("Saying text with OpenAI TTS");
            _openAI ??= new();
            //ShowSettingsForm the available key
            if (!await _openAI.OpenAICheckAPIKey())
            {
                _bBBlog.Error("OpenAI TTS error. Is there a problem with your API key or subscription?");
                MessageBox.Show("OpenAI TTS error. Is there a problem with your API key or subscription?", "OpenAI TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MainRecordingStart.Enabled = true;
                return;
            }

            await _openAI.OpenAITTS(TextToSpeak, UserSettingsManager.Settings.TTSAudioOutput, tmpPersona.VoiceName, tmpPersona.Rate, tmpPersona.Volume);
            _bBBlog.Info("OpenAI TTS done");
            UpdateTextLog("OpenAI TTS done\r\n");
            MainRecordingStart.Enabled = true;
        }

        //talk to the various LLM's
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TalkToLLM(string TextToPass, string tmpPersonaRoleText)
        {
            _gPTOutputText = "";
            _gPTDone = false;
            if (UserSettingsManager.Settings.SelectedLLM == "ChatGPT")
            {
                UpdateTextLog("Using ChatGPT\r\n");
                _bBBlog.Info("Using ChatGPT");
                await TalkToOpenAIGPT(TextToPass, tmpPersonaRoleText);
            }
            else if (UserSettingsManager.Settings.SelectedLLM == "Ollama")
            {
                UpdateTextLog("Using Ollama\r\n");
                _bBBlog.Info("Using Ollama");
                await TalkToOllama(TextToPass, tmpPersonaRoleText);
            }
            else if (UserSettingsManager.Settings.SelectedLLM == "Groq")
            {
                UpdateTextLog("Using Groq\r\n");
                _bBBlog.Info("Using Groq");
                await TalkToGroq(TextToPass, tmpPersonaRoleText);
            }
            else
            {
                _bBBlog.Error("No LLM selected");
                LLMGroupSettingsGroupBox.Enabled = false;
                MessageBox.Show("No LLM selected. This is bad!", "LLM error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //without this the wait loop below never exits
                _gPTDone = true;
            }
            //lets wait for GPT to be done
            while (!_gPTDone)
            {
                await Task.Delay(500);
            }
        }

        //main function for recording text, passing it to STT, then to GPT, then to TTS
        [SupportedOSPlatform("windows10.0.10240")]
        private async void MainRecordingStart_Click(object sender, EventArgs e)
        {
            _bigError = false;

            //selected STT provider
            String selectedProvider = STTSelectedComboBox.Text;
            //first, lets call STT
            if (STTSelectedComboBox.Text.Length < 1)
            {
                UpdateTextLog("Error! No STT provider selected!\r\n");
                _bBBlog.Error("Error! No STT provider selected!");
                MainRecordingStart.Text = "Start";
                return;
            }
            _sTTDone = false;
            if (MainRecordingStart.Text == "Start")
            {
                _sTTOutputText = "";
                MainRecordingStart.Text = "Recording";
                MainRecordingStart.BackColor = Color.Red;
                if (selectedProvider == "Native")
                {
                    UpdateTextLog("Main button Native STT calling\r\n");
                    _bBBlog.Info("Main button Native STT calling");
                    await InputStreamtoWav();
                    while (!_sTTDone)
                    {
                        await Task.Delay(500);
                    }

                }
                else if (selectedProvider == "OpenAI Whisper")
                {

                    UpdateTextLog("OpenAI Whisper STT calling\r\n");
                    _bBBlog.Info("OpenAI Whisper STT calling");
                    await InputStreamtoWav();
                    while (!_sTTDone)
                    {
                        await Task.Delay(500);
                    }
                }

                else if (selectedProvider == "Azure")
                {
                    UpdateTextLog("Azure STT calling\r\n");
                    _bBBlog.Info("Azure STT calling");
                    //cant be empty

                    if ((UserSettingsManager.Settings.AzureAPIKeyTextBox.Length < 1) || (UserSettingsManager.Settings.AzureRegionTextBox.Length < 1))
                    {
                        UpdateTextLog("Error! API Key or region cannot be empty!\r\n");
                        _bBBlog.Error("Error! API Key or region cannot be empty!");
                        MainRecordingStart.Text = "Start";
                    }
                    else
                    {
                        AzureConvertVoicetoText();
                        while (!_sTTDone)
                        {
                            await Task.Delay(500);
                        }

                    }
                }

                //if _bigError is true, stop! something is very wrong.
                if (_bigError)
                {
                    UpdateTextLog("Theres an error, stopping execution!\r\n");
                    _bBBlog.Error("Theres an error, stopping execution");
                    MainRecordingStart.Text = "Start";
                    return;
                }

                MainRecordingStart.Enabled = false;
                Thread.Sleep(500);

                //now the STT text is in _sTTOutputText, lets pass that to ChatGPT

                if (_sTTOutputText.Length > 1)
                {
                    // in this case we need to add the streamer name if entered
                    if (UserSettingsManager.Settings.TwitchBotName.Length > 0)
                    {
                        _bBBlog.Debug("Adding streamer name to STT text");
                        _sTTOutputText = UserSettingsManager.Settings.StreamerNameTextBox + " says: " + _sTTOutputText;
                    }

                    //no personas loaded (broken personas.json) must not crash this async void handler
                    var broadcasterPersona = GetSelectedPersona(BroadcasterSelectedPersonaComboBox.Text);
                    if (broadcasterPersona == null)
                    {
                        _bBBlog.Error("No persona loaded, cannot converse. Check personas.json");
                        UpdateTextLog("Error! No persona is loaded, check the personas file.\r\n");
                        MainRecordingStart.Enabled = true;
                        return;
                    }

                    await TalkToLLM(_sTTOutputText, broadcasterPersona.RoleText);

                    //this depends on the selected persona now
                    await SayText(_gPTOutputText, 0, broadcasterPersona);

                }
                else
                {
                    UpdateTextLog("No audio recorded");
                    _bBBlog.Info("No audio recorded");
                    MainRecordingStart.Enabled = true;
                }
            }
            else
            {

                MainRecordingStart.Text = "Start";
                UpdateTextLog("STT microphone stopped. -- STOPPED RECORDING -- \r\n");
                MainRecordingStart.BackColor = Color.Transparent;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void UpdateToNewSettingsFormat()
        {
            //init the new settings class
            var settings = SettingsManager.Instance;
            //if we already have a settings file in the new format, just ignore this.
            _bBBlog.Debug("Check for new settings format file");
            if (settings.NewSettingsFileExists())
            {
                _bBBlog.Debug("New settings format file exists. No update needed");
                ConvertToNewSettings = true;
                return;
            }

            //First of all lets check of there are any userspace Properties.Default.Settings
            //if so we need to convert them to the new settings format
            // Get the type of the Settings class
            Type settingsType = typeof(Properties.Settings);

            // Get all properties of the Settings class
            PropertyInfo[] properties = settingsType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Length > 1)
            {
                _bBBlog.Debug("Found user-scoped settings, converting to new format");
                UpdateTextLog("Found old settings file, converting to new format.");
                foreach (PropertyInfo property in properties)
                {
                    // Check if the property is user-scoped
                    var userScopedAttribute = property.GetCustomAttribute<UserScopedSettingAttribute>();
                    if (userScopedAttribute != null)
                    {
                        // Get the value of the property
                        object value = property.GetValue(Properties.Settings.Default);
                       // _bBBlog.Debug($"Property: {property.Name}, Value: {value}");
                        //and assign it to the new settings format
                        settings.SetValue(property.Name, value);
                    }
                }
                _bBBlog.Debug("New settings format has values");
                settings.SaveSettings();

                //do we need to make people do a manual check? Maybe open the settings file to verify its there?
                //or just show a message box?

                //alright now we can delete all the old Properties.Settings.Default
                //lets hold off on this until we know stuff works aight
                //DeleteAllOldSettings();
                ConvertToNewSettings = true;
            } else
            {
                _bBBlog.Debug("No user-scoped settings found, so we can start fresh with the json user settings");
                //we should fill in the "sane" defaults at least before saving
                settings.SetDefaultSettings();
                settings.SaveSettings();
                //without this the constructor busy-wait below never finishes on a fresh install
                ConvertToNewSettings = true;
            }
        }

        //here we actually delete the runtime version
        //of the Properties.Settings.Default settings
        [SupportedOSPlatform("windows10.0.10240")]
        private static void DeleteAllOldSettings()
        {
            // Get the settings collection
            var settings = Properties.Settings.Default;

            // Create a list to store the names of the settings to be removed
            var settingsToRemove = new List<string>();

            // Iterate through all settings and add their names to the list
            foreach (SettingsProperty property in settings.Properties)
            {
                settingsToRemove.Add(property.Name);
            }

            // Remove each setting from the collection
            foreach (var settingName in settingsToRemove)
            {
                settings.Properties.Remove(settingName);
            }

            // Save the changes to persist them
            settings.Save();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task LoadSettings()
        {
            //TODO if there are any Properties.Settings.Default around we need to read them and convert them to the new settings format
            //this is the first time we are loading the settings, so we need to check if we need to convert any old settings

            //init the new settings class
            //var settings = UserSettingsManager.Settings;


            //should we read from file?

            _bBBlog.Debug("Reading settings': " + UserSettingsManager.Settings.OllamaURI);

            TwitchCommandTrigger.Text = UserSettingsManager.Settings.TwitchCommandTrigger;
            TwitchChatCommandDelay.Text = UserSettingsManager.Settings.TwitchChatCommandDelay.ToString();
            TwitchNeedsSubscriber.Checked = UserSettingsManager.Settings.TwitchNeedsSubscriber;
            TwitchMinBits.Text = UserSettingsManager.Settings.TwitchMinBits.ToString();
            TwitchSubscribed.Checked = UserSettingsManager.Settings.TwitchSubscribed;
            TwitchGiftedSub.Checked = UserSettingsManager.Settings.TwitchGiftedSub;
            TwitchAutoStart.Checked = UserSettingsManager.Settings.TwitchAutoStart;
            TwitchDelayFinishToChatcCheckBox.Checked = UserSettingsManager.Settings.DelayFinishToChatcCheckBox;
            if (TwitchDelayFinishToChatcCheckBox.Checked)
            {
                TwitchDelayMessageTextBox.Enabled = true;
            }
            else
            {
                TwitchDelayMessageTextBox.Enabled = false;
            }
            TwitchDelayMessageTextBox.Text = UserSettingsManager.Settings.TwitchDelayMessageTextBox;
            //todo: change to button
            TwitchEnableCheckbox.Checked = UserSettingsManager.Settings.TwitchEnableCheckbox;
            //setting the Twitch items enabled/disabled based on the checkbox
            if (UserSettingsManager.Settings.TwitchEnableCheckbox)
            {
                TwitchStartButton.Enabled = true;
                TwitchChatTriggerSettings.Enabled = true;
                TwitchCheerSettings.Enabled = true;
                TwitchSubscriberSettings.Enabled = true;
                TwitchChannelPointsSettings.Enabled = true;
                TwitchAutoStart.Enabled = true;
                TwitchSoundsSettings.Enabled = true;
                TwitchResponseSettings.Enabled = true;
            }
            else
            {
                TwitchStartButton.Enabled = false;
                TwitchChatTriggerSettings.Enabled = false;
                TwitchCheerSettings.Enabled = false;
                TwitchSubscriberSettings.Enabled = false;
                TwitchChannelPointsSettings.Enabled = false;
                TwitchAutoStart.Enabled = false;
                TwitchSoundsSettings.Enabled = false;
                TwitchResponseSettings.Enabled = false;
            }

            TwitchReadChatCheckBox.Checked = UserSettingsManager.Settings.TwitchReadChatCheckBox;
            TwitchCheerCheckBox.Checked = UserSettingsManager.Settings.TwitchCheerCheckbox;
            TwitchCustomRewardName.Text = UserSettingsManager.Settings.TwitchCustomRewardName;
            TwitchChannelPointCheckBox.Checked = UserSettingsManager.Settings.TwitchChannelPointCheckBox;
            STTSelectedComboBox.SelectedIndex = STTSelectedComboBox.FindStringExact(UserSettingsManager.Settings.STTSelectedProvider);
            if (STTSelectedComboBox.SelectedIndex == -1)
            {
                STTSelectedComboBox.SelectedIndex = 0;
            }

            //loading the persona's
            BroadcasterSelectedPersonaComboBox.SelectedIndex = BroadcasterSelectedPersonaComboBox.FindStringExact(UserSettingsManager.Settings.MainSelectedPersona);
            if (BroadcasterSelectedPersonaComboBox.SelectedIndex == -1)
            {
                if (BroadcasterSelectedPersonaComboBox.Items.Count > 0)
                    BroadcasterSelectedPersonaComboBox.SelectedIndex = 0;
                else
                {
                    _bBBlog.Error("No selected persona found and not populated yet");
                    await LoadPersonas();
                    BroadcasterSelectedPersonaComboBox.SelectedIndex = 0;
                }

            }

            TwitchCheeringPersonaComboBox.SelectedIndex = TwitchCheeringPersonaComboBox.FindStringExact(UserSettingsManager.Settings.TwitchCheeringPersona);
            if (TwitchCheeringPersonaComboBox.SelectedIndex == -1)
            {
                TwitchCheeringPersonaComboBox.SelectedIndex = 0;
            }
            TwitchChannelPointPersonaComboBox.SelectedIndex = TwitchChannelPointPersonaComboBox.FindStringExact(UserSettingsManager.Settings.TwitchChannelPointPersona);
            if (TwitchChannelPointPersonaComboBox.SelectedIndex == -1)
            {
                TwitchChannelPointPersonaComboBox.SelectedIndex = 0;
            }
            TwitchSubscriptionPersonaComboBox.SelectedIndex = TwitchSubscriptionPersonaComboBox.FindStringExact(UserSettingsManager.Settings.TwitchSubscriptionPersona);
            if (TwitchSubscriptionPersonaComboBox.SelectedIndex == -1)
            {
                TwitchSubscriptionPersonaComboBox.SelectedIndex = 0;
            }

            TwitchResponseToChatDelayTextBox.Text = UserSettingsManager.Settings.TwitchResponseToChatDelayTextBox;
            TwitchChatPersonaComboBox.SelectedIndex = TwitchChatPersonaComboBox.FindStringExact(UserSettingsManager.Settings.TwitchChatPersona);
            if (TwitchChatPersonaComboBox.SelectedIndex == -1)
            {
                TwitchChatPersonaComboBox.SelectedIndex = 0;
            }

            TwitchResponseToChatCheckBox.Checked = UserSettingsManager.Settings.TwitchResponseToChatCheckBox;

            //load settings but also have safe defaults
            TwitchSubscriptionTTSResponseOnlyRadioButton.Checked = UserSettingsManager.Settings.TwitchSubscriptionTTSResponseOnlyRadioButton;
            if (!TwitchSubscriptionTTSResponseOnlyRadioButton.Checked)
            {
                TwitchSubscriptionTTSEverythingRadioButton.Checked = true;
            }

            TwitchCheeringTTSResponseOnlyRadioButton.Checked = UserSettingsManager.Settings.TwitchCheeringTTSResponseOnlyRadioButton;
            if (!TwitchCheeringTTSResponseOnlyRadioButton.Checked)
            {
                TwitchCheeringTTSEverythingRadioButton.Checked = true;
            }

            TwitchChannelPointTTSResponseOnlyRadioButton.Checked = UserSettingsManager.Settings.TwitchChannelPointTTSResponseOnlyRadioButton;
            if (!TwitchChannelPointTTSResponseOnlyRadioButton.Checked)
            {
                TwitchChannelPointTTSEverythingRadioButton.Checked = true;
            }

            TwitchChatTTSResponseOnlyRadioButton.Checked = UserSettingsManager.Settings.TwitchChatTTSResponseOnlyRadioButton;
            if (!TwitchChatTTSResponseOnlyRadioButton.Checked)
            {
                TwitchChatTTSEverythingRadioButton.Checked = true;
            }

            TwitchChatSoundCheckBox.Checked = UserSettingsManager.Settings.TwitchChatSoundCheckBox;
            if (TwitchChatSoundCheckBox.Checked)
            {
                TwitchChatSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchChatSoundTextBox.Enabled = false;
            }
            TwitchChatSoundTextBox.Items.Clear();
            AddFilesToDropdown(TwitchChatSoundTextBox);
            TwitchChatSoundTextBox.SelectedIndex = TwitchChatSoundTextBox.FindStringExact(UserSettingsManager.Settings.TwitchChatSound);

            TwitchChannelSoundCheckBox.Checked = UserSettingsManager.Settings.TwitchChannelSoundCheckBox;
            if (TwitchChannelSoundCheckBox.Checked)
            {
                TwitchChannelSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchChannelSoundTextBox.Enabled = false;
            }
            TwitchChannelSoundTextBox.Items.Clear();
            AddFilesToDropdown(TwitchChannelSoundTextBox);
            TwitchChannelSoundTextBox.SelectedIndex = TwitchChannelSoundTextBox.FindStringExact(UserSettingsManager.Settings.TwitchChannelSound);

            TwitchCheeringSoundCheckBox.Checked = UserSettingsManager.Settings.TwitchCheeringSoundCheckBox;
            if (TwitchCheeringSoundCheckBox.Checked)
            {
                TwitchCheeringSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchCheeringSoundTextBox.Enabled = false;
            }
            TwitchCheeringSoundTextBox.Items.Clear();
            AddFilesToDropdown(TwitchCheeringSoundTextBox);
            TwitchCheeringSoundTextBox.SelectedIndex = TwitchCheeringSoundTextBox.FindStringExact(UserSettingsManager.Settings.TwitchCheeringSound);

            TwitchSubscriptionSoundCheckBox.Checked = UserSettingsManager.Settings.TwitchSubscriptionSoundCheckBox;
            if (TwitchSubscriptionSoundCheckBox.Checked)
            {
                TwitchSubscriptionSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchSubscriptionSoundTextBox.Enabled = false;
            }
            TwitchSubscriptionSoundTextBox.Items.Clear();
            AddFilesToDropdown(TwitchSubscriptionSoundTextBox);
            TwitchSubscriptionSoundTextBox.SelectedIndex = TwitchSubscriptionSoundTextBox.FindStringExact(UserSettingsManager.Settings.TwitchSubscriptionSoundTextBox);

            TwitchBadWordFilterCheckBox.Checked = UserSettingsManager.Settings.BadWordFilter;

            //check if theres a speaker selected if not select the default onne
            if (UserSettingsManager.Settings.TTSAudioOutput.Length < 1)
            {
                _bBBlog.Info("No audio output selected, setting to default");
                var enumerator = new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                UserSettingsManager.Settings.TTSAudioOutput = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console).FriendlyName;
                UserSettingsManager.SaveSettings();
            }

            //check if thers a default microphone selected, if not, select the default one
            if (UserSettingsManager.Settings.VoiceInput.Length < 1)
            {
                _bBBlog.Info("No microphone selected, setting to default");
                var enumerator = new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                UserSettingsManager.Settings.VoiceInput = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console).FriendlyName;
                UserSettingsManager.SaveSettings();
            }

            //check twitch Broadcaster key
            _bBBlog.Info("Checking Twitch Broadcaster API key if Twitch not active");
            //if Twitch is already running, we dont need to check the key
            if (UserSettingsManager.Settings.TwitchAccessToken.Length > 1 && TwitchAPIStatusTextBox.Text == "DISABLED" && TwitchEventSubStatusTextBox.Text == "DISABLED")
            {
                _bBBlog.Info("Twitch Broadcaster API key is set");
                UpdateTextLog("Twitch Broadcaster API key is set.\r\n");
                //lets check if the key is valid
                TwitchAPIESub twitchAPI = new();
                if (await twitchAPI.ValidateAccessToken(UserSettingsManager.Settings.TwitchAccessToken))
                {
                    _bBBlog.Info("Twitch Broadcaster API key is valid");
                    UpdateTextLog("Twitch Broadcaster API key is valid.\r\n");
                    _twitchAPIVerified = true;
                    TwitchStartButton.Enabled = true;
                }
                else
                {
                    _bBBlog.Error("Twitch Broadcaster API key is invalid");
                    UpdateTextLog("Twitch Broadcaster API key is invalid.\r\n");
                    TwitchStartButton.Enabled = false;
                }
            }
            else if (UserSettingsManager.Settings.TwitchAccessToken.Length < 1)
            {
                _bBBlog.Error("Twitch Broadcaster API key is not set");
                UpdateTextLog("Twitch Broadcaster API key is not set.\r\n");
            }

            // check twitch bot key
            //if Twitch is already running, we dont need to check the key
            if (UserSettingsManager.Settings.TwitchBotAuthKey.Length > 1 && TwitchAPIStatusTextBox.Text == "DISABLED" && TwitchEventSubStatusTextBox.Text == "DISABLED")
            {
                _bBBlog.Info("Twitch Bot API key is set");
                UpdateTextLog("Twitch Bot API key is set.\r\n");
                //lets check if the key is valid
                TwitchAPIESub twitchAPI = new();
                if (await twitchAPI.ValidateAccessToken(UserSettingsManager.Settings.TwitchBotAuthKey))
                {
                    _bBBlog.Info("Twitch Bot API key is valid");
                    UpdateTextLog("Twitch Bot API key is valid.\r\n");
                    //  _twitchAPIVerified = true;
                    //    TwitchStartButton.Enabled = true;
                }
                else
                {
                    _bBBlog.Error("Twitch Bot API key is invalid");
                    UpdateTextLog("Twitch Bot API key is invalid.\r\n");
                    //    TwitchStartButton.Enabled = false;
                }
            }
            else if (UserSettingsManager.Settings.TwitchBotAuthKey.Length < 1)
            {
                _bBBlog.Error("Twitch Bot API key is not set");
                UpdateTextLog("Twitch Bot API key is not set.\r\n");
            }

            //load HotkeyList into _setHotkeys
            if (UserSettingsManager.Settings.PTTHotkey.Length < 1)
            {
                _bBBlog.Info("No hotkey set");
            }
            else
            {
                _bBBlog.Info($"Loading hotkeys: {UserSettingsManager.Settings.PTTHotkey}");
                UpdateTextLog("PPT hotkey: " + UserSettingsManager.Settings.PTTHotkey + "\r\n");
                _bBBlog.Info("Hotkey set");
                var hotKeys = UserSettingsManager.Settings.PTTHotkey.Split('+').ToList();
                _setHotkeys.Clear();
                foreach (var key in hotKeys)
                {
                    _setHotkeys.Add((Keys)Enum.Parse(typeof(Keys), key));
                }
                Subscribe();
            }

            //lets check if the selected OpenAI API key is valid   
            if (UserSettingsManager.Settings.SelectedLLM == "ChatGPT")
            {
                if (await OpenAI.ValidateApiKeyAsync(UserSettingsManager.Settings.GPTAPIKey))
                {
                    _bBBlog.Info("GPT API key is valid");
                    UpdateTextLog("OpenAI key is valid.\r\n");
                }
                else
                {
                    _bBBlog.Error("GPT API is selected but key is invalid invalid. \r\n");
                    UpdateTextLog("OpenAI ChatGPT is selected as LLM but key is invalid. \r\n");
                    MessageBox.Show("OpenAI ChatGPT is selected as LLM but key is invalid. \r\n", "OpenAI ChatGPT error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else if (UserSettingsManager.Settings.SelectedLLM == "None" || UserSettingsManager.Settings.SelectedLLM == "")
            {
                UpdateTextLog("No LLM selected. You should set one in the settings first\r\n");
                LLMGroupSettingsGroupBox.Enabled = false;
            }

            if (TwitchAutoStart.Checked && TwitchAPIStatusTextBox.Text == "DISABLED" && TwitchEventSubStatusTextBox.Text == "DISABLED")
            {
                _bBBlog.Info("Auto starting Twitch");
                UpdateTextLog("Auto starting Twitch\r\n");
                TwitchStartButton_Click(null, null);
            }

            if (UserSettingsManager.Settings.StreamerNameTextBox.Length > 1)
                StreamerNameTextBox.Text = UserSettingsManager.Settings.StreamerNameTextBox;
            else
                StreamerNameTextBox.Text = "Streamer";

            if (UserSettingsManager.Settings.TwitchLLMLanguageComboBox.Length > 1)
            {
                TwitchLLMLanguageComboBox.SelectedIndex = TwitchLLMLanguageComboBox.FindStringExact(UserSettingsManager.Settings.TwitchLLMLanguageComboBox);
            }
            else
            {
                TwitchLLMLanguageComboBox.SelectedIndex = 0;
            }

            _bBBlog.Debug("All settings loaded");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void SaveALLSettings()
        {
            _bBBlog.Debug("Saving settings");
            //alright lets make sure we assign all values to the settings class
            UserSettingsManager.Settings.TwitchCommandTrigger = TwitchCommandTrigger.Text;
            UserSettingsManager.Settings.TwitchChatCommandDelay = ParseIntOrDefault(TwitchChatCommandDelay.Text, UserSettingsManager.Settings.TwitchChatCommandDelay);
            UserSettingsManager.Settings.TwitchNeedsSubscriber = TwitchNeedsSubscriber.Checked;
            UserSettingsManager.Settings.TwitchMinBits = ParseIntOrDefault(TwitchMinBits.Text, UserSettingsManager.Settings.TwitchMinBits);
            UserSettingsManager.Settings.TwitchSubscribed = TwitchSubscribed.Checked;
            UserSettingsManager.Settings.TwitchGiftedSub = TwitchGiftedSub.Checked;
            UserSettingsManager.Settings.TwitchEnable = TwitchEnableCheckbox.Checked;
            UserSettingsManager.Settings.TwitchReadChatCheckBox = TwitchReadChatCheckBox.Checked;
            UserSettingsManager.Settings.TwitchCheerCheckbox = TwitchCheerCheckBox.Checked;
            UserSettingsManager.Settings.TwitchCustomRewardName = TwitchCustomRewardName.Text;
            UserSettingsManager.Settings.TwitchChannelPointCheckBox = TwitchChannelPointCheckBox.Checked;
            UserSettingsManager.Settings.STTSelectedProvider = STTSelectedComboBox.Text;
            UserSettingsManager.Settings.MainSelectedPersona = BroadcasterSelectedPersonaComboBox.Text;
            UserSettingsManager.Settings.TwitchChannelPointPersona = TwitchChannelPointPersonaComboBox.Text;
            UserSettingsManager.Settings.TwitchCheeringPersona = TwitchCheeringPersonaComboBox.Text;
            UserSettingsManager.Settings.TwitchSubscriptionPersona = TwitchSubscriptionPersonaComboBox.Text;
            UserSettingsManager.Settings.TwitchChatPersona = TwitchChatPersonaComboBox.Text;
            UserSettingsManager.Settings.TwitchAutoStart = TwitchAutoStart.Checked;
            UserSettingsManager.Settings.SelectedLLM = LLMResponseSelecter.Text;
            UserSettingsManager.Settings.TwitchChatSoundCheckBox = TwitchChatSoundCheckBox.Checked;
            UserSettingsManager.Settings.TwitchChatSound = TwitchChatSoundTextBox.Text;
            UserSettingsManager.Settings.TwitchChannelSoundCheckBox = TwitchChannelSoundCheckBox.Checked;
            UserSettingsManager.Settings.TwitchChannelSound = TwitchChannelSoundTextBox.Text;
            UserSettingsManager.Settings.TwitchCheeringSound = TwitchCheeringSoundTextBox.Text;
            UserSettingsManager.Settings.TwitchCheeringSoundCheckBox = TwitchCheeringSoundCheckBox.Checked;
            UserSettingsManager.Settings.TwitchSubscriptionSoundTextBox = TwitchSubscriptionSoundTextBox.Text;
            UserSettingsManager.Settings.TwitchSubscriptionSoundCheckBox = TwitchSubscriptionSoundCheckBox.Checked;
            UserSettingsManager.Settings.TwitchResponseToChatCheckBox = TwitchResponseToChatCheckBox.Checked;
            UserSettingsManager.Settings.TwitchResponseToChatDelayTextBox = TwitchResponseToChatDelayTextBox.Text;
            UserSettingsManager.Settings.TwitchDelayMessageTextBox = TwitchDelayMessageTextBox.Text;
            UserSettingsManager.Settings.DelayFinishToChatcCheckBox = TwitchDelayFinishToChatcCheckBox.Checked;
            UserSettingsManager.Settings.TwitchSubscriptionTTSResponseOnlyRadioButton = TwitchSubscriptionTTSResponseOnlyRadioButton.Checked;
            UserSettingsManager.Settings.TwitchCheeringTTSResponseOnlyRadioButton = TwitchCheeringTTSResponseOnlyRadioButton.Checked;
            UserSettingsManager.Settings.TwitchChannelPointTTSResponseOnlyRadioButton = TwitchChannelPointTTSResponseOnlyRadioButton.Checked;
            UserSettingsManager.Settings.TwitchChatTTSResponseOnlyRadioButton = TwitchChatTTSResponseOnlyRadioButton.Checked;
            UserSettingsManager.Settings.StreamerNameTextBox = StreamerNameTextBox.Text;
            UserSettingsManager.Settings.TwitchLLMLanguageComboBox = TwitchLLMLanguageComboBox.Text;
            UserSettingsManager.SaveSettings();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void BBB_FormClosing(object sender, FormClosingEventArgs e)
        {
            //turn off potentionally running Twitch stuff
            if (_globalTwitchAPI != null)
                await _globalTwitchAPI.EventSubStopAsync();

            if (_twitchEventSub != null)
                await _twitchEventSub.EventSubStopAsync();
            //only save if theres something to be saved!

            SaveALLSettings();


            //remove hotkey hooks
            Unsubscribe();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }


        [SupportedOSPlatform("windows10.0.10240")]
        //Keyboard hooks
        public void Unsubscribe()
        {
            if (_setHotkeys.Count < 1)
            {
                _bBBlog.Info("No hotkeys set, not unsubscribing");
                return;
            }
            else
            {
                _bBBlog.Info("Unsubscribing from hotkeys");
            }
            if (m_GlobalHook != null)
            {
                m_GlobalHook.KeyDown -= GlobalHookKeyDown;
                m_GlobalHook.KeyUp -= GlobalHookKeyUp;
                //It is recommened to dispose it
                m_GlobalHook.Dispose();
                m_GlobalHook = null;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public void Subscribe()
        {
            if (_setHotkeys.Count < 1)
            {
                _bBBlog.Info("No hotkeys set, not subscribing");
                return;
            }
            _bBBlog.Info("Subscribing to hotkeys");
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHookKeyDown;
            m_GlobalHook.KeyUp += GlobalHookKeyUp;

            var map = new Dictionary<Combination, Action>
             {
                {Combination.FromString(UserSettingsManager.Settings.PTTHotkey), async () => await HandleHotkeyButton() }
             };

            m_GlobalHook.OnCombination(map);

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task HandleHotkeyButton()
        {
            if (!_hotkeyCalled)
            {
                if (BBB.ActiveForm != null)
                    if (BBB.ActiveForm.Name == "SettingsForm")
                    {
                        _bBBlog.Debug("activeform: " + BBB.ActiveForm.Name + " has focus so ignoring hotkey");
                        return;
                    }

                //&& BBB.ActiveForm.Name != "SettingsForm"
                if (MainRecordingStart.Text == "Start")
                {
                    MainRecordingStart_Click(null, null);
                    _hotkeyCalled = true;
                    //ok lets wait 1 sec before checking shit again
                    await Task.Delay(1000);
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void GlobalHookKeyUp(object sender, KeyEventArgs e)
        {

            //if microphone is on (hotkeycalled = true) and of the hotkeys are in the keyup event turn off the microphone
            //current hotkeys are in _setHotkeys
            if (_hotkeyCalled)
            {
                //if one of the keys in the _setHotkeys is detected as UP, give it a second then stop recording

                if (_setHotkeys.Contains(e.KeyCode) && MainRecordingStart.Text == "Recording")
                {
                    MainRecordingStart_Click(null, null);
                    await Task.Delay(1000);
                    _hotkeyCalled = false;
                }
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        //handle the current hotkey setting
        private void GlobalHookKeyDown(object sender, KeyEventArgs e)
        {
            var keyCombo = "";
            for (int i = 0; i < _setHotkeys.Count; i++)
            {
                if (e.KeyCode == _setHotkeys[i])
                {
                    keyCombo += _setHotkeys[i].ToString() + " + ";
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void GithubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //spawn browser for github link
            var t = new Thread(() => Process.Start(new ProcessStartInfo("https://github.com/WhiskerWeirdo/BanterBrain-Buddy/wiki") { UseShellExecute = true }));
            t.Start();
            Thread.Sleep(100);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void DiscordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //spawn browser for discord link
            var t = new Thread(() => Process.Start(new ProcessStartInfo("https://discord.banterbrain.tv") { UseShellExecute = true }));
            t.Start();
            Thread.Sleep(100);
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchEnableCheckbox_Click(object sender, EventArgs e)
        {
            //this turns off and on all twitch options
            _bBBlog.Debug("Twitch enable checkbox changed to " + TwitchEnableCheckbox.Checked);
        }

        //here we star the main websocket client for Twitch EventSub
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task<bool> EventSubStartWebsocketClient()
        {
            _twitchEventSub = new();
            bool eventSubStart = false;

            //we set the channel chat sender and auth key using the API verification
            //this is due to that if we use a bot account, it is seperate from teh account we use for subscribing to eventsub
            if (UserSettingsManager.Settings.TwitchBotAuthKey.Length > 1)
            {
                _twitchChatUser = new();
                //CheckAuthCodeAPI(TwitchBotAuthKey.Text, TwitchBotName.Text, TwitchBroadcasterChannel.Text);
                await _twitchChatUser.CheckAuthCodeAPI(UserSettingsManager.Settings.TwitchBotAuthKey, UserSettingsManager.Settings.TwitchBotName, UserSettingsManager.Settings.TwitchChannel);
            }
            else
            {
                await _twitchEventSub.CheckAuthCodeAPI(UserSettingsManager.Settings.TwitchAccessToken, UserSettingsManager.Settings.TwitchChannel, UserSettingsManager.Settings.TwitchChannel);
            }

            if (await _twitchEventSub.EventSubInit(UserSettingsManager.Settings.TwitchAccessToken, UserSettingsManager.Settings.TwitchChannel, UserSettingsManager.Settings.TwitchChannel))
            {
                //we need to first set the event handlers we want to use

                //do we want to check chat messages?
                if (TwitchReadChatCheckBox.Checked)
                {
                    _bBBlog.Info("Twitch read chat enabled, calling eventsubhandlereadchat");
                    _twitchEventSub.EventSubHandleReadchat(TwitchCommandTrigger.Text, false, TwitchNeedsSubscriber.Checked);
                    //set local eventhanlder for valid chat messages to trigger the bot
                    _twitchEventSub.OnESubChatMessage += TwitchEventSub_OnESubChatMessage;
                }

                //do we want to check cheer messages?
                if (TwitchCheerCheckBox.Checked)
                {
                    _bBBlog.Info("Twitch cheers enabled, calling EventSubHandleCheer with the min amount of bits needed to trigger");
                    _twitchEventSub.EventSubHandleCheer(ParseIntOrDefault(TwitchMinBits.Text, UserSettingsManager.Settings.TwitchMinBits));
                    _twitchEventSub.OnESubCheerMessage += TwitchEventSub_OnESubCheerMessage;
                }

                //do we want to check for subscription events?
                if (TwitchSubscribed.Checked)
                {
                    //new subs
                    _bBBlog.Info($"Twitch subscriptions enabled, calling EventSubHandleSubscription: {_twitchEventSub}");
                    _twitchEventSub.EventSubHandleSubscription();
                    _twitchEventSub.OnESubSubscribe += TwitchEventSub_OnESubSubscribe;
                    _twitchEventSub.OnESubReSubscribe += TwitchEventSub_OnESubReSubscribe;
                    //todo set eventhandler being thrown when a new sub is detected or resub
                }
                //TODO: gifted subs TwitchGiftedSub.Checked
                if (TwitchGiftedSub.Checked)
                {
                    _bBBlog.Info("Twitch gifted subs enabled, calling EventSubHandleGiftedSubs");
                    _twitchEventSub.EventSubHandleSubscriptionGift();
                    _twitchEventSub.OnESubSubscriptionGift += TwitchEventSub_OnESubGiftedSub;
                }

                //do we want to check for channel point redemptions?
                if (TwitchChannelPointCheckBox.Checked)
                {
                    _bBBlog.Info("Twitch channel points enabled, calling EventSubHandleChannelPoints");
                    _twitchEventSub.EventSubHandleChannelPointRedemption(TwitchCustomRewardName.Text);
                    _twitchEventSub.OnESubChannelPointRedemption += TwitchEventSub_OnESubChannelPointRedemption;
                }

                // we need to enable MOCK here
                if (TwitchMockCheckBox.Checked)
                {
                    _bBBlog.Info("Twitch Mock enabled, calling EventSubHandleMock");
                    eventSubStart = await _twitchEventSub.EventSubStartAsyncMock();
                }
                else
                {
                    //this is prod
                    eventSubStart = await _twitchEventSub.EventSubStartAsync();
                }

                if (eventSubStart)
                {
                    if (TwitchMockCheckBox.Checked)
                    {
                        _bBBlog.Info("Twitch EventSub client started in MOCK mode successfully");
                        UpdateTextLog("Twitch EventSub client started in MOCK mode successfully\r\n");
                    }
                    else
                    {
                        _bBBlog.Info("Twitch EventSub client  started successfully");
                        UpdateTextLog("Twitch EventSub client started successfully\r\n");
                    }
                    TwitchEventSubStatusTextBox.Text = "ENABLED";
                    TwitchEventSubStatusTextBox.BackColor = Color.Green;
                    _twitchStarted = true;
                }
                else
                {
                    _bBBlog.Error("Issue with starting Twitch EventSub server. Check logs for more information.");
                    TwitchEventSubStatusTextBox.Text = "DISABLED";
                    TwitchEventSubStatusTextBox.BackColor = Color.Red;
                    _twitchStarted = false;
                    return false;
                }
                return true;
            }
            else
            {
                _bBBlog.Error("Issue with starting Twitch EventSub server. Check logs for more information.");
                TwitchEventSubStatusTextBox.Text = "DISABLED";
                TwitchEventSubStatusTextBox.BackColor = Color.Red;
                _twitchStarted = false;
                return false;
            }
        }


        [SupportedOSPlatform("windows10.0.10240")]
        /// <summary>
        /// To write to TextLog irregardless of thread
        public void UpdateTextLog(string TextToAppend)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            TextToAppend = timestamp + " " + TextToAppend;
            if (!InvokeRequired && TextLog != null)
            {
                try
                {
                    TextLog.AppendText(TextToAppend);
                    TextLog.SelectionStart = TextLog.Text.Length;
                    TextLog.ScrollToCaret();
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Error writing to TextLog: " + ex.Message);
                }
            }
            else if (TextLog != null)
            {
                Invoke(new Action(() =>
                {
                    TextLog.AppendText(TextToAppend);
                }));
            }
            else if (TextLog == null)
            {
                _bBBlog.Error("TextLog is null, cannot write to it");
            }
        }

        //A simple way to invoke UI elements from another thread
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task InvokeUI(Action a)
        {
            await Task.Run(() => this.BeginInvoke(new System.Windows.Forms.MethodInvoker(a)));
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchEventSub_OnESubCheerMessage(object sender, TwitchEventhandlers.OnCheerEventsArgs e)
        {
            //we got a valid cheer message, lets see what we can do with it
            _bBBlog.Info("Valid Twitch Cheer message received");

            string user = e.GetCheerInfo()[0];
            string message = e.GetCheerInfo()[1];

            //now that we have the user, we check if this user is in the notable viewer list, if so we add the flavour text from that user to be passed to the LLM
            string notableUserFlavourText = "";
            if (MyTwitchNotableViewers != null)
            {
                foreach (var viewer in MyTwitchNotableViewers)
                {
                    if (String.Equals(viewer.ViewerName, user, StringComparison.OrdinalIgnoreCase))
                    {
                        _bBBlog.Info("Notable viewer found in cheer message, adding flavour text");
                        UpdateTextLog($"Notable viewer {user} found in cheer message, adding flavour text\r\n");
                        notableUserFlavourText = " " + viewer.FlavourText;
                    }
                }
            }

            await InvokeUI(async () =>
            {
                if (!string.IsNullOrEmpty(message) && message.Length >= 1)
                    await TalkToLLM(TwitchLLMLanguage.CheerTalkToLLM.Replace("{user}", user).Replace("{message}", message), GetPersonaRoleTextOrDefault(UserSettingsManager.Settings.TwitchCheeringPersona) + notableUserFlavourText);
                else
                {
                    message = "no message";
                    _gPTOutputText = TwitchLLMLanguage.CheerDefaultNoMessage;
                    //no LLM call happened, so the wait below must not spin forever
                    _gPTDone = true;
                }

                //we have to await the GPT response, due to running this from another thread await alone is not enough.
                while (!_gPTDone)
                {
                    await Task.Delay(500);
                }

                //ok we waited, lets say the response, but we need a small delay to not sound unnatural      

                if (TwitchCheeringSoundCheckBox.Checked)
                {
                    if (TwitchCheeringSoundTextBox.Text.Length > 1)
                    {
                        _bBBlog.Info("Playing alert sound for cheer message");
                        await PlayWaveFile(TwitchCheeringSoundTextBox.Text);
                    }
                }

                if (TwitchCheeringTTSEverythingRadioButton.Checked)
                {
                    UpdateTextLog($"Valid Twitch Cheer message received from {user}\r\n");
                    await SayText(TwitchLLMLanguage.CheerWithMessage.Replace("{user}", user).Replace("{message}", message), 1000, GetSelectedPersona(UserSettingsManager.Settings.TwitchCheeringPersona));
                }

                _bBBlog.Info("LLM response received, saying it");
                await SayText(_gPTOutputText, 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchCheeringPersona));
                _bBBlog.Info("LLM response said");
                //do we need to post the response in chat?
                if (TwitchResponseToChatCheckBox.Checked)
                {
                    //first lets wait the delay set in the textbox
                    await Task.Delay(ParseIntOrDefault(TwitchResponseToChatDelayTextBox.Text, 1) * 1000);
                    //then post the response
                    await _twitchEventSub.SendMessage(_gPTOutputText);
                }
            });
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchEventSub_OnESubChannelPointRedemption(object sender, TwitchEventhandlers.OnChannelPointCustomRedemptionEventArgs e)
        {
            string user = e.GetChannelPointCustomRedemptionInfo()[0];
            string message = e.GetChannelPointCustomRedemptionInfo()[1];
            _gPTDone = false;
            _bBBlog.Info($"Valid Twitch Channel Point Redemption message received: {user} redeemed with message: {message}");

            string notableUserFlavourText = "";

            //now that we have the user, we check if this user is in the notable viewer list, if so we add the flavour text from that user to be passed to the LLM
            if (MyTwitchNotableViewers != null)
            {
                foreach (var viewer in MyTwitchNotableViewers)
                {
                    if (String.Equals(viewer.ViewerName, user, StringComparison.OrdinalIgnoreCase))
                    {
                        _bBBlog.Info("Notable viewer found in channel point redemption message, adding flavour text");
                        UpdateTextLog($"Notable viewer {user} found in channel point redemption message, adding flavour text\r\n");
                        notableUserFlavourText = " " + viewer.FlavourText;
                    }
                }
            }

            await InvokeUI(async () =>
            {
                if (!string.IsNullOrEmpty(message) && message.Length >= 1)
                    await TalkToLLM(TwitchLLMLanguage.ChannelPointTalkToLLM.Replace("{user}", user).Replace("{message}", message), GetPersonaRoleTextOrDefault(UserSettingsManager.Settings.TwitchChannelPointPersona) + notableUserFlavourText);
                else
                {
                    message = "no message";
                    _gPTOutputText = TwitchLLMLanguage.ChannelPointDefaultNoMessage;
                    //no LLM call happened, without this the wait below spins forever
                    _gPTDone = true;
                }

                while (!_gPTDone)
                {
                    await Task.Delay(500);
                }

                if (TwitchChannelSoundCheckBox.Checked)
                {
                    if (TwitchChannelSoundTextBox.Text.Length > 1)
                    {
                        _bBBlog.Info("Playing alert sound for channel point redemption message");
                        await PlayWaveFile(TwitchChannelSoundTextBox.Text);
                    }
                }

                if (TwitchChannelPointTTSEverythingRadioButton.Checked)
                {
                    _bBBlog.Info("Lets say a short \"thank you\" for the channel point redemption, and pass the text to the LLM");
                    await SayText(TwitchLLMLanguage.ChannelPointWithMessage.Replace("{user}", user).Replace("{message}", message), 3000, GetSelectedPersona(UserSettingsManager.Settings.TwitchChannelPointPersona));
                }

                //ok we waited, lets say the response, but we need a small delay to not sound unnatural
                await SayText(_gPTOutputText, 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchChannelPointPersona));
                _bBBlog.Info("LLM response said");
                //do we need to post the response in chat?
                if (TwitchResponseToChatCheckBox.Checked)
                {
                    //first lets wait the delay set in the textbox
                    await Task.Delay(ParseIntOrDefault(TwitchResponseToChatDelayTextBox.Text, 1) * 1000);
                    //then post the response
                    await _twitchEventSub.SendMessage(_gPTOutputText);
                }
            });
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //TwitchEventSub_OnESubGiftedSub
        private async void TwitchEventSub_OnESubGiftedSub(object sender, TwitchEventhandlers.OnSubscriptionGiftEventArgs e)
        {
            string user = e.GetSubscriptionGiftInfo()[0];
            string amount = e.GetSubscriptionGiftInfo()[1];
            string tier = e.GetSubscriptionGiftInfo()[2];
            _bBBlog.Info($"Valid Twitch Gifted Subscription message received: {user} gifted {amount} subs tier {tier}");

            await InvokeUI(async () =>
            {
                if (TwitchSubscriptionSoundCheckBox.Checked)
                {
                    if (TwitchSubscriptionSoundTextBox.Text.Length > 1)
                    {
                        _bBBlog.Info("Playing alert sound for subscription message");
                        await PlayWaveFile(TwitchSubscriptionSoundTextBox.Text);
                    }
                }
                //we always want to thank a gifted sub, so we dont need to check if the TTS is enabled
                _bBBlog.Info("Lets say a short \"thank you\" for the gifted sub(s)");
                if (ParseIntOrDefault(amount, 0) > 1)
                    await SayText(TwitchLLMLanguage.GiftedMultipleSubs.Replace("{user}", user).Replace("{amount}", amount).Replace("{tier}", tier), 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchSubscriptionPersona));
                else
                    await SayText(TwitchLLMLanguage.GiftedSingleSub.Replace("{user}", user).Replace("{amount}", amount).Replace("{tier}", tier), 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchSubscriptionPersona));
            });

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchEventSub_OnESubSubscribe(object sender, TwitchEventhandlers.OnSubscribeEventArgs e)
        {
            string user = e.GetSubscribeInfo()[0];
            string broadcaster = e.GetSubscribeInfo()[1];
            _bBBlog.Info($"Valid Twitch Subscription message received: {user} subscribed to {broadcaster}");

            await InvokeUI(async () =>
            {
                if (TwitchSubscriptionSoundCheckBox.Checked)
                {
                    if (TwitchSubscriptionSoundTextBox.Text.Length > 1)
                    {
                        _bBBlog.Info("Playing alert sound for subscription message");
                        await PlayWaveFile(TwitchSubscriptionSoundTextBox.Text);
                    }
                }


                if (TwitchSubscriptionTTSEverythingRadioButton.Checked)
                {
                    _bBBlog.Info("Lets say a short \"thank you\" for the subscriber");
                    await SayText(TwitchLLMLanguage.SubscribeFirstTimeThanks.Replace("{user}", user), 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchSubscriptionPersona));
                }
            });
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchEventSub_OnESubReSubscribe(object sender, TwitchEventhandlers.OnReSubscribeEventArgs e)
        {
            string user = e.GetSubscribeInfo()[0];
            string message = e.GetSubscribeInfo()[1];
            string months = e.GetSubscribeInfo()[2];
            _gPTDone = false;
            _bBBlog.Info($"Valid Twitch Re-Subscription message received: {user} subscribed for {months} months with message: {message}");

            string notableUserFlavourText = "";

            //now that we have the user, we check if this user is in the notable viewer list, if so we add the flavour text from that user to be passed to the LLM
            if (MyTwitchNotableViewers != null)
            {
                foreach (var viewer in MyTwitchNotableViewers)
                {
                    if (String.Equals(viewer.ViewerName, user, StringComparison.OrdinalIgnoreCase))
                    {
                        _bBBlog.Info("Notable viewer found in resubscriber message, adding flavour text");
                        UpdateTextLog($"Notable viewer {user} found in resubscriber message, adding flavour text\r\n");
                        notableUserFlavourText = " " + viewer.FlavourText;
                    }
                }
            }

            //a resub without a message still has to be acknowledged, so the LLM part is optional
            await InvokeUI(async () =>
            {
                bool hasMessage = !string.IsNullOrEmpty(message) && message.Length >= 1;
                if (hasMessage)
                {
                    _gPTDone = false;
                    await TalkToLLM(TwitchLLMLanguage.SubscribeResubThanksWithMessageLLM.Replace("{user}", user).Replace("{message}", message), GetPersonaRoleTextOrDefault(UserSettingsManager.Settings.TwitchSubscriptionPersona) + notableUserFlavourText);
                }
                else
                {
                    //no LLM call happened, so the wait below must not spin forever
                    _gPTDone = true;
                }

                //we have to await the GPT response, due to running this from another thread await alone is not enough.
                while (!_gPTDone)
                {
                    await Task.Delay(500);
                }

                //do we need to play a sound?
                if (TwitchSubscriptionSoundCheckBox.Checked)
                {
                    if (TwitchSubscriptionSoundTextBox.Text.Length > 1)
                    {
                        _bBBlog.Info("Playing alert sound for subscription message");
                        await PlayWaveFile(TwitchSubscriptionSoundTextBox.Text);
                    }
                }

                //do we need to TTS everything?
                if (TwitchSubscriptionTTSEverythingRadioButton.Checked)
                {
                    if (!hasMessage)
                        await SayText(TwitchLLMLanguage.SubscribeMonthsNoMessage.Replace("{user}", user), 1000, GetSelectedPersona(UserSettingsManager.Settings.TwitchSubscriptionPersona));
                    else
                    {
                        await SayText(TwitchLLMLanguage.SubscribeMonthsMessage.Replace("{user}", user).Replace("{message}", message), 1000, GetSelectedPersona(UserSettingsManager.Settings.TwitchSubscriptionPersona));
                    }
                }

                //ok we waited, lets say the response, but we need a small delay to not sound unnatural   
                if (hasMessage && !string.IsNullOrWhiteSpace(_gPTOutputText))
                {
                    await SayText(_gPTOutputText, 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchSubscriptionPersona));
                    _bBBlog.Info("LLM response said");
                    //do we need to post the response in chat?
                    if (TwitchResponseToChatCheckBox.Checked)
                    {
                        //first lets wait the delay set in the textbox
                        await Task.Delay(ParseIntOrDefault(TwitchResponseToChatDelayTextBox.Text, 1) * 1000);
                        //then post the response
                        await _twitchEventSub.SendMessage(_gPTOutputText);
                    }
                }
            });
        }


        //safe int parse for textbox values, event handlers run off the UI thread so a bad value must never throw
        private static int ParseIntOrDefault(string value, int defaultValue)
        {
            if (int.TryParse(value, out var result))
                return result < 0 ? 0 : result;
            _bBBlog.Warn($"Could not parse \"{value}\" to an int, using default {defaultValue}");
            return defaultValue;
        }

        //safe read of a persona VoiceOptions entry as a string (e.g. Azure style option), the list can be empty or shorter than expected
        private static string GetVoiceOptionOrDefault(List<string> options, int index, string defaultValue)
        {
            if (options != null && index >= 0 && index < options.Count && !string.IsNullOrWhiteSpace(options[index]))
                return options[index];
            _bBBlog.Warn($"Persona VoiceOptions missing at index {index}, using default '{defaultValue}'");
            return defaultValue;
        }

        //safe read+parse of a persona VoiceOptions entry, the list can be empty or shorter than expected
        private static int ParseVoiceOptionOrDefault(List<string> options, int index, int defaultValue)
        {
            if (options != null && index >= 0 && index < options.Count)
                return ParseIntOrDefault(options[index], defaultValue);
            _bBBlog.Warn($"Persona VoiceOptions missing at index {index}, using default {defaultValue}");
            return defaultValue;
        }

        //safe read of a persona's role text, the Twitch event handlers run from lambdas on the UI
        //thread where a null persona (empty personas list) must never throw
        [SupportedOSPlatform("windows10.0.10240")]
        private string GetPersonaRoleTextOrDefault(string personaName)
        {
            var persona = GetSelectedPersona(personaName);
            return persona != null ? persona.RoleText : "";
        }

        //eventhandler for valid chat messages trigger
        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchEventSub_OnESubChatMessage(object sender, TwitchEventhandlers.OnChatEventArgs e)
        {

            string message = e.GetChatInfo()[1].Replace(TwitchCommandTrigger.Text, "");
            string user = e.GetChatInfo()[0];
            //we got a valid chat message, lets see what we can do with it
            _bBBlog.Info("Valid Twitch Chat message received from user: " + user + " message: " + message + " using: " + UserSettingsManager.Settings.TwitchChatPersona + "\r\n");
            _gPTDone = false;
            //we use InvokeUI to make sure we can write to the textlog from another thread that is not the Ui thread.
            //we only have to say this part if we have to say everything!

            string notableUserFlavourText = "";

            //now that we have the user, we check if this user is in the notable viewer list, if so we add the flavour text from that user to be passed to the LLM
            if (MyTwitchNotableViewers != null)
            {
                foreach (var viewer in MyTwitchNotableViewers)
                {
                    if (String.Equals(viewer.ViewerName, user, StringComparison.OrdinalIgnoreCase))
                    {
                        _bBBlog.Info("Notable viewer found in chat message, adding flavour text");
                        UpdateTextLog($"Notable viewer {user} found in chat message, adding flavour text\r\n");
                        notableUserFlavourText = " " + viewer.FlavourText;
                    }
                }
            }

            await InvokeUI(async () =>
            {
                UpdateTextLog("Valid Twitch Chat message received from user: " + user + " message: " + message + ". Using trigger persona: " + UserSettingsManager.Settings.TwitchChatPersona + "\r\n");
                await TalkToLLM(TwitchLLMLanguage.ChatMessageResponseLLM.Replace("{user}", user).Replace("{message}", message), GetPersonaRoleTextOrDefault(UserSettingsManager.Settings.TwitchChatPersona) + notableUserFlavourText);

                //we have to await the GPT response, due to running this from another thread await alone is not enough.
                while (!_gPTDone)
                {
                    await Task.Delay(500);
                }

                //do we need to play the sound?
                if (TwitchChatSoundCheckBox.Checked)
                {
                    if (TwitchChatSoundTextBox.Text.Length > 1)
                    {

                        _bBBlog.Info("Playing alert sound for chat message");
                        await PlayWaveFile(TwitchChatSoundTextBox.Text);

                    }
                }
                //do we need to TTS everything?
                if (TwitchChatTTSEverythingRadioButton.Checked)
                {
                    await SayText(TwitchLLMLanguage.ChatMessageUserSaid.Replace("{user}", user).Replace("{message}", message), 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchChatPersona));
                }

                //ok we waited, lets say the response, but we need a small delay to not sound unnatural      
                await SayText(_gPTOutputText, 0, GetSelectedPersona(UserSettingsManager.Settings.TwitchChatPersona));

                _bBBlog.Info("LLM response said");
                //do we need to post the response in chat?
                if (TwitchResponseToChatCheckBox.Checked)
                {
                    //first lets wait the delay set in the textbox
                    await Task.Delay(ParseIntOrDefault(TwitchResponseToChatDelayTextBox.Text, 1) * 1000);
                    //then post the response
                    await _twitchEventSub.SendMessage(_gPTOutputText);
                }

                //we need to call the eventhandler again for chat messages to have its delay and continue.
                UpdateTextLog($"Starting command cooldown for next chat messageof : {TwitchChatCommandDelay.Text} seconds\r\n");
                await _twitchEventSub.EventSubEnableChatMessageHandlerAfterDelay(ParseIntOrDefault(TwitchChatCommandDelay.Text, UserSettingsManager.Settings.TwitchChatCommandDelay));
                UpdateTextLog("Cooldown ended listening again for next chat message\r\n");
            });

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchReadChatCheckBox_Click(object sender, EventArgs e)
        {
            //if twitch isnt enabled, we cant do anything internal so ignore anything Twitch related
            if (!_twitchStarted)
            {
                _bBBlog.Debug("Twitch not enabled, ignoring read chat checkbox");
                return;
            }
            _bBBlog.Debug($"eventsub websocket: {_twitchEventSub._eventSubWebsocketClient.SessionId}");
            if (TwitchReadChatCheckBox.Checked)
            {
                if (TwitchCommandTrigger.Text.Length < 1)
                {
                    MessageBox.Show("You need to set a command trigger to watch for!", "Twitch error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _bBBlog.Error("You need to set a command trigger to watch for!");
                    TwitchReadChatCheckBox.Checked = false;
                    return;
                }
                _bBBlog.Info("This enables reading chat messages to watch for a command, in busy channels this will cause significant load on your computer");
                MessageBox.Show("Reading chat creates a high load on busy channels. Be warned!", "Twitch Channel messages enabled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //just double check its not already enabled
                //_globalTwitchAPI
                if (!_globalTwitchAPI.EventSubReadChatMessages)
                {
                    _bBBlog.Info("Twitch read chat enabled.");
                    TwitchEnableDisableFields();
                    _twitchEventSub.EventSubHandleReadchat(TwitchCommandTrigger.Text, false, TwitchNeedsSubscriber.Checked);
                    //set local eventhanlder for valid chat messages to trigger the bot
                    _twitchEventSub.OnESubChatMessage += TwitchEventSub_OnESubChatMessage;
                }
            }
            else
            {
                _bBBlog.Info("Twitch read chat unchecked. Disabling event handler.");
                TwitchEnableDisableFields();
                _twitchEventSub.OnESubChatMessage -= TwitchEventSub_OnESubChatMessage;
                await _twitchEventSub.EventSubStopReadChat();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchCheerCheckbox_Click(object sender, EventArgs e)
        {
            //if twitch isnt enabled, we cant do anything internal so ignore anything Twitch related
            if (!_twitchStarted)
            {
                _bBBlog.Debug("Twitch not enabled, ignoring cheer checkbox");
                return;
            }
            _bBBlog.Debug($"eventsub websocket: {_twitchEventSub._eventSubWebsocketClient.SessionId}");
            if (TwitchCheerCheckBox.Checked)
            {
                if (TwitchMinBits.Text.Length < 1)
                {
                    MessageBox.Show("You need to set a minimum amount of bits to trigger the bot", "Twitch error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _bBBlog.Error("You need to set a minimum amount of bits to trigger the bot");
                    TwitchCheerCheckBox.Checked = false;
                    return;
                }
                _bBBlog.Info("This enables reading cheers and messages when cheered over a certain amount");
                //just double check its not already enabled
                if (!_globalTwitchAPI.EventSubCheckCheer)
                {
                    _bBBlog.Info("Twitch cheering enabled.");
                    _twitchEventSub.EventSubHandleCheer(ParseIntOrDefault(TwitchMinBits.Text, UserSettingsManager.Settings.TwitchMinBits));
                    //set local eventhanlder for valid chat messages to trigger the bot
                    _twitchEventSub.OnESubCheerMessage += TwitchEventSub_OnESubCheerMessage;
                }
            }
            else
            {
                _bBBlog.Info("Twitch cheering unchecked. Disabling event handler.");
                _twitchEventSub.OnESubCheerMessage -= TwitchEventSub_OnESubCheerMessage;
                await _twitchEventSub.EventSubStopCheer();

            }
            TwitchEnableDisableFields();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchSubscribed_Click(object sender, EventArgs e)
        {
            //if twitch isnt enabled, we cant do anything internal so ignore anything Twitch related
            if (!_twitchStarted)
            {
                _bBBlog.Debug("Twitch not enabled, ignoring subscribed checkbox");
                return;
            }
            _bBBlog.Debug($"eventsub websocket: {_twitchEventSub._eventSubWebsocketClient.SessionId}");
            if (TwitchSubscribed.Checked)
            {
                _bBBlog.Info("This enables reading subscription events");

                //just double check its not already enabled
                if (!_globalTwitchAPI.EventSubCheckSubscriptions)
                {
                    _bBBlog.Info("Twitch subscriptions enabled.");
                    _twitchEventSub.EventSubHandleSubscription();
                    //set local eventhanlder for valid chat messages to trigger the bot
                    _twitchEventSub.OnESubSubscribe += TwitchEventSub_OnESubSubscribe;
                }
            }
            else
            {
                _bBBlog.Info("Twitch subscriptions unchecked. Disabling event handler.");
                _twitchEventSub.OnESubSubscribe -= TwitchEventSub_OnESubSubscribe;
                await _twitchEventSub.EventSubStopSubscription();
            }
            TwitchEnableDisableFields();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchGiftedSub_Click(object sender, EventArgs e)
        {
            //if twitch isnt enabled, we cant do anything internal so ignore anything Twitch related
            if (!_twitchStarted)
            {
                _bBBlog.Debug("Twitch not enabled, ignoring gifted subs checkbox");
                return;
            }
            _bBBlog.Debug($"eventsub websocket: {_twitchEventSub._eventSubWebsocketClient.SessionId}");
            if (TwitchGiftedSub.Checked)
            {
                _bBBlog.Info("This enables reading gifted subscription events");

                //just double check its not already enabled
                if (!_globalTwitchAPI.EventSubCheckSubscriptionGift)
                {
                    _bBBlog.Info("Twitch gifted subscriptions enabled.");
                    _twitchEventSub.EventSubHandleSubscriptionGift();
                    //set local eventhanlder for valid chat messages to trigger the bot
                    _twitchEventSub.OnESubSubscriptionGift += TwitchEventSub_OnESubGiftedSub;
                }
            }
            else
            {
                _bBBlog.Info("Twitch gifted subscriptions unchecked. Disabling event handler.");
                _twitchEventSub.OnESubSubscriptionGift -= TwitchEventSub_OnESubGiftedSub;
                await _twitchEventSub.EventSubStopSubscriptionGift();
            }
            TwitchEnableDisableFields();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void BBB_Load(object sender, EventArgs e)
        {
            _bBBlog.Info("BanterBrain Buddy started");
            //MessageBox.Show("This is a beta version of BanterBrain Buddy. Please report any bugs to the discord!", "BanterBrain Buddy Beta", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void ExitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {

            if (_globalTwitchAPI != null)
            {
                _globalTwitchAPI.StopHourlyAccessTokenCheck();

                _globalTwitchAPI = null;
            }
            if (_twitchEventSub != null)
            {
                await _twitchEventSub.EventSubStopAsync();
                _twitchEventSub = null;
            }
            this.Close();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchChannelPointCheckBox_Click(object sender, EventArgs e)
        {
            //if twitch isnt enabled, we cant do anything internal so ignore anything Twitch related
            if (!_twitchStarted)
            {
                _bBBlog.Debug("Twitch not enabled, ignoring channelpoint redemption checkbox");
                return;
            }
            if (TwitchChannelPointCheckBox.Checked)
            {
                if (TwitchCustomRewardName.Text.Length < 1)
                { //no custom reward name set, so we cant do anything
                    MessageBox.Show("You need to set a custom reward name in the settings to enable channel point redemptions", "Twitch error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _bBBlog.Error("You need to set a custom reward name in the settings to enable channel point redemptions");
                    TwitchChannelPointCheckBox.Checked = false;
                    return;
                }
                _bBBlog.Info("This enables reading channel point redemption events");
                //just double check its not already enabled
                if (!_globalTwitchAPI.EventSubCheckChannelPointRedemption)
                {
                    _bBBlog.Info("Twitch channel points enabled, calling EventSubHandleChannelPoints");
                    _twitchEventSub.EventSubHandleChannelPointRedemption(TwitchCustomRewardName.Text);
                    _twitchEventSub.OnESubChannelPointRedemption += TwitchEventSub_OnESubChannelPointRedemption;
                }
            }
            else
            {
                _bBBlog.Info("Twitch channel points unchecked. Disabling event handler.");
                _twitchEventSub.OnESubChannelPointRedemption -= TwitchEventSub_OnESubChannelPointRedemption;
                await _twitchEventSub.EventSubStopChannelPointRedemption();
            }
            TwitchEnableDisableFields();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void SeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _bBBlog.Debug("BanterBrain Buddy leaving main form, saving settings (just in case)");
            SaveALLSettings();
            Unsubscribe();
            if (_twitchEventSub != null)
            {
                //twitch is running so....lets make sure the settings form knows this
                isTwitchRunning = true;
            }
            else
                isTwitchRunning = false;

            SettingsForm ShowSettingsForm = new();
            ShowSettingsForm.FormClosing += BBB_Test_FormClosing;
            ShowSettingsForm.ShowDialog();
        }

        //handle the settings form closing and be sure to reload the new settings
        [SupportedOSPlatform("windows10.0.10240")]
        private async void BBB_Test_FormClosing(object sender, FormClosingEventArgs e)
        {
            BBBTabs.Enabled = false;
            menuStrip1.Enabled = false;
            UpdateTextLog("Settings closed. We loading settings..be patient!\r\n");
            _bBBlog.Info("Settings form closed. We should load the new settings!");
            //we should clear the global TTS/STT and reload the possible new API settings
            //we cant do this for twitch though. 
            if (_azureSpeech != null)
                _azureSpeech.AzureAPIKey = UserSettingsManager.Settings.AzureAPIKeyTextBox;
            if (_elevenLabsApi != null)
                _elevenLabsApi.ElevelLabsAPIKey = UserSettingsManager.Settings.ElevenLabsAPIkey;
            if (_openAI != null)
                _openAI.OpenAIAPIKey = UserSettingsManager.Settings.GPTAPIKey;
            STTSelectedComboBox.Items.Clear();
            await LoadPersonas();
            LoadTwitchLLMLanguageFile();
            await CheckConfiguredSTTProviders();
            await CheckConfiguredLLMProviders();
            await LoadSettings();
            await SetSelectedSTTProvider();
            SetSelectedLLMProvider();
            BBBTabs.Enabled = true;
            menuStrip1.Enabled = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void BBB_NotableViewersFormClosing(object sender, FormClosingEventArgs e)
        {
            _bBBlog.Info("Notable viewers form closed. We should load the new settings!");
            LoadTwitchNotableViewers();
        }

        private void BBB_VisibleChanged(object sender, EventArgs e)
        {
            _bBBlog.Info("BanterBrain Buddy visible changed. to test if closing form2 is done");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void BroadcasterSelectedPersonaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (UserSettingsManager.Settings.MainSelectedPersona != BroadcasterSelectedPersonaComboBox.SelectedItem.ToString())
            {
                _bBBlog.Info("Broadcaster selected persona changed to: " + BroadcasterSelectedPersonaComboBox.SelectedItem.ToString());
                UserSettingsManager.Settings.MainSelectedPersona = BroadcasterSelectedPersonaComboBox.SelectedItem.ToString();
                UserSettingsManager.SaveSettings();
            }
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchStartButton_Click(object sender, EventArgs e)
        {
            //lets at least save the settings before we start
            SaveALLSettings();
            if (!_twitchAPIVerified)
            {
                MessageBox.Show("Twitch API key is invalid. Please check the settings.", "Twitch API error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bBBlog.Error("Twitch API key is invalid. Please check the settings.");
                return;
            }
            //we also need at least one subsription event enabled else its useless
            if (!TwitchReadChatCheckBox.Checked && !TwitchCheerCheckBox.Checked && !TwitchSubscribed.Checked && !TwitchGiftedSub.Checked && !TwitchChannelPointCheckBox.Checked)
            {
                MessageBox.Show("You need to enable at least one event to listen to. Please check the settings.", "Twitch error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bBBlog.Error("You need to enable at least one event to listen to. Please check the settings.");
                return;
            }


            //if the checkbox is checked, lets enable the timer to check the token every hour
            //and start the eventsub server
            //TODO: only allow this after both API and EventSub are tested and working
            if (TwitchStartButton.Text == "Start")
            {
                _bBBlog.Info("Twitch enabled. Starting API and EventSub");
                SetTwitchValidateBroadcasterTokenTimer(true);
                SetTwitchValidateBotTokenTimer();
                //we need to wait till the eventsub is started before we can enable the fields
                while (!_twitchValidateBroadcasterCheckStarted)
                {
                    await Task.Delay(500);
                }

                // we need to call this when the bot account API is entered
                if (UserSettingsManager.Settings.TwitchBotAuthKey.Length > 1)
                {
                    _bBBlog.Info("Twitch bot API key entered, starting bot validation");
                    while (!_twitchValidateBotCheckStarted)
                    {
                        await Task.Delay(500);
                    }
                }


                _bBBlog.Debug("Twitch eventsub started, setting button text");
                TwitchStartButton.Text = "Stop";
                TwitchEnableDisableFields();
            }
            else
            { //turning off Twitch
                _bBBlog.Info("Twitch disabled. Stopping timer and clearing token. Stopping Websocket client.");
                UpdateTextLog("Twitch disabled. Stopping timer and clearing token. Stopping Websocket client.\r\n");
                TwitchStartButton.Text = "Start";

                if (_globalTwitchAPI != null)
                {
                    _globalTwitchAPI.StopHourlyAccessTokenCheck();
                    if (_globalTwitchAPI != null)
                        await _globalTwitchAPI.EventSubStopAsync();

                    if (_twitchEventSub != null)
                        await _twitchEventSub.EventSubStopAsync();
                    _twitchValidateBroadcasterCheckStarted = false;
                    _twitchValidateBotCheckStarted = false;
                    TwitchAPIStatusTextBox.Text = "DISABLED";
                    TwitchAPIStatusTextBox.BackColor = Color.Red;
                    TwitchEventSubStatusTextBox.Text = "DISABLED";
                    TwitchEventSubStatusTextBox.BackColor = Color.Red;
                    TwitchEnableDisableFields();
                    _twitchEventSub = null;
                }
            }
            TwitchEnableDisableFields();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchEnableDisableFields()
        {
            _bBBlog.Info($"Twitch enable/disable fields. Twitch started: {_twitchValidateBroadcasterCheckStarted}");
            //we need to disable the ability to change settings of eventlisteners that are active
            if (_twitchValidateBroadcasterCheckStarted)
            {
                if (TwitchReadChatCheckBox.Checked)
                {
                    _bBBlog.Info("Twitch read chat enabled, disabling settings");
                    TwitchNeedsSubscriber.Enabled = false;
                    TwitchCommandTrigger.Enabled = false;
                    TwitchChatCommandDelay.Enabled = false;
                    TwitchChatPersonaComboBox.Enabled = false;
                    TwitchChatTTSEverythingRadioButton.Enabled = false;
                    TwitchChatTTSResponseOnlyRadioButton.Enabled = false;
                }
                else
                {
                    TwitchNeedsSubscriber.Enabled = true;
                    TwitchCommandTrigger.Enabled = true;
                    TwitchChatCommandDelay.Enabled = true;
                    TwitchChatPersonaComboBox.Enabled = true;
                    TwitchChatTTSEverythingRadioButton.Enabled = true;
                    TwitchChatTTSResponseOnlyRadioButton.Enabled = true;
                }

                if (TwitchCheerCheckBox.Checked)
                {
                    _bBBlog.Info("Twitch cheers enabled, disabling settings");
                    TwitchMinBits.Enabled = false;
                    TwitchCheeringPersonaComboBox.Enabled = false;
                    TwitchCheeringTTSEverythingRadioButton.Enabled = false;
                    TwitchCheeringTTSResponseOnlyRadioButton.Enabled = false;
                }
                else
                {
                    TwitchMinBits.Enabled = true;
                    TwitchCheeringPersonaComboBox.Enabled = true;
                    TwitchCheeringTTSEverythingRadioButton.Enabled = true;
                    TwitchCheeringTTSResponseOnlyRadioButton.Enabled = true;
                }

                if (TwitchSubscribed.Checked || TwitchGiftedSub.Checked)
                {
                    _bBBlog.Info("Twitch subscriptions enabled, disabling settings");
                    TwitchSubscriptionPersonaComboBox.Enabled = false;
                    TwitchSubscriptionTTSEverythingRadioButton.Enabled = false;
                    TwitchSubscriptionTTSResponseOnlyRadioButton.Enabled = false;
                }
                else
                {
                    TwitchSubscriptionPersonaComboBox.Enabled = true;
                    TwitchSubscriptionTTSEverythingRadioButton.Enabled = true;
                    TwitchSubscriptionTTSResponseOnlyRadioButton.Enabled = true;
                }

                if (TwitchChannelPointCheckBox.Checked)
                {
                    _bBBlog.Info("Twitch channel points enabled, disabling settings");
                    TwitchChannelPointPersonaComboBox.Enabled = false;
                    TwitchCustomRewardName.Enabled = false;
                    TwitchChannelPointTTSEverythingRadioButton.Enabled = false;
                    TwitchChannelPointTTSResponseOnlyRadioButton.Enabled = false;
                }
                else
                {
                    TwitchChannelPointPersonaComboBox.Enabled = true;
                    TwitchCustomRewardName.Enabled = true;
                    TwitchChannelPointTTSEverythingRadioButton.Enabled = true;
                    TwitchChannelPointTTSResponseOnlyRadioButton.Enabled = true;
                }
            }
            else
            {
                TwitchNeedsSubscriber.Enabled = true;
                TwitchCommandTrigger.Enabled = true;
                TwitchChatCommandDelay.Enabled = true;
                TwitchChatPersonaComboBox.Enabled = true;
                TwitchMinBits.Enabled = true;
                TwitchCheeringPersonaComboBox.Enabled = true;
                TwitchSubscriptionPersonaComboBox.Enabled = true;
                TwitchChannelPointPersonaComboBox.Enabled = true;
                TwitchCustomRewardName.Enabled = true;
                TwitchChannelPointTTSEverythingRadioButton.Enabled = true;
                TwitchChannelPointTTSResponseOnlyRadioButton.Enabled = true;
                TwitchSubscriptionTTSEverythingRadioButton.Enabled = true;
                TwitchSubscriptionTTSResponseOnlyRadioButton.Enabled = true;
                TwitchCheeringTTSEverythingRadioButton.Enabled = true;
                TwitchCheeringTTSResponseOnlyRadioButton.Enabled = true;
                TwitchChatTTSEverythingRadioButton.Enabled = true;
                TwitchChatTTSResponseOnlyRadioButton.Enabled = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchEnableCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            //first we check if theres actually info in the API settings or else lets not even bother
            if (UserSettingsManager.Settings.TwitchAccessToken.Length < 1 && UserSettingsManager.Settings.TwitchChannel.Length < 1 && UserSettingsManager.Settings.TwitchBotName.Length < 1)
            {
                MessageBox.Show("Twitch API settings are not filled in. Please check the settings.", "Twitch API error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bBBlog.Error("Twitch API settings are not filled in. Please check the settings.");
                return;
            }
            if (TwitchEnableCheckbox.Checked)
            {
                _bBBlog.Info("Twitch enabled. Enabling all settings");
                UserSettingsManager.Settings.TwitchEnableCheckbox = true;
                TwitchStartButton.Enabled = true;
                TwitchChatTriggerSettings.Enabled = true;
                TwitchCheerSettings.Enabled = true;
                TwitchSubscriberSettings.Enabled = true;
                TwitchChannelPointsSettings.Enabled = true;
                TwitchAutoStart.Enabled = true;
                TwitchSoundsSettings.Enabled = true;
                TwitchResponseSettings.Enabled = true;
            }
            else
            {
                _bBBlog.Info("Twitch disabled. Stopping API and EventSub");
                //do same as stop also disable stuff
                UserSettingsManager.Settings.TwitchEnableCheckbox = false;
                TwitchStartButton.Enabled = false;
                TwitchChatTriggerSettings.Enabled = false;
                TwitchCheerSettings.Enabled = false;
                TwitchSubscriberSettings.Enabled = false;
                TwitchChannelPointsSettings.Enabled = false;
                TwitchAutoStart.Enabled = false;
                TwitchSoundsSettings.Enabled = false;
                TwitchResponseSettings.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchChatPersonaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BBBTabs.SelectedTab.Name.Equals("StreamingSettingsTab"))
            {
                _bBBlog.Info("Twitch chat persona changed to: " + TwitchChatPersonaComboBox.Text);
                UserSettingsManager.Settings.TwitchChatPersona = TwitchChatPersonaComboBox.Text;
                UserSettingsManager.SaveSettings();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchSubscriptionPersonaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BBBTabs.SelectedTab.Name.Equals("StreamingSettingsTab"))
            {
                _bBBlog.Info("Twitch subscription persona changed to: " + TwitchSubscriptionPersonaComboBox.Text);
                UserSettingsManager.Settings.TwitchSubscriptionPersona = TwitchSubscriptionPersonaComboBox.Text;
                UserSettingsManager.SaveSettings();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchCheeringPersonaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BBBTabs.SelectedTab.Name.Equals("StreamingSettingsTab"))
            {
                _bBBlog.Info("Twitch cheering persona changed to: " + TwitchCheeringPersonaComboBox.Text);
                UserSettingsManager.Settings.TwitchCheeringPersona = TwitchCheeringPersonaComboBox.Text;
                UserSettingsManager.SaveSettings();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchChannelPointPersonaComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BBBTabs.SelectedTab.Name.Equals("StreamingSettingsTab"))
            {
                _bBBlog.Info("Twitch channel point persona changed to: " + TwitchChannelPointPersonaComboBox.Text);
                UserSettingsManager.Settings.TwitchChannelPointPersona = TwitchChannelPointPersonaComboBox.Text;
                UserSettingsManager.SaveSettings();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void LLMResponseSelecter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UserSettingsManager.Settings.SelectedLLM != LLMResponseSelecter.Text)
            {
                _bBBlog.Info("Selected LLM changed to: " + LLMResponseSelecter.Text);
                UserSettingsManager.Settings.SelectedLLM = LLMResponseSelecter.Text;
                UserSettingsManager.SaveSettings();
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private static void AddFilesToDropdown(ComboBox box)
        {
            string soundDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/sounds";
            string[] files = Directory.GetFiles(soundDir, "*.wav");
            foreach (string file in files)
            {
                box.Items.Add(Path.GetFileName(file));
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchChatSoundTextBox_Click(object sender, EventArgs e)
        {
            TwitchChatSoundTextBox.Items.Clear();
            AddFilesToDropdown(TwitchChatSoundTextBox);

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchChatSoundCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (TwitchChatSoundCheckBox.Checked)
            {
                TwitchChatSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchChatSoundTextBox.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchChannelSoundCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (TwitchChannelSoundCheckBox.Checked)
            {
                TwitchChannelSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchChannelSoundTextBox.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchCheeringSoundCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (TwitchCheeringSoundCheckBox.Checked)
            {
                TwitchCheeringSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchCheeringSoundTextBox.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchSubscriptionSoundCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (TwitchSubscriptionSoundCheckBox.Checked)
            {
                TwitchSubscriptionSoundTextBox.Enabled = true;
            }
            else
            {
                TwitchSubscriptionSoundTextBox.Enabled = false;
            }
        }

        private void TwitchChatSoundSelectButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\sounds");
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Could not open the sounds folder: " + ex.Message);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchResponseToChatCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (TwitchResponseToChatCheckBox.Checked)
            {
                TwitchResponseToChatDelayTextBox.Enabled = true;
                UserSettingsManager.Settings.TwitchResponseToChatCheckBox = true;
                UserSettingsManager.SaveSettings();
            }
            else
            {
                TwitchResponseToChatDelayTextBox.Enabled = false;
                UserSettingsManager.Settings.TwitchResponseToChatCheckBox = false;
                UserSettingsManager.SaveSettings();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchChatCommandDelay_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if the input is not a digit or control (like backspace)
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;  // Ignore the input
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchResponseToChatDelayTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if the input is not a digit or control (like backspace)
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;  // Ignore the input
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchMinBits_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if the input is not a digit or control (like backspace)
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;  // Ignore the input
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchResponseToChatDelayTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextBox currenttb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchMinBits_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextBox currenttb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchChatCommandDelay_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextBox currenttb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
            else
            {
                UserSettingsManager.Settings.TwitchChatCommandDelay = ParseIntOrDefault(currenttb.Text, UserSettingsManager.Settings.TwitchChatCommandDelay);
                UserSettingsManager.SaveSettings();
            }

        }

        private void LogfileDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //the log dir can be empty on old settings files, fall back to the default location
            string logDir = UserSettingsManager.Settings.LogDir;
            if (string.IsNullOrWhiteSpace(logDir))
                logDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\logs";
            _bBBlog.Debug("Opening log directory: " + logDir);
            try
            {
                Process.Start("explorer.exe", logDir);
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Could not open the log folder: " + ex.Message);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void STTSelectedComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (UserSettingsManager.Settings.STTSelectedProvider != STTSelectedComboBox.Text)
            {
                _bBBlog.Info("STT provider changed to: " + STTSelectedComboBox.Text);
                UserSettingsManager.Settings.STTSelectedProvider = STTSelectedComboBox.Text;
                UserSettingsManager.SaveSettings();
            }
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private void BBBTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bBBlog.Debug("Tab changed to: " + BBBTabs.SelectedTab.Name);
            _bBBlog.Debug("Leaving Main Window settings, saving");
            SaveALLSettings();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchDelayMessageTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextBox currenttb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
            else
            {
                UserSettingsManager.Settings.TwitchDelayMessageTextBox = currenttb.Text;
                UserSettingsManager.SaveSettings();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchDelayFinishToChatcCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (TwitchDelayFinishToChatcCheckBox.Checked)
            {
                TwitchDelayMessageTextBox.Enabled = true;
                UserSettingsManager.Settings.DelayFinishToChatcCheckBox = true;
                UserSettingsManager.SaveSettings();
            }
            else
            {
                TwitchDelayMessageTextBox.Enabled = false;
                UserSettingsManager.Settings.DelayFinishToChatcCheckBox = false;
                UserSettingsManager.SaveSettings();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void StreamerNameTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextBox currenttb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
            else
            {
                UserSettingsManager.Settings.StreamerNameTextBox = StreamerNameTextBox.Text;
                UserSettingsManager.SaveSettings();
            }
        }

        private void DownloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //spawn browser for github link
            var t = new Thread(() => Process.Start(new ProcessStartInfo("https://github.com/WhiskerWeirdo/BanterBrain-Buddy/releases/latest") { UseShellExecute = true }));
            t.Start();
            Thread.Sleep(100);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchLLMLanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TwitchLLMLanguageComboBox.Text.Length > 1)
            {
                UserSettingsManager.Settings.TwitchLLMLanguageComboBox = TwitchLLMLanguageComboBox.Text;
                UserSettingsManager.SaveSettings();
                UpdateTextLog("Twitch LLM language changed to: " + TwitchLLMLanguageComboBox.Text + "\r\n");
                _bBBlog.Info("Twitch LLM language changed to: " + TwitchLLMLanguageComboBox.Text);
            }

            if (TwitchLLMLanguageComboBox.Text == "Custom")
            {
                CustomResponseButton.Enabled = true;
            }
            else
            {
                CustomResponseButton.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void CustomResponseButton_Click(object sender, EventArgs e)
        {
            _bBBlog.Debug("BanterBrain Buddy leaving main form, saving settings (just in case)");
            SaveALLSettings();
            Unsubscribe();
            if (_twitchEventSub != null)
            {
                //twitch is running so....lets make sure the settings form knows this
                isTwitchRunning = true;
            }
            else
                isTwitchRunning = false;

            TwitchLLMCustomLanguage ShowTwitchLLMCustomLanguageForm = new();
            ShowTwitchLLMCustomLanguageForm.FormClosing += BBB_Test_FormClosing;
            ShowTwitchLLMCustomLanguageForm.ShowDialog();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void WordFilterButton_Click(object sender, EventArgs e)
        {
            _bBBlog.Debug("BanterBrain Buddy leaving main form, saving settings (just in case)");
            SaveALLSettings();
            Unsubscribe();

            WordFilterForm wordFilterForm = new();
            wordFilterForm.FormClosing += BBB_Test_FormClosing;
            wordFilterForm.ShowDialog();

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void BadWordFilterCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (TwitchBadWordFilterCheckBox.Checked)
            {
                _bBBlog.Info("Bad word filter enabled");
                UserSettingsManager.Settings.BadWordFilter = true;
                UserSettingsManager.SaveSettings();
            }
            else
            {
                _bBBlog.Info("Bad word filter disabled");
                UserSettingsManager.Settings.BadWordFilter = false;
                UserSettingsManager.SaveSettings();
            }
        }

        private void LLMStartNewConvo_Click(object sender, EventArgs e)
        {
            //aight here we call a new conversation i.e. reset the class of the existing GPT or Ollama conversation
            _ollamaLLM?.OllamaChatReset();
            _openAI?.ChatGPTChatReset();
            _groqLLM?.GroqChatReset();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void NotableViewersButton_Click(object sender, EventArgs e)
        {
            TwitchNotableViewers notableViewersForm = new();
            notableViewersForm.FormClosing += BBB_NotableViewersFormClosing;
            notableViewersForm.ShowDialog();

        }
    }
}
