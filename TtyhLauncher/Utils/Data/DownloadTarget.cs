namespace TtyhLauncher.Utils.Data {
    public class DownloadTarget {
        public readonly string Path;
        public readonly string Url;
        public readonly long Size;
        public readonly string Sha1;

        public DownloadTarget(string path, string url, long size, string sha1) {
            Path = path;
            Url = url;
            Size = size;
            Sha1 = sha1;
        }
    }
}