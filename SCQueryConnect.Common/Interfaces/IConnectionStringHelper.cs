namespace SCQueryConnect.Common.Helpers
{
    public interface IConnectionStringHelper
    {
        string GetVariable(string connectionString, string variableName);
        string SetDataSource(string connectionString, string newLocation);
    }
}
