# HatFor → ho-ai entegrasyon rehberi

Branch: `v5` (doğrudan ana branch)

## Mimari kural (RATING_ENGINE_PLAN.md)

> Mevcut V5 rating davranışı korunur. Yeni motorlar ayrı `IRatingEngine` olarak çalışır.

Excel mantığı **V5’in yerine yazılmaz**; yeni motor `HatFor` olarak eklenir. Varsayılan V5 kalır.

## Eklenen dosyalar

- `HattrickAI_V5/Core/HatForRatingEngine.cs`
- `HattrickAI_V5/Core/RatingEngineContracts.cs` (`HatFor` enum)
- `HattrickAI_V5/Core/RatingEngineRegistry.cs` (registry kaydı)
- `Docs/HATFOR_RATING_ENGINE.md`

## Formül

```
etkin  = max(0, skill - 1) + loyalty
formF  = 0.378 * sqrt(min(7, form - 1))
expKat = poly(exp-1) * 0.345
ham    = Σ (etkin * formF * coeff + expKat)
rating = ROUND(((ham * coach * scale)^1.2 / 4 + 1) * 4, 0) / 4
```

## Formasyon katsayıları

| Formasyon | Durum |
|-----------|--------|
| 5-4-1, 2-5-3 | Excel screenshot kalibrasyonu |
| 3-5-2, 4-4-2 | İskelet + yaklaşık katsayı |
| Diğerleri | Stub (aynı yapıyla genişletilebilir) |

## Kullanım

API/UI rating engine selector üzerinden `HatFor` seçilir. V5 default kalır.
