# -*- coding: utf-8 -*-
# B94_S2 · 日常/奖励线（第九十四会话 · 波 1）账补丁生成器。
# 读主表 -> 只取本代理的 52 行 -> 追加复核注记 / 更正落点 -> 写 账/_parts/B94_S2.tsv
# 规矩：裸 split("\t")、写后回环 11 列校验（同 merge_ledger_parts.py 的消费口径）。
import io

LEDGER = 'D:/4/Unity/资料/完全复刻/账/日常_奖励.tsv'
FLOG   = 'D:/4/Unity/资料/完全复刻/判据/B94_派活.tsv'
OUT    = 'D:/4/Unity/资料/完全复刻/账/_parts/B94_S2.tsv'
HEADER = '域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注'

rows = [l.split('\t') for l in io.open(FLOG, 'rb').read().decode('utf-8-sig').split('\r\n') if l]
mine = [r for r in rows if r[0] == 'S2_日常']
L = io.open(LEDGER, 'rb').read().decode('utf-8-sig').split('\n')
assert L[0].split('\t')[0] == '域', '主表表头不对'

# 行号 -> (备注追加, 落点覆盖 or None, 差异追加 or None, 状态覆盖 or None, 等级覆盖 or None)
NOTE = {}
def N(n, note, landing=None, diff=None, status=None, grade=None):
    NOTE[n] = (note, landing, diff, status, grade)

