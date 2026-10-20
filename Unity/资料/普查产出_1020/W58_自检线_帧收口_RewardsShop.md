# W58-FRAME-RS：设计帧 → 指针帧 收口 —— `RewardsScene` / `ShopScene`（第二批）

> 白名单内**只改** `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` 与
> `Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs`；
> ⛔ 没动 git / 正本 / `Shell/MenuDraw.cs` / `Editor/MenuCheck.cs` / `CollectionScene.cs` / 其它宿主。
> 日期：2026-10-20（第五十八会话 · 自检断言线）。

---

## 一、结论

1. **两个文件里「设计帧点直接喂指针 API」的调用点已全部换口**到
   `MenuCheck.PointerFramePxFromDesign(designPx, owner)`（唯一实现在 `Shell/MenuDraw.cs:114`，
   `MenuCheck` 侧一行转调）。共 **31 处换算调用**（一个换算可喂 1–3 个指针 API）：
   - `RewardsScene`：**27 处**（含 `ClickButtonByQuad` 这一颗被 ~13 个调用点共用的助手）。
   - `ShopScene`：**4 处**。
2. **没有改任何期望值 / 字面量本身** —— 只改「点从哪来」；设计 px 全部原样作为**入参**。
3. **今天全部调用点父链 `k == 1`** ⇒ 换算口走快路径、**逐位返回入参** ⇒ 读数与改前**逐位相同**。
   唯一会在今天之外的场景真改变行为的是 `ShopScene` 那 4 处（`Booster Info Popup` 的
   `extraScaleSmallScreen = 1.2`，小屏 UI 开关打开时**真会差 20%**）。
4. **两个宿主各新增一节两态探针**（`CheckPointerFrameFromDesign`，照 `CollectionScene` 同名探针）：
   单位缩放新旧口逐位相同 / 父链 ×2 新口值正确且命中正确 / **灭自证**（未换算的设计点命不中）。
5. **typecheck 0 错**（见 §六）。

---

## 二、逐站点表（文件 + 现读位置 + owner + 判据）

> 行号为改后现读（会漂，按内容/符号定位）。`owner` 一律是「这个设计点**所属的那一颗**」；
> 换算口乘的是它**父级**那一级 `lossyScale`（与 `PosInDesignSpace` 除的是同一级）。

### 2.1 `Editor/RewardsScene.cs`

