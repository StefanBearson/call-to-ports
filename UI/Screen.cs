using porter_of_call.Data;
using porter_of_call.Engine;
using porter_of_call.Models;
using porter_of_call.Persistence;
using Spectre.Console;

namespace porter_of_call.UI;

public class Screen
{
    private const int PreferredWindowWidth = 132;
    private const int PreferredWindowHeight = 42;

    private readonly GameEngine _engine;
    private readonly GameSaveStore _saveStore = new();

    public Screen(GameEngine engine) => _engine = engine;

    private enum CommandAction
    {
        None,
        Sail,
        Buy,
        Sell,
        Hire,
        Fire,
        View,
        Fleet,
        WorldPrices,
        Save,
        Load,
        Pause,
        Help,
        Quit,
        Victory,
        GameOver
    }

    // ── Main loop ─────────────────────────────────────────────────────────

    public void Run()
    {
        PrepareWindow();

        while (true)
        {
            CommandAction action = WaitForCommand();

            if (action == CommandAction.Victory)
            {
                ClearScreen();
                AnsiConsole.MarkupLine("\n[bold green]🎉 VICTORY! You reached $1,000,000 net worth![/]");
                break;
            }
            if (action == CommandAction.GameOver)
            {
                ClearScreen();
                AnsiConsole.MarkupLine("\n[bold red]💀 GAME OVER — Bankrupt.[/]");
                break;
            }
            if (action == CommandAction.Quit)
                return;
            if (action == CommandAction.None)
                continue;
            if (action == CommandAction.Pause)
            {
                _engine.TogglePause();
                continue;
            }

            ClearScreen();
            ExecuteCommand(action);
        }
    }

    // ── Interactive command picker ─────────────────────────────────────────

    private CommandAction WaitForCommand()
    {
        CommandAction action = CommandAction.None;
        var dashboard = CreateDashboardLayout();

        ClearScreen();
        AnsiConsole.Live(dashboard)
            .AutoClear(true)
            .Start(ctx =>
            {
                while (action == CommandAction.None)
                {
                    _engine.SyncTime();

                    if (_engine.IsVictory)
                    {
                        action = CommandAction.Victory;
                        break;
                    }

                    if (_engine.IsGameOver)
                    {
                        action = CommandAction.GameOver;
                        break;
                    }

                    UpdateDashboard(dashboard);
                    ctx.Refresh();

                    if (Console.KeyAvailable)
                        action = ReadCommand(Console.ReadKey(intercept: true).Key);

                    if (action == CommandAction.None)
                        Thread.Sleep(250);
                }
            });

        return action;
    }

    private CommandAction ReadCommand(ConsoleKey key) => key switch
    {
        ConsoleKey.S => CommandAction.Sail,
        ConsoleKey.B => CommandAction.Buy,
        ConsoleKey.L => CommandAction.Sell,
        ConsoleKey.H => CommandAction.Hire,
        ConsoleKey.X => CommandAction.Fire,
        ConsoleKey.V => CommandAction.View,
        ConsoleKey.F => CommandAction.Fleet,
        ConsoleKey.W => CommandAction.WorldPrices,
        ConsoleKey.G => CommandAction.Save,
        ConsoleKey.D => CommandAction.Load,
        ConsoleKey.P => CommandAction.Pause,
        ConsoleKey.C => CommandAction.Help,
        ConsoleKey.Q => CommandAction.Quit,
        _ => CommandAction.None
    };

    private void ExecuteCommand(CommandAction action)
    {
        switch (action)
        {
            case CommandAction.Sail:
                DoSail();
                break;
            case CommandAction.Buy:
                DoBuy();
                break;
            case CommandAction.Sell:
                DoSell();
                break;
            case CommandAction.Hire:
                DoHire();
                break;
            case CommandAction.Fire:
                DoFire();
                break;
            case CommandAction.View:
                DoView();
                break;
            case CommandAction.Fleet:
                PrintFleet();
                break;
            case CommandAction.WorldPrices:
                PrintWorldPrices();
                break;
            case CommandAction.Save:
                DoSave();
                break;
            case CommandAction.Load:
                DoLoad();
                break;
            case CommandAction.Help:
                PrintHelp();
                break;
        }
    }

