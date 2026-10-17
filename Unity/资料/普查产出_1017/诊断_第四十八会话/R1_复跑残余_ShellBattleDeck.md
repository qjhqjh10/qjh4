# R1 · 复跑残余 69 红里的 Shell 7 / Battle 3 / Deck 1 —— 只读诊断

> 只读诊断代理 R1，2026-10-17。**没有改任何代码、没跑 Unity、没动 git。**
> 输入 = `d:/4/_tmp_view/{shell,battle,deck}.log`（第四十八会话 20:39–21:22 那一轮）+ 现读源码。
> 每条给：**结论 · 证据（file:line）· 分类（α 断言错 / β 实现缺陷 / γ 本批回归 / δ 夹具前提不成立）· 修法 · 没查清**。

---

## 摘要（逐条一行）

| # | 条目 | 分类 | 一句话根因 |
|---|---|---|---|
| 1 | Shell `N23` 长文案根高 | **β** | `ToastRootHeight` 的「量不出来」上界写死 **400px**，长文案真值 **627.51px** 被当成哨兵 ⇒ 退回常量 108.86 |
| 2 | Shell `N20②`（未完成档） | **α** | 第一条断言拿的是**完成档那条** toast 的 `Rewards`（`doneRw`），与第二条自相矛盾 |
| 3 | Shell `A1126` Victories title `Label 拿得到` | **α** | 断言找 `"Victories title"`，实现那棵树叫 **`"Vicotries title"`**（原版拼写，故意照抄） |
| 4 | Shell `A1178 Timer` 渲出高 37.98 ≠ 框高 55.905 | **α** | 「渲出高 ≈ 框高」这个前提只在 CJK 行盒（1.52em）下成立；换 Pragati 后行盒 0.947em、自适应**停在 max**，自然只有 ~38px |
| 5 | Shell `A1178` Victories title `Label 拿得到` | **α** | 同 #3（同一个名字错） |
| 6 | Shell `A1126` Craft Explanation 静态断言 | **α** | 期望子串写死 `"…36f);"`，源码已变成 `"…36f, face: TmpFont.Face.LatinCardBody);"` |
| 7 | Shell `RT 深度 32 vs 24` | **α** | 代码确实传 24（`RTDepthBits`）；`RenderTexture.depth` 回报的是**实际分配的格式位深**（D24S8 ⇒ 32），不是构造实参 |
| 8 | Battle 对手称号 ≠ `BotTitleName` | **β** | `RefreshHudTitles()` **只在 `_hudBuilt` 闩住的 `BuildHudExtras` 里调一次**；之后每次 `Begin(seed)` 换种子都不刷新 ⇒ 名牌上停着第一局的称号 |
| 9 | Battle `QPText_Me 43.68` | **δ** | 那一颗是**未激活**的（非 DarkAngels），TMP 的 `ForceMeshUpdate` 对未激活对象直接返回 ⇒ 读到的是 `SetTextTmp` 留下的**标称字号**，不是自适应收敛值（同组的 `QPText_Foe` 共 40.50 就是证明） |
| 10 | Battle 教学四块描边 = 0/4 | **β** | `ApplyThinOutline` 用 `lb.GetComponent<TMP_Text>()`，而 TMP 是 **Label 的子节点**（`TmpFont.NewText` 新建 GameObject）⇒ 恒 null、静默 return ⇒ 一次都没描 |
| 11 | Deck `A1735⑥` 4.65 / 上限 4.88 | **γ** | 本轮 `EmPerFontSize` 改成面无关（0.103906 → 0.0948）⇒ 本颗 `fontSizeMax` 涨 9.6% ⇒ 从「撞上限」变成「被框高压缩」，比值 4.65/4.88 = 0.9529 |

---

## 1. `shell.log:33798` · N23 长文案根高

