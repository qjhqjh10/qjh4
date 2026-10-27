# T4 · 532 `ResolveRemoveEffect`（表现层两半）与 Premium Campaign SO —— 判据

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 范围：① `532 ResolveRemoveEffect` 缺的两个引擎侧输入（`ActiveCardEffect.Trait` 列 + 「一条效果被摘掉」的 `EvtKind`）；
> ② `Premium Campaign` 系列 SO（"15 个只抄了 UM"）。每条固定四段：原版做法 / 我们现状 / 施工建议 / 怎么验 + 还差什么。

---

## 摘要（先看这几句）

- **1a（Trait 列）**：数据**已经在** `cards_truth.json` 的 `abilities[].logic[].buff.trait`（`gen_cards_truth.py:148-163` 的 `compact_buff` 逐键照抄，**不需要改生成器**）。缺的是 **引擎运行期读的那份 `cards_engine.json` 里没有这一列**（`CardDef` 只读 `cards_engine.json`，`CardDatabase.ResourcePath = "cards_engine"`）⇒ 要把「附魔效果挂的 trait」落进引擎能读到的地方，**改的是 `gen_cards_engine.py`，不是 `gen_cards_truth.py`**（见 §1·③ 的订正）。
- **1b（表现信号）**：**原版没有「摘效果」广播** —— 它靠动作队列里 `actionType = 0x10 (removeEffect)` 的那一条**当场内联执行**（`ResolveAction` case 0x10 → `CardScript.RemoveEffect` + 4×`CancelAnimFromUnitFromQueue` + `RemoveDestroyableAnims` + `FinishResolvingAction`）。所以我们的等价「信号」= 我们那条 `removeEffect` 动作**出队那一刻**；因此 `EvtKind` 要**新开一档**（建议 `EnchantmentRemoved`）。
- **2（Premium Campaign）**：🔴 **「15 个」是错的（铁律 5 订正）** —— `bundle_menus_assets_all/MonoBehaviour/` 里名字带 `Premium Campaign` 的 **共 14 个文件** = **13 个阵营各一件（`PremiumItem`）+ 1 个 `Premium Campaign Store`（LiveOps 商店定义，另一个脚本）**。报告里「13 阵营 + Store + UM = 15」**把 UM 数了两遍**。真实缺口 = **13 个阵营的 `PremiumItem` SO 本地全在、我们只引用了 `premium_campaign_um`（且只在注释里，运行时 0 引用）**。
- ⚠️ **别把 `Premium Campaign SO` 和「战役节点表」混为一谈**：`CampaignData.cs` 说「本地只有 UM 一套」说的是 **`Campaign Node Data*.json` + `Ultramarines Campaign.json`（节点表/CampaignEventData）**，那是**原版本地本来就只有 UM**（`NodeId` 全是 `UM` 前缀）⇒ **不是我们的缺口**。真缺口是那 13 个 `PremiumItem`。

---

# 条目 1 · `532 ResolveRemoveEffect` 的两个引擎侧输入

## ① 原版做法（逐句 + 出处）

### 1·0 调用链与「表现层怎么知道」（很关键）

原版 `ResolveRemoveEffect` 是**动作队列的同步解析体**，**没有广播**：

- **动作入队**：`BattleManager.RemoveEnchantment` 只组装一条 `BattleAction{actionType = 0x10 (removeEffect)}` 带 `(carrier, effect)` 两个对象引用，再 `AddAutoActionToQueue(action, priority)`（账 481 已核，`decomp_full/BattleManager__RemoveEnchantment.c`）。
- **出队执行**：`BattleManager.ResolveAction` 的 **case 0x10** 里**内联**了整段（`decomp_full/BattleManager__ResolveAction.c:1640-1738`）：
  ```
  CardScript.RemoveEffect(local_338, effect, 0, 0);                         // :1726
  CancelAnimFromUnitFromQueue(manager, local_338, actionQueue);             // :1727  ← ①actionQueue
  CancelAnimFromUnitFromQueue(manager, local_338, instantQueue);            // :1729  ← ②instantQueue
  CancelAnimFromUnitFromQueue(manager, local_338, interruptQueue);          // :1731  ← ③interruptQueue
  CancelAnimFromUnitFromQueue(manager, local_338, delayedInstantQueue);     // :1733  ← ④delayedInstantQueue
  RemoveDestroyableAnims(manager, local_338, effect, 0);                    // :1735
  FinishResolvingAction(manager, 0x10, 0);                                  // :1736-1737（goto LAB，动作类型 0x10）
  ```
  ⇒ 独立方法 `decomp_full/BattleManager__ResolveRemoveEffect.c:1-19` 与它**逐句同形**（`decomp_full` 里 `ResolveRemoveEffect` **0 个直接调用点**，因为它被内联进 `ResolveAction` case 0x10 —— **不是死代码**，`FinishResolvingAction(0x10)` 就是它可达的证据）。
