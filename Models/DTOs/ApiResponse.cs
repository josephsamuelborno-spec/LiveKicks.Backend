namespace LiveKicks.Backend.Models.DTOs;

public class ApiResponse<T>
{
    public string Get { get; set; } = string.Empty;

    public Parameters Parameters { get; set; } = new();

    // API-FOOTBALL returns this as an object when errors exist,
    // and an empty object/array depending on the response.
    // Using object prevents JSON deserialization failures.
    public object? Errors { get; set; }

    public int Results { get; set; }

    public Paging? Paging { get; set; }

    public List<T> Response { get; set; } = new();
}

public class Parameters
{
    public string? Date { get; set; }
    public string? League { get; set; }
    public string? Season { get; set; }
    public string? Timezone { get; set; }
    public string? Live { get; set; }
    public string? Id { get; set; }
    public string? H2h { get; set; }
}

public class Paging
{
    public int Current { get; set; }
    public int Total { get; set; }
}