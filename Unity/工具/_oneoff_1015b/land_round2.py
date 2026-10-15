# -*- coding: utf-8 -*-
"""落这一轮（7 条拍板 + G1/G2/G3 的产出）的账：
① `A2020`/`A2022`/`A2043` 标结（用户拍板结论）
② 新增 `A2061`–`A2064`（SOR24 空串回归 + 三条顺手发现）"""
import io

P = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"
s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")


def append_row(prefix, frag, tag):
    hit = [i for i, L in enumerate(lines) if L.startswith(prefix)]
    assert len(hit) == 1, "%s：命中 %d" % (tag, len(hit))
    i = hit[0]
    L = lines[i].rstrip()
    assert L.endswith("|"), "%s：该行不以 | 收尾" % tag
    lines[i] = L[:-1].rstrip() + frag


def insert_after(prefix, rows, tag):
    hit = [i for i, L in enumerate(lines) if L.startswith(prefix)]
    assert len(hit) == 1, "%s：命中 %d" % (tag, len(hit))
    for r in rows:
        assert r.count("|") == 3, "%s：新行竖线 %d" % (tag, r.count("|"))
    lines[hit[0] + 1:hit[0] + 1] = rows


# ── ① 三条标结（用户 2026-10-15 拍板）────────────────────────────
append_row("| **`A2020`** |",
           " ➕ **2026-10-15 用户拍板（路 B）：保留那句 + 注明「非卡面原文」** —— 已落 `cardface_fixes.json` 的 "
           "`_manual_desc_note`（键 = `Ephrael Stern` 那条的说明字典，形状与 `AM` 那批一致）。"
           "⛔ **不许只删文本不管 op**（那句给 `deal` op 定着目标规格）。判据 → `资料/普查产出_第三十二会话/G1_SOR24撤改名.md` |",
           "A2020")

append_row("| **`A2022`** |",
           " ➕ **2026-10-15 用户拍板（路 B）：撤那次改名** —— `SOR24` 已撤回 `Simulacrum Imperialis`，"
           "**6 处同步全做完**（`card_stats.name` · `zh_cards` 键 · `cardface_fixes` 的 `desc`/`descZh`/`subtype`/`_manual_subtype`/`_manual_subtype_note`）· "
           "`card_ids.json:664` 本来就是新值 ⇒ 改名后**反而对上**。⛔ 三条「错字」照旧**不改**（照抄真值快照）。"
           "⚠️ 连带同族新账 = `A2061`。判据 → `G1_SOR24撤改名.md` |",
           "A2022")

append_row("| **`A2043`** |",
           " ➕ **2026-10-15 用户拍板（路 B）：⛔ 不改源表 + 补守卫** —— 守卫已加（`gen_cards_engine.py` 的 `OCR_ARMOR_FAMILY`），"
           "**三格一起比集合**（阵营/卡名/家族判据命中位）· **进 `_bad`**（rc=1、一个字节不写）· 经两次真实注入演示有效。"
           "⚠️ 实测订正：家族判据「`ranged_attack=null 且 armor≠null`」**只圈得出 5 张**（第 6 张 `Simulacrum …` 的 `ranged_attack` 已是 4）"
           "⇒ 守卫把这一事实**显式编进第三格**。判据 → `资料/普查产出_第三十二会话/G2_文案与守卫.md` |",
           "A2043")

# ── ② 新增 A2061–A2064 ───────────────────────────────────────
ROWS = [
"| **`A2061`** | ✅ **2026-10-15 已修**：**撤改名带出的「稀有度静默掉成空串」** —— 稀有度那层**按卡名**查宝石表（`资料/卡牌数据表/卡牌宝石稀有度_0824.md`，**键是 PnP 卡面写法**）⇒ `SOR24` 改名后 join 落空 ⇒ `rarity` 回落成 `\"\"`（**影响卡面稀有度宝石 / 稀有度筛选 / 构筑 `CopyLimit`**；卡框档位不受影响）。修法 = **修在生成器那一层**（`RARITY_BY_FACTION` 加 `(\"Sororitas\",\"Simulacrum Imperialis\"): \"epic\"`，**与 B20 那四条同形**）+ `card_stats.json:22388` 那格**回 `null`**（源表 = OCR 原文、修正在表里）。**并补了守卫（两条谓词，都进 `_bad`）**：① 症状层「`rarity` 空串、但按 `ocrName` 能查到」② 权威层「与游戏侧真值 `cardRarity` 不一致」；经**三次真实注入**演示有效。判据 → `资料/普查产出_第三十二会话/G3_rarity层与守卫.md` |",
"| **`A2062`** | 🔴 **原版快照的 `cardRarity` 是全池覆盖、却一直没被用的【权威稀有度源】** —— `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` 的 `cardRarity`（`1..5 = common/rare/epic/legendary/special`）按 `uniqueId` join 我们卡池：**1005/1005 一致、0 例外**（4 张快照无值跳过）。⇒ **换成它当第 1 层，一个值都不会变**，但能从根上消掉**「PnP 写法 vs 我们的卡名」这一整类 join**（`A2061` 本笔 + B20 那 4 条 + `RARITY_UNLISTED` 那 21 条**全属这一类**）。⚠️ `RARITY_BY_FACTION` 里那 13 条「宝石表读错」的条目与 `cardRarity` 也**全一致** ⇒ 那不是「读错、我们改对」，是**同一轮视觉扫描的读数偏差**。出处 → `G3_rarity层与守卫.md` 顺手发现 #2/#3 |",
"| **`A2063`** | ⚠️ **`资料/资源使用手册.md:249` 写错了判据层**：它写「稀有度 → `数据/游戏数据/gems_rarity.json`（1118/1118 映射）」，而生成器**实际读的是** `资料/卡牌数据表/卡牌宝石稀有度_0824.md`（`gen_cards_engine.py:33 RARITY_SRC`）。⚠️ 顺带查明 **`gems_rarity.json` 在 `d:/4` 里是【死文件】**（无任何 `.py`/`.cs` 读它；旧 Godot 仓 `d:/warpforge/autoload/game_data.gd:210` 有读者 ⇒ **搬迁遗留**）。⇒ 按铁律 5 订正手册那句，并决定那个 json 留还是删。出处 → `G3_rarity层与守卫.md` 顺手发现 #1/#4 |",
"| **`A2064`** | ⚠️ **`RARITY_UNLISTED` 那张表**：① 顶句「三份 `0824` 表没收录的 **9 张**」现读**偏窄**（表里 **21** 条）② 它**没有孤立键守卫**（本文件只给 `cardface_fixes`/`STAT_FIXES`/`OCR_ARMOR_FAMILY` 立了；`R6` 当天建议过）。⚠️ 现读 21/23 个键全部命中池内卡（今天必绿）⇒ 加不加要裁。出处 → `G3_rarity层与守卫.md` §六·2/§六·3 |",
]
insert_after("| **`A2049`** |", ROWS, "A2061-2064")

io.open(P, "wb").write("\n".join(lines).encode("utf-8"))
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
