# WE1c · 主菜单三尾（A666 手抄同一式 5 处 + A667 + A668）

> 写手代理 · 2026-10-13 · **只动一个文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（+ 本报告）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读的 `git diff --numstat`）· ⛔ 没改正本
> ⛔ 没越白名单（`Shell/*` · `Battle/*` · `Core/*` · `Library/PackageCache/*` **全部只读**）
> 📌 本报告的「现读」= **本件收工那一刻**的行号（改完，文件 8876 → **8981** 行）。

---

## 一、结论

| 账 | 结果 | 一句话 |
|---|---|---|
| **A666**（5 处手抄同一式） | ✅ **5/5 改完** | 5 处**逐处现读定位**（清单那 5 个行号**一个都没漂**，见 §二）；全换成 `TmpRenderedRect`；**期望值与容差一位未动**（含 site 1 那条**世界单位**的 `0.02f`，见 §二·末） |
| **A667**（`:2000` 引用实现常量） | ✅ **改完（1 行 + 10 行注释）** | `PracticeModePopup.DlContentB` → **字面量 `793.87f`**（原版 `Content` 下沿；出处 `Shell/PracticeModePopup.cs:394-395` 的头注释） |
| **A668**（`:1974` 与事实不符的旧注释） | ✅ **改完（只订正注释，保留更正痕迹）** | 原句「量的是渲出来的矩形」**对容器节点不成立** ⇒ 按铁律 5 就地订正；**量法一行未动** |
| 类型检查 | ✅ **0 / 0** | `TMPDIR=/tmp/wf_we1c bash d:/4/Unity/工具/typecheck.sh`（跑了两趟，收工那趟在全改完之后） |
| 行尾 | ✅ **没翻** | 二进制读：改前 `0 CRLF / 8876 LF` ⇒ 改后 `0 CRLF / 8981 LF`（全程只用 **Edit** 工具，⛔ 无 `sed -i`） |
| 净增行 | `+128 / −23`（净 **+105**） | 与 `git diff --numstat` 的两边账对得上（见 §八） |

**三件都改到，没有一件跳过。**

---

## 二、A666 逐处（现读行号 · 手抄的是什么 · 改成什么 · 期望值/容差动没动）

> ⚠️ **清单那 5 个行号一个都没漂** —— W-E1b 给的 `:2814 / :3342 / :3348 / :5306 / :7022` 与本件开工那一刻
> **逐行逐字吻合**（我按内容而不是行号定位，落点却完全一致）。下表「现读」是**改完之后**的行号。

| # | 清单行号 | **现读行号** | 手抄的是哪一句（**改之前**那几行的原文式子） | 手抄源（实现侧那一句 `AlignLeft/AlignRight`） | 改成什么 |
|---|---|---|---|---|---|
| 1 | `:2814` | **`:2862`**（整块 `2834–2869`） | `ttl.position.x − Label.WorldW * 0.5f`（**世界单位**） | `Shell/AvatarTab.cs:167` / `Shell/TitleTab.cs:136` → `PlayerProfileWindow.cs:644` `if (alignLeft) MenuDraw.AlignLeft(lb, r)`，`r.x1 = TtlL = 654.16` | `TmpRenderedRect(ttl, …)` 取 **x1** ⇒ `LayoutSpace.FromPixel(x1, 0f).x` **折回世界**再比 |
| 2 | `:3342` | **`:3408`**（整块 `3387–3409`） | `(ownHero.position.x + Label.WorldW * 0.5f) * 108f + 960f`（px） | `Shell/MatchLogRow.cs:221` → `:271` `if (lb != null && right) MenuDraw.AlignRight(lb, r)`，`r = heroR` | `TmpRenderedRect(ownHero, …)` 取 **x2**（右缘） |
| 3 | `:3348` | **`:3423`**（整块 `3410–3424`） | `(foeHero.position.x − Label.WorldW * 0.5f) * 108f + 960f`（px） | `Shell/MatchLogRow.cs:221` → `:270` `Text(…, alignLeft: !right)` → `:153` `if (alignLeft) MenuDraw.AlignLeft(lb, r)` | `TmpRenderedRect(foeHero, …)` 取 **x1**（左缘） |
| 4 | `:5306` | **`:5393`**（整块 `5376–5394`） | `(nt.position.x − Label.WorldW * 0.5f) * 108f + 960f`（px） | `Shell/AlliancesTab.cs:708`（`Field(...)` 里的 `Text(…, alignLeft: true, …)`）→ `Shell/SocialWindow.cs:389` `if (alignLeft) MenuDraw.AlignLeft(lb, r)` | `TmpRenderedRect(nt, …)` 取 **x1** |
| 5 | `:7022` | **`:7125`**（整块 `7103–7130`） | `LayoutSpace.PxX(xinfo.position.x + Label.WorldW * 0.5f)`（px） | `Shell/AllianceMemberTab.cs:1018-1020`：`v.Text(…, alignLeft: false, …)` + `MenuDraw.AlignRight(lb, ei)` | `TmpRenderedRect(xinfo, …)` 取 **x2** |

