// B118W2_Assert.cs — 执行代理 `B118-W2` 的断言宿主（2026-11-02）
//
// 覆盖两件（判据全写在每组头里；报告 → `资料/完全复刻/报告/B118_W2_悬停与TMP队列.md`）：
//   A. `B117-R64` 手牌悬停那张：缩放是**绝对值 1.3**（⛔ 不是「乘 1.3」）；
//      y 是**绝对 y**（= 手牌区底边 + **30 原版世界单位 = 445 px**（`B118-W17` 订正），与槽位/弧高无关）。
//      判据 = `CardScript__ShowCardInHand`（`判据/B117-G10-06_手牌.md` §四·1/§四·2）：
//        `localScale = Vector3.one × cardInHandShownScale` / `rotation = FromEulerRad(Vector3.zero)`
//        `position = (x, GetPlayerHand(isPlayer).transform.position.y + cardInHandShownYOffset, z − 0.01)`；
//      静止档 = `GetCardInHandScale()` = `one × m_scale`（我方 0.73）⇒ 悬停/静止 = 1.3 ÷ 0.73 = **1.7808**。
//   B. `B117-R73` 「收起再放开」之后，TMP **子网格**（回退字体 / `<sprite>` 那几路）的队列不许退回 3000。
//      判据链：`TMP_Text.OnEnable`（`TextMeshPro.cs:630-668`）→ `SetAllDirty` → 延迟一拍的
//      `TMP_UpdateManager.DoRebuilds` → `SetArraySizes`（`TextMeshPro.cs:1947`）把每颗子网格的
//      `sharedMaterial` 换回它自己的材质参考表（队列 3000）。
//      🔴 2026-11-03（`B124-W27` 实测）：本 build 的 `ForceMeshUpdate()` **不再**复现这条（掉队 0 颗）⇒ 夹具改为
//      **显式**把子网格材质队列写回 3000 来造出缺陷态（见 B 组 ④）；`RestampQueuesAfterEnable` 那一支仍照原样验。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B118W2_Assert.Run -logFile -
//
// 判绿红：`grep "^B118W2"` 的**合计行**；逐条按**行首** `B118W2 ✓` / `B118W2 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求（给调度台挂进 `Editor/BattleScene.cs` 时看）：
//   · `Run()` **零前提**：不读 `BattleDriver` / 不读场景 / 不读存档；自带临时根 + 收尾 `DestroyImmediate`。
//   · 两组都**不用** `CardTween`（A 用 `animateRelayout = false` 全 snap）⇒ 与时间线无关，可放在任意位置。
//   · B 组在**编辑器批处理里没有帧循环** ⇒ 它手工模拟「延迟一拍的重建」（自己 `ForceMeshUpdate`），
//     再把生产那一支（`RestampQueuesAfterEnable`）跑一遍 —— 这正是 `Update` 下一帧会做的事。
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CardPresentation;

