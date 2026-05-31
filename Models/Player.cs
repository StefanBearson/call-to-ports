namespace porter_of_call.Models;

public class Player
{
    public double Balance { get; set; } = 50_000;
    public List<Ship> Fleet { get; } = new();
    public double GameHoursElapsed { get; set; }
    public int Day => (int)Math.Floor(GameHoursElapsed / GameTime.HoursPerDay) + 1;
    public int HourOfDay => (int)Math.Floor(GameHoursElapsed % GameTime.HoursPerDay);
    public int MinuteOfHour => (int)Math.Floor((GameHoursElapsed - Math.Floor(GameHoursElapsed)) * 60);
    public string ClockText => $"Day {Day} {HourOfDay:00}:{MinuteOfHour:00}";
    public double NetWorth => Balance + Fleet.Sum(s => s.Spec.PurchaseCost * 0.6);

    public Port? SelectedPort { get; set; }
}