**「期望值 / 容差动没动」逐处点名（一位都没动）：**

| # | 期望值 | 容差 | 备注 |
|---|---|---|---|
| 1 | `MainMenuRuntime.Center(654.16f, 654.16f, 0f, 0f).x` | `0.02f`（**世界单位**） | ⚠️ 见本节末那条「为什么没折成 px」 |
| 2 | `901f` | `0.5f`（px） | |
| 3 | `1197f` | `0.5f`（px） | |
| 4 | `432.47f` | `1f`（px） | |
| 5 | `1862.35f` | `1f`（px，走 `CheckNear`） | |

每处**另加了一条「（前提）…量得到」**（`CheckTrue(TmpRenderedRect(...), "（前提）…")`）—— 与 A617 那三处的写法一致
（`MainMenuScene.cs:6664` / `:6666` / `:6826` 同款）。**它不改变判据强弱**：量不到时 `TmpRenderedRect` 的四个 out 全是 0，
下面那条一样会红；加它只是**把红的理由写在脸上**（「量法没生效」vs「实现没对齐」）。

### 2·末 🔴 为什么 site 1 **没有**把 `0.02f` 折成 px（这是本件唯一一处要解释的判断）

- 那一格的**期望值本身**就是世界单位：`wantLeft = MainMenuRuntime.Center(654.16f, 654.16f, 0f, 0f).x`
  （= `LayoutSpace.FromPixel(654.16, 0).x`）。
- 所以「**期望值与容差一位不动**」的唯一忠实做法 = **把量到的 px 折回世界再比**（`LayoutSpace.FromPixel(nx1, 0f).x`），
  这样判据强弱与旧写法**逐格相同**（16:9 下 `0.02` 世界单位 ≈ **2.16px**）。
- 若改成「拿 px 直接比 `654.16`，容差仍写 `0.02f`」⇒ 容差**悄悄紧了 108 倍**（0.02px）= **改的是判据**，
  而不是「换一个测量的口」。⛔ 我没有那么写。
- 显示用的那一句 `{leftWorld * 108f + 960f:F2}px` 换成同值的 `LayoutSpace.PxX(leftWorld):F2`
  （`:2865`）—— 1/108 这个换算**全仓只有 `LayoutSpace` 一份**（`Core/LayoutSpace.cs:142-149` 那条「别在别处再乘 108」）。

---

## 三、A666 的 `heroR` 实参核对（`:3342`/`:3348` 与 901 / 1197 的对应）

🔴 **核过了，两个数都能逐位复算出来**（W-E1b §七·1 说「没逐字核 ⇒ 真要改之前先核」——这一格就是核的结果）。

判据 = **`Shell/MatchLogRow.cs` 现读**：

