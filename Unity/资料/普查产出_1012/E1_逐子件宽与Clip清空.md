# E1 · A239（`HorizontalChild` 假定等宽）+ A353（`CampaignTab` 四处不设 `Clip`）（写手 · 2026-10-12）

> 白名单 = `Core/UguiRect.cs` · `Shell/CampaignRewardWindow.cs` · `Shell/CampaignTab.cs`（+ 本报告）。
> ⛔ 没动 `Shell/WindowsManager.cs` · `Editor/RewardsScene.cs`（别人在写）· 其它任何文件 · git · Unity · 两张正本。
> 行号 = **本件改完那一刻的现读**（A 表/S4 报告里那套行号已整体漂了 —— 见 §五·2）。
> ⚠️ **`Editor/RewardsScene.cs` 的行号本件读到过两套**（R1 那一刻正在改它：同一句「2135..2380」先读到 `:4351`、
> 后读到 `:4534`，**整篇涨了 ~184 行**）⇒ 该文件**只认锚点句、⛔ 别认行号**（本报告里凡是引用它，
> 一律同时给出锚点句）。

---

## 一、结论

### A239 ✅ 做完 —— 共用件加「逐子件宽度数组」重载，两个调用点改走它
- `Core/UguiRect.cs`：新增 `HorizontalChild(PxRect, float[] childWs, float childH, int index, padLeft, spacing)`
  与它的 `OwnHeight` 转发版；**旧签名（`float childW`）保留，算式逐字未改**（连求值顺序都一样）
  ⇒ 旧调用点**逐位零行为变化**。
- `Shell/CampaignRewardWindow.cs` 的 `BuildColumn`：槽号不再由「自增的 `k`」说了算，**一律取那张宽度表的下标**
  （`ws` 同时喂 `HorizontalContentW` 与四件的位置）。
- 🔴 **这一件其实是两半，两半都得修**（第②半是波 C1 没看出来的）：
  ① **算式错**：等宽式 `left = 容器左 + padL + (childW + spacing) × index` 在**混宽列**上只有 `index 0` 对；
  ② **槽号错**：旧 `k` 在 `!isBase` 时**无条件**替 `Warning` 占一格，而宽度表只在 `PremiumLocked` 时才收
  `WarnW` ⇒ **不锁时整体错一格**（徽标拿到的是「警告那一格」）。只修①、不修② ⇒ **徽标仍然错**。
- **实到矩形（独立重算，见 §六）**：1 件物品的高级列（`hr.x1 = 1025`：列左 960 + `HolderGap` 65；`padL 30`、`spacing 25`）
  | 件 | 改前实到 | **改后实到** | 期望 | 偏 |
  |---|---|---|---|---|
  | `Unlock Button`（槽 1） | 1325..1570 | **1280..1525** | 1280..1525 | **0** ✅ |
  | `Badge`（槽 2） | **1430..1530** | **1550..1650** | 1550..1650 | **0** ✅ |
  | 物品格（槽 0） | 1055..1255 | 1055..1255 | —— | 不变（本来就对） |

  （`Warning` 槽 2、不锁时它不在表里 ⇒ `iWarn == iBadge`，见 §二·B 的注释。）

### A353 ✅ 做完 —— 四处显式清 `Clip` / `ClipPad` / `ClipSoftness`
- `Shell/CampaignTab.cs` 内新增一对 `ClearClip()` / `RestoreClip(snap)`（**本文件私有**，⛔ 没往
  `Shell/WindowsManager.cs` 加 —— 那会让它变共用件、扯出全套自检）；四处**成对**包起来：
  ① `Campaign Background/Background Image` · ② `Campaign Army Selector/Background` ·
  ③ `Campaign Header` 整棵（含 `Title`/`Points` 两段 `_win.Text`）· ④ `BuildPremiumPanel` 整块。
- 判据 = 原版实读：路径含 `Campaign Tab` 的 `RectMask2D` **只有两条视口**
  （`Campaign Track/Viewport` · `Campaign Army Selector/Viewport`）⇒ 这四处**原版没有 mask**
  ⇒ 正确值就是 `Clip == null`。**本件独立复扫过一遍**（§六），只有那两条（各 2 个来源路径）。

