# W58 · 界面线：Toast 规格与生命周期（执行报告）

> 执行代理 **W58-TOASTFIX**，2026-10-20。
> 判据 = `资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md`（W58-INV-TOAST）。
> ⛔ 未改 `Editor/ShellScene.cs` / `Core/` / `Battle/` / 正本；未跑 Unity、未动 git、未截图。
> 只改白名单 5 个文件里实际需要的 3 个：`Shell/ToastNotifications.cs` · `Shell/MessageToast.cs` · `Shell/ShellRuntime.cs`
> （`Shell/LoadingMenu.cs` / `Shell/MainMenuRuntime.cs` 本批**零改动**）。

---

## 结论（3 句内）

1. **D5 已落地**：新增 `ToastNotifications.ClearLive()` / `DisposeForSceneChange()`；`ShellRuntime` 在 `LoadSceneWithFade` 的三条路径上**切场景前**调后者，并额外挂了一条**全局 `sceneLoaded` 兜底**（覆盖不走 `LoadSceneWithFade` 的直调 `LoadScene` 站点）⇒ toast 控制器与在飞飘窗**不再跨场景存活**，回主菜单由 `MainMenuRuntime.Build` 重建（与原版「控制器只属于主菜单场景」同构）。
2. **D6 / D7 / D9 已修**：三颗（`Toast` / `Level Up` / `Get XP`）把可见底图接进 `WindowButton.TintOn`（`.9608` / `.7843` / `0.1s` 用类默认值，**Mission 不接**）；`Player Level Text` 改 `wrap:true`，并**顺带修掉 `Tmp()` 里「自适应 + 想折行」那条静默 NoWrap 的分支**（`Level Up ▸ Text` 一起回到原版的 Normal）；`MessageToast.ShowError` 转发 `ErrorMessageBanner.ShowError`（`(0.8,0,0,1)`）。
3. **D4 / D10 只报告、不硬改**：D4 的前置（联盟搜索是窗、原版真机姿态未查清）不满足；D10 缺的是**原版真机 `rect.height` 读数**，本地静态资源推不出来 —— 两条的「缺什么 / 去哪拿」见下。

---

## D4–D10 逐条（原版值 vs 改后 + 出处）

### D5 [已改] Toast 生命周期：切场景即销毁

