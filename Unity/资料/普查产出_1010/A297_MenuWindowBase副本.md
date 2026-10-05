# A297 · `Shell/MenuWindowBase.cs` 里那份**同形副本** → 转调 `MenuDraw.PosInDesignSpace`

> 写手：批次 1 · A297（2026-10-11）· 工作目录 `d:/4/Unity`
> 一句话：**做完了。** 两态断言已落地；⛔ 没跑 Unity 自检（红线）、没动 git、没动两张正本。

---

## ① 结论

`Shell/MenuWindowBase.cs` 里那两个 `Local` 重载原来**自己又写了一遍**那份算式
（`RectCenter / FromPixel − parent.position`，**少除一次父链缩放** —— 与 A294 修的 `MenuDraw.Local` 是同一个病）
⇒ 本件**转调**全壳唯一那一份：

| 位置 | 改法 |
|---|---|
| `Shell/MenuDraw.cs:67` | `static Vector3 PosInDesignSpace(...)` ⇒ **`public static`**（**既有语义一字未动**，只放开可见性） |
| `Shell/MenuWindowBase.cs:185-186` | 4 参重载 ⇒ `=> MenuDraw.Local(parent, x1, y1, x2, y2);`（**逐字转发**，算式不再出现第二遍） |
| `Shell/MenuWindowBase.cs:198-199` | 「像素点」重载 ⇒ `=> LayoutSpace.FromPixel(xPx, yPx) - MenuDraw.PosInDesignSpace(parent);`（`MenuDraw` 没有「点」那一份可转 ⇒ 直接调它的公共件，**不自写除法**） |

⇒ **「两套口径并存」这条消掉了。** 受益面（全走同一个 `Local`）：

- `MainMenuSubmenuWindow.Node`（`Shell/MenuWindowBase.cs:204`）—— 实测 `Shell/` 下 `.Node(` **259 处**（`Editor/` 另 7 处）· 简报写的「55 / 260」量级对得上；
- `Text`（`:266`）· `TextBox`（`:291`）· `BuildShell` 的 `Content Area` 渐变背景 · 左栏键；
- 四个窗：`ShopWindow`（:67）/ `RewardsWindow`（:181）/ `SocialWindow`（:34）/ `CollectionWindow`（:82）。

判据、算式、「除的是哪一级的 `lossyScale`」全部仍在 `MenuDraw.PosInDesignSpace` 那一处（**唯一一份**）。

### 断言：两态 · 29 条（5 `CheckTrue` + 24 `CheckNear`）

**宿主 = `Editor/ShopScene.cs:2940-3079`**（本件唯一动的第三个文件）。

**为什么挑它**：① 简报**点名可用**；② 它与本件**同源同题** —— A294 那一段就在同一文件（`:2786-2938`），
放一起便于下个会话一起读；③ 本批其它候选宿主都有人占着（黑名单里的 `MainMenuScene` / `RewardsScene` /
`CollectionScene`，以及 `DeckScene` / `ShellScene` / `SettingsScene` / `BattleScene` 同批有写手）；
④ 本场景这扇窗**就是**被测对象 —— `Shell/ShopWindow.cs:67` 的 `ShopWindow : MainMenuSubmenuWindow`，
而且它走的是**生产那条挂法**（`EnsureHost` → `AttachToAnchor` 把窗根归到世界原点，
= `PosInDesignSpace` 的适用范围，这一点 A294 已在本文件里核过一遍）。

### 🔴 「改坏哪里它会红」（一句话说清）

| 态 | 钉什么 | 怎么改坏 ⇒ 红 |
|---|---|---|
| **关**（出厂态 ⇒ k=1） | 两个 `Local` 重载与**旧式**（`设计点 − 父的世界位置`）**逐值相同**（1e-5）；`Node` / `Text` 渲出来正好落在设计点（1e-3） | 把「减父的世界位置」那一项**整个丢掉** ⇒ 件飞到屏幕外 |
| **开**（开关开 + `extraScaleSmallScreen = 1.2` ⇒ 窗根 ×1.2） | `Node` / `Text` **渲出来** = **设计点 × 1.2**；「像素点」那一份返回的局部坐标**两态同值** | 🔴 **这两处不转调**（退回旧式 `设计点 − parent.position`）⇒ `Node`/`Text` 偏 `(1−1.2)×2 = −0.4` 局部单位 = **−0.48 世界单位 = −51.8px**（y 偏 **−38.9px**）；「像素点」那一份两态差 0.4 局部单位 ⇒ 红 |

