# HattrickAI V5

# README TOP BLOCK — 09.09.2026

> **Taktik + M9 formasyon karar motoru: devam eden tam denetim**
>
> Amaç: Taktik ve formasyon seçiminde `TacticFitScore` / `TacticalScore` gibi uygunluk sinyallerinin M9 maç sonucunun önüne geçmesini engellemek. Aynı XI + aynı rakip için kanonik ana sonuç `ExpectedPoints = 3 × WinProbability + DrawProbability` olarak korunur; WinProbability, ExpectedGoalDifference/xG, fit ve tactical skorlar yalnızca tanımlı tie-break/diagnostic katmanlarıdır.
> 
> Kaynakta olmayan gizli engine katsayıları resmî formül gibi kullanılmaz. Source-derived mekanik, V5 heuristiği ve gerçek M9 sonucu ayrı tutulur.

## 1. Kaynak / mekanik kapsamı

Ana referanslar:

- Hattrick Wiki — Tactics: https://wiki.hattrick.org/wiki/Tactics
- Match engine: https://wiki.hattrick.org/wiki/Match_engine
- Regular chances: https://wiki.hattrick.org/wiki/Regular_chances
- Rules / Manual: https://wiki.hattrick.org/wiki/Rules
- Pressing: https://wiki.hattrick.org/wiki/Pressing
- Counter-attacks: https://wiki.hattrick.org/wiki/Counter-attacks
- Attack in the middle: https://wiki.hattrick.org/wiki/Attack_in_the_middle
- Attack on wings: https://wiki.hattrick.org/wiki/Attack_on_wings
- Play creatively: https://wiki.hattrick.org/wiki/Play_creatively
- Long shots: https://wiki.hattrick.org/wiki/Long_shots
- 2026 Constantinou et al.: https://doi.org/10.1016/j.entcom.2026.101131

Kaynak çerçevesi: AiM ve AoW şans yönünü değiştirir ve ilgili savunma sektörünü zayıflatır; CA midfield maliyeti karşılığında rakibin başarısız normal saldırılarından fırsat üretir; Pressing toplam normal-chance hacmini bastırır ve stamina/DEF/EXP/Powerful önemlidir; Long Shots normal fırsatların bir bölümünü şuta çevirir ve shooter/GK kalitesi kritiktir; Creative özel olay fırsatlarını artırırken negatif olay ve savunma maliyeti taşır.

## 2. M9 formasyon karar sözleşmesi

Tüm legal formasyon finalistleri aynı rakibe karşı karşılaştırılır:

1. `ExpectedPoints = 3W + D` ana outcome metriğidir.
2. `WinProbability` ve `ExpectedGoalDifference` outcome kalite/tie-break sinyalidir.
3. `TacticFitScore`, `TacticalScore`, structural/stability sinyalleri outcome eşitliğinde veya açıklayıcı katmanda kalır.
4. Anti-lock mekanizması tüm legal formasyonları yarışta tutar.
5. M9 liderinden farklı bir final seçiminde açık ve testli override gerekir.

M10 regression artık formation ranking'in `ExpectedPoints`-first olduğunu, eşit EP durumunda WinProbability tie-break'inin korunmasını ve deterministic rerun'da aynı sıralamanın çıkmasını kontrol eder.

## 3. M4 → M5 → M6 → M7 → M7.2 → M8 → M9 → M10 → M11

M4 legal universe, M5 XI quality, M6 beam/refinement, M7 regional rating, M7.2 tactic scenario, M8 chance allocation, M9 W/D/L+xG, M10 formation competition ve M11 final selection birbirinden ayrı acceptance noktaları olarak tutulur.

M11 final ranking sırası outcome-first'tir: Expected Points → WinProbability → ExpectedGoalDifference → Tactical/fit/stability/deterministic tie-break. M11 final seçilen XI için yedi taktiklik DB3 zinciri korunur.

## 4. Yedi taktiğin denetim durumu

### Normal
Değişmeyen baseline. Taktik etkisi uygulanmaz.

