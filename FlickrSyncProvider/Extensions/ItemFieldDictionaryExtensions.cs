using FlickrNet;
using FlickrSyncProvider.Domain;
using Microsoft.Synchronization.SimpleProviders;

namespace FlickrSyncProvider.Extensions
{
    public static class ItemFieldDictionaryExtensions
    {
        public static FlickrMediaObject ToFlickrMediaObject(this ItemFieldDictionary itm)
        {
            var isDirectory = !itm.ContainsKey(FlickrMediaObject.photoId) ||
                              string.IsNullOrWhiteSpace((string) itm[FlickrMediaObject.photoId].Value);

            var result = isDirectory ? (FlickrMediaObject)new FlickrDirectory(itm) : new FlickrFile(itm);

            return result;
        }

        public static ItemFieldDictionary ToItemFieldDictionary(this Photoset photoset, PhotosetTree photosetTree)
        {
            return new FlickrDirectory(photoset, photosetTree).ToItemFieldDictionary();
        }

        /// <summary>
        /// Does not handle OriginalUrl!
        /// </summary>
        public static Photo AsPhoto(this PhotoInfo photoInfo)
        {
            var photo = new Photo();
            photo.PhotoId = photoInfo.PhotoId;
            photo.Title = photoInfo.Title;
            photo.DateUploaded = photoInfo.DateUploaded;
            photo.LastUpdated = photoInfo.DateLastUpdated;
            photo.OriginalFormat = photoInfo.OriginalFormat;
            photo.MachineTags = string.Empty;
            if (photoInfo.Tags.Count != 0)
            {
                var tags = MachineTag.ParseTags(photoInfo.Tags);
                photo.MachineTags = MachineTag.AsString(tags);
            }
            
            return photo;
        }
    }
}