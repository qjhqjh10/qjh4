# 第三方复刻「Cardboard Console」× 粉丝实体规则书 × 我们 —— 关键词与规则对照

> 2026-09-24 建立。**触发**：用户提供了一个爱好者做的网页版复刻（`silly-optical-subject-mold.trycloudflare.com`），
> 说它「已经在使用」，要我们和它对比规则与引擎。
> ⚠️ **本文是「对照」，不是「权威」** —— 按铁律 2，原版语义的权威永远是**原版反编译（第一权威）+ 粉丝实体规则书（第二，非官方）**；
> 第三方复刻（和我们自己的 Godot 原型一样）**只能当旁证**。

---

## 一、它是什么（实测，2026-09-24）

- **名字**：`📦 Cardboard Console`，React SPA，横屏专用，有三个界面 `match / workshop / collection`。
- **同时支持两款游戏**：`wf_*` = **Warpforge**、`hhl_*` = **HH:Legions**（规则集注册表 `wforiginal` / `horus_heresy_legions`）。
- **卡牌别名**：`/^(wf|hhl)_(foundation|wild|legacy)_[a-z0-9_]+$/` —— 例 `wf_foundation_ultramarines_marneus_calgar`。
- 🔴 **它的卡 id 不是原版 id**：`UM46` / `GOF65` / `TAU58` 全文 **0 命中**（用 `[A-Z]{2,4}\d{1,3}` 扫也 0）。
  ⇒ **它救不了我们那条「原版 id → 卡名」的缺口**（这是当初去看它的初衷）。
- 它的 UM 督军头像 **7 个齐全**（Marneus / Guilliman / Titus / Uriel / Varro / Valius / Letharius），与我们已知的 7 个督军一致。

### 🔴 最要紧的结构性结论：**它不是一个「引擎」，是「服务器权威的回放器」**

- 全文件 **零战斗数学**：没有 `health -=`、没有伤害钳制、没有护甲运算。
- 规则与结算**全在服务端**：客户端只收 `match:state`（快照 + 单调 `seq`）与 `match:events`，**按 `seq` 顺序播动画**。
- 客户端唯一改数的地方是 `blind`（把远程显示值置 0）。
- ⚠️ **卡牌数据也不在包里**（属性一律查不到）：运行时从后端取（`/cards`、`/cards/text`、`/keywords`、`/rulesets`…）。
  包内只有「辅助索引」：别名孪生表、按别名索引的音效表、Warpforge 卡图文件名表（`kD`，35,531 字符）、
  以及 12 张工坊演示假卡（`wsv2-N`）。

⇒ **所以「对比引擎」这件事只能做到「协议 + 关键词 + 若干常量」这一层**，真正的战斗数学看不到。

---

## 二、文件（已保存）

`d:/4/Unity/素材/第三方参照_CardboardConsole/`（**`素材/` 是 gitignore 的本地大件区，不进仓库**）

| 文件 | 大小 | 是什么 |
|---|---|---|
| `app_index.js` | 1.6 MB | 主包（原样） |
| `app_index.pretty.js` | — | **按 `;{}` 断行后的版本（41,894 行），grep/阅读用这一份** |
| `app_compat.js` | 5.7 MB | 兼容版包（内容与主包大概率重叠，未细看） |
| `app_index.css` | 0.6 MB | 样式 |
| `shell.html` | 4.7 KB | 页面外壳 |

---

## 三、关键词三方对照（本次核心）

### 3·1 它的关键词表 `xm`（`:19318-19320`，共 **48** 条）

⚠️ **这 48 条不是「Warpforge 关键词表」** —— 作者把**两款游戏**的词混在一张扁平表里（他只在少数几条里标注了规则集差异，
例如 `armour` 那条写着 "…to a minimum of 1 **where the ruleset uses that prevention rule**"）。

**逐条与粉丝实体规则书 61 条比对的结果：**

- ✅ **28 条能在规则书里找到对应**：`armour` `artifice` `backlash` `blast` `blind` `bloodthirst` `camouflage` `codex`
  `ephemeral` `fast` `flank` `flying` `invulnerable` `rally` `regeneration` `sentry` `shield` `slay` `sniper` `stealth`
  `strike` `stun` `swarm` `synapse` `talent` `tide` `vanguard` `vulnerable`