| # | 现读位置 | 调用 | 点的来源（= 设计帧判据） | `owner` | 为什么是它 |
|---|---|---|---|---|---|
| 1 | `:580` | `ButtonAt` / `ClickAt`（`:582/:587`） | `QuadRectOf(q)` 中心 —— 设计帧（`MenuDraw.QuadRectPx` 乙式） | `q.transform` | 与 `MenuDraw.QuadRectPx` / `FindOwnAbsorbPoint` 的 `designOwner = q.transform` 同一级；量的是它、换的也是它 |
| 2 | `:3939` | `ButtonAt`（`:3940`） | 字面量 `(5,5)`（页左上角空白） | `forge`（`Forge Tab` 页根） | 点在**页**这一层；`k` = 窗根缩放 |
| 3 | `:3944` | `WheelAt`（`:3946`） | 字面量 `(1125,700)`（轨道视口内） | `forge` | 同上（页坐标） |
| 4 | `:3945` | `WheelAt`（`:3948`） | 字面量 `(1125,300)`（轨道视口外） | `forge` | 同上 |
| 5 | `:3981` | `HoverAt`（`:3982`） | 字面量 `(5,5)`（挪开空白） | `forge` | 同上 |
| 6-9 | `:3996`–`:3999` | `ButtonAt` / `PressAt` / `MoveTo`×3 / `ReleaseAt` | 字面量 `(1125,700)` `(1131,700)` `(1160,700)` `(1170,700)`（拖拽序列） | `forge` | 同一套页坐标；换算口绕画布中心是仿射 ⇒ 相邻两点的设计距按 `k` 同比缩放，**口径一致** |
| 10 | `:4482` | `HoverAt`（`:4483`） | 字面量 `(5,5)`（Help tooltip 挪开） | `forge` | 同上 |
| 11 | `:4535` | `ButtonAt`（`:4536`） | `c0r`（`trackR` + `MenuScroll.Shift` 算出的格中心，再 +134.147） | `fcontent`（`Rewards Content`） | 点属于轨道内容那一层；`MenuScroll.Shift` 活在设计帧 |
| 12-13 | `:4570` / `:4572` | `ButtonAt`（`:4574/:4576`） | `RectOf(claimEdge)` 中心 / `qx1 − 20` | `claimEdge`（`ClaimHit`） | 与 `RectOf` 量的是**同一颗** |
| 14 | `:4891` | `ButtonAt`（`:4892`） | 夹具 `hitR` 的中心 `(5,5)`（`AddHit` 的入参是设计 px） | `qA.transform` | 被量那颗 quad 自己 |
| 15 | `:4902` | `ButtonAt`（`:4903`） | 同上 `(5,5)` | `tmpH`（夹具根） | ② 档命中的节点**没建**（`hB == null`）⇒ 用同层级夹具根；`k` 与建出来的命中区相同（父级 = 窗根） |
| 16-17 | `:4921` / `:4922` | `ButtonAt`（`:4923/:4924`） | 夹具 `hr3`/`hc3` 上的 `(125,150)` / `(175,150)` | `hC`（`H3` 命中区） | 本块建出来的命中区；`k` = 窗根缩放 |
| 18-19 | `:4947` / `:4948` | `ButtonAt`（`:4949/:4950`） | 夹具 `hr4`/`hc4` 上的 `(150,625)` / `(150,675)` | `hD`（`H4` 命中区） | 同上 |
| 20 | `:10772` | `HoverAt`（`:10774`） | `RectOfUnion(rowHitN)` 中心 —— 设计帧 | `rowHitN`（`RowHit`） | 被量的那一颗 |
| 21 | `:10787` | `HoverAt`（`:10788`） | 字面量 `(1200,500)`（窗内空地） | `rowHitN` | 同一棵窗树、同一缩放层 |
| 22 | `:10841` | `HoverAt`（`:10843`） | `RectOfUnion(hitS)` 中心 | `hitS`（被软边切过的 `RowHit`） | 被量的那一颗 |
| 23 | `:10870` | `HoverAt`（`:10871`） | 字面量 `(1200,500)` | `hitS` | 同上 |
| 24 | `:11052` | `HoverAt`（`:11053`） | 字面量 `(100,1000)`（奖励窗页坐标） | `mtA`（`Missions Tab` 页根） | 该点**不在**带 `1.15` 缩放的 `Daily Missions` 子树里 ⇒ ⛔ 不能拿 `rowA` |
| 25 | `:11241` | `ClickAt`（`:11242`） | 字面量 `(766.15,579)`（`Cancel` 中心） | `cancelN`（`ButtonLeft`） | 点在弹窗树里；`k` = 弹窗根缩放 |
| 26 | `:11258` | `ClickAt`（`:11259`） | 字面量 `(100,1000)`（窗外压暗） | `darkN`（`Menu Dark Background`） | 压暗层的坐标帧 |
| 27 | `:11271` | `ClickAt`（`:11272`） | 字面量 `(1153.85,579)`（`Confirm` 中心） | `confirmN`（`Generic UI Button`） | 同上 |
| 28 | `:11327` | `ClickAt` | 复用 `:11241` 的 `cancelP`（同一字面量 `(766.15,579)`） | ——（同一个 `cancelP`） | 点相同、帧相同 |

> 🔴 `ClickButtonByQuad` 的 `owner = q.transform` 是**逐站点自动对称**的：
> 被 `QuadRectOf` 量的就是 `q`，所以它同时覆盖了 ~13 个调用点（含 `Daily Missions` 子树里的
> `Collect`/`Reroll` 等——那一带父链带 `1.15`，换算口按父级 `lossyScale` 自动乘对）。

### 2.2 `Editor/ShopScene.cs`

