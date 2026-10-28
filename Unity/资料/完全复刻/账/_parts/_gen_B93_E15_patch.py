# -*- coding: utf-8 -*-
"""B93_E15: build 账/_parts/B93_E15.tsv from main ledgers (read-only on main tables).

Only rows whose current 状态 ∈ {B,C,D} are touched; A rows are never touched.
"""
import io, os, sys

LEDGER = "d:/4/Unity/资料/完全复刻/账"
OUTFILE = "d:/4/Unity/资料/完全复刻/账/_parts/B93_E15.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态", "我们的落点", "差异", "等级", "轮次", "备注"]

def rd(p):
    b = io.open(p, 'rb').read()
    if b[:3] == b'\xef\xbb\xbf':
        b = b[3:]
    return b.decode('utf-8').replace('\r\n', '\n').split('\n')

# (主表文件, 数据下标) -> (新状态, 新落点 or None=keep, 新差异, 轮次追加, 备注追加)
CHANGES = [
    ("未分类_其他", 165, "A",
     "CardPresentation/Battle/BattleDriver.cs:19474（PlaySignals）",
     "B93_Q3 现核：订阅式 ⇒ 直调，三跳已齐（本代理 2026-10-28 现读）：① 累计 `Battle/BattleDriver.cs:19474 PlaySignals` 内 :19576（`e.Kind == EvtKind.CollectWaystone ⇒ _waystonesCollected[seat] += …`）；② 交出并清零 `:32534 AwardAchievements` 内 :32547（`ApplyAchievementTally(…, _waystonesCollected[me]); _waystonesCollected[me] = 0;`）；③ 消费 `Shell/ProfileData.cs:682`（`case 「CollectSpiritStone」: return spiritStonesGained;`，末尾可选形参）。原版 `OnInitialize` 的 `Signal.Register` 那一跳由引擎直调承担 ⇒ 行为等价（原版名 ≠ 我方名，按行为/容器/消费点判）。",
     "B93_E15 2026-10-28", "状态 C→A（B93_Q3 现核 + 本代理现读三跳）"),

    ("未分类_其他", 561, "A",
     "CardPresentation/Battle/BattleDriver.cs:17870（ApplyTutorialActionView）",
     "B93_Q3 现核：取键已**内联**（本代理 2026-10-28 现读 `Battle/BattleDriver.cs:17870 ApplyTutorialActionView` 内 :17915-17918：`key = 「TutorialData/」 + a.textReference` ⇒ `Loc.OfficialCurrent(key)`，取不到回落 `a.arg` 并出声）。⚠️ 残差：原版 `isPCTip ? textReference + 「-PC」 : textReference` 的 `-PC` 支未做 —— 本仓数据 468/468 `isPCTip = false`（`RuleEngine/Core/TutorialScript.cs:273` 有声明、全库无一处为真）⇒ 该支本 build 不可达；将来出现 isPCTip=true 的关卡会取不到词条。",
     "B93_E15 2026-10-28", "状态 B→A（B93_Q3 现核 + 现读内联落点；-PC 支不可达，残差已写入差异列）"),

    ("日常_奖励", 46, "B",
     "CardPresentation/Shell/ChallengeData.cs:92（RemovePending）",
     "B93_Q3 现核：实现体已在（现读 `Shell/ChallengeData.cs:92 RemovePending` = 撤掉一条 + `UiBadgeNotificationManager.Refresh`）。判 B（不是 C）：等价物在、**只差接线** —— 缺 ① 点击来源（挑战挂起条的取消钮）② `RemovePending` 返回 true 后关掉对应窗。原版 `.c` 里 `RemovePracticeMatch(玩家名)`（服务端）那一半 = P3。",
     "B93_E15 2026-10-28", "状态 C→B（B93_Q3 现核：等价物在、只差接线）"),

    ("日常_奖励", 545, "B",
     "CardPresentation/Shell/MainMenuRuntime.cs:1190（NavButton）",
     "B93_Q3 现核：title / 倒计时已接（现读 `Shell/MainMenuRuntime.cs:1190 NavButton` 里 :1154 的 title 与 :1266 的 Timer 两段，B88_J2）。判 B：残差 = 红点类型登记那一支（原版按事件的 `EventComponents[EventBadge]` 逐条 `badgeNotification.AddNotificationType(...)`）⇒ 接到 `Shell/BadgeNotifications.cs` 的 provider 表。",
     "B93_E15 2026-10-28", "状态 C→B（B93_Q3 现核：title/倒计时已在、缺红点登记）"),

    ("日常_奖励", 583, "B",
     "CardPresentation/Shell/GameModesHandler.cs:364（SetResultCount）",
     "B93_Q3 现核：本地那一半已有落点 —— 现读 `Shell/GameModesHandler.cs:364 SetResultCount(string mode, BattleWinner winner)`（账 224 已建）。判 B：残差 = ① 把练习局结果接到它上面（今天 0 调用方）② `PlayerDataManager.RecordPracticeMatchFinished` 是服务端记账 = P3。",
     "B93_E15 2026-10-28", "状态 C→B（B93_Q3 现核：本地 SetResultCount 已在、缺接线；记账半 = P3）"),

    ("日常_奖励", 582, "B",
     "CardPresentation/Shell/GameModesHandler.cs:485（NotifyPracticeMatchWon）",
     "B93_Q3 现核：草稿解锁口已在（现读 `Shell/GameModesHandler.cs:485 NotifyPracticeMatchWon()` = 原版 `PracticeEvent.OnMatchWon` 的落地口，0 调用方；`DraftModeUnlocked` 在 :462）⇒ 判 B（不是 D：等价机制在、置位未接）。残差 = 「进主菜单时练习局未收尾 ⇒ 判负 + 弹 `Demo/MainMenu/OngoingMatch`」这一支未接。⏱ 时间限定：本 build 对局无中途存档 ⇒ 该状态本地不可产生，接上后只会走「无挂起局」那一档。",
     "B93_E15 2026-10-28", "状态 D→B（B93_Q3 现核：解锁口已在、Initialize 置位未接；可逆）"),

    ("未分类_其他", 478, "C",
     "—（无）",
     "B93_Q3 现核：**我们真的没做**（原判 B 的「机制不同」不成立 —— 不是机制不同，是整支缺）。缺的是 bot 档案支四件：`name = botName` · `IsBot = true` · `ProfileAllianceInfo = new` · `botElo > 0` 才写 `CurrentEloRating`；原版调用点 `PlayerProfileMenu__Open.c:106` / `__OnComplete.c:63`（目标是 bot ⇒ 给档案窗造一份假档案）。本代理 2026-10-28 现读 `Shell/PlayerProfileWindow.cs` 全文 **0 处 bot 支**（无 IsBot / botName）⇒ 要做 = 在 `PlayerProfileWindow.Open()` 加 bot 支（elo 只在 >0 时写）。",
     "B93_E15 2026-10-28", "状态 B→C（B93_Q3 现核 + 现读 0 处 bot 支；原「机制不同」不成立）"),

    ("战斗逻辑驱动", 1278, "B",
     "—（无；接入 Addressables 后才补）",
     "🔶 机制不同（原版走 Addressables）⇒ 记 **B、不判 D**：原版三类动作真实存在且可达（`.c:1-142`：`+0xd0` 的 `AssetReference` ⇒ Load · abilities 的 abilityLogic · cardAnimations），本仓运行时 **0 处** Addressables / AssetReference API、美术走 `Resources/` 同步加载 ⇒ 无对象可预载、玩家不可观测。接入条件 = 本工程接 Addressables 后按 `.c` 三类逐条照抄。",
     "B93_E15 2026-10-28", "状态 D→B（本批口径：架构性不适用 ⇒ B 不判 D）"),

    ("日常_奖励", 310, "B",
     "—（无；注入口 = Shell/LiveOpsEvent.cs 的 LiveOpsContainers，默认无 provider）",
     "🔶 机制不同（原版走 Addressables）⇒ 记 **B、不判 D**：原版 = 释放事件资产的 Addressables 句柄（真实可达）；本仓无 Addressables 装载层 ⇒ 无引用可释放、行为恒空。已有注入口 = `Shell/LiveOpsEvent.cs` 的 `LiveOpsContainers`（默认无 provider ⇒ 恒 null，但缺件出声）。接入条件 = 引入 Addressables 装载层并出现第一个消费者后按原版实现。",
     "B93_E15 2026-10-28", "状态 D→B（本批口径：架构性不适用 ⇒ B 不判 D）"),

    ("日常_奖励", 486, "B",
     "—（无；同上注入口）",
     "🔶 机制不同（原版走 Addressables）⇒ 记 **B、不判 D**（本批口径；此前 B→C→D 两次翻转，以本批为准）：原版 = 释放事件子容器的 Addressables 句柄；本仓无容器、无引用计数 ⇒ 无对象可释放。接入条件 = 接入 Addressables 事件资产容器层后按原版实现。",
     "B93_E15 2026-10-28", "状态 D→B（本批口径：架构性不适用 ⇒ B 不判 D）"),

    ("日常_奖励", 523, "B",
     "—（无；同上注入口）",
     "🔶 机制不同（原版走 Addressables）⇒ 记 **B、不判 D**（本批口径；此前 B→C→D 两次翻转，以本批为准）：原版 = 把配置/挑战解析出来的 Addressables 资产引用清掉；本仓无容器 ⇒ 无对象可释放。接入条件 = 同 486。",
     "B93_E15 2026-10-28", "状态 D→B（本批口径：架构性不适用 ⇒ B 不判 D）"),

    ("日常_奖励", 308, "B",
     "CardPresentation/Shell/LiveOpsEvent.cs（LiveOpsContainers.Load —— 注入口，默认无 provider）",
     "🔶 机制不同（原版走 Addressables）⇒ 记 **B、不判 D**：原版 = 按组件 GUID 从 EventAssets 表 `LoadComponent` 并实例化（真实可达）；本仓无 Addressables 装载层，最接近的等价物 = 注入口 `LiveOpsContainers.Load`（`Shell/LiveOpsEvent.cs`，默认无 provider ⇒ 恒 null + 缺件出声）。接入条件 = 补「本机解组件 GUID」的能力（同 160）+ 第一个消费者。⚠️ 同族重载 `LiveOpsAssetUtility.LoadComponent<T>(LiveOpsEventData, …)`（本表 309）不在 B93_Q3 的 220 之内，仍是 D —— 两行的处置口径需要调度台对齐。",
     "B93_E15 2026-10-28", "状态 D→B（本批口径：架构性不适用 ⇒ B 不判 D）"),
]

