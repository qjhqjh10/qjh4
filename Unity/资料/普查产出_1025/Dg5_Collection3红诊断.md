# Dg5 · `CollectionScene` 3 红诊断（2026-10-25 09:09 全量批）

## 现象

逐字见 `_tmp_view/20261025/b_collection9.log:161579 / :161594 / :161609`：

```
✗ 量得到这一格的文字网格（0 个顶点）
✗ **有 0 个顶点落在渐隐带里**（y > 805.46）—— 0 个 = 软边没接上
✗ 带内的字**确实被削了 alpha**（最小 255 < 250）—— 只夹顶点不削 alpha 是硬边
```

三条是**同一条因果链**：`nV=0` ⇒ `nBand=0` ⇒ `minA` 停在初值 255。紧随的 `nBad==0` / `nBadBelow==0` 因空集**恒真**（log `:161624` / `:161638`）—— **假绿，不是证据**。

## 1. 测的是哪一格 / 渐隐带判据

- 节点：`Player Profile Window ▸ Title Tab ▸ Item Display Panel ▸ Scroll Rect ▸ Item Drawer ▸ TitleDrawer_9 ▸ Content ▸ Title`；构造点 = `Shell/TitleTab.cs:332`（格）/ `:383-387`（`Title` 标签：`face: LatinUi` + `wrap:true` + autoFit）。
- 文本 = `items[9].Name` = **「幸存者」（3 个汉字）**：`Resources/profile_cosmetics.json` 的 `titles[9].id` = `2d27ce49c21f03946a02dadae47d82f8`，`Resources/official_names.json` 该键 `zh` = 「幸存者」。⇒ **与旧绿读数吻合**：`_tmp_view/w73_collection_new.txt:160849` 正是 **12 个顶点** = 3 字 × 4 角。
- `805.46 / 855.46`（`Editor/CollectionScene.cs:10519-10523` / `:10538-10544`，两页同矩形 `:10618-10619`）：原版 prefab 视口矩形 `654.16,210.69 → 1680.12,855.46`，其 `RectMask2D.m_Softness = (0,50)` ⇒ 下侧**带内沿** = 855.46 − 50 = **805.46**、**硬裁边** = **855.46**。断言读的是 TMP **模型**（`textInfo`），不是上传网格。

## 2. 「0 个顶点」卡在哪 —— 代码链

1. 档案窗六页**开窗时全建好**、只有默认页 active：`Shell/PlayerProfileWindow.cs:483-501`（`SetActive(i == DefaultTabIndex)` 在循环末，**第二圈**才逐页 `Setup()`）。
2. ⇒ `TitleTab` 的每格/每个 `Title` 标签都在**未激活父链**下建（`TitleTab.cs:332-387`）。
3. 未激活时 `Label.SetTextTmp` 的 `_tmp.ForceMeshUpdate()` **是空操作** —— `Battle/Label.cs:1604`；`TextMeshPro.ForceMeshUpdate → OnPreRenderObject` 首句 `if (!m_isAwake || (this.IsActive() == false && …)) return;`（`TextMeshPro.cs:347-352` / `:2117-2119`）。`SetWrapWidth` 此时只把「生成」记成待办 `_pendWrapW`（`Label.cs:641-649`）。
4. 唯一补做点 = **激活那一刻**的 `Label.OnEnable → TryApplyPendingWrap`（`Label.cs:666` / `:673-681`）→ `TmpFont.GenerateLayout`（`Core/TmpFont.cs:572-576`）= **只跑一遍 `GetTextInfo`**（不收敛）。
5. 本批这一跑里第 4 步**没落下网格**：`textInfo.characterCount` / `characterInfo` 全空 ⇒ `CollectionScene.cs:10567-10601` 的循环一次都没进，`nV` 停在 0。日志**无任何异常**（`NullReference`/`Exception` 全 0 命中）⇒ 不是生成时抛错，是**根本没生成**。

