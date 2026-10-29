# B97_REMOTE1 · 商店 7 条 REMOTE（89/91/92/102/108/112/203）

> 执行代理 `B97_REMOTE1` · 2026-10-29（第九十七会话）
> 口径 = **CLAUDE.md §18·0**：「只有和服务器有关的才允许自己设计」（服务器已关服、判据在远端）
> ⇒ 7 条一律走「**客户端 UI 照原版 + 数据本地模拟 + 出声**」，本地那一格打 `🔶 自己设计（非原版）`，⛔ 无一条判 D。
> 账补丁 = `账/_parts/B97_REMOTE1.tsv`（7 行 · 11 列 · 首行表头 · `merge --dry` = **更新 7 / 新增 0** ｜ `check_patch_conflicts.py` = 无冲突）
> 断言 = `MyGame/Assets/CardPresentation/Editor/B97_REMOTE1_Assert.cs`（R1–R12b · S1–S7）
> `typecheck` = **我的五个文件 0 错**（见 §三 的读数与口径）

---

## 〇 一句话结果

**7 条 = 4 条落地（89 / 91 / 92 / 203，B→A）+ 3 条「本地模拟方案备好、代码未落」（102 / 108 / 112，维持 B）**。
后三条的落点文件 `Shell/GachaEventWindow.cs` 本波在 `B97_W2` 名下 ⇒ 按文件所有权红线**不碰**，补丁全文见 §四（可粘贴、含应插哪一节 + 调用链自证）。

| 账行 | 原版方法 | 前 → 后 | 落地/卡在哪 |
|---|---|---|---|
| `商店:89` | `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick()` | **B → A** | 真把付费窗开出来（🔶 本地报价表） |
| `商店:91` | `…<PurchasePremiumClick>b__12_0()` | **B → A** | 关窗回调：重跑面板 `Initialize` |
| `商店:92` | `…<PurchasePremiumClick>b__12_1()` | **B → A** | 关窗回调：转出 `onClosePurchasePopup` |
| `商店:102` | `GachaCratePanel.<OpenCrateButtonClick>b__14_0(…)` | B → B | **补丁就绪**（🔶 本地开箱奖励表 + 默认 hook），文件被占 |
| `商店:108` | `GachaEventTab.Initialize()` | B → B | **补丁就绪**（🔶 本地 `CardsSpecialConfig` 表），文件被占 |
| `商店:112` | `GachaEventTab.SetEventTab(ILiveOps)` | B → B | **补丁就绪**（🔶 本地 `ActionScheduler` 替身），文件被占 |
| `商店:203` | `PurchasePremiumWindow.PremiumArmyPurchaseData.TryPurchase(onPurchase, onFailPurchase)` | **B → A** | 失败支真接了（错误弹窗 + 出声） |

---

## 一 本批**改了什么代码**

| # | 文件 | 改动 | 行号策略 |
|---|---|---|---|
| 1 | **新建** `Shell/ExpansionPassPremiumOffers.cs`（233 行） | 🔶 本地报价表（`ForEvent` / `Replace` / `ClearForTest` / `ResetForTest`）+ `SayCannotOpen`（原版第 ⑤ 句那条带 id 的告警）+ `WireClose`（两条 `+=`）+ `Reinitialize`（`b__12_0`）+ `NotifyParent`（`b__12_1`）+ 四个自检读口 + 次序串 | 新文件 |
| 2 | `Shell/ExpansionPassEventWindow.cs` | `PurchasePremiumClick()` 方法体重写（13 行 → 13 行）；另 4 处过期注释就地订正（铁律 5） | **文件总行数 2883 不变**（改写都是**等行替换**）⇒ `:2722/:2738/:2753/:2789/:2853` 等落点零漂移（`check_ledger_points` 现跑：本文件无任何漂移/失败） |
| 3 | `Shell/ShopTabPage.cs` | `DoBuy` 里**一行**改成 `if (!ShopData.PurchaseSucceeded(got)) return OnPurchaseFailed(idx, got); else Debug.Log(…);`；类尾追加 `OnPurchaseFailed` + 三个自检读口 + `ResetPurchaseFailForTest` | `DoBuy` 那一行**原位替换**（零行漂移）；新成员**追加类尾**（1250 → 1293，账里本文件最大落点 = `:1197`） |
| 4 | `Shell/ShopData.cs` | `Buy` 的 `return "越界"` 换成 `return BuyNotFound`（**等行替换**）；类尾追加 `BuyNotFound` 常量 + `PurchaseSucceeded` + 判据注释 | 690 → 714，新成员在**类尾**（账里本文件最大落点 = `:624`） |
| 5 | **新建** `CardPresentation/Editor/B97_REMOTE1_Assert.cs` | R 组（89/91/92）12 条 + S 组（203）7 条 | 新文件 |

