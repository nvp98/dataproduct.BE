using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MockDatasController : ControllerBase
    {
        [HttpGet("machines")]
        public IActionResult GetMachines([FromQuery] string shiftCode)
        {
            // Dữ liệu mẫu
            var machines = new List<object> {
                new { machineCode = "M01", machineName = "Máy Cắt 01",shift ="1A" },
                new { machineCode = "M02", machineName = "Máy Dập 02",shift ="2A" },
                new { machineCode = "M03", machineName = "Máy Ép 03",shift ="3A" }
            };
            // Nếu có truyền param shiftCode thì lọc
            if (!string.IsNullOrEmpty(shiftCode))
            {
                machines = machines
                    .Where(x => (string)x.GetType().GetProperty("shift").GetValue(x) == shiftCode)
                    .ToList();
            }

            // (Tùy bạn lọc theo shiftCode nếu muốn)
            return Ok(machines);
        }

    }
}
