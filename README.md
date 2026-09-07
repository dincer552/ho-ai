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

AŞAMA 10  SSH'SİZ / DÜŞÜK RAM DEPLOYMENT MİMARİSİ [DEVAM EDİYOR — 06.09.2026]
         Amaç: GitHub Actions'ın Azure VM'ye SSH ile bağlanma, ssh-keyscan, scp ve Docker image taşıma bağımlılığını kaldırmak.
         VM'de Docker build/test çalıştırılmayacak; böylece düşük RAM'li VM gereksiz build yükü taşımayacak.
         10.1 Deployment mimarisini sabitle: GitHub Actions → container registry → VM [TAMAMLANDI]
         10.2 GitHub Container Registry (GHCR) image yayınlama akışını ekle [TAMAMLANDI]
         10.3 Azure VM'ye self-hosted GitHub Actions runner kurulumu [TAMAMLANDI — 06.09.2026]
         10.4 VM runner'ın Docker yetkilerini ve servis olarak otomatik başlamasını doğrula [TAMAMLANDI — 06.09.2026]
         10.5 Workflow'dan SSH / DEPLOY_SSH_KEY / ssh-keyscan / scp bağımlılıklarını kaldır [TAMAMLANDI]
         10.6 VM runner üzerinden yalnızca image pull + container restart/deploy çalıştır [TAMAMLANDI — 06.09.2026]
         10.7 CHPP secret aktarımını güvenli şekilde koru [KODLANDI — UÇTAN UCA DOĞRULAMA SONRAKİ RUN'DA]
         10.8 Health check ve başarısız deployment rollback davranışını doğrula [PLAN]
         10.9 C20 + Docker build + GHCR + VM deployment uçtan uca regression [TAMAMLANDI — 06.09.2026]
         10.10 Deployment loglarını ve RAM kullanımını doğrula [PLAN]
         10.11 README / PROJECT_MEMORY / CHANGE_HISTORY / teknik manuel kaynaklarını güncelle [DEVAM EDİYOR]

```

### AŞAMA 10.3 — VM runner hazırlığı

Azure VM üzerinde self-hosted runner başarıyla kaydedildi ve systemd servisi olarak çalıştırıldı.

- Runner adı: `hattrick-vm`
- Runner dizini: `/home/azureuser/actions-runner`
- Runner sürümü: `2.337.0`
- GitHub bağlantısı: `Connected to GitHub`
- Runner durumu: `Listening for Jobs`
- Systemd servisi: `actions.runner.dincer552-ho-ai.hattrick-vm.service`
- Servis durumu: `active (running)` ve `enabled`
- Docker yetkisi: `azureuser` kullanıcısı Docker API'ye erişebiliyor.
- VM'de Docker build/test yapılmayacak.
- Runner sonraki aşamada yalnızca hazır GHCR image'ını çekip container deploy etmek için kullanılacak.

Runner servisinde `svc.sh` bulunmadığı için mevcut `bin/actions.runner.service.template` kullanılarak systemd servisi oluşturuldu. `runsvc.sh` systemd altında `/bin/bash` üzerinden çalıştırıldı.

### AŞAMA 10.7 — Secret aktarım güvenliği

Deploy job'ında CHPP consumer secret yalnızca `deploy` job'ının environment'ına GitHub Actions secret olarak aktarılıyor ve Docker container başlatılırken kullanılıyor. Secret değeri log mesajlarına yazdırılmıyor.

GHCR erişiminde job-scoped `GITHUB_TOKEN` kullanılıyor. Image pull tamamlandıktan hemen sonra `docker logout ghcr.io` çalıştırılıyor ve shell environment içindeki GHCR credential değişkenleri temizleniyor. Böylece runner üzerinde GHCR Docker credential'ının kalıcı olarak tutulması engelleniyor.

CHPP secret de container başlatıldıktan hemen sonra shell environment'ından `unset` ediliyor. Deployment scriptindeki hata trap'i de başarısızlık halinde GHCR logout ve secret cleanup işlemini çalıştırıyor.

Bu değişiklik mevcut analiz davranışını değiştirmez; yalnızca deployment sırasında credential yaşam süresini ve runner üzerindeki kalıcı credential izini azaltır.

Her aşama tamamlandığında bu bölüm güncellenecek ve hazırlanan PDF bölümleri manuel içerisine eklenecek.

Dokümantasyon prensibi:
- Tahmin edilen veya varsayılan hesap yazılmayacak.
- Kullanılan katsayılar sadece kod/config veya kaynak PDF'den alınacak.
- Her motor için input, output, hesaplama mantığı ve kullanılan dosya yolu belirtilecek.
- Stage 9 JSON kayıtları yeni karar mantığı eklemez; yalnızca mevcut gerçek üretim çıktısını saklar.
- Stage 10 deployment değişikliklerinde mevcut çalışan V5 analiz davranışı korunacak; yalnızca build/image/deployment taşıma mimarisi değiştirilecek.

### Motor / Kod ilişkilendirme standardı

Her motor dokümanında aşağıdaki bilgiler bulunacak:
- GitHub dosya yolu
- Class adı
- Ana çalışan fonksiyonlar
- Aldığı veri (input)
- Ürettiği veri (output)
- Hesaplama mantığı
- Kullandığı katsayılar ve kaynakları
- Önceki ve sonraki motor bağlantısı

---

## TEKNİK MANUEL İÇİNDEKİLER / KAYNAK HARİTASI

Birleşik PDF tek başına kaynak değildir. Aşağıdaki `.md` dosyaları yaşayan teknik kaynaklardır; PDF ise bu kaynakların belirli tarihte alınmış sabit bir yayım/snapshot sürümüdür.

1. `PROJECT_MEMORY.md`
2. `ENGINE_MAP.md`
3. `CHANGE_HISTORY.md`
4. `SYSTEM_ARCHITECTURE.md`
5. `DATA_MODEL.md`
6. `MATCH_ENGINE_MATH.md`
7. `MOTOR_TECHNICAL_MANUAL.md`
8. `REAL_MATCH_ANALYSIS.md`
9. `WEB_USER_MANUAL.md`
10. `WEB_INTERFACE.md`
11. `WEB_UI_FILE_MAP.md`
12. `DEVELOPER_API_MANUAL.md`
13. `M8_PHASE_D_PDF_CALIBRATION.md`
14. `MOTOR_OUTPUT_JSON_SCHEMA.md`

### PDF kaynak snapshot kaydı

- **A8 temel PDF snapshot tarihi:** 05.09.2026
- **A8 temel PDF:** `HattrickAI_V5_Teknik_Manuel_A8_FINAL.pdf`
- **A8 temel PDF sayfa sayısı:** 208
- **A9 Stage 9 snapshot tarihi:** 06.09.2026
- **A9 Stage 9 snapshot:** `HattrickAI_V5_Teknik_Manuel_A9_SNAPSHOT_2026-09-06.pdf`
- **Kaynak indexi:** `HattrickAI_V5/Docs/TECHNICAL_MANUAL_INDEX.md`

A9 snapshotı A8 temel manuelin yerine geçmez; 06.09.2026 itibarıyla eklenen Stage 9 JSON archive/API/panel/regression/determinism dokümantasyonunu tarihli yayın eki olarak kaydeder.

---

## DOĞRULANMIŞ TEKNİK NOTLAR — 05.09.2026

### Taktik hesaplama / seçim durumu

Kod incelemesi sonucunda mevcut web production analiz akışında takım taktiğini seçen ayrı bir motor/selector bulunmadığı doğrulandı.

`HattrickAI V5/Core/AnalysisService.cs` içinde `RatingContext` oluşturulurken `TeamTactic.Normal` veriliyor.

`HattrickAI_V5/Core/MotorPipelineService.cs` bu değeri M7/M7.2/M8 hesaplarına taşıyor.

`AdvancedTacticalScenarioEngine` verilen taktiğin etkilerini hesaplıyor; taktik seçmiyor. `M8ChanceAllocationEngine` verilen taktiğe göre dönüşüm ve şans dağılımı hesaplıyor; taktik seçmiyor. `M10FinalDecisionEngine` `TeamAttitude` yaklaşımı seçebiliyor; `TeamAttitude`, `TeamTactic` değildir.

Sonuç: UI'da `ORTADAN ATAK`, `KANATTAN ATAK` vb. değerleri motorun hesapladığı final taktikmiş gibi göstermek doğru değildir. Mevcut web path için gerçek durum `TAKTİK YOK` (input değeri `TeamTactic.Normal`) olarak dokümante edilmelidir.

## SON İŞLEMLER — 06.09.2026

- **06.09.2026 — AŞAMA 10.7:** GHCR deploy job'ında secret yaşam süresi azaltıldı. VM'de GHCR login sonrası `docker logout ghcr.io` çalıştırılıyor; GHCR credential environment değişkenleri temizleniyor. CHPP consumer secret yalnızca container başlatma adımında kullanılıyor ve ardından shell environment'ından temizleniyor. Hata durumunda da cleanup çalışıyor.
- **06.09.2026 — AŞAMA 10.6:** Workflow deploy job'ı self-hosted `hattrick-vm` runner'a taşındı. Deploy artık SSH/scp kullanmadan GHCR'dan `${GITHUB_SHA}` image'ını çekip `hattrick-v5` container'ını yeniden başlatacak şekilde uygulanıyor; C20 + build/push GitHub-hosted runner'da kalıyor. 06.09.2026 tarihli uçtan uca run'da C20, Docker build, GHCR push, VM image pull, container restart ve `/health` kontrolü başarıyla geçti.
- **06.09.2026 — AŞAMA 10.4:** Azure VM self-hosted runner systemd servisi `active (running)` ve `enabled` olarak doğrulandı. `azureuser` Docker grubuna eklendi ve `sudo -u azureuser docker ps` başarıyla çalıştı; Docker socket erişimi doğrulandı.
- **06.09.2026 — AŞAMA 10.3:** Azure VM self-hosted runner başarıyla kaydedildi ve servis olarak çalıştırıldı. Runner `hattrick-vm`, sürüm `2.337.0`, `/home/azureuser/actions-runner` altında çalışıyor.
- **06.09.2026 — AŞAMA 10.2:** GHCR image yayınlama akışı eklendi. C20 + Docker build sonrası image `ghcr.io/dincer552/ho-ai:<commit SHA>` ve `:v5` etiketleriyle yayınlanacak şekilde workflow düzenlendi.
- **06.09.2026 — AŞAMA 10.1:** Deployment mimarisi SSH'siz olarak sabitlendi: GitHub Actions → GHCR → Azure VM. Eski SSH/ssh-keyscan/scp deployment hattı workflow'dan çıkarıldı.
- **06.09.2026 — AŞAMA 10 PLANI:** Düşük RAM'li Azure VM üzerinde Docker build/test çalıştırmadan, SSH/ssh-keyscan/scp bağımlılığını kaldıran GHCR + VM self-hosted runner deployment mimarisinin aşamalı uygulanması planlandı.
- **06.09.2026 — AŞAMA 9.9:** Stage 9 için tarihli A9 publication supplement oluşturuldu; A8 208 sayfalık temel PDF'nin 05.09.2026 snapshotı korunarak yeni JSON archive dokümantasyonu ayrı yayın eki olarak kaydedildi. `TECHNICAL_MANUAL_INDEX.md` yeni snapshot ve kaynak tarihleriyle güncellendi.
- **06.09.2026 — AŞAMA 9.8:** C20 deterministic JSON regression eklendi. Aynı archive payload'ının `savedAt` ve `runId` metadata alanları hariç değişmediği C20 ile doğrulandı; C20 workflow acceptance gate'e bağlandı.
- **06.09.2026 — AŞAMA 9.7:** C19 Motor DB JSON regression geçti: archive writer + schema + latest/list/run lookup doğrulandı. C19 workflow sonrası Docker build ve Azure deployment da başarıyla tamamlandı.
- **06.09.2026 — AŞAMA 9.5:** Motor Panel JSON düğmesi gerçek Motor DB `latest` snapshot'ına bağlandı; archive snapshot yokken düğme pasif, analiz sırasında pasif ve eski client-side JSON paketi kaldırıldı.
- **06.09.2026 — AŞAMA 9.4:** Motor DB API tamamlandı: latest/list/runId erişimleri eklendi; liste endpoint'i yalnızca arşiv metadata'sını döndürüyor ve `latest.json` kaydını tekrarlamıyor.
- **06.09.2026 — AŞAMA 9.3:** Başarılı `/api/v5/analysis` çalışmasının gerçek `Analysis` + `MotorPipelineResult` + `MotorRunLog` snapshot'ı backend archive'a bağlandı.
- **06.09.2026 — AŞAMA 9.2:** `MotorResultArchive` gerçek pipeline çıktısını saklayacak şekilde düzenlendi; configurable `MOTOR_DB_PATH`, run bazlı JSON ve `latest.json` oluşturuldu; `/api/v5/motor-database/latest` eklendi.
- **06.09.2026 — AŞAMA 9.1:** `MOTOR_OUTPUT_JSON_SCHEMA.md` oluşturuldu. JSON snapshot'ın gerçek `MotorPipelineResult`, `Analysis` ve `MotorRunLog` verilerini taşıyan resmi sözleşmesi belirlendi.
- **06.09.2026 — AŞAMA 9 BAŞLANGICI:** Motor çıktı JSON veritabanı planı başlatıldı. Amaç, mevcut web analizinde motorların gerçekten ürettiği sonuçları değişiklik yapılmadan JSON snapshot olarak saklamak ve sonraki motor/taktik geliştirmelerinde karşılaştırılabilir bir veri havuzu oluşturmaktır.
- **05.09.2026 — PRODUCTION DEPLOY:** V5 Docker build ve Azure deployment başarıyla tamamlandı; deployment health check doğrulandı.
- **05.09.2026 — REGRESSION TESTLERİ:** C1–C18 offline acceptance/regression çalıştırması şimdilik durduruldu. Deployment artık regression gate'e bağlı olmadan devam ediyor.
- **05.09.2026 — AŞAMA 8:** 208 sayfalık birleşik teknik manuel PDF oluşturuldu.

## DOKÜMANTASYON DOSYALARI

- `HattrickAI_V5/Docs/PROJECT_MEMORY.md`
- `HattrickAI_V5/Docs/ENGINE_MAP.md`
- `HattrickAI_V5/Docs/CHANGE_HISTORY.md`
- `HattrickAI_V5/Docs/SYSTEM_ARCHITECTURE.md`
- `HattrickAI_V5/Docs/DATA_MODEL.md`
- `HattrickAI_V5/Docs/MATCH_ENGINE_MATH.md`
- `HattrickAI_V5/Docs/MOTOR_TECHNICAL_MANUAL.md`
- `HattrickAI_V5/Docs/REAL_MATCH_ANALYSIS.md`
- `HattrickAI_V5/Docs/WEB_USER_MANUAL.md`
- `HattrickAI_V5/Docs/WEB_INTERFACE.md`
- `HattrickAI_V5/Docs/WEB_UI_FILE_MAP.md`
- `HattrickAI_V5/Docs/DEVELOPER_API_MANUAL.md`
- `HattrickAI_V5/Docs/M8_PHASE_D_PDF_CALIBRATION.md`
- `HattrickAI_V5/Docs/TECHNICAL_MANUAL_INDEX.md`
- `HattrickAI_V5/Docs/MOTOR_OUTPUT_JSON_SCHEMA.md`

## TAKTİK ARAMA UZAYI — 07.09.2026

Önceki uygulamada taktik karşılaştırması yalnızca her formasyon için tek bir temsilci DB2 XI üzerinde yapılıyordu. Bu yaklaşım taktiği gerçek oyuncu yerleşiminden kopardığı için karar uzayını gereksiz şekilde daraltıyordu.

Yeni kural: taktik değerlendirme uzayı **XI × TeamTactic** olarak ele alınır. M6-B ile oluşan DB2 içindeki her geçerli XI, desteklenen tüm `TeamTactic` değerleriyle ayrı ayrı M7 → M7.2 → M8 → M9 zincirinden geçirilir. Böylece aynı dizilişin Normal, Pressing, CounterAttack, AttackMiddle, AttackWings, Creative ve LongShots senaryoları gerçek oyuncu yerleşimiyle birlikte değerlendirilir.

Taktik seçiminde kullanılan mevcut M11 final-score mantığı korunur; yeni katman aday uzayını genişletir ve aynı karar mantığını XI+taktik çiftleri üzerinde uygular. **M10/M11 eşik mekanizması değiştirilmez.**

Taktiklerin mekanik anlamları sonuç yorumunda korunmalıdır: Pressing iki tarafın potansiyel normal şanslarını azaltan; CounterAttack orta sahadan feragat edip kontra fırsatı arayan; AttackMiddle kanat hücumlarını merkeze kaydıran; AttackWings merkez hücumlarını kanatlara kaydıran; Creative özel olay üretimini artırmaya odaklanan; LongShots normal hücumların bir kısmını uzun şutlara dönüştüren mekanizmalardır. Bu nedenle yalnızca ham `TacticalScore` değil, taktiğin ürettiği M7.2/M8/M9 çıktıları birlikte değerlendirilmelidir.

Bu genişletme önceki `formation × 7 tactic` teşhis karşılaştırmasının yerine gerçek **DB2 XI × 7 tactic** aramasını koyar. Taktik sonuçları ayrıca Motor DB içinde saklanmaya devam eder.
