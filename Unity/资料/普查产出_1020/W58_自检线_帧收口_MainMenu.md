# W58 · 自检线 · `Editor/MainMenuScene.cs` 设计帧 → 指针帧 换口（第三批）

> 执行代理 **W58-FRAME-MM** · 2026-10-20 · 第五十八会话
> 白名单：只改 `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`。
> 口径：**只做不验**（铁律 12）—— ⛔ 没跑 Unity / `*.Run` / 截图；✅ 只跑了秒级 typecheck。
> 唯一换算口：`Shell/MenuDraw.cs:114` `PointerFramePxFromDesign`（`Editor/MenuCheck.cs:220` 一行转调）。

---

## 结论

1. ✅ **16 个站点 / 18 处换算表达式**全部改走唯一换算口：所有「设计帧点直接喂
   `PointerLayer.ButtonAt / ClickAt / WheelAt / ScrollUnder`」的调用点，先过
   `MenuCheck.PointerFramePxFromDesign(designPx, owner)` 再喂指针层。
   **期望值 / 设计字面量一个没改**（只改「点从哪来」）；`k == 1`（今天全部生产调用点）逐位返回入参
   ⇒ 现有读数一字不变。
2. ✅ **每个站点都显式给了 `owner`**（见下表）：一律取**设计矩形所属那一颗 quad / 节点**
   （乘的是它**父级**那一级 `lossyScale`，与 `MenuDraw.PosInDesignSpace` / `QuadRectPx` 对称）。
   滚动区站点取**视口节点**（`ScrollScale` 也是从它父级取缩放，与 `CollectionScene:9609` 的先例一致）。
3. ✅ **新增 10 条断言**（含 2 条前提）的 ×2 夹具探针：单位缩放逐位不变 · 换算值 = 画布中心 + 偏移×2 ·
   与 `PointerFramePx(quad)` 独立互证 · 生产命中路 `ButtonAt` 命中 · **灭自证**（设计点直接喂命不中）。
4. ⚠️ 断言**没真跑**（铁律 12）。运行时通过与否未实测。
5. ✅ typecheck：运行时 0 / 编辑器 0。

---

## 逐站点表

行号 = 本次改完后的现读（原始 `git status` 显示本文件在会话前已有未提交改动，行号可能继续漂）。