def main():
    tables = {}
    for fn in sorted(os.listdir(LEDGER)):
        if fn.endswith('.tsv') and not fn.startswith('_'):
            tables[fn[:-4]] = rd(os.path.join(LEDGER, fn))

    # key index across all tables (for duplicate detection)
    keyidx = {}
    for dom, L in tables.items():
        for i in range(1, len(L)):
            c = L[i].split('\t')
            if len(c) >= 3:
                keyidx.setdefault((c[1].strip(), c[2].strip()), []).append((dom, i))

    out = ["\t".join(COLS)]
    problems = []
    for dom, idx, st, land, diff, rnd, note in CHANGES:
        c = tables[dom][idx].split('\t')
        assert len(c) == 11, (dom, idx, len(c))
        cur = c[5].strip()
        if cur not in ('B', 'C', 'D'):
            problems.append(f"{dom}:{idx} 当前状态 {cur} 不在 B/C/D ⇒ 跳过")
            continue
        if '\t' in diff or '\n' in diff:
            problems.append(f"{dom}:{idx} 差异含制表/换行")
            continue
        hits = keyidx.get((c[1].strip(), c[2].strip()), [])
        new = list(c)
        new[0] = c[0]
        new[5] = st
        if land is not None:
            new[6] = land
        new[7] = diff
        new[9] = (c[9] + '｜' + rnd) if c[9].strip() else rnd
        new[10] = (c[10] + '｜' + note) if c[10].strip() else note
        out.append('\t'.join(new))
        mark = 'DUP' if len(hits) > 1 else 'ok '
        print(f"{mark} {dom}:{idx} {cur}->{st}  {c[1]}.{c[2][:55]}  hits={hits}")

    if problems:
        print("!! 问题：")
        for p in problems:
            print("   " + p)

    data = ('\n'.join(out) + '\n').encode('utf-8')
    if '--write' in sys.argv:
        io.open(OUTFILE, 'wb').write(data)
        print("wrote", OUTFILE, len(out) - 1, "rows")
    else:
        print("(dry) rows =", len(out) - 1, "-> pass --write to write")

main()
