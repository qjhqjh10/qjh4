# A2 · 战斗/VFX 五件（`D4` · `UM37`/`UM84` · `LoadForTest` · `mulliganEnabled` 录像头 · `EvtKind` 登记表）

> 执行代理 **A2** · 2026-10-24 · 有写权限（✅ 未跑 Unity、未动 git、未改任何正本 / 任务总表）
> 白名单内改动；`Net/**` / `Hand/**` / `Shell/**` / `RuleEngine/**` / `CardView.cs` **一个字节都没动**。
> 判据来源：`d:/2/tools/decomp_full/`（第一权威）· `资料/普查产出_1023e/`（A21 / J1 / J2 / F2 / C5 索引）
> · 运行期表 `CardPresentation/Resources/card_vfx_by_card.json` + `trait_effect_bindings.json`
> + `RuleEngine/Resources/cards_engine.json`（**全部现读，⛔ 没有一条数是从报告里抄的**）。

## 〇 · 一句话

五件**全部落地**；`typecheck` = **运行时 0 / 编辑器 0**；**21 条新断言**（D4 4 · UM37/UM84/SW18 3 ·
`LoadForTest` 3 · mulligan 6 · `EvtKind` 5）全部写成「改坏就红」，期望值有一条独立 Python 预演（见 §六）。`mulliganEnabled` 的落点**不在 `MsgStart`**（那在 `Net/NetProtocol.cs`、黑名单）
⇒ 按 `tutorialStage` / `botSeatPlus2` 的**同一套哨兵纪律**加在录像自己的头 `ReplayRecord` 上（白名单内）。

---

## 一 · `D4`：`trigger` 桶按拍分件（Start(0) 与 Trigger(3) 不再混播）

### 1·1 判据（逐句读全量反编译）

| 环节 | 判据 |
|---|---|
| 触发那一拍 | `CardScript__ActivateTriggerTraitAnim.c` → `BattleCardUI__DisplayTriggerAnim` → **`BattleCardUI__DisplayTriggerAnim.c:71`** 只处理 `TraitEffect.TriggerType ∈ {3, 6}` |
| 召唤 / 附魔那一刻 | `CardScript__ActivateTraitsOnSummonOrEnchantment.c` → **`BattleCardUI__DisplayStatusAnim.c:86-119`** 才处理 `Start(0)`（另有 `OnPlay(2)`） |

⇒ `trigger` 桶里 `TriggerType = 0/1/2/5` 的那几行**不该在触发那一拍播**。

### 1·2 现读形状（2026-10-24，运行期表 × 绑定表逐条算）

`trigger` 桶 **527 行**：`Start(0)` **258 行** · `Trigger(3)` **223 行** · `TriggerOnPlay(6)` **10 行** · 卡级行 **36 行**。
`≥2 行且混着 Start(0)` 的卡 **46 张 / 103 行**（其中 47 行是 Start(0)）。
⚠️ 任务简报里的「88 / 86 / 34 张」是**更早那一版数据的口径**；以上是现读。

### 1·3 改法（消费口，⛔ 不改数据）

- `Core/TraitFrames.cs`：新增只读反查口 **`TraitFrames.TriggerTypeOf(cardAnim, prefab)`**
  （两个键都建；两处都没有 ⇒ `-1` = 不是 trait 行）。数据源 = 已载入的
  `Resources/trait_effect_bindings.json`（= 原版 `CardTrait` SO）——**没有在 C# 里新写一张 trait→类型表**。
  现读 67 个不同名字，**0 个**同时对应两种 `TriggerType`（冲突会覆盖，doc 已写明）。
- `Core/VfxMap.cs`：`IsNonTriggerBeatTraitRow` + `TriggerBeatRows`；在 `TryResolveTriggerPicks`
  的**两条整桶兜底**（第 ② 档：这张卡没有 `animTimes`；第 ④ 档：关键词不在白名单 / 判不出目标）
  换成 `picks = TriggerBeatRows(trig)`。全被滤掉 ⇒ 回 false（`perCardOnly` 那一层回空数组），
  ⛔ **不再掉回整桶**。
