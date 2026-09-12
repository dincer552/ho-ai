# HattrickAI — Multi Rating Engine Plan / FINAL STATUS

## Ana kural

**Mevcut V5 rating davranışı korunur.** Yeni motorlar V5 pipeline'ına katsayı veya akış değişikliği olarak eklenmez; ayrı `IRatingEngine` implementasyonları olarak çalışır.

Production motor seçimi:

**V5 | HO | HattrickDash | Foxtrick**

V5 varsayılandır.

---

## Aşama sırası

`V5'i kilitle → HO → HattrickDash → Foxtrick → ortak sonuç modeli → web selector → karşılaştırma → validation`

### Aşama 1 — Motor mimarisi — TAMAMLANDI

- [x] `RatingEngineKind` / `RatingEngineRequest` / `RatingEngineResult` ortak sözleşmesi.
- [x] `IRatingEngine` ortak arayüzü.
- [x] `V5RatingEngine` mevcut `Stage2RegionalRatingEngineFixed` hesaplamasına adapter olarak bağlandı; mevcut V5 pipeline değiştirilmedi.
- [x] `RatingEngineRegistry` / factory oluşturuldu.
- [x] Registry dört motoru deterministik biçimde sunuyor: V5, HO, HattrickDash, Foxtrick.
- [x] Contract regression registry bütünlüğünü ve V5 adapter parity'sini kontrol ediyor.

### Aşama 2 — HO Engine — TAMAMLANDI

- [x] Legacy HO rating kaynakları izole adapter katmanında kullanılıyor.
- [x] Canonical V5 `Lineup` + `Player` verisi HO modeline çevriliyor.
- [x] 7 sektör, HatStats ve LoddarStats ortak sonuca çevriliyor.
- [x] Home/Away, PIC/MOTS, tactic ve match-minute context aktarımı regression ile kilitli.
- [x] Deterministik Stage-2 regression.
- [x] Gerçek CHPP fixture regression.

### Aşama 3 — HattrickDash Engine — TAMAMLANDI

- [x] Dash `lineup_service` yerel rating mantığı izole edildi.
- [x] Pozisyon tahmini: `primary × 0.70 + form × 0.20 + stamina × 0.10`.
- [x] Midfield / defence / attack aggregate.
- [x] HatStats / LoddarStats.
- [x] `IRatingEngine` adapter.
- [x] Deterministik Stage-3 regression.
- [x] Gerçek CHPP fixture validation.
- [x] CI green.

**Kaynak sınırı:** Dash açık kaynak projesinin local lineup/analytics mantığı uygulanır. Hattrick'ın kapalı server-side rating preview algoritmasının yeniden üretildiği iddia edilmez.

### Aşama 4 — Foxtrick Engine — TAMAMLANDI

- [x] Foxtrick `ratings.js` formülleri ayrıştırıldı.
- [x] HatStats / LoddarStats / PeasoStats / VnukStats / HTitaVal / GardierStats.
- [x] `IRatingEngine` adapter.
- [x] Deterministik synthetic regression.
- [x] Gerçek CHPP fixture regression.
- [x] Foxtrick sektörlerinde canonical V5 **raw** regional source kullanımı açıkça korunuyor.
- [x] Gerçek fixture Foxtrick istatistik expected-value'ları kilitli.
- [x] CI green.

Foxtrick gerçek fixture expected stats:

- HatStats `317.36089615638735`
- LoddarStats `23.78`
- PeasoStats `33.04`
- VnukStats `8.97`
- HTitaVal `301.7`
- GardierStats `335`

**Kaynak sınırı:** Foxtrick'ın açık kaynak kodu match sayfasındaki sektör ratinglerini kullanarak istatistikleri türetiyor; kapalı Hattrick server-side sektör üretimi yeniden hesaplanmıyor.

### Aşama 5 — Ortak sonuç modeli — TAMAMLANDI

Bütün motorlar `RatingEngineResult` üzerinden aynı canonical result modelini verir:

- 7 regional sectors
- optional HatStats
- optional LoddarStats
- engine identity

