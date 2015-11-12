# Furk.net Download Manager (dlm.exe)

Last update to this document: 2015-11-12
Documentation is not up-to-date with code

## Disclaimer

This software is not officially provided, endorsed or supported in any way by Furk.net: it just uses the provided Open API to query the service and download data.

## Licenses

This software is distributed under MIT license. Some parts of this software are or use other open source code or libraries: 

* RestSharp: [project page](https://github.com/restsharp/RestSharp) and [license](https://github.com/restsharp/RestSharp/blob/master/LICENSE.txt)
* Newtonsoft Json.NET: [project page](https://github.com/JamesNK/Newtonsoft.Json) and [license](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
* INI manager class: [project page](http://www.codeproject.com/Articles/646296/A-Cross-platform-Csharp-Class-for-Using-INI-Files) and [license](http://www.codeproject.com/info/cpol10.aspx)

## Overview

dlm is a small console application that

* Connects to your Furk.net seedbox via API
* Downloads ready content on your computer
* Moves downloaded content to a secondary position (e.g. NAS)
* Renames quirky files into a more readable format
* Downloads subtitles for downloaded movies or series

dlm runs on Windows (tested), Mac (tested) and Linux thanks to Mono runtime.  

## Configuration and INI file

Configuration is done via INI file. INI file is automatically created and initiated the first time the program runs. It's placed in the same path as the executing program.

### Bare minimum configuration

dlm requires at least your Furk.net API key. Your API key can be retrived on Furk website (in this moment at the following url: [https://www.furk.net/t/api](https://www.furk.net/t/api)). The bare minimum configuration is the following:

    [Options]
    FurkApiKey=some_long_exadecimal_string

That's it. All other settings are optional.

### Optional parameters

The following parameters can be used to modify and customize the behavior of the software.

#### Inclusion and exclusion

	[Options]
    IncludeTorrentsWithKeywords=some,kind,of,list
    ExcludeTorrentsWithKeywords=some,kind,of,list
	IncludeFilesWithKeywords=some,kind,of,list
	ExcludeFilesWithKeywords=some,kind,of,list

These parameters can be used to control the files to be processed and eventually downloaded. When one of these four lists is empty, the parameter is not considered. Otherwise, if something is specified, the list acts as a filter either to include or exclude a torrent or a file (sometimes torrent and file are ideally the same since a torrent can contain a single file; some other times a torrent contains lot of files). Inclusion is processed before exclusion, so an exclusion keyword has the latest word about when a file should or shouln't be included and processed.

The most common use for an include or exclude list can be with extensions or resolutions. The following example includes only files with .mkv, .srt or 720p in their names (everything not matching this filter will not be downloaded) and explicitly excludes any file containing 1080p. So "myfavouriteseries.720p.AAC.mkv" will be downloaded while "myotherseries.1080p.mkv" will be discarded.

	[Options]
	IncludeFilesWithKeywords=.mkv,.srt,720p
	ExcludeFilesWithKeywords=1080p

#### Maximum sizes

The following parameters can be used to specify a maximum torrent and/or file size. Torrents and files above this value will be discarded by the program. Size is in bytes (so: 1 MB = 1000000, 1 GB = 1000000000 etc)

	[Options]
	MaxTorrentSize=
	MaxFileSize=

#### Maximum concurrent downloads

Use this setting to specify how many downloads shall be processed at the same moment. When not specified, one file at time will be downloaded.

	[Options]
	MaxConcurrentDownloads=

#### Pushbullet notification

Program can optionally send notification messages via Pushbullet. To achieve this, you have to get your Pushbullet API key from Pushbullet website. If you have more than one device registered with Pushbullet and only want to be notified on a single device, type the name of that device in the appropriate key; otherwise, leave blank to be notified everywhere.

	PushbulletApiKey=your_pushbullet_api_key
	PushbulletDeviceName=your_device_name

### Per-computer specific settings

dlm can run from a shared location, like a Dropbox folder or USB key. Some settings can be therefore specified on a per-computer basis, notably paths. When configuration for a computer is not found it's automatically created using default settings. The INI section in which per-computer settings are stored is defined like this:

    [ComputerName:YourComputerName]

Where YourComputerName is - guess - your computer's name.

Supported settings in this section are:
	
	[ComputerName:YourComputerName]
	LocalPath=
	RemotePath=

**LocalPath** defines where files are stored when downloaded from FurkApi. This path defaults to user's documents folder.

**RemotePath** can be used to move files out of LocalPath; this may be a remote location, a NAS or any other valid path: when this path is available, files in LocalPath are moved there. When not specified, this value is ignored.