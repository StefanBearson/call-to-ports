using porter_of_call.Data;
using porter_of_call.Economy;
using porter_of_call.Models;
using porter_of_call.Persistence;

namespace porter_of_call.Engine;

public class EventSystem
{
    private readonly Random _rng = new();
    private readonly List<MarketEventDefinition> _globalEvents;
    private readonly List<MarketEventDefinition> _localEvents;

    public int LastGlobalEventDay { get; private set; } = -999;
    public int LastLocalEventDay { get; private set; } = -999;

    public EventSystem()
    {
        _globalEvents = BuildGlobalEvents();
        _localEvents = BuildLocalEvents();
    }

    public IReadOnlyList<NewsArticle> RollNews(int day, Market market, List<Port> ports)
    {
        var articles = new List<NewsArticle>();

        if (day - LastGlobalEventDay >= 7 && _rng.NextDouble() < 0.26)
        {
            var definition = _globalEvents[_rng.Next(_globalEvents.Count)];
            Apply(definition, market, ports, day, articles);
            LastGlobalEventDay = day;
        }

        if (day - LastLocalEventDay >= 7 && _rng.NextDouble() < 0.34)
        {
            var definition = _localEvents[_rng.Next(_localEvents.Count)];
            Apply(definition, market, ports, day, articles);
            LastLocalEventDay = day;
        }

        return articles;
    }

    public EventSystemStateSaveData ExportState() => new()
    {
        LastGlobalEventDay = LastGlobalEventDay,
        LastLocalEventDay = LastLocalEventDay
    };

    public void ImportState(EventSystemStateSaveData? state)
    {
        LastGlobalEventDay = state?.LastGlobalEventDay ?? -999;
        LastLocalEventDay = state?.LastLocalEventDay ?? -999;
    }

    private void Apply(MarketEventDefinition definition, Market market, List<Port> ports, int day, List<NewsArticle> articles)
    {
        if (definition.CargoId is null && definition.Category is { } category)
        {
            if (Math.Abs(definition.GlobalDemandDelta) > 0)
                market.ApplyCategoryDemandShock(category, definition.GlobalDemandDelta);

            var affectedPorts = GetAffectedPorts(definition, ports);
            if (Math.Abs(definition.LocalDemandDelta) > 0)
                market.ApplyRegionalCategoryDemandShock(affectedPorts, category, definition.LocalDemandDelta);
            if (Math.Abs(definition.LocalSupplyDelta) > 0)
                market.ApplyRegionalCategorySupplyShock(affectedPorts, category, definition.LocalSupplyDelta);
        }

        if (!string.IsNullOrWhiteSpace(definition.CargoId))
        {
            if (Math.Abs(definition.GlobalDemandDelta) > 0)
                market.ApplyGlobalDemandShock(definition.CargoId!, definition.GlobalDemandDelta);

            var affectedPorts = GetAffectedPorts(definition, ports);
            if (Math.Abs(definition.LocalDemandDelta) > 0)
                market.ApplyRegionalDemandShock(affectedPorts, definition.CargoId!, definition.LocalDemandDelta);
            if (Math.Abs(definition.LocalSupplyDelta) > 0)
                market.ApplyRegionalSupplyShock(affectedPorts, definition.CargoId!, definition.LocalSupplyDelta);
        }

        articles.Add(new NewsArticle
        {
            Day = day,
            Scope = definition.Scope,
            Title = definition.Title,
            Body = definition.Body,
            Color = definition.Color,
            Region = definition.Region
        });
    }

    private static IEnumerable<Port> GetAffectedPorts(MarketEventDefinition definition, List<Port> ports) =>
        string.IsNullOrWhiteSpace(definition.Region)
            ? ports
            : ports.Where(port => port.Region.Equals(definition.Region, StringComparison.OrdinalIgnoreCase));

