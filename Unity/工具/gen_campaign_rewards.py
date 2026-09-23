#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_campaign_rewards.py — 从 `Campaign Node Data` SO 抄出**每个节点的奖励表**（生成 C# 表）。

为什么要它：`Campaign Reward Window` 的两列（Base / Premium）**画什么**，就是 `context.Rewards`
（= 节点 SO 的 `Rewards[]`）按 `rewardTier` 分组的结果。手抄 47 个节点 / 95 条奖励必错
（上一个会话抄节点坐标也是脚本抄的，见 `资料/阶段二_锻造厂与战役页_原版规格.md` §十二）。

出处：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json`
字段：`Rewards[] = { item.targetId, quantity, rewardTier }` · `rewardTier{Basic=0, Premium=10}`。

用法（输出直接贴进 `CampaignData.cs` 的 `Rewards` 表）：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/gen_campaign_rewards.py
"""
import glob
import io
import json
import os
import sys

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

SRC = 'd:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour'


def main():
    ns = []          # (nodeId, [(targetId, qty, tier), ...])
    for f in glob.glob(os.path.join(SRC, 'Campaign Node Data*.json')):
        j = json.load(io.open(f, encoding='utf-8'))
        rw = [(r['item']['targetId'], int(r['quantity']), int(r['rewardTier']))
              for r in (j.get('Rewards') or [])]
        ns.append((j['NodeId'], rw))
    # 与 `CampaignData.Nodes` 同序：按数字后缀排（UM0, UM1, … UM13, UM15, …）
    ns.sort(key=lambda t: int(t[0][2:]))

    print('// ==== 由 `工具/gen_campaign_rewards.py` 从 SO 生成，别手改 ====')
    for nid, rw in ns:
        if not rw:
            print('            new RewardSpec[] { },\t\t// %s' % nid)
            continue
        items = ', '.join('new RewardSpec("%s", %d, %s)' % (t, q, 'TierPremium' if tier == 10 else 'TierBasic')
                          for (t, q, tier) in rw)
        print('            new RewardSpec[] { %s },\t// %s' % (items, nid))

    tiers = {}
    ids = {}
    for _, rw in ns:
        for (t, q, tier) in rw:
            tiers[tier] = tiers.get(tier, 0) + 1
            ids[t] = ids.get(t, 0) + 1
    sys.stderr.write('\n节点 %d 个 · 奖励 %d 条 · 档分布 %s · 唯一 targetId %d 个\n'
                     % (len(ns), sum(tiers.values()), tiers, len(ids)))
    sys.stderr.write('唯一 targetId：\n')
    for k, v in sorted(ids.items(), key=lambda kv: (-kv[1], kv[0])):
        sys.stderr.write('   %-28s %d\n' % (k, v))
    return 0


if __name__ == '__main__':
    sys.exit(main())
