using ElevenLabs;
using ElevenLabs.User;
using ElevenLabs.Voices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace BanterBrain_Buddy
{
    internal class ElLabs
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public string ElevelLabsAPIKey { get; set; }
        //private List<string> _elevenLabVoiceList;
        private readonly Dictionary<string, string> _elevenLabVoiceList;

        private int SelectedOutputDevice;

        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        private void SetSelectedOutputDevice(string OutputDevice)
        {
            if (string.IsNullOrEmpty(OutputDevice))
            {
                _bBBlog.Warn("No output device given for ElevenLabs TTS, using the default device");
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

        [SupportedOSPlatform("windows10.0.10240")]
        public async Task<bool> ElevenLabsTTS(string text, string outputDevice, string tmpVoice, int similarity, int stability, int style, int volumeBoost)
        {
            var api = new ElevenLabsClient(ElevelLabsAPIKey);
            _bBBlog.Info($"ElevenLabsTTS called with voice: {tmpVoice}");
            SetSelectedOutputDevice(outputDevice);
            var voiceId = tmpVoice.Split(';')[0];

            //get the available models
            //eleven_monolingual_v1 is the default model
            var elevenLabModels = await api.ModelsEndpoint.GetModelsAsync();
            _bBBlog.Info($"Models found: {elevenLabModels.Count}");
            for (int i = 0; i < elevenLabModels.Count; i++)
            {
                _bBBlog.Info($"Model: {elevenLabModels[i].Id} - {elevenLabModels[i].Name} - {elevenLabModels[i].CanDoTextToSpeech}");
            }

            var voice = await api.VoicesEndpoint.GetVoiceAsync(voiceId);
            VoiceSettings voiceSettings = new()
            {
                SimilarityBoost = similarity / 100f,
                Stability = stability / 100f,
                SpeakerBoost = false,
                Style = style / 100f
            };
            // var voiceSettings = await api.VoicesEndpoint.GetDefaultVoiceSettingsAsync();
            //await instead of .Result, blocking a UI thread context on an async call can deadlock
            if (await api.VoicesEndpoint.EditVoiceSettingsAsync(voiceId, voiceSettings))
            {
                _bBBlog.Info($"Voice settings updated for {voice.Name}");
            }
            else
            {
                _bBBlog.Error($"Voice settings update failed for {voice.Name}");
                return false;
            }
            //this returns in mp3 format...pfft
            //here we should set the model to the one we want to use
            ElevenLabs.Models.Model modelToUse = null;
            if (UserSettingsManager.Settings.ElevenLabsModel.StartsWith("Multilingual V2"))
            {
                _bBBlog.Info("Using Multilingual V2 model");
                modelToUse = ElevenLabs.Models.Model.MultiLingualV2;
            } else if (UserSettingsManager.Settings.ElevenLabsModel.StartsWith("Turbo V2.5"))
            {
                _bBBlog.Info("Using Turbo V2.5 model");
                modelToUse = ElevenLabs.Models.Model.TurboV2_5;
            }
            else if (UserSettingsManager.Settings.ElevenLabsModel.StartsWith("Turbo V2"))
            {
                _bBBlog.Info("Using Turbo V2 model");
                modelToUse = ElevenLabs.Models.Model.EnglishTurboV2;
            }
            else
            {
                _bBBlog.Info("Defaulting to Multilingual V2 model");
                modelToUse = ElevenLabs.Models.Model.MultiLingualV2;
            }

            var convertedText = await api.TextToSpeechEndpoint.TextToSpeechAsync(text, voice, voiceSettings, modelToUse);
            var mp3Data = convertedText.ClipData.ToArray();
            MemoryStream ms = new(mp3Data)
            {
                Position = 0
            };
            var WavStream = WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(ms));
            var waveOut = new WaveOut
            {
                DeviceNumber = SelectedOutputDevice
            };
            //it has to be 24000, 16, 1 for some reason?
            var waveStream = new RawSourceWaveStream(WavStream, new WaveFormat(44100, 16, 1))
            {
                //reset the stream to the beginning or you wont hear anything
                Position = 0
            };

            float volumeMultiplier = 1.0f + (volumeBoost / 100.0f);
            // Create a SampleChannel for volume control
            var sampleChannel = new SampleChannel(waveStream, false);

            _bBBlog.Info($"Volume boost: {volumeMultiplier}");
            // Use a VolumeSampleProvider to apply the volume multiplier
            var volumeProvider = new VolumeSampleProvider(sampleChannel)
            {
                Volume = volumeMultiplier
            };

            waveOut.Init(volumeProvider);

            waveOut.Play();
            while (waveOut.PlaybackState != PlaybackState.Stopped)
            {
                await Task.Delay(500);
            }
            //release the audio device and streams, they were leaking on every spoken line
            waveOut.Dispose();
            waveStream.Dispose();
            WavStream.Dispose();
            ms.Dispose();
            return true;
        }

        public async Task<List<string>> TTSGetElevenLabsVoices()
        {
            List<string> ElevenLabVoices = [];

            //we dont need to re-do this if its alrready filled
            if (_elevenLabVoiceList.Count > 0)
            {
                foreach (var voice in _elevenLabVoiceList)
                {
                    ElevenLabVoices.Add(voice.Value +";" + voice.Key);
                }
                return ElevenLabVoices;
            }

            var api = new ElevenLabsClient(ElevelLabsAPIKey);
            var tier = await api.UserEndpoint.GetSubscriptionInfoAsync();
            _bBBlog.Info($"Tier: {tier.Tier} for {tier.VoiceLimit}");
            //get all the voices but, lets also accept a timeout incase theres issues
            var getAllVoicesTask = api.VoicesEndpoint.GetAllVoicesAsync();

            //lets wait 10 seconds for the voices to come back
            var timeoutTask = Task.Delay(15000);

            var completedTask = await Task.WhenAny(getAllVoicesTask, timeoutTask);

            if (completedTask == getAllVoicesTask)
            {
                var voices = await getAllVoicesTask;
                _bBBlog.Info($"Voices found: {voices.Count}");
                // Process the voices...
                foreach (var voice in voices)
                {
                    //indexer not Add, ElevenLabs allows duplicate voice names and Add would throw
                    _elevenLabVoiceList[voice.Name] = voice.Id;
                    ElevenLabVoices.Add(voice.Id + ";" + voice.Name);
                }
                return ElevenLabVoices;
            }
            else
            {
                // Timeout occurred
                // Handle the timeout scenario...
                _bBBlog.Error("Timeout after 15 seconds occurred while fetching ElevenLab voices");
                return null; // or return null, throw an exception, etc.
            }
        }

  
        public async Task<bool> ElevenLabsAPIKeyTest()
        {
            _bBBlog.Info("ElevenLabsAPIKeyTest called");
            if (ElevelLabsAPIKey.Length > 1)
            {
                var api = new ElevenLabsClient(ElevelLabsAPIKey);
                try
                {
                    SubscriptionInfo result = await api.UserEndpoint.GetSubscriptionInfoAsync();
                    _bBBlog.Debug("ElevenLab API subscription test: " +result.Status);
                    if (result.Status == "invalid_api_key")
                    {
                        _bBBlog.Error("ElevenLabsAPIKeyTest failed, no key added");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _bBBlog.Error($"ElevenLabsAPIKeyTest failed, no key added. {ex.Message}");
                    return false;
                }
                return true;
            }
            else
            {
                _bBBlog.Error("ElevenLabsAPIKeyTest failed, no key added");
                return false;
            }
        }

        public ElLabs(string elevelLabsAPIKey)
        {
            _bBBlog.Info("ElevenLabs called");
            this.ElevelLabsAPIKey = elevelLabsAPIKey;
            _elevenLabVoiceList = [];
        }
    }
}
