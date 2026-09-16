# Player Rating Contribution Research — 2026-09-13

## 2026-09-16 — Güncel V5 durumu ve sonraki kalibrasyon

V5 regional rating motorunda mevcut ampirik kanıtlar yeniden özetlendi. **GK + CD + W + IM + FW + WB** pozisyon aileleri artık singleton/yan davranış gözlemleriyle kapsanıyor. Production'a alınan son ampirik düzeltme yalnızca **Normal FW slot simetrisi**: normal FW'nin L/C/R konumu değiştiğinde gözlenen 3 hücum sektörünün aynı kalması nedeniyle normal FW katkısı merkez-side normalize ediliyor. Defensive/Towards Wing FW davranışı değiştirilmedi.

Gerçek 3-4-3 CAL fixture'ında bu son değişiklikten sonra V5 ile eski kilitli değerler arasında yalnızca küçük farklar kaldı: **Sol Atak 9.91 vs 9.74 (+0.17)** ve **Sağ Atak 10.06 vs 9.93 (+0.13)**. Bu küçük farklar şimdilik kabul edilerek yeni katsayı oynanmayacak; eski regression lock'ları sırf sonucu eşleştirmek için değiştirmeden mevcut ampirik davranış korunacak.

### Pozisyon ailelerinin mevcut durumu

| Pozisyon | Mevcut bulgu | Durum |
|---|---|---|
| GK | Published GK katsayıları singleton ekranla çok yakın örtüşüyor | Güçlü |
| CD | L/C/R side redistribution ve 3-defender additivity doğrulandı | Güçlü |
| WB | WB-L/R mirror davranışı ve pair additivity doğrulandı; order varyantları eksik | Singleton temel davranış doğrulandı |
| IM | L/C/R'de MID ve central attack sabit; yan DEF/ATT yeniden dağılıyor | Mimari güçlü, state/scale açık |
| W | L/R mirror davranışı ve pair additivity doğrulandı | Güçlü |
| FW | Normal FW L/C/R ekranları tamamen 2/3/2 simetrisi gösteriyor | Normal order güçlü; diğer order'lar açık |

### 2026-09-16 sonraki deney sırası

1. **WB singleton order deneyleri:** aynı oyuncuyla Normal / Defensive / Offensive / Towards Middle; mümkünse L/C/R kontrollü ekranlar.
2. **IM order deneyleri:** Normal / Defensive / Offensive / Towards Wing; mümkün olduğunca aynı oyuncu ve aynı maç bağlamı.
3. **FW order deneyleri:** Defensive / Towards Wing; normal FW simetrisi değiştirilmeden karşılaştırılacak.
4. Her deneyde önce 7 sektörün mutlak beyaz ratingleri ground truth alınacak; yeşil/turuncu delta göstergeleri intrinsic contribution olarak kullanılmayacak.
5. Yeterli kontrollü veri birikmeden global Contribution katsayıları değiştirilmeyecek.

### Ana açık matematiksel konu

Pozisyon/side mapping artık büyük ölçüde gözlemlenmiş durumda. Bundan sonraki kalibrasyonun ana hedefi:

```text
skill normalization
→ loyalty
→ form
→ position/order contribution
→ overcrowding
→ experience
→ stamina/minute
→ context/tactic
→ raw contribution scale
→ Hattrick quarter-step display
```

Özellikle **raw contribution scale, XP ölçeği ve standard-state uplift'in form/XP katmanlarından ayrıştırılması** hâlâ çözülmemiştir. Bu nedenle yeni ekranlar öncelikle bu katmanları ayıracak şekilde seçilecektir.

## Kısa sonuç

Oyuncunun DB'deki skill değerleri takım ratingine doğrudan tek bir "oyuncu ratingi" olarak eklenmiyor. Oyuncu önce bulunduğu pozisyon + taraf + bireysel davranış için skill katkılarına ayrılıyor; bu katkılar 7 takım sektöründe toplanıyor.

Temel zincir:

```text
CHPP Player DB
→ skill / form / stamina / experience / loyalty
→ position + side + behaviour
→ skill × contribution coefficient
→ sector contribution
→ overcrowding / interaction
→ experience contribution
→ context / tactic / minute
→ 7 sector raw rating
→ production display scale
```

## 1. Contribution tablosu doğrulaması

`Contribution - Hattrick.pdf` oyuncu skill'lerinin pozisyona göre 7 takım rating sektörüne nasıl dağıldığını veriyor. Örnek:

