namespace SimplyBudget.Data;

public class Metadata : BaseItem
{
    public const string VERSION_KEY = "Version";

    public string? Key { get; set; }
    public string? Value { get; set; }

    public int ValueAsInt(int defaultValue = 0)
    {
        if (int.TryParse(Value ?? "", out int rv) == false)
            rv = defaultValue;
        return rv;
    }
}