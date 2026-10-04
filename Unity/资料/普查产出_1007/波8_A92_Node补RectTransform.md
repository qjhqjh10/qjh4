# 波 8 · A92 —— `MenuDraw.Node` 等空节点工厂补 `RectTransform`

> 执行代理报告（2026-10-07）。**白名单内 9 个文件**，⛔ 未碰 `Editor/ShellScene.cs`（归 ㉑）、未碰任何黑名单、未动 git、未跑 Unity、未改两张正本。
> 判据来源：`资料/待办判据_1006.md` §A92 ＋ `项目任务.md` A92 行 ＋ **解包 prefab 的原版节点类型**（见 §二·4）。

---

## 一、结论

**做完（A92 点名的影响面 100% 覆盖）。定性：`零可见行为变化`** —— 不是「看着没事」，而是可以静态证死：**我们这套实现里根本没有 uGUI 运行时**（没有 `Canvas` / `EventSystem` / `GraphicRaycaster` / `LayoutGroup` 的任何实例，只有注释里在讲原版），而节点类型能影响的只有这三样东西；位置/渲染/命中的判据全部走**世界坐标**（`.position`）与 `ImageQuad.WorldW/WorldH`，一条都不读 `RectTransform`。

**逐条交代「会改行为的那三件事」**（判据原文 §5 点名的）：

| 会上什么行为 | 实况 | 依据 |
|---|---|---|
| **参与布局组** | **不可能** —— 全工程**没有任何 uGUI `LayoutGroup` 实例**（`grep -rn "LayoutGroup"` 的命中**全在注释里**；`Core/UguiRect.cs` 是**纯数学**，不建组件） | `grep -rn "GetComponent<RectTransform>\|LayoutGroup\|AddComponent<RectTransform>" --include=*.cs .` ⇒ 0 个实例 |
| **被 `GetComponentsInChildren<RectTransform>()` 扫到** | 全工程只有 **2 处**读 `RectTransform` 的代码，**两处都不受本改动影响**：① `Editor/ChatBoxProbe.cs:76` `lb.GetComponentInChildren<RectTransform>()` —— 它在 **`Label` 自己那棵子树里往下找**（我们改的是**祖先**，方向相反）；② `Editor/RewardsScene.cs:2133` 那条断言断的正是**例外那一件**（见 §二·3） | 同上 grep（跨 `Assets/` 全仓，除 DOTween 插件外无其他命中） |
| **`rect` 不再是 0** | **没有任何一处读工厂建出来的节点的 `.rect`**（判据原文「0 处」复核成立）。全工程唯二真读 `.rect` 的：`Editor/ChatBoxProbe.cs:77`（读的是 `Label` 子树）与 `Battle/ChatPopupPanel.cs:120`（读的是它自己的 **纯 C# 类** `Btn.rect`，`ChatPopupPanel.cs:82-86`，与 `RectTransform` 无关） | `grep -rn "\.rect\b" --include=*.cs .` 逐条看 |

**⚠️ 一处必须写明的「没做」**（不是「影响小」，是**A92 只剩的那一半**）：节点现在**有了** `RectTransform`，但 **`sizeDelta` 没写** ⇒ `rect.width/height` 还是 `RectTransform` 的默认值、**仍然不等于 `PxRect` 的 W/H** ⇒ 判据原文那句「「空节点 + `PxRect`」的宽高验收不了」**目前仍然成立**。**要做**，判据 → 每个 `Node(...)` 传进去的 `PxRect`；做法 → `Node` 里补 `sizeDelta`（换算与 `Local()` 同一份）**并逐类核一次父链缩放**（非单位缩放的窗口要单独算）。本轮不做的唯一理由：判据原文把 A92 的「要做」写成「**给这类节点补 `RectTransform`（或在 `MenuDraw.Node` 上直接建）**」= **类型那一件**，写 `sizeDelta` 是**又一个可观察变化**、得自带一条断言与一次单位核查 —— 归下一批，别丢。

---

## 二、证据

### 1. 改法（判据原文点名的那一条，逐字遵守）

