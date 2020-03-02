using SCQueryConnect.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SCQueryConnect.Interfaces
{
    public interface IAttributeMappingEditorViewModel : INotifyPropertyChanged
    {
        bool IsInitialised { get; }
        List<AttributeDesignations> StoryAttributes { get; }
        List<AttributeMapping> AttributeMappings { get; }

        Task InitialiseEditor(IDictionary<string, string> existingMapping);
        Dictionary<string, string> ExtractMapping();
        void Clear();

        event EventHandler InitialisationError;
    }
}
