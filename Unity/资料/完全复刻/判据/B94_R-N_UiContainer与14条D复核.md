# B94-R-N · `UiContainer<T>` 6 行 + 14 条「Q3 判 C → 主表 D」复核

- 会话：第九十四会话 · 波 1.5 · 只读取证代理 R-N
- 上游：`报告/B94_R-I.md` §七·4（UiContainer 6 行待裁）· §八·1（14 条只核了 2 条）
- 输入：`判据/B93_Q3_无落点220.tsv`（Q3 三档）· `账/_parts/B94_RI.tsv` · 各域主表
- 红线：⛔ 未改源码 · ⛔ 未改账/正本 · ⛔ 未跑 Unity · ⛔ 未动 git
- **D 档判据统一用 `调度台日志.md` §18·4（2026-10-28 定死）四类 (a)(b)(c)(d)**，全文引用在 §五·0

---

## 〇 一句话结论

| 活 | 结论 |
|---|---|
| **A · `UiContainer<T>` 6 行** | 注释原文说的是「**整族还是 C**」⇒ Q3 / `B91-Q2` 确实读反了。但**不是 6 行全该 C**：逐方法回源后 = **4 行应 C**（`Initialize(object)` / `Initialize(T)` / `OnDestroy` / `HandleClick` 都有真实原版体）+ **2 行 D 成立**（`OnInitialize(T)` = `abstract`；`OnClick()` = `RVA 0x4B33B0` 空桩 `ret 0`）—— 这 2 行是 §18·4**(a)** 的合法 D，与「整族是 C」不矛盾 |
| **B · 14 条** | **12 条主表 D 成立**（证据都独立复核过，见 §二）· **2 条 D 不成立**（`DynamicList<T>.ListType` / `GetIndexType`，现理由 = 「本仓没有实现这个泛型容器类」= §18·4 明文禁止的「我们没做」，且与 `B90_C4` 调度台裁定 #2「那 12 条判 A」直接冲突） |

---

## 一 · `UiContainer<T>` 族 6 行

### 1·1 注释原文逐句读（`Assets/CardPresentation/Shell/LiveOpsEvent.cs:1056-1060`，本轮现读行号）

```
/// <summary>原版 `IUiContainer`（`GetContainer()` 的返回类型）。
/// 🔶 自己设计（非原版）：`UiContainer` 整族（`IUiContainer` / `UiContainer<T>` / `ContainerBuilder<T>`）
/// 在我们仓库里**还是 C**（属别的批：*容器/窗口*线），本批的独占文件里没有它们 ⇒
/// 这里只留一个「拿到的是什么」的最小读口（`ComponentId` = `EventComponents` 的 GUID）。
/// ⛔ 别把它当掉 `IUiContainer` 的复刻。调度台 C1 2026-10-27 定。</summary>
```

逐句：

| 句 | 读法 |
|---|---|
| 「`UiContainer` 整族……在我们仓库里**还是 C**」 | **状态声明**：这一族当前记账状态 = **C**（= 该做、还没做）。**不是**「已用自设计口替代」 |
| 「（属别的批：**容器/窗口**线）」 | 归属：不在本批的独占文件里，属另一条线的账 |
| 「本批的独占文件里没有它们 ⇒ 这里只留一个……最小读口」 | **为什么本批只留接口壳**：不是「不复刻」，是「本批不碰」 |
| 「⛔ 别把它当掉 `IUiContainer` 的复刻」 | **显式警告**：这个最小读口**不算**复刻完成 |

⇒ **Q3 与更早的 `B91-Q2` 把这个「还是 C」读成了「C1 已裁定用自设计口替代 ⇒ D」**，方向读反。
`B94_R-I` §六·1 的结论（「主表 C 正确」）**对 `ContainerBuilder` 那 3 行成立**；但对 `UiContainer<T>` 这 6 行，
主表当时已经是 **D**（不是 C）—— R-I 没往下追到 6 行本身。

### 1·2 逐行（账 = `账/泛型与启动链_补读.tsv`，行号现读）

> ⚠️ 这 6 行与 `账/未分类_其他.tsv:714-721` 是**同一个原版类 `UiContainer` 的两本账**（方法/出处行号一一对应，见 §1·3）。
> 判法必须两本一致，⛔ 否则又是一次「账内口径分裂」。

