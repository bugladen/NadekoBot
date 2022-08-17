﻿using System;
using System.Collections.Generic;

namespace NadekoBot.Core.Services.Database.Models
{
    public enum ShopEntryType
    {
        Role,
        List,
        //Infinite_List,
    }

    public class ShopEntry : DbEntity, IIndexed
    {
        public int Index { get; set; }
        public int Price { get; set; }
        public string Name { get; set; }
        public ulong AuthorId { get; set; }

        public ShopEntryType Type { get; set; }

        //role
        public string RoleName { get; set; }
        public ulong RoleId { get; set; }

        //list
        public HashSet<ShopEntryItem> Items { get; set; } = new HashSet<ShopEntryItem>();
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
            Text.GetHashCode(StringComparison.InvariantCulture);
    }
}
