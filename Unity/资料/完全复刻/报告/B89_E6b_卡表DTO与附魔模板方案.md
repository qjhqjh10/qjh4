# B89 · E6b —— 卡表 DTO 接线 + `HasAbilityDamage` 订正 + 附魔模板方案

> 执行代理 `E6b`（接 `E6` 的续派）· 2026-10-28
> 白名单 = `RuleEngine/Data/CardDatabase.cs` · `RuleEngine/Core/CardDef.cs` · `工具/gen_cards_engine.py` ·
> （新建）`RuleEngine/Editor/EnchantmentTemplateCheck.cs`
> 前情 = `资料/完全复刻/报告/B89_E6_卡表列与生成器.md`（生成器 v37 六列 / `CardDef` 新块 / 四条账状态 B 已交）

---

## §1 逐条

### ① `CardDatabase.cs`：DTO 两格 + 两个 `Attach*`（把 v37 两列真正接进引擎）

| 改动 | 现读行号 | 说明 |
|---|---|---|
| `public static partial class CardDatabase` | `:15` | **原地改词**（加 `partial`），零行数变化；同一条注释说明为什么新字段写在文件末 |
| `public partial class CardDto` | `:33` | 同上（`CardDto` 是 `CardDatabase` 的嵌套类型 ⇒ 两层都得 `partial`） |
| `def.AttachWhileInPlayAnim(...)` / `def.AttachPermanentBuffs(...)` | `:1749`（**与 `AttachAbilityDamage` 同一行**） | 版本闸 `dto.version >= 37 ? … : -1 / null`；`null` = 未知（⛔ 不要 `?? new[0]`） |
| `public int whileInPlayAnim = -1;` | `:3472` | 新 DTO 字段（缺键初值 `-1`，⛔ 不是 0） |
| `public PermanentBuffRowDto[] permanentBuffs;` | `:3476` | 新 DTO 字段（`null` = 未知） |
| `public partial class CardDto { … }`（文件末那一段） | `:3467`-`:3477` | **为什么写在文件末**：`CardDatabase.cs` 有 9 处账落点按行号引用（`74/1040/1610/1611/1690/1799/2303/2449/3291`），其中 `:1610`(×5) `:1611` `:1799` `:2449` 是 **A 行** ⇒ 在 `CardDto`（`:33`-`:651`）体内插行会让它们**整体漂移**。标 `partial` 后把新成员追加到**全部落点之后** = **零漂移**（`git diff` 只有 3 个单行 hunk + 1 个末尾追加 hunk） |
| `public class PermanentBuffRowDto` | `:3483` | 行 DTO（`at` / `ab` / `lg` / `buffType` / `token`，字段名 = JSON 键名逐字一致） |
| `static CardDef.PermanentBuffSpec[] MapPermanentBuffs(CardDto c)` | `:3508` | 形状校验：`at` 白名单外 ⇒ 出声丢行；`ab`/`lg` 缺或 <0 ⇒ 出声丢行；`(at,ab,lg)` 撞键 ⇒ 出声丢后一条；**`null` 入 `null` 出**（未知 ≠ 空表） |

**没有用可选参数**（调度台裁定 2）：`Attach*` 的形参是必填的，版本闸写在**调用点**，避免既有调用二义（`CS0121`）。
**`JsonUtility` 兼容性**：它按 `Type.GetFields()` 反射所有 public 字段，**不分区**（`CardDto` 的两个 `partial` 段等价）⇒ 两个字段放在文件末与放在体内**行为一致**。

### ② `AttachAbilityDamage` 把 `null` 折成空数组 —— 已修 + 断言钉死

- **原状**（v36 遗留）：`CardDef.cs:6824` `_abilityDamage = rows ?? new AbilityDamageSpec[0];`
  ⇒ 传 `null`（= 旧表 / 手写夹具，**整列未知**）之后 `_abilityDamage` 是**空数组** ⇒
  `HasAbilityDamage`（`:6817` `_abilityDamage != null`）**恒真**，与它自己那句 doc「`false` ⇒ 整列未知」正好相反。
