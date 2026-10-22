using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Forms;
using BanterBrain_Buddy.TwitchEventhandlers;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;


/// <summary>
/// CODING RULES:
/// •	Local variables, private instance, static fields and method parameters should be camelCase.
/// •	Public instance fields, methods, constants, properties, events and classes should be PascalCase.
/// </summary>

namespace BanterBrain_Buddy
{
    //eventhandler for valid chat commands
    public delegate void ESubChatEventHandler(object source, OnChatEventArgs e);
    //eventhandler for valid cheers
    public delegate void ESubCheerEventHandler(object source, OnCheerEventsArgs e);
    //evnthandler for subscriptions
    public delegate void ESubSubscribeEventHandler(object source, OnSubscribeEventArgs e);
    //eventhandler for re-subscriptions
    public delegate void ESubReSubscribeEventHandler(object source, OnReSubscribeEventArgs e);
    //eventhandler for subscription gifts
    public delegate void EsubSubscriptionGiftEventHandler(object source, OnSubscriptionGiftEventArgs e);
    //eventhandler for custom channel point redemptions
    public delegate void ESubChannelPointRedemptionEventHandler(object source, OnChannelPointCustomRedemptionEventArgs e);



    public class TwitchAPIESub
    {
        private static readonly log4net.ILog _bBBlog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //The eventhandler for a valid chat command triggered, it returns the username as [0] and the message as [1] in a string array
        public event ESubChatEventHandler OnESubChatMessage;
        //the eventhandler for a valid cheer, it returns the username as [0] and the message as [1] in a string array
        public event ESubCheerEventHandler OnESubCheerMessage;
        //the eventhandler for a subscription, it returns the username as [0] and the broadcaster as [1] in a string array
        public event ESubSubscribeEventHandler OnESubSubscribe;
        //the eventhandler for a re-subscription, it returns the username as [0] and the message as [1] in a string array
        public event ESubReSubscribeEventHandler OnESubReSubscribe;
        //the eventhandler for a subscription gift, it returns the username as [0] and the message as [1] in a string array
        public event EsubSubscriptionGiftEventHandler OnESubSubscriptionGift;
        //the eventhandler for custom channel poitn redemptions it returns the username as [0] and the message as [1] in a string array
        public event ESubChannelPointRedemptionEventHandler OnESubChannelPointRedemption;

        public string TwitchAccessToken { get; private set; }
        //Display name of the account that was just authorized via the implicit flow, so the UI can
        //auto-fill the matching "Account Name" field alongside the access token.
        public string TwitchAuthorizedUserName { get; private set; }
        private bool TwitchAuthRequestResult { get; set; }

        private static string TwitchAuthRedirect { get; set; }
        private static string TwitchAuthClientId { get; set; }

        private static string TwitchUserID { get; set; }
        private static string TwitchChannelID { get; set; }

        private static string TwitchChatUserID { get; set; }
        private static string TwitchChatAccessToken { get; set; }

        private bool TwitchDoAutomatedCheck { get; set; }

        //set when the websocket is stopped on purpose so the disconnect handler stops reconnecting
        private bool _eventSubIntentionalStop { get; set; }

        private bool TwitchMock { get; set; }

        public bool EventSubReadChatMessages { get; private set; }

        //needed for timeout of command trigger
        private bool IsCommandTriggered { get; set; }
        public bool EventSubCheckCheer { get; private set; }
        public bool EventSubCheckSubscriptions { get; private set; }
        public bool EventSubCheckSubscriptionGift { get; private set; }
        public bool EventSubCheckChannelPointRedemption { get; private set; }

        private readonly Dictionary<string, string> _eventSubIllist;

        private Dictionary<string, string> _conditions;

        private static TwitchLib.Api.TwitchAPI _gTwitchAPI;

        //if this is set, we need to send a test message to a channel on join.
        public string TwitchSendTestMessageOnJoin { get; set; }

        private SettingsManager UserSettingsManager = SettingsManager.Instance;

        public readonly EventSubWebsocketClient _eventSubWebsocketClient;

        public TwitchAPIESub()
        {
            TwitchAccessToken = "";
            TwitchAuthorizedUserName = "";
            TwitchAuthRequestResult = false;
            TwitchReadSettings();
            TwitchDoAutomatedCheck = true;
            _eventSubIllist = [];
            _gTwitchAPI = new TwitchLib.Api.TwitchAPI();
            _eventSubWebsocketClient = new EventSubWebsocketClient();
            
        }

        /// <summary>
        /// This will initialize the event handlers for the TwitchEventSub
        /// </summary>
        public async Task<bool> EventSubInit(string TwAuthToken, string TwUsername, string TwChannelName)
        {
            _bBBlog.Info("Set required globals for EventSub");
            IsCommandTriggered = false;
            TwitchAccessToken = TwAuthToken;

            if (await ValidateAccessToken(TwitchAccessToken))
            {
                try
                {
                    var broadCaster = (await _gTwitchAPI.Helix.Users.GetUsersAsync(null, [TwChannelName])).Users;
                    var messageSender = (await _gTwitchAPI.Helix.Users.GetUsersAsync(null, [TwUsername])).Users;
                    //a renamed or deleted account returns an empty list, indexing [0] would throw
                    if (messageSender == null || messageSender.Length < 1)
                    {
                        _bBBlog.Error($"Twitch user {TwUsername} was not found, cannot initialize EventSub");
                        return false;
                    }
                    if (broadCaster == null || broadCaster.Length < 1)
                    {
                        _bBBlog.Error($"Twitch channel {TwChannelName} was not found, cannot initialize EventSub");
                        return false;
                    }
                    TwitchUserID = messageSender[0].Id;
                    //this works great for validating
                    TwitchChannelID = broadCaster[0].Id;
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Looking up Twitch users for EventSub init failed: " + ex.Message);
                    return false;
                }
                //monitor channel ID, if the authorization user is not the same as the channel ID, we need to let the user know
                if (TwitchUserID != TwitchChannelID)
                {
                    _bBBlog.Debug("Authorization user is not the same as the channel ID");
                }

                _bBBlog.Info("TwitchEventSub eventhandlers Errors start");
                _eventSubWebsocketClient.WebsocketConnected += EventSubOnWebsocketConnected;
                _eventSubWebsocketClient.WebsocketDisconnected += EventSubOnWebsocketDisconnected;
                _eventSubWebsocketClient.WebsocketReconnected += EventSubOnWebsocketReconnected;
                _eventSubWebsocketClient.ErrorOccurred += EventSubOnErrorOccurred;


                _bBBlog.Info("TwitchEventSub eventhandlers setting done");
                return true;
            }
            else
            {
                _bBBlog.Error("TwitchEventSub failed to initialize. Most likely due to failing key");
                return false;
            }
        }

