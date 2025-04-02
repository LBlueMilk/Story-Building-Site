using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;


namespace BackendAPI.Services.GoogleSheets
{
    public class CanvasSheetService
    {
        private readonly SheetsService _sheetsService;

        private readonly string _spreadsheetId;     // 試算表 ID
        private readonly string _sheetName;         // 工作表名稱（Canvas 分頁）

        // 建構子，注入 Sheets API 服務與設定值
        public CanvasSheetService(GoogleSheetsService googleSheetsService, IConfiguration configuration)
        {
            // 取得 Google Sheets API 實體
            _sheetsService = googleSheetsService.GetSheetsService();
            // 從 appsettings.json 讀取
            _spreadsheetId = configuration["GoogleSheets:SpreadsheetId"];
            // Canvas 對應的工作表名稱
            _sheetName = configuration["GoogleSheets:CanvasSheetName"]; 
        }

        // 讀取指定 storyId 的 Canvas JSON 資料
        public async Task<string?> GetCanvasJsonAsync(string storyId)
        {
            // 只取第 2 列開始的 A（storyId）和 B（json）欄
            var range = $"{_sheetName}!A2:B";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();

            var rows = response.Values;
            if (rows == null || rows.Count == 0) return null;
            // 遍歷所有列，比對 storyId，找到即回傳 json 欄位
            foreach (var row in rows)
            {
                if (row.Count >= 2 && row[0]?.ToString() == storyId)
                {
                    return row[1]?.ToString();
                }
            }

            return null;
        }

        // 更新或新增畫布資料
        public async Task<bool> SaveCanvasJsonAsync(string storyId, string json)
        {
            var range = $"{_sheetName}!A2:B";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();

            var values = response.Values ?? new List<IList<object>>();
            // 尋找對應 storyId 是否已存在
            int rowIndex = -1;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Count >= 1 && values[i][0].ToString() == storyId)
                {
                    rowIndex = i;
                    break;
                }
            }

            if (rowIndex >= 0)
            {
                // 更新
                var updateRange = $"{_sheetName}!B{rowIndex + 2}";
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
                // 新增
                var appendRequest = _sheetsService.Spreadsheets.Values.Append(new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { storyId, json } }
                }, _spreadsheetId, $"{_sheetName}!A:B");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();
            }

            return true;
        }
    }
}
