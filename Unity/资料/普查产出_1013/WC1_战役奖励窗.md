# WC1 · 战役奖励窗（A472 高级档门 + A402 锁定支两条）

> 写手 **WC1**（第四轮「清空 A 表」· 波 4）· 账 = **A472 + A402**（都在 `Shell/CampaignRewardWindow.cs`）
> 白名单：`Shell/CampaignRewardWindow.cs`（**已改**）· 本报告
> 🔴 **断言没落进 `Editor/RewardsScene.cs`** —— 理由与「可直接粘贴的成品」见 §五·0 / §五·2。
> 口径：判据一律取**原版权威**（`d:/2/tools/decomp_full/` 方法体 + `d:/2/tools/il2cpp_out/dump.cs` 字段偏移 +
> `d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 的 prefab 原文 + uGUI 包源码），**本件全部亲读**；
> ⛔ 没跑 Unity、没动 git、没碰白名单外的文件。

---

## 一、结论

| 账 | 状态 | 一句话 |
| --- | --- | --- |
| **A402 ①**（`Warning` 显示） | ✅ **代码已改** | `Shell/CampaignRewardWindow.cs:608` 由**无条件 `SetActive(false)`** 改成 `SetActive(lockedHere)` |
| **A402 ②**（整颗高级 `Unlock Button` 隐藏） | ✅ **代码已改** | `:587` `if (lockedHere) btn.gameObject.SetActive(false);`（**不是变灰**） |
| **A402 布局表** | ✅ **已改（按简报给的形状）** | `:504/:507` 锁定支 = `items + WarnW + BadgeSize`（**不含 `UnlockW`**）· 槽号 `:567-568` |
| **A472**（基础档领了才点得动） | ✅ **代码已改** | `:726-727` `isBase` 分叉：高级列 = `ctx.BaseCollected`（原版 `SetPremiumButton.c:40`） |
| **两笔的断言** | 🔴 **未落盘**（宿主归别人） | §五·0；**成品块在 §五·2**，粘贴即可用（标识符已逐个查过不撞名） |
| 🔴 **顺手查出的一笔更大的偏离** | ⚠️ **已记录、⛔ 未改** | 原版那三颗（`Unlock Button` / `Warning` / `Badge`）**全都带 `m_IgnoreLayout = 1`** ⇒ 它们**根本不参与** holder 的 `HorizontalLayoutGroup`，**一张宽度表都不该收它们**；三颗件的真实落点是**锚点驱动**（按钮/警告 = 列**底部居中**、徽标 = 列**左上角**）。详见 **§七·1**（⚠️ 它同时**推翻 A239 那套模型的前提**，而 A239 的断言还没写 ⇒ 见 §七·1 末的调度提示） |

- **类型检查**：`TMPDIR=/tmp/wf_wc1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**（§八）。
- **改动量**：`Shell/CampaignRewardWindow.cs` 一个文件，`git diff --numstat` = **65 / 8**（行尾未翻：全文 LF，0 个 CRLF）。

---

## 二、A472 改动清单（含「原版那条张力的解释」）

### 2.1 改了哪一行

**`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/CampaignRewardWindow.cs:726-727`**（在 `BuildUnlockButton` 里）：

```csharp
bool clickable = !premiumLocked && !claimedState
                 && (isBase ? (ctx.PointCost < 1 || ctx.Claimable) : ctx.BaseCollected);
```csharp

改之前（**A466 2026-10-12 落的**，本件**没有推翻它、只把它限定在基础列**）：

```csharp
bool clickable = !premiumLocked && !claimedState && (ctx.PointCost < 1 || ctx.Claimable);
```

- **基础列**：**逐位保留** A466 那套（`SetBaseButton.c` 三条支：已领 ⇒ 假 · `PointCost < 1` ⇒ **硬编码真** · else ⇒ `ctx.Claimable`）。
- **高级列**：改成原版 `SetPremiumButton.c` 的唯一一支 —— `BaseCollected`。

### 2.2 判据（第一权威 = 反编译方法体，逐句亲读）

`d:/2/tools/decomp_full/CampaignRewardsWindow__SetPremiumButton.c`（**现读全文**，三支）：

| 支 | 行 | 做了什么 |
| --- | --- | --- |
| 已领（`ctx+0x29` `PremiumCollected`） | `:120` `:124` `:126` | `SetActive(warning, 0)` · `SetActive(btn, 1)` · `SetAsClaimed(btn)` |
| **锁定**（`ctx+0x10` `IsPremiumLocked`） | `:81` `:85` `:97-107` | `SetActive(warning, **1**)` · `SetActive(btn, **0**)` · `set_text(warning, premiumUnlockWarning)` |
| **不锁 ∧ 未领**（else） | `:36` `:40` | `SetActive(btn, 1)` · **`SetAsFreeClaim(btn, *(ctx + 0x28))`** |

- `0x28` = **`BaseCollected`**（`dump.cs:67543` `private bool <BaseCollected>k__BackingField; // 0x28`；
  getter = `CampaignRewardsWindowContext__get_BaseCollected.c`）。
- `d:/2/tools/decomp_full/CampaignUnlockButton__SetAsFreeClaim.c:13-15`：
  `ToggleTexts(btn, 0)` → **`UnityEngine_UI_Selectable__set_interactable(*(longlong *)(param_1 + 0x38), param_2, 0)`**
  ⇒ **第 2 个形参就是 `interactable`**。
- 🔴 **那一支里 `ctx+0x30`(PointCost) 与 `ctx+0x2a`(Claimable) 一次都没出现** —— 本件把 `SetPremiumButton.c`
  全文逐字段核过，出现的偏移只有 `0x29` / `0x10` / `0x28` / `0x88` / `0x90` / `0x98` / `0xb0–0xc0`
  ⇒ **高级列的门只看 `BaseCollected`**。⛔ 别把 A466 那套（基础列的 `PointCost`/`Claimable`）搬过去
  —— 那两列的门**本来就不同**（基础列 `SetBaseButton.c:40` 甚至是**硬编码 `1`**），这是原版的区别、不是笔误。

