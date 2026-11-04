// B140_WE_Assert.cs — 卡面域两件（`B140_WE` · 账 `B139-E3` / `B139-G3`）的断言
//
// 被测：
//   ① `CardPresentation/Core/CardView.cs` 的 `BuildHealText()` —— 建出飘字之后必须把原版的
//      `HealIcon` 挂到 `HealText` 底下（原版 `CardPrefab.prefab` 的 `Board Elements/HealText/HealIcon`；
//      接线口 = `PopNumber.AttachHealIcon`，判决处 `CardView.BuildHealText` 的 `AttachHealIcon` 那一行）。
//   ② `CardPresentation/Core/TraitFrames.cs` 的 `Play()` —— 建状态框之前必须把「这张框所属的卡」
//      交给 `WFEffectCards`（原版 `BattleDriver` 那条 `Resolver` 的等价物），并在 `Play` 之后**还原**。
//
// 直接跑（类在**全局命名空间**、类名 == 文件名、不依赖任何宿主场景）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WE_Assert.Run -logFile -
//
// 判绿红：按**行首** `B140WE ✓` / `B140WE ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//
// 四组：
//   A 治疗飘字：刚建的卡**还没有** `HealText`（前提）→ `ShowHeal(5)` 之后 `HealText` 与它底下的
//     `PopHealIcon` **一起**出现，且图标位姿 = 原版 prefab 实读值（scale 0.56046 · sortingOrder 100 ·
//     α 0.853 · localPosition (0,0,0)）。
//   B 特性框上下文：带 `CardView` 的挂点建框 ⇒ 装上的 resolver **解析出**那张卡（= 同一张卡）；
//     不认识的实例**转发**给外层 resolver（⛔ 不许把 `BattleDriver` 那份顶掉）；建好的实例**长期记住**
//     它的卡（`Exit`/`ModuleTick` 的延迟恢复在 `Play` 之后才读上下文）。
//   A' 🧨 灭自证（治疗）：把 `BuildHealText` 里那一行 `PopNumber.AttachHealIcon(...)` 删掉 ⇒ A2/A3 必红
//     （这里额外断「图标只在 `ShowHeal` 这一拍才出现」，不靠任何「我们自己的标记位」）。
//   B' 🧨 灭自证（上下文）：**同一个探针**在「不带 `CardView` 的挂点」上必须解析出 **null**
//     —— 若有人把 `LastCardContextActor` 写成常量 / 或删掉 `RegisterFrameCard`，B2 / B6b / B7b 必有一条红。
//
// ⚠️ 期望值：图标那四个数是**原版 prefab 实读值**（写在这里当外部量，⛔ 不拿 `PopNumber.HealIconScale`
//   反算 —— 那是同义反复）；「解析出的是哪张卡」比的是 `ReferenceEquals(cv.transform, …)`。
using System;
using UnityEngine;
using CardPresentation;

