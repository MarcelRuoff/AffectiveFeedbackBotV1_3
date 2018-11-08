// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IBM.WatsonDeveloperCloud.ToneAnalyzer.v3;
using IBM.WatsonDeveloperCloud.ToneAnalyzer.v3.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EchoWithCounterBot : IBot
    {
        private readonly EchoBotAccessors _accessors;
        private readonly ILogger _logger;
        private ToneChatInput toneChatInput = new ToneChatInput()
        {
            Utterances = new List<Utterance>(),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="EchoWithCounterBot"/> class.
        /// </summary>
        /// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public EchoWithCounterBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var response = turnContext.Activity.CreateReply();

                // Get the conversation state from the turn context.
                var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                if (turnContext.Activity.Text.ToLower() == "emoji")
                {
                    state.FeedbackType = turnContext.Activity.Text.ToLower();
                    response.Text = "Feedback Type changed to: Emoji";
                }
                else if (turnContext.Activity.Text.ToLower() == "graph")
                {
                    state.FeedbackType = turnContext.Activity.Text.ToLower();
                    response.Text = "Feedback Type changed to: Graph";
                }
                else
                {
                    string username = "c33ec0e1-58de-4dbb-987c-0a425f983a84";
                    string password = "5CsVdSN3ZXZn";
                    var versionDate = "2017-09-21";

                    ToneAnalyzerService toneAnalyzer = new ToneAnalyzerService(username, password, versionDate);

                    ToneInput toneInput = new ToneInput()
                    {
                        Text = turnContext.Activity.Text,
                    };

                    /**
                    Utterance input = new Utterance()
                    {
                        Text = turnContext.Activity.Text,
                        User = turnContext.Activity.From.Name,
                    };

                    toneChatInput.Utterances.Add(input);
                    */
                    var postToneResult = toneAnalyzer.Tone(toneInput, "application/json", null);

                    // foreach (Tone tone in tones)
                    if (state.FeedbackType == "emoji")
                    {
                        response.Text = EmojiResponseGenerator(postToneResult);
                    }
                    else if (state.FeedbackType == "graph")
                    {
                        response.Attachments = new List<Attachment>() { GraphResponseGenerator(postToneResult, state).ToAttachment() };
                    }
                }

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);

                await turnContext.SendActivityAsync(response);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

        public string EmojiResponseGenerator(ToneAnalysis postReponse)
        {
            string responseMessage = null;

            foreach (ToneScore tone in postReponse.DocumentTone.Tones)
            {
                    if (tone.ToneId == "joy")
                    {
                        responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F917 \n";
                    }
                    else if (tone.ToneId == "anger")
                    {
                        responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F612 \n";
                    }
                    else if (tone.ToneId == "sadness")
                    {
                        responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F642 \n";
                    }
                    else if (tone.ToneId == "fear")
                    {
                        responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F600 \n";
                    }
                    else
                    {
                        responseMessage += "No Tone detected.";
                    }
            }

            return responseMessage;
        }

        public HeroCard GraphResponseGenerator(ToneAnalysis postReponse, CounterState state)
        {
            string graphURL = "https://chart.googleapis.com/chart?cht=lc&chco=FF0000,00FF00,0000FF,000000&chs=250x150&chxt=x,y&chd=t4:";

            int joy = 0;
            int anger = 0;
            int sadness = 0;
            int fear = 0;

            foreach (ToneScore tone in postReponse.DocumentTone.Tones)
            {
                int score = (int)(100 * tone.Score);

                if (tone.ToneId == "joy")
                {
                    joy = score;
                }
                else if (tone.ToneId == "anger")
                {
                    anger = score;
                }
                else if (tone.ToneId == "sadness")
                {
                    sadness = score;
                }
                else if (tone.ToneId == "fear")
                {
                    fear = score;
                }
            }

            if (state.Joy == string.Empty)
                {
                    state.Joy += joy;
                }
                else
                {
                    state.Joy += "," + joy;
                }

            if (state.Anger == string.Empty)
                {
                    state.Anger += anger;
                }
                else
                {
                    state.Anger += "," + anger;
                }

            if (state.Sadness == string.Empty)
                {
                    state.Sadness += sadness;
                }
                else
                {
                    state.Sadness += "," + sadness;
                }

            if (state.Fear == string.Empty)
                {
                    state.Fear += fear;
                }
                else
                {
                    state.Fear += "," + fear;
                }

            graphURL = graphURL + state.Anger + '|' + state.Fear + '|' + state.Joy + '|' + state.Sadness;

            HeroCard heroCard = new HeroCard
            {
                Text = "Your current State: ",
                Images = new List<CardImage> { new CardImage(graphURL) },
            };

            return heroCard;
        }
    }
}
