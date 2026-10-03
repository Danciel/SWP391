namespace SWP301BETASK.Features.AssetManagement.DTOs
{
    public class UpdateAssetRequest
    {
        public string? Name { get; set; }

        public string? AssetType { get; set; }

        public string? Provider { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Note { get; set; }

        public string? Status { get; set; }
    }
}