外加**两条「相对」断言**（⛔ 不读 `LayoutSpace`、不读我们自己的任何常量，只有开关那个 1.2）：
把「**关**」那一态**实测到**的世界 x / y 乘 1.2，必须等于「**开**」那一态 ——
就算上面那两条的期望值抄错了，它也能把「不除父链缩放」照出来（样板 = A294 在本文件的那两条 `★②（相对那一断）`）。

**验收标准 2（硬约束）**：常态（`k == 1`）下**逐位不变**，两条断言各钉一份（4 参 / 点重载），另加两条「渲出来落在设计点」。
**验收标准 3**：**只增不删** —— 本件对 `Editor/ShopScene.cs` 是**纯追加**（`git diff` 里那 2 行删除是**别人之前就在的**改动，见 ③）。

---

## ② 证据（文件:行号）

**改前（旧式，= 病根）**
- `Shell/MenuWindowBase.cs:174-175`（改前）`=> LayoutSpace.RectCenter(x1,y1,x2,y2) - (parent != null ? parent.position : Vector3.zero);`
- `Shell/MenuWindowBase.cs:181-182`（改前）`=> LayoutSpace.FromPixel(xPx,yPx) - (parent != null ? parent.position : Vector3.zero);`
- 参照物（已经是对的、且是唯一一份）：`Shell/MenuDraw.cs:30-31` `=> LayoutSpace.RectCenter(...) - PosInDesignSpace(parent);`

**改后**
- `Shell/MenuDraw.cs:67` `public static Vector3 PosInDesignSpace(Transform t)`（**唯一改动 = 可见性**；`DivByScale` 仍是私有，`MenuDraw.cs:73-76`）
- `Shell/MenuWindowBase.cs:185-186` 4 参转调
- `Shell/MenuWindowBase.cs:198-199` 「像素点」转调
- 消费方（**未改，只是被覆盖**）：`Node` `:204` · `Text` `:266` · `TextBox` `:291`
- 断言：`Editor/ShopScene.cs:2940-3079`（`Section` = `:2950`；态一 `:2967-3006`；态二 `:3008-3075`；收尾 `:3076-3078`）

**`null` 父的等价性（逐条核过）**：旧写法 `(parent != null ? parent.position : Vector3.zero)`；
`PosInDesignSpace(null)` 第一句 `return Vector3.zero` ⇒ **null 档逐位相同**。

**「常态逐位不变」的判据（我自己核的，不是抄）**：`PosInDesignSpace(t)` 除的是 **`t.parent.lossyScale`**
（`MenuDraw.cs:67-72`）⇒ 本件与旧式分家的**精确条件**是「**parent 那一级链条**上有非 1 的 `lossyScale`」。
我把 `Shell/` 下全部 `localScale =` 写入点过了一遍，只有 5 处：`CampaignTab.cs:358`（`Premium Mark` = 2 ·
⚠️ **2026-10-12（A328②c）订正**：这里原来写 `:342` —— **行号是旧的**（`358` 才对；同一句旧行号还抄在
`Shell/MenuDraw.cs` 与 `资料/普查产出_1010/共用件_A294_A292.md`，三处已一起订正））·
`ShellParts.cs:54`（`UISafeArea` 的 zone）· `BoosterPackOpenWindow.cs:441/550-554`（卡节点，=1）·
`TransformScalerBySmallScreenUI.cs:148`（**就是我们测的那个缩放器**）· `WindowsManager.cs:808`（复位成 `one`）。
⇒ **开关关着时窗根不带缩放器 ⇒ `k == 1` ⇒ 逐位不变**。两个「父件自己带缩放」的档也逐个核过：
`RewardsWindow.Node(node, "Premium Mark", r)` 的 parent 是 `node`（链上全 1）·
`_win.Rect(pm, …)` 走 `MenuDraw.Local(pm)` ⇒ 除的是 `pm.parent` 的 scale（=1）⇒ **两处都不变**。

---

## ③ 改动清单（只这三个 `.cs`，白名单内）

| 文件 | 改了什么 |
|---|---|
| `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs` | `PosInDesignSpace` 前加 `public` + 一段**为什么公开**的注释（+5 行；**无行为改动**） |
| `MyGame/Assets/CardPresentation/Shell/MenuWindowBase.cs` | 两个 `Local` 重载转调；替换旧注释为「为什么必须转调 / 谁在消费 / k==1 时逐位相同 / 判据在哪」（+16 −8 行） |
| `MyGame/Assets/CardPresentation/Editor/ShopScene.cs` | **追加** A297 一段（`:2940-3079`，140 行；29 条断言）。⛔ 未删任何既有断言 |

