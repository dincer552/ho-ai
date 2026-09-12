namespace HattrickAI.V5.Core;

public sealed record MatchQuestionnaire(
    CoachStyle Coach,
    TeamSpiritLevel TeamSpirit,
    TeamAttitude MatchImportance)
{
    public static MatchQuestionnaire Default => new(CoachStyle.Neutral, TeamSpiritLevel.Composed, TeamAttitude.Normal);
}

public enum CoachStyle
{
    Neutral,
    Offensive,
    Defensive
}

public enum TeamSpiritLevel
{
    Murderous,
    Furious,
    Irritated,
    Composed,
    Calm,
    Content,
    Satisfied,
    Delirious,
    WalkingOnClouds,
    ParadiseOnEarth
}

public static class QuestionnaireRatingAdjuster
{
    public static RegionalRatingSnapshot Apply(RegionalRatingSnapshot rating, MatchQuestionnaire q)
    {
        var attack = q.Coach switch
        {
            CoachStyle.Offensive => 1.08,
            CoachStyle.Defensive => 0.92,
            _ => 1.0
        };
        var defence = q.Coach switch
        {
            CoachStyle.Offensive => 0.89,
            CoachStyle.Defensive => 1.14,
            _ => 1.0
        };

        var spirit = q.TeamSpirit switch
        {
            TeamSpiritLevel.Murderous => .72,
            TeamSpiritLevel.Furious => .86,
            TeamSpiritLevel.Irritated => .93,
            TeamSpiritLevel.Composed => 1.00,
            TeamSpiritLevel.Calm => 1.07,
            TeamSpiritLevel.Content => 1.14,
            TeamSpiritLevel.Satisfied => 1.21,
            TeamSpiritLevel.Delirious => 1.28,
            TeamSpiritLevel.WalkingOnClouds => 1.35,
            TeamSpiritLevel.ParadiseOnEarth => 1.42,
            _ => 1.0
        };

        return Rebuild(
            rating,
            ld => ld * defence,
            cd => cd * defence,
            rd => rd * defence,
            mid => mid * spirit,
            la => la * attack,
            ca => ca * attack,
            ra => ra * attack);
    }

    private static RegionalRatingSnapshot Rebuild(
        RegionalRatingSnapshot r,
        Func<double,double> ld,
        Func<double,double> cd,
        Func<double,double> rd,
        Func<double,double> mid,
        Func<double,double> la,
        Func<double,double> ca,
        Func<double,double> ra)
    {
        var rawLd = ld(r.RawLeftDefence);
        var rawCd = cd(r.RawCentralDefence);
        var rawRd = rd(r.RawRightDefence);
        var rawMid = mid(r.RawMidfield);
        var rawLa = la(r.RawLeftAttack);
        var rawCa = ca(r.RawCentralAttack);
        var rawRa = ra(r.RawRightAttack);

        return new RegionalRatingSnapshot(
            rawLd, rawCd, rawRd, rawMid, rawLa, rawCa, rawRa,
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftDefence, rawLd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralDefence, rawCd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightDefence, rawRd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.Midfield, rawMid),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftAttack, rawLa),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralAttack, rawCa),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightAttack, rawRa));
    }
}