| 账文件:行号 | 类.方法 | 现状态 | 现落点 | 注释怎么说 | 原版怎么做（回源） | 建议档位 | 证据 |
|---|---|---|---|---|---|---|---|
| `泛型与启动链_补读.tsv:612` | `UiContainer<T>.Initialize(object payload)` | **D** | `—（不复刻）` | 「整族还是 C」⇒ 应 C | **有真实体**（94 行）：① payload 不是 `T` ⇒ 拼一条带两个类型名的消息 + **throw Exception**；② `Content`(+0x38) 非空 ⇒ 在它的 `UnityEvent`(+0x100) 上 **RemoveListener 再 AddListener**（`OnEventClick`）；③ 写 `RawContent`(+0x30)；④ 末尾一次虚调用（slot `+0x1D8`） | **C** | `decomp_generic_trial/out/UiContainer_object___Initialize__1405600.c`（94 行）；ILSpy `UiContainer.cs:270`；另一本账 `未分类_其他.tsv:717` 同判据 |
| `泛型与启动链_补读.tsv:613` | `UiContainer<T>.Initialize(T e)` | **D** | `—（不复刻）` | 同上 | **有真实体**（12 行）：写字段 `+0x30 = e`，再转虚 slot `+0x1D8`（= 上面那一支） | **C** | `…/UiContainer_object___Initialize__1405de0.c`（12 行）；ILSpy `UiContainer.cs:362` |
| `泛型与启动链_补读.tsv:614` | `UiContainer<T>.OnDestroy()` | **D** | `—（不复刻）` | 同上 | **有真实体**（18 行）：取 `+0x30` 那个 `OnEventClick` 委托，非空 ⇒ `UnityEvent.RemoveListener`（退订） | **C** | `…/UiContainer_object___OnDestroy__1405e20.c`（18 行）；ILSpy `UiContainer.cs:369`（`protected virtual`） |
| `泛型与启动链_补读.tsv:615` | `UiContainer<T>.OnInitialize(T liveOps)` | **D** | `—（不复刻）` | 「整族还是 C」 | **`protected abstract void OnInitialize(T liveOps);`** —— 抽象声明，**无方法体**（子类 override 才有行为） | **D 成立**（§18·4**(a)** abstract） | ILSpy `UiContainer.cs:379` **原读逐字**；`decomp_generic_trial/out` 无独立产物（与此一致） |
| `泛型与启动链_补读.tsv:616` | `UiContainer<T>.HandleClick()` | **D** | `—（不复刻）` | 「整族还是 C」 | **有真实体**（17 行）：`+0x28` 那个回调非空 ⇒ `Invoke(this)`；然后转虚 slot `+0x1E8`（= `OnClick()`） | **C** | `…/UiContainer_object___HandleClick__1405540.c`（17 行）；ILSpy `UiContainer.cs:381/391` |
| `泛型与启动链_补读.tsv:617` | `UiContainer<T>.OnClick()` | **D** | `—（不复刻）` | 「整族还是 C」 | **空桩**：`RVA 0x4B33B0` = 全局共享空方法桩（机器码 `ret 0`，`_MANIFEST` 里 ICF 折叠到 `NetworkingPeer__OnMessage`） | **D 成立**（§18·4**(a)** 空桩） | `…/UiContainer_object___OnClick__4b33b0.c`（7 行，体只有 `return;`）；ILSpy `UiContainer.cs:394` = `protected virtual void OnClick() { }`；`out/_MANIFEST_generic_all.tsv` 两行 `4b33b0 … ICF_other_name` |

⇒ **净结论**：`状态列 4 改 C / 2 保持 D`，**`差异` 列 6 行全改**（现 6 行的差异开头都是
「🆕 2026-10-28 B91-Q2 **判 D**（类别 d）……调度台 C1 2026-10-27 裁定：`UiContainer` 整族用自设计口替代」= **读反的那句**，必须删）。

### 1·3 连带：`未分类_其他.tsv:714-721` 那 8 行（同族、同一份证据，必须同批改）

| 账行 | 方法 | 出处 | 现状态 | 原版体（回源） | 建议 |
|---|---|---|---|---|---|
| `未分类_其他.tsv:714` | `UiContainer.HandleClick()` | `UiContainer.cs:381` | D | 真实行为（同上表 616） | **C** |
| `未分类_其他.tsv:715` | `UiContainer.Initialize(T e)` | `UiContainer.cs:362` | D | 真实行为（同上表 613） | **C** |
| `未分类_其他.tsv:716` | `UiContainer.Initialize(object data)` | `UiContainer.cs:12` | D | `public abstract void Initialize(object data = null);` | **D 成立 (a)** |
| `未分类_其他.tsv:717` | `UiContainer.Initialize(object payload)` | `UiContainer.cs:270` | D | 真实行为（同上表 612） | **C** |
| `未分类_其他.tsv:718` | `UiContainer.OnClick()` | `UiContainer.cs:394` | D | 空桩（同 617） | **D 成立 (a)** |
| `未分类_其他.tsv:719` | `UiContainer.OnDestroy()` | `UiContainer.cs:369` | D | 真实行为（同 614） | **C** |
| `未分类_其他.tsv:720` | `UiContainer.OnInitialize(T liveOps)` | `UiContainer.cs:379` | D | `protected abstract` | **D 成立 (a)** |
| `未分类_其他.tsv:721` | `UiContainer.Refresh()` | `UiContainer.cs:14` | D | `public virtual void Refresh() { }` —— **空体** | **D 成立 (a)** |

⚠️ 这 8 行现在的 `差异` 全是同一句「断点=④ 抽象基类……**整个 LiveOps 容器族……本工程不建**」——
**对 714/715/717/719 这 4 行是错的理由**（「我们没做」不是 D 的理由，§18·4 ⛔ 明文）。

> 📌 这 8 行**不在**我这活的派工范围（活 A 只要 6 行）。但两本账是同一个原版类的两套说法，
> 只改一本 ⇒ 下一会话按另一本又是 D。**建议同批改**（或由调度台明确「以哪一本为准、另一本标历史」）。

---

## 二 · 14 条「Q3 判 C → 主表 D」复核

### 2·0 这 14 条是谁（口径先说清）

R-I 的冲突分布写的是 `Q3 C→主表 D` **14** 条，与 `B94_RI.tsv` 里 `状态 = D` 的 14 行**完全重合**
（`Counter(['A':110,'D':14,'C':6])` 现读）。逐条与 `B93_Q3_无落点220.tsv` 行号对上：

Q3 行 **26 · 27 · 32 · 33 · 42 · 86 · 87 · 93 · 184 · 185 · 197 · 199 · 200 · 212**（Q3 全判 C）。

### 2·1 一览

