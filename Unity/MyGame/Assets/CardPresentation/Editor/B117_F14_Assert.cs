// B117_F14_Assert.cs — `UI-21`：**卡面文字比原版实拍大**（用户真机点名「文字压立绘」）
//   执行代理 `F14` · 2026-11-03
//
// ============================ 判据（一手，逐像素量过）============================
//   · **原版实拍**（`C:/Users/qjh36/Desktop/warpforge参考图/卡组编辑界面参考.png`，1581×887；
//     卡宽 **174 px**，由卡框竖线 301..306 / 469..474 现测；卡高 = 174 × 3.3313/2.0927 = **276 px**）：
//       · 卡名「单字 advance / 卡宽」= **6.86%**（`伊莱亚克·泽菲尔刃` 的 8 个汉字逐字间距）
//       · 卡名「墨迹高 / 卡高」= **4.12%**（11 px / 276）
//       · 效果行「墨迹高 / 卡高」= **4.12%**（12 px / 276）
//   · **我们**（`_tmp_view/shots/20261103_s117/08_pool2.png`，2560×1440；卡宽 **283 px**，卡距 350、原版 215
//     ⇒ 屏幕比 1.6192 与卡片比 1.634 差 0.9%）：
//       · 卡名 advance / 卡宽 = **9.09%**（25.75 px / 283）
//       · 卡名墨迹高 / 卡高 = **5.03%**
//       · 效果行墨迹高 / 卡高 = **5.03%**
//   · 🔴 **冲突已查清**：原版 **prefab**（`D:/4/_tmp_view/orig_ui_proj/Assets/2DCard.prefab:593` 的
//     `NameTextUnit`，fs19 × localScale 0.01）**与我们是逐像素一致的**（都是 9.08% 字宽/卡宽），
//     但 **prefab 比发行的实拍大 24%** ⇒ 按 **铁律 4「实况与解包字段冲突时以实况为准」**照实拍改。
//   · 落地比例：卡名 **×0.75**（6.86/9.09）、效果文字 **×0.78**（4.12/5.03）。
//     ⚠️ 必须**连 `m_fontSizeMax`（`NameUnitBandMax` / `DescUnitBandMax`）一起缩** —— `FitToBox` 复刻的是
//     原版 autosize，**装得下时会一路涨到 max**，只改名义字号（em 常量）根本不会变小。
//
// ============================ 断言（量【渲染出来的墨迹】，⛔ 不是比字号常量）============================
//   ① **卡名**：TMP 里**汉字字形的前进宽**（`characterInfo[].xAdvance` 换算到卡单位）÷ `CardView.Width`
//      ∈ **[6.4%, 7.3%]**。改前 9.09% ⇒ 红；改后 6.81% ⇒ 绿。
//   ② **效果文字**：汉字字形的**渲染墨迹高**（`characterInfo[].vertex_TL/BL` 的并集换算到卡单位）÷
//      `CardView.Height` ∈ **[3.7%, 4.5%]**。改前 ~5.05% ⇒ 红；改后 ~3.95% ⇒ 绿。
//   ③ **灭自证**：把 `TitleEmOfCard` / `NameUnitBandMax`（或 desc 那一对）改回 0.0570 / 1.90（2.40），
//      ①② 各自越出上界 ⇒ 必红。⚠️ 若只改 em 常量、不改 band max，① 甚至会**纹丝不动**（见上面那条
//      autosize 说明）—— 所以两条都必须落在正确的那一对量上。
//
// 夹具 = **真卡** `Uriel Ventris`（真机截图里那张「乌拉尔·文特里斯特」，卡名 9 字 / 正文 1 行
//   「才能：舰队之主」；`BattleDriver.ToCardData` 会把它解析成中文并按 `CardFace.Full` 建卡面）。
//   ⚠️ 中文档 = 卡面字最多的那一档（英文更短，过了不代表中文能过），与 `AssertFaceTextFit` 同一条口径。
//
// 宿主 = `Editor/CollectionScene.cs` 的**聚合行**（`CheckUI21Measure(win); CheckB326Uv1(win); …` 那一条，
//   同一物理行尾、`//` 注释之前追加 ⇒ **零行数变化**）。
//
// 用法（⚠️ 要主对话跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod CollectionScene.Run -logFile -
// 判绿红：`grep "B117F14"` 的**合计行**；逐条按**行首** `B117F14 ✓` / `B117F14 ✗` 数（⛔ 别 `grep -c ✗`）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using TMPro;
using UnityEngine;

public static class B117F14_Assert
{
    const string P = "B117F14 ";

