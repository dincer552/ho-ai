# Player Contribution Deep Calculation — 2026-09-13

## Running research log

Kısa kural: Her yeni hesap/yorum buraya kısa ve sayısal olarak eklenecek. Üretim katsayıları, kanıt güçlenmeden değiştirilmeyecek.

### 2026-09-14 — Yeni veri: Doktor IM + FW singleton seti
- Bertalan Doktor oyuncu girdisi: **Def 2, PM 16, Pass 11, Winger 6, Scoring 6, Form 6, XP 8, Stamina 6**.
- **Normal IM — sağ:** DEF-L/C/R = **0 / 1 / 1**, MID **2.75**, ATT-L/C/R = **0 / 1.75 / 1.50**.
- **Normal IM — merkez:** DEF-L/C/R = **1 / 1 / 1**, MID **2.75**, ATT-L/C/R = **1.25 / 1.75 / 1.25**.
- **Normal IM — sol:** DEF-L/C/R = **1 / 1 / 0**, MID **2.75**, ATT-L/C/R = **1.50 / 1.75 / 0**.
- Bu üç IM ekranında **MID tam sabit 2.75**. Yan pozisyon yalnızca savunma ve yan hücum dağılımını değiştiriyor; merkez hücum **1.75** sabit. Bu, önceki stoper/W bulgularındaki "pozisyon yanlara katkıyı yeniden dağıtır, merkez katkıyı büyük ölçüde korur" invariantını güçlü biçimde genişletiyor.
- Published normal-IM skill-only katsayılarıyla Doktor'un kaba hesabı: side DEF `2×.028=.056`, center DEF `2×.070=.140`, MID `16×.139=2.224`, side ATT `11×.028=.308`, center ATT `11×.057 + 6×.038=.855`. Gözlenen değerler bundan belirgin yüksek; dolayısıyla form/XP/state + rating scale katmanı açıkça devrede. Katsayılar production'a değiştirilmedi.
- **Normal FW — sağ/merkez/sol:** üç pozisyonun tamamında DEF **0/0/0**, MID **1.25**, ATT-L/C/R = **2 / 3 / 2**. Bu çok değerli: Normal FW'nin yatay slotu değişmesine rağmen üç hücum sektörünün gözlenen ratingleri aynı kaldı. Şimdilik bu, normal FW'nin side/center attack üretiminde slot-simetrik olduğunu gösteriyor; FW tarafında pozisyon emirlerinden bağımsız temel katkıyı ayırmak için güçlü singleton kanıtı.
- Published normal-FW skill-only kaba hesap: MID `16×.041=.656`; side ATT `6×.058 + 6×.032 + 11×.048=1.068`; center ATT `6×.178 + 11×.066=1.794`. Gözlenen `1.25 / 2 / 3 / 2`, özellikle merkez hücumun skill-only değerden yaklaşık **1.67×** yüksek olması, state/XP/scale katmanının tek başına ihmal edilemeyeceğini gösteriyor.
- Son IM/FW ekranlarındaki yeşil/turuncu küçük sayılar **delta göstergesi**; bunları oyuncunun mutlak katkısı olarak kullanmıyoruz. Ana beyaz rating sayıları ground truth olarak tutuluyor.
- **Yeni sonuç:** IM için yan pozisyon değişiminde MID ve central-attack sabit kalıyor; FW için L/C/R değişiminde bütün hücum ratingleri sabit kalıyor. Bu, pozisyon katsayı matrisinin sadece "hangi skill ne kadar" değil, aynı zamanda **slot/order tarafından hangi sektöre yönlendirildiği** şeklinde modellenmesi gerektiğini doğruluyor.
- Artık singleton kapsaması: **GK + CD + W + IM + FW**. Eksik büyük pozisyon ailesi **WB**. Sonraki en değerli deney: Manuel Gobiet/Nándor Dobóvári gibi WB adaylarından biriyle WB-C/L/R; ardından IM/FW emirleri (offensive/defensive/towards wing).

## 1. Scope

M7 regional rating için derin hesaplama notu. Kaynaklar: production V5 fixed engine, CHPP oyuncu snapshot'ı ve Hattrick screenshot singleton/3-defender gözlemleri. Katsayılar reverse-engineering/calibration sonucudur; resmi Hattrick source code iddiası değildir.

## 2. Ground-truth player inputs

- Cristian Pesalovo: Def 16, PM 6, Pass 9, Winger 3, Scoring 6, Form 7, XP 6, Stamina 7.
- Dawid Nocoń: Def 17, PM 3, Pass 5, Winger 4, Scoring 7, Form 6, XP 12, Stamina 5.
- Abeiku Takyi: Def 17, PM 3, Pass 8, Winger 4, Scoring 7, Form 5, XP 10, Stamina 6.

## 3. Empirical singleton observations

### Pesalovo
- R: DEF-L 0 / DEF-C 2 / DEF-R 6, MID 1, ATT-R 1.25
- CR: 0 / 4 / 3.5, MID 1
- C: 2 / 4 / 2, MID 1, attacks 0
- CL: 3.5 / 4 / 0, MID 1
- L: 6 / 2 / 0, MID 1, ATT-L 1.25

### Nocoń
- R: 0 / 1.75 / 5.75, MID 1, ATT-R 1.25
- CR: 0 / 3.75 / 3.5, MID 1
- C: 2 / 3.75 / 2, MID 1, attacks 0
- CL: 3.5 / 3.75 / 0, MID 1
- L: 5.75 / 1.75 / 0, MID 1, ATT-L 1.5