### 🔴 A353 点名要的「今天到底有没有真漏值」= **第 1 种可能：成对还原挡住了 ⇒ 今天不泄漏**
逐条证据（⛔ 不是「读着像」）：
1. `Clip` 的**初值**是 `null` —— `public PxRect? Clip;`（`Shell/WindowsManager.cs:147` 声明在 `GameWindow` 上，
   `PxRect?` 的默认值就是 `null`；`ClipPad` = `default(Vector4)` = 零、`ClipSoftness` = `default(Vector2)` = 零）。
2. **这一个 `RewardsWindow` 实例上，写这三个字段的只有两处文件、五段，且每一段都成对还原**：
   `Shell/ForgeTab.cs:572/609`（阵营条，存 `Clip`+`Soft` 两件、还原两件）·
   `Shell/ForgeTab.cs:658/674`（奖励轨道，存三件、还原三件）·
   `Shell/CampaignTab.cs:326/342`（`BuildTrack`）· `:619/656`（`BuildArmyItems`）· `:741/764`（`RefreshNodes`）
   —— 逐段读过：**保存与还原之间没有任何 `return`**（只有循环内的 `continue`；`BuildNode` 的早退在循环体内、
   不会从 `BuildTrack` 里逃出去）。
3. **`Shell/MissionsTab.cs` · `Shell/RewardsWindow.cs` · `Shell/MenuWindowBase.cs` ·
   `Shell/WindowsManager.cs`（`GameWindow` 本体）对这三个字段一次都不写** ——
   `grep -n "Clip\s*=\|ClipPad\s*=\|ClipSoftness\s*="` 在这四个文件里 **0 命中**。
4. 四处的**语句位置**也落在「上一对已还原」之后：①②③ 在 `Build()` 里排在 `BuildTrack()`（第 ④ 步）**之前**；
   ④ `BuildPremiumPanel` 排在它**之后**（而 `BuildTrack` 会还原）⇒ 两种情况拿到的都是 `null`。
⇒ **结论：这四处是「潜伏型」缺口（与 A326 同形），今天的可见行为零变化**；清空是把
「这一处没有 mask」写死在这一页自己身上（下一个不还原的写入方 / 哪条路上提前 `return` 时才现形）。
⚠️ **另一半如实说**：三件里**今天只有 `Clip` 那一条能单独起作用** —— `MenuDraw.Rect` 只在
`clip.HasValue` 时才求交/采软边、`MenuWindowBase.Text` 只在 `RenderClip.HasValue` 时才 `ClipText`、
`MenuDraw.PaddedClip(null, pad)` 第一句就返 `null`（`Shell/MenuDraw.cs:1212+` · `Shell/MenuWindowBase.cs:286`）。
清 `pad`/`soft` 是**把「这一处没有 mask」写全**（同 `RefreshNodes` 里那句「显式写出来的『本来就是 0』」），
⛔ 不是今天多出来的行为 —— §三的「改坏法」按这一条如实写。

---

## 二、改动清单（文件:行 · 每处一句为什么）

### A. `Core/UguiRect.cs`（共用件）
| 位置 | 改了什么 · 为什么 |
|---|---|
| `:105-109`（旧签名 `HorizontalChild`） | **算式逐字未改**，只把「造矩形」那三行抽成 `HChild(left, cy, w, h)`；加两行注释指明「只对全同宽成立」。为什么：`HChild` 是**唯一一份**矩形造法，⛔ 别让两条算式各抄一遍 `CY − childH·0.5`。 |
| `:111-143`（**新增**，逐子件重载） | `left = 容器左 + padL + Σ_{j<index}(childWs[j] + spacing)`。为什么：等宽式把「前面每一件的宽」当成本件的宽（A239 的根因），且**这张表必须与子件次序逐格对应**（UGUI 跳过 INACT 子件 ⇒ 不参与排布的一格都不能占 = A239 的第②半）。越界时 `Debug.LogError` + 返回退化矩形（⛔ 不静默）。 |
| `:145-152`（新增 `HChild`） | 私有核心。为什么：等宽/逐子件两条路只该在「左边缘怎么算」上不同。 |
| `:166-177`（新增 `OwnHeight` 逐子件版） | 转调上面那个重载。为什么：调用点用的是 `OwnHeight` 这个名字（`childControlHeight = 1` 那一组），保留语义。 |

