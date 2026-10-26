# B8_BE · 全池「召唤/生成」类卡：卡面文本 vs 结构化真值 全表对拍

> 执行代理 **B8_BE** · 2026-10-26 · 白名单 = 两份产物（本报告 + `判据/B8_召唤类全池对拍.tsv`）
> ⛔ 本批**没有改任何** `.cs` / `.py` / `账/*.tsv` / 正本。全部结论 = 只读取证 + 两个只读读数口（`ruleprobe play` / `ruleprobe seg`）。
> 明细表：`d:/4/Unity/资料/完全复刻/判据/B8_召唤类全池对拍.tsv`（**251 行** = 244 主表 + 7 条第二类全集）

---

## 〇 · 先说一处**简报与事实相反**（铁律 5 就地订正）

我收到的简报写：
> `Homing Beacon`：原版 `TAU28 Enforcer` → 我们原来给 `TAU11 Crisis Battlesuit`
> `Monster Hunters`：原版 `GOF91+GOF93+GOF79` → 我们给 `GOF79×3`

**方向反了。** 三处独立取证一致指向另一边：

| 取证口 | 读数 |
|---|---|
| `cards_truth.json`（SO 真值） `1688c614…` `abilities[0].logic[0].summon.unitsToSummon` | `["TAU11"]` = **Crisis Battlesuit** |
| 卡面英文 `cards_engine.json` `desc` | `Deploy a Crisis Battlesuit and give it Flank.` |
| `descZh`（官方中文表） | `部署一个 $$TAU11$$ 并给予它[[flank]]` |
| `report/B8_AN_教程与引擎小文件.md:31-32` 原文 | 「原版真值 `unitsToSummon=[TAU11]`（Crisis Battlesuit）· **改前 = TAU28**（错卡）· **改后 = TAU11** ✓」 |

⇒ **原版 = TAU11；改前我们给 TAU28；改后已是 TAU11。** `Monster Hunters` 同理：真值 `["GOF79"]`（卡面 `Deploy a Beast Snagga Boy, or 3 if …`），B8_AN 记的是「改前 GOF91+GOF93+GOF79（两张错卡）⇒ 改后 GOF79 ×3」。
本批全部按「真值 + 卡面」这一边执行；**简报里那句若被写进账，请改掉**。

---

## 一 · 结论（数字）

### 1·1 全集口径

| 口径 | 数 |
|---|---|
| `cards_truth.json` 里 `unitsToSummon` **非空**的条目 | **335 条** |
| 落在这些条目上的卡 | **244 张**（= 简报的口径，已复核一致；`count` 1351 张 SO 中） |
| 其中在我们卡池（`cards_engine.json` 1128 张）里 | **220 张 / 306 条** |
| 池外（无卡面文本可对） | **24 张** |
| 非空 `unitsToSummon` 的**卡引用**总数 | **444 个** |

### 1·2 判定（251 行明细表）

| 判定 | 行数 | 说明 |
|---|---|---|
| **一致** | **210** | 卡面文本与真值指同一张卡（含「随机池」「惰性值」「同名重复对象」三种已解释形状） |
| **不一致** | **17** | 其中 **2 条是解析层真缺陷（已实证）**，**6 条是真值缺口**，其余 9 条为低危/待查 |
| **池外** | **24** | 真值有、卡池无 ⇒ 没有卡面文本，未参与对拍（不影响我们行为） |

**按简报的四分法**（244 张主表）：`一致 210` · `不一致 10` · `原版数据本身矛盾/惰性 35 张（已并入「一致」并在差异类型列注明）` · `文本不可解析 **0**`。
⚠️ 「原版数据本身矛盾」在本批**没有一例**是真矛盾 —— 全部落成「原版**遗留惰性值**」或「同名重复 SO 对象」两种可解释形状（见 §二·1、§二·2）。

### 1·3 严重度

| 严重度 | 行数 |
|---|---|
| **中** | **8**（2 条解析层真缺陷 + 6 条真值缺口） |
| 低 | 90 |
| —（无需处理） | 153 |

