using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BanterBrain_Buddy
{
    /// <summary>
    /// Groq chat wrapper. Groq serves an OpenAI compatible API, so this reuses the official
    /// OpenAI SDK with the endpoint pointed at Groq instead of OpenAI. Like the other LLM
    /// wrappers all SDK types stay inside this class.
    /// CODING RULES:
    /// •	Local variables, private instance, static fields and method parameters should be camelCase.
    /// •	Methods, constants, properties, events and classes should be PascalCase.
    /// </summary>
    internal class Groq
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // how many chat history messages we keep and send along for context
        private const int maxChatHistoryMessages = 40;
        // model that is used when the user setting is empty
        private const string defaultChatModel = "llama-3.1-8b-instant";
        // Groq's OpenAI compatible base URL, the SDK appends /chat/completions and /models
        private static readonly Uri groqEndpoint = new("https://api.groq.com/openai/v1/");

        private OpenAIClient _groqClient;
        private readonly List<ChatMessage> _chatHistory = new();
        public string GroqAPIKey { get; set; }
        // why the last GetGroqResponse returned null, the caller shows this instead of guessing
        // at a key or subscription problem, the wrapper knows whether it was the token budget,
        // the reasoning effort or the server refusing the request
        public string LastResponseError { get; private set; } = "";

        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        // builds an OpenAI SDK client aimed at the Groq endpoint instead of api.openai.com
        private static OpenAIClient CreateGroqClient(string apiKey)
        {
            OpenAIClientOptions options = new()
            {
                Endpoint = groqEndpoint
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
                OpenAIClient client = CreateGroqClient(apiKey);
                await client.GetOpenAIModelClient().GetModelsAsync();
                return true;
            }
            catch (ClientResultException)
            {
                return false;
            }
        }

        // the Groq model listing also contains models that can never be used for chat
        // (transcription, speech and embeddings), these name fragments are filtered out of
        // the settings model selector
        private static readonly string[] nonChatModelTokens =
        {
            "whisper", "tts", "transcribe", "translate", "embed", "audio"
        };

        // Pulls the chat-capable model names the key has access to from Groq's model listing,
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
                OpenAIClient client = CreateGroqClient(apiKey);
                ClientResult<OpenAIModelCollection> modelsResult = await client.GetOpenAIModelClient().GetModelsAsync();
                foreach (OpenAIModel model in modelsResult.Value)
                {
                    string id = model.Id;
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    if (!nonChatModelTokens.Any(token => id.Contains(token, StringComparison.OrdinalIgnoreCase)))
                    {
                        chatModels.Add(id);
                    }
                }
                chatModels.Sort();
                _bBBlog.Info($"Fetched {chatModels.Count} chat models from the Groq model listing");
                return chatModels;
            }
            catch (ClientResultException ex)
            {
                _bBBlog.Error("Groq model list fetch failed: " + ex.Status + " " + ex.Message);
                return new List<string>();
            }
        }

        // the models endpoints do not report reasoning capabilities, so we ask the API itself,
        // the same probe trick the OpenAI wrapper uses: a chat completion with an intentionally
        // invalid reasoning effort is rejected with a validation error before any token is
        // generated, and that error lists the supported levels. Returns the levels for the
        // model, an empty list when the model has no reasoning settings at all, or Levels=null
        // with a reason in Error when the API gave no answer (bad key, no credits, network, ...)
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
                OpenAIClient client = CreateGroqClient(apiKey);
                ChatClient chatClient = client.GetChatClient(model);
                ChatCompletionOptions probeOptions = new()
                {
                    //no output cap here on purpose: reasoning models with a thinking budget can
                    //reject a tiny max_tokens before the effort value is validated, which would
                    //mask the levels list. An invalid effort is always rejected before generation,
                    //and if a model ever accepts the bogus value we only pay for a short reply
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                    ReasoningEffortLevel = probeEffort,
#pragma warning restore OPENAI001
                };
                await chatClient.CompleteChatAsync(new[] { new UserChatMessage("x") }, probeOptions);
                //the API accepted the bogus effort, the supported levels cannot be read from it
                _bBBlog.Warn("Groq accepted the reasoning effort probe, cannot derive supported levels");
                return (null, "Groq accepted the probe request, it did not report any validation error");
            }
            catch (ClientResultException ex)
            {
                string message = ex.Message ?? "";
                //keep the raw rejection text in the log, Groq words this differently per model family
                _bBBlog.Info($"Groq reasoning effort probe returned {ex.Status}: {message}");
                if (ex.Status == 400)
                {
                    //OpenAI words it: "Invalid value: 'zzz-effort-probe'. Supported values are: 'low', 'medium', and 'high'."
                    //Groq words it:  "'reasoning_effort' : value is not one of the allowed values ['none','default','minimal','low','medium','high','xhigh','max']"
                    //so prefer a bracketed list anywhere in the message, then fall back to the sentence
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
                        //levels can be quoted with single or double quotes
                        foreach (Match value in Regex.Matches(tail, "['\"]([A-Za-z][A-Za-z0-9-]*)['\"]"))
                        {
                            AddProbeLevel(levels, value.Groups[1].Value, probeEffort);
                        }
                        //...or listed bare: "Supported values are: low, medium and high"
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
                            _bBBlog.Info($"Groq reported {levels.Count} reasoning levels for {model}");
                            //that enum is the same for every model, Groq validates the parameter
                            //before it looks at the model, so each value has to be tried on it
                            return (await FilterLevelsForModelAsync(apiKey, model, levels), null);
                        }
                        //the list was there but the levels could not be read from it
                        _bBBlog.Error("Could not parse reasoning levels from the Groq probe message, see the log line above");
                        return (null, TruncateProbeMessage($"HTTP 400: {message}"));
                    }
                    //only call it unsupported when the error really says so, other 400s (for example a
                    //token budget rejection on a thinking model) must surface as a load failure
                    if (message.Contains("reasoning", StringComparison.OrdinalIgnoreCase)
                        && (message.Contains("not supported", StringComparison.OrdinalIgnoreCase)
                            || message.Contains("unsupported", StringComparison.OrdinalIgnoreCase)
                            || message.Contains("does not support", StringComparison.OrdinalIgnoreCase)))
                    {
                        _bBBlog.Info($"Groq reports {model} has no reasoning settings");
                        return (levels, null);
                    }
                }
                //any other status (401, 404, 429, ...) means we got no answer for the model,
                //hand Groq's own reason to the UI so the dialog can show what went wrong
                _bBBlog.Error("Reasoning effort probe failed: " + ex.Status + " " + message);
                return (null, TruncateProbeMessage($"HTTP {ex.Status}: {message}"));
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Groq reasoning effort probe failed: " + ex.Message);
                return (null, ex.Message);
            }
        }

        // keeps the provider error text that the settings dialog shows to a readable size
        private static string TruncateProbeMessage(string message)
        {
            if (message != null && message.Length > 300)
            {
                return message.Substring(0, 300) + "...";
            }
            return message;
        }

        // Groq checks reasoning_effort against one global enum before it looks at the model, so the
        // parsed list is only a parameter schema. Every value has to be tried on the model itself:
        // an unusable one is refused while the request is validated, which costs nothing, an accepted
        // one answers with a single token. Only the values the model takes are kept.
        private static async Task<List<string>> FilterLevelsForModelAsync(string apiKey, string model, List<string> levels)
        {
            List<string> supported = new();
            foreach (string level in levels)
            {
                string rejection = await SendEffortProbeAsync(apiKey, model, level.ToLowerInvariant());
                if (IsModelEffortRejection(rejection))
                {
                    _bBBlog.Info($"Groq model {model} refuses the reasoning effort \"{level}\": {rejection}");
                    continue;
                }
                supported.Add(level);
            }
            _bBBlog.Info($"Groq model {model} accepts {supported.Count} of {levels.Count} reasoning effort values");
            return supported;
        }

        // sends one request with a single output token and the given effort, returns null when the
        // API took it and the rejection text when it did not
        private static async Task<string> SendEffortProbeAsync(string apiKey, string model, string effort)
        {
            try
            {
                OpenAIClient client = CreateGroqClient(apiKey);
                ChatClient chatClient = client.GetChatClient(model);
                ChatCompletionOptions probeOptions = new()
                {
                    MaxOutputTokenCount = 1,
                };
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                probeOptions.ReasoningEffortLevel = effort;
#pragma warning restore OPENAI001
                await chatClient.CompleteChatAsync(new[] { new UserChatMessage("x") }, probeOptions);
                return null;
            }
            catch (ClientResultException ex)
            {
                string message = $"{ex.Status}: {ex.Message ?? ""}";
                _bBBlog.Info($"Groq reasoning effort probe for {model} / {effort} returned {message}");
                return message;
            }
            catch (Exception ex)
            {
                _bBBlog.Error($"Groq reasoning effort probe for {model} / {effort} failed: " + ex.Message);
                return ex.Message;
            }
        }

        // "invalid Qwen3.8 reasoning_effort" is the model speaking about the value, the schema
        // error that lists every allowed value and the token budget complaints are not. Anything
        // that does not name the effort parameter keeps the value, hiding a working setting is the
        // worse failure than offering one the model ignores.
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
            //every 400 carries the invalid_request_error type and the schema error carries the value
            //list, neither is a verdict of the model about this particular value
            if (message.Contains("allowed values", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Supported values", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            //a thinking model asked for a bigger budget also mentions the parameter, that is an
            //answer about max_tokens and the value itself was accepted
            if (message.Contains("max_tokens", StringComparison.OrdinalIgnoreCase)
                || message.Contains("budget", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        // returns the bracketed part of a rejection message that holds the accepted values,
        // "['low','high']" out of "... not one of the allowed values ['low','high']", or null
        // when the message has no such list. Only brackets with quoted words count, so an
        // unrelated "[" in some other error text is not mistaken for a value list.
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

        // adds one parsed reasoning effort level to the list, skipping the bogus probe value,
        // the connecting words of bare lists and "default", which the "Auto (model default)"
        // entry of the selector already stands for. Shown title cased like the built-in items.
        private static void AddProbeLevel(List<string> levels, string level, string probeEffort)
        {
            if (string.IsNullOrEmpty(level)
                || level.Equals(probeEffort, StringComparison.OrdinalIgnoreCase)
                || level.Equals("and", StringComparison.OrdinalIgnoreCase)
                || level.Equals("or", StringComparison.OrdinalIgnoreCase)
                || level.Equals("default", StringComparison.OrdinalIgnoreCase))
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
                OpenAIClient client = CreateGroqClient(apiKey);
                ChatClient chatClient = client.GetChatClient(model);
                ChatCompletionOptions options = new()
                {
                    Temperature = temperature,
                    MaxOutputTokenCount = maxTokens,
                };
                //send the effort the same way GetGroqResponse does, so the test covers it too
                if (!string.IsNullOrEmpty(reasoningEffort) && !reasoningEffort.StartsWith("Auto", StringComparison.Ordinal))
                {
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                    options.ReasoningEffortLevel = reasoningEffort.ToLowerInvariant();
#pragma warning restore OPENAI001
                }
                await chatClient.CompleteChatAsync(new[] { new UserChatMessage("Reply with the single word OK.") }, options);
                _bBBlog.Info("Groq settings test call answered");
                return (true, null);
            }
            catch (ClientResultException ex)
            {
                _bBBlog.Error("Groq settings test rejected: " + ex.Status + " " + ex.Message);
                return (false, ex.Status + " " + ex.Message);
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Groq settings test failed: " + ex.Message);
                return (false, ex.Message);
            }
        }

        // Function to validate the API key with a timeout
        // if it times out, we will retry once
        public async Task<bool> GroqCheckAPIKey()
        {
            _bBBlog.Debug("Verifying Groq API key");
            if (string.IsNullOrEmpty(UserSettingsManager.Settings.GroqAPIKey))
            {
                _bBBlog.Error("Groq API Key is missing");
                return false;
            }
            else
            {
                GroqAPIKey = UserSettingsManager.Settings.GroqAPIKey;
                _groqClient ??= CreateGroqClient(GroqAPIKey);

                // Function to validate the API key with a timeout
                async Task<bool> ValidateAPIKeyWithTimeout()
                {
                    var validateTask = _groqClient.GetOpenAIModelClient().GetModelsAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15)); // 15 seconds timeout

                    var completedTask = await Task.WhenAny(validateTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        _bBBlog.Error("Groq API Key validation timed out");
                        return false;
                    }

                    try
                    {
                        await validateTask;
                        return true;
                    }
                    catch (ClientResultException ex)
                    {
                        _bBBlog.Error("Groq API Key rejected by server: " + ex.Status + " " + ex.Message);
                        return false;
                    }
                }

                // First attempt
                if (await ValidateAPIKeyWithTimeout())
                {
                    _bBBlog.Info("Groq API Key is valid");
                    return true;
                }

                // Retry once if the first attempt times out
                _bBBlog.Info("Retrying Groq API Key validation");
                if (await ValidateAPIKeyWithTimeout())
                {
                    _bBBlog.Info("Groq API Key is valid");
                    return true;
                }
                else
                {
                    _bBBlog.Error("Groq API Key is invalid");
                    GroqAPIKey = null;
                    return false;
                }
            }
        }

        // falls back to the default model when the setting is empty, Groq retires model
        // names regularly so an unknown saved name is still passed through, the server
        // error then tells the user which model is gone
        private string NormalizeGroqModel(string model)
        {
            if (string.IsNullOrEmpty(model))
            {
                _bBBlog.Info("No Groq Model set, using " + defaultChatModel);
                return defaultChatModel;
            }
            return model;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public async Task<string> GetGroqResponse(String UserInput, string tmpPersonaRoletext)
        {
            string groqOutputText = "";
            LastResponseError = "";
            _bBBlog.Info("Sending to Groq LLM: " + UserInput);

            //the SDK credential throws on an empty key, report it the same way as an API error
            if (string.IsNullOrWhiteSpace(UserSettingsManager.Settings.GroqAPIKey))
            {
                LastResponseError = "No Groq API key is configured, set it in the settings form";
                _bBBlog.Error(LastResponseError);
                return groqOutputText;
            }

            _groqClient ??= CreateGroqClient(UserSettingsManager.Settings.GroqAPIKey);

            string model = NormalizeGroqModel(UserSettingsManager.Settings.GroqModel);
            _bBBlog.Info("Groq Model: " + model);
            ChatClient chatClient = _groqClient.GetChatClient(model);

            // Groq hosts classic instruction models, temperature and the output cap always apply
            ChatCompletionOptions requestOptions = new()
            {
                Temperature = UserSettingsManager.Settings.GroqTemperature,
                MaxOutputTokenCount = UserSettingsManager.Settings.GroqMaxTokens,
            };
            //the dropdown lets the user pick the thinking budget on Groq reasoning models
            //(qwen3, gpt-oss, ...), an empty setting or "Auto" keeps the library default.
            //The levels come from the model itself (see FetchReasoningEffortLevelsAsync),
            //so send the chosen one as-is and let the API validate it
            string reasoningEffort = UserSettingsManager.Settings.GroqReasoningEffortComboBox;
            bool effortRequested = !string.IsNullOrEmpty(reasoningEffort) && !reasoningEffort.StartsWith("Auto", StringComparison.Ordinal);
            if (effortRequested)
            {
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                requestOptions.ReasoningEffortLevel = reasoningEffort.ToLowerInvariant();
#pragma warning restore OPENAI001
                _bBBlog.Info("Groq reasoning effort set to: " + reasoningEffort);
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
                groqOutputText = "";
                try
                {
                    _bBBlog.Info("Groq response: ");
                    AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = chatClient.CompleteChatStreamingAsync(messages, requestOptions);
                    ChatFinishReason? finishReason = null;
                    await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                    {
                        foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                        {
                            groqOutputText += contentPart.Text;
                        }
                        //the last update tells us why the model stopped generating
                        if (completionUpdate.FinishReason != null)
                            finishReason = completionUpdate.FinishReason;
                    }
                    if (finishReason == ChatFinishReason.Length)
                    {
                        //the output cap was reached, posting half a sentence to a live stream reads
                        //as a broken bot, so nothing is said and the setting is reported instead
                        LastResponseError = $"The answer was cut off by the Max Tokens limit ({UserSettingsManager.Settings.GroqMaxTokens} tokens). A reasoning model spends part of that budget on thinking, so raise Max Tokens in Settings or pick a lower reasoning effort.";
                        _bBBlog.Error("Groq answer hit the output token limit (finish_reason: length), nothing is posted. " + LastResponseError);
                        return null;
                    }
                    if (string.IsNullOrWhiteSpace(groqOutputText))
                    {
                        //an empty answer is an error, returning "" here left the bot saying nothing and
                        //crashing the chat post on an empty Twitch message body
                        LastResponseError = $"Groq returned no answer content (finish_reason: {finishReason}). The model probably spent its whole budget on thinking, raise Max Tokens in Settings.";
                        _bBBlog.Error(LastResponseError);
                        return null;
                    }
                    //keep the conversation for context, the way the OpenAI wrapper does
                    _chatHistory.Add(new UserChatMessage(UserInput));
                    _chatHistory.Add(new AssistantChatMessage(groqOutputText));
                    while (_chatHistory.Count > maxChatHistoryMessages)
                    {
                        _chatHistory.RemoveAt(0);
                    }
                    _bBBlog.Info(groqOutputText);
                    _bBBlog.Info("Groq Response done");
                    return groqOutputText;
                }
                catch (System.Security.Authentication.AuthenticationException ex)
                {
                    LastResponseError = "Groq authentication failed: " + ex.Message;
                    _bBBlog.Error(LastResponseError);
                    return null;
                }
                catch (ClientResultException ex)
                {
                    string providerError = $"HTTP {ex.Status}: {ex.Message ?? ""}";
                    //an effort saved for another model can be refused here, the model only checks
                    //it once the value passes the global schema, so retry without it
                    if (attempt == 1 && effortRequested && IsModelEffortRejection(providerError))
                    {
                        _bBBlog.Warn($"Groq refuses the saved reasoning effort for {model}, retrying without it: {providerError}");
#pragma warning disable OPENAI001 // the SDK still flags the reasoning effort level as experimental
                        requestOptions.ReasoningEffortLevel = null;
#pragma warning restore OPENAI001
                        effortRequested = false;
                        //the setting and the cached verdict of that model are both wrong now, the
                        //next probe has to come from the API instead of offering it again
                        UserSettingsManager.Settings.GroqReasoningEffortComboBox = "";
                        if (UserSettingsManager.Settings.GroqReasoningLevelsCache != null)
                        {
                            UserSettingsManager.Settings.GroqReasoningLevelsCache.Remove(model);
                        }
                        UserSettingsManager.SaveSettings();
                        continue;
                    }
                    //no dialog here, the caller reports LastResponseError once
                    LastResponseError = TruncateProbeMessage($"Groq rejected the request ({providerError})");
                    _bBBlog.Error("Groq API error: " + providerError);
                    return null;
                }
            }
            LastResponseError = "Groq returned no answer.";
            _bBBlog.Error(LastResponseError);
            return null;
        }

        public void GroqChatReset()
        {
            if (_chatHistory.Count > 0)
            {
                _bBBlog.Info("GroqChatReset called, forcefully resetting the history");
                _chatHistory.Clear();
            }
            else
            {
                _bBBlog.Debug("GroqChatReset failed, chat was not initialized anyway");
            }
        }

        public Groq()
        {
        }
    }
}