**病灶 = 「延后生成」链的第 4 步**。渐隐带/软边逻辑没坏（那三条只是它的下游）。

## 3. 归属判定：γ 本批回归（触因在 `f84cfff2` 的改动集）—— 但**不是字体换源**

- ❌ **先排掉「换源缺字形 / 度量变了」（已排除）**：fontTools 现读 `Unity/素材/Fonts/NotoSerifCJK-Regular-JP.ttf` —— `幸存者` 三字**全在 cmap**（cid17175 / cid15549 / cid33058），`advance = 1.0 em`，`hhea` (1151, −286, 0) ⇒ 行盒 1.437（= 资产 `m_LineHeight 71.85 / m_PointSize 50`）。
- 资产关键字段与换源前**逐项相同**（`git show f84cfff2^` 对读）：`m_AtlasPopulationMode 1`(Dynamic) · `m_GlyphTable []` / `m_CharacterTable []` · `m_SourceFontFileGUID fd6e3705…` · `m_PointSize 50` · `m_Scale 0.9` · atlas 2048² · padding 5 · 连字 232 条；头部唯一实差 = `m_FamilyName SC→JP`。
- 日志里字体类告警**一条都没有**（`[TmpFont] 找不到字体资产` / `Can't Generate Mesh` / `missing` / `fallback` 全 0 命中）。⇒「换源只改字形、不改度量」成立 ⇒ **它不可能是 0 个顶点的原因**。
- ✅ 同提交里**唯一**动了本窗行为的代码 = `Shell/AvatarTab.cs:395-415`：删掉 `if (!_scroll.Intersects(r)) continue;` ⇒ Avatar 页从「只建可见格」变成**建满 469 格**（注释自陈）。本断言现场次序正是「点 Avatar 页（`CollectionScene.cs:10532`）→ 点 Title 页（`:10547`）→ 立刻读 Title 格」⇒ 它把 469 个 label（大量汉字名）**在 Title 激活前一拍集体激活**，是与「延后生成链」同窗、同批次的唯一变化。
- ⚠️「Atlas / 子材质耗尽」只是**候选**，我没有证据（日志无相关告警）—— 不当结论写。

## 4. 最小修法（只给建议，⛔ 未动手、未跑）

1. **改测法 + 补前提**（`Editor/CollectionScene.cs:10558-10602`）：读 `textInfo` 之前先 `ttLb.GetComponentInChildren<Label>(true).ForceRelayout();`（`Label.cs:911-928`：已激活档走 `SetFontSize(现值) → ForceMeshUpdate` ⇒ 走 `OnPreRenderObject` 的**完整自适应收敛循环**，与第 4 步的单跑一遍不同）；并把前提拆成 **`CharacterCount > 0`**（模型排出来没有）与 **`nV > 0`**（网格有字没有）两条 ⇒ 出红即自证是哪一档。
2. 若证实是「469 格那一下」打断生成链：**修「激活时生成」这条链**（让 `TryApplyPendingWrap` 失败可重试 / `OnEnable` 后再补一次 `ForceMeshUpdate`），⛔ **不是**把 `AvatarTab` 改回「只建可见格」—— 那一句照的是原版（`AvatarTab__InitializeGameObjects.c:140-160`），本身是**去掉一处偏离**。

## 5. 不能判的（要一条 Unity 探针；本代理 ⛔ 不跑 Unity）

- 第 4 步是被 `_pendWrapW < 0` 早退、`_tmp.isActiveAndEnabled == false` 早退、还是单跑一遍确实排空 —— 三者产出同一个「0 顶点」。一行探针即可定：在 `:10602` 前打 `ttTmp.text` / `textInfo.characterCount` / `ttTmp.isActiveAndEnabled` / `Label.PendingWrapAppliedCount`（`Label.cs:950`）。
- 是否与 TMP 动态图集 / 子材质槽（469 格新开 `m_subTextObjects`）有关：**查不到**，日志无任何相关告警。
