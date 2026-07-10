using MediatR;

namespace Application.Features.Lines.Queries
{
    public record LineDto(int Id, string LineName, bool IsActive, DateTime CreatedAt);

    public record GetLinesQuery() : IRequest<List<LineDto>>;
}