- **调用点统计**：全仓（非注释）`HorizontalChild*` 调用点 **12 处**；**改走新（数组）重载的 = 4 处**
  （全在 `Shell/CampaignRewardWindow.cs:494/502/517/526`）；**仍走旧签名的 = 8 处**
  （`Editor/RewardsScene.cs:3243/3258` · `Shell/CampaignTab.cs:629` · `Shell/ForgeTab.cs:580/710` ·
  `Shell/MissionsTab.cs:286/778/927`）—— 那 8 处**逐位零行为变化**（算式与求值顺序都没动）。

### B. `Shell/CampaignRewardWindow.cs`
| 位置 | 改了什么 · 为什么 |
|---|---|
| `:447-451` | `widths.ToArray()` 提成 `float[] ws`，**同一张表**喂 `HorizontalContentW`（内容宽）与四件的位置。为什么：「容器宽」与「子件落点」各算一遍 ⇒ 迟早对不上（CLAUDE.md §三）。 |
| `:470-491`（注释块） | 写清 A239 的**两半**与实测数（1325..1570 / 1430..1530 vs 应 1280..1525 / 1550..1650），并留档「物品槽号 = `n−1−i`（反序），⛔ 别换成 `i`」（原版 `SetAsFirstSibling`；`CampaignRewardsWindow__Open.c:135-145`）。 |
| `:494` | 物品格改走 `ws`（槽号仍是反序的 `k` = `n−1−i`，**行为不变**，只是算式换成逐子件版）。 |
| `:498-500`（新增三行） | `iBtn = k`（= `items.Length`）· `iWarn = iBtn+1` · `iBadge = PremiumLocked ? iWarn+1 : iWarn`。为什么：槽号与宽度表**同源**（A239 第②半）。 |
| `:502` | `Unlock Button` → 槽 `iBtn`。为什么：改前它拿的是 `(245+25)×k`，混宽列偏 +45。 |
| `:517` | `Warning` → 槽 `iWarn`。为什么：锁定支下它才是「按钮与徽标之间」那一格（不锁时它恒 INACT、不在表里，拿的是下一个空槽的矩形 —— 布局组本来就跳过 INACT 子件）。 |
| `:526` | `Badge` → 槽 `iBadge`。为什么：不锁时顶上「警告那一格」（改前错一格 = 偏 −120）。 |

### C. `Shell/CampaignTab.cs`
| 位置 | 改了什么 · 为什么 |
|---|---|
| `:940-993`（新增） | `struct ClipSnap`（`:943`）+ `ClipSnap ClearClip()`（`:980`）+ `void RestoreClip(ClipSnap)`（`:990`）（本文件私有）。为什么：四处要清的是**同一个值**（`null`/零）⇒ 规则只写一处；`RestoreClip` 的顺序照既有三处（先 pad/soft、后 `Clip`）。 |
| `:134-144`（**A353 ①**） | `Campaign Background/Background Image` 包在 `ClearClip()`（`:136`）/`RestoreClip()`（`:144`）之间。为什么：原版这一件没有 mask（判据见 `ClearClip` 的注释）。 |
| `:146-154`（**A353 ②**） | `Campaign Army Selector/Background` 同上（`:149` / `:154`）。为什么：mask 长在它**子节点** `Viewport` 上（`BuildArmyItems` 里成对拿捏），它自己那一层没有。 |
| `:170-242`（**A353 ③**） | `Campaign Header` 整棵（底图/徽记/`Title`/`Points`/`Point Icon`×2/`Info Button`）同上（`:175` / `:242`）。为什么：那两段 `_win.Text` 也吃 `RenderClip`（`Clip` 非空时 `MenuDraw.Visible` 判不可见 ⇒ **整条返 null**，静默少两段字）。 |
| `:834-937`（**A353 ④**） | `BuildPremiumPanel` 整块同上（`:842` / `:937`）。为什么：它在 `BuildTrack()` **之后**建 —— `BuildTrack` 一旦不还原，这一整块（`344.29,867.01→720.35,1080.00`）会被整个裁掉（连 `AddHit` 的命中区）。 |

