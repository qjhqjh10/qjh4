# F7 · RewardsScene 八条红修复（**写手** · 2026-10-11 · 批次1）

输入判据：`资料/普查产出_1010/D3_Rewards八条红诊断.md`（99 行，逐条分类 + 修法 + 证据；本轮**照它落地**，未重新论证）。
白名单内只改了**两个 `.cs`**：`MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` · `MyGame/Assets/CardPresentation/Shell/RewardWindow.cs`。
⛔ 没跑 Unity 自检（按简报由主对话跑 `RewardsScene.Run`，~11 秒）；✅ 跑过秒级类型检查（见 §六）。

---

## ① 结论（两组各一句）

- **A 组（4 张全黑 = (a) 真回归）**：戊2 新加的 `wm2.CloseAllWindows();` 把**主壳窗 `win` 一起关了**（它也在 `openWindows` 里），
  而那 4 张拍的**正是 `win`** ⇒ 空帧。**取 D3 推荐的「挪一句」修法**：把那句挪到 4 张截图**之后**（`RewardsScene.cs:4432`），
  并给这 4 张加**前置护栏**（`ShootShell`：拍之前断 `win.gameObject.activeInHierarchy`）。
- **B 组（4 条断言错）**：B1 删掉（把我们的实现技术当成了原版行为）；B2/B3 两条期望**按原版反过来**，同时把
  `Shell/RewardWindow.cs` 那**四处 flag** 订正成原版读的那个偏移，并**新增一档判别夹具**（`IsPremiumLocked=true, IsPreview=false`）
  —— 这一档才是两个 flag 的判别式；B4 期望 `10 → 8`（8 才对）。

---

## ② 证据

### A 组
1. **机制（代码级，逐句核过）**：`win` 在 `RewardsScene.cs:611` 被 `wm.OpenWindow(win)` 登记进 `openWindows`
   → `WindowsManager.CloseAllWindows()`（`Shell/WindowsManager.cs:893` 起，逐扇 `Close()`）
   → `GameWindow.Close()` 尾句 `gameObject.SetActive(false)`（`:458`）。
   **而 `Close()` 只关物体、不销毁** ⇒ 节点还在、断言量得到、**画面上什么都没有**。
2. **为什么是空帧**：相机 `clearFlags = SolidColor` + `backgroundColor = Color.black`（`RewardsScene.cs:596-597`）。
3. **不是抖动**：4 张 **md5 完全相同**（各 27 260 B，D3 §一·5）· 同一次运行另 7 张亮度 13.6–33.7 · 历史日志里这 4 条**绿过**
   （10-04 23:30 = 31.8 / 39.0 / 31.8 / 20.2）= **本批引入的回归**。
4. **与旧代码逐块比对（我另做了一次）**：`git show HEAD:…/RewardsScene.cs` 里**首个** `CloseAllWindows()` 在旧 `:3818` ——
   排在旧 3727 / 3729 / 3752 / 3760 那 4 张**之后** ⇒ 旧版本拍那 4 张时 `win` 是开着的。
5. **挪句话之后 `win` 为什么一定活着（静态推演，逐跳）**：这一节最后一件事是 §⑩ 的 `CheckAbsorbRule` 点窗外关窗
   ⇒ `rw.Close()` → `Manager.NotifyClosed(rw)`（`:811`）→ `wasTop` 为真 → `ShowPreviousWindow()`（`:848`）
   → 列表尾是**非弹窗** ⇒ `currentWindow = win`；而 `win` **全程没被 `Hide()` 过**
   （`HideAllWindows` 只在**全屏窗**开时跑，这一段里开的 `cw` / `cwWide` / `rw` **都是弹窗**），
   `ToBackground()` 只写 `CurrentState`（`:395`，**不动物体激活态**）⇒ **`win` 一直 active**
   ⇒ 那 4 张的现场 = **只有壳**（与历史绿的那一版同形）。

