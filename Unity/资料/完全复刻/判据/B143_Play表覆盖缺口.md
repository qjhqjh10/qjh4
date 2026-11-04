# B143 · `Play` 表覆盖缺口复核

> ✅ **已修复（2026-11-05 · `B143-R1`）**：`gen_card_anim_order.py` 的 `cardAbilities` 源已从 2022 快照换到**当前 build SO 导出**（`cards_raw.json`），并让基底外卡补 `activeAbility + cardAbilities` 两路。
> **换源后总数**：trigger **807 卡 / 1,024 组 / 1,319 raw / 1,309 可播拍**（807 = 803 有拍 + 4 张全空 `GOF14/GOF21/TL64/UM21`；1,024 是 (ability,logic) **组数**，ability 级行数才是 **928**）· play **535 卡 / 659 行 / 802 raw / 796 拍**（`UM39` 已剔除、`TL64` 空行保留）· `unresolved_guid` **0**（601/601 CardAnim 全解出）· `--diff-old` 旧桶 534 ⊆ 新 play、差集仅 `TL64`。
> **「没动 active」的四条等价口径**（「全量件 `cards[]` 逐字节相同」字面不可能，`cards[]` 本身含 cardAbilities 行）：① 每张卡 `activeAbility` 投影 **0 差异**；② 有 active 行的卡 old/new **都是 136 张**；③ 新增 **104 卡无 active 行**；④ **active 瘦身版 `cards[]` 逐字节相同 = True**。
> **三个「新增」数口径不同（复核 `B143-R2` 后订正 · 2026-11-05）**：`_coverage.augment.augmentNewCardAnims = 63` 是 `len(cardAnims) − len(2022 基底解析集)`（601−538，**相对基底**，⛔ 不是相对旧表）；相对**换源前盘上那一版** = 旧 551 → 新 601 = **新增 51 条 cardAnims / 移除 1 条**（`Atk_BulletImpact_Tyranid_Fleshborer`）；其中**新 cb guid = 60 个**（9 个原本已被 `activeAbility` 引用）。三个数**别互相校验**；已写入全量件 `_coverage.augment.augmentNewCardAnimsBasis`。
> 下面保留**换源前**的取证（结论「漏 61 张」是当时的真缺陷，现已被本次修复覆盖）。
>
> 只读复核代理 `B143-R1` · 2026-11-05 · ⛔ 未改代码/账、未写 `d:/2`
> 对象：`B143-W1` 的 `Resources/card_anim_order_play.json`（475 卡 / 579 行） vs 旧 `play` 桶（534 卡）
> 判据源：**原版卡 SO 直读**（`UnityPy` typetree，见 §一）· `decomp_full` · `cards_engine.json` · `card_anim_order.json`

## 〇 · 结论（三档）

**🔴 我们漏了 61 张**（play 表），不是「原版本来就没动画」。
- 原版**当前 build** 的卡 SO 里，**534 张池内卡**有 `trigger == 0`（`ThisCardPlayed`）的非空 `cardAnimations`；
  W1 的新表只有 **475 张** ⇒ **缺 61 张 / 69 行 / 97 拍**。
- 旧 `play` 桶的 **534 张与 SO 真值逐张相等**（`pool_t0 − old = 0`）—— 旧桶反而是**完整**的那一份。
- 同一根因还让**触发表**漏得更多：SO 里池内 **803 张**卡有任意 trigger 的动画，全量件只覆盖 **722 张** ⇒ 缺 **81 张 / 98 行 / 137 拍**。
- 另有 **2 张被多收**：`UM39`（全量件里 trigger 0 的两拍是 **2022 快照的陈旧数据**，当前 SO 没有 trigger 0 行）· `TL64`（当前 SO 的 trigger 0 行两拍全是空 GUID，原版不播；收进来无害但不该算「有 play 动画」）。

---

## 一 · 判据源与读法（先解决「去哪读 SO」）

- 🔴 **`assets_full` 里没有卡 SO**：`bundle_*cardassets_*` 下只有 `AssetBundle/AudioClip/Sprite/Texture2D`（美术），
  `bundle_*cardanims_*` 是 `CardAnim` 资产；**卡的 `MonoBehaviour`（`cardAbilities` 所在）不在这棵树里**。
- **卡 SO 的真身** = `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1114a2de41519cac560a4bce4/ea8bca4f5c6909d3124fd4975dc96173/__data`
  （= `工具/export_allcards.py` 的 `DEFAULT_BUNDLE`，1,351 张；`cards_raw.json` 就是它 2026-10-18 的导出）。
- 本次**没有信 `cards_raw.json`**：用 `UnityPy.load(bundle)` → 逐对象 `o.read_typetree()`（1,360 对象），
  按 `uniqueId` 建表，读 `cardAbilities[].abilityLogic[].cardAnimations[].m_AssetGUID`。
  ⇒ 独立重读的结论与 `cards_raw.json` 对 61 张**逐张一致**（两源交叉通过）。
