# R · `LanguagesDropdown > Template` 子树逐字段（只读勘察，2026-10-17）

**判据**：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（prefab `Main Menu Settings Window`）
· 反编译 `d:/2/tools/decomp_full/` · 真包 `D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`。
**坐标口径**：下表「距根左上」= `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Main Menu Settings Window" --depth 12 --relative`
的输出（根 = 1920×1080 左上原点、y 向下）。⚠️ **窗口根 `Main Menu Settings Window` 的 RT `m_LocalScale = 0.9`**
⇒ 表里「宽×高 / 距根左上」**已含 ×0.9**；「设计值」= 把窗口 px ÷ 0.9 还原的 prefab 字段值。

## 一、主表（`Main Menu Settings Window > Menu Area > Tab Content > General Tab > Language Selector > LanguagesDropdown > …`）

| 路径 | 类名（组件 pid → 类） | 宽×高（窗口 px / 设计值） | 距根左上 (x1,y1) | 锚点 / pivot（prefab 字段） | 字号（基准 / auto） | 对齐 · 折行 | sprite 名 | 九宫格 (L,B,R,T) | 颜色 | active |
|---|---|---|---|---|---|---|---|---|---|---|
| `Language Selector`（父） | `LanguageSelector`（MB 8955730638659813286；字段只一个 `languagesDropdown`→ `5610346062133034918`） | 751.36×53.46（834.85×59.40） | (632.9, 359.1) | aMin(0,0.5) aMax(0,0.5) pivot(0,0.5) | — | — | 无 | — | — | T |
| `LanguagesDropdown` | `UnityEngine.UI.Image` + `TMPro.TMP_Dropdown`（MB `5610346062133034918`） | 360.60×53.46（400.666×59.398） | (632.9, 359.1) | aMin(0,0.5) aMax(0,0.5) aPos(0,0) pivot(0,0.5) | — | — | **`40K_dropdown_field_closed`** | **(60,35,60,35)** | (0.2863,0.9647,0.6863,1) | T |
| `…> Label` | `TMPro.TextMeshProUGUI`（MB `-4796174522888585306`）+ `EverguildTextController`（MB `-73537465749766234`，45/10） | 342.60×41.76（380.67×46.40） | (641.9, 365.4) | aMin(0,0) aMax(1,1) aPos(0,-0.5) size(-20,-13) pivot(.5,.5) | **18 / 基准14 / auto 18~40**（⚠️ 运行时 `Awake` 改 **10~45**，见 §三） | Left / Middle · **不折行**(0) | 无 | — | `m_fontColor`(0.6698,0.6698,0.6698,1) · `m_fontColor32`=**0xFF323232**（两者不一致，见 §五） | T |
| `…> Arrow` | `UnityEngine.UI.Image`（MB `4060538377871589286`） | 18.00×18.00（20×20） | (971.0, 376.8) | aMin(1,0.5) aMax(1,0.5) aPos(-15,0) pivot(.5,.5) | — | — | **`40K_dropdown_arrow_closed`** | 无 | (0.0902,0.3529,0.2510,1) · `preserveAspect=1` | T |
| **`…> Template`** | `UnityEngine.UI.Image` + `UnityEngine.UI.ScrollRect` + `Canvas`(pid `5080032225221836710`) + **`DropdownList`**（MB `-4690957962900242522`，游戏自己的类，见 §三） | **356.10×516.56**（395.667×573.960） | **(632.9, 405.6)** | aMin(0,0.5) aMax(1,0.5) aPos(**-2.5,-22**) pivot(.5,**1**) sizeDelta(**-4.9998, 573.96**) scale=1 | — | — | **`40K_dropdown_bg`** | **(23,20,23,20)** | (0.2863,0.9647,0.6863,1) `m_Type=1`(Sliced) **ppuMul=1.09** | **F** ← 出厂 inactive |
| `…> Template > Viewport` | `UnityEngine.UI.Mask` + `UnityEngine.UI.Image`（MB `2090174374868778918`；Mask MB `-7196551286430269530`） | 340.80×516.56（378.667×573.960） | (632.9, 405.6) | aMin(0,0) aMax(1,1) aPos(0,0) sizeDelta(-17,0) pivot(**0**,**1**) | — | — | `UIMask`（Unity 内置） | (10,10,10,10) | (0.3686,0.8941,0.5882,1) · **`m_ShowMaskGraphic=0`** | T（`ANC✗`：祖先 Template 不激活） |
| `…> Template > Viewport > Content` | **无任何组件**（只有 RectTransform） | 340.80×37.55（378.667×41.7226） | (632.9, 405.6) | aMin(0,**1**) aMax(1,**1**) aPos(0,≈0) sizeDelta(**0, 41.7226**) pivot(.5,**1**) | — | — | 无 | — | — | T（`ANC✗`） |
| `…> Content > Item` | `UnityEngine.UI.Toggle`（MB `7188086145049853862`，`m_IsOn=1`） | 340.80×**36.78**（378.667×**40.8707**） | (632.9, 406.0) | aMin(0,**0.5**) aMax(1,**0.5**) aPos(0,0) sizeDelta(0,40.8707) pivot(.5,.5) **scale=1** | — | — | 无 | — | — | T（`ANC✗`） |
| `…> Item > Item Background` | `UnityEngine.UI.Image`（MB `6332618492201041830`；= Toggle 的 `m_TargetGraphic`） | 340.80×36.78（同上） | (632.9, 406.0) | aMin(0,0) aMax(1,1) aPos(0,0) sizeDelta(0,0) | — | — | **`40K_dropdown_item`** | 无 | (0.2863,0.9647,0.6863,1) `m_Type=0`(Simple) | T |
| `…> Item > Item Checkmark` | `UnityEngine.UI.Image`（MB `3851575143278215078`；= Toggle `graphic`，`toggleTransition=1`） | 18.00×18.00（20×20） | (632.9, 415.4) | aMin(**0**,0.5) aMax(**0**,0.5) aPos(**10**,0) sizeDelta(20,20) | — | — | `Checkmark`（Unity 内置） | 无 | (1,1,1,1) | T |
| `…> Item > Item Label` | `TMPro.TextMeshProUGUI`（MB `4908040525706330022`；= Dropdown 的 `m_ItemText`） | 313.80×34.08（348.67×37.87） | (650.9, 407.8) | aMin(0,0) aMax(1,1) aPos(**5**,**-0.5**) sizeDelta(**-30**,**-3**) → **padding 左20 右10 下1 上2** | **30 / 基准14 / auto 18~40** | Left / Middle · **折行 Normal**(1) | 无 | — | `m_fontColor`(0.7830,0.7830,0.7830,1) · `m_fontColor32`=0xFFC8C8C8（一致） | T |
| `…> Template > Scrollbar` | `UnityEngine.UI.Image` + `UnityEngine.UI.Scrollbar`（MB `9072350931194904486`） | 18.00×516.56（20×573.960） | (971.0, 405.6) | aMin(**1**,0) aMax(**1**,1) aPos(0,0) sizeDelta(**20**,0) pivot(**1**,**1**) | — | — | `Background`（Unity 内置） | (10,10,10,10) | (1,1,1,1) · `m_Type=1`(Sliced) | T（`ANC✗`） |
| `…> Scrollbar > Sliding Area` | 无组件 | **0.00**×498.56（0×554.0） | (980.0, 414.6) | aMin(0,0) aMax(1,1) aPos(0,0) sizeDelta(**-20,-20**) pivot(.5,.5) | — | — | 无 | — | — | T（`ANC✗`）← ⚠️ 宽 0（`sizeDelta.x=-20` = 父宽20 − 20） |
| `…> Sliding Area > Handle` | `UnityEngine.UI.Image`（MB `9148800865146666918`；= Scrollbar `m_TargetGraphic`/`m_HandleRect`） | 18.00×480.32（20×533.96） | (971.0, 441.8) | aMin(0,0) aMax(1,**0.9273**) aPos(0,0) sizeDelta(20,20) pivot(.5,.5) | — | — | `UISprite`（Unity 内置） | (10,10,10,10) | (1,1,1,1) · `m_Type=1`(Sliced) | T（`ANC✗`） |

