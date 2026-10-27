# B9_C3T4d · 落点重钉（菜单/外壳 80 + 战斗界面/HUD 6）—— 报告

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 范围：`菜单_外壳.tsv` 的 80 条「落点非方法级」+ `战斗界面_HUD.tsv` 的 6 条「落点失效」（清单 = `判据/B行逐条核_菜单外壳.tsv` / `_HUD_前半.tsv`）。
> 产出：`判据/落点重钉_菜单HUD.tsv`（6 列 × 86）· `账/_parts/B9_C3T4d.tsv`（11 列 × 86，`我们的落点` 已改、`状态` 原值不动、`轮次`=B9_C3T4d）· 本报告。

## 一、方法

1. 按**方法名**到 `Assets/**/*.cs` 找真身（⛔ 不看账里旧行号）：先搜原版方法名，再搜落点里点名的等价方法名（`IsCompressed`/`RegisterDrag`/`RefreshGray`…），在落点提示的文件里定位**真声明行**。
2. 找到 ⇒ 新落点 = `路径:行（裸声明名）（等价实现 = <一句>）`；🔴 **每条都实际打开那一行并抄原文进本报告 §三**。
3. 找不到（我们只有内联逻辑 / 字段 / 走别类 / 未做）⇒ **不改落点**，在 TSV/报告写「无落点：<原因>」，⛔ 不硬指字段/注释行。
4. 产出前按方法名 + 旧落点对了一眼**当前主表**；`check_patch_conflicts.py` → 无跨补丁重复键（4 份补丁）。

## 二、结果分布

| 结果 | 条数 | 说明 |
|---|---|---|
| **已重钉** | **25** | 找到真实方法声明（7 条「是」等价、18 条「部分」等价） |
| **无落点** | **61** | 我们只有内联逻辑/字段/走别类/未做，**没有可指的方法声明** |
| 合计 | 86 | 菜单 80 + HUD 6 |

## 三、25 条已重钉（每条都实际打开该行，原文前 80 字符见「该行原文」列/下表）

| # | 域 | 原版方法 | 新落点（文件:行（裸声明名）） | 该行原文（前 80 字符） | 等价 |
|---|---|---|---|---|---|
| 1 | 菜单 | GetCardsInHandNumberWhereDistanceBetweenThemMustBeReduced | `CardPresentation/Hand/HandLayout.cs:545（IsCompressed）` | `public bool IsCompressed(int count)` | 部分 |
| 8 | 菜单 | DragDelegate | `CardPresentation/Shell/PointerLayer.cs:313（RegisterDrag）` | `public static void RegisterDrag(WindowButton b, System.Action<Vector2> onDrag, System.Ac…` | 部分 |
| 11 | 菜单 | ChangeCanvasColor | `CardPresentation/Shell/WindowButton.cs:933（RefreshGray）` | `void RefreshGray()` | 部分 |
| 19 | 菜单 | SetUpButtonColors | `CardPresentation/Shell/WindowButton.cs:166（KeyOf）` | `Color KeyOf(BtnState st)` | 部分 |
| 20 | 菜单 | DrawForArmy | `CardPresentation/Core/CardArt.cs:731（FlavorBg）` | `public static Texture2D FlavorBg(string faction)` | 部分 |
| 25 | 菜单 | CreateTabButton | `CardPresentation/Shell/GameWindowWithTabs.cs:228（CreateTabButton）` | `public WindowButton CreateTabButton(WindowTabBase tabInstance, string tabName)` | 是 |
| 26 | 菜单 | DestroyTab | `CardPresentation/Shell/GameWindowWithTabs.cs:241（DestroyTab）` | `public void DestroyTab(WindowTabBase tab)` | 是 |
| 32 | 菜单 | Start | `CardPresentation/Shell/GenericOptionsPanel.cs:240（Create）` | `public static GenericOptionsPanel Create(WindowsManager mgr, Context? ctx = null, …` | 部分 |
| 41 | 菜单 | ToggleScroll | `CardPresentation/Shell/MainMenuRuntime.cs:2640（ToggleScroll）` | `public void ToggleScroll(bool option)` | 是 |
| 43 | 菜单 | OnInitializationComplete | `CardPresentation/Shell/SceneInitializer.cs:75（OnInitializationComplete）` | `public interface IInitializationComplete { void OnInitializationComplete(); }` | 部分 |
| 44 | 菜单 | InstantiateInput | `CardPresentation/Shell/PromptPopup.cs:97（Create）` | `public static PromptPopup Create(WindowsManager mgr, string text, string okText, …` | 部分 |
| 52 | 菜单 | Awake | `CardPresentation/Shell/ShellRuntime.cs:341（Awake）` | `void Awake()` | 部分 |
| 55 | 菜单 | CalculateFocusedScrollPosition | `CardPresentation/Shell/MenuScroll.cs:370（FocusOn）` | `public void FocusOn(float contentCenter)` | 部分 |
| 58 | 菜单 | OnEnable | `CardPresentation/Shell/SearchingOpponentWindow.cs:147（OnEnable）` | `void OnEnable() { NetMatchmaking.OnHint += ShowHint; }` | 部分 |
| 59 | 菜单 | AddTabButton | `CardPresentation/Shell/TabButtons.cs:157（AddTabButton）` | `public WindowButton AddTabButton(string tabName)` | 是 |
| 60 | 菜单 | Draw | `CardPresentation/Shell/ItemDrawer.cs:840（Draw）` | `public static ItemDrawResult Draw(Transform parent, PxRect box, ItemSpec item, …` | 部分 |
| 62 | 菜单 | Draw | `CardPresentation/Shell/ItemDrawer.cs:840（Draw）` | 同 `Draw(...)`（图标档） | 部分 |
| 69 | 菜单 | LocalPositionMatrix | `CardPresentation/Core/CardArt.cs:776（Gradient）` | `public static Texture2D Gradient(Color c1, Color c2, float angleDeg, …` | 部分 |
| 71 | 菜单 | Initialize | `CardPresentation/Shell/SettingsWindow.cs:2728（AccountOpenUrl）` | `void AccountOpenUrl(string what, string url)` | 部分 |
| 72 | 菜单 | Close | `CardPresentation/Shell/GameWindow.cs:714（Close）` | `public virtual void Close()` | 是 |
| 74 | 菜单 | Close | `CardPresentation/Shell/GameWindow.cs:714（Close）` | `public virtual void Close()` | 是 |
| 79 | 菜单 | OnSetup | `CardPresentation/Shell/WindowTabBase.cs:52（OnSetup）` | `protected virtual void OnSetup() { }` | 部分 |
| 80 | 菜单 | Toggle | `CardPresentation/Shell/WindowTabBase.cs:76（Toggle）` | `void Toggle(bool option)` | 是 |
| H4 | HUD | CloseBattleDoors | `CardPresentation/Battle/BattleDoors.cs:434（Play）` | `public void Play()` | 部分 |
| H5 | HUD | GetUnitSizeRatio | `CardPresentation/Board/ArenaSlots.cs:90（CardScale）` | `public static float CardScale(bool enemy) { return enemy ? EnemyCardScale : …` | 部分 |

