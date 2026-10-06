# DIAG：ShellScene.Run 剩的两条红（A768① 条目级·态二 / A770 态二）
> 2026-10-14 · **只读诊断**（未跑 Unity、未改任何工程文件）· 判据全部来自读代码 + 日志算术（`d:/4/_tmp_view/shell.log:16348,16582`）
> 结论一句话：**两条都是 (α) 断言错，同一个根因** —— `GNodeRect` 量的是 **`MenuDraw.Hit` 那个节点的 rect**，
> 而 `MenuDraw.Hit` **故意**把节点摆在**父原点**（`localPosition = 0`），真正那块命中矩形长在**它子件那颗 quad** 上。
> ⇒ 量到的矩形 = 「**父件的中心** ± **命中矩形的一半尺寸**」= 一个**混合量**，态一恰好等于真值（巧合），态二就不等了。

---

## 一、逐条结论

| 条目 | 判定 | 一句话 |
|---|---|---|
| **A768①（条目级 · 态二）** | **(α) 断言错** | 剪裁本身**完全正确**（命中矩形宽度确实被切成 68.18）；错的是**量错了对象**。`+34.09` = 那条命中矩形的宽 ÷ 2 = 「交集中心 − 按钮中心」。 |
| **A770（态二）** | **(α) 断言错** | 同上（剪裁正确：命中矩形高确实被切成 153.216 = `253.79 − 100.57`）。`+5.06` = **被切掉那 10.11 的一半**。**另有第二个独立毛病**：这条还把「**世界帧**量到的 y」当成「**设计帧**的裁剪坐标」用（本窗窗根停在 `localScale = 0.8`，见 §四）。 |

⛔ **不是 (β)**：实现侧那条约定（节点在父原点）**被另一条既有的绿断言锁着** →
`Editor/RewardsScene.cs:3521-3532`（A218）里那一句
`CheckNear(a218hit.localPosition.magnitude, 0f, 1e-4f, "…仍在父原点（`Hit` 的既有约定：节点不摆位、quad 才摆在矩形中心）")`
—— **把节点改成「摆在矩形中心」= 当场把那条自检改红**（那是 (β) 路线的代价，见 §五）。
⛔ **也不是 (δ)**：夹具前提成立（两个 `Hit` 节点都量得到、非退化矩形，日志里两条「前提·不静默」都是 ✓）。

---

## 二、实现在这一处到底怎么摆（判据）

`Shell/MenuDraw.cs:1704-1767` 的 `Hit(...)`：

| 行 | 做了什么 |
|---|---|
| `:1714-1716` | `ViewportClip.Resolve(parent, clip, …)` 取裁切状态（父链解析） |
| `:1742` | `if (!ClipRect(r, PaddedClip(clip, maskPad), out hr)) return null;` ← **命中矩形 `hr`（= `R ∩ (V−pad)`，设计帧 px）** |
| `:1754-1755` | `var hit = new GameObject(name, typeof(RectTransform)).transform; hit.SetParent(parent, false);` ← **`localPosition` 恒 `0`**（父原点） |
| `:1761` | `SetPxSize(hit, hr.W, hr.H);` ← **只写尺寸**（`rect` 只由 `sizeDelta` 决定，`MenuDraw.cs:155-164`） |
| `:1762` | `MakeHitQuad(hit, hr, q, Local(hit, hr.x1, hr.y1, hr.x2, hr.y2));` ← **矩形中心长在 quad 上** |
| `:1743-1746` | 那段注释**明写**这条约定：「命中区那个**节点自己**摆在父原点（`localPosition = 0`）、quad 摆在矩形中心 …… 部分越界时 quad 摆在**截过那块**的中心 ⇒ `PointerLayer` 用它的中心 + 宽高做命中，自动就跟着截了」 |