    // ── Command implementations ───────────────────────────────────────────

    private Layout CreateDashboardLayout()
    {
        var layout = new Layout("Root");
        var body = new Layout("Body");
        body.SplitRows(
            new Layout("Fleet"),
            new Layout("Port"),
            new Layout("News"),
            new Layout("Log"));

        layout.SplitRows(
            new Layout("Header").Size(5),
            body,
            new Layout("Footer").Size(5));
        UpdateDashboard(layout);
        return layout;
    }

    private void UpdateDashboard(Layout layout)
    {
        var player = _engine.Player;
        var headerGrid = new Grid();
        headerGrid.AddColumn();
        headerGrid.AddColumn();
        headerGrid.AddColumn();
        headerGrid.AddColumn();
        headerGrid.AddRow(
            new Markup("[bold cyan]CALL TO PORTS[/]"),
            new Markup($"[dim]Time[/] [yellow]{player.ClockText}[/]"),
            new Markup($"[dim]State[/] [{(_engine.IsPaused ? "yellow" : "green")}]{(_engine.IsPaused ? "Paused" : "Running")}[/]"),
            new Markup($"[dim]View[/] [dim]{PreferredWindowWidth}x{PreferredWindowHeight}[/]"));
        headerGrid.AddRow(
            new Markup($"[dim]Balance[/] [green]${player.Balance:N0}[/]"),
            new Markup($"[dim]Net Worth[/] [cyan]${player.NetWorth:N0}[/]"),
            new Markup("[dim]Goal[/] [dim]$1,000,000[/]"),
            new Markup(""));

        var header = new Panel(headerGrid)
        {
            Border = BoxBorder.Rounded,
            Expand = true,
            Padding = new Padding(1, 0)
        };

        var footerGrid = new Grid();
        footerGrid.AddColumn();
        footerGrid.AddColumn();
        footerGrid.AddColumn();
        footerGrid.AddColumn();
        footerGrid.AddColumn();
        footerGrid.AddColumn();
        footerGrid.AddRow(
            new Markup("[dim][[S]][/] sail"),
            new Markup("[dim][[B]][/] buy"),
            new Markup("[dim][[L]][/] sell"),
            new Markup("[dim][[H]][/] hire"),
            new Markup("[dim][[X]][/] fire"),
            new Markup(""));
        footerGrid.AddRow(
            new Markup("[dim][[V]][/] view"),
            new Markup("[dim][[F]][/] fleet"),
            new Markup("[dim][[W]][/] world"),
            new Markup("[dim][[G]][/] save"),
            new Markup("[dim][[D]][/] load"),
            new Markup($"[dim][[P]][/] {(_engine.IsPaused ? "resume" : "pause")}"));
        footerGrid.AddRow(
            new Markup("[dim][[C]][/] help"),
            new Markup("[dim][[Q]][/] quit"),
            new Markup(GetViewportStatusMarkup()),
            new Markup(""),
            new Markup(""),
            new Markup(""));

        var footer = new Panel(footerGrid)
        {
            Border = BoxBorder.Rounded,
            Expand = true,
            Padding = new Padding(1, 0)
        };

        layout["Header"].Update(header);
        layout["Fleet"].Update(FleetPanel.Render(_engine));
        layout["Port"].Update(PortPanel.Render(_engine));
        layout["News"].Update(NewsPanel.Render(_engine.News));
        layout["Log"].Update(EventLogPanel.Render(_engine.Log, lines: 8));
        layout["Footer"].Update(footer);
    }

