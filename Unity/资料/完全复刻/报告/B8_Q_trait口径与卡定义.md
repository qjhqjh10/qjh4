# B8_Q · 卡定义 / 光环层：`armour` 算不算 trait ＋ `IsKeyword` 映射 ＋ `KwValue` 夹紧 ＋ `StatMod` 两列

> 执行代理 **B8_Q**（批次 8）· 2026-10-26
> 白名单：`RuleEngine/Core/{CardDef,UnitState,Aura}.cs` ＋ 账补丁 `账/_parts/B8_Q.tsv` ＋ 本报告。
> 账补丁 = **8 行**（1 条 A 就地补证 · 5 条 C〔原 4 条 B ＋ 1 条原 A 降档〕· 1 条 C〔`IsKeyword`，原 B〕· 1 条 B〔`GetCurrentTraitValueWithModifiers`，原 B，补差异〕）。
> 验证：`typecheck` **0 / 0** · `ruleprobe check` **rc=0（解析差异 0 行 · 哨兵合计 0 条）** ·
> 补丁合并（临时副本）**8 更新 / 0 新增 / 0 报错** · `check_a_vs_diff` **0 命中** · `verify_annot` **缺注解 0** ·
> `check_patch_conflicts` **无跨补丁重复键**。
> ⚠️ **`play check` 现在 rc=1，但不是本代理造成的**：指纹 `350c16b2…`（基线 `a93ffb8f…`），变的是
> `GOF97` / `TL27`（主档，`selfPlace 0→1`、`other 1→2`）与 `ASH16` / `GOF67` / `GOF97`（电池，`net 2→4` / `1→3`）——
> 全是**部署 / 光环重算 / 授予**那几条路。本代理开工时（07:13）`play check` 是 **rc=0**；此后
> `EffectResolver.cs`(07:18) · `PlayerState.cs`(07:20) · `BattleEvent.cs`(07:18) · `AiScoring.cs`(07:18) ·
> `SimpleAI.cs`(06:57) · `ConditionEval.cs`(06:56) 都被**别的代理**改过（`find -newermt` 现读）。
> 本代理三处改动**逐条可证惰性**：`CardDef` 那段是**无调用方**的新 API；`KwValue` 夹紧在「无负 traitValue」下恒等
> （池内 0 条负 `addTrait.traitValue`）；`StatMod.Trait` 恒 `null` ⇒ ⑥ 支恒 false（与原实现同）。
> ⏭ 复跑口径（铁律 12）：**等并发代理收工后**由调度台统一 `play check`，⛔ 别拿这次的 rc=1 归到本代理。

---

## ① 结论

