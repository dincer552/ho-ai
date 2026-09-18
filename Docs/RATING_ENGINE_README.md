# HattrickAI V5 — Rating Engine Katmanı

## Durum

**TAMAMLANDI — production display isolation fix uygulandı; acceptance yeniden koşuluyor.**

Production motorları:

`V5 | HO | HattrickDash | Foxtrick`

**Varsayılan motor: V5.** Mevcut V5 davranışı ve M3→M11 production pipeline'ı korunur.

## Amaç

Aynı canonical `Lineup + Player + RatingContext` girdisini bağımsız rating motorlarında çalıştırmak ve motorları V5 davranışını değiştirmeden karşılaştırabilmek.

## Motorlar

### V5
Mevcut V5 rating katmanının adapter'ıdır. `RegionalRatingEngineFixed` üzerinden canonical V5 regional rating üretir. Contract regression, adapter parity'sini mevcut Fixed motoruna karşı kontrol eder.

### HO
Legacy HO rating hesapları bağımsız adapter altında çalışır. 7 bölgesel sektör ile HatStats/LoddarStats ortak `RatingEngineResult` modeline taşınır. Gerçek CHPP fixture regression ile sektör ve context davranışları kilitlidir.

### HattrickDash
Açık kaynak Dash local lineup/analytics mantığı uygulanır. Pozisyon tahmini `primary × 0.70 + form × 0.20 + stamina × 0.10` ile başlar; midfield/defence/attack aggregate'leri ve Dash HatStats/LoddarStats ayrıdır.

Dash'in açık kaynak kodu Hattrick'ın kapalı server-side rating preview algoritmasını yeniden üretmez; bu sınır korunur.

### Foxtrick
Foxtrick `ratings.js` içindeki açık kaynak istatistik formülleri ayrı motor olarak uygulanır:

- HatStats
- LoddarStats
- PeasoStats
- VnukStats
- HTitaVal
- GardierStats

Foxtrick sektör üretiminin Hattrick server-side kısmı kapalı olduğu için canonical V5 **raw regional sectors** ortak veri kaynağı olarak kullanılır; Foxtrick istatistik katmanı bunun üzerine uygulanır.

## Ortak model

`IRatingEngine`:

- `Kind`
- `Name`
- `Calculate(RatingEngineRequest)`

`RatingEngineRequest` canonical V5 snapshot'ını opsiyonel olarak taşıyabilir. Böylece Foxtrick gibi server-side sektör üretimini yeniden kurmayan motorlar gerçek analizde Hattrick/V5'nin doğrulanmış raw sektörlerini kullanır.

`RatingEngineResult`:

- engine identity
- 7 regional sector
- optional HatStats
- optional LoddarStats

`RegionalRatingSnapshot` raw ve display ratingleri ayrı tutar.

## Kritik display sözleşmesi — 13.09.2026

Production'da **V5, HO, HattrickDash ve Foxtrick birbirlerinin display converter'ını kullanmaz.** Her motorun `Calculate()` sonucu kendi rating alanında doğrudan gösterilebilir.

Özellikle `HattrickRatingDisplayConverter` yalnızca deneysel Stage-2 raw→display dönüşüm katmanıdır. Production final rating zincirinde tekrar uygulanamaz.

### Tespit edilen bug

Motor seçimi backend'de doğru çalışmasına rağmen final `Analysis` sonucu oluşturulurken `ConfidenceRatingAdjuster` her motorun raw sektörlerini tekrar `HattrickRatingDisplayConverter.ToDisplay(...)` üzerinden geçiriyordu. Bu, normal 7–16 bandındaki değerleri yaklaşık 1–6 bandına sıkıştırabiliyordu.

Örnek production smoke gözlemi:

- V5 seçili analizde saha ratingleri yaklaşık `5.75 / 3.55 / 5.57 / 2.14 / 3.52 / 3.84 / 3.61` görünüyordu.
- HO seçili analizde yaklaşık `3.83 / 2.19 / 4.70 / 1.75 / 3.24 / 3.58 / 3.07` görünüyordu.

Sorun motor seçiminin kendisi değil, seçilen motorun final rating'inin ikinci kez yanlış display dönüşümünden geçirilmesiydi.

### Fix

`ConfidenceRatingAdjuster` artık:

1. seçilen motorun raw rating ledger'ını confidence ile gerektiği kadar değiştirir,
2. sonucu aynı motorun display space'inde doğrudan yayınlar,
3. `HattrickRatingDisplayConverter` çağırmaz.

Böylece seçilen motor V5/HO/HattrickDash/Foxtrick olsa da saha üzerindeki 7 rating doğrudan o motorun hesapladığı değerlerdir.

Bu değişiklik V5 katsayılarını veya Stage-2 converter'ı değiştirmez.

## V5 — 14 Pozisyon Bağımsız Katkı Matrisi

V5 rating modeli, Hattrick saha yerleşimini **14 ayrı canonical slot** olarak ele alır. Aynı rol ailesi içinde dahi slotlar birbirine eşit kabul edilmez. `WB-L/WB-R` wing-back, `DEF-CL/DEF-C/DEF-CR` ise üç ayrı central-defender slotudur. Eski `DEF-L/DEF-R` kodları yalnızca geriye dönük central-defender alias'ı olarak kabul edilir.