**真鼠标那一侧的判据**（= 这条断言想验的东西）：`Shell/PointerLayer.cs:870-886`
`HitBoxPx` = `center := ToPixel(q.transform.position)`（**quad** 的世界位置）、`half := q.WorldW/H × lossyScale × K ÷ 2`
⇒ **命中区 = quad**，与 `hit` 节点自己的 `rect` **无关**。`HitQuad`（`:897-903`）也是 `b.GetComponentInChildren<ImageQuad>()`。
⇒ **断言该量 quad**（或「quad 的中心 + 节点写进去的尺寸」），**不该量节点自己的 rect**。

**断言这一侧量的东西**：`Editor/ShellScene.cs:4559-4569`
```csharp
Vector2 c = LayoutSpace.ToPixel(t.position);      // ← 节点的【世界位置】= 父件的位置（localPosition 恒 0）
float hw = rt.rect.width * K * 0.5f, hh = rt.rect.height * K * 0.5f;   // ← 节点写进去的尺寸 = hr 的尺寸（设计帧）
x1 = c.x - hw; x2 = c.x + hw; y1 = c.y - hh; y2 = c.y + hh;
```
⇒ **= 「父件中心 ± `hr` 尺寸的一半」**，两半还**不同帧**（位置世界帧、尺寸设计帧，见 §四）。
那句头注释（`:4556-4558`「命中区那一路**不吃软边**⇒ 直接量节点本身就是真值」）**只对 `MenuDraw.Node` 建的节点成立**，
对 `MenuDraw.Hit` 建的节点**是错的**（建议顺手订正，铁律 5）。

---

## 三、算式（逐位复现日志里的每一个数）

### 3.1 A768①（榜单军种条，窗根**无缩放**）

| 量 | 值 | 出处 |
|---|---|---|
| 按钮框 `r` | `248.99,142.32 → 385.35,263.91`（136.36 × 121.59，中心 `317.17,203.115`） | `Shell/LeaderboardWindow.cs:512-518`（`ArmySelR.CY` + `cx = ArmySelR.x1 + BtnW/2`）· 断言文 `ShellScene.cs:4686-4687` |
| 节点位置（= 按钮节点中心） | `317.17, 203.115` | 按钮节点 = `MenuDraw.Node(_armyContent, army, r)`（`:522`），`Hit` 是它的子件、`localPosition = 0` |
| 态二视口框（GFit 写进去） | `248.99,147.64 → 317.17,258.59` | 断言 `ShellScene.cs:4706` |
| **`hr` = r ∩ 框** | `248.99,147.64 → 317.17,258.59`（**W = 68.18**，H = 110.95） | `MenuDraw.cs:192-211` |
| **`GNodeRect` 读出** | `(317.17 ± 34.09, 203.115 ± 55.475)` = **`283.08,147.64 → 351.26,258.59`** ✓ = 日志实得 | `ShellScene.cs:4559-4569` |
| `+34.09` 出处 | `hr.W ÷ 2 = 68.18 ÷ 2` = 「交集中心 `283.08` − 父件中心 `317.17`」 | —— |
| **两条 y 边为什么没动** | 交集在 y 上**正好以按钮中心对称**（`(147.64+258.59)/2 = 203.115` = `r.CY`）⇒ 混合量与真值的 y 两半边**相等** ⇒ 巧合 | —— |
| 期望 | `248.99,147.64 → 317.17,258.59`（= `hr` 本身） | `ShellScene.cs:4712-4716` |

⇒ **态二读出的宽 68.18 = 剪裁生效的铁证**（轴没剪的话它会是 136.36）；差只差在**摆位那一半**。

### 3.2 A770（高级版窗的 `Army Container`）