全 6 处工厂一律 `new GameObject(name, typeof(RectTransform))`；**⛔ 一处都没有用 `AddComponent<RectTransform>()`**（复核：`grep -rn "AddComponent<RectTransform>" --include=*.cs .` ⇒ **0 命中**）。

| # | 文件:行（改后） | 原来是 | 现在是 | 盖住的调用点 |
|---|---|---|---|---|
| 1 | `Shell/MenuDraw.cs:61` | `new GameObject(name).transform` | `new GameObject(name, typeof(RectTransform)).transform` | `MenuDraw.Node(` **207** 处（`grep` 实测；判据原文估 200） |
| 2 | `Shell/MenuWindowBase.cs:135` | `new GameObject(name)` | `new GameObject(name, typeof(RectTransform))` | `.Node(` 总数 261 − 207 = **54** 处（`MenuWindowBase.Node` / 继承它的 `RewardsWindow.Node`）＋ 直调 `New(` 的 5 处（`MenuWindowBase.cs:326` `Buttons` / `:417` `Hit` / `ShopWindow.cs:670` `Hit` / `Editor/RewardsScene.cs` 的 6 处自检探针） |
| 3 | `Shell/MainMenuRuntime.cs:127`（**本文件私有的 `New`**） | 同上 | 同上 | `New(` 20 处（`Background` / `Navigation Panel` / `Buttons Container` / `Main Menu Navigation Button - *` / `Hit` / `Upper bar` / …） |
| 4 | `Shell/PracticeModePopup.cs:405`（**本文件私有的 `New`**） | 同上 | 同上 | `New(` 9 处（`Deck info` / `Background Info` / `General container` / `Army Selector` / `Viewport` / `Filters` / `Decks Scroll view` / `Content`） |
| 5 | `Shell/MenuDraw.cs:1112`（`Hit` 里那个透明命中区节点） | `new GameObject(name).transform` | 同上 | `MenuDraw.Hit(` **54** 处 |
| 6 | `Shell/PracticeModePopup.cs:1566`（`HitOn` 里那个） | `new GameObject(name)` | 同上 | `HitOn(` 14 处 |
| 7 | `Battle/WaitBanner.cs:148`（本文件私有的 `New`） | 同上 | 同上 | 2 处（`Create` / `CreateWithArt`） |

**#5 / #6 为什么也在里面**（⚠️ 这两处**不在**判据原文那份 250–280 的数里，是执行中判的，**理由写在这里备查**）：它们和 `MenuWindowBase` 的 `New(b, "Hit")`（`MenuWindowBase.cs:438`，Tab 键那一颗）建的是**同一种东西**（透明 quad + `WindowButton` 的命中区节点），而代码自己就写着这两条路是同一份（`MenuDraw.cs:1076-1077`「这段原来只有 `MainMenuSubmenuWindow.AddHit` 一份…**收口到这里，那边转调**」）。**改一半 = 同一件东西两种类型**，比统一错更难查。判据也一样，而且**就写在原地**：`MenuDraw.cs:1073-1075` 的 `<summary>` 里那一句 —— 「**原版这一层就是按钮自己的 `RectTransform`**」（`:1074` 逐字）。
**⛔ 可回退**：若调度台认为越界，把 `MenuDraw.cs:1114` / `PracticeModePopup.cs:1568` 两行改回 `new GameObject(name…)` 即可，其余改动不依赖它们。

### 2. 保住的「必须保住」那两处（判据原文 §2）