| # | 现读位置 | 调用 | 点的来源 | `owner` | 判据 |
|---|---|---|---|---|---|
| 1 | `:2228` | `HoverAt`（`:2229`） | `RectOf(tipNode)` 中心（设计帧） | `tipQ.transform` | `RectOf` 量的那颗 `Tooltip` quad |
| 2 | `:2257` | `HoverAt`（`:2258`） | 字面量 `(5,5)`（挪开） | `tipNode`（`Tooltip` 节点） | 同一棵弹窗树 |
| 3 | `:2305`（复用 `blankTipP`） | `HoverAt` | 同上 `(5,5)` | —— | 收尾清悬停态 |
| 4 | `:2358` | `ButtonAt`（`:2359`） | `RectOf(hn)` 中心（设计帧） | `hq.transform` | `RectOf` 量的那颗命中区 quad |
| 5 | `:2375` | `ButtonAt`（`:2376`） | 字面量 `(360,1010)`（窗外页坐标） | `darkHitN`（`CloseHit`） | 压暗层坐标帧 |

---

## 三、跳过的站点与原因（「不是设计→指针」）

判据：点**已经是指针帧**（`MenuCheck.PointerFramePx` 直出，或已经过 `DmView(...)` 换算成屏幕 px）。

| 位置 | 形态 | 为什么跳过 |
|---|---|---|
| `RewardsScene :2170/:2172/:2227/:2229` | `pl63.ButtonAt/ClickAt(wantC63.CX, wantC63.CY)` | `wantC63 = DmView(...)` —— `DmView` 已经是**渲染/屏幕 px**（它按 `Daily Missions` 的 `pivot (0,1) × 1.15` 缩放）⇒ 再换算会**多乘一次 1.15** |
| `RewardsScene :3909` | `layer.ButtonAt(qx, qy)` | `qx/qy = MenuCheck.PointerFramePx(cb)`（指针帧，本批之前已收口） |
| `RewardsScene :3968/:3969` | `ButtonAt/HoverAt(hx, hy)` | `hx/hy = PointerFramePx(cb3)`（指针帧） |
| `RewardsScene :4455` | `HoverAt(hpx.x, hpx.y)` | `hpx = PointerFramePx(helpQ.transform)`（指针帧） |
| `RewardsScene :11043/:11062` | `HoverAt/ClickAt(tx, ty)` | `tx/ty = wantTrash.CX/CY`，`wantTrash = DmView(...)` ⇒ **屏幕 px**（见下「就地订正」） |
| `RewardsScene :11253/:11265/:11308/:11322` | `ClickAt(tx, ty)`（点垃圾桶） | 同上（`DmView` 屏幕 px） |
| `RewardsScene :11393/:11416/:11419` | `ButtonAt/ClickAt(wantCollect / tx,ty)` | `wantCollect = DmView(...)` ⇒ 屏幕 px |
| `RewardsScene` 里的 `CheckAbsorbRule` / `CheckShadeClickRule` 调用 | 共享件 | 唯一实现在 `Shell/MenuDraw.cs`（`W58-ABSORB` 已修），不在本宿主 |
| `ShopScene :2240` 等 | 注释 / 文案 | 不是调用点 |

> 🔴 **就地订正（铁律 5·b；只报告，不改别人的报告文件）**：
> 上一批 `W58_界面线_MenuDraw吸收点帧修正.md` §五把 `RewardsScene :11001`（= 现读的
> `pl.ClickAt(tx, ty)`，`tx/ty` 来自 `wantTrash`）标成「设计派」。**核后不成立**：
> `wantTrash = DmView(...)`，`DmView` 输出的是**屏幕 px**（该文件 `:10909` 自己写着
> 「期望值要再走一次 `DmView(...)` 才是画在屏幕上的矩形」）⇒ 它是**指针帧**，**不该再换算**。
> 本批**未改**它，属正确状态。

---

## 四、断言与灭自证（落在各自宿主文件）

两处各新增 `static void CheckPointerFrameFromDesign(string what)`（照 `CollectionScene.cs`
的同名探针，判据 = `Shell/MenuDraw.cs` 的恒等式；⛔ 不回读被测实现）：

- `RewardsScene.cs:12026` 调用 / `:12052` 定义；
- `ShopScene.cs:5450` 调用 / `:5477` 定义。

**① `k == 1` 逐位不变**：单位缩放节点上 `newP = PointerFramePxFromDesign(DesignPx(t), t)`
与**独立的** `oldP = PointerFramePx(t)` 比 `==`（逐位，不用容差）。
🧨 把「乘父链缩放」写成无条件乘 ⇒ 浮点尾数差异 ⇒ 红。

