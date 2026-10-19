# W57 · 战斗线 · `TraitParticles` 认 `#i` 后缀（SC-7）

- 日期：2026-10-20
- 波次口径：**只做不验**（未跑 Unity / 未起 player / 未跑任何 `*.Run`；只跑了秒级 typecheck）
- 白名单：`Unity/MyGame/Assets/CardPresentation/Core/TraitParticles.cs` —— **只动了这一个文件**
- 判据：`资料/普查产出_1019/W57_战斗线_T09_WIRE.md` 的「⚠️ 没改的一处（记下来）」（`:173-174`）
  与「⚠️ 留给下一会话」②（`:301`）；解码口 = `RuleEngine/Core/BattleEvent.cs:410` `SplitKeywordAbility`

---

## 0. 🔴 先纠一处「任务前提」（铁律 5 · 就地订正）

任务简报里写的是「`Core/TraitParticles.cs` 的 `PlayTraitParticles` Trigger 支」——
**`PlayTraitParticles` 不在 `TraitParticles.cs` 里**（那是只有 84 行的**静态查表类**）。

现核实位（`grep -n "PlayTraitParticles" Assets --include=*.cs`）：

| 东西 | 真实位置 |
|---|---|
| `void PlayTraitParticles(BattleEvent e)` | `CardPresentation/Battle/BattleDriver.cs:16080` |
| Trigger 支（`want = { e.Keyword.ToLowerInvariant() }`） | `BattleDriver.cs:16087-16089` |
| 真正的护栏 `if (u.Has(t)) TraitParticles.Play(...)` | `BattleDriver.cs:16099`（`u.Has` = `UnitState.Has`，`RuleEngine/Core/UnitState.cs:646`） |

⇒ **端到端那两行在 `BattleDriver.cs`，在本波白名单之外**。按简报红线（「必须改 ⇒ 停手写报告」）
我**没有**改它，只把**查表层**（白名单内的 `TraitParticles.cs`）修成认得 `#i`，
并在 §4 给出那两行的**逐字补丁**，交给能碰 `BattleDriver.cs` 的代理/主对话。

---

## 1. 改前 / 改后

### 1.1 改前（HEAD `c0cc4a82` 那一版，84 行）

```csharp
// TraitParticles.cs（HEAD）
public static bool Has(string trait)
{
    return !string.IsNullOrEmpty(trait) && FromCode.ContainsKey(Norm(trait));
}

public static string PrefabFor(string trait)
{
    string s;
    return (trait != null && FromCode.TryGetValue(Norm(trait), out s)) ? s : null;
}

static string Norm(string s) { return string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant(); }
```

- 全文件**没有任何 `#i` 处理**：`Norm("UseFaith#1")` = `"usefaith#1"`，`FromCode` 里没有这个键 ⇒
  `Has` = false、`PrefabFor` = **null**、`Play` = **false**。
- 而且这三个返回 false/null 时**只有 `Play` 的「效果库里没有 prefab」那一支会出声**；
  **「查不到表」这一支是完全静默的**（`if (name == null) return false;`，一行日志都没有）——
  正是本项目「不许静默失败」那种形状。

### 1.2 改后（`git diff --numstat` = **73 加 / 5 删**，LF 保持：152 行全 LF、CRLF 0）

① `using RuleEngine;`（`BattleEvent` 在 `RuleEngine` 命名空间；同族的 `VfxMap.cs:17` 早就这么用）。

② **唯一的规范化入口**改成会拆 `#i` 的重载，**解码只此一处**（转发 `BattleEvent.SplitKeywordAbility`，
⛔ 不另写解析、⛔ 不改事件本体）：

```csharp
static string Norm(string s, out int abilityIndex)
{
    abilityIndex = -1;
    if (string.IsNullOrEmpty(s)) return "";
    s = s.Trim();
    if (s.Length == 0) return "";
    s = BattleEvent.SplitKeywordAbility(s, out abilityIndex);   // 判据只此一处
    return s.Trim().ToLowerInvariant();
}

static string Norm(string s) { return Norm(s, out _); }
```

③ `Has` / `PrefabFor` 改走这个重载（**行为语义不变**：空串 / null 仍 false / null，
无后缀的旧键**逐字节不变**）：

