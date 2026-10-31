# B113_G3 · 判据：`WarpforgeCardLevels.json` 的 `owned` 是不是第二条坏源（`B-140`）· 默认卡背 13 条图名（`B-143`）

> 查判据代理 `B113_G3` · 2026-11-01（第一百一十一会话 · 判据缺口组）
> 红线遵守：未跑 Unity · 未动 git · **未对任何真实存档做写操作**（下文所有"跑"都是只读 `d:/2`）· 未改 `d:/2`。
> 只读复核脚本：`d:/4/_tmp_view/b113_g3/verify_defaults.py`（一次性，不进工程）。

---

## 一、`B-140`：`WarpforgeCardLevels.json` 的 `owned` 是不是第二条坏源

### 1·1 判据结论（一句话）

**不是。当前代码里 `owned` 的值**不可能**被写成 1 —— 它的三个写点里，只有一个是「从盘上读进来」（值 = 上一版写下去的值），另外**两个都是算出来的**、结果**恒 ≥ 10**（`DeckCap(rarity) + 9`）。
⇒ `B-84` 那条迁移**不需要扩**；它现在「识别成没修好并 `LogError`」的分支只有在**外部手改/损坏这份 json** 时才会走到。
⚠️ 但查出一条**该记的新事实**：`_owned` 缓存**参与**了「唯一播种口径」，所以它若含**任何**陈旧值（不只是 1），迁移会**把它当成正确值写下去、且不出声**（详见 1·4）。

### 1·2 `CardProgress._owned` 的读写链全貌（逐点，`文件:行号`）

| # | 位置 | 做什么 | 产生的值 |
|---|---|---|---|
| **W1（唯一从盘读）** | `Shell/CardDetailPopup.cs:189`（`EnsureLoaded`） | `foreach (d.owned) _owned[e.id] = e.v` —— 从 `Application.persistentDataPath/WarpforgeCardLevels.json` 的 `owned` 数组灌进缓存 | = 上一版 `Save()` 写下去的值 |
| **W2（算出来的）** | `CardDetailPopup.cs:247-248`（`Owned` 的兜底分支） | 卡**不在背包**里且缓存里没有 ⇒ `v = DeckCap(rarity) + (NeedCopies[MaxLevel] - NeedCopies[1]); _owned[id] = v` | `DeckCap + 9` = **10（传说）/ 11（其余）** — 见 1·3 |
| **W3（算出来的）** | `CardDetailPopup.cs:266`（`AddOwned` 的兜底分支） | 卡**不在背包**里 ⇒ `_owned[id] = Owned(id, rarity) + n` | 生产唯一调用点 `:2577`（`DoCraft`）的 `n = +1` ⇒ **≥ 11** |
| **读（唯一口径）** | `CardDetailPopup.cs:243-249`（`Owned`） | 优先级：**背包 `Inventories.Cards.Get(id)` → `_owned` 缓存 → 现算并缓存** | 见上 |
| **写盘（唯一写手）** | `CardDetailPopup.cs:206-224`（`Save`） | `foreach (_owned) d.owned.Add(...)` → `File.WriteAllText(SavePath, …)` | 整份缓存原样落盘 |

**「唯一写手」的独立复核**：全仓 `grep "WarpforgeCardLevels"` / `OursCardProgress` 只有三处是**真写盘/读盘**——
`CardDetailPopup.cs:161`（文件名常量）/ `:215`（`File.WriteAllText`）/ `LegacySaveReader.cs:498`（**导入式迁移**，走 `SaveCenter.SetRaw` 只写中央档，**不写这份旧档**，`LegacySaveReader.cs:183`）。
`Editor/CollectionScene.cs:9090` 只是**读字节**做「自检没改玩家存档」的断言。
⇒ **盘上 `owned` 里的每一个值，都出自 W1/W2/W3 三者之一。**

### 1·3 「1」到底可能从哪来 —— 逐写点核过

