using porter_of_call.Models;

namespace porter_of_call.Data;

public static class ShipDefinitions
{
    public static readonly List<ShipSpec> All = new()
    {
        new()
        {
            Type             = ShipType.Clipper,
            Symbol           = "◇",
            BaseTravelDays   = 5,
            CapacityTonnes   = 150,
            PurchaseCost     = 45_000,
            DailyCost        = 120,
            AllowedCategories = [
                CargoCategory.Agricultural, CargoCategory.Manufactured,
                CargoCategory.Metals, CargoCategory.Forestry
            ],
        },
        new()
        {
            Type             = ShipType.Freighter,
            Symbol           = "△",
            BaseTravelDays   = 8,
            CapacityTonnes   = 400,
            PurchaseCost     = 90_000,
            DailyCost        = 280,
            AllowedCategories = [
                CargoCategory.Agricultural, CargoCategory.Manufactured,
                CargoCategory.Metals, CargoCategory.Forestry, CargoCategory.Energy
            ],
        },
        new()
        {
            Type             = ShipType.ContainerShip,
            Symbol           = "▲",
            BaseTravelDays   = 9,
            CapacityTonnes   = 1000,
            PurchaseCost     = 220_000,
            DailyCost        = 600,
            AllowedCategories = [CargoCategory.Manufactured, CargoCategory.Agricultural],
        },
        new()
        {
            Type             = ShipType.BulkCarrier,
            Symbol           = "■",
            BaseTravelDays   = 12,
            CapacityTonnes   = 1500,
            PurchaseCost     = 180_000,
            DailyCost        = 450,
            AllowedCategories = [
                CargoCategory.Metals, CargoCategory.Forestry,
                CargoCategory.Agricultural, CargoCategory.Energy
            ],
        },
        new()
        {
            Type             = ShipType.Tanker,
            Symbol           = "●",
            BaseTravelDays   = 11,
            CapacityTonnes   = 1200,
            PurchaseCost     = 200_000,
            DailyCost        = 500,
            AllowedCategories = [CargoCategory.Energy],
        },
        new()
        {
            Type             = ShipType.Reefer,
            Symbol           = "◆",
            BaseTravelDays   = 6,
            CapacityTonnes   = 300,
            PurchaseCost     = 130_000,
            DailyCost        = 350,
            AllowedCategories = [CargoCategory.Agricultural],
        },
        new()
        {
            Type             = ShipType.Coaster,
            Symbol           = "C",
            BaseTravelDays   = 4,
            CapacityTonnes   = 110,
            PurchaseCost     = 38_000,
            DailyCost        = 95,
            AllowedCategories = [
                CargoCategory.Agricultural, CargoCategory.Manufactured, CargoCategory.Forestry
            ],
        },
        new()
        {
            Type             = ShipType.Multipurpose,
            Symbol           = "M",
            BaseTravelDays   = 7,
            CapacityTonnes   = 550,
            PurchaseCost     = 115_000,
            DailyCost        = 310,
            AllowedCategories = [
                CargoCategory.Agricultural, CargoCategory.Manufactured,
                CargoCategory.Metals, CargoCategory.Forestry
            ],
        },
        new()
        {
            Type             = ShipType.FeederContainer,
            Symbol           = "F",
            BaseTravelDays   = 6,
            CapacityTonnes   = 520,
            PurchaseCost     = 145_000,
            DailyCost        = 390,
            AllowedCategories = [CargoCategory.Manufactured, CargoCategory.Agricultural],
        },
        new()
        {
            Type             = ShipType.HeavyFreighter,
            Symbol           = "H",
            BaseTravelDays   = 10,
            CapacityTonnes   = 820,
            PurchaseCost     = 175_000,
            DailyCost        = 470,
            AllowedCategories = [
                CargoCategory.Agricultural, CargoCategory.Manufactured,
                CargoCategory.Metals, CargoCategory.Forestry, CargoCategory.Energy
            ],
        },
        new()
        {
            Type             = ShipType.OreCarrier,
            Symbol           = "O",
            BaseTravelDays   = 13,
            CapacityTonnes   = 1900,
            PurchaseCost     = 240_000,
            DailyCost        = 560,
            AllowedCategories = [CargoCategory.Metals],
        },
        new()
        {
            Type             = ShipType.LogCarrier,
            Symbol           = "L",
            BaseTravelDays   = 9,
            CapacityTonnes   = 900,
            PurchaseCost     = 135_000,
            DailyCost        = 320,
            AllowedCategories = [CargoCategory.Forestry, CargoCategory.Agricultural],
        },
        new()
        {
            Type             = ShipType.ChemicalTanker,
            Symbol           = "Y",
            BaseTravelDays   = 9,
            CapacityTonnes   = 700,
            PurchaseCost     = 210_000,
            DailyCost        = 520,
            AllowedCategories = [CargoCategory.Energy, CargoCategory.Manufactured],
        },
        new()
        {
            Type             = ShipType.GasCarrier,
            Symbol           = "G",
            BaseTravelDays   = 8,
            CapacityTonnes   = 780,
            PurchaseCost     = 225_000,
            DailyCost        = 540,
            AllowedCategories = [CargoCategory.Energy],
        },
        new()
        {
            Type             = ShipType.FastReefer,
            Symbol           = "R",
            BaseTravelDays   = 5,
            CapacityTonnes   = 240,
            PurchaseCost     = 165_000,
            DailyCost        = 430,
            AllowedCategories = [CargoCategory.Agricultural],
        },
        new()
        {
            Type             = ShipType.RoRo,
            Symbol           = "P",
            BaseTravelDays   = 7,
            CapacityTonnes   = 650,
            PurchaseCost     = 195_000,
            DailyCost        = 460,
            AllowedCategories = [CargoCategory.Manufactured, CargoCategory.Agricultural],
        },
    };

    public static ShipSpec Get(ShipType type) => All.First(s => s.Type == type);
    public static ShipSpec? TryGet(string typeName) =>
        All.FirstOrDefault(s => s.Type.ToString().Equals(typeName, StringComparison.OrdinalIgnoreCase));
}