⚠️ **没碰**：`Shell/GachaEventWindow.cs`（`B97_W2`）· 简报里列的其余别人名下的文件 · `Editor/RuleEngineTest.cs` · 账主表 · 正本 · `Pinned*` · `gen_cards_engine.py` · git。

---

## 二 逐条（7 条，一条不落）

### `商店:89` `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick()` — **B → A**

- **原版 UI 怎么做的**（`decomp_full/…__PurchasePremiumClick.c` 五跳，逐句）：
  ① `data = 组件 +0x58` 上的 `ExpansionPassData`；`premiumOfferData = data.PremiumOfferData`（**两道 null 闸：为 null 就整段什么都不做**）
  ② `eventData = LiveOpsManager.GetHandler<>().GetEvent(premiumOfferData.GetID())`
  ③ `prefab = LiveOpsAssetUtility.GetComponentReference(premiumOfferData, 0xc)` ← **要开的那扇窗**
  ④ `win = WindowsManager.Instance.OpenWindow(prefab, eventData)`
  ⑤ `win == null` ⇒ `CustomDebug.LogWarning("…" + premiumOfferData.GetID())`（**告警带事件 id**）
  ⑥ 开出来了 ⇒ 往 `win + 0xc` 那个 nullary 委托上 **`Delegate.Combine` 两条**（`b__12_0` 先、`b__12_1` 后；**没有 `-=`/去重**）
- **我们照做了什么**：`PurchasePremiumClick()` 逐跳对位 —— ① 查 🔶 本地报价表（`ForEvent(evId)`，查不到 ⇒ `SayCannotOpen` 出声返回 false）；③④ `WindowsManager.OpenPurchasePremiumWindow(offer.FocusArmy)`（= 原版 `PurchasePremiumWindow` 的本仓实现）；⑤ `win == null` ⇒ 同形状告警；⑥ `ExpansionPassPremiumOffers.WireClose(win, evId, this)` 挂两条。
- **哪一格是本地模拟（🔶）**：**③ 的「窗口 prefab 引用」那一格**。原版那一格是远端 `PremiumOfferData` 上的 `GetComponentReference(…, 0xc)`；本机 `assets_full` 全库 0 命中（原版已关服 ⇒ 永远在远端）⇒ 本地表（事件 id → 聚焦阵营）＋ 本仓那扇 `PurchasePremiumWindow` 顶替。`offers` 传 `null` **不是偷懒**：原版 prefab 的 `premiumStoreReference` 本身就是空引用 ⇒ `FetchData()` 空表 ⇒ 「一个 `Army Container` 都不建」，同形（`Shell/PurchasePremiumWindow.cs` 文件头 ④）。
- **`win + 0x60` 的对位（如实标，这是本批唯一有不确定度的地方）**：挂到 `GameWindow.OnCloseWindow`（nullary `Action`，`GameWindow__Hide.c` 末句 Invoke）。
  - 对得上的：**偏移**（`+0xc` 个 `longlong` = `+0x60`）· **委托形状**（nullary）· **语义**（`b__12_1` 调的是面板自己的 `onClosePurchasePopup`、`b__12_0` 重跑 `Initialize` ⇒ 两条都读作「购买弹窗关掉了」）。
  - 没能确证的：那个字段的**名字**。`GameWindow` 类桩里只有 `OnOpen` / `OnCloseWindow` 两个 `Action`，而全库**没有任何类声明** `UIGenericEventCatcher.SourceDelegate` 字段（`grep` 现读）⇒ 反编译把 `Action` 的 `___ctor` 显示成了同址的 `SourceDelegate___ctor`（泛型委托共用 VA）。
  - ⛔ **别改成「买入完成就刷新」**：那就和 `onClosePurchasePopup` 的命名、以及 `b__12_0` 的 `Initialize` 语义都对不上了。