| 任务 | 定案 |
|---|---|
| **1 · `armour` 算不算 trait** | **不算。它是【独立字段】**：原版 `RawCardScript.armour` = `+0xA0`（`dump.cs:23264`），运行时读进 `EntityScript.currentArmourValue(+0xB8)`，与 `currentMaxHealth`/`currentBaseMeleeAttack`/`currentBaseRangedAttack` **四兄弟并列**（`CardScript.CardSetup`，🥈 `CardScript.cs:6465-6481`）。trait 家族（`CountTraits`/`HasDefaultTrait`/`GetDefaultTraitValue`/`FindTraitInList`/`CheckIfTraitIsInList`）**只扫 `CardTraits(+0x230)`**。数据：`_trait==1020` 全快照 **5 张**（`BL27`/`GOF61`/`GSC28`/`GSC32`/`TL70`），而 `armour` 字段 `>0` **81 张**。⇒ 我们原来「`Has`/`KwValue`/`Keywords.Count` ≡ trait 家族」的映射**证伪**；已按原版建 `CardDef.CardTraits` ＋ 四个查询（`CardDef.cs:6373-6485`），**缺口 = 数据没透传**（`CardDatabase` 未传 `cardTraits:`）⇒ 5 条账行 **C（P0）**。 |
| **2 · `IsKeyword` 4 张对不上** | **我们的映射错了**（把「卡面那行橙字」`Subtype` 当成原版 `keyword(+0x98)` 字段）。4 张逐张核过：`BL1` SO=`Daemon(40)` 而卡面印 `Infantry`（**原版自己印刷与数据矛盾，按数据记**）；`EC55/56/57` SO=`Infantry(5)` 而卡面印 `Warlord`（**`Warlord` 根本不在 `Keyword` 枚举里**，那是我们给全部 56 张 hero 写的显示值）。⇒ 已建 `CardDef.KeywordId/IsKeyword(int)`；账行 **C（P0）**，缺口三处见 §⑥。 |
| **3 · `KwValue` 缺 `≥0` 夹紧** | **已补**（判据 = `EntityScript.GetCurrentTraitValueWithModifiers` 收尾 `num5 < 0 ⇒ 0`，🥈 `EntityScript.cs:3406` 段 · 🥇 `.c:68-71`）。配套把本类五处「读旧值 ± 增量」记账改走不夹的 `RawKw`（否则夹紧会让下一次授予多算）。**池内 0 张可达**（全快照没有任何负 `addTrait.traitValue`）⇒ 账行**保持 B（P2）**，标「补的是语义、无行为差异」。 |
| **4 · `StatMod` 补 `trait`/`buffType`** | **已补两列**（`UnitState.cs:1849`）＋ `AddStatMod(..., int buffType = 0, string trait = null)`；`HasWhileInPlayAbility` **第 ⑥ 支从「判不了 / 🔶 自己设计」变「逐字已核」**（`UnitState.cs:2406-2412`）。数据：全快照 507 条 `buffType == 1` 记录里 `trait` 只有 505×`null` / 1×930 / 1×1020，**`trait == 610(pack)` 一条都没有**（`pack` 只有 2 条、都是 `buffType 3 addTrait`）⇒ 该支**恒 false 是数据事实**，与原版逐字同结果。账行 **A**（就地补证）。 |

---

## ② 逐条证据

### 2.1 任务 1 —— `armour` 是字段，不是 trait

| 分量 | 判据 | 数值 / 出处 |
|---|---|---|
| `RawCardScript.armour` 是独立 int | `il2cpp_out/dump.cs:23264`（`public int armour; // 0xA0`） | — |
| `CardTraits` 是另一张表 | `dump.cs:23325`（`public List<CardTraitData> CardTraits; // 0x230`） | `CardTraitData`：`_trait`(`+0x18`) / `_traitValue`(`+0x1c`) |
| 运行时护甲从字段来 | 🥈 `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:6465-6481`：`currentMaxHealth = raw.maxHealth;` **`currentArmourValue = raw.armour;`** `currentBaseMeleeAttack = raw.meleeAttack;` `currentBaseRangedAttack = raw.rangeAttack;` | 四个字段逐个读，**并列** |
| 改护甲只动那一格 | `decomp_full/CardScript__AddArmour.c:21-59`（写 `+0xb8`，`<0` ⇒ 0） | — |
| trait 家族只扫 `+0x230` | `RawCardScript__CountTraits.c:9-12` · `__HasDefaultTrait.c:10-34` · `__GetDefaultTraitValue.c:21-34` · `__FindTraitInList.c:19-32` · `__CheckIfTraitIsInList.c:19-32` | 五份方法体逐句读过（🥇） |
| 「有护甲」这个条件读的是字段 | `FilterMethods__MeetsConditionCriteria.c:298-300`：`case 0xf4 /*244 hasArmour*/:` 读 **`param_1 + 0xb8`** | 我们的落点 `ConditionEval.cs:393`（`u.Armor > 0`）**已经对** |
| 原版**从不**用 trait 家族查 armour | `grep 'HasCurrentTrait(...,0x3fc'` / `HasDefaultTrait(...,0x3fc` / `GetDefaultTraitValue(...,0x3fc` = **0 命中**；🥈 全树 `DefinedTrait.armour` 出现 **0** 次 | `1020 = 0x3fc` |
| 数据①：trait 里的 armour | `cards_truth.json` 1,351 张 | `_trait == 1020` = **5 张**：`BL27` · `GOF61` · `GSC28` · `GSC32` · `TL70`（`_traitValue` 全 = 1） |
| 数据②：字段里的 armour | `cards_engine.json` 1,128 张 | `armour > 0` = **81 张**；`Armour N` 关键词 = **81 张**（`Armour 1` 62 · `Armour 2` 17 · `Armour 2.` 1 · `Armour 3` 1），**81/81 与字段值逐张相等** |
| 数据③：trait 表总体 | 同上 | 1,138 条 / 750 张带 trait / 601 张 0 条 / **重复 trait 0 张** |