| 判据原文写的位置 | **实际位置（行号已漂）** | 是什么 | 怎么保住的 |
|---|---|---|---|
| `Shell/ForgeTab.cs:396-397` | **`Shell/ForgeTab.cs:401-408`** —— `var neb = RewardsWindow.New(body, "Particle System nebula");`（原 396-397 那两行是 `RewardsWindow.Node(...)`，**行号对不上**，按内容认的） | 原版这一件**只有 `Transform`**（3D 子件） | 单开一个**显式名下**的新方法 **`MenuWindowBase.NewPlainTransform`（`Shell/MenuWindowBase.cs:148-153`）**，ForgeTab 那一行改成调它 ⇒ 那件**仍然是裸 `Transform`**，逐字保真。⛔ 没用 `AddComponent`、也没给 `New` 加开关（理由：让「例外」**显式可数** —— 数一下 `NewPlainTransform` 的调用点 = 原版有几个裸 `Transform` 节点；现在全工程**恰好 1 处**） |
| `Editor/RewardsScene.cs:1810` | **`Editor/RewardsScene.cs:2133-2134`**（原 1810 那个位置现在是一段 `HighlightBG` 的断言，**行号对不上**，按内容认的） | 配套断言：`CheckTrue(psNeb.GetComponent<RectTransform>() == null, "…**没有** `RectTransform`…")` | **一个字都没动它**（本轮禁改别人的断言）⇒ 语义不变、仍然绿：那件现在走 `NewPlainTransform`，**类型逐字同旧** |

### 3. 原版节点类型怎么查的（本件的**根本判据**，第一手）

不靠任何转抄，直接读解包 prefab 的**组件列表**（脚本：`GameObject/m_Name → m_Component[].m_PathID`，再看这个 PathID 落在 `RectTransform/` 还是 `Transform/` 目录）：

| 包 | `RectTransform` | 裸 `Transform` | **决定** |
|---|---|---|---|
| `bundle_menus_assets_all` | **16510** | **258** | 那 258 个全是**卡框 3D 子锚与粒子件**（`Textbackgrounds` 41 · `Tactic Container` 41 · `EffectAnchor` 41 · `Card Info` 41 · `MinionOrWarlord Container` 40 · `Wave Shine/right/left` · `Trails` · `Sparks` · `CardFlip` · `Glow*` · `Smoke ring` · `Rings expand` · `RewardAppearParticle` · `Particles Rank Up` · `Boosterpack Open Card Rarity 1-4` · `Particle System nebula` 2 · …）—— **一个菜单容器都没有** |
| `bundle_scenes_scenes_mainmenuwarpforge` | **592** | **105** | 同构（裸的那批同族） |
| `bundle_scenes_scenes_battlearena1` | 988 | 236 | 裸的那批 = `Card Info`/`Tactic Container`/`EffectAnchor`/`MinionOrWarlord Container` 各 8 ＋ `HandArea`/`LeftMinionArea`/`RightMinionArea`/`Board Center`/`Floor plane`/`Generator glows`/`Embers`/`Cube`/`Cannon*`/`Fence*`/`SandBag*` 一类 3D 挂点 |

**逐名实读（本件直接用来定判据的那几个）**：

| 节点名 | 原版组件 | 用在哪 |
|---|---|---|
| `War ParticleSystemUI`（8 个实例） | **`RectTransform`** | 断言「1 级是 RectTransform」 |
| `Warp Particle System`（8 个实例） | **`RectTransform`** | 断言「2 级是 RectTransform」 |
| **`Particle System nebula`（6 个实例）** | **裸 `Transform`** | ⭐ **这就是「必须保住」那一件** —— 判据原文说的「只有 1 处靠不是 RectTransform 成立」**实读坐实** |
| `Background`(1215) · `Viewport`(335) · `Content`(1050) · `Rewards Scroll View`(42) | **`RectTransform`** | `MenuDraw.Node` / 工厂产物必须是 RectTransform |
| `Deck info` · `Background Info` · `General container` · `Army Selector` · `Filters` · `Decks Scroll view` | **`RectTransform`** | `PracticeModePopup.New` 那一族 |
| `Navigation Panel` · `Buttons Container` · `Upper bar` · `GameModes` · `Resources Bar` · `Player Profile` · `ChatPreview` · `Avatar Item Small` · `TopBarButtons` · `InboxBtn` · `Main Menu Navigation Button - {Home,Collection,Shop,Rewards,Social}` | **`RectTransform`** | `MainMenuRuntime.New` 那一族 |
| `WaitText` · `Dark Shade` · `Generic Popup Background`（战斗场景） | **`RectTransform`** | `Battle/WaitBanner.New` 那一族 |

