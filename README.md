# HattrickAI V5

# README TOP BLOCK — 07.09.2026

> **07.09.2026 — TAKTİK UYGUNLUK MOTORU ÇALIŞMA PLANI**
>
> Amaç: Her `TeamTactic` için tek bir genel `TacticalScore` kullanmak yerine, taktiğin gerçek Hattrick match-engine gereksinimlerini, XI oyuncu yerleşimini, rakip eşleşmesini, faydasını ve karşılanmayan koşullarda oluşturduğu zararı ayrı ayrı hesaplamak.
>
> **Kritik kural:** M10/M11 eşik ve anti-lock mekanizması değiştirilmeyecek. Yeni katman taktik uygunluğunu doğru ölçmek için kullanılacak; mevcut eşiklerin yerine keyfi yeni eşikler konulmayacak.
>
> ## Taktik geliştirme sırası
>
> 1. **Yaratıcı Oyna (Creative)** — ilk ve en derin uygulama.
> 2. **Pressing** — savunma/stamina, rakip şans bastırma ve yan etki. **Araştırma + dedicated evaluator kodlandı — 07.09.2026.**
> 3. **Counter Attack** — eligibility, midfield kaybı, savunma üstünlüğü ve CA fırsat kalitesi. **Dedicated evaluator kodlandı — 07.09.2026.**
> 4. **Attack in the Middle (AiM)** — merkez hücum eşleşmesi, dönüşüm getirisi ve savunma maliyeti. **Dedicated evaluator kodlandı — 07.09.2026.**
> 5. **Attack on Wings (AoW)** — iki kanat eşleşmesi, dönüşüm getirisi ve merkezi savunma maliyeti. **Dedicated evaluator + araştırma kodlandı — 07.09.2026.**
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
> ### Creative ilk aşama
>
> Hattrick resmi geliştirici kaynaklarına göre Creative'de Passing, Experience'tan 4× daha önemlidir; Unpredictable oyuncular tactic-level katkısında 2× sayılır. Creative özel olay üretimini ve maksimum event sayısını artırır, bireysel event'in bize gelme ihtimalini etkiler ve savunmayı %7.5 düşürür. En kritik nokta: rakip daha uzman bir specialty portföyüne sahipse Creative geri tepebilir. Bu yüzden yalnızca `CreativeEventMultiplier` değil; **pozisyon + specialty + event skill matchup + negatif event riski + rakip specialty avantajı + savunma kaybı + Normal trade-off** birlikte hesaplanacak.
>
> ### Pressing ikinci aşama — 07.09.2026
>
> Pressing için resmi/ana kaynaklardan doğrulanan çekirdek: tüm outfield XI'ın defending + stamina katkısı, experience katkısı, Powerful oyuncuda defending'in 2× sayılması, normal şansların iki takım için azaltılması ve stamina maliyeti. Özel olaylar Pressing tarafından bastırılmaz. Yeni `PressingTacticEvaluator`, **DEF fit + STAM fit + EXP fit + Powerful katkısı + rakip normal şans bastırması + kendi şans kaybı + stamina riski + rakip saldırı değeri + Normal opportunity cost** katmanlarını ayrı değerlendiriyor.
>
> ### Counter Attack üçüncü aşama — 07.09.2026
>
> `CounterAttackTacticEvaluator` artık CA'yı genel objective hesabından ayırıyor. **Pre-penalty midfield eligibility + %7 midfield penalty + rakibin kaçırdığı Normal şans hacmi + %4–45 tactical CA conversion + DEF/Passing/Scoring fit + Quick/Technical CA etkileri + Normal opportunity cost** birlikte hesaplanıyor.
>
> ### Attack in the Middle dördüncü aşama — 07.09.2026
>
> `AttackMiddleTacticEvaluator` artık AiM'i genel objective hesabından ayırıyor. **Outfield Passing/Experience gereksinimi + %20–35 wing→centre conversion + %47–55 merkez payı + merkez hücum/merkez savunma eşleşmesi + kanat fırsat maliyeti + kanat savunma riski + Normal trade-off** birlikte hesaplanıyor. 2026 araştırmasının yayınlamadığı kesin savunma katsayısı için evaluator içinde açıkça bounded bir %10 risk proxy kullanılıyor; bu değer gizli motor katsayısı olarak iddia edilmiyor.
>
> ### Attack on Wings beşinci aşama — 07.09.2026
>
> `AttackWingsTacticEvaluator` artık AoW'u genel objective hesabından ayırıyor. **Outfield Passing/Experience gereksinimi + %34–52 centre→wing conversion + %63–70 wing share + iki kanat hücum/defans eşleşmesi + wing-vs-centre directional gain + merkez savunma riski + merkez fırsat maliyeti + Normal trade-off** birlikte hesaplanıyor. 2026 paper Appendix C Eq. C.2'deki AoW conversion curve M8 tarafından uygulanıyor; evaluator bu gerçek sonucu tüketiyor. Yayınlanmış kesin merkez-savunma katsayısı olmadığı için açıkça bounded %10 risk proxy kullanılıyor.
>
> Pressing araştırmasının ayrıntılı teknik şartnamesi: `HattrickAI_V5/Docs/PRESSING_TACTIC_RESEARCH_2026-09-07.md`
>
> AiM araştırmasının ayrıntılı teknik şartnamesi: `HattrickAI_V5/Docs/AIM_TACTIC_RESEARCH_2026-09-07.md`
>
> AoW araştırmasının ayrıntılı teknik şartnamesi: `HattrickAI_V5/Docs/AOW_TACTIC_RESEARCH_2026-09-07.md`
>
> **M10/M11 threshold + anti-lock değişmedi.**
>
> Creative araştırmasının ayrıntılı teknik şartnamesi: `HattrickAI_V5/Docs/CREATIVE_TACTIC_RESEARCH_2026-09-07.md`