- ⛔ **未碰**：`Shell/DeckInfoPopup.cs` · `Shell/MainMenuRuntime.cs` · `Shell/ItemDrawer.cs` · `Shell/RewardWindow*.cs`（别的写手）·
  `Editor/{MainMenuScene,RewardsScene,CollectionScene}.cs`（别的写手）· `项目任务.md` · `CLAUDE.md` · `资料/待办判据_*.md` · `资料/已知的坑.md`。
- **行尾**：三个文件改前改后都是**纯 LF**（`HEAD` blob 与工作树两次数出来 `CRLF=0`）⇒ **没有翻行尾**。
  `git diff --numstat`：`MenuDraw 228/49` · `MenuWindowBase 38/16` · `ShopScene 430/2` —— 前两个里的数字**大头是本批别人已改但未提交的部分**；
  `ShopScene` 那 **2 行删除**是**别人之前就在的**改动（`Check(MenuDraw.AbsorbTierWarns, 0, …)` 那两行），**不是本件删的** ⇒ 本件对断言账是**纯追加**。
- **没动 git**（没 commit / checkout / stash / reset）。

---

## ④ 没查清的部分

1. 🔴 **`UISafeArea`（`Shell/ShellParts.cs:54`）那条路**：它给「安全区 zone」的 `localScale` 乘
   `safe.width / screenW`。**桌面上 `Screen.safeArea == 全屏 ⇒ sx = sy = 1`**，所以常态无影响；
   但**「有刘海/挖孔的设备上、某个 zone 恰好落在某个窗的 parent 链上」**这一档会让本件**改变行为**（按设计是对的、但我**没验**）。
   **没查清**：那三个 `zones` 到底挂在谁的链上（本机也没法造刘海）。⇒ 若将来真 Play 发现某窗在带刘海机器上偏了，先来核这一条。
2. **`Shell/MenuScroll.cs` 内部**有没有同形的 `− ?.position` 落位/换算 —— **没读**（超出本件范围）。
   我这轮的负向判据是「全 `CardPresentation/` 下 grep `- <something>.position`」，它只在 ⑨ 那几处命中（见 ⑤），`MenuScroll.cs` 零命中。
3. **受影响调用点的准确总数**：`Shell/` 下 `.Node(` = **259**（实测）；但**裸 `Local(` 的调用点没法用 grep 精确归属**
   （`Local(` 这个名字在 `PromptPopup` / `MenuWindowBase` / `MenuDraw` / 各处都有），所以「一共多少处落位走了本件」**给不出准确数**。
4. **`DeckInfoPopup.cs` 的行号在漂**：简报给的是 `:1199`，我第一遍读到定义在 `:1208`，第二遍变成 **`:1269`**
   ⇒ **那个文件确实有写手正在改**（黑名单里的，我没碰）。**引用这个文件的行号时别当稳定坐标**。
5. **简报里那句「`Shell/MenuWindowBase.Local` 是第三份副本」**：按本件的实读，`MenuWindowBase` 那一份**就是第二份**
   （第一份 = `MenuDraw.Local`；A294 修的是 `MenuDraw` 内的 4 处**调用**、不是那一份的定义）。
   真正的「第三 / 四 / 五 / 六份同形副本」是 ⑤ 里那四处 —— **简报没点全**。

---

## ⑤ 顺手发现（⛔ 只报不改 —— 全部在白名单外）

**同一份量纲病的**另外四处「同形副本」**（`RectCenter / FromPixel − X.position`，都**没除父链缩放**、
都**没转调** `MenuDraw.PosInDesignSpace`）—— 与 A294 / A297 修掉的是**同一个病**：

| # | 位置 | 形状 | 备注 |
|---|---|---|---|
| 1 | `Shell/PromptPopup.cs:260-261` | `static Vector3 Local(Transform parent, …) => RectCenter(…) - (parent != null ? parent.position : Vector3.zero);` | **与 `MenuWindowBase` 改前逐字同形**（连 null 守卫都一样）。消费者 = 本文件自己的 `Node`（`:263-272`）· `Solid`（`:274-279`）· `:184/:217/:247`。**简报没点这一处** |
| 2 | `Shell/DeckInfoPopup.cs:1269-1271` | `static Vector3 Local3(Transform basis, …) => RectCenter(…) - basis.position;` | 简报点的是注释行 `:1199`，**定义在 `:1269`**（行号在漂，见 ④·4） |
| 3 | `Shell/ImportDeckPopup.cs:233-234` | 同上 `Local3(basis, …)` | 简报点的是注释行 `:273`，**定义在 `:233`** |
| 4 | `Shell/PracticeModePopup.cs:1541-1542` | 同上 `Local3(basis, …)` | 🔴 **简报完全没提这一处**（只点了 `DeckInfoPopup` / `ImportDeckPopup` 两处）⇒ 同族其实是**三**份 `Local3` |
| 5 | `Shell/CampaignTab.cs:450` | `a + ab * 0.5f - parent.position` **直接**喂 `ImageQuad.Create` 的 `pos` | 🔴 **连包装函数都没有** —— 只有裸算式，⛔ 最容易被漏掉的一份 |
| 6 | `Shell/ShopWindow.cs:445-446` | `LayoutSpace.FromPixel(cx, 0f).x - tc.position.x`（**只 x 分量**） | 同一份病的窄版。⚠️ **同一个 `if/else` 的另一支 `:444` 已走 A228 那条（`Label.AlignLeftOn`）** ⇒ **同一处代码两个分支、一支修了一支没修** |

