import crypto from 'node:crypto';

const BASE = 'http://127.0.0.1:8787';
const SECRET = 'local-development-secret-do-not-ship';
const today = new Date().toISOString().slice(0, 10);

function sign(body) {
  return crypto.createHmac('sha256', SECRET).update(body).digest('hex');
}

let nonceCounter = 0;
function baseRun(over = {}) {
  nonceCounter += 1;
  return {
    v: 1, mod: '0.4.0', game: '1.0.2.573',
    day: today, mode: 'daily', tier: 'normal',
    steamId: '76561190000000001', name: 'Gazump',
    floor: 5, kills: 143, turns: 812, damage: 640,
    victory: true, score: 0, durationSec: 3600,
    mods: [], profile: 'spec_a', class: 'class_a',
    nonce: crypto.randomBytes(8).toString('hex'),
    ts: Math.floor(Date.now() / 1000),
    ...over,
  };
}

function scoreOf(r) {
  const mult = { easy: 0.75, normal: 1.0, hard: 1.25 }[r.tier];
  const base = r.floor * 1000 + r.kills * 20 + (r.victory ? 5000 : 0) - Math.min(r.damage, 1000);
  return Math.max(0, Math.round(base * mult));
}

async function submit(run, { badSig = false } = {}) {
  const body = JSON.stringify(run);
  const res = await fetch(`${BASE}/v1/runs`, {
    method: 'POST',
    headers: { 'content-type': 'application/json', 'x-rogue-sig': badSig ? 'deadbeef' : sign(body) },
    body,
  });
  return { status: res.status, json: await res.json() };
}

const checks = [];
function check(name, cond, detail) {
  checks.push({ name, ok: !!cond, detail });
  console.log(`${cond ? 'PASS' : 'FAIL'}  ${name}${cond ? '' : '  <-- ' + JSON.stringify(detail)}`);
}

// 1. happy path
let run = baseRun(); run.score = scoreOf(run);
let r = await submit(run);
check('valid run accepted', r.json.accepted === true, r.json);
check('server score authoritative', r.json.score === scoreOf(run), r.json);
check('rank reported', r.json.rank === 1, r.json);

// 2. signature enforcement
r = await submit(baseRun(), { badSig: true });
check('bad signature rejected 401', r.status === 401, r);

// 3. replayed nonce
run = baseRun(); run.score = scoreOf(run);
await submit(run);
r = await submit(run);
check('replayed nonce rejected 409', r.status === 409, r);

// 4. score tampering
run = baseRun(); run.score = 999999;
r = await submit(run);
check('inflated score rejected 422', r.status === 422 && /components/.test(r.json.reason), r.json);

// 5. impossible victory
run = baseRun({ floor: 3, victory: true }); run.score = scoreOf(run);
r = await submit(run);
check('victory above the bottom floor rejected', r.status === 422, r.json);

// 6. too fast
run = baseRun({ durationSec: 5 }); run.score = scoreOf(run);
r = await submit(run);
check('implausibly fast run rejected', r.status === 422, r.json);

// 7. other mods active
run = baseRun({ mods: ['SomeOtherMod'] }); run.score = scoreOf(run);
r = await submit(run);
check('modded run rejected', r.status === 422 && /other mods/.test(r.json.reason), r.json);

// 8. random mode
run = baseRun({ mode: 'random' }); run.score = scoreOf(run);
r = await submit(run);
check('random mode rejected', r.status === 422, r.json);

// 9. stale clock
run = baseRun({ ts: Math.floor(Date.now() / 1000) - 5000 }); run.score = scoreOf(run);
r = await submit(run);
check('stale timestamp rejected 401', r.status === 401, r.json);

// 10. best-per-player: weaker resubmission must not replace
run = baseRun({ kills: 1, floor: 2, victory: false }); run.score = scoreOf(run);
r = await submit(run);
check('weaker run accepted but not stored', r.json.accepted && r.json.personalBest > r.json.score, r.json);

// 11. second player ranks below
run = baseRun({ steamId: '76561190000000002', name: 'Rival', kills: 10, floor: 5, victory: false });
run.score = scoreOf(run);
r = await submit(run);
check('second player ranked 2nd', r.json.rank === 2, r.json);

// 12. tier brackets are separate
run = baseRun({ steamId: '76561190000000003', name: 'HardDiver', tier: 'hard', kills: 5, floor: 4, victory: false });
run.score = scoreOf(run);
r = await submit(run);
check('hard bracket independent', r.json.rank === 1 && r.json.tier === 'hard', r.json);

// 13. name sanitization
run = baseRun({ steamId: '76561190000000004', name: '  <color=red>Hax</color>  ' });
run.score = scoreOf(run);
await submit(run);

// 14. board read
const board = await (await fetch(`${BASE}/v1/board?day=${today}&tier=normal&limit=10`)).json();
check('board returns normal bracket', board.entries.length === 3, board);
check('board ordered by score desc', board.entries[0].score >= board.entries[1].score, board.entries);
const hax = board.entries.find((e) => e.name.includes('Hax'));
check('rich-text markup stripped from name', hax && !hax.name.includes('<'), hax);

// 15. anonymous read needs no signature
check('board read unauthenticated', typeof board.total === 'number', board.total);

// 16. admin delete
const del = await fetch(`${BASE}/v1/runs/1`, { method: 'DELETE', headers: { authorization: 'Bearer local-admin-token' } });
check('admin delete works', del.status === 200, await del.json());
const noauth = await fetch(`${BASE}/v1/runs/2`, { method: 'DELETE', headers: { authorization: 'Bearer wrong' } });
check('admin delete needs token', noauth.status === 401, noauth.status);

const failed = checks.filter((c) => !c.ok);
console.log(`\n${checks.length - failed.length}/${checks.length} passed`);
process.exit(failed.length ? 1 : 0);
