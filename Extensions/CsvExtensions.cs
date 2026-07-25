using minaug.DTOs;

namespace minaug.Extensions;

internal static class CsvExtensions
{
  extension(Csv instance)
  {
    public static Csv ReadSimple(string filepath, string delimiter, bool hasHeader = true)
    {
      IEnumerable<string> lines = File.ReadAllLines(filepath);

      string[]? header = null;

      if (hasHeader)
      {
        header = lines
          .First()
          .Split(delimiter);

        lines = lines.Skip(1);
      }

      List<string[]> content = lines
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .Select(line => line.Split(delimiter))
        .ToList();

      return new(header, content);
    }

    public void WriteSimple(string filename, string delimiter)
    {
      IEnumerable<string> lines = Array.Empty<string>();

      if (instance.Header is not null)
      {
        lines = lines.Append(string.Join(delimiter, instance.Header));
      }

      lines = lines.Concat(instance.Content.Select(items => string.Join(delimiter, items)));

      File.WriteAllLines(filename, lines);
    }

    public Csv Validate()
    {
      int columns = instance.Content.FirstOrDefault()?.Length ?? 0;

      if (instance.Header is not null && columns != instance.Header.Length || instance.Content.Any(items => columns != items.Length))
      {
        throw new InvalidDataException("Malformed data");
      }

      return instance;
    }
  }
}