| # | 账文件:行号 | 类.方法 | Q3 | 主表 | **R-N 结论** |
|---|---|---|---|---|---|
| 1 | `其他_基础设施.tsv:544` | `PlayerDataManager.<RecordTutorialFinished>g__HandleSuccess\|464_0(OnlineMatchResult)` | C | D | **D 成立**（体 = 一句 Log；接线 = PlayFab 云回调 = P3） |
| 2 | `其他_基础设施.tsv:545` | `PlayerDataManager.<RecordTutorialPassed>g__HandleSuccess\|465_0()` | C | D | **D 成立**（同上；接线 = `PlayfabWrapper.GenericCloudScriptHandler`） |
| 3 | `商店.tsv:93` | `FixedDateDailyStoreItem.OnAfterWrite(JToken rawJson)` | C | D | **D 成立**（只往 store 配置自己的 JSON 写两格；玩家不可见；宿主 SO 本地 0 份） |
| 4 | `商店.tsv:106` | `GachaEventData.<get_Items>b__5_0(LoadableReference<ContainerOfferData>)` | C | D | **D 成立**（宿主 = 服务端事件 SO；本地 0 实例；我们无 `GachaEventData`/`ShopElement`） |
| 5 | `战斗逻辑驱动.tsv:484` | `BattleManager.ReplaceHeroPower(RawCardScript, bool, int)` | C | D | **D 成立**（§18·4**(c)** 四源全负 + 体只入队一条 `AutoAction(type 0x18)`） |
| 6 | `日常_奖励.tsv:749` | `TwoSidesEvent.PurchaseMilestone(int, Action<RewardInfo[]>, Action<string>)` | C | D | **D 成立**（IAP/货币 + 云脚本；用户边界②不做真实经济；本地无 rewards/里程碑数据） |
| 7 | `日常_奖励.tsv:751` | `TwoSidesEvent.TryColllect(Action<RewardInfo[]>, Action<string>)` | C | D | **D 成立**（体 = `vtable[0x568](currentMilestone(+0x34), …)` = 云脚本领奖） |
| 8 | `未分类_其他.tsv:211` | `CrappyDate.IsvalidDate()` | C | D | **D 成立**（四源 0 调用点；且我们存 `DateTime`，「0/0/0」这一态不存在） |
| 9 | `菜单_外壳.tsv:147` | `DynamicList<T>.GetIndexType(int index)` | C | D | 🔴 **D 不成立** —— 现理由 = 「本仓没有实现这个泛型容器类」= §18·4 禁止的「我们没做」；与 `B90_C4` 裁定 #2 冲突 ⇒ 建议 **A**（详见 §2·2·H） |
| 10 | `菜单_外壳.tsv:152` | `DynamicList<T>.ListType()` | C | D | 🔴 **D 不成立** —— 同上（两行必须同判） |
| 11 | `特效动画.tsv:498` | `BattleManager.StopTargetedSpellAnim()` | C | D | **D 成立**（体 = `Destroy(+0x370)`；该字段**唯一写点**在 `StartTargetedSpellAnim`，闸 = 卡 SO 的 PPtr，而 **1,351 张全 0** ⇒ 恒 `Destroy(null)`） |
| 12 | `菜单_外壳.tsv:44` | `BoostMenu.ShowAllBoosters()` | C | D | **D 成立**（§18·4**(b)**：全库 **0 实例**，见 §2·2·I 的三重否定） |
| 13 | `菜单_外壳.tsv:45` | `BoostMenu.UpdateAllPanelsTimers()` | C | D | **D 成立**（同上，同一个类） |
| 14 | `菜单_外壳.tsv:472` | `MenuSprites.GetWorldSprite(int playerTier)` | C | D | **D 成立**（§18·4**(b)**：类无实例 + `worlds[]` 本地 0 条 + 远端 722 MonoScript 清单无该类） |

### 2·2 逐条证据（只列**回源新读**的部分；R-I 已核过的直接标「R-I 已核」）

#### A · 544 / 545 `HandleSuccess` 闭包（P3 云回调）

- 体（**新读**）：`decomp_full/PlayerDataManager___RecordTutorialFinished_g__HandleSuccess_464_0.c` **全文 12 行**（含静态闸）= `CustomDebug__Log(DAT_1842c48b0); return;` ⇒ **零游戏状态、零副作用**。465 那份同体、同一个字符串常量。
- 接线（**新读逐字**）：`out_il2csharp/Assembly-CSharp/PlayerDataManager.cs:2982` = `action11 = new Action_1<OnlineMatchResult>(PlayerDataManager._RecordTutorialFinished_g__HandleSuccess_464_0);` → `:2984` `PathVisitor…Visit<BackgroundRepeat,Int32Enum>(1107, property21, action11)`（PlayFab 属性上送）；`:3013` = `Action action1 = new Action(..._465_0);` → `:3014` `PlayfabWrapper.GenericCloudScriptHandler(CloudHandle.onTutorialPassed, obj9, action1, action11)`（**云函数成功回调**）。
- ⇒ **D 成立**：两条腿都断（① 云后端 = P3，用户 2026-10-25 裁定 `PlayerDataManager` 带后端行 ⇒ D；② 即便触发也只打一行 debug 日志，**可观测行为为零**）。
- ⚠️ 主表 D 成立，但 Q3 的 C 也不是「乱判」—— Q3 只看到「体只有一句日志」，没写「接线在云回调」这一半。**主表更全**。

#### B · 93 `FixedDateDailyStoreItem.OnAfterWrite`（配置自写，玩家不可见）

- 体（**新读**，49 行）：`if (rawJson == null) return;` → 类型闸 → `item.Reference`（`LoadableReference.get_Reference`）非空 ⇒
  ① `rawJson["rarity"] = Reference.rarity`；② `rawJson[<Type.FullName 由虚表 +0x208/+0x210 取>] = <那个类型名>`。**0 处后端调用**。
- 宿主：`FixedDateDailyStoreItem` 是 `Everguild.LiveOps.DailyStoreData.fixedDateItems` 的元素，`DailyStoreData : ShopStoreData` = **store 配置**（不是玩家存档 DTO）。
- 本地数据（**新读 + 对照**）：`assets_full` 全库 grep `fixedDateItems` = **0 命中**；对照口径同库 `yCurveScaling` = **34 命中**（方法有效）⇒ 那份 store SO 本地确实 0 份。
- 我们这边（**新读**）：`ShopData.cs:529/541/570` 三处提到 `DailyStoreData.requiredItem`，**全是注释**；全仓无 `FixedDateDailyStoreItem` / `DailyStoreData` 类。
- ⇒ **D 成立**：玩家不可观测 + 宿主数据判据为空。
- ⚠️ **建议补一句**（§18·4(d) 要求写「接入条件」）：`接入条件：若将来复刻 store 配置 SO 的序列化层（或接 Newtonsoft），本条按 D→C 重开`。

