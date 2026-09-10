using System.Globalization;
using System.Xml.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// CHPP-WRITE-01 read-only katmanı.
/// Yaklaşan takım maçını ve aynı maç için mevcut match order kaydını okur.
/// Bu sınıf hiçbir CHPP write çağrısı yapmaz.
/// </summary>
public sealed class ChppMatchOrderReadService
{
    private readonly ChppV5 _chpp;

    public ChppMatchOrderReadService(ChppV5 chpp) => _chpp = chpp;

    public async Task<ChppUpcomingMatchSnapshot> ReadUpcomingAsync(CancellationToken ct)
    {
        if (!_chpp.Connected)
            throw new UnauthorizedAccessException("CHPP bağlantısı yok.");

        var teamXml = await _chpp.GetXmlAsync("teamdetails", new Dictionary<string, string?>
        {
            ["version"] = "3.0"
        }, ct);

        var team = XmlV5.Root(teamXml)?.Descendants("Team").FirstOrDefault();
        var teamId = XmlV5.Int(team, "TeamID");
        var teamName = XmlV5.Text(team, "TeamName");
        if (teamId <= 0)
            throw new InvalidOperationException("CHPP takım ID'si alınamadı.");

        var matchesXml = await _chpp.GetXmlAsync("matches", new Dictionary<string, string?>
        {
            ["version"] = "2.2",
            ["teamID"] = teamId.ToString(CultureInfo.InvariantCulture)
        }, ct);

        var upcoming = XmlV5.Root(matchesXml)?.Descendants("Match")
            .Select(m => new
            {
                MatchId = XmlV5.Int(m, "MatchID"),
                Date = XmlV5.Date(m, "MatchDate"),
                HomeId = XmlV5.Int(m, "HomeTeamID"),
                AwayId = XmlV5.Int(m, "AwayTeamID"),
                HomeName = XmlV5.Text(m, "HomeTeamName"),
                AwayName = XmlV5.Text(m, "AwayTeamName"),
                MatchType = XmlV5.Int(m, "MatchType")
            })
            .Where(m => m.MatchId > 0 && m.Date != default && m.Date > DateTimeOffset.UtcNow
                        && (m.HomeId == teamId || m.AwayId == teamId))
            .OrderBy(m => m.Date)
            .FirstOrDefault();

        if (upcoming is null)
            throw new InvalidOperationException("Yaklaşan maç bulunamadı.");

        var orderXml = await _chpp.GetXmlAsync("matchorders", new Dictionary<string, string?>
        {
            ["version"] = "3.1",
            ["matchID"] = upcoming.MatchId.ToString(CultureInfo.InvariantCulture),
            ["teamID"] = teamId.ToString(CultureInfo.InvariantCulture)
        }, ct);

        return ParseSnapshot(teamId, teamName, upcoming.MatchId, upcoming.Date,
            upcoming.HomeId, upcoming.AwayId, upcoming.HomeName, upcoming.AwayName,
            upcoming.MatchType, orderXml);
    }

    private static ChppUpcomingMatchSnapshot ParseSnapshot(
        int teamId,
        string teamName,
        int matchId,
        DateTimeOffset matchDate,
        int homeId,
        int awayId,
        string homeName,
        string awayName,
        int matchType,
        string orderXml)
    {
        var root = XmlV5.Root(orderXml);
        var matchData = root?.Descendants("MatchData").FirstOrDefault() ?? root;
        var tacticType = XmlV5.Int(matchData, "TacticType");
        var attitude = XmlV5.Int(matchData, "Attitude");
        var ordersSetText = XmlV5.Text(matchData, "OrdersSet");
        var ordersSet = bool.TryParse(ordersSetText, out var parsedOrdersSet) ? parsedOrdersSet : (bool?)null;

        var lineupRoot = matchData?.Descendants("Lineup").FirstOrDefault();
        var players = lineupRoot?.Descendants("Player")
            .Select(p => new ChppMatchOrderPlayer(
                XmlV5.Int(p, "PlayerID"),
                XmlV5.Int(p, "RoleID"),
                XmlV5.Int(p, "PositionCode"),
                XmlV5.Int(p, "Behaviour"),
                XmlV5.Text(p, "PlayerName")))
            .Where(p => p.PlayerId > 0)
            .ToList() ?? new List<ChppMatchOrderPlayer>();

        return new ChppUpcomingMatchSnapshot(
            teamId,
            teamName,
            matchId,
            matchDate,
            homeId,
            awayId,
            homeName,
            awayName,
            matchType,
            tacticType,
            attitude,
            ordersSet,
            players,
            string.IsNullOrWhiteSpace(orderXml) ? "empty" : "xml-read");
    }
}

public sealed record ChppUpcomingMatchSnapshot(
    int TeamId,
    string TeamName,
    int MatchId,
    DateTimeOffset MatchDate,
    int HomeTeamId,
    int AwayTeamId,
    string HomeTeamName,
    string AwayTeamName,
    int MatchType,
    int TacticType,
    int Attitude,
    bool? OrdersSet,
    IReadOnlyList<ChppMatchOrderPlayer> Players,
    string ReadStatus);

public sealed record ChppMatchOrderPlayer(
    int PlayerId,
    int RoleId,
    int PositionCode,
    int Behaviour,
    string PlayerName);
