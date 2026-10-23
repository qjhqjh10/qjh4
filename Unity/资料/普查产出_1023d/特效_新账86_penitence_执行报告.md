# 特效线「新账-86」· `penitence` 那一拍播不出来 / 播错件 —— 执行报告

> 执行代理（特效线）。日期 **2026-10-23**。仓库根 `d:/4`。
> ⛔ 未跑 Unity 批处理 / player；⛔ 未动 git；⛔ 未改 `trait_effect_bindings.json` / `BattleDriver.cs` /
> `CardView.cs` / `WarpforgeVFX/**` / 任何正本；⛔ 未跑 `sed -i`。
> 判据优先级：`d:/2/tools/decomp_full/` → `数据/游戏数据/trait_collection.json` → 卡 SO 导出（`cards_raw.json`）。
> 采用的修法 = **修法①**（主对话已裁定）：`VfxMap` 里把 `penitence` 归一成原版 trigger 名 **`Rage`**，再走
> `IsRoutedTriggerName` 的那一套。

---

## 一、结论（5 张卡逐张：改前 → 改后）

期望值全部由**卡池数据 + `card_vfx_by_card.json`** 独立推出（见 §五 的离线模拟），不是从被测实现读回。

| 卡 | 改前（`keyword="penitence"`，带 `times` 的生产路径） | 改后 |
|---|---|---|
| **`SOR8` Arco-flagellant** | `trigger` 粗桶 ⇒ 播 **`FlankEffect`**（**错件**） | **`PenitenceEffect`**(t=0) + `Arco-Flagellant Strike`(t=0.5)；**不再播 `FlankEffect`** |
| **`SOR10` Sister Repentia** | **什么都不播**（`trigger` 桶空 ⇒ `perCardOnly` 回空） | **`PenitenceEffect`**(t=0) |
| **`SOR21` Death Cult Assassin** | **什么都不播** | **`PenitenceEffect`**(t=0) + `Death Cult Strike` ×2（t=0.5 / 0.7） |
| **`SOR27` Penitent Engine** | **什么都不播** | **`PenitenceEffect`**(t=0) |
| **`SOR32` Mortifier** | **什么都不播** | **`PenitenceEffect`**(t=0) + `BulletImpact_4shot_trail`(t=0.5) |

- t 值 = `CardAnimTime.Offset`（`cards_engine.json` 的 `t` 列：`SOR8` = 0 / 0.5 · `SOR21` = 0 / 0.5 / 0.7 ·
  `SOR32` = 0 / 0.5；多候选用组偏移，单候选按回归口径压 0）。
- `SOR21` 那条 `CamouflageEffect`（`CamouflageTraitPlay`）**不在**这一拍 —— 它的 `CardAnimTime` 不在
  `AnimTimes` 里 ⇒ `TriggerNameOf` 回 null ⇒ 归口生效时被丢掉。这与 `TryResolveTriggerPicks` doc 里
  「未标名的候选在归口生效时两张桶都丢掉」的既定口径一致（改前因为整卡播放不出来，这一条本来也没播）。
- **音效（任务第 4 点）= 同一跳，不是另一跳**：`PenitenceEffect` 一旦播出，
  prefab 上的 AnimFX 模块 `sounds[0].sound = @asset:MonoBehaviour:Trait Penitence` 就会放
  `Penitence.wav`。证据：`数据/游戏数据/animfx_modules.json`（`PenitenceEffect` 段）·
  `CardPresentation/Resources/animfx_sounds.json`（cue `Trait Penitence` → clips `["Penitence"]`，pitch .95–1.05）·
  `CardPresentation/Resources/Art/audio/sfx/Penitence.wav` 存在 ·
  `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:492/513-535`（`BuildSoundList` 读 `sounds[i].sound`，`TickSounds` 到点播）。

---

## 二、改动清单

