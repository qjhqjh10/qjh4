# -*- coding: utf-8 -*-
"""生成 `账/_parts/B93_E11.tsv` —— B93 波 5 · E11（日常/奖励 第 2 批 16 条）的账补丁。

做法（⛔ 不手抄 11 列）：
  1. 读主表 `账/日常_奖励.tsv`，按「原版类 + 方法签名」定位我这 16 行（104 / 107 已按简报排除）；
  2. 每行**只改** 状态 / 我们的落点 / 差异 / 轮次 / 备注；其余列原样保留；
  3. 写出整行 11 列，表头逐字照抄主表；UTF-8 + LF（主表是纯 LF；合并脚本按 utf-8-sig 解、行尾照抄主表）。

校验（不过就抛，⛔ 不静默写）：每行 11 列 · `方法` 带 `(` · 状态是 A/B/C/D · 命中唯一 · 行数 = 16。
"""
import io
import sys

MAIN = r'd:/4/Unity/资料/完全复刻/账/日常_奖励.tsv'
OUT = r'd:/4/Unity/资料/完全复刻/账/_parts/B93_E11.tsv'
ROUND = 'B93_E11'
TAG = '[B93_E11 2026-10-28] '

C_DOMAIN, C_CLASS, C_METHOD, C_SRC, C_POINT, C_STATE, C_WHERE, C_DIFF, C_LEVEL, C_ROUND, C_NOTE = range(11)

# key = (原版类, 方法签名) —— 与主表 `方法` 列**逐字**相同
SET = {}
DIFF_APPEND = {}
NOTE_APPEND = {}


def put(cls, method, state, where, diff, note=None):
    k = (cls, method)
    SET[k] = {C_STATE: state, C_WHERE: where}
    DIFF_APPEND[k] = diff
    if note:
        NOTE_APPEND[k] = note


# ---------------------------------------------------------------- ① 原版死代码族（4 条）
DEAD = ('**判 D：原版这一族在本 build **不可达**（无行为可复刻）** —— 三源合判：'
        '① `decomp_full` 里 `ChallengeSerialData` 只出现在它自己的 2 个方法文件里（全树 `grep -rl` = 2）；'
        '② `out_cpp2il_dev_ilspy` 全树搜 `ChallengeCurrentValue|ChallengeLevelReached` 只命中 `ChallengeSerialData.cs` 自己；'
        '③ `out_il2csharp/Assembly-CSharp` 同（`ChallengeVarResult` 全树只 3 个文件）；'
        '④ `dump.cs` 里**没有任何字段/形参**用到 `ChallengeResults`/`ChallengeSerialData`/`ChallengeVarResult`'
        '（只有它们自己的成员互相引用），唯一持有者是编辑器 SO `ChallengesEditorObject`（`GetChallengesSerialData` 全树 0 调用 ⇒ 本表 `:71` 已判 D）；'
        '⑤ `ChallengeVariable` 枚举全树只被这三个类引用。'
        '⇒ 整族（`ChallengeVariable` + `ChallengeSerialData` + `ChallengeResults` + `ChallengeVarResult`）**没有可达入口**。')

put('ChallengeSerialData', 'ChallengeCurrentValue(Dictionary<string, ChallengeVarResult> challengeResults)', 'D',
    'Shell/ProfileData.cs:1168（AchievementValue）（参考等价物；原版侧不可达）',
    TAG + DEAD +
    ' 参考（⛔ 不是判 A 的理由）：原版**活**路是 `AchievementsManager.GetValue → ChallengeProgress.CurrentValue`，'
    '我们那一格是 `ProfileData.AchievementValue`；本行的方法本身在原版里没有任何调用者。'
    '按铁律 11 例外①（“原版本身就没有 / 死代码”）判 D。',
    '判档请调度台复核（铁律 11 例外①：死代码；证据见差异列）')

