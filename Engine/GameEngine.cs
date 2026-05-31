using porter_of_call.Data;
using porter_of_call.Economy;
using porter_of_call.Models;
using porter_of_call.Persistence;
using Spectre.Console;

namespace porter_of_call.Engine;

/// <summary>
/// Central game state and logic. All mutation goes through here.
/// </summary>
public class GameEngine
{
    public Player Player { get; }
    public List<Port> Ports { get; }
    public Market Market { get; }
    public List<string> Log { get; } = new();
    public List<NewsArticle> News { get; } = new();
    public bool IsPaused { get; private set; }

    private readonly EventSystem _events;
    private DateTime _lastWallClockSync = DateTime.UtcNow;

    private static readonly Dictionary<(string, string), double> DistanceMultiplier = BuildDistances();

    public GameEngine()
    {
        Ports = PortDefinitions.All;
        Market = new Market();
        Player = new Player { SelectedPort = Ports[0] };
        _events = new EventSystem();

        ResetPortMarkets(Ports);
        Market.Tick(Ports, 1);
        Market.Tick(Ports, 2);

        var ny = PortDefinitions.Get("New York");
        var spec = ShipDefinitions.Get(ShipType.Clipper);
        Player.Fleet.Add(new Ship { Name = "Albatross", Spec = spec, CurrentPort = ny });

        AddLog("Welcome to Call to Ports! Type [cyan]help[/] to see commands.");
        AddLog("Time is continuous: [cyan]1 real minute = 1 game day[/].");
        AddNews(new NewsArticle
        {
            Day = Player.Day,
            Scope = "Global",
            Title = "Freight Wire Opens for Trading",
            Body = "Analysts expect more volatile commodity cycles, stronger regional differences, and slower price mean reversion than before.",
            Color = "cyan"
        });
        AddLog($"Your clipper [yellow]Albatross[/] is docked at [green]{ny.Name}[/].");
    }

    public void SyncTime()
    {
        DateTime now = DateTime.UtcNow;
        if (IsPaused)
        {
            _lastWallClockSync = now;
            return;
        }

        TimeSpan elapsedRealTime = now - _lastWallClockSync;
        _lastWallClockSync = now;

        double gameHours = GameTime.ToGameHours(elapsedRealTime);
        if (gameHours > 0)
            AdvanceGameHours(gameHours);
    }

    public string TogglePause()
    {
        _lastWallClockSync = DateTime.UtcNow;
        IsPaused = !IsPaused;

        string msg = IsPaused ? "Time paused." : "Time resumed.";
        AddLog(IsPaused ? "⏸ Time paused." : "▶ Time resumed.");
        return msg;
    }

    private void AdvanceGameHours(double hours)
    {
        if (hours <= 0)
            return;

        double previousHours = Player.GameHoursElapsed;
        Player.GameHoursElapsed += hours;

        AdvanceShips(hours);
        ApplyMaintenance(hours);

        int previousDay = (int)Math.Floor(previousHours / GameTime.HoursPerDay) + 1;
        int currentDay = Player.Day;

        for (int day = previousDay + 1; day <= currentDay; day++)
        {
            Market.Tick(Ports, day);

            foreach (var article in _events.RollNews(day, Market, Ports))
                AddNews(article);
        }
    }

    private void AdvanceShips(double elapsedHours)
    {
        foreach (var ship in Player.Fleet)
        {
            if (!ship.IsAtSea)
                continue;

            double hoursAtSea = Math.Min(ship.HoursToArrival, elapsedHours);
            foreach (var lot in ship.Hold)
                lot.HoursOld += hoursAtSea;

            ship.HoursToArrival -= elapsedHours;
            if (ship.HoursToArrival <= 0)
                ArriveAtPort(ship);
        }
    }

    private void ApplyMaintenance(double elapsedHours)
    {
        double costs = Player.Fleet.Sum(s => s.Spec.DailyCost) * (elapsedHours / GameTime.HoursPerDay);
        Player.Balance -= costs;
    }

    private void ArriveAtPort(Ship ship)
    {
        ship.CurrentPort = ship.Destination!;
        ship.Destination = null;
        ship.HoursToArrival = 0;
        ship.TotalRouteHours = 0;
        AddLog($"⚓ [yellow]{ship.Name}[/] arrived at [green]{ship.CurrentPort.Name}[/].");
    }