| 文件 : 符号 | 改了什么 | 为什么 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs` : `IsRoutedTriggerName` | 白名单加 **`case "Rage":`**（+ 三条判据注释） | 不放行 ⇒ `target` 恒为 null ⇒ 这一拍继续播粗桶/什么都不播 |
| `Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs` : `CanonicalTriggerName`（**新增**） | `penitence` → `Rage` 的**唯一**归一（`OrdinalIgnoreCase`） | `penitence` 是显示名、`Rage` 是 `AbilityTrigger.Rage=130`；`NameIs` 逐字比 ⇒ 不归一永远筛不出候选。**判据收口在这一处** |
| `Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs` : `TryResolveTriggerPicks`（② 定目标） | 先归一再判白名单；`target` 存**归一后的名字** | `target="penitence"` 与候选的 `"Rage"` 比不上；映射只在上面那一处 |
| `Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs` : `TryResolveTriggerPicks` 的 doc | 「目标怎么定」同步成「先归一」 | 铁律 5：文档要等于当前认知 |
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/AnimFXCheck.cs` : `CheckTraitAuthority` **⑤**（新增段） | 6 条断言（见 §三） | 正例 + 反例 + 灭自证；期望值从 `CardDef.AnimTimes` 独立推 |
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/AnimFXCheck.cs` : `HasVfx` / `VfxNames`（新增小工具） | 结果里有没有某件 / 把结果名拼成一行 | 断言失败时能看见「实得什么」 |
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/AnimFXCheck.cs` : `CheckTraitAuthority` **②** 的注释与一条文案 | 改写成「**`times = null` 那一档**」的口径 | 原文案「这 5 张没有任何一条 `PenitenceEffect` 从 trigger 档播出去」在修好之后**会与新行为打架**（铁律 5） |

> ⚠️ **简报里的路径有误**：`Assets/CardPresentation/Editor/AnimFXCheck.cs` **不存在**；
> 唯一的 `AnimFXCheck.cs` 在 **`Assets/WarpforgeArena1/Editor/AnimFXCheck.cs`**（`AnimFXCheck.Run` 就是它）。
> 断言加在**这个**文件里（它的 `CheckTraitAuthority` 本来就是 T1/penitence 那组断言的家）。

**改后的判定链**（判据只两处，一行一处）：

```textkeyword="penitence"
  → CanonicalTriggerName → "Rage"
  → IsRoutedTriggerName("Rage") = true ⇒ target = "Rage"
  → 两张桶（trigger + ability）只留 TriggerNameOf(times, pick) == "Rage" 的候选
  → SOR8: [PenitenceEffect, Arco-Flagellant Strike]；SOR10/27: [PenitenceEffect]；
    SOR21: [PenitenceEffect, Death Cult Strike ×2]；SOR32: [PenitenceEffect, BulletImpact_4shot_trail]
```

---

## 三、断言清单（`AnimFXCheck.CheckTraitAuthority` ⑤，共 6 条）

| # | 断言 | 挡什么 |
|---|---|---|
| 1 | ⑤**前提**：卡池里拿得到 `SOR8` / `SOR10` | 卡池读不到时下面不会「空断言自己绿」 |
| 2 | ⑤**前提**：`SOR10.AnimTimes` 里 `PenitenceEffect` 标到 `Rage` | 数据侧变了（比如生成器改了 trigger 名）⇒ 先红在这里 |
| 3 | ⑤**正例**：`ResolveAllTimed(Trigger,"Sororitas","SOR10",times,perCardOnly:true,keyword:"penitence")` **含 `PenitenceEffect`** | 🧨 删掉 `CanonicalTriggerName` 的归一 ⇒ 红 |
| 4 | ⑤**反例**：同口径 `SOR8` **含 `PenitenceEffect` 且不含 `FlankEffect`** | 🧨 删归一 ⇒ 退回粗桶 = `FlankEffect` ⇒ 红 |
| 5 | ⑤**灭自证（结构上不可能同时满足的一对）**：同一张 `SOR8`、同一份 `times`，`keyword="Flank"`（**未归口**）⇒ **只有 `FlankEffect`、没有 `PenitenceEffect`** | 与 #4 一起：⛔ 退回粗桶 ⇒ #4 红；⛔ 把归口写成「keyword 非空/已归口就整桶播」⇒ #5 红（`Flank` 会变成 `PenitenceEffect`）。**两条不可能被同一种偷懒实现同时满足** |
| 6 | ⑤**防万金油**：`keyword="Ambush"` 在 `SOR10` 上**什么都不播**（`perCardOnly` 不回兜底） | ⛔ 「已归口就整桶播」/「无条件回 ability 桶」⇒ 红 |

