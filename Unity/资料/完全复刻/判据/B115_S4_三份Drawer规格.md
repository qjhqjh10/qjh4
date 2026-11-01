# B115_S4 · 三份 `m_AspectMode 2` 的 Drawer 规格（`Base` / `Deck` / `Wildcard`）

> **产出代理**：`B115_S4`（只读查证，2026-11-01）· **消费者**：`B-293` 的执行写手（下一波）
> **任务**：`活状态表.md:381` 的 `B-293` —— 「`Base Drawer` / `Deck Drawer` / `Wildcard Drawer` **我们根本没按 prefab 画**」
> **上游判据**：`报告/B114_W47_ARF迁移.md` §七-4（`B-293` 从那儿拆出来的，本文件把它落成可施工的规格）
> **本文件只写「原版长什么样 + 我们差在哪 + 怎么补」**，⛔ 不含任何代码改动（本代理只读）。

---

## 〇、一句话结论

| prefab | 原版是什么 | 我们的现状 | 差多少 | 建议 |
|---|---|---|---|---|
| **`Wildcard Drawer`** | **活的**（战役奖励 / 锻造奖励 / 战斗奖励三处**真在显示**）：712×1080 设计帧，底图铺满 + 阵营徽记横带（下沿）+ 名字条 + 阵营名 | `Wildcard()` 画的是**我们自己挑的版式**：居中方形主图（`min(W,H)×0.7`）+ 居中方形徽记（`×0.34`）+ 顶部两条自造名条 | **整版错**（不是偏移几 px） | **可**复用现成件重写，工作量小（≈`Container` 那份的形状） |
| **`Deck Drawer`** | 配置表 `PrebuiltDeck` 指的那一份：根 ARF 0.66，`Collection Deck` 328×496.97 缩满全根；**`background` 槽 null、`quantity` 槽 null**；`gameModeIcon` **非** null | `Deck()` 已经**基本照 prefab**（6 层锚点逐条抄对了），但**没有帧模型**（锚点乘的是 `box` 而不是「根」），且**多画了数量**、少了 `Content` 容器、少了 mask 裁切 | **小**（1 个帧模型 + 2 处槽位） | 改动面最小，但**今天 0 个实例**（数据里 0 条 `PrebuiltDeck` 发放物） |
| **`Base Drawer`** | **模板**：全库 **1 份**、**0 处实例化**、**没有 `BaseDrawer` 类**（`dump.cs` 只有 `ItemDrawer<T>` / `DeckDrawer` / `WildcardDrawer`） | 我们**没有**这一档（`Placeholder` 是我们自造的兜底，**不是**它在画） | **无可画** | ⛔ **不要写渲染器**；它只做「共享底三层」的规格源 |

> 🔴 **先纠正一条账上写错的话**（见 §九-1）：`活状态表.md:381` 把三份的括号注**串位**了（写成 `Base`=占位板 / `Deck`=我们挑的方框 / `Wildcard`=模板 0 实例化），**正确对应**按 `报告/B114_W47_ARF迁移.md` §七-4 是：`Deck`→占位板（`Deck==null` 时）/ `Wildcard`→我们挑的方块内接 / `Base`→0 处实例化。

---

## 一、判根 + 三条必须先知道的坑

### 1·1 判根方法（照本会话实际做法，可复跑）

根 = **`D:/2/tools/assetripper_out/ExportedProject/Assets/<prefab>.prefab` 里 `m_Father: {fileID: 0}` 的那个 `RectTransform` 所指的 GameObject**。

| prefab | 根 GameObject 名 | 根 `m_Father` | 根上组件（`!u!114`） |
|---|---|---|---|
| `Base Drawer.prefab` | `Base Drawer` | `{fileID: 0}` ✓ | `AspectRatioFitter`(mode 2 / 0.66) + `ItemDrawerComponents` |
| `Deck Drawer.prefab` | `Deck Drawer` | `{fileID: 0}` ✓ | `AspectRatioFitter`(mode 2 / 0.66) + `ItemDrawerComponents` + **`DeckDrawer`** |
| `Wildcard Drawer.prefab` | `Wildcard Drawer` | `{fileID: 0}` ✓ | `AspectRatioFitter`(mode 2 / 0.66) + `ItemDrawerComponents` + **`WildcardDrawer`** |

✅ 本会话现查：`assets_full` 里这三份的 `GameObject/<名>.json` **各只有 1 份**（不像 `Icon Campaign Points Drawer Variant` 有 36 份）⇒ **这三份没有「同名多副本」的坑**。

### 1·2 🔴 坑 A：全库有 **3 个** 叫 `Deck Drawer` 的 GameObject，只有一个是判据

| GameObject PathID | 根上抽屉类 | `background` | `quantity` | `gameModeIcon` | `cardBackDrawer` | 用途 |
|---|---|---|---|---|---|---|
| **`-3438027711880944002`** | **`DeckDrawer`** | `0`(null) | `0`(null) | **非 null**（`4586698872319452798`） | **字段不存在** | ✅ **配置表 `PrebuiltDeck` 指的就是它**（GUID `7ea5632eb059b5b4493c5047a53e1706`，`B109_E11` 已核）⇒ **本文件用它当判据** |
| `Deck Drawer`（无后缀） | `DeckAndCardbackDrawer` | null | null | **null** | `0` | 另一个实例 |
| `Deck Drawer_-2553787718627850794` | `DeckAndCardbackDrawer` | null | null | **null** | `0` | 第三个实例 |