> ⚠️ 标「部分」的：等价实现只覆盖原版那段的一部分（例 #1 只有「是否压缩」谓词、没有「返回临界张数」；#71 是唯一的开 URL 动作、不是 Initialize 的入口壳）。「是」= 方法级对齐。

## 四、61 条「无落点」（不改落点；全表见 `判据/落点重钉_菜单HUD.tsv`）

原因归四类（每条的具体原因在 TSV「新落点」列）：
- **只有字段/属性、无方法**（例 `ToggleESC`→`GameWindow.closeOnEsc` 字段、`RemoveClickListeners`→`onClick` 字段、`CheckActiveMenus`→`TopWindow` 属性、`SetPosition`→锚点属性）。
- **我等价实现是内联逻辑 / 数据表，不是方法**（例 `CalculateSpacing`（各窗写死）、`InverseLerp`（CardArt 渐变内联）、`GetWorldRect`（PxRect 体系）、`ItemDrawer.GetItemName`（名字层内联）、`ProfileData.SetTitle`（称号表为数据）、`ShopData.GetID`（商品为数据））。
- **未做 / 走别处**（例 `OnDisable`（本件无 DrivenRectTransformTracker）、`ScenarioGenericObjectToggler` 的 `SetTargetFade/Start/Update`、`ReferralPopupWindow.Refresh/Start`、`SettingsWindow` 的一串 Change 回调）。
- **走基类/别类**（例 `WelcomePopup.OnClose`（走基类）、`WhereToGetCardsPopup.RefreshLayout`）。

HUD 4 条：`GetParentCard` / `Start`（CardInteraction 无）、`BasicBattleEndSequence` / `ResolveDisplayFromHand`（BattleDriver 无同名方法，结束流程散在 `ShowEndPanel…`）。

> 🔴 **「无落点」不是「判 D / 不做」** —— 是本轮**没有可指的方法声明**（内联/字段/未做）；这些行 `状态` 仍保持原值 `B`，后续要么补方法（让落点可指），要么把等价实现抽成方法。

## 五、额外：#35 的完整签名（供你修主表）

- 账里 `LiveOpsAssetUtility.TRef>` 被截断，**完整签名**：
  ```
  private static AssetReference GetReference<T, TRef>(this LiveOpsEventData liveops, List<TRef> references, T key) where TRef : EventReference<T>
  ```
  它是 **extension method**（`this LiveOpsEventData`）。
- 出处：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/LiveOps/LiveOpsAssetUtility.cs:10` 与 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsAssetUtility.cs:154`（逐字一致）；调用示例 `RankedV3Event.cs:107`（`GetReference<EventComponentReferenceType, EventComponent>(…)`）。

## 六、验证与残余（如实）

- 25 条重钉**逐条打开目标行读过原文**（见 §三）；均为真实方法声明（含 2 条基类 `GameWindow.Close`、1 条接口声明 `OnInitializationComplete`、1 条 `Draw` 复用给两行）。
- 61 条「无落点」是**搜过落点提示文件 + 全仓按方法名**后仍无可指方法声明；原因已在 TSV 写清。⚠️ 其中极少数可能是被重命名到我未猜到的名字（如 `PlayButtonSound`、`OpenChooseNameMenu`），**若你掌握别名可再指定**。
- 补丁：`状态` 全保持原值 B；`轮次`=B9_C3T4d；`我们的落点` 只对 25 条已重钉行改写（61 条无落点行原样保留）；11 列无坏行。
- 生成器临时脚本：`d:/4/Unity/_tmp_view/gen_c3t4d.py`（按方法名+旧落点匹配主表、读行校验后写盘）。
