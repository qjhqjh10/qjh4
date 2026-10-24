# D11 · 卡表新增 `activeAbilityLogic` 列（原版主动能力整条链的数据侧）

> 卡表数据代理 `D11` · 2026-10-24 · 判据来源 = 战斗审计 `D10`（`资料/普查产出_1024/D10_abilityTargets管线.md`）
> ＋ 全量反编译 `d:/2/tools/decomp_full/`（逐行复核过）
> **口径：只落数据、⛔ 不接引擎消费**（本件 0 行 `.cs` 改动）。

---

## 〇 · 一句话结论

`cards_engine.json` 新增一列 **`activeAbilityLogic`**（**每张卡都落**，没行落 `[]`），
逐张落原版 `activeAbility` / `alternativeAbility` 两个能力对象里的 `activeAbilityLogic[]` 行：
**`ta`（`targetsAffected`）· `fm`（`furtherMultiplier`）· `needsLoading` · `trigger=400` · `gate`**。
`hasActiveAbility == 0` 的 **45 张 / 52 行**照落、带 **`gate: 0`**（理由见 §二）。
`version` **22 → 23**。生成器 `--check` rc=0、连跑两次 sha 相同。
⚠️ **本列零行为变化** —— 消费端（`CardDatabase` / `CardDef` / `EffectText` / `EffectResolver`）**另批做**，
下游清单见 §五。

---

## 一 · 列名与形状（🔴 写死，下游按这个接）

**列名：`activeAbilityLogic`**（逐卡字段，与同名原版 SO 字段同名 —— 便于反查；
⚠️ 注意：它是**我们这一列**，形状与 SO 里的裸 list 不同，见下）。

**形状**（`JsonUtility` 不支持字典，与 `abilityTargets` / `abilityMultipliers` 同一口径）：

```json
"activeAbilityLogic": [
  {"ab": -1, "gate": 1, "trigger": 400, "needsLoading": 0, "ta": [30], "fm": [0]}
]
```

| 键 | 语义 | 取值 / 缺值契约 |
|---|---|---|
| `ab` | 这一项来自哪个能力对象 | **`-1` = `activeAbility`** · **`-2` = `alternativeAbility`**（**与 `unitConditions` 的 `ab` 同词**，⛔ 别另造一套） |
| `gate` | 该分支**静态**可达性 | `1` 可达 · `0` **本 build 死数据** · `-1` 闸本身未知（⛔ 不拿 0 顶「已知不消费」）<br>定义：active → `hasActiveAbility`；alternative → `hasActiveAbility ∧ hasAlternativeAbility` |
| `trigger` | 原版逐行传的**虚拟时机** | **恒 `400`**（`ThisUnitAbilityUsed`，`RawCardScript__ResolveActiveAbility.c:65`）。⚠️ **行数据里没有 trigger 字段**，⛔ 别去 SO 里找 |
| `needsLoading` | 该能力对象的 `needsLoading`（`ActiveAbility.+0x22`） | int；缺键/坏值 `-1`（未知） |
| `ta` | **逐行** `activeAbilityLogic[j].targetCriteria.targetsAffected` | `ta[j]`；非整数坏值 **`-1`**（⛔ 不猜 `0` = `any`） |
| `fm` | **逐行** `activeAbilityLogic[j].furtherMultiplier` | `fm[j]`；缺键/非整数/负数 **`-1`**（⛔ 不猜 `0` = 不乘）；`0` = 原版明确不乘 |

**几条硬契约（与同胞列逐字对齐）：**

- `ta` / `fm` 与 **`abilityTargets` / `abilityMultipliers` 的 `ta` / `fm` 完全同契约**（只换了数据源）——
  下游可以复用同一套「坏值 = -1」的解释。
- **一个能力对象一行都没有 ⇒ 不落那一项**（数组里不出现 `ta: []` 的空对象）。
- 🔴 **每张卡都落**（没有 active/alt 行的 **995 张**落 `[]`）——「缺列」与「空数组」必须分得开（同 `abilityTargets`）。
  只有「真值快照里没有这张卡」才不落列（**现读 0 张**）。
