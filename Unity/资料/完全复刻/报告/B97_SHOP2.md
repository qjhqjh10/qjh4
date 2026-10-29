# B97_SHOP2 · 商店 —— 三条 REMOTE 落地（102/108/112）+ 限购售罄闸

> 执行代理 `B97_SHOP2` · 2026-10-29（第九十七会话 · 波 2）
> 判据来源 = `资料/完全复刻/报告/B97_REMOTE1.md` **§四 P1/P2/P3**（完整可粘贴方案 + 应插哪一节 + 调用链自证）
> 口径 = **CLAUDE.md §18·0**：「需要拍板的地方默认 = 复刻原版；**唯一例外 = 与服务器有关**（关服，判据在远端）」
> 账补丁 = `账/_parts/B97_SHOP2.tsv`（**5 行** · 11 列 · 首行表头 · `merge --dry --only B97_SHOP2` = **更新 5 / 新增 0 / 无变化 0** ｜ `check_patch_conflicts.py` = 无冲突）
> 断言 = `MyGame/Assets/CardPresentation/Editor/B97_SHOP2_Assert.cs`（A / G / D / E 四组，共 **60 条**：A 25 · G 16 · D 6 · E 13）
> `typecheck`（`TMPDIR=/tmp/wf_shop2`）= **0 / 0**（最终一次读数；本批改完每一轮都跑过）

---

## 〇 一句话结果

**四件全落地**：`商店:102` / `108` / `112` **B → A**（全部按 §18·0 走「客户端 UI 照原版 + 数据本地模拟 + 出声 + `🔶` 打标」）；
**限购售罄闸**按调度台 2026-10-29 裁定「要做」补上（落 `商店:297`）。
另外**顺手修掉一处静默卡死**（见 §一 第 5 条，它不是可选项 —— 接了 102 之后不修就是「点了开箱，输入再也回不来」）。

| 账行 | 原版方法 | 前 → 后 | 落点 |
|---|---|---|---|
| `商店:102` | `GachaCratePanel.<OpenCrateButtonClick>b__14_0(…)` | **B → A** | `Shell/GachaEventWindow.cs:1672` + 类尾 `InstallLocalCratePurchaseHook` / `LocalCrateRewards` + `:645`（`BuildPage` 每建即装） |
| `商店:108` | `GachaEventTab.Initialize()` | **B → A** | `:645`（`ResolveWhereToGetCardsVisibleFromLocalConfig` → `ApplyWhereToGetCardsVisible`）+ 类尾 `LocalWhereToGetCardsExpansionCount` |
| `商店:112` | `GachaEventTab.SetEventTab(ILiveOps)` | **B → A** | `:1286`（第 ④ 句）+ `:1273`（`EventTabSource.ScheduledServerTime`）+ `:1309`（`ResetEventTab` → `Release`）+ 类尾 `LocalActionScheduler` |
| `商店:297` | `ShopStore.TryPurchase(…)` | A（**补闸**） | `Shell/ShopData.cs:472`（`Buy`，闸在 `:475`）+ `Shell/ShopTabPage.cs:714`（`Buy` 第一句）+ `:764`（`DoBuy` 分家） |
| `商店:98` | `GachaCratePanel.OpenCrateButtonClick()` | A（**补 ⑥ 的 `StartCoroutine`**） | `Shell/GachaEventWindow.cs:1398`（⑥ 那一行，同一行内追加） |

---

## 一 本批**改了什么代码**（5 个文件）

