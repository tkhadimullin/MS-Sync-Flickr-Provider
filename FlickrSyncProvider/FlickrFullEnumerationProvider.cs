using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FlickrNet;
using FlickrSyncProvider.Domain;
using FlickrSyncProvider.Extensions;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;
using Microsoft.Synchronization.MetadataStorage;
using Microsoft.Synchronization.SimpleProviders;
using NLog;

namespace FlickrSyncProvider
{
    public class FlickrFullEnumerationProvider : FullEnumerationSimpleSyncProvider
    {
        private readonly string _rootFolder;
        private readonly SyncId _replicaId;
        private readonly SqlMetadataStore _metadataStore;
        private readonly Flickr _flickr;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _user;
        private const int _pageSize = 500;
        private readonly PhotosetTree _photosetTree;
        private readonly List<FlickrFile> _cachedMetadata;


        public FlickrFullEnumerationProvider(Guid guidReplica, string rootFolder, FlickrConnectionSettings flickrSettings)
        {
            _rootFolder = rootFolder;
            _flickr = new Flickr(flickrSettings.ApiKey, flickrSettings.SharedSecret)
            {
                OAuthAccessToken = flickrSettings.AccessToken,
                OAuthAccessTokenSecret = flickrSettings.AccessSecret,
                CurrentService = SupportedService.Flickr,
                InstanceCacheDisabled = false
            };
            _user = flickrSettings.UserId;
            _replicaId = new SyncId(guidReplica);
            _photosetTree = new PhotosetTree(_flickr);
            var metadataPath = Path.Combine(_rootFolder, "flickr.metadata");
            if (File.Exists(metadataPath))
            {
                _metadataStore = SqlMetadataStore.OpenStore(metadataPath);
            } else
            {
                _metadataStore = SqlMetadataStore.CreateStore(metadataPath);
                File.SetAttributes(metadataPath, FileAttributes.Hidden); 
            }
            _cachedMetadata = new List<FlickrFile>();
        }

        public override MetadataStore GetMetadataStore(out SyncId replicaId, out CultureInfo culture)
        {
            replicaId = _replicaId;
            culture = CultureInfo.CurrentCulture;

            return _metadataStore;
        }

        #region read
        public override object LoadChangeData(ItemFieldDictionary keyAndExpectedVersion, IEnumerable<SyncId> changeUnitsToLoad, 
                                              RecoverableErrorReportingContext recoverableErrorReportingContext)
        {
            // Figure out which item is being asked for
            var mediaItem = keyAndExpectedVersion.ToFlickrMediaObject();
            // Check if the item has been updated on flickr
            if (mediaItem.HasBeenUpdatedOnRemote(_flickr))
            {
                _logger.Warn("Photo id {0} has been updated on flickr ");
                recoverableErrorReportingContext.RecordRecoverableErrorForChange(new RecoverableErrorData(null));
                return null;
            }
            return new FlickrPhotoDataRetriever(mediaItem);            
        }


        /// <summary>
        /// Enumerates all files on Flickr
        /// </summary>
        public override void EnumerateItems(FullEnumerationContext context)
        {
            var items = new List<ItemFieldDictionary>();
            _cachedMetadata.Clear();

            try
            {                
                var photoSets = PhotosetsGetListAllPages(_user, PhotoSearchExtras.None).ToList();
                _photosetTree.UpdateCollectionTree(photoSets);

                foreach(var photoSet in photoSets.Take(2))
                {
                    // we represent photosets as folders
                    items.Add(photoSet.ToItemFieldDictionary(_photosetTree));

                    var photosInSet = PhotosetsGetPhotosAllPages(photoSet.PhotosetId, PhotoSearchExtras.OriginalUrl | PhotoSearchExtras.MachineTags | PhotoSearchExtras.OriginalFormat | PhotoSearchExtras.LastUpdated).ToList();
                    
                    foreach(var photo in photosInSet.Take(5))
                    {
                        var p = new FlickrFile(photo, photoSet.PhotosetId,
                                                    _photosetTree.ResolvePathByPhotoset(photoSet.PhotosetId));
                        _cachedMetadata.Add(p);
                        items.Add(p.ToItemFieldDictionary());
                    }
                }
                // go process Not In set special case. We'll dump all files in home folder root
                var notInSetPhotos = PhotosGetNotInSetAllPages(PhotoSearchExtras.OriginalUrl | PhotoSearchExtras.MachineTags | PhotoSearchExtras.OriginalFormat | PhotoSearchExtras.LastUpdated);
                foreach(var photo in notInSetPhotos)
                {
                    var p = new FlickrFile(photo, string.Empty, _photosetTree.ResolvePathByPhotoset(string.Empty));
                    _cachedMetadata.Add(p);
                    // PhotoSetTreeResolver.ResolvePathByPhotoset(string.Empty) will be hardcoded to root virtual path
                    items.Add(p.ToItemFieldDictionary());
                }

                // Report item to Simple Provider framework
                context.ReportItems(items);
            }
            catch (Exception ex)
            {
                _logger.Error("Error Polling Flickr", ex);
                throw;
            }            
        }
        #endregion