| 写点 | 能不能产出 1 | 判据 |
|---|---|---|
| **W2** | ❌ | `DeckCap(rarity)` 最小 = 1（传说；`RuleEngine/Core/GameplayVariables.cs:105` 的 `ClassicCopiesLegendary = 1`），加 `NeedCopies[4]-NeedCopies[1] = 9-0 = 9` ⇒ **最小 10**（`CardDetailPopup.cs:113` 的 `NeedCopies = {0,0,2,5,9}`） |
| **W3（生产）** | ❌ | 生产唯一调用 = `DoCraft` 的 `AddOwned(..., +1)`（`CardDetailPopup.cs:2577`）⇒ `≥ 10+1` |
| **W3（自检）** | ⚠️ **能**，但**不落盘** | 只有 `Editor/**` 会传负 delta（`CollectionScene.cs:5802/9069/9221/9258/9347/9379/9411/9466`、`CardFaceAssertE5.cs:388`、`B104_P3_Assert.cs:230`）。这些断言要么 `_persist=false`（`CardProgress.ResetForTest`），要么卡**在背包里**（走背包分支、不碰 `_owned`）⇒ **对盘上 json 零影响** |
| **W1** | ❌（除非外部改） | 值来自这份 json 自己；而写这份 json 的只有 `Save()`，它写的就是 W1/W2/W3 的并集 ⇒ **自洽，不可能凭空出现 1** |

### 1·4 原版有没有对应物（为什么这份 json 本来就是我们自定）

- **原版没有「卡牌等级/拥有数/通配符」这份本地档**：`Shell/SaveKeys.cs:309-312` 写着「**原版没有对应 key**：原版这三样由**服务端 inventory** 管（`EphemeralInventory` 是奖励库存，不是这个）」。
- 原版那一侧的形状也核到了：卡牌档位是**运行时表** `TierCollection` —— 每次由服务端 `CardTierConfig` 重建、ctor 里 `CleanTierCollection()` 先清空（`Shell/Inventory.cs:1348-1350` 的注释 + `:1392` 的注释，两条都是在读原版之后写的，不是猜）；而我们把它落成了**持久** `CardProgress`。⇒ **这份 json 是我们的替身，原版无对应物**。
- 升级表数值本身也是我们编的（`CardDetailPopup.cs:114-117` 的 `NeedGold` / `Points` 头注「**我们编的，不是原版**」）。
⇒ 这份 json 属于**「服务器相关 ⇒ 允许自己设计」**那一档（`CLAUDE.md` 铁律 18·0），不构成「拿我们的实现证明原版」的自证风险。

### 1·5 真查出来的那一条（该记，但不该按「坏源」修）

**`_owned` 缓存是会参与「唯一播种口径」的** —— 迁移调的是
`SeedCardsAllUnlocked(pool, c => CardProgress.Owned(c.Id, c.Rarity))`（`Shell/InventoryPartial_B112G.cs:202`），
而 `Owned` 对「刚被摘掉背包条目的那张卡」会先查 `_owned`（`CardDetailPopup.cs:246`）。因此：

| 情形 | 现状行为 | 出处 |
|---|---|---|
| `_owned[id] == 1`（外部手改/损坏） | 播种得 1 ⇒ 迁移判 `stillBad`、`LogError`、**这一条不动**；一条都没修好时**连盘都不写** | `InventoryPartial_B112G.cs:207-222` |
| `_owned[id] == 5`（外部改成的**任意小值 >1**） | 播种得 5 ⇒ 迁移算「修好」（`now > 1`）⇒ **把 5 写进背包且不出声** | `InventoryPartial_B112G.cs:211-213` |

⇒ **第二条比「残留 1」更隐蔽**：迁移的判据只有 `amount == 1`（`BadCardAmountSentinel`，`InventoryPartial_B112G.cs:72`），
它挡不住「>1 但低于基准量」的陈旧值。当前代码不会产生那种值（1·3），所以**今天不是 bug**；记它是为了将来别把它当「已覆盖」。

### 1·6 若将来真要扩迁移 —— 缺什么（判据为空的部分写清，不自己设计）

要真收口「缓存把播种口径带歪」这件事，必须有下面两样**当前不存在**的能力：

1. **读/摘 `CardProgress._owned` 的公开口** —— `_owned` 是 `private static Dictionary`（`CardDetailPopup.cs:133`），
   全仓**没有**任何公开的读/删/清 API（`ResetForTest` 是整份 `Clear` 且**永久关落盘**，不能给迁移用）。