        public async Task EventSubStopReadChat()
        {
            _bBBlog.Info("Unsubscribe from reading chat.");
            EventSubReadChatMessages = false;
            await EventSubUnsubscribe("channel.chat.message");
        }

        public async Task EventSubStopChannelPointRedemption()
        {
            _bBBlog.Info("Unsubscribe from checking channel point redemptions.");
            EventSubCheckChannelPointRedemption = false;
            await EventSubUnsubscribe("channel.channel_points_custom_reward_redemption.add");
        }

        public async Task EventSubStopSubscriptionGift()
        {
            _bBBlog.Info("Unsubscribe from checking subscription gifts.");
            EventSubCheckSubscriptionGift = false;
            await EventSubUnsubscribe("channel.subscription.gift");
        }

        public async Task EventSubStopCheer()
        {
            _bBBlog.Info("Unsubscribe from checking cheers.");
            EventSubCheckCheer = false;
            await EventSubUnsubscribe("channel.cheer");
        }

        public async Task EventSubStopSubscription()
        {
            _bBBlog.Info("Unsubscribe from listening to subs and re-subscriptions.");
            EventSubCheckSubscriptions = false;
            await EventSubUnsubscribe("channel.subscribe");
            await EventSubUnsubscribe("channel.subscription.message");
        }

        public async void EventSubHandleReadchat(string command, bool follower, bool subscriber)
        {
            _bBBlog.Info("Setting EventSubHandleReadchat");
            _bBBlog.Debug($"SessionID: {_eventSubWebsocketClient.SessionId}");
            EventSubReadChatMessages = true;
            _eventSubWebsocketClient.ChannelChatMessage += (sender, e) => EventSubOnChannelChatMessage(e, command, follower, subscriber);
            //we can call this function multile times, so we need to check if we are already connected
            if (_eventSubWebsocketClient.SessionId != null && !TwitchMock)
            {
                //set the global, we need this for the EventSub
                _conditions ??= new()
                    {
                    { "broadcaster_user_id", TwitchChannelID },
                             { "user_id", TwitchUserID },
                    };
                _bBBlog.Info("Subscribing to chat messages");
                await EventSubSubscribe("channel.chat.message", _conditions);
            }
        }

        //this is for first month subscriptions
        public async void EventSubHandleSubscription()
        {
            _bBBlog.Info("Setting EventSubHandleSubscriptions for new subs and re-subs");
            _bBBlog.Debug($"SessionID: {_eventSubWebsocketClient.SessionId}");
            EventSubCheckSubscriptions = true;
            _eventSubWebsocketClient.ChannelSubscribe += EventSubOnChannelSubscriber;
            _eventSubWebsocketClient.ChannelSubscriptionMessage += EventSubOnChannelReSubscriber;
            if (_eventSubWebsocketClient.SessionId != null && !TwitchMock)
            {
                //set the global, we need this for the EventSub
                _conditions ??= new()
                    {
                    { "broadcaster_user_id", TwitchChannelID },
                             { "user_id", TwitchUserID },
                    };
                _bBBlog.Info("Subscribing to new subscription events");
                await EventSubSubscribe("channel.subscribe", _conditions);
                _bBBlog.Info("Subscribing to resubscription events");
                await EventSubSubscribe("channel.subscription.message", _conditions);
            }
            else
            {
                _bBBlog.Error("EventSubWebsocket is not connected, cannot subscribe to subscriptions");
            }
        }

        //this is for subscription gifts
        public async void EventSubHandleSubscriptionGift()
        {
            _bBBlog.Info("Setting EventSubHandleSubscriptionGift");
            _bBBlog.Debug($"SessionID: {_eventSubWebsocketClient.SessionId}");
            EventSubCheckSubscriptionGift = true;
            _eventSubWebsocketClient.ChannelSubscriptionGift += EventSubOnChannelSubscriptionGift;
            if (_eventSubWebsocketClient.SessionId != null && !TwitchMock)
            {
                //set the global, we need this for the EventSub
                _conditions ??= new()
                    {
                    { "broadcaster_user_id", TwitchChannelID },
                             { "user_id", TwitchUserID },
                    };
                _bBBlog.Info("Subscribing to subscription gifts");
                await EventSubSubscribe("channel.subscription.gift", _conditions);
            }
        }

        public async void EventSubHandleCheer(int minbits)
        {
            _bBBlog.Info("Setting EventSubHandleCheer");
            _bBBlog.Debug($"SessionID: {_eventSubWebsocketClient.SessionId}");
            EventSubCheckCheer = true;
            _eventSubWebsocketClient.ChannelCheer += (sender, e) => EventSubOnChannelCheer(e, minbits);
            if (_eventSubWebsocketClient.SessionId != null && !TwitchMock)
            {
                //set the global, we need this for the EventSub
                _conditions ??= new()
                    {
                    { "broadcaster_user_id", TwitchChannelID },
                             { "user_id", TwitchUserID },
                    };
                _bBBlog.Info("Subscribing to cheers");
                await EventSubSubscribe("channel.cheer", _conditions);
            }
        }

        public async void EventSubHandleChannelPointRedemption(string redemptionName)
        {
            _bBBlog.Info("Setting EventSubHandleChannelPointCustomRedemption");
            _bBBlog.Debug($"SessionID: {_eventSubWebsocketClient.SessionId}");
            EventSubCheckChannelPointRedemption = true;
            _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += (sender, e) => EventSubOnChannelPointRedemption(e, redemptionName);
            if (_eventSubWebsocketClient.SessionId != null && !TwitchMock)
            {
                //set the global, we need this for the EventSub
                _conditions ??= new()
                    {
                    { "broadcaster_user_id", TwitchChannelID },
                             { "user_id", TwitchUserID },
                    };
                _bBBlog.Info("Subscribing to channel point redemptions");
                await EventSubSubscribe("channel.channel_points_custom_reward_redemption.add", _conditions);
            }
            
        }

