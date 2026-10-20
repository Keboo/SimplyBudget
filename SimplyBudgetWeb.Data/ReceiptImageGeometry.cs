using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimplyBudgetWeb.Data;

public sealed record ReceiptImagePoint(
    [property: JsonRequired] double X,
    [property: JsonRequired] double Y);

// Version 1: normalized coordinates in the EXIF-oriented original, clockwise from
// the desired output's top-left. Perspective/skew is derived from these four points.
public sealed record ReceiptImageGeometry(
    [property: JsonRequired] int Version,
    [property: JsonRequired] ReceiptImagePoint[] Corners)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool IsValid()
    {
        if (Version != 1 || Corners is not { Length: 4 } ||
            Corners.Any(p => p is null || !double.IsFinite(p.X) || !double.IsFinite(p.Y) ||
                p.X < 0 || p.X > 1 || p.Y < 0 || p.Y > 1))
            return false;

        double twiceArea = 0;
        for (var i = 0; i < 4; i++)
        {
            var a = Corners[i];
            var b = Corners[(i + 1) % 4];
            var c = Corners[(i + 2) % 4];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            if (dx * dx + dy * dy < 0.000025 ||
                dx * (c.Y - b.Y) - dy * (c.X - b.X) <= 0.00001)
                return false;
            twiceArea += a.X * b.Y - b.X * a.Y;
        }
        return twiceArea >= 0.002;
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static bool TryParse(string json, out ReceiptImageGeometry? geometry)
    {
        geometry = null;
        if (json.Length > 1024) return false;
        try
        {
            var parsed = JsonSerializer.Deserialize<ReceiptImageGeometry>(json, JsonOptions);
            if (parsed?.IsValid() != true) return false;
            geometry = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