**可观察量（G3）** —— `bash 工具/ruleprobe.sh layers` → `layers.jsonl`：

```text
ASH34: {"kw":["Armour 2","Talent"], "kwKeys":{"armour":2,"talent":1}}     ← 我们侧（关键词 2 条）
原版侧：CardTraits=[talent] ⇒ CountTraits()=1 · HasDefaultTrait("armour")=false · GetDefaultTraitValue("armour")=0
                             而 armour 字段 = 2
BL27 : {"kwKeys":{"armour":1}}  ← 原版 CardTraits=[Armour 1] ⇒ 值同（**但来源不同**：我们读到的是字段那一份）
```

### 2.1·b 🔴 为什么**没有**把 `Armour N` 从 `_keywords` 里删掉（对简报字面的偏离，请复核）

简报写「把 `Armour N` 从 `keywords` 里分出来」。**逐处核过调用者之后，我判定「删」是更不忠实的做法**，理由四条：

1. **原版的护甲本来就不在 trait 列表里，而是一个字段** —— `currentArmourValue(+0xB8)` 从 `raw.armour(+0xA0)` 读。
   我们这一格 `_keywords[armour]` **正是那个字段的等价物**（`UnitState.Armor = KwValue(armour)`，`UnitState.cs:900`）。
   删掉它 = 把「字段」也一起删了 ⇒ 护甲整条机制失效（`RuleCore.cs:7272` / `7398` / `CounterAttack.cs:146` 全读 `u.Armor`）。
2. **卡面确实印着 `Armour N`** —— 它是 PnP 卡面关键词行的一员（`ASH34` 的 `desc` 第一行就是 `Armour 2.`），
   删了会让 `BattleDriver.cs:27142` / `CardView` 的关键词行少一条（那两份文件**不在本代理白名单**，
   改动无法在本轮验收）。
3. **`Has("armour")` 有一个真·消费者**：`BattleDriver.cs:27142`（卡面关键词行）与 `RuleEngineTest.cs:2680`
   的既有断言（「列给的护甲也进 `Has`」）。删这个词 = 同时改两处别人的文件 + 让一条断言变红。
4. **真正错的不是存储，是「等价口径」** —— 把同一张字典当成原版 `defaultTraits` 去答 `CountTraits` 那一族。
   这一条**已经改掉**：trait 家族现在有独立的 `CardDef.CardTraits` ＋ 四个查询（`:6373-6485`）。

**调用者清点（这就是「逐处核」的产出）**：

- `CardDef.Has(...)` 的调用点：`grep` 命中 `.Card.Has(` / `c.Has(` / `card.Has(` 共 **62 处**（不含 `UnitState.Has`，那是运行时那一份）。
- `CardDef.Keywords` 的直接使用者只有 **3 处非测试**：`RuleCore.cs:13647`（`UnimplementedKeywords` 诊断）· `UnitState.cs:896`（构造时拷进运行时字典）· `SimpleAI.cs:1020-1022`（AI 打分权重）。**没有一处是 trait 列表查询**。
- ⇒ **结论：`Has` / `Keywords` 的现有消费者全是「关键词语义」，没有一个在答 trait 家族的问题。** 所以今天**没有活行为**依赖那条错映射（这也是 §① 里「5 条账行只能 C 却零行为差异」的原因）。

