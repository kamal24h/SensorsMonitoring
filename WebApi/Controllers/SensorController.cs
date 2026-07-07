
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly IReadSensorDataService _readingService;
    private readonly ILogger<SensorController> _logger;
    private readonly IConfiguration _configuration;

    public SensorController(IReadSensorDataService readingService,
        IConfiguration configuration,
        ILogger<SensorController> logger)
    {
        _readingService = readingService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessFile([FromQuery] string? filePath)
    {
        var path = _configuration["SensorDataFile"];
        filePath ??= path;
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