### Pressing — TAMAMLANDI
`PressingTacticEvaluator` DEF/STAM/EXP, Powerful, opponent attack, suppression, own-chance loss, opportunity cost ve matchup sinyallerini ayrı tutuyor. `TacticPaperMappingEngine` Pressing için V5 0–10 ölçeğini paper'ın yayımlanmış %5–%41 suppression envelope'una özel bridge ile bağlıyor. Evaluator ağırlıkları ayrı regression ile kilitlendi ve latest acceptance/MotorDB sweep'i başarılı.

### Counter Attack — TAMAMLANDI
`CounterAttackTacticEvaluator` midfield kaybı, DEF+2×Passing, experience, CA fırsat hacmi, finishing quality, opponent attack quality, specialty interaction ve Normal opportunity cost katmanlarını ayrı değerlendiriyor. 2026 paper CA curve ve %4–%45 envelope tactic-specific paper bridge ile temsil ediliyor. Çoklu rakip profili ve formasyon × 7 taktik acceptance matrisi başarıyla geçti; CA eligibility, outcome alanları ve canonical tactic zinciri doğrulandı.

### AiM — KOD/ARAŞTIRMA TAMAMLANDI, REGRESSION KONTROLÜ
AiM evaluator; outfield Passing, Experience, gerçek M8 conversion, centre-vs-wing quality, opponent wing-threat/defensive risk, possession context ve Normal opportunity cost katmanlarını ayırıyor. Hattrick mekanikleri 15–30%, paper simulation 20–35% wing→centre ve yaklaşık 47–55% central share çerçevesiyle uyumlu tutuluyor.

### AoW — KOD/ARAŞTIRMA TAMAMLANDI, REGRESSION KONTROLÜ
AoW evaluator; outfield Passing, Experience, M8 conversion, iki kanadın ayrı kalite katkısı, centre-defence risk, possession ve Normal opportunity cost katmanlarını ayırıyor. Hattrick 20–40% centre→wing; paper 34–52% conversion envelope'u source bridge üzerinden korunuyor.

### Long Shots — KOD/ARAŞTIRMA TAMAMLANDI, REGRESSION KONTROLÜ
`LongShotsTacticEvaluator` Scoring + Set Pieces gereksinimini, gerçek M8 LS conversion'ı, shooter pool'u, opponent keeper quality, possession, normal-attack opportunity cost ve win-probability loss'u ayrı değerlendiriyor. Gizli canlı katsayılar uydurulmuyor.

### Creative — TAMAMLANDI
Creative evaluator M9 özel olay çıktılarını temel alıyor ve `CreativeEventMultiplier`'ı ikinci kez ödül olarak uygulamıyor. Outfield specialty diversity, negative-event risk, Normal opportunity loss ve %7.5 defence trade-off ayrı sinyaller olarak tutuluyor. GK/bench yanlışlıkla specialty havuzuna katılmıyor.

## 5. DB3 / tek kanonik taktik seçimi

Aynı final XI için yedi taktik tek `TacticalMatchupDatabase` altında tutulur. `BestEligible()` outcome-first çalışır; eligibility önce gelir, sonra ExpectedPoints ve outcome kalite tie-break'leri, en son fit/tactical/deterministic katmanları gelir.

`MotorPipelineService` M11-selected XI'ın yedi taktik satırını DB3'e bağlar ve seçimi `BestEligible()` üzerinden üretir. `Analysis.SelectedTactic` aynı outcome-first sözleşmesini uygular.

Web karşılaştırma ekranında numeric enum ile string enum farkı için canonical tactic identity eklendi; seçilen taktik satırının UI'da yanlışlıkla eşleşmemesi düzeltildi. `node --check` regression guard workflow'a eklendi.

## 6. Rakip karar matrisi

Zorunlu senaryolar: güçlü midfield/attack, güçlü defence, güçlü centre defence, güçlü wing defence veya zayıf wing defence, yüksek/düşük stamina, güçlü shooter/GK eşleşmesi, Creative-friendly specialty yoğun rakip, Pressing rakibi ve CA rakibi.

