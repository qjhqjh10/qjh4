# W · `dodge` 整条（`A1077`）（写手交件 · 2026-10-08/09）

> 范围：`dodge` 这个关键词在我们引擎里**整条都没有** —— 本次把「认得出」+「结算侧」两半补齐。
> 白名单只碰了三个文件：`RuleEngine/Core/CardDef.cs` · `RuleEngine/Core/RuleCore.cs` · `RuleEngine/Editor/RuleEngineTest.cs`。
> **没有跑 Unity 自检**（用户本轮明确要求「待办没做完之前不跑」）；只跑了**秒级类型检查**。

---

## ① 结论

### 1. 第一问：现池到底有几张 `Dodge` / `Shield`

| 词 | 现池 `keywords` 命中 | 全文（含 `desc`/`descZh`）命中 | 结论 |
|---|---|---|---|
| **`Dodge`** | **0 / 1126** | **0**（`cards_engine.json` 全文扫 `Dodge`、`dodge` 两个词根**都是 0**） | **一张都没有** |
| `Shield` | **14 / 1126** | 54（另有正文里“Gain Shield”那种提到它的） | 14 张 |
| `Invulnerable` | 0（**不是关键词**，靠效果授予） | 16 张正文里授予它 | — |

实证（逐张扫 `keywords` 那一列，命令与输出见 §四）：

```
'dodge'  —— 0 张
'Shield' —— AM20 Astropath · AM29 Primaris Psyker · BL47 Iskandar Khayon · DA34 Ezekiel ·
            DA16 Librarian · EC34 Turyan Ghauze · GOF91 Weirdboy · SOR72 Adelaide the Serene ·
            SW43 Venerable Dreadnought · TAU22 Crisis Bodyguard · TAU42 Riptide Battlesuit ·
            TAU34 Aunshi Ethereal · TAU24 Honoured Ethereal · TAU4 Commander O'Maisos
```

**⇒ 即便 0 张，也照 `铁律 11` 做（不是「不做」）。** 补的是「**原版有、我们缺**」这一层；
`Dodge` 今天在**任何**数据源里都没有写方（`数据/游戏数据/trait_textsprites.json` 87 个图标名里没有它 ·
`数据/本地化/i18n/zh_CN.csv` 也无词条）⇒ 这是**结构性缺口**，将来只要有一张卡写它就必踩。
**⛔ 已在代码注释里写死「不许记成不做」。**

### 2. 结算侧「复用还是另写」——**另写一支，但不是另写一套公式**

**结论：结算侧不复用 `W8b3` 的那四个函数**（`DamageAfterReductionOne` / `DamageAfterReductionList` /
`WouldKillByEntries` / `AttackDamageEntries`），理由逐条：

1. **形状不同（这是根本原因）**：`W8b3` 那几个是**逐条预览**口径，签名里带**下标**
   （`DamageAfterReductionOne(u, dmg, index)`），输入是**一张条目表**；而结算侧一次
   `ApplyDamage(ctx, u, dmg, source)` **只有一个数**、**没有下标可传**。
   原版也是这个形状 —— 结算侧每次伤害都是一个独立的 `CardScript__ReceiveDamage(unit, type, DamageInfo)`
   调用（`BattleManager._ResolveAttack_d__438__MoveNext.c:686` 星镖一次、`:999` 主伤害一次），
   一次一个 `DamageInfo`，**没有条目表**。
2. **我们的结算路根本不产生条目表**：`RuleCore.DeclareAttack` 是「星镖 `Hurt` 一次 + 主伤害 `Hurt` 一次」
   （`RuleCore.cs:3031` / `:3053`），两次调用各带一个数 —— 要「复用」就得先把它们合并成表再拆开，
   那是**把结构改坏**去迁就函数签名。
