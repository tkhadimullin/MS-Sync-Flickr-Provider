using System;
using FlickrNet;
using Microsoft.Synchronization.Files;
using Microsoft.Synchronization.SimpleProviders;

namespace FlickrSyncProvider.Domain
{
    public abstract class FlickrMediaObject
    {
        // Constants define custom fields number in metadata storage
        public const uint photoId = 0;
        public const uint photosetId = 1;
        public const uint photoVirtualPath = 2;
        public const uint photoTitle = 3;
        public const uint photoUrl = 4;
        public const uint lastUpdatedTime = 5;
        public const uint photoMd5 = 6;
        public const uint photoHashCheckTime = 7;
        public const uint firstUploadedTime = 8;
        public const uint photoFormat = 9;
        public string PhotosetId { get; set; }
        public string VirtualPath { get; set; }
        public string Title { get; set; }
        public DateTime LastUpdatedTime { get; set; }
        public DateTime FirstUploadedTime { get; set; }

        public abstract bool IsDirectory { get; }
        public abstract string GetRelativeFileName();

        /// <summary>
        /// Returns schema defining our custom fields for <see cref="FlickrMediaObject"/>
        /// </summary>
        /// <returns></returns>
        public static ItemMetadataSchema GetSchema()
        {
            // returning a schema for our custom fields: fields descriptions and key fields
            var schema = new ItemMetadataSchema(
                new[] { 
                    new CustomFieldDefinition(photoId, typeof(string), 24), 
                    new CustomFieldDefinition(photosetId, typeof(string), 24), 
                    new CustomFieldDefinition(photoVirtualPath, typeof(string), 300), 
                    new CustomFieldDefinition(photoTitle, typeof(string), 100), 
                    new CustomFieldDefinition(photoUrl, typeof(string), 200), 
                    new CustomFieldDefinition(lastUpdatedTime, typeof(UInt64), 100), 
                    new CustomFieldDefinition(photoMd5, typeof(string), 32), 
                    new CustomFieldDefinition(photoHashCheckTime, typeof(UInt64), 100),
                    new CustomFieldDefinition(firstUploadedTime, typeof(UInt64), 100),
                    new CustomFieldDefinition(photoFormat, typeof(string), 10)
                },
                new[] { new IdentityRule(new[] { photoId, photosetId }) });

            return schema;
        }

        public abstract ItemFieldDictionary ToItemFieldDictionary();
        public abstract bool HasBeenUpdatedOnRemote(Flickr flickr);

        public abstract void DeleteOnRemote(Flickr flickr);
        public abstract ItemFieldDictionary UpdateRemote(IFileDataRetriever newFileData, Flickr flickr, PhotosetTree photosetTree);
    }
}
