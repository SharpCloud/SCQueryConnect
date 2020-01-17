using SCQueryConnect.Interfaces;

namespace SCQueryConnect.ViewModels
{
    public class ViewModelLocator
    {
        public static IBatchSequenceViewModel BatchSequenceViewModel =>
            Bootstrapper.Resolve<IBatchSequenceViewModel>();
    }
}
