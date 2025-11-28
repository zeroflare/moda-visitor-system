# 訪客簽到系統 (Check-in System)

訪客簽到系統的前端應用程式，使用 React + TypeScript + Vite 建置。

此系統提供 QRCode 掃描簽到功能，訪客可透過數位憑證皮夾 App 進行身份驗證並完成簽到。

## 功能特色

- **QRCode 簽到**: 自動生成 QRCode，訪客掃描後透過數位憑證皮夾 App 完成身份驗證
- **自動更新**: QRCode 每 5 分鐘自動更新，確保安全性
- **即時輪詢**: 每 2 秒自動檢查簽到狀態，即時顯示簽到結果
- **響應式設計**: 支援桌面、平板及手機等各種裝置
- **使用者友善**: 清晰的簽到提醒與須知，協助訪客順利完成簽到

## 技術堆疊

- **React 19**: 前端框架
- **TypeScript**: 型別安全
- **Vite**: 建置工具與開發伺服器
- **UUID**: 生成唯一交易 ID

## 專案結構

```
checkin/
├── src/
│   ├── App.tsx          # 主要應用程式元件
│   ├── App.css          # 應用程式樣式
│   ├── main.tsx         # 應用程式入口
│   └── index.css        # 全域樣式
├── public/              # 靜態資源
├── index.html           # HTML 模板
├── vite.config.ts       # Vite 設定檔
├── tsconfig.json        # TypeScript 設定檔
└── package.json         # 專案依賴與腳本
```

## 安裝與執行

### 前置需求

- Node.js >= 18.0.0
- npm >= 9.0.0

### 安裝依賴

```bash
npm install
```

### 開發模式

啟動開發伺服器（支援熱模組替換）：

```bash
npm run dev
```

應用程式將在 `http://localhost:5173` 啟動。

### 建置生產版本

建置生產版本並輸出到後端 public 目錄：

```bash
npm run build
```

建置後的檔案會輸出到 `../../backend/public/checkin/`。

### 預覽生產版本

預覽建置後的生產版本：

```bash
npm run preview
```

## 開發指令

- `npm run dev` - 啟動開發伺服器
- `npm run build` - 建置生產版本
- `npm run preview` - 預覽生產版本
- `npm run lint` - 執行 ESLint 檢查
- `npm run format` - 使用 Prettier 格式化程式碼
- `npm run format:check` - 檢查程式碼格式
- `npm test` - 執行單元測試
- `npm run test:ui` - 以 UI 模式執行測試
- `npm run test:coverage` - 執行測試並產生覆蓋率報告

## API 整合

應用程式與後端 API 整合，使用以下端點：

### 取得 QRCode

```
GET /api/checkin/qrcode?transactionId={uuid}
```

**回應範例：**
```json
{
  "qrcodeImage": "data:image/png;base64,...",
  "authUri": "modadigitalwallet://authorize"
}
```

### 檢查簽到狀態

```
GET /api/checkin/result?transactionId={uuid}
```

**回應範例（簽到成功）：**
```json
{
  "inviterEmail": "inviter@example.com",
  "inviterName": "邀請者姓名",
  "inviterDept": "邀請者單位",
  "inviterTitle": "邀請者職稱",
  "vistorEmail": "visitor@example.com",
  "vistorName": "訪客姓名",
  "vistorDept": "訪客單位",
  "vistorPhone": "0912345678",
  "meetingTime": "2024-01-01 10:00",
  "meetingRoom": "會議室 A"
}
```

**回應範例（等待中）：**
```json
{
  "message": "等待驗證中"
}
```

## 應用程式流程

1. **初始化**: 應用程式載入時自動生成 UUID 作為 transactionId
2. **取得 QRCode**: 使用 transactionId 向後端 API 取得 QRCode 圖片
3. **顯示 QRCode**: 顯示 QRCode 及簽到提醒與須知
4. **倒計時**: QRCode 顯示 5 分鐘倒計時，時間到自動更新
5. **輪詢檢查**: 每 2 秒檢查一次簽到狀態
6. **顯示結果**: 簽到成功後顯示邀請者、訪客及會議資訊
7. **重新簽到**: 點擊「返回簽到」按鈕可重新開始簽到流程

## 環境變數

目前 API 基礎 URL 寫死在程式碼中：

```typescript
const API_BASE_URL = 'https://vistor.zeroflare.tw/api'
```

未來可考慮使用環境變數：

```env
VITE_API_BASE_URL=https://vistor.zeroflare.tw/api
```

## 測試

### 執行測試

```bash
npm test
```

### 以 UI 模式執行測試

```bash
npm run test:ui
```

### 產生測試覆蓋率報告

```bash
npm run test:coverage
```

測試報告會輸出到 `coverage/` 目錄。

## 程式碼規範

專案使用以下工具確保程式碼品質：

- **ESLint**: 程式碼檢查
- **Prettier**: 程式碼格式化
- **TypeScript**: 型別檢查

執行檢查：

```bash
npm run lint
npm run format:check
```
