using System.Windows.Forms;

namespace BanterBrain_Buddy
{
    partial class BBB
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BBB));
            MainTab = new TabPage();
            TextLog = new RichTextBox();
            UpdateGroupBox = new GroupBox();
            VersionUpdateLabel = new Label();
            StreamerTTSNameGroupBox = new GroupBox();
            StreamerNameTextBox = new TextBox();
            StreamerPersonaGroupBox = new GroupBox();
            BroadcasterSelectedPersonaComboBox = new ComboBox();
            StreamerSTTGroupBox = new GroupBox();
            STTSelectedComboBox = new ComboBox();
            MicrophoneRecordGroupBox = new GroupBox();
            MainRecordingStart = new Button();
            TwitchEventSubStatusTextBox = new TextBox();
            TwitchEventSubStatusLabel = new Label();
            TwitchAPIStatusTextBox = new TextBox();
            TwitchStatusLabel = new Label();
            BBBTabs = new TabControl();
            StreamingSettingsTab = new TabPage();
            TwitchMockCheckBox = new CheckBox();
            TwitchLLMLanguageGroupBox = new GroupBox();
            CustomResponseButton = new Button();
            TwitchLLMLanguageComboBox = new ComboBox();
            LLMGroupSettingsGroupBox = new GroupBox();
            LLMStartNewConvo = new Button();
            LLMResponseSelecter = new ComboBox();
            label7 = new Label();
            TwitchSettingsGroupBox = new GroupBox();
            groupBox2 = new GroupBox();
            NotableViewersButton = new Button();
            groupBox1 = new GroupBox();
            TwitchBadWordFilterCheckBox = new CheckBox();
            WordFilterButton = new Button();
            TwitchResponseSettings = new GroupBox();
            TwitchResponseToChatDelayTextBox = new TextBox();
            TwitchPostResponseToChatDelayLabel = new Label();
            TwitchResponseToChatCheckBox = new CheckBox();
            TwitchSoundsSettings = new GroupBox();
            TwitchChatSoundSelectButton = new Button();
            TwitchSubscriptionSoundTextBox = new ComboBox();
            TwitchCheeringSoundTextBox = new ComboBox();
            TwitchChannelSoundTextBox = new ComboBox();
            TwitchChatSoundTextBox = new ComboBox();
            TwitchSubscriptionSoundCheckBox = new CheckBox();
            TwitchCheeringSoundCheckBox = new CheckBox();
            TwitchChannelSoundCheckBox = new CheckBox();
            TwitchChatSoundCheckBox = new CheckBox();
            TwitchAutoStart = new CheckBox();
            TwitchStartButton = new Button();
            TwitchChannelPointsSettings = new GroupBox();
            TwitchChannelPointTTSResponseOnlyRadioButton = new RadioButton();
            TwitchChannelPointTTSEverythingRadioButton = new RadioButton();
            TwitchChannelPointPersonaLabel = new Label();
            TwitchChannelPointPersonaComboBox = new ComboBox();
            TwitchCustomChannelPointNameLabel = new Label();
            TwitchCustomRewardName = new TextBox();
            TwitchChannelPointCheckBox = new CheckBox();
            TwitchEnableCheckbox = new CheckBox();
            TwitchSubscriberSettings = new GroupBox();
            TwitchSubscriptionTTSResponseOnlyRadioButton = new RadioButton();
            TwitchSubscriptionTTSEverythingRadioButton = new RadioButton();
            TwitchSubscriptionPersonaLabel = new Label();
            TwitchGiftedSub = new CheckBox();
            TwitchSubscriptionPersonaComboBox = new ComboBox();
            TwitchSubscribed = new CheckBox();
            TwitchCheerSettings = new GroupBox();
            TwitchCheeringTTSResponseOnlyRadioButton = new RadioButton();
            TwitchCheeringTTSEverythingRadioButton = new RadioButton();
            TwitchBitsPersonaLabel = new Label();
            TwitchCheeringPersonaComboBox = new ComboBox();
            TwitchCheerCheckBox = new CheckBox();
            TwitchMinimumBitsLabel = new Label();
            TwitchMinBits = new TextBox();
            TwitchChatTriggerSettings = new GroupBox();
            TwitchMessageToChatLabel = new Label();
            TwitchDelayMessageTextBox = new TextBox();
            TwitchDelayFinishToChatcCheckBox = new CheckBox();
            TwitchChatTTSResponseOnlyRadioButton = new RadioButton();
            TwitchChatTTSEverythingRadioButton = new RadioButton();
            TwitchChatPersonaLabel = new Label();
            TwitchChatPersonaComboBox = new ComboBox();
            TwitchReadChatCheckBox = new CheckBox();
            TwitchCommandDelayLabel = new Label();
            TwitchChatCommandDelay = new TextBox();
            TwitchNeedsSubscriber = new CheckBox();
            TwitchCommandTrigger = new TextBox();
            TwitchCommandTriggerLabel = new Label();
            fileToolStripMenuItem = new ToolStripMenuItem();
            ExitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            logfileDirectoryToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            downloadToolStripMenuItem = new ToolStripMenuItem();
            DiscordToolStripMenuItem = new ToolStripMenuItem();
            GithubToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1 = new MenuStrip();
            seToolStripMenuItem = new ToolStripMenuItem();
            BBBToolTip = new ToolTip(components);
            MainTab.SuspendLayout();
            UpdateGroupBox.SuspendLayout();
            StreamerTTSNameGroupBox.SuspendLayout();
            StreamerPersonaGroupBox.SuspendLayout();
            StreamerSTTGroupBox.SuspendLayout();
            MicrophoneRecordGroupBox.SuspendLayout();
            BBBTabs.SuspendLayout();
            StreamingSettingsTab.SuspendLayout();
            TwitchLLMLanguageGroupBox.SuspendLayout();
            LLMGroupSettingsGroupBox.SuspendLayout();
            TwitchSettingsGroupBox.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            TwitchResponseSettings.SuspendLayout();
            TwitchSoundsSettings.SuspendLayout();
            TwitchChannelPointsSettings.SuspendLayout();
            TwitchSubscriberSettings.SuspendLayout();
            TwitchCheerSettings.SuspendLayout();
            TwitchChatTriggerSettings.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // MainTab
            // 
            MainTab.Controls.Add(TextLog);
            MainTab.Controls.Add(UpdateGroupBox);
            MainTab.Controls.Add(StreamerTTSNameGroupBox);
            MainTab.Controls.Add(StreamerPersonaGroupBox);
            MainTab.Controls.Add(StreamerSTTGroupBox);
            MainTab.Controls.Add(MicrophoneRecordGroupBox);
            MainTab.Controls.Add(TwitchEventSubStatusTextBox);
            MainTab.Controls.Add(TwitchEventSubStatusLabel);
            MainTab.Controls.Add(TwitchAPIStatusTextBox);
            MainTab.Controls.Add(TwitchStatusLabel);
            resources.ApplyResources(MainTab, "MainTab");
            MainTab.Name = "MainTab";
            MainTab.UseVisualStyleBackColor = true;
            // 
            // TextLog
            // 
            TextLog.BackColor = System.Drawing.SystemColors.Menu;
            resources.ApplyResources(TextLog, "TextLog");
            TextLog.Name = "TextLog";
            // 
            // UpdateGroupBox
            // 
            UpdateGroupBox.Controls.Add(VersionUpdateLabel);
            resources.ApplyResources(UpdateGroupBox, "UpdateGroupBox");
            UpdateGroupBox.Name = "UpdateGroupBox";
            UpdateGroupBox.TabStop = false;
            // 
            // VersionUpdateLabel
            // 
            resources.ApplyResources(VersionUpdateLabel, "VersionUpdateLabel");
            VersionUpdateLabel.Name = "VersionUpdateLabel";
            // 
            // StreamerTTSNameGroupBox
            // 
            StreamerTTSNameGroupBox.Controls.Add(StreamerNameTextBox);
            resources.ApplyResources(StreamerTTSNameGroupBox, "StreamerTTSNameGroupBox");
            StreamerTTSNameGroupBox.Name = "StreamerTTSNameGroupBox";
            StreamerTTSNameGroupBox.TabStop = false;
            BBBToolTip.SetToolTip(StreamerTTSNameGroupBox, resources.GetString("StreamerTTSNameGroupBox.ToolTip"));
            // 
            // StreamerNameTextBox
            // 
            resources.ApplyResources(StreamerNameTextBox, "StreamerNameTextBox");
            StreamerNameTextBox.Name = "StreamerNameTextBox";
            StreamerNameTextBox.Validating += StreamerNameTextBox_Validating;
            // 
            // StreamerPersonaGroupBox
            // 
            StreamerPersonaGroupBox.Controls.Add(BroadcasterSelectedPersonaComboBox);
            resources.ApplyResources(StreamerPersonaGroupBox, "StreamerPersonaGroupBox");
            StreamerPersonaGroupBox.Name = "StreamerPersonaGroupBox";
            StreamerPersonaGroupBox.TabStop = false;
            // 
            // BroadcasterSelectedPersonaComboBox
            // 
            BroadcasterSelectedPersonaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            BroadcasterSelectedPersonaComboBox.FormattingEnabled = true;
            resources.ApplyResources(BroadcasterSelectedPersonaComboBox, "BroadcasterSelectedPersonaComboBox");
            BroadcasterSelectedPersonaComboBox.Name = "BroadcasterSelectedPersonaComboBox";
            BroadcasterSelectedPersonaComboBox.SelectedIndexChanged += BroadcasterSelectedPersonaComboBox_SelectedIndexChanged;
            // 
            // StreamerSTTGroupBox
            // 
            StreamerSTTGroupBox.Controls.Add(STTSelectedComboBox);
            resources.ApplyResources(StreamerSTTGroupBox, "StreamerSTTGroupBox");
            StreamerSTTGroupBox.Name = "StreamerSTTGroupBox";
            StreamerSTTGroupBox.TabStop = false;
            // 
            // STTSelectedComboBox
            // 
            STTSelectedComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            STTSelectedComboBox.FormattingEnabled = true;
            resources.ApplyResources(STTSelectedComboBox, "STTSelectedComboBox");
            STTSelectedComboBox.Name = "STTSelectedComboBox";
            STTSelectedComboBox.SelectedIndexChanged += STTSelectedComboBox_SelectedIndexChanged;
            // 
            // MicrophoneRecordGroupBox
            // 
            MicrophoneRecordGroupBox.Controls.Add(MainRecordingStart);
            resources.ApplyResources(MicrophoneRecordGroupBox, "MicrophoneRecordGroupBox");
            MicrophoneRecordGroupBox.Name = "MicrophoneRecordGroupBox";
            MicrophoneRecordGroupBox.TabStop = false;
            // 
            // MainRecordingStart
            // 
            MainRecordingStart.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(MainRecordingStart, "MainRecordingStart");
            MainRecordingStart.Name = "MainRecordingStart";
            MainRecordingStart.UseVisualStyleBackColor = false;
            MainRecordingStart.Click += MainRecordingStart_Click;
            // 
            // TwitchEventSubStatusTextBox
            // 
            TwitchEventSubStatusTextBox.BackColor = System.Drawing.Color.Red;
            TwitchEventSubStatusTextBox.ForeColor = System.Drawing.Color.White;
            resources.ApplyResources(TwitchEventSubStatusTextBox, "TwitchEventSubStatusTextBox");
            TwitchEventSubStatusTextBox.Name = "TwitchEventSubStatusTextBox";
            TwitchEventSubStatusTextBox.ReadOnly = true;
            // 
            // TwitchEventSubStatusLabel
            // 
            resources.ApplyResources(TwitchEventSubStatusLabel, "TwitchEventSubStatusLabel");
            TwitchEventSubStatusLabel.Name = "TwitchEventSubStatusLabel";
            // 
            // TwitchAPIStatusTextBox
            // 
            TwitchAPIStatusTextBox.BackColor = System.Drawing.Color.Red;
            TwitchAPIStatusTextBox.ForeColor = System.Drawing.Color.White;
            resources.ApplyResources(TwitchAPIStatusTextBox, "TwitchAPIStatusTextBox");
            TwitchAPIStatusTextBox.Name = "TwitchAPIStatusTextBox";
            TwitchAPIStatusTextBox.ReadOnly = true;
            // 
            // TwitchStatusLabel
            // 
            resources.ApplyResources(TwitchStatusLabel, "TwitchStatusLabel");
            TwitchStatusLabel.Name = "TwitchStatusLabel";
            // 
            // BBBTabs
            // 
            resources.ApplyResources(BBBTabs, "BBBTabs");
            BBBTabs.Controls.Add(MainTab);
            BBBTabs.Controls.Add(StreamingSettingsTab);
            BBBTabs.Name = "BBBTabs";
            BBBTabs.SelectedIndex = 0;
            BBBTabs.SelectedIndexChanged += BBBTabs_SelectedIndexChanged;
            // 
            // StreamingSettingsTab
            // 
            StreamingSettingsTab.Controls.Add(TwitchMockCheckBox);
            StreamingSettingsTab.Controls.Add(TwitchLLMLanguageGroupBox);
            StreamingSettingsTab.Controls.Add(LLMGroupSettingsGroupBox);
            StreamingSettingsTab.Controls.Add(TwitchSettingsGroupBox);
            resources.ApplyResources(StreamingSettingsTab, "StreamingSettingsTab");
            StreamingSettingsTab.Name = "StreamingSettingsTab";
            StreamingSettingsTab.UseVisualStyleBackColor = true;
            // 
            // TwitchMockCheckBox
            // 
            resources.ApplyResources(TwitchMockCheckBox, "TwitchMockCheckBox");
            TwitchMockCheckBox.Name = "TwitchMockCheckBox";
            TwitchMockCheckBox.UseVisualStyleBackColor = true;
            // 
            // TwitchLLMLanguageGroupBox
            // 
            TwitchLLMLanguageGroupBox.Controls.Add(CustomResponseButton);
            TwitchLLMLanguageGroupBox.Controls.Add(TwitchLLMLanguageComboBox);
            resources.ApplyResources(TwitchLLMLanguageGroupBox, "TwitchLLMLanguageGroupBox");
            TwitchLLMLanguageGroupBox.Name = "TwitchLLMLanguageGroupBox";
            TwitchLLMLanguageGroupBox.TabStop = false;
            // 
            // CustomResponseButton
            // 
            resources.ApplyResources(CustomResponseButton, "CustomResponseButton");
            CustomResponseButton.Name = "CustomResponseButton";
            CustomResponseButton.UseVisualStyleBackColor = true;
            CustomResponseButton.Click += CustomResponseButton_Click;
            // 
            // TwitchLLMLanguageComboBox
            // 
            TwitchLLMLanguageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchLLMLanguageComboBox.FormattingEnabled = true;
            TwitchLLMLanguageComboBox.Items.AddRange(new object[] { resources.GetString("TwitchLLMLanguageComboBox.Items"), resources.GetString("TwitchLLMLanguageComboBox.Items1"), resources.GetString("TwitchLLMLanguageComboBox.Items2"), resources.GetString("TwitchLLMLanguageComboBox.Items3") });
            resources.ApplyResources(TwitchLLMLanguageComboBox, "TwitchLLMLanguageComboBox");
            TwitchLLMLanguageComboBox.Name = "TwitchLLMLanguageComboBox";
            TwitchLLMLanguageComboBox.SelectedIndexChanged += TwitchLLMLanguageComboBox_SelectedIndexChanged;
            // 
            // LLMGroupSettingsGroupBox
            // 
            LLMGroupSettingsGroupBox.Controls.Add(LLMStartNewConvo);
            LLMGroupSettingsGroupBox.Controls.Add(LLMResponseSelecter);
            LLMGroupSettingsGroupBox.Controls.Add(label7);
            resources.ApplyResources(LLMGroupSettingsGroupBox, "LLMGroupSettingsGroupBox");
            LLMGroupSettingsGroupBox.Name = "LLMGroupSettingsGroupBox";
            LLMGroupSettingsGroupBox.TabStop = false;
            // 
            // LLMStartNewConvo
            // 
            resources.ApplyResources(LLMStartNewConvo, "LLMStartNewConvo");
            LLMStartNewConvo.Name = "LLMStartNewConvo";
            BBBToolTip.SetToolTip(LLMStartNewConvo, resources.GetString("LLMStartNewConvo.ToolTip"));
            LLMStartNewConvo.UseVisualStyleBackColor = true;
            LLMStartNewConvo.Click += LLMStartNewConvo_Click;
            // 
            // LLMResponseSelecter
            // 
            LLMResponseSelecter.DropDownStyle = ComboBoxStyle.DropDownList;
            LLMResponseSelecter.FormattingEnabled = true;
            resources.ApplyResources(LLMResponseSelecter, "LLMResponseSelecter");
            LLMResponseSelecter.Name = "LLMResponseSelecter";
            LLMResponseSelecter.SelectedIndexChanged += LLMResponseSelecter_SelectedIndexChanged;
            // 
            // label7
            // 
            resources.ApplyResources(label7, "label7");
            label7.Name = "label7";
            // 
            // TwitchSettingsGroupBox
            // 
            TwitchSettingsGroupBox.Controls.Add(groupBox2);
            TwitchSettingsGroupBox.Controls.Add(groupBox1);
            TwitchSettingsGroupBox.Controls.Add(TwitchResponseSettings);
            TwitchSettingsGroupBox.Controls.Add(TwitchSoundsSettings);
            TwitchSettingsGroupBox.Controls.Add(TwitchAutoStart);
            TwitchSettingsGroupBox.Controls.Add(TwitchStartButton);
            TwitchSettingsGroupBox.Controls.Add(TwitchChannelPointsSettings);
            TwitchSettingsGroupBox.Controls.Add(TwitchEnableCheckbox);
            TwitchSettingsGroupBox.Controls.Add(TwitchSubscriberSettings);
            TwitchSettingsGroupBox.Controls.Add(TwitchCheerSettings);
            TwitchSettingsGroupBox.Controls.Add(TwitchChatTriggerSettings);
            resources.ApplyResources(TwitchSettingsGroupBox, "TwitchSettingsGroupBox");
            TwitchSettingsGroupBox.Name = "TwitchSettingsGroupBox";
            TwitchSettingsGroupBox.TabStop = false;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(NotableViewersButton);
            resources.ApplyResources(groupBox2, "groupBox2");
            groupBox2.Name = "groupBox2";
            groupBox2.TabStop = false;
            // 
            // NotableViewersButton
            // 
            resources.ApplyResources(NotableViewersButton, "NotableViewersButton");
            NotableViewersButton.Name = "NotableViewersButton";
            NotableViewersButton.UseVisualStyleBackColor = true;
            NotableViewersButton.Click += NotableViewersButton_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(TwitchBadWordFilterCheckBox);
            groupBox1.Controls.Add(WordFilterButton);
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // TwitchBadWordFilterCheckBox
            // 
            resources.ApplyResources(TwitchBadWordFilterCheckBox, "TwitchBadWordFilterCheckBox");
            TwitchBadWordFilterCheckBox.Name = "TwitchBadWordFilterCheckBox";
            TwitchBadWordFilterCheckBox.UseVisualStyleBackColor = true;
            TwitchBadWordFilterCheckBox.CheckedChanged += BadWordFilterCheckBox_CheckedChanged;
            // 
            // WordFilterButton
            // 
            resources.ApplyResources(WordFilterButton, "WordFilterButton");
            WordFilterButton.Name = "WordFilterButton";
            WordFilterButton.UseVisualStyleBackColor = true;
            WordFilterButton.Click += WordFilterButton_Click;
            // 
            // TwitchResponseSettings
            // 
            TwitchResponseSettings.Controls.Add(TwitchResponseToChatDelayTextBox);
            TwitchResponseSettings.Controls.Add(TwitchPostResponseToChatDelayLabel);
            TwitchResponseSettings.Controls.Add(TwitchResponseToChatCheckBox);
            resources.ApplyResources(TwitchResponseSettings, "TwitchResponseSettings");
            TwitchResponseSettings.Name = "TwitchResponseSettings";
            TwitchResponseSettings.TabStop = false;
            // 
            // TwitchResponseToChatDelayTextBox
            // 
            resources.ApplyResources(TwitchResponseToChatDelayTextBox, "TwitchResponseToChatDelayTextBox");
            TwitchResponseToChatDelayTextBox.Name = "TwitchResponseToChatDelayTextBox";
            BBBToolTip.SetToolTip(TwitchResponseToChatDelayTextBox, resources.GetString("TwitchResponseToChatDelayTextBox.ToolTip"));
            TwitchResponseToChatDelayTextBox.KeyPress += TwitchResponseToChatDelayTextBox_KeyPress;
            TwitchResponseToChatDelayTextBox.Validating += TwitchResponseToChatDelayTextBox_Validating;
            // 
            // TwitchPostResponseToChatDelayLabel
            // 
            resources.ApplyResources(TwitchPostResponseToChatDelayLabel, "TwitchPostResponseToChatDelayLabel");
            TwitchPostResponseToChatDelayLabel.Name = "TwitchPostResponseToChatDelayLabel";
            // 
            // TwitchResponseToChatCheckBox
            // 
            resources.ApplyResources(TwitchResponseToChatCheckBox, "TwitchResponseToChatCheckBox");
            TwitchResponseToChatCheckBox.Name = "TwitchResponseToChatCheckBox";
            TwitchResponseToChatCheckBox.UseVisualStyleBackColor = true;
            TwitchResponseToChatCheckBox.CheckedChanged += TwitchResponseToChatCheckBox_CheckedChanged;
            // 
            // TwitchSoundsSettings
            // 
            TwitchSoundsSettings.Controls.Add(TwitchChatSoundSelectButton);
            TwitchSoundsSettings.Controls.Add(TwitchSubscriptionSoundTextBox);
            TwitchSoundsSettings.Controls.Add(TwitchCheeringSoundTextBox);
            TwitchSoundsSettings.Controls.Add(TwitchChannelSoundTextBox);
            TwitchSoundsSettings.Controls.Add(TwitchChatSoundTextBox);
            TwitchSoundsSettings.Controls.Add(TwitchSubscriptionSoundCheckBox);
            TwitchSoundsSettings.Controls.Add(TwitchCheeringSoundCheckBox);
            TwitchSoundsSettings.Controls.Add(TwitchChannelSoundCheckBox);
            TwitchSoundsSettings.Controls.Add(TwitchChatSoundCheckBox);
            resources.ApplyResources(TwitchSoundsSettings, "TwitchSoundsSettings");
            TwitchSoundsSettings.Name = "TwitchSoundsSettings";
            TwitchSoundsSettings.TabStop = false;
            // 
            // TwitchChatSoundSelectButton
            // 
            TwitchChatSoundSelectButton.Image = Properties.Resources.fileopenicon;
            resources.ApplyResources(TwitchChatSoundSelectButton, "TwitchChatSoundSelectButton");
            TwitchChatSoundSelectButton.Name = "TwitchChatSoundSelectButton";
            BBBToolTip.SetToolTip(TwitchChatSoundSelectButton, resources.GetString("TwitchChatSoundSelectButton.ToolTip"));
            TwitchChatSoundSelectButton.UseVisualStyleBackColor = true;
            TwitchChatSoundSelectButton.Click += TwitchChatSoundSelectButton_Click;
            // 
            // TwitchSubscriptionSoundTextBox
            // 
            TwitchSubscriptionSoundTextBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchSubscriptionSoundTextBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchSubscriptionSoundTextBox, "TwitchSubscriptionSoundTextBox");
            TwitchSubscriptionSoundTextBox.Name = "TwitchSubscriptionSoundTextBox";
            // 
            // TwitchCheeringSoundTextBox
            // 
            TwitchCheeringSoundTextBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchCheeringSoundTextBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchCheeringSoundTextBox, "TwitchCheeringSoundTextBox");
            TwitchCheeringSoundTextBox.Name = "TwitchCheeringSoundTextBox";
            // 
            // TwitchChannelSoundTextBox
            // 
            TwitchChannelSoundTextBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchChannelSoundTextBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchChannelSoundTextBox, "TwitchChannelSoundTextBox");
            TwitchChannelSoundTextBox.Name = "TwitchChannelSoundTextBox";
            // 
            // TwitchChatSoundTextBox
            // 
            TwitchChatSoundTextBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchChatSoundTextBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchChatSoundTextBox, "TwitchChatSoundTextBox");
            TwitchChatSoundTextBox.Name = "TwitchChatSoundTextBox";
            TwitchChatSoundTextBox.Click += TwitchChatSoundTextBox_Click;
            // 
            // TwitchSubscriptionSoundCheckBox
            // 
            resources.ApplyResources(TwitchSubscriptionSoundCheckBox, "TwitchSubscriptionSoundCheckBox");
            TwitchSubscriptionSoundCheckBox.Name = "TwitchSubscriptionSoundCheckBox";
            TwitchSubscriptionSoundCheckBox.UseVisualStyleBackColor = true;
            TwitchSubscriptionSoundCheckBox.CheckedChanged += TwitchSubscriptionSoundCheckBox_CheckedChanged;
            // 
            // TwitchCheeringSoundCheckBox
            // 
            resources.ApplyResources(TwitchCheeringSoundCheckBox, "TwitchCheeringSoundCheckBox");
            TwitchCheeringSoundCheckBox.Name = "TwitchCheeringSoundCheckBox";
            TwitchCheeringSoundCheckBox.UseVisualStyleBackColor = true;
            TwitchCheeringSoundCheckBox.CheckedChanged += TwitchCheeringSoundCheckBox_CheckedChanged;
            // 
            // TwitchChannelSoundCheckBox
            // 
            resources.ApplyResources(TwitchChannelSoundCheckBox, "TwitchChannelSoundCheckBox");
            TwitchChannelSoundCheckBox.Name = "TwitchChannelSoundCheckBox";
            TwitchChannelSoundCheckBox.UseVisualStyleBackColor = true;
            TwitchChannelSoundCheckBox.CheckedChanged += TwitchChannelSoundCheckBox_CheckedChanged;
            // 
            // TwitchChatSoundCheckBox
            // 
            resources.ApplyResources(TwitchChatSoundCheckBox, "TwitchChatSoundCheckBox");
            TwitchChatSoundCheckBox.Name = "TwitchChatSoundCheckBox";
            TwitchChatSoundCheckBox.UseVisualStyleBackColor = true;
            TwitchChatSoundCheckBox.CheckedChanged += TwitchChatSoundCheckBox_CheckedChanged;
            // 
            // TwitchAutoStart
            // 
            resources.ApplyResources(TwitchAutoStart, "TwitchAutoStart");
            TwitchAutoStart.Name = "TwitchAutoStart";
            TwitchAutoStart.UseVisualStyleBackColor = true;
            // 
            // TwitchStartButton
            // 
            TwitchStartButton.AllowDrop = true;
            resources.ApplyResources(TwitchStartButton, "TwitchStartButton");
            TwitchStartButton.Name = "TwitchStartButton";
            TwitchStartButton.UseVisualStyleBackColor = true;
            TwitchStartButton.Click += TwitchStartButton_Click;
            // 
            // TwitchChannelPointsSettings
            // 
            TwitchChannelPointsSettings.Controls.Add(TwitchChannelPointTTSResponseOnlyRadioButton);
            TwitchChannelPointsSettings.Controls.Add(TwitchChannelPointTTSEverythingRadioButton);
            TwitchChannelPointsSettings.Controls.Add(TwitchChannelPointPersonaLabel);
            TwitchChannelPointsSettings.Controls.Add(TwitchChannelPointPersonaComboBox);
            TwitchChannelPointsSettings.Controls.Add(TwitchCustomChannelPointNameLabel);
            TwitchChannelPointsSettings.Controls.Add(TwitchCustomRewardName);
            TwitchChannelPointsSettings.Controls.Add(TwitchChannelPointCheckBox);
            resources.ApplyResources(TwitchChannelPointsSettings, "TwitchChannelPointsSettings");
            TwitchChannelPointsSettings.Name = "TwitchChannelPointsSettings";
            TwitchChannelPointsSettings.TabStop = false;
            // 
            // TwitchChannelPointTTSResponseOnlyRadioButton
            // 
            resources.ApplyResources(TwitchChannelPointTTSResponseOnlyRadioButton, "TwitchChannelPointTTSResponseOnlyRadioButton");
            TwitchChannelPointTTSResponseOnlyRadioButton.Name = "TwitchChannelPointTTSResponseOnlyRadioButton";
            TwitchChannelPointTTSResponseOnlyRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchChannelPointTTSResponseOnlyRadioButton, resources.GetString("TwitchChannelPointTTSResponseOnlyRadioButton.ToolTip"));
            TwitchChannelPointTTSResponseOnlyRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchChannelPointTTSEverythingRadioButton
            // 
            resources.ApplyResources(TwitchChannelPointTTSEverythingRadioButton, "TwitchChannelPointTTSEverythingRadioButton");
            TwitchChannelPointTTSEverythingRadioButton.Name = "TwitchChannelPointTTSEverythingRadioButton";
            TwitchChannelPointTTSEverythingRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchChannelPointTTSEverythingRadioButton, resources.GetString("TwitchChannelPointTTSEverythingRadioButton.ToolTip"));
            TwitchChannelPointTTSEverythingRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchChannelPointPersonaLabel
            // 
            resources.ApplyResources(TwitchChannelPointPersonaLabel, "TwitchChannelPointPersonaLabel");
            TwitchChannelPointPersonaLabel.Name = "TwitchChannelPointPersonaLabel";
            // 
            // TwitchChannelPointPersonaComboBox
            // 
            TwitchChannelPointPersonaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchChannelPointPersonaComboBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchChannelPointPersonaComboBox, "TwitchChannelPointPersonaComboBox");
            TwitchChannelPointPersonaComboBox.Name = "TwitchChannelPointPersonaComboBox";
            TwitchChannelPointPersonaComboBox.SelectedIndexChanged += TwitchChannelPointPersonaComboBox_SelectedIndexChanged;
            // 
            // TwitchCustomChannelPointNameLabel
            // 
            resources.ApplyResources(TwitchCustomChannelPointNameLabel, "TwitchCustomChannelPointNameLabel");
            TwitchCustomChannelPointNameLabel.Name = "TwitchCustomChannelPointNameLabel";
            // 
            // TwitchCustomRewardName
            // 
            resources.ApplyResources(TwitchCustomRewardName, "TwitchCustomRewardName");
            TwitchCustomRewardName.Name = "TwitchCustomRewardName";
            // 
            // TwitchChannelPointCheckBox
            // 
            resources.ApplyResources(TwitchChannelPointCheckBox, "TwitchChannelPointCheckBox");
            TwitchChannelPointCheckBox.Name = "TwitchChannelPointCheckBox";
            TwitchChannelPointCheckBox.UseVisualStyleBackColor = true;
            TwitchChannelPointCheckBox.Click += TwitchChannelPointCheckBox_Click;
            // 
            // TwitchEnableCheckbox
            // 
            resources.ApplyResources(TwitchEnableCheckbox, "TwitchEnableCheckbox");
            TwitchEnableCheckbox.Name = "TwitchEnableCheckbox";
            TwitchEnableCheckbox.UseVisualStyleBackColor = true;
            TwitchEnableCheckbox.CheckedChanged += TwitchEnableCheckbox_CheckedChanged;
            TwitchEnableCheckbox.Click += TwitchEnableCheckbox_Click;
            // 
            // TwitchSubscriberSettings
            // 
            TwitchSubscriberSettings.Controls.Add(TwitchSubscriptionTTSResponseOnlyRadioButton);
            TwitchSubscriberSettings.Controls.Add(TwitchSubscriptionTTSEverythingRadioButton);
            TwitchSubscriberSettings.Controls.Add(TwitchSubscriptionPersonaLabel);
            TwitchSubscriberSettings.Controls.Add(TwitchGiftedSub);
            TwitchSubscriberSettings.Controls.Add(TwitchSubscriptionPersonaComboBox);
            TwitchSubscriberSettings.Controls.Add(TwitchSubscribed);
            resources.ApplyResources(TwitchSubscriberSettings, "TwitchSubscriberSettings");
            TwitchSubscriberSettings.Name = "TwitchSubscriberSettings";
            TwitchSubscriberSettings.TabStop = false;
            // 
            // TwitchSubscriptionTTSResponseOnlyRadioButton
            // 
            resources.ApplyResources(TwitchSubscriptionTTSResponseOnlyRadioButton, "TwitchSubscriptionTTSResponseOnlyRadioButton");
            TwitchSubscriptionTTSResponseOnlyRadioButton.Name = "TwitchSubscriptionTTSResponseOnlyRadioButton";
            TwitchSubscriptionTTSResponseOnlyRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchSubscriptionTTSResponseOnlyRadioButton, resources.GetString("TwitchSubscriptionTTSResponseOnlyRadioButton.ToolTip"));
            TwitchSubscriptionTTSResponseOnlyRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchSubscriptionTTSEverythingRadioButton
            // 
            resources.ApplyResources(TwitchSubscriptionTTSEverythingRadioButton, "TwitchSubscriptionTTSEverythingRadioButton");
            TwitchSubscriptionTTSEverythingRadioButton.Name = "TwitchSubscriptionTTSEverythingRadioButton";
            TwitchSubscriptionTTSEverythingRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchSubscriptionTTSEverythingRadioButton, resources.GetString("TwitchSubscriptionTTSEverythingRadioButton.ToolTip"));
            TwitchSubscriptionTTSEverythingRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchSubscriptionPersonaLabel
            // 
            resources.ApplyResources(TwitchSubscriptionPersonaLabel, "TwitchSubscriptionPersonaLabel");
            TwitchSubscriptionPersonaLabel.Name = "TwitchSubscriptionPersonaLabel";
            // 
            // TwitchGiftedSub
            // 
            resources.ApplyResources(TwitchGiftedSub, "TwitchGiftedSub");
            TwitchGiftedSub.Name = "TwitchGiftedSub";
            BBBToolTip.SetToolTip(TwitchGiftedSub, resources.GetString("TwitchGiftedSub.ToolTip"));
            TwitchGiftedSub.UseVisualStyleBackColor = true;
            TwitchGiftedSub.Click += TwitchGiftedSub_Click;
            // 
            // TwitchSubscriptionPersonaComboBox
            // 
            TwitchSubscriptionPersonaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchSubscriptionPersonaComboBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchSubscriptionPersonaComboBox, "TwitchSubscriptionPersonaComboBox");
            TwitchSubscriptionPersonaComboBox.Name = "TwitchSubscriptionPersonaComboBox";
            TwitchSubscriptionPersonaComboBox.SelectedIndexChanged += TwitchSubscriptionPersonaComboBox_SelectedIndexChanged;
            // 
            // TwitchSubscribed
            // 
            resources.ApplyResources(TwitchSubscribed, "TwitchSubscribed");
            TwitchSubscribed.Name = "TwitchSubscribed";
            BBBToolTip.SetToolTip(TwitchSubscribed, resources.GetString("TwitchSubscribed.ToolTip"));
            TwitchSubscribed.UseVisualStyleBackColor = true;
            TwitchSubscribed.Click += TwitchSubscribed_Click;
            // 
            // TwitchCheerSettings
            // 
            TwitchCheerSettings.Controls.Add(TwitchCheeringTTSResponseOnlyRadioButton);
            TwitchCheerSettings.Controls.Add(TwitchCheeringTTSEverythingRadioButton);
            TwitchCheerSettings.Controls.Add(TwitchBitsPersonaLabel);
            TwitchCheerSettings.Controls.Add(TwitchCheeringPersonaComboBox);
            TwitchCheerSettings.Controls.Add(TwitchCheerCheckBox);
            TwitchCheerSettings.Controls.Add(TwitchMinimumBitsLabel);
            TwitchCheerSettings.Controls.Add(TwitchMinBits);
            resources.ApplyResources(TwitchCheerSettings, "TwitchCheerSettings");
            TwitchCheerSettings.Name = "TwitchCheerSettings";
            TwitchCheerSettings.TabStop = false;
            // 
            // TwitchCheeringTTSResponseOnlyRadioButton
            // 
            resources.ApplyResources(TwitchCheeringTTSResponseOnlyRadioButton, "TwitchCheeringTTSResponseOnlyRadioButton");
            TwitchCheeringTTSResponseOnlyRadioButton.Name = "TwitchCheeringTTSResponseOnlyRadioButton";
            TwitchCheeringTTSResponseOnlyRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchCheeringTTSResponseOnlyRadioButton, resources.GetString("TwitchCheeringTTSResponseOnlyRadioButton.ToolTip"));
            TwitchCheeringTTSResponseOnlyRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchCheeringTTSEverythingRadioButton
            // 
            resources.ApplyResources(TwitchCheeringTTSEverythingRadioButton, "TwitchCheeringTTSEverythingRadioButton");
            TwitchCheeringTTSEverythingRadioButton.Name = "TwitchCheeringTTSEverythingRadioButton";
            TwitchCheeringTTSEverythingRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchCheeringTTSEverythingRadioButton, resources.GetString("TwitchCheeringTTSEverythingRadioButton.ToolTip"));
            TwitchCheeringTTSEverythingRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchBitsPersonaLabel
            // 
            resources.ApplyResources(TwitchBitsPersonaLabel, "TwitchBitsPersonaLabel");
            TwitchBitsPersonaLabel.Name = "TwitchBitsPersonaLabel";
            // 
            // TwitchCheeringPersonaComboBox
            // 
            TwitchCheeringPersonaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchCheeringPersonaComboBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchCheeringPersonaComboBox, "TwitchCheeringPersonaComboBox");
            TwitchCheeringPersonaComboBox.Name = "TwitchCheeringPersonaComboBox";
            TwitchCheeringPersonaComboBox.SelectedIndexChanged += TwitchCheeringPersonaComboBox_SelectedIndexChanged;
            // 
            // TwitchCheerCheckBox
            // 
            resources.ApplyResources(TwitchCheerCheckBox, "TwitchCheerCheckBox");
            TwitchCheerCheckBox.Name = "TwitchCheerCheckBox";
            BBBToolTip.SetToolTip(TwitchCheerCheckBox, resources.GetString("TwitchCheerCheckBox.ToolTip"));
            TwitchCheerCheckBox.UseVisualStyleBackColor = true;
            TwitchCheerCheckBox.Click += TwitchCheerCheckbox_Click;
            // 
            // TwitchMinimumBitsLabel
            // 
            resources.ApplyResources(TwitchMinimumBitsLabel, "TwitchMinimumBitsLabel");
            TwitchMinimumBitsLabel.Name = "TwitchMinimumBitsLabel";
            // 
            // TwitchMinBits
            // 
            resources.ApplyResources(TwitchMinBits, "TwitchMinBits");
            TwitchMinBits.Name = "TwitchMinBits";
            TwitchMinBits.KeyPress += TwitchMinBits_KeyPress;
            TwitchMinBits.Validating += TwitchMinBits_Validating;
            // 
            // TwitchChatTriggerSettings
            // 
            TwitchChatTriggerSettings.Controls.Add(TwitchMessageToChatLabel);
            TwitchChatTriggerSettings.Controls.Add(TwitchDelayMessageTextBox);
            TwitchChatTriggerSettings.Controls.Add(TwitchDelayFinishToChatcCheckBox);
            TwitchChatTriggerSettings.Controls.Add(TwitchChatTTSResponseOnlyRadioButton);
            TwitchChatTriggerSettings.Controls.Add(TwitchChatTTSEverythingRadioButton);
            TwitchChatTriggerSettings.Controls.Add(TwitchChatPersonaLabel);
            TwitchChatTriggerSettings.Controls.Add(TwitchChatPersonaComboBox);
            TwitchChatTriggerSettings.Controls.Add(TwitchReadChatCheckBox);
            TwitchChatTriggerSettings.Controls.Add(TwitchCommandDelayLabel);
            TwitchChatTriggerSettings.Controls.Add(TwitchChatCommandDelay);
            TwitchChatTriggerSettings.Controls.Add(TwitchNeedsSubscriber);
            TwitchChatTriggerSettings.Controls.Add(TwitchCommandTrigger);
            TwitchChatTriggerSettings.Controls.Add(TwitchCommandTriggerLabel);
            resources.ApplyResources(TwitchChatTriggerSettings, "TwitchChatTriggerSettings");
            TwitchChatTriggerSettings.Name = "TwitchChatTriggerSettings";
            TwitchChatTriggerSettings.TabStop = false;
            // 
            // TwitchMessageToChatLabel
            // 
            resources.ApplyResources(TwitchMessageToChatLabel, "TwitchMessageToChatLabel");
            TwitchMessageToChatLabel.Name = "TwitchMessageToChatLabel";
            // 
            // TwitchDelayMessageTextBox
            // 
            resources.ApplyResources(TwitchDelayMessageTextBox, "TwitchDelayMessageTextBox");
            TwitchDelayMessageTextBox.Name = "TwitchDelayMessageTextBox";
            TwitchDelayMessageTextBox.Validating += TwitchDelayMessageTextBox_Validating;
            // 
            // TwitchDelayFinishToChatcCheckBox
            // 
            resources.ApplyResources(TwitchDelayFinishToChatcCheckBox, "TwitchDelayFinishToChatcCheckBox");
            TwitchDelayFinishToChatcCheckBox.Name = "TwitchDelayFinishToChatcCheckBox";
            TwitchDelayFinishToChatcCheckBox.UseVisualStyleBackColor = true;
            TwitchDelayFinishToChatcCheckBox.CheckedChanged += TwitchDelayFinishToChatcCheckBox_CheckedChanged;
            // 
            // TwitchChatTTSResponseOnlyRadioButton
            // 
            resources.ApplyResources(TwitchChatTTSResponseOnlyRadioButton, "TwitchChatTTSResponseOnlyRadioButton");
            TwitchChatTTSResponseOnlyRadioButton.Name = "TwitchChatTTSResponseOnlyRadioButton";
            TwitchChatTTSResponseOnlyRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchChatTTSResponseOnlyRadioButton, resources.GetString("TwitchChatTTSResponseOnlyRadioButton.ToolTip"));
            TwitchChatTTSResponseOnlyRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchChatTTSEverythingRadioButton
            // 
            resources.ApplyResources(TwitchChatTTSEverythingRadioButton, "TwitchChatTTSEverythingRadioButton");
            TwitchChatTTSEverythingRadioButton.Name = "TwitchChatTTSEverythingRadioButton";
            TwitchChatTTSEverythingRadioButton.TabStop = true;
            BBBToolTip.SetToolTip(TwitchChatTTSEverythingRadioButton, resources.GetString("TwitchChatTTSEverythingRadioButton.ToolTip"));
            TwitchChatTTSEverythingRadioButton.UseVisualStyleBackColor = true;
            // 
            // TwitchChatPersonaLabel
            // 
            resources.ApplyResources(TwitchChatPersonaLabel, "TwitchChatPersonaLabel");
            TwitchChatPersonaLabel.Name = "TwitchChatPersonaLabel";
            // 
            // TwitchChatPersonaComboBox
            // 
            TwitchChatPersonaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            TwitchChatPersonaComboBox.FormattingEnabled = true;
            resources.ApplyResources(TwitchChatPersonaComboBox, "TwitchChatPersonaComboBox");
            TwitchChatPersonaComboBox.Name = "TwitchChatPersonaComboBox";
            TwitchChatPersonaComboBox.SelectedIndexChanged += TwitchChatPersonaComboBox_SelectedIndexChanged;
            // 
            // TwitchReadChatCheckBox
            // 
            resources.ApplyResources(TwitchReadChatCheckBox, "TwitchReadChatCheckBox");
            TwitchReadChatCheckBox.Name = "TwitchReadChatCheckBox";
            BBBToolTip.SetToolTip(TwitchReadChatCheckBox, resources.GetString("TwitchReadChatCheckBox.ToolTip"));
            TwitchReadChatCheckBox.UseVisualStyleBackColor = true;
            TwitchReadChatCheckBox.Click += TwitchReadChatCheckBox_Click;
            // 
            // TwitchCommandDelayLabel
            // 
            resources.ApplyResources(TwitchCommandDelayLabel, "TwitchCommandDelayLabel");
            TwitchCommandDelayLabel.Name = "TwitchCommandDelayLabel";
            // 
            // TwitchChatCommandDelay
            // 
            resources.ApplyResources(TwitchChatCommandDelay, "TwitchChatCommandDelay");
            TwitchChatCommandDelay.Name = "TwitchChatCommandDelay";
            TwitchChatCommandDelay.KeyPress += TwitchChatCommandDelay_KeyPress;
            TwitchChatCommandDelay.Validating += TwitchChatCommandDelay_Validating;
            // 
            // TwitchNeedsSubscriber
            // 
            resources.ApplyResources(TwitchNeedsSubscriber, "TwitchNeedsSubscriber");
            TwitchNeedsSubscriber.Name = "TwitchNeedsSubscriber";
            BBBToolTip.SetToolTip(TwitchNeedsSubscriber, resources.GetString("TwitchNeedsSubscriber.ToolTip"));
            TwitchNeedsSubscriber.UseVisualStyleBackColor = true;
            // 
            // TwitchCommandTrigger
            // 
            resources.ApplyResources(TwitchCommandTrigger, "TwitchCommandTrigger");
            TwitchCommandTrigger.Name = "TwitchCommandTrigger";
            // 
            // TwitchCommandTriggerLabel
            // 
            resources.ApplyResources(TwitchCommandTriggerLabel, "TwitchCommandTriggerLabel");
            TwitchCommandTriggerLabel.Name = "TwitchCommandTriggerLabel";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { ExitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            resources.ApplyResources(fileToolStripMenuItem, "fileToolStripMenuItem");
            // 
            // ExitToolStripMenuItem
            // 
            ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            resources.ApplyResources(ExitToolStripMenuItem, "ExitToolStripMenuItem");
            ExitToolStripMenuItem.Click += ExitToolStripMenuItem_Click_1;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { logfileDirectoryToolStripMenuItem, toolStripMenuItem1, downloadToolStripMenuItem, DiscordToolStripMenuItem, GithubToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            resources.ApplyResources(helpToolStripMenuItem, "helpToolStripMenuItem");
            // 
            // logfileDirectoryToolStripMenuItem
            // 
            logfileDirectoryToolStripMenuItem.Name = "logfileDirectoryToolStripMenuItem";
            resources.ApplyResources(logfileDirectoryToolStripMenuItem, "logfileDirectoryToolStripMenuItem");
            logfileDirectoryToolStripMenuItem.Click += LogfileDirectoryToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // downloadToolStripMenuItem
            // 
            downloadToolStripMenuItem.Name = "downloadToolStripMenuItem";
            resources.ApplyResources(downloadToolStripMenuItem, "downloadToolStripMenuItem");
            downloadToolStripMenuItem.Click += DownloadToolStripMenuItem_Click;
            // 
            // DiscordToolStripMenuItem
            // 
            DiscordToolStripMenuItem.Name = "DiscordToolStripMenuItem";
            resources.ApplyResources(DiscordToolStripMenuItem, "DiscordToolStripMenuItem");
            DiscordToolStripMenuItem.Click += DiscordToolStripMenuItem_Click;
            // 
            // GithubToolStripMenuItem
            // 
            GithubToolStripMenuItem.Name = "GithubToolStripMenuItem";
            resources.ApplyResources(GithubToolStripMenuItem, "GithubToolStripMenuItem");
            GithubToolStripMenuItem.Click += GithubToolStripMenuItem_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, seToolStripMenuItem, helpToolStripMenuItem });
            resources.ApplyResources(menuStrip1, "menuStrip1");
            menuStrip1.Name = "menuStrip1";
            // 
            // seToolStripMenuItem
            // 
            seToolStripMenuItem.Name = "seToolStripMenuItem";
            resources.ApplyResources(seToolStripMenuItem, "seToolStripMenuItem");
            seToolStripMenuItem.Click += SeToolStripMenuItem_Click;
            // 
            // BBB
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(BBBTabs);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            Name = "BBB";
            FormClosing += BBB_FormClosing;
            Load += BBB_Load;
            VisibleChanged += BBB_VisibleChanged;
            MainTab.ResumeLayout(false);
            MainTab.PerformLayout();
            UpdateGroupBox.ResumeLayout(false);
            UpdateGroupBox.PerformLayout();
            StreamerTTSNameGroupBox.ResumeLayout(false);
            StreamerTTSNameGroupBox.PerformLayout();
            StreamerPersonaGroupBox.ResumeLayout(false);
            StreamerSTTGroupBox.ResumeLayout(false);
            MicrophoneRecordGroupBox.ResumeLayout(false);
            BBBTabs.ResumeLayout(false);
            StreamingSettingsTab.ResumeLayout(false);
            StreamingSettingsTab.PerformLayout();
            TwitchLLMLanguageGroupBox.ResumeLayout(false);
            LLMGroupSettingsGroupBox.ResumeLayout(false);
            LLMGroupSettingsGroupBox.PerformLayout();
            TwitchSettingsGroupBox.ResumeLayout(false);
            TwitchSettingsGroupBox.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            TwitchResponseSettings.ResumeLayout(false);
            TwitchResponseSettings.PerformLayout();
            TwitchSoundsSettings.ResumeLayout(false);
            TwitchSoundsSettings.PerformLayout();
            TwitchChannelPointsSettings.ResumeLayout(false);
            TwitchChannelPointsSettings.PerformLayout();
            TwitchSubscriberSettings.ResumeLayout(false);
            TwitchSubscriberSettings.PerformLayout();
            TwitchCheerSettings.ResumeLayout(false);
            TwitchCheerSettings.PerformLayout();
            TwitchChatTriggerSettings.ResumeLayout(false);
            TwitchChatTriggerSettings.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TabPage MainTab;
        private Button MainRecordingStart;
        private TabControl BBBTabs;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem ExitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem GithubToolStripMenuItem;
        private ToolStripMenuItem DiscordToolStripMenuItem;
        private MenuStrip menuStrip1;
        private TabPage StreamingSettingsTab;
        private GroupBox TwitchSettingsGroupBox;
        private TextBox TwitchAPIStatusTextBox;
        private Label TwitchStatusLabel;
        private GroupBox TwitchCheerSettings;
        private GroupBox TwitchChatTriggerSettings;
        private CheckBox TwitchNeedsSubscriber;
        private TextBox TwitchCommandTrigger;
        private Label TwitchCommandTriggerLabel;
        private GroupBox TwitchSubscriberSettings;
        private CheckBox TwitchSubscribed;
        private Label TwitchMinimumBitsLabel;
        private TextBox TwitchMinBits;
        private Label TwitchCommandDelayLabel;
        private TextBox TwitchChatCommandDelay;
        private CheckBox TwitchGiftedSub;
        private CheckBox TwitchEnableCheckbox;
        private Label TwitchEventSubStatusLabel;
        private TextBox TwitchEventSubStatusTextBox;
        private CheckBox TwitchReadChatCheckBox;
        private GroupBox MicrophoneRecordGroupBox;
        private CheckBox TwitchCheerCheckBox;
        private GroupBox TwitchChannelPointsSettings;
        private Label TwitchCustomChannelPointNameLabel;
        private TextBox TwitchCustomRewardName;
        private CheckBox TwitchChannelPointCheckBox;
        private ToolTip BBBToolTip;
        private ToolStripMenuItem seToolStripMenuItem;
        private GroupBox StreamerSTTGroupBox;
        private ComboBox STTSelectedComboBox;
        private GroupBox StreamerPersonaGroupBox;
        private ComboBox BroadcasterSelectedPersonaComboBox;
        private Label TwitchChatPersonaLabel;
        private ComboBox TwitchChatPersonaComboBox;
        private Label TwitchChannelPointPersonaLabel;
        private ComboBox TwitchChannelPointPersonaComboBox;
        private Label TwitchSubscriptionPersonaLabel;
        private ComboBox TwitchSubscriptionPersonaComboBox;
        private Label TwitchBitsPersonaLabel;
        private ComboBox TwitchCheeringPersonaComboBox;
        private Button TwitchStartButton;
        private CheckBox TwitchAutoStart;
        private GroupBox LLMGroupSettingsGroupBox;
        private ComboBox LLMResponseSelecter;
        private Label label7;
        private GroupBox TwitchSoundsSettings;
        private CheckBox TwitchSubscriptionSoundCheckBox;
        private CheckBox TwitchCheeringSoundCheckBox;
        private CheckBox TwitchChannelSoundCheckBox;
        private CheckBox TwitchChatSoundCheckBox;
        private Button TwitchChatSoundSelectButton;
        private ComboBox TwitchChatSoundTextBox;
        private ComboBox TwitchChannelSoundTextBox;
        private ComboBox TwitchSubscriptionSoundTextBox;
        private ComboBox TwitchCheeringSoundTextBox;
        private GroupBox TwitchResponseSettings;
        private TextBox TwitchResponseToChatDelayTextBox;
        private Label TwitchPostResponseToChatDelayLabel;
        private CheckBox TwitchResponseToChatCheckBox;
        private RadioButton TwitchSubscriptionTTSResponseOnlyRadioButton;
        private RadioButton TwitchSubscriptionTTSEverythingRadioButton;
        private RadioButton TwitchChannelPointTTSResponseOnlyRadioButton;
        private RadioButton TwitchChannelPointTTSEverythingRadioButton;
        private RadioButton TwitchCheeringTTSResponseOnlyRadioButton;
        private RadioButton TwitchCheeringTTSEverythingRadioButton;
        private RadioButton TwitchChatTTSResponseOnlyRadioButton;
        private RadioButton TwitchChatTTSEverythingRadioButton;
        private ToolStripMenuItem logfileDirectoryToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private CheckBox TwitchDelayFinishToChatcCheckBox;
        private Label TwitchMessageToChatLabel;
        private TextBox TwitchDelayMessageTextBox;
        private GroupBox StreamerTTSNameGroupBox;
        private TextBox StreamerNameTextBox;
        private GroupBox UpdateGroupBox;
        private Label VersionUpdateLabel;
        private RichTextBox TextLog;
        private ToolStripMenuItem downloadToolStripMenuItem;
        private GroupBox TwitchLLMLanguageGroupBox;
        private ComboBox TwitchLLMLanguageComboBox;
        private Button CustomResponseButton;
        private GroupBox groupBox1;
        private CheckBox TwitchBadWordFilterCheckBox;
        private Button WordFilterButton;
        private Button LLMStartNewConvo;
        private GroupBox groupBox2;
        private Button NotableViewersButton;
        private CheckBox TwitchMockCheckBox;
    }
}

