// B140_WQ_Assert.cs — 执行代理 `B140_WQ`（第一百二十七会话 · 2026-11-04）
//
// ============================ 它盯的是什么 ============================
//  旧账 `UI-11`：`Shell/AllianceMemberTab.cs` 的**成员行头像三层画序**。
//  · 原版判据 = `Alliance Member Entry.prefab` 的 `Image Container/m_Children = [Highlight, Border, Image]`
//    （AssetRipper YAML 现读；同族 `Avatar Item Small.prefab` 同序）。uGUI **后建者在上**
//    ⇒ 画序 = **Highlight < Border < Image**。
//  · 我们改前 = `Image` 层 1 / `Border` 层 2 ⇒ **Border 压在立绘上**。
//    `Player_Profile_Border` 中心是不透明黑（256×286 PNG 实测中心 `(0,0,0,255)`、透明只占 16.3%），
//    框 123.59×82.12 居中盖住立绘 197.74×131.39 ⇒ **每一行头像中央一块黑板**
//    （与 2026-10-18 `AvatarTab.cs:443-451` 修过的「469 格全黑」是**同一条病**，当时没横向扫到成员行）。
//  · 修法 = `RowArtQueue(..., layer)` 的两个实参 1/2 对调（`Shell/AllianceMemberTab.cs` 的 `AllianceMemberRow.Build`）。
//
// ============================ 为什么这么断（⛔ 不自证）============================
//  断的是**建出来的那两颗图真的落地的渲染队列**（`ImageQuad.RenderQueue`，读的是材质的
//  `sharedMaterial.renderQueue`），**不是**回读 `RowArtQueue` 的返回值 —— 后者等于拿实现证实现。
//  绝对队列 = `Page.Q + QOff + RowArtQueue(...)`，同一条带内 `layer` 每 +1 ⇒ 队列 +1
//  ⇒ **Border 的队列必须严格小于 Image 的**。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  · A1 / A2 / A4：把 `Build` 里两个层号改回 **Image=1 / Border=2**
//    （= 修前那一版）⇒ `qb > qi` ⇒ A1 红、`qi − qb = −1` ⇒ A2 红、三段序反转 ⇒ A4 红。
//    只改这一处、别的都不动，就能红 —— 说明这三条**真的盯着画序**，不是复述常量。
//  · A3：把 `Highlight` 也改成 1 或 2（挤掉 Border）⇒ A3 红。
//  · A5 / A6：把 `AllianceMemberRowAvatarGeo.BorderRect` 的倍率换成 `1f`
//    （框缩回 98.87，仍然落在立绘内）⇒ A5 仍绿，但 A6 的「框宽 = 123.59」会红 ——
//    这几条断的是「框**确实盖在立绘中心**」，是本缺陷**为什么致命**的前提，不是画序本身。
//  ⛔ 全部前提（窗 / 视图 / 三层图）都单独断言；前提不成立时**明确红**，⛔ 不写 `if (x != null) Check(...)`。
//
// ============================ 怎么跑 / 怎么接 ============================
//  · 单跑：`-executeMethod B140_WQ_Assert.Run`（自建一扇 `SocialWindow`、跑完自拆；不碰 `SocialData.Members`）。
//  · 接宿主：`B140_WQ_Assert.Run(); pass += B140_WQ_Assert.Pass; fail += B140_WQ_Assert.Fail;`
//  · 判据 / 报告 → `资料/完全复刻/判据/B140_参数复核_G1P2_界面活动.md` §三·1
//                + `资料/完全复刻/报告/B140_WQ_成员行画序.md`
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B140_WQ`：`UI-11` 成员行头像画序（Highlight &lt; Border &lt; Image）。</summary>
public static class B140_WQ_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B140WQ] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>夹具用的立绘名（`Resources/Art/avatars/Avatar_AM_Commissar.png` 实存）。</summary>
    const string ProbeAvatar = "Avatar_AM_Commissar";

    /// <summary>唯一入口。</summary>
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "B140_WQ · UI-11 成员行头像画序（Highlight < Border < Image）");

        SocialWindow own = null;
        GameObject host = null;
        try
        {
            WindowsManager.Get();                       // 幂等：没有宿主就现建一台（`-executeMethod` 直跑时也要能活）
            own = SocialWindow.Create(null);
            T(own != null, "（前提）`SocialWindow` 建得出来（`Create(null)`，同 `B102ALLY_Assert` / `B113X2_Assert` 的夹具写法）");
            if (own == null) return;
            own.Open();

            var tab = own.PageAlliances != null ? own.PageAlliances.Member : null;
            T(tab != null, "（前提）`Alliances` 页的 `AllianceMemberTab` 在（`sw.PageAlliances.Member`）");
            T(tab != null && tab.Page != null, "（前提）该视图的 `Page` 已接上（队列档与取图都走它；没接上 ⇒ 下面的队列读数全是假的）");
            if (tab == null || tab.Page == null) return;

            // ---- 夹具：自建一行（750×100 = `资料/普查产出_0927` §A·2·3 的根尺寸），⛔ 不碰 SocialData.Members
            var rowR = new PxRect(0f, 0f, 750f, 100f);
            var rowHost = MenuDraw.Node(own.transform, "B140WQ Row Host", rowR);
            host = rowHost != null ? rowHost.gameObject : null;
            T(rowHost != null, "（前提）夹具行容器建出来了");
            if (rowHost == null) return;

            var m = new SocialData.Member
            {
                Index = 1,
                Name = "B140 Probe",
                Role = "Alliance Master",
                Online = true,
                AvatarArt = ProbeAvatar,
                DraftRating = "32",
                RankedRating = "32",
            };
            AllianceMemberRow.Build(tab, rowHost, m, rowR, 1);

            // ---- 前提：三层都在（⛔ 缺一层就 return，⛔ 不用 `if (x != null) Check(...)` 蒙混）
            // ⚠️ `Build` 先建一层容器 `Alliance Member Entry`（行根），`Avatar Item Small` 挂在它下面
            var rowNode = rowHost.Find("Alliance Member Entry");
            T(rowNode != null, "（前提）`Alliance Member Entry` 行节点建出来了");
            if (rowNode == null) return;
            var avn = rowNode.Find("Avatar Item Small");
            T(avn != null, "（前提）`Avatar Item Small` 建出来了");
            if (avn == null) return;
            var imgC = avn.Find("Image Container");
            T(imgC != null, "（前提）`Image Container` 建出来了");
            if (imgC == null) return;

            var hl = Quad(avn, "Highlight");
            var bd = Quad(avn, "Border");
            var im = Quad(imgC, "Image");
            T(hl != null, "（前提）`Highlight` 画出来了（`Player_Avatar_selected` 取到了）");
            T(bd != null, "（前提）`Border` 画出来了（`Player_Profile_Border` 取到了）");
            T(im != null, "（前提）`Image`（立绘）画出来了（`" + ProbeAvatar + "` 取到了）");
            if (hl == null || bd == null || im == null) return;

            int qh = hl.RenderQueue, qb = bd.RenderQueue, qi = im.RenderQueue;
            Debug.Log(_sink.P + "画序读数（渲染队列）：Highlight = " + qh + " · Border = " + qb + " · Image = " + qi);

            // ---- A 段：画序（唯一硬结论）
            T(qb < qi,
              "A1 ★ **`Border` 在 `Image` 之下**（队列 " + qb + " < " + qi
              + "）—— 改回 Image=1 / Border=2 ⇒ " + qb + " > " + qi + " ⇒ 红（那正是「每行头像中央一块黑板」那一版）");
            C(qi - qb, 1,
              "A2 两层同属一行同一条带（`QRowArtStride` 一档）⇒ 队列差恰为 `1`；队列相等 = 同层、谁压谁不定");
            T(qh < qb,
              "A3 `Highlight` 仍在最底（" + qh + " < " + qb + "）—— 把它的层号挤到 1/2 上 ⇒ 红");
            T(qh < qb && qb < qi,
              "A4 三段画序 = **Highlight < Border < Image**（= 原版 `Image Container` 子件序 `[Highlight, Border, Image]`）");

            // ---- B 段：这条为什么致命（几何佐证；断的是「框真盖在立绘中心」，不是画序）
            var av = new PxRect(rowR.x1 + 50.57f, rowR.y1 + 11.87f, rowR.x1 + 149.44f, rowR.y1 + 114.93f);
            var imR = AllianceMemberRowAvatarGeo.ImageRect(av);
            var bdR = AllianceMemberRowAvatarGeo.BorderRect(av);
            N(bdR.W, 123.59f, 0.10f, "B1 `Border` 视觉宽 = **123.59**（= 布局框 98.87 × 1.25；原版 `m_LocalScale.x = 1.25`）");
            N(imR.W, 197.74f, 0.10f, "B2 `Image` 视觉宽 = **197.74**（= 布局框 98.87 × 2.0）");
            T(bdR.x1 > imR.x1 && bdR.x2 < imR.x2 && bdR.y1 > imR.y1 && bdR.y2 < imR.y2,
              "B3【缺陷致命性】`Border` 视觉框**整个落在**立绘框内（x/y 两轴都内缩）⇒ 它一旦在上，"
              + "中心那块不透明黑就直接盖掉立绘 —— 正是改前每行头像中央那一片黑");
        }
        catch (Exception e)
        {
            T(false, "断言段抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            if (host != null) MainMenuSubmenuWindow.DestroySafe(host);
            if (own != null) SocialWindow.DestroySafe(own.gameObject);
            Debug.Log(_sink.P + "=== B140WQ 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败 ===");
        }
    }

    /// <summary>取某个直接子节点上的 `ImageQuad`（找不到 / 没建 = null，由调用方当**前提**断出来）。</summary>
    static ImageQuad Quad(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t == null) return null;
        return t.GetComponent<ImageQuad>();
    }
}
