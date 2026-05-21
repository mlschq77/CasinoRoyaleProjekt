namespace CasinoRoyale.Services;

public interface IRandomNumberService
{
    Task<int> GetIntAsync(int minInclusive, int maxExclusive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetIntsAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        bool allowDuplicates = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetUniqueIntsAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        CancellationToken cancellationToken = default);
}
