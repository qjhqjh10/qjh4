# B97_SHOP · 商店 16 条 B 清零报告

> 执行代理 `B97_SHOP` · 2026-10-29 · 第九十七会话波 1
> 分诊依据 = `资料/完全复刻/判据/B96_商店轮抽个人32_分诊.md` §一（16 条）
> 账补丁 = `账/_parts/B97_SHOP.tsv`（16 行 · 11 列 · 首行表头 · `check_patch_conflicts.py` 无冲突）
> 断言 = `MyGame/Assets/CardPresentation/Editor/B97_SHOP_Assert.cs`（A/B/C/D 四组 22 条）
> `typecheck` = **0 / 0**（`TMPDIR=/tmp/wf_shop`；本批自己的三次改动各跑过一次，全 0/0）
> ⚠️ 收尾时再跑出现报错，**全部落在别人正在写的文件上**（`DeckRuntime.cs` 12 条 · `Editor/B97_MISC2_Assert.cs` 2 条），我的三个文件（`MainMenuRuntime.cs` / `ShopScene.cs` / `B97_SHOP_Assert.cs`）**一条都没有** —— 按 CLAUDE.md 13·3②「错误全部集中在某个不是你负责的文件上 ⇒ 那不是你的问题」处置。

---

## 〇 一句话结果

**16 条 = 6 条判 A（37/38/96/98/101/162）+ 10 条有反证地留 B**（结构差 2 · 等 Unity 导入 1 · REMOTE 7）。
本批**真写的代码**只有一处新接线（`商店:162` 的调用方），另有 1 个新断言文件 + 1 行宿主接线。

| 档 | 条数 | 账行 |
|---|---|---|
| **判 A（本批转档）** | 6 | `商店:37` `38` `96` `98` `101` `162` |
| **留 B · 结构差（2026-10-28 已裁「保持 B、⛔ 不建空壳」）** | 2 | `商店:12` `13` |
| **留 B · 只剩一次 Unity 导入（调度台）** | 1 | `商店:95` |
| **留 B · REMOTE（服务端数据，本代理按红线不自行改档）** | 7 | `商店:89` `91` `92` `102` `108` `112` `203` |

---

## 一 本批**改了什么代码**

| # | 文件 | 改动 | 行号策略 |
|---|---|---|---|
| 1 | `CardPresentation/Shell/MainMenuRuntime.cs` | **`BuildModeCardForCard` 末尾追加一跳** `BindMainMenuOfferContainer(...)`（账 `商店:162` 的调用方） | **同一行内追加 ⇒ 行号零漂移**（先例 `GachaEventWindow.cs:858` 的「同一行 = 不加行号」）。账在本文件上**落在 :4066 之后**的引用有 10 个不同行号（`:4153` `:4290` `:4301` `:4378` `:4403` `:4429` `:4492` `:4504` `:4518` `:4718`）—— 本批一个字节都没挪它们（`check_ledger_points` 现跑：**漂移 0 · 失败 0**） |
| 2 | 同上 · **类尾** | 新增 `IsOfferContainerPrefab`(:4803) · `BindMainMenuOfferContainer`(:4812) · `BuildOfferDrawerForMenuCard`(:4846) · `LastOfferContainerForTest`(:4800) · `_offerNoDataSaid` | **类尾追加**（4793 → 4882 行，**+89 全在最后一个成员之后**）⇒ 前面所有落点零漂移 |
| 3 | `CardPresentation/Editor/ShopScene.cs` | 接 2 行断言调用（`B97SHOPAssert.Run()` + 两条 `_sink` 累加） | 接在 `ShopFamilyAssertB94S1` 那两行**之后**（`:5870` 附近）。⚠️ 该文件**无任何**账落点引用 > 5700 的行（现查）⇒ 加行安全 |
| 4 | **新建** `CardPresentation/Editor/B97_SHOP_Assert.cs` | A/B/C/D 四组 | 新文件 |

⚠️ **没碰**：`Editor/RuleEngineTest.cs` · 账主表 · 正本 · `Pinned*` · `gen_cards_engine.py` · git · 所有权表里别人名下的任何文件。

---

## 二 逐条（16 条，一条不落）

