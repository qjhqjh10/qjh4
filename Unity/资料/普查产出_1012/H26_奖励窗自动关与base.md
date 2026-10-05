# H26 · A448（战役奖励窗「领完自动关」）+ A459（两处同族欠 `base`）—— 写手报告（2026-10-12）

> 独占文件：`Shell/CampaignRewardWindow.cs` · `Shell/InboxWindow.cs` · `Shell/RewardWindow.cs`（**只有这三个被本件改过**）
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改正本 · ⛔ 没越白名单（`Editor/*` 只**读**不写）
> ⚠️ **本报告的「实测」一律是 `menu_dump.py` / 反编译 / 现读源码**；本件**没跑自检**（用户口径：A 表清完再跑）。

---

## 〇、🔴🔴 最高优先级：工作区发生**批量文件覆盖**（不是本件造成的，但**卡住所有人的自检**）

**2026-10-12 23:38:13**，**10 个 `.cs` 在同一秒被写成同一份内容** —— `Assets/WarpforgeArena1/Runtime/ArenaByArmy.cs`
（123 行 / 9012 B）的**逐字节副本**。逐个实查（`head -1` + `wc -l` + mtime，**全部 123 行 / 9012 B / 23:38:13**）：

| # | 路径（相对 `Unity/MyGame/`） | HEAD 行数 | 现状 | 观测到的连带错 |
|---|---|---|---|---|
| 1 | `Assets/CardPresentation/Shell/CampaignTab.cs` | 943（工作区原本 **1045**） | **被覆盖** | CS0101/CS0111 + **`CampaignTab` 类型消失** |
| 2 | `Assets/CardPresentation/Shell/ShopData.cs` | 264 | **被覆盖** | 4 处 `CS0246 ShopOffer`（`BoosterInfoPopup` / `ShopWindow`） |
| 3 | `Assets/CardPresentation/Shell/AllianceMemberTab.cs` | 1245 | **被覆盖** | `CS0246 AllianceMemberTab`（`AlliancesTab`） |
| 4 | `Assets/WarpforgeVFX/Runtime/WFModuleTween.cs` | — | **被覆盖** | `CS0234 WFModuleTween`（`Core/UnitTweenRuntime`） |
| 5 | `Assets/WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs` | — | **被覆盖** | `CS0234 WFEffectCardContext`（`Battle/BattleDriver`） |
| 6 | `Assets/WarpforgeVFX/Runtime/WFModuleChangeMaterial.cs` | — | **被覆盖** | 同上（同族重复定义） |
| 7 | `Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` | — | **被覆盖** | 同上 |
| 8 | `Assets/WarpforgeVFX/Runtime/ArenaParticleKeywords.cs` | — | **被覆盖** | 同上 |
| 9 | `Assets/WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` | — | **被覆盖** | 同上 |
| 10 | `Assets/WarpforgeVFX/Runtime/WFModuleTransformModifier.cs` | — | **被覆盖** | （本轮没看到它的连带错，但内容同样是 ArenaByArmy） |
| — | `Assets/WarpforgeArena1/Runtime/ArenaByArmy.cs` | 123 | **原样（`git diff` 零）** | 它就是**被复制的那一份源** |

**证据**：

- `find . -name "*.cs" -newermt "2026-10-05 23:35" -printf "%TH:%TM:%TS %s %p\n"` —— 上面 10 个文件的 mtime 全是 `23:38:13.84x`、size 全是 `9012`；
- `head -1` 每一份都是 `// ArenaByArmy.cs — 「本局打哪个战场」的原版查表（**运行时**，2026-09-25）`；
- `git diff --numstat`：`CampaignTab.cs 109/929` · `AllianceMemberTab.cs 103/1225` · `ShopData.cs 110/251` …（都是「整篇换掉」的形状）；
- **`ArenaByArmy.cs` 自己 `git diff` 为零** ⇒ 它是源、其余是受害者。

**后果（当时）**：`dotnet csc` 编不过（运行时程序集 **60 错**，逐条 trace 回这 10 个文件；`WFCheck.dll` 产不出来 ⇒ 编辑器那一档连编都没编）
⇒ **Unity 批处理那一刻一条自检都跑不了**。

