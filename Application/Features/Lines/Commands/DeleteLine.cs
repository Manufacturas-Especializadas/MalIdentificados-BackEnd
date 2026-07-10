using MediatR;

namespace Application.Features.Lines.Commands
{
    public record DeleteLineCommand(int Id) : IRequest<bool>;
}