- 两件都**没动** `.meta` / 行尾：`Core/UguiRect.cs` 与 `Shell/{CampaignRewardWindow,CampaignTab}.cs` 改前改后都是纯 LF。

---

## 三、断言

⛔ 本件**没碰** `Editor/RewardsScene.cs`（写手 R1 在写）⇒ 两条都写在这里，交调度台下一波补入。

### 1. A239 —— 建议落 `§三·d Campaign Reward Window`（锚点句 = `Section("§三·d \`Campaign Reward Window\`…")`，本件现读 `:4261`）
**位置**：接在既有那段「两列 holder」之后（锚点 = 那句
`CheckNear(cw.PremHolderRect.W, premW, 0.5f, …)`，本件现读 `:4324`；同一段的四个 `const` 在 `:4317-4320`）。
那一节已经有了 `itemW/btnW/badgeW` 三个常量、`bh`/`ph` 两个 holder 变量，以及 `RectOfUnion` 这个「量**渲染**矩形」的助手。

**为什么不需要夹具（不会空转）**：这两条量的是 `camp` 那棵**既有**的树，而它是
`RewardsWindow.Build → BuildTabContents → ct.Setup()` 那一趟**真跑出来的** ⇒ 观测量 = 被测算式本身。

```csharp
// ---- ★ A239：混宽列的槽位（判据 = 原版那三个子件的序列化宽 200 / 245 / 100 + padL 30 + spacing 25）----
//   期望值是**手算字面量**（⛔ 不读 `UguiLayout.HorizontalChild`、不读 `UnlockW/BadgeSize`）：
//     高级列 holder 左沿 1025（= 列左 960 + gap 65，既有断言已钉）· padL 30
//     槽 0（物品）1055..1255 · 槽 1（按钮）**1280..1525** · 槽 2（徽标）**1550..1650**
//   改坏法：把 `Shell/CampaignRewardWindow.cs` 那三个槽号换回自增的 `k`（或把
//   `Core/UguiRect.cs` 的逐子件重载换回 `(childW + spacing) * index`）⇒ 下面两条立刻红
//   （按钮 → 1325..1570、徽标 → 1430..1530 ⇒ 徽标还压在按钮上）。
var puBtn = FindChild(ph, "Unlock Button");
float px1, py1, px2, py2;
CheckTrue(RectOfUnion(puBtn, out px1, out py1, out px2, out py2), "（A239 前提）高级列按钮量得到渲染矩形");
if (RectOfUnion(puBtn, out px1, out py1, out px2, out py2))
{
    CheckNear(px1, 1280f, 0.3f, "★ A239：高级列 `Unlock Button` 左沿 = **1280**（1025 + 30 + (200+25)）");
    CheckNear(px2, 1525f, 0.3f, "★ …右沿 = **1525**（= 1280 + 245）");
}
var puBadge = FindChild(ph, "Badge");
if (RectOfUnion(puBadge, out px1, out py1, out px2, out py2))
{
    CheckNear(px1, 1550f, 0.3f, "★ A239：高级列 `Badge` 左沿 = **1550**（1280 + 245 + 25）—— 这一条同时钉住"
              + "「不占号规则」：改前 `k` 替 INACT 的 `Warning` 也占一格 ⇒ 1430");
    CheckNear(px2, 1650f, 0.3f, "★ …右沿 = **1650**（= 1550 + 100）");
}
```
- 容差 **0.3**：这两条是**算式字面量**（不是量出来的近似），而既有那一段用的是 0.5 —— 两条都可以，建议 0.3
  （与 A329 那条同族）。
- ⚠️ **⛔ 别用 `CampaignRewardWindow.ItemW/UnlockW/BadgeSize` 写期望值**：那三个常量**是被测实现**的字段
  （既有 `:4317-4320` 那几个 `const` 已经这么用了 —— 那是「内容宽」的自洽性检查，性质不同）。这里要的是**原版那三个
  序列化宽**，直接写字面量。

### 2. A353 —— 建议落 `§十`（锚点句 = `Section("§十 A266（折行宽的「生成」延后到激活）+ A303…")`，本件现读 `:7149`）
**位置**：接在 A326 那一段之后 —— WA3 已给锚点 = **锚点句 `CheckTrue(!SmallScreenUI.Enabled, "（收尾）A327…")`（本件现读 `:7409`）
之后、`Debug.Log(P + win.Dump());` 之前**（A326 的断言也还没落，两条挨着放；那一节在 `Run()` 末尾 ⇒ 多建一趟不可能
影响后面的断言与截图，⛔ 别往中间挪）。

