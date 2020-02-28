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

        Task InitialiseEditor(IDictionary<string, string> existingMapping);
        Dictionary<string, string> ExtractMapping();
        void Clear();
    }
}
