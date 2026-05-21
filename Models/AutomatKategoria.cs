namespace CasinoRoyale.Models;

public class AutomatKategoria
{
    public int AutomatId { get; init; }

    public AutomatInfo Automat { get; init; } = null!;

    public int KategoriaId { get; init; }

    public Kategoria Kategoria { get; init; } = null!;
}