## V5 Teknik Manuel PDF Projesi — Çalışma Planı

Bu bölüm HattrickAI V5'in gerçek kod, test ve kaynak dokümanlarından oluşturulacak teknik manuel PDF çalışmasının takip alanıdır.

Amaç: V5 motorlarının yaptığı işlemleri, kullanılan matematikleri, katsayıları, veri akışlarını ve web kullanımını sadece mevcut kaynaklara dayanarak dokümante etmek.

### Manuel hazırlama aşamaları

```
AŞAMA 0  Kaynak envanteri
AŞAMA 0.5 Motor / Kod Konum Haritası
AŞAMA 1  Sistem mimarisi [TAMAMLANDI]
AŞAMA 2  Veri modeli [TAMAMLANDI]
AŞAMA 3  Hattrick matematik modeli [TAMAMLANDI]
AŞAMA 4  Motor teknik dokümanları [TAMAMLANDI]
AŞAMA 5  Gerçek maç örnek analizi [TAMAMLANDI]
AŞAMA 6  Web arayüzü ve kullanıcı manueli [TAMAMLANDI]
AŞAMA 7  Developer/API manueli [TAMAMLANDI]
AŞAMA 8  Teknik Manuel PDF birleştirme ve yayın hazırlığı [TAMAMLANDI]
AŞAMA 9  MOTOR ÇIKTI JSON VERİTABANI [TAMAMLANDI — 06.09.2026]
         9.1 JSON veri sözleşmesi [TAMAMLANDI — 06.09.2026]
         9.2 JSON archive backend [TAMAMLANDI — 06.09.2026]
         9.3 Backend analysis entegrasyonu [TAMAMLANDI — 06.09.2026]
         9.4 Motor DB API [TAMAMLANDI — 06.09.2026]
         9.5 Motor Panel [TAMAMLANDI — 06.09.2026]
         9.6 Gerçek web JSON doğrulaması [PLAN — ayrı doğrulama gerektiriyor]
         9.7 C19 JSON regression [TAMAMLANDI — 06.09.2026]
         9.8 Deterministic JSON kontrolü [TAMAMLANDI — 06.09.2026]
         9.9 Dokümantasyon / PDF snapshot güncellemesi [TAMAMLANDI — 06.09.2026]
             - A8 temel PDF snapshotı korunarak Stage 9 için tarihli A9 publication supplement oluşturuldu.
             - TECHNICAL_MANUAL_INDEX.md 06.09.2026 snapshot ve yayın dosyasıyla güncellendi.
```

