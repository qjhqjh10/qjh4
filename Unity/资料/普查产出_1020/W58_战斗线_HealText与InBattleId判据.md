# W58 · 战斗线 · `HealText` 与 `InBattleId` 判据（只读调查）

> 代号 **W58-INV-HEAL**（2026-10-20）。只读调查，未改任何文件（除本报告）。
> 判据顺序：**全量反编译方法体** → **解包 prefab/组件** → **类桩 `dump.cs`**；常量用 `工具/read_literal.py` 读，字符串用 `il2cpp_out/script.json` 的 `ScriptString` 解。

---

## 结论（3 句内）

1. **`HealText` 的显示点找到了**：唯一入口是 `CardScript.Heal(CardScript actingCard, HealInfo healInfo)` 协程（`CardScript._Heal_d__468__MoveNext.c`）。它把 `healText` 的 GameObject 打开、写 **`"+" + min(请求治疗量, 缺失生命)`**，把缩放压到 **0.05** 后 `DOScale` 到 **1.0**（**0.7 s · OutQuad · Yoyo ×2 循环**），`WaitForSeconds(1.5)` 后 `SetActive(false)`。
2. **`InBattleId` 不是状态词条，是「本局唯一卡号」的调试显示**：编号在 `BattleCardManager.CreateCard` / `RegisterCardInPlay` 时从自增计数器分配、写进 `CardScript.inBattleId (+0x22c)` 并同步到 TMP 文本；但该节点 **prefab 默认关**、`SetRawCardData` 又显式关一次、**全量反编译里没有任何一处把它打开** ⇒ **正式对局中永远不显示**（死调试层）。
3. **我们这一侧目前只欠「接线」**：`CardView.ShowHeal/HideHeal` 已有，但**零调用点**、无动画/自动隐藏、`Mathf.Max(1,…)` 会把 0 治疗显示成 `+1`；且现有 `PopNumber.PopHeal` 已经在上层画了一颗治疗飘字（用的是**伤害计数器的时间轴**）⇒ 直接接线会变成两个治疗数字。

---

## 原版调用链（证据 `文件:行号`）

### A. `HealText`（治疗飘字，卡上节点）

**唯一写文字的入口 = `CardScript.Heal` 协程。** 全量反编译里 `CardScript__Heal` 的调用点只有 `ResolveHeal` 一处（3 个分支），没有第二条路。

```
BattleAction(type=10 heal)                       // BattleActionType.cs:13  `heal = 10`
  └ BattleManager.ResolveAction case 10          // decomp_full/BattleManager__ResolveAction.c:1053（构协程对象；1108 `goto LAB_180979203` 启动）
      └ ResolveHeal(BattleAction) 协程            // BattleManager._ResolveHeal_d__491__MoveNext.c
          · 单目标 :92-105（先 IsInPlay :95）→ CardScript.Heal :102 → StartCoroutine 并 yield
          · 多目标 :114-147（逐目标 IsInPlay :121/:159 → CardScript.Heal :131/:147）→ 每个都 `yield return` 等它放完
      └ CardScript.Heal(this=目标, actingCard, HealInfo) // decomp_full/CardScript__Heal.c:17,22-25
          └ <Heal>d__468.MoveNext            // CardScript._Heal_d__468__MoveNext.c
```

`Heal` 协程逐拍（行号 = `CardScript._Heal_d__468__MoveNext.c`）：