N(31, 'B94_S2 2026-10-29 复核：`_daily` 硬编码表仍在 `DailyData.cs:65`；三源否定（assets_full / AssetRipper / 远端 catalog.bin）复核成立 ⇒ B（原版值在远端 TitleData），本批无代码改动。')
N(32, 'B94_S2 2026-10-29 复核：同 `:31`（`MissionsConfig` 实例三源 0 命中）⇒ B。')
N(43, 'B94_S2 2026-10-29 复核：落点仍在 `ChallengeManager.cs:273`，签名 `AcceptPracticeChallenge(PracticeChallenge incoming)`（⚠️ 原版形参是 `GameObject source`，如实记差异）；本地半 = 撤挂起 + 建局入口，确认窗/组 `PlayerDuelPopUpData`/等在线仍是 P3 ⇒ B。')
N(45, 'B94_S2 2026-10-29 复核：`CancelDueling(bool)` 仍在 `:182`；三条提示词条键（Il2Cpp 元数据指针）仍未解 ⇒ B。')
N(46, 'B94_S2 2026-10-29 复核：`:137` 在；私聊传输 P3、本地走本进程事件 ⇒ B。')
N(52, 'B94_S2 2026-10-29 复核：`:118` 在；状态置 3 + 本进程事件，私聊 P3 ⇒ B。')
N(53, 'B94_S2 2026-10-29 复核：`:103` 在；`ChatGlobalManager` 两条回调的等价物 = `ChallengeData` 本地队列 ⇒ B。')
N(55, 'B94_S2 2026-10-29 复核：`:291` 在，签名 `(string modeKey, bool playerFirst)`（原版 `(IPlayEvent, bool)`）；服务器匹配 P3 ⇒ B。')
N(94, 'B94_S2 2026-10-29 复核：四跳全在（`WireUp():1012` ③、`BuildTimer` ④ `ToFocus`、`BuildSideBar` ①、`BuildArmyCell` ②）；形式分散（方案 §4.1「同一个文件:行覆盖多个方法 ⇒ 只能 B」）⇒ B。')
N(104, 'B94_S2 2026-10-29 复核：`HandlePurchase():1071` + `HandlePurchaseCompleted():1089` 在；第 ③ 句「买」= 🔶 本地模拟（判据在远端、关服）⇒ 按调度台 2026-10-28 裁定保持 B。｜B94_S2 2026-10-29：**同窗另修一个真缺陷** —— Premium 抽屉曾可无限领（`_rewardClaimed[]` 两轨共用一格），已拆出 `_rewardPremClaimed[]`（判据 `DailyRewardItemContainer__GetCurrentState.c:18-46` 的两格计数），断言在 `Editor/ShellScene_B94S2SelfCheck.cs` ①。')
N(107, 'B94_S2 2026-10-29 复核：`HandlePurchaseCompleted():1089` 在；原版体 = `CurrentChallenges.FirstOrDefault()` 非空则 `canCollect=true`（无谓词，ILSpy 案），本侧由 `PremiumOwned` 直接参与 `RewardStateOf` ⇒ 替代、按裁定 B。｜同 `:104`：Premium 无限领的真缺陷已修（`_rewardPremClaimed[]`）。')
N(159, 'B94_S2 2026-10-29：落点更正 → `Shell/EventBadge.cs:154（ShowNotification）`（旧锚点 :157 是方法内第一句 `if`）。并**就地订正**该方法的过期 doc（原文写「`FUN_180dc57c0` 未解没复刻」—— 它 = `From<float,float,FloatOptions>(t,0f,true,false)`，B93_D4 已解）。剩 punch 基向量 / `m_FillMethod` 两格判据为空 ⇒ B。', landing='B93_D4 2026-10-28：CardPresentation/Shell/EventBadge.cs:154（ShowNotification）+ :181（Tick：8 拍时间轴）+ 文件头常量区')
N(160, 'B94_S2 2026-10-29 复核：`Awake():39` → `EnsureBadges():47` → `BuildBadge():66` 在；prefab 缺席的三重否定（脚本反查 / 字段名全量扫 / 安卓包）成立 ⇒ B。')
N(270, 'B94_S2 2026-10-29 复核：`OnMatchFound():263` 在；残差 = tooltip 组件 + 云脚本 0x456（P3）⇒ B。')
N(272, 'B94_S2 2026-10-29 复核：`:331` 在；`OnlineMatchResult`（`Shell/PlayEvent.cs:288`）仍只有 `success/error/newRating` 三格 ⇒ `SaveGame.armyStats` 与 boosts 两支**无输入**（不是判据缺，是数据缺）；云脚本 0x180 属 P3 ⇒ B。')
N(309, 'B94_S2 2026-10-29：落点规范化 → `Shell/LiveOpsEvent.cs:963（LiveOpsContainers.Load）`；本仓无 Addressables 装载层 ⇒ B（不判 D）。', landing='CardPresentation/Shell/LiveOpsEvent.cs:963（LiveOpsContainers.Load —— 注入口，默认无 provider）')
N(394, 'B94_S2 2026-10-29 复核：`:553` 在；`Signal.Raise` + `Handler Refreshed` 两跳已补；残差 = `Task.Yield` 的条件 + `+0x2d8` 虚槽身份（判据未闭合，⛔ 不猜）⇒ B。')
N(461, 'B94_S2 2026-10-29 复核：同 `:31`（`MissionsConfig.challenges` 远端）⇒ B。')
N(470, 'B94_S2 2026-10-29 复核：落点更正 → `Shell/MissionsTab.cs:610（BuildDailyRow）`（旧锚点 :613 是方法内注释行）。六件在 `BuildDailyRow` 里逐件有判据，但**未拆同名方法**（拆要 `parent`+`rect` 两个建树参数 ⇒ 会成第二棵子树）⇒ B。', landing='CardPresentation/Shell/MissionsTab.cs:610（BuildDailyRow —— 六件仍内联）')
N(474, 'B94_S2 2026-10-29 复核：`MissionsTab.cs:913`（`CollectThenRebuild → OnCollect`）在；本仓无 `MissionEvent` 活动对象层（逐卡点 = 结果等价）⇒ B。')
N(480, 'B94_S2 2026-10-29 复核：`:369` = `public override IUiContainer GetContainer()`（签名已是原版形状）；`UseSmallContainer` 支无装载层 ⇒ 出声 + null ⇒ B。')
N(487, 'B94_S2 2026-10-29 复核：`:387` = `public override void ReleaseContainer()`；无容器/引用计数可释放 ⇒ B（按本批口径不判 D）。')
N(488, 'B94_S2 2026-10-29 复核：`:2713`（`RerollChallenge → RerollDaily`）在，**0 调用方**（真调用点 `Shell/MissionRerollPopup.cs:481` 不在本白名单）⇒ B。')
N(490, 'B94_S2 2026-10-29 复核：`:2719`（`UpdateChallengeProgress → OnBattleEnd`）在，**0 调用方**；⛔ 别让 `BattleDriver` 与它各推一次 ⇒ B。')
N(492, 'B94_S2 2026-10-29：premium 支已解（`FUN_1800021f0(10,…)` = `get_IsPremium`）。**新判据**：原版 `header.tooltip` 的组件就挂在 `info` 节点上（`tooltip` pathID `5316849761747358474` → `bundle_menus_assets_all/GameObject/info_-8160186231220055286.json`）⇒ 我们的 `AddInfoTooltip(inf, "Missions/Info/DailyMissionsHelp")` 是**同一个节点**，但文案不是原版的 `GetLabel(mission, Description)`（每任务描述由远端给）。premium 渐变值已读出（`premiumColor`: key0 `(0.4745,0.3059,0.1529,1)` → key1 同 rgb α `0.098`），但 `MissionProgressView` 无 `IsPremium` ⇒ 两格都无落点 ⇒ B。')
N(508, 'B94_S2 2026-10-29：`DailyData.LoginRewardCellList()` 末行已改调 `MissionRewardsDisplay.Setup(src)`（原来休眠的入口**上线**；内部仍是 `MergeSameItem` 唯一实现、行数守恒）。② `ApplyExtraRewards(mission, null)` 仍无落点（`RewardEventHandler` 未建）⇒ B。', diff='（部分已落 + B93_N4 2026-10-28 补了入口）原版三段：① `DestroyAllChildren(transform)`（**重建**，不是增量刷新）② 奖励源 = `LiveOpsManager.GetHandler<RewardEventHandler>().ApplyExtraRewards(mission, null)` —— ⚠️ **不是** `AvailableRewards()`：那两条（`AvailableRewards()` / `CurrentMilestone`）在 `.c` 里算完**再没被读**（`obj2` 全程未用）③ `GroupBy(r => r.ItemId)` 逐组：`Count==1` 用原对象、`>1` 用 `new RewardInfo(item, Sum(Quantity), Basic, …)`。我们：① = 建树期一次（无 `DestroyAllChildren`）② **无落点**（`RewardEventHandler` 属账 676，落点在 `Shell/DailyData.cs`）③ = `MergeSameItem`（**唯一实现**）。⛔ 不写第二份归并逻辑。｜🔴 **B94_S2 2026-10-29**：`Setup` **已接进生产路径**（`LoginRewardCellList` 转调它），不再是休眠件；残差只剩 ②。')
N(510, 'B94_S2 2026-10-29 复核：`Description():452` / `ExtraIconVisible():460` + `TimerDisplay.UpdateTimer()` 三段全在；`willEndAt==0 && completedAt==0` 支 = `GetNextRefreshTime()`（槽号未坐实、我们不需要）；`Weekly Mission` 的 displayRule 仍无周常行可判 ⇒ B。')
N(515, 'B94_S2 2026-10-29 复核：`PurchasePremiumWindow.cs:470` 打 `?` 是如实（`MissionsConfig` 实例 SO 本地 0 份）⇒ C。')
N(520, 'B94_S2 2026-10-29 复核：`:2732`（`UpdateMissionProgress → Persist:2034`）在；上传半无服务器、`onComplete` 故意不调并出声 ⇒ B。')
N(526, 'B94_S2 2026-10-29 复核：`:2500` 空实现 + 出声；两条理由（远端 `MissionsConfig.configs` 0 条 · `FlexibleGridLayout` 类不存在）仍成立 ⇒ C。')
N(561, 'B94_S2 2026-10-29 复核：`PlayEvent.cs:557` 在；四格 id + `Result=0` 对上；`RankedGame`/`Season` 两跳 getter 未解（VA `0x18042E360 → 0x1800021F0`），现用 `RankedData` 近似并已在代码 🔶 标注 ⇒ B。')
N(612, 'B94_S2 2026-10-29 复核：`RaidEventWindow.cs:1527` + `RaidEvent.cs:1111` 在；执行侧无 `LiveOpsAssetUtility.Unload` 装载层 ⇒ 空跳 ⇒ B。')
N(617, 'B94_S2 2026-10-29 复核：`:1592` + `RaidEvent.cs:1021` 在；`OnDestroy` 只在有事件时出声（本地恒 null ⇒ 零噪声）；无装载层 ⇒ B。')
N(621, 'B94_S2 2026-10-29 复核：`RaidEvent.cs:1158/:1161` 在；**仍 B 的原因不变** —— 宿主 `LiveopMenuContainer` 那一半在 `MainMenuRuntime.cs:3993`（不在本代理白名单）⇒ 未接线 + 本地 0 份在期事件 ⇒ 今天不可达。')
N(622, 'B94_S2 2026-10-29 复核：`:1187` 四支 `InitPlan` 在；宿主未接线 + 无事件数据 ⇒ B。')
N(623, 'B94_S2 2026-10-29 复核：`:1254/:1278` 在（11 句逐句、`next<=0 ⇒ 0` 不除零）；宿主未接线 ⇒ B。')
N(624, 'B94_S2 2026-10-29 复核：`:1218` 在（5 句；`+0x88` iconAssetType 数值未读出 ⇒ 做成入参）；宿主未接线 + 图在远端 ⇒ B。')
N(676, 'B94_S2 2026-10-29 复核：落点更正 → `Shell/DailyData.cs:2298（ShowCollectedWindow(string,int,bool)）`（旧锚点 :2287 是注释行；数组重载在 :2313）。「`showAnimation==false` ⇒ 不开窗、仍走 `RefreshOpenDailyWindows` 回调」已实现；无 caller 传 false、XP 系统本地无 ⇒ B。', landing='CardPresentation/Shell/DailyData.cs:2298（ShowCollectedWindow(string,int,bool)）；数组重载 :2313')
N(681, 'B94_S2 2026-10-29 复核：落点规范化 → `CardPresentation/Shell/Inventory.cs:2058（CollectSingleReward）`；五段里 ④ 联盟奖杯与 ⑤ 末尾 `+0x1b8` 两条虚槽名读不出（判据未闭合，⛔ 不猜）⇒ B。', landing='B93_D4 2026-10-28：CardPresentation/Shell/Inventory.cs:2058（CollectSingleReward）')
N(702, 'B94_S2 2026-10-29 复核：`LiveOpsFilter.cs:561` 在；残差仍 = 泛型方法体未导出（补法：给 `DecompG7.java` 名单加 `SignalEventTrigger` 跑一趟只读泛型导出、⛔ 别覆盖 `decomp_full/`）⇒ B。')
N(704, 'B94_S2 2026-10-29 复核：`:569` 在；残差同 `:702` ⇒ B。')
N(709, 'B94_S2 2026-10-29 复核：`EnergySinglePlayerOnlyEventWindow.cs:1396` 在；`_liveOpReleased` 幂等；无装载层 ⇒ 空跳 ⇒ B。')
N(715, 'B94_S2 2026-10-29 复核：`:1418 TryOpen(object,bool)` + `:298 Open 转调` + `:1148 ApplyTimerVisibility` 在（第 4 句 `RefreshScore` 已补）；activationType 与结束时刻在远端 ⇒ B。')
N(722, 'B94_S2 2026-10-29 复核：`SkirmishEventWindow.cs:599 CollectClicked` 在；领奖在服务器（上屏文案已按铁律 18 标 🔶 自设计）⇒ B。')
N(728, 'B94_S2 2026-10-29 复核：`HasMilestoneToCollect():936` + `MilestoneStates():955` 在；③ 投影用 `collectedRewards`（`.c` = `*(int*)(ev+0x70)`，⛔ 不是 ILSpy 的字面 0）；无 FastMode 事件数据源 ⇒ B。')
N(732, 'B94_S2 2026-10-29 复核：`:66 Awake` 在（行为逐句）；视觉件本地 0 命中、无调用方（缺整条生存事件窗线）⇒ B。')
N(733, 'B94_S2 2026-10-29 复核：`:74 SetEnemyName` 在（那句「关再开」是原版重播动画，⛔ 别优化成一句）；无调用方 ⇒ B。')
N(734, 'B94_S2 2026-10-29 复核：`:82 SetHiddenEnemy` 在（与 `Awake` 同 RVA 折叠）；无调用方 ⇒ B。')
N(745, 'B94_S2 2026-10-29 复核：落点规范化 → `Shell/ToastNotifications.cs:847（InitializeMissionToast）`。🔴 **生产接线缺口已关闭**：`ShowMission(int)` 原来直接 `ShowNotification`、从没人塞 `Data`；现改为按原版形状 `c.Data = new MissionsData{…}` 再走 `InitializeMissionToast`（**行数守恒**，下游账落点零漂移）。残差只剩 `ModifyLocalization`（模板占位符替换引擎，本地无等价物）⇒ B。', landing='B93_D4 2026-10-28：CardPresentation/Shell/ToastNotifications.cs:847（InitializeMissionToast）')
N(771, 'B94_S2 2026-10-29 复核：`ParentEventWindow.cs:1856 CollectClicked` 在；`TryCollect` 体是一句云调用 ⇒ P3；本地只出声说明 ⇒ B。')
N(772, 'B94_S2 2026-10-29 复核：`:1310 BuildRewardDisplay` 在（5 档记分行 + 箱子/骷髅/Collect 钮）；里程碑与 rewards 表在远端 ⇒ 进度条 Fill 不画 ⇒ B。')
N(774, 'B94_S2 2026-10-29 复核：`:1191 BuildOnGoingPage` 在（建树半逐件）；数据半（队伍/队长/比分）远端，注入口 `Teams/ApplyTeamInfo` 在 ⇒ B。')

