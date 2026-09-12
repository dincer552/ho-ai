# Takım + Oyuncu CHPP JSON Export — V1

## Amaç

Bu yardımcı, ana sayfadaki **ANALİZİ ÇALIŞTIR** butonunun hemen altında görünür.

Görevi CHPP bağlantısı aktifken mevcut takımın oyuncu snapshot'ını JSON olarak indirmektir. Özellikle oyuncu adı/ID, yetenek seviyeleri, form, deneyim, sadakat ve sakatlık seviyesi gibi V5'in kullandığı oyuncu alanlarını gelecekte modelleme, kalibrasyon ve regresyon çalışmalarında tekrar kullanmak için dışarı alır.

Bu özellik **geliştirme/veri toplama yardımcısıdır**. Maç emri yazmaz, match order değiştirmez ve OAuth token/secret bilgisini JSON'a koymaz.

## UI konumu

Ana analiz kartında:

```text
ANALİZİ ÇALIŞTIR
↓
👥 TAKIM + OYUNCU JSON AL
```

DOM buton ID:

`v5TeamPlayerChppExport`

Anchor button ID:

`analyze`

Frontend dosyası:

`HattrickAI_V5/wwwroot/team-player-chpp-export.js`

## CHPP bağlantı şartı

Buton sayfaya her durumda eklenir ancak **CHPP bağlı değilken disabled** kalır.

Durum kontrolü:

`GET /api/v5/status`

`connected === true` olduğunda buton aktif olur.

Frontend access token veya OAuth secret okumaz. Browser yalnızca mevcut HTTP session üzerinden status endpoint'ini kontrol eder.

## Veri kaynağı

Buton için özel ve **hafif bir backend endpoint'i** kullanılır:

`GET /api/v5/team-player-export`

Bu endpoint yalnızca iki CHPP çağrısı yapar:

1. `teamdetails` v3.0 — takım ID ve takım adını almak için.
2. `players` v1.3 — mevcut takımın oyuncularını almak için.

Böylece butona basıldığında `/api/v5/offline-export` gibi tam offline/regression paketinin maç, rakip, lineup ve analiz verileri tekrar çekilmez. Bu özellikle mobil kullanımda önemlidir.

Backend sınıfı:

`HattrickAI_V5/Core/TeamPlayerChppExportService.cs`

Oyuncular mevcut V5 `Player` modeliyle aynı alanlara normalize edilir. Takımın trainer/coach PlayerID'si oyuncu listesinden çıkarılır.

Oyuncu alanları mevcut V5 modelindeki gerçek alanlarla sınırlıdır:

- `id`
- `name`
- `keeper`
- `defending`
- `playmaking`
- `passing`
- `winger`
- `scoring`
- `stamina`
- `form`
- `experience`
- `loyalty`
- `injuryLevel`
- `specialty`
- `setPiecesSkill`

## İndirme yöntemi

Endpoint JSON'u doğrudan HTTP **attachment** olarak döndürür:

`Content-Disposition: attachment`

Frontend bu nedenle `fetch() + Blob + <a>.click()` yöntemi kullanmaz. Mobil tarayıcılarda async `fetch()` sonrasında programatik Blob indirmesinin engellenebilmesi nedeniyle buton doğrudan `/api/v5/team-player-export` adresine yönlenir.

Bu davranış özellikle Android mobil testinde korunmalıdır.

## İndirilen JSON

Dosya adı:

`hattrickai-team-players-YYYY-MM-DDTHH-mm-ss-fffZ.json`

Şema:

```json
{
  "schema": "hattrickai-v5-team-player-chpp-v1",
  "exportedAt": "...",
  "source": "CHPP",
  "security": {
    "credentialsIncluded": false,
    "oauthTokensIncluded": false,
    "sessionCookiesIncluded": false,
    "rawChppXmlIncluded": false
  },
  "team": {
    "teamId": 0,
    "teamName": "...",
    "playerCount": 0
  },
  "players": [],
  "sourceSnapshot": {
    "build": "...",
    "playerSource": "CHPP players v1.3",
    "trainerExcluded": true
  }
}
```

## Güvenlik

JSON içine özellikle şunlar alınmaz:

- OAuth access token
- OAuth access secret
- CHPP consumer secret
- browser session cookie
- ham `teamdetails.xml` / `players.xml`

CHPP tokenları yalnızca backend session tarafında kalır.

## Kod işaretleri / removal marker

Bu özellikte ortak removal marker:

`TEAM_PLAYER_CHPP_JSON_EXPORT_V1`

Bu marker hem frontend hem backend kodunda ve bu dokümanda aranabilir.

İlgili diğer sabitler:

- `v5TeamPlayerChppExport`
- `/api/v5/team-player-export`
- `team-player-chpp-export.js`

## İleride siteden tamamen kaldırma prosedürü

Bu özellik kaldırılacağı zaman repository'de önce şu aramayı yap:

```text
TEAM_PLAYER_CHPP_JSON_EXPORT_V1
v5TeamPlayerChppExport
/api/v5/team-player-export
team-player-chpp-export.js
```

### 1. Frontend dosyasını sil

Sil:

`HattrickAI_V5/wwwroot/team-player-chpp-export.js`

### 2. Dockerfile'dan COPY satırını sil

`HattrickAI_V5/Dockerfile` içinde şu satırı kaldır:

```dockerfile
COPY HattrickAI_V5/wwwroot/team-player-chpp-export.js /app/wwwroot/team-player-chpp-export.js
```

### 3. Dockerfile'dan script injection'ı sil

`HattrickAI_V5/Dockerfile` içindeki `</body>` injection satırından şu parçayı kaldır:

```html
<script src="/team-player-chpp-export.js?v=1"></script>
```

### 4. Workflow JS syntax check satırını kaldır

Workflow'a bu helper için syntax kontrolü eklendiyse şu satırı kaldır:

```bash
node --check HattrickAI_V5/wwwroot/team-player-chpp-export.js
```

### 5. Backend endpoint'i kaldır

`HattrickAI_V5/Program.cs` içindeki şu blok kaldırılmalı:

```csharp
// TEAM_PLAYER_CHPP_JSON_EXPORT_V1: lightweight DEV data collection endpoint.
app.MapGet("/api/v5/team-player-export", ...);
```

### 6. Backend servis sınıfını sil

Sil:

`HattrickAI_V5/Core/TeamPlayerChppExportService.cs`

### 7. Bu dokümanı sil

Sil:

`HattrickAI_V5/Docs/TEAM_PLAYER_CHPP_JSON_EXPORT.md`

### 8. `OfflineExportService` kaldırılmamalı

Bu buton artık `OfflineExportService` kullanmadığı için kaldırma sırasında `OfflineExportService` veya `/api/v5/offline-export` özelliğine dokunulmaz. Bunlar başka offline/regression işleri için kullanılabilir.

## Test / kabul

Deploy sonrasında:

1. CHPP bağlı değilken buton pasif.
2. CHPP bağlandıktan sonra buton aktif.
3. Butona basınca kısa CHPP veri toplama işlemi başlar.
4. Mobil tarayıcı JSON attachment indirmesini başlatır.
5. JSON'da takım bilgisi bulunur.
6. JSON'da oyuncu sayısı ve yetenek/form alanları bulunur.
7. Trainer oyuncu olarak export edilmez.
8. JSON'da OAuth secret/token bulunmaz.
9. Buton match order write işlemi yapmaz.