## 二、组件参数（同表出处：各 `MonoBehaviour_<pid>.json` / prefab `RectTransform_<pid>.json`）

- **`ScrollRect`（Template，MB `2692781255615610790`）**：`m_Content`=`6031937378233319334`(Content) · `m_Viewport`=`-7024256747985076314`(Viewport) ·
  `m_Horizontal=0 m_Vertical=1` · `m_MovementType=2`(**Clamped**) · `m_Elasticity=0.1` · `m_Inertia=1` · `m_DecelerationRate=0.135` ·
  **`m_ScrollSensitivity=1.0`** · `m_HorizontalScrollbar=null` · `m_VerticalScrollbar=`Scrollbar ✓ ·
  `m_HorizontalScrollbarVisibility=0` · **`m_VerticalScrollbarVisibility=2`** · `m_HorizontalScrollbarSpacing=0` · **`m_VerticalScrollbarSpacing=-3.0`**。
- **`Scrollbar`（MB `9072350931194904486`）**：`m_Direction=2`(**BottomToTop**) · **`m_Value=0.0`** · **`m_Size=0.9273074865341187`** · **`m_NumberOfSteps=0`** ·
  `m_Transition=1`(ColorTint) · 四色 = Normal 白 / Highlighted 0.9608 / Pressed 0.7843 / Disabled (0.7843,α0.502) · `m_ColorMultiplier=1` `m_FadeDuration=0.1`。
