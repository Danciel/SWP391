using LegacyVault.Features.WillManagement.Contracts;
using LegacyVault.Features.WillManagement.DTOs;
using LegacyVault.Models;
using LegacyVault.Shared.Security;

namespace LegacyVault.Features.WillManagement.Services
{
    public class WillService : IWillService
    {
        private readonly IWillRepository _willRepository;
        private readonly ICurrentUserService _currentUserService;

        public WillService(
            IWillRepository willRepository,
            ICurrentUserService currentUserService)
        {
            _willRepository = willRepository;
            _currentUserService = currentUserService;
        }

        public async Task<WillSummaryResponse> CreateAsync(
            CreateWillRequest request)
        {
            var now = DateTimeOffset.UtcNow;

            var will = new Will
            {
                Id = Guid.NewGuid(),
                OwnerAccountId =
                    _currentUserService.GetRequiredAccountId(),
                Title = request.Title.Trim(),
                Description =
                    string.IsNullOrWhiteSpace(request.Description)
                        ? null
                        : request.Description.Trim(),
                Status = "DRAFT",
                CreatedAt = now,
                UpdatedAt = now,
                SubmittedAt = null,
                ActivatedAt = null
            };

            await _willRepository.AddWillAsync(will);
            await _willRepository.SaveChangesAsync();

            return ToSummary(will);
        }

        public async Task<List<WillSummaryResponse>> GetMineAsync()
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var wills =
                await _willRepository
                    .GetOwnedWillsAsync(ownerId);

            return wills.Select(ToSummary).ToList();
        }

        public async Task<WillDetailResponse> GetByIdAsync(
            Guid willId)
        {
            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            return ToDetail(will);
        }

        public async Task<WillSummaryResponse> UpdateAsync(
            Guid willId,
            UpdateWillRequest request)
        {
            var will =
                await GetOwnedWillRequiredAsync(willId);

            EnsureDraft(will);

            will.Title = request.Title.Trim();
            will.Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim();
            will.UpdatedAt = DateTimeOffset.UtcNow;

            await _willRepository.SaveChangesAsync();

            return ToSummary(will);
        }

        public async Task DeleteAsync(Guid willId)
        {
            var will =
                await GetOwnedWillRequiredAsync(willId);

            EnsureDraft(will);

            _willRepository.RemoveWill(will);
            await _willRepository.SaveChangesAsync();
        }

        public async Task<WillAssetResponse> AddAssetAsync(
            Guid willId,
            AddWillAssetRequest request)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            EnsureDraft(will);

            if (will.WillAssets.Any(x =>
                x.AssetId == request.AssetId))
            {
                throw new InvalidOperationException(
                    "Asset is already added to this will.");
            }

            var asset =
                await _willRepository.GetOwnedAssetAsync(
                    request.AssetId,
                    ownerId);

            if (asset == null)
            {
                throw new KeyNotFoundException(
                    "Asset not found or not owned by current user.");
            }

            var willAsset = new WillAsset
            {
                Id = Guid.NewGuid(),
                WillId = will.Id,
                AssetId = asset.Id,
                AddedAt = DateTimeOffset.UtcNow,
                Asset = asset
            };

            await _willRepository.AddWillAssetAsync(
                willAsset);

            will.UpdatedAt = DateTimeOffset.UtcNow;
            await _willRepository.SaveChangesAsync();

