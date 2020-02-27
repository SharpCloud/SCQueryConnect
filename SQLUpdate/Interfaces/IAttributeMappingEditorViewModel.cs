using SCQueryConnect.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SCQueryConnect.Interfaces
{
    public interface IAttributeMappingEditorViewModel : INotifyPropertyChanged
    {
        List<AttributeDesignations> StoryAttributes { get; }
        List<AttributeMapping> AttributeMappings { get; }

        Task InitialiseEditor();
    }
}
