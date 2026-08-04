namespace LiveKicks.Backend.Models.DTOs;

public class OddsDto
{
    public League League { get; set; } = new();
    public Fixture Fixture { get; set; } = new();
    public DateTime Update { get; set; }
    public List<Bookmaker> Bookmakers { get; set; } = new();
}

public class Bookmaker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Bet> Bets { get; set; } = new();
}

public class Bet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<BetValue> Values { get; set; } = new();
}

public class BetValue
{
    public string Value { get; set; } = string.Empty;
    public string Odd { get; set; } = string.Empty;
}