| 步 | 行为 | 证据 |
|---|---|---|
| 0 | `healInfo.healValue`（coroutine 字段 `+0x28`） | `CardScript__Heal.c:22-25`（copy 4 个字：actingCard / healValue / healType） |
| 1 | **腐化不能治**：`HasCurrentTrait(0x140)`（`DefinedTrait.corrupt = 320`）⇒ 只打日志 `Tried to heal corrupt unit <name>` | `:107-108`；字符串地址 `0x1842db780` = `"Tried to heal corrupt unit "`（`script.json` ScriptString） |
| 2 | **实际治疗量 = `min(healValue, GetCurrentDamage())`**；`GetCurrentDamage = MaxHealth - Health` | `:109-112` + `CardScript__GetCurrentDamage.c:27` |
| 3 | **实际量 < 1 ⇒ 直接不显示**（清 pending、刷血条、结束） | `:136` → `LAB_1806112ce`（`:68-84`） |
| 4 | 打开 `BattleCardUI.healText`（`cardUI + 0x138`，`dump.cs:26173`） | `:155-158` |
| 5 | 文本 = **`"+" + 实际治疗量`**（`System_String__Concat`） | `:160-164`；字符串地址 `0x184275430` = `"+"`（ScriptString） |
| 6 | 先把 **localScale 设成 `Vector3.one × 0.05`**，再 `DOScale(目标=Vector3.one, 0.7s)` | `:166-196`；常量为 `Vector3.one`（`Vector3` 静态字段 `oneVector` 在 `+0xC`，`dump.cs:537035`）；`0x1834b3188` 读作 **float 0.7**，`0x1834b2f94` 读作 **float 0.05** |
| 7 | `SetEase(6)`（DG.Tweening `Ease` 枚举 **6 = OutQuad**）· `SetLoops(2, 1)`（**Yoyo**）⇒ 0.05→1.0→0.05，共 1.4 s | `:195-200` |
| 8 | **只有 `HealType.Normal(0)` 才生成治疗特效**：`BattleAnims.heal`（AssetReferenceTyped.Load）实例化到 `BattleCardUI.effectsAnchor (+0x1a0)` 的位置（GUID `4ac621b3…` = GameObject **`Healing_Circles`**） | `:201-225`；`HealType.cs`（Normal=0 / Syphon=1）；`BattleAnims.cs:8` `heal` 字段；GUID 解析见 `普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv:912` |
| 9 | `WaitForSeconds(1.5)`（`0x1834b3090` = **float 1.5**） | `:204` |
| 10 | 第二拍：`CanContinueAnim`（= 卡 GameObject 还 activeSelf）→ **`healText.SetActive(false)`** → 清 `CardScript.pendingHealing`（`+0x100`，`EntityScript.get_pendingHealing.c`）→ `UpdateHealthText(..., true)` | `:64-84`；`CardScript__CanContinueAnim.c`；`CardScript__RecordPendingHealing.c` |

**另有一个关闭点**：`BattleCardUI.HideExtraEffects` 会同时关 `minionLight / canAttackAnim / damageCounter / healText(+0x138)`，它由 `SetObjectVisibility` 调（卡离场/换可见性时）—— `BattleCardUI__HideExtraEffects.c:17-18`。

**同屏多条**：`ResolveHeal` 对每个目标 `yield return StartCoroutine(CardScript.Heal)` ⇒ **严格串行、一次一个**，每个占 1.5 s（含动画）；同一张卡多条 pending 治疗在动画结束时一次性清空（`+0x100`）。

**双方/督军**：显示代码挂在「被治疗那张卡」自己的 `CardScript` 上，与阵营无关 ⇒ **双方都显示**；督军也走同一个 `CardPrefab`（`BattleCardManager` 只有一个 `cardPrefab` 字段），所以督军回血也飘字。

### B. 原版 prefab 真值（`HealText`）

- 层级：`CardPrefab → Board Elements → HealText`；`Board Elements` = `Transform_-4334370980763821120`（localPos 0、scale 1），**与 `3DBody`(0,0,0) 同级**。
- `GameObject/HealText.json`：**`m_IsActive = false`**、`m_Layer = 10`、`m_Name = HealText`。
- `RectTransform_-4069981099246511168.json`：anchor 0.5/0.5 · **`m_AnchoredPosition = (0, 2)`** · `m_SizeDelta = 0.8009615 × 0.6837339` · pivot 0.5 · `m_LocalPosition.z = -0.05` · `m_LocalScale = 1`。
  - **换算**：`Board Elements` 空间即 3DBody 空间，`y = 0 = 卡底`（沿用 `资料/3DBody_原版场上卡体规格.md` §四 的既有判据）⇒ 我们的卡根（中心原点、H=3.3313）位置 = **(0, 2 − 3.3313/2 = +0.33435, −0.05)** —— 与 `CardView.HealTextLocalPos` 完全一致。