| # | 文件 | 改动 | 行号策略（🔴 账在本文件上有 20+ 落点） |
|---|---|---|---|
| 1 | `Shell/ShopData.cs` | `Buy` 加售罄闸（**同一行内**追加 `else if (SoldOut(...)) return BuySoldOut;`）；`PurchaseSucceeded` 认两个哨兵；过期注释就地订正；**类尾**追加 `SoldOut()` + `BuySoldOut` + 判据注释块 | 714 → **748**；`Buy` 仍 `:472`、`BuyNotFound` 仍 `:708`（账里本文件最大落点 = `:708`）⇒ **零漂移**（机检现跑：本文件无漂移/失败行） |
| 2 | `Shell/ShopTabPage.cs` | `Buy` 第一句加售罄闸（**两句合占一行**）；`DoBuy` 的失败分流加一档（**同一行**）；**类尾**追加 `SoldOutRefusals` / `ResetSoldOutForTest` / `RefuseSoldOut` / `OnPurchaseSoldOut` | 1293 → **1345**；`:714`/`:753`/`:764`/`:772` 等锚点**逐个复核未动** |
| 3 | `Shell/GachaEventWindow.cs` | ① `:645` 三句合一行（`Resolve…` + `Apply…` + `InstallLocalCratePurchaseHook`）② `EventTabSource` 补 `ScheduledServerTime`（**与 `CurrentOffer` 同一行**）③ `SetEventTab` 第 ④ 句整段重写（**6 行 → 6 行**）④ `ResetEventTab` 走 `Release`（**同一行**）⑤ `OpenCrateButtonClick` ⑥ 补 `StartCoroutine`（**同一行**）⑥ 三处过期注释就地订正 ⑦ **类尾**追加 P1/P2/P3 全部成员 | 1678 → **1790**（新增全在类尾）；账里 21 个锚点（`:358 :584 :639 :645 :803 :1065 :1171 :1204 :1286 :1309 :1325 :1352 :1372 :1398 :1425 :1436 :1452 :1462 :1548 :1569 :1672`）**逐个复核，全部未动** |
| 4 | **新建** `CardPresentation/Editor/B97_SHOP2_Assert.cs` | A/G/D/E 四组 38 条 | 新文件 |
| 5 | `CardPresentation/Editor/B94B5_Assert.cs` | **就地改写** SectionB 的 B2–B5（原口径已过期，见 §五） | 断言文件，账里**无任何** `文件:行` 引用（现查） |

⚠️ **没碰**：`Editor/RuleEngineTest.cs` · `Editor/ShopScene.cs`（接线建议见 §四，**由调度台接**）· 账主表 · 正本 · `Pinned*` · `gen_cards_engine.py` · git · 所有权表里别人名下的文件（`RuleCore*` / `EffectResolver*` / `UnitState` / `Enchantment` / `TutorialScript*` / `SimpleAI` / `BattleContext` / `CampaignScriptRunner` / `TransformUnit_B94B12` / `RankedTab` / `FactionScore` / `MainMenuRuntime` / `Alliance*` / `CollectionWindow` / `MilestoneScoreBar` / `SkirmishEventWindow` / `EnergySinglePlayerOnlyEventWindow` / `B97_*_Assert.cs`）。

### 🔴 第 5 条那处「顺手修掉的静默卡死」（必须记）

`OpenCrateButtonClick` 的第 ⑥ 句原文只写了 `CrateFlowBegin();`，而 **`CrateAnimationFinished()` 全仓 0 调用方**（现读 `grep`）⇒ 状态机**没有任何东西驱动**。
改前无害（第 ⑤ 句恒失败，走的是「收摊 + 把输入还回去」那一支）；**本批把 ⑤ 接上之后**，点了开箱就会：第 ④ 句 `PointerLayer.SetInputEnabled(false)` → ⑥ 起状态机 → **没人推它** ⇒ 输入永远回不来（**玩家点不动任何东西，且零报错**）。
⇒ 按原版补上 `StartCoroutine(CrateAnimationFinished())`，并加 `Application.isPlaying` 门（批处理没有帧循环，同 `RefreshVisuals` 那只口径）。
`商店:98` 的差异列已写全。断言侧不受影响（自检一直用 `CrateFlowStep` 显式驱动）。

---

## 二 逐条（四件，一条不落）

### ① P1 · `商店:102` `GachaCratePanel.<OpenCrateButtonClick>b__14_0(IReadOnlyList<RewardInfo>)` — **B → A**

- **原版**：这句是**服务器购买回包**的回调，两句 `crateRewards = rewards` → `crateOpenned = true`（`GachaCratePanel___OpenCrateButtonClick_b__14_0.c` 全文）。回调由 `GachaEvent.CurrentOffer.TryPurchaseOffer(onComplete, null, showRewardPopup:false)` 触发。
- **本批落的**：`InstallLocalCratePurchaseHook(bool replace = false)`（= 原版那一跳的 **🔶 本地替身**：取本地奖励表 → **出声**说清「不是服务器回包」→ 调 `onComplete`）；`LocalCrateRewards`（箱 id → 奖励）+ `AnyCrateId = "*"` 通配档 + `LocalCrateRewardFor`（查不到 id 退通配、连通配都没有 ⇒ **空表 + 出声**）。
- **接线**：`BuildPage`（`:645`）每建一次树就装一次（`replace:false` ⇒ **不覆盖**自检注入的 hook）。
- **为什么不是「假装开箱」**：奖励来自**声明过的本地表**、每次开箱都打一条 `[Gacha] 开箱：🔶 **本地模拟**…`；出厂表是**空**的（本地没有箱的真实 id 表）⇒ 默认发空表 + `LogWarning`，⛔ 不编假奖励。
- **调用链自证**：`OpenCrateButtonClick()`（`:1398`）⑤ → `TryPurchaseCrate()`（`:1425`，一句 `hook(OnCratePurchaseComplete)`）→ 本 hook → `OnCratePurchaseComplete`（`:1672`，两句逐字）→ `CrateFlowBegin()` → `CrateFlowStep` state1 的 `if (!_crateOpened) return false;` 现在能过 → state2。**没有新增任何一跳**，只把「远端那一跳」换成「本地那一跳」。
- **断言**：G1–G10（空表出声 / 返回空数组不返回 null / 通配回退 / 精确 id 优先 / `replace:false` 不覆盖 / 整条链走完 + `crateOpened` / 状态机到终点 / 背景关回去）。

