# `Booster动画地雷` —— A1101（`WarpforgeBooster` 里 7 处 legacy `Animation` 引用 guid 全 0，【已修】）

> 本件 = **一件**。在 `BoosterPackExporter.Export()` 里**照 `EffectExporter.cs:1203-1268` 逐行补了同一支**
> legacy `Animation` 的「接回引用」那一跳，把 `Booster Pack Open Window.prefab` 的 **7 处**接回第 ① 步
> 已经落好的 7 份 `.anim`。
> **本次没跑 Unity**（本轮红线：待办没做完不跑自检）· **没动 git** · **没改正本** · **没动 `d:/2`**（只读引用）· 行尾逐个数过。

---

## ① 结论

- **✅ 已做**：`BoosterPackExporter.cs` 的 `Export()` 在 binder 之前（**新 `:403-475`**；头注 `:45-54`）
  补了一支 `foreach (var an in inst.GetComponentsInChildren<Animation>(true))`：
  `GetAnimationClips(go)` + `an.clip` → `EffectExporter.ImportClip` 逐条落盘 → `SetAnimationClips` +
  `an.clip` **接回引用** → 接不上就**点名出声、留空**，⛔ **没造任何空 clip**。
- **根因（现读复核过，与简报一致）**：本文件第 ① 步（`:238-262`）**只把 8 条 clip 落成 `.anim`**，
  第 ③ 步导窗口 prefab 时**没有任何一步把引用接回去** —— 本文件里 `grep -n "Animation"` 的命中只有三处：
  文件头注释（`:2` / `:7` / `:10` / `:17` / `:25`）、`ClipNames` 的判据注释（`:113-127`）、
  第 ① 步的 clip 导入（`:238-262`）—— **没有任何一处写 `Animation` 组件、也没有任何一处接它的引用**。
- **✅ 与 A1095 不同：这 7 处的目标 `.anim` 工程里【是有的】** —— 7 处的 `fileID` **就是原版包里的 pathID**，
  逐个反查到 `assets_full/bundle_menus_assets_all/AnimationClip/AnimationClip_<pid>.json` 的 `m_Name`，
  **7/7 逐字命中第 ① 步 `ClipNames`（`:128-138`）那 8 条里的 7 条**（8 条里唯一没被用到的是 `OpenCardbacks`）
  ⇒ `ImportClip` 走的都是**原地覆盖**那一支（`EffectExporter.cs:2374-2381`，**guid 不变**），
  接回去之后引用的正是 2026-10-03 10:18 那批落好的 `.anim`。**没造空 clip、没手改 prefab。**
- **⚠️ 最大的未验证面**：本轮不跑 Unity ⇒ **新代码一次都没被执行过**（只在编译层面 0 错）。见 §⑥·1。

---

## ② 7 处清单（逐条 · 现读 `Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab`）

**怎么扫的**：python 二进制读，按 `--- !u!111 &<pid>` 切块 → `m_GameObject` → 回查 `--- !u!1` 的 `m_Name`；
`m_Animation` / `m_Animations[]` 里的 `fileID` **就是原版包里的 pathID** ⇒ 直接对
`assets_full/bundle_menus_assets_all/AnimationClip/AnimationClip_<fileID>.json` 的 `m_Name`。

| # | `Animation` 组件（pathID） | 所在 GameObject | prefab 内行号 | `m_Animation`（默认那条） | `m_Animations[]` 条数 | 每条的 fileID → **应指向的 `.anim`** |
|---|---|---|---|---|---|---|
| 1 | `6227921045116631228` | `CardInBoosterPack UI 1` | **3334** | `-7993935874865337289` | **4** | `-7993935874865337289` → `Booster Opening - Card Idle`（默认）<br>`8301228418192668656` → `Booster Opening - Card Open Normal`<br>`-9033435933554767437` → `Booster Opening - Card Open Rare`<br>`-1691631367813043119` → `Booster Opening - Card Open Legendary` |
| 2 | `1417448902387051396` | `CardInBoosterPack UI 2` | **7852** | 同上 | **4** | 同 #1（4 条） |
| 3 | `4146511013888212702` | `CardInBoosterPack UI 3` | **16848** | 同上 | **4** | 同 #1（4 条） |
| 4 | `266096832677092066` | `Booster pack Background` | **24826** | `-177604310092043705` | **1** | `-177604310092043705` → `Booster Window - Background Shake On Open`（默认） |
| 5 | `1618701548568947352` | `Booster Pack Open Window` | **29978** | `5911697182262325120` | **2** | `5911697182262325120` → `Booster Window Open`（默认）<br>`-8254079481253810722` → `Booster Window Close` |
| 6 | `3249542190549912323` | `CardInBoosterPack UI 5` | **33061** | `-7993935874865337289` | **4** | 同 #1（4 条） |
| 7 | `6428443356024140197` | `CardInBoosterPack UI 4` | **33248** | 同上 | **4** | 同 #1（4 条） |

