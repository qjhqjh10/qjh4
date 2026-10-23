# -*- coding: utf-8 -*-
"""P1 收尾 · ①（CampaignData.cs 那一支）—— 把 NodeId / reward targetId 两列【还原成原版 SO 原文】。

⚠️ 为什么**不是**写卡 id：
  `CampaignData.cs` 的 `Nodes` 与 `Rewards` 两张表，文件自己的表头写着
  「本表由脚本从 SO 直接抄出（不手抄）：源 = `bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json`」。
  ① `Nodes[i].Id` 是**原版 `NodeId`**（`UM0`–`UM47`，缺 `UM14`）；
  ② `Rewards[i][k].Id` 是**原版 `Rewards[].item.targetId`**（真值 = 原版 `allcards` 的 `uniqueId`，短 id 或 GUID 两种形态都有）。
  601005b（第二十六会话，2026-10-14）做「整仓扫写死的旧 id」时**盲替换**了这张表 ——
  原版 `NodeId "UM1"` 被当成「我们的旧卡 id `UM1`」换成了 `"UM46"`，26 行 NodeId + 9 行 reward targetId 一起被改坏。
  判据（三重）：
   a. `git show 601005b^:…/CampaignData.cs` = 原版逐字（与 SO 逐行吻合）；
   b. 现读 47 个 SO 的 `NodeId` 与 `position` 配对 —— 26 行对不上；
   c. 原版 `allcards`（`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`）证明
      SO 的 reward `targetId`（`UM2`/`UM34`/`UM13`…）就是原版卡的 `uniqueId`，也正是我们卡池现在的 id。

做法：把 `601005b^` 那一版里**变化的行**逐行贴回去（按行号 + 内容双断言）。
"""
import io, subprocess, sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/_oneoff_1015_p1fix/restore_campaign_types.py [--write]\n  说明：P1 收尾 · ①（CampaignData.cs 那一支）—— 把 NodeId / reward targetId 两列【还原成原版 SO 原文】。\n  选项：--write（脚本内按字符串匹配；一个都不带 = 默认模式）')
    _wf_help_sys.exit(0)

ROOT = 'd:/4/'
REL = 'Unity/MyGame/Assets/CardPresentation/Shell/CampaignData.cs'
P = ROOT + REL

before_txt = subprocess.check_output(
    ['git', '-C', ROOT, 'show', '601005b^:' + REL]).decode('utf-8')
cur_txt = io.open(P, encoding='utf-8').read()

# 行尾必须一致（本文件是纯 LF：CRLF==0）
assert before_txt.count('\r\n') == 0 and cur_txt.count('\r\n') == 0, 'line-ending surprise'

b = before_txt.split('\n')
c = cur_txt.split('\n')
assert len(b) == len(c), 'line count differs: %d vs %d' % (len(b), len(c))

diff = [(i, c[i], b[i]) for i in range(len(c)) if c[i] != b[i]]
print('different lines: %d' % len(diff))
for i, cur, bef in diff:
    print('  L%-5d ...%s' % (i + 1, cur.strip()[:110]))
    print('        -> %s' % bef.strip()[:110])
    # 断言：这一行确实是在两张表里、且是被换成「我们的卡 id」的那些行
    assert ('new NodeSpec(' in cur or 'new RewardSpec(' in cur), 'unexpected changed line %d' % (i + 1)

if '--write' not in sys.argv:
    print('DRY-RUN，未写盘')
    sys.exit(0)

out = '\n'.join(b)
assert out.count('\r\n') == 0
io.open(P, 'wb').write(out.encode('utf-8'))
print('WROTE %s' % P)