| 环节 | 出处 | 值 |
|---|---|---|
| 行矩形（夹具实测，本文件 `:3380` 那条断言同一份） | `MainMenuScene.cs` `CheckAtWorld(row, 326.03f, 1771.97f, 162.84f, 366.04f, "行矩形")` | `x 326.03..1771.97`（W = 1445.94）· `y 162.84..366.04`（H = **203.20** = `RowH` ✔） |
| 我方半边 `sideR` | `MatchLogRow.cs:215` `UguiRect.Child(r, SideA[0]=(0,0), SideB[0]=(0.5,1), P50c, (0,0), (0,0))`（常量在 `:56-57`） | 行**左半边** = `326.03..**1049.00**` |
| 敌方半边 `sideR` | 同上 `SideA[1]=(0.5,0)` / `SideB[1]=(1,1)` | 行**右半边** = `**1049.00**..1771.97` |
| `heroR`（**两份共用一行**） | `MatchLogRow.cs:220` `UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], HeroPos[side], HeroSz[side])` | 常量 `:59-62` |
| **我方**（side 0） | `HeroA[0]=(1,.5)` · `HeroP[0]=(1,.5)` · `HeroPos[0]=(−148,45)` · `HeroSz[0]=(340,50)` | 锚在**右沿** 1049.00 ⇒ **`x2 = 1049.00 − 148 = 901.00`** ✔ |
| **敌方**（side 1） | `HeroA[1]=(0,.5)` · `HeroP[1]=(0,.5)` · `HeroPos[1]=(148,45)` · `HeroSz[1]=(360,50)` | 锚在**左沿** 1049.00 ⇒ **`x1 = 1049.00 + 148 = 1197.00`** ✔ |

公式出处 = `Core/UguiRect.cs:41-58`（`pivot=(1,.5)` ⇒ 右沿 = 锚点 + `pos.x`；`pivot=(0,.5)` ⇒ 左沿 = 锚点 + `pos.x`）。
⇒ **`901` / `1197` 就是 `heroR.x2` / `heroR.x1` 本身**，与断言的那两条边**逐条对上**（两份的尺寸也不同：340 vs 360，
所以「两份不是镜像」那句也是对的）。✅ 可放心改。

⚠️ 附带核到的一条（**没改**）：`HeroP` 与 `HeroA` 两份**逐值相同**（`:59-60`），所以上面那个「右沿/左沿」的算法成立。

---

## 四、A667 / A668 改动

### 4·1 A667 —— `:2000` 的期望值引用了实现常量（**开工那一刻在 `:2002`**，现读 `:2023`）

> 📌 **行号**：简报写 `:2000` ⇒ **本件开工那一刻在 `:2002`（漂了 +2）** ⇒ 改完现读 **`:2023`**。
> （A666 那 5 处**没漂**，A668 那一条也没漂 —— 只有这一处漂了 2 行。）

```csharp
// 改前：
CheckTrue(rowBot <= PracticeModePopup.DlContentB + 0.5f,
          $"卡列表渲出来的底边 {rowBot:F1}px 不超过 `Content` 下沿 {PracticeModePopup.DlContentB}px");
// 改后（:2023-2025）：期望值 = **字面量**
CheckTrue(rowBot <= 793.87f + 0.5f,
          $"卡列表渲出来的底边 {rowBot:F1}px 不超过 `Content` 下沿 "
          + "793.87px（**原版字面量**，⛔ 不读 `PracticeModePopup.DlContentB`）");
```

- **为什么**：与**同文件**那条自订纪律打架 —— 现读 `:8472-8479` 那一节
  「断言用的**原版行几何**（判据源，独立于实现）」：`🔴 断言里⛔ 不许再引用实现常量 … ✅ 做法 = 把原版值在断言处重抄一遍并注明出处`；
  同族一条 = `CheckHAlign` 的 `<param name="x1">`（`:8352`）「原版字面量，⛔ 不从被测实现读 —— 读了就是自证」。