> ⚠️ **`Local3` 的语义比 `Local` 多一条**：它的第一个参数叫 `basis`（坐标基准）而**不是** `parent`
> （树父）—— 见 `DeckInfoPopup.cs:1203-1204` 与 `ImportDeckPopup.cs:273-278` 的注释：这两个参数在那些调用点
> **恰好是同一个对象**，才等价。⇒ **改这几处时不能无脑换成 `MenuDraw.Local(parent, …)`**，
> 要么保留 `basis`（= 用 `PosInDesignSpace(basis)`），要么先把 `basis != parent` 的那些点逐处核掉。
> （`ImportDeckPopup.cs:282-284` 已经为 `Nine` 加过一条「`basis` 必须等于 `parent`、否则出声」的守卫 —— 同一条口径。）

**不是缺陷、不用改的两处（我读过、确认已覆盖）**
- `Shell/CollectionWindow.cs:708 / :1318` 的 `Local(parent, r.x1, …)` —— **`CollectionWindow : MainMenuSubmenuWindow`（`:82`）且没有自己的 `Local`**
  ⇒ 走的就是**本件修的这份**，**已覆盖**。
- `Shell/SettingsWindow.cs:1001 / :1280` 的 `MenuDraw.Local(...)` —— A294 已覆盖。
- `Shell/SettingsWindow.cs:1321` 与 `Shell/ShopWindow.cs:422` 走的是**「像素点」那个重载**（生产调用点，共 2 处）⇒
  ⚠️ 它们**绕过 `MenuDraw.Local`**，只吃本件改的那个重载 —— 这也是我给「点」重载**单独配断言**的理由。
- `Shell/MissionsTab.cs:984` 的 `RewardsWindow.Local(parent, …)` —— 继承来的，已覆盖。

**一条与本件无关、但读数值得记的观察**
- `Editor/MainMenuScene.cs:459-460`（**别的写手**的文件）中途出现过 `error CS0103: 当前上下文中不存在名称"MenuWindowBase"` ×2。
  **全仓没有任何类型叫 `MenuWindowBase`**（`MenuWindowBase.cs` 只是**文件名**，里面的类是 `MainMenuSubmenuWindow`）⇒
  那是那位写手写到一半的笔误，**不是我引起的**（我的类型检查只有那两条、且全在别人的文件上）。
  我隔了 100 秒重跑，**已自行消失**（见 ⑥）。

---

## ⑥ 跑过的检查

**秒级类型检查**：`TMPDIR=/tmp/wf_a297 bash d:/4/Unity/工具/typecheck.sh`（**带独立 `TMPDIR`**，按简报要求）

| 次 | 运行时程序集 | 编辑器程序集 | 备注 |
|---|---|---|---|
| 1（改完两个 `.cs` + 断言后） | **0 错** | **0 错** | |
| 2（补完一条断言文案后） | **0 错** | **2 错** | 两条 `CS0103: MenuWindowBase` **全在 `Editor/MainMenuScene.cs:459,460`** —— 别人的文件（见 ⑤ 末条）⇒ 按铁律 13·3 **不当成自己的错、也没去改别人的文件** |
| 3（隔 100 秒重跑） | **0 错** | **0 错** | ✅ **交回时 0 错** |
| 4（改完最后两处文案后再跑） | **0 错** | **0 错** | ✅ 最终读数 |

- ⛔ **没跑 Unity 自检**（红线：子代理一律不许 `-executeMethod`）⇒ `ShopScene.Run` 那 29 条**尚未实跑**，
  由调度台在**同步点**按覆盖面决定何时跑（宿主 = `Editor/ShopScene.cs` ⇒ 对应入口 `ShopScene.Run`）。
- ⛔ **没跑 git 写命令**（只跑了只读的 `git status` / `git diff` / `git show`）。
