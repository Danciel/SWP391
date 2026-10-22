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

            var holderWillId =
                await _willRepository
                    .TryAddWillAssetExclusiveAsync(willAsset);

            if (holderWillId != null)
            {
                throw new WillConflictException(
                    "Asset is already used by another active will.");
            }

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

            if (string.IsNullOrWhiteSpace(request.Relationship))
            {
                throw new InvalidOperationException(
                    "Relationship is required.");
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

            if (request.AllocationPercentage <= 0m ||
                request.AllocationPercentage > 100m)
            {
                throw new InvalidOperationException(
                    "AllocationPercentage must be greater than 0 and at most 100.");
            }

            var existingAllocations = willAsset.Assignments.Sum(x => x.AllocationPercentage);
            if (existingAllocations + request.AllocationPercentage > 100m)
            {
                throw new InvalidOperationException(
                    $"Total allocation for this asset cannot exceed 100%. Current allocation: {existingAllocations:0.##}%.");
            }

            var assignment =
                new AssetAssignment
                {
                    Id = Guid.NewGuid(),
                    WillAssetId = willAsset.Id,
                    BeneficiaryDesignationId =
                        beneficiary.Id,
                    AllocationPercentage = request.AllocationPercentage,
                    CreatedAt = DateTimeOffset.UtcNow
                };

            will.UpdatedAt = DateTimeOffset.UtcNow;
            var added = await _willRepository.TryAddAssetAssignmentWithinLimitAsync(assignment);
            if (!added)
            {
                throw new InvalidOperationException(
                    "Total allocation for this asset cannot exceed 100%; another request may have changed the allocation concurrently.");
            }

            return new AssetAssignmentResponse
            {
                Id = assignment.Id,
                WillAssetId = assignment.WillAssetId,
                BeneficiaryDesignationId =
                    assignment.BeneficiaryDesignationId,
                AllocationPercentage = assignment.AllocationPercentage
            };
        }

        public async Task<AssetAssignmentResponse> UpdateAssetAssignmentAsync(
            Guid willId, Guid assignmentId, UpdateAssetAssignmentRequest request)
        {
            var will = await GetOwnedWillRequiredAsync(willId, includeDetails: true);
            EnsureDraft(will);

            var assignment = will.WillAssets
                .SelectMany(x => x.Assignments)
                .FirstOrDefault(x => x.Id == assignmentId);
            if (assignment == null)
            {
                throw new KeyNotFoundException("Asset assignment not found.");
            }

            var otherAllocations = will.WillAssets
                .First(x => x.Id == assignment.WillAssetId)
                .Assignments
                .Where(x => x.Id != assignment.Id)
                .Sum(x => x.AllocationPercentage);

            if (request.AllocationPercentage <= 0m || request.AllocationPercentage > 100m ||
                otherAllocations + request.AllocationPercentage > 100m)
            {
                throw new InvalidOperationException(
                    $"Allocation for this asset cannot exceed 100%. Other beneficiaries currently hold {otherAllocations:0.##}%.");
            }

            will.UpdatedAt = DateTimeOffset.UtcNow;
            var updated = await _willRepository.TryUpdateAssetAllocationWithinLimitAsync(assignment, request.AllocationPercentage);
            if (!updated)
            {
                throw new InvalidOperationException(
                    "Total allocation for this asset cannot exceed 100%; another request may have changed the allocation concurrently.");
            }

            return new AssetAssignmentResponse
            {
                Id = assignment.Id,
                WillAssetId = assignment.WillAssetId,
                BeneficiaryDesignationId = assignment.BeneficiaryDesignationId,
                AllocationPercentage = assignment.AllocationPercentage,
                BeneficiaryName = assignment.BeneficiaryDesignation?.FullName,
                Relationship = assignment.BeneficiaryDesignation?.Relationship
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
                        Bio = x.Bio,
                        ServiceFee = x.ServiceFee,
                        Currency = x.Currency
                    })
                .ToList();
        }

        public async Task<ExecutorPricingResponse> UpdateMyExecutorPricingAsync(
            UpdateExecutorPricingRequest request)
        {
            var accountId = _currentUserService.GetRequiredAccountId();
            var account = await _willRepository.GetAccountAsync(accountId);
            if (account == null || !string.Equals(account.Role, "EXECUTOR", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only an Executor can update Executor pricing.");
            }

            var profile = await _willRepository.GetExecutorProfileAsync(accountId);
            if (profile == null)
            {
                throw new InvalidOperationException("Executor profile was not found.");
            }

            profile.ServiceFee = request.ServiceFee;
            profile.Currency = request.Currency.Trim().ToUpperInvariant();
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            await _willRepository.SaveChangesAsync();

            return new ExecutorPricingResponse
            {
                ServiceFee = profile.ServiceFee,
                Currency = profile.Currency,
                UpdatedAt = profile.UpdatedAt
            };
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
                    WillId = will.Id,
                    Scope = "WILL"
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
            assignment.Scope = "WILL";
            assignment.ServiceFeeSnapshot = profile.ServiceFee;
            assignment.CurrencySnapshot = profile.Currency;
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
                Status = assignment.Status,
                Scope = assignment.Scope,
                ServiceFeeSnapshot = assignment.ServiceFeeSnapshot,
                CurrencySnapshot = assignment.CurrencySnapshot
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
            dms.GraceDays = request.GraceDays;
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

            if (dms.Status != "ACTIVE" &&
                dms.Status != "THRESHOLD_REACHED")
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
            dms.Status = "ACTIVE";
            dms.ThresholdReachedAt = null;
            dms.ExecutorNotifiedAt = null;
            dms.UpdatedAt = now;

            await _willRepository.SaveChangesAsync();

            return ToDmsResponse(dms);
        }

        public async Task<WillDetailResponse> CancelAsync(
            Guid willId)
        {
            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            // ACTIVE / handover states are handled by other flows.
            if (will.Status != "DRAFT" &&
                will.Status != "PENDING_EXECUTOR")
            {
                throw new WillConflictException(
                    "Only a DRAFT or PENDING_EXECUTOR will can be cancelled.");
            }

            var now = DateTimeOffset.UtcNow;
            will.Status = "CANCELLED";
            will.UpdatedAt = now;

            if (will.ExecutorAssignment != null &&
                will.ExecutorAssignment.Status == "PENDING")
            {
                will.ExecutorAssignment.Status = "CANCELLED";
                will.ExecutorAssignment.RespondedAt = now;
            }

            if (will.DmsConfiguration != null)
            {
                will.DmsConfiguration.Status = "CANCELLED";
                will.DmsConfiguration.UpdatedAt = now;
            }

            // CANCELLED is not in NonLockingStatuses' complement,
            // so every asset of this will is released automatically.
            await _willRepository.SaveChangesAsync();

            return ToDetail(will);
        }

        public async Task<BeneficiaryResponse>
            LinkBeneficiaryAsync(
                Guid willId,
                Guid beneficiaryId,
                LinkBeneficiaryRequest request)
        {
            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            if (will.Status == "CANCELLED")
            {
                throw new InvalidOperationException(
                    "Will is cancelled.");
            }

            var beneficiary = will.Beneficiaries
                .FirstOrDefault(x => x.Id == beneficiaryId)
                ?? throw new KeyNotFoundException(
                    "Beneficiary not found.");

            if (beneficiary.BeneficiaryAccountId != null)
            {
                throw new InvalidOperationException(
                    "Beneficiary is already linked to an account.");
            }

            var account =
                await _willRepository.GetAccountAsync(
                    request.AccountId)
                ?? throw new KeyNotFoundException(
                    "Account not found.");

            // Never claim by guesswork: the account must prove the
            // same email (verified) as the designation.
            if (!account.EmailVerified ||
                !account.Email.Equals(
                    beneficiary.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Account email is not verified or does not match the beneficiary.");
            }

            beneficiary.BeneficiaryAccountId = account.Id;
            beneficiary.UpdatedAt = DateTimeOffset.UtcNow;
            await _willRepository.SaveChangesAsync();

            return ToBeneficiaryResponse(beneficiary);
        }

        // W03: detaching deletes the WillAsset row, which is exactly
        // what releases the exclusive lock. Its assignments cascade.
        public async Task RemoveAssetAsync(
            Guid willId,
            Guid willAssetId)
        {
            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            EnsureDraft(will);

            var willAsset = will.WillAssets
                .FirstOrDefault(x => x.Id == willAssetId)
                ?? throw new KeyNotFoundException(
                    "Asset is not part of this will.");

            will.WillAssets.Remove(willAsset);
            await _willRepository.RemoveWillAssetAsync(willAsset);
            will.UpdatedAt = DateTimeOffset.UtcNow;
            await _willRepository.SaveChangesAsync();
        }

        public async Task RemoveAssignmentAsync(
            Guid willId,
            Guid assignmentId)
        {
            var will =
                await GetOwnedWillRequiredAsync(
                    willId,
                    includeDetails: true);

            EnsureDraft(will);

            var assignment = will.WillAssets
                .SelectMany(x => x.Assignments)
                .FirstOrDefault(x => x.Id == assignmentId)
                ?? throw new KeyNotFoundException(
                    "Assignment not found.");

            await _willRepository.RemoveAssignmentAsync(assignment);
            will.UpdatedAt = DateTimeOffset.UtcNow;
            await _willRepository.SaveChangesAsync();
        }

        public async Task<List<Guid>> GetLockedAssetIdsAsync()
        {
            return await _willRepository.GetLockedAssetIdsAsync(
                _currentUserService.GetRequiredAccountId());
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

            var incompletelyAllocatedAsset = will.WillAssets.FirstOrDefault(x =>
                Math.Abs(x.Assignments.Sum(a => a.AllocationPercentage) - 100m) > 0.01m);
            if (incompletelyAllocatedAsset != null)
            {
                throw new InvalidOperationException(
                    $"Beneficiary allocation for asset '{incompletelyAllocatedAsset.Asset?.Name ?? incompletelyAllocatedAsset.Id.ToString()}' must total 100% before submitting. Current total: {incompletelyAllocatedAsset.Assignments.Sum(a => a.AllocationPercentage):0.##}%.");
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
                GraceDays = dms.GraceDays,
                ThresholdReachedAt = dms.ThresholdReachedAt,
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
                                x.BeneficiaryDesignationId,
                            AllocationPercentage = x.AllocationPercentage,
                            BeneficiaryName =
                                x.BeneficiaryDesignation?.FullName,
                            Relationship =
                                x.BeneficiaryDesignation?.Relationship
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
                                    .Status,
                            Scope = will.ExecutorAssignment.Scope,
                            ServiceFeeSnapshot = will.ExecutorAssignment.ServiceFeeSnapshot,
                            CurrencySnapshot = will.ExecutorAssignment.CurrencySnapshot
                        },

                ExecutorServiceFee = will.ExecutorAssignment == null
                    ? null
                    : will.ExecutorAssignment.ServiceFeeSnapshot,
                ExecutorServiceFeeCurrency = will.ExecutorAssignment == null
                    ? null
                    : will.ExecutorAssignment.CurrencySnapshot,

                Dms =
                    will.DmsConfiguration == null
                        ? null
                        : ToDmsResponse(
                            will.DmsConfiguration)
            };
        }
    }
}
