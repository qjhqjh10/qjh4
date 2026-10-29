# -*- coding: utf-8 -*-
"""B99_E1 的账补丁生成器 —— 从主表取整行、只改「状态/我们的落点/差异/轮次/备注」四列，
其余列**逐字保留**，写成 `_parts/B99_E1.tsv`（11 列 · 裸 split("\t") · LF）。

⚠️ 纪律（`CLAUDE.md`）：
  · ⛔ 不改主表；本脚本只**读**主表、只**写** `_parts/B99_E1.tsv`。
  · 第一行必须是 11 列表头；`方法` 列必须是**完整签名带 `(...)`**（合并工具会校验）。
  · 裸 `split("\t")` 读写，⛔ 不用 csv 模块（默认方言会把反斜杠路径读坏）。
"""
import io

ROOT = "Unity/资料/完全复刻/账/"
MAIN = ROOT + "战斗逻辑驱动.tsv"          # 账/战斗逻辑驱动.tsv
OUT = ROOT + "_parts/B99_E1.tsv"

HDR = None

# 数据行号（= 主表文件行号 - 1；表头是第 1 行）⇒ {数据行: {列名: 新值}}
#   只列**本笔要改**的列；没列到的列原样保留。
EDITS = {
    232: {
        "状态": "A",
        # ⚠️ 落点列**整列重写**：原来那串是 `RuleEngine/Core/RuleCore.cs.CanPlayCard（:3260…）` 这种
        #    「点号 + 全角括号」形态，`check_ledger_points` **解析不出 `文件:行`** ⇒ 一翻 A 就报
        #    「落点里没有 `文件:行`」（2026-10-29 本代理实测：dry-run 失败 1 条，逐条重写成 `文件:行` 才过）。
        #    ⛔ 别改回点号形态。
        "我们的落点": (
            "RuleEngine/Core/RuleCore.cs:3270（CanPlayCard —— ① 的闸在 :3278 · ③ 侧闸 + 总旁路在 :3279 · ④ 在 :3288）"
            " · RuleEngine/Core/RuleCore.cs:17683（ResolvingTurnEnd —— 原版 `+0x245` 的等价位、恒 false；自检门 ResolvingTurnEndForTest 在 :17689）"
            " · RuleEngine/Core/RuleCore.cs:16775（WaitingForBlockingAction）"
            " · RuleEngine/Core/RuleCodes.cs（新开 `ErrWaitingBlockingAction = 30`）"
            " · CardPresentation/Editor/B99E1_Assert.cs（新，断言；宿主待调度台接）"
            " · RuleEngine/Editor/RuleEngineTest_Priority.cs（断言 `W61T33CanPlayCardWaitingBlocking`）"
        ),
        "差异_append": (
            "｜🔴【B99_E1 2026-10-29 **本件落地**】`actionAlreadyStarted` 那一格已落：`RuleCore.cs:3279` 改成 "
            "`if (ctx.IsOver) return RuleCodes.ErrNotTurn; if (p != ctx.Active && !(actionAlreadyStarted && ResolvingTurnEnd(ctx))) return RuleCodes.ErrNotTurn;`"
            "（**同一物理行 ⇒ 零行数变化**，改前/改后都是 1,7663 行），并在**类尾**新建 `ResolvingTurnEnd`（恒 `false`；doc 写全 "
            "`+0x245 resolvingTurnEndFlag` 的置真点 `EndTurnClick.c` / 清 0 点 `set_resolvingTurnEndFlag.c` / 唯一读点 `.c:68-71`，"
            "以及「本仓 `EndTurn` 同步、没有『回合结束结算中』这个可观测态 ⇒ 该旗不存在」）。"
            "⇒ 旁路**结构上在位、今天不可达**；全部调用点都把 `actionAlreadyStarted` 传默认 `false` ⇒ **零行为变化 / 零指纹变化**。"
            "断言 = `CardPresentation/Editor/B99E1_Assert.cs` 232a（基线拒）/ 232b（**灭自证**：非行动方带旗仍须拒）/ "
            "232c·232c′（行动方不受旗影响）/ 232d（钉住 `ResolvingTurnEnd == false`，将来接真值时先红）。"
        ),
        "轮次_append": "｜B99_E1",
        "备注_append": (
            "｜【B99_E1 2026-10-29】`actionAlreadyStarted` 由「只为签名对齐」升级为「**读了**、且旁路不可达（doc + 断言 232d 钉住）」。"
            "⛔ 别把它改成恒 `true`、⛔ 别拿掉 `ResolvingTurnEnd` 那一半（那样 `actionAlreadyStarted` 会单独生效 = 非行动方也能出牌 = 原版没有的行为）。"
            "⚠️ 已知近似（**改前就有、本笔不动**）：原版 ② 那一分支只在 `initiateAction` 为真时才成立，我们按 `p != ctx.Active` 合并判。"
        ),
    },
    706: {
        "状态": "B",
        "差异_append": (
            "｜【B99_E1 2026-10-29 现核】**① 已由 `B98_K2` 落** —— `RuleCore.cs:11623` 现读就是 "
            "`u.UsedActiveAbilityThisTurn = u.ActiveAbilityUsed = true; if (u.StepActiveAbilityLoading()) { ctx.Log(...); return RuleCodes.OK; }`，"
            "位置 = 两格「用过了」置位**之后**、能力宣布/结算**之前**（`ResolveActiveAbilityPlayed.c:54-128`，`+0x50/+0x4C` 在 `:29-30`）"
            "⇒ **J0 施工单 ① 不必再做**。③ FTUE 低血无敌读点现读仍在 `RuleCore.cs:8968`。"
            "**残余只在白名单外**：② `BattleDriver.cs` `ShowSkillPanel` 的读点改 `u.RequiresAbilityLoading`。"
        ),
        "轮次_append": "｜B99_E1",
        "备注_append": "｜【B99_E1 2026-10-29】本笔**零代码改动**（① 早已落；② 不在本代理白名单，见报告 §5）。",
    },
    769: {
        "状态": "A",
        "差异_append": (
            "｜【B99_E1 2026-10-29 现核（复核 `B98_K2`）】`RuleCore.cs:8064` 现读 = `Enchantments.FindCancelAttack(ctx, attacker, attacker, target)`"
            "（**五参版**）⇒ 调用点**已切**，`Enchantment.cs:885` 那句「`RuleCore` 那个调用点还没切」是**旧口径**（该文件不在本代理白名单，未改 doc）。"
            "另两处仍走单参版：`RuleCore.cs:16751`（`HasCancelAttackEffect` 谓词）与 `:17429`（替身守卫）—— 要的就是「只判在不在」的单参语义，**不是漏切**。"
            "⇒ 代码面全闭。残余 = 附魔卡进 `cards_engine.json`（`gen_cards_engine.py` 调度台独占）⇒ 另账。"
        ),
        "轮次_append": "｜B99_E1",
        "备注_append": "｜【B99_E1 2026-10-29】代码面全闭 ⇒ 核销为 A（残余为数据管线另账）；本笔零代码改动。",
    },
    866: {
        "状态": "B",
        "差异_append": (
            "｜【B99_E1 2026-10-29 回源核 ⑤ —— 结论：`+0x418` **不是** `ctx.IsOver`，且**不该补**】"
            "`dump.cs:30874 private bool waitingToResolveActiveAbilityFlag; // 0x418`；"
            "唯一读点 = `decomp_full/BattleManager__CanUseActiveAbility.c:90`（`if (*(char*)(param_1+0x418) != 0) return 0;`，紧跟在教程闸 `:85-89` 之后）；"
            "写点 = 置 1 `BattleManager__TryUsingActiveAbility.c:25/:43`、`TryUsingActiveAbilityOfCard.c:26/:48`；清 0 `ExecuteActiveAbility.c:161`、"
            "`CancelAbilityTargeting.c:5`、`FinishedChoosingAbilityTargetByClick.c:28` ⇒ 它是「主动能力**选目标 / 正在执行**」的**重入锁**。"
            "⇒ 本仓**没有那个窗口**（同步结算、没有选目标挂起态），引擎侧也没有等价位（表现侧等价物 = `BattleDriver._command == AttackKind.Ability`，引擎看不到）；"
            "既有裁定 = `BattleDriver.cs:36913-36915`「本仓无同名 flag ⇒ 按 R6 默认放行、不另造语义不明的闸」。"
            "「战斗结束」那一半由 `CanStartActiveAbility` 首行 `ctx.IsOver` 覆盖（原版靠 `StopBattleAfterBattleEnd` 清两面 allow 旗）⇒ **零改动**。"
            "④ 教程档 `actionType 30` 仍缺（要建档并挂 `BattleDriver.TutorialPermits`，白名单外）。"
        ),
        "轮次_append": "｜B99_E1",
        "备注_append": "｜【B99_E1 2026-10-29】⑤ 已回源核完（**不是** `ctx.IsOver`，且按既有裁定**不落代码**）；剩 ④（`BattleDriver` 白名单外）。",
    },
    870: {
        "状态": "B",
        "差异_append": (
            "｜🔴【B99_E1 2026-10-29 **独立复核 `B98_K2` 的反证 —— 结论相同、⛔ 不接**】① `decomp_full` 全树只有 2 个文件提到 "
            "`BattleManager__ResolveTriggerRequiem`：定义 + **`BattleManager__ResolveAction.c:5574`**（`case 0x40 = triggerRequiem` 那一支末尾）；"
            "② 生产者 `AbilityLogic__PlayAbility.c:2083`（`if (iVar8 == 0x186)` ⇒ `AddTriggerRequiem`，`0x186 = 390`），池内 `chosenAbility 390` = **0/1129**；"
            "③ `CardScript__ResolveDeadCard.c:264-283` 的安魂是**在相邻观察者身上直调** `RawCardScript__OnTrigger(0x1ae = 430, …, thisCard = 观察者, targetCard = 死者)` "
            "再 `BroadcastUnitRequiem`（闸 = 死者与观察者相邻 ∧ 观察者 `+0x228 != 5` ∧ 死者 `cardType != 10`）。"
            "⇒ **死亡收尾这条路根本不经过 `ResolveTriggerRequiem`** ⇒ 把它接进 `CleanupDeaths` = 死者自己跑一次**无目标**的 430 = 原版没有的半条路（且入口闸 `IsUnitInPlayOrDying` 对真死者本就过不了）。"
            "✅ 正确落点（本笔已定位）：(a) `RuleCore.ActiveRowEntersPlan`（`RuleCore.cs:17322`）加 `ca == 390` ⇒ 见 `ActiveRowEntersPlan` 的 doc 那四条「不看正文 op 也进计划表」的写法；"
            "(b) `EffectResolver.RunActiveRowOpsOrGrantedTrigger` 加 `ca == 390` 执行器（转 `ResolveTriggerRequiem`）—— **EffectResolver.cs 不在本代理白名单**；"
            "(c) 观察者侧 430 那一支（`FireTriggerAt(观察者, Requiem, …, chosen: 死者)` + `BroadcastUnitRequiem(观察者座位, 观察者)`）按调度台裁定**与账 230/435 分派同批**。"
            "数据现状（`cards_engine.json` v43 现扫 1,129 张）：`abilityTriggers` 230 = **0** / 430 = **0** / 435 = **0** ⇒ 今天**零可观测**。"
        ),
        "轮次_append": "｜B99_E1",
        "备注_append": (
            "｜【B99_E1 2026-10-29】本笔**零代码改动 + 反证**（纪律同 `B9_W69ENG4`）。"
            "⚠️ J0 施工单「把 `ResolveTriggerRequiem` 接进 `CleanupDeaths`」**入口找错了** —— 别再照它派活；照 (a)(b)(c) 那三步派。"
        ),
    },
}


