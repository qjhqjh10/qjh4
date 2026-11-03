// B124_W44_Assert.cs — `Label.AlignLeftOn` / `AlignRightOn` 的「设计帧 → 父局部」换算断言
//
// 活：账 `B123-ARMYTITLE`（卡组编辑窗「军队」标题画到屏幕右上）的**根因守卫**。
//   🔴 旧式（`Battle/Label.cs` 的 `ParentXInDesignSpace`）= `localX = worldX − P/k ± W/2`
//   （`P` = 父世界 x · `k` = 父 `lossyScale.x`）⇒ **渲染结果恒 = `k·worldX`**（`P` 在式子里
//   整体约掉！）⇒ 父链上**任何一级 `k ≠ 1`** 都会把设计 x 再乘一遍：`k = −1` 时整块**关于屏幕
//   中心镜像**（真机实拍：设计 x ≈ 2px 的标题画到 x ≈ 1900px）。`k` 为正但 ≠ 1 时同理成比例偏。
//   现行式 = 设计点经**顶层祖先**（= 设计帧）`TransformPoint` 折进世界、再 `parent.InverseTransformPoint`
//   折回父局部 ⇒ 渲染结果恒 = `root.TransformPoint(worldX).x`，与链上缩放 / 镜像 / 父位移无关。
//
// 接线：宿主里调 `B124_W44_Assert.Run()`（⛔ 本文件不改任何 `Editor/*Scene.cs`）。
//   也可直接跑（类在全局命名空间、自建夹具、不依赖宿主场景）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B124_W44_Assert.Run -logFile -
//
// 判绿红：按**行首** `B124W44 ✓` / `B124W44 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//   「未验」单独计数、**不算通过**（TMP 字体资产不在时成立不了，⛔ 不静默当绿）。
//
// ⚠️ 期望值**全部是字面量**（`wantX` / `rootX + wantX` / `wantX`），⛔ 不用被测函数去算期望
//   （那会变成同义反复）。量「渲出来的左 / 右边缘」用的是 Unity 原语（`transform.position` +
//   `WorldW × lossyScale`），与 `Editor/SettingsScene.cs` A228 那一节同一个量法。
using UnityEngine;
using CardPresentation;

public static class B124_W44_Assert
{
    static int _pass, _fail, _unverified;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int Unverified { get { return _unverified; } }

