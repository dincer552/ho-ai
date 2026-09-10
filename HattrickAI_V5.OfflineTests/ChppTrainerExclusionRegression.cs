using System.Xml.Linq;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class ChppTrainerExclusionRegression
{
    public static int Run()
    {
        const string teamXml = "<Team><TeamID>123</TeamID><TeamName>Test</TeamName><Trainer><PlayerID>437860841</PlayerID></Trainer></Team>";
        var team = XDocument.Parse(teamXml).Root;
        var trainerId = ChppRosterFilter.ReadTrainerId(team);
        if (trainerId != 437860841) throw new InvalidOperationException($"TrainerID parse hatalı: {trainerId}");

        var players = new List<Player>
        {
            new(437860841, "Antonín Vašica", 1, 1, 1, 1, 1, 1, 1, 7, 8),
            new(111, "Normal Player", 5, 6, 7, 6, 5, 6, 7, 7, 8)
        };
        var filtered = ChppRosterFilter.ExcludeTrainer(players, trainerId);
        if (filtered.Any(p => p.Id == trainerId)) throw new InvalidOperationException("Teknik direktör oyuncu havuzundan çıkarılmadı.");
        if (filtered.Count != 1 || filtered[0].Id != 111) throw new InvalidOperationException("Normal oyuncu yanlışlıkla filtrelendi.");

        Console.WriteLine($"PASS: trainer {trainerId} excluded; roster={filtered.Count}");
        return 0;
    }
}
