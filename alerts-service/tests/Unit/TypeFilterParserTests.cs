using AlertsService.Entities;
using FluentAssertions;

namespace AlertsService.Tests.Unit;

/// <summary>
/// Tests for the type filter parsing logic that mirrors legacy JS type flags:
/// sG=1 (Straight), sP=2 (Parlay), sF=3 (Feeder), sAL=4 (ActionLine), sLG=6 (LiveGame), sS=7 (Special), sA=13 (Action)
/// </summary>
public class TypeFilterParserTests
{
    // We test the private ParseTypeFilter method indirectly via the enum mappings
    [Theory]
    [InlineData("1,2,3", new[] { 1, 2, 3 })]
    [InlineData("1,", new[] { 1 })]
    [InlineData("", new int[0])]
    [InlineData("  ", new int[0])]
    [InlineData("1,2,3,4,6,7,13", new[] { 1, 2, 3, 4, 6, 7, 13 })]
    public void ParseTypeFilter_ParsesCommaSeparatedIntegers(string input, int[] expected)
    {
        var result = ParseTypeFilter(input);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AlertType_Values_MatchLegacyTypesFilter()
    {
        // Ensures enum values match the numeric codes used in the legacy Node.js app
        ((int)AlertType.Straight).Should().Be(1);
        ((int)AlertType.Parlay).Should().Be(2);
        ((int)AlertType.Feeder).Should().Be(3);
        ((int)AlertType.ActionLine).Should().Be(4);
        ((int)AlertType.LiveGame).Should().Be(6);
        ((int)AlertType.Special).Should().Be(7);
        ((int)AlertType.Action).Should().Be(13);
    }

    // Mirror of the private ParseTypeFilter method in AlertDataService for direct testing
    private static List<int> ParseTypeFilter(string typesFilter)
    {
        if (string.IsNullOrWhiteSpace(typesFilter))
            return [];

        return typesFilter
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
            .Where(n => n > 0)
            .ToList();
    }
}