### `商店:12` `BaseOfferPopup.OnDisable()` — **留 B（结构差）· 零代码**
- **判据现读**：`decomp_full/BaseOfferPopup__OnDisable.c:14-27` = `priceDisplayButton(+0xa0) == 0 ⇒ return`（**本方法没有 LogError**，LogError 在 `OnEnable` 那侧）→ `m_OnClick.RemoveListener(ProcessPurchase)`。
- **我们这一侧**：价签钮是**建树时**交给 `MenuDraw.Hit` 的**闭包**（`Shell/BaseOfferPopup.cs:1012`），**没有订阅可摘**（闭包随节点销毁一起没）。
- **反证（为什么不是漏做）**：原版是 `OnEnable` 挂 / `OnDisable` 摘的 **UnityEvent 订阅**；我们是「建一次闭包」。**等价口径已写死 = 「建树时挂一次」**（`商店:13` 的落点注释）。⇒ 无代码可做，**卡在宿主机制差异**。
- **⛔ 不判 D**：2026-10-28 已裁「架构性不适用 ⇒ 保持 B、不建空壳」（`账/商店.tsv` 该行备注原文）。⚠️ 顺手纠一句旧账：「本仓零 `UnityEvent`」是 overclaim —— `Shell/` 另有 2 处真 `Button.onClick`（`WindowHeaderWithBackButton.cs:90` / `EventBadge.cs:113`），本行已按「本类的按钮」收窄。

### `商店:13` `BaseOfferPopup.OnEnable()` — **留 B（结构差）· 零代码**
- **判据现读**：`BaseOfferPopup__OnEnable.c:16-35`：`op_Equality(priceDisplayButton, null)` ⇒ `LogError("Offer popup missing priceDisplayButton")` + return；否则 `m_OnClick.AddListener(ProcessPurchase)`。
- **能对位的两件都已落地**：① 点价签 → `ProcessPurchase`（`:1012` 的 `MenuDraw.Hit` 闭包）；② 缺节点 → 同一条 `LogError` 且**不挂命中区**（`:1011`；读口 `:1197 PriceDisplayButtonPresent`）。
- **只差「订阅」这半** ⇒ 与 `商店:12` 同一族，维持 B。

### `商店:37` `CampaignPremiumPointsPanel.CollectButtonClicked()` — **✅ B→A**
- **判据**：`CampaignPremiumPointsPanel__CollectButtonClicked.c:1-43` 三档。
- **我们的落点（现读行号）**：`Shell/CampaignTab.cs:1768 CollectButtonClicked` · `:1786 WindowsManager.OpenPurchasePremiumWindow(army)` · `:1901 CampaignArmyOfSelected` · `Shell/WindowsManager.cs:1263 OpenPurchasePremiumWindow`。
- **逐档**：state0 = 订阅方委托（`OnCollectSubscriber` 注入位 + 出声）· state1 = `OpenWindow(prefab+0x40, boxed army)`（**prefab 引用已证**：GUID `0a2c3f30…` → `AssetBundle_1.json` 的 `m_Container` → PathID `-7633618066679408583` → `RectTransform_108940773697247289` → GO `Purchase Premium Window`；boxed 值 = `LiveOpsEventData.get_Army`，vtable **Slot 8**）· state2 = 整段 return（`LastCollectBranch == -1`）。
- **为什么 A**：唯一残余是「state 1 今天走不到」，那是 `LocalPremiumState()` 的 🔶 取值（**属 `商店:38`**），不是本方法缺行为；state0 的订阅方在服务端链上 ⇒ 用注入位 + 出声，与 `商店:44`「判据由调用方给」同一口径。⛔ **不据此判 D**（客户端 UI + 服务端数据的 **UI 部分**不许判 D）。
- **断言**：`Editor/ShopFamilyAssertE13.cs` E1–E7（已接线，今天可跑）。

### `商店:38` `CampaignPremiumPointsPanel.Initialize(...)` — **✅ B→A**
- **判据**：`CampaignPremiumPointsPanel__Initialize.c:1-52` + `CampaignPointDrawer__DrawForArmy.c:1-52`。
- **六句逐句**：① `currentCampaign`(+0xb0) ↔ `CampaignData.Selected`（本仓等价物）· ② `pointDrawer.DrawForArmy(campaign.Data.Army)` ↔ `:1435` 传 `CampaignArmyOfSelected()` · ③ `pointsText = pointReward.ToString()` ↔ `:1426` · ④ `TimerDisplay.Initialize(timeToNextCollect)` ↔ `:1500`（`null` ⇒ 出声 + 稳定显示 `--`）· ⑤ `onCollect`(+0xa0) ↔ `OnCollectSubscriber` · ⑥ `ChangeState(collected ? 2 : (IsPremium ? 0 : 1))` ↔ `ApplyPremiumState(LocalPremiumState())`。
- **两处 🔶（服务端值，允许自设计门类）**：`PremiumPointsForTest` 默认 200 · `LocalPremiumState()` 恒 2。依据 = `CLAUDE.md` §18·0「唯一允许自己设计的门类 = 服务器相关（关服后判据在远端）」。
- **本批新增断言**：`B97_SHOP_Assert` **C1–C3** —— `ItemDrawer.DrawCampaignPointsReward(node, box, 200, 10, st)` ⇒ `res.Art == "40k_DeckSelection_icon_FactionUM"`（= Ultramarines 徽记，⛔ 不是出厂那张 `40K_genearl_icon_Campaign_points_big`）；`army = 0 (Neutral)` ⇒ `Art == null`（原版把 `Image` 关掉、整格不画）。