        //if test is validated and Twitch is enabled, we need to check if the access token is still valid every hour
        //this is a Twitch rule, see https://dev.twitch.tv/docs/authentication/validate-tokens/
        public async Task<bool> CheckHourlyAccessToken()
        {
            while (TwitchDoAutomatedCheck)
            {
                //45 minutes = 2700000
                await Task.Delay(2700000);
                //5 mins = 300000
                //await Task.Delay(300000);
                if (!await ValidateAccessToken(TwitchAccessToken))
                {
                    //the token itself is sensitive, never write it to the log
                    _bBBlog.Error("Hourly check! The access token is invalid, please re-authenticate");
                    return false;
                }
                else
                    _bBBlog.Info("Hourly check! The access token is validated and valid");
            }
            return true;
;
        }

        public void StopHourlyAccessTokenCheck()
        {
            _bBBlog.Info("Stopping hourly Twitch access token validation");
            TwitchDoAutomatedCheck = false;
        }

        [SupportedOSPlatform("windows10.0.10240")]
        public async Task<bool> GetTwitchAuthToken(List<string> scopes)
        {
            await ReqTwitchAuthToken(scopes);
            return TwitchAuthRequestResult;
        }

        //read the config file into globals
        private void TwitchReadSettings()
        {
               
            if (UserSettingsManager.Settings.TwitchAuthServerConfig.Length < 1)
            {
                _bBBlog.Error("TwitchAuthRedirect not found in Settings this should nto happen so lets use default");
                TwitchAuthRedirect = "http://localhost:8080";
                UserSettingsManager.Settings.TwitchAuthServerConfig = TwitchAuthRedirect;
                UserSettingsManager.SaveSettings();
            }
            else
            {
                TwitchAuthRedirect = UserSettingsManager.Settings.TwitchAuthServerConfig;
            }

            //read settings.json from %APPDATA%\BanterBrain (matching SetupConfigFiles) rather than the install
            //directory, so fresh installs that only have the appdata copy don't throw FileNotFoundException.
            string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\settings.json";
            if (!File.Exists(settingsPath))
            {
                _bBBlog.Warn("settings.json not found in %APPDATA%\\BanterBrain. Creating a default one.");
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                File.WriteAllText(settingsPath, "{\n  \"TwitchAuthClientId\": \"\"\n}\n");
            }

            using StreamReader r = new(settingsPath);
            //lets read the file and parse the json safely ;)
            var JsonData = JsonConvert.DeserializeObject<Dictionary<string, string>>(r.ReadToEnd());
            if (JsonData == null)
            {
                _bBBlog.Error("settings.json was empty or could not be parsed.");
                return;
            }

            bool test = JsonData.TryGetValue("TwitchAuthClientId", out string tmpVal);
            if (!test)
            {
                _bBBlog.Error("TwitchAuthClientId not found in settings.json");
            }
            else
            {
                TwitchAuthClientId = tmpVal;
            }
        }

        /// <summary>
        /// This validates the access token using the Twitch API
        /// </summary>
        /// <param name="TwAuthToken">The access token</param>
        /// <returns>true if token valid, false if invalid</returns>
        public async Task<bool> ValidateAccessToken(string TwAuthToken)
        {
            try
            {
                _gTwitchAPI.Settings.ClientId = TwitchAuthClientId;
                _gTwitchAPI.Settings.AccessToken = TwAuthToken;
                var validateTokenTest = await _gTwitchAPI.Auth.ValidateAccessTokenAsync(TwAuthToken);
                if (validateTokenTest == null)
                {
                    _bBBlog.Error("Access token is invalid, please re-authenticate");
                    return false;
                }
                else
                {
                    _bBBlog.Info("Access Token is validated and valid");
                    TwitchAccessToken = TwAuthToken;
                    return true;
                }
            }
            catch (Exception ex)
            {
                //a network hiccup is not the same as an invalid token but must not crash the caller
                _bBBlog.Error("Validating the access token failed: " + ex.Message);
                return false;
            }
        }

        //auth token, username
        /// <summary>
        /// We do a validate token and then check if we can get the user to verify not just the token but also the user
        /// </summary>
        /// <param name="TwAuthToken">The Twitch Access token</param>
        /// <param name="TwUsername">The Twitch user name</param>
        /// <param name="TwChannelName">The channel name entered on the GUI</param>
        /// <returns>true if both tests pass, false if not</returns>
        public async Task<bool> CheckAuthCodeAPI(string TwAuthToken, string TwUsername, string TwChannelName)
        {
            _bBBlog.Info($"Checking Authorization code using the API for " + TwUsername);

            _gTwitchAPI.Settings.ClientId = TwitchAuthClientId;
            _gTwitchAPI.Settings.AccessToken = TwAuthToken;
            _bBBlog.Debug($"clientid: {_gTwitchAPI.Settings.ClientId} ");
            //sensitive information 
            //"accesstoken: {_gTwitchAPI.Settings.AccessToken}");

            //before we do anything else, we validate the access token
            //if null, then the token is no longer valid and we need to let the user know
            if (!await ValidateAccessToken(TwAuthToken))
            {
                _bBBlog.Error("Access token is invalid, please re-authenticate");
                return false;
            }

            //assuming token itself is valid, lets continue
            try
            {
                _bBBlog.Info("Trying to see if I can getting the current user using Helix call.");
                await Task.Delay(500);
                var broadCaster = (await _gTwitchAPI.Helix.Users.GetUsersAsync(null, [TwChannelName])).Users;
                var messageSender = (await _gTwitchAPI.Helix.Users.GetUsersAsync(null, [TwUsername])).Users;
                TwitchUserID = messageSender[0].Id;
                TwitchChannelID = broadCaster[0].Id;
                _bBBlog.Info("Broadcaster: " + broadCaster[0].Login + " id:" + broadCaster[0].Id);
                _bBBlog.Info("Message sender: " + messageSender[0].Login + " id:" + messageSender[0].Id);
                
                //this is the user that sends that, if one is defined
                if (TwUsername != TwChannelName)
                {
                    TwitchChatUserID = messageSender[0].Id;
                    TwitchChatAccessToken = TwAuthToken;
                } else
                {
                    TwitchChatUserID = broadCaster[0].Id;
                    TwitchChatAccessToken = TwAuthToken;
                }

                //set the global, we need this for the EventSub
                _conditions = new()
                {
                    { "broadcaster_user_id", TwitchChannelID },
                             { "user_id", TwitchUserID },
                };

                //do we need to send a message also?
                if (TwitchSendTestMessageOnJoin != null)
                {
                    _bBBlog.Info("Sending message to channel");
                    try
                    {
                        await _gTwitchAPI.Helix.Chat.SendChatMessage(broadCaster[0].Id.ToString(), messageSender[0].Id.ToString(), TwitchSendTestMessageOnJoin, null, TwAuthToken);
                    }
                    catch (TwitchLib.Api.Core.Exceptions.TooManyRequestsException exception)
                    {
                        _bBBlog.Error("Waiting 2 seconds. Rate limit exceeded: " + exception.Message);
                        await Task.Delay(2000);
                        await _gTwitchAPI.Helix.Chat.SendChatMessage(broadCaster[0].Id.ToString(), messageSender[0].Id.ToString(), TwitchSendTestMessageOnJoin, null, TwAuthToken);
                    }
                }
                _bBBlog.Info("Authorization succeeded can read user so acces token is valid");
                return true;
            }
            catch (TwitchLib.Api.Core.Exceptions.BadScopeException exception)
            {
                _bBBlog.Error("Bad scope Issue with access token: " + exception.Message);
                return false;
            }
            catch (HttpRequestException exception)
            {
                _bBBlog.Error("HTTPrequest Issue with access token: " + exception.Message);
                return false;
            }
        }