- `ab` 顺序固定：先 `-1`（active），后 `-2`（alternative） —— 池内只有 `SW21 Wulfen Pack Leader` 两项都有。

### 1·1 判据（反编译逐行，第一权威）

| 环节 | 出处 | 说明 |
|---|---|---|
| 选 active / alternative | `RawCardScript__ResolveActiveAbility.c:18` `if (hasAlternativeAbility(+0x2AA) == 0)` ⇒ `:21` 取 `activeAbility(+0x260)` | 没有 alternative 的卡**只走 active** |
| 同上（有 alternative 时） | `:26-31` `BoardAnalysis__MeetsCriteria(+0x2B0 …)` —— **真**走 active、**假**走 `alternativeAbility(+0x2B8)` | 🔴 **方向别搞反**；这条是**运行期**闸，静态判不了 ⇒ 不进 `gate` |
| 取行列表 | `:50` `lVar6 = *(lVar6 + 0x48)` = 该 Ability 对象的 `activeAbilityLogic`（List） | |
| 逐行施放 | `:58-67` `AbilityLogic__PlayAbility(lVar4, bm, uVar1, 400, 0)` | **trigger 400 = `ThisUnitAbilityUsed`** |
| `ta` 的来历 | `AbilityLogic__PlayAbility.c:262` `GetTargets(bm, *(param_1 + 0x28) /* targetCriteria */, …)` | `targetCriteria` 挂在**行上** ⇒ `ta` 逐行 |
| `fm` 的来历 | `:235-236` `if (0 < *(int *)(param_1 + 0x38)) local_178 *= furtherMultiplier` | `0` = 明确不乘 |
| **死数据的判据** | `CardScript__ResolveActiveAbilityPlayed.c:22` `if (hasActiveAbility(+0x220) == 0) { LogWarning; return; }` | 整条路径**被它短路** ⇒ `hasActiveAbility = 0` 的行本 build 一条都不消费 |
| 🔴 踩坑 | `alternativeAbility` 里的列表**键名也叫 `activeAbilityLogic`** | 不是 `alternativeAbilityLogic`（全库 `grep -o` = 0）；按后者取会**静默得 0 行** |

### 1·2 生成器落点

| 位置（`Unity/工具/gen_cards_engine.py`） | 内容 |
|---|---|
| `:1632-1664` | 长注释：判据 / 形状 / `gate` 三值 / 为什么死数据照落 |
| `:1665-1668` | `ACTIVE_ABILITY_LOGIC_REPORT` |
| `:1671-1755` | `active_ability_logic_of()` |
| `:3826-3830` | 逐卡落列 `entry["activeAbilityLogic"]` |
| `:4302` | `"version": 23` |
| `:4439-4451` | v23 的版本注释（`CardDatabase.cs` 的 version 守卫靠它出声） |
| `:5452-5463` | `main()` 的读数摘要 |

---

## 二 · `hasActiveAbility = 0` 的处置与理由

**结论：照落，带 `gate: 0`**（⛔ 不丢、也不静默）。

- 数量（现读，与 D10 §2·1 逐项吻合）：**45 张卡 / 52 行**（池内）。
  池内 `hasActiveAbility = 1` 的 **87 张** + `SW21` 的 alternative 那一项 = `gate: 1` 的 **88 个能力对象**。
- 为什么**照落**（三条，按分量排）：
  1. 🔴 **本仓的硬契约是「缺席 = 未知」（`-1`）**。丢掉这 45 张 = 把「**已知本 build 不消费**」读成了
     「**我们没收到这份数据**」—— 那正是红线里最贵的那类静默错（与 `abilityTargets` 的「缺列 ≠ 空数组」同源）。
  2. `hasActiveAbility` 本身是**数据驱动**的（`RawCardScript +0x2A9` 灌进 `EntityScript +0x220`），
     换 build / 换卡数据时它会翻 —— 届时行数据还得在；现在丢掉就是提前把信息销毁。
  3. 成本极低（52 行，池内 +36 KB 里的一小截），且**消费端用 `gate` 一行就能筛掉**。
