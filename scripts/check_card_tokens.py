#!/usr/bin/env python3
"""Check that a card description and the card's C# agree in both directions.

The three-way lint compares cards.csv against the localization and the class list, but it
never looks at the C# values, so the two can disagree two ways:

  unknown token   the description names a var the card does not have. One of these makes the
                  formatter give up and the card renders its raw text in game, which is how
                  Prime shipped showing "{CalculatedBlock:diff()}" to players.
  unused inject   the card builds a string with description.Add and the description never
                  places it. Nothing breaks, it just silently goes missing, which is how
                  Hemorrhage lost its damage and HP-loss forecasts.

Run: python3 scripts/check_card_tokens.py   (exits 1 on either)
"""
import json, re, os, glob, sys
cards = json.load(open('Alchemist/localization/eng/cards.json', encoding='utf-8'))
GLOBAL = {'IfUpgraded','FermentSuffix','FermentTotal','HitsLine','energyIcons','energyPrefix',
          'singleStarIcon','MaxCount','MinCount','Amount',
          # CardModel.GetDescriptionForPile adds these to every card description
          'InCombat','OnTable','IsTargeting','TargetType','GainsBlock'}
BASE = open('AlchemistCode/Cards/AlchemistCard.cs', encoding='utf-8').read()
# AlchemistCard injects these for every card that overrides the matching preview property
BASE_ARGS = set(re.findall(r'description\.Add\("(\w+)"', BASE))
# Base-class preview property -> the token its value is placed with
OPT_IN = {'RawFormulaDamagePreview': 'FormulaDamage',
          'FormulaHpLossPreview': 'FormulaHpLoss',
          'FermentPeak': 'FermentSuffix',
          'FermentTotalText': 'FermentTotal'}

def declared(src):
    v = set(BASE_ARGS)
    if re.search(r'\bWithDamage\(', src): v |= {'Damage'}
    if re.search(r'\bWithBlock\(', src): v |= {'Block'}
    if re.search(r'\bWithCards\(', src): v |= {'Cards'}
    if re.search(r'\bWithEnergy\(', src): v |= {'Energy'}
    if re.search(r'\bWithCalculatedDamage\(', src): v |= {'CalculatedDamage','CalculationBase','ExtraDamage'}
    if re.search(r'\bWithCalculatedBlock\(', src): v |= {'CalculatedBlock','CalculationBase','CalculationExtra'}
    v |= set(re.findall(r'WithVar\("(\w+)"', src))
    v |= set(re.findall(r'WithCalculatedVar\("(\w+)"', src))
    v |= set(re.findall(r'WithPower<(\w+)>', src))
    v |= {m[:-3] if m.endswith('Var') else m for m in re.findall(r'WithVar\(new (\w+)\(', src)}
    v |= set(re.findall(r'description\.Add\("(\w+)"', src))
    return v

srcs = {os.path.basename(f)[:-3]: open(f, encoding='utf-8').read()
        for f in glob.glob('AlchemistCode/Cards/**/*.cs', recursive=True)}
def cls_for(key):
    cand = key.split('.')[0].replace('ALCHEMIST-','').replace('_','').lower()
    return next((c for c in srcs if c.lower() == cand), None)

bad = []
for key, text in cards.items():
    if not key.endswith('.description'): continue
    c = cls_for(key)
    if c is None:
        bad.append((key,'NO CLASS','')); continue
    have = declared(srcs[c]) | GLOBAL
    used = set(re.findall(r'\{(\w+)[:}]', text))
    for tok in used:
        if tok not in have: bad.append((key,'unknown token',tok))
    for tok in re.findall(r'description\.Add\("(\w+)"', srcs[c]):
        if tok not in used: bad.append((key,'unused inject',tok))
    # A base-class inject is opt-in: the card overrides a preview property to switch it on, so the
    # description has to place the matching token or the value is built and thrown away
    for prop, tok in OPT_IN.items():
        if re.search(r'override .*\b%s\b' % prop, srcs[c]) and tok not in used:
            bad.append((key,'unused inject',tok))
n = sum(1 for k in cards if k.endswith('.description'))
print(f'checked {n} card descriptions')
for k,w,t in bad: print(f'  {w:14} {t:18} {k}')
print('  OK: every token resolves' if not bad else f'  {len(bad)} problem(s)')
sys.exit(1 if bad else 0)
