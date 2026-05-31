using Spectre.Console;

namespace porter_of_call.UI;

public static class EventLogPanel
{
    public static Panel Render(List<string> log, int lines = 8)
    {
        var recent = log.TakeLast(lines).ToList();
        var markup = new Markup(string.Join("\n", recent).TrimEnd());

        return new Panel(markup)
        {
            Header  = new PanelHeader("[bold cyan]  OPERATIONS LOG  [/]"),
            Border  = BoxBorder.Rounded,
            Expand = true,
            Padding = new Padding(1, 0),
        };
    }
}
