# A77 ⑭ · 卡组线读数族四条（`UiHasQuad` / `UiTextureName` / `UiQuadCount` / `UiQueueOf`）

- 日期：2026-10-07 · 波 8 · 件 ⑭（全权限执行代理）
- 白名单内改动：`Deck/DeckRuntime.cs` · `Editor/DeckScene.cs`（+ 本报告）
- 判据：`资料/待办判据_审查发现_1005.md:90`（§⑭ 的「🔴 新待办（要做，单开一件）」）· 派单 §①
- 参照实现（**未动**）：`UiQuadActive`（现读 `Deck/DeckRuntime.cs:2304`）· `UiNodeRect`（现读 `:2379`）

---

## 一、结论

**四条全部改了**（没有一条是「本来就对」，也没有「没查清」）。四条各自的**查找链**改前/改后见下表；
**优先序一条都没翻**（`Lookup` / 直接子件的先后原样保留）⇒ 既有 key 找到的还是同一个节点、既有断言一个数都不变。

| 读数 | 现读行号 | 改前查找链 | 改后查找链 | 状态 |
|---|---|---|---|---|
| `UiHasQuad` | `Deck/DeckRuntime.cs:2262` | `Lookup` → `Root.Find`（**两步**，缺第三步） | `Lookup` → `Root.Find` → **`FindDeep`** | ✅ 改了 |
| `UiTextureName` | `:2431` | `Lookup` → `Root.Find` → `GetComponentInChildren`（**找名字只有两步**） | `Lookup` → `Root.Find` → **`FindDeep`** → `GetComponentInChildren` | ✅ 改了 |
| `UiQuadCount` | `:2457` | `Root.Find` → `Lookup` 兜底（**找名字两步**；且这两步的**优先序与另三条相反** —— 树优先） | `Root.Find` → **`FindDeep`** → `Lookup` 兜底 | ✅ 改了 |
| `UiQueueOf` | `:2478` | `Lookup` → `Root.Find` → `GetComponentInChildren` | `Lookup` → `Root.Find` → **`FindDeep`** → `GetComponentInChildren` | ✅ 改了 |

- **静默形态（本组要害）**：把「容器下的件」（挂在 `flt_drawer` / `cosmoflt_drawer` 底下、**不进 `_named`** 的九宫格/子树，
  如 `flt_input`）拿去问这四条，改前**不报错、不发警告**，只答 `false` / `null` / `0` / `−1`（`−1` 还会被「层序比大小」
  那种断言读成「队列最小」）。
- **`FindDeep` 不是新写的** —— 用它的是同文件现成的那份（`Deck/DeckRuntime.cs:2363`，`static GameObject FindDeep`）。
- **没引入新的静默**：三步都找不到时**照旧**答 `false` / `null` / `0` / `−1`（这是这四条**既有**的契约：它们只服务自检，
  自检那条断言会把**实得值**打出来），本批只把「以前找不到」变成「找得到」。四条都照参照实现的既有形状写，
  并在注释里写明「为什么」（`transform.Find` 只认直接子件 ⇒ 容器下的件看不见），出处标了判据文件 §⑭。

---

## 二、证据

### 2·1 改前 / 改后查找链逐条对照（`d:/4/Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs`）

**① `UiHasQuad`（改前 `:2254`，改后 `:2262`）**

```csharp
// 改前（两步）：
return Lookup(key) != null || (Root != null && Root.Find(key) != null);

// 改后（三步）：
return Lookup(key) != null
    || (Root != null && (Root.Find(key) != null || FindDeep(Root, key) != null));
```

**② `UiTextureName`（改前 `:2417`，改后 `:2431`）**

```csharp
// 改前：            var go = Root.Find(key);
//                   if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
// 改后（只加这一段，顺序不动）：
var go = Root.Find(key);
if (go == null)
{
    var deep = FindDeep(Root, key);
    if (deep != null) go = deep.transform;      // ⚠️ `FindDeep` 返回 `GameObject` ⇒ 要 `.transform`
}
if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
```

**③ `UiQuadCount`（改前 `:2429`，改后 `:2457`）** —— 同 ②，深查找**插在 `Root.Find` 之后、`Lookup` 兜底之前**：

```csharp
if (Root != null)
{
    var go = Root.Find(key);
    if (go == null)
    {
        var deep = FindDeep(Root, key);
        if (deep != null) go = deep.transform;
    }
    if (go != null) return go.GetComponentsInChildren<ImageQuad>(true).Length;
}
return Lookup(key) != null ? 1 : 0;
```

