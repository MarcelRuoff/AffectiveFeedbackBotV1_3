using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBotWithCounter
{
    public class User
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public int X { get; set; } = 50;

        public int Y { get; set; } = 50;

        public User(string id)
        {
            UserId = id;
        }

        public User()
        {
        }
    }
}