#### C · 106 `GachaEventData.<get_Items>b__5_0`（服务端事件 SO 的投影）

- 体（**新读**，20 行）：`new ShopElement(LoadableReference<object>.op_Implicit(o), *(this+0x80), -1)`；ILSpy 逐字 = `(o) => new ShopElement(o, openCrateCost, -1)`。
- 母体（**新读**）：`GachaEventData__get_Items.c` = `Enumerable.Select(*(this+0x88), selector)`，即对 `gachaOffers`（`List<LoadableReference<ContainerOfferData>>`）投影。
- 本地数据（**新读 + 对照**）：`assets_full` 全库 grep `gachaOffers` / `openCrateCost` = **0 命中**（对照 `yCurveScaling` = 34）⇒ **本机没有一张 `GachaEventData` 实例**。
- 我们这边：全仓无 `GachaEventData`；`ShopElement` 只在 `Inventory.cs:601` 的一条注释里出现（无该类）。我们的 `GachaEventWindow` 是**按设计数据自建奖励格**，不走这条投影。
- ⇒ **D 成立**（数据侧在服务端 + 机制不同）。
- ⚠️ 建议在差异里补**逐字体**（现在只写「对列表取可加载项」，没写 `new ShopElement(…, openCrateCost, -1)`）+ 「接入条件：若远端 LiveOps 包落地 / 若复刻 `FixedStoreData.Items` 这一层」。

#### D · 484 `ReplaceHeroPower`（§18·4(c) 四源全负）

- 体（**新读**，141 行）：栈上拼一个 **0x208 字节**的 `AutoAction`（首格 `local_208 = 0x18` = 动作类型 24；载荷 = `newPower` / `isPlayer` / `priority`）⇒ 末尾**唯一一次真实调用** `BattleManager__AddAutoActionToQueue(param_1, local_108, param_4, 0)`。
- **四源调用点扫描（新读，全负）**：
  - `decomp_full`：`grep -rl ReplaceHeroPower` = **只有它自己的 .c**；
  - `out_cpp2il_dev_ilspy/BattleManager.cs`：只有定义；
  - `out_il2csharp/Assembly-CSharp/BattleManager.cs`：只有定义；
  - `assets_full` 全库 `*.json` grep `ReplaceHeroPower`（m_MethodName 面）= **0 命中**。
- R-I §六·5 另外核过「不是被 `ResolveAction` 91 路 switch 内联」：`BattleManager__ResolveAction.c` 里 `ReplaceHeroPower / 0x208 / heroPower` **0 命中**。
- ⇒ **D 成立**（§18·4**(c)**：有体但零调用路径，且**不是「休眠」** —— 它的输入是一张卡，但**没有任何数据能让它变成被调用**，因为生产点只有它自己）。
- ⛔ 边界（如实记）：本条判的是**方法**。若将来发现「动作类型 0x18」在别处另有生产点，那是**另一条账**（那个动作类型的可达性），不是把本条翻案。

#### E · 749 / 751 `TwoSidesEvent.PurchaseMilestone` / `TryColllect`

- 749（R-I 已核，本轮只复读标题句）：体 = 校验 rewards 可买（`Enumerable.All`）→ 读 `data+0x34` 当前里程碑 → 购买（IAP/货币）→ 云脚本回调。本地无 rewards/价格/里程碑数据；用户边界②「不做真实经济」。
- 751（**新读，18 行全文**）：`if (param_1[4] != 0) { vtable[0x568](param_1, *(int*)(param_1[4] + 0x34), onComplete, onError, …); return; }` = **一次云脚本领奖**，无本地状态。
- ⇒ 两条 **D 成立**（P3 云脚本 + 本地 0 数据 + 用户边界②）。

#### F · 211 `CrappyDate.IsvalidDate`（R-I 已核）

- 体（**新读，11 行**）：`return year(+0x18)!=0 && month(+0x14)!=0 && day(+0x10)!=0;`
- 四源 0 调用点（R-I 已核 + 本轮 `decomp_full` 复读：`grep -rl` 只有它自己）。
- 我们存 `DateTime`（`Shell/DailyData.cs`）⇒ 「0/0/0」这一态在本仓不存在。
- ⇒ **D 成立**（§18·4(c)）。

#### G · 498 `StopTargetedSpellAnim`（数据恒 null ⇒ `Destroy(null)`）

- 体（**新读，18 行**）：`UnityEngine_Object__Destroy(*(this + 0x370), 0);` —— **就这一句**。
- **`+0x370` 的全部写点（新读，全库）**：只有 `BattleManager__StartTargetedSpellAnim.c:75` 一处（`*(param_1+0x370) = uVar6`），而它的闸是 `*(lVar4 + 0x130) != null`（= 卡 SO 的 `targetedSpellAnim` PPtr）。
- **数据侧（本轮独立复读）**：`资料/普查产出_1018/allcards_1350/cards_raw.json`，1,351 张逐卡读 `full_raw.targetedSpellAnim` / `nonTargetedSpellAnim` 的 `m_PathID` ⇒ **非 0 条数 = 0**。
- **调用点（新读，共 3 处，与 Q3 说的「三个」一致但第 3 处是内联）**：`BattleManager__Update.c:794` · `:825` · `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1144`；另一处 `BattleManager__CancelActionStates.c:61-64` 是**同一段体被内联**（读 `+0x370` 直接 `Destroy`）。
- ⇒ **D 成立**，但**理由必须是「数据恒 null ⇒ 恒 no-op」**，⛔ **不是**「0 调用点」（它确实被调 3 处）。
- ⚠️ Q3 记「三个调用点 = Update / CancelActionStates / ResolvePlayCardFromHand」——**本质上对**（第 3 处是内联），但严格说 `CancelActionStates` 不是 `call`，是内联副本；引用时别写成「它有 3 个 CALL」。

