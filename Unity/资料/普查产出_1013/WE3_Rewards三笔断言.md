# WE3 · RewardsScene 三笔断言（A239 + A353 + A390）

> 写手：**2026-10-13** · 白名单 = `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`（只动 A353 / A390 两段 + A390 那句陈旧文案）+ 本报告。
> ⛔ 没跑 Unity · 没动 git · 没改两张正本 · 没碰 `Shell/*`（`Shell/CampaignTab.cs` / `Shell/CampaignRewardWindow.cs` 今天都有别的写手在改，我只读）。
> ⚠️ 行号 = **本件收工那一刻的现读**。🔴 这个文件今天**有第二个写手同时在写**（见 §七·1），行号一直在涨 ⇒ **引用一律带锚点句，别只认行号**。

---

## 一、结论

| 账 | 状态 | 一句 |
|---|---|---|
| **A239** | ⛔ **停工（调度台令）· 一行未动** | 见 §二 |
| **A353** | ✅ **断言已落**：1 条控制组 + 4 条 ★ + 2 条量化 + 1 条前提哨兵 | 落点现读 **`:8655-8717`**；⚠️ **只验 `Clip` 那半**（如实标注，见 §三末） |
| **A390** | ⚠️ **已换独立判据，但落地时又查出【更大的一层】** | 落点现读 **`:1455-1505`**。原断言**不是「自证」，是「空转」**（一次都没跑过）⇒ 本件**同时把 bar 的取法修对了**，否则改完照样不执行（见 §四） |

