namespace Application.Features.Traceability.Queries
{
    public record ValidationDto(
        int Id,
        string? ContainerNumber,
        int? PayrollNumber,
        string ExpectedPartCode,
        int RequiredQuantity,
        int ScannedQuantity,
        string? Status,
        List<ScanDetailDto> ScanDetails
    );

    public record ScanDetailDto(
        int Id,
        string ScannedPartCode,
        bool IsCorrect,
        DateTime ScanDate,
        int? ReleasedByPayroll
    );
}