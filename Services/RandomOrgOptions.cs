namespace CasinoRoyale.Services;

public class RandomOrgOptions
{
    public const string SectionName = "RandomOrg";

    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://api.random.org/json-rpc/4/invoke";
}