### 2.3 「原版那条张力」的解释（⛔ 别顺手改 `CampaignData.StateOf`）

A 表原来挂着「做之前要先裁：这条门与 `CampaignNode.Collect` 的 `state ∈ {10,40}` 打架，本地判据不足」。
**本件把它读完了，它不是建模错，是原版自己的行为**：

1. `CampaignWindowTab.__c__DisplayClass24_0___TryCollect_g__Refresh_0.c:26` = `CampaignNode__Collect(node, tier)`
   + `CloseWindow<CampaignRewardsWindow>()`；
2. `d:/2/tools/decomp_full/CampaignNode__Collect.c:29-33` 的闸 **只放行 `state ∈ {10, 40}`**
   （`10` = `Unlocked` · `40` = `Repeatable`），写 `20`（基础档）或 `30`（高级档）；
3. ⇒ 非 `Repeatable` 节点**领完基础档后 state = 20** ⇒ 再点高级档 **`Collect` 进不去**（原版那一支只打一条
   `Debug.Log`）；全战役 **47 节点里只有 UM47 是 `IsRepeatable`**（`资料/阶段二_锻造厂与战役页_原版规格.md:452`）
   ⇒ **「钮可点、但节点样式不动」是原版对其中 46 格的既有行为**。
4. **我们这一侧同形**（现读 `Shell/CampaignData.cs:183` `:194-215`）：`Claim` 头一句就是 `if (!Claimable(i))`，
   而 `Claimable(i)` = `StateOf(i) ∈ {Unlocked, Repeatable}` ⇒ 领完基础档再点高级档会
   **出声**（`"[Campaign] 节点 X 领不了：这一格的基础档已经领过了"`）并返回 `false`
   ⇒ `OnUnlock` 里那句 `if (_ctx.OnCollect(tier)) Close();` **不关窗** ⇒ 与原版那一拍等价（**不静默**）。

⇒ **本件没有动 `CampaignData.StateOf` / `Claim` 的建模**（那会把原版行为改掉）。

### 2.4 影响面（如实说）

- **真数据下**：`Shell/CampaignTab.cs:807` 的 `PointCost = CampaignData.At(i).Cost` 恒 ∈ **100…1100**
  ⇒ 走「付点」那一支。
  - 改前：`Claimable == false`（= 已领过 / 没解锁）时两列都不点得动。
  - 改后：**高级列只看 `BaseCollected`** ⇒ **领了基础档之后，高级那颗钮变成可点**（原版语义），
    点了会走 2.3 那条失败出声（因为 `Claimable` 已假）—— 这正是原版的样子。
- **自检夹具**：`CampaignData.RewardsOf(0)`（UM0）有 1 基础 + 1 高级 ⇒ 上述两态都能摆出来（§五·2 的夹具 C/D）。

---

## 三、A402 改动清单（两条 + 布局表怎么改）

### 3.1 判据（`SetPremiumButton.c` 三支的 `SetActive` 逐句）

| 件 | 锁支 | 不锁支 | 已领支 |
| --- | --- | --- | --- |
| `premiumWarning`（`0x90`） | `:81` **`SetActive(…, 1)`** | `:32` `SetActive(…, 0)` | `:120` `SetActive(…, 0)` |
| `premiumUnlockButton`（`0x88`） | `:85` **`SetActive(…, 0)`** | `:36` `SetActive(…, 1)` | `:124` `SetActive(…, 1)` |

字段偏移 = `d:/2/tools/il2cpp_out/dump.cs:67461/67463/67465`
（`premiumUnlockButton // 0x88` · `premiumWarning // 0x90` · `premiumUnlockWarning // 0x98`）
—— 与 `SetPremiumButton.c` 里的 `param_1 + 0x88 / 0x90 / 0x98` **逐位对上**。
`Warning` 的文案：锁支里 `local_28 = *(param_1 + 0x98)` ⇒ `premiumUnlockWarning`
（我们建它时写的 `TxtPremWarning` 就是这条词条的占位，见 `:223-226` 的注释）⇒ **文案不用改**。

### 3.2 改动 ①：`Warning` 从「恒关」改成「锁支才开」

**`Shell/CampaignRewardWindow.cs:608`**：`_warn.gameObject.SetActive(false);` → `_warn.gameObject.SetActive(lockedHere);`

> 改之前它是**无条件** `SetActive(false)`，而全仓**没有第二处**开它（`grep -rn "_warn" Shell/CampaignRewardWindow.cs`
> 只有声明 / 建 / 那一次关）⇒ 锁定支下 `Warning` **永远不显示**，而原版那一支正是**唯一**显示它的地方。

### 3.3 改动 ②：锁定支「隐藏」而不是「变灰」

**`Shell/CampaignRewardWindow.cs:587`**（新增，紧跟 `BuildUnlockButton` 之后）：

```csharp
if (lockedHere) btn.gameObject.SetActive(false);
```

🔴 **这一句【不是】`wb.Interactable = clickable` 的等价物，两句都必须留着**：

- 本句管「**不在**」（原版 `SetActive(premiumUnlockButton.gameObject, 0)`）；
- `BuildUnlockButton` 里那句 `wb.Interactable = clickable`（`:739`）管「**在但点不动**」
  （= 原版**另外两支**里的 `set_interactable`）。
- ✅ 顺带正确：`Shell/PointerLayer.cs:488` 的 `CollectHits` 只挑 `WindowButton.isActiveAndEnabled`
  ⇒ 藏起来之后这一颗**不再吃点击**（= 原版 INACT 子件不进射线）。

### 3.4 改动 ③：布局表 + 槽号（`lockedHere = !isBase && PremiumLocked`）

- `:495-501` 新增 `bool lockedHere = !isBase && PremiumLocked;`（**只对高级列成立**：
  `IsPremiumLocked` 只在 `SetPremiumButton` 里被读，基础列走 `SetBaseButton`）。
