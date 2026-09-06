# HattrickAI V5

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
         10.6 VM runner üzerinden yalnızca image pull + container restart/deploy çalıştır [PLAN]
         10.7 CHPP secret aktarımını güvenli şekilde koru [PLAN]
         10.8 Health check ve başarısız deployment rollback davranışını doğrula [PLAN]
         10.9 C20 + Docker build + GHCR + VM deployment uçtan uca regression [PLAN]
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

- **06.09.2026 — AŞAMA 10.4:** Azure VM self-hosted runner systemd servisi `active (running)` ve `enabled` olarak doğrulandı. `azureuser` Docker grubuna eklendi ve `sudo -u azureuser docker ps` başarıyla çalıştı; Docker socket erişimi doğrulandı.
- **06.09.2026 — AŞAMA 10.3:** Azure VM self-hosted runner başarıyla kaydedildi ve servis olarak çalıştırıldı. Runner `hattrick-vm`, sürüm `2.337.0`, `/home/azureuser/actions-runner` altında çalışıyor.
- **06.09.2026 — AŞAMA 10.2:** GHCR image yayınlama akışı eklendi. C20 + Docker build sonrası image `ghcr.io/dincer552/ho-ai:<commit SHA>` ve `:v5` etiketleriyle yayınlanacak şekilde workflow düzenlendi.
- **06.09.2026 — AŞAMA 10.1:** Deployment mimarisi SSH'siz olarak sabitlendi: GitHub Actions → GHCR → Azure VM. Eski SSH/ssh-keyscan/scp deployment hattı workflow'dan çıkarıldı.
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
