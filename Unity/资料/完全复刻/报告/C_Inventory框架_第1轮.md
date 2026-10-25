# C · `Inventory<T>` 框架层 · 第 1 轮

> 2026-10-25 · 执行代理 C。判据 = `资料/完全复刻/判据/泛型与启动链.md` §三 #3（G 给的最小可用接口）+ 逐方法表
> （原版 `Inventory.cs:685-1748` · `ItemInfo.cs:213-252` · `InventorySubset.cs:112-281`）。
> `TMPDIR=/tmp/wf_c bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。⛔ 未跑 Unity / 未动 git。
> ⛔ 没碰 `RuleEngine/**`、没碰活动线文件。

---

## ① 结论（数字）

**框架三件已落地**（`Shell/Inventory.cs`，新文件）：`Inventory<T>` · `ItemInfo<T>` · `InventorySubset<T>` + 存档 `InventoryStore` + 注册表 `Inventories`。
**`ShopData._owned` 已迁进去**，那份临时落盘（`WarpforgeShop/owned.json` + 它自己的 DTO）**已删** —— 现在是 `WarpforgeInventory/shop.json`，由框架统一存。

| 账 | 变化 | 变化后 |
|---|---|---|
| `收藏.tsv` | **`Inventory` 14 → A · `InventorySubset` 4 → A · `Inventory.OnFinishUnpack` → D**（共 19 行）· `InventoryManager` 24 行保持 C（备注改写） | A**131** / B59 / **C61** / D**100** |
| `商店.tsv` | `ShopOfferInventory` 2 → **D**（解包队列，机制不同） | A114 / B5 / **C52** / D**171** |

⇒ 「`Inventory` 族 C」从 **60 行 → 41 行**（收藏 51→32，商店 9→7）。三本账仍 11 列 / E=0 / LF 未翻。

---

## ② 框架层做了什么（逐方法对位）

| 原版 | 我们的 | 备注 |
|---|---|---|
| `Inventory<T>.Add(item, amount, instanceId, customData, overrideAmount)` | `Inventory<T>.Add(T, int, string, bool)` | `customData`（原版给条目挂的自定义字典）**暂无调用点** ⇒ 不做空壳形参 |
| `.Create(item, instanceId, customData)` | `.Create(T, string)` | 只建条、不加量（数量 0） |
| `.Get(IIdentifiable, bool autoCreate)` / `.Get(string)` | `.Get(T, bool)` / `.Get(string)` | `autoCreate=true` 缺则建（同原版） |
| `.GetItems()` / `.GetItemInstances(bool useQuantity)` / `.GetWhere(Func<T,bool>)` | 同名 | `useQuantity=true` 时数量 N 的条目**重复出现 N 次**（已断言） |
| `.TryGetItem(string, out)` / `.TryGetItem(Func<T,bool>, out)` | 同名 | |
| `.OnItemCountModify(T, int)` / `OnItemCountModifyInternal(...)` | `.OnItemCountModify(T,int)` + 私有 `OnItemCountModifyInternal` | 对外事件 = `OnItemCountChange(T, **变化量**)` |
| `.RegisterSubset(Type, InventorySubset)` | `.RegisterSubset(InventorySubset<T>)` | 原版按 `Type` 注册（反射找子集）；我们**直接给实例**（少一层反射，语义同） |
| `.UpdateItemCollection()` | 同名 | **空实现**：我们是「字典即正本」，没有另一份实例列表；留着是为了让原版那些**调用点**有同名落点 |
| `.OnFinishUnpack()` | **无** | 判 **D**：那是 Addressables **异步解包完成**的回调；我们预导入 `Resources/` ⇒ 结构上不存在 |
| `ItemInfo<T>`：`Item/Amount/InstanceId/EphemeralState` + `SetInfo/Add/SetAmount/AssignEphemeralState` | 同名同义 | `EphemeralState` 存 `int`（原版是枚举；我们只要求「这一格存在且能读写」） |
| `InventorySubset`：`Add/AddRange/Get/TryGetValue` | 同名同义 | |

**两处如实差异（写在文件头，⛔ 不是「已经一样」）**：
1. 原版 `T : IIdentifiable` 自带 `GetID()` ⇒ 我们**构造时注入 `Func<T,string> idOf`**（同一语义、换一个取 id 的口）。
2. 原版 `Item` 是 `LoadableReference<T>`（惰性资源，G §三 #6 未建）⇒ 我们**直接持 T**；将来 `LoadableRef<T>` 落地时改 `ItemInfo.Item` 的内部表示即可，**调用点不用改**。

**注册表**：`Inventories.Shop`（`Inventory<ShopItem>`，id = `pageIndex/商品名`，**与旧键逐字相同 ⇒ 旧存档能直接读**）· `Inventories.Cards`（`Inventory<CardDef>`，已给播种口 `SeedCardsAllUnlocked`）。

---

## ③ 迁移：`ShopData._owned` → `Inventories.Shop`

- **删掉**：`static Dictionary<string,int> _owned` + 整段临时落盘（`ShopData.StoreDirName/StoreFileName/StoreDir/StorePath/StoreOn/LoadOwned/PersistOwned/OwnedRowDto/OwnedStoreDto/_loaded`）。
- **现在**：`OwnedOf` = `Inventories.Shop.Get(ItemFor(p,i)).Amount`（条目还没有时回表里的出厂值）；`Buy` = `Inventories.Shop.Add(it, 1)` + 限购 −1 + `PersistShop()`。
- **播种**：首次读口/写口 → 给每个商品建稳定 `ShopItem` 实例、按表里的 `Owned`（出厂几张）入袋 → 再从 `WarpforgeInventory/shop.json` 读盘覆盖。
- **限购 `Available` 的位置**（如实标）：原版它在 store 的 `TSave`、**不在 `ItemInfo`** ⇒ 我们暂存同一份文件的 `extra` 那一格（`InventoryStore.Save(..., extras)`），**⛔ 没塞进 `ItemInfo`**（那会污染框架层）。
- **对外 API 保持**（`ShopScene` 的断言一行没改）：`ShopData.OverrideDir / LastStorePath / LastStoreError / StorePath / DeleteStoreForTest() / ReloadForTest() / ResetForTest()` —— 全部转给 `InventoryStore`。
- **迁移注释**（`InventoryStore` 类头 + `ShopData` 那一段）：写清「这是 `PlayerDataManager` / `LiveOpsEvent<TSave>` 落地**之前**的临时本地等价物，与 `DailyData`/`DraftRunSave`/`CardProgress` 同一套路，将来一起并进 `PlayerDataManager`」，并给了**三步迁移路径**。

---

## ④ 断言（都标「待自检恢复时跑」，`Editor/ShopScene.cs`）

**A · 框架语义（新一节，5 条）**
1. `Add(item,3)` ⇒ 数量 3；
2. 再加 2 ⇒ 5，且 `OnItemCountChange` 抛的是**变化量** +2（⛔ 不是新总量）；
3. **同 id 的另一个实例取到同一条**（背包按 id 认条目 —— 挡「改成按实例认」）；
4. `GetItemInstances(useQuantity:true)` 把数量 4 **展开成 4 个**、`false` 只出 1 个（挡「展开被去掉」）；
5. 扣 -100 ⇒ **停在 0**（⛔ 不出负库存）。

**B · 落盘 + 灭自证（既有那一节，文案已更新）**
买一件 ⇒ 断「真写盘了」（`LastStorePath` 非 nul + 文件存在）⇒ `ReloadForTest()` ⇒ 断「拥有数还在」「限购剩余次数还在」。
🔑 **灭自证点**：`ReloadForTest()` 现在会**先清背包、再按出厂值重新播种、最后读盘**
⇒ 想读到「买过之后」的值，**唯一的路就是盘上那份**（把 `PersistShop()` 与 `InventoryStore.Load` **两边一起删掉**，断言仍会红 —— 因为重新播种只会给出厂值）。

---

## ⑤ 调度台点名的「自适应缩放」判据 —— **摊开，等你裁**（⛔ 我没动它）

### ① 原版公式是什么
- **方法**：`CollectionCard.OnRectTransformDimensionsChange`（`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CollectionCard.cs:172-181`）
- **公式**：`cardUI.transform.localScale = rect.sizeDelta.y * initialCardScale / initialHeight`
- **两个常量**（`CollectionCard.cs:24-25` 字段声明）：`initialCardScale = 150f` · `initialHeight = 512f`
- **输入**：`GetComponent<RectTransform>().sizeDelta`（**取 `.y`**；W11 那轮核过指令流：`mov [rsp+0x70], rax` → `movss xmm6,[rsp+0x74]`，即 y）
- **输出**：`CardUI`（卡体那一层）的 `localScale`
- **触发**：`OnRectTransformDimensionsChange` = **RectTransform 尺寸一变**就跑（不是每帧）

### ② 它什么时候会压住张数条
| 量 | 值 | 出处 |
|---|---|---|
| 卡格 `Collection Card` 运行时 | **262.5 × 384** | `PolyAndCode.UI.RecyclableScrollRect._cellWidth/_cellHeight`（`MonoBehaviour_345129740864909092` 等三份逐字相同） |
| 张数条 `Counter` 占的高 | **37.5px**（锚 `0.002…0.09966714` × 384） | `CollectionWindow.CardsCntMinY/MaxY`（prefab 实读） |
| 按原版公式算出的缩放 | `384 × 150 / 512 = 112.5` px/卡单位 ⇒ 世界缩放 `112.5/108 = **1.0417**` | — |
| 卡体高度（`CardView.Height = 3.3313` 卡单位） | `3.3313 × 112.5 ≈ **374.8px**` | — |
| 格子里「张数条以上」的可用高 | `345.7px` | `CollectionWindow.RebuildCardsCells` 现式 |
| **重叠量** | `374.8 − 345.7 = **≈29.1px**` | — |

⇒ **不是概率问题：只要这一格有张数条（每一格都有），按原版公式画就 100% 压住**。

### ③ 我们现状 + `W11` 当时的现象
- **现状**（`Shell/CollectionWindow.cs` 的 `RebuildCardsCells`）：`scale = (CardsCellH − CardsCounterH) / (CardView.Height × 108f)` = **0.9609**
  ⇒ 卡体 `345.7px`，**正好停在张数条上沿**（不重叠）。
- **`W11` 的原文**（`CollectionWindow` 那段注释里逐字留着）：
  「按 1.0417 画，卡块高 374.83 > 格高 − 张数条 345.728 ⇒ **卡底压住张数条 29.1px**，正是用户 **2026-10-18** 报的那条缺陷（`W11` 修的就是它）」
  另引用户参考图实测：**卡宽 ≈ 格的 81.7% · 卡高 ≈ 82.8%**。
- **三方对不上**（这是要你裁的核心）：

| 读法 | 卡高/格高 | 卡宽/格宽 | 与用户实拍（82.8% / 81.7%） |
|---|---|---|---|
| **原版公式 1.0417** | 97.6% | 89.7% | ❌ 差太多 |
| **我们现在的 0.9609** | 90% | 82.7% | 高差 7.2pt、**宽几乎一致** |
| **用户实拍** | 82.8% | 81.7% | — |

### ④ 三条可能，给裁定时挑
- **(a) 原版的张数条是【压在卡上】的**（半透明底条盖在满格卡体的下缘）⇒ 那么「重叠」本来就是原版长相，`W11` 是我们**误判成缺陷**、并按用户的观感改了 ⇒ 该**回到 1.0417**（但会再看到那 29.1px 重叠）。
- **(b) 原版 `sizeDelta.y` 不是 384**：`Collection Card` 预制体是 **350×512**，若 RSR 只改锚点/位置而不改 `sizeDelta`，`sizeDelta.y` 可能一直是 **512** ⇒ `scale = 512×150/512 = 150`（卡 122% 格高）—— 明显不可能，⇒ 反证「RSR 真的把 sizeDelta 改成了 384」。这条**可排除**。
- **(c) 实拍那 82.8% 量的不是卡体边界**（量到的是**立绘/边框内沿**，而不是 `CardUI` 的外框）⇒ 那实拍**不能当公式的尺子**，只能当观感参考。

**我的建议（仅供参考，等你裁）**：**保持现状 0.9609**，理由：① 它同时满足「不压张数条」与「卡宽≈实拍 81.7%」；② 按 (a) 改回 1.0417 会重现用户明确报过的重叠；③ 真要按原版核，**先得把 `CardUI` 那一层的外框量准**（350×512 原型 + `2DCard` 2.0927×3.3313 卡单位 + `initialCardScale=150` 三者的相对关系），这需要一张**原版实拍里卡体与格子的精确矩形**——现有那张参考图量到的是 82.8%，与两条公式都对不上。

---

## ⑥ 没查清 / 顺手

1. **收藏「已拥有」标记还没接进来**（调度台要求的那一步）：收藏格那行「xN」现在读的是
   **`Shell/CardDetailPopup.cs` 的 `CardProgress.Owned(id, rarity)`** —— **那个文件不在我白名单**。
   框架侧我已备好 `Inventories.Cards` + `SeedCardsAllUnlocked(pool)`（按 `DeckRules.CopyLimit` 播种，**幂等**）；
   等那份被划进来，**只需把 `CollectionWindow.CardsCounterText` 的读口换成 `Inventories.Cards.Get(def).Amount`**（一行）+ 在建池时调一次播种。
2. **`InventoryManager`（24 条）仍是 C**：框架已落地，剩下的是它那一层的**便利口**（按类型取 / `HasEnough` / `GetOwnedCount` / `Remove` / `DeserializeInventory`）——
   用到哪个补哪个（⛔ 别再一次性建 24 个空壳）。
3. **`Inventory.Add` 的 `customData`** 与 **`ItemInfo.Convert<TOut>`**（原版「转换件」）**没做**：今天没有调用点；`Convert` 需要「转换表」，等真有转换物品时再建。
4. **`ShopData` 里 `using UnityEngine;`** 现在只剩注释用到（`JsonUtility`/`Debug` 已全部搬进 `InventoryStore`）—— 留着无害，下一批若清理顺手删。
5. **轮播式奖励抽屉（P2）**：按调度台口径「可以照判据建，建完标『待并排比』」⇒ 排在**这之后**，我下一轮做（规格已在 `报告/C_收藏卡组商店_补C_第1轮.md` §④·2）。
