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
        List<ScanDetailsDto> ScanDetails,
        int? LineId = null,
        string? LineName = null
    );

    public record ScanDetailsDto(
        int Id,
        string ScannedPartCode,
        bool IsCorrect,
        DateTime ScanDate,
        int? ReleasedByPayroll
    );
}