- 池子口径：`cards_engine.json` 1142 条 − **13 个 `TRANSFORM_*` 合成卡** = **1,129 张真卡**
  （那 13 个不在 SO 里，这也正是「1129」这个分母的来历）。

---

## 二 · Q1：全量件为什么只覆盖 726/1129？

### 2·1 真值（SO 现读）

| 口径 | 数值 |
|---|---|
| 池内真卡 | **1,129** |
| SO 里**任意 trigger** 有非空动画的池内卡 | **803** 张 |
| SO 里 **trigger == 0** 有非空动画的池内卡 | **534** 张 |
| 全量件 `card_anim_order.json` 覆盖的池内卡（有 `cardAbilities` 行） | **726** 张（其中真有动画的 **722**） |
| 全量件**缺**的「有动画」池内卡 | **81 张 / 98 行 / 137 拍** |

⇒ **不是原版只有 726 张有动画**；是 **803 → 722**，且缺的 81 张在 SO 里**确有非空 `m_AssetGUID`**。

### 2·2 根因（现读生成器 + 两源对照）

`工具/gen_card_anim_order.py` 的 `cardAbilities` 那一路**只从 2022 快照取**：
- `_truth_rows(truth)`：`TRUTH = d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（**2022 快照**）；
- `_augment_rows(raw, base_ids)`：对**基底没有的卡**只补 **`activeAbility`**（注释写明「`cardAbilities` 不补」）。

⇒ 2022 之后新增 / 新增了动画的卡，`cardAbilities` 永远进不了全量件。实测对 81 张的分类：
- **78 张根本不在 2022 快照里**（新卡）；
- **3 张在快照里、但 2022 时 `cardAbilities` 动画为空**（`851f73da…` · `ASH12` · `SOR4`），当前 SO 才有 —— 属**同一类数据漂移**，不是另一条 bug。

> 另：池内还有 **326 张（1,129 − 803）卡在 SO 里确实一条动画都没有** ⇒ 它们的缺失是**正确**的，
> 不要把它们算进缺口（这也是「726/1129」这个分母最容易误读的地方）。

---

## 三 · Q2：那 4 张（有动画但无 `trigger==0` 行）打出时播不播？

**播。** 当前 SO 的四张都有 trigger 0 的非空动画：

| 卡 | 当前 SO 的行（trigger, rows, 非空拍） | 全量件里有什么 | 缺口原因 |
|---|---|---|---|
| `ASH52` Hosts of the Dead | **(0, 1, 1)** · (600, 1, 1) | 只有 trigger **600** | 2022 快照只有 600；当前 build 新增 trigger 0 |
| `GOF26` Stompa | **(0, 1, 1)** · (81, 1, 2) | 只有 trigger **81**（2 拍） | 同上 |
| `SW29` Morkai Eliminator | **(0, 1, 1)** | **无 `cardAbilities` 行**（只有 `activeAbility`） | 2022 无此卡 ⇒ 增量只补了 `activeAbility` |
| `UM89` Incursor | **(0, 1, 1)** | **无 `cardAbilities` 行** | 同上 |

### 3·1 `.c` 判据链（打出 ⇒ trigger 0 ⇒ 播卡动画）

1. **打出**：`CardScript.NonTargetSpellPlayed` / `ResolveCardPlayed` 等正常出牌路径调
   `RawCardScript.OnCardPlayedWithTarget(...)`（IL2CSharp `CardScript.cs:5887 / 5985 / 7975`；
   `CardScript__TargetedSpellPlayed.c:66` 另走 `OnCardPlayedWithTargetJustThisCardPlayed`）。
2. **trigger 0**：`RawCardScript__OnCardPlayedWithTarget.c:43`
   `RawCardScript__TriggerAbilities(param_1, 0, …)`（`JustThisCardPlayed.c:39` 同形）。
3. **遍历命中行 → PlayAbility**：`RawCardScript__TriggerAbilities.c:108/207` 的
   `CardAbility__CanTriggerAbility → RawCardScript__TriggerAbility`；`TriggerAbility.c:89/103`
   → `AbilityLogic__PlayAbility`。
4. **播**：`AbilityLogic__PlayAbility` 内对 `abilityLogic.cardAnimations[]` 调 `PlayAnim`（判据 → 现有
   `card_anim_order` 文件头；`PlayAnim` 外层 enumerator = `param_1 + 0x18`）。

⇒ **没有 trigger 0 行的卡**在这条链上**一拍都不播**（它们的动画属于别的时机：600 灵魂石 / 81 / 410 等）；
⇒ 有 trigger 0 行但 `m_AssetGUID` 全空的（`TL64`），`PlayAnim:62-64` 逐拍跳过 ⇒ **也不播**。
⇒ 所以「play 表应等于 SO 里有 trigger 0 非空动画的卡」= **534 张**。

---

## 四 · Q3：缺口清单

### 4·1 play 表缺的 61 张（= 旧桶独有；与 SO 的 534 − 新表 475 逐张相等）

```
GUID 形式（6）: 38871853fabb5244d8148766829230cd  40667e346ae58a84dbda1033dff70ccb
              4d4b9170663891f488c0e96ee8e55ba1  4de4fcfb5d7e0554a88b9f419a2cd7c2
              7a01882fe4610244292d16b15f4b873e  851f73daf747eb642934a16429b391db
