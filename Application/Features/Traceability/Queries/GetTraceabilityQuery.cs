using MediatR;

namespace Application.Features.Traceability.Queries
{
    public record GetTraceabilityQuery(): IRequest<List<ValidationDto>>;
}