using MediatR;

namespace Application.Features.Scanning.Query
{
    public record ValidateQualityApproverQuery(int PayrollNumber) : IRequest<bool>;
}