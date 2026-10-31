#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 B109 的 C1 账补丁（`账/_parts/B109_C1.tsv`）。

只改 `战斗逻辑驱动.tsv` 的三行（都是本笔改动/核实直接造成的）：
  · row 493 `BattleManager.ResolveAddEffect(BattleAction battleAction)` —— `poisoned` 的广播钩子补上（item 2）
  · row 1288 `RawCardScript.UpdateTriggerOnce(BattleManager manager, CardScript thisCard)` —— 「仍缺数据半」已过期（item 4a）
  · row 647 `BattleManagerSupport.BroadcastUnitDamaged(...)` —— 0 伤/盾挡不派 60 的闸已存在（item 1 核实）

⛔ 不写主表；⛔ 不猜：三行都按 (原版类, 方法前缀) 精确定位，命中数 != 1 就报错退出。
"""
import io

LEDGER = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B109_C1.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    assert b.count(b"\r\n") == 0, "主表应是纯 LF（现读不成立，先查行尾）"
    assert b.count(b"\r") - b.count(b"\r\n") == 0, "主表有裸 CR"
    return [l for l in text.split("\n") if l != ""]


def find(rows, cls, method_prefix):
    hits = []
    for i, l in enumerate(rows):
        c = l.split("\t")
        if len(c) < 11:
            continue
        if c[1] == cls and c[2].startswith(method_prefix):
            hits.append(i)
    if len(hits) != 1:
        raise SystemExit("定位失败：%s / %s* 命中 %d 处" % (cls, method_prefix, len(hits)))
    return hits[0]


R493_6 = (" · :12240（🆕 2026-10-31 `C1`：**通用 `addTrait` 支的 `poisoned` 广播钩子** = 原版 "
          "`BattleManager__ResolveAddEffect.c:180/:268` 的等价物，同一物理行追加）")

R493_7 = (" ｜🔴 **2026-10-31（执行代理 `C1` 就地订正 · 铁律 5）**：上面那句「该广播今天只有那一个生产者，覆盖齐」**不成立** —— "
          "原版那道闸挂在【通用 `addEffect`】上（`.c:180` 判 `buffType(+0x38)==3 ∧ trait(+0x3c)==200`），"
          "而我们改前**只在 `poisonous` 关键词那一支播**（`RuleCore.cs:16307`）⇒ **效果文本给出的 `give … poisoned` 原版会播、我们不会**"
          "（结构性缺口；池内今天 `buffType 3 + trait 200` **0 条** ⇒ 零可观察，铁律 11 照样补）。"
          "现已补：`EffectResolver.cs:12240`（同一物理行）`if (appliedTrait != null && CardDef.DefinedTraitNumber(p.Keyword) == 200) BroadcastUnitPoisoned(ctx, u, ctx.ActingUnit);` "
          "—— `appliedTrait != null` = 原版 `local_198 != 0`（记录**落账之后**才播）；`RuleCore.ResolveDamageDealtHook`(:16307) 直接调 `ApplyTraitEffect`、**不走本函数** ⇒ **不会双播**。"
          "判据 → `判据/B109_R19_poisoned_effectText.md` §1·4｜断言 = `Editor/C1_EngineFixes_Assert.cs` A 组")

R1288_7 = (" ｜✅ **2026-10-31（执行代理 `C1` 就地订正 · 铁律 5）：「仍缺数据半」已过期** —— "
           "现读 `Data/CardDatabase.cs:1749` 就是 `def.AttachTriggerOpsByNumber(MapTriggerOpsByNumber(def))`"
           "（组装口 = `CardDatabasePartial_B98H7.cs:129`），`cards_engine.json` 现读 **v45** ⇒ **数据半已挂、本口按数据工作**"
           "（`CardDef.HasTriggerOpsByNumber` 有断言钉住，`Editor/C1_EngineFixes_Assert.cs` C 组）。"
           "休眠的**唯一**原因是**数据里没有**：`abilityTriggers` 含 200 的卡 **0 / 1,129**。"
           "文件头 `CardDefPartial_B96CA.cs:35-39` 那句「数据半还没落列 ⇒ 休眠件」同批就地订正。")

R647_10 = (" ｜🔴 **2026-10-31（执行代理 `C1`）核实**：原版 `CardScript__ResolveDamageDealt.c:31/:34` 那两道早退"
           "（`damageAbsorbed` / `damageValue == 0`）**已经**落在 `RuleCore.EmitHit`（`RuleCore.cs:9534` 的 "
           "`absorbed || amount == 0`，2026-10-24 `A20`）⇒ **盾挡 / 0 伤不派 60/185/187**；"
           "`EvtKind.Hit` 照发（`Amount = 0`，那是表现层「挨了一下」的反馈，⛔ 别一起删）。"
           "⛔ 别再据 `RuleCore.cs:9127` 那句注释判「引擎仍派 60」（那句说的是表现层事件）。"
           "断言 = `Editor/C1_EngineFixes_Assert.cs` B 组。")


def main():
    rows = read_rows(LEDGER)

    i493 = find(rows, "BattleManager", "ResolveAddEffect(")
    i1288 = find(rows, "RawCardScript", "UpdateTriggerOnce(")
    i647 = find(rows, "BattleManagerSupport", "BroadcastUnitDamaged(")

    out = ["\t".join(COLS)]

    c = rows[i493].split("\t")
    c[6] = c[6] + R493_6
    c[7] = c[7] + R493_7
    c[9] = c[9] + "｜B109_C1"
    assert len(c) == 11, "row493 列数 %d" % len(c)
    out.append("\t".join(c))

    c = rows[i1288].split("\t")
    c[7] = c[7] + R1288_7
    c[9] = c[9] + "｜B109_C1"
    assert len(c) == 11, "row1288 列数 %d" % len(c)
    out.append("\t".join(c))

    c = rows[i647].split("\t")
    c[10] = c[10] + R647_10
    assert len(c) == 11, "row647 列数 %d" % len(c)
    out.append("\t".join(c))

    for l in out:
        assert "\r" not in l and "\n" not in l, "行内有换行"
        assert len(l.split("\t")) == 11, "输出行列数不是 11"

    data = ("\n".join(out) + "\n").encode("utf-8")
    io.open(OUT, "wb").write(data)
    print("written", OUT, len(out), "lines")


if __name__ == "__main__":
    main()
