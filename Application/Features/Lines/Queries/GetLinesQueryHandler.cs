using static Application.Features.Lines.Queries.GetLines;
using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Lines.Queries
{
    public class GetLinesQueryHandler : IRequestHandler<GetLinesQuery, List<LineDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetLinesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LineDto>> Handle(GetLinesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Lines
                .Select(line => new LineDto(
                    line.Id,
                    line.LineName,
                    line.IsActive,
                    line.CreatedAt))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}