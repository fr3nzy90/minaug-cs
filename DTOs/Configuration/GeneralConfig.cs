namespace minaug.DTOs.Configuration;

internal record GeneralConfig
{
  public DateTime ReferenceDate { get; init; } = DateTime.Now;
}