- **带字段名的 ILSpy 对照体**：`cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:26290-26299`（`ResolveRemoveEffect`）/ `:26301-26346`（`RemoveDestroyableAnims`）。
- **表现层没有任何「事件」**：原版是**单体对象图**，`CardScript.RemoveEffect` 自己在体内改完状态后**直接调表现方法**（`CardScript__RemoveEffect.c` 的 `CardScript__UpdateTraitIcons` `:82`/`:275`、`CardScript__UpdateFigures` `:140`/`:180`/`:186`/`:229`、`CardScript__AddArmour` `:179`）；`RemoveDestroyableAnims` 直接摸 `BattleCardUI.TraitFrameEffectsPlay(+0x2d0)` / `TraitFrameEffectsTrigger(+0x2d8)` 两本字典（`cpp2il BattleManager.cs:26309-26345`）。
- **对照（别混）**：`ResolveRemoveEffectsFrom`（动作类型 `0x2f`=47）**才有广播** —— `BattleManagerSupport.BroadcastRemoveEffectsFrom`（`decomp_full/BattleManager__ResolveRemoveEffectsFrom.c:1-8` → `BattleManagerSupport__BroadcastRemoveEffectsFrom.c:1-60`，棋盘 `IsInPlay` 全员 + 双方手牌逐卡 `RemoveExtrinsicEffectsFrom`）。**那是「按来源摘全部」，不是逐条 removeEffect**。

### 1·1 `cardEffect.trait(+0x3C)` 原版怎么用

**唯一用途 = 作状态框两本字典的键**（`cpp2il BattleManager.cs:26304-26345`，`decomp_full/BattleManager__RemoveDestroyableAnims.c`）：

```csharp
object obj = cardEffect.buffType - 15;
if ((nint)obj > 1) return;                                  // ← 闸：只认 buffType ∈ {15,16}
BattleCardUI ui = targetCard.CardUI;
if (ui.TraitFrameEffectsPlay.ContainsKey(cardEffect.trait)) {      // +0x2d0
    var go = ui.TraitFrameEffectsPlay[cardEffect.trait];
    if (go != null) {
        var fx = go.GetComponent<AnimFXController>();
        if (fx != null) fx.DoDestroy();                     // ⚠️ 没组件 ⇒ 不销毁
        ui.TraitFrameEffectsPlay.Remove(cardEffect.trait);  // 但字典照样摘
    }
}
if (!ui.TraitFrameEffectsTrigger.ContainsKey(cardEffect.trait)) return;  // +0x2d8；同上两跳
```