- **断言**：R1–R5（空表 ⇒ 返回 false + 出声 ／ 表里有 ⇒ 返回 true）。

### `商店:91` `…<PurchasePremiumClick>b__12_0()` — **B → A**

- **原版**：全文一句 `Initialize(this.data(+0x58), this.onClose(+0x60))`。
- **我们照做了什么**：`ExpansionPassPremiumOffers.Reinitialize(panel)` ⇒ `panel.ApplyPremiumPanel(panel.OnClosePurchasePopup)`。本仓的 `Initialize` 就是 `ApplyPremiumPanel`（它第一句 = `onClosePurchasePopup = 入参`，与原版 `Initialize` 首句逐字对齐；`_event == null` 时它自己早退）。
- **本地模拟**：无（这一条本身不含数据）。**挂接点**的取舍见上一条。
- **断言**：R7（关窗后计数 = 1）+ R12（次序串 `"RN"`）。

### `商店:92` `…<PurchasePremiumClick>b__12_1()` — **B → A**

- **原版**：全文一句 `onClose(+0x60)` 非空就调它（nullary）。
- **我们照做了什么**：`NotifyParent(panel)` —— 非空 ⇒ 调 `panel.OnClosePurchasePopup`；null ⇒ **原版「什么都不做」**，我们只记一格 + 出声（⛔ 不加「只调一次」的护栏、⛔ 不吞）。
- **本地模拟**：无。**注意**：今天 `onClosePurchasePopup` **恒 null**（`InitializeEventTab` 调 `ApplyPremiumPanel()` 不传参）⇒ 生产路径落「什么都不做」那一支，自检注入回调用例两态对照。
- **断言**：R8（真调出去 1 次）/ R9（非 null 时不落 null 支）/ R11（null 时落 null 支）/ R12b。

### `商店:102` `GachaCratePanel.<OpenCrateButtonClick>b__14_0(IReadOnlyList<RewardInfo>)` — **B（补丁就绪）**

- **原版**：服务器购买回包回调，两句 —— `crateRewards = rewards` → `crateOpenned = true`。
- **我们已经有什么**：`OnCratePurchaseComplete(rewards)`（两句逐字）+ 注入位 `CratePurchaseHook` + `TryPurchaseCrate()`。**缺的只有「本地回包」这一格**。
- **本地模拟（🔶）**：开箱奖励表（箱 id → 奖励）+ **默认 hook**，让整条开箱链在本地真的走完（`OpenCrateButtonClick` ⑤ → `b__14_0` → 状态机 state1 等到 `_crateOpened` → state2 发奖）。补丁见 **§四 P1**。
- **为什么不落地**：`Shell/GachaEventWindow.cs` 本波归 `B97_W2`（简报红线 + 文件所有权表）⇒ 只交补丁。

### `商店:108` `GachaEventTab.Initialize()` — **B（补丁就绪）**

- **原版 ⑥**：`getCardsButton.SetActive(ConfigManager.GetConfig<CardsSpecialConfig>().whereToGetCardsData.expansionData.Length != 0)`。
- **我们已经有什么**：`WhereToGetCardsVisible`（🔶 已打标、默认 `true`）+ `ApplyWhereToGetCardsVisible()`，两态都能摆。
- **本地模拟（🔶）**：`CardsSpecialConfig` 的实例 SO 本机没有（`assets_full` 只有 MonoScript 类桩）⇒ 建**本地配置表**（`LocalWhereToGetCardsExpansionCount`），⑥ 那个 bool 由它算 + 出声（替掉现在写死 `true`）。补丁见 **§四 P2**。