- **类型检查**：`TMPDIR=/tmp/wf_we3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（全程跑了 3 次，收工前最后一次仍 0）。
- **我的改动只落两处**（`git diff` 三个 hunk 里第 1、第 3 个是我的；第 2 个是别人的）：
  - `@@ -1446,21 +1446,62 @@` = **A390**（+41 行 / −0）
  - `@@ -8339,6 +8651,70 @@` = **A353**（+64 行 / −0）
  - `@@ -2215,6 +2256,277 @@` = **别人的 A389 段**（+271 行），**我没碰**。
- **行尾**：改前 **CRLF 0 / LF 8499**，收工 **CRLF 0 / LF 8875** ⇒ **纯 LF、没被翻**（⚠️ 简报里写「这文件是 CRLF」与实测不符，见 §七·4）。

---

## 二、A239 停工状态（调度台令）

**我没写过一行 A239 的东西** —— 收到停工令时我只在做 Read / Grep（一行 `Edit` 都还没发）⇒ **没有需要调度台回滚的改动**，报告里也没有「改动清单」可交。

### 停工前已查实的（交 A239 下一轮，别重查）

1. **锚点（现读）**：A239 那段要接在「两列 holder」之后 —— 上面是
   `const float itemW = CampaignRewardWindow.ItemW, btnW = CampaignRewardWindow.UnlockW;` / `const float badgeW = …BadgeSize;` 与 `baseW` / `premW` 两条算式，末尾那两句是
   `CheckNear(cw.PremHolderRect.W, premW, 0.5f, …)` 与 `CheckTrue(cw.BaseHolderRect.x1 >= 0f && cw.PremHolderRect.x2 <= 1920f, …)`。
2. 🔴 **`资料/普查产出_1013/A表现核_块4.md` 给的落点「在 `:4498`（那句 `CheckTrue(cw.BaseHolderRect.x1 >= 0f …)`）之后插 4 条」【照抄会编不过】**：
   E1 §三·1 那段断言用的是 `ph`（= `Content/Scroll View/Viewport/Content/Premium Rewards/Rewards`），
   而 `var bh = …; var ph = …;` 是在那两行**之后**才声明的 ⇒ **落点必须在 `bh`/`ph` 之后**（下一轮请用节点名/句子定位，⛔ 别用行号）。
3. **注释订正点**（`2135..2380` → **`1955..2200`**）：现读 = 那句
   ``"★ 它整块落在视口外（2135..2380）⇒ **底图 / 点击区 / 文字一个 quad 都没建**"``。
   ⚠️ 它的行号今天漂得厉害（A 表记 `:4534` · 块 4 记 `:4898` · 本件收工那一刻 **`:5210`**）⇒ 只认句子。
4. 我按 E1 §三·1 复核过那三个期望值的**算术**：`hr.x1 = 1025` + `padL 30` + `Σ(childW + 25)`
   ⇒ 物品 `1055..1255` · 按钮 `1280..1525` · 徽标 `1550..1650`（与 `ItemW=200 / UnlockW=245 / BadgeSize=100` 自洽）。
   ⚠️ **这份复核算废**：它只对 **E1 的模型**（三件参与 holder 的 `HorizontalLayoutGroup`）成立，而该前提**已被同批 W-C1 推翻**
   （`Unlock Button` / `Warning` / `Badge` 全是 `m_IgnoreLayout = 1`）⇒ ⛔ **别拿这两个数当新模型的判据**，全部重来。

---

## 三、A353 断言（现读 `:8655-8717`）

**位置**：`§十 A266 + A303` 那个作用域块的**最后一句之后**（锚点句 = `CheckTrue(!SmallScreenUI.Enabled, "（收尾）A327：自检跑完把开关放回**出厂值 关**");`），在 `Debug.Log(P + win.Dump());` **之前** —— 即 `Run()` 的末尾，后面只剩 dump / 退出，碰不到任何截图（本节之前的最后一次截图在 §十 之前很久）。

### 夹具怎么搭（4 步，照 E1 §三·2 的形状，我在两处加固）

| 步 | 动作 | 为什么 |
|---|---|---|
| 1 | 快照 `win.Clip / ClipPad / ClipSoftness` → 在 `camp.parent`（= `Tabs`）下 `MenuDraw.Node` 造**全新**宿主 `A353 CampaignTab Probe`（矩形 = 原版 `Campaign Tab` 330.69,70.94→1920,1080）→ `AddComponent<CampaignTab>()` + `SetHost(win, host)` | `MenuDraw.Node` 是**只建不找**的 ⇒ ⛔ 别就地 `campTab.Build()`（会在 `camp` 下再建一整套同名节点、`_armyScroll` 再登记一个滚动区）；新宿主与既有树**互不干扰** |
| 2 | **下毒**：`Clip = (2500,1200)-(2600,1300)`（与四件**全不相交**的屏外框）· `ClipPad = (40,40,40,40)` · `ClipSoftness = (10000,10000)` | 挑「全不相交」⇒ 改坏时四件**一个都不建**，信号最干净 |
| 3 | **控制组**：同一个毒值下 `win.DrawRect(host, CardArt.Solid(), (400,300)-(500,400), …)` ⇒ 必须 `== null` | 证明毒值**真的带电**（否则下面四条可能是空转） |
| 4 | `a353Tab.Setup()` → **立刻还原三件套** → 量四件 → `PointerLayer.UnregisterOwnedBy(host.gameObject)` + `Object.DestroyImmediate(host.gameObject)` | 还原是因为**本夹具不考裁切**；撤滚动区 + 销毁宿主是 `Setup()` 会**再登记一个滚动区**（`Owner = host`）的收尾 |

**加固的两处（E1 的片段里没有，或写得会崩）**：
- E1 的 ★② 写作 `FindChild(FindChild(host,"Campaign Army Selector"),"Background").GetComponentInChildren<ImageQuad>(true)` —— 若那一件**不建**（= 正是改坏后的样子），`FindChild` 返 `null` ⇒ **`NullReferenceException` 会打断整个自检**（不是「红」）。我改成先落到局部变量再 `!= null &&` 短路。
- 四件的取法我改用 **`FindPath(host, "A/B")`**（= `Transform.Find` 的**直接子件路径**）而不是 `FindChild`（它按名字在**整棵子树**里搜）—— 同样四个名字在子树里别处也有（例：`Premium Panel` 里还有别的东西），路径写法把歧义掐死。
- 收尾多了 `PointerLayer.UnregisterOwnedBy(host.gameObject)`（E1 只写了 `DestroyImmediate`；加这一句是显式撤登记，不靠 `PruneScrolls` 兜底）。

### 断什么 · 期望值来源 · 改坏法

| # | 断什么 | 期望值来源 | 改坏法（改回错的必须红） |
|---|---|---|---|
| **控制组** | 毒框下画屏内探针 ⇒ `DrawRect` **返 null** | 无关期望值：断的是「毒值带电」 | 把毒框换成屏内框 ⇒ 这条红（同时下面四条全变空转） |
| **★①** | `Campaign Background/Background Image` **建了且有 `ImageQuad`** | 存在性（原版这一件没有 mask ⇒ 该建） | 删 `Shell/CampaignTab.cs` 第①处 `ClearClip()`（现读 **`:136`**）⇒ 毒框与它的原版矩形不相交 ⇒ 整块不建 ⇒ 红 |
| **★②** | `Campaign Army Selector/Background` 同上 | 同上 | 删第②处 `ClearClip()`（现读 **`:149`**）⇒ 红 |
| **★③** | `Campaign Header/Title` **有 `Label`** | 同上（`_win.Text` 也吃 `RenderClip`：`Clip` 非空 ⇒ `MenuWindowBase.Text` 那句 `if (!MenuDraw.Visible(...)) return null;` 直接返 null） | 删第③处 `ClearClip()`（现读 **`:175`**）⇒ 红 |
| **★④** | `Premium Panel/Background` 同上 | 同上 | 删第④处 `ClearClip()`（现读 **`:876`**，⚠️ E1 记的 `:842` 已漂 +34）⇒ 红 |
| **量化 ×2** | `CheckAt(host 的 "Campaign Header", 330.69, 790.92, 60.94, 225.94)` · `CheckAt(host 的 "Premium Panel", 344.29, 720.35, 867.01, 1080.00)` | **原版字面量**（同一页既有断言 `CheckAt(chdr, …)` / `CheckAt(cpan, …)` 用的是**同一组数**，可互证） | 同上四处；另：把 `Build()` 里的矩形算式改错也会红（这一对同时钉「建出来了」与「摆对了」） |

**为什么必须在「新实例 + 毒框」上量**（不这么写 = 空转）：这四件是在 `Build()` 里建的，而**第一次 `Build()` 早就跑完了**
（`RewardsWindow.Build → BuildTabContents → Setup()`），那时 `Clip` 本来就是 `null` ⇒ **在既有那棵 `camp` 树上量，把那四句 `ClearClip()` 全删掉照样绿**。

### ⚠️ 如实标注：三件里**今天只有 `Clip` 那一条能单独起作用**

`MenuDraw.Rect` 只在 `clip.HasValue` 时才求交 / 采软边；`MenuWindowBase.Text` 只在 `RenderClip.HasValue` 时才 `ClipText`；
`MenuDraw.PaddedClip(null, pad)` 第一句就返 `null` ⇒ 清 `pad` / `soft` 两句是**纪律件**（把「这一处没有 mask」写全），**今天不产生行为差异**。
⇒ **改坏法只有「删掉那一处 `ClearClip()`」**；**只删 `ClipPad` / `ClipSoftness` 里那两句赋值，今天不会红**。
⛔ 断言文案里**没有**写「三件都验到了」（这一点 E1 点名要求）。

---

## 四、A390 断言（现读 `:1455-1505`）

### 🔴 落地时查出的第一件事：这一格原来**不是「自证」，是「空转」**

| 项 | 内容 |
|---|---|
| 旧写法 | `var wbar = FindChild(weekly, "Progress Bar"); if (wbar != null) { float t = DailyData.WeeklyProgress01(); … }` |
| 事实 | `FindChild(weekly, "Progress Bar")` **恒返 `null`** ⇒ **整段 `if` 一次都没进过**（既没红过、也没绿过） |
| 判据① | 全仓 `"Progress Bar"` 这个**节点名**只有一处会建：`Shell/MissionsTab.cs:540` 的 `NodeD(parent, "Progress Bar", pb)` —— 那是**每日行**那条路（`BuildDailyRow`）；**周常**这条是 `Shell/MissionsTab.cs:915` 的 `BuildBar(parent, bar, DailyData.WeeklyProgress01(), …)`，**把九宫格直接挂在 `Weekly Mission` 卡上、不建同名节点** ⇒ 周常卡子树里没有这个名字（⚠️ `Shell/MissionsTab.cs` **今天也有别的写手在改**，行号会漂：这一趟我读到过 `BuildBar` 在 `:905` → `:915`、`float t = …WeeklyProgress01()` 在 `:915` → `:925`；**判据认句子不认行号**） |
| 判据②（独立旁证） | 即便 `wbar` 非空，旧写法也会**红**：那条 bar 是**九宫格**（`MenuDraw.Nine` 建 9 块子 quad），而 `Wpx()` 取的是「子树里**第一个** `ImageQuad`」= 一块 **12×12 的角块**（`BuildNine(…, 12f, 12f, …)`），不是整条 1008.43 宽 |
| ⇒ 结论 | **这一格从来没被验过**。A 表 / `普查产出_1013/A表现核_块1.md` §A390 的「自证型」只诊断对了一半（它们都只读了源码，量不到「会不会进 `if`」） |

### 换法（三件，都在本格内）

1. **bar 按结构找**：周常卡里 `MenuDraw.Nine` **只有进度条这一处**（`BuildNine` 全仓只被 `BuildBar` 调、共 2 次：先 `40k_generial_bar_empty`、后 `…_bar_fill`，`Shell/MissionsTab.cs:1116/1120`）⇒ **第一棵 `Nine` = 整条 bar**；
   矩形改走 **`RectOfUnion`**（九宫格 9 块的**并集** = bar 自己；⛔ `Wpx()` 只量得到一块角）。
   ⚠️ 并集的 **x** 范围与 bar 完全相等（左/右角块各 12 宽、中段 = `W − 24`）；y 方向因 `2×border(24) > bar 高(22.766)` 可能略有出入 ⇒ 本条**只比 x**，不受影响。
2. **找不到就红**：新增 `CheckTrue(wbar != null, "（A390 前提）…找不到就量不了把手；⛔ 这是「不许静默空转」…")` —— 把原来的**静默跳过**改成**出声**。
3. **期望值 = 冻结字面量 + 夹具钉进度**：
   - `DailyData.ForceWeeklyProgressForTest(13)`（现成写口）+ `Check(WeeklyProgressValue(), 13, …)` 把前提钉明；
   - `const float tA390 = 13f / 30f;` —— **字面量**，⛔ **不再读 `WeeklyProgress01()`**；
   - **两条 ★**：① 数据那一半 `CheckNear(DailyData.WeeklyProgress01(), tA390, 1e-4f)`（**不依赖 bar 找不找得到**，去自证后**一定跑得到**的那一半）；② 几何那一半 `CheckNear(handle.x_px, bx1 + (bx2−bx1)*tA390, 3f)`。

**⚠️ 本夹具【不重建】`MissionsTab`**（已写进代码注释）：`MissionsTab.Build()` 会销毁整棵子树，而本节**后面**（`Reward 0` / `Collect` 那两处，现读 `:1512`/`:1526` 一带）还要用 `rows[0]` ⇒ 重建会把它们变成已销毁对象（既有的先例见 `:1285` 那条注释）。
树是在本趟开头按出厂进度 13 建的 ⇒ 把手本来就在 13/30 那一位，`ForceWeeklyProgressForTest(13)` 只是把前提**写明**。

**注释里如实写了**（原文在代码里）：
> ⚠️ **如实标注：这是【回归判据】，不是【原版读数】的判据** —— 原版那条进度是**服务端下发的玩家数据**（prefab 里那颗 `Handle` 是 Slider 把手、序列化位只是模板位）⇒ 本地**拿不到**「原版该在哪个 x」。

**改坏法**：`DailyData.WeeklyTarget` 改回 15（或出厂进度改掉）⇒ 实现把把手挪到 13/15 那一位 ⇒ **带 ★ 的那两条一起红**
（改前那一版两条**一起变**、恒绿；而且实际上连进都进不去 ⇒ 现在这一版**两条都会真的执行**）。
另：删 `Shell/MissionsTab.cs:915` 那句 `float t = DailyData.WeeklyProgress01();` / 把 `hx = bar.x1 + t*bar.W` 改错 ⇒ 几何那条红。

**顺手订正的两句陈旧文案**（同一节、`A390` 那个「顺手发现」点名要核的）：
- `CheckTrue(wcnt != null, "`counter`（`13/15`）建了")` → **`13/30`**（`WeeklyTarget` 已 15→30）。
- 那条注释「…（Slider 的行为；进度 = 13/15）」→ **`13/30`**。

---

## 五、没有牙口的（如实写，⛔ 别把「没验」写成「验过了」）

1. **A353 只验 `Clip` 那半**：`ClipPad` / `ClipSoftness` 两句赋值删掉**今天不红**（理由见 §三末）。它们不是「验到了」，是「写全了」。
2. 🔴 **A353 ★④ 的说明里那半句不成立**（E1 原话「观测的是『`BuildTrack` 有没有把 `Clip` 还原』的另一半」）：
   毒框下 ④ 自己的 `ClearClip()` 会把三件套清干净 ⇒ **`BuildTrack` 还不还原都不影响 `Premium Panel` 建不建**。
   ⇒ 这一条**只验 ④ 的 `ClearClip()` 在不在**，**验不到** `BuildTrack` 那对既有还原（那两处仍由别的断言盯着）。代码注释里我**没有**照抄 E1 那半句。
3. **A390 的「两态」验不到**：要验「进度变 ⇒ 把手真的跟着挪」必须重建 `MissionsTab`，而本节**不能重建**（会毁掉后面要用的 `rows[0]`）。
   ⇒ 今天能验的是**单态 + 回归**：`13/30` 这个值对应的 x（几何）+ `WeeklyProgress01()` 等于这个冻结比值（数据）。
   **没验**：「进度改成别的值时把手跟不跟」——那一档要另找落点（例如挪到 `§` 末尾再去重建，或用另一个宿主）。
4. **A390 几何那一半的 bar 几何仍取自实现侧**（`RectOfUnion` 量的是 `MissionsTab` 建出来的九宫格）—— 它不是从 `UguiRect.Child` 链重算的原版值。
   如实说：这一条的**独立变量只有 `t`**（期望值那一半），bar 的 x/宽是量出来的。要连 bar 一起独立，得把 `prog`/`bar` 的锚点算式用原版字面量重算一遍（本件**没做**，理由：不经实跑无法自证锚点算式，风险大于收益）。
5. **总的一句**：本节 7 条断言（控制组 1 + ★4 + 量化 2）**一条都没实跑过** —— 子代理不跑 Unity。
   A353 的夹具「在新实例上 `Setup()` 不抛异常」只有**读代码**的依据（`AddHit` 的调用方不接返回值、`MenuDraw.Hit` 全出框时**返 null 不抛**、`MenuWindowBase.Text` 同理、`MenuDraw.Rect` 在 `tex == null` / 全出框时返 null 不抛）—— **没有实证**。
   ⚠️ **A390 那条 `Nine` 前提（新增的哨兵）请务必在同步点看一眼**：它若红，说明我对「第一棵 `Nine` = bar」的结构判断错了（那时几何那一半会退化成只报一条前提红，**不会静默**）。

---

## 六、没查清 / 没做的

1. **A239 全部停工**（§二），一行未动，等调度台裁新模型（`m_IgnoreLayout = 1` 那一套）。
2. **没跑任何 Unity 自检**（红线：子代理不跑）⇒ §五·5 那一条适用。
3. **A390 的「两态」**（进度变 → 把手挪）没落（§五·3）。
4. **没做**：把 `FindChild(weekly,"Progress Bar")` 这个**恒 null 的取法**是不是在**别处**还有同病 —— 我只扫了 `RewardsScene.cs` 里 4 处 `"Progress Bar"`（`:1229` / `:1649` / `:8240` / `:8269` / `:8293`），那几处**都带 `rows[…]` 或 `rowFull/rowDone/rowBack`**（每日行）⇒ 名字与节点是对的，**没继续往别的窗查**（不在本件范围）。

---

## 七、顺手发现（⛔ 只报不改）

1. 🔴🔴 **同一文件有第二个写手**：`Editor/RewardsScene.cs` 在我这一趟期间**被别人改着**——`@@ -2215,6 +2256,277 @@`（`🆕 A389（2026-10-13）里程碑格的图…` 那一节，现读 `:2256` 起，约 +271 行）。
   我这期间读到过它的三个不同长度（`:2239,247` → `:2239,256` → `:2256,277`）⇒ **它当时还在写**。
   ⚠️ 与铁律 13·3「一个文件同一时刻只有一个写手」冲突；**但我与它的区间零重叠**（我在 `:1446` 与 `:8651`），**没有行级冲突、我一行都没碰它**。请调度台核一下这一批的排程。
2. 🔴 **`资料/普查产出_1013/A表现核_块4.md` 给 A239 的落点【编不过】**：它说「在 `:4498`（那句 `CheckTrue(cw.BaseHolderRect.x1 >= 0f …)`）之后插 4 条」，而那 4 条要用的 `ph` 在**后面两行**才声明（§二·2）。
3. 🔴 **A390 的诊断只对了一半**（§四）：它**不是自证型，是空转型**。两条独立证据（节点名只有每日路会建 + `Wpx()` 只能量到九宫格角块）。建议 A 表 A390 那一行的「症状」栏就地订正（`资料/*` 不在本件白名单 ⇒ 我没动）。
4. 🔴 **简报底稿写着「`Editor/RewardsScene.cs`（**CRLF 文件**）」，与实测不符**：改前 **CRLF 0 / LF 8499**（同一事实另见 `普查产出_1012/R1_骷髅与周常.md` §七「`RewardsScene.cs` CRLF 0 / LF 7245」）。
   我用 `Edit` 改完复量仍是 **CRLF 0**，**没翻行尾**。⇒ 建议订正简报模板里那一句（照 CRLF 去处理会误判）。
5. **`Shell/CampaignTab.cs` 第④处 `ClearClip()` 的行号已漂**：E1 记 `:842`，现读 **`:876`**（`RestoreClip` 相应为 `:937` → **`:971`**）；①②③ 三处（`:136/144` · `:149/154` · `:175/242`）**未漂**。⇒ 引用别只认 E1 那一套行号。
6. **A390 那一格的「bar 取法」这颗雷**：`FindChild(x, "Progress Bar")` 这种「按**每日行**的节点名去周常卡里找」的写法，全靠运气 —— 同族风险是**任何按名字取节点、名字又只存在于另一个兄弟子树**的地方（本件只报了 A390 这一处）。

---

## 八、类型检查结果

```text
$ TMPDIR=/tmp/wf_we3 bash d:/4/Unity/工具/typecheck.sh     # ① A353 落完之后
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0

$ TMPDIR=/tmp/wf_we3 bash d:/4/Unity/工具/typecheck.sh     # ② A390 两段落完之后
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0

$ TMPDIR=/tmp/wf_we3 bash d:/4/Unity/工具/typecheck.sh     # ③ 改完 bar 取法 + 注释订正之后（收工前，文件已含别人的 A389 段）
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 三次**都是 0 错**（含最后一次：那一刻文件里**已经**有别人的 A389 段，它当时**编得过** ⇒ 那一段不是半成品）。
- **行尾复量（二进制读）**：`RewardsScene.cs` **CRLF 0 / LF 8875**（收工）· 改前 **CRLF 0 / LF 8499** ⇒ 我的 `Edit` **没有翻行尾**（`git diff` 也没有整篇重写的迹象：我的两个 hunk 合计 **+105 / −0**）。
- ⛔ **没跑 Unity / 自检**（只有主对话能跑）· ⛔ **没动 git** · ⛔ **没改两张正本** · ⛔ **没碰 `Shell/*`**。