- **值本身是原版字面量**（不是我们挑的）：`Shell/PracticeModePopup.cs:394-395` 那段头注释写着
  「`Deck List Drawer/Content` 的矩形（**原版 LayoutGroup 节点本身**）」⇒ `DlContentT = 318.42` / `DlContentB = 793.87`。
  ⛔ 但**它是从实现常量读的** ⇒ 有人把那个常量改错，这条**不会红**（正是 `:8474-8477` 说的那种漏法）。
- **改坏法（有牙）**：把 `PracticeModePopup.DlContentB` 改成别的数（例如 `800`）⇒ 卡列表底边仍在 793.87 附近 ⇒
  **这一条不再跟着漂**（旧写法会跟着一起动、永远绿）。反过来说：把 `BuildCardRows` 的起排 y 往下推 10px ⇒
  `rowBot` 超过 `794.37` ⇒ **红**（新旧都红，这半不是本次增量）。

### 4·2 A668 —— `:1974`（现读 `:1973-1988`）那一句与事实不符的旧注释

**订正的是什么**：本节原来（现读 `:1976` 引的那一句）写着
「量的是**渲出来的矩形**（`Label.WorldW/H` + 世界坐标反算 ⇒ 1920×1080 像素），**不是源码常量**」。

**为什么不是事实**：`RenderedRect(t)`（`:285-302`）算的是「**节点 t 的世界 x/y** ± `t` 子树里**第一颗** `Label` 的**缓存** `WorldW/WorldH`」
—— 对**容器节点** `CardRow_i` 来说就是**混合量**：

| 底下那三处的节点 | `RenderedRect` 实际量到的是 | 旧描述成立吗 |
|---|---|---|
| `Deck Name` / `Warlord Name`（`:1997-1998`） | 节点**就是文字节点自己**（`MenuDraw.Text` 建的 `Label` 节点）⇒ 节点 ± 自己的宽高 | ✅ 成立 |
| 卡列表那三处（`:2003` 的 `foreach (var row in pw.CardRows)`） | **`CardRow_i` 容器中心** + **子件 `Name` 那颗 `Label` 的宽高** | ❌ **不成立** |

判据（现读）：`Shell/PracticeModePopup.cs:1260` 建的是**裸 `GameObject` + `RectTransform`**（`:1265` `SetPxSize(cell, DlCellW, DlCellH)`、
`:1266` `localPosition = Local3(...)` 摆在格中心），`:1267` 才在它底下建 `Name` 那颗 `Label`
—— **该容器只有这一颗子件**，所以「容器中心 ± 子件宽高」今天**数值上恰好等价**于「卡格的矩形」（建标签用的就是**同一个 `PxRect`**）。
⇒ **不是红**，但那句话会让下一个会话以为「量的就是这个容器自己的矩形」。

**改法**（铁律 5）：**保留更正痕迹**，把原句 + 错因 + 「今天为什么看不出」逐条写下；**量法一行未动**
（`RenderedRect` 三处照旧）—— 因为「该量**卡格**还是量**卡名**」本地判不了（WE1b §五·1 已如实记过），
动它会连带改 `#1/#2`（`nameBot <= rowTop`）的期望值。

---

## 五、断言清单（断什么 · 改坏法 · 灭自证那条补了没有）