### `商店:112` `GachaEventTab.SetEventTab(ILiveOps liveOps)` — **B（补丁就绪）**

- **原版 ④**：`CurrentOffer != null` ⇒ `ActionScheduler.Schedule(...)` 存句柄（`Reset` 时 Release）。
- **判据（`.c` 现读）**：`ActionScheduler__Schedule.c:26-30` = `如果 传入时刻 < TimeHelper.get_ServerTime() ⇒ 返回 0`；到期打 `PlayfabWrapper.GenericCloudScriptHandler`（**云脚本**）。
- **本地模拟（🔶）**：`ActionScheduler` 的本地替身 —— 服务端时钟用**本地时钟**、到期**只出声**（⛔ 不假装打过云脚本）；`EventTabSource` 补一格 `ScheduledServerTime`（现在是 `object CurrentOffer`，拿不到时刻，这才是「恒 null」的真正原因）。补丁见 **§四 P3**。

### `商店:203` `PremiumArmyPurchaseData.TryPurchase(onPurchase, onFailPurchase)` — **B → A**

- **原版**：`PurchasesManager.Instance.Purchase(storeItem, onSuccess, onFailPurchase)`（`…__TryPurchase.c` = 一句）。失败支 = `PurchasePremiumWindow.OnPurchasedFailed`：① `ErrorPopupWindowController.ShowErrorMessage(10)`（**10 = `CustomError.InternetUnreachable`**，枚举现读 `:30`）② 收等待框 ③ 清 `itemBeingPurchased`。
- **我们照做了什么**：`ShopData.Buy` 那句「这一件本地不存在」的**哨兵**升成常量 `BuyNotFound` + 判定口 `PurchaseSucceeded`；`DoBuy` 用它分流 ⇒ `ShopTabPage.OnPurchaseFailed(idx, got)`：出声 + `ErrorPopupWindowController.ShowErrorMessage(CustomError.InternetUnreachable)`，**⛔ 不再谎报「买到了 X ×N」**（改前越界那句会被当成成功打日志、还会继续走 `Setup()` + 开领奖窗）。
- **本地模拟（🔶）**：「服务端回包失败」这一格不可达（关服）⇒ 本地**唯一**能真失败的一支 = 这一件取不到。⛔ **没加余额闸**（用户 2026-09-17 边界②：不做真实经济；原版那一支恒成功）。
- **仍存的差异**（已写进账的差异列）：① 服务端回包那条失败源不可达；② **原版「限购售罄也拦」（`AvailableMax > 0 && Available == 0`）本地仍未拦** —— 见 §五 第 2 条；③ 顺序：我们收等待框在弹错误窗**之前**（原版相反；不能反过来，否则等待框会盖在错误窗上，最终画面一致）。
- **断言**：S1–S7（哨兵字面量 / 两态判定 / 10 的枚举值 / 词条在表里 / 读口清空）。

---

## 三 交付读数

### `typecheck`（`TMPDIR=/tmp/wf_rem1`）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 本批自己的四轮改动各跑过一次；收尾最后一次 = **0 / 0**。
- 中途有一轮出现过 **4 条错，全部落在 `Shell/RankedDivisionParticles.cs` 与 `Shell/RankedDivisionChangeWindow.cs`** —— 那是**别的代理正在写的文件**（本批全程没碰，`grep` 我的文件 **0 命中**）；对方改完之后复跑即回到 **0 / 0**。按 CLAUDE.md §13·3② 处置。
- ⚠️ 本批**没跑 Unity**（红线）⇒ R / S 两组断言**只过了 typecheck、没跑过**。

### 账补丁

```
python -I -X utf8 工具/merge_ledger_parts.py --dry --only B97_REMOTE1
  [dry] 会写 商店.tsv
  合计：更新 7 · 新增 0 · 无变化 0
python -I -X utf8 工具/check_patch_conflicts.py
  ✅ 没有跨补丁重复键（3 份补丁）
```
（键 = `(原版类, 方法)` 逐字照抄主表 ⇒ 7 条都是**就地更新**、没有新行。）