**验证方式（本会话现做，可复跑）**：`bundle_menus_assets_all` 里三个 `GameObject/Deck Drawer*.json` 的 `m_Component` 逐个取 `MonoBehaviour/*.json` → `m_Script.m_PathID` → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pid>.json` 的 `m_ClassName`。

⚠️ **`AssetRipper` 导出的 `Deck Drawer.prefab` = 上表第一行那份**（判据：导出件的根 `ItemDrawerComponents` 里 `background`/`quantity` 都是 `{fileID: 0}`、根抽屉组件的字段是 `showItemInfoOnClick / inspectSound / armyIcon / gameModeIcon` 这**四个**）⇒ **本文件后续所有 `Deck Drawer` 数字都出自它，可信**。
⚠️ 但 AssetRipper 把这颗组件的 `m_Script` 解析到了 **`DeckDrawer.cs`**，而**另两个实例**确实挂 `DeckAndCardbackDrawer` —— 两者字段不同（后者多 `cardBackDrawer`/`deckIcon`）。**判「哪份是哪个实例」看字段列表，⛔ 别看 AssetRipper 上的类名文件。**

### 1·3 坑 B：`Base Drawer` 真的没有运行时实例（本会话复核）

- `assets_full` 全树 `GameObject/Base Drawer.json` **只有 1 份**（= 它自己的定义）。
- `ExportedProject` 全量 `.prefab/.unity` 里搜 `Base Drawer.prefab.meta` 的 guid `00198b7704fdefb48a5d6b695d93501f` ⇒ **0 命中**（只有它自己那份）。
- `dump.cs` 里**没有 `BaseDrawer` 这个类**（`grep "class BaseDrawer"` = 0）。
- 它根上只有**基类**组件（`ItemDrawerComponents`），没有自己的抽屉类。

⇒ **它是「prefab 模板」，原版靠各族的副本/prefab variant 派生**。**没有任何运行时路径会画它** ⇒ 「按 prefab 画 Base Drawer」= **没有交付物**。

### 1·4 坑 C：`Content` 的 `m_LocalScale` 序列化值是 **`(0,0,0)`**

`Base Drawer` / `Wildcard Drawer` 的 `Content`、`Deck Drawer` 的 `Collection Deck` 上，`m_LocalScale` 都写成 `0`。**这不是设计值** —— 它由身上的 `UIScaleToFit` 在运行期算出来（见 §二）。⛔ 别照抄 `0`。

---

## 二、三份共用的帧模型（★ 这是三份里最要紧的一条）

### 2·1 根：`AspectRatioFitter` `m_AspectMode 2` · `m_AspectRatio 0.66`

- 现有判据已收口在 `Shell/ItemDrawerLayout.cs:168-181` 的 `RootFit.HeightControlsWidth`：
  **`K = box.H / 1080`**（宽 = 高 × 0.66，**完全不看框宽**；框比 0.66 更窄时原版**横向溢出**）。
- 三份 prefab 的根锚都是 `(0,0)-(1,1)` + `sizeDelta (0,0)` ⇒ **根高 = 框高、根宽 = 0.66×框高、左右居中**。
- ✅ 复用 `ItemDrawerLayout.cs:187-195` 的 `FrameOf(box, fit: RootFit.HeightControlsWidth)`，⛔ 别新写一套。

### 2·2 `Content` / `Collection Deck`：`UIScaleToFit` 的缩放算式（本会话现读反编译）

`UIScaleToFit` 字段偏移（`dump.cs:114751`）：`fitInParent +0x20` · `targetRect +0x28` · `padding +0x30` · `paddingType +0x38` · `rect +0x40`。
`decomp_full/UIScaleToFit__Fit.c` 的实际算式（**按上面这份偏移逐句读**）：

```
dims = (targetRect.width + padding.x, targetRect.height + padding.y)     // paddingType = 0 (Flat)
scale = min(dims.x / rect.width, dims.y / rect.height)                    // rect = 自己
self.localScale = (scale, scale)
```

三份 prefab 实例值（逐份现读）：`fitInParent: 1` · `padding: (0,0)` · `paddingType: 0`，`targetRect` = **根**（`Base`/`Wildcard` 的 `Content`）或 **`Content`**（`Deck` 的 `Collection Deck`）。

| prefab | 被缩放的那层 | 自身设计尺寸 | `targetRect` | 缩放结果（根 712.8×1080 时） |
|---|---|---|---|---|
| `Base` / `Wildcard` | `Content` | **712 × 1080** | 根（712.8×1080） | `min(712.8/712, 1080/1080) = 1.0` ⇒ 框不动，左右各留 0.4 px |
| `Deck` | `Collection Deck` | **328 × 496.97** | `Content`（= 根，锚拉伸） | `min(712.8/328, 1080/496.97) = 2.17317` ⇒ **正好铺满全根** |

⇒ **`Base` / `Wildcard` 的设计空间 = `Content` 的 712×1080**；`Deck` 的设计空间 = **根**（328×496.97 那层按 ×2.17317 后与根重合）。

### 2·3 由此得到的两个引用尺

- `Base` / `Wildcard`：`Content` 在根里 = **x 0.4…712.4 · y 0…1080**（= `ItemDrawerLayout.cs:129` 的 `DrCx1=0.4f` / `DrCx2=712.4f`，**逐位相同**）。
- `Deck`：`Content` 是**整根**（x 0…712.8），`Collection Deck` 缩完也 = 整根 ⇒ **`Deck` 内部所有锚点直接乘「根矩形」**，`Deck` 设计 px → 根 px 的系数 = `1080/496.97 = 2.173166`。

---

## 三、`Base Drawer` 规格（模板，⛔ 无渲染交付物）

### 3·1 层级（`m_Father: 0` 的根 + 兄弟序 = 画序）

| 序 | 节点 | `m_IsActive` | 说明 |
|---|---|---|---|
| 0 | **`Base Drawer`**（根） | 1 | ARF 2·0.66 + `ItemDrawerComponents` |
| 0·0 | `Content` | 1 | `UIScaleToFit` + `UIEffectTogglerController` |
| 0·0·0 | `Content/Background` | 1 | 底图（`background` 槽） |
| 0·0·1 | `Content/Image` | 1 | 主图（`image` 槽） |
| 0·0·2 | `Content/Label` | 1 | 名条底板 + `HorizontalLayoutGroup` |
| 0·0·2·0 | `…/Quantity` | 1 | TMP（`quantity` 槽） |
| 0·0·2·1 | `…/Name` | 1 | TMP（`label` 槽） |
| 0·0·3 | `Content/Converted Drawer` | **0** | 调用方 `SetConvertedItem` 才开 |
| 0·0·4 | `Content/Ephemeral Drawer` | **0** | 调用方 `SetEphemeralDisplay` 才开 |
| 0·0·5 | `Content/Collected Badge` | **0** | 调用方 `ToggleClaimedWarning` 才开 |
| 0·1 | `Premium Highlight` | **0** | 里面 `Highlight`/`Blackout` 的 `Image.m_Enabled = 0`、只有 `Badge` 是 1（与 `ItemDrawer.cs:891-899` 已收口的结论**逐条一致**） |

### 3·2 几何（根 712.8×1080 · 设计 px · 左上原点）

| 节点 | 锚 `min`→`max` | `sizeDelta` | 换算后矩形 | 备注 |
|---|---|---|---|---|
| `Content` | (0.5,0.5)→(0.5,0.5) | (712, 1080) | x 0.40…712.40 · y 0…1080 | `pos (0,0)` |
| `Content/Background` | (0,0)→(1,1) | (0,0) | 铺满 `Content` | |
| `Content/Image` | **(0,0)→(1,1)** | (0,0) | **铺满 `Content`** | ⚠️ 与 `Wildcard` 不同（那边是下沿横带） |
| `Content/Label` | (0.05,0.15)→(0.95,0.25) | (0,0) | x 36.00…676.80 · y 810…918 | 高 108 |
| `…/Quantity` / `…/Name` | 0×0（**布局驱动**） | (0,0) | HLG 分左右半：Q x 61.0…351.4 · N x 361.4…651.8 | 见 3·5 |

### 3·3 美术 / 交互

| 节点 | `m_Sprite` | `m_Color` | `m_PreserveAspect` | `m_Type` | `m_RaycastTarget` / `Padding` |
|---|---|---|---|---|---|
| `Background` | （`background` 槽，运行期由子类写） | 白 | **1** | 0 Simple | 1 / (0,0,0,0) |
| `Image` | **`null`**（运行期写） | 白 | **1** | 0 | 1 / 0 |
| `Label` | `40k_main_bt_nametag`（`Resources/Art/ui/`，109×41） | 白 | **0** | 0 | 1 / 0 |
| `Premium Highlight/Badge` | `40k_campaign_Premium-icon` | 白 | 1 | 0 | 1 / 0 |

### 3·4 `HorizontalLayoutGroup`（`Content/Label` 上，★ `Wildcard` 共用）

`m_ChildAlignment 0` · `m_Spacing 10` · `m_Padding L=25 R=25 T=0 B=0` ·
`m_ChildControlWidth 1` · `m_ChildControlHeight 1` · `m_ChildForceExpandWidth 1` · `m_ChildForceExpandHeight 1` · `m_ChildScaleWidth 0`。
⇒ 每个子件分到 `(640.8 − 50 − 10) / 2 = 290.4` 宽。

### 3·5 文字（TMP 逐格现读）

| 节点 | `m_text` | `fontSize` | `base` | `auto[min,max]` | H 对齐 | V 对齐 | 色 |
|---|---|---|---|---|---|---|---|
| `Quantity` | `2` | 75 | 36 | [12, 75] | **4 = Right** | 512 = Middle | 白 |
| `Name` | `Legendary Wildcard` | 75 | 36 | [12, 75] | **1 = Left** | 512 = Middle | 白 |
| `AlreadyOwned`（装饰层，默认关） | `Already Owned` | 75 | 36 | [12, 75] | 2 = Center | 512 | (1,1,0.737,1) |

### 3·6 结论

**不要为它写渲染器。** 它的价值是「基类三层（`Background` / `Image` / `Label`）的标准锚点」——
`Content/Background` 与 `Content/Image` **都铺满**、`Label` 在下沿 810…918。
若要「让我们的兜底抽屉长得像基类」，那条活属于 `Placeholder`（我们自造），**不在 `B-293` 范围内**。

---

## 四、`Wildcard Drawer` 规格（★ 活的，优先做）

### 4·1 层级与画序

| 序 | 节点 | `m_IsActive` | 挂在 `ItemDrawerComponents` 的哪一格 |
|---|---|---|---|
| 0 | **`Wildcard Drawer`**（根） | 1 | 根：ARF 2·0.66 + `ItemDrawerComponents` + **`WildcardDrawer`** |
| 0·0 | `Content` | 1 | `UIScaleToFit` + `UIEffectTogglerController` |
| 0·0·0 | `Content/Background` | 1 | **`background`** |
| 0·0·1 | `Content/Image` | 1 | **`image`** |
| 0·0·2 | `Content/Label` | 1 | （名条底板，**不是** `label` 槽） |
| 0·0·2·0 | `…/Quantity` | 1 | **`quantity`** |
| 0·0·2·1 | `…/Name` | 1 | **`label`** |
| 0·0·3 | `Content/Converted Drawer` | **0** | `conversionDisplay` |
| 0·0·4 | `Content/Ephemeral Drawer` | **0** | `ephemeralDisplay` |
| 0·0·5 | `Content/Collected Badge` | **0** | `claimedWarning` |
| 0·0·6 | **`Content/Army`** | 1 | **`WildcardDrawer.armyText`**（★ 只有这份有） |
| 0·1 | `Premium Highlight` | **0** | `premiumHighlight` |

> 画序要点：`Army` 排在 `Collected Badge` **之后**（兄弟序 = `.../Label, Converted, Ephemeral, Collected, Army`）。

### 4·2 几何（根 712.8×1080 · 设计 px · 左上原点）

| 节点 | 锚 `min`→`max`（相对 `Content` 712×1080） | `sizeDelta` | 换算后矩形（根 px） | 施工写法（`fr = FrameOf(box, fit: HeightControlsWidth)`） |
|---|---|---|---|---|
| `Content` | (0.5,0.5)→(0.5,0.5) | (712, 1080) | x 0.40…712.40 · y 0…1080 | `DrawerContent(node, fr)` |
| `Background` | (0,0)→(1,1) | (0,0) | **铺满 `Content`** | `fr.Map(DrCx1, 0, DrCx2, DrRootH)` |
| `Image` | **(0,−0.05)→(1,0.13)** | (0,0) | x 0.40…712.40 · **y 939.60…1134.00**（**越过 `Content` 下沿 54 px**） | `fr.Map(DrCx1, 939.6f, DrCx2, 1134f)` |
| `Label` | (0.05,0.15)→(0.95,0.25) | (0,0) | x 36.00…676.80 · y 810.00…918.00 | `fr.Map(DrLbX1, DrLbY1, DrLbX2, DrLbY2)` |
| `Label/Quantity` | 布局驱动 | (0,0) | x 61.0…351.4 · y 810…918 | `fr.Map(DrQtyX1, DrLbY1, DrQtyX2, DrLbY2)` |
| `Label/Name` | 布局驱动 | (0,0) | x 361.4…651.8 · y 810…918 | `fr.Map(DrNameX1, DrLbY1, DrNameX2, DrLbY2)` |
| `Army` | **(0,0.25)→(1,0.35)** | (0,0) | x 0.40…712.40 · **y 702…810** | `fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2)` |

> ✅ **`Label` 那三行的矩形与 `ItemDrawerLayout.cs:135-137` 的常量逐位相同**（`DrLbX1=36` / `DrLbX2=676.8` / `DrQty 61…351.4` / `DrName 361.4…651.8`），`Army` 与 `:133` 的 `DrArmyY1=702 / DrArmyY2=810` 也逐位相同 ⇒ **几何可以直接复用现成常量，不需要新数字**。
> ✅ `Image` 的 `939.6…1134` 与 `Shell/ItemDrawerContainer.cs:51` 里 `Army Icon` 用的那条算式**完全一样** —— 建议抽成共享常量（见 §七-3）。

### 4·3 美术 / 交互

| 节点 | `m_Sprite`（序列化值） | 运行期真值 | `m_Color` | PA | `m_Type` | Raycast / Padding |
|---|---|---|---|---|---|---|
| `Background` | `40k_general_wildcard_epic` | **`wildcardBackgrounds[rarity−1]`** = `40k_general_wildcard_{common,rare,epic,legendary}` | 白 | **1** | 0 | 1 / 0 |
| `Image` | `40k_DeckSelection_icon_FactionUM`（256²） | **`GetArmyIcon(cardArmy)`**（= 我们的 `ArmyIcons.Texture(army)` / `DeckRuntime.FactionIcon`） | 白 | **1** | 0 | 1 / 0 |
| `Label` | `40k_main_bt_nametag` | 不变（只有 TMP 会变） | 白 | **0** | 0 | 1 / 0 |
| `Collected Badge/Image` | null | — | 白 | 0 | 0 | 1 / 0（`Image.m_Enabled = 0`） |
| `Premium Highlight/Highlight` | `Highlight_Rounded_Square` | — | 白 | 0 | **1 Sliced** | `m_Enabled = 0` |
| `Premium Highlight/Blackout` | null | — | **(0,0,0,0.5529)** | 0 | **1 Sliced** | `m_Enabled = 0` |
| `Premium Highlight/Badge` | `40k_campaign_Premium-icon` | — | 白 | 1 | 0 | `m_Enabled = 1` |

**图源核对（`Resources/Art/` 现读，全部命中）**

| 图名 | 落点 | 尺寸 |
|---|---|---|
| `40k_general_wildcard_{common,rare,epic,legendary}` | `ui_deck/` | **328×497** |
| `40k_main_bt_nametag` | `ui/` | 109×41 |
| `40k_DeckSelection_icon_FactionUM` | `ui_deck/` | 256×256 |
| `Highlight_Rounded_Square`（落盘名下划线） | `ui_menu/` | 150×155 |
| `40k_campaign_Premium-icon` | `ui_menu/` | 301×305 |

### 4·4 文字（TMP 逐格现读）

| 节点 | `m_text`（出厂） | `fontSize` | `base` | `auto[min,max]` | H | V | 色 |
|---|---|---|---|---|---|---|---|
| `Quantity` | `2` | 75 | 36 | [12,75] | **4 Right** | 512 Middle | 白 |
| `Name` | `Legendary Wildcard` | 75 | 36 | [12,75] | **1 Left** | 512 Middle | 白 |
| `Army` | `Leviathan` | 75 | 36 | [12,75] | **2 Center** | **1024 Bottom** | **(0.6887, 0.6887, 0.6887, 0.851)** |

> ⚠️ **`Army` 的竖直档是 `Bottom`**（`Avatar Drawer` 是 `Middle`）—— 见 §六-3 的复用注意。

### 4·5 运行期到底写哪几层（`decomp_full/WildcardDrawer__Draw.c` 现读）

```
① background.sprite = wildcardBackgrounds[item.cardRarity − 1]        // 域外原版会 List.get_Item 抛
② image.sprite      = ArmyUtilities.GetArmyIcon(item.cardArmy)
③ armyText.text     = GameStaticData.CardArmyToString(item.cardArmy)  // = I2("Armies/<枚举名>")
```
- **`Name` 的文本不在这里** —— 它由基类 `ItemDrawer<T>.Draw` 写（`decomp_generic_trial/out/ItemDrawer_object___Draw__1ae9cc0.c:80-105` 现读）：
  `if (components.label != null) { label.gameObject.SetActive(true); 若 GetItemName(item) 非空 ⇒ label.text = GetItemName(item); 否则 SetActive(false); }`
- **`Quantity` 同理**：`if (components.quantity != null) quantity.text = qty.ToString()`。
- ⚠️ **`options.showName` 在这段泛型体里没有消费点**（现读）—— 所以「`Wildcard` 的 `showName = false`」**不代表不画名字**；判据是「`GetItemName` 非空」。
- `cardIcon`（`40k_general_wildcard_legendary_small`）在 `Draw` 里**没有被用到**（现读，整段只出现三次 `set_sprite`/`set_text`）。
- `GetItemName` = `string.Format(I2("MenuShop/RarityWildcard"), I2("Card_Rarity/" + rarity名))` —— 我们已实现（`ItemDrawer.cs:1257-1276` `WildcardName` / `RarityName`）。

---

## 五、`Deck Drawer` 规格（配置表 `PrebuiltDeck` 那一份）

### 5·1 层级与画序

| 序 | 节点 | `m_IsActive` | 组件 / 备注 |
|---|---|---|---|
| 0 | **`Deck Drawer`**（根） | 1 | ARF 2·0.66 + `ItemDrawerComponents`（`background`=**null**、`quantity`=**null**、`image`/`label` 有值）+ **`DeckDrawer`** |
| 0·0 | `Content` | 1 | **锚 (0,0)-(1,1) 拉伸、`sizeDelta (0,0)`** ⇒ **= 整根**（与 `Wildcard` 的 712 宽 `Content` 不同） |
| 0·0·0 | `Collection Deck` | 1 | `DragAndDropItem` + `UIImageGreyscaleController` + **`UIScaleToFit`**；设计尺寸 328×496.97 |
| 0·0·0·0 | `content` | 1 | 拉伸 (0,0)-(1,1) |
| 0·0·0·0·0 | `Deck` | 1 | 拉伸 (0,0)-(1,1)；**四个可视图层 + 三个默认关的都在这一层下面** |
| | `Deck/Highlight` | **0** | `Highlight_Rounded_Square`，`m_Color (1,0.7734,0,1)`、`m_Type 1 Sliced` |
| | `Deck/Deck Image(mask)` | 1 | **`RectMask2D`**（`m_Padding (0,0,0,0)`） |
| | `…/Deck Image` | 1 | 主图（`image` 槽），**过扫**、`PA = 0` |
| | `Deck/Frame` | 1 | `40K_bt_deck` |
| | `Deck/Deck Name` | 1 | TMP（`label` 槽） |
| | `Deck/Faction Icon` | 1 | `armyIcon` 槽 |
| | `Deck/Game Mode Icon` | 1 | `gameModeIcon` 槽 |
| | `Deck/DificultyLevel` | **0** | `Menu_Icon_Gallons_1` |
| 0·0·1 | `Ban Icon` | **0** | `40k_Cross_icon_cross_big_Banned_card` |
| | `…/Banned Text` | 1 | `EverguildTextMeshPro` |
| 0·0·2 | `Converted Drawer` | **0** | |
| 0·0·3 | `Ephemeral Drawer` | **0** | |
| 0·0·4 | `Collected Badge` | **0** | |
| 0·1 | `Premium Highlight` | **0** | 与 `Wildcard` 同构（`Highlight`/`Blackout` `m_Enabled 0`、`Badge` 1） |

### 5·2 几何（根 712.8×1080 · 设计 px · 左上原点 · 已把 `Collection Deck` 的 ×2.173166 算进去）

| 节点 | 锚 `min`→`max`（相对 `Deck` = 整根） | `sizeDelta` | 换算后矩形（根 px） |
|---|---|---|---|
| `Content` / `Collection Deck` / `content` / `Deck` | 全 (0,0)→(1,1) | (0,0) | **0…712.8 × 0…1080（整根）** |
| `Deck/Deck Image(mask)` | (0.044, 0.18)→(0.956, 0.93547) | (0,0) | **x 31.36…681.44 · y 69.69…885.60** |
| `…/Deck Image`（**相对 mask**） | (−0.265, −0.067)→(1.26, 1.065) | (0,0) | **x −140.91…850.45 · y 16.66…940.27**（过扫，靠 mask 裁） |
| `Deck/Frame` | (0, 0.049345683)→(1, 0.958) | **(−4.0127, 0)** | **x 2.01…710.79 · y 45.36…1026.71** |
| `Deck/Deck Name` | (0.08, 0.052)→(0.92, 0.15) | (0,0) | **x 57.02…655.78 · y 918.00…1023.84** |
| `Deck/Faction Icon` | (−0.042, 0.112530865)→(0.296, 0.32424694) | (0,0) | **x −29.94…210.99 · y 729.81…958.47** |
| `Deck/Game Mode Icon` | (0.682, 0.112530865)→(1.02, 0.32424694) | (0,0) | **x 486.13…727.06 · y 729.81…958.47** |
| `Deck/DificultyLevel`（**默认关**） | (0.6367673, 0.75370723)→(0.9764867, 0.961146) | (0,0) | x 453.89…696.04 · y 41.96…266.00 |
| `Ban Icon`（**默认关**） | (0.5,0.5)→(0.5,0.5) | (300, 377.61) · `pos (0, 38.806)` | x 206.40…506.40 · y 312.39…690.00 |
| `…/Banned Text` | (0.15,0.4)→(0.85,0.6) | (0,0) | x 251.40…461.40 · y 463.43…538.96 |

> ⚠️ **`Faction Icon` 与 `Game Mode Icon` 都溢出 `Deck` 左右边**（−29.94 与 727.06，根只有 0…712.8）—— **原版就是溢出**，⛔ 别夹回框内。

### 5·3 美术 / 交互

| 节点 | `m_Sprite` | `m_PreserveAspect` | 备注 |
|---|---|---|---|
| `Deck Image(mask)` | — | — | **`RectMask2D`**，`m_Padding (0,0,0,0)` |
| `Deck Image` | null（运行期 `cardImageReference.Load()`） | **0（拉伸！）** | 见 §六-2 的裁切讨论 |
| `Frame` | `40K_bt_deck`（`ui_deck/`，244×366） | **0** | 拉伸 |
| `Faction Icon` | null（运行期 `GetArmyIcon`） | **1** | |
| `Game Mode Icon` | null（运行期 `GetGameModeIcon`） | **1** | |
| `DificultyLevel` | `Menu_Icon_Gallons_1` | 1 | 默认关 |
| `Ban Icon` | `40k_Cross_icon_cross_big_Banned_card` | 1 | 默认关 |

### 5·4 文字

| 节点 | `m_text` | `fontSize` | `base` | `auto[min,max]` | H | V | 色 |
|---|---|---|---|---|---|---|---|
| `Deck Name` | `Deck name` | **32** | **30** | **[8, 32]** | **2 Center** | **8192 Capline** | 白 |

### 5·5 运行期（`decomp_full/DeckDrawer__Draw.c` 现读，`+0x58 = armyIcon` / `+0x60 = gameModeIcon`）

```
① armyIcon.sprite      = GetArmyIcon(DeckHero.cardArmy)
② image.sprite         = DeckHero.cardImageReference.Load()
③ gameModeIcon.sprite  = GetGameModeIcon(item.gameMode)     // gameMode 在 item +0x78，可为 null
④ gameModeIcon.enabled = (gameModeIcon.sprite != null)
```
- 每一步都有 `!= null` 守卫；**任一处为 null ⇒ 走 `FUN_1803f47a0()` = 抛 NRE**（该 helper 的定性见 `资料/AnimFX_18类方法体_块1.md:27`）。本 prefab 的 `gameModeIcon` **非 null** ⇒ 正常走通。
- `deckIcon`（`Sprite` 字段 `+0x68`）**在 `Draw` 里没有被用到**（现读全篇无引用）⇒ **我们不需要画它**。
- `GetItemName` = 词条 `"PrebuiltDecks/" + item.deckId`。

---

## 六、我们现在画成什么（逐项 `文件:行号` + 常数）

### 6·1 `Wildcard Drawer` —— 整版是我们自己的

`Shell/ItemDrawer.cs:1177-1251` `Wildcard(...)`，实际调用链：

| # | 原版那层 | 我们画的那次调用 | 位置 | 结论 |
|---|---|---|---|---|
| 1 | `Content/Background`（铺满 · PA=1 · `40k_general_wildcard_*`） | `WildcardIcon(...)` → `MenuDraw.Rect(node, tex, **IconRect(box, st)**, "Icon", st.QArt, null, **true**, …)` | `:1281-1292`（`IconRect` 定义 = `ItemDrawerPartial_B111R3W.cs:31-34`） | **不一样**：图对，但矩形是 `Square(box, IconFill=0.7)`（`min(W,H)×0.7` **居中正方形**），不是铺满；节点名叫 `Icon` 不是 `Background`；队列用 `QArt` 不是 `QBoard` |
| 2 | `Content/Image`（下沿横带 939.6…1134 · PA=1） | `MenuDraw.Rect(node, fatex, **Square(box, st.ArmyFill = 0.34)**, "Army Icon", st.QArt, …)` | `:1188-1189` | **不一样**：居中正方形（`min×0.34`），不是下沿横带 |
| 3 | `Content/Army`（702…810 · 灰 `(0.689,…,0.851)` · V=Bottom · fs75） | `ClippedText(node, **NameStrip(box, st.NamePx)**, Loc.T("Armies/"+fac), **Color.white**, …, align 0)` | `:1228-1229` + `NameStrip` `:1340-1343` | **不一样**：位置自造（顶边条）、**颜色写成白**（原版灰） |
| 4 | `Content/Label/Name`（右半 · Left · fs75） | `ClippedText(node, **NameStripAt(box, st.NamePx, 1)**, …)` | `:1246-1249` + `:1351-1357` | **不一样**：自造第二条（且注释自己标了 🔶 自己设计） |
| 5 | `Content/Label/Quantity`（左半 · Right · fs75） | `Quantity(node, **box**, qty, st)` → 底边右对齐 `side 10 / top 60 / bottom 15` | `:1363-1387` | **不一样**：钉在框底，不是名条左半 |
| 6 | `Content/Label`（名条底板 `40k_main_bt_nametag` + HLG） | **完全没建** | — | **我们根本没有这层** |
| 7 | `Content` 容器 | **没建** | — | **没有这层** |
| 8 | 帧模型（ARF 2 · K = `box.H/1080`） | `float k = box.H / DecorRefH`（`DecorRefH=1080`，`:310`）**只用在字号**；几何全走 `Square(box, …)` | `:1200` | 字号那一半的刻度**对**，几何那一半**没有帧** |

⚠️ **不要把 `Wildcard()` 和 `WildcardIcon()` 混为一谈**：`WildcardIcon()` 是**另一个 prefab**（`Icon Wildcard Drawer Variant`：根 ARF 2 · **比例 1**、`Content` 拉伸、**单个 `Image` 铺满 + `m_LocalScale 0.94` + PA=1**），已有断言 `Editor/B114_C_Assert.cs:363-378`（F1–F4）在护它 ⇒ **重写 `Wildcard()` 时 ⛔ 不许动 `WildcardIcon()`**（今天 `Wildcard()` 把它当底图那一层在用，那是**误用**，要拆开）。

### 6·2 `Deck Drawer` —— 其实已经照 prefab 了，只差三处

`Shell/ItemDrawer.cs:1768-1821` `Deck(...)`：

| # | 原版 | 我们 | 位置 | 结论 |
|---|---|---|---|---|
| 1 | 帧（ARF 2 ⇒ K = `box.H/1080`，根宽 = 0.66×高） | `DeckAnchor(box, …)` 直接乘 **`box.W` / `box.H`** | `:1758-1762`，调用点 `:1783/1799/1805/1812/1817` | **不一样**：框比例 ≠ 0.66 时横向被拉伸（例：锻造格 297.655×384.66 ⇒ 原版根只有 **253.9** 宽、左右各留 21.9 px，我们现在拉满 297.655） |
| 2 | `Content` 容器（整根） | **没建** | — | **没有这层**（几何影响 ≈0，见 §二·3） |
| 3 | `Deck Image(mask)`：`RectMask2D` + 过扫主图（PA=0） | 直接把主图画在 **mask 矩形**里、`keepAspect: false` | `:1783-1786` | **不一样**（注释 `:1765-1767` 已如实记）：原版是「拉伸到过扫框 456.18×424.99 → 裁到 mask 299.14×375.44」，我们是在 mask 框里**整张压扁**（比例不同） |
| 4 | `Frame` 45.36…1026.71 · PA=0 | `DeckAnchor(box, 0f, 0.04935f, 1f, 0.958f)` + `keepAspect false` | `:1797-1800` | **一样**（锚点逐字），只是乘错了基准（见 #1） |
| 5 | `Deck Name` fs **32** / base **30** / auto **[8,32]** / **Center · Capline** | `fs = 32f * (box.H / 496.97f)`（`DeckDrawerRefH`），`ClippedText(..., align 0)` | `:1816-1818` | **字号对**（见 §二·3：`box.H == root.H` ⇒ 与 `32×(root.H/496.97)` 等价）；**base/auto 没传**（默认 0 ⇒ 不开自适应）、**竖直档没设**（原版 Capline） |
| 6 | `Faction Icon` / `Game Mode Icon`（PA=1） | `DeckAnchor(...)` + `keepAspect true` | `:1802-1813` | **一样**（锚点逐字） |
| 7 | `quantity` 槽 = **null** ⇒ 原版不画数量 | `if (st.QuantityPx > 0f && Stackable(item)) Quantity(...)` | `:1820` | **不一样**：`PrebuiltDeck` 在 `ItemDrawerOptions.cs:64` 里 `stackable = true` ⇒ **我们会画，原版不画**（原版基类那句是 `if (components.quantity != null)` 才写，这一份槽是 null） |
| 8 | `Highlight` / `Ban Icon` / `DificultyLevel` 默认关 | 不建 | — | **一样**（`ItemDrawer.cs:1728-1733` 已写明理由） |
| 9 | `Deck Image` 的 `DragAndDropItem`（可拖拽） | 无 | — | **我们根本没有这层交互**（不在 `B-293` 范围，另立账） |

### 6·3 `Base Drawer` —— 我们没实现，也不需要

- 我们**没有** `Base` 抽屉；`Placeholder(...)`（`:1313-1323`）是**自造的兜底**（实心板 + `item.Label` + 数量），**不是**在画 `Base Drawer`。
- `Deck()` 在 `item.Deck == null` 时会落到 `Placeholder`（`:1771-1779`）—— 这就是 `报告/B114_W47` 说「`Deck Drawer` → `Placeholder`」的由来。
- ⚠️ `Placeholder` 的 `NodePlaceholder = "IconPlaceholder"`、`NodeItemName = "ItemName"` —— 与 `Base Drawer` 的节点名（`Background`/`Image`/`Label/Name`）**完全不同**。

---

## 七、施工建议

> 🔴 **行号纪律**：`Shell/ItemDrawer.cs` 现读 **2,497 行**，`账/` 里有 **220 条**落点钉在它上面、**最深到 `:2339`**
> （本会话用 `账/*.tsv` 全扫算出：`ItemDrawer.cs max_line 2339 · hits 220`）。
> ⇒ **新成员一律追加到 `:2340` 之后**，或**开新 partial**；⛔ 别在 `:2339` 之前插行。

### 7·1 `Wildcard Drawer`（先做）

**推荐做法：照 `Shell/ItemDrawerContainer.cs:32-62` 那份的形状重写 `Wildcard()`**（它已经是「帧 + Content + Background + 徽记 + 名字/数量」五件套的现成样板）：

```csharp
static void Wildcard(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)
{
    var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);     // ① 帧（§二·1）
    var content = DrawerContent(node, fr);                        // ② Content

    // ③ Content/Background —— 稀有度底图，铺满 + PA=1
    string bg = WildcardArtName(item.Rarity);                     // 域外 = null ⇒ 出声 + 退 WildcardTexName(item)
    if (!DrawerBackground(content, fr, st, bg)) { … 出声/置 MissingArt … }

    // ④ Content/Image —— 阵营徽记，落在「下沿横带」（与 Container 的 Army Icon 同一矩形）
    var emblemR = fr.Map(DrCx1, 939.6f, DrCx2, 1134f);
    MenuDraw.Rect(content, ArmyIcons.Texture(item.CardArmy), emblemR, NodeImage, st.QArt, null, true, …);

    // ⑤ Content/Army —— 阵营名（gray / Bottom）
    var armyLb = DrawerArmyName(content, fr, item.CardArmy, st);
    if (armyLb != null) MenuDraw.SetVAlign(armyLb, Label.VAlign.Bottom, fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2));

    // ⑥ Content/Label/Quantity + Name（★ Name 的 H 对齐是 Left，现成 helper 给的是 Center）
    DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: true, nameAlign: 1);
}
```

要落的改动点：

| # | 改什么 | 位置 | 说明 |
|---|---|---|---|
| W1 | 重写 `Wildcard()` 体 | `Shell/ItemDrawer.cs:1177-1251` | **零行数变化地整段替换**（改完核 `账` 闸：这一区没有 A 行落点，最深的在 `:2339`） |
| W2 | `DrawerNameQuantity` 加 `int nameAlign = 0` 形参 | `Shell/ItemDrawerLayout.cs:291-310` | **默认 0 = 现有 10 个调用点逐字节不变**；`Wildcard` 传 `1`（Left） |
| W3 | （可选）抽共享常量 `DrEmblemY1/Y2 = 939.6f / 1134f` | `ItemDrawerLayout.cs:121-146` | 现在 `ItemDrawerContainer.cs:51` 与 Wildcard 会各写一份；⛔ 两处写同一条规则 |
| W4 | `WildcardArtName` 域外兜底 | `ItemDrawer.cs:1482-1486` | `null` ⇒ 出声 + 用 `WildcardTexName(item)`（与 `WildcardIconDrawer` 的 `FirstOrDefault` 兜底**不是同一张表**，别混） |
| W5 | `st.IconFill` / `st.ArmyFill` 在 `Wildcard` 里**不再参与几何** | — | 与 `CampaignPoint` 那次同款（见 `ItemDrawer.cs:2198` 的先例）；调用点仍传值、不影响 |
| W6 | `Shell/ForgeTab.cs:1203` 的 `st.IconFill = 0.95f`（🔶 自己设计） | 该行 | 迁完之后这一格**不再需要**这个值 ⇒ 删掉它 + 删掉那条 🔶 标记（它写的理由是「原版判据拿不到」，**现在判据有了**） |

### 7·2 `Deck Drawer`（次做）

| # | 改什么 | 位置 | 说明 |
|---|---|---|---|
| D1 | `Deck()` 开头改成帧模型 | `Shell/ItemDrawer.cs:1768-1770` | `var root = RootRectOf(box, fit: RootFit.HeightControlsWidth);` 然后**把 `DeckAnchor(box,…)` 的 5 个调用点全换成 `DeckAnchor(root,…)`**（`DeckAnchor` 本身不用改，它只吃矩形） |
| D2 | 建 `Content` | 同上 | `MenuDraw.Node(node, NodeContent, root)`；⛔ 别用 `DrawerContent`（那是 712 宽，`Deck` 的 `Content` 是**整根 712.8**） |
| D3 | 去掉数量 | `:1820` | 原版 `quantity` 槽 = null ⇒ 这一句**删掉**（判据 = §1·2 的字段表） |
| D4 | `Deck Name` 补自适应 + 竖直档 | `:1816-1818` | `ClippedText(..., align 0, autoMinPx: 8f*k, autoMaxPx: 32f*k, autoBasePx: 30f*k)` + `MenuDraw.SetVAlign(lb, Label.VAlign.Capline, r)`；`k = root.H/496.97` |
| D5 | 主图裁切（**可选，风险最高**） | `:1783-1786` | 见下 |

**D5 的两个方案（都写清代价，由写手按当批风险预算选）**：

| 方案 | 做法 | 代价 |
|---|---|---|
| **保守（推荐先做）** | 维持现状：在 mask 矩形里 `keepAspect: false` | 比例不对（原版 = 拉伸到 1.073 的过扫框再裁成 0.797；我们是整张压成 0.797）—— **可见差异**，但 `Deck` 今天 **0 个实例**，零回归风险 |
| **精确** | 画在过扫矩形 `x −140.91…850.45 · y 16.66…940.27`，并给 `MenuDraw.Rect(..., clip: maskRect)` 传**显式裁切** | ⚠️ `MenuDraw.Rect` 的 `clip` 形参是「显式覆盖」（`MenuDraw.cs:1630-1637` `ViewportClip.Resolve`：**形参非空 ⇒ 形参赢**）⇒ 传了它就会**丢掉外层滚动视口的裁切**。要用得先扩 `ViewportClip`（交集），那是另一笔账 |

### 7·3 `Base Drawer`（不做渲染器）

- ⛔ 不新建 `BaseDrawer` 渲染函数（没有调用者、原版也没有实例）。
- ✅ 只把 §三 的锚点当作「基类三层」的参考资料；**今天没有任何一行代码需要按它改**。
- 如果将来要做「兜底抽屉长得像基类」，那是 `Placeholder` 的重构，**另立账**，别混进 `B-293`。

### 7·4 复用件清单（先 `grep` 过，别再造）

| 件 | 位置 | 能不能复用 |
|---|---|---|
| `FrameOf` / `FitK` / `RootRectOf` / `RootFit` | `Shell/ItemDrawerLayout.cs:187-214` | ✅ **必须**用 |
| `DrawerContent` | `:219-222` | ✅ `Wildcard` 直接用；`Deck` 用 `MenuDraw.Node`（整根） |
| `DrawerBackground`（`sprite` + `keepAspect` 都是形参） | `:247-261` | ✅ `Wildcard` 传 `WildcardArtName(rarity)`（默认 `keepAspect: true` 本来就是对的） |
| `DrawerArmyName` | `:274-284` | ✅ 矩形与字号**逐位对**；⚠️ **竖直档要另设 Bottom** |
| `DrawerNameQuantity` | `:291-310` | ✅ 几何逐位对；⚠️ **Name 的水平对齐要 Left**（需加形参，见 W2） |
| `DrawerImage` | `:313-319` | ✅ 可用于 `Content/Image`（PA 恒 true，与原版一致） |
| `DrawerFrame.Scale` | `:161-165` | `Deck` 目前不需要（没有 ×1.2 那档） |
| `Square` / `NameStrip` / `NameStripAt` / `Quantity` | `ItemDrawer.cs:1332-1387` | ❌ **`Wildcard` 改完之后这四个在它身上就不该再用了**（`Square`/`Quantity` 仍被 `Icon`/`Placeholder` 用着，⛔ 别删） |
| `CampaignPointsImageRect` | `ItemDrawer.cs:2188` 附近 | ❌ 只服务 `CampaignPoints`（锚点不同），**不能复用** |

---

## 八、与现有调用点 / 断言的耦合（★ 会红的清单，必须同批处理）

| 位置 | 它断的是什么 | 重写 `Wildcard` 之后 |
|---|---|---|
| `Editor/RewardsScene.cs:5029-5045`（`T21` 桶 H1） | 取 `FindChild(drawer, ItemDrawer.NodeIcon)` 量 `WorldW` 与 `UvRect`，还写死了「`Square(box, 0.95)` = 282.7725」这套口径 | 🔴 **必红**：`Icon` 节点不再存在（改成 `Background`），而且矩形由方形变铺满 ⇒ **这 3~5 条断言要按新几何重写**（它们现在测的正是「我们自己的错版式」，是自证） |
| `Editor/B114_C_Assert.cs:363-378`（F1–F4） | **Icon 变体**（`WildcardIconDrawer`）：0.94 内接、不建 `Label`/`Background` | ✅ **不该红** —— 前提是 `WildcardIcon()` 一个字节没动（见 §6·1 的警告） |
| `Editor/B110_F2_Assert.cs:71` | `HasQuantitySlot(DrawerWildcard)` = true | ✅ 不受影响（槽位表是数据，不是几何） |
| `Shell/CampaignRewardWindow.cs:936` | `st.IconFill = ItemIconPx / min(ItemW, ItemH)` = 0.7 | ✅ 不红（值仍传，只是不再参与 `Wildcard` 几何）；注释里的「= 0.7 的出处」要顺手改一句 |
| `Shell/RewardWindow.cs:1435` / `Shell/ForgeTab.cs:1203` | 同上 | 同上 |
| `Editor/ShopScene.cs:4331` / `:4200-4215` | 断言**路由**（`res.Drawer == DrawerWildcard`） | ✅ 不红 |
| `Editor/B114_W27_Assert.cs:267/350/390` | `CampaignPointsImageRect` 的锚点 | ✅ 不红（另一族） |

⚠️ **`Wildcard` 是三条生产路径共用的**：`CampaignRewardWindow`（200×300 格）、`RewardWindow`（200×300）、`ForgeTab`（**297.655×384.66**，比例 **0.774**）。
→ 在 0.774 的格子里，原版根只有 `0.66×384.66 = 253.88` 宽（左右各留 21.9 px），而我们现在画到 282.77 宽。
**这是原版行为**，但**画面上会明显变窄** ⇒ **改完必须进游戏看一眼**，别只靠断言。

---

## 九、顺手发现（⛔ 本代理没改任何文件，交调度台分流）

1. 🔴 **`活状态表.md:381`（`B-293` 那一行）的三个括号注串位了**。
   账上写：`Base Drawer`（占位板）· `Deck Drawer`（我们挑的方框）· `Wildcard Drawer`（模板基类 0 处实例化）。
   `报告/B114_W47_ARF迁移.md:183-188` 原文是：`Deck Drawer` → `Placeholder` · `Wildcard Drawer` → `Square`/`IconRect` · `Base Drawer` → **0 处实例化**。
   **按本会话现核，报告是对的**（`Base Drawer` 0 实例化见 §1·3；`Wildcard` 的 `Square` 见 `ItemDrawer.cs:1188/1290`）。⇒ **建议就地订正 `活状态表` 那一行**（铁律 5）。

2. 🔴 **`Shell/ItemDrawerLayout.cs:64-65` 那句话会被本次改动推翻**：
   现文「`Base Drawer` / `Deck Drawer` / `Wildcard Drawer` 这三份**本来就没有帧模型**（占位板 / 我们自己挑的方框），不在本次迁移面内」——
   ✅ 「本次（W47）没迁」仍成立，但 **`Deck` 的帧模型差 1 行、`Wildcard` 差整版** ⇒ 应在那一句后面补「⇒ 已由 `B-293` / `判据/B115_S4_三份Drawer规格.md` 接手」。

3. 🆕 **`ItemDrawer.cs:1754` 的 `DeckDrawerRefH = 496.97f` 与「根」是两把尺**：
   它其实是 `Collection Deck` 的设计高（328×496.97），换算到根要 **×2.173166**。今天因为 `box.H == root.H` 恰好等价，**但注释里没写这层，容易被后人当「根高」用错**。建议在那一行补一句。

4. 🆕 **`ItemDrawerOptions.cs` 文件头那两处「prefab 上没有 label 槽」的论据不成立**：
   文件头 `:17-20` 写「`Currency Drawer` `label` null / `Icon Currency …` `label` null …」用来推「`showName` 今天没有消费者」——
   但 **`Deck Drawer` 的 `label` = `Deck Name`、`Wildcard Drawer` 的 `label` = `Content/Label/Name`，都不是 null**。
   结论（`showName` 没有消费者）**依然对**，但依据要换成：**泛型基类 `Draw` 的体里根本没有读 `showName`**（判据 = `decomp_generic_trial/out/ItemDrawer_object___Draw__1ae9cc0.c:80-105`，`label` 的显隐只看 `GetItemName` 是否为空）。⛔ 别删结论，改依据。

5. 🆕 **`报告/B100_D1.md:30/43` 与 `判据/B100_D1_抽屉18档_施工单.md:213/214` 已在 `B109_E11` 订正过（配置表那份是 `DeckDrawer` + `gameModeIcon` 非 null）—— 本会话复核 ✅ 成立**；
   本文件只是把它再确认了一遍（三个同名实例的完整表在 §1·2），**不需要再改**。

6. 🆕 **`Placeholder` 与 `Base Drawer` 没有关系**（§6·3），但 `活状态表` 的括号注让人以为是同一件事 ⇒ 建议在 `ItemDrawer.cs:1310-1312` 的注释里补一句「原版 `Base Drawer` 是模板、0 实例化，不是这一档」。

---

## 三份的优先级排序

| 序 | prefab | 为什么排这个位置 | 建议工时量级 |
|---|---|---|---|
| **1** | **`Wildcard Drawer`** | ① **唯一「活的」**：`CampaignRewardWindow` / `RewardWindow` / `ForgeTab` 三处**真在显示**（野生牌在 `CampaignData.cs:428-471` 里有 **13+ 条**发放物）；② **差得最多**（整版是我们自己的，连名条底板都没有）；③ 现成复用件齐（`Container` 那份就是模板）⇒ **改动可预期** | 中（1 个函数体 + 1 个共享 helper 加参 + 1 处断言重写） |
| **2** | **`Deck Drawer`** | ① 几何**已经基本对**（6 层锚点逐字），只差「帧模型 / 数量槽 / 自适应 / 竖直档」四小处 ⇒ **回归面小**；② 但**今天 0 个实例**（`PrebuiltDeck` 发放物在 `数据/游戏数据/*.json` 里是 0 条），所以「验收只能靠断言、看不到画面」 | 小（1 个帧模型 + 3 处小改；裁切那条可另立） |
| **3** | **`Base Drawer`** | **没有渲染交付物**（0 实例化 + 无类 + 无运行时路径）⇒ 任何「按 prefab 画它」的代码都会变成**死代码**。它只值得当「基类三层锚点」的参考资料 | ≈0（只做文档订正） |

> 📌 **一条硬提醒**：第 1 项做完**必须进游戏看一眼**（战役奖励窗里找一个带 `Wildcard<阵营><1..4>` 的奖励）——
> 画面会从「居中方形 + 顶部名条」变成「底图铺满 + 下沿徽记横带 + 名条在中下 + 阵营名在其上」，
> **这是版式级变化，断言全绿也说明不了观感对不对**（`CLAUDE.md` §三 的「断言管行为，并排比管像不像」）。

---

## Wildcard 重写（✅ 已完成 · 2026-11-01 · `B115_S4`）

> 施工细节（对照表 / numstat / 三闸读数 / 断言补丁 / 进游戏看什么）→ **`报告/B115_S4_Wildcard重写.md`**。
> 本节只留「改完之后，本文件前面哪些话变了」。

**改了什么**：`Shell/ItemDrawer.cs` 的 `Wildcard()` **整段同物理行替换**（行数 2497→2497，`git diff --numstat` = **76/76**），
新增成员全在 **`Shell/ItemDrawerPartial_B115S4.cs`**（`WildcardEmblemRect` / `WildcardLabelRow` / 两个常量 + `.meta`）。
`WildcardIcon()` **一个字节没动**。

**改完之后，§四 / §六·1 里这些已过期**（铁律 5，就地记在这里）：

| 原来说 | 现在 |
|---|---|
| §六·1 那八行「我们的实现」全表（居中方形 / 自造名条 / 没建 `Content` 与 `Label`） | **全部作废** —— 现在四层按 prefab 锚点画，`Content` 与 `Content/Label` 都建了 |
| §四·2 的「施工写法」那一列 | 已**逐条落地**（`FrameOf` + `DrCx1/DrCx2` + `DrLb*`/`DrQty*`/`DrName*`，新常量只有 939.6 / 1134） |
| §七·1 的 W1–W6 | W1/W4/W5 已做；**W2（`DrawerNameQuantity` 加 `nameAlign`）没做** —— 共用件不在本批白名单 ⇒ 改成 `WildcardLabelRow` 自建那一行，几何仍照共用常量；W3（抽 `939.6/1134` 共用常量）**✅ 2026-11-01 已收口（`B-315`）** —— `ItemDrawerLayout.DrEmblemY1/Y2` 是唯一来源，`WildcardEmblemRect` 与 `ItemDrawerContainer.cs` 同引它；**W6（删 `ForgeTab.cs:1203` 的 `IconFill = 0.95`）不在本批白名单，未做** |
| §八 那行「`Editor/RewardsScene.cs:5029-5045` 必红」 | 仍然成立（该文件别的写手在改）⇒ **补丁文本已备好**在 `报告/B115_S4_Wildcard重写.md` §五，两处 `NodeIcon → NodeBackground` + 一段注释 |
| §四·3 的 `Content/Label` 底板 | 新增常量 `WildcardNametagSprite = "40k_main_bt_nametag"`（`Resources/Art/ui/` 现读命中） |

**⚠️ 一处与本文档其它地方口径相关的现读事实**：`Shell/ItemDrawer.cs` **是 LF 行尾**（不是 CRLF）。
同族 `ItemDrawerLayout.cs` 也是 LF。⇒ 后续写手按 LF 处理，⛔ 别「顺手统一行尾」（会把整份文件变成 2497/2497 的 diff）。

---

## Base Drawer 结论（**正式** · 2026-11-01 · `B115_S4` 复核）

> **一句话**：`Base Drawer` 是 **prefab 模板**，**运行时没有任何路径会画它** ⇒
> **⛔ 不要为它写渲染器；任何「按 prefab 画 Base Drawer」的代码都会是死代码。**
> **以后谁想实现 `Base Drawer` 渲染器，先看这一节**（下面三条复核跑一遍再动手）。

### 一、三条复核（都可复跑，缺一条都不算「查过」）

| # | 判据 | 怎么复核 | 2026-11-01 现读 |
|---|---|---|---|
| ① | **全库只有它自己一份 `GameObject`** | `find "D:/2/新解包资源/assets_full" -name "Base Drawer*.json"` | **1 份**：`bundle_menus_assets_all/GameObject/Base Drawer.json` |
| ② | **没有任何 prefab / 场景把它当实例引用** | 先取 `Base Drawer.prefab.meta` 的 `guid` = **`00198b7704fdefb48a5d6b695d93501f`**，再在 `D:/2/tools/assetripper_out/ExportedProject/Assets` 全量 `--include=*.prefab --include=*.unity` 搜这个 guid | **0 命中**（只有它自己那份 prefab 的 `.meta` 持有这个 guid） |
| ③ | **反编译里没有 `BaseDrawer` 这个类** | `grep -n "class BaseDrawer" D:/2/tools/il2cpp_out/dump.cs` | **0 命中** —— 那一族只有 `abstract ItemDrawer` / `abstract ItemDrawer<T>` / `DeckDrawer` / `WildcardDrawer` … |

补充：`Base Drawer` 的根上挂的是**基类**组件（`AspectRatioFitter` + `ItemDrawerComponents`），
**没有自己的抽屉类**（对比：`Deck Drawer` 根上有 `DeckDrawer`、`Wildcard Drawer` 根上有 `WildcardDrawer`）。
⇒ 它正是「各族的副本/prefab variant 从这儿派生」的那一份模板。

### 二、由此得到的口径（⛔ 别推翻）

1. **`B-293` 的「Base Drawer」那一项 = 不产出代码**。三份里它的「施工量 ≈ 0（只做文档订正）」。
2. **我们的 `Placeholder` 不是它在画**（`Shell/ItemDrawer.cs` 的 `Placeholder` / `NodePlaceholder = "IconPlaceholder"` /
   `NodeItemName = "ItemName"`）—— 那是我们自造的「判据空」兜底，节点名与 `Base Drawer` 的
   `Background` / `Image` / `Label/Name` **完全不同**。⛔ 别把两者合并叙述。
3. 真要做「让兜底抽屉长得像基类」这件事，那是 **`Placeholder` 的重构**，**另立账**，不属于 `B-293`。
4. 如果将来原版新 build 里 `Base Drawer` 开始被实例化（②的 guid 搜得出命中），本节作废、按 §三 的规格做。

### 三、规格本身就留在这里（真要画时照抄，⛔ 别重新查）

`Base Drawer` 的层级 / 几何 / 美术 / 文字四张表在本文档 **§三（Base Drawer 规格）**：
根 `ARF 2 · 0.66` · `Content` 712×1080（x 0.40…712.40）· `Content/Background` 与 `Content/Image` **都铺满**
（⚠️ 与 `Wildcard` 不同 —— 那边 `Image` 是下沿横带）· `Content/Label` (0.05,0.15)-(0.95,0.25) ·
`HorizontalLayoutGroup` padding L/R 25 · spacing 10 · 两条 TMP 的 `fs 75 / base 36 / auto[12,75]`。
