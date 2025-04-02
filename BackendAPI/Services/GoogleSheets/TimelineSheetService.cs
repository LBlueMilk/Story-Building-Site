using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;

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
        public async Task<string?> GetTimelineJsonAsync(string storyId)
        {
            var range = $"{_sheetName}!A2:B";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            if (values == null || values.Count == 0)
                return null;

            // 尋找符合 storyId 的資料列
            var row = values.FirstOrDefault(r => r.Count > 0 && r[0]?.ToString() == storyId);
            return row?.Count >= 2 ? row[1]?.ToString() : null;
        }

        // 寫入或更新指定 storyId 的時間軸 JSON。
        // 若存在則更新，否則新增。
        public async Task<bool> SaveTimelineJsonAsync(string storyId, string json)
        {
            var range = $"{_sheetName}!A2:B";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values ?? new List<IList<object>>();

            // 尋找 rowIndex
            int rowIndex = -1;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Count > 0 && values[i][0]?.ToString() == storyId)
                {
                    rowIndex = i;
                    break;
                }
            }

            // 要寫入的資料格式：storyId + json
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { new List<object> { storyId, json } }
            };

            if (rowIndex == -1)
            {
                // 新增一列
                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();
            }
            else
            {
                // 更新原有列
                var updateRange = $"{_sheetName}!A{rowIndex + 2}:B{rowIndex + 2}";
                var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, updateRange);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();
            }

            return true;
        }
    }
}