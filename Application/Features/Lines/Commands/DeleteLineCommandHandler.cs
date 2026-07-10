using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Lines.Commands
{
    public class DeleteLineCommandHandler : IRequestHandler<DeleteLineCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteLineCommand request, CancellationToken cancellationToken)
        {
            var line = await _context.Lines.FindAsync(new object[] { request.Id }, cancellationToken);

            if (line == null) return false;

            line.IsActive = false;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}