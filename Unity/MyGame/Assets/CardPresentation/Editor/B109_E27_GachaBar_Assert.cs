// B109_E27_GachaBar_Assert.cs — 执行代理 `B109_E27`（2026-10-31 · 第一百零九会话）
//
// ============================ 盯的是什么 ============================
//   `Shell/GachaEventWindow.cs` 的进度条填充 `Fill` 用错了图：
//   · 原版：`Gacha Tab ▸ Footer ▸ Player doesn't have all items ▸ ProgressBar ▸ Background ▸ Fill Area ▸ Fill`
//     那颗 `Image.m_Sprite` = PID `757582629421741544` = **`40k_campaign_bar_fill color`**（带空格）。
//   · 我们（改前）：常量 `ArtBarFill = "40k_campaign_bar_fill"` —— **少了 `color` 那档**（两张是**不同 sprite**）。
//
// ============================ 期望值从哪来（⛔ 灭自证）============================
//   下表每一格都是**原版字面量**，不是从我们自己的常量反算：
//     python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all --rt -3658555461922138495 --depth 0 --relative
//   （或直接读 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3214451262695876991.json`）
//
//   | 项             | 原版值                                                   | 出处 |
//   |----------------|----------------------------------------------------------|------|
//   | `m_Sprite` 名   | `40k_campaign_bar_fill color`（落盘名 = 空格→下划线）     | PID `757582629421741544` → `Sprite/40k_campaign_bar_fill color.json` 的 `m_Name` |
//   | sprite 尺寸     | 42×18                                                    | 同 json 的 `m_Rect` / `textureRect` |
//   | `m_Color`       | (1, 0.740566, 0.8215345, 1)                              | `MonoBehaviour_-3214451262695876991.json` 的 `m_Color` |
//   | `m_Type`        | 1（Sliced）                                              | 同上（九宫 (10,0,10,0) 是我们已对齐的那一档） |
//
//   ⛔ **不从 `GachaEventWindow.ArtBarFill` / `BarFillTint` 反算** —— 那正是改前那个错。
//      本件比的是**原版字面量**：有人把常量改回不带 `color` 的那张 ⇒ ③ 红。
//
// ============================ 两张图为什么不能互相顶（实测）============================
//   同一张 42×18 PNG，中心像素：`40k_campaign_bar_fill` = **(143,143,143)**（灰）；
//   `40k_campaign_bar_fill_color` = **(255,255,255)**（白）。同乘我们已对齐的 `BarFillTint`
//   （= 原版 `m_Color`）后，前者只有原版亮度 ~56% **且偏灰**。
//   ⚠️ 运行期读不到像素（两张 `Resources` 贴图 `isReadable: 0`）⇒ **像素/md5 这一半在离线核**
//      （见 `报告/B109_E27.md`：两张 PNG 的 md5 与 `d:/2/Warpforge_tools/data/ui_extract/atlasgroup_assets_all/Sprite/` 的原版切片**逐字节一致**）；
//      运行期这一半靠「名字 = 原版字面量」+「两张确实是不同对象/不同名」两条合判（见 ⑤）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B109_E27_GachaBar_Assert.Run(); pass += B109_E27_GachaBar_Assert.Pass; fail += B109_E27_GachaBar_Assert.Fail;`
//   接在某个宿主（`Editor/BattleScene.cs` / `Editor/ShellScene.cs` 等）的「合计」聚合行**之前**、**同一物理行追加**。
//   🔴 追加前先看清那一行第一个 `//` 在哪：落在它**之后** = 代码进注释，**这条断言一次都不会跑**
//      （`typecheck` / 账闸全绿、退出码 0，而一条都没执行）。
//
// ============================ 边界 ============================
//   · 只读：建一扇裸窗读它的结点 + 两个自检读口，**不写任何游戏状态**；结点在 `finally` 里 `DestroyImmediate`。
//   · 不依赖 `WindowsManager`（同 `B94B5_Assert` 的先例：裸 `AddComponent` + `Build()` 就能建树）。
using System;
using CardPresentation;
using UnityEngine;

