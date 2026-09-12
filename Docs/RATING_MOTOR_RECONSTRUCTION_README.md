# HattrickAI V5 — Rating Motor Reconstruction & Calibration Plan

> **Amaç:** Hattrick'in gerçek 7 bölgesel takım ratingini, yalnızca ampirik katsayı oynayarak değil; Contribution referansı, mevcut V5 kodu, HO/Hattrick araştırması ve gerçek Hattrick ekran görüntülerinden doğrulanabilen matematiksel katmanları yeniden kurarak hesaplamak.
>
> **Durum:** CAL-001 baseline tamamlandı. Henüz rating katsayıları değiştirilmedi.
>
> **Kural:** Gerçek Hattrick sonucu gözlemlenen ground truth'tur. Contribution/HO formülleri güçlü referans ve hipotez kaynağıdır; resmi olarak açıklanmamış engine ayrıntıları kesin gerçek kabul edilmeyecektir.

---

## 1. Ground Truth

Her kalibrasyon örneğinde Hattrick maç dizilişi ekranından şu 7 değer kaydedilir:

1. Sol defans
2. Merkez defans
3. Sağ defans
4. Orta saha
5. Sol atak
6. Merkez atak
7. Sağ atak

Aynı ekran görüntüsündeki:
- diziliş,
- oyuncular,
- oyuncuların pozisyonları,
- oyuncu davranışları,
- taktik,
- takım davranışı,
- maç bağlamı

CHPP oyuncu snapshot'ı ile eşleştirilir.

### CAL-001

Mevcut gerçek Hattrick örneği:

- Diziliş: **3-4-3**
- Taktik: **Normal**
- Takım davranışı: **Normal**
- Oyuncular:
  - GK Enzo Bultot
  - DEF-L Abeiku Takyi
  - DEF-C Dawid Nocoń
  - DEF-R Cristian Pesalovo
  - W-L Felix Gustavsson
  - IM-L Bertalan Doktor
  - IM-R Milen Bozev
  - W-R Manuel Gobiet
  - FW-L Ersin Akşın
  - FW-C Adrian Beţa
  - FW-R Andres Nahasepp
- Hattrick ground truth:
  - Sol defans **13.00**
  - Merkez defans **12.75**
  - Sağ defans **13.25**
  - Orta saha **7.00**
  - Sol atak **15.75**
  - Merkez atak **13.75**
  - Sağ atak **13.50**

Coach Antonín Vašica (PlayerID 437860841) oyuncu havuzundan hariç tutulmalıdır.

---

## 2. Kaynaklar ve Güven Seviyesi

### 2.1 Contribution — Hattrick PDF

Bu belge oyuncu skill'lerinin pozisyona göre takımın 7 bölgesel ratingine katkısını tanımlayan ana matematiksel referanstır.

Temel yaklaşım:

```text
oyuncu skill × pozisyon/davranış katkı katsayısı
→ sektöre katkı
→ diğer oyuncu katkılarıyla toplama
→ takım ratingi
```

Belgedeki örnek:

```text
Kaleci merkez defans katkısı
= Goalkeeping × 0.165 + Defending × 0.079
```

Aynı belgede Goalkeeper, Central Defender, Wing Back, Inner Midfielder, Winger ve Forward için skill katkı tabloları bulunur. Offensive, Defensive, Towards Wing ve Towards Middle davranışları da ayrı katsayılarla ele alınır.

**Önemli:** Bu belge doğrudan güncel Hattrick kaynak kodu değildir. Katsayılar tarihsel/araştırılmış engine davranışının referansıdır. Bu yüzden gerçek screenshot sonuçlarıyla doğrulanacaktır.

### 2.2 HO / araştırılmış rating implementasyonu

Araştırmada bulunan matematiksel yaklaşım, Contribution tablosunun üstünde ek katmanlar bulunduğunu gösterir:

```text
Skill rating
→ form
→ loyalty
→ position/behaviour contribution
→ overcrowding
→ experience contribution
→ stamina / match-minute effects
→ team/context modifiers
→ sector scaling
→ non-linear rating conversion
```

Bu katmanların tamamı resmi Hattrick dokümantasyonu tarafından aynı ayrıntıda doğrulanmış değildir. Kodlanmadan önce her katman kaynak + gerçek test ile doğrulanmalıdır.

