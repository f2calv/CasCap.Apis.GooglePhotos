using System;
namespace CasCap.Models;

public class PagingEventArgs : EventArgs
{
    public PagingEventArgs(int pageSize, int pageNumber, int recordCount)
    {
        this.pageSize = pageSize;
        this.pageNumber = pageNumber;
        this.recordCount = recordCount;
    }

    public int pageSize { get; }
    public int pageNumber { get; }
    public int recordCount { get; }
    public DateTime? minDate { get; set; }
    public DateTime? maxDate { get; set; }
}