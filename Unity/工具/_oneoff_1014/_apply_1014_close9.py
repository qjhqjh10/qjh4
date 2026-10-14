# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 把 9 笔【现读已闭】的账整行搬进收口档。
判据来源：只读复核代理（ab668173…）逐笔现核 + 主对话现核。
做法（本仓惯例）：原文一字未改地搬走；正本里删掉那 9 行；在「已收口（本会话搬走）」那一行补一句指针。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_close9.py
"""
import io

SRC = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
DST = r'D:\4\Unity\资料\历史\A表已收口_第三十会话.md'

IDS = ['A1461', 'A1599', 'A1689', 'A1693', 'A1695', 'A1762', 'A1791', 'A1806', 'A1807']

HEADER = """# A 表已收口 —— 第三十会话（2026-10-14）

> 🔴 **本文件 = 第三十会话【整行搬出】的已收口账**（原文**一字未改**，只换住处）。
> ⛔ **判据原文仍在各自的报告里**（见每行末尾的指针）；要查「当时怎么收口的」按 `A` 编号在本文件里找。
> 📌 本会话全部报告 → `资料/普查产出_第三十会话/`。
> ⬇️ 下面 **9 行全部来自** `Unity/资料/任务_卡组卡牌战斗.md`（§一～§三 那几张表），逐行搬入。
> 🔴 **2026-10-14（第三十会话）补记 —— 搬出那一刻逐行现核过（铁律 5）**：
> · **`A1461` / `A1599` / `A1689` / `A1693`（4 行）：已办、无残余。** 代码锚点逐条现读过，**行号有漂、函数名不变**：
>   `EffectText.TryWhenSuffix` 定义现读 **`Core/EffectText.cs:7021`**（账文写 `:6823`）· `PlayProbe.IsPaidSegment` 现读 **`工具/ruleprobe/PlayProbe.cs:1149`**、
>   `BucketSelfCheck` 现读 **`:1622`**（账文写 `:729`/`:1126`）· `A1689` 的断言 `TestS23ParseCostIconMarker` 现读
>   **`Assets/RuleEngine/Editor/RuleEngineTest_S23_Parse.cs:395`**（注册 `RuleEngineTest.cs:717` · `RuleEngineTest_Diag.cs:459`）·
>   `A1693` 判据 = `资料/普查产出_第二十五会话/R1_引擎判据收集.md` §1。两条连带账 **`A1686`（`A1461` 顺手立）与 `A1619`（`A1599` 的尾巴）
>   都已在 `历史/A表已收口_第二十三会话.md` 闭掉**（`:14` / `:84`）。
> · **`A1695`：它不是缺陷、是纪律** —— 「基线保留」是裁定，要留下的是**做法**，已另落到
>   **`资料/已知的坑.md:6128-6135`**（同一实据 78/89 行、同一句话）⇒ 本行搬走后**判据改指那一节**。
> · **`A1762`：只完成了【卡名那半】。** 复算：官方 `Card_Name/*` 命中 **973/1126**（id 直查 836 + 英文名桥 137），
>   其中 **973/973 与 `cards_engine.json` 的 `nameZh` 逐字相同、不同 0**。🔴 **描述那半不在这笔** —— `descZh` 仍是自译，
>   归**另开的 `A1763`（仍开着）** ⇒ ⛔ **别把本行读成「本地化全换完了」**。余 **153 张**官方 2025-07-30 快照里**根本没有**
>   （前缀 `SW`68 · `UM`37 · `DA`13 …）⇒ 按**铁律 11 例外①**（判据为空）保持自译；**要等更晚的 `allcards`** ⇒ 指针留在
>   `项目任务.md` §29·d 的 `A897`/`A970` 那一行，⛔ 不另立账。
> · **`A1791` + `A1807` 并成一条**（本来就是同一件事的两面）：根因 = **键名不同源**（官方那批键里 **124 个名 / 147 个描述**是 32 位 GUID）
>   ⇒ 做法 = **先按 id、查不到再经 `Card_Name` 的英文名桥**，已在 `工具/gen_cards_engine.py` 落地（`official_name()`，`:857`）。🔴 **数字订正（留痕）**：
>   账文与报告写的 `Card_Description` **1101 → 现读 1098**；连带 **835→832（按 id 直命中）· 972→969（两级）· 154→157（快照没有）**
>   —— 三个数随「键总数 −3」同步位移。⚠️ **那 −3 的来路没查到**（`官方本地化_en+zh-CN.json` 现读 mtime **2026-10-14 06:57**，
>   晚于第二十七会话 ⇒ 那份表被重写过，但**没找到重写记录**）⇒ **只按数字位移如实写，没敢编原因**。
> · **`A1806`：结论已交付。** 现读复核：`Card_Name/*` **1065 条全部是 `cardId → 英文名`**；全表 **GUID 形键 895 条**
>   （`Titles`368 · `Cosmetic_Lore`149 · `Card_Description`147 · `Card_Name`124 · `Warlord_Lore`106 · `Chat_Messages`1）—— 与账文**逐格一致**。
>   ⚠️ 消费方 **`A970` 仍挂起**（`项目任务.md` §29·d 等远端）⇒ 指针留那一边。

| 编号（原行原文） |
|---|
"""

NOTE_APPEND = (
    " ／ 🆕 **第三十会话（2026-10-14）再收 9 条**：`A1461` `A1599` `A1689` `A1693` `A1695` `A1762` `A1791` `A1806` `A1807` "
    "—— **逐条现核过**（原文 → `资料/历史/A表已收口_第三十会话.md`）。🔴 **`A1762` 只收【卡名那半】**，`descZh` 那半属**仍开着的 `A1763`**。"
)


def main():
    b = io.open(SRC, 'rb').read()
    assert b.count(b'\r\n') == 0, 'src must be LF'
    lines = b.decode('utf-8').split('\n')

    picked = []
    for cid in IDS:
        marker = '**`%s`**' % cid
        hits = [i for i, l in enumerate(lines) if l.startswith('| ') and marker in l]
        assert len(hits) == 1, '%s hits=%d' % (cid, len(hits))
        i = hits[0]
        assert lines[i].rstrip().endswith(' |'), lines[i][:70]
        picked.append((i, lines[i], cid))

    picked.sort()
    # 照既有收口档的做法：整行【原文原样】塞进去（不额外包一层 `| `，否则单元格被内部竖线切碎）
    body = '\n'.join(l for (_, l, _) in picked)

    io.open(DST, 'wb').write((HEADER + body + '\n').encode('utf-8'))

    drop = set(i for (i, _, _) in picked)
    out = [l for j, l in enumerate(lines) if j not in drop]
    t = '\n'.join(out)

    anchor = '已收口（本会话搬走）'
    hits = [i for i, l in enumerate(out) if anchor in l and l.startswith('| ')]
    assert len(hits) == 1, 'note row hits=%d' % len(hits)
    i = hits[0]
    assert out[i].rstrip().endswith(' |'), out[i][:70]
    out[i] = out[i].rstrip()[:-1].rstrip() + NOTE_APPEND + ' |'
    t = '\n'.join(out)
    assert '\x00' not in t and '\r\n' not in t
    io.open(SRC, 'wb').write(t.encode('utf-8'))
    print('OK  archived %d rows;  src %d -> %d lines' % (len(picked), len(lines), len(out)))


if __name__ == '__main__':
    main()
