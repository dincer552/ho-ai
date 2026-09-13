# Motor Observation DB

Bu dosya, Hattrick canlı arayüzünden kullanıcı tarafından manuel olarak yakalanan rating-board gözlemlerinin kalıcı kaynağıdır.

- Gözlem = ölçüm; engine katsayısı değildir.
- Ekrandaki mutlak 7 bölgesel rating saklanır.
- Yeşil/turuncu `+/-` değerleri önceki ekranla karşılaştırmalı UI farkı olduğu için katsayı olarak kaydedilmez.
- Fiziksel saha slotları `DEF-L / DEF-CL / DEF-C / DEF-CR / DEF-R` ayrı tutulur.
- Yeni gözlemler `HattrickAI_V5/Data/MotorObservationDB.json` içine eklenir.

## 2026-09-13 — tek defans oyuncusu baseline

Aynı oyuncu (`D. Nocon`), aynı maç bağlamı, `1-0-0`, Taktik=`Normal`, Takım Davranışı=`Normal`, Antrenman modu kapalı.

| Slot | DEF-L | DEF-C | DEF-R | MID | ATT-L | ATT-C | ATT-R |
|---|---:|---:|---:|---:|---:|---:|---:|
| DEF-L  | 5.75 | 1.75 | 0.00 | 1.00 | 1.25 | 0.00 | 0.00 |
| DEF-CL | 3.50 | 3.75 | 0.00 | 1.00 | 0.00 | 0.00 | 0.00 |
| DEF-C  | 2.00 | 3.75 | 2.00 | 1.00 | 0.00 | 0.00 | 0.00 |
| DEF-CR | 0.00 | 3.75 | 3.50 | 1.00 | 0.00 | 0.00 | 0.00 |
| DEF-R  | 0.00 | 1.75 | 5.75 | 1.00 | 0.00 | 0.00 | 1.25 |

Bu beş gözlem, tek defans oyuncusunun beş fiziksel defans slotundaki saha etkisini aynı koşullarda izole eden ilk simetrik baseline setidir. Engine'e doğrudan hard-code edilmeden önce ikili/üçlü defans kombinasyonlarıyla doğrulanmalıdır.
