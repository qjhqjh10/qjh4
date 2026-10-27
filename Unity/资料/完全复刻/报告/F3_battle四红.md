# F3 · `BattleScene.Run` 4 条红的修复（执行代理 `F3BAT` · 2026-10-27）

> 诊断依据 = `资料/完全复刻/报告/D2_battle四红.md`（**未重新诊断**，逐条照它的根因与修法落地）。
> 三处互不相干：①②（夹具）· ③（`BattleDriver` 飘字锚点）· ④（`W11ShakeAssert` 断言公式）。

## 一、结论

| # | 红 | 性质 | 处置 |
|---|---|---|---|
| ① | bloodthirst 第 1 次触发 | 夹具造了真机不可能的状态 | 夹具把行动方拨到事件那一侧（`ctx.Active = 1`），用完还原 |
| ② | bloodthirst 第 2 次触发 | 同 ①（同一条夹具） | 同上；**并补一条反向断言**把新闸钉住 |
| ③ | 飘字没落在挨打那张卡上 | 锚点吃了相机的瞬态位姿 | 取锚点前 `CardFeel.SettleShake()` |
| ④ | W11 HUD 根与相机同向等量 | 断言公式漏了 `M·(hudHome − camHome)` 整项 | 改断**屏空间不变式**（像素 + 相机空间双断） |

**必跑三项全过：**