- `:504` `if (!lockedHere) widths.Add(UnlockW);` —— **原来是无条件加**。
- `:507` `if (lockedHere) widths.Add(WarnW);`（原来是 `if (PremiumLocked)`，同义改写、只换成同一个局部量）。
- `:567-568` 槽号：
  - `int iWarn  = lockedHere ? iBtn : iBtn + 1;` ← **锁定支警告顶到按钮那一格**
  - `int iBadge = iBtn + 1;` ← 两种情况下都是它（不锁时徽标顶掉「警告那一格」）。

**两套表的逐格对照（1 件物品的高级列）**：

| 支 | `ws` | holder 内容宽 | 警告槽 | 徽标槽 |
| --- | --- | --- | --- | --- |
| 不锁（改前改后**不变**） | `[200, 245, 100]` | 30+30+545+50 = **655** | （警告 INACT，不占格） | `1+1 = 2` → 1550..1650 |
| 锁定（**改后**） | `[200, 300, 100]` | 30+30+600+50 = **710** | `1` → 1280..1580 | `2` → 1605..1705 |
| 锁定（改前·**错**） | `[200, 245, 300, 100]` | **980**（右边缘 2005 **出屏**） | `2` → 1550..1850 | `3` → 1875..1975（**出屏**） |

---

## 四、判据出处逐条（原版 `文件:行` ⇒ 我们 `文件:行`）

| # | 原版判据（亲读） | 我们落地处 | 说明 |
| --- | --- | --- | --- |
| 1 | `decomp_full/CampaignRewardsWindow__SetPremiumButton.c:40`（不锁∧未领支末句 `SetAsFreeClaim(btn, *(ctx+0x28))`）· `:22-40` 全文 | `Shell/CampaignRewardWindow.cs:707-727` | A472 高级列 `clickable = ctx.BaseCollected` |
| 2 | `decomp_full/CampaignUnlockButton__SetAsFreeClaim.c:15`（第 2 参 ⇒ `set_interactable`） | `Shell/CampaignRewardWindow.cs:726` / `:739` | 语义 = `interactable` |
| 3 | `il2cpp_out/dump.cs:67543`（`BaseCollected // 0x28`）· `:67537/:67545/:67547/:67551` | 同 | 偏移逐位对上 |
| 4 | `decomp_full/CampaignRewardsWindow__SetBaseButton.c:28-31`（已领 ⇒ `SetAsClaimed`）· `:38-40`（免费支硬编码 `1`）· `:78-90`（付点支 `:84` = `set_interactable(…, *(ctx+0x2a))`） | `:726`（`isBase ? …` 那一半，**A466 原样保留**） | 基础列不动 |
| 5 | `SetPremiumButton.c:81`（锁支 `SetActive(premiumWarning, 1)`） | `:602-608` | A402 ① |
| 6 | `SetPremiumButton.c:85`（锁支 `SetActive(premiumUnlockButton, 0)`） | `:579-587` | A402 ② |
| 7 | `SetPremiumButton.c:32` / `:120`（另两支都关警告） | 同上（`lockedHere` 为假时） | 两态都对 |
| 8 | `SetPremiumButton.c:97-107`（锁支 `set_text` = `premiumUnlockWarning` `0x98`） | `:600-601`（`TxtPremWarning` 占位） | 文案 |
| 9 | `il2cpp_out/dump.cs:67461/:67463/:67465`（`0x88` / `0x90` / `0x98`） | 同上 | 字段归属 |
| 10 | `decomp_full/CampaignNode__Collect.c:29-33`（闸只放行 `state ∈ {10,40}`） | 未改（**只解释**，见 §2.3） | 张力 |
| 11 | `decomp_full/CampaignRewardsWindow__Open.c:112-113`（**先 `SetBaseButton` 再 `SetPremiumButton`**，其余次序照旧） | `LayoutColumns`（未动） | 本件没改次序 |
| 12 | prefab 原文：两列 `Rewards` 的 `HorizontalLayoutGroup`（pad 30/30/25/85 · spacing 25 · `childControlWidth=0` · `childControlHeight=1` · alignment 4） | `:151-154`（常量）· `:513`（`HorizontalContentW`） | **⚠️ 这一条的解读本件查出疑点 ⇒ §七·1** |

---

## 五、断言清单

### 五·0 🔴 为什么我没落盘（要调度台裁一下）

- 宿主**只有一处**：`grep -rn "CampaignRewardWindow" Editor/*.cs` ⇒ **41 处全在 `Editor/RewardsScene.cs`**（§三·d 那一节）。
- 但那份文件**在批次计划里是别人的独占文件**：
  `资料/普查产出_1013/批次计划_1013.md:99` —— **W-E3** = `Editor/RewardsScene.cs`（A239 / A353 / A390 / A512 那半），
  同文件 `:135` 还写着「A469 剩余 …… **须独占 `Editor/RewardsScene.cs`**」。
- 我的简报那条白名单写的是：「断言所在的 `Editor/*Scene.cs`（**只在你确定宿主、且那个文件不是别人的活时**；
  拿不准就只报不改）」⇒ **条件不成立**（那文件明确是 W-E3 的活），所以**只报不改**。
- 🔴 **旁证（本件开工期间实测）**：`git status` 里已经出现了**同批 波 6 的** `Editor/MainMenuScene.cs`（W-E1）·
  `Deck/DeckRuntime.cs`+`Editor/DeckScene.cs`（W-D1）等 ⇒ **后续波次正在并行**，那文件**随时**可能被 W-E3 拿走
  （本件写完时它还是 `01:30` 的旧 mtime、未进 `git status`）⇒ 更不该由我去写它。
- ✅ 落盘成本已被我压到最低：**§五·2 是可直接粘贴的成品块**（插在 **A466 那两块之后**，即现 `Editor/RewardsScene.cs:4632` 的
  那个 `}` 与 `:4634` 的 `// ====== 🆕 2026-10-13（A496 …）` 之间），用到的标识符**逐个 grep 过全文件、零冲突**。