| 项 | 原版 | 改前（我们） | 改后 |
|---|---|---|---|
| 控制器归属 | **只在主菜单场景**（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour_1939.json`；13 个 `battlearena*` 里 **0 处**）⇒ 切场景整棵销毁 | 建在 DDOL 壳的 `Safe area Only Horizontal` 下（`Shell/MainMenuRuntime.cs:524` → `ToastNotifications.Build`），`LoadSceneWithFade` 不清 ⇒ 跨场景存活 | `ShellRuntime.LoadSceneWithFade` **切之前**调 `ToastNotifications.DisposeForSceneChange()`；另加全局 `sceneLoaded` 兜底 |
| 在飞飘窗 | 随控制器一起没 | 留到自然退场（5 s） | `ClearLiveInternal()`：先 `SetActive(false)` 再销毁、清 `_scheduled`、`_freeSpot=0` |
| 回主菜单 | 新场景里本来就有一份控制器 | — | `MainMenuRuntime.Start → Build` 重建（`sceneLoaded` 早于 `Start`，不会误杀新的一颗） |

**改动点**

- `Shell/ToastNotifications.cs:1210-1258`：
  - `ClearLive()` —— 只清在飞 + 排队 + 格位，控制器留着；
  - `DisposeForSceneChange()` —— `ClearLiveInternal()` + `gameObject.SetActive(false)` + `DisposeForTest()`（Play 下 `Destroy` 是帧末，先关掉保证切场景那一帧起不再画）。
- `Shell/ShellRuntime.cs`：
  - `:125` 直切分支（`rt == null || rt.Loading == null`）；
  - `:141` 批处理/编辑态分支（`LoadingFade(true, cb)` 的回调里）；
  - `:187` 播放态（黑场 α=1 时、`LoadScene` 之前）；
  - `:390-411` 全局 `SceneManager.sceneLoaded += OnSceneLoadedCleanupToast`（`Build()` 里挂、`OnDestroy` 撤）；`SceneCleanupHookInstalledForTest` 只读口。

**判据取舍（交付物要求的「两者选一」）**：选**生命周期等价**（切场景销毁 + 主菜单重建），
⛔ 没选「按场景名只允许主菜单创建」。理由：

1. 我们**有生产调用点的只有主菜单**（`grep "ToastNotifications\."` 除本文件/自检外只命中 `MainMenuRuntime.cs:524` 的 `Build`；`ShowMission` / `ShowCollectionGainXP` / `ShowCollectionLevelUp` 全仓零生产调用）；
2. 我们的**卡组编辑是独立场景**（`DeckEditor.unity`），而原版卡组编辑是**主菜单里的窗** —— 若做「场景名白名单」，就得先裁定「DeckEditor 算不算主菜单」，那是界面线的产品决定，不该由本批发明；
3. **残留差异如实记**：`ToastNotifications.Ensure()` 仍会在任何场景按需重建（若将来有战斗侧调用 `Show*`）。今天零调用点 ⇒ 实际不会出现；见「没查清的部分」。
4. 顶部错误横幅 `MessageToast` **故意不跟着清**：原版 13 个战场场景里都有 `UI Error Message Controller`（`Shell/MessageToast.cs` 文件头 ②）⇒ 它跨场景是**对的**。

### D6 [已改] 三颗 toast 的 hover 色偏

| 项 | 原版 | 改前 | 改后 |
|---|---|---|---|
| `Toast` / `Level Up` / `Get XP` 根 | `EverguildButton` · `m_Transition = 1 (ColorTint)` · `m_Colors` 全库默认（Normal 白 / Highlighted **0.9608…** / Pressed **0.7843…** / `m_FadeDuration 0.1`）· target = 各自根 Image（`MonoBehaviour_504/508/513.json`） | `MenuDraw.Hit(..., target: null)` ⇒ 色偏只打在全透明命中 quad 上 ⇒ **视觉零变化** | `ShowInternal` 里（**量高之后**）`hitBtn.TintOn(Background 的九宫格 quad)` |
| `Mission` 根 | `m_Transition = 0 (None)`、`target = 0`（`MonoBehaviour_520.json`） | 无（碰巧对） | **仍不接**（`ctx.Type != MissionProgress` 才接） |

**实现说明**（`Shell/ToastNotifications.cs:855-880`）：

- 用 `WindowButton.TintOn(...)` 而**不是** `MenuDraw.Hit(target: ...)`：我们的命中区是**另建的透明节点**，可见底图是它的兄弟 —— `TintOn` 正是 `WindowButton` 里为这一族准备的 `m_TargetGraphic` 等价口（`Shell/WindowButton.cs` A222 注释）。
- **必须放在 `ApplyMeasuredHeight` 之后**：`Toast`/`Get XP` 的 `Background` 九宫格会在那一刻按现量高度**重建**，之前抓的 quad 是旧的。
- `.9608` / `.7843` / `0.1s` 直接用 `WindowButton.HighlightKey` / `PressedKey` / `FadeSeconds` 的默认值 ⇒ 不逐颗覆盖（原版这三颗就是全库默认组）。
- 接不上时**出声**（`Background` 下没有 quad ⇒ 告警，⛔ 不静默）。

### D7 [已改] `Player Level Text` 折行

| 项 | 原版 | 改前 | 改后 |
|---|---|---|---|
| `Player Level Text` | `m_TextWrappingMode = 1 (Normal)`（`MonoBehaviour_505.json`） | 调用点 `wrap:false`（`ToastNotifications.cs:575` 旧行） | 调用点 `wrap:true`（`:582-586`） |
| `Level Up ▸ Text` | `m_TextWrappingMode = 1 (Normal)`（`MonoBehaviour_523.json`） | **也一直是 NoWrap**（静默） | 随下面那条一并回到 Normal |

🔴 **只改调用点不够**（实测 `Battle/Label.cs:1494` 的 `BuildTmp` 出厂就写 `NoWrap`）：
`Tmp()` 的 `autoMin > 0` 分支原来只在 `!wrap` 时调 `SetWrapping(false)`；`wrap == true` 时**什么都不调** ⇒ 仍是 NoWrap。
现改为 **两态都显式写** `lb.SetWrapping(wrap)`（`Shell/ToastNotifications.cs:1339-1345`）。
`Get XP ▸ Text` 仍是 `wrap:false` ⇒ NoWrap（原版 `MB_514` = 0，**不动**）。

### D9 [已改] 壳侧红色错误通道

| 项 | 原版 | 改前 | 改后 |
|---|---|---|---|
| `ShowError` | `UIMessageController.ShowError(串, localize)` = `ShowMessage(串, errorColor, …)`，`errorColor = (0.8,0,0,1)`（`__ShowError.c` 只有一句转调；`MC_2087.json`） | `Shell/MessageToast` **只转发 `ShowMessage`（白）** ⇒ 壳侧弹不了红字（`ErrorMessageBanner.ShowError` 本身早就有） | 新增 `MessageToast.ShowError(string, bool localize=false, string fallback=null)` → `_banner.ShowError(...)`（`Shell/MessageToast.cs:200-224`） |

- 空串行为同 `Show`：`Debug.LogError` + 返回 false（原版也是 LogError + return）。
- 白/红是**同一条通道的两个色档**（同「5 条一池、轮转」），⛔ 没另建第二颗横幅。

### D4 [未改 · 只报告] 转圈件层级

- 原版：`MenuLoadingController` 子树**没有自己的 Canvas**（唯一例外 `Cog` 的 `m_OverrideSorting = false`）⇒ 继承 `Game UI` 的 **Default / 0** ⇒ 在全部 PopUps（窗 0 / 错误横幅 1 / 教学 601 / toast 900）与 Blocking（tooltip / 黑场）**之下**。
  唯一原版调用点 = `JoinAllianceMenu`：`__Initialize.c:53` / `__FillOpenAlliances.c:29` `ShowLoading()`，回调 `…_b__0.c:30` `HideLoading()`。
- 我们：`Shell/LoadingMenu.cs:244` `QDark/QSkull/QCog = 4080/4081/4082`（**高于** toast 4072、高于窗 4030）；**今天零生产调用点**（`grep LoadingCtl` 只命中 `ShellRuntime` 的属性与 `PointerLayer` 的「转圈件挡指针」判断）。
- **为什么本批不动（判据不足）**：
  1. 我们的联盟搜索**不是页、是窗**：`AlliancesTab : SocialPage`（`Shell/AlliancesTab.cs:119`）里的 `AllianceSearchTab : SocialView`（`Shell/AllianceSearchTab.cs:13`），宿主 `SocialWindow : MainMenuSubmenuWindow`（`Shell/SocialWindow.cs:34`，页带 `QPageBase = 3200`），窗底 `MainMenuSubmenuWindow.QPanel = 3005` / `QContent = 3010` / `QText = 3011`（`Shell/MainMenuSubmenuWindow.cs:97`）。把转圈件降到原版那一档（本报告建议的 `2999–3001`）会**躲在窗外/窗底之下、根本看不见**。
  2. 原版自身的**运行期姿态未查清**：`Menu Dark Background` 序列化 `m_IsActive = false`，而它是 `Holder` 的唯一子件（`W_D_ShellLoading.md` §3·3）⇒ 原版到底怎么显示、显示时压住什么，本地没有实况证据。
- **还差什么（按顺序）**：① 界面线拍板「联盟搜索要不要从窗改成主菜单页」；② 改页后把 `LoadingCtl.Show()/Hide()` 接到 `JoinAllianceMenu` 那条数据加载链；③ 再把 `QDark/QSkull/QCog` 落到 `2999–3001`（壳内容之上、窗之下）；④ 进原版/实拍确认暗底姿态。
  ⇒ 本批**保持现状**并**记账**（铁律 11：要做，只是先后）。

### D10 [未改 · 只报告] 根高原版真机读数

- 原版：`VLG(m_ChildControlHeight = 0) + ContentSizeFitter(PreferredSize)` 三轮重建 ⇒ 根高随文案变。
- 我们：`ToastRootHeight` 按**本次文案现量**（`Text` 的 `Label.WorldH`）+ `padT + iconH + textH + padB`（`Shell/ToastNotifications.cs` 的 `ToastRootHeight`）。**口径方向对**，但**没有原版 `rect.height` 的实况读数**可对照。
- **缺什么**：原版运行时 `Toast` / `Toast Collection Get XP` 根 `RectTransform.rect.height`，至少三档 —— (a) 单行文案、(b) 折行多行文案、(c) `Get XP` 那一档（字号 55.46）。
- **去哪拿**：本地原版 player `d:/2/unity_run_ref/`（MelonLoader + `SceneJumpShot` mod，源码 `d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs`，可加探针）。探针做法：主菜单场景里取 `Safe area Only Horizontal/Toast Notification Controller`，读 `MonoBehaviour_1939` 的 `notificationTypes` 两张 prefab（491 / 492），`Instantiate` 到 `Holder`，灌一个已知文案后跑 `FixLayout()` + `ForceRebuildLayoutImmediate()`，打印 `rect.height`。⚠️ 原版前端已关服、真实 toast 事件不可触发 ⇒ **只能这样直接实例化 prefab 量**；这一步**本批未做**（红线 2：不跑 Unity/player），⛔ 不猜数。

---

## 断言块（可粘贴 —— ⛔ 本批没改 `Editor/ShellScene.cs`）

**插入位置**：`Editor/ShellScene.cs` 里 `Section("🆕 N20/N21/N23：Toast 控制器 …")` 那个块的**收尾 `}` 之后**、`Debug.Log(P + shell.Dump());` **之前**（即 `ToastNotifications.DisposeForTest();` 收尾那一段的右花括号外）。
依赖的公开口：`ToastNotifications.{ClearLive,DisposeForSceneChange,LiveCount,FreeSpot,ScheduledCount,Holder,Templates,LevelUpPrefabName}` ·
`ShellRuntime.SceneCleanupHookInstalledForTest` · `WindowButton.{HighlightK,PressedK,Enter,Press,Release,Exit}` ·
`ImageQuad.Tint` · `Label.WrappingMode` · `MessageToast.{Ensure,Show,ShowError,Banner}` · `ErrorMessageBanner.TextColorOf`.

```csharp
        // ============================================================ 🆕 2026-10-20（W58）Toast 规格与生命周期
        // 判据：资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md §五 D5/D6/D7/D9；
        //       落地与逐条对照 → 资料/普查产出_1020/W58_界面线_Toast规格与生命周期.md
        //  ⚠️ 本节只验「我们照判据做到了」；原版真机 `rect.height`（D10）本地没有读数 ⇒ 不在本节断。
        Section("🆕 W58：toast 生命周期 / hover 色偏 / Player Level 折行 / 错误色转发");
        {
            ToastNotifications.DisposeForTest();
            MessageToast.DisposeForTest();
            var w58 = ToastNotifications.Build(root);
            CheckTrue(w58 != null && w58.Built, "（前提）W58：Toast 控制器建出来了");

            // ---- ① D5：ClearLive() 只清在飞；DisposeForSceneChange() 连控制器一起销毁 ----
            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.a");
            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.b");
            CheckTrue(w58.LiveCount == 2 && w58.FreeSpot == 2,
                      $"（前提）W58-D5：两条在飞（现读 Live={w58.LiveCount} / freeSpot={w58.FreeSpot}）");
            ToastNotifications.ClearLive();
            CheckTrue(w58.LiveCount == 0 && w58.FreeSpot == 0 && w58.ScheduledCount == 0
                      && ToastNotifications.Instance == w58,
                      $"★★ W58-D5①：`ClearLive()` 清空在飞/排队/格位、**控制器还在**"
                    + $"（现读 Live={w58.LiveCount} / sched={w58.ScheduledCount} / freeSpot={w58.FreeSpot}）");

            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.c");
            var w58Ctrl = w58.gameObject;
            ToastNotifications.DisposeForSceneChange();
            CheckTrue(ToastNotifications.Instance == null && w58Ctrl == null,
                      "★★ W58-D5②：`DisposeForSceneChange()` ⇒ 控制器节点**整棵销毁**、`Instance` 归 null"
                    + "（= 原版『Toast 控制器只在主菜单场景，切场景即销毁』；13 个战场 0 处）"
                    + "｜🧨 只清在飞、不销毁控制器 ⇒ 本条红");
            CheckTrue(ShellRuntime.SceneCleanupHookInstalledForTest,
                      "★ W58-D5③：`ShellRuntime.Build` 挂上了『任何 sceneLoaded ⇒ 清 toast』的兜底订阅"
                    + "（覆盖不走 `LoadSceneWithFade` 的直调 `LoadScene` 站点）"
                    + "｜🧨 删掉 `Build()` 里 `EnsureSceneCleanupHook()` ⇒ 红");

            // ---- ② D6：Toast/LevelUp/GetXP 悬停乘 0.9608 / 按下 0.7843；Mission 不接 ----
            w58 = ToastNotifications.Build(root);          // ② 之前刚整棵销毁过 ⇒ 重新建
            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.hover");
            var aToast = w58.Holder != null ? w58.Holder.GetChild(w58.Holder.childCount - 1) : null;
            var aBg = aToast != null ? aToast.Find("Background") : null;
            var aQuads = aBg != null ? aBg.GetComponentsInChildren<ImageQuad>(true) : null;
            var aHit = aToast != null ? aToast.GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(aQuads != null && aQuads.Length > 0 && aHit != null,
                      "（前提）W58-D6：toast 有 `Background` quad 和命中 `WindowButton`");
            if (aQuads != null && aQuads.Length > 0 && aHit != null)
            {
                var aBase = aQuads[0].Tint;
                aHit.Enter();                 // 编辑模式 ⇒ `SetTarget` 直接落目标色（不走 0.1s 补间）
                float k = aQuads[0].Tint.g / Mathf.Max(1e-6f, aBase.g);
                CheckTrue(Mathf.Abs(k - WindowButton.HighlightK) < 1e-3f,
                          $"★★ W58-D6①：悬停后**可见底图**乘 0.9608（现读 k={k:F4}，期望 {WindowButton.HighlightK:F4}）"
                        + "（原版 `MonoBehaviour_504.json`：`m_Transition = 1` · `m_HighlightedColor = 0.9608` · fade 0.1）"
                        + "｜🧨 删掉 `ShowInternal` 里那句 `hitBtn.TintOn(bgQuads)` ⇒ 本条红（色只打在透明命中区上）");
                aHit.Press();
                float kp = aQuads[0].Tint.g / Mathf.Max(1e-6f, aBase.g);
                CheckTrue(Mathf.Abs(kp - WindowButton.PressedK) < 1e-3f,
                          $"★ W58-D6②：按下后乘 0.7843（现读 k={kp:F4}，期望 {WindowButton.PressedK:F4}）"
                        + "｜🧨 把 `TintOn` 换成只给命中区上色 ⇒ 红");
                aHit.Release(); aHit.Exit();
            }
            var mission = new ToastNotifications.Context("w58 mission",
                                                          ToastNotifications.ToastNotificationType.MissionProgress);
            mission.ShowProgress = true; mission.Progress01 = 0.5f; mission.ProgressText = "1/2";
            ToastNotifications.ShowNotification(mission);
            var mToast = w58.Holder.GetChild(w58.Holder.childCount - 1);
            var mBg = mToast.Find("Background");
            var mQuad = mBg != null ? mBg.GetComponentInChildren<ImageQuad>(true) : null;
            var mHit = mToast.GetComponentInChildren<WindowButton>(true);
            CheckTrue(mQuad != null && mHit != null, "（前提）W58-D6：mission toast 有 `Background` quad 和命中钮");
            if (mQuad != null && mHit != null)
            {
                var mBase = mQuad.Tint;
                mHit.Enter();
                CheckTrue(Mathf.Abs(mQuad.Tint.g - mBase.g) < 1e-4f,
                          $"★★ W58-D6③（灭自证）：**Mission 悬停不变色**（现读 {mBase.g:F4}→{mQuad.Tint.g:F4}）"
                        + "（原版 `MonoBehaviour_520.json`：`m_Transition = 0 (None)`、`target = 0`）"
                        + "｜🧨 去掉 `if (ctx.Type != MissionProgress)`（四颗一起接）⇒ 本条红");
                mHit.Exit();
            }

            // ---- ③ D7：`Player Level Text` 折行 = Normal(1) ----
            var lvlText = w58.Templates != null
                        ? w58.Templates.Find(ToastNotifications.LevelUpPrefabName + "/Player Level (1)/Player Level Text")
                        : null;
            var lvlLb = lvlText != null ? lvlText.GetComponent<Label>() : null;
            Check(lvlLb != null ? lvlLb.WrappingMode : -1, 1,
                  "★ W58-D7：`Player Level Text` 的 `m_TextWrappingMode` = 1 (Normal)（原版 `MB_505`）"
                + "｜🧨 调用点改回 `wrap: false`、或 `Tmp()` 里去掉 `lb.SetWrapping(wrap)` ⇒ 本条红");

            ToastNotifications.DisposeForTest();

            // ---- ④ D9：`MessageToast.ShowError` 转发错误色（0.8,0,0,1）；白档仍是白 ----
            var mt = MessageToast.Ensure();
            CheckTrue(mt != null && mt.Built, "（前提）W58-D9：`MessageToast` 建出来了");
            if (mt != null && mt.Built)
            {
                bool okRed = MessageToast.ShowError("w58.err", false, "x");
                var red = mt.Banner.TextColorOf(0);
                CheckTrue(okRed && Mathf.Abs(red.r - 0.8f) < 1e-4f
                          && red.g < 1e-4f && red.b < 1e-4f && Mathf.Abs(red.a - 1f) < 1e-4f,
                          $"★★ W58-D9①：`ShowError` 文字色 = (0.8,0,0,1)（现读 {red.r:F4},{red.g:F4},{red.b:F4},{red.a:F4}）"
                        + "（原版 `UIMessageController.ShowError` → `errorColor`）"
                        + "｜🧨 让 `ShowError` 改走 `ShowMessage` 白档 ⇒ 本条红");
                bool okWhite = MessageToast.Show("w58.msg", false, "x");
                var white = mt.Banner.TextColorOf(1);
                CheckTrue(okWhite && Mathf.Abs(white.r - 1f) < 1e-4f
                          && Mathf.Abs(white.g - 1f) < 1e-4f && Mathf.Abs(white.b - 1f) < 1e-4f,
                          $"★ W58-D9②（反证）：白档仍是 (1,1,1,1)（现读 {white.r:F4},{white.g:F4},{white.b:F4}）");
            }
            MessageToast.DisposeForTest();
        }