- **只动这两处**：`Resolve` / `ResolveAll` / `ResolvePerCard` 是**原始查询口**（`TryResolvePerCardPicks`），
  故意不动（它们的 doc 就写着「原始查询」，且 Trigger 没有生产消费者）。

### 1·4 断言（`Editor/BattleScene.cs`，J1 那一段末尾）

| 用例 | 期望 | 灭自证 |
|---|---|---|
| `AM27 Nork Deddog`（无 `animTimes` ⇒ 第 ② 档） | `[TalentEffect]`（Trigger(3)） | 删掉过滤 ⇒ `ArmourEffect`（Start(0)）跟着播 ⇒ 2 条 ⇒ 红 |
| `GOF26 Stompa` + 关键词 `unstable`（第 ④ 档） | `[ConcussiveEffect]` | 整桶 ⇒ 3 条 ⇒ 红 |
| `DA10 Black Knight`（**卡级行不许误伤**） | `[Trait_GainQuest]` | 把过滤写成「白名单外一律丢」⇒ 卡级行也没了 ⇒ 红 |
| `AM15 Minka Lesk`（正例，桶里全 Trigger(3)） | `[TalentEffect]` | 写成「整桶丢弃」⇒ 红 |

---

## 二 · `UM37` / `UM84`：分类键不再「只取第一条」

### 2·1 判据与现状

同一个 `CardAnim` 在**两条不同 `CardAbility`** 里各出现一次、`TriggerName` 不同（现读 **3 行**）：

| 卡 | `CardAnim` | 两条 occurrence（`Order` / `tn`） |
|---|---|---|
| `UM37 Death from Above` | `BulletTrail_Stormraven_SingleTarget` | `0.0.2 ThisCardPlayed` ↔ `1.0.0 Codex` |
| `UM84 Devastator Doctrine` | `Buff_Purple_target` | `0.0.1 ThisCardPlayed` ↔ `1.0.1 Codex` |
| `SW18 Hrolf the Ironhowl` | `Card_Buff_Hand_SW` | `0.0.1 WhileInPlay` ↔ `1.0.0 ThisCardPlayed` |

原 `TriggerNameOf` **只取第一条同名行** ⇒ 第二条那个 trigger **结构上取不到**（`UM37`/`UM84` 的 `Codex` 播不出来）。

### 2·2 改法

- **`TriggerNameOf` → `AddTriggerNamesOf`**：一条候选返回**它可能属于的全部** trigger 名（去重；
  `A` 非空只认 `A` 的规则原样保留）。
- 归口判定换成 **`CandidateHasTrigger(times, p, target)`**（命中任意一个名字即算）。
- 新增 **`TrimToLabelledRowCount`**：同一个 `CardAnim` 在多条 `CardAbility` 里时，候选可能「都命中」这个 target
  ⇒ 按 `times` 里**标到这一拍的行数**截断（先来先得）+ 出声。**现读一次都不触发**
  （3 行歧义里另一条 occurrence 都落在 `play` 桶，归口只看 `trigger`+`ability`）。
- **自检不再跳过**：删掉 `AmbiguousTriggerLabel` 与 `SelfCheckRoutedTriggers` 里的跳过 + `ambig` 那一套，
  这 3 行**逐条验**。

> ⚠️ **如实标（与简报措辞的差别）**：简报写「分类键带 `Order`」。运行期表**没有 `Order` 列**
> （`es[]` 只有 `n/c/a/rc/ai/gj/gn`），本项目白名单也**不含**生成器 / Resources JSON ⇒ 不能按 `Order` 直接 join。
> 实际做法 = **多值分类键 + 用 `times` 里标到该 trigger 的行数对齐**（`times` 本身按 `Order` 排序），
> 达成的结果一致：**同一 `CardAnim` 的第二条 trigger 取得到**。

### 2·3 断言

