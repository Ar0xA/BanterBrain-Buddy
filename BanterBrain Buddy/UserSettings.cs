using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace BanterBrain_Buddy
{
    public class UserSettings
    {
        //alright since Settings.settings is being a bitch, we do it ourselves in a json file
        public string VoiceInput {get;set;}
        public string PTTHotkey {get;set;}
        public string GPTModel {get;set;}
        public string GPTAPIKey {get;set;}
        public string TwitchBotName {get;set;}
        public string TwitchAccessToken {get;set;}
        public string TwitchChannel {get;set;}
        public string TwitchCommandTrigger {get;set;}
        public string TwitchTestSendText {get;set;}
        public bool TwitchSendTextCheckBox {get;set;}
        public int TwitchChatCommandDelay {get;set;}
        public bool TwitchNeedsFollower {get;set;}
        public bool TwitchNeedsSubscriber {get;set;}
        public int TwitchMinBits {get;set;}
        public bool TwitchSubscribed {get;set;}
        public bool TwitchCommunitySubs {get;set;}
        public bool TwitchGiftedSub { get;set;}
        public string AzureAPIKeyTextBox {get;set;}
        public string AzureRegionTextBox {get;set;}
        public string AzureLanguageComboBox { get; set; }
        public bool TwitchEnable { get; set; }
        public bool TwitchCheckAuthAtStartup { get; set; }
        public bool TwitchReadChatCheckBox { get; set; }
        public bool TwitchCheerCheckbox { get; set; }
        public bool TTSAPIKeyTextBoxEnabled { get; set; }
        public bool TTSRegionTextBoxEnabled { get; set; }
        public bool TwitchChannelPointCheckBox { get; set; }
        public string TwitchCustomRewardName { get; set; }
        public int GPTMaxTokens { get; set; }
        public float GPTTemperature { get; set; }
        //reasoning models (gpt-5 and newer) burn their budget on hidden reasoning tokens before
        //answering, this picks the reasoning effort sent with the request, empty means the model default
        public string GPTReasoningEffortComboBox { get; set; }
        //legacy: replaced by GPTReasoningEffortComboBox, kept so old settings files can still be
        //read and migrated to "Minimal" by the settings form
        public bool GPTNoReasoningCheckBox { get; set; }
        //the model list fetched from the OpenAI API, kept so the settings selector stays filled
        //across restarts, the Load models button refreshes it
        public List<string> GPTModelListCache { get; set; }
        //the reasoning effort levels probed from the OpenAI API per model (key = model name),
        //an empty list caches that the model has no reasoning settings, Load levels refreshes it
        public Dictionary<string, List<string>> GPTReasoningLevelsCache { get; set; }
        public string TTSNativeVoiceSelected { get; set; }
        public string STTSelectedProvider { get; set; }
        public string MainSelectedPersona { get; set; }
        public string TTSAudioOutput { get; set; }
        public string SelectedLLM { get; set; }
        public string TwitchChatPersona { get; set; }
        public string TwitchChannelPointPersona { get; set; }
        public string TwitchCheeringPersona { get; set; }
        public string TwitchSubscriptionPersona { get; set; }
        public bool TwitchAutoStart { get; set; }
        public string ElevenLabsAPIkey { get; set; }
        public string OllamaURI { get; set; }
        public string OllamaSelectedModel { get; set; }
        public bool UseOllamaLLMCheckBox { get; set; }
        public string OllamaResponseLengthComboBox { get; set; }
        //let a reasoning model think before it answers. Off by default: reasoning eats the whole
        //context window of small local models and leaves an empty answer behind. The settings UI
        //shows this inverted as a "Disable reasoning" checkbox, same as the OpenAI one.
        public bool OllamaThinkingCheckBox { get; set; }
        //num_ctx in tokens, 0 keeps the Ollama server default
        public int OllamaContextTokens { get; set; }
        //Groq (OpenAI compatible chat API)
        public string GroqAPIKey { get; set; }
        public string GroqModel { get; set; }
        public float GroqTemperature { get; set; }
        public int GroqMaxTokens { get; set; }
        //reasoning effort for Groq reasoning models (qwen3, gpt-oss, ...), empty means the model default
        public string GroqReasoningEffortComboBox { get; set; }
        //last model list fetched from Groq, fills the settings selector across restarts
        public List<string> GroqModelListCache { get; set; }
        //reasoning effort levels per Groq model, an empty list caches "no reasoning settings"
        public Dictionary<string, List<string>> GroqReasoningLevelsCache { get; set; }
        public string TwitchChatSound { get; set; }
        public bool TwitchChatSoundCheckBox { get; set; }
        public bool TwitchChannelSoundCheckBox { get; set; }
        public string TwitchChannelSound { get; set; }
        public bool TwitchCheeringSoundCheckBox { get; set; }
        public string TwitchCheeringSound { get; set; }
        public bool TwitchSubscriptionSoundCheckBox { get; set; }
        public string TwitchSubscriptionSoundTextBox { get; set; }
        public string TwitchResponseToChatDelayTextBox { get; set; }
        public bool TwitchResponseToChatCheckBox { get; set; }
        public bool TwitchSubscriptionTTSResponseOnlyRadioButton { get; set; }
        public bool TwitchCheeringTTSResponseOnlyRadioButton { get; set; }
        public bool TwitchChannelPointTTSResponseOnlyRadioButton { get; set; }
        public bool TwitchChatTTSResponseOnlyRadioButton { get; set; }
        public string NativeSpeechRecognitionLanguageComboBox { get; set; }
        public string WhisperSpeechRecognitionComboBox { get; set; }
        public string TwitchAuthServerConfig { get; set; }
        public string WebsourceServer { get; set; }
        public bool WebsourceServerEnable { get; set; }
        public bool DelayFinishToChatcCheckBox { get; set; }
        public string TwitchDelayMessageTextBox { get; set; }
        public string StreamerNameTextBox { get; set; }
        public string LogDir { get; set; }
        public string TwitchBotAuthKey { get; set; }
        public string TwitchLLMLanguageComboBox { get; set; }
        public string ElevenLabsModel { get; set; }
        public bool BadWordFilter { get; set; }
        public bool TwitchEnableCheckbox { get; set; }

    }

    public class SettingsManager
    {
        private static readonly Lazy<SettingsManager> instance = new(() => new SettingsManager());
        private UserSettings settings; 
        private readonly string settingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\usersettings.json";
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private SettingsManager()
        {
            _bBBlog.Info("SettingsManager created");
            LoadSettings();
        }

        //these are needed to be globally referenced so you dont have to keep creating the object
        public static SettingsManager Instance => instance.Value;
        public UserSettings Settings => settings;

        public void LoadSettings()
        {
            
            if (File.Exists(settingsFilePath))
            {
                _bBBlog.Info("Appdata settings found. Loading settings from file");
                try
                {
                    var json = File.ReadAllText(settingsFilePath);
                    settings = JsonConvert.DeserializeObject<UserSettings>(json);
                    if (settings == null)
                    {
                        _bBBlog.Error("Settings file parsed to null, starting with fresh default settings");
                        settings = new UserSettings();
                    }
                }
                catch (Exception ex)
                {
                    //a corrupt (or locked) settings file must never take down the Lazy singleton
                    _bBBlog.Error("Could not read settings file, starting with fresh default settings: " + ex.Message);
                    settings = new UserSettings();
                }
            }
            else
            {
                _bBBlog.Info("Appdata settings file not found, creating new settings object");
                settings = new UserSettings();
            }
            HealNullStrings();
        }

        //older settings files can miss string properties entirely, code all over the app reads
        //.Length / StartsWith on them, so a missing string is healed to empty right at the source
        private void HealNullStrings()
        {
            settings ??= new UserSettings();
            foreach (PropertyInfo property in typeof(UserSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType == typeof(string) && property.CanWrite && property.GetValue(settings) == null)
                {
                    _bBBlog.Debug($"Healing null setting '{property.Name}' to empty string");
                    property.SetValue(settings, "");
                }
            }
        }

        public bool NewSettingsFileExists()
        {
            if (File.Exists(settingsFilePath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SaveSettings()
        {
            _bBBlog.Info("Saving settings to file");
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(settingsFilePath, json);
        }

        //here we set the "sane" default settings incase we ever need to restore or start a new settings file
        public void SetDefaultSettings()
        {
            if (!File.Exists(settingsFilePath))
            {
                _bBBlog.Info("Creating new settings file with default settings");
                settings ??= new UserSettings();

                //general
                settings.PTTHotkey = "LControlKey + D";
                settings.StreamerNameTextBox = "Streamer";

                //twitch stuff
                settings.TwitchCommandTrigger = "$BBB";
                settings.TwitchTestSendText = "Hello! I am BanterBrain Buddy https://banterbrain.tv";
                settings.TwitchChatCommandDelay = 300;
                settings.TwitchMinBits = 100;
                settings.TwitchResponseToChatDelayTextBox = "1";
                settings.TwitchSubscriptionTTSResponseOnlyRadioButton = true;
                settings.TwitchCheeringTTSResponseOnlyRadioButton = true;
                settings.TwitchChannelPointTTSResponseOnlyRadioButton = true;
                settings.TwitchChatTTSResponseOnlyRadioButton = true;
                settings.TwitchAuthServerConfig = "http://localhost:8080";
                settings.TwitchDelayMessageTextBox = "Cooldown ended. GO! GO! GO!";
                settings.TwitchLLMLanguageComboBox = "English";

                //Azure
                settings.AzureLanguageComboBox = "en-US";

                //OpenAI ChatGPT
                settings.GPTModel = "gpt-5-mini";
                settings.GPTMaxTokens = 100;
                settings.GPTTemperature = 0;
                settings.GPTReasoningEffortComboBox = "";
                settings.GPTNoReasoningCheckBox = false;
                settings.GPTModelListCache = new List<string>();
                settings.GPTReasoningLevelsCache = new Dictionary<string, List<string>>();

                //OpenAI Whisper
                settings.WhisperSpeechRecognitionComboBox = "English";

                //Native 
                settings.NativeSpeechRecognitionLanguageComboBox = "en-US";

                //ollama. Ollama serves plain http locally, https makes the handshake fail
                settings.OllamaURI = "http://localhost:11434";
                settings.OllamaResponseLengthComboBox = "Normal";
                settings.OllamaThinkingCheckBox = false;
                settings.OllamaContextTokens = 0;

                //Groq
                settings.GroqModel = "llama-3.1-8b-instant";
                settings.GroqTemperature = 0.5f;
                settings.GroqMaxTokens = 100;
                settings.GroqReasoningEffortComboBox = "";
                settings.GroqModelListCache = new List<string>();
                settings.GroqReasoningLevelsCache = new Dictionary<string, List<string>>();

                //Webserversource
                settings.WebsourceServer = "http://localhost:9138";
                settings.WebsourceServerEnable = false;

                //ElevenLabs
                settings.ElevenLabsModel = "Multilingual V2 (Best Quality)";

                //log dir
                settings.LogDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\logs";
            } else
            {
                _bBBlog.Info("Settings file already exists, not creating new settings object");
            }
        }

        //we need this to be able to change the settings from the main thread
        //because the UserSettings class does not have a setter for the properties by propertyName
        public void SetValue(string propertyName, object value)
        {
            PropertyInfo property = settings.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(settings, value);
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' not found or is not writable.");
            }
        }

    }
}