🔴 **先说空转陷阱（不写这一条，下面一定是空转）**：
- 这四件是在 `CampaignTab.Build()` 里建的，而**第一次 `Build()` 早就跑完了**（`RewardsWindow.Build` →
  `BuildTabContents` → `ct.Setup()`）—— 那时 `Clip` 是 `null`（§一 已证）⇒ **在既有那棵树上量它们，
  改前改后都绿**（删掉 `ClearClip()` 照样绿）。
- ⛔ **也别就地 `campTab.Build()`**：`RewardsWindow.Node` 是**只建不找**的（`MenuDraw.Node` 每次
  `new GameObject`，`Shell/MenuDraw.cs:119-125`）⇒ 会在 `camp` 下面**再建一整套同名节点**（`_bgQuad`/`_title`/
  `_points` 重绑、`_armyScroll` **再注册一个滚动区**、`_trackContent` 换宿主），既让「新/旧」分不清、
  又会污染文件末尾那张 `02_战役.png`。
- ✅ **做法：夹具自己造一个全新的页实例**（节点独立 ⇒ 四件全挂在 `host` 下，与既有树互不干扰）。

**判据（原版字面量，⛔ 不读 `CampaignTab` 的私有字段）**：`_tabR` 系的三个矩形都能从
`资料/阶段二_锻造厂与战役页_原版规格.md` §一/§四 抄（`Campaign Tab` = 330.69,70.94→1920,1080 ·
`Campaign Header` = 330.69,60.94→790.92,225.94 · `Campaign Army Selector` = 745.92,70.94→1920.34,207.94 ·
`Premium Panel` = 344.29,867.01→720.35,1080.00）。