| 用例 | 期望 | 灭自证 |
|---|---|---|
| `UM37` + `codex` | `[BulletTrail_Stormraven_Single]` | 改回「只取第一条」⇒ 0 条 ⇒ 红 |
| `UM84` + `codex` | `[CodexEffect, Buff_Purple]`（两条） | 改回「只取第一条」⇒ `Buff_Purple` 丢 ⇒ 1 条 ⇒ 红 |
| `SW18` + `WhileInPlay`（正例） | `[Buff_Blue_SW, Card_Buff_Hand_SW]` | 切过头 ⇒ 红 |

**件级白名单自检**（`SelfCheckRoutedTriggers`，14 个名字全池逐卡）在**新的多值键**下预演过 **0 违规**
（Python 独立复现，见 §六）。

---

## 三 · `VfxMap.LoadForTest(json)`（特效线 `新账-72⑥`）

### 3·1 为什么

`EnsureLoaded` 有一条「**非权威档之间平票只留第一条**」（`if (!authoritative && picks.Count > 1) picks.RemoveRange(1, …)`）。
而现读**全表非权威平票行 = 0** ⇒ 用真表写的断言**只能恒绿**（自证）。

### 3·2 改法

- 把 `EnsureLoaded` 的装载体抽成 **`LoadTableFromJson(dto, sourceLabel, allowNonAuthoritativeInferredTie)`**
  （生产与自检**共用同一段**，⛔ 没有第二份过滤逻辑）；`EnsureLoaded` 只负责「读 Resources + 解析」。
- 新增 **`LoadForTest(json, allowNonAuthoritativeInferredTie = false)`** / **`ResetCardTableForTest`** /
  **`ReloadRealCardTableForTest`**（✅ 生产路径零影响：`EnsureLoaded` 恒传 `false`，且它是唯一生产入口）。
- `allowNonAuthoritativeInferredTie` 那一档的必要性**如实记**：2026-10-23（`T1`）在更前面还有一道闸
  「非 `authoritative` 一律不进表」⇒ 今天喂两条 `inferred` 会**一件都留不下**，那条「只留第一条」规则
  **结构上摸不到**。⇒ 自检显式打开这一档（= 退回 2026-10-23 之前的形状）**只为观测那条规则**。

### 3·3 断言

合成表 = `confPriority[authoritative, inferred-name, inferred-keyword]` +
`FIX_A2/ability` 两条**同档 `inferred-name`**（`First_Inferred` / `Second_Inferred`）。

- **(a) 生产口径**：`LoadForTest(json)`（默认）⇒ **0 件** 且 `CardTableNonAuthoritativeRows == 2`。
  🧨 删掉第二道闸 ⇒ 2 件 ⇒ 红。
- **(b) 自检档**：`LoadForTest(json, true)` ⇒ **1 件 = `First_Inferred`**（只留第一条）。
  🧨 删掉 `RemoveRange(1, …)` ⇒ 2 件 ⇒ 红。
- **收尾**：`ReloadRealCardTableForTest()` + 断「真表已还原」（`CardTableCards > 100 && CardTableNames > 100`）。

---

## 四 · `mulliganEnabled` 进录像头

### 4·1 落点（⚠️ 与简报的「停手条件」不同 —— 说明理由）

`MsgStart` 在 **`CardPresentation/Net/NetProtocol.cs:69`**（黑名单）。
⇒ 按简报的兜底要求**没有动它**；改走**录像自己的头** `ReplayRecord`（`Battle/ReplayStore.cs`，白名单内）——
与 `tutorialStage` / `botSeatPlus2` **完全同一套哨兵纪律**（它们也是「不属于 `MsgStart`、放在录像头里」）。

### 4·2 改法

