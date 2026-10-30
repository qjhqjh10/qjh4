#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：把 `CardSetup` 与 `GetCardEffectsToShow` 两条 B 改为 A（三处残差均已落地）。
用法：python -I -X utf8 d:/4/Unity/工具/b103_flip_3_4.py [--apply]
"""
import io, sys

LEDGER = "d:/4/Unity/资料/完全复刻/账/"
JOBS = [
    ("战斗逻辑驱动.tsv", "CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy",
     "【B103 2026-10-30 判 A】施工单里那三处「仍缺（跨文件）」**已全部落地**："
     "③ 写点 2/3 的调用点 = `RuleCore.cs:11623`（`B98_K2`，`StepActiveAbilityLoading` 接进产品路径）· "
     "读点 3 = `BattleDriver.ShowSkillPanel` 走类尾 `SkillPanelLoadingFlags`（`B99_E5`）· "
     "② FTUE 低血无敌**读点** = `RuleCore.cs:9010`（`B103_R5`：改成读**掉血之前**的血 + `AddKeyword(...,0)` 幂等）。"
     "⚠️ 剩余唯一非本地项：FTUE **配置值**来自远端 `MatchData.GetBotConfigFTUE` ⇒ 已按 `🔶 自己设计` 打标、本地恒 false 并出声（调度台 2026-10-29 裁定）。"),
    ("特效动画.tsv", "GetCardEffectsToShow()",
     "【B103 2026-10-30 判 A】`B101_R7` 列的 5 条真残差**已全部落地**（`B103_W3`）："
     "`RemoveTrait` 改有序簿记（`UnitState.MarkTraitRemoved` / `RemovedTraitSeq`）· S3-c `RowGroup`（数据槽 + `Of()` 投影 + `DoGive` 发号 + `AddStatMod/TempBuff` 透传）· "
     "S3-d 附魔 `Seq`（`ActiveCardEffect.Seq` + `Attach` 盖戳）· S3-f 手牌 `Seq`（`HandEffect.Seq` + 两处写点）· "
     "计数器已收成唯一的 `CardInstance.NextEffectSeq`（`EffectSeqOf` 全仓 0 命中）。"
     "`play check` rc=0 ⇒ 指纹未动。⚠️ 唯一剩下的 **S7 `ruleprobe effects` 探针**是**验收工具**、不是本方法的行为 ⇒ 另记待办，不影响本条判 A。"),
]


def main():
    apply = "--apply" in sys.argv
    for fn, msub, note in JOBS:
        path = LEDGER + fn
        raw = io.open(path, "rb").read()
        nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
        lines = raw.decode("utf-8").split(nl)
        hits = [i for i, l in enumerate(lines)
                if l.strip() and len(l.split("\t")) > 10 and msub in l.split("\t")[2]
                and l.split("\t")[5].strip() == "B"]
        print("== %s | %s | 命中 B 行 %d" % (fn, msub[:36], len(hits)))
        if len(hits) != 1:
            print("   ⛔ 命中数不是 1，跳过")
            continue
        i = hits[0]
        c = lines[i].split("\t")
        if apply:
            c[5] = "A"
            c[10] = c[10] + "｜" + note
            lines[i] = "\t".join(c)
            io.open(path, "wb").write(nl.join(lines).encode("utf-8"))
            back = io.open(path, "rb").read().decode("utf-8").split(nl)[i].split("\t")
            assert back[5] == "A" and len(back) == len(c), "往返校验失败"
            print("   ✅ 已写 A")
    print("完成" if apply else "（预演）")


if __name__ == "__main__":
    main()
