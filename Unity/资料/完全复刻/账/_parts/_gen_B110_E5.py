# -*- coding: utf-8 -*-
"""生成 `账/_parts/B110_E5.tsv`（只读主表取原行 → 追加几列 → 写补丁）。

为什么用脚本：目标行有两千多字，手抄必错；按 `(原版类, 方法)` 取原行再**只追加**，
可以保证补丁与主表除了追加的几列之外**逐字相同**（`merge_ledger_parts.py` 是整行替换的）。

用法（**不带显式动作参数 = 只打印用法并退出**，铁律：会写盘的脚本默认不许动手）：
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B110_E5.py --write
"""
import io
import sys

SRC = "d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
DST = "d:/4/Unity/资料/完全复刻/账/_parts/B110_E5.tsv"

# 列序 = 域 原版类 方法 判据出处 判据要点 状态 我们的落点 差异 等级 轮次 备注
APPEND = {
    3: " · 🆕 B110/E5 2026-10-31：`判据/B110_V1_真缺陷复核A.md` §🔴5（附魔模板的 `cardAbilities` 行从未被挂上）",
    4: " ｜ 🆕 B110/E5：原版模板卡**恒有** `cardAbilities` ⇒ `LoadEnchantmentTemplates` 必须挂 `AttachTriggerAbilityIndices`；"
       "`TriggerAbilityRowsOf(trigger)` 三态（`null` 未知 / `[]` 明确没有 / 非空）",
    6: " · 🆕 B110/E5 RuleEngine/Data/CardDatabase.cs:3631（triggerValue 字段）"
       " · 🆕 B110/E5 RuleEngine/Data/CardDatabase.cs:3711（LoadEnchantmentTemplates 的挂载调用）"
       " · 🆕 B110/E5 RuleEngine/Data/CardDatabasePartial_B110E5.cs:51（MapEnchantmentTriggerAbilityIndices）"
       " · 🆕 B110/E5 CardPresentation/Editor/B110_E5_Assert.cs:42（B110_E5_Assert.Run）",
    7: " ｜ 🆕 B110/E5 2026-10-31：**池外附魔模板一条 trigger 都派不出来**已修 —— 修前 `LoadEnchantmentTemplates` 只挂 "
       "`AttachCardType` / `AttachWhileInPlayAnim` / `AttachPermanentBuffs` ⇒ 模板卡 `TriggerAbilityRowsOf(...)` 恒 `null` "
       "⇒ `EnchantmentHasAbilityForTrigger` 落「按文本 op 兜底」那一档（模板正文 op 为空）⇒ 池外 30 张被授予的模板全部静默。"
       "修后 85 张模板全部挂上行（其中 2 张 `Hive Fleet Leviathan` / `Saviour Protocols` 真值里 `cardAbilities` 就为空 ⇒ **空数组 = 明确没有**）。"
       "`triggerValue` 数据列（= 410 去重闩的输入）要调度台重跑生成器，补丁见 `报告/B110_E5.md` §生成器补丁",
    9: "｜B110_E5",
    10: " ｜ 🆕 B110/E5 2026-10-31：新建的 `RuleEngine/Data/CardDatabasePartial_B110E5.cs` **必须加进 "
        "`工具/ruleprobe/ruleprobe.csproj`**（`Data/` 是逐个列举，漏了 = 探针编不过、报 `CS1061`）；"
        "断言 = `CardPresentation/Editor/B110_E5_Assert.cs`（A 映射器三态 / B 真表 85 张全挂 / C 池外被授予的 30 张全有行）；"
        "接线归调度台（`B110_E5_Assert.Run(); _sink.Pass += B110_E5_Assert.Pass; _sink.Fail += B110_E5_Assert.Fail;`）",
}

TARGET_ROW_NO = 1285          # 1-based（主表 `战斗逻辑驱动.tsv`）
TARGET_CLASS = "RawCardScript"
TARGET_PREFIX = "TriggerAbilities(AbilityTrigger trigger, BattleManager manager"


def main():
    if "--write" not in sys.argv:
        sys.stdout.write(__doc__ or "")
        sys.stdout.write("\n（未带 --write ⇒ 什么都没做）\n")
        return 0

    b = io.open(SRC, "rb").read()
    sep = "\r\n" if (b.count(b"\r\n") > 0 and b.count(b"\r\n") == b.count(b"\n")) else "\n"
    lines = b.decode("utf-8-sig").split(sep)
    header = lines[0]
    if len(header.split("\t")) != 11:
        sys.stdout.write("主表表头不是 11 列 ⇒ 停手\n")
        return 1

    row = lines[TARGET_ROW_NO - 1].split("\t")
    if len(row) != 11:
        sys.stdout.write("目标行列数 %d ≠ 11 ⇒ 停手\n" % len(row))
        return 1
    if row[1] != TARGET_CLASS or not row[2].startswith(TARGET_PREFIX):
        sys.stdout.write("目标行不匹配（类=%r 方法前 40=%r）⇒ 停手\n" % (row[1], row[2][:40]))
        return 1
    if any(t in row[0] for t in ("\n",)):
        sys.stdout.write("目标行异常 ⇒ 停手\n")
        return 1

    for idx, text in APPEND.items():
        if "\t" in text:
            sys.stdout.write("追加文本含 TAB ⇒ 停手（列 %d）\n" % idx)
            return 1
        row[idx] = row[idx] + text

    data = (header + sep + sep.join(["\t".join(row)]) + sep).encode("utf-8")
    io.open(DST, "wb").write(data)
    sys.stdout.write("wrote %s（%d 行 · 目标行 %d 列=11）\n" % (DST, 2, len(row)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
