# Hattrick Rating Motor — Gerçek Hattrick Rating Calibration / Offline Regression Plan

> **PLAN-01 — 12.09.2026**  
> Amaç: V5 rating motorunu, CHPP ile alınan gerçek oyuncu verileri ve Hattrick maç ekranında görülen gerçek sektör ratinglerini **ground truth** kabul ederek ölçmek ve adım adım kalibre etmek.

## 0. Ana hedef

Bu çalışma bir maç sonucu tahmini çalışması değildir. Öncelikli hedef, bizim rating motorumuzun aynı kadro + aynı pozisyon + aynı oyuncu durumları verildiğinde **Hattrick'in gerçek rating çıktısını mümkün olduğunca yeniden üretmesidir**.

Kanonik ilişki:

```text
Gerçek Hattrick kadrosu / oyuncu verileri
        +
Gerçek Hattrick maç dizilişi ve oyuncu pozisyonları
        +
Gerçek Hattrick ekran ratingleri
        ↓
GROUND TRUTH

Aynı girdiler
        ↓
HattrickAI V5 rating motoru
        ↓
PREDICTION

PREDICTION - GROUND TRUTH
        ↓
REGRESSION ERROR
        ↓
KATSAYI / FORMÜL KALİBRASYONU
```

## 1. Test girdileri

Her gerçek Hattrick testinden mümkün olduğunca şu bilgiler kaydedilecek:

- Takım ID / maç ID
- Rakip ve maç bağlamı
- Gerçek formasyon: ör. `3-5-2`, `5-5-0`, `3-4-3`, `2-5-3` veya özel/ekstrem varyant
- Sahadaki 11 oyuncunun gerçek PlayerID'leri
- Her oyuncunun gerçek CHPP skill değerleri
- Form
- Stamina
- Experience
- Loyalty
- Specialty
- Set Pieces / ilgili diğer alanlar
- Pozisyon ve varsa oyuncu davranışı
- Taktik
- Takım davranışı / attitude, veri mevcutsa
- Hattrick ekranında görünen gerçek sektör ratingleri

Oyuncu kaynağı CHPP JSON snapshot'ı olacak. Snapshot'ın güvenlik sözleşmesine göre OAuth token, credential ve session cookie JSON'a dahil edilmeyecek.

## 2. Ground truth standardı

Hattrick maç dizilişi ekranında görülen sektör ratingleri **ground truth** kabul edilir.

Örneğin bir testte ekran:

```text
GK      13.00
DEF-L   12.75
DEF-C   13.25
DEF-R    7.00
ATT-L   15.75
ATT-C   13.75
ATT-R   13.50
```

gösteriyorsa bu yedi değer regression hedefidir.

Ekrandaki ratinglerin hangi sektör sırasına karşılık geldiği test sırasında görüntüyle doğrulanacak; varsayım yapılmayacak.

## 3. Aynı girdinin V5 motoruna verilmesi

Regression koşusunda gerçek Hattrick kadrosu yeniden seçilmeyecek. Kullanıcının verdiği gerçek XI ve gerçek pozisyonlar doğrudan fixture olarak kullanılacak.

Amaç:

```text
Hattrick input == V5 input
```

olmasını sağlamaktır.

Formation candidate engine'ın başka bir formasyon seçmesi bu calibration testini değiştirmemeli. Calibration, **verilmiş gerçek dizilişin rating üretimini** ölçer.

## 4. Ölçülecek metrikler

Her testte en az:

- Her sektör için `prediction`
- Her sektör için `groundTruth`
- Her sektör için `error = prediction - groundTruth`
- `absoluteError`
- Ortalama mutlak hata (MAE)
- Signed bias / ortalama hata
- Mümkünse RMSE
- Maksimum sektör sapması
- Testin formation/tactic bazında özeti

çıkarılacak.

Örnek:

| Sektör | Hattrick | V5 | Hata | Mutlak hata |
|---|---:|---:|---:|---:|
| GK | 13.00 | 13.40 | +0.40 | 0.40 |
| DEF-L | 12.75 | 14.10 | +1.35 | 1.35 |
| DEF-C | 13.25 | 13.80 | +0.55 | 0.55 |
| DEF-R | 7.00 | 10.20 | +3.20 | 3.20 |
| ATT-L | 15.75 | 15.10 | -0.65 | 0.65 |
| ATT-C | 13.75 | 14.30 | +0.55 | 0.55 |
| ATT-R | 13.50 | 13.90 | +0.40 | 0.40 |

