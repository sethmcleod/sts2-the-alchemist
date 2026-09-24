// The Alchemist dashboard: filters, the URL, and one render function per tab. data.js and
// charts.js are generic; everything that knows about the Alchemist lives here and in index.html.

import {
  compareVersions,
  histogramMedian,
  load,
  median,
  rate,
  selectGroups,
  sum,
  sumBy,
  table,
  wilson,
  zScore,
} from './data.js';
import {
  barList,
  columns,
  dataTable,
  el,
  emptyNote,
  fixed,
  image,
  num,
  pct,
  points,
  range,
  scatter,
  stackBar,
  tiles,
} from './charts.js';

// The default version filter is the newest versions that together reach this many solo runs
const RECENT_MIN_RUNS = 500;

const MIX_KINDS = {
  bursting: ['Bursting', '#f03c3c'],
  syrupy: ['Syrupy', '#4a90e8'],
  zesty: ['Zesty', '#c07aff'],
  fuming: ['Fuming', '#ff9424'],
  acrid: ['Acrid', '#4fd06a'],
  sparkling: ['Sparkling', '#ffe14a'],
  compound: ['Compound', '#f4f4f4'],
};
const MIX_FIGHT_BUCKETS = ['0', '1', '2', '3', '4', '5-6', '7-9', '10+'];
const RARITY_FILL = { Common: 'var(--common)', Uncommon: 'var(--uncommon)', Rare: 'var(--rare)' };
const POOL_RARITIES = ['Common', 'Uncommon', 'Rare'];
const TIER_FILL = ['var(--bronze)', 'var(--silver)', 'var(--gold)'];
const BRANCHES = { 'public-beta': 'beta', public: 'main' };
const ENCOUNTER_KINDS = { BOSS: 'Boss', ELITE: 'Elite', WEAK: 'Hallway', NORMAL: 'Hallway' };
// The export's ascension bands, by their lowest ascension
const ASCENSION_BANDS = { 0: 'A0', 1: 'A1 to A4', 5: 'A5 to A9', 10: 'A10 and up' };
// Comparing many cards at once, a 95% test flags a few by chance, so card changes need 99%
const LIKELY_Z = 2.58;

const $ = (id) => document.getElementById(id);

const DEFAULTS = {
  view: 'overview',
  version: 'recent',
  ascension: 'all',
  players: 'solo',
  pool: 'all',
  build: 'all',
  min: 10,
  q: '',
  sort: 'vsPeers',
  rarity: '',
  theme: '',
  a: '',
  b: '',
};
const state = { ...DEFAULTS };
const VIEWS = ['overview', 'cards', 'mechanics', 'relics', 'fights', 'versions'];

let S; // summary.json
let groups; // one {version, build, ascension, pool, coop} per group index
let T; // the summary tables
let recentStart; // the oldest version in the "Recent versions" window

// ---------- helpers ----------

const prefix = () => S.meta.prefix;

// The mod's own names come from its localization. A base game id only has its words to go on
function nameOf(id) {
  if (S.names[id]) return S.names[id];
  if (S.card_info[id]) return S.card_info[id].name;
  return id
    .replace(/^[A-Z0-9]+-/, '')
    .toLowerCase()
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase())
    .replace(/(?!^)\b(Of|The|And|In)\b/g, (word) => word.toLowerCase());
}

// "KNOWLEDGE_DEMON_BOSS" -> {name: "Knowledge Demon", kind: "Boss"}
function encounterOf(id) {
  const suffix = id.split('_').at(-1);
  const kind = ENCOUNTER_KINDS[suffix];
  return { name: nameOf(kind ? id.slice(0, -suffix.length - 1) : id), kind: kind ?? 'Fight' };
}

// A tally label such as "fresh_batch_power" back to its model id
const labelToId = (label) => prefix() + label.toUpperCase();

// An image in the mod's repo, on GitHub's raw file host. Null when the export had no repo to point at
const asset = (path) => (path && S.meta.assets?.base ? S.meta.assets.base + path : null);
const iconOf = (id) => asset(S.icons?.[id]);

// Card text in the game's loc markup: [gold] keywords, [green] upgraded numbers, [energy] icons
function cardText(markup) {
  const energy = asset(S.meta.assets?.energy);
  let color = null;
  return markup.split(/\[(\/?(?:gold|green)|energy)\]/).map((part, i) => {
    if (i % 2 === 0) return color && part ? el('span', { class: `t-${color}` }, part) : part;
    if (part === 'energy') return energy ? image(energy, 'energy', 'Energy') : 'Energy';
    color = part.startsWith('/') ? null : part;
    return null;
  });
}

function minutes(value) {
  if (value == null) return '–';
  const m = Math.round(value);
  return m >= 60 ? `${Math.floor(m / 60)} h ${m % 60} min` : `${m} min`;
}

// A win rate bar: the rate, its likely range, and the run count
function rateItem(label, wins, runs, extra = {}) {
  const [lo, hi] = wilson(wins, runs);
  return { label, note: `${num(runs)} runs`, value: rate(wins, runs), text: pct(rate(wins, runs)), lo, hi, ...extra };
}

// Every counter, summed over the selected groups
const counters = (on) => sumBy(T.counters, on, (r) => r.counter);

// The counters that start with a prefix, keyed by the label after it
function byPrefix(all, start) {
  const out = new Map();
  for (const [key, total] of all) if (key.startsWith(start)) out.set(key.slice(start.length), total);
  return out;
}

const totalCount = (byKey) => [...byKey.values()].reduce((n, c) => n + c.count, 0);

function histogram(on, metric, column = 'runs') {
  const { width, last } = S.meta.histograms[metric];
  const counts = sumBy(T.histograms, on, (r) => (r.metric === metric ? r.bin : -1));
  const bins = [];
  for (let bin = 0; bin <= last; bin += width) bins.push({ bin, runs: counts.get(bin)?.[column] || 0 });
  return { bins, width, last };
}

function badgeShares(on, badgeId) {
  const byTier = sumBy(T.badges, on, (r) => (r.badge === badgeId ? r.tier : -1));
  byTier.delete(-1);
  const eligible = [...byTier.values()].reduce((n, t) => n + t.runs, 0);
  const atLeast = (tier) =>
    [...byTier].filter(([t]) => t >= tier).reduce((n, [, total]) => n + total.runs, 0);
  return { eligible, share: (tier) => rate(atLeast(tier), eligible) };
}

const badgeOf = (id) => S.meta.badges.find((b) => b.id === id);

// The schema 3 counters (plays per card, Antitoxin sources and decay, the self-Poison peak, Mix losses,
// enemy Poison, Ferment turns per card) start with one version. Every run of a version shares a
// schema, so the groups that carry them are a version cut
const detailSince = () => S.meta.schema_since?.['3'];

function withDetail(on) {
  const since = detailSince();
  return on.map((flag, i) => (flag && since && compareVersions(groups.rows[i].version, since) >= 0 ? 1 : 0));
}

function countedSince() {
  return detailSince() ? `from ${detailSince()} on` : 'from the next release on';
}

// ---------- filters ----------

function versionTest(choice) {
  if (choice === 'all') return () => true;
  if (choice === 'recent') return (v) => compareVersions(v, recentStart) >= 0;
  if (choice.startsWith('>=')) return (v) => compareVersions(v, choice.slice(2)) >= 0;
  return (v) => v === choice;
}

function selected(overrides = {}) {
  const f = { ...state, ...overrides };
  const inVersion = versionTest(f.version);
  return selectGroups(
    groups,
    (g) =>
      inVersion(g.version) &&
      (f.ascension === 'all' || g.ascension === Number(f.ascension)) &&
      (f.players === 'all' || g.coop === (f.players === 'coop' ? 1 : 0)) &&
      (f.pool === 'all' || g.pool === f.pool) &&
      (f.build === 'all' || g.build === f.build),
  );
}

