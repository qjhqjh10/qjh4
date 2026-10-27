# D1 · `RuleEngineTest.Run` 两条红的定性（只读诊断）

日期：2026-10-27 · 对象：`d:/4/_tmp_view/ruleengine.log`（第八十六会话同步点实跑，退出码 1）

---

## 〇 先纠一个数字：**不是 2 条红，是 1 条断言被印了两遍**

| 证据 | 说明 |
|---|---|
| `ruleengine.log:193368` | 该断言跑的时候**当场** `✗` 一次 |
| `ruleengine.log:272401` | 收尾汇总**把失败清单再印一遍**（`RuleEngineTest.cs:1265` 那个 `sb.Append`；`Check` 在 `:1402-1412` 既 `Debug.LogError` 又 `_failures.Add`） |
| `ruleengine.log:272400` | `=== 结束：9440/9441 通过，**1 条失败** ❌ ===` —— 汇总自己说 **1 条** |

⇒ 「2 条」是 `grep -c "UnityEngine.X"` 数出来的**印刷次数**，不是断言条数。真正要修的是**一条**。

---

## 一 结论：**γ（本批回归）**

> 一句话：这条断言**没有过时、也没有写错**，它抓到的是一件真事 —— 本批（`W13CDW`，未提交，`git diff --numstat` = `127 0`）在
> `RuleEngine/Core/` 里新写了 5 处 `UnityEngine.Vector2/Vector3`，破坏了本批之前一直成立的性质「`Core/` 只碰 `Debug`/`Random`」。

**为什么不判 (α) 断言错（意图已过时）**：它的意图是「`工具/ruleprobe` 能只编 `Core/*.cs`（极简桩、net8）跑真解析器」——
这条边界**今天仍在使用**（`check` / `play` / `ai` / `tutorial` / `traits` 各读数口都靠它），没过时。
**为什么不判 (β) 实现缺陷**：新代码的算术本身是对的、业务断言也绿；它破的是**架构规矩**，不是业务逻辑。
**为什么不判 (δ) 夹具前提不成立**：夹具前提（`Core/*.cs` 存在、≥20 个文件）实测成立 —— 现读 **41 个**文件。

---

## 二 证据

### 2·1 断言在哪、说它要什么

| 位置 | 内容 |
|---|---|
| `Assets/RuleEngine/Editor/RuleEngineTest.cs:29595` | `static void TestCoreLayerPurity()`（宿主方法） |
| `:29646` | `Check(r.Unity.Count, 0, "…`UnityEngine.X` 只有 `Debug` / `Random`" + HitDetail(r.Unity));` ← **红在这一行**（栈里报 `:29644` 是上一条 `Check(r.Banned…`，它本轮是 ✓） |
| `:30291` | `static readonly HashSet<string> CoreAllowedUnity = new HashSet<string> { "Debug", "Random" };` |
| `:30313-30345` | 扫描器 `ScanCoreTexts`：剥注释/字面量后逐个数 `UnityEngine.<Type>`（`:30334` 那行判白名单），不在名单就进 `r.Unity` |
| `:29571-29582` | 意图正文：**A941（2026-10-18）**「`Core/` 只许碰 `UnityEngine.Debug` / `UnityEngine.Random`」，理由 = 探针只编 `Core/*.cs`、用一份极简桩在 net8 下跑真解析器；当年 `TutorialScript.cs` 用 `#if UNITY_5_3_OR_NEWER` 包 `Resources.Load` + `JsonUtility.FromJson`，「Unity 下编得过、探针下走 `#else`，**两条路都不报错**」⇒ **处置是「把那一跳搬进 `Data/TutorialDatabase.cs`」，不是放宽白名单** |

### 2·2 5 处命中（与日志逐行对得上：650/651/660 是 Vector3，682/692 是 Vector2）

```
Assets/RuleEngine/Core/GameplayVariables.cs:650:  public static UnityEngine.Vector3 PlayedCardDisplayPosition(
Assets/RuleEngine/Core/GameplayVariables.cs:651:      UnityEngine.Vector3 handAnchorWorld, bool offensive, bool smallScreen)
Assets/RuleEngine/Core/GameplayVariables.cs:660:      return new UnityEngine.Vector3(handAnchorWorld.x + x, handAnchorWorld.y - y, handAnchorWorld.z);
Assets/RuleEngine/Core/GameplayVariables.cs:682:  public static UnityEngine.Vector2 PlayedCardDisplayOffsetPx(
Assets/RuleEngine/Core/GameplayVariables.cs:692:      return new UnityEngine.Vector2(x * k, y * k);
```

### 2·3 这条红的**真实代价已经发生过一次**（不是纸面上的）