`RegionalRatingSnapshot` raw ve display ratingleri ayrı tutar.

### Aşama 6 — Web motor selector — TAMAMLANDI

Production web API:

- `GET /api/v5/rating-engines`
- `GET /api/v5/rating-engine/selection`
- `POST /api/v5/rating-engine/selection`
- `GET /api/v5/rating-engine/selected`
- `GET /api/v5/rating-engines/compare`

UI'da `rating-engines.js` selector ve comparison panel bulunur.

Selection aynı analizdeki oyuncu/kadro/context/canonical V5 rating'i session'dan tekrar kullanır; V5 default olarak kalır.

### Aşama 7 — Motor karşılaştırması — TAMAMLANDI

Aynı XI ve aynı `RatingContext` üzerinde:

- V5
- HO
- HattrickDash
- Foxtrick

yan yana gösterilir. Karşılaştırma V5'i değiştirmez; V5 baseline'a göre 7 sektör farkları ayrıca tutulur.

### Aşama 8 — Validation — TAMAMLANDI

Gerçek tam CHPP fixture üzerinden bütün registry motorları çalıştırılır.

Validation şunları kilitler:

- dört motorun registry'de bulunması,
- canonical V5 raw sector expected-values,
- HO ve Dash sonuçlarının finite olması,
- Foxtrick'in canonical V5 raw sektörleri kullanması,
- Foxtrick HatStats/LoddarStats expected-values,
- comparison baseline/selected/result-row bütünlüğü.

Validation komutu:

`dotnet run --project HattrickAI_V5.OfflineTests/HattrickAI_V5.OfflineTests.csproj -- rating-validation`

---

## Son yeşil acceptance

### CAL-001 Rating Regression

**Run #80 / `34714871533` — GREEN**

Commit: `43f4be6e7ec53dd0af011ca0897767b39069a4f6`

Green adımlar:

1. Rating engine contract
2. CAL-001 current motor
3. CAL-001 model variants
4. HO Stage-2
5. HO real CHPP fixture
6. HattrickDash Stage-3
7. Foxtrick Stage-4
8. Rating engine real-fixture validation

### Production Build / Deploy

**Run #1195 / `34714871466` — GREEN**

Commit: `43f4be6e7ec53dd0af011ca0897767b39069a4f6`

Green kapılar:

- JavaScript syntax regression
- CHPP trainer exclusion
- WRITE-03
- WRITE-04
- WRITE-05
- WRITE-06
- WRITE-07
- Docker build
- GHCR login/push
- Azure VM deployment
- deployment health/homepage smoke path

Build failure #1194'ün nedeni Docker build context'inde `YEDEK` klasörünün olmamasıydı. `HattrickAI_V5.csproj` legacy HO kaynaklarını `../YEDEK/...` ile compile ettiği için Dockerfile'a `COPY YEDEK YEDEK` eklendi ve #1195 tamamen yeşil oldu.

---

## V5 invariance

- [x] V5 production rating coefficients değiştirilmedi.
- [x] Existing `AnalysisService` M3→M11 pipeline aynı kaldı.
- [x] Rating engine selector ayrı engine endpointlerini kullanıyor.
- [x] V5 default seçim.
- [x] V5 adapter parity regression green.
- [x] Production deploy green.

---

## Dokümantasyon sınırı

- Source-derived mekanik ile V5 heuristiği birbirine karıştırılmaz.
- Dash/Foxtrick için kapalı Hattrick server-side formüller uydurulmaz.
- Gerçek Hattrick ground truth ile araştırma/engine sonuçları ayrı tutulur.
- Raw sector ve display rating aynı şey değildir; validation buna göre yapılır.

Detaylı kullanıcı/developer dokümanı:

`Docs/RATING_ENGINE_README.md`

---

## KAPANIŞ

**Rating-engine çalışması TAMAMLANDI.**

Regression + production build/deploy + documentation + V5 invariance dört kapanış kapısı birlikte yeşildir.

Yeni motor seçimi production'da kullanılabilir; varsayılan ve korunmuş davranış V5'tir.
