# K2 — 卡牌 ID / 卡 SO 本地实证

只读调查。日期 2026-10-17。判据全部来自本地解包资源与本地 bundle 文件，未跑 Unity、未改工程。

## 0. 一句话结论

**本地只有「卡 ID 字符串」，没有一个 `RawCardScript` 卡 SO 实例**：卡体全部指向一个**本地不存在的 CAB**
`CAB-fb8f01449289635703d83d52c3f18245`；Addressables catalog 里唯一缺失的「卡包」是 `allcards_assets_all.bundle`，
且该 bundle 名旁边就列着 `RawCardScript`。⇒ **「只有 ID、没有 ID 对应的 SO」这句成立**。

---

## 1. 包 → 资产类型 → 字段 → 计数

### 1·1 全树扫描：卡 SO 字段命中 = 0

| 检查 | 结果 | 命令 |
|---|---|---|
| MonoBehaviour JSON 总数 | **66,543** | `find "d:/2/新解包资源/assets_full" -path "*/MonoBehaviour/*" -name "*.json" \| wc -l` |
| `cardShortId`（RawCardScript 专有） | **0** | `grep -rl "cardShortId" "d:/2/新解包资源/assets_full"` |
| `manaCost`（RawCardScript 专有） | **0** | `grep -rl '"manaCost"' "d:/2/新解包资源/assets_full"` |
| `cardDescriptionTextReferenceId` | **0** | 同上 |
| `cardImageReference` / `refNameId` / `alternateArts` | **0** | 同上 |
| `"cardName": "<非空字符串>"`（卡 SO 的自身字段） | **0** | `grep -rlE '"cardName": "[^"]+"' "d:/2/新解包资源/assets_full"` |

⚠️ `menus` 里有 **16** 个文件含 `cardName`，但值是 **PPtr 对象**（`{"m_FileID":0,"m_PathID":...}`），
那是 UI 组件对文本节点的引用，不是卡 SO。样例
`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1054263834097325334.json`。

**结论：`RawCardScript : PlayerItem` 的实例在 `assets_full` 里为 0。**
（`RawCardScript` 的 MonoScript 本身在 `bundle_Waprforge_monoscripts/MonoScript` 里，PathID `8864902669260471755`，
但那只是类脚本，不是资产实例。）

### 1·2 真正带 ID 的 5 个 bundle

脚本类名由 `MonoScript_<PathID>.json` 的 `m_ClassName` 反查（722 个 MonoScript 全在）。

| bundle | 资产类（m_Script PathID） | 文件数 | 带 ID 的字段 | 计数 | 样例 |
|---|---|---|---|---|---|
| `draftpacks` | `PrebuiltPack` (-6624639368848939580) | 882 | `cardIds: string[]` | 2,646 条 / **904 distinct**（全短 ID） | `AM10`,`GOF33`,`UM99` |
| `prebuiltdecks` | `PrebuiltDeck` (3298024840710435243) 224 个 + `PrebuiltSortedDeck` (146420486734122931) 12 个 | 236 | `cardLibraryIds: string[]`；`deckAvatar` PPtr；`deckHero` PPtr | 5,157 条 / **876 distinct**（859 短 ID + 17 个 32-hex）；`uniqueId` 字段存在但 **全部为空串** | `AM10` … / `0fca8843…` |
| `cosmeticsso` | `PlayerAvatar`(470) · `CosmeticItemTitle`(462) · `CosmeticItemCardback`(243) · `DropTableItem`(32) · `AllianceBadge`(16) · `ShopContainer`(10) … | 1,261 | `uniqueId`（PlayerItem 基类）· `obtainableItems[].targetId` | `uniqueId` 非空 **1,238 distinct**（1,180 hex32 + 58 短名）；`obtainableItems` 出现在 22 个 DropTableItem，targetId **382 distinct**（366 短 + 16 hex） | `AM_Forge_Badge` · `EC32` · `DT EmperorsChildren R1` |
| `menus` | 35,014 个 MB；带 ID 的只有 `DropTableItem`(2) / Booster 类(4) | 35,014 | `uniqueId`(4 个) · `obtainableItems[].targetId` | `uniqueId` **4 distinct**（`Booster Pack Ultramarines` 等）；targetId **50 distinct**，但**指向的是 DropTableItem，不是卡** | `DT Ultramarines All R2+` |
| `duplicateassetisolationso` | `DropTableItem`(2) · `Enchantments`(1) · `EnviromentalEffectCardsSO`(1) · `DemoDeckInfoSO`(2) … | 15 | `obtainableItems[].targetId` · `markOfChaos[].card[].targetId` · `offensiveCard` PPtr | Droptable targetId **45 distinct**（41 短 + 4 hex）；Enchantments 8 个卡引用 | `UM100` · `9a2cadc64c80a6b42912441105bb1cee` |

