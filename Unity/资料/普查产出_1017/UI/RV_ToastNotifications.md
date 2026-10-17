# RV · 审查报告 —— `Shell/ToastNotifications.cs`（写手 D，新建 1147 行）

- 审查者：**审查代理 RV**（只读；本文件是唯一写入）
- 日期：2026-10-17
- 审查对象：`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/ToastNotifications.cs`（未提交）
  ＋ 主对话在 `Shell/MainMenuRuntime.cs:415` 插的 `ToastNotifications.Build(_root);`
- 判据一律**现读**：`bundle_scenes_scenes_mainmenuwarpforge/{RectTransform,GameObject,MonoBehaviour,Canvas}`、
  `d:/2/tools/decomp_full/ToastNotificationController__*.c` · `ToastNotification__*.c` · `ToastNotificationMission__Initialize.c` ·
  **本工程自带的 uGUI 源码** `Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/*.cs` ·
  `工具/menu_rect.py` / `工具/menu_dump.py` / `工具/read_literal.py` ·
  `资料/主菜单_原版规格.md:64-74,237-238` · `资料/阶段二_Shell_原版规格.md:180` · `资料/普查产出_第八会话/E7_弹窗星号与toast.md`
- 红线遵守：没改任何代码/任务表/正本，没动 git，没跑 Unity，没起 player。

---

## 结论（一段话）

**不能直接用**：有一条**符号写反的高危几何错误**（`HolderRight = ScreenW − HolderAposX` ⇒ 整族右移 42.85 px、右缘出屏 21.42 px，而代码自己的注释写着正确的 1898.577），
两条**与原版行为静默不一致**的算法错误（`Toast`/`Get XP` 的高度公式与 uGUI `m_ChildControlHeight=0` 的语义相矛盾；`Mathf.Lerp` 自带的 `Clamp01` 把 `OutBack/InBack` 的回弹整个吃掉），
外加一条**父节点静默退化**（两个宿主里都找不到 `Safe area Only Horizontal`，而且不出声）。
时序常量（1s / 3s / 10 / −h）、`SetEase(0x1b/0x1a)` 的枚举值、`freeSpot = min(index, freeSpot)`、handler 分派语义、`Destroy`/`DestroyImmediate` 分流——这几项**逐句对得上**，可以留。

---

## 逐条发现（高 → 中 → 低）

### 🔴 高

#### H1 · `HolderRight` 符号写反 —— 整个 toast 家族右移 42.85 px、右缘画到屏外

- **结论**：`HolderRight` 算成 **1941.4228**，原版是 **1898.5771**。Holder 与 6 个模板全部从它派生 ⇒ 整体右移 **42.8457 px**。
- **证据**：
  - `ToastNotifications.cs:195`
    ```csharp
    static readonly float HolderRight = ScreenW - HolderAposX;   // 1898.5771484375
    ```
    `HolderAposX = -21.4228515625`（`:194`）⇒ `1920 − (−21.4228515625) = 1941.4228515625`，**不是注释里那个数**。
  - 正确的算式是 `1920 + (−21.4228515625) = 1898.5771484375` —— 与注释**逐位相同** ⇒ 代码与自己的注释互相打脸。
  - `RectTransform_1100.json`：`m_AnchorMin = m_AnchorMax = (1,0)` · `m_Pivot = (1,0)` ·
    **`m_AnchoredPosition.x = -21.4228515625`** · `m_SizeDelta = (634.4879760742188, 752.614013671875)`；
    父 = `RT_1096` `a(0,0)-(1,1) sd(0,0)`（满屏 1920×1080）⇒ pivot 点 = `1920 + (−21.4228) = 1898.5771`。
  - 两种独立实现互相印证：
    · `Core/UguiRect.cs:41-58` 的 `Child()` 公式（`refX = parent.x1 + refNx*pw`，`px = refX + pos.x`）；
    · `python 工具/menu_rect.py bundle_scenes_scenes_mainmenuwarpforge --rt 1096 --no-ancestor-scale`
      ⇒ `Holder  x1=-655.91 … x2=-21.42`（= 父右缘 0 − 21.42）⇒ 贴 1920 的父右缘即 **1898.58**。