### 2.2 任务 2 —— `IsKeyword` 逐张

| 卡 | SO `keyword`(+0x98) | 我们的 `Subtype`（卡面橙字） | 卡面判据（亲读 PNG） | 结论 |
|---|---|---|---|---|
| `BL1 Chaos Spawn` | **40 Daemon** | Infantry | 印 `Infantry`（`d:/2/Warpforge部队卡片/Chaos/3部队/Warpforge_9_Chaos-Spawn.png`） | **原版印刷 ↔ 数据自相矛盾**；数字版行为按字段 ⇒ `IsKeyword(Daemon)`=真 / `IsKeyword(Infantry)`=假 |
| `EC55 Lucius The Eternal` | **5 Infantry** | Warlord | 印 `Warlord`（`Emperor_s Children/1督军/Warpforge_03_Lucius-The-Eternal.png`） | 同：`Warlord` 不在 `Keyword` 枚举里，只能当显示 |
| `EC56 Lord Kaphrael` | **5 Infantry** | Warlord | （同族模板） | 同上 |
| `EC57 Xarahan Daemon Prince` | **5 Infantry** | Warlord | （同族模板） | 同上 |

- `RawCardScript__IsKeyword.c` **全文 7 行**：`return *(int *)(param_1 + 0x98) == param_2;`。
- `Keyword` 枚举 9 档（`dump.cs:45899-45912`）：`none 0 / Infantry 5 / Vehicle 10 / Monster 15 / Building 20 / Battlesuit 25 / Drone 30 / Beast 35 / Daemon 40`。
- 全池口径：`type ∈ {unit,hero}` 共 **642** 张；其中 `keyword != 0` 的 **589** 张里 **585 一致 / 4 不一致**（其余 56 张 hero 的 `keyword = 0`，与我们 `Subtype="Warlord"` 不同但**行为等价**：`IsKeyword(任何一档)` 都是假）。
- **活消费点**：`ConditionEval.cs:426-440` 的条件 **275 / 276 / 277**（`isWarlordOrVehicle` / `isBattlesuitOrVehicle` / `isInfrantryOrBeast`）→ 池内只有 **4 张**卡用到：`Path of the Warrior`(275) · `Skyborne Deployment`(275) · `Speshul Dose`(277) · `Deploy Anchors`(276)。
- **可观察量（G3）**：`Speshul Dose`（`isInfrantryOrBeast`）的候选集 —— 原版**不含** `BL1 Chaos Spawn`（SO = Daemon）；我们**会含**（`Subtype="Infantry"` ⇒ `SubtypeIs("Infantry")` 真）。⚠️ `ruleprobe` 没有「候选集」读数模式 ⇒ 今天只能给数据两侧值。

### 2.3 任务 3 —— `≥0` 夹紧

🥈 `out_cpp2il_dev_ilspy/EntityScript.cs:3406` 起的方法体收尾：

```csharp
int num5 = num4 + num;      // num4 = CurrentTraits.Find(t)._traitValue ; num = Σ(buffType==addTrait && trait)
bool flag4 = num5 < 0;
int result = 0;
if (!flag4) { result = num5; }
return result;
```

🥇 同义：`EntityScript__GetCurrentTraitValueWithModifiers.c:68-71`（`if (-1 < iVar8 + iVar7) iVar1 = iVar8 + iVar7;`，否则 `iVar1 = 0`）。
对照：**不带修饰**那一支 `EntityScript__GetCurrentTraitValue.c:22-30` **不夹**（输入是序列化值、恒 ≥0）⇒ 我们夹在 `KwValue`（合并视图 = 带修饰那一路）上，与 `Current*` 属性族同口径。

### 2.4 任务 4 —— `HasWhileInPlayAbility` 第 ⑥ 支

