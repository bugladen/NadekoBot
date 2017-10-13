namespace NadekoBot.Core.Services.Database.Models
{
    public class LoadedPackage : DbEntity
    {
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is LoadedPackage p
                ? p.Name == Name
                : false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