| 文件 | 改动 |
|---|---|
| `Battle/ReplayStore.cs` | 新字段 **`int mulliganPlus1`**（`0` = 没记 · `1` = false · `2` = true）+ 唯一读法 **`MulliganEnabledForReplay`**（`bool?`，没记 ⇒ `null`，⛔ 不是 `false`）。`version` **不 +1**（纯新增，老录像读出 `0` = 退回老行为） |
| `Battle/BattleDriver.cs` `RecBegin` | 写 `mulliganPlus1 = mulliganEnabled ? 2 : 1` |
| `Battle/BattleDriver.cs` `PlayReplay` | 进 `BeginFromPendingCore` **之前**按头恢复 `mulliganEnabled`；`finally` **还原**（别把回放的档留给下一局）；`MulliganEnabledForReplay == null`（老录像）⇒ 用 driver 当时的字段值（**逐字不变**） |
| 自检口 | `ReplayMulliganPlus1AppliedForTest`（头里读到的原值）+ `MulliganSeenByBeginForTest`（`Begin` 真正喂给 `RuleCore.NewBattle` 的那一位） |

### 4·3 断言（放在「放刚打完那一局」那一段）

1. 哨兵：`new ReplayRecord().MulliganEnabledForReplay == null`；`1 ⇒ false`；`2 ⇒ true`。
   🧨 改成 `bool` 字段 ⇒ 「没记」与「真的 false」混 ⇒ 红。
2. **这一局真打出来的录像头记了这一格**（`mulliganPlus1 ∈ {1,2}`）。🧨 删掉 `RecBegin` 那句 ⇒ 0 ⇒ 红。
3. `PlayReplay` **用的是录像头那一格**（`AppliedForTest == header`）。
   🧨 不读这一格、只靠 driver 当时字段（夹具默认 `false`）⇒ 红。
4. 它**真的传到了 `RuleCore.NewBattle(openMulligan:)`**（`SeenByBeginForTest == header`）。
   🧨 只在驱动字段上写一下、没在 `BeginFromPendingCore` **之前**恢复 ⇒ 红。

---

## 五 · `EvtKind` 三处 `default` 不再替新事件兜住（`J2` 顺手发现 3）

### 5·1 改法（`Core/EventTiming.cs`）

- 新增**登记表**：`ZeroDurationKinds`（9 档，逐档写清为什么是 0）· `TimedKinds`（9 档，`DurationOf` 里有 `case`）。
  现读 18 档**全部登记**（`EvtKind` 现读 18 个值，含 `ShowCardsWindow`）。
- 三处 `default:` 改为：
  · `DurationOf(EvtKind)`：零时长档 ⇒ `0f`（静默）；**未登记** ⇒ 出声 + 计数，仍回 `0f`；
  · `IsSourced(kind, out why)`：零时长档 ⇒ `true`（与改前逐字一致）；**未登记** ⇒ `false` + 原因 + 出声；
  · `SourceOf`：零时长档 ⇒ `"（无）"`（逐字一致）；**未登记** ⇒ 出声 + 明写「未登记」。
- **`ShowCardsWindow` 显式登记成零时长**（原版那一拍**非阻塞**，`FinishResolvingAction` 紧跟、无 `yield`）
  ⇒ 不会变成噪音。
- 新自检口：`UnregisteredKindHits` / `ResetUnregisteredKindForTest` / `IsRegisteredEventKind` / `IsZeroDurationKind`。

### 5·2 断言

1. **现有 18 档全部登记**（遍历 `Enum.GetValues`）。🧨 加一档不登记 ⇒ 红。
   —— 这也让 `BattleScene.cs` 那条 `unsourced == 2` **继续成立**（现有档一个都没变），
   而**将来**加一档未登记 ⇒ `unsourced` 变 3 ⇒ 那条也红（这就是 J2 要的「会拦住他」）。
2. **`(EvtKind)999`**：三处都不再静默 —— `!IsSourced` 且带原因 · `DurationOf` 仍 0 **但出声** ·
   `SourceOf` 含「未登记」· 计数上涨。🧨 三处改回 `return true; / 0f; / "（无）"` ⇒ 红。
3. `ShowCardsWindow` 读数**逐字不变**（`IsSourced = true` · `DurationOf = 0` · `SourceOf = "（无）"`）。
4. 抽查三格已登记**有时长**的事件读数不变（`Hit` / `Trigger` / `Heal` 与各自常量相等）。
   🧨 登记表把有时长的档漏进零时长那一张 ⇒ 红。