| # | 断什么（改后） | 期望值来源 | **改坏法**（都能现形） | **灭自证补了没有** |
|---|---|---|---|---|
| 1 | `Select Item` 那段字**渲出来的左缘 ≡ 654.16**（世界坐标口径） | 原版 prefab 矩形（`TtlL`，由 `AvatarTab`/`TitleTab` 两处共用） | 在 `Shell/TitleTab.cs:136`（`AvatarTab.cs:167` 同形）那句 `Text(…, alignLeft: true, …)` **之后**插一句重排文字的调用（`SetCharSpacing` / `SetWrapWidth` / `SetAutoFitBox`）⇒ 旧量法照旧报 654.16、**这一条红** | ✅ **换口即灭自证**：被测实现写的是**节点 transform + `_tmpW` 缓存**，检测器读的是 **TMP 自己的网格**（`textBounds`）⇒ **两边不再共用一个口**，「两边一起改回旧写法」没有可改的公共口。⛔ 没另补一条 |
| 2 | 我方 `Hero Name` **真渲染右缘 = 901** | 原版 prefab 锚点算出来的字面量（§三 复算） | 在 `Shell/MatchLogRow.cs:271` 那句 `MenuDraw.AlignRight` **之后**插一句重排调用 ⇒ 旧量法照旧报 901、**这一条红** | ✅ 同上 |
| 3 | 敌方 `Hero Name` **真渲染左缘 = 1197** | 同上（`MatchLogRow.cs:153` 那一句 `AlignLeft`） | 同上（插在 `:271` 之后即可，两条一起红） | ✅ 同上 |
| 4 | 建盟表 `Name input title` **真渲染左缘 = 432.47** | 原版 prefab 矩形 `432.47,257.65→1125.42,307.65`（`AlliancesTab.cs:654-656`） | 在 `Shell/AlliancesTab.cs:708` 那句之后插一句重排调用 ⇒ **这一条红** | ✅ 同上 |
| 5 | `extra_info` **真渲染右缘 = 1862.35** | 原版 prefab 矩形 `1408.66,183.02→1862.35,243.02`（`AllianceMemberTab.cs:940`） | 在 `Shell/AllianceMemberTab.cs:1020` 那句之后插一句重排调用 ⇒ **这一条红** | ✅ 同上 |

**为什么「换口」就够，不需要另立一条「两边一起改回去也红」的断言**（灭自证那条的推理，逐字写清）：

- **共享的口**在这 5 处原来是 `_tmpW`（= `Label.WorldW`）：**实现**用它把节点摆到框边，**断言**又用它把节点位置减回去
  —— 一个口同时喂两边。新写法里，**断言**读的是 `TMP.textBounds`（`m_textInfo.characterInfo[]` 的字形布局），
  **实现**一个字都没有写进那里；从「布局数据」到「屏幕上的字」中间还夹着 `RefreshBounds` 的**摆位**那一层
  （`Battle/Label.cs:799-809`：`_tmp.pos = (−anchor.x·W − b.min.x, …)`）—— **旧量法把那层假设掉了，新量法看得见它**。
- 因此「把实现与检测器**一起**改回旧写法」这件事**没有落点**：把实现改回去 ⇒ 网格跟着动 ⇒ 新量法照样报出来。
- ⛔ **没有**写「渲染宽（`x2 − x1`）」那一格 —— 本地**没有**「原版这一段渲出来多宽」的判据
  （`menu_dump` 给的是**布局框**）；同 A490 / A617 的如实标注。

⚠️ **如实标（写进代码注释了，这里再重复一遍）**：`Δ/2`（= 字距 × 字符数在该档字号下的实际 px）**与容差谁大，本地量不出来**
—— 若 `Δ/2 < 容差`，上面那几种「改坏法」之后**仍可能是绿的**。要真知道得跑起来（同 A490 §4·3 与 WE1b §五·3 已如实标过一遍）。

---

## 六、没查清 / 没做的（⛔ 不许猜）

1. ⚠️ **5 条改动的断言没实跑**（本件按简报一次 Unity 都没跑）⇒ **它们首跑就是第一次**。静态侧我把
   「改前改后逐位同值」的链条重新推了一遍（`RefreshBounds` 把整块字按 `anchor=(0.5,0.5)` 居中在节点上
   ⇒ mesh 左/右缘 ≡ 节点 x ∓ `W/2` ≡ `AlignLeftOn/AlignRightOn` 喂进去的那条边），所以预期不会红。
   **红了先量实得值**，判「实现没对齐」还是「量法没生效（天文数字 / 全 0）」，⛔ **别直接改期望值**（注释里也写了）。
2. ⚠️ **`Δ/2` 与三处容差（0.02 世界单位 / 0.5px / 1px）谁大 —— 判不了**（要跑起来或按 TMP 的
   `characterSpacing → m_cSpacing` 换算链算才知道）。同 WE1b §五·3。
