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
> 2. **Pressing** — savunma/stamina, rakip şans bastırma ve yan etki.
> 3. **Counter Attack** — eligibility, midfield kaybı, savunma üstünlüğü ve CA fırsat kalitesi.
> 4. **Attack in the Middle (AiM)** — merkez hücum eşleşmesi, dönüşüm getirisi ve savunma maliyeti.
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
> ### Uygulama döngüsü
>
> `Araştırma → veri modeli → taktiğe özel evaluator → M9/event entegrasyonu → regression → gerçek Motor DB → gerçek maç doğrulaması → kalibrasyon`
>
> Creative araştırmasının ayrıntılı teknik şartnamesi: `HattrickAI_V5/Docs/CREATIVE_TACTIC_RESEARCH_2026-09-07.md`
