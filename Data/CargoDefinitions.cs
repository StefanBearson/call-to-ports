using porter_of_call.Models;

namespace porter_of_call.Data;

public static class CargoDefinitions
{
    public static readonly List<CargoType> All = new()
    {
        // ── Energy ──────────────────────────────────────────────────────
        new() { Id="crude_oil",   Name="Crude Oil",     Category=CargoCategory.Energy,
                BasePrice=420,  Volatility=0.35,
                CompatibleShips=[ShipType.Tanker, ShipType.ChemicalTanker],
                Icon="🛢️" },
        new() { Id="diesel",      Name="Diesel",        Category=CargoCategory.Energy,
                BasePrice=580,  Volatility=0.30,
                CompatibleShips=[ShipType.Tanker, ShipType.ChemicalTanker],
                Icon="⛽" },
        new() { Id="lng",         Name="LNG",           Category=CargoCategory.Energy,
                BasePrice=650,  Volatility=0.40,
                CompatibleShips=[ShipType.Tanker, ShipType.GasCarrier],
                Icon="🔥" },
        new() { Id="coal",        Name="Coal",          Category=CargoCategory.Energy,
                BasePrice=180,  Volatility=0.20,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.Freighter, ShipType.HeavyFreighter],
                Icon="⬛" },

        // ── Agricultural ─────────────────────────────────────────────────
        new() { Id="grain",       Name="Grain",         Category=CargoCategory.Agricultural,
                BasePrice=210,  Volatility=0.25,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.Freighter, ShipType.Clipper, ShipType.Coaster, ShipType.Multipurpose, ShipType.HeavyFreighter, ShipType.LogCarrier],
                Icon="🌾" },
        new() { Id="coffee",      Name="Coffee",        Category=CargoCategory.Agricultural,
                BasePrice=3800, Volatility=0.45,
                CompatibleShips=[ShipType.Freighter, ShipType.ContainerShip, ShipType.Clipper, ShipType.Reefer, ShipType.Coaster, ShipType.Multipurpose, ShipType.FeederContainer, ShipType.FastReefer, ShipType.RoRo],
                Icon="☕" },
        new() { Id="cocoa",       Name="Cocoa",         Category=CargoCategory.Agricultural,
                BasePrice=2900, Volatility=0.40,
                CompatibleShips=[ShipType.Freighter, ShipType.ContainerShip, ShipType.Clipper, ShipType.Coaster, ShipType.Multipurpose, ShipType.FeederContainer, ShipType.RoRo],
                Icon="🍫" },
        new() { Id="tea",         Name="Tea",           Category=CargoCategory.Agricultural,
                BasePrice=2200, Volatility=0.30,
                CompatibleShips=[ShipType.Freighter, ShipType.ContainerShip, ShipType.Clipper, ShipType.Coaster, ShipType.Multipurpose, ShipType.FeederContainer, ShipType.RoRo],
                Icon="🍵" },
        new() { Id="spices",      Name="Spices",        Category=CargoCategory.Agricultural,
                BasePrice=5500, Volatility=0.50,
                CompatibleShips=[ShipType.Freighter, ShipType.ContainerShip, ShipType.Clipper, ShipType.Coaster, ShipType.Multipurpose, ShipType.FeederContainer, ShipType.RoRo],
                Icon="🌶️" },
        new() { Id="bananas",     Name="Bananas",       Category=CargoCategory.Agricultural,
                BasePrice=320,  Volatility=0.20,  PerishHours=8 * GameTime.HoursPerDay,
                CompatibleShips=[ShipType.Reefer, ShipType.Clipper, ShipType.FastReefer, ShipType.Coaster, ShipType.LogCarrier],
                Icon="🍌" },
        new() { Id="frozen_fish", Name="Frozen Fish",   Category=CargoCategory.Agricultural,
                BasePrice=900,  Volatility=0.25,  PerishHours=12 * GameTime.HoursPerDay,
                CompatibleShips=[ShipType.Reefer, ShipType.FastReefer],
                Icon="🐟" },

        // ── Metals ───────────────────────────────────────────────────────
        new() { Id="iron_ore",    Name="Iron Ore",      Category=CargoCategory.Metals,
                BasePrice=120,  Volatility=0.15,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.OreCarrier],
                Icon="🪨" },
        new() { Id="steel",       Name="Steel",         Category=CargoCategory.Metals,
                BasePrice=680,  Volatility=0.20,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.Freighter, ShipType.Multipurpose, ShipType.HeavyFreighter, ShipType.OreCarrier],
                Icon="⚙️" },
        new() { Id="copper",      Name="Copper",        Category=CargoCategory.Metals,
                BasePrice=8500, Volatility=0.35,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.Freighter, ShipType.ContainerShip, ShipType.Multipurpose, ShipType.HeavyFreighter, ShipType.OreCarrier],
                Icon="🔶" },
        new() { Id="aluminum",    Name="Aluminum",      Category=CargoCategory.Metals,
                BasePrice=2400, Volatility=0.25,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.Freighter, ShipType.ContainerShip, ShipType.Multipurpose, ShipType.HeavyFreighter],
                Icon="🥈" },

        // ── Forestry ─────────────────────────────────────────────────────
        new() { Id="timber",      Name="Timber",        Category=CargoCategory.Forestry,
                BasePrice=280,  Volatility=0.15,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.Freighter, ShipType.Coaster, ShipType.Multipurpose, ShipType.HeavyFreighter, ShipType.LogCarrier],
                Icon="🪵" },
        new() { Id="paper_pulp",  Name="Paper Pulp",    Category=CargoCategory.Forestry,
                BasePrice=450,  Volatility=0.18,
                CompatibleShips=[ShipType.BulkCarrier, ShipType.Freighter, ShipType.Multipurpose, ShipType.HeavyFreighter, ShipType.LogCarrier],
                Icon="📄" },

        // ── Manufactured ─────────────────────────────────────────────────
        new() { Id="electronics", Name="Electronics",   Category=CargoCategory.Manufactured,
                BasePrice=12000, Volatility=0.30,
                CompatibleShips=[ShipType.ContainerShip, ShipType.Freighter, ShipType.Clipper, ShipType.Coaster, ShipType.Multipurpose, ShipType.FeederContainer, ShipType.HeavyFreighter, ShipType.RoRo],
                Icon="💻" },
        new() { Id="vehicles",    Name="Vehicles",      Category=CargoCategory.Manufactured,
                BasePrice=18000, Volatility=0.20,
                CompatibleShips=[ShipType.ContainerShip, ShipType.Freighter, ShipType.FeederContainer, ShipType.HeavyFreighter, ShipType.RoRo],
                Icon="🚗" },
        new() { Id="machinery",   Name="Machinery",     Category=CargoCategory.Manufactured,
                BasePrice=7500, Volatility=0.22,
                CompatibleShips=[ShipType.ContainerShip, ShipType.Freighter, ShipType.Multipurpose, ShipType.FeederContainer, ShipType.HeavyFreighter, ShipType.RoRo],
                Icon="🔧" },
        new() { Id="textiles",    Name="Textiles",      Category=CargoCategory.Manufactured,
                BasePrice=3200, Volatility=0.28,
                CompatibleShips=[ShipType.ContainerShip, ShipType.Freighter, ShipType.Clipper, ShipType.Coaster, ShipType.Multipurpose, ShipType.FeederContainer, ShipType.RoRo],
                Icon="🧵" },
        new() { Id="chemicals",   Name="Chemicals",     Category=CargoCategory.Manufactured,
                BasePrice=1800, Volatility=0.35,
                CompatibleShips=[ShipType.ContainerShip, ShipType.Tanker, ShipType.Freighter, ShipType.FeederContainer, ShipType.HeavyFreighter, ShipType.ChemicalTanker],
                Icon="⚗️" },
        new() { Id="pharma",      Name="Pharmaceuticals", Category=CargoCategory.Manufactured,
                BasePrice=22000, Volatility=0.40,
                CompatibleShips=[ShipType.ContainerShip, ShipType.Clipper, ShipType.Reefer, ShipType.FeederContainer, ShipType.FastReefer, ShipType.RoRo],
                Icon="💊" },
    };

    public static CargoType Get(string id) => All.First(c => c.Id == id);
    public static CargoType? TryGet(string id) =>
        All.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