### 断言 —— **建议接线位置**（本批不改 `Editor/RuleEngineTest.cs` 之外的宿主）

`MyGame/Assets/CardPresentation/Editor/ShopScene.cs`，**接在 `:5876` 之后**：

```csharp
        B97SHOPAssert.Run();
        _sink.Pass += B97SHOPAssert.Pass; _sink.Fail += B97SHOPAssert.Fail;
        // 🆕 2026-10-29（B97_REMOTE1）：商店 REMOTE 四条（89/91/92 付费档 · 203 购买失败支）
        B97REMOTE1Assert.Run();
        _sink.Pass += B97REMOTE1Assert.Pass; _sink.Fail += B97REMOTE1Assert.Fail;
```

⚠️ 位置理由：R 组会**真的开一扇 `PurchasePremiumWindow`**（走生产那条 `WindowsManager.OpenPurchasePremiumWindow`）⇒ 必须有 `WindowsManager`；`ShopScene` 本来就在 `:5178` 开过同一扇，是已验证的路径。

---

## 四 三条补丁（`Shell/GachaEventWindow.cs` · 供调度台转派给 `B97_W2`）

> 三条共用一个前提：**都追加在该类【类尾】`OnCratePurchaseComplete` 之后**（`GachaEventWindow.cs` 现在 1678 行，`OnCratePurchaseComplete` 收在 `:1676`）。
> ⛔ 不要插在中间：账在本文件上有大量 `文件:行` 落点。

### P1 —— `商店:102`：🔶 本地开箱奖励表 + 默认 hook

**应插哪一节**：类尾（`OnCratePurchaseComplete` 之后）。**另需**在 `Build()`（或 `Open()`）末尾调一次 `InstallLocalCratePurchaseHook()`。

```csharp
        // ============================================================ 🆕 账 `商店:102`（B97_REMOTE1 补丁）
        // 🔶 自己设计（非原版）：开箱奖励表的**本地替身**。
        // · 这是什么：原版 `GachaEvent.CurrentOffer.TryPurchaseOffer(onComplete, null, showRewardPopup:false)` 的
        //   `onComplete`（= `b__14_0`）由**服务端回包**调，回包里带着 `IReadOnlyList<RewardInfo>`。
        // · 为什么必须自定：`CurrentOffer` 在远端 LiveOps（本机恒 null），原版已关服 ⇒ 判据永远在远端。
        // · 谁定的：调度台 2026-10-29 裁定（CLAUDE.md §18·0：只有和服务器有关的才允许自己设计）。
        // ⚠️ 与「不做假开箱」的关系：改前 `TryPurchaseCrate()` 恒 false ⇒ 点了只收摊；接上本 hook 之后，
        //   开箱链在本地**真的走完**（⑤ → `b__14_0` → state1 等到 `_crateOpened` → state2 发奖），
        //   而且**每次都出声**说清「奖励来自本地表，不是服务器回包」。
        public const string AnyCrateId = "*";

        /// <summary>🔶 箱 id → 奖励。出厂只有通配档一条（本地没有箱的真实 id 表）。</summary>
        public static readonly System.Collections.Generic.Dictionary<string, CampaignData.RewardSpec[]> LocalCrateRewards =
            new System.Collections.Generic.Dictionary<string, CampaignData.RewardSpec[]>();

        /// <summary>取这一箱的本地奖励（查不到就退回通配档；连通配档都没有 ⇒ 空表 + 出声）。</summary>
        public static CampaignData.RewardSpec[] LocalCrateRewardFor(string crateId)
        {
            CampaignData.RewardSpec[] r;
            if (!string.IsNullOrEmpty(crateId) && LocalCrateRewards.TryGetValue(crateId, out r)) return r;
            if (LocalCrateRewards.TryGetValue(AnyCrateId, out r)) return r;
            Debug.LogWarning("[Gacha] 本地开箱奖励表里没有这一箱（id = `" + (crateId ?? "<null>")
                           + "`）、也没有通配档 ⇒ 发空表（原版这一格是服务端回包的 `rewards`）");
            return new CampaignData.RewardSpec[0];
        }

        /// <summary>装上默认 hook（= 原版 `CurrentOffer.TryPurchaseOffer` 那一跳的本地替身）。
        /// <paramref name="replace"/> = false 且已有 hook ⇒ 不覆盖（自检注入的优先级更高）。</summary>
        public bool InstallLocalCratePurchaseHook(bool replace = false)
        {
            if (CratePurchaseHook != null && !replace) return false;
            CratePurchaseHook = onComplete =>
            {
                var rewards = LocalCrateRewardFor(null);
                Debug.Log("[Gacha] 开箱：🔶 **本地模拟**了一次服务器回包（" + rewards.Length
                        + " 条奖励）—— 原版这一格来自 `CurrentOffer.TryPurchaseOffer` 的服务端回包，"
                        + "本机已关服 ⇒ 奖励取自 `GachaEventWindow.LocalCrateRewards`（调度台 2026-10-29 按 §18·0 裁定）。");
                onComplete(rewards);                       // = 原版回包到达 ⇒ `b__14_0`
            };
            return true;
        }