#### H · 147 / 152 `DynamicList<T>.GetIndexType` / `ListType` —— 🔴 **D 不成立**

**原版体（新读，`decomp_generic_trial/out/`）**

| 方法 | 产物 | 体 |
|---|---|---|
| `ListType()` | `DynamicList_object___ListType__10175c0.c`（15 行） | `Type.GetTypeFromHandle(*(*(*(param_2+0x20)+0xc0)+0x30))` ⇒ **`typeof(T)`** |
| `GetIndexType(int index)` | `DynamicList_object___GetIndexType__18236d0.c`（19 行） | `list.get_Item(index)` 非空 ⇒ `System_Object__GetType(它)` ⇒ **`list[index].GetType()`** |

两者都是**真实行为**（不是空桩、不是抽象声明——`DynamicList<T>` 里是 `override`，ILSpy `DynamicList.cs:203/211`）。

**四源调用点（新读，全负）**

- `decomp_full`（25,096 个 `.c`）：`grep -rl ListType` / `GetIndexType` = **0 文件**；
- `out_cpp2il_dev_ilspy`：只有 `DynamicList.cs:203/211`（override）+ `:644/646`（abstract 声明）；
- `out_il2csharp`：只有 `DynamicList.cs:28/32` + `DynamicList_2.cs:7/8`；
- `assets_full`：`GetIndexType` = 0 命中。

**为什么「D 不成立」**

1. 主表现 `差异` 的**核心句是**「A 档要求落点能指到真实实现，而**本仓没有实现这个泛型容器类** ⇒ 记 D」
   —— 这**逐字命中** §18·4 的 ⛔「不判 D：**单纯『我们没做』**」。D 的定义是「**原版**可观测行为为零 / 原版那条路不可达」，
   不是「我们没写这个类」。
2. **与调度台自己的裁定直接冲突**：`判据/B90_C4_功能点C档施工单.md` §调度台裁定 **#2（2026-10-28）**：
   > 「`DynamicList<T>` 那 **12 条 → 判 A，不判 D**（裁决改向：C4 建议 D，调度台改判 A）。理由：D 的含义是『不复刻』，而我们**已经有等价物**（`List<T>`）且**行为一致**……把『有等价物且行为一致』记成 D，会让下一会话以为这块没做。」<br>
   > 「⚠️ 连带：……**同性质的要一起复核，同类就一起改 A** —— ⛔ 不许可种口径分裂」
   现读：同族 12 条里 **10 条已 A**（`泛型与启动链_补读.tsv:725/728-736`，落点统一 `Shell/LiveOpsEvent.cs:135（Triggers）`），
   **只有 `ListType` / `GetIndexType` 这 2 条还是 D** ⇒ 这正是那条警告说的「口径分裂」。
3. 同表 `备注` 与 `差异` **自相矛盾**：`未分类_其他`/`泛型` 两份的 `备注` 写「B90·C4A 2026-10-28：原判 D……**⇒ A**」，
   而 `差异` 写「【2026-10-28 调度台复核 · **状态回 D**】」—— 同一行两个相反结论，**必须砍掉一个**。

**原版这两格到底干什么**：`DynamicList<T>` 是「多态可序列化列表」（`private List<T> list;` + `ListType()` / `GetIndexType()` 供序列化层问「元素类型」）。
仓库里还有配套的 `ExampleUsage.cs` / `ExampleUsage2.cs`（作者自己写的用法示例，字段就是 `public DynamicList_1<A> myList;`）。
⇒ 它们**只服务「多态列表的序列化/属性抽屉」这一层**；Q3 的 [Q2] 记录也这么判（「只服务 Unity `[SerializeReference]` 多态表的自定义属性抽屉」），
而我们这批表是 **POCO/JSON**（`Shell/LiveOpsEvent.cs:135` 注释原文：`JsonUtility 序列化不了`）⇒ 运行时零消费。
⚠️ **但「只服务 SerializeReference 抽屉」这句是推断**：四源都**找不到调用者**，我也没找到「哪一个序列化器调用它」的直接证据。如实记在这里。

**建议（二选一，需调度台拍一次）**

- **首选 = A**（照 `B90_C4` 裁定 #2、与同族 10 条一致）：`落点 = Shell/LiveOpsEvent.cs:135（Triggers）`（同族 10 条就落这儿）；
  `差异` = 「等价物 = `List<T>`；原版这两格只是多态序列化层的类型自省口（四源 0 调用点），本工程这批表是 POCO/JSON、无该层 ⇒ 运行时零可观测 ⇒ 行为等价（非照建泛型类）。按 2026-10-28 调度台 `B90_C4` 裁定 #2 与同族 10 条同判」。
- **次选 = C**（若调度台坚持「A 必须有同形落点」）：那也是「休眠/该做没做」，**仍不是 D**。
- **⛔ 绝不能维持 D 的现理由**。

#### I · 44 / 45 `BoostMenu`（§18·4(b)：全库 0 实例）

本轮**独立复核**（不只引 B89-R3b）：

