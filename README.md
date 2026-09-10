# HattrickAI V5

# README TOP BLOCK — 09.09.2026

> **Taktik + M9 formasyon karar motoru: devam eden tam denetim**
>
> Amaç: Taktik ve formasyon seçiminde `TacticFitScore` / `TacticalScore` gibi uygunluk sinyallerinin M9 maç sonucunun önüne geçmesini engellemek. Aynı XI + aynı rakip için kanonik ana sonuç `ExpectedPoints = 3 × WinProbability + DrawProbability` olarak korunur; WinProbability, ExpectedGoalDifference/xG, fit ve tactical skorlar yalnızca tanımlı tie-break/diagnostic katmanlarıdır.
>
> Kaynakta olmayan gizli engine katsayıları resmî formül gibi kullanılmaz. Source-derived mekanik, V5 heuristiği ve gerçek M9 sonucu ayrı tutulur.

## 1. Kaynak / mekanik kapsamı

Ana referanslar:

- Hattrick Wiki — Tactics
- Hattrick Wiki — Match engine
- Hattrick Wiki — Regular chances
- Hattrick Wiki — Rules / Manual
- Hattrick Wiki — Pressing
- Hattrick Wiki — Counter-attacks
- Hattrick Wiki — Attack in the middle
- Hattrick Wiki — Attack on wings
- Hattrick Wiki — Play creatively
- Hattrick Wiki — Long shots
- 2026 Constantinou et al.

Kaynak çerçevesi: AiM ve AoW şans yönünü değiştirir ve ilgili savunma sektörünü zayıflatır; CA midfield maliyeti karşılığında rakibin başarısız normal saldırılarından fırsat üretir; Pressing toplam normal-chance hacmini bastırır ve stamina/DEF/EXP/Powerful önemlidir; Long Shots normal fırsatların bir bölümünü şuta çevirir ve shooter/GK kalitesi kritiktir; Creative özel olay fırsatlarını artırırken negatif olay ve savunma maliyeti taşır.

## 2. M9 formasyon karar sözleşmesi

Tüm legal formasyon finalistleri aynı rakibe karşı karşılaştırılır:

1. `ExpectedPoints = 3W + D` ana outcome metriğidir.
2. `WinProbability` ve `ExpectedGoalDifference` outcome kalite/tie-break sinyalidir.
3. `TacticFitScore`, `TacticalScore`, structural/stability sinyalleri outcome eşitliğinde veya açıklayıcı katmanda kalır.
4. Anti-lock mekanizması tüm legal formasyonları yarışta tutar.
5. M9 liderinden farklı bir final seçiminde açık ve testli override gerekir.

M10 regression formation ranking'in ExpectedPoints-first, eşit EP'de WinProbability tie-break ve deterministic rerun sözleşmesini kontrol eder.

## 3. M4 → M5 → M6 → M7 → M7.2 → M8 → M9 → M10 → M11

M4 legal universe, M5 XI quality, M6 beam/refinement, M7 regional rating, M7.2 tactic scenario, M8 chance allocation, M9 W/D/L+xG, M10 formation competition ve M11 final selection ayrı acceptance noktalarıdır.

M11 final ranking sırası outcome-first'tir: Expected Points → WinProbability → ExpectedGoalDifference → Tactical/fit/stability/deterministic tie-break. M11 final seçilen XI için yedi taktiklik DB3 zinciri korunur.

## 4. Yedi taktiğin denetim durumu

### Normal
Değişmeyen baseline. Taktik etkisi uygulanmaz.

### Pressing — TAMAMLANDI
`PressingTacticEvaluator` DEF/STAM/EXP, Powerful, opponent attack, suppression, own-chance loss, opportunity cost ve matchup sinyallerini ayrı tutuyor. Paper bridge ve evaluator regression doğrulandı.

### Counter Attack — TAMAMLANDI
`CounterAttackTacticEvaluator` midfield kaybı, DEF+2×Passing, experience, CA fırsat hacmi, finishing quality, opponent attack quality, specialty interaction ve Normal opportunity cost katmanlarını ayrı değerlendiriyor. Çoklu rakip ve formasyon × 7 taktik acceptance doğrulandı.