```

**调用链自证**：`OpenCrateButtonClick()`（`:1405`）⑤ → `TryPurchaseCrate()`（`:1442`，一句 `hook(OnCratePurchaseComplete)`）→ 本 hook → `OnCratePurchaseComplete`（`:1672`）写 `_crateRewards` / `_crateOpened = true` → `CrateFlowBegin()`（`:1452`）→ `CrateFlowStep`（`:1462`）state 1 的 `if (!_crateOpened) return false;` 现在能过 → state 2 发奖。**没有新增任何一跳**，只把「远端那一跳」换成「本地那一跳」。

**断言片段**（转派时一起接进 `B97_REMOTE1_Assert.cs` 的 R 组之后）：

```csharp
        MenuCheck.Section(_sink, "G 账 商店:102 —— 🔶 本地开箱奖励表把开箱链走完");
        var g = new GameObject("B97REMOTE1_Gacha");
        var gw = g.AddComponent<GachaEventWindow>();
        try
        {
            GachaEventWindow.LocalCrateRewards[GachaEventWindow.AnyCrateId] =
                new[] { new CampaignData.RewardSpec { Id = "gold", Quantity = 1 } };
            Tr(gw.InstallLocalCratePurchaseHook(true), "G1 ★ 默认 hook 装上了（= 原版 `CurrentOffer.TryPurchaseOffer` 那一跳）");
            bool done = gw.OpenCrateButtonClick();           // ⑤ 拿到「回包」⇒ ⑥ 起状态机
            Tr(done, "G2 ★★ 开箱不再停在服务器那一跳（改前恒 false）");
            int guard = 0;
            while (!gw.CrateFlowStep(10f) && guard++ < 10) { }
            Tr(gw.CrateFlowStep(0f), "G3 ★ 状态机走到终点（state1 等的 `_crateOpened` 已被本地回包置真）");
        }
        finally { GachaEventWindow.LocalCrateRewards.Clear(); Object.DestroyImmediate(g); }
