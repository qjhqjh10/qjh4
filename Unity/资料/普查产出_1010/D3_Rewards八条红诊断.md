# D3 · RewardsScene 八条红诊断（**只读** · 2026-10-05）

输入：`d:/4/_tmp_view/rewards.log`（第四批 · **1163 通过 / 8 失败**）· 历史：`final_RewardsScene.log`（10-04 23:30 · 1019/1）· `rewards_1006c.log`（10-04 · 916/0）
判据来源：`d:/2/tools/decomp_full/`（原版反编译方法体，第一权威）+ 本仓等效物；⛔ 没有一处拿我们自己的常量当判据。

## 一、A 组（4 张全黑）——【**真回归**，不是渲染抖动】

**一句话**：戊2 新加的 `wm2.CloseAllWindows()`（`Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs:4170`）把**主壳窗 `win` 一起关掉了**（`Close()` 尾句 `SetActive(false)`），
而紧接着那 4 张截图拍的**正是 `win`** ⇒ 空帧 = 相机的 clear 色（`RewardsScene.cs:596-597` 是 `SolidColor` + `Color.black`）⇒ 平均亮度 0.0。

1. **隶属（不是同一段夹具，但同一根因）**：`Shoot("01_日常_Missions.png")`=4199 与 `02_战役`=4201 在「领奖」段（`DailyData.CollectDaily(0)`=4193 之后）；
   `03_锻造厂`=4295 与 `03b_锻造厂_不可领`=4303 在 §三·b（`win.tabButtons.Click(2)`=4294 / `fgo.SelectArmy("SaimHann")`=4302 之后）。
   **四张的共同点 = 屏上只有 `win`**；其余 7 张拍的是别的窗（`cw` / `rw` / `dr` / `ds` / `inbox` / `rwA`）⇒ 都非空。
2. **机制（代码级）**：`WindowsManager.CloseAllWindows()`（`Shell/WindowsManager.cs:893`）逐扇 `Close()`；`win` 在 `RewardsScene.cs:611` 被 `wm.OpenWindow(win)` 登记进 `openWindows`
   ⇒ `GameWindow.Close()`（`Shell/WindowsManager.cs:444-458`）→ `CurrentState=Closed` + **`gameObject.SetActive(false)`**（`:458`）。
   diff 逐块核过：新文件**首个** `CloseAllWindows()` 就是 4170（旧文件首个在 3818，在旧 3727 那两张截图**之后**）；
   4170→4303 之间**没有任何**把 `win` 开回来的调用（全文件唯一的 `OpenWindow(win` 在 611）。
3. **为什么后面又不黑了**：`03_每日奖励`(4360) 拍的是 `dr`（4322 `OpenWindow(dr)`）、`04`→`ds`、`05`→`inbox`、`06`→`rwA`，各自带自己的整屏底图 ⇒ 壳关着也能出非空帧；07/07b 同理（`rw` 开着）。
4. **上一轮绿过（不是「从没绿过」）**——同一条断言的平均亮度：

   | 图 | 10-04 23:30 | 10-04 13:45 | 本次 |
   |---|---|---|---|
   | `01_日常_Missions` | 31.8 | 32.0 | **0.0** |
   | `02_战役` | 39.0 | 39.0 | **0.0** |
   | `03_锻造厂` | 31.8 | 32.0 | **0.0** |
   | `03b_锻造厂_不可领` | 20.2 | 20.3 | **0.0** |

   ⇒ 判据（`>3`）本身立得住，**是本批引入的回归**（判定标准 = 上一轮绿过）。
5. **不是抖动（三条判据）**：① 4 张图 **md5 完全相同**（`7bf59bc3a4d28b66e2eb9b9aac4e30e`，各 27 260 B）= 同一帧「什么都没有」，不是随机噪声；
   ② 同一次运行里另 7 张亮度 13.6–33.7、体积 340 KB–1.5 MB 全正常；③ 不是「场景被清空 / `DestroyImmediate`」——是 `SetActive(false)`（我们的 `Close()` **不销毁**，`:458` 只关物体）。
6. **`07_奖励窗_*.png` 点名确认（题面要求）**：两张**都在失败名单外，且是绿的** —— 日志有 Shoot 记录（`rewards.log:14148` / `:14710`）、盘上有文件
   （`07_奖励窗_Get.png` 356 203 B · `07b_奖励窗_Preview.png` 323 989 B）、平均亮度 **20.7 / 23.3**。⇒ 戊2 新加的两张图没问题，黑的 4 张全是**老图**。
7. **修法（建议，⛔ 不要照抄动手）**：
   - **最稳**：把 `RewardsScene.cs:4170` 那句 `wm2.CloseAllWindows();` **挪到 4 张截图之后**（例如挪到 4307 `win.tabButtons.Click(0)` 前面）。
     它想清掉的其实是 §三·d 留下的 `cw` 弹窗，而**旧代码里这 4 张本来就是带着 `cw` 拍的**（上一轮的绿就是这么来的，不是新问题）。
   - 若要「干净的壳截图」：`wm2.CloseAllWindows(); wm2.OpenWindow(win);`（`:723` 会重新登记 + `TryOpen → Open → Build` 重建）。
     ⚠️ **重建会让旧引用变陈**：`area`(685) 派生的 `camView/cpan2`（4206–4292 全在用）与 `bar`(697，4308 用) **必须重新取一遍**，否则那批断言会红。