function findRecentStart() {
  const soloByVersion = sumBy(T.totals, selectGroups(groups, (g) => !g.coop), (r) => groups.rows[r.group].version);
  const newestFirst = [...S.meta.versions].reverse();
  let runs = 0;
  for (const version of newestFirst) {
    runs += soloByVersion.get(version)?.runs || 0;
    if (runs >= RECENT_MIN_RUNS) return version;
  }
  return newestFirst.at(-1);
}

function versionPhrase(choice) {
  const latest = S.meta.versions.at(-1);
  if (choice === 'all') return 'every version';
  if (choice === 'recent') return recentStart === latest ? latest : `${recentStart} to ${latest}`;
  if (choice.startsWith('>=')) return `${choice.slice(2)} to ${latest}`;
  return choice;
}

function describeFilters(runs) {
  const players = { solo: 'solo ', coop: 'co-op ', all: '' }[state.players];
  const ascension = state.ascension === 'all' ? 'at every ascension' : `at ${ASCENSION_BANDS[state.ascension]}`;
  const pool = {
    all: '',
    full: ', with the full card pool',
    partial: ', with a partly unlocked pool',
    starter: ', with the starting pool',
  }[state.pool];
  const build = state.build === 'all' ? '' : `, on game build ${state.build}`;
  const summary = $('summary');
  summary.replaceChildren(
    'Showing ',
    el('strong', {}, `${num(runs)} ${players}runs`),
    ` from ${versionPhrase(state.version)}, ${ascension}${pool}${build}.`,
  );
}

function fillFilters() {
  const versionSelect = $('fVersion');
  const newestFirst = [...S.meta.versions].reverse();
  const latest = newestFirst[0];
  versionSelect.replaceChildren(
    new Option(recentStart === latest ? `Recent versions (${latest})` : `Recent versions (${recentStart}+)`, 'recent'),
    new Option(`Latest version (${latest})`, latest),
    new Option('All versions', 'all'),
    el(
      'optgroup',
      { label: 'Since a version' },
      newestFirst.slice(1).map((v) => new Option(`${v} and newer`, `>=${v}`)),
    ),
    el(
      'optgroup',
      { label: 'One version' },
      newestFirst.slice(1).map((v) => new Option(v, v)),
    ),
  );
  $('fBuild').replaceChildren(
    new Option('All builds', 'all'),
    ...Object.entries(S.meta.builds)
      .reverse()
      .map(([build, kind]) => new Option(BRANCHES[kind] ? `${build} (${BRANCHES[kind]})` : build, build)),
  );
  const both = [$('compareA'), $('compareB')];
  for (const select of both) select.replaceChildren(...newestFirst.map((v) => new Option(v, v)));
}

function syncControls() {
  $('fVersion').value = state.version;
  $('fAscension').value = state.ascension;
  $('fPlayers').value = state.players;
  $('fPool').value = state.pool;
  $('fBuild').value = state.build;
  $('fMin').value = String(state.min);
  $('cardSearch').value = state.q;
  $('cardSort').value = state.sort;
  const newest = [...S.meta.versions].reverse();
  $('compareB').value = state.b || newest[0];
  $('compareA').value = state.a || newest[1] || newest[0];
  if (state.pool !== 'all' || state.build !== 'all' || state.min !== DEFAULTS.min) $('moreFilters').open = true;
}

// ---------- URL ----------

// The tab and every non-default choice live in the hash, so a reload or a shared link opens the
// same view
function readUrl() {
  Object.assign(state, DEFAULTS);
  const [view, query = ''] = location.hash.slice(1).split('?');
  if (VIEWS.includes(view)) state.view = view;
  const params = new URLSearchParams(query);
  for (const key of Object.keys(DEFAULTS)) {
    if (key === 'view' || !params.has(key)) continue;
    state[key] = key === 'min' ? Math.max(1, Number(params.get(key)) || DEFAULTS.min) : params.get(key);
  }
  const versions = S.meta.versions;
  const knownVersion = (v) => ['recent', 'all'].includes(v) || versions.includes(v.replace(/^>=/, ''));
  if (!knownVersion(state.version)) state.version = DEFAULTS.version;
  if (state.version === `>=${versions.at(-1)}`) state.version = versions.at(-1);
  if (state.build !== 'all' && !(state.build in S.meta.builds)) state.build = 'all';
  for (const key of ['a', 'b']) if (state[key] && !versions.includes(state[key])) state[key] = '';
}

function writeUrl() {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(state)) {
    if (key !== 'view' && value !== DEFAULTS[key]) params.set(key, value);
  }
  const query = params.toString();
  try {
    history.replaceState(null, '', `#${state.view}${query ? `?${query}` : ''}`);
  } catch {}
}

// A browser that unloads an idle tab reloads it from the URL, which keeps the view but not the
// scroll offset. sessionStorage survives that reload, and the offset is put back after the first
// render because the page is still short before it
const SCROLL_KEY = 'alchemist-stats-scroll';

function keepScroll() {
  if ('scrollRestoration' in history) history.scrollRestoration = 'manual';
  const save = () => {
    try {
      sessionStorage.setItem(SCROLL_KEY, String(Math.round(window.scrollY)));
    } catch {}
  };
  let timer = 0;
  window.addEventListener('scroll', () => {
    clearTimeout(timer);
    timer = setTimeout(save, 150);
  }, { passive: true });
  window.addEventListener('pagehide', save);
}

function restoreScroll() {
  try {
    const y = Number(sessionStorage.getItem(SCROLL_KEY));
    if (y > 0) window.scrollTo(0, y);
  } catch {}
}

// ---------- overview ----------