    public string Buy(string shipName, string cargoId, int tonnes)
    {
        var ship = FindShip(shipName);
        if (ship is null) return Err($"Unknown ship: [yellow]{shipName}[/]");
        if (ship.IsAtSea) return Err($"[yellow]{ship.Name}[/] is at sea.");

        var cargo = FindCargo(cargoId);
        if (cargo is null) return Err($"Unknown cargo: [yellow]{cargoId}[/]. Check [cyan]help[/] for cargo IDs.");

        if (!ship.Spec.CanCarry(cargo))
        {
            string requiredShips = ShipTypeDisplay.FormatList(cargo.CompatibleShips);
            return Err($"[yellow]{ship.Name}[/] ({ShipTypeDisplay.Format(ship.Spec.Type)}) cannot carry {cargo.Name}. Requires: {requiredShips}.");
        }

        if (tonnes > ship.FreeCapacity)
            return Err($"Not enough hold space. Free: {ship.FreeCapacity}t.");

        int available = Market.AvailableTonnes(ship.CurrentPort, cargo);
        if (tonnes > available)
            return Err($"Only {available}t of {cargo.Name} available at {ship.CurrentPort.Name}.");

        double price = Market.BuyPrice(ship.CurrentPort, cargo);
        double total = price * tonnes;
        if (total > Player.Balance)
            return Err($"Insufficient funds. Need [red]${total:N0}[/], have [green]${Player.Balance:N0}[/].");

        Player.Balance -= total;
        Market.RecordPurchase(ship.CurrentPort, cargo.Id, tonnes);

        var existing = ship.Hold.FirstOrDefault(l => l.Type.Id == cargo.Id);
        if (existing is not null)
        {
            double newAvg = (existing.PurchasePrice * existing.Tonnes + price * tonnes)
                          / (existing.Tonnes + tonnes);
            existing.Tonnes += tonnes;
            existing.PurchasePrice = newAvg;
        }
        else
        {
            ship.Hold.Add(new CargoLot { Type = cargo, Tonnes = tonnes, PurchasePrice = price });
        }

        string msg = $"Bought {tonnes}t {cargo.Name} @ ${price:N0}/t = [red]-${total:N0}[/]";
        AddLog($"🛒 {msg}");
        return msg;
    }

    public string Sell(string shipName, string cargoId, int tonnes)
    {
        var ship = FindShip(shipName);
        if (ship is null) return Err($"Unknown ship: [yellow]{shipName}[/]");
        if (ship.IsAtSea) return Err($"[yellow]{ship.Name}[/] is at sea.");

        var lot = ship.Hold.FirstOrDefault(l =>
            l.Type.Id.Equals(cargoId, StringComparison.OrdinalIgnoreCase) ||
            l.Type.Name.Equals(cargoId, StringComparison.OrdinalIgnoreCase) ||
            NormaliseName(l.Type.Name) == NormaliseName(cargoId));
        if (lot is null) return Err($"[yellow]{ship.Name}[/] has no [yellow]{cargoId}[/] in hold.");

        int actualTonnes = Math.Min(tonnes, lot.Tonnes);
        double price = Market.SellPrice(ship.CurrentPort, lot.Type) * lot.CurrentValue;
        double total = price * actualTonnes;
        double profit = (price - lot.PurchasePrice) * actualTonnes;

        Player.Balance += total;
        Market.RecordSale(ship.CurrentPort, lot.Type.Id, actualTonnes);

        lot.Tonnes -= actualTonnes;
        if (lot.Tonnes <= 0) ship.Hold.Remove(lot);

        string profitStr = profit >= 0
            ? $"[green]+${profit:N0}[/] profit"
            : $"[red]${profit:N0}[/] loss";
        string msg = $"Sold {actualTonnes}t {lot.Type.Name} @ ${price:N0}/t = [green]+${total:N0}[/] ({profitStr})";
        AddLog($"💰 {msg}");
        return msg;
    }

    public string Sail(string shipName, string portName)
    {
        var ship = FindShip(shipName);
        if (ship is null) return Err($"Unknown ship: [yellow]{shipName}[/]");
        if (ship.IsAtSea) return Err($"[yellow]{ship.Name}[/] is already sailing to {ship.Destination!.Name}.");

        var port = PortDefinitions.TryGet(portName);
        if (port is null) return Err($"Unknown port: [yellow]{portName}[/]. Ports: {string.Join(", ", PortDefinitions.All.Select(p => p.Name))}");
        if (port == ship.CurrentPort) return Err($"[yellow]{ship.Name}[/] is already at {port.Name}.");

        double routeHours = GetTravelHours(ship.Spec, ship.CurrentPort.Name, port.Name);
        ship.Destination = port;
        ship.HoursToArrival = routeHours;
        ship.TotalRouteHours = routeHours;

        string msg = $"{ship.Name} sailing to {port.Name} — ETA {GameTime.FormatDuration(routeHours)}.";
        AddLog($"⛵ {msg}");
        return msg;
    }

