# 訪客註冊系統 (Register)

訪客註冊系統的前端應用程式，提供訪客註冊、電子信箱驗證、QRCode 生成等功能。

## 功能特色

- **註冊表單**：填寫姓名、電子信箱、電話、公司/單位等資訊
- **電子信箱驗證**：發送 OTP 驗證碼至電子信箱，確保資料正確性
- **OTP 管理**：
  - OTP 有效期 10 分鐘
  - 發送冷卻期 60 秒
  - 即時倒計時顯示
- **QRCode 註冊**：
  - 生成 QRCode 供數位憑證皮夾 App 掃描
  - QRCode 有效期 5 分鐘
  - 提供直接連結開啟數位憑證皮夾 App
- **狀態輪詢**：自動輪詢註冊狀態，即時顯示註冊結果
- **現代化 UI**：使用 Tailwind CSS 和 shadcn/ui 組件庫，響應式設計

## 技術堆疊

- **框架**：React 19 + TypeScript
- **建置工具**：Vite 7
- **樣式**：Tailwind CSS 4
- **UI 組件**：shadcn/ui (Radix UI)
- **圖標**：Lucide React
- **工具函式**：
  - `clsx` - 條件式 class 名稱
  - `tailwind-merge` - Tailwind class 合併
  - `class-variance-authority` - 組件變體管理
  - `uuid` - UUID 生成

## 環境需求

- Node.js >= 18.0.0
- npm >= 9.0.0

## 安裝

```bash
# 安裝依賴
npm install
```

## 開發

```bash
# 啟動開發伺服器
npm run dev

# 開發伺服器預設運行在 http://localhost:5173
```

## 建置

```bash
# 建置生產版本
npm run build

# 建置產物會輸出至 ../../backend/public/register
```

## 預覽

```bash
# 預覽建置後的應用程式
npm run preview
```

## 程式碼品質

```bash
# 檢查程式碼風格
npm run lint

# 格式化程式碼
npm run format

# 檢查格式化
npm run format:check
```

## 專案結構

```
register/
├── public/                 # 靜態資源
│   ├── androidQrcode.png  # Android App QRCode
│   ├── iosQrcode.png      # iOS App QRCode
│   ├── googleplay.png     # Google Play 圖示
│   └── applestore.png     # App Store 圖示
├── src/
│   ├── components/         # React 組件
│   │   ├── ui/            # shadcn/ui 基礎組件
│   │   │   ├── alert.tsx
│   │   │   ├── badge.tsx
│   │   │   ├── button.tsx
│   │   │   ├── card.tsx
│   │   │   ├── field.tsx
│   │   │   ├── input.tsx
│   │   │   ├── label.tsx
│   │   │   └── separator.tsx
│   │   ├── QRCodeSelection.tsx      # QRCode 選擇組件
│   │   ├── RegistrationForm.tsx    # 註冊表單組件
│   │   └── RegistrationNotice.tsx  # 註冊須知組件
│   ├── lib/
│   │   └── utils.ts       # 工具函式
│   ├── App.tsx            # 主應用程式組件
│   ├── App.css            # 應用程式樣式
│   ├── index.css          # 全域樣式
│   └── main.tsx           # 應用程式入口
├── components.json        # shadcn/ui 配置
├── vite.config.ts         # Vite 配置
├── tsconfig.json          # TypeScript 配置
└── package.json           # 專案依賴與腳本
```

## API 端點

應用程式使用以下 API 端點（預設基礎 URL：`/api`）：

### 發送 OTP
- **端點**：`POST /register/otp`
- **請求體**：
  ```json
  {
    "email": "user@example.com"
  }
  ```
- **限制**：每分鐘僅能發送一次

### 送出註冊並獲取 QRCode
- **端點**：`POST /register/qrcode`
- **請求體**：
  ```json
  {
    "name": "姓名",
    "email": "user@example.com",
    "phone": "0912345678",
    "company": "公司名稱",
    "otp": "123456"
  }
  ```
- **回應**：
  ```json
  {
    "message": "QRCode generated",
    "transactionId": "uuid",
    "qrcodeImage": "base64_image",
    "authUri": "https://..."
  }
  ```

### 查詢註冊狀態
- **端點**：`GET /register/result?transactionId={transactionId}`
- **輪詢頻率**：每 2 秒一次
- **回應**：
  ```json
  {
    "message": "Registration successful"
  }
  ```

## 使用流程

1. **查看註冊須知**：了解需要下載數位憑證皮夾 App
2. **填寫註冊表單**：
   - 輸入姓名、電子信箱、電話、公司/單位
   - 點擊「驗證」按鈕發送 OTP 至電子信箱
   - 輸入收到的 6 位數驗證碼
3. **送出註冊**：點擊「送出註冊」按鈕
4. **完成驗證**：
   - 掃描顯示的 QRCode，或
   - 點擊連結直接開啟數位憑證皮夾 App
5. **等待確認**：系統自動輪詢註冊狀態，顯示成功訊息

## 開發注意事項

- 建置產物會自動輸出至 `backend/public/register` 目錄
- 使用 `@/` 別名指向 `src/` 目錄
- UI 組件使用 shadcn/ui 的 New York 風格


