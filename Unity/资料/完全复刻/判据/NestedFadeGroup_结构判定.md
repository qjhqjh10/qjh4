# NestedFadeGroup 结构判定 —— 原版到底嵌不嵌套？

> 判据型子代理（只读）。判据 = 原版 prefab `CardPrefab` 的**整棵 `m_Children` 树**（⛔ 不只看 `m_Father` 字段名）。
> 源：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`（GameObject/Transform/RectTransform/MonoBehaviour），脚本类 = `NestedFadeGroup`(4592898973196831887) / `NestedFadeGroupSpriteRenderer`(7883161723670755170) / `NestedFadeGroupTextMeshPro`(-1225478152557325064)。

## 一、结论（一句话）

**原版【真嵌套】—— 9 个 `NestedFadeGroup` 组根，每个根的子树上都挂着 4~6 个 `NestedFadeGroupSpriteRenderer/TextMeshPro` 子件**（共 57 个 = 9 组根 + 38 SR + 10 TMP）。
⇒ **不是「机制不适用（D）」，是「我们结构缺（要做）」**：我方 `CardView._badgeAlpha` 是**整卡一个标量**（`ApplyBadgeAlpha`），没有「组根 alpha 广播给子树子件」这一层 ⇒ 分组淡入不独立。

> 🔴 **推翻 `[G12 现核]「组树不嵌套」`**：G12 只查了「9 个组根彼此的**父链**」（确实互不为父，parentChainNFG=0），**没查子树**。真正的嵌套在**根 → 子孙件**这一层（下面树）。G12 虽写了「57 个实例 = 9 组根 + 38 SR + 10 TMP」，但把结论下成「不嵌套 / 只是粒度」，**前半错**：那 48 个 SR/TMP 子件就是嵌套（订阅组根）本身。

## 二、逐层证据（`m_Children` 树，抽一个根完整展开）

`TraitIconContainer Right 3` 这一棵（`[GROUP]`=`NestedFadeGroup` 组根；`[SR]`=`NestedFadeGroupSpriteRenderer`；`[TMP]`=`NestedFadeGroupTextMeshPro`）：

```
TraitIconContainer Right 3        [GROUP]              ← 组根（NestedFadeGroup）
├─ IconBackground                 [SR]                 ← 子件（订阅组根 alpha，下同）
└─ Container
   ├─ Without counter
   │  ├─ TraitIcon                [SR]
   │  └─ Trait Not Active         [SR]
   └─ With counter
      ├─ TraitIcon                [SR]
      ├─ TraitCounter             [TMP]
      └─ Trait Not Active         [SR]
```
⇒ **一个组根 → 6 个子孙淡入件（含 TMP 计数）**，这就是「嵌套淡入组」的实际用法（组根 alpha 一广播，子树所有 SR/TMP 一起淡）。

## 三、全量统计（脚本现算，非转述）

- `NestedFadeGroup` 实例 **9**（组根）· `NestedFadeGroupSpriteRenderer` **38** · `NestedFadeGroupTextMeshPro` **10** ＝ **57**。
- **9 个组根的父链上 NFG 家族数 = 0**（互不为父，`parentChainNFG=0`，`ancestors=3~4`）—— 这点 G12 对。
- **但每个组根的子树里 NFG 家族子孙 = 4~6**：
  | 根 | 子树节点数 | 其中 NFG 家族子孙 |
  |---|---|---|
  | 根1 | 9 | 6 |
  | 根2 | 9 | 6 |
  | 根3 | 1 | 1 |
  | 根4 | 9 | 6 |
  | 根5 | 9 | 6 |
  | 根6 | 9 | 6 |
  | 根7 | 9 | 6 |
  | 根8 | 6 | 4 |
  | 根9 | 9 | 6 |
  ⇒ **嵌套确实在用**（每根带 4~6 个淡入子件）。
- 子件挂点：8 个 SR/TMP 的直接父是 NFG 家族（紧邻组根/子组），另 40 个在更深层（父是中间节点，但祖先链上有组根）——典型「组根容器 → 中间排版节点 → 叶子 SR/TMP」。

## 四、这与「我方现状」的差（=施工方向）

- 原版：**每个组根一份 alpha**（G12 另记「7 个由 `BoardTraitIcon.fadeGroup` 单独驱动」）→ 广播给子树 SR/TMP ⇒ **分组独立淡入**。
- 我们：`CardPresentation/Core/CardView.cs` 的 `_badgeAlpha`（`ApplyBadgeAlpha`）**整卡一个标量** ⇒ 一淡全淡、无法只淡某一组。
- **动作 = 要做（结构缺，非 D）**：给卡面 trait/计数那几组建「组根 + 子件」淡入结构（或在 `CardView` 加 `per-group alpha` 广播），对齐原版「组根 alpha → 子树 SR/TMP」。
- ⚠️ 可达性：`BoardTraitIcon.fadeGroup` 是**触发类图标的淡入**（战斗里词条触发时亮/灭）⇒ **真机可见**，不是休眠机制。

## 五、结论落账口径

- 17 条 `NestedFadeGroup*`（特效动画.tsv B 行）判 **B / 要做（结构缺）**，**不是 D**。
- 精确施工（形态，需调度台派活时细化）：
  ① 卡面 trait/计数几组各建一个「组根 alpha」；② 子树 SR/TMP 订阅组根（等价 `NestedFadeGroupSpriteRenderer/TMP.SubscribeToParent`）；③ `BoardTraitIcon` 一类按 `fadeGroup` 只推该组 alpha；④ 断言：「只淡某组 ⇒ 别的组 alpha 不变」（灭自证：整卡单标量时这条必红）。
- 原版树证据路径 = 本文 §二/§三（`bundle_battleprefabs_vfxandmisc_assets_all`）。