- `trait` 是 **`DefinedTrait` 枚举数值**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`）。
- **不参与任何数值结算** —— 这里只拿它找两件状态框 GameObject、`DoDestroy()` 并摘字典。
- 与我们的等价实现 `CardPresentation/Core/TraitFrames.cs:1338-1357`（`RemoveDestroyable` / `DestroyableOne`，三个「照抄的怪形状」已写在其上注释）逐句对齐。
- `CancelAnimFromUnitFromQueue`（`decomp_full/BattleManager__CancelAnimFromUnitFromQueue.c:25-81`）= 从队尾往前扫，撤 `targetCard == unit && actionType == 0xB (playAnim)` 的动作。

### 1·2 命中面（现读 `cards_truth.json`，1,351 张全表逐卡扫）

- **`buffType ∈ {15,16}` 的 buff 条目共 57 条**；其中**带 `trait` 的 28 条**（全卡扫：`abilities` / `activeAbility` / `alternativeAbility` 都扫过 —— `cards_truth.json` 里这 28 条全在 `abilities[].logic[].buff`）。
- **28 条 = 2 个 trait 值**：
  - **`480 vulnerable`（4 条）** —— 卡名都叫 **`Spreading Corruption`**，但**是两个不同 id**：`875eedc9…`（**不在池**，`cards_engine.json` 查不到）3 条 + **`BL74`（在池）** 1 条。`traitValue = 4`（全 4 条）。
  - **`1185 pray`（24 条 = 23 张卡，`SOR29 Ephrael Stern` 有 2 条）** —— **全部在池**；`traitValue` 缺省（0）。
- **映射判据**：`DefinedTrait.cs:48 vulnerable = 480`、`DefinedTrait.cs:117 pray = 1185`。`pray` 这个数字→名字在 `cardTraits` 里也能兜出来（23 张 SOR 卡的 `cardTraits[].{name:"pray",_trait:1185}`），**但 `480` 在真值 `cardTraits` 里 0 命中**（`vulnerable` 那两张 Spreading Corruption 的 `cardTraits = null`）⇒ **数字→名字的权威只能是 `DefinedTrait.cs`**。

**28 条逐条（`cards_truth.json` 现读；`path` = 卡内路径）**

| # | 卡 id | 卡名 | army | buffType | trait | traitValue | path | 在池 |
|---|---|---|---|---|---|---|---|---|
| 1 | 875eedc9… | Spreading Corruption | 50 | 15 | 480 | 4 | /abilities[0]/logic[1]/buff | 否 |
| 2 | 875eedc9… | Spreading Corruption | 50 | 15 | 480 | 4 | /abilities[0]/logic[2]/buff | 否 |
| 3 | 875eedc9… | Spreading Corruption | 50 | 15 | 480 | 4 | /abilities[1]/logic[0]/buff | 否 |
| 4 | **BL74** | Spreading Corruption | 50 | 15 | 480 | 4 | /abilities[0]/logic[1]/buff | **是** |
| 5 | SOR1 | Erika Luminas | 80 | 15 | 1185 | — | /abilities[1]/logic[0]/buff | 是 |
| 6 | SOR13 | Simulacrum Bearer | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 7 | SOR15 | Celestian | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 8 | SOR17 | Sister Superior | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 9 | SOR18 | Dialogus | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 10 | SOR2 | Junith Eruita | 80 | 15 | 1185 | — | /abilities[1]/logic[0]/buff | 是 |
| 11 | SOR20 | Hospitaller | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 12 | SOR24 | Simulacrum Imperialis | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 13 | SOR25 | Imagifier | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 14 | SOR26 | Aestred-Thurga | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 15 | SOR29 | Ephrael Stern | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 16 | SOR29 | Ephrael Stern | 80 | 15 | 1185 | — | /abilities[1]/logic[0]/buff | 是 |
| 17 | SOR3 | Morvenn Vahl | 80 | 15 | 1185 | — | /abilities[1]/logic[0]/buff | 是 |
| 18 | SOR30 | Celestian Sacresant | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 19 | SOR34 | Celestian Superior | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 20 | SOR35 | Immolator | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 21 | SOR37 | Triumph of Saint Katherine | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 22 | SOR4 | Sister Novitiate | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 23 | SOR5 | Battle Sister | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 24 | SOR6 | Battle Sister Gunner | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 25 | SOR65 | Celestian Sacresant Aveline | 80 | 15 | 1185 | — | /abilities[1]/logic[0]/buff | 是 |
| 26 | SOR67 | Sacred Rose Sister | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 27 | SOR68 | Ministorum Priest | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |
| 28 | SOR69 | Zephyrim Superior | 80 | 15 | 1185 | — | /abilities[0]/logic[0]/buff | 是 |

> 注：**`pray` 是能真命中的那个** —— 祈祷附魔 `Pray Enchantment`（id `9218576f…`）**是唯一已经进池的附魔模板**（`gen_cards_engine.py` 的 `ENCHANTMENT_EXECUTORS` 只收它），而它被 23 张 SOR 卡以 `buff{buffType 15, trait 1185, enchantment → 它}` 挂上 ⇒ 引擎侧一接上，**索里塔斯那 23 张卡就会命中**（不是理论值）。`vulnerable`(480) 那 4 条所在的附魔模板**没进池、也没有执行点** ⇒ 今天挂不上（见 §1·④）。

## ② 我们现状（落点 + 差什么）

| 段 | 原版 | 我们 | 差什么 |
|---|---|---|---|
| ① `RemoveEffect` | `CardScript.RemoveEffect`（撤属性/附魔 + 就地刷 `UpdateTraitIcons/UpdateFigures`） | **有**：`Enchantments.Remove`（`RuleEngine/Core/Enchantment.cs:287`，含 `Permanent(+0x74)` 拒绝闸 `:290`）+ `UnitState.RevertGrantsFrom` 分担属性那半 | ✅ 已有（账 481 的四个生产落点已切到排队摘） |
| ② 四条道撤 `playAnim` | `CancelAnimFromUnitFromQueue` ×4 | 表现层**入口已就位**：`BattleDriver.CancelPendingAnimsOf(side, slot)`（`CardPresentation/Battle/BattleDriver.cs:34024`，等价实现 = 倒序扫 `_timeline` 撤 `cueVfx`） | 🔴 **0 调用点**：原版两处活调用点都在「某张卡上的效果被移除」族，**我们没有那条引擎信号** |
| ③ `RemoveDestroyableAnims` | buffType 15/16 ⇒ 按 `cardEffect.trait` 销毁两件状态框 | 表现层**入口已就位**：`BattleDriver.RemoveDestroyableAnims(player, slot, trait)`（`:33759`）+ `TraitFrames.RemoveDestroyable`（`Core/TraitFrames.cs:1338`） | 🔴 **0 调用点**；且引擎侧拿不到 `trait`（`ActiveCardEffect` 没这一格） |
| ④ `FinishResolvingAction(0x10)` | 标动作完成 | ✅ 有（泵点） | — |
| **1a 数据**：`ActiveCardEffect` 没有 `Trait` | 原版 `CardEffect.trait(+0x3C)` | `ActiveCardEffect`（`RuleEngine/Core/Enchantment.cs:67-100`）只有 `EnchantmentCard / BuffType / Used / Permanent / Source / ActingUnit / Enchantment` | **缺 `Trait` 一格**，且没有任何数据源把它喂进来 |
| **1b 信号**：`EvtKind` 缺一档 | （原版无枚举，靠内联方法调用） | `EvtKind` 全表 = `RuleEngine/Core/BattleEvent.cs:25-389`，**20 个成员**（Play·Deploy·Attack·Hit·Death·Ability·Trigger·Return·GainFaith·GainSpirit·GainQuest·CollectWaystone·Draw·AmbushExit·SecretOrder·Heal·Retaliation·ShowCardsWindow·StatusApplied·TraitRemoved） | **没有「一条效果被摘掉」这一档**（`TraitRemoved` 是「**关键词**从卡上被摘」= `UnitState.RemoveAll` 那条，**别混**） |

**「我们只抄了 UM」的说明**：不适用于本条（本条无阵营维度）。

## ③ 施工建议

### 1a · 加 `ActiveCardEffect.Trait`（引擎侧）

**数据源 = 附魔「效果」自带的 trait，不是附魔卡自己的 trait**（关键：`Pray Enchantment` 模板本身 `cardTraits=[]`，`trait 1185` 是**挂它的那张 SOR 卡的 buff** 上写的）。

| 方案 | 落点 | 判据 | 评价 |
|---|---|---|---|
| **A（推荐 · 数据驱动）** | `cards_engine.json` 每个**附带魔效果的卡**加一列 `enchantTrait`（字符串，关键词名）；`CardDef` 加 `EnchantTrait`；`Enchantments.Attach` 读 `enchantmentCard.EnchantTrait` | 生成器 = **`工具/gen_cards_engine.py`**（`SRC=card_stats.json`、`TRUTH_CARDS_SRC=…/cards_raw.json`，`:33`/`:862`）；它本来就已在读 `cardAbilities[].abilityLogic[].buffEffect`（建 `ENCHANTMENT_EXECUTORS` 那段 `:3956-4060`），只需多落一列：`buffEffect{buffType∈{12,15,16}}.trait`（数字）→ **`DefinedTrait` 数字→枚举名**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`）→ 归一化成关键词名（`vulnerable` / `pray`） | 与「一切数据驱动」一致；一次覆盖 2 个 trait、将来新附魔自动生效 |
| **B（最小 · 直传）** | `Enchantments.AttachPray` 调 `Attach(..., trait: "pray")`；`Attach` 新形参 `string trait = null` 写进 `ActiveCardEffect.Trait` | 数据旁证：23 张 SOR 卡 buff 全 `trait 1185`（`DefinedTrait.cs:117 pray`）+ 24 条 | 只解决今天能命中的 `pray`；`vulnerable` 那条将来接执行点时要再补 —— **不算完整**（铁律 11） |