```

### P2 —— `商店:108`：🔶 本地 `CardsSpecialConfig` 表

**应插哪一节**：类尾。**另需**在 `BuildPage` 里**紧接** `ApplyWhereToGetCardsVisible()` 之前调一次 `ResolveWhereToGetCardsVisibleFromLocalConfig()`（现在是 `BuildPage` 末尾那一句直接 `ApplyWhereToGetCardsVisible()`）。

```csharp
        // ============================================================ 🆕 账 `商店:108`（B97_REMOTE1 补丁）
        // 🔶 自己设计（非原版）：`CardsSpecialConfig.whereToGetCardsData.expansionData.Length` 的**本地替身**。
        // · 这是什么：原版 `Initialize` 第 ⑥ 句 `getCardsButton.gameObject.SetActive(config.whereToGetCardsData
        //   .expansionData.Length != 0)` —— 一个**远端配置驱动的显隐**。
        // · 为什么必须自定：那份 config 的实例 SO 本机没有（`assets_full` 只有 MonoScript 类桩、`数据/` 无该表），
        //   内容由远端 Addressables 填 ⇒ 关服后永远拿不到。
        // · 谁定的：调度台 2026-10-29 裁定。出厂值 = 2（>0 ⇒ 显示；本地确实有 `WhereToGetCardsPopup` 可开）。
        public static int LocalWhereToGetCardsExpansionCount = 2;

        /// <summary>按 🔶 本地配置表算一次 `WhereToGetCardsVisible`（= 原版第 ⑥ 句），并出声说清这一格是本地模拟。</summary>
        public void ResolveWhereToGetCardsVisibleFromLocalConfig()
        {
            int n = LocalWhereToGetCardsExpansionCount;
            WhereToGetCardsVisible = n != 0;
            Debug.Log("[Gacha] `GachaEventTab.Initialize` 第 ⑥ 句：🔶 用**本地配置表**算（`expansionData.Length` = " + n
                    + " ⇒ 钮" + (n != 0 ? "显示" : "隐藏") + "）—— 原版读 `CardsSpecialConfig`（实例 SO 本机没有，远端下发）");
        }
```

**调用链自证**：原版 `Initialize` ⑥ 是 `Initialize` 的最后一句；我们 `BuildPage`（`:584`）末尾本来就调 `ApplyWhereToGetCardsVisible()`（`:645`）⇒ 在它**之前**插一行 `ResolveWhereToGetCardsVisibleFromLocalConfig()`，语义完全等价（先取值、再落地），且 `ApplyWhereToGetCardsVisible` 仍是**唯一**把 bool 变成 `SetActive` 的口（⛔ 不开第二条路）。

### P3 —— `商店:112`：🔶 本地 `ActionScheduler` 替身

**应插哪一节**：① 类尾追加替身类；② `EventTabSource` 补**一格字段** `ScheduledServerTime`（现有 `LiveOpsId` / `IsGachaEvent` / `CurrentOffer` 那一小节）；③ `SetEventTab` 的第 ④ 句那三行换成本补丁的写法。

```csharp
        // ============================================================ 🆕 账 `商店:112`（B97_REMOTE1 补丁）
        // 🔶 自己设计（非原版）：`ActionScheduler` 的**本地替身**。
        // · 原版 `GachaEventTab.SetEventTab` 第 ④ 句 = `ActionScheduler.Schedule(offer 的委托)` 存句柄（`Reset` 时 Release）；
        //   `ActionScheduler__Schedule.c:26-30` 现读 = `if (传入时刻 < TimeHelper.get_ServerTime()) return 0;`，
        //   到期打 `PlayfabWrapper.GenericCloudScriptHandler`（**云脚本**）。
        // · 为什么必须自定：服务端时钟与云脚本都在远端（已关服）⇒ 判据永远在远端。
        // · 谁定的：调度台 2026-10-29 裁定。⚠️ **到期只出声，⛔ 不假装打过云脚本**。
        public static class LocalActionScheduler
        {
            static readonly System.Collections.Generic.List<System.DateTime> _pending = new System.Collections.Generic.List<System.DateTime>();
            /// <summary>到点（本地时钟）⇒ 出声一次；返回句柄（0 = 原版那条「时刻已过 ⇒ 不排程」）。</summary>
            public static int Schedule(System.DateTime at, string what)
            {
                if (at <= System.DateTime.UtcNow) return 0;
                _pending.Add(at);
                Debug.Log("[Gacha] `ActionScheduler.Schedule`：🔶 本地排程（" + what + " @ " + at.ToString("u")
                        + "）—— 原版打云脚本 `GenericCloudScriptHandler`，本机已关服 ⇒ 到期**只出声**");
                return _pending.Count;
            }
            public static int PendingCount { get { return _pending.Count; } }
            public static void Release(int handle) { }
            public static void ResetForTest() { _pending.Clear(); }
        }
```

`EventTabSource` 加一格：

```csharp
            /// <summary>= 原版 `ActionScheduler.Schedule` 的**时刻实参**（服务端时间）。`null` = 拿不到（本地常态）。</summary>
            public System.DateTime? ScheduledServerTime;
