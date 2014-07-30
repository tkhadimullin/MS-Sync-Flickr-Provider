using FlickrSyncProvider.Domain;
using FlickrSyncProvider.Extensions;
using Microsoft.Synchronization.SimpleProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlickrSyncProvider.Test
{
    [TestClass]
    public class FlickrDirectoryTest
    {
        ItemFieldDictionary _itemDictionary;

        [TestInitialize]
        public void Init()
        {            
            _itemDictionary = new ItemFieldDictionary
                {
                    new ItemField(FlickrMediaObject.photosetId, typeof (string), "11"),
                    new ItemField(FlickrMediaObject.photoVirtualPath, typeof (string), "Test\test1"),
                    new ItemField(FlickrMediaObject.photoTitle, typeof (string), "Title"),
                    new ItemField(FlickrMediaObject.lastUpdatedTime, typeof (ulong), (ulong)100),
                    new ItemField(FlickrMediaObject.firstUploadedTime, typeof (ulong), (ulong)100),
                };
        }

        [TestMethod]
        public void FlickrDirectory_Constructors()
        {
            var empty = new FlickrDirectory();
            Assert.IsInstanceOfType(empty, typeof(FlickrDirectory));

            var fromDictionary = new FlickrDirectory(_itemDictionary);
            Assert.AreEqual("11", fromDictionary.PhotosetId);
            Assert.AreEqual("Test\test1", fromDictionary.VirtualPath);
            Assert.AreEqual("Title", fromDictionary.Title);
            Assert.AreEqual(((ulong)100).UnixTimeStampToDateTime(), fromDictionary.LastUpdatedTime);
            Assert.AreEqual(((ulong)100).UnixTimeStampToDateTime(), fromDictionary.FirstUploadedTime);
        }

        [TestMethod]
        public void FlickrDirectory_GetRelativeName()
        {
            var dummy = new FlickrDirectory()
                {
                    Title = "directory",
                    VirtualPath = @"root\level_one"
                };
            
            Assert.AreEqual(@"root\level_one\directory", dummy.GetRelativeFileName());
        }

        [TestMethod]
        public void FlickrDirectory_ToItemFieldDictionary()
        {
            var fromDictionary = new FlickrDirectory(_itemDictionary);
            var dictionary = fromDictionary.ToItemFieldDictionary();
            var newDirectory = new FlickrDirectory(dictionary);
            Assert.AreEqual(fromDictionary.PhotosetId, newDirectory.PhotosetId);
            Assert.AreEqual(fromDictionary.VirtualPath, newDirectory.VirtualPath);
            Assert.AreEqual(fromDictionary.Title, newDirectory.Title);
        }

        
    }
}