> ⚠️ **2026-10-27 现核订正（执行代理 `E7ENG`，落地时发现；判据理据不变、只是「从哪张卡读」这一句错了）**
>
> 上面方案 A 那格写的「`Enchantments.Attach` 读 **`enchantmentCard`**.`EnchantTrait`」—— **按字面实现会永远读到空**。
> 原因：`Pray Enchantment` 模板自己的 `cardTraits = []`、它自己的 `cardAbilities` 里**没有** `buffEffect`；
> `trait 1185` 写在**挂它的那张卡**（23 张 SOR）的 `cardAbilities[].abilityLogic[].buffEffect` 上 ——
> 这一点本文件 §1·2 自己也已经写明（「`trait 1185` 是**挂它的那张 SOR 卡的 buff** 上写的」）。
> ⇒ **现行落法（已落地）**：
> 1. `cards_engine.json` 的 `enchantTrait` 列落在**载体卡（挂载方）**身上（v33 起；落值 24 张 = `pray` 23 张 SOR ＋ `vulnerable` 1 张 `BL74`）；
> 2. `Enchantments.Attach` 的取值次序 = **① 调用方显式传的 `trait` → ② 载体卡的 `CardDef.EnchantTrait` → ③ 附魔模板的 `CardDef.EnchantTrait`（兜底，今天恒空）**；
> 3. `null` = **未知**（旧表 &lt; v33）· `""` = **明确没有** —— 未知时 `Attach` **出声**，⛔ 不静默当「没有」。
> 落地记录 → `资料/完全复刻/报告/B9_E7ENG_532引擎侧与EvtKind.md` §2·2 / §④·1。