| 否定 | 本轮读数 |
|---|---|
| ① 类只有脚本桩 | `assets_full/globalgamemanagers/MonoScript/MonoScript_2818.json` 是**唯一**含 `"BoostMenu"` 的 json（全库 `grep -rl '"BoostMenu"'` = 1 命中） |
| ② 序列化指纹 0 实例 | `assets_full` 全库 grep `"AllBoostersPanel"` / `"BoosterPanelPrefab"` = **0 命中**；对照 `yCurveScaling` = **34** |
| ③ prefab/scene 0 挂接 | `assetripper_out/ExportedProject/Assets`（现读 1,389 个 `.prefab` + 17 个 `.unity`）里 `*.prefab / *.unity / *.asset / *.mat` grep 类 meta guid `ffefe14f5cabbd2f897a8f1c533c1759` = **0**；**同一条 grep 在 `Assets/Scripts/Assembly-CSharp/BoostMenu.cs.meta` 命中 1 次（对照组）⇒ 扫描生效** |
| ④ 调用点 | `decomp_full` 只有它自己 + 它自己的 lambda `BoostMenu.__c__DisplayClass7_0___ShowAllBoosters_b__0`；ilspy/il2csharp 只有 `BoostMenu.cs`；`assets_full` m_MethodName = 0 |

⇒ **D 成立**（原版这个类在本 build 里**根本没有活实例** ⇒ 那条路永不可达）。

#### J · 472 `MenuSprites.GetWorldSprite`（§18·4(b)：数据恒空）

- 体（**新读**，35 行）：`worlds = *(this+0x30)`；`playerTier < worlds.Length` ⇒ 返回 `worlds[playerTier]`；否则 `CustomDebug.LogError(<常量>)` + 返回 `worlds[len-1]`（越界再抛）。
- 否定（**新读 + 沿用 B90_C2 三重**）：
  ① `assets_full` 只有 `globalgamemanagers/MonoScript/MonoScript_1145.json`（`m_ClassName = MenuSprites`，`m_AssemblyName = Assembly-CSharp`）；本轮再 grep 指纹字段 `friendlyWorld` / `crateItemsCounterSprites` / `achievementsTier1` / `worldBGAnimations` = **0 命中**（对照 34）；
  ② 远端 `apk135/monoscripts.json` 的 **722 个 MonoScript 清单里没有 `MenuSprites`**（判据 `B90_C2_阻塞25条裁定表.md:17/53`）；
  ③ `assetripper_out` 里 `MenuSprites.cs.meta`（guid `2fb8f81dc28bccfcde208b639ab43b2d`）**只出现在 .meta 自己那一份**，
      `*.prefab/*.unity/*.asset/*.mat` 里 **0 处引用**（本轮实测；同批 grep 对 `BoostMenu` 的 guid 也在其 `.meta` 命中 1 次 = 对照组）。
- ⇒ **D 成立**（类无实例 + `worlds[]` 0 条 ⇒ 原版离线也没有这份数据）。
- ⚠️ 差异列现写「时间限定：本 build / 2025-07-30 资源快照」**已够**；建议把「远端 722 MonoScript 清单无该类」这条**加进差异**（现在只在 B90_C2 的判据文件里，账里没写）。

---

## 三 · 可直接改账的清单

> 写入纪律：账 `.tsv` 一律**裸 `split("\t")`**（⛔ 别用 `csv` 模块）；改完**逐列往返比对**。
> 下面只给 `状态` / `我们的落点` / `差异` 三格；`判据出处` / `判据要点` / `等级` / `轮次` / `备注` **不动**（除非另行注明）。

### 3·1 必改：`UiContainer<T>` 6 行（`账/泛型与启动链_补读.tsv`）

| 键 | 状态 | 我们的落点 | 差异（建议新值，整体替换旧的「B91-Q2 判 D 类别 d」段） |
|---|---|---|---|
| `612` `UiContainer<T>.Initialize(object payload)` | **C** | `—（未实现 · 属「容器/窗口」线）` | `❌ 2026-10-29 B94-R-N 推翻 B91-Q2 的 D：`Shell/LiveOpsEvent.cs:1057-1060` 原文是「UiContainer 整族**还是 C**（属别的批）……⛔ 别把它当掉 IUiContainer 的复刻」⇒ 不是「已自设计替代」。原版体（`decomp_generic_trial/out/UiContainer_object___Initialize__1405600.c` 94 行 / ILSpy `UiContainer.cs:270`）= payload 不是 T 就拼类型名并 throw → Content(+0x38) 非空则在它的 UnityEvent(+0x100) 先 RemoveListener 再 AddListener → 写 RawContent(+0x30) → 转虚 slot +0x1D8 ⇒ **有真实行为，属该做没做**。时间限定：本 build` |
| `613` `UiContainer<T>.Initialize(T e)` | **C** | `—（未实现 · 属「容器/窗口」线）` | `❌ 2026-10-29 B94-R-N 推翻 B91-Q2 的 D：同 612（注释原文 = 整族还是 C）。原版体（`…__Initialize__1405de0.c` 12 行 / ILSpy `UiContainer.cs:362`）= 写字段 +0x30 = e → 转虚 slot +0x1D8。⚠️ 旧「判据出处 = UiContainer.cs:270」不准，真行号 **:362**` |
| `614` `UiContainer<T>.OnDestroy()` | **C** | `—（未实现 · 属「容器/窗口」线）` | `❌ 2026-10-29 B94-R-N 推翻 B91-Q2 的 D：同 612。原版体（`…__OnDestroy__1405e20.c` 18 行 / ILSpy `UiContainer.cs:369` `protected virtual`）= 取 +0x30 的 OnEventClick 委托，非空 ⇒ UnityEvent.RemoveListener（退订）` |
| `615` `UiContainer<T>.OnInitialize(T liveOps)` | **D（保持）** | `—（不复刻）` | `🆕 2026-10-29 B94-R-N 换理由（(a) abstract，非「类别 d」）：ILSpy `UiContainer.cs:379` 逐字 = `protected abstract void OnInitialize(T liveOps);` ⇒ 抽象声明无体，行为在子类 override。⛔ 别再写「decomp_generic_trial 无独立产物 ⇒ 基类空/虚」（那只是导出名单没请求）。时间限定：本 build` |
| `616` `UiContainer<T>.HandleClick()` | **C** | `—（未实现 · 属「容器/窗口」线）` | `❌ 2026-10-29 B94-R-N 推翻 B91-Q2 的 D：同 612。原版体（`…__HandleClick__1405540.c` 17 行 / ILSpy `UiContainer.cs:381`）= +0x28 回调非空 ⇒ Invoke(this)；再转虚 slot +0x1E8（= OnClick）` |
| `617` `UiContainer<T>.OnClick()` | **D（保持）** | `—（不复刻）` | `🆕 2026-10-29 B94-R-N 复核维持 (a)：RVA **0x4B33B0** = 全局共享空方法桩（`…__OnClick__4b33b0.c` 体只有 `return;`；`out/_MANIFEST_generic_all.tsv` 两行标 `ICF_other_name → NetworkingPeer__OnMessage`）；ILSpy `UiContainer.cs:394` = `protected virtual void OnClick() { }`。时间限定：本 build` |

