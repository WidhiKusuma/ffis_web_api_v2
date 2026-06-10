using ffis_web_api.Models.CBM;
using ffis_web_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ffis_web_api.Controllers.CBM
{
    [Route("api/cbm/admin")]
    [ApiController]
    public class CbmAdminController : ControllerBase
    {
        private readonly ICbmRepository _repository;

        public CbmAdminController(ICbmRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("pending-orders")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var pendingOrders = await _repository.GetPendingOrdersAsync();
            return Ok(pendingOrders);
        }

        [HttpPost("assign-driver")]
        public async Task<IActionResult> AssignDriver([FromBody] dynamic request)
        {
            int orderId = request.GetProperty("orderId").GetInt32();
            int driverId = request.GetProperty("driverId").GetInt32();

            var success = await _repository.AssignDriverAsync(orderId, driverId);
            if (success)
            {
                return Ok(new { Success = true, Message = "Driver successfully assigned to order." });
            }
            
            return BadRequest(new { Success = false, Message = "Failed to assign driver. Order may no longer be pending." });
        }
    }
}