- **`Canvas`（Template 自身）**：`m_RenderMode=2`(WorldSpace) · **`m_OverrideSorting=true`** · `m_SortingLayerID=0`(Default) · **`m_SortingOrder=32767`** · `m_PlaneDistance=100` · `m_ReceivesEvents=true`。
- **`TMP_Dropdown`（`LanguagesDropdown`，MB `5610346062133034918`）**：`m_Template`→Template ✓ · `m_CaptionText`→`Label` ✓ · **`m_CaptionImage=null`** ·
  **`m_Placeholder=null`** · `m_ItemText`→`Item Label` ✓ · **`m_ItemImage=null`** · `m_Value=0` · `m_MultiSelect=0` · **`m_Options`=空表**（运行时由 `LanguageSelector` 填） ·
  `m_AlphaFadeSpeed=0.15` · `m_Transition=2`(**SpriteSwap**) · `m_TargetGraphic`→自身 Image ·
  `m_SpriteState`：Highlighted=Pressed=Selected=**`40K_dropdown_field_opened`**（727×102，九宫 60,35,60,35）、Disabled=null ·
  `m_Colors`：Normal(1,1,1,1) Highlighted/Selected(0.9608…) Pressed/Disabled(0.7843…) Disabled α=0.502。

## 三、行为（反编译，逐条带出处）

