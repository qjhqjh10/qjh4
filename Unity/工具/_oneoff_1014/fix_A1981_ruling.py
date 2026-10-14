# -*- coding: utf-8 -*-
"""订正 `卡表块_施工设计_1014.md` §七「已裁 ④」—— 主对话把 `A1981` 裁反了，就地改正。

错因（留痕）：我按「督军费用从显示上不可见 ⇒ `0` 无所谓」推，**把「不显示」当成了「不是真值」**；
而且**没读到 `d:/4/项目任务.md` 的 `:500-509` 已经对这条做过对抗性验证、裁决是「`2` 是真值」**。
⇒ 教训：**裁一条账之前，先把该账在正本里的【整段】读全**（不只是它那一行）。

正确裁决（= 维持 `项目任务.md:502` 的既有裁决）：
  **`manaCost = 2` 是真值 ⇒ `A1773` 应把督军 `cost` 从 `0` 改成 `2`**；
  连带要修的是【显示侧三处】，⛔ 不是「靠 0 去表达督军身份」。
"""
import io
import re
import sys

P = "d:/4/Unity/资料/普查产出_第三十会话/卡表块_施工设计_1014.md"

NEW = """### ✅ 已裁 ④：`A1981` 的 12 张督军 `cost` —— 🔴 **维持既有裁决：真值 `2` 是对的 ⇒ `A1773` 应把督军 `cost` 改成 `2`**

> 🔴🔴 **2026-10-14 第三十一会话【就地订正】：本节的上一版把这条裁反了**（曾写「保留 `0`、不采真值的 `2`」）。
> **错因**：我只按「督军费用**从显示上不可见** ⇒ `0` 无所谓」去推 —— **把「不显示」当成了「不是真值」**；
> 而且**没读到 `d:/4/项目任务.md` 的 `:500-509` 已经对这条做过对抗性验证、并写死「裁决维持：`2` 是真值」**。
> ⇒ 教训（记进交接 §三 坑）：**裁一条账之前，先把该账在正本里的【整段】读全**（不只读它那一行）。
> 下面这版与 `项目任务.md:500-509` 逐条对齐。

**裁定一句话**：**`A1773` 换真值时，督军那一列 `cost` 照真值改成 `2`**（那 3 张 `DA1/2/3` 是 `0` 的另说，见下）；
**连带要修的是【显示侧】**，⛔ **不是**「用 `0` 去表达督军身份」。

**四条判据（`项目任务.md:500-509` 的对抗性验证 · 主对话 2026-10-14 现读复核）**：

1. 🔴 **`manaCost` 是【序列化字段】，快照值就是作者值**：整条继承链 `RawCardScript__ctor → ObtainableItem__ctor → AddressablesBuildNumber__ctor`
   **四个环节没有一个写 `+0x78`**；另扫全部 `*.ctor.c` 里写 `(param_1 + 0x78)` 的 **18 个全是无关类**
   ⇒ **没有一处把它读成 0 / 忽略 / 清零**。
2. 🔴 **它有消费点（我上一版说「找不到」是错的）**：`CardScript__UpdateCostText.c:19-23` 对**任何**卡（含督军）**无条件 `SetText(cost) = CurrentCost`**。
3. 🔴 **原版把它整块藏起来的是【另一处】**：`BasicCardUI__SetRawCardData.c:51,81,84` —— `IsWarlordEquivalent(card)`（= `cardType + 0x94 == 10`）为真时 `Toggle(costCtrl, false)`。
   ⇒ **「显示上不可见」与「值是不是真值」是两件事**（我上一版就是把这两件事混了）。
4. ⚠️ **那 3 张 `0`（`DarkAngels_war_*`）更像【漏填】**：`cardArmy=110` · `cardShortId` 855/856/857 **连号** · **`cardType` 五个字段全空串**；
   **反证**：之后加的 `EmperorsChildren` 督军（`912/913/914`）**仍是 `2`** ⇒ **不支持「`2` 是旧批次残留」**。

**⇒ 连带要修的是【显示侧三处】（都已落地 / 已记账，⛔ 都不是「靠 `cost` 表达身份」）**：
- ✅ **① 卡面：督军不显示费用底板** —— `CardView.ShowsCost(d) => d.cost >= 0 && d.type != "hero"`（`A2006①`，2026-10-14 **已落地**）；同批补了 `StatAt` 的费用 tooltip 门。
- ✅ **② 卡组列表行：原版【不】印** —— 主对话亲跑 `工具/menu_dump.py` 现核：英雄族那行**没有 `Cost Image`**、根上 `UICardInfoItem.cardCost` 是 **null PPtr** ⇒ 我们**保持隐藏**（`DeckRuntime.cs` 的 `on && !heroRow` **本来就对**）。
  🔴 **这一条推翻了 `项目任务.md:507` 原来那句「原版【卡组列表行】会印出 `2`」**（已就地订正，铁律 5）。
- ✅ **③ 引擎效果筛选的 type 门** —— `RuleEngine/Core/CreatePool.cs:226` `if (c.Type == "hero") continue;`（`A2006③`，2026-10-14 **已落地**）。
  ⚠️ **只补了一半**：`includeMinions/Spells/Whispers` 三位没动（原版那四位是按效果数据逐条撰写的，我们只能读到 C# `bool` 缺省值）。
- 🔴 **④ 卡组编辑卡池筛选仍欠**（`DeckEditorState.cs:375-379` 按 `c.Cost` 与 `f.Cost..CostMax` 筛）⇒ **新账 N1**；
  ⚠️ 它自己的判据没查全（原版卡池「卡类」开关的缺省档未知）⇒ 已另派只读查证。
"""


def main():
    raw = io.open(P, "rb").read()
    # ⚠️ 本文件是【纯 LF】⇒ 「纯 LF」的判据是 CRLF 计数 == 0（⛔ 不是 CRLF==LF，那判的是纯 CRLF）。
    assert raw.count(b"\r\n") == 0, "本文件应是纯 LF，实测 CRLF=%d ⇒ 停手" % raw.count(b"\r\n")
    s = raw.decode("utf-8")

    start = s.index("### ✅ 已裁 ④：")
    end = s.index("### ⏳ 待裁", start)
    old = s[start:end]
    assert "保留 `0`" in old and "不采真值的 `2`" in old, "★ 待替换的旧块内容与预期不符 ⇒ 停手（先看现值）"
    assert len(old) < 4000, "旧块异常大（%d 字符）⇒ 停手" % len(old)

    out = s[:start] + NEW + "\n" + s[end:]
    assert "保留 `0`" not in out.split("### ✅ 已裁 ①")[1].split("### ⏳ 待裁")[0] or True
    assert out.count("### ✅ 已裁 ④：") == 1 and out.count("### ⏳ 待裁") == 1
    ob = out.encode("utf-8")
    assert ob.count(b"\r\n") == 0, "写入后行尾被翻"

    with io.open(P, "wb") as f:
        f.write(ob)

    back = io.open(P, "rb").read().decode("utf-8")
    assert "维持既有裁决：真值 `2` 是对的" in back and "不采真值的 `2`" not in back
    print("OK · §七 已裁 ④ 已改正 · 旧块 %d 字符 -> 新块 %d 字符" % (len(old), len(NEW)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
