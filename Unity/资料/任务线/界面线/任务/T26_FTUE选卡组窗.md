# T26 · `Choose Army Deck FTUE Window`（选阵营送卡组窗）

> 状态：`✗` 未做 · 起于 2026-10-17（主对话现核）· 关联：**音效线 `S02`**（该窗的 `openSound`）
> ⚠️ 本件是**界面线**任务（一扇整窗），**不是**音效线任务 —— 音效线 S02 卡在它建成之后。

## 一、要做什么（做完之后有什么不同）

从壳里能开出一扇**「SELECT ARMY」**窗：横排若干个阵营卡（`Deck Container FTUE`），
每个阵营显示阵营图/名/描述/督军立绘，底部 `Continue` 钮带「已选阵营」图标；
点继续 ⇒ 领取该阵营的起始卡组（原版 `starterDecks` → `TutorialData/TutorialFTUEDeck/UltraMarines`）。
做完同时解锁 **`S02`**（原版这扇窗带显式 `openSound`）。

## 二、判据（逐条带出处）

**全量树 dump（现读，2026-10-17）→ `资料/普查产出_1017/UI/D_FTUE_ChooseArmyDeck_dump.txt`**
（命令：`python -I 工具/menu_dump.py bundle_menus_assets_all "Choose Army Deck FTUE Window" --depth 6`）

| 项 | 值 | 出处 |
|---|---|---|
| prefab | `bundle_menus_assets_all/GameObject/Choose Army Deck FTUE Window.json` | 本地有 |
| 类 | **`FTUESelectDeckWindow`** | dump 根行 |
| 根 rect | 1920×1080（全屏） | dump 根行 |
| 窗字段 | `closeOnESC · closeSound · container · content · continueButton · continueGroup · extraScaleSmallScreen · openSound · selectedFactionIcon · selectedFactionIconShine · starterDecks · type · updateNavPanel · useDefaultCloseSound…` | 根行（字段被截断，⛔ 用 `--no-layout` 或 `menu_rect.py` 读全） |
| `Menu Dark Background` | 4574.60×2572.36 @(−1327.3,−746.2) · `Image` + `BackgroundCloseButton` + `BackgroundOverDrawController` · 色 (0,0,0,0.773) | dump |
| `Title` | 0,55 → 1920,165.3（1920×110.35）· `EverguildTextMeshPro` + **`Localize`** · `'SELECT ARMY'` · 字号 **80** · Center/Midline | dump |
| `SubTitle` | 0,140 → 1920,219.4（1920×79.42）· `'You will receive a full deck and a Warlord from the selected army.'` · 字号 50 / 基准 45 / auto[18~50] | dump |
| `Continue Group` | 585.7,969 → 1334.3,1053.6（748.64×84.5） | dump |
| `Continue Button` | 同 rect · `EverguildButton` + `HorizontalLayoutGroup` + `LayoutGroupContentFixer` · 图 `40k_bt_underbutton` 485×83 九宫(240,0,240,0) · Sliced (0.837,0,0.914,1) · **`trans=1`** | dump |
| `Continue Button/Text (TMP)` | 217.19×85.51 · **`Localize` + `ContentSizeFitterMinMax`** · `'Continue'` · 字号 72 / 基准 24 / auto[18~72] | dump |
| `Selected Faction Icon` | 105.33² · preserveAspect | dump |
| `Armies` | 0,186.1 → 1920,948.2 · **`ScrollRect` + `Image` 两件 `m_Enabled = 0`**（组件级禁用 ≠ 节点隐藏）· h=1 v=0 mode=1 inertia=1 elasticity=0.1 decel=0.135 · **原版滚动灵敏度 = 100**（`N29` 表） | dump + `D_N29_滚动灵敏度逐视图.md` |
| `Armies/Viewport/Content` | 1920×522.02 · **`HorizontalLayoutGroup`** spacing **106.58** · pad(70,44,0,0) · align=4 · CSF 被禁 | dump |
| `Deck Container FTUE`（每个阵营一份） | 362.55×522.02 · **`FTUESelectDeckContainer` + `SimpleArmyImage`** · 字段 `armyDescription / armyObjects / button / glowImage / highlightObjects / warlordImage` · 子件：Glow 523.79²（`Glow UI W40K` 123×123, 色 (0,0.646,0.991,1)）· Warlord Image 523.79² · Army Icon 233.03² + Shadow（`40k_Smooth shadow background` 102×102 九宫 50,50,50,50）· Army Text 526.32×75.17（字号 52 / 基准 36 / auto[18~52]，Center/Capline）· Selected Text 371.07×128.26（**出厂 `INACT`**，`40k_replace_display` 468×152，ARF 3.07895）· Army Description 398.64×138.21（字号 45 / 基准 36 / auto[18~45]，Left/Top）· Select Button 523.79×774.83（`NonDrawingGraphic` + `EverguildButton`，`trans=0`） | dump |