        //for splitting long messages into substring
        public static void ProcessParts(string message, int partLength, Action<string> processFunction)
        {
            int stringLength = message.Length;

            for (int i = 0; i < stringLength; i += partLength)
            {
                if (partLength + i > stringLength)
                    partLength = stringLength - i;

                string part = message.Substring(i, partLength);
                processFunction(part);
            }
        }

        //send split up message with delay to Twitch
        private async Task SendMessageWithDelay(string messageToSend, int delay)
        {
            var parts = SplitMessage(messageToSend, 254);
            foreach (var part in parts)
            {
                try
                {
                    _bBBlog.Info($"Sending message: {part} to channel {TwitchChannelID} from user {TwitchChatUserID}");
                    await _gTwitchAPI.Helix.Chat.SendChatMessage(TwitchChannelID, TwitchChatUserID, part, null, TwitchChatAccessToken);
                    await Task.Delay(delay);
                }
                catch (TwitchLib.Api.Core.Exceptions.TooManyRequestsException exception)
                {
                    _bBBlog.Error("Rate limit exceeded: " + exception.Message);
                    await Task.Delay(10000);
                    try
                    {
                        await _gTwitchAPI.Helix.Chat.SendChatMessage(TwitchChannelID, TwitchChatUserID, part, null, TwitchChatAccessToken);
                    }
                    catch (Exception retryException)
                    {
                        _bBBlog.Error($"Retry after rate limit failed: {retryException.Message}");
                    }
                    await Task.Delay(delay);
                }
                catch (Exception exception)
                {
                    //a failing part must not abort the whole post, and must not crash the calling handler
                    _bBBlog.Error($"Could not send message part: {exception.Message}");
                }
            }
        }

        //this just splits a message in its seperate parts and puts it in a stringlist
        private static List<string> SplitMessage(string message, int partLength)
        {
            var parts = new List<string>();
            for (int i = 0; i < message.Length; i += partLength)
            {
                parts.Add(message.Substring(i, Math.Min(partLength, message.Length - i)));
            }
            return parts;
        }

