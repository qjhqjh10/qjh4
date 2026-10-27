# B9_W66MID —— 段位窗 `Rank Icon` **中段峰值断言**（判别式）+ `CardDetailPopup` 过期注释订正

> 代理 `B9_W66MID`（2026-10-27）· **执行型** · 只做两件：① 给段位窗补**中段峰值断言** ② 订正 `Shell/CardDetailPopup.cs:2826` 的过期注释。
> 判据一手来自 `报告/B9_W60SCALE_段位缩放断言.md`（§三·2 + §五·3）与 `报告/B9_W53DIV_段位clip缩放修正.md` §二那张四键表 —— **本代理没有重查原版、没有再动数据、没有再动生成器**。
> ⛔ 未跑 Unity（`ShellScene.Run` 由调度台在同步点跑）· ⛔ 未动 git · ⛔ 未越白名单（只碰 2 个源文件 + 本报告）。

---

## 一、结论

1. **空档已补**：`Editor/ShellScene.cs` 段位那一节新增一条**中段峰值断言**（`:9546-9592`）：
   把 `Rank Up` 手动推进到**峰值键**（现读 t=**2.5999999 s**，**按 `division_clips.json` 的键读、不写死**），
   断 `Division Image/Rank Icon` 的 `localScale.x` ≈ **2.043**（容忍 0.02）。
2. **它是判别式，不是重复断末帧**：末帧是 **1.0**、峰值是 **2.043** ⇒ **同一个静态值不可能同时是这两个数**。
   ⇒ 「本条绿 **且** 末帧绿」才证明动画**真的在推进**、并且停在正确末帧；单靠末帧证明不了
   （`Animation.Stop()` 若把 `localScale` 复位到出厂 1.0，末帧那条会**碰巧绿**）。
3. **期望值 2.043 是原版判据，⛔ 不是拿本 JSON 的峰值反算**：原版 clip 峰值 `2.2275424` ÷ 原版 prefab 静止 `1.0903292` = **2.0429999**
   （判据 → `报告/B9_W53DIV_段位clip缩放修正.md` §二那张表，四键 `1.0903292 / 2.2275424 / 0.77230197 / 1.0903292`）。
   只有**时刻**从 JSON 读（这是「填点在数据里、值在原版判据里」的分工，避免自证）。
4. **顺手订正了一条会误导的过期注释**：`Shell/CardDetailPopup.cs:2826` 的「3 条 clip 还没导进工程 ⇒ `myAnimation == null`」
   → 拆成**「文件在不在」（都在 `Resources/Animations/`）** 与 **「加载点取不取得到」（只有 `Level Up Card` 有 `Resources.Load` 加载点；`Rank Up`/`Division Up` 没有，段位窗走 `division_clips.json`）** 两件事分开写，并注明「本代理没跑 Unity、加载点未实测」。
5. **账：本波不写补丁**（理由见 §2.6：本次不产生新的「原版类 × 方法 → 我们实现」映射）；**且两处改动都按构造保证账闸零漂移**（已核，见 §2.6）。

**必跑结果**：`TMPDIR=/tmp/wf_w66 bash d:/4/Unity/工具/typecheck.sh` ⇒ **我这两个文件 0 错**（编辑器程序集 **0**；运行时程序集当次残留的是别人在飞的文件，见 §3·4）。

---

## 二、证据（文件:行号）

### 2.1 新增断言（落点 = `Editor/ShellScene.cs` 段位那一节，`winRank` 块内、末帧那一段**之前**）

