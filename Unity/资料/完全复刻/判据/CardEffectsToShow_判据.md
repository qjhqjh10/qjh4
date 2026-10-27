# `GetCardEffectsToShow` 判据（卡面「受到以下影响」效果表 —— 只出判据，不改代码）

> 判据型子代理（只读）。目的：把 `EntityScript.GetCardEffectsToShow`（喂 `CardEffectsService`/`CardEffectItem`）的**项 / 判定 / 序 / 数据来源 / 与 5 槽的关系**钉死，供 `CardDisplayWindow.cs` 释放后一次做对。
> 主据 = `decomp_full/EntityScript__GetCardEffectsToShow.c`（迭代 `+0x108` 列表）。辅据 = `CardEffectsService__EnableEffects.c` / `CardEffectsService__DisableLeftoverSlots.c` / `CardEffectItem__LoadEffect.c` / `CardEffectItem__IsActive.c`。
> 我方现状 = `CardPresentation/Battle/CardDisplayWindow.cs:851 RowsOf(IReadOnlyList<UnitState.TempBuff> buffs)`（项源是 **TempBuff**，见 §四）。

---

## 一、原版 `abilities(+0x108)` 里到底哪几类进这张表（逐类）

`GetCardEffectsToShow` **按下标遍历 `+0x108` 列表**（`EntityScript__GetCardEffectsToShow.c:34`：`List.get_Item(list, i)` 循环），逐元素 `lVar4 = list[i]`，按字段 `+0x38 / +0x3c / +0x68 / +0x78` 分流：

| 类 | 判据（.c 行） | 进表方式 |
|---|---|---|
| **mark-of-chaos（混沌神）** | `+0x3c == 0x14a`（trait=mark，`:47`/`:185`）+ `+0x78`(trigger) ∈ **{10, 0x14, 0x1e, 0x28}**（`:53`/`:72`/`:92`/`:111`） | 每个神 **聚合一条**（`CreateMarkCardEffect(card, god)`，按神去重 `uVar6/7/8/9`）；计数累加到 `+0x40`（`:63-69` 等）⇒ **同神多条 → 一条、计数合并** |
| **trigger==0 或 0x32 的** | `+0x78 == 0` 或 `== 0x32`（`:185` 附近 `goto LAB_18092f3b1`） | **也走 mark 分支**（需 `+0x3c==0x14a`，否则落空） |
| **普通属性/其它 effect** | 非 mark 分支 → 若 `+0x38 == 2 && +0x68 == 0` **跳过**（`:135`）；若 `+0x38 == 3 && +0x3c ∈ {0x4fd,0x4fc,0x4fe}` **跳过**（`:146-161`）；**否则整条加入**（`:161` 的 `List.Add(uVar5)`） | **每条一行**（不聚合） |

> 关键词 / 属性增减的**文字**渲染不在本方法，在 `CardEffectItem__LoadEffect.c`（按 `+0x38 ∈ {8,0x11}` 属性、`+0x78` trigger、`+0x90/+0x91` 的 isPlayer 掩码走 `Battle/Effect/*` 键或 `Card_Trait/*`）。
> ⇒ **一句话**：进表的 = `+0x108` 里**除「type2 且 flag0」与「type3 且 trait∈{4fc,4fd,4fe}」之外**的所有 effect；其中 mark 按神**聚合**，其余**逐条**。

---

## 二、「这条 ability 当前该不该显示」的判定条件

- **本方法里没有时间窗 / isActive / 来源在场这类闸** —— 只有上面那张表的 **type / trait 排除** + **mark 聚合**。
- `CardEffectItem.IsActive()` = **`gameObject.activeSelf`**（`CardEffectItem__IsActive.c`：`get_gameObject().get_activeSelf()`）—— 那是**渲染出来的那格显不显示**，不是「该不该进列表」。
- ⇒ **「该不该出现在这张表」= 由 `+0x108` 列表的内容决定**（列表里有、且不是那两个排除档，就显示）。**列表本身**（哪些 effect 在当前卡上）= **引擎 `activeEffects`（含手牌/附魔族）实时内容**，由战斗逻辑增删 ⇒ **时间窗/来源在场是在「列表内容」这一层，不在 GetCardEffectsToShow 里**。