        public async Task<bool> SendMessage(string messageToSend)
        {
            _bBBlog.Info($"Trying to send message: {messageToSend} to channel");
            //we need to send the message to the channel, so we need the channel id and the user id
            //we also need the access token
            //TwitchLib throws a BadParameterException ("message must be set") on an empty body, which happens
            //when the LLM call failed and left the output text empty, so bail out before we hit the API.
            if (string.IsNullOrWhiteSpace(messageToSend))
            {
                _bBBlog.Error("Message is empty, nothing to send to channel");
                return false;
            }
            //Twitch does not accept leading/trailing whitespace in a chat message
            messageToSend = messageToSend.Trim();
            if (messageToSend.Length == 0)
            {
                _bBBlog.Error("Message only contained whitespace, nothing to send to channel");
                return false;
            }
            if (TwitchAccessToken == null)
            {
                _bBBlog.Error("No access token set, cannot send message");
                return false;
            }
            else if (TwitchChannelID == null)
            {
                _bBBlog.Error("No channel ID set, cannot send message");
                return false;
            }
            else if (TwitchUserID == null)
            {
                _bBBlog.Error("No user ID set, cannot send message");
                return false;
            }
            else if (TwitchChatUserID == null)
            {
                _bBBlog.Error("No chat user ID set, cannot send message");
                return false;
            }
            else if (TwitchChatAccessToken == null)
            {
                _bBBlog.Error("No chat access token set, cannot send message");
                return false;
            }
            _bBBlog.Info($"Sending to channel {TwitchChannelID} from user {TwitchChatUserID}. Message: {messageToSend}");

            //if this message is longer than 255 characters we need to split it up
            if (messageToSend.Length > 254)
            {
                _bBBlog.Info("Message is longer than 254 characters, splitting it up");
                try
                {
                    await SendMessageWithDelay(messageToSend, 1000);
                    return true;
                }
                catch (Exception exception)
                {
                    //never let a chat post bring down the bot, this runs from an async void event handler
                    _bBBlog.Error($"Could not send split message to channel: {exception.Message}");
                    return false;
                }
            }
            else
            {
                //message is shorter than 255 characters, lets just send
                _bBBlog.Info("Message is shorter than 255 characters, sending it");
                try
                {
                    await _gTwitchAPI.Helix.Chat.SendChatMessage(TwitchChannelID, TwitchChatUserID, messageToSend, null, TwitchChatAccessToken);
                    return true;
                }
                catch (TwitchLib.Api.Core.Exceptions.TooManyRequestsException exception)
                {
                    _bBBlog.Error("Rate limit exceeded: " + exception.Message);
                    await Task.Delay(2000);
                    try
                    {
                        await _gTwitchAPI.Helix.Chat.SendChatMessage(TwitchChannelID, TwitchChatUserID, messageToSend, null, TwitchChatAccessToken);
                        return true;
                    }
                    catch (Exception retryException)
                    {
                        _bBBlog.Error($"Retry after rate limit failed: {retryException.Message}");
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    //Bad request/Bad parameter/Token problems all end up here, log and keep the bot running
                    _bBBlog.Error($"Could not send message to channel: {exception.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// This is the function that will request the auth token from Twitch
        /// </summary>
        /// <param name="scopes"></param>
        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240")]
        private async Task ReqTwitchAuthToken(List<string> scopes)
        {
            //Guard against a missing or still-templated Client ID. Launching the browser with an invalid
            //client_id throws a Win32Exception on the background thread below, which would crash the whole
            //program, so bail out gracefully with a clear message instead.
            if (string.IsNullOrWhiteSpace(TwitchAuthClientId) || TwitchAuthClientId.Contains("insert clientid"))
            {
                _bBBlog.Error("Twitch Client ID is not configured. Enter a valid Client ID in Settings before authorizing.");
                MessageBox.Show("The Twitch App Client ID is not set (or still the template placeholder).\r\n\r\nOpen Settings and enter a valid Client ID from https://dev.twitch.tv/console/apps, then try again.",
                    "Twitch Client ID required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TwitchAuthRequestResult = false;
                return;
            }

            // create twitch api instance
            _gTwitchAPI.Settings.ClientId = TwitchAuthClientId;

            // start local web server
            var server = new TwitchAuthWebserver(TwitchAuthRedirect);

            //implicit flow is rather simple compared to client cred
            //wrap Process.Start so a failure to launch the browser can't crash the process from this thread
            string oauthUrl = GetOAUTHCodeUrl(TwitchAuthClientId, TwitchAuthRedirect, scopes, "Implicit");
            var tImplicit = new Thread(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(oauthUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    _bBBlog.Error("Failed to launch browser for Twitch authorization: " + ex.Message);
                }
            });
            tImplicit.Start();

            var authImplicit = await server.ImplicitListen();

            if (authImplicit != null)
            {
                TwitchAuthRequestResult = true;
                // update TwitchLib's api with the recently acquired access token
                _gTwitchAPI.Settings.AccessToken = authImplicit.Code;

                //also save this the global to return
                TwitchAccessToken = authImplicit.Code;

                // get the auth'd user to test the access token's validity and to grab the account name
                var users = (await _gTwitchAPI.Helix.Users.GetUsersAsync()).Users;
                server.Stop();
                if (users != null && users.Length > 0)
                {
                    var user = users[0];
                    //also return this so the Settings UI can fill the "Account Name" box
                    TwitchAuthorizedUserName = user.DisplayName;

                    // print out all the data we've got
                    _bBBlog.Info($"Authorization success! User: {user.DisplayName} (id: {user.Id})");
                    MessageBox.Show($"Authorization success! User: {user.DisplayName}. Remember to log out if you want to authorize the other account also.");
                }
                else
                {
                    _bBBlog.Error("Authorization succeeded but no user was returned for the token.");
                    MessageBox.Show("Authorization succeeded and the token was captured, but the account name could not be retrieved.\r\n\r\nPlease enter the Account Name manually.", "Twitch Authorization", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                server.Stop();
                TwitchAuthRequestResult = false;
            }
        }

        private static string GetOAUTHCodeUrl(string clientId, string redirectUri, List<string> scopes, string OAUTHType)
        {
            string scopesStr = String.Join("+", scopes);
            string responseType;
            //The redirect URI is sent exactly as configured (no extra path appended) so it matches the
            //OAuth Redirect URL users register in the Twitch console verbatim.

            if (string.Equals(OAUTHType, "auth", StringComparison.OrdinalIgnoreCase))
                responseType = "code";
            else if (string.Equals(OAUTHType, "implicit", StringComparison.OrdinalIgnoreCase))
                responseType = "token";
            else
            {
                _bBBlog.Error("Unknown OAUTHType: " + OAUTHType);
                return null;
            }

            string returnStr = "https://id.twitch.tv/oauth2/authorize?" +
                    $"client_id={clientId}&" +
                    $"redirect_uri={redirectUri}&" +
                    $"response_type={responseType}&" +
                    $"scope={scopesStr}";
            _bBBlog.Info("OAUTH URL: " + returnStr);
            return returnStr;
        }

        public async Task<bool> EventSubStartAsync()
        {
            //a fresh start means automatic reconnecting is wanted again
            _eventSubIntentionalStop = false;
            var result = await _eventSubWebsocketClient.ConnectAsync();
            if (result)
            {
                _bBBlog.Info($"Websocket {_eventSubWebsocketClient.SessionId} connected!");
                return true;
            }
            else
            {
                _bBBlog.Error($"Websocket {_eventSubWebsocketClient.SessionId} failed to connect!");
                return false;
            }
        }


        //mock for testing see https://dev.twitch.tv/docs/cli/websocket-event-command/
        public async Task<bool> EventSubStartAsyncMock()
        {
            //a fresh start means automatic reconnecting is wanted again
            _eventSubIntentionalStop = false;
            //we just faking it ;)
            TwitchMock = true;
            var result = await _eventSubWebsocketClient.ConnectAsync(new Uri("ws://127.0.0.1:8080/ws"));
            if (result)
            {
                _bBBlog.Info($"Websocket {_eventSubWebsocketClient.SessionId} connected to MOCK EventSub!");
                return true;
            }
            else
            {
                _bBBlog.Error($"Websocket {_eventSubWebsocketClient.SessionId} failed to connect MOCK EventSub!!");
                return false;
            }
        }

        public async Task EventSubStopAsync()
        {
            //tell the disconnect handler that this stop is on purpose, no reconnect wanted
            _eventSubIntentionalStop = true;
            await _eventSubWebsocketClient.DisconnectAsync();
            _bBBlog.Info($"Websocket {_eventSubWebsocketClient.SessionId} manually stopped and disconnected.");
        }

        private async Task EventSubOnErrorOccurred(object sender, ErrorOccuredArgs e)
        {

            _bBBlog.Error($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
            await Task.Delay(1);
        }

        private static async Task EventSubOnChannelFollow(object sender, ChannelFollowArgs e)
        {
            var eventData = e.Notification.Payload.Event;

            _bBBlog.Info($"{eventData.UserName} followed {eventData.BroadcasterUserName} at {eventData.FollowedAt}");
            await Task.Delay(1);
        }

        //check the message string of twitch messages for a filtered word
        //if the message contains a filtered word, return true
        //else return false
        private bool CheckMessageForFilteredWords(string message)
        {
            //if the filter is not enabled, return false cos we dont need to check anything.
            if (UserSettingsManager.Settings.BadWordFilter == false)
            {
                return false;
            }

            //cheers and channel point redemptions can arrive without any text
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            //load the bad word filter into a list for checking
            //bad words are stored in a file in the appdata folder and are comma delimited
            var tmpFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BanterBrain\\WordFilter.txt";
            var badWords = new List<string>();
            try
            {
                if (File.Exists(tmpFile))
                {
                    using StreamReader r = new(tmpFile);
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        badWords.AddRange(line.Split(','));
                    }
                }
            }
            catch (Exception ex)
            {
                _bBBlog.Error("Could not read the word filter file, not filtering: " + ex.Message);
                return false;
            }
            _bBBlog.Debug($"Bad words: {badWords.Count}");
            //now to see if the message contains any words or sentences found in the badWords list
            foreach (string word in badWords)
            {
                //an empty entry (stray comma in the file) matches EVERY message, skip those
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }
                if (message.Contains(word.Trim()))
                {
                    _bBBlog.Info($"Message contains a bad word: {word}, ignoring");
                    return true;
                }
            }

            //when in doubt dont filter!
            return false;
        }

            //eventhandler for reading chat messages.
            //This receives: string command, int delay, bool follower, bool subscriber
            private async Task EventSubOnChannelChatMessage(ChannelChatMessageArgs e, string command, bool follower, bool subscriber)
        {
            bool tmpFol = follower;
            _bBBlog.Debug($"Chat message websocket {_eventSubWebsocketClient.SessionId}");
            //if the command is still on timeout, ignore else set it to true
            if (IsCommandTriggered)
            {
                _bBBlog.Info("Command trigger is still on timeout, ignoring");
                return;
            }

            var eventData = e.Notification.Payload.Event;

            _bBBlog.Debug($"{eventData.ChatterUserName} said {eventData.Message.Text} in {eventData.BroadcasterUserName}'s chat.");
            if (CheckMessageForFilteredWords(eventData.Message.Text))
            {
                _bBBlog.Info($"{eventData.ChatterUserName} said a bad word, ignoring");
                return;
            }

            //TODO: check for the command and then trigger the bot if its the right command and delay has been passed since bot was triggered last
            //we need to also check if the user is a follower or subscriber if that is set
            //also we should check when last time bot was triggered
            if (eventData.Message.Text.StartsWith(command, StringComparison.OrdinalIgnoreCase))
            {
                _bBBlog.Info($"{eventData.ChatterUserName} triggered the bot with command {command}");
                IsCommandTriggered = true;
                //if this does not work we need to, when starting, request all follows and store them in a list and kee it updated
                //var result = await _gTwitchAPI.Helix.Channels.GetChannelFollowersAsync(TwitchChannelID, eventData.ChatterUserId);

               // _bBBlog.Info($"Followerscheck length: {result.Data.Length} channel: {TwitchChannelID} chatter: {eventData.ChatterUserId}");

                //user needs to be a subscriber but is not
                _bBBlog.Debug($"{eventData.ChatterUserName} issubscriber: {eventData.IsSubscriber}");
                if (subscriber)
                {
                    if (!eventData.IsSubscriber && !eventData.IsBroadcaster)
                    {
                        _bBBlog.Info($"{eventData.ChatterUserName} is not a subscriber, but the command requires it. Not going to execute.");
                        IsCommandTriggered = false;
                        return;
                    }
                    else if (eventData.IsSubscriber)
                    {
                        _bBBlog.Info($"{eventData.ChatterUserName} is a subscriber and the command requires it, executing!");
                    }
                    else if (eventData.IsBroadcaster)
                    {
                        _bBBlog.Info($"{eventData.ChatterUserName} is not a subscriber but is a broadcaster so its allowed");
                    }
                } else
                {
                    _bBBlog.Info($"{eventData.ChatterUserName} is not a subscriber, but the command does not require it. Executing!");
                }

                if (OnESubChatMessage == null)
                {
                    _bBBlog.Debug("Chat trigger is null, no eventhandler set. Cannot continue");
                    return;
                }

                //Define the eventhandlers for being able to be thrown back to the main form
                OnESubChatMessage(this, new TwitchEventhandlers.OnChatEventArgs(eventData.ChatterUserName, eventData.Message.Text));


                //We need to set a delay before the bot can be triggered again, waiting delay seconds 
                //not sure if this works ;)
                _bBBlog.Debug($"Disabling eventhandler for chatmessage");
                
            }
        }

        public async Task EventSubEnableChatMessageHandlerAfterDelay(int delay)
        {
            await Task.Delay(delay * 1000);
            IsCommandTriggered = false;
            _bBBlog.Debug($"Enabling eventhandler for chatmessage");
            if (UserSettingsManager.Settings.DelayFinishToChatcCheckBox)
            {
                await SendMessageWithDelay(UserSettingsManager.Settings.TwitchDelayMessageTextBox, 1000);
            }
        }

        //eventhandler for checking cheers and amoun tof min bits
        private async Task EventSubOnChannelCheer(ChannelCheerArgs e, int minbits)
        {
            _bBBlog.Debug($"Cheer websocket {_eventSubWebsocketClient.SessionId}");
            var eventData = e.Notification.Payload.Event;

            _bBBlog.Info($"{eventData.UserName} cheered {eventData.Bits} bits in {eventData.BroadcasterUserName}'s chat with message: {eventData.Message}");

            //check for bad words
            if (CheckMessageForFilteredWords(eventData.Message))
            {
                _bBBlog.Info($"{eventData.UserName} said a bad word, ignoring");
                return;
            }

            if (eventData.Bits >= minbits)
            {
                _bBBlog.Info($"{eventData.UserName} cheered {eventData.Bits} bits in {eventData.BroadcasterUserName}'s chat, trigger bot with message {eventData.Message}");
                if (OnESubCheerMessage == null)
                {
                    _bBBlog.Debug("Cheer trigger is null, no eventhandler set. Cannot continue");
                    return;
                }
                OnESubCheerMessage(this, new TwitchEventhandlers.OnCheerEventsArgs(eventData.UserName, eventData.Message));
            }
            else
            {
                _bBBlog.Info($"{eventData.UserName} cheered {eventData.Bits} bits in {eventData.BroadcasterUserName}'s chat, but it was not enough to trigger the bot");
            }
            await Task.Delay(1);
        }

        //eventhandler for RE-subscriptions
        //they contain a message(might be empty) and the duration in months
        private async Task EventSubOnChannelReSubscriber(object sender, ChannelSubscriptionMessageArgs e)
        {

            _bBBlog.Debug($"Subscriber websocket {_eventSubWebsocketClient.SessionId}");
            var eventData = e.Notification.Payload.Event;

            //eventdata can be null, so we need to check for that
            var message = "";
            if (eventData.Message != null)
                message = eventData.Message.Text;

            _bBBlog.Info($"{eventData.UserName} re-subscribed to {eventData.BroadcasterUserName} for {eventData.CumulativeMonths} months with message {eventData.Message}");

            //check for bad words   
            if (CheckMessageForFilteredWords(message))
            {
                _bBBlog.Info($"{eventData.UserName} said a bad word, ignoring");
                return;
            }

            if (OnESubReSubscribe == null)
            {
                _bBBlog.Debug("Re-Subscriber trigger is null, no eventhandler set. Cannot continue");
                return;
            }
            _bBBlog.Debug($"Re-Subscriber eventhandler triggered: {OnESubReSubscribe}");
            OnESubReSubscribe(this, new TwitchEventhandlers.OnReSubscribeEventArgs(eventData.UserName, message, eventData.CumulativeMonths.ToString()));
            await Task.Delay(1);
        }

        //eventhandler for NEW subscriptions
        private async Task EventSubOnChannelSubscriber(object sender, ChannelSubscribeArgs e)
        {

            _bBBlog.Debug($"Subscriber websocket {_eventSubWebsocketClient.SessionId}");
            var eventData = e.Notification.Payload.Event;
            _bBBlog.Info($"{eventData.UserName} subscribed to {eventData.BroadcasterUserName}");
            if (OnESubSubscribe == null)
            {
                _bBBlog.Debug("Subscriber trigger is null, no eventhandler set. Cannot continue");
                return;
            }
            _bBBlog.Debug($"Subscriber eventhandler triggered: {OnESubSubscribe}");
            OnESubSubscribe(this, new TwitchEventhandlers.OnSubscribeEventArgs(eventData.UserName, eventData.BroadcasterUserName));
            await Task.Delay(1);
        }

        //eventhandler for subscription gifts
        private async Task EventSubOnChannelSubscriptionGift(object sender, ChannelSubscriptionGiftArgs e)
        {

            _bBBlog.Debug($"Subscriber websocket {_eventSubWebsocketClient.SessionId}");
            var eventData = e.Notification.Payload.Event;
            var tierSub = int.TryParse(eventData.Tier, out var parsedTier) ? parsedTier / 1000 : 1;
            _bBBlog.Info($"{eventData.UserName} gifted {eventData.Total} tier {tierSub} subscription(s) to a total of {eventData.CumulativeTotal}");
            if (OnESubSubscriptionGift == null)
            {
                _bBBlog.Debug("Gift trigger is null, no eventhandler set. Cannot continue");
                return;
            }
            _bBBlog.Debug($"Subscriber gift triggered: {OnESubSubscriptionGift}");
            OnESubSubscriptionGift(this, new TwitchEventhandlers.OnSubscriptionGiftEventArgs(eventData.UserName, eventData.Total.ToString(), tierSub.ToString()));
            await Task.Delay(1);
        }

        //eventhandler for custom channel point redemptions
        private async Task EventSubOnChannelPointRedemption(ChannelPointsCustomRewardRedemptionArgs e, string redemptionName)
        {

            _bBBlog.Debug($"Subscriber websocket {_eventSubWebsocketClient.SessionId}");
            var eventData = e.Notification.Payload.Event;

            _bBBlog.Info($"{eventData.UserName} used {redemptionName} redeemed {eventData.Reward.Title} with message {eventData.UserInput}");

            //check for bad words
            if (CheckMessageForFilteredWords(eventData.UserInput))
            {
                _bBBlog.Info($"{eventData.UserName} said a bad word, ignoring");
                return;
            }

            if (OnESubChannelPointRedemption == null)
            {
                _bBBlog.Debug("Redemption trigger is null, no eventhandler set. Cannot continue");
                return;
            }
            else if (redemptionName.Equals(eventData.Reward.Title, StringComparison.CurrentCultureIgnoreCase))
            {
                _bBBlog.Debug($"Channel point redemption triggered: {OnESubChannelPointRedemption}");
                OnESubChannelPointRedemption(this, new TwitchEventhandlers.OnChannelPointCustomRedemptionEventArgs(eventData.UserName, eventData.UserInput));
            }
            else
            {
                _bBBlog.Debug($"Redemption name {redemptionName} does not match the redemption name {eventData.Reward.Title} so ignoring it.");
            }
            await Task.Delay(1);
        }

        private async Task EventSubOnWebsocketConnected(object sender, WebsocketConnectedArgs e)
        {
            _bBBlog.Info($"Websocket {_eventSubWebsocketClient.SessionId} connected!");
            //if mock dont subscribe
            if (TwitchMock)
            {
                _bBBlog.Info("Mock is set, not subscribing to topics");
                return;
            }

            if (!e.IsRequestedReconnect)
            {
                // subscribe to topics using API Helix.EventSub.CreateEventSubSubscriptionAsync
                //sooo we need the ID of the channel to read (username of the viewing channel)
                // the ID of the bot (username of the bot)
                // the client ID (_twitchAuthClientId)
                // the access token
                _bBBlog.Info($"Subscribing to topics. - ClientID: {TwitchAuthClientId} -ChannelID: {TwitchChannelID} -UserID {TwitchUserID} "); // -Accesstoken {TwitchAccessToken}");

                _conditions = new()
                {
                    { "broadcaster_user_id", TwitchChannelID },
                             { "user_id", TwitchUserID },
                };

                //we have to wait to subscribe to the topics until the websocket is connected
                //but after that we dont really have to await
                if (EventSubReadChatMessages)
                {

                    _bBBlog.Info($"Subscribing to chat messages. WebsocketSessionId: {_eventSubWebsocketClient.SessionId}");
                    await EventSubSubscribe("channel.chat.message", _conditions);
                }

                if (EventSubCheckCheer)
                {
                    _bBBlog.Info($"Subscribing to cheers. WebsocketSessionId: {_eventSubWebsocketClient.SessionId}");
                    await EventSubSubscribe("channel.cheer", _conditions);
                }

                if (EventSubCheckSubscriptions)
                {
                    _bBBlog.Info($"Subscribing to subscriptions. WebsocketSessionId: {_eventSubWebsocketClient.SessionId}");
                    await EventSubSubscribe("channel.subscribe", _conditions);
                    _bBBlog.Info($"Subscribing to resubscriptions. WebsocketSessionId: {_eventSubWebsocketClient.SessionId}");
                    await EventSubSubscribe("channel.subscription.message", _conditions);
                }

                if (EventSubCheckSubscriptionGift)
                {
                    _bBBlog.Info($"Subscribing to subscription gifts. WebsocketSessionId: {_eventSubWebsocketClient.SessionId}");
                    await EventSubSubscribe("channel.subscription.gift", _conditions);
                }

                if (EventSubCheckChannelPointRedemption)
                {
                    _bBBlog.Info($"Subscribing to channel point redemptions. WebsocketSessionId: {_eventSubWebsocketClient.SessionId}");
                    await EventSubSubscribe("channel.channel_points_custom_reward_redemption.add", _conditions);
                }
            }
        }

        private async Task EventSubSubscribe(string type, Dictionary<string, string> conditions)
        {
            _bBBlog.Info($"Subscribing to {type}");
            try
            {
                var response = await _gTwitchAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(type, "1", conditions,
                               TwitchLib.Api.Core.Enums.EventSubTransportMethod.Websocket, _eventSubWebsocketClient.SessionId, clientId: TwitchAuthClientId, accessToken: TwitchAccessToken);
                foreach (var sub in response.Subscriptions)
                {
                    _bBBlog.Info($"Sub Type: {sub.Type} is {sub.Status} with {sub.Id}");
                    //indexer instead of Add, re-subscribing to the same type would throw on a duplicate key
                    _eventSubIllist[type] = sub.Id;
                };
                foreach (var item in _eventSubIllist)
                {
                    _bBBlog.Debug($"Active subscriptions: {item.Key} with id {item.Value}");
                }
            }
            catch (TwitchLib.Api.Core.Exceptions.BadRequestException exception)
            {
                _bBBlog.Error($"Bad request exception: {exception.Message}");
            }
            catch (TwitchLib.Api.Core.Exceptions.BadParameterException exception)
            {
                _bBBlog.Error($"Bad parameter exception: {exception.Message}");
            } 
            catch (TwitchLib.Api.Core.Exceptions.BadResourceException exception)
                        {
                _bBBlog.Error($"Bad resource exception: {exception.Message}");
            }
            catch (TwitchLib.Api.Core.Exceptions.BadScopeException exception)
            {
                _bBBlog.Error($"Bad scope exception: {exception.Message}");
            }
            catch (TwitchLib.Api.Core.Exceptions.TooManyRequestsException exception)
            {
                _bBBlog.Error($"Rate limit exceeded: {exception.Message}");
                await Task.Delay(3000);
                await EventSubSubscribe(type, conditions);
            }
            catch (TwitchLib.Api.Core.Exceptions.BadTokenException exception)
            {
                _bBBlog.Error($"Bad token exception: {exception.Message}");
            }
            catch (TwitchLib.Api.Core.Exceptions.TokenExpiredException exception)
            {
                _bBBlog.Error($"token expired exception: {exception.Message}");
            }
            catch (TwitchLib.Api.Core.Exceptions.UnexpectedResponseException exception)
            {
                _bBBlog.Error($"unexecpted response exception: {exception.Message}");
            }
            catch (TwitchLib.Api.Core.Exceptions.HttpResponseException exception)
            {
                _bBBlog.Error($"http response exception: {exception.Message}");
            }
            catch (TwitchLib.Api.Core.Exceptions.InvalidCredentialException exception)
            {
                _bBBlog.Error($"invalid credential response exception: {exception.Message}");
            }
        }

        //ubsubscribe from an EventSub topic
        private async Task EventSubUnsubscribe(string type)
        {
            if (_eventSubIllist.TryGetValue(type, out string subId))
            {
                var result = await _gTwitchAPI.Helix.EventSub.DeleteEventSubSubscriptionAsync(subId, TwitchAuthClientId, TwitchAccessToken);
                if (result)
                {
                    _bBBlog.Info($"Unsubscribed from {type} with id {subId}");
                    //remove the subscription from the list
                    _eventSubIllist.Remove(type);
                }
                else
                    _bBBlog.Error($"Could not unsubscribe from {type} with id {subId}");
            }
            else
            {
                _bBBlog.Error($"Could not find subscription for {type}");
                foreach (var item in _eventSubIllist)
                {
                    _bBBlog.Info($"Subscriptions: {item.Key} with id {item.Value}");
                }
            }
        }

        private async Task EventSubOnWebsocketDisconnected(object sender, EventArgs e)
        {
            _bBBlog.Error($"Websocket {_eventSubWebsocketClient.SessionId} disconnected.");

            //when the user stopped EventSub on purpose we must not reconnect in a loop, the old
            //"while (!ReconnectAsync)" never ended and burned CPU even after an intentional stop
            while (!_eventSubIntentionalStop && !await _eventSubWebsocketClient.ReconnectAsync())
            {
                await Task.Delay(1000);
            }
        }

        private async Task EventSubOnWebsocketReconnected(object sender, EventArgs e)
        {
            _bBBlog.Warn($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
            await Task.Delay(1);
        }
    }

}

