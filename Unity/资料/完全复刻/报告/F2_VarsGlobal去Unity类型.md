# F2 · `VarsGlobal` 去 Unity 值类型（执行 A-1）· 报告

日期：2026-10-27 · 执行代理 `F2PURE` · 判据 = `资料/完全复刻/报告/D1_ruleengine两条红.md` §二 (A-1)

---

## 一 结论

**做完，四条必跑全绿**（Unity 宿主一条没跑，按红线）。`RuleEngine/Core/` 的代码里现在只剩 `UnityEngine.Debug`
（80 处 / 20 个文件，与 D1 §4·1 逐格相同，那 5 处 `Vector2/Vector3` 已归零）⇒ `RuleEngineTest.cs:29646`
那条架构断言**应当转绿**（本波禁跑 Unity，由调度台在同步点用 `RuleEngineTest.Run` 验）。

| 必跑项 | 结果 |
|---|---|
| `typecheck.sh`（`TMPDIR=/tmp/wf_f2`） | **运行时 0 / 编辑器 0** |
| `ruleprobe.sh check` | **rc=0**（全池 1121 张 · 解析差异 0 行 · 哨兵失败 0 · 输出指纹 `c9a64798…` 与钉住一致） |
| `ruleprobe.sh play check` | **rc=0**（基线 `e7f1de8f…` 与钉住一致 ⇒ 结算层零变化，符合预期） |
| `check_ledger_points.py --all-A`（未加 `--fix`） | `GameplayVariables.cs` **零行**出现在漂移/失败明细里；该文件行数 **695 → 695 不变** |

⚠️ 全局闸退出码是 1（`合计：A 3802 · 通过 3625（告警 181）· 漂移 52 · 失败 125`），
但**没有一条落在本次动的三个文件上**（理由见 §五·1）—— 那是本批其他人 / 之前批次留下的，不是我引入的。

---

## 二 证据（文件:行号）

### 2·1 五处「同格改」（`Assets/RuleEngine/Core/GameplayVariables.cs`，**零行数变化** 695 → 695）

| 行 | 改前 | 改后 |
|---|---|---|
| `:650` | `public static UnityEngine.Vector3 PlayedCardDisplayPosition(` | `public static WorldPoint PlayedCardDisplayPosition(` |
| `:651` | `UnityEngine.Vector3 handAnchorWorld, bool offensive, bool smallScreen)` | `WorldPoint handAnchorWorld, bool offensive, bool smallScreen)` |
| `:660` | `return new UnityEngine.Vector3(handAnchorWorld.x + x, handAnchorWorld.y - y, handAnchorWorld.z);` | `return new WorldPoint(handAnchorWorld.x + x, handAnchorWorld.y - y, handAnchorWorld.z);` |
| `:682` | `public static UnityEngine.Vector2 PlayedCardDisplayOffsetPx(` | `public static PxOffset PlayedCardDisplayOffsetPx(` |
| `:692` | `return new UnityEngine.Vector2(x * k, y * k);` | `return new PxOffset(x * k, y * k);` |

- **算术逐字未动**：`:653-656` / `:685-690` 的 `x` / `y` / `k` 三行一个字符没改；只有「构造哪个类型」这一格换了。
- **账锚 `:682` 落在原位**（现读复验：`:682` 仍是 `public static PxOffset PlayedCardDisplayOffsetPx(`）；
  另一条账锚 `:607-623`（8 个纯 float 常量块）现读复验逐行未动。

### 2·2 新文件 `Assets/RuleEngine/Core/VarsGlobalTypes.cs`（35 行 · LF · 带 `.meta` guid `0dcf2f71…`）

`namespace RuleEngine { public struct PxOffset { float x, y; ctor(x,y) } public struct WorldPoint { float x, y, z; ctor(x,y,z) } }`
—— 只有**字段 + 构造**，没有任何运算符 / 换算（⛔ 按铁律 18 ⛔②「命名/结构不同但语义一致」**未**打 `🔶 自己设计`）。
`ruleprobe.csproj` 的 `<Compile Include="../../MyGame/Assets/RuleEngine/Core/*.cs" />` 通配自动带上（现读 Core 42 个 `.cs`，原 41）。