---

## 3. Contribution Katsayı Referansı

Aşağıdaki katsayılar mevcut V5 motorunun araştırılmış Contribution tablosundan taşınmış temel katsayılardır. **Kalibrasyon sırasında bunlar rastgele değiştirilmez.** Önce eksik hesap katmanı bulunur.

### Goalkeeper

Normal GK için temel defans katkıları:

```text
Central Defence:
  Keeper × 0.165
  Defending × 0.079

Left/Right Defence:
  Keeper × 0.183
  Defending × 0.082
```

### Central Defender

Normal davranış için temel katkılar:

```text
Central Defence: Defending × 0.186
Side Defence:    Defending × 0.077
Midfield:        Playmaking × 0.035
```

Davranış değişimleri:

```text
Offensive:    Central Defence × 0.130, Side Defence × 0.058, Midfield × 0.047
Towards Wing: Central Defence × 0.133, Side Defence × 0.217, Midfield × 0.023
```

Towards Wing stoper ayrıca kendi kanadına:

```text
Passing × 0.063 → side attack
```

### Wing Back

Normal:

```text
Central Defence: Defending × 0.083
Side Defence:    Defending × 0.268
Midfield:        Playmaking × 0.023
Side Attack:     Winger × 0.129
```

Davranış katsayıları:

```text
Defensive:      CD .089 / sideD .284 / MID .009 / sideA .082
Towards Middle: CD .126 / sideD .209 / MID .023 / sideA .072
Offensive:      CD .071 / sideD .175 / MID .032 / sideA .163
```

### Inner Midfielder

Normal:

```text
Central Defence: Defending × 0.070
Side Defence:    Defending × 0.028
Midfield:        Playmaking × 0.139
Side Passing:    Passing × 0.028
Central Attack:  Passing × 0.057 + Scoring × 0.038
```

Defensive:

```text
CD .115 / sideD .040 / MID .131 / sidePass .018 / centerPass .039 / centerScore .028
```

Offensive:

```text
CD .115 / sideD .040 / MID .131 / sidePass .018 / centerPass .039 / centerScore .025
```

Towards Wing:

```text
CD .059 / sideD .068 / MID .113 / sidePass .064 / centerPass .038 / sideWinger .117
```

### Winger

Normal:

```text
Central Defence: Defending × 0.037
Side Defence:    Defending × 0.104
Midfield:        Playmaking × 0.065
Side Attack:     Passing × 0.219 + Winger × 0.054
Central Attack:  Passing × 0.018
```

Defensive:

```text
CD .050 / sideD .148 / MID .054 / sidePass .185 / sideWinger .044 / centerPass .009
```

Towards Middle:

```text
CD .047 / sideD .093 / MID .082 / sidePass .160 / sideWinger .043 / centerPass .026
```

Offensive:

```text
CD .016 / sideD .055 / MID .054 / sidePass .247 / sideWinger .062 / centerPass .024
```

### Forward

Normal:

```text
Midfield:        Playmaking × 0.041
Side Attack:     Scoring × 0.058 + Passing × 0.048 + Winger × 0.032
Opposite Attack: Scoring × 0.058 + Passing × 0.048
Central Attack:  Scoring × 0.178 + Passing × 0.066
```

Towards Wing:

```text
Midfield:        Playmaking × 0.024
Side Attack:     Scoring × 0.093 + Passing × 0.101 + Winger × 0.044
Opposite Attack: Scoring × 0.018 + Passing × 0.034
Central Attack:  Passing × 0.102 + Scoring × 0.044
```

Defensive:

```text
Midfield:        Playmaking × 0.058
Side Attack:     Scoring × 0.030 + Passing × 0.033 + Winger × 0.059
Opposite Attack: Scoring × 0.030 + Passing × 0.033
Central Attack:  Scoring × 0.102 + Passing × 0.108
```

---

## 4. Mevcut V5'te Zaten Doğru Olanlar

`RegionalRatingEngine.cs` şu anda yukarıdaki Contribution katsayılarının büyük bölümünü doğrudan kullanıyor.

Mevcut kodda:

- skill × contribution katsayıları vardır,
- pozisyon ayrımı vardır,
- davranış ayrımı vardır,
- side-aware katkı vardır,
- 7 sektör ayrı hesaplanır,
- oyuncu katkıları toplanır.