2. **单独落一次 json 的口** —— `Save()` 是 `private static bool`（`CardDetailPopup.cs:206`），迁移侧调不到。

⇒ 两样都在 `Shell/CardDetailPopup.cs` 里，**不在本代理白名单**。**⛔ 本代理没有自己发明「基准量下限」之类的第二套算法** ——
迁移的判据必须继续是「唯一播种口径」（`InventoryPartial_B112G.cs:9-17` 那张头注的理由仍然成立）。

### 1·7 还差什么

- **本机没有 `WarpforgeCardLevels.json`**（`B112_G` 已核、本代理复核 `%LOCALAPPDATA%Low\QJHWarpforge\WarpforgeReplica` 下 0 命中）
  ⇒ 以上结论是**代码路径级**的（静态可证），**不是**「盘上样本级」的。
- 「玩家真消费过的数量」在原版由服务端管，我们本地**没有任何扣卡路径**（`CardDetailPopup.cs:2623` 的 `DoDowngrade` 注释：「**不消耗任何东西**（副本 / 金币都不退也不扣）」）
  ⇒ 「`owned` 与玩家真消费量同字段」这个顾虑**在本工程不成立**（我们根本没有消费）。这条是 `B112_G` 报告 §⑧·1 写的，**本代理就地订正**。

---

## 二、`B-143`：`CardbackTable.DefaultFor` 的 13 条图名

### 2·1 判据结论（一句话）

**这 13 条图名本地早就解出来了，而且和我们表里的一字不差（13/13）** ——
解它的工具是 `工具/read_default_cardbacks.py`（2026-10-03 立，`A20`），产物已经烤进 `Resources/Cardbacks.json` 的 `defaults`（2026-10-03 起就在，**未被改过**）。
⇒ `B-143` 记的「本地没解出图名」是**过期结论**（`B112_T2C` 只看了 `CardbackTable.cs`、没看那个工具/产物）。

### 2·2 判据链（逐跳，全部本代理现读复核）

| 跳 | 结论 | 证据 |
|---|---|---|
| ① | `level0` pathID **391** 是 `ArmyUtilities` | `globalgamemanagers.assets` pathID **3281** 的 MonoScript `m_ClassName = "ArmyUtilities"`；391 的 `m_Script = {fileID 1, pathID 3281}`（`level0` externals[0] = `globalgamemanagers.assets`） |
| ② | 它的 `+0x20` = **`defaultCardBacks`** = PPtr `{fileID 2, pathID 420}` | `d:/2/tools/il2cpp_out/dump.cs:70013-70014`：`[SerializeField] private DefaultCarbackByArmySO defaultCardBacks; // 0x20`；`level0` externals[1] = `sharedassets0.assets` |
| ③ | pathID **420** 是 `DefaultCarbackByArmySO`、名字就叫这个、列表 **13 条** | `sharedassets0.assets` 420 的 `m_Script = {1, 625}` → `globalgamemanagers.assets` 625 的 `m_ClassName = "DefaultCarbackByArmySO"`；`m_Name = "DefaultCarbackByArmySO"`；首字段偏移 **56**（`m_Name` 长 22、对齐后 24 ⇒ `32+24`），`count = 13` |
| ④ | 全文件**只有这一个** `DefaultCarbackByArmySO` 实例 ⇒ PPtr 无歧义 | 扫 `sharedassets0.assets` 全部 `MonoBehaviour`，`m_Script pathID == 625` 的只有 `[420]` |
| ⑤ | 13 条每条 = `int32 army` + `PPtr{fileID 0 → 同文件, pathID}` | 复核脚本逐条打出来的 pathID：446 / 439 / 441 / 442 / 435 / 440 / 445 / 443 / 438 / 434 / 436 / 437 / 444 |
| ⑥ | 每个目标是 `CosmeticItemCardback` | 它们的 `m_Script pathID = 3315` → `globalgamemanagers.assets` 3315 的 `m_ClassName = "CosmeticItemCardback"` |
| ⑦ | 图名 = `imageReference.m_SubObjectName` 去掉结尾 `_Main` | 243 个 `Cardback*` SO 名字**无重名**（dups = 0）⇒ 按名反查无歧义；每条 `cardArmy` 与列表里的 army 值**逐一相等**（10…130）—— 这是**独立于文件名**的第二重印证 |

