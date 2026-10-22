using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BanterBrain_Buddy
{
    public partial class SettingsForm : Form
    {
        //set logger
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<Personas> personas = [];
        private List<AzureVoices> tTSAzureVoicesList = [];
        private List<NativeVoices> nativeRegionVoicesList = [];

        private TwitchAPIESub twitchTestEventSub;
        private bool twitchStartedTest;
        private bool twitchAPIVerified;
        private bool personaEdited;
        private ElLabs elevenLabsApi;
        private List<string> _elevenLabsVoicesList;
        private int previousPersonaSelectedIndex;

        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        //safe parses for textbox values, the test-voice path runs from an async void click handler so a bad value must never throw
        private static int ParseIntOrDefault(string value, int defaultValue)
        {
            if (int.TryParse(value, out var result))
                return result;
            _bBBlog.Warn($"Could not parse \"{value}\" to an int, using default {defaultValue}");
            return defaultValue;
        }

        private static float ParseFloatOrDefault(string value, float defaultValue)
        {
            if (float.TryParse(value, out var result))
                return result;
            _bBBlog.Warn($"Could not parse \"{value}\" to a float, using default {defaultValue}");
            return defaultValue;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public SettingsForm()
        {
            InitializeComponent();
            SetupTwitchKeyInfoLinks();
            DisablePersonaEventHandlers();
            GetAudioDevices();
            GetInstalledNativeSpeecRecognitionCultures();
            LoadSettings();
            MenuTreeView.ExpandAll();

            //we need to disable the Twitch settings if its running
            if (BBB.isTwitchRunning)
            {
                _bBBlog.Info("Twitch is running, disabling Twitch settings");
                TwitchPanel.Enabled = false;
            }
            else
            {
                _bBBlog.Info("Twitch is not running, enabling Twitch settings");
                TwitchPanel.Enabled = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void GetInstalledNativeSpeecRecognitionCultures()
        {
            foreach (RecognizerInfo config in SpeechRecognitionEngine.InstalledRecognizers())
            {
                NativeSpeechRecognitionLanguageComboBox.Items.Add(config.Culture);
                _bBBlog.Debug("Found native speech recognition language: " + config.Culture);
            }

            if (NativeSpeechRecognitionLanguageComboBox.Items.Count < 1)
            {
                _bBBlog.Error("No native speech recognition languages found");
                MessageBox.Show("No native speech recognition languages found. Be sure to install one!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //fill the audio input and output list boxes
        public void GetAudioDevices()
        {
            for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                SoundInputDevices.Items.Add(NAudio.Wave.WaveIn.GetCapabilities(i).ProductName);
            }

            for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
            {
                TTSAudioOutputComboBox.Items.Add(NAudio.Wave.WaveOut.GetCapabilities(i).ProductName);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void DisablePersonaEventHandlers()
        {
            _bBBlog.Debug("Disabling Persona screen eventhandlers");
            PersonaComboBox.SelectedValueChanged -= PersonaComboBox_SelectedValueChanged;
            PersonaRoleTextBox.TextChanged -= PersonaRoleTextBox_TextChanged;
            TTSProviderComboBox.SelectedValueChanged -= TTSProviderComboBox_SelectedValueChanged;
            TTSOutputVoice.SelectedValueChanged -= TTSOutputVoice_SelectedValueChanged;
            TTSOutputVoiceOption1.SelectedIndexChanged -= TTSOutputVoiceOption1_SelectedIndexChanged;
            TTSOutputVoiceOption1.TextChanged -= TTSOutputVoiceOption1_TextChanged;
            TTSOutputVoiceOption2.TextChanged -= TTSOutputVoiceOption2_TextChanged;
            TTSOutputVoiceOption3.TextChanged -= TTSOutputVoiceOption3_TextChanged;
            VolumeTrackBar.ValueChanged -= VolumeTrackBar_ValueChanged;
            RateTrackBar.ValueChanged -= RateTrackBar_ValueChanged;
            PitchTrackBar.ValueChanged -= PitchTrackBar_ValueChanged;

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void EnablePersonaEventHandlers(
            )
        {
            _bBBlog.Debug("Enabling Persona screen eventhandlers");
            PersonaComboBox.SelectedValueChanged += PersonaComboBox_SelectedValueChanged;
            PersonaRoleTextBox.TextChanged += PersonaRoleTextBox_TextChanged;
            TTSProviderComboBox.SelectedValueChanged += TTSProviderComboBox_SelectedValueChanged;
            TTSOutputVoice.SelectedValueChanged += TTSOutputVoice_SelectedValueChanged;
            TTSOutputVoiceOption1.SelectedIndexChanged += TTSOutputVoiceOption1_SelectedIndexChanged;
            TTSOutputVoiceOption1.TextChanged += TTSOutputVoiceOption1_TextChanged;
            TTSOutputVoiceOption2.TextChanged += TTSOutputVoiceOption2_TextChanged;
            TTSOutputVoiceOption3.TextChanged += TTSOutputVoiceOption3_TextChanged;
            VolumeTrackBar.ValueChanged += VolumeTrackBar_ValueChanged;
            RateTrackBar.ValueChanged += RateTrackBar_ValueChanged;
            PitchTrackBar.ValueChanged += PitchTrackBar_ValueChanged;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ShowPanels(e.Node.Name);
        }

        //show panels based on selected node and hide the others
        [SupportedOSPlatform("windows10.0.10240")]
        private void PanelVisible(string panelName)
        {
            foreach (var panel in splitContainer1.Panel2.Controls.OfType<Panel>())
            {
                //_bBBlog.Debug("Panel name: " + panel.Name);
                if (panel.Name == panelName)
                {
                    panel.Show();
                }
                else
                {
                    panel.Hide();
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void ShowPanels(string selectedNode)
        {

            switch (selectedNode)
            {
                case "VoiceSettings":
                    PanelVisible(MicrophonePanel.Name);
                    break;
                case "Microphone":
                    PanelVisible(MicrophonePanel.Name);
                    break;
                case "APISettings":
                    PanelVisible(AzurePanel.Name);
                    break;
                case "Azure":
                    PanelVisible(AzurePanel.Name);
                    break;
                case "Speaker":
                    PanelVisible(SpeakerPanel.Name);
                    break;
                case "OpenAIChatGPT":
                    PanelVisible(OpenAIChatGPTPanel.Name);
                    break;
                case "Twitch":
                    PanelVisible(TwitchPanel.Name);
                    break;
                case "ElevenLabs":
                    PanelVisible(ElevenLabsPanel.Name);
                    break;
                case "OllamaLLM":
                    PanelVisible(OllamaPanel.Name);
                    break;
                case "GroqLLM":
                    PanelVisible(GroqPanel.Name);
                    break;
                case "StreamingSettings":
                    PanelVisible(TwitchPanel.Name);
                    break;
                case "TwitchTriggers":
                    PanelVisible(TwitchPanel.Name);
                    break;
                case "NativeSpeech":
                    PanelVisible(NativeSpeechPanel.Name);
                    break;
                case "Personas":
                    PanelVisible(PersonasPanel.Name);
                    break;
                case "OBS":
                    PanelVisible(OBSPanel.Name);
                    break;
                default:
                    break;
            }
        }

        //The Twitch application Client ID is stored in settings.json (separate from usersettings.json)
        //because TwitchAPIESub reads it from there. These helpers read/write just that key while
        //preserving any other keys already present in the file.
        private static string TwitchSettingsJsonPath =>
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\settings.json";

        [SupportedOSPlatform("windows10.0.10240")]
        private string ReadTwitchClientId()
        {
            try
            {
                if (File.Exists(TwitchSettingsJsonPath))
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(TwitchSettingsJsonPath));
                    if (data != null && data.TryGetValue("TwitchAuthClientId", out string clientId)
                        && !string.IsNullOrWhiteSpace(clientId) && !clientId.Contains("insert clientid"))
                    {
                        return clientId;
                    }
                }
            }
            catch (Exception ex)
            {
                _bBBlog.Error("ReadTwitchClientId failed: " + ex.Message);
            }
            return "";
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void WriteTwitchClientId(string clientId)
        {
            try
            {
                string folder = Path.GetDirectoryName(TwitchSettingsJsonPath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                Dictionary<string, string> data = null;
                if (File.Exists(TwitchSettingsJsonPath))
                {
                    try { data = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(TwitchSettingsJsonPath)); }
                    catch { data = null; }
                }
                data ??= new Dictionary<string, string>();
                data["TwitchAuthClientId"] = clientId;

                File.WriteAllText(TwitchSettingsJsonPath, JsonConvert.SerializeObject(data, Formatting.Indented));
                _bBBlog.Info("Saved TwitchAuthClientId to settings.json");
            }
            catch (Exception ex)
            {
                _bBBlog.Error("WriteTwitchClientId failed: " + ex.Message);
            }
        }

        //Persist the Client ID typed in the Settings UI to settings.json immediately, so the user does
        //not have to close the window for the value to be written.
        [SupportedOSPlatform("windows10.0.10240")]
        private void SaveClientIdButton_Click(object sender, EventArgs e)
        {
            string clientId = TwitchClientIdTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("insert clientid"))
            {
                _bBBlog.Warn("Save Client ID clicked with an empty or placeholder value.");
                MessageBox.Show("Please enter a valid Twitch App Client ID before saving.\r\n\r\nYou can get one from https://dev.twitch.tv/console/apps",
                    "Twitch Client ID required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                WriteTwitchClientId(clientId);
                MessageBox.Show("Twitch Client ID saved to settings.json.", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _bBBlog.Error("SaveClientIdButton_Click failed: " + ex.Message);
                MessageBox.Show("Failed to save the Twitch Client ID: " + ex.Message, "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private const string TwitchDevPortalUrl = "https://dev.twitch.tv/console/apps";

        //The instructions label is a LinkLabel; register the developer portal URL as an actual clickable
        //link spanning its occurrence in the text (WinForms LinkLabel does not auto-detect URLs).
        [SupportedOSPlatform("windows10.0.10240")]
        private void SetupTwitchKeyInfoLinks()
        {
            int idx = TwitchKeyInfoLabel.Text.IndexOf(TwitchDevPortalUrl, StringComparison.Ordinal);
            if (idx >= 0)
            {
                TwitchKeyInfoLabel.Links.Clear();
                TwitchKeyInfoLabel.Links.Add(idx, TwitchDevPortalUrl.Length, TwitchDevPortalUrl);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchKeyInfoLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                if (e.Link?.LinkData is string url && !string.IsNullOrWhiteSpace(url))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Failed to open Twitch key info link: " + ex.Message);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void LoadSettings()
        {
            _bBBlog.Info("Loading settings");
            
            _bBBlog.Debug("Loading settings from new settings: " + UserSettingsManager.Settings.OllamaURI);
            //TwitchUsername.Text = UserSettingsManager.Settings.TwitchUsername;
            TwitchBroadcasterAccessToken.Text = UserSettingsManager.Settings.TwitchAccessToken;
            TwitchBroadcasterChannel.Text = UserSettingsManager.Settings.TwitchChannel;
            TwitchBotAuthKey.Text = UserSettingsManager.Settings.TwitchBotAuthKey;
            TwitchBotName.Text = UserSettingsManager.Settings.TwitchBotName;
            TwitchSendTextCheckBox.Checked = UserSettingsManager.Settings.TwitchSendTextCheckBox;
            SoundInputDevices.SelectedIndex = SoundInputDevices.FindStringExact(UserSettingsManager.Settings.VoiceInput);
            MicrophoneHotkeyEditbox.Text = UserSettingsManager.Settings.PTTHotkey;
            TTSAudioOutputComboBox.SelectedIndex = TTSAudioOutputComboBox.FindStringExact(UserSettingsManager.Settings.TTSAudioOutput);
            AzureAPIKeyTextBox.Text = UserSettingsManager.Settings.AzureAPIKeyTextBox;
            AzureRegionTextBox.Text = UserSettingsManager.Settings.AzureRegionTextBox;
            AzureLanguageComboBox.Text = UserSettingsManager.Settings.AzureLanguageComboBox;
            //restore the model list from the last fetch, so the selector stays filled across restarts
            if (UserSettingsManager.Settings.GPTModelListCache != null && UserSettingsManager.Settings.GPTModelListCache.Count > 0)
            {
                GPTModelComboBox.Items.Clear();
                foreach (string cachedModel in UserSettingsManager.Settings.GPTModelListCache)
                {
                    GPTModelComboBox.Items.Add(cachedModel);
                }
            }
            GPTModelComboBox.SelectedIndex = GPTModelComboBox.FindStringExact(UserSettingsManager.Settings.GPTModel);
            //older settings files may still contain a legacy model name that is no longer in the list
            if (GPTModelComboBox.SelectedIndex < 0)
            {
                _bBBlog.Info("Saved GPT model not found in list, selecting default");
                GPTModelComboBox.SelectedIndex = 0;
            }
            GPTAPIKeyTextBox.Text = UserSettingsManager.Settings.GPTAPIKey;
            GPTMaxTokensTextBox.Text = UserSettingsManager.Settings.GPTMaxTokens.ToString();
            if (GPTMaxTokensTextBox.Text.Length < 1)
                GPTMaxTokensTextBox.Text = "100";
            GPTTemperatureTextBox.Text = UserSettingsManager.Settings.GPTTemperature.ToString();
            //Groq: older settings files have no values for these, stay null-safe and fall back to defaults
            GroqAPIKeyTextBox.Text = UserSettingsManager.Settings.GroqAPIKey ?? "";
            //restore the model list from the last fetch, so the selector stays filled across restarts
            if (UserSettingsManager.Settings.GroqModelListCache != null && UserSettingsManager.Settings.GroqModelListCache.Count > 0)
            {
                GroqModelComboBox.Items.Clear();
                foreach (string cachedModel in UserSettingsManager.Settings.GroqModelListCache)
                {
                    GroqModelComboBox.Items.Add(cachedModel);
                }
            }
            if (string.IsNullOrEmpty(UserSettingsManager.Settings.GroqModel))
            {
                _bBBlog.Info("No Groq model set, using the built-in defaults");
                GroqModelComboBox.SelectedIndex = 0;
                GroqTemperatureTextBox.Text = "0.5";
                GroqMaxTokensTextBox.Text = "100";
            }
            else
            {
                GroqModelComboBox.SelectedIndex = GroqModelComboBox.FindStringExact(UserSettingsManager.Settings.GroqModel);
                //a model loaded from the Groq API is not in the built-in list
                if (GroqModelComboBox.SelectedIndex < 0)
                    GroqModelComboBox.SelectedIndex = 0;
                GroqTemperatureTextBox.Text = UserSettingsManager.Settings.GroqTemperature.ToString();
                GroqMaxTokensTextBox.Text = UserSettingsManager.Settings.GroqMaxTokens.ToString();
            }
            //show the levels cached for the selected model first, a saved effort like "Xhigh" only
            //exists in that list and would silently fall back to Auto when it is applied afterwards
            ApplyCachedGroqReasoningLevels(GroqModelComboBox.Text);
            //empty effort means "Auto (model default)"
            GroqReasoningComboBox.SelectedIndex = GroqReasoningComboBox.FindStringExact(UserSettingsManager.Settings.GroqReasoningEffortComboBox ?? "");
            if (GroqReasoningComboBox.SelectedIndex < 0)
            {
                GroqReasoningComboBox.SelectedIndex = 0;
            }
            ElevenlabsAPIKeyTextBox.Text = UserSettingsManager.Settings.ElevenLabsAPIkey;
            ElevenLabsModelComboBox.SelectedIndex = ElevenLabsModelComboBox.FindStringExact(UserSettingsManager.Settings.ElevenLabsModel);
            if (ElevenLabsModelComboBox.SelectedIndex == -1)
                ElevenLabsModelComboBox.SelectedIndex = 0;
            OllamaModelsComboBox.SelectedIndex = OllamaModelsComboBox.FindStringExact(UserSettingsManager.Settings.OllamaSelectedModel);
            OllamaResponseLengthComboBox.SelectedIndex = OllamaResponseLengthComboBox.FindStringExact(UserSettingsManager.Settings.OllamaResponseLengthComboBox);
            OllamaURITextBox.Text = UserSettingsManager.Settings.OllamaURI;
            UseOllamaLLMCheckBox.Checked = UserSettingsManager.Settings.UseOllamaLLMCheckBox;
            //stored as "thinking on/off" like the OpenAI checkbox this is shown inverted as "disable reasoning"
            OllamaNoReasoningCheckBox.Checked = !UserSettingsManager.Settings.OllamaThinkingCheckBox;
            //older settings files have no value for this, 0 means "use the Ollama server default"
            OllamaContextTokensTextBox.Text = (UserSettingsManager.Settings.OllamaContextTokens > 0 ? UserSettingsManager.Settings.OllamaContextTokens : 0).ToString();
            //the reasoning effort dropdown replaced the old disable-reasoning checkbox, a checked
            //box from an older settings file migrates to the lowest effort
            string reasoningEffort = UserSettingsManager.Settings.GPTReasoningEffortComboBox;
            if (string.IsNullOrEmpty(reasoningEffort) && UserSettingsManager.Settings.GPTNoReasoningCheckBox)
            {
                reasoningEffort = "Minimal";
            }
            //restore the levels probed earlier for the selected model, so the effort selector is
            //filled without pressing Load levels again
            ApplyCachedReasoningLevels(GPTModelComboBox.Text);
            GPTReasoningComboBox.SelectedIndex = GPTReasoningComboBox.FindStringExact(reasoningEffort);
            if (GPTReasoningComboBox.SelectedIndex < 0)
            {
                GPTReasoningComboBox.SelectedIndex = 0;
            }
            TwitchAuthServerConfig.SelectedIndex = TwitchAuthServerConfig.FindStringExact(UserSettingsManager.Settings.TwitchAuthServerConfig);
            //Twitch App Client ID is stored in settings.json, load it into the textbox
            TwitchClientIdTextBox.Text = ReadTwitchClientId();

            WebsourceServerEnable.Checked = UserSettingsManager.Settings.WebsourceServerEnable;
            //if empty set default
            if (UserSettingsManager.Settings.TwitchAuthServerConfig.Length < 1)
            {
                TwitchAuthServerConfig.Text = "http://localhost:8080";
            }
            WebsourceServer.Text = UserSettingsManager.Settings.WebsourceServer;
            //if empty set default
            if (UserSettingsManager.Settings.WebsourceServer.Length < 1)
            {
                WebsourceServer.Text = "http://localhost:9138";
            }
            if (UserSettingsManager.Settings.WhisperSpeechRecognitionComboBox.Length > 1)
            {
                WhisperSpeechRecognitionComboBox.SelectedIndex = WhisperSpeechRecognitionComboBox.FindStringExact(UserSettingsManager.Settings.WhisperSpeechRecognitionComboBox);
            }
            else
            {
                //ok nothing is set, so lets just select the first one by default
                _bBBlog.Info("No whisper speech recognition language set, selecting English");
                WhisperSpeechRecognitionComboBox.SelectedIndex = WhisperSpeechRecognitionComboBox.FindStringExact("English");
            }

            if (UserSettingsManager.Settings.NativeSpeechRecognitionLanguageComboBox.Length > 1)
            {
                NativeSpeechRecognitionLanguageComboBox.SelectedIndex = NativeSpeechRecognitionLanguageComboBox.FindStringExact(UserSettingsManager.Settings.NativeSpeechRecognitionLanguageComboBox);
            }
            else
            {
                //ok nothing is set, so lets just select the first one by default
                _bBBlog.Info("No native speech recognition language set, selecting the first one");
                NativeSpeechRecognitionLanguageComboBox.SelectedIndex = 0;
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (personaEdited)
            {
                _bBBlog.Debug("Personas panel hidden. we should save the persona's if anything changed");
                //ask if we need to save the persona
                var result = MessageBox.Show("Information changed. Do you want to save the persona?", "Save Persona", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    SavePersona_Click(sender, e);
                }
                personaEdited = false;
            }
            _bBBlog.Info("Settings form closing, saving settings");
            //only save if theres actual data to be saved
            //these can be empty
            UserSettingsManager.Settings.TwitchBotName = TwitchBotName.Text;
            UserSettingsManager.Settings.TwitchBotAuthKey = TwitchBotAuthKey.Text;
            //persist the Twitch App Client ID to settings.json if one was entered
            if (TwitchClientIdTextBox.Text.Length > 0)
                WriteTwitchClientId(TwitchClientIdTextBox.Text);
            //these cannot be empty
            if (TwitchBroadcasterAccessToken.Text.Length > 0)
                UserSettingsManager.Settings.TwitchAccessToken = TwitchBroadcasterAccessToken.Text;
            if (TwitchBroadcasterChannel.Text.Length > 0)
                UserSettingsManager.Settings.TwitchChannel = TwitchBroadcasterChannel.Text;
            UserSettingsManager.Settings.TwitchSendTextCheckBox = TwitchSendTextCheckBox.Checked;
            if (SoundInputDevices.Text.Length > 0)
                UserSettingsManager.Settings.VoiceInput = SoundInputDevices.Text;
            if (MicrophoneHotkeyEditbox.Text.Length > 0)
                UserSettingsManager.Settings.PTTHotkey = MicrophoneHotkeyEditbox.Text;
            if (TTSAudioOutputComboBox.Text.Length > 0)
                UserSettingsManager.Settings.TTSAudioOutput = TTSAudioOutputComboBox.Text;
            if (AzureAPIKeyTextBox.Text.Length > 0)
                UserSettingsManager.Settings.AzureAPIKeyTextBox = AzureAPIKeyTextBox.Text;
            if (AzureRegionTextBox.Text.Length > 0)
                UserSettingsManager.Settings.AzureRegionTextBox = AzureRegionTextBox.Text;
            if (AzureLanguageComboBox.Text.Length > 0)
                UserSettingsManager.Settings.AzureLanguageComboBox = AzureLanguageComboBox.Text;
            if (GPTModelComboBox.Text.Length > 0)
                UserSettingsManager.Settings.GPTModel = GPTModelComboBox.Text;
            if (GPTAPIKeyTextBox.Text.Length > 0)
                UserSettingsManager.Settings.GPTAPIKey = GPTAPIKeyTextBox.Text;
            if (GPTMaxTokensTextBox.Text.Length > 0)
                UserSettingsManager.Settings.GPTMaxTokens = ParseIntOrDefault(GPTMaxTokensTextBox.Text, UserSettingsManager.Settings.GPTMaxTokens);
            if (GPTTemperatureTextBox.Text.Length > 0)
                UserSettingsManager.Settings.GPTTemperature = ParseFloatOrDefault(GPTTemperatureTextBox.Text, UserSettingsManager.Settings.GPTTemperature);
            if (ElevenlabsAPIKeyTextBox.Text.Length > 0)
                UserSettingsManager.Settings.ElevenLabsAPIkey = ElevenlabsAPIKeyTextBox.Text;
            if (OllamaModelsComboBox.Text.Length > 0)
                UserSettingsManager.Settings.OllamaSelectedModel = OllamaModelsComboBox.Text;
            if (OllamaResponseLengthComboBox.Text.Length > 0)
                UserSettingsManager.Settings.OllamaResponseLengthComboBox = OllamaResponseLengthComboBox.Text;
            if (OllamaURITextBox.Text.Length > 0)
                UserSettingsManager.Settings.OllamaURI = OllamaURITextBox.Text;
            if (OllamaContextTokensTextBox.Text.Length > 0)
                UserSettingsManager.Settings.OllamaContextTokens = ParseIntOrDefault(OllamaContextTokensTextBox.Text, UserSettingsManager.Settings.OllamaContextTokens);
            if (GroqModelComboBox.Text.Length > 0)
                UserSettingsManager.Settings.GroqModel = GroqModelComboBox.Text;
            if (GroqAPIKeyTextBox.Text.Length > 0)
                UserSettingsManager.Settings.GroqAPIKey = GroqAPIKeyTextBox.Text;
            if (GroqMaxTokensTextBox.Text.Length > 0)
                UserSettingsManager.Settings.GroqMaxTokens = ParseIntOrDefault(GroqMaxTokensTextBox.Text, UserSettingsManager.Settings.GroqMaxTokens);
            if (GroqTemperatureTextBox.Text.Length > 0)
                UserSettingsManager.Settings.GroqTemperature = ParseFloatOrDefault(GroqTemperatureTextBox.Text, UserSettingsManager.Settings.GroqTemperature);
            if (NativeSpeechRecognitionLanguageComboBox.Text.Length > 1)
                UserSettingsManager.Settings.NativeSpeechRecognitionLanguageComboBox = NativeSpeechRecognitionLanguageComboBox.Text;
            if (WhisperSpeechRecognitionComboBox.Text.Length > 1)
                UserSettingsManager.Settings.WhisperSpeechRecognitionComboBox = WhisperSpeechRecognitionComboBox.Text;
            if (TwitchAuthServerConfig.Text.Length > 1)
                UserSettingsManager.Settings.TwitchAuthServerConfig = TwitchAuthServerConfig.Text;
            if (WebsourceServer.Text.Length > 1)
                UserSettingsManager.Settings.WebsourceServer = WebsourceServer.Text;
            UserSettingsManager.Settings.UseOllamaLLMCheckBox = UseOllamaLLMCheckBox.Checked;
            UserSettingsManager.Settings.OllamaThinkingCheckBox = !OllamaNoReasoningCheckBox.Checked;
            UserSettingsManager.Settings.GPTReasoningEffortComboBox = GPTReasoningComboBox.SelectedIndex > 0 ? GPTReasoningComboBox.Text : "";
            UserSettingsManager.Settings.GroqReasoningEffortComboBox = GroqReasoningComboBox.SelectedIndex > 0 ? GroqReasoningComboBox.Text : "";
            UserSettingsManager.Settings.WebsourceServerEnable = WebsourceServerEnable.Checked;
            UserSettingsManager.Settings.ElevenLabsModel = ElevenLabsModelComboBox.Text;
            UserSettingsManager.SaveSettings();

            //we should also close the EventSub client if it is running
            if (twitchStartedTest)
            {
                _bBBlog.Info("Closing EventSub client");
                await twitchTestEventSub.EventSubStopAsync();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void SoundInputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bBBlog.Info("Selected input device changed to " + SoundInputDevices.Text);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void PersonasPanel_VisibleChanged(object sender, EventArgs e)
        {
            //we dont need the eventhandlers when its becoming visible
            if (PersonasPanel.Visible)
            {
                DisablePersonaEventHandlers();
                //TTSProvider combobox needs to be filled with the available providers
                TTSProviderComboBox.Items.Clear();
                TTSProviderComboBox.Items.Add("Native");
                if (AzureAPIKeyTextBox.Text.Length > 0 && AzureRegionTextBox.Text.Length > 0)
                    TTSProviderComboBox.Items.Add("Azure");
                if (GPTAPIKeyTextBox.Text.Length > 0)
                    TTSProviderComboBox.Items.Add("OpenAI Whisper");
                if (ElevenlabsAPIKeyTextBox.Text.Length > 0)
                    TTSProviderComboBox.Items.Add("ElevenLabs");
                TTSProviderComboBox.SelectedIndex = 0;

                _bBBlog.Debug("Personas panel visible. we need to load the persona's and enable the eventhandlers");
                await LoadPersonas();
                //bound the wait, if the personas never load we must not hang here forever
                int personaWaitTries = 0;
                while (PersonaComboBox.Items.Count == 0 && personaWaitTries++ < 10)
                {
                    await Task.Delay(1000);
                }
                if (PersonaComboBox.Items.Count == 0)
                {
                    _bBBlog.Error("No personas available after waiting, skipping persona load");
                    PersonasPanel.Enabled = true;
                    return;
                }

                //prevent deleting of Default persona
                if (PersonaComboBox.Text == "Default")
                {
                    DeletePersona.Enabled = false;
                }
                else
                {
                    DeletePersona.Enabled = true;

                }

                PersonaComboBox_SelectedValueChanged(sender, e);
            }
            else
            {
                if (personaEdited)
                {
                    _bBBlog.Debug("Personas panel hidden. we should save the persona's if anything changed");
                    //ask if we need to save the persona
                    var result = MessageBox.Show("Information changed. Do you want to save the persona?", "Save Persona", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        SavePersona_Click(sender, e);
                    }
                }
                DisablePersonaEventHandlers();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task<bool> FillVoiceBoxes()
        {
            _bBBlog.Info("Fill voice boxes: " + TTSProviderComboBox.Text);
            if (TTSProviderComboBox.Text == "Native")
            {
                TTSOutputVoice.Text = "";
                TTSOutputVoiceOption1.Visible = false;
                TTSOption1Label.Visible = false;
                TTSOutputVoiceOption2.Text = "";
                TTSOutputVoiceOption2.Visible = false;
                TTSOption2Label.Visible = false;
                TTSOutputVoiceOption3.Text = "";
                TTSOutputVoiceOption3.Visible = false;
                TTSOption3Label.Visible = false;
                //clear and fill the option box with voices
                await TTSGetNativeVoices();
                TTSFillNativeVoicesList();
                PitchTrackBar.Enabled = true;
                RateTrackBar.Enabled = true;
                PitchTrackBar.Scroll += PitchTrackBar_ScrollSnapToTen;

                return true;
            }
            else if (TTSProviderComboBox.Text == "ElevenLabs")
            {
                TTSOutputVoice.Text = "";
                TTSOutputVoiceOption1.Visible = true;
                TTSOption1Label.Visible = true;
                TTSOutputVoiceOption1.DropDownStyle = ComboBoxStyle.Simple;
                TTSOption1Label.Text = "Similarity  (0-100)";
                TTSOutputVoiceOption2.Visible = true;
                TTSOutputVoiceOption2.DropDownStyle = ComboBoxStyle.Simple;
                TTSOption2Label.Visible = true;
                TTSOption2Label.Text = "Stability (0-100)";
                TTSOutputVoiceOption3.Visible = true;
                TTSOption3Label.Visible = true;
                TTSOutputVoiceOption3.DropDownStyle = ComboBoxStyle.Simple;
                TTSOption3Label.Text = "Style (0-100)";
                await TTSGetElevenLabsVoices();
                PitchTrackBar.Scroll -= PitchTrackBar_ScrollSnapToTen;
                PitchTrackBar.Enabled = false;
                PitchTrackBar.Value = 0;
                RateTrackBar.Enabled = false;
                RateTrackBar.Value = 0;
                return true;
            }
            else if (TTSProviderComboBox.Text == "Azure")
            {
                TTSOutputVoice.Text = "";
                TTSOutputVoiceOption1.Visible = true;
                TTSOption1Label.Visible = true;
                TTSOption1Label.Text = "Style";
                TTSOutputVoiceOption1.DropDownStyle = ComboBoxStyle.DropDownList;
                TTSOutputVoiceOption2.Visible = false;
                TTSOutputVoiceOption2.Text = "";
                TTSOption2Label.Visible = false;
                TTSOutputVoiceOption3.Visible = false;
                TTSOption3Label.Visible = false;
                TTSOutputVoiceOption3.Text = "";
                PitchTrackBar.Scroll -= PitchTrackBar_ScrollSnapToTen;
                PitchTrackBar.Enabled = true;
                RateTrackBar.Enabled = true;
                if (AzureAPIKeyTextBox.Text.Length > 0 && AzureRegionTextBox.Text.Length > 0)
                {
                    if (await TTSGetAzureVoices())
                    {
                        //fill the listboxes
                        TTSFillAzureVoicesList();
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    MessageBox.Show("Please enter your Azure API key and region", "Azure API error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (TTSProviderComboBox.Text == "OpenAI Whisper")
            {
                TTSOutputVoice.Text = "";
                TTSOutputVoiceOption1.Visible = false;
                TTSOption1Label.Visible = false;
                TTSOutputVoiceOption2.Visible = false;
                TTSOption2Label.Visible = false;
                TTSOutputVoiceOption3.Visible = false;
                TTSOption3Label.Visible = false;
                //these voices are static
                //alloy, echo, fable, onyx, nova, and shimmer
                TTSOutputVoice.Items.Clear();
                TTSOutputVoice.Items.Add("alloy");
                TTSOutputVoice.Items.Add("echo");
                TTSOutputVoice.Items.Add("fable");
                TTSOutputVoice.Items.Add("onyx");
                TTSOutputVoice.Items.Add("nova");
                TTSOutputVoice.Items.Add("shimmer");
                TTSOutputVoice.Sorted = true;
                PitchTrackBar.Enabled = false;
                RateTrackBar.Value = 0;
                RateTrackBar.Enabled = true;
                TTSOutputVoice.Text = TTSOutputVoice.Items[0].ToString();
                PitchTrackBar.Scroll -= PitchTrackBar_ScrollSnapToTen;
                return true;
            }
            return false;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task LoadPersonas()
        {
            PersonaComboBox.Items.Clear();
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
                    new() { Name = "Default", RoleText = "You are a cheeky streamer assistant with a silly personality.", VoiceProvider = "Native", VoiceName = tmpNativeVoice, VoiceOptions = [], Volume = 0, Rate = 0, Pitch = 0 }
                };
                string tmpString = JsonConvert.SerializeObject(newPersonas);
                try
                {
                    //the settings directory might not exist yet on a first start, StreamWriter throws without it
                    Directory.CreateDirectory(Path.GetDirectoryName(tmpFile));
                    using var sw = new StreamWriter(tmpFile, true);
                    sw.Write(tmpString);
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Could not create the personas file: " + ex.Message);
                    return;
                }
            }
            //load the file in both cases, a freshly created persona file still has to fill the
            //combobox otherwise the panel stays empty and the wait loop above never finishes
            _bBBlog.Debug("Loading personas from file into the list.");
            personas.Clear();
            PersonaComboBox.Items.Clear();
            try
            {
                using var sr = new StreamReader(tmpFile);
                var tmpString = sr.ReadToEnd();
                var tmpPersonas = JsonConvert.DeserializeObject<List<Personas>>(tmpString) ?? new List<Personas>();
                foreach (var persona in tmpPersonas)
                {
                    _bBBlog.Debug("Loading persona into available list: " + persona.Name);
                    personas.Add(persona);
                    PersonaComboBox.Items.Add(persona.Name);
                }
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Could not read the personas file: " + ex.Message);
            }
            DeletePersona.Enabled = true;
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private void NewPersona_Click(object sender, EventArgs e)
        {
            _bBBlog.Debug("New persona button clicked");
            //if the persona is changed we first need to check if the persona was edited if so we need to ask to save it
            if (personaEdited && PersonasPanel.Visible)
            {
                var result = MessageBox.Show("New Persona. Do you want to save the persona?", "Save Persona", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    SavePersona_Click(sender, e);

                }
                personaEdited = false;
            }
            PersonaRoleTextBox.Text = "";
            TTSProviderComboBox.Text = "";
            TTSOutputVoice.Text = "";
            PersonaComboBox.Text = "";
            DeletePersona.Enabled = false;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void SavePersona_Click(object sender, EventArgs e)
        {
            personaEdited = false;
            _bBBlog.Debug("Save persona button clicked");
            if (PersonaComboBox.Text == "")
            {
                MessageBox.Show("Please enter a name for the persona", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (PersonaRoleTextBox.Text == "")
            {
                MessageBox.Show("Please enter a role for the persona", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (TTSProviderComboBox.Text == "")
            {
                MessageBox.Show("Please select a TTS provider for the persona", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (TTSOutputVoice.Text == "")
            {
                MessageBox.Show("Please select a voice for the persona", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            //if the persona.name is already in the file, we need to update it not add
            //else we do add it
            bool personaExists = false;
            foreach (var persona in personas)
            {
                if (persona.Name == PersonaComboBox.Text)
                {
                    personaExists = true;
                    _bBBlog.Debug($"Persona {persona.Name} already exists, updating it");
                    persona.RoleText = PersonaRoleTextBox.Text;
                    persona.VoiceProvider = TTSProviderComboBox.Text;
                    persona.Rate = ParseIntOrDefault(TTSSpeedLevel.Text, persona.Rate);
                    persona.Volume = ParseIntOrDefault(TTSVoiceLevel.Text, persona.Volume);
                    persona.Pitch = ParseIntOrDefault(TTSPitchLevel.Text, persona.Pitch);

                    //we need to add the voiceID for the ElevenLabs API
                    if (TTSProviderComboBox.Text == "ElevenLabs")
                    {
                        persona.VoiceName = ElevenlabIDbyVoice(TTSOutputVoice.Text);
                    }
                    else
                    {
                        persona.VoiceName = TTSOutputVoice.Text;
                    }

                    persona.VoiceOptions.Clear();
                    if (TTSOutputVoiceOption1.Text != "")
                    {
                        persona.VoiceOptions.Add(TTSOutputVoiceOption1.Text);
                    }
                    if (TTSOutputVoiceOption2.Text != "")
                    {
                        persona.VoiceOptions.Add(TTSOutputVoiceOption2.Text);
                    }
                    if (TTSOutputVoiceOption3.Text != "")
                    {
                        persona.VoiceOptions.Add(TTSOutputVoiceOption3.Text);
                    }
                }
            }
            if (!personaExists)
            {
                _bBBlog.Debug("Persona does not exist, adding it");

                var tmpVoiceOptions = new List<string>();
                if (TTSOutputVoiceOption1.Text != "")
                {
                    tmpVoiceOptions.Add(TTSOutputVoiceOption1.Text);
                }

                if (TTSProviderComboBox.Text == "ElevenLabs")
                {
                    tmpVoiceOptions.Add(TTSOutputVoiceOption2.Text);
                    tmpVoiceOptions.Add(TTSOutputVoiceOption3.Text);
                }
                personas.Add(new Personas { Name = PersonaComboBox.Text, RoleText = PersonaRoleTextBox.Text, VoiceProvider = TTSProviderComboBox.Text, VoiceName = TTSOutputVoice.Text, VoiceOptions = tmpVoiceOptions });

            }
            //and write the file
            var tmpFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\personas.json";
            _bBBlog.Debug("Writing personas to file: " + personas.Count);
            using (var sw = new StreamWriter(tmpFile, false))
            {
                sw.Write(JsonConvert.SerializeObject(personas));
            }
            await LoadPersonas();
            personaEdited = false;
            SavePersona.Enabled = false;
            if (PersonaComboBox.Text == "Default")
                DeletePersona.Enabled = false;
            return;
        }

        //finds the voiceID for the selected voice and adds it to the string
        //so that we have both the ID and the voice name in a string that can be stored
        [SupportedOSPlatform("windows10.0.10240")]
        private string ElevenlabIDbyVoice(string voiceName)
        {
            foreach (var elevenLabsVoice in _elevenLabsVoicesList)
            {
                //we find the voiceID for the selected voice and add it to the string
                //so we save both the ID and the name
                string[] tmpVoice = elevenLabsVoice.Split(";");
                if (tmpVoice[1] == voiceName)
                {
                    _bBBlog.Debug("Adding voiceID to persona: " + elevenLabsVoice);
                    return tmpVoice[0] + ";" + tmpVoice[1];
                }
            }
            return null;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSGetNativeVoices()
        {
            _ = new NativeSpeech();
            nativeRegionVoicesList = await NativeSpeech.TTSNativeGetVoices();
            if (nativeRegionVoicesList == null)
            {
                MessageBox.Show("Problem retreiving Native voicelist. Do you have any native voices installed?", "Native No voices", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _bBBlog.Info($"Found {nativeRegionVoicesList.Count} voices");
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSGetElevenLabsVoices()
        {
            elevenLabsApi ??= new(ElevenlabsAPIKeyTextBox.Text);

            _elevenLabsVoicesList = await elevenLabsApi.TTSGetElevenLabsVoices();
            if (_elevenLabsVoicesList == null)
            {
                _bBBlog.Debug("ElevenLabs API error, can be a timeout lets try once more");
                await Task.Delay(2000);
                _elevenLabsVoicesList = await elevenLabsApi.TTSGetElevenLabsVoices();
                if (_elevenLabsVoicesList == null)
                {
                    _bBBlog.Error("ElevenLabs API error, can be a timeout");
                    MessageBox.Show("Problem retreiving ElevenLabs voicelist. Is your API key still valid or was it a timeout? Cycle back to try again", "ElevenLabs No voices", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    TTSOutputVoice.Items.Clear();
                    return;
                }
            }
            else
            {
                _bBBlog.Info($"Found {_elevenLabsVoicesList.Count} voices");
            }
            int tmpIndex = TTSOutputVoice.SelectedIndex;
            TTSOutputVoice.Items.Clear();
            foreach (var elevenLabsVoice in _elevenLabsVoicesList)
            {
                _bBBlog.Debug("Adding voice: " + elevenLabsVoice);
                //we only want to add the name, not the id
                string[] tmpVoice = elevenLabsVoice.Split(";");
                if (tmpVoice.Length > 1)
                {
                    TTSOutputVoice.Items.Add(tmpVoice[1]);
                    TTSOutputVoice.SelectedIndex = 0;
                }
                else
                    TTSOutputVoice.Items.Add(tmpVoice[0]);
                TTSOutputVoice.SelectedIndex = TTSOutputVoice.Items.IndexOf(tmpVoice[0]);
            }
            TTSOutputVoice.Sorted = true;
            //the API can return an empty voice list, picking item 0 would throw
            if (TTSOutputVoice.Items.Count > 0)
            {
                TTSOutputVoice.SelectedIndex = 0;
                TTSOutputVoice.Text = TTSOutputVoice.Items[0].ToString();
            }
            if (TTSOutputVoiceOption2.Text.Length < 1)
            {
                TTSOutputVoiceOption1.Text = "100";
                TTSOutputVoiceOption2.Text = "100";
                TTSOutputVoiceOption3.Text = "0";
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSFillNativeVoicesList()
        {
            _bBBlog.Info("Fill Native voice list");
            //no need to clear we only add if we find new things
            TTSOutputVoice.Items.Clear();
            foreach (var nativeVoice in nativeRegionVoicesList)
            {
                string OutputVoice = nativeVoice.Name + "-" + nativeVoice.Gender + "-" + nativeVoice.Culture;
                TTSOutputVoice.Items.Add(OutputVoice);
            }
            TTSOutputVoice.Sorted = true;
            //a machine without installed voices gives an empty list
            if (TTSOutputVoice.Items.Count > 0)
            {
                TTSOutputVoice.SelectedIndex = 0;
                TTSOutputVoice.Text = TTSOutputVoice.Items[0].ToString();
            }
            TTSOutputVoiceOption1.Text = "";
        }


        [SupportedOSPlatform("windows10.0.10240")]
        /// <summary>
        /// This function gets the list of voices available in the Azure TTS API
        /// </summary>
        private async Task<bool> TTSGetAzureVoices()
        {
            //only bother if the two fields are not empty or not "placeholder"
            if ((AzureAPIKeyTextBox.Text.Length < 1) || (AzureRegionTextBox.Text.Length < 1))
            {
                _bBBlog.Error("API Key or region cannot be empty!");
                MessageBox.Show("API Key or region cannot be empty!", "Azure TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                PersonasPanel.Enabled = true;
                if (PersonasPanel.Visible)
                {
                    EnablePersonaEventHandlers();
                }
                return false;
            }

            AzureSpeechAPI AzureSpeech = new(AzureAPIKeyTextBox.Text, AzureRegionTextBox.Text, AzureLanguageComboBox.Text);
            tTSAzureVoicesList = await AzureSpeech.TTSGetAzureVoices();
            if (tTSAzureVoicesList == null)
            {
                MessageBox.Show("Problem retreiving Azure API voicelist. Is your API key or subscription information still valid?", "Azure API Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                PersonasPanel.Enabled = true;
                if (PersonasPanel.Visible)
                {
                    EnablePersonaEventHandlers();
                }
                return false;
            }
            else
            {
                _bBBlog.Info($"Found {tTSAzureVoicesList.Count} voices");
                return true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        /// <summary>
        /// This function fills the Azure voice list in the GUI TTSOutputVoice
        /// </summary>
        private void TTSFillAzureVoicesList()
        {
            _bBBlog.Info("Fill Azure voice list");

            // Locale, Gender, Localname
            TTSOutputVoice.Items.Clear();
            foreach (var azureRegionVoice in tTSAzureVoicesList)
            {
                TTSOutputVoice.Items.Add(azureRegionVoice.LocaleDisplayname + "-" + azureRegionVoice.Gender + "-" + azureRegionVoice.LocalName);
            }
            TTSOutputVoice.Sorted = true;
            //select the first item, to have at least something, the voice list can be empty
            if (TTSOutputVoice.Items.Count > 0)
            {
                TTSOutputVoice.SelectedIndex = 0;
                TTSOutputVoice.Text = TTSOutputVoice.Items[0].ToString();
            }
            TTSOutputVoiceOption1.Text = "";
            TTSAzureFillOptions(TTSOutputVoice.Text);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //Azure Text-To-Speach
        private async Task TTSAzureSpeakToOutput(string TextToSpeak)
        {
            _bBBlog.Info("Azure TTS called with text, seting up");
            AzureSpeechAPI azureSpeechAPI = new(AzureAPIKeyTextBox.Text, AzureRegionTextBox.Text, AzureLanguageComboBox.Text);

            //set the output voice, gender and locale, and the style
            _bBBlog.Info("Setting up Azure TTS: " + ParseIntOrDefault(TTSVoiceLevel.Text, 0));
            await azureSpeechAPI.AzureTTSInit(TTSOutputVoice.Text, TTSOutputVoiceOption1.Text, TTSOutputVoice.Text, ParseIntOrDefault(TTSVoiceLevel.Text, 0), ParseIntOrDefault(TTSSpeedLevel.Text, 0), ParseIntOrDefault(TTSPitchLevel.Text, 0));

            var result = await azureSpeechAPI.AzureSpeak(TextToSpeak);
            if (!result)
            {
                _bBBlog.Error("Azure TTS error. Is there a problem with your API key or subscription?");
                MessageBox.Show("Azure TTS error. Is there a problem with your API key or subscription?", "Azure TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (PersonasPanel.Visible)
                {
                    EnablePersonaEventHandlers();
                }

                PersonasPanel.Enabled = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        /// <summary>
        /// This function fills the Azure voice options in the GUI TTSOutputVoiceOptions, depends on the selected voice
        /// </summary>
        private void TTSAzureFillOptions(string SelectedVoice)
        {
            _bBBlog.Info("Finding Azure voice options (if available)");
            TTSOutputVoiceOption1.Items.Clear();
            //the voice is the item in TTSOutputVoice 
            //now to find it in AzureRegionVoicesList
            foreach (var azureRegionVoice in tTSAzureVoicesList)
            {
                if (SelectedVoice == (azureRegionVoice.LocaleDisplayname + "-" + azureRegionVoice.Gender + "-" + azureRegionVoice.LocalName))
                {
                    _bBBlog.Info($"Match found, checking for voice options. Voice has a total of {azureRegionVoice.StyleList.Count}");

                    foreach (var voiceOption in azureRegionVoice.StyleList)
                    {
                        if (voiceOption.Length > 0)
                            TTSOutputVoiceOption1.Items.Add(voiceOption);
                        else
                            TTSOutputVoiceOption1.Items.Add("Default");
                    }
                }
            }
            //a voice without any style gives an empty option list
            if (TTSOutputVoiceOption1.Items.Count > 0)
            {
                TTSOutputVoiceOption1.SelectedIndex = 0;
                TTSOutputVoiceOption1.Text = TTSOutputVoiceOption1.Items[0].ToString();
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TestAzureAPISettings_Click(object sender, EventArgs e)
        {
            //test if the Azure API settings are correct
            if (await TTSGetAzureVoices())
            {
                MessageBox.Show("Azure API settings are correct", "Azure API Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TestVoiceButton_Click(object sender, EventArgs e)
        {
            TestVoiceButton.Enabled = false;
            if (PersonaRoleTextBox.Text.Length < 1)
            {
                await SayTextTest("Hi, this is a test123");
            }
            else
            {
                await SayTextTest(PersonaRoleTextBox.Text);
            }

            TestVoiceButton.Enabled = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //this is the generic caller for TTS function that makes sure the TTS is done before continuing
        //we make this public so we can call it to test
        public async Task SayTextTest(string TextToSay)
        {
            if (TTSProviderComboBox.Text == "Native")
            {
                await TTSNativeSpeakToOutput(TextToSay);
            }
            else if (TTSProviderComboBox.Text == "Azure")
            {
                //we also need to fill the list and select the first available voice
                //then the options
                if (TTSOutputVoice.Items.Count < 1 || TTSOutputVoice.Text == "placeholder")
                {
                    if (await TTSGetAzureVoices())
                    {
                        TTSFillAzureVoicesList();
                    }
                    else
                    {
                        return;
                    }
                }
                await TTSAzureSpeakToOutput(TextToSay);
            }
            else if (TTSProviderComboBox.Text == "OpenAI Whisper")
            {
                await TTSOpenAIWhisperSpeakToOutput(TextToSay);
            }
            else if (TTSProviderComboBox.Text == "ElevenLabs")
            {
                await TTSElevenLabsSpeakToOutput(TextToSay);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSOpenAIWhisperSpeakToOutput(string TextToSay)
        {
            OpenAI openAI = new();
            var result = await openAI.OpenAITTS(TextToSay, TTSAudioOutputComboBox.Text, TTSOutputVoice.Text, ParseIntOrDefault(TTSSpeedLevel.Text, 0), ParseIntOrDefault(TTSVoiceLevel.Text, 0));
            if (!result)
            {
                _bBBlog.Error("OpenAI Whisper TTS error. Is there a problem with your API key or subscription?");
                MessageBox.Show("OpenAI Whisper TTS error. Is there a problem with your API key or subscription?", "OpenAI Whisper TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSElevenLabsSpeakToOutput(string TextToSay)
        {
            string outputVoice = TTSOutputVoice.Text;
            //output voice text has to be in the format of voiceID;voiceName we need to find it in _elevenLabsVoicesList
            //the voice fetch can have failed, iterating a null list would crash the async caller
            if (_elevenLabsVoicesList != null)
            for (int i = 0; i < _elevenLabsVoicesList.Count; i++)
            {
                if (_elevenLabsVoicesList[i].Contains(outputVoice))
                {
                    _bBBlog.Debug("Found voice in list: " + _elevenLabsVoicesList[i]);
                    outputVoice = _elevenLabsVoicesList[i];
                    break;
                }
            }


            elevenLabsApi ??= new(ElevenlabsAPIKeyTextBox.Text);

            var result = await elevenLabsApi.ElevenLabsTTS(TextToSay, TTSAudioOutputComboBox.Text, outputVoice, ParseIntOrDefault(TTSOutputVoiceOption1.Text, 0), ParseIntOrDefault(TTSOutputVoiceOption2.Text, 0), ParseIntOrDefault(TTSOutputVoiceOption3.Text, 0), ParseIntOrDefault(TTSVoiceLevel.Text, 0));
            if (!result)
            {
                _bBBlog.Error("ElevenLabs TTS error. Is there a problem with your API key or subscription?");
                MessageBox.Show("ElevenLabs TTS error. Is there a problem with your API key or subscription?", "ElevenLabs TTS error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async Task TTSNativeSpeakToOutput(String TTSText)
        {
            _bBBlog.Info("Saying text with Native TTS");
            NativeSpeech nativeSpeech = new();
            await nativeSpeech.NativeTTSInit(TTSOutputVoice.Text, TTSAudioOutputComboBox.Text, ParseIntOrDefault(TTSVoiceLevel.Text, 0), ParseIntOrDefault(TTSSpeedLevel.Text, 0), ParseIntOrDefault(TTSPitchLevel.Text, 0));
            await nativeSpeech.NativeSpeak(TTSText);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void GPTTestButton_Click(object sender, EventArgs e)
        {
            if (await OpenAI.ValidateApiKeyAsync(GPTAPIKeyTextBox.Text))
            {
                _bBBlog.Info("ChatGPT API key is valid");
                MessageBox.Show("ChatGPT API key is valid", "OpenAI API Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _bBBlog.Error("ChatGPT API key is invalid");
                MessageBox.Show("ChatGPT API key is invalid", "OpenAI API Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchAuthorizeButton_Click(object sender, EventArgs e)
        {
            //lets not block everything, but lets try get a Twitch Auth token.
            //This is done by spawning a browser where the user has to authorize (implicit grant) 
            //the application. 
            TwitchAPIESub twitchAPI = new();
            //see https://dev.twitch.tv/docs/authentication/scopes/ and https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessage
            //API events:
            //channel.send.message ("user:write:chat")
            //eventsub events:
            //channel.chat.message (user:read:chat) to read chat
            //channel.subscribe (channel:read:subscriptions) to get subscription events
            //channel.subscription.gift (channel:read:subscriptions) to get gifted sub events
            //channel.subscription.message (channel:read:subscriptions) to get sub message events
            //channel.cheer (bits:read) to get information on cheered bits
            //channel.follow (moderator:read:followers) to get who followed a channel
            //channel.channel_points_automatic_reward_redemption.add (channel:read:redemptions) to get automatic reward redemptions by MyTwitchNotableViewers
            //channel.channel_points_custom_reward_redemption.add (channel:read:redemptions) to get custom reward redemptions by MyTwitchNotableViewers

            try
            {
                var twitchAPIResult = await twitchAPI.GetTwitchAuthToken([
                        //API scope to send text to chat
                        "user:write:chat", 
                    //EventSub scopes for subscription types to read chat, get subscription events, read when people cheer (bits) and follower events
                    "user:read:chat", "channel:read:subscriptions", "bits:read", "moderator:read:followers", "channel:read:redemptions"]);

                if (!twitchAPIResult)
                {
                    _bBBlog.Error("Issue with getting auth token. Check logs for more information.");
                    MessageBox.Show($"Issue with getting Auth token. Check logs for more information.", "Twitch Authorization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    TwitchBroadcasterAccessToken.Text = twitchAPI.TwitchAccessToken;
                    //auto-fill the broadcaster account name that was just authorized
                    if (!string.IsNullOrWhiteSpace(twitchAPI.TwitchAuthorizedUserName))
                        TwitchBroadcasterChannel.Text = twitchAPI.TwitchAuthorizedUserName;
                }
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Twitch Broadcaster authorization failed: " + ex.Message);
                MessageBox.Show("Twitch authorization failed: " + ex.Message, "Twitch Authorization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TwitchTestButton_Click(object sender, EventArgs e)
        {
            //first lets make sure people cant click too often
            twitchAPIVerified = false;
            TwitchAPITestButton.Enabled = false;
            //first we check if the Authorization key is fine, using the API
            TwitchAPIESub twAPITest = new();

            //check to see if we need to send a message on join
            //only if there is no bot account authorized. If there is a bot account authorized we will send a message on join wiht that account
            if (TwitchSendTextCheckBox.Checked && TwitchBotAuthKey.Text.Length < 1)
            {
                twAPITest.TwitchSendTestMessageOnJoin = TwitchTestSendText.Text;
            }

            //we need the username AND channel name to get the broadcasterid which is needed for sending a message via the API
            var VerifyOk = await twAPITest.CheckAuthCodeAPI(TwitchBroadcasterAccessToken.Text, TwitchBroadcasterChannel.Text, TwitchBroadcasterChannel.Text);
            if (!VerifyOk)
            {
                _bBBlog.Error("Problem verifying Broadcaster Access token, something is wrong with the access token!");
                //  TextLog.Text += "Problem verifying Access token, invalid access token\r\n";
                MessageBox.Show("Problem verifying Broadcaster Access token, invalid access token", "Twitch Access Token veryfication result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TwitchAPITestButton.Enabled = true;
                twitchAPIVerified = false;
                TwitchEventSubTestButton.Enabled = false;
                return;
            }
            else
            {
                _bBBlog.Info($"Twitch Broadcaster Access token verified success!");
                twitchAPIVerified = true;
                TwitchEventSubTestButton.Enabled = true;
                MessageBox.Show($"Twitch Broadcaster Access token verified success!", "Twitch Access Token verification result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //alright now we need to test the bot IF it has been verified
            if (TwitchBotAuthKey.Text.Length > 0)
            {
                //check to see if we need to send a message on join
                if (TwitchSendTextCheckBox.Checked)
                {
                    twAPITest.TwitchSendTestMessageOnJoin = TwitchTestSendText.Text;
                }

                VerifyOk = await twAPITest.CheckAuthCodeAPI(TwitchBotAuthKey.Text, TwitchBotName.Text, TwitchBroadcasterChannel.Text);
                if (!VerifyOk)
                {
                    _bBBlog.Error("Problem verifying Bot Access token, something is wrong with the access token!");
                    //  TextLog.Text += "Problem verifying Access token, invalid access token\r\n";
                    MessageBox.Show("Problem verifying Bot Access token, invalid access token", "Twitch Access Token veryfication result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    TwitchAPITestButton.Enabled = true;
                    twitchAPIVerified = false;
                    TwitchEventSubTestButton.Enabled = false;
                    return;
                }
                else
                {
                    _bBBlog.Info($"Twitch Bot Access token verified success!");
                    twitchAPIVerified = true;
                    TwitchEventSubTestButton.Enabled = true;
                    MessageBox.Show($"Twitch Bot Access token verified success!", "Twitch Access Token verification result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            TwitchAPITestButton.Enabled = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void EventSubTest_Click(object sender, EventArgs e)
        {
            //This only works once API access-token is verified
            if (!twitchAPIVerified)
            {
                MessageBox.Show("You need to verify the API key first.", "Twitch EventSub error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (await EventSubStartWebsocketClientTest())
            {
                MessageBox.Show("EventSub server started successfully so all is well!", "Twitch EventSub success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Issue with starting EventSub server. Check logs for more information.", "Twitch EventSub error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //here we star the main websocket client for Twitch EventSub
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task<bool> EventSubStartWebsocketClientTest()
        {
            twitchTestEventSub = new();
            bool eventSubStart = false;
            //we should set here what eventhandlers we want to have enabled based on the twitch Settings

            if (await twitchTestEventSub.EventSubInit(TwitchBroadcasterAccessToken.Text, TwitchBroadcasterChannel.Text, TwitchBroadcasterChannel.Text))
            {
                //we need to first set the event handlers we want to use

                //if we are in MOCK mode, just enable all handlers?
                if (TwitchMockEventSub.Checked)
                {
                    _bBBlog.Info("Twitch TEST read chat enabled, calling eventsubhandlereadchat. Command set to $BBB for test");
                    twitchTestEventSub.EventSubHandleReadchat("$BBB", false, true);
                    //set local eventhanlder for valid chat messages to trigger the bot
                    twitchTestEventSub.OnESubChatMessage += TwitchEventSub_OnESubChatMessage;
                }

                //do we want to check cheer messages?
                if (TwitchMockEventSub.Checked)
                {
                    _bBBlog.Info("Twitch TEST cheers enabled, calling EventSubHandleCheer with the min amount of bits needed to trigger. Set to 10 for test");
                    twitchTestEventSub.EventSubHandleCheer(10);
                    twitchTestEventSub.OnESubCheerMessage += TwitchEventSub_OnESubCheerMessage;
                }

                //do we want to check for subscription events?
                if (TwitchMockEventSub.Checked)
                {
                    //new subs
                    _bBBlog.Info($"Twitch TEST subscriptions enabled, calling EventSubHandleSubscription: {twitchTestEventSub}");
                    twitchTestEventSub.EventSubHandleSubscription();
                    twitchTestEventSub.OnESubSubscribe += TwitchEventSub_OnESubSubscribe;
                    twitchTestEventSub.OnESubReSubscribe += TwitchEventSub_OnESubReSubscribe;
                    //todo set eventhandler being thrown when a new sub is detected or resub
                }
                //TODO: gifted subs TwitchGiftedSub.Checked
                if (TwitchMockEventSub.Checked)
                {
                    _bBBlog.Info("Twitch TEST gifted subs enabled, calling EventSubHandleGiftedSubs");
                    twitchTestEventSub.EventSubHandleSubscriptionGift();
                    twitchTestEventSub.OnESubSubscriptionGift += TwitchEventSub_OnESubGiftedSub;
                }

                //do we want to check for channel point redemptions?
                if (TwitchMockEventSub.Checked)
                {
                    _bBBlog.Info("Twitch TEST channel points enabled, calling EventSubHandleChannelPoints");
                    twitchTestEventSub.EventSubHandleChannelPointRedemption("rewardtest");
                    twitchTestEventSub.OnESubChannelPointRedemption += TwitchEventSub_OnESubChannelPointRedemption;
                }

                if (!TwitchMockEventSub.Checked)
                {
                    //if we are not in mock mode, we can start the client
                    eventSubStart = await twitchTestEventSub.EventSubStartAsync();
                }
                else
                { //we are in mock mode, so we just say we started
                    _bBBlog.Info("Twitch EventSub client starting successfully in mock mode");
                    eventSubStart = await twitchTestEventSub.EventSubStartAsyncMock();

                    twitchStartedTest = true;
                }

                if (eventSubStart)
                {
                    _bBBlog.Info("Twitch EventSub client  started successfully");

                    twitchStartedTest = true;
                }
                else
                {
                    _bBBlog.Error("Issue with starting Twitch EventSub server. Check logs for more information.");
                    twitchStartedTest = false;
                    return false;
                }
                return true;
            }
            else
            {
                _bBBlog.Error("Issue with starting Twitch EventSub server. Check logs for more information.");
                twitchStartedTest = false;
                return false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchEventSub_OnESubChannelPointRedemption(object sender, TwitchEventhandlers.OnChannelPointCustomRedemptionEventArgs e)
        {
            string user = e.GetChannelPointCustomRedemptionInfo()[0];
            string message = e.GetChannelPointCustomRedemptionInfo()[1];
            _bBBlog.Info($"TEST: Valid Twitch Channel Point Redemption message received: {user} redeemed with message: {message}");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //TwitchEventSub_OnESubGiftedSub
        private void TwitchEventSub_OnESubGiftedSub(object sender, TwitchEventhandlers.OnSubscriptionGiftEventArgs e)
        {
            string user = e.GetSubscriptionGiftInfo()[0];
            string amount = e.GetSubscriptionGiftInfo()[1];
            string tier = e.GetSubscriptionGiftInfo()[2];
            _bBBlog.Info($"TEST: Valid Twitch Gifted Subscription message received: {user} gifted {amount} subs tier {tier}");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchEventSub_OnESubSubscribe(object sender, TwitchEventhandlers.OnSubscribeEventArgs e)
        {
            string user = e.GetSubscribeInfo()[0];
            string broadcaster = e.GetSubscribeInfo()[1];
            _bBBlog.Info($"TEST: Valid Twitch Subscription message received: {user} subscribed to {broadcaster}");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchEventSub_OnESubReSubscribe(object sender, TwitchEventhandlers.OnReSubscribeEventArgs e)
        {
            string user = e.GetSubscribeInfo()[0];
            string message = e.GetSubscribeInfo()[1];
            string months = e.GetSubscribeInfo()[2];
            _bBBlog.Info($"TEST: Valid Twitch Re-Subscription message received: {user} subscribed for {months} months with message: {message}");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        //eventhandler for valid chat messages trigger
        private void TwitchEventSub_OnESubChatMessage(object sender, TwitchEventhandlers.OnChatEventArgs e)
        {

            string message = e.GetChatInfo()[1].Replace("$BBB", "");
            string user = e.GetChatInfo()[0];
            //we got a valid chat message, lets see what we can do with it
            _bBBlog.Info("TEST: Valid Twitch Chat message received from user: " + user + " message: " + message);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchEventSub_OnESubCheerMessage(object sender, TwitchEventhandlers.OnCheerEventsArgs e)
        {
            //string user = e.GetCheerInfo()[0];
            // string message = e.GetCheerInfo()[1];
            //we got a valid cheer message, lets see what we can do with it
            _bBBlog.Debug("TEST: Valid Twitch Cheer message received");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (twitchAPIVerified)
            {
                TwitchEventSubTestButton.Enabled = true;
            }
            else
            {
                TwitchEventSubTestButton.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void PersonaRoleTextBox_TextChanged(object sender, EventArgs e)
        {
            if (PersonasPanel.Visible)
            {
                // _bBBlog.Debug("Persona role text changed");
                personaEdited = true;
                SavePersona.Enabled = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoiceOption1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PersonasPanel.Visible)
            {
                _bBBlog.Debug("Persona voice option changed");
                personaEdited = true;
                SavePersona.Enabled = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void PersonaComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            PersonasPanel.Enabled = false;
            _bBBlog.Debug($"Persona selected value changed to {PersonaComboBox.Text} index {PersonaComboBox.SelectedIndex}");
            var tmpNewPersonaIndex = PersonaComboBox.SelectedIndex;
            _bBBlog.Debug($"Disable eventhandlers for now while loading the new persona data");
            DisablePersonaEventHandlers();


            //if the persona is changed we first need to check if the persona was edited if so we need to ask to save it
            if (personaEdited && PersonasPanel.Visible)
            {
                //a hand-edited or emptied persona list can make the previous index invalid
                string previousPersonaName = (previousPersonaSelectedIndex >= 0 && previousPersonaSelectedIndex < personas.Count)
                    ? personas[previousPersonaSelectedIndex].Name : "unknown";
                _bBBlog.Debug($"Persona {previousPersonaName} was edited, asking to save it");
                var result = MessageBox.Show("Persona Changed. Do you want to save the persona?", "Save Persona", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    //we need to save the previous persona index with the values, not the current one otherwise we save the wrong tts info with the wrong persona
                    PersonaComboBox.SelectedIndex = previousPersonaSelectedIndex;
                    SavePersona_Click(sender, e);
                    //now we return to the newly selected persona index
                    PersonaComboBox.SelectedIndex = tmpNewPersonaIndex;

                }
                personaEdited = false;
                SavePersona.Enabled = false;
            }

            //alright index is changed! We need to load the persona into the form
            //To make sure we get back to the actual selected persona index, we need to re-assign it incase savepersona was called
            //but on the first load, it might also be empty so we need to check for that and load the default persona if so.
            if (tmpNewPersonaIndex == -1)
            {
                _bBBlog.Debug("Persona index is -1, setting to default (0)");
                PersonaComboBox.SelectedIndex = 0;
            }
            else
            {
                PersonaComboBox.SelectedIndex = tmpNewPersonaIndex;
            }

            //if the list is empty or the index invalid, loading the persona cannot continue
            if (PersonaComboBox.SelectedIndex < 0 || PersonaComboBox.SelectedIndex >= personas.Count)
            {
                _bBBlog.Error("No persona available for the selected index, aborting persona load");
                PersonasPanel.Enabled = true;
                if (PersonasPanel.Visible)
                {
                    EnablePersonaEventHandlers();
                }
                return;
            }

            var selectedPersona = personas[PersonaComboBox.SelectedIndex];
            _bBBlog.Debug($"Loading persona into form. Personaname: {selectedPersona.Name} VoiceProvider: {selectedPersona.VoiceProvider} TTSOutputvoice: {selectedPersona.VoiceName}  Amount of options: {selectedPersona.VoiceOptions.Count}");
            PersonaRoleTextBox.Text = selectedPersona.RoleText;
            //we should set the volume and rate of the voice
            _bBBlog.Debug($"Setting rate and volume to {selectedPersona.Rate} and {selectedPersona.Volume}");
            RateTrackBar.Value = selectedPersona.Rate;
            VolumeTrackBar.Value = selectedPersona.Volume;
            PitchTrackBar.Value = selectedPersona.Pitch;
            TTSPitchLevel.Text = selectedPersona.Pitch.ToString();
            TTSVoiceLevel.Text = selectedPersona.Volume.ToString();
            TTSSpeedLevel.Text = selectedPersona.Rate.ToString();

            //now lets load the one thats part of the selected persona
            TTSProviderComboBox.SelectedIndex = TTSProviderComboBox.FindStringExact(selectedPersona.VoiceProvider);
            //the provider can be different from the one before so we need to load teh voices
            if (!await FillVoiceBoxes())
            {
                _bBBlog.Error("Error connecting to TTS provider " + selectedPersona.VoiceProvider + ". No connection or no valid API?");
                TTSOutputVoice.Text = "";
                TTSOutputVoice.Items.Clear();
                PersonasPanel.Enabled = true;
                if (PersonasPanel.Visible)
                {
                    EnablePersonaEventHandlers();
                }
                MessageBox.Show("Persona uses " + selectedPersona.VoiceProvider + " But no connection or no valid API key", "TTS Provider error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _bBBlog.Debug($"Voice boxes filled, now to select the voice. Personavoice: {selectedPersona.VoiceName} ");

            //if provider is elevenlabs we need to select the voice by the split part not the voice name
            if (TTSProviderComboBox.Text == "ElevenLabs")
            {
                string[] tmpVoice = selectedPersona.VoiceName.Split(";");
                try
                {
                    _bBBlog.Debug($"Elevenlabs voice selected: {tmpVoice[1]}");
                    TTSOutputVoice.SelectedIndex = TTSOutputVoice.FindStringExact(tmpVoice[1]);
                }
                catch (Exception)
                {
                    _bBBlog.Error("Error loading persona voice, no voiceID found in persona");
                    MessageBox.Show("Error loading persona voice, no voiceID found in persona", "Persona voice error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    TTSOutputVoice.SelectedIndex = 0;
                    PersonasPanel.Enabled = true;
                    if (PersonasPanel.Visible)
                    {
                        EnablePersonaEventHandlers();
                    }
                    return;
                }
            }
            else
                TTSOutputVoice.SelectedIndex = TTSOutputVoice.FindStringExact(selectedPersona.VoiceName);


            //now to fill the options field
            if (TTSProviderComboBox.Text == "Azure")
                TTSAzureFillOptions(TTSOutputVoice.Text);
            if (selectedPersona.VoiceOptions.Count > 0)
            {
                _bBBlog.Debug("Voice options found, loading them into the combo box");
                if (TTSProviderComboBox.Text == "Azure")
                    TTSOutputVoiceOption1.SelectedIndex = TTSOutputVoiceOption1.FindStringExact(selectedPersona.VoiceOptions[0]);
                if (TTSProviderComboBox.Text == "ElevenLabs")
                {
                    _bBBlog.Debug("Persona Elevenlabs voice options found, loading them into the combo box");
                    //a hand edited persona can have fewer than the three options ElevenLabs uses
                    string elevenOption1 = selectedPersona.VoiceOptions.Count > 0 ? selectedPersona.VoiceOptions[0] : "100";
                    string elevenOption2 = selectedPersona.VoiceOptions.Count > 1 ? selectedPersona.VoiceOptions[1] : "100";
                    string elevenOption3 = selectedPersona.VoiceOptions.Count > 2 ? selectedPersona.VoiceOptions[2] : "0";
                    _bBBlog.Debug($"Option1: {elevenOption1} option2: {elevenOption2} option3: {elevenOption3}");
                    TTSOutputVoiceOption1.Text = elevenOption1;
                    TTSOutputVoiceOption2.Text = elevenOption2;
                    TTSOutputVoiceOption3.Text = elevenOption3;
                }
            }
            if (PersonaComboBox.Text == "Default")
            {
                DeletePersona.Enabled = false;
            }
            else
            {
                DeletePersona.Enabled = true;
            }
            //enable them again but only if visible
            if (PersonasPanel.Visible)
            {
                EnablePersonaEventHandlers();
                PersonasPanel.Enabled = true;
            }
            previousPersonaSelectedIndex = PersonaComboBox.SelectedIndex;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void TTSProviderComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            PersonasPanel.Enabled = false;
            _bBBlog.Debug("TTSProvider selected value changed");
            if (PersonasPanel.Visible)
            {
                SavePersona.Enabled = true;
                personaEdited = true;
                _bBBlog.Info("TTS Provider changed to " + TTSProviderComboBox.Text);
                TTSOutputVoice.Items.Clear();

                TTSOutputVoiceOption1.Text = "";
                TTSOutputVoiceOption2.Text = "";
                TTSOutputVoiceOption3.Text = "";
                if (!await FillVoiceBoxes())
                    return;
            }
            PersonasPanel.Enabled = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoice_SelectedValueChanged(object sender, EventArgs e)
        {
            if (PersonasPanel.Visible)
            {
                SavePersona.Enabled = true;
                personaEdited = true;
                if (TTSProviderComboBox.Text == "Azure")
                {
                    _bBBlog.Info($"Azure voice changed, filling options on {TTSOutputVoice.Text}");
                    //depending on what voice is selected we need to now select the voice options (if any)
                    TTSAzureFillOptions(TTSOutputVoice.Text);
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void MenuTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (personaEdited)
            {
                _bBBlog.Debug("Personas panel hidden. we should save the persona's if anything changed");
                //ask if we need to save the persona
                var result = MessageBox.Show("Information changed. Do you want to save the persona?", "Save Persona", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    SavePersona_Click(sender, e);
                }
                personaEdited = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public void ShowHotkeyDialogBox()
        {
            HotkeyForm HotkeyDialog = new();

            var result = HotkeyDialog.ShowDialog(this);
            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            if (result == DialogResult.OK && HotkeyDialog.ReturnValue1 != null)
            {
                // _setHotkeys.Clear();
                this.MicrophoneHotkeyEditbox.Text = "";
                List<Keys> hotKeys = HotkeyDialog.ReturnValue1;
                //ok now we got the keys, parse them and put them in the index box
                // and the global list for hotkeys

                for (var i = 0; i < hotKeys.Count; i++)
                {

                    if (i < hotKeys.Count - 1)
                        this.MicrophoneHotkeyEditbox.Text += hotKeys[i].ToString() + " + ";
                    else
                        this.MicrophoneHotkeyEditbox.Text += hotKeys[i].ToString();

                }
            }
            _bBBlog.Info("Hotkey set to " + MicrophoneHotkeyEditbox.Text);
            HotkeyDialog.Dispose();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void MicrophoneHotkeySet_Click(object sender, EventArgs e)
        {
            ShowHotkeyDialogBox();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void DeletePersona_Click(object sender, EventArgs e)
        {
            if (PersonasPanel.Visible)
            {
                if (PersonaComboBox.Text == "Default")
                {
                    MessageBox.Show("You cannot delete the default persona", "Delete Persona", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var result = MessageBox.Show("Delete Persona. Do you want to delete the persona?", "Delete Persona", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                //new persona's wont have a selectedindex yet.
                if (PersonaComboBox.SelectedIndex == -1)
                    PersonaComboBox.SelectedIndex = PersonaComboBox.FindStringExact(PersonaComboBox.Text);
                _bBBlog.Info("Delete persona selected: " + PersonaComboBox.SelectedIndex + " " + PersonaComboBox.Text);

                foreach (var persona in personas)
                {
                    _bBBlog.Info("Personas available : " + persona.Name);
                }
                if (result == DialogResult.Yes)
                {
                    personas.Remove(personas[PersonaComboBox.SelectedIndex]);
                    PersonaComboBox.Items.Remove(PersonaComboBox.SelectedItem);
                    PersonaComboBox.SelectedIndex = 0;
                    SavePersona_Click(sender, e);
                }
                personaEdited = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void ElevenLabsTestButton_Click(object sender, EventArgs e)
        {
            //call test api key for elevenlabs
            if (elevenLabsApi != null)
                elevenLabsApi = null;
            elevenLabsApi ??= new(ElevenlabsAPIKeyTextBox.Text);

            if (await elevenLabsApi.ElevenLabsAPIKeyTest())
            {
                _bBBlog.Info("ElevenLabs API key is valid");
                MessageBox.Show("ElevenLabs API key is valid", "ElevenLabs API Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _bBBlog.Error("ElevenLabs API key is invalid");
                MessageBox.Show("ElevenLabs API key is invalid", "ElevenLabs API Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoiceOption1_TextChanged(object sender, EventArgs e)
        {
            if (PersonasPanel.Visible && TTSOutputVoiceOption1.DropDownStyle == ComboBoxStyle.Simple)
            {
                SavePersona.Enabled = true;
                personaEdited = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoiceOption2_TextChanged(object sender, EventArgs e)
        {
            if (PersonasPanel.Visible && TTSOutputVoiceOption2.DropDownStyle == ComboBoxStyle.Simple)
            {
                SavePersona.Enabled = true;
                personaEdited = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoiceOption3_TextChanged(object sender, EventArgs e)
        {
            if (PersonasPanel.Visible && TTSOutputVoiceOption3.DropDownStyle == ComboBoxStyle.Simple)
            {
                SavePersona.Enabled = true;
                personaEdited = true;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void OllamaTestButton_Click(object sender, EventArgs e)
        {
            if (OllamaURITextBox.Text.Length > 1)
            {
                OllamaTestButton.Enabled = false;
                OllamaLLM ollama = new(OllamaURITextBox.Text);

                if (await ollama.OllamaVerify())
                {
                    _bBBlog.Debug("Ollama LLM is running on the URI you selected");
                }
                else
                {
                    _bBBlog.Error("Ollama LLM is not running on the URI you selected");
                    MessageBox.Show("Ollama LLM is not running on the URI you selected", "Ollama Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UseOllamaLLMCheckBox.Checked = false;
                    OllamaTestButton.Enabled = true;
                    return;
                }
                var installedModels = await ollama.OllamaLLMGetModels();

                if (installedModels == null || installedModels.Count < 1)
                {
                    _bBBlog.Error("Ollama model error. Is Ollama running on the URI you selected?");
                    MessageBox.Show("Ollama model error. Is there a problem with your API key or subscription?", "Ollama Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UseOllamaLLMCheckBox.Checked = false;
                    OllamaTestButton.Enabled = true;
                    return;
                }
                //add the installed models
                OllamaModelsComboBox.Items.Clear();
                for (int i = 0; i < installedModels.Count; i++)
                {
                    OllamaModelsComboBox.Items.Add(installedModels[i]);
                }
                //if theres models but none is yet loaded, use the default one
                if (OllamaModelsComboBox.SelectedIndex == -1)
                    OllamaModelsComboBox.SelectedIndex = 0;
                var result = await ollama.OllamaGetResponse("this is a test!", OllamaModelsComboBox.Text);
                if (result == null)
                {
                    _bBBlog.Error("Ollama response error. Is Ollama running on the URI you selected?");
                    MessageBox.Show("Ollama response error. Is there a problem with your API key or subscription?", "Ollama Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UseOllamaLLMCheckBox.Checked = false;
                    OllamaTestButton.Enabled = true;
                    return;
                }
                _bBBlog.Debug("Ollama test result: " + result);
                OllamaTestButton.Enabled = true;
                if (result == null || result.Length < 1)
                {
                    _bBBlog.Error("Ollama error.Is Ollama running on the URI you selected?");
                    MessageBox.Show("Ollama error. Is there a problem with your API key or subscription?", "Ollama Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UseOllamaLLMCheckBox.Checked = false;
                    OllamaTestButton.Enabled = true;
                    return;
                }
                MessageBox.Show("Ollama works!", "Ollama Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OllamaTestButton.Enabled = true;
            }
            else
            {
                MessageBox.Show("Ollama URI cannot be empty", "Ollama Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void OllamaPanel_VisibleChanged(object sender, EventArgs e)
        {
            //dont bother filling if we dont use the Ollama LLM
            if (OllamaPanel.Visible && !UseOllamaLLMCheckBox.Checked)
            {
                _bBBlog.Info("Ollama not enabled dont bother loading anything.");
                return;
            }

            OllamaPanel.Enabled = false;
            _bBBlog.Info("Ollama panel visible.");
            _bBBlog.Info("Ollama now we load the Uri");

            //if theres nothing we use the default for Ollama
            if (OllamaPanel.Visible && UserSettingsManager.Settings.OllamaURI.Length < 1)
            {
                _bBBlog.Info("Ollama URI is empty, setting to default");
                OllamaURITextBox.Text = "http://localhost:11434";
                UserSettingsManager.Settings.OllamaURI = OllamaURITextBox.Text;
                UserSettingsManager.SaveSettings();
            }
            else
            {
                OllamaURITextBox.Text = UserSettingsManager.Settings.OllamaURI;
            }

            if (OllamaPanel.Visible && UserSettingsManager.Settings.OllamaURI.Length > 1)
            {

                OllamaLLM ollama = new(OllamaURITextBox.Text);
                List<string> installedModels = await ollama.OllamaLLMGetModels();
                if (installedModels == null)
                {
                    _bBBlog.Info("Ollama no models found");
                    MessageBox.Show("Ollama no models found. You installed none or Ollama is not running on the selected URI", "Ollama Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    OllamaPanel.Enabled = true;
                    if (UseOllamaLLMCheckBox.Checked)
                        UseOllamaLLMCheckBox.Checked = false;
                    return;
                }

                OllamaModelsComboBox.Items.Clear();
                for (int i = 0; i < installedModels.Count; i++)
                {
                    OllamaModelsComboBox.Items.Add(installedModels[i]);
                }
                _bBBlog.Info($"Found {installedModels.Count} models");
                // lets set a default if there's none
                if (UserSettingsManager.Settings.OllamaSelectedModel.Length < 1)
                {
                    _bBBlog.Info("Ollama no model selected, setting to default");
                    if (OllamaModelsComboBox.Items.Count > 0)
                        OllamaModelsComboBox.SelectedIndex = 0;
                    else
                        _bBBlog.Info("Ollama no models found");
                    //TODO show throw an error here
                } //else we pre-select the one thats in the settings
                else
                {
                    _bBBlog.Info("Setting selected model to: " + UserSettingsManager.Settings.OllamaSelectedModel);
                    OllamaModelsComboBox.SelectedIndex = OllamaModelsComboBox.FindStringExact(UserSettingsManager.Settings.OllamaSelectedModel);
                }

            }
            else
            {
                _bBBlog.Info("Ollama Uri is empty, so we cant preload models");
            }
            OllamaPanel.Enabled = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void UseOllamaLLMCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            /*    if (UseOllamaLLMCheckBox.Checked)
                {
                    _bBBlog.Info("Ollama LLM enabled, loading settings if not yet loaded");
                    if (OllamaModelsComboBox.Items.Count < 1)
                    {
                        OllamaPanel_VisibleChanged(sender, e);
                    }
                }
            */
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void GPTMaxTokensTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if the input is not a digit or control (like backspace)
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;  // Ignore the input
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void GPTTemperatureTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            System.Windows.Forms.TextBox currenttb = (System.Windows.Forms.TextBox)sender;
            string decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            // control keys like backspace and the delete key always have to pass, otherwise an
            // impossible value could never be corrected again
            if (char.IsControl(e.KeyChar))
            {
                return;
            }
            // only digits and the decimal separator of the current culture can be typed
            if (!char.IsDigit(e.KeyChar) && e.KeyChar.ToString() != decimalSeparator)
            {
                e.Handled = true;  // Ignore the input
                return;
            }
            // the separator is only allowed once
            if (e.KeyChar.ToString() == decimalSeparator && currenttb.Text.Contains(decimalSeparator))
            {
                e.Handled = true;  // Ignore the input
            }
            // the value range itself is checked when the field is left, blocking single keystrokes
            // here only made it impossible to edit a value that was already out of range
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void GPTTemperatureTextBox_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.TextBox currenttb = (System.Windows.Forms.TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
                return;
            }
            string decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string text = currenttb.Text.Trim();
            // a pasted value from another locale still parses, a point is read as the local separator
            if (decimalSeparator != "." && !text.Contains(decimalSeparator) && text.Contains("."))
            {
                text = text.Replace(".", decimalSeparator);
            }
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out float temperature))
            {
                MessageBox.Show($"Temperature must be a number between 0 and 2");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
                return;
            }
            // values above the supported range are corrected here, pasting is not seen by KeyPress
            if (temperature < 0 || temperature > 2)
            {
                float clamped = Math.Clamp(temperature, 0f, 2f);
                _bBBlog.Info($"Temperature {temperature} out of range, clamped to {clamped}");
                MessageBox.Show($"Temperature must be between 0 and 2, using {clamped.ToString(CultureInfo.CurrentCulture)}");
                currenttb.Text = clamped.ToString(CultureInfo.CurrentCulture);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void GPTMaxTokensTextBox_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.TextBox currenttb = (System.Windows.Forms.TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void OllamaContextTokensTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Check if the input is not a digit or control (like backspace)
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;  // Ignore the input
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void OllamaContextTokensTextBox_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.TextBox currenttb = (System.Windows.Forms.TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty. Use 0 for the Ollama server default.");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void PersonaComboBox_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.ComboBox currenttb = (System.Windows.Forms.ComboBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void PersonaRoleTextBox_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.TextBox currenttb = (System.Windows.Forms.TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoiceOption1_Validating(object sender, CancelEventArgs e)
        {
            if (TTSOutputVoiceOption1.Enabled)
            {
                System.Windows.Forms.ComboBox currenttb = (System.Windows.Forms.ComboBox)sender;
                if (string.IsNullOrWhiteSpace(currenttb.Text))
                {
                    MessageBox.Show("This field cannot be empty");
                    e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoiceOption2_Validating(object sender, CancelEventArgs e)
        {
            if (TTSOutputVoiceOption2.Enabled)
            {
                System.Windows.Forms.ComboBox currenttb = (System.Windows.Forms.ComboBox)sender;
                if (string.IsNullOrWhiteSpace(currenttb.Text))
                {
                    MessageBox.Show("This field cannot be empty");
                    e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSOutputVoiceOption3_Validating(object sender, CancelEventArgs e)
        {
            if (TTSOutputVoiceOption3.Enabled)
            {
                System.Windows.Forms.ComboBox currenttb = (System.Windows.Forms.ComboBox)sender;
                if (string.IsNullOrWhiteSpace(currenttb.Text))
                {
                    MessageBox.Show("This field cannot be empty");
                    e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void UseOllamaLLMCheckBox_Click(object sender, EventArgs e)
        {
            OllamaURITextBox.Enabled = true;
            OllamaModelsComboBox.Enabled = true;
            OllamaResponseLengthComboBox.Enabled = true;
            OllamaTestButton.Enabled = true;
            if (UseOllamaLLMCheckBox.Checked)
            {
                OllamaPanel_VisibleChanged(sender, e);
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchAuthServerConfig_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.TextBox currenttb = (System.Windows.Forms.TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchAuthServerConfig_Leave(object sender, EventArgs e)
        {
            //if something new entered, save it.
            if (TwitchAuthServerConfig.Text.Length > 1 && TwitchAuthServerConfig.Text != UserSettingsManager.Settings.TwitchAuthServerConfig)
            {
                UserSettingsManager.Settings.TwitchAuthServerConfig = TwitchAuthServerConfig.Text;
                UserSettingsManager.SaveSettings();
            }
        }

        private void TwitchChatSoundSelectButton_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\html");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void OBSPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (WebsourceServerEnable.Checked)
            {
                WebsourceServer.Enabled = true;
            }
            else
            {
                WebsourceServer.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void WebsourceServerEnable_Click(object sender, EventArgs e)
        {
            if (WebsourceServerEnable.Checked)
            {
                WebsourceServer.Enabled = true;
            }
            else
            {
                WebsourceServer.Enabled = false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void WebsourceServer_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.TextBox currenttb = (System.Windows.Forms.TextBox)sender;
            if (string.IsNullOrWhiteSpace(currenttb.Text))
            {
                MessageBox.Show("This field cannot be empty");
                e.Cancel = true;  // Cancel the event and keep the focus on the TextBox
            }
        }

        //fills the GPT model selector from the library's model listing instead of the statically
        //configured items, the selected model is restored when it is still offered
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task<bool> RefreshGPTModelsFromLibraryAsync(string apiKey)
        {
            OpenAIChatGPTPanel.Enabled = false;
            try
            {
                List<string> chatModels;
                try
                {
                    chatModels = await OpenAI.FetchChatModelNamesAsync(apiKey);
                }
                catch (Exception ex)
                {
                    //this runs unobserved from async void handlers, nothing may escape
                    _bBBlog.Error("GPT model fetch failed: " + ex.Message);
                    return false;
                }
                if (chatModels.Count < 1)
                {
                    _bBBlog.Info("No GPT models fetched, keeping the built-in list");
                    return false;
                }
                string previousSelection = GPTModelComboBox.Text;
                GPTModelComboBox.Items.Clear();
                foreach (string model in chatModels)
                {
                    GPTModelComboBox.Items.Add(model);
                }
                //prefer the model the user had picked, then the one from the settings, else the first
                int index = GPTModelComboBox.FindStringExact(previousSelection);
                if (index < 0)
                    index = GPTModelComboBox.FindStringExact(UserSettingsManager.Settings.GPTModel);
                if (index >= 0)
                    GPTModelComboBox.SelectedIndex = index;
                else
                    GPTModelComboBox.SelectedIndex = 0;
                //keep the list, the selector is filled from this cache on every restart
                UserSettingsManager.Settings.GPTModelListCache = chatModels;
                UserSettingsManager.SaveSettings();
                _bBBlog.Info($"Loaded {chatModels.Count} GPT models from the OpenAI API");
                return true;
            }
            finally
            {
                OpenAIChatGPTPanel.Enabled = true;
            }
        }

        //pulls the model list for the entered (or saved) key into the GPT model selector
        [SupportedOSPlatform("windows10.0.10240")]
        private async void GPTLoadModelsButton_Click(object sender, EventArgs e)
        {
            string apiKey = GPTAPIKeyTextBox.Text;
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = UserSettingsManager.Settings.GPTAPIKey;
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                _bBBlog.Info("GPT model list load requested without an API key");
                MessageBox.Show("Enter an OpenAI API key first", "GPT models", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GPTLoadModelsButton.Enabled = false;
            bool loaded = await RefreshGPTModelsFromLibraryAsync(apiKey);
            GPTLoadModelsButton.Enabled = true;
            if (!loaded)
            {
                MessageBox.Show("Could not load the model list. Check the API key or your internet connection.", "GPT models", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //refills the reasoning effort selector with the levels cached for a model, the current pick
        //is kept when the model also supports it. Returns false when the model was never fetched,
        //the caller then leaves the list as it is until Load levels probes the API
        [SupportedOSPlatform("windows10.0.10240")]
        private bool ApplyCachedReasoningLevels(string model)
        {
            Dictionary<string, List<string>> cache = UserSettingsManager.Settings.GPTReasoningLevelsCache;
            if (cache == null || string.IsNullOrEmpty(model) || !cache.TryGetValue(model, out List<string> levels) || levels == null)
            {
                return false;
            }
            string previousSelection = GPTReasoningComboBox.Text;
            GPTReasoningComboBox.Items.Clear();
            GPTReasoningComboBox.Items.Add("Auto (model default)");
            foreach (string level in levels)
            {
                GPTReasoningComboBox.Items.Add(level);
            }
            int index = GPTReasoningComboBox.FindStringExact(previousSelection);
            GPTReasoningComboBox.SelectedIndex = index >= 0 ? index : 0;
            return true;
        }

        //when the user picks another model, show the reasoning levels cached for it, no API call
        [SupportedOSPlatform("windows10.0.10240")]
        private void GPTModelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyCachedReasoningLevels(GPTModelComboBox.Text);
        }

        //asks the OpenAI API which reasoning effort levels the model in the GPT model selector
        //supports and fills the effort dropdown with them, the models endpoints do not report
        //capabilities so the fetch uses a rejected probe request, see OpenAI.FetchReasoningEffortLevelsAsync
        [SupportedOSPlatform("windows10.0.10240")]
        private async void GPTReasoningLoadButton_Click(object sender, EventArgs e)
        {
            string apiKey = GPTAPIKeyTextBox.Text;
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = UserSettingsManager.Settings.GPTAPIKey;
            }
            string model = GPTModelComboBox.Text;
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
            {
                _bBBlog.Info("Reasoning levels requested without an API key or model");
                MessageBox.Show("Select a GPT model and enter an OpenAI API key first", "Reasoning effort", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GPTReasoningLoadButton.Enabled = false;
            OpenAIChatGPTPanel.Enabled = false;
            List<string> levels;
            string fetchError;
            try
            {
                (levels, fetchError) = await OpenAI.FetchReasoningEffortLevelsAsync(apiKey, model);
            }
            finally
            {
                GPTReasoningLoadButton.Enabled = true;
                OpenAIChatGPTPanel.Enabled = true;
            }
            if (levels == null)
            {
                //show OpenAI's own reason, a 429 for example means the account has no credits
                MessageBox.Show($"OpenAI did not report the reasoning levels for this model:\r\n\r\n{fetchError}", "Reasoning effort", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (levels.Count < 1)
            {
                _bBBlog.Info($"OpenAI reports {model} has no reasoning settings");
            }
            //remember the levels for this model, an empty list caches "no reasoning settings" too
            UserSettingsManager.Settings.GPTReasoningLevelsCache ??= [];
            UserSettingsManager.Settings.GPTReasoningLevelsCache[model] = levels;
            UserSettingsManager.SaveSettings();
            ApplyCachedReasoningLevels(model);
            if (levels.Count < 1)
            {
                MessageBox.Show($"The model \"{model}\" has no reasoning effort settings.", "Reasoning effort", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _bBBlog.Info($"Loaded {levels.Count} reasoning levels for {model}");
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void OpenAIChatGPTPanel_Validating(object sender, CancelEventArgs e)
        {
            if (OpenAIChatGPTPanel.Visible)
            {
                if (GPTAPIKeyTextBox.Text.Length > 1)
                {
                    //we need to see if the entered key is valid
                    if (!await OpenAI.ValidateApiKeyAsync(GPTAPIKeyTextBox.Text))
                    {
                        MessageBox.Show("Invalid OpenAI API key. Value cleared", "OpenAI API key error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        GPTAPIKeyTextBox.Text = "";
                        e.Cancel = true;
                    }
                    else
                    {
                        //might as well save since its a valid key
                        UserSettingsManager.Settings.GPTAPIKey = GPTAPIKeyTextBox.Text;
                        UserSettingsManager.SaveSettings();
                    }
                }
                else
                {
                    //its empty so lets save it
                    UserSettingsManager.Settings.GPTAPIKey = "";
                    UserSettingsManager.SaveSettings();
                }
            }
        }

        //checks the entered Groq key with a free model listing call and saves it when valid,
        //mirrors the OpenAI panel handler
        [SupportedOSPlatform("windows10.0.10240")]
        private async void GroqPanel_Validating(object sender, CancelEventArgs e)
        {
            if (GroqPanel.Visible)
            {
                if (GroqAPIKeyTextBox.Text.Length > 1)
                {
                    //we need to see if the entered key is valid
                    if (!await Groq.ValidateApiKeyAsync(GroqAPIKeyTextBox.Text))
                    {
                        MessageBox.Show("Invalid Groq API key. Value cleared", "Groq API key error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        GroqAPIKeyTextBox.Text = "";
                        e.Cancel = true;
                    }
                    else
                    {
                        //might as well save since its a valid key
                        UserSettingsManager.Settings.GroqAPIKey = GroqAPIKeyTextBox.Text;
                        UserSettingsManager.SaveSettings();
                    }
                }
                else
                {
                    //its empty so lets save it
                    UserSettingsManager.Settings.GroqAPIKey = "";
                    UserSettingsManager.SaveSettings();
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void GroqTestButton_Click(object sender, EventArgs e)
        {
            if (await Groq.ValidateApiKeyAsync(GroqAPIKeyTextBox.Text))
            {
                _bBBlog.Info("Groq API key is valid");
                MessageBox.Show("Groq API key is valid", "Groq API Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _bBBlog.Error("Groq API key is invalid");
                MessageBox.Show("Groq API key is invalid", "Groq API Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //pulls the model list for the entered (or saved) key into the Groq model selector
        [SupportedOSPlatform("windows10.0.10240")]
        private async void GroqLoadModelsButton_Click(object sender, EventArgs e)
        {
            string apiKey = GroqAPIKeyTextBox.Text;
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = UserSettingsManager.Settings.GroqAPIKey;
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                _bBBlog.Info("Groq model list load requested without an API key");
                MessageBox.Show("Enter a Groq API key first", "Groq models", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GroqLoadModelsButton.Enabled = false;
            bool loaded = await RefreshGroqModelsFromAPIAsync(apiKey);
            GroqLoadModelsButton.Enabled = true;
            if (!loaded)
            {
                MessageBox.Show("Could not load the model list. Check the API key or your internet connection.", "Groq models", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //fills the Groq model selector from Groq's model listing instead of the statically
        //configured items, the selected model is restored when it is still offered
        [SupportedOSPlatform("windows10.0.10240")]
        private async Task<bool> RefreshGroqModelsFromAPIAsync(string apiKey)
        {
            GroqPanel.Enabled = false;
            try
            {
                List<string> chatModels;
                try
                {
                    chatModels = await Groq.FetchChatModelNamesAsync(apiKey);
                }
                catch (Exception ex)
                {
                    //this runs unobserved from async void handlers, nothing may escape
                    _bBBlog.Error("Groq model fetch failed: " + ex.Message);
                    return false;
                }
                if (chatModels.Count < 1)
                {
                    _bBBlog.Info("No Groq models fetched, keeping the built-in list");
                    return false;
                }
                string previousSelection = GroqModelComboBox.Text;
                GroqModelComboBox.Items.Clear();
                foreach (string model in chatModels)
                {
                    GroqModelComboBox.Items.Add(model);
                }
                //prefer the model the user had picked, then the one from the settings, else the first
                int index = GroqModelComboBox.FindStringExact(previousSelection);
                if (index < 0)
                    index = GroqModelComboBox.FindStringExact(UserSettingsManager.Settings.GroqModel ?? "");
                if (index >= 0)
                    GroqModelComboBox.SelectedIndex = index;
                else
                    GroqModelComboBox.SelectedIndex = 0;
                //keep the list, the selector is filled from this cache on every restart
                UserSettingsManager.Settings.GroqModelListCache = chatModels;
                UserSettingsManager.SaveSettings();
                _bBBlog.Info($"Loaded {chatModels.Count} Groq models from the Groq API");
                return true;
            }
            finally
            {
                GroqPanel.Enabled = true;
            }
        }

        //asks the Groq API which reasoning effort levels the model in the Groq model selector
        //supports and fills the effort dropdown with them, the models endpoints do not report
        //capabilities so the fetch uses a rejected probe request, see Groq.FetchReasoningEffortLevelsAsync
        [SupportedOSPlatform("windows10.0.10240")]
        private async void GroqReasoningLoadButton_Click(object sender, EventArgs e)
        {
            string apiKey = GroqAPIKeyTextBox.Text;
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = UserSettingsManager.Settings.GroqAPIKey;
            }
            string model = GroqModelComboBox.Text;
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
            {
                _bBBlog.Info("Reasoning levels requested without an API key or model");
                MessageBox.Show("Select a Groq model and enter a Groq API key first", "Reasoning effort", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            GroqReasoningLoadButton.Enabled = false;
            GroqPanel.Enabled = false;
            List<string> levels;
            string fetchError;
            try
            {
                (levels, fetchError) = await Groq.FetchReasoningEffortLevelsAsync(apiKey, model);
            }
            finally
            {
                GroqReasoningLoadButton.Enabled = true;
                GroqPanel.Enabled = true;
            }
            if (levels == null)
            {
                //show Groq's own reason, a 401 for example means the key was rejected
                MessageBox.Show($"Groq did not report the reasoning levels for this model:\r\n\r\n{fetchError}", "Reasoning effort", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (levels.Count < 1)
            {
                _bBBlog.Info($"Groq reports {model} has no reasoning settings");
            }
            //remember the levels for this model, an empty list caches "no reasoning settings" too
            UserSettingsManager.Settings.GroqReasoningLevelsCache ??= [];
            UserSettingsManager.Settings.GroqReasoningLevelsCache[model] = levels;
            UserSettingsManager.SaveSettings();
            ApplyCachedGroqReasoningLevels(model);
            if (levels.Count < 1)
            {
                MessageBox.Show($"The model \"{model}\" has no reasoning effort settings.", "Reasoning effort", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _bBBlog.Info($"Loaded {levels.Count} reasoning levels for {model}");
        }

        //refills the Groq reasoning effort selector with the levels cached for a model, the current
        //pick is kept when the model also supports it. Returns false when the model was never
        //fetched, the caller then leaves the list as it is until Load levels probes the API
        [SupportedOSPlatform("windows10.0.10240")]
        private bool ApplyCachedGroqReasoningLevels(string model)
        {
            Dictionary<string, List<string>> cache = UserSettingsManager.Settings.GroqReasoningLevelsCache;
            if (cache == null || string.IsNullOrEmpty(model) || !cache.TryGetValue(model, out List<string> levels) || levels == null)
            {
                return false;
            }
            string previousSelection = GroqReasoningComboBox.Text;
            GroqReasoningComboBox.Items.Clear();
            GroqReasoningComboBox.Items.Add("Auto (model default)");
            foreach (string level in levels)
            {
                GroqReasoningComboBox.Items.Add(level);
            }
            int index = GroqReasoningComboBox.FindStringExact(previousSelection);
            GroqReasoningComboBox.SelectedIndex = index >= 0 ? index : 0;
            return true;
        }

        //when the user picks another Groq model, show the reasoning levels cached for it, no API call
        [SupportedOSPlatform("windows10.0.10240")]
        private void GroqModelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //a model that was never probed has no known levels. Leaving the list of the previous
            //model up makes it look like Groq answered for this one, so go back to the built-in items
            if (!ApplyCachedGroqReasoningLevels(GroqModelComboBox.Text))
            {
                string previousSelection = GroqReasoningComboBox.Text;
                GroqReasoningComboBox.Items.Clear();
                GroqReasoningComboBox.Items.AddRange(new object[] { "Auto (model default)", "Minimal", "Low", "Medium", "High" });
                int index = GroqReasoningComboBox.FindStringExact(previousSelection);
                GroqReasoningComboBox.SelectedIndex = index >= 0 ? index : 0;
                _bBBlog.Info($"No cached reasoning levels for {GroqModelComboBox.Text}, using the built-in list until Load levels");
            }
        }

        //sends one small real chat request with the values currently in the Groq panel so the
        //provider itself confirms the key, model, temperature, max tokens and reasoning effort
        //all work together, obvious local range errors are reported without calling the API
        [SupportedOSPlatform("windows10.0.10240")]
        private async void GroqTestSettingsButton_Click(object sender, EventArgs e)
        {
            string apiKey = GroqAPIKeyTextBox.Text;
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = UserSettingsManager.Settings.GroqAPIKey;
            }
            string model = GroqModelComboBox.Text;
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
            {
                _bBBlog.Info("Groq settings test requested without an API key or model");
                MessageBox.Show("Enter a Groq API key and select a model first", "Groq settings test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //empty boxes fall back to the saved values, the parse helpers reject junk input too
            float temperature = ParseFloatOrDefault(GroqTemperatureTextBox.Text, UserSettingsManager.Settings.GroqTemperature);
            int maxTokens = ParseIntOrDefault(GroqMaxTokensTextBox.Text, UserSettingsManager.Settings.GroqMaxTokens);
            if (temperature < 0 || temperature > 2)
            {
                _bBBlog.Info("Groq settings test blocked by out of range temperature: " + temperature);
                MessageBox.Show("Temperature must be between 0 and 2", "Groq settings test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (maxTokens < 1)
            {
                _bBBlog.Info("Groq settings test blocked by invalid max tokens: " + maxTokens);
                MessageBox.Show("Max tokens must be at least 1", "Groq settings test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string reasoningEffort = GroqReasoningComboBox.SelectedIndex > 0 ? GroqReasoningComboBox.Text : "";
            GroqTestSettingsButton.Enabled = false;
            GroqPanel.Enabled = false;
            (bool success, string error) result;
            try
            {
                result = await Groq.TestSettingsAsync(apiKey, model, temperature, maxTokens, reasoningEffort);
            }
            finally
            {
                GroqTestSettingsButton.Enabled = true;
                GroqPanel.Enabled = true;
            }
            if (result.success)
            {
                _bBBlog.Info("Groq settings test passed");
                MessageBox.Show("Groq accepted all the current settings.", "Groq settings test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _bBBlog.Error("Groq settings test failed: " + result.error);
                MessageBox.Show("Groq rejected these settings:\r\n\r\n" + result.error, "Groq settings test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void AzurePanel_Validating(object sender, CancelEventArgs e)
        {
            if (AzurePanel.Visible)
            {
                if (AzureAPIKeyTextBox.Text.Length > 1)
                {
                    if (!await TTSGetAzureVoices())
                    {
                        MessageBox.Show("Invalid Azure API key. Value cleared", "Azure API key error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AzureAPIKeyTextBox.Text = "";
                        e.Cancel = true;
                    }
                    else
                    {
                        //might as well save since its a valid key
                        UserSettingsManager.Settings.AzureAPIKeyTextBox = AzureAPIKeyTextBox.Text;
                        UserSettingsManager.SaveSettings();
                    }
                }
                else
                {
                    //its empty so lets save it
                    UserSettingsManager.Settings.AzureAPIKeyTextBox = "";
                    UserSettingsManager.SaveSettings();
                }

            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void ElevenLabsPanel_Validating(object sender, CancelEventArgs e)
        {
            if (ElevenLabsPanel.Visible)
            {
                if (ElevenlabsAPIKeyTextBox.Text.Length > 1)
                {
                    //we need to see if the entered key is valid
                    //call test api key for elevenlabs
                    //we also need a reset
                    if (elevenLabsApi != null)
                        elevenLabsApi = null;
                    elevenLabsApi ??= new(ElevenlabsAPIKeyTextBox.Text);

                    if (!await elevenLabsApi.ElevenLabsAPIKeyTest())
                    {

                        MessageBox.Show("Invalid ElevenLabs API key. Value cleared", "ElevenLabs API key error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ElevenlabsAPIKeyTextBox.Text = "";
                        e.Cancel = true;
                    }
                    else
                    {
                        //might as well save since its a valid key
                        UserSettingsManager.Settings.ElevenLabsAPIkey = ElevenlabsAPIKeyTextBox.Text;
                        UserSettingsManager.Settings.ElevenLabsModel = ElevenLabsModelComboBox.Text;
                        UserSettingsManager.SaveSettings();
                    }
                }
                else
                {
                    //its empty so lets save it 
                    UserSettingsManager.Settings.ElevenLabsAPIkey = "";
                    UserSettingsManager.SaveSettings();
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private async void AuthorizeBotTwitch_Click(object sender, EventArgs e)
        {
            //lets not block everything, but lets try get a Twitch Auth token.
            //This is done by spawning a browser where the user has to authorize (implicit grant) 
            //the application. 
            TwitchAPIESub twitchAPI = new();
            //see https://dev.twitch.tv/docs/authentication/scopes/ and https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessage
            //API events:
            //channel.send.message ("user:write:chat")
            //eventsub events:
            //channel.chat.message (user:read:chat) to read chat
            //channel.subscribe (channel:read:subscriptions) to get subscription events
            //channel.subscription.gift (channel:read:subscriptions) to get gifted sub events
            //channel.subscription.message (channel:read:subscriptions) to get sub message events
            //channel.cheer (bits:read) to get information on cheered bits
            //channel.follow (moderator:read:followers) to get who followed a channel
            //channel.channel_points_automatic_reward_redemption.add (channel:read:redemptions) to get automatic reward redemptions by MyTwitchNotableViewers
            //channel.channel_points_custom_reward_redemption.add (channel:read:redemptions) to get custom reward redemptions by MyTwitchNotableViewers

            try
            {
                var twitchAPIResult = await twitchAPI.GetTwitchAuthToken([
                        //API scope to send text to chat
                        "user:write:chat"
                    //EventSub scopes for subscription types to read chat, get subscription events, read when people cheer (bits) and follower events
                    ]);

                if (!twitchAPIResult)
                {
                    _bBBlog.Error("Issue with getting auth token. Check logs for more information.");
                    MessageBox.Show($"Issue with getting Auth token. Check logs for more information.", "Twitch Authorization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    TwitchBotAuthKey.Text = twitchAPI.TwitchAccessToken;
                    //auto-fill the bot account name that was just authorized
                    if (!string.IsNullOrWhiteSpace(twitchAPI.TwitchAuthorizedUserName))
                        TwitchBotName.Text = twitchAPI.TwitchAuthorizedUserName;
                }
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Twitch Bot authorization failed: " + ex.Message);
                MessageBox.Show("Twitch authorization failed: " + ex.Message, "Twitch Authorization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TTSAudioOutputComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //get the device volume
            float? deviceVolume = GetDeviceVolume(TTSAudioOutputComboBox.Text);
            if (deviceVolume != null)
            {
                int volumeValue = (int)(deviceVolume * 100);
                SpeakerDeviceVolumeTrackBar.Value = volumeValue;
            }
            else
            {
                SpeakerDeviceVolumeTrackBar.Value = 0;
            }
            UserSettingsManager.Settings.TTSAudioOutput = TTSAudioOutputComboBox.Text;
            UserSettingsManager.SaveSettings();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void VolumeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            personaEdited = true;
            SavePersona.Enabled = true;
            TTSVoiceLevel.Text = VolumeTrackBar.Value.ToString();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void RateTrackBar_ValueChanged(object sender, EventArgs e)
        {
            personaEdited = true;
            SavePersona.Enabled = true;
            TTSSpeedLevel.Text = RateTrackBar.Value.ToString();
        }

        public static float? GetDeviceVolume(string deviceName)
        {
            var enumerator = new MMDeviceEnumerator();
            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (endpoint.FriendlyName.Contains(deviceName))
                {
                    return endpoint.AudioEndpointVolume.MasterVolumeLevelScalar;
                }
            }
            return null; // Device not found or an error occurred
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void SpeakerDeviceVolumeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            OutputVolumeLabel.Text = SpeakerDeviceVolumeTrackBar.Value.ToString() + "%";
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void PitchTrackBar_ValueChanged(object sender, EventArgs e)
        {
            personaEdited = true;
            SavePersona.Enabled = true;
            _bBBlog.Debug("Pitch trackbar value changed to " + PitchTrackBar.Value);
            _bBBlog.Debug("Pitch smallchange: " + PitchTrackBar.SmallChange + " largechange: " + PitchTrackBar.LargeChange + " tickfrequency: " + PitchTrackBar.TickFrequency);
            TTSPitchLevel.Text = PitchTrackBar.Value.ToString();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void PitchTrackBar_ScrollSnapToTen(object sender, EventArgs e)
        {
            // Calculate the nearest multiple of 10
            int value = PitchTrackBar.Value;
            int remainder = value % 10;
            if (remainder != 0)
            {
                // Adjust the value to the nearest multiple of 10
                if (remainder < 5)
                {
                    // If the remainder is less than 5, subtract it from the value
                    PitchTrackBar.Value -= remainder;
                }
                else
                {
                    // If the remainder is 5 or more, add the difference to reach the next multiple of 10
                    PitchTrackBar.Value += (10 - remainder);
                }
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void ElevenLabsModelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bBBlog.Info("ElevenLabs model changed to " + ElevenLabsModelComboBox.Text);
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchAuthServerConfig_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bBBlog.Info("Twitch Auth server config changed to " + TwitchAuthServerConfig.Text);
            UserSettingsManager.Settings.TwitchAuthServerConfig = TwitchAuthServerConfig.Text;
            UserSettingsManager.SaveSettings();
        }
    }
}
