﻿using SCQueryConnect.Models;
using System.ComponentModel;

namespace SCQueryConnect.Interfaces
{
    public interface IQueryItem : INotifyPropertyChanged
    {
        string Id { get; }
        string Name { get; set; }
        string Description { get; set; }
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        QueryBatch ParentFolder { get; set; }
        string ParentFolderId { get; set; }
    }
}
