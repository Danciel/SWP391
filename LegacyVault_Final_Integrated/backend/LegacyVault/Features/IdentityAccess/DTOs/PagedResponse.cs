namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class PagedResponse<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }
    }
}