## 07.09.2026 — TAKTİK → M9 → RAKİP MAÇUP OPTİMİZASYONU / DB3 ÇALIŞMA PLANI

Amaç: Motorun yalnızca “bu kadro bu taktiğe uygun mu?” sorusunu değil, **“bu XI ile bu rakibe karşı hangi taktik gerçek W/D/L sonucunu en çok iyileştiriyor?”** sorusunu cevaplaması.

Bu aşamada taktik uygunluk skoru tek başına final karar kriteri olmayacak. Her DB2 XI için 7 taktik ayrı ayrı M7 → M7.2 → M8 → M9 zincirinden geçirilecek ve M9'un rakibe karşı ürettiği sonuçlar karşılaştırılacak.

**Kritik sınırlar:**
- M10/M11 threshold ve anti-lock mekanizmasına dokunulmayacak.
- Mevcut M8 chance-pool conservation korunacak.
- Yayınlanmamış/gizli Hattrick katsayıları uydurulmayacak; paper/wiki/formül kaynakları ile heuristic/proxy açıkça ayrılacak.
- M9 kalibrasyonu tamamlanmadan `WinProbability` tek başına “gerçek maç motoru” diye sunulmayacak.
- Son DB, inceleme amacıyla indirilebilir JSON olarak üretilecek.

### AŞAMA T1 — Mevcut M9 audit ve baseline [TAMAMLANDI — 07.09.2026]
- [x] M9'un bugün hangi M8 alanlarını kullandığını tek tek çıkart.
- [x] Normal'i değişmeyen baseline olarak sabitle.
- [x] Aynı XI + aynı rakip için 7 taktiğin mevcut M8/M9 çıktısını yan yana kaydet.
- [x] `ExpectedGoals` 5.00 tavanının karşılaştırmayı bozup bozmadığını kontrol et; mevcut üretim sınırı korunarak T1 kapsamında sınır değişikliğine gidilmedi.
- [x] Mevcut M9 regressionlarını baseline olarak koru; C8 regressionına aynı final XI üzerinde yedi taktik audit kontrolü eklendi.

### AŞAMA T2 — 7 taktiğin M9'a gerçek etki zinciri [PLAN]
Her taktiğin M9'a taşıdığı mekanik ayrı doğrulanacak:
- [ ] **Normal:** neutral baseline.
- [ ] **Creative:** event üretimi, event ownership, specialty pozitif/negatif etkileri ve savunma trade-off'u.
- [ ] **Pressing:** iki tarafın normal chance suppression etkisi, stamina etkisinin temsil edilmesi ve special event'lerin korunması.
- [ ] **Counter Attack:** midfield penalty, rakibin kaçırdığı normal şans, CA conversion ve CA goal katkısı.
- [ ] **AiM:** centre'a taşınan fırsatlar, centre-vs-centre sonucu ve wing defence/opportunity cost.
- [ ] **AoW:** iki kanada taşınan fırsatlar, iki yönlü matchup ve centre defence/opportunity cost.
- [ ] **Long Shots:** normal saldırıdan LS dönüşümü, shooter quality, Scoring + Set Pieces, rakip GK/defence etkileşimi ve normal chance kaybı.
- [ ] Her taktiğin pozitif ve negatif etkilerinin aynı M9 total expected-goals/W-D-L hesabında birlikte görünmesini sağla.

### AŞAMA T3 — M9 W/D/L ve maç sonucu motoru [PLAN]
- [ ] Her XI × taktik için `ExpectedHomeGoals`, `ExpectedAwayGoals`, `WinProbability`, `DrawProbability`, `LossProbability` üret.
- [ ] Poisson/Monte Carlo çıktısının aynı senaryo için tutarlı olduğunu doğrula.
- [ ] `ExpectedPoints = 3×Win + Draw` metriğini ekle.
- [ ] `ExpectedGoalDifference` ve gerekirse en olası skor dağılımını sakla.
- [ ] W/D/L değerlerinin Normal'e göre farkını hesapla: `ΔWin`, `ΔDraw`, `ΔLoss`, `ΔxG`, `ΔExpectedPoints`.
- [ ] M9 çıktısını kalibrasyon durumu ile birlikte sakla; tahmin ile doğrulanmış motor gerçeğini karıştırma.

