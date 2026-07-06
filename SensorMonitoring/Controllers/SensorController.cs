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

        [HttpGet("aggregate")]
        public async Task<IActionResult> GetAggregatedData( [FromQuery] string deviceId,
            [FromQuery] string metric, [FromQuery] DateTime from,
            [FromQuery] DateTime to, [FromQuery] int bucketSizeSeconds = 60)
        {
            try
            {
                if (from >= to)
                    return BadRequest("'from' must be before 'to'");

                if (bucketSizeSeconds <= 0)
                    return BadRequest("Bucket size must be greater than 0");

                var result = await _readingService.GetAggregatedDataAsync(deviceId, metric, from, to, bucketSizeSeconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting aggregated data");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}