**TOTAL = 7 处 / 7 个不同片段 / 30 条 guid 全 0 的引用**（每处 = `m_Animation` 1 条 + `m_Animations[]` N 条）。
**7/7 组件、7/7 片段的 guid 都是 `00000000000000000000000000000000`、`type: 0`**（现读逐条核过）。

### 配对关系是**怎么核出来的**（两条独立判据，都指向同一张表）

1. **判据 A（首选，机器可判）：`fileID` = 原版 pathID ⇒ 反查包里的 `m_Name`。**
   `bundle_menus_assets_all/AnimationClip/AnimationClip_<pid>.json` **7/7 都存在**，`m_Name` 逐条是：
   `Booster Opening - Card Idle`(wrap 2) · `… Card Open Normal`(0) · `… Card Open Rare`(0) ·
   `… Card Open Legendary`(0) · `Booster Window - Background Shake On Open`(0) · `Booster Window Open`(0) ·
   `Booster Window Close`(0)。这三个「Normal/Rare/Legendary」与 `Rare/…` 的名字**逐字**对得上本文件
   `ClipNames` 里那三条 ⇒ **不是按顺序猜的，是反查出来的**。
   ⚠️ 本判据**只对上了「名字」**，**没告诉你哪条挂在哪个 GO 上** —— 所以还要判据 B。
2. **判据 B（交叉印证，来自本文件自己的头注）：`资料/阶段二_商店_原版规格.md` §五·三 记的 MB 字段**
   （本文件 `:114-122` 转抄）：`windowAnimation`（`Booster Pack Open Window` GO 上的 `Animation`）的
   `m_Animations` = **[Booster Window Open, Booster Window Close]**；`backgroundAnimation`
   （`Booster pack Background` 上的） = **[Booster Window - Background Shake On Open]**；
   `CardInBoosterPack.cardAnimation` 的 = **[Card Idle, 8301228418192668656, Card Open Rare, Card Open Legendary]**，
   且 `8301228418192668656` 与 `contentByRarities[0].animationClip` 同 pid = `Card Open Normal`。
   ⇒ **与判据 A + 上表的 GO 名字严丝合缝**：#5 恰好 2 条、#4 恰好 1 条、5 个 `CardInBoosterPack UI *` 恰好各 4 条。
   **两条判据独立（一条读包、一条读 MB 字段），结论一致** ⇒ 配对成立。
3. **反证（说明导出那一刻引用是【活的】）**：guid 全 0 但 **fileID 是原版的真 pathID**
   —— 判据是**同一份文件里的对照**：Unity 对**空引用**写的是 `{fileID: 0}`，
   而这份 prefab 里这样的空引用有 **4739 条**（`m_Material` 187 · `m_Icon` 313 · `m_fontMaterial` 127 · …），
   并且 **`m_Animation: {fileID: 0}` 在整份文件里是 【0】 条**（现读数过）——
   也就是说「`Animation` 这一格没接片段」在这份 prefab 里**根本不存在**，
   7 格**全部**写的是**真 fileID**。⇒ 那一刻 `an.clip` / `m_Animations[i]` **指向的是真对象**，
   **只是 guid 为空、落不进工程**。
   （同族旁证：`WarpforgeVFX/Prefabs/` 里有 4 件 `m_Animation: {fileID: 0}` —— 那 4 件是**原版本来就没接**，
   见 A1095 报告 §② 第 3 行；**本件这份 prefab 一条都不是**。）
   ⇒ 这正是「补一支 `ImportClip` + 接回」能修好的**前提**（与 A1095 §④ 同一逻辑）。