---

## 二 · 最值钱的几类（同一形状重复出现几次，各举例）

### 2·1 🔴🔴 `unitsToSummon` **不是「召唤」专用字段** —— 25% 的原值是**惰性遗留**，照它当判据会造出成片假缺陷

`summonCriteria.unitsToSummon` 只在 `AbilityLogic.chosenAbility`（枚举名 `AbilityEffect`，
`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`）是**召唤/造牌/变身**类时才被原版读取：
`70 summonUnit` · `71` · `72` · `75 summonBuffedUnit` · `77` · `90 transformUnit` · `100 createCardInHand` · `101` · `270 createCardInDeck`。
其余（`10 doDamage` / `30 buffOthers` / `40 buffSelf` / `50 buffHand` / `130 stun` …）**原版不读这个字段**，值是设计者复制 ability 时没清干净的遗留。

| 口径 | 有效条目 | 惰性条目 |
|---|---|---|
| 条目（335） | **242** | **93（27.8%）** |
| 卡引用（444） | **333** | **111（25.0%）** |
| 卡（244） | 只有有效 173 · 两者都有 36 | **只有惰性 35（14.4%）** |

**交叉验证**（文本能否点名该单位 vs `chosenAbility`）：

| chosenAbility | 文本点名 | 文本没点名 |
|---|---|---|
| 100 createCardInHand | 117 | 16（**全是「random X 类池」**，见 §2·3） |
| 70 summonUnit | 64 | 5（3 条是同名重复对象，见 §2·2） |
| 75 summonBuffedUnit | 9 | 3（同上） |
| 40 buffSelf | 8 | **29** |
| 30 buffOthers | 9 | **17** |
| 270 createCardInDeck | 0 | 4 |
| 10 doDamage / 130 stun / 50 buffHand / 0 drawCard … | 3 | **8** |

**最典型的惰性遗留（卡面完全没有召唤，真值却挂着具体卡）：**

| 卡 | 真值（惰性） | 卡面 |
|---|---|---|
| `TAU28 Enforcer Battlesuit` | trigger 240 / chosen **30** → `Serene Unifier` | `Vanguard. Adjacent units have +1 [Ranged]. Companion 2: Guardian Drone` |
| `GOF52 Toxic Bonfire` | trigger 0 / chosen **10** → `Grot×2` | `Deal 2 damage to an enemy troop and Stun it` |
| `d3e99cb1… Witchfire` | trigger 0 / chosen **10** → `Storm Guardian` | `Deal 1-3 damage. 1 [Spirit Stone]: Repeat this effect` |
| `SOR15 Celestian` 及**全池共 23 张 Sisters 卡**（Aestred-Thurga · Battle Sister · Battle Sister Gunner · Celestian · Celestian Sacresant · Celestian Sacresant Aveline · Celestian Superior · Dialogus · Ephrael Stern · Erika Luminas · Hospitaller · Imagifier · Immolator · Junith Eruita · Ministorum Priest · Morvenn Vahl · Sacred Rose Sister · Simulacrum Bearer · Simulacrum Celestian · Sister Novitiate · Sister Superior · Triumph of St. Katherine · Zephyrim Superior） | trigger 655 `Pray` / 580 `Strike` / 120 `Slay` 等 + chosen **40 `buffSelf`**（个别 30）→ `Righteous Repugnance` | 全是 `Pray: …` 类句子，无一提到这张卡 |
| `461354d9… Hardwired Destruction` | chosen **30** → `Necron Warrior` | `Give +1 [Attack] and +1 [Ranged] to a friendly unit …` |
| `GOF33 Ard as Nails` | chosen **30** → `Follower of Gork` | `Give +2 Health to your units` |
| `GOF53 Extractor Rig` | chosen **50** → `Grot×2` | `Your next Vehicle costs 2 less` |
| `AM56 Vengeance for Cadia` / `AM55`（第二条） | trigger 90/190 / chosen 30·40 → `Shock Trooper Sgt.×3` | 均无此句 |

