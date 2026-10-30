// B103W7b_Assert.cs — 第一百零三会话 · 波 1 · 执行代理 `B103_W7b`（2026-10-30 · 资源/工具线）
//
// ============================ 它盯的是什么 ============================
// 同族的另外 4 个 `repack_tree` 产物，按**卡包那一套**（`B103_W7` / `B102_D10`）重新定级并修完之后，
// 把它们**运行时真的能不能取到材质**钉成硬断言：
//   `wf_menus_extra.bundle`（卡包打开那扇窗 + 4 件开包粒子 + 领奖粒子）
//   `wf_prefabs_extra.bundle`（阵亡爆散体 / Vanguard VAT / 环境 clip 的宿主）
//   `wf_battleshared_extra.bundle`（回合倒计时脉冲 `TurnTimerTickPulse`）
//   `wf_mainmenu_extra.bundle`（主菜单 8 个 `Wildcard_Use_*`）
//
// 改前（`B97_W32` §7.2 的旧尺子，只统计「包内找不找得到」）：材质槽悬空 **29 / 31 / 13 / 58**，
// 材质→shader 悬空 **6 / 7 / 0 / 3**。
// 🔴 旧尺子把「指向别的**已加载** bundle」也算成缺陷 ⇒ 本件的**新尺子**只算「运行时真解不到」的：
//   那 4 只产物悬空的材质槽**只指向 4 个源包**（`duplicateassetisolation` 100 条 ·
//   `battlesharedresources` 12 · `menusharedresources` 1 · `fonts` 5），而这 4 只包**我们一只都没发**
//   ⇒ **29/31/13/58 全部是真悬空**（逐条 pid 级复核：本机任何 bundle 里都没有那些 pid）。
//
// 修法（⛔ 没写在这一支里，只在工具 + 产物）：`工具/extract_missing_shaders.py` 的 `BOOSTER_IMPORTS`
//   扩成**5 个产物共用的材质池**（判据 = 它们要的材质**同一批源包**）⇒ 4 只产物也走
//   `merge_imports_into_bundle` 的**独立依赖包**路（照原版分包；`B102_D10` 已判死「并进同包」）。
//
// ============================ 怎么跑 ============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B103W7bAssert.Run -logFile d:/4/_tmp_view/b103w7b.log
//   判绿 = 小结那行 `失败 0`。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `BOOSTER_IMPORTS` 里新加的那 13 个材质删掉（本件加的）⇒ 第 2 段的 `空材质槽` 立刻回到
//     29 / 31 / 13 / 58（这一条就是旧尺子那四个数）。
//   · 把 6 只依赖包的文件名改掉（`wf_booster_mats*` / `wf_booster_fx*`）⇒ `DepBundles` 一只都加载不上 ⇒
//     `LoadAllAssets<Renderer>()` 里的 `sharedMaterials` 全是 null ⇒ 同一条红（且第 1 段会点名缺哪只）。
//   · 把依赖包**并回主产物**（旧的兄弟 CAB 形态）⇒ Unity 整包行为见 `B102_D10`（200/172 那组读数），
//     本支第 2 段同样会红。
//   · 把产物换成 `--prefabs` 之前那一份（`_tmp_view/b103w7b/backup/`）⇒ 第 3 段的字节数红。
//   · 🆕 **把 `工具/extract_missing_shaders.py` 的 `PREFAB_TARGETS` 里 `VanguardIdleEffect` 删掉再重打**
//     ⇒ 根没登记进容器 ⇒ `wf_prefabs_extra` 的读数回到 **30 / 32** ⇒ 第 2 段两条前提断红
//     （2026-10-30 `B103_W7c` 真跑过那一版，读数与 `B103_D4` 的分诊逐字相同；同时也把字节数改了）。
//
// ⚠️ **非空转保险**：`renderers > 0` 与 `slots == 期望` 是**前提断言** —— 包被拒收 / API 取不到东西时
//    旧写法（`nullSlots == 0`）会空转绿（本仓踩过 3 次），所以先断前提再断结论。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class B103W7bAssert
{
    const string P = "B103W7b ";

    /// <summary>6 只依赖包（**与 `工具/extract_missing_shaders.py` 的 `BOOSTER_IMPORTS[].dep` 逐字相同**；
    /// 运行时的第三份副本在 `CardPresentation/Core/BoosterPackAssets.cs` 的 `DepBundleNames`）。
    /// ⚠️ 这张表**三处各有一份**（工具 / 本支 / 运行时）—— 改一处必须三处一起改。</summary>
    static readonly string[] DepBundles =
    {
        "wf_booster_matsdup.bundle", "wf_booster_matsbs.bundle", "wf_booster_matsmenu.bundle",
        "wf_booster_matsfont.bundle", "wf_booster_fxshaders.bundle", "wf_booster_fxbuiltin.bundle",
    };

    /// <summary>4 只产物 + 它们的实测期望值（2026-10-30 `--prefabs` 之后逐只静态读 + 本支运行时量）。
    /// `Slots` = 全部渲染器 `sharedMaterials` 数组项数之和（含 pid=0 的空项）；
    /// `Renderers` = 包内 `Renderer` 组件数。⚠️ 两者都随源包变，改动工具后先看日志里的实得值再钉。
    /// 🔴 **2026-10-30（`B103_D4` 复核）**：这两个数**以静态读为准**（UnityPy 把包里每一个 `Renderer`
    /// 序列化对象都数出来，不看容器登记）；运行时的量法见 `Run()` 里那三条路。
    /// ✅ **2026-10-30（`B103_W7c`）**：`wf_prefabs_extra` 那棵 26 个渲染器的根 `VanguardIdleEffect`
    /// **已登记进容器** ⇒ 两条路都给 55/57（改前只数得到 30/32 —— 那 26 个当时在容器外）。</summary>
    struct Pack { public string Name; public long Bytes; public int Renderers; public int Slots; }
    static readonly Pack[] Packs =
    {
        new Pack { Name = "wf_menus_extra.bundle",        Bytes = 377660, Renderers = 36, Slots = 36 },
        // 🔴 `Bytes` 2026-10-30（`B103_W7c`）639212 → **639206**：根登记进容器（+2 条容器项 / 预载 12→13）
        //   之后 LZ4 压出来反而小了 6 B；同一次真打里 `wf_battleshared_extra` 也 88494 → **88489**
        //   —— 后者的原因**与我这条改动无关**（把 `VanguardIdleEffect` 那行临时去掉再真打，它仍是 88489：
        //   上一次真打把 `wf_booster_matsbs` 从 360137 重写成 360130 带出来的漂移）。
        new Pack { Name = "wf_prefabs_extra.bundle",      Bytes = 639206, Renderers = 55, Slots = 57 },
        new Pack { Name = "wf_battleshared_extra.bundle", Bytes =  88489, Renderers = 13, Slots = 13 },
        new Pack { Name = "wf_mainmenu_extra.bundle",     Bytes =  81775, Renderers = 66, Slots = 68 },
    };

    static int _ok, _bad;
    // 🔴 2026-10-30（调度台接线）：原来只有自带 `_ok/_bad` + 小结日志 ⇒ **失败不进宿主合计、不影响退出码**（= 静默）。
    //   补两个只读口，让 `ShellScene` 的聚合行能把它们加进 `_sink`。
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 4 只 repack_tree 产物的材质槽（新尺子：运行时真解不到才算）= 开始 ===");

        // ---------------------------------------------------------------- 0 · 依赖包先就位
        string dir = Path.Combine(Application.streamingAssetsPath, "WarpforgeVFX");
        int depOk = 0;
        var depHandles = new List<AssetBundle>();
        for (int i = 0; i < DepBundles.Length; i++)
        {
            string path = Path.Combine(dir, DepBundles[i]);
            if (!File.Exists(path))
            {
                Debug.Log(P + "  🔴 依赖包缺失：" + path
                          + "（重打：`python -I -X utf8 工具/extract_missing_shaders.py --prefabs`）");
                continue;
            }
            AssetBundle b = null;
            try { b = AssetBundle.LoadFromFile(path); }
            catch (Exception e) { Debug.Log(P + "  🔴 依赖包 " + DepBundles[i] + " 加载抛异常：" + e.Message); }
            if (b == null)
            {
                // 同一份内容已在进程里加载过时 `LoadFromFile` 返回 null（Unity 限制）——
                // 这时它**仍然在进程里可用**（别人加载的），不算失败，但要出声。
                Debug.Log(P + "  ⓘ 依赖包 " + DepBundles[i] + " 的 LoadFromFile 返回 null"
                          + "（同内容已在进程里加载过 ⇒ 引用照样解得出）");
                depOk++;
                continue;
            }
            depHandles.Add(b);
            depOk++;
        }
        Check(depOk == DepBundles.Length,
              "6 只依赖包都可用（实得 " + depOk + "/" + DepBundles.Length
              + "；缺任一只 ⇒ 下面各段的空材质槽会回来）");

        // ---------------------------------------------------------------- 1~2 · 逐只产物
        for (int i = 0; i < Packs.Length; i++)
        {
            Pack p = Packs[i];
            string path = Path.Combine(dir, p.Name);
            Debug.Log(P + "---- " + p.Name + " ----");

            if (!File.Exists(path)) { Check(false, p.Name + " 在盘上"); continue; }
            long size = 0;
            try { size = new FileInfo(path).Length; } catch (Exception e) { Debug.Log(P + "  取大小抛：" + e.Message); }
            Check(size == p.Bytes, p.Name + " 字节数 == " + p.Bytes + "（实得 " + size + "）");

            AssetBundle b = null;
            try { b = AssetBundle.LoadFromFile(path); }
            catch (Exception e) { Debug.Log(P + "  🔴 LoadFromFile 抛：" + e.Message); }
            if (b == null)
                Debug.Log(P + "  🔴 `LoadFromFile` 返回 null —— 要么这台进程里已经加载过同一份内容"
                          + "（那就换一个宿主跑），要么包本身坏了");
            Check(b != null, p.Name + " 能被 LoadFromFile 加载（null = 同内容已在进程里 / 包坏了）");
            if (b == null) continue;

            // 取渲染器三条路（顺序 2026-10-30 定过两版，以下是**现行**口径）：
            //   ① `LoadAllAssets<Renderer>()` —— 走**容器登记**（`Renderer` 组件本身不是「主资产」，
            //      但挂在本包容器登记过的 GameObject 上的组件会被一起枚举出来）。
            //   ② `LoadAllAssets<GameObject>()`（GameObject 是主资产）+ 逐根
            //      `GetComponentsInChildren<Renderer>(true)` + **按实例去重**。
            //   ③ 容器键 + `GetComponentsInChildren` —— 前两条都取不到东西时的兜底。
            // 🔴 为什么留 ② 而不是只用 ①（现在**两条都给 55/57**，② 留着当「量法对不对」的自证）：
            //   `wf_prefabs_extra` 的 5 棵根里，`VanguardIdleEffect`（26 个渲染器）**改前不在
            //   `m_Container` 里**（容器键 `Vanguard Frame Animated VAT` 指的是它下面那个子物件）
            //   ⇒ 那时只走 ① 只能数到 30 / 32，而包里实际是 **55 / 57**。
            //   ✅ **2026-10-30（`B103_W7c`）已把根登记进容器**（判据：**原版容器登记的就是这个根**，
            //   键 = GUID `c53d2e222bd19854d9b34c23a3a0543f`；分诊 → `报告/B103_D4.md` §①，
            //   修法与取证 → `报告/B103_W7c.md`）⇒ ① 现在也数得到 55/57，② 与它相等（取大者 ⇒ 仍走 ①）。
            var renderers = new List<Renderer>(b.LoadAllAssets<Renderer>());
            string how = "LoadAllAssets<Renderer>";
            // 🔴 **2026-10-30 订正（调度台复跑实测）**：`LoadAllAssets<Renderer>()` 对 `wf_prefabs_extra` **不是 0、是 30**
            //   （只覆盖登记进容器的那批）⇒ 原来那句 `if (renderers.Count == 0)` 把更好的第二条路**整个跳过了**，读数卡在 30。
            //   ⇒ **无条件跑第二条路，取更大的那一份**（两条路应是子集关系；取大者同时可当「量法对不对」的自证）。
            {
                GameObject[] allGos = null;
                try { allGos = b.LoadAllAssets<GameObject>(); }
                catch (Exception e) { Debug.Log(P + "  `LoadAllAssets<GameObject>()` 抛：" + e.Message); }
                if (allGos != null && allGos.Length > 0)
                {
                    var viaGo = new List<Renderer>();
                    var seenGo = new HashSet<int>();
                    for (int g = 0; g < allGos.Length; g++)
                    {
                        if (allGos[g] == null) continue;
                        var rs = allGos[g].GetComponentsInChildren<Renderer>(true);
                        for (int r = 0; r < rs.Length; r++)
                            if (rs[r] != null && seenGo.Add(rs[r].GetInstanceID())) viaGo.Add(rs[r]);
                    }
                    if (viaGo.Count > renderers.Count)
                    {
                        renderers = viaGo;
                        how = "LoadAllAssets<GameObject> + GetComponentsInChildren（去重）";
                    }
                }
            }
            if (renderers.Count == 0)
            {
                how = "容器键 + GetComponentsInChildren（去重）";
                var seen = new HashSet<int>();
                string[] keys = b.GetAllAssetNames();
                for (int k = 0; k < keys.Length; k++)
                {
                    GameObject go = null;
                    try { go = b.LoadAsset<GameObject>(keys[k]); } catch (Exception) { }
                    if (go == null) continue;
                    var rs = go.GetComponentsInChildren<Renderer>(true);
                    for (int r = 0; r < rs.Length; r++)
                        if (rs[r] != null && seen.Add(rs[r].GetInstanceID())) renderers.Add(rs[r]);
                }
            }
            Debug.Log(P + "  取渲染器的口 = " + how + " · 实得 " + renderers.Count + " 个（期望 " + p.Renderers + "）");

            int slots = 0, nullSlots = 0, badShader = 0;
            for (int r = 0; r < renderers.Count; r++)
            {
                var ms = renderers[r].sharedMaterials;
                if (ms == null) continue;
                for (int m = 0; m < ms.Length; m++)
                {
                    slots++;
                    var mat = ms[m];
                    if (mat == null) { nullSlots++; continue; }
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader") badShader++;
                }
            }
            Debug.Log(P + "  渲染器 " + renderers.Count + " · 材质槽 " + slots
                      + " · 空材质槽 " + nullSlots + " · shader 坏 " + badShader);

            // 前提断言（非空转）：拿不到渲染器时下面两条会**空转绿**，所以先断前提。
            Check(renderers.Count == p.Renderers,
                  p.Name + " 渲染器数 == " + p.Renderers + "（实得 " + renderers.Count
                  + "；0 = 包被拒收 / API 取不到，后面几条不算数）");
            Check(slots == p.Slots, p.Name + " 材质槽数 == " + p.Slots + "（实得 " + slots + "）");
            // 结论断言：改前 29 / 31 / 13 / 58（旧尺子），现在是 0。
            Check(nullSlots == 0, p.Name + " 空材质槽 == 0（实得 " + nullSlots + "；改前 29/31/13/58）");
            Check(badShader == 0, p.Name + " shader 坏 == 0（实得 " + badShader
                  + "；改前 6/7/0/3 个 `m_Shader` 悬空）");

            b.Unload(false);   // 只卸自己这份（依赖包句柄在 `depHandles` 里，别一起卸）
        }

        Debug.Log(P + "=== 小结：" + _ok + " 通过 / " + _bad + " 失败 ===");
    }
}