**短 ID 并集**（`^[A-Z]{1,4}[0-9]+$`）：**927 个**，13 个阵营前缀（UM 112 / GOF 101 / ASH 75 / DA·TL·TAU·SOR·SAU·AM·BL·GSC 各 65 / SW 58 / EC 58 / C 3）。
作为旁证：我们自己的 `d:/4/Unity/数据/游戏数据/card_ids.json` 有 1,310 行（1,049 短 + 259 hex、1,308 条非空）——量级吻合。

---

## 2. ID ↔ 卡本体：能连到哪一档

### 2·1 结构上「ID + PPtr」是有的，但 PPtr 指向本地不存在的 CAB

用 UnityPy 读每个 bundle 的 **SerializedFile 外部引用表**（注意：表的索引 0 对应 `m_FileID=1`）：

| bundle | `m_FileID` | 外部 CAB | 本地有吗 |
|---|---|---|---|
| `cosmeticsso` | 1 / 2 / 3 | monoscripts / **CAB-fb8f01449289635703d83d52c3f18245** / duplicateassetisolationso | 2 = **无** |
| `duplicateassetisolationso` | 1..4 | monoscripts / prebuiltdecks / armyicons / **CAB-fb8f…** | 4 = **无** |
| `prebuiltdecks` | 1 / 2 / 3 | monoscripts / cosmeticsso / **CAB-fb8f…** | 3 = **无** |
| `menus` | 22（还有 9/25/28 是本地包，只是 CAB 串在 4 KB 头之外） | **CAB-fb8f…** | **无** |
| `draftpacks` | 只有 1 = monoscripts | — | 全部本地 |

**逐条验过**（`LoadableReference<T>`：`genericObjectReference` / `targetId` / `reference` 三件套）：

- `cosmeticsso/DT EmperorsChildren R1.json`：20 条 `targetId`（`EC32`,`EC11`…），`m_FileID=2` → **CAB-fb8f**；
- `duplicateassetisolationso/DT Ultramarines R4.json`：18 条（`UM77`,`UM12`…），`m_FileID=4` → **CAB-fb8f**；
- `duplicateassetisolationso/Enchantments.json`：8 条 hex ID（`9a2cadc6…` = 项目表里的 `Mark of Khorne (enchantment)`），`m_FileID=4` → **CAB-fb8f**；
- `prebuiltdecks/AM_SK_1.json` `deckHero.targetId="AM3"`，`m_FileID=3` → **CAB-fb8f**。

**统计**：有 PPtr 且落到 CAB-fb8f 的 card ID = **380 distinct**（366 短 ID + 14 hex32）。
分布在 `prebuiltdecks` 236 文件、`cosmeticsso` 30 文件、`duplicateassetisolationso` 3 文件。
⇒ **927 个短 ID 里，只有 366 个连得到「卡 SO 指针」，其余 561 个只有字符串。**

### 2·2 唯一「能真正落到实体」的那一档 = 督军（已验证的旧结论）

- `prebuiltdecks.deckAvatar`（`m_FileID=2`）→ **cosmeticsso 的 `PlayerAvatar`**：`m_FileID 2 → CAB-79aceaa0b2968acd539309745c14ca49 = cosmeticsso_assets_all.bundle`，本地**存在**。
- `prebuiltdecks.deckHero`（`m_FileID=3`）→ **CAB-fb8f（卡 SO，缺失）**，但它的 `targetId`（如 `AM3`）和 `deckAvatar` 的名字（`Avatar_<阵营>_<督军名>`）可以对上 ⇒ 督军这一档 ID ↔ 名字可解（与第三十三会话 R9 一致）。
- 预组卡组：`deckId` / `deckName` 自带（`AM_SK_1` / `Press the Attack`），卡表只有 `cardLibraryIds`。
- 普通卡：**没有任何一档能在本地连到名字**，因为卡 SO 不在本地。

### 2·3 「只有 ID」的三种形态（别混为一谈）

1. **纯字符串列表**：`draftpacks.cardIds`（904）、`prebuiltdecks.cardLibraryIds`（859 短 + 17 hex）——无 PPtr。
2. **ID + PPtr（目标缺失）**：`cosmeticsso` 22 个 DropTableItem、`duplicateassetisolationso` 2 个 DropTableItem + Enchantments、`prebuiltdecks.deckHero` —— 有 `targetId` 和 `reference`，但 `reference` 落在 CAB-fb8f。
3. **不带 targetId 的 PPtr**：`EnviromentalEffectCardsSO.offensiveCard` —— 只有 PPtr（也指向 CAB-fb8f），**ID 反而要回 SO 才知道**。

---

## 3. `allcards` 在哪

### 3·1 本地：不存在（反证：搜过的路径与前缀）