| 行 | 内容 |
|---|---|
| `:9546-9555` | 10 行判据注释：为什么必须补 · **为什么这是判别式** · 期望值 2.043 的原版出处 · 「⛔ 不拿本 JSON 反向反算」 · 「⛔ 时刻不写死」 |
| `:9556-9564` | 从 `dRank.curves` 扫 `Division Image/Rank Icon` / `localScale.x`，取 **v 最大的那个键** ⇒ `peakT` / `peakV` |
| `:9565-9568` | **前提①**：峰值键读到了（`peakT > 0` 且 `peakV > 1.5`） |
| `:9569-9573` | **数据侧钉一次峰值键** `peakV ≈ 2.043 ± 0.005`（期望值 = 原版 2.2275424 ÷ 1.0903292，⛔ 不是本 JSON 自己的值） |
| `:9574-9578` | **前提②**：峰值那一拍**还在当前进度之后**（`dtToPeak > 0` 且 `IsPlaying`）—— 否则推不到峰值、断言就是空转 |
| `:9579` | `winRank.TickForTest(Mathf.Max(0f, dtToPeak))` —— **手动推**（批处理没有帧循环） |
| `:9580-9581` | **前提③**：`AnimationTime ≈ peakT ± 0.001`（真的推到位了） |
| `:9582-9584` | **前提④**：节点找得到、`NodeScaleForTest` 没退化成 `(0,0,0)` |
| `:9587-9592` | **结论**：`rankIconPeakS.x ≈ 2.043 ± 0.02` |

**断言三问**：

- **断什么**：`Rank Up` 播到**峰值键那一拍**时，`Division Image/Rank Icon` 的 `localScale.x == 2.043 ± 0.02`
  （= 原版静止 1.0 的 2.043 倍；原版绝对峰值 2.2275424 除以原版静止 1.0903292）。
- **改坏哪处会红**：
  ① 删掉 `RankedDivisionChangeWindow.ApplyChannels` 的**缩放段**（`Shell/RankedDivisionChangeWindow.cs:863-874`）⇒ 节点全程停在静态 1.0 ⇒ 峰值读数 1.0 ⇒ **红**（差 1.04 ≫ 0.02）；
  ② 让 `_rt.scales` 不再按 path 写 `localScale`（例如按节点存在性过滤掉 `Rank Icon`）⇒ 同上 ⇒ **红**；
  ③ 撤掉 `工具/gen_division_clips.py` 的 `÷1.0903292`（峰值回 `2.2275`）⇒ **数据侧那条**（`:9571`，差 0.185）**与**本条（差 0.185）一起红；
  ④ **`Animation.Stop()` 把 `localScale` 复位到出厂 1.0** 这一档：本条**仍然走 `ApplyChannels(2.6)`**（`done=false`，`Stop()` 还没被调）⇒ 本条**照常绿**，但末帧那条会变假绿 ⇒ 这正是「两条一起看」的意义（见 §2.4）。
- **是灭自证吗**：**是**。期望值 2.043 来自**原版判据**（原版 clip 键值 ÷ 原版 prefab 静止缩放），**不是**从 `Resources/division_clips.json` 的现值反算；这一点与 `gen_division_clips.py` 断言 ⑥b 的口径一致（那里也明确写了「⛔ 不拿 `RANK_ICON_REST_SCALE` 反算」）。

### 2.2 为什么它会绿（读链逐环核过，与 `B9_W60SCALE` §2.2 同一条链）

1. 数据：`Resources/division_clips.json` → `clips[Rank Up]` → `curves` 索引 3/4/5（`path = "Division Image/Rank Icon"`，`property = localScale.x/.y/.z`），
   四键 `t/v` = `0.38333333/1.0` · **`2.5999999/2.0429999`** · `2.6500001/0.70832` · `2.8666666/1.0`（本代理现读，逐位与 `B9_W60SCALE` §2.2·1 一致）。
2. `Shell/RankedDivisionChangeWindow.cs:672-675` 建 map、`:702-708` 组 `_rt.scales`（**不按节点存在性过滤**）。
3. `TickAnimation`（`:822-857`）是**累加**（`_animT += dt`，`:841`），`done` 时夹到 `_animLen`（`:842-843`）；**未 done** 时 `ApplyChannels(_animT)`（`:847`）
   →`:863-874` 逐 path 写 `n.localScale = (x.Evaluate(t), y.Evaluate(t), z.Evaluate(t))`。
