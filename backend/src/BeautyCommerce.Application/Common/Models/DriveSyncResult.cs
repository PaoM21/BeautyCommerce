namespace BeautyCommerce.Application.Common.Models;

public class DriveSyncResult
{
    public List<DriveSyncMatch> UpdatedProducts { get; set; } = new();

    public List<string> UnmatchedFiles { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public int FilesProcessed { get; set; }
}

public class DriveSyncMatch
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SourceFile { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}