**🆕 本件收工前已被修复（不是我修的）**：`23:43:39 … 23:45:24` 之间，这 10 个文件**逐个被写回真实内容**
（size 从清一色 9012 变回 3.3 KB…104 KB：`ArenaOriginalMaterial.cs 33804` · `ArenaParticleKeywords.cs 9333` ·
`WFModuleChangeMaterial.cs 24920` · `WFModuleTransformModifier.cs 32074` · `WFModuleTween.cs 17397` ·
`WarpforgeEffectBinder.cs 25646` · `WarpforgeShaderMap.cs 40304` · `AllianceMemberTab.cs 104643` ·
`ShopData.cs 17227` · `CampaignTab.cs 77708`）
⇒ 运行时错误数 **60 → 11**（新的 11 条**全是别的写手在飞的文件**，见 §六）。
**📌 给同步点**：① 这 10 个文件**要各自核一下写回来的是哪一版**（`git diff --numstat` 再看一眼 —— 例如
`CampaignTab.cs` 现在是 **1021** 行、而 `23:2x` 时是 **1045** 行 ⇒ **上一次修复可能用的是更早/更晚的一份快照**，
那 24 行要查是不是有意丢的）；② 这一场覆盖的**根因**（谁在 23:38:13 干了什么）还没人给判据 ——
本件只报事实、**不猜**。

**本件没动它们**（铁律 13·3 ②：错误全部集中在**不是本件负责**的文件上 ⇒ 记进报告、⛔ 别当成自己的错去改别人的文件；且 ⛔ 本件不许动 git）。

---

## 一、结论

| 账 | 状态 | 一句话 |
|---|---|---|
| **A448** | ✅ **做了** | `Shell/CampaignRewardWindow.cs`：「关窗三条路」的 **①「领到 ⇒ 关窗」**接上（原先只有 ② ESC / ③ 点暗底）。**关门条件是「这一次真的领到了」**，⛔ 不是「点了就关」、⛔ 也不是「两档全领完才关」。 |
| **A459** | ✅ **做了** | `Shell/InboxWindow.cs` 三行各补 `autoBasePx = 41f`；`Shell/RewardWindow.cs` 标题一处补 `autoBasePx = 36f`。**`max` 一处没动**（原版那几张的 `m_fontSizeMax` 本来就等于我们传的 `fontPx`）。 |
| 类型检查 | ⚠️ **本件三文件 0 错；全局不是 0/0** | 全局 60 错 **全部**来自 §〇 那 10 个被覆盖的文件；**本件三个文件在每一轮都是 0 错的来源**。见 §六。 |

---

## 二、改动清单（文件 : 行号 = 收工时读数）

### A448 —— `Shell/CampaignRewardWindow.cs`

| # | 行 | 改动 |
|---|---|---|
| 1 | `:92`（doc `:82-91`） | `public System.Action<int> OnCollect;` → **`public System.Func<int, bool> OnCollect;`**（`bool` = 本次领取成不成）。doc 写明「原版是 `Action<RewardTier>`、**这是我们的收窄**」以及为什么必须收窄。 |
| 2 | `:681-686`（doc `:650-680`） | `OnUnlock` 换成：`if (_ctx == null \|\| _ctx.OnCollect == null) return;` + **`if (_ctx.OnCollect(tier)) Close();`** —— 那一条 `Close()` 就是原版 `g__Refresh_0` 末尾那一跳。方法头把判据、两条口径、落点差异、关窗次序**逐条写全**。 |

**A448 的判据（本件亲读，全部在 `d:/2/tools/decomp_full/`）**：

1. **窗自己不关** —— `CampaignRewardsWindow*` 家族 **36 个 `.c`**，`grep -c CloseWindow` **逐文件全 0**，
   `grep -l "Close"` **零命中**；`CampaignRewardsWindow__UnlockClicked.c` 只把 tier 透传给 `context+0x18` 那个委托。
2. **关窗在「领取成功回调」那一拍** —— `CampaignWindowTab.<TryCollect>g__Refresh_0`
   （`CampaignWindowTab.__c__DisplayClass24_0___TryCollect_g__Refresh_0.c`，逐句照原文）：
   `CampaignHeaderDisplay.Initialize` → `ArmySelector.Refresh` → **`CampaignNode.Collect(id, tier)`** →
   **`WindowsManager.CloseWindow<CampaignRewardsWindow>()`**。
3. **失败支不关** —— `Everguild.LiveOps.Campaign.__c__DisplayClass11_0___CollectRewards_g__OnFail_11_1.c`
   体里只有 `ErrorPopupWindowController.ShowErrorMessage`，**没有关窗那一跳**。
