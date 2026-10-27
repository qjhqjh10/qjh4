# B9_W25UPGR · 把 `CardDisplayLevelUp` 挂到升级流程上

> 执行代理 `B9_W25UPGR` · 2026-10-27 · **禁 Unity / 禁 git / 禁改正本**。
> 白名单：`Shell/CardDetailPopup.cs`（独占）+ 本报告 + 账补丁 `账/_parts/B9_W25UPGR.tsv`。
> `typecheck`：`TMPDIR=/tmp/wf_w25 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> （中途有一次跑出 2 条错，全在 `RuleEngine/Core/EffectResolver.cs` = **S0 正在写的文件**，隔 45 s 复跑归零 —— 不是本件的）。

---

## 一、结论

1. **挂点已接**：`DoUpgrade()` 成功之后（末尾、与 `RebuildKeepingState()` **同一行**）调
   `PlayUpgradeAnimation(after)` → 懒建 `CardDisplayLevelUp` 挂**窗根** → 接线 → `DoAnim()`。
   `onChangeFrame = OnLevelUpChangeFrame`（内部 = `RebuildKeepingState()` + 重指三格）⇒ 原版
   `upgradePanel.ChangeFrameOnLevelUp()` 的等价落点。
2. **输入锁**：组件侧「上锁 `DoAnim` / 解锁 `AnimationFinished`」照旧（`B9_W17FX2` 的产物，未改一行）；
   本件补的是**消费点** —— `UpgradeAnimLocked` + 升级钮点击闸。🔴 它是 **`🔶 自己设计`**（原版
   `UpgradePanel.TryUpgrade` **没有方法体产物**，读不到它到底怎么关输入），代码里已打标 + 写明裁定人与日期。
   ⚠️ 判据还多加了一条硬条件 `myAnimation != null`：**clip 接不上时锁必须不生效**，否则 `AnimationFinished`
   永不到达 ⇒ 升级钮**永久点不动**（比「没锁」更糟的静默缺陷）。
3. **关窗收尾**：`ResetFanState`（`Close`/`Hide` 共用）里加 `SettleLevelUpAnimation()` —— 组件的时钟只在它自己的
   `Update` 里推进，**窗一关就停** ⇒ 抖动那 1 s 的 `localPosition` 位移会**永久留在窗根上**（再开也是偏的，≤20 px）。
   🔶 这一档「动画被关窗打断怎么收尾」也是**我们定的**（判据缺）。
4. 🔴 **三条 clip 的真实状态比简报以为的好，但有一个致命的第二层**（本笔现读，见 §二·3）：
   * ✅ `Assets/WarpforgeVFX/Animations/{Level Up Card,Rank Up,Division Up}.anim` **已经在工程里**（今天 07:00 落盘，
     `.meta` 已在、`m_Legacy: 1`），`Level Up Card.anim` 的 `m_Events` **两条都在**（1.45 / 3.483333，与原版逐位相同）。
   * ❌ **但它们没有运行时加载路径**：目录不在 `Resources/` 下、也没有任何 prefab 引用它们
     （`grep -rn "Level Up Card" --include=*.cs --include=*.prefab --include=*.unity` 只命中**注释**）。
   * 🔴 **而且三条的 `m_AnimationClipSettings.m_StopTime = 1`**，而事件在 **3.4833 / 3.9167 / 3.8333 s**
     ⇒ 就算现在把 `Animation` 挂上去，**播一次也永远到不了事件时刻** ⇒ `ChangeFrame` / `AnimationFinished`
     一次都不会来（这正是结论 2 那条硬条件要防的现场）。
   ⇒ **本件没有接 `Animation`**（接了就是「看着接了、其实永远不触发」的静默缺陷）。同步点要做的是**三件**（§四末）。
5. **看得见的现状**（如实说）：升级之后玩家只会看到 **① 窗抖一下（DoAnim ③）② 1.45 s 时再抖一下 + 换框（ChangeFrame ①②）**；
   背景淡入淡出、2D cue、`Level Up Effect` 两件**都不会发生**（三格在我们这棵树上不存在 + cue 没查到 + clip 接不上），
   组件已经为「三格没接线」**出声一次**、本件为「cue 没接」**出声一次**。

---

## 二、证据（文件:行号）

### 2·1 本件改的地方（全部是**零行数变化**的既有行就地改写，除类尾追加）

| 位置 | 内容 |
|---|---|
| `Shell/CardDetailPopup.cs:1631` | 升级钮的点击闭包加闸：`if (UpgradeAnimLocked) { Debug.Log(…); return; } DoUpgrade();` |
| `Shell/CardDetailPopup.cs:2599` | `RebuildKeepingState(); PlayUpgradeAnimation(after);`（= 原版 `TryUpgrade` 里 `levelUpAnimationController.DoAnim()` 那一跳） |
| `Shell/CardDetailPopup.cs:983` | `_swapping = false; SettleLevelUpAnimation();`（关窗/藏窗收尾；`ResetFanState` 是 `Close`/`Hide` 的共用口） |
| `Shell/CardDetailPopup.cs:2843` | `public CardDisplayLevelUp LevelUpEffect { get; private set; }`（懒建；读数口） |
| `:2853` | `public bool UpgradeAnimLocked`（🔶 自己设计的消费点，见 §一·2） |
| `:2867` | `void PlayUpgradeAnimation(int afterLevel)` |
| `:2908` | `void OnLevelUpChangeFrame()`（`RebuildKeepingState()` + `RewireLevelUpTargets()`） |
| `:2917` | `void RewireLevelUpTargets()`（三格按名接；`fadeBackground` **保持空**，理由见 §三·2） |
| `:2931` | `CardDisplayLevelUp.FadeImage FadeImageOf(string)`（`ImageQuad` 优先 / `Renderer` 兜底） |
| `:2949` | `void SettleLevelUpAnimation()` |
| `:2979` | `public static List<string> SelfCheckCardDisplayLevelUp()`（组件六跳 + 3 条灭自证） |
| `:3094` | `public List<string> SelfCheckUpgradeAnimationWiring()`（本窗挂点 + 2 条灭自证） |
| `:3213` | `public static List<string> SelfCheckScenarioFaderDoFade()`（`doFade` 归零的**黑盒**灭自证） |

**行数账**：改前 **2816** 行 → 改后 **3265** 行（`git diff --numstat` = `452 / 3`，3 条删的都是**同行替换**）。
账里按 `文件:行号` 引用的落点**逐个现验未漂**：`:995 Build` · `:2254 CraftNow` · `:2564 DoUpgrade` ·
`:2626 RebuildKeepingState` · `:2788 ResetDisplayOrder`（`未分类_其他.tsv:114/115/116`、`卡牌面_渲染.tsv:65` 用得到）。

### 2·2 判据（本笔现读）

| 项 | 内容 |
|---|---|
| 驱动源 | `bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`：`m_Events` = `ChangeFrame@1.4500000476837158` · `AnimationFinished@3.4833333492279053` ⇒ **动画事件** |
| 组件公开面 | `Effects/CardDisplayLevelUp.cs`：`OnEnable/DoAnim/ChangeFrame/AnimationFinished/NotifierChangeFrame/NotifierAnimationFinished/FadeOutImage(s)/Tick` + 7 个计数器 + `InputBlocked` / `BackgroundAlpha` / `ShakeLeft`（`:236/244/263/272/305/321/331/334/344`） |
| 原版方法体 | `CardDisplayLevelUpAnimationController__{OnEnable,FadeOutImage,DoAnim,ChangeFrame,AnimationFinished}.c` · `CardDisplayLevelUpAnimationNotifier__{ChangeFrame,AnimationFinished}.c`（`d:/2/tools/decomp_full/`；逐句对照见 `报告/B9_W17FX2_特效组件第二批.md` §2·1） |
| 原版锁点 | 🔴 `UpgradePanel__TryUpgrade` 在 `decomp_full` **0 命中**、cpp2il 那几份只有签名壳 ⇒ **「谁上的锁」查不到**（口径见 §一·2） |
| 我们树上有没有那三格 | `grep -rn "Level Up Effect" Shell/` 只命中 `CardDisplayEffects.cs`（主菜单展示窗那棵）⇒ `Upgrade Panel` 这棵**没有** |
| 挂窗根的理由 | `Build()` 首句 `for (int i = root.childCount - 1; …) CollectionWindow.DestroySafe(root.GetChild(i).gameObject)`（`:998`）+ `ChangeFrame` @1.45 s 会再触发一次重建 |
| 量纲 | `LayoutSpace.Px(px) = px / (1080/10)`（`Core/LayoutSpace.cs:211`）；窗根挂在 `WindowHolder`（`WindowsManager.AttachToAnchor` 把 `localPosition=0`/`localScale=1` 归位）⇒ 抖 20 px 成立 |

### 2·3 三条 clip 的现读（本笔新证据，纠正两处旧记录）

```
Assets/WarpforgeVFX/Animations/Level Up Card.anim
  m_Name: Level Up Card · m_Legacy: 1 · m_SampleRate: 60 · m_WrapMode: 0
  m_Events:  {time 1.45, functionName ChangeFrame} · {time 3.483333, functionName AnimationFinished}
  m_AnimationClipSettings.m_StopTime: 1        ← 🔴 与事件时刻矛盾（三条都一样）