    private void DoSail()
    {
        var ship = PickShip("Which ship should sail?");
        if (ship is null) return;

        var destinations = PortDefinitions.All
            .Where(p => p != ship.CurrentPort)
            .OrderBy(p => p.Name)
            .Select(p =>
            {
                double hours = _engine.GetTravelHours(ship.Spec, ship.CurrentPort.Name, p.Name);
                return $"{p.Name}  ({GameTime.FormatDuration(hours)})";
            })
            .ToList();

        string? picked = PromptSelection(
            $"[grey]Destination for[/] [yellow]{ship.Name}[/][grey]?[/]",
            destinations,
            pageSize: 14);
        if (picked is null) return;

        string portName = picked.Split("  ")[0].Trim();
        AnsiConsole.MarkupLine(_engine.Sail(ship.Name, portName));
    }

    private void DoBuy()
    {
        var ship = PickShip("Buy cargo for which ship?", onlyAtPort: true);
        if (ship is null) return;

        var availableCargo = CargoDefinitions.All
            .Where(ship.Spec.CanCarry)
            .Where(c => _engine.Market.AvailableTonnes(ship.CurrentPort, c) > 0)
            .OrderBy(c => c.Category).ThenBy(c => c.Name)
            .ToList();

        if (!availableCargo.Any())
        {
            AnsiConsole.MarkupLine("[red]No compatible cargo available at this port.[/]");
            return;
        }

        var choices = availableCargo.Select(c =>
        {
            double price = _engine.Market.BuyPrice(ship.CurrentPort, c);
            int avail    = _engine.Market.AvailableTonnes(ship.CurrentPort, c);
            string ships = ShipTypeDisplay.FormatList(c.CompatibleShips, "/");
            return $"{c.Icon} {c.Name,-18} ${price,8:N0}/t   {avail,5}t avail   {ships}";
        }).ToList();

        string? picked = PromptSelection(
            $"[grey]What cargo to buy for[/] [yellow]{ship.Name}[/][grey]? (free: {ship.FreeCapacity}t)[/]",
            choices,
            pageSize: 14);
        if (picked is null) return;

        var cargo = availableCargo[choices.IndexOf(picked)];

        int maxAffordable = Math.Min(
            ship.FreeCapacity,
            (int)(_engine.Player.Balance / _engine.Market.BuyPrice(ship.CurrentPort, cargo)));
        maxAffordable = Math.Min(maxAffordable, _engine.Market.AvailableTonnes(ship.CurrentPort, cargo));

        if (maxAffordable <= 0)
        {
            AnsiConsole.MarkupLine("[red]Cannot afford any — check balance or hold space.[/]");
            return;
        }

        int tonnes = PickTonnage($"How many tonnes of [yellow]{cargo.Name}[/]?", maxAffordable);
        if (tonnes <= 0) return;

        AnsiConsole.MarkupLine(_engine.Buy(ship.Name, cargo.Id, tonnes));
    }

    private void DoSell()
    {
        var ship = PickShip("Sell cargo from which ship?", onlyAtPort: true);
        if (ship is null) return;

        if (!ship.Hold.Any())
        {
            AnsiConsole.MarkupLine("[red]That ship has nothing to sell.[/]");
            return;
        }

        var choices = ship.Hold.Select(lot =>
        {
            double price  = _engine.Market.SellPrice(ship.CurrentPort, lot.Type) * lot.CurrentValue;
            double profit = (price - lot.PurchasePrice) * lot.Tonnes;
            string pStr   = profit >= 0 ? $"[green]+${profit:N0}[/]" : $"[red]${profit:N0}[/]";
            string perish = lot.Type.PerishHours.HasValue ? $" ⏳{lot.CurrentValue:P0}" : "";
            return $"{lot.Type.Icon} {lot.Type.Name,-18} {lot.Tonnes,5}t  ${price,8:N0}/t  {pStr}{perish}";
        }).ToList();

        string? picked = PromptSelection(
            $"[grey]What to sell from[/] [yellow]{ship.Name}[/][grey]?[/]",
            choices,
            pageSize: 12);
        if (picked is null) return;

        var lot = ship.Hold[choices.IndexOf(picked)];
        int tonnes = PickTonnage($"Sell how many tonnes of [yellow]{lot.Type.Name}[/]?", lot.Tonnes);
        if (tonnes <= 0) return;

        AnsiConsole.MarkupLine(_engine.Sell(ship.Name, lot.Type.Id, tonnes));
    }