> ⚠️ **本条为什么没有照 `UiQuadActive`/`UiNodeRect` 那样把 `Lookup` 提到最前**（**故意的**，注释里也写了）：
> 树那一支答的是「**这棵子树里有几块**」（九宫格 9 / 平铺 N），而 `_named` 那一支答的是**硬编码 1** —— 两支答的
> 不是同一个问题。既有断言（`name_bg >= 9` · `hdr_sep == 3` · `tab_hi0 == 9` · `row_*` 那几条）全是在**树优先**下量的。
> 深查找插在 `Root.Find` 之后 ⇒ 树里找得到的 key 一个数都不变，只把「树里找不到、`_named` 也没有」的从 `0` 变成**真块数**。

**④ `UiQueueOf`（改前 `:2440`，改后 `:2478`）** —— 同 ②：

```csharp
var go = Root.Find(key);
if (go == null)
{
    var deep = FindDeep(Root, key);
    if (deep != null) go = deep.transform;
}
if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
```

### 2·2 新断言（`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs:1058-1100`，插在 A57 ① 那组之后）

```csharp
            // ============================================================ 🆕 2026-10-07（A77 ⑭）
            // **同族另外四条读数**：`UiHasQuad` / `UiTextureName` / `UiQuadCount` / `UiQueueOf`
            // —— 它们以前**只有两步**（`Lookup` → `Root.Find`），容器下的件（`flt_input` 这类
            // **不在 `_named` 里**、又挂在 `flt_drawer` 底下的九宫格）会被**静默**答成 false / null / 0 / −1。
            // 🔴 判据 = `资料/待办判据_审查发现_1005.md` §⑭（裁定：四条**统一到三步找法**、
            //   与 `UiNodeRect`/`UiQuadActive` 一致；⚠️ 且「**别只改一行**」—— 四条都得改）。
            // 🔴 期望值**不是**从这四条自己读回来的（⛔ 自证）：
            //   · 「它在 `Root` 那棵树里」由**本文件自己的** `FindDeep`（`GetComponentsInChildren<Transform>`）
            //     作证 —— 与 `DeckRuntime.FindDeep` 是**两份**实现；
            //   · 「它**不在** `Root` 的直接子件里」由 `_root.Find` 作证 = 缺第三步时看不见它的**原因**；
            //   · 图名 / 块数 / 队列取自**建法本身**：`FilterPanelModel.InputSprite` 那张图（同上 `name_bg`
            //     那条的 `InputFieldBackground`）· border 10 的九宫格 = **3×3 九块** · 队列 = `QFltRow`（3021）。
            //   ⚠️ 这四条读数用 `true`（含未激活）取件 ⇒ **与抽屉开没开无关**（这里是开着量的，与 A57 那组同时刻）。
            {
                var deepIn = FindDeep(_root, "flt_input");
                CheckTrue(deepIn != null && _root.Find("flt_input") == null,
                          "结构前提：`flt_input`（搜索框底）**在 Root 那棵树里、但不是直接子件**"
                          + "（它挂在容器 `flt_drawer` 底下）—— 这正是四条读数缺 `FindDeep` 时**看不见它**的原因"
                          + "（这条若红：下面四条失去判别力，先修这里）");
                CheckTrue(_rt.UiHasQuad("flt_input"),
                          "`UiHasQuad(flt_input)` = **真**（A77 ⑭：只走两步的旧写法在这里**静默答 false**）");
                Check(_rt.UiTextureName("flt_input"), "InputFieldBackground",
                      "`UiTextureName(flt_input)` = 原版那张 `InputFieldBackground`（同 `name_bg` 那条的判据）"
                      + " —— 九宫格 9 块里取第一块，即「一棵树取第一块」那一半也在");
                CheckTrue(_rt.UiQuadCount("flt_input") >= 9,
                          "`UiQuadCount(flt_input)` = **九宫格那 9 块**（border 10 ⇒ 3×3；旧写法在这里答 **0**；"
                          + $"实测 {_rt.UiQuadCount("flt_input")} 块）");
                Check(_rt.UiQueueOf("flt_input"), 3021,
                      "`UiQueueOf(flt_input)` = `QFltRow` **3021**（旧写法在这里答 **−1**；"
                      + "−1 会被「层序比大小」那种断言当成「队列最小」）");
                // 负例：**哪儿都没有**的名字 —— 四条必须照旧答「没有」（钉住兜底**不是恒真**）
                CheckTrue(!_rt.UiHasQuad("flt_input_zzz") && _rt.UiQuadCount("flt_input_zzz") == 0
                          && _rt.UiTextureName("flt_input_zzz") == null && _rt.UiQueueOf("flt_input_zzz") == -1,
                          "负例：查一个**哪儿都没有**的名字 ⇒ 四条一律 `false` / `0` / `null` / `−1`（实得 "
                          + $"{_rt.UiHasQuad("flt_input_zzz")} / {_rt.UiQuadCount("flt_input_zzz")} / "
                          + $"{_rt.UiTextureName("flt_input_zzz") ?? "<null>"} / {_rt.UiQueueOf("flt_input_zzz")}）");
                // 回归钉：`_named` 里那件（同一容器下的**单块**）加了深查找之后**仍是 1 块**。
                // ⚠️ 它钉的是**答案**（`_named` 那一支没被深查找挤掉），⛔ 钉不了「优先序」——
                //    今天没有任何 `_named` 的 key 包着 **>1** 块（`Img()` 只建单块）⇒
                //    「把 `Lookup` 提到最前」这种改法**本工程今天没有断言能分辨**（如实记在报告里）。
                Check(_rt.UiQuadCount("flt_bg"), 1,
                      "`UiQuadCount(flt_bg)` 仍是 **1**（`_named` 单块；深查找插在 `Root.Find` 之后 ⇒ 树那一支数的还是同一个节点）");
            }
```

