using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FlickrNet;
using FlickrSyncProvider.Extensions;
using Microsoft.Synchronization.Files;
using Microsoft.Synchronization.SimpleProviders;

namespace FlickrSyncProvider.Domain
{
    public class FlickrFile : FlickrMediaObject
    {
        public string PhotoId { get; set; }
        public string PhotoUrl { get; set; }
        public string Md5Hash { get; set; }
        public DateTime PhotoHashCheckTime { get; set; }
        public string PhotoFormat { get; set; }
        
        public override bool IsDirectory { get { return false; } }
        
        public FlickrFile(Photo photo, string photosetId, string virtualPath)
        {
            PhotoUrl = photo.OriginalUrl;

            PhotoId = photo.PhotoId;
            PhotosetId = string.IsNullOrWhiteSpace(photosetId) ? "0" : photosetId;
            VirtualPath = virtualPath;
            Title = photo.Title;            
            PhotoHashCheckTime = new DateTime(1970, 01, 01);
            FirstUploadedTime = photo.DateUploaded == new DateTime(1, 1, 1) ? PhotoHashCheckTime : photo.DateUploaded;
            LastUpdatedTime = photo.LastUpdated;
            PhotoFormat = photo.OriginalFormat;            
            Md5Hash = string.Empty;            
            if (string.IsNullOrWhiteSpace(photo.MachineTags)) return;
            var tags = MachineTag.ParseMachineTags(photo.MachineTags);
            Md5Hash = tags.Where(x => x.Ns == "file" && x.Predicate == "md5sum")
                          .Select(x => x.Value)
                          .FirstOrDefault() ?? string.Empty;
            PhotoHashCheckTime = tags.Where(x => x.Ns == "flickrsync" && x.Predicate == "checktime")
                          .Select(x => x.Value.UnixTimeStampToDateTime())
                          .FirstOrDefault() ?? new DateTime(1970, 01, 01);
        }

        public FlickrFile(PhotoInfo photo, string photosetId, string virtualPath) : this(photo.AsPhoto(), photosetId, virtualPath)
        {
            PhotoUrl = photo.OriginalUrl;
        }

        public FlickrFile(ItemFieldDictionary itm)
        {
            PhotoId = (string)itm[FlickrMediaObject.photoId].Value;
            PhotoUrl = (string)itm[FlickrMediaObject.photoUrl].Value;
            Md5Hash = (string)itm[FlickrMediaObject.photoMd5].Value;
            PhotoHashCheckTime = ((UInt64)itm[FlickrMediaObject.photoHashCheckTime].Value).UnixTimeStampToDateTime();
            PhotoFormat = (string)itm[FlickrMediaObject.photoFormat].Value;
            PhotosetId = (string)itm[FlickrMediaObject.photosetId].Value;
            VirtualPath = (string)itm[FlickrMediaObject.photoVirtualPath].Value;
            Title = (string)itm[FlickrMediaObject.photoTitle].Value;
            LastUpdatedTime = ((UInt64)itm[FlickrMediaObject.lastUpdatedTime].Value).UnixTimeStampToDateTime();
            FirstUploadedTime = ((UInt64)itm[FlickrMediaObject.firstUploadedTime].Value).UnixTimeStampToDateTime();
        }

        public FlickrFile()
        {
        }

        public static string GenerateMachineTags(IFileDataRetriever fileData)
        {
            var tags = new List<MachineTag>
                {
                    new MachineTag("file", "md5sum", GetMd5Hash(fileData.FileStream)),
                    new MachineTag("flickrsync", "checktime", DateTime.UtcNow.ToUnixTimestamp().ToString(CultureInfo.InvariantCulture))
                };
            return MachineTag.AsString(tags);
        }

        protected static string GetMd5Hash(Stream data)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = data)
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        public override string GetRelativeFileName()
        {
            return string.Format("{0}\\{1}.{2}", VirtualPath.TrimEnd('\\'), Title, PhotoFormat);
        }

        public override ItemFieldDictionary ToItemFieldDictionary()
        {
            var item = new ItemFieldDictionary();
            item.Add(new ItemField(photoId, typeof(string), PhotoId));
            item.Add(new ItemField(photosetId, typeof(string), PhotosetId));
            item.Add(new ItemField(photoVirtualPath, typeof(string), VirtualPath));
            item.Add(new ItemField(photoTitle, typeof(string), Title));
            item.Add(new ItemField(photoUrl, typeof(string), PhotoUrl));
            item.Add(new ItemField(lastUpdatedTime, typeof(UInt64), (UInt64)LastUpdatedTime.ToUnixTimestamp()));
            item.Add(new ItemField(photoMd5, typeof(string), Md5Hash));
            item.Add(new ItemField(photoHashCheckTime, typeof(UInt64), (UInt64)PhotoHashCheckTime.ToUnixTimestamp()));
            item.Add(new ItemField(firstUploadedTime, typeof(UInt64), (UInt64)FirstUploadedTime.ToUnixTimestamp()));
            item.Add(new ItemField(photoFormat, typeof(string), PhotoFormat));
            return item;
        }

        [ExcludeFromCodeCoverage]
        public override bool HasBeenUpdatedOnRemote(Flickr flickr)
        {
            var info = flickr.PhotosGetInfo(PhotoId);
            return (info.DateLastUpdated.ToUniversalTime() != LastUpdatedTime.ToUniversalTime());
        }

        [ExcludeFromCodeCoverage]
        public override void DeleteOnRemote(Flickr flickr)
        {
            flickr.PhotosDelete(PhotoId);
        }

        public override ItemFieldDictionary UpdateRemote(IFileDataRetriever newFileData, Flickr flickr, PhotosetTree photosetTree)
        {
            var newPhotosetId = PhotosetId;
            // check if file has just been moved                                    
            if (String.Compare(VirtualPath, newFileData.RelativeDirectoryPath, StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                newPhotosetId = (photosetTree.ResolvePhotosetByPath(newFileData.RelativeDirectoryPath) ?? new Photoset { PhotosetId = "0" }).PhotosetId;
                flickr.PhotosetsRemovePhoto(PhotosetId, PhotoId);
                if (newPhotosetId != "0")
                    flickr.PhotosetsAddPhoto(newPhotosetId, PhotoId);
            }
            // or the file has been renamed. we'll change the title
            else if ( String.Compare(Title, Path.GetFileNameWithoutExtension(newFileData.FileData.Name), StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                flickr.PhotosSetMeta(PhotoId, Path.GetFileNameWithoutExtension(newFileData.FileData.Name), "");// TODO: preserve description
                flickr.PhotosSetTags(PhotoId, GenerateMachineTags(newFileData));
            }
            else // in the end it looks like we'll have to reupload it
            {
                var photoId = flickr.ReplacePicture(newFileData.AbsoluteSourceFilePath,
                                                     PhotoId);
                flickr.PhotosSetTags(photoId, GenerateMachineTags(newFileData));
            }

            // report resulting photo back
            var photo = flickr.PhotosGetInfo(PhotoId);
            return new FlickrFile(photo, newPhotosetId, newFileData.RelativeDirectoryPath).ToItemFieldDictionary();
        }
    }
}