function renderOverview(on) {
  const t = sum(T.totals, on);
  const overall = rate(t.wins, t.runs);
  const length = histogram(on, 'run_minutes');
  const winLength = histogram(on, 'run_minutes', 'wins');
  tiles($('overviewTiles'), [
    { label: 'Win rate', value: pct(overall), note: `${num(t.wins)} wins from ${num(t.runs)} runs` },
    {
      label: 'Reach Act 3',
      value: pct(rate(t.reached_act3, t.runs)),
      note: `${pct(rate(t.reached_act2, t.runs))} reach Act 2`,
    },
    {
      label: 'Typical run',
      value: minutes(histogramMedian(length.bins, length.width)),
      note: `a typical win takes ${minutes(histogramMedian(winLength.bins, winLength.width))}`,
    },
    {
      label: 'Antitoxin peak',
      value: fixed(rate(t.antitoxin_peak, t.runs_with_peak), 0),
      note: 'the most held at once, on average',
    },
  ]);

  const stages = [
    ['Reach Act 2', t.reached_act2],
    ['Reach Act 3', t.reached_act3],
    ['Win', t.wins],
  ];
  barList(
    $('funnel'),
    t.runs
      ? stages.map(([label, n], i) => ({
          label,
          value: n / t.runs,
          text: pct(n / t.runs),
          textNote: num(n),
          fill: `var(--stage-${i + 1})`,
        }))
      : [],
    { max: 1 },
  );
  const per100 = (n) => Math.round((100 * n) / t.runs);
  $('funnelNote').textContent = t.runs
    ? `Out of every 100 runs, ${per100(t.reached_act2)} reach Act 2, ${per100(t.reached_act3)} reach Act 3 and ${per100(t.wins)} win.`
    : '';

  const byAscension = [...sumBy(T.ascensions, on, (r) => r.ascension)]
    .sort((a, b) => a[0] - b[0])
    .filter(([, g]) => g.runs >= state.min);
  barList(
    $('byAscension'),
    byAscension.map(([ascension, g]) => rateItem(`A${ascension}`, g.wins, g.runs)),
    { max: 1, reference: overall, referenceLabel: `All runs together: ${pct(overall)}`, labelWidth: '9rem' },
  );

  $('themesNote').textContent =
    `Each run counts toward the theme with the most cards in its final deck. A deck with fewer than ` +
    `${S.meta.theme_min_cards} cards of any one theme counts as Unfocused.`;
  const themes = [...sumBy(T.themes, on, (r) => r.theme)]
    .filter(([, g]) => g.runs >= state.min)
    .sort((a, b) => b[1].runs - a[1].runs);
  barList(
    $('byTheme'),
    themes.map(([theme, g]) => rateItem(theme, g.wins, g.runs, { note: `${pct(rate(g.runs, t.runs))} of runs` })),
    { max: 1, reference: overall, referenceLabel: `All runs together: ${pct(overall)}`, labelWidth: '11rem' },
  );

  const everyVersion = selected({ version: 'all' });
  const byVersion = [...sumBy(T.totals, everyVersion, (r) => groups.rows[r.group].version)]
    .filter(([, g]) => g.runs >= state.min)
    .sort((a, b) => compareVersions(b[0], a[0]));
  barList(
    $('byVersion'),
    byVersion.map(([version, g]) => rateItem(version, g.wins, g.runs)),
    { max: 1, limit: 8, labelWidth: '11rem' },
  );

  const days = sumBy(T.days, everyVersion, (r) => r.day);
  const last = new Date(`${S.meta.last_day}T00:00:00Z`);
  const series = [];
  for (let i = 29; i >= 0; i--) {
    const day = new Date(last);
    day.setUTCDate(last.getUTCDate() - i);
    const key = day.toISOString().slice(0, 10);
    const g = days.get(key) || { runs: 0, wins: 0 };
    const label = day.toLocaleDateString('en-US', { month: 'short', day: 'numeric', timeZone: 'UTC' });
    series.push({
      label,
      value: g.runs,
      tip: [label, `${num(g.runs)} runs`, g.runs ? `${pct(g.wins / g.runs)} won` : 'no runs'],
    });
  }
  columns($('byDay'), series, { height: 160, ariaLabel: 'Runs uploaded per day over the last 30 days' });
}

// ---------- cards ----------

let CARDS;

function cardRows(on) {
  const t = sum(T.totals, on);
  const rows = [];
  for (const [id, c] of sumBy(CARDS, on, (r) => r.card)) {
    const meta = S.card_info[id];
    rows.push({
      id,
      name: meta?.name ?? nameOf(id),
      rarity: meta?.rarity ?? 'Retired',
      themes: meta?.themes ?? [],
      tags: meta?.tags ?? [],
      ...c,
      winrate: rate(c.held_wins, c.held),
      pickrate: rate(c.picked, c.offered),
      deckrate: rate(c.held, t.runs),
      playsPerRun: rate(c.plays, c.held_with_plays),
      unplayed: rate(c.held_never_played, c.held_with_plays),
    });
  }
  // Long runs finish with more cards, so a card is measured against its rarity's typical card
  const peers = {};
  for (const rarity of POOL_RARITIES) {
    peers[rarity] = median(
      rows.filter((r) => r.rarity === rarity && r.held >= state.min).map((r) => r.winrate),
    );
  }
  for (const r of rows) {
    r.peer = peers[r.rarity] ?? null;
    r.vsPeers = r.peer != null && r.winrate != null ? r.winrate - r.peer : null;
  }
  return { rows, overall: rate(t.wins, t.runs) };
}

const inPool = (r) => POOL_RARITIES.includes(r.rarity);

function cardTip(r) {
  return [
    r.name,
    `${r.rarity}${r.themes.length ? `, ${r.themes.join(' and ')}` : ''}`,
    `Wins ${pct(r.winrate)} of ${num(r.held)} runs (${range(wilson(r.held_wins, r.held))})`,
    r.offered ? `Picked ${pct(r.pickrate)} of the ${num(r.offered)} times offered` : 'Never offered as a reward',
  ];
}

