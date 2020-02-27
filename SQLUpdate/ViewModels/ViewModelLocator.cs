using SCQueryConnect.Interfaces;

namespace SCQueryConnect.ViewModels
{
    public class ViewModelLocator
    {
        public static IAttributeMappingEditorViewModel AttributeMappingEditorViewModel =>
            Bootstrapper.Resolve<IAttributeMappingEditorViewModel>();

        public static IMainViewModel MainViewModel => Bootstrapper.Resolve<IMainViewModel>();
        public static IProxyViewModel ProxyViewModel => Bootstrapper.Resolve<IProxyViewModel>();
    }
}
