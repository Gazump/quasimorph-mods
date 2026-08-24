CREATE TABLE IF NOT EXISTS runs (
  id           INTEGER PRIMARY KEY AUTOINCREMENT,
  player_key   TEXT    NOT NULL,
  name         TEXT    NOT NULL,
  day          TEXT    NOT NULL,
  tier         TEXT    NOT NULL,
  score        INTEGER NOT NULL,
  floor        INTEGER NOT NULL,
  kills        INTEGER NOT NULL,
  turns        INTEGER NOT NULL,
  damage       INTEGER NOT NULL,
  victory      INTEGER NOT NULL,
  duration_sec INTEGER NOT NULL,
  profile      TEXT,
  class        TEXT,
  mod_version  TEXT    NOT NULL,
  game_version TEXT,
  created_at   INTEGER NOT NULL,
  UNIQUE (player_key, day, tier)
);

CREATE INDEX IF NOT EXISTS idx_runs_board ON runs (day, tier, score DESC, created_at ASC);

CREATE TABLE IF NOT EXISTS submit_counts (
  player_key TEXT    NOT NULL,
  day        TEXT    NOT NULL,
  count      INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (player_key, day)
);

CREATE TABLE IF NOT EXISTS nonces (
  nonce TEXT    PRIMARY KEY,
  ts    INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_nonces_ts ON nonces (ts);