> 🔴 **对本项目的直接后果**：`B8_AN` 报告 §五·2（`报告/B8_AN_教程与引擎小文件.md:96`）建议「335 条 `summon.unitsToSummon` 已经在 `cards_truth.json` 里，逐张对拍即可」——
> **按字面照做会拿这 93 条惰性值当「原版真值」，报出 ~93 条假缺陷**（我第一版脚本就是这么报的，逐条查清后全部撤掉）。
> ✅ 它实际用的那 3 张（Homing Beacon / Monster Hunters / GOF96）**都在有效条目里**，那 3 条结论不受影响。
> 📌 判据：`AbilityEffect` 枚举（上列 9 个值）+ 本文 §2·1 的交叉表。

### 2·2 🔴 真值里「**同一张卡有两个 id**」：70 组同名「短 id（池内）+ GUID（池外）」，其中 **9 组召唤数据只落在池外那份**

`cards_truth.json` 的 `id` 是 SO 的 `uniqueId`。allcards 包里有 **70 组同名但两个不同 `uniqueId`** 的对象
（例：`Tempestus Scion` = `AM68`（池内）/ `66486b3f…`（池外）；`The Fang` = `SW48`（池内）/ `7210fc3a…` + `c988c599…`（池外））。

其中 **9 组**的**召唤条目只挂在池外那份上**，池内 id 一个 `summon` 都没有：

| 卡（池内 id） | 卡面 | 真值落在哪 |
|---|---|---|
| `AM51` Cadia Stands | `… at the start of your turn, deploy a Shock Trooper` | 池外 `44761bc2…`（trigger 31 → Shock Trooper） |
| `ASH71` Craftworld Convergence | `Choose one: … Deploy a Veteran Guardian …` | 池外 `cd9b6eaa…`（chosen 70 → Veteran Guardian） |
| `DA46` The Rock | `Choose one: … deploy a Sergeant Taaman …` | 池外 `8a028d37…`（chosen 70 → Sergeant Taaman） |
| `SW48` The Fang | `Choose one: Deploy a Grey Hunter; …` | 池外 `7210fc3a…`（chosen 70 → Grey Hunter） |
| `61046837…` Helspear Assault | `Slay: Deploy a Black Legionary` | 池外 `3df005b0…`（trigger 120 → Black Legionary） |
| `c433e3dd…` Master Outrider | — | 池外 `70ce5448…` |
| `UM117` No Respite | — | 池外 `79fe7580…` |
| `EC51` Coterie of the Conceited | — | 池外 `e9d68c65…` |
| `BL37` Abaddon's Chosen | — | 池外 `f3df0cfc…` |

⚠️ **这不是我们复刻的错**（我们走文本解析，行为不受影响），但**任何「按 id 把 truth 连到池子」的分析都会静默漏掉这些卡** ——
`AM76 Armoured Lightning` 那条就是同一个坑的镜像面：真值指 `66486b3f…`（池外），我们落 `AM68`（池内）—— **同名的同一张卡**，`play AM76` 读数 3/3 与卡面 `three Tempestus Scion` 一致。

### 2·3 🟠「**随机池**」共 **24 张卡**：真值是候选池，原版按 `summonAllTargets == 0 ⇒ GetRandomInt` **取 1**

判据 = `decomp_full/AbilityLogic__GetCardToSummon.c:191-206`：候选非空 ∧ `summonAllTargets == 0` ⇒ `GetRandomInt(0, count)` 取一个；`summonAllTargets == 1` ⇒ 全取。
**卡面写法各不相同**（`a random Combat Elixir` / `a random Sabotage` / `A random Black Legion Psychic Power` /
`Deploy a Grot or a Snakebite Grot at random` / `Choose one: … `），但真值形状一致。

