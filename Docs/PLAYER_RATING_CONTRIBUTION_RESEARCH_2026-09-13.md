# Player Rating Contribution Research — 2026-09-13

## Kısa sonuç

Oyuncunun DB'deki skill değerleri takım ratingine **doğrudan tek bir "oyuncu ratingi" olarak eklenmiyor**. Oyuncu önce bulunduğu pozisyon + taraf + bireysel davranış için skill katkılarına ayrılıyor; bu katkılar 7 takım sektöründe toplanıyor.

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

`V5RatingEngine` doğrudan `RegionalRatingEngineFixed` kullanıyor. Yani production V5 ratinginde oyuncunun M3 Foxtrick skoru tekrar takım ratingine sokulmuyor; CHPP'deki gerçek skill/form/stamina/experience/loyalty değerleri doğrudan regional contribution hesabına giriyor.

Bu ayrım kritik: **M3 = oyuncu uygunluğu, M7 = takım sektör ratingi.**

## 3. DB alanlarının ratinge etkisi

Mevcut production `RegionalRatingEngineFixed` akışında:

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

Örnek:

```text
Abeiku Takyi:    DEF 17, PM 3, PASS 8, WING 4, SCORE 7, STAM 6, FORM 5, EXP 10
Dawid Nocoń:     DEF 17, PM 3, PASS 5, WING 4, SCORE 7, STAM 5, FORM 6, EXP 12
Cristian Pesalovo: DEF 16, PM 6, PASS 9, WING 3, SCORE 6, STAM 7, FORM 7, EXP 6
Bertalan Doktor: PM 16, PASS 11, DEF 2, WING 6, SCORE 6, STAM 6, FORM 6, EXP 8
Manuel Gobiet:   PM 11, WING 15, PASS 9, DEF 9, SCORE 8, STAM 6, FORM 8, EXP 7
```

## 6. Gerçek ekran verisiyle yeni kanıt

13.09.2026 tarihli 3-0-0 ekran gözlemleri `HattrickAI_V5/Core/EmpiricalMotorObservationDb.json` içine iki ayrı capture olarak kaydedildi.

Aynı görünen oyuncu dizilimi için iki capture'da DEF-L 7.75 ve 7.50 olarak farklı çıktı. Bu nedenle iki ölçüm ayrı tutuldu; ortalaması alınmadı. Bu farkın UI refresh/context/slot mapping kaynaklı olup olmadığı ayrıca araştırılacak.

### 6A. 14.09.2026 WB singleton + pair kanıtı

Pesalovo ve Nocoń ile WB-L/WB-R tek oyuncu testleri ve karşılıklı WB-L + WB-R çift testleri kaydedildi. Sonuçlar, oyuncu sağa/sola taşındığında yan savunma ve yan hücum katkılarının beklenen şekilde karşı tarafa yeniden dağıldığını; çift testlerde ise sektörlerin büyük ölçüde tek oyuncu katkılarının toplamı olduğunu gösteriyor.

Özellikle:

- Pesalovo WB-L: DEF 6.00 / 2.00 / 0.00, ATT 1.25 / 0.00 / 0.00
- Pesalovo WB-R: DEF 0.00 / 2.00 / 6.00, ATT 0.00 / 0.00 / 1.25
- Nocoń WB-L: DEF 5.75 / 1.75 / 0.00, ATT 1.25 / 0.00 / 0.00
- Nocoń WB-R: DEF 0.00 / 1.75 / 5.75, ATT 0.00 / 0.00 / 1.25

İki WB birlikte kullanıldığında merkez savunmanın yaklaşık 3.25'e çıkması, tekil merkez katkıların toplandığını destekliyor. Bu veri **WB position/side mapping ve additivity hipotezini güçlendiriyor**, ancak form/XP/display katmanları ayrıştırılmadan production katsayılarını değiştirmek için tek başına yeterli değil.

Bu nedenle mevcut karar korunuyor: **veri DB'ye eklenir, production coefficient değiştirilmez.** Bir sonraki kontrollü deney WB davranış varyantları (normal/defensive/offensive/towards middle) ve ardından kalan davranışların A/B karşılaştırmasıdır.

## 7. Kalibrasyon kararı

Henüz Contribution katsayılarını rastgele değiştirmiyoruz.

Önce:

1. Her oyuncunun contribution ledger'ı çıkarılacak.
2. Aynı XI'nin Hattrick 7 sektör ground truth'u ile karşılaştırılacak.
3. Skill normalization, form, experience, loyalty, crowding ve position/side mapping ayrı A/B testleriyle ölçülecek.
4. Raw contribution → displayed rating dönüşümü ayrı test edilecek.
5. Birden fazla gerçek maç/screenshot aynı hatayı doğrularsa production katsayısı değiştirilecek.

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

Bir sonraki teknik hedef: `Player Contribution Ledger` + gerçek screenshot karşılaştırması.
