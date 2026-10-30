# AUD08 —— 反向审查「我们多做的」（原版不可达 / 死支 / 恒 null，我们却实现了）

> 只读查判据代理 `B103_R4` · 2026-10-30 · 第一百零三会话 · 波 1
> 配套施工清单：`判据/AUD08_we_do_more.tsv`（逐行：账行 / 原版类 / 方法 / 原版为何不可达（出处）/ 我们做了没（文件:行号）/ 判档 / 建议）
> ⚠️ 本代理**只读**：未改任何 `.cs` / 账 / 正本 / `d:/2`；写盘仅本 `.md` 与配套 `.tsv`。

---

## 〇 一句话结论

- 在 **3,855 条 D 档**里，「原版不可达 / 死代码 / 恒 null / 恒 false」并且**有资产侧证据**（0 实例 / 类名只命中 MonoScript / 独有字段名 0 命中 / 字段恒 null）的严格子集 = **237 条 / 114 个类**。本轮的覆盖 = **这一整个严格子集**。
- 其中**真正「我们多做了」的只有 5 个件**，且**当前全部不影响画面** —— 4 个是我们写了但**没接线**的休眠件（`TipPrebattle` · `BoostTimerPanel` · `EventBadgeController`），1 个接了线但**原版无消费者**（`EnergySpent`）；另有 `TroopsKilledCount` 已接线且我方日任务会推进（与日任务数据 B/C 冲突，交调度台裁定）。
- **没有发现「一进游戏就比原版多画一块」的件**（这是好事，但也说明这类风险主要落在「休眠代码被误接」上）。
- 之前两条已定基线（`ExtraAbilityTraits` / `CurvedLayout`）**复核成立，不动**。
- 🔴 本轮还**排除了 6 个最像 AUD08 的东西**（`YourTurnUI` / `RadioMessageScript` / `CemeteryLog*` / `TraitDescription*` / `UIGroupTint` / `SceneTransitionList`）——它们看着像「原版死了我们还做」，**回源后都不是**。这几条要留在报告里，防止下一轮重查或用错结论去删活功能。

---

## 一 覆盖面（账的边界，别把「查完 237」误当成「查完 D 档」）

| 项 | 数 |
|---|---|
| D 档总数（21 个账文件求和） | **3,855** |
| 本报告覆盖：严格「资产侧死」子集 | **237 条 / 114 类** |
| 其中确证「我们多做了」 | **5 个件**（见 §二） |
| 其中「判不准」 | **2 条**（见 §三） |
| 其中「原版死 + 我们也没做」 | 其余（约 200+ 行，落点=—/无）—— 不是 AUD08 的活 |
| **未覆盖** | 其余 **约 3,618 条** D 档（见下「为什么不查」） |

**未覆盖的 3,618 条为什么不算 AUD08 的候选**（本轮按任务口径**主动排除**）：
1. **P3 服务器类 ≈ 944+ 条**（PlayFab/GPFS/CloudScript/CloudHandle/LiveOps 远端/账户/遥测）—— 任务明确「别把 P3 服务器类算进来」。
2. **纯编辑器工具 ≈ 78 条**（AssetDatabase/构建工具）。
3. **只有 decomp「0 调用点」的 ≈ 500+ 条**（`0 调用/死代码` 桶）—— 这一族是**"0 调用点可能是内联假象"**（`ResolveAction` 巨型 switch 吞体 / 编译器内联），原版**实际会执行**，我方的「等价实现」是复刻活行为，**不是多做了**。本轮抽到的 `PlayAi`/`AddCardToDeck`/`ResolveMissAction`/`hasMaintenanceEffects` 等都属此类，已确认不是 AUD08。
4. **编译器生成物 / 结构样板**（`<>f__AnonymousType*` / `_003C*` 闭包类 / Unity 生命周期样板）—— 无独立观测量。

> 🔴 **下一轮入口**：若要把 AUD08 真正做完，剩余缺口不再在「资产侧死」这一族里（已覆盖），而在**「运行时状态恒不成立」**——即某字段在**某个具体场景/prefab 实例**里恒 null/恒 0/恒 false，而**另一实例**里不是（铁律 5·c「一个值 ≠ 全部情况」）。这需要**逐实例**扫场景/prefab 的序列化值，本轮未做。建议下一轮：`资料/完全复刻/账/*.tsv` 里 `恒 null/恒 false` 且**理由指向「某实例」而非「全类」**的行（约 60+ 条），逐条回 `assets_full` 该实例的字段值核。

