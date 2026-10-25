# F · CardTrait 对账（第 1 轮）

> 对象：原版 `sharedassets0.assets` 的 **74 条 `CardTrait` SO** ↔ 我们的关键词实现（数据 + 代码）。
> 产出：`资料/完全复刻/判据/CardTrait对账.md`（逐条）· `资源/R6_补核.md`（9 个「只在 Resources」的对象）。
> ⛔ 没改 `.cs` / 没改账 TSV / 没动 git / 没跑 Unity / 没跑 `gen_replica_ledger.py`。

---

## ① 数字

| 档 | 数量 | 说明 |
|---|---|---|
| **① 一致** | **73**（原 64） | SO 的触发槽全部落在我们的泛型分派里（或原版该槽本身不播） |
| **② 语义不同** | **0**（原 9） | 🔴 **2026-10-25 改判**：两处曾判「语义不同」，都已被一手判据推翻/修好 ⇒ 全部转 ①。见 §2.2 |
| **③ 我们完全没有** | **0** | 74 条原版词条我们都有数据落点 |
| **④ 特殊（不是词条）** | **1** | `TraitsCollection` = 集合 SO（77 条 trait 对），不是一条词条 |
| （反向）我们有但原版枚举没有 | **2 个 token 待核** | `Destroyer`(10 卡) / `Penitence`(5 卡) —— 卡表字符串，不是实现 |

**辅助核对（全部通过）**

| 核对 | 结果 |
|---|---|
| 我的 R1 解码 ↔ 工程已有 `数据/游戏数据/trait_collection.json` | **74/74 逐字段一致**（两个独立解码互证；唯一"差异"是我这边的显示伪影：`TriggerType=0` 被我的展平器当假值丢了） |
| 词条图标 | **66/66 全有**（`素材/Warpforge原版/UI图集/图集/40ktraiticonatlas/slices/*.png`）；另 8 条原版 `_traitIcon` 本来就是空 |
| `DefinedTrait` 枚举 ↔ SO | 枚举 **134** 条 → **77 条有 SO**、**57 条没有**（原版本身没给表现元数据） |
| 触发档分布（原版现数） | `Start(0)` 13 · `End(1)` 6 · `OnPlay(2)` 18 · `Trigger(3)` 21 · **`OnHand(4)` 0** · `FromCode(5)` 10 · `TriggerOnPlay(6)` 7 |
| FromCode 绑定 | 10 条全部有绑定行（`Resources/trait_effect_bindings.json`，68/75 有 prefab、7 条原版空引用） |

---

## ② 判据

### 2.1 先厘清「`CardTrait` SO 到底定义了什么」（不然会对错东西）

`dump.cs` 里 `CardTrait : ScriptableObject` 只有 8 个字段：
`_traitIcon / _hasInPlayAnim / _hasIcon / _isInPlayIcon / _stackable / _aIScore / customObject / _traitEffects`。
**它是「表现元数据」，不是规则定义**（规则在 `DefinedTrait` + `CardScript`/`BattleManager`）。
所以本轮对的是：**① 图标与显示开关 ② 每条 `TraitEffect` 落在哪个触发档、放哪个 CardAnim ③ 阵营覆盖 / customObject**。

枚举语义取自 `dump.cs`（**不是我们猜的**）：
- `EffectTriggerType` = `Start 0 · End 1 · OnPlay 2 · Trigger 3 · OnHand 4 · FromCode 5 · TriggerOnPlay 6`（TypeDefIndex 405）
- `TraitStackType` = `replace 0 · add 10 · count 20`（TypeDefIndex 404）
  🔴 **`_stackable` 不是层数**：72 条 = 10（`add`）、1 条 = 20（`count`，`MarkOfChaos`）。上一轮我把它读成「10 层」，**已就地订正**。

### 2.2 🔴 改判记录：两处曾判「语义不同」→ **已改判「一致」**（2026-10-25，保留痕迹）

**（1）`TriggerOnPlay(6)` 去重表的写入时机** —— 曾判影响 7 条
（`Agenda` / `BloodThirst` / `Duty` / `Ecstasy` / `HuntMark` / `Pray` / `Uprising`）

