using System;

namespace NadekoBot.Core.Services.Database.Models
{
    public class WaifuItem : DbEntity
    {
        public string ItemEmoji { get; set; }
        public int Price { get; set; }
        public ItemName Item { get; set; }

        public enum ItemName
        {
            Cookie,
            Rose,
            LoveLetter,
            Chocolate,
            Rice,
            MovieTicket,
            Book,
            Lipstick,
            Laptop,
            Violin,
            Ring,
            Helicopter,
        }

        public WaifuItem()
        {

        }

        public WaifuItem(string itemEmoji, int price, ItemName item)
        {
            ItemEmoji = itemEmoji;
            Price = price;
            Item = item;
        }

        public static WaifuItem GetItem(ItemName itemName)
        {
            switch (itemName)
            {
                case ItemName.Cookie:
                    return new WaifuItem("🍪", 10, itemName);
                case ItemName.Rose:
                    return new WaifuItem("🌹", 50, itemName);
                case ItemName.LoveLetter:
                    return new WaifuItem("💌", 100, itemName);
                case ItemName.Chocolate:
                    return new WaifuItem("🍫", 200, itemName);
                case ItemName.Rice:
                    return new WaifuItem("🍚", 400, itemName);
                case ItemName.MovieTicket:
                    return new WaifuItem("🎟", 800, itemName);
                case ItemName.Book:
                    return new WaifuItem("📔", 1500, itemName);
                case ItemName.Lipstick:
                    return new WaifuItem("💄", 3000, itemName);
                case ItemName.Laptop:
                    return new WaifuItem("💻", 5000, itemName);
                case ItemName.Violin:
                    return new WaifuItem("🎻", 7500, itemName);
                case ItemName.Ring:
                    return new WaifuItem("💍", 10000, itemName);
                case ItemName.Helicopter:
                    return new WaifuItem("🚁", 20000, itemName);
                default:
                    throw new ArgumentException(nameof(itemName));
            }
        }
    }
}


/*
🍪 Cookie 10
🌹  Rose 50
💌 Love Letter 100
🍫  Chocolate 200
🍚 Rice 400
🎟  Movie Ticket 800
📔 Book 1.5k
💄  Lipstick 3k
💻 Laptop 5k
🎻 Violin 7.5k
💍 Ring 10k
*/