---

## 二 确证「我们多做了」（施工建议）

| # | 件 | 我们做了什么 | 原版为何没有 | 当前可见？ | 建议 |
|---|---|---|---|---|---|
| 1 | **TipPrebattle**（账 `战斗界面_HUD:653-662` ×10） | `Battle/TipPrebattle.cs` 近乎逐句移植（战前提示条） | 原版 0 实例（类名只命中 MonoScript_1941；openBgWidth/timeToOpenBg 0 命中） | ❌ 未接线（仅 Editor 自检引） | **标 `⏸ 原版不可达`**，保留不接线，⛔ 不删（账已裁定休眠件） |
| 2 | **BoostTimerPanel**（账 `日常_奖励:19-21` ×3） | `Shell/BoostTimerPanel.cs`（加成倒计时面板，Build/Init/SetDescription/SetTimer） | 原版加成系统整体死：`allBoosts` 恒空 ⇒ `GetBoostItemFromId` 恒 null（账 `商店:27`） | ❌ 未接线（全仓无 Build 调用方） | **标 `⏸ 原版不可达`**，保留不接线 |
| 3 | **EventBadgeController**（账 `日常_奖励:159`） | `Shell/EventBadgeController.cs`（活动角标构建） | 原版 prefab 缺席三否（脚本反查+字段名全量扫+安卓包，B93_D4 现核） | ❌ 仅 Editor 自检引（`ShellScene_B93D4SelfCheck.cs:38`） | **标 `⏸ 原版不可达`**，保持不接运行时 |
| 4 | **EnergySpent**（账 `未分类_其他:277`） | `Shell/EnergySpent.cs` + **已接线** `BattleDriver.cs:38822/38824`（每回合 raise） | 原版 0 实例：`profile_cosmetics.json` 102 条成就里无 EnergySpent challenge | ❌ 无消费者注册 ⇒ 零可见效果 | **在 raise 点标 `⏸ 原版不可达`**（或保留，无害） |
| 5 | **TroopsKilledCount**（账 `战斗逻辑驱动:1357`） | `Shell/TroopsKilledCount.cs` + **已接线** `BattleDriver.cs:38820`（我方 AchievementTally 会推进） | 原版零实例（类名仅命中 MonoScript；0 外部调用点） | ⚠️ 我方本地日任务计数会推进（若日任务含「击杀 N」） | **不动 + 备注**：与「日任务从远端复活时要它」冲突，交调度台按 P3 日任务数据裁定 |

> 共同点：4 个是「**写了、没接线**」的休眠件 —— 逐行回源确认过 `.cs` 里**没有任何运行时调用方**（`grep` 非 Editor 引用为空）。这就解释了为什么**画面不会比原版多**：多出来的代码是「死件」，不是「活件」。
> 真正的风险点在于**将来有人把它们接到运行时**（`TipPrebattle.Build` / `BoostTimerPanel.Build` / `EventBadgeController`）—— 所以建议在**声明处**打 `⏸ 原版不可达`，让接线者一眼看见。

---

## 三 判不准（2 条，回源后定）

1. **`Missions.GetPointsPerArmyOnDailyLogin`**（账 `日常_奖励:514`）—— 我们在 `PurchasePremiumWindow.cs:465` 打了 `'?'` + `:470` 出声；原版读 `MissionsConfig` SO（本地缺）。**缺 SO 时原版这一格显示空 / 0 / 不显示 / '?'，本地判不了** ⇒ 若原版是「没有」，我们的 `'?'` 就是多画了一个字符。建议回 `资源` 或真机核。
2. **`RawCardScript.HasAlternativeArtStyles`**（账 `战斗界面_HUD:795`）—— 已在 P3 交界（依赖 LiveOps 远端配表 `CardsSpecialConfig`）。我们 `CardDetailPopup.cs:1705` 按本地 `alternateArts` 判（本地 7 张异画 AA/HB）。若原版因**配表缺失**而不标异画，我们就多于原版。**随 LiveOps 配表裁定**，暂不删。

---

## 四 复核成立的已定基线（⛔ 别重查）

