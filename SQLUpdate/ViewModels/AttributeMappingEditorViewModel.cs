using SCQueryConnect.Interfaces;
using SCQueryConnect.Models;
using System;
using System.Collections.Generic;

namespace SCQueryConnect.ViewModels
{
    public class AttributeMappingEditorViewModel : IAttributeMappingEditorViewModel
    {
        public List<AttributeDesignations> StoryAttributes { get; set; }
        public List<AttributeMapping> AttributeMappings { get; set; }

        public void InitialiseEditor()
        {
        }

        private IList<AttributeDesignations> GetStoryAttributes()
        {
            throw new NotImplementedException();
        }

        private IList<AttributeMapping> InitialiseAttributeMappings()
        {
            throw new NotImplementedException();
        }
    }
}
