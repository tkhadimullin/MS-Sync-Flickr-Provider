using System;
using System.Diagnostics.CodeAnalysis;
using FlickrNet;
using FlickrSyncProvider.Extensions;
using Microsoft.Synchronization.Files;
using Microsoft.Synchronization.SimpleProviders;

namespace FlickrSyncProvider.Domain
{
    public class FlickrDirectory : FlickrMediaObject
    {
        public override bool IsDirectory
        {
            get { return true; }
        }

        public FlickrDirectory(ItemFieldDictionary itm)
        {
            PhotosetId = (string)itm[FlickrMediaObject.photosetId].Value;
            VirtualPath = (string)itm[FlickrMediaObject.photoVirtualPath].Value;
            Title = (string)itm[FlickrMediaObject.photoTitle].Value;
            LastUpdatedTime = ((UInt64)itm[FlickrMediaObject.lastUpdatedTime].Value).UnixTimeStampToDateTime();
            FirstUploadedTime = ((UInt64)itm[FlickrMediaObject.firstUploadedTime].Value).UnixTimeStampToDateTime();
        }

        public FlickrDirectory()
        {
        }

        public FlickrDirectory(Photoset photoset, PhotosetTree photosetTree)
        {
            PhotosetId = photoset.PhotosetId;
            VirtualPath = photosetTree.ResolvePathByPhotoset(photoset.PhotosetId);
            Title = photoset.Title;
            LastUpdatedTime = photoset.DateUpdated;
            FirstUploadedTime = photoset.DateCreated == new DateTime(1, 1, 1) ? new DateTime(1970, 01, 01) : photoset.DateCreated;
        }

        public override string GetRelativeFileName()
        {
            return string.Format("{0}\\{1}", VirtualPath.TrimEnd('\\'), Title);
        }

        public override ItemFieldDictionary ToItemFieldDictionary()
        {
            var item = new ItemFieldDictionary();
            item.Add(new ItemField(photosetId, typeof(string), PhotosetId));
            item.Add(new ItemField(photoVirtualPath, typeof(string), VirtualPath));
            item.Add(new ItemField(photoTitle, typeof(string), Title));
            item.Add(new ItemField(lastUpdatedTime, typeof(UInt64), (UInt64)LastUpdatedTime.ToUnixTimestamp()));
            item.Add(new ItemField(firstUploadedTime, typeof(UInt64), (UInt64)FirstUploadedTime.ToUnixTimestamp()));
            return item;
        }

        [ExcludeFromCodeCoverage]
        public override bool HasBeenUpdatedOnRemote(Flickr flickr)
        {
            var info = flickr.PhotosetsGetInfo(PhotosetId);
            return (info.DateUpdated.ToUniversalTime() != LastUpdatedTime.ToUniversalTime());
        }

        [ExcludeFromCodeCoverage]
        public override void DeleteOnRemote(Flickr flickr)
        {
            flickr.PhotosetsDelete(PhotosetId);
        }

        public override ItemFieldDictionary UpdateRemote(IFileDataRetriever newFileData, Flickr flickr, PhotosetTree photosetTree)
        {
            //directory has changed. means we'll have to either raname or reposition a photoset in hierarchy
            if (String.Compare(Title, newFileData.FileData.Name, StringComparison.CurrentCultureIgnoreCase) != 0) // rename it is
            {
                flickr.PhotosetsEditMeta(PhotosetId, newFileData.FileData.Name, "");// TODO: preserve description
                
                var newPhotoset = flickr.PhotosetsGetInfo(PhotosetId);                
                return new FlickrDirectory(newPhotoset, photosetTree).ToItemFieldDictionary();
            }
            // nothing has changed as far as we're concerned
            return ToItemFieldDictionary();
        }
    }
}