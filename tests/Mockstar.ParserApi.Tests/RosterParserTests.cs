using Mockstar.Scoring;
using Mockstar.ParserApi.Services.Rosters;
using Mockstar.Services.Rosters;

namespace Mockstar.ParserApi.Tests;

public sealed class RosterParserTests
{
    private readonly RosterParser _parser = new();

    [Fact]
    public void ParsesStrictlySwingCouples()
    {
        const string input = """
            Swing Summit
            Strictly Swing Novice Finals
            Heat 1
            123/456
            222 and 333
            """;

        var result = _parser.Parse(input);

        Assert.Equal(DivisionKind.StrictlySwing, result.DivisionKind);
        Assert.Equal(RoundPhase.Final, result.Phase);
        Assert.Collection(
            result.Heats.Single().Couples,
            first => Assert.Equal((123, 456), (first.LeaderBib, first.FollowerBib)),
            second => Assert.Equal((222, 333), (second.LeaderBib, second.FollowerBib)));
    }

    [Fact]
    public void DetectsLeaderAndFollowerSections()
    {
        const string input = """
            Liberty Swing
            Advanced Jack and Jill Semis
            Heat 1
            Leaders
            101 102 103
            Followers
            201 202 203
            """;

        var result = _parser.Parse(input);
        var heat = result.Heats.Single();

        Assert.Equal(RoundPhase.Semifinal, result.Phase);
        Assert.Equal(new[] { 101, 102, 103 }, heat.LeaderBibs);
        Assert.Equal(new[] { 201, 202, 203 }, heat.FollowerBibs);
    }

    [Fact]
    public void ParsesFinalsWithLeaderBibsOnly()
    {
        const string input = """
            Liberty Swing
            All-Star Jack and Jill Final
            Heat 1
            Leaders
            9
            11
            15
            """;

        var parsed = _parser.Parse(input);
        var normalizer = new RosterNormalizer(() => new DateTimeOffset(2026, 3, 8, 12, 30, 45, TimeSpan.Zero));
        var normalized = normalizer.Normalize(parsed, ImportSourceKind.Web);

        var heat = Assert.IsType<JackAndJillFinalHeat>(normalized.EventRecord.Heats.Single());
        Assert.Equal(new[] { 9, 11, 15 }, heat.LeaderEntries.Select(entry => entry.Bib));
        Assert.Empty(heat.FollowerEntries);
        Assert.Empty(heat.Pairings);
    }

    [Fact]
    public void SplitsMultipleHeats()
    {
        const string input = """
            Capital City Swing
            Intermediate Jack and Jill Prelim
            Heat 1
            Leaders
            1 2
            Followers
            11 12
            Heat 2
            Leaders
            3 4
            Followers
            13 14
            """;

        var result = _parser.Parse(input);

        Assert.Equal(2, result.Heats.Count);
        Assert.Equal(new[] { 1, 2 }, result.Heats[0].LeaderBibs);
        Assert.Equal(new[] { 13, 14 }, result.Heats[1].FollowerBibs);
    }

    [Fact]
    public void FallsBackToSingleHeatWhenNoHeatHeadersExist()
    {
        const string input = """
            Open Swing Classic
            Newcomer Jack and Jill
            Leaders
            12 14 16
            Followers
            22 24 26
            """;

        var result = _parser.Parse(input);

        Assert.Single(result.Heats);
        Assert.Equal("Heat 1", result.Heats.Single().Name);
    }

    [Theory]
    [InlineData("Quarterfinal", RoundPhase.Quarterfinal)]
    [InlineData("Semifinal", RoundPhase.Semifinal)]
    [InlineData("Final", RoundPhase.Final)]
    [InlineData("Prelim", RoundPhase.Prelim)]
    public void DetectsRoundPhaseAcrossLabels(string phaseLabel, RoundPhase expectedPhase)
    {
        var input = $"""
            Event
            Masters Jack and Jill {phaseLabel}
            Heat 1
            Leaders
            1
            """;

        var result = _parser.Parse(input);

        Assert.Equal(expectedPhase, result.Phase);
        Assert.Equal("Masters Jack and Jill " + phaseLabel, result.DivisionName);
    }

    [Fact]
    public void FlagsAmbiguousRoleAssignments()
    {
        const string input = """
            Liberty Swing
            Intermediate Jack and Jill
            Heat 1
            101
            102
            103
            """;

        var result = _parser.Parse(input);
        var heat = result.Heats.Single();

        Assert.True(heat.HasAmbiguousRoles);
        Assert.Equal(new[] { 101, 102, 103 }, heat.AmbiguousBibs);
        Assert.Empty(heat.LeaderBibs);
        Assert.Empty(heat.FollowerBibs);
    }

    [Fact]
    public void RecognizesBibLeaderHeaderAsLeaderOnlyHeat()
    {
        const string input = """
            ET

            Bib Leader

            #116 Alpha One

            #159 Alpha Two

            #168 Alpha Three

            #170 Alpha Four

            #302 Alpha Five

            #355 Alpha Six

            #366 Alpha Seven

            #378 Alpha Eight

            #381 Alpha Nine

            #452 Bravo Ten

            #469 Bravo Eleven

            #484 Bravo Twelve

            #493 Bravo Thirteen

            #504 Bravo Fourteen

            #574 Bravo Fifteen

            #583 Bravo Sixteen

            #584 Bravo Seventeen
            """;

        var result = _parser.Parse(input);
        var heat = result.Heats.Single();

        Assert.Equal(new[] { 116, 159, 168, 170, 302, 355, 366, 378, 381, 452, 469, 484, 493, 504, 574, 583, 584 }, heat.LeaderBibs);
        Assert.Empty(heat.FollowerBibs);
        Assert.False(heat.HasAmbiguousRoles);
    }

    [Fact]
    public void ParsesCombinedLeaderAndFollowerRowsIntoBothRoles()
    {
        const string input = """
            Advanced Jack&Jill prelim

            EE

            Bib Leader Pos Bib Follower

            #116 Alpha One 1 #121 Beta One

            #159 Alpha Two 2 #123 Beta Two

            #168 Alpha Three 3 #128 Beta Three

            #170 Alpha Four 4 #143 Beta Four

            #302 Alpha Five 5 #166 Beta Five

            #355 Alpha Six 6 #307 Beta Six

            #366 Alpha Seven 7 #344 Beta Seven

            #378 Alpha Eight 8 #409 Beta Eight

            #381 Alpha Nine 9 #417 Beta Nine

            #443 Alpha Ten 10 #461 Beta Ten

            #469 Alpha Eleven 11 #485 Beta Eleven

            #484 Alpha Twelve 12 #492 Beta Twelve

            #493 Alpha Thirteen 13 #517 Beta Thirteen

            #504 Alpha Fourteen 14 #544 Beta Fourteen

            #574 Alpha Fifteen 15 #576 Beta Fifteen

            #583 Alpha Sixteen 16 #593 Beta Sixteen

            #584 Alpha Seventeen 17 #701 Beta Seventeen
            """;

        var result = _parser.Parse(input);
        var heat = result.Heats.Single();

        Assert.Equal(
            new[] { 116, 159, 168, 170, 302, 355, 366, 378, 381, 443, 469, 484, 493, 504, 574, 583, 584 },
            heat.LeaderBibs);
        Assert.Equal(
            new[] { 121, 123, 128, 143, 166, 307, 344, 409, 417, 461, 485, 492, 517, 544, 576, 593, 701 },
            heat.FollowerBibs);
        Assert.False(heat.HasAmbiguousRoles);
    }
}
