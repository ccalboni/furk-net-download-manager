using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace dlm
{
    enum FileStatus
    {
        //Fetched,
        //Queued,
        DownloadingFromApiToDisk,
        DownloadedToLocalDisk,
        //Downloaded,

    }

    class TorrentInfo
    {
        public string ID { get; set; }

        public string InfoHash { get; set; }

        public string Name { get; set; }

        public long Size { get; set; }

        public List<FileInfo> Files { get; set; }

        public TorrentInfo()
        {
            this.Files = new List<FileInfo>();
        }

        public override string ToString()
        {
            return string.Format("Name={0}, Files={1}", Name, Files.Count);
        }
    }
}