    public string HireShip(string typeName)
    {
        var spec = ShipDefinitions.TryGet(typeName);
        if (spec is null) return Err($"Unknown ship type: [yellow]{typeName}[/]. Types: {string.Join(", ", Enum.GetNames<ShipType>())}");

        if (Player.Balance < spec.PurchaseCost)
            return Err($"Need [red]${spec.PurchaseCost:N0}[/], have [green]${Player.Balance:N0}[/].");

        Player.Balance -= spec.PurchaseCost;
        var names = new[] { "Meridian", "Poseidon", "Triton", "Nereid", "Calypso", "Horizon",
                            "Tempest", "Solstice", "Equinox", "Voyager", "Pioneer", "Nomad" };
        string name = names.FirstOrDefault(n => Player.Fleet.All(s => s.Name != n)) ?? $"Ship-{Player.Fleet.Count + 1}";

        var ship = new Ship
        {
            Name = name,
            Spec = spec,
            CurrentPort = Player.SelectedPort ?? Ports[0],
        };
        Player.Fleet.Add(ship);

        string msg = $"Hired {ShipTypeDisplay.Format(spec.Type)} '{name}' for ${spec.PurchaseCost:N0}.";
        AddLog($"🚢 {msg}");
        return msg;
    }

    public string FireShip(string shipName)
    {
        var ship = FindShip(shipName);
        if (ship is null) return Err($"Unknown ship: [yellow]{shipName}[/]");
        if (Player.Fleet.Count == 1) return Err("Cannot sell your last ship!");
        if (ship.IsAtSea) return Err("Cannot sell a ship at sea.");

        double value = ship.Spec.PurchaseCost * 0.6;
        Player.Balance += value;
        Player.Fleet.Remove(ship);
        string msg = $"Sold {ship.Name} for ${value:N0}.";
        AddLog($"⚓ {msg}");
        return msg;
    }