- **影响面**：`holderRect`（`:305`）、`PrefabRect()`（`:1019-1022`）⇒ Holder + `Toast` / `Get XP` / `Level Up` / `Mission` 全部右移 42.85 px；
  右缘落在 1941.42（屏宽 1920）⇒ 最右 **21.4 px 画到屏外**。`Get XP` 的 435.6 宽文本右端由 1898.9 变成 1941.8（出屏 21.8 px）。
- **该照哪个判据**：`RectTransform_1100.json` 的 `m_AnchoredPosition.x = -21.4228515625` + uGUI 锚点公式。
- **建议**：`ScreenW + HolderAposX`；顺手把注释里的 `1898.5771484375` 改成断言式的自证（或干脆写成 `ScreenW + HolderAposX` 并只留数值注释）。

#### H2 · `Toast` / `Toast Collection Get XP` 的高度公式与 uGUI 语义矛盾 —— 根高应恒为 108.8638 / 86.7019

- **结论**：实现按「VLG 会把 TMP 的首选高算进去」来求根高（`padT + iconH + textH + padB`），
  但原版这两个 prefab 的 VLG **`m_ChildControlHeight = 0`**，uGUI 在这种档位下取的是**子件自己的 `sizeDelta.y`**，而两个 Text 的 `sizeDelta.y` 都是 **0**
  ⇒ 根高**与文案无关**，恒为 `12 + 89.8638 + 0 + 7 = 108.8638`（Toast）/ `0 + 86.7019 + 0 + 0 = 86.7019`（Get XP）。
- **证据**：
  - `MonoBehaviour_204.json`（=`Toast` 根 GO 222 的 VerticalLayoutGroup）：`m_Padding L83 R0 T12 B7` · `m_Spacing 0` ·
    `m_ChildAlignment 0` · `m_ChildControlWidth 0` · **`m_ChildControlHeight 0`** · `m_ChildForceExpandHeight 0`。
  - `MonoBehaviour_205.json`（=`Get XP` 根 GO 223）：`m_Padding L83 T0 B0` · **`m_ChildControlHeight 0`**。
  - `RectTransform_356.json`（Toast 的 `Text`）：`m_SizeDelta = (362.0566101074219, **0.0**)`。
  - `RectTransform_357.json`（Get XP 的 `Text`）：`m_SizeDelta = (435.6188049316406, **0.0**)`。
  - `MonoBehaviour_525.json`（Toast 的 ContentSizeFitter）：`m_VerticalFit = 2 (PreferredSize)`；`MonoBehaviour_527.json`（Get XP）同。
  - **本工程自带的 uGUI 源码** `.../Layout/HorizontalOrVerticalLayoutGroup.cs:221-239`：
    ```csharp
    private void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand, …)
    {
        if (!controlSize) { min = child.sizeDelta[axis]; preferred = min; flexible = 0; }
        …
    }
    ```
    以及 `:182-216` 的 `pos += childSize * scaleFactor + spacing;` 用的是同一个 `childSize`。
  - `LayoutGroupContentFixer__FixLayout.c` 只对 `layouts` 里每个组做 `ForceRebuildLayoutImmediate`，**不改子件尺寸** ⇒ 没有旁路。
- **影响面**：`:510-512`、`:545-546`。`sizeDelta.y` 被写成量出来的文案高 ⇒ ① 背景比原版高一截（占位文案两行、fs40，量出来大概率 150+）；
  ② `ShowNotificationInternal` 的堆叠步长 `index*h + 10`（`:847`）也一起变 ⇒ 多条同时在场时槽位间距与原版不同。
- **该照哪个判据**：uGUI `HorizontalOrVerticalLayoutGroup.GetChildSizes`（本工程 `Library/PackageCache` 里那一份源码）+ `MonoBehaviour_204/205` + `RT_356/357`。
- **建议**：改成 `ToastH = 108.8638f` / `GetXPH = 86.7019f` 两个常量，并把「为什么 textH 不计入」写进注释（现在注释的理由是反的）。
- ⚠️ **本条是静态推导，没跑原版实况读 `rect.height`**（见「没查到的」第 1 条）。但推导链是确定性的：源码 + 字段都在本地。

#### H3 · `Mathf.Lerp` 自带的 `Clamp01` 把 `OutBack` / `InBack` 的回弹整个吃掉

