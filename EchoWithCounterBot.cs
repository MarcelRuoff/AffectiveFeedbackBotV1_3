// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EchoBotWithCounter;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;
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
                bool responseExists = false;
                bool notAdmin = true;
                User currentUser = new User();
                TimeSpan difference = DateTime.Now - state.Date;
                TimeSpan differenceImage = DateTime.Now - state.DateImage;

                // SqlConnection myConnection = new SqlConnection("Server=tcp:thesis-affective.database.windows.net,1433;Initial Catalog=Bachelorarbeit;Persist Security Info=False;User ID=issd-affective;Password=zWBR5IRI3u7zUzjMqADZ;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

                if (turnContext.Activity.From.Id == "UECMZ1UKV:TEAAW1S5V" || turnContext.Activity.From.Id == "UEF40P8QP:TEDA8FEEL")
                {
                    notAdmin = false;

                    if (turnContext.Activity.Text.StartsWith("mysql-database:"))
                    {
                        state.GroupName = turnContext.Activity.Text.Substring(16);
                    }
                    else if (turnContext.Activity.Text.StartsWith("Chatbot-turn-off"))
                    {
                        state.ChatbotOn = false;
                    }
                    else if (turnContext.Activity.Text.StartsWith("Chatbot-turn-on"))
                    {
                        state.ChatbotOn = true;
                    }
                }
                else if (turnContext.Activity.Text == "Yes, I want to see our current state.")
                {
                    HeroCard heroCard = new HeroCard
                    {
                        Title = "Your current State",
                        Images = new List<CardImage> { new CardImage(state.ScatterURL) },
                    };

                    response.Attachments = new List<Attachment>() { heroCard.ToAttachment() };

                    if (differenceImage > TimeSpan.FromSeconds(10))
                    {
                        state.SendImage = true;
                        state.DateImage = DateTime.Now;
                    }
                }
                else if (turnContext.Activity.Text == "Gib den usernamen aus!.!")
                {
                    response.Text += turnContext.Activity.From.Id;
                }
                else
                {
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
                        currentUser = new User(turnContext.Activity.From.Id)
                        {
                            UserName = turnContext.Activity.From.Name,
                        };

                        if (turnContext.Activity.From.Name == "jourdyn77")
                        {
                            currentUser.UserName = "Marine";
                        }
                        else if (turnContext.Activity.From.Name == "zayo")
                        {
                            currentUser.UserName = "Police";
                        }
                        else if (turnContext.Activity.From.Name == "sihicracip")
                        {
                            currentUser.UserName = "Helicopter";
                        }
                        else if (turnContext.Activity.From.Name == "marcelruoff")
                        {
                            currentUser.UserName = "Member_2";
                        }
                        else if (turnContext.Activity.From.Name == "sletratrug")
                        {
                            currentUser.UserName = "Member_1";
                        }
                        else
                        {
                            currentUser.UserName = turnContext.Activity.From.Name;
                        }

                        state.Users.Add(currentUser);
                    }

                    var versionDate1 = "2018-03-19";

                    TokenOptions iamAssistantTokenOptions = new TokenOptions()
                    {
                        IamApiKey = "Do7LE3vasQbi5I81tj9fWnnsEGvE00_yhYA6yCugj3bz",
                        ServiceUrl = "https://gateway-fra.watsonplatform.net/natural-language-understanding/api",
                    };

                    NaturalLanguageUnderstandingService understandingService = new NaturalLanguageUnderstandingService(iamAssistantTokenOptions, versionDate1);

                    Parameters parameters = new Parameters()
                    {
                        Text = turnContext.Activity.Text,
                        Features = new Features()
                        {

                            Emotion = new EmotionOptions()
                            {
                                Document = true,
                            },
                        },
                        Language = "en",
                    };

                    var result = understandingService.Analyze(parameters);

                    /*
                    string queryString = "INSERT INTO " + state.GroupName + " (time, id, text, result) Values(@time, @id, @text, @result)";

                    SqlCommand command = new SqlCommand(queryString, myConnection);
                    command.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd H:mm:ss"));
                    command.Parameters.AddWithValue("@id", turnContext.Activity.From.Id);
                    command.Parameters.AddWithValue("@text", turnContext.Activity.Text);
                    command.Parameters.AddWithValue("@result", result.ResponseJson.ToString());
                    command.Connection = myConnection;
                    myConnection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                    */

                    HeroCard heroCard = EmpathyResponseGenerator(result, state, currentUser);

                    string responseText = string.Empty;
                    foreach (User user1 in state.Users)
                    {
                        foreach (User user2 in state.Users)
                        {
                            double distance = Math.Abs(Math.Sqrt(Math.Pow(user1.X - user2.X, 2) + Math.Pow(user1.Y - user2.Y, 2)));
                            if (distance > 100 && responseText == string.Empty)
                            {
                                responseText = "Your team mood is dispersed.";
                            }
                        }

                        if (user1.X >= 50 && user1.X < 75 && user1.Y < 45 && responseText == string.Empty)
                        {
                            Random random = new Random();
                            int caseSwitch = random.Next(1, 5);
                            if (caseSwitch > 4)
                            {
                                caseSwitch = 4;
                            }

                            switch (caseSwitch)
                            {
                                case 1:
                                    responseText = "You may feel currently upset, but you can do this."; // angry
                                    break;
                                case 2:
                                    responseText = "It sounds like some felt fairly frustrated choosing the fields."; // angry
                                    break;
                                case 3:
                                    responseText = "You're working hard. Keep on trying.  \n \U0001F917"; // angry
                                    break;
                                case 4:
                                    responseText = "Keep moving forward and you will succeed."; // angry
                                    break;
                            }
                        }
                    }

                    foreach (User user1 in state.Users)
                    {
                        if (user1.X < 25 && user1.Y < 50 && responseText == string.Empty)
                        {
                            Random random = new Random();
                            int caseSwitch = random.Next(1, 3);
                            if (caseSwitch > 2)
                            {
                                caseSwitch = 2;
                            }

                            switch (caseSwitch)
                            {
                                case 1:
                                    responseText = "Don't let yourself down. You can do this. \n \U0001F917"; // sad
                                    break;
                                case 2:
                                    responseText = "Believe in yourself. \n \U0001F917"; // sad
                                    break;
                            }
                        }
                    }

                    foreach (User user1 in state.Users)
                    {
                        if (user1.X >= 75 && user1.Y < 50 && responseText == string.Empty)
                        {
                            Random random = new Random();
                            int caseSwitch = random.Next(1, 5);
                            if (caseSwitch > 4)
                            {
                                caseSwitch = 4;
                            }

                            switch (caseSwitch)
                            {
                                case 1:
                                    responseText = "Don't worry. You can do this. \n \U0001F917"; // afraid
                                    break;
                                case 2:
                                    responseText = "Just keep discussing - you will get the hang of it."; // afraid
                                    break;
                                case 3:
                                    responseText = "Don't let your fears to stand in your way."; // afraid
                                    break;
                                case 4:
                                    responseText = "Believe in yourself. \n \U0001F917"; // afraid
                                    break;
                            }
                        }
                    }

                    foreach (User user1 in state.Users)
                    {
                        if (user1.X >= 25 && user1.X < 50 && user1.Y < 45 && responseText == string.Empty)
                        {
                            Random random = new Random();
                            int caseSwitch = random.Next(1, 3);
                            if (caseSwitch > 2)
                            {
                                caseSwitch = 2;
                            }

                            switch (caseSwitch)
                            {
                                case 1:
                                    responseText = "You're working hard. Keep on trying.  \n \U0001F917"; // disgusted
                                    break;
                                case 2:
                                    responseText = "Just keep discussing - you will get the hang of it"; // disgusted
                                    break;
                            }
                        }
                    }

                    if (responseText != string.Empty)
                    {
                        responseExists = true;
                    }

                    heroCard.Title = responseText;

                    response.Attachments = new List<Attachment>() { heroCard.ToAttachment() };
                }

                if (state.ChatbotOn && notAdmin && ((difference >= state.NeededDifference && responseExists) || state.SendImage))
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

        public HeroCard EmpathyResponseGenerator(AnalysisResults postReponse, CounterState state, User currentUser)
        {
            string[] userNames = new string[state.Users.Count];
            string userName = string.Empty;

            currentUser.Joy = (0.4 * currentUser.Joy) + (0.6 * postReponse.Emotion.Document.Emotion.Joy.Value);
            currentUser.Anger = (0.4 * currentUser.Anger) + (0.6 * postReponse.Emotion.Document.Emotion.Anger.Value);
            currentUser.Sadness = (0.4 * currentUser.Sadness) + (0.6 * postReponse.Emotion.Document.Emotion.Sadness.Value);
            currentUser.Fear = (0.4 * currentUser.Fear) + (0.6 * postReponse.Emotion.Document.Emotion.Fear.Value);
            currentUser.Disgust = (0.4 * currentUser.Disgust) + (0.6 * postReponse.Emotion.Document.Emotion.Disgust.Value);

            // int numberOfTones = (int)(Math.Ceiling(currentUser.Joy) + Math.Ceiling(currentUser.Anger) + Math.Ceiling(currentUser.Fear) + Math.Ceiling(currentUser.Sadness) + Math.Ceiling(currentUser.Disgust));
            double numberOfTones = Math.Max(currentUser.Joy + currentUser.Anger + currentUser.Fear + currentUser.Sadness + currentUser.Disgust, 1);

            if (numberOfTones != 0)
            {
                currentUser.X = 50 + (int)Math.Ceiling(50 * ((0.5 * currentUser.Joy) + (0.3 * currentUser.Anger) + (0.7 * currentUser.Fear) - (0.8 * currentUser.Sadness) - (0.2 * currentUser.Disgust)) / numberOfTones);
                currentUser.Y = 50 + (int)Math.Ceiling(50 * ((0.9 * currentUser.Joy) - (0.8 * currentUser.Anger) - (0.6 * currentUser.Fear) - (0.4 * currentUser.Sadness) - (0.7 * currentUser.Disgust)) / numberOfTones);

                currentUser.X = Math.Max(currentUser.X, 0);
                currentUser.X = Math.Min(currentUser.X, 100);

                currentUser.Y = Math.Max(currentUser.Y, 0);
                currentUser.Y = Math.Min(currentUser.Y, 100);
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

                userName += user.UserName + "|";
            }

            userName = userName.Remove(userName.Length - 1);
            state.Users = updatedUsers;

            finalX = finalX.Remove(finalX.Length - 1);
            finalY = finalY.Remove(finalY.Length - 1);

            int numberOfUsers = state.Users.Count;

            state.ScatterURL = "https://chart.googleapis.com/chart?cht=s&chs=470x400&chem=y;s=bubble_text_small;d=bbT,Joy,FFFFFF;dp=" + numberOfUsers + "|chem=y;s=bubble_text_small;d=bbT,Anger,FFFFFF;dp=" + (numberOfUsers + 1) + "|chem=y;s=bubble_text_small;d=bbT,Sadness,FFFFFF;dp=" + (numberOfUsers + 2) + "|chem=y;s=bubble_text_small;d=bbT,Fear,FFFFFF;dp=" + (numberOfUsers + 3) + "|chem=y;s=bubble_text_small;d=bbT,Disgust,FFFFFF;dp=" + (numberOfUsers + 4) + "&chm=R,d10300,0,0.5,1|R,ffd800,0,0,0.5|r,008000,0,1,0.5&chco=000000|0c00fc|5700a3,ffffff&chxt=x,x,y,y&chdl=" + userName + "&chxr=0,-1,1,0.5|1,-1,1|2,-1,1,0.5|3,-1,1&chxl=1:|low%20arousal|high%20arousal|3:|displeasure|pleasure&chxs=0,ff0000|1,ff0000,15|2,0000ff|3,0000ff,15&chd=t:" + finalX + ",75,60,2,85,25|" + finalY + ",95,5,20,15,5";

            List<CardAction> cardButtons = new List<CardAction>()
            {
                new CardAction() { Title = "Yes", Type = ActionTypes.PostBack, Value = "Yes, I want to see our current state." },
            };

            HeroCard heroCard = new HeroCard
            {
                Text = "Do you want to see your current State? ",
                Buttons = cardButtons,
            };

            return heroCard;
        }
    }
}
