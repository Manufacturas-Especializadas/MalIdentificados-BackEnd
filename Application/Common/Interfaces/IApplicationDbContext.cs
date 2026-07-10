using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Line> Lines { get; }
        DbSet<Client> Clients { get; }
        DbSet<PartNumber> PartNumbers { get; }
        DbSet<ContainerValidation> ContainerValidations { get; }
        DbSet<ScanDetail> ScanDetails { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}