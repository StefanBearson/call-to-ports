namespace porter_of_call.Models;

public sealed class NewsArticle
{
    public int Day { get; init; }
    public string Scope { get; init; } = "Global";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public string Color { get; init; } = "yellow";
    public string? Region { get; init; }
}
