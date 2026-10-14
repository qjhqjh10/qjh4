# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 改判 A1966（立项当天结案）+ 修 A1943 里那句被推翻的话。
判据：原版卡 SO 全字段真值导出里五个 initial*Modifier 全部为 0（含 60 个督军）。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a1966.py
"""
import io

P = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'

OLD_A1943_FRAG = "**原版开局 Faith = 0 · QuestPoints = 0**，**三格里只有灵魂石是真缺口**（另见新账 `A1966`）"
NEW_A1943_FRAG = (
    "**原版开局 Faith = 0 · QuestPoints = 0**，**三格开局原版都是 0**"
    "（灵魂石那一路虽由督军 `RawCardScript` 传入，但那份**全字段真值导出**里 1195 张**全部为 0**、含 60 个督军 ⇒ 见 `A1966`）"
)

A1966 = (
    "| ✅ **`A1966`** | ✅ **2026-10-14 当场查清 ⇒ 本账【结案】—— 原版逐卡 `initial*Modifier` 全部为 0，我们与之一致**"
    "（本账**当天立项、当天结案**：立项时只搜了 `assets_full` + `d:/4` 数据表 ⇒ 误判成「判据在远端 ⇒ 挂起」，**现核推翻**）。<br>"
    "**判据（现核）**：原版卡 SO **全字段真值导出** `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`"
    "（1,195 张 · **2025-07-30 快照** · 来源 = 原版 `allcards` 包，见 `资料/卡牌id_换真值_落地记录.md:20-22`）里，"
    "`initialHandModifier` / `initialManaModifier` / `initialPsychicModifier` / `initialPlasmaModifier` / **`initialSpiritStoneModifier`** "
    "五个字段 **1195 张全部为 0**，含**全部 60 个督军**（`cardType == 10`；抽核 `SAU37 Orikan the Diviner` / `SOR3 Morvenn Vahl` / "
    "`GOF27 Boss Zagstruk` 皆 0）。<br>**旁证**：开局能量**根本不走这个字段** —— 我们 `RuleEngine/Core/GameplayVariables.cs:181-182` 写着"
    "「字段语义照原版 `BattleManager__SetupInitialMana.c:20,28,46`：先手取 `startingMana`、后手取 `startingManaSecond`」"
    "⇒ 开局资源来自**全局变量**，逐卡的 `initial*Modifier` 只是**额外的督军增量**，而那个增量在原版数据里**恒 0**。<br>"
    "🔴 **错因（留痕 · 铁律 5）**：立项时只搜了 `d:/2/新解包资源/assets_full`（66,543 份 MonoBehaviour dump，字段名 0 命中）与 `d:/4` 数据表，"
    "**没搜 `d:/2/远端缓存_旧profile_1013/导出/`** ⇒ 正是本仓那条老坑「**报『没有』之前先打出『搜过哪几个包』**」。<br>"
    "⏭ **只留一条尾巴**：该快照之后新增的 **266 张卡**没有真值（同 `资料/卡牌id_换真值_落地记录.md` §五·1）⇒ "
    "**若将来拿到更晚的 `allcards`，这五个字段要按新数据再复核一遍**（届时本账可重开）。<br>"
    "⚠️ 本条**与 `A1943` 的夹具干预不是一回事**（那条是量具、本条是产品），⛔ 别合并。 |"
)


def main():
    b = io.open(P, 'rb').read()
    assert b.count(b'\r\n') == 0, 'file must stay LF'
    text = b.decode('utf-8')
    assert text.count(OLD_A1943_FRAG) == 1, 'A1943 anchor hits=%d' % text.count(OLD_A1943_FRAG)
    text = text.replace(OLD_A1943_FRAG, NEW_A1943_FRAG)

    lines = text.split('\n')
    hits = [i for i, l in enumerate(lines) if '**`A1966`**' in l]
    assert len(hits) == 1, 'A1966 hits=%d' % len(hits)
    i = hits[0]
    assert lines[i].startswith('| ') and lines[i].rstrip().endswith(' |'), lines[i][:80]
    lines[i] = A1966

    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(P, 'wb').write(out.encode('utf-8'))
    print('OK bytes %d -> %d' % (len(b), len(out.encode('utf-8'))))


if __name__ == '__main__':
    main()
