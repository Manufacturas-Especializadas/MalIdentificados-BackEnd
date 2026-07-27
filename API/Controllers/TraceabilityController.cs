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
        public async Task<ActionResult<List<ValidationDto>>> GetValidations()
        {
            var query = new GetTraceabilityQuery();

            var result = await _mediator.Send(query);

            return Ok(result);
        }
    }
}