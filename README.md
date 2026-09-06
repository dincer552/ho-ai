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
AŞAMA 9  MOTOR ÇIKTI JSON VERİTABANI [DEVAM EDİYOR — 06.09.2026]
         9.1 JSON veri sözleşmesi [TAMAMLANDI — 06.09.2026]
         9.2 JSON archive backend [TAMAMLANDI — 06.09.2026]
             - MotorResultArchive gerçek MotorPipelineResult + Analysis + MotorRunLog ile bağlandı.
             - Snapshot: motorPipeline, DB1/DB2, metadata ve telemetry içeriyor.
             - MOTOR_DB_PATH ile kayıt yolu yapılandırılabilir.
             - latest.json ve run bazlı JSON kayıtları oluşturuluyor.
             - /api/v5/motor-database/latest endpoint'i eklendi.
         9.3 Backend analysis entegrasyonu [TAMAMLANDI — 06.09.2026]
             - Başarılı /api/v5/analysis sonucu archive'a kaydediliyor.
         9.4 Motor DB API [TAMAMLANDI — 06.09.2026]
             - /api/v5/motor-database/latest son snapshot'ı döndürüyor.
             - /api/v5/motor-database/list arşivdeki run kayıtlarının metadata listesini döndürüyor.
             - /api/v5/motor-database/{runId} ilgili run'ın tam JSON snapshot'ını döndürüyor.
             - latest.json listesinde tekrar kayıt oluşmaması için list endpoint'i latest.json'ı hariç tutuyor.
             - Geçersiz/bozuk JSON arşiv dosyaları listede yok sayılıyor.
         9.5 Motor Panel [TAMAMLANDI — 06.09.2026]
             - Paneldeki JSON indirme düğmesi gerçek Motor DB latest snapshot'ını indiriyor.
             - Archive snapshot yokken düğme pasif.
             - Analiz sırasında düğme pasif ve eski client-side JSON paketi oluşturulmuyor.
         9.6 Gerçek web JSON doğrulaması [PLAN]
         9.7 C19 JSON regression [TAMAMLANDI — 06.09.2026]
             - Archive writer, schema, latest/list/run lookup regression doğrulandı.
         9.8 Deterministic JSON kontrolü [TAMAMLANDI — 06.09.2026]
             - Aynı gerçek payload'ın iki archive yazımında metadata dışındaki JSON içeriğinin değişmediği C20 ile doğrulandı.
             - savedAt'ın her yazımda yeni metadata olarak kaldığı ayrıca doğrulandı.
             - C20 acceptance workflow gate'e bağlandı.
         9.9 Dokümantasyon / PDF snapshot güncellemesi [DEVAM EDİYOR — 06.09.2026]
             - Stage 9 değişiklikleri teknik kaynaklarda kayda alınıyor.
             - Mevcut A8 PDF snapshot'ı 05.09.2026 tarihli olduğu için yeni Stage 9 kaynaklarıyla yeniden oluşturulması gerekiyor.
             - PDF yeniden oluşturulduktan sonra TECHNICAL_MANUAL_INDEX.md snapshot tarihi ve yayın kaydı güncellenecek.
