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
> 6. **Long Shots** — taktik seviyesi, shooter kalitesi, fırsat maliyeti ve rakip GK/defence eşleşmesi. **Dedicated evaluator + M9 scoring zinciri kodlandı — 07.09.2026.**
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
> ### T1 — M9 audit — TAMAMLANDI 07.09.2026
>
> Aynı XI + aynı rakip için yedi taktiğin M9 W/D/L ve xG çıktıları doğrulanıyor; Normal değişmeyen baseline olarak korunuyor. Mevcut xG clamp ve M10/M11 mekanizmaları değiştirilmedi.
>
> ### T2 — M9 tactic effect chain — TAMAMLANDI 07.09.2026
>
> Taktiklerin M8 → M9 zincirleri bağlandı: Creative yalnızca kendi event katmanını etkiler, Pressing normal chance volume'u bastırır, CA kaçırılan rakip Normal şansından fırsat üretir, AiM/AoW gerçek sektör paylarını M9 weighted quality'ye taşır, Long Shots public shooter-vs-GK formülüyle M9'a gerçek event goal katkısı verir. Rakip event hesabında own-tactic sızıntısı düzeltildi.
>
> ### T3 — M9 outcome layer — TAMAMLANDI 07.09.2026
>
> M9 `ExpectedPoints = 3×W + D` ve `ExpectedGoalDifference = xG farkı` çıktıları kanonikleştirildi. Aynı seed Monte Carlo deterministikliği ve yedi taktik outcome metrikleri regression'a bağlandı.
>
> ### T4 — DB3 Tactical Matchup — TAMAMLANDI 07.09.2026
>
> DB2'deki XI × 7 taktik sonuçlarını tek matchup veri modelinde toplamak için `TacticalMatchupDatabase` ve builder eklendi. Her kayıt W/D/L, xG, Expected Points, ΔxG, fit/eligibility ve explanation taşır. Bir XI'ın en iyi taktiği Expected Points üzerinden deterministik sıralanabilir. M10/M11 threshold + anti-lock değişmedi.
>
> **Sonraki aşama: T5 — DB3'ün gerçek pipeline çıktısına bağlanması ve final taktik seçiminde M9 outcome katmanının kullanılması.**
>
> ## V5 Teknik Manuel PDF Projesi — Çalışma Planı
>
> Bu bölüm HattrickAI V5'in gerçek kod, test ve kaynak dokümanlarından oluşturulacak teknik manuel PDF çalışmasının takip alanıdır.
>
> Amaç: V5 motorlarının yaptığı işlemleri, kullanılan matematikleri, katsayıları, veri akışlarını ve web kullanımını sadece mevcut kaynaklara dayanarak dokümante etmek.
>
> ### Manuel hazırlama aşamaları
>
```