### `商店:89` `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick()` — **留 B · REMOTE 反证**
- 窗引用来自 `ExpansionPassData.PremiumOfferData`（服务端下发的 premium 报价资产）；`grep assets_full` = **0 文件**。
- 已逐句落地 + 出声（`Shell/ExpansionPassEventWindow.cs:2685`，告警带事件 id，照原版第 ⑤ 句 `LogWarning(id)` 形状）。
- ⇒ **原版调用链断在这儿**：③④ 的输入（`GetHandler → GetEvent → GetComponentReference(0xc)`）全在远端 ⇒ 本地打不开窗。**按简报红线不自行改档**，建议调度台按 §四 / §18·0 批量裁。

### `商店:91` `…<PurchasePremiumClick>b__12_0()` — **留 B · REMOTE 反证**
- 回调体只有一句 `Initialize(this.data(+0x58), this.onClose(+0x60))`；只能挂在那扇**远端窗**的 `+0xc` 委托上 ⇒ 窗口打不开时连 `Delegate.Combine` 都到不了。
- 落点已订正为「**无对应物**」（旧 `:2442` 实读是 `StartPurchasePremiumPanel`，与本法无关）。⛔ 不造空壳凑落点（方案 §4.1 明令）。

### `商店:92` `…<PurchasePremiumClick>b__12_1()` — **留 B · REMOTE 反证**
- 同上；回调体 = 一句间接调用。旧落点 `:2442` 同样指错，已订正为「无对应物」。

### `商店:95` `GachaCratePanel.Awake()` — **留 B · 只剩「一次 Unity 导入」**
- 五句全落地（`Shell/GachaEventWindow.cs:1352 BuildCrateVideo` · `:1372 OnCrateVideoStarted`）+ `renderMode = CameraNearPlane`（`B97_W2` 已补）。
- **本批现读的新事实**：`Assets/CardPresentation/Resources/Art/videos/gacha_crate_opening.mp4` **已在盘上**（5,141,522 B · 头 12 字节 `00 00 00 18 66 74 79 70 6D 70 34 32` = `ftypmp42`），**但缺 `.meta` ⇒ Unity 尚未导入** ⇒ `Resources.Load` 仍拿不到 ⇒ `started` 不触发。
- **卡在哪**：**一次 Unity 导入**（`Unity 批处理只有调度台能跑`）。⛔ 别走 ffmpeg、⛔ 别改名成 `.webm`（内容是标准 MP4，改名会让 Unity 用 VP8 解码器去解）。
- **本批断言**：`B97_SHOP_Assert` **A1–A7** —— 资源名常量 / 文件在盘 / **没有同名 `.webm`** / `ftyp` / `mp42` / 5,141,522 B。

### `商店:96` `GachaCratePanel.CrateAnimationFinished()` — **✅ B→A**
- 三档可步进状态机逐句一致：`CrateFlowBegin`(:1452) / `CrateFlowStep`(:1462)，`state0 = clip.length × 0.85` · `state1 = 等 crateOpenned` · `state2 = 恢复输入 + Stop + 关背景 + RefreshVisuals + Collect`。
- 可达性前置 = `商店:95` 的导入 —— 那**只影响 state0 的等待长度**，不影响三档结构；E13 D7–D9 今天就能跑（无 clip 时那一档等待为 0）。

### `商店:98` `GachaCratePanel.OpenCrateButtonClick()` — **✅ B→A**
- ①②③④ 全落地（`:1398`）；⑤ 今天必失败（无 `CurrentOffer`）⇒ 我们**收摊并返回 false**（原版会无限等 —— **有意偏离，已写死**）；⑥ 只在成功时起；注入 `CratePurchaseHook`（`:1666`）时整条链可驱动。
- 断言 E13 D2–D6 今天可跑（不谎报走完 / 输入必须还回去 / 背景关回去）。

