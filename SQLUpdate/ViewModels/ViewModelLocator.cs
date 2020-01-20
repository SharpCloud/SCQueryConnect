using SCQueryConnect.Interfaces;

namespace SCQueryConnect.ViewModels
{
    public class ViewModelLocator
    {
        public static ISolutionViewModel SolutionViewModel => Bootstrapper.Resolve<ISolutionViewModel>();
    }
}
