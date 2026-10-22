using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BanterBrain_Buddy
{
    internal class OllamaLLM
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly OllamaApiClient _ollama;
        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        // Conversation turns we keep as context for the model. Local models usually run with a small
        // context window, so an ever growing history is itself a cause of truncated (or empty) answers.
        private const int maxHistoryMessages = 8;
        // the answer of a reasoning model is NOT stored here, only what it was actually asked/answered
        private readonly List<Message> _chatHistory = [];

        public async Task<List<string>> OllamaLLMGetModels()
        {
            if (_ollama == null)
            {
                _bBBlog.Error("OllamaApiClient not initialized");
                return null;
            }

            List<string> models = [];
            _bBBlog.Debug("OllamaLLMGetModels");

            IEnumerable<OllamaSharp.Models.Model> localModels;
            try
            {
                localModels = await _ollama.ListLocalModelsAsync();
            }
            catch (Exception e)
            {
                _bBBlog.Error("OllamaLLMGetModels failed: " + e.Message);
                return null;
            }
            //ListLocalModelsAsync() can complete without throwing but return null (e.g. when the /api/tags
            //response deserializes to a null collection). Guard before enumerating so Any() can't throw
            //ArgumentNullException, and treat it the same as "no models" (callers all handle a null return).
            if (localModels == null || !localModels.Any())
            {
                _bBBlog.Error("No local models found");
                return null;
            }
            foreach (var model in localModels)
            {
                _bBBlog.Debug("Model found: " + model.Name);
                models.Add(model.Name);
            }
            return models;
        }

        public async Task<string> OllamaGetResponse(string Text, string tmpRoleText)
        {
            if (_ollama == null)
            {
                _bBBlog.Error("OllamaApiClient not initialized");
                return null;
            }

            string model = UserSettingsManager.Settings.OllamaSelectedModel;
            if (string.IsNullOrWhiteSpace(model))
            {
                _bBBlog.Error("OllamaGetResponse failed, no model selected from saved settings.");
                return null;
            }

            int sentenceLength = 1;
            switch (UserSettingsManager.Settings.OllamaResponseLengthComboBox)
            {
                case "Short":
                    _bBBlog.Info("Ollama short response selected");
                    sentenceLength = 2;
                    break;
                case "Normal":
                    _bBBlog.Info("Ollama normal response selected");
                    sentenceLength = 4;
                    break;
                case "Long":
                    _bBBlog.Info("Ollama long response selected");
                    sentenceLength = 8;
                    break;
            }
            string tmpSetupString = tmpRoleText + ". Make your response a maximum of " + sentenceLength + " sentences! Absolutely not longer! How would you then respond to: " + Text;

            //Reasoning models spend their whole token budget on hidden "think" tokens before they answer.
            //With a small context window that results in an empty answer (done_reason: length) after 20+
            //seconds, so thinking is off unless the user asks for it in Settings.
            bool think = UserSettingsManager.Settings.OllamaThinkingCheckBox;
            //0 means "leave it to the Ollama server default"
            int contextTokens = UserSettingsManager.Settings.OllamaContextTokens;

            var request = new ChatRequest
            {
                Model = model,
                Stream = true,
                Think = think,
                Messages = BuildMessages(tmpSetupString),
            };
            if (contextTokens > 0)
            {
                request.Options = new RequestOptions { NumCtx = contextTokens };
            }

            _bBBlog.Info("Sending to Ollama LLM: " + Text);
            _bBBlog.Info("SystemRole: " + tmpRoleText + " \r\nModel: " + model + " thinking: " + think + " context tokens: " + (contextTokens > 0 ? contextTokens.ToString() : "server default"));

            var response = new System.Text.StringBuilder();
            string doneReason = "";
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                await foreach (var chunk in _ollama.ChatAsync(request, cts.Token))
                {
                    if (chunk == null)
                        continue;
                    if (chunk.Message?.Content != null)
                        response.Append(chunk.Message.Content);
                    //the last chunk of a stream carries why generation stopped, only that one has the reason
                    if (chunk is ChatDoneResponseStream doneChunk)
                        doneReason = doneChunk.DoneReason;
                }
            }
            catch (Exception e)
            {
                _bBBlog.Error("OllamaGetResponse failed: " + e.Message);
                return null;
            }

            string result = response.ToString();

            if (string.IsNullOrWhiteSpace(result))
            {
                //an empty answer must be reported as an error, returning "" here used to make the bot
                //silently say nothing and later crash the chat post on an empty Twitch message body
                _bBBlog.Error($"Ollama returned no answer (done_reason: '{doneReason}'). The model most likely used the whole context for reasoning. Turn thinking off in Settings or raise the Ollama context tokens.");
                return null;
            }

            if (string.Equals(doneReason, "length", StringComparison.OrdinalIgnoreCase))
                _bBBlog.Warn("Ollama answer hit the token/context limit and may be incomplete (done_reason: length). Raise the Ollama context tokens in Settings.");

            //keep the conversation for context, but only the last few turns (see maxHistoryMessages)
            _chatHistory.Add(new Message(ChatRole.User, Text));
            _chatHistory.Add(new Message(ChatRole.Assistant, result));
            while (_chatHistory.Count > maxHistoryMessages)
            {
                _chatHistory.RemoveAt(0);
            }

            _bBBlog.Info("Ollama response: " + result);
            return result;
        }

        //prior turns plus the current question for a single request
        private List<Message> BuildMessages(string prompt)
        {
            var messages = new List<Message>();
            foreach (var previous in _chatHistory)
            {
                messages.Add(previous);
            }
            messages.Add(new Message(ChatRole.User, prompt));
            return messages;
        }

        public async Task<bool> OllamaVerify()
        {
            _bBBlog.Info("OllamaVerify called");

            var result = await OllamaLLMGetModels();

            if (result == null || result.Count == 0)
            {
                _bBBlog.Error("OllamaVerify failed, no models found or not running on URI");
                return false;
            }
            if (!result.Contains(UserSettingsManager.Settings.OllamaSelectedModel))
            {
                _bBBlog.Error("OllamaVerify failed, selected model not found");
                return false;
            }
            return true;
        }

        //we reset the conversation
        public void OllamaChatReset()
        {
            if (_chatHistory.Count > 0)
            {
                _bBBlog.Info("OllamaChatReset called, forcefully resetting the history");
                _chatHistory.Clear();
            }
            else
            {
                _bBBlog.Debug("OllamaChatReset failed, chat was not initialized anyway");
            }
        }

        public OllamaLLM(string LocalUri)
        {
            _bBBlog.Info("OllamaLLM init on Uri: " + LocalUri);
            _ollama = new OllamaApiClient(new Uri(LocalUri));
        }
    }
}
