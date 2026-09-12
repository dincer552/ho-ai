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
- [x] CI üzerinde `HOEngineStage2Regression` ve gerçek CHPP fixture regression adımları tanımlandı.
- [x] `CAL-001`, model-variant matrix, Stage-2 HO regression ve gerçek CHPP fixture regression aynı CI koşusunda başarıyla doğrulandı.

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

### Aşama 3 mevcut durum

**DEVAM EDİYOR**

- [x] HattrickDash açık kaynak `lineup_service.py` rating mantığı incelendi.
- [x] Dash'ın pozisyon rating tahmini izole edildi: `primary skill × 0.70 + form × 0.20 + stamina × 0.10`.
- [x] Dash'ın midfield / defence / attack aggregate hesaplaması uygulandı.
- [x] Dash'ın kullandığı HatStats ve LoddarStats aggregate formülleri uygulandı.
- [x] `HattrickDashEngine` → `IRatingEngine` sözleşmesine bağlandı.
- [x] Canonical V5 slot kodları Dash rol hesaplamasına bağlandı.
- [x] Deterministik `HattrickDashEngineStage3Regression` eklendi.
- [x] Regression runner'a `dash-stage3` komutu eklendi.
- [x] CI pipeline'a Stage-3 Dash regression adımı eklendi.
- [ ] CI Stage-3 regression'ın yeşil sonuç vermesi doğrulanacak.
- [ ] Aynı gerçek CHPP fixture üzerinde HO / Dash / V5 sonuç farkları kilitlenecek.

**Not:** HattrickDash'ın kendi README'sinde lineup rating preview'ın Hattrick tarafından hesaplanan ayrı bir server-side preview olduğu belirtiliyor. Buradaki Stage-3 motor, Dash'ın açık kaynak `lineup_service` içindeki yerel pozisyon-rating / aggregate mantığını izole eder; Hattrick'ın kapalı server-side algoritmasını taklit ettiği iddia edilmez.

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

### Aşama 4 mevcut durum

**KOD TAMAMLANDI — CI SON KAPANIŞ GATE'İ**

- [x] Foxtrick açık kaynak `content/matches/ratings.js` incelendi.
- [x] HatStats formülü izole edildi.
- [x] LoddarStats formülü izole edildi.
- [x] PeasoStats formülü izole edildi.
- [x] VnukStats formülü izole edildi.
- [x] HTitaVal formülü izole edildi.
- [x] GardierStats formülü izole edildi.
- [x] `FoxtrickEngine` → `IRatingEngine` sözleşmesine bağlandı.
- [x] Deterministik `FoxtrickEngineStage4Regression` eklendi.
- [x] Foxtrick Stage-4 regression gerçek CHPP fixture'a genişletildi.
- [x] Gerçek CHPP fixture üzerinde Foxtrick/V5 sektörleri kilitlendi.
- [x] Aynı fixture üzerinde HO / HattrickDash / Foxtrick sonuçları ve farkları expected-value olarak kilitlendi.
- [x] Regression runner'a `foxtrick-stage4` komutu eklendi.
- [x] CI pipeline'a Stage-4 Foxtrick regression adımı eklendi.
- [ ] Son push sonrası CI Stage-4 regression'ın yeşil sonucu doğrulanacak.

**Not:** Foxtrick'in `ratings.js` kodu sektör ratinglerini Hattrick match sayfasından alıp istatistikleri türetiyor; Foxtrick'in kendisi kapalı Hattrick server-side sektör-rating üretimini yeniden hesaplamıyor. Bu nedenle Stage-4 motorunda yedi sektör için mevcut canonical regional calculator yalnızca ortak veri kaynağı olarak kullanılıyor; HatStats/LoddarStats/PeasoStats/VnukStats/HTitaVal/GardierStats hesaplarının tamamı Foxtrick kodundan ayrı olarak uygulanıyor.

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

---

## İş Bitiminde Temizlik ve Kapanış

Motor geliştirme aşamaları tamamlandığında proje yarım kalmış testler, geçici dosyalar veya neyin neden yapıldığının belirsiz olduğu bir durumda bırakılmayacak.

Kapanış sırası:

1. **Tüm regression testleri yeşil olacak.**
   - CAL-001
   - model-variant matrix
   - HO Stage-2
   - gerçek CHPP fixture
   - HattrickDash Stage-3
   - Foxtrick Stage-4
   - ortak contract testleri

2. **Deploy workflow'u ayrıca yeşil olacak.**
   - Uygulama build'i başarılı olacak.
   - Docker image başarıyla oluşturulacak ve GHCR'a gönderilecek.
   - Azure deployment ve health check başarılı olacak.

3. **Açık kalan acceptance maddeleri kapatılacak.**
   - Plan içinde `[ ]` kalan maddeler ya gerçekten tamamlanacak ya da neden ertelendiği açıkça yazılacak.
   - Tamamlanmış gibi işaretleme yapılmayacak.

4. **Geçici/debug dosyaları temizlenecek.**
   - Sadece regression, fixture, kaynak kodu ve dokümantasyon için gerekli dosyalar repository'de bırakılacak.
   - Geçici çıktılar, local test artıkları ve kullanılmayan deneme dosyaları repository'de tutulmayacak.

5. **Dokümantasyon son durumla eşitlenecek.**
   - `RATING_ENGINE_PLAN.md`, README ve ilgili teknik dokümanlar gerçek uygulama durumunu yansıtacak.
   - Eski/yanlış “DEVAM EDİYOR” veya “TAMAMLANDI” işaretleri kontrol edilecek.

6. **V5 davranışının değişmediği son kez doğrulanacak.**
   - Yeni motorlar V5 pipeline'ını değiştirmeyecek.
   - V5 varsayılan motor olarak kalacak.

7. **Son repository kontrolü yapılacak.**
   - Build temiz olacak.
   - Regression temiz olacak.
   - Deploy temiz olacak.
   - Kullanılmayan dosya bırakılmayacak.
   - Plan ile gerçek kod arasında açık bir uyumsuzluk kalmayacak.

### Kapanış kriteri

**Proje ancak testler + build + deploy + dokümantasyon + repository temizliği tamamlandığında ilgili rating-engine çalışması için “TAMAMLANDI” kabul edilir.**

Amaç: geliştirme bittikten sonra repository'nin geçici dosyalar, yarım acceptance maddeleri ve belirsiz durumlarla dolu bir “çöplük” haline gelmesini önlemek.

---

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
