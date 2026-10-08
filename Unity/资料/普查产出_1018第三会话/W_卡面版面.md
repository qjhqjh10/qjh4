# W_卡面版面 · `A848` + `A994③`

> 执行写手代理（卡面版面）· 2026-10-18 第三会话 · 白名单 = `Core/CardView.cs` · `Core/DraggableController.cs` · `Editor/CardBaseDemo.cs` · `Editor/DeckScene.cs`（**只动了这四个**）
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改两张正本 · ✅ 秒级类型检查 **3 次**，末次 **运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_cv`）
> ✅ 行尾逐个核过、**四个文件都保持原样**（`DraggableController` **LF** · 其余三份 **CRLF**）；`git diff --numstat` = 56/6 · 63/4 · 59/7 · 49/12（**不是整篇重写**）
> 我写的只读脚本：`d:/tmp/cv1018/dump_tmp_align.py`（按 `m_Component` 反查 GO 名）· 既有 `d:/tmp/wa712/scan2.py` · `d:/4/Unity/工具/menu_dump.py`

---

## 一、🔴 先报一条**推翻简报前提**的现核（铁律 13·4⑧）

**`A848`「卡面 78 处」那一笔，在简报说它「仍开着」的时候就已经落地了。**
现读（工作树**无 diff**）：`Core/CardView.cs` 的 `_title.verticalAlignment = VerticalAlignmentOptions.Geometry`（**HEAD `fe239e9` 里就有**，
注释判据在它上面那 20 行）· `Editor/CardBaseDemo.cs` 的 `AssertTitleMidline`（含 2026-10-18 那次夹具修正）—— 与 `资料/历史/A表已收口_1018.md`
的 601/602 两行、`普查产出_1018/AUD_A表盘点.md:53/57/58` **逐条对得上**。⇒ 本笔**没有重复落一遍**，改做三件简报没覆盖到的：
**① 判据自己现核一遍 ② 补一层从没被钉过的卡面文字 ③ 修一个由 A848 引入的次序缺陷**（§三）。

### 1·1 那「78 处」的清单/口径（**我自己重跑既有普查脚本核过，不是抄报告**）

`PYTHONUTF8=1 python -I d:/tmp/wa712/scan2.py bundle_scenes_scenes_mainmenuwarpforge bundle_scenes_scenes_battlearena1`
⇒ `Midline/Geometry` 命中 136 行，其中 `NameText*` **78 行**（逐行末列都是 Asar；六个变体各 13 行：`NameTextTactic` / `NameTextTacticBig` /
`NameTextUnit` / `NameTextUnit Big` / `NameTextUnit No description` / `NameTextUnit No Description Big`）。
⇒ **78 = 6 个变体 × 13 个场景实例**（主菜单 6×5 = 30 · 战斗 6×8 = 48）；6 个变体由 `CardTextsController` 状态机**互斥**
⇒ **我们的可落点只有 1 处**（卡名 `_title`）；**简报里「78 处要逐处落」这个读法不成立**（那是原版侧的实例数）。
余下那 58 行 `Midline` 是别的件（主菜单导航 `PLAY`/`COLLECT`… · `Resource QuantityText` · 两张俄文徽标 …），不在卡面。

### 1·2 原版逐层读数（**我自己读的**，三级跳：`GameObject/*.json` → `m_Component` → TMP 的 `m_VerticalAlignment`）

判据包：`d:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/`（**卡预制体本体**，`2DCard` + 那一族 `*Text` GO 都在这）；枚举值 = `256 Top / 512 Middle / 1024 Bottom / 2048 Baseline / 4096 Midline(Geometry) / 8192 Capline`。

| 原版节点 | V 档 |
|---|---|
| `NameTextUnit` · `…Unit Big` · `…Unit No description` · `…Unit No Description Big` · `NameTextTactic` · `NameTextTacticBig`（6 个） | **4096 `Midline`** |
| `DescText*` ×5（效果文字）· `ArmyTextUnit`/`ArmyTextTactc`/`Army No Desciption` · `RaceText` ×4 | 512 `Middle` |
| `CostText` ×3 · `Melee Attack Text` · `Range Attack Text` · `HealthText` · `Armour Text` · `Badge` 那两颗（俄文 fs0.25/fs27.7） | 前 7 颗 512；**Badge 两颗 4096** |

⇒ **只有卡名那一层该是 `Midline`**；`keywords`/`army`/`race` 三层**原版就是 `Middle`** —— 已落的实现 ✅ 对。
🔴 **`CostText`/四个数值那 7 颗原版也是 `512`，但我们这侧走 `PragatiDigits`/`TextCanvas`（烘好的数字图）、没有 TMP 档位可落** ⇒ 这一格判「**无对应物**」（不是漏做）。

### 1·3 本笔补的那一层：**角标数字**（卡面第 5 层文字，原来从没被钉过）

原版 `TraitCounter`（棋盘上单位卡身上 buff/debuff 的计数）现读
`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/` 的 `GameObject/TraitCounter*.json` → `m_Component` 里那颗 TMP，
**6 份实例逐份同值**：**`m_VerticalAlignment = 4096`** · `H = 2` · `fs = 5.5` · `fontStyle = 1`(Bold) · `m_fontColor = (1, 0.9729, 0.9104)`
（那 6 份的 MB：`MonoBehaviour_159564208624016320` · `_-2512500826360931392` · `_-1953590729870173248` · `_-3450971320567358528` ·
`_5561894497802492864` · `_7597200136860769216`）。
🔴 我们这一层原来**什么都没设** ⇒ `TmpFont.NewText` 出厂 `Middle`(512)（`Core/TmpFont.cs:158` 的 `TextAlignmentOptions.Center`）
⇒ **一直差这一档**。**本笔已落**（`CardView.PlaceBadgeCounter`）。

### 1·4 落档 / 改了哪 / 断言

| 件 | 文件 | 改动 |
|---|---|---|
| 角标数字 = `Geometry`(4096) | `Core/CardView.cs` | 新增 `PlaceBadgeCounter(int i)`（`SetBadges` 里的 `PlaceAt(...)` 改成调它）；注释里带 6 份实例的出处 |
| **次序守卫**（见 §三） | `Core/CardView.cs` | `BuildTextLayers` ① 在 `Fill` **之前**把 `_title` 复位成出厂 `Middle`；设档位那一段补一句 `ForceMeshUpdate()` |

**断言**（宿主 `Editor/CardBaseDemo.cs` 的 `AssertTitleMidline`，夹具加了一枚 `Badge{armour,2}`）：

| # | 断言 | 两态 | 🧨 改坏法 |
|---|---|---|---|
| ① | `title.verticalAlignment == 4096`（原有） | 512 / 4096 | 删掉 `_title.verticalAlignment = Geometry` ⇒ 红 |
| ② | `army`/`race`/**`keywords`** 三层 `== 512`（本笔把 `keywords` 加进来） | 512 / 4096 | 四层一刀切改 `Midline` ⇒ 那三条红 |
| ②·b | `badgeCounter0.verticalAlignment == 4096`（本笔新增） | 512 / 4096 | 删掉 `PlaceBadgeCounter` 里那句 `Geometry` ⇒ 红 |
| ③ | **再刷一次（`SetData`）两层摆位逐位不变**（本笔新增） | 稳定 / 差 `c_G` | 删掉「`Fill` 前复位成 `Middle`」或把设档挪到 `Fill` 前 ⇒ 红 |

---

## 二、`A848` 的多行偏置 —— **本件不涉及**（如实说明）

- `A847` 那条（`Label.VOffsetWorldNow` 不吃块高、多行落 `Top`/`Bottom` 偏 `(n−1)/2` 行）**是 `Battle/Label.cs` 的事**；卡面这条线**不走 `Label`**
  （`CardView` 用裸 `TextMeshPro` + 自己的 `PlaceAt`/`PlaceBottomAt`）⇒ 卡名那一层**碰不到**它；卡名本身也**不折行**（`Fill(_title, …, wrap: false, …)`、`TmpFont.NewText` 出厂 `NoWrap`）⇒ 单行。
- ⚠️ **本笔引入的那个「差一个 `c_G`」不是 `A847` 那条**（那条是 `(n−1)/2` 行 ≈49px；这条是墨心↔行盒心 ≈1.25px、与行数无关）—— 别把两条并成一条。

---

## 三、🔴 本笔查出的**真缺陷**：档位设在 `PlaceAt` **之后** ⇒ 重建时摆位会漂（`A848` 引入的）

**机制（可逐条核）**：`Fill` 末句是 `PlaceAt`，`PlaceAt` 写 `localPosition = center − t.textBounds.center`；而 `textBounds` = `TMP_Text.GetTextBounds()`，
取 `characterInfo[].ascender/descender` = **行盒 AABB**，**随档位整体平移** ⇒「先摆后设档」：行盒心落到目标点、再设 `Geometry` 平移 `c_G`
⇒ **墨心落到目标点**（= 原版 `Midline` 的定义，✅ 对）；**但** `SetData` 每刷新一次都会再走 `BuildTextLayers`（`CardView.cs:1113`，战场掉血/每回合都走），
那时 `_title` **已带着 `Geometry`** ⇒ 第二次 `PlaceAt` 与首次**差整整一个 `c_G`**（卡名实测 **0.0116 卡单位 ≈ 1.25px**；`A表已收口_1018.md` 那两行读数
`boxCenterY 0 → 0.0116` / `inkCenterY −0.0116 → 0` 就是它）——**静默、只在刷新后现形**（铁律 10 第 5 条那一族）。
**修**：`Fill` 之前先把档位复位成出厂 `Middle`（首建 `_title == null` ⇒ 这句是无操作，**首建几何逐位不变**）；角标同理（每次先复位再摆）。**断言 ③ 钉住它。**

---

## 四、`A994③` —— 卡背的矩形比例：**判「要按 sprite 比例」**（原版是 `m_PreserveAspect = 1`）

### 4·1 判据（两条，都是原版侧）

1. **原版字段**（`menu_dump.py bundle_menus_assets_all "<根>" --depth N`，三处都是 `Image` + `preserveAspect`）：

| 原版节点（prefab） | Image 的框 | sprite | `m_PreserveAspect` |
|---|---|---|---|
| `Cosmetic Drag Controller > Collection Cosmetic > content > Image`（类 `CosmeticPreview`） | **220×330**（父件 ×`m_LocalScale` **0.6**） | `Cardback_UM_Campaign_Premium_Main` **707×1020** | **1** |
| `Collection Cosmetic`（类 `CollectionCosmetic`，**卡背格原型**）`> Cardback Container > Cardback` | **250×405** | 出厂空（运行期给） | **1** |
| `Sidebar/Deck Details > Cosmetic Drawer > Cosmetic` | **335.31×400** | 出厂空 | **1** |
| （同族第 4 处）`Cardback Shadow SDF`（两个 prefab 里都有） | 337.5×550.8 | 出厂空 | **1** |

2. **它怎么算**：uGUI 源码 `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/Image.cs` 的 `PreserveSpriteAspectRatio(ref rect, spriteSize)`
   （**本机现读**）：`spriteRatio > rectRatio` ⇒ `rect.height = rect.width / spriteRatio`（**宽定**），否则 `rect.width = rect.height * spriteRatio`（**高定**）；
   两支都绕 `rectTransform.pivot`（= .5/.5 ⇒ **绕中心**）居中 ⇒ **比的是「贴图自己的比例」，⛔ 不是「框的比例」**。
   `A944` 那轮把 `m_PreserveAspect=1` 读成「比例就取 220/330」= **拉伸**，是错的。

### 4·2 三张真实卡背的 sprite rect 实测（`bundle_cosmeticscardbacksimages_assets_all/Sprite/*.json` 的 `m_Rect`）

| sprite | `m_Rect` | pivot | 我们导进工程的 PNG |
|---|---|---|---|
| `Cardback_AM_Shield of Humanity_Main` | **707×1020**（rect x=158,y=2） | .5/.5 | `Cardback_AM_Shield of Humanity.png` = **707×981** |
| `Cardback_UM_Campaign_Premium_Main` · `Cardback_AM_Cold Blood_Main` | **707×1020** · **707×1020** | .5/.5 | `…Cold Blood.png` = **707×996** |
| （对照）`Cardback_AM_Shield of Humanity_SDF` | 100×130.5 | .5/.5 | `…_sdf.png` = 100×130 |

⭐ **关键**：我们的 PNG 是**按 alpha 裁过的**（707×1020 的框里那圈透明被裁掉了）⇒ **按「我们贴图自己的比例」内接 = 按「原版 sprite 的比例」内接**
（同一像素尺度，原版那圈透明本来也看不见）；**不裁的那批**也一样对（233 张的宽高比实测区间 **0.6188 ~ 0.7652**，其中 **211 张宽定 / 22 张高定**）。

### 4·3 偏离量（预览那一处，探针 `Cardback_AM_Shield of Humanity`，比例 0.7207）

| | 外接框 | 原版画出来 | 我们（改前） | 差 |
|---|---|---|---|---|
| 拖拽预览 | 132×198（= 220×330 × 0.6） | **132×183.16** | **132×198** | 高 **14.8px**（竖向拉长 8.1%） |
| 卡背格 | 250×405 | 250×346.9 | 250×405 | 高 **58px**（+16.7%） |
| `Cosmetic Drawer` | 335.31×400 | **288.3**×400 | 335.31×400 | 宽 **47px**（+16.3%） |

### 4·4 改 / 没改

| 站点 | 本笔 |
|---|---|
| **拖拽预览**（`Core/DraggableController.cs` 的 `CosmeticPreview.Initialize`） | ✅ **改了** —— 新增 `public static PreserveAspectSize(t, boxW, boxH, out w, out h)`（**全仓唯一一份口径**，卡背格/抽屉那两条要复刻时**调它**）；框由 `BindView` 在建完那一刻记下（⛔ 不能现读 —— 现读到的会是上一次内接后的框 ⇒ **每拖一次缩一点**） |
| ⛔ **白名单外、请派工**：`Deck/DeckRuntime.cs` 的卡背格 `SetAspect(CosmoCellW/CosmoCellH)` · `Cosmetic Drawer` 那两句 `SetAspect(DrawerW/DrawerH)` · `Shell/CollectionWindow.cs` 卡背页 233 格（走 `MenuDraw.Rect`，含 `Cardback Shadow SDF` 那一层） | 修法一律：`float w,h; if (CosmeticPreview.PreserveAspectSize(tex, 框W, 框H, out w, out h)) { q.SetWorldHeight(h); q.SetAspect(w / h); }`。⚠️ 卡背格那处上面第 2078 行是 `q.SetTexture(tex)` **单参**重载 ⇒ 它先把 `_aspect` 冲成贴图比例，这跳**不能省** |
| ⛔ 白名单外：两处**过期注释**「画出来 = 132×198」/「`preserveAspect` ⇒ 比例取 220/330」 | `Deck/DeckRuntime.cs` 的 `BuildCosmeticDrag` 那段 · `UiCosmeticPreviewSize` 的 doc（随上一笔一起订正，⛔ 别留着） |

**断言**（宿主 `Editor/DeckScene.cs` 的 `TestCosmeticDrag` ④，**改了 3 处 + 新增探针挑法**）：
- **探针不再写死第 7 格**：起拖点必须在**第 2 行**（第 1 行 `cy≈358.5` 差 2.5px 进不了落点栏、第 3 行 `cy≈1168.5` 在屏幕外），在那 6 格里
  **挑「内接 ≠ 拉满」差得最远的一张**（233 张里有 **11 张**落在 `0.652~0.667` 窄带，写死哪一格就是在赌，赌输 = **假绿**）；
  前提 `probeD > 3f`（挑不到就**先红**，⛔ 别让下面两条空转）。
- 两条期望值**按起拖那格的贴图算**（`rt.UiCosmeticTex(names[Cell])`，与建格**同一条**会出声的取值路）：
  `CheckNear(宽, expW, 0.5)` · `CheckNear(高, expH, 0.5)`；落点 x 改成常量 `DropX = 240.1`（原来写 `cx6 − 260`，探针换列就会落到栏外）。
- 🧨 改坏法：删掉 `CosmeticPreview.Initialize` 里那跳 `PreserveAspectSize` ⇒ 回到恒定的 132×198 ⇒ 两条红。

---

## 五、没查清 / 停手的地方

1. 🔴 **本笔新写的 4 条断言一条都没实跑**（不许跑 Unity ⇒ 铁律 12 攒批）⇒ 红绿以收口那次 `CardBaseDemo.Run` / `DeckScene.Run` 为准。
   **最可能红的两处**：① `DeckScene` 那条「探针差 > 3px」的前提（**没实跑验证过**第 2 行那 6 张的比例分布）；
   ② `CardBaseDemo` 的「再刷一次逐位不变」——`SetData` 之后 `textBounds` 要求**逐位可复现**（我按「同数据 ⇒ 同度量」推的，**没实测**）。
2. ⚠️ **新落的两条档位在真实画面上的效果没量**：`badgeCounter` 与卡名的 `inkCenterY` 都是**推断**（`Geometry` = 墨心落框心，出处是
   TMP `TextMeshPro.cs:4193-4232` 的 `anchorOffset` + `A848` 那次的探针读数）；本笔**没跑 `CardFaceProbe` 再拍一次**。
   另一件同族没做的：**我们那批 alpha 裁过的卡背 PNG「裁得正不正」没量** —— 内接之后整体位置与裁切的对称性有关（上下裁得不均 ⇒ 会整体偏一点）。
3. ⚠️ **角标那层的「目标点」是不是原版那个框心没核**：`BadgeIconAt01(i)` 是上一轮挑的点，我**没去解 `TraitCounter` 的父链/RT** ⇒ 本笔只保证
   「墨心落在**我们既有的**目标点」。要「照原版」得读它的 RT + anchoredPosition（**没查**）。
4. ⚠️ **`A848` 那条「PnP 侧像素级字墨」照旧没量**（要 Unity / 要写从 PNG 量白字墨带的脚本）—— 与 `A表已收口_1018.md` 602 行同一句「没做到」，本笔**也没做**。
5. **查过哪些包 / 哪些词**（免下一位重查）：
   - 包：`bundle_staticgeneralassets_assets_all`（卡预制体）· `bundle_battleprefabs_vfxandmisc_assets_all`（`TraitCounter`）·
     `bundle_menus_assets_all`（`Collection Cosmetic` ×2 · `Cosmetic Display` · `Cosmetic Drawer`）·
     `bundle_cosmeticscardbacksimages_assets_all`（466 个 `Sprite/*.json`）· `bundle_scenes_scenes_{mainmenuwarpforge,battlearena1}`（复核 78）
   - 词：`NameText` · `DescText` · `ArmyText` · `RaceText` · `CostText` · `TraitCounter` · `Collection Cosmetic` · `Cosmetic Display` ·
     `Cosmetic Drawer` · `Cardback Container` · `preserveAspect`
   - **一处「没找到」**：`bundle_menus_assets_all/GameObject/` 里**没有** `NameText*`（只有 `2DCard*`；TMP 那半边**没逐颗扫**，
     所以只能写「GO 目录里没有」）；卡面 TMP 的**本体**在 `staticgeneralassets` 那一包，场景包里那 78 颗是**实例**，两处读数**逐值一致**。
