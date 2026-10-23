# 战斗表现 · `bloodthirst` 改挂（执行代理，2026-10-23）

> **范围**：`T03 · bloodthirst 硬绑` —— 把触发框从「按名字硬绑 `BloodThirstEffect`」改成
> **原版真值 `BloodThirstEffect_Gain`**。
> **没跑 Unity**（不跑 `-executeMethod`、不跑 `*.Run`）· **没动 git** · **没改正本**
> （`CLAUDE.md` / `项目任务.md` / 其它线台账）。
> ✅ `TMPDIR=/tmp/wf_bt bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（最后两版都验过）。

---

## ① 结论

| 件 | 结果 |
|---|---|
| **触发框改挂** | ✅ `bloodthirst` 的触发框 = **`BloodThirstEffect_Gain`**（原版 `BloodThirst.asset` 的 **Trigger(3)** 那一条）。原先的 `BloodThirstEffect` 是**按名字硬绑、判据不成立**（那件属于 CardAnim `BloodThirstTraitTrigger`，`BloodThirst.asset` 一条都没引用它）。 |
| **表结构** | ✅ `TraitFrames.TriggerFrames` 改成**由真值表 `TriggerEffectBindings` 派生**（每行五列原版值：`DefinedTrait / TriggerType / EffectGuid / CardAnim / Prefab`）。prefab 名**只写一处**。 |
| **TriggerOnPlay(6) / End(1)** | ✅ **保持原样**（不归这张表）—— 仍是「含 6 ⇒ 进 `TriggerOnPlayTraits` 去重」那套。 |
| **断言** | ✅ `BattleScene.cs` ⑥·b 补 **4 条**（含灭自证）。**宿主 = `BattleScene.Run`**（`CardBaseDemo` 里没有 TraitFrames 那一套，见 §5）。 |
| **夹具** | ✅ **池内有真卡**（`Lheorvine Ukris` `BL21` 带 `Blood Thirst`；`Threnodic Noise Marine` / `Murderfang` 会授予它）；宿主里也**已有现成夹具**（⑥ 段给 `Tide Minion` `AddKeyword(bloodthirst)`）。⛔ 没造测试卡。 |

---

## ② 原版判据（逐跳可核）

### 2.1 数据真值（`BloodThirst.asset`，trait id 220 = `0xdc`）

出处：**`Unity/数据/游戏数据/trait_collection.json`** 的 `traits[name=="bloodThirst"]`
（从 `sharedassets0.assets` 的 `TraitsCollection` + 各 `CardTrait` 资产**逐字节**解出；
生成器 `工具/export_trait_collection.py`，`generated: 2026-10-22`）。现读全文：

```text
name = "bloodThirst" · definedTrait = 220 · m_Name = "BloodThirst" · traitConfig.pathID = 465
traitEffects[]:
  · name="Trigger"       triggerType=3  effectAnimGuid=874dd68077c21cc4c86c5362b436d792  effectAnim="BloodThirstGainAnim"  playIfWarlord=true
  · name="TriggerOnPlay" triggerType=6  effectAnimGuid=5a60dbe6e84c80742bf4f9c74b0502ed  effectAnim="BloodThirstIdleAnim"  playIfWarlord=true
  · name="End"           triggerType=1  effectAnimGuid=2b63fd560f1162e468349038c37ad642  effectAnim="BloodThirstLoseAnim"  playIfWarlord=true