**② 构造 `localScale = 2` 夹具**：`mid.transform.localScale = (2,2,1)`，quad 建在设计矩形
`(1200,320)-(1400,520)`（中心 `1300,420`）：
- 期望值**由夹具字面量独立算**：`x = 960 + (1300 − 960) × 2 = 1640`、`y = 540 + (420 − 540) × 2 = 300`；
- 与 `PointerFramePx(q.transform)` **同帧互证**（两个口各自独立）；
- `PointerLayer.ButtonAt(p)` **命得中**探针 quad。

**③ 灭自证**：同一夹具上 `ButtonAt(设计点 1300,420)` **必须命不中**
（探针命中区在指针帧 x∈[1440,1840]）。
⇒ 把实现退回「原样返回设计点」时：② 读到 1300、③ 反而命中 —— **两条同时红**；
若想「把 ② 的期望值也改成 1300」圆回去，③ 立刻红。**实现与断言一起改回旧写法不可能同时绿。**

⚠️ 探针放在各自 `RunCore` **最末尾**（所有既有断言之后、汇总之前），建完即
`DestroyImmediate`；quad 队列 `4000` 压过真界面（保证命中路拿到它）。

---

## 五、没查清 / 风险

1. **断言没真跑**（铁律 12：只做不验）。逻辑前提：`PointerLayer.Instance != null`
   （宿主前面几十条 `ButtonAt` 同前提；探针里也显式先断）。
2. **`owner` 是逐站点判的**，判据是「这个点所属的颗 + 它父级那一级 `lossyScale`」。
   本批两个宿主的窗（`RewardsWindow` / `MissionRerollPopup` / `ShopWindow`）`extraScaleSmallScreen = 1.0`
   ⇒ **今天与将来都不缩放**（转换算口后逐位不变）；`BoosterInfoPopup = 1.2` ⇒ 小屏开关开时才真的缩放，
   这正是 `ShopScene` 那 4 处要修的**真缺陷**（x 偏 `0.2 × (设计 − 960)`）。
3. **弹窗多实例共用 owner**：`Cancel`/`Confirm`/`暗底` 那几处 `owner` 取的是第一扇 `pop` 树上的节点，
   而点击目标是 `pop2/pop3/pop4`。**这是安全的**：换算口只用「缩放级别」（`owner.parent.lossyScale`），
   同类窗口同 `extraScaleSmallScreen` ⇒ 同一 `k`。若将来某扇单独覆盖了 `extraScaleSmallScreen`，
   那几处要改成各自实例的节点。
4. **`ClickButtonByQuad` 的 `owner` 覆盖 `Daily Missions` 子树**（父链 `1.15`）：换算口自动乘对；
   但 `DmView` 那一族**不走它**（`wantC63`/`wantTrash` 是屏幕 px），两条路不要混。
5. **主菜单 / ShellScene / SettingsScene 的同类站点未动**（白名单外；仍挂在待办）。

---

## 六、typecheck

```text
TMPDIR=/tmp/wf_framers bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾 / BOM（二进制读）：

```text
RewardsScene.cs  CRLF 0  LF 12406  BOM True
ShopScene.cs     CRLF 0  LF 5568   BOM True
```

> `git diff --numstat`（对 HEAD，**含本批之前工作区里已有的改动**）：
> `RewardsScene.cs 311/93` · `ShopScene.cs 179/24`。
> 本批自身新增约：RewardsScene +108 行（探针 ~95 + 站点/注释），ShopScene +109 行（探针 ~95 + 站点/注释）；
> **没有整篇重写**（行数变化与新增行数一致，CRLF 仍为 0）。

---

## 七、改动清单

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` | 24 个调用点换口 + 1 节两态探针（`CheckPointerFrameFromDesign`） |
| `Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs` | 4 个调用点换口 + 1 节两态探针（同名） |

**没碰**：`Shell/MenuDraw.cs` · `Editor/MenuCheck.cs` · `Editor/CollectionScene.cs` · 其它宿主 ·
`CLAUDE.md` · `项目任务.md` · 任务线正本 · git。