Dolayısıyla ilk hedef katsayıları değiştirmek değildir.

---

## 5. Mevcut V5'te Şüpheli / Eksik Katmanlar

### 5.1 Raw contribution → displayed Hattrick rating dönüşümü

En önemli araştırma maddesi.

V5'te sektör katkıları hesaplanıyor ancak gerçek Hattrick ekranındaki rating ölçeğine dönüşümün ayrı ve doğrulanabilir bir katman olarak kurulması gerekiyor.

Test edilecek hipotez:

```text
raw sector contribution
→ sector-specific scale
→ non-linear conversion
→ displayed rating
```

Araştırılmış HO implementasyonunda yaklaşık olarak:

```text
rating ≈ ((raw × sectorScale)^1.2 / 4) + 1
```

şeklinde bir dönüşüm görülmüştür.

Araştırılmış scale referansları:

```text
Midfield:      0.312
Left/Right Def:0.834
Central Def:   0.501
Left/Right Att:0.615
Central Att:   0.513
```

**Bunlar henüz Hattrick 2026 resmi formülü olarak kabul edilmeyecek.** Önce CAL testleriyle doğrulanacaktır.

### 5.2 Skill normalization

Araştırılmış yaklaşımda temel skill rating:

```text
max(0, skill - 1)
```

olarak ele alınmaktadır.

V5 şu anda ham skill değerlerini contribution hesabına sokuyor. Bu fark ayrı bir A/B testiyle ölçülecek.

### 5.3 Form

V5'te form mevcut Contribution baseline'ına göre normalize ediliyor:

```text
FormFactor(current) / 0.755
```

Araştırılmış modelde form için ayrı bir fonksiyon kullanıldığı görülmektedir. Bu iki yaklaşım CAL örnekleriyle karşılaştırılacaktır.

### 5.4 Loyalty

V5'te loyalty yaklaşık olarak skill'e eklenen küçük bir değer şeklinde kullanılmaktadır:

```text
Loyalty × 0.05
```

Araştırılacak nokta: loyalty'nin gerçek engine'de skill-equivalent mi yoksa ayrı contribution katmanı mı olduğu.

### 5.5 Experience

Mevcut V5 experience delta'sını her skill'e eklemektedir.

Araştırılmış yaklaşımda experience'ın ayrı sektör katkısı olarak ele alınması daha güçlü bir hipotezdir.

Araştırılmış referans experience contribution katsayıları:

```text
Defence side:   × 0.345
Defence centre: × 0.480
Midfield:       × 0.730
Attack side:    × 0.375
Attack centre:  × 0.450
```

Bu değerler doğrudan uygulanmadan önce test edilecektir.

### 5.6 Overcrowding

Araştırılmış referanslar:

```text
Central defenders:
  2 → yaklaşık -3.6%
  3 → yaklaşık -10%

Inner midfielders:
  2 → yaklaşık -6.5%
  3 → yaklaşık -17.5%

Forwards:
  2 → yaklaşık -5.5%
  3 → yaklaşık -13.5%
```

V5 şu anda bazı crowding etkilerini özellikle playmaking tarafına uygular. Bunun yerine gerçek katkı katmanının tamamına uygulanıp uygulanmaması test edilecektir.

### 5.7 Stamina / match minute

V5'te maç dakikasına bağlı basit midfield decay bulunmaktadır.

Araştırılmış yaklaşımda stamina oyuncu katkısıyla daha yakından ilişkilidir. Başlangıç ratingi ile maç içi rating ayrılmalıdır.

### 5.8 Pozisyon / behaviour / side mapping

Bu katman özellikle CAL-001 için kritik.

3-4-3'teki:

```text
DEF-L / DEF-C / DEF-R
```

oyuncularının gerçekten `CentralDefender` olarak mı, yoksa fiziksel slot adına göre `WingBack` olarak mı hesaplandığı kesinleştirilecektir.

Aynı kontrol W-L/W-R, IM-L/IM-R ve FW-L/FW-R için yapılacaktır.

### 5.9 Team/context modifiers

Ayrı katmanlar halinde doğrulanacak:

- home/away
- team attitude / PIC / MOTS
- team spirit
- coach type
- confidence
- tactic
- weather
- match minute
- goal difference / lead retreat

Bunlar Contribution katsayılarına gömülmeyecek.

---

## 6. Hattrick Rating Reconstruction Pipeline

Yeni motor hedef mimarisi:

```text
CHPP Player Data
      ↓
Skill normalization
      ↓
Form / Loyalty / Experience state
      ↓
Position + Side + Behaviour
      ↓
Contribution Matrix
      ↓
Player contribution ledger
      ↓
Overcrowding / position interaction
      ↓
Experience contribution
      ↓
Stamina / minute (only when applicable)
      ↓
Team context
      ↓
Tactic / coach / confidence / spirit
      ↓
Sector raw values
      ↓
Sector scale
      ↓
Non-linear rating conversion
      ↓
7 displayed Hattrick sector ratings
```

Her aşama ayrı fonksiyon olacak ve test edilebilir olacak.

---

## 7. Aşama Aşama Revizyon Planı

### AŞAMA 0 — Baseline freeze

- CAL-001 mevcut motor ile kaydedildi.
- Katsayılara dokunma.
- Gerçek Hattrick değerlerini fixture olarak sakla.
- V5 mevcut sonuçlarını sakla.

### AŞAMA 1 — Position mapping audit

- 3-4-3 oyuncularının her slotunu kontrol et.
- `DEF-L/C/R` yanlışlıkla WingBack hesaplanıyor mu kontrol et.
- Winger/IM/Forward side mapping'i doğrula.
- Aynı oyuncu/aynı skill ile her pozisyonun katkısını unit test et.

**Başarı:** Pozisyon mapping'i Contribution modeliyle tutarlı.

### AŞAMA 2 — Raw → displayed rating dönüşümü

- V5 raw sektör değerlerini expose et.
- Contribution raw toplamını ayrı tut.
- Sector scale + non-linear conversion hipotezini izole test et.
- CAL-001'de yalnız bu katmanı değiştir.

**Başarı:** Hata bütün sektörlerde aynı yönde ölçek problemi olmaktan çıkar.

### AŞAMA 3 — Skill normalization

A/B:

```text
A = skill
B = max(0, skill - 1)
```

CAL-001 + yeni formation örnekleriyle karşılaştır.

### AŞAMA 4 — Form modeli

- Mevcut baseline normalize yaklaşımı.
- Araştırılmış form fonksiyonu.
- Gerçek Hattrick örnekleri.

Oyuncu form dağılımı değiştikçe hangi modelin daha iyi sonuç verdiğini ölç.

### AŞAMA 5 — Loyalty ve Experience

Experience'ı skill'lere eklemek yerine ayrı contribution katmanı olarak test et.

Aynı anda loyalty'yi ayrı test et.

### AŞAMA 6 — Overcrowding

2/3 central defender, 2/3 IM, 2/3 forward senaryolarını izole test et.

### AŞAMA 7 — Stamina / match minute

Başlangıç ratingi için stamina'nın rolünü ayır.
Maç içi dakika hesabını ayrı test et.

### AŞAMA 8 — Context

Tek tek:

1. home/away
2. attitude
3. team spirit
4. coach
5. confidence
6. tactic
7. weather
8. minute

Her değişken ayrı fixture ile doğrulanacak.

### AŞAMA 9 — Formation regression

En az:

- 3-4-3
- 3-5-2
- 4-4-2
- 4-5-1
- 5-3-2
- 5-5-0
- 2-5-3
- 2-5-3 iki bek varyantı
- özel/extreme dizilişler

### AŞAMA 10 — Calibration lock

Sonuçlar:

- MAE
- RMSE
- mean bias
- max sector error
- formation-specific error

ile raporlanacak.

Bir tek screenshot'a göre formül değiştirilmeyecek.

---

## 8. Test / Ölçüm Kuralı

Her test için:

```text
V5 prediction
Hattrick ground truth
error = prediction - ground truth
absolute error = |error|
```

Ölçümler:

```text
MAE = mean(|error|)
RMSE = sqrt(mean(error²))
Bias = mean(error)
```

Ayrıca sektör bazında:

```text
LD / CD / RD / MID / LA / CA / RA
```

ayrı hata raporu tutulacak.

---

