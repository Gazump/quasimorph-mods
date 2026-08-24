export interface Env {
  DB: D1Database;
  SUBMIT_SECRET: string;
  ID_SALT: string;
  ADMIN_TOKEN: string;
  ALLOWED_MOD_VERSIONS?: string;
  MAX_SUBMITS_PER_DAY?: string;
}

const TIERS = ["easy", "normal", "hard"] as const;
type Tier = (typeof TIERS)[number];

const TIER_MULT: Record<Tier, number> = { easy: 0.75, normal: 1.0, hard: 1.25 };

const MAX_CLOCK_SKEW_SEC = 900;
const NONCE_TTL_SEC = 3600;
const DEFAULT_MAX_SUBMITS_PER_DAY = 25;
const FLOOR_COUNT = 10;

interface Submission {
  v: number;
  mod: string;
  game?: string;
  day: string;
  mode: string;
  tier: string;
  steamId: string;
  name: string;
  floor: number;
  kills: number;
  turns: number;
  damage: number;
  victory: boolean;
  score: number;
  durationSec: number;
  mods: string[];
  profile?: string;
  class?: string;
  nonce: string;
  ts: number;
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "content-type": "application/json; charset=utf-8",
      "cache-control": "no-store",
    },
  });
}

function reject(reason: string, status = 400): Response {
  return json({ accepted: false, reason }, status);
}

function utcDay(offsetDays = 0): string {
  return new Date(Date.now() + offsetDays * 86400000).toISOString().slice(0, 10);
}

function isDayString(value: unknown): value is string {
  return typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value);
}

function isTier(value: unknown): value is Tier {
  return typeof value === "string" && (TIERS as readonly string[]).includes(value);
}

function computeScore(floor: number, kills: number, victory: boolean, damage: number, tier: Tier): number {
  const base = floor * 1000 + kills * 20 + (victory ? 5000 : 0) - Math.min(damage, 1000);
  return Math.max(0, Math.round(base * TIER_MULT[tier]));
}

function sanitizeName(raw: unknown): string {
  if (typeof raw !== "string") return "operator";
  let stripped = "";
  for (const ch of raw) {
    const code = ch.codePointAt(0) ?? 0;
    if (code < 0x20 || code === 0x7f) continue;
    if (ch === "<" || ch === ">") continue;
    stripped += ch;
  }
  const cleaned = stripped.replace(/\s+/g, " ").trim().slice(0, 24);
  return cleaned.length > 0 ? cleaned : "operator";
}

function constantTimeEquals(a: string, b: string): boolean {
  if (a.length !== b.length) return false;
  let diff = 0;
  for (let i = 0; i < a.length; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}

function toHex(buffer: ArrayBuffer): string {
  return [...new Uint8Array(buffer)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

async function hmacHex(secret: string, message: string): Promise<string> {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  return toHex(await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message)));
}

async function playerKey(salt: string, steamId: string): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(salt + ":" + steamId));
  return toHex(digest).slice(0, 32);
}

function validate(body: Submission, env: Env): string | null {
  if (body.v !== 1) return "unsupported payload version";
  if (typeof body.mod !== "string" || body.mod.length === 0 || body.mod.length > 16) return "bad mod version";

  const allowed = (env.ALLOWED_MOD_VERSIONS ?? "")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
  if (allowed.length > 0 && !allowed.includes(body.mod)) return "this mod version is not accepted by the ladder";

  if (body.mode !== "daily") return "only daily runs are ranked";
  if (!isTier(body.tier)) return "bad tier";
  if (!isDayString(body.day)) return "bad day";
  if (body.day !== utcDay(0) && body.day !== utcDay(-1)) return "day is not a current daily bracket";

  if (typeof body.steamId !== "string" || !/^\d{5,20}$/.test(body.steamId)) return "bad player id";
  if (!Array.isArray(body.mods)) return "bad mods list";
  if (body.mods.length > 0) return "ranked runs must have no other mods active";

  if (!Number.isInteger(body.floor) || body.floor < 1 || body.floor > FLOOR_COUNT) return "bad floor";
  if (!Number.isInteger(body.kills) || body.kills < 0 || body.kills > 5000) return "bad kills";
  if (!Number.isInteger(body.turns) || body.turns < 1 || body.turns > 200000) return "bad turns";
  if (!Number.isInteger(body.damage) || body.damage < 0 || body.damage > 1000000) return "bad damage";
  if (typeof body.victory !== "boolean") return "bad victory flag";
  if (body.victory && body.floor !== FLOOR_COUNT) return "victory requires the bottom floor";
  if (!Number.isInteger(body.durationSec) || body.durationSec < 0) return "bad duration";
  if (body.durationSec > 7 * 86400) return "duration too long";
  if (body.durationSec < body.floor * 20) return "run too fast to be real";
  if (body.turns < body.floor * 10) return "too few turns for that depth";

  return null;
}

