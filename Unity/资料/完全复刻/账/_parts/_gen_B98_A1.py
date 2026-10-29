# -*- coding: utf-8 -*-
# B98_A1 的账补丁生成器：以主表当前第 681 行为基线，只改「状态 / 落点 / 差异 / 轮次 / 备注」五列。
import io

MAIN = r'd:/4/Unity/资料/完全复刻/账/日常_奖励.tsv'
OUT = r'd:/4/Unity/资料/完全复刻/账/_parts/B98_A1.tsv'

b = io.open(MAIN, 'rb').read()
assert b.count(b'\r') == b.count(b'\r\n'), '主表里有裸 CR ⇒ 行号会算错，停手'
text = b.decode('utf-8-sig')
rows = text.split('\n')
hdr = rows[0].split('\t')
row = rows[680].split('\t')
assert len(hdr) == 11, len(hdr)
assert len(row) == 11, len(row)
assert row[1] == 'RewardService' and row[2].startswith('CollectSingleReward('), (row[1], row[2])
assert row[5] == 'B', row[5]

LAND = (' ｜[B98_A1 2026-10-29 复核] ⑤/⑥ 的**三条出口**已接齐：货币支 `Shell/Inventory.cs:2083`'
        '（同一行追加 `HandleOnCollectEquivalent`，零行数变化）· ④ 支后紧接 `:2092` · **末尾统一跳** `:2102`；'
        '等价物 = `Shell/Inventory.cs:2152 HandleOnCollectEquivalent(string)`'
        '（id 命中 `ProfileData.Borders` 且 `aa != 0` ⇒ `ProfileData.SetEquippedBorderById`，`:1528`）')

DIFF = (' ｜[B98_A1 2026-10-29 **B→A**] ⑤/⑥ 收官。**A 的两条边界（不是没做，是已定案）**：'
        '② `NetworkingPeer.OnMessage` 上报 = **P3**（PlayFab，无后端 ⇒ 出声一次、不上报）；'
        '① 限时到期：原版比**服务器时刻** `EphemeralEndTime <= ServerDateTime`，我们 `RewardInfo` 只有**时长**'
        '（`ExpiredByDuration` 恒 false）—— `RewardInfo` 的 `🔶 自己设计` 已写明「本机没有服务器时刻」，'
        '且今天 89 条奖励 `EphemeralMs` 全 0 ⇒ 两侧行为**逐条一致**；换一份带时长的数据才会分叉，届时补起始时刻即可。'
        '③ 单例为 null 时原版 NRE，我们取不到就不写、不抛（与 ④ 支同一口径）。'
        '断言 = `Editor/B98A1_Assert.cs`（本波独立复验：D0 批处理前提硬断言 · A id 逐字相等 · B `aa==0` 灭自证 · C 非边框不动且出声）')

NOTE = (' ｜[B98_A1 2026-10-29 波 5a] 现读复核：`Shell/Inventory.cs` 与 HEAD **无差异**'
        '（⑥ 由同会话 `B97_W40` 已落地并提交），`B97W40_Assert.cs` 已由调度台接进 `Editor/ShellScene.cs:10639`；'
        '本波**不重复改实现**，只补**独立复验**断言 `Editor/B98A1_Assert.cs`（`Pass/Fail` + `RunCount`，'
        '独有 = D0 批处理前提硬断言、A5 id 必须逐字相等）＋ 本补丁把该行 B→A。`typecheck` 0/0。')

new = row[:5] + [
    'A',
    row[6] + LAND,
    row[7] + DIFF,
    row[8],
    row[9] + '·B98_A1',
    row[10] + NOTE,
]
assert len(new) == 11, len(new)
assert all('\t' not in c and '\n' not in c and '\r' not in c for c in new), '列里有控制字符'

payload = '\t'.join(hdr) + '\n' + '\t'.join(new) + '\n'
io.open(OUT, 'wb').write(payload.encode('utf-8'))

# 回读自证：11 列、状态 A、方法签名带括号
back = io.open(OUT, 'rb').read().decode('utf-8').split('\n')
assert len(back[0].split('\t')) == 11 and len(back[1].split('\t')) == 11
assert back[1].split('\t')[5] == 'A'
assert back[1].split('\t')[2] == row[2]
print('OK cols', len(back[1].split('\t')), 'status', back[1].split('\t')[5], 'len', len(payload))