- 📌 **建议**：要么由调度台直接粘（一眼可核），要么把 §五·2 整块并进 W-E3 的简报（那文件本来就在它手上）。

### 五·1 要断什么 · 期望值来源 · 改坏法

| # | 断什么 | 期望值来源（**原版读数，不是我们的常量**） | 改坏法（怎么让它红） |
| --- | --- | --- | --- |
| ① | **A402①** 锁定态 ⇒ `Warning` **开着** + 文案 = `TxtPremWarning` | `SetPremiumButton.c:81` / `:97-107` | `Shell/CampaignRewardWindow.cs:608` 改回 `SetActive(false)` |
| ② | **A402②** 锁定态 ⇒ `Unlock Button` 节点**在**、`activeSelf == false` | `SetPremiumButton.c:85` | 删掉 `:587` 那句 `SetActive(false)`（**只留 `Interactable = false`** ⇒ ②红、①绿） |
| ②b | **弱断言（故意留着当对照）**：同一颗的 `wb.Interactable == false` | `CampaignUnlockButton__SetAsFreeClaim.c:15` | —— ⚠️ **它单独什么都证明不了**（「隐藏」与「变灰」两种世界里都是真）⇒ 必须与 ② 并存 |
| ③ | **A402 布局表** 锁定支 holder 内容宽 = **710**（1 物品）**不含 `UnlockW`** | 原版子件集（物品 + 警告 + 徽标）逐格累加：30+200+25+**300**+25+100+30 | `:504` 改回无条件 `widths.Add(UnlockW)` ⇒ 980 ⇒ 红 |
| ③b | **量渲染真值**：`Badge` 渲染中心 x = **1655** | 同一张表（1025+30+225+325+50） | 同 ③（错表下它会落到 **1925 = 出屏**）⇒ 红 |
| ③c | 基础列**不受**影响：`BaseHolderRect.W == baseW`（530） | `IsPremiumLocked` 只在 `SetPremiumButton` 里被读 | 把 `lockedHere` 的 `!isBase` 去掉 ⇒ 红 |
| ④ | **对照态**（不锁 · 不可点）⇒ 那颗钮**在**且 `Interactable == false` 且 `GrayedForTest == true`、`Warning` **关着** | `SetPremiumButton.c:32` / `:36` + `SetBaseButton.c:84` | —— 与 ②③ 合起来才把「**隐藏**」与「**变灰**」分成两种状态（弱断言分不出） |
| ⑤ | **A472 反例**：`BaseCollected = false` ⇒ 高级钮 **点不动**（同一份 context 里基础钮**可点**） | `SetPremiumButton.c:40` ⇒ `SetAsFreeClaim.c:15` | 高级列改回 A466 那套（`PointCost<1 \|\| Claimable`）⇒ ⑤红 |
| ⑥ | **A472 正例**：`BaseCollected = true`（`Claimable = false`）⇒ 高级钮 **可点**，而基础钮**不可点** | 同上 + `SetBaseButton.c:28-31` | 同上（A466 那套在这里给的是假） |

### 五·2 可直接粘贴的断言块（**成品**，插在现 `Editor/RewardsScene.cs:4632` 的 `}` 之后）

