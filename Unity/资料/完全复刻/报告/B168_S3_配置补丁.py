#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B168 · S3 —— 配置补丁草稿（OFF-32 改名/买原画价格 · OFF-35 榜单 key · OFF-48 任务 + 战役点数 · OFF-33 联盟奖杯）

⛔ **本文件不是生成器、不改任何配置**。它是给调度台 / 下一个写手的「照抄粘贴」施工单：
   每一块都是【独立可粘】的语句，粘到 `E:/warpforge/05_工具/server/tools/add_all_configs.py`
   标注的锚点行【之后】即可 —— **不碰既有字面量**（不用数括号、不会把别的条目带坏）。

为什么是「锚点之后追加」而不是「改括号里的字面量」：
   · 这些 dict 在 `:972` 的 `MINIMAL` 里被 `MINIMAL = {...}` 引用，**追加/赋值只要发生在 :972 之前**就会生效；
   · 本文所有锚点都在 :972 之前（见每块头部），所以零风险。

🔴 **落地状态（2026-11-07 S3 已执行，证据见 `B168_S3_改名联盟榜单.md` §七）**：
   · **§1 ⏭ 跳过（不是遗漏）** —— 现网任务奖励走的是 `CurrencyConfig` 里 `code="campaignPoints"`(type=71)
     的 Currency（S1 实现，奖励页 3 条 + 图标 + 300 已实测通过）；再给 `PointsConfig` 加一条 CampaignPoints
     是**重复物**，且会让 `ToCurrency(campaignPoints)` 换对象（顶栏白图标回归风险）⇒ **不加**。
   · **§2 ✅ 已落** —— `add_all_configs.py` 的 `LEADERBOARDS` 已含 LegendPoints / MaxRating / MaxFastRating。
   · **§3 ✅ 已落** —— `ITEMS` 两张价格表已填（`ChangeName` ×1、原画 rarity 1..4，全 `C1`/0）。
   · **§4 ⛔ 不要再粘（仅历史留档）** —— S1 已经给了 **6 条** challenges（`Daily_5/9/10` + 周挑战 + 骷髅 + 登录），
     奖励物是 `campaignPoints` Currency。本文件 §4 是**旧版**（`Daily_Generic_*` + `Campaign Points Neutral`），
     粘上去会把已实测通过的 6 条打回 3 条。
   · **§5 ⚠️ 只落了后半段** —— `maxStoredRefill=3` / `availableRefills=3` 已写进 `MISSION_DAILY_EVENT`
     （落之前现网是 **0**，不是 3）；`challengeProgress` 那段**别粘**（现网 3 条已是 `Daily_5/9/10`）。
   · **§6 ✅ 已落** —— 13 条奖杯（第一版 `reward:null`）。

落地顺序（**历史步骤**，已执行完；再做增量时按上面状态只取还没落的部分）：
   1. 先落 `responses/ExecuteCloudScript.ChangePlayerName.json` / `.BuyOriginalCard.json`
      （已由 S3 建好）—— **模板先于价格**，否则按钮点亮后弹 NotImplemented。
   2. 按 §1 → §2 → §3 → §4 → §5 → §6 把补丁粘进 `add_all_configs.py`。
   3. 空跑：`python -I -X utf8 E:/warpforge/05_工具/server/tools/add_all_configs.py`
   4. 真写：加 `--write`（会先备份成 `*.bak_before_b160`，已有则跳过）。
   5. 验两份 JSON（`responses/ExecuteCloudScript.InitializeLiveOps.json` 与
      `fragments/playfab_profile.json`）能 `json.load`、键值相同、行尾仍是 LF。
   6. ⚠️ **本文件 §1 与 `add_all_configs.py:172-180` 的注释冲突**（那段写着「本次故意不填 CampaignPoints」）
      ⇒ 落地 §1 时**同批把那 8 行注释改写成「已填，见下」**，否则文档自相矛盾（铁律 5）。

判据总表 → `d:/4/Unity/资料/完全复刻/报告/B168_对账_A2_任务联盟榜.md`
          （§二 = OFF-48 · §三 = OFF-32 · §五 = OFF-35 · §六 = OFF-33）

用法：
    python -I -X utf8 B168_S3_配置补丁.py            # 打印全部补丁块
    python -I -X utf8 B168_S3_配置补丁.py --check    # 语法 + JSON 可序列化 + 关键自洽检查
