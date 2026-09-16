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
- C: 2 / 3.75 / 2, MID 1
- CL: 3.5 / 3.75 / 0, MID 1
- L: 5.75 / 1.75 / 0, MID 1, ATT-L 1.5

### Takyi
- R: 0 / 1.75 / 5.75, MID 1, ATT-R 1.5
- CR: 0 / 3.75 / 3.25, MID 1
- C: 2 / 3.75 / 2, MID 1
- CL: 3.25 / 3.75 / 0, MID 1
- L: 5.75 / 1.75 / 0, MID 1, ATT-L 1.5

### Yeni Doktor singleton seti
- IM-R: 0 / 1 / 1, MID 2.75, ATT 0 / 1.75 / 1.50
- IM-C: 1 / 1 / 1, MID 2.75, ATT 1.25 / 1.75 / 1.25
- IM-L: 1 / 1 / 0, MID 2.75, ATT 1.50 / 1.75 / 0
- FW-R: 0 / 0 / 0, MID 1.25, ATT 2 / 3 / 2
- FW-C: 0 / 0 / 0, MID 1.25, ATT 2 / 3 / 2
- FW-L: 0 / 0 / 0, MID 1.25, ATT 2 / 3 / 2

### 2026-09-16 Pesalovo WB controlled order/position set
- **WB-L Normal:** DEF-L/C/R = **3.50 / 4.00 / 0**, MID **1.00**, ATT = **0 / 0 / 0**.
- **WB-L Towards Wing:** **5.25 / 2.75 / 0**, MID **1.00**, ATT = **0 / 0 / 0**.
- **WB-L Offensive:** **2.75 / 3.00 / 0**, MID **1.25**, ATT = **0 / 0 / 0**.
- **WB-C Normal:** **2.00 / 4.00 / 2.00**, MID **1.00**, ATT = **0 / 0 / 0**.
- **WB-C Offensive:** **1.75 / 3.00 / 1.75**, MID **1.25**, ATT = **0 / 0 / 0**.
- Normal L→C preserves central defence at **4.00** and mirrors the side defence (**3.50/0 → 2.00/2.00**).
- Offensive L→C preserves central defence at **3.00** and mirrors the side defence (**2.75/0 → 1.75/1.75**); MID remains **1.25**.
- Normal→Offensive in the controlled same-side/center captures reduces central defence by **1.00**, shifts side defence downward, and raises MID by **0.25**.
- Normal→Towards Wing on the left concentrates defence toward the player's own side: **DEF-L +1.75**, **DEF-C -1.25**, while MID stays **1.00**.

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

## 9. Production V5 status

- Normal FW slot-symmetry invariant production `RegionalRatingEngineFinal` içinde aktif.
- 2026-09-16 Pesalovo WB controlled seti DB'ye işlendi.
- Yeni WB Towards Wing ekranı üretim motorunda ayrı routing olarak işlendi: Normal WB savunma matrisine göre **side defence ×1.50**, **central defence ×0.6875**; PM ve wing-attack katsayıları korunuyor. Bu katman doğrudan supplied screenshot delta'sına dayalı empirik kalibrasyondur; resmi Hattrick katsayısı iddiası değildir.
- WB Normal/Offensive L↔C mirror davranışı mevcut slot-side routing ile korunuyor.
- Raw→Hattrick quarter-step display dönüşümü ve XP kesin ölçeği hâlâ bağımsız kalibrasyon katmanı olarak tutuluyor; bu aşamada global display dönüşümü production path'e bağlanmadı.