**结论**：`ToastRootHeight` 的哨兵上界 `textH > 400f` 定得太低。长文案量出来的首选高 **627.51px** 是**真值**（不是天文哨兵），被这条闸判成「量不出来」⇒ 退回兜底常量 108.86；而模板本来就是 108.86 ⇒ `ApplyMeasuredHeight` 的 `|newH-oldH|<0.01` 早退 ⇒ 长文案根高没变。短文案那条（178.59）走的是正常路，所以断言两侧一高一低正好反了。

**证据**
- 判红那条：`shell.log:33798`「短 178.59px → 长 108.86px」。
- 前一条 warning：`shell.log:33770`「文本首选高量不出来（**627.51px**）⇒ 退回模板常量 108.86px」。
- 闸门：`Assets/CardPresentation/Shell/ToastNotifications.cs:1000` `if (!(textH > 0f) || textH > 400f)`；文案与兜底同函数 `:1002`。
- 627.51 是真测量的**旁证**：短文案文本高 69.72px（`shell.log:33812` 那条绿：`12 + 89.8638 + 69.72 + 7 = 178.59`），而 **627.51 ÷ 69.72 = 9.000** ⇒ 正好 9 行，是排版出来的多行高度，不是哨兵。
- 该函数自己的 doc（`ToastNotifications.cs:985-990`）写着「量不出（未激活 / 字体资产缺失 / 哨兵值）」——**400px 并不对应这三档里的任何一档**（哨兵是 `4.29e9` 那一量级，见 `Battle/Label.cs` 的 `UsableBounds` 家族）。

**分类**：β（实现缺陷；断言没错）。

**修法**：把上界改成同族「量得出来吗」那类哨兵闸，例如 `textH > 100000f`（`Editor/BattleScene.cs` 的 `szOk` 与 `A1126` 的「字块量不到」闸都用 100000），或 `textH > 4000f` 并在注释里写清「long toast 9 行 ≈ 630px」。⛔ 不要靠调小阈值把长文案绕过去 —— 现在的 400 连 6 行就会误判。

**没查清**：没有 Unity 可跑 ⇒ 无法逐字复算长串断成 9 行；但「9.000 × 单行高」这个整数倍关系足以排除哨兵/脏读数。

---

## 2. `shell.log:33839` · N20②（未完成档）

**结论**：**第一条断言引用了第二条断言的对象**。`half`/`done` 两条 toast 取对了，但「未完成档」那条把 `done.Find("Rewards")`（**完成档**的 Rewards）也算进「都在画」；而完成档的 `Rewards` 按 `displayRule = 1` 是**应该关**的 —— 于是这两条断言**结构上不可能同时通过**，实际就是第二条绿、第一条红。`Progress Bar` / `progress` 那两个对象本身是对的。

**证据**
- `Assets/CardPresentation/Editor/ShellScene.cs:7039` `var doneRw = done.Find("Rewards");`
- `:7040-7043` 第一条断言要 `doneRw.activeSelf`（未完成档）；
- `:7045-7048` 第二条断言要 `!doneRw.activeSelf`（完成档）—— 同一颗对象、互斥。
- 实现侧对应：`Shell/ToastNotifications.cs:899-902` `rewards.SetActive(!complete && …)`；`:914-916` `pg.SetActive(!complete)` / `pbNode.SetActive(!complete)`。

**分类**：α。

**修法**：新增 `var halfRw = half.Find("Rewards");`，第一条用它；第二条继续用 `doneRw`。改完顺带确认三条各自都有「完成/未完成」两态对照（现在第一次的 `halfPB/halfPg` 没有反例，只靠第二条的 done 侧成对，仍可）。

**没查清**：日志只打了整条断言的布尔，没逐项打 `activeSelf`；但「两条断言互斥」已足以定性。

---

## 3 / 5. `shell.log:36465` 与 `:36551` · A1126 / A1178 的 Victories title

**结论**：断言的节点名与实现不一致。实现里那颗叫 **`Vicotries title`**（原版 prefab 的拼写，实现有意照抄并注释了「⛔ 别顺手改成 Player」），断言找的是 `Victories title` ⇒ `FindChildIn` 返回 null ⇒ 两条「`Label` 拿得到」红；**同一颗的 A1126 四格五项与 A1178 ①②两项也跟着没跑**（`if (lb == null) return/continue`）。

