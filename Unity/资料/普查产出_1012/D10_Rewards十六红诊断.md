# D10 · `RewardsScene.Run` 十六条红 —— 只读诊断（找根因，⛔ 未修）

> 角色 = **只读诊断代理** · 2026-10-13 · 输入 = `/d/4/_tmp_view/rewards.log`（2.97 MB，2026-10-06 01:18 跑的那一份；
> ⚠️ 盘上还有 `/d/4/_tmp_view/baseline/rewards.log`，**不是**这一份）+ `Editor/RewardsScene.cs`（8465 行）·
> `Shell/RewardWindow.cs` · `Shell/WindowsManager.cs` · `Shell/DailyData.cs` · `Shell/DailyStreakPopup.cs`。
> ⛔ **一个字节没改生产代码 / 正本 / 别人的报告**；⛔ **没跑 Unity**（日志已在盘上）；⛔ 没动 git（只读了 `git diff HEAD` / `git log`）。
> 判据 = 反编译 + 本仓现读（逐条给行号）；**没查清的一律写在 §没查清，⛔ 不当猜测写**。

**总计**：`=== 合计：1626 通过 / 16 失败 ===`（`rewards.log:36450`）。**十六条归三个根因、两件事**。

---

## 结论表

| 红号 | 断言（截断） | 根因 | 证据（文件:行 / 日志:行） | 置信度 | 最小改法 |
|---|---|---|---|---|---|
| **1** | `★ 内容比视口宽 ⇒ 可滚上界 = +520`（0.00 ≈ 520±1） | **㈠ 同窗再开没重建内容**（A217② 的漏网据点） | `Editor/RewardsScene.cs:5005`（开 2 条）→ **`:5297` 重开 12 条、中间无 `Close()`** ⇒ `Shell/WindowsManager.cs:455-487` 的 `Open` 支早退；日志 `22888`（Dump 自相矛盾）· `22899` | 高 | 见 §㈠·改法 |
| **2** | `12 条奖励 ⇒ 12 个抽屉节点`（实得 **2**） | 同上 | `:5312` · `:5196`（前档那一条断的就是 2）· 日志 `22927` | 高 | 同上 |
| **3** | `整块落在视口外的 4 格…数量文字一个都不建`（实得 **2**） | 同上（那 2 格是**旧的**那 2 条） | `:5321` · 日志 `22939`（同段 `:5340` 的**相对**断言反而 ✓ ⇒ 自洽） | 高 | 同上 |
| **4** | `（A302 前提）找得到一格的数量文字伸进渐隐带`（最右顶点 1168.7） | 同上 —— 1168.7 = **2 条内容**时第 2 格右沿 1180 − 10 的落点 | `:5304` 的算式（内容宽 120+2×200+40 = 560 / 居中 680 / 第 2 格 980..1180）· 日志 `22963` | 高 | 同上 |
| **5** | `…最暗那个角 1.000 < 1` | **红 4 的算术后果**：那条 label（x ≤ 1168.7）**根本不在渐隐带**（内沿 1720）⇒ 剖面 `clamp01((1920−x)/200)` 恒 1 | `:5367-5379` · 日志 `23001` | 高 | 修红 4 即自动转绿 |
| **6** | `…而且有斜坡` | 同上（全 α=1 ⇒ 左右相等） | `:5380` · 日志 `23014` | 高 | 同上 |
| **7** | `★ 判别档：Premium Disclaimer 开着` | **㈠ 同窗再开没重建**（第二次重开 `:5400`）—— `Build()` 里那句 `_premium.SetActive(ctx.IsPremiumLocked && HasPremium(...))` 没跑 ⇒ 还是 **ctx#1（`IsPremiumLocked=false`）**那一版 | `Shell/RewardWindow.cs:1145` · `:5400-5417` · 日志 `23040`（Dump 已换成 `PremiumLocked`）· `23077` | 高 | 同 ㈠·改法 |
| **8** | `★ 判别档：揭示从头播`（1.00 ≈ 0.00） | 同上 —— `_reveal` 只在 `Build()` 里被置（`:1150`）⇒ 停在 1.00 | `Shell/RewardWindow.cs:1150` · `:653`（字段注释）· 日志 `23090` | 高 | 同上 |
| **9** | `…遮罩收在最拢那一档`（实测 0.0） | 同上 —— `MaskPad` 是 `MaskPadAt(1 − _reveal)` ⇒ 跟着停在「全开」 | `Shell/RewardWindow.cs:702` · 日志 `23104` | 高 | 同上 |
| **10** | `★ IsPreview = true ⇒ 换成 Preview 底图` | 同上（第三次重开 `:5427`）—— `ConfigureIsPreviewState` 那一段在 `Build()` 里 | `:5437-5438` · 日志 `23117`（Dump 已是 `…/Preview`）· `23128` | 高 | 同上 |
| **11** | `★ OnCollect == null ⇒ Collect Button 整颗关掉` | 同上 —— `_collect.SetActive(ctx.OnCollect != null)` 在 `Build()` 里；ctx#1 的 `OnCollect ≠ null` | `Shell/RewardWindow.cs:1147` · `:5448` · 日志 `23193` | 高 | 同上 |
| **12** | `★ IsPreview = true ⇒ Tap To Continue 关着` | 同上 —— `_tap.SetActive(!ctx.IsPreview)` 在 `Build()` 里 | `Shell/RewardWindow.cs:1146` · `:5456` · 日志 `23206` | 高 | 同上 |
| **13** | `★ IsPremiumLocked && 有高级档奖励 ⇒ Premium Disclaimer 打开` | 同上（与红 7 同一条节点、不同档） | `Shell/RewardWindow.cs:1145` · `:5458` · 日志 `23219` | 高 | 同上 |
| **14** | `（前提）本节起点那一格没领过`（期望 False、实得 **True**） | **㈡ 连登格在 A316 之前就被「关窗自动收」真收了** —— `§五` 的 `CheckAbsorbRule` 点压暗层 ⇒ `StreakAutoCollect()` | `Editor/RewardsScene.cs:5842-5844` + 助手 `:303` · `Shell/DailyStreakPopup.cs:222-223` · `Shell/DailyData.cs:909→849→852` · 日志 **`25096` + `25113`（栈 = `CheckAbsorbRule` ← `PointerLayer.ClickAt`）** | 高 | 见 §㈡·改法 |
| **15** | `（还原）起点是「没领过」⇒ 那一格的 Collect 又画出来了` | 同上 —— `:5974` 把那一格还原成 **`claimed0`（读到的就是 True）** | `:5922` · `:5974-5979` · 日志 `26140` | 高 | 同上 |
| **16** | `（A482 还原）那一格回到「没领过」⇒ Collect 又画出来` | 同上 —— A482 用的是**同一个** `claimed0`（A316 的 `{` 到 `:6063` 才闭） | `:6055-6060` · 日志 `26548` | 高 | 同上 |

