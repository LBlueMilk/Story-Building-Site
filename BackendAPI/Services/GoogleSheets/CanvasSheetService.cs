using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using BackendAPI.Models;
using BackendAPI.Application.DTOs;


namespace BackendAPI.Services.GoogleSheets
{
    /// <summary>
    /// 此服務僅適用於 Google 登入使用者，用於操作 Google Sheets 中的 Canvas 資料。
    /// Email 註冊使用者不會使用這個類別（他們的資料儲存在 PostgreSQL）。
    /// </summary>
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

        /// <summary>
        /// 讀取單列儲存的 Canvas JSON（僅供早期版本或小資料使用）
        /// </summary>
        public async Task<string?> GetCanvasJsonAsync(string storyId, string userId)
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

        /// <summary>
        /// 讀取單列儲存的 Canvas JSON 並附帶最後修改時間（僅供早期版本）
        /// </summary>
        public async Task<JsonWithModifiedDto?> GetCanvasWithLastModifiedAsync(string storyId, string userId)
        {
            var range = $"{_sheetName}!A2:D";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var rows = response.Values;

            if (rows == null || rows.Count == 0)
                return null;

            foreach (var row in rows)
            {
                if (row.Count >= 4 &&
                    row[0]?.ToString() == storyId &&
                    row[1]?.ToString() == userId)
                {
                    var json = row[2]?.ToString() ?? "";
                    var lastModifiedStr = row[3]?.ToString();

                    return new JsonWithModifiedDto
                    {
                        Json = json,
                        LastModifiedRaw = lastModifiedStr
                    };
                }
            }

            return null;
        }


        /// <summary>
        /// 儲存單筆 Canvas JSON（不支援分段，大量資料應使用 SaveCanvasChunksAsync）
        /// </summary>
        public async Task<bool> SaveCanvasJsonAsync(string storyId, string userId, string json, DateTime lastModified)
        {
            var range = $"{_sheetName}!A2:D";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values ?? new List<IList<object>>();

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
                // 更新已存在的資料
                var updateRange = $"{_sheetName}!C{rowIndex + 2}:D{rowIndex + 2}";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { json, lastModified.ToString("o") } }
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
                    Values = new List<IList<object>> {
                        new List<object> { storyId, userId, json, lastModified.ToString("o") }
                    }
                }, _spreadsheetId, $"{_sheetName}!A:D");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();
            }

            return true;
        }


        /// <summary>
        /// 分段儲存大量 Canvas JSON（建議使用）
        /// </summary>
        public async Task SaveCanvasChunksAsync(string storyId, string userId, string json, DateTime lastModified)
        {
            const int chunkSize = 40000;
            var chunks = Enumerable.Range(0, (json.Length + chunkSize - 1) / chunkSize)
                                   .Select(i => json.Substring(i * chunkSize, Math.Min(chunkSize, json.Length - i * chunkSize)))
                                   .ToList();

            // 先刪除舊的資料（storyId + userId）
            var existing = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{_sheetName}!A2:E").ExecuteAsync();
            var rows = existing.Values ?? new List<IList<object>>();
            int baseRow = 2;

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                var row = rows[i];
                if (row.Count >= 3 && row[0]?.ToString() == storyId && row[1]?.ToString() == userId)
                {
                    var deleteRange = new BatchUpdateSpreadsheetRequest
                    {
                        Requests = new List<Request>
                {
                    new Request
                    {
                        DeleteDimension = new DeleteDimensionRequest
                        {
                            Range = new DimensionRange
                            {
                                SheetId = await GetSheetIdAsync(), // 取得 Sheet ID
                                Dimension = "ROWS",
                                StartIndex = baseRow - 1 + i,
                                EndIndex = baseRow + i
                            }
                        }
                    }
                }
                    };
                    await _sheetsService.Spreadsheets.BatchUpdate(deleteRange, _spreadsheetId).ExecuteAsync();
                }
            }

            // Append 分段資料
            var valueRange = new ValueRange
            {
                Values = chunks.Select((chunk, index) => new List<object>
        {
            storyId, userId, index, chunk, lastModified.ToString("o")
        }).Cast<IList<object>>().ToList()
            };

            var append = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, $"{_sheetName}!A:E");
            append.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            await append.ExecuteAsync();
        }

        /// <summary>
        /// 合併讀取 Canvas 所有分段資料，重組為完整 JSON
        /// </summary>
        public async Task<JsonWithModifiedDto?> ReadCanvasChunksAsync(string storyId, string userId)
        {
            var range = $"{_sheetName}!A2:E";
            var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var rows = response.Values ?? new List<IList<object>>();

            var chunks = rows
                .Where(r => r.Count >= 5 && r[0]?.ToString() == storyId && r[1]?.ToString() == userId)
                .OrderBy(r => int.Parse(r[2].ToString()))
                .ToList();

            if (chunks.Count == 0)
                return null;

            string combinedJson = string.Join("", chunks.Select(r => r[3].ToString()));
            string lastModified = chunks.Last()[4].ToString();

            return new JsonWithModifiedDto
            {
                Json = combinedJson,
                LastModifiedRaw = lastModified
            };
        }

        private async Task<int> GetSheetIdAsync()
        {
            var spreadsheet = await _sheetsService.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == _sheetName);
            return sheet?.Properties.SheetId ?? throw new Exception("找不到指定工作表 ID");
        }
    }
}