### 3·2 必改：`DynamicList<T>` 2 行（**两本账都要改**）

| 键 | 状态 | 我们的落点 | 差异 |
|---|---|---|---|
| `账/菜单_外壳.tsv:147` `DynamicList<T>.GetIndexType(int index)` | **A**（次选 C） | `CardPresentation/Shell/LiveOpsEvent.cs:135（Triggers）` | `🆕 2026-10-29 B94-R-N：**D → A**。两条理由：① 原 D 的核心句「本仓没有实现这个泛型容器类」= 调度台 §18·4 ⛔ 明文禁止的「我们没做」，D 只留给「原版可观测为零 / 原版那条路不可达」；② 调度台 `B90_C4` 裁定 #2（2026-10-28）已把 `DynamicList<T>` 这 12 条**改判 A**（等价物 = List<T>、行为一致），同族 10 条现读已是 A，只这 2 条还是 D = 该裁定明令禁止的「口径分裂」。原版体（`decomp_generic_trial/out/DynamicList_object___GetIndexType__18236d0.c`）= `list[index].GetType()`，四源 0 调用点（decomp_full / ilspy / il2csharp / assets_full m_MethodName）⇒ 只服务多态序列化层，本工程这批表是 POCO/JSON（`LiveOpsEvent.cs:135` 注释「JsonUtility 序列化不了」）⇒ 运行时零可观测` |
| `账/菜单_外壳.tsv:152` `DynamicList<T>.ListType()` | **A**（次选 C） | `CardPresentation/Shell/LiveOpsEvent.cs:135（Triggers）` | `同 :147（必须同判）：原版体 = `typeof(T)`（`…__ListType__10175c0.c`）。另：现「备注」写「B90·C4A 改 A」而「差异」写「状态回 D」—— **同一行两个相反结论**，本轮一并清掉` |
| **连带** `账/泛型与启动链_补读.tsv:726` / `:727` | 同上 | 同上 | 这两行是**同一个方法**在泛型表的第二本账（`726 = ListType` / `727 = GetIndexType`），⛔ 不改就会「一本 A 一本 D」 |

### 3·3 可选（不改状态，只补证据/修旧理由）

| 键 | 改哪格 | 建议 |
|---|---|---|
| `账/商店.tsv:93` | 差异（追加一句） | `接入条件（§18·4(d)）：若将来复刻 store 配置 SO 的序列化层（或接 Newtonsoft），本条按 D→C 重开`；并把「那份 store SO 本地 0 份」的**对照口径**写进去（`fixedDateItems` 0 命中 / `yCurveScaling` 34 命中） |
| `账/商店.tsv:106` | 差异（补逐字体） | 现在只写「对列表取可加载项」⇒ 补 **`new ShopElement(o, openCrateCost, -1)`** 与母体 `Enumerable.Select(gachaOffers, …)`；并补 `本地 0 实例（gachaOffers / openCrateCost 各 0 命中，对照 yCurveScaling = 34）` + `接入条件` |
| `账/特效动画.tsv:498` | 差异（补两句） | ① `+0x370` 的**唯一写点** = `BattleManager__StartTargetedSpellAnim.c:75`（闸 = 卡 SO `+0x130` 非空，1,351 张全 0 ⇒ 恒 `Destroy(null)`）—— 本轮独立复读 `cards_raw.json` 复现；② 三个调用点写清「2 个真 call（Update:794/825、ResolvePlayCardFromHand:1144）+ 1 个**内联副本**（`CancelActionStates.c:61-64`）」，⛔ 别写成「3 个 CALL」 |
| `账/战斗逻辑驱动.tsv:484` | 差异（把「两树」升成四源） | 现在写「`decomp_full` 与 `cpp2il dev` 两棵树……0 外部调用」⇒ 补上 `out_il2csharp` 与 `assets_full m_MethodName` 也 0，凑齐 §18·4(c) 的四源；并把体写清「栈上拼 0x208 字节 AutoAction（类型 0x18）⇒ `AddAutoActionToQueue`」 |
| `账/菜单_外壳.tsv:472` | 差异（补一条否定） | 补 `远端 apk135 的 722 MonoScript 清单无 MenuSprites`（现在这条只在 `B90_C2_阻塞25条裁定表.md:17/53`，账里没写） |
| `账/其他_基础设施.tsv:544/545` | 不改 | D 成立，证据已经比 Q3 全（含云回调接线） |
| `账/日常_奖励.tsv:749/751` · `账/未分类_其他.tsv:211` · `账/菜单_外壳.tsv:44/45` | 不改 | D 成立 |

### 3·4 同批建议（不在本活范围，但与 3·1 是同一条证据链）

