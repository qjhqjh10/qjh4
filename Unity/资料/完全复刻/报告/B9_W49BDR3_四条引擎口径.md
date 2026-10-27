# B9_W49BDR3 —— `BattleDriver.cs` 四条引擎口径 / 接线修正

> 代理：**B9_W49BDR3** · 2026-10-27 · 白名单文件**只有一个**：`Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`
> （另写本报告 + 账补丁 `账/_parts/B9_W49BDR3.tsv`）。⛔ 未碰 `.unity` / `ProjectSettings/` / 别人的文件 / 正本 / 主表。
> 🔑 **行数守恒**：`BattleDriver.cs` 开工 35,534 行 → 收工 **35,534 行**（所有改动都是**原地替换**，没有一行新增 ⇒ 账里 A 落点**零漂移**）。

---

## 一、结论

四条全部落地，`typecheck 0/0`：

| # | 件 | 结论 |
| --- | --- | --- |
| ① | Duty 徽标读错字段 | ✅ 改读 `!u.ActiveAbilityUsed`（原版 `+0x4c`，**本局一次、不随回合重置**）；原来读 `UsedActiveAbilityThisTurn`（`+0x50` 那族、每回合清零）⇒ **徽标每回合会亮回来** |
| ② | 过期警告的因果链 | ✅ 注释 + 开局警告文案都改了，**零行数变化**；`unitsShake` 那半**已不成立**（数据走 `cardanim_clips.json` 的 `ush` 列，现读 **257 拍非空**；消费链 `ScheduleUnitShakes` → `CardFeel.ResolveUnitShake` → `ResolveUnitShakeNow` ④ 抖动支 → `ShakeUnit`） |
| ③ | 战斗侧异画的卡组层接线（`HUD:92/93` 转 A 的必要条件） | ✅ `CollectAltArtCopies` 加扫 `WarlordId` / `DefensiveId`；4 处裸查 `WarlordId` 全换 `PlayerDeck.SameCardKey(...)`（`WarlordFaction` / 先手 initiative / `AddTierOfDeckWarlord` / `deckWarlord` 日志）。⛔ 没造第二套解析口（全部走 `DeckRules.PlayerDeck` 现成 API） |
| ④ | 敌方展示位终点 | ✅ `RevealEndPx` `(960,520)` → **`(589,495)`** 并删掉该格的 🔶；`RevealStartPx` **保持 🔶**（只把注释改准，值未动） |

⚠️ **③ 只解决「必要条件」那一半**：`HUD:92/93` **状态仍 B** —— 还差「没有任何入口把异画条目放进卡组」（今天只有「导入原版分享串」一条路，收藏窗异画页签归 W16）。

---

## 二、证据（文件:行号）

### ① Duty 徽标

- 原版：`decomp_full/CardTraitDuty__IsActive.c:19` = `return *(char *)(card + 0x4c) == 0;`
- `+0x4C` 的语义 = **本局一次**、不随回合重置：`RuleEngine/Core/UnitState.cs:3188`（`ActiveAbilityUsed` 的 doc：清零三处 = 卡初始化 / 变形回手牌 / 装填 duty；`OnTurnEnd.c:209-210` **只清** `+0x48`/`+0x50`）
- 改点：`BattleDriver.cs:27866`（`KeywordActive` 的 Duty 支）→ `!u.ActiveAbilityUsed`；同一格的另一消费者 `RuleEngine/Core/ConditionEval.cs` 的 `cond=341` 早已是这一位（`B9_W27ENG` 改的）⇒ 两处口径现在一致。

### ② 过期警告

- 抖动链（现读）：`BattleDriver.cs:23863` / `:35399`（两处调用 `ScheduleUnitShakes`）· `:35405`（`ScheduleUnitShakes`）· `:35417/:35419`（`CardFeel.ResolveUnitShake`）· `CardFeel.cs:3906`（`ResolveUnitShake`）→ `:3921`（`ResolveUnitShakeNow`）→ **`:3934` 抖动支调 `ShakeUnit`**（`:3814`）
- 数据：`CardPresentation/Resources/cardanim_clips.json` 的 `ush` 列 —— `rows = 1067`，**非空 257**（`python -I` 现读）
- 改点：`BattleDriver.cs:34754-34759`（doc）+ `:34765-34768`（`WarnMissingPresentationData` 的文案）；`②「未抽到的卡回牌库」` 仍然成立，如实保留。

### ③ 异画卡组层接线

