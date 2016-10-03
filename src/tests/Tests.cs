using NadekoBot.Modules.Permissions;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
    public class Tests
    {

        private Permission GetRoot()
        {
            Permission root = new Permission();
            root.SecondaryTargetName = "Root";
            var cur = root;
            for (var i = 1; i < 10; i++)
            {
                var p = new Permission();
                p.SecondaryTargetName = i.ToString();
                p.Previous = cur;
                cur.Next = p;
                cur = p;
            }
            return root;
        }
        [Fact]
        public void CountTest() 
        {
            var root = GetRoot();

            Assert.Equal(10, root.Count());
        }

        [Fact]
        public void AddTest()
        {
            var root = GetRoot();

            root.Prepend(new Permission() { SecondaryTargetName = "Added" });

            root = root.GetRoot();

            Assert.Equal(11, root.Count());

            Assert.Equal("Added", root.AsEnumerable().First().SecondaryTargetName);
        }

        [Fact]
        public void GetAtTest()
        {
            var root = GetRoot();
            Assert.Equal("Root", root.GetAt(0).SecondaryTargetName);
            Assert.Equal("1", root.GetAt(1).SecondaryTargetName);
            Assert.Equal("5", root.GetAt(5).SecondaryTargetName);
            Assert.Equal("9", root.GetAt(9).SecondaryTargetName);

            Assert.Throws(typeof(IndexOutOfRangeException), () => { root.GetAt(-5); });
            Assert.Throws(typeof(IndexOutOfRangeException), () => { root.GetAt(10); });
        }

        [Fact]
        public void InsertTest() {

            var root = GetRoot();

            root.Insert(5, new Permission() { SecondaryTargetName = "in2" });

            Assert.Equal(11, root.Count());
            Assert.Equal("in2", root.GetAt(5).SecondaryTargetName);

            root.Insert(0, new Permission() { SecondaryTargetName = "Inserted" });

            root = root.Previous;
            Assert.Equal("Inserted", root.SecondaryTargetName);
            Assert.Equal(12, root.Count());
            Assert.Equal("Root", root.GetAt(1).SecondaryTargetName);

            Assert.Throws(typeof(IndexOutOfRangeException), () => { root.GetAt(12); });
        }

        [Fact]
        public void RemoveAtTest()
        {
            var root = GetRoot();

            var removed = root.RemoveAt(3);

            Assert.Equal("3", removed.SecondaryTargetName);
            Assert.Equal(9, root.Count());
            Assert.Throws(typeof(IndexOutOfRangeException), () => { root.RemoveAt(0); });
            Assert.Throws(typeof(IndexOutOfRangeException), () => { root.RemoveAt(9); });
            Assert.Throws(typeof(IndexOutOfRangeException), () => { root.RemoveAt(-1); });
        }

        [Fact]
        public void TestGetRoot()
        {
            var root = GetRoot();

            var random = root.GetAt(5).GetRoot();

            Assert.Equal("Root", random.SecondaryTargetName);
        }
    }
}