- **改后**：`_abilityDamage = rows;`（**同一行原地替换，零行数变化**）。
  ⇒ `null` 保留 = 未知；`[]` 才是「明确没有行」；`AbilityDamageAt` 两者都回 `null`（**不变**）。
- 🔴 **行为影响 = 0，证据**：`HasAbilityDamage` 全仓 **0 个消费者**（`grep -rn "HasAbilityDamage" MyGame/Assets` 只命中
  `CardDef.cs:6817` 定义与 `:9035` 我自己那段 doc；唯一的 `abilityDamage` 读取口是
  `EffectResolver.cs:17829` 的 `AbilityDamageAt`，它空数组与 `null` 一样回 `null`）。
  ⇒ **调度台说的「读者在 `DoDeal` 的 90 档」那一位读的是 `AbilityDamageAt`，不是 `HasAbilityDamage`**，
  所以本条**不会改任何卡的结算**（这一半是反证，⛔ 别按「会改行为」去做回归）。
- **受影响卡（若有消费者的话）**：现读卡池 **1,129 张全部带 `abilityDamage` 列**（0 张缺列，119 张是 `[]`）
  ⇒ 真表里没有一张会翻面；**只有手写夹具 / 旧表**会从「有」变成「未知」。将来若真有消费者，翻面的集合 = 旧表全量 + 夹具。
- **断言**（新建 `MyGame/Assets/RuleEngine/Editor/EnchantmentTemplateCheck.cs`，独立 `-executeMethod`）：
  · `§A1` 新建 `CardDef` ⇒ `HasAbilityDamage == false`（🧨 改回 `?? new[0]` ⇒ 红）
  · `§A2` 挂 `[]` ⇒ `true` 但仍查不到行（🧨 把空数组也置 null ⇒ 红）
  · `§A3` 挂一行 ⇒ 查得到
  · `§A4` 灭自证：**真卡池每一张都必须带这一列**（现读 1129/1129；🧨 生成器漏列 ⇒ 红）

### ③ 附魔模板入池 —— **只出方案，未实现**（见 §5）

---

## §2 typecheck + 断言

**`TMPDIR=/tmp/wf_B89E6b bash d:/4/Unity/工具/typecheck.sh`**（本轮多次跑）：

| 时间点 | 运行时 | 编辑器 | 我的文件 |
|---|---|---|---|
| ① `CardDatabase` + ② `CardDef` 改完（新 editor 文件还没写） | **0** | **0** | 0 错 |
| 加入 `EnchantmentTemplateCheck.cs` 之后 | 1 | 44 | **0 错**（`grep EnchantmentTemplateCheck\|CardDatabase\|CardDef.cs` = 无命中） |

🔴 **新增的 1 + 44 个错全部落在白名单之外 ⇒ 是别人的活**（简报红线：报错全在别处就不算我的）：
- 运行时 **1**：`RuleEngine/Core/BattleContext.cs(2350,21) CS0708`（`MatchTypes.ResolvingExtrinsic` 不能在静态类里做实例成员）
- 编辑器 **44**：`CardPresentation/Editor/GameStaticDataCheck.cs`（`TipPrebattle.*`）· `PlayerDataMathCheck.cs`（`PlayerDataMath.*`）· `GameModesCheck.cs`（`GameModesHandler.*`）
- 现象：**同一命令连跑两次报错集合都不一样**（一次 GameModesCheck 8 条 + …，另一次 GameStaticDataCheck 12 条）⇒ 那几个文件**正被人写到一半**。
- ⚠️ 这 44 条会把「编辑器程序集」整体判红，**不等于**我的三个文件有问题；我的三个文件在「0/0 那一趟」里已经通过过一次（那趟已含 ①② 的全部改动）。**同步点请再跑一次 typecheck**，若那时只剩我的文件仍 0 错即可确认。

