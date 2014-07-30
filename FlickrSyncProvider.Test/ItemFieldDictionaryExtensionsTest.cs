using System;
using System.Collections.ObjectModel;
using FlickrNet;
using FlickrSyncProvider.Domain;
using FlickrSyncProvider.Extensions;
using Microsoft.Synchronization.SimpleProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlickrSyncProvider.Test
{
    [TestClass]
    public class ItemFieldDictionaryExtensionsTest
    {
        ItemFieldDictionary _flickrFileDictionary, _flickrDirectoryDictionary;

        [TestInitialize]
        public void Init()
        {
            _flickrFileDictionary = new ItemFieldDictionary
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
            _flickrDirectoryDictionary = new ItemFieldDictionary
                {
                    new ItemField(FlickrMediaObject.photosetId, typeof (string), "11"),
                    new ItemField(FlickrMediaObject.photoVirtualPath, typeof (string), "Test\test1"),
                    new ItemField(FlickrMediaObject.photoTitle, typeof (string), "Title"),
                    new ItemField(FlickrMediaObject.lastUpdatedTime, typeof (ulong), (ulong)100),
                    new ItemField(FlickrMediaObject.firstUploadedTime, typeof (ulong), (ulong)100),
                };
        }

        [TestMethod]
        public void ItemFieldDictionaryExtensions_ToFlickrMediaObject()
        {
            var dir = _flickrDirectoryDictionary.ToFlickrMediaObject();
            var file = _flickrFileDictionary.ToFlickrMediaObject();

            Assert.IsInstanceOfType(dir, typeof(FlickrDirectory));
            Assert.IsInstanceOfType(file, typeof(FlickrFile));
        }

        [TestMethod]
        public void ItemFieldDictionaryExtensions_AsPhoto()
        {
            var photoInfo = new PhotoInfo
                {
                    PhotoId = "1",
                    Title = "Title",
                    DateUploaded = new DateTime(1, 1, 1),
                    DateLastUpdated = new DateTime(2, 2, 2),
                    OriginalFormat = "jpg",
                    Tags = new Collection<PhotoInfoTag>
                        {
                            new PhotoInfoTag {Raw = "ns:pr=val", IsMachineTag = true}
                        }
                };
            var photo = photoInfo.AsPhoto();
            Assert.AreEqual("1", photo.PhotoId);
            Assert.AreEqual(new DateTime(1, 1, 1), photo.DateUploaded);
            Assert.AreEqual(new DateTime(2, 2, 2), photo.LastUpdated);
            Assert.AreEqual("jpg", photo.OriginalFormat);
            Assert.AreEqual("ns:pr=val", photo.MachineTags);
        }
    }
}
