using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;

/// <summary>
/// CODING RULES:
/// •	Local variables, private instance, static fields and method parameters should be camelCase.
/// •	Methods, constants, properties, events and classes should be PascalCase.
/// </summary>

namespace BanterBrain_Buddy
{
    internal class AzureSpeechAPI
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string AzureAPIKey { get; set; }
        public string AzureRegion { get; set; }
        public string AzureLanguage { get; set; }
        private int AzureVoiceRate; 
        private int AzureVoiceVolume;
        private int AzureVoicePitch;

        private SpeechConfig _azureSpeechConfig;
        private AudioConfig _azureAudioConfig;

        private Microsoft.CognitiveServices.Speech.SpeechRecognizer _azureSpeechRecognizer;
        private string AzureVoiceName { get; set; }
        private string AzureVoiceOptions { get; set; }

        private NAudio.CoreAudioApi.MMDevice outDevice;
        private NAudio.CoreAudioApi.MMDevice inDevice;

        //global list to speed stuff up
        List<AzureVoices> _azureRegionVoicesList = [];

        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        //this is a test to see if the API is working, it tries to say something to a null device but the result is what matters
        public async Task<bool> AzureVerifyAPI()
        {
            _bBBlog.Info("Verifying Azure API");
            SpeechConfig speechConfig = SpeechConfig.FromSubscription(AzureAPIKey, AzureRegion);
            var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);