### B 组（**判据全部直读反编译，⛔ 没有一处读被测实现**）
1. `d:/2/tools/decomp_full/RewardWindowContext__.ctor.c:51-52`：
   **`*(param_1 + 0x19) = param_9`**、**`*(param_1 + 0x18) = param_3`**；形参表
   （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/RewardWindowContext.cs` 的 ctor 签名）逐位对上：
   `param_3 = isPremiumLocked`（第 2 个）、`param_9 = isPreview`（第 8 个）。
   ⇒ **`0x19` = `IsPreview`**、**`0x18` = `IsPremiumLocked`**。
2. `RewardWindow__Open.c` **逐句读偏移**：
   - `:164-185` `Premium Disclaimer`（`0x88`）← **`0x18` = `IsPremiumLocked`** `&& Any(premium)`；
   - `:189` `Tap To Continue`（`0x90`）← **`0x19`**（`SetActive(!IsPreview)`）；
   - `:205-217` `0x98/0xA0/0xA8/0xB0`（两态底图 + 两团底光）← **`0x19`**（`ConfigureIsPreviewState` 那四句）；
   - `:250` 自定义标题落点 ← `0x19`；`:280` `claimRewardParticles` ← `0x19`；`:283` **`if (0x19 == 0) DoRewardAnimation()`** ← `0x19`。
   ⇒ 本工程 `Shell/RewardWindow.cs` 那四处读的是 `IsPremiumLocked` ⇒ **(a) 类真偏离**（三档夹具里两个 flag 恒等 ⇒ 看不出来）。
3. B1：全仓 `AddComponent<RectMask2D>` **零命中**，等效物是 `MenuDraw.Clip` / `GameWindow.Clip`
   （`Shell/MenuDraw.cs:121` 那节标题就写着「裁切（等效 `RectMask2D`）」）⇒ 那条断言**首跑即红、从没绿过**。
4. B4：内容宽 = 120 + 12×200 + 11×40 = **2960**、**居中**在 1920 视口 ⇒ 左沿 **−520**
   （同段 `ClampLo = −ClampHi` 那条已绿，`PadL=60` / `ItemW=200` / `ItemSpacing=40` 见 `RewardWindow.cs:131/140`）
   ⇒ 第 **1** 格 −460..−260 · 第 **2** 格 −220..−20 · 第 **11** 格 1940..2140 · 第 **12** 格 2180..2380
   ⇒ **整块在外 4 格，12 − 4 = 8**。⚠️ 这条与「按中心点判」也同解（4 格中心同样在外）⇒ **两种口径都是 8**，判据稳。

---

## ③ 改动清单

### `MyGame/Assets/CardPresentation/Shell/RewardWindow.cs`（四处 flag + 同源注释）

| 位置（改后行号） | 原 | 改 |
|---|---|---|
| `:398` | `ConfigureIsPreviewState(ctx.IsPremiumLocked)` | `ConfigureIsPreviewState(ctx.IsPreview)` |
| `:399` | `_premium.SetActive(ctx.IsPreview && HasPremium(…))` | `_premium.SetActive(ctx.IsPremiumLocked && HasPremium(…))` |
| `:400` | `_tap.SetActive(!ctx.IsPremiumLocked)` | `_tap.SetActive(!ctx.IsPreview)` |
| `:404` | `_reveal = ctx.IsPremiumLocked ? 1f : 0f;` | `_reveal = ctx.IsPreview ? 1f : 0f;` |

同源注释一并订正（铁律 5，保留更正痕迹）：
- 文件头「三组状态」（`:36-45`）—— **①② 原来写反了**（① `IsPremiumLocked` 写成管整套 Preview、② `IsPreview` 写成只多管 Disclaimer）；
- `RewardWindowContext.IsPremiumLocked / IsPreview` 两条字段注释（`:63-72`）；`:372` 的 ⑦ 段注释；`:390-404` 的 ⑨/⑩ 段；
- `ConfigureIsPreviewState` 的 `<summary>`（`:479-482`，原写「参数是 `IsPremiumLocked`」）。

### `MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`

| # | 行（改后） | 改动 |
|---|---|---|
| A | **`:4427-4432`** | **新增**：挪过来的 `wm2.CloseAllWindows();`（放在 §四 之前；附「为什么不能再往前挪」） |
| A | `:4264-4273` | **删除**原有的那一句 + **就地订正说明**（根因、4 张 md5 相同、以后靠护栏拦） |
| A | `:673-682` | **新增** `ShootShell(GameObject shell, string file)`：先 `CheckTrue(shell.activeInHierarchy, …)` 再 `Shoot(file)` |
| A | `:4306 / 4308 / 4402 / 4410` | 4 张壳截图 `Shoot(...)` → **`ShootShell(win.gameObject, ...)`** |
| A | `:4303-4305` | 那 4 张上方的说明改成「**四张**『屏上只有壳』…」+ 记这次为什么又踩 |
| B1 | `:3928-3941` | **删掉** `Viewport` 挂 `RectMask2D` 那条断言（**唯一被删的一条**），换成一段「判据在本工程是 `MenuDraw.Clip`、删了不留空洞」的订正说明 |
| B2 | `:3996-4006` | 档①两条：`Premium Disclaimer` 改「`IsPremiumLocked = false` ⇒ 关着」；`Tap To Continue` **期望反过来**（`IsPreview = false` ⇒ **开着**），并改掉括号里那句 `SetActive(!IsPremiumLocked)` |
| B2 | `:3990` | `Reward Background Get Reward` 那条的文案 `IsPremiumLocked = false` → `IsPreview = false`（★与实测无关的措辞订正） |
| B3 | `:4256-4266` | §⑨ 两条：TTC 改「`IsPreview = true` ⇒ **关着**」、Disclaimer 改「`IsPremiumLocked && 有高级档奖励`」；删掉「这一句的唯一判据」那句错话 |
| B3 | `:4225` | `Preview` 底图那条的文案 `IsPremiumLocked = true` → `IsPreview = true` + 补一句「本档分不出两个 flag」 |
| B3 | `:4252-4254` | 「锁着的那一档不播揭示」→「**预览那一档**不播揭示」，判据行号改成 `:283`（读 `0x19`） |
| B3 | `:4173-4211` | **新增「⑧·b 判别档」**：`IsPremiumLocked = true` + `IsPreview = false` + 高级档奖励 + `OnCollect = null`，**5 条断言**（底图那一套 / TTC / Disclaimer / 揭示从头播 / `padding ≥ 950`） |
| B4 | `:4120-4148` | 期望 `10 → 8`、文案改成「第 **1、2、11、12** 格（最左两格 + 最右两格）」+ 算式与坐标；**并补一条相对断言**（见 §③·补） |
| — | `:3969-3981`（③ 段那条长注释）· `:3986` | **订正一条错注释**（见 §④·2） |

**断言条数净变化**：**删 1 条**（B1）· **加 6 条**（判别档 5 + B4 的相对断言 1）· 其余**只改期望/文案、不增不减**。

### ③·补 —— 相对断言（血的教训③「期望值抄一份错文档 = 同源错误」）
B4 的绝对期望值（8）虽然是从**原版字面量**算出来的，但它仍然是一份「纸面推导」⇒ 按本会话的教训再配一条**不写死任何常数**的：
`:4130-4148` 用**场上节点的实际位置**（`PxOf(ch.position.x)`，`MenuDraw.Node` 把节点摆在格心）数出「格心落在 0..1920 的格子数」，
再断 `qLabels` 等于它 —— 夹具里「格心在外」与「整块在外」同解（四个越界格的最内侧分别是 −20 / −20 / 1940，
第 3、10 格则整格在内）⇒ 两条口径互为独立的一对。
**改坏法**：`CenteredContent` 改成左对齐 ⇒ 绝对那条与这条**同时**红（这条**不靠** 520 那个数）。
**它多拦一类**：几何一改（`ItemW` / `ItemSpacing` / 内缩变了）时，绝对那条会变成「抄旧的」假红，而这条跟着场上真值走。

### ③·判别力（每条逐条写清「改坏哪里它会红」）

| # | 断言 | 改坏法 ⇒ 红 |
|---|---|---|
| A 护栏 | `ShootShell` 的前置 `win.activeInHierarchy` | **把任何一句 `wm2.CloseAllWindows()` 挪到这 4 张之前** ⇒ 立刻红（不必等到出黑图）；把 4 张改回裸 `Shoot` ⇒ 这类改动又变回静默 |
| A 挪句 | 那 4 条的「空图护栏」（平均亮度 > 3） | 挪句之后壳活着 ⇒ 4 张各自非黑且 **md5 互不相同**（旧的「4 张 md5 全同」是最强线索） |
| B1 | （已删） | —— 删了**不留空洞**：结构由 `CheckAt(rwVp, …)` 钉住，裁切由第 ⑧ 段三条（视口外不建 / 压边字夹在 1920 / 软边逐顶点对原版剖面）咬住 |
| B2 | 档① `Tap To Continue` **开着** | `RewardWindow.cs:400` 改回 `!ctx.IsPremiumLocked` ⇒ 本档（`false,false`）**这条仍绿**（两 flag 恒等）—— **单靠它分不出**，判别力在 ⑧·b |
| B3 | §⑨ `Tap To Continue` **关着** / Disclaimer **开着** | 同上：改回 `IsPremiumLocked` 本档照样绿；**判别力在 ⑧·b** |
| ⑧·b | 5 条（底图 / TTC / Disclaimer / 揭示 / padding） | **逐条咬住那四处 flag**：`ConfigureIsPreviewState` 改回 `IsPremiumLocked` ⇒ 底图那条红 · `_tap` 改回 ⇒ TTC 那条红 · `_premium` 改回 `IsPreview` ⇒ Disclaimer 那条红 · `_reveal` 改回 `IsPremiumLocked` ⇒ 揭示两条（进度 0 / padding ≥ 950）红 |
| B4 | `qLabels == 8` | 期望改回 10 ⇒ 红；删掉 `ClippedText` 那句 `MenuDraw.Visible` ⇒ 建满 12 ⇒ 红（**不因改期望而变弱**） |
| B4 相对 | `qLabels == 按量出来的格心数` | `CenteredContent` 改左对齐 / 格距或内缩变了 ⇒ 红（**不靠** 520 / 8 这两份纸面数） |

---

## ④ 没查清的部分（含 D3 那两条「判不了」我的处理）

### 1. D3 §四·1「`:4142` 与 `:4150` 的互斥观测」—— ✅ **查清了：不矛盾，也不需要「重复节点」**
同一条 `FindChild(rw.transform, "Tap To Continue")` 被观测成「有 `Label`（⇒ 取得到）」与「`activeInHierarchy == false`」，
**两件事用的不是同一把尺子**：
- `Label` 在**子件** `Tap Text` 上，而 `_tap.SetActive(false)` 关的是**父节点自己**；
- `GetComponentInChildren<T>()` 单参重载（`includeInactive = false`）**只跳过「自己 `activeSelf == false` 的子件」，⛔ 不查祖先链**
  —— **判据 = 本仓已有结论**：`资料/已知的坑.md:3865-3875`（2026-10-11 · F4：「默认重载**不查祖先链**；『取得到』与『在不在画面上』是两把尺子」）。
- **本轮日志两条独立实证**：① `:4142` 那条**绿**（那一刻节点**自身不激活**，字照样取到）；
  ② 4 张壳截图**之后**那三条边缘断言（`Premium Panel/Title` 右/左 · `Campaign Header/Title` 左）在**壳窗已被关掉**
  （整个 `win` 子树 `activeInHierarchy == false`）的情况下仍量出 **710.2 / 408.6 / 480.69**，**不是 NaN**
  （`TextLeftPx`/`TextRightPx` 取不到 `Label` 时**恒回 NaN** ⇒ 一定是取到了）。
⇒ **同一条事实同时解释了 A 组 §一·9 与 B2/B3 §四·1 两个「悬案」**；⛔ 没有「重复节点」，也不必加探针跑。
（**顺带解掉 D3 §一·9 的另一半**：它担心的「壳在那几行时是活的 ⇒ 黑图另有一层成因」**不成立** —— 壳确实是关着的。）

### 2. D3 §四·2「单参 `GetComponentInChildren<Label>()` 是否过滤不激活」—— ✅ **判为「不查祖先链」⇒ 已按铁律 5 订正注释**
订正了 `RewardsScene.cs:3969-3981`（③ 段那条长注释）与 `:3986` 两处：
原来写「`GetComponentInChildren<T>()` 单参重载**会跳过不激活的子件**」⇒ 改成「该重载**不查祖先链**（父链关着照样取得到），
**真正量不到的是渲染真值**（未激活时 TMP 量不出 `textBounds`，见坑①）」—— 后半句**原样保留**（那是另一回事、且成立）。

### 3. ⛔ 仍然「没跑到」的一处（如实记）
**这 4 张图现在到底是不是非黑，我没跑 Unity ⇒ 没有实测。** 支撑 = §②·5 的逐跳静态推演 + 旧文件同形现场曾绿过。
**判据（请主对话在复跑时对一眼）**：`01/02/03/03b` 四张的**平均亮度**应当回到 **31.8 / 39.0 / 31.8 / 20.2 量级**，且 **md5 各不相同**。

### 4. 一处**没有落地**的可选加固（如实记，不是漏）
D3 §一·8② 建议的「4 张同名图**不许 md5 相同**」护栏**没做** —— 简报只点了 ①（`activeInHierarchy` 前置），
而 ① 咬的正是这次的根因（拍摄对象被关掉）。⛔ 若要做，得在 `ShootShell` 里留一份「本轮拍过的 md5」表。

---

## ⑤ 顺手发现（**只报不改**）

1. 🔴 **`资料/已知的坑.md` 里同族两条口径不一致**（该文件在红线名单里，我没动）：
   `:3715` 的同族第 ① 条写「**单参 `GetComponentInChildren<T>()` 默认只找激活对象** ⇒ 拿它当存在性判据会漏」——
   读起来像「父链关着就取不到」，而那半句是**错的**；同文件 `:3865-3875`（F4）已经把它写细了
   （**只跳过自己 inactive 的子件，不查祖先链**）。**建议**：把 `:3715` 那一条补一句指针指到 `:3865`，
   免得下一个人照着它把「取得到」当成「不活」（D3 §四·1 那次绕圈就是这么来的）。
2. `Shell/RewardWindow.cs:536-537` 的 `Dump()` 把状态打成 `PremiumLocked/Ok · /Preview//Collect` ——
   **读的两个 flag 现在与原版一致**，但那串文案把「态」混成一个词（`Ok` 与 `Preview` 是两把尺子）。
   纯诊断输出、不影响任何断言 ⇒ **只记账**。