- **原判**：原版在「动画播完的回调」里写表，我们同步写 ⇒ 被打断时原版会重播、我们不会。
- **推翻（一手判据）**：`d:/2/tools/decomp_full/BattleManager__CreateAnim.c` —— `Instantiate(...)` 之后
  **紧接着** `(**(code **)(param_8 + 0x18))(*(param_8 + 0x40), lVar6, *(param_8 + 0x28))` 调那个回调，
  而 `AddComponent<CardAnimController>` + `CardAnimController__Initialize(...)` **在它之后**
  ⇒ 那是**实例化回调**、不是「播完」回调 ⇒ 与我们「同步写」**等价**。
- **边界（如实标）**：原版可能先等 CardAnim 的 `delay` 才开始实例化 —— 能核到的 `Pray_Idle`
  三个时间字段全 0，其余本地缺该时间表 ⇒ **未逐条核**。

**（2）`overrideByArmies` 的阵营来源** —— 曾判影响 2 条：`Shield`(4 覆盖) / `Invulnerable`(6 覆盖)

- **原判**：原版读卡自己的 `cardArmy`，我们只能从 `CardView.Data.faction`（= 拥有者阵营）反推。
- **已修（本轮亲自核过代码，不是转述）**：
  `BattleDriver.cs:26529-26530` 现在是
  `string cardFaction = (u.Card != null && !string.IsNullOrEmpty(u.Card.Faction)) ? u.Card.Faction : faction;`
  并在 `:26548` 写进 `CardData.faction`；`Core/TraitFrames.cs:103-105` 的注释确认
  **`TraitFrames.ArmyId` 读的就是 `CardData.faction` 这一格** ⇒ 拿到的已是卡自己的阵营 ⇒ **偏差消除**。

> 结果：**75 条语义判定里 0 条「语义不同」**（74 条对账 + 1 条特殊）。四档统计见 `判据/CardTrait对账.md` §3。

### 2.3 其余「看起来像差异、其实不是」的（逐条给了证据）

| 情况 | 条数 | 为什么不是差异 |
|---|---|---|
| 只声明 `End(1)` | 1（`Ambush`） | 原版三处调用点（`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait`）**都不处理 End(1)** ⇒ 双方都不播 |
| `effectAnim` 是空引用 | 7 | 原版那条 effect **本来就无动画**（`trait_effect_bindings.json` 的 `unresolvedRows` 逐条写了） |
| `_traitEffects` 为空 | 若干 | 原版不播任何词条特效 |
| `OnHand(4)` | 0 条 | 枚举有、**本 build 数据里一条都没有** |

### 2.4 「我们有没有用」的判定口径

- 「一致」= ① 该 trait 的 `TraitEffect` 行在我们 `trait_collection.json` 里逐字段相同 **且**
  ② 它用到的每个 `TriggerType` 都有**具体代码落点**（`Core/TraitFrames.cs:372 / :394 / :860-894`、
  `Core/TraitParticles.cs` + `trait_effect_bindings.json`），或原版该档无调用点。
- ⛔ **没有一条是「名字出现了就算一致」** —— 上一轮 `r5` 的「规则层/显示层」分档只用来**找候选**，
  本轮逐条看的是**触发档 → 代码落点**。

---

## ③ 产出清单

| 文件 | 内容 |
|---|---|
| `资料/完全复刻/判据/CardTrait对账.md` | §0 口径 · §1 七档→代码落点 · **§2 74 条逐条表**（含图标/数据/代码落点/判定/台账档）· §3 四档汇总 · §4 例外与「取法不同但等价」· §5 反向（57 条无 SO）· §6 反向（我们卡表里多出来的 token）· §7 **台账里已判过的（别重复立项）** |
| `资源/R6_补核.md` | 9 个「只在 sharedassets0」的对象逐个补核（含 `#HB_GOF28` 的双向 PPtr 证据链） |
| `_tmp_view/wfres/cardtrait_raw.json` | 74 条结构化数据（上一轮产物，本轮复用） |
| `_tmp_view/wfres/definedtrait.json` | `DefinedTrait` 134 条枚举（从 `dump.cs` 解析） |

---

## ④ 脚本用法（可重跑）