**证据**
- 实现：`Assets/CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs:592`
  `MenuDraw.Text(pv, vtR, "Victories: ", Color.white, "Vicotries title", 53.5f, …)`
- 断言①：`Assets/CardPresentation/Editor/ShellScene.cs:7309` `FindChildIn(a1126En.transform, "Victories title")`
- 断言②：`:7367` `foreach (var nm in new[] { "Timer", "Victories title" })`
- 同一文件里**正确**的写法就在旁边：`:7225` 找的是 `"Vicotries title"`（遭遇战窗那颗），`:7227/:7311` 的说明文字也写着原版是 `Vicotries title`。

**分类**：α。

**修法**：把 `ShellScene.cs:7309`、`:7367` 的字符串改成 `"Vicotries title"`；`7308/7296` 的说明文案可以顺手统一。⚠️ 改完这两处，A1178 的第 ① 条会**开始跑**，而它很可能按 #4 的原因红一次（见下）—— 两件一起改，别分两轮。

**没查清**：无（名字是逐字现读）。

---

## 4. `shell.log:36522` · A1178 `Timer` 渲出高 37.98 vs 框高 55.905

**结论**：这条**关系式的前提已经失效**，不是字被压小。原意是「高度那一侧是紧的 ⇒ 渲出高 ≈ 原版框高」。它成立的前提是**我们的行盒比原版高**（注释里写的「我们行盒 1.52em ⇒ 38×1.52=57.8 > 55.905 ⇒ 被压到 55.9」）。但这两颗现在挂的是 `TmpFont.Face.LatinCardBody`（Pragati，行盒 0.947em，见 `Deck/DeckRuntime.cs:1983` 那段判据）⇒ 38px 的字自然只有 ~38px 高，**高度根本不是约束**；自适应停在 max。渲出 37.98 是这一档的正常值。

**证据**
- 判红：`shell.log:36522`（`37.982 ≈ 55.905±1.500` 不成立）。
- 同一颗**宽度那一侧**的断言是绿的：`Timer` 框宽 262.44 − 渲出宽 189.81 = 72.63 ≥ 1.5 ⇒ **两侧都不紧**。
- 实现：`Shell/EnergySinglePlayerOnlyEventWindow.cs:720-724`（`MenuDraw.Text(..., 38f, …, face: LatinCardBody)`）· `:745-748` `SetAutoFitBox(w, 55.9, 10, 38, 38)`。
- 断言前提的出处与旧推导：`Editor/ShellScene.cs:7330-7361`（那段注释写「我们行盒 ≈1.52em」是 **2026-10-10** 的 CJK 字体时代的读数；`A1024③` 换 Pragati 之后不再成立）。
- 与本轮 `EmPerFontSize` 的关系：本轮改动把这颗的渲出高从 ≈34.65 抬到 37.98（×1.096，Pragati 的 `0.103906/0.0948`），**离 55.905 仍然远** ⇒ 这条红**不是本轮引入**，是换面那一步（`A1024③`）带来的断言过期。

**分类**：α（断言前提过期；不是实现缺陷）。

**修法（二选一，建议后者）**
- (a) 保留「关系式」但换成对当前字体仍成立的双侧式：`rz.y <= boxHOrig + 1.5`（装得下）+ 已有 ②（宽度不紧）。
- (b) 直接断**原版 `m_fontSize` 38px 这个字面量**：`lb.FontSizeToPxNow(lb.FontSize, lb.ActualFace) ≈ 38 ± 0.06`。它是**原版实读值**，不是实现里的常量，能咬住「字被任何一侧压小」；⛔ 不要写「≈ 上限」（`fontSizeMax` 是实现写进去的，属自证）。
- ⚠️ 修 #3/#5 之后 `Victories title` 会走到同两条关系式上：它现在也不再被框高顶住（Pragati 行盒小），**同一个修法要一起套上去**，否则只是把红从 Timer 挪到 Victories title。

