using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Media.SpeechSynthesis;

/// <summary>
/// CODING RULES:
/// •	Local variables, private instance, static fields and method parameters should be camelCase.
/// •	Methods, constants, properties, events and classes should be PascalCase.
/// </summary>

namespace BanterBrain_Buddy
{
    internal class NativeSpeech
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Windows.Media.SpeechSynthesis.SpeechSynthesizer _nativeSynthesizer;
        private MemoryStream _nativeAudioStream;

        private int SelectedOutputDevice;
        private string _sTTOutputText;
        private bool _sTTDone;
        private float _voiceVolume;
        private int _voiceRate;
        private int _voicePitch;
        private string _voiceCulture;
        private SettingsManager UserSettingsManager = SettingsManager.Instance;


        private void SetSelectedOutputDevice(string OutputDevice)
        {
            if (string.IsNullOrEmpty(OutputDevice))
            {
                _bBBlog.Warn("No output device given for Native TTS, using the default device");
                return;
            }
            _bBBlog.Info($"Setting selected output device for Native TTS to: {OutputDevice}");
            for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
            {
                var tmpOut = NAudio.Wave.WaveOut.GetCapabilities(i).ProductName;
                if (OutputDevice.StartsWith(tmpOut))
                {
                    _bBBlog.Debug($"Selected outputdevice = {tmpOut}");
                    SelectedOutputDevice = i;
                }
            }
        }

