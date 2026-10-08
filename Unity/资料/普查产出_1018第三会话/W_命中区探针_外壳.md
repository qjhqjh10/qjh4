# W-命中区探针_外壳（A964 · 外壳侧）· 2026-10-18 第三会话

> 写手代理交付。**只改了一个文件**：`Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs`（+293 行 / −0 行）。
> 🔴 **本件没跑 Unity**（红线）· **没动 git** · 没改两张正本、没碰 `Shell/**`（`PointerLayer.cs` 只读调用）。
> 判据 = `资料/待办判据_1018.md` §A964 + §A964 续 · 前置侦察 = `资料/普查产出_1018第三会话/RECON_A964前置.md`。

---

> 🔴 **2026-10-18 更正（铁律 5）：「未跑 Unity ⇒ 一切运行时读数都是静态推断」—— 实际【已经跑过了】。**
> **原来写 X**：文头「**本件没跑 Unity**（红线）」· §2 表里四个「**静态**（未跑 Unity）」· §4「🔴 如实：**未跑 Unity ⇒ 能不能扫到哪些窗类是运行时才知道的**」· §5「⛔ 没跑 Unity ⇒ 本段的**一切运行时读数都是静态推断**」。
> **实际是 Y**：**调度台在同步点跑了**（写手侧交付时不许跑，交付**之后**跑了）——
> `d:/4/_tmp_view/shell.log:20434`：**`A964 扫了 237 颗 / E1 报 0 / E3 报 37`**
> （**覆盖 16 个窗类** · 本场景有实例 16 个 · 全库 `GameWindow` 子类 **38** 个）；
> 明细 **237 行** → `d:/4/_tmp_view/hitprobe/shell_hits.tsv`（**238 行** = 1 行表头 + 237 行数据，现读）。
> **错因（Z）**：本件是**写手侧交付物**，红线不许跑 Unity；跑完**没人回头把这几句改掉**。
> ⇒ 具体作废的几处：**§4 那句「能不能扫到哪些窗类是运行时才知道的」已有答案（覆盖 16 个窗类）**；
> **§2 四条判别式的「实测」栏、§4 覆盖率三档、§5「没查清」第 1 条**（能实扫几扇窗）**现都有实测读数**，⛔ 别再当「静态推断」引用。
> ⚠️ **仍成立的那半**：§5「没查清」第 2 / 3 / 4 条（`OpenWindow` 清 `Data` 的后果 · E3 的 0.5px 阈值是**我挑的**、非原版 · `MakeHitQuad` 是 `private`）**未受实测影响**。

---

## 1 探针落点 · 扫了什么 · 输出什么

- **落点**：`Editor/ShellScene.cs` 的 `Run()` **最后一个节**——A435·庚那一节的 `{}` 收口**之后**、
  `Debug.Log(P + shell.Dump())` **之前**（现读 `:5584`–`:5875` 那一段；按符号认：`Section("★ A964：命中区覆盖探针…")`）。
  ⛔ **不是新建文件**（宿主私有件 `QuadPxRect` / `CheckTrue` / `root` 都在原地）。
  ⚠️ 本段是 `Run()` 里**最后一段**，跑完只影响 `shell.Dump()` 的打印（收尾已 `CloseAllWindows()`）。
- **扫什么**：`PointerLayer.AllButtonsForTest()`（= **生产那张表**，`FindObjectsByType` 现扫）逐颗过，
  作用域分两档：① **常驻树**（`GetComponentInParent<GameWindow>(true) == null`，= `PointerReachable` 说「一直可点」那些）
  ② **逐扇窗**（`FindObjectsByType<GameWindow>(FindObjectsInactive.Include)` → `CloseAllWindows()` →
  `OpenWindow(w)` → 扫 → 关，**一次只开一扇**）。
  取矩形一律走生产口 `HitBoxForTest`（转发 `HitBoxPx`）/ `HitQuadForTest`（转发 `HitQuad`）——**探针不抄算式**。
- **输出**：`d:/4/_tmp_view/hitprobe/shell_hits.tsv`（目录不存在就建），17 列
  `窗类·窗态·节点路径·命中矩形×4·实绘矩形×4·四边差×4·RenderQueue·判定`
  ——「四边差」= **命中超出实绘**的量（**负 = 没覆盖到**）。
  日志：**`扫了 N 颗 / E1 报 M / E3 报 K`**（N 必打）+ 全部 E1 明细 + E3 前 40 条 + 三档覆盖率清单。

## 2 两个「已知阳性」的判别式（**静态**：未跑 Unity ⇒ 只写了断言，没实测）

