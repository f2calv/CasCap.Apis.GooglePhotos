using System;
namespace CasCap.Models
{
    public class PagingEventArgs : EventArgs
    {
        public PagingEventArgs(int pageSize, int pageNumber, int recordCount, object? pageData)
        {
            this.pageSize = pageSize;
            this.pageNumber = pageNumber;
            this.recordCount = recordCount;
            this.pageData = pageData;
        }

        public int pageSize { get; }
        public int pageNumber { get; }
        public int recordCount { get; }
        public object? pageData { get; }
        public DateTime? minDate { get; set; }
        public DateTime? maxDate { get; set; }
    }
}