    private void DoHire()
    {
        var choices = ShipDefinitions.All.Select(s =>
            $"{s.Symbol} {ShipTypeDisplay.Format(s.Type),-14}  {s.CapacityTonnes,5}t  speed {s.BaseTravelDays:0}d  ${s.PurchaseCost:N0}"
        ).ToList();

        string? picked = PromptSelection("[grey]Which ship type to hire?[/]", choices, pageSize: 8);
        if (picked is null) return;

        var spec = ShipDefinitions.All[choices.IndexOf(picked)];
        AnsiConsole.MarkupLine(_engine.HireShip(spec.Type.ToString()));
    }

    private void DoFire()
    {
        var ship = PickShip("Sell which ship?", onlyAtPort: true);
        if (ship is null) return;

        bool confirm = ConfirmAction(
            $"Sell [yellow]{ship.Name}[/] for [green]${ship.Spec.PurchaseCost * 0.6:N0}[/]?");
        if (confirm)
            AnsiConsole.MarkupLine(_engine.FireShip(ship.Name));
    }

    private void DoView()
    {
        var portChoices = PortDefinitions.All.Select(p => p.Name).OrderBy(n => n).ToList();

        string? picked = PromptSelection("[grey]View which port?[/]", portChoices, pageSize: 14);
        if (picked is null) return;

        _engine.Player.SelectedPort = PortDefinitions.Get(picked);
    }

    private void DoSave()
    {
        int? slot = PickSlot("Save to which slot?", includeEmpty: true);
        if (slot is null) return;

        var slotInfo = _saveStore.GetSlots().First(info => info.Slot == slot.Value);
        if (slotInfo.Exists && !slotInfo.IsCorrupt)
        {
            bool overwrite = ConfirmAction($"Overwrite [yellow]slot {slot.Value}[/]?");
            if (!overwrite)
                return;
        }

        _saveStore.Save(slot.Value, _engine.ExportState());
        AnsiConsole.MarkupLine($"[green]Saved game to slot {slot.Value}.[/]");
    }

    private void DoLoad()
    {
        var slots = _saveStore.GetSlots();
        if (!slots.Any(slot => slot.Exists))
        {
            AnsiConsole.MarkupLine("[red]No saved games found.[/]");
            return;
        }

        int? slot = PickSlot("Load which slot?", includeEmpty: false);
        if (slot is null) return;

        try
        {
            _engine.ImportState(_saveStore.Load(slot.Value));
            AnsiConsole.MarkupLine($"[green]Loaded game from slot {slot.Value}.[/]");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
        }
    }

    // ── Shared pickers ────────────────────────────────────────────────────

    private int? PickSlot(string title, bool includeEmpty)
    {
        var slots = _saveStore.GetSlots()
            .Where(slot => includeEmpty || slot.Exists)
            .ToList();

        if (!slots.Any())
            return null;

        var choices = slots.Select(slot => slot.Label).ToList();
        string? picked = PromptSelection($"[grey]{title}[/]", choices, pageSize: 10);
        if (picked is null) return null;

        return slots[choices.IndexOf(picked)].Slot;
    }

    private Ship? PickShip(string title, bool onlyAtPort = false)
    {
        var ships = onlyAtPort
            ? _engine.Player.Fleet.Where(s => !s.IsAtSea).ToList()
            : _engine.Player.Fleet.ToList();

        if (!ships.Any())
        {
            AnsiConsole.MarkupLine(onlyAtPort
                ? "[red]All ships are at sea.[/]"
                : "[red]You have no ships.[/]");
            return null;
        }

        var choices = ships.Select(s =>
        {
            string loc = s.IsAtSea
                ? $"→ {s.Destination!.Name} ({GameTime.FormatDuration(s.HoursToArrival)}, {Math.Clamp(s.SailProgress, 0, 1):P0})"
                : s.CurrentPort.Name;
            return $"{s.Spec.Symbol} {s.Name,-12}  {ShipTypeDisplay.Format(s.Spec.Type),-16}  {loc,-22}  {s.UsedCapacity}/{s.Spec.CapacityTonnes}t";
        }).ToList();

        string? picked = PromptSelection($"[grey]{title}[/]", choices, pageSize: 10);
        if (picked is null) return null;

        return ships[choices.IndexOf(picked)];
    }

