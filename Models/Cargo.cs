namespace porter_of_call.Models;

public enum CargoCategory { Energy, Agricultural, Metals, Forestry, Manufactured }

public class CargoType
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public CargoCategory Category { get; init; }
    public double BasePrice { get; init; }   // $ per tonne
    public double Volatility { get; init; }  // 0–1, how wildly prices swing
    public List<ShipType> CompatibleShips { get; init; } = new();
    public double? PerishHours { get; init; }   // null = non-perishable
    public string Icon { get; init; } = "📦";
}

public class CargoLot
{
    public CargoType Type { get; init; } = null!;
    public int Tonnes { get; set; }
    public double HoursOld { get; set; }        // for perishables
    public double PurchasePrice { get; set; } // $ per tonne paid

    public double CurrentValue =>
        Type.PerishHours.HasValue
            ? Math.Max(0.1, 1.0 - HoursOld / Type.PerishHours.Value)
            : 1.0;
}
