using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Ollie.Dialogs.WatchLists;
using Ollie.Models;

namespace Ollie.Dialogs
{
    public class WatchlistDialog : ComponentDialog
    {
        // Define a "done" response for the company selection prompt.
        private const string DoneOption = "stop";

        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";

        private List<WatchList> watchlists;

        private List<WatchListItem> watchlistItems;

        private WatchlistWatchListitems watchList;

        public WatchlistDialog() : base(nameof(WatchlistDialog))
        {
            // Create Prompts
            watchList = new WatchlistWatchListitems();

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
            AddDialog(new AddWatchListItemDialog());
            AddDialog(new RemoveWatchListItemDialog());

            //AddDialog(new ReviewSelectionDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ListAvailableWatchlistsStep,
                AddDeleteWatchlistStep,
                ListWatchlistItemsStep
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ListAvailableWatchlistsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve Watchlists:

            watchlists = await RetrieveWatchLists();

            if (watchlists == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Didn't find any watchlists!"), cancellationToken);

                return await stepContext.EndDialogAsync();
            }

            var cardAttachment = CreateAdaptiveCardAttachment(Path.Combine(".", "Resources", "SelectWatchList.json"), watchlists);

            // Create the text prompt
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Attachments = new List<Attachment>() { cardAttachment },
                    Type = ActivityTypes.Message
                }
            };

            // Prompt the user for a choice.
            return await stepContext.PromptAsync(nameof(TextPrompt), opts, cancellationToken);
        }

        private static Attachment CreateAdaptiveCardAttachment(string filePath, List<WatchList> watchlists)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var template = new AdaptiveCardTemplate(adaptiveCardJson);

            var customJson = "";

            foreach (var watchlist in watchlists)
            {
                customJson += "{\"title\": \"" + watchlist.Name + "\",\"value\": \"" + watchlist.Properties.WatchlistId + "\"},";
            }

            var cardJson = File.ReadAllText(filePath);
            cardJson = cardJson.Replace("<PLACEHOLDER>", customJson);

            //List<string> options = new List<string>();
            //foreach (var item in indicator.WatchListItems)
            //{
            //    options.Add(item.Properties.ItemsKeyValueString);
            //}

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            return adaptiveCardAttachment;
        }

        private async Task<DialogTurnResult> AddDeleteWatchlistStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var value = ((string)stepContext.Result);

            var watchlistSelected = JsonConvert.DeserializeObject<ResponseValue>((string)stepContext.Result);

            //var watchlist = (WatchList)stepContext.Options;

            var watchlist = watchlists.Find(w => w.Properties.WatchlistId.Equals(watchlistSelected.CompactSelectVal));

            watchList.WatchList = watchlist;

            string message = $"Would you like to add or remove an item from the watchlist, or {DoneOption} to Cancel.";

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(message),
                RetryPrompt = MessageFactory.Text("Please choose to either add a new item to a watchlist or remove an existing one."),
                Choices = ChoiceFactory.ToChoices(new string[] { "Add", "Remove", DoneOption }),
            };

            return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ListWatchlistItemsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var value = ((FoundChoice)stepContext.Result).Value;
            var done = value == DoneOption;
            if (done)
            {
                return await stepContext.EndDialogAsync();
            }

            watchlistItems = await RetrieveWatchListItems();

            watchList.WatchListItems = watchlistItems;

            // gather WatchListItems
            if (value == "Remove")
            {
                return await stepContext.BeginDialogAsync(nameof(RemoveWatchListItemDialog), watchList, cancellationToken);
                
            }
            else 
            {
                return await stepContext.BeginDialogAsync(nameof(AddWatchListItemDialog), watchList, cancellationToken);
            }
        }

        private async Task<List<WatchList>> RetrieveWatchLists()
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
                var test =  await apiCaller.GETCallSentinelWebApiAndProcessResultASync($"{config.SentinelApiUrl}subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourcegroupName}/providers/Microsoft.OperationalInsights/workspaces/{config.WorkspaceName}/providers/Microsoft.SecurityInsights/watchlists?api-version={config.SentinelApiVersion}", result.AccessToken);

                var testObject = test.First.First;

                var watchlists  = testObject.ToObject<List<WatchList>>();

                // PROCESS RESULT


                return watchlists;

            }

            return null;
        }

        private async Task<List<WatchListItem>> RetrieveWatchListItems()
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
                var test = await apiCaller.GETCallSentinelWebApiAndProcessResultASync($"{config.SentinelApiUrl}subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourcegroupName}/providers/Microsoft.OperationalInsights/workspaces/{config.WorkspaceName}/providers/Microsoft.SecurityInsights/watchlists/TeamsBotWatchList/watchlistItems?api-version={config.SentinelApiVersion}", result.AccessToken);

                var testObject = test.First.First;

                var watchlists = testObject.ToObject<List<WatchListItem>>();

                return watchlists;

            }

            return null;
        }
    }
}
