using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BanterBrain_Buddy
{
    /// <summary>
    /// Google AI (Gemini) chat wrapper. Google provides an official OpenAI compatible API endpoint
    /// at https://generativelanguage.googleapis.com/v1beta/openai/, so this reuses the official
    /// OpenAI SDK with the endpoint pointed at Google instead of OpenAI. Like the other LLM
    /// wrappers all SDK types stay inside this class.
    /// CODING RULES:
    /// •	Local variables, private instance, static fields and method parameters should be camelCase.
    /// •	Methods, constants, properties, events and classes should be PascalCase.
    /// </summary>
    internal class GoogleAI
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // how many chat history messages we keep and send along for context
        private const int maxChatHistoryMessages = 40;
        // model that is used when the user setting is empty
        private const string defaultChatModel = "gemini-2.5-flash";
        // Google's OpenAI compatible base URL, the SDK appends /chat/completions and /models
        private static readonly Uri googleEndpoint = new("https://generativelanguage.googleapis.com/v1beta/openai/");

        private int SelectedOutputDevice;
        private OpenAIClient _googleClient;
        private readonly List<ChatMessage> _chatHistory = new();
        public string GoogleAPIKey { get; set; }
        // why the last GetGoogleAIResponse returned null, the caller shows this instead of guessing
        // at a key or subscription problem, the wrapper knows whether it was the token budget,
        // the reasoning effort or the server refusing the request
        public string LastResponseError { get; private set; } = "";

        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        private void SetSelectedOutputDevice(string OutputDevice)
        {
            _bBBlog.Info($"Setting selected output device for Google AI TTS to: {OutputDevice}");
            for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
            {
                var tmpOut = NAudio.Wave.WaveOut.GetCapabilities(i).ProductName;
                if (!string.IsNullOrEmpty(OutputDevice) && OutputDevice.StartsWith(tmpOut))
                {
                    _bBBlog.Debug($"Selected outputdevice = {tmpOut}");
                    SelectedOutputDevice = i;
                }
            }
        }

        // builds an OpenAI SDK client aimed at the Google Gemini endpoint instead of api.openai.com
        private static OpenAIClient CreateGoogleClient(string apiKey)
        {
            OpenAIClientOptions options = new()
            {
                Endpoint = googleEndpoint
            };
            return new OpenAIClient(new ApiKeyCredential(apiKey), options);
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
                OpenAIClient client = CreateGoogleClient(apiKey);
                await client.GetOpenAIModelClient().GetModelsAsync();
                return true;
            }
            catch (ClientResultException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Google API key validation unexpected error: " + ex.Message);
                return false;
            }
        }

        // the Google model listing also contains non-chat models (embeddings, imagen, aqa, etc.),
        // these name fragments are filtered out of the settings model selector
        private static readonly string[] nonChatModelTokens =
        {
            "embed", "imagen", "aqa", "tts", "transcribe", "translate", "audio"
        };

        // Pulls the chat-capable model names the key has access to from Google's model listing,
        // used by the settings UI to fill the model selector dynamically.
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
                OpenAIClient client = CreateGoogleClient(apiKey);
                ClientResult<OpenAIModelCollection> modelsResult = await client.GetOpenAIModelClient().GetModelsAsync();
                foreach (OpenAIModel model in modelsResult.Value)
                {
                    string id = model.Id;
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    if (id.StartsWith("models/"))
                    {
                        id = id.Substring(7);
                    }
                    if (!nonChatModelTokens.Any(token => id.Contains(token, StringComparison.OrdinalIgnoreCase)))
                    {
                        chatModels.Add(id);
                    }
                }
                chatModels.Sort();
                _bBBlog.Info($"Fetched {chatModels.Count} chat models from the Google model listing");
                return chatModels;
            }
            catch (ClientResultException ex)
            {
                _bBBlog.Error("Google model list fetch failed: " + ex.Status + " " + ex.Message);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Google model list fetch failed: " + ex.Message);
                return new List<string>();
            }
        }

        public static readonly string[] DefaultVoiceModels = new[]
        {
            "gemini-3.8-flash-tts",
            "gemini-3.8-flash-lite-tts",
            "gemini-3.1-flash-tts-preview",
            "gemini-2.5-flash-preview-tts",
            "gemini-2.5-pro-preview-tts"
        };

        public static async Task<List<string>> FetchVoiceModelNamesAsync(string apiKey)
        {
            List<string> voiceModels = new();
            if (string.IsNullOrEmpty(apiKey))
            {
                return new List<string>(DefaultVoiceModels);
            }
            try
            {
                OpenAIClient client = CreateGoogleClient(apiKey);
                ClientResult<OpenAIModelCollection> modelsResult = await client.GetOpenAIModelClient().GetModelsAsync();
                foreach (OpenAIModel model in modelsResult.Value)
                {
                    string id = model.Id;
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    if (id.StartsWith("models/"))
                    {
                        id = id.Substring(7);
                    }
                    if (id.Contains("tts", StringComparison.OrdinalIgnoreCase))
                    {
                        voiceModels.Add(id);
                    }
                }
                voiceModels = voiceModels
                    .OrderByDescending(m => m.Equals("gemini-3.8-flash-tts", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(m => m.Equals("gemini-3.8-flash-lite-tts", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(m => m)
                    .ToList();
                if (voiceModels.Count == 0)
                {
                    voiceModels = new List<string>(DefaultVoiceModels);
                }
                _bBBlog.Info($"Fetched {voiceModels.Count} voice models from the Google model listing");
                return voiceModels;
            }
            catch (ClientResultException ex)
            {
                _bBBlog.Error("Google voice model list fetch failed: " + ex.Status + " " + ex.Message);
                return new List<string>(DefaultVoiceModels);
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Google voice model list fetch failed: " + ex.Message);
                return new List<string>(DefaultVoiceModels);
            }
        }

        public static bool TryGetValidReasoningEffort(string reasoningEffort, out string effortLevel)
        {
            effortLevel = null;
            if (string.IsNullOrWhiteSpace(reasoningEffort))
            {
                return false;
            }
            string trimmed = reasoningEffort.Trim();
            if (trimmed.StartsWith("Auto", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("None", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            effortLevel = trimmed.ToLowerInvariant();
            return true;
        }

        private static string ExtractClientErrorMessage(ClientResultException ex)
        {
            try
            {
                BinaryData content = ex.GetRawResponse()?.Content;
                if (content != null)
                {
                    string body = content.ToString();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        JObject json = JObject.Parse(body);
                        string errMessage = (string)json["error"]?["message"];
                        if (!string.IsNullOrWhiteSpace(errMessage))
                        {
                            return $"{ex.Status} {errMessage}";
                        }
                    }
                }
            }
            catch
            {
                // Fall back to ex.Message if parsing raw response fails
            }
            return $"{ex.Status} {ex.Message}";
        }

        // the models endpoints do not report reasoning capabilities, so we ask the API itself:
        // a chat completion with an intentionally invalid reasoning effort is rejected with a
        // validation error before any token is generated, and that error lists the supported levels.
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
                OpenAIClient client = CreateGoogleClient(apiKey);
                ChatClient chatClient = client.GetChatClient(model);
                ChatCompletionOptions probeOptions = new()
                {
                    MaxOutputTokenCount = 1,
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                    ReasoningEffortLevel = probeEffort,
#pragma warning restore OPENAI001
                };
                await chatClient.CompleteChatAsync(new[] { new UserChatMessage("x") }, probeOptions);
                //the API accepted the bogus effort, or ignores it
                _bBBlog.Warn("Google accepted the reasoning effort probe, checking standard thinking levels");
                return (await FilterLevelsForModelAsync(apiKey, model, new List<string> { "Minimal", "Low", "Medium", "High" }), null);
            }
            catch (ClientResultException ex)
            {
                string message = ex.Message ?? "";
                _bBBlog.Info($"Google reasoning effort probe returned {ex.Status}: {message}");
                if (ex.Status == 400)
                {
                    string tail = ExtractProbeListTail(message);
                    int sentenceIndex = Math.Max(
                        message.IndexOf("Supported values", StringComparison.OrdinalIgnoreCase),
                        message.IndexOf("allowed values", StringComparison.OrdinalIgnoreCase));
                    if (tail == null && sentenceIndex >= 0)
                    {
                        tail = message.Substring(sentenceIndex);
                    }
                    if (tail != null)
                    {
                        foreach (Match value in Regex.Matches(tail, "['\"]([A-Za-z][A-Za-z0-9-]*)['\"]"))
                        {
                            AddProbeLevel(levels, value.Groups[1].Value, probeEffort);
                        }
                        if (levels.Count == 0)
                        {
                            Match listMatch = Regex.Match(tail, @"[A-Za-z][A-Za-z0-9-]*(?:\s*(?:,|and|or)\s*[A-Za-z][A-Za-z0-9-]*)+");
                            if (listMatch.Success)
                            {
                                foreach (string level in Regex.Split(listMatch.Value, @"\s*(?:,|and|or)\s*"))
                                {
                                    AddProbeLevel(levels, level.Trim(), probeEffort);
                                }
                            }
                        }
                        if (levels.Count > 0)
                        {
                            _bBBlog.Info($"Google reported {levels.Count} reasoning levels for {model}");
                            return (await FilterLevelsForModelAsync(apiKey, model, levels), null);
                        }
                    }

                    if (message.Contains("reasoning", StringComparison.OrdinalIgnoreCase)
                        && (message.Contains("not supported", StringComparison.OrdinalIgnoreCase)
                            || message.Contains("unsupported", StringComparison.OrdinalIgnoreCase)
                            || message.Contains("does not support", StringComparison.OrdinalIgnoreCase)))
                    {
                        _bBBlog.Info($"Google reports {model} has no reasoning settings");
                        return (levels, null);
                    }

                    // Fallback to testing standard Gemini reasoning levels if standard 400 was encountered
                    return (await FilterLevelsForModelAsync(apiKey, model, new List<string> { "Minimal", "Low", "Medium", "High" }), null);
                }
                _bBBlog.Error("Reasoning effort probe failed: " + ex.Status + " " + message);
                return (null, TruncateProbeMessage($"HTTP {ex.Status}: {message}"));
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Google reasoning effort probe failed: " + ex.Message);
                return (null, ex.Message);
            }
        }

        private static string TruncateProbeMessage(string message)
        {
            if (message != null && message.Length > 300)
            {
                return message.Substring(0, 300) + "...";
            }
            return message;
        }

        private static async Task<List<string>> FilterLevelsForModelAsync(string apiKey, string model, List<string> levels)
        {
            List<string> supported = new();
            foreach (string level in levels)
            {
                if (!TryGetValidReasoningEffort(level, out string effort))
                {
                    continue;
                }
                string rejection = await SendEffortProbeAsync(apiKey, model, effort);
                if (!string.IsNullOrEmpty(rejection))
                {
                    _bBBlog.Info($"Google model {model} refuses the reasoning effort \"{level}\": {rejection}");
                    continue;
                }
                supported.Add(level);
            }
            _bBBlog.Info($"Google model {model} accepts {supported.Count} of {levels.Count} reasoning effort values");
            return supported;
        }

        private static async Task<string> SendEffortProbeAsync(string apiKey, string model, string effort)
        {
            try
            {
                OpenAIClient client = CreateGoogleClient(apiKey);
                ChatClient chatClient = client.GetChatClient(model);
                ChatCompletionOptions probeOptions = new()
                {
                    MaxOutputTokenCount = 1,
                };
#pragma warning disable OPENAI001
                probeOptions.ReasoningEffortLevel = effort;
#pragma warning restore OPENAI001
                await chatClient.CompleteChatAsync(new[] { new UserChatMessage("x") }, probeOptions);
                return null;
            }
            catch (ClientResultException ex)
            {
                string detailedError = ExtractClientErrorMessage(ex);
                _bBBlog.Info($"Google reasoning effort probe for {model} / {effort} returned {detailedError}");
                return detailedError;
            }
            catch (Exception ex)
            {
                _bBBlog.Error($"Google reasoning effort probe for {model} / {effort} failed: " + ex.Message);
                return ex.Message;
            }
        }

        private static bool IsModelEffortRejection(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }
            if (!message.Contains("reasoning_effort", StringComparison.OrdinalIgnoreCase)
                && !message.Contains("reasoning effort", StringComparison.OrdinalIgnoreCase)
                && !message.Contains("thinking", StringComparison.OrdinalIgnoreCase)
                && !message.Contains("invalid argument", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (message.Contains("allowed values", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Supported values", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (message.Contains("max_tokens", StringComparison.OrdinalIgnoreCase)
                || message.Contains("budget", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        private static string ExtractProbeListTail(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return null;
            }
            for (int open = message.IndexOf('['); open >= 0; open = message.IndexOf('[', open + 1))
            {
                int close = message.IndexOf(']', open + 1);
                if (close <= open)
                {
                    continue;
                }
                string candidate = message.Substring(open, close - open + 1);
                if (candidate.Contains("'") || candidate.Contains("\""))
                {
                    return candidate;
                }
            }
            return null;
        }

        private static void AddProbeLevel(List<string> levels, string level, string probeEffort)
        {
            if (string.IsNullOrEmpty(level)
                || level.Equals(probeEffort, StringComparison.OrdinalIgnoreCase)
                || level.Equals("and", StringComparison.OrdinalIgnoreCase)
                || level.Equals("or", StringComparison.OrdinalIgnoreCase)
                || level.Equals("default", StringComparison.OrdinalIgnoreCase)
                || level.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            levels.Add(char.ToUpperInvariant(level[0]) + level.Substring(1).ToLowerInvariant());
        }

        // sends one tiny chat completion with the exact values from the settings UI so the
        // provider itself confirms key, model, temperature, token cap and reasoning effort
        // work together. Returns success or the provider error text for the UI to show.
        public static async Task<(bool success, string error)> TestSettingsAsync(string apiKey, string model, float temperature, int maxTokens, string reasoningEffort)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model))
            {
                return (false, "An API key and a model are required");
            }
            try
            {
                OpenAIClient client = CreateGoogleClient(apiKey);
                ChatClient chatClient = client.GetChatClient(model);
                ChatCompletionOptions options = new()
                {
                    Temperature = temperature,
                    MaxOutputTokenCount = maxTokens,
                };
                if (TryGetValidReasoningEffort(reasoningEffort, out string effort))
                {
#pragma warning disable OPENAI001
                    options.ReasoningEffortLevel = effort;
#pragma warning restore OPENAI001
                }
                await chatClient.CompleteChatAsync(new[] { new UserChatMessage("Reply with the single word OK.") }, options);
                _bBBlog.Info("Google settings test call answered");
                return (true, null);
            }
            catch (ClientResultException ex)
            {
                string detailedError = ExtractClientErrorMessage(ex);
                _bBBlog.Error("Google settings test rejected: " + detailedError);
                return (false, detailedError);
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Google settings test failed: " + ex.Message);
                return (false, ex.Message);
            }
        }

        // Function to validate the API key with a timeout
        // if it times out, we will retry once
        public async Task<bool> GoogleAICheckAPIKey()
        {
            _bBBlog.Debug("Verifying Google AI API key");
            if (string.IsNullOrEmpty(UserSettingsManager.Settings.GoogleAPIKey))
            {
                _bBBlog.Error("Google AI API Key is missing");
                return false;
            }
            else
            {
                GoogleAPIKey = UserSettingsManager.Settings.GoogleAPIKey;
                _googleClient ??= CreateGoogleClient(GoogleAPIKey);

                async Task<bool> ValidateAPIKeyWithTimeout()
                {
                    var validateTask = _googleClient.GetOpenAIModelClient().GetModelsAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15)); // 15 seconds timeout

                    var completedTask = await Task.WhenAny(validateTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        _bBBlog.Error("Google AI API Key validation timed out");
                        return false;
                    }

                    try
                    {
                        await validateTask;
                        return true;
                    }
                    catch (ClientResultException ex)
                    {
                        _bBBlog.Error("Google AI API Key rejected by server: " + ex.Status + " " + ex.Message);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        _bBBlog.Error("Google AI API Key check exception: " + ex.Message);
                        return false;
                    }
                }

                // First attempt
                if (await ValidateAPIKeyWithTimeout())
                {
                    _bBBlog.Info("Google AI API Key is valid");
                    return true;
                }

                // Retry once if the first attempt times out
                _bBBlog.Info("Retrying Google AI API Key validation");
                if (await ValidateAPIKeyWithTimeout())
                {
                    _bBBlog.Info("Google AI API Key is valid");
                    return true;
                }
                else
                {
                    _bBBlog.Error("Google AI API Key is invalid");
                    GoogleAPIKey = null;
                    return false;
                }
            }
        }

        private string NormalizeGoogleModel(string model)
        {
            if (string.IsNullOrEmpty(model))
            {
                _bBBlog.Info("No Google Model set, using " + defaultChatModel);
                return defaultChatModel;
            }
            return model;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public async Task<string> GetGoogleAIResponse(String UserInput, string tmpPersonaRoletext)
        {
            string googleOutputText = "";
            LastResponseError = "";
            _bBBlog.Info("Sending to Google AI LLM: " + UserInput);

            if (string.IsNullOrWhiteSpace(UserSettingsManager.Settings.GoogleAPIKey))
            {
                LastResponseError = "No Google AI API key is configured, set it in the settings form";
                _bBBlog.Error(LastResponseError);
                return googleOutputText;
            }

            _googleClient ??= CreateGoogleClient(UserSettingsManager.Settings.GoogleAPIKey);

            string model = NormalizeGoogleModel(UserSettingsManager.Settings.GoogleModel);
            _bBBlog.Info("Google Model: " + model);
            ChatClient chatClient = _googleClient.GetChatClient(model);

            ChatCompletionOptions requestOptions = new()
            {
                Temperature = UserSettingsManager.Settings.GoogleTemperature,
                MaxOutputTokenCount = UserSettingsManager.Settings.GoogleMaxTokens,
            };

            string reasoningEffort = UserSettingsManager.Settings.GoogleReasoningEffortComboBox;
            bool effortRequested = TryGetValidReasoningEffort(reasoningEffort, out string effortLevel);
            if (effortRequested)
            {
#pragma warning disable OPENAI001
                requestOptions.ReasoningEffortLevel = effortLevel;
#pragma warning restore OPENAI001
                _bBBlog.Info("Google reasoning effort set to: " + effortLevel);
            }

            _bBBlog.Info("SystemRole: " + tmpPersonaRoletext);
            List<ChatMessage> messages = new()
            {
                new SystemChatMessage(tmpPersonaRoletext)
            };
            messages.AddRange(_chatHistory);
            messages.Add(new UserChatMessage(UserInput));

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                googleOutputText = "";
                try
                {
                    _bBBlog.Info("Google AI response: ");
                    AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = chatClient.CompleteChatStreamingAsync(messages, requestOptions);
                    ChatFinishReason? finishReason = null;
                    await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                    {
                        foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                        {
                            googleOutputText += contentPart.Text;
                        }
                        if (completionUpdate.FinishReason != null)
                            finishReason = completionUpdate.FinishReason;
                    }
                    if (finishReason == ChatFinishReason.Length)
                    {
                        LastResponseError = $"The answer was cut off by the Max Tokens limit ({UserSettingsManager.Settings.GoogleMaxTokens} tokens). A reasoning model spends part of that budget on thinking, so raise Max Tokens in Settings or pick a lower reasoning effort.";
                        _bBBlog.Error("Google answer hit the output token limit (finish_reason: length), nothing is posted. " + LastResponseError);
                        return null;
                    }
                    if (string.IsNullOrWhiteSpace(googleOutputText))
                    {
                        LastResponseError = $"Google AI returned no answer content (finish_reason: {finishReason}). The model probably spent its whole budget on thinking, raise Max Tokens in Settings.";
                        _bBBlog.Error(LastResponseError);
                        return null;
                    }
                    _chatHistory.Add(new UserChatMessage(UserInput));
                    _chatHistory.Add(new AssistantChatMessage(googleOutputText));
                    while (_chatHistory.Count > maxChatHistoryMessages)
                    {
                        _chatHistory.RemoveAt(0);
                    }
                    _bBBlog.Info(googleOutputText);
                    _bBBlog.Info("Google AI Response done");
                    return googleOutputText;
                }
                catch (System.Security.Authentication.AuthenticationException ex)
                {
                    LastResponseError = "Google AI authentication failed: " + ex.Message;
                    _bBBlog.Error(LastResponseError);
                    return null;
                }
                catch (ClientResultException ex)
                {
                    string detailedError = ExtractClientErrorMessage(ex);
                    string providerError = $"HTTP {detailedError}";
                    if (attempt == 1 && effortRequested && (ex.Status == 400 || IsModelEffortRejection(providerError)))
                    {
                        _bBBlog.Warn($"Google refuses the saved reasoning effort for {model}, retrying without it: {providerError}");
#pragma warning disable OPENAI001
                        requestOptions.ReasoningEffortLevel = null;
#pragma warning restore OPENAI001
                        effortRequested = false;
                        UserSettingsManager.Settings.GoogleReasoningEffortComboBox = "";
                        if (UserSettingsManager.Settings.GoogleReasoningLevelsCache != null)
                        {
                            UserSettingsManager.Settings.GoogleReasoningLevelsCache.Remove(model);
                        }
                        UserSettingsManager.SaveSettings();
                        continue;
                    }
                    LastResponseError = TruncateProbeMessage($"Google rejected the request ({providerError})");
                    _bBBlog.Error("Google API error: " + providerError);
                    return null;
                }
                catch (Exception ex)
                {
                    LastResponseError = "Google AI request failed: " + ex.Message;
                    _bBBlog.Error(LastResponseError);
                    return null;
                }
            }
            LastResponseError = "Google AI returned no answer.";
            _bBBlog.Error(LastResponseError);
            return null;
        }

        public void GoogleAIChatReset()
        {
            if (_chatHistory.Count > 0)
            {
                _bBBlog.Info("GoogleAIChatReset called, forcefully resetting the history");
                _chatHistory.Clear();
            }
            else
            {
                _bBBlog.Debug("GoogleAIChatReset failed, chat was not initialized anyway");
            }
        }

        public async Task<bool> GoogleAITTS(string text, string outputDevice, string voice, int speed, int volumeControl, string voiceModel = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
            SetSelectedOutputDevice(outputDevice);

            string apiKey = UserSettingsManager.Settings.GoogleAPIKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = GoogleAPIKey;
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                LastResponseError = "No Google AI API key configured";
                _bBBlog.Error("Google AI TTS failed: " + LastResponseError);
                return false;
            }

            string voiceName = string.IsNullOrWhiteSpace(voice) ? "Puck" : voice;

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = text }
                        }
                    }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "AUDIO" },
                    speechConfig = new
                    {
                        voiceConfig = new
                        {
                            prebuiltVoiceConfig = new
                            {
                                voiceName = voiceName
                            }
                        }
                    }
                }
            };

            string jsonPayload = JsonConvert.SerializeObject(requestBody);
            byte[] audioBytes = null;

            string selectedVoiceModel = !string.IsNullOrWhiteSpace(voiceModel)
                ? voiceModel
                : (!string.IsNullOrWhiteSpace(UserSettingsManager.Settings.GoogleVoiceModel)
                    ? UserSettingsManager.Settings.GoogleVoiceModel
                    : "gemini-3.8-flash-tts");

            List<string> candidateModels = new() { selectedVoiceModel };
            foreach (string fallback in DefaultVoiceModels)
            {
                if (!candidateModels.Contains(fallback, StringComparer.OrdinalIgnoreCase))
                {
                    candidateModels.Add(fallback);
                }
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            string lastError = "";
            foreach (string ttsModel in candidateModels)
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{ttsModel}:generateContent?key={apiKey}";
                    using var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                    };

                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        try
                        {
                            JObject errJson = JObject.Parse(errorBody);
                            string msg = (string)errJson["error"]?["message"];
                            if (!string.IsNullOrEmpty(msg))
                            {
                                errorBody = msg;
                            }
                        }
                        catch { }
                        lastError = $"HTTP {(int)response.StatusCode}: {errorBody}";
                        _bBBlog.Warn($"Google AI TTS attempt on {ttsModel} returned {lastError}");
                        continue;
                    }

                    string responseJson = await response.Content.ReadAsStringAsync();
                    JObject jObj = JObject.Parse(responseJson);
                    string base64Audio = jObj["candidates"]?[0]?["content"]?["parts"]?[0]?["inlineData"]?["data"]?.ToString()
                        ?? jObj["candidates"]?[0]?["content"]?[0]?["inlineData"]?["data"]?.ToString();

                    if (!string.IsNullOrEmpty(base64Audio))
                    {
                        audioBytes = Convert.FromBase64String(base64Audio);
                        _bBBlog.Info($"Google AI TTS received {audioBytes.Length} audio bytes from {ttsModel}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _bBBlog.Error($"Google AI TTS request failed for {ttsModel}: " + ex.Message);
                }
            }

            if (audioBytes == null || audioBytes.Length == 0)
            {
                LastResponseError = !string.IsNullOrEmpty(lastError) ? lastError : "No audio received from any Gemini TTS model";
                _bBBlog.Error("Google AI TTS failed: " + LastResponseError);
                return false;
            }

            try
            {
                volumeControl = Math.Clamp(volumeControl, -100, 100);
                float volumeMultiplier = 1.0f + (volumeControl / 100.0f);

                var waveOut = new WaveOut
                {
                    DeviceNumber = SelectedOutputDevice
                };

                WaveStream waveStream;
                if (audioBytes.Length > 4 && audioBytes[0] == 'R' && audioBytes[1] == 'I' && audioBytes[2] == 'F' && audioBytes[3] == 'F')
                {
                    waveStream = new WaveFileReader(new MemoryStream(audioBytes));
                }
                else
                {
                    // Gemini audio default is 24kHz 16-bit mono PCM
                    var waveFormat = new WaveFormat(24000, 16, 1);
                    waveStream = new RawSourceWaveStream(new MemoryStream(audioBytes), waveFormat);
                }

                var sampleChannel = new SampleChannel(waveStream, false);
                var volumeProvider = new VolumeSampleProvider(sampleChannel)
                {
                    Volume = volumeMultiplier
                };

                waveOut.Init(volumeProvider);
                waveOut.Play();
                while (waveOut.PlaybackState != PlaybackState.Stopped)
                {
                    await Task.Delay(200);
                }
                waveOut.Dispose();
                waveStream.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Google AI TTS playback failed: " + ex.Message);
                return false;
            }
        }

        public GoogleAI()
        {
        }
    }
}