### 反向清单：7 个片段 → 哪些组件在用它们（现读逐数）

| 片段 fileID | `m_Name` | 被几个组件引用（默认 + 数组） |
|---|---|---|
| `-7993935874865337289` | `Booster Opening - Card Idle` | **10**（5 个 UI × [默认 1 + 数组 1]） |
| `8301228418192668656` | `Booster Opening - Card Open Normal` | **5**（5 个 UI 的 `m_Animations[1]`） |
| `-9033435933554767437` | `Booster Opening - Card Open Rare` | **5**（同上 `[2]`） |
| `-1691631367813043119` | `Booster Opening - Card Open Legendary` | **5**（同上 `[3]`） |
| `-177604310092043705` | `Booster Window - Background Shake On Open` | **2**（1 默认 + 1 数组） |
| `5911697182262325120` | `Booster Window Open` | **2**（1 默认 + 1 数组） |
| `-8254079481253810722` | `Booster Window Close` | **1**（仅数组） |

合计 **30**（= §⑧ 收口判据要盯的那个数）。

### 目标 `.anim`（第 ① 步已落、guid 稳定、**本件没碰**）

| 片段 | `.anim` 路径 | 字节 | md5(前12) | mtime | `.meta` guid |
|---|---|---|---|---|---|
| `Booster Window Open` | `WarpforgeVFX/Animations/Booster Window Open.anim` | 26668 | `71329fe2d89a` | 2026-10-03 10:18 | `4707992a0661d3843a41c1b54c392338` |
| `Booster Window Close` | `…/Booster Window Close.anim` | 21676 | `06a12fe05ab1` | 2026-10-03 10:18 | `c8438056df7a10e48a3a7df7870fa9d7` |
| `Booster Window - Background Shake On Open` | `…/Booster Window - Background Shake On Open.anim` | 3483 | `543f299978ed` | 2026-10-03 10:18 | `097e39b1fc025654aaf15a174ee36e22` |
| `Booster Opening - Card Idle` | `…/Booster Opening - Card Idle.anim` | 247962 | `0bc06ddb05d2` | 2026-10-03 10:18 | `c0189699598d48d4c8064a8b2fa55749` |
| `Booster Opening - Card Open Normal` | `…/Booster Opening - Card Open Normal.anim` | 22632 | `7b6953a5c0b7` | 2026-10-03 10:18 | `8404e0135237b084ab4d652aaca8e34f` |
| `Booster Opening - Card Open Rare` | `…/Booster Opening - Card Open Rare.anim` | 37534 | `ffd03138f2fa` | 2026-10-03 10:18 | `ab77a28cf182e104385ec838d189615f` |
| `Booster Opening - Card Open Legendary` | `…/Booster Opening - Card Open Legendary.anim` | 45272 | `64483eb53388` | 2026-10-03 10:18 | `1cd7c7aa5b0d6bb48955abb0252915a6` |

⚠️ 名字全部**含空格与连字符**，但 `EffectExporter.Sanitize` 只换 `Path.GetInvalidFileNameChars()`
（Windows 上是 `<>:"/\|?*` + 控制字符）⇒ **这 7 个名字过 `Sanitize` 不变** ⇒
`ImportClip` 拼出的 `{AnimDir}/{name}.anim` **逐字命中上表路径** ⇒ 走**原地覆盖**（`EffectExporter.cs:2374-2381`）。

---

## ③ 改动清单

**只改了 1 个文件：`Unity/MyGame/Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs`（**+83 行 / 0 删除**）**