```csharp
            // ============================================================ 🆕 **2026-10-13（A402 + A472 · WC1 出稿）**
            // 两笔账：**A402**（锁定支 = 显 `Warning` + **整颗高级 `Unlock Button` 隐藏**，+ 布局表）·
            //         **A472**（高级列 `interactable = ctx.BaseCollected`，「基础档领了才点得动」）。
            // 🔴 **期望值全是原版读数**（第一权威 = 反编译方法体）：
            //   `CampaignRewardsWindow__SetPremiumButton.c:81`（锁支 `SetActive(premiumWarning, 1)`）·
            //   `:85`（锁支 `SetActive(premiumUnlockButton, 0)`）·
            //   `:40`（不锁∧未领支 `SetAsFreeClaim(btn, *(ctx + 0x28))`，`0x28` = `BaseCollected`）·
            //   `CampaignUnlockButton__SetAsFreeClaim.c:15`（第 2 参直落 `set_interactable`）。
            // 🔴 **怎么自造锁定态**：真数据下 `IsPremiumLocked` 恒 `false`（用户 2026-09-22 裁决 ·
            //   `Shell/CampaignTab.cs:805` 写死）⇒ **只有夹具到得了那一支** —— 这正是它必须有断言的理由。
            {
                // ---- 夹具 A：**锁定态** ⇒ ①警告显示 ②按钮整个不在 ③布局表不含 UnlockW ----
                var cwA402 = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwA402, new CampaignRewardsContext
                {
                    Rewards = CampaignData.RewardsOf(0), BaseCollected = false, PremiumCollected = false,
                    Claimable = true, IsPremiumLocked = true, PointCost = 100, Army = 10,
                });
                var pLk = FindPath(cwA402.transform, "Content/Scroll View/Viewport/Content/Premium Rewards/Rewards");

                // ① `Warning`（原版 `SetPremiumButton.c:81`）
                var warnLk = FindPath(pLk, "Warning");
                CheckTrue(warnLk != null && warnLk.gameObject.activeSelf,
                          "★★ A402①：锁定态 ⇒ `Warning` **开着**（原版 `SetPremiumButton.c:81` `SetActive(premiumWarning, 1)`）"
                          + " —— 把 `Shell/CampaignRewardWindow.cs` 那句 `_warn.gameObject.SetActive(lockedHere)` 改回"
                          + " `SetActive(false)`（= 本件之前的写法）⇒ 红");
                CheckTrue(warnLk != null && TextOf(warnLk) == CampaignRewardWindow.TxtPremWarning,
                          $"★ A402①：警告写的是 `premiumUnlockWarning` 那条词条的占位"
                          + $"（`{CampaignRewardWindow.TxtPremWarning}`；原版 `SetPremiumButton.c:97-107` 锁支才 `set_text`）");

                // ② 整颗按钮【不在】—— 强断言：`activeSelf`（**弱断言分不出「隐藏 vs 变灰」**）
                var btnLk = FindPath(pLk, "Unlock Button");
                CheckTrue(btnLk != null,
                          "★ A402②：（前提）锁定态下 `Unlock Button` 的**节点还在**（原版 prefab 里这个节点在、只是 `SetActive(0)`）");
                CheckTrue(btnLk != null && !btnLk.gameObject.activeSelf,
                          "★★ A402②：锁定态 ⇒ 整颗 `Unlock Button` **`SetActive(false)`**（原版 `SetPremiumButton.c:85`）"
                          + " —— 🔴 这条与下面那条弱断言**必须并存**：`!wb.Interactable` 在「隐藏」与「只变灰」"
                          + "两种世界里**都是真**（= 弱断言），只有 `activeSelf` 分得开。"
                          + "删掉 `if (lockedHere) btn.gameObject.SetActive(false);` ⇒ 这条红、弱断言照样绿");
                var hitLk = FindPath(pLk, "Unlock Button/Hit");
                var wbLk = hitLk != null ? hitLk.GetComponent<WindowButton>() : null;
                CheckTrue(wbLk != null && !wbLk.Interactable,
                          "★ A402②（**弱**）：同一颗钮的 `interactable` 也是假 —— ⚠️ **单靠这条什么都证明不了**（见上）");

                // ③ 布局表：`items + Warning + Badge`（**不含 `UnlockW`**）
                //    期望值 = 原版子件集逐格累加的字面量（1 件物品：30+200+25+300+25+100+30 = 710；
                //    ⚠️ 其中 300 = `WarnW`，那是**我们挑的**常量，见 `Shell/CampaignRewardWindow.cs:626` 的注释）
                CheckNear(cwA402.PremHolderRect.W, 710f, 0.5f,
                          "★★ A402③：锁定支的高级列 holder 内容宽 = **710**（1 物品 200 + `Warning` 300 + `Badge` 100 ·"
                          + " pad 30/30 · spacing 25 —— **没有 `UnlockW`(245)**）"
                          + " —— 把 `widths.Add(UnlockW)` 改回无条件 ⇒ 980（右边缘 2005 出屏）⇒ 红");
                CheckTrue(cwA402.PremHolderRect.x2 <= 1920f,
                          $"★ A402③：锁定支的高级列 holder 落在屏内（{cwA402.PremHolderRect.x1:F0}..{cwA402.PremHolderRect.x2:F0}）");
                CheckNear(cwA402.BaseHolderRect.W, baseW, 0.5f,
                          "★ A402③：**基础列不受锁定支影响**（`IsPremiumLocked` 只在 `SetPremiumButton` 里被读）");
                // ③b 量**渲染真值**（表里若还留着 `UnlockW`，徽标会被推到 1925 = 出屏）
                float lkx1, lky1, lkx2, lky2;
                var badgeLk = FindChild(pLk, "Badge");
                CheckTrue(RectOf(badgeLk, out lkx1, out lky1, out lkx2, out lky2),
                          "★ A402③：（前提）锁定支的 `Badge` 量得到渲染矩形");
                if (RectOf(badgeLk, out lkx1, out lky1, out lkx2, out lky2))
                    CheckNear((lkx1 + lkx2) * 0.5f, 1655f, 2f,
                              $"★★ A402③：`Badge` 的渲染中心 x = **1655**（= 列左 1025 + 30 + 225 + 325 + 50；"
                              + $"实测 {(lkx1 + lkx2) * 0.5f:F1}）—— 表里若还留着 `UnlockW` 它会落到 1925（出屏）");
                cwA402.Close();

                // ---- 夹具 B：**不锁 ∧ 不可点** ⇒ 那颗钮【在】但【变灰】—— 与 ② 成对 ----
                var cwA402b = CampaignRewardWindow.Create(wm2);
                wm2.OpenWindow(cwA402b, new CampaignRewardsContext
                {
                    Rewards = CampaignData.RewardsOf(0), BaseCollected = false, PremiumCollected = false,
                    Claimable = false, IsPremiumLocked = false, PointCost = 100, Army = 10,
                });
                var pGr = FindPath(cwA402b.transform, "Content/Scroll View/Viewport/Content/Premium Rewards/Rewards");
                var btnGr = FindPath(pGr, "Unlock Button");
                var hitGr = FindPath(pGr, "Unlock Button/Hit");
                var wbGr = hitGr != null ? hitGr.GetComponent<WindowButton>() : null;
                CheckTrue(btnGr != null && btnGr.gameObject.activeSelf && wbGr != null && !wbGr.Interactable,
                          "★★ A402（对照）：**不锁**时同位置那颗钮 **在**（`activeSelf` 真）**且 `interactable` 假**"
                          + " ⇒ 与 ①② 合起来把「**隐藏**」与「**变灰**」钉成两种可分辨的状态"
                          + " —— 把锁定支改回「只 `Interactable = false`」⇒ 这条绿、②红（正是要的鉴别力）");
                CheckTrue(wbGr != null && wbGr.GrayedForTest,
                          "★ A402（对照）：而且**真的灰了**（材质换成 `Everguild/UI/Greyscale`，同 A466②b 的口径）");
                var warnGr = FindPath(pGr, "Warning");
                CheckTrue(warnGr != null && !warnGr.gameObject.activeSelf,
                          "★ A402（对照）：不锁 ⇒ `Warning` **关着**（原版 `SetPremiumButton.c:32`）");
                cwA402b.Close();

                // ---- 夹具 C/D：**A472** 那道门 ----
                System.Func<bool, bool, CampaignRewardWindow> mkA472 = (baseCollected, claimable) =>
                {
                    var w = CampaignRewardWindow.Create(wm2);
                    wm2.OpenWindow(w, new CampaignRewardsContext
                    {
                        Rewards = CampaignData.RewardsOf(0), BaseCollected = baseCollected, PremiumCollected = false,
                        Claimable = claimable, IsPremiumLocked = false, PointCost = 100, Army = 10,
                    });
                    return w;
                };
                System.Func<CampaignRewardWindow, bool, WindowButton> hitA472 = (w, baseTier) =>
                {
                    var h = FindPath(w.transform, "Content/Scroll View/Viewport/Content/"
                                    + (baseTier ? "Base Rewards" : "Premium Rewards") + "/Rewards/Unlock Button/Hit");
                    return h != null ? h.GetComponent<WindowButton>() : null;
                };

                // C：基础档**没领** ⇒ 高级那颗**点不动**（反例断言）；同一刻基础那颗可点
                var cwA472No = mkA472(false, true);
                var pbC = hitA472(cwA472No, true);
                var ppC = hitA472(cwA472No, false);
                CheckTrue(pbC != null && ppC != null, "★ A472：（前提）两列那颗 `Unlock Button/Hit` 都取得到");
                CheckTrue(pbC != null && pbC.Interactable,
                          "★★ A472：同一份 context（`Claimable = true` · `PointCost = 100`）⇒ **基础**那颗可点"
                          + "（原版 `SetBaseButton.c:84` `set_interactable(btn+0x38, *(ctx+0x2a))`）");
                CheckTrue(ppC != null && !ppC.Interactable,
                          "★★ A472（**反例**）：**基础档没领 ⇒ 高级那颗点不动**"
                          + "（原版 `SetPremiumButton.c:40` `SetAsFreeClaim(btn, ctx.BaseCollected)` ⇒"
                          + " `SetAsFreeClaim.c:15` 直落 `interactable`）—— 把高级列也按 A466 那套"
                          + "（`PointCost < 1 || Claimable`）算 ⇒ 这条红（那套在这里给的是真）");
                cwA472No.Close();

                // D：基础档**领了**（哪怕 `Claimable = false`）⇒ 高级那颗**可点**
                var cwA472Yes = mkA472(true, false);
                var pbD = hitA472(cwA472Yes, true);
                var ppD = hitA472(cwA472Yes, false);
                CheckTrue(pbD != null && !pbD.Interactable,
                          "★ A472：基础档已领 ⇒ **基础**那颗进 `SetAsClaimed`（`SetBaseButton.c:28-31`）⇒ 不可点");
                CheckTrue(ppD != null && ppD.Interactable,
                          "★★ A472：基础档已领 ⇒ **高级**那颗可点（`ctx.BaseCollected` 真）"
                          + " —— ⚠️ 这条与上面一条**同一枚 `Claimable`**（都是假）却给出相反的可点性"
                          + " ⇒ 两条合起来钉死「高级列读的不是 `Claimable`」");
                cwA472Yes.Close();
            }
```