```

`SetEventTab` 第 ④ 句改写成：

```csharp
            if (liveOps.CurrentOffer == null) return;                                 // ④ `CurrentOffer != null` 那道闸
            // ④ 原版 `ActionScheduler.Schedule(offer 的委托)` ⇒ 🔶 本地替身（见本文件类尾 `LocalActionScheduler`）
            if (!liveOps.ScheduledServerTime.HasValue)
            {
                _eventTabHandle = null;
                if (!_noSchedulerSaid)
                {
                    _noSchedulerSaid = true;
                    Debug.LogWarning("[Gacha] `GachaEventTab.SetEventTab` 第 ④ 句：`CurrentOffer` 非空，但**排程时刻拿不到**"
                                   + "（`EventTabSource.ScheduledServerTime` = null；原版那一格在远端 offer 上）"
                                   + " ⇒ 句柄位仍为 null，这一拍不排程（⛔ 不假装排过任务）");
                }
                return;
            }
            _eventTabHandle = LocalActionScheduler.Schedule(liveOps.ScheduledServerTime.Value, "GachaEventTab 的 offer 委托");
```

**调用链自证**：`SetEventTab`（`:1286`）④ 是唯一写 `_eventTabHandle` 的地方；`ResetEventTab` 的 ① 句 `_eventTabHandle = null` 是唯一读它的地方（原版 `ActionScheduler.Release(+0x68)`）⇒ 替身只在这两处出现，⛔ 没有第二个排程口。

---

## 五 待接线 / 没查清（给调度台）

| # | 事 | 谁做 | 依据 |
|---|---|---|---|
| 1 | **三条补丁转派**（`§四 P1/P2/P3` 落到 `Shell/GachaEventWindow.cs`）—— 落点文件现在归 `B97_W2` | 调度台 → `B97_W2`（或该文件的下一个写手） | 简报红线 + 文件所有权表 |
| 2 | **「限购售罄」要不要拦**（原版 `AvailableMax > 0 && Available == 0` 会拦，我们**仍未拦**） | 调度台裁 | ⚠️ 加它会改变既有页面的可买性：`Editor/ShopScene.cs` 里若有「买同一件两次」的断言会红。今天 `ShopTabPage.DoBuy` 在售罄时照样「成功」，与本批修掉的越界谎报**同一族**（本代理按「不自行发明闸」只记账、没动） |
| 3 | **`PurchasePremiumClick` 的关窗对位**要一次真机确认（`win + 0x60` ≈ `GameWindow.OnCloseWindow`） | 调度台（真 Play 时点一次「Get now!」→ 关掉付费窗 → 看面板有没有刷新） | 反编译只到「偏移 + nullary 委托」，字段名未确证（§二 商店:89 末段） |
| 4 | 断言接线（`Editor/ShopScene.cs:5876` 之后，见 §三） | 调度台 | 本代理不改 `RuleEngineTest.cs` |
| 5 | `商店:203` 的服务端失败支永远不可达 | 记账即可，⛔ 不判 D | 关服；客户端 UI 部分已照原版 |

### 没查清（如实记）

1. **`win + 0x60` 的字段名**：见 §二 `商店:89` 末段。已排除「类桩里有声明该字段的类」（`grep SourceDelegate` 全库只 4 个文件，都不是窗口类）。
2. **`GachaEventTab.Initialize` 第 ⑥ 句那个 config 的**真实取值**：实例 SO 本机没有 ⇒ 只能本地模拟（🔶），这也是 `商店:108` 一直维持 B 的原因。
3. **`EventTabSource.CurrentOffer` 的真实类型**：我们这一侧是 `object`（刻意不绑远端类型）⇒ 排程时刻只能由调用方补（P3 的 `ScheduledServerTime`）。⚠️ 今天**没有任何调用方**构造 `EventTabSource` 带 `CurrentOffer`，所以 P3 的替身在本地也是「可达但无人调」—— 与改前同一条可达性，⛔ 别据此判 D。
