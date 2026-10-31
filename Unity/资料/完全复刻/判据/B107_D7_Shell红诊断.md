# B107_D7 — ShellScene 6 条红的只读诊断

- 日期：2026-10-31 · 代理：`B107_D7`（只读诊断）· 未改任何源码/账/正本
- 日志：`d:/4/_tmp_view/shell.log`，合计行 `:158643` = **3941 通过 / 6 失败**
- 对照基线（本批改动前的全绿运行）：`d:/4/_tmp_view/host2_ShellScene.log`（2026-10-31 02:33，`:153202` = **3685 通过 / 0 失败**，**没有** `B107R3d`）

## 结论总表

| # | 失败（日志行） | 判定 | 一句话 |
|---|---|---|---|
| 1–5 | `A5[0..4]s 相对倍率`（`:75782 / 75854 / 75926 / 75998 / 76070`） | **α 断言错** | 期望式的分母取成「**本槽的起点**缩放 210」，而实现口径是「**槽 0 的终点**缩放 250」⇒ 期望值整体被放大 `250/210 = 1.190476` 倍 |
| 6 | `wf_prefabs_extra.bundle 能被 LoadFromFile 加载`（`:152325`） | **δ 夹具前提不成立**（本会话新接线造成的**宿主内加载顺序**冲突；不是 W22/W16/W29 改了产物） | 同进程里 `B107R3d`（`ShellScene.cs:4812`，本会话新加）先经 `WarpforgeAnimatorBridge` 加载并**全程持有**这只包；8 万行后 `B103W7b` 对**同一文件**再 `LoadFromFile` ⇒ Unity 拒绝 |

> 责任划分：两条都是**断言/夹具侧**的问题，产品实现（`CardFan` / `WarpforgeAnimatorBridge`）经复核**与原版/生产口径一致，⛔ 不要改**。

---

## 失败 1–5 · `A5[*]s 相对倍率` —— α（断言期望算错）

### 结论

`CardFan` 的「相对倍率」实现是对的：`Rel(i) = _scl[i] / _scl[0]`，
分母是**槽 0 的终点缩放 250**（`_scl[0] = 250f`）。
断言第 157 行的期望式却用 `ss.keys[0].v[0]` 做分母 —— 那是**该槽曲线第 0 个键（t = 0 的起点缩放）**，五槽全是 **210**。
两者恒差 `250/210 = 1.190476`，所以五条全红，且**和槽号无关地**整体偏大同一个倍数。

### 证据

- 断言：`Unity/MyGame/Assets/CardPresentation/Editor/B107_R3c_Assert.cs:157-158`
  ```csharp
  N(CardFan.Wpx(i) / CardFan.FrontWpx, cScl / ss.keys[0].v[0], 1e-4f, ...)
  ```
- 实现：`Unity/MyGame/Assets/CardPresentation/Core/CardFan.cs`
  - `:42` `_scl = { 250f, 232.9537f, 221.5926f, 204.5463f, 181.8148f, ... }`
  - `:108` `Rel(i) = _scl[i] / _scl[0]` · `:112` `FrontWpx = 2.0927f * 250f` · `:115` `Wpx(i) = FrontWpx * Rel(i)`
  - ⇒ `CardFan.Wpx(i) / CardFan.FrontWpx ≡ _scl[i] / 250`
- 原版数据（`Unity/MyGame/Assets/CardPresentation/Resources/misc_clips.json` · `Card Display Open` · `m_LocalScale`，五槽各 2 键）：
  - 首键（t=0）：`210 / 210 / 210 / 210 / 210`
  - 末键（t=length）：`250 / 232.9537 / 221.5926 / 204.5463 / 181.8148`
- 日志实得 / 期望（`:75782 / 75854 / 75926 / 75998 / 76070`）：
  | 槽 | 实得（末键/250） | 期望（末键/210） |
  |---|---|---|
  | 0 | 1.000 | 1.190 |
  | 1 | 0.932 | 1.109 |
  | 2 | 0.886 | 1.055 |
  | 3 | 0.818 | 0.974 |
  | 4 | 0.727 | 0.866 |
  两列逐行相除恒为 `1.190476` ⇒ 只有一个分母错了，与曲线内容无关。
- 旁证：同段的 `A4a`（起点五槽都 = 210）**绿**，`A5[*]x / y / r` **全绿** ⇒ 曲线转录、`CardFan` 的 x/y/rot 都对，只有这一条的分母选错。
- 该断言文件 `B107_R3c_Assert.cs` 是**本会话新建、未提交**（`git status` = `??`）⇒ 这是**新断言自身的错**，不是实现回归。

### 能从原版数据独立复算的期望公式

```
rel_i = scaleEnd(槽 i) / scaleEnd(槽 0)      // 两端都取 clip 末键（t = length），全部来自 misc_clips.json
```
代入上表末键：`1.0 / 0.9318 / 0.8864 / 0.8182 / 0.7273`，与实得逐位相同。

