namespace AnythingToMp3Converter.UI.Models
{
    using AnythingToMp3Converter.UI.Enums;

    public class MediaFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public FileStatus FileStatus { get; set; }
        public int Progress { get; set; }
    }
}