**两句话的结论**：

* **红 1–13（十一红）＝ 一件事**：`Editor/RewardsScene.cs` 那扇 `RewardWindow` 被**同一个实例连开四次**（`:5005` / `:5297` / `:5400` / `:5427`），
  而 **A217②（H12，2026-10-12）把 `GameWindow.TryOpen` 照原版改成「`Open` 档早退、不重建」** ⇒ 只有第一次真建过，
  后面三次**只换了 `_ctx`、内容一格没重画**。⇒ **不是实现缺陷，是夹具的旧前提（「重开必重建」）被 A217② 推翻**。
* **红 14–16（三红）＝ 另一件事**：那不是 H42 预告的那件事（见 §㈡·「与 H42 预告的关系」）。它是 `§五` 的 `CheckAbsorbRule`
  真的点了压暗层 ⇒ 触发连登窗「关窗自动收」⇒ 那一格被**真收**，而 A316/A482 把「本节起点」**读成了出厂值**。

---

## ㈠ 红 1–13：同窗再开不重建（「重开必重建」这个夹具前提已被 A217② 推翻）

### 1. 机制（逐句）

```
Editor/RewardsScene.cs:5004   var rw = RewardWindow.Create(wm2);
Editor/RewardsScene.cs:5005   wm2.OpenWindow(rw, ctx#1);   // 2 条奖励 · IsPremiumLocked=false · IsPreview=false
                                                           //  · OnCollect≠null(collectCalls++) · OnClose≠null(closeCalls++)
                    …（:5193 Shoot；:5196 Check(childCount==2) ✓；:5202-5228 点 Collect 两下）
Editor/RewardsScene.cs:5297   wm2.OpenWindow(rw, ctx#2);   // ★ 12 条奖励 —— 中间【没有 Close() / Dismiss】
Editor/RewardsScene.cs:5400   wm2.OpenWindow(rw, ctx#3);   // 判别档：IsPremiumLocked=true · IsPreview=false · OnCollect=null
Editor/RewardsScene.cs:5427   wm2.OpenWindow(rw, ctx#4);   // 预览档：IsPreview=true · OnCollect=null · OnClose=closeCalls++
```

