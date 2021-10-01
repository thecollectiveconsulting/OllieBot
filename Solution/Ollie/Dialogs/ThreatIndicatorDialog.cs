using AdaptiveCards.Templating;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ollie.Models;
using System.Text;
using Ollie.Resources;
using Microsoft.Bot.Connector.Authentication;

namespace Ollie.Dialogs
{
    public class ThreatIndicatorDialog : CancelAndHelpDialog
    {
        // Define a "done" response for the company selection prompt.
        private const string DoneOption = "stop";

        // Define the TI choices for the TI selection prompt.
        private readonly string[] _TIOptions = new string[]
        {
            "IP",
            "Domain",
            "Url",
            "FileHash"
        };

        private List<string> queries;
        private string fieldToSearch;
        private int count;
        private string fieldToSearchValue;

        public ThreatIndicator ThreatIndicator { get; set; }

        private readonly string[] _cards =
        {
            Path.Combine(".", "Resources", "TISummaryCard.json")
        };

        public ThreatIndicatorDialog() : base(nameof(ThreatIndicatorDialog))
        {

            // Create Prompts            
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ThreatIndicatorTypeStepAsync,
                ThreatIndicatorFormStepAsync,
                ProcessFormInput,
                SubmitStepAsync,
                SearchIncidentsStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        #region WaterFallSteps

        private async Task<DialogTurnResult> ThreatIndicatorTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //// Create a prompt message.
            //string message = $"Please select the type of indicator you want to create or click {DoneOption} to cancel.";
            ////string message = $"Please choose the TI type to create, or `` to Cancel.";

            //// Create the list of options to choose from.
            //var options = _TIOptions.ToList();
            //options.Add(DoneOption);

            //var promptOptions = new PromptOptions
            //{
            //    Prompt = MessageFactory.Text(message),
            //    RetryPrompt = MessageFactory.Text("Please choose an option from the list."),
            //    Choices = ChoiceFactory.ToChoices(options),
            //};

            //// Prompt the user for a choice.
            //return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);

            var formTemplatePath = Path.Combine(".", "Resources", "SelectIndicator.json");

            var cardPath = formTemplatePath;
            var cardJson = File.ReadAllText(cardPath);
            var cardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
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

        private async Task<DialogTurnResult> ThreatIndicatorFormStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<ResponseValue>((string)stepContext.Result);

                //var threatIndicatorType = ((FoundChoice)stepContext.Result).Value.ToLower();
                //// Possibly need this later on?
                //stepContext.Values["ThreatIndicatorType"] = threatIndicatorType;
                var done = result.Id.ToLower() == DoneOption;

                if (done)
                {
                    return await stepContext.EndDialogAsync();
                }

                var formTemplatePath = "";
                switch (result.Id.ToLower())
                {
                    case "ip":
                        formTemplatePath = Path.Combine(".", "Resources", "TIFormIP.json");
                        queries = Queries.IpQueries;
                        fieldToSearch = "NetworkDestinationIPv4";
                        break;
                    case "domain":
                        formTemplatePath = Path.Combine(".", "Resources", "TIFormDomain.json");
                        queries = Queries.DomainQueries;
                        fieldToSearch = "DomainName";
                        break;
                    case "url":
                        formTemplatePath = Path.Combine(".", "Resources", "TIFormUrl.json");
                        queries = Queries.UrlQueries;
                        fieldToSearch = "Url";
                        break;
                    case "filehash":
                        formTemplatePath = Path.Combine(".", "Resources", "TIFormFileHash.json");
                        queries = Queries.FileHashQueries;
                        fieldToSearch = "FileHashValue";
                        break;
                    default:
                        await stepContext.RepromptDialogAsync();
                        break;
                }

                var cardPath = formTemplatePath;
                var cardJson = File.ReadAllText(cardPath);
                var template = new AdaptiveCardTemplate(cardJson);

                var card = template.Expand(
                    new
                    {
                        expiration = DateTime.UtcNow.AddDays(365).ToString("yyyy-MM-dd")
                    }
                );

                var cardAttachment = new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(card),
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
            catch (Exception)
            {

            }
            return await stepContext.ReplaceDialogAsync(nameof(ThreatIndicatorDialog));
        }

        private async Task<DialogTurnResult> ProcessFormInput(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                //var test = JsonConvert.SerializeObject(activity.Value);
                ThreatIndicator = JsonConvert.DeserializeObject<ThreatIndicator>((string)stepContext.Result);

                ThreatIndicator.Description = $"Added through bot by user: {stepContext.Context.Activity.From.Name}";
                ThreatIndicator.TargetProduct = "Azure Sentinel";

                var attachments = new List<Attachment>();
                var reply = MessageFactory.Attachment(attachments);
                var cardAttachment = CreateAdaptiveCardAttachment(_cards[0], ThreatIndicator);

                reply.Attachments.Add(cardAttachment);

                await stepContext.Context.SendActivityAsync(reply, cancellationToken);

                var messageText = "Thank you, would you like to add this Threat Intelligence Indicator?";

                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

                return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            catch (Exception)
            {
                await stepContext.Context.SendActivityAsync($"Couldn't process the form! Please restart");
            }
            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> SubmitStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var success = await PushThreatIndicatorToGraphApiAsync(ThreatIndicator);

                if (success)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("TI indicator was successfully added."), cancellationToken);

                    // Ask the user if he/she wants Search All TI's and create an Incident?
                    // ConfirmPrompt
                    var messageText = "Do you want to search all your logs for this indicator and create an incident for any matches?";

                    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
                else
                {
                    var messageText = "Something went wrong while creating the indicator, please try again.";
                    // TODO: Show message to the user

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(messageText), cancellationToken);

                }
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> SearchIncidentsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                count = await SearchIncidentsAsync();

