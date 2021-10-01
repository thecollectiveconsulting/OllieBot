using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ollie.Models;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.IO;
using System;

namespace Ollie.Dialogs
{
    /// <summary>
    /// This is the root dialog, we start from here and redirect to the specific child dialogs
    /// based on user input (TI or watchlist)
    /// </summary>
    public class MainDialog : ComponentDialog
    {
        private readonly UserState _userState;

        public MainDialog(UserState userState) 
            : base(nameof(MainDialog))
        {
            _userState = userState;

            // Add child dialog(s)
            AddDialog(new ThreatIndicatorDialog());
            AddDialog(new WatchlistDialog());

            // Define steps of this dialog
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                ProcessSelectionStepAsync,
                FinalStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);

        }

        /// <summary>
        /// Prompt the user to choose an action
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var test = _userState;

            var formTemplatePath = Path.Combine(".", "Resources", "SelectAction.json");

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


            //// WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            //// Running a prompt here means the next WaterfallStep will be run when the user's response is received.
            //return await stepContext.PromptAsync(nameof(ChoicePrompt),
            //    new PromptOptions
            //    {
            //        Prompt = MessageFactory.Text("Please select an action to perform"),
            //        Choices = ChoiceFactory.ToChoices(new List<string> {"Add Threat Intelligence Indicator.", "Update watchlist items." }),
            //    }, cancellationToken);
        }

        /// <summary>
        /// Activate child dialogs based on the users choice
        /// </summary>
        /// <param name="stepContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<DialogTurnResult> ProcessSelectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<ResponseValue>((string)stepContext.Result);

                // Switch on securityChoice
                return result.Id.ToLower() switch
                {
                    // Create a TI
                    "add" => await stepContext.BeginDialogAsync(nameof(ThreatIndicatorDialog), new ThreatIndicator(), cancellationToken),
                    "update" => await stepContext.BeginDialogAsync(nameof(WatchlistDialog), new WatchList(), cancellationToken),
                    _ => await stepContext.EndDialogAsync()
                };
            }
            catch (Exception)
            {
                return await stepContext.ReplaceDialogAsync(nameof(MainDialog));
            }
        }

        // The Final step is to submit the form
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If no TI was generated -> Stop the conversation
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        

        

        //private static async Task<DialogTurnResult> TiStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    stepContext.Values["security"] = ((FoundChoice)stepContext.Result).Value;

        //    // Prompt user to make a choice
        //    return await stepContext.PromptAsync(nameof(ChoicePrompt),
        //        new PromptOptions
        //        {
        //            Prompt = MessageFactory.Text("What kind of TI do you want to Add?"),
        //            Choices = ChoiceFactory.ToChoices(new List<string> { "IP", "Domain", "Url", "File hash" }),
        //        }, cancellationToken);

        //    //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") }, cancellationToken);
        //}

        //private static async Task<DialogTurnResult> WatchlistStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    stepContext.Values["security"] = ((FoundChoice)stepContext.Result).Value;

        //    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") }, cancellationToken);
        //}
    }
}