3. ✅ **「整棵关着」的节点能不能用 `TmpRenderedRect` —— 本件查实了（不是没查清）**，结论 = **能，但有前提**。
   链条（全部现读，证据在 `Library/PackageCache`，**不是猜测**）：
   - `TMP_Text.textBounds` → `GetTextBounds()`：`…/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:1354-1362` 与 `:4864-4891`
     ⇒ **只读 `m_textInfo.characterInfo[]` 的 `origin/descender/xAdvance/ascender`**，**与 `activeSelf` 无关**；
     一个可见字符都没有时留着哨兵 `k_LargePositiveVector2 / k_LargeNegativeVector2` ⇒ `size = −4.2949673e9`
     （**这才是在本仓被记成「4.29e9」的那个天文数字的确切来源**，`Battle/Label.cs:977-985` 那条注释记的就是它）。
   - `ForceMeshUpdate` → `OnPreRenderObject`（`…/TMP/TextMeshPro.cs:347-352` 与 `:2114-2120`）：
     `if (!m_isAwake || (this.IsActive() == false && m_ignoreActiveState == false)) return;`
     ⇒ **关着的时候推不出 mesh**（= 唯一会读到天文数字的那条路）。
   - `OnDisable`（`…/TMP/TextMeshPro.cs:671-684`）**不碰 `m_textInfo`**（只 unregister + `sharedMesh = null` + `SetActiveSubMeshes(false)`）
     ⇒ **渲过一次之后关掉，`textBounds` 仍然是对的**。
   - 我们的建窗次序是「**先 `SetActive(true)` 再 `Open()`**（内容只在这一支里重建）」→ `Shell/WindowsManager.cs:467-475`；
     act F 那棵 `GeneralDetails` 是「**建完才 `SetActive(false)`**」→ `Shell/AlliancesTab.cs:356-364`；
     `MenuDraw.Node` 建的物体**默认是激活的**（`Shell/MenuDraw.cs:119-125`，全程没 `SetActive(false)`）。
   ⇒ **本件涉及的三棵树（档案窗 `Title` 页 / `Battle Log` 页 / 社交窗未入盟支）都是在激活状态下建的**
   ⇒ `characterInfo` 有效 ⇒ **关着也量得到**。**残余风险只有一种**：某个 TMP「激活时从没渲过、之后才 `SetText`」
   —— **本件涉及的 5 处都不是这一种**（文案都在 `Build()` 里一次给足，那棵 act F 的树是在激活态建的）。
4. ⚠️ **原版那两颗 `Hero Name` / `Select Item` / `extra_info` 的 `m_VerticalAlignment` 我没查**（同 WE1b §五·4）
   —— 那几处「纵向」的期望值是**原版矩形**的中线，不是「原版那颗 TMP 的垂直对齐档推出来的中线」。
   本件**没动**那些纵向断言，也**没改**它们的期望值。
5. ⚠️ **site 4 那段文案「非空」这件事，我是从实现实参判的**（不是从资产读的）：`AlliancesTab.cs:654` 传的是
   `"Alliance Name"`；本文件**没有** `CheckText` 钉它（只有 `CheckFontBase` 钉字号，见 `:5448`）。
   若哪天它变空串 ⇒ `AlignLeft` 本来就不挪节点 ⇒ **新旧写法都会红**（不是本次新引入的坑）。如实记。
6. ⚠️ **本件没扫「别的形状」的手抄式**（同 WE1b §六·4）：我按 `WorldW * 0.5f` / `WorldW * 108f` 两个形状全文扫了一遍
   （见 §七·1、§七·3），**没扫**「先算 `RectCenter` 再拿它比实参」「先算 `UnionQuadRect` 再比」这一族。

---

## 七、顺手发现（⛔ 我一个都没改）

### 7·1 🔴 `CheckHAlign` **自己就是手抄同一式** —— A666 的真实答案比「5 处」还大