- TMP 组件（`MonoBehaviour_-7677963611639080000.json`，旧解包目录）：`m_text = "+5"`（模板字）· **`m_fontSize = 0.7`** · `m_enableAutoSizing = 0`（min 0.35 / max 1.4 是死值）· **`m_isOrthographic = 1`** ⇒ 渲染 em = 0.7 · `m_fontColor = 白 (1,1,1,1)` · H=2 Center / V=512 Middle · `m_TextWrappingMode = 0` · `m_overflowMode = 0` · `m_margin.x = -0.09` · 字体 `m_fontAsset` pid `3485036404935369831`（= **Pragati-Regular**，同卡面正文那 68 颗 TMP 共用）· 材质 pid `-5050103597772954521` = **`Pragati-Regular Atlas Material`**（无描边）。
- 声音（`:146-154`）：`SoundAssetCollection.MinionHeal` → 再 `SoundAssetCollection.Syphon`。⚠️ 我们既有的 `BattleSfx` 结论：本 build 里 Syphon 那一格是空的 ⇒ 实际只响 MinionHeal（与现实现一致）。

### C. `InBattleId`（本局唯一卡号，调试显示）

**分配 / 写文本**

| 点 | 行为 | 证据 |
|---|---|---|
| 造卡 | `BattleCardManager.CreateCard` → `SetInBattleId(newCard, counter)` | `BattleCardManager__CreateCard.c:151-152`（计数器 `+0x28..+0x38` 是 ObscuredInt，:170+ 自增） |
| 入场 | `BattleCardManager.RegisterCardInPlay` → `SetInBattleId(card, counter)` | `BattleCardManager__RegisterCardInPlay.c:57-58`（:62 自增） |
| 写值 | `CardScript.inBattleId (+0x22c) = n`；若 `cardUI.inBattleIdText (+0x230)` 存在 → `text = n.ToString()` | `CardScript__SetInBattleId.c:13,15-27`；`dump.cs:26214` |
| 读值 | 全工程按 `+0x22c` 取「本局卡号」：`GetCardFromUniqueId` / 换牌 / 回放 / 语音 / `BattleManager.ParsePlayerAction` 等 41 处 | `grep "0x22c" decomp_full` |

**显示**：
- `GameObject/InBattleId.json`：**`m_IsActive = false`**、`m_Layer = 0`（默认层）。
- `RectTransform_3756633904034061248.json`：anchor 0.5/0.5 · **`m_AnchoredPosition = (−0.004, 0.261)`**（3DBody 空间、y=0 卡底 ⇒ 我们的卡根 y ≈ −1.4047，**卡底中央**）· `m_SizeDelta = 0.4244 × 0.3512` · pivot 0.5 · scale 1。
- TMP（`MonoBehaviour_3405572449271978944.json`）：`m_text = "12"` · **`m_fontSize = 1.61`，`m_enableAutoSizing = 1`（min 0.225 / max 1.61）** · **`m_isOrthographic = 0`** ⇒ 世界 em = 1.61 × 0.1 = **0.161** · 白 · Center/Middle · 无折行 · 字体 pid `-8244042478085975641` = **Asar-Regular**（与 `Serghar Targorst` 卡名那一批同字体）· 材质 pid `1185407168034066855` = **`Asar-Regular Atlas Material`**。
- **永远不会显示**：
  1. prefab 里 `m_IsActive = false`；
  2. `BattleCardUI.SetRawCardData` **显式再关一次**（`BattleCardUI__SetRawCardData.c:196-200`）；
  3. 全量反编译按 `+0x230` 扫，只有 `SetRawCardData`（关）与 `CardScript.SetInBattleId`（只写 text），**没有任何 `SetActive(true)`**；
  4. 该 bundle 的 AnimationClip 里 **0 条**曲线引用 `InBattleId` / `HealText`。
  ⇒ 结论：**原版正式对局不显示它**，是遗留的调试用 overlay（与潜行/变体/被标记都无关）。

---

## 状态 → 参数表（原版）