- **结论**：入场/退场虽然不是线性，但**回弹（overshoot / undershoot）完全没有**——`OutBack` 超标的那一段被夹成 1、`InBack` 下沉的那一段被夹成 0。
- **证据**：
  - `ToastNotifications.cs:956` `Pose(l, Mathf.Lerp(-h, l.TargetPx, EaseOutBack(l.T / MoveSeconds)));`
  - `ToastNotifications.cs:965` `Pose(l, Mathf.Lerp(l.TargetPx, -h, EaseInBack(k)));`
  - Unity 的 `Mathf.Lerp(a,b,t) => a + (b-a) * Mathf.Clamp01(t)`；
    `EaseOutBack`（`:1010`）在 `t=0.75` 时 ≈ **1.0641**（>1），`EaseInBack`（`:1011`）在 `t=0.25` 时 ≈ **−0.0641**（<0）⇒ 两处都被夹平。
  - 原版要的就是这条曲线：`__ShowNotificationInternal.c` 的两个 `SetEase(0x1b/0x1a)`，而 `0x1b = 27 = OutBack`、`0x1a = 26 = InBack`
    （DOTween `Ease` 枚举声明序：`… InElastic 23 / OutElastic 24 / InOutElastic 25 / **InBack 26** / **OutBack 27** / InBounce 28 …`）——
    这一条实现**写对了**，是插值那一步把效果抹掉了。
- **该照哪个判据**：DOTween `Ease.OutBack / InBack` 的曲线定义 + Unity `Mathf.Lerp` 的 `Clamp01`。
  （同类坑在本仓已经记过：记忆文件 `warpforge-cardface-layout.md`「`Mathf.InverseLerp/Lerp` 自带 `Clamp01`」。）
- **建议**：手写 `-h + (l.TargetPx + h) * ease` / `l.TargetPx + (-h - l.TargetPx) * ease`，⛔ 不要经 `Mathf.Lerp`。
- **严重度**：高。终值对、曲线错，而且**没有任何断言会红**（位置终值一样）。

---

### 🟠 中

#### M1 · 父节点静默退化：两个宿主里都找不到 `Safe area Only Horizontal`，而且不出声

- **结论**：`MainMenuRuntime.cs:413` 与 `ToastNotifications.cs:166/256-257` 都声称「建在 `Safe area Only Horizontal` 下」，**实际两支都落不到**；而这条退化**没有日志**。
- **证据**：
  - `Editor/MainMenuScene.cs:584-603`：`Build()` 建的是 `new GameObject("MainMenu")` + `AddComponent<MainMenuRuntime>()`，`_root = transform`（`Shell/MainMenuRuntime.cs:359`）
    ⇒ `_root` 下面**没有** `Safe area Only Horizontal`（那颗是 `Shell/ShellRuntime.cs:256-257` 在**壳根**下建的，属于另一个场景/另一棵树）。
  - `ToastNotifications.cs:275-283`：`host.Find(...)` 返 null ⇒ `where = host`，**只有 `where == null` 才 warning** ⇒ 找不到安全区这一支**一个字都不出**。
  - 同一张账单在 `MessageToast` 那边已经写死：`Editor/ShellScene.cs` 的 A1139 那段注释明写
    「🔴 **反例**：`Editor/MainMenuScene.cs` 的 `EnsureHost(menu.transform)` 根 = **菜单根**（它下面**没有**那个节点）⇒ 会退回把横幅挂在**菜单根**上」。
- **该照哪个判据**：`RT_1096.m_Father = 1540`（= `Safe area Only Horizontal`）+ 仓内既有 `A1139①` 那条断言的口径。
- **建议**：要么在 `MainMenuRuntime.Build` 里先建/找那颗安全区再传进来，要么**明确写成「我们挂在菜单根」并出声**（⛔ 别只在注释里写「照原版挂」）。

#### M2 · 菜单重建之后 toast 控制器会「消失一帧、随后在别处重建」

