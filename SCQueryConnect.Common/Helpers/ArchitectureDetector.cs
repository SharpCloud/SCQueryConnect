using SCQueryConnect.Common.Interfaces;
using System;

namespace SCQueryConnect.Common.Helpers
{
    public class ArchitectureDetector : IArchitectureDetector
    {
        public bool Is32Bit => IntPtr.Size == 4;
    }
}