### AiM — KOD/ARAŞTIRMA TAMAMLANDI, REGRESSION KONTROLÜ
AiM evaluator; Passing, Experience, gerçek M8 conversion, centre-vs-wing quality, opponent wing-threat/defensive risk, possession ve Normal opportunity cost katmanlarını ayırıyor.

### AoW — KOD/ARAŞTIRMA TAMAMLANDI, REGRESSION KONTROLÜ
AoW evaluator; Passing, Experience, M8 conversion, iki kanadın kalite katkısı, centre-defence risk, possession ve Normal opportunity cost katmanlarını ayırıyor.

### Long Shots — KOD/ARAŞTIRMA TAMAMLANDI, REGRESSION KONTROLÜ
`LongShotsTacticEvaluator` Scoring + Set Pieces, gerçek M8 LS conversion, shooter pool, opponent keeper quality, possession, opportunity cost ve win-probability loss'u ayrı değerlendiriyor.

### Creative — TAMAMLANDI
Creative evaluator M9 özel olay çıktılarını temel alıyor; specialty diversity, negative-event risk ve Normal opportunity loss ayrı tutuluyor.

## 5. DB3 / tek kanonik taktik seçimi

Aynı final XI için yedi taktik tek `TacticalMatchupDatabase` altında tutulur. `BestEligible()` outcome-first çalışır; eligibility önce gelir, sonra ExpectedPoints ve outcome kalite tie-break'leri, en son fit/tactical/deterministic katmanları gelir.

`MotorPipelineService` M11-selected XI'ın yedi taktik satırını DB3'e bağlar ve seçimi `BestEligible()` üzerinden üretir. `Analysis.SelectedTactic` aynı outcome-first sözleşmesini uygular.

Web karşılaştırma ekranında canonical tactic identity ve `node --check` regression guard bulunur.

## 6. Rakip karar matrisi

Zorunlu senaryolar: güçlü midfield/attack, güçlü defence, güçlü centre defence, güçlü/zayıf wing defence, yüksek/düşük stamina, shooter/GK eşleşmesi, Creative-friendly specialty yoğun rakip, Pressing rakibi ve CA rakibi.

CA-01 kapsamında çoklu rakip profilleri üzerinden Formasyon × 7 taktik matrix koşuldu; M9 outcome alanları, 7/7 tactic coverage ve canonical final XI zinciri doğrulandı. MATCHUP-01 ayrıca takip ediliyor.

## 7. MotorDB / regression / acceptance

Zorunlu kontroller:

- 7 taktik mevcut ve eligibility doğru.
- W/D/L finite/normalize.
- `ExpectedPoints = 3W + D`.
- aynı seed deterministic.
- M9 formation winner deterministic.
- tactic winner deterministic.
- fit/outcome çelişkisi regression ile yakalanıyor.
- M11 final XI üzerinde 7/7 tactic chain mevcut.
- DB3 winner ile selected tactic aynı canonical identity'ye sahip.

### C22 / C23 / C24

C22 ve C23 regression katmanları uygulandı. C24 strict historical tactic-labelled calibration corpus bekliyor.

**C24 kapanış blokajı:** `TestJSON/TacticalOutcomeCalibrationCorpus_2026-09-07.json` branch'te mevcut değil. Mevcut CHPP fixture farklı schema olduğu için C24 yerine geçirilemez; tarihsel tactic-labelled veri uydurulmayacak.

## 8. Uygulama sırası / güncel durum

1. BUG-01 — KOD DÜZELTİLDİ; full acceptance ile son doğrulama sürüyor.
2. FORM-01 — REGRESSION GÜÇLENDİRİLDİ; full acceptance ile son doğrulama sürüyor.
3. PRESS-01 — TAMAMLANDI.
4. CA-01 — TAMAMLANDI.
5. AIM-01 — SIRADAKİ.
6. AOW-01 — UYGULAMA/ARAŞTIRMA TAM; regression kapanışı gerekiyor.
7. LS-01 — UYGULAMA/ARAŞTIRMA TAM; regression kapanışı gerekiyor.
8. CREATIVE-REG — TAMAMLANDI.
9. MATCHUP-01 — AÇIK.
10. FINAL-01 — AÇIK.
11. UI-01 — KOD TAMAMLANDI; production behavior smoke final acceptance kapsamında.

