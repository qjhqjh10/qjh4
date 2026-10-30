// B99E7_Assert.cs — 执行代理 `B99_E7`（第九十九会话 · 波 1 · 日常/奖励线）的断言集
//
// ============================ 它盯的是什么（四件）============================
//   账 `日常_奖励` 的 `RaidOrArmyReleaseMenuContainer.{OnEnable,OnInitialize,RefreshProgressBar,SetAndRefreshProgressBar}`
//   （#4–#7）的**宿主渲染那一半** —— 即 `Event Progression` 子树。
//   判据文档 = `资料/完全复刻/判据/B99_R2_日常9条终局.md` §二（几何逐件现读，2026-10-29 `B99_R2` 解帧）。
//   生产实现 = `Shell/MainMenuRuntime.cs` 类尾 `CardHasEventProgression` / `BuildEventProgressionForOwner` / `BuildEventProgression`。
//   ⛔ 别在这里抄第二份几何；要改几何先改判据文档 + 生产实现。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · Gate：把 `CardHasEventProgression` 的「低卡档 return false」或「prefab 名判等」删掉 ⇒ 那两条红。
//   · StructureAndClip：把 `BuildEventProgression` 里的 `New(card,"Event Progression",…)` 三件删一件 ⇒ 三件齐那条红；
//     把 `ap.y = 122` 改成别的 ⇒ Outline 顶/底那条红；把整条挪到卡外（ap.y 换成 −400）⇒「整条在卡内」那条红。
//   · UnknownNoFake：把 `counterText ?? string.Empty` 改成原版占位串 `"100/200"`（或把 `levelText ?? ""` 改成 `"--"`）
//     ⇒ 「不显示假 counter / 假档位」红；把 `fill01 < 0` 时也建 Fill ⇒ 「不建 Fill」红。
//   · KnownProgress：把 Fill 宽度写成固定满条（不看 `fill01`）⇒ 「0.5 ⇒ 宽 193.39」红。
//     两档（0.5 与 1.0）宽度**结构上不可能同时满足** ⇒ 「恒满 / 恒半」都会被抓。
//
// ============================ 怎么用（宿主）============================
//   宿主 = `Assets/CardPresentation/Editor/MainMenuScene.cs` 的 `Run()`。
//   在**任意已有的一行**末尾追加 `B99E7_Assert.Run(_sink);`（同一行 ⇒ 行号零漂移；先例见 `MainMenuScene` 里那些
//   「同一行追加」的注释）。⚠️ 本批白名单不含宿主 ⇒ 这一行要**调度台**加；加之前本文件一条都不跑（如实标）。
//   ⚠️ 本文件**自建探针**（一棵裸 `MainMenuRuntime` + 一张裸卡节点），不依赖宿主已建好的菜单；
//     也不建资产、不写盘、不跑 Unity 批处理。

using UnityEngine;
using CardPresentation;

/// <summary>B99_E7 的全部断言（宿主：`MainMenuScene.Run` 里 `B99E7_Assert.Run(_sink);`）。</summary>
public static class B99E7_Assert
{
    /// <summary>本批唯一入口。</summary>
    public static void Run(CheckSink s)
    {
        MenuCheck.Section(s, "B99_E7 —— 日常_奖励:434（B99_R2 §二）`Event Progression` 子树：结构 / 高卡不裁切 / 低卡不建 / 未知进度不造假");
        Gate(s);
        StructureAndClip(s);
        UnknownNoFake(s);
        KnownProgress(s);
    }