`WindowsManager.OpenWindow` → `win.TryOpen(data)`（`Shell/WindowsManager.cs:869`，`:846-882`）：

```
Shell/WindowsManager.cs:422-426   TryOpen(object data) { SetupData(data); return OpenByState(); }
Shell/WindowsManager.cs:455-487   OpenByState():
                                    state == Closed      ⇒ 激活 + state=Open + Open()（← 内容只在这一支重建）
                                    state == Background  ⇒ 只提回前台（不出声？出声）·
                                    state == Open        ⇒ ★【一个字段都不写就 return】← 三次重开全落这里
Shell/RewardWindow.cs:840         SetupData 覆写：_ctx = data as RewardWindowContext;   ← 所以 _ctx 每次都真的换了
Shell/RewardWindow.cs:843         Open() 覆写 = Build()                                  ← 一次都没被调
```

⇒ **症状是「自相矛盾的一扇窗」**：`Dump()` 报的东西（`:1433-1451` 读 `_ctx`）全是对的，**画出来的**全是最旧那一版。
证物 = 日志 `22888`：

```
RewardWindow：12 条奖励 · 态 Ok/Collect · 揭示 1.00 · pad (0.00,0.00,0.00,0.00) · 滚动 [0.0, 0.0] · …
              ↑「12 条」= ctx#2 已生效     ↑ 滚动 [0,0] = 内容还是 560 宽（2 条那一版）⇒ 自相矛盾
```

### 2. 为什么这是「断言/夹具错」而不是「实现错」

* **A217② 是照原版改的**（`资料/普查产出_1012/H12_同窗再开不重建.md`：`GameWindow.TryOpen` VA `0x180836120` 三档逐句读出；
  本仓现读 = `Shell/WindowsManager.cs:380-487`）。⇒ **生产行为对**。
* 同族先例：H12 自己就点出**另外 4 处既有自检**会因它变红，并给了「前面插一行 `Close()`」的改法；
  本处（`RewardsScene` 的 `rw`）**不在它那张清单里** ⇒ **漏网**。H12 全文只在 `:188` 顺带提过一次 `RewardsScene`。
* 🔴 **H12 §④ 那一句被本跑推翻**（就地订正）：它写
  「三个 `SetupData` 覆写（`CampaignRewardWindow` / `RewardWindow` / `MissionRerollPopup`）……今天**无可观测差异**；
  但**没有实跑**」—— **有**可观测差异，且**本跑的十一红就是它**：`_ctx` 被重喂成新 context，内容不重建 ⇒ 窗自我矛盾。

### 3. 最小改法（二选一；⛔ 我没动，只给判据）

**(a) 每次重开后补一次显式刷新（推荐）** —— 与 H12 §9 给 `CardDetailPopup.ShowCard` 的处置**同形**
（`Shell/CardDetailPopup.cs` 那处就是「复用同一个窗 + 自己显式 `Build()`」）：

