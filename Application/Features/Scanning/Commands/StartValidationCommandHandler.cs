using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Scanning.Commands.RegisterCompletedBatch
{
    public class RegisterCompletedBatchCommandHandler : IRequestHandler<RegisterCompletedBatchCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public RegisterCompletedBatchCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(RegisterCompletedBatchCommand request, CancellationToken cancellationToken)
        {
            var catalogPart = await _context.PartNumbers
                .FirstOrDefaultAsync(p => p.PartNumbersCode == request.ExpectedPartCode && p.IsActive, cancellationToken);

            var correctScansCount = request.Scans.Count(s => s.IsCorrect);

            var newValidation = new ContainerValidation
            {
                PayrollNumber = request.PayrollNumber,
                ExpectedPartCode = request.ExpectedPartCode,
                IdPartNumber = catalogPart?.Id,
                RequiredQuantity = request.RequiredQuantity,
                ScannedQuantity = correctScansCount,
                Status = "completed",

                ScanDetails = request.Scans.Select(scan => new ScanDetail
                {
                    ScannedPartCode = scan.ScannedPartCode,
                    IsCorrect = scan.IsCorrect,
                    ScanDate = scan.ScanDate,
                    ReleasedByPayroll = scan.ReleasedByPayroll
                }).ToList()
            };

            _context.ContainerValidations.Add(newValidation);
            await _context.SaveChangesAsync(cancellationToken);

            return newValidation.Id;
        }
    }
}