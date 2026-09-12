using System;
namespace HattrickAI.V5.Core;

public sealed class FoxtrickEngine : IRatingEngine
{
    private readonly RegionalRatingEngine _sectorSource = new();
    public RatingEngineKind Kind => RatingEngineKind.Foxtrick;
    public string Name => "Foxtrick";

    public RatingEngineResult Calculate(RatingEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = _sectorSource.CalculateLineup(request.Lineup, request.Players, request.Context);
        var stats = FoxtrickRatingStatistics.Calculate(snapshot, request.Context.Tactic);
        return new RatingEngineResult(Kind, snapshot, stats.HatStats, stats.LoddarStats);
    }
}

public sealed record FoxtrickRatingStatisticsResult(double HatStats, double LoddarStats, double PeasoStats, double VnukStats, double HTitaVal, double GardierStats);

public static class FoxtrickRatingStatistics
{
    public static FoxtrickRatingStatisticsResult Calculate(RegionalRatingSnapshot r, TeamTactic tactic)
        => new(HatStats(r), LoddarStats(r, tactic), PeasoStats(r), VnukStats(r), HTitaVal(r), GardierStats(r, tactic));

    public static double HatStats(RegionalRatingSnapshot r)
        => Math.Max(0, 3 * (1 + 4 * r.RawMidfield) + 3 + 4 * (r.RawLeftAttack + r.RawCentralAttack + r.RawRightAttack) + 3 + 4 * (r.RawLeftDefence + r.RawCentralDefence + r.RawRightDefence));

    public static double LoddarStats(RegionalRatingSnapshot r, TeamTactic tactic)
    {
        static double H(double x) => 2 * x / (x + 80);
        var mf=1+4*r.RawMidfield; var la=1+4*r.RawLeftAttack; var ca=1+4*r.RawCentralAttack; var ra=1+4*r.RawRightAttack;
        var ld=1+4*r.RawLeftDefence; var cd=1+4*r.RawCentralDefence; var rd=1+4*r.RawRightDefence;
        const double vf=.47, zg=.37, kg=.25;
        var kk=tactic==TeamTactic.CounterAttack ? kg*2/(1+20) : 0;
        var kzg=zg;
        if(tactic==TeamTactic.AttackMiddle) kzg += .2*(1-1)/19+.2;
        if(tactic==TeamTactic.AttackWings) kzg -= .2*(1-1)/19+.2;
        var kag=(1-kzg)/2;
        var attack=(1-vf+kk)*(kzg*H(ca)+kag*(H(la)+H(ra)));
        var defence=vf*(zg*H(cd)+((1-zg)/2)*(H(ld)+H(rd)));
        return Math.Round(80*H(mf)*(attack+defence),2,MidpointRounding.ToEven);
    }

    public static double PeasoStats(RegionalRatingSnapshot r)
    {
        var mf=1+4*r.RawMidfield; var la=1+4*r.RawLeftAttack; var ca=1+4*r.RawCentralAttack; var ra=1+4*r.RawRightAttack;
        var ld=1+4*r.RawLeftDefence; var cd=1+4*r.RawCentralDefence; var rd=1+4*r.RawRightDefence;
        return Math.Max(0,Math.Round(.46*mf+.32*(.3*(la+ra)+.4*ca)+.22*(.3*(ld+rd)+.4*cd),2,MidpointRounding.ToEven));
    }

    public static double VnukStats(RegionalRatingSnapshot r)
        => Math.Round((11+5*r.RawMidfield+r.RawLeftAttack+r.RawCentralAttack+r.RawRightAttack+r.RawLeftDefence+r.RawCentralDefence+r.RawRightDefence)/11,2,MidpointRounding.ToEven);

    public static double HTitaVal(RegionalRatingSnapshot r)
    {
        var value=3*(1+4*r.RawMidfield)+.8*((1+4*r.RawLeftAttack)+(1+4*r.RawRightAttack))+1.4*(1+4*r.RawCentralAttack)+.64*((1+4*r.RawLeftDefence)+(1+4*r.RawRightDefence))+1.12*(1+4*r.RawCentralDefence);
        return Math.Max(0,Math.Round(value,1,MidpointRounding.ToEven));
    }

    public static double GardierStats(RegionalRatingSnapshot r, TeamTactic tactic)
    {
        var la=1+4*r.RawLeftAttack; var ca=1+4*r.RawCentralAttack; var ra=1+4*r.RawRightAttack; var ld=1+4*r.RawLeftDefence; var cd=1+4*r.RawCentralDefence; var rd=1+4*r.RawRightDefence; var mf=1+4*r.RawMidfield;
        var defence=.275*rd+.45*cd+.275*ld; var attack=.275*ra+.45*ca+.275*la; var real=4.15*mf+2.77*attack+2.08*defence;
        var tactical=tactic==TeamTactic.CounterAttack ? defence/10 : tactic==TeamTactic.AttackMiddle ? ca/7 : tactic==TeamTactic.AttackWings ? (ra+la)/14 : real/9;
        return Math.Max(0,Math.Round(real+tactical,MidpointRounding.ToEven));
    }
}
