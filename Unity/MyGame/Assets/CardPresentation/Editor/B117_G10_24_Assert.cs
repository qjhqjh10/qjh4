// B117_G10_24_Assert.cs — 执行代理 `B117_G10_24`（战斗域第 24 部分「输入与手感」）的断言
//
// ============================ 它盯的是什么 ============================
// 本部分 5 个文件里，**此前一条断言都没有**的那两块（2026-11-02 现读 `Editor/*.cs` 全库 grep 确认）：
//   · `Battle/TapParticleController.cs` —— 点战场地面的**触发器**（掩码 / hits 上限 / 法线预偏移）
//   · `Battle/TapHitMaterialDefinition.cs` —— 命中后**播粒子+音**的定义（含嵌套 `ParticleCollisionDefinition`）
//   · `Core/CardButtons.cs` —— 卡片详情窗下缘两颗钮「建不建」的唯一判据
//   ⛔ 不重复：`TouchInputManager` 已由 `Editor/BattleScene.cs` 的 **A463** 段 10 条覆盖（含桌面路 /
//      forceMobileInput 两层门 / `.y` 滚轮 / 捏合符号 / 单指 deltaPosition）；
//      `CardFeel` 的补间由 `B103R5_Assert` / `B104_CardFeel_Assert` 覆盖。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `资料/完全复刻/判据/Tap族_地面点击反馈.md` §1（路由）· §2.2（213 实例/掩码）· §3.3（PlayParticle 149 行）
//     · §4（八道闸）· §8（一页速查：**点棋盘正中央不该有反馈**）
//   · 反编译原文（第一权威，逐行读过）：
//       `d:/2/tools/decomp_full/TapHitMaterialDefinition__Awake.c`（**一句：`enabled = false`**）
//       `d:/2/tools/decomp_full/TapHitMaterialDefinition__Update.c`（11 行：减 dt，`<= 0` 关自己）
//       `d:/2/tools/decomp_full/TapHitMaterialDefinition.ParticleCollisionDefinition__PlayParticle.c`（149 行）
//       `d:/2/tools/decomp_full/TapParticleController__Update.c`（385 行，0 条 `//IL_` 注记）
//       `d:/2/tools/decomp_full/AudioCue__{CanPlay,ConfigAudioSource}.c`
//   · 常量实读（`工具/read_literal.py`，4 字节 float）：
//       `0x1834b2dc4` = **0.1**（法线预偏移）· `0x1834b2d94` = **3.40282e+38 = float.MaxValue**（射线长度）
//   · 掩码：原版 `collisionMask.m_Bits = 1073744897`，**13 个战场场景逐份同值**
//
// ============================ 怎么用 ============================
//   `B117G1024Assert.Run();`   （静态类 + 无参 `Run()`；宿主由调度台接，本文件不改任何 `Editor/*Scene.cs`）
//   本文件**不建持久资产**：建出来的 GameObject / 粒子实例在 `finally` 里全部 `DestroyImmediate`，
//   并还原 `WFSoundPlayer.Enabled` 与计数、清掉 `ParticleCollisionDefinition` 的两张静态表。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A3 ← 把 `CollisionMask` 改回旧的 `0x40000001`（丢掉 layer 10/11）⇒ A1 + A3 + A3b + A4 全红。
//             🔴 这一格是判据正本**算错过**的地方（§2.2/§8 写「0x40000001 = 0+30，打不到棋盘」），
//             所以它必须是灭自证：序列化值 `1073744897` 十进制的真身是 **0x40000C01**（含 10/11）。
//   · A4b ← 按**名字**把 layer 12 `ParticleTapCollider` 加进掩码（原版没有这一位）⇒ A4b 红。
//   · E1  ← **建好的战场里一个 Tap 定义都打不到**（宿主层不在掩码里）⇒ E1 红 ——
//           今天就是这样（`ArenaBuilder` 的整树刷层把宿主刷回 layer 8，见 §E 的注释）。
//   · C1  ← 把「audio 无效 ⇒ 整条 return」挪到 `Instantiate` **之后** ⇒ 缓存 +1 ⇒ C1 红。
//   · C4  ← 把「按 prefab 缓存一件反复挪」改成「每次 `Instantiate`」⇒ 实例不是同一个、缓存 +1 ⇒ C4 红。
//   · C5  ← 把 `OnDestroy` 的「整表清空」"优化"成「只删自己那一项」⇒ 另一件仍留在表里 ⇒ C5 红。
//   · B3  ← 把 `Update` 的 `remaining <= 0` 写成 `remaining == 0` ⇒ −1 那一档关不掉 ⇒ B3 红。
//   · D2  ← 把 `HasTextButton` 的 `type` 白名单加上 `"defence"`（原版 `cardType == 0 || == 10` 之外）
//           ⇒ D2 红。
//
// ============================ 🔴 本文件**不覆盖**的 ============================
//   · `TapParticleController.Tick` 的**八道闸**（battleStage / 模态窗 / EventSystem / 指针在 UI / 按下沿 /
//     相机 / 射线 / 最近那条没定义）—— 它要 `BattleDriver` + 真实射线 + `Mouse.current`，
//     批处理里三样都没有 ⇒ **如实记「未覆盖」**，⛔ 不写成「已覆盖」。
//   · 粒子**真的播出来了**（视觉）与音效**真的响了**（听觉）—— 那要真机（`资料/真Play待验清单.md`）。
//   · ⚠️ **E 组要求宿主里战场已建好**（`BattleScene.Run`）。没有战场 ⇒ E1 会红并写明「前提不成立 ⇒ 未验」，
//     那是**如实**（⛔ 不是缺陷、也⛔ 不能静默跳过）。
using UnityEngine;
using WarpforgeVFX;
using CardPresentation;

