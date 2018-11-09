// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Stores counter state for the conversation.
    /// Stored in <see cref="Microsoft.Bot.Builder.ConversationState"/> and
    /// backed by <see cref="Microsoft.Bot.Builder.MemoryStorage"/>.
    /// </summary>
    public class CounterState
    {
        /// <summary>
        /// Gets or sets the number of turns in the conversation.
        /// </summary>
        /// <value>The number of turns in the conversation.</value>
        public int TurnCount { get; set; } = 0;

        public string FeedbackType { get; set; } = "emoji";

        public string Joy { get; set; } = string.Empty;

        public string Anger { get; set; } = string.Empty;

        public string Sadness { get; set; } = string.Empty;

        public string Fear { get; set; } = string.Empty;

        public string X { get; set; } = string.Empty;

        public string Y { get; set; } = string.Empty;

        public List<int>Radius { get; set; } = new List<int>();
    }
}
