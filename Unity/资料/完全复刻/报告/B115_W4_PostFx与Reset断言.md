# `B115_W4` — `B-299`（`Attach` 补原版 `Awake` 的初始 `Blit`）+ `B-300`（Reset 断言覆盖面）

> 执行写手 `B115_W4` · 2026-11-01 · 工作目录 `d:/4`。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `账/*.tsv` / `活状态表.md` / `CLAUDE.md` / `项目任务.md` / `RuleEngine/**`。
> 唯一改动的两个源码文件：`CardPresentation/Battle/BattlePostFx.cs` · `CardPresentation/Editor/BattleScene.cs`（本波独占）。
> 先读的诊断（未重查）：`判据/B114_R10_Reset_ColorLookup诊断.md`（§〇 / §四 / §五-4 / §六-3、§六-5）· `报告/B114_W41_A243与聚合行自检.md:182-186`。

---

## 〇、两条的现状（进来时就已经是什么样）

| 活 | 进来时的实际状态 | 本波做了什么 |
|---|---|---|
| `B-299` | **真没做**：`Attach` 建完两张 RT 就 `return`，原版 `Awake` 那两步（`Blit(original → combined)` + 把 `ColorLookup.texture` 指到 `combined`）一句都没有 ⇒ 第一个 `BlendTo` 之前 `texture` 还指着静态资产 | ✅ **补齐**（做法见 §一） |
| `B-300` 之「硬前提」 | **已被 `B114_W53` 做掉**：`CheckResetLut`（`BattleScene.cs:50694-50699`）已有 `hasOrigin` 硬前提 + 三件断言，`11988` 那段注释也已订正 | ✅ 复核通过，**另补一条** `hasRt` 前提（§二-1） |
| `B-300` 之「7 场专属 LUT」 | **真没做**：`CheckResetLut` 只在 `Run()` 里对 `battlearena1`（= 贴共享恒等 `LUT Normal` 那一档）跑过；7 场专属 LUT 的战场**从来没走到** | ✅ **新增** `CheckResetLutOwnLutArenas`（§二-2） |
| `B-300` 之「两边同 null 恒真」 | `ReferenceEquals(texture.value, pfx.Combined)` 在两边同 null 时恒真，原前提只钉了 `OriginalLut != null`，**没钉 `Combined`** | ✅ 补 `hasRt` 前提 |

⚠️ **`报告/B114_W41` 那句「Reset 断言仍成立」已经订正过了**（`:182-186` 已由 `B114_W53` 就地改成「这句是错的 + 错因 + 修法」）⇒ 本波**不需要再改它**，只做了核对。

---

## 一、`B-299`：`BattlePostFx.Attach` 补上原版 `LUTBlender.Awake` 的两步

### 1.1 判据（`d:/2/tools/decomp_full/LUTBlender__Awake.c` 现读，逐句）

原版 `Awake` 的**完整次序**（按 `.c` 里的调用序）：

1. `new Material(<LUTBlender shader>)` → `this+0x58`
2. `Volume.profile` → `TryGet<ColorLookup>` → `this+0x38`；**取不到** ⇒ `CustomDebug.LogError` + `enabled = 0` + `return`
3. `ColorLookup.texture`（`*(this+0x38 + 0x38)`）那个 `VolumeParameter` 的 **value getter**（vtable `+0x218`）→ `this+0x50` = **`originalLUTTexture`**
4. 用它的 `width(+0x188)` / `height(+0x1a8)` 建 `combinedLUT` → `this+0x40`（`useMipMap = 0` · 名字 `Particle FX controller LUT`）
5. 🔴 **对 `texture` 那个 `VolumeParameter` 虚调用（vtable `+0x228`，实参 = `*(this+0x40)`）⇒ 把引用指到 `combinedLUT`**
6. 再建 `workingRT` → `this+0x48`（名字 `LUT Blend helper`）
7. 🔴 **`UnityEngine_Graphics__Blit(*(this+0x50) → *(this+0x40))`** ＝ 把 `originalLUTTexture` 先拷进 `combinedLUT`
8. `Blit(*(this+0x40) → *(this+0x48))` + `SetTexture(mat, "_LUT1", workingRT)`（**这两句我们不需要** —— 文件头第 ④ 条偏离：不建 `workingRT`）

⇒ 第 5、7 步就是本活要补的**那两步**。补完之后 `ColorLookup.texture` **恒指 `combinedLUT`**：
`PostFXController__ResetLUT.c` / `LUTBlender__SetTargetLUT.c` / `LUTBlender__DoBlend.c` **一次都不改那个引用**，只改 `combined` 的**内容**。
⇒ 这也正是 `CheckResetLut` 那三条断言（引用不变 / `_Blend → 1` / 像素 = 原 LUT）所断的形状 —— 补完这两步之后「实现形状」和「断言形状」才第一次对齐。