短 id（55）: ASH52 DA72 DA73 DA74 GOF26 SW12 SW14 SW18 SW19 SW20 SW28 SW29 SW30 SW33
            SW37 SW39 SW40 SW41 SW42 SW43 SW44 SW45 SW46 SW47 SW48 SW49 SW50 SW52 SW53
            SW54 SW55 SW56 SW57 SW58 SW59 SW60 SW61 UM109 UM110 UM111 UM112 UM113 UM114
            UM115 UM116 UM117 UM118 UM89
            a51adc8f5c311584385c3117cfd919e9 b14d1db2b0730d649b3bec35af100363
            b3dce3f10cc118541910156ec8da1d9b b551c8351ebe5784faf9912b573eaf93
            e3b5735d801fcbd49aa8d3975a5b0c31 e874c2bd84895e64694daa44bd5fd5c2
            ef596edae1828bd46bfc4348535a3acc
```
（其中 **ASH52 / GOF26 / SW29 / UM89** = §三 那 4 张；**851f…** 等 6 个 GUID = 池内旧 id 的卡。）

- 合计 **69 行 / 97 拍**（只算非空 `m_AssetGUID`）。
- **旧桶 534 ⊇ 这 61**，且旧桶与 SO 的 534 **完全相等** ⇒ **可以直接用旧桶当兜底/校验源**。

### 4·2 被多收的 2 张

| 卡 | 现表 | SO 现读 | 判定 |
|---|---|---|---|
| `UM39` Inspired Retribution | trigger 0 · 2 拍（`56dd62cd…` / `8cc52be2…`） | **没有 trigger 0 行**（只有 410 一拍） | 🔴 **陈旧数据**：那 2 拍来自 **2022 快照**，当前 build 已没有 ⇒ 按现表会**多播 2 拍** |
| `TL64` Ravenous Hunter | trigger 0 行、2 拍但 `ca` 全空 | trigger 0 行、2 拍 `m_AssetGUID` 全空 | ⚪ 无害（`PlayAnim` 跳过 ⇒ 0 拍）；按「卡」计数会多 1，按「拍」不多 |

### 4·3 触发表的连带缺口（同一根因，供调度台排期）

- 全量件缺 **81 张 / 98 行 / 137 拍**（任意 trigger）。名单 = §2·1 的 81（含 play 的 61 + 20 张只有别的 trigger 的卡）。
- 修好全量件后，**触发表**应从现读 726 卡 / 911 行 / 1,178 raw 拍，升到 **803 卡 / 928 行（非空）/ 1,309 拍**
  （SO 现算，含空 `ca` 的行的取舍同现有规则；若把 5 行「有 steps 但全空」也算行，则 raw 行 = **933**）。

---

## 五 · 修复建议（⛔ 本代理不改代码）

1. **换源**：`gen_card_anim_order.py` 的 `cardAbilities` 一路**改为读当前 build 的 SO 导出**
   （`cards_raw.json` / `export_allcards.py` 的产物）——这是**一处改动**，同时修好触发表与 play 表。
   ⛔ 不要再「只给 base 缺失的卡补 `activeAbility`」。
2. **重生成两张表**：`gen_card_anim_order_trigger.py --write` → `gen_card_anim_order_play.py --write`；
   预期：trigger0 = **534 卡**（或 532 + TL64 空行口径）、任意 trigger = **803 卡**。
   ⚠️ 新引入的 `CardAnim` GUID 要复核 `unresolved_guid`（`cardAnims` 来自 `assets_full`，大概率齐；现读 0 未解）。
3. **在换源前**：W1 的 play 表**不可当完整**用；消费端 fallback 应回落到旧桶
   `数据/游戏数据/card_vfx_by_card.json` 的 `byCard[id]["play"]`（534 卡，与 SO 逐张相等）。
4. **UM39 需从现表剔除**（除非换源后 SO 重新出现 trigger 0 —— 现读没有）；`TL64` 可留（0 拍）。
5. 回归判据：`--diff-old` 应变成「旧桶 534 ⊆ 新表，差集 = {TL64}（空行）+ UM39 若换源后仍保留则另议」；
   任何 `pool_t0 − new` ≠ 0 都是回归。

---

## 六 · 边界（没查 / 不能查的）

- **没查**「SO 的 `cardAnimations` 引用指向的 `CardAnim` 资产是否都能在 `assets_full` 的 `cardanims` 包里解出」——
  这属换源后的 `unresolved_guid` 校验（现表 0 未解，但新引入的 61 张要重跑才知道）。
- **没查** `playerHand` / 化身 / 教程脚本等**别的入口**是否也会触发 `ThisCardPlayed`（本复核只按正常出牌链）。
- `cards_raw.json` 与本次 SO 直读逐张一致（61 张），但 `cards_raw.json` 在 `.gitignore` 里 ⇒ 换机器要用
  `export_allcards.py` 重导（或依赖旧桶当兜底）。