3. **但也没有第二份伤害公式**（这一条是给 `铁律 6` 交代的）：新增的那一支**落在已经存在的两个函数上** ——
   · `DamageAfterReduction`（`RuleCore.cs:3296`，**全仓唯一一份伤害公式**；`ApplyDamage` 读它、
     `SimpleAI` / `WouldKill` 也读它）加 **1 行**；
   · `ApplyDamage`（`RuleCore.cs:3583-3606`，**唯一副作用口**）把原来那段「盾挡下」**扩成「盾/闪避」并列**。
   ⇒ **没有第三处写「dodge 挡下」这条规则**，也没有改动任何既有数值算式。
4. **`W8b3` 明令「别合并」照旧成立**：预览（逐条、带下标）与结算（一次一个数）**仍然是两条路**，
   第 (7) 组断言专门钉住「两条路必须给出不同的数」，把它们合并回一条**必红**。

### 3. 语义（与 `W8b3` 那一半的关系）

- **预览侧**（`W8b3`，`DamageAfterReductionOne`）：`dodge`/`shield` **只跳第 0 条**、`invulnerable` 任意条目都跳。
- **结算侧**（本次）：`dodge` 与 `shield` **完全同一条分支** —— **挡下这一下 + 把这条摘掉**。
- 两者**不矛盾**：结算侧一次 `ReceiveDamage` = 条目表里的一条，被摘掉之后后续条目自然照算。
  ⇒ 合起来正是「第 0 条被跳、后续条目照算」。

---

## ② 改动清单

### A. `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs`（行尾 **LF**，改前 0/2700 → 改后 0/2732）

| 位置（现行号） | 改前 | 改后 |
|---|---|---|
| `CardDef.cs:1919-1952` | `public const string Shield = "shield";` 后面**直接**是 `CantAttack` | 中间插入 `public const string Dodge = "dodge";` + 一整段判据注释（原版 trait id 240 / 两处反编译出处 / 全池 0 张 / 预览与结算各一支） |
| `CardDef.cs:2307` | `Vanguard, Stealth, Flying, Armour, Shield,` | `Vanguard, Stealth, Flying, Armour, Shield, Dodge,` + 3 行注释指向 `KeywordTable.Dodge` |
| `CardDef.cs:2553` | `new[] { "shield", Shield }, new[] { "regeneration", …` | `new[] { "shield", Shield }, new[] { "dodge", Dodge },` +（换行）`new[] { "regeneration", …` |

**前缀撞车已核**：`Prefixes` 里没有任何条目以 `dodge` 开头，`dodge` 也不以任何既有前缀开头
（以 `d` 开头的只有 `dark pact` / `destroyer` / `duty`）⇒ **不改动任何既有解析结果**。

### B. `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`（行尾 **LF**，改前 0/5887 → 改后 0/5924）

