namespace SCQueryConnect.Common.Interfaces
{
    public interface IExcelWriter
    {
        void RewriteExcelFile(string path);
        void WriteToExcel(string filename, string[,] itemsData, string[,] relationshipsData);
    }
}
