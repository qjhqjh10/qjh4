// B102PACK_Assert.cs — 执行代理 `B102_PACK`（账 `商店:345` 的真缺陷：卡包 prefab 两颗 `Army Text` 的 TMP 是 missing script）
//
// ============================ 它盯的是什么 ============================
//  卡包 17 棵 prefab 上那 34 颗 `Army Text` / `Army Text 2` 的 TMP 组件 `m_Script` 指向**外部 CAB `CAB-17b1b5f8…`**，
//  而全部 StreamingAssets bundle 里**都没有那个 CAB** ⇒ 运行时是 **missing script** ⇒
//  `GetComponentsInChildren<TextMeshPro>()` = 0（日志 `[BoosterPack] … texts=0`），`B98A5_Assert.cs:202` 那条前提因此是红的。
//  修法（本批 = ①）：`Shell/BoosterPackOpenWindow.cs` 的 `EnsureArmyTexts` 在两颗节点上**自己建世界空间 TMP**，
//  逐格值 = 原版 prefab 的序列化值（实读，⛔ 不是我们挑的）。
//  判据全文 → `资料/完全复刻/报告/B102_D2_ShellScene红分诊.md` §二·①。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 **接 `CardPresentation/Editor/ShopScene.cs` 的聚合行**（第 5882 行 `=== 合计 ===` 那句**之前**，**同一物理行末尾追加**，
//     ⛔ 别加行）：
//      B102PACK_Assert.Run(); _sink.Pass += B102PACK_Assert.Pass; _sink.Fail += B102PACK_Assert.Fail;
//  **为什么是 ShopScene 而不是 ShellScene**：
//    · 这两颗文本出现的窗是「`Booster Pack Open Window`」，而它**只在商店的购买链上出现** ——
//      `ShopTabPage.DoBuy` → `OpenBoosterPack`（`Editor/ShopScene.cs:2843-2900` 那一整段就是买一包 → 自动开包 → 逐层断言这扇窗）；
//    · `ShellScene` 只是把窗口类型登记进壳的注册表（`Editor/ShellScene.cs:7572`），**从不真的开一包**；
//      `B98A5_Assert` 之所以在 ShellScene，是它自建夹具（本文件也自建），与宿主是否开包无关。
//  ⚠️ `Run()` 自带夹具（不依赖宿主已建窗），所以接在 `ShellScene` 也跑得动；但**真路径在商店** ⇒ 推荐 ShopScene。
//
// ============================ 🔴 灭自证（A3：怎么才能红）============================
//  🧨 把 `EnsureArmyTexts` 里那句 `BuildArmyText(nodes[i])` 注释掉（= 回到「不建 TMP」的旧行为）
//     ⇒ 节点上 `GetComponent<TextMeshPro>()` 仍是 null ⇒ **A1 两条全红**，且 `armyTexts` 退回 0 颗 ⇒ **A2 全红**。
//  🧨 把 `BuildArmyText` 里的 `t.fontStyle` / `fontSizeMax` / `fontSizeMin` / 字体面 任一改掉
//     ⇒ **A3 红**（A3 断的就是「这颗 TMP 确实是我们按原版值建的那一颗」：字体面 = `Pragati-Regular SDF`、
//        `UpperCase`、`fontSizeMax 10`、`fontSizeMin` 按节点名 2 / 8）—— A1/A2 仍可能绿 ⇒ **A3 是独立的一面**。
//  🧨 把节点 `SetActive(true)` 硬写进产品代码（原版 17 棵全是 `m_IsActive: 0`）⇒ **A1 后半（保持出厂关闭）红**。
//
// ============================ 判据 / 报告 ============================
//  · 原版序列化值：`d:/2/tools/assetripper_out/ExportedProject/Assets/Booster Pack Standard *.prefab`（17 棵逐棵一致）；
//    解包侧同值 → `d:/2/新解包资源/assets_full/bundle_boosterpacks_assets_all/MonoBehaviour/MonoBehaviour_5658580387210265460.json`
//  · 节点 `m_IsActive: 0` → 同目录 `GameObject/Army Text.json` / `Army Text 2.json`（17 棵全部）
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_PACK`：卡包 prefab 的两颗 `Army Text` 必须带**可用的世界空间 TMP**（原版脚本在外部 CAB ⇒ 我们自建）。</summary>
public static class B102PACK_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102PACK] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    static BoosterPackOpenWindow _win;
    static GameObject _camGo;

    /// <summary>唯一入口（宿主聚合行里调）。自建「开包窗 + 一包 Leviathan」的夹具（与 `B98A5_Assert` 第 2 段同一件）。</summary>
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B102_PACK：卡包 `Army Text` 两颗必须带可用的世界空间 TMP（账 `商店:345`）");

        // ── A0 前提（硬断言，⛔ 不用 `if (x != null) Check(...)` —— 那会让前提不成立时整段静默跳过）──
        string prefabName = null;
        GameObject prefab = null;
        bool gotName = BoosterPackAssets.TryGetPrefabForArmy("Leviathan", out prefabName);
        T(gotName && !string.IsNullOrEmpty(prefabName), "（A0 前提）阵营 Leviathan 能查到 prefab 名");
        T(gotName && BoosterPackAssets.TryGetPrefab(prefabName, out prefab) && prefab != null,
          "（A0 前提）卡包 prefab 取得回来（`BoosterPackAssets.TryGetPrefab`）");

        BuildFixture();
        var item = _win != null ? _win.VisualItem : null;
        T(item != null, "（A0 前提）卡包实物建出来了（`BuildVisualItemLayer`）");
        if (item == null)
        {
            Cleanup();
            S("A0 前提不成立 ⇒ A1~A3 无法执行（已在上一条红）");
            return;
        }

        // 节点：名字逐字来自 prefab 实读（`Army Text` / `Army Text 2`，含 inactive）
        Transform nodeA = null, nodeB = null;
        var trs = item.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < trs.Length; i++)
        {
            if (trs[i] == null) continue;
            if (trs[i].name == "Army Text") nodeA = trs[i];
            else if (trs[i].name == "Army Text 2") nodeB = trs[i];
        }
        T(nodeA != null, "（A0 前提）节点 `Army Text` 在卡包实物子树里");
        T(nodeB != null, "（A0 前提）节点 `Army Text 2` 在卡包实物子树里");

        // ── A1 ★ 两颗节点上有可用的 TMP（组件在 + 字体在）──
        var tA = nodeA != null ? nodeA.GetComponent<TMPro.TextMeshPro>() : null;
        var tB = nodeB != null ? nodeB.GetComponent<TMPro.TextMeshPro>() : null;
        T(tA != null, "★ A1 `Army Text` 上有 TMP 组件（原版脚本在外部 CAB ⇒ 原状是 missing script）");
        T(tB != null, "★ A1 `Army Text 2` 上有 TMP 组件");
        T(tA != null && tA.font != null, "★ A1 `Army Text` 的 TMP 有字体资产（`font != null`）");
        T(tB != null && tB.font != null, "★ A1 `Army Text 2` 的 TMP 有字体资产（`font != null`）");

        var texts = item.ArmyTexts;
        T(texts != null && texts.Length == 2, "★ A1 `armyTexts` 是 **2** 颗（原版 `TextMeshPro[2]`）");
        T(texts != null && texts.Length == 2 && texts[0] == tA && texts[1] == tB,
          "★ A1 `armyTexts` 正好是这两颗节点上的 TMP（`Bind` 取的就是它们）");
        T(texts != null && texts.Length == 2
          && texts[0] != null && texts[0].GetType() == typeof(TMPro.TextMeshPro)
          && texts[1] != null && texts[1].GetType() == typeof(TMPro.TextMeshPro),
          "★ A1 `armyTexts` 是**世界空间** `TextMeshPro`（⛔ 不是 `TextMeshProUGUI`）");
        // 原版出厂两颗都是关着的（17 棵 prefab 的 `m_IsActive: 0`）—— 我们只补组件，⛔ 不替它打开
        T(nodeA != null && !nodeA.gameObject.activeSelf, "A1 `Army Text` **保持出厂关闭**（原版 `m_IsActive: 0`）");
        T(nodeB != null && !nodeB.gameObject.activeSelf, "A1 `Army Text 2` **保持出厂关闭**（同上）");

        // ── A2 ★ 文字真的写得上去、且**渲得出宽度**（不是只断组件在）──
        if (tA != null) { string d; bool ok = RendersNonEmpty(tA, out d); T(ok, "★ A2 `Army Text` 写上了字且**渲出非空网格**（" + d + "）"); }
        if (tB != null) { string d; bool ok = RendersNonEmpty(tB, out d); T(ok, "★ A2 `Army Text 2` 写上了字且**渲出非空网格**（" + d + "）"); }
        string want = Loc.T("Armies/Leviathan");
        T(tA != null && tA.text == want, "A2 `Army Text`.text = `Loc.T(\"Armies/Leviathan\")`（原版全数组写同一串）");
        T(tB != null && tB.text == want, "A2 `Army Text 2`.text 同上");

        // ── A3 灭自证：这两颗 TMP 确实是**我们按原版值建的那一颗**（见文件头 🧨 第二条）──
        var pragati = TmpFont.FaceAsset(TmpFont.Face.LatinCardBody);
        T(tA != null && pragati != null && ReferenceEquals(tA.font, pragati),
          "A3 `Army Text` 的字体面 = `Pragati-Regular SDF`（原版 `m_fontAsset`；本工程 `Resources/Fonts/`）");
        T(tB != null && pragati != null && ReferenceEquals(tB.font, pragati),
          "A3 `Army Text 2` 的字体面 = `Pragati-Regular SDF`");
        T(tA != null && tA.fontStyle == TMPro.FontStyles.UpperCase, "A3 `Army Text` `fontStyle = UpperCase`（原版 `m_fontStyle = 16`）");
        T(tB != null && tB.fontStyle == TMPro.FontStyles.UpperCase, "A3 `Army Text 2` `fontStyle = UpperCase`");
        T(tA != null && Mathf.Abs(tA.fontSizeMax - 10f) < 1e-4f && Mathf.Abs(tA.fontSizeMin - 2f) < 1e-4f,
          "A3 `Army Text` autoSizing 区间 = **2..10**（原版 `m_fontSizeMin 2` / `m_fontSizeMax 10`）");
        T(tB != null && Mathf.Abs(tB.fontSizeMax - 10f) < 1e-4f && Mathf.Abs(tB.fontSizeMin - 8f) < 1e-4f,
          "A3 `Army Text 2` autoSizing 区间 = **8..10**（原版 `m_fontSizeMin 8` / `m_fontSizeMax 10`）");
        T(tA != null && tA.enableAutoSizing && tB != null && tB.enableAutoSizing,
          "A3 两颗都开着 autoSizing（原版 `m_enableAutoSizing = 1`）");
        T(tA != null && !tA.isOrthographic && tB != null && !tB.isOrthographic,
          "A3 两颗都是**透视**世界空间（原版 `m_isOrthographic = 0`）");

        Cleanup();
        S("小结：✓ " + _sink.Pass + " · ✗ " + _sink.Fail);
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  夹具：与 `B98A5_Assert` 第 2 段同一件（第 0 页第 4 件 = 一包 Leviathan），不依赖宿主已建窗
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void BuildFixture()
    {
        BoosterPackAssets.Reset();
        WindowSounds.ResetCounters();

        _camGo = new GameObject("Main Camera (B102PACK)");
        _camGo.tag = "MainCamera";
        var cam = _camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.aspect = LayoutSpace.DesignAspect;
        LayoutSpace.Apply(cam);

        var go = new GameObject("Booster Pack Open Window (B102PACK)");
        _win = go.AddComponent<BoosterPackOpenWindow>();
        _win.Show(0, 3);            // 第 0 页第 4 件 = Leviathan（与 `Editor/ShopScene.cs` 那件同一件）
    }

    static void Cleanup()
    {
        if (_win != null) { Object.DestroyImmediate(_win.gameObject); _win = null; }
        if (_camGo != null) { Object.DestroyImmediate(_camGo); _camGo = null; }
    }

    /// <summary>真的让它生成一次网格并量出宽度 —— ⛔ 只断「组件在」等于没查。
    /// <para>⚠️ 编辑模式下 `AddComponent` 不触发 `Awake`（`TMP` 的 `m_textInfo` / `m_TextProcessingArray`
    /// 都在 `Awake` 里建）⇒ 先临时激活、`SendMessage("Awake")` 补一拍，量完**恢复出厂关闭**
    /// （原版 17 棵 prefab 的两颗都是 `m_IsActive: 0`，别留成打开状态污染后面的断言）。</para></summary>
    static bool RendersNonEmpty(TMPro.TextMeshPro t, out string detail)
    {
        bool was = t.gameObject.activeSelf;
        if (!was) t.gameObject.SetActive(true);
        t.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);   // 幂等：mesh / 处理数组都有 null 闸
        t.ForceMeshUpdate(true);                                         // ignoreActiveState = true

        var info = t.textInfo;
        int chars = info != null ? info.characterCount : -1;
        int verts = -1;
        if (info != null && info.meshInfo != null && info.meshInfo.Length > 0 && info.meshInfo[0].mesh != null)
            verts = info.meshInfo[0].mesh.vertexCount;
        float w = t.preferredWidth;
        detail = "char=" + chars + " · verts=" + verts + " · preferredWidth=" + w.ToString("F4")
                 + " · text=`" + (t.text == null ? "<null>" : t.text) + "`";

        if (!was) t.gameObject.SetActive(false);
        return chars > 0 && verts > 0 && w > 0f;
    }
}