`账/未分类_其他.tsv:714 / 715 / 717 / 719` → **C**；`:716 / 718 / 720 / 721` → **D 保持（(a)）**（详见 §1·3 表）。
理由同 3·1：同一个原版类 `UiContainer` 的**第二本账**，只改一本 ⇒ 下一会话按另一本又是 D。

---

## 四 · 判不了 / 差什么

| # | 差什么 | 影响的结论 |
|---|---|---|
| 1 | **`UiContainer<T>` 的虚槽 `+0x1D8` 到底解析到 `Initialize(object)` 还是 `OnInitialize(T)`** —— 两条体的尾部都是这一跳（`Initialize(T)` 的 ILSpy 体是空壳 + `NoteDecompilerIssue("Indirect call: +1D8")`，解不出）。查法：`dump.cs` 该类的方法表 slot 顺序，或 VA 级反汇编 `sub_1814057d7` 附近 | 只影响「体怎么描述」，**不影响档位**（两格都是真实行为 ⇒ C） |
| 2 | `DynamicList.ListType/GetIndexType` 的**调用者身份** —— 四源 0 命中，所以「只服务 SerializeReference 属性抽屉」是从「方法用途」推出来的，**没有直接证据**。查法：`D:/2/tools/assetripper_out/ExportedProject/Assets/Scripts` 里找 `PropertyDrawer` / `ISerializationCallbackReceiver`；或 `out_il2csharp` 全树 `grep "DynamicList"` 逐个看（本轮只看了文件名） | 只影响 §2·2·H 的**措辞**（A 还是 C 的选择），**D 一定不成立** |
| 3 | 14 条里 **484 `ReplaceHeroPower`** 的「动作类型 0x18」是否另有生产点（别的 ability 直接入队同类型动作） | 只影响「那个**动作**可不可达」，**不影响本条（方法）判 D** |
| 4 | `UiContainer` 族的「**别的批是谁**」—— 注释只写「*容器/窗口*线」。现读证据指向 `账/未分类_其他.tsv`（`ContainerBuilder` / `ContainerHolder` / `ContainerService` / `ReferralContainer` / `UiContainer` 都在这本） | 只影响**归属/轮次**列，不影响状态（C） |

---

## 五 · 顺手发现（⛔ 都没改，只报）

**0 · D 档的统一口径原文（本轮所有 D 判定都按这一版）** —— `调度台日志.md` §18·4（2026-10-28 定死）：
> **D = 原版可观测行为为零，或原版那条路在本 build 永不可达。** 四类：(a) 空桩 / `return` 常量 / `abstract`（先查 `RVA 0x4B33B0`、`0x4BD440`）· (b) 原版数据恒 0 / 表恒空（含 prefab 全库 0 实例）· (c) **四源全负**的死代码（有体但零调用路径）· (d) 架构性不适用（必须写 `🔶 机制不同 + 接入条件`）。
> **⛔ 不判 D**：单纯「我们没做」；「0 调用点」但原版体有真实行为**且数据一变就可达**（= 休眠，保持 C）。

1. **`B91-Q2` 的误读范围 = 正好这 6 行，没有更大**（本轮查证，纠正一个容易想当然的猜法）：
   全账 grep `LiveOpsEvent.cs:1057` / `调度台 C1 2026-10-27` ⇒ **只有 `泛型与启动链_补读.tsv:612` 一行显式引用**，
   613-616 写「同上」继承它。⇒ 原文「整族还是 C」这句**没有被搬去解释别的类**。
   ⚠️ 但另有 9 行（`:577/578` `ConfigProvider<T>` · `:600` `MultiKeyDictionary` · `:605/606` `ReferenceList<T>` · 其余同表）
   共用同一句**通用模板**「接入条件：若将来把 UI 改成 prefab 实例化 / 接 Addressables·PlayFab TitleData / 接调试控制台」——
   那是 B91-Q2 那批的**一句话兜底**，对 `ConfigProvider`（TitleData）与 `ReferenceList`（Addressables）勉强对得上，
   但「UI 改成 prefab 实例化」那一支与它们无关 ⇒ **理由链偏空**，建议下一轮各换成自己真实的 (d) 证据（⛔ 不改状态，只改措辞）。
2. ⚠️ **`泛型与启动链_补读.tsv` 的行号漂移在这 2 条上又是 2 个不同值**：Q3 记 `760/761` ⇒ 主表实际 `:726/:727`；
   而 `菜单_外壳.tsv` 那本账是 `:147/:152`。**同一方法三套行号** ⇒ 引用一律按「类 + 方法」认，⛔ 别按行号。
3. ⚠️ **`菜单_外壳.tsv:147/152` 的 `备注` 与 `差异` 自相矛盾**（一个说 A、一个说「状态回 D」）—— 这是「同一行两套说法」的教科书例子，
   改的时候必须**同时清掉备注里那半句**，否则下一会话还会读反一次。
4. ℹ️ **`FixedDateDailyStoreItem` / `ContainerBuilder` 在 `assets_full` 里连 MonoScript 桩都没有**（`globalgamemanagers/MonoScript/` 4,453 份里 0 命中）——
   因为它们是 `[Serializable]` 普通类 / 静态工具类，本来就不需要 MonoScript。⇒ **「没有 MonoScript」不能当「类不存在」的证据**（与「按类名搜不到 ≠ 本地没有」同族）。
5. ℹ️ **本轮新增一条可复用的否定判据**：`assets_full` 全库 `grep -rl <字段名> --include=*.json`，
   **对照组用 `yCurveScaling`（应 = 34）**。本轮用它一次性否掉 4 组（`gachaOffers` / `openCrateCost` / `fixedDateItems` / `AllBoostersPanel|BoosterPanelPrefab` / `friendlyWorld|crateItemsCounterSprites|achievementsTier1|worldBGAnimations`）。