### Takyi
- R: 0 / 1.75 / 5.75, MID 1, ATT-R 1.5
- CR: 0 / 3.75 / 3.25, MID 1
- C: 2 / 3.75 / 2, MID 1, attacks 0
- CL: 3.25 / 3.75 / 0, MID 1
- L: 5.75 / 1.75 / 0, MID 1, ATT-L 1.5

### Yeni Doktor singleton seti
- IM-R: 0 / 1 / 1, MID 2.75, ATT 0 / 1.75 / 1.50
- IM-C: 1 / 1 / 1, MID 2.75, ATT 1.25 / 1.75 / 1.25
- IM-L: 1 / 1 / 0, MID 2.75, ATT 1.50 / 1.75 / 0
- FW-R: 0 / 0 / 0, MID 1.25, ATT 2 / 3 / 2
- FW-C: 0 / 0 / 0, MID 1.25, ATT 2 / 3 / 2
- FW-L: 0 / 0 / 0, MID 1.25, ATT 2 / 3 / 2

## 4. Additivity check

- P-L + N-C + T-R => **7.75 / 7.00 / 7.50**
- P-L + N-CL + T-R => **9.50 / 7.00 / 5.75**
- P-L + N-CR + T-R => **6.00 / 7.00 / 9.25**
- T-L + N-C + P-R => **7.50 / 7.00 / 7.75**

İlk üç kombinasyon Nocoń C→CL için yaklaşık **+1.75 sol / -1.75 sağ**, C→CR için **-1.75 sol / +1.75 sağ** gösteriyor. Residual genelde 0.25–0.50; çoğunlukla additive ledger geçerli.

## 5. Current-engine mismatch

`RegionalRatingEngineFixed`: `max(0, skill-1)` → loyalty → form multiplier → stamina/minute → ayrı XP contribution → crowding → team/context. Eski engine XP delta'yı skill vector içine alıyor. İki yaklaşımın hangisinin gerçek olduğu henüz kanıtlanmadı.

## 6. First numerical test — normal central defender

| Player | Current raw DEF-C | Observed | Delta |
|---|---:|---:|---:|
| Pesalovo | ~3.933 | 4.00 | -0.067 |
| Nocoń | ~3.935 | 3.75 | +0.185 |
| Takyi | ~3.531 | 3.75 | -0.219 |

Side DEF:

| Player | Current raw | Observed | Delta |
|---|---:|---:|---:|
| Pesalovo | ~1.716 | 2.00 | -0.284 |
| Nocoń | ~1.751 | 2.00 | -0.249 |
| Takyi | ~1.574 | 2.00 | -0.426 |

**Not:** Bu bölümdeki "Observed" değerleri doğrudan raw kabul edilmemeli; yeni display-coordinate bulgusuyla yeniden yorumlanacak.

## 7. Display conversion finding

Repo'da `pow(x,1.2)/4+1` araştırma converter'ı bulunuyor; production fixed engine ise 2-decimal round/clamp kullanıyor. Schum/HO araştırması ise canlı Hattrick rating display'inin 0.75 tabanlı quarter-step koordinat kullandığını gösteriyor. Bu iki dönüşüm aynı şey olmayabilir. Bu nedenle nonlinear converter **şimdilik M7'ye bağlanmayacak**; controlled screenshot ile doğrulanacak.

## 8. Deep-calculation conclusions

**Yüksek güven:** katkı yaklaşık additive; pozisyon/yan davranışı birinci derecede belirleyici; sektör değerleri quarter-step quantized.

**Yeni yüksek/orta güven:** published coefficient × skill önce raw contribution üretir; form/loyalty/crowding state katmanı bunu değiştirir; XP ayrı bir son katkı olabilir. Displayed quarter-step değer raw contribution ile birebir aynı ölçek değildir.

**Çözülmedi:** skill normalization, form eğrisi/baseline, XP'nin exact scale'i, raw→display dönüşümü, tactic/context ve coefficient tablosunun standard-state uplift'inin nasıl ayrıştırılacağı.

## 9. Next calibration target

Öncelik: **raw contribution scale → form/loyalty → XP → overcrowding → display quantization → coefficient refinement**. Singleton set artık GK/CD/W/IM/FW ailelerini kapsıyor. Sonraki kritik aile **WB**. Ardından IM/FW individual-order deneyleriyle order katsayıları ayrıştırılacak. Aynı oyuncu/slot ve farklı state içeren kontrollü screenshot yine en değerli kalibrasyon kanıtı.

## 10. 2026-09-16 — Empirical final V5 motor

Son screenshot setleriyle doğrulanan WB-L/WB-R additivity + W mirror davranışı + normal IM slot invariantı + normal FW L/C/R simetrisi birlikte değerlendirildi.

**Production'a alınan tek yeni davranış:** `Normal Forward` katkısı artık slot tarafına bağlanmıyor. V5 final wrapper'ı normal FW'leri hesaplama sırasında merkez-side normalize ederek üç hücum sektöründeki kanıtlanmış `L/C/R` simetrisini koruyor. Defensive/TowardsWing FW davranışı değiştirilmedi; bu order'lar için henüz yeterli singleton kanıt yok.

**Korunan katmanlar:** skill-1 normalization, loyalty, form/stamina, published position coefficients, crowding, separate XP, context/tactic ve mevcut raw/display pipeline.

**Değiştirilmeyen belirsizlikler:** raw→Hattrick quarter-step display dönüşümü, XP'nin kesin ölçeklemesi ve standard-state uplift ayrıştırması için yeni kontrollü ekranlar gerekiyor. Bunlar doğrulanmadan global katsayılar değiştirilmedi.