`Editor/MainMenuScene.cs:8355-8380`（断言助手，**全文件 14 个调用点**：`:6001 6004 6008 6011 6015 6019 6023 6040 6047 6056 6059 6062 6066 6069`，现读 `grep -c 'CheckHAlign("'`）：

```csharp
float w = lb.WorldW * 108f;                      // :8365
…
float cx = LayoutSpace.PxX(t.position.x);        // :8379
float px1 = cx - w * 0.5f, px2 = cx + w * 0.5f;  // :8380   ← 与 A666 那 5 处**逐字同一个式子**
```

⇒ 它**不走 `RenderedRect`**，所以 **A617 那张「16 处调用点」清单同样看不见它**。
它的头注释（`:8351`）还写着「量的是【渲出来的边】（`RenderedRect` = 节点世界 x ± `Label.WorldW/2`）」
—— **`RenderedRect` 那四个字是错的**（它自己手写了一遍，没调那个助手），而且那句话描述的量法**正是 A617/A666 判为自证**的那一种。

**要不要改请调度台裁**（⛔ 我没碰）：① 它**不是**纯自证 —— 断的是「我们把左缘钉在哪 vs **原版矩形**」，所以「实参传错矩形」它会红；
它**看不见**的只是「`AlignLeft` 之后又被重排」那一档（与 A617 那三处同一个盲区）。
② 但它**一次改就动 24 条断言的语义**（而且那 24 处里有一批的被测文字是**空串**，现在靠「宽度 < 0.01 ⇒ 自己判红并说明」兜着，
换量法之后那一段的措辞/门槛都要一起重写）⇒ 不在本件（5 处断言）的账上。

### 7·2 🔴 卡名那一条**也**引用了实现常量（与 A667 同一处病灶、同一条纪律）

`Editor/MainMenuScene.cs:1946` 与 `:1948`（现读）：

```csharp
if (wpx > PracticeModePopup.DlCellW + 0.5f) over++;                              // :1946
Check(over, 0, $"卡列表**每一行的卡名都放得进 {PracticeModePopup.DlCellW}px 的格**（…；"
               + "判据是**原版格宽**，不是我们自己的常量 —— 第一版撞列就是这条没断）");   // :1948
```

- 病灶与 A667 **一模一样**：期望值读的是**被测实现那一侧**的常量（`Shell/PracticeModePopup.cs:397` `DlCellW = 231f`）。
- 更扎眼的是：**同一句文案自己写着**「判据是**原版格宽**，不是我们自己的常量」—— 而它读的就是我们的常量。
- **同一节 §A218 已经用字面量了**：`:2067` 的 `box(…, "卡格 `CardRow_0`", 231f, 27.88f, "原版卡列表格 `DlCellW × DlCellH` = **231 × 27.88**")`
  ⇒ **本文件里两种写法并存**。
- **补丁形状**（一行两处）：`PracticeModePopup.DlCellW` → `231f`（原版 `GridLayoutGroup` 的格宽，同 `:2067` 那个字面量）。
- ⛔ **我没改**：A667 的账只点了 `:2000` 那一处（`DlContentB`），且简报红线是「顺手发现别自己改」。

### 7·3 ℹ️ `Label.WorldW` **没乘 `lossyScale`**（两条量法的隐含前提，值得记一笔）

`Battle/Label.cs:53`：`public float WorldW { get { … return _tmpW; } }` —— `_tmpW` 是 **`Label` 节点局部空间**的量
（`RefreshBounds` 拿它写 `_tmp` 子节点的 `localPosition`，那两处必须同量纲）。

⇒ 而 `TmpRenderedRect` 的文件头（`:307-308`）把它描述成「节点世界 x ± 缓存宽 `Label.WorldW`」。
**今天两者等价**，因为菜单/外壳那一片父链是**单位缩放**（本文件 §A218 那段注释 `:2037-2040` 已经点过：
「`WindowsManager.AttachToAnchor` 把窗根写成 `localScale = one` ⇒ 今天父链是单位缩放」）。
⚠️ **一旦有人给某棵菜单树加缩放**，这两条量法就会分家 —— 那时**新量法（读真网格）才是对的**。
⛔ 不在本件白名单（`Battle/Label.cs`）⇒ **只报不改**。