## 9. Acceptance workflow

`.github/workflows/v5-build.yml` acceptance stage'leri ve JavaScript syntax regression guard içerir. Geçici çalışma düzeninde offline C1-C24 regression pipeline'dan çıkarılmış; JS syntax → Docker build → GHCR push → Azure deploy akışı korunmuştur.

Run #998 (`34453061720`) JS syntax, Docker build, GHCR push ve Azure deploy adımlarını başarıyla tamamladı.

## 10. V5 Teknik Manuel PDF Projesi

Teknik manuel yalnızca repository code, tests ve verified sources üzerinden güncellenecek. A8 base PDF ile A9 dated supplement ayrı tutulur. Manuel, yayımlanmamış hidden-engine formüllerini resmî gerçek gibi göstermeyecek.

## 11. Çalışma kuralı

Her madde kod + regression + acceptance/MotorDB ile doğrulanmadan TAMAMLANDI sayılmayacak. Bir stage kapanınca sıradaki stage'e geçilecek; failure önce düzeltilip aynı stage tekrar koşturulacak.

## 12. Yedek Oyuncu Seçimi — Uygulama Planı

Yedek oyuncu seçimi ayrı bir analiz değildir. Ana analiz pipeline'ının tamamlanmasının hemen ardından, aynı analiz sırasında CHPP'den alınmış oyuncu verileri ve seçilmiş final XI kullanılarak çalışır. M6–M11 yeniden çalıştırılmaz ve yeni analiz başlatılmaz.

Amaç: İlk 11'e seçilmeyen oyuncular arasından Hattrick maç dizilişindeki yedek ekranına benzer şekilde 7 bölge için hızlı yedek önerileri üretmek. Her bölge için 2 alternatif gösterilir.

### 12.1 Slotlar

1. Kaleci
2. Göbek Defans
3. Bek
4. İç Orta Saha
5. Forvet
6. Kanat
7. Ekstra

### 12.2 Seçim mantığı — V1

- Kaynak havuz ana analizde CHPP'den alınan tüm oyunculardır.
- Final 11 yedek havuzundan çıkarılır.
- Kalan oyuncular ilgili slot uygunluğuna göre değerlendirilir.
- M6 beam search veya M9 tekrar hesabı kullanılmaz.
- Her slot için en uygun 2 oyuncu önerilir.
- Kullanıcı öneriyi manuel değiştirebilir.

### 12.3 Ana analiz entegrasyonu

`CHPP → oyuncular → M3 → M4 → M5 → M6 → M7 → M7.2 → M8 → M9 → M10 → M11 → Yedek Seçimi → Analysis sonucu → UI`

Yedek sonuçları aynı `MotorRunLog` payload'ında taşınır. Yeni CHPP oyuncu okuması yapılmaz; mevcut oyuncu listesi ve final XI yeniden kullanılır. `Analysis` JSON kök şeması değiştirilmez.

### 12.4 UI

- Yedek bölümü İlk 11 kutusunun hemen altında.
- Varsayılan kapalı accordion.
- `Yedek Oyuncular (7 slot)` başlığı.
- Her slotta motor önerileri ve uygun takım oyuncularından manuel seçim.
- Manuel seçim tarayıcıda korunur.

### 12.5 Uygulama sırası

1. **YED-01 — TAMAMLANDI:** 7 slot için iki alternatif.
2. **YED-02 — TAMAMLANDI:** aynı analiz run'ına payload entegrasyonu.
3. **YED-03 — TAMAMLANDI:** kapalı accordion ve 7 slot UI.
4. **YED-04 — TAMAMLANDI:** motor önerileri + manuel oyuncu seçimi.
5. **YED-05 — TAMAMLANDI:** production build/deploy ve UI smoke erişimi doğrulandı.

## 13. CHPP → Hattrick Match Order Aktarımı — Yeni Plan

Amaç: HattrickAI'nin analiz sonucunu kullanıcı onayıyla Hattrick'in yaklaşan maçına gerçek match order olarak göndermek.

### 13.1 İnternet araştırması sonucu — doğrulanmış çerçeve

