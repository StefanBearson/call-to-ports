namespace porter_of_call.Models;

public enum ShipType
{
    Clipper,
    Freighter,
    ContainerShip,
    BulkCarrier,
    Tanker,
    Reefer,
    Coaster,
    Multipurpose,
    FeederContainer,
    HeavyFreighter,
    OreCarrier,
    LogCarrier,
    ChemicalTanker,
    GasCarrier,
    FastReefer,
    RoRo
}

public static class ShipTypeDisplay
{
    public static string Format(ShipType type) => type switch
    {
        ShipType.ContainerShip => "Container Ship",
        ShipType.BulkCarrier => "Bulk Carrier",
        ShipType.FeederContainer => "Feeder Container",
        ShipType.HeavyFreighter => "Heavy Freighter",
        ShipType.OreCarrier => "Ore Carrier",
        ShipType.LogCarrier => "Log Carrier",
        ShipType.ChemicalTanker => "Chemical Tanker",
        ShipType.GasCarrier => "Gas Carrier",
        ShipType.FastReefer => "Fast Reefer",
        _ => type.ToString(),
    };

    public static string FormatList(IEnumerable<ShipType> types, string separator = ", ") =>
        string.Join(separator, types.Select(Format));
}

public class ShipSpec
{
    public ShipType Type { get; init; }
    public string Symbol { get; init; } = "△";
    public double BaseTravelDays { get; init; }
    public int CapacityTonnes { get; init; }
    public double PurchaseCost { get; init; }
    public double DailyCost { get; init; }
    public List<CargoCategory> AllowedCategories { get; init; } = new();

    public bool CanCarry(CargoType cargo) =>
        AllowedCategories.Contains(cargo.Category) &&
        cargo.CompatibleShips.Contains(Type);
}

public class Ship
{
    public string Name { get; set; } = "";
    public ShipSpec Spec { get; init; } = null!;
    public Port CurrentPort { get; set; } = null!;
    public Port? Destination { get; set; }
    public double HoursToArrival { get; set; }
    public double TotalRouteHours { get; set; }
    public List<CargoLot> Hold { get; } = new();
    public double ConditionPct { get; set; } = 100;

    public int UsedCapacity => Hold.Sum(l => l.Tonnes);
    public int FreeCapacity => Spec.CapacityTonnes - UsedCapacity;

    public bool IsAtSea => Destination != null;

    /// Progress 0.0–1.0 for map interpolation
    public double SailProgress =>
        TotalRouteHours > 0
            ? 1.0 - HoursToArrival / TotalRouteHours
            : 0;
}
