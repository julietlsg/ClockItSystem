using Microsoft.AspNetCore.Mvc;
using libzkfpcsharp;

namespace ClockItSystem.ScannerAgent.Controllers;

[ApiController]
[Route("api/fingerprint")]
public class FingerprintController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        try
        {
            int result = zkfp2.Init();

            return Ok(new
            {
                success = result == zkfperrdef.ZKFP_ERR_OK,
                initResult = result
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("device")]
    public IActionResult Device()
    {
        try
        {
            zkfp2.Init();

            int count = zkfp2.GetDeviceCount();

            return Ok(new
            {
                success = true,
                deviceCount = count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("open")]
    public IActionResult Open()
    {
        try
        {
            zkfp2.Init();

            IntPtr devHandle = zkfp2.OpenDevice(0);

            return Ok(new
            {
                success = devHandle != IntPtr.Zero,
                handle = devHandle.ToInt64()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("db")]
    public IActionResult Database()
    {
        try
        {
            zkfp2.Init();

            IntPtr dbHandle = zkfp2.DBInit();

            return Ok(new
            {
                success = dbHandle != IntPtr.Zero,
                handle = dbHandle.ToInt64()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("capture")]
    public async Task<IActionResult> Capture()
    {
        var result = await _fingerprintService.CaptureAsync();

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}