    // ==============================================================================================
    //  A · 闸：只有「Raid or Army Release」这张 1×2 高卡才建（低卡档 / 通用 1×2 / 1×1 都不建）
    // ==============================================================================================
    static void Gate(CheckSink s)
    {
        const string ep = "Base Game Mode Container 1x2 Raid or Army Release";

        MenuCheck.True(s, MainMenuRuntime.CardHasEventProgression(ep, 848.8f),
            "★ 闸：`" + ep + "` @ 848.8（1×2 高卡）⇒ **建**（唯一带 `Event Progression` 的 prefab）");

        MenuCheck.True(s, !MainMenuRuntime.CardHasEventProgression(ep, 414.4f),
            "★ 闸（反面）：同一个 prefab 名 @ 414.4（低卡档）⇒ **不建**"
          + "（🧨 删掉 `h <= CardH + 1f` 那条 ⇒ 本行红；低卡档原版那个 1×1 prefab 根子树里根本没有这一件）");

        MenuCheck.True(s, !MainMenuRuntime.CardHasEventProgression("Base Game Mode Container 1x1", 414.4f),
            "★ 闸（反面）：`Base Game Mode Container 1x1` ⇒ **不建**（`1x1` prefab 根子树 = Background/Foreground/TextDarkening/Border，现读无 `Event Progression`）");

        MenuCheck.True(s, !MainMenuRuntime.CardHasEventProgression("Base Game Mode Container 1x2", 848.8f),
            "★ 闸（反面）：通用 `Base Game Mode Container 1x2` @ 848.8 ⇒ **不建**（它也没有 `Event Progression`）"
          + "（🧨 把 prefab 名判等放宽成「带 1x2 就建」⇒ 本行红）");

        MenuCheck.True(s, MainMenuRuntime.CardHasEventProgression(ep + " - SomeKind", 848.8f),
            "★ 闸：带 ` - <后缀>` 的事件卡节点名也要认（`CardHasEventProgression` 会先剥后缀）");
    }

    // ==============================================================================================
    //  B · 结构 + 高卡档几何（子树建齐、整条在卡内、位置照 §二）
    // ==============================================================================================
    static void StructureAndClip(CheckSink s)
    {
        GameObject probeGo, cardGo;
        var rt = NewProbe(out probeGo);
        var card = NewCard(out cardGo, 535f, 848.8f);
        try
        {
            rt.BuildEventProgressionForTest(card, 0f, 0f, 535f, 848.8f, null, null, -1f, null);

            var ep = MenuCheck.FindChild(card, "Event Progression");
            MenuCheck.True(s, ep != null, "(前提) 高卡(848.8)下建出了 `Event Progression` 节点（不建 ⇒ 后面几条全空转）");
            var pb = ep != null ? MenuCheck.FindChild(ep, "ProgressBar") : null;
            var sih = ep != null ? MenuCheck.FindChild(ep, "Scoring Icon Holder") : null;
            var lv = ep != null ? MenuCheck.FindChild(ep, "Level") : null;

            MenuCheck.True(s, pb != null, "★ 三件齐 ①：`Event Progression/ProgressBar`");
            MenuCheck.True(s, sih != null, "★ 三件齐 ②：`Event Progression/Scoring Icon Holder`");
            MenuCheck.True(s, lv != null, "★ 三件齐 ③：`Event Progression/Level`");

            float x1, y1, x2, y2;
            var outline = pb != null ? MenuCheck.FindChild(pb, "Outline") : null;
            bool ok = RectOf(outline, out x1, out y1, out x2, out y2);
            MenuCheck.True(s, ok, "(前提) 量到 `Outline` 的渲染矩形（缺图/缺节点 ⇒ 下面几何全空转）");
            if (ok)
            {
                MenuCheck.Near(s, y1, 679.288f, 1.0f,
                    "★ `Event Progression` 顶边 = 卡底 − 181.39 ⇒ 679.288（判据 B99_R2 §二：ap.y 122 + 高 59.39）");
                MenuCheck.Near(s, y2, 714.922f, 1.0f, "★ …底边 714.922（= 卡底 − 122 上方那条中带底）");
                MenuCheck.Near(s, x1, 74.11f, 1.0f, "★ 横向左边 74.11（= 535/2 − 386.781/2）");
                MenuCheck.Near(s, x2, 460.89f, 1.0f, "★ 横向右边 460.89");
                MenuCheck.True(s, y1 >= -0.01f && y2 <= 848.81f,
                    "★★ 高卡档（848.8）下子树**整条落在卡内 [0, 848.8]**（原版 `RectMask2D` 裁到 535×848.8 ⇒ 不会被裁掉；"
                  + "🧨 把 `ap.y` 换成 −400 之类挪出卡 ⇒ 本行红）");
            }

            var lvq = lv != null ? OwnQuad(lv) : null;
            float lx1 = 0f, ly1 = 0f, lx2 = 0f, ly2 = 0f;
            bool lok = lvq != null && MenuDraw.QuadRectPx(lvq, out lx1, out ly1, out lx2, out ly2);
            MenuCheck.True(s, lok, "(前提) 量到 `Level` 徽章渲染矩形");
            if (lok)
                MenuCheck.True(s, lx1 >= -0.01f && lx2 <= 535.01f && ly1 >= -0.01f && ly2 <= 848.81f,
                    "★ `Level` 徽章也在卡内（B99_R2 §二：anchor/pivot 0.5 · ap 188.879,−2.12 · size 53.263×55.143）");
        }
        finally
        {
            Object.DestroyImmediate(cardGo);
            Object.DestroyImmediate(probeGo);
        }
    }

