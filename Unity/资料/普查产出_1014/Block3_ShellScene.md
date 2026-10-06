# Block3 · `Editor/ShellScene.cs` 15 条红（清单 #12–#26）—— 写手报告

> 2026-10-14 · 写手代理 B3 · **独占文件 = `Assets/CardPresentation/Editor/ShellScene.cs`**（本批只改了这一个）
> 判据：`资料/普查产出_1014/清单_90条红改法.md` §一·3（#12–#26）+ `资料/普查产出_1013/D1013_诊断_块2_Shell与Collection.md` §二（#1–#15）
> 行尾：`git show HEAD:Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs | file -b -` = **纯 LF**；改完实测 `CRLF 0 / LF 4937` ✓
> 类型检查：`TMPDIR=/tmp/wf_b3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了两次：改动中、改动后各一次）
> ⛔ 没跑 Unity · 没动 git · 没改正本 · 没碰 `Battle/Label.cs`（调度台已改完）· 没碰别的代理的五个宿主

---

## 一、逐条

| # | 改了什么（文件:行） | 判据 | 做完没有 |
|---|---|---|---|
| **12** | `Editor/ShellScene.cs:3802-3819`（**新增**态一「压边」断言）＋ `:3856-3864`（态三文案订正）。**实现侧 `Battle/Label.cs` 由调度台改完，本代理一个字没动** | 清单 #12 · D2 #3；`Shell/InboxWindow.cs:54` 的 `MsgList`；`Battle/Label.cs:839-843 ReclipNow` | ✅ |
| **13** | `Editor/ShellScene.cs:3869-3894`（态四：**夹具 + 期望一起换**） | 清单 #13 · D2 #4（「期望要换成真·未裁值；别继续写 851.44」） | ✅ |
| **14** | `Editor/ShellScene.cs:3437-3444`：`MaxQuadBottomPx` 的 `foreach` 体首插一行跳过空件与未激活件（`if (q == null · !q.gameObject.activeInHierarchy) continue;`，照同文件 `GUnion:4516` 抄） | 清单 #14 · D2 #1；`Shell/MenuDraw.cs:1409`（函数体 `:1393`，`ClipNineChildren` 整块在框外 ⇒ `SetActive(false)`、不删节点） | ✅ |
| **15** | **不用改**（#14 的级联）。已**读断言核过**：态一 `b0 ≈ vpProd.y2`（`Editor/ShellScene.cs:3516`）**仍然绿** —— 911 那个读数来自**第 10 行被夹到视口下沿的那颗本体 quad（`activeInHierarchy = true`）**，被跳掉的只有第 9 行**已 `SetActive(false)` 的底边条**；态二修完 `b1 = 811.00` ⇒ `b0 − b1 = 100 > 99`（`:3533`）自动转绿 | D2 #2 | ✅（零改动） |
| **16** | `Editor/ShellScene.cs:4405-4414`：`content` 取法限定到 `AllFactions` 那一支 ⇒ `FindChildIn(FindChildIn(pgRk2.transform, "AllFactions"), "content")` | 清单 #16 · D2 #5；`Shell/RankedTab.cs:285`(`Top4/content`) vs `:417`(`AllFactions/.../content`)；`FindChildIn` 是 `GetComponentsInChildren` **取第一个**（`:229-235`） | ✅ |
| **17** | `Editor/ShellScene.cs:4438`：`var rw2 = RewardsWindow.Create(shell.Windows);` **之后**插 `shell.Windows.OpenWindow(rw2);` | 清单 #17 · D2 #6；`Shell/RewardsWindow.cs:210` 的 `Create` 只 `AttachToAnchor`、不调 `mgr.OpenWindow` | ✅ |
| **18** | `Editor/ShellScene.cs:4613`：`var lbG = LeaderboardWindow.Create(...)` **之后**插 `shell.Windows.OpenWindow(lbG);` | 清单 #18 · D2 #7；`Shell/LeaderboardWindow.cs:210-224` 同形（不自开窗） | ✅ |
| **19–23** | **不用改**（#18 的级联）。窗口一打开 ⇒ `armSelG`/`armContentG`/`armVcG`/`h0G` 全部可取 ⇒ 五条自动恢复判别力。**实现侧本来是对的**（`Shell/LeaderboardWindow.cs:488` 的 `_armyContent = Node(vpVc.transform, …)`、`:498` 的 `_armyScroll.ClipNode = vpVc;` 都在，只是从没执行过） | D2 #8–#12 | ✅（零改动） |
| **24–26** | **本代理一个字不用改**（实现缺陷在 `Shell/PurchasePremiumWindow.cs`，调度台已修）。**已现读复核落地**：`PurchasePremiumWindow.cs:308` = `Build();`（在 `Initialize();` **之前**，顺序对）· `:512` = `ViewportClip.Hang(sv, "Viewport", …)` · `:525` = `_scroll.ClipNode = vpVc;` ⇒ **等复跑** | 清单 #24–#26 · D2 #13–#15 | ✅（零改动，待复跑） |

**合计**：15 条里 **6 条动了 `Editor/ShellScene.cs`**（#12 #13 #14 #16 #17 #18 —— 落成 **7 处**编辑：#12 拆成「态一新增 + 态三文案订正」两处）、
**9 条零改动**（#15 级联 · #19–#23 级联 · #24–#26 实现侧由调度台改完）。
`git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` = **82 插入 / 10 删除**（含大量「判据 + 改坏法 + 更正痕迹」注释）。

---

## 二、#12/#13 到底改了什么（这一段是本批唯一要动脑的地方）

### 复算（动手前先算清「修完之后会量到几」）

从日志实得值反推（D2 #3 的三个数）：
`态一 = 851.438 = MsgList.x2(728.28) + 123.158` · `态三 = 660.240 = cutX(537.085) + 123.155` · `态四 = 954.83`
⇒ 位移是**纯平移 +123.16**，且三个态**逐位相同** ⇒ 裁切框整个被 A804 推走。反推得：

- 这段字**在 AlignLeft 之后**的自然跨度 = `[222.73, 954.83]`（`954.83 − 222.73 = 732.10` = 对齐前的 `831.67 − 99.57`，两边自洽）；
- **A804 修完之后**（= `AlignLeftOn` 尾句补 `ReclipNow()`）应当量到：
  - 态一 = `min(954.83, MsgList.x2 728.28)` = **728.28**（**压边**）· 左沿 = `222.73`（`MsgList.x1 = 99.57` 夹不到它）
  - 态三 = `min(954.83, cutX)` = **cutX**，而 `cutX = (aMinX + aMaxX)/2` 自己会**跟着变**（537.085 → **475.51**）——夹具是自适应的，⛔ **别拿旧日志的 537.08 去对**
  - 态四（新夹具） = `954.83`（真·未裁值）

### 落地的改法

1. **态一：新增一条「压边」断言** —— `CheckNear(aMaxX, InboxWindow.MsgList.x2, 1.0f)`（`if (natOk)` 守着）。
   🔴 **这一条是判别式**：A804 一回归，它立刻量到 `x2 + 123.16` ⇒ 红。
2. **态三：期望 `CheckNear(c3, cutX, 1.0f)` 一个字没改**（修完之后它本来就对）；只把文案里那句错的
   「**态一未切时是 `aMaxX`**」就地订正成「态一（同一条数据、框放到整条 `MsgList`）**压边**量到 `aMaxX`」（保留更正痕迹）。
3. **态四：夹具与期望一起换。**
   - 夹具：`new PxRect(aMinX-200f, aMinY-200f, aMaxX+200f, aMaxY+200f)` → **`new PxRect(aMinX-4000f, aMinY-200f, aMaxX+4000f, aMaxY+200f)`**
     （理由：旧夹具的 `aMaxX` **本身就是被裁过的值** ⇒ ① 旧文案「回到未切值」是假的 ② 修完后 `aMaxX+200` 整个落进字里）。
     新框**一定包得住整段字** ⇒ **「另测的未裁宽度」由态四自己给**（= 诊断给的第二条路）。
   - 期望：`CheckNear(d3, aMaxX)` → **`CheckTrue(d3 > InboxWindow.MsgList.x2 + 50f)`**
     （「形参赢」⇒ 右沿**越过节点框**；「节点赢」会正好落在 `MsgList.x2` 上 ⇒ 相距 ~226px，判得出）。
   ⛔ 两条都**没有放宽容差、没有删断言**；判别力见下节。

### ⚠️ 顺手发现①：诊断建议的「相对差」**挡不住 A804 回归**（这是一条判据订正）

D2 #3/#4 建议把态一/态三的对照改成 **相对差** `态一右沿 − 态三右沿 == MsgList.x2 − cutX`。
**实测复算：这条在 A804 回归时照样成立** —— 两个态**同量**偏 `+123.16`，相减时被约掉：
`851.438 − 660.240 = 191.198` vs 期望 `728.28 − 537.085 = 191.195`（差 **0.003** < 容差 1.0）⇒ **绿**。
⇒ 相对差是**比现状更弱**的断言（回归时它会「假绿」）。所以本代理落的是**绝对钉法**（态一 = `MsgList.x2`），
它在数学上**蕴含**相对差那条（给定态三 = `cutX`），但反过来不成立。这句话也写进了代码注释（`:3817-3818`）。
**⇒ 建议同步点把「清单 #12/#13 建议改成相对差」这句就地订正**（本代理无权改正本）。

---

## 三、顺手发现（⛔ 只报不改）

1. **同形但【故意】的，别去「修」**：`Editor/ShellScene.cs:1148 padWin` 与 `:1522 vpWin` 都是
   `RewardsWindow.Create(...)` **紧跟着没有 `OpenWindow`** —— 但它们**是刻意的夹具**
   （`:1521` 的注释明写「同 §⑤·b 的 `padWin` 那种取法：`Create` 出来**不开窗**，直接当夹具用」；
   两处都是在窗根下挂**临时探针节点**，不读窗自己的树）。⛔ 别照 #17 的形状顺手补 `OpenWindow`。
2. **本文件里 `Create` + `OpenWindow` 的配对现已全对**：`:3621` 的 `LeaderboardWindow.Create` 后面有
   `OpenWindow(lb)`（`:3622`）✓；`:4388`（档案窗）/`:4422`（榜单 ②）✓；本次补的两处 ✓。
   ⇒ 清单 #17/#18 说的「只有这一处漏」在**本文件范围内成立**。
3. **清单 #26 的「置信度中高」对本宿主不适用**（可以升为「高」）：ShellScene 那一条**不断言「2 个容器」** ——
   `:4744` 只是 `ppwG.Containers.Count > 0 ? Containers[0] : null`，`:4748` 断「命中区量得到、不是退化矩形」。
   ⇒ 「2 条报价 ⇒ 2 个容器」这个**没实测过的推导不在本宿主里**（它是 `Editor/ShopScene.cs` 的事）。
4. **态三的 `cutX` 会变**：修完之后 `aMaxX` 从 851.44 → 728.28 ⇒ `cutX = (222.73+728.28)/2 = 475.51`
   （旧日志是 537.085）。复跑时看日志的人别把这两个数对错（夹具是按**实测跨度**自算的）。
5. **`[aMinX-4000, aMaxX+4000]` 这个「宽框」不会触发任何副作用**：`ib.ClipPad = (0,0,0,0)`、
   `ib.ClipSoftness = (0,0)`（同段 `:3775` 已断过）⇒ 框只是「包住整段字」；
   框宽到没有角被夹 ⇒ `ClipTmpMesh` 一个字都不写（`any == false`、不 `UpdateVertexData`）⇒ 不会 NRE。
6. **`InboxWindow` 那颗视口节点的 `softness = (0,25)`**（`Shell/InboxWindow.cs:87` 的常量 `ListSoft`，
   用在 `:292` 的 `ViewportClip.Hang`）⇒ 软边只在 **y** 方向、**不影响**本段三条断言的 x 判据
   （顺带说明：x 方向是硬边 ⇒ 夹出来的右沿是**精确**等于框沿的）。

---

## 四、没做完的 / 判不了的（如实标）

1. **⛔ 一条都没跑 Unity**（本批禁止）⇒ 上面所有「修完之后应当量到 X」都是从日志实得值**反推**的，
   **未经实测**。复跑时请优先看这三条：
   `态一 = 728.28`（新那条）· `态三 = cutX`（`cutX` 会是 475.51）· `态四 > 778.28`。
2. **反推里唯一没验算的一步**（D2 §四·1 也标了）：`态一` 读数「自然右沿 > `MsgList.x2`」——
   由「A804 未修时态一**精确**等于 `x2 + 123.16`（说明当时**发生了夹切**）」反推，**不是**直接量的自然宽度。
   ⇒ 若复跑后新那条「态一压边」红、且实得 ≈ 954.83（= 没夹切），那说明这段字的自然右沿其实比框窄、
   该断言的前提不成立（概率低：旧日志里它被夹过）。
3. **`#24–#26` 只看得到「实现侧在位」、看不到「真跑出 2 个容器」** —— 那要等 `ShellScene.Run` 复跑（本代理无权跑）。
4. **`#15` 的「态一仍然绿」是读代码推的**（哪颗 quad 被 `SetActive(false)`）—— 若复跑后态一也红了，
   那说明 911 那个读数也来自已关掉的件，届时按「读数器只跳 inactive」这条口径再核一次即可。
