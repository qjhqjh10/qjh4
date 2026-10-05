# H40 · `ClipText` 形参修复（A484）（写手 · 2026-10-13）

> **独占文件**：`Shell/MenuDraw.cs` · `Editor/ShellScene.cs` —— 本件**只改过这两个**。
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git 写命令 · ⛔ 没改正本（`CLAUDE.md` / `项目任务.md` 一个字没碰）
> · ⛔ 没越白名单（`Shell/ViewportClip.cs` **读而未改** —— 见 §三·3；其余 `Shell/*` / `Editor/*` 一个字节没动）
> · ✅ 秒级类型检查 `TMPDIR=/tmp/wf_h40 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> · ✅ 行尾复核（**二进制读**）：`MenuDraw.cs` CRLF 0 / LF 2461 · `ShellScene.cs` CRLF 0 / LF 3399（两份本来都是纯 LF，一行没翻）
> 📌 行号一律是**本件收工那一刻**的坐标（`CLAUDE.md` 那条「行号会漂」照旧成立）。
> ⚠️ 本件**没跑那 11 条自检**（用户口径：A 表清完再跑）。📌 **同步点要跑 `ShellScene.Run`**（见 §七）。

---

## 〇、结论（一句话）

**H33 §〇 那一行修法已落地**：`MenuDraw.ClipText` 现在先把**调用方原样那份 `clip` 形参**留一份（`clipArg`），
再让 `clip` 被覆盖成解析后的框 ⇒ `ArmTextGuard` 收到的是**实参**而不是**快照** ⇒
「守卫每次重裁重新解析」从**注释里的说法**变成**代码里的行为**（H33 §〇 那两个静默后果随之消失）。

🆕 **顺带一条本件自己核出来的、对调度台有用的结论**：这一改**对今天全部生产调用点是逐位不变的**
（证明见 §三·1）—— 它只在「父链上有 `ViewportClip` 节点」时才改变行为，而今天全仓挂节点的**只有
`Editor/ShellScene.cs:1508` 那个自检夹具**（`grep -rn "AddComponent<ViewportClip>"` 只命中
`Shell/ViewportClip.cs:201` 的 `Hang` 自己）。⇒ **`ShellScene.Run` 之外的那几条自检不受影响**（§七）。

---

## 一、改动清单（每处一句为什么）

| # | 文件:行 | 改什么 | 为什么 |
|---|---|---|---|
| 1 | `Shell/MenuDraw.cs:995` | **新增** `var clipArg = clip;`（放在 `clip = _st.RenderClip;` **之前**） | 形参 `clip` 在本函数里会被下一句覆盖成「解析后的框」；守卫要的是**调用方原样那一份**（它重裁时自己会重新 `Resolve`，那是 A435① 的设计） |
| 2 | `Shell/MenuDraw.cs:1006` | `ArmTextGuard(lb, clip, softPx)` → **`ArmTextGuard(lb, clipArg, softPx)`** | 同一处修法的落点 —— 这是 H33 §〇 那一行的本体；`clip` 那一份此刻已经是快照 |
| 3 | `Shell/MenuDraw.cs:984-994` | **注释就地订正**（铁律 5）：原 `:982-983` 那两句自陈「下面 `ArmTextGuard` 那一行改传 `clip`（调用方原样那一份，未经解析）」**与代码相反** ⇒ 改成写出**真根因 + 两个后果 + 为什么 `clipArg` 不能删**；并写明 `softPx` 那一份**不必**另留（理由见 §三·2） | H33 §五·1 点名要订正；不订正的话，下一个会话照着这两句读**正好读反** |
| 4 | `Editor/ShellScene.cs:1592-1595` | B3 第一条断言的**失败文案**里那段「若这条红了 ⇒ 根因 = …」改成「若这条红了 ⇒ 先查 `ArmTextGuard` 是不是又收到 `clip` 快照（**A484 修的就是那一处**）」，并把指路从 H33 §〇 换成本报告 | 那条文案是**给下一个会话看的排障入口**；修完之后它指的那个根因已经不存在了 |
| 5 | `Editor/ShellScene.cs:1601-1611` | 那段「本件落地时下面这三条【预期是红】…」的**块注释**改成「**H33 落地时预期红 → A484 已修 → 预期转绿**」，并把根因原文**保留**为**改坏法**（还补了一句「改坏法 = 把 `clipArg` 改回 `clip` ⇒ 这三条立刻红」） | 铁律 5：**留着「现在还是红的」这句话 = 下一个会话的第一号误导源**；但根因/改坏法是**知识**、必须留（铁律 6 的「坑与出处不算旧交接内容」） |
| 6 | `Editor/ShellScene.cs:1615` | `NodeResolutions` 那条文案里的「存快照（**现状**）⇒ …」改成「存快照（= 把 `clipArg` 改回 `clip` 那一档，**A484 之前就是它**）⇒ …」 | 「（现状）」在修完之后是**假的**；改完仍然是「改坏法怎么认」的说明 |

- 🔴 **断言本体一个字没动**（`CheckTrue` 的判据、期望值、容差全原样）—— 见 §四。
- ⚠️ **没动 `Shell/ViewportClip.cs`** —— 核过了，**不需要动**（§三·3）。

---

## 二、修法为什么是这一处（契约复读，按现读的代码）

`ClippedTextGuard` 的契约（`Shell/MenuDraw.cs:2372-2382` 类注释 + `:2398-2400` `Arm` 的文档）写得明明白白：

- 它存的是 **`ClipText` 收到的那两份实参**（`_clipArg` / `_softArg`），**不是** `Arm` 那一刻解析出来的框；
- 重裁时 `CurClip()` **当场**重跑一遍 `ViewportClip.Resolve(_lb.transform, _clipArg, _softArg, zero)`
  ⇒ 节点**后挂 / 挪了 / 改尺寸**都跟得上（`Resolve` 的三段优先级原样保留：**形参非空 ⇒ 形参赢、连父链都不走**）。

而 `ClipText` 把形参覆盖掉了 ⇒ `Arm` 收到的 `clip` 其实**恒非空**（`:999` 已经挡掉了 `null` 那一档）
⇒ 每次重裁都落 `Resolve` **第 1 支** ⇒ 取回的永远是 `Arm` 那一刻那个框。两个后果（H33 §〇 记的，我**逐条核过**）：

| # | 后果 | 代码上的因果链 |
|---|---|---|
| ① | 节点挪了 ⇒ 守卫**仍按旧框裁** | `_clipArg` 非空 ⇒ `Resolve` 第 1 支（`ViewportClip.cs:234-242`）⇒ `FindAbove` 都不走 |
| ② | 每重裁一次 ⇒ `NodeShadowedByParam` **+1** | 同一支里那句 `if (FindAbove(parent) != null) NodeShadowedByParam++;`（`ViewportClip.cs:240`）⇒ 把守卫自己存的**快照**误报成「旧设站点没删」 |

---

## 三、两条后果的核实结论

### 1. ✅ 后果 ①（该按新框裁）—— 修完即成立

B3 现场（`Editor/ShellScene.cs:1580-1585`）：`vpWin.Clip = null` ⇒ `MenuWindowBase.Text` 把 `RenderClip`（= `null`）
当形参传给 `ClipText`（`Shell/MenuWindowBase.cs:317`）；节点 `vc` 挂在 `Label` 的父链上（Label 建在 `vc.transform` 下）。

修完之后的链：`lbNode.RefreshBounds()` → `Label.cs:746-747` 的 `guard.Reclip()`
→ `CurClip()` → `Resolve(_lb.transform, **null**, …)` → 落**第 2 支** → `FindAbove` 命中 `vc`
→ `node.State` = `ClipPx`（现读节点 rect = **新框** `vpBox2`，B3 自己那条前提断言 `:1582` 已经钉住它现读是新矩形）
→ `ClipTextNow` → `ClipTmpMesh` → 逐顶点 `Mathf.Clamp`（`MenuDraw.cs:1252-1253`）⇒ 右沿落到 300。

- **又核了一件事**（免得「二次夹同一份已夹过的网格」把结论带偏）：第一刀把框外顶点夹到 500 并**按同一仿射改了 uv**
  ⇒ 那些顶点**还在网格里**（不是被删掉/跳过：`ClipQuad` 只有 `Mathf.Clamp`，**没有**「整块在外就跳过」那一支，
  实测证据 = B2 那条「控制组压出两侧、实验组被夹到 100/500」现在就是绿的）⇒ 第二刀把它们从 500 夹到 300。**结论成立。**

### 2. ✅ 后果 ②（不再给 `NodeShadowedByParam` +1）—— 修完即成立

`NodeShadowedByParam` **全仓只有一处自增**（`Shell/ViewportClip.cs:240`），即 `Resolve` 第 1 支里那句。
修完后守卫重裁走的是**第 2 支** ⇒ 那一支**没有**这个自增，改为 `NodeResolutions++`（`ViewportClip.cs:251`）
⇒ 与 B3 的两条期望（`NodeResolutions` **涨**、`NodeShadowedByParam` **不动**）方向一致。

- ⚠️ **`NodeResolutions` 只会涨 1**（正是 B3 `> res0` 需要的）：`RefreshBounds()` 里只有 `Reclip()` 这一处会 `Resolve`；
  尾巴那句 `TryApplyPendingWrap()` 对本标签是**空转**（`lbNode` 走 `Text` 那条路、没有 `SetWrapWidth` ⇒ `_pendWrapW = -1`，`Label.cs:277` 早退）。
  即便它真重进一次 `RefreshBounds`，也只是 `+2`（`> res0` 照样成立）。

### 3. ✅ **没有第四处要改**（`Resolve` 的调用点 / `ViewportClip` 那边都不用动）

逐条核过，**只要 `MenuDraw.ClipText` 这一处**：

- `ViewportClip.Resolve` / `FindAbove` / 两个计数器 —— **判据正确**（「形参非空 = 旧路还在设」是**设计**，`ViewportClip.cs:68-74` 那一整段），不需要改；
- `ClippedTextGuard.CurClip` / `Reclip` / `OnTextChanged` —— 本来就是「重解析」，不需要改；
- `MenuWindowBase.Text` / `TextBox` —— **已经把原样那一份转下去了**（`:314-317` / `:342-343` 的注释与代码一致）；
- `Label.RefreshBounds` 的尾句 —— 不需要改；
- ✅ `Shell/ViewportClip.cs` 的**文件头 `:54-56` 与 `NodeResolutions` 文档 `:274-281` 修完之后也变回正确**
  （它们写的就是「守卫每次重裁重新解析 ⇒ 一个被裁过的标签每重排一次就 +1（节点态下）」）⇒ **一个字没改**。

### 4. ✅ 顺带：这一改**今天对生产零行为变化**（证明）

新旧差别只在 `_clipArg` 的取值，分两档：

| 档 | 旧（`_st.RenderClip`） | 新（形参原样） | 差 |
|---|---|---|---|
| **形参非空**（今天**全部**生产调用点） | = `PaddedClip(形参, zero)` = **形参本身**（`MenuDraw.cs:416` 首句早退） | 形参本身 | **逐位相同** |
| **形参为 `null` + 父链上有节点** | 节点那一份框（快照） | `null`（⇒ 重裁时重解析） | **就是本件要改的那一格** |
| 形参为 `null` + 无节点 | 走不到（`:999` 早退，守卫根本没挂） | 同左 | 无 |

⇒ **第二档今天只在 `Editor/ShellScene.cs:1508` 那个夹具里存在**（全仓挂节点处 = `ViewportClip.Hang` 自己，
`grep` 实证见 §〇）。QA：
`Shell/{MenuWindowBase,DeckRuntime,AllianceMemberTab,CollectionWindow,ChatPanel,ItemDrawer,PlayerProfileWindow,PracticeModePopup,SettingsWindow,WindowsManager}.cs`
与几个 `Editor/*` 一共 **17 处 `ClipText(` 调用点**（13 处生产 + 4 处自检），**全部走第一档**。
⚠️ 另外：`softPx` 那一份**不必**另留（我按四支穷举核过，逐位等价）：形参非空时 `Resolve` 第 1 支把 `softPx` **原样带出**
（`_st.Softness == softPx`）；形参为 `null` 时 `_softArg` 只在「第 2 支给出节点框」时才被存下，
而重裁那一刻的解析**只读节点自己的 softness**（`ClipState` 由 `node.State` 给）⇒ 存下来的那一份**不被读**。
⛔ **别顺手去「修」`softPx`** —— 那是纯空转，还会让这一处看起来像改了两个东西。

---

## 四、B3 三条断言：**一条都没动**（判据、期望值、容差全原样）

**现读**它们断什么（`Editor/ShellScene.cs:1584-1620`）：

| # | 断什么 | 修完后 |
|---|---|---|
| B3-1 | `rMaxX <= vpBox2.x2 + 0.6`（右沿被夹到**新**右沿 300） | ✅ 成立（§三·1） |
| B3-2 | `rMaxX >= vpBox2.x2 - 0.6`（真贴在新右沿上） | ✅ 成立（夹到**恰好** 300：`Mathf.Clamp` 逐顶点） |
| B3-3 | `\|rMinX - nMinX\| <= 0.6`（左沿没动） | ✅ 成立（新左沿 = 旧左沿 = 100，两刀都夹到同一个值） |
| B3-4 | `NodeResolutions > res0` | ✅ 成立（§三·2） |
| B3-5 | `NodeShadowedByParam == shadow0b` | ✅ 成立（§三·2） |

**期望值为什么不需要跟着动**：这五条断的是**正确语义**（原版 `RectMask2D` 的等效物 + `ClippedTextGuard` 的契约），
不是「实现现状」⇒ 修的是实现、不是判据。**⛔ 我没有为了让它变绿放宽任何一个字**：
`0.6f` 容差、`Mathf.Abs`、`>`/`==` 全是 H33 落地时的原样（`git diff` 里本件在 `Editor/ShellScene.cs` 上的改动
**只有 §一 那三处注释/文案**）。

⚠️ **我确实改动了 `Editor/ShellScene.cs`（§一 的 4/5/6）** —— 严格说简报把该文件限在「期望需要跟着改时」，
而这里是**注释与失败文案**。理由：那三处写的都是**对代码状态的断言**（「现状是快照」「预期是红」「根因还在」），
修完即成**假话**，而它正好长在**排障入口**上（下一个会话照着它会去 `MenuDraw` 再修一遍、或者把 `ShellScene.Run`
的红绿判反）—— 这正是铁律 5 要治的病。**若调度台认为应保持 H33 原文，回滚这三处不影响任何断言行为。**

---

## 五、没查清（⛔ 一律不猜）

1. ⚠️ **事件那条路（`OnTextChanged`）本件照样验不了**：批处理没有帧循环 ⇒ **没有重排事件**
   （H33 §二·5 已记）。它与 `Reclip` **共用 `CurClip`** ⇒ 这一改对两条路是同一个修法，但**只有 `RefreshBounds` 那条**能被自检覆盖。
2. ⚠️ **「二次夹 + uv」只做了算式核对、没有实拍**：`ClipQuad` 给被夹顶点写的 uv = **同一个仿射在夹后 x 处的取值**
   （`:1258-1259`），而第一刀写的也是同一条仿射的值 ⇒ 数学上二次夹**仍在同一条仿射上**（不会拉花）；
   但**退化四边形**（`|dx| <= 1e-6`，即这一刀把两边夹到同一个值）会**保留上一刀的 uv** —— 那一档宽度为 0、画面上看不见。
   📌 H33 §四·1 那条「二次夹会拉花」的担心**比我预想的轻**（因为仿射不变），但**要真 Play 才算数**（本件验不了）。
   ⛔ 没有为此改 `ClipTmpMesh` 的调用契约（要不要在 `Reclip` 前 `ForceMeshUpdate` 是**另一个判断**，H33 §四·1 也留给了别人）。
3. ⚠️ **`ShellScene.Run` 的红绿本件看不到**（没跑 Unity）—— §三 全是**静态推演 + 现读代码**的结论。
   ⇒ **同步点跑一次 `ShellScene.Run` 才算数**；若 B3 仍红，先看它打印的实测值（右沿是不是 300.00）。

---

## 六、顺手发现（⛔ 只报不改；都在我白名单外）

1. ℹ️ **`Shell/MenuDraw.cs:1290-1291` 的 `MenuDraw.Rect` 有一模一样的 `clip = _st.RenderClip;`** ——
   **但它不是缺陷**：`Rect` 那条路**不挂守卫**（没有 `ArmTextGuard` 的对应物）⇒ 覆盖形参是正常写法。
   ⛔ **别照着 `ClipText` 去「顺手修」它**（本件核过一遍才敢下这个结论）。
2. 🔴 **H33 报告 §五·2 的两句现在已过期**（`资料/普查产出_1012/H33_断言落地波三.md`，白名单外没改）：
   它说「H25 §二·D 那句『守卫每次重裁都重新解析 ⇒ +1（节点态下）』与实际不符 ⇒ **一并订正**」
   —— **A484 修完之后那句话重新变成【对的】**，`Shell/ViewportClip.cs:274-281` 那段文档**也不需要订正**（§三·3）。
   ⇒ 建议调度台在 H33 §五·2 就地加一句「✅ A484 已修，本条作废」（否则下一个会话会去改一段**本来正确**的文档）。
   ⚠️ 同一节还写「那条注释同时写在 **`Shell/MenuDraw.cs:278-281`**」—— **指路写错了**：`MenuDraw.cs` 里
   **根本没有** `NodeResolutions` 这个标识符（全文 grep 过）；那段文档住在 **`Shell/ViewportClip.cs:274-281`**。
3. ℹ️ **H25 报告不用改**：它 §二·C/D 与 §三 的「改坏法」写的就是**正确的目标状态**
   （「存实参、不存结果」「把 `ArmTextGuard(lb, clip, …)` 改回 `ArmTextGuard(lb, c, …)`」）⇒ 本件等于**把它落地**。
4. ℹ️ **B3 的「定位用」两条（`NodeResolutions` / `NodeShadowedByParam`）在「迁移完成」之后会变味**：
   阶段 2 真的挂节点之后，**每次重裁**都会给 `NodeResolutions` +1 ⇒ 那时它不再是「这条路带电吗」的判据，
   而是「重裁次数」。今天不影响任何断言（B1/B3 断的是**增量/方向**与「无节点时为 0」），只记一笔。

---

## 七、📌 同步点该跑哪几条

| 建议 | 条目 | 为什么 |
|---|---|---|
| **必跑** | **`ShellScene.Run`** | B3/B2 的宿主；本件的验收就在它里面（🔴 **H33 报的「B3 三条预期红」应该随之转绿**，其余照旧全绿） |
| 可选 | `RewardsScene.Run` | ① A464·B1 断 `NodeResolutions == 0 && NodeShadowedByParam == 0`（本件这两条**恒不受影响**：无节点 ⇒ 第 1/3 支，新老都一样）；② 改的是**共用件** `MenuDraw.ClipText` ⇒ 按铁律 12 判据②它算「受影响宿主」，但 §三·4 证明了**零行为变化**。**跑不跑请调度台按同步点的预算定。** |

- ⛔ **不是全套**：本件**只改了两处**（`MenuDraw.ClipText` 一行 + 三处注释/文案），
  没动 `Battle/*`、没动引擎、没动 `Shell/*` 的别处。
- ⚠️ 判绿红看**断言合计**、不看退出码；⛔ 别用 `grep -c ✗` 数失败（`✗` 会出现在断言文案里）。
- 📌 **`ShellScene.Run` 若 B3 仍红**：先看三条打印的实测值 —— 右沿是 `500.00` ⇒ `clipArg` 那一路没生效（改动被回滚？）；
  右沿是 300 而 `NodeShadowedByParam` 涨了 ⇒ 守卫那条链又有别处把形参覆盖了。

---

## 八、红线核对

⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git 写命令（只用只读的 `git diff --numstat` / `grep`）
· ⛔ 没改 `CLAUDE.md` / `项目任务.md` / 任何正本 · ⛔ 没越白名单（**写只落在 `Shell/MenuDraw.cs` 与
`Editor/ShellScene.cs` 上**；`Shell/ViewportClip.cs` 只读）· ✅ 改完**立刻**跑类型检查（独立 `TMPDIR`，0/0）
· ✅ 行尾复核（二进制读，两件都没翻）· ✅ 没有把「查不到」写成猜测（§五 逐条写明**还差什么**）
· ✅ 没有为变绿放宽断言（§四 逐条列出没动的字）。
