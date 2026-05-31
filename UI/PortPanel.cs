using porter_of_call.Data;
using porter_of_call.Engine;
using porter_of_call.Models;
using Spectre.Console;

namespace porter_of_call.UI;

public static class PortPanel
{
    public static Panel Render(GameEngine engine)
    {
        var port = engine.Player.SelectedPort;
        if (port is null)
            return new Panel("No port selected.") { Header = new PanelHeader("[bold cyan]  PORT  [/]"), Border = BoxBorder.Rounded, Expand = true };

        var table = new Table()
            .NoBorder()
            .AddColumn(new TableColumn("[dim]Cargo[/]").Width(14))
            .AddColumn(new TableColumn("[dim]Ships[/]").Width(120))
            .AddColumn(new TableColumn("[dim]Avail[/]").Width(7))
            .AddColumn(new TableColumn("[dim]Buy $[/]").Width(8))
            .AddColumn(new TableColumn("[dim]Sell $[/]").Width(8))
            .AddColumn(new TableColumn("[dim]D/S[/]").Width(5));

        // Show all cargo types relevant to this port (exports + imports + popular)
        var relevantIds = new HashSet<string>(port.SpecialisedExports.Concat(port.SpecialisedImports));
        var cargoList = CargoDefinitions.All
            .Where(c => relevantIds.Contains(c.Id))
            .OrderBy(c => c.Category)
            .ToList();

        if (cargoList.Count == 0)
            cargoList = CargoDefinitions.All.Take(6).ToList();

        foreach (var cargo in cargoList)
        {
            double demand = port.Demand.GetValueOrDefault(cargo.Id, 0.5);
            double supply = port.Supply.GetValueOrDefault(cargo.Id, 0.5);
            double buy    = engine.Market.BuyPrice(port, cargo);
            double sell   = engine.Market.SellPrice(port, cargo);
            int avail     = engine.Market.AvailableTonnes(port, cargo);
            string ships  = ShipTypeDisplay.FormatList(cargo.CompatibleShips, "/");

            // Colour based on demand vs supply
            string demandStr = demand > supply + 0.2
                ? $"[green]{demand:0.0}↑[/]"
                : demand < supply - 0.2
                    ? $"[red]{demand:0.0}↓[/]"
                    : $"[dim]{demand:0.0}[/]";

            string availStr = avail > 500 ? $"[green]{avail}[/]" :
                              avail > 100 ? $"[yellow]{avail}[/]" : $"[red]{avail}[/]";

            table.AddRow(
                $"{cargo.Icon} {cargo.Name}",
                ships,
                availStr,
                $"${buy:N0}",
                $"${sell:N0}",
                demandStr);
        }

        return new Panel(table)
        {
            Header  = new PanelHeader($"[bold cyan]  {port.Name.ToUpper()}  [/]"),
            Border  = BoxBorder.Rounded,
            Expand = true,
            Padding = new Padding(1, 0),
        };
    }
}
