using System.Diagnostics.CodeAnalysis;

namespace minaug.DTOs;

internal record Csv
{
  public string[]? Header { get; init; }
  public required IList<string[]> Content { get; init; }
  public IDictionary<string, int>? HeaderKeyIndexes { get; init; }

  [SetsRequiredMembers]
  public Csv(string[]? header, IList<string[]> content)
  {
    Header = header;
    Content = content;
    HeaderKeyIndexes = Header?
      .Index()
      .ToDictionary(kp => kp.Item, kp => kp.Index);
  }

  public int? GetHeaderKeyIndex(string name)
  {
    ArgumentNullException.ThrowIfNull(HeaderKeyIndexes, nameof(Header));

    return HeaderKeyIndexes.TryGetValue(name, out int index) ? index : null;
  }
}