def main():
    raw = io.open(MAIN, "rb").read()
    lines = raw.split(b"\n")
    if lines and lines[-1] == b"":
        lines = lines[:-1]
    text = [l.rstrip(b"\r").decode("utf-8") for l in lines]
    hdr = text[0].split("\t")
    assert len(hdr) == 11, "主表表头不是 11 列：%d" % len(hdr)
    assert hdr[0] == "域" and hdr[2] == "方法", "主表表头不合约定：%r" % hdr

    out_rows = []
    for dr, ed in sorted(EDITS.items()):
        f = text[dr].split("\t")
        assert len(f) == 11, "数据行 %d 不是 11 列（%d）" % (dr, len(f))
        old = dict(zip(hdr, f))
        assert "(" in old["方法"] and old["方法"].rstrip().endswith(")"), \
            "数据行 %d 的 `方法` 列不是完整签名：%r" % (dr, old["方法"])
        for col, val in ed.items():
            if col.endswith("_append"):
                base = col[:-len("_append")]
                assert base in old, "没有这一列：%s" % base
                old[base] = old[base] + val
            else:
                assert col in old, "没有这一列：%s" % col
                old[col] = val
        newf = [old[h] for h in hdr]
        assert len(newf) == 11
        out_rows.append("\t".join(newf))

    payload = "\t".join(hdr) + "\n" + "\n".join(out_rows) + "\n"
    # ⚠️ 先算进变量、确认没错再 write（`io.open(p,'wb').write(<表达式>)` 会先截断）
    io.open(OUT, "wb").write(payload.encode("utf-8"))
    print("wrote", OUT, "rows", len(out_rows))


main()
