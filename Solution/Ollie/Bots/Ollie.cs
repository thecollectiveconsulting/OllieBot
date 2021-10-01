using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ollie.Bots
{
    public class Ollie<T> : ActivityHandler where T : Dialog
    {
        private readonly ConversationState conversationState;
        private readonly UserState userState;
        private readonly T dialog;
        private readonly ILogger<Ollie<T>> logger;

        public Ollie(ConversationState conversationState, UserState userState, T dialog, ILogger<Ollie<T>> logger)
        {
            this.conversationState = conversationState;
            this.userState = userState;
            this.dialog = dialog;
            this.logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await this.conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await this.userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            logger.LogInformation("Running dialog with message activity");

            var activity = turnContext.Activity;

            if (string.IsNullOrWhiteSpace(activity.Text) && activity.Value != null)
            {
                activity.Text = JsonConvert.SerializeObject(activity.Value);
            }

            await dialog.RunAsync(turnContext, conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}
