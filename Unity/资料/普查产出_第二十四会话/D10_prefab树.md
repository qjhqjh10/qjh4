# D10 · 原版「Deck Editing Menu」prefab 树普查（只读）

> 2026-10-13 · 只读普查。真身 = `menus_assets_all.bundle` 里的 GameObject `Deck Editing Menu`
> （脚本类 `DeckEditingWindow : GameWindow`）。所有数字都是**现读原始 bundle**（UnityPy）
> 与 `bundle_menus_assets_all/MonoBehaviour/*.json` 拿到的，不是抄表。

## 结论
1. **根**：`Deck Editing Menu`（GameObject pathID `-5436018876966637788` · active=true · layer=5）
   直接子件**只有 1 个**（顺序）：`Content Area`。
   `Content Area` 的 5 个子件（顺序）：`Background` · `Header` · `Sidebar` · `Card Display` · `Cosmetic Display`。
2. **相机/Canvas/CanvasGroup**：
   - `Camera`：**没有**（整个 `bundle_menus_assets_all` 里 0 个 Camera 对象）。
   - `Canvas`：**没有**（该包全包有 63 个 Canvas，**子树里 0 个**）。
   - `CanvasGroup`：**有 6 个**，全部 `alpha=1` · `interactable=true` · `ignoreParentGroups=false`；
     `blocksRaycasts=false` 的 2 个 = 两条**拖拽跟着的那张卡**（下面整条路径）；
     另 4 个 `blocksRaycasts=true`。
3. **根 RectTransform**：`sizeDelta=(0,0)` · `anchoredPosition=(0,0)` · `anchorMin=(0,0)` ·
   `anchorMax=(1,1)` · `pivot=(0.5,0.5)` · `localScale=(1,1,1)` · `localRotation=identity` ·
   **`m_Father = null(0)` ⇒ 它自己就是 prefab 根**（全屏拉伸）。
   ⚠️ 子件 `Content Area`：`sizeDelta=(-167.175,-70.94)` · `apos=(83.5875,-35.5)` · `anchor 0,0→1,1`（= 四周内缩 167.175/70.94）。
4. **根上 MonoBehaviour：2 个**
   | comp pid | script pid | 类名 |
   |---|---|---|
   | `5918047927837519652` | `4613934872454734249` | `DeckEditingWindow` |
   | `-1321084918042398940` | `3068921175722844841` | `EventRelay` |
   （另有 1 个 `RectTransform`；根上**没有** Canvas / CanvasGroup）
5. **Selectable 节点（带 `m_Transition` / `m_SpriteState`）：29 个**
   - `EverguildButton` ×6 —— `trans=2`(SpriteSwap) 3 个：`Header/Generic Simplified UI Button_updated`、
     `Header/Close`、`Sidebar/Footer/Done`（这三个`m_SpriteState`**非空**：HL=`4884069812420410470`、
     P=`1985575716887326321`，`m_FileID=3` ⇒ **图在别的包**）；`trans=1`(ColorTint) 3 个：
     三条 `Deck Selector …` 按钮。
   - `EverguildToggle` ×12 —— `trans=1` 7 个（`Header/Filters` · `Window Options/Buttons/{Cards,Info,Cosmetics}` ·
     `Owned Toggle` · `Upgradable Toggle` · `Cosmetic FIlter/Filters/Owned Toggle`）；
     `trans=0` 5 个（`Card Filters` 下 `Army/Rarity/Cost/Type FIlter/Content/Toggle` ·
     `Cosmetic FIlter/Filters/Army Filter/Content/Toggle`）。
   - `EverguildInputField` ×2（`Sidebar/Window Options/Deck Name` · `Card Filters/…/Name FIlter/Input Field`）`trans=1`。
   - `Slider` ×9（`Deck Information cost drawer/Content/Deck CostQuanityt Row Drawer (1..8)` + 无后缀那个）`trans=0`。
   ⇒ **`m_SpriteState` 非空的只有那 3 个 SpriteSwap 按钮，其余 26 个四槽全 `null`**。
