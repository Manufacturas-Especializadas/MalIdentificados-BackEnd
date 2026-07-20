using Application.Features.Scanning.Query;
using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Quality.Queries.ValidateQualityApprover
{
    public class ValidateQualityApproverQueryHandler : IRequestHandler<ValidateQualityApproverQuery, bool>
    {
        private readonly IApplicationDbContext _context;

        public ValidateQualityApproverQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ValidateQualityApproverQuery request, CancellationToken cancellationToken)
        {
            return await _context.QualityApprovers
                .AnyAsync(q => q.PayrollNumber == request.PayrollNumber && q.IsActive, cancellationToken);
        }
    }
}