| 池 | 候选数 | 被引用的卡（举例） |
|---|---|---|
| Combat Elixir | 6 | `EC17` `EC44` `EC45` `EC52`(×3) `b9665ead…` Euphoric Strike |
| Sabotage | 4（`forEnemy`） | `GSC7` `GSC15` `GSC16` `GSC27` `GSC40` `GSC45` `GSC70` |
| Black Legion Psychic Power | 3 | `BL23` `BL36` |
| Dark Angels「Rites」 | 5（`chosen 270 createCardInDeck`） | `DA1` `DA2` `DA3` `DA75` —— ⚠️ **卡面完全没写**，见 §四 |
| Grot / Snakebite Grot | 2 | `GOF87`（`play` 已实证落 `GOF78` ✓） |
| Hunting Wolf / Fenrisian Wolf | 2 | `SW18`（走 `transformUnit`，`EffectResolver` 的 become 支承接） |

⚠️ **别把「候选 N 个」当成「要召 N 张」** —— 那是 `summonAllTargets == 1` 才成立的语义。

### 2·4 🟢 解析层**真缺陷只有 2 条**（都有实打读数，不是静态推断）

| # | 卡 | 卡面 | 现状（实证） | 真值 | 修法 | 严重度 |
|---|---|---|---|---|---|---|
| 1 | `65cfec7d…` **Lead the Righteous** | `Deploy a Battle Sister. 8 ☀: Deploy an additional Battle Sister` | `play` 原文：「部署不了 —— 「additional battle sister」既不是原版卡名、也不是我们认识的兵种词（…最接近的是「Battle Sister」，但名字对不上，**没有**拿它顶替」⇒ **第二条部署整条不结算** | `SOR5 ×2` | **解析层**：`CreatePool.Resolve` 在剥 `random ` / `ephemeral ` / `copy of ` 之外，**再剥 `additional `**（同类修饰词） | **中** |
| 2 | `GOF94` **Painboss** | `Rally: Deploy a random friendly Beast that died this game` | `play` 原文：「按兵种「beast」在卡池里一张都没筛到（**这条没生效**）」 | 真值只有天赋（`Dok’s Toolz`）—— **这一段真值本身没有结构化表示**，只能按兵种筛 | **解析层**：`CreatePool` 的兵种词表里 `beast` 要能命中（池内 `subtype == Beast` 有 **15 张**） | **中** |

> ⚠️ **一条方法论**：我第一版静态判定把 `EC58 Vengeful Surge`（`Deploy an Infractor and a Tormentor`）判成「第二个目标丢失」——
> `seg` 确实只打出 1 条 op（`payload=[infractor]`），但 `play EC58` 实打 **2/2**（`Infractor→2、Tormentor→7`）。
> ⇒ **`seg` 的 op 列表不是运行时真相**，凡「漏结算」结论必须有 `play` 读数兜底。

**其余 8 条初判「不一致」全部在查清后化为「一致」**（这是本批最有价值的一次反向验证）：

| 卡 | 初判 | 查清后 |
|---|---|---|
| `AM55 Mechanised Infantry` | 真值多出 `Shock Trooper Sgt.×3` | 那条是 trigger 190 / chosen 40 = **惰性**；`play` 落 `AM5×3` ✓ |
| `AM76 Armoured Lightning` | 真值 id 与解析目标不是同一个 | **同名重复对象**（见 §2·2）；`play` 落 `AM68×3` ✓ |
| `GOF87 Zodgrod` | 真值 3 个、`play` 只落 1 个 | **随机池取 1** ✓ |
| `SAU28 Ghost Ark` / `SAU34 Monolith` | 真值含 `Reanimate` | 那是 **trigger 46 Talent**（天赋卡）✓ |
| `SOR34 Celestian Superior` | 真值含 `Righteous Repugnance` | trigger 655 / chosen 40 = **惰性** ✓ |
| `TAU19` / `TAU28` / `TAU29` | 真值 → `Serene Unifier` | trigger 240 / chosen 30 = **惰性**；TAU19 那条真天赋另有 trigger 46 条目 ✓ |

### 2·5 🟢 `ruleprobe play` 全批读数：**没有一张卡落错卡**

