#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 `账/_parts/B9_E4BDR2.tsv`（第二批：20 / 755 / 134 / 22 + 出对半的自动断言）。

定位：`原版类 + 方法`（⛔ 不按行号）。只改 `差异` / `备注` / `轮次` / `我们的落点`；`状态` 原样带走。
"""
import io
import os

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(LEDGER, "_parts", "B9_E4BDR2.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

ROT = "E4BDR2 2026-10-27"

PATCH = [
    # ---------------- 20 SetupActiveSkill ----------------
    dict(cls="ActiveSkillDisplay", meth="SetupActiveSkill(CardScript parentCard)",
         landing="CardPresentation/Battle/BattleDriver.cs（ShowSkillPanel）+ 类尾 `ShouldShowAbilityTargetsCounter`"
                 "〔等价实现 = 后缀闸改读 **ability 级**（今天是「读不到 ⇒ 不拼」）；描述文本另走 `SkillPanel.AbilityDescription`〕",
         diff="[E4BDR2 2026-10-27] **后缀闸已改读 correct 那一层，并就地订正两条过期口径**："
              "① 原差异句「`scan_ability` 不 emit ability 级 `targetsAvailableCounter`」**不成立**（生成器已在 ability 级 emit，"
              "`gen_cards_truth.py:296-303` 的 `for k in (\"targetCriteria\", \"targetsAvailableCounter\")` 就在 `compact_active` 里）。"
              "② **原判断「池内 17 张该拼」也是错的**：本轮逐卡现扫 `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张）——"
              "(a) `activeAbility.targetsAvailableCounter.targetsAffected != 0` 的卡 **0 张**（`alternativeAbility` 0 · `cardAbilities[]` 0）；"
              "(b) 那 17 张（非 any）全部在**卡根级** `/targetsAvailableCounter`，而它们 `hasActiveAbility` **全 = 0** ⇒ "
              "`SetupActiveSkill` 对它们**根本不会跑**（`GetActiveAbilityCard()` 为 null 就早退）⇒ **原版在这份数据上永远不拼后缀**。"
              "③ 因此 `ShowSkillPanel` 的后缀闸从**卡级** `CardFaceRules.ShouldShowTargetsAvailable`（= `RawCardScript +0x298`，"
              "喂手牌下方「N 个可用」那行）换成原版 `ActiveSkillDisplay.cs:160` 读的 **`ActiveAbility.targetsAvailableCounter(+0x40)`**"
              "（落点 = `BattleDriver.ShouldShowAbilityTargetsCounter`，今天恒 false —— 读不到那一格 ⇒ 按原版「字段为 null」）。"
              "④ 仍缺的只剩**数据**：`RuleEngine/Core/CardDef.cs` + `CardDatabase` 补 ability 级那一格；落地后**只改那一个方法**。",
         note="改动请求（不属于本件）：`RuleEngine/Core/CardDef.cs` 加 `ActiveAbilityTargetsAvailableCounter` + "
              "`CardDatabase` 落列；**生成器那一半不用改**（已 emit）。⚠️ ⛔ 别改回卡级读法：那会把 17 张之外的卡也拼上后缀"
              "（原版只在 ability 级那一格存在时拼）。",
         ),
    # ---------------- 755 GetCurrentLocalizedAbilityDescription ----------------
    dict(cls="CardScript", meth="GetCurrentLocalizedAbilityDescription()",
         landing="CardPresentation/Battle/SkillPanel.cs（`Show` 的 5 个新可选形参 + 类尾 `AbilityDescription`）"
                 "〔等价实现 = 三档后缀链 `-load` / `-effect` / 裸键 + `Card_Abilities/GenericSiegeText` 兜底〕· "
                 "调用点 = `CardPresentation/Battle/BattleDriver.cs` 的 `ShowSkillPanel`",
         diff="[E4BDR2 2026-10-27] **三档后缀链已实现并接线**："
              "`CardScript__GetCurrentLocalizedAbilityDescription.c` + ILSpy `CardScript.cs:17102-17157` 逐行对齐 —— "
              "① `requiresLoading` 且 `!abilityLoaded` ⇒ `Card_Description/<id>-load`，缺键落 `Card_Abilities/GenericSiegeText`；"
              "② `requiresLoading` 且已加载 ⇒ `-effect`，缺键落裸键；③ 否则（`abilityNeedsLoading && hasRemovedReload`）"
              "原版走 `GetLocalizedAbilityDescRemovingSiege()` —— **那一支的后缀仍没解出来**"
              "（`RawCardScript__GetLocalizedAbilityDescRemovingSiege.c` 内部还拼 5 个常量）⇒ 我们**出声一次 + 退回裸键**，⛔ 不自造后缀。"
              "输入 = `CardDef.HasActiveAbility(+0x220)` / `ActiveAbilityNeedsLoading(+0x22)` / `UnitState.HasRemovedTrait(\"reload\")`；"
              "`activeAbilityLoaded(+0x224)` 我们**没有装填机制** ⇒ 恒 false（且 `ActiveAbilityNeedsLoading` 现读 1,351 张全 0 ⇒ 今天不可达）。"
              "⇒ 行为今天与改前**逐字相同**（走裸键），但形状与判据齐了。",
         note="`SkillPanel.Show` 的 5 个新形参全部**可选**（`cardId` / `requiresLoading` / `abilityLoaded` / "
              "`abilityNeedsLoading` / `hasRemovedReload`）⇒ 其它调用点零改动。缺键判据用 `Loc.T(key) != key`"
              "（⛔ 别用 `Loc.HasEntry`：它只认手写表）。",
         ),
    # ---------------- 22 CompleteBattleFinished ----------------
    dict(cls="BattleManager", meth="CompleteBattleFinished()",
         landing="CardPresentation/Battle/BattleDriver.cs（ShowEndPanelAndFreezeInput 里 `CompleteBattleFinished` 那一段）"
                 "〔等价实现 = 三段真时间闸：① 认输才等 2.0 s ② `finishedProcessingResult` 闸 ③ 再等 0.25 s → `_battleStageLeaving` + `LeaveNetRoom(force)`〕",
         diff="[E4BDR2 2026-10-27] **四条残余全补齐**（判据 = `_CompleteBattleFinished_d__389__MoveNext.c` 状态机 + `dump.cs` 字段表）："
              "① 2.0 s（`0x1834b2bbc`）**只在 `RankedMatchResult.battleResult(+0x24) == BattleResult.Forfeit(2)` 时等**（原实现无条件）；"
              "② 新增第 ② 道闸 `+0x1c == finishedProcessingResult`：**为假 ⇒ 整条协程结束**（不置 `battleStage = 8`、不离房、"
              "结算面板不出现）—— 我们离线等价恒真，落成字段 `_matchResultProcessed` + `SetMatchResultProcessedForTest`（可验）；"
              "③ 第二段等待 = **0.25 s**（`0x1834b2e7c` 现读 0.25f，原写「没读出来 ⇒ 没做」），常量 `EndPanelPostWait`；"
              "④ `LeaveBattleRoom(bVar6)` 的 `bVar6 = (battleWinner == Enemy(20)) && (battleResult == Forfeit(2))` ⇒ 落成 "
              "`LeaveNetRoom(force: Ctx.ForfeitedBy == _me && Ctx.Winner == 1-_me+1)`；`force` 在原版只影响 "
              "`NetworkCustomManager.RemoveRoomAfterLeaving()`（服务器房间删除）—— 我们**没有那一步**（P2P 无房间目录）⇒ 只记日志。"
              "⚠️ 批处理（`Application.isPlaying == false`）仍**不等**（同 `_hudHidePlayed` 那档先例）。",
         note="⚠️ `BattleManager +0x500` 是 **`RankedMatchResult _currentMatchResult`**（**不是** `MatchResultData`）—— "
              "`+0x1c/+0x24/+0x28` 分别是 `finishedProcessingResult` / `battleResult` / `battleWinner`，"
              "⛔ 别按 `MatchResultData` 的字段表去解（那一个的 `+0x1c` 是 `showAdAfterMatch`）。",
         ),
    # ---------------- 134 ShoultSetAiOpponent ----------------
    dict(cls="MatchData", meth="ShoultSetAiOpponent(PlayModes playMode)",
         landing="CardPresentation/Battle/BattleDriver.cs（类尾 `ShoultSetAiOpponent(GameMode)` + `ShoultSetAiOpponentForTest`）"
                 "〔等价实现 = 纯函数，判据逐条对齐；**本波没有接线**（理由见差异列）〕",
         diff="[E4BDR2 2026-10-27] **判据已落成纯函数，但没有接到任何闸上 —— 请调度台裁**："
              "① 函数本体照 `.c`：`u = mode - 3; u ∈ [0,10) 且 u ∉ {4,7,8}` ⇒ 真（= mode 3/4/5/6/8/9/12 为 AI），其余假；"
              "② 🔴 **它在原版是 `private` 且全量反编译 0 调用点**：按名字 `grep -rl ShoultSetAiOpponent` 只命中它自己那份文件，"
              "按地址（VA `0x736af0` / `FUN_180736af0`）也 0 命中 ⇒ **消费方不在可达代码里**，拿不到「它决定什么」的判据；"
              "③ 我们的 `AiShouldDriveOpponent`（`= _net == null`）**同时**担着 `botSeat`（match-data「对面是不是电脑」）"
              "**和** `DriveAiTurn`（本机要不要替对面出牌）两件事 —— 换成这张白名单会让 "
              "`Classic(0)/Duel(1)/PracticeLodge(2)/ClosedDeck(7)/Replay(10)/RankedFriendly(11)/Skirmish(13)` 的**单机局整局没人出牌**"
              "（原版那些模式是真人 PvP，不需要 AI；我们的离线替代必须替它们驱动对手）。"
              "⇒ **两个候选落点**：(a) 只接 `botSeatThisGame`（match-data 语义）—— 会改引擎指纹基线（防御卡 / `MatchTypes.Effective`）；"
              "(b) 接 `DriveAiTurn` —— 上面那批模式会 stall。两条都请先裁再做。",
         note="原账「净结果同」已被 R2T3B 订正为「口径不同」；本轮补的是**判定函数 + 不可达证据**。"
              "对照表：AI 模式 = Dungeon 3 / Tutorial 4 / CutScene 5 / OfflinePractice 6 / Campaign 8 / TutorialReplay 9 / OwnDeckTraining 12；"
              "非 AI = Classic 0 / Duel 1 / PracticeLodge 2 / ClosedDeck 7 / Replay 10 / RankedFriendly 11 / Skirmish 13 / Battle4Warpforge 14 及以上。",
         ),
    # ---------------- 出对半：自动断言已加 ----------------
    dict(cls="CardScript", meth="MoveIntoFieldFromHand()", append_diff=True,
         add="｜[E4BDR2 2026-10-27] **自动断言已加**：`Editor/BattleScene.cs` 新增 19·c 一节（夹具 = `tactic` + `targeted: 1`，"
             "落在**两侧都空**的格位以避开「松手即结算」快捷路）：进等目标态 ⇒ `FieldDissolvedForTest == true` 且手牌视图 "
             "`Alpha2D == 0` 且 `HitEnabled == false`；点空白取消 ⇒ `FieldDissolvedForTest == false` 且 `Alpha2D == 1` 且碰撞开回来。"
             "⚠️ 三条**前提断言**（视图在 / 起点 α=1 / 找到空格）排在结论断言之前。",
         note_add="自检 = `BattleScene.Run` 的 19·c 一节。"),
    dict(cls="CardScript", meth="DissolveWholeCard(float timeToDissolve)", append_diff=True,
         add="｜[E4BDR2 2026-10-27] 同一份断言覆盖了这一族的**驱动用法**（19·c 一节）。",
         note_add=None),
    dict(cls="CardScript", meth="UnDissolveWholeCard()", append_diff=True,
         add="｜[E4BDR2 2026-10-27] **α 0→1 现在有自动断言了**（19·c：点空白取消后 `Alpha2D == 1` 且 `HitEnabled == true`）。",
         note_add=None),
]

TABLES = ["战斗界面_HUD.tsv", "战斗逻辑驱动.tsv", "特效动画.tsv", "音频_语音.tsv", "联机.tsv"]


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n")
    return text.split("\n")


def main():
    hits = {}
    for tbl in TABLES:
        p = os.path.join(LEDGER, tbl)
        if not os.path.exists(p):
            print("MISSING", p)
            continue
        for ln in read_rows(p):
            f = ln.split("\t")
            if len(f) != 11:
                continue
            hits.setdefault((f[1], f[2]), []).append(f)
    out = ["\t".join(COLS)]
    for it in PATCH:
        got = hits.get((it["cls"], it["meth"]), [])
        if len(got) != 1:
            print("!! 命中 %d 处：%s | %s" % (len(got), it["cls"], it["meth"]))
            continue
        f = list(got[0])
        if it.get("append_diff"):
            f[7] = f[7] + it["add"]
            if it.get("note_add"):
                f[10] = (it["note_add"] + "｜" + f[10]) if f[10] else it["note_add"]
        else:
            f[7] = it["diff"]
            if it.get("note"):
                f[10] = it["note"] + ("｜原备注：" + f[10] if f[10] else "")
            if it.get("landing"):
                f[6] = it["landing"]
        f[9] = (f[9] + "·" + ROT) if f[9] else ROT
        out.append("\t".join(f))
    body = "\n".join(out) + "\n"
    io.open(OUT, "wb").write(body.encode("utf-8"))
    print("wrote", OUT, len(out) - 1, "rows")


main()
