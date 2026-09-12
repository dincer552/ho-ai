# HattrickAI V5 — Rating Engine Katmanı

## Amaç

Mevcut V5 rating davranışını değiştirmeden aynı canonical `Lineup + Player + RatingContext` girdisini bağımsız rating motorlarında çalıştırmak:

`V5 | HO | HattrickDash | Foxtrick`

**Varsayılan motor: V5.** Yeni motorlar mevcut M3→M11 production pipeline'ına katsayı veya akış değişikliği olarak eklenmez.

## Motorlar

### V5
Mevcut V5 rating katmanının adapter'ıdır. `Stage2RegionalRatingEngineFixed` üzerinden canonical V5 regional rating üretir. Contract regression, adapter parity'sini doğrudan mevcut Fixed motoruna karşı kontrol eder.

### HO
Legacy HO rating hesapları bağımsız adapter altında çalışır. 7 bölgesel sektör ile HatStats/LoddarStats ortak `RatingEngineResult` modeline taşınır. Gerçek 2026-09-01 CHPP fixture regression ile sektör ve context davranışları kilitlidir.

### HattrickDash
Açık kaynak Dash local lineup/analytics mantığı uygulanır. Pozisyon tahmini `primary × 0.70 + form × 0.20 + stamina × 0.10` ile başlar; midfield/defence/attack aggregate'leri ve Dash HatStats/LoddarStats hesapları ayrıdır.

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

`RatingEngineResult`:

- engine identity
- 7 regional sector
- optional HatStats
- optional LoddarStats

`RegionalRatingSnapshot` raw ve display ratingleri ayrı tutar. Validation'da V5/Foxtrick raw değerleri ile HO/Dash display değerleri birbirine karıştırılmaz.

## Registry

`RatingEngineRegistry` dört motoru deterministik biçimde sunar. V5 registry'de zorunludur.

`RatingEngineComparisonService` aynı XI ve aynı `RatingContext` ile dört motoru yan yana çalıştırır ve V5 baseline'a göre 7 sektör farklarını üretir.

## Web selector

Production endpoint'leri:

- `GET /api/v5/rating-engines`
- `GET /api/v5/rating-engine/selection`
- `POST /api/v5/rating-engine/selection`
- `GET /api/v5/rating-engine/selected`
- `GET /api/v5/rating-engines/compare`

Web UI'da `rating-engines.js` selector ve comparison panelini sağlar. Analiz sonrası aynı oyuncu havuzu, XI ve rating context session'dan tekrar kullanılır.

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

HO real-fixture sectors ve Dash real-fixture outputları bağımsız motorların kendi hesaplarından gelir; hard-coded çapraz motor eşitliği varsayılmaz.

## V5 invariance

- V5 production coefficients değişmez.
- Existing M3→M11 pipeline değişmez.
- Selector yalnızca ayrı engine endpoint'lerini kullanır.
- Default engine V5'tir.
- V5 adapter parity regression vardır.

## Kapanış

Rating-engine çalışması ancak regression, production build/deploy, documentation ve V5 invariance kapıları birlikte yeşil olduğunda tamamlanmış kabul edilir.