4. **不是「两档全领完才关」** —— 上面那个回调里**没有任何**「另一档也领了」的判据；
   且 `CampaignNode__Collect.c` 领完把节点从 `10/40` 改写成 `20/30` ⇒ `Claimable` 随之为假
   （我们的 `CampaignData.StateOf` 同形：`Shell/CampaignData.cs:175-183`）⇒ 另一档要**再点一次节点**把窗开回来。
5. **关窗次序** —— 原版 `RewardService.Collect(...)`（`…g__OnSuccess_0.c:27`，`showAnimation = 1`）**先**开领奖窗，
   **再**由 `onCollected` 回调关本窗 ⇒ 本件也是先 `OnCollect`（内含 A438 的 `RewardWindow.ShowCollected`）**再** `Close()`。

> ⚠️ **两处如实标注（⛔ 不是「原版就是这样」）**：
> ① **落点在窗里**：原版那一句在**调用方**（`CampaignTab` 的领取回调）里。本件把门放在窗里的原因是
> **`Shell/CampaignTab.cs` 不在本件白名单**（本件只能碰三扇窗）。可观测行为一致；
> **更贴原版结构**的等价写法 = `CampaignTab.ClaimForTest` 的 `if (…Claim…)` 支里补一句
> `_win.Manager.CloseWindow<CampaignRewardWindow>()`，并把 `OnCollect` 收回 `Action<int>`。
> ② **`Func<int,bool>` 是收窄**：原版 `Action<RewardTier>` 没有返回值，成功信号走服务端回包；
> 我们的领取是**同步**的（`CampaignTab.cs:838`），把同一个信号沿这条回调带回来是最小改法。
> **现有调用点 `Shell/CampaignTab.cs:808` 的 `OnCollect = tier => ClaimForTest(i, tier)` 一个字符都不用改**
> （表达式 lambda 对两种委托都成立 —— ⚠️ 但**该文件当前处于 §〇 的被覆盖状态**，没法在编译里复验，见 §六）。

### A459 —— `Shell/InboxWindow.cs` + `Shell/RewardWindow.cs`

| # | 文件 : 行 | 改动 |
|---|---|---|
| 1 | `InboxWindow.cs:129-141` | 新增常量 `RowTitleAutoBase = 41f, RowDateAutoBase = 41f, RowNewAutoBase = 41f;`（doc 里写全判断命令与三行原文读数） |
| 2 | `InboxWindow.cs:413` | `Title` 行：`…, RowTitleAutoMin, 1, autoBasePx: RowTitleAutoBase);` |
| 3 | `InboxWindow.cs:415` | `Date` 行：`…, RowDateAutoMin, 1, autoBasePx: RowDateAutoBase);` |
| 4 | `InboxWindow.cs:418` | `New` 行：`…, RowNewAutoMin, 2, autoBasePx: RowNewAutoBase);` |
| 5 | `RewardWindow.cs:219-232` | 新增常量 `TitleAutoBase = 36f;`（doc 里写全判断命令 + 两行原文读数 + **自证**那条） |
| 6 | `RewardWindow.cs:1387` | `GlowText()` 里 `…, Glow.W, TitleAutoMin, autoBasePx: TitleAutoBase);` |

**A459 的判据（**本件亲跑**，读数逐字抄自输出）**：

- 命令 A：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Message Container" --depth 8 --md --no-sprite`
  → 三行：`Title`「字号=50.0 **基准=41.0** auto[18.0~50.0]」·`Date`「40.0 **41.0** auto[18.0~40.0]」·
  `New`「40.0 **41.0** auto[18.0~40.0]」（模板正是 `Shell/InboxWindow.cs:79` 认的那个 prefab）。
- 命令 B：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Reward Window" --depth 6 --md --no-sprite`
  → `Content/Title/Glow Get reward/Text Get Reward` 与 `…/Glow Preview reward/Text Preview Reward` **两颗逐字相同**：
  「字号=50.0 **基准=36.0** auto[25.0~50.0] 对齐=Center/Middle 折行=1」。
  🔴 **这两颗就是本文件 `GlowText()` 建的那两颗**（**自证**：dump 里它们的矩形 `660.00,187.38→1260.00,262.38`
  与本类 `Glow` 常量**逐位吻合**）⇒ ⛔ 不是 H22 §⑥·1 那句「同族旁证」。