```csharp
wm2.OpenWindow(rw, ctx#2);   rw.Build();     // ← 加这一句（:5297 之后）
wm2.OpenWindow(rw, ctx#3);   rw.Build();     // ← :5400 之后
wm2.OpenWindow(rw, ctx#4);   rw.Build();     // ← :5427 之后
```

* `Build()` 幂等：首句就清空 root 子件 + 清账 + 杀旧 tween（`Shell/RewardWindow.cs:1005-1023`）。
* 首开那一次会**跑两遍**（`Closed` 支的 `Open()` 已跑过一遍）—— 与 H12 §10 记的「有意如此」同一种；
  代价 = 一帧内多建一次，换来「不依赖 `CurrentState` 去猜会不会重建」。
* ⚠️ **三处都要补**：只补 `:5297` ⇒ 红 1–6 转绿、红 7–13 照红。

**(b) 每次重开前插 `rw.Close();`**（H12 给那 4 处的一行改法）—— ⚠️ **本处会踩一个坑，别照抄**：
`Shell/RewardWindow.cs:845-850` 的 `Close()` 覆写**先发 `_ctx.OnClose` 再走基类**，而 **ctx#1 的 `OnClose = _ => closeCalls++`**（`:5014`）
⇒ 在 `:5297` 前插 `Close()` 会让 `closeCalls = 1`，§⑩ 那句 `Check(closeCalls, 1)`（`:5477`）之后还会再 +1 ⇒ **变 2 ⇒ 多出一条红**。
要用 (b) 就得同时把 `closeCalls` 归零（或改成记增量）。

---

## ㈡ 红 14–16：连登格在 A316 之前就被「关窗自动收」真收了

### 1. 证据链（每一环都是现读）

| # | 事实 | 出处 |
|---|---|---|
| ① | `§五` 那段对连登窗调了吸收层判据 | `Editor/RewardsScene.cs:5842-5844` `CheckAbsorbRule("每日连登窗", ds.transform, "AbsorbHit", …)` |
| ② | 那个助手的**最后一步就是点压暗层** | 同文件 `:303` `CheckTrue(pl.ClickAt(5f, 5f), "…点面板外 (5,5)…")`、`:304` 断「⇒ 关窗」 |
| ③ | 连登窗那颗压暗层的动作 = **先收再关** | `Shell/DailyStreakPopup.cs:222-223`<br>`MenuDraw.ShadeHit(root, Shade, QShade, QContent, () => { DailyData.StreakAutoCollect(); Close(); }, "BackgroundHit");` |
| ④ | `StreakAutoCollect` 会**真收**（A400 修的） | `Shell/DailyData.cs:909-918` → 尾句 `return CollectStreak(StreakCollected);` |
| ⑤ | `CollectStreak` 置位 + 发奖 + 弹窗 | `Shell/DailyData.cs:849-858`（**`:852`** `_streakClaimed[SI(i)] = true;`） |
| ⑥ | 于是 `StreakRewardClaimed(5)` 变真 | `Shell/DailyData.cs:828` `_streakClaimed[SI(i)] \|\| SI(i) < StreakCollected` ⇒ `SI(5)==StreakCollected(5)` ⇒ 只由前一半决定 |
| ⑦ | **日志实锤**（两条 `Say` 的调用栈都落在助手里面） | `rewards.log:25096` `[Daily] 关窗时自动收取可领的那一格（原版 Close() = LiveOp.TryCollect(...)）`<br>`rewards.log:25113` `[Daily] 连登第 6 格已领取`<br>两条的栈 = `WindowButton:Click ← PointerLayer:ClickAt ← RewardsScene:CheckAbsorbRule`（`:25106-25108` / `:25124-25126`）<br>紧接着 `:25378` `✓ 每日连登窗：**点面板外 ⇒ 关窗**` |
| ⑧ | A316 那一节**在它之后**才开始，第一条就是这条前提 | `rewards.log:25653`（节标题）→ `:25665`（红） |