**代码判据（未读，开工前必读）**：`d:/2/tools/decomp_full/FTUESelectDeckWindow__*.c` +
`FTUESelectDeckContainer__*.c`（尤其 `Open/Initialize/Continue/Select` 与 `starterDecks` 的取法）。

## 三、我们现状

- **整窗不存在**（`ls Shell | grep -i ftue` 0 命中；`grep -rn "FTUESelect" 代码` 0 命中）。
- 音效线 `S02` 因此**无落点**（`资料/任务线/音效线/任务总表.md:16`）。
- 本地已有可复用的同族件：`Shell/ItemDisplayWindow.cs` · `Shell/WelcomePopup.cs` · `Shell/TutorialMessagePopup.cs`（窗参/建树/注册的模板）。

## 四、怎么改（分步）

1. **先读反编译** `FTUESelectDeckWindow` 与 `FTUESelectDeckContainer` 的方法体，把「窗参 / 建树 / 选阵营 / Continue」逐句抄成判据（照 `ItemDisplayWindow` 文件头的写法）。
2. 新建 `Shell/FTUESelectDeckWindow.cs`（**类名 == 文件名**）+（若容器需要 MonoBehaviour）`Shell/FTUESelectDeckContainer.cs`。
3. 在 `Shell/WindowsManager.cs` 加注册口（照 `OpenItemDisplay` 的形状）。
4. **`openSound`**：按 prefab 字段值接进 `Shell/WindowSounds.cs`（这是 `S02` 的落点）。
5. 数据：`starterDecks` 指向 `TutorialData/TutorialFTUEDeck/UltraMarines` —— 先查本地 `TutorialData` 是否存在；不存在则**如实标注「本机没有这份数据」并按原版结构留空**（⛔ 不编卡表）。
6. 入口：原版是首启/教程链的一环；⚠️ 我们侧**没有菜单版教程状态机**（同 `TutorialMessagePopup` 的已知缺口）⇒ **建窗 + 注册，不伪造入口**，如实写进注释。

## 五、做完了怎么验收（现行口径：只做不验）

- 只跑 `bash d:/4/Unity/工具/typecheck.sh`；⛔ 不跑 `*.Run`、不起 player。
- 断言写进 `Editor/MainMenuScene.cs`（本件单独一节）：窗参逐值 + 根树关键件 rect/文案/字号 + `openSound` cue 名。

## 六、还差什么 / 判不准的

1. **反编译方法体还没读**（本文件只读了 prefab；方法体是开工第一步）。
2. `starterDecks` 的本地数据是否存在**没查**。
3. 根字段表被 dump 截断（`……[+26]`）—— 开工时用 `--no-layout` / `menu_rect.py` 读全。
4. 入口：原版走首启/教程，本机**判据不足** ⇒ 不伪造（与 `TutorialMessagePopup` 同口径）。