    /// <summary>卡名「单字 advance / 卡宽」的合格带 —— 原版实拍 6.86%，取 ±0.45 个百分点。</summary>
    public const float NameRatioMin = 0.064f, NameRatioMax = 0.073f;
    /// <summary>效果行「墨迹高 / 卡高」的合格带 —— 原版实拍 4.12%，取 ±0.4 个百分点。</summary>
    public const float DescRatioMin = 0.037f, DescRatioMax = 0.045f;

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var langBack = Loc.Current;
        bool persistBack = Loc.PersistOverride;
        Loc.PersistOverride = true;                       // 自检不许动玩家的真设置
        GameObject root = null;
        try
        {
            if (!TmpFont.Available)
            {
                Check(false, "前提：字体资产取得到 —— 取不到 `title`/`keywords` 那两层根本不建，下面全是空转");
                return;
            }
            Loc.SetLanguage(AvailableLanguages.Chinese);  // 中文档 = 字最多那一档（同 `AssertFaceTextFit`）

            // ---- 夹具：真卡 `Uriel Ventris`（真机截图那张）----
            var pool = CardDatabase.Load();
            CardDef def = null;
            for (int i = 0; i < pool.Count; i++) if (pool[i].Name == "Uriel Ventris") { def = pool[i]; break; }
            Check(def != null, "前提：卡池里有 `Uriel Ventris`（真机截图里那张「乌拉尔·文特里斯特」9 字名 / 1 行正文）");
            if (def == null) return;

            var cdata = BattleDriver.ToCardData(def, def.Faction);   // 正文/图标只有这一条路
            root = new GameObject("b117f14_probe");
            var cv = CardView.Create(root.transform, cdata, "ui21_font_probe");   // = `CardFace.Full` / `CardUse.Standard`
            Check(cv != null, "前提：探针卡建出来了（`CardView.Create` 返回非 null）");
            if (cv == null) return;

            var title = cv.TitleText;
            var desc = cv.KeywordsText;
            Check(title != null, "前提：卡名层 `title` 建出来了（原版 `NameTextUnit`）");
            Check(desc != null, "前提：效果文字层 `keywords` 建出来了（原版 `DescTextUnit`）");
            Check(title != null && !string.IsNullOrEmpty(title.text),
                  "前提：卡名非空（实测 `" + (title != null ? title.text : "（null）") + "`）");
            Check(desc != null && !string.IsNullOrEmpty(desc.text),
                  "前提：效果文字非空（实测 `" + (desc != null ? desc.text : "（null）") + "`）");
            if (title == null || desc == null) return;

            // ---- ① 卡名：汉字前进宽 / 卡宽 ----
            float nameAdv, nameInk, descAdv, descInk; int nameN, descN;
            bool okName = CjkMetrics(cv.transform, title, out nameAdv, out nameInk, out nameN);
            bool okDesc = CjkMetrics(cv.transform, desc, out descAdv, out descInk, out descN);
            Check(okName, $"前提：卡名层量得出**汉字**字形（实测 {nameN} 个；0 ⇒ 夹具不是中文档，① 拿 NaN 假红）");
            Check(okDesc, $"前提：效果文字层量得出**汉字**字形（实测 {descN} 个；0 ⇒ 正文没解析出来，② 拿 NaN 假红）");
            if (!okName || !okDesc) return;

            float nameRatio = nameAdv / CardView.Width;        // 卡单位 ÷ 卡单位
            float descRatio = descInk / CardView.Height;
            Debug.Log($"{P}[UI-21] 实测：卡名单字 advance {nameAdv:F4} 卡单位 / 卡宽 {CardView.Width:F4}"
                    + $" = **{nameRatio * 100f:F2}%**（墨迹高 {nameInk:F4}，{nameN} 个汉字；卡名 `{title.text}`）"
                    + $"｜效果行墨迹高 {descInk:F4} / 卡高 {CardView.Height:F4} = **{descRatio * 100f:F2}%**"
                    + $"（单字 advance {descAdv:F4}，{descN} 个汉字；正文 `{desc.text}`）");

            Check(nameRatio >= NameRatioMin && nameRatio <= NameRatioMax,
                  $"UI-21 ① 卡名「单字 advance / 卡宽」= **{nameRatio * 100f:F2}%** ∈"
                + $" [{NameRatioMin * 100f:F2}%, {NameRatioMax * 100f:F2}%]"
                + $"（实测 advance {nameAdv:F4} 卡单位 · 卡宽 {CardView.Width:F4} · {nameN} 个汉字）"
                + "｜判据 = 原版**实拍** 6.86%（prefab 那个 9.08% 比 shipped 大 24%，按铁律 4 采实拍）"
                + "｜🧨 把 `TitleEmOfCard`/`NameUnitBandMax`（或 `…Zoom` / Big 那两档）改回 0.0570/1.90 ⇒ 9.09% ⇒ 红");

            Check(descRatio >= DescRatioMin && descRatio <= DescRatioMax,
                  $"UI-21 ② 效果文字「墨迹高 / 卡高」= **{descRatio * 100f:F2}%** ∈"
                + $" [{DescRatioMin * 100f:F2}%, {DescRatioMax * 100f:F2}%]"
                + $"（实测墨迹高 {descInk:F4} 卡单位 · 卡高 {CardView.Height:F4} · {descN} 个汉字）"
                + "｜判据 = 原版**实拍** 4.12%（prefab 那档比 shipped 大 22%，按铁律 4 采实拍）"
                + "｜🧨 把 `DescUnitBandMax`（或 `DescTacticBandMax` / `Desc*BigBandMax`）改回 2.40 / 1.90 / 2.60 ⇒ 5.05% ⇒ 红");
        }
        catch (System.Exception e)
        {
            // ⛔ 别读成通过 —— 后面的断言**没跑到**
            Check(false, "宿主抛异常（后面的断言没跑到）：" + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            Loc.SetLanguage(langBack);
            Loc.PersistOverride = persistBack;
        }
        Debug.Log(P + "合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    /// <summary>量一层 TMP 里**汉字字形**的两个渲染量（全部换算到**卡单位**，用卡根当标架）：
    /// <paramref name="avgAdvance"/> = 汉字前进宽（= 该面渲染汉字的 em，与 `CardFaceProbe.CheckFaceSize` ② 同法）、
    /// <paramref name="medianInkH"/> = 汉字字形**四角并集**高的中位数（= 可见墨迹高，与 `InkBoxCardLocal` 同一条判据）。
    /// <para>🔴 **`characterInfo[].xAdvance` 是【累积笔位】，不是单字宽** —— 单字前进宽 = 相邻两个汉字的
    /// `xAdvance` 之差（`CardFaceProbe.MeasureCjkAdvancePerFontSize` 用的就是这条，`xAdvance[1] − xAdvance[0]`）。
    /// 直接拿 `xAdvance` 当单字宽会随下标线性放大。取**中位数**（不是均值）避开行末那个被 `maxAdvanceOffset`
    /// 再修一次的字（`TMP_Text.cs:5049`）。</para>
    /// <para>⚠️ 「四角是否全 0」= 不可见字形（与生产 `CardView.InkBox` / `CardFaceProbe.InkSpanY` 逐字同一条），
    /// ⛔ 别用 `isVisible`（它在 TMP 内部会被后续流程改写）。</para>
    /// <para>⚠️ 标架必须是**卡根**：期望值 `CardView.Width/Height` 就是卡单位，而 `2DCard/Front` 的位移/缩放
    /// （倾摆、`F14` 这批不动它）会污染读数。</para></summary>
    static bool CjkMetrics(Transform card, TextMeshPro t, out float avgAdvance, out float medianInkH, out int nCjk)
    {
        avgAdvance = 0f; medianInkH = 0f; nCjk = 0;
        if (card == null || t == null) return false;
        t.ForceMeshUpdate();                       // 批处理没有帧循环
        var ti = t.textInfo;
        if (ti == null || ti.characterInfo == null) return false;
        int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
        // 卡标架下的 x 单位长度（`xAdvance` 是 TMP 局部单位，父链若有缩放要换过来）
        Vector3 o0 = card.InverseTransformPoint(t.transform.TransformPoint(Vector3.zero));
        Vector3 o1 = card.InverseTransformPoint(t.transform.TransformPoint(Vector3.right));
        float xUnit = Vector3.Distance(o0, o1);
        var advs = new List<float>(n);
        var hs = new List<float>(n);
        int prevHan = -1;
        for (int i = 0; i < n; i++)
        {
            var ch = ti.characterInfo[i];
            if (!IsHan(ch.character)) continue;     // 只量汉字：图标（`<sprite>`）/标点/拉丁的墨高与前进宽不是同一把尺
            Vector3 bl = ch.vertex_BL.position, tl = ch.vertex_TL.position;
            Vector3 tr = ch.vertex_TR.position, br = ch.vertex_BR.position;
            if (bl.x == 0f && bl.y == 0f && tl.x == 0f && tl.y == 0f
                && tr.x == 0f && tr.y == 0f && br.x == 0f && br.y == 0f) continue;   // 不可见字形
            // 前进宽：只取**紧邻的**两个汉字（中间夹标点/图标那两个的差里含别人的宽度）
            if (prevHan >= 0 && i == prevHan + 1)
            {
                float d = (ch.xAdvance - ti.characterInfo[prevHan].xAdvance) * xUnit;
                if (d > 1e-5f) advs.Add(d);
            }
            prevHan = i;
            // 墨迹高：四角并集的 y 跨度
            Vector3 c0 = card.InverseTransformPoint(t.transform.TransformPoint(bl));
            Vector3 c1 = card.InverseTransformPoint(t.transform.TransformPoint(tl));
            Vector3 c2 = card.InverseTransformPoint(t.transform.TransformPoint(tr));
            Vector3 c3 = card.InverseTransformPoint(t.transform.TransformPoint(br));
            float hi = Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y));
            float lo = Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y));
            if (hi - lo > 1e-5f) hs.Add(hi - lo);
            nCjk++;
        }
        if (nCjk == 0 || advs.Count == 0 || hs.Count == 0) return false;
        advs.Sort();
        avgAdvance = advs[advs.Count / 2];
        hs.Sort();
        medianInkH = hs[hs.Count / 2];
        return avgAdvance > 1e-5f && medianInkH > 1e-5f;
    }

    /// <summary>汉字（含扩展 A）—— 卡面中文档里只有它们是「一字 = 一个 em」的等宽全角字形。</summary>
    static bool IsHan(char c) { return (c >= '\u3400' && c <= '\u4DBF') || (c >= '\u4E00' && c <= '\u9FFF'); }
}