| 量 | 值 | 出处 |
|---|---|---|
| `ContainerRect(0)`（相对根） | `58.88,29.63 → 590.11,192.956`（高 = `ContainerH = 163.326`） | `Shell/PurchasePremiumWindow.cs:438-442` · `:164` |
| `Abs(...)`（+ 根原点 `167.175,70.94`） | `226.055,100.57 → 757.285,263.896`，**中心 `491.67,182.233`** | `:428-431`（= A806 那条订正想让节点落在的地方） |
| 节点位置（设计帧） | = 上面那个中心（`Hit` 是 `cn` 的子件、`localPosition = 0`） | `:629-635` |
| **日志实得中心** | `585.335, 253.785` ← **= 设计中心 × 0.8（绕画布中心 `960,540` 缩）** | `960+0.8(491.67−960)=585.336` · `540−0.8(540−182.233)=253.786`（差 < 0.002px） |
| 态一 `hr` | = `on`（整块在视口内）⇒ H = **163.326** ⇒ 读出 `253.786 ± 81.663` = **`172.12 → 335.45`** ✓ 日志实得 | 日志 `:16569` |
| 态二 `cutY` | `(172.12+335.45)/2 = 253.786`（**世界帧**的节点 y） | `ShellScene.cs:4783` |
| 态二视口框 | `frP.x1,frP.y1 → frP.x2, 253.79`（**设计帧**） | `:4785` |
| 态二 `hr` | `226.055,100.57 → 757.285,253.786` ⇒ **H = 153.216**（= `253.79 − 100.57`） | `MenuDraw.cs:192-211` |
| **`GNodeRect` 读出** | `253.786 ± 76.608` = **`177.18 → 330.39`** ✓ = 日志实得 | —— |
| **`+5.06` 出处** | `ΔH ÷ 2 = (163.326 − 153.216) ÷ 2 = 10.11 ÷ 2 = 5.055` ✓（另半边 `−5.07` 同源） | —— |
| 期望（断言写法） | `px1,py1,px2,cutY` = 「只有下沿动、上沿与两条 x 边不动」 | `:4791-4797` |

⇒ **剪裁生效的铁证**：命中矩形高确实被切成 `153.216`（= 只被切掉 `263.896 − 253.786 = 10.11`）。
但节点自己的 rect 是**以节点位置为中心**摆的 ⇒ **上下两条边各动一半**，与「只有下沿动」的期望不符。

---

## 四、第二个独立毛病：A770 把**世界帧**的量当成**设计帧**的裁剪坐标（只有本窗带电）

- **根因**：`PurchasePremiumWindow` 的开场动画把**窗根**停在 `localScale = 0.8`
  —— `Shell/PurchasePremiumWindow.cs:226` `PopFromScale = 0.8f`、`:737-741` `StartPop()` 写 `transform.localScale = 0.8`、
  `:746-757` 的 `Tick()` 才把它 `Lerp` 回 1（**批处理没有帧循环** ⇒ `Tick` 从不跑 ⇒ **永远是 0.8**）。
  全库**只有这一扇**有这套 pop（`grep -rn "PopFromScale" Shell/` 只命中本文件）⇒ 另外两扇（A768① 榜单 / A768③ 活动窗）不受影响。
- **谁被它影响**：一切走**世界帧**的量 —— `LayoutSpace.ToPixel(t.position)`、`QuadPxRect`（`ShellScene.cs:141-151`，
  位置是世界帧、尺寸是 `WorldW×K` 设计帧，**本身就是混合的**）、`PointerLayer.HitBoxPx`。
  而 **`ViewportClip.ClipPx`（`Shell/ViewportClip.cs:169-191`）与 `MenuScroll.Viewport`、`GFit` 写进去的框全是【设计帧】**
  （`ClipPx` 走 `MenuDraw.PosInDesignSpace` 除回父链缩放，`MenuDraw.cs:71-79`）。
- ⇒ **本条第 ③ 件（`GUnion`）今天恰好安全**（活动窗没有 pop）；但**任何一扇有 pop 的窗**里，
  「量世界帧 + 拿它当裁剪坐标」这种写法都会**静默差一个 0.8**。
