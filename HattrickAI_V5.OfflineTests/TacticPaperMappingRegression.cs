using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class TacticPaperMappingRegression
{
    public static int Run()
    {
        AssertNear(0.0, TacticPaperMappingEngine.ToPaperRt(0), "V5 0 -> RT 0");
        AssertNear(10.0, TacticPaperMappingEngine.ToPaperRt(5), "V5 5 -> RT 10");
        AssertNear(20.0, TacticPaperMappingEngine.ToPaperRt(10), "V5 10 -> RT 20");
        AssertNear(20.0, TacticPaperMappingEngine.ToPaperRt(999), "V5 upper clamp -> RT 20");
        AssertNear(0.0, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.Normal, 5), "Normal has no TCR");

        AssertNear(0.20, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.AttackMiddle, 0), "AiM V5 0 -> 20% floor");
        AssertNear(0.35, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.AttackMiddle, 10), "AiM V5 10 -> 35% ceiling");
        AssertStrictlyIncreasing(AdvancedTactic.AttackMiddle, "AiM conversion is monotonic across V5 levels");
        AssertSectorEnvelope(AdvancedTactic.AttackMiddle, 0, 0.46, 0.48, "AiM low-level centre share");
        AssertSectorEnvelope(AdvancedTactic.AttackMiddle, 10, 0.53, 0.56, "AiM high-level centre share");

        AssertNear(0.34, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.AttackWings, 0), "AoW V5 0 -> 34% floor");
        AssertNear(0.52, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.AttackWings, 10), "AoW V5 10 -> 52% ceiling");
        AssertStrictlyIncreasing(AdvancedTactic.AttackWings, "AoW conversion is monotonic across V5 levels");
        AssertSectorEnvelope(AdvancedTactic.AttackWings, 0, 0.61, 0.64, "AoW low-level wing share");
        AssertSectorEnvelope(AdvancedTactic.AttackWings, 10, 0.66, 0.71, "AoW high-level wing share");

        AssertNear(0.04, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.CounterAttack, 0), "CA V5 0 -> 4% floor");
        AssertNear(0.45, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.CounterAttack, 10), "CA V5 10 -> 45% ceiling");
        AssertStrictlyIncreasing(AdvancedTactic.CounterAttack, "CA conversion is monotonic across V5 levels");

        AssertNear(
            M8ChanceAllocationEngine.CalculateTacticConversionRateFromPaperRt(AdvancedTactic.LongShots, 20),
            TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.LongShots, 10),
            "V5 10 -> paper RT 20 LS");

        AssertNear(0.05, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.Pressing, 0), "Pressing V5 0 -> 5% floor");
        AssertNear(0.41, TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.Pressing, 10), "Pressing V5 10 -> 41% ceiling");
        var midPressing = TacticPaperMappingEngine.PaperTacticConversionRate(AdvancedTactic.Pressing, 5);
        if (midPressing <= 0.05 || midPressing >= 0.41)
            throw new InvalidOperationException($"TacticPaperMappingRegression failed: Pressing V5 5 must stay inside 5%-41%; actual {midPressing:P2}");

        Console.WriteLine("TacticPaperMappingRegression: PASS | tactic-specific RT bridges + monotonic conversion + sector envelopes");
        return 0;
    }

    private static void AssertSectorEnvelope(AdvancedTactic tactic, double level, double min, double max, string message)
    {
        var allocation = M8ChanceAllocationEngine.Calculate(50.0, 50.0, tactic, level);
        var sum = allocation.SectorLeftShare + allocation.SectorCentreShare + allocation.SectorRightShare + allocation.SectorSetPieceShare;
        if (Math.Abs(sum - 1.0) > 1e-9)
            throw new InvalidOperationException($"TacticPaperMappingRegression failed: {message}; sector sum={sum}");

        var target = tactic == AdvancedTactic.AttackMiddle
            ? allocation.SectorCentreShare
            : allocation.SectorLeftShare + allocation.SectorRightShare;
        if (target < min || target > max)
            throw new InvalidOperationException($"TacticPaperMappingRegression failed: {message}; expected [{min:P1},{max:P1}], actual {target:P2}");
    }

    private static void AssertStrictlyIncreasing(AdvancedTactic tactic, string message)
    {
        var previous = TacticPaperMappingEngine.PaperTacticConversionRate(tactic, 0);
        for (var level = 1; level <= 10; level++)
        {
            var current = TacticPaperMappingEngine.PaperTacticConversionRate(tactic, level);
            if (current <= previous)
                throw new InvalidOperationException($"TacticPaperMappingRegression failed: {message}; level={level}; previous={previous}; current={current}");
            previous = current;
        }
    }

    private static void AssertNear(double expected, double actual, string message)
    {
        if (Math.Abs(expected - actual) > 1e-9)
            throw new InvalidOperationException($"TacticPaperMappingRegression failed: {message}; expected {expected}, actual {actual}");
    }
}