| 状态 / 触发路径 | 飘不飘字 | 显示的数值 | 动画 / 时长 | 特效 / 声音 | 备注 |
|---|---|---|---|---|---|
| **普通治疗**（卡面 `Heal N` / 主动技能 / 触发） | ✅ `"+" + N` | `min(N, MaxHealth−Health)` | scale 0.05→1.0（0.7 s OutQuad）→ 回 0.05（Yoyo 第 2 圈）；**1.5 s 后关** | `Healing_Circles` 于 `effectsAnchor`；MinionHeal（+Syphon 尝试） | `HealType.Normal(0)` |
| **溢疗**（治疗量 > 缺失生命） | ✅（只显示实际量） | 同上，**溢出丢弃、不显示** | 同上 | 同上 | 例：3/5 时治疗 4 ⇒ 显示 `+2` |
| **满血时被治疗** | ❌ 完全不显示 | — | — | 无 | `实际量 < 1` 直接 goto hide（`:136`） |
| **治疗量 ≤ 0 / 目标非法** | ❌ | — | — | 无 | 日志：`Tried to heal with negative value N`（`0x1842db880`）/ `Tried to heal corrupt unit …`（`0x1842db780`） |
| **目标带 `corrupt`(320)** | ❌ 不治也不显示 | — | — | 无 | `:107` |
| **吸魂 Syphon（trait 110，`HealType.Syphon`）** | ✅ 同普通治疗 | `min(1, 缺失)` | 同上 | **无 `Healing_Circles`**（VFX 只在 Normal 分支） | `CardScript__ResolveDamageDealt.c:49-59`（`HasCurrentTrait(0x6e)` → `HealOneCharacter(..., healType=1)`） |
| **回合末再生 Regeneration（trait 1030 / `0x406`）** | ✅ | `min(trait值, 缺失)` | 同上 | Normal ⇒ 有治疗圈 | `CardScript__OnTurnEnd.c:84-107`（`HealOneCharacter(..., healType=0)`，priority=1） |
| **猎杀标记回血（Hunt Mark）** | ✅（走 `TriggerOnMinionDeath` → `HealOneCharacter`） | 同上 | 同上 | Normal | `CardScript__TriggerOnMinionDeath.c:91-93` |
| **多目标治疗** | ✅ 逐个显示 | 各自 clamp | **串行**：每个目标 1.5 s，`ResolveHeal` 逐个 `yield return` | 每个目标各一份特效/声音 | `_ResolveHeal_d__491__MoveNext.c:114-147` |
| **伤害与治疗同批** | ✅ 两个都显示 | 各自独立 | 伤害走 `DamageCounter` 的时间轴，治疗走本节的 DOScale 时间轴 | 两颗不同 TMP（DamageText / HealText） | 两条**独立路径**（`PopNumber.cs` 文件头） |
| **复活 / Reanimate（`TransformFromRemnant`）** | ❌ 不飘治疗字 | — | — | 有各自的复活演出 | 复活链 **不经过 `CardScript.Heal`**（`grep TransformFromRemnant` 全链 0 条 Heal） |
| **幸存者 Survivor（trait 430/440）** | ❌ **不飘字** | — | — | Survivor 触发动画 + 音效 | `CardScript__UseSurvivor.c:36-40` **直接写 `+0x68` 生命**，然后只 `BroadcastUnitHealed`（`:77`），**没有 Heal 协程** |
| **双方 / 督军** | ✅ 双方都显示；督军同 prefab | — | — | — | 显示挂在被治疗卡的 `CardScript` 上 |

> ⚠️ 我们引擎把 `EvtKind.Hit` 负数当「治疗」用（`RuleCore.cs:1627` 注释），因此上面 **Survivor 那行是当前最容易接错的**：原版 Survivor **只广播 healed、不显示数字**。
> ⚠️ 原文「光环持续治疗」在原版**没有独立路径**：`HealOneCharacter/HealMultipleCharacters` 的全部调用点只有 4 个（`AbilityLogic__PlayAbility.c:1295/1303` · `CardScript__ResolveDamageDealt.c:56` · `CardScript__TriggerOnMinionDeath.c:91` · `CardScript__OnTurnEnd.c:105`）⇒ 持续回血就是「再生（回合末）」那一档，走同一条显示。

---

## 原版 vs 我们（逐条）