🥈 `CardScript.cs:6800-6863` 的 lambda：`if (x.trait != DefinedTrait.pack) return false; object obj = x.buffType - 1; return obj == null;`
⇒ 条件 = **`x.trait == pack && x.buffType == 1(addStats)`**。
我们的落点：`UnitState.cs:2406-2412`（`for` 扫 `_statMods`，`m.BuffType == 1 && m.Trait == "pack" ⇒ true`）。
数据（`cards_truth.json` 1,351 张现扫，递归扫字典里**同时**有 `buffType`/`trait` 的 415 个对象）：
`buffType == 1` 的 **507** 条，`trait` = 505×`null` · 1×`930(concussive)` · 1×`1020(armour)`；**`trait == 610(pack)` 0 条**（`pack` 只出现 2 次，都是 `buffType 3 addTrait`：`SW25` / `SW39`）。
⇒ 第 ⑥ 支**逐字实现后仍恒 false，且这是原版的数据事实**。

---

## ③ 改了哪些文件哪些行

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `RuleEngine/Core/CardDef.cs` | ① **类尾新增**（`:6318` 之后）：`CardTraitEntry` / `_cardTraits` / `CardTraitsKnown` / `CardTraits` / `AttachCardTraits` / `CountTraits(out)` / `HasDefaultTrait(out)` / `GetDefaultTraitValue(out)` / `FindDefaultTrait(out)` / `CardTraitsWhy` / `TraitKeyOf` / `_keywordId` / `KeywordId` / `AttachKeywordId` / `IsKeyword(int, out)`，并把「`armour` 是字段不是 trait」「`keyword` 与 `Subtype` 是两件事」的判据全文写成块注释（`:6320-6371`）。② `KeywordTable.FirstNumber` **`static` → `internal static`**（同一行，供上面的解析复用，不另写第二份数字解析）。 | **8404 → 8615（+211）**；插入点 `:6318` 以上**零位移** |
| `RuleEngine/Core/UnitState.cs` | ① `KwValue` 加 `≥0` 夹紧（**改写同一行**，`:1076`）。② 五处「读旧值 ± 增量」改走新私有 `RawKw`（`:1109` / `:1219` / `:1502` / `:1529` / `:2300`，均**行内替换**）。③ `StatMod` 补 `Trait` / `BuffType` 两列（**把 `Source` 那一行就地扩成三个成员**，`:1849`；doc 同一行改写）。④ `AddStatMod` 加两个可选形参（**同一行**）＋ 建对象那一行就地扩（**同一行**）。⑤ `HasWhileInPlayAbility` 第 ⑥ 支**逐字实现**（`:2406-2412`，7 行 → 7 行）；方法 doc 的就地订正（12 行 → 12 行）。⑥ **类尾新增** `RawKw` ＋ 「夹紧判据」块注释（`:2431-2467`）。 | **2444 → 2474（+30）**，**全部落在最后一个账落点（`:2429`）之后** ⇒ 零位移 |
| `RuleEngine/Core/Aura.cs` | **未改**（见 §④ 第 3 条：本轮四条任务没有一处需要动它） | 0 |
| `账/_parts/B8_Q.tsv` | 8 行（11 列 / LF / 无裸 CR）；合并校验：临时副本上 `merge_ledger_parts --only B8_Q.tsv` = **8 更新 / 0 新增 / 0 报错** | 新建 |

> 账落点位移自查：`CardDef.cs` 的插入点在 `:6318`，其下**没有任何 A 档落点**（`check_ledger_points` 跑完**没有 CardDef.cs / UnitState.cs 条目**）；
> `UnitState.cs` 的插入点在全文件最后一个 A 档落点之后。`check_ledger_points` 的 8 条失败全部在 `CardView.cs` / `EffectResolver.cs`（**别人的活**，本轮之前就在）。

---

## ④ 没查清的部分（如实）