3. `RewardsScene.cs` 里 `CloseAllWindows()` 还有 **5 处**（`:4475 / 4662 / 5024 / 5057` + §四末尾那处），
   全部排在**各自那扇窗的截图之后**，本轮核过**没有第二处会关掉拍摄对象**（其余截图拍的都不是 `win`）。
   ⇒ **风险点只剩「以后有人把 `CloseAllWindows()` 往 4 张壳截图前面挪」**，现在有护栏咬住（见 §③·A）。

---

## ⑥ 跑过的检查

| 检查 | 读数 |
|---|---|
| **秒级类型检查** `TMPDIR=/tmp/wf_f7 bash d:/4/Unity/工具/typecheck.sh` | **运行时程序集 0 错 · 编辑器程序集 0 错**（改了这两个 `.cs` 之后跑的最后一次） |
| **Unity 自检** | ⛔ **没跑**（按简报：由主对话跑 `RewardsScene.Run`，~11 秒）。**注意**：Unity 只能串行跑 |
| 行尾 | 两个文件**纯 LF**（`RewardWindow.cs` 0 CRLF / 548 LF · `RewardsScene.cs` 0 CRLF / 5638 LF）· ⛔ 没用过 `sed -i`，一律 Edit 工具 |
| 越界 | 只动白名单里的两个 `.cs`（`RewardWindow.cs` 是**未入 git 的新文件**，`git status` 显示 `??`）· ⛔ 没动 git · ⛔ 没改正本与 `已知的坑.md` |

### 复跑时对一眼（**期望合计**）
`1163 通过 / 8 失败` ⇒ 预期 **1176 通过 / 0 失败**：+7（8 条红里 7 条翻绿；B1 那条被删、不计）
+6（判别档 5 条 + B4 的相对断言 1 条）· ⛔ 若通过数**低于**这个数，说明有断言被**吞掉**（早退/异常）—— 按 `已知的坑.md:3858`
那条口径查（通过数也是判据）。