    private static List<MarketEventDefinition> BuildGlobalEvents()
    {
        var themes = new (string CargoId, CargoCategory Category, string MarketName, string Sector)[]
        {
            ("grain", CargoCategory.Agricultural, "grain", "food"),
            ("coffee", CargoCategory.Agricultural, "coffee", "consumer"),
            ("spices", CargoCategory.Agricultural, "spice", "retail"),
            ("crude_oil", CargoCategory.Energy, "crude oil", "energy"),
            ("lng", CargoCategory.Energy, "LNG", "utilities"),
            ("iron_ore", CargoCategory.Metals, "iron ore", "steelmaking"),
            ("copper", CargoCategory.Metals, "copper", "industrial"),
            ("timber", CargoCategory.Forestry, "timber", "construction"),
            ("electronics", CargoCategory.Manufactured, "electronics", "consumer tech"),
            ("vehicles", CargoCategory.Manufactured, "vehicle", "transport")
        };

        var events = new List<MarketEventDefinition>(50);
        foreach (var theme in themes)
        {
            events.Add(new MarketEventDefinition(
                "Global",
                $"Global {ToHeadline(theme.MarketName)} Supply Squeeze",
                $"Export bottlenecks are tightening the global {theme.MarketName} market, pushing traders to bid more aggressively for spot cargoes.",
                theme.CargoId,
                theme.Category,
                null,
                0.18,
                0,
                0,
                "red"));

            events.Add(new MarketEventDefinition(
                "Global",
                $"{ToHeadline(theme.Sector)} Demand Accelerates",
                $"Manufacturers and wholesalers are booking extra {theme.MarketName} volumes as the wider {theme.Sector} cycle turns hotter.",
                theme.CargoId,
                theme.Category,
                null,
                0.14,
                0,
                0,
                "yellow"));

            events.Add(new MarketEventDefinition(
                "Global",
                $"{ToHeadline(theme.MarketName)} Harvest and Output Beat Forecasts",
                $"Fresh supply has landed ahead of schedule, softening the global {theme.MarketName} complex and easing price pressure.",
                theme.CargoId,
                theme.Category,
                null,
                -0.16,
                0,
                0,
                "green"));

            events.Add(new MarketEventDefinition(
                "Global",
                $"Investment Wave Lifts {ToHeadline(theme.Sector)} Trade",
                $"New project spending is feeding through to shipping desks, with buyers locking in forward {theme.MarketName} coverage.",
                theme.CargoId,
                theme.Category,
                null,
                0.12,
                0,
                0,
                "blue"));

            events.Add(new MarketEventDefinition(
                "Global",
                $"{ToHeadline(theme.Sector)} Demand Cools",
                $"Importers are stepping back from the market and allowing inventories to rebuild, weighing on global {theme.MarketName} pricing.",
                theme.CargoId,
                theme.Category,
                null,
                -0.14,
                0,
                0,
                "green"));
        }

        return events;
    }

    private static List<MarketEventDefinition> BuildLocalEvents()
    {
        var regions = new (string Region, string CargoId, CargoCategory Category, string ExportName, string DemandName)[]
        {
            ("N. America", "vehicles", CargoCategory.Manufactured, "vehicle", "retail"),
            ("S. America", "coffee", CargoCategory.Agricultural, "coffee", "food"),
            ("Europe", "chemicals", CargoCategory.Manufactured, "chemical", "industrial"),
            ("Africa", "iron_ore", CargoCategory.Metals, "ore", "infrastructure"),
            ("Middle East", "crude_oil", CargoCategory.Energy, "oil", "energy"),
            ("India", "textiles", CargoCategory.Manufactured, "textile", "consumer"),
            ("SE Asia", "electronics", CargoCategory.Manufactured, "electronics", "factory"),
            ("China", "steel", CargoCategory.Metals, "steel", "industrial"),
            ("Japan", "vehicles", CargoCategory.Manufactured, "vehicle", "retail"),
            ("Oceania", "grain", CargoCategory.Agricultural, "grain", "food")
        };

        var events = new List<MarketEventDefinition>(50);
        foreach (var region in regions)
        {
            events.Add(new MarketEventDefinition(
                "Local",
                $"{region.Region} Port Congestion Slows {ToHeadline(region.ExportName)} Exports",
                $"Berth shortages and labor friction are slowing cargo handling across {region.Region}, trimming available export supply for nearby buyers.",
                region.CargoId,
                region.Category,
                region.Region,
                0,
                0.08,
                -0.22,
                "red"));

            events.Add(new MarketEventDefinition(
                "Local",
                $"{region.Region} Buyers Step Up {ToHeadline(region.DemandName)} Orders",
                $"Local importers in {region.Region} are chasing faster replenishment, which is lifting spot demand across the regional market.",
                region.CargoId,
                region.Category,
                region.Region,
                0,
                0.20,
                0,
                "yellow"));

            events.Add(new MarketEventDefinition(
                "Local",
                $"Weather Disrupts {region.Region} {ToHeadline(region.ExportName)} Output",
                $"Production setbacks are cutting into nearby supply chains in {region.Region}, leaving fewer cargoes available at the docks.",
                region.CargoId,
                region.Category,
                region.Region,
                0,
                0.05,
                -0.18,
                "red"));

            events.Add(new MarketEventDefinition(
                "Local",
                $"{region.Region} Building Cycle Boosts {ToHeadline(region.DemandName)} Demand",
                $"Construction, retail, and industrial projects are accelerating around {region.Region}, firming bids in the local freight market.",
                region.CargoId,
                region.Category,
                region.Region,
                0,
                0.16,
                0,
                "blue"));

            events.Add(new MarketEventDefinition(
                "Local",
                $"{region.Region} Emergency Stock Release Cools Nearby Prices",
                $"Authorities and major traders in {region.Region} have released reserves into the market, improving local availability for now.",
                region.CargoId,
                region.Category,
                region.Region,
                0,
                -0.08,
                0.18,
                "green"));
        }

        return events;
    }

    private static string ToHeadline(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? text
            : char.ToUpperInvariant(text[0]) + text[1..];

    private sealed record MarketEventDefinition(
        string Scope,
        string Title,
        string Body,
        string? CargoId,
        CargoCategory? Category,
        string? Region,
        double GlobalDemandDelta,
        double LocalDemandDelta,
        double LocalSupplyDelta,
        string Color);
}
