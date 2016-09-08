using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Services.Database.Models
{
    public class BotConfig : DbEntity
    {
        public HashSet<BlacklistItem> Blacklist { get; set; }
        public ulong BufferSize { get; set; } = 4000000;
        public bool DontJoinServers { get; set; } = false;
        public bool ForwardMessages { get; set; } = true;
        public bool ForwardToAllOwners { get; set; } = true;

        public float CurrencyGenerationChance { get; set; } = 0.1f;
        public int CurrencyGenerationCooldown { get; set; } = 10;

        public List<ModulePrefix> ModulePrefixes { get; set; } = new List<ModulePrefix>()
        {
           new ModulePrefix() { ModuleName="Administration", Prefix="." },
           new ModulePrefix() { ModuleName="Searches", Prefix="~" },
           new ModulePrefix() { ModuleName="NSFW", Prefix="~" },
           new ModulePrefix() { ModuleName="ClashOfClans", Prefix="," },
           new ModulePrefix() { ModuleName="Help", Prefix="-" },
           new ModulePrefix() { ModuleName="Music", Prefix="!!" },
           new ModulePrefix() { ModuleName="Trello", Prefix="trello" },
           new ModulePrefix() { ModuleName="Games", Prefix=">" },
           new ModulePrefix() { ModuleName="Gambling", Prefix="$" },
           new ModulePrefix() { ModuleName="Permissions", Prefix=";" },
           new ModulePrefix() { ModuleName="Pokemon", Prefix=">" },
           new ModulePrefix() { ModuleName="Utility", Prefix="." }
        };

        public List<PlayingStatus> RotatingStatusMessages { get; set; } = new List<PlayingStatus>();

        public bool RotatingStatuses { get; set; } = false;
        public string RemindMessageFormat { get; set; } = "❗⏰**I've been told to remind you to '%message%' now by %user%.**⏰❗";


        public string CurrencySign { get; set; } = "🌸";
        public string CurrencyName { get; set; } = "Nadeko Flower";
        public string CurrencyPluralName { get; set; } = "Nadeko Flowers";

        public List<EightBallResponse> EightBallResponses { get; set; } = new List<EightBallResponse>
        {
            new EightBallResponse() { Text = "Most definitely yes" },
            new EightBallResponse() { Text = "For sure" },
            new EightBallResponse() { Text = "Totally!" },
            new EightBallResponse() { Text = "As I see it, yes" },
            new EightBallResponse() { Text = "My sources say yes" },
            new EightBallResponse() { Text = "Yes" },
            new EightBallResponse() { Text = "Most likely" },
            new EightBallResponse() { Text = "Perhaps" },
            new EightBallResponse() { Text = "Maybe" },
            new EightBallResponse() { Text = "Not sure" },
            new EightBallResponse() { Text = "It is uncertain" },
            new EightBallResponse() { Text = "Ask me again later" },
            new EightBallResponse() { Text = "Don't count on it" },
            new EightBallResponse() { Text = "Probably not" },
            new EightBallResponse() { Text = "Very doubtful" },
            new EightBallResponse() { Text = "Most likely no" },
            new EightBallResponse() { Text = "Nope" },
            new EightBallResponse() { Text = "No" },
            new EightBallResponse() { Text = "My sources say no" },
            new EightBallResponse() { Text = "Dont even think about it" },
            new EightBallResponse() { Text = "Definitely no" },
            new EightBallResponse() { Text = "NO - It may cause disease contraction" }
        };

        public List<RaceAnimal> RaceAnimals { get; set; } = new List<RaceAnimal>
        {
            new RaceAnimal { Icon = "🐼", Name = "Panda" },
            new RaceAnimal { Icon = "🐻", Name = "Bear" },
            new RaceAnimal { Icon = "🐧", Name = "Pengu" },
            new RaceAnimal { Icon = "🐨", Name = "Koala" },
            new RaceAnimal { Icon = "🐬", Name = "Dolphin" },
            new RaceAnimal { Icon = "🐞", Name = "Ladybird" },
            new RaceAnimal { Icon = "🦀", Name = "Crab" },
            new RaceAnimal { Icon = "🦄", Name = "Unicorn" }
        };
    }

    public class PlayingStatus :DbEntity
    {
        public string Status { get; set; }
    }

    public class BlacklistItem : DbEntity
    {
        public ulong ItemId { get; set; }
        public enum BlacklistType
        {
            Server,
            Channel,
            User
        }
    }

    public class EightBallResponse : DbEntity
    {
        public string Text { get; set; }
    }

    public class RaceAnimal : DbEntity
    {
        public string Icon { get; set; }
        public string Name { get; set; }
    }
    
    public class ModulePrefix : DbEntity
    {
        public string ModuleName { get; set; }
        public string Prefix { get; set; }
    }
}
