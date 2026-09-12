# HattrickAI — Multi Rating Engine Plan

## Amaç

Mevcut **V5 Rating Engine kesinlikle korunacak**.

Yeni rating motorları V5'i değiştirmeden ayrı modüller olarak eklenecek.

Sitede üst bölümde motor seçimi olacak:

**V5 | HO | HattrickDash | Foxtrick**

Seçilen motor aynı kadro/veri üzerinde kendi rating hesabını gösterecek.

---

## Aşama 1 — Motor mimarisi

Ortak `RatingEngine` arayüzü oluşturulacak.

```text
RatingEngine
 ├── V5Engine          ← mevcut motor, DOKUNULMAYACAK
 ├── HOEngine
 ├── HattrickDashEngine
 └── FoxtrickEngine
```

Her motor bağımsız çalışacak.

---

## Aşama 2 — HO Engine

Açık kaynak **Hattrick Organizer** kodu incelenecek ve rating hesaplama mantığı ayrı `HOEngine` içine aktarılacak.

Öncelik:

- RatingPredictionModel
- pozisyon katkıları
- sektör ratingleri
- midfield / defense / attack
- HatStats
- LoddarStats
- lineup rating

**V5 koduna müdahale edilmeyecek.**

### Aşama 2 mevcut durum

- [x] Mevcut legacy HO `LineupRatingEngine` / `RatingContributionTable` / `PlayerRatingCalculator` / `TeamRatings` kaynakları izole referans olarak V5 projesine bağlandı.
- [x] `HOEngineAdapter` ile `IRatingEngine` sözleşmesine bağlandı.
- [x] HO pozisyon/slot davranışları canonical V5 `Lineup` + `Player` verisinden legacy HO modeline dönüştürülüyor.
- [x] HO sektör sonuçları ortak `RegionalRatingSnapshot` formatına dönüştürülüyor.
- [x] HO HatStats hesaplaması eklendi.
- [x] HO LoddarStats hesaplaması, upstream Hattrick Organizer formülü ile adapter katmanına eklendi.
- [x] Deterministik `HOEngineStage2Regression` eklendi.
- [ ] Upstream `RatingPredictionModel` ile kalan tüm context/average-minute davranışları birebir parity seviyesine getirilecek.
- [ ] HO engine için gerçek Hattrick fixture'larından sabit expected-value regression seti oluşturulacak.
- [ ] CI üzerinde `HOEngineStage2Regression` çalıştırılacak ve build sonucu doğrulanacak.

---

## Aşama 3 — HattrickDash Engine

HattrickDash açık kaynak kodu incelenecek.

Rating hesaplama mantıkları ayrı `DashEngine` içine uygulanacak.

HO ve V5 ile aynı test verileri üzerinden karşılaştırılacak.

---

## Aşama 4 — Foxtrick Engine

Foxtrick'in açık kaynak rating hesaplamaları incelenecek.

Özellikle:

- HatStats
- LoddarStats
- PeasoStats
- VnukStats
- GardierStats
- HTitaVal

mümkün olduğu ölçüde ayrı modüller halinde uygulanacak.

---

## Aşama 5 — Ortak sonuç modeli

Bütün motorlar aynı sonuç formatını döndürecek:

```text
Team Rating
Midfield
Left Defense
Central Defense
Right Defense
Left Attack
Central Attack
Right Attack
HatStats
LoddarStats
```

Böylece motorlar doğrudan karşılaştırılabilecek.

---

## Aşama 6 — Web arayüzü

Sayfanın üst kısmına motor seçici eklenecek:

```text
Rating Engine: [ V5 ▼ ]
```

Seçenekler:

```text
V5
HO
HattrickDash
Foxtrick
```

V5 varsayılan motor olarak kalacak.

Motor değiştirildiğinde aynı kadro seçilen motorla yeniden hesaplanacak.

---

## Aşama 7 — Motor karşılaştırması

İlerleyen aşamada aynı kadro için motor sonuçları yan yana gösterilecek.

Bu özellik V5'in hesaplamasını değiştirmeyecek.

---

## Aşama 8 — Validation

Aynı gerçek Hattrick maç/kadro verileri bütün motorlara verilecek.

```text
Gerçek Hattrick Rating
        ↓
V5 / HO / Dash / Foxtrick
        ↓
Fark analizi
```

Amaç her motorun gerçek Hattrick sonucuna ne kadar yakın olduğunu ölçmek.

---

## Geliştirme sırası

**V5'i kilitle → HO → HattrickDash → Foxtrick → ortak sonuç modeli → web motor selector → karşılaştırma → validation**

## Ana kural

**V5 mevcut haliyle korunacak.**

Yeni motor eklenmesi V5'in davranışını veya mevcut sonuçlarını değiştirmeyecek.

Her motor:

- bağımsız
- test edilebilir
- seçilebilir
- gerektiğinde devre dışı bırakılabilir

olacak.

---

## Durum

### Aşama 1 — Motor mimarisi

**DEVAM EDİYOR**

- [x] `RATING_ENGINE_PLAN.md` repository'ye eklendi.
- [x] Ortak `RatingEngineKind` tanımlandı.
- [x] Ortak `RatingEngineRequest` tanımlandı.
- [x] Ortak `RatingEngineResult` tanımlandı.
- [x] `IRatingEngine` sözleşmesi eklendi.
- [x] V5 mevcut pipeline'a bağlanmadı; mevcut hesap akışı korunuyor.
- [x] Contract regression testi eklendi: `HattrickAI_V5.OfflineTests/RatingEngineContractsRegression.cs`.
- [ ] Contract regression'ın CI/build üzerinde çalıştığı doğrulanacak.
- [ ] V5 adapter'ı mevcut rating üretimini birebir koruyacak şekilde bağlanacak.
- [ ] Engine registry/factory oluşturulacak.
- [ ] Production pipeline'a selector bağlanmayacak; selector sonraki aşamada eklenecek.

Aşama 1 yalnızca mimari sınırı kurar. **Bu aşamada V5 rating katsayıları ve mevcut hesap akışı değiştirilmez.**