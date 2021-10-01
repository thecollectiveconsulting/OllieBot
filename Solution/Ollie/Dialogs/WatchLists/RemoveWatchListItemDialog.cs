using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Ollie.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Choices;
using AdaptiveCards.Templating;
using Newtonsoft.Json.Linq;

namespace Ollie.Dialogs.WatchLists
{
    public class RemoveWatchListItemDialog : CancelAndHelpDialog
    {
        // Define a "done" response for the company selection prompt.
        private const string DoneOption = "stop";

        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";

        private List<WatchList> watchlists;

        private WatchlistWatchListitems watchList;

        private List<WatchListItem> watchlistItems;

        public RemoveWatchListItemDialog() : base(nameof(RemoveWatchListItemDialog))
        {
            // Create Prompts

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));

            //AddDialog(new ReviewSelectionDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStep,
                RemoveWatchListStep
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            watchList = ((WatchlistWatchListitems)stepContext.Options);

            var cardAttachment = CreateAdaptiveCardAttachment(Path.Combine(".", "Resources", "WatchListDelete.json"), watchList);

            // Create the text prompt
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message
                }
            };


            return await stepContext.PromptAsync(nameof(TextPrompt), opts, cancellationToken);
        }

        private async Task<DialogTurnResult> RemoveWatchListStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var value = (string)stepContext.Result;

            var itemToDelete = JsonConvert.DeserializeObject<ResponseValue>((string)stepContext.Result);

            var item = watchList.WatchListItems.Find(w => w.Properties.WatchlistItemId.Equals(itemToDelete.CompactSelectVal));

            var IdToDelete = item.Properties.WatchlistItemId;

            // Delete Watchlist item
            var deleted = await DeleteWatchListItem(item);

            if (deleted)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Successfully deleted item from the watchlist!"), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Failed to delete item from the watchlist!"), cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<bool> DeleteWatchListItem(WatchListItem item)
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            IConfidentialClientApplication app;

            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            string[] scopes = new string[] { $"{config.SentinelApiUrl}.default" };

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                Console.WriteLine("Token acquired");
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                Console.WriteLine("Scope provided is not supported");
            }

            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                var test = await apiCaller.DeleteCallSentinelWebApiAndProcessResultASync($"{config.SentinelApiUrl}subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourcegroupName}/providers/Microsoft.OperationalInsights/workspaces/{config.WorkspaceName}/providers/Microsoft.SecurityInsights/watchlists/{watchList.WatchList.Properties.WatchlistAlias}/watchlistItems/{item.Properties.WatchlistItemId}?api-version={config.SentinelApiVersion}", result.AccessToken);

                // PROCESS RESULT


                return test;

            }

            return false;
        }

        private static Attachment CreateAdaptiveCardAttachment(string filePath, WatchlistWatchListitems indicator)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var template = new AdaptiveCardTemplate(adaptiveCardJson);

            var customJson = "";

            // Only need the fields, so just take the first item from the list
            foreach (var key in indicator.WatchListItems)
            {
                customJson += "{\"title\": \"" + key.Properties.ItemsKeyValueString + "\",\"value\": \""+ key.Properties.WatchlistItemId + "\"},";
            }
            var cardJson = File.ReadAllText(filePath);
            cardJson = cardJson.Replace("<PLACEHOLDER>", customJson);
            

            List<string> options = new List<string>();
            foreach (var item in indicator.WatchListItems)
            {
                options.Add(item.Properties.ItemsKeyValueString);
            }

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            return adaptiveCardAttachment;
        }
    }
}