| 位置（改后行号） | 改前 | 改后 |
|---|---|---|
| `:45-54` | （无） | 文件头新增一节「🆕 2026-10-09（A1101）动画那一跳」（**10 行**）：漏在哪 / 与 A1095 的关系 / 与 A1095 的差别 / 指针到本报告 |
| `:403-475` | （无） | 新增一支（**73 行**）：**头注 `:403-435`**（33 行：根因 / 7 处 / 7 个 fileID→名字的**完整对照表**（写在注释里）/ 两条入口 / 为什么不传 `loop` / 零幻觉兜底）+ **主体 `:436-475`**（40 行：`foreach (GetComponentsInChildren<Animation>(true))` → `ImportClip` → `SetAnimationClips` + `an.clip` → 出声） |
| （原 `:393` 起）| `var binder = inst.GetComponent<WarpforgeEffectBinder>();` | 被推下去到 **`:476`**（铁律 5：就地改掉；**本件只新增、没有一行被改写**） |

**净改动**：`+83 行 / 0 删除 / 1 个文件`（`git diff --numstat` 实测 `83 0`）。
**没动**：`EffectExporter.cs`（**只读**，别人的已交件）· `WarpforgeVFX/**` · `WarpforgeBooster/**`（**本轮一个产物都没落**）·
别的任何文件 · `.gitignore` · `项目任务.md` / `CLAUDE.md`。

---

## ④ 证据：与 `EffectExporter.cs:1203-1268` 的**逐行对应**（同一形状）

| `EffectExporter`（A1095，`:1203-1268`） | 本件（`BoosterPackExporter.cs:403-475`） | 说明 |
|---|---|---|
| `:1203-1230` 头注 28 行 | `:403-435` 头注 33 行 | 都是「根因 / 实测规模 / 两条入口 / 不传 `loop` / 零幻觉」五段。本件多一段**7 个 fileID→片段名的对照表**（因为本件规模小、且配对要留证据） |
| `:1231` `int clipOk = 0, clipMiss = 0;` | `:436` 同 | **逐字相同** |
| `:1232` `foreach (var an in inst.GetComponentsInChildren<Animation>(true))` | `:437` 同 | **逐字相同**（`true` = 含未激活；本件 7 个 `m_Enabled` 实测全 = 1） |
| `:1234-1237` `var clips = AnimationUtility.GetAnimationClips(an.gameObject);` | `:439-442` 同 | **逐字相同**（含「`GetAnimationClips(Animation)` 那个重载已 obsolete」那段注） |
| `:1238` `var def = an.clip;` | `:443` 同 | **逐字相同**（含「先取下来」那句） |
| `:1239-1256` `if (clips != null && clips.Length > 0) { … }` | `:444-460` 同 | 形状相同；**本件把 `ImportClip` 写成 `EffectExporter.ImportClip`**（跨类调用，A1095 在本类内直接调） |
| `:1244` `if (clips[i] == null) continue;` | `:449` 同 | 空槽原样留空（本件 7 个组件**实测没有空槽**，这条是防御） |
| `:1245` `var a = ImportClip(clips[i]);` | `:450` `var a = EffectExporter.ImportClip(clips[i]);` | 同一个落盘套路（**不传 `loop`**，走默认 `null`） |
| `:1248-1251` `Debug.LogWarning(EX1 + …)` | `:451-457` `Debug.LogWarning(P + …)` | 同一格式；**本件改用本文件自己的输出前缀 `P = "BP "`**（`EX1` 是 `EffectExporter` 的 `private const`，跨类取不到） |
| `:1256` `AnimationUtility.SetAnimationClips(an, imported);` | `:461` 同 | **逐字相同** |
| `:1258-1263` `if (def != null) { var d = ImportClip(def); if (d != null) an.clip = d; }` | `:463-470` 同 | **逐字相同**；含「数组之后才写默认」那段顺序注释 |
| `:1266-1268` `if (clipOk > 0 \|\| clipMiss > 0) Debug.Log(EX1 + …)` | `:472-474` 同 | 同上，前缀换成 `P`、代号换成 `A1101` |
| `:1270` 之后 = 挂 binder | `:476` 之后 = 挂 binder | **插在同一处**（binder 之前） |
| （A1095 那支的 `origController` 记账） | —— | **本件不需要**：`BoosterPackExporter` 里 `binder.animatorController` 是**硬写死 `""`**（`:476` 往下第 5 行），没有消费方 |