- CHPP'nin `matchOrders` API'si yaklaşan maç için match order verisini kapsıyor; mevcut dokümantasyonda takım tutumu (`Attitude`), taktik (`TacticType`), sahadaki oyuncular (`Lineup`) ve resmi rol slotları bulunuyor. `RoleID` ile kaleci, sağ/merkez/sol defans, kanatlar, orta saha ve forvet slotları tanımlanıyor; ayrıca 7 tip yedek slotu için substitution roller bulunuyor. 
- Hattrick'in güncel eşleşme emri modeli, kadro + yedekler + taktik/maç emirlerini tek match-order konseptinde ele alıyor. Hattrick kuralları maç emrinin maç başlamadan önce verilmesini zorunlu kılıyor; bu nedenle aktarım zamanı ayrıca kontrol edilecek.
- CHPP üzerinden match order yazma gerçek bir desteklenen özellik: CHPP yetkilendirme ekranında ürünün "upcoming matches" için match orders ayarlama yetkisi açıkça gösterilebiliyor. Yazma yetkileri CHPP uygulama izinlerine bağlıdır.
- CHPP kullanımı için uygulamanın uygun CHPP onayına ve kullanıcı OAuth yetkilendirmesine dayanması gerekiyor; kullanıcı Hattrick şifresini Hattrick dışına vermeyecek.
- Araştırılan güncel CHPP istemci implementasyonlarında `matchorders` için daha yeni API sürümleri ve JSON tabanlı lineup payload'ları kullanılıyor; 2.5+ matchorders formatında lineup constructor'ları 14 başlangıç/pozisyon slotu ve 7 primary + 7 backup bench slotunu destekliyor. Bu nedenle bizim 7 slot yedek modelimiz ilk aşamada **primary bench** olarak eşlenecek; backup bench ayrıca planlanacak.
- Taktik enum'larının güncel CHPP sürümüyle birebir doğrulanması gerekiyor. Eski resmi XML sayfasında Normal, Pressing, Counter-attacks, AiM, AoW ve Creative kodları açıkça listeleniyor; Long Shots için güncel `matchorders` şemasında ayrıca doğrulama yapılmadan UI'da yazma seçeneği açılmayacak.

### 13.2 Ürün akışı

`Analiz → İlk 11 + 7 Yedek + Taktik → Aktarım Önizleme → [ HATTRICK'E AKTAR ] → OAuth/CHPP yetkisi → matchOrders write → doğrulama`

Aktarım butonu doğrudan sessiz yazma yapmayacak. Önce özet/önizleme gösterilecek ve kullanıcı açıkça onaylayacak.

### 13.3 Aktarılacak veri sözleşmesi

**A. İlk 11**
- 11 oyuncunun gerçek Hattrick PlayerID'leri.
- Her oyuncunun resmi RoleID/position slotu.
- Gerekliyse behaviour/repositioning bilgisi.
- Oluşturulan formasyonun Hattrick'in legal role modeline dönüştürülmesi.

**B. Yedekler**
- Bizim 7 slotumuz CHPP'nin primary bench slotlarına eşlenecek:
  - Keeper
  - Central Defender
  - Wing Back
  - Inner Midfielder
  - Forward
  - Winger
  - Extra