**没查清**：没有 Unity 可跑 ⇒ 无法量 `Victories title` 换名后的渲出高；按同一套字体/同一机制推，它也会落在「自然行高 < 框高」。

---

## 6. `shell.log:37826` · A1126 `Craft Explanation` 静态断言

**结论**：纯静态断言的期望子串过期。源码那一行在 A1024 换面时末尾多了 `, face: TmpFont.Face.LatinCardBody`，于是 `"…36f);"` 不再是它的子串。

**证据**
- 断言：`Assets/CardPresentation/Editor/ShellScene.cs:7491` `const string wantArgs = "1726f - 1307.5f, 10f, 40f, 36f);";`
- 源码：`Assets/CardPresentation/Shell/CardDetailPopup.cs:901-902`
  `MenuDraw.Text(… "Craft Explanation", 40f, QCdText, 1726f - 1307.5f, 10f, 40f, 36f, face: TmpFont.Face.LatinCardBody);`

**分类**：α。

**修法**：把 `wantArgs` 去掉收尾的 `);`（例如 `"1726f - 1307.5f, 10f, 40f, 36f"`），并另加一条 `cdSrc.Contains("Craft Explanation")` + `face: TmpFont.Face.LatinCardBody` 的存在性检查（原版那颗也是 Pragati 系）。⚠️ 这条本身就只有「源码文本」的判别力（原注释已如实写），别当成与运行时同级。

**没查清**：无。

---

## 7. `shell.log:40769` · RT 深度 24 vs 32

**结论**：实现传的就是 **24**；`RenderTexture.depth` 在运行时回报的是**实际分配到的深度格式位数**（显卡/API 把 24 位深度+8 位模板合成 D24S8，Unity 报 32），不是构造函数的实参。断言把「构造函数实参」当成「运行时属性」在比 ⇒ 环境相关红。本轮 `UIBakeScreen.cs` 只改了 `RTName`（见 `git diff`），**与深度无关**。

**证据**
- 实现：`Assets/CardPresentation/Shell/UIBakeScreen.cs:59` `public const int RTDepthBits = 24;` · `:179` `new RenderTexture(w, h, RTDepthBits, RenderTextureFormat.Default);`
- 断言：`Assets/CardPresentation/Editor/ShellScene.cs:7793` `Check(bake.RT.depth, 24, …)`
- 本轮 diff：`UIBakeScreen.cs` 仅 `RTName` 一行（`"Background Render Texture"` → `"RT Background bake"`）。

**分类**：α（量错了对象：构造实参 ≠ 分配结果）。

**修法（建议两条一起）**
1. `Check(UIBakeScreen.RTDepthBits, 24, "请求的深度 = 原版 0x18")` —— 断的是「我们按原版传了 24」，这是**输入量**，同 `ShellLabelBoxPx` 那类「读实现写下的输入」的先例；
2. `CheckTrue(bake.RT.depth >= 24, $"实际分配的深度 = {bake.RT.depth}（≥24 即满足；D3D 下 24+8 模板会报 32）")` —— 断「确实有深度」，不断具体位数。
（若一定要断 `==24`：得在 `CreateRT` 里把**请求值**记到一个自检口，别读 `RT.depth`。）

**没查清**：Unity 6 / 本机图形 API 下 `RenderTexture.depth` 的具体回报规则没有实跑验证（红线不跑 Unity）；但**代码传的是 24** 是逐字现读，足以定性「断言量错了」。

---

## 8. `battle.log:161432` · 对手称号 = `BotTitleName`「猎女」