- ❌ **20 条在规则书 61 条里找不到**（疑属 HH:Legions，或作者自定义）：
  `battle_honour` `death_touch` `drop_pod` `efficiency` `front_line` `link` `maintenance`
  `mark_of_chaos` `mark_of_khorne` `mark_of_nurgle` `mark_of_slaanesh` `mark_of_tzeentch`
  `ordnance` `poison` `poisonous` `relentless` `resolution` `speartip` `terror` `unstoppable`
  （术语佐证：`ordnance` 释义讲 "Vehicles and **Structures**"、`link`/`synergy` 一族 —— 都是 HH:Legions 的词。）

- 🔴 另有**两张按规则集分套的表**（比 `xm` 更接近「真实用到的关键词」）：
  - `j2`（`:26886`）**分套主动技能关键词**：`wforiginal` → **`duty` `pray` `ferocity` `agenda` `faith` `oath`**；`horus_heresy_legions` → `siege`
  - `wO`（`:16218`）**HH:Legions 专属音效**、`dN`（`:16280`）音频 cue 表里另有 `ambush` `concussive` `cruelty` `ecstasy`
    `long_range` `markerlight` `mob` `penitence` `regiment` `shuriken` `stimulation` `teleport` `unstable` `uprising` `waystone` 等
    —— **这些正好补上规则书里我们有、而 `xm` 没有的那一批**。

### 3·2 我们这边（`RuleEngine`，2026-09-24 实读）

- **规则书 61 条：我们 58 条有机制** + **3 条走别的通道**（`faith` 无关键词级机制 · `quest` 判据挂在「获得任务点」事件 ·
  `spiritstone` 走 `N [Spirit Stone]:` 前缀通道）⇒ **61 条全覆盖**。
- **「仅图标无机制」= 0**、**「认不出」= 0**（`CardDef.cs:2066-2267` 的 `KeywordTable.Implemented` 是代码正本）。
- ⚠️ 反向缺口：**有机制但没图** 3 个 —— `quest` / `spiritstone` / `ability`（`资料/关键词图标_现状与总表.md:172`）。

⇒ **结论：关键词这一层，我们没有可向它学的地方** —— 它 48 条里只有 28 条与 Warpforge 对得上，
我们 61 条全覆盖。**唯一值得借的是它的「英文释义措辞」**，可以拿来核官方中译有没有跑偏。

---

## 四、常量/流程对照

| 项 | 粉丝实体规则书（非官方） | 第三方（协议/客户端侧） | 我们（`文件:行号`） |
|---|---|---|---|
| **先手** | 掷骰/抛硬币决定 | **按 Warlord 的 `initiative` 字段**：高者先手，平局掷币；档位 `1 Low / 2 Medium / 3 High / 4 Very High` | **本作玩家恒先手**（`RuleCore.cs:63-65`）—— **我们挑的**，掷骰未做 |
| **能量** | 起始 1，每回合 +1；不跨回合保留 | 每回合 = `min(energyMax+1, cap)`；展示模式 `cap:10` | `MaxEnergy = TurnCount + 1` 并回合开始回满（`RuleCore.cs:451-452`）—— **照原版** |
| **护甲减伤** | 减 X，**最低 1 点** | 词条释义同规则书；**运算在服务端** | `Math.Max(1, actual - Armor)`（`RuleCore.cs:1713`）—— **照原版** ✓ |
| **伤害顺序** | 规则书只给了「攻击 → 双方结算伤害 → 归零触发效果 → 摧毁方触发效果」 | 客户端**不做**结算，只在动画步表里排序 | 四道：**盾挡 → 无敌 → 易伤/护甲 → 扣血**（`RuleCore.cs:1731-1740`；⚠️ **2026-10-17 更正：这里原来写的是「照 `rule_core.gd:4406`」** —— `rule_core.gd` 是**我们自己的 Godot 复刻、非权威**（铁律 2 的 2026-09-18 更正），只作旁证；🔴 真判据 = `d:/2/tools/decomp_full/EntityScript__DamageKillsTarget.c` · `BattleManager__CheckIfAttackKillsTarget.c`） |
| **牌库抽空** | **不判负** —— 每次抽牌督军吃疲劳伤害（次数递增） | **无疲劳/抽空逻辑**（全文件无匹配） | 有疲劳（`DeckRules`） |
| **回合上限** | **没有**（只有 Overtime） | 常规局**查不到**；特殊 events 模式才有 `turns` 上限 | 无硬上限 + Overtime（后手 MaxEnergy≥10 触发） |
| **胜负** | 敌方督军生命归 0；同时归 0 = 平局 | `winner===null` 才是平局；`reason==="concede"` 或 `"death"` | 同规则书 |
| **动作菜单优先级** | — | `{LEADING:0, INFO:50, PRIMARY:100, SUPPLEMENTAL:200, TRAILING:300}`，同档按插入序稳定排序（`:19762`） | **没有独立的「结算阶段/优先级表」** |
| **关键词触发时机名** | — | `on_play` / `on_deploy` / `on_attack` / `on_attack_melee\|ranged` / `on_attack_select` / `on_trigger` / `on_grant` / `choice:<n>` / `keyword:<id>` / `play` / `death` / `ability` / `resource_payoff`（`:22114`） | `WhenEvent` 21 个取值（`Core/WhenEvent.cs:74-212`） |