put('ChallengeSerialData', 'ChallengeLevelReached(Dictionary<string, ChallengeVarResult> challengeResults)', 'D',
    'Shell/ProfileData.cs:1203（AchievementTierOf）（参考等价物；原版侧不可达）',
    TAG + DEAD +
    ' 参考：本方法体内那套「按 `challengeComparison` 逐档比、取最后一个满足的档」在活的成就路上对应的'
    '**是另一个方法**（`Achievement.TargetTier` / `CurrentTier`），我们那一格是 `ProfileData.AchievementTierOf`；'
    '`Resources/profile_cosmetics.json` 的 102 条成就 `f.reachMilestoneWhen` **102/102 = 10（HigherOrEqual）** ⇒ 我们的 `v >= 阈值` 与该比较档一致。'
    '按铁律 11 例外① 判 D。',
    '判档请调度台复核（铁律 11 例外①：死代码；证据见差异列）')

put('ChallengeVarResult', 'UpdateValue(int newValue, int newValueCumulative)', 'D',
    'Shell/ProfileData.cs:528（GainAchievement）（参考等价物；原版侧不可达）',
    TAG + DEAD +
    ' 参考（原版语义，逐句）：`highest = max(highest,newValue)` · `lowest = min(lowest,newValue)`（`-1` 哨兵、`int.MaxValue` 初值）· '
    '`cumulative += newValueCumulative`（`cumulative < 0` 时直接取 `newValueCumulative`）。'
    '本 build 唯一消费者是被**编译器内联**进 `ChallengeResults.UpdateChallengeResults` 的那一份（本表 `:64` 判 A），'
    '而 `ChallengeResults` 自己 0 引用 ⇒ 三格里只有 `cumulative` 在我们这边有对应（`GainAchievement`/`AddAchievementValue` 累加；'
    '`highest` 由 `SetAchievementValueAtLeast` 兜；`lowest` 无活消费者）。按铁律 11 例外① 判 D。',
    '判档请调度台复核（铁律 11 例外①：死代码；证据见差异列）')

put('ChallengeVarResult', 'UpdateValueAbsolute(int newValue)', 'D',
    'Shell/ProfileData.cs:563（SetAchievementValueAtLeast）（参考等价物；原版侧不可达）',
    TAG + DEAD +
    ' 参考（原版语义）：`highest/lowest` 同上，`cumulative = max(cumulative,newValue)` —— 即**只抬不落**，'
    '与我们 `SetAchievementValueAtLeast` 同义（那一格的注释本来就写着“原版 `highestValue` 语义”）。'
    '原版这一支的活入口是 `ChallengeResults.SetOnlineChallengeData`（与**在线值取高**，纯服务端；本表 `:63` 判 D）。按铁律 11 例外① 判 D。',
    '判档请调度台复核（铁律 11 例外①：死代码；证据见差异列）')

# ---------------------------------------------------------------- ② 450：原版活的，等价物逐句对过 ⇒ A
put('MissionChallenge', 'UpdateProgressIfHigher(int value)', 'A',
    'Shell/ProfileData.cs:563（SetAchievementValueAtLeast）',
    TAG + '**判 A（原版是活的，等价物逐句对过）**：原版体 = `MissionChallenge__UpdateProgressIfHigher.c`（造一个 payload：`+0x10 = value`）'
    '+ 谓词 `MissionChallenge.__c__DisplayClass37_0___UpdateProgressIfHigher_b__0.c`（`cur = progress.CurrentValue;` → '
    '`progress.CurrentValue = Math.Max(cur, value)` → `return cur != 新值`）= **只抬到至少 N**，并对“变了的那几行”发 `OnProgressUpdate`。'
    '我们的 `SetAchievementValueAtLeast`（`:563`）就是同一句“只涨不落”。'
    '**调用点也逐一对上**：原版只有 2 个调用者（`ConstantChallenge__UpdateProgress.c`、`DraftWins__OnTrigger.c`）；'
    '我们是 `Shell/DailyData.cs:1043/1044`（`Ach_17` 累计登录 / `Ach_18` 连登 = ConstantChallenge 那两条）+ '
    '`Shell/DraftModeWindow.cs:2286`（`Ach_26` DraftWins）。**残留差异**：① 原版回“这批 progress 里有没有变化”并广播 `OnProgressUpdate`，'
    '我们回 void、界面靠重建刷新；② 原版一次调用批量刷全部匹配行，我们按 `Ach_N` 单条写。')

