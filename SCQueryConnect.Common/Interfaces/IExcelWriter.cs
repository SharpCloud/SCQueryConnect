namespace SCQueryConnect.Common.Interfaces
{
    public interface IExcelWriter
    {
        string GetValidFilename(string filename);
        void RewriteExcelFile(string path);
        void WriteToExcel(string filename, string[,] itemsData, string[,] relationshipsData);
    }
}
