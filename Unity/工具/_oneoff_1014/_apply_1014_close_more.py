# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 核销：① A1943 / A1966 整行搬进收口档 ② A1964 改成「已办并已验证」。
判据：A1943 = play check rc=0（写手 2 报告）· A1964 = 自检实测（_tmp_view/arena3/run_battle_1014.log）· A1966 = 判据恒 0（本会话现核）。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_close_more.py
"""
import io

SRC = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
ARC = r'D:\4\Unity\资料\历史\A表已收口_第三十会话.md'

MOVE = ['A1943', 'A1966']          # 整行搬走（已办/已结案）

NOTE_APPEND = (
    " ／ 🆕 **第三十会话（2026-10-14）再收 2 条**：`A1943` `A1966` —— `A1943` 已办（`play check` **rc=0**、①-c 净差=0 **175 → 154**，"
    "残余归 `A1965`/`A1973`）· `A1966` 已结案（原版逐卡 `initial*Modifier` 1195 张全为 0 ⇒ 不是复刻缺口）。"
)

ARC_ADD = """> · 🔴 **2026-10-14（第三十会话）补记 —— 搬进来的两笔（`A1943` / `A1966`）当天都已办/已结案**：
>   · **`A1943` = 已办**：夹具起始派系资源补齐（`Faith 12 / SpiritStones 5 / QuestPoints 0`，**量具干预、⛔ 非原版值**，理由与依据全写在
>     `工具/ruleprobe/PlayProbe.cs:60-113` 的注释里）→ 基线整体重生成 → `PinnedPlayMd5` 重钉为 **`76ced8a686a9c316624c37415a91d58c`**。
>     验收：`play check` **rc=0**、①-c 净差=0 **175 → 154**（那 21 张全转成 `net > 0`）、`check` rc=0 / 哨兵 0 失败。
>     🔴 **残余不在这笔**：剩 **154 张「电池抬不到那个事件」** = `A1965` 的真实工作面；**另外重钉吞掉了 ~1118 行已存在的漂移** = 新账 **`A1973`**。
>   · **`A1966` = 已结案（不是复刻缺口）**：原版卡 SO **全字段真值导出** `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
>     （1,195 张 · 2025-07-30 快照 · 就是原版 `allcards` 包）里 `initialHandModifier` / `initialManaModifier` / `initialPsychicModifier` /
>     `initialPlasmaModifier` / **`initialSpiritStoneModifier`** **1195 张全部为 0**，含**全部 60 个督军**（`cardType == 10`）⇒ 我们与之一致。
>     ⚠️ **立项时的错因留痕**：只搜了 `assets_full` + `d:/4` 数据表、**没搜 `d:/2/远端缓存_旧profile_1013/导出/`** ⇒ 误判成「判据在远端」。

"""

A1964_OLD = "| 🆕 **`A1964`** | ✅ **判据已查清（2026-10-14 只读代理 + 主对话现核）—— 可以放心加**："
A1964_NEW = (
    "| ✅ **`A1964`** | ✅ **已办并已验证（2026-10-14 第三十会话）** —— 断言已落进 `CardPresentation/Editor/BattleScene.cs` 新起的 **`20-b` 节**"
    "（纯新增 **156 行 / 23 条 `Check`**；改前 23283−156 = 23127 行、**CRLF 未翻**；`typecheck.sh` 运行时 0 / 编辑器 0）。"
    "🔴 **自检实测（`Unity/_tmp_view/arena3/run_battle_1014.log`）**：(a) `GSC65` **摆 4 张** ✓ 且那 4 张**就是**域里那 4 张（各一张、不多不少）✓ "
    "`IsPicked(0)` ✓ 面板关 ✓ 引擎真按面板选了一张 `Sabotage` ✓；(b) **灭自证配对** `GSC50` **摆 3 张** ✓ 且从**同一个 4 张域**里抽 ✓。"
    "**判据查证段（保留）**："
)


def main():
    # ---- ① 正本：A1964 改判 ----
    b = io.open(SRC, 'rb').read()
    assert b.count(b'\r\n') == 0
    t = b.decode('utf-8')
    assert t.count(A1964_OLD) == 1
    t = t.replace(A1964_OLD, A1964_NEW)

    # ---- ② 正本：搬走 A1943 / A1966 ----
    lines = t.split('\n')
    picked = []
    for cid in MOVE:
        marker = '**`%s`**' % cid
        hits = [i for i, l in enumerate(lines) if l.startswith('| ') and marker in l]
        assert len(hits) == 1, '%s hits=%d' % (cid, len(hits))
        i = hits[0]
        assert lines[i].rstrip().endswith(' |'), lines[i][:70]
        picked.append((i, lines[i]))
    picked.sort()
    rows = '\n'.join(l for (_, l) in picked)

    drop = set(i for (i, _) in picked)
    out = [l for j, l in enumerate(lines) if j not in drop]

    anchor = '已收口（本会话搬走）'
    hits = [i for i, l in enumerate(out) if anchor in l and l.startswith('| ')]
    assert len(hits) == 1, 'note row hits=%d' % len(hits)
    i = hits[0]
    assert out[i].rstrip().endswith(' |')
    out[i] = out[i].rstrip()[:-1].rstrip() + NOTE_APPEND + ' |'

    t2 = '\n'.join(out)
    assert '\x00' not in t2 and '\r\n' not in t2
    io.open(SRC, 'wb').write(t2.encode('utf-8'))
    print('SRC  %d -> %d lines' % (len(lines), len(out)))

    # ---- ③ 收口档：补记 + 追加两行 ----
    a = io.open(ARC, 'rb').read()
    assert a.count(b'\r\n') == 0
    at = a.decode('utf-8')
    hdr9 = '> ⬇️ 下面 **9 行全部来自**'
    hdr11 = '> ⬇️ 下面 **11 行全部来自**'
    assert at.count(hdr9) == 1
    at = at.replace(hdr9, ARC_ADD + hdr11)
    lines2 = at.split('\n')
    sep = [i for i, l in enumerate(lines2) if l.strip() == '|---|']
    assert len(sep) == 1, '|---| hits=%d' % len(sep)
    j = sep[0] + 1
    assert lines2[j].startswith('| 🆕'), lines2[j][:60]
    lines2[j:j] = [rows]
    at2 = '\n'.join(lines2)
    assert '\x00' not in at2 and '\r\n' not in at2
    io.open(ARC, 'wb').write(at2.encode('utf-8'))
    print('ARC  %d -> %d lines' % (len(a.split(b'\n')), len(lines2)))


if __name__ == '__main__':
    main()