                if (count > 0)
                {
                    var incidentCreated = await CreateIncident();

                    if (incidentCreated)
                    {
                        var message = $"Successfully created an incident, you can view it with the following URI: {CreatedIncident.Properties.IncidentUrl}";
                        // Send Link to user of where te incident can be found
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                    }
                }
            }

            var messageText = "Thank you! This is the end of our conversation.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(messageText), cancellationToken);

            // End Dialog
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private Incident CreatedIncident { get; set; }

        private async Task<bool> CreateIncident()
        {
            var g = Guid.NewGuid();
            //PUT https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.OperationalInsights/workspaces/{workspaceName}/providers/Microsoft.SecurityInsights/incidents/{incidentId}?api-version=2021-04-01

            var incident = new Incident
            {
                Properties = new Properties()
            };

            // TODO Title & Description aanpassen
            incident.Properties.Title = $"Hit has been found for TI {fieldToSearchValue} added by Ollie Bot";
            incident.Properties.Status = "New";
            incident.Properties.Severity = "High";
            // Hit was found for TI: olivier.be, Add Query Where hit was found

            incident.Properties.Description = $"This incident has been generated because a hit has been found for a Threat Intelligence Indicator which was added through the Ollie Bot. Within the last 180 days, {count} hits were found for the TI {fieldToSearchValue}. It's recommended to investigate the occurrences and hunt through the data.";

            string json = JsonConvert.SerializeObject(incident, Formatting.Indented, new JsonSerializerSettings
            {
                // IGNORE Null values!
                NullValueHandling = NullValueHandling.Ignore
            });

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
                var response = await apiCaller.PUTCallSentinelWebApiAndProcessResultASync($"{config.SentinelApiUrl}subscriptions/{config.SubscriptionId}/resourceGroups/{config.ResourcegroupName}/providers/Microsoft.OperationalInsights/workspaces/{config.WorkspaceName}/providers/Microsoft.SecurityInsights/incidents/{g}?api-version={config.SentinelApiVersion}", result.AccessToken, stringContent);

                CreatedIncident = JsonConvert.DeserializeObject<Incident>(response);

                return true;

            }

            return false;
        }

        private async Task<int> SearchIncidentsAsync()
        {
            //https://api.loganalytics.io/v1/workspaces/20d758dc-1b14-4801-965a-636625e706a6/query?query=OfficeActivity| where ClientIP == "178.119.88.237"

            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            IConfidentialClientApplication app;

            app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            string[] scopes = new string[] { $"{config.LogAnalyticsApiUrl}.default" };

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

                switch (fieldToSearch)
                {
                    case "NetworkDestinationIPv4":
                        fieldToSearchValue = ThreatIndicator.NetworkDestinationIPv4;
                        break;
                    case "FileHashValue":
                        fieldToSearchValue = ThreatIndicator.FileHashValue;
                        break;
                    case "DomainName":
                        fieldToSearchValue = ThreatIndicator.DomainName;
                        break;
                    case "Url":
                        fieldToSearchValue = ThreatIndicator.Url;
                        break;
                    default:
                        break;
                }

                // Determine what queries we need to check
                var count = 0;
                foreach (var query in queries)
                {
                    count = await apiCaller.GETCallWebApiAndProcessResultASync($"{config.LogAnalyticsApiUrl}v1/workspaces/{config.WorkspaceId}/query?query={query}" + $"'{fieldToSearchValue}'" + "|summarize count()", result.AccessToken);

                    if (count > 0)
                    {
                        // no need to keep searching
                        break;
                    }
                }

                return count;
            }

            return -1;

        }

        #endregion

        #region PrivateMethods

        private static Attachment CreateAdaptiveCardAttachment(string filePath, ThreatIndicator indicator)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var template = new AdaptiveCardTemplate(adaptiveCardJson);

            var card = template.Expand(
                new
                {
                    title = "FileHash TI Summary",
                    action = indicator.Action,
                    description = indicator.Description,
                    threatType = indicator.ThreatType,
                    tlpLevel = indicator.TlpLevel,
                    expirationDateTime = indicator.ExpirationDateTime,
                    fileHashType = string.IsNullOrEmpty(indicator.FileHashType) ? string.Empty : indicator.FileHashType,
                    fileHashValue = string.IsNullOrEmpty(indicator.FileHashValue) ? string.Empty : indicator.FileHashValue,
                    networkDestinationIPv4 = string.IsNullOrEmpty(indicator.NetworkDestinationIPv4) ? string.Empty : indicator.NetworkDestinationIPv4,
                    domain = string.IsNullOrEmpty(indicator.DomainName) ? string.Empty : indicator.DomainName,
                    url = string.IsNullOrEmpty(indicator.Url) ? string.Empty : indicator.Url,

                }
            );

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(card),
            };
            return adaptiveCardAttachment;
        }

        private async Task<bool> PushThreatIndicatorToGraphApiAsync(ThreatIndicator ti)
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
            string[] scopes = new string[] { $"{config.GraphApiUrl}.default" };

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

            string json = JsonConvert.SerializeObject(ti, Formatting.Indented, new JsonSerializerSettings
            {
                // IGNORE Null values!
                NullValueHandling = NullValueHandling.Ignore
            });

            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                return await apiCaller.POSTCallWebApiAndProcessResultASync($"{config.GraphApiUrl}beta/security/tiIndicators", result.AccessToken, Display, stringContent);
            }

            return false;
        }

        // TODO Check if need this?
        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }
        }

        #endregion
    }
}