async function handleSubmit(request: Request, env: Env): Promise<Response> {
  const raw = await request.text();
  if (raw.length > 4096) return reject("payload too large", 413);

  const signature = (request.headers.get("x-rogue-sig") ?? "").toLowerCase();
  const expected = await hmacHex(env.SUBMIT_SECRET, raw);
  if (!constantTimeEquals(signature, expected)) return reject("bad signature", 401);

  let body: Submission;
  try {
    body = JSON.parse(raw) as Submission;
  } catch {
    return reject("malformed json");
  }

  const now = Math.floor(Date.now() / 1000);
  if (!Number.isInteger(body.ts) || Math.abs(now - body.ts) > MAX_CLOCK_SKEW_SEC) {
    return reject("client clock is too far from server time", 401);
  }
  if (typeof body.nonce !== "string" || !/^[0-9a-f]{16,64}$/.test(body.nonce)) return reject("bad nonce");

  const problem = validate(body, env);
  if (problem) return reject(problem, 422);

  const tier = body.tier as Tier;
  const key = await playerKey(env.ID_SALT, body.steamId);
  const name = sanitizeName(body.name);
  const score = computeScore(body.floor, body.kills, body.victory, body.damage, tier);

  if (typeof body.score !== "number" || Math.abs(score - body.score) > 1) {
    return reject("score does not match its own components", 422);
  }

  const nonceInsert = await env.DB.prepare("INSERT OR IGNORE INTO nonces (nonce, ts) VALUES (?, ?)")
    .bind(body.nonce, now)
    .run();
  if (nonceInsert.meta.changes === 0) return reject("duplicate submission", 409);

  const maxSubmits = Number(env.MAX_SUBMITS_PER_DAY ?? DEFAULT_MAX_SUBMITS_PER_DAY);
  const counted = await env.DB.prepare(
    `INSERT INTO submit_counts (player_key, day, count) VALUES (?, ?, 1)
     ON CONFLICT (player_key, day) DO UPDATE SET count = count + 1
     RETURNING count`,
  )
    .bind(key, body.day)
    .first<{ count: number }>();
  if (counted && counted.count > maxSubmits) {
    return reject("too many submissions for this bracket today", 429);
  }

  const previous = await env.DB.prepare(
    "SELECT score FROM runs WHERE player_key = ? AND day = ? AND tier = ?",
  )
    .bind(key, body.day, tier)
    .first<{ score: number }>();

  await env.DB.prepare(
    `INSERT INTO runs (player_key, name, day, tier, score, floor, kills, turns, damage, victory,
                       duration_sec, profile, class, mod_version, game_version, created_at)
     VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16)
     ON CONFLICT (player_key, day, tier) DO UPDATE SET
       name = excluded.name, score = excluded.score, floor = excluded.floor, kills = excluded.kills,
       turns = excluded.turns, damage = excluded.damage, victory = excluded.victory,
       duration_sec = excluded.duration_sec, profile = excluded.profile, class = excluded.class,
       mod_version = excluded.mod_version, game_version = excluded.game_version,
       created_at = excluded.created_at
     WHERE excluded.score > runs.score`,
  )
    .bind(
      key,
      name,
      body.day,
      tier,
      score,
      body.floor,
      body.kills,
      body.turns,
      body.damage,
      body.victory ? 1 : 0,
      body.durationSec,
      body.profile ?? null,
      body.class ?? null,
      body.mod,
      body.game ?? null,
      now,
    )
    .run();

  const best = await env.DB.prepare(
    "SELECT score, created_at FROM runs WHERE player_key = ? AND day = ? AND tier = ?",
  )
    .bind(key, body.day, tier)
    .first<{ score: number; created_at: number }>();

  let rank = 0;
  if (best) {
    const ahead = await env.DB.prepare(
      `SELECT COUNT(*) AS ahead FROM runs
       WHERE day = ? AND tier = ? AND (score > ? OR (score = ? AND created_at < ?))`,
    )
      .bind(body.day, tier, best.score, best.score, best.created_at)
      .first<{ ahead: number }>();
    rank = (ahead?.ahead ?? 0) + 1;
  }

  const counts = await env.DB.prepare("SELECT COUNT(*) AS total FROM runs WHERE day = ? AND tier = ?")
    .bind(body.day, tier)
    .first<{ total: number }>();

  await env.DB.prepare("DELETE FROM nonces WHERE ts < ?").bind(now - NONCE_TTL_SEC).run();

  return json({
    accepted: true,
    ranked: true,
    score,
    personalBest: best?.score ?? score,
    improved: previous === null || score > (previous?.score ?? -1),
    rank,
    total: counts?.total ?? 0,
    day: body.day,
    tier,
  });
}

