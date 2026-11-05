# B145-E8 · A-25 / A-26（`WarpforgeArena1/Editor/ArenaBuilder.cs`）

> 执行代理 `B145-E8` · 2026-11-05 · 判据 = `判据/B145_施工单汇总_离线.md` §二（A-25 / A-26）
> + `报告/B144_D_战场环境.md` §2 / §一·M1 + `报告/B144_R1_网格结构.md` §8·6。
> 只改了两个文件：`ArenaBuilder.cs`（+35 行）· 新建 `CardPresentation/Editor/B145_E8_Assert.cs`（242 行）。

---

## A-25 改前/改后

**落点**：改前 `ArenaBuilder.cs:5410-5416`（7 行，**两段重复 `<summary>`**）→ 改后 `:5422-5428`（7 行，合并成一段）。
行号 `+12` 是本文件前面 A-26 的两次加行造成的（见下），**本段自身 7 → 7 行、零行数变化**。

改前（段首尾各 3 行）：
```csharp
    /// <summary>⚠️ 2026-09-20：**「用原版网格 shader」这条路当前是关的** —— 实测整屏洋红
    /// （原版这批网格 shader 在我们工程里跑不起来，详见 `GetOrCreateMaterial` 里的长注释）。
    ...
    /// 用环境变量 `WF_ORIGSHADER=1` 打开（A/B 用，不必改代码）。
    /// 清单里的 `shader` 字段**已经带出来了**，开关一翻就能试。</summary>
```

改后（逐字）：
```csharp
    /// <summary>🔶 自己设计（非原版）：**本体网格 shader 一律用 `Universal Render Pipeline/Unlit`** —— 不是原版的
    /// `Everguild/UnlitAmbient`(523 槽) / `Unlit Wind`(56) / `UnlitAmbient Emissive Flickker`(24) / `Pulsating Mesh`(23) /
    /// `Unlit UV scroll`(13) …。为什么必须自己定：原版这批网格 shader **在本工程里跑不起来**（`WF_ORIGSHADER=1` 实测整屏洋红；
    /// 探针说 6 个 shader 全都 `isSupported=True` 也不等于能渲，详见 `GetOrCreateMaterial` 的长注释）。谁定的+日期：调度台 2026-11-05 按铁律 18 补标
    /// （判据 → `报告/B133_审计_战场场景与环境.md` §四·M1 · `报告/B144_D_战场环境.md` §一·M1）。
    /// 清单里的 `shader` 字段**已经带出来了**，`WF_ORIGSHADER=1` 可 A/B（不必改代码）。
    /// ⚠️ 上一版此处有两段重复的 `<summary>`，本补丁合并成一段（行数不变）。</summary>
    static bool ArenaUseOriginalShaders
        => System.Environment.GetEnvironmentVariable("WF_ORIGSHADER") == "1";
```

**铁律 18 三样齐全**：① 是什么 = 本体网格一律 URP/Unlit ② 为什么必须自定 = 原版 shader 在本工程跑不起来（`WF_ORIGSHADER=1` 整屏洋红）③ 谁定的+日期 = 调度台 2026-11-05。
**行为零变化**：开关函数本体一字未动（`WF_ORIGSHADER == "1"`），断言 D7 实跑两态验证。

---

## A-26 DTO 字段 + 应用段

### ① DTO（`ParticleEntry`）

落点：`ArenaBuilder.cs:331-337`（原 `:330` `public int maskInteraction;` 之后）。
**加 7 行**（4 行注释 + 3 个字段）。

| 字段 | 类型 | 默认值 | 依据 |
|---|---|---|---|
| `renderingLayerMask` | **`uint`** | `1u` | 原版 All = 4294967295，`int` 装不下（会被 `<0 ? 1` 映射回 1） |
| `lightProbeUsage` | `int` | `0` | 原版 1×14，其余 0（enum 值直传） |
| `sortingOrder` | `int` | `0` | 原版 1×2；arena2 有 `-1` / `5` ⇒ **不夹 0..1** |

清单侧已由 `工具/scripts快照/gen_unity_arena_manifest.py:2211-2213` 输出这三键（本波未改生成器）。

### ② 应用段

**新方法** `ApplyParticleRendererParams(ParticleSystemRenderer, ParticleEntry)`：`ArenaBuilder.cs:6833-6853`（doc + 方法，**23 行**），紧跟在 `ApplyRendererParams(MeshRenderer, MeshEntry)` 之后。

- `renderingLayerMask`：**0 当缺键 ⇒ 兜底 `1u`**（与 `ApplyRendererParams` 的 `<0 ? 1u` 同一口径）。
  🔴 这条兜底不是可选项：本波实测 **5/13 场的 manifest 还没有这三个键**（见「顺手发现 §2」），
  若直接写 0 会让渲染层掩码全灭（相机/光掩码与 0 相与恒 0）⇒ 那 5 场可能整场不渲染。