| # | 站点（现行） | 原形态 | 设计帧证据 | `owner` | 依据 |
|---:|---|---|---|---|---|
| 1 | 齿轮两态 `:1165` → 调用 `:1167/1172/1181/1186` | `const gearX=1847, gearY=35.5` → `ButtonAt/ClickAt` ×4 | 顶栏 `GearBox` 设计矩形 `1803.1..1890.9 × 4.6..66.4` 的中心 | `gearHit`（`SettingsBtn/Image`） | 设计矩形就是从这颗 quad 量的；乘的是它父级 `SettingsBtn` 那一级 `lossyScale` |
| 2 | A1814 探针循环 `:1292` | 12 个设计字面量 → `ButtonAt` | 原版布局框 + `m_RaycastPadding` 现算（注释 `/A1835`） | `p.B == bI ? qInbox : qGear` | 探针所属那颗钮的命中 quad（齿轮探针 → `qGear`、信封 → `qInbox`） |
| 3 | N117 左栏导航 `:2264` → 调用 `:2269/2297/2306/2317` | `navX=82.69, navY=228.25` → `ButtonAt` ×4 | Home 钮设计矩形 `y 143.4..313.1` 的中心 | `navHome.transform`（`Hit` 节点上的 `WindowButton`） | 设计矩形所属那颗钮；父级 = 导航钮节点 |
| 4 | 选卡组弹窗层序 `:3917` | `ButtonAt(1847,35.5)` | 同 #1（顶栏齿轮中心） | `SettingsBtn/Image`；取不到退 `bar` | 与 #1 同一设计帧；`bar` 与齿轮父级同缩放 |
| 5 | 称号页滚轮 `:5105` → 调用 `:5106/5113` | `WheelAt(1167,533)` ×2 | `Scroll Rect` 视口 `654.16..1680.12 × 210.69..855.46` 的中心 | `scr`（`Scroll Rect` 视口节点） | 滚动区视口就是它自己的矩形；`ScrollScale` 取的是它父级那一级 |
| 6 | 头像页滚轮 `:5211` → 调用 `:5212/5225` | `WheelAt(1167,533)` ×2 | 同上（与 Title 页同值） | `scr2`（`Scroll Rect` 视口节点） | 同 #5 |
| 7 | 排位页阵营栏滚轮 `:6284` → 调用 `:6285` | `WheelAt(1526,576)` | `scroll rect/viewport` `1305.29..1746.97 × 288.62..864.38` 的中心 | `vp5`（`viewport` 节点） | 同 #5 |
| 8 | T16 `ChangeRankedToggle` `:7097` → 调用 `:7093..7102` | `hx1/hx2/cyT16` + `100/600` → `ButtonAt` ×5 | `QuadPxRect(tgQ)`（设计帧）；命中区并集 `12.30..671.50` | `tgQ.transform` | 命中区那颗 quad —— 设计矩形就是从它量的 |
| 9 | 联盟公开列表滚轮 `:9463` → 调用 `:9464/9466` | `ScrollUnder/WheelAt(1117.94,708.53)` | `Viewport` `360.99..1874.90 × 337.29..1079.77` 的中心 | `oaVp`（`Open Alliances/Viewport`） | 同 #5 |
| 10 | 好友页滚轮 `:9588` → 调用 `:9589/9591` | `(1103.98,697.43)` | `Viewport` `332.15..1875.80 × 314.80..1080.06` 的中心 | `fvNode`（`Friends Container/Viewport`） | 同 #5 |
| 11 | 联盟成员列滚轮 `:9948` → 调用 `:9949/9951` | `(1125.17,786.84)` | `MemberList>Scroll View>Viewport` `369.67..1880.67 × 493.63..1080.05` 的中心 | `mVp`（该 `Viewport` 节点） | 同 #5 |
| 12 | 奖杯视口 + 成员列重叠探针 `:10084/:10088` → 调用 `:10090/10095/10104` | `(1143.985,729.175)` 与 `(1125.17,786.84)` | 奖杯 `Scroll Rect` `366.97..1921.00 × 378.51..1079.84`；成员列同上 #11 | 奖杯 = `tScroll`（它自己就是视口）；成员列 = 现场按 `GeneralDetails/MemberList/Scroll View/Viewport` 取回，取不到退 `mv.transform` | 已出成员段作用域 ⇒ 按同一路径取回同一颗视口节点；`mv` 是该灯段的宿主、父级同缩放 |
| 13 | 点奖杯格开弹窗 `:10405` → 调用 `:10407` | `ClickAt(525.97,578.51)` | 第 1 格 `376.97..674.97 × 401.51..755.51` 的中心 | `cell0`；取不到退内容容器 `drawer` | 设计矩形所属那一格自己（先例 = `CollectionScene:6540` 用 `cell0`） |
| 14 | 聊天选项行 T16 `:11614` → 调用 `:11617/11620/11622` | `cxT16 / oy1±1 / oy1+10` → `ButtonAt` ×3 | `QuadPxRect(hitQT16)`（设计帧；上沿 `379.35`） | `hitQT16.transform` | 命中区那颗 quad |
| 15 | 战斗日志滚轮 `:11942` → 调用 `:11944/11946` | `(972.5,546.5)` | `Matches>Viewport` `235..1710 × 130..963` 的中心 | `bvp`（该 `Viewport` 节点） | 同 #5 |

> 共 **16 个站点**；换算表达式 **18 处**（#1/#3/#5/#6/#9/#10/#11/#12/#15 各 1，探针循环 1，
> #8 的局部函数 1（5 次调用），#14 3 处）。

---

## 跳过的站点

| 位置 | 形态 | 为什么不是「设计 → 指针」 |
|---|---|---|
| `:5233 / :5237` `ScrollUnder(200f,200f)` | 自检**合成夹具**（`new GameObject("scroll_probe_host")` + `new MenuScroll(PxRect(100,100,300,300))`） | 宿主在世界根（`parent == null`），`ScrollScale = host.lossyScale = 1` ⇒ **设计帧 == 指针帧**；它验的是「登记表只增不减 / 死条目」，不是界面几何。换算口在这里恒等，加一跳无意义。 |
| `:1230 / :1259` `PointerLayer.HitBoxForTest` | **测量** API（取命中区矩形） | 不喂点；期望值本身是设计 px，属于「量」不是「命中」。 |
| `CheckAt / CheckAtWorld / RenderedRect / QuadPxRect / MenuCheck.DesignPx / NodeRectPx` 全部调用点 | **读口**（A1350 那批） | 不喂指针 API；按简报要求**一个字没动**。 |
| 注释里出现的 `PointerLayer.ButtonAt/ClickAt/WheelAt` | 文档 | 非调用。 |

---

## 断言与灭自证（`CheckPointerFrameFromDesign`，`:15354`）