```csharp
// ---- ★ A353：这一页四处「原版没有 mask」的件 —— 下毒法 + 控制组 ----
//   判据（原版实读）：路径含 `Campaign Tab` 的 `RectMask2D` 只有 `Campaign Track/Viewport` 与
//   `Campaign Army Selector/Viewport` 两条 ⇒ 这四处的正确值就是 `Clip == null`
//   （`资料/普查产出_1011/WA3_A326.md` §六·1 · `…/普查产出_1012/S4_外壳共用件_开账现核.md` A353 行）。
var keepClipB = win.Clip; var keepPadB = win.ClipPad; var keepSoftB = win.ClipSoftness;
var a353Host = MenuDraw.Node(camp.parent, "A353 CampaignTab Probe",
                             new PxRect(330.69f, 70.94f, 1920f, 1080f));   // ⛔ 新节点，别挂进 camp
var a353Tab = a353Host.gameObject.AddComponent<CampaignTab>();
a353Tab.SetHost(win, a353Host);
// 毒值：一块**与四件全不相交**的屏外框（选「全不相交」是为了让改前四件**一个都不建**，信号最干净）
win.Clip = new PxRect(2500f, 1200f, 2600f, 1300f);
win.ClipPad = new Vector4(40f, 40f, 40f, 40f);
win.ClipSoftness = new Vector2(10000f, 10000f);
// 控制组：同一个毒值下画一颗**屏内**的探针 ⇒ 必须被裁掉（证明毒值真的带电、下面四条不是空转）
var a353Ctl = win.DrawRect(a353Host, CardArt.Solid(), new PxRect(400f, 300f, 500f, 400f), "A353Ctl",
                           CampaignTab.QTabBg, Color.white);
CheckTrue(a353Ctl == null, "（A353 控制组）毒框下画屏内那颗 ⇒ `MenuDraw.Visible` 判不可见、**返 null**"
                         + "（它不 null ⇒ 毒值没生效，下面四条全是空转）");
a353Tab.Setup();                                   // 这一趟的四件落在 a353Host 之下
win.Clip = keepClipB; win.ClipPad = keepPadB; win.ClipSoftness = keepSoftB;   // 收尾还原（本夹具不考裁切）
// 实验组：四件都必须**建出来**（改前：毒框把它们整块排除 ⇒ `_win.Rect` / `_win.Text` 直接返 null）
CheckTrue(FindChild(FindChild(a353Host, "Campaign Background"), "Background Image") != null
          && FindChild(FindChild(a353Host, "Campaign Background"), "Background Image")
                .GetComponentInChildren<ImageQuad>(true) != null,
          "★ A353①：`Campaign Background/Background Image` 照建 —— 改坏法：删掉 `Shell/CampaignTab.cs` 里"
        + "第①处那句 `ClearClip()` ⇒ 毒框(2500,1200→2600,1300) 与它(330.69,−45.18→1920,1544.48)不相交 ⇒ 整块不建");
CheckTrue(FindChild(FindChild(a353Host, "Campaign Army Selector"), "Background")
          .GetComponentInChildren<ImageQuad>(true) != null,
          "★ A353②：`Campaign Army Selector/Background` 照建（删掉第②处那句 `ClearClip()` ⇒ 红）");
CheckTrue(FindChild(FindChild(a353Host, "Campaign Header"), "Title") != null
          && FindChild(FindChild(a353Host, "Campaign Header"), "Title").GetComponentInChildren<Label>(true) != null,
          "★ A353③：`Campaign Header/Title` 那段字照建（`_win.Text` 也吃 `RenderClip`：`Clip` 非空 ⇒ "
        + "`MenuDraw.Visible` 判不可见 ⇒ **整条返 null**、静默少一段字）");
CheckTrue(FindChild(FindChild(a353Host, "Premium Panel"), "Background")
          .GetComponentInChildren<ImageQuad>(true) != null,
          "★ A353④：`Premium Panel/Background` 照建 —— 它在 `BuildTrack()` **之后**建，观测的是"
        + "「`BuildTrack` 有没有把 `Clip` 还原」的另一半");
// 量化那一档（四条，期望值 = 原版字面量；量**渲染**矩形）
CheckAt(FindChild(a353Host, "Campaign Header"), 330.69f, 790.92f, 60.94f, 225.94f, "A353③ …而且矩形 = 原版");
CheckAt(FindChild(a353Host, "Premium Panel"),  344.29f, 720.35f, 867.01f, 1080.00f, "A353④ …而且矩形 = 原版");
Object.DestroyImmediate(a353Host.gameObject);
```
**改坏法（如实写一半）**：
- 删掉**任意一处** `ClearClip()` ⇒ 对应那条 ★ 立刻红（毒框把那一件整块排除 ⇒ `_win.Rect`/`_win.Text` 返 null）。
- ⚠️ **只删 `ClipPad` / `ClipSoftness` 那两句赋值 ⇒ 今天不红**（`MenuDraw.Rect` 只在 `clip.HasValue` 时才采
  软边、`PaddedClip(null, pad)` 第一句返 null、`Text` 那一路只在 `RenderClip.HasValue` 时才 `ClipText`）
  ⇒ **可观测的只有 `Clip` 那一半**；pad/soft 是「把『这一处没有 mask』写全」的纪律件。⛔ 别在注释里
  写成「三件都验到了」。
- ⚠️ 夹具代价：`Setup()` 会**再注册一个滚动区**（`PointerLayer.RegisterScroll(_armyScroll)`，
  `Shell/CampaignTab.cs:160`）—— 它 `Owner = a353Host`，**块尾那句 `DestroyImmediate(a353Host.gameObject)` 是必须的**
  （⛔ 别省）。好消息：那一页的截图 `ShootShellPage(win, WindowTabType.Campaign, "02_战役.png")` 在
  `:5135` = **§十（`:7149`）之前** ⇒ 探针**碰不到截图**；本节在 `Run()` 末尾 ⇒ 也碰不到后面的断言。

---

## 四、没查清的部分（⛔ 不猜）

1. 🔴 **一条都没实跑**（红线：子代理不跑 Unity）。A239 的「改后实到」= **独立重算**（Python，只用
   原版序列化宽 200/245/100 + `padL 30`/`spacing 25` + 列左 960 + `HolderGap` 65），与既有断言
   `PremHolderRect.x1 == 1025` / `W == 655`（`Editor/RewardsScene.cs` 本件现读 `:4323`/`:4324`）自洽 ——
   但**那两条本身也是没跑过的**（批次 2 之前就在的老断言）。
