# B9_W37CLIP · 「升级动画 clip 播不到事件时刻」结案 + 三条 clip 的运行时路径

> 执行代理 `B9_W37CLIP` · 2026-10-27 · **禁 Unity / 禁 git / 禁改正本**。
> 白名单：`工具/gen_anim_clips.py` · `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` ·
> `CardPresentation/Effects/CardDisplayLevelUp.cs` · `CardPresentation/Shell/CardDetailPopup.cs` ·
> `CardPresentation/Resources/Animations/**`（新建）+ 本报告 + 账补丁 `账/_parts/B9_W37CLIP.tsv`。
> `typecheck`：`TMPDIR=/tmp/wf_w37 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完两轮各跑一次）。

---

## 一、结论

### 1. 争议点裁定 = **(a)**：`m_StopTime` **不是** legacy clip 的运行时 `length` ⇒ **⛔ 不要改它**

> **生成器 `工具/gen_anim_clips.py:13` 的注释（「原版也是 1，⛔ 别改」）是对的；`B9_W25UPGR` 的担心不成立。**

四条独立证据（详见 §二）：

1. **总体分布**：AssetRipper 导出的**原版 74 条 `AnimationClip`** 里，**66 条 `m_Legacy: 1` 全部是
   `m_AnimationClipSettings.m_StopTime: 1.0`** —— 不管曲线只有 0 s 还是 **7.05 s**（`Godspear Warhead`）；
   而 **8 条 `m_Legacy: 0` 才是真值**（0.81666666 / 1.0333 / 2.0 / 10.05 / 11.92 / 11.96 / 17.133）。
   ⇒ 「1.0」是 legacy 的**常量默认值**，不是逐条设的长度。
2. **字段在哪一层**：`AnimationClipSettings` / `stopTime` 在 **`UnityEditor.CoreModule.dll` 里 7 处 / 1 处**，
   在**运行时程序集 `UnityEngine.CoreModule.dll` 里 0 命中** ⇒ 它是编辑器侧（Animation 窗口 / 导入器）的字段。
3. **原版自己依赖 >1 s 的事件**（`d:/2/tools/decomp_full/` 有接收方法体）：
   `RankedEventDivisionChangeWindow__AnimationFinished`（`Rank Up`/`Division Up` 的 **3.9167 / 3.8333 s**）·
   `CardDisplayLevelUpAnimationController__AnimationFinished`（**3.4833 s**）+
   `__ChangeFrame`（**1.45 s**）· `RemnantNecrons__AnimationResurrectionEnd`（**1.8 s**）·
   `SwarmVFXController__OnAnimationEndEvent`（**1.2 s**）。
   ⇒ 若 `StopTime: 1` 真会截断，**原版这些功能一个都不会工作**（残骸体永远回不来、升段窗永远不结束）。
4. **结构上也没有别的时长来源**：legacy clip 的 `m_MuscleClip.m_Clip` 是**空的**
   （`m_FrameCount: 0` / `m_CurveCount: 0` / `m_SampleArray: []` / `m_DenseClip.m_BeginTime: 0`），
   `m_MuscleClip.m_StopTime` 也是 1.0 —— 那份 muscle 数据**根本不带时长** ⇒ `length` 只能来自**曲线最晚键**。

**并且三条升级 clip 的事件点恰好等于片尾**（这正是原版作者的意图，也是「事件能到」的直接判据）：

| clip | 曲线最晚键 | 事件 |
|---|---|---|
| `Level Up Card` | **3.4833333 s**（`2DCard/Glow Explosion`） | `ChangeFrame` @1.4500000 · `AnimationFinished` @**3.4833333** |
| `Rank Up` | **3.9166667 s**（`Text Content`） | `AnimationFinished` @**3.9166667** |
| `Division Up` | **3.8333333 s**（`Text Content`） | `AnimationFinished` @**3.8333333** |

⇒ 口头结论：**W25 的「永远到不了事件时刻」是错的**；事件就在片尾，length 由曲线决定，与 `m_StopTime` 无关。

### 2. 三条 clip 现在**有运行时路径**了

* 生成器改成**按 clip 分别输出**：三条升级 clip 写到
  **`Assets/CardPresentation/Resources/Animations/`**（`工具/gen_anim_clips.py:54` 的 `OUT_RES`、`:60-62`），
  `Booster Window - Background Shake On Open` 留在 `WarpforgeVFX/Animations/`（它是判据，由采样表驱动）。
* 顺带写**确定性 `.meta`**（`md5("WarpforgeAnimClip/" + 名字)`；已有 `.meta` 不覆盖 ⇒ 保 guid），重跑幂等。
* 生成物**逐字节等于**原来那三份（`cmp` 逐条 identical）⇒ 内容没有变化，只是多了一条**运行期可加载**的副本。

### 3. `Animation` 组件已接通（`Level Up Card`）

`CardDetailPopup.PlayUpgradeAnimation` 里新增 `EnsureUpgradeAnimation()`（`:3293`）：
`Resources.Load<AnimationClip>("Animations/Level Up Card")` → 给**窗根**挂/取 `Animation` →
`playAutomatically=false` · `wrapMode=Once` · `AddClip` → **设为默认 clip**（`anim.clip = …`，因为原版 `DoAnim`
是**无参** `myAnimation.Play()`）→ 赋给 `LevelUpEffect.myAnimation`。
⇒ `DoAnim` 第 ② 跳（`myAnimation.Play()`）现在**真播**，两条事件由 legacy `Animation` 的 `SendMessage`
回调到同一 GameObject 上的 `CardDisplayLevelUp.ChangeFrame()` / `.AnimationFinished()`（两个方法都已存在）。

### 4. 输入锁判据换成「真在播」

新增 `CardDisplayLevelUp.AnimationWillRun`（`CardDisplayLevelUp.cs:432`）：
**有 `Animation` 且它挂着真 clip**；真 Play 时还要求 `isPlaying`（编辑器/批处理无帧循环 ⇒ `isPlaying` 恒 false，
那一档按「挂了真 clip」算）。`CardDetailPopup.UpgradeAnimLocked`（`:2853`）从
`myAnimation != null` 改为 `fx.AnimationWillRun`。⇒ 三条路各配断言（自检 §④）。

### 5. 关于「导出器建议自检」

**⛔ 不采纳 `StopTime >= max(m_Events.time)`** —— 按 §一·1 的结论，那个口径是**错的**，会把一个正确值
（原版 66/66 都是 1）判成缺陷、逼人去改它。
**已实现的替代自检**（`工具/gen_anim_clips.py` 的 `--check`，`:258-266`）：
**`max(m_Events[].time) <= max(所有曲线键的 time)`**（+ 打印 `length(曲线最晚键)` 与事件时刻）。
逻辑：legacy 的 length = 曲线最晚键 ⇒ 事件若晚于它，才是真「够不到」。三条都过（事件点 = 片尾）。

---

## 二、证据（文件:行号）

### 2.1 原版侧

| 证据 | 出处 |
|---|---|
| 66/66 条 `m_Legacy: 1` 的 clip `m_StopTime = 1.0`；8 条 `m_Legacy: 0` 才是真值 | `d:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/*.anim`（74 份 `.anim`，逐份读 `m_AnimationClipSettings.m_StopTime` + 曲线 `- time:`） |
| `Godspear Warhead.anim`：`m_Legacy: 1` · `m_StopTime: 1` · 曲线到 **7.05 s** | 同上，`:209-233` |
| `Level Up Card.anim`：`m_StopTime: 1`（`:559-564`）· 事件 `ChangeFrame`@1.45（`:584-585`）+ `AnimationFinished`@3.4833333（`:591-592`） | 同上 |
| `Rank Up.anim`：`m_StopTime: 1`（`:460-465`）· `AnimationFinished`@3.9166667（`:484-486`） | 同上 |
| `Division Up.anim`：`m_StopTime: 1`（`:474-479`）· `AnimationFinished`@3.8333333（`:498-500`） | 同上 |
| legacy 的 muscle 数据是空的（`m_FrameCount: 0` / `m_CurveCount: 0` / `m_SampleArray: []`）+ `m_MuscleClip.m_StopTime = 1.0` | `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`（`m_MuscleClip.m_Clip.data` 现读；三条同形） |
| 原版 `m_AnimationClipSettings` **不在** `assets_full` 的 JSON dump 里（dump 无该键），真值只能从 AssetRipper 导出读 | 同上（`d.keys()` 无 `m_AnimationClipSettings`） |
| `AnimationClipSettings` 只在编辑器程序集 | `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEditor.CoreModule.dll` = 7 处；`UnityEngine.CoreModule.dll` = **0** 处 |
| 原版**依赖** >1 s 的事件（方法体在） | `d:/2/tools/decomp_full/RankedEventDivisionChangeWindow__AnimationFinished.c` · `CardDisplayLevelUpAnimationController__AnimationFinished.c` · `CardDisplayLevelUpAnimationController__ChangeFrame.c` · `RemnantNecrons__AnimationResurrectionEnd.c` · `SwarmVFXController__OnAnimationEndEvent.c` |

### 2.2 我们这侧

| 证据 | 出处 |
|---|---|
| 生成器注释「原版也是 1，⛔ 别改」升级成带证据的版本（+ `m_ScaleCurves` 有意不生成的理由 + 输出目录说明） | `工具/gen_anim_clips.py:9-30` |
| 三条升级 clip → `Resources/Animations/` | `工具/gen_anim_clips.py:54`（`OUT_RES`）· `:60-62` |
| `m_StopTime: 1` 行保留，注释写明「不是 length」 | `工具/gen_anim_clips.py:187` |
| 确定性 `.meta` 生成 | `工具/gen_anim_clips.py:228`（`meta_text`）· `:275-283` |
| `--check` 的新自检（length = 曲线最晚键，事件必须 ≤ 它） | `工具/gen_anim_clips.py:258-266` |
| 运行期加载 + 挂 `Animation` + 设默认 clip | `CardPresentation/Shell/CardDetailPopup.cs:3293`（`EnsureUpgradeAnimation`）· 调用点 `:2883` |
| 锁判据换成「真在播」 | `CardPresentation/Shell/CardDetailPopup.cs:2853-2860`（`UpgradeAnimLocked`）→ `CardPresentation/Effects/CardDisplayLevelUp.cs:432`（`AnimationWillRun`） |
| 自检新增两条（正：真 clip ⇒ 锁 true；反：空壳 ⇒ 锁 false） | `CardPresentation/Shell/CardDetailPopup.cs:3148-3188` |
| 两处过期注释就地订正（零行数变化） | `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs:75` · `CardPresentation/Effects/CardDisplayLevelUp.cs:69` |
| 三条 clip 内容与原先逐字节相同（只是多一条运行期副本） | `cmp` 三条 `Resources/Animations/*.anim` vs `WarpforgeVFX/Animations/*.anim` ⇒ identical |

### 2.3 幂等 / 回归

* `python -I -X utf8 工具/gen_anim_clips.py --write` 连跑 **两遍**：第二遍只重写同样的字节，
  三份 `.meta` 打印「已存在，未覆盖」；`--check` **四条全是「逐字节一致」**。
* `TMPDIR=/tmp/wf_w37 bash 工具/typecheck.sh` ⇒ **0 / 0**（两次）。
* 账落点未漂移：`CardDetailPopup.cs:2867` 仍是 `void PlayUpgradeAnimation(int afterLevel)`
  （`UpgradeAnimLocked` 那段是**等行数改写**：doc 8 行 + 属性 8 行，前后各 16 行；其余新增全在 `:2867` 之后）。

---

## 三、没查清的部分

1. 🔴 **运行时未验（本波禁 Unity）**：`m_StopTime` 不截断的结论是**静态三源合判**（原版导出分布 + 程序集归属 +
   原版代码依赖）得出的，**没有在 Play 模式里实测「事件真的在第 3.4833 s 回调」**。
   唯一残余风险 = Unity legacy 对**恰好落在片尾**的事件是否派发（事件时刻 == length，这是原版自己的写法）。
   已加**兜底**：`AnimationWillRun` 在 `isPlaying` 转 false 时放行输入锁 ⇒ 即使事件漏一拍，
   也不会出现「升级钮永久点不动」的静默缺陷（只是背景淡出/换框那一拍会少一次）。
   ⇒ **建议同步点用一次真 Play 观测**：升级后 `ChangeFrame`（1.45 s，窗抖 + 重建）与 `AnimationFinished`
   （3.4833 s，解锁）各一次，日志/视觉都能看出来。
2. ⚠️ **`m_ScaleCurves` 没生成**（见 §五·1）：三条 clip 的缩放曲线全部缺失。`Rank Up` / `Division Up` 的两条
   绑 `Division Image` / `Rank Icon`（我们树上没有这两个节点）⇒ 天生绑不上、无影响；
   **`Level Up Card` 那条不能照搬**（path 是 `''` = 挂在 clip 自己的根上、值 **250 → 288.98 → 250**，
   照搬会把我们的窗根放大 250 倍）⇒ **怎么映射到我们的节点还没有判据**（原版 `Upgrade Panel` 的节点树与我们不同）。
   ⇒ 这条**没做**，需要「原版节点 ↔ 我们节点」映射表（归卡面/界面线）。
3. ⚠️ **`Rank Up` / `Division Up` 的接线不在本件**（`Shell/RankedDivisionChangeWindow.cs` 归 W28）。
   本件只保证它们**现在取得到**（`Resources/Animations/`），W28 可直接
   `Resources.Load<AnimationClip>("Animations/Rank Up")` / `("Animations/Division Up")`。
4. ⚠️ 原版 `Animation` 组件的 `m_WrapMode` 我**没去 prefab 里现读**（本件按「clip 非循环」设 `WrapMode.Once`）。
   若同步点要逐字段对齐，需读 `scenes_scenes_mainmenuwarpforge` 的 `Upgrade Panel` 那颗 `Animation` 的 `m_WrapMode`。

---

## 四、改动清单

| 文件 | 改动 |
|---|---|
| `工具/gen_anim_clips.py` | ① 文件头写入 (a) 的判据链 + 输出目录说明 + `m_ScaleCurves` 有意不生成的理由；② `OUT_VFX` / `OUT_RES` 分目录，三条升级 clip → `Resources/Animations/`；③ 新增确定性 `.meta` 生成（已存在不覆盖）；④ `--check` 新增「事件 ≤ 曲线最晚键」自检 + 打印 length/事件；⑤ `m_StopTime` 行注释订正；⑥ 写盘前先算 bytes（防截断） |
| `CardPresentation/Resources/Animations/{Level Up Card,Rank Up,Division Up}.anim` + `.meta` | **生成器新产出**（内容与原先三份逐字节相同；`.meta` 是新 guid，`WarpforgeVFX/Animations/` 那三份没有任何 guid 引用 —— 已 grep `.prefab/.unity/.asset/.anim/.controller/.mat` 全仓 0 命中） |
| `CardPresentation/Effects/CardDisplayLevelUp.cs` | ① 末尾新增 `public bool AnimationWillRun`（`🔶 自己设计`，写明裁定人/日期）；② `:69` 过期注释就地订正（**零行数变化**） |
| `CardPresentation/Shell/CardDetailPopup.cs` | ① `UpgradeAnimLocked` 判据 → `AnimationWillRun`（**等行数改写** 16 行）；② `PlayUpgradeAnimation` 里加 `EnsureUpgradeAnimation()`（`:2883`，在 `:2867` 之后 ⇒ 账不漂移）；③ 类尾新增 `UpgradeAnimClipName` / `UpgradeAnimResourcePath` / `_warnedNoUpgradeClip` / `EnsureUpgradeAnimation()`（`:3288-3320`）；④ 自检 ② 的锁断言重写（真 clip 正向 + 空壳反向）；⑤ `:1631` 那句 Debug.Log 的判据文字跟着订正 |
| `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` | `:75` 过期注释就地订正（**零行数变化**） |
| `资料/完全复刻/账/_parts/B9_W37CLIP.tsv` | 账补丁 5 行（177/178/179/184/185）：`备注` 追加本轮结论、`轮次` 追加 `B9_W37CLIP 2026-10-27`；**状态保持 B**（理由从「事件到不了」换成「运行时未验」） |

---

## 五、顺手发现

1. 🔴 **`m_ScaleCurves` 被生成器整批丢掉（三条 clip 都有）**，而它对 `Level Up Card` **不能照搬**：
   `path: ''`（= clip 自己所在的对象）的值是 **250.0 → 288.977/288.98/254.304 → 250.0**
   （原版 `Upgrade Panel` 根的真实 `localScale` 量级）。我们的窗根 scale = 1 ⇒ 照搬会把整个详情窗放大 250 倍。
   **另外两条**绑 `Division Image` / `Division Image/Rank Icon`（我们树上没有）⇒ 绑不上、无副作用。
   ⇒ 已在生成器文件头写死「有意不生成 + 为什么」，并记进 §三·2。**这条要做，但缺节点映射判据**。
2. ⚠️ **`config/卡面/…` 里 `Div Image` 类节点缺失导致 3 条曲线永远不会绑上** —— 与账 84/88 行已记的
   「`Division Image` / `Rank Icon` 我们没画」是同一件事；本件只做加载路径，不碰画面。
3. ⚠️ **`WarpforgeVFX/Animations/` 里的三份旧副本现在成了「生成器不再写、但仍存在」的重复**（内容与
   `Resources/Animations/` 逐字节相同、全仓 0 引用）。本代理**不越白名单**、没删 —— 建议调度台决定：
   ① 删掉旧三份（保留账 84/88 行文档引用的话，改成指 `Resources/` 路径）；② 或留作判据副本并在生成器里注明。
   ⚠️ 留着的唯一风险是「以后有人改一份、另一份不动」，因为 `--check` 现在只覆盖 `Resources/` 那三份。
4. 🔴 **`assets_full` 的 `AnimationClip/*.json` 根本没有 `m_AnimationClipSettings` 这个键**
   （`d.keys()` 现读）；以前几路代理写的「原版 JSON 里 `m_MuscleClip.m_StopTime = 1.0`」是把
   **muscle 的** `m_StopTime` 当成了 **settings 的** `m_StopTime`。两者恰好都是 1.0，所以结论没错，
   但**判据链要说准**：settings 侧的真值只能从 AssetRipper 导出的 `.anim` 读。
5. ✅ **原版 `Level Up Card` 的两条事件在 `d:/2` 与我们生成的 `.anim` 里逐值相同**
   （1.4500000476837158 / 3.4833333492279053），`f()` 的 `%.7g` 没有造成精度漂移（`cmp` 也印证内容未变）。