**⚠️ 两条如实标注**（写进断言注释里，别当自证）：

1. `710` / `1655` 这两个期望值里含 `WarnW = 300`（**我们挑的**常量，`Shell/CampaignRewardWindow.cs:626`）
   —— 其余每一项都是原版读数。**§七·1 那笔账做完时，这两个数会一起变**（表变成「只有物品」）。
2. 夹具 B 那条「对照」是**我们这一侧**的两态对照（原版里没有「显示着的灰高级钮」这个状态），
   它的用处是**证明 ② 那条强断言有鉴别力**，不是「原版长这样」。

### 五·3 怎么自造锁定态（一句话）

`new CampaignRewardsContext { …, IsPremiumLocked = true, … }` + `wm2.OpenWindow(win, ctx)` 即可
（真数据恒 `false`：`Shell/CampaignTab.cs:805`；见 §五·2 注释里那句）。

### 五·4 🔴 「派活必查行」三条 —— 拿它们 grep 了被测宿主 `Editor/RewardsScene.cs`

| 必查行 | 本件会不会踩 | 判据 |
| --- | --- | --- |
| ① **断言自证 / 同义反复** | ⚠️ **有一条带保留**（已标注） | §五·2 的 **③ / ③b** 期望值（`710` / `1655`）里含 `WarnW = 300`（**我们挑的**常量）⇒ 那两格**不是**纯原版读数（块里第 3 条注释已写明）。其余每个数都是**原版读数**：`245/45`（`Unlock Button` sizeDelta）· `100×100`（`Badge`）· `pad 30/30` · `spacing 25` · `530`（基础列）· `BaseCollected` 语义（`SetPremiumButton.c:40`） |
| ② **弱断言分不出两种状态** | ✅ **本件正是来修它的** | ② 用 `activeSelf`（强），并把「`!wb.Interactable` 在『隐藏』与『变灰』两种世界里**都真**」写成注释；④ 是对照档。两条合起来才有鉴别力 |
| ③ **「凡 `!RectOfUnion` / 『一个 quad 都没有』」式断言** | ✅ **不会变红、也不会变假绿** | §三·d 一带这类只有两处：`:4885` `RectOfUnion(premRewards, …)`、`:4897` 高级列 `Unlock Button` 的 `GetComponentsInChildren<ImageQuad>(true).Length == 0`（「整块在视口外 ⇒ 一个 quad 都不建」）。🔴 **两处的夹具都是 `IsPremiumLocked = false`**（`:4845`）⇒ 本件那句 `SetActive(false)` 只落在锁定支 ⇒ **两处一行都不动** |

**🔴 还有一条更要紧的（顺手查出）**：`Editor/RewardsScene.cs` 里**没有任何既有夹具是「我们这扇窗」的锁定态** ——
`grep -n "IsPremiumLocked = true" Editor/RewardsScene.cs` 只命中 `:5413` / `:5444`，而那是**领奖窗 `rw`**
（`RewardWindowContext`，另一个类、另一节 §⑧·b/§⑨），**不是本窗**
⇒ **A402 这两条在自检里今天零覆盖**（本件之前它连「实现」都没有）。这正是 §五·2 那两块夹具必须落地的理由。