---

## 六 · 独立预演（Python，读同一份 JSON，不复用 C# 实现）

按 `EnsureLoaded` 的同一套 `confPriority` 算法 + 新的多值分类键 + `TriggerBeatRows`，在现读 JSON 上逐卡算：

```
selftest bad: 0                 # 14 个白名单名字的全池件级自检（含 UM37/UM84/SW18 那 3 行）
AM27 None -> ['TalentEffect']
GOF26 unstable -> ['ConcussiveEffect']
DA10 None -> ['Trait_GainQuest']
AM15 None -> ['TalentEffect']
UM37 codex -> ['BulletTrail_Stormraven_Single']
UM84 codex -> ['CodexEffect', 'Buff_Purple']
SW18 WhileInPlay -> ['Buff_Blue_SW', 'Card_Buff_Hand_SW']
```

⇒ 上面 §一/§二 的 7 条期望值与实际算法一致；且**新的件级自检（不跳过歧义行）0 违规**。

---

## 七 · 改动清单

| 文件 | 性质 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs` | 改（+`IsNonTriggerBeatTraitRow` / `TriggerBeatRows` · `TriggerNameOf`→`AddTriggerNamesOf` + `CandidateHasTrigger` + `TrimToLabelledRowCount` · `LoadTableFromJson` 抽取 + `LoadForTest` / `ResetCardTableForTest` / `ReloadRealCardTableForTest` · 件级自检去掉歧义跳过 · 多处 doc 就地订正） |
| `Unity/MyGame/Assets/CardPresentation/Core/TraitFrames.cs` | 改（+`TriggerTypeOf` 反查口 + 两张反查表） |
| `Unity/MyGame/Assets/CardPresentation/Core/EventTiming.cs` | 改（+登记表 + 三处 `default` + 自检口） |
| `Unity/MyGame/Assets/CardPresentation/Battle/ReplayStore.cs` | 改（+`mulliganPlus1` / `MulliganEnabledForReplay`） |
| `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | 改（`RecBegin` 写头 · `PlayReplay` 恢复/还原 · 两个自检口 · `Begin` 记 `MulliganSeenByBeginForTest`） |
| `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | 改（+21 条断言：D4 四条 / UM37·UM84·SW18 三条 / LoadForTest 三条 / mulligan 六条 / EvtKind 五条（= 21 条 `Check`，其中 3 条是 `LoadForTest` 的收尾/前提）） |
| `Unity/资料/普查产出_1024/A2_战斗VFX五件.md` | 本报告 |

⛔ 未改：`RuleEngine/**` · `CardPresentation/Core/CardView.cs` · `CardPresentation/Shell/**` ·
`CardPresentation/Hand/**` · `CardPresentation/Net/**` · `WarpforgeVFX/**` · `工具/**` ·
`Resources/*.json` · `CLAUDE.md` / `项目任务.md` / 任何 `任务总表.md` / git。

**行尾**：`git diff --numstat` = BattleDriver `22/1` · ReplayStore `24/0` · EventTiming `108/3` ·
TraitFrames `46/0` · VfxMap `279/97` · BattleScene `199/0` —— **没有整篇翻行尾**。

---

## 八 · 验收怎么跑

```bash
TMPDIR=/tmp/wf_a2 bash d:/4/Unity/工具/typecheck.sh          # 本件跑过：运行时 0 / 编辑器 0
# 主对话（同步点）跑这两条：
#   BattleScene.Run        —— 新增 21 条断言都在它里面（J1 段 + EventTiming 段 + 回放段）
#   AnimFXCheck.Run        —— TraitFrames 新增反查口 / 绑定表载入不受影响（132 条基线）
```

⚠️ 顺带影响面：`VfxMap` 的 `trigger` 桶消费变了（D4）⇒ 任何「触发那一拍播了什么」的断言都受影响；
本件已把全池件级自检（`SelfCheckRoutedTriggers`）预演到 0 违规，且现有 T21 那三条
（`Concealed Explosives` / `Dark Pact of Excess` / `Acolyte Heavy`）逐条核过**结果不变**。

---

## 九 · 没查清 / 没做

1. ⚠️ **`D4` 只挡 trait 行**（`TriggerTypeOf` 认得出的）。卡级行与 `Trigger(3)/TriggerOnPlay(6)` 一律保留。
   「trait 件在 `CardDef.AnimTimes` 里没有行」这一族（`MobEffect` / `ArmourEffect` 等）归口时仍被
   「未标名一律丢」丢掉 —— 那是既有的另一笔账（`A21` §1·6 / `P7`），**本件没动**。
2. ⚠️ **`D4` 的数据侧根治没做**：更彻底的做法是让 `gen_card_vfx_by_card.py` 把 `TriggerType` 写进运行期表
   （这样 `Start(0)` 根本不会进 `trigger` 桶）。那需要改生成器 + `Resources` 产物 —— **都不在本件白名单**。
3. ⚠️ **`UM37`/`UM84` 的 `Order` 没有直接进分类键**（运行期表没有这一列，见 §2·2）。
   现读结果正确；但**将来**若出现「同一个 `CardAnim` 同时落在 `trigger` 与 `ability` 两张桶」的形状，
   `TrimToLabelledRowCount` 只能按「先来先得」截断（同 prefab，画面一样、只可能时刻差一档）并出声。
4. ⚠️ **`mulliganEnabled` 只覆盖本地录像这一条路**：联机重连（`NetReplay`）用的是开局包，不是录像头
   ⇒ 它照旧读驱动当时的字段值（**没有改**，也没法在 `Net/**` 外改）。
5. ⚠️ **`EvtKind` 登记表不校验「枚举值有没有被重排」**（那要另一条：枚举只许追加，见 `BattleEvent.cs` 的注释）。
6. ⚠️ **没跑 Unity**（红线）⇒ 上面所有断言**未实跑**；§六 只是独立 Python 预演。

---

## 十 · 顺手发现（只报不改，铁律 13·4⑧ / 14）

1. 🔴 **`VfxMap.Trigger` 桶里 `Start(0)` 的 258 行是数据侧「归桶时没带 `TriggerType`」造成的** ——
   本件在消费口挡了，但**根**在 `gen_card_vfx_by_card.py` 的 `TRAIT_TRIGGER_EVENT`（它把 trait 行按
   trigger 名归桶、不带 `TriggerType`）。建议立账：**生成器给 `es[]` 加一列 `tt`**，消费端就能直接分桶
   （那时 `TraitFrames.TriggerTypeOf` 可以退役）。
2. ⚠️ **`TriggerNameOf` 那套「`A` 非空只认 `A`」的规则**意味着：同一个 prefab 出现在不同 `CardAnim`
   下时无法互相替代。现读没有踩到，但 `D4` 的 trait 反查用的是 **`A` 先、`N` 后**（宽松一档）——
   两者口径不同（一个严、一个宽），已在各自 doc 里写明；将来若要统一，得先确认「同名不同 Anim」的用例。
3. ⚠️ **`ReplayRecord` 的「新增字段哨兵」纪律现在有三份（`tutorialStage` / `botSeatPlus2` / `mulliganPlus1`）**，
   每份都写着同一段「`JsonUtility` 跑不跑初始化器本地无判据」。建议哪天实测一次
   （喂一份缺键 JSON 看读出什么），把那三段的「没判据」一次消掉。
4. ℹ️ **`J2` 顺手发现 1（`GSC55` 收走活人而不是墓地）不在本件范围**，仍未修（它不是表现层、会改结算语义）。
5. ℹ️ **`EventTiming.IsSourced` 现在会给未登记档出声** —— 若哪个自检/工具**遍历一个枚举子集**并调用它，
   会多出 `LogError`。现读全仓只有 `BattleScene` 那两处遍历全枚举，**已核过**不受影响。
