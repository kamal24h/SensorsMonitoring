using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SensorMonitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorController(IReadSensorDataService _readingService,
        ILogger<SensorController> _logger) : Controller
    {

        private readonly string filePath = "./Resources/readings.jsonl";

        [HttpPost("process")]
        public async Task<IActionResult> ProcessFile()
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return BadRequest("File not found");

                await _readingService.ProcessFileAsync(filePath);
                var stats = await _readingService.GetProcessedStatsAsync();

                return Ok(new
                {
                    Message = "File processed successfully",
                    Statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file");
                return StatusCode(500, "Internal server error");
            }
        }        
    }
}