### 最小修法（可粘贴）

在 `SectionA_CardDisplayOpen()` 的 A5 循环**之前**（例如 A4 之后）加：

```csharp
        // 🔴 2026-10-31（B107_D7 诊断）：原式 `cScl / ss.keys[0].v[0]` 的分母是【本槽的起点缩放】
        //   （键 0 = t0 = 210），而 `CardFan.Wpx/FrontWpx` 的分母是 `_scl[0]` =【槽 0 的终点缩放】250
        //   ⇒ 期望整体被放大 250/210 = 1.190476，A5[0..4]s 五条全红。
        //   判据（原版数据自证）：rel_i = 末键(槽 i) / 末键(槽 0)；⛔ 别改 CardFan，它和原版一比就对上。
        var s0Curve = B107MiscClips.FindCurve(clip, "m_LocalScale", SlotPathA(0));
        float s0End = (s0Curve != null && s0Curve.keys != null && s0Curve.keys.Length > 0)
                      ? s0Curve.keys[s0Curve.keys.Length - 1].v[0] : float.NaN;
```

再把 `:157-158` 替换成：

```csharp
            N(CardFan.Wpx(i) / CardFan.FrontWpx, cScl / s0End, 1e-4f,
              "A5[" + i + "]s 槽 " + i + " 相对倍率 = 末端缩放(i) / 末端缩放(槽 0)（"
              + cScl.ToString("F4") + " / " + s0End.ToString("F1") + "）");
```

### 还差什么

- 改完跑一次 `ShellScene.Run` 复核（纯数据断言，不需要真机/截图）。修好后这 5 条应从 ✗ 变 ✓。

---

## 失败 6 · `wf_prefabs_extra.bundle 能被 LoadFromFile 加载` —— δ（夹具前提不成立：同进程内加载顺序）

### 结论

**不是包坏了，也不是本会话 W22 / W16 / W29 改了产物**：
- 包体 `Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle` = **639206 B、mtime 2026-10-30 23:32**（今天没动过），字节断言 `:152287` **绿**。
- 08:33 那次全绿运行里同一只包的六条检查全绿（见下）。

真因是**同一进程内的加载顺序冲突**：本会话新接线的 `B107R3d` 在 `ShellScene.cs:4812`（早于 `B103W7b` 的 `:11093`）调用
`WarpforgeAnimatorBridge.TryGetClip(...)` → `EnsureLoaded()` → `AssetBundle.LoadFromFile(wf_prefabs_extra.bundle)`（`WarpforgeAnimatorBridge.cs:271`），
并**把句柄一直存在 `_bundle` 里**。之后 `B103W7bAssert.Run()` 对**同一文件**再 `LoadFromFile`（`B103W7b_Assert.cs:141`），
Unity 按「同一份内容不能加载两次」的规则返回 null。

### 证据

- `:77191` `[WarpforgeAnimatorBridge] 战场动画小包 9/9 就绪（自载 0 · 复用已加载包 9）`，栈里是 `B107R3d_Assert:SectionB_BridgeEvidence (:220)`
  ⇒ 这是本进程**第一次**执行 `EnsureLoaded`（此前后文无任何 bridge 日志，也**没有**「加载不出」告警）⇒ 主包是**它**加载成功的。
- `:77207` `B107R3d ✓ B1 … TryGetClip("Dark Angels Void Combat animations") 取得到` ⇒ 包确实活着、句柄有效。
- `:152301` Unity 原文：`The AssetBundle 'StreamingAssets\WarpforgeVFX\wf_prefabs_extra.bundle' can't be loaded because another AssetBundle with the same files is already loaded.`
- `:152325` 红：`B103W7b ✗ wf_prefabs_extra.bundle 能被 LoadFromFile 加载`。
- 基线：`_tmp_view/host2_ShellScene.log`（2026-10-31 02:33）里 `grep -c B107R3d` = **0**（新断言未接线），
  同一只包六条全绿：`:148841`（bytes）`:148868`（load ✓）`:148908`（55 renderers）`:148922`（57 slots）`:148936`（0 null）`:148950`（0 bad shader），
  总计 `3685 通过 / 0 失败`（`:153202`）。
- 新接线是**未提交**的一行（`git diff ShellScene.cs`：`@@ -4811,0 +4812 @@`，`+ B107W15_Assert…; B107R3c_Assert.Run(); …; B107R3d_Assert.Run();`）。
- 生产侧同一坑早有对策：`WarpforgeAnimatorBridge.cs:219-232`（null ⇒ 去 `GetAllLoadedAssetBundles()` 捞）、`ScenarioBlendables.cs:1390-1401`（同一条兜底）。
  本断言没写这条兜底，所以第一次遇到「别人先加载」就判红。

### 比 1 条红更糟的连带影响

`B103W7b_Assert.cs:146-147` 在 `b == null` 时直接 `continue` ⇒ 这只包的 **4 条后续断言全部静默跳过**
（渲染器 55 / 材质槽 57 / 空材质槽 0 / shader 坏 0）。
所以修好后不止「6→1」，总断言数还会 **+4**（这几条在基线里都是绿的）。

