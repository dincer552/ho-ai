# CAL-001 Model Variant Diagnostic Protocol

Bu protokol, `RATING_MOTOR_RECONSTRUCTION_README.md` içindeki araştırılmış hipotezlerle gerçek Hattrick ground truth'unu birbirine karıştırmamak için oluşturuldu.

## Karar kuralı

- CAL-001 Hattrick ekranı ground truth'tur.
- Contribution / HO / araştırma formülleri referans ve hipotezdir.
- Hiçbir model yalnızca dokümanda bulunduğu için doğru kabul edilmez.
- Önce MAE, Bias ve RMSE karşılaştırılır.
- Daha iyi çıkan model production'a doğrudan alınmaz; yeni ground-truth örneklerinde doğrulanır.
- Katsayılar varyant testi tamamlanmadan rastgele değiştirilmez.

## CAL-001 varyantları

| Varyant | Skill modeli | Experience modeli | Motor |
|---|---|---|---|
| A | `skill - 1` | ayrı experience contribution | Fixed |
| B | raw skill | ayrı experience contribution | Fixed |
| C | `skill - 1` | experience yok | Fixed |
| D | raw skill | experience yok | Fixed |
| E | raw skill | `ExperienceBonus - 1.13` effective delta | Research |
| F | `skill - 1` | `ExperienceBonus - 1.13` effective delta | Research |

## Test çıktısı

Her varyant için:

```text
LD CD RD MID LA CA RA
MAE
Bias
RMSE
```

hesaplanır.

Ayrıca Fixed production baseline'ın mevcut raw değerleri sabit regression ile korunur.

## Yorumlama sırası

1. Skill normalization etkisi.
2. Experience modelinin etkisi.
3. Side defence / side attack açığının değişimi.
4. Central defence / midfield / central attack yan etkileri.
5. Sonucun yalnızca CAL-001'e overfit olup olmadığı.

`skill - 1` veya effective-experience modeli bu test sonucundan önce kesin doğru ilan edilmez.