### ② P2 · `商店:108` `GachaEventTab.Initialize()` — **B → A**

- **原版第 ⑥ 句**：`getCardsButton.gameObject.SetActive(ConfigManager.GetConfig<CardsSpecialConfig>().whereToGetCardsData.expansionData.Length != 0)`。
- **本批落的**：`LocalWhereToGetCardsExpansionCount`（🔶 本地配置表，出厂 **2**）+ `ResolveWhereToGetCardsVisibleFromLocalConfig()`（按它算 `WhereToGetCardsVisible` 并出声）。
- **接线**：`BuildPage` 末尾 `Resolve…(); Apply…;`（同一行）—— **取值**与**落地**分成两个口，落地仍然只有 `ApplyWhereToGetCardsVisible` 一处（⛔ 不在别处再写一条 `SetActive`）。
- **为什么必须 🔶**：实例 SO 本机没有（`assets_full` 只有 MonoScript 类桩、`数据/` 无该表），内容由远端 Addressables 填 ⇒ 关服后永远拿不到。
- **断言**：D1–D5（出厂 2 / 两态 0↔2 / `Build()` 之后那颗钮真的跟着关/开）。

### ③ P3 · `商店:112` `GachaEventTab.SetEventTab(ILiveOps)` — **B → A**

- **原版第 ④ 句**：`CurrentOffer != null` ⇒ `ActionScheduler.Schedule(offer 的委托)` 存句柄（`Reset` 时 `Release`）；`ActionScheduler__Schedule.c:26-30` 现读 = `if (传入时刻 < TimeHelper.get_ServerTime()) return 0;`，到期打 `PlayfabWrapper.GenericCloudScriptHandler`（**云脚本**）。
- **本批落的**：`LocalActionScheduler`（类尾）——`Schedule(DateTime at, string what)`（`at <= DateTime.UtcNow` ⇒ 返回 **0**，= 原版那条「时刻已过」；否则入表、出声、回句柄）/ `PendingCount` / `Release(handle)`（出声，⛔ 不静默删）/ `ResetForTest()`。
- **`EventTabSource` 补一格** `ScheduledServerTime`（`DateTime?`）—— 这才是「句柄恒 null」的真正原因：原来的 `EventTabSource` 只有 `CurrentOffer`（`object`），**拿不到时刻**。
- **两态**：时刻拿不到 ⇒ 句柄仍 null + **出声一次**（⛔ 不假装排过任务）；拿得到 ⇒ 真排。
- **断言**：E1–E8（过去时刻⇒0 / 将来⇒排上 / 时刻 null⇒出声且句柄 null / 时刻有⇒句柄非 null / `ResetEventTab` 清句柄 / 非 Gacha 事件早退）。

### ④ 限购售罄闸（账 `商店:297`，调度台 2026-10-29 裁定 = **要做**）

- **判据（第一权威 = 反编译方法体）** `decomp_full/CatalogItemContainer__Refresh.c:45-51`：
  `iVar5 = CollectionExtensions.GetValueOrDefault(<已购数表>, key)`；`if (*(int*)(lVar7 + 0x20) < 1) bVar12 = true; else bVar12 = iVar5 < *(int*)(lVar7 + 0x20);`（`+0x20` = 限购上限）；
  `:65-77`：`if (((bVar12 & bVar3) == 0) || (cVar2 != '\0')) uVar4 = 0;` → `Selectable.set_interactable(uVar4)` · `PriceDisplayButton.set_Interactable(uVar4)`。
  ⇒ **售罄（已购 ≥ 上限且上限 > 0）时，原版把整格按钮设成不可点** —— 连 `MenuShop/WaitingPopup` 都不弹。