public static class B118W2_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B118W2 ✓ " + msg); }
        else { _fail++; Debug.LogError("B118W2 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5")
                  + "；实得 " + got.ToString("F5") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Group("A·悬停缩放与 y", A_HoverPose);
        Group("B·重启用后子网格队列", B_Restamp);
        Debug.Log("B118W2 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    /// <summary>跑一组并把异常收成一条失败（⛔ 不让一组的崩溃吞掉后面各组）。</summary>
    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ==================================================================
    //  A · `B117-R64` 悬停那张：缩放绝对值 1.3 · y 绝对（手牌区底边 + 30 原版世界单位 = 445 px）
    // ==================================================================
    //  判据（`CardScript__ShowCardInHand`，见判据文件 §四·1/§四·2）：
    //   ③ `localScale = Vector3.one × cardInHandShownScale`（= **1.3 本身**，⛔ 不是乘在 `m_scale 0.73` 上）
    //   ② `position = (x, PlayerHand.transform.position.y + cardInHandShownYOffset, z − 0.01)`
    //      —— `PlayerHand` 挂在 `BottomAnchor/PlayerArea/HandArea`（纯 `Transform`、`localPosition = 0`）上
    //      ⇒ 卡心 = **手牌区底边 + `cardInHandShownYOffset`**（🔴 2026-11-02 `B118-W17` 订正：**30 原版世界单位 = 445 px**，⛔ 不是 30 px），与槽位序号 / 弧高无关。
    //  🔑 灭自证：
    //    · A1 断的是「与同一把牌里**未悬停那张**的比值 = 1.3/0.73 = 1.7808」—— 改回「乘 1.3」时比值退成 1.3 ⇒ 红；
    //    · A2 断的是**两个不同槽位**都落在同一个绝对 y 上 —— 改回「槽位 y + 30」时两端卡差一个弧高 ⇒ 红。
    static void A_HoverPose()
    {
        var root = new GameObject("B118W2_A_hover");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            layout.animateRelayout = false;                    // 全 snap ⇒ 位姿当场精确，不必推补间
            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i), "B118W2_A" + i, CardFace.Hand));

            // ① 静止档（没人悬停）—— 第 1 张的缩放 = `cardScale × LayoutSpace.Scale`
            layout.Refresh(made, -1, -1);
            Vector3 lp; float la, sRest;
            bool okRest = layout.TryLastPose(made[1], out lp, out la, out sRest);
            Check(okRest && sRest > 0f, $"（前提）未悬停那张的位姿读得到（实得 scale={sRest:F5}）");

            // ② 悬停第 0 张
            layout.Refresh(made, 0, -1);
            float sHover;
            bool okHover = layout.TryLastPose(made[0], out lp, out la, out sHover);
            Check(okHover, "（前提）悬停那张的位姿读得到");

            // A1a：**绝对** 1.3（× 静止档那个窄屏系数）
            CheckClose(sHover, layout.hoverScale * LayoutSpace.Scale, 1e-3f,
                       "★ A1 `B117-R64`：悬停那张的缩放 = **绝对值 `hoverScale × LayoutSpace.Scale`**"
                     + "（原版第三句 `localScale = Vector3.one × cardInHandShownScale`）"
                     + "｜🧨 改回 `scale *= hoverScale`（0.73×1.3 = 0.949）⇒ 红");

            // A1b：与未悬停那张的**比值** = 1.3 ÷ 0.73 = 1.7808（期望值是原版两个常量的比值，不从实现里读）
            float ratio = sRest > 1e-6f ? sHover / sRest : -1f;
            CheckClose(ratio, 1.3f / 0.73f, 2e-3f,
                       "★ A1b `B117-R64`：悬停/静止 = **1.3 ÷ 0.73 = 1.7808**（原版两个 `VarsDevice` 常量之比）"
                     + "｜🧨 「乘 1.3」⇒ 比值 = 1.3 ⇒ 红");

            // A2a：悬停第 0 张的**绝对 y** = 手牌区底边 + **30 原版世界单位 = 445 px**
            //     🔴 2026-11-02（`B118-W17`）就地订正：原来这里拿 `layout.hoverLift` 自己当期望（**自证**），
            //       而那个字段当时是错的（30 px）。现在期望从**原版两个常量**算：
            //       `VarsDesktop.cardInHandShownYOffset = 30`（原版**世界单位**）× 14.835 px/单位 ÷ 108 px/我们单位。
            //     判据 = `CardScript__ShowCardInHand.c:69-79`（`Transform.get_position` 的 y + 该字段 ⇒ 世界长度）+
            //       `VarsDesktop.json` 实读（同结构的 `moveCardYOffset 0.2` / `extraSpaceOnSelectedCard 2.0` 都是世界单位）。
            const float OrigPxPerWorldUnit = 14.835f;                 // 原版 UI 相机：fov40 / 平面 100
            const float OrigCardInHandShownYOffset = 30f;             // `VarsDesktop.cardInHandShownYOffset`
            float wantLift = OrigCardInHandShownYOffset * OrigPxPerWorldUnit / 108f;   // = 4.12083 我们的单位
            Vector3 p0; float a0, sc0;
            Check(layout.TryLastPose(made[0], out p0, out a0, out sc0), "（前提）悬停第 0 张的位姿读得到");
            CheckClose(layout.hoverLift, wantLift, 1e-4f,
                       "★ A0 `B118-J9`：`hoverLift` = **30 原版世界单位**（= 445 px）换成我们的单位 = 4.12083"
                     + "｜🧨 退回「当 px 处理」的 `HoverLiftPx/108` = 0.27778（差 14.835 倍）⇒ 红");
            CheckClose(layout.hoverLift * 108f, OrigCardInHandShownYOffset * OrigPxPerWorldUnit, 0.05f,
                       $"★ A0b：`hoverLift × 108` = **445.05 px**（实得 {layout.hoverLift * 108f:F2} px）"
                     + "｜🧨 两个量纲不可能同时满足 A0 与「30 px」");
            float wantY = (layout.areaOffsetY - 0.5f) * LayoutSpace.VisibleHeight + wantLift;
            CheckClose(p0.y, wantY, 1e-3f,
                       "★ A2 `B117-R64`：悬停那张的 y = **手牌区底边 + 30 原版世界单位（445 px）**"
                     + "（原版取 `PlayerHand.transform.position.y`，那个 `Transform` 就挂在 `HandArea` 底边上）"
                     + "｜🧨 改回「槽位 y + hoverLift」⇒ 中心那张高 131.35 px ⇒ 红");

            // A2b：**换一个槽位**悬停（第 2 张，它在扇形外端、弧高 ≠ 0）⇒ 同一个绝对 y
            layout.Refresh(made, 2, -1);
            Vector3 p2; float a2, sc2;
            bool ok2 = layout.TryLastPose(made[2], out p2, out a2, out sc2);
            Check(ok2, "（前提）悬停第 2 张的位姿读得到");
            CheckClose(p2.y, wantY, 1e-3f,
                       "★ A2b `B117-R64`：**换到外端那一张**悬停，y 仍是同一个绝对值（与槽位/弧高无关；期望 = 手牌区底边 + 30 原版世界单位）"
                     + $"（第 0 张 {p0.y:F4} / 第 2 张 {p2.y:F4}）"
                     + "｜🧨 改回「槽位 y + hoverLift」⇒ 两张差一个弧高 ⇒ 红");
        }
        finally
        {
            foreach (var c in made) if (c != null) Object.DestroyImmediate(c.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  B · `B117-R73` 「收起再放开」后 TMP 子网格队列不退回 3000
    // ==================================================================
    //  做法（⛔ 不是「调一次 SetQueue 就完事」—— 那条早就有、挡不住这个缺陷）：
    //    ① 造一张**含 CJK 卡名**的卡 —— 三份拉丁字体资产把 CJK 挂成运行期 fallback
    //       （`TmpFont.EnsureCjkFallback`）⇒ TMP 会为它多造一颗子网格（`materialCount > 1`），
    //       正是本条缺陷受害的那一类（换牌面板实测 9 颗全是 `TMP SubMesh`）；
    //    ② 盖章到 3007（≠ 3000，能区分「盖过」与「没盖」）；
    //    ③ **收起 → 放开**（= 换牌面板那颗「眼睛」的那条路）；
    //    ④ **模拟「延迟一拍的重建」**（自己 `ForceMeshUpdate`，= `TMP_UpdateManager.DoRebuilds` 干的事）
    //       ⇒ 判别式：这一刻**必须**有渲染器掉回 3000，否则这组夹具没有判别力（如实红，⛔ 别读成通过）；
    //    ⑤ 跑生产那一支 `RestampQueuesAfterEnable()`（`Update` 下一帧做的同一件事）⇒ 每颗都回到 3007。
    static void B_Restamp()
    {
        var root = new GameObject("B118W2_B_restamp");
        CardView card = null;
        var forcedMats = new List<Material>();   // 🔴 2026-11-03（`B124-W27`）：② 那次「模拟重建」自己造的材质副本，收尾销毁
        try
        {
            const int QMull = 3007;
            var data = CardData.Simple("术士哈洛克", 3, 2, 5);   // CJK ⇒ 拉丁字体走 fallback ⇒ 子网格
            card = CardView.Create(root.transform, data, "B118W2_B", CardFace.Hand);
            Check(card != null, "（前提）卡建出来了");
            if (card == null) return;

            // ① 至少一颗 TMP 带子网格（否则下面验不到「子网格」那一半）
            int sub = 0;
            foreach (var t in card.GetComponentsInChildren<TextMeshPro>(true))
            {
                if (t == null || !t.gameObject.activeSelf) continue;
                t.ForceMeshUpdate();
                if (t.textInfo != null && t.textInfo.materialCount > 1) sub++;
            }
            Check(sub > 0,
                  $"（前提）这张卡的 TMP 里至少有 **1 颗带子网格**（`materialCount > 1`；实得 {sub} 颗）"
                + " —— 0 颗时本组下面三条验的是「没有子网格的情形」，⛔ 不能当成通过");

            // ② 盖章
            card.SetQueue(QMull);
            int badStamp = CountWrongQueue(card, QMull, out string diagStamp);
            Check(badStamp == 0,
                  $"（前提）盖章之后每一颗渲染器都是 {QMull}（违规 {badStamp} 颗{diagStamp}）");

            // ③ 收起 → 放开（整张卡 = 换牌面板「眼睛」那条路）
            card.gameObject.SetActive(false);
            card.gameObject.SetActive(true);
            Check(card.EnableRestampPendingForTest,
                  "★ B1 `B117-R73`：**收起再放开**之后挂起了「下一帧重盖」"
                + "（`CardView.EnableRestampPendingForTest`；判据 → 类尾 `_enableRestampPending` 的 doc）"
                + "｜🧨 删掉 `OnEnable` 里那句挂起 ⇒ 红（那样 `Update` 那一刀永远不会来）");

            // ④ 模拟「延迟一拍的重建」：
            //   🔴 2026-11-03（`B124-W27`）：本 build 的 `ForceMeshUpdate()` **不再**把子网格材质换回 3000
            //   （实测掉队 **0 颗** ⇒ 原判别式已失去判别力、B3 会变成假绿）。⇒ 改成**显式**把子网格渲染器的材质
            //   队列写回 3000（各换一份队列 = 3000 的副本，⛔ 不动 `CardView` 那份 `(源材质,队列)` 共享缓存），
            //   不再依赖 TMP 内部行为 —— 这样 B3 验的仍是「`RestampQueuesAfterEnable` 能不能把子网格盖回来」。
            foreach (var t in card.GetComponentsInChildren<TextMeshPro>(true))
                if (t != null && t.gameObject.activeInHierarchy) t.ForceMeshUpdate();
            int reverted = 0;
            foreach (var mr in card.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null || mr.sharedMaterial == null || mr.GetComponent<Label>() != null) continue;
                if (mr.GetComponent<TextMeshPro>() != null) continue;      // 只动**子网格**那几颗（缺陷受害的那一类）
                var revert = new Material(mr.sharedMaterial) { renderQueue = 3000 };
                forcedMats.Add(revert);
                mr.sharedMaterial = revert;
                reverted++;
            }
            int badAfterRebuild = CountWrongQueue(card, QMull, out string diagRebuild);
            Check(reverted > 0 && badAfterRebuild == reverted,
                  $"（夹具布置）显式模拟「重建后子网格掉回 3000」：应掉队 {reverted} 颗、实测 {badAfterRebuild} 颗"
                + $"{diagRebuild}｜🧨 布置不出 >0 颗 ⇒ 本夹具没有判别力（下面 B3 就证明不了修复），⛔ 别读成通过");

            // ⑤ 生产那一支（`Update` 下一帧干的就是这一句）
            card.RestampQueuesAfterEnable();
            int badFixed = CountWrongQueue(card, QMull, out string diagFixed);
            Check(badFixed == 0,
                  $"★ B3 `B117-R73`：重盖之后**每一颗**（含 `TMP SubMesh`）都回到盖章值 {QMull}（违规 {badFixed} 颗{diagFixed}）"
                + "｜判据：`RestampQueuesAfterEnable` = **先 `ForceMeshUpdate` 再 `SetQueue`**（顺序反了就盖不住）");
            Check(!card.EnableRestampPendingForTest,
                  "★ B4：盖完把挂起位清掉（⛔ 别让下一帧白跑一遍；`RestampQueuesAfterEnable` 的第一件事）");
        }
        finally
        {
            if (card != null) Object.DestroyImmediate(card.gameObject);
            foreach (var m in forcedMats) if (m != null) Object.DestroyImmediate(m);   // B124-W27：④ 自造的材质副本
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>数「队列不等于 <paramref name="want"/> 的渲染器」并拼一段可复核的诊断（名字=队列）。
    /// ⚠️ 跳过 `Label`（点阵文字自己实例化材质，`CardView.SetQueue` 本来就不盖它 —— 与实现同一口径）。</summary>
    static int CountWrongQueue(CardView card, int want, out string diag)
    {
        var parts = new List<string>();
        int bad = 0;
        foreach (var mr in card.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (mr == null || mr.sharedMaterial == null) continue;
            if (mr.GetComponent<Label>() != null) continue;
            int q = mr.sharedMaterial.renderQueue;
            if (q == want) continue;
            bad++;
            bool sub = mr.GetComponent<TextMeshPro>() == null;
            parts.Add($"{mr.name}{(sub ? "(子网格)" : "")}={q}");
        }
        diag = parts.Count == 0 ? "" : "：" + string.Join(" / ", parts.ToArray());
        if (diag.Length > 600) diag = diag.Substring(0, 600) + "…";
        return bad;
    }
}