async function renderCards(on) {
  CARDS ??= table((await load('cards.json')).cards);
  const { rows, overall } = cardRows(on);
  const measured = rows.filter((r) => inPool(r) && r.held >= state.min);

  const t = sum(T.totals, on);
  tiles($('cardTiles'), [
    {
      label: 'Final deck',
      value: `${fixed(rate(t.deck_size, t.runs), 0)} cards`,
      note: `${fixed(rate(t.win_deck_size, t.wins), 0)} in a winning run`,
    },
    { label: 'Card choices', value: fixed(rate(t.reward_screens, t.runs), 0), note: 'per run, rewards and events' },
    { label: 'Skipped', value: pct(rate(t.reward_skips, t.reward_screens)), note: 'of card choices' },
    {
      label: 'Upgrades',
      value: fixed(rate(sum(CARDS, on).upgraded, t.runs), 1),
      note: 'per run, at rest sites',
    },
  ]);

  const standout = (r) =>
    rateItem(r.name, r.held_wins, r.held, {
      note: r.rarity,
      textNote: points(r.vsPeers),
      ref: r.peer,
      onSelect: () => openCard(r.id),
    });
  const byGap = [...measured].sort((a, b) => b.vsPeers - a.vsPeers);
  const peerKey = { max: 1, referenceLabel: 'The typical card of the same rarity' };
  barList($('standoutsUp'), byGap.filter((r) => r.vsPeers > 0).slice(0, 6).map(standout), peerKey);
  barList($('standoutsDown'), byGap.filter((r) => r.vsPeers < 0).slice(-6).reverse().map(standout), peerKey);

  const rarityChoice = state.rarity;
  $('rarityChips').replaceChildren(
    ...['', ...POOL_RARITIES].map((rarity) =>
      el(
        'button',
        {
          type: 'button',
          class: 'chip',
          'aria-pressed': String(rarityChoice === rarity),
          onclick: () => update({ rarity }),
        },
        rarity || 'All rarities',
      ),
    ),
  );
  const cardMedian = median(measured.map((r) => r.winrate));
  const query = state.q.toLowerCase();
  const matches = (r) => (!rarityChoice || r.rarity === rarityChoice) && (!query || r.name.toLowerCase().includes(query));
  scatter(
    $('cardScatter'),
    measured
      .filter((r) => r.offered > 0)
      .map((r) => ({
        x: r.pickrate,
        y: r.winrate,
        r: Math.max(4, Math.min(9, 3 + Math.sqrt(r.held) / 3)),
        fill: RARITY_FILL[r.rarity],
        label: r.name,
        weight: r.held,
        dim: !matches(r),
        tip: cardTip(r),
        id: r.id,
      })),
    {
      xLabel: 'Pick rate when offered',
      reference: cardMedian,
      labelCount: $('cardScatter').clientWidth < 520 ? 10 : 36,
      onSelect: (p) => openCard(p.id),
      ariaLabel: 'Pick rate against win rate for each card',
    },
  );
  $('scatterLegend').replaceChildren(
    ...POOL_RARITIES.map((rarity) => el('span', {}, el('i', { class: 'dot', '--dot': RARITY_FILL[rarity] }), rarity)),
    el('span', { class: 'ref-key' }, `Typical card: ${pct(cardMedian)}`),
  );

  const themeChoice = state.theme;
  $('themeChips').replaceChildren(
    ...['', ...S.meta.themes].map((theme) =>
      el(
        'button',
        {
          type: 'button',
          class: 'chip',
          'aria-pressed': String(themeChoice === theme),
          onclick: () => update({ theme }),
        },
        theme || 'All themes',
      ),
    ),
  );
  const listed = rows.filter(
    (r) =>
      r.held >= state.min &&
      r.rarity !== 'Token' &&
      (!themeChoice || r.themes.includes(themeChoice)) &&
      (!query || r.name.toLowerCase().includes(query)),
  );
  const sortDir = state.sort === 'name' ? 1 : -1;
  dataTable($('cardTable'), {
    columns: [
      {
        key: 'name',
        label: 'Card',
        render: (r) => [
          el('i', { class: 'dot', '--dot': RARITY_FILL[r.rarity] || 'var(--other)' }),
          r.name,
          el('small', {}, [r.rarity, ...r.tags].join(', ')),
        ],
      },
      {
        key: 'winrate',
        label: 'Win rate',
        num: true,
        render: (r) => [pct(r.winrate), r.vsPeers != null && el('small', {}, `${points(r.vsPeers)} vs peers`)],
      },
      {
        key: 'range',
        label: 'Likely range',
        num: true,
        wide: true,
        sortable: false,
        render: (r) => range(wilson(r.held_wins, r.held)),
      },
      {
        key: 'pickrate',
        label: 'Pick rate',
        num: true,
        render: (r) => [pct(r.pickrate), el('small', {}, r.offered ? `of ${num(r.offered)}` : 'not offered')],
      },
      {
        key: 'playsPerRun',
        label: 'Plays',
        num: true,
        wide: true,
        render: (r) =>
          r.held_with_plays ? [fixed(r.playsPerRun, 1), el('small', {}, `${pct(r.unplayed)} unplayed`)] : '–',
      },
      { key: 'held', label: 'Runs', num: true, wide: true, render: (r) => num(r.held) },
    ],
    rows: listed,
    sort: { key: state.sort, dir: sortDir },
    onSort: ({ key }) => update({ sort: key }),
    onRow: (r) => openCard(r.id),
    limit: 25,
    empty: 'No cards match.',
  });

  const tracked = rows.filter((r) => r.held_with_plays >= state.min && r.rarity !== 'Token');
  const noPlays = `No runs in these filters count card plays yet. They are counted ${countedSince()}.`;
  const since = detailSince() ? ` Counted ${countedSince()}.` : '';
  $('unplayedNote').textContent = `The share of runs that finished with the card but never played it.${since}`;
  barList(
    $('unplayed'),
    [...tracked]
      .sort((a, b) => b.unplayed - a.unplayed)
      .map((r) => ({
        label: r.name,
        note: r.rarity,
        value: r.unplayed,
        text: pct(r.unplayed),
        textNote: `of ${num(r.held_with_plays)} runs`,
        onSelect: () => openCard(r.id),
      })),
    { limit: 10, empty: noPlays },
  );
  $('mostPlayedNote').textContent =
    `How many times a run that finished with the card played it, on average. Starting cards are left out.${since}`;
  barList(
    $('mostPlayed'),
    tracked
      .filter((r) => r.rarity !== 'Basic')
      .sort((a, b) => b.playsPerRun - a.playsPerRun)
      .map((r) => ({
        label: r.name,
        note: r.rarity,
        value: r.playsPerRun,
        text: fixed(r.playsPerRun, 1),
        textNote: 'a run',
        onSelect: () => openCard(r.id),
      })),
    { limit: 10, empty: noPlays },
  );

  const early = rows.filter((r) => r.early_picks >= state.min).sort((a, b) => b.early_picks - a.early_picks);
  barList(
    $('earlyPicks'),
    early.map((r) =>
      rateItem(r.name, r.early_pick_wins, r.early_picks, {
        note: `${num(r.early_picks)} picks`,
        onSelect: () => openCard(r.id),
      }),
    ),
    { max: 1, limit: 10, reference: overall, referenceLabel: `All runs together: ${pct(overall)}` },
  );

  const upgraded = rows
    .filter((r) => r.held >= state.min && r.upgraded > 0)
    .map((r) => ({ ...r, per100: (100 * r.upgraded) / r.held }))
    .sort((a, b) => b.per100 - a.per100);
  barList(
    $('upgrades'),
    upgraded.map((r) => ({
      label: r.name,
      note: r.rarity,
      value: r.per100,
      text: fixed(r.per100, 0),
      textNote: 'per 100',
      onSelect: () => openCard(r.id),
    })),
    { limit: 10 },
  );
}

// The card's art and text, then its stats in the current filters. A card no run in the filters
// finished with still shows what it is
function openCard(id) {
  const info = S.card_info[id];
  const r = cardRows(selected()).rows.find((row) => row.id === id);
  if (!r && !info) return;
  const head = asset(S.meta.assets?.character);
  $('sheetTitle').replaceChildren(head ? image(head, 'head') : '', info?.name ?? r.name);
  $('sheetSub').textContent = [
    [info?.rarity ?? r.rarity, info?.type].filter(Boolean).join(' '),
    ...(info?.themes ?? []),
    ...(info?.tags ?? []),
  ].join(' · ');
  const art = asset(info?.art);
  $('sheetFace').hidden = !info?.text;
  $('sheetFace').replaceChildren(
    art ? image(art, 'card-art') : '',
    el(
      'div',
      {},
      info?.cost && el('p', { class: 'card-cost' }, cardText(`Costs ${info.cost} [energy]`)),
      info?.text && el('p', { class: 'card-text' }, cardText(info.text)),
      el(
        'p',
        { class: 'card-version' },
        `Card text from ${S.meta.text_version}`,
        r ? `, stats from ${versionPhrase(state.version)}.` : '.',
      ),
    ),
  );
  $('sheetStats').hidden = !r;
  $('sheetEmpty').hidden = Boolean(r);
  if (r) cardStats(id, r);
  $('cardSheet').showModal();
}

function cardStats(id, r) {
  tiles($('sheetTiles'), [
    { label: 'Win rate', value: pct(r.winrate), note: `${num(r.held)} runs, likely ${range(wilson(r.held_wins, r.held))}` },
    {
      label: 'vs its peers',
      value: points(r.vsPeers),
      note: r.peer != null ? `the typical ${r.rarity} card wins ${pct(r.peer)}` : `${r.rarity} cards are not compared`,
    },
    {
      label: 'Pick rate',
      value: pct(r.pickrate),
      note: r.offered ? `picked ${num(r.picked)} of ${num(r.offered)} offers` : 'never offered as a card reward',
    },
    {
      label: 'As an early pick',
      value: pct(rate(r.early_pick_wins, r.early_picks)),
      note: r.early_picks ? `win rate over ${num(r.early_picks)} early picks` : 'never an early pick',
    },
    {
      label: 'Plays per run',
      value: fixed(r.playsPerRun, 1),
      note: r.held_with_plays
        ? `never played in ${pct(r.unplayed)} of ${num(r.held_with_plays)} runs`
        : `counted ${countedSince()}`,
    },
    {
      label: 'Two or more copies',
      value: pct(rate(r.held_twice_wins, r.held_twice)),
      note: r.held_twice ? `win rate over ${num(r.held_twice)} runs` : 'no run held two copies',
    },
  ]);
  const mine = { rows: CARDS.rows.filter((row) => row.card === id), counts: CARDS.counts };
  const byBand = [...sumBy(mine, selected({ ascension: 'all' }), (row) => groups.rows[row.group].ascension)].sort(
    (a, b) => a[0] - b[0],
  );
  barList(
    $('sheetAscension'),
    byBand.filter(([, g]) => g.held > 0).map(([band, g]) => rateItem(ASCENSION_BANDS[band], g.held_wins, g.held)),
    { max: 1, labelWidth: '9rem' },
  );
  const byVersion = [...sumBy(mine, selected({ version: 'all' }), (row) => groups.rows[row.group].version)]
    .filter(([, g]) => g.held > 0)
    .sort((a, b) => compareVersions(b[0], a[0]));
  barList(
    $('sheetVersions'),
    byVersion.map(([version, g]) => rateItem(version, g.held_wins, g.held)),
    { max: 1, limit: 6, labelWidth: '9rem' },
  );
}

