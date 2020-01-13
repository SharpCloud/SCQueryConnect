namespace SCQueryConnect.Interfaces
{
    public interface IPostDeserializeAction<T>
    {
        void OnPostDeserialization(T model);
    }
}
