# HattrickAI V5 — Rating Engine Katmanı

## Durum

**TAMAMLANDI.**

Production motorları:

`V5 | HO | HattrickDash | Foxtrick`

**Varsayılan motor: V5.** Mevcut V5 davranışı ve M3→M11 production pipeline'ı korunur.

## Amaç

Aynı canonical `Lineup + Player + RatingContext` girdisini bağımsız rating motorlarında çalıştırmak ve motorları V5 davranışını değiştirmeden karşılaştırabilmek.

## Motorlar

### V5
Mevcut V5 rating katmanının adapter'ıdır. `Stage2RegionalRatingEngineFixed` üzerinden canonical V5 regional rating üretir. Contract regression, adapter parity'sini mevcut Fixed motoruna karşı kontrol eder.

### HO
Legacy HO rating hesapları bağımsız adapter altında çalışır. 7 bölgesel sektör ile HatStats/LoddarStats ortak `RatingEngineResult` modeline taşınır. Gerçek 2026-09-01 CHPP fixture regression ile sektör ve context davranışları kilitlidir.

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

`RegionalRatingSnapshot` raw ve display ratingleri ayrı tutar. Validation'da V5/Foxtrick raw değerleri ile HO/Dash display değerleri birbirine karıştırılmaz.

## Registry ve karşılaştırma

`RatingEngineRegistry` dört motoru deterministik biçimde sunar. V5 registry'de zorunludur.

`RatingEngineComparisonService` aynı XI ve aynı `RatingContext` ile dört motoru yan yana çalıştırır ve V5 baseline'a göre 7 sektör farklarını üretir.

## Web selector

Production endpoint'leri:

- `GET /api/v5/rating-engines`
- `GET /api/v5/rating-engine/selection`
- `POST /api/v5/rating-engine/selection`
- `GET /api/v5/rating-engine/selected`
- `GET /api/v5/rating-engines/compare`

Web UI'da `rating-engines.js` selector ve comparison panelini sağlar. Analiz sonrası oyuncu havuzu, XI, rating context ve canonical V5 rating session'dan tekrar kullanılır.

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

## Final acceptance

### CAL-001 Rating Regression

**Run #80 / `34714871533` — GREEN**

Commit: `43f4be6e7ec53dd0af011ca0897767b39069a4f6`

Contract, CAL-001, model variants, HO Stage-2, HO real fixture, Dash Stage-3, Foxtrick Stage-4 ve full rating-engine validation tamamı başarılı.

### Production Build / Deploy

**Run #1195 / `34714871466` — GREEN**

Commit: `43f4be6e7ec53dd0af011ca0897767b39069a4f6`

JavaScript syntax, CHPP regression seti, Docker build, GHCR push ve Azure VM deployment başarıyla tamamlandı.

Build #1194'teki Docker hatasının kökü, `.csproj` tarafından `../YEDEK/YEDEK/V1/webapp/HOEngine/*.cs` üzerinden kullanılan legacy HO kaynaklarının Docker build context'ine alınmamış olmasıydı. Dockerfile'a `COPY YEDEK YEDEK` eklendi ve #1195 tamamen yeşil oldu.

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
- V5 adapter parity regression green.
- Production build/deploy green.

## Kapanış

Regression + production build/deploy + documentation + V5 invariance dört kapanış kapısı yeşildir. Rating-engine çalışması **TAMAMLANDI**.