    private int PickTonnage(string title, int max)
    {
        // Offer sensible preset amounts + a custom option
        var presets = new[] { 10, 25, 50, 100, 200, 500, 1000 }
            .Where(n => n <= max)
            .Select(n => n.ToString())
            .ToList();
        presets.Add($"All ({max}t)");
        presets.Add("Custom amount");

        string? picked = PromptSelection(title, presets, pageSize: 10);
        if (picked is null) return 0;

        if (picked.StartsWith("All")) return max;
        if (picked == "Custom amount")
        {
            return PromptInt($"[grey]Enter tonnes (1–{max})[/]", 1, max) ?? 0;
        }
        return int.Parse(picked);
    }

    // ── Help / Fleet detail ───────────────────────────────────────────────

    private void PrintHelp()
    {
        ClearScreen();
        var table = new Table()
            .Title("[bold cyan]COMMAND REFERENCE[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("Action")
            .AddColumn("Description");

        table.AddRow("[bold]S[/] sail",   "Pick a ship, then a destination — ETA shown per port");
        table.AddRow("[bold]B[/] buy",    "Pick a ship, then cargo (filtered to ship type), then amount");
        table.AddRow("[bold]L[/] sell",   "Pick a ship, then cargo from its hold, then amount");
        table.AddRow("[bold]H[/] hire",   "Pick a ship type to purchase");
        table.AddRow("[bold]X[/] fire",   "Sell a docked ship for 60% of purchase price");
        table.AddRow("[bold]V[/] view",   "Switch the port panel to any port");
        table.AddRow("[bold]F[/] fleet",  "Detailed fleet status including cargo");
        table.AddRow("[bold]W[/] world",  "Show world prices for all goods across all ports");
        table.AddRow("[bold]G[/] save",   "Save the current game to one of 10 slots");
        table.AddRow("[bold]D[/] load",   "Load a saved game from one of 10 slots");
        table.AddRow("[bold]P[/] pause",  "Pause or resume time progression");
        table.AddRow("[bold]C[/] help",   "Show command reference");
        table.AddRow("[bold]Q[/] quit",   "Exit the game");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Target viewport: {PreferredWindowWidth}x{PreferredWindowHeight} for the most consistent layout.[/]");

        AnsiConsole.MarkupLine("\n[bold]CARGO TYPES[/]");
        var cargoTable = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Name").AddColumn("Category").AddColumn("Base $/t")
            .AddColumn("Ships").AddColumn("Perishable");

        foreach (var c in CargoDefinitions.All)
            cargoTable.AddRow(
                $"{c.Icon} {c.Name}", c.Category.ToString(), $"${c.BasePrice:N0}",
                string.Join(", ", c.CompatibleShips),
                c.PerishHours.HasValue ? GameTime.FormatDuration(c.PerishHours.Value) : "-");

        AnsiConsole.Write(cargoTable);

        AnsiConsole.MarkupLine("\n[bold]PORTS[/]: " +
            string.Join(", ", PortDefinitions.All.Select(p => p.Name)));

        WaitForBack();
    }

    private void PrintFleet()
    {
        ClearScreen();
        foreach (var ship in _engine.Player.Fleet)
        {
            AnsiConsole.MarkupLine($"\n[bold yellow]{ship.Spec.Symbol} {ship.Name}[/] " +
                $"({ShipTypeDisplay.Format(ship.Spec.Type)}) — [dim]Condition[/] {ship.ConditionPct:0}%");

            string loc = ship.IsAtSea
                ? $"Sailing → {ship.Destination!.Name} ({GameTime.FormatDuration(ship.HoursToArrival)})"
                : $"Docked at {ship.CurrentPort.Name}";
            AnsiConsole.MarkupLine($"  📍 {loc}");
            if (ship.IsAtSea)
                AnsiConsole.MarkupLine($"  🧭 Progress: [cyan]{Math.Clamp(ship.SailProgress, 0, 1):P0}[/]");
            AnsiConsole.MarkupLine($"  📦 Hold: {ship.UsedCapacity}/{ship.Spec.CapacityTonnes}t");

            if (ship.Hold.Count > 0)
                foreach (var lot in ship.Hold)
                {
                    string perish = lot.Type.PerishHours.HasValue
                        ? $" [yellow](value {lot.CurrentValue:P0})[/]" : "";
                    AnsiConsole.MarkupLine($"     {lot.Type.Icon} {lot.Tonnes}t {lot.Type.Name} " +
                        $"@ ${lot.PurchasePrice:N0}/t{perish}");
                }
            else
                AnsiConsole.MarkupLine("     [dim](empty hold)[/]");
        }
        WaitForBack();
    }

    private void PrintWorldPrices()
    {
        ClearScreen();

        var table = new Table()
            .Title("[bold cyan]WORLD PRICES[/]")
            .Border(TableBorder.Rounded)
            .Expand()
            .AddColumn(new TableColumn("").Width(4).Centered())
            .AddColumn(new TableColumn("Goods").Width(14).NoWrap())
            .AddColumn(new TableColumn("Best Buy").Width(20).NoWrap())
            .AddColumn(new TableColumn("Best Sell").Width(20).NoWrap())
            .AddColumn(new TableColumn("World").Width(7).NoWrap())
            .AddColumn(new TableColumn("Trend").Width(8).NoWrap())
            .AddColumn(new TableColumn("Spread").Width(9).NoWrap())
            .AddColumn(new TableColumn("Stock").Width(7).NoWrap())
            .AddColumn(new TableColumn("Ships").Width(26).NoWrap());

        bool firstCategory = true;
        foreach (var group in CargoDefinitions.All
                     .OrderBy(c => c.Category)
                     .ThenBy(c => c.Name)
                     .GroupBy(c => c.Category))
        {
            if (!firstCategory)
                table.AddEmptyRow();

            table.AddRow(
                "",
                $"[bold cyan]{FormatCategoryLabel(group.Key)}[/]",
                "",
                "",
                "",
                "",
                "",
                "",
                "");

            foreach (var cargo in group)
            {
                var buyMarket = PortDefinitions.All
                    .Select(port => new
                    {
                        Port = port,
                        Buy = _engine.Market.BuyPrice(port, cargo),
                        Sell = _engine.Market.SellPrice(port, cargo),
                        Stock = _engine.Market.AvailableTonnes(port, cargo)
                    })
                    .ToList();

                var bestBuy = buyMarket.OrderBy(entry => entry.Buy).First();
                var bestSell = buyMarket.OrderByDescending(entry => entry.Sell).First();
                double spread = bestSell.Sell - bestBuy.Buy;
                double worldDemand = _engine.Market.GetGlobalDemand(cargo.Id);
                double trend = _engine.Market.GetTrend(cargo.Id);
                string spreadMarkup = spread >= 0
                    ? $"[green]+${spread:N0}[/]"
                    : $"[red]${spread:N0}[/]";
                string worldMarkup = worldDemand >= 1.10
                    ? $"[red]{worldDemand:0.00}x[/]"
                    : worldDemand <= 0.92
                        ? $"[green]{worldDemand:0.00}x[/]"
                        : $"[yellow]{worldDemand:0.00}x[/]";
                string trendMarkup = trend >= 0.01
                    ? $"[red]▲ {trend:P0}[/]"
                    : trend <= -0.01
                        ? $"[green]▼ {Math.Abs(trend):P0}[/]"
                        : "[yellow]→ flat[/]";
                string stockMarkup = bestBuy.Stock >= 1000
                    ? $"[green]{bestBuy.Stock}t[/]"
                    : bestBuy.Stock >= 300
                        ? $"[yellow]{bestBuy.Stock}t[/]"
                        : $"[red]{bestBuy.Stock}t[/]";

                table.AddRow(
                    FormatIconCell(cargo.Icon),
                    Markup.Escape(cargo.Name),
                    $"{bestBuy.Port.Name} [dim]${bestBuy.Buy:N0}[/]",
                    $"{bestSell.Port.Name} [dim]${bestSell.Sell:N0}[/]",
                    worldMarkup,
                    trendMarkup,
                    spreadMarkup,
                    stockMarkup,
                    Markup.Escape(FormatShipTypesCompact(cargo.CompatibleShips)));
            }

            firstCategory = false;
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[dim]Press Enter or Esc to go back.[/]");
        WaitForBack();
    }

    // ── Utility ───────────────────────────────────────────────────────────

    private static string? PromptSelection(string title, IReadOnlyList<string> choices, int pageSize)
    {
        if (choices.Count == 0)
            return null;

        int index = 0;

        while (true)
        {
            ClearScreen();

            int start = Math.Max(0, Math.Min(index - pageSize / 2, Math.Max(0, choices.Count - pageSize)));
            int end = Math.Min(choices.Count, start + pageSize);

            var table = new Table()
                .Title(title)
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("").NoWrap().Width(2))
                .AddColumn(new TableColumn("Option"));

            for (int i = start; i < end; i++)
            {
                string marker = i == index ? "[yellow]>[/]" : " ";
                table.AddRow(marker, Markup.Escape(choices[i]));
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("\n[dim]↑/↓ move  Enter select  Esc back[/]");

            var key = Console.ReadKey(intercept: true).Key;
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    index = (index - 1 + choices.Count) % choices.Count;
                    break;
                case ConsoleKey.DownArrow:
                    index = (index + 1) % choices.Count;
                    break;
                case ConsoleKey.PageUp:
                    index = Math.Max(0, index - pageSize);
                    break;
                case ConsoleKey.PageDown:
                    index = Math.Min(choices.Count - 1, index + pageSize);
                    break;
                case ConsoleKey.Home:
                    index = 0;
                    break;
                case ConsoleKey.End:
                    index = choices.Count - 1;
                    break;
                case ConsoleKey.Enter:
                    return choices[index];
                case ConsoleKey.Escape:
                    return null;
            }
        }
    }

    private static bool ConfirmAction(string message)
    {
        bool confirmed = false;

        while (true)
        {
            ClearScreen();
            AnsiConsole.Write(new Panel(new Markup(message))
            {
                Border = BoxBorder.Rounded,
                Expand = true,
                Header = new PanelHeader("[bold cyan]CONFIRM[/]")
            });
            AnsiConsole.MarkupLine("\n[dim][Y] yes  [N] no  Enter = no  Esc = back[/]");
            AnsiConsole.MarkupLine(confirmed ? "[green]Current: Yes[/]" : "[yellow]Current: No[/]");

            var key = Console.ReadKey(intercept: true).Key;
            switch (key)
            {
                case ConsoleKey.Y:
                case ConsoleKey.RightArrow:
                    confirmed = true;
                    break;
                case ConsoleKey.N:
                case ConsoleKey.LeftArrow:
                    confirmed = false;
                    break;
                case ConsoleKey.Enter:
                    return confirmed;
                case ConsoleKey.Escape:
                    return false;
            }
        }
    }

    private static int? PromptInt(string title, int min, int max)
    {
        var buffer = "";
        string? error = null;

        while (true)
        {
            ClearScreen();
            AnsiConsole.Write(new Panel(new Markup($"{title}\n[bold yellow]{Markup.Escape(buffer)}[/]"))
            {
                Border = BoxBorder.Rounded,
                Expand = true
            });

            if (!string.IsNullOrEmpty(error))
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(error)}[/]");

            AnsiConsole.MarkupLine("\n[dim]Type a number, Backspace edits, Enter confirms, Esc goes back[/]");

            var keyInfo = Console.ReadKey(intercept: true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.Escape:
                    return null;
                case ConsoleKey.Backspace:
                    if (buffer.Length > 0)
                        buffer = buffer[..^1];
                    error = null;
                    break;
                case ConsoleKey.Enter:
                    if (int.TryParse(buffer, out int value) && value >= min && value <= max)
                        return value;
                    error = $"Enter a value between {min} and {max}.";
                    break;
                default:
                    if (char.IsDigit(keyInfo.KeyChar))
                    {
                        buffer += keyInfo.KeyChar;
                        error = null;
                    }
                    break;
            }
        }
    }

