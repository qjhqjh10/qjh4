# W2 — `DeckInfoPopup` 行底纹稀有度 tint 与 `x2` 复核（执行代理 · 2026-10-18）

> 施工单：第二轮实机验收两处视觉缺陷（行内稀有度渐变呈近白条 / `x2` 被裁）。
> 白名单：`Shell/DeckInfoPopup.cs`（本件只动了这一个 `.cs`）。
> ⛔ 没跑 Unity / `*.Run`；⛔ 没动 git；⛔ 没碰两张正本。
> ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_f5 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**。

---

## 1. 两张图的像素真值（python 直接读 PNG，非猜测）

### `Resources/Art/ui_deck/40k_deck_cardlist_bg.png`（318×54 · RGBA8）

| 采样点 | RGB | α |
|---|---|---|
| TL (0,0) | `#A89E88` | **0** |
| TR (317,0) | `#A89E87` | **0** |
| BL (0,53) | `#AD9B7D` | **0** |
| BR (317,53) | `#A89E87` | **0** |
| 中心 (159,27) | **`#0C0C0A`** | 254 |
| (79,27) | `#1A1914` | 255 |
| (238,27) | `#040402` | 254 |
| (1,27) | `#A89E87` | 255（左沿一圈倒角高光） |
| 全图均值 | **(31.2, 29.6, 24.3)** | **243.5** |

⇒ **这张图本身是近黑的**，白 tint 出来就是原版那个暗行底。**它不是问题所在。**

### `…/40k_deck_cardlist_bg_rarityColorGradient.png`（114×54 · RGBA8）

| 采样点 | RGB | α |
|---|---|---|
| 四角 | **`#FFFFFF`** | **0** |
| (28,27) | `#FFFFFF` | 0 |
| (57,27) | `#FFFFFF` | 32 |
| **(85,27)** | `#FFFFFF` | **127（峰值，x≈75%）** |
| (112,27) | `#FFFFFF` | 4 |
| 全图均值 | **255 / 255 / 255** | **60.8** |

⇒ **纯白 + 一条 alpha 坡**（左 0 → 75% 处 0.5 → 最右回落到 ~0.02）。
白 tint 出来 = **一条近白条** ⇒ 这就是本轮「行右段 `#F9F9F8`」的**直接原因**。
**它必须由稀有度色染色**（见 §2）。

### `…/40k_deck_cardlist_border.png`（11×11 · RGBA8）

RGB **全 255**；α：外圈 255（(1,5)/(9,5) = 255，四角 0～1），内部 (5,5) = **0**；全图 α 均值 **57.1**。
⇒ 也是一张**纯白描边图**，同样按稀有度染色（见 §2）。

---

## 2. 稀有度 → 颜色表（每档带判据出处）

### 判据链（三步都可复查，**不是参考图反推**）

1. **谁上色** —— `d:/2/tools/decomp_full/UICardInfoItem__Initialize.c:139-156`：
   ```
   uVar6 = PlayerItem__get_Rarity(param_2, 0);
   puVar9 = CardRarityColorsSO__GetColor(&local_48, *(long long *)(param_1 + 0x50), uVar6, 0);
   lVar7 = *(long long *)(param_1 + 0x48);
   for (i = 0; i < 数组长; i++) (**(code **)(*plVar1 + 0x2a8))(plVar1, color);   // Image.color = color
   ```
   `PlayerItem__get_Rarity.c` 读的是 `+0x28` → `SupportMethods.GetCardRarityFromInt`；
   `ObtainableItem.cs` 那个偏移上的字段就是 **`public int cardRarity`**。
2. **哪两颗** —— `_probe_deckinfo.py bundle_menus_assets_all "Deck Selector Card Info button"
   --class UICardInfoItem --pid 2994867481076140456`（= `Deck info Popup ▸ Info Panel ▸ Deck List ▸ Content`
   那一颗，本包同名 8 颗里的这一颗）现读：
   ```
   imagesToChangeColorByRarity = [→GO「Rarity Gradient」, →GO「Background Border」]
   colorsByRarity              = →pid -2272266055686004801
   ```
   ⇒ **`Background`（行底）不在名单里** ⇒ 它保持 prefab 的 `m_Color = (1,1,1,1)`。
3. **哪张表 / 色值** —— `colorsByRarity.m_PathID = -2272266055686004801` 在真包
   `D:/2/unity_run_ref/.../menus_assets_all.bundle` 里用 UnityPy 按 pid 反查出
   **`m_Name = CardUIDisplayColorsSO`**（⚠️ **不是** `bundle_menus_assets_all/MonoBehaviour/CardRarityColorsSO.json`
   —— 那是同一 `m_Script` 的**另一个实例**，pid `764426219195092032`）。
   数值 = `…/MonoBehaviour/CardUIDisplayColorsSO.json` 的 `colors[]` 六档逐条现读。