    public Ship? FindShip(string name) =>
        Player.Fleet.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            s.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));

    public CargoType? FindCargo(string input)
    {
        string norm = NormaliseName(input);
        return CargoDefinitions.All.FirstOrDefault(c =>
            c.Id.Equals(input, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(input, StringComparison.OrdinalIgnoreCase) ||
            NormaliseName(c.Name) == norm ||
            NormaliseName(c.Id) == norm);
    }

    public double GetTravelHours(ShipSpec spec, string fromPort, string toPort)
    {
        double multiplier = GetDistance(fromPort, toPort);
        return Math.Max(1, spec.BaseTravelDays * GameTime.HoursPerDay * multiplier);
    }

    public GameSaveData ExportState()
    {
        return new GameSaveData
        {
            IsPaused = IsPaused,
            SelectedPortName = Player.SelectedPort?.Name,
            Balance = Player.Balance,
            GameHoursElapsed = Player.GameHoursElapsed,
            Fleet = Player.Fleet.Select(ship => new ShipSaveData
            {
                Name = ship.Name,
                ShipType = ship.Spec.Type.ToString(),
                CurrentPortName = ship.CurrentPort.Name,
                DestinationPortName = ship.Destination?.Name,
                HoursToArrival = ship.HoursToArrival,
                TotalRouteHours = ship.TotalRouteHours,
                ConditionPct = ship.ConditionPct,
                Hold = ship.Hold.Select(lot => new CargoLotSaveData
                {
                    CargoId = lot.Type.Id,
                    Tonnes = lot.Tonnes,
                    HoursOld = lot.HoursOld,
                    PurchasePrice = lot.PurchasePrice
                }).ToList()
            }).ToList(),
            Ports = Ports.Select(port => new PortMarketSaveData
            {
                PortName = port.Name,
                Supply = new Dictionary<string, double>(port.Supply),
                Demand = new Dictionary<string, double>(port.Demand),
                SupplyBias = new Dictionary<string, double>(port.SupplyBias),
                DemandBias = new Dictionary<string, double>(port.DemandBias)
            }).ToList(),
            Log = [.. Log],
            News = News.Select(article => new NewsArticleSaveData
            {
                Day = article.Day,
                Scope = article.Scope,
                Title = article.Title,
                Body = article.Body,
                Color = article.Color,
                Region = article.Region
            }).ToList(),
            Market = Market.ExportState(),
            Events = _events.ExportState()
        };
    }

    public void ImportState(GameSaveData save)
    {
        IsPaused = true;
        _lastWallClockSync = DateTime.UtcNow;

        if (save.Version != GameSaveStore.SaveVersion)
            throw new InvalidDataException($"Unsupported save version {save.Version}.");

        Player.Balance = save.Balance;
        Player.GameHoursElapsed = save.GameHoursElapsed;

        Player.Fleet.Clear();
        Log.Clear();
        News.Clear();
        ResetPortMarkets(Ports);
        Market.ImportState(save.Market);
        _events.ImportState(save.Events);

        foreach (var portState in save.Ports ?? [])
        {
            var port = ResolvePort(portState.PortName);
            port.Supply.Clear();
            foreach (var entry in portState.Supply)
                port.Supply[entry.Key] = entry.Value;

            port.Demand.Clear();
            foreach (var entry in portState.Demand)
                port.Demand[entry.Key] = entry.Value;

            port.SupplyBias.Clear();
            foreach (var entry in portState.SupplyBias)
                port.SupplyBias[entry.Key] = entry.Value;

            port.DemandBias.Clear();
            foreach (var entry in portState.DemandBias)
                port.DemandBias[entry.Key] = entry.Value;
        }

        foreach (var shipState in save.Fleet ?? [])
        {
            var spec = ShipDefinitions.TryGet(shipState.ShipType)
                ?? throw new InvalidDataException($"Unknown ship type '{shipState.ShipType}' in save.");
            var currentPort = ResolvePort(shipState.CurrentPortName);
            var destination = string.IsNullOrWhiteSpace(shipState.DestinationPortName)
                ? null
                : ResolvePort(shipState.DestinationPortName);

            var ship = new Ship
            {
                Name = shipState.Name,
                Spec = spec,
                CurrentPort = currentPort,
                Destination = destination,
                HoursToArrival = destination is null ? 0 : shipState.HoursToArrival,
                TotalRouteHours = destination is null ? 0 : shipState.TotalRouteHours,
                ConditionPct = shipState.ConditionPct
            };

            foreach (var lotState in shipState.Hold)
            {
                var cargo = CargoDefinitions.TryGet(lotState.CargoId)
                    ?? throw new InvalidDataException($"Unknown cargo '{lotState.CargoId}' in save.");
                ship.Hold.Add(new CargoLot
                {
                    Type = cargo,
                    Tonnes = lotState.Tonnes,
                    HoursOld = lotState.HoursOld,
                    PurchasePrice = lotState.PurchasePrice
                });
            }

            Player.Fleet.Add(ship);
        }

        Player.SelectedPort = string.IsNullOrWhiteSpace(save.SelectedPortName)
            ? Ports[0]
            : ResolvePort(save.SelectedPortName);

        foreach (var entry in (save.Log ?? []).TakeLast(200))
            Log.Add(entry);

        foreach (var article in (save.News ?? []).TakeLast(40))
        {
            News.Add(new NewsArticle
            {
                Day = article.Day,
                Scope = article.Scope,
                Title = article.Title,
                Body = article.Body,
                Color = article.Color,
                Region = article.Region
            });
        }

        IsPaused = save.IsPaused;
        _lastWallClockSync = DateTime.UtcNow;
    }

    private static string NormaliseName(string s) =>
        s.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();

    private static Port ResolvePort(string name) =>
        PortDefinitions.All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidDataException($"Unknown port '{name}' in save.");

    private string Err(string msg)
    {
        string full = $"[red]✗[/] {msg}";
        AddLog(full);
        return full;
    }

    private void AddLog(string msg)
    {
        Log.Add($"[dim]{Player.ClockText}[/] {msg}");
        if (Log.Count > 200) Log.RemoveAt(0);
    }

    private void AddNews(NewsArticle article)
    {
        News.Add(article);
        if (News.Count > 40)
            News.RemoveAt(0);

        string location = string.IsNullOrWhiteSpace(article.Region) ? article.Scope : article.Region!;
        AddLog($"📰 [{article.Color}]{Markup.Escape(article.Title)}[/] [dim]({Markup.Escape(location)})[/]");
    }

    private static void ResetPortMarkets(IEnumerable<Port> ports)
    {
        foreach (var port in ports)
        {
            port.Supply.Clear();
            port.Demand.Clear();
            port.SupplyBias.Clear();
            port.DemandBias.Clear();
        }
    }

    private static Dictionary<(string, string), double> BuildDistances()
    {
        var raw = new List<(string, string, double)>
        {
            ("New York",    "Los Angeles",   1.2),
            ("New York",    "Santos",        0.9),
            ("New York",    "Rotterdam",     1.0),
            ("New York",    "Hamburg",       1.1),
            ("New York",    "Cape Town",     1.8),
            ("New York",    "Dubai",         2.0),
            ("New York",    "Mumbai",        2.2),
            ("New York",    "Singapore",     2.8),
            ("New York",    "Shanghai",      3.0),
            ("New York",    "Tokyo",         3.0),
            ("New York",    "Sydney",        3.2),

            ("Los Angeles", "Santos",        1.8),
            ("Los Angeles", "Rotterdam",     2.2),
            ("Los Angeles", "Hamburg",       2.3),
            ("Los Angeles", "Cape Town",     2.5),
            ("Los Angeles", "Dubai",         2.4),
            ("Los Angeles", "Mumbai",        2.2),
            ("Los Angeles", "Singapore",     1.8),
            ("Los Angeles", "Shanghai",      1.5),
            ("Los Angeles", "Tokyo",         1.2),
            ("Los Angeles", "Sydney",        1.6),

            ("Santos",      "Rotterdam",     1.1),
            ("Santos",      "Hamburg",       1.2),
            ("Santos",      "Cape Town",     1.0),
            ("Santos",      "Dubai",         1.8),
            ("Santos",      "Mumbai",        2.0),
            ("Santos",      "Singapore",     2.4),
            ("Santos",      "Shanghai",      2.6),
            ("Santos",      "Tokyo",         2.8),
            ("Santos",      "Sydney",        2.2),

            ("Rotterdam",   "Hamburg",       0.3),
            ("Rotterdam",   "Cape Town",     1.5),
            ("Rotterdam",   "Dubai",         1.4),
            ("Rotterdam",   "Mumbai",        1.6),
            ("Rotterdam",   "Singapore",     2.0),
            ("Rotterdam",   "Shanghai",      2.2),
            ("Rotterdam",   "Tokyo",         2.4),
            ("Rotterdam",   "Sydney",        2.8),

            ("Hamburg",     "Cape Town",     1.6),
            ("Hamburg",     "Dubai",         1.5),
            ("Hamburg",     "Mumbai",        1.7),
            ("Hamburg",     "Singapore",     2.1),
            ("Hamburg",     "Shanghai",      2.3),
            ("Hamburg",     "Tokyo",         2.5),
            ("Hamburg",     "Sydney",        2.9),

            ("Cape Town",   "Dubai",         1.2),
            ("Cape Town",   "Mumbai",        1.4),
            ("Cape Town",   "Singapore",     1.8),
            ("Cape Town",   "Shanghai",      2.2),
            ("Cape Town",   "Tokyo",         2.4),
            ("Cape Town",   "Sydney",        1.6),

            ("Dubai",       "Mumbai",        0.5),
            ("Dubai",       "Singapore",     1.2),
            ("Dubai",       "Shanghai",      1.6),
            ("Dubai",       "Tokyo",         1.8),
            ("Dubai",       "Sydney",        2.0),

            ("Mumbai",      "Singapore",     0.9),
            ("Mumbai",      "Shanghai",      1.3),
            ("Mumbai",      "Tokyo",         1.5),
            ("Mumbai",      "Sydney",        1.7),

            ("Singapore",   "Shanghai",      0.7),
            ("Singapore",   "Tokyo",         0.9),
            ("Singapore",   "Sydney",        1.0),

            ("Shanghai",    "Tokyo",         0.4),
            ("Shanghai",    "Sydney",        1.2),

            ("Tokyo",       "Sydney",        1.1),
        };

        var dict = new Dictionary<(string, string), double>();
        foreach (var (a, b, d) in raw)
        {
            dict[(a, b)] = d;
            dict[(b, a)] = d;
        }

        return dict;
    }

    public double GetDistance(string fromPort, string toPort)
    {
        if (DistanceMultiplier.TryGetValue((fromPort, toPort), out double d))
            return d;

        return 2.0;
    }

    public bool IsGameOver => Player.Balance < 0 && Player.Fleet.All(s => !s.IsAtSea && s.Hold.Count == 0);
    public bool IsVictory => Player.NetWorth >= 1_000_000;
}