两个旧样本今天都**不是活体阳性**（`A8` 修于 2026-10-04 的 `MenuDraw.MakeHitQuad`；`E12` 修于 2026-10-18 的 `BattleDriver`）
⇒ 按侦察 §4·3 的口径**改当判别式用**（造同样形状的坏件，两态配对断）。两条都在 `shell.Windows.CloseAllWindows()` 之后、
`root` 之下造，**用完 `DestroyImmediate`**（批处理无帧循环；命中表是现扫，留着会污染后面的断言）。

| 判别式 | 怎么造 | 期望 | 实测 |
|---|---|---|---|
| **①（A8 那一族）态一** | `new GameObject` + `AddComponent<WindowButton>()`，**子树里没有 `ImageQuad`** | 探针**必须报** | **静态**（未跑 Unity） |
| **① 态二** | 给它挂一颗 `ImageQuad`（照 `MakeNavButton` 的形状：`SetAspect`/`SetTint(0,0,0,0)`/`SetRenderQueue`） | **必须不报** | **静态** |
| **②（E12 那一族）态一** | 钮活着 + 子件 `Hit` 上挂 quad | **不报** | **静态** |
| **② 态二** | 只把**那颗 quad 所在节点** `SetActive(false)`（钮仍活） | **必须报** | **静态** |

🔴 **②的形态是本次的一处实质发现**：**外壳侧「藏起来」不在钮上、在 quad 上** —— `AllButtons()` 走的是
`FindObjectsForType` 的 **`FindObjectsInactive.Exclude`**（`Shell/PointerLayer.cs:908-909`）⇒ **钮一关就根本不在表里**、
连报都不会报（那正是原版行为，不是缺陷）。能体现「激活链」这一层的只剩 **`HitQuad` 第二句的 `!q.gameObject.activeInHierarchy`**
⇒ 判别式只能做成「钮活着、quad 藏了」，**不能照搬战斗侧那种 `SetActive` 钮的写法**。

## 3 E1 / E3 / 两条结构断言

### E1（主判据）· 报告式 · 不设断言
- **判据**：`!b.absorbOnly && b.isActiveAndEnabled && PointerLayer.HitQuadForTest(b) == null`（全段**唯一一份**，主扫与判别式共用）。
- **🔴 必须分「三条原因」里的哪一条**（否则「窗没开」冒充「缺 quad」= 侦察点名的那片假红）：
  用 `b.GetComponentInChildren<ImageQuad>(true)`（**含 inactive**）分开 ②/③ ——
  ① 钮自己不 `activeAndEnabled` · ② 子树里**根本没有** `ImageQuad`（= A8 那一族真缺陷）· ③ 有 quad 但不在激活链（多半是窗没开）。
  每次报都带 `[窗名] 节点全路径 —— 原因`。
- **🧨 改坏法**：让 `HitQuad` 恒返回 `null` ⇒ 判别式①态二 + 判别式②态一**同时**红。
- **待调度台逐条判**（A964 续 ② 的裁定：报告式，不设普适断言）。

### E3（同队列命中区相交）· 报告式 · 不设断言
- **判据**：只比**命中区之间**、**只比 `RenderQueue` 相等**的那些（队列同号 ⇒ `HitButton` 的「队列大者先」失效，
  赢家由 z、z 也同则**由枚举顺序**）⇒ 逐对求交，`> 0.5px` 才算（≤0.5px 当相切/浮点残差）。
- ⚠️ **不碰文字层**（文字层不带命中区 ⇒ 不踩 `MenuDraw.cs` 那条「`Absorb` 与文字档同号是故意的」）。
- **🧨 改坏法**：把某颗命中 quad 的 `SetRenderQueue` 改成与它上面那层同号 ⇒ 这条立刻报。

### 结构断言①：命中 quad 的 `anchor == (0.5,0.5)`
- **判据**：`PointerLayer.HitBoxPx` 拿 `q.transform.position` 当**矩形中心**（`:875-877`）——那**以 anchor 是中心为前提**；
  anchor 不是 (0.5,0.5) 时画出来的块是偏的、而命中区仍按中心算 ⇒ 症状正是「看着在钮上、点不动」。今天**零断言**。
- **两态**：今天的命中 quad 全是 `MenuDraw.MakeHitQuad` / `MakeNavButton` 建的（都传 `(0.5,0.5)`）⇒ 应绿；
  🧨 **改坏法**：把某处建成 `new Vector2(0f,0f)` ⇒ 立刻红。

### 结构断言②：命中 quad **不是九宫格/平铺的子块**
- **判据**：`ImageQuad.CreateNineSlice` / `CreateTiled` 的子件命名契约 `{根名}_{i}{j}`、**根上没有 quad**
  ⇒ `HitQuad` 的 `GetComponentInChildren` 会取到**第一块角块** ⇒ 命中区只剩一个角（症状同 A8）。
  实现 = 「`q.name == 父名 + '_' + 两位数字`」。
- 🧨 **改坏法**：把某颗命中节点的子树换成 `ImageQuad.CreateNineSlice(...)` ⇒ 这条立刻报。

