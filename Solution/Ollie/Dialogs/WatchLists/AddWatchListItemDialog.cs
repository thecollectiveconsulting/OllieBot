using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ollie.Models;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Ollie.Dialogs.WatchLists
{
    public class AddWatchListItemDialog : CancelAndHelpDialog
    {
        // Define a "done" response for the company selection prompt.
        private const string DoneOption = "stop";

        private List<WatchList> watchlists;

        private WatchlistWatchListitems watchlist;

        public AddWatchListItemDialog() : base(nameof(AddWatchListItemDialog))
        {
            // Create Prompts

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));

            //AddDialog(new ReviewSelectionDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStep,
                ProcessFormInput
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var watchListItem = ((WatchlistWatchListitems)stepContext.Options);

            var customJson = "";

            // Only need the fields, so just take the first item from the list
            foreach (var key in watchListItem.WatchListItems.FirstOrDefault().Properties.fields())
            {
                customJson += "{\"type\": \"Input.Text\",\"placeholder\": \"Placeholder text\",\"isRequired\": true,\"id\": \"" + key + "\",\"label\": \"Please Provide "+ key+" Value\",\"errorMessage\": \"Please Provide Error\"},";
            }
            var cardPath = Path.Combine(".", "Resources", "WatchListForm.json"); ;
            var cardJson = File.ReadAllText(cardPath);
            cardJson = cardJson.Replace("<PLACEHOLDER>", customJson);

            var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson)
            };

            // Create the text prompt
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message
                }
            };

            // Display a Text Prompt and wait for input
            return await stepContext.PromptAsync(nameof(TextPrompt), opts, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessFormInput(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var watchListItem = ((WatchlistWatchListitems)stepContext.Options);

            //var test = JsonConvert.SerializeObject(activity.Value);
            var test = JsonConvert.DeserializeObject((string)stepContext.Result);

            var json = "{\"properties\": {\"itemsKeyValue\": "+ test.ToString() +"}}";

            var success = await AddWatchListItem(json, watchListItem);

            if (success)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Successfully added item to the watchlist!"), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Failed to add item to the watchlist!"), cancellationToken);
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<bool> AddWatchListItem(string json, WatchlistWatchListitems watchListItem)
        {
            var g = Guid.NewGuid();

            //"/subscriptions/f8ec5586-7fa7-4815-8a1e-cb33f83ed118/resourceGroups/secops/providers/Microsoft.OperationalInsights/workspaces/tctest-sentinel/providers/Microsoft.SecurityInsights/Watchlists/TestWatchlist/WatchlistItems/a0eb051a-244d-489c-b056-d53e46b13725",

            //PUT https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.OperationalInsights/workspaces/{workspaceName}/providers/Microsoft.SecurityInsights/incidents/{incidentId}?api-version=2021-04-01

            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");


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
                await apiCaller.PUTCallSentinelWebApiAndProcessResultASync($"{config.SentinelApiUrl}subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourcegroupName}/providers/Microsoft.OperationalInsights/workspaces/{config.WorkspaceName}/providers/Microsoft.SecurityInsights/watchlists/{watchListItem.WatchList.Properties.WatchlistAlias}/watchlistItems/{g}?api-version={config.SentinelApiVersion}", result.AccessToken, stringContent);

                // PROCESS RESULT

                return true;
            }

            return false;
        }
    }
}
