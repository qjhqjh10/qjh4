# -*- coding: utf-8 -*-
"""用户 2026-10-15 拍板 → 落正本：
① `A2040` 以 `任务 §一` 那处为准（把 §29·b 那句改成指向它）
② `A2041` 在 §29·b 头部加一行指针（四类编号在本表 0 命中是正常的）
③ `A2019` 已裁完 ⇒ 从 §29·c 移出，落进 `资料/历史/A表已收口_第三十二会话.md`"""
import io

PROJ = r"d:/4/项目任务.md"
CLOSED = r"d:/4/Unity/资料/历史/A表已收口_第三十二会话.md"

s = io.open(PROJ, encoding="utf-8", newline="").read()
lines = s.split("\n")


def find_one(sub, tag):
    hit = [i for i, L in enumerate(lines) if sub in L]
    assert len(hit) == 1, "%s：命中 %d" % (tag, len(hit))
    return hit[0]


# ── ① A2040：§29·b 那句「列进要做那一族」改成指向 §一 的拆法 ─────────
i = find_one("其中最要紧的一族是**「我们自译/兜底 → 换原版真值」**", "A2040")
old = lines[i]
assert old.rstrip().endswith("|") is False or True
add = (" ⚠️ **2026-10-15 用户拍板（`A2040`）**：这一族**以 `资料/任务_卡组卡牌战斗.md` §一 页脚那句为准** —— "
       "**`A1762` 只收【卡名那半】**，`descZh` 那半属 `A1763`（那半本会话已确认落地：官方 `descZh` 已灌进卡池）。"
       "⛔ 别再把 `A1762` 整条读成「还没做」或「等远端」。判据 → `资料/普查产出_第三十二会话/R1_B2正本锚点.md` B2-17。")
lines[i] = old + add

# ── ② A2041：§29·b 头部加一行指针 ────────────────────────────────
j = find_one("🔑 **四类已整行搬走**", "A2041")
lines.insert(j + 1,
             "> 🔑 **顺带一条**（2026-10-15 加，`A2041`）：**凡是编号带 `A1xxx`/`A2xxx` 的【引擎 / 卡组编辑 / 卡牌 / 界面·功能复刻】四类账，"
             "在本表里 `grep` 不到是【正常的】** —— 它们的自身行在 `资料/任务_卡组卡牌战斗.md`；"
             "⚠️ 本表里偶尔出现的它们，**只是别处的顺带提及**（例：`A1590③` 在 `任务` 是 `:35` · `A1522` 是 `:480` · `A1226` 是 `:416`）⇒ "
             "⛔ 别当成 §29·b 自己的账去收。")

# ── ③ A2019：从 §29·c 移出 ─────────────────────────────────────
k = find_one("| **`A2019`** |", "A2019")
row = lines[k]
del lines[k]

io.open(PROJ, "wb").write("\n".join(lines).encode("utf-8"))

io.open(CLOSED, "wb").write((
    "# A 表已收口 · 第三十二会话（2026-10-15）\n\n"
    "> 本件收**这一轮用户拍板后销掉的账**。原文照抄，⛔ 不重写。\n\n"
    "## 一、用户 2026-10-15 拍板 ⇒ 当场销账\n\n"
    "| 编号 | 拍板结论（照抄落盘时那行） |\n|---|---|\n"
    "| `A2019` | 官方 **`_new` 键那一代**要不要让 `descZh` 去取 —— **用户 2026-10-15 拍板：要**。\n"
    "落法 = `load_official_desc()` 让 `_new` 新代**覆盖**旧代（`工具/gen_cards_engine.py`）；"
    "判据 = **逐张开 PnP 成品卡图核过**（`TL66 Neurogaunt` 卡面印 `Tide 2`、`TL44 Hardened Biology` 印 `+1 Health`，都是新代）。"
    "当日池里 5 张变动、逐条列出。判据 → `资料/普查产出_第三十二会话/R3_卡牌线施工单.md` |\n"
    "\n> 原行（从 `项目任务.md` §29·c 移出）：\n>\n> " + row.strip() + "\n"
).encode("utf-8"))

b = io.open(PROJ, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