    const string Tag = "B124W44";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4") + "；实得 " + got.ToString("F4") + "）");
    }

    static void MarkUnverified(string msg) { _unverified++; Debug.Log(Tag + " … 未验：" + msg); }

    /// <summary>**渲出来**的左缘（世界 x）。宽 = `WorldW`（TMP 的局部真测量）× 该件自己的
    /// `lossyScale`（父链缩放会跟着缩）—— 同 `Editor/SettingsScene.cs` A228 的那两条量法，
    /// 只是这里对 `k` 取绝对值（本文件有一条 `k = −1` 的夹具）。</summary>
    static float RenderedLeft(Label l)
    {
        var t = l.transform;
        return t.position.x - l.WorldW * Mathf.Abs(t.lossyScale.x) * 0.5f;
    }

    static float RenderedRight(Label l)
    {
        var t = l.transform;
        return t.position.x + l.WorldW * Mathf.Abs(t.lossyScale.x) * 0.5f;
    }

    public static void Run()
    {
        _pass = _fail = _unverified = 0;

        // 实参长相与 `MenuDraw.AlignLeft` 一致（`LayoutSpace.FromPixel(r.x1, 0f).x`）；取**正值**
        // 是为了让「镜像」（`k = −1`）那一条的正负号分得开。
        float wantX = LayoutSpace.FromPixel(1400f, 0f).x;

        GroupA_UnitChainUnchanged(wantX);   // 回归：k=1 + 根在原点 ⇒ 与旧式**逐值相同**（20 个生产调用点靠它）
        GroupB_RootOffset(wantX);           // 设计帧（顶层祖先）被挪离原点
        GroupC_ScaledAncestor(wantX);       // 根与父之间夹一层 1.2 倍缩放（= 真机 B123 那一类）
        GroupD_MirroredAncestor(wantX);     // 同上但是 **−1**（镜像 —— 真机实拍的正字形）

        Check(float.IsNaN(wantX) == false && Mathf.Abs(wantX) > 1e-3f,
              "（前提）`wantX` 是有限非零值 —— 否则下面每一条的近比都退化成假绿");
        Debug.Log(Tag + " ==== 合计：通过 " + _pass + " · 失败 " + _fail + " · 未验 " + _unverified + " ====");
    }

    // ---------------- A 组：k = 1 · 根在世界原点 ⇒ 新旧两式恒等（防回归） ----------------
    static void GroupA_UnitChainUnchanged(float wantX)
    {
        var root = new GameObject("w44 A root").transform;                 // 无父 ⇒ 世界原点、scale 1
        var parent = new GameObject("w44 A parent").transform;
        parent.SetParent(root, false);
        parent.localPosition = new Vector3(2f, 0f, 0f);                    // 设计 x = 2

        var l = Label.Create(parent, "Wg", Vector3.zero, 5, Color.white,
                             new Vector2(0.5f, 0.5f), "w44 A label");
        Check(l != null && l.CanRenderChinese,
              "（前提）A 组：TMP 后端在（点阵后端下 `Align*On` 不出声、也不挪 ⇒ 下面几条无从谈起）");
        if (l != null && l.CanRenderChinese)
        {
            CheckNear(parent.position.x, 2f, 1e-3f, "（前提）A 组：父的世界 x = 2（设计值 2、k = 1）");
            l.AlignLeftOn(wantX);
            // 🔴 这一条刻意用**旧式**当期望：k = 1 且根在原点时新旧两式**必须逐值相同**
            //    ⇒ 它挡的是「新式顺手把 20 个生产调用点的既有结果改了」。
            CheckNear(l.transform.localPosition.x, wantX - parent.position.x + l.WorldW * 0.5f, 1e-3f,
                      "A① k=1：`localPosition` 与旧式（`worldX − 父世界 x + W/2`）**逐值相同**（生产调用点零位移）");
            CheckNear(RenderedLeft(l), wantX, 0.03f,
                      "A② k=1：`AlignLeftOn` 之后**渲出来**的左缘 = 传进去的那个 x");
            l.AlignRightOn(wantX);
            CheckNear(RenderedRight(l), wantX, 0.03f,
                      "A③ k=1：`AlignRightOn` 之后**渲出来**的右缘 = 传进去的那个 x（与左对齐差一个整宽）");
        }
        Object.DestroyImmediate(root.gameObject);
    }

    // ---------------- B 组：设计帧（顶层祖先）不在世界原点 ----------------
    //   旧式渲染结果 = `k·worldX`（= `wantX`）⇒ **整块少了根那一份位移**；新式 = `rootX + wantX`。
    static void GroupB_RootOffset(float wantX)
    {
        const float rootX = 3.5f;
        var root = new GameObject("w44 B root").transform;
        root.position = new Vector3(rootX, 0f, 0f);                        // 顶层祖先（设计帧）挪离原点
        var parent = new GameObject("w44 B parent").transform;
        parent.SetParent(root, false);
        parent.localPosition = new Vector3(2f, 0f, 0f);

        var l = Label.Create(parent, "Wg", Vector3.zero, 5, Color.white,
                             new Vector2(0.5f, 0.5f), "w44 B label");
        if (l != null && l.CanRenderChinese)
        {
            CheckNear(parent.position.x, rootX + 2f, 1e-3f,
                      "（前提）B 组：父的世界 x = 根偏移 + 设计值（k 仍 = 1，所以这一组只测「根偏移」）");
            l.AlignLeftOn(wantX);
            CheckNear(RenderedLeft(l), rootX + wantX, 0.05f,
                      "★B 设计帧被挪走：渲出来的左缘 = **根偏移 + 设计 x**"
                      + "；旧式「`worldX − P/k`」拿不到根偏移 ⇒ 会落在 `wantX`（差整个 " + rootX
                      + " 世界单位）⇒ 这一条红");
        }
        else MarkUnverified("B 组（根偏移）：TMP 字体资产不在");
        Object.DestroyImmediate(root.gameObject);
    }

    // ---------------- C 组：根与父之间夹一层 1.2 倍缩放 ----------------
    //   旧式：渲染 = `k·wantX`（= 1.2·wantX，偏 +0.2·wantX）；新式：渲染 = `wantX`。
    static void GroupC_ScaledAncestor(float wantX)
    {
        var root = new GameObject("w44 C root").transform;                 // 原点、scale 1 = 设计帧
        var mid = new GameObject("w44 C mid").transform;
        mid.SetParent(root, false);
        mid.localScale = new Vector3(1.2f, 1.2f, 1f);                      // 链上缩放（≠ 顶层祖先）
        var parent = new GameObject("w44 C parent").transform;
        parent.SetParent(mid, false);
        parent.localPosition = new Vector3(2f, 0f, 0f);                    // 世界 x = 2.4，k = 1.2

        var l = Label.Create(parent, "Wg", Vector3.zero, 5, Color.white,
                             new Vector2(0.5f, 0.5f), "w44 C label");
        if (l != null && l.CanRenderChinese)
        {
            CheckNear(l.transform.lossyScale.x, 1.2f, 1e-4f,
                      "（前提）C 组：标签的 `lossyScale.x` = 1.2（缩放真的在链上）");
            CheckNear(parent.position.x, 2.4f, 1e-3f,
                      "（前提）C 组：父的世界 x = 2.4 ≠ 设计值 2 —— 两套量纲真的差着（不是在断恒等式）");
            l.AlignLeftOn(wantX);
            CheckNear(RenderedLeft(l), wantX, 0.05f,
                      "★C 链上 1.2 倍缩放：渲出来的左缘仍 = **设计 x**（`wantX`）"
                      + "；旧式恒等于 `k·wantX` = " + (1.2f * wantX).ToString("F4")
                      + "（偏 " + (0.2f * wantX).ToString("F4") + " 世界单位 ≈ "
                      + (0.2f * wantX * 108f).ToString("F1") + "px）⇒ 这一条红");
            l.AlignRightOn(wantX);
            CheckNear(RenderedRight(l), wantX, 0.05f,
                      "★C `AlignRightOn` 同理（两个口各算自己那一式，改一个漏一个时只有这一条红）");
        }
        else MarkUnverified("C 组（链上缩放）：TMP 字体资产不在");
        Object.DestroyImmediate(root.gameObject);
    }

    // ---------------- D 组：同上但是 −1（镜像） ----------------
    //   旧式：渲染中心 = `−wantX − W/2`、渲染左缘 = `−wantX − W`（整块镜像到屏幕另一头）；
    //   真机 `B123-ARMYTITLE` 的实拍（设计 x≈2px 的标题出现在 x≈1900px）就是这个形状。
    static void GroupD_MirroredAncestor(float wantX)
    {
        var root = new GameObject("w44 D root").transform;
        var mid = new GameObject("w44 D mid").transform;
        mid.SetParent(root, false);
        mid.localScale = new Vector3(-1f, 1f, 1f);                         // 镜像（链上，不是顶层）
        var parent = new GameObject("w44 D parent").transform;
        parent.SetParent(mid, false);
        parent.localPosition = new Vector3(-2f, 0f, 0f);                   // 世界 x = +2（镜像把它翻正）

        var l = Label.Create(parent, "Wg", Vector3.zero, 5, Color.white,
                             new Vector2(0.5f, 0.5f), "w44 D label");
        if (l != null && l.CanRenderChinese)
        {
            CheckNear(l.transform.lossyScale.x, -1f, 1e-4f,
                      "（前提）D 组：标签的 `lossyScale.x` = −1（镜像真的在链上）");
            CheckNear(parent.position.x, 2f, 1e-3f,
                      "（前提）D 组：父的世界 x = +2（局部 −2 经镜像翻正）—— 旧式会把它当设计 x 除去");
            l.AlignLeftOn(wantX);
            CheckNear(RenderedLeft(l), wantX, 0.05f,
                      "★D 链上镜像：渲出来的左缘仍 = **设计 x**；旧式 = `k·wantX` = −"
                      + wantX.ToString("F4") + "（整块翻到屏幕另一头，正是 `B123-ARMYTITLE` 的形状）⇒ 这一条红");
        }
        else MarkUnverified("D 组（链上镜像）：TMP 字体资产不在");
        Object.DestroyImmediate(root.gameObject);
    }
}
