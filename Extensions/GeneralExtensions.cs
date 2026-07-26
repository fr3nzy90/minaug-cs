namespace MinAug.Extensions;

internal static class GeneralExtensions
{
  extension<T>(IEnumerable<T> instance)
  {
    public IEnumerable<T> ConcatNonNull(IEnumerable<T?> items) =>
      instance
        .Concat(items
          .Where(item => item is not null)
          .Select(item => item!));
  }
}