public static class B140_WE_Assert
{
    const string Tag = "B140WE";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Section(string t) { Debug.Log("--- " + Tag + " · " + t + " ---"); }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + "（实得 " + got.ToString("0.####") + " / 原版 " + want.ToString("0.####") + "）");
    }

    // ==================================================================
    //  A 治疗飘字 + 那颗 `HealIcon`（`B139-E3`）
    // ==================================================================
    static void A_HealIcon(GameObject root)
    {
        Section("A 治疗飘字里的 `HealIcon`（B139-E3）");
        var go = new GameObject("B140WE_HealProbe");
        go.transform.SetParent(root.transform, false);
        var cv = go.AddComponent<CardView>();      // CardView 没有 Awake/Start（OnEnable 里有 `_built` 早退）⇒ Editor 里可以直接挂
        Check(cv != null, "A0 （前提）探针卡上挂到了 `CardView`");
        if (cv == null) return;

        // 前提：`HealText` 是**懒建**的 —— 没治疗过就没有这一层
        Check(go.transform.Find("HealText") == null,
              "A1 （前提）还没治疗过 ⇒ 卡上**没有** `HealText` 这一层（它是按需建的）");

        cv.ShowHeal(5);
        var heal = go.transform.Find("HealText");
        Check(heal != null, "A1b `ShowHeal(5)` 之后 `HealText` 建出来了");
        Check(cv.HealTextVisible && cv.HealTextText == "+5",
              "A1c 飘字显示着、文案 `+5`（实得 `" + cv.HealTextText + "` / visible=" + cv.HealTextVisible + "）");
        if (heal == null) return;

        // 结论：原版那颗图标必须**跟着飘字一起**建出来
        var icon = heal.Find("PopHealIcon");
        Check(icon != null,
              "A2 🧨 `HealText` 底下有 `HealIcon` 那一层（原版 `CardPrefab/Board Elements/HealText/HealIcon`）"
            + " —— 删掉 `BuildHealText` 里 `PopNumber.AttachHealIcon(...)` 那一行 ⇒ 本条红");
        if (icon == null) return;

        var sr = icon.GetComponent<SpriteRenderer>();
        Check(sr != null, "A2b 那颗图标带 `SpriteRenderer`（原版那颗就是 SpriteRenderer，不是 Image）");
        if (sr != null)
        {
            Check(sr.sortingOrder == 100, "A3a `sortingOrder = 100`（原版 prefab 实读；实得 " + sr.sortingOrder + "）");
            Check(Mathf.Abs(sr.color.a - 0.853f) <= 1e-3f,
                  "A3b `m_Color.a = 0.853`（原版 prefab 实读、恒定；实得 " + sr.color.a.ToString("0.####") + "）");
        }
        CheckClose(icon.localScale.x, 0.56046f, 1e-4f, "A3c 图标 `localScale = 0.56046`（三轴同值）");
        CheckClose(icon.localScale.y, 0.56046f, 1e-4f, "A3d 图标 `localScale.y = 0.56046`");
        CheckClose(icon.localPosition.x, 0f, 1e-4f, "A3e 图标 `localPosition.x = 0`");
        CheckClose(icon.localPosition.y, 0f, 1e-4f, "A3f 图标 `localPosition.y = 0`");
        CheckClose(icon.localPosition.z, 0f, 1e-4f,
                   "A3g 图标 `localPosition.z = 0`（那一跳 z 烘在父件 `HealText` 的 −0.05 上，⛔ 别加到子件）");

        // 幂等（同一次 `BuildHealText` 只该建一颗）+ 重入不重建
        int n = 0;
        for (int i = 0; i < heal.childCount; i++)
            if (heal.GetChild(i).name == "PopHealIcon") n++;
        Check(n == 1, "A4 `HealText` 底下**只有一颗** `PopHealIcon`（实得 " + n + "）");

        cv.HideHeal();
        Check(go.transform.Find("HealText").Find("PopHealIcon") != null,
              "A5 收起飘字只关 `HealText` 节点，图标节点还在（它是 `HealText` 的子件 ⇒ 一起显示/隐藏）");
    }

    // ==================================================================
    //  B 特性框的「所属卡」上下文（`B139-G3`）
    // ==================================================================
    static void B_TraitFrameCardContext(GameObject root)
    {
        Section("B 特性框建框时的『所属卡』上下文（B139-G3）");
        Check(TraitFrames.PrefabFor("stealth", false) != null,
              "B0 （前提）`stealth` 有绑定的 OnPlay 框（实得 `" + TraitFrames.PrefabFor("stealth", false) + "`）");

        // 外层 resolver：测「不认识的那个实例必须**转发**给它」（我们那份不许把别人顶掉）
        var sentinel = new GameObject("B140WE_Sentinel").transform;
        sentinel.SetParent(root.transform, false);
        Func<WarpforgeVFX.WarpforgeEffectPlayer, WarpforgeVFX.WFEffectCardContext> outer =
            _ => new WarpforgeVFX.WFEffectCardContext { valid = true, actingCard = sentinel };
        var prevOuter = WarpforgeVFX.WFEffectCards.Resolver;

        var go = new GameObject("B140WE_TraitProbe");
        go.transform.SetParent(root.transform, false);
        var cv = go.AddComponent<CardView>();
        Check(cv != null, "B0b （前提）探针卡上挂到了 `CardView`");
        if (cv == null) return;

        var tf = TraitFrames.Attach(cv);
        Check(tf != null && tf.Anchor != null, "B0c （前提）挂点建好了（含 `effectsAnchor`）");
        if (tf == null || tf.Anchor == null) return;

        try
        {
            WarpforgeVFX.WFEffectCards.Resolver = outer;
            TraitFrames.ResetCardContextDiagnostics();

            int playersBefore = WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers.Count;
            tf.DisplayStatus("stealth", 1);

            int touched = TraitFrames.CardContextPlays + TraitFrames.CardContextMissing;
            Check(touched >= 1,
                  "B1 （前提）建框真的走到了 `TraitFrames.Play`（装了 " + touched + " 次上下文）");
            Check(ReferenceEquals(TraitFrames.LastCardContextActor, cv.transform),
                  "B2 🧨 建框那一下 resolver **解析出了这张框所属的那张卡**（= 探针卡的 transform）"
                + " —— 删掉 `Play` 里那句 `InstallFrameResolver();` ⇒ 本条红");
            Check(TraitFrames.CardContextPlays >= 1,
                  "B3 解析出「所属卡」的次数 ≥ 1（实得 " + TraitFrames.CardContextPlays + "）");
            Check(TraitFrames.CardContextMissing == 0,
                  "B4 带 `CardView` 的挂点**没有**一次解析成空（实得 " + TraitFrames.CardContextMissing + "）");

            // ---- B5 转发：不认识的实例必须落到外层（⛔ 不许把 BattleDriver 那份顶掉）----
            WarpforgeVFX.WFEffectCardContext fwd;
            bool fwdOk = WarpforgeVFX.WFEffectCards.TryGet(null, out fwd, false);
            Check(fwdOk && ReferenceEquals(fwd.actingCard, sentinel),
                  "B5 🧨 不认识的实例**转发给外层 resolver**（拿到的是外层的哨兵卡）"
                + " —— 若把 `Resolver` 就地写成 `_ => 我的卡`，本条红");

            // ---- B6 **延迟**那一跳：`Exit()`/`ModuleTick` 的恢复在 `Play` 之后才读上下文 ----
            var added = new System.Collections.Generic.List<WarpforgeVFX.WarpforgeEffectPlayer>();
            var act = WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers;
            for (int i = playersBefore; i < act.Count; i++) if (act[i] != null) added.Add(act[i]);
            Check(added.Count >= 1, "B6a （前提）`Play` 之后产生了 ≥1 个特效实例（实得 " + added.Count + "）");
            bool anyMapped = false;
            for (int i = 0; i < added.Count; i++)
            {
                Transform got;
                if (TraitFrames.TryGetFrameCard(added[i], out got) && ReferenceEquals(got, cv.transform))
                    anyMapped = true;
            }
            Check(anyMapped,
                  "B6b 🧨 **按实例**再问一次上下文，仍然解析出那张卡（`Exit`/`ModuleTick` 的延迟恢复走的就是这条路）"
                + " —— 删掉 `Play` 里那句 `RegisterFrameCard(p, actor);` ⇒ 本条红");

            // ---- B' 灭自证：**同一个探针**，挂点不是卡 ⇒ 必须解析出 null ----
            var plain = new GameObject("B140WE_PlainProbe");
            plain.transform.SetParent(root.transform, false);
            var tf2 = TraitFrames.Attach((Transform)plain.transform);   // 这条重载不认 `CardView` ⇒ `_card == null`
            Check(tf2 != null, "B7a （前提）非卡的挂点也挂上了 `TraitFrames`");
            if (tf2 != null && tf2.Anchor != null)
            {
                TraitFrames.ResetCardContextDiagnostics();
                tf2.DisplayStatus("stealth", 1);
                Check(TraitFrames.LastCardContextActor == null,
                      "B7b 🧨 **灭自证**：挂点不是卡 ⇒ 同一个探针解析出 **null**"
                    + "（若有人把它写成常量 / 或不区分「这一拍没有卡」直接落到外层，本条红）");
                Check(TraitFrames.CardContextMissing >= 1,
                      "B8 解析成空的次数 ≥ 1（实得 " + TraitFrames.CardContextMissing + "）");
            }
        }
        finally
        {
            WarpforgeVFX.WFEffectCards.Resolver = prevOuter;   // 别把探针的 resolver 留在全局静态上
        }
    }

    // ==================================================================
    //  入口
    // ==================================================================
    public static void Run()
    {
        _pass = _fail = 0;
        var root = new GameObject("B140_WE_Assert_Root");
        try
        {
            Debug.Log("=== " + Tag + " 开始（卡面域两件 · B139-E3 / B139-G3）===");
            A_HealIcon(root);
            B_TraitFrameCardContext(root);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);   // 批处理没有帧循环 ⇒ 用 DestroyImmediate
            Debug.Log("=== " + Tag + " 结束：通过 " + _pass + " · 失败 " + _fail + " ===");
        }
    }
}
