using System.Threading.Tasks;
using System.Web.Http;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System.Net.Http;
using System;
using System.Linq;

namespace LoGeekImageDescriptionBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // GDPR stuff should be here in case we store user data
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                string welcomeMessage = 
                    @"### Welcome to Image Description Bot!
Drop me an image and I'll do my best describe what's on it :)";

                Func<ChannelAccount, bool> isBot = acc => acc.Id == message.Recipient.Id;

                if (message.MembersAdded?.Any(isBot) ?? false) // I was added to conversation
                {
                    var reply = message.CreateReply(welcomeMessage);
                    var connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}