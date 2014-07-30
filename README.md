MS-Sync-Flickr-Provider
=======================

A Microsoft Sync Framework SimpleSyncProvider implementation for Flickr

## Usage

```C#
var localProv = new FileSyncProvider(homeFolder);
var flickrSettings = new FlickrConnectionSettings
	{
		ApiKey = "Your Flickr Key",
		SharedSecret = "Secret",
		AccessToken = "oAuth Token",
		AccessSecret = "oAuth Secret",
		UserId = "Flickr User Id"
	};
var flickrProv = new FlickrFullEnumerationProvider(Guid.Parse("0360D566-B745-4879-9E54-F6BB083D92E1"), homeFolder, flickrSettings);
var agent = new SyncOrchestrator
	{
		Direction = SyncDirectionOrder.DownloadAndUpload,
		LocalProvider = localProv,
		RemoteProvider = flickrProv
	};
agent.Synchronize();
```