```

> ⚠️ 若别的代理在这之前已经往 `ShellScene.cs` 加过同名 `Section` / 同名局部变量（`w58` 等），只改局部名即可；不要合并两套断言。

---

## 没查清的部分（如实记）

1. **D5 的残留差异**：`ToastNotifications.Ensure()` 仍会在**任何场景**按需重建控制器 —— 若将来有战斗/卡组编辑侧代码调 `ToastNotifications.Show*`，它会重新长在壳上。
   今天**零生产调用点**（已 `grep`），所以实际不会出现；没加场景名白名单的理由见 D5「判据取舍」。
2. **D5 的「战斗场景不再显示残留」是结构性保证，不是实况截图**：控制器在 `LoadScene` 之前就被 `SetActive(false)` + `Destroy`，`sceneLoaded` 再兜底一次；但**本批没进 player 看**（红线 2）。要实况确认：进主菜单弹一条 → 进战斗 → 截图（``_tmp_view/shots/…``）。
3. **D4 的原版运行期姿态**（暗底到不到 `activeSelf`、转圈件实际压在什么上面）本地没有实况证据（`W_D_ShellLoading.md` §3·3 早已记）。
4. **D10 的原版 `rect.height`**：静态资源只给得出「VLG + CSF 三轮重建」的推导，拿不到真机读数（见 D10 的「去哪拿」）。
5. 本批**未动** D1（Tooltip 层）/ D2（教学高亮）/ D3（错误横幅菜单档）/ D8（黑场 dt 注释）/ D11（自检口径）/ §六·11 的过期文档 —— 它们属于其他代理 / 主对话。
6. 看到 `Shell/MenuTutorialHighlight.cs` 有别人的在飞改动（`git diff --numstat` 7/2），**不是本批改的**。

---

## 改动清单

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/ToastNotifications.cs` | ① 文件头加 ⑤（D6）/ ⑥（D5）两条判据索引；② `ShowInternal` 里按类型接 `hitBtn.TintOn(Background quads)`（Mission 除外，量高之后）；③ `Player Level Text` `wrap:true`；④ `Tmp()` 的 `autoMin>0` 分支两态都写 `SetWrapping(wrap)`（修 LevelUp Text 的静默 NoWrap）；⑤ 新增 `ClearLive()` / `ClearLiveInternal()` / `DisposeForSceneChange()` 与判据段 |
| `Unity/MyGame/Assets/CardPresentation/Shell/MessageToast.cs` | 新增 `ShowError(string, bool, string)` → `ErrorMessageBanner.ShowError`（`(0.8,0,0,1)`）；空串 LogError + false |
| `Unity/MyGame/Assets/CardPresentation/Shell/ShellRuntime.cs` | ① `LoadSceneWithFade` 三条路径切场景前 `ToastNotifications.DisposeForSceneChange()`；② 全局 `sceneLoaded` 兜底订阅（`Build` 挂 / `OnDestroy` 撤）；③ `SceneCleanupHookInstalledForTest` |
| `Shell/LoadingMenu.cs` / `Shell/MainMenuRuntime.cs` | **零改动**（D4 只报告；D5 的回程重建走已有的 `MainMenuRuntime.Build`） |

`git diff --numstat`（本批三件）：
```
25	0	Shell/MessageToast.cs
49	1	Shell/ShellRuntime.cs
92	3	Shell/ToastNotifications.cs
```
（数字与文件行数同量级 ⇒ 没有翻行尾/整篇重写。`ToastNotifications` 的 92 行里含大段注释。）

---

## typecheck 结果

`TMPDIR=/tmp/wf_toastfix bash d:/4/Unity/工具/typecheck.sh`：

- 第一次：**运行时 0 / 编辑器 0** ✅
- 中途两次撞上**别的代理正在写的半成品**（`Editor/BattleScene.cs` CS0104 `MatchType` 歧义 · `RuleEngine/Core/RuleCore.cs` CS0103 `splashKills`）—— 均**不在本批白名单**，未改；
- 末次复跑：**运行时 0 / 编辑器 0** ✅

未跑任何 `*.Run` / 未起 player / 未截图（铁律 12）。
