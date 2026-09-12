using System.Collections.Generic;

namespace HattrickAI.HOEngine;

public enum TeamAttitude
{
    Normal,
    PIC,
    MOTS
}

public enum TeamLocation
{
    Home,
    Away,
    AwayDerby
}

/// <summary>
/// Match-level modifiers consumed by the legacy HO lineup rating engine.
/// Kept in the isolated HO adapter surface so the V5 rating pipeline remains untouched.
/// </summary>
public sealed class TeamMatchContext
{
    public int TacticType { get; init; }
    public int TacticLevel { get; init; }
    public TeamAttitude Attitude { get; init; } = TeamAttitude.Normal;
    public TeamLocation Location { get; init; } = TeamLocation.Away;
    public bool IsHome { get; init; }
    public int CoachModifier { get; init; }
    public double TeamSpirit { get; init; }
    public double Confidence { get; init; }
    public MatchWeather Weather { get; init; } = MatchWeather.Normal;
    public int Minute { get; init; }
    public TeamRatings? OpponentRatings { get; init; }
    public IReadOnlyDictionary<int, PlayerBehaviour> SlotBehaviours { get; init; }
        = new Dictionary<int, PlayerBehaviour>();
}