8. **判别力**：① 在每张「拍壳」的截图**之前**加 `CheckTrue(win.gameObject.activeInHierarchy, "…壳还开着")` —— 这一整类问题立刻变成一条断言（现有「空图护栏」只报「黑了」、报不出「为什么黑」）；
   ② 加一条「4 张同名图**不许 md5 相同**」的护栏（本次全同就是最强线索）；③ 出图后 `ls -l` 看体积。
9. ⚠️ **A 组唯一未闭合的一环（如实记）**：`02` 之后 4206/4289/4291 那三条读文字的量出了 **710.2 / 408.6 / 480.69**（**不是 NaN**，全日志 `NaN` 零命中），
   而它们走的 `TextRightPx/TextLeftPx`（`:427/:433`）都是 `t.GetComponentInChildren<Label>()` 单参重载 ⇒ 要么**该重载在父链 `SetActive(false)` 时照样找得到件**
   （那 `RewardsScene.cs:3938` 那句「单参重载会跳过不激活的子件」就是**错的、待订正**），要么**壳在那几行时是活的** ⇒ 黑图另有一层成因。
   **我判不了是哪一条**（不能跑 Unity）。但前者能解释**全部**观测（含 4142 那条绿），后者解释不了 `03/03b`：那时壳若活着，画面里必有整屏 `Background` 渐变（历史亮度 31.8 ⇒ 空壳也 ≈13，**不可能 0.0**）。⇒ **结论仍取「回归」**。

## 二、B 组四条

### B1 · `Viewport` 上挂着 `RectMask2D`（`RewardsScene.cs:3916`）—— 期望 True 实得 False
- **现象**：`rwVp.GetComponent<UnityEngine.UI.RectMask2D>() == null`。
- **谁的红**：戊2 新段（新增块 3871..4172 内），**首跑即红 ⇒ 从没绿过**。
- **分类：(b) 断言定错了**。
- **判据**：原版那件确实是 `RectMask2D`（`Reward Window/Content/Scroll View/Viewport`，`soft=(200,0)`），但**本工程的等效物是 `MenuDraw.Clip` / `GameWindow.Clip`**
  （`Shell/MenuDraw.cs:121` 那节标题就是「裁切（等效 `RectMask2D`）」）—— **全仓 `AddComponent<RectMask2D>` 零命中**，全文件只有 3916 这一句引用它；
  同族断言（`Editor/CollectionScene.cs:2435`）也只断「是那个 `RectTransform`」。断言把**我们的实现技术**当成了原版行为。
- **修法**：删掉这一条（或改成「建窗时 `Clip` 被设成 `Viewport` 那矩形」的行为断言）。上面 3915 的 `CheckAt(rwVp, 0,1920,300,932.5)` 已经把结构钉住了。
- **判别力**：⑧ 段那三条更强（12 格里视口外的不建 / 压边文字被夹在 1920 / 软边切线）—— 删了不会留下空洞。