1. **`LanguageSelector`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/LanguageSelector.cs` = 签名桩：唯一字段 `TMP_Dropdown languagesDropdown`；方法体见 `.c`）：
   - `Initialize()`（`decomp_full/LanguageSelector__Initialize.c`）→ `languagesDropdown.onValueChanged.AddListener(OnLanguageChanged)` 然后 `ResetLanguagesDropdown()`。
   - `ResetLanguagesDropdown()`（`…__ResetLanguagesDropdown.c`）→ ① `options` 清空 ② 遍历一个**静态 `string[]`**，每项 `new TMP_Dropdown.OptionData()`，
     `text = LocalizationManager.GetTermTranslation("MainMenu/Settings/LanguageName/" + arr[i])`（前缀 = `stringliteral.json` 地址 `0x42c1910` → 实读该串），
     加进 `options` ③ 若 `LocalizationManager.CurrentLanguage == arr[i]` 记下下标 ④ **末尾 `languagesDropdown.value = 该下标`**。
     ⚠️ 那个静态数组**没钉到具体类**（候选 `GameStaticData.languagesDefinition`，**未确证**；见 §五）。
   - `OnLanguageChanged(int i)`（`…__OnLanguageChanged.c`）→ `Loc.Log(arr[i])` → `LocalizationManager.CurrentLanguage = arr[i]` → **再调一次 `ResetLanguagesDropdown()`**
     → 再 `BattleManager.BroadcastChangedLanguage()` / `BattleHud.BroadcastChangedLanguage()`（`op_Inequality(实例,null)` 为真才调）。
   - `OnDestroy()` → `onValueChanged.RemoveListener(OnLanguageChanged)`。
2. **`DropdownList`**（⚠️ **不是** `UnityEngine.UI.Extensions.DropDownList` —— 那是另一个类）：MonoScript `5802042210957062025`，`m_Namespace=''`、`m_AssemblyName='Assembly-CSharp'`；
   `DropdownList.cs` 桩 = 字段 `Canvas canvas` + `string targetSortingLayer`（带 `[SortingLayer]`）+ 方法 `OnEnable`；
   `DropdownList__OnEnable.c` 实读 = 一行：**`canvas.sortingLayerName = targetSortingLayer;`**（prefab 里字段值 = **`"PopUps"`**）。
   ⇒ 这一颗**只干一件事**：把下拉列表那个 Canvas 的 sortingLayer 顶到 `PopUps`。
3. **打开 / 收起 / 选中**（`TMP_Dropdown` 本体）：
   - ⚠️ `decomp_full/` 里**没有 `TMPro_TMP_Dropdown__*.c` 的方法体文件**（`ls | grep -i tmp_dropdown` 零命中）；但 `script.json` 里**方法齐全**
     （`Show / Hide / SetupTemplate / CreateItem / AddItem / OnSelectItem / OnPointerClick / OnCancel / OnSubmit / CreateBlocker / DestroyBlocker / CreateDropdownList / AlphaFadeList / SetAlpha / DelayedDestroyDropdownList / ImmediateDestroyDropdownList …`，地址见下）。**方法体我按两条旁证读**：
     (a) **本 build 的 `Unity.TextMeshPro.dll`**（`d:/2/tools/il2cpp_out/DummyDll/`）里这批成员名逐条都在（`SetupTemplate`/`CreateItem`/`AddItem`/`OnSelectItem`/`CreateDropdownList`/`m_Dropdown`…）；
     (b) **`global-metadata` 的字面量表**（`il2cpp_out/stringliteral.json`）里 `"Dropdown List"`(0x4288F60) · `"Blocker"`(0x4293F58) · `"Item "`(0x4292C00) ·
     TMP 五条报错原文（`"The dropdown template is not valid. …"` 0x429EB10 起）**全在** ⇒ 本 build 用的是**未改动的官方 TMP_Dropdown**。
     (c) 用 `工具/disasm_va.py` 反汇编本 build 的 `Show`（VA `0x182e6aba0`）实核前 220 条指令：开头 `StopCoroutine(m_Coroutine)`+`ImmediateDestroyDropdownList()`、
     随后 `IsActive()`(vtable +0x1c8) / `IsInteractable()`(+0x2b8)、`cmp byte[rsi+0x170],0` → 调 `0x182e6a4c0`(=**`SetupTemplate`**) → 复核；与官方源码同形。
     ⇒ 下面「关键行」= **官方源码 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/TMP/TMP_Dropdown.cs`**
     （⚠️ **本机编辑器是 6000.3.23f1、游戏是 6000.2.6f2** —— 行号取自本机这份源码，**版本未必逐字相同**，我按上面 (a)(b)(c) 核过形状一致）：
   - **点开**：`OnPointerClick`(`:742→Show()`) / `Submit`(`:753→Show()`) → `Show()`(`:778`)：`IsActive() && IsInteractable()` 且 `m_Dropdown==null` 才继续；
     取 rootCanvas → **`if(!validTemplate){SetupTemplate(); if(!validTemplate) return;}`** → `m_Template.gameObject.SetActive(true)`
     → `m_Template.GetComponent<Canvas>().sortingLayerID = rootCanvas.sortingLayerID` → **`m_Dropdown = CreateDropdownList(m_Template.gameObject)`**（= `Instantiate`，`:1099`）
     → `m_Dropdown.name="Dropdown List"` → `m_Dropdown.SetActive(true)` → **`dropdownRectTransform.SetParent(m_Template.transform.parent, false)`（= 克隆体挂在 `LanguagesDropdown` 下、与 Template 同级）**
     → 逐项摆位（`rectTransform.anchoredPosition.y = offsetMin.y + itemSize.y*(n-1-i) + itemSize.y*pivot.y`；`offsetMin/itemSize` 由 **Item 与 Content 的 rect 差** 算出，`:840-844`）
     → `AlphaFadeList(0.15, 0f→1f)` → **`m_Template.gameObject.SetActive(false)`** + `itemTemplate.gameObject.SetActive(false)` → **`m_Blocker = CreateBlocker(rootCanvas)`**。
     `SetupTemplate()`(`:642`)：`GetComponentInChildren<Toggle>()` 拿 Item，给 Item **`AddComponent<DropdownItem>()`**，并要求 Item 的父有 `RectTransform`、`itemText`/`itemImage` 是 Item 的后代 —— **本 prefab 全部满足**（Item 是 Toggle、父是 Content(RT)、Item Label 在 Item 下）。
   - **点空白收起**：`CreateBlocker`(`:1009`) 建 `new GameObject("Blocker")`（铺满 rootCanvas、`Image` 色 = `Color.clear`、外加 `GraphicRaycaster`/`CanvasGroup(ignoreParentGroups=true)`），
     关键行 **`:1073-1074`：`Button blockerButton = blocker.AddComponent<Button>(); blockerButton.onClick.AddListener(Hide);`** ⇒ 点空白 = 点 Blocker = `Hide()`。
   - **ESC 收起**：`m_Blocker` 上另有内部类 `DropdownBlocker`（`:46`）与 `TMP_Dropdown.OnCancel`(`:763` → **`:765` `Hide()`**)；
     `disasm_va 0x182e699f0` 实核：`OnCancel` 函数体 = `xor edx,edx; jmp 0x182e695c0`(**= `Hide`**) —— 纯尾调用。
   - **点行选中**：`AddItem`(`:1141`) 里给每行 `item.toggle.onValueChanged.AddListener(x => OnSelectItem(item.toggle))`（`:900`）；
     `OnSelectItem(Toggle)`(`:1247`)：**按 Toggle 在父里的兄弟序算行号**（`i-1`，`:1250-1259`）、`value = selectedIndex`（非多选支，`:1300`）→ **末尾 `Hide()`(`:1310`)**。
     `m_MultiSelect=0` ⇒ **Nothing/Everything 两条特殊项不走**（`:845-870` 只在多选时加）。
   - **收起**：`Hide()`(`:1199`) → `AlphaFadeList(m_AlphaFadeSpeed,0f)` → `StartCoroutine(DelayedDestroyDropdownList(0.15))`（`:1205`，等 0.15 s 再 `ImmediateDestroyDropdownList`）
     → `DestroyBlocker(m_Blocker)` → `m_Blocker=null` → `Select()`。
   - **有效帧**：`0.15 s`（`m_AlphaFadeSpeed`）+ 一个 `WaitForSecondsRealtime(0.15)` 的延迟销毁协程。
     `Item` 命名：`"Item " + items.Count + ": " + text`（`:1148`）。