CA-01 kapsamında çoklu rakip profilleri üzerinden aynı acceptance fixture setinde Formasyon × 7 taktik matrix koşuldu; M9 outcome alanları, 7/7 tactic coverage ve canonical final XI zinciri başarıyla doğrulandı. Daha geniş MATCHUP-01 profili hâlâ ayrıca takip ediliyor.

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

**C24 kapanış blokajı:** `TestJSON/TacticalOutcomeCalibrationCorpus_2026-09-07.json` branch'te mevcut değil. Mevcut CHPP fixture farklı bir schema ve C24 corpus yerine geçirilemez. Tarihsel tactic-labelled veri uydurulmayacak. Gerçek CHPP-derived corpus sağlanınca C24 strict schema ile çalıştırılacak.

## 8. Uygulama sırası / güncel durum

1. **BUG-01 — KOD DÜZELTİLDİ:** TacticFitScore final selection'dan ayrıldı; DB3/M11 outcome-first zinciri ve UI tactic identity düzeltildi. Full acceptance ile son doğrulama sürüyor.
2. **FORM-01 — REGRESSION GÜÇLENDİRİLDİ:** M10 formation ranking için EP-first + tie-break + deterministic checks eklendi. Full acceptance ile son doğrulama sürüyor.
3. **PRESS-01 — TAMAMLANDI:** source bridge + evaluator weight regression + acceptance/MotorDB sweep başarılı.
4. **CA-01 — TAMAMLANDI:** opponent-aware CA evaluator ve çoklu rakip/formasyon × 7 taktik acceptance doğrulandı.
5. **AIM-01 — SIRADAKİ:** uygulama/araştırma mevcut; şimdi full regression matrix ve acceptance kapanışı yapılacak.
6. **AOW-01 — UYGULAMA/ARAŞTIRMA TAM:** full regression matrix kapanışı gerekiyor.
7. **LS-01 — UYGULAMA/ARAŞTIRMA TAM:** full regression matrix kapanışı gerekiyor.
8. **CREATIVE-REG — TAMAMLANDI:** önceki Creative kazanımları korunuyor.
9. **MATCHUP-01 — AÇIK:** çoklu rakip × çoklu formasyon × 7 taktik matrix acceptance kapsamı genişletilecek.
10. **FINAL-01 — AÇIK:** M9 → M10 → M11 full end-to-end acceptance sweep.
11. **UI-01 — KOD TAMAMLANDI:** canonical tactic identity + JS syntax guard mevcut; gerçek production behavior smoke final acceptance kapsamında.

## 9. Acceptance workflow

`.github/workflows/v5-build.yml` C1–C20, C22–C24 stage'lerini tek tek çalıştırır ve `tactic-comparison.js` için syntax regression yapar. Acceptance bir stage'de başarısız olursa Docker build/deploy durur.

Latest full workflow run 974 başarıyla tamamlandı; offline acceptance, JavaScript syntax regression, Docker build/push ve Azure deploy adımlarının tamamı başarılı.

## 10. V5 Teknik Manuel PDF Projesi

Teknik manuel yalnızca repository code, tests ve verified sources üzerinden güncellenecek. A8 base PDF ile A9 dated supplement ayrı tutulur. Manuel, yayımlanmamış hidden-engine formüllerini resmî gerçek gibi göstermeyecek.

## 11. Çalışma kuralı

Her madde kod + regression + acceptance/MotorDB ile doğrulanmadan TAMAMLANDI sayılmayacak. Bir stage kapanınca sıradaki stage'e geçilecek; failure önce düzeltilip aynı stage tekrar koşturulacak.

## 12. Yedek Oyuncu Seçimi — Uygulama Planı

Yedek oyuncu seçimi **ayrı bir analiz değildir**. Ana analiz pipeline'ının tamamlanmasının hemen ardından, aynı analiz sırasında CHPP'den alınmış oyuncu verileri ve seçilmiş final XI kullanılarak çalışır. M6–M11 yeniden çalıştırılmaz ve yeni bir analiz başlatılmaz.

