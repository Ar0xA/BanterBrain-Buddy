using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanterBrain_Buddy
{
    internal class TwitchLLMResponseLanguage
    {
        //all strings default to empty so a fallback new() instance can never NRE the Twitch
        //event handlers that call .Replace() directly on these template fields
        public string Language { get; set; } = "";
        public string CheerTalkToLLM{ get; set; } = "";
        public string CheerDefaultNoMessage { get; set; } = "";
        public string CheerWithMessage { get; set; } = "";

        public string ChannelPointTalkToLLM { get; set; } = "";
        public string ChannelPointDefaultNoMessage { get; set; } = "";
        public string ChannelPointWithMessage { get; set; } = "";

        public string GiftedSingleSub { get; set; } = "";
        public string GiftedMultipleSubs { get; set; } = "";

        public string SubscribeFirstTimeThanks { get; set; } = "";
        public string SubscribeResubThanksWithMessageLLM { get; set; } = "";

        public string SubscribeMonthsNoMessage { get; set; } = "";
        public string SubscribeMonthsMessage { get; set; } = "";

        public string ChatMessageResponseLLM { get; set; } = "";
        public string ChatMessageUserSaid { get; set; } = "";
    }
}