### 1.2 改在哪

| 文件:行 | 改动 |
|---|---|
| `CardPresentation/Battle/BattlePostFx.cs:203` | 在 `_targetLutRt = NewRt(...)` **同一物理行**末尾（`//` 之前）追加 `InitCombinedLikeAwake();` —— 位置刻意选在「两张 RT 都建完」之后，与原版第 6→7 步的位置一致 |
| `CardPresentation/Battle/BattlePostFx.cs:562-586` | **类尾新增** `void InitCombinedLikeAwake()`（新成员一律追加到类尾，见 §三 的零漂移说明） |

方法体只有三句：

```csharp
if (_cl == null || _combined == null || _original == null) return;   // ⚠️ 降级支，见 doc
Graphics.Blit(_original, _combined);      // 原版第 ⑦ 步
_cl.texture.overrideState = true;
_cl.texture.value = _combined;            // 原版第 ⑤ 步
```

### 1.3 三个刻意的判断（都写进了 doc 注释，免得下个会话当 bug 改）

1. **`_original == null` 时整段跳过**：原版走到「取原图宽高」那一步会 **NRE**（`FUN_1803f47a0`）。我们跳过而不是「指到一张没初始化过的 RT」—— 后者会**直接黑屏**，比「还指着静态资产」更糟。这时引用形态与重建前一致，而自检的硬前提 `pfx.OriginalLut != null` 会把这种局面**红出来**。
2. **`overrideState = true` 这一句是我们加的**（原版那一步只是 `texture.value = combined`）。理由：本文件 `BlendTo`（`:347`）本来就每次都置 `true`，不在这里置会让「`Attach` 之后 / 第一次 `BlendTo` 之前」出现一个**语义不一致的中间态**；而且 13/13 场的 `*_PostFx.asset` 现读 `m_OverrideState: 1`（文件头第 ② 条）⇒ 实际是 no-op，`Detach` 也会按 `_texOverride0` 还回去。
3. **⛔ 别把它和 `Detach` 里那句「交还 `_original`」当矛盾**：后者是**销毁 RT 之前必须做的**（否则 `ColorLookup.texture` 指着已释放的 RT ⇒ 粉/黑屏）。两件事各管一头，注释里写死了。

### 1.4 净视觉

**与改动前一致**（`combined` 的内容 = 原 LUT）。变的只有**引用形态**：现在 `texture` 从 `Attach` 起就指 `combined`，与「`DoBlend` 的常驻形态」相同。

---

## 二、`B-300`：Reset 断言的覆盖面

### 2.1 补第二条硬前提（`BattleScene.cs:50696-50704`）

原来只有 `hasOrigin`（`pfx.OriginalLut != null`）。但下面这句

```csharp
check(ReferenceEquals(pfx.ColorLookupRef.texture.value, pfx.Combined), …)
```

在 **`texture.value` 与 `Combined` 同时为 null** 时**恒真** —— 那正是本项目 2026-10-26 一天撞到 3 次的假绿形状。⇒ 新增：

```csharp
bool hasRt = pfx.Combined != null && pfx.ColorLookupRef != null;
check(hasRt, "（前提）合并 RT 与 `ColorLookup` 都在 ⇒ 下面「引用相等」那条才排得掉「两边同 null 恒真」");
if (!hasRt) return;
```

⚠️ 仍是「**先 `Check(前提)`、再 `Check(结论)`**」，⛔ 不是 `if (x != null) Check(...)` 那种静默跳过。

### 2.2 让它在 7 场专属 LUT 的战场上也真跑到（新增 `CheckResetLutOwnLutArenas`）

**为什么必须补**（缺口出处）：`Run()` 只建 `battlearena1`，而它属于「原版贴**共享恒等** `LUT Normal`」那 6 场之一（`ArenasWithOwnLut` 之外）。⇒ 「`_original` 是**本场专属** `LUT <场>` 时，`Reset` 的净结果还对不对」**从来没被自检碰到过**（`B114_R10` §五-4 / §六-5 记的就是这条）。

**做法**（`BattleScene.cs:11991` 调用 → `:50733` 方法体）：