- **结论**：`Build()` 的幂等判据只判 `Instance != null`，而重建主菜单时旧对象是**延迟销毁**的 ⇒ 第二次 `Build` 拿到一颗**已被标记销毁**的实例并直接返回，**不建新的**；帧末 `OnDestroy` 把 `Instance` 置 null ⇒ 场景里没有 toast 控制器，直到下一次 `Ensure()` 在**壳根/场景根**重造一颗。
- **证据**：
  - `Shell/MainMenuRuntime.cs:360` `for (…) DestroySafe(_root.GetChild(i).gameObject);`
  - `Shell/MainMenuRuntime.cs:576-582` `DestroySafe`：`#if UNITY_EDITOR if (!Application.isPlaying) { DestroyImmediate(go); return; } #endif Destroy(go);`
    ⇒ **Play 下是 `Destroy`（延迟到帧末）**。
  - `ToastNotifications.cs:268` `if (Instance != null) return Instance;`（Unity 的 `!=` 重载**不会**把「已标记销毁但还没销毁」判成 null）。
  - `ToastNotifications.cs:993` `void OnDestroy() { if (Instance == this) Instance = null; }`。
- **影响**：每次「离开卡组编辑 / 回主菜单」之后，下一次 `Ensure()` 会在 `ShellRuntime.Instance.transform` 或
  `WindowsManager.Instance.transform.parent` 上重造一颗 —— **不在 `_root` 下**，也不在 `Templates` 的原始位置上。
- **建议**：幂等判据加一条 `|| Instance.gameObject == null`；或建之前先 `DisposeForTest()`。

#### M3 · `count`（任务奖励数量）水平对齐反了、autosize 也没接

- **结论**：原版这一颗是 **Center + 自适应 20~40（base 36）**；实现 **强制左对齐 + 无自适应**。
- **证据**：
  - 实现 `ToastNotifications.cs:387-390`：
    ```csharp
    var count = Tmp(variant, countRect, "100", Color.white, "count", 40f, QText,
                    wrap: false, autoMin: 0f, autoMax: 0f, autoBase: 0f);
    MenuDraw.AlignLeft(count, countRect);
    ```
  - 判据 `MonoBehaviour_555.json`（`m_GameObject = 320` = `RT_375` 的 GO，`m_text = "100"`）：
    `m_fontSize 40` · `m_enableAutoSizing 1` · `m_fontSizeMin 20` · `m_fontSizeMax 40` · `m_fontSizeBase 36` ·
    **`m_HorizontalAlignment = 2 (Center)`** · `m_VerticalAlignment = 4096 (Midline)` · `m_TextWrappingMode 0`。
  - `Label` 出厂是**居中**（`Shell/MenuDraw.cs:1946` `AlignLeft` 的注释：「`Label` 默认把文字块**居中**放在锚点上」）⇒ 这里**不调** `AlignLeft` 才是对的。
- **建议**：去掉 `AlignLeft`；改 `autoMin: 20, autoMax: 40, autoBase: 36`。

#### M4 · 三个 tooltip 文本的 autosize 没接（其中两条原版是开的）

- **结论**：`Collector Level Tooltip` 的三段字里，`Description` 与 `Xp to next level` 原版开自适应，实现都没开。
- **证据**（`TooltipText` 一路传 `autoMin: 0`，`ToastNotifications.cs:657-664`）：
  | 节点 | MB | 原版 | 实现 |
  |---|---|---|---|
  | `Title` | `MonoBehaviour_560.json`（GO 322） | fs 41.76 · **auto 0** · v=4096 Midline | auto 0 ✓ |
  | `Description` | `MonoBehaviour_559.json`（GO 323） | fs 36 · **auto 1** · min 18 / max 36 / base **41.76** · v=**256 Top** | `autoMin 0` ⇒ **未开** ✗ |
  | `Xp to next level` | `MonoBehaviour_502.json`（GO 230） | fs 32 · **auto 1** · min 18 / max 32 / base 32 · v=4096 Midline | `autoMin 0` ⇒ **未开** ✗ |
- **影响**：长文案会溢出 476×80.7 / 467.9×37.9 的框（`RT_378` / `RT_365`）。
- **建议**：`Description` → `SetAutoFitBox(w, h, 18, 36, 41.76)`；`Xp` → `(…, 18, 32, 32)`。

#### M5 · 渲染队列 5000 起压过全壳（含加载黑场 4090 / 压暗 4070）

