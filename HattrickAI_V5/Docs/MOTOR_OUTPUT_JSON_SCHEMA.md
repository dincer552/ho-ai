# HattrickAI V5 — Motor Output JSON Schema

**Stage:** 9.1 — JSON veri sözleşmesi  
**Tarih:** 06.09.2026  
**Durum:** TAMAMLANDI

## 1. Amaç

Bu belge, HattrickAI V5 web analizinin gerçek motor çıktılarının JSON snapshot olarak saklanması için veri sözleşmesini tanımlar.

Bu JSON bir hesaplama motoru değildir. Mevcut `MotorPipelineResult`, gerçek `Analysis` çıktısı ve `MotorRunLog` verisini kaydetmek için kullanılan taşıma/snapshot sözleşmesidir.

## 2. Kaynak gerçeklik

Ana motor kaynağı:

- `HattrickAI_V5/Core/MotorPipelineService.cs`
- `HattrickAI_V5/Core/Models.cs`
- `HattrickAI_V5/Core/MotorRunLogStore.cs`

`MotorPipelineResult` mevcut üretim zincirinde M3, M4, M5, M6, M7, M7.2, M8, M9, M10, FinalPlan ve FinalPrediction değerlerini; ayrıca M11 ile DB1/DB2 alanlarını taşıyabilir.

JSON'a source kodda bulunmayan yeni motor sonucu eklenmeyecektir.

## 3. Üst seviye sözleşme

```json
{
  "schemaVersion": "hattrickai-v5-motor-database-v1",
  "savedAt": "ISO-8601",
  "build": "V5 build",
  "runId": "motor run id",
  "source": "HattrickAI V5 web analysis",
  "analysis": {},
  "motorPipeline": {},
  "candidateDatabases": {
    "db1": [],
    "db2": [],
    "db1Count": 0,
    "db2Count": 0
  },
  "motorLog": {}
}
```

## 4. Motor çıktıları

`motorPipeline` altında yalnızca gerçek `MotorPipelineResult` alanları temsil edilir:

- `m3`
- `m4`
- `m5`
- `m6`
- `m7`
- `m72`
- `m8`
- `m9`
- `m10`
- `m11` (production result içinde mevcutsa)
- `finalPlan`
- `finalPrediction`
- `selectedMatchApproach`
- `m6BFormationBudgets`

Alanların gerçek JSON isimlendirmesi ASP.NET Core'un mevcut camelCase serializer sözleşmesine tabidir.

## 5. Candidate DB sözleşmesi

DB1 ve DB2, pipeline'ın gerçek `CandidateEvaluationRecord` kayıtlarından gelir.

- DB1: M6-A sonrasında oluşturulan exposed candidate listesi.
- DB2: M6-B sonrasında oluşturulan exposed candidate listesi.
- `db1Count` ve `db2Count`: pipeline'ın gerçek database count değerleri.

DB kayıtları yeniden hesaplanmaz ve JSON snapshot oluşturulurken sıralama/puan değiştirilmez.

## 6. Run telemetry

`motorLog` gerçek `MotorRunLog` nesnesidir. Motor stage kayıtları motor adı, durum, mesaj, süre, iteration ve candidate count gibi mevcut telemetry alanlarını içerir.

Run ID, pipeline sonucu ile telemetry arasında ortak ilişkilendirme anahtarıdır.

## 7. Metadata

- `schemaVersion`: JSON veri sözleşmesinin sürümü.
- `savedAt`: snapshot'ın oluşturulduğu UTC zaman.
- `build`: analizi çalıştıran V5 build değeri.
- `runId`: analizin benzersiz run kimliği.
- `source`: snapshot'ın üretim kaynağı.

Metadata, motor hesabının sonucu değildir.

## 8. Taktik sınırı

Bu schema motor tarafından seçilmemiş bir taktiği seçilmiş gibi göstermeyecektir. Production web path'te `TeamTactic.Normal` input olarak kullanılmaktadır. `TeamTactic` değeri optimum taktik seçimi sonucu olarak etiketlenmeyecektir.

## 9. Sürümleme kuralı

Schema'da geriye dönük uyumsuz bir değişiklik olduğunda `schemaVersion` artırılır.

Uyumlu yeni alanlar aynı major schema sürümünde eklenebilir; ancak alanın kaynağı ve gerçek üretim davranışı dokümante edilmelidir.

## 10. Stage 9 veri disiplini

1. JSON yalnızca mevcut V5 üretim çıktısını kaydeder.
2. JSON snapshot oluşturmak için yeni karar mantığı çalıştırılmaz.
3. Kodda bulunmayan M3–M11 ara sonucu tahmin edilmez.
4. CHPP verisi ile motor tarafından türetilen veri birbirine karıştırılmaz.
5. `savedAt`, `build` ve `runId` gibi metadata alanları motor sonucu olarak yorumlanmaz.
6. Aynı run'ın telemetry ve pipeline çıktısı aynı `runId` ile ilişkilendirilir.

## 11. Sonraki aşama

Stage 9.1 tamamlandı. Sonraki adım Stage 9.2'dir: mevcut JSON archive sınıfını bu sözleşmeye bağlamak, kalıcı kayıt yolunu güvenli/configurable hale getirmek ve tamamlanan analiz sonucunu backend tarafından kaydetmek.