4. 进到本断言的时刻：`TickForTest(PromoteDelay(4) + 0.001f)` = `TickForTest(1.401)` ⇒ promote 触发（`PlayClip("rankUp")` 把 `_animT` 归 0）
   ⇒ 这一拍末尾 `_animT = 1.401`（**< 峰值 2.6**）⇒ `dtToPeak = 1.199` ⇒ 推完 `_animT ≈ 2.6` ⇒ `Evaluate(2.6) = 2.0429999`。
5. 之后既有的 `TickForTest(AnimationLength + 1f)` 从 2.6 一路推到 `_animLen = 3.9166667` ⇒ **末帧那几条断言完全不受影响**（末键 1.0）。

### 2.3 前提硬检查（⛔ 不是 `if (前提) Check(结论)`）

按纪律写成**四条独立断言**（`:9565` / `:9576` / `:9580` / `:9583`），全部**无条件执行**、且都在被断读数之前：

- 峰值键读到了（`peakT > 0 && peakV > 1.5`）—— 数据表坏了先红这条；
- 峰值还在进度之后（`dtToPeak > 0 && IsPlaying`）—— 推不到峰值时**先红这条**，不会让下面的读数变成「拿 1.0 比 2.043」的假红/假绿；
- `AnimationTime ≈ peakT` —— 「推到位了」本身是一条断言；
- 节点与读口没退化（`NodeForTest != null && scale != Vector3.zero`）。

⇒ 前提不成立时**先红前提**；被断读数也照常跑（双重兜底）。⛔ 没有 `if (x != null) Check(...)` 那种静默跳过。

### 2.4 与末帧为什么**结构上互斥** + **同步点怎么判**

- 末帧（`Division Image` 与 `Rank Icon`）= **1.0**（`:9600` / `:9615`，容忍 0.005）；
- 中段峰值（`Rank Icon`）= **2.043**（`:9587`，容忍 0.02）；
- 两个容忍带**相隔 1.04**，**任何单一静态值都不可能同时落在两带里** ⇒ 这两条**不是同一件事的两种写法**。
- **同步点判法（写给调度台）**：
  - **两条都绿** ⇒ 动画**真的在播**（峰值读到 2.043）、且停在正确末帧（1.0）⇒ **不是 `Stop()` 复位造成的假绿**；
  - **峰值绿 + 末帧红** ⇒ 播到峰值没问题，但末键/停帧不对（查数据末键或 `ApplyChannels` 末值）；
  - **峰值红 + 末帧绿** ⇒ **典型的「复位/静态 1.0」假绿**：动画没被驱动（查 `_rt.scales` 是否被过滤、`ApplyChannels` 缩放段是否还在），末帧那条是被静态 1.0 蒙对的；
  - **两条都红** ⇒ 缩放链整段没接上（先看 `LastCurveErrors` 与 `ApplyChannels` 是否被调用）。
- 顺带把 `B9_W60SCALE` §三·2 的悬案（「`Animation.Stop()` 会不会复位」）**变成可判的**：只要出现「峰值红 + 末帧绿」就证明复位存在。

### 2.5 `Shell/CardDetailPopup.cs:2826` 注释订正（**两件事分开写**）

