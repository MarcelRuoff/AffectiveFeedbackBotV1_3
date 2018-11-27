using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBotWithCounter
{
    public class Conversation
    {
        private string id;
        private string text;
        private string postResult;

        public Conversation(string id, string text, string postResult)
        {
            this.id = id;
            this.text = text;
            this.postResult = postResult;
        }
    }
}
