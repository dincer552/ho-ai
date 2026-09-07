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
> 5. **Attack on Wings (AoW)** — iki kanat eşleşmesi, dönüşüm getirisi ve savunma maliyeti.
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
> Pressing araştırmasının ayrıntılı teknik şartnamesi: `HattrickAI_V5/Docs/PRESSING_TACTIC_RESEARCH_2026-09-07.md`
>
> AiM araştırmasının ayrıntılı teknik şartnamesi: `HattrickAI_V5/Docs/AIM_TACTIC_RESEARCH_2026-09-07.md`
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