- **我们的等价判据**：`ShopData.SoldOut(page, i)` = `AvailableMax > 0 && Available <= 0`（我们存的是**剩余**次数，`Buy` 每买一次 `--`）。
- **落两处**：`ShopData.Buy` 回新哨兵 `BuySoldOut`（数据层真值）；`ShopTabPage.Buy` 的**第一句**拒绝（早于传奇确认、早于等待框 —— 原版那道闸就在点击之前）；`DoBuy` 里再挡一次（`OnPurchaseSoldOut`）。
- **⛔ 不弹错误窗**：原版售罄**没有** `OnPurchasedFailed`（错误窗只属于服务端回包失败）⇒ 售罄走「出声 + 不记账」，与「越界」（`BuyNotFound` → `InternetUnreachable` 错误窗）**分开**。
- **断言**：A1–A16b。其中 **A3 是灭自证那条**：`AvailableMax = 0`（= 无限购信息）**不是**售罄 —— 把 `SoldOut` 写成 `Available <= 0` 就会把 0/0 那三件全判成售罄。
- **⚠️ 仍存的差异（已写进账）**：原版售罄格的**按钮不可点且变灰**；我们的 `MenuDraw.Hit` 是建树时挂的闭包 ⇒ **点得到**，由 `RefuseSoldOut` 拒绝 + 出声。变灰那一档要 prefab 的 `m_DisabledColor`（`PriceDisplayButton__set_Interactable.c` 只做 `Selectable.set_interactable` + 一个 `Behaviour.enabled = value`，颜色由 prefab 给，**那个值尚未取**）⇒ 待接线，⛔ 没编一个灰色。
- **另两格刻意不加**：`bVar3`（货币够不够 —— 与用户 2026-09-17 边界②「不做真实经济」冲突，原版那一支恒成功）、`cVar2`（已拥有 —— 我们的商品表是可重复购买的）。

---

## 三 交付读数

### `typecheck`（`TMPDIR=/tmp/wf_shop2`）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 本批改完每一轮都跑过（含最后一次）。
- ⚠️ 本批**没跑 Unity**（红线：Unity 批处理只有调度台能跑）⇒ A/G/D/E 四组断言**只过了 typecheck，没跑过**。

### 账补丁

```
python -I -X utf8 工具/merge_ledger_parts.py --dry --only B97_SHOP2
  [dry] 会写 商店.tsv
  合计：更新 5 · 新增 0 · 无变化 0
python -I -X utf8 工具/check_patch_conflicts.py
  ✅ 没有跨补丁重复键（1 份补丁）
```

- 键 = `(原版类, 方法)` **逐字照抄主表** ⇒ 5 条都是**就地更新**、没有新行。
- 落点机检：`python -I -X utf8 工具/check_ledger_points.py` **现跑**，输出里**没有任何一行**指向 `ShopData.cs` / `ShopTabPage.cs` / `GachaEventWindow.cs`（= 本批零漂移、零失败；全表另有的漂移/失败都是别的文件的既存项）。

### 断言 —— **建议接线位置**（⛔ 本批不改 `Editor/ShopScene.cs`）

`MyGame/Assets/CardPresentation/Editor/ShopScene.cs`，**接在 `:5878` 之后**（那一行现在收尾的是 `B97REMOTE1Assert.Run();`）：

```csharp
        // 🆕 2026-10-29（`B97_SHOP2`）：限购售罄闸（商店:297）+ 三条 REMOTE 的本地模拟（102/108/112）。
        B97SHOP2Assert.Run(); _sink.Pass += B97SHOP2Assert.Pass; _sink.Fail += B97SHOP2Assert.Fail;
```

⚠️ 位置理由：A 组会**写盘**（`ShopData.Buy` 的 `PersistShop`）但全程走 `OverrideDir` 临时目录，收尾 `DeleteStoreForTest()` + `ResetForTest()` 后才关门；
`ShopScene` 本来就在 `:2048-2071` 那一节做过同样的事（同一个 `OverrideDir` 套路），是已验证的路径。
G 组会裸 `AddComponent<GachaEventWindow>()` + `Build()`（`B93S1` / `B94S1` 已在同一宿主里这么干过）；D 组同理。

---

## 四 被改动 / 更新了哪些既有断言（点名）