| # | 项 | 原版 | 我们 | 结论 |
|---|---|---|---|---|
| 1 | 触发接线 | `CardScript.Heal` 协程唯一入口 | `CardView.ShowHeal/HideHeal` **0 调用点** | 🔴 **缺接线** |
| 2 | 数值 | `min(请求, 缺失)`；实际 <1 不显示 | API 收 `amount`，`Mathf.Max(1, amount)` 后显示 | 🔴 0/负数会被显示成 `+1`；调用方应传**实际治疗量** |
| 3 | 动画 | scale 0.05→1.0，0.7 s OutQuad，Yoyo ×2 | 无（瞬间全尺寸） | 🔴 缺 |
| 4 | 生命周期 | 1.5 s 后自动 `SetActive(false)` | 只有 `SetFace(非场上)` 调 `HideHeal()` ⇒ 会**常驻** | 🔴 缺自动隐藏/重开计时 |
| 5 | 位置 | `(0, +0.33435, −0.05)` 卡根空间 | `HealTextLocalPos = (0, 2 − CardUnitH/2, −0.05)` = 同值 | ✅ 一致 |
| 6 | 字号/字体/对齐/颜色 | 0.7 em · Pragati · Center/Middle · 白 · 模板 `"+5"` | `HealTextFontSize = 7.0`（3D TMP ×0.1 = 0.7 em）· `LatinCardBody`(Pragati) · Center · 白 · 模板 `"+5"` | ✅ 一致（材质我们自建，原版是 `Pragati-Regular Atlas Material` 无描边） |
| 7 | 关闭点 | 两个：协程 1.5 s、`HideExtraEffects`（可见性变化） | 只有 `SetFace(!board)` 一处 | 🟡 建议在可见性/离场同族入口也隐藏 |
| 8 | 特效 | `BattleAnims.heal` → `Healing_Circles`，**仅 Normal** | `VfxMap` 对 **任意** `EvtKind.Hit<0` 都出 `VfxMap.Heal` | 🟡 Syphon 治疗会多出治疗圈 |
| 9 | 声音 | MinionHeal（Syphon cue 空，实响一条） | `EvtKind.Hit<0` → `BattleSfx.MinionHeal` | ✅ 一致 |
| 10 | 多目标 | 串行，每个 1.5 s | 引擎一次性结算，表现层无排队 | 🟡 若接卡上飘字，需要按事件顺序排队（或接受同帧覆盖） |
| 11 | 治疗事件完整性 | 所有治疗都走 `CardScript.Heal` | **只有 3 处发负数 Hit**：再生(`RuleCore.cs:1627`)、Survivor(`:7240`)、Hunt Mark(`:7284`)；**主力 `DoHeal`(`EffectResolver.cs:2433-2451`) 与 `RuleCore.cs:9530-9538` 的 `heal` 分支一条事件都不发** | 🔴 大部分卡的治疗目前连飘字都没有 |
| 12 | Survivor | 不显示治疗数字 | 发负数 Hit ⇒ 会显示 | 🔴 语义被混用 |
| 13 | 伤害/治疗分离 | 两颗独立 TMP、两套锚与时间轴 | 已有 `PopNumber.PopHeal`（字号/框照 HealText，但**位置 = HUD 偏移、时间轴 = DamageCounter 的 Variation 1**） | 🔴 **与新增的卡上 `HealText` 重复**；接 `ShowHeal` 前必须先关掉 `PopNumber.PopHeal` 这一支，或让它只做兜底 |
| 14 | 受击表现 | 治疗不震镜头、不后仰 | `PlayHitFeel` 对**所有** `EvtKind.Hit` 无条件 `CardFeel.HitReact(...)` + `ShakeCameraForHit(...)`（`BattleDriver.cs:16516-16543`） | 🔴 治疗当前会让卡后仰 + 震镜头（顺手发现，与本账同一条链） |
| 15 | `InBattleId` | 分配 + 写文本，但**永不显示** | 我们没有该节点；引擎已有等价语义 `UnitState.InstanceId`（E12） | ✅ **不必做显示层**；⛔ 不要为它新增一个看不见的图层 |

---

## 落地建议（文件 + 符号）

### 1. 引擎：把「治疗」发成一条**能显示**的事件
- `d:/4/Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs` · `DoHeal`（:2433）：逐目标记 `before`，写完后 `healed = t.Health - before`，`healed > 0` 才发事件；督军那支同理（:2442）。
- `d:/4/Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` · `case "heal"`（:9530-9538）：同上。
- **不要**直接沿用「负数 `Hit`」给 Survivor：`RuleCore.cs:7240` 那条在原版**不出飘字**（`UseSurvivor` 直接写血，只 `BroadcastUnitHealed`）。
  - 最小改法：给 `BattleEvent` 加一个显式的「这是治疗表现」标志（或新增 `EvtKind.Heal`），只在「原版会进 `CardScript.Heal`」的路径上置位；Survivor 那条保持只广播。
  - 判据：原版唯一显示口 = `CardScript.Heal`；本报告上表就是「哪些路径会进它」。