- 改前（3 行）：`🔴 **3 条 clip（…）还没导进工程**（导它要 Unity ⇒ 不在本代理范围）` / `⇒ 现在 myAnimation == null：…` / `（组件 DoAnim 已经为这件事出声一次，不静默）。`
- 改后（5 行，`:2826-2830`）：
  - **① 文件**：三份 `.anim`（`Level Up Card` / `Rank Up` / `Division Up`）**都已在** `Resources/Animations/`（各带 `.meta`，现读目录确认）。
  - **② 加载点**（全仓**唯一一处代码里的** `Resources.Load<AnimationClip>`）：`Level Up Card` → `EnsureUpgradeAnimation()`（本文件 `:3300`，常量 `:3289`）→ `LevelUpEffect.myAnimation`（`:3318`）；
    `Rank Up` / `Division Up` **没有** `Resources.Load` 加载点 —— 段位窗 `Shell/RankedDivisionChangeWindow.cs` 走 `division_clips.json`（曲线数据），**不经这两份 `.anim`**。
  - **③ 结论**：`myAnimation == null` **只在 `Resources.Load` 取不到时**成立（`:3301-3311` 会出声并把 `myAnimation` 置 null）；**「文件在」≠「加载点取得到」，本代理没跑 Unity、这一条未实测**。
- 事实核过（不是照搬结论）：
  · `ls Resources/Animations/` 现读 = `Level Up Card.anim` / `Rank Up.anim` / `Division Up.anim`（+ 各自 `.meta`）；
  · `grep -rn "Resources.Load<AnimationClip>" --include=*.cs MyGame/Assets` 现读 = **2 命中**，其中 `Shell/BoosterPackOpenWindow.cs:1283` **是注释里提到这个名字**，真代码只有 `CardDetailPopup.cs:3300`；
  · `grep -rn "Animations/Rank Up|Animations/Division Up"` 现读 = **0 命中** ⇒ 那两份 `.anim` 确实**没有**加载点（与 `B9_W53DIV` §六·4「死资产」一致）。

### 2.6 账：为什么不写补丁 + 为什么**零漂移**

- `工具/check_ledger_points.py` 只取 `状态` 与 **`我们的落点`** 两列。本代理现扫 `资料/完全复刻/账/*.tsv`：
  - **`Editor/ShellScene.cs` 在 `我们的落点` 列 = 0 命中**（16 处提及全在**备注列**）⇒ 插 48 行**不可能**产生漂移。
  - **`Shell/CardDetailPopup.cs` 在 `我们的落点` 列有 30+ 命中，但最大行号 = `:2788`**（`未分类_其他.tsv:116` 的 `ResetDisplayOrder`）。本代理的改动在 **`:2826`**（`2788` 之下方）⇒ **所有落点行号不受影响**（现读 `:2788` 仍是 `public void ResetDisplayOrder()`，`:2626` `RebuildKeepingState`，`:2564` `DoUpgrade` —— 均未位移）。
- **不写 `账/_parts/B9_W66MID.tsv`**：本次是「补一条自检 + 一句注释」，**不新增「原版类 × 方法 → 我们实现」映射**；写一行没有类×方法语义的补丁只会制造补丁冲突/漂移。先例 = `B9_W60SCALE` §2.4、`B9_W53DIV` §2.4。
- ⚠️ **备注列那 16 个 `Editor/ShellScene.cs:NNNN` 指针**（`菜单_外壳.tsv` 14 处 + `其他_基础设施.tsv:35` + `未分类_其他.tsv:583`）**在我动手前就早已过期**，本次插 48 行让 `> 9544` 的指针再 +48（见 §四·3 的对照表）—— 它们**不是闸扫的列**，要修只能人工按**符号名**重定位，⛔ 别机械平移。

### 2.7 必跑（typecheck）

`TMPDIR=/tmp/wf_w66 bash d:/4/Unity/工具/typecheck.sh`，连跑 4 次（为躲开别人在飞的半成品）：

| 次 | 运行时程序集 | 编辑器程序集 | 我这两个文件 |
|---|---|---|---|
| 1 | 0 | 2（`WarpforgeArena1/Editor/ArenaBuilder.cs` —— W59 在飞） | 0 命中 |
| 2 | 1（`Shell/InboxWindow.cs` —— W63 在飞） | **0** | 0 命中 |
| 3 | 3（`Deck/DeckRuntime.cs` × 3 —— W63 在飞） | **0** | 0 命中 |
| 4 | 2（`Shell/CollectionWindow.cs` × 2 —— W63 在飞） | **0** | 0 命中 |

