# Tactic-Specific Objective Stage

## Amaç

Her XI için 7 taktik ayrı bir hedef fonksiyonu ile değerlendirilir. `TacticalScore` ortak bir taktik seçim skoru olarak kullanılmaz; yalnızca mevcut motor çıktısı olarak DB'de tutulur.

## Arama uzayı

- DB2'deki her XI
- Her XI × 7 TeamTactic
- Aynı XI için `Normal` baseline
- Taktik senaryosu M7.2 → M8 → M9 zincirinden geçirilir
- Taktik sonucu kadro uyumu + rakip matchup + mekanik değer + baseline trade-off ile değerlendirilir

## Taktik hedefleri

### Normal
Dengeli temel senaryo: yapısal şans, maç matchup'ı ve galibiyet olasılığı.

### Pressing
Ana hedef rakibin normal şans hacmini bastırmaktır. Kadronun defending + stamina kapasitesi, rakip suppression ve kendi şans kaybı birlikte değerlendirilir.

### Counter Attack
Ana hedef düşük midfield possession durumunu cezaya dönüştürmektir. CA eligibility, kaçırılan rakip normal şansları, CA dönüşüm oranı, passing + defending girdisi ve possession dezavantajı birlikte değerlendirilir.

### Attack Wings
Ana hedef kanat eşleşmesinden üretilen avantajdır. Sol/sağ attack-vs-defence matchup'ı, kanat şans payı, AoW dönüşüm oranı ve merkezdeki trade-off birlikte değerlendirilir.

### Attack Middle
Ana hedef merkez attack-vs-centre-defence avantajıdır. Merkez şans payı, AiM dönüşüm oranı ve kanatlarda kaybedilen hacim birlikte değerlendirilir.

### Creative
Ana hedef özel olay potansiyelidir. Creative event multiplier, passing + experience + playmaking kadro uyumu, M9 özel olay çıktısı ve normal senaryoya göre trade-off birlikte değerlendirilir.

### Long Shots
Ana hedef uzun şut üretimidir. Long-shot fırsat hacmi, scoring + passing + experience kadro uyumu ve rakip kaleci eşleşmesi birlikte değerlendirilir. Uzun şutların normal şans hacminden aldığı pay trade-off olarak tutulur.

## Kadro uyumu

Taktik uygunluğu doğrudan aynı XI'nin ilgili beceri toplamlarından çıkarılır. Böylece örneğin yüksek Pressing seviyesi tek başına Pressing'in iyi seçim olduğu anlamına gelmez; XI'nin defending/stamina yapısı da yeterli olmalıdır. Aynı şekilde Long Shots için scoring/passing, Creative için passing/experience/playmaking, CA için passing/defending, AoW için winger/passing/scoring, AiM için passing/playmaking/scoring dikkate alınır.

## Seçim

Taktik adayları `TacticFitScore` üzerinden sıralanır. `TacticPrimaryMetric`, `TacticSquadFit`, `TacticMatchupFit` ve `TacticTradeoffCost` açıklanabilir alt metriklerdir. CA eligibility başarısızsa aday seçilemez.

Mevcut M10/M11 threshold/anti-lock mekanizması değiştirilmez. Bu aşama mevcut final seçim hattına taktik boyutu ekler.

## DB doğrulama planı

1. C20 ve normal build doğrulaması.
2. Gerçek web analizi çalıştırılır.
3. Motor DB JSON içinden `TacticComparisons` alınır.
4. DB2 aday sayısı × 7 kadar tactic kaydı olduğu kontrol edilir.
5. Her CandidateId için tam 7 taktik bulunması kontrol edilir.
6. Her taktik için `TacticFitScore`, `TacticPrimaryMetric`, `TacticSquadFit`, `TacticMatchupFit`, `TacticTradeoffCost` ve `TacticEligible` incelenir.
7. Aynı XI'nin 7 taktik sonucu yan yana karşılaştırılır; generic `TacticalScore` ile `TacticFitScore` arasındaki fark özellikle kontrol edilir.
8. Pressing, CA, AoW, AiM, Creative ve Long Shots için mekanik açıklama ile sayısal çıktı tutarlılığı kontrol edilir.
9. Beklenmeyen sonuçlar görülürse katsayı/threshold değiştirilmeden önce neden analizi yapılır.

## Sonraki aşama

Gerçek DB çıktısından ilk sonuçlar incelendikten sonra gerekirse her taktik için objective ağırlıkları ve kalibrasyon ayrı ayrı revize edilir. Bu aşamada eşik mekanizması değiştirilmez.