### 2·3 三个调用点（各改一行声明 `Vector2 x = …` → `var x = …`，`.x` / `.y` 用法原样）

| 文件:行 | 现读 |
|---|---|
| `CardPresentation/Battle/BattleDriver.cs:35557` | `var offPx = VarsGlobal.PlayedCardDisplayOffsetPx(false, SmallScreenUI.Enabled, 1080f);`（下一行只用 `.x` / `.y`） |
| `CardPresentation/Editor/BattleScene.cs:48950` | `var wantOffDesk = VarsGlobal.PlayedCardDisplayOffsetPx(false, false, 1080f);`（`.x` / `.y`） |
| `CardPresentation/Editor/BattleScene.cs:48982` | `var wantOffSmall = VarsGlobal.PlayedCardDisplayOffsetPx(false, true, 1080f);`（`.x`） |

- 三处**都没有**对返回值做向量运算 / 隐式转换 / 与 `Vector2` 比较（改前逐处读过）⇒ 不需要给新结构加任何运算符。
- ⚠️ 同文件里 `BattleScene.cs:48961`（`Vector2 small;`）与 `:48992`（`static Vector2 Measure(...)`）**未动** ——
  它们装的是「量出来的落点像素」，跟 `VarsGlobal` 无关，属表现层自己的 Unity 向量。
- `PlayedCardDisplayPosition`（世界版）**调用点仍为 0**（全仓 `grep` 只有定义 + 注释）⇒ 改签名零成本。

### 2·4 桩撤回（`工具/ruleprobe/Stub.cs`，原 `:59-80` 两个 struct 已删，改为 5 行说明 `:59-63`）

撤回后 **`ruleprobe.sh check` rc=0**（该脚本每次先 `dotnet build`、构建非 0 就出声并停 ⇒ 这个 0 是真编过+真跑过的，
不是拿旧 dll 的假绿），`play check` 同样 rc=0 ⇒ **没有别处需要 `Vector2`/`Vector3` 桩**。

### 2·5 断言侧我自己的复核（不跑 Unity 也能查的那一半）

按 `RuleEngineTest.ScanCoreText` 的同构逻辑（剥 `//` · `/* */` · 字符串/字符字面量）现扫 `Core/*.cs`（**42 个文件**）：
`UnityEngine.Debug` **80 处 / 20 个文件**，**再无其它类型**（改前是 80 + `Vector3` 3 + `Vector2` 2 = 85）。
与 D1 §4·1 那张表逐格相同。另核 `:29641` 那条 `CountOf(r.CodeAll, "namespace RuleEngine") >= files.Length`：
新文件带 `namespace RuleEngine` ⇒ 不会把这条前提断言带红。

---

## 三 没查清的部分

1. **`RuleEngineTest.Run` 没跑**（红线：本波禁跑 Unity 宿主）⇒ 「那条断言转绿」是**按判据推出来的**，不是实测。
   复核办法：调度台同步点跑 `RuleEngineTest.Run`，看 `9440/9441` 是否变 `9441/9441`（或失败 0）。
2. **`check_ledger_points` 不评估「落点括号里嵌的第二个 `文件:行`」**：主表 `战斗界面_HUD.tsv:756` 那行
   的**落点列第一个**是 `BattleDriver.cs:25877`，`GameplayVariables.cs:682` 只出现在它后面的括号里 ⇒
   这两条账锚**根本不在机检范围内**（所以「没有新漂移」有一半是空转的绿灯）。
   我另用两条硬判据补上：① 行数 695 → 695（任何插行都不可能）② 现读 `:682` / `:607-623` 内容逐行未变。