| 位置（现行号） | 改前 | 改后 |
|---|---|---|
| `RuleCore.cs:3282-3284` | `/// 四道（…）：/// 护盾挡下 → 0 · 无敌 → 0 · 易伤 +X · 护甲 max(1,…)。` | `/// 五道（…）：/// 护盾挡下 → 0 · **闪避**挡下 → 0 · 无敌 → 0 · 易伤 +X · 护甲 max(1,…)。` + 3 行「三者挡下判据在原版是并列的（`GetAdjustedDamage.c:36-48`）⇒ 先判谁结果一样」（**逐字保留了原注释其余部分**） |
| `RuleCore.cs:3295-3297` | `if (u.HasShield) return 0;` （下一行就是 `invulnerable`） | 中间插入 `if (u.Has(KeywordTable.Dodge)) return 0;    // 闪避挡下（并会消耗）—— 与盾**同一条分支**` |
| `RuleCore.cs:3341-3348`（W8b3 那段分工注释） | `· 合计版 = …判盾 / 无敌 / 易伤 / 护甲。` · `⛔ **别把 `ApplyDamage` 改成读逐条版**：那会改掉真实伤害的语义（**本件不动伤害模型**）。` | 改成 `…判盾 / 闪避 / 无敌 / 易伤 / 护甲。` · 末句改成「结算侧 = 一次 `ReceiveDamage` 一个数；预览侧 = 一张条目表，**两者形状本来就不同**」（去掉已经过期的「本件不动伤害模型」） |
| `RuleCore.cs:3362-3370`（`DamageAfterReductionOne` 的文档块） | `⚠️ **`dodge` 在我们引擎里本来【完全没有】**（`DamageAfterReduction` 里没有该分支）—— 这一份是它的第一处落地，且**只落在预览这一层**（真伤害模型不动）。详见报告。` | 换成「沿革 + 就地订正」：`W8b3` 当时那句只在**当时**成立；`A1077` 当天把结算侧也补上 ⇒ 现在**两条路各一支**（**保留了更正痕迹**，`铁律 5`） |
| `RuleCore.cs:3550-3557`（`ApplyDamage` 文档头） | `① Shield 全挡（不受伤害）` 紧接 `② Invulnerable …` | 中间插入 `①·b **Dodge 全挡**（不受伤害）—— 🆕 2026-10-08（A1077）…` + 3 行出处（并**如实标注**「`rule_core.gd` 那张表里没有它 ⇒ 这一条不是照 `.gd` 排的，是照原版反编译放进去的」） |
| `RuleCore.cs:3567` | `…只负责**副作用**：日志 / 事件 / 消费盾 / 翻伏击 / 扣血。` | `…消费盾与闪避 / 翻伏击 / 扣血。` |
| `RuleCore.cs:3571-3604`（**主改动**） | `if (u.HasShield) { u.HasShield = false; ctx.Log("… Shield 挡下了 …"); EmitHit(ctx,u,0); return 0; }` | 换成「盾 / 闪避**并列**」的一段（见下） |
| `RuleCore.cs:~3618`（伏击段那句） | `⚠️ 位置在**所有减免之后**（盾挡下 / 无敌 / 护甲减到 0 都到不了这里）` | `…（盾 / 闪避挡下 · 无敌 / 护甲减到 0 都到不了这里）` |

主改动（`ApplyDamage` 里新那一支）的骨架：

```csharp
bool hadShield = u.HasShield;
bool hadDodge  = u.Has(KeywordTable.Dodge);
if (hadShield || hadDodge)
{
    if (hadShield) { u.HasShield = false; ctx.Log($"{u.Name} 的 Shield 挡下了 {source} 的伤害"); }
    if (hadDodge)  { u.RemoveAll(KeywordTable.Dodge); ctx.Log($"{u.Name} 的 Dodge 闪开了 {source} 的伤害"); }
    EmitHit(ctx, u, 0);
    return 0;
}
```

- **「并列」不是随手写的**：原版 dodge 与 shield 各占**一段独立的 `if`**（不是 `else if`），
  ⇒ 两者同时在身时**一起被消耗**（第 (5) 组断言钉这条）。
- **只带盾时行为与改前逐字等价**（同一句日志、同一次 `EmitHit`、同一个返回值）——
  第 (6) 组回归断言钉这条。
- `RemoveAll` 是照原版 `RemoveTrait(0xf0)`「**把这条整个摘掉**」；不是 `RemoveKeyword`（那只减一层）。

### C. `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`（行尾 **CRLF**，改前 23077/23077 → 改后 23211/23211）

| 位置（现行号） | 改前 | 改后 |
|---|---|---|
| `RuleEngineTest.cs:284`（Step 表） | `Step(TestWillDiePreviewEntries);` 后直接 `Step(TestHealAndDraw);` | 中间插入 `Step(TestDodgeKeyword);`（新方法在 `:19170`） |
| `RuleEngineTest.cs:19078-19081`（`W8b3` 预览测试里那段注释） | `🔴 `dodge` 在我们引擎里本来**完全没有**（`DamageAfterReduction` 里没有该分支）—— 这一份是它的第一处落地，且**只在预览这一层**` | 改成「就地订正（2026-10-08，`A1077`）：**前半句当时对、后半句已经不对**」+ 「结算那一支的断言全在 `TestDodgeKeyword`，别在这里抄第二份」 |
| 新方法 `TestDodgeKeyword`（`:19149-19268`） | — | 7 组断言，见下 |

