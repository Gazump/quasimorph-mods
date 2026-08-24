# The Dive — daily ladder service

A Cloudflare Worker + D1 database backing the online daily leaderboard in the
Quasimorph `RoguelikeMode` mod. The mod submits finished daily runs over HTTPS and
reads back the day's board.

## What it does and does not guarantee

The mod is a full-trust C# assembly running on the player's machine, so **nothing
here proves a run actually happened.** Steam identity cannot be verified either:
that needs `ISteamUserAuth/AuthenticateUserTicket` with a *publisher* Web API key for
AppID 2059170, which belongs to Magnum Scriptum, not to us. The player id in a
submission is a self-reported claim.

What the service does do:

- **Server-authoritative scoring.** The score stored is always recomputed from the
  run's components (floor, kills, victory, damage, tier). A submission whose claimed
  score disagrees with its own components by more than 1 point is rejected.
- **Plausibility bounds.** Floor 1–5, victory only on floor 5, minimum turns and
  wall-clock duration per floor reached, capped kills/damage.
- **Bracket integrity.** Only `daily` runs are ranked, only for today's or yesterday's
  UTC bracket, and only when the run reported no other mods active.
- **Replay and flood control.** Every submission carries a nonce and timestamp;
  nonces are single-use for an hour and clocks must be within 15 minutes. Each player
  is capped at `MAX_SUBMITS_PER_DAY` submissions per bracket.
- **A shared-secret signature.** Requests carry `X-Rogue-Sig`, an HMAC-SHA256 of the
  raw body. The secret ships inside the mod DLL, so a determined person can extract
  it — this stops casual `curl` abuse and gives a rotation lever, nothing more.
- **Version gating.** `ALLOWED_MOD_VERSIONS` lets you refuse old or broken clients.

Treat the board as a friendly scoreboard, not a competitive record. Keep the admin
delete endpoint handy for obvious nonsense.

## Privacy

The raw SteamID is **never stored.** The Worker hashes it with a server-only salt
(`ID_SALT`) into a 128-bit `player_key` used solely for per-day deduplication. The
database holds that hash, the sanitized persona name, and run statistics. Changing
`ID_SALT` orphans every existing row's identity, so set it once and leave it.

Persona names are stripped of control characters and angle brackets (the in-game
board renders through TextMeshPro, which would otherwise parse `<...>` as rich-text
markup) and truncated to 24 characters.

Submission is **opt-in** in the mod and off by default; reading the board sends
nothing but the requested day and tier.

## Deploy

Prerequisites: a Cloudflare account and Node 18+.

```bash
cd service/ladder
npm install
npx wrangler login
```

Create the database and paste the returned id into `wrangler.toml` under
`database_id`:

```bash
npx wrangler d1 create dive-ladder
```

Create the tables, then set the three secrets. Generate long random values for the
first two — anything you can regenerate from memory is too weak:

```bash
npm run schema:remote
npx wrangler secret put SUBMIT_SECRET
npx wrangler secret put ID_SALT
npx wrangler secret put ADMIN_TOKEN
```

Deploy:

```bash
npm run deploy
```

Wrangler prints the live URL, `https://quasimorph-dive-ladder.<your-subdomain>.workers.dev`.
That URL and the `SUBMIT_SECRET` value go into `src/RoguelikeMode/LadderConfig.cs`
(`DefaultEndpoint` and `SubmitSecret`) before building the mod for release.

To serve it from your own domain instead, add a route in `wrangler.toml` and point a
CNAME at the Worker — the mod only cares that the base URL answers `/v1/health`.

## Local development

```bash
npm run schema:local
npm run dev
```

`wrangler dev` serves on `http://127.0.0.1:8787` against a local SQLite file. Point
the mod at it with the console command `rogue_ladder endpoint http://127.0.0.1:8787`
— plain HTTP is fine for localhost.

## API

### `GET /v1/health`

```json
{ "ok": true, "day": "2026-08-18" }
```

### `GET /v1/board?day=YYYY-MM-DD&tier=normal&limit=25`

`day` defaults to today (UTC), `tier` to `normal`, `limit` to 25 (max 100).

```json
{
  "day": "2026-08-18",
  "tier": "normal",
  "total": 41,
  "entries": [
    { "rank": 1, "name": "Gazump", "score": 18450, "floor": 5, "kills": 143,
      "turns": 812, "damage": 640, "victory": true, "durationSec": 3600,
      "class": "class_id" }
  ]
}
```

### `POST /v1/runs`

Header `X-Rogue-Sig: <hex HMAC-SHA256 of the raw request body, keyed by SUBMIT_SECRET>`.

```json
{
  "v": 1, "mod": "0.4.0", "game": "1.0.2.573",
  "day": "2026-08-18", "mode": "daily", "tier": "normal",
  "steamId": "76561190000000000", "name": "Gazump",
  "floor": 5, "kills": 143, "turns": 812, "damage": 640,
  "victory": true, "score": 18450, "durationSec": 3600,
  "mods": [], "profile": "spec_id", "class": "class_id",
  "nonce": "0123456789abcdef", "ts": 1786000000
}
```

Success returns the authoritative score and the player's placement:

```json
{ "accepted": true, "ranked": true, "score": 18450, "personalBest": 18450,
  "improved": true, "rank": 3, "total": 41, "day": "2026-08-18", "tier": "normal" }
```

Rejections return `{ "accepted": false, "reason": "..." }` with a 4xx status. Only the
player's best score per day and tier is kept; a weaker resubmission is accepted and
counted but does not replace the stored row.

### `DELETE /v1/runs/:id`

Header `Authorization: Bearer <ADMIN_TOKEN>`. Removes one row — for pruning obviously
fake entries.

```bash
npx wrangler d1 execute dive-ladder --remote \
  --command "SELECT id, name, score, floor, day, tier FROM runs ORDER BY score DESC LIMIT 20"
```

## Cost

Well inside Cloudflare's free tier at any plausible mod audience: the free Workers
plan covers 100k requests/day and D1 covers 5M row reads/day. One player generates
roughly one write and a handful of reads per day.