Amaç: Ana analiz tarafından ilk 11'e seçilmeyen oyuncular arasından, Hattrick maç dizilişindeki yedek ekranına benzer şekilde 7 bölge için hızlı ve basit yedek önerileri üretmek. Her bölge için 2 alternatif gösterilir.

### 12.1 Slotlar

7 yedek slotu:

1. Kaleci
2. Göbek Defans
3. Bek
4. İç Orta Saha
5. Forvet
6. Kanat
7. Ekstra

Her slotta:
- 1. tercih
- 2. alternatif

bulunur.

### 12.2 Seçim mantığı — V1

- Kaynak havuz = CHPP'den ana analizde alınan tüm oyuncular.
- Ana analizde seçilen final 11 oyuncu yedek havuzundan çıkarılır.
- Kalan oyuncular ilgili yedek slotuna uygunluklarına göre değerlendirilir.
- Karmaşık optimizasyon, M6 beam search veya M9 tekrar hesabı kullanılmaz.
- Öncelik ilgili bölge/pozisyonu oynayabilme uygunluğu ve mevcut oyuncu kalitesidir.
- Her slot için en uygun 2 oyuncu önerilir.
- Aynı oyuncunun birden fazla slotu gereksiz şekilde doldurması mümkün olduğunca engellenir.
- Uygun oyuncu bulunamazsa slot boş gösterilir.
- Kullanıcı daha sonra önerilen oyuncuyu manuel olarak değiştirebilir.

### 12.3 Ana analiz entegrasyonu

Akış:

`CHPP → oyuncular → M3 → M4 → M5 → M6 → M7 → M7.2 → M8 → M9 → M10 → M11 → Yedek Seçimi → Analysis sonucu → UI`

Yedek sonuçları ana analiz çalışmasının aynı `MotorRunLog` payload'ında taşınır. Yeni CHPP oyuncu okuması yapılmaz; ana analizde zaten alınmış olan oyuncu listesi ve final XI yeniden kullanılır. Mevcut `Analysis` JSON kök şeması değiştirilmez.

### 12.4 UI

- Yedek bölümü, mevcut **İlk 11 kutusunun hemen altında** yer alır.
- Varsayılan durumda **kapalı** olur.
- Kullanıcı açtığında `Yedek Oyuncular (7 slot)` bölümü görünür.
- Her slotta iki oyuncu önerisi bulunur ve aynı seçim alanından uygun takım oyuncuları arasından manuel değişiklik yapılabilir.
- Seçim tarayıcıda saklanır; analiz tekrar çalıştırılmadıkça kullanıcının manuel seçimi korunur.
- İlk sürümde amaç hızlı ve okunabilir kullanım; karmaşık kadro optimizasyon arayüzü yapılmaz.

### 12.5 Uygulama sırası

1. **YED-01 — TAMAMLANDI:** Final XI dışındaki oyunculardan 7 slot için iki alternatif üretiliyor.
2. **YED-02 — TAMAMLANDI:** Yedek payload'ı aynı analiz run'ının motor loguna bağlandı; yeni CHPP okuması yapılmıyor.
3. **YED-03 — TAMAMLANDI:** İlk 11'in altında kapalı accordion ve 7 slot UI'sı eklendi.
4. **YED-04 — TAMAMLANDI:** Kullanıcı iki motor önerisi arasından veya slot için uygun takım oyuncularından seçim yapabiliyor; seçim tarayıcıda korunuyor.
5. **YED-05 — SIRADAKİ:** Ayrı regression aşaması eklemeden gerçek CHPP verisiyle analiz çalıştırılıp yedek sonuçları UI'da kontrol edilecek.

YED-04 için uygulama kodu ve mevcut acceptance workflow'a bağlı build doğrulaması yapılıyor; production smoke kontrolü YED-05'te yapılacak.
