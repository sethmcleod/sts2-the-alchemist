-- The one table behind Alchemist run analytics. Run once in the Supabase SQL editor.
--
-- Promoted columns carry what the dashboard filters on. `data` is the vanilla-shaped RunMetrics
-- payload and `alchemist` holds what vanilla cannot see (epochs, counters, config, deck themes),
-- both as jsonb so a new field never needs a migration.

create table if not exists public.runs (
    id            bigint generated always as identity primary key,
    created_at    timestamptz not null default now(),
    mod_version   text        not null,
    game_version  text        not null,
    victory       boolean     not null,
    ascension     smallint    not null,
    floor         smallint    not null,
    playtime      integer     not null,
    player_hash   text        not null,
    epochs        smallint    not null default 0,
    data          jsonb       not null,
    alchemist     jsonb       not null default '{}'::jsonb,
    -- A deck plus a full run history is 5 to 15 KB. Anything past this is not a run
    constraint runs_data_size check (pg_column_size(data) < 262144),
    constraint runs_alchemist_size check (pg_column_size(alchemist) < 65536)
);

create index if not exists runs_created_at_idx on public.runs (created_at);
create index if not exists runs_mod_version_idx on public.runs (mod_version);
create index if not exists runs_player_hash_idx on public.runs (player_hash);

-- The publishable key in the DLL may insert and do nothing else. Reads use the secret key,
-- which bypasses RLS, so no select policy exists on purpose
alter table public.runs enable row level security;

drop policy if exists "anon insert" on public.runs;
create policy "anon insert" on public.runs
    for insert to anon
    with check (true);

revoke all on public.runs from anon;
grant insert on public.runs to anon;