- 🔴 **`base` 是活值、不是死值（本件在原始 JSON 上再核了一层）**：
  `m_enableAutoSizing = 1` —— `Message Container/Title`（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-2428977095636472911.json`）
  与 `Reward Window` 标题（`…/MonoBehaviour_701106173098715505.json`）**都开着自适应**
  ⇒ `m_fontSizeBase` 正是「二分的起点」（`TextMeshPro.cs:2148-2149`）。
  （`menu_dump.py:607` 的 `auto[…]` 那一列**只在 `m_enableAutoSizing` 为真时才打** ⇒ 表里印了 `auto[` 就说明开着。）
- **`max` 一处没动**：原版 `m_fontSizeMax` = `50 / 40 / 40`（Inbox 三行）与 `50`（RewardWindow 标题），
  而 `MenuDraw.Text` 在 `autoMaxPx <= 0` 时**退回 `fontPx`** ⇒ **本来就等价**（`Shell/MenuDraw.cs:1555`）。

---

## 三、待接线（**断言一律写进本报告** —— `Editor/*` 本件不碰）

### 1) 🔴 A448 · 主断言（宿主 `Editor/RewardsScene.cs`，落点 = 现读 `:4255-4290` 那一节）

**现状（本件现读）**：`:4258` `cTab.ClickNodeForTest(0)` 开出窗 ⇒ `:4262` **直调** `cTab.ClaimForTest(0, TierBasic)`
⇒ **绕过了窗里那颗钮** ⇒ 新行为**一次都不被覆盖**（`:4260` 那句注释「再从窗里领（`Unlock` 钮那条路…）」与实际不符）。

```csharp
// 建议：在 :4258 之后把「直调」换成**真点**（本文件已有 ClickButtonByQuad，static 私有、同文件可用）
//   ⚠️ 需要一个取窗把手（本件不写 Editor，落点由调度台定）：
//      static CampaignRewardWindow FindOpenCampaignRewardWindow()  ⇒ 扫 wm2.openWindows 里 `as CampaignRewardWindow`、取 CurrentState != Closed 那扇
var cwReal = FindOpenCampaignRewardWindow();
CheckTrue(cwReal != null, "（前提）场上那扇 `Campaign Reward Window` 取得到");
var ub  = FindPath(cwReal.transform, "Content/Scroll View/Viewport/Content/Base Rewards/Rewards/Unlock Button/Hit");
var ubW = ub != null ? ub.GetComponent<WindowButton>() : null;
CheckTrue(ClickButtonByQuad(PointerLayer.Instance, ub, ubW, "战役奖励窗的 `Unlock`"), "★ A448：真点那颗 `Unlock`");
CheckTrue(CampaignData.BaseClaimed(0), "…而且**真领到了**（前提没成立的话下一条红的原因就不是 A448）");
Check(cwReal.CurrentState, WindowState.Closed, "★ A448：领到 ⇒ **本窗自动关**（原版 `g__Refresh_0` 末尾那一跳）");
```

| # | 判据（期望值 = 原版立即数 / 现读数） | 改坏法（**能分辨什么**） |
|---|---|---|
| ① | `CurrentState == Closed`（`WindowState.Closed = 0`） | 删掉 `Shell/CampaignRewardWindow.cs:685` 的 `if (_ctx.OnCollect(tier)) Close();` ⇒ 红（**= A448 之前的现状**，正是这条要钉的） |
| ② | 🔴 **负例（分辨力最强的一条）**：造一扇 `ctx.OnCollect = _ => false`（或 `Claimable=false`）的窗 ⇒ 点 ⇒ **`CurrentState` 仍是 `Open`** | 把 `:685` 改成 `_ctx.OnCollect(tier); Close();`（= **点了就关**）⇒ 红。判据 = `…CollectRewards…g__OnFail_11_1.c` 失败支**没有**关窗那一跳 |
| ③ | **「另一档还有奖没领完」那一档**：UM0 的 `ctx0`（`BaseCollected=false && PremiumCollected=false`）真点基础档 ⇒ 窗**照样关** | 把关门条件写成 `if (BaseCollected && PremiumCollected) Close();` ⇒ 红（钉住「不是全领完才关」） |
| ④ | **`Func<int,bool>` 那个信号真的被消费**：`ctx.OnCollect = t => { calls++; return false; }` ⇒ 点一下 ⇒ `calls == 1` **且窗还开着**；换成 `return true` ⇒ 点一下 ⇒ 窗关 | 在 `OnUnlock` 里忽略返回值（`_ctx.OnCollect(tier); Close();`）⇒ 第一轮红；把 `Func` 改回 `Action<int>` ⇒ 编译错 |

⚠️ **这条断言需要动 Editor 夹具的一处前提**：`:4262` 那句直调**要么保留**（它现在验的是 `ClaimForTest` 的返回值，
是对的），**要么**改成真点（那就同时覆盖 A448）。**建议两条并存**：直调那句原样留着、**另起一段**做「真点 + 关窗」。

### 2) 🔴 A448 · **点名会被动到的既有断言**（不是「可能」，是现读逐条核过）

| 断言 | 位置（现读） | 本件结论 |
|---|---|---|
| `CheckTrue(cTab.ClickNodeForTest(0), …)` | `:4258` | **不受影响**（开窗，不发奖） |
| `CheckTrue(cTab.ClaimForTest(0, TierBasic), …)` | `:4262` | **不受影响但不够了**：它直调 ⇒ **不走**新的关窗那一跳；`:4260` 那句「从窗里领」的注释**名不副实** |
| ★ A449 三条（`rwCamp` / `DismissRewardWindows` / `OpenRewardWindowCount`） | `:4277-4283` | **不受影响**（关的是领奖窗；本件关的是战役奖励窗，且那时它已被压到 `Background` ⇒ `NotifyClosed` **不**会调 `ShowPreviousWindow`） |
| `CheckAbsorbRule(…, () => cw.CurrentState)` + `cw.TryOpen(null)` / `cw.Close()` | `:4590-4596` | **不受影响**（点的是吸收层、不是 `Unlock`；那几句本来就是显式关窗） |
| `cw2.Close()` / `cw3.Close()` / `cw4.Close()` | `:4537 / :4554 / :4567` | **不受影响**（显式关） |
| 收件箱那一节 | `:6427-6601` | **不受影响**（A459 只改 `m_fontSizeBase`，不断矩形） |
| `RewardWindow` 那一节 | `:4771-5200` | **不受影响**（A459 只改标题那颗的 base；`CheckNear` 类断言没盯字号） |

> **一句话**：本件**没有一条既有断言会变红**（A448 的新行为只在「真点 `Unlock`」那条路上，而 `Editor/*` 今天一处都不点它）。
> 代价是**新行为零覆盖** ⇒ 上面 §三·1 那三条必须补。

### 3) A459 · 断言（Inbox 落点 = 现读 `Editor/RewardsScene.cs:6570-6576` 那一段，`rowTop` 已经在手）

```csharp
var titleLb = FindChild(rowTop, "Title") != null ? FindChild(rowTop, "Title").GetComponent<Label>() : null;
CheckTrue(titleLb != null, "…`Title` 那颗挂得到 `Label`（下一条要读它的 `m_fontSizeBase`）");
CheckTrue(titleLb == null || titleLb.FontSizeBase > 0f,
          "（前提）走的是 TMP 后端 —— `Label.FontSizeBase` 在点阵后端返回 **−1**（如实报，不是 0）");
CheckNear(Label.FontSizeToPx(titleLb.FontSizeBase), 41f, 0.6f,
          "★ A459：`Title` 的 `m_fontSizeBase` = 原版 **41.0**（px；判据 = `Message Container` prefab 的 `m_fontSizeBase`）");
// `Date` / `New` 同形（41.0 / 41.0）；⚠️ `New` 只在 `Unread` 那一条上存在（`fake[0]` 是唯一 unread）
```

- **为什么这条有牙**：`Label.FontSizeBase`（`Battle/Label.cs:507`）是**反射读的真字段**、`Label.FontSizeToPx`（`:481`）是**唯一那条 px 口径** ⇒ 期望值 `41` 是**原版 prefab 原文**，⛔ 不是读被测实现自己的常量（不构成自证）。
- **改坏法**：把 `Shell/InboxWindow.cs:413` 的 `autoBasePx: RowTitleAutoBase` 删掉 ⇒ base 退回 `fontPx` = **50** ⇒ 红（三行都红）。
- ⛔ **别写**「`Check(RowTitleAutoBase, 41f, …)`」那种（读的是被测实现自己的常量 = 自证）；也⛔ **别只断渲染字号** —— 按 `MenuDraw.Text` 头注释，base 只改**二分起点**、终点两侧收敛 ⇒ 渲染差 ≤ 0.05 fontSize 单位 ⇒ **量渲染是弱断言**。
- `RewardWindow` 那处同理，落点建议 = 现读 `:4777` 那一带的标题断言之后：
  `FindPath(rw.transform, "Content/Title/Glow Get reward/Text Get Reward").GetComponent<Label>()` ⇒
  `CheckNear(Label.FontSizeToPx(lb.FontSizeBase), 36f, 0.6f, "★ A459：…原版 36.0")`；
  **改坏法** = 删 `Shell/RewardWindow.cs:1387` 的 `autoBasePx: TitleAutoBase` ⇒ base 退回 50 ⇒ 红。

---

## 四、没查清（⛔ 一律不猜）

1. ⚠️ **`CampaignWindowTab.<OnNodeClicked>b__1` 那一支的方法体本地没有**：`OnNodeClicked` 的 lambda `b__0`
   把 `Campaign.CollectRewards(…, onCollect)` 的回调登记成 `…DisplayClass23_0` 上的一个方法
   （token `DAT_1842a8600`），而 `decomp_full` 里**只有 `b__0`**（`<TryCollect>` 那一支的 `g__Refresh_0` 有）。
   ⇒ **本件关窗的判据取自 `g__Refresh_0`**（两支同形：都是「刷新 → `CampaignNode.Collect` → `CloseWindow`」）。
   ⛔ 我**没有**拿到 `b__1` 的逐句原文；要补的话得拿 `DAT_1842a8600` 去 Il2Cpp metadata 里查方法名再定位 VA。
   **但**「领取成功 ⇒ 关窗」这条结论**不依赖它**：正本 §十四:646、`g__Refresh_0`、以及「`CampaignRewardsWindow` 家族零关窗调用」三处互相独立地指向同一结论。
2. ⚠️ **`OnUnlock` 的落点差异**（窗里 vs 调用方）已如实标注，**没有**在真机/自检里验过「两条路的可观测行为完全一致」
   —— 本件按代码路径推演（`WindowsManager.NotifyClosed` 的 `wasTop` 那一支、`OpenWindow` 的 `ToBackground` 不改 activeSelf 逐条读过）。
3. ⚠️ **A439（商店）那条路上 `ClaimForTest` 之外的调用方**：`OnCollect` 现在多一个返回值语义，
   **本件只核了** `Shell/CampaignTab.cs:808` 与 `Editor/RewardsScene.cs` 里**没有**第二处赋值（全仓 `grep -rn "OnCollect"` 逐条读过）
   —— 但 `CampaignTab.cs` 当前被覆盖，**没法复验**。
4. ⚠️ **`_ctx.OnCollect(tier)` 抛异常**（比如领取里 `ShowCollected` 出错）时窗**不会关**（`Close()` 在那句之后）；
   原版那一支是服务端回调、同样不会关 —— **形状一致，但没实跑**。

---

## 五、顺手发现（⛔ 一个都没在本件里改）

1. 🔴 **`BuildUnlockButton` 的 `clickable` 少了 `ctx.Claimable` 这一项**（真偏离，本件范围外）：
   - **原版**（本件亲读 `CampaignRewardsWindow__SetBaseButton.c`）：
     `ctx.PointCost (0x30) < 1` 那一支 ⇒ **`SetAsFreeClaim(btn, **1**, 0)`（`interactable` 硬编码真）**；
     `else` 那一支 ⇒ `ToggleTexts(true)` + **`Selectable.set_interactable(*(btn+0x38), ctx.Claimable(0x2a))`**。
   - **我们**（`Shell/CampaignRewardWindow.cs:632`，行号已随本件后移）：
     `bool clickable = !premiumLocked && !claimedState;` ⇒ **「付点解锁」那一支恒定可点**。
   - 症状：`Claimable == false`（点数不够 / 节点状态是 20/30）时，原版那颗钮**不可点**，我们**可点**；
     点下去 `ClaimForTest` 返回 false 并打日志（**不是静默**，所以后果有限）。**建议另立一条**。
2. 🔴 **`InboxWindow` 那三行的折行模式是反的**（同一批实读里撞见）：
   原版三颗 `m_TextWrappingMode = **0**`（NoWrap，本件在 `MonoBehaviour_-2428977095636472911.json` 上直接读到 `0`），
   而 `Label.SetAutoFitBox` 内部会调 `SetWrapWidth`、那个**无条件把模式设成 `Normal`**
   （`Battle/Label.cs:355-358` 自己写着这条）⇒ 我们这三行**被悄悄打开了折行**（= A34-F4 那一族）。
   修复面：`GameWindow.Text` 之后补 `SetWrapping(false)`（`Label.cs:363`），**要在对齐/裁切之前**。
   ⛔ 本件没改（不在 A459 的范围里；且那是**另一条账**）。
   ⚠️ `RewardWindow` 标题那颗原版就是 `折行=1`，**不受影响**。
3. 🔴 **`InboxWindow` 另外三颗（不是行模板）根本没接自适应**（顺手发现，⚠️ 只查了「有没有」，**没逐颗核完**）：
   `Shell/InboxWindow.cs:220`（`Title` 48f）· `:236`（`Message Display/Title` 58f）· `:240`（`No News Warning` 50f）
   都是 `MenuDraw.Text(..., fontPx, QText)` **不传 `autoMinPx`** ⇒ 一个自适应都没有；
   而 `menu_dump` 里原版这三颗**都印着 `auto[…]`**（= `m_enableAutoSizing` 为真）：`auto[18~48]` · `auto[10~58]` · `auto[12~50]`，base 都是 **36.0**。
   ⇒ 这是**同一族、更大的一笔**（不是「只差 base」）。**没查**：我**没核**这三颗与代码里那三处的**逐颗对应**（只对上了名字）。
4. 🔴 **`CampaignTab.cs:844-845` 的注释现在半错**（该文件不在本件白名单，且当前处于 §〇 的覆盖状态）：
   原话「领完就把窗刷新成 `Get` 态（… 而 `CampaignRewardsWindow` 是「领一次就 `CloseWindow`」——见正本 §十四「关窗三条路」）」
   —— 前半句描述的是**窗留着**时的样子，而 A448 之后窗**直接关**（`Get` 态要**重开**才看得到）。建议顺手改成一句。
5. ℹ️ **`CampaignTab.BuildContext` 的 `Army = 10` 是写死的**（`Shell/CampaignTab.cs:807`）：原版那一格来自
   `Campaign` 实例（`CampaignRewardsWindowContext` 的 `0x2c` = 当前战役的阵营）。**只报**，本件没查它今天会不会画错。
6. ℹ️ **H22 §⑥·1 的「§5」指路在正文里对不上**（文档形状问题）：那一段落在 `## ⑥ 顺手发现` 的第 1 条，
   派单里写的是「H22 §5」⇒ 下一个会话按「§5」去找会落在 `## ⑤ 没查清`。**建议调度台把指路改成 `§⑥·1`**。

---

## 六、类型检查（`TMPDIR=/tmp/wf_h26 bash d:/4/Unity/工具/typecheck.sh`）

- **本件三个文件：0 错**（四轮复跑都一样；**没有一条**错误落在 `CampaignRewardWindow.cs` / `InboxWindow.cs` /
  `RewardWindow.cs` 上 —— 逐条 `grep` 过）。
- 🔴 **A448 的签名收窄已在【真调用点】上复验过**：`Shell/CampaignTab.cs` 在 `23:45` 被写回真实内容之后，
  `CampaignTab.cs:808` 的 `OnCollect = tier => ClaimForTest(i, tier),` **编译零错**
  ⇒ `Action<int>` → `Func<int, bool>` 这一改**不需要动 `CampaignTab.cs` 一个字符**（本报告的 §二 断言成立）。
- **全局仍不是 0/0**，但**与 §〇 那一场覆盖无关了**（那次是 **60** 错，现在是 **11** 错、且**全是另一种错**）。
  现读的 11 条**逐条落在别人的文件上**（本件收工时的读数）：

| 文件 | 条数 | 错误 | 说明 |
|---|---|---|---|
| `Shell/AllianceMemberTab.cs` | **10** | `CS7036`：`SocialView.Text(Transform, PxRect, string, Color, string, float, int, float, bool, bool, float, float)` 少一个 `alignLeft` | `:299/363/367/729/896/965/973/1203/1221/1223` —— `SocialView.Text` 的**签名与调用点对不上**（有写手正改这一支） |
| `Shell/ShopWindow.cs` | **1** | `CS0117`：`ShopData` 里没有 `GrantsOf` | `:782` —— `ShopData.cs` 刚被写回（17227 B）⇒ 与 `ShopWindow.cs` 期望的那一版**对不上**（同 §〇 那一步修复的余波） |

- **判据（铁律 13·3 ②）**：错误**全部集中在不是本件负责的文件**上 ⇒ **不是本件的问题**，⛔ **本件没去碰它们**。
  ⚠️ **别拿「11」当本件红**；等那两条别人的活收口，正常应当是 **0/0**。
- 编辑器程序集仍然连**编都没编**（`CS0006 找不到 WFCheck.dll`）—— 因为运行时那一步**还没产出 DLL**。
  本件**没动任何新类型/新文件**（只改了三处表达式与一个委托类型），所以运行时一绿、编辑器那档的结论照旧。

## 七、📌 同步点该跑哪几条（请调度台按这个跑）

**前提**：§〇 的 10 个文件**必须先在 git 侧还原**，否则 Unity 批处理连编译都过不去（一条都跑不了）。

| 建议 | 条目 | 为什么它在名单里 |
|---|---|---|
| **必跑** | `RewardsScene.Run` | **本件三个文件里两个的宿主**：`CampaignRewardWindow`（`:4397-4740` 整节）· `InboxWindow`（`:6427-6601`）· `RewardWindow`（`:4771-5200`） |
| **必跑** | `ShopScene.Run` | `RewardWindowFixture` 三条（A439）在它里面（`:1540-1555`）；本件改了 `RewardWindow` 的标题那一颗 |
| **建议** | `MainMenuScene.Run` | `InboxWindow` 的**另一个**宿主（`Editor/MainMenuScene.cs` 引用它） |

- ⛔ **不是全套**：本件**没动** `Shell/WindowsManager.cs` / `MenuDraw.cs` / `MenuWindowBase.cs` / `Editor/*`，
  也**没动** `RuleEngine/`、`Battle/*`、`Net/*` ⇒ 按铁律 12 判据①（只动一个自检宿主就只跑那一条）+「共用件先 grep」
  ⇒ **三条足够**（`RewardWindow` 是共用件，所以要**跑遍它的宿主**：`RewardsScene` + `ShopScene`；
  `InboxWindow` 也是共用件 ⇒ 再加它的另一个宿主 `MainMenuScene`）。
- ⚠️ **预期红**：**本件没有已知的必红项**（§三·2 逐条核过，「本件没有一条既有断言会变红」）。
  若真看到红，**先看是不是 §〇 那 10 个文件没还原干净**（那种红会集中在 `CampaignTab` / `ShopData` / VFX 一族）。
- ⚠️ 判绿红看**断言合计**、不看退出码；⛔ 别用 `grep -c ✗` 数失败（`✗` 会出现在断言文案里）。

## 八、红线核对

⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用了只读的 `git status` / `git diff` / `git show`）
· ⛔ 没改 `CLAUDE.md` / `项目任务.md` / 任何正本 · ⛔ 没越白名单（写只落在三个 `.cs` 上；`Editor/*`、`Shell/CampaignTab.cs`、`Shell/WindowsManager.cs` **只读**）
· ✅ 改完**立刻**跑了类型检查（`TMPDIR=/tmp/wf_h26`，独立目录；三轮）
· ✅ 行尾复核（二进制读）：`CampaignRewardWindow.cs` **CRLF 0 / LF 809** · `InboxWindow.cs` **CRLF 0 / LF 440** ·
  `RewardWindow.cs` **CRLF 0 / LF 1416** ⇒ 三份**一行都没翻**；`git diff --numstat` = `114/20` · `19/3` · `383/67`
  —— ⚠️ **这三个数不等于本件的改动量**：`InboxWindow` 那份（`19/3`）**全是本件加的**；
  `CampaignRewardWindow` 含 **E1/H22 早先未提交**的改动、`RewardWindow` 的 `383/67` **绝大部分**是**别的写手早先未提交**的改动
  （hunk 一大片：`@@ -316,0 +346,234 @@` 那种）⇒ **本件只加了 `InboxWindow` 19 行 / `RewardWindow` 约 21 行**（两处 hunk：`@@ -217,0 +218,15 @@` 与 `GlowText` 那一处）+ `CampaignRewardWindow` 的 2 行代码 + 两段 doc
· ✅ 没有把「查不到」写成猜测：§四 的没查清部分**逐条写明还差什么**；§五·3 明写「没核完」
· ⚠️ **一处如实标注**：`OnCollect` 的 `Func<int,bool>` 是**我们的收窄**、`OnUnlock` 的落点在窗里是**我们的选择**
  —— 两处都在代码注释里写了「⛔ 不是原版就是这样」。