```text
GK central defence = Keeper × 0.165 + Defending × 0.079
GK side defence    = Keeper × 0.183 + Defending × 0.082

Normal central defender:
  central defence = Defending × 0.186
  side defence    = Defending × 0.077
  midfield        = Playmaking × 0.035
```

Aynı tablo wing back, inner midfielder, winger ve forward için Passing/Winger/Scoring/Playmaking/Defending katkılarını ve Offensive/Defensive/Towards Wing/Towards Middle davranışlarını ayrı katsayılarla tanımlıyor.

Bu katsayılar araştırılmış referanstır; gizli Hattrick source-code formülü olarak kabul edilmiyor.

## 2. V5 kodunda iki farklı oyuncu modeli var

### M3 — oyuncu uygunluğu

`FoxtrickPositionContributionEngine` 20 yasal pozisyon/davranış için oyuncunun göreli uygunluğunu hesaplıyor. Form + stamina etkin; experience + loyalty varsayılan olarak bu M3 uygunluk hesabında kapalı. Sonuç normalleştirilmiş pozisyon skorudur.

**M3 skoru takım ratinginin kendisi değildir.** M5/XI seçiminde oyuncunun hangi pozisyonda ne kadar uygun olduğunu belirlemek için kullanılır.

### M7 — gerçek takım rating katkısı

`V5RatingEngine` `RegionalRatingEngineFinal` üzerinden çalışacak şekilde tasarlanmıştır. Final wrapper'ın tabanı `RegionalRatingEngineFixed`; CHPP'deki gerçek skill/form/stamina/experience/loyalty değerleri regional contribution hesabına girer. Yeni wrapper yalnızca gerçek ekran kanıtıyla doğrulanan normal-FW slot simetrisini production yoluna alır.

Bu ayrım kritik: **M3 = oyuncu uygunluğu, M7 = takım sektör ratingi.**

## 3. DB alanlarının ratinge etkisi

Mevcut production rating akışında:

- `keeper`, `defending`, `playmaking`, `passing`, `winger`, `scoring`: pozisyon katsayılarıyla sektörlere katkı verir.
- `form`: contribution'ın form çarpanını değiştirir.
- `stamina`: maç dakikasına bağlı stamina etkisine girer.
- `experience`: ayrı bir sektör katkısı katmanı olarak eklenir.
- `loyalty`: effective skill'e eklenen loyalty etkisi olarak kullanılır.
- `injuryLevel == 999`: oyuncu uygun değildir; katkı dışıdır.
- `specialty`: normal rating katkısının ana skill katsayısı değil, daha sonraki event/taktik katmanları için bağlamdır.

Production Fixed motorunda temel skill girişi `max(0, skill - 1)` şeklindedir. Form ve stamina uygulanır; experience ayrı eklenir; ardından crowding, context/taktik ve diğer takım etkileri uygulanır.

## 4. En önemli bulgu: contribution ledger gerekli

Şu anda takım için yalnızca 7 sektör toplamını görüyoruz. Araştırmanın bir sonraki doğru adımı her oyuncu için şu ledger'ı üretmek olmalı:

```text
Player
Position / Side / Behaviour
Effective skills
DEF-L contribution
DEF-C contribution
DEF-R contribution
MID contribution
ATT-L contribution
ATT-C contribution
ATT-R contribution
Experience contribution
Crowding loss
Context adjustment
```

Böylece örneğin bir DEF oyuncusunun `Defending 17` değerinin DEF-C'ye kaç puan, yan defansa kaç puan ve Playmaking 3'ün MID'e kaç puan taşıdığı tek tek görülebilecek.

## 5. 13.09.2026 oyuncu DB snapshot

CHPP snapshot'ında 31 oyuncunun skill/form/stamina/experience/loyalty/specialty alanları mevcut. Bu veri gelecekte contribution ledger ve gerçek Hattrick screenshot kalibrasyonu için kullanılacak. Ham oyuncu verisi production katsayılarına otomatik olarak yazılmayacak.

## 6. Gerçek ekran verisiyle ana kanıtlar

### 6A. WB singleton + pair

Pesalovo ve Nocoń ile WB-L/WB-R tek oyuncu testleri ve karşılıklı WB-L + WB-R çift testleri kaydedildi. Sonuçlar, oyuncu sağa/sola taşındığında yan savunma ve yan hücum katkılarının karşı tarafa yeniden dağıldığını; çift testlerde sektörlerin büyük ölçüde tek oyuncu katkılarının toplamı olduğunu gösteriyor.

