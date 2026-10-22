using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BanterBrain_Buddy
{
    public partial class TwitchLLMCustomLanguage : Form
    {
        //set logger
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private TwitchLLMResponseLanguage TwitchLLMLanguage;
        private bool TwitchLLMTextChanged = false;
        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        [SupportedOSPlatform("windows10.0.10240")]
        public TwitchLLMCustomLanguage()
        {

            InitializeComponent();
            DisableEventHandlers();

        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void DisableEventHandlers()
        {
            _bBBlog.Debug("Disabling event handlers for text changes");
            //lets disable teh eventhandlers
            MinBitsNoMessageTextBox.TextChanged -= MinBitsNoMessageTextBox_TextChanged;
            MinBitsWithMessageTextBox.TextChanged -= MinBitsWithMessageTextBox_TextChanged;
            ChanPointsNoMessageTextBox.TextChanged -= ChanPointsNoMessageTextBox_TextChanged;
            ChanPointsWithMessageTextBox.TextChanged -= ChanPointsWithMessageTextBox_TextChanged;
            GiftedOneSubTextBox.TextChanged -= GiftedOneSubTextBox_TextChanged;
            GiftedMultipleSubsTextBox.TextChanged -= GiftedMultipleSubsTextBox_TextChanged;
            FirstMonthSubTextBox.TextChanged -= FirstMonthSubTextBox_TextChanged;
            ResubNoMessageTextBox.TextChanged -= ResubNoMessageTextBox_TextChanged;
            ResubMessageTextBox.TextChanged -= ResubMessageTextBox_TextChanged;

            BitsCheeredIntermediaryTextBox.TextChanged -= BitsCheeredIntermediaryTextBox_TextChanged;
            ChannelPointsRedeemedIntermediaryTextBox.TextChanged -= ChannelPointsRedeemedIntermediaryTextBox_TextChanged;
            ResubscribedIntermediaryTextBox.TextChanged -= ResubscribedIntermediaryTextBox_TextChanged;
            ChatMessageIntermediaryTextBox.TextChanged -= ChatMessageIntermediaryTextBox_TextChanged;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void EnableEventHandlers()
        {
            _bBBlog.Debug("Enabling event handlers for text changes");
            MinBitsNoMessageTextBox.TextChanged += MinBitsNoMessageTextBox_TextChanged;
            MinBitsWithMessageTextBox.TextChanged += MinBitsWithMessageTextBox_TextChanged;
            ChanPointsNoMessageTextBox.TextChanged += ChanPointsNoMessageTextBox_TextChanged;
            ChanPointsWithMessageTextBox.TextChanged += ChanPointsWithMessageTextBox_TextChanged;
            GiftedOneSubTextBox.TextChanged += GiftedOneSubTextBox_TextChanged;
            GiftedMultipleSubsTextBox.TextChanged += GiftedMultipleSubsTextBox_TextChanged;
            FirstMonthSubTextBox.TextChanged += FirstMonthSubTextBox_TextChanged;
            ResubNoMessageTextBox.TextChanged += ResubNoMessageTextBox_TextChanged;
            ResubMessageTextBox.TextChanged += ResubMessageTextBox_TextChanged;

            BitsCheeredIntermediaryTextBox.TextChanged += BitsCheeredIntermediaryTextBox_TextChanged;
            ChannelPointsRedeemedIntermediaryTextBox.TextChanged += ChannelPointsRedeemedIntermediaryTextBox_TextChanged;
            ResubscribedIntermediaryTextBox.TextChanged += ResubscribedIntermediaryTextBox_TextChanged;
            ChatMessageIntermediaryTextBox.TextChanged += ChatMessageIntermediaryTextBox_TextChanged;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void LoadClassIntoText()
        {
            _bBBlog.Info($"Twitch LLM language file loaded with language: {TwitchLLMLanguage.Language}");
            //twitch messages
            MinBitsNoMessageTextBox.Text = TwitchLLMLanguage.CheerDefaultNoMessage;
            MinBitsWithMessageTextBox.Text = TwitchLLMLanguage.CheerWithMessage;
            ChanPointsNoMessageTextBox.Text = TwitchLLMLanguage.ChannelPointDefaultNoMessage;
            ChanPointsWithMessageTextBox.Text = TwitchLLMLanguage.ChannelPointWithMessage;
            GiftedOneSubTextBox.Text = TwitchLLMLanguage.GiftedSingleSub;
            GiftedMultipleSubsTextBox.Text = TwitchLLMLanguage.GiftedMultipleSubs;
            FirstMonthSubTextBox.Text = TwitchLLMLanguage.SubscribeFirstTimeThanks;
            ResubMessageTextBox.Text = TwitchLLMLanguage.SubscribeMonthsMessage;
            ResubNoMessageTextBox.Text = TwitchLLMLanguage.SubscribeMonthsNoMessage;

            //LLM intermediary messages
            BitsCheeredIntermediaryTextBox.Text = TwitchLLMLanguage.ChannelPointTalkToLLM;
            ChannelPointsRedeemedIntermediaryTextBox.Text = TwitchLLMLanguage.CheerTalkToLLM;
            ResubscribedIntermediaryTextBox.Text = TwitchLLMLanguage.SubscribeResubThanksWithMessageLLM;
            ChatMessageIntermediaryTextBox.Text = TwitchLLMLanguage.ChatMessageResponseLLM;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchLLMCustomLanguage_Shown(object sender, EventArgs e)
        {
            _bBBlog.Info("Showing custom TwitchLLM language file, loading current settings");
            //load current settings
            var tmpFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\CustomTwitchLLMLanguage.json";
            if (!File.Exists(tmpFile))
            {
                //create it from English so first use does not crash
                _bBBlog.Debug("Twitch LLM custom language file not found, creating it from English");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tmpFile));
                    var sourcefolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var englishFile = sourcefolder + "\\TwitchLLMLanguageFiles\\English.json";
                    if (File.Exists(englishFile))
                        File.Copy(englishFile, tmpFile);
                    else
                        _bBBlog.Error($"English language template {englishFile} not found, Error!");
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Could not create custom Twitch LLM language file: " + ex.Message);
                }
            }

            if (!File.Exists(tmpFile))
            {
                _bBBlog.Error($"Twitch LLM language {UserSettingsManager.Settings.TwitchLLMLanguageComboBox} file not found, Error!");
                MessageBox.Show("Custom language file not found and could not be created from the English template.", "Twitch LLM Language", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                EnableEventHandlers();
                return;
            }

            _bBBlog.Debug($"Twitch LLM language {UserSettingsManager.Settings.TwitchLLMLanguageComboBox} file found, loading it.");
            using var sr = new StreamReader(tmpFile);
            var tmpString = sr.ReadToEnd();

            TwitchLLMLanguage = JsonConvert.DeserializeObject<TwitchLLMResponseLanguage>(tmpString) ?? new();

            LoadClassIntoText();

            //loading done, enable the event handlers   
            EnableEventHandlers();

        }

        private void MinBitsNoMessageTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void MinBitsWithMessageTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void ChanPointsNoMessageTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void ChanPointsWithMessageTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void GiftedOneSubTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void GiftedMultipleSubsTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void FirstMonthSubTextBox_TextChanged(object sender, EventArgs e)
        {
            _bBBlog.Debug("First month sub text changed");
            TwitchLLMTextChanged = true;
        }

        private void ResubNoMessageTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void ResubMessageTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void BitsCheeredIntermediaryTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void ChannelPointsRedeemedIntermediaryTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void ResubscribedIntermediaryTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        private void ChatMessageIntermediaryTextBox_TextChanged(object sender, EventArgs e)
        {
            TwitchLLMTextChanged = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void SaveCurrentLanguage()
        {
            var result = MessageBox.Show(" Do you want to save the custom language?", "Save Custom Language Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                //when the initial load failed the class is null, saving would crash the FormClosing handler
                if (TwitchLLMLanguage == null)
                {
                    _bBBlog.Error("No language loaded, cannot save the custom language");
                    TwitchLLMTextChanged = false;
                    return;
                }
                //we save the current language back into the class and then file
                _bBBlog.Info("Saving current language settings");
                TwitchLLMLanguage.CheerDefaultNoMessage = MinBitsNoMessageTextBox.Text;
                TwitchLLMLanguage.CheerWithMessage = MinBitsWithMessageTextBox.Text;
                TwitchLLMLanguage.ChannelPointDefaultNoMessage = ChanPointsNoMessageTextBox.Text;
                TwitchLLMLanguage.ChannelPointWithMessage = ChanPointsWithMessageTextBox.Text;
                TwitchLLMLanguage.GiftedSingleSub = GiftedOneSubTextBox.Text;
                TwitchLLMLanguage.GiftedMultipleSubs = GiftedMultipleSubsTextBox.Text;
                TwitchLLMLanguage.SubscribeFirstTimeThanks = FirstMonthSubTextBox.Text;
                TwitchLLMLanguage.SubscribeMonthsNoMessage = ResubNoMessageTextBox.Text;
                TwitchLLMLanguage.SubscribeMonthsMessage = ResubMessageTextBox.Text;

                TwitchLLMLanguage.ChannelPointTalkToLLM = BitsCheeredIntermediaryTextBox.Text;
                TwitchLLMLanguage.CheerTalkToLLM = ChannelPointsRedeemedIntermediaryTextBox.Text;
                TwitchLLMLanguage.SubscribeResubThanksWithMessageLLM = ResubscribedIntermediaryTextBox.Text;
                TwitchLLMLanguage.ChatMessageResponseLLM = ChatMessageIntermediaryTextBox.Text;

                //save the class back to the custom language file
                var tmpFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\CustomTwitchLLMLanguage.json";
                _bBBlog.Debug($"Twitch LLM language {UserSettingsManager.Settings.TwitchLLMLanguageComboBox} file found, saving it.");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tmpFile));
                    using var sw = new StreamWriter(tmpFile);
                    sw.Write(JsonConvert.SerializeObject(TwitchLLMLanguage));
                }
                catch (Exception ex)
                {
                    //a locked or unwritable file must not throw out of the closing event
                    _bBBlog.Error("Could not save the custom language file: " + ex.Message);
                    MessageBox.Show("Could not save the custom language file: " + ex.Message, "Twitch LLM Language", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            TwitchLLMTextChanged = false;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchLLMLoadLanguage()
        {
            _bBBlog.Info("Current language has not been changed, loading new language");
            string sourcefolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string tmpFile;

            tmpFile = sourcefolder + $"\\TwitchLLMLanguageFiles\\{TwitchLLMLanguageComboBox.Text}.json";
            if (!File.Exists(tmpFile))
            {
                _bBBlog.Error($"Twitch LLM language {TwitchLLMLanguageComboBox.Text} file not found at {tmpFile}, Error!");
                MessageBox.Show($"Language file for {TwitchLLMLanguageComboBox.Text} was not found.", "Twitch LLM Language", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _bBBlog.Debug($"Twitch LLM language {TwitchLLMLanguageComboBox.Text} file found, loading it.");
            using var sr = new StreamReader(tmpFile);
            var tmpString = sr.ReadToEnd();

            TwitchLLMLanguage = JsonConvert.DeserializeObject<TwitchLLMResponseLanguage>(tmpString) ?? new();
            _bBBlog.Info($"Twitch LLM language file loaded with language: {TwitchLLMLanguage.Language}");
            DisableEventHandlers();
            LoadClassIntoText();
            EnableEventHandlers();
            TwitchLLMTextChanged = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchLLMLanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //be sure that the selected language isnt empty
            //this should be impossible, but just in case
            if (TwitchLLMLanguageComboBox.Text == "")
            {
                _bBBlog.Error("Selected language is empty, error!");
                return;
            }

            //we load the selected language
            _bBBlog.Info($"Selected language: {TwitchLLMLanguageComboBox.Text}");
            if (TwitchLLMTextChanged)
            {
                _bBBlog.Info("Current language has been changed, saving it before loading new language");
                SaveCurrentLanguage();
            }
            TwitchLLMLoadLanguage();
        }

        [SupportedOSPlatform("windows10.0.10240")]
        private void TwitchLLMCustomLanguage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (TwitchLLMTextChanged)
            {
                _bBBlog.Info("Current language has been changed, saving it before loading new language");
                SaveCurrentLanguage();
            }
        }
    }
}