**断言（新增文件，需调度台加一条 `-executeMethod`）**：
```
-executeMethod EnchantmentTemplateCheck.Run
```
共 **§A 6 条 · §B 6 条 · §C 5 条 · §D 9 条 = 26 条**（含 4 条「🧨 改哪一行会红」的**灭自证**断言与 1 条真表全池下限）。
⚠️ 本代理按红线**没有跑 Unity**，所以这 26 条**一条都还没执行过** —— 请在同步点跑，红了先看 `🧨` 那半句指向哪一行。

---

## §3 顺手发现（⛔ 没自己改）

1. **`HasAbilityDamage` 是「零消费者」的死属性** —— 修它纯粹是**契约正确性**（`null` = 未知），
   不影响任何结算。⛔ 别为了它去跑全量自检。
2. **`MapAbilityDamage` 与 `MapPermanentBuffs` 的契约已经对齐**（都 `null` 进 `null` 出），
   但 `AttachActiveAbilityCost`（`CardDefActiveAbility.cs:100`）用的是另一套口径：它没有「整列未知」这个概念，
   只有四格各自的 `-1` + 一个汇总 `ActiveAbilityCostKnown`（`||` 起来）。**两套并存不是错**，
   但如果将来有人想统一，先看 `ActiveAbilityCostKnown` 的 3 个消费者（`RuleCore.cs:15321/15408/15573`）。
3. **`CardDatabase.cs` 的行号密度**：9 处账落点里有 5 处是 A 行集中在 `:1610-1799` 这一小段
   ⇒ 以后往这个文件加任何东西都**先看落点表**，否则一动就漂（我这轮靠 `partial` 绕开了）。
4. `permanentBuffs` 的 `token` 字段现读只有 1 个值（`0474b16360827b6408fb02b6820e7e63`）；
   `at == "enchantment"` 的条目**永远是成对出现**（`GSC51` 两条 logic 挂同一张模板）—— 去重口径**没定**（今天 0 冲突，见 §4-3）。

---

## §4 没查清

1. **真表联调没跑**（红线）：`§D` 那 8 条要证明 `CardDatabase.Parse` 里那行三连 `Attach*` 真被调到。
   静态看是对的（`dto.version >= 37` + `c.whileInPlayAnim` / `MapPermanentBuffs(c)` 都在作用域内、typecheck 通过），
   但**没有运行时证据**。
2. **`MatchTypes.ResolvingExtrinsic`（`BattleContext.cs:2350`）那 1 条运行时红是谁的**：没查（不在白名单）。
   它会让 `ruleprobe` 也编不过 ⇒ **同步点前必须由调度台找该文件的所有者确认**。
3. **`permanentBuffs` 的 `(at,ab,lg)` 撞键口径**：`MapPermanentBuffs` 里「后一条丢弃 + 出声」是我按
   `MapAbilityDamage` 同族写法定的，今天 0 例；如果将来「同一 logic 行里 `buffEffect` 与 `handBuff` 都永久」
   出现，是否该**合并成一条**（原版扫的是一个统一 `activeEffects` 列表）还没定 —— 那要 `EffectRecord` 那一层先建模。

---

## §5 附魔模板方案（⛔ 未实现；下一批做）

### 5·1 问题（为什么非要第二张表）

`HasEnchantmentWhileInPlay`（账 **L1044**）读的是**附魔卡自己**的 `whileInPlayAnim`，而真值里 `= 1` 的那 3 张
全是 `m_Name` 尾 `_enchantment` 的**模板**（`Undying Legions` / `Hive Fleet Leviathan` / `Everstorm`），
它们**一张都不在 `cards_engine.json` 的 `cards[]` 里**。原因 = v30 入池闸 `cardType == 20` **∧** 名字含 `enchant`
（`gen_cards_engine.py` 的 `build_enchantment_entries` §③），这三个名字里没有 `enchant`。

