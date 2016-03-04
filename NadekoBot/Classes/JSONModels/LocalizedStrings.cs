using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Classes.JSONModels {
    internal class LocalizedStrings {
        public string[] Insults { get; } = {
            " You are a poop.", " You're a jerk.",
            " I will eat you when I get my powers back."
        };

        public string[] Praises = {
            " You are cool.",
            " You are nice!",
            " You did a good job.",
            " You did something nice.",
            " is awesome!",
            " Wow."
        };
    }
}