- **结论**：5000–5004 是**当前全工程最高档**，会把 toast 画在加载黑场（`QPanel = 4090`、`BlockingOverlay 4070/4071`）**之上**。
- **证据**：
  - 实现 `ToastNotifications.cs:182` `QShade 5000 / QBg 5001 / QArt 5002 / QText 5003 / QHit 5004`。
  - 现网实测最高：`QPanel = 4090`（`Shell/…` 那一族）· `BlockingOverlay._dim = 4070` / `_spinner = 4071`（`Shell/BlockingOverlay.cs:50,57`）·
    `QTip/QTipText = 3606/3607`（`Core/Tooltip.cs:114`）。`grep 'const int … = [0-9]{4,}'` 全仓**没有第二处 ≥5000**。
  - 原版这一层的判据只有「Canvas `m_SortingOrder = 900`」（`Canvas_1074.json`，`m_OverrideSorting = true`）——
    **它在加载黑场之上还是之下，本地没有判据**（见「没查到的」第 3 条）。
- **判断**：不是「与现有档冲突」（数值上不撞），而是**把一个原版没有证据的位置当成了最高层**；
  实际后果是「加载时弹一条 toast 会画在黑屏上」。
- **建议**：至少让 toast 落在加载黑场（4090）**之下**，并在注释里写明「原版靠 `SortingOrder 900`，我们的对应档位**查不到**」。

---

### 🟡 低

#### L1 · `Player Level Text` 的换行模式与原文不符

- 实现 `ToastNotifications.cs:494-495` `wrap: false`（autoMin 18 > 0 ⇒ 走 `SetWrapping(false)`）。
- 判据 `MonoBehaviour_505.json`（GO 231）：`m_TextWrappingMode = **1**`（Normal）。
- 后果：单/双位数看不出差别，但字段是反的。建议 `wrap: true`。

#### L2 · `Resource Counter Item` 被实现了**第二遍**

- `Shell/TopBar.cs:496,542` 已经按 `GameObject/Resource Counter Item.json` 的组件表（含 `HorizontalLayoutGroup 206`）建过一格；
  `ToastNotifications.BuildResourceCounter`（`:670-708`）又建了一份。
- 撞 CLAUDE.md §三「两处写同一条规则 = 迟早不一致」。而且这一份**从不被 toast 使用**（`_templateByType` 只装 4 个）⇒ 纯死重量，还会被烘进 `MainMenu.unity`。

#### L3 · `Collector Level Tooltip` 同样不会被使用

- `BuildCollectorTooltip`（`:578-655`）建了整棵，但没有入口能把它弹出来；而壳里已有 `Core/Tooltip.cs` 那一套 tooltip 体系（`:98-103` 引用的是同一段兄弟序）。
- 同样会**烘进场景**（`MainMenuScene.BuildAndSaveScene` → `SaveScene`）。

#### L4 · `MeasureTextPx` 第一条兜底分支「说了出声、其实没出声」

- `:1080-1087` 注释写「极端兜底（点阵后端）—— 出声」，代码 `if (lb == null) return fontPx * 1.25f;` **一行日志都没有**。
- 第二条兜底（`h` 不合理）**有** `Debug.LogWarning`（`:1093`）⇒ 同一函数两种待遇。

#### L5 · `FindQuad` / `FindLabel` 在「路径根本不存在」时静默返回 null

- `:1099-1115`：只有「路径在、组件不在」才 warning；`root.Find(path) == null` 直接 `return null`。
- 具体风险：`:381` 的 `MenuDraw.Rect(drawer, CardArt.MenuUi("40k_topmarquee_currency_gold"), …, "Icon", …)`
  在**缺图**时会返回 `null`（`MenuDraw.Rect` 首句 `if (tex == null) return null;`）⇒ `Icon` 节点根本不存在 ⇒
  `Fill`（`:868`）拿不到、**不出声**，而且**以后也永远补不回来**（模板已经建死）。
- 建议：`Icon` 至少建一颗占位节点，或在 `BuildMission` 里对缺图出声。

#### L6 · `Node` 写进 RectTransform 的 `sizeDelta` / `anchoredPosition` 是**换算成世界单位**的，不是原版值

- `:1036-1037`
  ```csharp
  rt.sizeDelta = new Vector2(LayoutSpace.Px(sd.x), LayoutSpace.Px(sd.y));
  rt.anchoredPosition = new Vector2(LayoutSpace.Px(apos.x), LayoutSpace.Px(apos.y));
  ```