"""

import json
import sys

# ---------------------------------------------------------------------------
# §1  PointsConfig 加 `CampaignPoints`（战役点数 / 顶栏蓝漩涡 / 任务行奖励图标）
#     —— ⏭ **跳过（2026-11-07 实测）：现网奖励走 Currency `campaignPoints`(71)，见文件头「落地状态」**
# ---------------------------------------------------------------------------
# 锚点：add_all_configs.py:188（`POINTS = [...]` 的收尾 `]`）之后
# 原文：
#   POINTS = [
#       {"$type": "ForgePoints, Assembly-CSharp", "cardArmy": 0, "cardRarity": 0},
#       {"$type": "ScoringPoint, Assembly-CSharp", "Id": "OfflineSkulls",
#        "leaderboardKey": "OfflineSkulls", "icon": 0, "cardArmy": 0, "cardRarity": 0},
#   ]
# 判据（出处）：
#   · id = `Type.Name.Replace("Handler","") + " Points " + CardArmy`
#     （`Points<T>.get_Id`，cpp2il out_cpp2il_dev_ilspy/Points.cs:117-129）
#     ⇒ `CampaignPoints : Points<CampaignHandler>` + cardArmy=0(Neutral) ⇒ **"Campaign Points Neutral"**；
#     这个字符串就是 §4 里任务奖励 `{"item": "Campaign Points Neutral"}` 的解析键。
#   · 🔴 两个图标【必须都给】：`CampaignPoints.GetIcon` 在 smallIcon/bigIcon 为空时**直接 throw**
#     （decomp_full/CampaignPoints__GetIcon.c 末尾 FUN_1803f47a0）；而 `ResourcesBarController.GetCounter`
#     会走 `ToCurrency(campaignPoints).GetIcon(Small)` ⇒ 塞一条没图标的 CampaignPoints 会让**顶栏白块回归**
#     （= OFF-02 那个坑，B168_A2 §2·3 / §2·4-①）。
#   · GUID 实证（引用 Sprite 用 `.asset` 的 guid，不是 `.png` 的）：
#     `…/ExportedProject/Assets/40K_genearl_icon_Campaign points.asset.meta`     = 4d7b83a3ad8e6994c8ff4ca56a6ca68a
#     `…/ExportedProject/Assets/40K_genearl_icon_Campaign points_big.asset.meta` = d2b2111cc38ea6542aa8031e1578f84f
#   · `AssetReferenceTyped<Sprite>` 的 JSON 形状 = `{"m_AssetGUID": "…"}`（同 CurrencyConfig 的
#     smallIcon/bigIcon，add_all_configs.py:959-960）—— ⚠️ **非空 guid 的反序列化本工程从没跑过**，需实机确认。
# 🔶 自己设计（非原版）：`maxAmount=2000`（参考图顶栏显示 `300/2000`；服务端数据，判据在远端）·
#    调度台 2026-11-07
PATCH_1_POINTS = r'''
# OFF-48 / OFF-45：战役点数（蓝色漩涡）。id 由 Points<T>.get_Id 算 = "Campaign Points Neutral"
# （CampaignPoints : Points<CampaignHandler>，cardArmy=0 Neutral）——§4 任务奖励的 item 就是按这个 id 找。
# 🔴 两个图标必须都给：CampaignPoints.GetIcon 在图标为空时直接 throw（CampaignPoints__GetIcon.c 末尾），
#    而 ResourcesBarController.GetCounter 会 ToCurrency(campaignPoints).GetIcon(Small) ⇒ 缺图标 = 顶栏白块回归。
#    GUID 来自 40K_genearl_icon_Campaign points.asset.meta / …_big.asset.meta（引用 Sprite 用 .asset 的 guid）。
# 🔶 自己设计（非原版）：maxAmount=2000（参考图 300/2000；服务端数据，判据在远端）· 调度台 2026-11-07
POINTS.append({
    "$type": "CampaignPoints, Assembly-CSharp",
    "cardArmy": 0,
    "cardRarity": 0,
    "smallIcon": {"m_AssetGUID": "4d7b83a3ad8e6994c8ff4ca56a6ca68a"},
    "bigIcon": {"m_AssetGUID": "d2b2111cc38ea6542aa8031e1578f84f"},
    "maxAmount": 2000,
})
'''

# ---------------------------------------------------------------------------
# §2  LeaderboardConfig 补 3 个 key（OFF-35 的前置；不补 = 个人资料页照样 NRE）
#     —— ✅ **已落（2026-11-07）**
# ---------------------------------------------------------------------------
# 锚点：add_all_configs.py:595（`LEADERBOARDS = {...}` 的收尾 `]}`）之后
# 原文：
#   LEADERBOARDS = {"leaderboardDefinitions": [
#       {"$type": "PlayfabLeaderboardDefinition, Assembly-CSharp",
#        "leaderboardKey": "OfflineSkulls", "scoringMode": 0, "preload": False, "entryCountToFetch": 50},
#       {"$type": "RankedV2LeaderboardDefinition, Assembly-CSharp",
#        "leaderboardKey": "OfflineClassic", "scoringMode": 1, "preload": False, "entryCountToFetch": 100},
#   ]}
# 判据（出处）：
#   · 键名是硬判据：`Everguild.LiveOps.IRankedEvent__get_LegendPoints.c` → `GetCachedLeaderboard(0x42A7D58, 0)`，
#     stringliteral.json 在 0x42A7D58 = **"LegendPoints"**；
#     `IRankedEvent__get_MaxRating.c` → **"MaxRating"**(0x42CAB00) / **"MaxFastRating"**(0x42CA700)。
#   · 缺任何一个 ⇒ `GetLeaderboardDefinition` 返回 null ⇒ `LeaderboardManager.GetLeaderboard` 直接抛
#     （`LeaderboardManager__GetLeaderboard.c` 开头 `if (param_2 == 0) goto throw`）——
#     `Latest.log:492-496` 的「打开个人资料页即 NRE」就是这个。
#   · `scoringMode`：`LeaderboardScoringMode.Increment=0 / SetIfHigher=1 / SetValue=2`；这两类都是「取最高」⇒ 1。
#   · `$type` 用哪个子类运行时无差别：`RankedV2/V3LeaderboardDefinition` 全树无 cast，
#     `GetLeaderboard` 只读 `definition.LeaderboardKey(+0x10)`。
#   · ⚠️ **别删 `OfflineSkulls`**：`PointsConfig` 里 `ScoringPoint.leaderboardKey` 指着它。
# 🔶 自己设计（非原版）：`scoringMode` / `entryCountToFetch` · 服务端数据，判据在远端 · 调度台 2026-11-07
PATCH_2_LEADERBOARDS = r'''
# OFF-35：个人资料页 / 排位窗要的三个 key（缺任一 ⇒ GetLeaderboardDefinition 为 null ⇒ GetLeaderboard 抛）。
#   键名硬判据：IRankedEvent__get_LegendPoints.c → "LegendPoints"(0x42A7D58)；
#              IRankedEvent__get_MaxRating.c  → "MaxRating"(0x42CAB00) / "MaxFastRating"(0x42CA700)。
#   scoringMode=1 = SetIfHigher（取最高）；$type 子类运行时无差别（只读 LeaderboardKey）。
#   ⚠️ 别删 OfflineSkulls（PointsConfig 的 ScoringPoint.leaderboardKey 指着它）。
# 🔶 自己设计（非原版）：scoringMode / entryCountToFetch · 服务端数据，判据在远端 · 调度台 2026-11-07
LEADERBOARDS["leaderboardDefinitions"] += [
    {"$type": "PlayfabLeaderboardDefinition, Assembly-CSharp",
     "leaderboardKey": "LegendPoints", "scoringMode": 1, "preload": False, "entryCountToFetch": 50},
    {"$type": "PlayfabLeaderboardDefinition, Assembly-CSharp",
     "leaderboardKey": "MaxRating", "scoringMode": 1, "preload": False, "entryCountToFetch": 50},
    {"$type": "PlayfabLeaderboardDefinition, Assembly-CSharp",
     "leaderboardKey": "MaxFastRating", "scoringMode": 1, "preload": False, "entryCountToFetch": 50},
]
'''

# ---------------------------------------------------------------------------
# §3  ItemsConfig 两张价格表（OFF-32；⚠️ 必须在两个模板落地之后）
#     —— ✅ **已落（2026-11-07）**
# ---------------------------------------------------------------------------
# 锚点：add_all_configs.py:694（`ITEMS = {...}` 的收尾 `}`）之后
# 原文：
#   ITEMS = {
#       "limitedItems": [...],
#       "itemsOrActionPrices": [],
#       "unlockOriginalArtPrices": [],
#   }
# 判据（出处）：
#   · `name` 有硬判据：`ChangeNameWindow.cs:6` `ITEM_ACTION_NAME = "ChangeName"`，
#     消费点 `ChangeNameWindow.cs:122-123`（`ItemsConfig.TryGetItemOrActionPrice(ITEM_ACTION_NAME, …)`）。
#   · `rarity`：`CardRarity.Common=1 / Rare=2 / Epic=3 / Legendary=4`（dump.cs:45686-45696），
#     消费点 `AlternateArtPanel.cs:617-619`（`ItemsConfig.GetOriginalArtPrice(rarity)`）。
#   · 两个 getter 都对 **null** List 抛异常（`ItemsConfig__TryGetItemOrActionPrice.c` /
#     `ItemsConfig__GetOriginalArtPrice.c` 直接枚举那个 List）⇒ 只能给数组，⛔ 不能给 null。
#   · `Price` = `currency`（**裸字符串**，= Currency 的 `code`；本地只有 "C1"）+ `amount`(int)（Price.cs:5-9）。
#   · ⚠️ **顺序**：先落 ChangePlayerName / BuyOriginalCard 两个模板，再给价 —— 只给价会把按钮点亮然后弹
#     `NotImplemented`（B168_A2 §3·4-B 的「顺序」条）。
# 🔶 自己设计（非原版）：`amount=0`（免费）· 服务端数据，判据在远端（关服）· 调度台 2026-11-07
PATCH_3_ITEMS = r'''
# OFF-32：改名 / 买原画价格。amount=0 = 免费（本地经济固定 2000，别真扣）。
#   name 硬判据 = ChangeNameWindow.cs:6 ITEM_ACTION_NAME = "ChangeName"；
#   rarity = CardRarity.Common=1 / Rare=2 / Epic=3 / Legendary=4（dump.cs:45686-45696）。
#   ⚠️ 两个 getter 都对 null List 抛异常 ⇒ 必须给数组（这里给真价，不是 []）。
#   ⚠️ 顺序：先落 responses/ExecuteCloudScript.ChangePlayerName.json / .BuyOriginalCard.json，再给价。
# 🔶 自己设计（非原版）：amount=0 · 服务端数据，判据在远端（关服）· 调度台 2026-11-07
ITEMS["itemsOrActionPrices"] = [
    {"name": "ChangeName", "price": {"currency": "C1", "amount": 0}},
]
ITEMS["unlockOriginalArtPrices"] = [
    {"price": {"currency": "C1", "amount": 0}, "rarity": rarity} for rarity in (1, 2, 3, 4)
]
'''

# ---------------------------------------------------------------------------
# §4  MissionsConfig：3 条日常 challenge + challengeLists（OFF-48）
#     —— ⛔ **不要再粘（旧版）：S1 已给 6 条 `Daily_5/9/10/…`，本段是 `Daily_Generic_*` 旧 id，仅历史留档**
# ---------------------------------------------------------------------------
# 锚点：add_all_configs.py:777（`MISSIONS = {...}` 的收尾 `}`）之后
# 原文：
#   "challenges": [{
#       "$type": "ConstantChallenge, Assembly-CSharp", "id": "Daily_Generic_1",
#       "rewards": [{"difficulty": 1, "targetValue": 1, "rewards": []}], ... }],
#   "challengeLists": [{"id": "OfflineDailyChallenges", "challenges": ["Daily_Generic_1"]}],
# 做法：**整体替换**这两个键（本块就是赋值，天然覆盖旧值；不用去括号里删东西）。
# 判据（出处）：
#   · 条数 = `Persistence.challengeProgress` 的长度（MissionsTab.cs:355-400 的 TryAdd 写在循环体内），
#     但这里必须先注册 3 条 challenge —— `MissionChallengeProvider.GetAssets()` 返回的就是
#     `MissionsConfig.challenges`，而 `MissionChallengeProgress.Challenge` 走
#     `AssetLocator.GetAsset<MissionChallenge>(challengeId)` **直接解引用** ⇒ id 必须能解析。
#   · 参考图三行文字 ↔ 官方本地化词条（`官方本地化_en+zh-CN.json` 的 terms，逐条核过）：
#       Daily_Generic_13 = "Deal {0} damage to the enemy Warlord"（对敌方战将造成 45 伤害）
#       Daily_Generic_8  = "Attack with units {0} times"          （攻击单位 15 次）
#       Daily_Generic_11 = "Attack Ranged {0} times"              （远程攻击 15 次）
#   · 行内图标 + 300 = 里程碑 `rewards[]` 里那一条 `{"item": "Campaign Points Neutral", "quantity": 300}`：
#     `MissionRewardsDisplay__Setup.c` 对每条 RewardInfo Instantiate `MissionRewardItem`，
#     后者 `ItemDrawer.Draw(holder, info.Item, info.Quantity, 10)` ⇒ 图标 = 奖励物的 drawer。
#     `RewardInfo` 的键名带 `[JsonProperty]` = `item` / `quantity`（ItemInfo.cs:8-20），
#     `LoadableReference` 写**裸字符串**（= §1 提供的 id）。
#   · `challengeType=1` = DifficultyBased（按 difficulty 过滤里程碑）；`difficulty=1` = Easy。
#   · ⚠️ `entryId` 是字典键（completedChallenges 那侧），§5 三条必须各不相同。
# 🔶 自己设计（非原版）：目标值 / 奖励数量 / challengeList id · 服务端数据，判据在远端 · 调度台 2026-11-07
PATCH_4_MISSIONS = r'''
# OFF-48：3 条日常（描述词条逐条对上参考图：Missions/Daily_Generic_13 / _8 / _11）。
#   每条里程碑 rewards 里那一条 = 任务行内的「蓝漩涡图标 + 300」；
#   item 字符串 = §1 那条 CampaignPoints 的 id（"Campaign Points Neutral"）。
#   条数上限是 prefab 的 dailyMissionsHolder.maxAmount = 3（Missions Tab.prefab:21158），第 4 条会被丢弃。
# 🔶 自己设计（非原版）：目标值 / 奖励数量 · 服务端数据，判据在远端（关服）· 调度台 2026-11-07
MISSIONS["challenges"] = [
    {"$type": "ConstantChallenge, Assembly-CSharp", "id": "Daily_Generic_13",
     "rewards": [{"difficulty": 1, "targetValue": 45,
                  "rewards": [{"item": "Campaign Points Neutral", "quantity": 300}]}],
     "expansion": 0, "reachMilestoneWhen": 0, "challengeType": 1,
     "cummulative": True, "loopable": False, "loopIndex": 0,
     "allowOnTutorials": False, "allowOnDuels": False,
     "playModeBlocklist": [], "playModeAllowlist": []},
    {"$type": "ConstantChallenge, Assembly-CSharp", "id": "Daily_Generic_8",
     "rewards": [{"difficulty": 1, "targetValue": 15,
                  "rewards": [{"item": "Campaign Points Neutral", "quantity": 300}]}],
     "expansion": 0, "reachMilestoneWhen": 0, "challengeType": 1,
     "cummulative": True, "loopable": False, "loopIndex": 0,
     "allowOnTutorials": False, "allowOnDuels": False,
     "playModeBlocklist": [], "playModeAllowlist": []},
    {"$type": "ConstantChallenge, Assembly-CSharp", "id": "Daily_Generic_11",
     "rewards": [{"difficulty": 1, "targetValue": 15,
                  "rewards": [{"item": "Campaign Points Neutral", "quantity": 300}]}],
     "expansion": 0, "reachMilestoneWhen": 0, "challengeType": 1,
     "cummulative": True, "loopable": False, "loopIndex": 0,
     "allowOnTutorials": False, "allowOnDuels": False,
     "playModeBlocklist": [], "playModeAllowlist": []},
]
MISSIONS["challengeLists"] = [
    {"id": "OfflineDailyChallenges",
     "challenges": ["Daily_Generic_13", "Daily_Generic_8", "Daily_Generic_11"]},
]
'''

# ---------------------------------------------------------------------------
# §5  Missions 事件：3 条进度 + maxStoredRefill / availableRefills = 3（OFF-48）
#     —— ⚠️ **只落了后半段**：`maxStoredRefill=3` / `availableRefills=3` 已写进 `MISSION_DAILY_EVENT`；
#        `challengeProgress` 那段别粘（现网 3 条已是 `Daily_5/9/10`）
# ---------------------------------------------------------------------------
# 锚点：add_all_configs.py:873（`MISSION_DAILY_EVENT = {...}` 的收尾 `}`）之后
# 原文：
#   "maxStoredRefill": 0,          # :834  （Data）
#   "availableRefills": 0,         # :857  （Persistence）
#   "challengeProgress": [{ "entryId": "offline_daily_1", "challengeId": "Daily_Generic_1", ... }],
#                                  # :859-870
# 做法：三句赋值，整体替换 `challengeProgress`（不用去括号里删）。
# 判据（出处）：
#   · 任务卡数量 = `Persistence.challengeProgress` 长度（MissionsTab.cs:355-400）。
#   · 右上「3 可用」= `maxStoredRefill > 0` 且求和 `availableRefills`：
#     `MissionsTab.cs:177-250`（求和）/ `:477-486`（要 `maxStoredRefill > 0` 才计入 label）；
#     文案 `Missions/RefillCount` = "{0} Available" / "{0} 可用"（官方本地化里有）。
#   · `entryId` 是 `completedChallenges` 的字典键 ⇒ **不能重复**（offline_daily_1/2/3）。
# 🔶 自己设计（非原版）：`maxStoredRefill=3` / `availableRefills=3` / 进度初值 0 ·
#    服务端数据，判据在远端（关服）；参考图右上就是「3 可用」· 调度台 2026-11-07
PATCH_5_MISSION_EVENT = r'''
# OFF-48：右上「3 可用」+ 3 条任务卡。
#   maxStoredRefill > 0 才计入（MissionsTab.cs:477-486），availableRefills 会被求和显示（:177-250）。
#   challengeProgress 是【任务卡数量的真正来源】（MissionsTab.InstantiateNormalMissions 的循环体）；
#   entryId 是 completedChallenges 的字典键 ⇒ 三条必须唯一。
# 🔶 自己设计（非原版）：可用数 / 进度初值 · 服务端数据，判据在远端（关服）· 调度台 2026-11-07
MISSION_DAILY_EVENT["Data"]["maxStoredRefill"] = 3
MISSION_DAILY_EVENT["Persistence"]["availableRefills"] = 3
MISSION_DAILY_EVENT["Persistence"]["challengeProgress"] = [
    {"entryId": "offline_daily_1", "challengeId": "Daily_Generic_13",
     "currentValue": 0, "difficulty": 1,
     "canCollect": False, "willEndAt": 0, "completedAt": 0, "startedAt": 0,
     "collectedRewards": 0, "premiumCollectedRewards": 0},
    {"entryId": "offline_daily_2", "challengeId": "Daily_Generic_8",
     "currentValue": 0, "difficulty": 1,
     "canCollect": False, "willEndAt": 0, "completedAt": 0, "startedAt": 0,
     "collectedRewards": 0, "premiumCollectedRewards": 0},
    {"entryId": "offline_daily_3", "challengeId": "Daily_Generic_11",
     "currentValue": 0, "difficulty": 1,
     "canCollect": False, "willEndAt": 0, "completedAt": 0, "startedAt": 0,
     "collectedRewards": 0, "premiumCollectedRewards": 0},
]
'''

# ---------------------------------------------------------------------------
# §6  AlliancesConfig.trophies 补 13 条阵营奖杯（OFF-33；第一版 reward=null）
#     —— ✅ **已落（2026-11-07）**
# ---------------------------------------------------------------------------
# 锚点：add_all_configs.py:640（`ALLIANCES = {...}` 的收尾 `}`）之后
# 原文：`"trophies": [{ "id": "Default_Trophy", ... 一条 ... }]` —— 本块是 `+=`，**默认奖杯保留不动**。
# 判据（出处）：
#   · id / title.mTerm：有判据 —— 官方本地化 `SocialMenu/Alliances/Trophy/Trophy_1..13` 13 条都在。
#   · id ↔ 阵营：🔶 **只有标题语义，没有字段级判据**（软判据）——顺序照标题：
#       1 Honoured Company→UM · 2 Infamous Warband→BL · 3 Savage Clan→Orks · 4 Devouring Swarm→Leviathan
#       5 Conquering Dynasty→Sautekh · 6 Wild Host→Saim-Hann · 7 Blessed Preceptory→Sororitas
#       8 Hunter Cadre→T'au · 9 Ascending Sect→GSC · 10 Decorated Platoon→AM · 11 Sons of the Lion→DA
#       12 Dark Prince's Coterie→EC · 13 Wolves of Fenris→SW
#   · `challenge.$type = GamesPlayed`（全局命名空间，dump.cs:59359-59373）+ `army`：有判据
#     （`AllianceTrophy.IsHidden` 会读 `challenge.Army` 去问 `FeatureConfig.IsArmyHidden`）。
#   · `challenge.id = Daily_XX_1`：13 条阵营词条全在（Missions/Daily_{UM,ORK,ASH,SAU,BL,TL,TAU,SOR,GSC,AM,DA,EC,SW}_1），
#     但「原版用的是不是这个 id」无字段判据（服务端数据）。
#   · `challengeType=2` = MilestonesBased（ChallengeType：DifficultyBased=1 / MilestonesBased=2 / DailyLogin=4）。
#   · `description`：🔴 **Trophy_1..13 没有独立 description 词条**（只有 Default / SM2 / UMRF2 有）
#     ⇒ 借用 Default_Trophy 的描述，**这不是判据、是兜底**，账里要写清。
#   · `reward`：第一版一律 `null`。第二版按 badge SO 的 `cardArmy` 填（**硬判据**）：
#       UM_Forge_Badge(10) / GOFF_Forge_Badge(20) / SH_Forge_Badge(30) / SAU_Forge_Badge(40) /
#       BL_Forge_Badge(50) / LEV_Forge_Badge(60) / TAU_Forge_Badge(70) / SOR_Forge_Badge(80) /
#       GSC_Forge_Badge(90) / AM_Forge_Badge(100) / DA_Forge_Badge(110) / EC_Forge_Badge(120) /
#       SW_Forge_Badge(130)
#     ⚠️ 未验：`AllianceBadgeAssetProvider` 取的可能是「已拥有」列表而不是全量 SO 表 ⇒ 先 null、再实机验。
#   · `rewards[].targetValue` 10/25/50：🔶 **无判据**（服务端数据）。
# 🔶 自己设计（非原版）：id↔阵营映射（软判据）、目标值 10/25/50 · 服务端数据，判据在远端 · 调度台 2026-11-07
PATCH_6_ALLIANCE_TROPHIES = r'''
# OFF-33：13 个阵营奖杯（默认奖杯 Default_Trophy 保留在列表里，本句只追加）。
#   id / title 词条：有判据（SocialMenu/Alliances/Trophy/Trophy_1..13）。
#   🔶 id ↔ 阵营只有【标题语义】（Honoured Company→UM / Infamous Warband→BL / Savage Clan→Orks /
#      Devouring Swarm→Leviathan / Conquering Dynasty→Sautekh / Wild Host→Saim-Hann /
#      Blessed Preceptory→Sororitas / Hunter Cadre→T'au / Ascending Sect→GSC / Decorated Platoon→AM /
#      Sons of the Lion→DA / Dark Prince's Coterie→EC / Wolves of Fenris→SW）——无字段级判据。
#   description 借用 Default_Trophy：Trophy_1..13 本地没有独立描述词条（不是判据，是兜底）。
#   challenge = GamesPlayed（army 有判据；AllianceTrophy.IsHidden 会读 challenge.Army）；
#   challengeType=2 = MilestonesBased；rewards 的 10/25/50 无判据（🔶 自己设计）。
#   reward 第一版 null（badge 能不能解析未验）；第二版按 badge SO 的 cardArmy 填，见本文件 §6 头注释。
ALLIANCES["trophies"] += [{
    "id": "Trophy_%d" % n,
    "title": {"value": {"mTerm": "SocialMenu/Alliances/Trophy/Trophy_%d" % n}, "Args": []},
    "description": {"value": {"mTerm": "SocialMenu/Alliances/Trophies/Default_Trophy/Description"},
                    "Args": []},
    "challenge": {
        "$type": "GamesPlayed, Assembly-CSharp",
        "id": "Daily_%s_1" % code,
        "army": army,
        "onlyRanked": False,
        "anyGameMode": True,
        "anyArmy": False,
        "onlyAccountWins": False,
        "gameModes": [],
        "rewards": [{"difficulty": 1, "targetValue": target, "rewards": []}
                    for target in (10, 25, 50)],
        "expansion": 0,
        "reachMilestoneWhen": 10,
        "challengeType": 2,
        "cummulative": True,
        "loopable": False,
        "loopIndex": 0,
        "allowOnTutorials": False,
        "allowOnDuels": False,
        "playModeBlocklist": [],
        "playModeAllowlist": [],
    },
    "keepPointsOnLeave": True,
    "hideIfNotAchieved": False,
    "dontShowProgress": False,
    "reward": None,
    "itemDescriptionKey": "",
    "itemLoreKey": "",
    "cardRarity": 0,
    "cardArmy": army,
} for (n, army, code) in (
    (1, 10, "UM"), (2, 50, "BL"), (3, 20, "ORK"), (4, 60, "TL"), (5, 40, "SAU"),
    (6, 30, "ASH"), (7, 80, "SOR"), (8, 70, "TAU"), (9, 90, "GSC"),
    (10, 100, "AM"), (11, 110, "DA"), (12, 120, "EC"), (13, 130, "SW"),
)]
'''

# ---------------------------------------------------------------------------
# 注册表（打印 / 自检用；粘贴时按上面每块的头注释找锚点）
# ---------------------------------------------------------------------------
PATCHES = [
    ("§1 PointsConfig += CampaignPoints", "add_all_configs.py:188 之后", PATCH_1_POINTS),
    ("§2 LeaderboardConfig += 3 key", "add_all_configs.py:595 之后", PATCH_2_LEADERBOARDS),
    ("§3 ItemsConfig 两张价格表", "add_all_configs.py:694 之后", PATCH_3_ITEMS),
    ("§4 MissionsConfig 3 条 challenge + list", "add_all_configs.py:777 之后", PATCH_4_MISSIONS),
    ("§5 Missions 事件 3 条进度 + 3 可用", "add_all_configs.py:873 之后", PATCH_5_MISSION_EVENT),
    ("§6 AlliancesConfig += 13 奖杯", "add_all_configs.py:640 之后", PATCH_6_ALLIANCE_TROPHIES),
]


def _check():
    """语法 + JSON 可序列化 + 关键自洽（不读任何外部文件、不改任何东西）。"""
    ns = {
        # 与 add_all_configs.py 锚点处同形的空壳（只为了能 exec 这些补丁语句）
        "POINTS": [],
        "LEADERBOARDS": {"leaderboardDefinitions": []},
        "ITEMS": {},
        "MISSIONS": {},
        "MISSION_DAILY_EVENT": {"Data": {}, "Persistence": {}},
        "ALLIANCES": {"trophies": []},
    }
    for title, anchor, src in PATCHES:
        exec(compile(src, title, "exec"), ns)
    problems = []

    points = ns["POINTS"]
    json.dumps(points, ensure_ascii=False)
    campaign = [p for p in points if p.get("$type", "").startswith("CampaignPoints")]
    if len(campaign) != 1:
        problems.append("§1 期望恰好 1 条 CampaignPoints，实际 %d" % len(campaign))
    else:
        cp = campaign[0]
        for key in ("smallIcon", "bigIcon"):
            if not (cp.get(key) or {}).get("m_AssetGUID"):
                problems.append("§1 %s 的 m_AssetGUID 为空（GetIcon 会 throw）" % key)
        if cp.get("maxAmount") != 2000:
            problems.append("§1 maxAmount 应为 2000")
        # 复刻 Points<T>.get_Id：Type.Name.Replace("Handler","") + " Points " + CardArmy
        # `Type` = 泛型实参 T（CampaignPoints : Points<CampaignHandler> ⇒ "CampaignHandler"），
        # 不是子类名 —— 见 out_cpp2il_dev_ilspy/Points.cs:5,9-29。
        cls = cp["$type"].split(",")[0].split(".")[-1]
        base = cls[:-len("Points")] if cls.endswith("Points") else cls
        handler_name = base + "Handler"
        point_id = handler_name.replace("Handler", "") + " Points " + {0: "Neutral"}.get(
            cp.get("cardArmy", 0), str(cp.get("cardArmy")))
        if point_id != "Campaign Points Neutral":
            problems.append("§1 推出来的 id 不是 'Campaign Points Neutral'：%r" % point_id)

    lb = ns["LEADERBOARDS"]["leaderboardDefinitions"]
    json.dumps(lb, ensure_ascii=False)
    keys = [d.get("leaderboardKey") for d in lb]
    for want in ("LegendPoints", "MaxRating", "MaxFastRating"):
        if want not in keys:
            problems.append("§2 缺 key %s" % want)
    if len(set(keys)) != len(keys):
        problems.append("§2 leaderboardKey 有重复：%s" % keys)
    for d in lb:
        if d.get("preload") is not False:
            problems.append("§2 %s 的 preload 应为 False（B160 §②·4）" % d.get("leaderboardKey"))
        if d.get("scoringMode") not in (0, 1, 2):
            problems.append("§2 %s 的 scoringMode 非法" % d.get("leaderboardKey"))

    items = ns["ITEMS"]
    json.dumps(items, ensure_ascii=False)
    names = [x.get("name") for x in items.get("itemsOrActionPrices", [])]
    if "ChangeName" not in names:
        problems.append("§3 itemsOrActionPrices 缺 ChangeName")
    rarities = sorted(x.get("rarity") for x in items.get("unlockOriginalArtPrices", []))
    if rarities != [1, 2, 3, 4]:
        problems.append("§3 unlockOriginalArtPrices 的 rarity 应为 1..4，实际 %s" % rarities)
    for x in items.get("itemsOrActionPrices", []):
        if (x.get("price") or {}).get("currency") != "C1":
            problems.append("§3 %s 的 currency 必须是本地唯一货币 C1" % x.get("name"))

    missions = ns["MISSIONS"]
    json.dumps(missions, ensure_ascii=False)
    ch_ids = [c.get("id") for c in missions.get("challenges", [])]
    if ch_ids != ["Daily_Generic_13", "Daily_Generic_8", "Daily_Generic_11"]:
        problems.append("§4 challenges id 不对：%s" % ch_ids)
    list_ids = (missions.get("challengeLists") or [{}])[0].get("challenges")
    if list_ids != ch_ids:
        problems.append("§4 challengeLists 与 challenges 不一致")
    for c in missions.get("challenges", []):
        for ms in c.get("rewards", []):
            for rw in ms.get("rewards", []):
                if rw.get("item") != "Campaign Points Neutral":
                    problems.append("§4 %s 的奖励物不是 'Campaign Points Neutral'" % c.get("id"))

    ev = ns["MISSION_DAILY_EVENT"]
    json.dumps(ev, ensure_ascii=False)
    if ev["Data"].get("maxStoredRefill") != 3:
        problems.append("§5 maxStoredRefill 应为 3")
    if ev["Persistence"].get("availableRefills") != 3:
        problems.append("§5 availableRefills 应为 3")
    prog = ev["Persistence"].get("challengeProgress") or []
    if len(prog) != 3:
        problems.append("§5 challengeProgress 应为 3 条，实际 %d" % len(prog))
    entry_ids = [p.get("entryId") for p in prog]
    if len(set(entry_ids)) != len(entry_ids):
        problems.append("§5 entryId 重复：%s" % entry_ids)
    prog_ch = [p.get("challengeId") for p in prog]
    if prog_ch != ch_ids:
        problems.append("§5 challengeProgress 的 challengeId 与 §4 不一致：%s" % prog_ch)

    trophies = ns["ALLIANCES"]["trophies"]
    json.dumps(trophies, ensure_ascii=False)
    if len(trophies) != 13:
        problems.append("§6 应追加 13 条奖杯，实际 %d" % len(trophies))
    tids = [t.get("id") for t in trophies]
    if len(set(tids)) != 13:
        problems.append("§6 trophy id 重复：%s" % tids)
    for t in trophies:
        n = t["id"].split("_")[-1]
        if t["title"]["value"]["mTerm"] != "SocialMenu/Alliances/Trophy/Trophy_" + n:
            problems.append("§6 %s 的 title term 不对" % t["id"])
        ch = t.get("challenge") or {}
        if ch.get("$type") != "GamesPlayed, Assembly-CSharp":
            problems.append("§6 %s 的 challenge 类型不对" % t["id"])
        if ch.get("army") != t.get("cardArmy"):
            problems.append("§6 %s 的 army 与 cardArmy 不一致" % t["id"])
        if ch.get("challengeType") != 2:
            problems.append("§6 %s 的 challengeType 应为 2（MilestonesBased）" % t["id"])
        if t.get("reward") is not None:
            problems.append("§6 %s 第一版 reward 应为 null" % t["id"])
    armies = sorted(t.get("cardArmy") for t in trophies)
    if armies != [10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130]:
        problems.append("§6 13 个 cardArmy 应覆盖 10..130 步进 10，实际 %s" % armies)

    if problems:
        print("✗ 自检失败（%d 条）：" % len(problems))
        for p in problems:
            print("  - " + p)
        return 1
    print("✓ 自检通过：6 块补丁语法可 exec、全部 JSON 可序列化、关键字段自洽。")
    print("  摘要：POINTS=%d（含 CampaignPoints 1）· LEADERBOARDS=%d · "
          "ITEMS 价格 %d/%d · MISSIONS challenges=%d · 进度=%d · 奖杯=%d"
          % (len(points), len(lb),
             len(items.get("itemsOrActionPrices", [])), len(items.get("unlockOriginalArtPrices", [])),
             len(ch_ids), len(prog), len(trophies)))
    return 0


def main(argv):
    if "--check" in argv:
        return _check()
    print(__doc__)
    for title, anchor, src in PATCHES:
        print("\n" + "=" * 78)
        print("# %s    ← 插在 %s" % (title, anchor))
        print("=" * 78)
        print(src.strip("\n"))
    print("\n（粘贴完照本文头部「落地顺序」第 3-5 步跑生成器并验 JSON。）")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