`资料/完全复刻/报告/B9_S0EFFECT.md:131` 早在本批中途就记了：`GameplayVariables.cs(651,25): error CS0234` ⇒
**「全批次所有人的 `ruleprobe check / play` 读数此刻都不可用」**（那位代理重试 3 次都是这个错）。
⇒ 这条断言保护的正是这条生产链，**它报得对**。
现读补救现状：`工具/ruleprobe/Stub.cs:59-80` 已被调度台补了 `Vector2` / `Vector3` 两个桩（含注「⛔ 别扩成完整向量库」）。

### 2·4 消费者账（决定改法成本）

| 方法 | 真实调用点 | 说明 |
|---|---|---|
| `PlayedCardDisplayOffsetPx`（`Vector2`） | `CardPresentation/Battle/BattleDriver.cs:35557`<br>`CardPresentation/Editor/BattleScene.cs:48950`<br>`CardPresentation/Editor/BattleScene.cs:48982` | **共 3 处**，且**只读 `.x` / `.y`**（无向量运算、无 `Vector2` 之间比较） |
| `PlayedCardDisplayPosition`（`Vector3`） | **0 处** | 全仓现读只有定义 + 注释（`B9_W44HUD_战斗界面剩余C.md:194` 也记「现读零消费者」）⇒ 改签名**零调用点成本** |

### 2·5 账落点（决定「能不能动行数」）

`.tsv` 里锚到 `GameplayVariables.cs:<行>` 的行（现读可读的 9 个账文件）：
主表 `账/战斗界面_HUD.tsv` **2 处 = `607-623` ＋ `:682`**；归档补丁 `_parts/_merged_B9_W67CENTER/B9_W67CENTER.tsv` 同样那 2 处；
`_merged_B9_W44HUD/B9_W44HUD.tsv` 只有 `607-623`。（`607-623` = `VarsGlobal` 那 8 个纯 float 常量块。）
⇒ **`:682` 正好是这次要改的那一行**。任何**在其之前插行**的动作都会把这 2 条账顶漂。
⇒ 改法必须**同格改、零行数变化**（新类型放到 `:692` 之后 / 另起新文件）。

---

## 三 两条修法对比 + 建议

### (A) 改实现 —— 让 `VarsGlobal` 不返回 `UnityEngine.*` ✅ **建议这条**

做法（三选一，都**不改算术**）：

| 微变体 | 形态 | 行数影响 |
|---|---|---|
| A-1（推荐） | 新增 `Assets/RuleEngine/Core/VarsGlobalTypes.cs`：`public struct WorldPoint { public float x, y, z; … }` + `public struct PxOffset { public float x, y; … }`，两方法改用它 | **零**（新文件，`GameplayVariables.cs` 只改 5 行同格） |
| A-2 | 把两个 struct 追加在 `VarsGlobal` 类尾（**`:692` 之后**） | 零（插入点在第 682 行之后 ⇒ 账锚不动） |
| A-3 | 返回命名值元组 `(float x, float y)` / `(float x, float y, float z)` | 零，但**本仓 RuleEngine 里现读 0 处元组返回**（新习语，风格不统一） |

具体改动清单（A-1）：

1. `Core/GameplayVariables.cs` —— 5 行**同格**改（650/651/660/682/692），**不增不删行**。
2. 新文件 `Core/VarsGlobalTypes.cs`（约 14 行，两个 struct）。名字自带单位（`PxOffset` = 设计像素、`WorldPoint` = 世界坐标）
   —— 这不是「自己设计的行为」，只是**换掉一个 Unity 值类型**，语义逐字等价 ⇒ **按铁律 18 ⛔②「命名/结构不同但语义一致」不打 `🔶` 标**。
3. 3 处调用点各改**一行声明**（`Vector2 x = …` → `var x = …`），`.x`/`.y` 继续原样可用 ⇒ **逻辑零改动**：
   `BattleDriver.cs:35557`、`BattleScene.cs:48950`、`BattleScene.cs:48982`。
4. `工具/ruleprobe/Stub.cs:59-80`：`Vector2` / `Vector3` 两个桩**可以撤回**（`Core` 不再需要它们；`Data/CardDatabase.cs` 等仍需要的 `Resources`/`JsonUtility`/`TextAsset` 不受影响）。

**代价**：3 个文件、约 20 行；两个新类型名（但换来「world / px」单位写在类型上 —— 本仓正被「14.835 px/单位 vs 108 px/单位」混淆坑过，见 `调度台日志.md:6535`）。
**收益**：白名单恢复「`Core` 用到的桩成员 = `{Debug, Random}`」这条**可核对的不变量**；与 A941 先例（**把 Unity 依赖搬出 `Core`**）同向。

### (B) 改断言 —— 白名单加上 `Vector2` / `Vector3`

做法：`RuleEngineTest.cs:30291` 一行改成 `{ "Debug", "Random", "Vector2", "Vector3" }`（零行数变化，代价最小的字面改动）。

**代价 / 失去什么**：

1. **白名单不再等价于「探针要哪些桩」** —— 这条断言原本是「探针能不能只编 `Core`」的**代理指标**。放开之后，`Core` 用到 `Vector2` 而桩里没有，
   唯一会被发现的地方是**探针构建**（`CS0234`，不是这条断言）。