    // ==============================================================================================
    //  C · 进度未知 ⇒ 不显示假值（不印 `--` / 不印 `100/200` / 不建填充条）
    // ==============================================================================================
    static void UnknownNoFake(CheckSink s)
    {
        GameObject probeGo, cardGo;
        var rt = NewProbe(out probeGo);
        var card = NewCard(out cardGo, 535f, 848.8f);
        try
        {
            rt.BuildEventProgressionForTest(card, 0f, 0f, 535f, 848.8f, null, null, -1f, null);

            var ep = MenuCheck.FindChild(card, "Event Progression");
            var pb = ep != null ? MenuCheck.FindChild(ep, "ProgressBar") : null;
            var bg = pb != null ? MenuCheck.FindChild(pb, "Background") : null;
            var fa = bg != null ? MenuCheck.FindChild(bg, "Fill Area") : null;
            var cn = pb != null ? MenuCheck.FindChild(pb, "counter") : null;
            var lv = ep != null ? MenuCheck.FindChild(ep, "Level") : null;
            var lt = lv != null ? MenuCheck.FindChild(lv, "Current Milestone Level text") : null;

            var cnLb = OwnLabel(cn);
            var ltLb = OwnLabel(lt);
            MenuCheck.True(s, cnLb != null, "(前提) `counter` 标签在（不在 ⇒ 下面两条空转）");
            MenuCheck.True(s, ltLb != null, "(前提) `Current Milestone Level text` 标签在");

            string cnText = cnLb != null ? (cnLb.Text ?? "") : null;
            string ltText = ltLb != null ? (ltLb.Text ?? "") : null;
            MenuCheck.True(s, cnText == string.Empty,
                "★ 进度未知时 **counter 不显示假值**（原版占位串是 `100/200`；实测 \"" + cnText + "\"）"
              + "（🧨 把 `counterText ?? string.Empty` 写成占位串 `100/200` ⇒ 本行红）");
            MenuCheck.True(s, ltText == string.Empty,
                "★ 进度未知时 **档位文字不显示假值**（原版占位串是 `--`；实测 \"" + ltText + "\"）"
              + "（🧨 把 `levelText ?? string.Empty` 写成 `--` ⇒ 本行红）");

            var fillGo = fa != null ? MenuCheck.FindChild(fa, "Fill") : null;
            MenuCheck.True(s, fillGo == null,
                "★ 进度未知时 **不建 `Fill` 条**（⛔ 不让空条读成「0 进度」；🧨 把 `if (fill01 >= 0f)` 去掉 ⇒ 本行红）");
        }
        finally
        {
            Object.DestroyImmediate(cardGo);
            Object.DestroyImmediate(probeGo);
        }
    }