### 4. 新断言（三节，**都是「另开一节」**，⛔ 一条别人的断言都没动）

**① `Editor/RewardsScene.cs:2179-2243`（新 `Section("§A92 …")` 在 **`:2197`**，插在 §三·b3-a 与 §三·b3-b（`:2244`）之间）**

```csharp
Section("§A92 节点类型：三条空节点工厂都建 `RectTransform`（原版 16510/16768；例外只有粒子 3D 子件）");
{
    var a92r = new PxRect(0f, 0f, 10f, 10f);
    var a92n1 = MenuDraw.Node(win.transform, "A92NodeProbe", a92r);
    CheckTrue(a92n1 != null && a92n1.GetComponent<RectTransform>() != null, "`MenuDraw.Node` 建出来的是 **`RectTransform`**…");
    var a92n2 = RewardsWindow.Node(win.transform, "A92WindowNodeProbe", a92r);
    CheckTrue(a92n2 != null && a92n2.GetComponent<RectTransform>() != null, "`RewardsWindow.Node`（= `MenuWindowBase.Node`…）…");
    var a92n3 = RewardsWindow.New(win.transform, "A92NewProbe");
    CheckTrue(a92n3 != null && a92n3.GetComponent<RectTransform>() != null, "`RewardsWindow.New`（直调那条路）…");
    // 🔴 反向那一半：原版唯一那类「只有 `Transform`」的件（3D 粒子子件）**不许被顺手改齐**
    var a92n4 = RewardsWindow.NewPlainTransform(win.transform, "A92PlainProbe");
    CheckTrue(a92n4 != null && a92n4.GetComponent<RectTransform>() == null, "🔴 `NewPlainTransform` 建的**不是** `RectTransform`…");
    …（四条 DestroyImmediate，批处理下 `Destroy` 不生效）
    var a92hit = MenuDraw.Hit(win.transform, "A92HitProbe", new PxRect(400f, 400f, 500f, 450f), 3000, () => { }, null, null, null, null, null);
    CheckTrue(a92hit != null && a92hit.GetComponent<RectTransform>() != null, "`MenuDraw.Hit` 的命中区节点是 **`RectTransform`**…");
    var a92hq = a92hit.GetComponentInChildren<ImageQuad>();
    CheckNear(a92hq != null ? a92hq.WorldW * 108f : -1f, 100f, 0.5f,
              "…命中区那颗透明 quad 的宽仍是 **100px**（= 传进去的矩形 400→500；换类型不许动它）");
    // ---- 真树上那三级（§三·b3-a 断的是「第 3 级没有」，这里断「1/2 级有」）----
    var a92Host = FindPath(forge, "Background/War ParticleSystemUI");
    var a92Body = FindPath(forge, "Background/War ParticleSystemUI/Warp Particle System");
    CheckTrue(a92Host != null && a92Host.GetComponent<RectTransform>() != null, "原版第 1 级 …");
    CheckTrue(a92Body != null && a92Body.GetComponent<RectTransform>() != null, "原版第 2 级 …");
}
```
> 期望值全部来自**原版节点类型**（§二·3 那张表）或**入参**（100px = 我传进去的矩形宽），⛔ 没有一处读被测实现。⛔ 无浮点精确比（唯一那条 `CheckNear` 容差 0.5px）。

**② `Editor/MainMenuScene.cs:6049-6080`（新 `Section("§A92 …")` 在 **`:6061`**，插在最后两条 `MeasureText` 与 `Shoot("01_主菜单.png")`（`:6081`）之间）**
5 条：`Navigation Panel` / `Buttons Container` / `Upper bar` / `GameModes` / `Main Menu Navigation Button - Home` 各 `GetComponent<RectTransform>() != null`（名字**逐个点过名**，不是拿一个当代表）。

**③ `Editor/CollectionScene.cs:1856-1901`（新 `Section("§A92 …")` 在 **`:1868`**，插在 A132 那节（`:1854` 收尾）与 `Import Deck Popup` 那节（`:1903`）之间）**
自开一扇练习窗（照 A132 那节的写法），断 `Army Selector` / `…/Viewport` / `…/Filters` / `Deck info` / `Deck info/General container` / `ToggleHit` 六件是 `RectTransform`，**末尾 `Close()` 并把 `LastOpened` 清回 `null`**（不留状态给后段 —— 复核过 `LastOpened` 在 1807 之后**没有任何断言**再读它）。

