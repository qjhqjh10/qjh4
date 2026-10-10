# G0 · `BattleScene.Run` 那 6 条红（F4 新加的反向组断言）

> 日期 2026-10-10 · 执行代理 G0 · 白名单只有一个文件：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`
> 日志来源 `d:/4/_tmp_view/battle.log`（`:47157` ⑥ + `:47181/47205/47229/47253/47277` ⑤×5）

---

## 1. 一句话结论

**6 条红全部是 (α) 断言错，一条实现缺陷都没有。**
错在期望值本身：那 6 条除了断「`AutoSizing == false`」之外，还断「`min/max == 0`」——
而 `min/max` 实测恒 **18/72**，那是 **TMP 自己的 `Awake → LoadDefaultSettings()` 在新建对象时写进去的出厂值**，
**不是我们给的缺省**（我们的代码里写 `fontSizeMin/Max` 的地方**全仓只有一处**，就是 `SetAutoFitBox`，那条路这 5 颗 + 探针都没走）。
处置 = **删掉那一格、把「从没被 `SetAutoFitBox` 碰过」这个性质改成「与同路对照逐位相等」**（⑤ 五条），
并给 ⑥ 补上 `WrappingMode == 0`（**净变强**，见 §4）。**其余 4 格断言逐条不变**（它们本来就通过）。
类型检查 **0 错**；`git diff --numstat` = **36 / 14**，行尾仍是纯 CRLF。

---

## 2. 🔑 `18/72` 是哪来的（`文件:行号`，全链现读）

### 2·1 先排除「我们给的」

| 我们这边会不会写 `fontSizeMin/Max` | 出处 | 结论 |
|---|---|---|
| `Label.Create` | `Battle/Label.cs:98-115` | 只写 `scale/color/anchor` + `BuildTmp` + `SetText` ⇒ **不碰** |
| `Label.BuildTmp` | `Battle/Label.cs:1004-1009` | 只写 `textWrappingMode = NoWrap`（`:1008`）⇒ **不碰** |
| `Label.SetTextTmp` / `SetText` / `SetSizes` / `SetFontSize` | `Label.cs:1011-1017` · `:156-171` · `:946-963` · `:618-629` | 只写 `text` / `fontSize` / 内部 `_capHeight/_glyphHeight` ⇒ **不碰** |
| `TmpFont.NewText`（全工程建 TMP 的唯一一处） | `Core/TmpFont.cs:184-213`（`NoWrap` 在 `:203`） | 写 `font`/`fontSize`/`color`/`alignment`/`spriteAsset`/`textWrappingMode`/`text` ⇒ **不碰** |

🔑 **硬判据（一条 grep，可复查）**：`grep -rln "\.fontSizeMin =\|\.fontSizeMax =" --include=*.cs CardPresentation/`
→ **只返回 `CardPresentation/Battle/Label.cs` 一个文件**，而那里只有 `SetAutoFitBox` 里的两行
（`Label.cs:907` `_tmp.fontSizeMax = cur * (maxPx / nomPx);` · `:908` `_tmp.fontSizeMin = cur * (minPx / nomPx);`）。
⇒ **「我们没写过」= 已证**，不是推断。

### 2·2 那就是 TMP 出厂值 —— 三跳，逐跳有行号

1. `TMP_Text.cs:470`：`protected float m_fontSize = -99;` ← **新建对象的哨兵**。
   （⚠️ 同处 `:538` / `:550`，`m_fontSizeMin` / `m_fontSizeMax` 的 **C# 字段初始值是 `0`** —— 这正是「断 `== 0` 看着很合理」的来源，见 §3）
2. `TextMeshPro.cs:549` `protected override void Awake()` → `:607` **`LoadDefaultSettings();`**
   （`TMP_Text.cs:5966` 那个 `protected void LoadDefaultSettings()`，`:5968` 的闸就是 `m_fontSize == -99 || m_isWaitingOnResourceLoad`）
3. `TMP_Text.cs:5998-6000`：
   ```csharp
   m_fontSize = m_fontSizeBase = TMP_Settings.defaultFontSize;                    // :5998
   m_fontSizeMin = m_fontSize * TMP_Settings.defaultTextAutoSizingMinRatio;       // :5999
   m_fontSizeMax = m_fontSize * TMP_Settings.defaultTextAutoSizingMaxRatio;       // :6000
   ```

代入实测值（`Assets/TextMesh Pro/Resources/TMP Settings.asset:29-31`，现读）：

| 字段 | 值 |
|---|---|
| `m_defaultFontSize` | **36** |
| `m_defaultAutoSizeMinRatio` | **0.5** |
| `m_defaultAutoSizeMaxRatio` | **2** |

⇒ **36 × 0.5 = 18**、**36 × 2 = 72** —— 与日志的 `min/max = 18/72` **逐位吻合**。
（同一份 `LoadDefaultSettings` 还把 `sizeDelta` 从出厂 `(100,100)` 写成
`defaultTextMeshProTextContainerSize`（`:5980-5981`，`TMP Settings.asset:32` = `{x: 20, y: 5}`）——
**这正是那两条「框宽 `20.0000` = 新建一条 Label 的 `20.0000`」为什么成立**，同一次赋值、同一个证据链。）

### 2·3 这一对还是**死值**

`m_enableAutoSizing` 为 `false` 时 TMP 一个字节都不采它（`TMP_Text.cs:3684` 那族
`float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;`）——
**本工程 `Label.AutoSizing` 的 doc 早就写着这条结论**：

> `Battle/Label.cs:454-458`：「读 `FontSizeMin/Max` 分不出来（TMP 出厂就带一对默认值 `m_fontSizeMin/Max`，
> 没开自适应时它们只是**死值**，见 `FilterPanelModel` 那条「`min18/max72` 是不生效的残留值」）」

🔴 **也就是说：这条错期望值，在它自己断言的那个类的 doc 里早就被否掉了**（见 §7 顺手发现 ①）。

### 2·4 第二问（`AutoSizing == false` 那半对不对）—— **对，而且有鉴别力**

- `Label.cs` 里 `enableAutoSizing = true` 的**唯一**写点 = `SetAutoFitBox`（`:903` 先 `false`、`:909` 再 `true`）。
- `Hud` 调它的条件 = `BattleDriver.cs:13249`：`if (l != null && boxWpx > 0f && boxHpx > 0f && autoMaxPx > 0f)`，
  而这 5 颗走的是形参默认值（`BattleDriver.cs:13243-13245`：`boxWpx = 0f, boxHpx = 0f, autoMinPx = 0f, autoMaxPx = 0f, …`）。
- ⇒ `SetAutoFitBox` **一次都没被调** ⇒ `!AutoSizing` 是**真命题、且是真判据**；
  它反过来足以挡住「把框/四格接到这些件上」那类错法。**「没有自适应」这个性质已经被 `AutoSizing` 那一格断住了**，
  `min/max` 那一格是**多余且错的**（`F4` 自己那两格也是这么并列写的）。

---

## 3. 逐处改动

改动**只在 `Editor/BattleScene.cs` 的两处**（`F4` 新加的那一组之内），行尾纯 CRLF 未翻。

### 改动 A —— ⑥（`Hud` 之外那条路）

- **改前**：`BattleScene.cs:11928-11933`
  ```csharp
  Check(!hudProbe.AutoSizing
        && hudProbe.FontSizeMin == 0f && hudProbe.FontSizeMax == 0f, …);
  ```
- **改后**：`BattleScene.cs:11925-11953`（新加一条 20 行的**出处注**：TMP 那条三跳链 + 那三个设置值 + 「是死值」）
  ```csharp
  float defSd = hudProbeTmp.rectTransform.sizeDelta.x;
  //  🔴 …（18/72 的完整出处，见 §2）
  float defMin = hudProbe.FontSizeMin, defMax = hudProbe.FontSizeMax;   // ← 新增：同路对照那一对
  Check(!hudProbe.AutoSizing && hudProbe.WrappingMode == 0, …);
  ```
  消息里**仍把 `min/max` 印出来**（诊断），但明确写「= TMP 出厂的死值，⛔ 不作断言」。
- **为什么旧形态是 (α)**：期望值 0 是从 `TMP_Text.cs:538/550` 的**字段初始值**推的，
  漏了 `Awake → LoadDefaultSettings()` 那一跳（它在**构造那一刻**就把 0 改成了 18/72）。
  ⚠️ `F4` **自己已经把这个疑点写进注释了**（「若它红了先怀疑 `Awake`/`TMP_Settings` 载入时机，别当实现缺陷」）——
  **它的怀疑是对的**，只是期望值先写下了、没验。⇒ 归因 = 断言错，不是实现缺陷。

### 改动 B —— ⑤ 那 5 颗（逐颗一条）

- **改前**：`BattleScene.cs:11945-11951`
  ```csharp
  Check(!lb0.AutoSizing && lb0.FontSizeMin == 0f && lb0.FontSizeMax == 0f
        && lb0.WrappingMode == 0
        && Mathf.Abs(tm0.rectTransform.sizeDelta.x - defSd) <= 1e-4f, …);
  ```
- **改后**：`BattleScene.cs:11962-11973`
  ```csharp
  Check(!lb0.AutoSizing && lb0.WrappingMode == 0
        && Mathf.Abs(tm0.rectTransform.sizeDelta.x - defSd) <= 1e-4f
        && Mathf.Abs(lb0.FontSizeMin - defMin) <= 1e-4f      // ← 新增：同路对照
        && Mathf.Abs(lb0.FontSizeMax - defMax) <= 1e-4f, …);
  ```
- **为什么旧形态是 (α)**：同改动 A。**并且它把 `min/max` 当「框之一部分」来断**——
  可 `min/max` 是**只有给框才会被写**的那一对（唯一写点 `Label.cs:907-908`），
  所以正确的判据不是「它等于 0」，而是**「它等于同一条路、同样没给框的那条 Label 的那一对」**：
  同一个 `defSd` 的思路（`F4` 自己那条已经被认可：「那个对照已经在用、且过了」）。

**净改动**：`+36 / -14` 行；**只动了这 6 条所在的两次 `Check(` 调用 + 它们上面的注释**，
`F4` 另外那 169 条一个字节没碰。

---

## 4. 改后每条还挡得住什么错法（逐条：错法 → 红不红）

先给一条**可预判为绿**的依据：新加的对照那一格**两边都印在日志里** ——
`⑥` 那条印的是**探针**的 `min/max = 18/72`，`⑤` 那 5 条印的是**各自**的 `min/max = 18/72`
⇒ `18 == 18` · `72 == 72`。`⑤` 的另外 4 格（`AutoSizing=False` · `折行档 0` · `框宽 20.0000 = 20.0000`）
日志里也都是绿值 ⇒ **这 6 条的唯一假红就是被删掉的那一格**，改完即绿。

### ④ ⑥（`Label.Create` 这条路 / 探针）

| 错法 | 红不红 | 靠哪一格 |
|---|---|---|
| 共用件里偷偷调 `SetAutoFitBox`（= 把 `Hud` 那套框挪进共用件，`A1213①` 的反面） | **红** | `AutoSizing`（`WrappingMode` 也一起红） |
| 共用件里调 `SetWrapWidth`（只加折行宽、不开自适应） | **红** | `WrappingMode == 0` ← **旧形态抓不到这条，现在抓得到（净变强）** |
| `TmpFont.NewText` 改成不写 `NoWrap`（`TmpFont.cs:203` 被删） | **红** | `WrappingMode == 0`（净新增覆盖） |
| 有人只手写 `fontSizeMin/Max`、**不开自适应** | **不红** | ⛔ **这是删掉那一格唯一的净值损失**（但它是死值、TMP 不采，见 §2·3；如实登记，不粉饰） |
| TMP 换版本 / `TMP Settings.asset` 改 `defaultFontSize` 或两个 ratio | **不再假红**（旧形态会红） | —— 这是**修掉的假红** |

### ⑤ 那 5 颗（`TurnLabel` / `TurnClock` / `HintLabel` / `ResultLabel` / `HandLabel`）

| 错法 | 红不红 | 靠哪一格 |
|---|---|---|
| **`Hud` 那 6 个形参被填上非零默认值**（这一组要挡的**首要**错法，`BattleDriver.cs:13243-13245`） | **红 ×4** | `AutoSizing` + `WrappingMode` + `sizeDelta` + `min/max 对照` **四格同时红** |
| 有人给这 5 颗**单独**补 `SetAutoFitBox`（「主动制造偏离」那条红线） | **红 ×4** | 同上 |
| 有人给这 5 颗单独调 `SetWrapWidth`（只折行） | **红 ×2** | `WrappingMode` + `sizeDelta` |
| 补框之后又 `SetWrappingMode(0)` 把折行档还原（想瞒过折行那一格） | **仍红 ×3** | `AutoSizing` + `sizeDelta` + `min/max 对照`（⇒ 「把折行档还原」这种**半遮**挡不住） |
| 有人只手写某一颗的 `min/max`（死值） | **红** | **新增的对照那一格** ← 这一格在 ⑤ 上**没有丢覆盖** |
| 这 5 颗里某一颗被改名/删掉 | **红** | 上面那条「（前提）`{n}` 在、且走 TMP 后端」 |
| TMP 换版本 / 设置资产改比例 | **不再假红** | —— 修掉的假红 |

### 「不自证」这一面（本组**不**是自证，理由三条）

1. **对照对象是另一条 Label**（同一条建造路径 `Label.Create`、从没给框），不是常量、不是被测对象自己。
2. **不是同义反复**：给框那条路会把这一对写成 `cur × minPx/nomPx`（`Label.cs:907-908`，例如 min≈0.78 / max≈3.9），
   **必然 ≠ 18/72** ⇒ 「改坏 ⇒ 值真的会动」。
3. **✅ 正向那一批是对照组**：同一批断言里那 16 颗走 `CheckHudBox`（`:438-441` 断的正是 `FontSizeMin/Max` **真被写成了原版窗口**）
   ——**「给了框的必须写出原版那一对」与「没给框的必须没被动过」是两支**，任何一支单独改坏，另一支的语义就立不住。

⚠️ **仍有一处薄口（不在本件 6 条之内，如实登记、未动）**：若有人把 `SetAutoFitBox` 里
`_tmp.enableAutoSizing = true;`（`Label.cs:909`）**整句删掉**，那么「给框」与「不给框」都不开自适应
⇒ 本组 6 条会**一起变绿**（而上面那 16 颗多数仍绿：不给自适应时 `fontSize = baseCur`，收敛值恰好落在 `[min,max]` 窗口里）。
堵法 = 在已有的「给了框」那颗探针上加**一条**正向断言（`posProbe.SetAutoFitBox(...)` 之后断 `posProbe.AutoSizing == true`，
位置在 `BattleScene.cs:11985-11993` 那一节）—— **那属于 `F4` 的另一组，本件按简报「只改这 6 条」没动**，交给调度台裁。

---

## 5. 验证

| 项 | 读数 |
|---|---|
| 类型检查（`TMPDIR=/tmp/wf_g0 bash d:/4/Unity/工具/typecheck.sh`，改完立刻跑，共两次） | `--- 运行时程序集 --- 运行时错误数: 0` · `--- 编辑器程序集 --- 编辑器错误数: 0` |
| `git diff --numstat -- …/Editor/BattleScene.cs` | `36	14`（= 只动了那两处；没有整篇重写） |
| 行尾 | `CRLF 20752 / LF 20752` ⇒ **纯 CRLF、未被翻**（用 `Edit` 工具改，⛔ 未用 `sed -i`） |
| 同族错期望值是否还有别处 | `grep -rn "FontSizeMin == 0\|FontSizeMax == 0" Assets/**/*.cs` = **0 处**（本件是全仓唯一一处）⇒ **没有潜伏的同类红** |
| Unity 自检 | **未跑**（简报禁止；`BattleScene.Run` 由调度台在同步点跑） |

---

## 6. 没查清 / 停手的部分

1. **「改完真的绿」我没有亲验** —— 只做了**静态预判**（§4 开头那条：新加的对照两边都印在日志里、其余 4 格日志里都是绿值）。
   真正跑 `BattleScene.Run` 由调度台在同步点做。
2. **`18/72` 为什么在 `Label.Create` 的探针与那 5 颗上完全相同** —— 我只证到「同一次 `LoadDefaultSettings` 的产物、同一个 `TMP Settings.asset`」，
   **没有**去验「`TMP_Settings.instance` 在自检过程中会不会被换过」（那会推翻「两次同值」）。
   今天两份读数的实证是同值，且 `TMP_Settings.instance` 是单例缓存 ⇒ 不认为有事，**但这一条我没有独立判据**。
3. §4 那条**薄口**（删掉 `enableAutoSizing = true` ⇒ 本组 6 条一起假绿）**没堵** —— 简报限定「只改这 6 条」，其堵法落在另一组，**未动、已登记**。

---

## 7. 🔑 顺手发现

1. **这条错期望值，本仓早就写着答案是错的、而且就在同一个类里**
   —— `Battle/Label.cs:454-458`（`AutoSizing` 的 doc）明说「TMP 出厂就带一对默认值 `m_fontSizeMin/Max`，
   没开自适应时它们只是**死值**」，连 `min18/max72` 这两个数都点出来了。
   ⇒ 教训（与铁律 10 同族）：**给一个类编断言前，先读那个类自己的 doc** —— 这一条省下的是整整一轮红。
2. **`F4` 的注释里那句出处是准的**：`TMP_Text.cs:5980` 确实是
   `if (m_rectTransform.sizeDelta == new Vector2(100, 100)) m_rectTransform.sizeDelta = TMP_Settings.defaultTextMeshProTextContainerSize;`
   （`:5980-5981`）。只是它的**宿主方法名**叫 `LoadDefaultSettings()`（`TMP_Text.cs:5966`），由 `TextMeshPro.cs:549` 的 `Awake` 在 `:607` 调 ——
   **链上多一跳**，而 `F4` 那句只写「`Awake` 会把出厂的 `(100,100)` 换成它」。我这次把整条链写进了新注释（§2·2），**没有改动 `F4` 那句原文**（它不算错，只是不够细）。
3. **「我们写没写过某个 TMP 字段」有一条一跳可查的判据**：`grep -rln "\.fontSizeMin =\|\.fontSizeMax ="` ⇒ 只 1 个文件。
   建议以后凡是要断「TMP 某字段还是出厂值」，先跑这条 grep 定位**唯一写点**，再决定期望值 —— 比读字段初始值可靠（字段初始值会被 `Awake` 改掉，这次就是这么栽的）。
4. **`sizeDelta = 20` 与 `min/max = 18/72` 是同一个证据链**（同一次 `LoadDefaultSettings`）：前者是 `TMP Settings.asset:32`
   的容器尺寸，后者是 `:29-31` 的字号三件套。下次看到 `20` 或 `18/72` 其中任一，可以拿另一条反向佐证「`Awake` 跑过了」。
5. **`CheckHudBox` 那 16 颗的正向断言与这 6 颗的反向断言天然互为对照组**（§4 最后那一条），
   但**它们不覆盖 `enableAutoSizing` 这个开关本身**（薄口）。
   建议的堵法只需**一行**，见 §4 末尾；要不要加、加到哪一组，请调度台裁。
