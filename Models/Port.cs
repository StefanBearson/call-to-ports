namespace porter_of_call.Models;

public class Port
{
    public string Name { get; init; } = "";
    public string Region { get; init; } = "";
    public int MapCol { get; init; }
    public int MapRow { get; init; }

    // Per-cargo supply (0–1) and demand (0–1), updated each turn
    public Dictionary<string, double> Supply { get; } = new();
    public Dictionary<string, double> Demand { get; } = new();
    public Dictionary<string, double> SupplyBias { get; } = new();
    public Dictionary<string, double> DemandBias { get; } = new();

    // Cargo types this port specialises in (higher base demand/supply)
    public List<string> SpecialisedExports { get; init; } = new();
    public List<string> SpecialisedImports { get; init; } = new();
}
