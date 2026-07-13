using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Features.Lines.Commands
{
    public class CreateLineCommandHandler
    {
        private readonly IApplicationDbContext _context;

        public CreateLineCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(CreateLineCommand request, CancellationToken cancellationToken)
        {
            TimeZoneInfo mexicoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");

            DateTime nowInMexico = TimeZoneInfo.ConvertTime(DateTime.UtcNow, mexicoTimeZone);

            var newLine = new Domain.Entities.Lines
            {
                LineName = request.LineName,
                CreatedAt = nowInMexico,
            };

            _context.Lines.Add(newLine);
            await _context.SaveChangesAsync(cancellationToken);

            return newLine.Id;
        }
    }
}