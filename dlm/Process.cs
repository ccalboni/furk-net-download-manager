using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace dlm
{
    internal class Process
    {
        private FurkApi _furkApi;

        public void Run()
        {
            this._furkApi = new FurkApi(Settings.FurkApiKey, Settings.FurkApiEndpoint);

            var availableTorrents = this.GetTorrentsFromSeedbox();
            var availableFiles = this.GetFilteredFiles(availableTorrents);

            Logger.Info("{0} available files found in {1} torrents", availableFiles.Count, availableTorrents.Count);

            this.ProcessDownloads(availableFiles);

            //waiting for last downloads to finish
            while (UI.Instance.ActiveDownloadsCount != 0)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Logger.Info("All downloads processed");
        }

        /// <summary>
        /// Gets torrents and contained files information from Furk API
        /// </summary>
        private List<TorrentInfo> GetTorrentsFromSeedbox()
        {
            var availableTorrents = new List<TorrentInfo>();
            var availableTorrentsResponse = this._furkApi.GetReadyTorrents();
            if (availableTorrentsResponse.status == "ok")
            {
                foreach (dynamic availableTorrent in availableTorrentsResponse.files)
                {
                    Logger.Trace("{0} - {1}", availableTorrent.name, availableTorrent.id);
                    var torrent = new TorrentInfo()
                    {
                        ID = availableTorrent.id,
                        InfoHash = availableTorrent.info_hash,
                        Name = availableTorrent.name,
                        Size = availableTorrent.size
                    };
                    availableTorrents.Add(torrent);

                    var availableTorrentFiles = this._furkApi.GetTorrentDetails(torrent.InfoHash);

                    if (availableTorrentFiles != null
                        && availableTorrentFiles.files != null
                        && availableTorrentFiles.files.Count == 1)
                    {
                        foreach (var availableFile in availableTorrentFiles.files[0].t_files)
                        {
                            var file = new FileInfo() { Name = availableFile.name, Uri = availableFile.url_dl, Length = availableFile.size };
                            file.InferDataFromFileName();
                            torrent.Files.Add(file);
                        }
                    }
                }
            }
            else
            {
                Logger.Error("Furk API reported an error: '{0}'", availableTorrentsResponse.error);
            }
            return availableTorrents;
        }

        /// <summary>
        /// Applies user's rules to filter out undesidered downloads
        /// </summary>
        private List<FileInfo> GetFilteredFiles(IList<TorrentInfo> availableTorrents)
        {
            var downloadableFiles = new List<FileInfo>();
            foreach (var torrent in availableTorrents)
            {
                bool canQueueTorrent = false;

                //include torrents by keyword
                if (Settings.IncludeTorrentsWithKeywords.Count > 0)
                {
                    //if include keywords are provided, accepts only torrents matching keywords
                    foreach (var includeTorrentKeyword in Settings.IncludeTorrentsWithKeywords)
                    {
                        if (torrent.Name.ToLowerInvariant().Contains(includeTorrentKeyword))
                        {
                            canQueueTorrent = true;
                            Logger.Trace("Torrent '{0}' included by keyword: '{1}'", torrent.Name, includeTorrentKeyword);
                            break;
                        }
                    }
                    Logger.Trace("Torrent '{0}' excluded because no matching including keywords were found", torrent.Name);
                }
                else
                {
                    //if zero keywords are provided, every torrent is good
                    canQueueTorrent = true;
                }

                //if exclude keywords are provided, rejects torrent matching keywords
                if (Settings.ExcludeTorrentsWithKeywords.Count > 0)
                {
                    foreach (var excludeTorrentKeyword in Settings.ExcludeTorrentsWithKeywords)
                    {
                        if (torrent.Name.ToLowerInvariant().Contains(excludeTorrentKeyword))
                        {
                            canQueueTorrent = false;
                            Logger.Trace("Torrent '{0}' excluded by keyword: '{1}'", torrent.Name, excludeTorrentKeyword);
                            break;
                        }
                    }
                }

                //if there's a filter for maximum torrent size
                if (Settings.MaxTorrentSize > 0)
                {
                    if (torrent.Size > Settings.MaxTorrentSize)
                    {
                        Logger.Trace("Torrent '{0}' excluded because larger than maximum size: {1} > {2}", torrent.Name, torrent.Size, Settings.MaxTorrentSize);
                        canQueueTorrent = false;
                    }
                }

                if (canQueueTorrent)
                {
                    foreach (var file in torrent.Files)
                    {
                        var canQueueFile = false;

                        //if include keywords are provided, accepts only files matching keywords
                        if (Settings.IncludeFilesWithKeywords.Count > 0)
                        {
                            foreach (var includeFileKeyWord in Settings.IncludeFilesWithKeywords)
                            {
                                if (file.Name.ToLowerInvariant().Contains(includeFileKeyWord))
                                {
                                    canQueueFile = true;
                                    Logger.Trace("File '{0}' included by keyword: '{1}'", file.Name, includeFileKeyWord);
                                    break;
                                }
                            }
                            Logger.Trace("File '{0}' excluded because no matching including keywords were found", torrent.Name);
                        }
                        else
                        {
                            //if zero keywords are provided, every file is good
                            canQueueFile = true;
                        }

                        //if exclude keywords are provided, rejects files matching keywords
                        if (Settings.ExcludeFilesWithKeywords.Count > 0)
                        {
                            foreach (var excludeFileKeyword in Settings.ExcludeFilesWithKeywords)
                            {
                                if (file.Name.ToLowerInvariant().Contains(excludeFileKeyword))
                                {
                                    canQueueFile = false;
                                    Logger.Trace("File '{0}' excluded by keyword: '{1}'", file.Name, excludeFileKeyword);
                                    break;
                                }
                            }
                        }

                        //if there's a filter for maximum torrent size
                        if (Settings.MaxFileSize > 0)
                        {
                            if (file.Length > Settings.MaxFileSize)
                            {
                                Logger.Trace("File '{0}' excluded because larger than maximum size: {1} > {2}", file.Name, file.Length, Settings.MaxFileSize);
                                canQueueFile = false;
                            }
                        }

                        if (canQueueFile)
                        {
                            //exclude already downloaded files
                            if (Settings.DownloadsHistory.Contains(file.Name))
                            {
                                Logger.Info("File '{0}' excluded because it is marked as downloaded", file.Name);
                            }
                            else
                            {
                                if (File.Exists(file.LocalFilePath))
                                {
                                    //check if a partial download is present, in that case clean up and restart
                                    var existingFileInfo = new System.IO.FileInfo(file.LocalFilePath);
                                    if (existingFileInfo.Length < file.Length)
                                    {
                                        File.Delete(file.LocalFilePath);
                                        Logger.Warn("Unfinished file download will restart: '{0}'", file.Name);
                                    }
                                    else if (existingFileInfo.Length == file.Length)
                                    {
                                        Settings.AddHistoryItem(file.Name);
                                        Logger.Info("File '{0}' excluded because already fully downloaded", file.Name);
                                    }
                                }

                                if (!downloadableFiles.Contains(file))
                                {
                                    downloadableFiles.Add(file);
                                }
                            }
                        }
                    }
                }
            }
            return downloadableFiles;
        }

        private void ProcessDownloads(IList<FileInfo> downloadableFiles)
        {
            int currentDownloadIndex = 0;
            UI.Instance.SetTotalBytesToReceive(downloadableFiles);

            while (currentDownloadIndex < downloadableFiles.Count - 1)
            {
                if (UI.Instance.ActiveDownloadsCount <= Settings.MaxConcurrentDownloads)
                {
                    var downloadableFile = downloadableFiles[currentDownloadIndex];
                    downloadableFile.Status = FileStatus.DownloadingFromApiToDisk;
                    downloadableFile.LocalFilePath = Path.Combine(Settings.LocalPath, downloadableFile.Name);

                    if (!Settings.MustSimulateDownloads)
                    {
                        downloadableFile.DownloadBeginTime = DateTime.Now;
                        Logger.Info("Startig download, threads: {0}/{1}, downloads: {2}/{3}",
                            UI.Instance.ActiveDownloadsCount, Settings.MaxConcurrentDownloads, currentDownloadIndex, downloadableFiles.Count);
                        var webClient = new WebClient();
                        webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                        webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                        UI.Instance.ActiveDownloadsCount++;
                        webClient.DownloadFileAsync(new Uri(downloadableFile.Uri), downloadableFile.LocalFilePath, downloadableFile);
                    }
                    else
                    {
                        this.SimulateDownload(downloadableFile);
                    }

                    currentDownloadIndex++;
                }
                else
                {
                    Logger.Trace("All workers busy, waiting");
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Creates an empty file simulating download without actually downloading it from the net
        /// </summary>
        /// <param name="fileInfo"></param>
        private void SimulateDownload(FileInfo fileInfo)
        {
            Logger.Warn("Simulating download: {0}", fileInfo.Name);
            File.AppendAllText(fileInfo.LocalFilePath, "");
            System.Threading.Thread.Sleep(1000);
            UI.Instance.ActiveDownloadsCount--;
        }

        /// <summary>
        /// Notifies advancement while downloading a file
        /// </summary>
        private static void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var activeFile = (FileInfo)e.UserState;
            activeFile.Status = FileStatus.DownloadingFromApiToDisk;
            activeFile.DownloadPercentage = e.ProgressPercentage;
            activeFile.DownloadedBytes = e.BytesReceived;
            UI.Instance.DownloadProgress(activeFile);

            if (!UI.Instance.IsAlive)
            {
                ((WebClient)sender).CancelAsync();
                Logger.Error("Program closed, download aborted: {0}", activeFile.Name);
            }
        }

        /// <summary>
        /// Notifies when a download finishes
        /// </summary>
        private static void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled == false)
            {
                var activeFile = (FileInfo)e.UserState;
                activeFile.Status = FileStatus.DownloadedToLocalDisk;
                UI.Instance.DownloadProgress(activeFile);
                Settings.AddHistoryItem(activeFile.Name);
                UI.Instance.ActiveDownloadsCount--;
                Logger.Info("Finished downloading: {0}", activeFile.Name);
                Logger.Notify("Finished downloading: {0}", activeFile.Name);
            }
        }


    }
}
