# Index reference

Every explicit index declared in `Repository/ApplicationDbContext.cs`, and why it
exists. Primary keys are **not** listed — each table's PK is indexed automatically
by the provider, so we never declare one by hand. What we *do* declare are
uniqueness constraints on natural keys, indexes on foreign keys we join or cascade
through, and composites that match real query paths.

Schema of record: [`air-quality-schema.dbml`](air-quality-schema.dbml).

## Station

| Index | Kind | Purpose |
|-------|------|---------|
| `ExternalId` | unique | `ExternalId` is the id from the upstream data source, not our PK. Unique so an ETL run can't insert the same station twice; also the lookup key when reconciling inbound data against our rows. |

## Pollutant

| Index | Kind | Purpose |
|-------|------|---------|
| `Code` | unique | Natural key (e.g. `PM25`, `NO2`). Uniqueness prevents duplicate pollutant definitions; the index serves lookups by code. |

## Measurement

| Index | Kind | Purpose |
|-------|------|---------|
| `(StationId, PollutantId, MeasuredAtUtc)` — `ux_measurement_station_pollutant_time` | unique | Doubles as a constraint and the hot read path. **Constraint:** one reading per station + pollutant + timestamp, so re-running ingestion is idempotent. **Read:** the dominant query is "measurements for this station and pollutant over a time range" — this index covers it directly, and the leftmost prefix `(StationId, PollutantId)` also covers "all readings for a station/pollutant". |

The `StationId` and `PollutantId` foreign keys are cascade-delete; the composite
above starts with both, so deletes and joins are index-backed without needing
separate single-column FK indexes.

## AlertSubscription

| Index | Kind | Purpose |
|-------|------|---------|
| `(UserId, StationId, PollutantId)` — `ux_subscription_user_station_pollutant` | unique | Prevents a user from subscribing to the same station/pollutant twice. Leftmost prefix also serves "all subscriptions for a user" and "this user's subscriptions to a station" — so no separate `UserId` index is needed. |
| `(StationId, PollutantId, IsActive)` — `ix_subscription_eval` | non-unique | The alert-evaluation path: when a new measurement arrives, find the **active** subscriptions for that station + pollutant. Ordered so the filter narrows station → pollutant → active in one seek. |

`StationId` / `PollutantId` here are `Restrict` on delete (reference data in use
can't be deleted) and `UserId` is `Cascade`; both foreign keys sit at the front of
one of the indexes above, so those checks are index-backed.

## AlertNotification

| Index | Kind | Purpose |
|-------|------|---------|
| `(AlertSubscriptionId, SentAtUtc)` — `ix_notification_subscription_time` | non-unique | Indexes the cascade-delete FK **and** the read "notification history for a subscription, newest first". |

## EtlSyncLog

| Index | Kind | Purpose |
|-------|------|---------|
| `(JobName, StartedAt)` — `ix_etl_sync_log_job_started` | non-unique | "Recent runs of job X" — the operational query for monitoring ETL health. |

## InboundMeasurementEntry

| Index | Kind | Purpose |
|-------|------|---------|
| `(Status, ReceivedAtUtc)` — `ix_inbound_status_received` | non-unique | The queue-drain query: pull `Pending` entries in arrival order for processing. |

## Rules of thumb applied here

- **Never index a primary key** — the provider already does. A hand-declared index
  on a PK is pure redundant write cost.
- **Do index foreign keys** you join or cascade through — they are *not*
  auto-indexed. Here they're the leading columns of composites, so they're covered
  without standalone indexes.
- **Leftmost prefix covers subsets** — a composite `(A, B, C)` also serves queries
  on `A` and `(A, B)`, so don't add narrower indexes those already cover.
- **Unique indexes pull double duty** — constraint plus fast lookup.