> 🔴 **铁律 5 订正（生成器归属）**：任务简报写「`工具/gen_cards_truth*.py`」。实况是：
> - **`gen_cards_truth.py` 不需要改** —— 它的 `compact_buff`（`:148-163`）是「`if is_set(v): out[k]=v`」**逐键照抄**，`trait` / `traitValue` **早已落进 `cards_truth.json`**（现读确证）。
> - **真正要改的是 `gen_cards_engine.py`** —— 因为引擎运行期**只读 `cards_engine.json`**（`RuleEngine/Data/CardDatabase.cs:18` `ResourcePath = "cards_engine"`），而 `cards_engine.json` **没有 buff 任何列**（现读 `cards` 元素 key 表：`id/name/type/…/activeAbilityLogic/…`，无 `buffType`/`trait`）。`cards_truth.json` 只是「旁路真值」，**引擎今天 0 处读它**（全仓 `grep cards_truth` 只命中注释）。

**建议的具体落点（方案 A）**：
1. `工具/gen_cards_engine.py`：主循环落新列 `enchantTrait`（每卡一条；该卡没有 buffType 12/15/16 的 buff 时落 `""`）；数字→名字用 `DefinedTrait.cs`（**别用 `cardTraits` 兜** —— `480` 在真值 `cardTraits` 里 0 命中）。
2. `RuleEngine/Core/CardDef.cs`：加 `public string EnchantTrait`（`null`/`""` = 没有；⚠️ **别用 `""` 冒充「未知」**，按本仓既有契约写清「没有」与「未知」的区别）。
3. `RuleEngine/Core/Enchantment.cs`：`ActiveCardEffect` 加 `public string Trait;`（类里注释标原版 `CardEffect.trait +0x3C`）；`Attach` 加形参并赋值。
4. `Enchantments.AttachPray`：无需手写 `"pray"` —— 走**载体卡**的 `CardDef.EnchantTrait`（⚠️ **不是** `enchantmentCard`，见上面 2026-10-27 订正；`Attach` 内部按「显式实参 → 载体卡 → 模板」兜）；若卡表还没这一列，**如实出声**（红线），⛔ 别静默。

### 1b · 加 `EvtKind.EnchantmentRemoved`（表现信号）

**放在枚举末尾**（`BattleEvent.cs` 那段「只许追加、不许插队」的纪律）。建议字段约定：
- `Player` / `Slot` = **载体（被摘效果的那张卡）**在棋盘上的格位（`Enchantments.BoardOwnerSlotOf` 现成，`Enchantment.cs:966`）
- `CardId` = **trait 关键词名**（`vulnerable` / `pray`；与 `TraitRemoved`/`StatusApplied` 同一约定 —— 引擎把关键词塞 `CardId`，⛔ 不是 `Keyword`）
- `Amount` = **buffType**（15/16）；消费端据此判原版那道闸
- `Effect` = 附魔卡名（诊断）
- `InstanceId` = 载体实例号（`CardInstance.Id`）

**要动的点（清单）**：

| 类别 | 文件:行（现读） | 改什么 |
|---|---|---|
| **发射点（唯一）** | `RuleEngine/Core/Enchantment.cs:287` `Remove` 成功分支（`wasThere == true`） | 摘成功之后 `ctx.Emit(EvtKind.EnchantmentRemoved, player, slot, trait, amount: buffType, effect: 附魔卡名, instanceId: …)`。**一个口覆盖全部四个生产落点**：`EnqueueRemove` 出队体 / `RemovePray` / `RemoveByCard`（今天 0 生产调用点）/ 直调 `Remove` —— 四条都汇进 `Remove`。⚠️ `EffectResolver` 残骸整表 `Clear()`（`EffectResolver.cs:16412`）**不经过 `Remove`**，原版那条也不走 `ResolveRemoveEffect` ⇒ **不该发**，别加。 |
| **消费点①（撤 cue）** | `CardPresentation/Battle/BattleDriver.cs:22436` `PlaySignal`（在 `:22459` `StatusApplied` 那一支旁边加新分支） | `CancelPendingAnimsOf(e.Player, e.Slot)` —— **无条件**（原版 ② 不按 buffType 闸）。 |
| **消费点②（帧特效）** | 同上 | `if (e.Amount == 15 \|\| e.Amount == 16) RemoveDestroyableAnims(e.Player, e.Slot, e.CardId);`（原版闸 `(buffType-15) > 1` 在方法体第一句；我们的入口 `:33759` 不重复判、由调用方判 —— 见它的 doc）。 |
| **时长登记** | `CardPresentation/Core/EventTiming.cs:475-502` `ZeroDurationKinds` | **加进去**（原版 `ResolveRemoveEffect` 是同步方法、不 `yield`，与 `TraitRemoved`/`StatusApplied` 同档）。不登记 ⇒ 会落进「未登记事件种类」告警（静默副作用）。 |
| **枚举计数断言** | `RuleEngine/Editor/RuleEngineTest.cs:15942-15953` | 现在钉的是 **19 + TraitRemoved → 20**；加这一档要变 **21**（`kinds.Contains("EnchantmentRemoved")`）。 |
| **`ToString`（可选）** | `RuleEngine/Core/BattleEvent.cs:687-717` | 加一个 `case` 便于日志（不加也行）。 |

