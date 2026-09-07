# T3 — M9 W/D/L ve Maç Sonucu Katmanı

Tarih: 07.09.2026

## Amaç

Aynı XI + aynı rakip için her taktik M9 üzerinden karşılaştırılabilir maç sonucu çıktısı üretir. Bu katman M10/M11 threshold veya anti-lock davranışını değiştirmez.

## Kanonik çıktı

`MatchPrediction` artık doğrudan şu iki türetilmiş metriği taşır:

- `ExpectedPoints = 3 × WinProbability + DrawProbability`
- `ExpectedGoalDifference = ExpectedHomeGoals - ExpectedAwayGoals`

Mevcut kanonik alanlar korunur:

- `ExpectedHomeGoals`
- `ExpectedAwayGoals`
- `WinProbability`
- `DrawProbability`
- `LossProbability`

## M9 olasılık üretimi

M9 mevcut bağımsız Poisson skor dağılımından W/D/L üretmeye devam eder. 0–20 gol aralığı hesaplanır ve toplam olasılık tekrar normalize edilir.

Mevcut üretim xG sınırı `0.05..5.00` değiştirilmedi.

## Monte Carlo

M9'un mevcut 18 tick Monte Carlo katmanı korunur. Aynı seed + aynı senaryo için sonuçların deterministik olması regression ile doğrulanır. Simülasyon çıktısı ayrıca:

- W/D/L
- en sık skor
- senaryo özetleri
- simülasyon kayıtları

üretmeye devam eder.

Monte Carlo sonucu, mevcut model kalibrasyonu tamamlanmadan gizli/live match-engine gerçeği olarak sunulmaz. `M9CalibrationStatus.StructuralModelAwaitingHistoricalCalibration` korunur.

## Taktik karşılaştırma

M9 auditinde aynı final XI + aynı rakip için yedi `TeamTactic` satırı korunur. Her satır için:

- W/D/L
- xG
- Expected Points
- Expected Goal Difference
- tactic fit / eligibility

kontrol edilir.

Bu aşamada taktik seçimi hâlâ `TacticFitScore` tabanlı mevcut davranıştır. M9 sonucunu doğrudan final taktik seçim kriteri yapmak sonraki DB3 / matchup karar katmanının işidir.

## Regression

`M9PredictionRegression` artık:

1. W/D/L toplamını,
2. Expected Points formülünü,
3. Expected Goal Difference formülünü,
4. Monte Carlo W/D/L toplamını,
5. aynı seed ile Monte Carlo deterministikliğini,
6. aynı XI için yedi taktiğin W/D/L + xG + outcome metriklerini

doğrular.

## Kapsam dışı

- M10/M11 threshold değişikliği yok.
- M10/M11 anti-lock değişikliği yok.
- DB3 kalıcı veri tabanı bu aşamada oluşturulmadı.
- Historical calibration katsayıları uydurulmadı.
