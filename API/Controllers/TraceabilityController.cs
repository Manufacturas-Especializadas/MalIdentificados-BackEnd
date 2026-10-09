using Application.Features.Traceability.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TraceabilityController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TraceabilityController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("validations")]
        public async Task<ActionResult<List<ValidationDto>>> GetValidations([FromQuery] int? lineId = null)
        {
            if (lineId is <= 0)
            {
                ModelState.AddModelError("lineId", "La línea debe ser un entero positivo.");
                return ValidationProblem(ModelState);
            }

            var query = new GetTraceabilityQuery(lineId);

            var result = await _mediator.Send(query, HttpContext.RequestAborted);

            return Ok(result);
        }
    }
}