**`TestDodgeKeyword` 的 7 组**（每组期望值都写死「原版算出来那个数」，不拿被测函数反推）：

1. **登记**：`Normalize("Dodge")` / `Normalize("Dodge.")` 都 = `"dodge"`；`Implemented` 含它；
   `KeywordTable.Parse(["Dodge"])` 真的收进关键词表（改前这条会**静默丢弃**）。
2. **结算侧主断言**：带 dodge 的目标挨 3 点 ⇒ `ApplyDamage` 返回 **0**、`Health` 一点没动、
   **`Dodge` 被消耗**；再挨 3 点 ⇒ **返回 3**、`Health` 6→3（**「第 0 条被跳、后续照算」**）。
3. **【灭自证·方向】**：同形状但不带 dodge ⇒ 照常吃满 **3**（与 (2) 的 0 **相反** ——
   把 dodge 从结算侧删掉，(2) 必红）。
4. **攻击路**：`Shuriken 3` + 主伤害 2 打带 dodge 的 7 血目标 ⇒ **星镖（先落的第 0 条）被挡下、主伤害照算** ⇒ 剩 **5**。
5. **盾 + 闪避同时在身** ⇒ 一下把**两条都消耗掉**、挡下 = 0、血量没动（原版那两段是并列的 `if`）。
6. **回归**：盾单独在身时行为没变（本件动过 `ApplyDamage` 那一支的结构）。
7. **【灭自证·两条路】**：同一局面下**预览逐条** `DamageAfterReductionList` = **3**、
   **结算合计** `DamageAfterReduction` = **0** ⇒ 两条口径**必须不同**（合并成一条必红）。

---

## ③ 证据：`CardScript__EnoughPendingDamageToDieWithDamageValues.c:115-119` 逐句

```
115:  cVar4 = EntityScript__HasCurrentTrait(param_1,0xf0,0);              ← 0xf0 = 240 = dodge
116:  if ((((cVar4 == '\0') || (iVar11 != 0)) &&                           ← ① 「没有 dodge」**或**「不是第 0 条」
117:      ((cVar4 = EntityScript__HasCurrentTrait(param_1,0x50), cVar4 == '\0' ||   ← 0x50 = 80 = shield
118:       (iVar11 != 0)))) &&                                               ← ② 同上：只对第 0 条生效
119:     (cVar4 = EntityScript__HasCurrentTrait(param_1,0x136), cVar4 == '\0')) break;  ← 0x136 = 310 = invulnerable
```

逐句读法：

- `iVar11` = 循环里那一条伤的**下标**（`:121 iVar11 = iVar11 + 1`）。
- `:115-118`：`dodge` 与 `shield` **各一个条件、形状完全相同**，两个都带 `|| iVar11 != 0`
  ⇒ **只在 `iVar11 == 0`（第 0 条）时会把它们判成「跳过」**；第 1 条起那两个条件恒真、不再跳。
- `:119`：`invulnerable` **不在 `iVar11 != 0` 那一组里** ⇒ **任何条目**都跳。
- `break` 出去之后（`:123` 起）才真的读伤害值、`:138` 判 `0 < iVar7`、
  `:156 iVar7 = Math.Max(1, iVar7 - iVar8)`（护甲**每条各扣一次**）。

**同族第二处（结算侧，本次新引的判据）**：

- `CardScript__GetAdjustedDamage.c:36-48` —— `0xe6`(dropPod) / `0xf0`(dodge) / `0x136`(invulnerable) /
  `0x50`(shield) **四个并列**，任一命中就写同一个 `uVar3 = 1` ⇒ **四者「挡住」这件事上不分差异**；
  只有 `invulnerable` 在后面**没有** `RemoveTrait`（即**不消耗**）。