    // ==============================================================================================
    //  D · 注入已知进度 ⇒ 真的驱动文字与填充（灭自证：0.5 与 1.0 两档宽度不同）
    // ==============================================================================================
    static void KnownProgress(CheckSink s)
    {
        GameObject probeGo, cardGo;
        var rt = NewProbe(out probeGo);
        var card = NewCard(out cardGo, 535f, 848.8f);
        try
        {
            rt.BuildEventProgressionForTest(card, 0f, 0f, 535f, 848.8f, "3", "120/200", 0.5f, null);

            var ep = MenuCheck.FindChild(card, "Event Progression");
            var pb = ep != null ? MenuCheck.FindChild(ep, "ProgressBar") : null;
            var bg = pb != null ? MenuCheck.FindChild(pb, "Background") : null;
            var fa = bg != null ? MenuCheck.FindChild(bg, "Fill Area") : null;
            var cn = pb != null ? MenuCheck.FindChild(pb, "counter") : null;
            var lv = ep != null ? MenuCheck.FindChild(ep, "Level") : null;
            var lt = lv != null ? MenuCheck.FindChild(lv, "Current Milestone Level text") : null;

            var ltLb = OwnLabel(lt);
            var cnLb = OwnLabel(cn);
            MenuCheck.True(s, ltLb != null && (ltLb.Text ?? "") == "3",
                "★ 注入 levelText=3 ⇒ 档位文字写进去了（`RefreshProgressBar` 的 `levelText` 那一支是活的）");
            MenuCheck.True(s, cnLb != null && (cnLb.Text ?? "") == "120/200",
                "★ 注入 counterText ⇒ counter 写进去了（`ProgressBar.SetProgress` 的 `{0}/{1}` 那一支）");

            var fillGo = fa != null ? MenuCheck.FindChild(fa, "Fill") : null;
            float fx1, fy1, fx2, fy2;
            bool ok = RectOf(fillGo, out fx1, out fy1, out fx2, out fy2);
            MenuCheck.True(s, ok, "(前提) 已知进度时量到 `Fill` 渲染矩形");
            if (ok)
            {
                MenuCheck.Near(s, fx2 - fx1, 193.39f, 1.0f,
                    "★ fill01=0.5 ⇒ Fill 宽 = 0.5×386.781 = 193.39（🧨 忽略 `fill01` 画成满条 386.781 ⇒ 本行红）");
                MenuCheck.Near(s, fx1, 74.11f, 1.0f, "★ Fill 左缘贴着 Fill Area 左缘 74.11（Slider `LeftToRight`）");
            }
            var endGo = fillGo != null ? MenuCheck.FindChild(fillGo, "end") : null;
            MenuCheck.True(s, endGo != null, "★ 有进度时 `end` 端点存在（原版 `Fill` 的子、骑在填充右缘）");
            float ex1, ey1, ex2, ey2;
            if (endGo != null && MenuDraw.QuadRectPx(OwnQuad(endGo), out ex1, out ey1, out ex2, out ey2))
                MenuCheck.Near(s, ex2, 270.8f, 1.2f,
                    "★ `end` 右缘 = 填充右缘 267.5 + ap 3.3 = 270.8（🧨 忽略 ap ⇒ 偏 3.3）");
        }
        finally
        {
            Object.DestroyImmediate(cardGo);
            Object.DestroyImmediate(probeGo);
        }
    }

    // ==============================================================================================
    //  夹具
    // ==============================================================================================

    static MainMenuRuntime NewProbe(out GameObject go)
    {
        // ⚠️ 编辑模式下 `AddComponent` **不触发** Awake/Start（本工程已记的坑）⇒ 裸组件即可，不需要 `Build()`。
        //    `BuildEventProgression` 只用到 `New`(静态) / `Center`(静态) / `Art` / `Rect` / `Text`（都不读 `_root`）。
        go = new GameObject("B99E7 Probe");
        return go.AddComponent<MainMenuRuntime>();
    }

    static Transform NewCard(out GameObject go, float w, float h)
    {
        go = new GameObject("B99E7 Card", typeof(RectTransform));
        MenuDraw.SetPxSize(go.transform, w, h);
        return go.transform;
    }

    /// <summary>取某个容器节点**自己那一颗** `ImageQuad`（= 直接子件里的；容器下可能还挂着别的子图的子树）。</summary>
    static ImageQuad OwnQuad(Transform container)
    {
        if (container == null) return null;
        var qs = container.GetComponentsInChildren<ImageQuad>(true);
        for (int i = 0; i < qs.Length; i++) if (qs[i] != null && qs[i].transform.parent == container) return qs[i];
        return qs.Length > 0 ? qs[0] : null;
    }

    /// <summary>取某个容器节点下的那个 `Label`（`Text()` 建的直接子件）。</summary>
    static Label OwnLabel(Transform container)
    {
        return container != null ? container.GetComponentInChildren<Label>(true) : null;
    }

    // 🔴 2026-10-30（调度台 · 配合 `B102RAID`）：三条进度条改成**九宫格**之后，原来的 `OwnQuad(container)`
    //   取到的是「容器下第一颗 quad」= 某个**角块**（20 / 10 宽）⇒ 这里必须改成**并集**量法
    //   （判据 = `MenuDraw.UnionQuadRectPx` 的 doc：九宫格/平铺/软边切块**必须**用并集，⛔ 只取第一块会量成角块）。
    //   ⚠️ `Fill` 那条的容器里还含 `end` 子树（单块，不属于 fill）⇒ **要排掉**，否则右边多算 3.3px。
    static bool RectOf(Transform container, out float x1, out float y1, out float x2, out float y2)
    {
        return MenuDraw.UnionQuadRectPx(container, MenuDraw.QuadGate.None, true,
            q =>
            {
                for (var t = q.transform; t != null && t != container; t = t.parent)
                    if (t.name == "end") return false;
                return true;
            }, out x1, out y1, out x2, out y2);
    }
}