2. **下一个值类型的口子会被同一理由推开**：`Color` / `Quaternion` / `Rect` / `Mathf`。其中 **`Mathf` 是真危险** ——
   桩要复刻 Unity 语义，而本仓已记过「`Mathf.InverseLerp/Lerp` 自带 `Clamp01`」这类**语义陷阱**
   ⇒ 那正是 A941 要防的「两条路都不报错、行为不一样」。
3. **与先例反向**：A941（2026-10-18）遇到同类事（`TutorialScript` 碰 Unity）是**把依赖搬出 `Core`**，不是放宽白名单。
4. 允许的 5 处命中里有 **3 处属于零消费者的 `PlayedCardDisplayPosition`** —— 为了一个没人调用的方法去放宽一条架构断言，性价比尤其差。

### 建议

**走 (A-1)**。三条理由：
① 成本可控（3 文件 ~20 行，**零行数变化**、账锚 `:682` 不动、3 个调用点逻辑零改）；
② 保住断言作为「探针可编」的唯一自动闸（`Mathf` 那类**会静默分叉**的值类型仍在闸外）；
③ 与 A941 先例同向，不开「每个值类型来一次表决」的先例。

⚠️ 若走 (A)，顺带确认一句：**`VarsGlobal.PlayedCardDisplayPosition`（世界版）零消费者** ——
`B9_W44HUD` / `B9_W67CENTER` 只要求「公式别写第二份」，没要求它必须用 Unity 向量 ⇒ 换类型不影响任何判据。

---

## 四 核对：`Core/` 里还有没有**别的** `UnityEngine.*` 类型引用

现读写脚本（剥注释 + 剥字面量，与 `RuleEngineTest.StripCsComments` 同构；`Core/*.cs` = **41 个文件**）：
`d:/4/_tmp_view/d1_census.py`。

### 4·1 代码里（剥注释后）合计 **85 处**

| 类型 | 处数 | 分布 | 白名单 |
|---|---|---|---|
| `UnityEngine.Debug` | **80** | **20 个文件**（`ActionPump.cs` 18 · `RuleCore.cs` 13 · `TutorialScript.cs` 13 · `CardDef.cs` 9 · `EffectResolver.cs` 7 · `BoardSlots.cs` 3 · `ActionQueue.cs` / `BattleContext.cs` / `ConditionEval.cs` 各 2 · 另 11 个文件各 1） | ✅ 允许 |
| `UnityEngine.Vector3` | **3** | `GameplayVariables.cs:650 / 651 / 660` | ❌ 本次红 |
| `UnityEngine.Vector2` | **2** | `GameplayVariables.cs:682 / 692` | ❌ 本次红 |

⇒ **除这 5 处外，`Core/` 的代码里没有任何别的非白名单 `UnityEngine.*` 类型**。`UnityEngine.Random` 在**代码里一次都没用到**（白名单是天花板、不是要求）。

### 4·2 只出现在**注释 / 字符串**里的（不参与编译、断言不报、也**不该**改）

| 记号 | 原文次数 | 代码次数 |
|---|---|---|
| `UnityEngine.Random` | 14 | **0** |
| `UnityEngine.Object` | 5 | **0** |
| `UnityEngine.Object__op_Equality` | 1 | **0** |
| `UnityEngine.Rendering` | 1 | **0** |
| `UnityEngine.Vector3` | 4 | **3**（多出来的 1 次是 `:634` 讲解里那句「`DAT_…` 就是 `UnityEngine.Vector3` 的 Il2CppClass」） |

### 4·3 顺带复核禁用词那一条（同一轮里它是 ✓）

`ruleengine.log:193354` 是 ✓。我的独立复核（`d:/4/_tmp_view/d1_banned.py`）：6 条禁用词在
`ForcedAttackRefund.cs` / `OffensiveCardData.cs` / `RelatedCardData.cs` / `TalentCardData.cs` / `TutorialScript.cs` 里
**原文共 12 次、剥注释后 0 次** ⇒ 全是讲解文字，没有真引用。

---

## 五 没查清 / 边界

1. **`Stub.cs` 撤回两个 struct 这件事没实测**（只读代理不编译、不跑 dotnet）：预期是 `Core` 侧 0 引用、`Data/*` 侧也不用，
   但**撤回后必须跑一次 `ruleprobe check`** 才能算数。
2. **`(B)` 的「探针构建会报 `CS0234`」是别人实测的转述**（`B9_S0EFFECT.md:131-136`），我本人没复现构建。
3. `_parts/_merged_B9_*.tsv` 有一批**读不了（Permission denied）**，所以「账锚只有 607-623 与 682 两条」的统计
   只覆盖了可读的那 9 个文件 + 主表；⛔ 不排除只读锁住的那批里还有别的 `GameplayVariables.cs:<行>` 锚点。
