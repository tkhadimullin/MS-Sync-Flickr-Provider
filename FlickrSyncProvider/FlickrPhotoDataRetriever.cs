using System;
using System.IO;
using System.Net;
using FlickrSyncProvider.Domain;
using Microsoft.Synchronization.Files;

namespace FlickrSyncProvider
{
    public class FlickrPhotoDataRetriever : IFileDataRetriever
    {
        private readonly FlickrMediaObject _item;

        public FlickrPhotoDataRetriever(FlickrMediaObject mediaItem)
        {
            _item = mediaItem;
        }

        public string RelativeDirectoryPath { get { return _item.VirtualPath; }}
        public string AbsoluteSourceFilePath { get { throw new NotImplementedException(); }}

        public FileData FileData
        {
            get
            {
                return new FileData(
                    _item.GetRelativeFileName(),//For the relative path on FileData, provide relative path including file name
                    _item.IsDirectory ? FileAttributes.Directory : FileAttributes.Normal,
                    _item.FirstUploadedTime,
                    _item.LastUpdatedTime,
                    _item.LastUpdatedTime,
                    Size);
            }
        }

        private long? _size;
        private long Size 
        {            
            get
            {
                if (_item.IsDirectory)
                    return 0;
                if (!_size.HasValue)
                {
                    var req = WebRequest.Create((_item as FlickrFile).PhotoUrl);
                    req.Method = "HEAD";
                    var response = req.GetResponse();
                    _size = response.ContentLength;
                }
                return (long) _size;
            }            
        }

        public Stream FileStream
        {
            get
            {
                if (_item.IsDirectory)
                    throw new NotSupportedException("Filestream is not supported for directories");
                var sourceStream = new MemoryStream();
                var req = WebRequest.Create((_item as FlickrFile).PhotoUrl);                    
                var response = req.GetResponse();
                using (var stream = response.GetResponseStream())
                {                    
                    if (stream != null) stream.CopyTo(sourceStream);
                }
                return sourceStream;
            }
        }
    }
}