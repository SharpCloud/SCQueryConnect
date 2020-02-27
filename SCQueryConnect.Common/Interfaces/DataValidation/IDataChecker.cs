﻿using SCQueryConnect.Common.Models;
using System.Data;
using System.Threading.Tasks;

namespace SCQueryConnect.Common.Interfaces.DataValidation
{
    public interface IDataChecker
    {
        QueryEntityType TargetEntity { get; }

        Task<bool> CheckData(IDataReader reader);
        Task<bool> CheckData(IDataReader reader, object state);
    }
}
