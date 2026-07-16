namespace LotteryService.Combinatorics;

/// <summary>
/// Enumerates all lexicographic permutations of a list of values.
/// Ported from Facet.Combinatorics by Adrian Akison (CPOL license).
/// </summary>
public class Permutations<T> : IEnumerable<IList<T>>
{
    private readonly List<T> _values;
    private readonly int[] _lexicographicOrders;
    private readonly GenerateOption _type;
    private readonly long _count;

    public Permutations(IList<T> values, GenerateOption type = GenerateOption.WithoutRepetition, IComparer<T>? comparer = null)
    {
        _type = type;
        _values = new List<T>(values);
        _lexicographicOrders = new int[values.Count];

        if (type == GenerateOption.WithRepetition)
        {
            for (int i = 0; i < _lexicographicOrders.Length; i++)
                _lexicographicOrders[i] = i;
        }
        else
        {
            comparer ??= Comparer<T>.Default;
            _values.Sort(comparer);
            int j = 1;
            if (_lexicographicOrders.Length > 0) _lexicographicOrders[0] = j;
            for (int i = 1; i < _lexicographicOrders.Length; i++)
            {
                if (comparer.Compare(_values[i - 1], _values[i]) != 0) j++;
                _lexicographicOrders[i] = j;
            }
        }
        _count = ComputeCount();
    }

    public long Count => _count;
    public GenerateOption Type => _type;
    public int UpperIndex => _values.Count;
    public int LowerIndex => _values.Count;

    public IEnumerator<IList<T>> GetEnumerator() => new Enumerator(this);
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

    private long ComputeCount()
    {
        int runCount = 1;
        var divisors = new List<int>();
        var numerators = new List<int>();
        for (int i = 1; i < _lexicographicOrders.Length; i++)
        {
            numerators.AddRange(SmallPrimeUtility.Factor(i + 1));
            if (_lexicographicOrders[i] == _lexicographicOrders[i - 1])
            {
                runCount++;
            }
            else
            {
                for (int f = 2; f <= runCount; f++) divisors.AddRange(SmallPrimeUtility.Factor(f));
                runCount = 1;
            }
        }
        for (int f = 2; f <= runCount; f++) divisors.AddRange(SmallPrimeUtility.Factor(f));
        return SmallPrimeUtility.EvaluatePrimeFactors(SmallPrimeUtility.DividePrimeFactors(numerators, divisors));
    }

    public class Enumerator : IEnumerator<IList<T>>
    {
        private readonly Permutations<T> _parent;
        private readonly int[] _lexOrders;
        private List<T> _current = new();
        private Position _position = Position.BeforeFirst;

        public Enumerator(Permutations<T> source)
        {
            _parent = source;
            _lexOrders = new int[source._lexicographicOrders.Length];
            source._lexicographicOrders.CopyTo(_lexOrders, 0);
        }

        public void Reset() => _position = Position.BeforeFirst;

        public bool MoveNext()
        {
            if (_position == Position.BeforeFirst)
            {
                _current = new List<T>(_parent._values);
                Array.Sort(_lexOrders);
                _position = Position.InSet;
            }
            else if (_position == Position.InSet)
            {
                if (_current.Count < 2 || !NextPermutation())
                    _position = Position.AfterLast;
            }
            return _position != Position.AfterLast;
        }

        public IList<T> Current => _position == Position.InSet
            ? new List<T>(_current)
            : throw new InvalidOperationException();

        object System.Collections.IEnumerator.Current => Current;
        public void Dispose() { }

        private bool NextPermutation()
        {
            int i = _lexOrders.Length - 1;
            while (_lexOrders[i - 1] >= _lexOrders[i])
            {
                if (--i == 0) return false;
            }
            int j = _lexOrders.Length;
            while (_lexOrders[j - 1] <= _lexOrders[i - 1]) j--;
            Swap(i - 1, j - 1);
            i++;
            j = _lexOrders.Length;
            while (i < j) { Swap(i++ - 1, --j); }
            return true;
        }

        private void Swap(int i, int j)
        {
            (_current[i], _current[j]) = (_current[j], _current[i]);
            (_lexOrders[i], _lexOrders[j]) = (_lexOrders[j], _lexOrders[i]);
        }

        private enum Position { BeforeFirst, InSet, AfterLast }
    }
}