1. 遍历 `ArenasWithOwnLut`（7 场：`battlearenaaeldari` / `…emperorschildren` / `…genestealers` / `…leviathan` / `…sororitas` / `…spacewolves` / `…tauriorla`）
2. `Resources.Load<GameObject>("ArenaPrefabs/<场>")` → `Instantiate`（**`A345` 已经在用同一条路**，`BattleScene.cs:5660/5674`）
3. 找那个 **`isGlobal` 的 `Volume`**（同 `:47293`），`new BattlePostFx()` + `Attach(vol)`
4. 先发一条 `LutBlend(setTextures:true)` + `BlendTo(0.25f)` —— **与实战场那条同形**，保证 `blendBefore = 0.25`（而不是 `-1` 那种「从没写过」的弱前提）
5. 调 **同一个** `CheckResetLut(pfx, check)` —— ⛔ **判据只有一份**，不另写一套（否则两处口径迟早分叉）
6. `finally` 里 `pfx.Detach()` + `Object.DestroyImmediate(inst)`

**已核实的两个前提**（没跑 Unity，但都是**静态可证**的）：

- `Resources/ArenaPrefabs/` 下这 7 份 prefab **都在**（`ls` 现读）。
- prefab 里那个全局 `Volume` 的 `sharedProfile` **就是** `Arenas/<场>/Profiles/<场>_PostFx.asset`：现读 `battlearenaaeldari.prefab:102870-102882`，`m_IsGlobal: 1` + `sharedProfile: {fileID: 11400000, guid: 7ca1bb01628357c4d980a59c5198d6c1}`，而 `battlearenaaeldari_PostFx.asset.meta` 的 guid **正是** `7ca1bb01…`。

**两条必须处理的副作用**（已做，注释写死）：

1. `BattlePostFx.Attach` 会**覆盖** `WFModulePostProcess.OnFlushVolumeStack` 这个**静态**钩子（`BattlePostFx.cs:230`）⇒ 方法进出各**存/还**一次。**漏了的话**这一节跑完，真战场那台相机的 Volume 刷新就断了，**后面所有后期断言会莫名其妙地红**（而且找不到原因）。
2. `Attach` 拿的是 `vol.profile` **getter 造出来的运行时副本**（`BattlePostFx.cs:139`）⇒ **写不脏**那 7 份 `*_PostFx.asset`；prefab 实例必须 `DestroyImmediate`（批处理没有帧循环）。

**覆盖面断言**（`ran == ArenasWithOwnLut.Length`，即 7）：**故意**是硬断言 —— 它红说明上面某一场**没跑到**（取不到 prefab / 没有全局 Volume / 后期链没就绪），⛔ 别把「没跑到」当「通过」。

### 2.3 调用点为什么写成「同一物理行」

`BattleScene.cs:11991` 那行**本来就带一段 `//` 说明**。按 CLAUDE.md 的铁律（「追加到注释之后 = 静默不执行」），新调用接在 **`//` 之前**：

```csharp
CheckResetLut(pfx, Check); CheckResetLutOwnLutArenas(Check);   // 发 `Reset(instant)` 请求 + …
```

⇒ 行数**零变化**，顺带 8 条账落点不漂移（见 §三）。

---

## 三、行号映射

### 3.1 `CardPresentation/Editor/BattleScene.cs` —— **零漂移（映射表为空）**

设计成零漂移的两条依据：

- `账/*.tsv` 里指向 `BattleScene.cs` 的落点一共 **10 条**，**最大行号 41355**（现读：6143 / 7517 / 13558 / 14268 / 15410 / 22213 / 26363 / 32098 / 37609 / 41355）。
- 本波所有**新增行**都落在 **≥ 50696**（`hasRt` 前提 + 类尾新方法），**全部在最大落点之后** ⇒ 那些落点一个都不动。
- 唯一在 11991 的改动是**同一物理行替换**（+1 条调用，行数不变）。

| 位置 | 旧行号 | 新行号 |
|---|---|---|
| 调用点（内容变了，行号**不变**） | 11991 | 11991 |
| `CheckResetLut` 的 `hasRt` 前提（新） | — | 50697-50703 |
| `CheckResetLutOwnLutArenas`（新，类尾） | — | 50733-50793 |

### 3.2 `CardPresentation/Battle/BattlePostFx.cs` —— **零漂移（映射表为空）**

- 指向本文件的落点 **12 条**，**最大行号 378**（131 ×2 / 300 ×2 / 306 / 342 ×2 / 363 ×4 / 378）。
- 新增行只有两处：`:203`（**同一物理行**追加调用，行数不变）与 **类尾 `:562` 起**的新方法 —— 都在 378 **之后** ⇒ 零漂移。
- `:349` 那句注释是**就地改写**（同一行，行数不变）。

⇒ `check_ledger_points.py` 读数 **漂移 0 · 失败 0**（与基线一致，见 §四）。