**结论**：`RefreshHudTitles()` 只在 `BuildHudExtras()` 里调一次，而 `BuildHud()` 被 `_hudBuilt` 闩住（整局只建一次）。`BattleScene.Run` 这一条长链里在这条断言之前**已经又 `Begin()` 了十几次**（最后一次 `Editor/BattleScene.cs:15020`，种子 20260916），每次 `Begin` 都会改写 `_seed`，但**没有人再刷新称号** ⇒ 名牌上留着**第一次 `Begin`（`:410`，种子 20260911）**按那个种子挑的那条，而断言/日志里的 `BotTitleName` 是**当前种子**算的 ⇒ 不等。日志里紧随其后的「选第 8 条 ⇒ 贴到我方」是绿的，正因为那一条**显式调了** `drv.RefreshHudTitles()`。

**证据**
- 断言：`Assets/CardPresentation/Editor/BattleScene.cs:15307-15310`（四个合取里唯一会变的就是 `TitleTextOf(false) == BotTitleName`）。
- 种子：`Battle/BattleDriver.cs:3117` `_seed = seed;`；`BotTitleIndex()` `:15409` 用 `_seed`。
- 最近一次 `Begin`：`Editor/BattleScene.cs:15020` `drv.Begin("Ultramarines", "Goff", 20260916);`（在 `Run` 的**顺序执行**里，早于 15307）。
- HUD 只建一次：`Battle/BattleDriver.cs:14364` `if (_hudBuilt) return;`
- `RefreshHudTitles` 唯一调用点：`:15569`（在 `BuildHudExtras` 里）；`:6883-6887` 是定义。
- 侧面证据：`battle.log:161444`「选第 8 条 ⇒ …贴到我方」绿（它显式调了刷新）。

**分类**：β（真缺陷：换局 / 重开一局后对手称号不跟种子走；不是本轮改动引入的）。

**修法**：在 `Begin()` 里、`_seed = seed` **之后**无条件调一次 `RefreshHudTitles()`（与 `HookAnimFxShake()` 那一族「每次 Begin 都重挂」同一条纪律，`BattleDriver.cs:3100-3120` 就是那一族的位置）；`BuildHudExtras` 里那次可留（幂等）。顺带把断言失败消息补上 `TitleTextOf(false)`，下次一眼看出是「文本不等」还是「不可见」。

**没查清**：日志没打 `_titleFoeText`，无法 100% 排除「`TitleVisible/BgVisible` 为假」；但 `ApplyTitle`（`BattleDriver.cs:6891-6896`）在建完时就是 `show=true`，且后面 `TitlePosPx` 两侧都量得到，故文本不等是最可能的那一条。加一行日志即可闭环。

---

## 9. `battle.log:163452` · `QPText_Me` 43.68px 超出 [15.79, 40.5]

**结论**：**被量的那颗标签是未激活的**，TMP 在未激活对象上 `ForceMeshUpdate()` 直接返回（`TextMeshProUGUI.ForceMeshUpdate` → `OnPreRenderCanvas` 的 active 闸），于是：
- 建树时（激活态）它收敛过一次（= **40.50**，就是 `QPText_Foe` 现在读到的值）；
- 之后某次 `SetText`（`UpdateHud` 写 `X/3`）在**未激活**状态下把它重置成 `TmpFontSize()` 的**标称值**，而标称值在当前 px 尺下正好是 **43.68**（= `CapPxPerScale(4) × Em/Cap(Pragati)` = 30.24 × 0.0948/0.0656 ≈ 43.7），`ForceMeshUpdate` 没能把它压回 max。
⇒ 断言读到的是「从没排版过的那一档」，不是渲染结果。产品侧在真机里**激活那一帧会重排**，画面没问题；坏的是**批处理里没有帧循环 + 标签未激活**这个前提。