### B2+B3 · `Tap To Continue`（`RewardsScene.cs:3967` 与 `:4150`）—— 两条**方向相反**地都红
- **现象**：`IsPremiumLocked=false` 那档期望「关着」实得「开着」；`IsPremiumLocked=true` 那档期望「打开」实得「关着」。
- **谁的红**：戊2。两条断言都把 **`IsPremiumLocked`** 当成了那个开关。
- **判据（反编译直读，且与戊2 自己的模型相反）**：`RewardWindow__Open.c:189` = `SetActive(tapToContinueText /*0x90*/, ctx[0x19] == 0)`；
  `RewardWindowContext__.ctor.c` 把 **`isPreview`（第 8 个形参）写进 `0x19`**、**`isPremiumLocked`（第 2 个形参）写进 `0x18`**
  （形参表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RewardWindowContext.cs`：`rewards, bool isPremiumLocked, onCollect, onClose, onPremiumUpgrade, cratePrefab, collectedRewards, bool isPreview, customTitle`）。
  ⇒ **原版：`Tap To Continue` 开 ⟺ `!IsPreview`**。同段 `:205-208` 的两态底图 / `:250` 自定义标题落点 / `:277` 粒子 + `DoRewardAnimation` 全走 **0x19 = IsPreview**；
  而 `:164` 的 `Premium Disclaimer` 走 **0x18 = IsPremiumLocked**。⇒ **两条断言的期望值都反了**（`(b)`）。
- **同一处还查出一条真偏离（(a) 类，铁律 11 要记）**：`Shell/RewardWindow.cs:378/379/381/383` 四处都用了 `ctx.IsPremiumLocked`，按原版应为 `ctx.IsPreview`；
  而 `:381` 的 `_premium`（Premium Disclaimer）**反过来**该用 `ctx.IsPremiumLocked`。**三档夹具里两个 flag 恒等**（`false,false` / `false,false` / `true,true`）⇒ 现在**观测不到**差别，
  但一旦出现「locked 但非 preview」（或反之）就会**静默画错**。
- **修法**：① 两条断言的期望反过来，并改掉 3968 括号里那句 `SetActive(!IsPremiumLocked)` 与 4151 的「唯一判据」措辞；
  ② `Shell/RewardWindow.cs` 那四处 flag 按原版对调（含文件头 `:37` 与 `:459` 的注释）；
  ③ **加一档 `IsPremiumLocked=true, IsPreview=false`**（以及 `false/true`）—— 这是唯一能分辨两个 flag 的夹具。
- **判别力**：③ 才是判别式；只反期望的话两个 flag 仍然分不开（现在两种实现都能过）。
- ⚠️ **同段第 4142 行（`TextOf`）反而是绿的** ⇒ 它隐含那一刻 TTC 节点是**激活**的，与 6 行后的 4150 观测互斥（中间只有只读助手 `CheckArt:5486` / `TintOf:116`）⇒ 见 §四。

### B4 · 视口外 2 格的「数量文字一个都不建」（`RewardsScene.cs:4082`）—— 期望 10 实得 8
- **现象**：12 个抽屉节点里只有 **8** 个建了数量文字。
- **分类：(b) 断言定错了** —— **实得 8 才是对的**。
- **判据（同段自己的算式 + 已绿的兄弟断言）**：内容宽 = 120 + 12×200 + 11×40 = **2960**，**居中**在 1920 视口（`ClampHi=+520` 那条已绿）
  ⇒ 内容左沿 = −520；格距 240、左侧内缩 60 ⇒ 第 **1** 格 −460..−260、第 **2** 格 −220..−20（**整块在视口外，在左边**）；右端第 **11** 格 1940..2140、第 **12** 格 2180..2380。
  ⇒ 整块在外的共 **4** 格，12 − 4 = **8**。断言只数了右边两格（文案自己写着「第 11、12 格」）。
- **修法**：期望值 `10 → 8`，文案改成「第 1、2、11、12 格（最左两格 + 最右两格）」。
- **判别力**：删掉 `ClippedText` 里那句 `MenuDraw.Visible` ⇒ 建满 12 ⇒ 仍会红（这条的改坏法依然成立，不因为改期望而变弱）。

## 三、汇总

| # | 红 | 分类 | 一句话 |
|---|---|---|---|
| A1–A4 | 4 张全黑（01/02/03/03b） | **(a) 回归** | 4170 新增的 `CloseAllWindows()` 把拍摄对象 `win` 一起关了 ⇒ 空帧（相机 clear=纯黑） |
| B1 | `Viewport` 无 `RectMask2D` 组件 | **(b)** | 全仓就没有这个组件；等效物是 `MenuDraw.Clip`，断言把实现技术当成了原版行为（首跑即红） |
| B2 | `Tap To Continue`（`=false` 档） | **(b)** (+ 附带 (a)) | 原版开关是 `IsPreview` 不是 `IsPremiumLocked`，两条期望值都反；同时查出 `RewardWindow.cs` 四处 flag 用错 |
| B3 | `Tap To Continue`（`=true` 档） | **(b)** (+ 附带 (a)) | 同上；且「这一句的唯一判据」这个说法不成立（两个 flag 恒等的夹具分不开） |
| B4 | 数量文字期望 10 实得 8 | **(b)** | 少算了最左边两格；正确答案就是 8 |

## 四、判不了的（如实 · 各附「还差什么」）

1. **B2/B3 里 4142 与 4150 的互斥观测**（也牵连 A 组第 9 点）：同一个 `FindChild(rw.transform,"Tap To Continue")` 在两个只读断言之间被观测成「有 `Label`（⇒ 活）」与「`activeInHierarchy == false`」。
   **还差一次带探针的跑**：在 4142 前加 `Debug.Log(CountByName(rw.transform,"Tap To Continue"))`、逐个匹配打印 `name / activeSelf / activeInHierarchy / 父链`，并打印 `_tap` 是其中哪一个（怀疑有重复节点）。
   ⛔ 这不改变 B2/B3 的结论（两条期望值与 **原版** 相反，无论节点实际是哪个态）。
2. **`GetComponentInChildren<Label>()` 单参重载在父链不激活时是否过滤**：`RewardsScene.cs:3938` 的注释说「会跳过」，但本次观测（4206/4289/4291 量出 710.2/408.6/480.69 而非 NaN）与它相反。
   **还差什么**：一条最小自检 —— 造 `parent.SetActive(false)`，断 `child.GetComponentInChildren<Label>() != null`。这条若为真，**`RewardsScene.cs:3938` 那句注释要按铁律 5 就地订正**（同族还有 A/B 两处依赖它的注释）。
