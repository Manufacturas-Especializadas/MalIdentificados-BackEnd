using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Lines.Commands
{
    public class UpdateLineCommandHandler : IRequestHandler<UpdateLineCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public UpdateLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateLineCommand request, CancellationToken cancellationToken)
        {
            var line = await _context.Lines.FindAsync(new object[] { request.Id }, cancellationToken);

            if (line == null) return false;

            line.LineName = request.LineName;
            line.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}