3. 只读诊断 §五·3 说的「`_parts/_merged_B9_*.tsv` 有一批读不了（Permission denied）」我没有复核；
   若那批里还有 `GameplayVariables.cs:<行>` 锚，本波**改法是同格改**（零行数变化）⇒ 不受影响。

---

## 四 改动清单

| 文件 | 动作 | 行数 |
|---|---|---|
| `MyGame/Assets/RuleEngine/Core/VarsGlobalTypes.cs` | **新建**（+ 同名 `.meta`，guid `0dcf2f7162334128b4161919e2192de6`） | 新增 35 行 |
| `MyGame/Assets/RuleEngine/Core/GameplayVariables.cs` | 5 行**同格**改（`:650` `:651` `:660` `:682` `:692`） | **695 → 695（零变化）** |
| `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | `:35557` 声明改 `var` | 行数不变（35682） |
| `MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | `:48950` / `:48982` 声明改 `var` | 行数不变（49018） |
| `工具/ruleprobe/Stub.cs` | 删 `Vector2`/`Vector3` 两个 struct（原 `:59-80`），留一段「⛔ 别再补回来」的说明 | 75 → 64 行 |

**行尾**：改完逐个按二进制数过 —— `GameplayVariables.cs` / `VarsGlobalTypes.cs` / `Stub.cs` 纯 LF（CRLF=0 · 裸 CR=0），
`BattleDriver.cs`（35682/35682）与 `BattleScene.cs`（49018/49018）仍是**全 CRLF**、无一裸 CR。**没翻行尾**。

⛔ 未动：`RuleEngineTest.cs`（白名单一个字没动）、`PlayProbe.cs`、账主表、基线、别人的报告、`*.unity`、`ProjectSettings`。

---

## 五 顺手发现

1. **`--all-A` 的 `漂移 52 / 失败 125` 与本次无关，但里面有 11 条指着我这两个 `CardPresentation` 文件** ——
   全部是**文本层**问题，不是行号漂移：10 条「歧义（同名声明有 2~4 处，账里那行不在其中，
   ⛔ 不自动改，请人工确认）」（例：`ViewAt` 4 处 / `SetHint` 2 处 / `SetVis` 2 处 / `PlayReplay` 2 处 /
   `RandomOffsetAmp` 4 处 / `ResolveAnimAnchor` 3 处）、2 条失败是
   「落点里没有 `文件:行`」（`BattleDriver.cs`（`ptIsBoardCenter` 分支 `:23315-…`））和
   「`)` 后接的是语句」（`BattleDriver.cs:5918` 的 `_chatPopup.Show();`）。
   ⇒ **归调度台另派人工复核**（要改的是**账的落点指法**，不是代码）。
2. **`Stub.cs` 现在没有任何向量类型**，而 `ruleprobe.csproj` 末尾那段注释
   （「拿掉 `Stub.cs` ⇒ 18 个 `CS0103`，落在 6 个文件」）**仍然成立**（`Debug` 还在 20 个文件里用）⇒ 那段不用改。
3. **`WorldPoint` / `PxOffset` 这两个名字全仓此前 0 命中**（改前 `grep -w` 确认），不会与既有类型重名。
4. `BattleScene.cs:48901` 那条注释（「🔴 别拿 `VarsGlobal.PlayedCardDisplayPosition`（世界版）算期望 ——
   单位差 7.28 倍」）**仍然正确**、也不需要跟着改：它讲的是「别用世界版算 px 期望」，与返回类型无关。

---

## 交接给下一个代理

本件已做完，无半成品。若要在 Unity 侧收口：在同步点跑 `RuleEngineTest.Run`，
预期 `TestCoreLayerPurity` 的两条 `UnityEngine` 判据都绿（`Banned` 本来就是绿，`Unity` 那条由红转绿）。
若意外仍红，先按 `:29646` 的 `HitDetail(r.Unity)` 打出来的 `文件:行` 定位 —— 那说明本波之后又有人往 `Core/` 写了 Unity 值类型。