- `lightProbeUsage` / `sortingOrder`：**非默认才写**（缺键保持 Unity 新建 PSR 默认，⛔ 不把 `JsonUtility` 的 0 兜底洗进产物）。
- `Mathf.Clamp(p.lightProbeUsage, 0, 4)`；`sortingOrder` 直传（允许负值）。

**调用点**：`ArenaBuilder.cs:2894-2898`（`BuildContent` 内、`renderMode` 的 if/else 收尾之后、`renderAlignment` 之前）。
**加 5 行**（4 行注释 + 1 行调用）。

**「两种 renderer 都调」**：`MeshRenderer` 走原有的 `ApplyRendererParams`（3 处调用点未动），
`ParticleSystemRenderer` 走新的 `ApplyParticleRendererParams`。两边**故意不合并**：MR 的 `lightProbeUsage`
默认是 1、PSR 是 0，合并会把一侧的缺键洗成另一侧的默认。

**本文件总行数**：6,977（HEAD）→ **7,012**（+35 = DTO 7 + 调用 5 + 方法 23）。

### ⚠️ 生效边界

代码侧落盘≠画面变化：要跑 `ArenaBuilder.BuildPrefabFromCLI`（13 场）才会写进 PSR。
这是 **B-04** 的活（Unity 窗口），本轮**未跑**。

---

## 断言

文件：`CardPresentation/Editor/B145_E8_Assert.cs`（新建，242 行）· 类 `B145_E8_Assert` · **23 条** `Check`。

| 组 | 条数 | 内容 |
|---|---:|---|
| A（DTO） | 6 | 字段存在 / `renderingLayerMask` 类型必须是 `uint` / 另两键 `int` / 缺键默认 `1,0,0` |
| B（uint 容量） | 4 | `4294967295` 装得进；同一值按 `int` 读 = −1（**灭自证**：改回 `int` ⇒ A2+`B2` 同红）；Unity API 回读相等 |
| C（应用段） | 6 | PSR：All / UseProxyVolume / `−1` 逐值落上；缺键 0/0 不洗默认；**`renderingLayerMask=0` 兜底成 1**；MR 侧 All / BlendProbes / 3 逐值落上 |
| D（A-25 + 接线） | 7 | 旧两段 `<summary>` 已删 / `🔶 自己设计（非原版）` 存在 / 三样齐全 / **调用点真的接线了** / `WF_ORIGSHADER=1` 仍能开、不设仍关 |

**灭自证**：期望值来自原版 PSR 逐颗读数（34 条：All×28 / 1×6 · lightProbe 1×14 · sorting 1×2）与字段语义，不是拿被测常量证明自己。
**⛔ 无 `if (前提) Check(结论)`**：前提单独 `Check`（A1/C1/C4/D0/D6），结论 Check 无条件执行。

**建议宿主**：dispatcher 统一接线（⛔ 本代理不碰宿主）。
首选 `CardPresentation/Editor/BattleScene.cs` 那条聚合断言行（同一物理行已有 `B144_B2_Assert.Run()`，
见 `B145_施工单汇总_离线.md` §六·1），形如 `B145_E8_Assert.Run(); _sink.Pass += B145_E8_Assert.Pass; _sink.Fail += B145_E8_Assert.Fail;`。
⚠️ 该行是**一整行聚合**，追加时必须落在**行内第一个 `//` 之前**（铁律：注释吞代码）。
若调度台更愿意挂在战场域，`WarpforgeArena1/Editor/B139_H_Assert.cs` 的 `Run` 也可（同一 Editor 程序集、类型可见）。

---

## typecheck 结果

| 次 | 运行时 | 编辑器 | 说明 |
|---|---:|---:|---|
| 1 | **1** | 3 | 全是别人在飞的文件：`RuleEngine/Core/RuleCore.cs:18873` CS0111（重复成员）+ `RuleEngine/Editor/B142_E62_Assert.cs` 3 条（`DefensiveCardInventoryCheck` 找不到 = 运行时没编过的**级联假错**）。我的两个文件**零 error**。 |
| 2 | 1 | 3 | 同上（`RuleCore.cs` 与 `B142_E62` 在 07:33 仍在被写）。 |
| 3（收尾） | **0** | **0** | ✅ 别人交回后复跑全绿。 |

命令：`cd D:/4/Unity/MyGame && TMPDIR=/tmp/wf_e8 bash D:/4/Unity/工具/typecheck.sh`
（新文件已确认在 `wf_csc_editor.rsp` 的源清单里，不是漏编。）

