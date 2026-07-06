//using Application.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace SensorMonitoring.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class SensorController1(IReadSensorDataService _readingService,
//        ILogger<SensorController1> _logger) : Controller
//    {

//        private readonly string filePath = "./Resources/readings.jsonl";

//        [HttpPost("process")]
//        public async Task<IActionResult> ProcessFile()
//        {
//            try
//            {
//                if (!System.IO.File.Exists(filePath))
//                    return BadRequest("File not found");

//                await _readingService.ProcessFileAsync(filePath);
//                var stats = await _readingService.GetProcessedStatsAsync();

//                return Ok(new
//                {
//                    Message = "File processed successfully",
//                    Statistics = stats
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error processing file");
//                return StatusCode(500, "Internal server error");
//            }
//        }

//        [HttpGet("aggregate")]
//        public async Task<IActionResult> GetAggregatedData(
//            [FromQuery] string deviceId,
//            [FromQuery] string metric,
//            [FromQuery] DateTime from,
//            [FromQuery] DateTime to,
//            [FromQuery] int bucketSizeSeconds = 60)
//        {
//            try
//            {
//                if (from >= to)
//                    return BadRequest("'from' must be before 'to'");

//                if (bucketSizeSeconds <= 0)
//                    return BadRequest("Bucket size must be greater than 0");

//                var result = await _readingService.GetAggregatedDataAsync(
//                    deviceId, metric, from, to, bucketSizeSeconds);

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting aggregated data");
//                return StatusCode(500, "Internal server error");
//            }
//        }

//        [HttpGet("stats")]
//        public async Task<IActionResult> GetStats()
//        {
//            try
//            {
//                var stats = await _readingService.GetProcessedStatsAsync();
//                return Ok(stats);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting stats");
//                return StatusCode(500, "Internal server error");
//            }
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;

namespace SensorMonitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly IReadSensorDataService _readingService;
    private readonly ILogger<SensorController> _logger;
    private readonly string defaultFilePath = "./Resources/readings.jsonl";

    public SensorController(IReadSensorDataService readingService, ILogger<SensorController> logger)
    {
        _readingService = readingService;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessFile([FromQuery] string? filePath)
    {
        filePath ??= defaultFilePath;
        try
        {
            _logger.LogInformation("Processing request for file: {FilePath}", filePath);

            if (string.IsNullOrEmpty(filePath))
                return BadRequest("File path is required");

            if (!System.IO.File.Exists(filePath))
                return BadRequest($"File not found: {filePath}");

            var stats = await _readingService.ProcessFileAsync(filePath);
            return Ok(new
            {
                Success = true,
                Message = "File processed successfully",
                Statistics = stats,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found: {FilePath}", filePath);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
            return StatusCode(500, new
            {
                Success = false,
                Message = "An error occurred while processing the file",
                Error = ex.Message
            });
        }
    }

    [HttpGet("aggregate")]
    public async Task<IActionResult> GetAggregatedData(
        [FromQuery] string deviceId,
        [FromQuery] string metric,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int bucketSizeSeconds = 60)
    {

        try
        {
            _logger.LogInformation("Aggregation request: Device={DeviceId}, Metric={Metric}, From={From}, To={To}, Bucket={Bucket}",
               deviceId, metric, from, to, bucketSizeSeconds);

            var result = await _readingService.GetAggregatedDataAsync(
                deviceId, metric, from, to, bucketSizeSeconds);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aggregated data");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            _logger.LogInformation("Getting processing statistics");
            var stats = await _readingService.GetProcessedStatsAsync();

            return Ok(new
            {
                Success = true,
                Statistics = stats,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An error occurred while getting statistics",
                Error = ex.Message
            });
        }
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetStats()
    {
        try
        {
            _logger.LogWarning("Resetting all statistics and data");
            await _readingService.ResetStatsAsync();

            return Ok(new
            {
                Success = true,
                Message = "All statistics and data have been reset",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting stats");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An error occurred while resetting statistics",
                Error = ex.Message
            });
        }
    }
}