**断言用的那把「两态尺子」= `flt_input`（搜索框底）** —— 它同时满足三件事（缺一就分辨不出两态）：

| 条件 | 出处（现读） |
|---|---|
| **不在 `_named`**（`Lookup` 一定落空 ⇒ 只能靠深查找） | 它由 `MenuDraw.Nine(...)` 建（`Deck/DeckRuntime.cs:1037`），**不是** `Img()` ⇒ 不进 `_named` |
| **不是 `Root` 的直接子件**（`Root.Find` 一定落空） | `MenuDraw.Nine(FltParent, …)`，`FltParent` = `NewDrawer("flt_drawer").Node`（`:788` / `:902`），`NewDrawer` 把它 `SetParent(Root)`（`:891-899`）⇒ 父级是**抽屉容器**；`ImageQuad.CreateNineSlice` 把根 `SetParent(parent)`（`Battle/ImageQuad.cs:335-337`） |
| **是九宫格 / 有块数可数** | border 10（`Core/FilterPanelModel.cs:106`）· 矩形 281.28×40（权威表 `flt_input`）⇒ 3×3 全部 w,h > 0（`CreateNineSlice` 只跳过 w≤0/h≤0 的块）⇒ **9 块** |

**每条期望值的出处（⛔ 都不是从这四条自己读回来的）**

| 期望值 | 出处 / 凭什么 |
|---|---|
| 节点在树里 | `_root` 自己的 `FindDeep`（`Editor/DeckScene.cs:196`，`GetComponentsInChildren<Transform>(true)`）—— 与 `DeckRuntime.FindDeep`（DFS 手写）是**两份独立实现**；且 A57 ① 已绿的那条 `UiQuadActive("flt_input")`（`:1053`）也要求该节点存在 |
| 不是直接子件 | `_root.Find("flt_input") == null`（`Transform.Find` 只认直接子件 = 本组的根因） |
| 图名 `InputFieldBackground` | 与 `:893` 那条**已绿**的 `name_bg` 断言**同源**：两者都走 `Ui(FilterPanelModel.InputSprite)`（`DeckRuntime.cs:628` 与 `:1020`），常量字面量在 `Core/FilterPanelModel.cs:105`；原版判据 = `Deck Editing Menu > … > Card Filters/…/Name FIlter/Input Field` 那颗 Image（原版图名就是 `InputFieldBackground`） |
| 块数 ≥ 9 | 建法本身：`FilterPanelModel.InputBorder = 10` + 九宫格 3×3；同族 `name_bg >= 9`（`:895`）用的是同一条判据 |
| 队列 3021 | `Deck/DeckRuntime.cs:261` 的 `const int QFltRow = 3021`（`MenuDraw.Nine(..., QFltRow, …)`，`:1037-1040` 又逐块 `SetRenderQueue`） |

### 2·3 交叉核对（确认「既有断言一个数都不变」）

- 四条读数**全工程只有 `Editor/DeckScene.cs` 一个调用方**（`grep -rn --include=*.cs` 全仓：除定义处外只命中 `Editor/DeckScene.cs`
  与三份文档；**没有任何生产代码调用它们**）。