- 方法的 doc（`:1024-1025`）说「**元数据照原版**（anchor / pivot / sizeDelta / anchoredPosition 都写进去）」——
  实际写的是**原版值 ÷ 108**，与 prefab JSON 对不上；而且 `anchoredPosition` 下一行（`:1042`）就被 `localPosition` 覆盖 ⇒ **死数据**。
- 风险：将来任何按「`sizeDelta == 原版 px`」写的断言都会全错；`ShowInternal`（`:842`）又是靠 `PxOf()` 反算回来的，属于自证闭环。

#### L7 · 若干 `sizeDelta` 被截断（亚像素，可忽略但应标注）

| 节点 | 原版（JSON） | 实现 |
|---|---|---|
| `Rewards`（RT_346） | `139.7100067138672` | `139.71f`（`:361,364`） |
| `Mission Milestones Progress Bar`（RT_348） | `216.68890380859375` / `51.830101013183594` | `216.689f` / `51.8301f` |
| `Fill Area`（RT_366） | `(-4.5775999751640484e-05, 0)` | `Vector2.zero`（`:643`） |

#### L8 · 注释里的出处写错（把 Sprite 的 PathID 写成了 MB）

- `:517` 「根 Image：`UI_Deck_Information_submenu_Back_opaque` · Sliced · border 18 · ppuMul 1（**`MB 3788653729785366146`** 那一颗）」
  —— `3788653729785366146` 是 **Sprite 的 PathID**（`MonoBehaviour_402.json` 的 `m_Sprite.m_PathID`，`m_FileID: 15`），
  不是 MonoBehaviour。正确出处：`MonoBehaviour_402.json`（`m_GameObject = 222` = `Toast` 根）。
- 顺带核对（**这条对**）：`UI_Deck_Information_submenu_Back_opaque` = `m_Rect 69×63` · `m_Border 18` ✓；
  `WF_UI_Generic_FancyNotification_Background` = `m_Rect 352×116` · `m_Border 20` · `m_PixelsPerUnitMultiplier 1.3` ✓。

#### L9 · 三个 toast 的 hover / press 色偏没接（Mission 那一颗反而**不该有**）

- 判据：`MonoBehaviour_504/508/513.json`（LevelUp / Toast / GetXP 的 `Button`）`m_Transition = **1 (ColorTint)**`、
  `m_TargetGraphic` = 各自的**根 Image**（401 / 402 / 403）；`m_Colors` Normal 白 · Highlighted 0.9608 · Pressed 0.7843 · `m_FadeDuration 0.1`。
- 实现 `MenuDraw.Hit(root, "Hit", rootRect, QHit, null)` 没传 `target` / `art` ⇒ `WindowButton` 的悬停色偏打在那颗
  **全透明**命中 quad 上（`MenuDraw.MakeHitQuad` 里 `SetTint(new Color(0,0,0,0))`）⇒ 视觉上什么也不变。
- 反例：`MonoBehaviour_520.json`（Mission 的 `EverguildButton`）`m_Transition = **0 (None)**`、`m_TargetGraphic = 0`
  ⇒ **Mission 那颗本来就没有悬停变化**，实现「不传」在这一档是**对的**。⛔ 别把四处一起补。

#### L10 · Mission 那一颗被多接了一条可点击通道

- `MonoBehaviour_488.json`（`ToastNotificationMission`）`button = 0`；`ToastNotificationMission__Initialize.c` **完全不碰 OnClick**。
- 实现照建 `MenuDraw.Hit(root, "Hit", …)`（`:441-442`）并在 `Fill` 里把 `ctx.OnClick` 接上去（`:917-918`）。
- 不是错误（多一条通道），但**应标成「我们加的」**，⛔ 别写成「原版行为」。

#### L11 · `DisposeForTest` 不是「从零」

- `:996-1002` 只把 `Instance` 置 null 并销毁节点；静态 `_handlers`（`:217`）与 `_warnedFancyBg`（`:218`）**留着**。
- 自检若用 `DisposeForTest` 做「干净起点」，`RegisterNotificationHandler` 注册过的 id 会跨节存活。

