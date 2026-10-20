# W58 战斗线 · Slay 状态动画（D6 收口）

> 执行代理：`W58-SLAYVIS`（战斗线）。只改 `CardPresentation/Core/TraitFrames.cs` 一个产品文件；
> `Editor/BattleScene.cs` **被别人占用**（见 §四），断言写成**可粘贴块**。未跑 Unity / `*.Run` / player / git。

---

## 〇、结论（先看这四句）

1. **原版 `AddEffect.c:889-891` 那句 `DisplayStatusAnim(0x2ee)` 在当前 build 是空转。**
   `0x2ee = 750 = DefinedTrait.quest`，而当前 build 的 `TraitsCollection`（`sharedassets0.assets` pid 531，
   **77 条 `TraitPairs`，id 范围 5–1280**）里**根本没有 750** ⇒ `GetTrait(750)` 返回 null ⇒
   `DisplayStatusAnim` 在 `op_Equality(null)` 那一支直接 return（代码逐句见 §一）。
   ⚠️ **不能也不该拿 quest 的框顶替** —— quest 连 `CardTrait` 配置都没有。
2. **Slay 在当前 build 的正确 id = `260`**（`DefinedTrait.slay = 260`，签名桩与 `dump.cs` 两处一致）。
   它挂的唯一一条 `TraitEffect` 是 **`TriggerType = 3 (Trigger)`**、`playIfWarlord = 1`，
   GUID `7dc58e5b008d8804f8fe911400c9bd53` → CardAnim **`Slay_Effect_target`** → prefab
   **`SlayEffect`**（`bundle_battleprefabs_vfxandmisc_assets_all`，GUID `d634c206…`）。
   `Slay_Effect_self` / `Slay_Effect_target` / `Slay_Effect_warlord` 三个 CardAnim **同指这一件**。
3. ⇒ **原版真正的 Slay 演出发生在「Slay 真的触发（击杀）」那一刻**（`TriggerAbility` →
   `PlayTriggerTraitAnim` → `ActivateTriggerTraitAnim` → `DisplayTriggerAnim`），而**不是附魔加身那一刻**；
   `slay` 的 `_hasInPlayAnim = 0`，连 `AddEffect` 通用那条 `_hasInPlayAnim` 闸也不会放行。
4. **本笔按任务要求**把 `slay → SlayEffect` 绑进 **`TraitFrames.PlayFrames`**（OnPlay 那本），
   让附魔加身时演一次、移除时被 `Clean` 清掉。prefab 取的是 **Slay 自己那条 TraitEffect 的 GUID 链**，
   **不是**猜的、也不是 quest 的框。`SlayEffect` **本来就在效果库里**（无需重导）。

---

## 一、trait 750 查证（当前 build）

### 1.1 方法（可复现）

按 `K12` 报告（`资料/普查产出_1019/W_卡牌ID线_K12清零_1019b.md` §1.1）的无 type tree 手工解：

- 文件：`d:/2/unity_run_ref/Warpforge_Data/sharedassets0.assets`（8,407,132 B）
- `CardTraitCollection` = pid **531**（`m_Name = TraitsCollection`，脚本 pathID 3970）
- 73 个 trait 配置 = pid **458–530**（脚本 1795 / 474 是 Duty 1586 / 478 Ferocity 3039 / 497·498 Oath 231）
- `CardTrait` 字段序：`_traitIcon` PPtr(12) · `_hasInPlayAnim` · `_hasIcon` · `_isInPlayIcon` ·
  `_stackable` · `_aIScore` · `customObject`(3 strings) · `_traitEffects` List
- `TraitEffect`（类，List 内联）：`name` string · `effectAnim`(3 strings) · `overrideByArmies`(int + 每项
  `{army int, effectAnim}`) · `TriggerType` int · `playIfWarlord` bool

本轮用 UnityPy（1.25.3）+ 上述偏移重解，**73/73 资产 `consumed == len(raw)` 自校验通过**；
`TraitsCollection` 77 条 `TraitPairs`、**无 750**（逐条 id 表见 §1.3）。

### 1.2 逐条结论