6. **规模**：**248 个节点**（247 个带 `RectTransform` + 1 个纯 `Transform` 3D 件）；
   **最深 12 层**。组件直方图：`RectTransform 247` · `MonoBehaviour 436` · `CanvasRenderer 182` ·
   `CanvasGroup 6` · `Transform 1` · `ParticleSystem 1` · `ParticleSystemRenderer 1`。
7. **挂载线索**：树内**没有任何** `WindowsManager` 或带 `Window` 字样的父级/兄弟节点；
   根 `m_Father=0` ⇒ 无父级。真正的线索在**根脚本字段**上：
   `DeckEditingWindow.windowsPlacement = 10` = `WindowsPlacement.World` · `type = 0`
   （枚举出处 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsPlacement.cs`：
   `None=0 / Canvas=5 / World=10 / Popup=15`）⇒ **由 `WindowsManager` 运行时实例化、放在 World 层**。
   本包 16768 个 GameObject 里**名字含 `Manager` 的 = 0 个**（含 `Window` 的 135 个，都是别的窗）。

## 证据（资产路径）
- 真包：`d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/menus_assets_all.bundle`
- 解包份：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（`GameObject/Deck Editing Menu.json` ·
  `RectTransform/RectTransform_2633458108729585444.json` · `MonoBehaviour/MonoBehaviour_5918047927837519652.json`）
- 类名：`assets_full/bundle_Waprforge_monoscripts/MonoScript/*.json` 的 `m_ClassName`（5175 条）
- 树表（已按 uGUI 跑过布局）：`python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 40`（247 行 + 表尾清单）
- 模板位表：同上换 `--no-layout`；或 `d:/4/Unity/工具/menu_rect.py bundle_menus_assets_all "Deck Editing Menu"`
- `WindowsPlacement` 枚举：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsPlacement.cs:1-6`

## 我搜过哪些文件/哪些词
- 文件：`menus_assets_all.bundle`（82038 个对象 / 16768 个 GameObject / 16510 个 RectTransform）·
  `bundle_menus_assets_all/{GameObject,RectTransform,MonoBehaviour,Canvas,CanvasGroup}/` ·
  `bundle_Waprforge_monoscripts/MonoScript/` · `globalgamemanagers/MonoScript/` · `decomp_full/` ·
  `Warpforge_code/Scripts/Assembly-CSharp/WindowsPlacement.cs`
- 词：`Deck Editing Menu` · `DeckEditingWindow` · `EventRelay` · `WindowsManager` · `Manager` ·
  `Window` · `Deck` · `Canvas` / `CanvasGroup` / `Camera`（按 UnityPy `obj.type.name` 判，不按名字）·
  `m_Transition` / `m_SpriteState` / `m_Alpha`（逐 MB JSON 的 **key 存在性**判）
- 工具：`工具/menu_dump.py` · `工具/menu_rect.py`（复用，未新写解析器）；补的两条探针是**内联 `python -c`**，无落盘。

## 没查清的
- `m_Transition` 那张表是按 **MonoBehaviour JSON 里 `m_Transition`/`m_SpriteState` 这两个 key 在不在**判的 ——
  原生 `Selectable` 子类若**没序列化**这两个字段就不会进那 29 条（本包实测 `menu_dump` 那 6 条 `trans=` 全在其中，未见反例）。
- `blocksRaycasts=false` 那两条 Drag Controller 下的卡**是否运行期被改写**（本表是 prefab 出厂值）—— 未查。
- 那两张 SpriteSwap 高亮图（`4884069812420410470` / `1985575716887326321`，`m_FileID=3`）**只拿到 pid、没解成 sprite 名**（跨包引用）。
- `DeckEditingWindow` 余下 ~20 个 `[SerializeField]` 字段的**运行时语义**（谁在什么时候改）未查。