### 2·3 13 条逐条表（**判据值**；与我们的映射对账 = 13/13 一致）

| # | army | 阵营（`CardArmy`） | SO 名（`m_Name`） | **图名（= 我们 `Cardbacks.json` 的 `name`）** | `uniqueId` |
|---|---|---|---|---|---|
| 1 | 10 | Ultramarines | `Cardback_UM_Campaign_Free` | `Cardback_UM_Campaign_Free` | `10a1e3f4c77c0491196b655ad4d7d72d` |
| 2 | 20 | Goff | `Cardback_GOF_Campaign_Free` | `Cardback_GOF_Campaign_Free` | `7a504f07fd985ef489440b8d11180057` |
| 3 | 30 | SaimHann | `Cardback_ASH_Campaign_Free` | `Cardback_ASH_Campaign_Free` | `00346e3d75a0542caad6ce7c2e39cbad` |
| 4 | 40 | Sautekh | `Cardback_SAU_Campaign_Free` | `Cardback_SAU_Campaign_Free` | `7aa743e46193140b99ce2d208cf9a070` |
| 5 | 50 | BlackLegion | `Cardback_BL_Campaign_Free` | `Cardback_BL_Campaign_Free` | `01544685e30e1404f848876965c9f238` |
| 6 | 60 | Leviathan | `Cardback_TL_Campaign_Free` | `Cardback_TL_Campaign_Free` | `d496acee924da9b43a4b8465c35f7f7c` |
| 7 | 70 | TauEmpire | `Cardback_TAU_Campaign_Free` | `Cardback_TAU_Campaign_Free` | `93a7273d14f2d344f90326aec15ad574` |
| 8 | 80 | Sororitas | `Cardback_SOR_Campaign_Free` | `Cardback_SOR_Campaign_Free` | `20b8d9bd938ca416e9be525def18787c` |
| 9 | 90 | Genestealers | `Cardback_GSC_Dawn of Uprising` | `Cardback_GSC_Dawn of Uprising` | `a9e3cc532f23540b382d22b2b519c94f` |
| 10 | 100 | AstraMilitarum | `Cardback_AM_Shield of Humanity` | `Cardback_AM_Shield of Humanity` | `6edc0c2f2e1c64dfe92a272bab138646` |
| 11 | 110 | DarkAngels | `Cardback_DA_The First Legion` | `Cardback_DA_The First Legion` | `ac8bf09615211453c9a95d6ad90341b1` |
| 12 | 120 | EmperorsChildren | `Cardback_EC_Emperors Children` | `Cardback_EC_Emperors Children` | `325fc8c467671664eab5b5e7896811f4` |
| 13 | 130 | SpaceWolves | `Cardback_SW_Campaign_Free` | `Cardback_SW_Campaign Free` ⚠️ **空格不是下划线** | `07b44a359d4e74a87aa0ba04070c4079` |

（没有 `Neutral` —— 原版那表就 13 条。`CardbackTable.DefaultFor("Neutral")` 必须返回 `null`。）

**两个要小心的格子**（**图名里带空格**）：
- `army 120`：`Cardback_EC_Emperors Children` —— SO 名与图名**相同**、都带空格；
- `army 130`：SO 名 `Cardback_SW_Campaign_Free` → 图名 `Cardback_SW_Campaign Free` —— **13 条里唯一一条 SO 名 ≠ 图名**（`_Free` 变 ` Free`）。

`CardArt.Cosmetic(name)` 拼路径用的是**图名**（`Core/CardArt.cs:959-963`：`Root + "cardbacks/" + fileName`）⇒ 空格写错就取不到图（`CardArt.Get` 还会把 null 缓存起来）。

### 2·4 与我们的映射对账（13/13 一致，逐条核过）

| 核的 | 结果 |
|---|---|
| `Resources/Cardbacks.json` 的 `defaults` 条数 | **13**（2026-10-03 的 `868c4604` 起就有，之后**没有改动**） |
| `defaults[i].name` == 判据图名 | **13/13** |
| `defaults[i].cardback` == 判据 SO 名 | **13/13** |
| `defaults[i].uniqueId` == 判据 uniqueId | **13/13** |
| `defaults[i].name` 在 `items[]` 里、且 `items[].army` == 该阵营 | **13/13** |
| 图真的在工程里（`Resources/Art/cardbacks/<name>.png`） | **13/13**（含两个带空格的） |
| 13 条两两不同 | ✅（13 个唯一） |