### `商店:101` `GachaCratePanel.<Awake>b__9_0(VideoPlayer)` — **✅ B→A**
- cue 已解出（`Gacha vault open`，clip `Vault Crate Open Full`）并**每次 `started` 都播**（`:1372`；原版不判 clip 有无 ⇒ 我们⛔不 gate）。
- **本批新增断言**（`B97_SHOP_Assert` **B1–B5**，**不依赖 clip 导入，今天就是绿的**）：反射直调 `OnCrateVideoStarted` —— 第 1 次 ⇒ `WindowSounds.Played = 1` 且 `LastCue = Gacha vault open`；第 2 次 ⇒ `Throttled = 1`（证明**每次都真的调了 `PlayFx`**；若加「只播一次」的闸 ⇒ B5 红，而 B4 看不出来）。

### `商店:102` `…<OpenCrateButtonClick>b__14_0(...)` — **留 B · REMOTE 反证**
- 回包回调两句按原版顺序落地（`Shell/GachaEventWindow.cs:1672 OnCratePurchaseComplete`）+ 注入位 `CratePurchaseHook`（= 原版 `CurrentOffer.TryPurchaseOffer(onComplete,…)` 那一跳）。
- **接线点已在** ⇒ 一旦有回包就走原版链；**生产路径恒不触发**（无服务器回包 = 本地不存在）。

### `商店:108` `GachaEventTab.Initialize()` — **留 B · REMOTE 反证**
- ⑥ 的 `CardsSpecialConfig` **本地 0 命中**（`assets_full` 只有 MonoScript 类桩、`数据/` 无该表）⇒ 那个 bool 是远端真值 ⇒ 已按 🔶 默认 `true` 落地 + 出声（`Shell/GachaEventWindow.cs:1640 ApplyWhereToGetCardsVisible`，断言 `Editor/MainMenuScene.cs:17785` 钉着 `DataNote` 点名 `CardsSpecialConfig`）。

### `商店:112` `GachaEventTab.SetEventTab(ILiveOps liveOps)` — **留 B · REMOTE 反证**
- ①②③ 全部落地（`:1286`）；④ `ActionScheduler.Schedule` —— **`.c` 现读** `ActionScheduler__Schedule.c:26-30`：`lVar5 = TimeHelper__get_ServerTime(0); if (param_2 < lVar5) return 0;` ⇒ **服务端时钟** + 到期打 `PlayfabWrapper.GenericCloudScriptHandler`（云脚本）。
- ⇒ 现做法（句柄位恒 null + 出声一次、⛔ 不假装排过任务）**是对的**，不需要新建调度器。⚠️ 账里「本仓没有 ActionScheduler」这句容易被读成「我们漏建了一层」，本批已在备注里改成「服务端时钟 + 云脚本」口径。

### `商店:162` `MainMenuOfferContainer.OnInitialize(LiveOpsEvent)` — **✅ B→A（本批接了调用方）**
- **缺的就是「全仓唯一调用方 = 自检」这一跳**，本批补上：
  - 原版链：`MainMenuWindow.InstantiateLiveOp` → `ContainerBuilder<T>.MakeContainer(data.Event, 35, …)` 之后宿主喂 `OnInitialize(data.Event)`。
  - 我们的等价落点：`BuildModeCardForCard` 已算出的 `owner`（=`ModeCardOwnerEvent`）⇒ **卡根名落在 `Main Menu Offer Container` 族**时才挂组件 + 喂一次。
- **报价格容器的本地实例（本批现查，按 `m_Script.m_PathID = -6437201350575876930` 反查）**：`bundle_menus_assets_all/GameObject/` 里 **5 份** ——
  `Main Menu Offer Container 1x1` · `… 1x1 Premium_booster_title_avatarOrResource` · `… Carousel 1x1` · `… Static Image 1x1` · `… Static Image 1x2`。
  ⚠️ 这 5 个名字**不在文件名以外的任何地方**（类名既不等于文件名、也不在 JSON 里）⇒ 只能按脚本反查（CLAUDE.md 2026-10-28 那条「⛔ 不能按名字搜」）。
