using System.Collections.Generic;

namespace Saber.Risk.Core.Utils
{
    public class PagedData<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}