using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Traceability.Queries
{
    public class GetTraceabilityQueryHandler : IRequestHandler<GetTraceabilityQuery, List<ValidationDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetTraceabilityQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ValidationDto>> Handle(GetTraceabilityQuery request, CancellationToken cancellationToken)
        {
            var validations = await _context.ContainerValidations
                    .AsNoTracking()
                    .OrderByDescending(v => v.Id)
                    .Take(50)
                    .Select(v => new ValidationDto(
                        v.Id,
                        v.ContainerNumber,
                        v.PayrollNumber,
                        v.ExpectedPartCode!,
                        v.RequiredQuantity,
                        v.ScannedQuantity,
                        v.Status,
                        v.ScanDetails.OrderByDescending(s => s.ScanDate)
                                        .Select(s => new ScanDetailsDto(
                                            s.Id,
                                            s.ScannedPartCode,
                                            s.IsCorrect,
                                            s.ScanDate,
                                            s.ReleasedByPayroll
                                        )).ToList()
                    )).ToListAsync(cancellationToken);

            return validations;
        }
    }
}