- `CardScript._ReceiveDamage_d__381__MoveNext.c:39-46`（挡下：四个 trait 一命中就**整段跳过扣血**）·
  `:320-325`（dodge：`RemoveBuffedTrait(0xf0)` + `RemoveTrait(0xf0)`）·
  `:326-333`（shield：同形，`0x50`）—— **两段是并列的 `if`，不是 `else if`**。

**我们引擎的对应**（`RuleCore` 两处，各自只有一行/一段）：

| 原版 | 我们 |
|---|---|
| `GetAdjustedDamage.c:41`（dodge 挡下） | `DamageAfterReduction:3296` + `ApplyDamage:3583-3603` |
| `_ReceiveDamage…:320-325`（dodge 消耗） | `ApplyDamage:3597` `u.RemoveAll(KeywordTable.Dodge)` |
| `EnoughPendingDamage…:115-118`（预览只跳第 0 条） | `DamageAfterReductionOne:3377`（`W8b3`，未动） |

---

## ④ 验证

### 1. 秒级类型检查（命令：`TMPDIR=/tmp/wf_dodge bash d:/4/Unity/工具/typecheck.sh`）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（共跑 **3 次**：CardDef 改完 1 次 · RuleCore 改完 1 次 · 自检断言写完 1 次 —— 三次都是 `0 / 0`，
**没有出现「错误全在别人正在写的文件上」那种噪声**。）

### 2. 行尾（**二进制读**，`b.count(b'\r\n')` vs `b.count(b'\n')`）

| 文件 | 改前 | 改后 | 判定 |
|---|---|---|---|
| `CardDef.cs` | `0 / 2700`（纯 LF） | `0 / 2732`（纯 LF） | ✅ 没翻 |
| `RuleCore.cs` | `0 / 5887`（纯 LF） | `0 / 5924`（纯 LF） | ✅ 没翻 |
| `RuleEngineTest.cs` | `23077 / 23077`（纯 CRLF） | `23211 / 23211`（纯 CRLF） | ✅ 没翻 |

（全程**只用 Edit 工具**，没有 `sed -i`、没有用 python 文本模式写。）

### 3. `git diff --numstat`

```
43	3	Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs
689	72	Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs
1054	19	Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs
```

🔴 **这三个数【不是本件的改动量】** —— 三个文件在**本会话开工前就已经是 `M`（未提交）**，
`git diff` 里混着**上一会话 + 别的写手**的改动（`git status --porcelain RuleEngine/`：
`BattleContext.cs` · `CardDef.cs` · `CreatePool.cs` · `EffectResolver.cs` · `RuleCore.cs` ·
`UnitState.cs` · `DeckBuilder.cs` · `SimpleAI.cs` · `RuleEngineTest.cs` 全是 `M`）。
**本件的改动量**（逐处数过）：CardDef ≈ **+37 / ~2 改**；RuleCore ≈ **+40 / ~6 改**；
RuleEngineTest ≈ **+135 / ~6 改**。**判「行尾没翻」看的是上面那张表**（numstat 只用来排除「整篇重写」）。

### 4. 卡池计数命令（`①` 那一节的实证来源）

```python
# 逐张扫 keywords（count=1126）
d = json.load(open('.../RuleEngine/Resources/cards_engine.json', encoding='utf-8'))
# 'dodge' → 0 张；'Shield' → 14 张（逐张列在 §①）
# 另：raw.count('Dodge') == 0 且 raw.count('dodge') == 0（全文，含 desc/descZh）
```

---

## ⑤ 没查清 / 停手的

1. **🔴 断言没有跑过（本件最大的未验项）** —— 用户本轮明确要求**不跑 Unity 自检**，
   所以 `TestDodgeKeyword` 的 7 组断言**只做了逐条手推**（每一步都对着 `RuleCore` / `ApplyDamage`
   的实际代码走了一遍，推理过程见 §②C 与 §①3），**没有真跑**。
   ⇒ 请调度台在收口那一轮跑 `RuleEngineTest.Run` 时重点看这一段的红绿。
   **纯逻辑（`DamageAfterReduction` / `DamageAfterReductionOne`）我有把握；**
   **攻击路那组（第 (4) 组）依赖 `Duel` + `DeclareAttack` 夹具链路，是这一批里唯一有夹具风险的一条**
   （同类先例：`RuleEngineTest.cs:4425-4430` 的星镖用例，夹具形态一致）。