public static class B109_E27_GachaBar_Assert
{
    const string P = "[B109_E27] ";

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    // ============================================================ 原版字面量（⛔ 别从我们的常量抄）
    const string WantSprite = "40k_campaign_bar_fill_color";      // 原版 `m_Sprite.m_Name` = `40k_campaign_bar_fill color`（空格→下划线）
    const string WrongSprite = "40k_campaign_bar_fill";           // 改前那张（灰），⛔ 不能用来顶
    const int WantW = 42, WantH = 18;
    static readonly Color WantTint = new Color(1f, 0.740566f, 0.8215345f, 1f);   // 原版 `m_Color`

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try { RunBar(); }
        catch (Exception e) { Ok(false, "段异常（算红，⛔ 不是跳过）: " + e); }
        finally { Debug.Log(P + $"=== B109_E27 合计：{Pass} 通过 / {Fail} 失败 ==="); }
    }

    static void RunBar()
    {
        Debug.Log(P + "--- `GachaEventWindow` 进度条 `Fill`：`40k_campaign_bar_fill_color`（原版 `_color` 档）---");

        var go = new GameObject("B109_E27_Gacha");
        GachaEventWindow win = null;
        // 🔴 把「奖励数据源」显式置空再建：别的自检可能给静态口装过 provider（`Build → RefreshRewards →
        //    SetRewardProgress` 会把 `Fill` 提前撑开），那样 ②a「出厂 0 宽」就不成立了。收尾还原。
        var prevProvider = GachaEventWindow.RewardsProvider;
        GachaEventWindow.RewardsProvider = null;
        try
        {
            win = go.AddComponent<GachaEventWindow>();

            // 前提（硬检查）：`Build()` 不抛 —— 抛了就算红并**停止**（后面的断言没有树可读）
            bool built = true;
            try { win.Build(); }
            catch (Exception e) { built = false; Ok(false, "①-前提：`GachaEventWindow.Build()` 不抛（实抛 " + e.GetType().Name + "：" + e.Message + "）"); }
            if (!built) return;

            // ---- ① 原版结构：Footer ▸ Player doesn't have all items ▸ ProgressBar ▸ Background ▸ Fill Area ▸ Fill ----
            var footer = Deep(win.transform, "Footer");
            Ok(footer != null, "①a 前提：`Footer` 在（原版 `Gacha Tab ▸ Footer`）");
            var noAll = footer != null ? Child(footer, "Player doesn't have all items") : null;
            Ok(noAll != null, "①b 前提：`Player doesn't have all items` 在（原版那一支）");
            var bar = noAll != null ? Child(noAll, "ProgressBar") : null;
            Ok(bar != null, "①c 前提：`ProgressBar` 在");
            var bg = bar != null ? Child(bar, "Background") : null;
            Ok(bg != null, "①d 前提：`ProgressBar ▸ Background` 在");
            var area = bg != null ? Child(bg, "Fill Area") : null;
            Ok(area != null, "①e 前提：`Background ▸ Fill Area` 在");
            var fill = area != null ? Child(area, "Fill") : null;
            Ok(fill != null, "①f ★ 前提：`Fill Area ▸ Fill` 在（原版那颗被换图的节点）");

            // ---- ② 出厂 0 宽（进度 0）不许出错 ----
            //   判据：原版 `FillR` 的 x1 == x2（我们照抄）；`Slider` 的 Fill 在 value=0 时也是 0 宽。
            //   ⚠️ 这一档**九宫格一个子块都不建**（`CreateNineSlice` 里 `w <= 0` 全跳过）⇒ 树上看不到图，
            //      「图对不对」只能走 ③ 的读口。这里只断「不出错、不静默丢图」。
            float wpx = 0f;
            if (fill != null)
            {
                var rt = fill as RectTransform;
                if (rt != null) wpx = rt.sizeDelta.x * 108f;   // `MenuDraw.SetPxSize` 的反换算（`LayoutSpace.Px` = px ÷ 108）
            }
            Ok(fill != null && Mathf.Abs(wpx) < 0.01f,
               "②a ★ 出厂 `Fill` = **0 宽**（进度 0；实得 " + wpx.ToString("F2") + " px）—— 原版 `FillR` 的 x1 == x2");
            bool missFill = win.MissingArt != null
                         && (win.MissingArt.Contains(WantSprite) || win.MissingArt.Contains(WrongSprite));
            Ok(!missFill,
               "②b ★★ 填充图**没被记进缺图表**（`MissingArt` 共 " + (win.MissingArt != null ? win.MissingArt.Count : -1) + " 条"
             + (win.MissingArt != null && win.MissingArt.Count > 0 ? "：" + string.Join("、", win.MissingArt.ToArray()) : "")
             + "）—— `Tex()` 取不到会出声并登记；这里断的是「这条链真的取到了图」而不是「整窗一张不缺」");

            // ---- ③ ★★ 贴图 = 原版字面量（改回不带 `color` 那张 ⇒ 这里红）----
            var tex = win.BarFillTexture;
            Ok(tex != null,
               "③a ★★ 进度条填充图取得到（`CardArt.MenuUi(\"" + WantSprite + "\")` ≠ null）");
            if (tex != null)
            {
                Ok(tex.name == WantSprite,
                   "③b ★★★ 填充图 = `" + WantSprite + "`（实得 `" + tex.name + "`）—— 期望值是**原版 `m_Sprite.m_Name`**"
                 + "（PID 757582629421741544）；🧨 把常量改回 `\"" + WrongSprite + "\"` ⇒ 本条红");
                Ok(tex.width == WantW && tex.height == WantH,
                   "③c 贴图尺寸 = " + WantW + "×" + WantH + "（实得 " + tex.width + "×" + tex.height
                 + "）—— 判据 = 原版 sprite 的 `m_Rect` / `textureRect`");
            }

            // ---- ④ tint = 原版 `m_Color`（⛔ 别用「调色」去补图的亮度差）----
            var tint = win.BarFillTintValue;
            bool near = Mathf.Abs(tint.r - WantTint.r) < 0.005f && Mathf.Abs(tint.g - WantTint.g) < 0.005f
                     && Mathf.Abs(tint.b - WantTint.b) < 0.005f && Mathf.Abs(tint.a - WantTint.a) < 0.005f;
            Ok(near, "④ ★★ `BarFillTint` = 原版 `m_Color` " + Fmt(WantTint) + "（实得 " + Fmt(tint) + "）"
                   + "｜🧨 有人改成「更亮的白」去给不带 color 那张补亮度 ⇒ 本条红（那种改法是错的：原版就是乘这个色）");

            // ---- ⑤ 灭自证：两张是**不同 sprite**（不是同一张的别名）----
            var a = CardArt.MenuUi(WantSprite);
            var b = CardArt.MenuUi(WrongSprite);
            Ok(a != null && b != null, "⑤a 前提：两张图都在 `Resources/`（`" + WantSprite + "` / `" + WrongSprite + "`）");
            if (a != null && b != null)
            {
                Ok(!ReferenceEquals(a, b) && a.name != b.name,
                   "⑤b ★★ 两张是**不同资产**（实得 `" + a.name + "` vs `" + b.name + "`，同一对象 = "
                 + ReferenceEquals(a, b) + "）—— 若有人把其中一张换成另一张的拷贝「凑绿」⇒ 本条红");
            }

            // ---- ⑥ 改进度这条路不抛（0 宽 → 非 0 宽）----
            bool threw = false;
            try { win.SetRewardProgress(3, 6); }
            catch (Exception e) { threw = true; Ok(false, "⑥a 段异常：`SetRewardProgress(3, 6)` 抛了 " + e.GetType().Name + "：" + e.Message); }
            if (!threw)
                Ok(win.ProgressValue == 3 && win.ProgressMax == 6,
                   "⑥a ★ 进度 3/6 写进去了（实得 " + win.ProgressValue + "/" + win.ProgressMax + "）");
            if (fill != null)
            {
                var rt = fill as RectTransform;
                float w2 = rt != null ? rt.sizeDelta.x * 108f : 0f;
                // 原版 `Fill Area` 宽 514.0 px（1574.4 − 1060.4）× 3/6 = 257.0 px
                Ok(Mathf.Abs(w2 - 257f) < 0.5f,
                   "⑥b ★ 进度 3/6 ⇒ `Fill` 宽 = 原版 `Fill Area` 514.0 × 0.5 = **257.0 px**（实得 " + w2.ToString("F2") + " px）");
            }
        }
        finally
        {
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            else { try { UnityEngine.Object.DestroyImmediate(go); } catch { } }
            GachaEventWindow.RewardsProvider = prevProvider;
        }
    }

    static string Fmt(Color c)
    {
        return "(" + c.r.ToString("F3") + "," + c.g.ToString("F3") + "," + c.b.ToString("F3") + "," + c.a.ToString("F3") + ")";
    }

    static Transform Child(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }

    static Transform Deep(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = Deep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