    private static void WaitForBack()
    {
        AnsiConsole.MarkupLine("\n[dim]Press Enter or Esc to go back...[/]");
        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key is ConsoleKey.Enter or ConsoleKey.Escape)
                return;
        }
    }

    private static string GetViewportStatusMarkup()
    {
        return Console.WindowWidth == PreferredWindowWidth && Console.WindowHeight == PreferredWindowHeight
            ? "[green]fixed viewport active[/]"
            : $"[yellow]best at {PreferredWindowWidth}x{PreferredWindowHeight}[/]";
    }

    private static string FormatCategoryLabel(CargoCategory category) => category switch
    {
        CargoCategory.Agricultural => "Agricultural",
        CargoCategory.Energy => "Energy",
        CargoCategory.Metals => "Metals",
        CargoCategory.Forestry => "Forestry",
        CargoCategory.Manufactured => "Manufactured",
        _ => category.ToString()
    };

    private static string FormatShipTypesCompact(IEnumerable<ShipType> types) =>
        string.Join("/", types.Select(type => type switch
        {
            ShipType.Clipper => "Clip",
            ShipType.Freighter => "Frt",
            ShipType.ContainerShip => "Cont",
            ShipType.BulkCarrier => "Bulk",
            ShipType.Tanker => "Tank",
            ShipType.Reefer => "Reef",
            ShipType.Coaster => "Coast",
            ShipType.Multipurpose => "Multi",
            ShipType.FeederContainer => "Feed",
            ShipType.HeavyFreighter => "Heavy",
            ShipType.OreCarrier => "Ore",
            ShipType.LogCarrier => "Log",
            ShipType.ChemicalTanker => "Chem",
            ShipType.GasCarrier => "Gas",
            ShipType.FastReefer => "Fast",
            ShipType.RoRo => "RoRo",
            _ => type.ToString()
        }));

    private static string FormatIconCell(string icon) =>
        Markup.Escape(icon);

    private static void ClearScreen()
    {
        PrepareWindow();
        AnsiConsole.Clear();
    }

    private static void PrepareWindow()
    {
        Console.CursorVisible = false;

        if (Console.WindowWidth == PreferredWindowWidth && Console.WindowHeight == PreferredWindowHeight)
            return;

        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            if (Console.BufferWidth < PreferredWindowWidth || Console.BufferHeight < PreferredWindowHeight)
            {
                int bufferWidth = Math.Max(Console.BufferWidth, PreferredWindowWidth);
                int bufferHeight = Math.Max(Console.BufferHeight, PreferredWindowHeight);
                Console.SetBufferSize(bufferWidth, bufferHeight);
            }

            int width = Math.Min(PreferredWindowWidth, Console.LargestWindowWidth);
            int height = Math.Min(PreferredWindowHeight, Console.LargestWindowHeight);

            if (Console.WindowWidth != width || Console.WindowHeight != height)
                Console.SetWindowSize(width, height);

            if (Console.BufferWidth != width || Console.BufferHeight != height)
                Console.SetBufferSize(width, height);
        }
        catch (IOException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }
}
