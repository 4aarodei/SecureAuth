# SecureAuth

Невеликий ASP.NET Core 8 API для тестового завдання: двоетапна автентифікація з підписом кожного запиту через `ApiSignature`.

## Що реалізовано

- `POST /auth/login` перевіряє логін і пароль, після чого видає короткоживучий `simpleToken`.
- `POST /auth/token` одноразово обмінює `simpleToken` на довгоживучий `fullToken`.
- `POST /auth/logout` видаляє активний `fullToken`.
- Усі `/auth/*` запити спочатку проходять перевірку `ApiSignature` і `requestDate`.
- Токени є opaque-рядками, не JWT.
- Пароль demo-користувача зберігається як PBKDF2-SHA256 hash.
- Сховище токенів є thread-safe і автоматично чистить протерміновані токени у background service.
- Помилки повертаються в одному JSON-форматі.

## Запуск

Потрібен .NET SDK 8.

```powershell
dotnet restore SecureAuth.slnx
dotnet run --project src/SecureAuth/SecureAuth.csproj
```

Swagger у Development доступний після запуску за адресою з `launchSettings.json`, наприклад:

```text
http://localhost:5015/swagger
```

## Тести

```powershell
dotnet test SecureAuth.slnx
```

Тести покривають:

- успішний login;
- одноразове використання `simpleToken`;
- logout для `fullToken`;
- невалідний і застарілий `ApiSignature`;
- конкурентне споживання токена;
- валідацію security-конфігурації;
- інтеграційні сценарії API.

## Demo user

Локальна конфігурація містить demo-користувача з PBKDF2-SHA256 hash у `Security:SeedUsers`.
Plain text пароль не зберігається в репозиторії; для локальної перевірки використайте власний пароль і відповідний hash у локальній конфігурації або env-змінній.

```text
login: demo
```

## ApiSignature

Підпис обчислюється так:

```text
ApiSignature = SHA-256(StaticKey + RequestDate)
```

Де:

- `StaticKey` береться з `Security:StaticKey`;
- `RequestDate` це Unix timestamp у мілісекундах UTC;
- `ApiSignature` передається як hex-рядок SHA-256.

PowerShell-приклад:

```powershell
$requestDate = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$payload = "dev-static-key-change-me$requestDate"
$apiSignature = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($payload)
    )
).ToLowerInvariant()

"requestDate=$requestDate"
"apiSignature=$apiSignature"
```

`requestDate` без перевірки freshness не захищає від replay-атак. Якщо зловмисник перехопить підписаний запит, він зможе повторити його пізніше з тим самим timestamp і підписом. Тому сервер відхиляє запити, час яких відрізняється від поточного UTC більше ніж на налаштоване вікно, за замовчуванням 5 хвилин.

## Endpoints

У прикладах нижче `requestDate` треба замінити на поточний Unix timestamp у мілісекундах, а `apiSignature` - на SHA-256 від `StaticKey + requestDate`.

### POST /auth/login

```json
{
  "login": "demo",
  "password": "your-local-password",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Успішна відповідь:

```json
{
  "token": "simple-token",
  "expiresAt": "2026-05-18T12:00:00+00:00"
}
```

### POST /auth/token

```json
{
  "simpleToken": "simple-token",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Успішна відповідь:

```json
{
  "token": "full-token",
  "expiresAt": "2026-05-19T12:00:00+00:00"
}
```

### POST /auth/logout

```json
{
  "fullToken": "full-token",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Успішна відповідь: `200 OK`.

## Формат помилок

```json
{
  "error": "invalid_signature",
  "message": "Request signature is invalid."
}
```

## Додаткова документація

- [Технічне завдання](docs/TECH_TASK.md)
- [Діаграми та карта файлів](docs/ArchitectureDiagrams.md)