### 五·5 本件改动对**既有断言**的静态影响面（没跑 Unity，只能静态判）

- 全部既有夹具都是 `IsPremiumLocked = false`（`:4447 / 4584 / 4619 / 4655 / 4675 / 4690 / 4731 / 4845 / 5012 / 5299`）
  ⇒ `lockedHere == false` ⇒ **宽度表逐格不变**（`items + UnlockW + BadgeSize`）·
  `iWarn = iBtn + 1` / `iBadge = iBtn + 1`（与改前的 `iWarn = iBtn+1` / `PremiumLocked ? … : iWarn` **完全同值**）·
  `_warn.SetActive(false)`（与改前同）· 按钮 `SetActive` 那句**不进**。
- 基础列：`clickable` 表达式**逐位未变**（`isBase ?` 取的那一支 = A466 原式）。
- 高级列：`clickable` 变了，但**没有任何既有断言读高级列那颗钮**（A466 那三块全在 `Base Rewards/Rewards`，`:4568-4631`）。
  ⇒ 静态判定：**既有断言零影响**。⚠️ 这只是静态判定，真红绿要等收口那次跑（本批口径：A 表清零前不跑中途自检）。

---

## 六、没查清 / 没做的

1. 🔴 **两笔账的断言没落盘**（§五·0）—— 是本件唯一没做完的事，原因 = 宿主文件归 W-E3（`批次计划_1013.md:99/:135`）。
   成品块在 §五·2（标识符零冲突，直接粘）。
2. ⚠️ **`WarnW = 300` 这个常量的「原版真值」没定**（它不是本件两笔账的一部分，见 §七·1 与 §七·3）：
   - 现读 prefab：`Warning` 的 `anchorMin.x 0.1 / anchorMax.x 0.9` · `sizeDelta = (30, **45**)` · `pivot (0.5,0)`
     · `anchoredPosition (0,15)` ⇒ **显示宽 = 0.8 × 父宽 + 30**（用 prefab 自己那组数验算：父 60 ⇒ 0.8×60+30 = **78**
     = dump 里的 `78.00×45.00` ✓ 逐位吻合）；
   - ⇒ 它到底**占不占布局格**：**不占**（§七·1 的 `m_IgnoreLayout = 1`）—— 所以「显示宽」与「布局贡献」是两件事，
     我们现在这张表把两者混成了一个数。
3. ⛔ **没跑任何 Unity 自检**（简报里的红线；本批口径也是「A 表清零前不跑中途自检」）。
   ⇒ §五·2 那块断言**没被执行过**，只是按同一节里既有断言的写法（`FindPath` / `RectOf` / `TextOf` / `WindowButton`）
   逐句对齐写出来的。**类型检查过了**（§八）—— 但那块**还没插进文件**，所以不在那 0 里。
4. ⛔ 没碰：`CampaignTab.cs`（A472 的真身不在那儿）· `CampaignData.cs`（§2.3 的张力**只解释不改**）·
   其他任何白名单外文件 · git。

---

## 七、顺手发现（⛔ 别自己顺手改；请调度台分流）

### 七·1 🔴🔴 **原版那三颗件全都 `m_IgnoreLayout = 1` ⇒ 我们整张「子件宽度表」模型的前提是错的**

**证据链（三条独立互证，全部亲读）：**