#### L12 · `ScreenW / ScreenH` 写死 1920×1080

- `:190`。本工程设计空间恒 1920×1080 ⇒ 今天等价；但这是第二份口径（本仓已有 `LayoutSpace.DesignPxW/H`）。建议改用常量。

---

## 没查到的（逐条写搜过什么）

1. **原版运行时的 `rect.height`（H2 的实况读数）** —— 没跑原版、没起 player（红线）。
   H2 是**静态**推导：本工程自带的 uGUI 源码（`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs:221-239`）
   ＋ `MonoBehaviour_204/205` ＋ `RT_356/357`。**建议在真机/运行时把 `rect.height` 打一次确认**。
2. **`Toast` 根 `Background` / `Get XP` 根 `Background` 的旋转** —— 我确认了 `m_Sprite` / `m_Type` / `m_Color` / `m_PixelsPerUnitMultiplier`，
   但 `m_Rect` / `m_Border` 是从 `素材/Warpforge原版/UI图集/图集/0_mainmenu/Sprite/*.json` 取的，没有在 `assets_full` 里复核（该包**没有导出 `Image` 类**：
   `MonoBehaviour/` 里 1134 个文件里没有 `/Image` 目录，`Image`/`ContentSizeFitter`/`VerticalLayoutGroup` 的空目录都在）。
3. **原版 `LoadingMenu` / `BlockingOverlay` 的 Canvas `SortingOrder`（M5 的判据）** —— 只查到 `Toast Notification Controller` 自己的 `Canvas_1074.json`（`900` + `m_OverrideSorting`）。
   `资料/主菜单_原版规格.md` §四「未查到的」也没有这一项。
4. **`40k_popup`（Collector Level Tooltip 底图）与 `40k_generial_bar_empty` / `40k_generial_bar_fill` 的 `m_Rect` / `m_Border`** ——
   没在 `assets_full` 里找到对应 Sprite（同上，该包没导出 Image/Sprite 索引）；`L11` 那三组 `texW/texH`（359×336 / 12×12）**未核**。
5. **`Handle Slide Area` 出厂 inactive** —— 间接确认：`menu_dump.py --rt 341` 的尾巴报「1 处『自己 `m_IsActive=1`、但祖先里有 inactive』：**Handle**」
   ⇒ 它的父 `Handle Slide Area` 是 inactive。**没有直接读 GO 317 的 `m_IsActive`**（该 pid 在本包有 GO 撞车）。
6. **`TooltipText` 三段的 `m_VerticalAlignment` 与实现的 `Label.VAlign` 映射** —— 已按原版字段值（512/4096/8192/256）对上枚举名，
   但 `Label.VAlign` 的**墨心位移**是否忠实于原版 TMP 的档位，我按 `Battle/Label.cs:1589-1592` 的声明与既有普查（`WA712`）认定，**没有另开一轮核**。
7. **`menu_dump.py` / `menu_rect.py` 在本包上的可靠性** —— 两个工具都对 `bundle_scenes_scenes_mainmenuwarpforge` 报
   「**49 个 GO pid 撞车**」（例：`pid 222: CostText_222 / Toast`），并明说「⛔ 你自己写脚本时别按 pid 认 GO」。
   ⇒ 凡是**按 GO pid 取组件**的读数（`menu_dump` 的「组件（类名）/ 文字」那几列）在本包上**不可信**；
   本报告一律改用 **`MonoBehaviour_*.json` 的 `m_GameObject` + `m_text` 内容**逐个认领（例：`MB_507` 的
   `m_text = "Congratulations!\nYou've earned an achievement. "` 与 `Toast` prefab 的 `Text` 对上）。
   `menu_dump` 那行把 `Toast 根` 的组件印成 `TextMeshProUGUI`、把 `Text` 印成 `'Serghar Targorst Serghar '` —— **就是撞车的产物，不要采信**。

---

## 总体评价（只写事实）

**对得上的（逐颗核过，不算客套）**