**四处「有意的不一样」（与 A1095 完全一致，本件逐条保留）**：
1. **不传 `loop`**（`ImportClip(c)` 走默认 `null`）—— clip 自己的 `m_WrapMode` / `AnimationClipSettings`
   随 `Instantiate` 一起过来。**本件反证**：实测这 **7 条片段**的 `m_WrapMode` = `2/0/0/0/0/0/0`
   （`Card Idle` 那条是 `2` = Loop，其余 6 条 `0`）—— **彼此不同** ⇒ 更不该统一设一个值。
2. **用非 obsolete 的重载**（`GameObject` 那个）。
3. **两条出口都写**（`m_Animations[]` + `m_Animation`）—— 本件有活证据，见 §② 的「`m_Animations[]` 条数」列（4 / 4 / 4 / 1 / 2 / 4 / 4）。
4. **零幻觉兜底**：导不出来**留空 + 出声**，⛔ 绝不 `CreateAsset` 空 clip。

---

## ⑤ 验证

**秒级类型检查**（`TMPDIR=/tmp/wf_boost bash d:/4/Unity/工具/typecheck.sh`）—— 跑了 **4 次**：
```
第1次（补完主体后）：运行时错误数: 0 / 编辑器错误数: 1  ← RuleEngine/Editor/RuleEngineTest.cs(284,14) CS0103 TestDodgeKeyword
第2次（隔 50 秒）：  运行时错误数: 0 / 编辑器错误数: 0
第3次（改完 m_WrapMode 那条注释）：运行时错误数: 0 / 编辑器错误数: 0
第4次（改完两条行号引用 · 最终）：运行时错误数: 0 / 编辑器错误数: 0
```
⚠️ **第 1 次那条错不在我的文件上** —— `Assets/RuleEngine/Editor/RuleEngineTest.cs(284,14): error CS0103: 当前上下文中不存在名称"TestDodgeKeyword"。`
那是**另一个写手正在写的 `RuleEngine/**`（`dodge`，简报点名在飞）** 的半成品，隔一分钟重跑即 0 ⇒
**不是本件的错，我没有去改别人的文件**（照简报那条已知纪律办）。
✅ **另核「我的文件真被编进去了没有」**：`/tmp/wf_boost/wf_csc_editor.rsp` 里**有**
`"D:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs"` ⇒ **不是「没编所以 0 错」**。

**行尾**（二进制读，改前 → 改后；⛔ 没用 `sed -i`、⛔ 没用 python 文本模式写）

| 文件 | 改前 CRLF / LF | 改后 CRLF / LF | 改后字节 | md5 |
|---|---|---|---|---|
| `BoosterPackExporter.cs` | **0 / 462**（纯 LF） | **0 / 545**（+83 行整行新增，**LF 未翻**） | 26514 → **34046** | `d0fb3c6a7d6b7ad17be8388685e5486d` |

**`git diff --numstat`**（改完立刻跑）：`83  0  Unity/MyGame/Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs`
⇒ **纯新增、0 删除**，**没有全文件重写**（+83 不是 462）。

**⚠️ 被 ignore 挡住、`git status` 看不见的产物**（本件**一个都没落**，下表是**现有**状态 + 判据）
`git check-ignore -v` 逐条核过：

| 路径 | 命中哪条 ignore | 说明 |
|---|---|---|
| `Assets/WarpforgeVFX/Animations/*.anim`（7 份目标 + 另 3 份） | **`.gitignore:26: /Unity/MyGame/Assets/WarpforgeVFX/*`** | 第 ① 步的产物；本件**只读不改**（mtime 仍是 2026-10-03 10:18） |
| `Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab` | **`.gitignore:184: /Unity/MyGame/Assets/WarpforgeBooster/`** 🆕 | 🔴 **简报只提了 `.gitignore:26`** —— 现读**本目录另有自己的一条**（见 §⑦·1） |

**⇒ 本件没有任何产物落在被 ignore 的目录里**：唯一的改动是 `WarpforgeArena1/Editor/BoosterPackExporter.cs`，
它 `git check-ignore` 报 **NOT ignored**、`git status` 报 **` M`**（正常可见）。

