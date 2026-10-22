using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BanterBrain_Buddy
{
    /// <summary>
    /// CODING RULES:
    /// •	Local variables, private instance, static fields and method parameters should be camelCase.
    /// •	Methods, constants, properties, events and classes should be PascalCase.
    /// </summary>
    /// 

    internal class OpenAI
    {

        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // how many chat history messages we keep and send along for context
        private const int maxChatHistoryMessages = 40;
        // model that is used when the user setting is empty or a legacy/retired value
        private const string defaultChatModel = "gpt-5-mini";

        private int SelectedOutputDevice;
        private OpenAIClient _OpenAPI;
        private readonly List<ChatMessage> _chatHistory = new();
        public string OpenAIAPIKey { get; set; }
        // why the last GetOpenAIIGPTResponse returned null, the caller shows this instead of
        // guessing at a key or subscription problem, the wrapper knows whether it was the token
        // budget, the reasoning effort or the server refusing the request
        public string LastResponseError { get; private set; } = "";

        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        private void SetSelectedOutputDevice(string OutputDevice)
        {
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

        // Validates an API key by listing the models it gives access to.
        // This is a free (non token consuming) authenticated call, used by the UI to test keys.
        public static async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return false;
            }
            try
            {
                OpenAIModelClient modelClient = new(apiKey);
                await modelClient.GetModelsAsync();
                return true;
            }
            catch (ClientResultException)
            {
                return false;
            }
        }

        // the model listing also contains models that can never be used for chat completions
        // (transcription, speech, embeddings, moderation, images and the legacy text engines),
        // these name fragments are filtered out of the settings model selector
        private static readonly string[] nonChatModelTokens =
        {
            "whisper", "tts", "transcribe", "translate", "embed", "moderation",
            "dall", "audio", "realtime", "computer-use", "text-", "davinci", "babbage", "search"
        };

        // Pulls the chat-capable model names the key has access to from the library's model
        // listing call, used by the settings UI to fill the model selector dynamically.
        // Returns an empty list when no key is set or the call fails, the UI then keeps its built-in list.
        public static async Task<List<string>> FetchChatModelNamesAsync(string apiKey)
        {
            List<string> chatModels = new();
            if (string.IsNullOrEmpty(apiKey))
            {
                return chatModels;
            }
            try
            {
                OpenAIModelClient modelClient = new(apiKey);
                ClientResult<OpenAIModelCollection> modelsResult = await modelClient.GetModelsAsync();
                foreach (OpenAIModel model in modelsResult.Value)
                {
                    string id = model.Id;
                    //custom deployment names like "base-model:owner:suffix" duplicate the base model
                    if (string.IsNullOrEmpty(id) || id.Contains(':'))
                    {
                        continue;
                    }
                    if (!nonChatModelTokens.Any(token => id.Contains(token, StringComparison.OrdinalIgnoreCase)))
                    {
                        chatModels.Add(id);
                    }
                }
                chatModels.Sort();
                _bBBlog.Info($"Fetched {chatModels.Count} chat models from the OpenAI model listing");
                return chatModels;
            }
            catch (ClientResultException ex)
            {
                _bBBlog.Error("OpenAI model list fetch failed: " + ex.Status + " " + ex.Message);
                return new List<string>();
            }
        }

        // Function to validate the API key with a timeout
        // if it times out, we will retry once
        public async Task<bool> OpenAICheckAPIKey()
        {
            _bBBlog.Debug("Verifying OpenAI API key");
            if (string.IsNullOrEmpty(UserSettingsManager.Settings.GPTAPIKey))
            {
                _bBBlog.Error("OpenAI API Key is missing or bad");
                return false;
            }
            else
            {
                OpenAIAPIKey = UserSettingsManager.Settings.GPTAPIKey;
                _OpenAPI ??= new(OpenAIAPIKey);

                // Function to validate the API key with a timeout
                async Task<bool> ValidateAPIKeyWithTimeout()
                {
                    var validateTask = _OpenAPI.GetOpenAIModelClient().GetModelsAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15)); // 15 seconds timeout

                    var completedTask = await Task.WhenAny(validateTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        _bBBlog.Error("OpenAI API Key validation timed out");
                        return false;
                    }

                    try
                    {
                        await validateTask;
                        return true;
                    }
                    catch (ClientResultException ex)
                    {
                        _bBBlog.Error("OpenAI API Key rejected by server: " + ex.Status + " " + ex.Message);
                        return false;
                    }
                }

                // First attempt
                if (await ValidateAPIKeyWithTimeout())
                {
                    _bBBlog.Info("OpenAI API Key is valid");
                    return true;
                }

                // Retry once if the first attempt times out
                _bBBlog.Info("Retrying OpenAI API Key validation");
                if (await ValidateAPIKeyWithTimeout())
                {
                    _bBBlog.Info("OpenAI API Key is valid");
                    return true;
                }
                else
                {
                    _bBBlog.Error("OpenAI API Key is invalid");
                    OpenAIAPIKey = null;
                    return false;
                }
            }
        }

        public async Task<string> OpenAISTT(string audioFile)
        {
            _bBBlog.Info("Sending to OpenAI STT with language: " + UserSettingsManager.Settings.WhisperSpeechRecognitionComboBox);
            string ISOLanguage;
            switch (UserSettingsManager.Settings.WhisperSpeechRecognitionComboBox)
            {
                case "Dutch":
                    ISOLanguage = "nl";
                    break;
                case "Danish":
                    ISOLanguage = "da";
                    break;
                case "English":
                    ISOLanguage = "en";
                    break;
                case "French":
                    ISOLanguage = "fr";
                    break;
                case "German":
                    ISOLanguage = "de";
                    break;
                case "Italian":
                    ISOLanguage = "it";
                    break;
                case "Japanese":
                    ISOLanguage = "ja";
                    break;
                case "Norwegian":
                    ISOLanguage = "no";
                    break;
                case "Polish":
                    ISOLanguage = "pl";
                    break;
                case "Spanish":
                    ISOLanguage = "es";
                    break;
                case "Swedish":
                    ISOLanguage = "sv";
                    break;
                default:
                    _bBBlog.Error("OpenAI STT language not supported, using english");
                    ISOLanguage = "en";
                    break;
            }

            if (_OpenAPI == null)
            { 
            _OpenAPI = new(UserSettingsManager.Settings.GPTAPIKey);
            OpenAIAPIKey = UserSettingsManager.Settings.GPTAPIKey;
            }
            AudioClient whisperClient = _OpenAPI.GetAudioClient("whisper-1");
            AudioTranscriptionOptions transcriptionOptions = new()
            {
                Language = ISOLanguage
            };
            try
            {
                ClientResult<AudioTranscription> STTResult = await whisperClient.TranscribeAudioAsync(audioFile, transcriptionOptions);

                if (STTResult?.Value?.Text == null)
                {
                    _bBBlog.Error("OpenAI STT failed");
                    return null;
                }
                return STTResult.Value.Text;
            }
            catch (ClientResultException ex)
            {
                _bBBlog.Error("OpenAI STT failed: " + ex.Status + " " + ex.Message);
                return null;
            }
        }

        //convert the speed range from 0.25 to 4.0 using the -100 to 100 slider
        double ConvertRange(int inputValue)
        {
            // Define the input range and corresponding speed range
            int inputMin = -100, inputMax = 100;
            float speedAtZero = 1.0f, speedMin = 0.25f, speedMax = 4.0f;

            // Calculate the slope for the negative and positive ranges
            float slopeForNegativeRange = (speedAtZero - speedMin) / -inputMin; // From -100 to 0
            float slopeForPositiveRange = (speedMax - speedAtZero) / inputMax;  // From 0 to +100

            // Apply the piecewise linear mapping
            if (inputValue <= 0)
            {
                return speedAtZero + (inputValue * slopeForNegativeRange);
            }
            else // inputValue > 0
            {
                return speedAtZero + (inputValue * slopeForPositiveRange);
            }
        }

        public async Task<bool> OpenAITTS(string text, string outputDevice, string voice, int speed, int volumeControl)
        {
            SetSelectedOutputDevice(outputDevice);
            if (_OpenAPI == null)
            {
                _OpenAPI = new(UserSettingsManager.Settings.GPTAPIKey);
                OpenAIAPIKey = UserSettingsManager.Settings.GPTAPIKey;
            }
                //alloy, echo, fable, onyx, nova, and shimmer
            AudioClient ttsClient = _OpenAPI.GetAudioClient("tts-1");
            SpeechGenerationOptions ttsRequestArgs = new()
            {
                //speed 0.25 to 4.0
                SpeedRatio = (float)ConvertRange(speed),
                //we use WAV streams format
                ResponseFormat = GeneratedSpeechFormat.Wav,
            };
            byte[] wavBytes;
            try
            {
                ClientResult<BinaryData> TTSResult = await ttsClient.GenerateSpeechAsync(text, new GeneratedSpeechVoice(voice), ttsRequestArgs);
                if (TTSResult?.Value == null)
                {
                    _bBBlog.Error("OpenAI TTS failed");
                    return false;
                }
                wavBytes = TTSResult.Value.ToArray();
            }
            catch (ClientResultException ex)
            {
                _bBBlog.Error("OpenAI TTS failed: " + ex.Status + " " + ex.Message);
                return false;
            }
            if (wavBytes.Length == 0)
            {
                _bBBlog.Error("OpenAI TTS returned empty audio");
                return false;
            }
            // Ensure volumeControl is within the expected range
            volumeControl = Math.Clamp(volumeControl, -100, 100);

            // Convert volumeControl to a volume multiplier
            //float volumeMultiplier = (float)Math.Pow(10, volumeControl / 100.0); // Use logarithmic scale for volume control
            // Convert volumeControl to a volume multiplier
            float volumeMultiplier = 1.0f + (volumeControl / 100.0f);


            var waveOut = new WaveOut
            {
                DeviceNumber = SelectedOutputDevice
            };
            //the response is a full WAV file, so we can read it with a WaveFileReader
            var waveStream = new WaveFileReader(new MemoryStream(wavBytes));
            // Create a SampleChannel for volume control
            var sampleChannel = new SampleChannel(waveStream, false);

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
            waveOut.Dispose();
            return true;
        }

        // maps legacy/retired model names from older settings.json files to current models
        private string NormalizeGPTModel(string model)
        {
            switch (model)
            {
                case "gpt-3.5-turbo": // retired, use the current low cost model instead
                    _bBBlog.Info("Legacy GPT-3.5 Turbo setting detected, using " + defaultChatModel);
                    return defaultChatModel;
                case "gpt-4-omni":
                    _bBBlog.Info("Legacy gpt-4-omni setting detected, using gpt-4o");
                    return "gpt-4o";
                case "gpt-4-omni-mini":
                    _bBBlog.Info("Legacy gpt-4-omni-mini setting detected, using gpt-4o-mini");
                    return "gpt-4o-mini";
                case null or "":
                    _bBBlog.Info("No GPT Model set, using " + defaultChatModel);
                    return defaultChatModel;
                default:
                    return model;
            }
        }

        // reasoning models manage a hidden thinking budget, for these we must not send a
        // temperature or an output cap the way we do for the classic models. The model listing
        // can return names newer than this code, so every gpt release from version 5 up and
        // the whole o-series are treated as reasoning models instead of a fixed prefix list
        private static bool IsReasoningModel(string model)
        {
            //o1, o2, o3, ... any single letter o followed by a digit
            if (model.StartsWith("o") && model.Length > 1 && char.IsDigit(model[1]))
            {
                return true;
            }
            Match match = Regex.Match(model, @"^gpt-(\d+)");
            return match.Success && int.Parse(match.Groups[1].Value) >= 5;
        }

        // the models endpoints do not report reasoning capabilities, so we ask the API itself:
        // a chat completion with an intentionally invalid reasoning effort is rejected with a
        // validation error before any token is generated, and that error lists the supported
        // levels. Returns the levels for the model, an empty list when the model has no
        // reasoning settings at all, or Levels=null with a reason in Error when the API gave
        // no answer (bad key, no credits, network, ...). Note that OpenAI checks the billing
        // quota before parameter validation, so a 429 here means the levels cannot be probed
        public static async Task<(List<string> Levels, string Error)> FetchReasoningEffortLevelsAsync(string apiKey, string model)
        {
            List<string> levels = new();
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
            {
                return (null, "No API key or model selected");
            }
            const string probeEffort = "zzz-effort-probe";
            try
            {
                ChatClient chatClient = new(model, apiKey);
                ChatCompletionOptions probeOptions = new()
                {
                    //cap the output in case a model ever accepts the bogus value
                    MaxOutputTokenCount = 1,
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                    ReasoningEffortLevel = probeEffort,
#pragma warning restore OPENAI001
                };
                await chatClient.CompleteChatAsync(new[] { new UserChatMessage("x") }, probeOptions);
                //the API accepted the bogus effort, the supported levels cannot be read from it
                _bBBlog.Warn("OpenAI accepted the reasoning effort probe, cannot derive supported levels");
                return (null, "OpenAI accepted the probe request, it did not report any validation error");
            }
            catch (ClientResultException ex)
            {
                string message = ex.Message ?? "";
                if (ex.Status == 400)
                {
                    //"Invalid value: 'zzz-effort-probe'. Supported values are: 'minimal', 'low', 'medium', and 'high'."
                    int supportedIndex = message.IndexOf("Supported values", StringComparison.Ordinal);
                    if (supportedIndex >= 0)
                    {
                        foreach (Match value in Regex.Matches(message.Substring(supportedIndex), @"'([A-Za-z-]+)'"))
                        {
                            string level = value.Groups[1].Value;
                            if (!level.Equals(probeEffort, StringComparison.OrdinalIgnoreCase))
                            {
                                //show the levels title cased like the built-in selector items
                                levels.Add(char.ToUpperInvariant(level[0]) + level.Substring(1).ToLowerInvariant());
                            }
                        }
                        _bBBlog.Info($"OpenAI reported {levels.Count} reasoning levels for {model}");
                        return (levels, null);
                    }
                    //"Unsupported parameter: 'reasoning_effort' is not supported with this model."
                    if (message.Contains("reasoning", StringComparison.OrdinalIgnoreCase))
                    {
                        _bBBlog.Info($"OpenAI reports {model} has no reasoning settings");
                        return (levels, null);
                    }
                }
                //any other status (401, 404, 429, ...) means we got no answer for the model,
                //hand the API's own reason to the UI, a 429 for example means the account
                //has no credits and the probe never reached parameter validation
                _bBBlog.Error("Reasoning effort probe failed: " + ex.Status + " " + message);
                string reason = $"HTTP {ex.Status}: {message}";
                if (reason.Length > 300)
                {
                    reason = reason.Substring(0, 300) + "...";
                }
                return (null, reason);
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Reasoning effort probe failed: " + ex.Message);
                return (null, ex.Message);
            }
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public async Task<string> GetOpenAIIGPTResponse(String UserInput, string tmpPersonaRoletext)
        {
            string gPTOutputText = "";
            LastResponseError = "";
            _bBBlog.Info("Sending to OpenAI GPT LLM: " + UserInput);

            //the SDK credential throws on an empty key, report it the same way as an API error
            if (string.IsNullOrWhiteSpace(UserSettingsManager.Settings.GPTAPIKey))
            {
                LastResponseError = "No OpenAI API key is configured, set it in the settings form";
                _bBBlog.Error(LastResponseError);
                return gPTOutputText;
            }

            _OpenAPI ??= new(UserSettingsManager.Settings.GPTAPIKey);

            string model = NormalizeGPTModel(UserSettingsManager.Settings.GPTModel);
            _bBBlog.Info("GPT Model: " + model);
            ChatClient chatClient = _OpenAPI.GetChatClient(model);

            bool reasoningModel = IsReasoningModel(model);
            ChatCompletionOptions requestOptions = new();
            //tells the retry below whether an effort went out with the request, the SDK property is
            //an enum and cannot be read back as text
            bool effortRequested = false;
            // reasoning models (gpt-5 and newer) reject a non default temperature and
            // manage their own output budget, so only apply these to the classic models
            if (!reasoningModel)
            {
                requestOptions.Temperature = UserSettingsManager.Settings.GPTTemperature;
                requestOptions.MaxOutputTokenCount = UserSettingsManager.Settings.GPTMaxTokens;
            }
            else
            {
                //the dropdown lets the user pick the thinking budget, an empty setting or "Auto"
                //keeps the library default. The levels come from the model itself (see
                //FetchReasoningEffortLevelsAsync), so send the chosen one as-is and let the API validate it
                string reasoningEffort = UserSettingsManager.Settings.GPTReasoningEffortComboBox;
                effortRequested = !string.IsNullOrEmpty(reasoningEffort) && !reasoningEffort.StartsWith("Auto", StringComparison.Ordinal);
                if (effortRequested)
                {
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                    requestOptions.ReasoningEffortLevel = reasoningEffort.ToLowerInvariant();
#pragma warning restore OPENAI001
                    _bBBlog.Info("GPT reasoning effort set to: " + reasoningEffort);
                }
            }
            //mood is setting the system text description
            //this is the persona role
            _bBBlog.Info("SystemRole: " + tmpPersonaRoletext);
            List<ChatMessage> messages = new()
            {
                new SystemChatMessage(tmpPersonaRoletext)
            };
            messages.AddRange(_chatHistory);
            messages.Add(new UserChatMessage(UserInput));

            //the answer is asked for at most twice, a reasoning effort that this particular model
            //refuses is dropped and the very same request goes out again without it
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                gPTOutputText = "";
                try
                {
                    _bBBlog.Info("ChatGPT response: ");
                    AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = chatClient.CompleteChatStreamingAsync(messages, requestOptions);
                    ChatFinishReason? finishReason = null;
                    await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                    {
                        foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                        {
                            gPTOutputText += contentPart.Text;
                        }
                        //the last update tells us why the model stopped generating
                        if (completionUpdate.FinishReason != null)
                            finishReason = completionUpdate.FinishReason;
                    }
                    if (finishReason == ChatFinishReason.Length)
                    {
                        //the output cap was reached, posting half a sentence to a live stream reads
                        //as a broken bot, so nothing is said and the setting is reported instead
                        LastResponseError = $"The answer was cut off by the Max Tokens limit ({UserSettingsManager.Settings.GPTMaxTokens} tokens), raise Max Tokens in Settings.";
                        _bBBlog.Error("GPT answer hit the output token limit (finish_reason: length), nothing is posted. " + LastResponseError);
                        return null;
                    }
                    if (string.IsNullOrWhiteSpace(gPTOutputText))
                    {
                        //an empty answer is an error, returning "" here left the bot saying nothing and
                        //crashing the chat post on an empty Twitch message body
                        LastResponseError = $"GPT returned no answer content (finish_reason: {finishReason}). The model probably spent its whole budget on reasoning, raise Max Tokens or select another reasoning effort in Settings.";
                        _bBBlog.Error(LastResponseError);
                        return null;
                    }
                    //keep the conversation for context, the way the old Conversation object did
                    _chatHistory.Add(new UserChatMessage(UserInput));
                    _chatHistory.Add(new AssistantChatMessage(gPTOutputText));
                    while (_chatHistory.Count > maxChatHistoryMessages)
                    {
                        _chatHistory.RemoveAt(0);
                    }
                    _bBBlog.Info(gPTOutputText);
                    _bBBlog.Info("GPT Response done");
                    return gPTOutputText;
                }
                catch (System.Security.Authentication.AuthenticationException ex)
                {
                    LastResponseError = "GPT authentication failed: " + ex.Message;
                    _bBBlog.Error(LastResponseError);
                    return null;
                }
                catch (ClientResultException ex)
                {
                    string providerError = $"HTTP {ex.Status}: {ex.Message ?? ""}";
                    //an effort saved for another model can be refused here, retry without it
                    if (attempt == 1 && effortRequested && IsModelEffortRejection(providerError))
                    {
                        _bBBlog.Warn($"GPT refuses the saved reasoning effort for {model}, retrying without it: {providerError}");
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                        requestOptions.ReasoningEffortLevel = null;
#pragma warning restore OPENAI001
                        effortRequested = false;
                        //the setting and the cached verdict of that model are both wrong now, the
                        //next probe has to come from the API instead of offering it again
                        UserSettingsManager.Settings.GPTReasoningEffortComboBox = "";
                        if (UserSettingsManager.Settings.GPTReasoningLevelsCache != null)
                        {
                            UserSettingsManager.Settings.GPTReasoningLevelsCache.Remove(model);
                        }
                        UserSettingsManager.SaveSettings();
                        continue;
                    }
                    //no dialog here, the caller reports LastResponseError once
                    LastResponseError = $"GPT rejected the request ({providerError})";
                    if (LastResponseError.Length > 300)
                    {
                        LastResponseError = LastResponseError.Substring(0, 300) + "...";
                    }
                    _bBBlog.Error("GPT API error: " + providerError);
                    return null;
                }
            }
            LastResponseError = "GPT returned no answer.";
            _bBBlog.Error(LastResponseError);
            return null;
        }

        // "Unsupported parameter: 'reasoning_effort' is not supported with this model" and
        // "invalid <family> reasoning_effort" are the model speaking about the value, the schema
        // error that lists every allowed value and a token budget complaint are not. Anything else
        // keeps the value, hiding a working setting is the worse failure.
        private static bool IsModelEffortRejection(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }
            if (!message.Contains("reasoning_effort", StringComparison.OrdinalIgnoreCase)
                && !message.Contains("reasoning effort", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (message.Contains("allowed values", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Supported values", StringComparison.OrdinalIgnoreCase)
                || message.Contains("max_tokens", StringComparison.OrdinalIgnoreCase)
                || message.Contains("budget", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public void ChatGPTChatReset()
        {
            if (_chatHistory.Count > 0)
            {
                _bBBlog.Info("ChatGPTChatReset called, forcefully resetting the history");
                _chatHistory.Clear();
            } else
            {
                _bBBlog.Debug("ChatGPTChatReset failed, chat was not initialized anyway");
            }
        }
        public OpenAI()
        {
        }



    }
}
