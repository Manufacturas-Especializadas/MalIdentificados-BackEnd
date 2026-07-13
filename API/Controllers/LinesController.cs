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

        [HttpGet("getLines")]
        public async Task<ActionResult<List<LineDto>>> GetLines()
        {
            var result = await _mediator.Send(new GetLinesQuery());
            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<ActionResult<int>> CreateLine([FromBody] CreateLineCommand command)
        {
            var lineId = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetLines), new { id = lineId }, lineId);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateLine(int id, [FromBody] UpdateLineCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest("El ID de la ruta no coincide con el ID del objeto.");
            }

            var success = await _mediator.Send(command);

            if (!success)
            {
                return NotFound($"No se encontró la línea con el ID {id}.");
            }

            return NoContent();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteLine(int id)
        {
            var success = await _mediator.Send(new DeleteLineCommand(id));

            if (!success)
            {
                return NotFound($"No se encontró la línea con el ID {id}.");
            }

            return NoContent();
        }
    }
}