---

## 四、闸的读数（现跑，全绿）

| 闸 | 命令 | 读数 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_w4 bash d:/4/Unity/工具/typecheck.sh` | 运行时 **0** / 编辑器 **0** |
| 聚合行闸 | `python -I -X utf8 d:/4/Unity/工具/check_aggregate_lines.py MyGame/Assets/CardPresentation/Editor/BattleScene.cs MyGame/Assets/CardPresentation/Battle/BattlePostFx.cs` | `BattleScene.cs:46896` **52 个调用全在 `//` 之前**（首个 `//` 在字符 5219 · 行宽 5860）· `BattlePostFx.cs` 未发现聚合行 · **违规 0 ⇒ PASS**（rc=0） |
| 账落点闸（只读，未 `--fix`） | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | **A 5841 条 · 通过 5841 · 漂移 0 · 失败 0**；B 档 4 条 / 落点问题 2 条（`[PROSE]`，与基线**完全相同**） |
| 行尾未被翻 | 二进制盘点 | `BattlePostFx.cs`：LF 588 / CRLF 0 / 裸 CR 0（**本来就是 LF**）· `BattleScene.cs`：LF 50794 / CRLF 50794 / 裸 CR 0（**全 CRLF，未翻**） |
| `git diff --numstat` | — | `BattlePostFx.cs` **28 / 2** · `BattleScene.cs` **83 / 1**（都是「类尾新增 + 同一行替换」，没有整篇重写） |

---

## 五、要跑的宿主与预期（本波**不能**跑 Unity，交给同步点）

### 5.1 跑哪一条

**`BattleScene.Run`** —— 只碰了战斗侧的两个文件，不涉及共用件（没动 `Shell/` 的 `MenuDraw/MenuWindowBase/ImageQuad`，也没动 `Label/WfSlider/字体`）⇒ 按铁律 12 的「只动一个宿主就只跑那一条」，**只跑这一条**。

⚠️ 注意：`Editor/BattleScene.cs` 是被 `.cs` 写手碰过的文件，若同步点当时还有别的 `BattleScene.cs` 写手，**先确认没人在写**再跑（本波我是唯一写手，已声明）。

### 5.2 预期新增的断言（数一下，便于对照日志）

**每场 8 条 × 7 场 + 1 条总覆盖 = 57 条新断言**，文案固定（方便 `grep`）：

| # | 文案前缀 | 条数 |
|---|---|---|
| 1 | `（前提）Resources/ArenaPrefabs/<场> 取得到` | 7 |
| 2 | `（前提）<场> 的 prefab 里有**全局** Volume + sharedProfile` | 7 |
| 3 | `（前提）<场> 的后期下游就绪` | 7 |
| 4 | `（前提）进场时 ColorLookup.texture 非空`（`CheckResetLut` 内） | 7 |
| 5 | `（前提）合并 RT 与 ColorLookup 都在`（本波新增） | 7 |
| 6 | `★ Reset 之后 ColorLookup.texture 仍指合并 RT` | 7 |
| 7 | `★ 而且 _Blend 落到了 1（Reset 前 0.25）` | 7 |
| 8 | `★ Reset 之后合并 RT 的内容 = 原 LUT` | 7 |
| 9 | `★ 7 场专属 LUT 的战场都真跑到了 Reset 的净结果（跑到 7 场）` | 1 |

**另外**：`battlearena1` 那一份（`CheckResetLut(pfx, Check)`）**照旧**，条数不变（现在是 5 条：`hasOrigin` / `hasRt` / 引用 / `_Blend` / 像素）。

### 5.3 可能的红点与判断（**先看再说，别先猜着修**）

| 若红在上面的 # | 最可能的根因 | 不要做的事 |
|---|---|---|
| #1 | `Resources/ArenaPrefabs/<场>` 缺 prefab（跑 `ArenaBuilder.BuildArenaPrefabs`） | ⛔ 别把断言改成 `if (apf != null)` |
| #2 | prefab 里的 Volume 不是 `isGlobal` / 没 `sharedProfile` | ⛔ 别改 `ArenaPrefabs`（那是产物，要重生成） |
| #3 | `Hidden/LUTBlender` 解析不到（`WarpforgeShaderMap`） | ⛔ 别在断言侧兜底 |
| #4 | 某场的 `_PostFx.asset` 没有 `ColorLookup` 槽或 `texture` 为空 | 这正是 `A243` 那条逐场静态对账要抓的东西，两边应该一起红 |
| #6/#7/#8 | `Reset` 实现在某场走岔（`_mat` 空 / `_original` 空） | 先看 `BattlePostFx.cs:489` 那个降级支判据 |
| #9（而 1-8 全绿） | 中间抛过异常被 `finally` 吃掉？→ 看日志里有没有 `Exception` | — |

