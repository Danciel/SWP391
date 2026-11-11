-- Sprint 12: assets of a COMPLETED handover leave the Owner's vault.
-- New handovers do this in HandoverService.CompleteAsync. This backfills the ones that
-- were completed before the fix (the will is already HANDED_OVER, the asset still ACTIVE).
UPDATE "Assets" a
SET "Status" = 'HANDED_OVER',
    "UpdatedAt" = now()
WHERE a."Status" <> 'HANDED_OVER'
  AND EXISTS (
      SELECT 1
      FROM "WillAssets" wa
      JOIN "Wills" w ON w."Id" = wa."WillId"
      WHERE wa."AssetId" = a."Id"
        AND w."Status" = 'HANDED_OVER');
