using System;
using System.IO;
using FlickrNet;
using FlickrSyncProvider.Domain;
using FlickrSyncProvider.Extensions;
using Microsoft.Synchronization.Files;
using Microsoft.Synchronization.SimpleProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FlickrSyncProvider.Test
{
    [TestClass]
    public class FlickrFileTest
    {
        ItemFieldDictionary _itemDictionary;

        [TestInitialize]
        public void Init()
        {
            _itemDictionary = new ItemFieldDictionary
                {
                    new ItemField(FlickrMediaObject.photoId, typeof(string), "11"),
                    new ItemField(FlickrMediaObject.photosetId, typeof (string), "11"),
                    new ItemField(FlickrMediaObject.photoVirtualPath, typeof (string), "Test\test1"),
                    new ItemField(FlickrMediaObject.photoTitle, typeof (string), "Title"),
                    new ItemField(FlickrMediaObject.lastUpdatedTime, typeof (ulong), (ulong)100),
                    new ItemField(FlickrMediaObject.firstUploadedTime, typeof (ulong), (ulong)100),
                    new ItemField(FlickrMediaObject.photoUrl, typeof (string), "http://example.com/1.jpg"),
                    new ItemField(FlickrMediaObject.photoMd5, typeof (string), "aabbccdd"),
                    new ItemField(FlickrMediaObject.photoHashCheckTime, typeof (ulong), (ulong)100),
                    new ItemField(FlickrMediaObject.photoFormat, typeof (string), "jpg"),
                };

        }

        [TestMethod]
        public void FlickrFile_Constructors()
        {
            var empty = new FlickrFile();
            Assert.IsInstanceOfType(empty, typeof(FlickrFile));

            var fromDictionary = new FlickrFile(_itemDictionary);
            Assert.AreEqual("11", fromDictionary.PhotosetId);
            Assert.AreEqual("Test\test1", fromDictionary.VirtualPath);
            Assert.AreEqual("Title", fromDictionary.Title);
            Assert.AreEqual("11", fromDictionary.PhotoId);
            Assert.AreEqual("jpg", fromDictionary.PhotoFormat);
            Assert.AreEqual("aabbccdd", fromDictionary.Md5Hash);
            Assert.AreEqual("http://example.com/1.jpg", fromDictionary.PhotoUrl);
            Assert.AreEqual(((ulong)100).UnixTimeStampToDateTime(), fromDictionary.PhotoHashCheckTime);
            Assert.AreEqual(((ulong)100).UnixTimeStampToDateTime(), fromDictionary.LastUpdatedTime);
            Assert.AreEqual(((ulong)100).UnixTimeStampToDateTime(), fromDictionary.FirstUploadedTime);
        }

        [TestMethod]
        public void FlickrFile_Constructor2()
        {
            var p = new Photo
                {
                    PhotoId = "11",
                    Title = "title1",
                    DateUploaded = new DateTime(1, 1, 1),
                    LastUpdated = new DateTime(2, 2, 2),
                    OriginalFormat = "jpg",
                    MachineTags = "file:md5sum=abcd1234 flickrsync:checktime=1"
                };
            var flickrFile = new FlickrFile(p, "1", "test\\test1");
            Assert.IsNotNull(flickrFile);
            Assert.AreEqual("11",flickrFile.PhotoId);
            Assert.AreEqual("1", flickrFile.PhotosetId);
            Assert.AreEqual("test\\test1", flickrFile.VirtualPath);
            Assert.AreEqual("abcd1234", flickrFile.Md5Hash);
            Assert.AreEqual(1, flickrFile.PhotoHashCheckTime.ToUnixTimestamp());
        }

        [TestMethod]
        public void FlickrFile_GenerateMachineTags()
        {
            var memStream = new MemoryStream(new byte[] {1, 2, 3});
            var mockFileRetriever = new Mock<IFileDataRetriever>(MockBehavior.Loose);
            mockFileRetriever.Setup(x => x.FileStream).Returns(memStream);

            var tags = FlickrFile.GenerateMachineTags(mockFileRetriever.Object);
            Assert.IsNotNull(tags);
            Assert.IsTrue(tags.StartsWith("file:md5sum=5289df737df57326fcdd22597afb1fac flickrsync:checktime="));
        }

        [TestMethod]
        public void FlickrFile_GetRelativeFileName()
        {
            var fromDictionary = new FlickrFile(_itemDictionary);
            var path = fromDictionary.GetRelativeFileName();
            Assert.AreEqual("Test\test1\\Title.jpg", path);
        }

        [TestMethod]
        public void FlickrFile_ToItemFieldDictionary()
        {
            var fromDictionary = new FlickrFile(_itemDictionary);
            var dictionary = fromDictionary.ToItemFieldDictionary();
            var newDirectory = new FlickrDirectory(dictionary);
            Assert.AreEqual(fromDictionary.PhotosetId, newDirectory.PhotosetId);
            Assert.AreEqual(fromDictionary.VirtualPath, newDirectory.VirtualPath);
            Assert.AreEqual(fromDictionary.Title, newDirectory.Title);
        }
    }
}
