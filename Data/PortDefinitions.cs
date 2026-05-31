using porter_of_call.Models;

namespace porter_of_call.Data;

public static class PortDefinitions
{
    // Map canvas is 70 wide × 16 tall (0-indexed col/row)
    public static readonly List<Port> All = new()
    {
        new()
        {
            Name    = "New York",
            Region  = "N. America",
            MapCol  = 12, MapRow = 4,
            SpecialisedExports = ["grain", "coal", "chemicals"],
            SpecialisedImports = ["electronics", "vehicles", "textiles", "coffee"],
        },
        new()
        {
            Name    = "Los Angeles",
            Region  = "N. America",
            MapCol  = 4,  MapRow = 7,
            SpecialisedExports = ["vehicles", "electronics"],
            SpecialisedImports = ["crude_oil", "textiles", "machinery"],
        },
        new()
        {
            Name    = "Santos",
            Region  = "S. America",
            MapCol  = 16, MapRow = 12,
            SpecialisedExports = ["coffee", "grain", "iron_ore", "timber"],
            SpecialisedImports = ["machinery", "chemicals", "electronics"],
        },
        new()
        {
            Name    = "Rotterdam",
            Region  = "Europe",
            MapCol  = 36, MapRow = 3,
            SpecialisedExports = ["chemicals", "steel", "machinery"],
            SpecialisedImports = ["crude_oil", "grain", "coffee", "vehicles"],
        },
        new()
        {
            Name    = "Hamburg",
            Region  = "Europe",
            MapCol  = 38, MapRow = 2,
            SpecialisedExports = ["machinery", "chemicals", "paper_pulp"],
            SpecialisedImports = ["coffee", "cocoa", "grain", "copper"],
        },
        new()
        {
            Name    = "Cape Town",
            Region  = "Africa",
            MapCol  = 35, MapRow = 13,
            SpecialisedExports = ["iron_ore", "coal", "aluminum", "frozen_fish"],
            SpecialisedImports = ["machinery", "electronics", "vehicles"],
        },
        new()
        {
            Name    = "Dubai",
            Region  = "Middle East",
            MapCol  = 49, MapRow = 6,
            SpecialisedExports = ["crude_oil", "lng", "diesel"],
            SpecialisedImports = ["vehicles", "electronics", "machinery", "textiles"],
        },
        new()
        {
            Name    = "Mumbai",
            Region  = "India",
            MapCol  = 51, MapRow = 9,
            SpecialisedExports = ["textiles", "spices", "tea", "chemicals"],
            SpecialisedImports = ["crude_oil", "coal", "machinery", "electronics"],
        },
        new()
        {
            Name    = "Singapore",
            Region  = "SE Asia",
            MapCol  = 57, MapRow = 10,
            SpecialisedExports = ["electronics", "spices", "rubber", "palm_oil"],
            SpecialisedImports = ["crude_oil", "lng", "iron_ore", "grain"],
        },
        new()
        {
            Name    = "Shanghai",
            Region  = "China",
            MapCol  = 61, MapRow = 7,
            SpecialisedExports = ["electronics", "textiles", "machinery", "steel"],
            SpecialisedImports = ["iron_ore", "coal", "lng", "copper", "timber"],
        },
        new()
        {
            Name    = "Tokyo",
            Region  = "Japan",
            MapCol  = 64, MapRow = 5,
            SpecialisedExports = ["vehicles", "electronics", "machinery"],
            SpecialisedImports = ["iron_ore", "coal", "lng", "timber", "grain"],
        },
        new()
        {
            Name    = "Sydney",
            Region  = "Oceania",
            MapCol  = 64, MapRow = 14,
            SpecialisedExports = ["coal", "iron_ore", "timber", "grain", "frozen_fish"],
            SpecialisedImports = ["vehicles", "electronics", "machinery"],
        },
    };

    public static Port Get(string name) =>
        All.First(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static Port? TryGet(string name) =>
        All.FirstOrDefault(p => p.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
}