# ---------------------------------------------------------------- ③ 每日奖励窗（9 条）
PUT = 'Shell/DailyRewardPopup.cs'
put('DailyRewardDrawerController', 'SetState(State state)', 'A',
    PUT + ':891（Drawer.SetState）',
    TAG + '**判 A（已落同名同流程）**：新增 `Drawer.SetState(RewardState)`，逐句对应 `DailyRewardDrawerController__SetState.c:8-28` 那五句：'
    '`premiumLockIndicator`⇔PremiumLocked · `claimedIndicator`⇔Collected · `shadow`⇔Collected · `highlight`⇔Unlocked · '
    '`claimButton`（`SetActive` + `interactable`）⇔Unlocked · 末句 `CurrentState = state`。'
    '`BuildDrawer` 里原来散着的四条 `SetActive` 已删，改由它一处说了算。**两处如实差异**：'
    '① `Shadow` 那一件我们**没有**（无图，文件头已标）⇒ 那一句不落；② 原版是四层 `if (x != null)` 嵌套、任何一件为空就抛，'
    '我们是运行时建节点 ⇒ 逐件判空跳过（⛔ 不照抄那个抛出）。')

put('DailyRewardItemContainer', 'OnCollect()', 'A',
    PUT + ':905（Drawer.OnCollect）',
    TAG + '**判 A（已落同名同流程）**：`Drawer.OnCollect()` → `DailyRewardPopup.CollectRewardClicked(day, premium)`'
    '（= 原版 `DailyRewardSelector.CollectRewardClicked` → `m_OnCollect`）→ 窗上 `OnCollect`（= `<Start>b__6_0` 的 `TryCollect()`）'
    '→ `DailyData.CollectReward`。四跳与原版 `+0x70 → +0x58 → <Start>b__6_0 → TryCollect` 一一对应；'
    '原版 `OnCollect` 体只有一句“调回调”，我们同形。')

put('DailyRewardPopup', 'Start()', 'B',
    PUT + ':1012（WireUp）',
    TAG + '**判 B（判据未闭合，只差第 4 跳）**：新增 `WireUp()` 承担原版 `Start()` 的四跳接线中的第 3 跳，并逐跳写清落点：'
    '① `closeButton.m_OnClick += Close` ⇒ `BuildSideBar` 那颗圆钮（`CloseCollecting`）；'
    '② `armySelector.OnArmySelected += SetArmy` ⇒ `BuildArmyCell` 的命中区（换阵营 + 重建）；'
    '③ `rewardsTrack.OnCollect += (milestone) => TryCollect()` ⇒ `WireUp` 里 `OnCollect += …`（**本轮新接**）；'
    '④ `timerText.OnRunOutOfTime += <Il2CppClass+0x1B0 上的方法常量>` ⇒ 🔴 **未接**。'
    '**第 4 跳为什么判 B**：那个处理函数在 `.c` 与 ILSpy 里都只给出偏移、解不出名字；按 `0x168 + slot*8` 反推，'
    '它同时对得上 `GameWindow.TryOpen`（壳层）与 `ToFocus` 两种可能 ⇒ ⛔ 不猜（铁律 2/3）。'
    '今天到点只有 `TimerDisplay` 自己“隐藏文本 + 只发一次事件”，与本窗无关 ⇒ 可观察差别只在跨日那一拍。')

put('DailyRewardSelector', 'CollectRewardClicked(ChallengeMilestone milestone)', 'A',
    PUT + ':927（CollectRewardClicked）',
    TAG + '**判 A**：同名落地（形参从 `ChallengeMilestone` 换成 `(int day, bool premium)` —— 我们没有里程碑 SO，'
    '而原版那条链末句 `TryCollect(null)` **根本没用那个 milestone 形参**）。体同原版：只做 `OnCollect?.Invoke(...)`。')

put('DailyRewardSelector', 'CreateRewards(bool refreshAllRewards)', 'A',
    PUT + ':938（CreateRewards）',
    TAG + '**判 A**：原版“先 `Reset()`、再按 `challenge.rewards` 逐格 `Instantiate` + `Initialize`”已按同名落地。'
    '**一处如实差异**：原版有 `if (i < 7) 强制刷` 的闸（前 7 格 = 一屏必刷，第 8 格起才看 `refreshAllRewards`）；'
    '我们现读只有 4 格 ⇒ 那个闸**恒真**，写成空分支就是死代码 ⇒ **只记不写**。')

