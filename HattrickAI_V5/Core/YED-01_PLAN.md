# YED-01 — Basit Yedek Oyuncu Seçim Motoru

## Amaç

Analiz sonucunda M11 tarafından seçilen ilk 11'e giremeyen takım oyuncuları arasından, Hattrick maç dizilişindeki 7 yedek bölgesi için hızlı ve anlaşılır iki alternatif üretmek.

## Kapsam

- Kaleci
- Göbek defans
- Bek
- İç orta saha
- Forvet
- Kanat
- Ekstra

Her slotta en fazla 2 oyuncu gösterilir. İlk 11'de bulunan oyuncular aday havuzuna alınmaz.

## Motor kuralı

İlk sürüm M6/M7/M8/M9 optimizasyonuna girmez. Basit pozisyon becerisi ağırlıklarıyla sıralama yapar; yeterli uygun oyuncu yoksa slot boş kalabilir. Aynı oyuncunun birden fazla slotta önerilmesi engellenir.

## Uygulama sırası

1. YED-01 motor: takım oyuncularından ilk 11'i çıkar, 7 slot için iki alternatif üret.
2. YED-02 pipeline/API: motor çıktısını mevcut `Analysis` sonucuna bağla.
3. YED-03 UI: ilk 11 kutusunun altında varsayılan kapalı accordion oluştur.
4. YED-04 manuel seçim: iki öneriyi kullanıcı tarafından değiştirilebilir hale getir.
5. YED-05 regression: 7 slot, ilk 11 hariç, maksimum 2 alternatif, benzersiz oyuncu ve deterministic sıralama kontrolleri.
6. YED-06 acceptance: gerçek CHPP fixture + production smoke.

## Not

Bu ilk sürüm otomatik CHPP `matchorders` göndermez. CHPP'ye geri yazma ayrı bir iş olarak ele alınacaktır.
