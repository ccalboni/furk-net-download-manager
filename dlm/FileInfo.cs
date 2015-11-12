using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace dlm
{
    class FileInfo
    {
        public string Name { get; set; }

        public string Uri { get; set; }

        public long Length { get; set; }

        public FileStatus Status { get; set; }

        public string LocalFilePath { get; set; }

        public string RemoteFilePath { get; set; }

        public int DownloadPercentage { get; set; }

        public long DownloadedBytes { get; set; }

        public DateTime DownloadBeginTime { get; set; }

        public override bool Equals(object obj)
        {
            var otherFileInfo = obj as FileInfo;
            return otherFileInfo != null && otherFileInfo.Uri.Equals(this.Uri);
        }

        public override int GetHashCode()
        {
            return this.Uri.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Name={0}", this.Name);
        }

        public string DestinationFolderName { get; set; }

        public string FriendlyName { get; set; }

        public void InferDataFromFileName()
        {
            string extension = Path.GetExtension(this.Name);
            string originalFileName = Path.GetFileNameWithoutExtension(this.Name);
            string friendlyFileName = string.Empty;

            //get and preserve known encoding or other "tags" present in filename
            var preservableTags = new List<string> { "720p", "1080p", "Ita", "Eng" };
            var preserveTags = new List<string>();
            foreach (var preservableTag in preservableTags)
            {
                var match = Regex.Match(originalFileName, string.Format(@"\b{0}\b", preservableTag), RegexOptions.IgnoreCase);
                if (match != null && match.Success)
                {
                    Regex.Replace(originalFileName, string.Format(@"\b{0}\b", preservableTag), string.Empty);
                    preserveTags.Add(preservableTag);
                }
            }

            //clean everything in square brackets
            Regex.Replace(originalFileName, @"\[.+\]", string.Empty);

            var seriesMatch = Regex.Match(originalFileName, @"\b(S[0-9][0-9]?E\d\d?|\dx\d\d?)");
            if (seriesMatch != null && seriesMatch.Success)
            {
                //a serie
                var leftPart = originalFileName.Substring(0, originalFileName.IndexOf(seriesMatch.Value));
                //var rightPart = fileName.Substring(fileName.IndexOf(seriesMatch.Value) + seriesMatch.Value.Length);
                leftPart = leftPart.Replace(".", " ").Trim();
                this.DestinationFolderName = leftPart;
                var seriesAndEpisodeNumbersMatches = Regex.Matches(seriesMatch.Value, @"\d+");
                var serieNumber = int.Parse(seriesAndEpisodeNumbersMatches[0].Value);
                var episodeNumber = int.Parse(seriesAndEpisodeNumbersMatches[1].Value);
                friendlyFileName = string.Format("{0} S{1:0#}E{2:0#}", leftPart, serieNumber, episodeNumber);



            }
            else
            {
                //not a serie
            }

            //reattach preservable tags
            if (preserveTags.Count > 0)
            {
                friendlyFileName += " [";
                for (int i = 0; i < preserveTags.Count; i++)
                {
                    friendlyFileName += preserveTags[i];
                    if (i < preserveTags.Count - 1)
                    {
                        friendlyFileName += " - ";
                    }
                }
                friendlyFileName += "]";
            }

            //compose final name
            this.FriendlyName = string.Format("{0}.{1}", friendlyFileName, extension);

            //bool isSeries = Regex.IsMatch(filename, "S[0-9]") || Regex.IsMatch("[0-9]x[0-9]");

            //switch (extension.ToLowerInvariant())
            //{
            //    case "mkv":
            //    case "mp4":
            //        filetype = isSeries ? Filetype.TVSeries : Filetype.Movie;
            //    default:
            //        filetype = Filetype.NotSpecified;
            //}

            //if (isSeries)
            //{

            //}
            //else
            //{
            //}
        }
    }
}