### 2·5 我们的运行期消费链（确认「表对了 = 画面对」）

`CardArt.CardBack(faction)` → `CardbackTable.DefaultFor(ModernFaction(faction))` → `CardArt.Cosmetic(name)`
（`Core/CardArt.cs:399`、`:959-963`）；原版对应 `CardDeck.GetDeckCardback` → `ArmyUtilities.GetDefaultCardback`
（`decomp_full/ArmyUtilities__GetDefaultCardback.c:1-8`：读 `+0x20` → `DefaultCarbackByArmySO.GetCardbackCosmeticItem`；
调用点 `decomp_full/CardDeck__GetDeckCardback.c:36`）。⇒ 同一条链、同一个回退口。

### 2·6 还差什么

**没有缺口。** 唯一要改的是**记录**：`报告/B112_T2C.md:283` 与 `Editor/B112_T2C_Assert.cs:327,360-364` 里
「13 个 PPtr 本地没解出图名 / 判据缺口」这条**已过期**（那份数据 2026-10-03 就在，见 §2·4 的 git 证据）。
⚠️ 两处都**不在本代理白名单**（只允许新建 `Editor/B113_G3_Assert.cs`）⇒ 本代理**没改**，施工单里给出逐句改法。

---

## 三、施工单（谁能直接照着做）

| # | 做什么 | 落点 | 判据/验收 |
|---|---|---|---|
| **S1** | ✅ **本代理已做**：新建 `Editor/B113_G3_Assert.cs`，把 13 条图名钉成**字面量** + 每个 `CardArt.Cosmetic(name) != null` + `Neutral == null` | `CardPresentation/Editor/B113_G3_Assert.cs` | 运行后应 **≥ 30 条全绿**；把 `Cardbacks.json` 任一 `defaults[i].name` 改成别的 ⇒ 对应那条红 |
| **S2** | ✅ **本代理已做**：同文件里钉 `B-140` 的三条不变量（W2 恒 ≥10 / W3 生产 +1 / 毒化 `_owned=1` ⇒ 迁移不写盘 + 报「一条都没修好」） | 同上 | 见文件头的「灭自证」一栏 |
| **S3** | 把 `B113_G3_Assert` 接进宿主 | `CardPresentation/Editor/CollectionScene.cs:13250` 那条聚合行末尾（`//` 之前，**同一物理行**）：`B113_G3_Assert.Run(); _sink.Pass += B113_G3_Assert.Pass; _sink.Fail += B113_G3_Assert.Fail;` | 跑 `CollectionScene.Run` 看合计里多出来这几十条 |
| **S4** | **就地改掉过期记录**（铁律 5）：`报告/B112_T2C.md:283` 与 `Editor/B112_T2C_Assert.cs:327,360-364` 的「本地没解出图名 ⇒ 判据缺口」 | 两处 | 改成：「⚠️ 2026-11-01 订正（`B113_G3`）：图名**已解出**（`工具/read_default_cardbacks.py`，2026-10-03 起在 `Resources/Cardbacks.json` 的 `defaults`）⇒ 已由 `B113_G3_Assert` 钉成 13 条字面量；判据表 → `判据/B113_G3_存档owned与卡背图名.md` §二」 |
| **S5** | 活状态表两条改成「已查清 / 无缺口」 | `活状态表.md` `B-140`/`B-143` 行（**调度台**写，本代理不碰） | `B-140`：判据结论「当前代码不可能产生 1 ⇒ 迁移不需扩」+ 新事实（1·5 那条「>1 陈旧值会静默写入」）；`B-143`：13/13 一致 |
| **S6** | **⛔ 不要做**：给 `CardProgress._owned` 加「≤基准量就忽略」之类的净洗 | —— | 判据为空（见 1·6）：要动得先有 `CardDetailPopup.cs` 的公开口；**现在没有**，⛔ 别自己造第二套算法 |
