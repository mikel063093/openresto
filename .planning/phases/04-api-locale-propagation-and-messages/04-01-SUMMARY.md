# 04-01 Summary

## Outcome

- Centralized frontend `Accept-Language` propagation in `openresto-frontend/api/client.ts` using the existing persisted locale contract plus browser-language fallback.
- Removed the known ad hoc route-level locale header from `openresto-frontend/app/(user)/booking-confirmation/[bookingRef].tsx`.
- Extended focused frontend API Jest coverage to prove the shared client sends `en` and `es-CO` correctly for auth, booking, and hold requests.

## Evidence

- `npm test -- --runInBand tests/api/client.test.ts tests/api/auth.test.ts tests/api/bookings.test.ts tests/api/holds.test.ts`
  - Passed on 2026-07-23.

## Notes

- The client continues preserving existing request bodies, credentials defaults, and custom headers while adding centralized locale propagation.
- `es` persisted from older frontend state is normalized to `es-CO` before transport.

## Files

- `openresto-frontend/api/client.ts`
- `openresto-frontend/app/(user)/booking-confirmation/[bookingRef].tsx`
- `openresto-frontend/tests/api/client.test.ts`
- `openresto-frontend/tests/api/auth.test.ts`
- `openresto-frontend/tests/api/bookings.test.ts`
- `openresto-frontend/tests/api/holds.test.ts`
