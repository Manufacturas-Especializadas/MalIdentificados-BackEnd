using Application.Features.Scanning.Commands;
using Application.Features.Scanning.Query;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScanningController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ScanningController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("validate-approver/{payrollNumber}")]
        public async Task<ActionResult<bool>> ValidateApprover(int payrollNumber)
        {
            var isValid = await _mediator.Send(new ValidateQualityApproverQuery(payrollNumber));
            return Ok(isValid);
        }

        [HttpPost("start")]
        public async Task<ActionResult<int>> StartValidation([FromBody] RegisterCompletedBatchCommand command)
        {
            var validationId = await _mediator.Send(command);

            return Ok(new { validationId, message = "Sesión iniciada correctamente." });
        }
    }
}