# LV-31 - Phan tich chi phi / van hanh DMS

## DMS chay the nao
- Mot BackgroundService (`DmsMissedCycleWorker`) nam trong chinh backend, chay moi 1 gio.
- Moi lan chay: 1 query lay cac DMS ACTIVE, tinh chu ky bi miss, ghi DB, gui email nhac. Voi vai tram Will thi nhe (vai query/gio).

## Chi phi
| Hang muc | Cach lam | Chi phi |
|---|---|---|
| Chay dinh ky | Worker trong backend | 0 (chay local / cung server API) |
| Nhac Owner / bao Executor | Email qua Gmail SMTP (app password) | 0, gioi han ~500 mail/ngay cua Gmail |
| Luu du lieu | Bang DmsConfigurations, DmsCheckIns tren Supabase free tier | 0 (dong DmsCheckIns chi tang 1 dong/chu ky) |
| Che do demo | Admin `POST /api/admin/dms/evaluate` voi "Now" gia lap | 0, khong can cho nhieu ngay |

## Gioi han khi backend tat
- Worker khong chay -> khong gui nhac. Khi backend bat lai, no **tinh bu** moi chu ky da lo (moi chu ky chi ghi 1 lan nen khong dem trung). Mail nhac cu co the den tre.
- Chu ky dai hon (vd 90 ngay) khong ton them gi: van la 1 query/gio.

## Co can tra them phi khi doi chu ky?
Khong. Chu ky chi la gia tri trong DmsConfigurations; tai nguyen chay khong doi. Khong co bang gia DMS, khong tinh phi DMS (khop yeu cau "khong tu tao bang gia").

## Khi len moi truong that (ngoai pham vi do an)
Can host luon bat (hoac cron ben ngoai goi endpoint), va dich vu email chuyen dung. Ghi la gioi han cua ban demo.