夹具：
- ① 单位缩放：世界原点 `unitRoot` → 子件 `leaf`（父级 lossyScale = 1）。
- ② ×2：`frameRoot.localScale = (2,2,1)`（世界原点）→ `MenuDraw.Absorb` 建设计矩形
  `(1200,320)-(1400,520)` 的吸收层（档 2900 / 4000 —— `qContentMin-1 = 3999` 压过真界面最高档）。
  设计中心 `(1300,420)`；指针帧期望 `C + (design−C)·2 = (1640, 300)`。

| # | 断言 | 抓什么 |
|---|---|---|
| 前提① | 指针层在 | 不在 ⇒ 命中类断言等于没查 |
| 前提② | `PointerLayer.InputBlocked == false` | 锁着时 `HitButton` 直接 null ⇒ 不把红打在错原因上 |
| ★① | 单位缩放：`same.x == design.x && same.y == design.y`（`==` 精确） | 快路径不是逐位返回 |
| 前提③ | 吸收层由 `MenuDraw.Absorb` 建、下面有 `ImageQuad` | 没有 quad 进不了命中表 |
| ★② | `p2.x = 1640` / `p2.y = 300`（容差 0.01，期望由恒等式独立算） | **整值乘 k**（会得 2600）与「不换算」（会得 1300） |
| ★③ | `MenuCheck.PointerFramePx(quad)` 与 `p2` 同帧（0.01） | 与另一条**独立读口**互证 |
| ★④ | `PointerLayer.ButtonAt(p2)` 命中本夹具 | 走生产命中路，不是另算一遍 |
| ★⑤ | **灭自证**：设计点 `(1300,420)` 直接喂 `ButtonAt` 命不中本夹具 | 「实现不换算 + 期望值跟着改成设计点」两边不可能同时绿 |

**为什么结构上不可能同时满足**：×2 下本夹具的指针帧命中区 `x ∈ [1440,1840]`、`y ∈ [100,500]`；
设计中心 `(1300,420)` 的 x 在命中区之外。实现退回「设计点直接喂 `at`」⇒ ★④ 红；
想靠把 ★④ 期望值改成 1300 圆回去 ⇒ ★⑤ 红。

调用点：`WindowsManager.EnsureHost(menu.transform)` 之后（`:2239-2240`，指针层此时已建好）。

---

## 没查清的部分 / 风险

1. **断言没真跑**（铁律 12）。×2 夹具按 16:9 帧书写（与 `ShellScene` 的 `W58-ABSORB` 同一做法）；
   若将来宿主改钉非 16:9，`PxPerWorldX` 会让 1640 差一个比例 —— 那时应改成「用 `PointerFramePx(quad)` 当期望」。
2. **`owner` 是逐站点判的**：滚动区一律取**视口节点**（`ScrollScale` 取的是它父级那一级，与
   `CollectionScene:9609` 先例一致）。若将来某扇窗在**窗根与视口之间**多插一级自带 `localScale` 的节点，
   `owner.parent.lossyScale` 与 `ScrollScale`（`Owner.lossyScale`）会分家 —— 那时该处要按
   `MenuScroll.ClipNode` 的实际层级重判（属 `A1350` §二·3，不属本件）。
3. **#12 成员列的 owner 是现场取回的**（`mv` 在该段作用域外）：`FindChild(GeneralDetails/MemberList/Scroll View/Viewport)`
   取不到时退 `mv.transform`（父级同缩放）。没查到「这一路径在奖杯段一定还在」的独立判据 ——
   但同段上方 `CheckTrue(ms != null …)` / `CheckAtWorld(mVp, …)` 已经把它钉过；取不到时 `mv` 非空
   （前面 `mv.gameObject.SetActive(true)` 会先炸），所以最坏退到 `mv.transform`、不会静默变成哨兵。
4. **#3 导航钮 owner 的父级**：`navHome` 是 `Hit` 节点，其父级 = `Main Menu Navigation Button - Home`。
   今天单位缩放 ⇒ 无可观测差；若左栏整列被缩放，乘的应是那一级 —— 与设计矩形读口对称。
5. 本文件会话前已有未提交改动（`git status` 一开始就是 `M`）⇒ `git diff --numstat` 的 **321/70** 含那部分；
   本件净增 = 16 站点换口（含注释）+ 1 个 `CheckPointerFrameFromDesign` helper + 1 个 Section 调用。
6. 行尾：改前改后均 **LF（CRLF 0）/ 无 BOM**（用 Edit，未用 `sed -i`）。

---

## typecheck

```text
TMPDIR=/tmp/wf_framemm bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