补充：#5/#6 的期望值**不来自 `VfxMap` 的内部判定**（不调 `TriggerNameOf`、不读它的 picks），
全部是「名字集合」的硬期望 + 卡池数据，避免「实现和检测器同一个口」。

---

## 四、没查清 / 判不准

1. **没有跑 Unity**（规则：Unity 批处理只有主对话能跑）⇒ 断言**本代理没有实跑过**，只做到
   ① `bash 工具/typecheck.sh` 两个程序集 **0 错**；② 用真实数据离线模拟判定链（§五）。
   主对话请跑：`unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod AnimFXCheck.Run -logFile -`
   （上一次基线 **79/0**，本批应变成 **85/0**：+6 条）。
2. **没有原版实况**：原版已关服 ⇒ 「Rage 触发播 `PenitenceEffect`」是**反编译链 + 卡 SO 引用**推的
   （`RawCardScript__TriggerAbility.c` → `AbilityLogic__PlayAbility` → `PlayAnim`；`PenitenceTraitTrigger` 的
   `abilityLogic[0].cardAnimations[0]`），⛔ 不是实拍。与 `普查产出_1023c` 报告 §E·1 的结论同一档。
3. **`times = null` 那一档仍走旧口径**：`TryResolveTriggerPicks` 的 ② 分支（没有逐候选时间）**先于**目标判定，
   所以只回 `trigger` 粗桶 —— 这一档**没有**被本修法覆盖。生产路径（`BattleDriver.FireEventAll` →
   `AnimTimesOf(cardId,faction)`）**恒带 times**，今天不受影响；若将来有调用方不带 times 又期待 `PenitenceEffect`，
   它仍然播不出来（⛔ 不在本件范围内改：那是「无时间表时怎么归口」的新裁定）。
4. **`AbilityTrigger` 侧的枚举值**用了 `130`（`AbilityTrigger.Rage`）作为注释判据，**没有**在本机反编译桩里
   重新数一遍（`CardDef.cs:4892-4893` 已引用 `dump.cs:20259` / `0x82`，本件只做**二次引用**）。
5. **播放时刻/观感**（t=0/0.5/0.7 是否手感对）未验 —— 需要真机。断言只管「播哪件」。

---

## 五、验证记录（离线，可复现）

**① 秒级类型检查**（改完两个 `.cs` 之后）：