**证据**
- 判红：`battle.log:163452`；同组 `QPText_Foe` 40.50 **绿**（`:163582`）—— 两侧同一套参数，唯一差别就是「哪一颗被改过文本 / 是否激活」。
- 两颗建法完全一致：`Battle/BattleDriver.cs:15881-15889`（同框 48.17×45.26、同四格 15.79/40.5/36、同 `LatinCardBody`），建完按阵营 `SetActive(meQp/foeQp)`。
- 本局双方都不是 DarkAngels（`battle.log:161252` 那条绿），`QuestPointsFaction = "DarkAngels"`（`BattleDriver.cs:127/162-165`）⇒ 两颗都关着。
- `Label.SetTextTmp`：`Battle/Label.cs:1331-1337` —— **无条件** `_tmp.fontSize = TmpFontSize();` 再 `ForceMeshUpdate()`。
- TMP：`Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TextMeshProUGUI.cs:556-566` → `OnPreRenderCanvas()`（未激活且 `ignoreActiveState=false` 时不做排版）。
- 量法：`Editor/BattleScene.cs:532-611` 的 `CheckHudBox` 只做 `lb.ForceRelayout()`（`Label.cs:698-709`）——它也没有 `ignoreActiveState` 这一档。

**分类**：δ（夹具前提：量一颗未激活的标签）+ 半条实现隐患（未激活时把字号重置成标称值）。**不是本轮 `EmPerFontSize` 引入的**：旧尺下同一颗的标称值是 47.9px，同样 > 40.5、同样红。

**修法（建议先做夹具侧，最小改动）**：`CheckHudBox` 在量之前，若 `!t.gameObject.activeSelf` ⇒ `SetActive(true)` → `ForceRelayout()` → 量 → **还原**（同 `BattleScene.cs:15400-15408` 对 `MyPileLabel/FoePileLabel` 用 `SetCardHoldForTest(true)` 的那套「先点亮再量」先例）。或在 `Label` 加一个自检口 `ForceRelayoutForTest()` 走 `ForceMeshUpdate(ignoreActiveState:true)`。
⚠️ 可选的产品侧加固（要不要做由调度台定）：`SetTextTmp` 里 `if (!_tmp.enableAutoSizing) _tmp.fontSize = TmpFontSize();`，避免未激活时把已收敛值冲成标称值。它影响全仓所有自适应标签，**不建议顺手改**。

**没查清**：`QuestPoints` 在哪一步从 0 变 1 又变回 0（日志 `battle.log:35949` 只有「任务点 0 → 1」）没有逐帧追；不影响结论（未激活 + SetTextTmp 这条链已足以复现读数）。

---

## 10. `battle.log:290755` · 教学四块描边 0/4

**结论**：`ApplyThinOutline` 取 TMP 的口用错了：`Label` 的 TMP 是**子节点**（`TmpFont.NewText/NewTextUgui` 里 `new GameObject(...); SetParent(parent)`），而它在 **Label 自己的 GameObject** 上 `GetComponent<TMP_Text>()` ⇒ 恒 null ⇒ `if (t == null) return;` **静默返回**，四颗一次都没描。断言侧用了**同一个错口**，于是 `at == null → am == null → continue`，`outlined` 停在 0。

**证据**
- 实现：`Assets/CardPresentation/Battle/TutorialOverlay.cs:632` `var t = lb.GetComponent<TMP_Text>();`（`:633` 就是 `if (t == null) return;`）
- TMP 是子节点：`Core/TmpFont.cs:376-379`（`new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<TextMeshPro>()`）；`Battle/Label.cs:1321-1328` 把 `transform` 当父件传进去。
- 断言同错口：`Assets/CardPresentation/Editor/BattleScene.cs:21197` `al.GetComponent<TMPro.TMP_Text>()`；`:21199` `if (am == null) continue;`（静默跳过 ⇒ 本该报「前提不成立」）。
- 断言里另一条「共享材质没被写脏」（`:21214-21222`）是绿的，与「一次都没描」完全自洽。

**分类**：β（实现缺陷）+ 断言侧同错口（改一处不够，两处都要改）。

