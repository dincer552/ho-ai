# HattrickAI V5

# README TOP BLOCK — 09.09.2026

> **09.09.2026 — TAKTİK + M9 FORMASYON KARAR MOTORU TAM DENETİM PLANI**
>
> Amaç: Taktik seçimini yalnızca `TacticFitScore` veya genel `TacticalScore` ile değil, **aynı XI + aynı rakip için M9'un gerçek W/D/L, Expected Points ve xG sonuçlarını** temel alan outcome-first bir karar zincirine dönüştürmek. Aynı denetim formasyon seçimine de uygulanacak: M9 hangi formasyonun rakibe karşı daha iyi maç sonucu ürettiğini gösteriyorsa, final önerinin bunu gerçekten dikkate aldığı kanıtlanacak.
>
> **Kritik karar ilkesi:** Hattrick'in yayımlanmış mekanikleri ile V5'in kendi heuristikleri birbirinden ayrılacak. Kaynakta olmayan gizli katsayılar "resmî formül" gibi kullanılmayacak. M10/M11 threshold ve anti-lock mekanizması, testler açıkça başka bir davranış gerektirmedikçe korunacak.
>
> ## 0. İLK TESPİT — BUG / KARAR ZİNCİRİ KOPUKLUĞU
>
> 09.09.2026 incelemesinde canlı ekranda **Yaratıcı Oyna** seçili görünürken aynı XI için taktik karşılaştırmasında **Pres = 2.63 Expected Points** ile en yüksek sonuç göründü. Kod tarafında da `MotorPipelineService` içindeki final taktik seçimi eligible sonuçları `TacticFitScore` ile sıralıyor; bu, DB3'ün daha önce tanımlanan outcome-first sözleşmesiyle çelişiyor.
>
> **Düzeltme hedefi:** `ExpectedPoints` birincil seçim metriği olacak; `TacticFitScore`, `WinProbability`, `TacticalScore` ve diğer uygunluk sinyalleri yalnızca açıkça tanımlanmış tie-break / risk katmanı olacak. Ekrandaki "Seçilen taktik" ile DB3'te birinci olan taktik aynı kanonik kaynaktan üretilecek.
>
> ## 1. KAYNAK / MEKANİK DENETİMİ — TAM KAPSAM
>
> Her taktik için önce Hattrick kaynak mekanikleri çıkarılacak, sonra mevcut V5 koduyla satır satır karşılaştırılacak.
>
> Kullanılacak ana referanslar:
>
> - Hattrick Wiki — Tactics: https://wiki.hattrick.org/wiki/Tactics
> - Hattrick Wiki — Match engine: https://wiki.hattrick.org/wiki/Match_engine
> - Hattrick Wiki — Regular chances: https://wiki.hattrick.org/wiki/Regular_chances
> - Hattrick Wiki — Rules: https://wiki.hattrick.org/wiki/Rules
> - Pressing: https://wiki.hattrick.org/wiki/Pressing
> - Counter-attacks: https://wiki.hattrick.org/wiki/Counter-attacks
> - Attack in the middle: https://wiki.hattrick.org/wiki/Attack_in_the_middle
> - Attack on wings: https://wiki.hattrick.org/wiki/Attack_on_wings
> - Play creatively: https://wiki.hattrick.org/wiki/Play_creatively
> - Long shots: https://wiki.hattrick.org/wiki/Long_shots
> - Hattrick Community Press — tactic explanations: https://www.hattrick.org/en/Community/Press/
> - 2026 Pressing research paper: https://doi.org/10.1016/j.entcom.2026.101131
>
> Kaynaklardan doğrulanan temel çerçeve:
>
> - Normal maç şansları midfield/possession ile dağıtılır; saha şansları merkez + iki kanat ve set-piece bileşenlerinden oluşur.
> - AiM toplam şans sayısını artırmaz; merkez/kanat dağılımını değiştirir ve karşılığında ilgili savunma sektörünü zayıflatır.
> - AoW bunun tersidir: kanatlara yönlendirir ve merkez savunmasında maliyet oluşturur.
> - Counter Attack midfield gücünden vazgeçerek rakibin başarısız saldırılarından ek fırsat üretir; kullanılabilmesi için midfield kaybı, savunma üstünlüğü ve bu fırsatları değerlendirecek hücum kalitesi birlikte düşünülmelidir.
> - Pressing rakibin normal şanslarını bastırır fakat kendi şanslarını da yok edebilir; stamina özellikle ikinci yarı midfield/possession kaybı açısından kritiktir.
> - Long Shots normal hücum fırsatlarının bir bölümünü uzun şuta dönüştürür; güçlü savunmaya karşı ve yeterli shooter/GK-vs-shooter dengesi olduğunda anlamlıdır.
> - Creative özel olay hacmini ve olay sahipliği olasılığını artırır ancak savunma tarafında maliyeti vardır; negatif özel olaylar da artabilir.
>
> ## 2. M9 FORMASYON KARARI — ÖNCE BUNU NETLEŞTİR
>
> Şu an M10 formasyon yarışında kompozit skor kullanıyor: tactical quality + Monte Carlo win probability + structural score. M11 ise tactical, win, structural, stability ve risk-adjusted outcome bileşenlerini birlikte kullanıyor. Dolayısıyla sistem henüz "M9'da hangi formasyonun maç sonucu en iyiyse onu oynat" kadar saf bir outcome-first karar vermiyor.
>
> ### Hedef karar sözleşmesi
>
> Aynı rakip ve aynı maç koşulları altında tüm legal formasyon finalistleri için M9 sonucu karşılaştırılacak:
>
> 1. `ExpectedPoints = 3 × WinProbability + DrawProbability` kanonik ana outcome metriği.
> 2. `WinProbability` ikinci seviye tie-break / risk sinyali.
> 3. `ExpectedGoalDifference` ve xG seviyesi üçüncü seviye kalite kontrolü.
> 4. Tactical/structural suitability yalnızca outcome birbirine yeterince yakınsa tie-break olarak kullanılacak.
> 5. Anti-lock gereği tüm legal formasyonlar yarışta kalacak; düşük skor alan formasyonlar gizlice elenmeyecek.
> 6. M9 sonucu ile final öneri arasındaki fark açıklanabilir olacak: "M9 outcome lideri X, fakat Y seçildi çünkü ..." gibi gerekçesiz bir override olmayacak.
>
> **Acceptance:** En yüksek Expected Points üreten formasyonun, tanımlı risk/tie-break kuralı dışında final öneriden sebepsiz biçimde farklı olması testte başarısız sayılacak.
>
> ## 3. FORMASYON DENETİMİ — M4 → M5 → M6 → M9 → M10 → M11
>
> Her aşama ayrı doğrulanacak:
>
> ### M4 — legal formation universe
> - Tüm legal formasyonlar üretiliyor mu?
> - Anti-lock havuzu korunuyor mu?
> - Aynı formasyon yanlışlıkla farklı isim/signature ile iki kez sayılıyor mu?
>
> ### M5 — XI quality
> - Her formasyon için yeterli XI derinliği var mı?
> - En iyi oyuncuların başka formasyona kayması nedeniyle bazı formasyonlar yapay olarak zayıf bırakılıyor mu?
> - Position/order legality ve player-order kombinasyonları doğru mu?
>
> ### M6-A / M6-B
> - Beam search güçlü formasyonları korurken alternatifleri de tutuyor mu?
> - M10 rank-driven M6-B bütçesi outcome liderini erken kaybettirmiyor mu?
> - Her legal formasyon DB1/DB2'ye gerçekten taşınıyor mu?
>
> ### M7 / M7.2 / M8
> - Formasyonun rating etkisi ve taktik seviyesi gerçek XI'dan geliyor mu?
> - Rakip rating ve rakip XI her finalistte aynı bağlamla kullanılıyor mu?
> - Taktik yönlendirmesi chance allocation'a doğru taşınıyor mu?
>
> ### M9
> - Her formasyon + taktik kombinasyonu aynı rakibe karşı çalıştırılıyor mu?
> - W/D/L normalize ve deterministic mi?
> - Expected Points kanonik olarak `3W+D` mi?
> - xG / ΔxG ile outcome arasında tutarsızlıklar raporlanıyor mu?
> - Aynı seed / aynı girdide aynı sonucu veriyor mu?
>
> ### M10 / M11
> - Formasyon liderliği outcome ile karşılaştırılıyor mu?
> - TacticalScore'un yüksek olması kötü M9 sonucunu haksız yere bastırıyor mu?
> - M11'de final winner gerçekten tanımlı outcome hedefini optimize ediyor mu?
> - Threshold/anti-lock mekanizması korunurken objective değişikliği açık ve testli mi?
>
> ## 4. TAKTİK DENETİMİ — YEDİ TAKTİĞİN TAMAMI
>
> Sıra:
>
> 1. **Normal** — değişmeyen baseline.
> 2. **Pressing** — suppression + own-chance loss + stamina + DEF + EXP + Powerful + rakip attack.
> 3. **Counter Attack** — midfield kaybı + defans üstünlüğü + CA fırsat hacmi + fırsat kalitesi.
> 4. **Attack in the Middle** — merkez attack gain + wing defence loss + Passing/Experience tabanı + rakip merkez savunması.
> 5. **Attack on Wings** — iki kanat attack gain + centre defence loss + kanat attack/defence matchup.
> 6. **Long Shots** — shooter pool + Set Pieces/GK ilişkisi + gerçek LS conversion + kaybedilen regular chance maliyeti.
> 7. **Creative** — special-event upside + negative event risk + specialty diversity + defence trade-off + rakip event etkisi.
>
> Her evaluator için zorunlu çıktı:
>
> - Requirements
> - Benefit
> - Penalty
> - Opponent interaction
> - Opportunity cost vs Normal
> - Suitability
> - M9 outcome contribution
> - Explanation
>
> **Önemli:** `TacticFitScore` hiçbir evaluator'da M9 outcome'un yerine geçmeyecek. Fit, "bu taktiği kullanmak için oyuncu ve rakip şartları uygun mu?" sorusudur; Expected Points ise "bu taktik bu maçta sonucu ne yapıyor?" sorusudur. İkisi aynı şey değildir.
>
> ## 5. PRESSING — İLK TAM DENETİM
>
> Mevcut `PressingTacticEvaluator` içinde şu katsayılar tek tek source-vs-heuristic olarak ayrılacak:
>
> - `PowerfulDefenceBoost`
> - DEF contribution
> - STAM contribution
> - EXP contribution
> - suppression → benefit dönüşümü
> - own chance loss
> - net suppression
> - opponent attack value
> - midfield risk
> - opportunity cost
> - matchup weighting
>
> 2026 paper'daki Pressing suppression aralığı V5 tactical-level 0–10 ölçeğine bridge edilirken source-derived sınırlar korunacak. V5'te yayımlanmamış katsayılar "resmî" diye etiketlenmeyecek.
>
> **Acceptance:** V5=0 ve V5=10 uçları source-derived suppression sınırlarını aşmayacak; orta seviyeler monoton ve fiziksel olarak anlamlı olacak; yüksek DEF/STAM/EXP her zaman otomatik olarak "Pressing kazanır" anlamına gelmeyecek, gerçek M9 outcome ile birlikte değerlendirilecek.
>
> ## 6. COUNTER ATTACK DENETİMİ
>
> `CounterAttackTacticEvaluator` ve M9 CA zinciri birlikte denetlenecek.
>
> - Midfield kaybı gerçekten gerekli mi?
> - Defending + Passing CA eligibility ile doğru bağlanmış mı?
> - İki/üç defender kaynaklı non-tactical CA katkıları double-count ediliyor mu?
> - Rakip normal attack kalitesi ile CA fırsat kalitesi ayrılıyor mu?
> - CA'nın düşük possession maliyeti M9'da gerçekten görülüyor mu?
> - Rakip zayıfsa CA'nın "rakibin fırsatını kesip kendine çevirme" faydası yapay büyütülüyor mu?
>
> **Acceptance:** Midfield'i kazanırken CA'nın otomatik olarak önerilmesi mümkün olmayacak; CA ancak kaybedilen possession + savunma üstünlüğü + yeterli hücum bitiriciliği üçlüsü mantıklı olduğunda güçlü aday olacak.
>
> ## 7. AiM / AoW DENETİMİ
>
> Hedef, yalnızca taktik seviyesini yükseltmek değil, **yönlendirilen şansın gerçekten daha kaliteli bir hücum sektörüne gidip gitmediğini** ölçmek.
>
> AiM:
> - merkez attack gain
> - wing defence penalty
> - toplam attack quality
> - rakip centre defence
> - passing/experience tactical level
>
> AoW:
> - left/right attack gain ayrı ayrı
> - centre defence penalty
> - iki kanadın rakip savunmayla ayrı matchup'ı
> - toplam attack quality
>
> **Acceptance:** Güçlü merkez attack + zayıf rakip merkez defans AiM lehine; güçlü kanat attack + zayıf rakip kanat defans AoW lehine sonuç üretmeli. Sadece "tactical level yüksek" olduğu için yönlendirme kazanmayacak.
>
> ## 8. LONG SHOTS DENETİMİ
>
> `LongShotsTacticEvaluator` ile M8/M9 Long Shot zinciri aynı veri üzerinden kontrol edilecek.
>
> - Gerçek shooter pool çıkarılacak.
> - Shooter kalitesi ile GK/Set Pieces karşılaştırılacak.
> - LS conversion aralığı source-derived sınırlar içinde tutulacak.
> - LS'nin normal attack fırsatından vazgeçme maliyeti ölçülecek.
> - Güçlü rakip defans + iyi shooter kombinasyonu beklenen durumda LS lehine dönecek.
> - Zayıf shooter veya kolay normal hücum varken LS gereksiz yere yükselmeyecek.
>
> ## 9. CREATIVE DENETİMİ — KORUNACAK
>
> Creative audit tamamlandı ve mevcut kazanımlar regression ile korunacak.
>
> - CreativeEventMultiplier double-count edilmeyecek.
> - M9 own/opponent special-event çıktısı temel alınacak.
> - Unpredictable / specialty etkileri source-derived çerçevede tutulacak.
> - Negatif event riski hesaba katılacak.
> - %7.5 savunma trade-off'u ayrı gösterilecek.
> - Goalkeeper ve yedek oyuncular yanlışlıkla outfield specialty havuzuna katılmayacak.
>
> ## 10. TAKTİK SEÇİMİ — TEK KANONİK KARAR
>
> Aynı final XI için yedi taktik aynı rakibe karşı çalıştırılacak ve tek bir `TacticalMatchupDatabase` altında tutulacak.
>
> Önerilen karar sırası:
>
> **Eligibility → Expected Points → risk/WinProbability → ΔxG/xG → TacticFitScore → TacticalScore → deterministic signature**
>
> Ancak ilk dört adım için kesin ağırlıklı skor yazılmayacak; önce her metriğin bağımsız davranışı regression ile doğrulanacak. Amaç tekrar M10'daki gibi birbirinden farklı objective'lerin tek bir sihirli composite score içinde gizlenmesini önlemek.
>
> UI, API ve `FinalMatchPlan` aynı `SelectedTactic` kaynağını kullanacak. Karşılaştırma tablosunun birinci satırı ile üstte gösterilen "Seçilen taktik" farklıysa acceptance fail olacak.
>
> ## 11. RAKİBE GÖRE KARAR MATRİSİ
>
> Test fixture'ları özellikle şu rakip profillerini kapsayacak:
>
> 1. **Güçlü midfield / güçlü attack / zayıf defence**
> 2. **Güçlü defence / zayıf attack**
> 3. **Güçlü centre defence / zayıf wing defence**
> 4. **Zayıf centre defence / güçlü wing defence**
> 5. **Yüksek stamina rakip**
> 6. **Düşük stamina rakip**
> 7. **Güçlü shooter/GK eşleşmesi**
> 8. **Çok sayıda specialty / Creative-friendly rakip**
> 9. **Pressing kullanan rakip**
> 10. **Counter Attack kullanan rakip**
>
> Her fixture'da beklenen sonuçtan önce gerçek M9 sonucu kaydedilecek; sonra evaluator'ın açıklaması bu sonuçla karşılaştırılacak. Heuristic sonuç source-derived mekanik ile çelişiyorsa katsayı değiştirilecek, gerekçesiz yeni katsayı eklenmeyecek.
>
> ## 12. FORMASYON × TAKTİK BİRLİKTE TEST
>
> En kritik acceptance matrisi:
>
> **Formasyon A/B/C × 7 taktik × aynı rakip**
>
> Burada iki ayrı kazanan tutulacak:
>
> - **M9 formation winner:** en iyi formasyon sonucu.
> - **Tactic winner for selected XI:** seçilen formasyon üzerindeki en iyi taktik sonucu.
>
> Bunlar karıştırılmayacak. Önce formasyonun rakibe karşı uygunluğu, sonra o XI'ın taktik tercihi kanonik olarak gösterilecek.
>
> Ayrıca:
>
> - M9'da formasyon A kazanırken M11'in B seçmesi açıklamasız olamaz.
> - Pres 2.63 ve Creative 2.51 iken Creative seçilemez; ancak explicit eligibility/risk override varsa açıklaması JSON'a yazılmalıdır.
> - Aynı XI'da sadece fit yüksek diye düşük Expected Points'li taktik seçilemez.
>
> ## 13. MOTORDB / REGRESSION / ACCEPTANCE
>
> Her aşama sonunda latest MotorDB ile kontrol yapılacak.
>
> Zorunlu regression'lar:
>
> - 7 taktiğin tamamı mevcut.
> - Her taktik için eligibility doğru.
> - W/D/L normalize.
> - `ExpectedPoints = 3W + D`.
> - Aynı seed deterministic.
> - M9 formation winner deterministik.
> - Tactic winner deterministik.
> - Fit/outcome çelişkisi özellikle test ediliyor.
> - UI selected tactic = DB3 winner.
> - FinalPlan formation = canonical formation winner veya açık, testli override.
> - Anti-lock tüm legal formasyonları koruyor.
> - M6 cancellation/performance guard korunuyor.
>
> ## 14. UYGULAMA SIRASI
>
> 1. **BUG-01:** TacticFitScore ile final tactic seçiminin ayrıştırılması; Expected Points kanonikliğinin kod/test/UI'da doğrulanması.
> 2. **FORM-01:** M9 formasyon winner audit; M10/M11'in gerçekten hangi objective'i optimize ettiğinin netleştirilmesi.
> 3. **PRESS-01:** Pressing tam source-vs-heuristic katsayı denetimi.
> 4. **CA-01:** Counter Attack eligibility + opportunity model.
> 5. **AIM-01:** Attack in the Middle.
> 6. **AOW-01:** Attack on Wings.
> 7. **LS-01:** Long Shots.
> 8. **CREATIVE-REG:** Creative tamamlanan katmanın tüm yeni değişikliklere karşı regression korunması.
> 9. **MATCHUP-01:** 7 tactic × multiple opponent profiles × multiple formations matrix.
> 10. **FINAL-01:** M9 → M10 → M11 final recommendation chain end-to-end acceptance.
> 11. **UI-01:** Comparison table, selected tactic, selected formation ve explanation'ın aynı canonical result'tan geldiğinin production testi.
>
> **KURAL:** Her madde gerçekten kod + test + MotorDB/acceptance ile tamamlanmadan README'de TAMAMLANDI olarak işaretlenmeyecek. Her tamamlanan maddeden sonra bu README güncellenecek.
>
> ## Önceki çalışma kayıtları
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
> ### T5 — DB3 outcome-driven final tactic — GERİ AÇILDI 09.09.2026
>
> Önceki "tamamlandı" kabulü canlı davranışla yeniden doğrulanacak. Mevcut pipeline'da `MotorPipelineService` final taktiği `TacticFitScore` ile sıraladığı için T5'in outcome-first sözleşmesi fiilen bozulmuş durumda. Önce BUG-01 ile gerçek kanonik seçim yeniden bağlanacak.
>
> ### T6 — DB3 JSON görünürlüğü — ARA DENETİMDE
>
> DB3 matchup kayıtları, seçilen taktik ve Expected Points web/JSON katmanına taşınıyor; ancak seçilen taktik ile karşılaştırma tablosunun birinci satırı aynı kaynaktan gelene kadar production acceptance tamamlanmış sayılmayacak.
>
> ### T7 — canlı web DB3 karşılaştırma ekranı — ARA DENETİMDE 09.09.2026
>
> UI'da yedi taktiğin karşılaştırması görünür. Ancak canlı örnekte karşılaştırmanın lideri Pres iken üstte Yaratıcı Oyna gösterildiği için canonical selected-tactic zinciri yeniden doğrulanacak. Bu konu BUG-01 kapsamındadır.
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
> **KALDIĞIMIZ YER:** Önce BUG-01 ve FORM-01 ile final karar zinciri düzeltilecek. Ardından Pressing yeniden açılıp `PressingTacticEvaluator.cs` içindeki `PowerfulDefenceBoost`, DEF/STAM/EXP katkıları, suppression → benefit, opportunity cost ve matchup ağırlıkları kaynak mekanikleriyle tek tek denetlenecek. Sonra latest MotorDB ile regression kontrolü yapılacak ve Counter Attack'a geçilecek.
>
> ## V5 Teknik Manuel PDF Projesi — Çalışma Planı
>
> Bu bölüm HattrickAI V5'in gerçek kod, test ve kaynak dokümanlarından oluşturulacak teknik manuel PDF çalışmasının takip alanıdır.
>
> Amaç: V5 motorlarının yaptığı işlemleri, kullanılan matematikleri, katsayıları, veri akışlarını ve web kullanımını sadece mevcut kaynaklara dayanarak dokümante etmek.
>
> ### Manuel hazırlama aşamaları
>
> ## ÇALIŞMA OTURUMU — 09.09.2026
>
> Yeni çalışma planı README'ye gömüldü. İlk sırada BUG-01 ve FORM-01 var. Taktiklerin tamamı rakibe göre baştan sona denetlenmeden ve M9 formasyon kazananı ile final öneri zinciri doğrulanmadan yeni taktik katsayılarına geçilmeyecek.