2. **A353 的「今天不漏」是「静态全覆盖」反证，不是运行时观测**：判据 = ①字段初值 + ②全部写入点逐段读过 +
   ③「不写它的那几个文件」0 命中。**没覆盖**的情形：将来别的窗/别的代理往**同一个 `RewardsWindow` 实例**上写
   （今天没有）；或某条路上抛异常导致还原没执行（那属于构建中断，不是「漏值」）。
3. **`PremiumLocked == true` 那一支没有任何实据**：`IsPremiumLocked` 今天**恒 false**
   （`Shell/CampaignTab.cs:805` 写死 + 所有夹具都传 `false`）⇒ 锁定支的 holder 宽（980）与我改后的
   槽号（warn=槽2 / badge=槽3）**都只是「按表算的」**，与原版是否逐位一致**没人验过**（见 §五·3）。
4. **`MenuScroll` 的同步回调嵌套**：A268② 已证 `SetOffset` 会**同步**叫 `OnChanged` ⇒ 理论上存在
   「`RefreshNodes`/`BuildArmyItems` 在别人的 `Clip` 区间里被重入」的形状。本件**读了**所有
   `FocusOn`/`SetOffset` 调用点，都**不在**别人的 set/restore 区间里 —— 但那是**读出来的**，没跑。
5. **`CampaignTab.Build()` 被调用两次**（本报告 §三·2 的夹具会造第二个实例）会不会有别的副作用
   （`PointerLayer` 里多一个 scroll、`NoIconRewards` 列表被重填）—— 只读了代码：`NoIconRewards.Clear()` 是
   幂等的、`Owner` 随宿主销毁；**没实证**。

---

## 五、顺手发现（⛔ 只报不改，交调度台分流）

1. 🔴 **`资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §四·3 的「Badge 实到 1305..1405（−245，与按钮重叠 80px）」是错的**
   （铁律 5 该就地改，但那文件不在本件白名单）：
   - `1305 = 1055 + (100+25) × **2**` —— 那是**「用正确槽号 + 错误算式」**算出来的；
   - **当时代码真正的产物 = 1430..1530**（旧 `k` 在 `!isBase` 时替 INACT 的 `Warning` 也占一格 ⇒ 徽标拿到槽 3）；
   - ⇒ 波 C1 **只看到了 A239 的第①半（算式），漏了第②半（槽号/宽度表不同源）**。而**只修第①半的话
     徽标仍然错**（1305..1405，正好是波 C1 那个数）—— 那半是这一件真正的坑。
   - `Unlock Button` 那两个数（实到 1325..1570 / 应 1280..1525）**是对的**。
   - 建议 A 表 A239 那一行的「实害」栏改成：
     `Unlock Button 1325..1570（应 1280..1525）· Badge 1430..1530（应 1550..1650，压按钮 100px）`
     ＋ 一句「**两半：算式 + 槽号**」。
2. **S4 报告与 A353 那一行里的行号已随本件整体漂移**（`S4_外壳共用件_开账现核.md:26` 写
   `:141`/`:145`/`:165`/…/`:825`/`:892`/`:897`/`:906`）：改完现读 = **四个 `ClearClip()` 在 `:136`/`:149`/`:175`/`:842`**
   / **四个 `RestoreClip()` 在 `:144`/`:154`/`:242`/`:937`**。（🔴 与铁律「行号会漂」同一件事，**只报**。）
3. **`Editor/RewardsScene.cs:4534` 的括号数字过期**：「它整块落在视口外（**2135..2380**）」= 改前的产物（旧 `k = 5`）；
   改后 = **1955..2200**，**仍整块在视口外**（右沿 > 1920）⇒ **那条断言本身照样绿**（`GetComponentsInChildren<ImageQuad>().Length == 0`），
   只有括号里那两个数该改。⛔ 本件没动那个文件（写手 R1 在写）。
4. 🔴 **`Warning` 的两处自相矛盾（今天走不到）**：`Shell/CampaignRewardWindow.cs:447-455` 的宽度表在
   `PremiumLocked` 时收 `WarnW`（holder 980 宽），而 `:524` **无条件** `SetActive(false)`、**全仓没有第二处开它**
   （`grep _warn` 只有声明 / 建 / 关三处）⇒ **锁定支**下 holder 会比真正参与排布的子件宽 **300px**（UGUI 跳过 INACT 子件）。
   今天 `IsPremiumLocked` 恒 false ⇒ 走不到；接真数据时才现形。**不在 A239 范围内，本件没动**（改了会动「锁定支」的行为，
   而那一条**没有判据**——见 §四·3）。
5. **一条值得进 `资料/已知的坑.md` 的口径**（照 A182 §五·5 的先例）：
   **同一页里「有 mask 的件要成对拿捏、没有 mask 的件要成对清空」，判据都是「原版那一页到底挂了几个 `RectMask2D`」**
   —— A326（补框）与 A353（清空）**同形、修法相反**，今天全工程只有 `Campaign Tab` 这一页两个方向都出现过
   （两处 mask = 轨道视口 + 阵营条视口；其余四件都没有）。
6. 🔴 **本件收工那一刻，编辑器程序集仍编不过 —— 两条都是别人的在飞改动，⛔ 没动**（细节与两次读数 → §六）：
   ① `Editor/BattleScene.cs(1649,50)` `CS0136`（`foreach (var it in …)` 与同一块外层已有的 `it` 重名）——
   那一段是 `🆕 2026-10-12（A340）` 的新夹具；**本件收工前它已被它的作者修掉**。
   ② `Editor/MainMenuScene.cs(4929,42)` `CS7036`（`SocialPage.Text(…, bool alignLeft)` 少传一个实参）—— 收工时仍在。
   **本件三个文件 0 错**（§六）。

---

## 六、跑过的检查

```text
$ TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh        # ① A239/A353 全部改完那一次
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0

$ TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh        # ② 把 HChild 重构成 (left,cy,w,h) 之后复跑
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
Assets\CardPresentation\Editor\BattleScene.cs(1649,50): error CS0136: 无法在此范围中声明名为"it"的局部变量…
编辑器错误数: 1

$ TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh        # ③ 收工前再看一眼（树里全是别人的在飞改动）
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
Assets\CardPresentation\Editor\MainMenuScene.cs(4929,42): error CS7036: 未提供与"SocialPage.Text(…, bool alignLeft)"对应的参数…
编辑器错误数: 1
```
- ②③ 的错**都不在本件白名单上**（② = `Editor/BattleScene.cs` 的 A340 新夹具、③ = `Editor/MainMenuScene.cs` 的
  `SocialPage.Text` 调用点 —— 都是**别人正在写**的文件，且 ③ 那次 ② 那条**已经被它的作者修掉了**）
  ⇒ 按简报口径**记进报告、没去改**（§五·6）。
- **本件三个文件在三次里都是 0 错**：运行时程序集 0 错已覆盖 `Core/` + `Shell/`（含本件全部改动），
  且三次都没出现过指向 `Core/UguiRect.cs` / `Shell/{CampaignRewardWindow,CampaignTab}.cs` 的报错。
- A353 的判据**独立复扫**了一遍（不是照抄留档）：`grep -n "Campaign" d:/4/_tmp_view/q1_rm2d.txt`
  ⇒ 路径含 `Campaign Tab` 的 **只有** `Campaign Track/Viewport`（`:94` · `:298`）与
  `Campaign Army Selector/Viewport`（`:150` · `:182`/`:186`），另有一条 `Campaign Reward Window/…/Viewport`（`:272`，**另一扇窗**）。
- A239 的期望值**独立重算**（python，只喂原版序列化宽与 padding，⛔ 不读被测实现）：
  `n=1` 高级列 ⇒ holder `1025..1680`（W = 655）· 物品 `1055..1255` · 按钮 **`1280..1525`** · 徽标 **`1550..1650`**；
  改前 = 按钮 `1325..1570` · 徽标 `1430..1530`。`n=4` 加压夹具 ⇒ holder `1025..2355`（与既有注释一致）、
  按钮 `1955..2200`（改前 `2135..2380`）、第 4 格物品中心 `1830`（**不变**，与既有断言
  「第 4 个物品格：box 中心 x = … = **1830**」一致，本件现读 `:4571`）。
- ⛔ 本件**没跑** Unity 批处理 / 自检（用户口径：A 表清完再跑）· 没动 git · 没改任何正本。
