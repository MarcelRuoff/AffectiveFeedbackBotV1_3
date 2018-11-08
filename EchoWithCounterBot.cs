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
                string username = "c33ec0e1-58de-4dbb-987c-0a425f983a84";
                string password = "5CsVdSN3ZXZn";
                var versionDate = "2017-09-21";

                ToneAnalyzerService toneAnalyzer = new ToneAnalyzerService(username, password, versionDate);

                ToneInput toneInput = new ToneInput()
                {
                    Text = turnContext.Activity.Text,
                };

                Utterance input = new Utterance()
                {
                    Text = turnContext.Activity.Text,
                    User = turnContext.Activity.From.Name,
                };

                toneChatInput.Utterances.Add(input);

                var postToneResult = toneAnalyzer.ToneChat(toneChatInput, "application/json", null);

                // Get the conversation state from the turn context.
                var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Bump the turn count for this conversation.
                state.TurnCount++;

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);

                // Echo back to the state of the user.
                string responseMessage = null;

                // foreach (Tone tone in tones)
                foreach (UtteranceAnalysis utterance_Tone in postToneResult.UtterancesTone)
                {
                    foreach (ToneChatScore tone in utterance_Tone.Tones)
                    {
                        if (tone.ToneId == ToneChatScore.ToneIdEnum.EXCITED)
                        {
                            responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F917 \n";
                        }
                        else if (tone.ToneId == ToneChatScore.ToneIdEnum.FRUSTRATED)
                        {
                            responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F612 \n";
                        }
                        else if (tone.ToneId == ToneChatScore.ToneIdEnum.IMPOLITE)
                        {
                            responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F620 \n";
                        }
                        else if (tone.ToneId == ToneChatScore.ToneIdEnum.POLITE)
                        {
                            responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F642 \n";
                        }
                        else if (tone.ToneId == ToneChatScore.ToneIdEnum.SAD)
                        {
                            responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F642 \n";
                        }
                        else if (tone.ToneId == ToneChatScore.ToneIdEnum.SATISFIED)
                        {
                            responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F60A \n";
                        }
                        else if (tone.ToneId == ToneChatScore.ToneIdEnum.SYMPATHETIC)
                        {
                            responseMessage += $"You feel '{tone.ToneName}' with a score of: {tone.Score}\n \U0001F600 \n";
                        }
                        else
                        {
                            responseMessage += "No Tone detected.";
                        }
                    }

                    await turnContext.SendActivityAsync(responseMessage);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }
    }
}
