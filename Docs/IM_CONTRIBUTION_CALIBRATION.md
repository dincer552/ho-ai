# IM Contribution Calibration — Doktor / Bozev

## Amaç

Gerçek Hattrick maç dizilişi ekranlarından alınan oyuncu-pozisyon gözlemlerini V5 rating motorunun kalibrasyonunda kullanılacak ayrı bir ground-truth veri katmanında tutmak.

Ana veri: `TestJSON/RatingCalibration/IM_Doktor_Bozev_2026-09-13.json`

## Mevcut gözlem seti

13.09.2026 tarihinde iki oyuncu için ayrı ayrı `IM-L`, `IM-C` ve `IM-R` pozisyonları gözlendi:

| Oyuncu | IM-L | IM-C | IM-R |
|---|---|---|---|
| Bertalan Doktor | mevcut | mevcut | mevcut |
| Milen Bozev | mevcut | mevcut | mevcut |

Her kayıtta ekranın sağ üstündeki görünen rating değerleri ve görünen delta değerleri ham gözlem olarak saklanır.

## Önemli

Bu veri **Hattrick'in gizli formülü** olarak yorumlanmaz. Önce aynı fixture üzerinde V5 çıktısı ile karşılaştırılır; ancak birden fazla bağımsız örnekte sistematik hata görüldüğünde katsayı/formül revizyonu yapılır.

Önceki iki oyunculu/pair testleri ayrı observation kayıtları olarak eklenecek. Exact ekran değerleri olmadan pair verisi yeniden oluşturulmayacak.

## Kalibrasyon akışı

```text
Hattrick ekranı
    ↓
Observed ground truth
    ↓
Oyuncu + gerçek pozisyon
    ↓
V5 hesap
    ↓
Per-sector error
    ↓
Pair / interaction analizi
    ↓
Gerekirse minimum formül revizyonu
    ↓
Tüm fixture regression
```

Bu dosya veri kaydıdır; tek başına V5 katsayılarını değiştirmez.