| 问题 | 现读答案 | 证据 |
|---|---|---|
| `0x2ee = 750` 是什么 | `DefinedTrait.quest` | `d:/2/tools/il2cpp_out/dump.cs:45796`（`slay = 260` 在 `:45747`）；`d:/2/Warpforge_code/.../DefinedTrait.cs:74` |
| 750 在 `TraitsCollection` 里有配置吗 | **没有**（77 条 id 里无 750；无名为 Quest 的 `CardTrait` 资产） | 本轮重解 §1.3；与 `K12` 报告 §1.2 的 73 条表一致 |
| 所以 `GetTrait(750)` | 返回 null | `CardTraitCollection__GetTrait.c`：字典 `TryGetValue` 失败返回 0 |
| `DisplayStatusAnim` 遇 null | **直接返回**（什么都不演） | `BattleCardUI__DisplayStatusAnim.c`：`op_Equality(lVar4,0)` 为真则整个方法体不进 |
| Slay 的正确 id | **260** | 同上两处枚举；`TraitsCollection` 里 `'slay' → traitType 260 → config pid 513` |
| Slay 的 CardTrait（pid 513）字段 | `hasInPlayAnim=0` · `hasIcon=1` · `isInPlayIcon=1` · `stackable=10(add)` · `aIScore=0` | 本轮重解 |
| Slay 的 TraitEffect | 1 条：`name='Trigger'`、**TriggerType=3**、`playIfWarlord=1`、GUID `7dc58e5b008d8804f8fe911400c9bd53` | 本轮重解 |
| GUID 落到哪件 | `Slay_Effect_target`（CardAnim）→ prefab **`SlayEffect`** | `数据/索引/anim_address_map.json`（`guid_to_asset` + `cardanim_to_asset`）；`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv:1883` |
| 另外两个变体 | `Slay_Effect_self` / `Slay_Effect_warlord` **同指 `SlayEffect`** | `anim_address_map.json` 的 `cardanim_to_asset` 三行 |
| prefab 在不在我们效果库 | **在**：`效果库报告.tsv:827`（判定 `Z` 对得上 · 置信度中 · 亮度比 1.08 · destroyTime 4.0 · 自然时长 6.20）· `Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 有项 · `Assets/WarpforgeVFX/Prefabs/SlayEffect.prefab` 存在 | 现读 |

### 1.3 `TraitsCollection` 77 条 id（本轮现读，只摘与本笔相关）

```
 260 'slay'   -> CardTrait pid 513
 750  （不存在）
 260 之外与本笔相关的：350 'blast' -> 463 · 120 'backlash' -> 462 · 220 'bloodThirst' -> 465
