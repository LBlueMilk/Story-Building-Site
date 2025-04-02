using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;

namespace BackendAPI.Services.GoogleSheets
{
    public class CharacterSheetService
    {
        private readonly SheetsService _sheetsService;   // Google Sheets API 服務實例
        private readonly string _spreadsheetId;          // 試算表 ID（整份 Google Sheets 檔案）
        private readonly string _sheetName;              // 工作表名稱（角色資料分頁）


        public CharacterSheetService(GoogleSheetsService googleSheetsService, IConfiguration configuration)
        {
            // 取得 Google Sheets API 實體
            _sheetsService = googleSheetsService.GetSheetsService();
            // 從 appsettings.json 讀取
            _spreadsheetId = configuration["GoogleSheets:SpreadsheetId"];
            // 角色資料對應的工作表名稱
            _sheetName = configuration["GoogleSheets:CharacterSheetName"];
        }

        // 讀取指定 storyId 的角色 JSON 資料
        public async Task<string?> GetCharacterJsonAsync(string storyId)
        {
            // 從第 2 列開始讀取 A 欄（storyId）與 B 欄（json）
            var range = $"{_sheetName}!A2:B";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            // 若資料為空則直接回傳 null
            if (values == null || values.Count == 0)
                return null;

            // 使用 LINQ 尋找第一筆符合的資料列
            var row = values.FirstOrDefault(r => r.Count > 0 && r[0]?.ToString() == storyId);
            // 若存在且包含 JSON 欄位，則回傳 JSON 字串
            return row?.Count >= 2 ? row[1]?.ToString() : null;
        }

        // 更新或新增角色資料
        public async Task<bool> SaveCharacterJsonAsync(string storyId, string json)
        {
            // 定義讀取範圍（從第 2 列開始，跳過標題列）
            var range = $"{_sheetName}!A2:B";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values ?? new List<IList<object>>();

            // 尋找 storyId 對應的資料列索引（rowIndex）
            int rowIndex = -1;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Count >= 1 && values[i][0].ToString() == storyId)
                {
                    rowIndex = i;
                    break;
                }
            }

            // 要寫入的資料格式：storyId + 對應的 json
            ValueRange valueRange = new()
            {
                Values = new List<IList<object>> { new List<object> { storyId, json } }
            };

            if (rowIndex == -1)
            {
                // 新增新的資料列
                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();
            }
            else
            {
                // 更新指定資料列
                string updateRange = $"{_sheetName}!A{rowIndex + 2}:B{rowIndex + 2}";
                var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, updateRange);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();
            }

            return true;
        }
    }
}