2. **`dodge` 为什么与 `shield` 并存**（原版两处代码**任何地方都分不出差异**）—— **没查到**。
   搜过的路径：`d:/2/tools/decomp_full/`（25,096 个 `.c`，`grep -rl "Dodge"` 只命中
   `UnityEngine.UI.Extensions.UILinearDodgeEffect*` 三个**非机制**文件）· `all_strings.txt` ·
   `数据/本地化/i18n/zh_CN.csv`（0 词条）· `数据/游戏数据/trait_textsprites.json`（87 个图标名里没有）·
   `d:/2/Warpforge部队卡片/`（`grep -rn "Dodge"` **0 命中**）。⇒ **如实报「查不到」**，不猜语义。
3. **原版的两处伤害次序不一致**（→ §⑥ 第 3 条）：**只查实了「两处确实不同」，没查清「为什么」**。

---

## ⑥ 顺手发现的（不在本件范围，交调度台分流）

1. **🔴 `GivePayload.GiveKw` 里也没有 `dodge`**（`RuleEngine/Core/GivePayload.cs:131-167`，54 条）。
   影响面：**效果文字**写 `Give Dodge to a friendly unit` 那类**载荷路**时，
   `GiveKw` 从头匹配不到 ⇒ 落到 `ReAttr` / `ReBareSigned` 也都不中 ⇒ **整段判「不认识」**
   （好在会进 `UnparsedEffects` 诊断单 + 卡面打 `*`，**不是完全静默**）。
   ⇒ 这是「认得出」这一层的**第二个口子**，本件**没动**（理由：`GivePayload.cs` 不在白名单，
   且金池 0 张卡受影响、改它不改任何现有行为）。**建议另开一小件补 `new[] { "dodge", "dodge" },`。**
2. **`Shield` 的两个表示**（`UnitState.HasShield` 字段 vs `"shield"` 关键词）**会互相脱节**：
   - **消耗时**：`RuleCore.cs:3589` 只写 `u.HasShield = false`，**从不摘关键词**
     （全仓 `grep -rn "RemoveAll(\"shield\")\|RemoveKeyword(\"shield\")"` **0 命中**）
     ⇒ 盾用掉之后 `u.Has("shield")` **仍然为真**。
     后果：`RuleEngine/Data/SimpleAI.cs:588` 读的是**关键词**（`target.Has(KeywordTable.Shield)`）
     ⇒ **AI 会把「盾已经用掉」的单位继续当成带盾、只给「一点点」分**（表现层没事 ——
     `CardPresentation/Battle/BattleDriver.cs:11217` 读的是**字段**，是对的）。
   - **反向**：`UnitState.RemoveAll("shield")` **不会**把 `HasShield` 清零
     （`UnitState.cs:604-622` 只处理了 `armour` 与 `blind`）⇒ 万一有卡 `lose Shield`，
     关键词没了、**那一刻还能再挡一下**。
   - ⇒ 本件的 `dodge` **没有踩这两个坑**（一律走 `Has("dodge")` + `RemoveAll`，**只有一个表示**）。
     修那两个坑要动 `UnitState.cs`（不在白名单）⇒ **只报不改**。
