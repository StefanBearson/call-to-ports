using porter_of_call.Data;
using porter_of_call.Models;
using porter_of_call.Persistence;

namespace porter_of_call.Economy;

public class Market
{
    private readonly Random _rng = new();
    private readonly Dictionary<string, double> _globalDemand = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _globalMomentum = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<CargoCategory, double> _categoryPressure = new();

    public Market()
    {
        foreach (var cargo in CargoDefinitions.All)
        {
            _globalDemand[cargo.Id] = 1.0;
            _globalMomentum[cargo.Id] = 0.0;
        }

        foreach (var category in Enum.GetValues<CargoCategory>())
            _categoryPressure[category] = 1.0;
    }

    public double BuyPrice(Port port, CargoType cargo)
    {
        double supply = port.Supply.GetValueOrDefault(cargo.Id, 0.5);
        double demand = port.Demand.GetValueOrDefault(cargo.Id, 0.5);
        double worldDemand = GetGlobalDemand(cargo.Id);
        double imbalance = (demand - supply) * (0.8 + cargo.Volatility);
        double worldPremium = (worldDemand - 1.0) * (0.75 + cargo.Volatility * 0.4);
        double factor = Math.Clamp(1.0 + imbalance + worldPremium, 0.35, 3.25);
        return Math.Max(cargo.BasePrice * 0.3, cargo.BasePrice * factor);
    }

    public double SellPrice(Port port, CargoType cargo) => BuyPrice(port, cargo) * 0.92;

    public int AvailableTonnes(Port port, CargoType cargo)
    {
        double supply = port.Supply.GetValueOrDefault(cargo.Id, 0.5);
        double worldDemand = GetGlobalDemand(cargo.Id);
        double storageFactor = 0.65 + Math.Max(0, 1.15 - worldDemand);
        return (int)Math.Clamp(supply * 2200 * storageFactor, 40, 3200);
    }

    public void Tick(List<Port> ports, int day)
    {
        AdvanceCategoryPressure(day);
        AdvanceGlobalDemand(day);

        foreach (var port in ports)
        {
            foreach (var cargo in CargoDefinitions.All)
            {
                double supply = port.Supply.GetValueOrDefault(cargo.Id, 0.5);
                double demand = port.Demand.GetValueOrDefault(cargo.Id, 0.5);
                double supplyBias = port.SupplyBias.GetValueOrDefault(cargo.Id, 0.0);
                double demandBias = port.DemandBias.GetValueOrDefault(cargo.Id, 0.0);

                double targetSupply = BuildSupplyTarget(port, cargo, supplyBias);
                double targetDemand = BuildDemandTarget(port, cargo, demandBias);

                port.Supply[cargo.Id] = Math.Clamp(Drift(supply, targetSupply, 0.06), 0.04, 1.20);
                port.Demand[cargo.Id] = Math.Clamp(Drift(demand, targetDemand, 0.06), 0.04, 1.25);
                port.SupplyBias[cargo.Id] = DriftBias(supplyBias);
                port.DemandBias[cargo.Id] = DriftBias(demandBias);
            }
        }
    }

    public void RecordPurchase(Port port, string cargoId, int tonnes)
    {
        double supply = port.Supply.GetValueOrDefault(cargoId, 0.5);
        double demand = port.Demand.GetValueOrDefault(cargoId, 0.5);
        double delta = tonnes / 2400.0;
        port.Supply[cargoId] = Math.Clamp(supply - delta, 0.04, 1.20);
        port.Demand[cargoId] = Math.Clamp(demand + delta * 0.20, 0.04, 1.25);
        port.DemandBias[cargoId] = Math.Clamp(port.DemandBias.GetValueOrDefault(cargoId) + delta * 0.12, -0.45, 0.55);
    }

    public void RecordSale(Port port, string cargoId, int tonnes)
    {
        double supply = port.Supply.GetValueOrDefault(cargoId, 0.5);
        double demand = port.Demand.GetValueOrDefault(cargoId, 0.5);
        double delta = tonnes / 2400.0;
        port.Supply[cargoId] = Math.Clamp(supply + delta, 0.04, 1.20);
        port.Demand[cargoId] = Math.Clamp(demand - delta * 0.15, 0.04, 1.25);
        port.SupplyBias[cargoId] = Math.Clamp(port.SupplyBias.GetValueOrDefault(cargoId) + delta * 0.08, -0.45, 0.55);
    }