```bash
cd d:/4/_tmp_view/wfres
PY="D:/2/Warpforge_tools/py312/python.exe"
$PY -I -X utf8 r5_cardtrait.py        # 74 条 SO → 判据/CardTrait词条权威表.md（含本次订正的 _stackable 口径）
$PY -I -X utf8 r7_trait_reconcile.py  # 74 条对账 → 判据/CardTrait对账.md
```
`r7` 的输入：`cardtrait_raw.json`（R1 解码）、`数据/游戏数据/trait_collection.json`、
`MyGame/Assets/CardPresentation/Resources/trait_effect_bindings.json`、`definedtrait.json`、
`素材/…/40ktraiticonatlas/slices/`（图标点检）、`资料/完全复刻/账/*.tsv`（**只读**台账档）。
耗时 < 5 秒。

---

## ⑤ 没查清 / 不能下结论

1. **「语义不同」只覆盖了能静态证的两处**。运行期行为（比如状态框的实际观感、被打断后的重播）
   **没法离线证**，要真 Play 才知道。本轮没跑 Unity。
2. **台账档是「方法级」的**（一行 = 一个原版方法），不是「trait 级」。表里 `A×1` 只表示
   提到它的那些方法里**有一条**判过 A，**不能当完成度**。
3. **`Destroyer` / `Penitence` 两个 token 判不了**：`DefinedTrait` 与 `Card_Trait/*` 都没有它们，
   只在**粉丝规则书**里出现 ⇒ 要么是卡面正文被误抽成 trait，要么原版有但枚举名不同。需要看那 15 张卡的卡面。
4. **57 条 `DefinedTrait` 没有 SO** —— 我只能说「原版没给表现元数据」，**不能**说这些 trait 没有规则语义。
5. **`_aIScore` 74/74 全是 0** ⇒ 这个字段在本 build 里**不承载信息**；别拿它推 AI 行为。
6. **`UM_Forge_Badge` 判不了**（见 `R6_补核.md` §4）：0 PPtr、0 容器、0 字符串字面量 ⇒ 只能按名加载，
   静态图给不出证据。

---

## ⑥ 顺手发现

1. 🔴 **`Core/CardView.cs:7060` 那句「CardTrait 资产本身本地没有（全库 grep `_traitIcon`/`customObject` 0 命中）」是错的**。
   资产**就在** `sharedassets0.assets`（pid 413–446 + 458–531），当年 grep 不到是因为它们**正好是「解不出」的 905 个**
   （没有类型树 ⇒ 导出 JSON 全失败）。⇒ 那句话的**结论（拿不到）不成立，但取法（走材质 `_CardImage`）依然等价**，
   因为两边取的是**同一张贴图** `GenestealerCults_special_Ambush`。**建议由能改 `.cs` 的批次按铁律 5 就地订正那行注释。**
2. 🔴 **`trait_collection.json` 里 4 个 `customObject` GUID 标为「未解」，其实都能解**
   （`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` 有完整行）：
   `GenestealerCults_special_Ambush`(Sprite) · `BloodThirstEffect_Gain`(GameObject) ·
   `CrueltyEffect`(GameObject) · `Swarm Card Merge VFX`(GameObject) —— 最后一个**我们代码已经在用**
   （`Battle/BattleDriver.cs:23591`）。**建议把这份"未解"清单订正掉。**
3. **`AlternateArtCard #HB_GOF28` 是真漏网**（`R6_补核.md` §3）：原版 `GOF28.alternateArts → #423` 双向 PPtr，
   而我们 `cards_engine.json` 的 `alternateArts` **0 张卡有值**、`#HB_GOF28` 全工程零命中。
   判据齐、只差「生成 `card → altArt[]` 表并接进卡表」。
4. **`Ultramarines_war_Uriel Ventris_tutorial1`**（`cardName = "Uriel Ventris (tutorial)"`，2 费 30 血 2/2 战将）
   **不在我们卡表里**，但我们的 `lore/ai_scoring` 原始数据里有它 ⇒ 只有「逐字复刻原版教程战」时才需要补。
5. **我们的卡表里有 5 个 token 不在 `DefinedTrait`**：3 个是拆分伪影（`Long Range`/`Blood Thirst`/`Can't Attack`），
   2 个（`Destroyer`/`Penitence`）待卡面核 —— **数据卫生问题，不是实现问题**。