### 它的「动画步骤表」（`:20211`）—— **不是规则顺序，别当规则抄**

`capture`(离场残影) → `sound` → `reveal` → 逐 duel：`duelOpen` → `commit` → `floats` → `duelResolve` → `death` → `dwell`。
它只决定**演出顺序**，规则顺序在服务端。

---

## 五、结论与建议

1. **它救不了「原版 id → 卡名」**（它的 id 是 `wf_foundation_…` 别名体系，与原版 id 无关）—— 那条线仍要靠别的办法。
2. **「引擎对比」只能到协议/关键词/常量这一层** —— 它的战斗数学在服务端，包里没有。
3. **关键词我们全覆盖（61/61），它只覆盖 28 条 Warpforge 的** ⇒ 这一层没有可学的。
4. **有一件事值得做**：它的 **28 条英文释义**可以拿来**核我们自己那份中译有没有跑偏**（尤其 `armour` 那条它自己都标了跨规则集差异）。
   ⚠️ 但它是**旁证**，冲突时以**反编译 / 卡面原文**为准（⚠️ **规则书非官方** —— 粉丝实体版，见 `CLAUDE.md` 铁律 2）。
5. **它的「先手 = Warlord `initiative` 字段」是一个我们没实现的东西**（我们玩家恒先手）——
   值得回规则书/反编译核一下原版到底怎么定先手（规则书写的是「掷骰/抛硬币」，与 `initiative` 不完全一致）。
6. **注意别被污染**：这张 48 条表**混了两款游戏**，直接照搬会把 HH:Legions 的词带进 Warpforge（本文件已把两者分列）。

---

## 六、它的「id 体系」与我们卡池的对照（2026-09-24 实测）—— **本轮最有价值的产出**

### 它没有序号

它的卡标识是**纯 slug**：`wf_foundation_<阵营>_<卡名>`（如 `wf_foundation_ultramarines_marneus_calgar`），
**一个数字都没有**（1126 个里只有 6 个含数字，且都是卡名自带的：`1st_company_terminator`、`7th_company_bike`、`ds8_support_turret`）。
⇒ **「序号一致吗」这个问题在它身上不成立** —— 它压根不用序号，也**依旧给不出原版 id**。

### 🔴 但 `wf_foundation_*` = **1126 个 = 我们卡池的 1126 张**（数量完全相等）

把它的别名逐条与我们的卡池按「卡名 slug」对照：

| | 数 |
|---|---|
| 我们卡池的卡能在它那里找到同名别名 | **1060 / 1126** |
| 我们卡池有、它没有同名 | **66** |
| 它有、我们卡池没有同名 | **44** |

**那 66 条「对不上」的，是「**同一张卡的两种写法**」** —— ⚠️ **但不要以为它对、我们错**：
它的别名是**照着 PnP 卡图做的 slug**，而 **PnP 是印刷品**（印的是当年重印的写法）。
🔴 **2026-09-24 当天实测已推翻「我们错」的结论** —— 拿**我们自己的译名表** `zh_CN.csv` 精确查键，
**我那 19 个「被改掉」的旧名在表里全是精确的键**（连括注都在）。**详见 §七。**
⇒ 这里的正确读法是：**它的写法 ≠ 原版**，**我们的写法 = 原版**（因为表里就是那么写的）。

