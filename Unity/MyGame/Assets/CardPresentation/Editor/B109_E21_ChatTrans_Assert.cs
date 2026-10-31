// B109_E21_ChatTrans_Assert.cs — 第一百零九会话 · AUD05 J2（聊天翻译三态视觉）执行代理 `B109_E21` 的断言
//   施工单 = `资料/完全复刻/判据/B109_R13_AUD05剩余.md` §2（材质三份的逐字段实读值 / 状态机 / 改法 / 断言要点）。
//   报告   = `资料/完全复刻/报告/B109_E21.md`。
//
// ============================ 它盯的是什么 ============================
//  原版 `ChatMessageUI.SetMessageText()`（`out_il2csharp/ChatMessageUI.cs:126-152`）三态**只换 `fontMaterial`**：
//    `Normal(0)`      ⇒ `normalFontMaterial`（**不动** —— 就是我方字体资产自带那份 `Pragati-Regular Atlas Material`）
//    `Translating(1)` ⇒ `Pragati-Regular Light Green` + `text = text.TrimEnd('\n') + " (Translating 词条)"`
//    `Translated(2)`  ⇒ `Pragati-Regular Light Cream`
//  三份材质的图集 / shader / 字形**完全相同**，差别只有 `_FaceColor` + `_FaceDilate`/`_OutlineWidth`
//  + `_StencilRead/WriteMask`（`d:/2/新解包资源/assets_full/bundle_fonts_assets_all/Material/` 实读）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B109_E21_ChatTrans_Assert.Run -logFile "d:/4/_tmp_view/b109e21.log"
//   🔴 **接线由调度台做**（9 个断言宿主不在本代理白名单）：在 `Editor/ShellScene.cs` 的 `ShellScene.Run()`
//      合计行之前**同一行追加**（行数零变化，照既有代接线的做法）：
//          B109_E21_ChatTrans_Assert.Run(); _sink.Pass += B109_E21_ChatTrans_Assert.Pass; _sink.Fail += B109_E21_ChatTrans_Assert.Fail;
//   ⚠️ 本件**不需要宿主已开着聊天窗**：自己建探针根 + 调生产建树口 `ChatPanel.Build()`；
//     进来先快照 `SocialData.ChatMessages`，收尾原样还原（⛔ 不污染别的谱系的断言）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `BodyText` 的 `TrimEnd('\n')` 改成 `TrimEnd()` ⇒ A3 红（末尾两个空格被吃掉）。
//   · 删掉 `" (" + 词条 + ")"` 那一段 ⇒ A3 / C6 红。
//   · 把两份新 `.mat` 的 `_FaceColor` 抄错一格 ⇒ B3–B4 / B8 红（期望值是**解包原版字面量**，⛔ 不从材质回读）。
//   · 让 `ApplyTranslationFont` 三态都换同一份材质 ⇒ C7 / C8 / C10 红。
//   · 让 Normal 也换成新材质 ⇒ C4 红（Normal 的材质名必须是字体资产自带那份）。
//   · 把 `SetFontMaterial` 的 `new Material(m)` 去掉、直接赋工程资产 ⇒ C11 红（写队列会落到工程资产上）。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B109_E21`：聊天消息的翻译三态视觉（Normal / Translating / Translated）。</summary>
public static class B109_E21_ChatTrans_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E21] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, string msg) { MenuCheck.Near(_sink, got, want, 1e-4f, msg); }

    // ============================================================ 常量（判据侧字面量，⛔ 不读实现）
    /// <summary>原版三份 `m_Name`（`bundle_fonts_assets_all/Material/`）。</summary>
    const string NameNormal = "Pragati-Regular Atlas Material";
    const string NameGreen = "Pragati-Regular Light Green";
    const string NameCream = "Pragati-Regular Light Cream";

    /// <summary>原版 `_FaceColor`（`Material_-5050103597772954521` / `-7792147938614732247` / `122820007967670363` 实读）。</summary>
    static readonly Color FaceNormal = new Color(1f, 1f, 1f, 1f);
    static readonly Color FaceGreen = new Color(0.8820755f, 1f, 0.9557783f, 1f);
    static readonly Color FaceCream = new Color(0.9921569f, 0.8675871f, 0.7333333f, 1f);

    /// <summary>原版 `_FaceDilate` / `_OutlineWidth` / `_ScaleRatioB,C`（同上三份实读）。</summary>
    const float DilateNormal = 0f, DilateTrans = 0.05f;
    const float ScaleBNormal = 0.8928571f, ScaleBTrans = 0.8452381f;

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109E21 聊天翻译三态（Normal / Translating / Translated）：材质 · 正文后缀 · 真渲染");
        SectionA_BodyText();
        SectionB_MaterialAssets();
        SectionC_RenderedRows();
        Debug.Log(_sink.P + "B109E21 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static SocialData.ChatMessage Msg(string text, int status)
    {
        return new SocialData.ChatMessage
        {
            Channel = ChatPanel.Channels[0],
            Type = SocialData.ChatMessage.TypeMessage,
            Sender = "B109E21",
            Text = text,
            TranslationStatus = status,
            Mine = false,
        };
    }

    static Transform ContentOf(ChatPanel win)
    {
        var tab = win != null && win.CurrentTabView is ChatTab ? (ChatTab)win.CurrentTabView : null;
        if (tab == null) return null;
        var vp = tab.transform.Find("Viewport");
        return vp != null ? vp.Find("Content") : null;
    }

    /// <summary>按建出来的顺序取每一行的正文 `Label`（读**渲染那棵树**，⛔ 不读数据表）。</summary>
    static List<Label> MsgLabels(Transform content)
    {
        var list = new List<Label>();
        if (content == null) return list;
        for (int i = 0; i < content.childCount; i++)
        {
            var row = content.GetChild(i);
            if (row.name != "ChatMessageRow") continue;
            var msg = row.Find("Message");
            list.Add(msg != null ? msg.GetComponent<Label>() : null);
        }
        return list;
    }

    // ============================================================ A 纯函数：三态正文
    static void SectionA_BodyText()
    {
        MenuCheck.Section(_sink, "A 正文串 = 原版 `SetMessageText` 那串赋值跑完的结果（纯函数 `ChatMessageRow.BodyText`）");

        string tip = Loc.T(ChatMessageRow.TranslatingTipTerm);
        Tr(!string.IsNullOrEmpty(tip), "A0 **前提**：词条 `" + ChatMessageRow.TranslatingTipTerm + "` 解析出真文案（=「" + tip + "」）");
        Tr(tip != ChatMessageRow.TranslatingTipTerm, "A0b **前提**：该词条**不是印键名**（官方 13 语言表里有它）");

        const string raw = "Hello world  \n\n";        // 末尾两个空格 + 两个换行（同时验「只削 \n」
        C(ChatMessageRow.BodyText(Msg("Hello world", ChatMessageRow.TransNormal)), "Hello world",
          "A1 Normal：正文原样（⛔ 不削不改）");
        C(ChatMessageRow.BodyText(Msg(raw, ChatMessageRow.TransNormal)), raw,
          "A1b Normal：带换行/空格的原文也原样（削/追只发生在 Translating 那一支）");
        C(ChatMessageRow.BodyText(Msg("Bonjour le monde", ChatMessageRow.TransTranslated)), "Bonjour le monde",
          "A2 Translated：正文原样（材质换、文字不追加任何东西）");
        var dT = Msg("Bonjour le monde", ChatMessageRow.TransTranslated); dT.TranslatedText = "Bonjour (traduit)";
        C(ChatMessageRow.BodyText(dT), "Bonjour (traduit)",
          "A2b Translated：`TranslatedText` 非空 ⇒ 正文取它（原版 `translatedMessage ?? msg`，判据 `ChatMessageData.GetMessage()`）");

        string want = "Hello world  " + " (" + tip + ")";
        C(ChatMessageRow.BodyText(Msg(raw, ChatMessageRow.TransTranslating)), want,
          "A3 ★ Translating：只削结尾 `\\n`、**末尾两个空格留住**，再追加 ` (词条)`"
        + "｜🧨 改成 `TrimEnd()` ⇒ 红");

        string got = ChatMessageRow.BodyText(Msg(raw, ChatMessageRow.TransTranslating));
        Tr(got.IndexOf('\n') < 0, "A4 Translating 的正文里**没有**换行（`TrimEnd('\\n')` 真跑了）");
        Tr(got.EndsWith(")"), "A5 Translating 的正文以 `)` 收尾（后缀真的追加上了）");
    }

    // ============================================================ B 两份新 `.mat` 的字段
    static void SectionB_MaterialAssets()
    {
        MenuCheck.Section(_sink, "B 两份新 `.mat` 的字段 = 解包原版实读值（`assets_full/bundle_fonts_assets_all/Material/`）");

        var green = Resources.Load<Material>("Fonts/Pragati-Regular Light Green");
        var cream = Resources.Load<Material>("Fonts/Pragati-Regular Light Cream");
        Tr(green != null, "B0 **前提**：`Resources/Fonts/Pragati-Regular Light Green.mat` 能加载");
        Tr(cream != null, "B0b **前提**：`Resources/Fonts/Pragati-Regular Light Cream.mat` 能加载");
        if (green == null || cream == null) return;

        C(green.name, NameGreen, "B1 绿材质名 = 原版 `m_Name`");
        C(cream.name, NameCream, "B2 米黄材质名 = 原版 `m_Name`");

        Near(green.GetColor("_FaceColor").r, FaceGreen.r, "B3 绿 `_FaceColor.r`（原版 0.8820755）");
        Near(green.GetColor("_FaceColor").b, FaceGreen.b, "B3b 绿 `_FaceColor.b`（原版 0.9557783）");
        Near(cream.GetColor("_FaceColor").r, FaceCream.r, "B4 米黄 `_FaceColor.r`（原版 0.9921569）");
        Near(cream.GetColor("_FaceColor").g, FaceCream.g, "B4b 米黄 `_FaceColor.g`（原版 0.8675871）");
        Near(cream.GetColor("_FaceColor").b, FaceCream.b, "B4c 米黄 `_FaceColor.b`（原版 0.7333333）");

        Near(green.GetFloat("_FaceDilate"), DilateTrans, "B5 绿 `_FaceDilate` = 0.05（原版）");
        Near(green.GetFloat("_OutlineWidth"), 0.05f, "B5b 绿 `_OutlineWidth` = 0.05（原版）");
        Near(green.GetFloat("_ScaleRatioB"), ScaleBTrans, "B5c 绿 `_ScaleRatioB` = 0.8452381（原版）");
        Near(green.GetFloat("_ScaleRatioC"), ScaleBTrans, "B5d 绿 `_ScaleRatioC` = 0.8452381（原版）");
        Near(cream.GetFloat("_FaceDilate"), DilateTrans, "B6 米黄 `_FaceDilate` = 0.05（原版）");
        Near(cream.GetFloat("_ScaleRatioB"), ScaleBTrans, "B6b 米黄 `_ScaleRatioB` = 0.8452381（原版）");

        // Normal 那一份 = 字体资产自带的（原版 `normalFontMaterial` 的等价物），**不另建**
        var fa = TmpFont.FaceAsset(TmpFont.Face.LatinCardBody);
        Tr(fa != null, "B7 **前提**：Pragati 字体资产能加载（`TmpFont.FaceAsset(LatinCardBody)`）");
        var normal = fa != null ? fa.material : null;
        Tr(normal != null, "B7b **前提**：字体资产自带那份材质在");
        if (normal != null)
        {
            C(normal.name, NameNormal, "B8 ★ Normal 用字体资产自带那份（`Pragati-Regular Atlas Material`），⛔ 不另建");
            Near(normal.GetColor("_FaceColor").r, FaceNormal.r, "B9 Normal `_FaceColor.r` = 1（原版）");
            Near(normal.GetFloat("_FaceDilate"), DilateNormal, "B10 Normal `_FaceDilate` = 0（原版）");
            Near(normal.GetFloat("_ScaleRatioB"), ScaleBNormal, "B10b Normal `_ScaleRatioB` = 0.8928571（原版）");
        }

        var texN = normal != null ? normal.GetTexture("_MainTex") : null;
        var texG = green.GetTexture("_MainTex");
        var texC = cream.GetTexture("_MainTex");
        Tr(texG != null && texC != null && texN != null, "B11 **前提**：三份的 `_MainTex` 都取得到");
        Tr(ReferenceEquals(texG, texC) && ReferenceEquals(texG, texN),
           "B12 ★ 三份用**同一张**图集（原版 `_MainTex` 三份同 PathID 8692869601067476071 ⇒ 视觉差异只在颜色/膨胀）");
    }

    // ============================================================ C 真渲染：三态各一行
    static void SectionC_RenderedRows()
    {
        MenuCheck.Section(_sink, "C 真渲染：三态各建一行，读 `ChatMessageRow/Message` 那颗 `Label` 的材质与正文");

        var saved = new List<SocialData.ChatMessage>(SocialData.ChatMessages);
        SocialData.ChatMessages.Clear();
        ChatPanel chat = null;
        try
        {
            var go = new GameObject("B109E21_ChatProbe", typeof(RectTransform));
            chat = go.AddComponent<ChatPanel>();
            chat.Build();                                          // 生产建树口（⛔ 不复制一份建法）
            if (chat.tabButtons != null) chat.tabButtons.Click(0); // 第 0 页活着（`RefreshMessages` 只重画 active 那页）

            var content = ContentOf(chat);
            Tr(content != null, "C0 **前提**：消息行挂点 `Chat Tab/Viewport/Content` 在（不在 ⇒ 下面全部不可信，⛔ 不是绿）");
            if (content != null)
            {
                SocialData.ChatMessages.Add(Msg("Hello world", ChatMessageRow.TransNormal));
                SocialData.ChatMessages.Add(Msg("Hello world  \n\n", ChatMessageRow.TransTranslating));
                var m3 = Msg("Bonjour le monde", ChatMessageRow.TransTranslated);
                m3.TranslatedText = "Bonjour le monde (FR)";           // 原版 `translatedMessage` 非空 ⇒ 正文取它
                SocialData.ChatMessages.Add(m3);
                chat.RefreshMessages();

                var lbs = MsgLabels(content);
                C(lbs.Count, 3, "C1 **前提**：三行都真建出来了");

                var lbN = lbs.Count > 0 ? lbs[0] : null;
                var lbT = lbs.Count > 1 ? lbs[1] : null;
                var lbD = lbs.Count > 2 ? lbs[2] : null;
                Tr(lbN != null, "C2 **前提**：Normal 行的 `Message` 是 `Label`");
                Tr(lbT != null, "C2b **前提**：Translating 行的 `Message` 是 `Label`");
                Tr(lbD != null, "C2c **前提**：Translated 行的 `Message` 是 `Label`");

                string tip = Loc.T(ChatMessageRow.TranslatingTipTerm);

                if (lbN != null)
                {
                    C(lbN.Text, "Hello world", "C3 Normal 行正文 = 原文");
                    C(lbN.FontMaterialName, NameNormal,
                      "C4 ★ Normal 行**仍是**字体资产自带那份材质（灭自证：三态一起换成新材质也会「两两不等」，"
                    + "所以这里比的是**名字等于谁**）");
                }
                if (lbT != null)
                {
                    C(lbT.Text, "Hello world  " + " (" + tip + ")",
                      "C6 ★ Translating 行正文 = 原文（削 `\\n`、留空格）+ ` (词条)`");
                    C(lbT.FontMaterialName, NameGreen, "C7 ★ Translating 行材质 = `Pragati-Regular Light Green`"
                    + "｜🧨 正文含汉字时若整颗翻 CJK 面（= 原实现的 `if (ActualFace != LatinCardBody) return;`）⇒ 红");
                    Tr(lbT.CjkFallbackFace && lbT.ActualFace == TmpFont.Face.LatinCardBody,
                       "C7c ★ 正文带汉字也**不整颗翻面**（`CjkFallbackFace = true` ⇒ `ActualFace = LatinCardBody`）"
                     + "｜🧨 判据 `判据/B109_D1_八条红.md` §3 修法 A：原版从不整颗翻面、汉字走 TMP fallback");
                    var mT = lbT.FontMaterialNow;
                    Tr(mT != null, "C7b **前提**：Translating 行读得到 `fontMaterial`");
                    if (mT != null)
                        Near(mT.GetColor("_FaceColor").r, FaceGreen.r,
                             "C8 屏上那份材质 `_FaceColor.r` = 0.8820755（期望值写**解包原版字面量**，⛔ 不回读）");
                }
                if (lbD != null)
                {
                    C(lbD.Text, "Bonjour le monde (FR)",
                      "C9 Translated 行正文 = `TranslatedText`（非空就用它；原版 `translatedMessage ?? msg`）");
                    C(lbD.FontMaterialName, NameCream, "C9b ★ Translated 行材质 = `Pragati-Regular Light Cream`");
                }

                if (lbN != null && lbT != null && lbD != null)
                {
                    string n1 = lbN.FontMaterialName, n2 = lbT.FontMaterialName, n3 = lbD.FontMaterialName;
                    Tr(n1 != n2 && n2 != n3 && n1 != n3,
                       "C10 灭自证：三态材质名**两两不同**（" + n1 + " / " + n2 + " / " + n3 + "）");
                }

                // 🔴 `SetFontMaterial` 必须克隆：写队列**不能**落回工程资产
                var greenAsset = Resources.Load<Material>("Fonts/Pragati-Regular Light Green");
                if (lbT != null && greenAsset != null)
                {
                    int before = greenAsset.renderQueue;
                    lbT.SetRenderQueue(3141);
                    C(greenAsset.renderQueue, before,
                      "C11 ★ 换材质之后 `SetRenderQueue` **不写回工程资产**（= `SetFontMaterial` 里那份 `new Material(m)` 在起作用）");
                }
                else Tr(false, "C11 **前提不成立**：缺 Translating 标签或绿材质资产（本节这条没验到，⛔ 不是绿）");
            }
        }
        catch (System.Exception ex)
        {
            Tr(false, "（不静默）夹具抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            SocialData.ChatMessages.Clear();
            SocialData.ChatMessages.AddRange(saved);
            if (chat != null) UnityEngine.Object.DestroyImmediate(chat.gameObject);
        }
    }
}