- 判据：`资料/完全复刻/报告/B9_W31ALTART_异画副本进卡组.md` §六·2（改动请求 2）
- API（一处，⛔ 未另写）：`RuleEngine/Core/DeckRules.cs` 的 `PlayerDeck.IsAltArtEntry` / `AltArtBaseIdOf` / `SameCardKey` / `ResolveEntry`
- 改点：
  | 位置 | 改法 |
  | --- | --- |
  | `BattleDriver.cs:34547`（`CollectAltArtCopies`） | 扫描集合 = `CardIds` + **`WarlordId`** + **`DefensiveId`**（`:34550`）。理由：本地 7 张异画**全是督军**、住在 `WarlordId` ⇒ 原来**永远不触发** |
  | `:13032`（`WarlordFaction`） | `DeckLookup(pool)(PlayerDeck.SameCardKey(d.WarlordId))` ⇒ 异画督军查得到 ⇒ **开局阵营不再悄悄退回默认值** |
  | `:5448` / `:5450`（`FirstSeatRules.WarlordInitiative`） | 同上（异画督军能读到 `Initiative` 真值） |
  | `:34649`（`AddTierOfDeckWarlord`） | `AddTierOf(PlayerDeck.SameCardKey(d.WarlordId))` ⇒ `_cardTiers` 的键 = **引擎卡 id**（`TierOfCard` 就是按引擎 id 查的；原来 `#HB_xx` 会成为一个没人问的键） |
  | `:34627`（`deckWarlord` 日志） | `CardDatabase.FindById(_pool, PlayerDeck.SameCardKey(myDeck.WarlordId))` ⇒ 异画督军的本地化名不再打空 |
- 附带：剥不出原卡时的 `LogWarning` 文案从「`CardIds` 里有一条…」改成「卡组里（`CardIds`/督军/防御卡）有一条…」（来源不止一处了）。

### ④ 展示位终点

- 判据：`资料/完全复刻/判据/enemyHand屏幕锚点_判定.md` §一·3 + §二·4 —— `enemyHand` = `UpperAnchor/EnemyArea/HandArea`（`Transform_1295`，`isPlayer=0` 那份 GO 318），屏幕锚点 **(960, 50) @1080p**；原版 `enemyHand.transform.position − right×25 − up×30`（世界 y **−30**）⇒ `×14.8364` ⇒ **(589.1, 495.1)** ⇒ 取整 **(589, 495)**
- 改点：`BattleDriver.cs:9860`（值 + 删 🔶）；`:9859` 仍 🔶（起点 = 世界零，`z=0` 不在 UI 平面上 ⇒ 投屏约 `(28440,540)`，正交 UI 布局**不可表示**）；`:9804-9807` 的那段「仍解不出」注释同步改成「终点已解出、起点仍 🔶」。

---

## 三、没查清的部分（如实，不猜）

1. **画面没验**（本代理不许跑 Unity 批处理 / 真机）：④ 的落点观感、① 的徽标「用过主动能力后永久灰」、③ 的「异画督军立绘真的换成异画」都只做到**代码 + 判据**层面 ⇒ 留给调度台在同步点跑 `BattleScene.Run` / 真机。
2. **非 1080p 档未验**：`RevealEndPx`/`RevealStartPx` 是**设计像素常量**、经 `LayoutSpace.FromPixel` 转世界；本轮的 (589,495) 只在 **1080p（CanvasScaler 参考 1920×1080、`match=0`、`scaleFactor=1`）** 下逐值成立。其它分辨率是否仍等价（我们的 layout 是否整体按 1080p 基准缩放）**没查**。
3. **`②`（未抽到的卡回牌库）** 仍然是缺的，本轮只改文案，**没动**那条路径（`EventTiming` 明写没这一档）。
4. `WarnMissingPresentationData` 末句仍写「引擎事件由别的包做」—— 对 `②` 其实不是引擎事件（是我们没有这条路径），措辞**不够准**；因「零行数变化」要求只做了最小改写，**没再抠**。

---

## 四、改动清单

**代码**（`Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`，行数 35,534 → 35,534）

| 行 | 改动 |
| --- | --- |
| `:27852-27854` | Duty 判据的注释订正（`+0x4C` 是「本局一次」，不是「本回合」） |
| `:27866` | `!u.UsedActiveAbilityThisTurn` → **`!u.ActiveAbilityUsed`** |
| `:34754-34759` | `WarnMissingPresentationData` 的 doc：① 标「已过期」并写清现在的消费链 |
| `:34765-34768` | 警告文案：只报 `②`，并说明 `unitsShake` 已接上（账 `732` 已消） |
| `:34546-34550` | `CollectAltArtCopies` 扫描集合加 `WarlordId` / `DefensiveId` |
| `:34566` | 剥不出原卡时的警告文案（来源不止 `CardIds`） |
| `:13032` | `WarlordFaction` 走 `SameCardKey` |
| `:5448` / `:5450` | 先手 initiative 走 `SameCardKey` |
| `:34627` | `deckWarlord` 日志走 `SameCardKey` |
| `:34649` | `AddTierOfDeckWarlord` 走 `SameCardKey` |
| `:9804-9807` | reveal 注释：终点已解出、起点仍 🔶 |
| `:9856-9860` | `RevealEndPx = (589,495)`（删 🔶）· `RevealStartPx` 保持值与 🔶，注释改准 |

