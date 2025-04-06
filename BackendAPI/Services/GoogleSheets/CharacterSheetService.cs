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
        public async Task<string?> GetCharacterJsonAsync(string storyId, string userId)
        {
            // 從第 2 列開始讀取 A 欄（storyId）與 B 欄（userId）與 C 欄（json）
            var range = $"{_sheetName}!A2:C";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var rows = response.Values;
            // 若資料為空則直接回傳 null
            if (rows == null || rows.Count == 0)
                return null;

            foreach (var row in rows)
            {
                if (row.Count >= 3 &&
                    row[0]?.ToString() == storyId &&
                    row[1]?.ToString() == userId)
                {
                    return row[2]?.ToString();
                }
            }

            return null;
        }

        // 更新或新增角色資料
        public async Task<bool> SaveCharacterJsonAsync(string storyId, string userId, string json)
        {
            // 從第 2 列開始讀取 A 欄（storyId）與 B 欄（userId）與 C 欄（json）
            var range = $"{_sheetName}!A2:C";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values ?? new List<IList<object>>();

            // 尋找 storyId 對應的資料列索引（rowIndex）
            int rowIndex = -1;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Count >= 2 &&
                    values[i][0]?.ToString() == storyId &&
                    values[i][1]?.ToString() == userId)
                {
                    rowIndex = i;
                    break;
                }
            }

            // 要寫入的資料格式：storyId + userId + json
            if (rowIndex >= 0)
            {
                // 更新已存在的資料
                var updateRange = $"{_sheetName}!C{rowIndex + 2}";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { json } }
                };
                var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, updateRange);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();
            }
            else
            {
                // 新增新的資料
                var appendRequest = _sheetsService.Spreadsheets.Values.Append(new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { storyId, userId, json } }
                }, _spreadsheetId, $"{_sheetName}!A:C");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();
            }

            return true;
        }
    }
}