### 灭自证（这一条是**结构上不可能同时满足**的那种）
`WouldReport` 的「报」走 **`HitQuadForTest`→生产 `HitQuad`**，而两条判别式的**期望值是手写的 `true`/`false` 字面量**
（⛔ 不是从被测实现读出来的）。⇒ 「实现与探针一起改回旧写法」不可能让四种（①态一/①态二/②态一/②态二）同时绿：
- 让 `HitQuad` **恒 null** ⇒ ①态一绿、①态二 + ②态一**红**；
- 让 `HitQuad` **恒非 null** ⇒ ①态一 + ②态二**红**；
- 删掉 `HitQuad` 那句 `!q.gameObject.activeInHierarchy` ⇒ **只有 ②态二红**（= 精确定位到那一句）。

## 4 覆盖率（🔴 如实：**未跑 Unity ⇒ 能不能扫到哪些窗类是运行时才知道的**）

探针自己会在日志里打**三档**：`covered`（开起来并真扫过的）· `failed`（本场景有实例、但没开起来/抛异常）·
`notHere`（`typeof(GameWindow).Assembly.GetTypes()` 里**本场景连实例都没有**的窗类）。**逐条打名字，不打条数糊弄。**

**静态读出的事实**（供调度台预判）：
- 直系 `: GameWindow` 的子类 **34 个**（另加 `IsSubclassOf` 会带上的孙子辈，如 `MainMenuSubmenuWindow` 的
  `CollectionWindow` / `RewardsWindow` / `ShopWindow` / `SocialWindow`）⇒ `allTypes.Count` 运行时会 > 34。
- **本场景里有没有实例 = 取决于前面几十节建过哪些窗**；`Run()` 里建过的窗（聊天 / 社交 / 档案 / 榜单 / 对局历史 /
  设置 / 练习 / 教程 / 排位奖励 / 活动 / 弹窗族 …）**都还在场**（`Close()` 只 `SetActive(false)`、**不销毁**）
  ⇒ 预期覆盖数不小。**但这是推断，不是实测**（静态限制，见 §5）。
- 🔴 **已知会拖低覆盖率的一档**：`OpenWindow(w)` 走 `TryOpen(data=null)` ⇒ **`SetupData(null)` 会清 `Data`**
  （`Shell/WindowsManager.cs:487`）＋ `Close()` 本来就 `Data = null`（`:609`）⇒ 那些「`Build()` 依赖 `Data`」的窗
  可能**开出来是空的**。探针为此单列 `emptyWin`（**开出来了却一颗钮都没有**）并 `Debug.LogWarning` 打出来 ——
  ⛔ **不让它冒充「没问题」**。

## 5 类型检查 · 行尾 · 没查清 / 停手

- **秒级类型检查**（`TMPDIR=/tmp/wf_hp6 bash d:/4/Unity/工具/typecheck.sh`）：**运行时 0 / 编辑器 0**。
  另跑了 `WF_DOC=1` 档：新增的 `///` 一处 `CS1570`（未闭合 `<ImageQuad>`）**已就地改掉**（三个局部函数的 `///` 一律降成 `//`
  —— 局部函数本来也吃不到 XML doc）；`ShellScene.cs` 剩下的 `CS1573` 全在 `:597`/`:692` 那两条**本件没碰**的旧警告上。
- **行尾**：改前 `b.count(b'\r\n') == 0` / `LF == 5657`；改后 **`0 / 5950`**（**纯 LF，一个字节没翻**）。
  `git diff --numstat` = **`293 0`**（**纯插入**，⛔ 没用过 `sed -i`）。
- ⛔ **没跑 Unity**（红线；且同一工程同时只能一个实例）⇒ 本段的**一切运行时读数都是静态推断**。
- **没查清（已写进代码注释，未解决）**：
  1. **能实扫几扇窗没实测** —— 全靠探针自己在日志里打 `covered / failed / notHere` 三档（§4）。
  2. **`OpenWindow` 重开时 `Data` 被清** 会不会让某些窗 `Build()` 出空树或抛异常 —— 探针用 `try/catch` +
     `emptyWin` 兜着（抛异常的那扇记 `failed`、**不让它拖垮整条 `Run`**），但**没实测**。
  3. **E3 的交集阈值 0.5px** 是**我挑的**（挡浮点残差），⛔ 不是原版判据 —— 原版没有「命中区相交怎么办」这条规则。
  4. **顺手发现（写进报告，⛔ 没自己动手改）**：`Shell/MenuDraw.cs` 的 **`MakeHitQuad` 是 `private`**
     （`:1871`，无修饰符）⇒ 宿主外建命中 quad 只能自己 `ImageQuad.Create`（本件照 `MakeNavButton` 的形状抄的）。
     将来若要在别的宿主里造同形夹具，要么把它提到 `internal`/`public`，要么照本件这份写。