Assets/WarpforgeVFX/Animations/Rank Up.anim      m_Legacy: 1 · m_StopTime: 1 · m_Events: AnimationFinished@3.916667
Assets/WarpforgeVFX/Animations/Division Up.anim  m_Legacy: 1 · m_StopTime: 1 · m_Events: AnimationFinished@3.833333
```
* 三份都在（`ls -la` 时间戳 **2026-10-27 07:00**，`.meta` 07:40，guid 例：`5d0d5007e39f8cf4ea6ec31cd9db7476`），
  且 `Assets/WarpforgeVFX/*` 在 `.gitignore:26` ⇒ **它们是生成物、不入 git**（不是「没导」）。
* ❌ **两处旧记录已过期**（铁律 5，⛔ 本件无权重文件的作者文件，见 §五·1）：
  `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs:75`「`Rank Up` / `Division Up` / `Level Up Card`（**工程里没有，要导入**）」·
  `Effects/CardDisplayLevelUp.cs:69`「3 条 clip 没有导入」。

---

## 三、没查清的部分（如实记，⛔ 不是「大概是」）

1. 🔴 **原版那把输入锁是谁上的、怎么上的** —— `UpgradePanel.TryUpgrade` 无方法体产物（`decomp_full` 0 命中；
   四份 cpp2il 里只有 `out_cpp2il_dev_ilspy` 有真方法体这条口径，而它那一份是签名壳）。
   ⇒ 我们做成「组件上锁/解锁 + 点击闸」的**结构性等价**，并在代码里标 `🔶 自己设计`。**还差**：`UpgradePanel` 的
   VA 级反汇编（RVA 可查 `dump.cs`）才能定「它关的是 `EventSystem` 还是钮」。
2. **`fadeBackground` 我们这棵树上没有、也没有等价物** —— 唯一候选 `Menu Dark Background` 是**点它关窗**的常驻遮罩
   （alpha 0.773 + 自带命中区）：拿它当 `fadeBackground`，`ChangeFrame` 收尾会把它 `SetActive(false)`
   ⇒ 整窗背后不再变暗、玩家也看不到「点空白关窗」那块。⇒ 本件**保持空**（组件 `FadeOutImage` 出声一次）。
   **还差**：原版 `fadeBackground` 那格的图/色（`levelUpEffect1/2` 也是）—— 要么在 `Upgrade Panel` 那棵树上补建，
   要么把 `CardDisplayEffects` 那两件复用到本窗（那是**界面线**的活，需要 Unity 看效果）。
3. **升级 2D cue 的名字** —— 原版 `sound = {m_FileID 12, m_PathID -4696912684080399316}`
   （MB `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2522.json`）；该外部资产在
   `assets_full` 全树**按文件名 0 命中**（不在我们能按 pathID 反查的那批包里）。⇒ 本件留 null + 出声一次。
   **还差**：MB 那 12 号外部文件对应的 bundle（或 AudioCue 名单）。
4. **`m_StopTime: 1` 是不是导出参数写错** —— 原版 JSON 里 `m_MuscleClip.m_StopTime = 1.0`，而 `m_Events` 到 3.4833；
   导入产物的 `m_AnimationClipSettings.m_StopTime` 也是 **1**。**Unity 到底按哪个当 length** 本机没有 Unity ⇒ **未证**。
   🔴 但从「事件全在 3.48–3.92 s」与「StopTime=1」这组矛盾看，**接上去大概率一次事件都收不到**
   ⇒ 这就是本件**没有**接 `Animation` 的直接理由。**还差**：同步点跑一次批处理，读 `clip.length`。
5. **`SceneJumpShot` 那种实况验证没做**（本波禁 Unity）⇒ 「窗抖 20 px 在真机上是什么观感」「1.45 s 换框那一下有没有跳帧」
   全部**未验证**。

---

## 四、改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs` | ① `:1631` 点击闸 ② `:2599` 挂点调用 ③ `:983` 关窗收尾（**三条都是同行替换，0 行数变化**）④ 类尾追加：`LevelUpEffect` / `UpgradeAnimLocked` / `PlayUpgradeAnimation` / `OnLevelUpChangeFrame` / `RewireLevelUpTargets` / `FadeImageOf` / `SettleLevelUpAnimation` / 三段自检（`SelfCheckCardDisplayLevelUp` · `SelfCheckUpgradeAnimationWiring` · `SelfCheckScenarioFaderDoFade`）+ `_warnedNoLevelUpSound` | `452 / 3`（净 +449；2816 → 3265 行） |
| `资料/完全复刻/账/_parts/B9_W25UPGR.tsv` | 账补丁 **6 行**（表头 + 5）：177/178/179/184/185 → 备注追加「挂点已接」+「两条事件仍不可达（clip 无运行时路径 + StopTime=1）」；**状态保持 B**（不改档） | 新建 |
| `_tmp_view/w25upgr_patch.py` | 补丁的**生成脚本**（只读主表；改文案就改脚本里的 `NOTE` 再重跑，⛔ 别手改 `_parts/*.tsv`） | 新建（工具） |
| `资料/完全复刻/报告/B9_W25UPGR_升级动画挂点.md` | 本报告 | 新建 |

**补丁自检**：6 行（表头 + 5）· 每行 **11 字段** · **LF / 0 裸 CR** · 表头与主表逐字相同 · `状态` 列（第 6 列）仍是 `B`
（⚠️ 不是 `等级` 列，见 `报告/B9_W17FX2` §5·5）· `方法` 列带完整 `()` · `工具/check_patch_conflicts.py` ⇒ **没有跨补丁重复键（3 份补丁）**。

### 怎么验（同步点用；本代理**未跑 Unity**）

三段自检都返回 `List<string>`（空 = 全绿），宿主侧 `foreach (var f in …) Check(true, false, f);` 即可：

```csharp
// ① 组件六跳 —— 自带夹具，不依赖任何窗口；建议进 Editor/ShellScene.cs
foreach (var f in CardDetailPopup.SelfCheckCardDisplayLevelUp()) Check(true, false, f);

// ② 本窗挂点 —— 前置：CardProgress.ResetForTest() + 一扇已 ShowCard 的详情窗
foreach (var f in dw.SelfCheckUpgradeAnimationWiring()) Check(true, false, f);

// ③ ScenarioMaterialFader 的 doFade 归零 —— 与卡片详情窗无关（放这里只因为白名单），建议挪进 Editor/BattleScene.cs
foreach (var f in CardDetailPopup.SelfCheckScenarioFaderDoFade()) Check(true, false, f);
```

**灭自证清单（改坏法 ⇒ 必红）**

| 断言 | 改哪里会红 |
|---|---|
| ②`InputBlocked` 在 `DoAnim` 之后必须 **true**（断「锁过」） | 删掉 `DoAnim` 末尾那句 `InputBlocked = true` |
| ②第二次 `DoAnim` 必须**重新上锁** | 把锁改成一次性/常量 |
| ②动画在播 + clip 已挂 ⇒ 点「升级」**不生效** | 删掉 `:1631` 那半个闸 |
| ②`ChangeFrame` 必须**真的触发整窗重建**（`UpgradeHit` 换节点） | 把 `onChangeFrame` 接到空回调 / 只 `RewireLevelUpTargets` |
| ②抖完 `localPosition` 必须回原位 | 删掉 `StepShake` 的还原那一条 |
| ①`FadeOutImage` **只清 α、RGB 不变** | 写成 `color = new Color(0,0,0,0)` |
| ①`OnEnable` 必须 +3 次淡出 | `OnEnable` 不转 `FadeOutImages` |
| ③`DoScenarioBlend` **跑完再调一次 α 必须还会动** | 删掉 `ScenarioMaterialFader.Complete()` 里的 `_doFade = false`（原版 `__Update.c:75`）⇒ 第二次被冻住 |

> 🔴 ③ 的为什么用「α 还会不会动」而不是读 `_doFade`：那个字段是**私有**、没有读数口（`BlendCalls/BlendCallsForward` 只数调用次数）。
> 黑盒版反而更硬 —— 它同时钉住「归零」与「`Advance` 首句闸」两条。

### 🔴 同步点还要做的三件（本件**做不了**，逐条给了判据）

1. **修导出参数**：`m_AnimationClipSettings.m_StopTime = 1` → **≥ 最后一条事件时刻**（`Level Up Card` 3.4834 /
   `Rank Up` 3.9167 / `Division Up` 3.8334）。⚠️ 三条**同一个病** ⇒ 改**生成侧**（不是手改 `.anim`，那是生成物）。
   判据 = `clip.length` 必须 ≥ 最大事件时刻，否则事件永远不到。
2. **给 clip 一条运行时路径**：现在 `Assets/WarpforgeVFX/Animations/` 不在 `Resources/`、也无 prefab 引用。
   最小改法 = 把 `Level Up Card.anim` + `.meta` 复制进 `Assets/CardPresentation/Resources/Art/animations/`
   （先例：`Resources.Load<VideoClip>("Art/videos/intro")`），然后
   `var anim = gameObject.AddComponent<Animation>(); anim.playAutomatically = false; anim.AddClip(clip, clip.name); anim.clip = clip;`
   并把这个 `anim` 赋给 `LevelUpEffect.myAnimation`（**挂在窗根** ⇒ 动画事件正好回调到同一 GameObject 上的
   `ChangeFrame` / `AnimationFinished`）。
3. **接上之后把 `UpgradeAnimLocked` 的硬条件换掉**：`myAnimation != null` → `myAnimation.isPlaying`
   （或「事件真的到过」），并把 `SettleLevelUpAnimation` 里那句「放锁」重核一次 —— 见 `:2853` 的 doc。

---

## 五、顺手发现

1. 🔴 **两处旧记录与事实不符（铁律 5，本件无权重文件的作者文件 ⇒ 只报不改）**：
   * `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs:75`：「`Rank Up` / `Division Up` / `Level Up Card`（**工程里没有，要导入**）」
     —— 现读**三份都在** `Assets/WarpforgeVFX/Animations/`（`m_Legacy: 1`、`.meta` 齐全、事件齐全）。
   * `Effects/CardDisplayLevelUp.cs:69`：「3 条 clip（…）没有导入（本波禁 Unity）」—— 同上。
   ⚠️ 真正缺的不是「文件」，是**运行时可达性**（不在 `Resources/`、无 prefab 引用）+ **`m_StopTime=1` 压死事件**（§二·3）。
   ⇒ 这两处不改，下个会话会照「没有」去**重新导一遍**（白跑一轮 Unity）。
2. 🔴 **`m_StopTime` 那个坑是「导出参数」级的，不是个案**：三条 clip **全部**是 `StopTime=1` + 事件在 3.8–3.9 s。
   凡是**靠动画事件驱动**的 legacy clip（`ChangeFrame` / `AnimationFinished` / `From Remnant Necrons` 那一族）
   都可能被同一支导出器压死。**建议**：在导出器（`WarpforgeArena1/Editor/EffectExporter.cs` 的 clip 落盘那一支 /
   今天 07:00 落这批文件的那支）加一条**自检**：`StopTime >= max(m_Events.time)`，否则 `LogError` 不出包。
3. `Effects/CardDisplayLevelUp.cs` 的 `DoAnim` ⑥ 在 `sound == null` 时**静默跳过**（组件里没有出声分支）⇒
   本件在调用方补了一条「cue 没接」的日志（`Shell/CardDetailPopup.cs:2896`）。若以后有人把 `sound` 接上，记得**删掉那条**（否则会一直响一次）。
4. `SelfCheckScenarioFaderDoFade` 落在 `Shell/CardDetailPopup.cs` **只是白名单的妥协**（`Battle/ScenarioMaterialFader.cs`
   是 `B9_W17FX2` 的产物、`Editor/BattleScene.cs` 归 W24）⇒ 建议调度台把它**挪进 `Editor/BattleScene.cs`** 的
   `A1967 残尾` 那一节旁边（那里已经有同类夹具，见 `Editor/RewardsScene.cs:6833-6868`）。
5. 自检副作用的边界（写进代码注释了，这里再记一笔）：`SelfCheckUpgradeAnimationWiring` 会**真的调 `DoUpgrade()`**
   ⇒ 会写 `CardProgress`（等级）与 `ProfileData`（成就计数）。前者本件自己还原、后者在 `-batchmode` 下
   `SaveIfAuto()` 不落盘（`Shell/ProfileData.cs:1151`）⇒ **只能在批处理宿主里调**，⛔ 别在真 Play 里点触发器。
