using porter_of_call.Models;
using Spectre.Console;

namespace porter_of_call.UI;

public static class NewsPanel
{
    public static Panel Render(List<NewsArticle> news, int articles = 4)
    {
        var rows = news.TakeLast(articles).Reverse().ToList();
        var lines = new List<string>();

        foreach (var article in rows)
        {
            string location = string.IsNullOrWhiteSpace(article.Region) ? article.Scope : article.Region!;
            lines.Add($"[{article.Color}]● {Markup.Escape(article.Title)}[/] [dim]Day {article.Day} • {Markup.Escape(location)}[/]");
            lines.Add($"[grey]{Markup.Escape(article.Body)}[/]");
            lines.Add("");
        }

        if (lines.Count == 0)
            lines.Add("[dim]No market news yet.[/]");

        var markup = new Markup(string.Join("\n", lines).TrimEnd());
        return new Panel(markup)
        {
            Header = new PanelHeader("[bold cyan]  NEWS WIRE  [/]"),
            Border = BoxBorder.Rounded,
            Expand = true,
            Padding = new Padding(1, 0),
        };
    }
}