            var result = await speechSynthesizer.SpeakTextAsync("test");
            var test = result.Reason.ToString();
            _bBBlog.Debug("Azure API test result: " + test);
            if (test == "SynthesizingAudioCompleted")
            {
                _bBBlog.Info("Azure API verified by speech SUCCESS");
                return true;
            }
            else
            {
                _bBBlog.Error("Azure API verification FAILED by speech");
                return false;
            }
        }

        /// <summary>
        /// This method gets all the voices and styles available for the region and language
        /// this should load in a global list of voices and styles for the region so we dont have to reload it
        /// </summary>
        /// <returns>List<AzureVoices></returns>
        public async Task<List<AzureVoices>> TTSGetAzureVoices()
        {
            if (_azureRegionVoicesList.Count > 0)
            {
                return _azureRegionVoicesList;
            }
            //ok aparently we need to get the voices again
            List<AzureVoices> azureRegionVoicesList = [];
            _bBBlog.Info("Finding TTS Azure voices available");
            //sensitive information
            //_bBBlog.Debug("Get voices Azure API Key: " + AzureAPIKey);
            _bBBlog.Debug("Get voices Azure Region: " + AzureRegion);

            SpeechConfig speechConfig = SpeechConfig.FromSubscription(AzureAPIKey, AzureRegion);
            var speechSynthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(speechConfig, null as AudioConfig);
            SynthesisVoicesResult result = await speechSynthesizer.GetVoicesAsync();

            //if we got any voices back we add them to the list
            if (result.Reason == ResultReason.VoicesListRetrieved)
            {
                //remove the old listed items
                azureRegionVoicesList.Clear();
                _bBBlog.Info($"Found {result.Voices.Count} voices");
                foreach (var voice in result.Voices)
                {
                    //RegionInfo throws on locales with non-standard region parts (e.g. zh-Hans-CN),
                    //fall back to the raw locale instead of killing the whole voice listing
                    string localeDisplayName;
                    try
                    {
                        localeDisplayName = new RegionInfo(voice.Locale).ThreeLetterISORegionName;
                    }
                    catch (ArgumentException)
                    {
                        localeDisplayName = voice.Locale;
                    }
                    var tmpVoice = new AzureVoices()
                    {
                        Locale = voice.Locale,
                        LocalName = voice.LocalName,
                        Name = voice.Name,
                        Gender = voice.Gender.ToString(),
                        StyleList = new List<string>(voice.StyleList),
                        LocaleDisplayname = localeDisplayName
                    };
                    azureRegionVoicesList.Add(tmpVoice);
                }
                _azureRegionVoicesList = azureRegionVoicesList;
                return azureRegionVoicesList;
            }
            else //no voices back means something is definately bad
            {
                _bBBlog.Error("Problem retreiving Azure API voicelist. Is your API key or subscription information still valid?");
            }
            return null;
        }

        //this sets "SelectedInputDevice" to the correct input/microphone or capture device
        private void SetSelectedInputDevice(string InputDevice)
        {
            if (string.IsNullOrEmpty(InputDevice))
            {
                _bBBlog.Warn("No input device given for Azure STT, the default microphone will be used");
                return;
            }
            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            foreach (var device in
                     enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active))
            {
                if (device.FriendlyName.StartsWith(InputDevice))
                {
                    inDevice = device;
                }
            }
        }



        private void SetSelectedOutputDevice(string OutputDevice)
        {
            _bBBlog.Info($"Setting selected output device for Azure TTS to: {OutputDevice}");
            if (string.IsNullOrEmpty(OutputDevice))
            {
                _bBBlog.Warn("No output device given for Azure TTS, no device will be selected");
                return;
            }


            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            foreach (var device in
                     enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.DeviceState.Active))
            {
                if (device.FriendlyName.StartsWith(OutputDevice))
                {
                    outDevice = device;
                }
            }
        }

        /// <summary>
        /// Initializes the Azure Text to Speech API with the selected voice, style and output device
        /// </summary>
        /// <param name="AzureVoiceName">This holds the azure voice. This need to be parsed to be usable for Azure</param>
        /// <param name="TTSVoiceOptions">This holds the style of the voice (if available)</param>
        /// <param name="OutputDevice">This is the output device selected in the GUI</param>
        public async Task AzureTTSInit(string AzureVoiceParseName, string TTSVoiceOptions, string OutputDevice, int VolumeLevel, int RateLevel, int PitchLevel)
        {
            _bBBlog.Info("Starting Azure Text To Speech, Initializing");
            AzureVoiceRate = RateLevel;
            AzureVoiceVolume = VolumeLevel;
            AzureVoicePitch = PitchLevel;
            _bBBlog.Debug("Init Azure Region: " + AzureRegion);
            _bBBlog.Debug("Init Azure Output Device: " + OutputDevice);
            SetSelectedOutputDevice(OutputDevice);

            _azureSpeechConfig = SpeechConfig.FromSubscription(AzureAPIKey, AzureRegion);
            
            //set the options that we can just pass along, this holds the style of the voice
            AzureVoiceOptions = TTSVoiceOptions;

            //now find the correct name associated with the selected voice
            var azureRegionVoicesList = await TTSGetAzureVoices();
            //the voice fetch fails when the key/region is wrong, iterating null would crash
            if (azureRegionVoicesList == null)
            {
                _bBBlog.Error("No Azure voices available, cannot initialize Azure TTS. Check your API key and region.");
                return;
            }

            //we need to do some parsing. Azure voices are in the format of "en-US-Guy-Azure" and the text we get is not.
            //what is returned is the usable voicename for Azure.
            foreach (var azureRegionVoice in azureRegionVoicesList)
            {
                if (AzureVoiceParseName == (azureRegionVoice.LocaleDisplayname + "-" + azureRegionVoice.Gender + "-" + azureRegionVoice.LocalName))
                {

                    AzureVoiceName = azureRegionVoice.Name;
                    _bBBlog.Debug($"Azure Voice found. Assigning {AzureVoiceName}");
                    return;
                }
            }

        }

        /// <summary>
        /// This method speaks the text passed to it using the Azure TTS API using SSML so it support styles
        /// </summary>
        /// <param name="TextToSay">The text to be spoken by the Azure TTS</param>
        /// <returns>True if no error, False if error</returns>
        
        /// speed and volume can be set in the SSML
        public async Task<bool> AzureSpeak(string TextToSay)
        {
            _bBBlog.Info($"Starting Azure Text To Speech, Speaking with: {AzureVoiceName}");
            if (!string.IsNullOrEmpty(AzureVoiceName))
            {
                //SSML is XML: LLM answers and cheered messages can contain & < > etc, unescaped
                //they make Azure reject the whole synthesis
                string safeText = System.Security.SecurityElement.Escape(TextToSay);
                //an empty style breaks the SSML (style="" is rejected), only add express-as when the
                //persona actually has a style selected
                string innerSpeech = safeText;
                if (!string.IsNullOrWhiteSpace(AzureVoiceOptions))
                {
                    innerSpeech = $" <mstts:express-as style=\"{System.Security.SecurityElement.Escape(AzureVoiceOptions)}\" styledegree=\"1\"> \r\n{safeText}\r\n        </mstts:express-as>\r\n    ";
                }
                string SSMLText =
               
                $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xmlns:emo=\"http://www.w3.org/2009/10/emotionml\" xml:lang=\"{UserSettingsManager.Settings.AzureLanguageComboBox}\">\r\n   " +
                $" <voice name=\"{AzureVoiceName}\">\r\n" +
                $"<prosody rate=\"{AzureVoiceRate}%\" pitch= \"{AzureVoicePitch}%\" volume=\"{AzureVoiceVolume}%\"> \r\n" +
                $"{innerSpeech}" +
                "</prosody>" +
                "</voice>\r\n" +
                "</speak>";

                _bBBlog.Debug("SSML: " + SSMLText);
                //now lets speak the SSML and handle the result 
                _azureSpeechConfig.SpeechSynthesisVoiceName = AzureVoiceName;
                _azureSpeechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
                if (outDevice == null)
                {
                    _bBBlog.Error("No output device selected for Azure TTS. It should be set to: " + UserSettingsManager.Settings.TTSAudioOutput);
                    SetSelectedOutputDevice(UserSettingsManager.Settings.TTSAudioOutput);
                }
                if (outDevice == null)
                {
                    //the configured device is gone (unplugged/renamed), speaking into null would NRE
                    _bBBlog.Error("Still no matching output device found, cannot speak via Azure TTS");
                    return false;
                }
                _bBBlog.Debug($"SelectedOutputdevice: {outDevice.ID}");
                var tmpAudioConfig = AudioConfig.FromSpeakerOutput(outDevice.ID);

                var speechSynthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(_azureSpeechConfig, tmpAudioConfig);
                
                var speechSynthesisResult = await speechSynthesizer.SpeakSsmlAsync(SSMLText);
                var result = TTSAzureOutputSpeechSynthesisResult(speechSynthesisResult, TextToSay);
                if (result)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                _bBBlog.Error("Cannot find selected voice in the list. Is there a problem with your API key or subscription?");
                return false;
            }
        }


        /// <summary>
        /// This parses the output of the Azure TTS and returns 
        /// </summary>
        /// <param name="speechSynthesisResult">Holds the result of the action of SSML speaking</param>
        /// <param name="text">The text that has been spoken</param>
        /// <returns>True if no error, false if not </returns>
        private static bool TTSAzureOutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
        {
            switch (speechSynthesisResult.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    _bBBlog.Info($"Speech synthesized for text: [{text}]");
                    return true;
                case ResultReason.Canceled:
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                    _bBBlog.Info($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        _bBBlog.Error($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        _bBBlog.Error($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        _bBBlog.Error($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    return false;
                default:
                    break;
            }
            return false;
        }


        /// <summary>
        /// Initializes the Azure Speech to Text API with the selected input device
        /// </summary>
        /// <param name="InputDevice">This is the name of the selected sound capture device</param>
        public void AzureSTTInit(string InputDevice)
        {
            _bBBlog.Info("Starting Azure Speech to Text, Initializing");
            //sensitive information
            //_bBBlog.Debug("Init Azure API Key: " + AzureAPIKey);
            _bBBlog.Debug("Init Azure Region: " + AzureRegion);
            _azureSpeechConfig = SpeechConfig.FromSubscription(AzureAPIKey, AzureRegion);
            _azureSpeechConfig.SpeechRecognitionLanguage = AzureLanguage; //default language

            SetSelectedInputDevice(InputDevice);
            if (inDevice != null)
            {
                _bBBlog.Info("selected audio input device for Azure: " + inDevice.FriendlyName);
                _azureAudioConfig = AudioConfig.FromMicrophoneInput(inDevice.ID);
            }
            else
            {
                //the configured mic was unplugged/renamed, fall back to the system default instead of NRE
                _bBBlog.Warn("No matching Azure input device found, using the system default microphone");
                _azureAudioConfig = AudioConfig.FromDefaultMicrophoneInput();
            }
            _azureSpeechRecognizer = new SpeechRecognizer(_azureSpeechConfig, _azureAudioConfig);
        }

        /// <summary>
        /// this is the recognizer loop for Azure STT, it returns the speech recognized as text
        /// or null when an error occurs
        /// </summary>
        /// <returns>Recognized text, NOMATCH or null</returns>
        public async Task<string> RecognizeSpeechAsync()
        {
            _bBBlog.Info("Starting Azure Speech to Text, Recognizing");
            var tmpResult = await _azureSpeechRecognizer.RecognizeOnceAsync();
            var returnResult = AzureOutputSpeechRecognitionResult(tmpResult);
            return returnResult;
        }

        /// <summary>
        /// This method stops the Azure STT recognizer and parses the result for errors or recognized text
        /// </summary>
        /// <param name="speechRecognitionResult"></param>
        /// <returns>Recognized text, NOMATCH or null</returns>
        private static string AzureOutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
        {
            switch (speechRecognitionResult.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    _bBBlog.Info($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                    return speechRecognitionResult.Text;
                case ResultReason.NoMatch:
                    _bBBlog.Info("NOMATCH: Speech could not be recognized.");
                    return "NOMATCH";
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(speechRecognitionResult);

                    _bBBlog.Info($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        _bBBlog.Error($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        _bBBlog.Error($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        _bBBlog.Error($"CANCELED: Did you set the correct API resource key and region values?");
                    }
                    //whatever the cancel reason, its an error so return null. 
                    //user should checklogfile for more info
                    return null;
            }
            return null;
        }

        /// <summary>
        /// We need to at least know the API key, the  region and the language to use this class.
        /// </summary>
        /// <param name="AzAPIKey">The API key from Azure needed for Speech services</param>
        /// <param name="AzRegion">The region the key is valid for</param>
        /// <param name="AzLanguage">The language requested for the Speech service</param>
        public AzureSpeechAPI(string AzAPIKey, string AzRegion, string AzLanguage)
        {
            //first we convert the passed local strings to global
            AzureAPIKey = AzAPIKey;
            //Sensitive information
            //_bBBlog.Debug("Azure API Key: " + AzureAPIKey);
            AzureRegion = AzRegion;
            _bBBlog.Debug("Azure Region: " + AzureRegion);
            AzureLanguage = AzLanguage;
            _bBBlog.Debug("Azure Language: " + AzureLanguage);
        }
    }
}