        #region write
        public override void InsertItem(object itemData, IEnumerable<SyncId> changeUnitsToCreate,
                                        RecoverableErrorReportingContext recoverableErrorReportingContext,
                                        out ItemFieldDictionary keyAndUpdatedVersion, out bool commitKnowledgeAfterThisItem)
        {
            // Figure out where to create it
            var fileData = itemData as IFileDataRetriever;
          
            // upload a photo
            ItemFieldDictionary file = null;
            string photoId = string.Empty;
            try
            {
                if (fileData == null)
                    throw new ArgumentNullException("itemData");

                if (fileData.FileData.IsDirectory) // create photoset in a correct place
                {
                    Photoset photoset = _photosetTree.EnsurePhotosetExists(fileData.RelativeDirectoryPath, _flickr);
                    // report resulting photo back
                    file = photoset.ToItemFieldDictionary(_photosetTree);
                }
                else// upload the photo
                {
                    var tags = FlickrFile.GenerateMachineTags(fileData);
                    photoId = _flickr.UploadPicture(fileData.AbsoluteSourceFilePath, Path.GetFileNameWithoutExtension(fileData.FileData.Name), "", tags, false, false, false);// TODO: Upload privacy settings
                    // place in a correct photoset if applicable
                    var photoset = _photosetTree.ResolvePhotosetByPath(fileData.RelativeDirectoryPath) ?? new Photoset { PhotosetId = "0" };// a hack to present root folder photos as not in set
                    if (photoset.PhotosetId != "0")
                        _flickr.PhotosetsAddPhoto(photoset.PhotosetId, photoId);
                    // report resulting photo back
                    var photo = _flickr.PhotosGetInfo(photoId);
                    file = new FlickrFile(photo, photoset.PhotosetId, fileData.RelativeDirectoryPath).ToItemFieldDictionary();   
                }
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("Upload Flickr photo {0} failed", photoId), ex);
                recoverableErrorReportingContext.RecordConstraintError(file);

                commitKnowledgeAfterThisItem = false;
                keyAndUpdatedVersion = file;
                return;
            }
            
            // Return particulars to Simple Provider framework
            keyAndUpdatedVersion = file;
            commitKnowledgeAfterThisItem = false;
        }

