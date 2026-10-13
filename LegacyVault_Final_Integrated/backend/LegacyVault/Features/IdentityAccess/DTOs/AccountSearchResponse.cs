namespace LegacyVault.Features.IdentityAccess.DTOs
{
    public class AccountSearchResponse
    {
        // Cần để sau này gắn BeneficiaryAccountId
        public Guid Id { get; set; }

        // Hiển thị để nhận diện người dùng
        public string FullName { get; set; } = string.Empty;

        // Không trả email đầy đủ
        public string? MaskedEmail { get; set; }

        // Không trả số điện thoại đầy đủ
        public string? MaskedPhoneNumber { get; set; }

        // Cho biết account đã được định danh chưa
        public string IdentityStatus { get; set; } = string.Empty;
    }
}