### 最小修法（可粘贴）

1) `Pack` 增加一个「容器探针键」字段（键名逐字取自四只包 `m_Container`，UnityPy 只读核对过）：

```csharp
    struct Pack { public string Name; public long Bytes; public int Renderers; public int Slots; public string Probe; }
    static readonly Pack[] Packs =
    {
        new Pack { Name = "wf_menus_extra.bundle",        Bytes = 377660, Renderers = 36, Slots = 36, Probe = "Booster Pack Open Window" },
        new Pack { Name = "wf_prefabs_extra.bundle",      Bytes = 639206, Renderers = 55, Slots = 57, Probe = "Vanguard Frame Animated VAT" },
        new Pack { Name = "wf_battleshared_extra.bundle", Bytes =  88489, Renderers = 13, Slots = 13, Probe = "TurnTimerTickPulse" },
        new Pack { Name = "wf_mainmenu_extra.bundle",     Bytes =  81775, Renderers = 66, Slots = 68, Probe = "Wildcard_Use_Button 1 Common" },
    };
```

2) 把加载块（`:140-147`）改成「先 LoadFromFile，null 就**认领已加载包**」：

```csharp
            AssetBundle b = null;
            bool owned = true;
            try { b = AssetBundle.LoadFromFile(path); }
            catch (Exception e) { Debug.Log(P + "  🔴 LoadFromFile 抛：" + e.Message); }
            if (b == null)
            {
                // 同一份内容已在进程里加载过时 Unity 拒绝第二次 LoadFromFile（返回 null）——
                // 这不是包坏了：**认领已加载的那只、继续跑材质槽检查**（生产侧同一条兜底：
                // `WarpforgeAnimatorBridge.EnsureLoaded` / `ScenarioBlendables.EnsureClipBundle`）。
                b = FindLoadedWith(p.Probe);
                owned = false;
                if (b != null)
                    Debug.Log(P + "  ⓘ " + p.Name + " 的 LoadFromFile 返回 null（同内容已在进程里，认领 `"
                              + b.name + "` 继续检查）");
            }
            Check(b != null, p.Name + " 可用（LoadFromFile 或认领已加载包；null = 包坏了 / 同内容未在进程里）");
            if (b == null) continue;
```

3) 循环末尾 `:232` 改成只在「自己加载的」上卸载：

```csharp
            if (owned) b.Unload(false);   // 借来的句柄是别人的，别卸
```

4) 类里加辅助方法：

```csharp
    /// <summary>在**已加载的**包里按容器键找一只（判据 = 生产侧 `WarpforgeAnimatorBridge.FindLoadedWith`）。
    /// 只问容器/资产名表，**不实例化、不挑类型**；找不到返回 null。</summary>
    static AssetBundle FindLoadedWith(string probe)
    {
        if (string.IsNullOrEmpty(probe)) return null;
        string needle = probe.ToLowerInvariant();
        foreach (var lb in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (lb == null) continue;
            try
            {
                if (lb.Contains(probe)) return lb;
                var names = lb.GetAllAssetNames();
                for (int n = 0; n < names.Length; n++)
                    if (names[n] == needle || names[n].EndsWith("/" + needle + ".prefab")) return lb;
            }
            catch (Exception) { }
        }
        return null;
    }
```

### 考虑过但不推荐的替代法

- **只把 `B107R3d` 挪到 `B103W7b` 之后**：依赖 `Unload(false)` 之后能否再加载同一文件（未实测），而且以后任何更早的 bridge 调用都会把这条红带回来 —— 脆弱。
- **`b == null` 直接判绿**：会连「包坏了」一起放行，还会继续静默跳过 4 条真检查（本项目「不许静默失败」红线）。

### 还差什么

- `AssetBundle.Contains(probe)` 这条**没有实跑过**（生产侧 `WarpforgeAnimatorBridge.HasAsset:336-339` 用的就是 `Contains` + `GetAllAssetNames` 两层，先 `Contains`，所以置信度高，但以一次实跑为准）。
- 改完跑一次 `ShellScene.Run`：预期 `3946 + 1 + 4 = 3951 通过 / 0 失败`（5 条 A5 + 1 条加载 + 4 条被跳过的检查）。

---

## 附：两条共同的「别再犯」

1. **断言里的分母/分子必须与本工程常量的口径逐字对齐**（这里是「起点缩放」vs「终点缩放」）——
   且期望值要能从**原版数据独立复算**，⛔ 不要从被测常量反推。
2. **同一进程内的多宿主断言会共享 `AssetBundle` 进程状态**：任何断言里对 `LoadFromFile` 的第一次调用都会让后面同文件的调用返回 null。
   断言要么**声明自己必须先跑**、要么照生产侧写「认领已加载包」的兜底 —— ⛔ 不能把「别人先加载」判成失败。