最小 id 5 ('ephemeral')，最大 1280 ('sentry')
```

> 注：`TraitsCollection` 里另有 `berzerk(360)` / `dodge(240)` 两条 `config = {0,0}`（空引用，原版数据如此）。

### 1.4 为什么「不能拿 quest 顶替」

- quest(750) **在 `TraitsCollection` 里没有任何 `CardTrait` 配置** ⇒ 它没有 `_traitEffects`、没有 GUID、没有 prefab；
  顶替等于凭空造一件美术（违反铁律 11 的「判据是空的也可以不做」前提，而且会是**错的那件**）。
- `AddEffect` 那一句 750 是**原版自己数据/代码的一条陈旧引用**（当前 build 空转）；
  把它的 id 语义反过来当成「Slay 的框」是把 bug 当判据。

---

## 二、改法（符号）

**文件：`Unity/MyGame/Assets/CardPresentation/Core/TraitFrames.cs`**

1. `PlayFrames` 表新增一行：

```csharp
{ KeywordTable.Slay, "SlayEffect" },
```

   - 绑定的**来路**：Slay 自己那条 `TraitEffect` 的 GUID 链（§1.2），**数据硬绑**；
     与 stealth/swarm/vanguard 同一口径（不是从运行时实况证出来的）。
   - 行上注释完整写清了「原版那句 750 空转」「slay=260」「TriggerType=3」「prefab 来源」「不是 quest 顶替」。
2. 文件头补齐一条 **2026-10-20 就地订正**（铁律 5）：原文「`CardTrait` 那份 SO 本体全库 0 命中」
   **不成立** —— 资产在 `sharedassets0.assets`，只是 `assets_full` 的根文件 JSON 导出缺口；
   并保留「表仍不许当成『原版就是这样』」的警告。

**没有改 `BattleDriver.cs`**：引擎侧不需要新事件 —— `GrantEmbeddedCore` 已 `AddKeyword("slay",1)`，
`SyncKeywords` 按集合对差可见；Slay 击杀时引擎本来就发 `EvtKind.Trigger + Keyword="slay"`
（`RuleCore.TryCreditKill` → `FireTriggerAt(..., KeywordTable.Slay, ...)`，`RuleCore.cs:4617`）。

**两态**：
- 附着：关键词集合里出现 `slay` ⇒ `SyncKeywords` → `DisplayStatus("slay")` → `Play("SlayEffect")`，写进 `_play`。
- 移除（`untilEndOfTurn` 到期）：集合里没有 `slay` ⇒ `Clean(now,false)` → `Drop` → `Exit()` 并移出表。

---

## 三、断言（可粘贴块）

> ⚠️ **未插入 `Editor/BattleScene.cs`** —— 该文件 **18:51 刚被别的 W58 代理改过**（`git status` 显示 `M`，
> mtime `2026-10-20 18:51:33`，同批还有 `W58_…断言插入.md` 几份报告），按「一个文件同一时刻只有一个写手」留给调度台合并。
> 位置：**紧接现有「状态框（trait frame）」那一节 ⑥ bloodthirst 之后**（`BattleScene.cs` 的 `RemoveKeyword(KeywordTable.BloodThirst,1)` 那几行之后、
> 该 `if (tv != null) { … }` 块收尾之前）。上下文变量：`ctx` / `driver` / `tfSlot` / `Check` / `KeywordTable` / `TraitFrames` 都已在作用域。

```csharp
                // ⑦ 🆕 2026-10-20（W58 · `SLAYVIS`）：**`slay` 状态框接上了**
                //   原版「Slay 附魔加身演一下」的调用点 = `CardScript__AddEffect.c:889-891`（`DisplayStatusAnim(0x2ee)`）。
                //   判据 = Slay 自己那条 `CardTrait`（`sharedassets0.assets` pid 513）的唯一 TraitEffect：
                //     GUID `7dc58e5b008d8804f8fe911400c9bd53` → CardAnim `Slay_Effect_target`
                //     → prefab `SlayEffect`（`bundle_battleprefabs_vfxandmisc_assets_all`；已在效果库里）。
                //   🔴 原版那句传的 `0x2ee = 750` 在当前 build 是 `DefinedTrait.quest`，且它**不在
                //     `TraitsCollection` 里**（77 条 TraitPairs 现读无 750）⇒ 原版这一句**实际空转**；
                //     我们按「附魔加身演一次」的意图绑进 OnPlay 那本（来路见 `TraitFrames.cs` 的 `slay` 行）。
                Check(TraitFrames.PrefabFor(KeywordTable.Slay, false) == "SlayEffect",
                      "★ `slay` 绑定的框 = `SlayEffect`（Slay 那条 TraitEffect 的 GUID 链解出来的那一件）");
                Check(WarpforgeVFX.WarpforgeEffectLibrary.Available
                   && WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet("SlayEffect", out _),
                      "★ `SlayEffect` **真的在效果库里**（表里有行 ≠ 运行期加载得到）");
                ctx.Players[1].Board[tfSlot].AddKeyword(KeywordTable.Slay, 1);
                driver.RefreshAll();
                var tvSlay = driver.FoeUnits[tfSlot];
                var tfSlay = tvSlay != null ? tvSlay.GetComponent<TraitFrames>() : null;
                Check(tfSlay != null && tfSlay.HasPlayFrame(KeywordTable.Slay),
                      $"★ 关键词「slay」加上来 ⇒ OnPlay 那本里有了它（现有 {(tfSlay != null ? tfSlay.PlayCount : -1)} 张）");
                // 移除那一态（`untilEndOfTurn` 到期）：关键词没了 ⇒ `CleanStatusAnims(false)` 清掉
                ctx.Players[1].Board[tfSlot].RemoveKeyword(KeywordTable.Slay, 1);
                driver.RefreshAll();
                Check(tfSlay != null && !tfSlay.HasPlayFrame(KeywordTable.Slay),
                      "★ slay 撤掉（untilEndOfTurn 到期）⇒ 那张框被清掉（同一条 Clean 谓词）");
