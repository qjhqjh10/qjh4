# -*- coding: utf-8 -*-
"""独立校验：`CampaignData.cs`（601005b^ 那一版，= 我们准备还原成的样子）
两张表是否与 47 个原版 SO 逐行吻合。只读。"""
import io, re, json, glob, subprocess

ROOT = 'd:/4/'
REL = 'Unity/MyGame/Assets/CardPresentation/Shell/CampaignData.cs'
txt = subprocess.check_output(['git', '-C', ROOT, 'show', '601005b^:' + REL]).decode('utf-8')

# ---- SO 侧 ----
so = {}
for p in glob.glob(ROOT.replace('d:/4', 'd:/2') + '/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json'):
    d = json.load(io.open(p, encoding='utf-8', errors='replace'))
    so[d['NodeId']] = d
print('SOs:', len(so))

# ---- C# 侧 ----
nodes = re.findall(r'new NodeSpec\("([^"]+)",\s*(-?\d+),\s*(-?\d+),\s*(\d+),\s*(true|false),\s*(true|false),',
                   txt)
print('NodeSpec rows:', len(nodes))
bad = 0
for nid, x, y, cost, rep, prem in nodes:
    d = so.get(nid)
    if d is None:
        print('  !! NodeId not in SO:', nid); bad += 1; continue
    if (float(x), float(y)) != (d['position']['x'], d['position']['y']):
        print('  !! pos mismatch', nid, (x, y), (d['position']['x'], d['position']['y'])); bad += 1
    if int(cost) != d['Cost']:
        print('  !! cost mismatch', nid, cost, d['Cost']); bad += 1
    if (rep == 'true') != bool(d['IsRepeatable']):
        print('  !! repeatable mismatch', nid, rep, d['IsRepeatable']); bad += 1
    hp = any(r['rewardTier'] == 10 for r in d['Rewards'])
    if (prem == 'true') != hp:
        print('  !! premium mismatch', nid, prem, hp); bad += 1
print('Nodes mismatches:', bad)

rows = re.findall(r'new RewardSpec\[\]\s*\{(.*?)\}\s*,?\s*(?://\s*(\S+))?', txt)
print('Rewards rows:', len(rows))
bad2 = 0
for body, tag in rows:
    if tag is None:
        print('  !! row without comment:', body[:60]); bad2 += 1; continue
    d = so.get(tag)
    if d is None:
        print('  !! row tag not an SO NodeId:', tag); bad2 += 1; continue
    mine = re.findall(r'new RewardSpec\("([^"]+)",\s*(-?\d+),\s*Tier(Basic|Premium)\)', body)
    truth = [(r['item']['targetId'], r['quantity'], 'Premium' if r['rewardTier'] == 10 else 'Basic')
             for r in d['Rewards']]
    if mine != truth:
        print('  !! rewards differ', tag)
        print('      ours :', mine)
        print('      truth:', truth)
        bad2 += 1
print('Rewards mismatches:', bad2)