put('DailyRewardSelector', 'Initialize(MainMenuMission evt)', 'A',
    PUT + ':990（InitializeRewards）',
    TAG + '**判 A**：原版 = 清空内容区 → `RefreshData(null, refreshAllRewards: true)` → `ForceRebuildLayoutImmediate` → '
    '`if (5 < 当前格) FocusOnItem(items[当前格 − 3])`。我们落成 `InitializeRewards()` = `ResetRewards()` + `CreateRewards(true)`（`Build` 第 3 步调它）。'
    '**两处如实差异**：① 口名不是原版名（`Initialize` 与既有成员/生命周期撞名，`Build` 里的 `Open` 才是建窗口）；'
    '② 最后那条“聚焦”**恒不动作**（4 格 ⇒ `5 < 4` 恒假）⇒ 不写死分支（同 `Shell/DailyStreakPopup.cs` 的 `FocusCurrentDay` 口径）。')

put('DailyRewardSelector', 'RefreshData(MainMenuMission evt, bool refreshAllRewards)', 'A',
    PUT + ':974（RefreshData）',
    TAG + '**判 A**：原版三支（换事件 ⇒ 重 `Initialize`；有容器 ⇒ 逐条 `Refresh()`；否则 `CreateRewards`）里，'
    '前两支的前半“换事件”恒不成立（我们只有一件日常活动）⇒ 省掉那个形参；后两支按原样落地。'
    '**并且补上了它的调用者**：新增 `DailyRewardPopup.ToFocus()` 覆写（`:235`）= 原版 `ToFocus → RefreshData(evt, true)`，'
    '顺带修掉一个真缺陷 —— 领完奖回到本窗时抽屉状态原来是**旧的**（`RewardStateOf` 只在 `Build` 那一刻读）。')

put('DailyRewardSelector', 'Reset()', 'A',
    PUT + ':957（ResetRewards）',
    TAG + '**判 A**：原版 = 逐条 `Destroy(container.gameObject)` + 清表，已同名落地（口名 `ResetRewards` —— `Reset()` 是 Unity 的 MonoBehaviour 消息名，'
    '占用它会改变引擎行为）。**一处如实差异**：原版第一句 `CustomDebug.LogWarning("[TODO] We have to clear the addressables when closing this window")` '
    '**不复刻** —— 那是原版作者给自己的 TODO（他们走 Addressables、关窗要 Release）；我们走 `Resources` 直取、没有引用计数要放，'
    '照抄会天天刷一条与本工程无关的 TODO。')

put('DailyRewardTrackPanel', 'Initialize(MainMenuMission mission)', 'A',
    PUT + ':1048（InitializeTrackPanel）',
    TAG + '**判 A**：原版的判据形状写成显式实参 `InitializeTrackPanel(holder, purchaseButton, hasPremiumItem, owned, active)`：'
    '`premiumItem == null ⇒ holder 关`；否则 `purchaseButton.SetActive(!HasItem && IsActive())`。'
    '**两处如实差异**：① 原版两个输入本机拿不到 —— `InventoryManager.HasItem(premiumItem)` ↔ `Shell/DailyData.cs:777` 的 '
    '`static readonly _premiumOwned = false`（该文件不在本代理白名单，由 E10 改可写后再读它），`mission.IsActive()` 是远端活动期（单机恒开）'
    '⇒ 调用点先传当前真实取值（`owned:false` / `active:true`），等 `_premiumOwned` 可写后**只改那一个实参**；'
    '② `purchaseButton.Setup(PremiumOffer)` 没有对应物（`IShopOffer` 是远端商店目录）⇒ 价格仍走 `DailyData.PremiumTrackPrice()` 的字面量（🔶 已标）。'
    '另外补了调用者：`DailyRewardPopup.ToFocus()` 里会再调一次（= 原版 `ToFocus → trackSidePanel.Initialize(mission)`）。')