- 现有调用 key 逐个过一遍：`hdr_sep` · `hdr_back` · `hdr_fltbtn` · `hdr_clear` · `hdr_wcbg` · `hdr_army` · `side_bg` ·
  `tab_hi0` · `tab_ic0` · `name_bg` · `name_clear` · `foot_done` · `foot_ic` · `row_0..row_10` · `row_b0` · `row_g0` ·
  `poolbar_0` · `tab_nm{i}` · `tab_ic{i}` —— 每一个都能在**前两步之内**命中（直接子件，或在 `_named` 里）
  ⇒ 新加的第三步**对它们一次都不会被执行到**。
- 面板里那批 `flt_*` / `cosmoflt_*` 是**容器下的件**（不是直挂 `Root`），但三条（`UiHasQuad`/`UiTextureName`/`UiQueueOf`）
  由 `Lookup`（`_named`）命中、`UiQuadCount` 由 `_named` 兜底答 1（`flt_bg` 真是单块 ⇒ 答案恰好对）⇒ **今天没有一条既有断言答错**
  （与判据里「今天没触发」的结论一致；⚠️ 但判据给的理由「现有调用 key 全是直挂 `Root` 的」**不准确**，见 §三·3）。

---

## 三、没查清的部分（⛔ 不猜）

1. **这四条新断言没有实跑**（红线：写手不跑 Unity）。已做的是**静态复核**（§二·3 逐 key + §2·2 逐期望值出处）+ **秒级类型检查**。
   要在同步点跑 `DeckScene.Run` 才能定「绿」。**风险最高的一条** = `UiTextureName("flt_input") == "InputFieldBackground"`
   （我靠的是「与已绿的 `name_bg` 同源」这条链 —— 若 `MenuUi` 那条加载路的命名与 `name_bg` 不同，它会红）。
2. **`UiQuadCount` 的「优先序」今天无法被断言分辨**：要有「在 `_named` 里、且那棵子树有 **>1** 块」的 key 才能分辨，
   而 `Img()` 只建单块 ⇒ **本工程今天造不出这个两态**。照实记在断言旁（不假装那条 `flt_bg == 1` 能钉优先序）。
3. **原版这一层没有判据**（不是「查不到」，是**不存在**）：这四条是我们**自检口**（只服务 `Editor/DeckScene.cs` 的断言），
   原版游戏里没有对应物 ⇒ 没有「原版参数」可比，也**没有去反编译查**（`d:/2/tools/decomp_full/` 与本件无关）。
   本件的判据是**结构事实**（「节点真的在那棵树里」）+ 建法本身（图名/块数/队列）。
4. **`flt_input` 若被挪成 `Root` 的直接子件**（将来谁把抽屉容器去掉）⇒ 那段断言的**判别力会消失**（不是变红，是变得分辨不出两态）。
   已加一条「结构前提」把它变成**会红的**前置（`:1073-1076`），但**没有**做「断言自己检测自己是否还有判别力」那种机制。

---

## 四、改动清单

| 文件 | 改了什么 | `git diff --numstat` |
|---|---|---|
| `d:/4/Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs` | 四个读数各补第三步 `FindDeep`（`UiHasQuad:2262` · `UiTextureName:2431` · `UiQuadCount:2457` · `UiQueueOf:2478`）；每条加「为什么 / 优先序为什么不动 / 找不到时照旧答什么」的注释（判据 → §⑭）。**`FindDeep` 本体、`UiQuadActive`、`UiNodeRect`、以及那一族「求交 = `MenuDraw.ClipRect`」的注释一个字都没动** | **48 / 5** |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs` | 新增一段 7 条断言（`:1058-1097`，插在 A57 ① 那组之后、`UiQuadRect("flt_bg")` 之前），覆盖四条读数 + 1 条结构前提 + 1 条负例 + 1 条回归钉 | **45 / 0** |
| `d:/4/Unity/资料/普查产出_1007/波8_A77_14_DeckRuntime四条.md` | 本报告 | （新文件） |

- **行尾**：两个 `.cs` 都是 **CRLF**，改完实测 `CRLF == LF == 行数`（`DeckRuntime.cs` 3780/3780 · `DeckScene.cs` 2303/2303）⇒ **没翻行尾**
  （`git diff --numstat` 也不是「接近文件行数」那种整篇重写）。改动全部用 Edit 工具。
- **秒级类型检查**：`TMPDIR=/tmp/wf_w8e bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**（最后一次）。
  ⚠️ 中间两次跑出过**别人在飞文件**的错（`Battle/ScenarioBlendables.cs(58,37)` CS0426 · `Editor/ShellScene.cs(1438,28)` CS1061）
  —— 都不是本件文件、随后一次自行消失；**我的两个文件在任何一次里都没有报过错**。
  ⚠️ 第一轮**抓到自己写的真错**：`FindDeep` 返回 `GameObject` 而我当时按 `Transform` 接（CS0029 ×3），当场改成 `deep.transform`。
