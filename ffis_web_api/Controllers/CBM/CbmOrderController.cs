using ffis_web_api.Models.CBM;
using ffis_web_api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ffis_web_api.Controllers.CBM
{
    [Route("api/cbm/orders")]
    [ApiController]
    public class CbmOrderController : ControllerBase
    {
        private readonly ICbmRepository _repository;

        public CbmOrderController(ICbmRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("active/{userId}")]
        public async Task<IActionResult> GetActiveOrders(int userId)
        {
            var activeOrders = await _repository.GetUserActiveOrdersAsync(userId);
            return Ok(activeOrders);
        }

        [HttpPost("request")]
        public async Task<IActionResult> CreateOrder([FromBody] CbmCreateOrderRequest request)
        {
            var orderNumber = await _repository.CreateOrderAsync(
                request.UserId, 
                request.ServiceType, 
                request.PickupLocation, 
                request.DropoffLocation
            );
            
            return Ok(new { Success = true, Message = "Order created successfully", OrderNumber = orderNumber });
        }
    }
}