```text
TMPDIR=/tmp/wf_a86 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

（同时段没有别的代理的半成品错误混进来。）

**② 行尾**：`VfxMap.cs` 全 LF（2180/2180）；`AnimFXCheck.cs` 全 CRLF（1110/1110）——
Edit 没有翻行尾；`git diff --numstat` = `50/4`、`91/0`（不是整篇重写）。

**③ 判定链离线模拟**（用 `cards_engine.json` 的 `animTimes` + `card_vfx_by_card.json` 的 `es[]` 按同一规则推）：

```text
SOR8  penitence -> ['PenitenceEffect', 'Arco-Flagellant Strike']
SOR10 penitence -> ['PenitenceEffect']
SOR21 penitence -> ['PenitenceEffect', 'Death Cult Strike', 'Death Cult Strike']
SOR27 penitence -> ['PenitenceEffect']
SOR32 penitence -> ['PenitenceEffect', 'BulletImpact_4shot_trail']
SOR8  Flank     -> ['FlankEffect']          （灭自证那一条）
SOR10 Ambush    -> []                        （防万金油那一条）
```

**④ 影响面**：全池 `AnimTimes` 里 `TriggerName == "Rage"` 的卡**只有这 5 张**（`SOR8/10/21/27/32`）；
`EvtKind.Trigger` 的**所有**发射点在引擎里都带非空 keyword（`RuleCore.cs:5937/10549/10660/10941` ·
`EffectResolver.cs:6112/6184/6354/9412`）⇒ 新增的 `Rage` 放行只可能命中这 5 张（以及池外的
`Hallowed Martyrs`，同一族、同一修法自然覆盖）。

---

## 六、顺手发现（⛔ 都没改，交主对话分流）

1. **简报路径**：`Assets/CardPresentation/Editor/AnimFXCheck.cs` 不存在，真身在
   `Assets/WarpforgeArena1/Editor/AnimFXCheck.cs`（见 §二注）。
2. **简报说「`trigger` 桶的 `PenitenceEffect`」不准确**：5 张卡的 `PenitenceEffect` 都在
   **`ability` 桶**（`card_vfx_by_card.json`：`ev:"ability"`，`a:"PenitenceTraitTrigger"`）；
   `trigger` 桶只有 `SOR8` 那条 `FlankEffect`。修法本身是「两张桶合并、按 trigger 名筛」，所以结论不变。
3. **`SOR8` 的 `FlankEffect` 在修后可能变成「无人播」**：那条来自 `flank` 的
   `TraitEffect`（`triggerType: 0 = Start`、`playIfWarlord: true`、`effectAnim: FlankTraitStart`），
   生成器按 `TRAIT_TRIGGER_EVENT[0] = trigger` 塞进了 `trigger` 桶。修前它是「忏悔那一拍的错件」；
   修后这一拍不播它（**正确** —— 原版那一拍查的是 `rage` 的 traitEffects = 空数组）。
   但 `TraitFrames` 的两本字典**只收 `2 / 3 / 6`**（`TraitFrames.cs:292`），Start(0) 行没有消费者
   ⇒ **`Flank` 的 Start 特效（含其它 trait 的 Start(0) 行）可能整档都没接**。
   判据 → `trait_collection.json`（flank 那条）+ `trait_effect_bindings.json`（flank 行，`triggerType: 0`）。
   ⛔ 未改：`TraitFrames.cs` 不在本代理白名单，且「Start(0) 在原版哪一刻播」需要主对话裁。
4. **一个潜在陷阱**：`CardDef.HasAnimTrigger("penitence")` 对这 5 张卡**恒为 false**（它比的是
   `TriggerName` = `"Rage"`）。今天 `RuleCore.HurtCore` 里 `FireTriggerOnBoard(..., Penitence)` **没有**
   过这道闸（`RuleCore.cs:7952`），所以没事；**若将来有人把 Penitence 那一发也套上
   `HasAnimTrigger(keyword)`，这 5 张会静默不再触发**（表现层收不到事件）。⛔ 本件没动引擎。
5. **`animfx_sounds.json` / 音效链**：`Trait Penitence` cue 与 `Penitence.wav` 都在（§一最后一段）；
   `PenitenceEffect` 在效果库报告里（`WarpforgeVFX/效果库报告.tsv:707`，控制器 1 个）。
   ⇒ 音效**不是**独立缺口，**同一跳**。
6. **`SOR8` 的 `trigger` 桶里那条 `FlankEffect` 仍在数据里**（本件没删数据）。它现在只在
   「`keyword` 为空 / 未归口」的直调路径上可达；生产路径 `keyword="penitence"` 不再走它。
   ⛔ 按铁律 11 不删：它是 `flank` 的**真** `TraitEffect`（有 CardTrait SO 出处），只是「Start(0) 该在哪播」另账（见 3）。

---

## 七、一句话

**改了什么**：`VfxMap` 加了一处 `penitence → Rage` 的别名归一 + 白名单放行 `Rage`
（判据三条写在 `case "Rage"` 的注释里，映射只此一处），`AnimFXCheck` 加 6 条断言（含一对结构上互斥的灭自证）。
**验了什么**：`typecheck.sh` 运行时/编辑器两个程序集 **0 错**；行尾未翻；用真实卡表 + 逐卡特效表离线重算
5 张卡的判定结果（`SOR8` 不再 `FlankEffect`，5 张全含 `PenitenceEffect`）。
**还差什么**：主对话跑一次 `AnimFXCheck.Run`（期望 **85/0**）与真机看这一拍；`times = null` 那一档的归口、
以及 `EffectTriggerType.Start(0)` 特异的播放时机是**另外两笔**（§四·3、§六·3）。