---

## ⑥ 没查清 / 停手的

### 1. 🔴 本件的新代码**一次都没被执行过**（最大的未验证面）

本轮不跑 Unity ⇒ 新那一支只在**编译**层面验过（0 错）。两个**只有跑起来才知道**的点：

1. **导出那一刻 `an.clip` / `GetAnimationClips(go)` 真取得到吗？**
   旁证很硬（**不是猜**）：现读 prefab 里这 30 条引用**写的是原版真 pathID**，
   而**同一份文件**里空引用一律写 `{fileID: 0}`（4739 条）；**`m_Animation: {fileID: 0}` 一条都没有**
   ⇒ 说明**上一次导出时这些引用是活的**。**但我没在实况里量过**。
2. **`AnimationUtility.SetAnimationClips` 落出来的 `m_Animations` 形状**对不对
   —— 尤其是 **5 个 UI 组件的 4 元素表**与 **`Booster Pack Open Window` 的 2 元素表**
   （A1095 §⑥·1 第 2 条也把同类问题列成未验证面）。
3. **`ImportClip` 的原地覆盖这一支对这 7 份**是**第一次**真跑
   —— 那 7 份 `.anim` 是 2026-10-03 由 `CreateAsset` **新建**的，`CopySerialized(existing)` 那一支
   在本目录**还没实测过**。理论依据：`CopySerialized` 只写资产内容、**不碰 `.meta`** ⇒ guid 不变；
   且全仓别处同函数在用。**仍是未验证面。**

**怎么收口**：见 §⑧。判绿红**只看动画那 30 条**，⛔ **别拿「全文件 guid 0 → 0」当判据**（**做不到**，见 §⑦·2）。

### 2. 没查的

- **`Booster Pack Open Window.prefab` 的另外 320 条 guid 全 0** —— 报在 §⑦·2，**没修**（**不在本件范围**，
  且它是另一族缺陷：`Export()` 缺 `TextMeshProUGUI` / uGUI `Image` 的引用那一跳）。
- **那 4 个粒子 prefab 有没有别的 guid 0** —— 现读核过：`!u!111 = 0` · `!u!95 = 0` · **`guid 0 = 0`**（干净）。
- 原版 `menus_assets_all` 里另 4 条 clip（`Division Config` / `SearchingOpponentCog` / `Rank Up` / `Division Up`）
  **仍不导** —— 与本件无关（本文件 `:126-127` 已记「它们属于别的界面」），**本件没有推翻它**。

---

## ⑦ 顺手发现的

1. 🔴 **`Assets/WarpforgeBooster/` 整棵目录也是 ignore 的** —— **`.gitignore:184: /Unity/MyGame/Assets/WarpforgeBooster/`**。
   简报与 A1095 那件只提了 `.gitignore:26`（`WarpforgeVFX/*`）⇒ **本目录是第二条**。
   **实际影响**：**本件的验收对象（那份 prefab）在 `git status` / `git diff` 里根本看不见**
   ⇒ 收口时必须**直接看文件**（mtime / 字节 / md5 / 数 guid），⛔ **别拿 git 当判据**（这条与本件简报里
   关于 `WarpforgeVFX` 的那句同源，但**是另一条 ignore 规则、另一棵树**）。