    public double GetGlobalDemand(string cargoId) =>
        _globalDemand.GetValueOrDefault(cargoId, 1.0);

    public double GetTrend(string cargoId) =>
        _globalMomentum.GetValueOrDefault(cargoId, 0.0);

    public void ApplyGlobalDemandShock(string cargoId, double delta)
    {
        _globalDemand[cargoId] = Math.Clamp(GetGlobalDemand(cargoId) + delta, 0.60, 1.90);
        _globalMomentum[cargoId] = Math.Clamp(GetTrend(cargoId) + delta * 0.20, -0.08, 0.08);
    }

    public void ApplyCategoryDemandShock(CargoCategory category, double delta)
    {
        foreach (var cargo in CargoDefinitions.All.Where(c => c.Category == category))
            ApplyGlobalDemandShock(cargo.Id, delta);

        _categoryPressure[category] = Math.Clamp(_categoryPressure.GetValueOrDefault(category, 1.0) + delta * 0.6, 0.65, 1.80);
    }

    public void ApplyRegionalDemandShock(IEnumerable<Port> ports, string cargoId, double delta)
    {
        foreach (var port in ports)
            port.DemandBias[cargoId] = Math.Clamp(port.DemandBias.GetValueOrDefault(cargoId) + delta, -0.45, 0.70);
    }

    public void ApplyRegionalSupplyShock(IEnumerable<Port> ports, string cargoId, double delta)
    {
        foreach (var port in ports)
            port.SupplyBias[cargoId] = Math.Clamp(port.SupplyBias.GetValueOrDefault(cargoId) + delta, -0.45, 0.70);
    }

    public void ApplyRegionalCategoryDemandShock(IEnumerable<Port> ports, CargoCategory category, double delta)
    {
        foreach (var cargo in CargoDefinitions.All.Where(c => c.Category == category))
            ApplyRegionalDemandShock(ports, cargo.Id, delta);
    }

    public void ApplyRegionalCategorySupplyShock(IEnumerable<Port> ports, CargoCategory category, double delta)
    {
        foreach (var cargo in CargoDefinitions.All.Where(c => c.Category == category))
            ApplyRegionalSupplyShock(ports, cargo.Id, delta);
    }

    public MarketStateSaveData ExportState() => new()
    {
        GlobalDemand = new Dictionary<string, double>(_globalDemand, StringComparer.OrdinalIgnoreCase),
        GlobalMomentum = new Dictionary<string, double>(_globalMomentum, StringComparer.OrdinalIgnoreCase),
        CategoryPressure = _categoryPressure.ToDictionary(
            entry => entry.Key.ToString(),
            entry => entry.Value,
            StringComparer.OrdinalIgnoreCase)
    };

    public void ImportState(MarketStateSaveData? state)
    {
        foreach (var cargo in CargoDefinitions.All)
        {
            _globalDemand[cargo.Id] = Math.Clamp(state?.GlobalDemand.GetValueOrDefault(cargo.Id, 1.0) ?? 1.0, 0.60, 1.90);
            _globalMomentum[cargo.Id] = Math.Clamp(state?.GlobalMomentum.GetValueOrDefault(cargo.Id, 0.0) ?? 0.0, -0.08, 0.08);
        }

        foreach (var category in Enum.GetValues<CargoCategory>())
        {
            string key = category.ToString();
            _categoryPressure[category] = Math.Clamp(state?.CategoryPressure.GetValueOrDefault(key, 1.0) ?? 1.0, 0.65, 1.80);
        }
    }

