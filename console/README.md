# Console - Scheduled Job Service

背景排程服務，用於同步 Google Calendar 和 Google People API 資料到資料庫。

## 功能

- **Google Calendar 同步**：定期從 Google Calendar API 取得事件資料並寫入資料庫
- **Google People 同步**：定期從 Google People API 取得聯絡人資料並寫入資料庫
- **排程執行**：預設每小時執行一次同步任務

## 專案結構

```
console/
├── Program.cs                    # 應用程式入口點
├── Services/
│   ├── IGoogleCalendarService.cs # Google Calendar 服務介面
│   ├── GoogleCalendarService.cs  # Google Calendar 服務實作
│   ├── IGooglePeopleService.cs   # Google People 服務介面
│   ├── GooglePeopleService.cs    # Google People 服務實作
│   └── ScheduledJobService.cs   # 排程任務服務
├── appsettings.json              # 設定檔
└── Dockerfile                    # Docker 建置檔
```

## 設定

### 認證檔案

此專案使用 OAuth2 認證方式：

1. **client_secret.json**：從 Google Cloud Console 下載的 OAuth2 憑證檔案
   - 放在 `console/` 目錄下

2. **token.json**：OAuth2 授權後產生的 token 檔案
   - 放在 `console/` 目錄下

### appsettings.json

```json
{
  "Google": {
    "ClientSecretPath": "client_secret.json",
    "TokenPath": "token.json",
    "CalendarId": "primary",
    "SyncIntervalHours": 1,
    "Scopes": [
      "https://www.googleapis.com/auth/calendar.readonly",
      "https://www.googleapis.com/auth/directory.readonly"
    ]
  },
  "Database": {
    "ConnectionString": ""
  }
}
```

## 執行方式

### Docker Compose（推薦）

使用 Docker Compose 啟動背景排程服務，每小時自動執行一次：

```bash
# 從專案根目錄執行
docker-compose up console
```

### 本地開發（需要安裝 .NET SDK）

如果本地有安裝 .NET SDK，也可以直接執行：

```bash
cd console
dotnet run
```

## 注意事項

- 此服務應維持單一實例執行，避免重複觸發排程任務
- 在 `docker-compose.yml` 中不應對 `console` 服務進行 scale
- Google API 認證需要設定 Service Account 或 OAuth2

