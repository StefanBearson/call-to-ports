using System.Text.Json;
using porter_of_call.Models;

namespace porter_of_call.Persistence;

public sealed class GameSaveStore
{
    public const int SlotCount = 10;
    public const int SaveVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _saveDirectory = Path.Combine(
        string.IsNullOrWhiteSpace(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "call-to-ports",
        "saves");

    public IReadOnlyList<SaveSlotInfo> GetSlots()
    {
        Directory.CreateDirectory(_saveDirectory);

        var slots = new List<SaveSlotInfo>(SlotCount);
        for (int slot = 1; slot <= SlotCount; slot++)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                slots.Add(new SaveSlotInfo(slot, false, false, $"Slot {slot}  Empty"));
                continue;
            }

            try
            {
                var save = ReadSlot(path);
                int day = (int)Math.Floor(save.GameHoursElapsed / GameTime.HoursPerDay) + 1;
                int hour = (int)Math.Floor(save.GameHoursElapsed % GameTime.HoursPerDay);
                int minute = (int)Math.Floor((save.GameHoursElapsed - Math.Floor(save.GameHoursElapsed)) * 60);
                int fleetCount = save.Fleet?.Count ?? 0;
                string label = $"Slot {slot}  Day {day} {hour:00}:{minute:00}  " +
                               $"${save.Balance:N0}  {fleetCount} ship(s)  " +
                               $"{save.SavedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
                slots.Add(new SaveSlotInfo(slot, true, false, label));
            }
            catch
            {
                slots.Add(new SaveSlotInfo(slot, true, true, $"Slot {slot}  Corrupt or incompatible save"));
            }
        }

        return slots;
    }

    public void Save(int slot, GameSaveData save)
    {
        ValidateSlot(slot);
        Directory.CreateDirectory(_saveDirectory);

        save.Version = SaveVersion;
        save.SavedAtUtc = DateTime.UtcNow;

        string path = GetSlotPath(slot);
        string tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(save, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    public GameSaveData Load(int slot)
    {
        ValidateSlot(slot);

        string path = GetSlotPath(slot);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Save slot {slot} is empty.");

        return ReadSlot(path);
    }

    private GameSaveData ReadSlot(string path)
    {
        var save = JsonSerializer.Deserialize<GameSaveData>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Save file is empty or unreadable.");

        if (save.Version != SaveVersion)
            throw new InvalidDataException($"Unsupported save version {save.Version}. Expected {SaveVersion}.");

        save.Fleet ??= [];
        save.Ports ??= [];
        save.Log ??= [];
        save.News ??= [];
        save.Market ??= new MarketStateSaveData();
        save.Events ??= new EventSystemStateSaveData();
        return save;
    }

    private string GetSlotPath(int slot) => Path.Combine(_saveDirectory, $"slot-{slot}.json");

    private static void ValidateSlot(int slot)
    {
        if (slot < 1 || slot > SlotCount)
            throw new ArgumentOutOfRangeException(nameof(slot), $"Slot must be between 1 and {SlotCount}.");
    }
}

public sealed class GameSaveData
{
    public int Version { get; set; }
    public DateTime SavedAtUtc { get; set; }
    public bool IsPaused { get; set; }
    public string? SelectedPortName { get; set; }
    public double Balance { get; set; }
    public double GameHoursElapsed { get; set; }
    public List<ShipSaveData>? Fleet { get; set; }
    public List<PortMarketSaveData>? Ports { get; set; }
    public List<string>? Log { get; set; }
    public List<NewsArticleSaveData>? News { get; set; }
    public MarketStateSaveData? Market { get; set; }
    public EventSystemStateSaveData? Events { get; set; }
}

public sealed class ShipSaveData
{
    public string Name { get; set; } = "";
    public string ShipType { get; set; } = "";
    public string CurrentPortName { get; set; } = "";
    public string? DestinationPortName { get; set; }
    public double HoursToArrival { get; set; }
    public double TotalRouteHours { get; set; }
    public double ConditionPct { get; set; }
    public List<CargoLotSaveData> Hold { get; set; } = [];
}

public sealed class CargoLotSaveData
{
    public string CargoId { get; set; } = "";
    public int Tonnes { get; set; }
    public double HoursOld { get; set; }
    public double PurchasePrice { get; set; }
}

public sealed class PortMarketSaveData
{
    public string PortName { get; set; } = "";
    public Dictionary<string, double> Supply { get; set; } = [];
    public Dictionary<string, double> Demand { get; set; } = [];
    public Dictionary<string, double> SupplyBias { get; set; } = [];
    public Dictionary<string, double> DemandBias { get; set; } = [];
}

public sealed record SaveSlotInfo(int Slot, bool Exists, bool IsCorrupt, string Label);

public sealed class MarketStateSaveData
{
    public Dictionary<string, double> GlobalDemand { get; set; } = [];
    public Dictionary<string, double> GlobalMomentum { get; set; } = [];
    public Dictionary<string, double> CategoryPressure { get; set; } = [];
}

public sealed class EventSystemStateSaveData
{
    public int LastGlobalEventDay { get; set; } = -999;
    public int LastLocalEventDay { get; set; } = -999;
}

public sealed class NewsArticleSaveData
{
    public int Day { get; set; }
    public string Scope { get; set; } = "Global";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Color { get; set; } = "yellow";
    public string? Region { get; set; }
}