- 6 个 prefab 根的 `m_AnchorMin/Max` / `m_Pivot` / `m_AnchoredPosition` / `m_SizeDelta`：`RT_341/342/343/344/351/359` 逐字段一致（除 H1 那个派生符号）。
- 子件序：`Mission` = `349→347→345→346→348` ✓；`LevelUp` = `353→352→354(→369)` ✓；`Toast` = `355→356` ✓；`GetXP` = `358→357` ✓；
  `Tooltip` = `367(→380→382, 379, 378, 365, 368)` ✓；`ResourceCounter` = `372(→370), 371` ✓。
- TMP 字号 / 颜色 / 水平·垂直对齐 / 折行：`MB_498 / 507 / 514 / 505 / 519 / 517 / 523 / 560 / 559 / 502` 逐条比过，**只有 L1 / M3 / M4 四处不对**。
- 根底图：`Toast` = `UI_Deck_Information_submenu_Back_opaque`（69×63 · border 18 · ppuMul 1）✓；
  `LevelUp`/`GetXP` = `WF_UI_Generic_FancyNotification_Background`（352×116 · border 20 · ppuMul 1.3）✓；
  `Background Darkener` = 无 sprite + `(0.2828,0.3011,0.3868)` ✓；`Handle` = 无 sprite + `(0.9412,0.7255,0.3137)` ✓；
  进度条两格 = `Sliced · ppuMul 2` · `(1,0.5926,0)` / `(1,0.7686,0.3333)` ✓。
- 时序常量：`工具/read_literal.py d:/2/unity_run_ref/GameAssembly.dll 0x1834b2bb8 0x1834b2da8 0x1834b2e8c 0x1834b2c80`
  复读 = **1.0 / 10 / 3 / −0**（`−0` 的 int = `0x80000000` = 符号位翻转）✓ —— 与实现的 `MoveSeconds 1` / `FirstSlotOffsetPx 10` / `HoldSeconds 3` / `−h` 一致。
- `SetEase(0x1b)` = **27 = OutBack** · `SetEase(0x1a)` = **26 = InBack** ✓（唯一问题是 H3 的插值）。
- 序列结构：`Append(→index*h+10, 1s)` → `AppendInterval(3s)` → `Append(→−h, 1s)` → `AppendCallback(销毁 + freeSpot=min(index,freeSpot))` 逐句对上；`index = freeSpot; freeSpot++` 也对上。
- `TryShowNotification`：`ControllerId` 非空 **且** 注册过 ⇒ 走 handler；否则（含「非空但没注册」）⇒ `ShowNotificationInternal` ✓。
- `ScheduleNotification` 的「推迟一拍」用 `_scheduled` 队列在 `Step` 开头排空 ✓（等价，已如实标注）。
- `ToastNotification.Initialize` 的「`context.Icon` 为 null 时**不清**图标」与「最后 `SetActive(true)`」两条语义对上 ✓（`:835`、`:902-910`）。
- `Instantiate(prefab, holder, false)` 与原版 2 参重载等价（Unity 的 `Instantiate(Object, Transform)` 内部传 `false`）✓。
- 批处理：`Step(dt)` 自检口在（`:944`）、`Update` 只喂 `Time.deltaTime`、`Kill` 用 `Application.isPlaying ? Destroy : DestroyImmediate`（`:988`）✓ ——
  与 CLAUDE.md「批处理无帧循环 / 用 DestroyImmediate」一致。
- 缺图出声：`WF_UI_Generic_FancyNotification_Background` **确实不在** `Resources/`（`find Assets -ipath "*Resources*" -iname "*Fancy*"` = 0 条；
  只有源图 `CardPresentation/Art/原版/0_mainmenu/WF_UI_Generic_FancyNotification_Background.png`）⇒ 实现的「退回同族 + 出声一次」**是真的**，没把缺图写成原版行为 ✓。
- 没有数据源的三档（`ShowCollectionGainXP` / `ShowCollectionLevelUp` / 成就）**都真的 `LogWarning` + 返 false** ✓，没有编数值。

**不对的**：H1（几何符号）· H2（高度公式）· H3（回弹被夹平）· M1（父节点静默退化）· M2（重建后控制器丢失）· M3/M4（对齐 + autosize）· M5（队列压在黑场之上）· L1–L12。

**最该先修的三条**：**H1 → H2 → H3**（H1 是纯符号、改一行；H2 会把背景高度与槽位步长一起改对；H3 会让动画真的像原版）。
M1/M2 建议同批处理（都是 `Build` 入口的事）。
