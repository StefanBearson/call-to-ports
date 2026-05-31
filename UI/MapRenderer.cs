using porter_of_call.Data;
using porter_of_call.Models;
using Spectre.Console;

namespace porter_of_call.UI;

/// <summary>
/// Renders the ASCII world map at 2× scale (140 cols × 32 rows).
/// The base map is 70×16; each cell is doubled in both dimensions.
/// Port map coords in PortDefinitions remain in base (70×16) space —
/// they are multiplied by 2 when rendering.
/// </summary>
public static class MapRenderer
{
    // Base map: 70 wide × 16 tall
    private static readonly string[] BaseMap =
    [
        //0         1         2         3         4         5         6
        //0123456789012345678901234567890123456789012345678901234567890123456789
        "......~~~~....~~~~.................~~~~...~~~~..~~~~..~~..~~~~..~~~~..",
        "..~~~~.####..####..~~~......~~~~~~.####..####..####.####.####.####...",
        "..#################.......########################################....",
        "..####.##########........###########################################..",
        "...####.#######.........############################################..",
        "....####.######..........##########################################...",
        ".....####.####............#########################################...",
        "......####.##..............#######################################....",
        "......####.#................########.############################.....",
        ".......###.....................####.##############################.....",
        "........##....................#####.##############################.....",
        "........##...................######.######.#####.####.##..##.###......",
        "........##....................####...#####.####.#####.##..##.###......",
        ".......####......................##.......######..####.##..##.###.....",
        "........####.....................#.......#######..######.######.##....",
        ".........####....................................##.####.####.#.......",
    ];

    private const int BaseW = 70;
    private const int BaseH = 16;
    private const int Scale = 2;
    private const int W = BaseW * Scale;   // 140
    private const int H = BaseH * Scale;  // 32

    public static Panel Render(List<Port> ports, List<Ship> ships)
    {
        // Build scaled char grid: each base cell → Scale×Scale block
        var grid = new char[H, W];
        for (int br = 0; br < BaseH; br++)
        {
            string row = BaseMap[br];
            for (int bc = 0; bc < BaseW; bc++)
            {
                char ch = bc < row.Length ? row[bc] : '.';
                for (int dr = 0; dr < Scale; dr++)
                for (int dc = 0; dc < Scale; dc++)
                    grid[br * Scale + dr, bc * Scale + dc] = ch;
            }
        }

        // Place port markers and collect labels — use scaled coords
        var portLabels = new Dictionary<(int col, int row), string>();
        foreach (var port in ports)
        {
            int c = Math.Clamp(port.MapCol * Scale, 0, W - 1);
            int r = Math.Clamp(port.MapRow * Scale, 0, H - 1);
            grid[r, c] = '*';
            portLabels[(c, r)] = AbbreviatePort(port.Name);
        }

        // Place ships at interpolated scaled positions
        var shipPositions = new Dictionary<(int col, int row), char>();
        foreach (var ship in ships)
        {
            var (col, row) = ShipMapPos(ship);
            col = Math.Clamp(col, 0, W - 1);
            row = Math.Clamp(row, 0, H - 1);
            shipPositions[(col, row)] = ship.Spec.Symbol[0];
        }

        // Build markup string
        var sb = new System.Text.StringBuilder();
        for (int r = 0; r < H; r++)
        {
            for (int c = 0; c < W; c++)
            {
                char ch = grid[r, c];

                if (shipPositions.TryGetValue((c, r), out char shipChar))
                {
                    sb.Append($"[bold yellow]{shipChar}[/]");
                    continue;
                }

                if (ch == '*') { sb.Append("[bold green]*[/]"); continue; }
                if (ch == '~') sb.Append("[blue]~[/]");
                else if (ch == '#') sb.Append("[grey]#[/]");
                else sb.Append("[blue].[/]");
            }

            // Port labels appear on the first scaled row only (even rows)
            var labelsThisRow = portLabels
                .Where(kv => kv.Key.row == r)
                .OrderBy(kv => kv.Key.col)
                .ToList();
            if (labelsThisRow.Count > 0)
            {
                sb.Append("  ");
                foreach (var kv in labelsThisRow)
                    sb.Append($"[green]{kv.Value}[/] ");
            }

            sb.AppendLine();
        }

        return new Panel(new Markup(sb.ToString().TrimEnd()))
        {
            Header  = new PanelHeader("[bold cyan]  WORLD MAP  [/]"),
            Border  = BoxBorder.Rounded,
            Padding = new Padding(1, 0),
        };
    }

    private static (int col, int row) ShipMapPos(Ship ship)
    {
        if (!ship.IsAtSea)
            return (ship.CurrentPort.MapCol * Scale, ship.CurrentPort.MapRow * Scale);

        var from = ship.CurrentPort;
        var to   = ship.Destination!;
        double t = ship.SailProgress;

        int col = (int)Math.Round((from.MapCol + (to.MapCol - from.MapCol) * t) * Scale);
        int row = (int)Math.Round((from.MapRow + (to.MapRow - from.MapRow) * t) * Scale);
        return (col, row);
    }

    private static string AbbreviatePort(string name) => name switch
    {
        "New York"    => "NY",
        "Los Angeles" => "LA",
        "Santos"      => "SA",
        "Rotterdam"   => "RT",
        "Hamburg"     => "HB",
        "Cape Town"   => "CT",
        "Dubai"       => "DB",
        "Mumbai"      => "MB",
        "Singapore"   => "SG",
        "Shanghai"    => "SH",
        "Tokyo"       => "TK",
        "Sydney"      => "SY",
        _             => name[..Math.Min(2, name.Length)].ToUpper(),
    };
}