**「两态可分辨」**：三节都成对 —— `Node/New` **有** vs `NewPlainTransform` **没有**；Forge 三级 1/2 **有** vs 3（§三·b3-a 那条既有断言）**没有**。弱断言（只断 `!= null` 之类）在这里**分不出两态**，所以一律断到 `GetComponent<RectTransform>()` 的有/无。

### 5. 判据原文 §4（同批要核的那件）：`rect_of(rt, parent_rect, scale)` 的 `scale` 是死参

**✅ 已经修过了，不用再修**（复核现状，⛔ 未改 `工具/**`）：
- `工具/menu_rect.py:167` `def rect_of(rt, parent_rect, scale):`，docstring 第 170 行逐字写着「🔴 **2026-10-05 修一处真缺陷：`scale` 原来是个【死参】—— 收了、函数体一次没用**（A60⑤⑨）」，函数体 `:208` `sx, sy = scale` 并真的用进算式（`:184-190` 那段推导）；
- `工具/menu_dump.py` 也把它当活参传：`:1486` / `:1499` / `:1519` `MR.local_size(rt, parent_rect, scale)`。

---

## 三、没查清的部分（⛔ 不猜）

1. **`new GameObject(name, typeof(RectTransform))` 建出来的 `RectTransform` 默认值到底是多少**（`sizeDelta` / `anchorMin/Max` / `pivot`）—— **本轮不跑 Unity（铁律 12），没实测**，所以**一个字都没往注释里写**。它只影响「下一批要不要写 `sizeDelta`」那一问的写法，不影响本批的正确性（判据原文点名的三件行为里没有它）。
2. **`WaitBanner` 的根节点**：判据原文的 250–280 里**没有点名** `Battle/WaitBanner.cs`，是白名单里有它 + `New(` 那 31 处的清单里有它 ⇒ 我按「原版 `WaitText` 实读是 `RectTransform`」改了。⚠️ **但「我们这条提示的触发时机与文案是我们挑的」是既有事实**（`WaitBanner.cs:12-16`）⇒ 这一件到底算不算 A92 的射程，**请调度台裁**（回退成本 = 一行）。
3. **`Battle/` 侧那几十处 `new GameObject(...)`**（窗口根 / HUD 根 / 各种 holder）**原版对不对**？只逐个核了 `WaitText` / `Dark Shade` / `Generic Popup Background` 三件（都 RectTransform）；其余**没查**（不在白名单，见 §四·2 的清单）。

---

## 四、改动清单

### 1. 逐文件（**我的净行数**用「改前/改后二进制行数」单列；`numstat` 是**相对 HEAD**、含本轮**其他写手**未提交的改动，两者不同是正常的）

| 文件 | 我改了什么 | 净行数（改前→改后） | `git diff --numstat`（含别人） | 行尾 |
|---|---|---|---|---|
| `Shell/MenuDraw.cs` | `Node` ①、`Hit` 命中区节点 ⑤（各 1 行 + 注释） | 1570 → **1589**（+19） | `130 39` | **LF**（CRLF=0） |
| `Shell/MenuWindowBase.cs` | `New` ② ＋ 新增 `NewPlainTransform` | 435 → **456**（+21） | `54 123` | **LF** |
| `Shell/MainMenuRuntime.cs` | 私有 `New` ③ | 1038 → **1045**（+7） | `21 3` | **LF** |
| `Shell/PracticeModePopup.cs` | 私有 `New` ④ ＋ `HitOn` ⑥ | 1572 → **1582**（+10） | `301 39` | **LF** |
| `Shell/ForgeTab.cs` | ⚠️ **只动了保住 `:396-397` 相关那一处**：nebula 改调 `NewPlainTransform` | 827 → **833**（+6） | `22 9` | **LF** |
| `Battle/WaitBanner.cs` | 私有 `New` ⑦ | 247 → **253**（+6） | `7 1` | **LF** |
| `Editor/RewardsScene.cs` | 新 §A92 断言节 ① | 4288 → **4353**（+65） | `65 0`（= 全是我的） | **LF** |
| `Editor/MainMenuScene.cs` | 新 §A92 断言节 ② | 6692 → **6725**（+33） | `534 13` | **LF** |
| `Editor/CollectionScene.cs` | 新 §A92 断言节 ③ | 3702 → **3748**（+46） | `209 2` | **LF** |