```

**灭自证**：第一条钉的是**精确 prefab 名**（`"SlayEffect"`，来自数据链），不是「非 null」——
把 `TraitFrames` 里那行 `{ KeywordTable.Slay, "SlayEffect" }` 删掉/改回缺失 ⇒ `PrefabFor` 返回 null ⇒ 第一条红；
把 `DisplayStatus` 的播放删掉 ⇒ 第三条 `HasPlayFrame` 红；把 `Clean` 那条谓词改坏 ⇒ 第四条红。
第三条/第四条用的是 **`_play` 字典的实际内容**（不是返回值自证）。

---

## 四、没查清的部分 / 发现的问题

1. ⚠️ **`Editor/BattleScene.cs` 被占用**（mtime 18:51，同批多个「断言插入」代理）⇒ 断言按 §三 交给调度台合并，
   本笔没有动这个文件（行尾 CRLF，手改风险也高）。
2. 🔴 **顺手发现（真缺陷，未改 —— 不在本笔判据范围、会动别人的断言）：**
   `TraitFrames.DisplayTrigger` **对所有触发类框都写进 `_trigger` 字典**（`TraitFrames.cs` 的 `_trigger[key] = p`），
   而原版 `DisplayTriggerAnim` 的回调**只对 `TriggerType == 6 (TriggerOnPlay)` 写表**
   （`BattleCardUI.__c__DisplayClass91_1___<DisplayTriggerAnim>b__0.c`：`if (*(int*)(..+0x28) != 6) return;`）。
   ⇒ 原版 **Trigger(3) 每触发一次演一次**（不进表），我们是「这个关键词一辈子只演第一次」。
   这正是**不把 `slay` 绑进 `TriggerFrames`** 的原因之一（Slay 的 effect 就是 Trigger(3)，
   绑了会被这条缺陷变成「全局只演一次」）。要修得同时改那两条依赖 `TriggerCount` 的既有断言
   （`oath` / `bloodthirst`，`BattleScene.cs` 现写 `TriggerCount == 1/2`），属跨宿主改动 ⇒ 记账，等调度台排。
   一并记：`oath` 的 effect 是 Trigger(3)，按原版它**不该**留在 `_trigger` 里。
3. **`SlayEffect` 的画质**：`效果库报告.tsv` 判定 `Z`（对得上）、置信度 **中**、亮度比 1.08 —— 可用，
   但「中」不是高置信；真机验收时建议顺带看一眼它与原版的差异（本笔按铁律 12 没跑 Unity / 没截图）。
4. **`Vanguard` 那条的旧注释已顺带订正**：原版 `CardTrait.Vanguard` 的 OnPlay effect 是
   `VanguardIdleAnim → VanguardIdleEffect`，与我们绑的具名 `vanguardFrame`（`Vanguard Frame Animated VAT`）
   是**两套东西**（`K12` §四·2）—— 本笔只在文件头加了提示，未改绑定（不属本笔）。
5. **原版附魔加身那一刻到底演不演**：当前 build 的判据是**否**（750 无配置 + `_hasInPlayAnim=0`）。
   我们这条绑定是「按调用点意图」的口径，已在代码注释与报告里都标了，**不是**「原版实况证出来的」。

---

## 五、改动清单

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Core/TraitFrames.cs` | `PlayFrames` 新增 `{ KeywordTable.Slay, "SlayEffect" }` + 文件头 `CardTrait` 判据订正（31 增 / 4 删；LF 未翻） |
| （未改）`Editor/BattleScene.cs` | 被别人占用 ⇒ 断言见 §三 可粘贴块 |
| （未改）`Battle/BattleDriver.cs` | 引擎事件已就绪，无需动 |

---

## 六、typecheck

```
TMPDIR=/tmp/wf_slayvis bash d:/4/Unity/工具/typecheck.sh
运行时程序集：0 错误
编辑器程序集：0 错误
```

`git diff --numstat`：`TraitFrames.cs 31/4`（无整篇行尾翻转；该文件是 LF）。
