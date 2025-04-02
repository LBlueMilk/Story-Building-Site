using BackendAPI.Services.GoogleSheets;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    //測試用的 Controller，用來測試 Google Sheets 連線是否正常
    [ApiController]
    [Route("api/test/sheets")]
    public class SheetsTestController : ControllerBase
    {
        private readonly GoogleSheetsService _googleSheetsService;

        public SheetsTestController(GoogleSheetsService googleSheetsService)
        {
            _googleSheetsService = googleSheetsService;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            try
            {
                var service = _googleSheetsService.GetSheetsService();
                var appName = service.ApplicationName;
                return Ok(new { status = "ok", application = appName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "fail", message = ex.Message });
            }
        }
    }
}
