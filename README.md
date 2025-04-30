# 📚 Story Building Site – 後端服務（ASP.NET Core 8）

🔗 線上展示網址： [https://story-building-site-fe.vercel.app/](https://story-building-site-fe.vercel.app/)

此專案為一套**故事創作平台的後端服務**，以 ASP.NET Core 8 架構設計，搭配 RESTful API 提供使用者帳號驗證、故事資料儲存、畫布操作、角色與時間軸管理，並整合 PostgreSQL 與 Google Sheets 雲端儲存。  

---

## 🔧 技術架構

- **語言 / 框架**：C# / ASP.NET Core 8
- **驗證機制**：JWT（支援 Token 刷新）+ Google OAuth 2.0
- **資料儲存策略**：
  - PostgreSQL（註冊帳號用戶資料）
  - Google Sheets（Google 登入帳號資料）
- **API 文件產出工具**：Swagger / Swashbuckle
- **部署平台**：
  - Render（後端 API 與資料庫）
  - Vercel（Next.js 前端）

---

## ✅ 功能模組

- 使用者帳號管理（註冊 / 登入 / Token 驗證與刷新）
- 故事 CRUD 管理（支援軟刪除與還原）
- 畫布儲存功能：
  - 支援筆畫、圖片、標記、位置資訊
  - 可搭配 Google Drive 儲存圖片
- 角色模組：
  - 動態建立角色
  - 支援自定義屬性與角色關聯圖
- 時間軸模組：
  - 儲存歷史事件、年號、標籤
  - 支援視覺化時間軸設計
- SmartStorageService：
  - 自動依帳號來源切換儲存策略（PostgreSQL 或 Google Sheets）

---

## 💡 使用情境說明

使用者可註冊帳號或透過 Google 登入，進入系統後可建立故事並進行以下操作：

1. **新增故事**：建立創作專案
2. **管理角色**：設定人物屬性與關聯圖
3. **編輯畫布**：自由繪圖、標記地點與事件
4. **設定時間軸**：建立歷史事件，支援西元與年號
5. **資料自動儲存**：依登入方式自動寫入 PostgreSQL 或 Google Sheets
6. **支援還原與分享**：故事可軟刪除與透過網址分享

適合個人創作用途。

---

## 🚀 環境需求與本地執行

### 安裝需求

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- PostgreSQL 資料庫（可本地或 Render 部署）
- Google Sheets API 憑證檔案（`credentials.json`）

### 本地執行方式

```bash
dotnet restore
dotnet run --project BackendAPI
```

---

## ☁️ 雲端部署狀態
✅ 已部署至 Render（包含 API 與 PostgreSQL）

✅ 已與前端 API 串接驗證通過

✅ 前端部署於 Vercel，支援 RWD 設計與登入驗證

✅ 測試用帳號與情境皆通過 E2E 測試

---

## 📌 資安與最佳實務
- 密碼使用雜湊加鹽處理（Hash + Salt）

- 支援 JWT 驗證與自動刷新

- API 路由分層清晰，支援角色授權控制

- 規劃資料同步機制與快取策略（Google Sheets 與 PostgreSQL）

---

## 🙋‍♂️ 開發動機（作者簡介）

- 建構一個多模組、多資料來源整合的後端服務

- 熟悉雲端部署流程與 OAuth 身份整合實務

- 強化 API 設計能力與資安防護邏輯

- 提供一個可擴展的「故事創作平台」後台範例

---

## 📄 授權 License
本專案採用 MIT License。
歡迎自由參考、修改與商業應用，惟請保留原作者著作聲明。