🔴 **调度台 2026-10-28 裁定：⛔ 不许放宽那个入池闸**（会改 `count` ⇒ 影响可玩池与探针基线；
**附魔模板不是可玩卡**）。⇒ 正确做法 = **第二张小表**，只喂「按 id 查模板」这一条读路。

### 5·2 表结构与字段清单

- 文件：`MyGame/Assets/RuleEngine/Resources/enchantment_templates.json`
- 顶层：`{version: 1, source, note, count, templates: [...]}`（与 `cards_engine.json` 同风格；`version` **独立计数**，
  与卡表的 v37 无关 —— 两张表各自 bump）
- **不进** `cards_engine.json` 的 `cards[]`，**不改** `count`（现读 1,129）

| 字段 | 原版真值来源（`cards_raw.json` 的 `full_raw`） | 用途 |
|---|---|---|
| `id` | `uniqueId` | **唯一 join 键**：`buffEffect.enchantment.m_PathID` → `path_id → uniqueId` 表 → 它 |
| `name` / `nameZh` | `cardName` / 官方本地化 | 日志、tooltip |
| `mName` | `m_Name` | 排查（尾 `_enchantment` 的判据） |
| `cardType` / `spellType` | 同名 | 附魔族判别（`spellType 60/120` = 密令/任务） |
| `manaCost` | 同名 | 附魔费用（展示；**与 `ActiveAbility` 那一套无关**） |
| `psychicCost` / `plasmaCost` / `targeted` | 同名 | 若将来附魔也走主动能力那一跳 |
| `whileInPlayAnim` | 同名（**int 0/1**） | **账 L1044 就要这一格** |
| `cardTraits` | `CardTraits` | 附魔卡自带 trait |
| `permanentBuffs` | 本卡自己的 `buffEffect/handBuff.permanentBuff`（**稀疏**，口径同 v37 主表） | 账 L847 的同族数据（模板层） |
| `abilities` | `cardAbilities[].{trigger, abilityLogic[]}`（`chosenAbility` / `buffEffect` / `handBuff` / `manaBuff` / `summon` …） | 效果正文；**与主表用同一批 helper 落**，否则消费端要另写一份解析（两处写同一条规则 = 迟早不一致） |
| `owners` | 反向索引：谁挂它 = `[{id, buffType, trigger}, …]`（来自 `buffEffect{buffType 12/15/16}.enchantment` PPtr） | 判「今天有没有池内卡能挂上」；也是「要不要保留这一行」的判据 |

### 5·3 预计张数（现读）

| 口径 | 张数 | 备注 |
|---|---|---|
| `m_Name` 尾 `_enchantment`（真值 1,351 张里） | **85** | `cardType` 20:83 · 0:2 |
| 其中有**任何**卡挂载（`owners` 非空） | **79** | —— |
| 其中**池内卡**能挂上（真正可达） | **41** | 建议至少落这 41 张 |
| 可达集合里 `whileInPlayAnim == 1` | **2**（`Undying Legions` / `Hive Fleet Leviathan`） | 第 3 张 `Everstorm` 的宿主 `SW64` 自己不在池 ⇒ 落全量 85 才覆盖得到 3/3 |
| 可达集合的 `manaCost` 分布 | 4:13 · 2:11 · 3:7 · 5:4 · 7:3 · 6:2 · 1:1 | 全非 0 ⇒ 附魔本身是有费用的 |
| 可达集合的 `spellType` | 0:40 · 240:1 | **密令 60 / 任务 120 一张都没有**（与卡表的 `HasSecretOrders`/`HasQuests` 那两条「判不了」同源） |
| 可达集合的 `abilityLogic` 行数 | **49** | 均 1.2 行/张 |

**建议落全量 85 张**（多 44 行、体积可忽略）：可达性是**会变**的（挂载链/池随版本变），
而「哪 3 张 `whileInPlayAnim = 1`」这种真值本来就该整表在。

### 5·4 谁能消费 / 怎么接（⚠️ 有一个架构约束）