Bu tablo örnektir; değerler gerçek regression çıktısından üretilecek.

## 5. Revizyon kuralı

Her testten sonra yalnızca **gerekli ve kanıtlanan** bölümler revize edilecek.

Sıra:

1. Regression çalıştır.
2. Hangi sektör/katmanın sistematik saptığını belirle.
3. Sapmanın muhtemel kaynağını kod/formül seviyesinde bul.
4. En küçük gerekli revizyonu yap.
5. Aynı fixture üzerinde regression tekrar çalıştır.
6. Önceki testlerin tamamında regresyon kontrolü yap.
7. İyileşme varsa yeni katsayı/formül kabul edilir.
8. Kötüleşme varsa değişiklik geri alınır veya revize edilir.

**Tek bir screenshot'a bakıp katsayı değiştirmek yasaktır.** Birden fazla test aynı yönde bias göstermeden global katsayı değişikliği yapılmayacak.

## 6. Overfitting koruması

İlk testler calibration sample olarak kullanılacak. Yeni sample'lar geldikçe:

- Daha önce görülmeyen formasyonlar eklenecek.
- Farklı oyuncu skill dağılımları eklenecek.
- Farklı form/stamina seviyeleri eklenecek.
- Mümkünse farklı taktikler eklenecek.
- Aynı değişiklik hem eski hem yeni fixture'larda kontrol edilecek.

Amaç tek maçın ratinglerini ezberlemek değil, V5 rating modelinin Hattrick çıktısını farklı kadro ve dizilişlerde genelleyebilmesini sağlamak.

## 7. Formasyon coverage

Calibration dataset'i özellikle şu örnekleri kapsayacak:

- `3-5-2`
- `3-4-3`
- `4-4-2`
- `4-5-1`
- `5-3-2`
- `5-5-0`
- `2-5-3`
- Kullanıcının göndereceği özel / ekstrem dizilişler

`2-5-3` için mevcut V5 sözleşmesindeki **iki bekli varyant** ayrıca korunacak; gerçek Hattrick screenshot'ında kullanılan fiziksel slotlar fixture'a aynen yazılacak.

Özel dizilişlerde isimden ziyade gerçek fiziksel slotlar ground truth olacaktır.

## 8. Oyuncu → sektör katkısı analizi

Sadece takım sektör toplamına bakılmayacak. Sapma bulunduğunda oyuncu katkı zinciri de incelenecek.

Örneğin:

```text
DEF-R yüksek sapıyor
        ↓
DEF-R oyuncusunun skillleri
        ↓
pozisyon katsayıları
        ↓
winger / playmaking / passing yan katkıları
        ↓
form / stamina / experience etkisi
        ↓
sektör aggregation
```

Böylece gerçek hata `DEF-R coefficient`, `wingback contribution`, `form modifier` veya aggregation katmanından hangisinden geliyorsa ayrıştırılacak.

## 9. Hattrick rating ile V5 rating ayrımı

Hattrick'in yayınlanmamış/hidden engine formülleri tahmin edilip resmi formül gibi yazılmayacak.

İki katman ayrı tutulacak:

- **Observed / Ground Truth:** Hattrick ekranında gerçekten gözlenen rating.
- **V5 Model:** bizim açıkça tanımladığımız ve regression ile kalibre ettiğimiz model.

Regression sonucu bir katsayı için güçlü ampirik kanıt sağlasa bile bu, Hattrick'in resmi iç formülü olarak sunulmayacak.

## 10. Fixture formatı

Her calibration sample mümkün olduğunca makine tarafından tekrar çalıştırılabilir fixture olarak saklanacak.

Önerilen yapı:

```json
{
  "schema": "hattrickai-v5-rating-calibration-v1",
  "sampleId": "CAL-001",
  "matchId": 769648184,
  "formation": "3-4-3",
  "tactic": "Normal",
  "players": [],
  "positions": [],
  "groundTruth": {
    "goalkeeper": 13.0,
    "defenceLeft": 12.75,
    "defenceCentre": 13.25,
    "defenceRight": 7.0,
    "attackLeft": 15.75,
    "attackCentre": 13.75,
    "attackRight": 13.5
  },
  "source": "Hattrick match-order screenshot + CHPP player snapshot"
}
```

Fixture'da screenshot'tan çıkarılan değerler ile CHPP JSON'daki oyuncu verileri birlikte tutulabilir; credential/token/session bilgisi tutulamaz.