- **`ExtraAbilityTraits`**：原版整条链死（`B92_B6b` 四源现核）；我们的 `CardDef.ExtraAbilityTraits`（`CardDefExtraTraits.cs:110`）**全仓 0 消费者** ⇒ 今天零行为，`Badges.For` 只吃 `u.Keywords`，**没有**多画徽标。判据 → `判据/B92_战斗判据_B6.md:56/67`「今天零行为」。**已闭合·不做**（⛔ 别把它接进 `Badges.For`）。
- **`CurvedLayout`**：原版 0 实例；我们 `grep CurvedLayout` 全仓 `.cs` **0 命中**。**已闭合**。

---

## 五 排除的「看着像 AUD08，其实不是」（重要：防止误删活功能）

| 件 | 为什么像 | 回源后为什么不是 |
|---|---|---|
| **YourTurnUI**（`战斗逻辑驱动:1368-1370`） | 原版 0 实例 0 调用，而我们 `BattleDriver` 有 `ShowYourTurnHeadsUp`（:14601，调用 :380/:14552） | 「你的回合」抬头源头是**活的另一个类**：`BattleManager._NextTurn_d__395__MoveNext.c:444-491`（`GetTranslation(Battle/HUD/YourTurn)` + `ShowHeadsUpMessage`）。我们复刻的是**活源**，不是死类。⛔ 别据此删抬头。 |
| **RadioMessageScript**（`战斗界面_HUD:596`） | 该类子类无实例，而我们 `TutorialOverlay` 有箭头淡入 | 我们的箭头淡入判据写的是**活** `TutorialTipScript.FadeElements`（`TutorialOverlay.cs:1229-1231`），基类 overlay 本身活。不是复刻死子类。 |
| **CemeteryLogCard/Group/Manager**（`战斗界面_HUD:316-324`） | 三个死类 | 面板本体（活的 `CemeteryLogPanel`）已复刻为 `BattleLogPanel.cs`；**原版死类多出来的「行内 30px 头像 + actionImage 动作图标」已在 2026-09-28 用户拍板删除**。多出来的部分**已销账**，不是待办。 |
| **TraitDefinitionDisplay / TraitDescItem / TraitDescriptionUI**（`卡牌面_渲染:135-158`） | 三死类，看着像「trait 说明窗」 | 我们**没建**这三个类（落点=—）；关键词说明走**活** `CardTraitDescription/*` 词条（`Core/Tooltip.cs:925/1087`）。不是死类复刻。 |
| **UIGroupTint**（`菜单_外壳:763/765/767`） | 3 行 D | **非死类**：同族 764 OnEnable / 766 SetColor / 768 Update **全是 A** 且我方有实现。那 3 行 D 只是「原版空体 / 非生命周期调用」，不是不可达。⛔ 别当死类。 |
| **SceneTransitionList.GetTransition**（`战役:585`） | D | 判据**空**（那张 SO 本地 0 实例），我们**没建**。属「判据空」，留 B/C，不是我们多做。 |

---

## 六 附录 · 顺手发现

1. **「休眠件」是这个工程里 AUD08 的主要形态**：`TipPrebattle` / `BoostTimerPanel` / `EventBadgeController` 都是「原版死 + 我们照抄了一份、但没接线」。这类**不会让画面变多**，但**等于给将来埋了雷**（谁一接就多）。建议调度台统一在**声明处**打 `⏸ 原版不可达（原版 0 实例/死类，<出处>）`，并加一条自检断言「这些件不被运行时 Build」。
2. **`TroopsKilledCount` 与 `EnergySpent` 属「已接线但原版无数据」**：`EnergySpent` 无消费者（无害），`TroopsKilledCount` 会推进我方日任务 —— 这是**唯一有可见风险的一条**，但它取决于「日任务数据是否含击杀类挑战」（P3 远端数据，本地判不了），故列判不准的邻域，交调度台。
3. **账里 `EventBadgeController:159` 的状态列是 D，但备注写「结论维持 B」** —— 状态列与备注自相矛盾，建议调度台订正状态（这条本轮只读，未改）。
4. **`YourTurnUI` 的「等价可见效果 = …」写法容易被误读成「我们多做」**：建议账里这类行显式写「可见源 = 活类 X，非本死类」，否则下轮或验收时会被当成 AUD08 误删。
5. **严格「资产侧死」子集只有 237 条**，但 D 档 3,855 条里绝大多数（P3 + decomp-0 调用）**不是 AUD08 素材**。若用户要「100% 的 AUD08」，下一步应转向**逐实例**的「恒 null/恒 false」核查（见 §一末「下一轮入口」），而不是继续在这 237 条里挖。