### 2. 三条红怎么从同一个 `claimed0` 长出来

```
:5921  int  dClaim   = DailyData.StreakClaimableDay;              // = StreakCollected = 5
:5922  bool claimed0 = DailyData.StreakRewardClaimed(dClaim);     // ★ 读到 True（= 被 §五 那一下收掉了）
:5923  Check(claimed0, false, "（前提）本节起点那一格没领过（出厂就是如此…）");   ← 红 14
:5925  DailyData.ForceStreakClaimedForTest(dClaim, false);        // ← 本节真正的测试都从这里起，所以 ① 之后的断言全 ✓
…
:5974  DailyData.ForceStreakClaimedForTest(dClaim, claimed0);     // ★ 还原成 True（本意是「还原成 False」）
:5977  CheckTrue(eBack == null || FindChild(eBack, "Collect") != null, …)        ← 红 15
…
:6005  {  // A482 子块（⚠️ A316 那个 { 一直到 :6063 才闭 ⇒ claimed0 在这里仍在作用域里）
:6055  DailyData.ForceStreakClaimedForTest(dAuto, claimed0);      // ★ 同一个 claimed0
:6058  CheckTrue(eAutoBack == null || FindChild(eAutoBack, "Collect") != null, …) ← 红 16
```

⇒ **一条根因、三处症状**；且 `:5974` / `:6055` 这两处**本意就是**「还原成本节起点」，只是「起点」取错了。

### 3. 与 H42 预告的那件事的关系（🔴 调度台点名要核的）

**不是同一件事。** H42 §三 预告的是**每日奖励**那条链（`§九(a)` 点抽屉 ⇒ 把**唯一**能进 `Unlocked` 的下标 2 收掉 ⇒
`_rewardClaimed[]` 没有写口 ⇒ A479 的正例 ①–⑥ 落不进去）；而红 14–16 读的是**连登轨**（`_streakClaimed[]` /
`StreakRewardClaimed` / `StreakClaimableDay`）—— **两个不同的数组、两扇不同的窗**（`DailyRewardPopup` vs `DailyStreakPopup`）。

实据两条：
* H42 预告的那个后果**今天是绿的**：日志 `29871` `✓ ★ A479⑦：（前提）这一刻**一格里没有可领的**` —— 已经按「只留负例」处理掉了（`Editor/RewardsScene.cs:6668-6689`）。
* 调度台裁的那件事（加 `ForceRewardClaimedForTest` 写口）**确实还没做**：`grep -n "ForceReward" Shell/DailyData.cs` ⇒ **零命中**
  （只有 `ForceDailyClaimedForTest` / `ForceStreakClaimedForTest` / `ForceLoginStateForTest` … 那一族，`:453-520`）。
  ⇒ 那是一条**待办**，**不是**本十六条红里的任何一条。
  （`_rewardClaimed` 的**唯一**写入点 = `CollectReward` 的 `:697`。）

### 4. 时间线（为什么这三条「首跑即红」）

* `§五` 那颗压暗层的点击区是 **2026-10-05（A81）**加的（`Shell/DailyStreakPopup.cs:209-223`）；那一刻 `StreakAutoCollect` 还是**空壳**。
* `git diff HEAD` 那份旧体（`Shell/DailyData.cs`）逐字是：`if (_streakClaimed[SI(StreakCollected)]) return;` + 只 `Say` ⇒ **不置位、不发奖**。
  ⇒ 那时点掉压暗层**不改变状态**，A316 的「起点没领过」自然成立。
* **A400（2026-10-12，未提交）**把空壳改成「转调 `CollectStreak`」的真实收（`Shell/DailyData.cs:868-918`）⇒ 那一下**从无害变成有状态**。
* A316（2026-10-12）/ A482（2026-10-13）是同批新写的；写「起点没领过」这条前提时**没跑过**（用户口径：A 表清完再跑）⇒ **首跑即红**。

### 5. 最小改法（⛔ 我没动）

