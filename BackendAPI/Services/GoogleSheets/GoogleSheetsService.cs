using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace BackendAPI.Services.GoogleSheets
{
    public class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;

        public GoogleSheetsService(IConfiguration configuration)
        {
            var credentialsPath = configuration["GoogleSheets:CredentialsPath"];

            if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
            {
                throw new FileNotFoundException("無法找到 Google Sheets 憑證檔案", credentialsPath);
            }

            var credential = GoogleCredential
                .FromFile(credentialsPath)
                .CreateScoped(SheetsService.Scope.Spreadsheets);

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