# ---------------------------------------------------------------- ④ 连登窗（2 条）
PUS = 'Shell/DailyStreakPopup.cs'
put('DailyStreakItemContainer', 'SetState(State state)', 'A',
    PUS + ':921（Entry.SetState）',
    TAG + '**判 A（已落同名同流程）**：新增 `DailyStreakPopup.Entry.SetState(State)`（`State { Locked=0, Unlocked=1 }`，值照原版），'
    '逐句对应 `DailyStreakItemContainer__SetState.c`：`highlight` ⇔ Unlocked · `claimLabel` ⇔ Unlocked · '
    '`claimButton.gameObject` ⇔ Unlocked · `claimButton.interactable` ⇔ Unlocked · 末句 `CurrentState = state`。'
    '口径也改成原版的“**建满三件再设态**”（原来是不满足就不建 ⇒ 节点数随状态变、且没有二次设态这条路）。'
    '**两处如实差异**：① 原版 `claimLabel` 与 `claimButton` 是**两颗** GameObject，我们把后两件并成一颗 `Collect`'
    '（我们那张 quad 上挂 `WindowButton`、文字是它的子件）⇒ 两句开关落在同一颗上；'
    '② 原版四层 `if (x != null)` 嵌套、空件就抛，我们逐件判空跳过。')

put('DailyStreakWindow', 'RefreshRewards()', 'A',
    PUS + ':977（RefreshRewards）',
    TAG + '**判 A**：同名落地 = `BuildTrack()`（同样是“先清后建、幂等”）+ 重取 `Current Streak Value` 文本；'
    '并补上它的调用者 `DailyStreakPopup.ToFocus()` 覆写（`:952`，= 原版 `DailyStreakWindow__ToFocus.c` **第一句就是 `RefreshRewards()`**）。'
    '**三处如实差异**：① 原版的 7 格从**当前天**（`ChallengeProgress.collectedRewards`，`+0x10`）起排、格子名用**绝对天号**，'
    '我们把第 1–7 天全列出来（🔶 自建数据 `DailyData.StreakDays` = 7）；'
    '② 原版“放大那一格”= **窗口首格**（`i == collectedRewards`），我们固定在 `i == DailyData.StreakCollected`（只在当前天 = 0 时重合）；'
    '③ 原版每格态 = `milestone.Index <= collectedRewards`（已领的天也还是 Unlocked，只是按钮被按灰），'
    '我们的 `DailyData.StreakRewardUnlocked` 只把“当前那天且未领”算解锁（🔶 自建数据的口径）。'
    '④ 原版末尾那颗 `+0x80` 文本取的是 `progress.currentValue`（`+0x40`），我们取 `DailyData.StreakCurrentValue()`（自建口径）。')


def main():
    b = io.open(MAIN, 'rb').read()
    eol = '\r\n' if (b.count(b'\r\n') and b.count(b'\r\n') == b.count(b'\n')) else '\n'
    lines = b.decode('utf-8-sig').split(eol)
    hdr = lines[0]
    if len(hdr.split('\t')) != 11:
        raise SystemExit('主表表头不是 11 列')

    out = [hdr]
    used = set()
    for k, sets in SET.items():
        cls, method = k
        hits = []
        for i in range(1, len(lines)):
            c = lines[i].split('\t')
            if len(c) != 11:
                continue
            if c[C_CLASS] == cls and c[C_METHOD] == method:
                hits.append((i, c))
        if len(hits) != 1:
            raise SystemExit('定位失败：%s.%s 命中 %d 处' % (cls, method, len(hits)))
        i, c = hits[0]
        used.add(i)
        for col, val in sets.items():
            if col == C_STATE:
                if val not in ('A', 'B', 'C', 'D'):
                    raise SystemExit('状态非法：' + val)
                c[col] = val
            else:
                c[col] = val
        if k in DIFF_APPEND:
            c[C_DIFF] = c[C_DIFF] + '  ' + DIFF_APPEND[k]
        if k in NOTE_APPEND:
            c[C_NOTE] = (c[C_NOTE] + '｜' if c[C_NOTE] else '') + NOTE_APPEND[k]
        c[C_ROUND] = (c[C_ROUND] + '·' + ROUND) if ROUND not in c[C_ROUND] else c[C_ROUND]
        if len(c) != 11:
            raise SystemExit('列数坏了：' + cls + '.' + method)
        if '(' not in c[C_METHOD]:
            raise SystemExit('方法列缺签名：' + c[C_METHOD])
        out.append('\t'.join(c))

    if len(out) - 1 != 16:
        raise SystemExit('补丁行数 = %d（应为 16）' % (len(out) - 1))

    data = eol.join(out) + eol
    io.open(OUT, 'wb').write(data.encode('utf-8'))
    print('WROTE', OUT, len(out) - 1, 'rows')


if __name__ == '__main__':
    main()