把「起点」从**出厂值**改成**本节自己显式置位后的真值**（`:5925` 那句本来就已经置了）：

```csharp
// 🔴 2026-10-13 订正（铁律 5·b）：本节**不能**再读「出厂值」当起点 —— 上面 §五 的
//    `CheckAbsorbRule("每日连登窗", …)` 最后一步会真点压暗层（`RewardsScene.cs:303`），
//    而 `Shell/DailyStreakPopup.cs:222` 那颗的动作 = `StreakAutoCollect(); Close();`（判据 = A400，
//    `Shell/DailyData.cs:909`）⇒ 那一下**已经把这一格真收了**（日志 `[Daily] 连登第 6 格已领取`，栈在 CheckAbsorbRule 里）。
//    ⇒ 起点一律取「显式置位之后」的读数（原先读到的 True 是**前一段的残留**，不是出厂值）。
DailyData.ForceStreakClaimedForTest(dClaim, false);          // ← :5925 挪上来
bool claimed0 = DailyData.StreakRewardClaimed(dClaim);       // ← :5922 的读数
Check(claimed0, false, "（前提）这一格**已显式置成「没领过」**（§五 的关窗自动收已经真收过一次 —— 见本行注释）");
```

* `:5974` / `:6055` 两处**一个字都不用改**（它们仍取 `claimed0`，从此 = `false`）⇒ **红 14/15/16 一起转绿**。
* ⚠️ **不建议**的另一条路：把 §五 的 `CheckAbsorbRule` 换成「不触发 `ShadeHit` 的关窗」——那会把
  「点窗外 ⇒ 关窗 **+ 关窗自动收**」这条**真行为**的现场从自检里抹掉（A81 + A400 两人刚接上的东西）。

---

## §没查清（⛔ 不是结论，是下一件要做的）

1. **`rw.Build()` 显式调用的副作用没验**：`Build()` 里会 `PointerLayer.RegisterScroll(_scroll)`（`Shell/RewardWindow.cs:1057`）、
   重新建 `ShadeHit` / `AbsorbHit` / `Reward Claim` 子树。**幂等看着成立**（首句清空 root 子件与 `_punches`），
   H12 对 `CardDetailPopup` 的同款处置也已绿 —— 但**本窗没跑过**，跑之前别把它写成「一定安全」。
2. **`closeCalls` 在 (a) 方案下会不会受别的影响没核完**：只读到 `:5427` 为止 `§⑧·b` / `§⑨` 的 ctx 都不带 `OnClose`，
   §⑩ 的 `CheckAbsorbRule` 是唯一一次关窗（`Check(closeCalls, 1)`，`:5477`）——**没跑**。
3. **红 1–13 修完之后，A302 那一段会不会转成另一批红**：段头注释（`:5288-5292`）自己预言过
   「A310③ 一接 premium 高亮，本段量到的东西会变，尤其按位置数的 `qLabels == 8` 必须重新标定；
   到那时**这里该红是正常的**」。本轮**没验**现在的实现落在哪一侧。
4. **全 `Editor/` 的「同一实例重开」普查没做**（那正是 H12 §一 漏掉本处的原因）。便宜的粗筛：
   `grep -rnE "OpenWindow\([A-Za-z_][A-Za-z0-9_]*\s*[,)]" Editor/*.cs`（去掉 `new` / `Create(` 那些新实例）⇒
   命中数 `ShellScene 38 · RewardsScene 37 · CollectionScene 13 · MainMenuScene 5 · SettingsScene 4 · ShopScene 2`。
   ⚠️ **这是启发式计数**（同一变量重开与不同变量各开一次混在一起）⇒ **只当线索，不能当结论**；
   要不要按它铺一轮普查，请调度台裁。
5. **`DailyRewardPopup`（每日奖励窗）有没有同型的「同窗再开」站点**：本轮只核了 `RewardWindow` 的 `rw`（3 处）。
   `dr`（`:5712` 一带）与 `§九(a)` 那几处**没逐个数**。