```csharp
public static bool Has(string trait)
{
    int abilityIndex;
    string key = Norm(trait, out abilityIndex);
    NoteKeyForTest(trait, key, abilityIndex);
    return key.Length != 0 && FromCode.ContainsKey(key);
}

public static string PrefabFor(string trait)
{
    int abilityIndex;
    string key = Norm(trait, out abilityIndex);
    NoteKeyForTest(trait, key, abilityIndex);
    string s;
    return (key.Length != 0 && FromCode.TryGetValue(key, out s)) ? s : null;
}
```

④ `Play` **一行没动**（它内部先调 `PrefabFor` ⇒ 自动跟着认 `#i`）；
连带地，它那两条日志里的 `{Norm(trait)}` 现在印**基名**（`usefaith` 而不是 `usefaith#1`）—— 正是「玩家该看到的形态」。

⑤ **Editor-only 只读探针**（`#if UNITY_EDITOR`；player 里只剩一个空体 `NoteKeyForTest`，零开销）：

| 成员 | 用途 |
|---|---|
| `LastRawKeyForTest` | 最近一次**原始键**（可能含 `#i`） |
| `LastTriggerKeyForTest` | 最近一次**基名**（已拆 `#i` · 已 trim + 小写） |
| `LastAbilityIndexForTest` | 最近一次拆出的能力序号（**无后缀 = -1**） |
| `ResetProbeForTest()` | 清读数（免得断言读到别人留下的值） |

- 写入点 = `Has` / `PrefabFor`（`Play` 先调 `PrefabFor`，所以 `Play` 也覆盖）。
- 探针**不参与**任何「播不播」的判断，纯只读。

### 1.3 行为核对（逐例）

| 输入 | 改前 | 改后 | 说明 |
|---|---|---|---|
| `"swarm"` | `swarm` | `swarm` | 不变 |
| `"UseFaith#1"` | `usefaith#1`（查不到） | `usefaith` + idx=1（**仍查不到，因为表里没绑 `UseFaith`**） | 键对了，但表里本来就没有 |
| `"Synapse#0"` | `synapse#0`（查不到） | `synapse` + idx=0 ⇒ **能命中** | 这才是真正被修好的形态 |
| `null` / `""` | false / null | false / null | 不变 |
| `"a#b"`（后缀不是整数） | `a#b` | `a#b` | `SplitKeywordAbility` 原样返回（判据 `:417-419`） |
| `"#1"`（`at <= 0`） | `#1` | `#1` | 同上（判据 `:415`） |

---

## 2. 同文件其它「读 `Keyword`」的点（grep 现核）

`grep -ni keyword Assets/CardPresentation/Core/TraitParticles.cs` —— **全文只有 1 处命中，而且是注释**：

| 行（改后） | 内容 | 是否有同样的 `#i` 问题 |
|---|---|---|
| `:25`（doc） | `/// <summary>trait（规范化键，与 UnitState.Keywords 同一套）→ …` | 注释，不是读取点 |
| `:48-56` `Has` | 走 `Norm` | ✅ 已随 §1 修好 |
| `:63-70` `PrefabFor` | 走 `Norm` | ✅ 已修好 |
| `:74-89` `Play` | 走 `PrefabFor` + 日志用 `Norm` | ✅ 已修好（透明继承） |

⇒ **同文件没有别的 `Keyword` 读取点**（这个类不读 `BattleEvent`，只收字符串键）；
所以也没有第二处要改。

---

## 3. 哪些支现在认 `#i`、哪些不认（如实）

对照 `BattleDriver.PlayTraitParticles`（`BattleDriver.cs:16080-16100`）的三支：

| 支 | 键从哪来 | 改前 | 改后 | 结论 |
|---|---|---|---|---|
| `EvtKind.Ability` | 常量 `KeywordTable.Ferocity`（`:16085`） | N/A | N/A | 不涉及事件 `Keyword`，`#i` 问题不存在 |
| `EvtKind.Attack` | 字面量 `sniper/markerlight/longrange/stomp`（`:16103-16104`） | N/A | N/A | 同上 |
| `EvtKind.Trigger` | `e.Keyword.ToLowerInvariant()`（`:16088`） | ❌ 不认 | ⚠️ **半认**：`TraitParticles.Play` 这一层已认 `#i`；**但 `:16099` 的 `u.Has(t)` 护栏不认** ⇒ **端到端仍然不播** | 差 `BattleDriver.cs` 那两行（见 §4） |