**修法**
1. `TutorialOverlay.cs:632` → `lb.GetComponentInChildren<TMP_Text>(true)`；
2. `BattleScene.cs:21197` → 同样 `GetComponentInChildren`，并**补一条前提闸**：`at != null && am != null` 不成立就直接红（现在是 `continue`，属于「断言可静默不跑」）。
3. 🔴 **顺手发现的第二条**（改完 1 之后才会现形）：`ApplyThinOutline` 只在 `Build` 时描一次，而 `SetAnnotations` 之后会把文本换成**本地化中文**（`BattleDriver.cs:10773-10777` → `Loc.T("Battle/Tips/MeleeAttack")` = **「近战攻击」**，见 `Core/Loc.cs:1072`）。`Label.SetTextTmp` 首句 `SyncFace()` 见汉字会把 `_tmp.font` 换成 CJK（`Label.cs:906-912`），而 TMP 换字体时 `m_sharedMaterial` 会被换成新字体的材质、`fontMaterial` getter 随即**新建一份实例**（`TextMeshProUGUI.cs:1257-1263`、`:1474-1496`）⇒ **描边丢掉**。修法：在 `SetAnnotations` 里 `lb.SetText(...)` **之后**再调一次 `ApplyThinOutline(lb)`（或把「描边配方」记在 Label 上、在 `SyncFace` 换面后重放）。
   ⚠️ 与本轮 `SetFace` 强制重排的关系：**不是 `SetFace` 的锅** —— 这里换面的是 `SyncFace`（文本变汉字那条），`SetFace` 在 `MakeLbl` 里跑在 `ApplyThinOutline` **之前**，顺序是对的。

**没查清**：批处理里那四颗文本是空串（`battle.log:290887` 显示「」，纯拉丁 ⇒ 面没翻），所以**今天这次红只由 #1 的单口错解释**；第 3 条（中文进来丢描边）是按代码与本地化表推的，**没在真机验证过**。

---

## 11. `deck.log:18880` / `:44402` · A1735⑥（4.65 / 上限 4.88）

**结论**：本轮 `TmpFont.EmPerFontSize` 改成「与字体面无关」（Pragati 0.103906 → 0.0948）后，`SetAutoFitBox` 里 `nomPx` 缩小 8.8% ⇒ `fontSizeMax = cur × maxPx / nomPx` 涨 **9.6%**。这一颗（`row_cnt*`，纯数字、`LatinCardBody`）以前是**撞上限**（装得下、TMP 不缩），现在上限比「装得下的最大号」大了 9.6% ⇒ 自适应改由**框高**约束，收敛到 4.65（= 4.88 × 0.9529）。断言断的是「仍停在上限」⇒ 红。**这条不是纯断言过期：实际渲出的字号也比本条改动前大了约 4.4%，而且现在是被框压住的。**

**证据**
- 判红：`deck.log:18880`（实得 4.65 / 上限 4.88）；`:44402` 是收尾汇总。
- 断言：`Assets/CardPresentation/Editor/DeckScene.cs:4091-4094` `tmp.fontSize >= tmp.fontSizeMax - 0.01f`。
- 标签建法与四格：`Deck/DeckRuntime.cs:1956-1986`（`Txt` → `Label.Create` 走 **cap 路径**；`SetFace(LatinCardBody)`；`SetAutoFitBox(48.82, 47.88, 18, 50, 32)`）。
- 换算链：`Battle/Label.cs:1094`（`cur`）· `:1106`（`nomPx = NominalPx()`）· `:1146-1148`（写 `fontSizeMax/Min`）；`Core/TmpFont.cs:239-242`（本轮新行为：恒 `WorldGlyphPerFontSize`）；`Label.cs:867`（`FontPxNow`）· `:853-856`（`FontSizeToPxNow`）。
- 比值对得上：`0.103906 ÷ 0.0948 = 1.09605`；4.88 ÷ 1.096 ≈ **4.45**（旧上限）< 4.65（装得下的最大号）⇒ 旧状态必撞上限、必绿。

**分类**：**γ（本轮改动引起的真行为变化）**。是否算「回归」取决于口径（见下）。