4. **档号 ↔ 名字** —— `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs`：
   `None 0 · Common 1 · Rare 2 · Epic 3 · Legendary 4 · Special 5`。

### 表（写入 `DeckInfoPopup.RarityColorsBySo`）

| tier | `CardRarity` | SO 原值 (r,g,b,a) | 叠在行底上的色相 | 判据 |
|---|---|---|---|---|
| 0 | None | 0.509434, 0.509434, 0.509434, **1.0** | 灰 | `CardUIDisplayColorsSO.json` `colors[0]` |
| 1 | Common | 0.158419, 0.281239, 0.377358, **1.0** | 深蓝 | 同上 `colors[1]` |
| 2 | Rare | 0.188661, 0.349057, 0.074092, **1.0** | 深绿 | 同上 `colors[2]` |
| 3 | Epic | 0.415094, 0.178177, 0.395885, **1.0** | 紫 | 同上 `colors[3]` |
| 4 | Legendary | 0.792453, 0.438180, 0.0, **1.0** | 橙 | 同上 `colors[4]` |
| 5 | Special | 0.622642, 0.072626, 0.055803, **0.639216** | 暗红 | 同上 `colors[5]` |

**作用对象**：`Rarity Gradient`（`ImageQuad.SetTint`）+ `Background Border`（`Nine(..., tint)`），
**行底 `Background` 不上色**（不在这两颗名单里）。

### ⚠️ 两个如实记的保留项（不是猜，是「还没核透」）

1. **量纲 gamma vs linear（未决）**：本工程端到端按 **gamma** 解释颜色
   （实测：卡名 `SetColor(0.934)` 的实拍最大像素 = `0xEEEEED` = **238** = `0.934×255`）。
   同类兄弟 SO `CardRarityColorsSO` 的数值**恰好是 n/255**（`0.5647059 = 144/255`、`0.1607843 = 41/255`）⇒ 那份坐实 gamma。
   本表数值**不是** n/255；按 **linear→sRGB** 折过去，五个色相与游戏自带的军衔图
   `ui_deck/{1..5}_40k_cardframe_rarity_*.png`（均值 `#7EA7AA` / `#44E04C` / `#AD49DE` / `#EBBE4E` / `#E97742`）
   **对得上**（Epic 的 R：173 vs 173；Legendary 的 R：231 vs 236）。
   **本笔按 gamma 落**（与同类 SO、与 `DeckRuntime` 既有口径一致），线性读法记在代码注释里备查。
2. **`DeckRuntime.cs:8362 RarityColorsBySo` 用的是【另一份 SO】**（`CardRarityColorsSO`，Common 的 α = 0、
   Legendary 的 r = 1.28）。按 prefab 字段，卡组编辑窗与收藏窗的行**也**该用 `CardUIDisplayColorsSO`
   ⇒ **那处是缺陷，本笔不在白名单内、没动它**，已登记等调度台派单（改完后两处应收口到同一份表）。

### 🔴 与参考图的出入（必须让下一轮知道）

参考图 `C:/Users/qjh36/Desktop/warpforge参考图/选择卡组出现的界面参考.png`（1577×888 = 1920×1080 ×0.82135）
与 `卡组编辑界面参考.png` 里，**所有行的右侧渐变都是同一种暗红**，与卡片的稀有度**无关**：

| 行（列 0） | 我方数据 `rarity` | 参考图梯度区均值（设计 px 250–315，避开字） |
|---|---|---|
| 游侠 | rare | `#2A1210` |
| 驭风者摩托 | epic | `#281211` |
| 女妖面具 | rare | `#281511` |
| 灵活机动 | common | `#291514` |
| 邪术风暴 | rare | `#291513` |

五行**逐位近似相同**（R 38–48 / G 18–28 / B 16–26），**没有任何蓝/绿/紫的痕迹**。
而按上表，`Rare` 应为深绿 `(0.19,0.35,0.07)`、`Legendary` 应为橙 —— 与参考图**对不上**。
参考图是**视频截帧**（右下角有播放器水印）、且 `R_DeckInfoPopup_对账.md` 已记「参考图与 bundle 有版本差」；
本地 bundle 是最新 build、prefab 字段逐条可查 ⇒ **本笔以反编译 + 资产为准**。
**这一条没查清（参考图那一版到底用什么色），如实挂账，⛔ 别拿它推翻 §2 的表。**