| 文件 | 断言 | 为什么必须改 |
|---|---|---|
| `Editor/B94B5_Assert.cs` | **B2**（`CratePurchaseHook == null` 那条「前提」） | `BuildPage` 现在装默认 hook ⇒ 旧断言必红。改成 ★ 断言「装上了」 |
| 同上 | **B3 / B4 / B5** | 旧口径 = 「生产路径 hook 恒 null ⇒ `OpenCrateButtonClick` 返回 false」。现在改成**先显式置 `CratePurchaseHook = null` 造出那一档**（于是 B3–B5 仍然验「没有 `CurrentOffer` ⇒ 收摊」），语义没丢 |
| 同上 | **新增 B5b / B5c / B5d** | 两态的另一半：重新装回默认 hook ⇒ 返回 true、`crateOpened` 同步变真 |
| 同上（文件头「灭自证」节） | 加一段 | 写清「删掉 `BuildPage` 那个 `InstallLocalCratePurchaseHook()` ⇒ B2 红」/「默认 hook 只出声不回调 ⇒ B5c/B5d 红」 |

**已经核对过、确认【不需要改】的既有断言**（逐条现读）：
- `Editor/B94B5_Assert.cs` A 组（`CampaignTab` 越界早退）—— 与商店购买链无关。
- `Editor/ShopFamilyAssertE13.cs` D 组 —— `D2` 在 `Build()` **之前**调 `OpenCrateButtonClick()`（那时还没装 hook）⇒ 仍是 false，D2–D9 全部保持。
- `Editor/ShopFamilyAssertB93S1.cs` A 组 / `Editor/B97_W2_Assert.cs` C 组 —— 只 `Build()` 后看视觉，不碰购买链。
- `Editor/ShopFamilyAssertB94S1.cs` A1–A6 —— 默认本地表 = 2 ⇒ `WhereToGetCardsVisible` 仍是 `true`，A3/A4 不动。
- 🔴 **`Editor/ShopScene.cs` 里「买同一件两次」的用例：`grep` 现读 = 一个都没有** —— 全文件 9 处 `.Buy(` 逐条核过：
  `:2089 pg.Buy(2)`（传奇件，**没买**，只弹确认框）· `:2106 pg.Buy(0)`（`_cards[0]` 1/5 ⇒ 买一次，**只此一次**；之前 `:2053` 那次在 `OverrideDir` 临时目录里、`:2070 ResetForTest()` 已复位）·
  `:2144 pgD439.Buy(0)`（页 2 `Daily Gold Cache` 1/1 ⇒ 只买一次）· `:2212` / `:2844` / `:3295`（全是 `_cards[3]` = 0/0 **无限购**）· `:2241 Buy(99)`（越界）· `:2276`（页 3 `_items[0]` = 0/0）· `Shell/BoosterInfoPopup.cs:665`（自检里没有购买用例）。
  ⇒ **限购售罄闸不会打红任何既有断言**（这一条是派活时点名要查的）。

---

## 五 待接线 / 没查清（给调度台）

| # | 事 | 谁做 | 依据 |
|---|---|---|---|
| 1 | **断言接线**（`Editor/ShopScene.cs:5878` 之后两行，见 §三） | 调度台 | 本批不改 `ShopScene.cs` / `RuleEngineTest.cs` |
| 2 | **售罄格的「不可点 + 变灰」视觉** | 待派（`Shell/ShopTabPage.cs` 的 `BuildCell`） | `PriceDisplayButton__set_Interactable.c` 只做 `Selectable.set_interactable(value)` + 一个 `Behaviour.enabled = value`；**颜色来自 prefab 的 `m_DisabledColor`，那个值尚未取** ⇒ 取到值之前 ⛔ 不编一个灰 |
| 3 | `商店:102` 的**真实奖励表**永远拿不到（远端 LiveOps） | 记账即可，⛔ 不判 D | 关服；客户端 UI + 出声那一半已照原版 |
| 4 | HTTP 真机验证：点一次开箱（现在会真的走完 state0→2） | 调度台（真 Play） | 本批只过 typecheck，没跑 Unity |

### 没查清（如实记）

1. **`m_DisabledColor` 的真值**（§五 第 2 条）—— 需要读 prefab（`Button` 组件的序列化字段）；本轮没取，所以视觉那半没做。
2. **`EventTabSource.CurrentOffer` 的真实类型**：我们这一侧仍是 `object`（刻意不绑远端类型）⇒ 排程时刻只能由调用方补（`ScheduledServerTime`）。⚠️ 今天**没有任何调用方**构造带 `CurrentOffer` 的 `EventTabSource` ⇒ E5/E6 那两条替身路径**可达但生产无人调**，与改前同一条可达性，⛔ 别据此判 D。
3. **`LocalCrateRewards` 的箱 id**：本地没有箱的真实 id 表 ⇒ 出厂只有通配档这条路（且出厂是空的）。真值在远端 `GachaEvent.CurrentContainer`。