**另外两条如实说明：**

1. **今天看不到可见差异**：全仓 `#i` 后缀**只有一处**能产生 —— `EffectResolver.EmitAbilityUseTrigger`
   （`EffectResolver.cs:5916` / `:5921` 传 `idx[i]`）→ `EmitCardTrigger:5794` 调 `BattleEvent.KeywordWithAbility`）；
   其它所有 `EvtKind.Trigger` 发射点（`EmitFollowUpResolution:5755` · `EmitTurnTrigger:5957` ·
   `EmitListenerTrigger:5765`（唯一发射体 `EmitCardTrigger:5793`）· `TriggerEnchantment:8203` ·
   `RuleCore:4956/8830/8919`）**都是裸名、不带后缀**。
   而那两个带后缀的 trigger 名是 **`UseFaith` / `UseSpiritStone`**，**它们本来就不在 `FromCode` 表里**
   ⇒ 即使 §4 那两行补上，**今天也不会有粒子**。
   ⇒ 本改动是**正确的底座**（等这两族将来绑了 prefab 就直接能用），但它**不是**「双 605 今天少播了一个粒子」。
   ⚠️ 建议把「`UseFaith`/`UseSpiritStone` 该不该有 FromCode 粒子」单独入账（本轮判不了：本地 `CardTrait` SO 0 命中，见文件头 `:13-14`）。
2. **`swarm` / `synapse` 这两条真会命中的 trigger，今天都不带 `#i`** ⇒ 现有播放行为**零变化**（风险面 = 0）。

---

## 4. ⏭ 还差的那一步（**不在本波白名单内，我没改**）

`Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` 两处：

```csharp
// :16087-16089（改前）
case EvtKind.Trigger:
    if (!string.IsNullOrEmpty(e.Keyword)) want = new[] { e.Keyword.ToLowerInvariant() };
    break;

// 建议改成（拆后缀 → 再小写；解码口仍然只走 BattleEvent 那一处）
case EvtKind.Trigger:
    if (!string.IsNullOrEmpty(e.Keyword))
        want = new[] { BattleEvent.SplitKeywordAbility(e.Keyword, out _).Trim().ToLowerInvariant() };
    break;
```

- 这一行同时修好 `:16099` 的 `u.Has(t)` 护栏（`UnitState.Has` 是 `_keywords.ContainsKey`，`UnitState.cs:646`，
  **引擎键本来就永不带后缀** ⇒ 不应该去改 `UnitState.Has`，要在**调用点**拆）。
- ⛔ 别改成「改 `UnitState.Has` 也拆」—— 那会把「传输形态」漏进引擎层，违反 `BattleEvent.cs:400`
  「解码在 `SplitKeywordAbility`（同一处）」的口径。

---

## 5. 没查清的部分

1. **`UseFaith` / `UseSpiritStone` 到底该不该有 FromCode 粒子** —— 判不了：
   `CardTrait` SO 本体全库 0 命中（`TraitParticles.cs:13-14` 已记），
   而 `FromCode` 表是从 `CardAnim` 名反推的（**我们的口径**）。⇒ 这张表要不要为 605/600 族加行，缺判据。
2. **`#i` 与 `FromCode` 的真实交集** —— 今天为空（§3 第 1 条）。等将来上表加了新 trait 再核。
3. 本波**按要求没验**：没跑任何 `*.Run`、没进 player、没截图 ⇒
   「探针在真断言里读得出对的值」只有**代码层推演**，没有运行时读数。

---

## 6. typecheck 结果

```
TMPDIR=/tmp/wf_sc7 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 一次通过，**没有**遇到「报错集中在别人的文件」那种情况（本波并行代理改的 `RuleEngine/**`、
  `Shell/AlliancePanelWindow` 当时都没有半成品残留在编译面上）。
- 行尾核对：`TraitParticles.cs` 改后 **152 行 / LF 152 / CRLF 0**（改前 84 行 LF）；
  `git diff --numstat` = `73  5` ⇒ **没有整篇翻行尾**。
- 只动了一个文件：`git status --porcelain` 里本代理名下只有 `Assets/CardPresentation/Core/TraitParticles.cs`（+ 本报告）。
