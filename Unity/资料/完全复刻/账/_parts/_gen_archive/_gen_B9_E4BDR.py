#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 `账/_parts/B9_E4BDR.tsv`（音频线 C · A-05 + 新账「出对半」）。

只用 `原版类 + 方法` 精确定位主表里的行（⛔ 不按行号：主表正被多路并行改）。
**只改三列**：`差异` / `备注` / `轮次`（`状态`、`判据出处`、`我们的落点`、`等级` 一律原样带走）。
"""
import io
import os

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(LEDGER, "_parts", "B9_E4BDR.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

# (原版类, 方法, 新差异, 新备注(追加；None = 原样), 轮次追加)
PATCH = [
    # ---------------- 音频_语音.tsv ----------------
    ("BattleCardSounds", "GetInvokeCue(int cardRarity)",
     "[E4BDR 2026-10-27] **已修**：条件改成 `rarity == \"legendary\" && !Has(Ambush)` —— 原版 "
     "`CardToMinionAnim.c:20-26` 是 `num = HasCurrentTrait(0x4b0) ? 1 : cardRarity`，"
     "`GetInvokeCue.c:5-8` 只在 `num == 4` 时给 legendary ⇒ **伏击单位走普通 cue**（原账口径反了）。"
     "`CardToMinionAnimTeleport` 不判 ambush，但我们这条链只服务落地那一拍（没有 Teleport 部署入口）"
     "⇒ ⛔ 不加 `teleport` 形参。",
     "可达性不变：卡池 0 张 ambush ⇒ 今天不可达，必须用夹具断言"
     "（legendary+ambush ⇒ 普通；legendary 无 ambush ⇒ legendary）。"
     "顺手就地订正 `LandDustFieldFor` 那段「两条规则故意不同」——实为**同形**（都是 `!ambush && rarity==4`）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "CanChat(bool isPlayer)",
     "[E4BDR 2026-10-27] **`+0x78` 那半已补**：落成两个量 —— `_chatPopUpBlocked`"
     "（`SetPopUpDisabled/Enabled`）＋ `ChatPopupPanel.Visible`（`PopUpShowed/Hid`），"
     "`ChatPopUpBlocksLocal` 合读；`CanAiChat()` 加这道闸；`SpeakHurry` 走 `CanLocalChat()`"
     "（原版 `DisplayHurryUpChatMessage.c:3` 传 `skipCanChat = 0`）。"
     "🔴 **`SpeakCantDo` 刻意不加**：原版 `NotifyCantDoAction.c:11` 传的是 `skipCanChat = 1`，"
     "而 `DisplayLocalChatMessage.c:5-8` 只在 `param_3 == 0` 时才查 `CanChat` "
     "⇒ 那一句在弹窗开着/开场白期间**照样说**（施工单第②步把两句一起写了，按 `.c` 判据只做 Hurry）。"
     "⚠️ 一处如实记的取舍：原版那道闸在 `isPlayer != 0` 分支，我们并进 `CanAiChat`（敌方档也吃）"
     "⇒ 弹窗开着时**敌方那一句也会被挡**（施工单与断言都是这么要求的）。",
     "自检口 `ChatPopUpBlockedForTest` / `ChatPopUpDisabledForTest`。"
     "落点列仍是旧行号（本波只许改 差异/备注/轮次）：实际在 `BattleDriver.CanAiChat` 与类尾那一族。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "CanUseVOs(CardTypeOptions cardType)",
     "[E4BDR 2026-10-27] **已实现**：`BattleDriver.CanUseVOs(string cardType)` —— "
     "`__CanUseVOs.c:23-48` 逐条：① Tutorial(100)/TutorialReplay(140) 直接 true；"
     "② `\"unit\"`(Minion 0) ⇒ EnableTroopVOs；③ `\"hero\"`(Hero 10) ⇒ EnableWarlordVOs；④ 其余卡型 true。"
     "调用点 = `SpeakWarlordLine` / `SpeakIntro`。两个远端开关落成本地常量 `true`"
     "（🔶 自己设计：`UnityRemoteConfigData` 本地不可得 ⇒ 离线等价，常量处有标记）。",
     "落点列仍是「—」（本波只许改 差异/备注/轮次）：实际在 `BattleDriver` 类尾 `CanUseVOs` 一带；"
     "自检口 `CanUseVOsForTest(cardType)`。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "CloseChatBox(bool isPlayer)",
     "[E4BDR 2026-10-27] **两处都已修**：① `CloseTutorialChatBoxes` 从「无条件关三颗」改成**按颗关** —— "
     "`RadioMessage(0x5A)` ⇒ `HideBox(2)`（`__CloseRadio.c:5`）；其余 ⇒ "
     "`HideBox(BubbleSideOf(isPlayer ? _me : 1-_me))`（`__CloseChatBox.c:5` 的 `isPlayer ^ 1`；"
     "`isPlayer = action.playerAction(+0x40) || actionType ∈ {0x32,0x37}`，`AiScripted__CloseChatBoxes.c:20-31`）。"
     "② 停音那半由 `HideBox → HideBubble` 自带（`UnitsVoiceLinesPanel__HideChatBox.c:11`），"
     "`StopVoice()` 保留为兜底。",
     "可达：教程局点空白跳过聊天。⚠️ 旧备注「同一时刻只有一颗气泡」已作废（A13 三槽之后 `Speak` 不再 `HideAll`）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "DisplayWarlordChatMessage(ChatMessage chatMsgIndex, bool isPlayer, bool ignoreUiState)",
     "[E4BDR 2026-10-27] **驱动侧三处都已接**：① 我方认输 ⇒ `SpeakConcede(_me)` 之后 "
     "`_net.OnLocalChat(4)`（新增常量 `ChatIdConcede = 4`）；② `NetRemoteResign` 在 `Forfeit` 之后 "
     "`SpeakConcede(1-_me)`（**只在 `reason == Forfeit`** —— 掉线那一档对面根本没发过 chat 4）；"
     "③ 收侧 `DisplayRemoteChat` 认得 `chatId == 4`（原来会落到「越界」被丢弃）。"
     "🔴 **仍差发送闸那半**：`NetBattle.OnLocalChat` 现在还有 `IsButtonLine(5…10)` 的闸 ⇒ `4` 会被它挡掉"
     "（那一半在 `A-06` 的 `Net/**`，本波白名单外）。",
     "可达：联机局主动/被动认输。`ShouldRecord(4) == false` ⇒ 认输那句**不进动作流水**（原版 `.c:138` 同）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "DisplayWarlordRegularChatMessage(ChatMessage chatMsgIndex, bool isPlayer)",
     "[E4BDR 2026-10-27] **已修**：`SpeakWarlordLine` / `SpeakIntro` 在 `clip == null` 时**照样出气泡**"
     "（原版 `_ShowChat_d__19_MoveNext.c:37-45` 只在 `data+0x28 != null` 时才 `Play()`；"
     "时长走 `Duration(null) = 2.0`），并且 `expectVoice: clip != null` —— 免得把「这条本来没配音」"
     "报成「该有却没取到」。`CanUseVOs` 那一半见 `CanUseVOs` 行。",
     "可达：任何只有文本没有语音的一句（数据缺一张）。`ChatTermKey` 不走 `ChatIdOf` 那条顺手发现**仍未做**"
     "（`UnitChatPanel` 不在本波白名单）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "ForcePopUpClose()",
     "[E4BDR 2026-10-27] **第三个调用点已接**：`ShowAsk` 开选牌面板之前 "
     "`_chatPopup.ForcePopUpClose(immediate: !Application.isPlaying)`"
     "（原版 `BattleManager__ChoiceOfCardPlayer.c:27` 第一句）。"
     "🔴 原账那句「原版前置条件读出来是反的」**已定案、可划掉**：`+0x78` 四个写点 = "
     "PopUpShowed=0 / PopUpHid=1 / SetPopUpDisabled=0 / SetPopUpEnabled=1 ⇒ **0 = 弹窗开着**，逻辑自洽"
     "（`__ForcePopUpClose.c:5-9` 的 `if (+0x78 == 0) return` 就是「开着才关」）。",
     "战后两处仍走 `HideImmediate`（既有行为不动）。"
     "⚠️ 原版是**一个** `+0x78` 被四个写点轮流写；我们拆成 `_chatPopUpBlocked` + `ChatPopupPanel.Visible` 两个量。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "GenerateVoiceLineData(ChatMessage chatMessage, bool isPlayer)",
     "[E4BDR 2026-10-27] 与 `DisplayWarlordRegularChatMessage` 同批：① `clip == null` **不再整句丢**；"
     "② 开场白那一跳补上 `CanUseVOs`（Hero 档 = EnableWarlordVOs，离线 true）。"
     "③ `ChatTermKey` 仍不走 `ChatIdOf`（**仍欠**，落点在 `UnitChatPanel`，本波白名单外）。",
     "定制开场白三条支（同督军 -Chat1 / 定制档 / 按阵营）与原版逐条对应，本轮未动。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "SetPopUpDisabled()",
     "[E4BDR 2026-10-27] **已实现 + 判据订正（铁律 5/11）**：原写「原版整份汇编 0 调用点 ⇒ 死代码 ⇒ 建议不做」"
     "**不成立** —— `decomp_full` 里 `Missions__BlockAutoUpdates.c` 的内容才是这两个方法的本体"
     "（文件名与内容不符的旧坑）；真调用点 = `BattleManager__OnInitializationComplete.c:20`（开局）与 "
     "`_ShowHeroesIntroMessageCampaign_d__339_MoveNext.c:48`（战前脚本回合之前）。"
     "落成 `_chatPopUpBlocked` + `DisableChatPopUp()`，由 `StartIntroMonologue` 在开场白**真的开始**时调。",
     "🔴 **为什么不在 `Begin` 里 Disable**：换牌流程若没走完就没人 Enable，整局聊天钮都点不开"
     "（自检那一档尤其会被静默污染）⇒ 收口到「开场白真的开始 / 演完」两处。"
     "自检口 `ChatPopUpDisabledForTest`（只看这一半）/ `ChatPopUpBlockedForTest`（与 Visible 合一）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "SetPopUpEnabled()",
     "[E4BDR 2026-10-27] **已实现 + 判据订正**：与 `SetPopUpDisabled` 同一对。"
     "`EnableChatPopUp()` 的三个调用点 = 开场白演完（`TickIntroMonologue` 收尾）· 教程/战役局"
     "（`IsTutorialOrCampaignMatch` ⇒ 根本不播开场白）· `_unitChat`/`VoiceLines` 没就绪或先手侧算不出。"
     "后三处没有「原版调用点」，是为了不把聊天钮锁死整局（如实记）。",
     "原账「原版 0 调用点 ⇒ 建议不做」**不成立**（调用点见 `SetPopUpDisabled` 那一行）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "SetTransformedWarlordNewChats(RawCardScript selectedWarlord, MatchData matchData, bool updateAvailable)",
     "[E4BDR 2026-10-27] **已实现（控制器层一句转发）**：`BattleDriver.SetTransformedWarlordNewChats(string, bool)` "
     "→ `ChatPopupPanel.SetTransformedWarlordNewChats`（`__SetTransformedWarlordNewChats.c:5-7`；"
     "⚠️ 反编译把三个实参**全吞了** ⇒ ⛔ 别照 `.c` 里那句空调用抄）。"
     "上游「督军换卡」（`CheckForHeroDeckTransform` + `BoardAnalysis.DeckMeetsCriteria`）**整条仍未复刻** "
     "⇒ 今天 0 生产调用者（夹具可直调，自检已断）。",
     "当前数据永不触发：1351 张卡 `transformedWarlordId` 全空（原备注仍成立）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "SetUpChatOptions(MatchData matchData)",
     "[E4BDR 2026-10-27] **已接线**：`BattleDriver.Begin` 在 `BuildHud()` 之后**每次开局**调 "
     "`_chatPopup.SetChatContext(本局我方督军.Card.Id, Ctx.MatchType)`（原版 `__SetUpChatOptions.c:5-7` 转发 → "
     "选择器层 `__SetupChatOptions.c:31-48` 取的是**本局我方督军**）。"
     "⚠️ 不取 `_myDeckSrc.WarlordId`（自动凑牌那一档它是 null，而引擎侧永远有督军）；"
     "⚠️ 必须放在 `BuildHud()` 之后且每次 `Begin` 都跑（`BuildHud` 有 `_hudBuilt` 闩，二次开局面板是旧的）。"
     "同批改了 `Editor/BattleScene.cs` 那条断言：原来断「6 颗钮 = 分类词」，现在断 "
     "`Chat_Messages/<战将 id>-Chat5..10`，并加「换督军 ⇒ 文字跟着换」。",
     "可达：每局点开聊天钮就看得见（P1）。",
     "E4BDR 2026-10-27"),

    ("VoiceLinesController", "ShowHeroesIntroMessage()",
     "[E4BDR 2026-10-27] **已接**：`StartIntroMonologue` 起播时 `DisableChatPopUp()`、"
     "`TickIntroMonologue` 收尾 `EnableChatPopUp()`；另三条早退（教程/战役、语音表没就绪、先手侧算不出）"
     "各 `Enable` 一次。`ClickChatHud` 补 `+0x78` 闸（原版 `VoiceLinesPopupSelector__ClickChat.c:9` "
     "的 `if (+0x78 != 0)` 才继续）。",
     "可达：每局开局 ~2–5 秒。自检 = `ChatPopUpBlockedForTest` / `ChatPopUpDisabledForTest` 那一组 + "
     "「开场期间点聊天钮打不开」（`ClickChatHud` 那道闸）。",
     "E4BDR 2026-10-27"),

    # ---------------- 其它三份主表（「出对半」那一条横跨） ----------------
    ("CardScript", "MoveIntoFieldFromHand()",
     "「卡进 play area」这一跳有等价物；**准星与「瞄准期溶卡」没接**（特效线）。非目标战术卡的溶卡在我们的 DoPlay/CardFeel 那一侧"
     " ｜[C2T7b 2026-10-27] 卡进 play area 有等价物；但「准星」与「瞄准期溶卡(DissolveWholeCard)」没接。"
     "｜**[E4BDR 2026-10-27] 「瞄准期溶卡」这一半已接**：`BattleDriver.TryBeginPlayTargetPick` 在**非单位** targeted 卡"
     "进「等目标」态时调 `DissolveWholeCardInField`（关碰撞 + `DOScale(Vector3.one, 0.2)` + 2D α→0 OutQuad）；"
     "取消 / 回合易主 / 又拖了另一张 / 引擎拒这一手 四条路各自 `UndissolveFieldCard`（α 0→1）。"
     "**「准星」仍未接**（那一半属特效线，`StartTargetedSpellAnim` 的 prefab）。",
     "[E4BDR 2026-10-27] 判据逐句：`CardScript__MoveIntoFieldFromHand.c:60-75`（`cardType ∈ {0x14,0x28}` 且 "
     "`targeted` ⇒ `StartSpellFrom` + `StartTargetedSpellAnim` + `DissolveWholeCard(VarsGlobal+0xb0 = 0.2)`）；"
     "单位卡（`cardType == 0`）那一支**不溶**。批处理（`animateFeel` 关）当场落终态 α=0（与 DoPlay 那一跳同一约定）。",
     "E4BDR 2026-10-27"),

    ("CardScript", "DissolveWholeCard(float timeToDissolve)",
     "缺三句（原写两句）+ 一句命中开关。⚠️ 2026-10-26 J4FX 订正：① 原写「缺：StopCodex / ToggleHighlight 两句」**少算一句** —— 原版 `.c:34-51` 是 **`CardUI.StopShowingCodex()` + `CardUI.StopReflected()` + `card2DController.ToggleHighlight(false,false,false)`**（三句）+ `CardUI.FadeCardItems(0, timeToDissolve)`；另有首句 `cardCollider2D.Toggle(cardType == 10)`（我们的命中区开关是否同义要核一眼）。② 本行 `判据要点` 写「DOScale(0)」**不准** —— 实读目标是 **`Vector3.one × VarsGlobal.spellPlayedScale`**（静态块 `+0x0c = Vector3.one`；`ScriptableObject` 首字段在 **`+0x18`** ⇒ `+0x40 = spellPlayedScale`，实例值 1.0）⇒ **缩放到 1、不是缩到 0**；我们 `BattleDriver.cs:13764-13780` 已按 one 实现（①②⑥ 已做）。"
     "｜**[E4BDR 2026-10-27] 驱动层两处用法都已补齐并在判据上对齐**：① 非 targeted 那一半（`DoPlay`）"
     "的淡出从整卡 `SetAlpha` 改成 **`SetAlpha2D`**（原版 `FadeCardItems` 淡的是 `fadeGroup`，即 2D 那一层）"
     "并显式 `Ease.OutQuad`；② targeted 那一半新增 `DissolveWholeCardInField`（同一套：关碰撞 + `DOScale(one,t)` + 2D α→0）。"
     "③ `StopShowingCodex` / `StopReflected` / `ToggleHighlight(false)` 三句在我们这侧**无对应层**"
     "（战术卡没有 codex、没有反射体、非单位卡不进点亮）⇒ 如实记为「无对应物」，⛔ 没假装做了。",
     "[E4BDR 2026-10-27] 时长 = `VarsGlobal.timeToDissolveCard = 0.2`（`CardFeel.DissolveCardTime` 同源）；"
     "缓动 = DOTween 默认 OutQuad（`DOTweenSettings.asset:31 defaultEaseType = 6`，`FadeCard` 没写 `SetEase`）。",
     "E4BDR 2026-10-27"),

    ("CardScript", "UnDissolveWholeCard()",
     None,   # 差异太长了，只追加（见下 ADD 逻辑）
     "[E4BDR 2026-10-27] 🔴 **「出对半」已落地 ⇒ 本条的观测量不再是「恒 1 的起点」**："
     "战术/附魔 targeted 卡进「等目标」态时 2D α → 0（`BattleDriver.DissolveWholeCardInField`），"
     "取消时由本方法 0→1 淡回来（`BattleDriver.UndissolveFieldCard` 调 `CardFeel.UnDissolveWholeCard`）。"
     "⚠️ 批处理档（`animateFeel` 关）补间不跑，走「终态当场落」（α=1 或 0）—— 与 DoPlay 那一跳同一约定。",
     "E4BDR 2026-10-27"),
]

ADD_TO = {
    ("CardScript", "UnDissolveWholeCard()"):
        "｜[E4BDR 2026-10-27] 「出对半」已落地 ⇒ 上一条「观测量不可观察」**已不成立**（起点 α=0 现在真的会出现）。",
}

TABLES = ["音频_语音.tsv", "战斗界面_HUD.tsv", "特效动画.tsv", "战斗逻辑驱动.tsv"]


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n")
    return text.split("\n")


def main():
    rows = []
    for tbl in TABLES:
        p = os.path.join(LEDGER, tbl)
        if not os.path.exists(p):
            print("MISSING", p)
            continue
        rows.extend([(tbl, ln) for ln in read_rows(p) if ln.strip()])
    hits = {}
    for tbl, ln in rows:
        f = ln.split("\t")
        if len(f) != 11:
            continue
        hits.setdefault((f[1], f[2]), []).append((tbl, f))
    out = ["\t".join(COLS)]
    for cls, meth, diff, note, rot in PATCH:
        got = hits.get((cls, meth), [])
        if len(got) != 1:
            print("!! 命中 %d 处：%s | %s" % (len(got), cls, meth))
            for g in got:
                print("     ", g[0], g[1][5])
            continue
        tbl, f = got[0]
        f = list(f)
        if diff is not None:
            f[7] = diff
        else:
            add = ADD_TO.get((cls, meth))
            if add:
                f[7] = f[7] + add
        if note:
            f[10] = note + ("｜原备注：" + f[10] if f[10] else "")
        f[9] = (f[9] + "·" + rot) if f[9] else rot
        out.append("\t".join(f))
    body = "\n".join(out) + "\n"
    io.open(OUT, "wb").write(body.encode("utf-8"))
    print("wrote", OUT, len(out) - 1, "rows")


main()