### 🔴 它比我们「新」：有 44 个我们没有的卡/阵营

其中一整族 **`wf_foundation_dreadmind_*`**（`dreadleaper` / `infestation_node` / `vanguard_command`）**我们没有**。
⇒ **它上线过新内容，我们的卡池停在旧版本**。

### 它的接口（实测）

- `/factions` → 400（要 `buildId` 或 `ruleset`+`build`）· `/builds` → **401 要鉴权** ⇒ **接口是活的**
- `/cards` · `/cards/text` · `/cards/{id}` → **都要 `Authorization: Bearer <token>`**（= 要登录）
- 🔴 **没有任何「给我规则/引擎」的接口** —— 客户端只收 `match:state` / `match:events`
  ⇒ **引擎与规则拿不到**（它是服务端内部逻辑）

---

## 七、用它对照我们的卡名（2026-09-24）—— 🔴 **结论反转：该改的是它，不是我们**

### 一、我一开始搞错了（**当天已全部回滚**）

我拿「PnP 卡图 + 我们 2026-09-13 的逐张抄录表 + 它的别名」当证据，**改了 19 条卡名**。**全错。**
**根因**：那两个来源**都是从 PnP 派生的、不独立**；而 **PnP 是印刷品** —— 它印的是
「**当年重印的写法**」，**不等于原版数字版的卡名**（这正是铁律 10 第 3 条，也正是
`gen_cards_engine.py` 里 **2026-09-16 对 `Veteran Flyboy` 的结案**已经写过的）。

### 🔴 正确判据：`Unity/数据/本地化/i18n/zh_CN.csv`（**我们自己的译名表** —— 原版客户端没有中文表）

**精确查第一列的键**，结果一边倒 —— **被我改掉的 19 个旧名，在表里全是精确的键**：

| 我们卡池（= 原版键） | 表里的译文 | PnP 卡图印的（≠ 原版） |
|---|---|---|
| `Dok’s Toolz (Painboss' talent)` | 医官的工具（痛医头目天赋） | `Dok's Toolz` |
| `Commissar Elan` | 政委·埃兰 | `Commissar Denkler` |
| `Medic Scion` | 医护圣选兵 | `Scion Medic` |
| `Sisters Repentia` | 忏悔修女 | `Sister Repentia` |
| `Sylar Hexcorn` | 赛拉尔·赫克科恩 | `Sylar Hexscorn` |
| `Abaddons Chosen` | 阿巴顿神选 | `Abaddon's Chosen` |
| `Extermination Protocols` | 歼灭协议 | `Extermination Protocol` |
| `Acolyte Iconward` | 侍僧持旗者 | `Iconward Malak Vorenth` |
| `Veteran Flyboy` | 飞行老兵 | `Veteran Stormboy` |

⇒ **我们的卡名本来就是对的**；**`(某某的 Talent)` 那个括注是原版键的一部分**，不是我们自己加的。
⇒ 而 `Scion Medic` / `Extermination Protocol` / `Reconstitution Protocol`（PnP 那一侧的写法）**在表里根本不存在**。

**⛔ 由此得到的规矩**：**改卡名之前，先拿 `zh_CN.csv` 精确查键（第一列、整串相等）。**
只查 PnP / 第三方清单 = **一定会改错**。（已写进 `gen_cards_engine.py` 与 `资料/已知的坑.md`）

### 顺带查实的（这些有价值，保留）

- 它的 `wf_foundation_*` 别名 **正好 1126 个 = 我们卡池的 1126 张**（数量相等）。
- 它**没有序号**（纯 slug：`wf_foundation_<阵营>_<卡名>`），所以**给不出原版 id**；`UM46`/`GOF65` 全文 0 命中。
- 它有 **44 个我们没有的**（含整整一族 `dreadmind_*`）⇒ **它上线过新内容，我们的卡池停在旧版本**。
- 接口实测：`/factions` 400（要 build scope）· `/builds` **401** · `/cards` `/cards/text` **要 `Authorization: Bearer`**
  ⇒ 🔴 **没有任何「给我规则/引擎」的接口**（客户端只收 `match:state` / `match:events`）。