> ⚠️ `TraitRemoved`（`BattleEvent.cs:388`）**不能复用**：它是「**关键词**从 `UnitState` 上被摘」（`EffectResolver.EmitTraitRemoved` / `RuleCore` 回合末摘标记光），而这里要表达的是「**一条附魔 effect 被摘**，且 `buffType` 决定要不要销毁它的状态框」。两条的 `Amount` 语义也不同（那条是「值」、这条是「buffType」）⇒ **单开**（与仓里「别把两条合并成一条带 bool 的事件」的纪律一致）。

## ④ 怎么验 + 还差什么

**验**（改完）：
1. `bash 工具/typecheck.sh` 过（几秒）。
2. **`RuleEngineTest.Run`**：`TestPrayEnchantmentX13` 必跑（它断言「自己回合开始 +1 信仰 → 附魔被摘 / 真受伤 → 摘 / 被盾挡下 0 伤 → 不摘」，本批把三处改成排队摘，出队点在 `BeginTurn` / `ApplyDamage` 的 `ActionScope` 退出）。再加一条断言：**摘成功后 `ctx.Signals` 里有一条 `EvtKind.EnchantmentRemoved`，`CardId == "pray"`、`Amount == 15`、`(Player,Slot)` = 载体格**。
3. `ruleprobe play check` / `play all dump` 指纹会变（新增纯表现事件**不进** `play all dump` 的 23 列 —— 那是结算层；若指纹不变是对的，⛔ 别拿「指纹没变」当「没生效」的证据；改用 Signals 断言）。
4. 表现层：人造夹具 —— 给一张带 `pray` 的 SOR 卡挂 `TraitFrames`、让 `_trigger["pray"]` 有件（`Pray_Idle`），摘附魔后断言 `HasTriggerFrame("pray") == false` 且那件 `DoDestroy` 被调（`WarpforgeEffectPlayer` 的 `DoDestroy` 已有入口）；再断言 `_timeline` 里演在该卡的 `cueVfx` 被 `CancelPendingAnimsOf` 撤掉。

**还差什么 / 边界（如实）**：
- **`vulnerable`(480) 那 4 条今天挂不上**：它们引用的附魔模板**没进池**、也没有执行点（`gen_cards_engine.py` 的 `ENCHANTMENT_EXECUTORS` 只收 `Pray Enchantment`）。⇒ 加完 `Trait` 列 + 信号后，**能命中的只有 `pray` 那 23 张卡**；`vulnerable` 要等「被动 `cardAbilities[].abilityLogic[]` 解释器」把 `buffOthers(30)` 的 `buffType 15` 附魔挂上去（那是另一件，缺口清单见 `资料/普查产出_1025/X20_附魔能力表.tsv`）。
- **`traitValue` 不参与本方法**（`RemoveDestroyableAnims` 只用 trait 当键）⇒ `vulnerable` 的 `traitValue=4` 在 532 这条链上用不到；别顺手把它塞进别处。
- **`RemoveSelfBuffsFromQueues` 的 `shouldIgnoreDestroy:1`** 是**账 483** 的另一半，**不属于 532**，别混进来。
- `EnqueuedRemove` 的 `lane` 仍按「该族数据真值」取（密令 2 / 祈祷 1），没有「逐 ability 读 `priority(+0x44)`」的通用口 —— 属账 481(a)，同样不在本条。

---

# 条目 2 · `Premium Campaign` SO（"15 个只抄了 UM"）

## ① 原版做法（逐句 + 出处）

### 2·1 到底是什么资产