### Savunma hattı — 5 ayrı slot

| Slot | Normal temel katkı |
|---|---|
| `WB-L` | Central Defence = Defending × 0.083; Left Defence = Defending × 0.268; Midfield = Playmaking × 0.023; Left Attack = Winger × 0.129 |
| `DEF-CL` | Central Defence = Defending × 0.186; Left Defence = Defending × 0.077; Midfield = Playmaking × 0.035 |
| `DEF-C` | Central Defence = Defending × 0.186; Side Defence = Defending × 0.077; Midfield = Playmaking × 0.035 |
| `DEF-CR` | Central Defence = Defending × 0.186; Right Defence = Defending × 0.077; Midfield = Playmaking × 0.035 |
| `WB-R` | Central Defence = Defending × 0.083; Right Defence = Defending × 0.268; Midfield = Playmaking × 0.023; Right Attack = Winger × 0.129 |

`DEF-L` / `DEF-R` artık diziliş sayısına bakılarak otomatik olarak WingBack'e dönüştürülmez. Canonical hesapta gerçek WingBack `WB-L/WB-R`, yan stoper `DEF-CL/DEF-CR` koduyla temsil edilir. Böylece aynı XI içinde örneğin `WB-L + DEF-L(alias)` açıkça iki farklı rol olarak hesaplanabilir.

Central Defender tarafında merkez/yan yerleşimin savunma dağılımı ve birden fazla CD olduğunda overcrowding etkisi ayrıca uygulanır; tek bir ortak DEF katsayısı kullanılmaz.

### Orta saha hattı — 5 ayrı slot

`W-L | IM-L | IM-C | IM-R | W-R`

**Normal Inner Midfielder:**
- Central Defence = Defending × 0.070
- Side Defence = Defending × 0.028
- Midfield = Playmaking × 0.139
- Side Attack = Passing × 0.028
- Central Attack = Passing × 0.057 + Scoring × 0.038

**Defensive Inner Midfielder:**
- Central Defence = Defending × 0.115
- Side Defence = Defending × 0.040
- Midfield = Playmaking × 0.131
- Side Attack = Passing × 0.018
- Central Attack = Passing × 0.039 + Scoring × 0.028

**Offensive Inner Midfielder:**
- Central Defence = Defending × 0.115
- Side Defence = Defending × 0.040
- Midfield = Playmaking × 0.131
- Side Attack = Passing × 0.018
- Central Attack = Passing × 0.039 + Scoring × 0.025

`IM-L`, `IM-C` ve `IM-R` aynı katkıları farklı yönlere taşır. Merkez oyuncunun iki yana dağıttığı katkı ile sol/sağ oyuncunun kendi tarafına taşıdığı katkı ayrı uygulanmalıdır.

**Normal Winger:**
- Central Defence = Defending × 0.037
- Side Defence = Defending × 0.104
- Midfield = Playmaking × 0.065
- Own Side Attack = Winger × 0.219 + Passing × 0.054
- Central Attack = Passing × 0.018

`W-L` sol bölgeye, `W-R` sağ bölgeye yönlendirilir. Winger emirleri Normal / Defensive / Offensive / Towards Middle olarak ayrı tutulmalıdır.

### Forvet hattı — 3 ayrı slot

`FW-L | FW-C | FW-R`

**Normal FW-C:**
- Midfield = Playmaking × 0.041
- Left Attack = Scoring × 0.058 + Passing × 0.048 + Winger × 0.032
- Right Attack = Scoring × 0.058 + Passing × 0.048 + Winger × 0.032
- Central Attack = Scoring × 0.178 + Passing × 0.066

**Normal FW-L:**
- Midfield = Playmaking × 0.041
- Left Attack = Scoring × 0.058 + Passing × 0.048 + Winger × 0.032
- Right Attack = Scoring × 0.058 + Passing × 0.048
- Central Attack = Scoring × 0.178 + Passing × 0.066

**Normal FW-R:**
- Midfield = Playmaking × 0.041
- Right Attack = Scoring × 0.058 + Passing × 0.048 + Winger × 0.032
- Left Attack = Scoring × 0.058 + Passing × 0.048
- Central Attack = Scoring × 0.178 + Passing × 0.066

### Forvet emirleri

**Towards Wing:**
- Midfield = Playmaking × 0.024
- Own Side Attack = Scoring × 0.093 + Passing × 0.101 + Winger × 0.044
- Far Side Attack = Scoring × 0.018 + Passing × 0.034
- Central Attack = Passing × 0.102 + Scoring × 0.044

**Defensive:**
- Midfield = Playmaking × 0.058
- Own Side Attack = Scoring × 0.030 + Passing × 0.033 + Winger × 0.059
- Far Side Attack = Scoring × 0.030 + Passing × 0.033
- Central Attack = Scoring × 0.102 + Passing × 0.108

### Uygulama kuralı

