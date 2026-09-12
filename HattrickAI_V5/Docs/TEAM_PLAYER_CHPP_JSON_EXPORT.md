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

Bu nedenle frontend tarafında access token veya OAuth secret okunmaz. Browser yalnızca mevcut HTTP session üzerinden `/api/v5/status` ve `/api/v5/offline-export` çağrılarını yapar.

## Veri kaynağı

V1 yeni bir backend CHPP endpoint'i eklemez.

Mevcut endpoint tekrar kullanılır:

`GET /api/v5/offline-export`

Mevcut `OfflineExportService` CHPP'den `players` verisini okuyup `normalized.ownPlayers` altında V5 `Player` modeline normalize eder. Bu export'ta ham CHPP XML ayrıca indirilecek JSON'a konmaz.

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
- `specialty` (modelde mevcutsa)
- `setPiecesSkill` (modelde mevcutsa)

## İndirilen JSON

Dosya adı:

`hattrickai-team-players-YYYY-MM-DDTHH-mm-ss-sssZ.json`

Şema:

```json
{
  "schema": "hattrickai-v5-team-player-chpp-v1",
  "exportedAt": "...",
  "source": "CHPP",
  "purpose": "...",
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
    "matchContext": {}
  }
}
```

## Neden mevcut `offline-export` tekrar kullanılıyor?

Böylece aynı CHPP oyuncu parser'ı ve aynı V5 `Player` modeli tekrar kullanılmaktadır. Yeni bir ikinci `players.xml` parser'ı oluşturulmaz; bu da iki farklı yerde oyuncu verisinin farklı yorumlanması riskini azaltır.

Dezavantajı: `/api/v5/offline-export` yalnız oyuncuları değil, mevcut offline test paketinin diğer verilerini de CHPP'den toplar. Frontend yalnızca `normalized.ownPlayers` ve gerekli takım bilgisini ayırıp yeni küçük JSON'u oluşturur. Bu nedenle butonun amacı hafif bir canlı API değildir; **geliştirme amaçlı snapshot alma aracıdır**.

## Güvenlik

JSON içine özellikle şunlar alınmaz:

- OAuth access token
- OAuth access secret
- CHPP consumer secret
- browser session cookie
- ham `teamdetails.xml` / `players.xml`

Button yalnızca browser'da oluşturulan indirilebilir JSON'u üretir.

## Kod işaretleri / removal marker

Kodun başında şu sabit removal marker bulunur:

`TEAM_PLAYER_CHPP_JSON_EXPORT_V1`

Frontend dosyasının hemen başındaki yorum, bu belgeyi kaldırma referansı olarak gösterir.

## İleride siteden tamamen kaldırma prosedürü

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

### 5. Bu dokümanı sil

Bu dosya da artık gerekli değil:

`HattrickAI_V5/Docs/TEAM_PLAYER_CHPP_JSON_EXPORT.md`

### 6. Backend'i silmek gerekmez

V1 yeni backend endpoint'i eklemediği için **`/api/v5/offline-export` ve `OfflineExportService` sırf bu buton yüzünden kaldırılmamalıdır**. Başka offline/regression kullanımları devam edebilir.

## Hızlı kaldırma kontrol listesi

Repository içinde arat:

```text
TEAM_PLAYER_CHPP_JSON_EXPORT_V1
v5TeamPlayerChppExport
team-player-chpp-export.js
👥 TAKIM + OYUNCU JSON AL
```

Arama sonucunda yalnızca eski dokümantasyon/artifact referansları kalıyorsa UI özelliği kaldırılmıştır.

## Test

CI'da JavaScript syntax regression adımında bu dosya `node --check` ile doğrulanmalıdır.

Deploy sonrasında kontrol:

1. CHPP bağlı değilken buton pasif.
2. CHPP bağlandıktan sonra buton aktif.
3. Butona basıldığında CHPP oyuncu verileri alınır.
4. JSON indirilir.
5. JSON'da oyuncu sayısı ve yetenek/form alanları bulunur.
6. JSON'da OAuth secret/token bulunmaz.
