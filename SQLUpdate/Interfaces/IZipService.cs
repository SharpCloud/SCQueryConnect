namespace SCQueryConnect.Interfaces
{
    public interface IZipService
    {
        void ExtractZipFileToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
    }
}
