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
            Iphone, //4000
            Cat, //2000
            Dog, //2001
            Lollipop, //30
            Purse, //3500
            Sushi, //300
            Icecream, //200
            Piano, //8000
            Yacht, //12000
            Car, //9000
            House, //15000
            Spaceship, //30000
            Beer, //75
            Pizza, //150
            Milk, //125
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
                case ItemName.Iphone:
                    return new WaifuItem("📱", 4000, itemName);
                case ItemName.Cat:
                    return new WaifuItem("🐱", 2000, itemName);
                case ItemName.Dog:
                    return new WaifuItem("🐶", 2001, itemName);
                case ItemName.Lollipop:
                    return new WaifuItem("🍭", 30, itemName);
                case ItemName.Purse:
                    return new WaifuItem("👛", 3500, itemName);
                case ItemName.Sushi:
                    return new WaifuItem("🍣", 300, itemName);
                case ItemName.Icecream:
                    return new WaifuItem("🍦", 200, itemName);
                case ItemName.Piano:
                    return new WaifuItem("🎹", 8000, itemName);
                case ItemName.Yacht:
                    return new WaifuItem("🛳", 12000, itemName);
                case ItemName.Car:
                    return new WaifuItem("🚗", 9000, itemName);
                case ItemName.House:
                    return new WaifuItem("🏠", 15000, itemName);
                case ItemName.Spaceship:
                    return new WaifuItem("🚀", 30000, itemName);
                case ItemName.Beer:
                    return new WaifuItem("🍺", 75, itemName);
                case ItemName.Pizza:
                    return new WaifuItem("🍕", 150, itemName);
                case ItemName.Milk:
                    return new WaifuItem("🥛", 125, itemName);
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
