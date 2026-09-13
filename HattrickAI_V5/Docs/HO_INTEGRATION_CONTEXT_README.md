# HO Engine Integration — Match Context Handoff

**Tarih:** 13.09.2026  
**Branch:** `v5`

## Amaç

Hattrick Organizer (HO) motorunun ana HattrickAI analiz pipeline'ında seçildiğinde, yalnızca oyuncu + diziliş verisiyle değil, M7'de üretilen gerçek maç bağlamıyla çalışmasını sağlamak.

Hedef akış:

`CHPP → oyuncular → M3 → M4 → M5 → M6 → M7 → seçilen rating engine → M7.2 → M8 → M9 → M10 → M11`

HO seçildiğinde M7 rating çağrısında HO adapter'a özel match context de taşınır.

## Önceki problem

HO adapter içinde bazı match-context alanları sabitlenmişti:

- `TacticLevel = 1`
- `CoachModifier = 0`
- `TeamSpirit = 0`
- `Confidence = 0`
- `Weather = Normal`

Bunun sonucu, kullanıcı seçimlerinden M7'ye ulaşan bazı değerler HO motoruna geçmeden kayboluyordu.

Ayrıca non-V5 motor sonuçlarının V5 nonlinear display converter'dan geçirilmesi bağımsız motorların native ölçeğini bozabiliyordu.

## Yapılan değişiklik

### 1. Kanonik HO context taşıyıcısı

`RatingEngineContracts.cs` içine `HOEngineContext` eklendi.

Taşınan alanlar:

| Alan | Kaynak | HO'ya aktarım |
|---|---|---|
| `TeamSpirit` | M7 `MatchState` | Aktif |
| `Confidence` | M7 `MatchState` | Aktif |
| `CoachStyle` | M7 `MatchState` | Context içinde taşınır |
| `TacticLevel` | M7/HO context | Şimdilik 1; gerçek kaynak yoksa uydurulmaz |
| `CoachModifier` | M7/HO context | Şimdilik 0; gerçek kaynak yoksa uydurulmaz |
| `Weather` | M7/HO context | Şimdilik Normal; gerçek kaynak yoksa uydurulmaz |

`RatingEngineRequest.HOContext` opsiyoneldir. Bu nedenle V5, HattrickDash ve Foxtrick mevcut contract davranışlarını korur.

### 2. M7 → HO geçişi

`RegionalRatingScenarioEngine.CalculateLineup()` artık seçilen motor için `HOEngineContext` oluşturur ve `RatingEngineRequest` içine koyar.

HO seçili değilse V5 davranışı değişmez.

### 3. HO adapter

`HOEngineAdapter` artık `request.HOContext` üzerinden legacy `TeamMatchContext` üretir.

Aktarılan değerler:

- takım ruhu
- confidence
- taktik seviyesi
- coach modifier
- oyuncu bireysel davranışları
- maç dakikası
- ev/deplasman
- takım tutumu
- takım taktiği

Gerçek veri bulunmayan alanlar varsayılan değerlerde tutulur; veri uydurulmaz.

### 4. Native display ölçeği

Non-V5 motorlar V5'in nonlinear display conversion katmanından tekrar geçirilmez.

- V5 → mevcut V5 display path
- HO → HO native result
- HattrickDash → kendi result'ı
- Foxtrick → kendi result'ı

## Formation hatası

Daha önce canlı analizde:

`No lineup slot available for HO role CentralDefender.`

hatası oluşuyordu.

Kök neden legacy HO role table ile canonical V5 slot kodlarının özellikle 2-5-3 / 5-5-0 gibi formasyonlarda birebir uyuşmamasıydı.

HO adapter artık aynı taktik hat içindeki eşdeğer slotlara kontrollü fallback uyguluyor.

Regression testi M4'ün production legal formation listesindeki tüm formasyonları HO üzerinden geçiriyor.

## Regression kapsamı

HO regression şu kontrolleri yapar:

1. HO engine contract adı/tipi.
2. Deterministik aynı input → aynı output.
3. HatStats ve LoddarStats üretimi.
4. Production legal formasyonlarının tamamının HO'dan geçmesi.
5. Playmaking artışının midfield'i artırması.
6. Scoring artışının central attack'ı artırması.
7. `TeamSpirit` context'inin HO sonucunu değiştirmesi.
8. `Confidence` context'inin HO sonucunu değiştirmesi.

## Önemli sınır

Şu anki M7 `MatchState` içinde gerçek `TacticLevel`, gerçek `CoachModifier` ve gerçek hava durumu kaynağı bulunmadığı için bunlar tahmin edilerek doldurulmuyor.

Bunları ileride CHPP / kanonik match context'ten gerçekten elde edebilirsek ayrıca bağlayacağız.

## Test edilen commit

Context handoff zincirinin son değişiklikleri:

- `9af3019333` — HO context contract
- `d4487a19b6` — M7 → HO context handoff
- `3e0002a696` — HO adapter context forwarding
- `78de584028` — HO context regression
- `6e3b8b4d7e` — non-V5 native display isolation

## Acceptance

13.09.2026 tarihinde CAL-001 regression pipeline'ın HO ile ilgili mevcut kontrolleri başarılı geçti:

- Contract regression ✅
- CAL-001 ✅
- Model variant matrix ✅
- HO Stage-2 regression ✅
- HO real CHPP fixture regression ✅
- HattrickDash regression ✅
- Foxtrick regression ✅
- Real Hattrick 3-4-3 all-engine regression ✅
- Real fixture validation ✅

Context handoff değişikliklerinden sonra yeni CAL-001 run'ı tekrar çalıştırılmalıdır. Deployment başarılı olmadan canlı site testi yapılmış kabul edilmez.

## Canlı test planı

1. Siteyi aç.
2. CHPP'nin bağlı olduğunu doğrula.
3. Analiz başlamadan **HO** motorunu seç.
4. Analizi çalıştır.
5. Daha önce görülen `%27` / `CentralDefender` hatasının oluşmadığını kontrol et.
6. M7'ye kadar ilerlediğini kontrol et.
7. Analiz tamamlanırsa HO sonuçlarının makul rating ölçeğinde olduğunu kontrol et.
8. Aynı analizi V5 ile çalıştırıp V5'in eski davranışının bozulmadığını kontrol et.
9. Sonuç kutusunda HO / V5 / Dash / Foxtrick karşılaştırmasının mevcut final XI üzerinden üretildiğini kontrol et.

## Sonraki adım

Canlı HO analizi artık hata vermeden tamamlanırsa bir sonraki inceleme M7 sonrası HO çıktısının M7.2 → M8 → M9 → M10 → M11 zincirinde hangi alanlara dönüştürüldüğünü kontrol etmek olacaktır.

Özellikle kontrol edilecekler:

- HO regional rating'in M7.2'ye doğru taşınması
- HO rating'in M8 chance allocation'da doğru kullanılması
- HO rating'in M9 W/D/L ve xG hesabına doğru ulaşması
- M10/M11 final seçiminde HO sonucunun kaybolmaması
- final XI için diğer motor karşılaştırmasının aynı XI üzerinde yapılması

Bu doküman, gerçek olmayan engine katsayılarını veya Hattrick'in kapalı server-side formüllerini resmi gerçek olarak göstermemek için özellikle veri kaynağı ile varsayılan değerleri ayırır.