- 为什么**不能**改成「不落 + 出声」：出声只对**跑生成器的这一次**可见，
  而 `cards_engine.json` 是**跨会话的载体** —— 按铁律 5·b「下个会话需不需要知道」这条信息必须留在数据里。
- ⚠️ **`gate` 的边界**（别当成万能判定）：
  · `gate` 只回答「**静态**可达」。alternative 还有一条**运行期**闸 `MeetsCriteria == false`（§1·1 第二行），
    静态判不了 ⇒ **不在 `gate` 里**；下游要判 alternative 得走 `CardDef.UsesAlternativeAbility(...)`
    （`Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs:206`，v21 已就位）。
  · `gate = -1`（闸未知）今天**0 张**（1,351 张真值里 `hasActiveAbility` 全有、全是 0/1）。

---

## 三 · diff 汇总

### 3·1 文件级

| 文件 | 改动 | 判据 |
|---|---|---|
| `Unity/工具/gen_cards_engine.py` | **+219 / −7 行**（`git diff --numstat`） | 新增 1 段长注释 + 1 个 report + 1 个函数 + 1 处落列 + 1 段摘要 + v23 注释 + `version` 22→23 |
| `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 单行文件，整行重写（`+1 / −1`） | 1,416,841 B → **1,453,225 B**（+36,384 B · +2.6%） |
| 行尾 | `gen_cards_engine.py` **纯 CRLF 未翻**（5,582 CRLF / 5,582 LF，按二进制数） | ⛔ 没踩「`sed -i` / 文本模式翻 LF」那三个坑 |
| `cards_engine.json` 末字节 | **不是换行**（与历次一致，单行文件） | 由 `json.dump(..., separators=(",", ":"))` 生成 |

### 3·2 表级

| 项 | 前 | 后 |
|---|---|---|
| `version` | 22 | **23** |
| `count` | 1127 | 1127（不变） |
| 顶层键 | 6 个 | 6 个（**没增没减** —— 新列是**逐卡**字段，不是顶层键） |
| 卡 id 集合 | — | **完全相同**（1127 张，0 增 0 减） |
| 逐卡变化的字段 | — | **只有 `activeAbilityLogic`**：1127 张全部新增该键（995 张 `[]` + 132 张非空） |
| 其它字段的任何值变化 | — | **0 处**（逐卡逐字段程序化比对，非 `activeAbilityLogic` 的差异列表 = 空） |

### 3·3 新列读数（与 D10 §2·1 逐项对账，**全部吻合**）

| 读数 | 本列现读 | D10 记的 |
|---|---|---|
| 有 active/alt 行的卡（池内） | **132 张 / 133 个能力对象 / 193 行** | 132 张 / 192 行（active）＋ `SW21` 1 行（alternative）⇒ 193 |
| 其中 active 行 / alternative 行 | **192 / 1** | 192 / 1 ✅ |
| `gate = 0`（死数据） | **45 张 / 52 行** | 45 张 / 52 行 ✅ |
| `gate = 1` | **88 个能力对象**（87 active + `SW21` 的 alternative） | `hasActiveAbility=1` 87 张 ✅ |
| `fm > 0` 的行 | **109 行**（`fm=1`:107 · `fm=2`:2） | 72 张 / 109 行 ✅ |
| `needsLoading != 0` | **0 项**（133 个对象全 `0`） | 真值全 0 ✅ |
| `alternativeAbility` 有行 | **1 张**（`SW21`，`ta=[10]`） | 1 张 ✅ |
| `ta` 分布（193 行） | `30:69 · 20:51 · 10:30 · 0:17 · 90:7 · 65:6 · 150:3 · 110:3 · 50:2 · 60:2 · 51:1 · 180:1 · 120:1` | D10 的 192 行 = 同表但 `10:29`；**+1 就是 `SW21` 的 alternative `ta=10`** ✅ |
| 卡型分布（132 张） | unit 81 · hero 51 | — |
| 阵营分布 | Sororitas 28 · Ultramarines 25 · AstraMilitarum 24 · DarkAngels 13 · SpaceWolves 11 · Goff 6 · SaimHann 5 · BlackLegion 4 · Genestealers 4 · Sautekh 4 · TauEmpire 4 · Leviathan 4 | — |

**逐卡明细**（132 行，含每张的 `gate` / `needsLoading` / `ta` / `fm`）：
→ **`资料/普查产出_1024/D11_activeAbilityLogic_逐卡.tsv`**（同目录）。

抽样核对（与 D10 §2·2 的表逐格比过）：

| 卡 | 本列 | D10 表 |
|---|---|---|
| `AM38 Baneblade Tank` | `active(gate=1, ta=[30], fm=[0])` | ta 30 ✅ |
| `SW21 Wulfen Pack Leader` | `active(gate=1, ta=[10], fm=[0])` ＋ **`alternative(gate=1, ta=[10], fm=[0])`** | active 10 · alternative 10 ✅ |
| `SOR13 Simulacrum Bearer` | `active(gate=1, ta=[10,120,20], fm=[1,1,1])` | 10,120,20 ✅ |
| `SAU37 Orikan the Diviner` | `active(gate=0, ta=[10], fm=[1])` | `hasActive=0`, ta 10 ✅ |
| `SOR70 Sororitas Rhino` | `active(gate=0, ta=[30,20], fm=[1,1])` | `hasActive=0`, 30,20 ✅ |

---

## 四 · 版本号

- **`version`: 22 → 23**，并在 `:4439` 补了 v23 的注释（说明加的是什么列、判据、以及「只落数据」）。
- **下游守卫核查（只读，未改 `.cs`）**：
  - `Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs` 的 version 守卫**全是「旧表出声」**：
    形如 `if (dto.version < N) Debug.LogWarning(...)`（另有两条 `>= 15 && < 16` / `>= 15 && < 22` 只是
    **缩小出声范围**，`P1` 修的），**没有任何一条拦下 / 拒绝更高的版本**（没有 `throw` / `return` / 跳过装载）
    ⇒ **升到 23 不会被拒**。
  - 也**没有**任何 `version == N` 的精确比较（`grep -rn 'version ==' MyGame/Assets/RuleEngine/` = 0 命中）
    ⇒ 不会因为「没人认识 23」而走异常分支。
  - ⚠️ **缺口（留给下游）**：`CardDatabase.cs` **目前没有 `version < 23` 那条警告** —— 即「表是 v22、没有这一列」
    会被**静默**读成「全池空表」。下游加消费端时**必须同时补这条 warning**（照 `:982`（v22）那条的写法）。
- 池指纹（**没动常量**，只报数值）：`cards_engine.json` 的 md5
  **`ab8c5238a7077846a2dfab42278b2689` → `b6035055b993b33e9bcfa93bbce97d13`**
  （旧值与 `工具/ruleprobe/Program.cs:216` 现钉的 `PinnedPoolMd5` **逐字相同** ⇒ 这次变化**全部来自本件**）。
  ⇒ **请主对话重钉 `PinnedPoolMd5`**（`PinnedBaselineMd5` / `PinnedParseMd5` 按 D10 的归因口径**不动**：
  本件 **0 行 `.cs` 改动 ⇒ 解析输出逐字节不变**）。
  sha256 = `4acdd2e9a4d8632d6a1c71dcac83557ed9e7981999bac8d1e2fe4193a7a9432b`。

---

## 五 · 幂等验证（实跑）

| 检查 | 结果 |
|---|---|
| `python -I -X utf8 工具/gen_cards_engine.py --check`（第 1 次） | **rc = 0** · 末行「现有 … cards_engine.json 一致 ✅」 |
| 同上（第 2 次） | **rc = 0** · `cmp` 两次完整输出 ⇒ **逐字节相同** |
| `--write` 连跑两次 | 两次写完 **sha256 完全相同**（`4acdd2e9…`）⇒ 生成确定性 |
| 结构自检（脚本级） | 每项键集合恒为 `{ab,gate,trigger,needsLoading,ta,fm}` · `len(ta) == len(fm)` · `trigger` 恒 400 · 1127 张**全都有**该键 |
| `git status --porcelain` | 只有白名单内两个文件 + 本报告目录 |

---

## 六 · 下游要接什么（引擎消费点清单）

> 🔴 **今天这些行有 0 个运行期消费者**（D10 §3·1：`abilityTargets` 那套闸门只服务 `cardAbilities` 一族）。
> 本件**只落数据** —— 下列任何一条不做，行为都**不会**因为这列而变。

| # | 落点 | 要做什么 | 判据 / 备注 |
|---|---|---|---|
| **①** | `Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs` | `CardDto` 加 `public ActiveAbilityLogicDto[] activeAbilityLogic;` ＋ 对应 DTO 类（**字段名逐字 = JSON 键名**：`ab` / `gate` / `trigger` / `needsLoading` / `ta` / `fm`）；`Parse` 里映射进 `CardDef`；外加 `activeAbilityLogicVersioned: dto.version >= 23` | 照 `AbilityMultiplierDto`（`:456-459`）与 `MapAbilityMultipliers`（`:1482`）的写法抄 |
| **②** | 同上，`CardDatabase.cs:982` 一带 | **补 `if (dto.version < 23)` 的 `Debug.LogWarning`**（「旧表没有主动能力整条链，读到的是空表 = 未知，⛔ 不是全池没有主动能力」） | 照 v22 那条的写法；**没有这条 = 旧表被静默当成「全池空表」** |
| **③** | `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs` | 构造函数加 `IEnumerable<ActiveAbilityLogicSpec> activeAbilityLogic` ＋ `ActiveAbilityLogicVersioned` 标志（`null` ≠ `[]` 的判定要留着）；`HasActiveAbility`（`:146`）/ `UsesAlternativeAbility`（`:206`）已有，不重写 | 与 `AbilityTriggersVersioned`（`:2119`）/ `AbilityMultipliersVersioned`（`:968`）同形 |
| **④** | `EffectText.cs` 的「卡面能力块 ↔ ability」归位 | 🔴 **这是最硬的一环**：现在 `AssignAbilityTargets` 靠 **G1（整卡能力块数 == ability 数）** 对齐 `cardAbilities`（D10 §3·1）。**`Duty:` / `Pray:` / `Oath:` / `Ferocity:` / `Agenda:` 这些块在 `cardAbilities` 里没有对应项** ⇒ G1 天然失配（`AM38` 就是 2 块 vs 1 ability）⇒ 光有数据也落不到 op 上。**必须先有「块 → active 行」的归位规则** | D10 §6·1②。现池「active 行非空 ∧ `cardAbilities` 为空」的卡 **34 张** |
| **⑤** | `EffectResolver.cs` 的 `EffectOp.AbilityFurtherMultiplier` 那一格 | active 行的 `fm` 要能被乘进那条 op 的次数/倍数 —— 现状 `fm` 只服务 `cardAbilities` 一族（`EffectText.AssignAbilityTargets` 那两道闸） | `AbilityLogic__PlayAbility.c:235-236`；现池 `fm>0` 的 active 行 **109 行** |
| **⑥** | `CardDef.UsesAlternativeAbility(ctx, owner, actor)`（`:206`） | 消费端要按 `gate` ＋ 这条运行期闸决定用 `ab=-1` 还是 `ab=-2` 的那一组 `ta`/`fm`；今天只有 `SW21` 两组都有、**且两组 `ta` 都是 10** ⇒ 接不接当前无差，但**口径必须先定**，否则以后出现「两组 ta 不同」的卡就分不出 | `RawCardScript__ResolveActiveAbility.c:18/26-31` |
| **⑦** | 自检 | ①`RuleEngineTest` 加「v22 旧表 ⇒ 恰好一条 `< 23` 警告」的探针（照 `:35046` 那条 v14 探针的写法）；②一条「本列 132 张 / 193 行 / 45 张 `gate=0`」的数据断言 | ⚠️ 断言要盯**原版判据**，⛔ 别只盯我们自己的常量 |
| **⑧** | `工具/ruleprobe/Program.cs:216` | **主对话重钉 `PinnedPoolMd5` → `b6035055b993b33e9bcfa93bbce97d13`**（本代理不动常量） | §四 |

### 6·1 一条**已经不需要**做的（D10 当时记为待办，现已落地 —— 别重复做）

D10 §3·3 说「裸 `Deal N damage` 走 `Auto + PickMost("-health")`，与真值 `ta=30` 冲突」——
**这条已由引擎线 `G5`（2026-10-24）修掉**：`EffectText.cs:6392-6427` 现在把裸 `Deal N damage` 解成
「原版 `ta=30 target` ⇒ 由施放方选一个敌方单位」（`Auto` 为假、`Count = 1` ⇒ 玩家走 `PickTarget`、
AI 逐目标枚举打分），**不再自动挑生命最低**。判据全文 → `资料/普查产出_1024/G5_裸Deal与ta200.md`。
⇒ **⑤ 之外那条「真缺陷」不再是本列的下游必需项**（数据列仍然有价值：
正文没写目标、或正文写的与数据 `ta` 不一致时，只有本列有真值）。

---

## 七 · 没查清 / 没做

1. 🔴 **没接任何消费端**（本件明令不做）。所以：**`Duty: Deal 8 damage` 这类主动能力正文的分辨行为今天一点没变**
   —— 本列是「数据就位」，⛔ 别报成「已修」。真正让它生效至少要 §五 的 ①③④⑤ 四件一起。
2. **`gate` 不含运行期闸**：alternative 还需要 `BoardAnalysis.MeetsCriteria == false`（读的是**运行时棋盘**），
   静态算不出来 ⇒ 本列只给静态那半。`MeetsCriteria` 的参数 `normalAbilityBoardCriteria` 已有列（v21）。
3. **`activeAbility.*` 对象级的 `targetCriteria`（不是 per-row）仍没有任何一列收**：
   例如 `AM38.activeAbility.targetCriteria` = `ta=10(allFiltered)` ＋ `friendlyUnits/enemyUnits/includeHero/…`
   —— 那是「**这条主动技能能点哪些格子**」的过滤器（影响 UI 高亮/合法性），不是效果目标。
   D10 §6·2·4 已登记为顺手发现，**本件未做**（它属于另一列，别混进 `activeAbilityLogic`）。
4. **`loadingAbilityLogic` 未落**：1,351 张 SO 里 `m_PathID != 0` 的 **0 张** ⇒ 没有这份数据（D10 §1·2），
   ⛔ 不是漏收。同理 `normalAbility` / `otherAbilityAnims` 在本 build 的 SO 里**不存在**。
5. **没有逐张核「卡面能力块数」**：§五 ④ 的规模（有多少张因 G1 失配而让数据落不到 op 上）**没有总量**，
   那要跑 `EffectText.CountAbilityBlocks`（C#），本代理不跑 Unity。已知的只有：
   `abilityTargets == []` 的 34 张必然无效 + `AM38` 一张手推。
6. **`fm` 的语义没有逐卡解释**：本列只搬 `furtherMultiplier` 的**数值**；
   「`fm=2` 到底是召唤两份还是伤害翻倍」取决于同行的 `chosenAbility`（`AM73` / `AM16` 两张是 2），
   那要走 `PlayAbility` 的分支，**没查**。
7. **未进原版实机**：原版已关服、进不了对局；`ta=30 ⇒ 弹选目标` 的判据是**反编译**
   （`AbilityLogic__GetTargets.c:192-207`），不是运行期观测。