- ⚠️ **顺带一条**：本窗这 0.8 也会让 `PointerLayer` 的真实命中盒变成 0.8 倍（`HitBoxPx` 乘 `lossyScale`）——
  那是**批处理独有的**（真 Play 里 0.3 秒后回到 1）；本节只记，**不算这两条红的成因**（成因见 §三）。

---

## 五、最小改法（改断言，**不改实现**）

### 5.1 加一个「量 `MenuDraw.Hit` 命中区」的助手（放在 `GNodeRect` 旁边，`Editor/ShellScene.cs:4569` 之后）

```csharp
// 🔴 量 `MenuDraw.Hit` 建的命中区**必须量它那颗 quad**：`hit` 节点自己恒在**父原点**
//    （`Shell/MenuDraw.cs:1743-1746` 的约定；`Editor/RewardsScene.cs:3531` 的 A218 把 `localPosition == 0` 钉着）
//    ⇒ 节点的 `rect` 只是「父件中心 ± 命中矩形的一半」，态二那种「只切一条边」的场合**它必然两半边一起动**。
// 🔴 而且一律走**设计帧**（`MenuDraw.PosInDesignSpace` 除回父链缩放）：`GFit` / `sc.Viewport` / `ClipPx` 全是设计帧，
//    而 `QuadPxRect` / `ToPixel(t.position)` 是世界帧 —— 本窗窗根停在 `localScale = 0.8`
//    （`PurchasePremiumWindow.StartPop`，批处理没帧循环）⇒ 两个帧差 0.8（实测 253.79 vs 182.23）。
bool GHitRect(Transform hitNode, out float x1, out float y1, out float x2, out float y2)
{
    const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
    x1 = y1 = x2 = y2 = 0f;
    var q = hitNode != null ? hitNode.GetComponentInChildren<ImageQuad>(true) : null;
    if (q == null) return false;                       // ⛔ 不静默：调用点的「前提」断言会红
    Vector2 c = LayoutSpace.ToPixel(MenuDraw.PosInDesignSpace(q.transform));   // 设计帧中心
    float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;                  // `WorldW/H` 是设计单位 ⇒ ×K = 设计 px
    x1 = c.x - hw; x2 = c.x + hw; y1 = c.y - hh; y2 = c.y + hh;
    return true;
}
```
（`WorldW/WorldH` 见 `Battle/ImageQuad.cs:107-108`：`_worldH` 就是建 quad 时传进去的 `LayoutSpace.Px(hr.H)`，
**不含父链缩放** ⇒ 正是设计帧；`MakeHitQuad` 建的 quad 只有一颗、名字也叫 `Hit`，`GetComponentInChildren` 取到的就是它。）

### 5.2 换 6 个调用点（**只动这些行**）

| 行 | 现在 | 改成 |
|---|---|---|
| `ShellScene.cs:4692` | `GNodeRect(h0G, …)` | `GHitRect(h0G, …)` |
| `ShellScene.cs:4711` | `GNodeRect(h0c, …)` | `GHitRect(h0c, …)` |
| `ShellScene.cs:4724` | `GNodeRect(h0r, …)` | `GHitRect(h0r, …)` |
| `ShellScene.cs:4777` | `GNodeRect(ch0G, …)` | `GHitRect(ch0G, …)` |
| `ShellScene.cs:4790` | `GNodeRect(ch0c, …)` | `GHitRect(ch0c, …)` |
| `ShellScene.cs:4807` | `GNodeRect(ch0r, …)` | `GHitRect(ch0r, …)` |

**改完这两条按算式应当变绿**（我没跑，只能给算术）：
- A768① 态二：`GHitRect` 读出 = `hr` 本身 = `248.99,147.64→317.17,258.59` = 期望 ✓（态一/态三的期望值**本来就是 `hr`**，
  今天只不过在 y 上巧合相等 ⇒ 一并变准）。
