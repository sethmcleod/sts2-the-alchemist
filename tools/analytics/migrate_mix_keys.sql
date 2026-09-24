-- Re-keys the Mix counters of rows uploaded before Acrid Mix took the Weak and Vulnerable effect
-- and Fuming Mix took Tainted. The keys follow the card name, so the old "fuming" counts (Weak and
-- Vulnerable) move to "acrid", and the old "acrid" counts (the retired Poison Mix) move to "poison".
-- The same move applies to the tally keys mixmade:, mixplay: and pair:. A newer row carries
-- alchemist.mix_keys = 2 or alchemist.schema >= 2 and is left alone, so the script is safe to run
-- twice. Run it once in the Supabase SQL editor, then export.

create or replace function pg_temp.remap_label(label text) returns text language sql immutable as $$
  select case label when 'fuming' then 'acrid' when 'acrid' then 'poison' else label end
$$;

create or replace function pg_temp.remap_pair(pair text) returns text language sql immutable as $$
  select string_agg(l, '+' order by l)
  from (select pg_temp.remap_label(x) as l from unnest(string_to_array(pair, '+')) as x) as s
$$;

create or replace function pg_temp.remap_key(key text) returns text language sql immutable as $$
  select case
    when key like 'mixmade:%' then 'mixmade:' || pg_temp.remap_label(substr(key, 9))
    when key like 'mixplay:%' then 'mixplay:' || pg_temp.remap_label(substr(key, 9))
    when key like 'pair:%' then 'pair:' || pg_temp.remap_pair(substr(key, 6))
    else key
  end
$$;

create or replace function pg_temp.remap_tally(tally jsonb) returns jsonb language sql immutable as $$
  select coalesce((select jsonb_object_agg(pg_temp.remap_key(key), value) from jsonb_each(tally)), '{}'::jsonb)
$$;

create or replace function pg_temp.remap_mixes(mixes jsonb) returns jsonb language sql immutable as $$
  select (mixes - 'acrid' - 'fuming')
    || jsonb_strip_nulls(jsonb_build_object('acrid', mixes->'fuming', 'poison', mixes->'acrid'))
$$;

create or replace function pg_temp.remap_alchemist(a jsonb) returns jsonb language sql immutable as $$
  select a
    || case when a ? 'mixes' then jsonb_build_object('mixes', pg_temp.remap_mixes(a->'mixes')) else '{}'::jsonb end
    || case when a ? 'tally' then jsonb_build_object('tally', pg_temp.remap_tally(a->'tally')) else '{}'::jsonb end
    || '{"mix_keys": 2}'::jsonb
$$;

update public.runs
set alchemist = pg_temp.remap_alchemist(alchemist)
where coalesce((alchemist->>'schema')::int, (alchemist->>'mix_keys')::int, 1) < 2;