- **消费点**：`Core/Enchantment.cs` 的 `ctx.CardPool` 查找（`:718` / `:1119`）—— 那是 **E4 的文件**，
  加一个 **fallback**：先查 `ctx.CardPool`（可玩池），查不到再查模板表。**找到之后 `Attach` 的缺省**改成
  `enchantmentCard.WhileInPlayAnim`（`Enchantment.cs:160` 那条 P-07 待办）。
- 🔴 **架构约束**：`Core/` **不能引用 `Data/CardDatabase`**（`Enchantment.cs:703` 那条注释：`ruleprobe` 只编 `Core/*.cs`）
  ⇒ 模板表必须由**外层**把 `Dictionary<string, CardDef>` 注入 `BattleContext`（建议新加一格
  `ctx.EnchantmentTemplates`，与 `ctx.CardPool` 同一注入方式；`BattleDriver` 与 `ruleprobe` 夹具各喂一份）。
- `CardDef` **不需要新字段**（`WhileInPlayAnim` / `PermanentBuffs` 已在 v37 块里）⇒ 表行可以直接造成 `CardDef`。

### 5·5 落地步骤（下一批，5 步）

1. `工具/gen_cards_engine.py` 加 `build_enchantment_templates()`（复用 `_permanent_rows_of` / 现有的 ability helper）
   + 第二份产物写出（⚠️ 该脚本默认**不写盘**，必须显式 `--write`；两份产物同一次跑一起写）。
2. `CardDatabase` 加 `LoadEnchantmentTemplates()` → `Dictionary<string, CardDef>`（**不进** `Load()` 的池）。
3. `BattleContext` 加 `EnchantmentTemplates` 一格（**不在本批白名单**：`BattleContext.cs` 有 1 条别人的在飞错误）。
4. `Enchantment.cs` 两处查找加 fallback + `Attach` 缺省读 `WhileInPlayAnim`（E4）。
5. `EnchantmentTemplateCheck` 加 `§E`：模板表非空 / 那 3 张 `whileInPlayAnim = 1` 都在 / `count` 与 `cards_engine.json`
   的 `count` **分别**报（钉住「两张表互不串」）。

---

## §6 本批产物

- 改：`MyGame/Assets/RuleEngine/Data/CardDatabase.cs`（+94 / −3 · 3 个单行 hunk + 末尾追加 91 行 · **零漂移**）
- 改：`MyGame/Assets/RuleEngine/Core/CardDef.cs`（**同一行原地替换** 1 行 · 零行数变化）
- 新：`MyGame/Assets/RuleEngine/Editor/EnchantmentTemplateCheck.cs`（26 条断言 · 需 `-executeMethod EnchantmentTemplateCheck.Run`）
  ⚠️ ⛔ **不含 `.meta`** —— Unity 首次打开工程会自己生成；若调度台要求入库，请让 Unity 生成后再提交。
- 账补丁：`资料/完全复刻/账/_parts/B89_E6b.tsv`（表头 + **2 行** · `轮次 = B89-E6b`）
  · 这 2 行（`RemoveBuffHandCostEffects` / `HasEnchantmentWhileInPlay`）与上一批 `B89_E6.tsv` **同键**，
    字典序 `B89_E6` < `B89_E6b` ⇒ **E6b 赢** ⇒ 本补丁的行是**在 E6 那一版之上追加**的（合并后 E6 的文本不会丢）。
  · 🔴 **`CardScript.GetAdjustedDamage`（主表第 748 行）故意没写**：`B89_R1b.tsv:14` 也写了同一行且字典序更靠后（R1b 赢），
    写进去只会被静默冲掉、还留下「已记录」的假象 ⇒ 该缺陷（`HasAbilityDamage` 折 null）的记载在本文 §1②，请调度台合并 R1b 后按需补一句。
- 未改：`gen_cards_engine.py`（本批**没有再动**；v37 六列是 `E6` 落的）