- Backup bench (ikinci 7'li) ilk sürümde boş bırakılacak.
- Aynı oyuncunun aynı match order içinde iki kez atanması engellenecek.

**C. Taktik**
- `Analysis.SelectedTactic` canonical identity'si CHPP `TacticType` değerine çevrilecek.
- Desteklenmeyen/ambiguous tactic varsa aktarım engellenecek; fallback olarak Normal'a sessizce düşülmeyecek.
- Taktik seviyesi gerekiyorsa CHPP'nin gerçekten yazılabilir alanı ayrıca doğrulanacak; engine içi V5 skorları Hattrick'e gönderilmeyecek.

### 13.4 Güvenlik ve izinler

1. Mevcut OAuth/CHPP akışı incelenecek.
2. Match-order write permission uygulama seviyesinde mevcut mu doğrulanacak.
3. Yoksa CHPP başvuru/izin gereksinimi dokümante edilecek.
4. Access token/secret yalnızca mevcut güvenli credential katmanında tutulacak; frontend'e çıkarılmayacak.
5. Frontend yalnızca kendi analiz run'ının aktarım payload'ını backend'e gönderecek.

### 13.5 Backend mimarisi

Yeni bir `MatchOrderExportService`/eşdeğer servis katmanı oluşturulacak.

Akış:

`UI POST → validation → CHPP OAuth client → upcoming match kontrolü → matchOrders payload → CHPP set → response parse → saved export result`

Backend, kullanıcıdan gelen PlayerID/role kombinasyonunu körlemesine göndermeyecek. Aktarım öncesi:
- oyuncuların takımda olup olmadığı,
- final XI'da duplicate olup olmadığı,
- 11 saha slotunun legal olup olmadığı,
- 7 yedek slotunun duplicate/uygunsuz olup olmadığı,
- maçın yaklaşan ve emir kabul edebilir durumda olup olmadığı
kontrol edilecek.

### 13.6 UI planı

İlk 11 + Yedek bölümünün altında:

**[ Hattrick'e Aktar ]**

Butona basınca:

1. `Aktarım Önizleme` açılır.
2. Formasyon, 11 oyuncu, 7 yedek ve taktik gösterilir.
3. Hedef maç ve başlama zamanı gösterilir.
4. CHPP write izni yoksa açıkça `CHPP match-order yazma izni gerekli` denir.
5. Kullanıcı `Onayla ve Hattrick'e Aktar` der.
6. Başarı/ret/error sonucu kullanıcıya gösterilir.

### 13.7 Doğrulama / read-after-write

Write başarılı cevabı tek başına yeterli kabul edilmeyecek.

1. CHPP `OrdersSet`/eşdeğer başarı cevabı kaydedilecek.
2. Aynı upcoming match için match order tekrar okunacak.
3. İlk 11, yedekler ve taktik karşılaştırılacak.
4. Uyuşmazlık varsa sonuç `Aktarım doğrulanamadı` olarak işaretlenecek.
5. Kullanıcıya Hattrick tarafındaki gerçek durum gösterilecek.

### 13.8 Test sırası

1. **CHPP-WRITE-01 — READ ONLY:** upcoming match + mevcut match order okunacak.
2. **CHPP-WRITE-02 — PAYLOAD:** V5 XI → RoleID/Behaviour + 7 primary bench mapping üretilecek.
3. **CHPP-WRITE-03 — VALIDATION:** duplicate, takım üyeliği, legal formation ve match deadline guard testleri.
4. **CHPP-WRITE-04 — MOCK WRITE:** gerçek Hattrick'e yazmadan CHPP request/response contract testi.
5. **CHPP-WRITE-05 — PERMISSION:** uygulamanın match-order write izni doğrulanacak.
6. **CHPP-WRITE-06 — REAL WRITE:** yalnızca kullanıcı açık onayıyla kontrollü bir yaklaşan maçta gerçek aktarım.
7. **CHPP-WRITE-07 — READ-BACK:** yazılan diziliş/yedek/taktik tekrar okunup birebir doğrulanacak.
8. **CHPP-WRITE-08 — UI ACCEPTANCE:** `Hattrick'e Aktar` → önizleme → onay → başarı/ret → read-back zinciri production smoke ile kapatılacak.

### 13.9 Kapsam sınırları

İlk sürümde yalnızca **mevcut analizde seçilmiş İlk 11 + 7 primary yedek + desteklenen taktik + yaklaşan maç** aktarılacak.

Captain, set-pieces, penalty taker, otomatik substitution orders, conditional behaviour changes ve ikinci 7'li backup bench ilk sürümde aktarım kapsamına alınmayacak; bunlar ayrı acceptance maddeleri olacak.

### 13.10 Tamamlanma kriteri

Bu özellik ancak şu zincir gerçek CHPP üzerinde doğrulanınca TAMAMLANDI sayılacak:

`Analiz → Önizleme → Kullanıcı onayı → CHPP matchOrders write → başarı cevabı → read-back → Hattrick'teki gerçek XI + yedek + taktik eşleşmesi`

CHPP write yetkisi/uygulama onayı mevcut değilse kod tarafı hazırlanabilir ancak özellik production'da aktif olarak TAMAMLANDI sayılmayacak.