Örnek:

- Pesalovo WB-L: DEF 6.00 / 2.00 / 0.00, ATT 1.25 / 0.00 / 0.00
- Pesalovo WB-R: DEF 0.00 / 2.00 / 6.00, ATT 0.00 / 0.00 / 1.25
- Nocoń WB-L: DEF 5.75 / 1.75 / 0.00, ATT 1.25 / 0.00 / 0.00
- Nocoń WB-R: DEF 0.00 / 1.75 / 5.75, ATT 0.00 / 0.00 / 1.25

İki WB birlikte kullanıldığında merkez savunmanın yaklaşık 3.25'e çıkması, tekil merkez katkıların toplandığını destekliyor. Bu veri WB position/side mapping ve additivity hipotezini güçlendiriyor; ancak form/XP/display katmanları ayrıştırılmadan production katsayılarını değiştirmek için tek başına yeterli değil.

### 6B. Doktor IM/FW singleton

Bertalan Doktor'un IM-L/C/R singleton seti MID **2.75** ve central attack **1.75** değerlerini sabit tutarken yan DEF/ATT dağılımını değiştiriyor. Aynı oyuncunun normal FW-L/C/R setinde ise üç hücum sektörü **2 / 3 / 2** olarak sabit kalıyor.

**Production sonucu:** normal FW için slot simetrisi V5 final motorunda uygulanıyor. Defensive/TowardsWing FW order'ları için ayrı screenshot seti bekleniyor.

### 6C. CD ve W additivity / mirror bulguları

CD singleton ve üçlü kombinasyonlar, oyuncunun L/C/R taraf değişiminin yan sektörleri yeniden dağıttığını ve katkıların büyük ölçüde additive olduğunu gösterdi. W singleton ve pair testleri de aynı mirror/additivity davranışını destekledi. Bu iki aile için mevcut pozisyon/side mapping korunuyor.

## 7. Kalibrasyon kararı

Global Contribution katsayıları hâlâ rastgele değiştirilmiyor.

Korunan/kanıtlı katmanlar:

1. skill normalization (`skill - 1`)
2. loyalty
3. form/stamina
4. published position/order coefficients
5. crowding
6. separate experience contribution
7. context/tactic/minute
8. raw/display pipeline
9. normal-FW slot symmetry correction

**Henüz çözülmeyenler:** raw contribution → Hattrick quarter-step display dönüşümü, XP'nin kesin ölçeklemesi ve standard-state uplift'in form/XP ile ayrıştırılması.

## 8. Proje araştırma durumu — kısa özet

- M3: Foxtrick-compatible 20-position player suitability modeli mevcut.
- M4: legal formation universe ve anti-lock aday havuzu mevcut.
- M5/M6: XI aday üretimi ve beam/refinement araması mevcut.
- M7: 7 sektör regional rating production engine mevcut; crowding, form, experience, stamina ve context katmanları ayrı tutuluyor.
- M7.2: tactic scenario katmanı mevcut.
- M8: chance/matchup katmanı mevcut.
- M9: W/D/L + xG/simulation sonucu mevcut.
- M10: formasyonlar outcome-first karşılaştırılıyor.
- M11: final XI selection ve canonical 7-tactic chain mevcut.
- MotorDB: analiz sonuçları JSON archive olarak saklanıyor; deterministic JSON regression mevcut.
- Rating engine comparison: V5 / HO / HattrickDash / Foxtrick aynı XI üzerinde yan yana çalıştırılabiliyor.
- Yedek oyuncu sistemi: 7 slot × 2 öneri ve aynı analysis run entegrasyonu tamamlandı.
- CHPP match-order aktarımı: araştırılmış plan mevcut; gerçek write yalnızca kullanıcı onayı + uygun CHPP yetkisiyle ele alınacak.

## 9. Açık araştırma sorusu

**Asıl eksik artık "oyuncunun skill'i ratinge katkı yapıyor mu?" değil; katkının tam olarak hangi ara katmanlardan geçip Hattrick'in görünen 7 sektör değerine dönüştüğü.**

Bir sonraki teknik hedef: **WB order singleton testleri → IM order singleton testleri → FW defensive/towards-wing testleri → Player Contribution Ledger → raw/display/XP kalibrasyonu.**