async function handleBoard(url: URL, env: Env): Promise<Response> {
  const day = url.searchParams.get("day") ?? utcDay(0);
  const tier = url.searchParams.get("tier") ?? "normal";
  if (!isDayString(day)) return reject("bad day");
  if (!isTier(tier)) return reject("bad tier");

  const requested = Number(url.searchParams.get("limit") ?? 25);
  const limit = Math.min(Math.max(Number.isFinite(requested) ? requested : 25, 1), 100);

  const { results } = await env.DB.prepare(
    `SELECT name, score, floor, kills, turns, damage, victory, duration_sec, class
     FROM runs WHERE day = ? AND tier = ?
     ORDER BY score DESC, created_at ASC
     LIMIT ?`,
  )
    .bind(day, tier, limit)
    .all<{
      name: string;
      score: number;
      floor: number;
      kills: number;
      turns: number;
      damage: number;
      victory: number;
      duration_sec: number;
      class: string | null;
    }>();

  const counts = await env.DB.prepare("SELECT COUNT(*) AS total FROM runs WHERE day = ? AND tier = ?")
    .bind(day, tier)
    .first<{ total: number }>();

  return json({
    day,
    tier,
    total: counts?.total ?? 0,
    entries: (results ?? []).map((row, index) => ({
      rank: index + 1,
      name: row.name,
      score: row.score,
      floor: row.floor,
      kills: row.kills,
      turns: row.turns,
      damage: row.damage,
      victory: row.victory === 1,
      durationSec: row.duration_sec,
      class: row.class,
    })),
  });
}

async function handleAdminDelete(request: Request, env: Env, id: string): Promise<Response> {
  const auth = request.headers.get("authorization") ?? "";
  if (!auth.startsWith("Bearer ") || !constantTimeEquals(auth.slice(7), env.ADMIN_TOKEN)) {
    return reject("unauthorized", 401);
  }
  const numeric = Number(id);
  if (!Number.isInteger(numeric)) return reject("bad id");
  const result = await env.DB.prepare("DELETE FROM runs WHERE id = ?").bind(numeric).run();
  return json({ deleted: result.meta.changes });
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    if (request.method === "GET" && url.pathname === "/v1/health") {
      return json({ ok: true, day: utcDay(0) });
    }
    if (request.method === "GET" && url.pathname === "/v1/board") {
      return handleBoard(url, env);
    }
    if (request.method === "POST" && url.pathname === "/v1/runs") {
      return handleSubmit(request, env);
    }

    const deleteMatch = url.pathname.match(/^\/v1\/runs\/(\d+)$/);
    if (request.method === "DELETE" && deleteMatch) {
      return handleAdminDelete(request, env, deleteMatch[1]);
    }

    return reject("not found", 404);
  },
} satisfies ExportedHandler<Env>;