1. **字段**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`，prefab 原文）：
   - `Warning`：其 TMP 的 `m_GameObject` → `GameObject/Warning_7716984605155957894.json` →
     组件里的 `LayoutElement` = `MonoBehaviour_-2770069720850916218.json`：
     **`m_IgnoreLayout = 1`**（`m_MinWidth/m_PreferredWidth/...` 全 `-1`）。
   - `Unlock Button`：`GameObject/Unlock Button.json` 与 `Unlock Button_1169604027140578438.json`（两列各一颗）
     —— 两颗的 `LayoutElement` **都是 `m_IgnoreLayout = 1`**。
   - `Badge`：**几何反证**——它的序列化矩形 `1025.00,295.00→1125.00,395.00`（`anchor (0,1)→(0,1)`、`pos (0,0)`）
     正好落在 holder 的**左上角**（holder = `1025..1085`），**连 padL(30) 都没加**
     而布局组摆出来的子件一定从 `holder.x1 + padL` 起 ⇒ **它没被布局组摆过**（配 `Image,LayoutElement` 两颗组件）。
2. **uGUI 源码**（本地包）：`d:/4/Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/LayoutGroup.cs:52-79`
   `CalculateLayoutInputHorizontal()` 建 `m_RectChildren` 时**跳过**两类子件：① `!rect.gameObject.activeInHierarchy`（`:58-59`）
   ② 带 `ILayoutIgnorer` 且 **`ignoreLayout == true`**（`:70-78`）—— 而 `LayoutElement` 就是 `ILayoutIgnorer`。
   `m_RectChildren` 是**算尺寸与摆位置**唯一用的那张表。
3. **几何**（同一份 prefab dump，两列都验过）：
   - `Unlock Button`（`(0.5,0)→(0.5,0)` · `pivot (0.5,0)` · `pos (0,15)` · `sizeDelta (245,45)`）：
     高级列 `932.50,850.00→1177.50,895.00` = **以 holder 中心（1055）为心**、**底边离 holder 底 15px**
     —— 布局组**永远不会**摆出「水平居中」。
   - `Badge` 同上：holder 的**左上角**，不是「按钮右边那一格」。
   - `Rewards`（holder）自己在 prefab 里 = **60 宽**（`CSF h:MinSize=60`）= **padL30 + padR30 + 0 个物品**
     ⇒ **三颗件一格都没贡献**（若按我们那张表算，应该是 485+）。

**⇒ 正确的模型（后续那笔账直接照这个做）**：

| 件 | 谁摆 | 落点 |
| --- | --- | --- |
| 物品抽屉（运行时插进来的） | **holder 的 `HorizontalLayoutGroup`**（唯一参与布局的子件） | `MiddleCenter` + `childControlHeight=1` ⇒ **竖向居中在内容区**，从 `holder.x1+30` 起、spacing 25 —— ✅ **我们这一半是对的**（`VertCenter` + `Horizont…(PadL, Spacing)`） |
| `Unlock Button` | **锚点** | 列**底部居中**：`anchor (0.5,0)` · `pivot (0.5,0)` · `pos (0,15)` · `245×45` |
| `Warning` | **锚点** | 列**底部居中**、宽 = **0.8×holder宽 + 30**、同样离底 15px（锁支替掉按钮） |
| `Badge` | **锚点** | 列**左上角**：`anchor (0,1)` · `pivot (0,1)` · `pos (0,0)` · `100×100` |

**⇒ 我们今天的偏差（都在 `BuildColumn` 里）**：
① 三颗件**都不该进那张宽度表**（表 = 只有物品）⇒ holder 内容宽 = `60 + n×200 + (n−1)×25`（1 物品 = **260**，
不是 655/710）；② 按钮与警告应在**列底部居中**（我们 `VertCenter` 到内容区中心 + 排在物品右边）；
③ 徽标应在**列左上角**（我们排在按钮右边）。

**🔴 调度提示（时间敏感）**：`A239`（`UguiLayout` 逐子件宽）的**断言**正是 **W-E3** 待做的
（`批次计划_1013.md:99`），而它要钉的就是**这张模型的表** ⇒ **建议在 W-E3 动笔前先裁这一条**，
否则它会把错模型钉死（同 `CLAUDE.md` 「自检绿不等于口径对」）。另外现存的
`Editor/RewardsScene.cs:4489-4500`（`baseW`/`premW` 两个期望值）也建在同一前提上，要一起改。
⚠️ 同时提醒：`Editor/RewardsScene.cs:4522` 那条「物品图标与 `Unlock Button` **竖向中心对齐**」的断言，
**在原版几何下是错的**（按钮在列底）⇒ 那一笔要连断言一起翻。

### 七·2 高级列那颗钮在**原版**里永远不是「付点解锁」态

- `CampaignUnlockButton__SetUnlockCost` 在**全量反编译**（`d:/2/tools/decomp_full/`，26,282 个方法体）里
  **零调用点**：`grep -rn "UnlockCost" .` 只命中它自己的定义文件。
  ⚠️ **覆盖面如实**：那是「抓出来的方法体集合」，**不能 100% 排除**某个没被反编译到的方法里调它。
- 而 `CampaignRewardsWindow__Open.c:112-113` 只调 `SetBaseButton` + `SetPremiumButton`，后者的三支里
  **只有 `SetAsClaimed` / `SetAsFreeClaim`**，没有 `SetUnlockCost` ⇒ 原版高级钮的文案恒为「`Claim`（免费）」、**不显点数**。
- **我们这一侧**：`BuildUnlockButton`（`:648-653`）对**两列**都走「`PointCost ≥ 1` ⇒ 开 pointDrawer + 写点数」，
  而 `Shell/CampaignTab.cs:807` 给的 `PointCost` 恒 ≥ 100 ⇒ **今天这扇窗的高级钮在真数据下恒显点数**。
  ⛔ **本件没改**（不在 A402/A472 的判据里；改它要连 `SetUnlockCost` 到底谁调一起查清）⇒ **另立账**。

### 七·3 `WarnW = 300` 的来历与本件改动的关系

- `Shell/CampaignRewardWindow.cs:626` 明写着「**这是我们挑的**」（原版取不到，循环依赖）。
- 本件**没有动它**（简报给的形状就是 `items + WarnW + BadgeSize`）。
- 但 §七·1 查清之后：**它不该在表里**（`m_IgnoreLayout = 1`）⇒ 那笔账做完时 `WarnW` 只剩「显示宽」一个用途
  （而且真值 = `0.8 × holder宽 + 30`，见 §六·2）⇒ **这一格会再改一次（710 → 260）**。

### 七·4 一条过期记录（铁律 5，⛔ 本件只报不改）

`资料/普查产出_1012/E1_逐子件宽与Clip清空.md` §四·3 那句「**锁定支的 holder 宽（980）只是按表算的**」——
**本件改完之后 980 不再成立**（锁定支 = **710**）。它是另一份报告里的历史读数，
`A表现核_块1.md`（块 1 §A402 末）已经预告了它「自然消失」⇒ 建议调度台顺手在那一处加一行订正指针。

---

## 八、类型检查结果

```
$ cd d:/4/Unity && TMPDIR=/tmp/wf_wc1 bash 工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 独立 `TMPDIR`（`/tmp/wf_wc1`），**没有**编到别人正在写的半成品（错误数 0，无需「重跑一次 + 如实记」）。
- 🔁 **收尾又跑了一次**（因为同批别的写手在并行改 `.cs`：`git status` 里已有 `Editor/MainMenuScene.cs` 等）
  ⇒ **同样 0 / 0**（两次都是干净的：`运行时 0 · 编辑器 0`）。
  ⇒ 这条 0 是「**整棵树**」的 0（含别人当时的文件），**不只是我那个文件**。
- 改动文件行尾：`Shell/CampaignRewardWindow.cs` **全文 LF（CRLF 0 / LF 910）**，`git diff --numstat` = **65 / 8**
  （不是整篇重写）。
- 白名单核对：`git status` 里我这一个文件是 `Shell/CampaignRewardWindow.cs`（65/8）；
  其余 `.cs` 全是**别的写手**的（`Deck/DeckRuntime.cs` · `Editor/{DeckScene,MainMenuScene,BattleScene}.cs` ·
  `Shell/{MainMenuRuntime,AllianceMemberTab,AlliancesTab,ChatPanel,FriendsTab,SocialWindow,Daily*Popup}.cs` ·
  `Battle/BattleDriver.cs`）—— **我一行都没碰**。
