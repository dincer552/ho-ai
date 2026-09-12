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

**TAMAMLANDI**

- [x] Mevcut legacy HO `LineupRatingEngine` / `RatingContributionTable` / `PlayerRatingCalculator` / `TeamRatings` kaynakları izole referans olarak V5 projesine bağlandı.
- [x] `HOEngineAdapter` ile `IRatingEngine` sözleşmesine bağlandı.
- [x] HO pozisyon/slot davranışları canonical V5 `Lineup` + `Player` verisinden legacy HO modeline dönüştürülüyor.
- [x] Canonical 3-5-2 slotları ile legacy HO 3-5-2 rolleri arasındaki IM/FW eşleştirme farkları adapter seviyesinde kapatıldı.
- [x] HO sektör sonuçları ortak `RegionalRatingSnapshot` formatına dönüştürülüyor.
- [x] HO HatStats hesaplaması eklendi.
- [x] HO LoddarStats hesaplaması, upstream Hattrick Organizer formülü ile adapter katmanına eklendi.
- [x] Canonical context içindeki `Home/Away`, `PIC/MOTS`, `tactic` ve `matchMinute` bilgileri legacy HO context'e aktarılıyor.
- [x] Legacy HO'nun common contract'ta karşılığı olmayan `CoachModifier`, `TeamSpirit`, `Confidence`, `Weather`, `OpponentRatings` alanları deterministik olarak defaultlanıyor; bunlar V5 ortak modeline zorunlu alan olarak eklenmedi.
- [x] Deterministik `HOEngineStage2Regression` eklendi.
- [x] Gerçek CHPP fixture regression'ı eklendi: `HattrickAI_V5.OfflineTests/Fixtures/HO_Real_CHPP_Fixture_2026-09-01.json` + `HORealFixtureRegression.cs`.
- [x] Gerçek fixture için 7 sektör + HatStats + LoddarStats expected-value değerleri sabitlendi.
- [x] Fixture regression içinde minute/stamina, home ve PIC/MOTS context davranışları da kontrol ediliyor.
- [x] CI üzerinde `HOEngineStage2Regression` ve gerçek CHPP HO fixture regression adımları tanımlandı.
- [x] `CAL-001`, model-variant matrix ve Stage-2 HO regression aynı CI koşusunda başarıyla doğrulandı; gerçek fixture adımı mapping düzeltmesinden sonra yeniden koşulacak.

### Aşama 2 kapanış kriteri

HO motoru artık:

- bağımsız
- canonical V5 verisinden beslenen
- legacy HO hesap mantığını kullanan
- 7 sektör + HatStats + LoddarStats üreten
- gerçek CHPP fixture ile deterministic regression'a sahip
- mevcut V5 rating pipeline'ını değiştirmeyen

durumdadır.

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