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
            ValidateContract(request);

            if (request.LineId.HasValue && !await _context.Lines
                .AnyAsync(l => l.Id == request.LineId.Value && l.IsActive, cancellationToken))
            {
                throw new BatchValidationException("lineId", "La línea no existe o está inactiva.");
            }

            var catalogPart = await ResolvePartAsync(request, cancellationToken);

            var correctScansCount = request.Scans.Count(s => s.IsCorrect);

            var newValidation = new ContainerValidation
            {
                PayrollNumber = request.PayrollNumber,
                LineId = request.LineId,
                // Preserve the empty value written by legacy clients.
                ContainerNumber = request.ContainerNumber ?? string.Empty,
                ShopOrder = request.shopOrder,
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

        private static void ValidateContract(RegisterCompletedBatchCommand request)
        {
            if (request.ExpectedPartCode is null)
                throw new BatchValidationException("expectedPartCode", "El número de parte es obligatorio.");

            if (request.Scans is null || request.Scans.Any(s => s is null))
                throw new BatchValidationException("scans", "La lista de lecturas no puede ser nula ni contener elementos nulos.");

            if (request.ValidationMode is null)
            {
                if (request.LineId.HasValue || request.ContainerNumber is not null)
                    throw new BatchValidationException("validationMode", "Las solicitudes con línea o contenedor requieren una modalidad explícita.");

                // Previously enforced by MVC's non-nullable shopOrder parameter.
                // Do not change legacy empty strings, scan counts or completion rules.
                if (request.shopOrder is null)
                    throw new BatchValidationException("shopOrder", "Shop Order es obligatorio para las solicitudes anteriores.");

                return;
            }

            if (request.ValidationMode is not (ValidationModes.ShopOrder or ValidationModes.Container))
                throw new BatchValidationException("validationMode", "Las modalidades permitidas son shopOrder y container.");

            if (request.LineId is null or <= 0)
                throw new BatchValidationException("lineId", "La modalidad explícita requiere una línea válida.");

            if (string.IsNullOrWhiteSpace(request.ExpectedPartCode))
                throw new BatchValidationException("expectedPartCode", "El número de parte es obligatorio.");

            if (request.ValidationMode == ValidationModes.ShopOrder)
            {
                if (string.IsNullOrWhiteSpace(request.shopOrder))
                    throw new BatchValidationException("shopOrder", "Shop Order es obligatorio para esta modalidad.");

                if (request.ContainerNumber is not null)
                    throw new BatchValidationException("containerNumber", "El contenedor corresponde a la modalidad container.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.ContainerNumber))
                    throw new BatchValidationException("containerNumber", "El número de contenedor es obligatorio para esta modalidad.");

                if (!string.Equals(request.ExpectedPartCode.Trim(), request.ContainerNumber.Trim(), StringComparison.Ordinal))
                    throw new BatchValidationException("containerNumber", "El código del contenedor debe coincidir con el número de parte.");
            }
        }

        private async Task<PartNumber?> ResolvePartAsync(RegisterCompletedBatchCommand request, CancellationToken cancellationToken)
        {
            var partCode = request.ValidationMode == ValidationModes.Container
                ? request.ExpectedPartCode.Trim()
                : request.ExpectedPartCode;

            var parts = _context.PartNumbers.AsNoTracking()
                .Where(p => p.PartNumbersCode == partCode && p.IsActive);

            if (request.LineId.HasValue)
                parts = parts.Where(p => p.IdLine == request.LineId.Value);

            var matches = await parts.Take(2).ToListAsync(cancellationToken);

            if (matches.Count > 1)
            {
                if (request.LineId.HasValue)
                    throw new BatchValidationException("expectedPartCode", "El catálogo contiene varias partes activas con ese código en la línea indicada.");

                // Keep receiving legacy batches without assigning an arbitrary product or line.
                return null;
            }

            if (matches.Count == 1)
                return matches[0];

            // Catalog coverage is not established: unlisted products remain operational.
            // An existing catalog entry, however, must not be linked to another line.
            if (request.LineId.HasValue && await _context.PartNumbers
                .AnyAsync(p => p.PartNumbersCode == partCode, cancellationToken))
            {
                throw new BatchValidationException("expectedPartCode", "La parte catalogada no tiene una asociación activa con la línea indicada.");
            }

            return null;
        }
    }
}
