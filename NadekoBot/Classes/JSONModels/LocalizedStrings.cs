using System.IO;

namespace NadekoBot.Classes.JSONModels {
    public class LocalizedStrings {
        public string[] Insults { get; set; } = {
            " You are a poop.", " You're a jerk.",
            " I will eat you when I get my powers back."
        };

        public string[] Praises { get; set; } = {
            " You are cool.",
            " You are nice!",
            " You did a good job.",
            " You did something nice.",
            " is awesome!",
            " Wow."
        };

        public static string[] GetAvailableLocales() {
            Directory.CreateDirectory("data/locales");
            return Directory.GetFiles("data/locales");
        }

        //public static void HandleLocalization() {
        //    var locales = LocalizedStrings.GetAvailableLocales();


        //    Console.WriteLine("Pick a language:\n" +
        //                      "1. English");
        //    for (var i = 0; i < locales.Length; i++) {
        //        Console.WriteLine((i + 2) + ". " + Path.GetFileNameWithoutExtension(locales[i]));
        //    }
        //    File.WriteAllText("data/locales/english.json", JsonConvert.SerializeObject(new LocalizedStrings(), Formatting.Indented));
        //    try {
        //        Console.WriteLine($"Type in a number from {1} to {locales.Length + 1}\n");
        //        var input = Console.ReadLine();
        //        if (input != "1")
        //            Locale = LocalizedStrings.LoadLocale(locales[int.Parse(input) - 2]);
        //    } catch (Exception ex) {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine(ex);
        //        Console.ReadKey();
        //        return;
        //    }
        //}

        public static LocalizedStrings LoadLocale(string localeFile) =>
            Newtonsoft.Json.JsonConvert.DeserializeObject<LocalizedStrings>(File.ReadAllText(localeFile));
    }
}
