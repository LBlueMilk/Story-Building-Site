using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System.Text;

namespace BackendAPI.Services.GoogleSheets
{
    public class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;

        public GoogleSheetsService(IConfiguration configuration)
        {
            GoogleCredential credential;

            // 雲端優先：從環境變數中讀取 JSON 字串
            var envJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");

            if (!string.IsNullOrWhiteSpace(envJson))
            {
                Console.WriteLine("[GoogleSheetsService] 使用環境變數中的 Google 憑證");
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(envJson));
                credential = GoogleCredential
                    .FromStream(stream)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);
            }
            else
            {
                // 本機開發端：讀取 appsettings.Development.json 中的路徑
                var credentialsPath = configuration["GoogleSheets:CredentialsPath"];

                if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
                {
                    throw new FileNotFoundException("無法找到 Google Sheets 憑證檔案", credentialsPath);
                }

                Console.WriteLine($"[GoogleSheetsService] 使用本機憑證路徑: {credentialsPath}");
                credential = GoogleCredential
                    .FromFile(credentialsPath)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            _sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "StoryBuildingSite"
            });
        }

        public SheetsService GetSheetsService()
        {
            return _sheetsService;
        }
    }
}