## 四、工程现状（对照用 · 只报位置与做法，未改）

- **`Shell/SettingsWindow.cs` 图像页「画质下拉」= 假下拉（点击循环，不是列表）**：`:1096` 建 `Quality Selector` 行（常量 `:154-156` `QualL/T/R/B=551.52/267.71/1386.37/327.10`、`QualBoxR=952.18`）；
  `:1101` `Rect(row,"Quality DropDown", QualL..QualBoxR, "40K_dropdown_field_closed", QContent)`；`:1102` `Text(row,"Quality Value",…)`；
  `:1103` `Text(row,"Quality selector text","Quality")`；`:1107` **`Hit(row,"QualityHit", …, QOverlay, CycleQuality, …)`** ⇒ **命中区点击 = `CycleQuality()` 循环切档**。
- **同窗语言那一行同病**：`Shell/SettingsWindow.cs:915-935` 的 `CycleLanguage()` 注释里**已自认偏离**（原版是 12 行滚动列表，本批没建）。
- **战斗内设置窗也有一份**：`Battle/SettingsPanel.cs:60-65`（框 + 当前语言名 + `40K_dropdown_arrow_closed`）· `:227` 两个图名常量 · `:472-475`/`:551-553` 命中区与「已知偏离」注释；`:731` 「这一下点在语言下拉框上吗」。
- **`CardPresentation/` 下没有任何真的 `Dropdown`/`Template` 子树实现**（`grep -rn -i dropdown --include=*.cs` 只命中上面几处 + `Deck/DeckRuntime.cs:1007-1008,2327`（那是**导入弹窗的输入框**用了 `40K_dropdown_bg`）+ `Core/Loc.cs:11,13,174`（纯数据）+ 三处 `Editor/*.cs` 的断言/注释）。
- **可复用的现成件**：`Shell/MenuScroll.cs`（`:167 LeftAligned(PxRect viewport,float contentW)` / `:173 TopAligned` / `:212 ScrollBy` / `:234 Wheel` / `:267 BeginDrag` / `:279 DragTo`）
  —— 视口裁切 + 拖拽/滚轮/偏移，是工程里**唯一的滚动列表件**；另有 `Shell/ImageQuad.SetRenderQueue` 用于分层（别靠 z）。