    private void AdvanceCategoryPressure(int day)
    {
        foreach (var category in Enum.GetValues<CargoCategory>())
        {
            double target = category switch
            {
                CargoCategory.Agricultural => 1.0 + 0.14 * Math.Sin((day + 1) / 8.0) - 0.03 * Math.Cos(day / 17.0),
                CargoCategory.Energy => 1.0 + 0.18 * Math.Sin((day + 6) / 11.0) + 0.03 * Math.Cos(day / 5.0),
                CargoCategory.Metals => 1.0 + 0.12 * Math.Sin((day + 13) / 14.0),
                CargoCategory.Forestry => 1.0 + 0.09 * Math.Cos((day + 7) / 10.0),
                CargoCategory.Manufactured => 1.0 + 0.11 * Math.Sin((day + 19) / 16.0) + 0.02 * Math.Cos(day / 6.0),
                _ => 1.0
            };

            double relatedLift = category switch
            {
                CargoCategory.Metals => (_categoryPressure[CargoCategory.Energy] - 1.0) * 0.18,
                CargoCategory.Manufactured => (_categoryPressure[CargoCategory.Metals] - 1.0) * 0.20,
                CargoCategory.Agricultural => (_categoryPressure[CargoCategory.Energy] - 1.0) * 0.10,
                CargoCategory.Forestry => (_categoryPressure[CargoCategory.Manufactured] - 1.0) * 0.12,
                _ => 0.0
            };

            target += relatedLift;
            _categoryPressure[category] = Math.Clamp(
                _categoryPressure[category] + (target - _categoryPressure[category]) * 0.12 + ((_rng.NextDouble() - 0.5) * 0.02),
                0.70,
                1.65);
        }
    }

    private void AdvanceGlobalDemand(int day)
    {
        foreach (var cargo in CargoDefinitions.All)
        {
            double current = GetGlobalDemand(cargo.Id);
            double momentum = GetTrend(cargo.Id);
            double seasonal = cargo.Category == CargoCategory.Agricultural
                ? 0.10 * Math.Sin((day + cargo.Id.Length) / 7.0)
                : cargo.Category == CargoCategory.Energy
                    ? 0.08 * Math.Cos((day + cargo.Id.Length) / 9.0)
                    : 0.05 * Math.Sin((day + cargo.Id.Length) / 13.0);

            double cargoTarget = Math.Clamp(
                _categoryPressure[cargo.Category]
                + seasonal
                + (cargo.Volatility - 0.20) * 0.20
                + CrossCategoryEffect(cargo),
                0.70,
                1.70);

            momentum = Math.Clamp(momentum + (cargoTarget - current) * 0.14 + ((_rng.NextDouble() - 0.5) * 0.015), -0.06, 0.06);
            current = Math.Clamp(current + momentum, 0.60, 1.90);

            _globalDemand[cargo.Id] = current;
            _globalMomentum[cargo.Id] = momentum * 0.85;
        }
    }

    private double CrossCategoryEffect(CargoType cargo) => cargo.Category switch
    {
        CargoCategory.Agricultural => (_categoryPressure[CargoCategory.Energy] - 1.0) * 0.18,
        CargoCategory.Energy => (_categoryPressure[CargoCategory.Manufactured] - 1.0) * 0.15,
        CargoCategory.Metals => (_categoryPressure[CargoCategory.Manufactured] - 1.0) * 0.22,
        CargoCategory.Forestry => (_categoryPressure[CargoCategory.Manufactured] - 1.0) * 0.12,
        CargoCategory.Manufactured => (_categoryPressure[CargoCategory.Metals] - 1.0) * 0.18,
        _ => 0.0
    };

    private double BuildSupplyTarget(Port port, CargoType cargo, double bias)
    {
        double worldDemand = GetGlobalDemand(cargo.Id);
        double baseSupply = port.SpecialisedExports.Contains(cargo.Id) ? 0.86 : 0.34;
        double categoryTailwind = (_categoryPressure[cargo.Category] - 1.0) * 0.15;
        double scarcity = Math.Max(0, worldDemand - 1.0) * -0.22;
        return Math.Clamp(baseSupply + bias + categoryTailwind + scarcity, 0.08, 1.15);
    }

    private double BuildDemandTarget(Port port, CargoType cargo, double bias)
    {
        double worldDemand = GetGlobalDemand(cargo.Id);
        double baseDemand = port.SpecialisedImports.Contains(cargo.Id) ? 0.84 : 0.28;
        double categoryTailwind = (_categoryPressure[cargo.Category] - 1.0) * 0.18;
        double worldLift = (worldDemand - 1.0) * 0.45;
        return Math.Clamp(baseDemand + bias + categoryTailwind + worldLift, 0.08, 1.20);
    }

    private double Drift(double current, double target, double maxStep)
    {
        double noise = (_rng.NextDouble() - 0.5) * 0.04;
        double step = (target - current) * 0.18 + noise;
        return current + Math.Clamp(step, -maxStep, maxStep);
    }

    private double DriftBias(double current)
    {
        double drift = current * 0.94;
        if (Math.Abs(drift) < 0.005)
            return 0;

        return Math.Clamp(drift, -0.60, 0.60);
    }
}