1. `TMPDIR=/tmp/wf_f3 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（终态复跑一次）。
2. `bash 工具/ruleprobe.sh check` ⇒ **rc=0**；指纹 `c9a64798…` **未变**，全池 1121 张解析差异 0 行。
3. `bash 工具/ruleprobe.sh play check` ⇒ **rc=0**（③ 是表现层、结算指纹未动，符合预期）。

⛔ 没跑 Unity（`BattleScene.Run` 归调度台）。两个文件行尾仍是**纯 CRLF、无裸 CR**（二进制数：`crlf == lf`，`bare_cr == 0`）。

## 二、证据（文件:行号 · 逐条「改前断什么 / 改后断什么 / 🧨 改坏哪处仍会红」）

### ① ② bloodthirst（`Editor/BattleScene.cs:10851-10905`）

**闸本身不动**（`Battle/BattleDriver.cs:24656-24662`，判据 `CardScript__ActivateBloodThirst.c:15-21` 的四条合取 + `ResolveBloodThirstAnim.c:21-24`）。

- **改前**：夹具直接发 `Player = 1` 的 `EvtKind.Trigger`，而本局 `ctx.Active = 0`（`PinFirstSeat(driver, 0)`，`:46961`）
  ⇒ 新闸（`e.Player != Ctx.Active`）早退 ⇒ 实得 `(0 张 / 演过 N 次)` ⇒ 两条都红。
- **改后**：`int btSide = 1; int btActiveWas = ctx.Active; Check(btActiveWas >= 0, …前提…); ctx.Active = btSide;`
  （同 `:10480-10481` 那个先例的做法），两段 `Step` 之后 ⑤ 反向断言跑完再 `ctx.Active = btActiveWas;` 还原。
- **新增反向断言**（`:10881-10904`）：「触发点**不在行动侧** ⇒ 一次都不演」。
  ⚠️ 关键细节：**先 `tf3.Remove(BloodThirst)` 清去重表**（= 原版 `RemoveStatusAnim`，`Core/TraitFrames.cs:961`），
  否则第三条会被 `DisplayTrigger` 自己的「已有 ⇒ 一条都不播」守卫挡下（`TraitFrames.cs:930`）⇒ 断言**空转**。
  另配两条硬前提：`TriggerCount == 0`（证明去重表真清空了）、`ctx.Active != btSide`（证明两则确实异侧）。
- 🧨 **① 改坏哪处仍会红**：夹具不拨 `ctx.Active`（或把 `Player` 改回非行动侧）⇒ `(0 张 / 演过 N 次)` ⇒ 红。
  🧨 **② 改坏哪处仍会红**：把闸去掉 ⇒ 反向那条实得「演 2 次、建 1 张框」⇒ 红；只绑/只播一条 ⇒ 正向第 1 条红。

### ③ 飘字锚点（`Battle/BattleDriver.cs:25039`，紧随其后 `:25040` 取 `at`）

- **根因**：`layout.DropTargetWorld(e.Slot)` 走 **实况相机投影**（`BoardLayout.cs:523-535` 的 `boardCam.WorldToScreenPoint`），
  而这一刻 `boardCam` 正被**命中帧震屏**推着（`PlayHitFrameShake` 把 `boardCam` 当 `extraCam` 传，`BattleDriver.cs:22666`）；
  震屏总时长 `0.1+0.3 = 0.4 s`、伤害 `Hit` 在 `Attack + MeleeDamageLag = 0.4 s` ⇒ **取锚点那一拍正好是最后一帧**，
  残余位移被烘进飘字（实测偏差 **0.1453**，纯 x 方向）。
- **改后**：`CardFeel.SettleShake();` 再取 `at`。`SettleShake` 走 tween 的 `OnKill`（`CardFeel.cs:1490-1500`）
  ⇒ 相机 / `boardCam`(extraCam) / `HudRoot` **各自精确归位**，投影回到常规位姿。
- **生产行为**：这是本次唯一一处生产改动。诊断判它视觉无损（此刻震屏只剩最后一帧，收尾归位与下一帧的 `OnKill` 同拍）。
- 🧨 **改坏哪处仍会红**：删掉这句 ⇒ 只要震屏幅度 ≠ 0，飘字就沿**震屏方向**偏（现档位是纯水平 20.38 世界单位 ⇒ 远超 0.05 容差）⇒ `BattleScene.cs` 那条「飘在挨打那张卡上」红。

### ④ W11 HUD（`Editor/BattleScene.cs:48613-48670`，`static class W11ShakeAssert.Check`）

- **改前断**：`hudRoot.position ≈ home + dA`（纯平移）—— 实现是绕相机位置的刚体变换
  `hudRoot.position = camHome + d + M·(hudHome − camHome)`（`Core/CardFeel.cs:1473`）⇒ 漏了整项 `M·(hudHome − camHome)`；
  本场景 `hudHome = (0,0,0)`、`cam` 在 `(0,0,−20)` ⇒ 恒差 ≈ **20 世界单位**（阈值 1e-4）⇒ **必红**，而接线本来就是好的
  （同意图的老断言 `BattleScene.cs:24099-24113` 一直绿）。
- **改后断**（二选一里选了**屏空间不变式**）：在 HUD 上钉一个点（HUD 局部 `(1,1,0)`，**故意离开相机光轴**），
  取震前 `WorldToScreenPoint` 与震后 `WorldToScreenPoint`，要求**相同**；同时断**相机空间**坐标相同（等价形式）。
  - **为什么选它而不是照抄 `:24099-24113` 的算式**：① 那是「屏幕不变」的**语义本身**，实现以后怎么改 `M` 都不影响它；
    ② 抄算式要拿 `camHomeRot` + `ShakeOrientationRef`（二者都在 `Run` 里、本文件尾的静态类**拿不到**），
    而且**把实现里那套算式再抄一遍 = 与实现同源 = 自证**（铁律「审查盯原版判据，不盯我们自己的常量」）。
    ③ 成本更低：不需要额外记录 `hudHomeW` / `hudHomeRotW`，也不需要在静态类里复制 `AngleDegTiny` 那套。
  - **两条一起断的理由**：像素那条是字面意义上的屏空间，但依赖批处理下 `pixelWidth/Height` 的取值；
    相机空间那条与它**数学等价**（正交/透视都一样：屏幕 = 相机空间经同一个投影矩阵的线性映射）且不依赖像素尺度
    ⇒ 像素那条万一度量退化，也不会让整条变空转。
  - **前提（硬检查，⛔ 不写 `if (hudRoot != null) Check(…)` 那种静默跳过）**：
    `check(hudOk, "…前提不成立 ⇒ 下面两条一次都没跑过（⛔ 不算通过）")`；
    另有防空转前提 `|dA.x| > 1`（相机真的横着动了）且钉子点相机空间横向分量 `> 0.1`（在光轴外）。
- **换算核对（为什么这条结构上真的能抓到旧错）**：已 shake ⇒ `HUD' = C + d + M(H − C)`、
  `R' = R·q`、`C' = C + d` ⇒ 相机空间 `R'⁻¹(P' − C') = R⁻¹(P − C)` **恒等**。
  若 HUD 只跟竖直分量（或不跟），残差 ≈ `|dA| ≈ 40` 世界单位 ⇒ 两条都爆表。
- 🧨 **改坏哪处仍会红**：`CardFeel.cs:1473` 退回纯平移 / 只跟竖直分量 / 漏掉 `hudRoot.rotation = M·hudHomeRot`
  ⇒ 钉子点在屏上/or 相机空间里移位 ⇒ 红。
- 阈值：屏幕 `< 0.1 px`、相机空间 `< 1e-3` 世界单位（float32 在 ±20 量级上是 ~1e-6，留了 3 个数量级余量；旧错是 40 量级）。
- 附：`hudRoot == null` 那条前提原先写「下面三条一条都没跑过」，实际条数已不止三条 ⇒ 就地改「下面几条」（**零行数变化**）。

## 三、没查清的部分 / 还欠

1. 🔴 **③ 的长期正解没做**：原版那颗 `DamageCounter` 是 3D 卡的**子件**（真机上跟着卡一起晃），
   我们仍是「世界坐标 + 静止」。本波只堵了「锚点吃瞬态位姿」这一半；**飘字跟随震屏位移**要单开一条。
2. ⚠️ **震屏幅度律本身未经真机对照**（`CardFeel.cs:4146-4152` 自己就这么写）：20～81 世界单位 = 2～8 个屏高，
   `rotationGain` 还同时乘到 `|dir|×2/4/8`。若这条幅度律判错，③ 会「因为另一个原因变绿」；而「锚点走实况相机」的隐患本次已收口。
