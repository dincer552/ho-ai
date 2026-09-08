# HattrickAI V5

# README TOP BLOCK — 08.09.2026

> **08.09.2026 — TAKTİK UYGUNLUK MOTORU ÇALIŞMA PLANI**
>
> Amaç: Her `TeamTactic` için tek bir genel `TacticalScore` kullanmak yerine, taktiğin gerçek Hattrick match-engine gereksinimlerini, XI oyuncu yerleşimini, rakip eşleşmesini, faydasını ve karşılanmayan koşullarda oluşturduğu zararı ayrı ayrı hesaplamak.
>
> **Kritik kural:** M10/M11 eşik ve anti-lock mekanizması değiştirilmeyecek. Yeni katman taktik uygunluğunu doğru ölçmek için kullanılacak; mevcut eşiklerin yerine keyfi yeni eşikler konulmayacak.
>
> ## Taktik geliştirme sırası
>
> 1. **Yaratıcı Oyna (Creative)** — **DENETİM TAMAMLANDI 08.09.2026.**
> 2. **Pressing** — savunma/stamina, rakip şans bastırma ve yan etki. **Sıradaki tam katsayı denetimi.**
> 3. **Counter Attack** — eligibility, midfield kaybı, savunma üstünlüğü ve CA fırsat kalitesi.
> 4. **Attack in the Middle (AiM)** — merkez hücum eşleşmesi, dönüşüm getirisi ve savunma maliyeti.
> 5. **Attack on Wings (AoW)** — iki kanat eşleşmesi, dönüşüm getirisi ve merkezi savunma maliyeti.
> 6. **Long Shots** — taktik seviyesi, shooter kalitesi, fırsat maliyeti ve rakip GK/defence eşleşmesi.
> 7. **Normal** — diğer taktiklerin değişmeyen baseline'ı.
>
> Her taktik için ortak çıktı katmanları:
>
> - **Requirements:** gerekli oyuncu/skill/pozisyon koşulları.
> - **Benefit:** koşullar sağlandığında gerçek mekanik avantaj.
> - **Penalty:** taktiğin yapısal yan etkisi.
> - **Opponent interaction:** rakibin taktiği etkisizleştirme/cezalandırma gücü.
> - **Opportunity cost:** aynı XI'ın Normal'e göre kaybı.
> - **Suitability:** XI + rakip için taktik uygunluğu.
> - **Explanation:** sonucu belirleyen koşulların açıklaması.
>
> ### Creative audit — TAMAMLANDI 08.09.2026
>
> Creative katmanında katsayı ve double-counting denetimi tamamlandı.
>
> - Hattrick'in yayımladığı mekanikler esas kaynak olarak tutuldu: taktik seviyesinde Passing 4× Experience ağırlığı, Unpredictable oyuncuların 2× katkısı, özel olay hacmi/ownership etkisi ve %7.5 savunma azaltımı. Kesin canlı taktik-seviye formülü yayımlanmadığı için V5 sahte bir exact formula iddia etmiyor.
> - `CreativeTacticEvaluator` artık `CreativeEventMultiplier` değerini ikinci kez ödül olarak uygulamıyor. Özel olay faydası M9'un gerçek çıktı katmanından okunuyor; böylece M9'da uygulanan event-volume mekanizması evaluator'da tekrar sayılmıyor.
> - Rakip özel olay etkisi artık oyuncu/yedek sayısından türetilen ikinci bir gizli formülle uydurulmuyor. Evaluator, M9'un own/opponent special-event goal çıktısından sınırlı bir event-edge sinyali çıkarıyor.
> - Creative uygunluğu; taktik seviyesi, gerçekleşen özel olay gol dengesi, specialty diversity, negatif özel olay riski, aynı XI için Normal fırsat maliyeti ve %7.5 savunma trade-off'u üzerinden hesaplanıyor. Bu ağırlıkların V5 heuristiği olduğu açıkça kodda belirtiliyor; Hattrick'in gizli formülü olarak sunulmuyor.
> - Goalkeeper Creative taktik seviyesi hesabına dahil edilmiyor; oyuncu ağırlıkları başlangıçtaki outfield oyuncular üzerinden yürütülüyor.
> - Yedek oyuncular Creative specialty hesabına dahil edilmiyor; event karşılaştırması M9'un gerçek rakip XI bağlamına bırakılıyor.
> - Kritik sınır: Hattrick'in exact Creative tactical-level ve special-event allocation formülleri yayımlanmış değildir. Bu nedenle Creative sonucu historical match corpus ile kalibre edilene kadar "source-derived mechanics + bounded V5 heuristic" olarak kabul edilecek.
>
> ### T1 — M9 audit — TAMAMLANDI 07.09.2026
>
> Aynı XI + aynı rakip için yedi taktiğin M9 W/D/L ve xG çıktıları doğrulanıyor; Normal değişmeyen baseline olarak korunuyor. Mevcut xG clamp ve M10/M11 mekanizmaları değiştirilmedi.
>
> ### T2 — M9 tactic effect chain — TAMAMLANDI 07.09.2026
>
> Taktiklerin M8 → M9 zincirleri bağlandı: Creative kendi event katmanını etkiler, Pressing normal chance volume'u bastırır, CA kaçırılan rakip Normal şansından fırsat üretir, AiM/AoW gerçek sektör paylarını M9 weighted quality'ye taşır, Long Shots public shooter-vs-GK formülüyle M9'a gerçek event goal katkısı verir. Rakip event hesabında own-tactic sızıntısı düzeltildi.
>
> ### T3 — M9 outcome layer — TAMAMLANDI 07.09.2026
>
> M9 `ExpectedPoints = 3×W + D` ve `ExpectedGoalDifference = xG farkı` çıktıları kanonikleştirildi. Aynı seed Monte Carlo deterministikliği ve yedi taktik outcome metrikleri regression'a bağlandı.
>
> ### T4 — DB3 Tactical Matchup — TAMAMLANDI 07.09.2026
>
> DB2'deki XI × 7 taktik sonuçlarını tek matchup veri modelinde toplamak için `TacticalMatchupDatabase` ve builder eklendi. Her kayıt W/D/L, xG, Expected Points, ΔxG, fit/eligibility ve explanation taşır. Bir XI'ın en iyi taktiği Expected Points üzerinden deterministik sıralanabilir. M10/M11 threshold + anti-lock değişmedi.
>
> ### T5 — DB3 outcome-driven final tactic — TAMAMLANDI 07.09.2026
>
> DB3 gerçek pipeline'a bağlandı. Final taktik artık eligible sonuçlar arasında `ExpectedPoints` üzerinden seçiliyor; suitability/fit yalnızca deterministik tie-break olarak kalıyor. Seçilen taktik `FinalMatchPlan` ve `Analysis` üzerinden kanonik şekilde taşınıyor. M10/M11 threshold + anti-lock değişmedi.
>
> ### T6 — DB3 JSON görünürlüğü — TAMAMLANDI 07.09.2026
>
> Motor sonucu DB3 matchup kayıtlarını, seçilen taktiği ve seçilen taktiğin Expected Points değerini web/JSON katmanına taşıyor.
>
> ### T7 — canlı web DB3 karşılaştırma ekranı — ARA VERİLDİ 08.09.2026
>
> Canlı web ekranı için güvenli UI katmanı uygulandı ancak bu çalışma oturumunda yarıda bırakıldı. `tactic-comparison.js` ile final XI'ın eligible taktik sonuçlarının ve Expected Points sıralamasının görünür olması hedefleniyor. `Analysis` tarafında final-XI `CandidateId` eşleşmesi düzeltildi; Docker tarafında browser cache busting yapıldı. Bu UI'nın production build/deploy sonucu bu noktada ayrıca doğrulanmadı. Buradan devam edilecek.
>
> ### T8 — tactical outcome edge-case regression — UYGULANDI 07.09.2026
>
> DB3 outcome katmanı için yeni acceptance regression eklendi: yedi taktiğin tamamının korunması, W/D/L sonlu ve normalize olması, `ExpectedPoints = 3×W + D` kanonikliği, yüksek fit ama düşük outcome durumunda outcome'un kazanması, ineligible kayıtların dışlanması, duplicate satırların deterministik replacement davranışı ve geçersiz olasılıkların reddedilmesi test ediliyor. Bu katman istatistiksel olarak eğitilmiş bir calibration modeli değildir; mevcut M9 outcome sözleşmesini koruyan regression guard'dır.
>
> ### Pressing — ARA VERİLEN NOKTA 08.09.2026
>
> Pressing'in suppression katmanı source-vs-heuristic denetiminde kısmen düzeltildi. 2026 paper Equation B.2'nin Pressing için verdiği %5–%41 normal-chance suppression aralığının V5 0–10 tactical-level ölçeğine taşınması için `TacticPaperMappingEngine` içinde özel Pressing RT bridge uygulandı. Böylece V5 artık Pressing'i fiziksel olarak anlamsız şekilde %100 suppression'a extrapolate etmiyor. Regression guard'ları V5=0 → %5, V5=10 → %41 ve ara değerin bu aralıkta kaldığını kontrol ediyor.
>
> MotorDB latest-10 kontrolünde Pressing örnekleri yaklaşık %16–%18 suppression seviyesinde görüldü; `own chance loss` suppression ile aynı ve `net suppression` bu örneklerde 0. Pressing'in DEF/STAM/EXP desteği, Powerful DEF boost'u, opponent attack value ve opportunity-cost ağırlıkları ise **henüz tam source-vs-heuristic katsayı denetiminden geçirilmedi**.
>
> **KALDIĞIMIZ YER:** Pressing'i yeniden açınca ilk iş `PressingTacticEvaluator.cs` içindeki katsayıları ve özellikle `PowerfulDefenceBoost`, DEF/STAM/EXP katkıları, suppression → benefit, opportunity cost ve matchup ağırlıklarını kaynak mekanikleriyle tek tek denetlemek. Ardından latest MotorDB ile regression kontrolü yapılacak. Sonra Counter Attack'a geçilecek.
>
> ## V5 Teknik Manuel PDF Projesi — Çalışma Planı
>
> Bu bölüm HattrickAI V5'in gerçek kod, test ve kaynak dokümanlarından oluşturulacak teknik manuel PDF çalışmasının takip alanıdır.
>
> Amaç: V5 motorlarının yaptığı işlemleri, kullanılan matematikleri, katsayıları, veri akışlarını ve web kullanımını sadece mevcut kaynaklara dayanarak dokümante etmek.
>
> ### Manuel hazırlama aşamaları
>
> ## ÇALIŞMA OTURUMU DURAKLATILDI — 08.09.2026
>
> Bu oturum burada bilinçli olarak yarıda kesildi. Bir sonraki oturumda yukarıdaki **KALDIĞIMIZ YER** maddesinden devam edilecek. Önceki Creative/T1–T8 kazanımları korunacak; Pressing yeniden başlatılacak ve tamamlanmadan Counter Attack'a geçilmeyecek.