2. 🔴🔴 **那份 prefab 里 guid 全 0 的引用共 350 条，其中【只有 30 条是动画】** ——
   本件修掉 30 条之后，**全文件仍会有 320 条 guid 0**。现读按字段名分组（`Booster Pack Open Window.prefab`）：

   | 字段 | 条数 | 是什么 | `Export()` 现在管不管 |
   |---|---|---|---|
   | `m_Animation`（默认）+ `m_Animations[]` 列表项 | **30** | legacy 动画 | ✅ **本件补上了** |
   | `m_fontAsset` | **127** | TMP 字体资产 | ⛔ **没有这一跳** |
   | `m_sharedMaterial`（TMP 那 127 个的） | **127** | TMP 材质 | ⛔ 同上 |
   | `m_Sprite` | **45** | **uGUI `Image.sprite`**（卡面/卡框/图标） | ⛔ `Export()` 只做 `SpriteRenderer` / `SpriteMask` / 粒子 TSA，**没有 uGUI `Image` 那一支** |
   | `m_Material` | **21** | uGUI `Graphic.material` | ⛔ 同上 |

   ⇒ 这是**同一个形状、更大的一个洞**：`BoosterPackExporter.Export()` 从 `EffectExporter.Export` 抄形状时，
   **`EffectExporter` 那套（粒子/网格/精灵）根本不含 uGUI 与 TMP 这两族**，而**窗口 prefab 里恰恰全是这两族**。
   ⚠️ **为什么一直没被发现**：本文件 `:18` 那张表写着这扇窗「**我们运行时不用它**（参考/诊断用）」，
   我们**不实例化**这份 prefab ⇒ 不炸。但按铁律 11（**发现差异先记录、之后完全复刻**），
   **这是个待办，不是「影响小所以不做」** —— 建议落到正本时按「`BoosterPackExporter` 窗口 prefab 的
   uGUI/TMP 引用那一跳」立一条。判据：本文件 `:18` 的表 + 本报告 §⑦·2 的分组数。
3. **`binder.animatorController = ""` 是硬写死的（`:482`），而 `Export()` 里没有 `Animator` 那一支** ——
   与 `EffectExporter.Export`（`:1193-1201` + `:1280`）的另一处形状差。**现读实测无影响**：
   两扇窗 `!u!95 = 0`、那 4 个粒子 prefab `!u!95 = 0`。⚠️ 但**将来若把带 `Animator` 的根加进
   `WindowRoots` / `CardFxRoots`**，会静默落成 guid 全 0（且 `binder.animatorController` 也拿不到名字）。
   **没改**（不在本件范围；改它就得先有判据说哪一件需要）。
4. **`Booster Info Popup.prefab` 是 0 个 `Animation`**（现读核过）—— **那件本来就没有，不是漏做** ⇒
   新那支对它**是空转**（0 次迭代），正确。
5. 那 **4 个粒子 prefab**（`Assets/CardPresentation/Effects/Boosterpack Open Card Rarity 1..4.prefab`）
   现读 `guid 0 = 0` 条 ⇒ 新那支对它们**也是空转**；**唯一实际生效的对象就是 `Booster Pack Open Window`**。
6. **本文件 `:239` 的第 ① 步里也有一个 `clipOk`** —— 我在新那一支里用了**同名局部变量 `clipOk`**，
   但**作用域不同**（第 ① 步那个在 `Run()` 里、我的在 `Export()` 里）⇒ 编译 0 错（已核）。
   留个记号：以后若把这两段搬进同一个函数，**必须改名**（否则 `CS0136`）。

---

## ⑧ 收口判据（重导之后怎么核「7 → 0」）

**重导命令**（与文件头 `:31-41` 那三步同；**本次没跑**）：
```bash
python d:/4/Unity/工具/extract_missing_shaders.py --prefabs      # ① 重打小包（若源包/小包被清过才需要）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod BoosterPackExporter.Run -logFile "d:/4/_tmp_view/booster_export.log"
```
日志里**新增**的判据行（本件加的）：
```
BP A1101 `Booster Pack Open Window` legacy `Animation` 片段：接回 **30** 条
```
⚠️ **期望值就是 `30`**（7 处 × 各自的 `m_Animation` 1 条 + `m_Animations[]` N 条，逐数见 §② 反向清单）。
若出现 `**没接上 N 条**` ⇒ 有片段落不下盘（**那就是真的取不到，别猜**，照 §⑥·1 去看是哪一条）。

**判据 1（本件的主判据 —— 「7 个组件不再有待接的槽」）**
```bash
python -I -c "
import io,re
s=io.open(r'd:/4/Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab','rb').read().decode('utf-8')
pids=['-7993935874865337289','8301228418192668656','-9033435933554767437','-1691631367813043119',
      '-177604310092043705','5911697182262325120','-8254079481253810722']
n=sum(len(re.findall(r'fileID: %s, guid: 00000000000000000000000000000000'%p, s)) for p in pids)
print('动画 guid-0 引用 =', n, '(目标 0)')
"
```
**改前实测 = `30`（本件现读确认）；改后目标 = `0`。**

