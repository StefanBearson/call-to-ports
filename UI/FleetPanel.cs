using porter_of_call.Engine;
using porter_of_call.Models;
using Spectre.Console;

namespace porter_of_call.UI;

public static class FleetPanel
{
    public static Panel Render(GameEngine engine)
    {
        var table = new Table()
            .NoBorder()
            .AddColumn(new TableColumn("[dim]Sym[/]").Width(3))
            .AddColumn(new TableColumn("[dim]Name[/]").Width(12))
            .AddColumn(new TableColumn("[dim]Type[/]").Width(16))
            .AddColumn(new TableColumn("[dim]Location[/]").Width(14))
            .AddColumn(new TableColumn("[dim]Trip[/]").Width(7))
            .AddColumn(new TableColumn("[dim]Hold[/]").Width(12))
            .AddColumn(new TableColumn("[dim]Cond[/]").Width(6));

        foreach (var ship in engine.Player.Fleet)
        {
            string sym      = $"[yellow]{ship.Spec.Symbol}[/]";
            string name     = ship.Name;
            string type     = ShipTypeDisplay.Format(ship.Spec.Type);
            string location = ship.IsAtSea
                ? $"→ {ship.Destination!.Name} ({GameTime.FormatDuration(ship.HoursToArrival)})"
                : $"[green]{ship.CurrentPort.Name}[/]";
            string progress = ship.IsAtSea
                ? $"[cyan]{Math.Clamp(ship.SailProgress, 0, 1):P0}[/]"
                : "[grey]--[/]";
            string hold     = $"{ship.UsedCapacity}/{ship.Spec.CapacityTonnes}t";
            string cond     = ship.ConditionPct >= 70
                ? $"[green]{ship.ConditionPct:0}%[/]"
                : ship.ConditionPct >= 40
                    ? $"[yellow]{ship.ConditionPct:0}%[/]"
                    : $"[red]{ship.ConditionPct:0}%[/]";

            table.AddRow(sym, name, type, location, progress, hold, cond);
        }

        return new Panel(table)
        {
            Header  = new PanelHeader("[bold cyan]  YOUR FLEET  [/]"),
            Border  = BoxBorder.Rounded,
            Expand = true,
            Padding = new Padding(1, 0),
        };
    }
}