### AŞAMA T4 — DB3: Taktik Matchup Database [PLAN]
DB2'den gelen her XI için 7 taktiğin tam matchup sonucu DB3'e yazılacak.

**Hedef uzay:** `DB2 XI × 7 TeamTactic`.

Her DB3 kaydı en az şunları içerecek:
- [ ] CandidateId / Formation / XI.
- [ ] Tactic.
- [ ] TacticalLevel ve M8 chance dağılımı.
- [ ] Own/opp expected goals.
- [ ] W/D/L probabilities.
- [ ] ExpectedPoints.
- [ ] ExpectedGoalDifference.
- [ ] TacticFitScore / SquadFit / MatchupFit / TradeoffCost.
- [ ] M9 event/xG breakdown (Creative, Pressing, CA, LS, PNF vb.).
- [ ] CalibrationStatus / source-proxy bilgisi.
- [ ] Rakibe karşı rank ve karar açıklaması.

DB3, yalnızca final seçimi için değil, **motoru incelemek ve hangi taktiğin neden kazandığını görmek için ham hesap alanı** olarak korunacak.

### AŞAMA T5 — Rakibe karşı taktik seçim motoru [PLAN]
- [ ] DB3'te yalnızca `TacticEligible == true` adayları yarıştırsın.
- [ ] Birincil karar: rakibe karşı en yüksek `WinProbability`.
- [ ] İkincil karar: `ExpectedPoints`.
- [ ] Üçüncül karar: `ExpectedGoalDifference`.
- [ ] Sonraki karar: `TacticFitScore`.
- [ ] Son karar: `TacticalScore`.
- [ ] Seçilen taktiğin açıklamasında “neden bu rakibe karşı?” farklarını göster.
- [ ] Aynı XI içinde taktik farklarını ve farklı XI'lar arasında taktik farklarını ayrı raporla.

### AŞAMA T6 — DB3 çıktı / indirme / Motor Panel [PLAN]
Mevcut Motor DB JSON indirme özelliği yeni DB3'ü de kapsayacak.
- [ ] Mevcut `📥 Motor DB JSON İndir` butonunun archive endpointinden aldığı JSON'a DB3'ü dahil et.
- [ ] DB3 ayrı bir bölüm/collection olarak export edilsin; DB1/DB2 verisi kaybolmasın.
- [ ] Export içinde runId, timestamp, formation, XI, tactic, M8, M9 ve final decision ilişkisi korunmalı.
- [ ] Büyük DB3 çıktısında veri kaybı/truncation olmadığını test et.
- [ ] İndirilen JSON'u offline acceptance ile tekrar okunabilir/doğrulanabilir hale getir.
- [ ] Motor Panel'de DB3 sonucu incelenebilir olsun: en iyi taktik, W/D/L, xG ve alternatiflerin farkı.

### AŞAMA T7 — Acceptance / regression / performans [PLAN]
- [ ] DB3 deterministic JSON regression testi ekle.
- [ ] 7 taktiğin tamamı için taktik routing regression testi ekle.
- [ ] M9 W/D/L conservation / probability sum testleri ekle.
- [ ] DB3 candidate/tactic count testleri ekle.
- [ ] Export edilen DB3 ile API'deki DB3 aynı sonucu verdiğini doğrula.
- [ ] Hesap süresini ölç; gerekirse DB3 hesaplamasını paralelleştir/optimize et, fakat doğruluk pahasına kısaltma yapma.
- [ ] M10/M11 threshold + anti-lock regressionlarını aynen çalıştır.
- [ ] GitHub Actions offline acceptance geçmeden production build/deploy başarılı kabul edilmesin.

### AŞAMA T8 — Gerçek maç doğrulaması ve kalibrasyon [PLAN]
- [ ] DB3'te saklanan W/D/L/xG sonuçlarını gerçek maç sonuçlarıyla karşılaştıracak veri seti oluştur.
- [ ] Taktik bazında calibration error ölç.
- [ ] Sistematik sapmaları yalnızca yeterli tarihsel veriyle düzelt.
- [ ] Gizli production katsayısı varsaymak yerine ölçülmüş kalibrasyon kullan.
- [ ] Kalibrasyon tamamlanana kadar UI'da modelin calibration status'unu göstermeye devam et.