**账补丁**：`账/_parts/B9_W49BDR3.tsv`（**7 行 · 11 列 · 已过 `merge_ledger_parts.py --dry`：更新 7 · 新增 0**；`check_patch_conflicts.py`：**3 份补丁无重复键**）
- `战斗逻辑驱动.tsv` · `EntityScript.set_usedActiveAbility(bool value)`：差异 → 「剩余 0 条」（原来那行明写「Duty 徽标仍读旧位」是残差）
- `未分类_其他.tsv` · `CardTraitDuty.IsActive(CardScript card)`：判据要点 / 落点 / 差异 三格订正为 `!ActiveAbilityUsed`
- `未分类_其他.tsv` · `CardTraitFerocity` / `CardTraitOath`（**顺手**）：落点是**倒序坏行号段** `27797-26867` ⇒ `:27864`（同一 `KeywordActive`）
- `战斗界面_HUD.tsv` · `CardScript.GetOffensiveCardPlayedDisplayPosition()`：**C → A**（锚点已算出 + 已接线），🔶 残差只剩「起点」
- `战斗界面_HUD.tsv` · `BattleCardManager.CheckToBeAlternate(...)` / `CreateCard(...)`（`HUD:92` / `HUD:93`）：落点补扫描范围与 4 处接线；差异改成「只剩 ② 入口」；**状态仍 B**
- 我在**临时副本**上验过闸：`check_ledger_points --all-A` —— 我这 7 行全 **OK**（失败 26→**24**、漂移 404→**402**，全是顺手修掉的两条坏行号）。

**验过**：`TMPDIR=/tmp/wf_w49 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（改完最后一处 `.cs` 后跑的；之后只动了账与报告）· `bash 工具/ruleprobe.sh check` → **rc=0**（全池 1121 张 · 解析差异 **0 行** · 改名 0 · 新增 0 · 消失 0；哨兵三项全 ✓）。本件**没动引擎行为**（只动了表现层读引擎字段 + 卡组解析的调用点）。

---

## 五、顺手发现（只报，不改）

1. 🔴 **`ruleprobe check` 本次无读数**：`工具/ruleprobe/PlayProbe.cs:1940/1943` 引用未定义的 `snaps`（`CS0103` ×4）⇒ `dotnet build rc=1`。那是 **W41 正在写的文件**（`工具/ruleprobe/**` 归它），**不是我的问题**；调度台在同步点重跑即可。
2. **跨补丁协调已就位**（值得记一笔）：`账/_parts/B9_W44HUD.tsv` 的 `CardScript.DisplayOffensiveCardInCenter` 行备注里**主动写明**「第 8 条 `GetOffensiveCardPlayedDisplayPosition` 已由 `B9_W49BDR3.tsv` 判 A ⇒ 本补丁故意不含它」⇒ **两份补丁没有重复键**（`check_patch_conflicts.py` 已复核）。合并顺序不影响结果。
3. **`RuleEngine/Core/UnitState.cs:750-757` 的 doc 已过期**：`UsedActiveAbilityThisTurn` 那段还写着「⚠️ 本字段今天**只服务表现层**：`BattleDriver.cs:27866` 拿它画 Duty 徽标 … **那处应改读 `ActiveAbilityUsed`**」—— 本轮已经改完了 ⇒ 那两句应该删/改写。**该文件不归我**（W27 域），留给人改（纯注释，一行的事）。
4. **`战斗逻辑驱动.tsv:300`（`BattleManager.DisplayRevealedCard`）差异列过期**：「那个『目标旋转』原版没解出来 ⇒ 我们恒 identity」—— `BattleDriver.cs` 现读的注释写的是 **2026-10-24 已解出 = 零向量**（终态等价）。同样是纯文案，**本补丁未含**（避免动别人的行）。
5. `CardPresentation/Resources/cardanim_clips.json` 现读 **`rows = 1067` / `ush` 非空 257**（账里另有两处写「257 行 / 271 条」—— 那是「行 vs 条」两个口径，不是矛盾；引用时注意别混）。
6. `AltArtCopyCount`（`:34575` 的自检口）语义**扩了**：现在也计督军 / 防御卡登记进来的异画。全仓**零调用方**（`grep` 过），但将来谁写断言要知道这一点。