```

### 2.2 GUID → CardAnim → prefab

- CardAnim `BloodThirstGainAnim` 的 target prefab =
  **`BloodThirstEffect_Gain`**（`GameObject`，`bundle_battleprefabs_vfxandmisc_assets_all`）
  —— 出处 `Unity/数据/索引/anim_address_map.json` 的 `cardanim_to_asset["BloodThirstGainAnim"].targetName`；
  同一份 join 在**运行期**那份 `Resources/cardanim_clips.json` 的 `a→n` 两列里也有（`a=BloodThirstGainAnim, n=BloodThirstEffect_Gain`）。
- GUID 本身出自 `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`
  （`874dd68077c21cc4c86c5362b436d792` → `cardanimsgeneral_assets_all.bundle` → `MonoBehaviour/CardAnim/BloodThirstGainAnim`）。
- 🔴 **两件容易认错的**（这就是原先挂错的原因）：
  - prefab `BloodThirstEffect` 的 CardAnim 是 **`BloodThirstTraitTrigger`**（guid `d9a21c777d8e7cd4fb48587beb3ba528`），
    名字确实像 `…TraitTrigger` 那一族，但 **`BloodThirst.asset` 的 `_traitEffects` 一条都没引用它**；
  - prefab `BloodThirstEffect` 还有第二条来路 `Bloodthirst_Flare_Warlord` —— 同样**不在 `_traitEffects` 里**。
  - ⚠️ 别把 `customObject.guid = fb07f52246771d5489569abf9c95df92` 当成 `effectAnim` 的 GUID ——
    那是 BloodThirst 自己的 `customObject`（`trait_collection.json` 的 `unresolvedCustomObjectGuids` 里有它）。

### 2.3 原版运行时怎么挑这一条（全量反编译现读）

| 步 | 文件:行号 | 内容 |
|---|---|---|
| ① 入口 | `BattleCardUI__DisplayTriggerAnim.c:71` | 只处理 `TraitEffect.TriggerType == 3 \|\| == 6` |
| ② 取 effect | `BattleManager__GetTraitEffect.c:6` | → `CardTraitCollection.GetTraitEffectObject(traitId, triggerType, …)` |
| ③ 选条 | `CardTraitCollection__GetTraitEffect.c:34` | 遍历 `CardTrait._traitEffects(+0x38)`，**返回第一条 `TriggerType == 参数`** 的；找不到返回 0 |
| ④ 解 prefab | `CardTraitCollection__GetTraitEffectObject.c:20` | 取该 `TraitEffect` 的 `effectAnim`（`AssetReference`）→ `Load` |
| ⑤ 去重 | `BattleCardUI__DisplayTriggerAnim.c:88-91` | `TraitFrameEffectsTrigger.ContainsKey(traitId)` 已有 ⇒ 不播；写表在回调 `…b__0.c:19-21`（**只对 `TriggerType == 6` 写**） |
| ⑥ 触发来源 | `CardScript__ActivateTriggerTraitAnim.c:9-13` | `GetTraitFromAbilityTrigger(trigger)` → `HighlightTraitIcon` → `DisplayTriggerAnim(...)` |

⇒ Trigger(3) 那一条是**确定的**（不是任选一条），所以 `_Gain` 是唯一正确答案。

> ⚠️ **如实记一处简化**：原版 `DisplayTriggerAnim` 是**逐条**遍历，理论上 trait 有 3 和 6 两条时会**两条都播**
> （同步排两个协程、去重表在回调才写）。我们每个 trait 只绑**一个** prefab（本类既有简化，文件头已标）；
> 本条任务口径也只要求「绑 Trigger(3) 那一条、另两条保持原样」⇒ `_Gain` = 我们播的那一个。

---

## ③ 改动

| 文件 | 行尾 | 改动 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Core/TraitFrames.cs` | **LF（改后仍纯 LF，465 行）** | ① 新增 `public struct TraitEffectBinding`（5 列原版真值）；② 新增 `public static readonly TraitEffectBinding[] TriggerEffectBindings`（`oath` / `bloodthirst` 两行，**prefab 名只此一处**）；③ `TriggerFrames` 改成 `BuildTriggerFrames()` 从真值表派生（键按 `Norm` 归一 = `PrefabFor` 的查法）；④ 新增 `TryGetTriggerBinding(trait, out binding)`（自检用，与 `PrefabFor` **同源**）；⑤ 文件头 + bloodthirst 那一段注释按真值重写（含「别拿 `BloodThirstEffect` / `Bloodthirst_Flare_Warlord` 顶替」的理由）；⑥ `MissingTraits` 里去掉 `bloodthirst`（它已绑定，本来就走不到那一支）。 |
| `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | **CRLF（改后仍纯 CRLF，38412/38412）** | ⑥·b 新增 **4 条断言**；⑥ 段的旧判据注释与一条断言文案按铁律 5 就地订正（原来写的是不成立的 `BloodThirstTraitTrigger → BloodThirstEffect`）。**只加断言 + 改注释，没动别的逻辑**。 |
| `Unity/资料/任务线/战斗线/任务总表.md` | LF | §一 顶部加「2026-10-23 · 本轮已核销」一小节（只写 bloodthirst 这条）。 |

> ⚠️ `BattleScene.cs` 在本代理开工**前就已是脏的**（工作树里有别人的未提交改动）：
> `git diff --numstat` 的 570/9 里，**只有 39 行是本笔**（hunk `@@ -7975,8 +8015,11` /
> `@@ -7986,6 +8029,39` / `@@ -8010,7 +8086,7`）；其余 hunk（`1321` / `12992` / `18005` / `19446` / `34444`）**不是本笔**。

---

## ④ 断言（`BattleScene.Run` 的 ⑥·b）

| # | 断言 | 作用 |
|---|---|---|
| 前提 | `TryGetTriggerBinding(BloodThirst)` 为 true | 表里有这一行 |
| ① | `PrefabFor(BloodThirst, trigger:true) == "BloodThirstEffect_Gain"` | **解析结果 == `_Gain`** |
| ② | `TriggerType == 3` ∧ `CardAnim == "BloodThirstGainAnim"` ∧ `EffectGuid == "874dd68077c21cc4c86c5362b436d792"` | 真值三列（触发的是**那一条**） |
| ③ | `PrefabFor(...) != "BloodThirstEffect"` | **灭自证**：与 ① 结构上不可能同时满足 |
| ④ | `WarpforgeEffectLibrary.Instance.TryGet("BloodThirstEffect_Gain")` | 那一件**真在效果库里**（否则这一挂是空转、框上不了场） |

**灭自证为什么成立**：三条期望值一律写成**原版资产里的字面量**（⛔ 不从 `TriggerEffectBindings` 取了再比自己）⇒
把那一行改回 `BloodThirstEffect` ⇒ **①③ 同时红**；改 GUID / CardAnim / TriggerType ⇒ **② 红**。
即使有人「两边一起改回去」（表 + 某条断言），③ 是**反向**断言、也必须同时改，而它检查的正是原版数据里不存在的那件。

**复跑**：宿主 = `BattleScene.Run`（`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`，约 7m56s）。
`CardBaseDemo.Run` **不需要跑** —— 它里面没有任何 `TraitFrames` 断言（`grep TraitFrames CardBaseDemo.cs` = 0 命中）。

---

## ⑤ 没查清 / 顺带发现（⛔ 只报不改）

1. 🔴 **`PlayFrames` 里 `codex` 的时序可能与原版不符（未改，属另一笔）**：
   `trait_collection.json` 现读 `codex`（definedTrait 1000）**只有一条 `Trigger(3)`** → `CodexTraitTrigger` → `CodexEffect`，
   **没有 OnPlay(2)**；而我们把 `codex` 绑在 `PlayFrames`（OnPlay 那一本）里。
   prefab 名字对得上（`CodexEffect`），但**挂的是哪一本**存疑 —— 两条本子的触发时机完全不同（`DisplayStatusAnim` vs `DisplayTriggerAnim`）。
2. ⚠️ **`vanguard` 也有同类疑点**（文件头已标过）：`trait_collection.json` 里 `vanguard` 的 OnPlay(2) 是指向
   `VanguardIdleAnim → VanguardIdleEffect`，而我们绑的是 `Vanguard Frame Animated VAT`（原版那件是**具名字段** `vanguardFrame`，
   与 TraitEffect 是两套）。属「按名字硬绑」的那一族，未改。
3. ⚠️ **`playIfWarlord` 那道门仍未实现**（`DisplayTriggerAnim.c:62-68`）。本笔只把真值记进注释：
   bloodthirst 三条**都是 `true`** ⇒ 这道门在 bloodthirst 上永远不拦（所以本笔不受它影响）；
   但 **`oath` 是 `false`** ⇒ 原版「卡是督军就不播 oath 框」我们**仍会播**。属另一笔。
4. ⚠️ **`DisplayTriggerAnim` 逐条遍历 ⇒ 3 和 6 会各播一次**（见 §2.3 末尾）。我们把 6 那件（`_Idle`）当作
   「状态粒子、归 `TraitParticles` 那条链」—— 那条链**在本仓里还没接**（`TriggerFrames` 只管框）。
   要不要补「触发时也播 `_Idle`」需要主对话裁（涉及去重语义：原版头一次两条都播、之后都被挡）。
5. ✅ **没有造测试卡**：池内 `Lheorvine Ukris`(BL21) 自带 `Blood Thirst`；宿主 ⑥ 段本来就用
   `Tide Minion` + `AddKeyword(bloodthirst)` 这套现成夹具，本笔沿用它、没加新夹具。
