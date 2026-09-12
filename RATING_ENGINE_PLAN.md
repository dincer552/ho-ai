# HattrickAI — Multi Rating Engine Plan / Current Status

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
- [x] Contract regression artık registry bütünlüğünü ve V5 adapter parity'sini kontrol ediyor.

### Aşama 2 — HO Engine — TAMAMLANDI

- [x] Legacy HO rating kaynakları izole adapter katmanında kullanılıyor.
- [x] Canonical V5 `Lineup` + `Player` verisi HO modeline çevriliyor.
- [x] 7 sektör, HatStats ve LoddarStats ortak sonuca çevriliyor.
- [x] Home/Away, PIC/MOTS, tactic ve match-minute context aktarımı regression ile kilitli.
- [x] Deterministik Stage-2 regression.
- [x] Gerçek CHPP fixture regression.

Gerçek fixture beklenen HO sektörleri:

`8.9278407632371284 / 14.241616934311249 / 8.8845667124997263 / 5.437522215250981 / 8.1350636338080307 / 9.5241231738830496 / 7.647351119659394`

### Aşama 3 — HattrickDash Engine — KOD TAMAMLANDI / CI KAPANIŞI

- [x] Dash `lineup_service` yerel rating mantığı izole edildi.
- [x] Pozisyon tahmini: `primary × 0.70 + form × 0.20 + stamina × 0.10`.
- [x] Midfield / defence / attack aggregate.
- [x] HatStats / LoddarStats.
- [x] `IRatingEngine` adapter.
- [x] Deterministik Stage-3 regression.
- [x] Gerçek CHPP fixture beklenen değerleri validation katmanında kilitli.
- [x] CI Stage-3 komutu workflow'a bağlı.

**Kaynak sınırı:** Dash açık kaynak projesinin local lineup/analytics mantığı uygulanır. Hattrick'ın kapalı server-side rating preview algoritması yeniden oluşturuluyor iddiası yoktur.

### Aşama 4 — Foxtrick Engine — KOD TAMAMLANDI / CI KAPANIŞI

- [x] Foxtrick `ratings.js` formülleri ayrıştırıldı.
- [x] HatStats.
- [x] LoddarStats.
- [x] PeasoStats.
- [x] VnukStats.
- [x] HTitaVal.
- [x] GardierStats.
- [x] `IRatingEngine` adapter.
- [x] Deterministik synthetic regression.
- [x] Gerçek CHPP fixture regression.
- [x] Foxtrick sektörlerinde canonical V5 **raw** regional source kullanımı açıkça korunuyor.
- [x] Gerçek fixture Foxtrick istatistik expected-value'ları kilitli.

Foxtrick gerçek fixture expected stats:

- HatStats `317.36089615638735`
- LoddarStats `23.78`
- PeasoStats `33.04`
- VnukStats `8.97`
- HTitaVal `301.7`
- GardierStats `335`

**Kaynak sınırı:** Foxtrick'ın açık kaynak kodu sektörleri Hattrick match verisinden okuyup istatistikleri türetiyor; kapalı Hattrick server-side sektör-rating üretimi Foxtrick tarafından yeniden hesaplanmıyor.

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

UI'da `rating-engines.js` ile selector ve comparison panel bulunur.

Selection aynı analizdeki oyuncu/kadro/context'i session'dan tekrar kullanır; V5 default olarak kalır.

### Aşama 7 — Motor karşılaştırması — TAMAMLANDI

Aynı XI ve aynı `RatingContext` üzerinde:

- V5
- HO
- HattrickDash
- Foxtrick

yan yana gösterilir. Karşılaştırma V5'i değiştirmez; V5 baseline'a göre 7 sektör farkları ayrıca tutulur.

### Aşama 8 — Validation — TAMAMLANDI / CI KAPANIŞI

Gerçek CHPP fixture üzerinden bütün registry motorları çalıştırılır.

Validation şunları kilitler:

- dört motorun registry'de bulunması,
- V5 raw sector expected-values,
- HO sector expected-values,
- Dash sector expected-values,
- Foxtrick'in V5 canonical raw sektörleri kullanması,
- Foxtrick HatStats/LoddarStats expected-values,
- comparison baseline/selected/result-row bütünlüğü.

Validation komutu:

`dotnet run --project HattrickAI_V5.OfflineTests/HattrickAI_V5.OfflineTests.csproj -- rating-validation`

---

## Regression / CI kapanış sırası

`.github/workflows/cal001-regression.yml` sırası:

1. Rating engine contract
2. CAL-001 current motor
3. CAL-001 model variants
4. HO Stage-2
5. HO real CHPP fixture
6. HattrickDash Stage-3
7. Foxtrick Stage-4
8. Rating engine real-fixture validation

`.github/workflows/v5-build.yml` içinde ayrıca `rating-engines.js` syntax check, Docker build, GHCR push, Azure deploy ve health/homepage smoke korunur.

---

## V5 invariance

- [x] V5 production rating coefficients değiştirilmedi.
- [x] Existing `AnalysisService` pipeline aynı M3→M11 zincirini çalıştırıyor.
- [x] Rating engine selector yalnızca ayrı engine calculation endpointlerini kullanıyor.
- [x] V5 default seçim.
- [x] V5 adapter parity regression mevcut.

---

## Dokümantasyon sınırı

- Source-derived mekanik ile V5 heuristiği birbirine karıştırılmaz.
- Dash/Foxtrick için kapalı Hattrick server-side formüller uydurulmaz.
- Gerçek Hattrick ground truth ile araştırma/engine sonuçları ayrı tutulur.
- Raw sector ve display rating aynı şey değildir; validation buna göre yapılır.

---

## Kapanış kriteri

Rating-engine çalışması aşağıdaki dört kapı yeşil olmadan **TAMAMLANDI** sayılmaz:

1. regression,
2. production build/deploy,
3. documentation,
4. V5 invariance.

Son CI sonucu görülmeden yeşil kabul edilmez. Failure çıkarsa aynı stage düzeltilir ve tekrar koşturulur.