| 口径 | 数 |
|---|---|
| 跑了 `play` 的召唤类卡（池内） | **220** |
| 真的落了 ≥1 张（非自身）的 | **49** |
| 其中落场卡名**不在卡面文本里**的 | **0**（另有 2 张 `TAU19`/`TAU28` 命中探针夹具 `FIX_WAY`，是夹具噪声不是召唤） |
| 有「本版没结算」告警的 | **2**（= §2·4 那两条） |

⇒ **`B8_AN` 修的那一处（`CreatePool.Resolve` 未知限定词被静默丢掉）没有残留同类**：49 张实打的落场卡，卡名逐一都能在卡面上找到。
剩余风险已不是「落错卡」，而是「**没落**」（§2·4 的 2 条）。

### 2·6 🟡 卡名归一：**40 组 id→name 不一致**，其中 1 组是**真撞名**

`cards_truth.json` 的 `name` 取的是 SO 的 `cardName`，`cards_engine.json` 的 `name` 取**官方显示名**。40 组不同，绝大多数是同一张卡两种写法
（`DA1` `Azrael, Grand Master` vs `Azrael`；`BL57` `Helfire Torch` vs `Hellfire Torch`；`SOR37` `Triumph of Saint Katherine` vs `Triumph of St. Katherine` …）。
但有一组是**两张不同的天赋撞了同一个 `cardName`**：

| id | SO `cardName` | SO `m_Name`（资产名） | 官方显示名 |
|---|---|---|---|
| `e59ac9e4…` | `Path of the Warrior` | `Z-NC SaimHann-Skyborne Deployment` | **`Skyborne Deployment`** |
| `1bfe0893…` | `Path of the Warrior` | `Z-NC SaimHann-Path of the Warrior` | `Path of the Warrior` |

⇒ 按 `name` 反查 id 会把 `ASH29 Wave Serpent` 的 `Talent: Skyborne Deployment` 连到 `Path of the Warrior` 上。
另：**曲引号 / 前导撇号**（`’Uge Choppa` U+2019 vs 卡面 `'Uge Choppa` U+0027、`Dok’s Toolz`）会让「按名查」落空 —— 天赋路径目前由 `TalentCardData` 真值表兜住，但只要有新卡走按名查就会静默失败。

---

## 三 · 修法归口

| 归口 | 条目 | 具体 |
|---|---|---|
| **解析层 `EffectText` / `CreatePool`** | **2 条（中）** | ① `CreatePool.Resolve` 剥 `additional `（Lead the Righteous）② 兵种词 `beast`（Painboss） |
| **数据侧 `工具/gen_cards_truth.py`** | **6 条（中）+ 1 条（低）** | ① **同名重复对象 9 组**（§2·2）：其中 5 组卡面点了名、进了明细表（`AM51`/`ASH71`/`DA46`/`SW48`/`61046837…`），另 4 组（`c433e3dd…` Master Outrider · `UM117` No Respite · `BL37` Abaddon's Chosen · `EC51` Coterie of the Conceited）没点名就没有行；② `4d348cff… Hymn of Battle` 的 `Deploy a Crusader` **真值彻底没有**；③（低）`c4f5c920… Drone Companion` 的「Gun/Guardian/Marker Drone 三候选」没落成 `unitsToSummon` |
| **数据侧（建议，不改行为）** | — | ① `cards_truth.name` 换官方显示名（或加一列 `displayName`）② 同名重复对象合表（按名归一）③ 惰性条目加一列标记（`unitsToSummonIgnored`），免得下一个人再拿它当判据 |
| **`cardface_fixes.json` / 卡面组装层** | **0 条** | 本批所有差异都**不在**卡面层（文字/图标/数值） |
| **`CreatePool` 之外** | 0 | — |
| **无需修** | 210 + 24 | 惰性遗留值（93 条）· 随机池（24 张）· 同名重复对象（按名等价）· 池外卡（24 张） |