---

## 三、序：原版到底按什么排

- **按 `+0x108` 列表的【索引序】**（`get_Item(list, i)`，i=0..Count-1，`:34` 起）—— **插入/数据作者序**，**不是优先级、不是字典序**。
- mark 聚合那条按**首次出现的那个神**的顺序占位（后续同神累加、不再新增）。
- ⇒ 原版序 = **能力/效果的添加序**（`List.Add` 的次序）。

---

## 四、我们这边要拿到同样的项：需要 `RuleEngine` 提供什么（只给接口形状，不写实现）

**根因**：我方 `CardDisplayWindow.RowsOf` 的项源 = `UnitState.TempBuff`（**只含限时增益**）（`CardDisplayWindow.cs:851`）；而原版项源 = `+0x108`（**含永久 effect / 关键词 / 属性增减 / mark-of-chaos**）。⇒ **项源少了整类**（永久 effect / 关键词 / 属性）与 mark 聚合。

**好消息**：我方引擎**已经有 `+0x108` 的两半** —— `CardInstance.ActiveEffects`（附魔族）＋ `CardInstance.HandEffects`（手牌族），`RuleEngine/Core/Enchantment.cs:48-49` 明写「**两条 list 都对应原版 `+0x108`**，按消费者拆开」。所以**不需要新造数据**，缺的是**一个「按 `+0x108` 序合并、带标记字段」的读口**。

**接口形状（建议，⛔ 不写实现）**：
```csharp
// RuleEngine/Core/CardInstance.cs（或一个只读投影件）—— 新增只读方法：
public IReadOnlyList<CardEffectShown> GetEffectsToShow(bool isPlayer);
// 返回按「+0x108 合成序」的项；每项字段 = 原版 GetCardEffectsToShow/CardEffectItem.LoadEffect 消费的那几列：
public struct CardEffectShown {
    public int Type;        // 原 +0x38（8/0x11=属性；2/3=被排除/特殊；…）
    public int Trait;       // 原 +0x3c（0x14a=mark；0x4fc/4fd/4fe=排除）
    public int Trigger;     // 原 +0x78（10/0x14/0x1e/0x28=mark 神；0x32=…）
    public int Value;       // 原 +0x40（mark 聚合后的计数）
    public bool FromPlayer; // 原 +0x90/+0x91 的 isPlayer 掩码
    public string SourceName; // 「谁给的」（我方 RowsOf 已有的 SourceCard 口径）
}
```
- **合成序** = `ActiveEffects` + `HandEffects` 按各自 `List.Add` 序（谁先加谁先）—— 若两者无共同时序，退而按「先 ActiveEffects 后 HandEffects」（需在 RuleEngine 侧确认原版 `+0x108` 是否真的是**一条 list** 被我们拆成两条 ⇒ 若是拆的，合成时要还原原序；`Enchantment.cs:48-49` 说「按消费者拆开」，**原版是一条** ⇒ 合成顺序应还原原 `List` 序，⚠️ 这点要在 RuleEngine 侧坐实）。
- **过滤（type2&flag0 / type3&trait∈{4fc,4fd,4fe}）与 mark 聚合**：建议**放在这个读口里做**（一次做对，表现层只渲染），或表现层做但必须拿全字段。推荐**读口做过滤+聚合、返回已规整的项**（与 `CardEffectItem.LoadEffect` 的输入对齐）。

**现有 API 够不够**：`ActiveEffects`/`HandEffects` 有（但没合并、没按 `+0x108` 序、没聚合 mark）；`CardDef.Effects`（EffectSpec dict）是解析表、非「当前实例效果列表」；`UnitState.TempBuff` 是限时 buff、**不是** `+0x108`。⇒ **需要一个新读口**（如上面的 `GetEffectsToShow`）。

---

## 五、与 `SetEffectRows` 的「5 槽 / 空行占位 / 槽满截断」怎么配合