⇒ **`Editor/ShellScene.cs` 在编辑器程序集、连续 3 次 `0` 错**；`Shell/CardDetailPopup.cs`（运行时程序集）**从未出现在错误列表里**。
残留错误**全部**落在 `CLAUDE.md` 13·3 点名的「别人正在写」的三个文件上（W59 的 `Assets/WarpforgeArena1/**`、W63 的 `InboxWindow.cs`/`DeckRuntime.cs`/`CollectionWindow.cs`）—— 按纪律**不是我改**。

---

## 三、没查清的部分（⛔ 不猜）

1. **没跑 Unity ⇒ 没亲眼看见这两条变绿**（本代理不许跑批处理；`ShellScene.Run` 归调度台同步点）。上面「会绿」是**读链推理**（四键值现读 + `_animT += dt` 累加语义 + `ApplyChannels` 逐帧写 `localScale` + 节点路径可解析），**不是实测**。
2. **`Resources.Load<AnimationClip>("Animations/Level Up Card")` 是否真能取到**（注释②里已如实标「未实测」）：文件在、加载点也在，但 `.anim` 走 `Resources.Load` 的**运行期可加载性**只有跑一次 Unity/真机才能证。若取不到，`:3301-3311` 会**出声**并把 `myAnimation` 置 null（不静默）。
3. **`Animation.Stop()` 在当前 Unity 版本对 `localScale` 到底动不动** —— 仍**未实测**；但本波之后它**变成可判的**（判法见 §2.4）。
4. **`Rank Icon` 的 `m_Sprite = 0`（服务端给）** ⇒ 真机上肉眼只能看子件 `Rank Icon Glow` 的缩放；本条断的是**节点缩放**，与有没有图无关。

---

## 四、改动清单

| # | 文件 | 改动 | 行数 |
|---|---|---|---|
| 1 | `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` | 段位那一节新增**中段峰值断言**：10 行判据注释 + JSON 峰值键扫描 + 4 条前提 + 1 条数据侧钉值 + 1 条结论（`:9546-9592`） | 10,665 → **10,713**（**+48**，纯 LF / CRLF 0 / 裸 CR 0） |
| 2 | `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs` | `:2826` 过期注释订正（拆成「文件在不在」/「加载点取不取得到」/「`myAnimation == null` 何时成立」） | 3,321 → **3,323**（**+2**，纯 LF / CRLF 0 / 裸 CR 0） |
| 3 | `d:/4/Unity/资料/完全复刻/报告/B9_W66MID_段位中段断言.md` | 本报告 | 新建 |

⛔ 没有动：`Resources/division_clips.json` 与 `数据/游戏数据/division_clips.json`（生成物）· `工具/gen_division_clips.py` · `Shell/RankedDivisionChangeWindow.cs`（只读它的 API/动画链）· 任何 `.unity` / `ProjectSettings` · 任何别的代理正在写的文件。

**§四·3 备注列指针的机械位移（仅记录，⛔ 别照它平移 —— 它们在插行前就已过期）**：`> 9544` 的 `+48`：

