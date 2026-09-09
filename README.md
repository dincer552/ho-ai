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

### Pressing — ARA DENETİMDE
`PressingTacticEvaluator` DEF/STAM/EXP, Powerful, opponent attack, suppression, own-chance loss, opportunity cost ve matchup sinyallerini ayrı tutuyor. `TacticPaperMappingEngine` Pressing için V5 0–10 ölçeğini paper'ın yayımlanmış %5–%41 suppression envelope'una özel bridge ile bağlıyor. Exact hidden engine formula iddia edilmiyor.

Eksik kapanış: evaluator ağırlıklarının her birinin kaynak mekaniğinden ayrı regression ile kilitlenmesi ve latest MotorDB üzerinden kabulü.

### Counter Attack — KOD/ARAŞTIRMA TAMAMLANDI, ACCEPTANCE KONTROLÜ
`CounterAttackTacticEvaluator` midfield kaybı, DEF+2×Passing, experience, CA fırsat hacmi, finishing quality, opponent attack quality, specialty interaction ve Normal opportunity cost katmanlarını ayrı değerlendiriyor. 2026 paper CA curve ve %4–%45 envelope tactic-specific paper bridge ile temsil ediliyor.

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

Henüz kapanmamış madde: bu profillerin tamamını aynı acceptance fixture setinde **Formasyon A/B/C × 7 taktik** matrisi olarak koşup tek kanonik sonuç tablosunda doğrulamak.

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
3. **PRESS-01 — ARA DENETİMDE:** source bridge tamam; evaluator bileşenlerinin ayrı weight regression kapanışı gerekiyor.
4. **CA-01 — UYGULAMA/ARAŞTIRMA TAM:** full opponent/profile acceptance kapanışı gerekiyor.
5. **AIM-01 — UYGULAMA/ARAŞTIRMA TAM:** full regression matrix kapanışı gerekiyor.
6. **AOW-01 — UYGULAMA/ARAŞTIRMA TAM:** full regression matrix kapanışı gerekiyor.
7. **LS-01 — UYGULAMA/ARAŞTIRMA TAM:** full regression matrix kapanışı gerekiyor.
8. **CREATIVE-REG — TAMAMLANDI:** önceki Creative kazanımları korunuyor.
9. **MATCHUP-01 — AÇIK:** çoklu rakip × çoklu formasyon × 7 taktik matrix acceptance.
10. **FINAL-01 — AÇIK:** M9 → M10 → M11 full end-to-end acceptance sweep.
11. **UI-01 — KOD TAMAMLANDI:** canonical tactic identity + JS syntax guard mevcut; gerçek production behavior smoke final acceptance kapsamında.

## 9. Acceptance workflow

`.github/workflows/v5-build.yml` C1–C20, C22–C24 stage'lerini tek tek çalıştırır ve `tactic-comparison.js` için syntax regression yapar. Acceptance bir stage'de başarısız olursa Docker build/deploy durur.

Son full-sweep denemesi C1 M3 continuity assertion'ında durdu: Foxtrick family-level primary/secondary mapping ile raw rank #1/#2 yanlış özdeşleştirilmişti. Test düzeltildi; sonraki sweep bu düzeltmenin sonucunu doğrulayacaktır.

## 10. V5 Teknik Manuel PDF Projesi

Teknik manuel yalnızca repository code, tests ve verified sources üzerinden güncellenecek. A8 base PDF ile A9 dated supplement ayrı tutulur. Manuel, yayımlanmamış hidden-engine formüllerini resmî gerçek gibi göstermeyecek.

## 11. Çalışma kuralı

Her madde kod + regression + acceptance/MotorDB ile doğrulanmadan TAMAMLANDI sayılmayacak. Bir stage kapanınca sıradaki stage'e geçilecek; failure önce düzeltilip aynı stage tekrar koşturulacak.