## 9. Overfitting Kuralları

1. Tek screenshot'a göre katsayı değiştirme.
2. Önce eksik matematiksel katmanı araştır.
3. Contribution katsayısı ile rating scale katsayısını birbirine karıştırma.
4. Bir değişiklikten sonra eski tüm fixture'ları tekrar çalıştır.
5. Bir formül değişikliği bir sektörü düzeltirken başka formationları bozuyorsa değişiklik reddedilir veya ayrı context katmanına taşınır.
6. Resmi Hattrick bilgisi, araştırılmış HO implementasyonu ve bizim gözlemimizi ayrı etiketle.
7. Gizli engine davranışlarını kesin gerçek gibi dokümante etme.

---

## 10. Contribution'dan Gerçek Hesaba Geçişte Öncelik

Öncelik sırası:

```text
1. Position mapping
2. Raw → displayed rating conversion
3. Skill normalization
4. Form
5. Experience
6. Loyalty
7. Overcrowding
8. Stamina
9. Context
10. Tactic
11. Formation-wide regression
```

**Katsayı değişikliği son çare olacaktır.** Eğer Contribution katsayısı ile gerçek Hattrick screenshotları sistematik olarak uyuşmuyorsa önce eksik dönüşüm/katman aranacaktır.

---

## 11. CAL Fixture Formatı

```json
{
  "schema": "hattrickai-v5-rating-calibration-v1",
  "sampleId": "CAL-001",
  "formation": "3-4-3",
  "tactic": "Normal",
  "attitude": "Normal",
  "players": [],
  "positions": [],
  "groundTruth": {
    "leftDefence": 13.0,
    "centralDefence": 12.75,
    "rightDefence": 13.25,
    "midfield": 7.0,
    "leftAttack": 15.75,
    "centralAttack": 13.75,
    "rightAttack": 13.5
  },
  "source": "Hattrick match-order screenshot + CHPP player snapshot"
}
```

---

## 12. İlk Teknik İş Emri

**Şu an yapılacak ilk revizyon:** kodda hiçbir katsayıyı değiştirmeden `RegionalRatingEngine` için tam contribution ledger çıkarılacak.

Her oyuncu için:

```text
Player
Position
Behaviour
Form factor
Loyalty
Experience
GK contribution
DEF-L contribution
DEF-C contribution
DEF-R contribution
MID contribution
ATT-L contribution
ATT-C contribution
ATT-R contribution
```

kaydedilecek.

Ardından CAL-001 için:

```text
11 oyuncu × 7 sektör
```

katkı matrisi çıkarılacak.

Böylece Hattrick'in 13 / 12.75 / 13.25 / 7 / 15.75 / 13.75 / 13.5 değerleriyle hangi oyuncu ve hangi matematiksel katmanın nerede eksik kaldığı görülecek.

**Bu ledger çıkarılmadan yeni katsayı yazılmayacak.**

---

## 13. Durum Takibi

- [x] Calibration planı oluşturuldu.
- [x] CAL-001 ground truth doğrulandı.
- [x] Contribution katsayıları mevcut V5 ile karşılaştırıldı.
- [x] Mevcut motorun Contribution yaklaşımını kullandığı doğrulandı.
- [ ] Position mapping audit
- [ ] Contribution ledger
- [ ] Raw → displayed conversion
- [ ] Skill normalization
- [ ] Form reconstruction
- [ ] Experience reconstruction
- [ ] Loyalty reconstruction
- [ ] Overcrowding reconstruction
- [ ] Stamina reconstruction
- [ ] Context reconstruction
- [ ] 3-5-2 regression
- [ ] 5-5-0 regression
- [ ] 2-5-3 regression
- [ ] Special formation regression
- [ ] Final calibration lock

---

## 14. Temel İlke

> **Hedefimiz Hattrick'e benzeyen bir rating üretmek değil; elimizdeki gerçek Hattrick sonuçlarını açıklayabilecek, katmanları izlenebilir ve yeni screenshotlarla doğrulanabilir bir rating motoru kurmaktır.**

Önce matematiği yeniden kuracağız. Sonra gerçek maç ratingleriyle kalibre edeceğiz. Katsayı oynaması ancak bütün hesap katmanları doğrulandıktan sonra gündeme gelecek.
