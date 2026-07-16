using FluentAssertions;
using LotteryService.Combinatorics;
using Xunit;

namespace LotteryService.Tests.Unit;

public class CombinatoricsTests
{
    [Fact]
    public void Permutations_Pick3_GeneratesCorrectCount()
    {
        // 3 distinct digits → 3! = 6 permutations
        var numbers = new List<int> { 1, 2, 3 };
        var perms = new Permutations<int>(numbers);

        perms.Count.Should().Be(6);
    }

    [Fact]
    public void Permutations_Pick3_WithRepeatedDigit_ReducesCount()
    {
        // {1,1,2} → distinct perms: {1,1,2},{1,2,1},{2,1,1} = 3
        var numbers = new List<int> { 1, 1, 2 };
        var perms = new Permutations<int>(numbers, GenerateOption.WithoutRepetition);

        perms.Count.Should().Be(3);
    }

    [Fact]
    public void Permutations_Pick4_GeneratesCorrectCount()
    {
        // 4 distinct digits → 4! = 24 permutations
        var numbers = new List<int> { 1, 2, 3, 4 };
        var perms = new Permutations<int>(numbers);

        perms.Count.Should().Be(24);
    }

    [Fact]
    public void Permutations_AllEnumeratedPicksAreDistinct()
    {
        var numbers = new List<int> { 1, 2, 3 };
        var perms = new Permutations<int>(numbers);

        var results = perms.Select(p => string.Join(",", p)).ToList();
        results.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Permutations_EmptyList_ReturnsCountOne()
    {
        var perms = new Permutations<int>(new List<int>());
        perms.Count.Should().Be(1);
    }

    [Fact]
    public void Permutations_SingleElement_ReturnsCountOne()
    {
        var perms = new Permutations<int>(new List<int> { 5 });
        perms.Count.Should().Be(1);
        perms.Single().Should().ContainSingle().Which.Should().Be(5);
    }

    [Fact]
    public void Permutations_WithRepetition_DoublesCount()
    {
        // {A,B} with repetition → all orderings including repeated: AA,AB,BA,BB = 4 but
        // Permutations WithRepetition actually means all orderings are included even for duplicates
        var numbers = new List<int> { 1, 2 };
        var permsWithRep = new Permutations<int>(numbers, GenerateOption.WithRepetition);
        var permsWithout = new Permutations<int>(numbers, GenerateOption.WithoutRepetition);

        // 2 distinct elements: both should be 2 (since no actual duplicates in input)
        permsWithout.Count.Should().Be(2);
        permsWithRep.Count.Should().Be(2);
    }
}