- **闸（两态都钉了）**：非报价格卡 ⇒ 不挂；手挑那 5 张卡（`owner == null`）⇒ 不挂。
- **数据侧如实记**：报价 item（`ShopOfferBase.Items`）在远端 ⇒ `OfferEventData.Base` 传 `null`，由 `MainMenuOfferDrawer.Initialize(null)` 那条**既有**告警出声（⛔ 不编假报价）。
- **新断言**：`B97_SHOP_Assert` **D1–D10**。
- ⚠️ **调用链的可达性仍为 0**（本机 13 份活动 SO 日期全在 2025 年；且 `MainMenuContainerPrefabByGuid` 只解出 1 条容器 GUID = `Base Game Mode Container 1x2 Raid or Army Release`）—— 那是**数据**不可达，不是代码缺，⛔ 别据此判 D。

### `商店:203` `PurchasePremiumWindow.TryPurchase(...)` — **留 B · REMOTE 反证**
- `onFailPurchase` 恒不触发 = 两因叠加：① 原版失败支的来源是**服务端回包**；② 我们照**用户 2026-09-17 边界②「不做真实经济」**，`Shell/ShopData.cs:472 Buy` **不扣钱、不判余额**。
- ⛔ **别自己加本地余额闸**（那会让原版恒成功的一支变成可能失败）。

---

## 三 待接线 / 卡在哪（给调度台）

| # | 事 | 谁做 | 依据 |
|---|---|---|---|
| 1 | **Unity 导入** `Assets/CardPresentation/Resources/Art/videos/gacha_crate_opening.mp4`（现无 `.meta`） | **调度台**（Unity 批处理独占） | 解锁 `商店:95`，并让 96/98/101 的**可视/可听**面真跑起来 |
| 2 | 7 条 REMOTE（`89/91/92/102/108/112/203`）批量裁定 | **调度台** | 简报红线：REMOTE 不自行发明、不改档；建议按 §四 / §18·0 统一裁 |
| 3 | 2 条结构差（`12/13`）维持 B | 已裁（2026-10-28） | ⛔ 不建空壳 |
| 4 | 跑一次 `ShopScene.Run` 确认 D 组（`AddComponent<MainMenuCarouselDrawer>` 在编辑器里能挂） | 调度台 | 见下节风险 |

---

## 四 没查清 / 风险

1. 🔴 **`AddComponent<MainMenuCarouselDrawer>()` 未实测**。该类在 `MainMenuOfferContainer.cs` 里（**类名 ≠ 文件名 ⇒ 没有脚本资产**）。CLAUDE.md 的口径是「没有脚本资产 ⇒ 能挂」（先例：`ShopFamilyAssertE13` 的 `TestDrawer` 同样没有脚本资产、已绿）。若实测被拒，D5/D8/D9 会红 —— 修法 = 把它拆成同名文件（与 A1506/A1508 那次同一条路）。
2. ⚠️ **`BuildOfferDrawerForMenuCard` 的 `SlotBox` 是 🔶 我们挑的**（卡内 1 个正方形槽、以卡原点为 0）—— 原版是 `contentAnchor` + `UI_InfiniteScroll`（那份源码本地没有）。已在代码里打 `🔶 自己设计（非原版）` 并写清「谁定的 + 日期」。**今天不可达**（见 `商店:162` 的可达性说明）。
3. ⚠️ **`商店:37/38` 判 A 用的是「服务端值走 🔶 + UI 部分逐句对齐」这条口径**（与 `商店:36`/`商店:44` 的既有判法一致）。若调度台要求更严（「🔶 未落地实测就不许判 A」），把这两行退回 B 即可 —— **证据都写在本报告第二节**，不需要重查。
4. 本批**没有跑 Unity**（红线），所以 A/B/C/D 四组断言**只过了 `typecheck`，没跑过**。C 组依赖 `CardArt.MenuUi` 的三级兜底（`ui_menu/ → ui_deck/ → ui/`）拿到 `40k_DeckSelection_icon_FactionUM`（文件现读在 `Resources/Art/ui_deck/`）。

---

## 五 断言接线位置（已接）

`CardPresentation/Editor/ShopScene.cs`，接在：

```csharp
ShopFamilyAssertB94S1.Run();
_sink.Pass += ShopFamilyAssertB94S1.Pass; _sink.Fail += ShopFamilyAssertB94S1.Fail;
```

**之后**：

```csharp
B97SHOPAssert.Run();
_sink.Pass += B97SHOPAssert.Pass; _sink.Fail += B97SHOPAssert.Fail;
```

⚠️ `Editor/RuleEngineTest.cs` **一个字没碰**（红线）。