**像素容差没实测**：仍是 `B114_R10` §五-2 那条 —— `0.02`（≈5/255）是保守档；项目是 Gamma（`m_ActiveColorSpace: 0`），实测若更紧可收。**本波没有跑 Unity，容差一个字没动。**

### 5.4 时长会变长

7 场各 `Instantiate` 一份**整场 prefab**（`battlearenaaeldari.prefab` 现读 **124,970 行**）—— `A345` 那段本来就要实例化 13 份，本波再加 7 份 ⇒ 那一带的耗时大致再 +50%。`BattleScene.Run` 基线 **7m56s**，预计增加**十几秒到几十秒**量级（未实测）。若同步点觉得太慢，**别删断言**，改成「只在 `WF_ARENA_RESET_SWEEP=1` 时跑」要另开裁定。

---

## 六、顺手发现（⛔ 只报，除了注明「已就地订正」的那两条）

1. **`BattlePostFx.cs:349` 的注释已经过期，已就地订正（铁律 5）**：原文写
   「`_lastBlend` = 读口 **+ `ResetLutCore` 的起点**」，而 `ResetLutCore` **根本不读 `_lastBlend`**（非 instant 支是从 `BlendTo(0f)` 起的，`:512`）⇒ 已改成「只是自检读口 `BlendNow`，**不是** `ResetLutCore` 的输入」。⚠️ 这条要是不改，下个会话很容易照着它给 `ResetLutCore` 造一个不存在的「起点」依赖。
2. **`B114_W41` 那句预测已由 `B114_W53` 就地订正过**（`报告/B114_W41_A243与聚合行自检.md:182-186`）⇒ 本波**不重复改**，只核对内容与新判据一致。
3. **`CheckResetLut` 的 `blendBefore` 前提在 7 场那条路上是「我们造出来的」**：实战场那条是上游 `BlendTo(0.25f)` 留下的，7 场那条是我显式 `BlendTo(0.25f)` 造的。若哪天有人把实战场那条上游调用删掉，`blendBefore` 会变成 `-1`（`< 0.99` 仍然成立 ⇒ 断言**照样绿**，但它已经不再证明「Reset 把 0.25 推到了 1」）。⇒ **建议**（本波没做，怕越界）：在 `CheckResetLut` 里加一条 `check(blendBefore >= 0f && blendBefore < 0.99f, "（前提）Reset 之前 `_Blend` 是个真实的非 1 值")`。**这条属新增断言，需要再动一次 `CheckResetLut`，留给调度台排。**
4. **7 场专属 LUT 的运行时 `Reset` 语义此前从未被验** —— 这也意味着：若这 7 场里任何一场的 `LUT <场>` 资产本身缺失/损坏，**到今天为止没有任何断言会红**（`A243` 只比 `texture.value.name` 字符串，不读像素）。本波之后 **像素那条会兜住它**。
5. `BattlePostFx.Attach` 里 `_cl.texture.overrideState = true`（本波新增于 `InitCombinedLikeAwake`）与 `Detach` 的 `_texOverride0` 还原是配对的 —— 但**没有任何断言**验「`Detach` 之后 `overrideState` 回到了进场值」。**已有先例**：`Detach` 里另一句无条件 `false` 曾把 7 场的静态 LUT 从 Volume 栈上掀掉过（文件头 :62 那段）。⇒ 建议（本波没做）在 `BattleScene.Run` 里给「`Attach`→`Detach` 一圈之后 `overrideState` / `active` / `contribution` 三件回到进场值」加一条。**属新活，进账。**

---

## 七、改动清单（供调度台核对）

| 文件 | 行 | 类型 |
|---|---|---|
| `CardPresentation/Battle/BattlePostFx.cs` | `:203` | 同一物理行追加 `InitCombinedLikeAwake();`（`//` 之前） |
| 同上 | `:349` | 注释就地订正（行数不变） |
| 同上 | `:562-586` | 类尾新增 `void InitCombinedLikeAwake()` |
| `CardPresentation/Editor/BattleScene.cs` | `:11991` | 同一物理行追加 `CheckResetLutOwnLutArenas(Check);`（`//` 之前） |
| 同上 | `:50697-50703` | `CheckResetLut` 内新增 `hasRt` 硬前提 |
| 同上 | `:50733-50793` | 类尾新增 `static void CheckResetLutOwnLutArenas(...)` |