- **资产路径**：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Premium Campaign *.json`。
- **现读文件数 = 14**（不是 15）：
  - **13 个阵营的 `PremiumItem`**：`AM · ASH · BL · DA · EC · GOF · GSC · SAU · SOR · SW · TAU · TL · UM`。
  - **1 个 `Premium Campaign Store`**：`eventId = "PremiumCampaignStore"`，**`m_Script` 与上 13 个不同**（前者 `4669455528150324631`，后者 `-7175108129811943783`）⇒ 它是 **LiveOps 商店定义**，**不是** `PremiumItem`。
- **类 = `PremiumItem : ObtainableItem, IPurchasableItem, IIdentifiable, IReleasable`**（`d:/2/tools/il2cpp_out/dump.cs:19208`，TypeDefIndex 355）。字段（dump.cs `:19208-19230` 与 `ObtainableItem` `:18763-18772`）：
  `itemDescriptionKey(+0x18)` · `itemLoreKey(+0x20)` · `cardRarity(+0x28)` · `cardArmy(+0x2C)` · `expansionPack(+0x30)` · `localizationKey(+0x38)` · `id(+0x40)` · `itemIcon(+0x48)` · `backgroundSprite(+0x50)`。

### 2·2 各阵营内容（13 件**除 `cardArmy` + `itemIcon` 外全同**）

现读 13 件 `PremiumItem` 的字段（逐件 JSON）：

| SO | id | cardArmy | Expansion / Rarity | itemDescriptionKey | localizationKey | itemIcon（GUID → 图） |
|---|---|---|---|---|---|---|
| Premium Campaign UM | `premium_campaign_um` | 10 | 0 / 0 | `Item_Description/PremiumCampaign` | `MenuShop/PremiumCampaign` | `5c839d39…` → `40k_DeckSelection_icon_FactionUM` |
| Premium Campaign GOF | `premium_campaign_gof` | 20 | 0 / 0 | 同上 | 同上 | `5037de52…` → `…FactionOrks` |
| Premium Campaign ASH | `premium_campaign_ash` | 30 | 0 / 0 | 同上 | 同上 | `aede04a1…` → `…FactionSaimHann` |
| Premium Campaign SAU | `premium_campaign_sau` | 40 | 0 / 0 | 同上 | 同上 | `1bd5410b…` → `…FactionSautekh` |
| Premium Campaign BL | `premium_campaign_bl` | 50 | 0 / 0 | 同上 | 同上 | `61273bcb…` → `…FactionBlackLegion` |
| Premium Campaign TL | `premium_campaign_tl` | 60 | 0 / 0 | 同上 | 同上 | `841e066a…` → `…FactionLeviathan` |
| Premium Campaign TAU | `premium_campaign_tau` | 70 | 0 / 0 | 同上 | 同上 | `e0b4afa4…` → `…FactionTauEmpire` |
| Premium Campaign SOR | `premium_campaign_sor` | 80 | 0 / 0 | 同上 | 同上 | `2184ec99…` → `…FactionSororitas` |
| Premium Campaign GSC | `premium_campaign_gsc` | 90 | 0 / 0 | 同上 | 同上 | `99f674da…` → `…Genestealers` |
| Premium Campaign AM | `premium_campaign_am` | 100 | 0 / 0 | 同上 | 同上 | `3407b24c…` → `…FactionAstraMilitarum` |
| Premium Campaign DA | `premium_campaign_da` | 110 | 0 / 0 | 同上 | 同上 | `04b9d446…` → `…FactionDarkAngels` |
| Premium Campaign EC | `premium_campaign_ec` | 120 | 0 / 0 | 同上 | 同上 | `9fcb1488…` → `…FactionEmperorsChildren` |
| Premium Campaign SW | `premium_campaign_sw` | 130 | 0 / 0 | 同上 | 同上 | `cd1608df…` → `…SpaceWolves` |

（`itemLoreKey` 全 `""`、`backgroundSprite` 全空引用 —— 原版就没配。）

> **图标全部本地已有**：13 张 `40k_DeckSelection_icon_*` 在 `Assets/CardPresentation/Resources/Art/ui_deck/`（现读 13/13 命中），且 `Core/ArmyIcons.IconName(faction)` 已把这 13 个阵营→图名的映射收口（`DeckRuntime.FactionIcon` 只是转调）。**不需要新导图。**

### 2·3 谁在用（`Premium Campaign Store` 的 `items[]`）

`Premium Campaign Store.json` 的 `items[]` **13 条**，形状 = `{ item{genericObjectReference(pathID), targetId}, price{Currency:30, Amount:1999}, availability:-1, sku:"premium_campaign" }`（判据：现读该 SO；旁证 `Shell/ShopData.cs:133-141` 已抄过这个形状）。

🔴 **原版自己有一处数据 bug（照抄，⛔ 别修好的地方要标注）**：`items[]` 最后一条的 `targetId` 写的是 **`premium_campaign_ec`**，但它 `genericObjectReference.m_PathID = -8070502029665518250` —— 该 pathID 对应的是 **`Premium Campaign SW`**（判据 = `判据/B9_资源没用上_分档.tsv:7070` 的 `PathID=-8070502029665518250` 明确标 `Premium Campaign SW`）。⇒ **第 13 条 targetId 字符串是复制粘贴错的，对象引用是对的（SW）**。这属「原版自己的 bug」，按 532 那族的同一条原则：**照抄 + 注释写死「原版这里 targetId 写错成 ec、对象是 SW」**。

### 2·4 与「战役节点表」的区分（别混）

- `Campaign Node Data*.json` **47 个**，`NodeId` 前缀**全是 `UM`**；`Ultramarines Campaign.json`（`CampaignEventData`，`eventId "UMCampaign"`、`filter.army 10`、`PointsPerLevel 100`、`Premium → premium_campaign_um`）。⇒ **本地只有 UM 一套节点表，是原版就这样**（不是我们没抄）。
- `PremiumItem`（13 件）是**商店可购买物**，与节点表是两件事。

## ② 我们现状（落点 + 差什么）

- **运行时 0 引用**：全仓 `Assets/**/*.cs` 里 `premium_campaign` **只出现在两处注释** —— `Shell/CampaignData.cs:10`（引 `Ultramarines Campaign.json` 的 `Premium.targetId "premium_campaign_um"`）与 `Shell/ShopData.cs:135`（引 store item 形状）。`Shell/PurchasePremiumWindow.cs` 的数据源 `premiumStoreReference` **是 null（远端 LiveOps）**，本地恒无数据 ⇒ 该窗「Content 下一个容器都不建」。
- **只涉及 UM 的判据**：`CampaignData.HasContent(army) => army == "Ultramarines"`（`CampaignData.cs:270-271`）—— 但那是**节点表**的 hasContent，**不是** premium item。
- **13 个 `PremiumItem` 一个都没落成数据表**（`premium_campaign_gof/ash/…` 在工程里 0 命中数据表）。
- **UI 侧已备但没数据喂**：`PurchasePremiumWindow`（节点表全建、`Army Container` 模板关着、无容器）、`ItemDrawer`（可画物品）、`ArmyIcons`（13 个阵营图标齐全）。

**差什么**：
1. 一张 **13 行的 `PremiumItem` 数据表**（id / army / 图 / 描述键 / 本地化键 / 价格），落点见 §2·③。
2. 那条 `Premium Campaign Store` 的 `items[]`（13 条，price `Currency 30 / Amount 1999`、`availability -1`）—— 用于「高级战役」购买窗。
3. `PurchasePremiumWindow` 把上面那张表 **喂进 `Content`**（今天 0 容器）；并处理原版自己那条 targetId bug。

## ③ 施工建议

| # | 改哪个文件 | 加什么 |
|---|---|---|
| 1 | `CardPresentation/Shell/CampaignData.cs`（或新 `Shell/PremiumCampaignData.cs`） | 新增 `PremiumCampaignItem` 表（13 行，字段 = `Id / Army / IconName / DescriptionKey / LocalizationKey / PriceCurrency / PriceAmount / Sku`）。**`IconName` 直接复用 `ArmyIcons.IconName(army)`**（别抄第二份图名映射）。 |
| 2 | `CardPresentation/Shell/CampaignData.cs:493` `ItemIcon` | 若要让 `ItemDrawer` 认得 `premium_campaign_*`，在此加：`if (targetId.StartsWith("premium_campaign_")) return ArmyIcons.IconName(armyOf(targetId));`（⛔ 别另建一张 id→图 的重复表）。 |
| 3 | `CardPresentation/Shell/PurchasePremiumWindow.cs` | 用表里的 13 行实例化 `Army Container`（模板 `:33759` 旁的那个 `armyInfoContainerReference` 已建）；价格读表；原版 targetId bug 在注释里写死。 |
| 4 | `CardPresentation/Shell/ShopData.cs:131-152` | 把 store 的 13 条 `items[]`（含价格真值）落成数据；标注那条 targetId bug。 |

**TSV 补齐清单** → `判据/PremiumCampaign_15SO_补齐清单.tsv`（13 阵营行 + 1 个 Store 行，见文件本身）。

## ④ 怎么验 + 还差什么

**验**：
1. `bash 工具/typecheck.sh` 过。
2. `ShellScene.Run` / `MainMenuScene.Run`（碰了菜单/外壳）—— 断言：13 行表逐行 `ArmyIcons.IconName` 非空、id 与 SO 的 `id` 逐字相等、`cardArmy` 数值对（10/20/…/130）。
3. 真机（批量验收）：进 Shell → 打开「购买高级战役」窗，选 13 个阵营逐个看容器都在、图标是**该阵营的徽记**。
4. `PurchasePremiumWindow` 原版**无本地数据时会在 `_armies[0]` 越界**（`Initialize()` 尾段）—— 别为了让窗开出来就造数据；我们保留「无数据则 0 容器 + 出声」的口径（除非本批已把表填上）。

**还差什么 / 边界（如实）**：
- **`Premium Campaign Store` 的 `dates`/`enabled`/`uploadToLive`** 等 LiveOps 元数据是**服务端字段**，本地这份 SO 只是一个快照样板；「过期的活动不做」（用户 2026-09-22 边界）⇒ **别把整份 store 当活动做**。
- 我们**没有** `PremiumItem` 对应的 `ItemDrawer` 类型判据（`PremiumItem.RewardType`/`ItemClass` 的具体值未读；`BaseOfferPopup` 里那批 `Icon Premium Campaign Drawer Variant` 是**抽屉 prefab 名**，不是物品类型）。
- 那 13 件除 `cardArmy`/`itemIcon` 外全同 ⇒ 表可以**只存 per-army 的 id + army + 价格**，其余（描述键/本地化键）走常量（但要在账里写明「13 件确实全同」这条判据）。