3. ⚠️ **`Camera.WorldToScreenPoint` 在批处理下的像素尺度未实测**（正因为如此才加了相机空间那条等价断做兜底）；
   若 `BattleScene.Run` 里像素那条出现异常读数，先把它的残差印出来看是不是度量退化。
4. ⚠️ **`ctx.Active` 临时拨到 `1` 期间 `RefreshAll()` 会翻 HUD 的回合态**（`SetDeckLight` 等，`BattleDriver.cs:31817` /
   `:34071`），只影响表现层，段末已 `ctx.Active = btActiveWas` 还原 + `RefreshAll`；**没跑 Unity 验**这一段的连带影响。
5. 🔴 **账的行号漂移**（本波在这两个文件里插了行，`账/*.tsv` 的 `文件:行号` 会整体漂）：
   `BattleScene.cs` **+79 行**：`≥10851` 起 **+12** · `≥10881` 起再 **+25**（合计 `≥10906` 处 **+37**）·
   `≥48617` 起再 **+23**（合计 `≥48640` 处 **+60**）· `≥48649` 处再 **+19**（合计 **+79**）。
   `Battle/BattleDriver.cs` **+13 行**：`≥25027` 起（原 `Vector3 at = …` 在 `:25027`、现 `:25040`）。
   ⇒ 需在**没有 `.cs` 写手**的同步点跑一次 `check_ledger_points --all-A --fix`（⛔ 我没动账主表）。
6. ⚠️ `SettleShake()` 的 doc 仍自称「**自检专用**的收尾口」（`CardFeel.cs:1710-1711`），而现在生产路径（`PlayHitFeel`）也在用
   ⇒ 这段文档口径过期了，建议下一波顺手订正（本波 `CardFeel.cs` 不在我的白名单，没碰）。

## 四、改动清单

| 文件 | 位置（改后） | 改动 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | `:10851-10862` | 夹具：`btSide` / `btActiveWas` / 硬前提 / `ctx.Active = btSide`（+12 行） |
| 同上 | `:10881-10905` | 新增反向断言「非行动侧 ⇒ 一次都不演」（含 `tf3.Remove` 清去重表 + 两条硬前提）+ `ctx.Active` 还原（+25 行） |
| 同上 | `:48609` | 文案「下面三条」→「下面几条」（0 行数变化） |
| 同上 | `:48616-48638` | 记 `homeRot` / 钉点 `pinLocal (1,1,0)` / 震前屏空间与相机空间基线（+23 行） |
| 同上 | `:48649-48670` | ⑤b：前提硬断 + 防空转前提 + 屏空间/相机空间不变式（原 3 行 → 22 行，净 +19） |
| `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | `:25039` | `CardFeel.SettleShake();` 插在 `Vector3 at = layout.DropTargetWorld(...)`（`:25040`）之前（+13 行，含判据注释） |

⛔ 未碰：`git` / 两张正本 / 账主表 / 基线 / 别人的报告 / `RuleEngine/**` / `CardFeel.cs` / 白名单外任何文件。
⛔ 未新增 `EvtKind.Ability` 的代码出现点。

## 五、顺手发现（未改，供调度台分流）

1. **`SettleShake()` 的「自检专用」标签已失真**（`CardFeel.cs:1710`）：现在生产路径也在调 ⇒ 文档口径要更新；
   另外它是「**杀掉 tween**」而不是「把相机摆回常规位姿再继续」，若将来出现「挨打时正在播别的屏震」，
   这里会**顺带截断那个屏震**（当前档位下无碍：命中帧震屏本来就在这一拍结束）。
2. **`TraitFrames.Remove()` 不清累计量**（`TraitFrames.cs:961`）：只摘两本字典，`TriggerPlayCount`(= `_triggerPlays`)
   与 `StartPlayCountOf` 的历史**保留** ⇒ 自检要用「清零」语义时**必须自己记基线**（我这块就是按这个写的）。
   同时它**不清 `_prev`** —— 关键词还在单位上时不会自动重挂，这一点对断言也有影响（别指望 `RefreshAll` 会补回来）。
3. **`BattleScene.cs` 里「夹具 vs `ctx.Active`」是系统性的**：`PinFirstSeat(driver, 0)` 让全文件 40+ 处夹具都假设我方先手，
   而新增的表演闸开始**真的读 `ctx.Active`** ⇒ 以后再往这文件加「由某一侧触发」的夹具，必须显式声明「事件侧是否行动侧」
   （`BattleScene.cs:10851-10862` 这段是新的样板）。第 9b/9c 两节会**拔掉钉子**，那里若也发 Trigger 类事件要留意。
4. `D2_battle四红.md` §顺带那条「`grep -c "✗"` 会多数出文案里的 ✗」仍然成立（本次日志口径未复核，未跑 Unity）。
