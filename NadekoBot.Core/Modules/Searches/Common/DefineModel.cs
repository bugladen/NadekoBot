using System.Collections.Generic;

namespace NadekoBot.Modules.Searches.Common
{
    public class Audio
    {
        public string Url { get; set; }
    }

    public class Example
    {
        public List<Audio> Audio { get; set; }
        public string Text { get; set; }
    }

    public class GramaticalInfo
    {
        public string Type { get; set; }
    }

    public class Sens
    {
        public object Definition { get; set; }
        public List<Example> Examples { get; set; }
        public GramaticalInfo Gramatical_info { get; set; }
    }

    public class Result
    {
        public string Part_of_speech { get; set; }
        public List<Sens> Senses { get; set; }
        public string Url { get; set; }
    }

    public class DefineModel
    {
        public List<Result> Results { get; set; }
    }
}