```

Her aşama tamamlandığında bu bölüm güncellenecek ve hazırlanan PDF bölümleri manuel içerisine eklenecek.

Dokümantasyon prensibi:
- Tahmin edilen veya varsayılan hesap yazılmayacak.
- Kullanılan katsayılar sadece kod/config veya kaynak PDF'den alınacak.
- Her motor için input, output, hesaplama mantığı ve kullanılan dosya yolu belirtilecek.
- Stage 9 JSON kayıtları yeni karar mantığı eklemez; yalnızca mevcut gerçek üretim çıktısını saklar.

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

- **PDF snapshot tarihi:** 05.09.2026
- **PDF:** `HattrickAI_V5_Teknik_Manuel_A8_FINAL.pdf`
- **PDF sayfa sayısı:** 208
- **Kaynak indexi:** `HattrickAI_V5/Docs/TECHNICAL_MANUAL_INDEX.md`

PDF yeniden oluşturulduğunda snapshot tarihi güncellenecek ve `TECHNICAL_MANUAL_INDEX.md` içindeki kaynak tarihleri yeniden kaydedilecek.

---

## DOĞRULANMIŞ TEKNİK NOTLAR — 05.09.2026

### Taktik hesaplama / seçim durumu

Kod incelemesi sonucunda mevcut web production analiz akışında takım taktiğini seçen ayrı bir motor/selector bulunmadığı doğrulandı.

`HattrickAI_V5/Core/AnalysisService.cs` içinde `RatingContext` oluşturulurken `TeamTactic.Normal` veriliyor.

`HattrickAI_V5/Core/MotorPipelineService.cs` bu değeri M7/M7.2/M8 hesaplarına taşıyor.

`AdvancedTacticalScenarioEngine` verilen taktiğin etkilerini hesaplıyor; taktik seçmiyor. `M8ChanceAllocationEngine` verilen taktiğe göre dönüşüm ve şans dağılımı hesaplıyor; taktik seçmiyor. `M10FinalDecisionEngine` `TeamAttitude` yaklaşımı seçebiliyor; `TeamAttitude`, `TeamTactic` değildir.

Sonuç: UI'da `ORTADAN ATAK`, `KANATTAN ATAK` vb. değerleri motorun hesapladığı final taktikmiş gibi göstermek doğru değildir. Mevcut web path için gerçek durum `TAKTİK YOK` (input değeri `TeamTactic.Normal`) olarak dokümante edilmelidir.

## SON İŞLEMLER — 06.09.2026

- **06.09.2026 — AŞAMA 9.9:** Dokümantasyon/PDF snapshot güncellemesi başlatıldı. Stage 9 JSON değişiklikleri kaynak Markdown'lara işlendi; mevcut A8 PDF'nin 05.09.2026 snapshot'ı olduğu ve yeni PDF üretimi gerektiği kayıt altına alındı.
- **06.09.2026 — AŞAMA 9.8:** C20 deterministic JSON regression eklendi. Aynı archive payload'ının `savedAt` ve `runId` metadata alanları hariç değişmediği doğrulanıyor; C20 workflow acceptance gate'e bağlandı.
- **06.09.2026 — AŞAMA 9.7:** C19 Motor DB JSON regression geçti: archive writer + schema + latest/list/run lookup doğrulandı. C19 workflow sonrası Docker build ve Azure deployment da başarıyla tamamlandı.
- **06.09.2026 — AŞAMA 9.5:** Motor Panel JSON düğmesi gerçek Motor DB `latest` snapshot'ına bağlandı; archive snapshot yokken düğme pasif, analiz sırasında pasif ve eski client-side JSON paketi kaldırıldı.
- **06.09.2026 — AŞAMA 9.4:** Motor DB API tamamlandı: latest/list/runId erişimleri eklendi; liste endpoint'i yalnızca arşiv metadata'sını döndürüyor ve `latest.json` kaydını tekrarlamıyor.
- **06.09.2026 — AŞAMA 9.3:** Başarılı `/api/v5/analysis` çalışmasının gerçek `Analysis` + `MotorPipelineResult` + `MotorRunLog` snapshot'ı backend archive'a bağlandı.
- **06.09.2026 — AŞAMA 9.2:** `MotorResultArchive` gerçek pipeline çıktısını saklayacak şekilde düzenlendi; configurable `MOTOR_DB_PATH`, run bazlı JSON ve `latest.json` oluşturuldu; `/api/v5/motor-database/latest` eklendi.
- **06.09.2026 — AŞAMA 9.1:** `MOTOR_OUTPUT_JSON_SCHEMA.md` oluşturuldu. JSON snapshot'ın gerçek `MotorPipelineResult`, `Analysis` ve `MotorRunLog` verilerini taşıyan resmi sözleşmesi belirlendi.
- **06.09.2026 — AŞAMA 9 BAŞLANGICI:** Motor çıktı JSON veritabanı planı başlatıldı. Amaç, mevcut web analizinde motorların gerçekten ürettiği sonuçları değişiklik yapılmadan JSON snapshot olarak saklamak ve sonraki motor/taktik geliştirmelerinde karşılaştırılabilir bir veri havuzu oluşturmaktır.
- **05.09.2026 — PRODUCTION DEPLOY:** V5 Docker build ve Azure deployment başarıyla tamamlandı; deployment health check doğrulandı.
- **05.09.2026 — REGRESSION TESTLERİ:** C1–C18 offline acceptance/regression çalıştırması şimdilik durduruldu. Deployment artık regression gate'e bağlı olmadan devam ediyor.
- **05.09.2026 — C12:** M6-B refinement acceptance doğrulandı: DB2=100, 6 formasyon, 6 bütçe, 23701 değerlendirme.
- **05.09.2026 — C13:** DB2 formation coverage düzeltildi; acceptance production DB2=100 içinden exposed DB2=90 kapsamını doğru kabul ediyor. 6 yasal formasyonun tamamı kapsanıyor.
- **05.09.2026 — C14:** M11 finalist pool ve telemetry doğrulaması düzeltildi; M11 finalist pool 90 aday / 6 formasyon olarak geçiliyor.
- **05.09.2026 — C15:** M11 final selection testindeki top-N ranking davranışı production davranışıyla hizalandı.
- **05.09.2026 — AŞAMA 5:** Gerçek CHPP offline fixture üzerinden maç örnek analizi `REAL_MATCH_ANALYSIS.md` içine işlendi.
- **05.09.2026 — AŞAMA 6:** Web arayüzü teknik dosyaları ve kullanıcı manueli tamamlandı.
- **05.09.2026 — AŞAMA 7:** Developer/API manueli tamamlandı.
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