// ---------- mechanics ----------

// A card, relic, potion or power named by a source tally. A card opens its sheet
function sourceOf(label) {
  if (label === 'unknown') return { label: 'Other' };
  const id = labelToId(label);
  return { label: nameOf(id), icon: iconOf(id), onSelect: S.card_info[id] ? () => openCard(id) : null };
}

async function renderMechanics(on) {
  CARDS ??= table((await load('cards.json')).cards);
  const t = sum(T.totals, on);
  const all = counters(on);
  const count = (key) => all.get(key)?.count || 0;
  const lostWithTally = t.runs_with_tally - t.wins_with_tally;
  const absorbed = t.poison_absorbed;
  const bled = t.poison_bled;

  // The schema 3 counters read only the groups whose client sends them. Enemy Poison counts only
  // in solo runs, so it has its own denominator
  const on3 = withDetail(on);
  const totals3 = sum(T.totals, on3);
  const runs3 = totals3.runs;
  const all3 = counters(on3);
  const count3 = (key) => all3.get(key)?.count || 0;
  const solo3 = on3.map((flag, i) => (flag && !groups.rows[i].coop ? 1 : 0));
  const soloRuns3 = sum(T.totals, solo3).runs;
  const notYet = `Counted ${countedSince()}.`;
  const detail = (runs, value, note) => (runs ? { value, note } : { value: '–', note: `counted ${countedSince()}` });

  tiles($('poisonTiles'), [
    { label: 'Poison per run', value: fixed(rate(t.poison_gained, t.runs_with_poison), 0), note: 'self-Poison gained' },
    {
      label: 'Poison peak',
      ...detail(runs3, fixed(rate(totals3.poison_peak, runs3), 0), 'the most held at once, on average'),
    },
    {
      label: 'Poison dealt',
      ...detail(soloRuns3, fixed(rate(counters(solo3).get('poison_dealt')?.count || 0, soloRuns3), 0), 'to enemies per solo run'),
    },
    {
      label: 'Lost to own Poison',
      value: pct(rate(t.poison_deaths, lostWithTally)),
      note: 'of lost runs ended on a Poison tick',
    },
  ]);
  histogramChart($('poisonPeakChart'), on3, 'poison_peak', 'Poison', {
    empty: `No runs in these filters count the self-Poison peak yet. ${notYet}`,
  });
  stackBar(
    $('poisonSplit'),
    [
      { label: 'Absorbed by Antitoxin', value: absorbed, fill: 'var(--antitoxin)' },
      { label: 'Hit HP', value: bled, fill: 'var(--bled)' },
    ],
    { format: num },
  );
  const ticks = count('tick_covered') + count('tick_bled');
  $('tickFoot').textContent = ticks
    ? `${pct(count('tick_covered') / ticks)} of the Poison ticks taken while holding Antitoxin were absorbed in full.`
    : '';

  const antitoxinSources = [...byPrefix(all3, 'atxsrc:')].sort((a, b) => b[1].count - a[1].count);
  const gained = antitoxinSources.reduce((n, [, c]) => n + c.count, 0);
  const decayed = count3('atx_decayed');
  tiles($('antitoxinTiles'), [
    {
      label: 'Antitoxin peak',
      value: fixed(rate(t.antitoxin_peak, t.runs_with_peak), 0),
      note: 'the most held at once, on average',
    },
    { label: 'Gained per run', ...detail(runs3, fixed(rate(gained, runs3), 0), 'from every source') },
    { label: 'Lost to decay', ...detail(runs3, pct(rate(decayed, gained)), 'of the Antitoxin gained') },
    { label: 'Absorbed', value: pct(rate(absorbed, absorbed + bled)), note: 'of self-Poison damage' },
  ]);
  badgeHistogram($('peakChart'), $('peakFoot'), on, 'ALCHEMIST-ANTITOXIN_PEAK', 'Antitoxin');
  barList(
    $('antitoxinSources'),
    antitoxinSources.map(([label, c]) => ({
      ...sourceOf(label),
      value: c.count / gained,
      text: pct(c.count / gained),
      textNote: num(c.count),
    })),
    { limit: 10, labelWidth: '11rem', empty: `No runs in these filters count Antitoxin sources yet. ${notYet}` },
  );
  $('decayFoot').textContent = gained
    ? `Of the Antitoxin gained, ${pct(decayed / gained)} thinned away at 1 a turn. The rest was still up when each fight ended.`
    : '';

  const made = byPrefix(all, 'mixmade:');
  const played = byPrefix(all, 'mixplay:');
  const madeTotal = totalCount(made);
  const playedTotal = totalCount(played);
  const fights = byPrefix(all, 'mixfight:');
  const fightBins = MIX_FIGHT_BUCKETS.map((bucket) => ({ bucket, fights: fights.get(bucket)?.count || 0 }));
  const fightTotal = fightBins.reduce((n, b) => n + b.fights, 0);
  let seen = 0;
  const typicalFight = fightBins.find((b) => (seen += b.fights) >= fightTotal / 2)?.bucket;
  tiles($('mixTiles'), [
    { label: 'Mixes per run', value: fixed(rate(t.mixes, t.runs_with_mixes), 0), note: 'created' },
    { label: 'Played', value: pct(rate(playedTotal, madeTotal)), note: 'of the Mixes created' },
    {
      label: 'Compound Mixes',
      value: fixed(rate(made.get('compound')?.count || 0, t.runs_with_tally), 1),
      note: 'per run',
    },
    { label: 'Typical fight', value: typicalFight ?? '–', note: 'Mixes played' },
  ]);
  const kinds = Object.keys(MIX_KINDS).filter((kind) => made.has(kind) || played.has(kind));
  barList(
    $('mixMade'),
    kinds
      .map((kind) => ({ kind, n: made.get(kind)?.count || 0 }))
      .sort((a, b) => b.n - a.n)
      .map(({ kind, n }) => ({
        label: MIX_KINDS[kind][0],
        dot: MIX_KINDS[kind][1],
        value: n / madeTotal,
        text: pct(n / madeTotal),
        textNote: num(n),
      })),
    { labelWidth: '9rem' },
  );
  barList(
    $('mixPlayed'),
    kinds
      .map((kind) => ({ kind, made: made.get(kind)?.count || 0, played: played.get(kind)?.count || 0 }))
      .filter((k) => k.made > 0)
      .sort((a, b) => b.played / b.made - a.played / a.made)
      .map((k) => ({
        label: MIX_KINDS[k.kind][0],
        dot: MIX_KINDS[k.kind][1],
        value: Math.min(1, k.played / k.made),
        text: pct(Math.min(1, k.played / k.made)),
        textNote: `${num(k.played)} of ${num(k.made)}`,
      })),
    { max: 1, labelWidth: '9rem' },
  );
  columns(
    $('mixFights'),
    fightBins.map((b) => ({
      label: b.bucket,
      value: b.fights,
      tip: [`${b.bucket} Mixes played`, `${num(b.fights)} fights (${pct(b.fights / fightTotal)})`],
    })),
    { height: 180, ariaLabel: 'Fights by the number of Mixes played in them' },
  );
  const sources = [...byPrefix(all, 'mixsrc:')].sort((a, b) => b[1].count - a[1].count);
  const sourceTotal = sources.reduce((n, [, c]) => n + c.count, 0);
  barList(
    $('mixSources'),
    sources.map(([label, c]) => ({
      ...sourceOf(label),
      value: c.count / sourceTotal,
      text: pct(c.count / sourceTotal),
      textNote: num(c.count),
    })),
    { limit: 10, labelWidth: '11rem' },
  );
  const pairs = [...byPrefix(all, 'pair:')].sort((a, b) => b[1].count - a[1].count);
  const pairTotal = pairs.reduce((n, [, c]) => n + c.count, 0);
  barList(
    $('mixPairs'),
    pairs.map(([pair, c]) => ({
      label: pair
        .split('+')
        .map((kind) => MIX_KINDS[kind]?.[0] ?? kind)
        .join(' + '),
      value: c.count / pairTotal,
      text: pct(c.count / pairTotal),
      textNote: num(c.count),
    })),
    { limit: 8, labelWidth: '13rem' },
  );

  // A discarded Mix can come back later in the fight, so an unplayed Mix was either combined, still
  // in a pile when the fight ended, or exhausted first (mostly Sparkling's Ethereal)
  const made3 = totalCount(byPrefix(all3, 'mixmade:'));
  const unplayed = Math.max(0, made3 - totalCount(byPrefix(all3, 'mixplay:')));
  const combined = count3('mixlost:combined');
  const leftover = count3('mixlost:leftover');
  $('mixLostNote').textContent = unplayed
    ? `${pct(unplayed / made3)} of the Mixes created were never played. This is where they went.`
    : '';
  barList(
    $('mixLost'),
    unplayed
      ? [
          ['Still in a pile when the fight ended', leftover],
          ['Combined into a Compound Mix', combined],
          ['Exhausted first, by Ethereal or another card', Math.max(0, unplayed - combined - leftover)],
        ]
          .sort((a, b) => b[1] - a[1])
          .map(([label, n]) => ({ label, value: n / unplayed, text: pct(n / unplayed), textNote: num(n) }))
      : [],
    { max: 1, labelWidth: '15rem', empty: `No runs in these filters count unplayed Mixes yet. ${notYet}` },
  );

  const fermentPlays = count('ferment_plays');
  tiles($('fermentTiles'), [
    {
      label: 'Turns fermented',
      value: fixed(rate(count('ferment_turns'), fermentPlays), 1),
      note: 'when a Ferment card is played, on average',
    },
    {
      label: 'Played unfermented',
      value: pct(rate(count('ferment_zero'), fermentPlays)),
      note: 'of Ferment card plays',
    },
    { label: 'Ferment plays', value: fixed(rate(fermentPlays, t.runs_with_tally), 0), note: 'per run' },
    {
      label: 'Earn Aged',
      value: pct(badgeShares(on, 'ALCHEMIST-FERMENTED').share(1)),
      note: 'of runs, or a higher tier',
    },
  ]);
  badgeHistogram($('fermentChart'), $('fermentFoot'), on, 'ALCHEMIST-FERMENTED', 'turns');
  $('fermentCardsNote').textContent =
    `The turns Fermented when a card is played, on average, for the Ferment cards in a run's final deck.` +
    (detailSince() ? ` Counted ${countedSince()}.` : '');
  barList(
    $('fermentCards'),
    [...sumBy(CARDS, on, (r) => r.card)]
      .filter(([, c]) => c.ferment_plays >= state.min)
      .map(([id, c]) => ({ id, turns: c.ferment_turns / c.ferment_plays, plays: c.ferment_plays }))
      .sort((a, b) => b.turns - a.turns)
      .map((r) => ({
        label: nameOf(r.id),
        value: r.turns,
        text: fixed(r.turns, 1),
        textNote: `over ${num(r.plays)} plays`,
        onSelect: () => openCard(r.id),
      })),
    { limit: 10, labelWidth: '11rem', empty: `No runs in these filters count Ferment turns per card yet. ${notYet}` },
  );

  tiles($('brewTiles'), [
    { label: 'Brews per run', value: fixed(rate(t.brews, t.runs), 1), note: 'at rest sites' },
    { label: 'Never Brewed', value: pct(rate(t.no_brew_runs, t.runs)), note: 'of runs, short ones included' },
    { label: 'Potions sold', value: fixed(rate(t.potions_sold, t.runs), 1), note: 'per run' },
    { label: 'Potions drunk', value: fixed(rate(t.potions_drunk, t.runs_with_drinks), 1), note: 'per run' },
  ]);
  const offers = byPrefix(all, 'brew_offer:');
  const picks = byPrefix(all, 'brew_pick:');
  const even = rate(totalCount(picks), totalCount(offers));
  $('brewNote').textContent =
    `How often each Brew potion is taken when a rest site offers it. A potion picked evenly sits near ${pct(even)}.`;
  barList(
    $('brewPicks'),
    [...offers]
      .filter(([, c]) => c.count >= state.min)
      .map(([label, c]) => {
        const picked = picks.get(label)?.count || 0;
        return rateItem(nameOf(labelToId(label)), picked, c.count, {
          note: `${num(c.count)} offers`,
          icon: iconOf(labelToId(label)),
        });
      })
      .sort((a, b) => b.value - a.value),
    { max: 1, reference: even, referenceLabel: `Even share: ${pct(even)}`, labelWidth: '11rem' },
  );

  renderBadges(on);
}