**行尾核对**：9 个文件**改前改后都是纯 LF**（`b.count(b'\r\n') == 0`，二进制读法数的）—— ✅ 一个都没翻，也没混行尾。**全是净增行，我这一批 0 删行**（唯一的「−1/+1」是 `WaitBanner.cs` 那行 `new GameObject(name)` → 带 `typeof(RectTransform)`，以及 `ForgeTab.cs` 那行换方法名）。

**⚠️ 改完立刻跑的两遍类型检查**（独立 `TMPDIR`，两次都干净）：
```
TMPDIR=/tmp/wf_w8o  bash d:/4/Unity/工具/typecheck.sh   →  运行时 0 / 编辑器 0
TMPDIR=/tmp/wf_w8o3 bash d:/4/Unity/工具/typecheck.sh   →  运行时 0 / 编辑器 0
```

### 2. ⛔ 本轮**没改**、但**同为这一类**的地方（**要做**，判据在括号里 → 只是**先后**，不是「不做」）

**(a) 在**我的**白名单里、我**故意没动**的：**
- **窗口根工厂**：`Shell/RewardsWindow.cs:212` `new GameObject("Rewards Base Submenu Variant")` · `Shell/PracticeModePopup.cs:614` `new GameObject("Practice Mode Menu")` · `Shell/ShopWindow.cs:94` `new GameObject("Shop Menu Variant")`（判据：原版那三扇窗的根 prefab 都是 `RectTransform`）。
  **先不做它的理由 = 同一时刻只有一个写手**：窗口根是 ㉑ 这批正在动的对象（`Shell/WindowsManager.cs` · `Shell/PointerLayer.cs` · `Editor/ShellScene.cs` **三处都对我黑名单**）⇒ **同一个对象**别在两条线上并行改，**排到 ㉑ 那批收口之后**。
- **格子 / 抽屉节点**：`Shell/PracticeModePopup.cs:787` `new GameObject("Army_" + i)` · `:853` `"DeckRow_" + i` · `:1074` `"Deck List Drawer"` · `:1107` `"CardRow_" + i`（判据：原版对应件——格容器 / `Deck List Drawer`——都是 `RectTransform`；⚠️ 名字 `Army_<i>` 那类**像是我们自己的命名**，得先跟原版对一次名字）。
- **`Battle/WaitBanner.cs:228` `wait_fillRoot`**（判据：它对应的原版件是 `Generic Popup Background` 下的 `Mask` + `Background fill`，两件**实读都是 `RectTransform`**；⚠️ 节点名是我们自己起的 ⇒ 名字要不要照原版是**另一件**）。

**(b) 不在我白名单里（⛔ 我没动，交给调度台切批）—— 同类「空节点容器/窗口根」共 ~30 处：**
`Battle/BattleBackdrop.cs:54` · `BattleDoors.cs:128,138` · `BattleLogPanel.cs:377,397` · `CardDisplayWindow.cs:141` · `ChatPopupPanel.cs:125` · `ChoosePanel.cs:74` · `EndPanel.cs:103,135,161` · `MulliganPanel.cs:134` · `MultiCardDisplay.cs:93` · `ReplayBar.cs:112,124` · `SettingsPanel.cs:129` · `TargetReticle.cs:345` · `UnitChatPanel.cs:165` · `WfSlider.cs:273` · `BattleDriver.cs:1430,1435,6873,7477` · `Remot…`（`RemnantSfx.cs:164`）· `Board/BoardLayout.cs:129` · `Core/BlobShadow.cs:92`；外加 **`ImageQuad.cs:118,338,413` / `Label.cs:51`**（这两个是**叶子渲染件**，原版对应件也是 uGUI 节点 ⇒ 同族，但**语义不同**：它们自己带 `WorldW/WorldH` 可量，**不在 A92「空节点」那句射程里**，要不要一起收由调度台定）。