1. **`CardTraits` 的条目顺序/重复**：原版是 `List<CardTraitData>`，`Find` 取**第一条**命中；我们照 `List` 存（不去重、保序）。现读 1,351 张 **0 张有重复** ⇒ 顺序问题今天不可观察，但**没有**卡能证明「重复时取第一条」这条语义（只能靠方法体）。
2. **`GetDefaultTraitValue` 的「未知 trait」与「值为 0」**：原版两者都返回 0，无法区分 —— 我们照做（要区分得先问 `HasDefaultTrait`）。**没有**验证过「原版有没有调用方依赖这个不可区分性」。
3. **`Aura.cs` 为什么在白名单里**：本轮四条任务没有一处需要动它。逐条核过：光环授甲走 `AddAuraKeyword` → `SyncKeywordState`（`UnitState.cs:1212` 那一段），与「armour 算不算 trait」无关；`Aura.cs` 里提到 `Armour` 的三处是注释与 `If the target has Armour` 的正文（那种条件走 `hasArmour(244)` = **字段**，`ConditionEval.cs:393` 已对）。⇒ 如实记「未改」。
4. **`CardEffect.trait` 的其它消费者**：`GetCurrentTraitValueWithModifiers` 也按 `cardEffect.trait` 过滤（`buffType == addTrait && trait == 参数`）。我们对应的合并视图 `_keywords` 在**授予**这一侧是对的（`AddKeyword` 收 `keyword`），但**没有**逐条记「这一条 `+2 attack` 的 `CardEffect.trait` 是什么」——那正是本轮补 `StatMod.Trait` 的动机；`EffectResolver` 侧要不要把 `trait` 传进来**未查清**（不在白名单）。
5. **`IsKeyword` 的缺口三处都改不了**（白名单）：见 §⑥。

---

## ⑤ 顺手发现（请调度台分流）

1. 🔴 **`Has()` ≡ `HasDefaultTrait()` 的偏差**不止 armour：现扫 `cards_engine.json` 1,128 张，
   「`keywords` 有、`cardTraits` 没有」的 (卡,词) 对 = **85 对 / 83 张**：
   `armour` 75 · `armour.` 1 · `codex` 3 · `strike` 1 · `stomp` 1 · 另有 `remnant.` / `destroyer.` / `vanguard.` / `stealth.` 4 个带句号的写法。
   ⇒ 除 armour 外，`codex`(3) / `strike`(1) / `stomp`(1) 这 5 张在 trait 家族上也会假阳。**这些词是否该进 `cardTraits`** → 需要一次数据侧裁定。
2. ⚠️ **`Armour N` 关键词的解析有一个句号写法**：`GSC26 Aberrant Hypermorph` 的 `keywords` 里是 `"Armour 2."`（句号进了词）。
   `KeywordTable` 靠「前缀匹配 + 首个数字」仍取值 2，**今天不出错**；但 `layers.jsonl` 的 `kw` 原样带着句号 ⇒ 任何**按名字精确比**的消费者会漏它。
3. 🔴 **两条账的落点行号是陈旧的（不是我改出来的）**：
   - `战斗界面_HUD.tsv:168`（`UpdateBastionr`，D 档）备注写 `规则见 CardDef.cs:6399-6410` —— 现读那一段是 `Stealth` 常量；`public const string Bastion` 现在在 **`CardDef.cs:6845`**（我的 +211 之前是 6634，与 6399 已经差 235 行）。
   - `战斗逻辑驱动.tsv:1121`（`hasMaintenanceEffects`，B 档）落点写 `CardDef.cs:1294（Maintenance 常量）· CardDef.cs:7864-7867（裸数值 trait 说明）` —— 前者现读不是 `Maintenance` 常量，后者现读是 `drop pod` 的注释。