// A run-total histogram as columns. markers: [{at, label}] draw a line where that value's bin starts.
// Returns the runs it counted
function histogramChart(host, on, metric, unit, { markers = [], empty } = {}) {
  const { bins, width, last } = histogram(on, metric);
  const total = bins.reduce((n, b) => n + b.runs, 0);
  columns(
    host,
    bins.map((b) => ({
      label: b.bin === last ? `${b.bin}+` : String(b.bin),
      value: b.runs,
      tip: [
        b.bin === last ? `${b.bin} or more ${unit}` : `${b.bin} to ${b.bin + width - 1} ${unit}`,
        `${num(b.runs)} runs (${pct(b.runs / total)})`,
      ],
    })),
    {
      height: 200,
      markers: markers.map((m) => ({ before: m.at / width, label: m.label })),
      ariaLabel: `Runs by ${unit}`,
      empty,
    },
  );
  return total;
}

// A run-total histogram with a line where each badge tier starts, and a sentence with the shares
function badgeHistogram(chartHost, footHost, on, badgeId, unit) {
  const badge = badgeOf(badgeId);
  const total = histogramChart(chartHost, on, badge.metric, unit, {
    markers: badge.tiers.map((tier) => ({ at: tier.at, label: tier.title })),
  });
  const { share } = badgeShares(on, badgeId);
  const parts = badge.tiers.map((tier, i) => `${pct(share(i + 1))} ${tier.title}`);
  footHost.textContent = total
    ? `Runs that earned each tier or better: ${parts.slice(0, -1).join(', ')} and ${parts.at(-1)}.`
    : '';
}

function badgeName(id) {
  return id
    .replace(prefix(), '')
    .toLowerCase()
    .replace(/_/g, ' ')
    .replace(/^\w/, (c) => c.toUpperCase());
}