out = [HEADER]
missing = []
for r in mine:
    n = int(r[3])
    c = L[n-1].split('\t')
    if len(c) != 11:
        missing.append('%d 主表列数 %d' % (n, len(c))); continue
    if c[1] != r[5]:
        missing.append('%d 主表键不符：%s vs %s' % (n, c[1], r[5])); continue
    note, landing, diff, status, grade = NOTE.get(n, ('B94_S2 2026-10-29 复核：未见改动，维持原判。', None, None, None, None))
    if landing: c[6] = landing
    if diff: c[7] = diff
    if status: c[5] = status
    if grade: c[8] = grade
    c[10] = (c[10] + '｜' if c[10] else '') + note
    if 'B94-S2' not in c[9]:
        c[9] = (c[9] + '·B94-S2') if c[9] else 'B94-S2'
    out.append('\t'.join(c))

# 新增行：GetCurrentState（原版四态两轨计数）—— 本批修掉的真缺陷的判据本体
out.append('\t'.join([
    '日常/奖励', 'DailyRewardItemContainer', 'GetCurrentState(int index)',
    'decomp_full/DailyRewardItemContainer__GetCurrentState.c:18-46',
    '四态两轨：`index < 普通轨已领数(+0x10)` ⇒ Collected(2)；否则 `milestone.Target(+0x14) <= 当前值` ⇒ Unlocked(1) / Locked(0)；Premium 未拥有 ⇒ PremiumLocked(3)；否则「base==Collected ∧ Premium 轨已领数(+0x14) <= index」⇒ Unlocked，领过 ⇒ 保持 Collected。',
    'A', 'Shell/DailyData.cs:808（RewardStateOf）',
    '（等价）两轨各一格计数已分开：`_rewardClaimed[]`（普通）与 `_rewardPremClaimed[]`（Premium，🆕 2026-10-29 B94_S2）。🔴 **修掉的真缺陷**：修前两轨共用 `_rewardClaimed[]`，而 premium 支把「普通轨已领」映射成 `Unlocked` ⇒ **Premium 抽屉可无限领**（状态永不前进、每点一次再发一次）。',
    'P1', 'B94-S2',
    'B94_S2 2026-10-29：断言 = `Editor/ShellScene_B94S2SelfCheck.cs` ①（4 条 + 灭自证：改回「两轨共用一格」⇒ 立刻红）。接线在 `Editor/ShellScene.cs` 合计行之前一行。'
]))

# 写前自检：每行 11 列
bad = [i for i, line in enumerate(out) if len(line.split('\t')) != 11]
assert not bad, '列数不对的行：%s' % bad[:5]
data = ('\r\n'.join(out) + '\r\n').encode('utf-8')
io.open(OUT, 'wb').write(data)

# 回环：按消费方口径（裸 split('\t')）读回
back = io.open(OUT, 'rb').read().decode('utf-8-sig').split('\r\n')
back = [x for x in back if x]
print('rows written =', len(back) - 1, '(expect 53)')
print('all 11 cols =', all(len(x.split('\t')) == 11 for x in back))
print('header ok =', back[0] == HEADER)
print('missing =', missing)
