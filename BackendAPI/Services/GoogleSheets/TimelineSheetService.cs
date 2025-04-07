using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using BackendAPI.Models;

namespace BackendAPI.Services.GoogleSheets
{
    public class TimelineSheetService
    {
        private readonly SheetsService _sheetsService;      // Google Sheets API 服務
        private readonly string _spreadsheetId;             // 試算表 ID
        private readonly string _sheetName;                 // 工作表名稱（時間軸分頁）

        // 建構子，注入 Sheets API 服務與設定值
        public TimelineSheetService(GoogleSheetsService googleSheetsService, IConfiguration configuration)
        {
            // 取得 Google Sheets API 實體
            _sheetsService = googleSheetsService.GetSheetsService();
            // 從 appsettings.json 讀取
            _spreadsheetId = configuration["GoogleSheets:SpreadsheetId"];
            // 時間軸對應的工作表名稱
            _sheetName = configuration["GoogleSheets:TimelineSheetName"];
        }


        // 取得指定 storyId 的時間軸 JSON 資料。
        public async Task<string?> GetTimelineJsonAsync(string storyId, string userId)
        {
            // 從第 2 列開始讀取 A 欄（storyId）與 B 欄（userId）與 C 欄（json）
            var range = $"{_sheetName}!A2:C";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var rows = response.Values;
            // 若資料為空則直接回傳 null
            if (rows == null || rows.Count == 0)
                return null;

            // 尋找指定 storyId 的資料列
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

        // 儲存 timeline json（若存在則更新，否則新增）
        public async Task<bool> SaveTimelineJsonAsync(string storyId, string userId, string json, DateTime lastModified)
        {
            var range = $"{_sheetName}!A2:D";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values ?? new List<IList<object>>();

            // 尋找 rowIndex
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

            // 要寫入的資料格式：storyId + userId + json + lastModified
            if (rowIndex >= 0)
            {
                var updateRange = $"{_sheetName}!C{rowIndex + 2}:D{rowIndex + 2}";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> {
                new List<object> { json, lastModified.ToString("o") }
            }
                };
                var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, updateRange);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();
            }
            else
            {
                var appendRequest = _sheetsService.Spreadsheets.Values.Append(new ValueRange
                {
                    Values = new List<IList<object>> {
                new List<object> { storyId, userId, json, lastModified.ToString("o") }
            }
                }, _spreadsheetId, $"{_sheetName}!A:D");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();
            }

            return true;
        }
    }
}