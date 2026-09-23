namespace TreningsAppHaffi.Data;

public class NavJob
{
    public int Id { get; set; }

    public string NavId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Employer { get; set; }

    public string? Municipality { get; set; }

    public DateTime? PublishedDate { get; set; }

    public DateTime? Deadline { get; set; }

    public string? Position { get; set; }

    public string? Url { get; set; }

    public string? Status { get; set; }

    public DateTime? LastModified { get; set; }

    public bool IsNew { get; set; }
}