### 7·4 ℹ️ `Battle/Label.cs:977-985` 那条注释的因果链可以写得更准（本件顺带查实）

原文：「`WorldW` 在『量不出来』的时候是 TMP 的未定义值：实测**空串**读到的是 **`4.29e9`**
（同 `Core/Tooltip.cs` 里记的那条同族现象：**对象没激活时也是这个天文数字**）」。

查实（§六·3 的那条链条）：4.29e9 = `GetTextBounds()` 在**没有可见字符**时留下的哨兵差（`2.147e9 − (−2.147e9)`）。
⇒ 「**空串**」那一档**恒成立**；「**对象没激活**」那一档**只在「激活时从没渲过、之后才 `SetText`」时才成立**
（`ForceMeshUpdate` 在 `IsActive()==false` 时早退），**不是「一关就变天文数字」**（`OnDisable` 不碰 `m_textInfo`）。
⛔ 不在本件白名单 ⇒ **只报不改**（本件报告 §六·3 已经把判据与出处列全，够下一个人直接订正）。

### 7·5 ✅ WE1b §7·4 报过的那两条指针 —— **本件现读：两条都已经结清了**（⛔ 别再派一次）

| WE1b 报的 | 现读 | 结论 |
|---|---|---|
| `Shell/DailyData.cs:1223-1224`（「同一句话在 `Editor/MainMenuScene.cs:7602` 也有一份 ⇒ 要一起改」） | **`:1281-1284`**（行号已漂） | ✅ **已结清**：那里明写着「✅ 2026-10-13 订正（铁律 5）：那句话已经改掉了 —— `Editor/MainMenuScene.cs` 那一份由写手 W-E1 一并改完（**现读在约 `:7742`**）⇒ **本指针已结清，⛔ 别再派一次**」 |
| `Editor/RewardsScene.cs:8842-8843`（`CountByName` 那半「待接线」） | **`:1001-1004`**（行号已漂） | ✅ **已接**：「**改坏法**：删掉 `CountByName` 里那句 `if (root == null) return 0;` ⇒ 下面第一条**抛**」+ `Check(CountByName(null, "Highlight"), 0, "★ A361：…")` |

⇒ **两条都不需要再派活**（本件只在报告里更正行号；⛔ 两处都在别人的白名单里，一个字没动）。

---

## 八、类型检查结果

```
# 收工那一刻（全改完之后）
$ TMPDIR=/tmp/wf_we1c bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- **跑了两趟**（5 处改完一趟 + 收尾注释改完一趟），**两趟都是 `0 / 0`**；
  ⛔ **全程没有出现**「错误集中在某个不是我负责的文件上」那种情形（不需要「隔一会儿重跑」那条处置）。
- **行尾**：二进制读 `0 CRLF / **8981** LF`（改前 `0 / 8876` ⇒ **纯 LF 未翻**；全程只用 **Edit** 工具，⛔ 没有 `sed -i`）。
- **`git diff --numstat`**（收工那一刻）：`774  50  Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`
  （**相对 HEAD** —— 里面含 W-E1 那轮的 `550/16` 与 WE1b 那轮的 `646/27`，⛔ **别当本件的改动量**）。
  ⇒ **本件真正加的是 +128 行 / 删 23 行（净 +105 行，8876 → 8981）**——两边的账对得上（646+128=774 · 27+23=50）。
- ⛔ **没跑 Unity**（简报禁）· ⛔ **没动 git**（只跑了只读的 `git diff` / `git status`）· ⛔ **没改正本** ·
  只碰白名单里那一个 `.cs` + 本报告。**别的写手在飞的文件一个字没动**。

---

*（报告完 · 写手代理 WE1c · 只写了本文件 + `Editor/MainMenuScene.cs`）*
