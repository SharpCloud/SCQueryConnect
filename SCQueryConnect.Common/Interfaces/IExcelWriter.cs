namespace SCQueryConnect.Common.Helpers
{
    public interface IExcelWriter
    {
        void WriteToExcel(string filename, string[,] itemsData, string[,] relationshipsData);
    }
}