            return new WillAssetResponse
            {
                WillAssetId = willAsset.Id,
                AssetId = asset.Id,
                AssetName = asset.Name,
                AssetType = asset.AssetType
            };
        }

        public async Task<BeneficiaryResponse>
            AddBeneficiaryAsync(
                Guid willId,
                CreateBeneficiaryRequest request)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            EnsureDraft(will);

            string fullName;
            string email;
            string? phone;

            if (request.BeneficiaryAccountId.HasValue)
            {
                if (request.BeneficiaryAccountId.Value ==
                    ownerId)
                {
                    throw new InvalidOperationException(
                        "Owner cannot be their own beneficiary.");
                }

                var account =
                    await _willRepository.GetAccountAsync(
                        request.BeneficiaryAccountId.Value);

                if (account == null)
                {
                    throw new KeyNotFoundException(
                        "Beneficiary account not found.");
                }

                fullName = account.FullName;
                email = account.Email;
                phone = account.Phone;
            }
            else
            {
                fullName =
                    request.FullName?.Trim()
                    ?? string.Empty;

                email =
                    request.Email?.Trim()
                    ?? string.Empty;

                phone =
                    string.IsNullOrWhiteSpace(request.Phone)
                        ? null
                        : request.Phone.Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    throw new InvalidOperationException(
                        "FullName is required for a manual beneficiary.");
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new InvalidOperationException(
                        "Email is required for a manual beneficiary in the current V1 schema.");
                }
            }

            if (will.Beneficiaries.Any(x =>
                x.Email.Equals(
                    email,
                    StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Beneficiary already exists in this will.");
            }

            var now = DateTimeOffset.UtcNow;

            var beneficiary =
                new BeneficiaryDesignation
                {
                    Id = Guid.NewGuid(),
                    WillId = will.Id,
                    BeneficiaryAccountId =
                        request.BeneficiaryAccountId,
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Relationship =
                        string.IsNullOrWhiteSpace(
                            request.Relationship)
                            ? null
                            : request.Relationship.Trim(),
                    Status = "DESIGNATED",
                    CreatedAt = now,
                    UpdatedAt = now
                };

            await _willRepository.AddBeneficiaryAsync(
                beneficiary);

            will.UpdatedAt = now;
            await _willRepository.SaveChangesAsync();

            return ToBeneficiaryResponse(beneficiary);
        }

        public async Task<AssetAssignmentResponse>
            AssignAssetAsync(
                Guid willId,
                CreateAssetAssignmentRequest request)
        {
            var will =
                await GetOwnedWillRequiredAsync(willId);

            EnsureDraft(will);

            var willAsset =
                await _willRepository.GetWillAssetAsync(
                    request.WillAssetId);

            if (willAsset == null ||
                willAsset.WillId != will.Id)
            {
                throw new InvalidOperationException(
                    "WillAsset does not belong to this will.");
            }

            var beneficiary =
                await _willRepository.GetBeneficiaryAsync(
                    request.BeneficiaryDesignationId);

            if (beneficiary == null ||
                beneficiary.WillId != will.Id)
            {
                throw new InvalidOperationException(
                    "Beneficiary does not belong to this will.");
            }

            var alreadyExists =
                await _willRepository.AssignmentExistsAsync(
                    willAsset.Id,
                    beneficiary.Id);

            if (alreadyExists)
            {
                throw new InvalidOperationException(
                    "This asset is already assigned to this beneficiary.");
            }

            var assignment =
                new AssetAssignment
                {
                    Id = Guid.NewGuid(),
                    WillAssetId = willAsset.Id,
                    BeneficiaryDesignationId =
                        beneficiary.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                };

            await _willRepository.AddAssetAssignmentAsync(
                assignment);

            will.UpdatedAt = DateTimeOffset.UtcNow;
            await _willRepository.SaveChangesAsync();

            return new AssetAssignmentResponse
            {
                Id = assignment.Id,
                WillAssetId = assignment.WillAssetId,
                BeneficiaryDesignationId =
                    assignment.BeneficiaryDesignationId
            };
        }

        public async Task<List<ExecutorListItemResponse>>
            GetExecutorsAsync()
        {
            var executors =
                await _willRepository
                    .GetVerifiedExecutorsAsync();

            return executors
                .Select(x =>
                    new ExecutorListItemResponse
                    {
                        AccountId = x.AccountId,
                        FullName =
                            x.Account?.FullName
                            ?? string.Empty,
                        Rating = x.Rating,
                        CompletedCases =
                            x.CompletedCases,
                        ExperienceYears =
                            x.ExperienceYears,
                        Bio = x.Bio
                    })
                .ToList();
        }

        public async Task<ExecutorAssignmentResponse>
            AssignExecutorAsync(
                Guid willId,
                AssignExecutorRequest request)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var will =
                await GetOwnedWillRequiredAsync(willId);

            EnsureDraft(will);

            if (request.ExecutorAccountId == ownerId)
            {
                throw new InvalidOperationException(
                    "Owner cannot be the executor of their own will.");
            }

            var profile =
                await _willRepository
                    .GetExecutorProfileAsync(
                        request.ExecutorAccountId);

            if (profile == null ||
                !profile.IsVerified ||
                profile.Account == null ||
                !string.Equals(
                    profile.Account.Role,
                    "EXECUTOR",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Selected account is not a verified executor.");
            }

            var assignment =
                await _willRepository
                    .GetExecutorAssignmentAsync(will.Id);

            if (assignment == null)
            {
                assignment = new ExecutorAssignment
                {
                    Id = Guid.NewGuid(),
                    WillId = will.Id
                };

                await _willRepository
                    .AddExecutorAssignmentAsync(assignment);
            }

            assignment.ExecutorAccountId =
                request.ExecutorAccountId;
            assignment.Status = "PENDING";
            assignment.AssignedAt =
                DateTimeOffset.UtcNow;
            assignment.RespondedAt = null;
            assignment.ExecutorAccount =
                profile.Account;

            will.UpdatedAt = DateTimeOffset.UtcNow;

            await _willRepository.SaveChangesAsync();

            return new ExecutorAssignmentResponse
            {
                Id = assignment.Id,
                ExecutorAccountId =
                    assignment.ExecutorAccountId,
                ExecutorName =
                    profile.Account.FullName,
                Status = assignment.Status
            };
        }

        public async Task<DmsConfigurationResponse>
            ConfigureDmsAsync(
                Guid willId,
                ConfigureDmsRequest request)
        {
            var will =
                await GetOwnedWillRequiredAsync(willId);

            EnsureDraft(will);

            if (request.ReminderBeforeDays >=
                request.CheckInIntervalDays)
            {
                throw new InvalidOperationException(
                    "ReminderBeforeDays must be less than CheckInIntervalDays.");
            }

            var dms =
                await _willRepository
                    .GetDmsConfigurationAsync(will.Id);

            if (dms == null)
            {
                dms = new DmsConfiguration
                {
                    Id = Guid.NewGuid(),
                    WillId = will.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _willRepository
                    .AddDmsConfigurationAsync(dms);
            }

            dms.Enabled = request.Enabled;
            dms.CheckInIntervalDays =
                request.CheckInIntervalDays;
            dms.ReminderBeforeDays =
                request.ReminderBeforeDays;
            dms.MaxMissedCheckIns =
                request.MaxMissedCheckIns;
            dms.Status = "WAITING";
            dms.UpdatedAt = DateTimeOffset.UtcNow;

            will.UpdatedAt = DateTimeOffset.UtcNow;

            await _willRepository.SaveChangesAsync();

            return ToDmsResponse(dms);
        }

        public async Task<DmsConfigurationResponse>
            CheckInAsync(Guid willId)
        {
            var will =
                await GetOwnedWillRequiredAsync(willId);

            var dms =
                await _willRepository
                    .GetDmsConfigurationAsync(will.Id);

            if (dms == null || !dms.Enabled)
            {
                throw new InvalidOperationException(
                    "DMS is not enabled for this will.");
            }

            if (!string.Equals(
                dms.Status,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "DMS is not active.");
            }

            var now = DateTimeOffset.UtcNow;
            var scheduledAt =
                dms.NextCheckInAt ?? now;

            var checkIn = new DmsCheckIn
            {
                Id = Guid.NewGuid(),
                DmsConfigurationId = dms.Id,
                ScheduledAt = scheduledAt,
                CheckedInAt = now,
                Status = "CHECKED_IN",
                CreatedAt = now
            };

            await _willRepository
                .AddDmsCheckInAsync(checkIn);

            dms.LastCheckInAt = now;
            dms.NextCheckInAt =
                now.AddDays(dms.CheckInIntervalDays);
            dms.MissedCheckIns = 0;
            dms.UpdatedAt = now;

            await _willRepository.SaveChangesAsync();

            return ToDmsResponse(dms);
        }

        public async Task<WillDetailResponse> SubmitAsync(
            Guid willId)
        {
            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            EnsureDraft(will);

            if (will.WillAssets.Count == 0)
            {
                throw new InvalidOperationException(
                    "Add at least one asset before submitting.");
            }

            if (will.Beneficiaries.Count == 0)
            {
                throw new InvalidOperationException(
                    "Add at least one beneficiary before submitting.");
            }

            if (will.WillAssets.Any(x =>
                x.Assignments.Count == 0))
            {
                throw new InvalidOperationException(
                    "Every will asset must have at least one beneficiary assignment.");
            }

            if (will.ExecutorAssignment == null)
            {
                throw new InvalidOperationException(
                    "Select an executor before submitting.");
            }

            if (will.DmsConfiguration == null)
            {
                throw new InvalidOperationException(
                    "Configure DMS before submitting.");
            }

            var now = DateTimeOffset.UtcNow;

            will.Status = "PENDING_EXECUTOR";
            will.SubmittedAt = now;
            will.UpdatedAt = now;

            if (will.DmsConfiguration.Enabled)
            {
                will.DmsConfiguration.Status = "ACTIVE";
                will.DmsConfiguration.LastCheckInAt = now;
                will.DmsConfiguration.NextCheckInAt =
                    now.AddDays(
                        will.DmsConfiguration
                            .CheckInIntervalDays);
                will.DmsConfiguration.UpdatedAt = now;
            }

            await _willRepository.SaveChangesAsync();

            var refreshed =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            return ToDetail(refreshed);
        }

        private async Task<Will> GetOwnedWillRequiredAsync(
            Guid willId,
            bool includeDetails = false)
        {
            var ownerId =
                _currentUserService.GetRequiredAccountId();

            var will =
                await _willRepository.GetOwnedWillAsync(
                    willId,
                    ownerId,
                    includeDetails);

            if (will == null)
            {
                throw new KeyNotFoundException(
                    "Will not found.");
            }

            return will;
        }

        private static void EnsureDraft(Will will)
        {
            if (!string.Equals(
                will.Status,
                "DRAFT",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only a DRAFT will can be modified.");
            }
        }

        private static WillSummaryResponse ToSummary(
            Will will)
        {
            return new WillSummaryResponse
            {
                Id = will.Id,
                Title = will.Title,
                Description = will.Description,
                Status = will.Status,
                CreatedAt = will.CreatedAt,
                UpdatedAt = will.UpdatedAt
            };
        }

        private static BeneficiaryResponse
            ToBeneficiaryResponse(
                BeneficiaryDesignation beneficiary)
        {
            return new BeneficiaryResponse
            {
                Id = beneficiary.Id,
                BeneficiaryAccountId =
                    beneficiary.BeneficiaryAccountId,
                FullName = beneficiary.FullName,
                Email = beneficiary.Email,
                Phone = beneficiary.Phone,
                Relationship =
                    beneficiary.Relationship,
                Status = beneficiary.Status
            };
        }

        private static DmsConfigurationResponse
            ToDmsResponse(DmsConfiguration dms)
        {
            return new DmsConfigurationResponse
            {
                Id = dms.Id,
                Enabled = dms.Enabled,
                CheckInIntervalDays =
                    dms.CheckInIntervalDays,
                ReminderBeforeDays =
                    dms.ReminderBeforeDays,
                MaxMissedCheckIns =
                    dms.MaxMissedCheckIns,
                MissedCheckIns =
                    dms.MissedCheckIns,
                LastCheckInAt =
                    dms.LastCheckInAt,
                NextCheckInAt =
                    dms.NextCheckInAt,
                Status = dms.Status
            };
        }

        private static WillDetailResponse ToDetail(
            Will will)
        {
            return new WillDetailResponse
            {
                Id = will.Id,
                Title = will.Title,
                Description = will.Description,
                Status = will.Status,
                CreatedAt = will.CreatedAt,
                UpdatedAt = will.UpdatedAt,
                SubmittedAt = will.SubmittedAt,
                ActivatedAt = will.ActivatedAt,

                Assets = will.WillAssets
                    .Select(x =>
                        new WillAssetResponse
                        {
                            WillAssetId = x.Id,
                            AssetId = x.AssetId,
                            AssetName =
                                x.Asset?.Name
                                ?? string.Empty,
                            AssetType =
                                x.Asset?.AssetType
                                ?? string.Empty
                        })
                    .ToList(),

                Beneficiaries = will.Beneficiaries
                    .Select(ToBeneficiaryResponse)
                    .ToList(),

                Assignments = will.WillAssets
                    .SelectMany(x => x.Assignments)
                    .Select(x =>
                        new AssetAssignmentResponse
                        {
                            Id = x.Id,
                            WillAssetId =
                                x.WillAssetId,
                            BeneficiaryDesignationId =
                                x.BeneficiaryDesignationId
                        })
                    .ToList(),

                Executor =
                    will.ExecutorAssignment == null
                        ? null
                        : new ExecutorAssignmentResponse
                        {
                            Id =
                                will.ExecutorAssignment.Id,
                            ExecutorAccountId =
                                will.ExecutorAssignment
                                    .ExecutorAccountId,
                            ExecutorName =
                                will.ExecutorAssignment
                                    .ExecutorAccount
                                    ?.FullName
                                ?? string.Empty,
                            Status =
                                will.ExecutorAssignment
                                    .Status
                        },

                Dms =
                    will.DmsConfiguration == null
                        ? null
                        : ToDmsResponse(
                            will.DmsConfiguration)
            };
        }
    }
}
