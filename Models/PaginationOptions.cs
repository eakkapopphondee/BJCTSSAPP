namespace thaibev.Models;

public sealed class PaginationOptions
{
    public int[] PageSizes { get; set; } = [];
    public int DefaultPageSize { get; set; }

    public void Validate()
    {
        if (PageSizes is not { Length: > 0 } || PageSizes.Any(size => size <= 0) ||
            PageSizes.Distinct().Count() != PageSizes.Length || !PageSizes.Contains(DefaultPageSize))
            throw new InvalidOperationException(
                "Pagination:PageSizes must contain distinct positive integers and include Pagination:DefaultPageSize.");
    }

    public int Normalize(int pageSize) => PageSizes.Contains(pageSize) ? pageSize : DefaultPageSize;
}