3. **原版自己有两套「伤害条目次序」，`W8b3` 选了其中一套、本次结算侧跟着另一套**：
   - `CardHighlight__ToggleCombatPreviewHighlight.c:62-81`：**主伤害先**（`:62-65`）、
     再星镖（`:67-72`）、再标记光（`:74-81`）⇒ 条目 = **`[主伤害, 星镖, 标记光]`**（`W8b3` 的 `AttackDamageEntries` 照这套）。
   - `BattleManager__RecordAttackPendingDamage.c:59-84`：**星镖先记**（`:69-73`）、
     再标记光、再主伤害（`:80-84`）⇒ 条目 = **`[星镖, 标记光, 主伤害]`**；而 `ReceiveDamage` 也确实是星镖先
     （`BattleManager._ResolveAttack_d__438__MoveNext.c:686` 在前、`:999` 主伤害在后）。
   ⇒ **谁被 `dodge` 挡掉，这两套给的答案不同**（预览说挡「主伤害」、实际结算挡「星镖」）。
   **本次结算侧照的是 `RecordAttackPendingDamage` 那一套（= 真实落地的次序）**，与 `DeclareAttack`
   （星镖先于主伤害）自洽；第 (4) 组断言把这条**显式写进注释**。
   ⚠️ **`AttackDamageEntries`（喂 `BattleDriver.SetReticleTarget` 的「会不会死」准星）用的是另一套** ⇒
   对「带 dodge 且有星镖」的极小局面，**准星可能与实际结果不符**。
   **⇒ 只报不改**（那是 `W8b3` 的件、且改它会动到准星与它那批断言，属需要裁定的判断）。
4. **`SyncKeywordState` 不需要为 `dodge` 加分支**（`UnitState.cs:575-586`）——
   `shield` 之所以要在那里写一句 `HasShield = true`，是因为它**另有一个缓存字段**被引擎别处读；
   `dodge` **没有字段**（全仓 `dodge` 的读点只有 `Has(...)`），关键词字典就是唯一表示
   ⇒ **加了反而是第二份表示**（正是 §⑥2 那个坑的成因）。**如实说明「简报里的 ② 不适用」，不是漏做。**
   （且 `UnitState.cs` 不在本件白名单。）

---

## ⑦ 同批的如实标注（`A1077` 那一栏要求的、别丢）

- **`§8b 3(a)`：技能致死预览在真池上今天结构性走不到** —— `Ability:` 行 **0 条**；
  `Oath N:` 里能打伤害的全是裸 `Deal N damage` ⇒ 正是原版该排除的 `lowestLevel`(=240) 那一档
  （只认 `TargetsAffected.target`(=30)）。出处：`资料/普查产出_第六会话/W8b3_主动技能致死预览与逐条扣甲.md:192`
  一带 + 该文件 §一。**本件没有改动这一条，照抄留痕。**
- **本件新增的 `dodge` 也一样是「今天 0 张卡」** —— 它不是「修一个今天会犯的错」，
  而是照 `铁律 11` 把**原版有、我们缺**的那一支补上。两件事**性质相同、都要留痕**。

---

## 摘要（300 字以内）

现池 **`Dodge` 0 张 · `Shield` 14 张**（逐张扫 `cards_engine.json` 的 `keywords`；全文扫 `dodge` 词根 0 命中）。
按铁律 11 **照做**，不改口径。补齐两半：
① `CardDef.cs` 加 `KeywordTable.Dodge` 常量 + 进 `Prefixes`（`:2553`）与 `Implemented`（`:2307`）
⇒ 卡数据写 `Dodge` 不再被 `Normalize` 判 null 后静默丢弃（前缀撞车已核）。
② 结算侧按原版落两处：`DamageAfterReduction:3296` 加一行、`ApplyDamage:3583-3604` 把「盾挡下」
扩成「盾 / 闪避**并列**」（照原版两段独立 `if` ⇒ 同时在身时一起消耗；只带盾时逐字等价）。
**没有另写公式**，也没有与 `W8b3` 的逐条预览合并。
新增 `TestDodgeKeyword`（7 组：登记 / 结算挡下+消耗+后续照算 / 不带反例 / 攻击路 / 盾闪避同在 /
盾回归 / 两条路必须不同），**只手推未跑**（本轮禁止跑自检）。
类型检查 **0 错 ×3**；三个文件行尾未翻（LF / LF / CRLF）。

