namespace SCQueryConnect.Common.Interfaces.DataValidation
{
    public interface IDataValidityProcessor
    {
        void ProcessDataValidity(bool isOk, object state);
    }
}