- **没跑 Unity**（红线）：`DeckScene.Run` 由调度台在同步点跑。

### 「怎么改坏就红」（逐条，全部指向前面贴出的那 7 条）

| 改坏法 | 哪条红 | 红成什么样 |
|---|---|---|
| 删 `UiHasQuad` 的 `FindDeep`（回到两步） | `:1077` | `UiHasQuad(flt_input)` 期望 `true`、实得 `false` |
| 删 `UiTextureName` 的深查找 | `:1079` | 期望 `InputFieldBackground`、实得 `null` |
| 删 `UiQuadCount` 的深查找 | `:1082` | `>= 9` 不成立、实得 `0` |
| 删 `UiQueueOf` 的深查找 | `:1085` | 期望 `3021`、实得 `−1` |
| 把 `FindDeep` 写成「乱返一个节点」（丢了名字比对） | `:1089` 负例 | `flt_input_zzz` 会查出东西 ⇒ 「四样一律没有」不成立 |
| 把深查找改成**恒返回某个祖先**（例：只看根） | `:1073` 结构前提 + 四条全红 | 前提那条量的是 `_root.Find(...) == null` 与「树里找得到」**同时成立** |
| 把 `UiQuadCount` 的 `Lookup` 提到最前**且**改成「树里的块数」 | `:1098` | `flt_bg` 会从 1 变成…**今天仍是 1** ⇒ ⚠️ **这条分辨不出**（见 §三·2：本工程造不出两态，如实记） |
| （反向）把四条全删了 | 5 条 | 4 条读数 + 负例/回归钉之外的 4 条各红一次 |

---

## 五、顺手发现（**报上来，未改** —— 调度台分流）

1. **同族第五条同形读数：`UiLabelText`（`Deck/DeckRuntime.cs:2321`）** —— 它**只有 `Root.Find` 一步**（连 `Lookup` 都没有）：
   ```csharp
   var t = Root.Find(key);
   var lb = t != null ? t.GetComponent<Label>() : null;
   ```
   ⇒ 对**容器下的 Label**（`flt_input_t` · `flt_title_*`，都在 `flt_drawer` 底下）**恒答 `null`**（静默）。
   今天没炸：**唯一调用方 `Editor/DeckScene.cs:1016/1026` 用的是 `poolcnt_0`**（直挂 `Root`）。
   ⚠️ 这件事**已经被本工程的文档记过一次**：`Editor/DeckScene.cs:193-195` 写着「`DeckRuntime.UiLabelText` 那种只认直接子物体的口子
   **在这四个字上恒答 `null`（别拿它量）**」—— 是**已知**的口子，但它**不在本件四条里**、判据 §⑭ 也没点它的名
   （§⑭ 列的是 `UiHasQuad`/`UiTextureName`/`UiQueueOf`/`UiQuadCount`）。⇒ **建议单开一件**（补第三步，与这四条同一形状、同一个坑）。
2. **第六条（潜伏，今天结构上安全）：`UiPoolCellRect`（`Deck/DeckRuntime.cs:2278`）** 也是 `Root.Find("pool_" + i)` 一步；
   `pool_*` 现在由 `CardView.Create(Root, …)`（`:1138`）**直挂 `Root`** ⇒ 今天答得对。
   ⚠️ 但它是同一形状 ⇒ **哪天卡根被套进容器**（「卡根多一层 `2DCard`」那类改动继续蔓延的话），它会**静默**答 `false`（谎报「卡位没显示」）。
   建议：**记账即可**，等真被套进容器时再补（补法与这四条同）。
3. **判据文件里那句理由要订正（给调度台改，不是我改正本）**：`资料/待办判据_审查发现_1005.md:90` 写「今天没触发：
   **现有调用 key 全是直挂 `Root` 的**」。**实测不成立** —— 抽屉那批 `flt_*` / `cosmoflt_*` 就在容器底下；
   它们没炸的真实原因是**另两条**：① 三条读数被 `Lookup`（`_named`）救下；② `UiQuadCount` 的 `_named` 兜底**硬编码 1**，
   而 `flt_bg` 恰好真是单块。**换句话说：这颗雷不是「没装药」，是「药被 `_named` 盖着」** ——
   任何**新加**的容器下、且不进 `_named` 的自检 key（本件新加的就是）都会立刻踩中。
