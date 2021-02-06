using Line.Messaging;
using Line.Messaging.Webhooks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BirthdayBot
{
    public class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }

        public LineBotApp(LineMessagingClient lineMessagingClient)
        {
            this.messagingClient = lineMessagingClient;
        }

        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message.Type)
            {
                case EventMessageType.Text : await HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId);
                    break;
            }
        }

        private async Task HandleTextAsync(string replyToken, string userMessage, string userId)
        {
            userMessage = userMessage.ToLower().Replace(" ", "");
            ISendMessage replyMessage = new TextMessage(userMessage);

            await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage });
        }
    }
}
