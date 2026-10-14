# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 追加查证落账：
 ① A1995 追加（ASH83 真缺陷成立 + 294/266 已查清）
 ② A1941 追加（那 6 张 desc 为空的卡里 5 张已查实原因）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_ash83.py
"""
import io

MAIN = r'D:\4\项目任务.md'
TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'

A1995_SEG = (
    " ✅ **2026-10-14 判据补全（追加查证）**：\n"
    "① **`ASH83` 的 `A1995①` = 真缺陷，成立** —— 判据链：官方 EN `Card_Description/ASH83` = `[[waystone]]. ((Spirit_x,1)) …`，"
    "而我们 `desc` **整条没有 `Waystone`**；**同族两张 PnP 亲读**证明了 `[[waystone]]` **确实是印出来的**"
    "（`Aeldari/3部队/Warpforge_7_Guardian-Defender.png` 卡面 = `〔菱形图标〕Waystone.` ↔ 官方 `[[waystone]].` **逐位置对应**）。\n"
    "🔴 **关键鉴别点**：官方 EN 带 `[[waystone]]` 的共 **25 张**，`desc` 里带 `Waystone` 的 **12** / 缺 **13**；"
    "但缺的那 13 张里 **12 张的 `keywords` 数组里有 `Waystone`** ⇒ 按 `CardText.KeywordSegment`（把「`desc` 没声明、但 `keywords` 里有」的关键词补在卡面最前）"
    "**卡面渲染仍会补出来** ⇒ 那 12 张**画面上未必看得出问题**；**`ASH83` 是唯一例外（它的 `keywords = []`）** ⇒ **没有任何一层会把 `Waystone.` 补出来**。\n"
    "⚠️ **两条要如实记**：(a) **`ASH83` 的 PnP 成品图【本地没有】**（`find -iname \"*Autarch*\"` 只有 `ASH31 Winged Autarch`）"
    "⇒ **无法走「和成品图并排比」那一步验收**（铁律 10 第 6 条）；(b) 那 **12 张**「依赖 `keywords` 兜底」的**与 `ASH83` 不同档** "
    "⇒ 本笔只把 `ASH83` 记成**真缺陷**、另 12 张记成「**画面对、但 `desc` 不完整**」。\n"
    "② **`294 vs 266` 已查清，且【没有一张是「我们该有而没有」】**：**294 = 289（不在 2022 快照里 = 快照后新增）＋ 5（在快照里、官方表却没键）**。"
    "那 **5** 张 = `ASH61` / `BL2` / `BL26` / `TL61` / `UM6` —— 现读 `cards_1195` 里它们 **`itemDescriptionKey = ''`、`itemNameKey = None`** "
    "⇒ **原版数据源自己就没给描述键**（而同 5 张的 `Card_Name/<id>` 键在官方表里**都存在**）⇒ 按**铁律 11 例外①** 处置，**不再挂「待查」**"
    "（⇒ 另见 `A1941` 那一行）。⚠️ 旁证：那 **289** 张里 `Card_Name/<id>` 键在的 = **0 张** ⇒ 官方表里**连名字都没有** ⇒ 确认是同一批快照之后新增的卡。\n"
    "🔴 **`266` 与 `294`【不同源、⛔ 别混用】**：`Card_Description/*` 共 **1098** = **951 非 hex 键 ＋ 147 个 32 位 hex 键**；"
    "落进我们池 id 的 = **832** ⇒ `1098 − 832 = **266**` ⇒ **266 = 「官方表有键、但键名不是我们池 id」**（147 hex + 119 非 hex 不在池），"
    "而官方表 `_note` 那句「卡池那 266 张没有真值的卡」**是另一个方向**的口径。"
)

A1941_SEG = (
    " ✅ **2026-10-14 那半【已查实 5/6】**（判据 = 现读 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` 的**原始字段**，"
    "只读查证 `A1995` 那一趟顺手做掉）：**`ASH61` / `BL2` / `BL26` / `TL61` / `UM6` 这 5 张的 `itemDescriptionKey = ''`、`itemNameKey = None`** "
    "⇒ **原版数据源自己就没给这 5 张描述**（而同 5 张的 `Card_Name/<id>` 键在官方本地化表里**都存在**）"
    "⇒ **不是「我们的数据缺口」，是【原版侧本身就没有】** ⇒ 按**铁律 11 例外①** 处置，⛔ 别再当「待查」。"
    "⚠️ **只剩 `GOF8` 没查**（它不在那 5 张里）⇒ **那 1 张仍按本笔原状开着**。"
)


def append_seg(path, head_prefix, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith(head_prefix)]
    assert len(hits) == 1, (path, head_prefix, len(hits))
    j = hits[0]
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines)
    lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s (尾行 L%d)' % (head_prefix, j + 1))


def main():
    append_seg(MAIN, '| 🆕 **`A1995`**', A1995_SEG)
    append_seg(TASKS, '| 🆕 **`A1941`**', A1941_SEG)


if __name__ == '__main__':
    main()