---

## 3. 改动清单（`Shell/DeckInfoPopup.cs`）

| 行 | 改前 | 改后 |
|---|---|---|
| `:1636-1639` | 注释写「稀有度六档 tint 没逐档核 ⇒ 三张**先全上白**」 | 删掉那句过期结论，换成「谁上色 / 哪两颗 / 哪张表 / 色值」四步判据 |
| `:1662` | —（无） | `var rarityTint = RarityTint(card.Rarity);` |
| `:1665-1667` | `Img(… "Row Rarity Gradient", QDIRowArt, false);`（白 tint） | `var gradQ = Img(…); if (gradQ != null) gradQ.SetTint(rarityTint);` |
| `:1668-1669` | `Nine(… "Row Border");`（不传 tint ⇒ 白） | `Nine(… "Row Border", rarityTint);` |
| `:1663-1664` | `Row Bg` 白 tint | **不变**（原版 `imagesToChangeColorByRarity` 不含它） |
| `:1810-1857` | —（无） | 新增 `RarityColorsBySo` 六档表 + `RarityTier(string)` + `public static Color RarityTint(string)`（带完整判据 doc） |
| `:1740-1751` | Count 的注释只到「框宽 22.87 / 颜色 .933」 | 追加 W2 复核结论（见 §4） |

`git diff --numstat` = **340 / 48**（绝大部分是注释；行尾未被翻）。

---

## 4. `x2` 的修法与理由 —— **实测没被裁，是对比度假象**

**结论：不裁几何。** 把 `_tmp_view/shots/2026-10-18_卡组选择弹窗_第二轮/02_deckinfo.png`
（2560×1440，设计 1920×1080 的 1.333×）按 3× 放大逐像素读：

- `x` 与 `2` **两笔都完整、没有任何一刀切边**；
- 「只看见 ×」的原因是**它脚下那条 `Rarity Gradient` 当时被染成近白 `#F9F9F8`**
  ⇒ **白字压白底，`2` 溶进背景**。同屏量到的行底右沿 = 设计 `x+358.5`（≈360），
  `2` 的墨迹右沿 ≈ 设计 `x+348.0` ⇒ **离行右缘还有 ~12px**，装得下；
- 纵向：行 `y 400→468`（屏幕），字墨 `425→443`，**中心重合**（`Midline` 档生效）。

原版规格本来就对、我们也一直是这么传的：
`fs32 · auto min2/max32 · base36 · Right/**Midline** · 框 22.87 · 右沿 x+351.60`
（`Txt(..., autoMinPx:2, autoMaxPx:32, autoBasePx:36, vAlign: Midline)`）——
`SetAutoFitBox` 把 `x2` 压到 ≈22.5px 宽，确实收进了 22.87 的框里（放大图实测墨迹宽 ≈19.5 设计 px）。

⇒ **第 1 条修好（行底变暗）后 `x2` 自然可见**；⛔ 不许为了「让它看见」把框加宽（22.87 是原版 `m_SizeDelta.x`）。
这条已写进 Count 的调用点注释（`:1740-1751`），供下一轮并排比。

---

## 5. 没查清 / 待办

1. **参考图那两版预算什么色**（§2 末）：两套参考图里所有行都是同一种暗红、与稀有度无关；
   本地资产却是蓝/绿/紫/橙/红五档。**没查清**，按「反编译 + prefab + SO」落。
2. **SO 色值的量纲**（gamma vs linear）：按 gamma 落，linear 读法的旁证（军衔图五个色相）记在代码注释里。
3. **`Deck/DeckRuntime.cs:8362` 用错 SO 实例**（`CardRarityColorsSO` 而非 `CardUIDisplayColorsSO`）
   —— 同一颗 prefab 字段已证；**不在本次白名单**，请调度台派单收口（收口后本类的表应改为转调它，避免两份）。
4. **`40k_deck_cardlist_border` 染色后是否与参考图一致**：参考图行边界那两条亮线实测 `#745C5A`（视频压缩糊过），
   判不出是不是稀有度色 ⇒ 按 prefab 字段（它**在** `imagesToChangeColorByRarity` 里）染色，**如实挂「未与参考图逐像素核」**。

### typecheck 原文（最后一次）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（同一轮里前一次跑出的 5 条错全部落在**别人的在写文件**上 —— `RuleBuilder`/`ReplayStore.cs` 的 `LegacyCardIds.MigrateDeck`、
`ShopScene.cs` 的 `TmpFont.Face`；隔 45 秒复跑即 0，不是本笔引入的。）