## 五、没查清 / 存疑（⛔ 不许当已知用）

1. `Label` 的 `m_fontColor32 = 0xFF323232` 与 `m_fontColor = (0.6698,0.6698,0.6698,1)` **对不上**（Item Label 的这两个是一致的 0xFFC8C8C8 / 0.783）。
   运行时以哪个为准 = **查不到**（`TMP_Text` 本体不在 `decomp_full`）。
2. `EverguildTextController.Awake()`（`decomp_full/EverguildTextController__Awake.c`）实读：`Application.isPlaying` 为真 → 再看**一个静态 bool**（`*(char*)(<类>+0xb8)+0x11c`，**这个类是哪一个没钉死**）为假就 `return`；
   为真时：`maxFontSize(45)>0` ⇒ `enableAutoSizing? fontSizeMax=45 : fontSize=45`；`minFontSize(10)>0` ⇒ `fontSizeMin=10`。
   ⇒ `Label` 的**运行时 auto 区间是 10~45**（prefab 序列化写的是 18~40）；**Item Label 上没有这个组件**，仍是 18~40。⚠️ 那个静态 bool 没解出来 ⇒ 「45/10 到底生效不生效」**未验**。
3. `ResetLanguagesDropdown` 里那个静态 `string[]` 属于哪个类 = **未确证**（候选 `GameStaticData.languagesDefinition`，形状最像但没找到把类指针 `DAT_18427be00` 钉到类名的路）。
4. 字库：`Label` 与 `Item Label` 的 `m_fontAsset` 都是 PathID `3485036404935369831`（`m_FileID=5` 跨文件）+ `m_sharedMaterial` `-5050103597772954521`；
   在真包 `fonts_assets_all.bundle` 里按 pid 查到 = **`Pragati-Regular SDF`** / 材质 **`Pragati-Regular Atlas Material`**（`D:/tmp/wf_r/fontpid.py` 的输出，判据 = `o.path_id`）。
   ⚠️ 只在**文件名含 font 的包**里查了这两条 pid ⇒ 若别的包也有同号对象，理论上可能撞号（未做全库扫）。
5. `Template > Canvas` 的 `m_SortingLayerID=0` 是**出厂序列化值**；运行时被 `DropdownList.OnEnable` 改成字符串 `"PopUps"`（`sortingLayerName`）—— **改完的 ID 未验**（`PopUps` 是不是工程里已注册的 sorting layer，我没查 `TagManager`）。
6. `Scrollbar.m_Size = 0.9273` 是 **prefab 里的序列化值**（= 编辑器上一次烘焙的）；运行时会不会被 `ScrollRect.UpdateBounds` 重算 = **未验**。
7. 12 个语言选项的**具体 12 个枚举名**没在这颗 prefab 里（`m_Options` 出厂是空表）；枚举 `AvailableLanguages` 有 **12 个值**（0/10/…/110，`il2cpp_out/dump.cs:46049-46062`），但**哪个走哪些**取决于上面第 3 条那个 `string[]`。
8. `Arrow` 用的是 `40K_dropdown_arrow_closed`；同目录还有 `40K_dropdown_arrow_opened`（同尺寸 46×19）**本 prefab 未引用**（只核了这颗 Arrow 的 `m_Sprite`，没全库找谁在用）。