| 搜索 | 结果 |
|---|---|
| `ls "d:/2/新解包资源/assets_full"` （90 个顶层项） | 84 个 `bundle_*` + 6 个根资产目录，**无 allcards** |
| `find "d:/2/新解包资源" -maxdepth 6 -iname "*allcard*"` | 唯一命中：`bundle_chaosspacemarinesblacklegioncardanims_assets_all/MonoBehaviour/Hand_Buff_UM_AllCards.json`（动画状态，不是卡包） |
| `find "d:/2" -maxdepth 6 -iname "*allcard*"` | 仅命中反编译方法名（`GetAllCardsInHand.c` 等）与上面那一份动画 JSON |
| `ls .../StreamingAssets/aa/StandaloneWindows64`（两份安装：`d:/2/unity_run_ref` 与 `d:/2/Warhammer 40k Warpforge`） | 各 **84 个 bundle 文件，无 `allcards_assets_all.bundle`** |
| `find "d:/" -maxdepth 8 -iname "allcards_assets_all.bundle"` | **0** |
| Addressables 下载缓存 `%LocalLow%/Everguild/Warpforge/com.unity.addressables` 与 `%LocalLow%/Unity/Everguild_Warpforge` | **无 allcards** |

### 3·2 但 catalog 里明确列着它

`d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/catalog.bin`（两份安装内容一致，1,305,148 B）里可读到：

```
offset 122560  allcards_assets_all_ea8bca4f5c6909d3124fd4975dc96173.bundle
offset 122623  allcards_assets_all.bundle
offset 122817  RawCardScript
offset 1304896 allcards@   (bundle 哈希表项 ea8bca4f…bundle)
```

把 catalog 里出现的 bundle 名与本地 84 个文件做差集，**本地缺 3 个**：
`allcards_assets_all.bundle`、`alternateartstyles_assets_all.bundle`、`localization_assets_all.bundle`。
其中只有 **allcards** 是「装卡 SO」的（`RawCardScript` 就列在它旁边）。

### 3·3 CAB-fb8f = allcards

- `CAB-fb8f01449289635703d83d52c3f18245` 是 4 个 bundle（cosmeticsso / duplicateassetisolationso / prebuiltdecks / menus）共同依赖的外部文件；
- 本地 84 个 bundle 的 CAB 名**逐个读过**（`CAB-xxxx` 在文件头），**没有一个是 fb8f**；
- 所有卡 ID 的 PPtr 都落在它上面；
- 本地缺失的 card-bearing bundle 只有 allcards 一个。

⇒ **高置信推断（非直接验证，因为文件本身不在）**：CAB-fb8f = `allcards_assets_all.bundle`。
要直接坐实，只能拿到该 bundle 读它的 CAB 名。

---

## 4. 可复现命令（关键几条）

```bash
# 全树卡 SO 字段 = 0
grep -rl "cardShortId" "d:/2/新解包资源/assets_full"
grep -rlE '"cardName": "[^"]+"' "d:/2/新解包资源/assets_full"

# 各 bundle 的 ID 字段
grep -rl '"cardIds"'        .../MonoBehaviour | wc -l   # draftpacks 882
grep -rl '"cardLibraryIds"' .../MonoBehaviour | wc -l   # prebuiltdecks 236
grep -rl '"obtainableItems"' .../MonoBehaviour | wc -l  # cosmeticsso 32
grep -rl '"uniqueId"'       .../MonoBehaviour | wc -l   # cosmeticsso 1238

# 外部引用表（m_FileID 0=自身，索引 0 对应 m_FileID 1）
python -I -c "import UnityPy;env=UnityPy.load(r'...\\cosmeticsso_assets_all.bundle');bf=list(env.files.values())[0];sf=[v for k,v in bf.files.items() if not str(k).endswith('.resS')][0];print([str(e.path) for e in sf.externals])"

# catalog 里的 allcards
python -I -c "import re,io;b=io.open(r'...\\aa\\catalog.bin','rb').read();print([m.group().decode() for m in re.finditer(rb'[A-Za-z0-9_]*allcards[A-Za-z0-9_.]*',b)][:5])"
```

本报告的两个扫描脚本留在同目录：`_k2_scan.py` / `_k2_overlap.py` / `_k2_deps.py` / `_k2_union.py` / `_k2_classes.py`（纯读，可删）。

---

## 5. 没查清

1. **CAB-fb8f 的名字**：只能推到「allcards」，没有 bundle 文件就无法读它头部的 CAB 名（见 §3·3）。
2. **`prebuiltdecks.cardLibraryIds` 里 17 个 32-hex**：落在 10 个文件（混格式）。用项目 `card_ids.json` 抽验 3 个，映射到
   `Firstborn (tutorial)` / `Infernal Gaze (tutorial)` / `Honour Guard (tutorial)` ⇒ **是卡（教程/变体卡）的 uniqueId**，不是异常；
   但 17 个全量没逐条核。
3. **`menus` 的 4 个 `uniqueId`（Booster Pack / DT …）**：它们是 drop table 的 id；`obtainableItems` 指向的是别的 DropTable，
   **不直接指卡**。真正「包 → 卡」的链要跨到 `duplicateassetisolationso`，这一段没逐条展开。
4. **`alternateartstyles_assets_all.bundle`** 同样本地缺失；它是否也装 `RawCardScript` 变体、是否也被这 4 个包依赖，未查。
5. **`EnviromentalEffectCardsSO.offensiveCard`** 有 PPtr 无 targetId，其卡 ID 无法从本地文本层恢复。
