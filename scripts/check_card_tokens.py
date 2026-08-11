#!/usr/bin/env python3
"""Check that every {Token} in a card description resolves to a var the card declares.

The three-way lint compares cards.csv against the localization and the class list, but it
never looks at the C# values, so a description can name a var the card does not have. One
unknown token makes the formatter give up and the card renders its raw text in game, which
is how Prime shipped showing "{CalculatedBlock:diff()}" to players.

Run: python3 scripts/check_card_tokens.py   (exits 1 on any unresolved token)
"""
import json, re, os, glob, sys
cards = json.load(open('Alchemist/localization/eng/cards.json', encoding='utf-8'))
GLOBAL = {'IfUpgraded','FermentSuffix','FermentTotal','HitsLine','energyIcons','energyPrefix',
          'singleStarIcon','MaxCount','MinCount','Amount'}
BASE = open('AlchemistCode/Cards/AlchemistCard.cs', encoding='utf-8').read()
# AlchemistCard injects these for every card that overrides the matching preview property
BASE_ARGS = set(re.findall(r'description\.Add\("(\w+)"', BASE))

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
    for tok in re.findall(r'\{(\w+)[:}]', text):
        if tok not in have: bad.append((key,'unknown token',tok))
n = sum(1 for k in cards if k.endswith('.description'))
print(f'checked {n} card descriptions')
for k,w,t in bad: print(f'  {w:14} {t:18} {k}')
print('  OK: every token resolves' if not bad else f'  {len(bad)} problem(s)')
sys.exit(1 if bad else 0)
