using System;
namespace CasCap.Models;

public class UploadProgressArgs : EventArgs
{
    public UploadProgressArgs(string fileName, long totalBytes, int batchIndex, long uploadedBytes, long batchSize)
    {
        this.fileName = fileName;
        this.totalBytes = totalBytes;
        this.batchIndex = batchIndex;
        this.uploadedBytes = uploadedBytes;
        this.batchSize = batchSize;
    }

    public string fileName { get; }
    public long totalBytes { get; }
    public long batchIndex { get; }
    public long uploadedBytes { get; }
    public long batchSize { get; }
}