namespace HattrickAI.V5.Core;

/// <summary>
/// CHPP-WRITE-07: compares the exact post-write CHPP snapshot with the payload
/// that was submitted. First version verifies XI, seven primary bench slots,
/// tactic and attitude; captain, set pieces and substitutions remain out of scope.
/// </summary>
public static class ChppMatchOrderReadBackVerifier
{
    public static ChppMatchOrderReadBackResult Verify(
        ChppMatchOrderPayload expected,
        ChppUpcomingMatchSnapshot actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (!actual.OrdersSet.GetValueOrDefault())
            return Fail("CHPP read-back OrdersSet=true değil.");

        if (actual.MatchId <= 0 || actual.TeamId <= 0)
            return Fail("CHPP read-back match/team kimliği geçersiz.");

        var expectedPositions = expected.Lineup.Positions;
        var actualStarters = actual.Players
            .Where(p => p.RoleId is >= 1 and <= 11)
            .OrderBy(p => p.RoleId)
            .ToArray();
        if (actualStarters.Length != 11)
            return Fail($"Read-back ilk 11 sayısı 11 değil: {actualStarters.Length}.");

        for (var i = 0; i < expectedPositions.Count; i++)
        {
            var expectedSlot = expectedPositions[i];
            var actual = actual.Players.FirstOrDefault(p => p.RoleId == i + 1);
            if (expectedSlot.Id <= 0 || actual is null || actual.PlayerId != expectedSlot.Id || actual.Behaviour != expectedSlot.Behaviour)
                return Fail($"XI eşleşmedi: RoleID {i + 1}.");
        }

        var expectedBench = expected.Lineup.Bench.Take(7).ToArray();
        for (var i = 0; i < expectedBench.Length; i++)
        {
            var actual = actual.Players.FirstOrDefault(p => p.RoleId == i + 12);
            if (expectedBench[i].Id <= 0 || actual is null || actual.PlayerId != expectedBench[i].Id)
                return Fail($"Primary bench eşleşmedi: slot {i + 1}.");
        }

        var tacticCode = expected.Lineup.Settings.Tactic;
        if (!int.TryParse(tacticCode, out var expectedTactic) || actual.TacticType != expectedTactic)
            return Fail($"Taktik eşleşmedi: beklenen {tacticCode}, CHPP {actual.TacticType}.");

        var attitude = expected.Lineup.Settings.SpeechLevel;
        if (!int.TryParse(attitude, out var expectedAttitude) || actual.Attitude != expectedAttitude)
            return Fail($"Attitude eşleşmedi: beklenen {attitude}, CHPP {actual.Attitude}.");

        return new ChppMatchOrderReadBackResult(true, "CHPP read-back doğrulandı.");
    }

    private static ChppMatchOrderReadBackResult Fail(string reason)
        => new(false, reason);
}

public sealed record ChppMatchOrderReadBackResult(bool Verified, string Reason);