14 slotun tamamı bağımsız rol/yan/emir hesabından geçirilir:

`DEFENCE : WB-L / DEF-CL / DEF-C / DEF-CR / WB-R
`MIDFIELD: W-L / IM-L / IM-C / IM-R / W-R
`ATTACK  : FW-L / FW-C / FW-R`

Her slot için gerçek saha rolü, tarafı ve oyuncu emri ayrı belirlenir; skill katkıları ilgili sektörlere dağıtılır; overcrowding ile Form/Experience/Stamina ve diğer V5 context katmanları daha sonra uygulanır.

Bu bölüm, V5 katsayılarının tek bir genel DEF/IM/FW fonksiyonunda birleştirilmemesini sağlayan referans tasarım sözleşmesidir.

> Not: Pozisyon/katsayı matrisi Hattrick Wiki'deki açık kaynak topluluk araştırma tabloları temel alınarak dokümante edilmiştir; Hattrick'ın kapalı server-side kodunun resmi kaynak kodu değildir. Kontrollü gerçek Hattrick fixture'ları son kalibrasyonda ground truth olarak kullanılacaktır.

## Registry ve karşılaştırma

`RatingEngineRegistry` dört motoru deterministik biçimde sunar. V5 registry'de zorunludur.

`RatingEngineComparisonService` aynı XI ve aynı `RatingContext` ile dört motoru yan yana çalıştırır ve V5 baseline'a göre 7 sektör farklarını üretir.

Karşılaştırma ekranında da her motorun `RegionalRatingSnapshot` display alanları doğrudan gösterilir; ikinci bir converter uygulanmaz.

## Web selector

Production endpoint'leri:

- `GET /api/v5/rating-engines`
- `GET /api/v5/rating-engine/selection`
- `POST /api/v5/rating-engine/selection`
- `GET /api/v5/rating-engine/selected`
- `GET /api/v5/rating-engines/compare`

Web UI'da `rating-engines.js` selector ve comparison panelini sağlar. Analiz sonrası oyuncu havuzu, XI, rating context ve canonical V5 rating session'dan tekrar kullanılır.

Motor seçimi analizi başlatmadan önce yapılabilir; seçilen motor analiz pipeline'ında kullanılır. Analiz tamamlandıktan sonra aynı final XI dört motorla ayrıca karşılaştırılabilir.

## CI / regression sırası

`.github/workflows/cal001-regression.yml`:

1. rating engine contract
2. CAL-001 current V5 motor
3. CAL-001 model variants
4. HO Stage-2
5. HO real CHPP fixture
6. HattrickDash Stage-3
7. Foxtrick Stage-4
8. full real-fixture rating-engine validation

`RatingEngineValidationRegression` artık dört motorun production display alanlarının raw sektörleri tekrar sıkıştırmadığını ve `ConfidenceRatingAdjuster` sonrası da display==raw sözleşmesini kontrol eder.

`.github/workflows/v5-build.yml` ayrıca rating selector JavaScript syntax checkini production Docker buildinden önce çalıştırır.

## Gerçek fixture acceptance

Canonical V5 raw sectors:

`8.814876331125825 / 15.131275059602647 / 8.755167139072846 / 5.3073582240775785 / 9.3881423692354 / 10.733139483443706 / 8.34554898438368`

Foxtrick real-fixture stats:

- HatStats: `317.36089615638735`
- LoddarStats: `23.78`
- PeasoStats: `33.04`
- VnukStats: `8.97`
- HTitaVal: `301.7`
- GardierStats: `335`

HO ve HattrickDash real-fixture sonuçları bağımsız motorların kendi hesaplarından gelir; hard-coded çapraz motor eşitliği varsayılmaz.

## V5 invariance

- V5 production coefficients değişmez.
- Existing M3→M11 pipeline değişmez.
- Selector yalnızca ayrı engine endpointlerini kullanır.
- Default engine V5'tir.
- V5 adapter parity regression korunur.
- Stage-2 nonlinear converter korunur; production final rating zincirine sokulmaz.

## Acceptance kriteri

Bir motorun production saha rating'i geçerli sayılması için:

`Engine.Calculate() → RegionalRatingSnapshot → (gerekirse confidence adjustment) → UI`

zincirinde başka bir engine'in veya Stage-2 converter'ın display dönüşümü uygulanmamalıdır.

Aynı fixture için V5, HO, HattrickDash ve Foxtrick ayrı ayrı finite sonuç vermeli; her sonucun display alanları kendi raw sektörleriyle tutarlı olmalı; confidence adjustment neutral seviyede raw değerleri değiştirmeden display'i tekrar sıkıştırmamalıdır.

## Kapanış

Bu fix, motor seçiminin gerçekten seçilen motorla analiz yapması ile rating değerlerinin UI'a doğru taşınmasını aynı acceptance altında kilitler. V5 default davranışı korunur; diğer üç motorun sonuçları V5 converter'ından geçirilmez.

**Kod + regression + production build/deploy doğrulaması tamamlanmadan bu madde TAMAMLANDI sayılmayacak.**
