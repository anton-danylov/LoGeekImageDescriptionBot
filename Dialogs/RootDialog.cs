using System;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Web.Configuration;
using System.Net.Http.Headers;
using Microsoft.ProjectOxford.Vision;
using System.Linq;
using System.IO;

namespace LoGeekImageDescriptionBot
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private const string LineSeparator = "\n\n\u200C";
        private static readonly string ApiKey = WebConfigurationManager.AppSettings["MicrosoftVisionApiKey"];
        private static readonly string ApiEndpoint = WebConfigurationManager.AppSettings["MicrosoftVisionApiEndpoint"];
        private static readonly VisionServiceClient visionServiceClient = new VisionServiceClient(ApiKey, ApiEndpoint);

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            Activity activity = context.Activity as Activity;

            var imageAttachment = activity.Attachments?.FirstOrDefault(a => a.ContentType.Contains("image"));
            if (imageAttachment != null)
            {
                try
                {
                    await context.PostAsync($"Downloading image...");
                    byte[] imageContents = await DownloadImage(activity, new Uri(imageAttachment.ContentUrl));

                    await context.PostAsync($"Image received, processing started...");


                    var resultDescribe = await visionServiceClient.DescribeAsync(new MemoryStream(imageContents), 3);

                    string description = String.Join(LineSeparator,
                        resultDescribe?.Description?.Captions?.Select(c => $"{c.Text} ({c.Confidence:P2})"));

                    await context.PostAsync(String.IsNullOrEmpty(description)
                        ? "Sorry, but I've no idea what it is :("
                        : $"#### My best guesses are:  \n{description}");


                    var resultAnalyse = await visionServiceClient.AnalyzeImageAsync(new MemoryStream(imageContents), new VisualFeature[] { VisualFeature.Categories, VisualFeature.Faces });

                    if (resultAnalyse.Faces?.Any() ?? false)
                    {
                        string people = String.Join(LineSeparator,
                            resultAnalyse.Faces?.Select(face => $"{face.Gender}, {face.Age} at ({face.FaceRectangle.Left}, {face.FaceRectangle.Top})"));

                        await context.PostAsync($"#### people on photo:\n {people}");
                    }
                }

                catch (Exception ex)
                {
                    await context.PostAsync($"Error occured: {ex}");
                }
            }
            else
            {
                await context.PostAsync($"Please send me an image and I'll describe it :)");
            }


            context.Wait(MessageReceivedAsync);
        }

        private static async Task<byte[]> DownloadImage(Activity activity, Uri uri)
        {
            byte[] imageContents = null;
            using (var httpClient = new HttpClient())
            {
                await HandleSkypeSecurityToken(activity, httpClient);

                imageContents = await httpClient.GetByteArrayAsync(uri);
            }

            return imageContents;
        }

        private static async Task HandleSkypeSecurityToken(Activity activity, HttpClient httpClient)
        {
            if (activity.ChannelId == "skype")
            {
                httpClient.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(new ConnectorClient(new Uri(activity.ServiceUrl))));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            }
        }

        private static async Task<string> GetTokenAsync(ConnectorClient connector)
        {
            var credentials = connector.Credentials as MicrosoftAppCredentials;
            if (credentials != null)
            {
                return await credentials.GetTokenAsync();
            }

            return null;
        }
    }
}