        public override void UpdateItem(object itemData, IEnumerable<SyncId> changeUnitsToUpdate, ItemFieldDictionary keyAndExpectedVersion,
                                        RecoverableErrorReportingContext recoverableErrorReportingContext,
                                        out ItemFieldDictionary keyAndUpdatedVersion, out bool commitKnowledgeAfterThisItem)
        {
            // Figure out where to create it
            var newFileData = itemData as IFileDataRetriever;
            var currentObject = keyAndExpectedVersion.ToFlickrMediaObject();
            // upload a photo
            FlickrFile file = null;

            try
            {
                if (newFileData == null)
                    throw new InvalidDataException("no new data passed from Sync Framework");

                // Return particulars to Simple Provider framework
                keyAndUpdatedVersion = currentObject.UpdateRemote(newFileData, _flickr, _photosetTree);                
                commitKnowledgeAfterThisItem = false;
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("Update Flickr photo {0} failed", currentObject.GetRelativeFileName()), ex);
                recoverableErrorReportingContext.RecordConstraintError(file.ToItemFieldDictionary());

                keyAndUpdatedVersion = null;
                commitKnowledgeAfterThisItem = false;

            }
        }

        public override void DeleteItem(ItemFieldDictionary keyAndExpectedVersion,
                                        RecoverableErrorReportingContext recoverableErrorReportingContext, out bool commitKnowledgeAfterThisItem)
        {
            // Figure out what is being asked for
            var mediaObject = keyAndExpectedVersion.ToFlickrMediaObject();
            
            try
            {
                if (mediaObject.HasBeenUpdatedOnRemote(_flickr))// Check if it changed --- race condition
                {
                    recoverableErrorReportingContext.RecordRecoverableErrorForChange(
                        new RecoverableErrorData(null, mediaObject.VirtualPath, "File modified on Flickr"));
                }
                else
                {
                    mediaObject.DeleteOnRemote(_flickr);                    
                }                    
            }
            catch (Exception ex)
            {
                _logger.Error("Delete operation failed", ex);
            }
            commitKnowledgeAfterThisItem = true;
        }

        #endregion

        public override SyncIdFormatGroup IdFormats
        {
            get
            {
                var idFormats = new SyncIdFormatGroup();                    
                idFormats.ItemIdFormat.Length = 24;
                idFormats.ItemIdFormat.IsVariableLength = false;
                idFormats.ReplicaIdFormat.Length = 16;
                idFormats.ReplicaIdFormat.IsVariableLength = false;
                idFormats.ChangeUnitIdFormat.Length = 4;
                idFormats.ChangeUnitIdFormat.IsVariableLength = false;

                return idFormats;
            }
        }        

        public override ItemMetadataSchema MetadataSchema
        {
            get { return FlickrFile.GetSchema(); }
        }

        public override short ProviderVersion
        {
            get { return 1; }
        }

        public override void BeginSession() { }

        public override void EndSession() { }

        #region De-paging methods for api results
        private IEnumerable<Photo> PhotosetsGetPhotosAllPages(string photosetId, PhotoSearchExtras photoSearchExtras, int page = 1)
        {
            _logger.Trace("Calling PhotosetsGetPhotosAllPages for set {0}, page {1}", photosetId, page);
            var photoCollection = _flickr.PhotosetsGetPhotos(photosetId, photoSearchExtras, page, _pageSize);
            var result = new List<Photo>();
            result.AddRange(photoCollection.ToList());
            if (photoCollection.Total > page * photoCollection.PerPage)
            {
                var photoCollectionNextPage = PhotosetsGetPhotosAllPages(photosetId, photoSearchExtras, page + 1);
                result.AddRange(photoCollectionNextPage);
            }
            return result;
        }

        private IEnumerable<Photoset> PhotosetsGetListAllPages(string userId, PhotoSearchExtras photoSearchExtras, int page = 1)
        {
            _logger.Trace("Calling PhotosetsGetListAllPages for user {0}, page {1}", userId, page);
            var photoSetCollection = _flickr.PhotosetsGetList(userId, page, _pageSize, photoSearchExtras);
            var result = new List<Photoset>();
            result.AddRange(photoSetCollection.ToList());
            if (photoSetCollection.Total > page * photoSetCollection.PerPage)
            {
                var photoSetCollectionNextPage = PhotosetsGetListAllPages(userId, photoSearchExtras, page + 1);
                result.AddRange(photoSetCollectionNextPage);
            }
            return result;
        }

        private IEnumerable<Photo> PhotosGetNotInSetAllPages(PhotoSearchExtras photoSearchExtras, int page = 1)
        {
            _logger.Trace("Calling PhotosGetNotInSetAllPages, page {0}", page);
            var photoCollection = _flickr.PhotosGetNotInSet(page, _pageSize, photoSearchExtras);
            var result = new List<Photo>();
            result.AddRange(photoCollection.ToList());
            if (photoCollection.Total > page * photoCollection.PerPage)
            {
                var photoCollectionNextPage = PhotosGetNotInSetAllPages(photoSearchExtras, page + 1);
                result.AddRange(photoCollectionNextPage);
            }
            return result;
        }
        #endregion

    }
}