**全在渲染层（`CardEffectsService`），项源不做截断**：
- 项源 `GetCardEffectsToShow` **返回全量**（含 mark 聚合；不按 5 截断）。
- `CardEffectsService.EnableEffects`（`CardEffectsService__EnableEffects.c`）：按下标逐项 `CardEffectItem.Enable(0) → LoadEffect(item)`：
  - `IsTextEmpty(item)` 为**假** ⇒ 计数 `iVar6++`（非空条数）；
  - 为**真** ⇒ 调「隐藏那格」（`BattleAlliancePanel.CloseButtonClick` = 关这件）；
  - **无论空不空都 `slotIndex(+0x18)++`**（`:35` 之后）⇒ **空文本项也占一槽 = 空行占位**；
  - 循环条件 `slotIndex < effects.Count` 且 `slotIndex < slots.Length`（`:23`/`:27`）⇒ **槽满（= slots.Length，恒 5）即 break 截断**；
  - 收尾 `DisableLeftoverSlots` 把**没用到的槽关掉**；返回 `(非空条数 > 0)`。
- ⇒ **我方差在**：`CardDisplayWindow.cs:913` 的 doc 自己写「我们按 buff 铸行、**空的根本不铸 ⇒ 中间不留空洞**」—— 与原版「**空项占槽、留空洞、槽满截断**」**不同**。要还原：**项源给全量 → 渲染层逐项 LoadEffect → 空文本也占槽 → 5 槽满截断**。

---

## 六、结论（施工口径，供文件释放后一次做）
1. **项源换成 `+0x108` 合成列表**（`ActiveEffects`+`HandEffects`，按原序）—— 不是 TempBuff。
2. **过滤**：排除 `type2&flag0`、`type3&trait∈{4fc,4fd,4fe}`；**mark 按神聚合计数**。
3. **序 = 添加序**（`+0x108` 索引序）。
4. **渲染层保留 5 槽 / 空项占槽（留空洞）/ 槽满截断**（当前我们「空的不铸」是差异）。
5. **接口**：`RuleEngine.CardInstance.GetEffectsToShow(isPlayer) → IReadOnlyList<CardEffectShown>`（字段见 §四）；合成序要在 RuleEngine 侧确认 `ActiveEffects`+`HandEffects` 是否真是原 `+0x108` 拆出来的（`Enchantment.cs:48-49` 说是）。

> ⚠️ 判据出处补充：`+0x38/+0x3c/+0x68/+0x78/+0x40/+0x90/+0x91` 的字段含义需在 `dump.cs` 的 `CardEffect`/`CardScript` 类定义里再确认一次（本文按 .c 用法反推：0x14a=mark trait、10/14/1e/28=四神 trigger、4fc/4fd/4fe=三个排除 trait）—— 落地前拿 `dump.cs` 字段表交叉一次更稳。

---

## 七、🔴 交叉 `dump.cs` 后的**字段表订正** + **模型不匹配（A 为什么现在不能硬做）**

### 7.1 字段表订正（`dump.cs` `CardEffect // TypeDefIndex 3199` 亲读，推翻 §四/§六 的按 .c 反推）
| 偏移 | 真字段（dump.cs） | 我在 §一/§四 原来的叫法 | 订正 |
|---|---|---|---|
| `+0x38` | **`buffType`（BuffType）** | Type | ✔ 是 buffType |
| `+0x3c` | **`trait`（DefinedTrait）** | Trait | ✔ |
| `+0x40` | **`traitValue`（int）** | Value | ✔（mark 聚合计数就是它） |
| `+0x68` | **`costChange`（int）** | （排除档里比 0） | ✔ 是 costChange |
| `+0x78` | **`markOfChaosType`（MarkOfChaosType）** | ❌ 我写成「trigger」 | **是四神类型**（10/0x14/0x1e/0x28 = 四个混沌神） |
| `+0x90`/`+0x91` | **`secretBuff` / `questBuff`（bool）** | ❌ 我写成「isPlayer 掩码」 | **不是 isPlayer**；真 isPlayer = **`originalIsPlayer(+0xA0)`** |

⇒ **mark 分支的真语义（订正）**：`trait == DefinedTrait.markOfChaos(0x14a)` **且** `markOfChaosType ∈ {10,20,30,40}(四神)` ⇒ **按神聚合一条 + `traitValue` 累加**。排除档：`buffType==2 && costChange==0`、`buffType==3 && trait∈{0x4fc,0x4fd,0x4fe}`。**其余 effect 逐条一行，序 = `+0x108` 索引序。**