| 账文件 | 旧 | 新（+48） |
|---|---|---|
| 菜单_外壳.tsv（782） | `:10218` | `:10266` |
| 菜单_外壳.tsv（781） | `:10220` | `:10268` |
| 菜单_外壳.tsv（780） | `:10218` | `:10266` |
| 菜单_外壳.tsv（779） | `:10121-10130` | `:10169-10178` |
| 菜单_外壳.tsv（777） | `:10131-10132` | `:10179-10180` |
| 菜单_外壳.tsv（774） | `:10113-10115` | `:10161-10163` |
| 菜单_外壳.tsv（773） | `:10111-10115` | `:10159-10163` |
| 菜单_外壳.tsv（772） | `:10109` | `:10157` |
| 菜单_外壳.tsv（671） | `:10334-10337` | `:10382-10385` |
| 菜单_外壳.tsv（669） | `:10338-10340` | `:10386-10388` |
| 菜单_外壳.tsv（667） | `:10332-10333` | `:10380-10381` |
| 菜单_外壳.tsv（322） | `:10303` | `:10351` |
| 菜单_外壳.tsv（321） | `:10302` | `:10350` |
| 菜单_外壳.tsv（314） | `:10304-10306` | `:10352-10354` |
| 其他_基础设施.tsv（35） | `:7019` | `:7019`（未变，< 9544） |
| 未分类_其他.tsv（583） | `:7205` | `:7205`（未变，< 9544） |

---

## 五、顺手发现（⛔ 本代理没改，交调度台分流）

1. **`Effects/CardDisplayLevelUp.cs` 文件头有同一族过期说法（不在本代理白名单 ⇒ 只报不改）**：
   `:47` 仍写「把 **3 条 clip 导进工程后**挂 `Animation` 即可（… 本波禁 Unity ⇒ 本代理**没有**导入）」，`:41` 的 `dump.cs` 描述里也只提 `Level Up Card`。
   ✅ **但 `:69` 已有订正行**（`~~3 条 clip 没有导入~~ ⇒ 订正 2026-10-27（B9_W37CLIP）`：三条已在 `Resources/Animations/`，`Level Up Card` 由 `CardDetailPopup` 接线播放）⇒ **`:47` 与 `:69` 现在并存且口径不一致**（铁律 5：两份说法打架比没有更糟）。建议下一路把 `:47` 那句改成「（订正见 :69；⚠️ `Rank Up`/`Division Up` 无加载点）」。
2. **`Shell/BoosterPackOpenWindow.cs:1283` 也提到 `Resources.Load<AnimationClip>`，但那是注释**（那条注释在讲「取不到 ⇒ legacy clip 的 MonoBehaviour 曲线只有 Animation 能驱动」）。
   ⚠️ 我已在 `CardDetailPopup.cs:2828` 里把话说准（「全仓唯一一处**代码里的**」）—— 以后若有人用 `grep` 数这个 API，**别把注释命中当成第二处加载点**。
3. **`Assets/WarpforgeArena1/Editor/ArenaBuilder.cs:5262/5270` 的 `TextureImporter.spriteMeshType` CS1061**（W59 在飞时的瞬时错）—— 本代理不碰；记一笔是提醒**同步点若还红，先看这份，不是 `ShellScene`**。
4. **`ShellScene.Run` 的断言条数会 +6**（我新加 6 条 `Check*`：2 条数据侧 + 4 条前提 + 1 条结论中的 `CheckNear(AnimationTime)`；合计 **6**）⇒ 同步点对「断言合计」时**记得这一档多了 6 条**，别拿旧合计当基线。

---

### 交接给下一个代理（若本波被截断）

- 已完成：① 中段峰值断言（`ShellScene.cs:9546-9592`）② 注释订正（`CardDetailPopup.cs:2826-2830`）③ 类型检查（我这两个文件 0 错）④ 行尾未翻（两文件纯 LF / 裸 CR 0）⑤ 账闸零漂移（按构造证明 + 现读复核）。
- 唯一我该做但做不了的：**跑 `ShellScene.Run` 看这两条是否绿**（归调度台同步点；本代理不许跑批处理）。
- 复跑命令：`TMPDIR=/tmp/wf_w66 bash d:/4/Unity/工具/typecheck.sh`（我这两个文件应 0 错；运行时残留若仍报 `Hook/UnhookWildcardUpdate` ⇒ 那是 W63 在飞，不是本波）。
- 判绿口径：**峰值 2.043 绿 + 末帧 1.0 绿 ⇒ 动画真在播，不是 `Stop()` 复位**（见 §2.4 的四象限判法）。
