namespace SCQueryConnect.Common.Interfaces
{
    public interface IExcelWriter
    {
        string GetValidFilename(string filename);
        void WriteToExcel(string filename, string[,] itemsData, string[,] relationshipsData);
    }
}
