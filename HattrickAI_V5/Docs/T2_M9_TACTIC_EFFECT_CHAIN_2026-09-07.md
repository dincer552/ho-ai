# T2 — M9 Tactic Effect Chain — 2026-09-07

## Amaç

Her taktiğin M8'de oluşturduğu mekanik değişimin M9 expected-goals ve W/D/L hesabına gerçekten taşınmasını sağlamak. M10/M11 threshold ve anti-lock mekanizmasına dokunulmaz.

## Kaynak temeli

- Hattrick 2026 akademik paper: M8 chance allocation, tactic conversion/suppression ve tactic-specific chance dönüşümleri.
- Hattrick Manual / Wiki: tacticlerin normal chance, special event, long-shot ve goalkeeper etkileşimleri.
- Hattrick Developer Blog: Creative event generation/ownership, specialty etkileri ve Pressing/Powerful event davranışları.

## T2 uygulaması

### Normal
Normal değişmeyen baseline'dır. M9, M8'in regular sector chance hacmini ve normal sector scoring probability'sini doğrudan kullanır.

### Creative
`M9EventGoalEngine`, takımın gerçek `Creative` taktiğini alır ve `CreativeEventMultiplier` ile player/team special-event bütçesini artırır. Specialty bazlı pozitif/negatif event eligibility korunur. Rakip perspektifinde kendi Creative multiplier'ını yanlışlıkla uygulamamak için rakip event hesabı `Normal` olarak değerlendirilir; rakibin ayrı taktik seçimini taşıyacak matchup katmanı ileride eklenebilir.

### Pressing
Pressing'in normal chance suppression'ı M8 allocation katmanında iki takımın regular chance hacmine uygulanır. M9 ayrıca mevcut PDIM/sitting-midfielder event suppression sinyalini normal chance hacmine uygular. Special events normal chance suppression'dan ayrı kalır.

### Counter Attack
M8, CA eligibility'yi pre-penalty midfield karşılaştırmasından üretir ve %7 midfield penalty ile opportunity/conversion hacmini hesaplar. M9 `CounterAttackChanceExpected * ownRegularQuality` ile CA goal katkısını total expected goals içine taşır. Böylece CA fırsatı normal chance goal hesabından ayrı görünür.

### Attack in the Middle / Attack on Wings
M8 tactic-specific sector conversion sonucunu `LeftChanceShare/CentreChanceShare/RightChanceShare` olarak üretir. M9 sector scoring probability'yi bu dağılımlarla ağırlıklandırdığı için centre/wing chance redistribution doğrudan normal expected goals'a taşınır. Evaluator'ların savunma/opportunity-cost proxyleri seçim katmanında kalır; M9'a gizli savunma katsayısı eklenmez.

### Long Shots
M8 `LongShotChanceExpected` ile regular attacks'tan LS fırsat hacmini ayırır ve `NormalRegularChanceExpectedAfterLongShots` ile normal chance kaybını hesaplar. M9 artık LS fırsatını gerçek shooter-vs-GK goal check'e taşır.

Kullanılan yayınlanmış Wiki mekanizması:

- Shooter = `0.0643 * Scoring^2.3808 * SetPieces^2.7720`
- Goalkeeper = `1977.4524 * Goalkeeping^0.9 + 31.4827 * SetPieces^2.3262`
- `P(LS goal) = Shooter / (Shooter + Goalkeeper)`
- IM ve winger shooter ağırlığı 2x; defender/forward 1x.

Bu hesap yayınlanmış/community formula olarak kullanılır; gizli live-engine coefficient iddiası değildir.

## Kritik düzeltme

Önceki M9 akışında `chance.Tactic` hem kendi event engine'ine hem rakip event engine'ine veriliyordu. Bu, kendi takımının Creative/Pressing seçiminin rakibe de uygulanmasına yol açabilirdi. T2 ile rakip perspektifi `Normal` olarak ayrıldı. Gelecekte DB3 gerçek rakip taktiğini taşıdığında ikinci bir opponent-tactic parametresi eklenebilir.

## Sonuç

T2 kapsamında M8 -> M9 zincirinde tactic-specific mekaniklerin total expected-goals/W-D-L hesabına taşınması kodlandı. M9 kalibrasyonu halen `StructuralModelAwaitingHistoricalCalibration` statüsündedir; çıktı gerçek motor kalibrasyonu olarak sunulmaz.