**三条低危「结构多余」待定**（不是我们做错，是原版真值与卡面对不上）：
`SOR70` Sororitas Rhino · `SOR71` Adelaide the Serene · `SOR72` Castigator —— 三张的 `activeAbility` 里都有一条 **`chosen 70` 召唤自身**，卡面/解析都没有对应文字。
`SOR70` 更是同时有「TurnEnd → Sacred Rose Sister」（卡面有 ✓）和「activeAbility → 自己」两条。

---

## 四 · 没查清的部分（⛔ 都写成「没查清」，不拿猜测填空）

1. **`trigger 240 (WhileInPlayVariable)` 的语义**：全池 4 条（3 条指向 `Serene Unifier` 这类 tactic、1 条指向单位）。
   我把它判成「惰性」的**唯一**依据是它的 `chosenAbility` 是 30（`buffOthers`，不进召唤分支）—— **没有**逐句读
   `AbilityLogic__PlayAbility.c` 里 240 那一段的 switch。若 240 有独立分支读 `summonCriteria`，这 4 条就得重新定级。
2. **「Duty」在 AM 系卡上的 trigger 映射**：6 张卡的卡面写着 `Duty: Deploy …`（`AM2` `AM10` `AM28` `AM34` `AM70` `AM73`），
   但真值里 `trigger == 662 (Duty)` **一条都没有**；这些卡的 trigger 分布是 `{46, 0, 301, 410, 240, 180}`。
   ⇒ **⛔ 不能按 `trigger 662` 去筛真值里的 Duty 效果**；承载它的是哪条 trigger，**没查清**。
3. **`DA1/DA2/DA3/DA75` 的 `chosen 270 = createCardInDeck`**（往牌库塞随机「Rites」候选）**卡面一个字没写**。
   是原版隐藏机制还是惰性值？**没查清**（`270` 在枚举里是真效果码，但卡面完全无对应文字）。
4. **`SOR70/71/72` 的 `activeAbility` 自召唤**是不是原版隐藏机制（「这张卡进场自带一个副本」）？**没查清**。
5. **夹具覆盖**：220 张里只有 **49 张**的召唤在夹具里真触发（其余是 Rally/Duty/Strike/Pray/Mob/条件分支没满足）。
   ⇒ 那些卡的「我们现值」是**文本解析读数**（`seg` 的 `payload`），不是实打读数。
6. **池外 24 张卡**：`cards_engine.json` 不含它们（无 `desc`/`descZh`），本轮**没做文本对拍**，只列了真值。
7. **`cards_truth.name` vs 官方显示名 40 处不一致**：只把 `e59ac9e4…` 那一组判成「真撞名」（有 `m_Name` 佐证），
   其余 39 组**没逐条判**「SO `cardName` 过期还是官方表改名」。
8. **`cards_raw.json` 有 `full_raw` 双份**（CLAUDE.md 已记）—— 本批统计**一律用 `cards_truth.json`**（单份），
   没有一处递归扫 `cards_raw`，所以「恰好翻倍」那个坑本轮没踩到；但也没顺手核 `cards_raw` 与 `cards_truth` 的逐字节一致性（那是别人的账）。

---

## 五 · 复现命令（只读）

```bash
# ① 单卡实打（结算层）：落场/生成 逐条
dotnet D:/4/Unity/工具/ruleprobe/bin/Debug/net8.0/wfprobe.dll play "Lead the Righteous"
# ② 单卡文本解析：看 deploy/create 的 payload（⚠️ 不是运行时真相，见 §2·4 的方法论）
dotnet D:/4/Unity/工具/ruleprobe/bin/Debug/net8.0/wfprobe.dll seg "Deploy an Infractor and a Tormentor"
# ③ 本批 220 张的 play / seg 读数原件（按卡 id 一份）
#    C:/Users/qjh36/AppData/Local/Temp/b8be/{play,seg}/<卡id>.txt
# ④ 明细表
#    d:/4/Unity/资料/完全复刻/判据/B8_召唤类全池对拍.tsv
```
⚠️ ③ 是**临时目录**，重启后会被系统清掉；要长期留证就把需要的几份拷进 `资料/普查产出_<会话>/`。