        public async Task<bool> NativePlayWaveFile(string _tmpWavFile)
        {
            try
            {
                SetSelectedOutputDevice(UserSettingsManager.Settings.TTSAudioOutput);
                _bBBlog.Debug("Playing Native Wave File: " + _tmpWavFile);
                var waveOut = new WaveOut
                {
                    DeviceNumber = SelectedOutputDevice
                };
                var waveStream = new WaveFileReader(_tmpWavFile);
                waveOut.Init(waveStream);
                waveOut.Play();
                while (waveOut.PlaybackState != PlaybackState.Stopped)
                {
                    await Task.Delay(500);
                }
                //release the audio device and the file, they were leaking on every played sound
                waveOut.Dispose();
                waveStream.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                //a missing or corrupt sound file must not crash the async void callers
                _bBBlog.Error($"Error playing wave file {_tmpWavFile}: {ex.Message}");
                return false;
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public async Task<bool> NativeSpeak(string TextToSay)
        {
            try
            {
                //_voiceRate += 100;
                //_voiceRate = 0;
                int voiceVolume = 0;
                string voicePitch = $"{_voicePitch}%";
                //SSML is XML, unescaped text (like & or < from an LLM answer) would fail synthesis
                string safeText = System.Security.SecurityElement.Escape(TextToSay);
                //a broken culture (voice name without a language part) must not leave xml:lang empty
                string cultureForSsml = string.IsNullOrWhiteSpace(_voiceCulture) ? "en-US" : _voiceCulture;
                var tmpSSML = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{cultureForSsml}'>" +
                              $"<prosody rate='{_voiceRate}%' volume='{voiceVolume}%' >{safeText}</prosody></speak>";
                _bBBlog.Debug("Native SSML: " + tmpSSML);

                // Generate the audio stream from the SSML
                SpeechSynthesisStream synthesisStream = await _nativeSynthesizer.SynthesizeSsmlToStreamAsync(tmpSSML);

                // Read the stream into a memory stream
                using (var memoryStream = new MemoryStream())
                {
                    await synthesisStream.AsStreamForRead().CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Convert the memory stream to a wave stream
                    var waveStream = new RawSourceWaveStream(memoryStream, new WaveFormat(16000, 16, 1))
                    {
                        // Reset the stream to the beginning or you won't hear anything
                        Position = 0
                    };

                    // Create a SampleChannel for volume control
                    var sampleChannel = new SampleChannel(waveStream, false);

                    float volumeMultiplier = Math.Clamp(_voiceVolume, 0.0f, 2.0f);
                    _bBBlog.Debug("Native Volume: " + _voiceVolume);
                    // Use a VolumeSampleProvider to apply the volume multiplier
                    var volumeProvider = new VolumeSampleProvider(sampleChannel)
                    {
                        Volume = volumeMultiplier
                    };
                 
                    var waveOut = new WaveOut
                    {
                        DeviceNumber = SelectedOutputDevice
                    };

                    var semitone = Math.Pow(2, 1.0 / 12);
                    var upOneTone = semitone * semitone;
                    var downOneTone = 1.0 / upOneTone;
                    Double pitchTone = 1.0;
                    //alright lets set the pitch tone
                    if (_voicePitch == 0)
                    {
                        pitchTone = 1.0;
                    }
                    else if (_voicePitch > 0)
                    {
                        pitchTone = Math.Pow(upOneTone, _voicePitch / 10);
                    }
                    else if (_voicePitch < 0)
                    {
                        pitchTone = Math.Pow(downOneTone, Math.Abs(_voicePitch) / 10);
                    }
                    var pitch = new SmbPitchShiftingSampleProvider(volumeProvider);
                    pitch.PitchFactor = (float)pitchTone;


                    _bBBlog.Debug("Native Pitch: " + pitch.PitchFactor);
                    if (pitch.PitchFactor == 1.0)
                    {
                        waveOut.Init(pitch);
                    }
                    else
                    {
                        //ok we need to chop off the start, because this buggers the audio due to artifacts from pitching
                        waveOut.Init(pitch.Skip(TimeSpan.FromMilliseconds(300)));
                    }

                    waveOut.Play();
                    while (waveOut.PlaybackState != PlaybackState.Stopped)
                    {
                        await Task.Delay(500);
                    }
                    //release the audio device, it was leaking on every spoken line
                    waveOut.Dispose();
                }

                return true;
            }
            catch (Exception ex)
            {
                _bBBlog.Error($"Error during speech synthesis: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// initialize the native Windows TTS engine
        /// </summary>
        /// <param name="OutputDevice">The text of the selected output device. Limited to 32 characters (Windows limition)</param>
        /// <returns></returns>
        [SupportedOSPlatform("windows10.0.10240")]
        public Task NativeTTSInit(string VoiceUsed, string OutputDevice, int VoiceVolume, int VoiceRate, int VoicePitch)
        {
            _bBBlog.Info("Starting Native Text To Speech, Initializing");
            _bBBlog.Debug("Init Native Output Device: " + OutputDevice + " using: " + VoiceUsed);
            //we get the culture from teh VoiceUsed string
            //which is basically the last part of the string after the second last -
            //the format is Name-Gender-Culture but a hand edited/old persona can break that,
            //so only parse when the dashes are actually there
            string selectedVoiceName = VoiceUsed;
            if (!string.IsNullOrEmpty(VoiceUsed) && VoiceUsed.Contains('-'))
            {
                int lastDash = VoiceUsed.LastIndexOf('-');
                int cultureStart = Math.Max(lastDash - 2, 0);
                _voiceCulture = VoiceUsed[cultureStart..];
                selectedVoiceName = VoiceUsed[..VoiceUsed.IndexOf('-')];
            }
            else
            {
                _bBBlog.Warn($"Voice name \"{VoiceUsed}\" is not in Name-Gender-Culture format, using defaults");
                _voiceCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
            }
            _bBBlog.Debug("Init Native Culture: " + _voiceCulture);
            SetSelectedOutputDevice(OutputDevice);
            _nativeSynthesizer = new();
            var selectedVoice = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.DisplayName == selectedVoiceName);
            if (selectedVoice == null)
            {
                //a renamed/uninstalled voice from an old persona file, fall back to the first voice
                //of the current language (or just the first one) instead of assigning null which
                //the synthesizer rejects later
                _bBBlog.Warn($"Native voice \"{selectedVoiceName}\" not found, using a fallback voice");
                string currentLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
                selectedVoice = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices
                    .FirstOrDefault(v => v.Language == currentLanguage)
                    ?? Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.FirstOrDefault();
            }
            if (selectedVoice != null)
            {
                _nativeSynthesizer.Voice = selectedVoice;
            }


            // Ensure volumeControl is within the expected range
            VoiceVolume = Math.Clamp(VoiceVolume, -100, 100);

            // Convert volumeControl to a volume multiplier
            //float volumeMultiplier = (float)Math.Pow(10, volumeControl / 100.0); // Use logarithmic scale for volume control
            // Convert volumeControl to a volume multiplier
            _voiceVolume = 1.0f + (VoiceVolume / 100.0f);
            
            _voiceRate = VoiceRate;
            _voicePitch = VoicePitch;
            _nativeAudioStream = new();
            //_nativeSynthesizer.SetOutputToWaveStream(_nativeAudioStream);
            return Task.CompletedTask;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public static Task<List<NativeVoices>> TTSNativeGetVoices()
        {
            List<NativeVoices> nativeVoicesList = [];
           /* var synthesizer = new SpeechSynthesizer();
            foreach (var voice in synthesizer.GetInstalledVoices())
            {
                var info = voice.VoiceInfo;
                //_bBBlog.Info($"Native TTS Name: {info.Name}");

                var tmpVoice = new NativeVoices()
                {
                    Name = info.Name,
                    Culture = info.Culture.ToString(),
                    Gender = info.Gender.ToString()
                };
                nativeVoicesList.Add(tmpVoice);
            }*/

            foreach (var voice in Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices)
            {
                _bBBlog.Info($"Native UWP TTS Name: {voice.DisplayName}");  
                var tmpVoice = new NativeVoices()
                {
                    Name = voice.DisplayName.ToString(),
                    Culture = voice.Language.ToString(),
                    Gender = voice.Gender.ToString()
                };
                nativeVoicesList.Add(tmpVoice);
            }
            return Task.FromResult(nativeVoicesList);
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private void NativeRecognizeCompletedHandler(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _bBBlog.Error("Native STT Error encountered, " + e.Error.GetType().Name + " : " + e.Error.Message);
                _sTTOutputText = null;
            }
            if (e.Cancelled)
            {
                _bBBlog.Info("Native STT Operation cancelled");
                _sTTOutputText = null;
            }
            if (e.InputStreamEnded)
            {
                _bBBlog.Info("Mative STT recognize Stopped.");
            }

            _sTTDone = true;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        // Handle the SpeechRecognized event.  
        private void NativeSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && e.Result.Text != null)
            {
                _bBBlog.Info("Native recognized text: " + e.Result.Text);
                _sTTOutputText += e.Result.Text + "\r\n";
            }
            else
            {
                _bBBlog.Info("Native recognized text not available.");
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public async Task<string> NativeSpeechRecognizeStart(string _tmpWavFile)
        {

            string culture = UserSettingsManager.Settings.NativeSpeechRecognitionLanguageComboBox;
            if (!string.IsNullOrEmpty(culture) && culture.Length > 1)
            {
                _bBBlog.Info("Setting Native Speech Recognition Language to: " + culture);
            }
            else
            {
                _bBBlog.Info("Nothing set, so setting Native Speech Recognition Language to: en-US");
                culture = "en-US";
            }
            _bBBlog.Info("Starting Native Speech Recognition with language: " + culture);
            // Create an in-process speech recognizer for the en-US locale.  
            SpeechRecognitionEngine recognizer2 = new(new System.Globalization.CultureInfo(culture));
            // Create and load a dictation grammar.  
            recognizer2.LoadGrammar(new DictationGrammar());
            recognizer2.SetInputToWaveFile(_tmpWavFile);
            // Attach event handlers for the results of recognition.  
            recognizer2.SpeechRecognized +=
              new EventHandler<SpeechRecognizedEventArgs>(NativeSpeechRecognized);
            recognizer2.RecognizeCompleted +=
              new EventHandler<RecognizeCompletedEventArgs>(NativeRecognizeCompletedHandler);

            _bBBlog.Info("Starting asynchronous Native recognition... on " + _tmpWavFile);

            _sTTDone = false;
            recognizer2.RecognizeAsync(RecognizeMode.Multiple);
            while (!_sTTDone)
            {
                await Task.Delay(500);
            }
            _bBBlog.Info("Native STT done.");
            recognizer2.Dispose();
            return _sTTOutputText;
        }

        public NativeSpeech()
        {
            _bBBlog.Info("Native Speech Engine Initialized");
            _sTTDone = false;
        }

    }
}