## 11. Regression runner

Yeni bir offline regression runner oluşturulacak veya mevcut regression altyapısına calibration stage eklenecek.

Runner şunları yapmalı:

1. Fixture'ı oku.
2. Gerçek XI'ı oluştur.
3. Verilen gerçek pozisyonları uygula.
4. V5 rating motorunu çalıştır.
5. Ground truth ile karşılaştır.
6. Per-sector error üret.
7. MAE / bias / RMSE üret.
8. Önceki baseline ile karşılaştır.
9. Deterministic tekrar kontrolü yap.
10. Sonucu CI'da raporla.

## 12. Calibration acceptance kriteri

Bir revizyon ancak:

- hedef sektörde hatayı azaltıyorsa,
- toplam MAE'yi gereksiz artırmıyorsa,
- diğer fixture'ları bozmadığı gösteriliyorsa,
- deterministic sonucu koruyorsa,
- hidden-engine formülü iddiası oluşturmuyorsa

kabul edilecek.

İlk aşamada mutlak eşleşme zorunluluğu yerine **ölçülebilir hata azaltımı** hedeflenecek. Dataset büyüdükçe daha sıkı eşikler tanımlanacak.

## 13. Test akışı — kullanıcıdan gelecek screenshot'lar

Kullanıcı her test için mümkünse:

1. Hattrick maç dizilişi ekran görüntüsünü gönderir.
2. Gerçek formasyonu gösterir.
3. Ekrandaki ratingleri gösterir.
4. Gerekirse özel diziliş / oyuncu davranışlarını belirtir.
5. CHPP takım-oyuncu JSON snapshot'ı mevcutsa aynı dataset kullanılacak.

Biz:

```text
Screenshot
   ↓
Ground-truth extraction
   ↓
CHPP player mapping
   ↓
Offline V5 regression
   ↓
Error report
   ↓
Root-cause analysis
   ↓
Gerekliyse küçük kod/katsayı revizyonu
   ↓
Tüm regression tekrar
```

## 14. İlk calibration sample

İlk örnek olarak kullanıcının verdiği `3-4-3` gerçek Hattrick ekranı kullanılacak.

Gerçek XI:

- Enzo Bultot — GK
- Abeiku Takyi — DEF-L
- Dawid Nocoń — DEF-C
- Cristian Pesalovo — DEF-R
- Felix Gustavsson — W-L
- Bertalan Doktor — IM-L
- Milen Bozev — IM-R
- Manuel Gobiet — W-R
- Ersin Akşın — FW-L
- Adrian Beţa — FW-C
- Andres Nahasepp — FW-R

Ground truth ekran değerleri:

```text
13.00
12.75
13.25
7.00
15.75
13.75
13.50
```

Bu sample üzerinde V5 motorunun aynı XI ve pozisyonlarla ürettiği değerler çıkarılmadan katsayı değiştirilmeyecek.

CHPP snapshot'ı takım için 31 oyuncu içeriyor; calibration sırasında gerçek XI dışındaki oyuncular rating hesabına yanlışlıkla girmemeli. Teknik direktör `Antonín Vašica` da oyuncu havuzundan analiz girdisi olarak kullanılmamalı.

## 15. Versiyonlama / audit

Her calibration değişikliği:

- commit SHA
- önceki baseline
- yeni baseline
- kullanılan fixture sayısı
- MAE değişimi
- sektör bazlı değişim
- değiştirilen dosya/katsayı
- regression sonucu

ile kayıt altına alınacak.

Amaç birkaç test sonra "hangi katsayıyı neden değiştirmiştik?" sorusunun cevapsız kalmamasıdır.

## 16. İlk iş sırası

**CAL-001:** Kullanıcının mevcut `3-4-3` screenshot + CHPP oyuncu JSON'u ile gerçek V5 ratinglerini çalıştır.

**CAL-002+:** Kullanıcının göndereceği `3-5-2`, `5-5-0`, `2-5-3` ve diğer gerçek/özel diziliş screenshot'larını fixture'a çevir.

Her yeni fixture önce **değişikliksiz baseline** üzerinde çalıştırılacak. Sonra gerekli görülen katsayı/formül revize edilecek ve bütün fixture seti yeniden koşulacak.

### Çalışma prensibi

> **Önce ölç → sonra nedenini bul → en küçük değişikliği yap → bütün testleri tekrar koş.**

Bu plan tamamlanmadan rating motorunda toplu katsayı değişikliği yapılmayacak.
