// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using EchoBotWithCounter;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;
using IBM.WatsonDeveloperCloud.ToneAnalyzer.v3;
using IBM.WatsonDeveloperCloud.ToneAnalyzer.v3.Model;
using IBM.WatsonDeveloperCloud.Util;
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

                bool exist = false;
                bool notAdmin = true;
                User currentUser = new User();
                TimeSpan difference = DateTime.Now - state.Date;

                /*
                IMongoCollection<Conversation> conversationCollection = null;

                var client = new MongoClient("mongodb://admin:Insertant_-96@affectivefeedback-shard-00-00-advf9.azure.mongodb.net:27017,affectivefeedback-shard-00-01-advf9.azure.mongodb.net:27017,affectivefeedback-shard-00-02-advf9.azure.mongodb.net:27017/test?ssl=true&replicaSet=AffectiveFeedback-shard-0&authSource=admin&retryWrites=true");
                var database = client.GetDatabase("AffectiveFeedback");

                if (state.CollectionName == string.Empty)
                {
                    string collectionName = "ChatHistory-" + DateTime.Now.ToLongTimeString() + ".sqlite";
                    collectionName = collectionName.Replace(":", "-");

                    state.CollectionName = collectionName;

                    database.CreateCollection(state.CollectionName);
                    conversationCollection = database.GetCollection<Conversation>(state.CollectionName);
                }
                else
                {
                    conversationCollection = database.GetCollection<Conversation>(state.CollectionName);
                }
                */

                foreach (User user in state.Users)
                {
                    if (user.UserId == turnContext.Activity.From.Id)
                    {
                        exist = true;
                        currentUser = user;
                    }
                }

                if (exist == false)
                {
                    currentUser = new User(turnContext.Activity.From.Id);
                    currentUser.UserName = turnContext.Activity.From.Name;
                    state.Users.Add(currentUser);
                }

                if (turnContext.Activity.From.Id == "UECMZ1UKV:TEAAW1S5V" || turnContext.Activity.From.Id == "UEF40P8QP:TEDA8FEEL")
                {
                    notAdmin = false;
                }
                else if (turnContext.Activity.Text.ToLower() == "emoji")
                {
                    state.FeedbackType = turnContext.Activity.Text.ToLower();
                    response.Text = "Feedback Type changed to: Emoji";
                    state.NeededDifference = TimeSpan.FromMinutes(0);
                }
                else if (turnContext.Activity.Text.ToLower() == "graph")
                {
                    state.FeedbackType = turnContext.Activity.Text.ToLower();
                    response.Text = "Feedback Type changed to: Graph";
                    state.NeededDifference = TimeSpan.FromSeconds(30);
                }
                else if (turnContext.Activity.Text.ToLower() == "empathy")
                {
                    state.FeedbackType = turnContext.Activity.Text.ToLower();
                    response.Text = "Feedback Type changed to: Empathy";
                    state.NeededDifference = TimeSpan.FromSeconds(30);
                }
                else if (turnContext.Activity.Text.ToLower() == "scatter")
                {
                    state.FeedbackType = turnContext.Activity.Text.ToLower();
                    response.Text = "Feedback Type changed to: Scatter";
                    state.NeededDifference = TimeSpan.FromSeconds(30);
                }
                else if (turnContext.Activity.Text == "Yes, I want to see our current state.")
                {
                    HeroCard heroCard = new HeroCard
                    {
                        Title = "Your current state ",
                        Images = new List<CardImage> { new CardImage(state.ScatterURL) },
                    };

                    response.Attachments = new List<Attachment>() { heroCard.ToAttachment() };

                    state.SendImage = true;
                }
                else if (turnContext.Activity.Text == "Gib den usernamen aus!.!")
                {
                    response.Text += turnContext.Activity.From.Id;
                }
                else
                {
                    string username = "c33ec0e1-58de-4dbb-987c-0a425f983a84";
                    string password = "5CsVdSN3ZXZn";
                    var versionDate = "2017-09-21";
                    var versionDate1 = "2018-03-19";
                    string apikey = "X8a8d3FSqoadDqcWMsRe2hDd5uSeQR9E6gcm92zIxXMA";
                    string url = "https://gateway-fra.watsonplatform.net/natural-language-understanding/api";

                    /*
                    TokenOptions iamAssistantTokenOptions = new TokenOptions()
                    {
                        IamApiKey = apikey,
                        ServiceUrl = url,
                    };

                    NaturalLanguageUnderstandingService understandingService = new NaturalLanguageUnderstandingService(iamAssistantTokenOptions, versionDate1);

                    Parameters parameters = new Parameters()
                    {
                        Text = "This is very good news!!",
                    };

                    var result = understandingService.Analyze(parameters);
                    */

                    ToneAnalyzerService toneAnalyzer = new ToneAnalyzerService(username, password, versionDate);

                    ToneInput toneInput = new ToneInput()
                    {
                        Text = turnContext.Activity.Text,
                    };

                    var postToneResult = toneAnalyzer.Tone(toneInput, "application/json", null);

                    Conversation conversation = new Conversation(turnContext.Activity.From.Id, turnContext.Activity.Text, postToneResult.ToString());
                    // conversationCollection.InsertOne(conversation);

                    // foreach (Tone tone in tones)
                    if (state.FeedbackType == "emoji")
                    {
                        response.Text = EmojiResponseGenerator(postToneResult);
                    }
                    else if (state.FeedbackType == "graph")
                    {
                        response.Attachments = new List<Attachment>() { GraphResponseGenerator(postToneResult, state).ToAttachment() };
                    }
                    else if (state.FeedbackType == "scatter")
                    {
                        response.Attachments = new List<Attachment>() { ScatterResponseGenerator(postToneResult, state, currentUser).ToAttachment() };
                    }
                    else if (state.FeedbackType == "empathy")
                    {
                        HeroCard heroCard = EmpathyResponseGenerator(postToneResult, state, currentUser);

                        string responseText = string.Empty;
                        foreach (User user1 in state.Users)
                        {
                            foreach (User user2 in state.Users)
                            {
                                double distance = Math.Abs(Math.Sqrt(Math.Pow(user1.X - user2.X, 2) + Math.Pow(user1.Y - user2.Y, 2)));
                                if (distance > 50 && responseText == string.Empty)
                                {
                                    responseText = "Your team mood is dispersed.";
                                }
                            }

                            if (user1.X >= 40 && user1.X < 60 && user1.Y < 40 && responseText == string.Empty)
                            {
                                responseText = "You're working hard. Keep on trying.  \n \U0001F917";
                            }
                        }

                        foreach (User user1 in state.Users)
                        {
                            if (user1.X < 40 && user1.Y < 40 && responseText == string.Empty)
                            {
                                responseText = "Don't let yourself down. You can do this. \n \U0001F917";
                            }
                        }

                        foreach (User user1 in state.Users)
                        {
                            if (user1.X >= 60 && user1.Y < 40 && responseText == string.Empty)
                            {
                                responseText = "Don't worry. You can do this. \n \U0001F917";
                            }
                        }

                        foreach (User user1 in state.Users)
                        {
                            if (user1.X >= 40 && user1.X < 70 && user1.Y >= 40 && user1.Y < 50 && responseText == string.Empty)
                            {
                                responseText = "You're working hard. Keep on trying.  \n \U0001F917";
                            }
                        }

                        foreach (User user1 in state.Users)
                        {
                            if (user1.X >= 70 && user1.Y >= 40 && user1.Y < 50 && responseText == string.Empty)
                            {
                                responseText = "You're doing great. \n \U0001F917";
                            }
                        }

                        heroCard.Title = responseText;

                        response.Attachments = new List<Attachment>() { heroCard.ToAttachment() };
                    }
                }

                if (notAdmin && (difference >= state.NeededDifference || state.SendImage))
                {
                    await turnContext.SendActivityAsync(response);
                    if (!state.SendImage)
                    {
                        state.Date = DateTime.Now;
                    }

                    state.SendImage = false;
                }

                notAdmin = true;

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

        public string EmojiResponseGenerator(ToneAnalysis postReponse)
        {
            string responseMessage = null;
            double highestScore = 0;

            foreach (ToneScore tone in postReponse.DocumentTone.Tones)
            {
                if (tone.ToneId == "joy" && highestScore <= tone.Score)
                    {
                        responseMessage = $"\U0001F917 \n";
                        highestScore = (double)tone.Score;
                    }
                else if (tone.ToneId == "sadness" && highestScore <= tone.Score)
                    {
                        responseMessage = $"\U0001F614 \n";
                        highestScore = (double)tone.Score;
                }
                else if (tone.ToneId == "fear" && highestScore <= tone.Score)
                    {
                        responseMessage = $"\U0001F61F \n";
                        highestScore = (double)tone.Score;
                }
                else if (tone.ToneId == "anger" && highestScore <= tone.Score)
                {
                    responseMessage = $"\U0001F620 \n";
                    highestScore = (double)tone.Score;
                }
            }

            return responseMessage;
        }

        public HeroCard GraphResponseGenerator(ToneAnalysis postReponse, CounterState state)
        {

            state.TurnCount += 1;
            string graphURL = "https://chart.googleapis.com/chart?cht=lc&chco=FF0000,000000,00FF00,0000FF&chdl=Anger|Fear|Joy|Sadness&chxr=0,0," + state.TurnCount + "|1,0.5,1&chs=250x150&chxt=x,y&chd=t4:";

            int joy = 0;
            int anger = 0;
            int sadness = 0;
            int fear = 0;

            foreach (ToneScore tone in postReponse.DocumentTone.Tones)
            {
                int score = (int)(100 * (tone.Score - 0.5) * 2);

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

        public HeroCard EmpathyResponseGenerator(ToneAnalysis postReponse, CounterState state, User currentUser)
        {
            double joy = 0;
            double anger = 0;
            double sadness = 0;
            double fear = 0;
            int x = 0;
            int y = 0;
            string[] userNames = { "-", "-", "-" };

            foreach (ToneScore tone in postReponse.DocumentTone.Tones)
            {
                if (tone.ToneId == "joy")
                {
                    joy = (double)tone.Score;
                }
                else if (tone.ToneId == "anger")
                {
                    anger = (double)tone.Score;
                }
                else if (tone.ToneId == "sadness")
                {
                    sadness = (double)tone.Score;
                }
                else if (tone.ToneId == "fear")
                {
                    fear = (double)tone.Score;
                }
            }

            int numberOfTones = (int)(Math.Ceiling(joy) + Math.Ceiling(anger) + Math.Ceiling(fear) + Math.Ceiling(sadness));

            if ((Math.Ceiling(joy) + Math.Ceiling(anger) + Math.Ceiling(fear) + Math.Ceiling(sadness)) != 0)
            {
                x = 50 + (int)Math.Ceiling(49 * ((0.5 * joy) + (0.5 * anger) + (0.8 * fear) - (0.6 * sadness)) / numberOfTones);
                y = 50 + (int)Math.Ceiling(49 * ((0.9 * joy) - (0.5 * anger) - (0.6 * fear) - (0.8 * sadness)) / numberOfTones);
            }

            if (currentUser.X == 0)
            {
                currentUser.X += x;
            }
            else
            {
                currentUser.X = (int)((0.4 * x) + (0.6 * currentUser.X));
            }

            if (currentUser.Y == 0)
            {
                currentUser.Y += y;
            }
            else
            {
                currentUser.Y = (int)((0.4 * y) + (0.6 * currentUser.Y));
            }

            List<User> updatedUsers = new List<User>();
            string finalX = string.Empty;
            string finalY = string.Empty;
            foreach (User user in state.Users)
            {
                if (user.UserId == currentUser.UserId)
                {
                    updatedUsers.Add(currentUser);
                    finalX += currentUser.X + ",";
                    finalY += currentUser.Y + ",";
                }
                else
                {
                    updatedUsers.Add(user);
                    finalX += user.X + ",";
                    finalY += user.Y + ",";
                }

                userNames[state.Users.IndexOf(user)] = user.UserName;
            }

            state.Users = updatedUsers;
            /**
            List<int> updatedRadius = new List<int>();
            state.Radius.ForEach(radius => updatedRadius.Add(radius - 10));
            updatedRadius.Add(100);
            state.Radius = updatedRadius;

            string radiusString = string.Empty;
            state.Radius.ForEach(radius => radiusString += radius + ",");

            radiusString = radiusString.Remove(radiusString.Length - 1);
    */
            finalX = finalX.Remove(finalX.Length - 1);
            finalY = finalY.Remove(finalY.Length - 1);

            state.ScatterURL = "https://chart.googleapis.com/chart?cht=s&chs=470x400&chm=R,d10300,0,0.5,1|R,ffd800,0,0,0.5|r,008000,0,1,0.5&chco=000000|0c00fc|5700a3,ffffff&chxt=x,x,y,y&chdl=" + userNames[0] +" | " + userNames[1] + " | " + userNames[2] + " & chxr=0,-1,1|1,-1,1|2,-1,1|3,-1,1&chxl=1:|low%20arousal|high%20arousal|3:|displeasure|pleasure&chxs=0,ff0000|1,ff0000|2,0000ff|3,0000ff&chd=t:" + finalX + "|" + finalY;

            List<CardAction> cardButtons = new List<CardAction>()
            {
                new CardAction() { Title = "Yes, I want to see our current state.", Type = ActionTypes.ImBack, Value = "Yes, I want to see our current state." },
            };

            HeroCard heroCard = new HeroCard
            {
                Text = "Do you want to see your current State? ",
                Buttons = cardButtons,
            };

            return heroCard;
        }

        public HeroCard ScatterResponseGenerator(ToneAnalysis postReponse, CounterState state, User currentUser)
        {
            double joy = 0;
            double anger = 0;
            double sadness = 0;
            double fear = 0;
            int x = 0;
            int y = 0;

            string[] userNames = { "-", "-", "-" };

            foreach (ToneScore tone in postReponse.DocumentTone.Tones)
            {
                if (tone.ToneId == "joy")
                {
                    joy = (double)tone.Score;
                }
                else if (tone.ToneId == "anger")
                {
                    anger = (double)tone.Score;
                }
                else if (tone.ToneId == "sadness")
                {
                    sadness = (double)tone.Score;
                }
                else if (tone.ToneId == "fear")
                {
                    fear = (double)tone.Score;
                }
            }

            int numberOfTones = (int)(Math.Ceiling(joy) + Math.Ceiling(anger) + Math.Ceiling(fear) + Math.Ceiling(sadness));

            if ((Math.Ceiling(joy) + Math.Ceiling(anger) + Math.Ceiling(fear) + Math.Ceiling(sadness)) != 0)
            {
                x = 50 + (int)Math.Ceiling(49 * ((0.5 * joy) + (0.5 * anger) + (0.8 * fear) - (0.6 * sadness)) / numberOfTones);
                y = 50 + (int)Math.Ceiling(49 * ((0.9 * joy) - (0.5 * anger) - (0.6 * fear) - (0.8 * sadness)) / numberOfTones);
            }

            if (currentUser.X == 0)
            {
                currentUser.X += x;
            }
            else
            {
                currentUser.X = (int)((0.4 * x) + (0.6 * currentUser.X));
            }

            if (currentUser.Y == 0)
            {
                currentUser.Y += y;
            }
            else
            {
                currentUser.Y = (int)((0.4 * y) + (0.6 * currentUser.Y));
            }

            List<User> updatedUsers = new List<User>();
            string finalX = string.Empty;
            string finalY = string.Empty;
            foreach (User user in state.Users)
            {
                if (user.UserId == currentUser.UserId)
                {
                    updatedUsers.Add(currentUser);
                    finalX += currentUser.X + ",";
                    finalY += currentUser.Y + ",";
                }
                else
                {
                    updatedUsers.Add(user);
                    finalX += user.X + ",";
                    finalY += user.Y + ",";
                }

                userNames[state.Users.IndexOf(user)] = user.UserName;
            }

            state.Users = updatedUsers;
            /**
            List<int> updatedRadius = new List<int>();
            state.Radius.ForEach(radius => updatedRadius.Add(radius - 10));
            updatedRadius.Add(100);
            state.Radius = updatedRadius;

            string radiusString = string.Empty;
            state.Radius.ForEach(radius => radiusString += radius + ",");

            radiusString = radiusString.Remove(radiusString.Length - 1);
    */
            finalX = finalX.Remove(finalX.Length - 1);
            finalY = finalY.Remove(finalY.Length - 1);

            state.ScatterURL = "https://chart.googleapis.com/chart?cht=s&chs=470x400&chm=R,d10300,0,0.5,1|R,ffd800,0,0,0.5|r,008000,0,1,0.5&chco=000000|0c00fc|5700a3,ffffff&chxt=x,x,y,y&chdl=" + userNames[0] +"|" + userNames[1] + "|" + userNames[2] + "&chxr=0,-1,1|1,-1,1|2,-1,1|3,-1,1&chxl=1:|low%20arousal|high%20arousal|3:|displeasure|pleasure&chxs=0,ff0000|1,ff0000|2,0000ff|3,0000ff&chd=t:" + finalX + "|" + finalY;

            HeroCard heroCard = new HeroCard
            {
                Text = "Your current State: ",
                Images = new List<CardImage> { new CardImage(state.ScatterURL) },
            };

            return heroCard;
        }
    }
}