### 3. 「怎么改坏就红」（回改任一处 ⇒ 哪条断言先红）

| 改坏的动作 | 会红的断言 |
|---|---|
| `MenuDraw.Node` 改回 `new GameObject(name)` | `RewardsScene` §A92 第 1 条 ＋ 真树那两条（`War ParticleSystemUI` / `Warp Particle System`） |
| `MenuWindowBase.New` 改回裸 `Transform` | `RewardsScene` §A92 第 2、3 条（`RewardsWindow.Node` / `.New`） |
| `MainMenuRuntime.New` 改回 | `MainMenuScene` §A92 那 5 条 |
| `PracticeModePopup.New` 改回 | `CollectionScene` §A92 前 5 条 |
| **`NewPlainTransform` 改成带 `RectTransform`**（或 `ForgeTab` 那一行改回 `New`） | `RewardsScene` §A92 第 4 条 ＋ **§三·b3-a 那条「`Particle System nebula` 没有 `RectTransform`」**（判据原文点名必须保住的那条） |
| `MenuDraw.Hit` / `PracticeModePopup.HitOn` 改回 | `RewardsScene` §A92 的 Hit 那两条 ／ `CollectionScene` §A92 的 `ToggleHit` 那条 |
| **顺手把节点挪了位**（哪怕是 0.1px） | §三·b3-a 那些 `CheckNearPx(psHost, 1125.35, 575.5)` 等（期望值是**原版字面量**）—— 这是「换类型没换位置」的回归闸 |

---

## 五、顺手发现（⛔ 一处都没自己改，交调度台分流）

1. 🔴 **`MainMenuRuntime.cs:746` 建的节点名字是 `Icon/Player Level`（一个带斜杠的**字面节点名**）**，而原版是**两级**：`Player Profile` → `Player Level`（`bundle_scenes_scenes_mainmenuwarpforge` 实读：`Player Level` 的父是 **`Player Profile`**，不是 `Icon`；`Icon` 在别处另有 29 个同名件）。我们把它**压成了一级**，副作用是 `FindChild` 那类按名字找的路（以及任何把 `A/B/C` 当路径的地方）都会踩到本仓已经踩过一次的坑（`RewardsScene.cs:132-134` 那条注释记的就是它）。
2. 🔴 **`Deck List Drawer` 在我们的 `PracticeModePopup` 里是 `_cardHolder`（`PracticeModePopup.cs:1074`），名字对、类型待收**（见 §四·2a）。
3. `Editor/RewardsScene.cs:1810` / `Shell/ForgeTab.cs:396-397` 这两处**行号已经漂了**（判据原文按行号指的路**指不到东西**）：实际内容在 `RewardsScene.cs:2133` 与 `ForgeTab.cs:401-408`。建议正本里的指针**改成「按内容/名字」**，否则下一次还会有人按行号去找空。
4. `MenuDraw.cs:1076`（`Hit` 的收口头注释）里说「`MenuDraw.Hit` 这段原来只有 `MainMenuSubmenuWindow.AddHit` 一份」—— 但 `MenuWindowBase.cs:438` 的 `New(b, "Hit")`（Tab 键那一颗）**并没有转调** `MenuDraw.Hit`，两者是**两条各写一遍**的路（`MakeHitQuad` 那一层收口了，建节点那一层没收）。**要不要连节点创建也收成一份**，由调度台定（本批只把类型对齐了）。
5. `PointerLayer.cs` 里那条「`CollectHits` 取按钮下第一个 `ImageQuad`（`PointerLayer.cs:492`）」**行号也漂了**（㉑ 在改这个文件）—— 我复核过实现确实是 `AllButtons()` + `HitBoxPx`（quad 中心 + `WorldW/H`），**结论不变，只是行号要重指**。