public static class B117G1024Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B117G1024";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5")
                  + "；实得 " + got.ToString("F5") + "）");
    }

    // ==========================================================================================
    //  原版字面量（⛔ 别读我们自己的常量反推 —— 那就是拿实现证明实现）
    //    · 掩码 `1073744897` = **`0x40000C01`**：`assets_full` 13 个 `battlearena*` 场景里的
    //      `TapParticleController.collisionMask.m_Bits`，逐份现读同值（= layer 0 / 10 / 11 / 30）
    //      🔴 **判据正本 §2.2/§8 把这一格算错了**（写成「0x40000001 = 0+30 ⇒ 打不到棋盘」）⇒ 以序列化值为准。
    //    · `RaycastHit[3]`：`TapParticleController__ctor.c` 里 `FUN_1803f37c0(...,3)`
    //    · `0.1`：`0x1834b2dc4`（`read_literal.py` 现读）
    // ==========================================================================================
    const int OriginalCollisionMask = 0x40000C01;
    const int OriginalMaxHits = 3;
    const float OriginalNormalOffset = 0.1f;

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        bool wasSoundEnabled = WFSoundPlayer.Enabled;

        var temps = new System.Collections.Generic.List<Object>();
        var instances = new System.Collections.Generic.List<Object>();

        try
        {
            // 静态表先清干净（跨趟不残留：这张表是 `static`，上一次跑漏了清就会污染基线）
            TapHitMaterialDefinition.ParticleCollisionDefinition.ClearCache();
            int cache0 = TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest;

            SectionE_Reachability();          // ⚠️ 必须**第一个**跑：它数场景里的定义，本文件自己建的临时件会被数进去（自证）
            SectionA_ControllerConstants(temps);
            SectionB_DefinitionLifecycle(temps);
            SectionC_PlayParticle(temps, instances, cache0);
            SectionD_CardButtons();
        }
        finally
        {
            // ---- 还原：静态表 / 音效开关 / 建出来的对象 ----
            TapHitMaterialDefinition.ParticleCollisionDefinition.ClearCache();
            WFSoundPlayer.Enabled = wasSoundEnabled;
            for (int i = instances.Count - 1; i >= 0; i--)
                if (instances[i] != null) Object.DestroyImmediate(instances[i]);
            for (int i = temps.Count - 1; i >= 0; i--)
                if (temps[i] != null) Object.DestroyImmediate(temps[i]);

            Debug.Log(Tag + " · 合计：" + (_pass - pass0) + " 通过 / " + (_fail - fail0) + " 失败"
                    + "（缓存已清：" + TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest
                    + " 件 · `WFSoundPlayer.Enabled` 已还原 = " + WFSoundPlayer.Enabled + "）");
        }
    }

    // ==========================================================================================
    //  E. 可达性：「建好的战场里，到底有没有一条定义能被射线打到」
    //
    //  🔴 为什么单开这一组（2026-11-02 现读查出）：**今天一条都打不到**。
    //     `ArenaBuilder.BuildOneArenaPrefab` 的顺序是
    //         `BuildContent(...)`（它在 `:3145` 调 `BuildTapHits`，那里 `go.layer = e.layer;` 照原版设 0/11）
    //       → **然后**在 `:758-759` 把**整棵树**刷成 `ArenaSlayer.Layer = 8`（3D 相机只画 8）
    //     ⇒ 宿主层全部被刷回 **8**，而掩码是 0/10/11/30 ⇒ **掩码 ∧ 宿主层 = 0**。
    //     实测产物：`Resources/ArenaPrefabs/battlearena{1,2,3,…}.prefab` 里 `m_Layer` 只有 8（+1 颗 Volume 在 0），
    //     `tapHits` 的 166 条宿主（153×layer0 + 13×layer11）**一个都没保住**。
    //     ⚠️ `ArenaBuilder.VerifyPrefabTapHits` 只数组件处数、**不数层** ⇒ 这条一直是隐形的。
    //     修法要调度台裁定（两条路都动别人的文件）：
    //       ① 把整树刷层挪到 `BuildTapHits` **之前**（或刷完再把宿主层设回去），并让**战场相机**也渲 0/10/11
    //          —— 那会把「手牌层 0」也画进战场相机（`CardView.SetLayer(0)` 飞行段），要先评估；
    //       ② 保留层 8、把 bit8 加进掩码 —— 🔶 自己设计（原版掩码没有 8），且会让**整片战场几何**都参与射线。
    // ==========================================================================================
    static void SectionE_Reachability()
    {
        Debug.Log(Tag + " ——— E 组：建好的战场里，Tap 定义到底打不打得到（宿主层 ∧ 掩码）———");

        var defs = Object.FindObjectsByType<TapHitMaterialDefinition>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        Check(defs != null && defs.Length > 0,
              "E0 （前提）本宿主里有建好的 `TapHitMaterialDefinition`（本文件要跑在 `BattleScene.Run` 里："
            + "战场由 `ArenaRuntimeLoader` 实例化）—— 没有战场 ⇒ E1 无从验，这里如实报红（⛔ 不静默跳过）"
            + "（实得 " + (defs == null ? 0 : defs.Length) + " 个）");
        if (defs == null || defs.Length == 0) { Debug.Log(Tag + " · E1 未验（前提不成立，见 E0）"); return; }

        int mask = TapParticleController.CollisionMask;
        int reachable = 0;
        var hist = new System.Collections.Generic.SortedDictionary<int, int>();
        for (int i = 0; i < defs.Length; i++)
        {
            if (defs[i] == null) continue;
            int L = defs[i].gameObject.layer;
            int c; hist.TryGetValue(L, out c); hist[L] = c + 1;
            if ((mask & (1 << L)) != 0) reachable++;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var kv in hist) sb.Append("layer").Append(kv.Key).Append("×").Append(kv.Value).Append(' ');

        Check(reachable > 0,
              "E1 🧨灭自证 至少**一条** Tap 定义的宿主在射线掩码里 —— 否则整个「点地面反馈」在实机上是死的"
            + "（可打的 " + reachable + " / 共 " + defs.Length + " · 宿主层分布 " + sb
            + "· mask = 0x" + mask.ToString("X8") + "）。"
            + "⚠️ 若是 0 且宿主层全是 8 ⇒ 那是 `ArenaBuilder` 整树刷层的顺序问题（见本组头注），⛔ 不是掩码错");
    }

    // ==========================================================================================
    //  A. `TapParticleController` 的序列化常量与掩码（纯静态，零副作用）
    // ==========================================================================================
    static void SectionA_ControllerConstants(System.Collections.Generic.List<Object> temps)
    {
        Debug.Log(Tag + " ——— A 组：`TapParticleController` 掩码 / hits 上限 / 法线预偏移 ———");

        // 🔶 自己设计（非原版）：本工程 arena prefab 整树刷 layer 8（`ArenaSlots.ArenaLayer`）、3D 相机只画 8
        //    ⇒ 原版那套 0/11 在本地渲不出来，掩码必须额外认 bit8。调度台 2026-11-02 裁定（方案②）。
        //    实测依据：165 颗 tap 宿主里 109 颗带 MeshRenderer（看得见的道具），改回 0/11 会被 3D 相机丢掉。
        const int OurExtraLayerBit = 1 << 8;
        Check(TapParticleController.CollisionMask == (OriginalCollisionMask | OurExtraLayerBit),
              "A1 `CollisionMask` = 原版 `0x40000C01` | bit8(`ArenaLayer`) = **0x40000D01**"
            + "（🔶 自己设计：arena prefab 整树刷 8、3D 相机只画 8 ⇒ 掩码必须认 8；调度台 2026-11-02 裁定）"
            + "（期望 " + (OriginalCollisionMask | OurExtraLayerBit) + "；实得 " + TapParticleController.CollisionMask + "）");

        int mask = TapParticleController.CollisionMask;
        Check((mask & (1 << 0)) != 0 && (mask & (1 << 30)) != 0,
              "A2 掩码**含** layer 0（Default，木桶/栅栏/发电机那一批）与 layer 30（原版 `GroundCollider`）"
            + "（实得 mask = 0x" + mask.ToString("X8") + "）");

        // 🧨 灭自证：原版**是**打得中棋盘的。`CardLowBoardLimit` 在原版是 layer 11（`BoardCardLimit`），
        //    而序列化掩码 `1073744897` 含 bit11 ⇒ 点棋盘空地 ⇒ `Default Tap Floor` + `Tap Hit General`。
        //    🧨 谁把这里改回 `0x40000001`（旧值，十进制算错的那一版）⇒ 本行立刻红。
        Check((mask & (1 << 11)) != 0,
              "A3 🧨灭自证 掩码**含** layer 11（`BoardCardLimit`）—— 棋盘那颗 `CardLowBoardLimit` 就挂在这一层，"
            + "原版点棋盘空地**有**粒子+音（`Default Tap Floor` / `Tap Hit General`）；"
            + "把常量改回 `0x40000001` ⇒ 红（实得 mask = 0x" + mask.ToString("X8") + "）");

        Check((mask & (1 << 10)) != 0,
              "A3b 掩码**含** layer 10（`BoardElements`）—— 原版这一位用来让**场上的卡/棋盘件挡住地面反馈**"
            + "（最近那条没有定义 ⇒ 整帧不播，闸⑬不顺延）（实得 mask = 0x" + mask.ToString("X8") + "）");

        int bits = 0;
        for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) bits++;
        Check(bits == 5,
              "A4 🧨灭自证 掩码**恰好 5 位**（原版 0/10/11/30 四格 + 本工程 bit8；不是「多开一格试试」）"
            + "（实得位数 " + bits + " · mask = 0x" + mask.ToString("X8") + "）");

        Check((mask & OurExtraLayerBit) != 0,
              "A4c 掩码**含** bit8（`ArenaLayer`）—— 🔶 自己设计：本工程 tap 宿主全刷在 layer 8"
            + "（实得 mask = 0x" + mask.ToString("X8") + "）");

        // 🧨 另一条「看着很合理」的错法：原版 TagManager 里 layer 12 叫 `ParticleTapCollider`，
        //    名字太诱人 —— 但序列化掩码里 **没有它**（1073744897 的 bit12 = 0）。
        Check((mask & (1 << 12)) == 0,
              "A4b 🧨灭自证 掩码**不含** layer 12（虽然它叫 `ParticleTapCollider`，但原版掩码没有这一位 —— "
            + "按名字加进去就与原版不符）");

        Check(TapParticleController.MaxHits == OriginalMaxHits,
              "A5 `hits` 上限 = 原版 `new RaycastHit[3]`"
            + "（期望 " + OriginalMaxHits + "；实得 " + TapParticleController.MaxHits + "）");

        Near(TapParticleController.NormalOffset, OriginalNormalOffset, 1e-6f,
             "A6 粒子落点的法线预偏移 = 原版常量 `0x1834b2dc4` = 0.1");

        // RaycastNonAlloc 的**最长距离**：原版传的是 `0x1834b2d94` = float.MaxValue。
        // 这一格不是常量（在 `Tick` 里写死）⇒ 只能从代码里读不到，如实记在这里但不假装断言过。
        Debug.Log(Tag + " · A7（不是断言）`Physics.RaycastNonAlloc` 的 maxDistance 现读 = `float.MaxValue`，"
                     + "与原版 `0x1834b2d94`（read_literal = 3.40282e+38）一致 —— 它在 `Tick` 体内写死，"
                     + "本文件读不到，故只记录不判");
    }

    // ==========================================================================================
    //  B. `TapHitMaterialDefinition` 的生命周期（Awake 关自己 / Update 减冷却）
    // ==========================================================================================
    static void SectionB_DefinitionLifecycle(System.Collections.Generic.List<Object> temps)
    {
        Debug.Log(Tag + " ——— B 组：`TapHitMaterialDefinition` 的 `Awake` → `enabled=false` 与 `Update` 冷却 ———");

        var go = new GameObject("G10_24_Def_B");
        temps.Add(go);
        var def = go.AddComponent<TapHitMaterialDefinition>();
        // ⚠️ 编辑模式下 `AddComponent` 跑不跑 `Awake` 本工程没有定论（`A321` 订正）⇒ 显式补一次，⛔ 不赌生命周期。
        go.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

        Check(!def.enabled,
              "B1 原版 `Awake` 全文只有一句 `enabled = false`（`TapHitMaterialDefinition__Awake.c`）"
            + "⇒ 建出来**不参与每帧**（实得 enabled = " + def.enabled + "）");

        // B2：冷却没走完 ⇒ 仍然开着（原版 `Update`：`remaining -= dt; if (remaining <= 0) enabled = false;`）
        def.remainingNotPlayTime = 1000f;
        def.enabled = true;
        go.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
        Check(def.enabled && def.CoolingForTest && def.remainingNotPlayTime > 0f,
              "B2 冷却还剩很久 ⇒ `Update` 后仍然开着（原版那段 11 行只在 `<= 0` 才关自己）"
            + "（实得 enabled = " + def.enabled + " · remaining = " + def.remainingNotPlayTime.ToString("F3") + "）");

        // B3：🧨 灭自证 —— 负值也必须关（写成 `== 0` 就漏掉这一档）
        def.remainingNotPlayTime = -1f;
        def.enabled = true;
        go.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
        Check(!def.enabled,
              "B3 🧨灭自证 冷却已过（remaining = −1）⇒ `Update` 必须 `enabled = false`"
            + "（原版判据是 `<= 0`，写成 `== 0` 这一档就关不掉）"
            + "（实得 enabled = " + def.enabled + "）");
    }

    // ==========================================================================================
    //  C. `ParticleCollisionDefinition.PlayParticle`（原版 149 行）：
    //     ① 两条参考**任一无效 ⇒ 整条不播**（而且**在 Instantiate 之前**就返回）
    //     ② 粒子实例**按 prefab 缓存一件、反复挪**（⛔ 不是每次 Instantiate）
    //     ③ `parent == null` ⇒ 世界点 + 法线 × 0.1、`LookRotation(normal)`；非空 ⇒ 锚点**本地零位**
    //     ④ 音效打在**射线命中点**（不是加过偏移的那个点）
    // ==========================================================================================
    static void SectionC_PlayParticle(System.Collections.Generic.List<Object> temps,
                                      System.Collections.Generic.List<Object> instances, int cache0)
    {
        Debug.Log(Tag + " ——— C 组：`PlayParticle` 的两条无效闸 / 缓存复用 / 落点与朝向 ———");

        const string CueName = "Tap Hit Metal Light";   // 判据 §2.4 那张表里的一条（现读在 `animfx_sounds.json` 里）
        Check(WFSoundBank.HasCue(CueName),
              "C0 （前提）音效表里有 `" + CueName + "` —— 没有的话「有效 cue 那一档」根本走不到，"
            + "而它看起来会像「粒子没播」");

        // 两件「像 prefab」的粒子源（只要根上有 `ParticleSystem` 就够 —— 原版是 `GetComponent<ParticleSystem>()`）
        var psGoA = new GameObject("G10_24_TapPS_A"); psGoA.AddComponent<ParticleSystem>(); temps.Add(psGoA);
        var psGoB = new GameObject("G10_24_TapPS_B"); psGoB.AddComponent<ParticleSystem>(); temps.Add(psGoB);

        var defGo = new GameObject("G10_24_Def_C");
        temps.Add(defGo);
        var def = defGo.AddComponent<TapHitMaterialDefinition>();
        defGo.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
        var pd = def.particleDefinition;

        bool wasEnabled = WFSoundPlayer.Enabled;
        WFSoundPlayer.Enabled = false;            // 只记账不发声（不建 AudioSource、不出声），`Played` 照样涨
        WFSoundPlayer.ResetCounter();
        int played0 = WFSoundPlayer.Played;
        try
        {
            // ---- C1：有粒子、**没有音** ⇒ 整条不播，而且**缓存也不能涨**（音闸在 Instantiate 之前）----
            pd.particles = psGoA;
            pd.audio = "";
            pd.PlayParticle(new Vector3(1f, 2f, 3f), Vector3.right, null);
            Check(pd.lastInstance == null
               && TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest == cache0
               && WFSoundPlayer.Played == played0,
                  "C1 🧨灭自证 原版①：`particles` 有、`audio` 无效 ⇒ **整条 return**"
                + "（连粒子都不 Instantiate、缓存不涨、音不响）。把音闸挪到 Instantiate 之后 ⇒ 红"
                + "（实得 lastInstance = " + (pd.lastInstance == null ? "<null>" : pd.lastInstance.name)
                + " · 缓存 " + TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest
                + " / 基线 " + cache0 + " · Played +" + (WFSoundPlayer.Played - played0) + "）");

            // ---- C2：有音、**没有粒子** ⇒ 同样整条不播 ----
            pd.particles = null;
            pd.audio = CueName;
            pd.PlayParticle(new Vector3(1f, 2f, 3f), Vector3.right, null);
            Check(pd.lastInstance == null
               && TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest == cache0
               && WFSoundPlayer.Played == played0,
                  "C2 原版①（另一半）：`audio` 有、`particles` 空 ⇒ 同样整条 return"
                + "（实得缓存 " + TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest
                + " / 基线 " + cache0 + " · Played +" + (WFSoundPlayer.Played - played0) + "）");

            // ---- C3：两侧都有效 ⇒ 播一次：缓存 +1、落点 = 点 + 法线 × 0.1、朝向 = 法线 ----
            pd.particles = psGoA;
            pd.audio = CueName;
            var point = new Vector3(1f, 2f, 3f);
            var normal = Vector3.right;                      // (1,0,0)：`LookRotation` 无歧义
            pd.PlayParticle(point, normal, null);
            var first = pd.lastInstance;
            if (first != null) instances.Add(first.gameObject);
            Check(first != null
               && TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest == cache0 + 1,
                  "C3 两侧都有效 ⇒ 播一次、缓存 +1"
                + "（实得 lastInstance = " + (first == null ? "<null>" : first.name)
                + " · 缓存 " + TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest
                + " / 期望 " + (cache0 + 1) + "）");
            if (first != null)
            {
                Near(first.transform.position.x, point.x + normal.x * OriginalNormalOffset, 1e-4f,
                     "C3a 世界落点 = `position + normal × 0.1`（x 分量）");
                Near(first.transform.position.y, point.y, 1e-4f, "C3b 世界落点（y 分量，法线 y = 0 ⇒ 不加）");
                Check(Vector3.Angle(first.transform.forward, normal) < 0.01f,
                      "C3c 朝向 = `Quaternion.LookRotation(normal)`（原版那一句）"
                    + "（实得 forward = " + first.transform.forward.ToString("F3") + "）");
            }

            int played1 = WFSoundPlayer.Played;
            Check(played1 == played0 + 1,
                  "C3d 音效走 `Play3D(position, 0)` ⇒ 命中点那条路被请求了 1 次"
                + "（`AudioCue__Play3D` → `SoundManager.Play`；实得 Played +" + (played1 - played0) + "）");

            // ---- C4：🧨 再播一次 —— 必须**复用同一个实例**（原版：缓存一件、反复挪）----
            var anchorGo = new GameObject("G10_24_Anchor");
            temps.Add(anchorGo);
            anchorGo.transform.position = new Vector3(5f, 6f, 7f);
            pd.PlayParticle(new Vector3(9f, 9f, 9f), Vector3.up, anchorGo.transform);
            var second = pd.lastInstance;
            Check(second != null && ReferenceEquals(second, first)
               && TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest == cache0 + 1,
                  "C4 🧨灭自证 原版②：**同一 prefab 复用同一个粒子实例**（⛔ 不是每次 Instantiate）"
                + "（实得实例" + (ReferenceEquals(second, first) ? "相同" : "不同")
                + " · 缓存 " + TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest
                + " / 期望 " + (cache0 + 1) + "）");
            if (second != null)
            {
                Check(second.transform.parent == anchorGo.transform
                   && Vector3.Distance(second.transform.localPosition, Vector3.zero) < 1e-4f,
                      "C4a `parent` 非空 ⇒ 挂到锚点**本地零位**（与你点在哪儿无关：点的是 (9,9,9)）"
                    + "（实得 parent = " + (second.transform.parent == null ? "<null>" : second.transform.parent.name)
                    + " · localPosition = " + second.transform.localPosition.ToString("F3") + "）");
                Near(second.transform.position.y, 6f, 1e-3f,
                     "C4b 挂到锚点之后世界 y = 锚点 y（6）—— 不是点击点 y（9）");
            }

            // ---- C5：🧨 `OnDestroy` ⇒ **整表清空**（原版 `cacheParticles.Clear()`）----
            //     先让缓存里有两件（第二件用 psGoB），再销毁一个**不相干**的定义 ⇒ 两件都要没。
            pd.particles = psGoB;
            pd.PlayParticle(new Vector3(0f, 0f, 0f), Vector3.right, null);
            var third = pd.lastInstance;
            if (third != null) instances.Add(third.gameObject);
            int twoEntries = TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest;
            Check(twoEntries == cache0 + 2,
                  "C5 （前提）两件不同 prefab ⇒ 缓存里有两件（实得 " + twoEntries + "）");
            var otherGo = new GameObject("G10_24_Def_Destroy");
            temps.Add(otherGo);
            otherGo.AddComponent<TapHitMaterialDefinition>();
            otherGo.SendMessage("OnDestroy", SendMessageOptions.DontRequireReceiver);
            Check(TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest == 0,
                  "C5a 🧨灭自证 原版③：**任意一个定义被销毁 ⇒ 整表清空**（`OnDestroy` → `cacheParticles.Clear()`）；"
                + "把它「优化」成只删自己那一项 ⇒ 这里还剩 1 件 ⇒ 红"
                + "（实得 " + TapHitMaterialDefinition.ParticleCollisionDefinition.CacheCountForTest + "）");
        }
        finally
        {
            WFSoundPlayer.Enabled = wasEnabled;
            TapHitMaterialDefinition.ParticleCollisionDefinition.ClearCache();
        }
    }

    // ==========================================================================================
    //  D. `CardButtons.HasTextButton` —— 原版 `showCardTextButton.SetActive(cardType == 0 || cardType == 10)`
    //     （0 = Minion、10 = Hero ⇒ 我们的 `type` 字符串 = `"unit"` / `"hero"`；
    //      映射出处 `RuleEngine/Core/CardDef.cs:6542-6543`：`10 → "hero"`、`0 → "unit"`）
    // ==========================================================================================
    static void SectionD_CardButtons()
    {
        Debug.Log(Tag + " ——— D 组：`CardButtons.HasTextButton` 的卡类白名单 ———");

        Check(HasTextButtonOfType("unit") && HasTextButtonOfType("hero"),
              "D1 「眼睛钮」对 **Minion(0) / Hero(10)** 建（我们的 `type` = `\"unit\"` / `\"hero\"`）");

        // 🧨 灭自证：白名单里只要多一个卡类就红（战术卡 / 防御卡 / 督军以外的一切都不建）
        Check(!HasTextButtonOfType("tactic") && !HasTextButtonOfType("defence")
           && !HasTextButtonOfType("") && !HasTextButtonOfType(null),
              "D2 🧨灭自证 「眼睛钮」对 **战术卡 / 防御卡 / 空类型** 一律不建"
            + "（原版判据只有 `cardType == 0 || == 10` 两个值）");
    }

    static bool HasTextButtonOfType(string type)
    {
        var d = new CardData { type = type };
        return CardButtons.HasTextButton(d);
    }
}
