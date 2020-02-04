using SCQueryConnect.Interfaces;

namespace SCQueryConnect.ViewModels
{
    public class ViewModelLocator
    {
        public static IMainViewModel MainViewModel => Bootstrapper.Resolve<IMainViewModel>();
    }
}