4. 📌 **`CountTraits()` 在已恢复的原版方法体里 0 个调用方**（`decomp_full` 与 🥈 两份都 grep 过，只有定义）⇒ 这一条即使做到 100% 也**没有可观察行为**；它的价值是「模型完整性」。
5. 📌 **`GetExtraAbilityTraits`（账 `:1380`，B）** 的落点写「`CardDef` 的 `cardTraits` 列」—— 本轮之前 `CardDef` **没有**这一列（只有 DTO 有）。现在有了 `CardDef.CardTraits`，那一行的落点描述**变成真的**了（但仍未逐句核 `GetTraitFromAbilityTrigger` 那条派生链，故不动它）。

---

## ⑥ 给调度台的请求（都不在本代理白名单）

| # | 文件 | 需要什么 | 为什么 |
|---|---|---|---|
| **R1** | `RuleEngine/Data/CardDatabase.cs` | `new CardDef(...)` 加一行 `cardTraits: c.cardTraits` + 构造后调 `AttachCardTraits(...)`（或直接在 ctor 里接 —— 但 ctor 加行会让 `CardDef.cs` 40 个账落点整体漂移，见下） | **trait 家族 5 条 C（P0）收口的唯一前提**。`cardTraits` 是 **v9 起就有的既有列**（`CardDatabase.cs:172`，注释明写「本列不喂引擎」）⇒ 不需要动生成器 |
| **R2** | `工具/gen_cards_engine.py` | 新落一列 `keyword`（原版 `RawCardScript.keyword +0x98`；真值已在 `cards_truth.json.keyword`，1,351 张全有） | 任务 2 的数据前提（`cards_engine.json` 现读**没有**这一列） |
| **R3** | `RuleEngine/Data/CardDatabase.cs` | `CardDto` 加 `public int keyword;`，`new CardDef(...)` 传 `keyword:` ⇒ `AttachKeywordId(...)` | 同 R2 |
| **R4** | `RuleEngine/Core/ConditionEval.cs:426-440` | 条件 275/276/277 的 `SubtypeIs(...)` 换成 `card.IsKeyword(5/10/25/35, out why)`，`null`（判不了）时退回 `SubtypeIs` 并**出声** | 任务 2 的行为面（池内 4 张卡受影响，`Speshul Dose` 那张今天会给 `BL1 Chaos Spawn` 一个非法目标） |
| **R5** | `RuleEngine/Core/EffectResolver.cs`（`ApplyOneGain` / `ApplyOneBuff` / `setHealth` 那几个建 `StatMod` 的地方） | 建修饰时把 `CardEffect.trait` 与 `buffType` 传进 `AddStatMod(..., buffType: N, trait: "...")` | 第 ⑥ 支的写入侧（今天 `Trait` 恒 null；数据上 0 条可达 ⇒ 不阻塞，但「已核」要写全） |
| **R6** | `工具/ruleprobe/`（新增一个只读模式） | 例如 `ruleprobe traits`：逐卡 dump `CountTraits` / `CardTraits` / `KeywordId` | trait 家族与 `IsKeyword` 的 G3 可观察量**今天读不到**（`layers` 只给 `kwKeys`）。有了它，5 条 C 的验收就不用再靠「数据两侧值」 |
| **R7** | 调度台裁定 | 上面 §⑤-1 那 5 个词（`codex` 3 / `strike` 1 / `stomp` 1）**该不该进 `cardTraits`** | 影响 trait 家族在 5 张卡上的真假；数据侧口径，不是代码能自己定的 |

> ⚠️ **不要为了 R1 去改 `CardDef` 的构造函数签名**：构造函数在 `:839-1000`，下面有 40 个账落点 ⇒ 加一行会让它们**整体漂移**（`--fix` 能修，但要占一次「没有 `.cs` 写手」的窗口）。本代理已经用「构造后挂载」把路铺好，CardDatabase 侧**一行**即可。
> ⚠️ R1/R3 落地后，`CardTraitsKnown` 会从 `false` 变 `true`，四条查询立刻开始真判 —— 届时请**同时**跑一次 `ruleprobe check` + `play check`（预期：**指纹逐字节不变**，因为今天没有任何引擎读点走这四个方法）。