function renderBadges(on) {
  const host = $('badges');
  host.replaceChildren();
  for (const badge of S.meta.badges) {
    const { eligible, share } = badgeShares(on, badge.id);
    const list = el('div', { class: 'tiers' });
    const card = el(
      'article',
      { class: 'card badge' },
      el('h3', {}, asset(badge.icon) && image(asset(badge.icon), 'badge-icon'), badgeName(badge.id)),
      el(
        'p',
        { class: 'note' },
        badge.tiers[0].text,
        badge.needs_win ? ` Counted over ${num(eligible)} wins.` : ` Counted over ${num(eligible)} runs.`,
      ),
      list,
    );
    barList(
      list,
      eligible
        ? badge.tiers.map((tier, i) => ({
            label: tier.title,
            note: badge.tiers.length > 1 ? `${tier.at}+` : null,
            value: share(i + 1),
            text: pct(share(i + 1)),
            fill: TIER_FILL[i],
          }))
        : [],
      { max: 1, labelWidth: '11rem' },
    );
    host.append(card);
  }
}

// ---------- relics and potions ----------

let RELICS;
let POTIONS;

async function renderRelics(on) {
  if (!RELICS) {
    const data = await load('relics.json');
    RELICS = table(data.relics);
    POTIONS = table(data.potions);
  }
  const t = sum(T.totals, on);
  const relics = [...sumBy(RELICS, on, (r) => r.relic)].map(([id, g]) => ({ id, ...g, name: nameOf(id) }));
  const typical = median(relics.filter((r) => r.held >= state.min).map((r) => r.held_wins / r.held));
  const relicItem = (r) =>
    rateItem(r.name, r.held_wins, r.held, { note: `in ${pct(rate(r.held, t.runs))} of runs`, icon: iconOf(r.id) });
  const reference = { max: 1, reference: typical, referenceLabel: `Typical relic: ${pct(typical)}`, labelWidth: '13rem' };
  const mine = relics.filter((r) => r.id.startsWith(prefix()) && r.held >= state.min).sort((a, b) => b.held - a.held);
  barList($('modRelics'), mine.map(relicItem), reference);
  const base = relics.filter((r) => !r.id.startsWith(prefix()) && r.held >= state.min).sort((a, b) => b.held - a.held);
  barList($('baseRelics'), base.map(relicItem), { ...reference, limit: 12 });

  const offered = relics.filter((r) => r.offered >= state.min).sort((a, b) => b.offered - a.offered);
  const even = rate(
    offered.reduce((n, r) => n + r.picked, 0),
    offered.reduce((n, r) => n + r.offered, 0),
  );
  barList(
    $('ancients'),
    offered.map((r) => rateItem(r.name, r.picked, r.offered, { note: `${num(r.offered)} offers`, icon: iconOf(r.id) })),
    { max: 1, limit: 10, reference: even, referenceLabel: `Even share: ${pct(even)}`, labelWidth: '11rem' },
  );

  const potions = [...sumBy(POTIONS, on, (r) => r.potion)]
    .map(([id, g]) => ({ id, ...g, per100: (100 * g.drunk) / (t.runs_with_drinks || 1) }))
    .filter((p) => p.drunk >= state.min)
    .sort((a, b) => b.per100 - a.per100);
  barList(
    $('potions'),
    potions.map((p) => ({
      label: nameOf(p.id),
      icon: iconOf(p.id),
      value: p.per100,
      text: fixed(p.per100, 0),
      textNote: p.bought ? `${num(p.bought)} bought` : null,
    })),
    { limit: 12, labelWidth: '11rem' },
  );
}

// ---------- fights ----------

let ENCOUNTERS;

async function renderFights(on) {
  ENCOUNTERS ??= table((await load('fights.json')).encounters);
  const floors = sumBy(T.death_floors, on, (r) => r.floor);
  const deaths = [...floors.values()].reduce((n, f) => n + f.deaths, 0);
  const top = Math.max(0, ...floors.keys());
  const series = [];
  for (let floor = 1; floor <= top; floor++) {
    const n = floors.get(floor)?.deaths || 0;
    series.push({
      label: String(floor),
      value: n,
      tip: [`Floor ${floor}`, `${num(n)} runs ended here`, `${pct(n / deaths)} of lost runs`],
    });
  }
  columns($('deathFloors'), series, { height: 200, ariaLabel: 'Lost runs by the floor they ended on' });

  const rows = [...sumBy(ENCOUNTERS, on, (r) => r.encounter)]
    .filter(([, g]) => g.fights >= state.min)
    .map(([id, g]) => ({
      id,
      ...encounterOf(id),
      fights: g.fights,
      deaths: g.deaths,
      lethality: rate(g.deaths, g.fights),
      damage: g.damage / g.fights,
      turns: g.turns / g.fights,
    }));
  const sort = encounterSort;
  dataTable($('encounters'), {
    columns: [
      {
        key: 'name',
        label: 'Fight',
        render: (r) => [r.name, el('small', {}, `${r.kind}, ${num(r.fights)} fights`)],
      },
      {
        key: 'deaths',
        label: 'Runs ended',
        num: true,
        render: (r) => [num(r.deaths), el('small', {}, `${pct(r.lethality)} of fights`)],
      },
      { key: 'lethality', label: 'Ends the run', num: true, wide: true, render: (r) => pct(r.lethality) },
      { key: 'damage', label: 'Damage', num: true, wide: true, render: (r) => fixed(r.damage, 1) },
      { key: 'turns', label: 'Turns', num: true, wide: true, render: (r) => fixed(r.turns, 1) },
    ],
    rows,
    sort,
    onSort: (next) => {
      encounterSort = next;
      render();
    },
    limit: 15,
  });

  const acts = [...sumBy(T.acts, on, (r) => r.act)].filter(([act, g]) => act > 0 && g.fights).sort((a, b) => a[0] - b[0]);
  dataTable($('acts'), {
    columns: [
      { key: 'act', label: 'Act', render: (r) => `Act ${r.act}` },
      { key: 'fights', label: 'Fights', num: true, render: (r) => num(r.fights) },
      { key: 'turns', label: 'Turns', num: true, render: (r) => fixed(r.turns, 1) },
      { key: 'damage', label: 'Damage', num: true, render: (r) => fixed(r.damage, 1) },
    ],
    rows: acts.map(([act, g]) => ({ act, fights: g.fights, turns: g.turns / g.fights, damage: g.damage / g.fights })),
    sort: { key: 'act', dir: 1 },
  });
}

let encounterSort = { key: 'deaths', dir: -1 };

// ---------- versions ----------

