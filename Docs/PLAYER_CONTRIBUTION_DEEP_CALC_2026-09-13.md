# Player Contribution Deep Calculation — 2026-09-13

## Running research log

Kısa kural: Her yeni hesap/yorum buraya kısa ve sayısal olarak eklenecek. Üretim katsayıları, kanıt güçlenmeden değiştirilmeyecek.

### 2026-09-14 — Yeni screenshot seti: GK + W singleton ve W çiftleri
- **GK singleton — Bultot:** DEF-L/C/R = **4.25 / 3.75 / 4.25**, MID/ATT = 0. Empty-sector floor 0.75 kabul edilirse raw katkı = **3.50 / 3.00 / 3.50**. Bultot GK17, DEF4 için published GK skill-only hesap: side `17×.183 + 4×.082 = 3.430`, center `17×.165 + 4×.079 = 3.123`. Gözlem farkı side **+0.070**, center **-0.123**.
- **W singleton — Gobiet:** W-L/W-R simetrik. Raw katkı: side DEF **1.00**, center DEF **0.50**, MID **0.75**, side ATT **4.50**, center ATT **0.25**.
- Gobiet normal-W skill-only: D9 → side DEF `.936`, center DEF `.333`; PM11 → MID `.715`; side ATT `9×.219 + 15×.054 = 2.781`; center ATT `.162`. DEF/MID/central ATT gözlemleri katsayı tablosuyla aynı mertebede; side ATT state/XP/ölçek katmanı için ayrıca kalibre edilmeli.
- **W singleton — Dobóvári:** W-L/W-R simetrik. Raw katkı: side DEF **0.50**, center DEF **0.25**, MID **0.25**, side ATT **4.50**, center ATT **0.25**.
- Dobóvári normal-W skill-only: D3 → side DEF `.312`, center DEF `.111`; PM5 → MID `.325`; side ATT `7×.219 + 17×.054 = 2.451`; center ATT `.126`.
- **W çiftleri:** Gobiet-L + Dobóvári-R => **1.75 / 1.25 / 1.25 DEF, MID 1.75, ATT 5.25 / 1.25 / 5.25**. Ters yerleşim Dobóvári-L + Gobiet-R => **1.25 / 1.25 / 1.75 DEF, MID 1.75, ATT 5.25 / 1.25 / 5.25**.
- Singleton raw katkılar toplandığında iki çift ekranı da tam olarak yeniden üretiyor. Örn. MID `.75 + .25 + .75 floor = 1.75`; ATT side `4.5 + .75 floor = 5.25`. Bu, kontrollü W örneğinde **additive player contribution ledger** için çok güçlü kanıt.
- **Sonuç:** 0.75 display floor + oyuncu katkılarının sektör bazında toplanması şu anki en güçlü model. GK savunma ve W savunma/orta saha/merkez hücum değerleri published katsayılarla aynı mertebede. Side-attack için state/XP ölçeği henüz çözülmedi.
- Production katsayılarına dokunulmadı. Bir sonraki kritik veri **IM singleton**, ardından **FW singleton**; W için aynı oyuncunun farklı emirleri de değerli.

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

Öncelik: **raw contribution scale → form/loyalty → XP → overcrowding → display quantization → coefficient refinement**. Önce singleton set fit edilecek, sonra tüm 3-defender kombinasyonları tekrar test edilecek. Aynı oyuncu/slot ve farklı state içeren kontrollü screenshot en değerli yeni kanıt; özellikle IM/W/F singletonları artık bir sonraki büyük veri adımı.
