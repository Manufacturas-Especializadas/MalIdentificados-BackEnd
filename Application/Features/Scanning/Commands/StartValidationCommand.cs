using MediatR;

namespace Application.Features.Scanning.Commands
{
    public record ScanDetailDto(
        string ScannedPartCode,
        bool IsCorrect,
        DateTime ScanDate,
        int? ReleasedByPayroll
    ) : IRequest<int>;

    public record RegisterCompletedBatchCommand(
        int PayrollNumber,
        string ExpectedPartCode,
        int RequiredQuantity,
        string shopOrder,
        List<ScanDetailDto> Scans
    ) : IRequest<int>;
}