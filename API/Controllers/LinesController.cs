using Application.Features.Lines.Commands;
using Application.Features.Lines.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LinesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<LineDto>>> GetLines()
        {
            var result = await _mediator.Send(new GetLinesQuery());
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateLine([FromBody] CreateLineCommand command)
        {
            var lineId = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetLines), new { id = lineId }, lineId);
        }
    }
}