### 2. 表现层：接卡上 `HealText`，并停掉重复的 `PopNumber.PopHeal`
- `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` · `PlayHitFeel`（:16490）：
  - 现在 `e.Amount != 0` 统一走 `PopNumber.PopHeal/PopDamage`（:16547-16558）；
  - 建议把**治疗**分支提到前面：`if (heal) { var v = ViewAt(e.Player, e.Slot); v?.ShowHeal(实际量); return; }`（`ViewAt` 已覆盖督军格 `BoardSpec.WarlordSlot`，双方都在内）；
  - **不要在治疗分支里跑 `HitReact` / `ShakeCameraForHit`**（原版治疗无这两样）。
- `d:/4/Unity/MyGame/Assets/CardPresentation/Core/CardView.cs` · `ShowHeal(int)` / `HideHeal()` / `BuildHealText()`（:5566-5599）：
  - `ShowHeal`：`amount < 1 ⇒ HideHeal(); return;`（去掉 `Mathf.Max(1, …)`）；文本 `"+" + amount`；`SetActive(true)`；
  - 动画：`transform.localScale = Vector3.one * 0.05f` → `DOScale(Vector3.one, 0.7f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo)`，用 `CardTween.Use(seq, …)`（批处理 `Manual` 档可 `CardTween.Advance`）；序列末尾 `AppendInterval(1.5f - 1.4f)` 后 `SetActive(false)`；
  - 重入：先 `DOKill` 旧序列再开新（同一张卡连续被治疗 = 原版重置动画）；
  - `HideHeal` 保留，另在 `SetFace(非场上)` 与同族可见性入口（对应原版 `HideExtraEffects`）调用。
- `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/PopNumber.cs` · `PopHeal`（:212）：接上卡上 `HealText` 后**停用或降级为兜底**（视图缺失时才用），否则同一次治疗会出现两颗 `+N`。

### 3. `InBattleId`
- **不建议**加显示层（原版永不显示，加一个看不见的节点没有意义）。
- 语义侧已由 `RuleEngine` 的 `UnitState.InstanceId`（E12，`RuleCore.EmitUnit` 统一填）覆盖，与本报告的原版 `+0x22c` 一一对应。若只为「调试」需要，可保留本报告 §C 的坐标/字号作为实现判据，但必须默认关。

### 4. 自检断言建议（复刻完成后）
- `ShowHeal(0)` / `ShowHeal(-3)` ⇒ `HealTextVisible == false`；`HealTextText` 不含 `+1`。
- `ShowHeal(5)` ⇒ 文本 `"+5"`、可见；`Advance(1.4f)` 后仍可见（或按选定实现检查 scale 回到 0.05）；`Advance(1.6f)` 后不可见。
- 满血单位被治疗 ⇒ 不显示（需要引擎把实际量 0 的治疗过滤掉）。
- 灭自证：断言 `ShowHeal` 的文本来自**引擎实际治疗量**（例：3/5 治 4 ⇒ `"+2"`），而不是调用方传的请求值。

---

## 没查清的部分（如实列出 + 去哪拿）

1. **`Board Elements` 空间的 `y = 0 = 卡底`** 沿用 `资料/3DBody_原版场上卡体规格.md` §四的既有判据（本次只复核了 `Board Elements` 与 `3DBody` 同在 (0,0,0)、以及 HealText 的 `m_AnchoredPosition`），**没有重新独立推导整卡基准**。若要重推：读 `CardPrefab` 的 `Card 3D` 网格 Renderer bounds 与各 3D 锚点。
2. **`InBattleId` 是否被某个非 `Assembly-CSharp` 的调试工具/控制台**在运行时打开：本次只覆盖了 `decomp_full`（全量方法体）+ prefab + AnimationClip；**没有任何代码把它打开**，但无法排除外部 MelonLoader mod 之类。若要做判据：进原版跑一局并 dump `CardPrefab` 全树 `activeSelf`（铁律 4）。
3. **`SetEase(6) = OutQuad`** 是按 DG.Tweening `Ease` 枚举顺序判的（`Unset=0, Linear=1, InSine=2, OutSine=3, InOutSine=4, InQuad=5, OutQuad=6`），**没有从 DLL 里反查枚举值**。
4. **`healText` 的 `m_sharedMaterial` 具体渲染效果**（无描边）已按材质名判定；我们自建材质若带描边会与原版有差，但**没做像素级对比**。
5. **`HealText` 在卡被放大/缩小（`desiredScale` 不同行）时的视觉字号**：原版节点跟着卡的 lossyScale 走（`PopNumber` 已有 0.6071 / 0.5507 两档桥），我们卡上节点同样吃卡缩放；**未实机对比两档**。