**判据 2（结构判据 —— 反向必须【出现】的那 7 个 guid 及其条数）**
```bash
python -I -c "
import io
s=io.open(r'd:/4/Unity/MyGame/Assets/WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab','rb').read().decode('utf-8')
want={'4707992a0661d3843a41c1b54c392338':2,'c8438056df7a10e48a3a7df7870fa9d7':1,
      '097e39b1fc025654aaf15a174ee36e22':2,'c0189699598d48d4c8064a8b2fa55749':10,
      '8404e0135237b084ab4d652aaca8e34f':5,'ab77a28cf182e104385ec838d189615f':5,
      '1cd7c7aa5b0d6bb48955abb0252915a6':5}
for g,c in want.items(): print(g, s.count(g), '期望', c, 'OK' if s.count(g)==c else '✗')
"
```
**期望 = `2 / 1 / 2 / 10 / 5 / 5 / 5`（合计 30）。**

> 🔴 **这两条必须【同时】成立 —— 这就是「灭自证」的那一对**：
> 判据 1 的期望值（`0`）来自**我们的实现**（它写了就 0），判据 2 的期望值（那 7 个 guid 与条数）
> 来自**`.anim` 自己的 `.meta`**（`EffectExporter.ImportClip` 只能落回**已存在的那份资产**、guid 由 Unity 定）
> ⇒ **把这一支整段删掉，两条会同时变红**（判据 1 回到 30、判据 2 全 0）；
> **只把「写数组」或「写默认」删掉一条**，判据 2 的条数**立刻错**（例如 #5 会从 `2/1` 掉成 `1/1` 或 `2/0`）。
> 换个说法：**没有一种「两边一起改回去」能让这两条同时绿。**

> ⛔ **别用「全文件 `grep -c "guid: 00000000000000000000000000000000"` → 0」当判据** ——
> 那份 prefab 里另有 **320 条**非动画的 guid 0（§⑦·2）⇒ **那个数永远到不了 0**，
> 拿它当判据只会得出「修了没用」的假结论。（A1095 那份报告里那条 `grep -c` 对**特效包**成立，
> 因为那批 prefab 里 guid 0 的**只有动画**；**换到这份 prefab 上就不成立**。）

**改前基线（现读，供对比）**：`动画 guid-0 引用 = 30` · 全文件 guid-0 = **350** ·
`--- !u!111` = **7** · `--- !u!95` = **0** · 文件 962823 B / 纯 LF（CRLF 0 / LF 33412）· mtime 2026-10-03 10:18。

---

## ⑨ 摘要（300 字以内）

**A1101 已做**：`BoosterPackExporter.Export()` 原来只落 8 条 clip、**不接引用** ⇒ `Booster Pack Open Window.prefab`
的 **7 个 legacy `Animation`**（`:3334/7852/16848/24826/29978/33061/33248`）`m_Animation` 全 guid 0（共 **30 条**）。
照 `EffectExporter.cs:1203-1268` **逐行补了同一支**（新 `:403-475`）：`ImportClip` → `SetAnimationClips` + `an.clip`，
接不上**出声留空、不造空 clip**。**配对非猜**：fileID 即原版 pathID，反查包内 `m_Name`，
**7/7 命中第 ① 步 `ClipNames` 那 8 条里的 7 条**，再以头注记的 MB 字段交叉印证；目标 `.anim` **工程里本就有**。

**验证**：类型检查 **0/0**（唯一那条错在别人的 `RuleEngineTest.cs`）· +83/0 · 1 个文件 · LF 未翻（0/462→0/545）·
**没跑 Unity ⇒ 新代码未被执行**。**另报**：`WarpforgeBooster/` 另有 `.gitignore:184` 整棵 ignore；
该 prefab guid 0 共 **350 条、只 30 条是动画**，其余 320 条是 TMP 字体/材质与 uGUI —— `Export()` 缺那两跳（待办）。
