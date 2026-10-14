# C · 远端判据重判（2026-10-14 · 第三十一会话 · 只读）

> 口径（主对话给定）：值能从 `cards_1195_2022时代.json`（38.6 MB · 1195 张 SO）· `官方本地化_en+zh-CN.json`（`terms` 9,441）· `alternateartstyles` 三份本地产物**直接读出** ⇒ 能开工；否则**真缺**并写清缺什么。
> 原件 → `d:/2/远端缓存_旧profile_1013/`。行号 = 2026-10-14 现读。

| 账 | 现在能开工吗 | 判据（文件 + 列/键） | 还缺什么 |
|---|---|---|---|
| **`A1033`** 6 颗聊天钮真键 | ✅ **已解决 ⇒ 应销账**（不属等远端） | ① `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` → `terms` 的 **6 键** `Battle/Chat/{Greetings,Oops,Sorry,Taunt,Threat,WellPlayed}`（en+zh 两列都有值：问候/哎呀/抱歉/嘲讽/威胁/打得好）；② 原版枚举 `ChatMessage` `Greet=5…Oops=10`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ChatMessage.cs`）与之逐值对齐；③ **已接线** → `MyGame/Assets/CardPresentation/Battle/ChatPopupPanel.cs:82-83`（`ChatTerms`）+ `LabelFor()` `:89`（官方词条优先、英文枚举名降兜底并出声） | 无 |
| **`A1762`** 官方卡名/descZh 换真值 | ✅ **能开工**（余 153 张走 join 线） | ① `Card_Name/*` **1065 键 / 1064 非空**（en 与 zh-CN 同非空；124 个 32 位 hex 键）；② `Card_Description/*` **1098 键 / 917 非空**（147 hex）；③ `Card_Abilities/*` 85 键（仅 49 非空）· `Card_Trait/*` 77 · `CardTraitDescription/*` 76（全非空） | 余 **153** 张 join 不上（= 我们自造 slug id，`gen_cards_engine.py:506-508`）⇒ 根因在 `A1782`/`A1800` 那条「官方英文值→键」线，**不是**远端缺 |
| **`A163`** SM 卡容器预制体 | ⛔ **真缺** | 判据 = PlayFab TitleData 的 mission 数据里 `EventComponents` 的 `key=0 (Container)` AssetReference GUID（`LiveOpsAssetUtility.LoadComponent(...,0)`） | 后端已关（`历史/会话_2026-10-11_批次2.md:146`）；三只包都不含 mission 数据 |
| **`A186`+`A376`** 登录卡格数 | ⛔ **真缺** | 判据 = `MissionsConfig`（PlayFab TitleData），`待办判据_1007.md:34` | 同上 |
| **`A897`** 卡 id 真值 | 🟡 **已开工**（部分可做） | `Unity/数据/游戏数据/card_ids.json` 的 **`mapping`**（1058 键）；源 = 快照 `uniqueId` → `cardName`，且 **`uniqueId` 就是游戏卡 id**（现读：1195 张里 **933** 是 `<前缀><数字>`、**259** 是 32 位 GUID、3 空）| 🔴 123 条旧表残留（`AM20`/`DA65` 在真值里不存在）+ 259 张 GUID 卡进不了表 ⇒ `A1800`；**更新的 allcards**（266 张新卡） |
| **`A970`** 逐卡 animInfo | ⛔ **真缺** | — | 要**卡预制体**；卡 SO 的 100+ 键里**没有** animInfo 列表（逐键现读确认）⇒ 三只包都不含 |
| **`A1947`** `hasAlternativeAbility==1` 的卡 | 🟡 **判据到手一半** | 快照 `hasAlternativeAbility`：**1192 张全 0 / 3 空**；`alternativeAbility` 列表 **0 张非空** | 266 张新卡（快照 12 个阵营前缀里无 SW 等） |
| **`A1108`** `dodge` 与 `shield` 并存 | 🟡 **多一条硬否定，仍无解** | 官方 I2 全表 **9,441 条**：键、值里 `dodge` **各 0 命中**；`Card_Trait/*` 无 `dodge`；快照 `CardTraits` 里 `dodge` **0 次**（`shield` 12 次）—— 但 `DefinedTrait.dodge = 240`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:23`）**有定义** | 「为什么并存」仍无解（`dodge=240` 定义在、数据面完全不用） |
| **`A1748` 尾巴**（`SW13`/`SW31` 要 `ferocity`） | ✅ **能开工 —— 但挂起理由要改** | ① `DefinedTrait.ferocity = 1265`（`…/DefinedTrait.cs:130`）· `dontReturnFerocity=1270`；② `AbilityTrigger.Ferocity=725` / `TriggeredFerocity=726`（`…/AbilityTrigger.cs:97-98`）；③ `CardTraitFerocity` 类 · `CardTraitCollection.ferocitySprite`（`…/CardTraitCollection.cs:24`）；④ 我们自己的 `cards_engine.json` 里 `SW13`/`SW31` 的 **`desc` 头就是 `Ferocity:`** | ⚠️ **不是「原版判据为空」**：快照 `CardTraits` 里 `ferocity` 0 次的**真因 = 该快照整个 SpaceWolves（0 张）都不在内**（12 个前缀无 SW；最大 `_trait`=1255 `cruelty` < 1265）⇒ 缺的是**更新的 allcards**（与 `A897`/`A970` 同一笔） |
| **`A985⑤`** `BattleEndPlayerData` 三格 | ⛔ **真缺** | — | 宿主 prefab 本地零实例；三只包都不含 |
| **`A1070③`** 督军 `goSecondCardInHand` | ✅ **已做完 ⇒ 从 §29·d 搬走** | 自陈「✅✅ 已接线、已验」（`项目任务.md:668`）；产物 `数据/游戏数据/warlord_goes_second.json` | 无 |
| **「等真 Play」族** `A577`/`A586`/`A914`/`A860 的续`/`A962③`/`A367`（`:613`）· `A939`（`:618`） | ✅ **不该算「等远端」**（§29·c 可留，但要重标为「**可开工**」） | 判据 = `资料/真Play待验清单.md`（`D41`/`D42`）+ `工具/wf_click.py` | 无远端依赖。🔴 **2026-10-11 用户已取消「真 Play 攒着做」** ⇒ 现口径 = 进游戏 → 截图 → 点击（`CLAUDE.md` 铁律 12 第十七会话） |
| `extrinsic` 真旗标（`:615`）· `ClockManager.SetEndTurnText` 的 `bool`（`:616`） | ⛔ **保持挂起**（**不是**等远端） | 前者等输入、后者判别式在方法体里但调用点全库 0 | 与三只包无关，**别搬进 §29·d** |

## 一、与两张正本说法不一致的地方

1. 🔴 **§29·d `A1033`（`项目任务.md:652`）整行已过期**：它写「6 颗聊天钮的真键**【没查到】**⇒ 归等远端」，真键 = `Battle/Chat/{Greetings,Oops,Sorry,Taunt,Threat,WellPlayed}`，**而且早已接线**（`ChatPopupPanel.cs:82-83`，账 `A1845`/`A1804` · 2026-10-14）。⚠️ 它还点名去搜 `Chat_Messages/warcry-*` —— **那是另一件东西**（督军台词，见下），不是钮标签。
2. 🔴 **§29·d `A897`·`A970`（`:654`）的「现状」段已过期**：原文写「`card_ids.json` 的 `id` 列**不是游戏卡 id**…真值表只采到 **~4.3%（48/1126）**」—— 该表 **2026-10-13（第二十六会话）已整表重建**（`card_ids.json` 的 `note` 自陈：真值覆盖 935 · 推翻 853 全替换 · 保留 123）。
3. 🔴 **§29·c `A1748` 尾巴（`任务_卡组卡牌战斗.md:56`）的挂起理由不准确**：写「真值快照 `CardTraits` 里 `ferocity` 出现 0 次 ⇒ **原版判据为空**（铁律 11 例外①）」。0 次是**快照不含 SW 整族**造成的**退化否定**，且 `ferocity` 的枚举/类/触发器/图标**本地全有**。⇒ 建议改挂「**等更新的 allcards**」（与 `A897` 同一笔）。
4. **§29·c `A1108`（`:619`）** 列的三条否定（`decomp_full` / `all_strings.txt` / `zh_CN.csv` / `trait_textsprites.json`）**都对**，但**漏了最强的一条**：官方 I2 全表 9,441 条的**键与值里 `dodge` 各 0**，而同族的 `shield` 有 `Card_Trait/shield` + `CardTraitDescription/shield`。
5. **§29·d 抬头（`:622-632`）要重写**：它自陈「等的是 `allcards` 与 I2」—— 两只**都已到手**；本节真正还缺的**两项都与那两只包无关**（TitleData/MissionConfig 与**卡预制体**）。
6. **`A1947` 与 `hAA` 在两张正本里 0 命中**（只在 `交接_第三十一会话.md:104` 被顺带提到）—— 与 `普查产出_第三十一会话/A_29b状态盘.md:54` 记的一致。

## 二、顺手发现

1. **官方 `Chat_Messages/*` 共 1548 键**（首个大发现）：形态 = `<卡id>-Chat<N>` **748** · 裸 `<卡id>` **522** · `<卡id>-Chat<N>-<敌id>` **165** · `<卡id>-vs<名>` **18** · `-OUT` 3 · hex 键 2 · 杂项 10。**是原版逐卡的登场/对位台词真值**（例 `Chat_Messages/AM1-Chat0` = "Arise, warriors of the Imperium!…"）。⚠️ `stringliteral.json` 里的 `"Chat_Messages/warcry-default"` **在官方表里查不到**（`warcry` 0 命中）⇒ 疑似运行时拼 `<id>`，**未查清**。
2. 快照 `keyword` 字段是 **`Keyword` 枚举 = 兵种**（`none/Infantry=5/Vehicle=10/Monster=15/Building=20/Battlesuit=25/Drone=30/Beast=35/Daemon=40`，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Keyword.cs`）—— ⛔ **不是**关键词位掩码（我先按位掩码试过，是错的）。
3. `cardShortId` 是**数字**（非 `AM1` 形）；真正 `<前缀><数字>` 形的是 **`uniqueId`**（933 条）。另：`Card_Name/*` 有 124 个 hex 键、`Card_Description/*` 有 147 个 ⇒ 与「259 张 GUID 卡」同一来源。
4. 快照 **阵营前缀 12 个**（AM/ASH/BL/DA/EC/GOF/GSC/SAU/SOR/TAU/TL/UM）· `CardTraits` 最大 `_trait` = **1255 `cruelty`** ⇒ `huntMark`(1260)/`ferocity`(1265)/`oath`(1275) **全在快照之外**。
5. `DefinedTrait` 全表（129 值）已在本地：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs` —— 是「哪个 trait id = 哪个词」的**权威词典**（`trait词典_原版真值.tsv` 我未逐条比对）。
6. `Card_Abilities/*` 85 键里 **36 条值为空**（49 非空）—— 空值键是原版自己留的坑，别当「有文案」。
7. `A1759` 那条线的 `D7`（`卡不在池 316`）与 `A1880`/`A970`/`A1800` 同源，均已在本表第 5 行覆盖。