⚠️ 未跑 Unity、未跑任何自检宿主（简报红线）；断言**没有实跑过**，只保证编译通过。

---

## 卡住 / 顺手发现

### 1. 🔴 施工单 §五 的「`ArenaBuilder` 无账按行号引用 = 0」**是错的**

`账/未分类_其他.tsv` 有 **6 条 A 行按行号**引用 `WarpforgeArena1/Editor/ArenaBuilder.cs`（TSV 第 757~762 行，行号在**第 6 列**）：

| 符号 | 账里写的行号 | HEAD 实际 | 本波后实际 | 我的增量 |
|---|---:|---:|---:|---:|
| `BuildContent(Transform parent, Manifest mf)` | 2187 | 2271 | **2278** | +7 |
| `LoadSpriteAsset(string sceneName, …)` | 5482 | 5593 | **5605** | +12 |
| `ApplySceneRenderAndBakeSettings(Manifest mf)` | 6579 | 6714 | **6726** | +12 |
| `PatchLightmapEditorSettings(string scenePath, …)` | 6609 | 6744 | **6756** | +12 |
| `ApplyRendererParams(MeshRenderer mr, MeshEntry e)` | 6653 | 6798 | **6810** | +12 |
| `ApplyInferredParticleBlendPreSave(Material mat, ParticleEntry p)` | 6695 | 6830 | **6865** | +35 |

🔑 **这 6 条在我动手【之前】就已经漂了 +84…+145 行**（HEAD 实测已对上表第 3 列）⇒ **不是我造成的**；
我的加行只让它再漂 +7…+35。

`python -I -X utf8 工具/check_ledger_points.py` 现在把这 6 条都报成「漂移」。
**建议**：同步点（没有 `.cs` 写手时）跑 `check_ledger_points --all-A --fix` **一次**重钉；
⛔ 本代理无权改账，未动。

### 2. 🔴 **5/13 场的 manifest 还没有 A-26 的三个键**（323 颗粒子）

```
有键（8 场）：arena1 34 · arena2 46 · arena3 46 · aeldari 23 · astra 20 · blacklegion 39 · darkangels 34 · emperorschildren 13
缺键（5 场）：genestealers 35 · leviathan 45 · sororitas 49 · spacewolves 31 · tauviorla 163
```

- 后果：`CardPresentation/Editor/B144_R1_Manifest_Assert.cs` 的 **A1（每场三键齐）/ A2（13/13）现在应当是红的**；
  且这 5 场的 PSR 三字段即使代码对了也拿不到真值（会吃兜底 1/默认）。
- 修法 = 重跑 `工具/scripts快照/gen_unity_arena_manifest.py`（**离线**），但它会重写 `arenas/*/*_manifest.json`
  —— **不在本代理白名单**（简报也禁止「重生成任何 arena」）⇒ ⛔ 未做，交调度台派。
- 真正改变画面还要再跑 `ArenaBuilder.BuildPrefabFromCLI`（B-04，Unity 窗口）。

### 3. ⚠️ `JsonUtility` 对 `uint` 字段缺键时是否跑字段初始化器 —— **未实测**

本机不允许跑 Unity，无法验证 `JsonUtility.FromJson<ParticleEntry>` 在缺键时给 `1u`（初始化器）还是 `0`（未初始化）。
两种情况下本补丁都安全：`0 ⇒ 兜底 1u`；`1u ⇒ 原样写入`。断言 C3b 钉住「0 必须兜底成 1」。
⚠️ 若将来有人把 `renderingLayerMask` 改成 `int`，`A2`/`B2` 会当场红。

### 4. 顺手发现（未改）

- `ArenaBuilder.cs` 里 `ApplyRendererParams` 的 3 个调用点（`:2402` / `:2453` / `:2501`）写在
  `else ApplyShadowFlags(...); ApplyRendererParams(...);` **同一行、且位于 `else` 的单语句后** ——
  语义上 `ApplyRendererParams` 在 `else` 之外（每行都在 `if/else` 之后无条件执行），当前是对的；
  但这类「同一行 if/else + 追加调用」正是「注释吞代码」那族的高危形状，**下次改这几行前先确认位置**。
- 本文件 `ParticleEntry` 里已有 `maskInteraction`（`:330`）与新的三字段相邻，**没有冲突**；
  旧的 `maskInteraction` 走 `if (p.maskInteraction != 0)`（`:2942`），与新的「非默认才写」口径一致。

---

## 改动清单（`git diff --stat` 口径）

| 文件 | 变化 |
|---|---|
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` | 6,977 → 7,012 行（+35：DTO 7 · 调用 5 · 方法 23）；行尾全程 CRLF、裸 CR = 0（已二进制核） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B145_E8_Assert.cs` | 新建 242 行（LF，与同目录 B144/B145 断言一致） |
