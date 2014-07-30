using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FlickrNet;

namespace FlickrSyncProvider
{
    public class PhotosetTree
    {
        private readonly Dictionary<Photoset, string> _mapping = new Dictionary<Photoset, string>(); // photoset:virtualPath
        private readonly Flickr _client;
        
        private CollectionCollection Collections { get; set; }

        public PhotosetTree(Flickr client)
        {
            _client = client;
        }
        
        public Photoset ResolvePhotosetByPath(string path)
        {
            return _mapping.FirstOrDefault(x => x.Value == path).Key;
        }

        public string ResolvePathByPhotoset(string photoSetId)
        {
            return _mapping.FirstOrDefault(x => x.Key.PhotosetId == photoSetId).Value;
        }

        public string ResolvePathByPhotoset(Photoset photoSet)
        {
            return _mapping.FirstOrDefault(x => x.Key.PhotosetId == photoSet.PhotosetId).Value;
        }

        public string ResolvePathByPhotoset(ContextSet contextSet)
        {
            return _mapping.FirstOrDefault(x => x.Key.PhotosetId == contextSet.PhotosetId).Value;
        }

        private bool ScanCollection(string pathSoFar, Collection collection, Photoset photoSet, out string setPath, bool isRoot = false)
        {
            setPath = null;
            pathSoFar = pathSoFar + @"\" + collection.Title;
            var collectionsToScan = new List<Collection>();
            if (isRoot)
                // since FlickrNet treats the root collection slightly differently we'll have to fake the root Collection for consistency
            {
                if (Collections != null )
                    collectionsToScan = Collections.ToList();   
            }                
            else
            {
                if (collection.Sets.Any(x => x.SetId == photoSet.PhotosetId))
                {
                    setPath = pathSoFar + @"\" + photoSet.Title;
                    return true;
                }
                collectionsToScan = collection.Collections.ToList();
            }            
            foreach (var col in collectionsToScan.AsParallel())
            {
                if (ScanCollection(pathSoFar, col, photoSet, out setPath))
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateCollectionTree(IEnumerable<Photoset> photosets)
        {
            Collections = _client.CollectionsGetTree();
            _mapping.Clear();
            foreach (var photoset in photosets)
            {
                _mapping.Add(photoset, ResolvePhotosetMapping(photoset));
            }
        }

        public string ResolvePhotosetMapping(Photoset photoSet)
        {
            string res;

            var root = new Collection { Title = @"" };// since FlickrNet treats the root collection slightly differently we'll hace to fake the root Collection for consistency

            if (!ScanCollection("", root, photoSet, out res, true))
                res = @"\" + photoSet.Title; // if couldn't find the photoset in collections - place it in the root of our virtual tree

            var virtualPath = res.TrimStart(new[] { '\\' });

            return virtualPath;
        }

        public Photoset EnsurePhotosetExists(string relativeDirectoryPath, Flickr flickr)
        {
            // first check if we already have the photoset
            var photoset = ResolvePhotosetByPath(relativeDirectoryPath);
            if (photoset != null)
                return photoset;
            // if not - Ensure its parent exists
            var m = Regex.Match(relativeDirectoryPath, "^(.*)\\([^\\]+)\\?$");
            if (m.Success)
            {
                var path = m.Groups[0].Value.Trim('\\');
                var directory = m.Groups[1].Value.Trim('\\');
                if(!string.IsNullOrWhiteSpace(path))
                {
                    var parentPhotoset = EnsurePhotosetExists(path, flickr);
                }
                if (!string.IsNullOrWhiteSpace(directory))
                    return flickr.PhotosetsCreate(directory, string.Empty, string.Empty);               
            }
            throw new Exception(string.Format("Regex failed on {0}", relativeDirectoryPath));
        }
    }
}