- A770 态二：态一读 `226.06,100.57→757.29,263.90` ⇒ `cutY = 182.233`（**设计帧**，与 `GFit` 同帧）⇒
  `hr = 226.055,100.57→757.285,182.233` ⇒ 态二读出 **= `px1,py1,px2,cutY`** ✓；态三还原 ✓。

### 5.3 顺手要改的注释（铁律 5：把错的记录就地改掉）

1. `ShellScene.cs:4556-4558`：`GNodeRect` 那句「命中区那一路…**直接量节点本身就是真值**」**只对 `MenuDraw.Node` 建的节点成立**；
   `MenuDraw.Hit` 建的节点要量 quad（否则下一个人还会照它写）。
2. `ShellScene.cs:4769-4773`（A770 开头那段）：「本窗的容器节点与它的子件**不在同一档坐标**里（差一个根原点 `167.175,70.94`）」
   —— 换 `GHitRect` 之后这条**不再是它要绕开的东西**（真正要绕的是 §四那个 0.8 + 「节点 rect ≠ 命中矩形」）⇒ 一并订正，
   ⛔ 但「**故意不写死绝对坐标**、只断相对关系」这条纪律**要留**（它仍然挡着「容器整块不在视口里」那种假绿）。
3. （可不改，只记）`ShellScene.cs:4714-4717` / `:4793-4800` 两段「改坏法」的**方向仍然对**，
   但要如实补一句：**今天这两条在【改坏】与【没改坏】两种状态下都红**（见 §六）⇒ 换完之后才重新有鉴别力。

---

## 六、顺带查实的第三件事：这两条今天**鉴别力 = 0**

- A770 的「改坏法」写的是「把 `ViewportClip.Hang` 改回 `MenuDraw.Node(...)` ⇒ **y2 不动** ⇒ 红」。
  今天实测：**没改坏**时读出 `…→330.39`（y2 确实动了 5.07）、期望 `…→253.79` ⇒ **也是红**。
  ⇒ 两条红在「实现是对的」与「实现被改坏」两种状态下**都红** ⇒ 它们**分不出**自己该分的那两种状态（弱断言的反面：恒红）。
  这正说明问题出在**量错了对象**，而不是「值不对」。

---

## 七、置信度 / 判不了的那一步

- **置信度：高（≈0.9）**。依据：§三 两张表的每一个数（A770 的 6 个数含态一/态三、A768① 的 3 个数）都被
  「节点在父原点 + `hr` 尺寸」这一条模型**逐位复现**（A770 那个 0.8 的拟合残差 < 0.002px），
  且替代解释（把节点搬去矩形中心）**会撞上既有的绿断言** `Editor/RewardsScene.cs:3521-3532`。
- **判不了的一步（缺什么）**：
  1. **没跑 Unity**（红线：主对话在跑、串行）⇒ 「改完就绿」只有算术，没有实跑回执。要收口需在同步点跑一次
     `ShellScene.Run`（这一批只动 `Editor/ShellScene.cs` 一个宿主 ⇒ 按铁律 12 只需这一条）。
  2. **0.8 的来源**：`PurchasePremiumWindow.StartPop` 是**唯一**能给出 0.8 的地方（`grep` 全仓只有它有 `PopFromScale`；
     小屏缩放器那一路是 `1`（开关出厂关）或 `1.35`，都对不上 585.336），但**我没法在不跑的前提下**排除
     「还有别的东西把窗根乘了 0.8」。**判据够用**：不影响 §二的根因判定，只影响 §四那条附注的措辞。
  3. 本窗 `Tick()` 在批处理下**没人推**（`grep -rn "\.Tick(" Shell/*.cs Editor/ShellScene.cs` → 只有 `BlinkGraphic` / `SearchBox` 那几处，
     **零处**指向 `PurchasePremiumWindow.Tick`）⇒ 0.8 会一直停着。若将来有人加统一推帧，§四那条附注要跟着更新；
     **不影响 §五的改法**（设计帧的 `GHitRect` 在缩不缩 0.8 下**读数一致**）。
