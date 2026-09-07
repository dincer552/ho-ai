# T4 — DB3 Tactical Matchup

Tarih: 07.09.2026

## Amaç

DB2'de üretilen her XI için aynı rakibe karşı yedi taktiğin M9 sonuçlarını tek bir karşılaştırma katmanında toplamak.

## Kayıt anahtarı

`CandidateId + Tactic` benzersizdir.

Her kayıt şunları taşır:

- Formation / CandidateId
- Tactic / eligibility
- TacticFitScore / SquadFit / MatchupFit / TradeoffCost
- TacticalScore
- ExpectedHomeGoals / ExpectedAwayGoals
- Win / Draw / Loss
- ExpectedPoints
- ExpectedGoalDifference
- Explanation

## Kaynak

DB3 yeniden M8/M9 hesaplamaz. Mevcut kanonik `FormationTacticComparison` satırlarını dönüştürür. Böylece DB3 ile sitedeki taktik karşılaştırması arasında ikinci bir hesap yolu oluşmaz.

## Sıralama

Bir XI için taktikler `ExpectedPoints` önceliğiyle sıralanabilir; eşitlikte WinProbability ve deterministik tactic sırası kullanılır.

Bu, mevcut `TacticFitScore` hesabını silmez. Fit skoru açıklama/uygunluk sinyali olarak kalır. Final karar katmanının M9 outcome'a geçirilmesi bir sonraki aşamadır.

## Acceptance

T4 regression:

- tüm XI × 7 tactic kayıtlarının korunması,
- her CandidateId için tam 7 taktik,
- ExpectedPoints kanonikliği,
- bir XI için tek best-tactic sonucu,
- deterministik sıralama

kontrollerini yapar.

## Kapsam dışı

M10/M11 threshold ve anti-lock mekanizmasına dokunulmadı.
