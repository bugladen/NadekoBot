using System.Collections.Generic;

namespace NadekoBot.Services.Database.Models
{
    public enum ShopEntryType
    {
        Role,
        List,
        Infinite_List,
    }

    public class ShopEntry : DbEntity, IIndexed
    {
        public int Index { get; set; }
        public int Price { get; set; }
        public string Name { get; set; }
        public ulong AuthorId { get; set; }

        public ShopEntryType Type { get; set; }
        public string RoleName { get; set; }
        public ulong RoleId { get; set; }
        public List<ShopEntryItem> Items { get; set; }
    }

    public class ShopEntryItem : DbEntity
    {
        public string Text { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return ((ShopEntryItem)obj).Text == Text;
        }

        public override int GetHashCode() =>
            Text.GetHashCode();
    }
}
