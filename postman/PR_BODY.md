## Summary

Scopes the API to the Minew ESL feature set and makes it testable end to end from
Postman. Three pre-existing bugs surfaced during verification and are fixed here.

Verified against a restored copy of `SmartShelfDb` and the **live Minew cloud** —
product create/update with binding, bulk load, price push, scheduled runs and
unbind all confirmed working. All test data was removed afterwards.

## New endpoints

| Endpoint | Purpose |
|---|---|
| `GET /api/products/by-code/{productCode}` | Product plus every bound ESL |
| `GET /api/products/{id}/details` | Same payload, keyed on id |
| `GET /api/device/device/by-mac/{mac}` | Device plus what it is displaying; bare or separated MAC |
| `POST /api/products/bulk` | JSON bulk create, partial success |
| `PUT /api/products/bulk` | JSON bulk update — patch semantics |

Bulk update is deliberately patch-style: every field is optional, so a price-only
feed cannot blank descriptions or categories. `PUT /api/products/product/{id}`
keeps its replace semantics.

## Bind asymmetry fixed

Removing an assignment already unbound the label in the cloud, but creating one
only recorded intent locally — the label kept showing whatever it showed before.
`EslBindingService` now performs the bind from the product save paths, reported
per row next to the price push.

This also fixes `POST /api/device/bind`, which sent the local `StoreId` where the
cloud expects `MinewStoreId` and answered `门店不存在` on every call. The documented
workaround for the asymmetry did not actually work either.

## Queue fixes

- **Four list endpoints returned 500** (`upcoming`, `active`, by-device,
  by-location). The projection called an instance method that queries the
  DbContext, which EF Core cannot translate. Names now resolve after
  materialisation via two batched lookups rather than one `Find` per row.
- **Guard rejections corrupted a good run.** Activating an already-run or
  not-yet-due queue was recorded as `Failed`, overwriting the outcome of a run
  that had succeeded seconds earlier. Guard rejections are now separated from
  execution failures and answer `409` / `400`; genuine bind failures are still
  recorded as `Failed`.
- **Finished queues could not be deleted.** The guard ignored `IsActive`, so the
  error's own advice — "deactivate it first" — could not be followed.

## Scope reduction

**SignalR removed entirely**: hub, the never-registered `TableChangeMonitorService`,
the `IHubContext` dependencies and the package reference.

⚠️ This removes the only transport for non-Minew **"Standard"** screens.
`ExecuteStandardQueue` now throws a clear message rather than appearing to
succeed, so a queue targeting one records as Failed with the reason. This is a
capability removal, not just dead-code cleanup.

**Device-message endpoints removed**: 14 `messagecombo` routes, `MessageController`
and `PublicDeviceAssignmentController` (which was `[AllowAnonymous]`). Models,
tables and repository methods are intentionally left in place — `DeviceAssignment`
still carries a `MESSAGE` type, and a Minew template can still render a message
image.

## Swagger

XML documentation was never being generated, so every `<summary>` in the codebase
was invisible in the UI. Enabled it, wired `IncludeXmlComments`, and documented
every operation.

**187 of 187 operations now carry a summary, up from 0.** Title corrected from
`TERMS_MOBILE_WEB_API`, with a description covering store scoping, partial-success
bulk results and UTC scheduling.

## Postman

31 requests across 7 folders, now versioned in `postman/` alongside the code.
Login captures the token and store automatically; the scheduling requests convert
their window to UTC in a pre-request script, which is the most common thing to get
wrong by hand.

## Reviewer notes

- `appsettings*.json` are **not** included — those are local connection-string
  edits. Separately, the Development one has a missing semicolon
  (`Data Source=(local)TrustServerCertificate=true`) that will fail at runtime.
- Two behaviour changes worth a second opinion: binding now defaults **on**
  (`bindToEsl: false` opts out), matching unbind which was always automatic; and
  Standard-device scheduling now fails loudly.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