**修法与口径（需要调度台裁）**
- 若**保留** `EmPerFontSize` 的面无关口径（D1 方案 A）：这条断言必须改写。它现在的判别力是「框换小之后字没被压」，但换面/换尺之后「撞上限」不再是正确形态。建议改成断**原版实读字号**：`Label.FontSizeToPxNow(tmp.fontSize, ActualFace) ≈ 50 ± 0.06`（50 = 原版 `m_fontSize` 字面量）。按新尺，本颗现在读 **47.6px**（4.65 × 0.0948 × 108），**不是 50** ⇒ 这条新断言会红，说明**画面确实比原版小 ~5%**，得先确认这是可接受的。
- 若目标是「同一个 px = 同一个渲染大小」：注意新常量 **0.0948 不是 em**，它是 CJK 的**字墨**（0.898 em + SDF padding；见 `Core/TmpFont.cs:225-238` 的注释与 `D1` §2·2）。按 TMP 的 `fontScale = fontSize/pointSize×0.1`，物理 em 应 ≈ **0.1**：拉丁支旧值 0.1039（含 padding，偏大 ~4%），CJK 支 0.0948（字墨，偏小 ~5%）。**两个都不是精确 em**。⇒ 若真要「与面无关且等于 em」，正确值大概率是 **0.1**，而不是 0.0948。
- ⛔ 不建议为了让这条变绿去调 `fontSizeMax` 或阈值 —— 那会把「字被压小」这件事本身盖掉。

**没查清**：
1. `D1` 里没有覆盖这一条（`grep 4\.65 / A1735 / row_cnt` 零命中），所以「本轮之前它是绿的」只能由日志 + 比值推，没有上一轮的 deck 日志对照；
2. 0.1 这个「物理 em」是**套 TMP 公式的算术推断**，没有跑 Unity 实测（红线）。
3. 本条的实际观感变化（+4.4% 且被框压）建议在真机上看一眼，但那是复刻完成后的验收动作。

---

## 建议同批一起改的（同文件合并）

| 文件 | 一起改哪几条 | 说明 |
|---|---|---|
| `Editor/ShellScene.cs` | **#2 · #3 · #5 · #6 · #7** | 全在一个文件、互不重叠：`7039-7047`（N20 的 doneRw）· `7309`/`7367`（名字）· `7491`（wantArgs）· `7793`（RT 深度）。改完 `ShellScene.Run` 一次复跑即可 |
| `Editor/BattleScene.cs` | **#9 · #10（断言侧）** | `532-611` 的 `CheckHudBox` 补「未激活先点亮再量」；`21197` 换 `GetComponentInChildren` + 补前提闸。改完 `BattleScene.Run` 一次复跑 |
| `Battle/TutorialOverlay.cs` | **#10（实现侧）** | `632` 换查找口；顺手把描边挂到 `SetAnnotations` 的 `SetText` 之后（中文换面会丢实例材质） |
| `Battle/BattleDriver.cs` | **#8** | `Begin` 里 `_seed = seed` 之后补 `RefreshHudTitles()`；也可把断言消息补 `TitleTextOf(false)` |
| `Shell/ToastNotifications.cs` | **#1** | 只改 `:1000` 的哨兵上界（400 → 100000）+ 注释 |
| `Core/TmpFont.cs` / `Editor/DeckScene.cs` | **#11** | 先裁口径（保留 0.0948 / 改用 0.1 / 回滚），再决定 `DeckScene.cs:4091` 改写成什么；⛔ 别只改断言把红盖掉 |

> 复跑建议（按覆盖面，铁律 12）：上面动到 `ShellScene.cs`、`BattleScene.cs`、`ToastNotifications.cs`、`TutorialOverlay.cs`、`BattleDriver.cs`、`TmpFont.cs` ⇒ `ShellScene.Run` + `BattleScene.Run` + `DeckScene.Run`；`TmpFont.cs` 是共用件 ⇒ 按铁律 12 的「共用件先 grep」应把 `MainMenuScene/RewardsScene/ShopScene/SettingsScene/CollectionScene/CardBaseDemo` 也纳入（即全套 12 条），**由调度台在同步点决定时机**。
