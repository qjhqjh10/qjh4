# -*- coding: utf-8 -*-
"""独立校验：**当前工作树**的 `Shell/CampaignData.cs` 两张表是否与 47 个原版 SO 逐行吻合。

用法（无参数、无第三方依赖）：
    python -I -X utf8 d:/4/Unity/工具/verify_campaign_vs_so.py

返回码：0 = 节点表 + 奖励表 **0 处不符**；1 = 有不符（逐条打印）；2 = 输入缺失。

🔴 **2026-10-19（SC-4）从 `工具/_oneoff_1015_p1fix/` 搬出并修好两处病**（判据 →
`资料/普查产出_1019/W57_自检线_A1967残尾与A2038.md` §五·1）：
  ① 原来 `:8` 读 `git show 601005b^:…`（**历史提交**，不是工作树）⇒ 后面每改一次这个脚本都在验一份
     四个月前的旧文件；现在直接读工作树。
  ② 原来 `:46` 把正则捕到的 `quantity`（`str`）直接和 SO 的 `quantity`（`int`）比
     ⇒ `('Booster Pack…','1','Basic') != ('Booster Pack…',1,'Basic')` ⇒ **47 条全假红**；
     现在显式 `int(quantity)`。
⚠️ 它只读，不写任何文件（`python -I` 下运行时不会加载工作目录里的同名模块）。
"""
import glob
import io
import json
import os
import re
import sys

ROOT = 'd:/4'
CS = os.path.join(ROOT, 'Unity/MyGame/Assets/CardPresentation/Shell/CampaignData.cs')
# 原版资源库（本机固定只读路径；不在 d:/4 仓库里）
SO_DIR = 'd:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour'
SO_GLOB = 'Campaign Node Data*.json'


def fail(msg):
    print('[ERR] ' + msg)


def main():
    if not os.path.isfile(CS):
        fail('工作树里找不到 ' + CS)
        return 2
    # utf-8-sig：工作树文件可能是无 BOM / 带 BOM 两种（BOM 不影响下面的正则）
    txt = io.open(CS, encoding='utf-8-sig', errors='replace').read()

    # ---- SO 侧（47 个 MenuBehaviour 资产）----
    so = {}
    files = glob.glob(os.path.join(SO_DIR, SO_GLOB))
    for p in files:
        try:
            d = json.load(io.open(p, encoding='utf-8', errors='replace'))
        except Exception as e:  # 坏文件要出声，不许静默跳过
            fail('读不动 SO：%s（%s）' % (p, e))
            return 2
        nid = d.get('NodeId')
        if not nid:
            fail('SO 没有 NodeId：' + p)
            return 2
        if nid in so:
            fail('SO NodeId 重复：%s（%s）' % (nid, p))
            return 2
        so[nid] = d
    print('SO files: %d / unique NodeIds: %d' % (len(files), len(so)))
    if len(so) != 47:
        fail('期望 47 个原版 SO，实得 %d 个（资源库路径：%s）' % (len(so), SO_DIR))
        return 2

    # ---- C# 侧：节点表 ----
    node_re = re.compile(
        r'new\s+NodeSpec\(\s*"([^"]+)"\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,'
        r'\s*(true|false)\s*,\s*(true|false)\s*,')
    nodes = node_re.findall(txt)
    print('NodeSpec rows: %d' % len(nodes))
    bad_nodes = 0
    seen = set()
    for nid, x, y, cost, rep, prem in nodes:
        seen.add(nid)
        d = so.get(nid)
        if d is None:
            print('  !! NodeId not in SO: %s' % nid)
            bad_nodes += 1
            continue
        pos = d.get('position') or {}
        want_pos = (pos.get('x'), pos.get('y'))
        if (int(x), int(y)) != (float(want_pos[0]), float(want_pos[1])):
            print('  !! pos mismatch %s ours=(%s,%s) so=%s' % (nid, x, y, want_pos))
            bad_nodes += 1
        if int(cost) != int(d.get('Cost')):
            print('  !! cost mismatch %s ours=%s so=%s' % (nid, cost, d.get('Cost')))
            bad_nodes += 1
        if (rep == 'true') != bool(d.get('IsRepeatable')):
            print('  !! repeatable mismatch %s ours=%s so=%s' % (nid, rep, d.get('IsRepeatable')))
            bad_nodes += 1
        hp = any(r.get('rewardTier') == 10 for r in (d.get('Rewards') or []))
        if (prem == 'true') != hp:
            print('  !! premium mismatch %s ours=%s so=%s' % (nid, prem, hp))
            bad_nodes += 1
    missing = sorted(set(so) - seen)
    extra = sorted(seen - set(so))
    if missing or extra:
        # 47 vs 47 时两边差集都为空；任一非空 = 有节点漏抄 / 多抄
        print('  !! node set differs: missing=%s extra=%s' % (missing, extra))
        bad_nodes += len(missing) + len(extra)
    print('Node mismatches: %d' % bad_nodes)

    # ---- C# 侧：奖励表 ----
    row_re = re.compile(r'new\s+RewardSpec\[\]\s*\{(.*?)\}\s*,?\s*(?://\s*(\S+))?', re.S)
    rows = row_re.findall(txt)
    print('Rewards rows: %d' % len(rows))
    item_re = re.compile(
        r'new\s+RewardSpec\(\s*"([^"]+)"\s*,\s*(-?\d+)\s*,\s*Tier(Basic|Premium)\s*\)')
    bad_rewards = 0
    tags = set()
    for body, tag in rows:
        if tag is None:
            print('  !! row without // NodeId comment: %s' % body[:80].replace('\n', ' '))
            bad_rewards += 1
            continue
        if tag in tags:
            print('  !! duplicate reward row tag: %s' % tag)
            bad_rewards += 1
        tags.add(tag)
        d = so.get(tag)
        if d is None:
            print('  !! row tag not an SO NodeId: %s' % tag)
            bad_rewards += 1
            continue
        # 🔴 修的就是这里：quantity 是 **int**（原来拿 str 比 int ⇒ 47 条全假红）
        mine = [(tid, int(qty), tier) for tid, qty, tier in item_re.findall(body)]
        truth = [(r['item']['targetId'], int(r['quantity']),
                  'Premium' if r.get('rewardTier') == 10 else 'Basic')
                 for r in (d.get('Rewards') or []) if r.get('item')]
        if mine != truth:
            print('  !! rewards differ %s' % tag)
            print('      ours :', mine)
            print('      truth:', truth)
            bad_rewards += 1
    missing_rows = sorted(set(so) - tags)
    extra_rows = sorted(tags - set(so))
    if missing_rows or extra_rows:
        print('  !! reward row set differs: missing=%s extra=%s' % (missing_rows, extra_rows))
        bad_rewards += len(missing_rows) + len(extra_rows)
    print('Rewards mismatches: %d' % bad_rewards)

    total = bad_nodes + bad_rewards
    print('TOTAL mismatches: %d' % total)
    if total == 0:
        print('OK: 节点表 %d 行 + 奖励表 %d 行 与 %d 份原版 SO 逐行吻合（0 处不符）'
              % (len(nodes), len(rows), len(so)))
        return 0
    return 1


if __name__ == '__main__':
    sys.exit(main())