async function renderVersions() {
  CARDS ??= table((await load('cards.json')).cards);
  const a = $('compareA').value;
  const b = $('compareB').value;
  if (!a || !b || a === b) {
    $('compareTiles').replaceChildren(emptyNote('Pick two different versions.'));
    $('cardChanges').replaceChildren();
    return;
  }
  const onA = selected({ version: a });
  const onB = selected({ version: b });
  const tA = sum(T.totals, onA);
  const tB = sum(T.totals, onB);
  const change = (hitsA, nA, hitsB, nB) => {
    const delta = rate(hitsB, nB) - rate(hitsA, nA);
    return Number.isNaN(delta) ? null : { text: points(delta), up: delta >= 0 };
  };
  const winZ = zScore(tA.wins, tA.runs, tB.wins, tB.runs);
  tiles($('compareTiles'), [
    { label: `Runs in ${b}`, value: num(tB.runs), note: `${num(tA.runs)} in ${a}` },
    {
      label: 'Win rate',
      value: pct(rate(tB.wins, tB.runs)),
      delta: change(tA.wins, tA.runs, tB.wins, tB.runs),
      note: `was ${pct(rate(tA.wins, tA.runs))}, ${Math.abs(winZ) > 1.96 ? 'a likely change' : 'within random swing'}`,
    },
    {
      label: 'Reach Act 3',
      value: pct(rate(tB.reached_act3, tB.runs)),
      delta: change(tA.reached_act3, tA.runs, tB.reached_act3, tB.runs),
      note: `was ${pct(rate(tA.reached_act3, tA.runs))}`,
    },
    {
      label: 'Antitoxin peak',
      value: fixed(rate(tB.antitoxin_peak, tB.runs_with_peak), 0),
      note: `was ${fixed(rate(tA.antitoxin_peak, tA.runs_with_peak), 0)}, on average`,
    },
  ]);

  // Each side needs enough runs for a rate to mean something, so the floor here is at least 20.
  // A version that is harder overall drags every card down with it, so each card's change is
  // measured against the change of the typical card of its rarity
  const enough = Math.max(state.min, 20);
  const cardsA = sumBy(CARDS, onA, (r) => r.card);
  const cardsB = sumBy(CARDS, onB, (r) => r.card);
  const peerMedian = (cards, rarity) =>
    median(
      [...cards]
        .filter(([id, c]) => S.card_info[id]?.rarity === rarity && c.held >= enough)
        .map(([, c]) => c.held_wins / c.held),
    );
  const peerShift = Object.fromEntries(
    POOL_RARITIES.map((rarity) => [rarity, (peerMedian(cardsB, rarity) ?? 0) - (peerMedian(cardsA, rarity) ?? 0)]),
  );
  const rows = [];
  for (const [id, ca] of cardsA) {
    const cb = cardsB.get(id);
    const rarity = S.card_info[id]?.rarity;
    if (!cb || ca.held < enough || cb.held < enough || !POOL_RARITIES.includes(rarity)) continue;
    const winA = ca.held_wins / ca.held;
    const winB = cb.held_wins / cb.held;
    const relative = winB - winA - peerShift[rarity];
    const se = Math.sqrt((winA * (1 - winA)) / ca.held + (winB * (1 - winB)) / cb.held);
    const z = se > 0 ? relative / se : 0;
    rows.push({
      id,
      name: nameOf(id),
      rarity,
      winA,
      winB,
      relative,
      confidence: Math.abs(z),
      likely: Math.abs(z) > LIKELY_Z,
      pickDelta: ca.offered && cb.offered ? cb.picked / cb.offered - ca.picked / ca.offered : null,
    });
  }
  dataTable($('cardChanges'), {
    columns: [
      {
        key: 'name',
        label: 'Card',
        render: (r) => [r.name, el('small', {}, `${r.rarity}, won ${pct(r.winA)} → ${pct(r.winB)}`)],
      },
      {
        key: 'confidence',
        label: 'vs peers',
        num: true,
        render: (r) => [points(r.relative), r.likely && el('small', { class: 'likely' }, 'likely')],
      },
      { key: 'pickDelta', label: 'Pick rate', num: true, wide: true, render: (r) => points(r.pickDelta) },
    ],
    rows,
    sort: { key: 'confidence', dir: -1 },
    onRow: (r) => openCard(r.id),
    limit: 15,
    empty: `No card has ${enough} runs in both versions yet.`,
  });
}

// ---------- page ----------

const RENDERERS = {
  overview: renderOverview,
  cards: renderCards,
  mechanics: renderMechanics,
  relics: renderRelics,
  fights: renderFights,
  versions: renderVersions,
};

let renderToken = 0;

async function render() {
  const token = ++renderToken;
  writeUrl();
  const on = selected();
  const totals = sum(T.totals, on);
  describeFilters(totals.runs);
  for (const view of VIEWS) {
    const tab = document.querySelector(`[data-view="${view}"]`);
    $(`view-${view}`).hidden = view !== state.view;
    tab.setAttribute('aria-selected', String(view === state.view));
    tab.tabIndex = view === state.view ? 0 : -1;
  }
  const host = $(`view-${state.view}`);
  host.style.opacity = '';
  if (!totals.runs) {
    host.hidden = true;
    $('loadError').hidden = false;
    $('loadError').textContent = 'No runs match these filters. Try a wider version range or another ascension.';
    return;
  }
  $('loadError').hidden = true;
  try {
    host.style.opacity = '0.6';
    await RENDERERS[state.view](on);
  } catch (error) {
    console.error(error);
    $('loadError').hidden = false;
    $('loadError').textContent = `Something went wrong drawing this tab (${error.message}).`;
  } finally {
    if (token === renderToken) host.style.opacity = '';
  }
}

function update(changes) {
  Object.assign(state, changes);
  render();
}

function wireControls() {
  const bind = (id, key, parse = (v) => v) =>
    $(id).addEventListener('change', (e) => update({ [key]: parse(e.target.value) }));
  bind('fVersion', 'version');
  bind('fAscension', 'ascension');
  bind('fPlayers', 'players');
  bind('fPool', 'pool');
  bind('fBuild', 'build');
  bind('fMin', 'min', Number);
  bind('cardSort', 'sort');
  bind('compareA', 'a');
  bind('compareB', 'b');
  let typing = 0;
  $('cardSearch').addEventListener('input', (e) => {
    clearTimeout(typing);
    typing = setTimeout(() => update({ q: e.target.value.trim() }), 150);
  });
  $('filters').addEventListener('submit', (e) => e.preventDefault());
  document.querySelectorAll('[data-view]').forEach((tab) =>
    tab.addEventListener('click', () => {
      update({ view: tab.dataset.view });
      tab.scrollIntoView({ block: 'nearest', inline: 'nearest' });
      // The tab bar is sticky, so its own offset moves. The new tab starts at the top of main
      const top = document.querySelector('main').offsetTop - $('tabs').offsetHeight - 18;
      if (window.scrollY > top) window.scrollTo(0, top);
    }),
  );
  $('tabs').addEventListener('keydown', (e) => {
    const step = { ArrowRight: 1, ArrowLeft: -1 }[e.key];
    if (!step) return;
    const next = VIEWS[(VIEWS.indexOf(state.view) + step + VIEWS.length) % VIEWS.length];
    document.querySelector(`[data-view="${next}"]`).focus();
    update({ view: next });
  });
  $('cardSheet').addEventListener('click', (e) => {
    if (e.target === $('cardSheet')) $('cardSheet').close();
  });
  window.addEventListener('hashchange', () => {
    readUrl();
    syncControls();
    render();
  });
  let lastWidth = window.innerWidth;
  let resizing = 0;
  window.addEventListener('resize', () => {
    clearTimeout(resizing);
    resizing = setTimeout(() => {
      if (Math.abs(window.innerWidth - lastWidth) < 20) return;
      lastWidth = window.innerWidth;
      render();
    }, 200);
  });
}

async function init() {
  try {
    S = await load('summary.json');
  } catch (error) {
    $('lede').textContent = 'No stats yet. The nightly export has not run, or nothing has been uploaded.';
    console.error(error);
    return;
  }
  groups = table(S.groups);
  T = Object.fromEntries(
    ['totals', 'ascensions', 'days', 'themes', 'badges', 'histograms', 'counters', 'acts', 'death_floors'].map(
      (name) => [name, table(S[name])],
    ),
  );
  recentStart = findRecentStart();

  const { meta } = S;
  const head = asset(meta.assets?.character);
  if (head) {
    $('favicon').href = head;
    Object.assign($('brandIcon'), { src: head, hidden: false });
  }
  $('lede').textContent = `Stats from ${num(meta.total_runs)} Alchemist runs shared by ${num(meta.players)} players. Updated every night.`;
  const updated = new Date(meta.generated_at).toLocaleString('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'UTC',
  });
  $('fresh').textContent = `Last updated ${updated} UTC, with runs from ${meta.first_day} to ${meta.last_day}.`;

  fillFilters();
  readUrl();
  syncControls();
  wireControls();
  keepScroll();
  $('filters').hidden = false;
  $('tabs').hidden = false;
  await render();
  restoreScroll();
}

init();
