using MediatR;

namespace Application.Features.Traceability.Queries
{
    public record GetTraceabilityQuery(int? LineId = null): IRequest<List<ValidationDto>>;
}
