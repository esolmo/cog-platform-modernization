namespace LotteryService.Combinatorics;

/// <summary>
/// Utility for prime factorization used in permutation count calculation.
/// Ported from Facet.Combinatorics by Adrian Akison (CPOL license).
/// </summary>
internal static class SmallPrimeUtility
{
    private static readonly int[] SmallPrimes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 };

    public static IList<int> Factor(int value)
    {
        var factors = new List<int>();
        foreach (int prime in SmallPrimes)
        {
            while (value % prime == 0)
            {
                factors.Add(prime);
                value /= prime;
            }
            if (value == 1) break;
        }
        if (value > 1) factors.Add(value);
        return factors;
    }

    public static IList<int> DividePrimeFactors(IList<int> numerators, IList<int> denominators)
    {
        var result = new List<int>(numerators);
        foreach (int d in denominators)
        {
            int idx = result.IndexOf(d);
            if (idx >= 0) result.RemoveAt(idx);
        }
        return result;
    }

    public static long EvaluatePrimeFactors(IList<int> factors)
    {
        long result = 1;
        foreach (int f in factors) result *= f;
        return result;
    }
}
