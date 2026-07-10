using MediatR;

namespace Application.Features.Lines.Commands
{
    public record CreateLineCommand(string LineName) : IRequest<int>;
}