### 7.2 🔴 模型不匹配：**我方根本没有「带这些字段的统一 `+0x108` 列表」**
读我方模型（`RuleEngine/Core/CardInstance.cs` + `Enchantment.cs`）：
- `CardInstance.HandEffects` = **`HandEffect`（`EffectOp` 载荷模型**：`Op/RecordId/Source/Expire/Extrinsic/ActingUnit/OnlyInstanceId/…`）—— **没有 `buffType/trait/markOfChaosType/traitValue` 这套字段**。
- `CardInstance.ActiveEffects` = **`ActiveCardEffect`（只承载附魔 12/15/16）**：有 `BuffType/Trait/…`，但**只附魔族**，且 `Trait` 是 string、没有 `markOfChaosType`。
- 卡面「受到以下影响」现在读 **`UnitState.TempBuff`（限时 buff）**（`CardDisplayWindow.RowsOf`）。
- 永久 effect / 光环贡献在 **`Aura`/`Persistent`（计算式，非一个可枚举的 CardEffect 列表）**。
- 原版 `+0x108` = **一条** list（`Enchantment.cs:48-49` 自己写明「我们**按消费者拆开**成两条」）。

⇒ **要做 `GetEffectsToShow` 返回 §四 那个 `CardEffectShown{ buffType/trait/markOfChaosType/traitValue/originalIsPlayer/source }`，需要一个「当前这张卡身上所有生效效果、统一成原版 CardEffect 形状」的源 —— 而我们的引擎把这件事拆到了 4~5 套异构模型（HandEffect(EffectOp) / ActiveCardEffect(附魔) / TempBuff(限时) / Aura / Persistent），且各有各的字段口径，没有一处能直接给出原版那套 `buffType/trait/markOfChaosType`。**

**这就是 A 不能硬做的原因（= 你交代的「确认不了就写清差什么、别硬拼」）**：
1. **没有统一源**：要在 `CardInstance.GetEffectsToShow` 里 union `HandEffect + ActiveCardEffect + TempBuff + Aura + Persistent`，而 **`Aura`/`EffectResolver`/`CardDef`/`Enchantment` 全被别的代理占着（E6ENG/E7ENG）**，我拿不到它们的贡献枚举、也不知道如何把 `EffectOp` 反推成 `buffType/trait`。
2. **字段不可逆推**：`HandEffect.Op`（EffectOp）→ 原版 `buffType/trait/markOfChaosType/traitValue` 是一张**语义映射表**，不在我手上（`GivePayload`/`EffectOp` 的判据在 EffectResolver，被占）。
3. **blast radius**：`GetEffectsToShow` 一旦接进卡面显示，影响**所有对局所有卡**的「受到以下影响」行（BattleScene 断言面极大）；半对的项源会**静默显示错**。

### 7.3 建议的**最小正确第一步**（等 E6ENG/E7ENG 释放、或由你并入它们）
1. **先在 RuleEngine 侧统一一个只读投影**：`CardInstance.GetEffectsToShow(bool isPlayer)` 内部 union `ActiveEffects + HandEffects + (Aura/Persistent 的贡献)`，并把每条**归一成原版 CardEffect 形状**（需要 EffectOp→buffType/trait 的映射，由 EffectResolver 提供）。
2. 归一层落定后，`CardDisplayWindow.SetEffectRows` 再接它 + 上 5 槽/空项占槽/截断。
3. ⚠️ 在此之前，`CardDisplayWindow` 的「空项占槽/留空洞」差异**不影响观感**（因为现源 TempBuff 每条都有文本、根本不出空项）—— 所以 B 的「留空洞」这半**现在改了也没有可见效果**，真正的可见差异要等项源换掉（§7.2）。

> 结论：**A/B 都阻塞在「项源模型不统一」**（触 Aura/EffectResolver/CardDef/Enchantment，均被占）。**本轮不硬做**；判据已铺平（字段表订正 + 统一口径 + 施工顺序）。等你解封那几处或把它并入 E6ENG/E7ENG，即可按 §7.3 一次做完。
