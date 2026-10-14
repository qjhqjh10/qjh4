# -*- coding: utf-8 -*-
"""W4：`Core/Loc.cs` 里那几条**已被官方表推翻的旧注**（铁律 5：就地改掉、留更正痕迹）。"""
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

DO = "--write" in sys.argv

EDITS = [
    # ① 文件头那条「逐处已于同日加注」—— 现核**不成立**（下面这些旧注还在）
    ("//    ⚠️ **本文件里凡是还写着「原版中文在远端 I2 表 / 中文那一列是我们译的 / 本地没有中文表」的旧注，\n"
     "//       一律以本条为准**（逐处已于同日加注，见各处 `2026-10-14`）。\n",
     "//    ⚠️ **本文件里凡是还写着「原版中文在远端 I2 表 / 中文那一列是我们译的 / 本地没有中文表」的旧注，\n"
     "//       一律以本条为准**（逐处已于同日加注，见各处 `2026-10-14`）。\n"
     "//       🔴 **2026-10-14（第二十八会话 · `A1803`）就地订正上面那句「逐处已于同日加注」—— 不成立**：\n"
     "//       本文件里**仍有约 35 处**旧注写着「值在远端 I2 表 / 本地取不到」（`grep -n \"远端 I2\\|远端 CCD\"`\n"
     "//       现读）。★ **那批不是「值取不到」，是「当时还没接」** —— 官方 zh-CN 表 2026-10-14 已在本地。\n"
     "//       本会话（`A1803`）只按账**逐条接了 `Battle/` 那一族 + 关键词 tooltip + 6 条中文**，\n"
     "//       **没有做全量注释扫**；其余各处**仍按文件头这一条理解**（⛔ 别照那些旧注当判据）。\n")
    ,

    # ② `Battle/Mulligan/Undo`：值**不是**自拟的（官方两列逐字相同）
    ("            { \"Battle/Mulligan/Undo\",              new Entry(\"撤销\",     \"Undo\") },        "
     "// 🔴 EN/ZH **都自拟**（值在远端 I2 表）\n",
     "            // 🔴 **2026-10-14（`A1803`）就地订正**：上面那句「EN/ZH 都自拟」**不成立** —— 官方\n"
     "            //   `Battle/Mulligan/Undo` = `[\"Undo\", \"撤销\"]`，与本条两列**逐字相同**（错因：当时表还没解开）。\n"
     "            { \"Battle/Mulligan/Undo\",              new Entry(\"撤销\",     \"Undo\") },        "
     "// ✅ 两列 = 官方值（2026-10-14 核）\n"),

    # ③ `Battle/Tips/GoFirst`：EN 也不是自拟的
    ("            { \"Battle/Tips/GoFirst\",               new Entry(\"你先行\",   \"You go first\") }, "
     "// 🔴 中文 = **原版实拍**（2026-10-13 用户参考图）；EN 仍自拟（值在远端 I2 表）\n",
     "            { \"Battle/Tips/GoFirst\",               new Entry(\"你先行\",   \"You go first\") }, "
     "// 🔴 中文 = **原版实拍**（2026-10-13 用户参考图）；"
     "✅ **2026-10-14（`A1803`）订正**：EN 那句「值在远端 I2 表」**不成立** —— 官方 = `[\"You go first\", \"你先行\"]`，**两列都是官方值**\n"),

    # ④ `Battle/Tips/HandFull` 上面那段（「这一条的值【不是原版的】」）
    ("            //   🔴 **这一条的值【不是原版的】** —— `0x428A218` 查出来的那个键的**文案在远端 I2 表**，本地拿不到；\n"
     "            //   下面是**我们自己写的兜底**（`BattleDriver.HandFullText()` 的文档里已如实标注）。\n",
     "            //   🔴 **2026-10-14（`A1803`）就地订正**：这一段原来写「这一条的值**不是原版的** —— 文案在远端 I2 表、\n"
     "            //   本地拿不到」—— **两句都不成立**：官方 `Battle/Tips/HandFull` = `[\"Hand is full\", \"手牌已满\"]`，\n"
     "            //   表里这两列**就是官方值**（ZH 早已对上；EN 本会话也换成官方那句了）。\n"
     "            //   ⚠️ 仍**不是**原版的只有 `BattleDriver.HandFullText()` 里那句兜底副本（`Hand full - the drawn\n"
     "            //   card goes to the discard pile`）—— 它今天走不到（`Loc.HasEntry` 为真），但要跟着改。\n"),

    # ⑤ `Battle/Chat/*` 那 6 条：「本地没有任何一处能印真标签」——键与值都到手了
    ("            //   ⚠️ `Battle/Chat/*` 那 6 条：原版标签来自**远端 I2**、序列化兜底 6 个**全是 `Greetings`** ⇒ 本地没有任何一处能印真标签\n"
     "            //     （`Battle/ChatPopupPanel` 里那 6 个钮那段头注）；本表的 EN = 调用点现用的那 6 个英文标签，ZH 是**自拟**。\n",
     "            //   🔴 **2026-10-14（`A1803`）就地订正**：这一段原来写「原版标签来自远端 I2、本地没有任何一处能印真标签」\n"
     "            //     —— **不成立**：官方表里 `Battle/Chat/{Greetings,Threat,WellPlayed,Taunt,Sorry,Oops}` 六条**都在**\n"
     "            //     （`Greetings` 那条已随本会话的 105 条进表，本表原来那条键名写错的 `Battle/Chat/Greet` 已删）。\n"
     "            //     ⛔ **但今天仍印不出真标签** —— 钮上的字走 `Battle/ChatPopupPanel` 里那颗**硬编码数组**\n"
     "            //     `{ \"Greet\", \"Threat\", \"Well Played\", … }`，**没接本表** ⇒ 那一步要改 `Battle/ChatPopupPanel.cs`\n"
     "            //     （⛔ 不在本笔白名单，已记进 W4 报告）。\n"
     "            //     ⚠️ 同时订正：下面那 5 条里除 `WellPlayed` 的 EN（本会话已换成官方的 `Well played`）外，\n"
     "            //     ZH **本来就是官方值**（原来标「自拟」是错的）。\n"),
]


def main():
    p = L.LOC
    data = io.open(p, "rb").read()
    s = data.decode("utf-8")
    n = 0
    for old, new in EDITS:
        c = s.count(old)
        if c != 1:
            raise SystemExit("❌ 命中 %d 次（应为 1）：%r" % (c, old[:100]))
        s = s.replace(old, new)
        n += 1
    out = s.encode("utf-8")
    if DO:
        io.open(p, "wb").write(out)
        print("落盘 %s（%d 处注释订正，%d -> %d 字节）" % (p, n, len(data), len(out)))
    else:
        print("（干跑，%d 处）" % n)


if __name__ == "__main__":
    main()
