// CollectionScene.cs — 收藏线（`Collection Menu Variant`）的**场景 / 自检 / 截图**
//
// 用法：`Unity -batchmode -quit -executeMethod CollectionScene.Run`
// 判据全部来自 **原版参数**（`资料/阶段二_卡组线_原版规格.md` + `资料/普查产出_0923/A1~A4`），
// **不是我们自己的常量**（否则就是自证 —— 见 CLAUDE.md §二 那条）。
//
// ⚠️ 本文件的辅助函数（`Check*/FindChild/Shoot`）是**照 `ShopScene` / `RewardsScene` 又抄了一份**
//    —— 那三份各自有一整套，**这是一笔明账**（该收口成 `Editor/MenuCheck.cs`）。已记在
//    `项目任务.md` §三 第 15 条；**本轮先不动那三个绿着的文件**。
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RuleEngine;
using CardPresentation;     // ⚠️ 类本身**留在全局命名空间** —— 见下面那条注释

// 🔴 **`-executeMethod` 按类名找，带命名空间就找不到**（2026-09-23 踩：写成 `namespace CardPresentation`
//    之后跑出「executeMethod class 'CollectionScene' could not be found」，三次都是这一条）。
//    其余三个自检（`RewardsScene`/`ShopScene`/`DeckScene`）也都在**全局命名空间** ⇒ 照它们来。
public static class CollectionScene
{
        const string P = "[Collection] ";
        const string ShotDir = "d:/4/_tmp_view/collection";
        const string TestDeckFile = "d:/4/_tmp_view/collection/_test_decks.json";

        /// <summary>🆕 2026-10-18（第四会话）：断言计数器 + 输出口径**收口到共用件 `Editor/MenuCheck.cs`**
        /// （唯一实现处；本文件只剩同名的一行转发 ⇒ 5,534 个调用点一个字没动）。
        /// 🔴 **逐宿主一份 `CheckSink`**（⛔ 不是全局 static）—— 「拿别处的 `Check` 去断，失败会
        /// **记进别人的合计**里 ⇒ 静默」，判据见 `Editor/RewardWindowFixture.cs`。</summary>
        static readonly CheckSink _sink = new CheckSink(P)
        {
            Near = MenuNearStyle.Compact2,
            BlankNote = MenuBlankNote.Plain,     // 本文件原来那句「（按已知情况放行）」
            StarNotBlank = true,                 // 本文件原来那句「**不是空图**」带 `**`
        };

        // 🆕 **2026-10-17（界面线 T18/T25）**：自检**独立读数据**用的两份 DTO ——
        // 期望值从 `Resources/Cardbacks.json` / `Resources/official_names.json` **原样**算，
        // ⛔ 不经过 `CardbackTable` / `ItemDisplayWindow` 的取数口（那会变成自证）。
        [System.Serializable] sealed class CbRawItem { public string name, so, army, uniqueId; }
        [System.Serializable] sealed class CbRawFile { public CbRawItem[] items; }
        [System.Serializable] sealed class OfficialRow { public string k, en, zh; }
        [System.Serializable] sealed class OfficialFile { public OfficialRow[] entries; }

        static void Section(string t) => MenuCheck.Section(_sink, t);
        static void Check<T>(T got, T want, string msg) => MenuCheck.Check(_sink, got, want, msg);
        static void CheckTrue(bool c, string msg) => MenuCheck.True(_sink, c, msg);

        /// <summary>🆕 **2026-10-04（A47 接线批）**：压暗层（「点窗外关窗」）命中区那条不变量。
        /// 🔴 **2026-10-07（A77⑬⑥）本文件里的副本已删** —— 唯一一份在 `MenuDraw.CheckShadeRule`。
        /// ⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
        /// <para>🔴 **2026-10-07（A77⑬③）那条判据的期望值也换了**：不再比「调用方传进来的常量」
        /// （与 `ShadeHit` 的实参同一个符号 = 同义反复），改成**量同一扇窗里「视觉压暗层」那颗 quad 的
        /// `RenderQueue`**。🔴 **为什么仍要问 `WasShadeHit`**：档本来就对的那几扇窗，走不走公共件
        /// **没有任何可见行为差异** ⇒ 只有那一句能分出两种状态（改回自己那份 `MenuDraw.Hit` 就红）。</para></summary>

        /// <summary>🆕 **2026-10-15（A825）：本文件原来那一份 `CheckAbsorbRule` 已【收口】——
        /// 唯一一份实现在 `MenuDraw.CheckAbsorbRule`。**</summary>
        /// <para>**签名与 26 个调用点一个字都没动**（本包装的形参表与原来那份逐字相同）；
        /// 「点哪儿 / 为什么钉死 (5,5) / 六步各查什么」的判据全文 → `Shell/MenuDraw.cs` 的 `CheckAbsorbRule`
        /// （⛔ 别在本文件里再抄第二份）。</para>
        /// <para>本文件原来那份里读过的 `EdgeInset` 常量表随函数一起搬进 `MenuDraw.AbsorbEdgeInset`
        /// （本文件那一份**只被这一处读**，2026-10-15 实读）。理由 = 「**两处写同一条规则 = 迟早不一致**」
        /// （`CLAUDE.md` §三）—— 与 `CheckShadeRule`（2026-10-07 · A77⑬⑥）同族。</para>
        static void CheckAbsorbRule(string what, Transform winRoot, string nodeName,
                                    float x1, float y1, float x2, float y2,
                                    int qShade, int qContentMin, System.Func<WindowState> state)
        {
            MenuDraw.CheckAbsorbRule(CheckTrue, CheckNear, what, winRoot, nodeName,
                                     x1, y1, x2, y2, qShade, qContentMin, state);
        }

        /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
        /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
        static void CheckHoverSwap(Transform root, string what) => MenuCheck.HoverSwap(_sink, root, what);

        static void CheckNoMissingSwapArt(string what) => MenuCheck.NoMissingSwapArt(_sink, what);
        static void CheckNear(float got, float want, float tol, string msg)
            => MenuCheck.Near(_sink, got, want, tol, msg);
        static void CheckText(string got, string want, string msg)
            => CheckTrue(got == want, $"{msg} —— 实测「{got}」，期望「{want}」");

        /// <summary>世界坐标比对（±0.01 世界单位 ≈ ±1px）。期望值必须来自原版像素矩形。</summary>
        static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
        {
            if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
            var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
            float d = Vector3.Distance(t.position, want);
            CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d * 108f:F2}px）");
        }

        /// <summary>🆕 2026-10-11（F6）：**像素空间 2D** 版的 `CheckAt` —— ⛔ **z 不参与比较**。
        /// <para>为什么单开一条（而不是改 `CheckAt`）：`CheckAt` 比的是 `Vector3.Distance(节点, 期望)`，
        /// **含 z**、容差 **0.01 世界单位 = 1.08px**；而命中区节点可以被**有意**前移 z ——
        /// `Shell/DeckInfoPopup.cs` 的 `HitZFront` 就是那把「同队列（`QDIHit` 3129）同 z 打平 ⇒
        /// 显式排出谁深谁浅」的尺子（原版靠 `m_Children` 的兄弟序定胜负，我们这侧只能靠 z）。
        /// **那不是「节点没摆对」**，却**一个人就能吃满 `CheckAt` 的容差** ⇒ 那种红是**口径错**，不是缺陷。</para>
        /// <para>口径：容差 **±1.5px**、**逐轴**比（与四条边那组 `CheckNear` 同一个口径；换算尺子仍只此一份
        /// —— 转调 `MenuCheck.DesignPx`（设计帧 / 乙式；🆕 2026-10-20 `A1350` 第二批换口））。判别力照旧：抓的是「节点停在容器 `(0,0)`」这类
        /// **几百 px** 的错（2026-09-23 那个「节点全停在容器原点、只有里面的 quad 画对了」的坑）。</para></summary>
        static void CheckAtPx(Transform t, float cx, float cy, float tol, string what)
        {
            if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
            var p = MenuCheck.DesignPx(t);
            float dx = p.x - cx, dy = p.y - cy;
            CheckTrue(Mathf.Abs(dx) <= tol && Mathf.Abs(dy) <= tol,
                      $"{what}（实测中心 ({p.x:F2},{p.y:F2})，期望 ({cx:F2},{cy:F2})±{tol:F2}px，"
                      + $"差 ({dx:F2},{dy:F2})；**z 不参与** —— 那颗节点的 z 是按 `HitZFront` 有意前移的）");
        }

        static void CheckRectPx(Transform t, float x1, float x2, float y1, float y2, string what)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
            CheckNear(q.WorldW * 108f, x2 - x1, 2.0f, what + " 宽(px)");
            CheckNear(q.WorldH * 108f, y2 - y1, 2.0f, what + " 高(px)");
        }

        static void CheckArt(Transform t, string want, string what)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            var nm = q != null && q.Texture != null ? q.Texture.name : null;
            CheckText(nm, want, what);
        }

        /// <summary>🆕 2026-10-05（A32③）读一棵子树里那个 `ImageQuad` 的 tint 并比对。
        /// ⚠️ 期望值写的是**原版 prefab 里 `offColor` 的字面量**（不是我们代码里的常量 —— 否则就是自证）；
        /// 容差 **2/255**（颜色是从 float 字面量来的，四舍五入到 1 位足够）。
        /// 取不到那个 quad 时返回 `(-1,-1,-1,-1)` ⇒ **必红**（不会静默放过）。</summary>
        static void CheckTint(Transform t, Color want, string what)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            var got = q != null ? q.Tint : new Color(-1f, -1f, -1f, -1f);
            bool ok = Mathf.Abs(got.r - want.r) < 2f / 255f && Mathf.Abs(got.g - want.g) < 2f / 255f
                      && Mathf.Abs(got.b - want.b) < 2f / 255f && Mathf.Abs(got.a - want.a) < 2f / 255f;
            CheckTrue(ok, $"{what} —— 实测 ({got.r:F3},{got.g:F3},{got.b:F3},{got.a:F3})，"
                          + $"期望 ({want.r:F3},{want.g:F3},{want.b:F3},{want.a:F3})");
        }

        /// <summary>🆕 2026-10-18（第四会话）：收口到 `MenuCheck.FindChild`（5 份逐字相同的那一份）。
        /// ⚠️ **它不认识 `A/B/C` 这种路径写法** —— 要路径用本文件自己的 `FindPath`。</summary>
        static Transform FindChild(Transform parent, string name) => MenuCheck.FindChild(parent, name);

        /// <summary>🆕 **2026-10-24（A6）**：点一扇 `PopUpGameWindow` 上的**真钮**（按节点名找，⛔ 不拿
        /// 「子树里第一颗 `WindowButton`」—— 那会先撞上 `MenuDraw.Absorb` 建的压暗层吸收件，点了等于没点）。
        /// <para>判据同 `Editor/ShellScene.cs` 那个同名助手：节点 → `WindowButton` → `ClickForTest()`；
        /// 取不到就红（取不到却往下走 = 静默变绿）。</para></summary>
        static void ClickPopUpButton(GameWindow popup, string btnName)
        {
            var node = popup != null ? FindChild(popup.transform, btnName) : null;
            var wb = node != null ? node.GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(wb != null, $"（点钮）`{btnName}` 上挂着 `WindowButton`（拿不到 ⇒ 这扇窗的钮点不动）");
            if (wb != null) wb.ClickForTest();
        }

        /// <summary>🆕 **2026-10-25（K3）**：点 `DeckInfoPopup` 的 `Delete`，并在弹出来的**确认窗**上点**左钮**。
        /// <para>原版 `DeckInfoPopup__DeleteDeck.c` 逐句：`ShowPopUp(正文 = <c>MenuDeck/HUD/DeleteDeckCheck</c>,
        /// 左钮 <c>Demo/FriendsMenu/DeleteFriendButton</c> = **真删**（<c>g__Delete|27_0</c>）,
        /// 右钮 <c>MainMenu/General/Cancel</c> = 只关窗) </para>
        /// <para>三个键都是地址表实读（`DAT_1842cffe8` / `DAT_184276d78` / `DAT_1842be120`）。
        /// ⛔ 别把这一下写成「点完就当删了」—— 那就等于把本批补上的确认窗又验成不存在。</para></summary>
        static void ClickDeleteAndConfirm(DeckInfoPopup pop, System.Action<Transform> click)
        {
            CheckTrue(pop != null, "（前提）`DeckInfoPopup` 拿得到 —— 拿不到 ⇒ 这一下无可点（不静默）");
            if (pop == null) return;
            click(pop.Opt("Delete"));
            var conf = pop.DeleteConfirmPopup;
            CheckTrue(conf != null && conf.gameObject.activeSelf,
                      "★ 点 `Delete` ⇒ **先弹确认窗、还没删**（原版 `DeckInfoPopup__DeleteDeck.c` 的 `ShowPopUp`）"
                    + " —— 改坏法：退回「点了就删」⇒ 本条红");
            if (conf == null) return;
            Check(conf.MessageKey, DeckInfoPopup.KeyDeleteConfirmText,
                  "★ 确认窗正文键 = `MenuDeck/HUD/DeleteDeckCheck`（`DAT_1842cffe8` → `0x42CFFE8` 地址表实读）");
            Check(conf.PrimaryKey, DeckInfoPopup.KeyDeleteConfirmOk,
                  "★ 左钮键 = `Demo/FriendsMenu/DeleteFriendButton`（`DAT_184276d78`；⚠️ 原版**自己复用了删好友那颗键**）");
            Check(conf.SecondaryKey, DeckInfoPopup.KeyDeleteConfirmCancel,
                  "★ 右钮键 = `MainMenu/General/Cancel`（`DAT_1842be120`）");
            // 用调用点那一份 `click` 点（= 本文件那段局部 `ClickHit`：**同时抓住这一下的日志**）
            var node = FindChild(conf.transform, "ButtonLeft");
            CheckTrue(node != null, "（前提）确认窗左钮节点 `ButtonLeft` 拿得到（拿不到 ⇒ 点不动、等于没删）");
            if (node != null) click(node);
        }

        /// <summary>🆕 **2026-10-09（`A1125`）**：一颗**关窗钮**的「命中区 + 换图层」四连断。
        /// <para>四个闸落在**四个不同对象**上（前提 / 命中区尺寸 / 命中区位置 / 换图层绑定）⇒ 改坏任一处只红其中一条：
        /// ① 按钮 / 命中 / 可见面三件都取得到（取不到就不许往下断 = 不静默变绿）；
        /// ② 命中区**尺寸** = 原版可射线件**并集**（⛔ 不是根矩形）；③ 命中区**中心** == 可见面渲染中心
        /// （可见面矩形与命中区**不同源** ⇒ 位置那一半的独立锚）；④ 换图层 = 原版 `m_TargetGraphic`
        /// 指到的那一颗（节点名 + 贴图名**两个条件**；`targetNodeName == null` ⇒ 只断**贴图名**）。</para>
        /// <para>`btnName == null` ⇒ 直接在**窗根**下找 `hitName` / `faceName`（命中节点挂窗根那两扇）。
        /// ⛔ 期望值全是**原版 prefab 的读数**，不从被测实现里读。判据 → 同族五份 `A1125Close`。</para></summary>
        static void A1125Close(string win, Transform winRoot, string btnName, string hitName,
                               string faceName, string faceTex, string targetNodeName, float wPx, float hPx)
        {
            var btn = winRoot == null ? null : (btnName == null ? winRoot : FindChild(winRoot, btnName));
            CheckTrue(btn != null, $"（前提·不静默）A1125 {win}：关窗钮节点 `{btnName ?? "<窗根>"}` 拿得到"
                                 + " —— ⛔ 取不到就不往下断（不静默变绿）");
            var hitG = btn != null ? FindChild(btn, hitName) : null;
            CheckTrue(hitG != null, $"（前提·不静默）A1125 {win}：命中节点 `{hitName}` 拿得到");
            var faceG = btn != null ? FindChild(btn, faceName) : null;
            CheckTrue(faceG != null, $"（前提·不静默）A1125 {win}：可见面子件 `{faceName}` 拿得到");
            var hq = hitG != null ? hitG.GetComponentInChildren<ImageQuad>(true) : null;
            float hx1 = 0f, hy1 = 0f, hx2 = 0f, hy2 = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
            bool okH = hq != null && MenuDraw.QuadRectPx(hq, out hx1, out hy1, out hx2, out hy2);
            CheckTrue(okH && Mathf.Abs((hx2 - hx1) - wPx) <= 0.5f && Mathf.Abs((hy2 - hy1) - hPx) <= 0.5f,
                      $"★★ A1125 {win}：关窗钮**命中区** = 原版可射线件并集 **{wPx}×{hPx}**（设计 px）"
                    + (okH ? $"（现读 {hx2 - hx1:F2}×{hy2 - hy1:F2}）" : "（命中 quad 取不到）")
                    + "｜🧨 ① 改回根矩形 ⇒ 每边小 ~11（绿族 ~10.7/9.8）⇒ 红；② 改回子件裸矩形 ⇒ 每边小 20 ⇒ 红");
            var fq = faceG != null ? faceG.GetComponentInChildren<ImageQuad>(true) : null;
            float fx1 = 0f, fy1 = 0f, fx2 = 0f, fy2 = 0f;
            bool okF = fq != null && MenuDraw.QuadRectPx(fq, out fx1, out fy1, out fx2, out fy2);
            CheckTrue(okH && okF
                      && Mathf.Abs((hx1 + hx2) * 0.5f - (fx1 + fx2) * 0.5f) <= 0.6f
                      && Mathf.Abs((hy1 + hy2) * 0.5f - (fy1 + fy2) * 0.5f) <= 0.6f,
                      $"★★ A1125 {win}：命中区**中心** == 可见面 `{faceName}` 的渲染中心（独立锚）"
                    + (okH && okF ? $"（{((hx1 + hx2) * 0.5f):F2},{((hy1 + hy2) * 0.5f):F2} vs "
                                    + $"{((fx1 + fx2) * 0.5f):F2},{((fy1 + fy2) * 0.5f):F2}）" : "（取不到）")
                    + "｜🧨 把命中节点整体搬走（`localPosition += (20,0,0)`）⇒ 只错位置不错尺寸 ⇒ 只这条红（与 ② 不同源）");
            var wb = hitG != null ? hitG.GetComponent<WindowButton>() : null;
            string got = wb == null || wb.target == null || wb.target.Texture == null
                       ? "<没绑>" : wb.target.gameObject.name + " / " + wb.target.Texture.name;
            CheckTrue(wb != null && wb.target != null && wb.target.Texture != null
                      && wb.target.Texture.name == faceTex
                      && (targetNodeName == null || wb.target.gameObject.name == targetNodeName),
                      $"★★ A1125 {win}：**换图层** = 子件 `{targetNodeName ?? faceName}`（图 `{faceTex}`；"
                    + "= 原版 `m_TargetGraphic` 指到的那一颗）—— 现读「" + got + "」"
                    + "｜🧨 把它传成根圆底盘（`UI_Button_Round_background`）⇒ 红（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）");
        }

        /// <summary>🆕 **2026-10-18（`A1149` 第二半 · 第九会话 P6）**：关窗钮**圆底盘**那颗 quad 的
        /// **实绘矩形**两连断 —— 补 `A1125Close` 上面那个**静默缺口**（那四格读的是：命中区 quad 的矩形 /
        /// 命中区中心 vs 见面中心 / 可见面的**节点名** / 可见面的**贴图名** —— **没有一格量圆底盘的矩形成像**；
        /// 圆底盘等比后**仍居中** ⇒ 连「中心」那一格也抓不住它）。
        /// <para>⛔ **期望值全部来自原版 prefab 的逐字段直读**（⛔ 不是我们现在的实绘值、更不是从被测实现里读回来）：
        /// 圆底盘那几颗一律 `m_Type=0`(Simple) · **`m_PreserveAspect=1`** · `m_PixelsPerUnitMultiplier=1.0`；
        /// 贴图 `UI_Button_Round_background` 的 sprite `m_Rect` = **237×237 正方**、`m_Border` / `m_Offset` 全 0
        /// ⇒ 等比内接进「原版根那一格」= **实绘 min(框宽,框高) 见方**（橙族框 74.39×75.61 ⇒ **74.39×74.39**；
        /// 绿族框 75×75 ⇒ **75×75**）。逐窗读数 → `资料/普查产出_第八会话/B4_InboxWindow圆底盘归真.md` §1·2/§1·3
        /// （本件 P6 用 `d:/tmp/wf_b4probe/pa.py` 逐窗复跑核过）。</para>
        /// <para>`frameW` / `frameH` = **原版根那一格**的框（只进消息）；`basePx` = 期望边长。</para>
        /// <para>🔴 **两格各钉一轴**（① 实绘**宽** == `basePx`；② 实绘**宽 == 实绘高** = 等比不变量）⇒
        /// 两种改坏法**各红不同的一格**、**结构上不可能一起变绿**：把矩形改回**子件框** ⇒ 只 ① 红（那档也近正方 ⇒ ② 绿）；
        /// 去掉 `keepAspect` ⇒ 只 ② 红（宽没变 ⇒ ① 绿）。⛔ 别把高度也塞进 ①（否则 ① 两种改坏法都红、② 失去独立作用）。</para>
        /// <para>🆕 **取法 = 「先根后子」**：① 先取**根节点自己**身上那一颗（= 原版结构：原版那颗 `Image` 就长在根节点
        /// `Generic Close Button Orange` / `Generic Close Button Green` 上、**没有独立子件名**）；
        /// ② 根上没有、且调用点**显式给了**子件名 `baseChild` 时才退一步取 `btn/&lt;baseChild&gt;` 那一颗
        /// —— 那是 `A1149` 第一半现读出来的**已知偏离**（圆底盘被画在自造子件上），消息里点名。
        /// `baseChild == null` ⇒ **不许退**。⚠️ 用意：把圆底盘**归真到根节点上不会让本格变红**；
        /// ⛔ 两处都**不下钻**（不用 `GetComponentInChildren` —— 盲扫子树会把「搬到别的层」静默量成合格）。</para>
        /// <para>⚠️ 原版根那一格本身是**正方**的窗（绿族 75×75）第二格**恒真** —— 那种窗本来就没有「等比不变量」可丢，
        /// **不是缺口**（见各调用点注释）。</para></summary>
        static void A1125CloseBase(string win, Transform winRoot, string btnName, string baseTex,
                                   float basePx, float frameW, float frameH, string baseChild = null)
        {
            var btn = winRoot == null ? null : (btnName == null ? winRoot : FindChild(winRoot, btnName));
            // 🔴 `GetComponent`（**本节点自己**那一颗）而不是 `GetComponentInChildren` —— 见上面 doc：
            //    `GetComponentInChildren` 会往下钻，把「圆底盘搬到别的层」那种偏离量成合格（静默）。
            var bq = btn != null ? btn.GetComponent<ImageQuad>() : null;
            bool baseOnRoot = bq != null && bq.Texture != null && bq.Texture.name == baseTex;
            if (!baseOnRoot && btn != null && baseChild != null)
            {
                // ⚠️ **只有调用点显式点名子件时才退这一步**（= `A1149` 第一半现读的已知偏离，消息里点名）
                var baseHost = FindChild(btn, baseChild);
                bq = baseHost != null ? baseHost.GetComponent<ImageQuad>() : null;
            }
            string baseWhere = baseOnRoot ? "根节点自己身上（= 原版结构）"
                             : (baseChild != null
                                ? "子件 `" + baseChild + "`（⚠️ **已知偏离**：原版长在根节点自己身上 —— `A1149` 第一半）"
                                : "根节点自己身上（⚠️ 那颗 quad 取不到）");
            string baseGot = bq == null ? "<没有 quad>" : (bq.Texture == null ? "<没贴图>" : bq.Texture.name);
            CheckTrue(bq != null && bq.Texture != null && bq.Texture.name == baseTex,
                      $"（前提·不静默）A1125 {win}：圆底盘 `{baseTex}` 那颗 quad 拿得到、且贴图就是原版那一张"
                    + " —— ⛔ 取不到 / 取错就不往下断（不静默变绿）"
                    + $"｜现读「{baseGot}」｜取处 = {baseWhere}"
                    + "｜🧨 把圆底盘整颗删掉 / 换成别张图 ⇒ 本格红（⛔ 别改成盲扫子树去「修」它）");
            float bx1 = 0f, by1 = 0f, bx2 = 0f, by2 = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
            bool okB = bq != null && MenuDraw.QuadRectPx(bq, out bx1, out by1, out bx2, out by2);
            float bw = bx2 - bx1, bh = by2 - by1;
            // 🔴 **本格【只钉宽】**（高由下面那格「宽==高」钉）—— 两格各钉一轴，**改坏法才各红一格**。
            CheckTrue(okB && Mathf.Abs(bw - basePx) <= 0.5f,
                      $"★★ A1125 {win}：**圆底盘实绘宽** = 原版 **{basePx:F2}**（设计 px；配下面那格「宽==高」"
                    + $"⇒ 两条一起 = 原版 **{basePx:F2}×{basePx:F2}**）"
                    + (okB ? $"（现读宽 {bw:F2}，高 {bh:F2}）" : "（圆底盘 quad 取不到）")
                    + $"｜= 原版根那一格 **{frameW:F2}×{frameH:F2}** 的宽（正方贴图 + `m_PreserveAspect=1` 的等比内接）"
                    + "｜🧨 把矩形改回**子件框**（橙族 56.86×58.13 / 绿族 56.37×54.50）⇒ 宽少 ~17 ⇒ **只本格红**"
                    + "（那两档也近正方 ⇒ 下一格**仍绿**）");
            // 🔴 **本格 = 等比这条不变量**（比断绝对数抗「将来换贴图」：换成别的正方贴图它照旧成立）。
            CheckTrue(okB && Mathf.Abs(bw - bh) <= 0.5f,
                      $"★★ A1125 {win}：圆底盘**实绘宽 == 实绘高**（= **等比**这条不变量；原版根那一格本身是 "
                    + $"{frameW:F2}×{frameH:F2} 的框 ⇒ 只有真等比才两轴相等）"
                    + (okB ? $"（现读 {bw:F2}×{bh:F2}）" : "（圆底盘 quad 取不到）")
                    + $"｜🧨 把 `keepAspect: true` 去掉 ⇒ 实绘变框那一格 {frameW:F2}×{frameH:F2}"
                    + (Mathf.Abs(frameW - frameH) > 0.5f ? "（**宽≠高**）⇒ **只本格红**（宽没变 ⇒ 上一格绿）"
                                                         : "（= 同值）⇒ 本窗**框本身正方**，去掉 `keepAspect` 本就无差别")
                    + "｜🔴 这就是「它用的是**根那一格** + 等比」的判别式 —— 少了等比，高度就顶到框高");
        }

        /// <summary>一格卡的渲染队列（取卡内所有层里**最小的那个** —— 卡内层序靠 z 偏移、整格一起平移，
        /// 所以最小号就代表这一格；见 `CardFan.SetCardQueue`）。
        /// ⚠️ 读 `sharedMaterial`：`SetCardQueue` 走 `.material`（会把实例写回 `sharedMaterial`），
        /// 两边读到的是同一份，且**不会再实例化一次**。</summary>
        static int CardQueue(CardView v)
        {
            if (v == null) return -1;
            int q = int.MaxValue;
            foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.sharedMaterial != null) q = Mathf.Min(q, mr.sharedMaterial.renderQueue);
            return q == int.MaxValue ? -1 : q;
        }

        /// <summary>Deck 页那条 `Empty Collection Warning` 现在亮着没有。
        /// ⚠️ **必须限定在 Deck 页里找** —— 四个页各有一份**同名**节点（原版如此，矩形逐页不同）。</summary>
        static bool DeckEmptyShown(CollectionWindow win)
        {
            var ew = FindChild(win.PageRoot(0), "Empty Collection Warning");
            return ew != null && ew.gameObject.activeSelf;
        }

        // 🆕 **2026-10-20（`A1350` 第二批）**：本文件原来那两个甲式点读定义
        // `static float PxOf(float worldX) => worldX * 108f + 960f;` /
        // `static float PxYOf(float worldY) => 540f - worldY * 108f;` **已删除**（V3 结构闸：真定义 0 处）。
        // 全部调用点换口：与设计 px 期望值比的走 `MenuCheck.DesignPx(t)`（设计帧 / 乙式）；
        // 喂 `PointerLayer` 的走 `MenuCheck.PointerFramePx(t)`（指针帧）。判据 →
        // `资料/普查产出_1019/W57_自检线_A1350读口统一计划.md` §1·2 / §2·2。

        /// <summary>🆕 **2026-10-13（A750）**：量一段文字 **TMP 自己渲出来那块**的像素矩形
        /// （1920×1080 · 左上原点 · y 向下）—— **这一份才是「字真的从哪开始画」**。
        /// <para>写法与契约**照抄** `Editor/MainMenuScene.cs` 的同名助手（`TmpRenderedRect`，A490/A617 收口的那一份）；
        /// 本文件再留一份，是因为四个自检各自一套辅助函数（本文件开头那条注释已经明记这是**一笔明账**、
        /// 该收口成 `Editor/MenuCheck.cs`）。</para>
        /// <para>🔴 **为什么要用它（灭自证）**：`Label.WorldW/WorldH` 读的是**字段缓存** `_tmpW/_tmpH`，
        /// 而那份缓存**只有 `RefreshBounds()` 写**（`Battle/Label.cs`）—— 也就是**被测实现自己**；
        /// 反过来 `SetCharSpacing`（`CardPresentation/Battle/Label.cs`）· `SetFontSize`（`CardPresentation/Battle/Label.cs`）这一族**只重排 mesh、不刷新缓存**
        /// ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**（实现与检测器共用一个口 = 自证）。
        /// 本助手读的是 TMP 自己的 `textBounds`（mesh 的**活值**），**不在实现那条链上**。
        /// ⚠️ 与本文件既有的 `TextExtentPx`（按**字形顶点**量）**不是同一件事**：那一份不含字距/前进宽，
        /// 而本助手量的 `textBounds` 与 `_tmpW` **同源**（`RefreshBounds` 读的就是它）⇒ 换口当天两条量法**逐位同值**。</para>
        /// <para>⚠️ **取组件带 `true`（含 inactive）**：单参那版只找**激活**的对象，会把「出厂关着」的件
        /// 误报成「这一段字不在」（同 `CheckWrapMode` 那条教训）。
        /// ⚠️ **量不到时（`t` 不在 / 底下没有 TMP）返回 `false`、四个 out 全 0**（与 `RectOf` 同一契约）
        /// ⇒ 调用方**必须先判它**，否则 `0 ≤ 期望值` 会**假绿**。
        /// ⚠️ TMP 在、但字是**空串**时 `textBounds` 是 TMP 的未定义值（哨兵 **4.29e9**）⇒ 那时报出来的是
        /// **天文数字 = 量法没生效**，⛔ 别照它去改实现。</para></summary>
        static bool TmpRenderedRect(Transform t, out float x1, out float y1, out float x2, out float y2)
        {
            // 🆕 **2026-10-20（`A1350` 第一批）**：实现已收口到 `SelfCheckTextMetrics.RenderedRect`
            // （`A1265` 的法定唯一实现处；本轮机核四份 `TmpRenderedRect` 归一化后逐字相同 ⇒ 纯搬运）。
            // 本函数**保留原名与本形参 ⇒ 调用点 0 改动**；行为逐字不变（世界帧四角 → `PxX/PxY` 的 min/max）。
            return SelfCheckTextMetrics.RenderedRect(t, out x1, out y1, out x2, out y2);
        }

        /// <summary>🆕 **2026-10-16（A796′ 换口）**：一个 `Label` **渲出来**的像素宽高（画布 px · `Vector2(w, h)`）。
        /// 🔴 **走哪条口**：`TmpRenderedRect`（= TMP 自己渲出来那块 `textBounds`）——
        ///   ⛔ **不是** `Label.WorldW/H`（那是**字段缓存** `_tmpW/_tmpH`，只有 `RefreshBounds()` 写
        ///   —— 也就是**被测实现自己**；`SetFontSize` / `SetCharSpacing` 这一族**只重排 mesh、不刷缓存**
        ///   ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**）；
        ///   ⛔ **也不是**「TMP 网格顶点」那条口（实测首字左边距：`Current Streak` 43.00 vs 45.22 = **2.22px**
        ///   ⇒ 换错口会让一批期望值**集体偏 1–2px**）。判据 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3。
        /// <para>⚠️ **两条量法同源**（`RefreshBounds` 读的就是 `textBounds`）⇒ **换口当天逐位同值**，
        /// 期望值与容差**一个都不用动**；变的只是「以后重排看得见」。</para>
        /// <para>⚠️ 量不到（`lb == null` / **点阵后端** / 底下没有 TMP）⇒ **退回旧口** `Label.WorldW/H`：
        /// 点阵那条路**本来就没有** `textBounds`（`WorldW` 读的是 `_texW`）⇒ 那是**同一条口的老行为**，
        /// ⛔ 不是「静默吞掉新口」。⚠️ TMP 在、但字是**空串**时两条口读的是**同一个** `textBounds`
        /// 哨兵（4.29e9）⇒ 这一档与换口无关，照旧由调用方的上下界守卫挡。</para>
        /// <para>⚠️ **它只给宽高**：调用方拿它算边缘时，**中心仍是节点位置**（`AlignLeft/Right` 把节点挪走那一档
        /// 一个字没动）。若改成直接用 `textBounds` 那块矩形，矩形会跟着**两项**整体挪 —— TMP 子节点上的
        /// `_vOffset`（A712 字墨校正，`Battle/Label.cs` 的 `RefreshBounds` 里那句）与 `(0.5 − anchor)` 那一项
        /// —— 两者今天**都不保证为 0** ⇒ 期望值会集体漂（那就不是「换口」了）。</para></summary>
        static Vector2 LabelRenderedPx(Label lb)
        {
            // 🆕 **2026-10-20（`A1350` 第一批）**：实现转调 `SelfCheckTextMetrics.RenderedPx`
            // （`A1265` 的法定唯一实现；本函数原来的 `TmpRenderedRect(lb.transform, …)` + 量不到退回
            // `Label.WorldW/H × 108` 两条支与它逐字相同）⇒ 调用点 0 改动、行为逐字不变。
            return SelfCheckTextMetrics.RenderedPx(lb);
        }

        /// <summary>🆕 2026-10-08（A181）：一个节点子树里**所有启用中的 `MeshRenderer`** 的网格顶点，
        /// 在画布像素里的范围（左上原点 · y 向下）。返回 false = 一个顶点都没量到。
        /// 🔴 **为什么要它**：视口裁切那件事**量节点位置量不出来** —— `CardView` 的自建网格是按局部系摆的，
        /// 节点全对而**画出来的层越界**（这正是 A181 当初漏掉的那一条）。只有逐顶点换算才看得见。
        /// ⚠️ 跳过 `ImageQuad`（它自己会 `RebuildMesh`，是战斗里光环那一族；要量它请用
        /// `RectOf` / `WorldW/H`，如卡背页那条断言）。
        /// 🆕 2026-10-11（F3）：**TMP 那份网格只量字形顶点**（`GlyphVertsOf`）——
        /// 它那份数组里夹着零面积的**非字形占位槽**，全扫会量出假的范围。</summary>
        static bool RenderExtentPx(Transform t, out float x1, out float y1, out float x2, out float y2, out int verts)
        {
            x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue; verts = 0;
            if (t == null) return false;
            foreach (var mr in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null || !mr.enabled || mr.GetComponent<ImageQuad>() != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var vs = mf.sharedMesh.vertices;
                if (vs == null) continue;
                var tmp = mr.GetComponent<TMPro.TextMeshPro>();
                List<int> gi = null;
                if (tmp != null)                      // TMP 自家那份网格 ⇒ 按字取四角（见 `GlyphVertsOf`）
                {
                    gi = GlyphVertsOf(tmp, vs.Length);
                    if (gi.Count == 0) continue;      // 这一段没有字形 ⇒ 一个点都不量（别把占位槽当几何）
                }
                // 🔴 **2026-10-20（`A989②`）：这一处【不受】`materialReferenceIndex != 0` 那条过滤的影响。**
                //    理由（实读 TMP 源码）：TMP 给非 0 号槽建的子件**是这颗 TMP 的子节点**
                //    （`TMP_SubMesh.AddSubTextObject`，`TMP_SubMesh.cs:235-260`：`SetParent` +
                //    localPosition/rotation/scale **全是单位值**），而本函数是
                //    `t.GetComponentsInChildren<MeshRenderer>(true)` —— 那个子件的 `MeshRenderer`
                //    **同样会被遍历到**，只是它身上没有 `TextMeshPro` ⇒ 走下面 `gi == null` 那一支
                //    （整条数组扫，含零填充槽；对「整张卡的外接盒」这种量法无所谓）
                //    ⇒ **`<sprite>` 那枚图标照样被量进来**。⚠️ 别以为这里漏了图标而改过滤（见 `GlyphVertsOf`）。
                int cnt = gi != null ? gi.Count : vs.Length;
                for (int i = 0; i < cnt; i++)
                {
                    var p = LayoutSpace.ToPixel(mr.transform.TransformPoint(vs[gi != null ? gi[i] : i]));   // 甲式保留：顶点量法（W57 §1·4 第 4 条）
                    verts++;
                    x1 = Mathf.Min(x1, p.x); x2 = Mathf.Max(x2, p.x);
                    y1 = Mathf.Min(y1, p.y); y2 = Mathf.Max(y2, p.y);
                }
            }
            return verts > 0;
        }

        /// <summary>同上，但量的是**一段文字**（TMP 网格顶点，画布像素范围）。
        /// ⚠️ TMP 的网格只在「重排过」之后才有内容 —— 这里如实返回 false（**别当通过**）。
        /// `verts == 0` = 这一段没有可量的网格。
        /// 🆕 2026-10-11（F3）：**按字取**（`characterInfo[i].vertexIndex`），⛔ 不扫整条数组 ——
        /// 同一份数组里还夹着零面积的**非字形占位槽**（见 `GlyphVertsOf`），
        /// 它们停在「这一段字自己的原点」上 ⇒ 整条一扫量出来的是**假的范围**。
        /// ⚠️ 这里读的仍旧是 `textInfo`（**模型**）；要读**真上传的那一份**见本文件那条
        /// 「★ 卡上没有一个**字的顶点**画到视口外」（A250 块）。
        /// <para>🆕 **2026-10-16（A844 · 跨文件那一半）**：本函数**只剩名字** —— 实现全部转调
        /// `ShellScene.TmpSpanPx(Transform, …)`（全仓这一条量法**唯一一份**实现；本文件内层那段循环已删）。
        /// 🔴 **读数逐位不变**：那一份的内层与这里原来的逐句相同（同一个 `isVisible` 过滤 / 同一个
        /// `LayoutSpace.ToPixel(TransformPoint(v))` / 同一套越界检查），而 min/max 与顶点计数**与遍历次序无关**
        /// ⇒ 收口只换了「谁持有这段代码」，⛔ 不是 A490 说的「量法一变」。
        /// ⚠️ `includeInactive` 这一档**写死 `true`**（本函数的判据就是「关着的那张卡上有没有字画到框外」）
        /// —— 那一份的重载里没有这个形参，原因写在它的 doc 里。</para></summary>
        static bool TextExtentPx(Transform t, out float x1, out float y1, out float x2, out float y2, out int verts)
        {
            return ShellScene.TmpSpanPx(t, out x1, out y1, out x2, out y2, out verts);
        }

        /// <summary>🆕 2026-10-11（F3）：一份 TMP 的网格里，**真正是字形的那些顶点**的下标
        /// （每个可见字 4 个角 · 序 = BL·TL·TR·BR，与 `MenuDraw.ClipTmpMesh` / `BaseCornerAlpha` 同一个序）。
        /// <para>🔴 **为什么要它**：`mesh.vertices` **不是**「一个个字的四角」—— 同一份数组里还夹着
        /// **非字形的退化占位槽**（零面积、不进任何三角形 ⇒ **画不出来**）：
        /// ① TMP 对**不可见的字**（空格 / 超出 `maxVisibleCharacters` 的字）把四角全写成 `Vector3.zero`
        ///   并置 `isVisible = false`，**却照旧占这 4 个槽**
        ///   （判据 = `TextMeshPro.cs:4536-4541` 写零 + `:4550-4552` `FillCharacterVertexBuffers(i)` 照跑）；
        /// ② 数组按 2 的幂扩容 ⇒ 尾巴上还有一段**从没写过的零槽**（`TMP_MeshInfo.ResizeMeshInfo`）。
        /// 换算成像素，这些点全落在**那一段字自己的原点**（= 该 TMP 的 pivot）上 —— 字被裁到视口边时
        /// **它们不动**（`ClipTmpMesh` 只认 `isVisible` 的字，`:867` ⇒ **压根不夹这些槽**），
        /// 于是一整条数组扫下来，它们会被当成「字画到视口外」。
        /// **2026-10-11 实红**：`CollectionScene.Run` 的 qOut 期望 [0] 实得 [12] = **3 槽 × 4 点**。</para>
        /// <para>⚠️ **只收 `materialReferenceIndex == 0` 的字**：别的材质那份顶点**不在这一份网格上**
        /// （`TextMeshPro.UpdateVertexData` 把 `meshInfo[0]` 推给 `m_mesh`、`meshInfo[i>0]` 推给子件
        /// `m_subTextObjects[i].mesh`，判据 `TextMeshPro.cs:408-446`）⇒ 拿 `vertexIndex` 索引**本网格**会串位。
        /// ⚠️ 这段**过滤本身是对的**（返回的下标必须能索引**调用方手里那一份**网格）—— 别删。</para>
        /// <para>🔴 **2026-10-20（`A989①`）就地订正（铁律 5）**：原来这里还接着写
        /// 「本工程的卡面标签**只有一个字体资产**（`Resources/Fonts/NotoSerifCJK-Regular SDF.asset`，
        /// `m_FallbackFontAssetTable: []`）⇒ **实际恒为 0**」—— **那个前提不成立**：
        /// `Core/TmpFont.cs` 给**全工程每颗 TMP** 都挂了 `CardIcons.SpriteAsset`，
        /// 而卡面效果文字里**现在就带** `&lt;sprite name=…&gt;`（`Core/CardText.cs` 的 `sprite` /
        /// `Core/CardIcons.cs` 的 `Rewrite`）⇒ TMP 会**切出第二个材质槽**（sprite asset 自带一份
        /// **独立的材质**）—— `A965` 已证（`资料/普查产出_1018/S6_A965卡面探针取槽.md` §2/§3：
        /// 卡面效果文字那层 `keywords` **就是 2 个槽**）。
        /// <b>⇒ 后果 = 本函数会【跳过】那些字</b>（`&lt;sprite&gt;` 那枚图标本身**不在**返回的下标里）。
        /// **两个调用点各是什么后果，见各自的注释**：`RenderExtentPx`（下面）**不受影响**；
        /// 本文件 `Run` 里那段「★ 卡上没有一个**字的顶点**画到视口外」（A250 块，约 `本文件`）
        /// **量不到图标**。⚠️ 那是**自检覆盖面**的缺口，**不是**产品缺陷 —— 裁切本身走
        /// `MenuDraw.ClipTmpMesh`（`Shell/MenuDraw.cs` 的 `ClipTmpMesh` 按**每个字自己的**
        /// `materialReferenceIndex` 取槽、写回 `ti.meshInfo[mi]`），`UpdateVertexData` 再把
        /// **每个槽**都上传（`TextMeshPro.cs:408-446`）⇒ 图标**确实被裁也确实上了屏**。</para>
        /// <returns>字形顶点下标（空表 = 这一段没有可量的字形）。</returns></summary>
        static List<int> GlyphVertsOf(TMPro.TextMeshPro tmp, int meshVertCount)
        {
            var dst = new List<int>(64);
            if (tmp == null) return dst;
            var ti = tmp.textInfo;
            if (ti == null || ti.characterInfo == null) return dst;
            int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
            for (int ci = 0; ci < n; ci++)
            {
                var ch = ti.characterInfo[ci];
                if (!ch.isVisible) continue;
                if (ch.materialReferenceIndex != 0) continue;
                int v = ch.vertexIndex;
                if (v < 0 || v + 3 >= meshVertCount) continue;
                dst.Add(v); dst.Add(v + 1); dst.Add(v + 2); dst.Add(v + 3);
            }
            return dst;
        }
        static float Wpx(Transform t)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            return q != null ? q.WorldW * 108f : 0f;
        }
        static float Hpx(Transform t)
        {
            var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
            return q != null ? q.WorldH * 108f : 0f;
        }

        /// <summary>🆕 **2026-10-18（A994③）**：uGUI `Image.PreserveSpriteAspectRatio` 的
        /// **独立复述**（判据 = uGUI 源码 `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/Image.cs`：
        /// 贴图比例 &gt; 框比例 ⇒ **宽定、高缩**；否则**高定、宽缩**）。返回内接后该填的 `(w, h)`。
        /// <para>🔴 **为什么自检里要单独写一份**：被测实现那一份是 `CosmeticPreview.PreserveAspectSize`
        /// （`Core/DraggableController.cs`）—— **拿它算期望值就是自证**（两边一起改错照样全绿）。
        /// 本函数只吃两样**外部输入**：**贴图资产自己的宽高**（`Texture2D.width/height`）
        /// 与**原版字面量的框**（250×405 / 337.5×550.8 / 220×330），算法照 uGUI 原文复述。
        /// <para>🔴 **2026-10-19（B2）就地收窄用途**：**卡背本体**不再走本式（它走
        /// <see cref="CardbackDrawnPx"/> 的**两段式**）—— 本式现在**只服务 `_SDF` 那两层**，
        /// 而 `_SDF` 是**故意不进** `CardbackFace` 那张表的（它的 `padding` 恒 0、两段式差 &lt;0.4%，
        /// 理由 → `Core/CardbackFace.cs` 文件头）⇒ 本式对 SDF 仍然是**正确**的期望值。</para></summary>
        static Vector2 InsetFit(float spriteAspect, float boxW, float boxH)
        {
            if (spriteAspect <= 0f || boxW <= 0f || boxH <= 0f) return new Vector2(boxW, boxH);
            return spriteAspect > boxW / boxH
                ? new Vector2(boxW, boxW / spriteAspect)          // 宽定（贴图「更宽」）
                : new Vector2(boxH * spriteAspect, boxH);         // 高定
        }

        /// <summary>🆕 **2026-10-19（B2）**：一张**卡背**在 `boxW×boxH` 的框里**画出来**该多大
        /// —— 原版 uGUI `Image` 的**两段式**（判据与算式全文 → `Core/CardbackFace.cs`）。
        ///
        /// <para>🔴 **为什么自检里要单独写一份**：被测那一份是 `CardbackFace.Fit`（`CosmeticPreview.PreserveAspectSize`
        /// 转调它）—— **直接调它就是自证**（两边一起改错照样全绿）。本函数只吃三样**外部真值**：
        /// ① **贴图资产自己的 `width/height`**（`Resources/Art/cardbacks/` 那 233 张 = 原版 `textureRect` 的裁片）；
        /// ② 原版字面量的框（250×405 / 335.31×400 / 220×330）；
        /// ③ 原版那张 sprite 的 **`m_Rect` = `707×1020`**（233/233 恒定，出处 → `Core/CardbackFace.cs` 文件头）。
        /// 算式照 uGUI 原文复述：**先按 `m_Rect` 的比例定框**（`PreserveSpriteAspectRatio`），
        /// **再按 `textureRect/m_Rect` 缩**（`GetDrawingDimensions` 里那四个 `v`）。</para>
        ///
        /// <para>⚠️ 这里拿 `tex.width/height` 顶 `textureRect` 的宽高（差 ≤0.5px 的取整 ⇒ 尺寸上 ≤0.18px，
        /// 容差 0.6px 挡得住）；**位置**那一半（按 `padding` 偏）拿不到 ⇒ 由
        /// <see cref="CardbackOffsetPx"/> 另走一路。</para></summary>
        static Vector2 CardbackDrawnPx(Texture2D tex, float boxW, float boxH)
        {
            const float RectW = 707f, RectH = 1020f;              // 原版 sprite 的 `m_Rect`（233/233 恒定）
            if (tex == null || tex.height <= 0 || boxW <= 0f || boxH <= 0f) return new Vector2(boxW, boxH);
            // ① 定框：比的是 **`m_Rect` 的比例**，⛔ 不是贴图自己的（这就是 B2 之前漏掉的那一段）
            float spriteRatio = RectW / RectH, boxRatio = boxW / Mathf.Max(1e-6f, boxH);
            float fw, fh;
            if (spriteRatio > boxRatio) { fw = boxW; fh = boxW / spriteRatio; }   // 宽定
            else { fh = boxH; fw = boxH * spriteRatio; }                          // 高定
            // ② 画心 = 框 × (textureRect / m_Rect)（两轴系数相等 ⇒ 等比）
            return new Vector2(fw * tex.width / RectW, fh * tex.height / RectH);
        }

        /// <summary>🆕 **2026-10-19（B2）**：画心相对**框中心**的偏移（画布像素 · **y 向下** · 左上原点）。
        ///
        /// <para>数据 = 原版那张 sprite 的 **`padding`**（`Resources/Cardbacks.json` 的 `padL/padR/padB/padT`
        /// 四列，由 `工具/gen_cardbacks.py` 从原版 `Sprite/&lt;名&gt;_Main.json` 的 `m_Rect − textureRect` 直读）。
        /// ⚠️ **这是「数据」、不是「被测算法」** —— 拿被测实现算期望值才是自证；读同一份**原版数据**
        /// 不算（口径先例：本文件 `InsetFit` 吃 `tex.width/height` 也是数据）。
        /// 残余风险（**明账**）：表里某一行的 `padX` 若被生成器抄错，这一条抓不到（它只验「实现有没有按这四列挪」）。
        /// 独立复核那四列要走 `d:/2/…/Sprite/*.json`，本文件**没读**（不引外部目录依赖）。</para>
        ///
        /// <para>算式的方向照 uGUI 原文**独立复述**：`offset = ((padL−padR)/2, (padB−padT)/2) × (框宽 / `m_Rect.w`)`，
        /// 而 uGUI 的 y **向上** ⇒ 本仓画布像素（y 向下）要**反号**（**这是最容易抄反的一处**）。</para>
        /// 查不到该贴图（不是卡背 / 表是旧版）⇒ 返回 `false`（调用方按「前提不成立」红出来，⛔ 不静默跳过）。</summary>
        static bool CardbackOffsetPx(Texture2D tex, float boxW, float boxH, out float dx, out float dyDown)
        {
            dx = dyDown = 0f;
            if (tex == null) return false;
            float rw, rh, pl, pr, pb, pt;
            if (!CardbackTable.TrySpriteRect(tex.name, out rw, out rh, out pl, out pr, out pb, out pt)) return false;
            float spriteRatio = rw / rh, boxRatio = boxW / Mathf.Max(1e-6f, boxH);
            float fw = spriteRatio > boxRatio ? boxW : boxH * spriteRatio;
            float k = fw / rw;                                    // == fh / rh
            dx = (pl - pr) * 0.5f * k;
            dyDown = -(pb - pt) * 0.5f * k;                       // uGUI 向上为正 ⇒ 本仓向下为正 = 反号
            return true;
        }

        /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
        /// 图走 `ImageQuad.WorldW/H`（材质的真测量，不是回读我们传进去的数）、
        /// 字走 **TMP 自己渲出来那块 `textBounds`**（🆕 **2026-10-16 · A796′ 换口** —— 见 <see cref="LabelRenderedPx"/>）；
        /// 取的是**组件自己的 transform**（`AlignLeft/Right` 会把 `Label` 的节点挪走，拿外层容器算就会偏）。
        /// 🆕 2026-10-03：本文件原来只有 `Wpx/Hpx`（只给宽高）—— 要量「这块矩形落在哪」时不够用，
        /// 照 `RewardsScene.RectOf` / `ShopScene.RectOf` 的同名口子补一份
        /// （本文件开头那条注释已经明记：「四个自检各自一套辅助函数」是**一笔明账**，本轮不动那三个绿着的文件）。</summary>
        static bool RectOf(Transform t, out float x1, out float y1, out float x2, out float y2)
        {
            // 🆕 **2026-10-20（`A1350` 第一批）**：实现转调 `MenuCheck.NodeRectPx(…, LabelNode, …)`
            // （自检侧**唯一一份**节点矩形读口）。中心本来就是乙式（`A1350` 第 ② 步 · 第五十五会话 S3 已落）
            // ⇒ 纯搬、**行为零变化**；本函数保留原名与本形参 ⇒ 调用点 0 改动。
            // 🆕 **2026-10-21（`T02` · 计划 §2·3「尺寸帧」收口）**：原来这里有一块**尺寸覆盖块**
            // （拿 `SelfCheckTextMetrics.RenderedRectDesign` 的宽高、以原中心重铺），本轮**提回**
            // `MenuCheck.NodeRectPx` 的 `designSize: true` 档（四份逐字相同的覆盖块归一 ⇒ 全仓一份）
            // —— 行为**逐字不变**：中心仍是设计帧（`LabelNode` 档），尺寸改读设计帧；
            // 量不到时与缺省档同一条退路（`Label.WorldW/H × 108`，本来就是设计长度）。
            // ⚠️ 只动**尺寸来源**：中心不从 TMP 的 `textBounds` 重算（那是 `AlignLeft/Right` 挪节点后的口径，会动读数）。
            // 🧨 **把 `designSize: true` 改回缺省（或删掉 `MenuCheck` 里那一支）⇒ 父链 ×2 时本行报 `2 × 设计宽`
            //     ⇒ 本节尾部「T02：`RectOf` 的尺寸帧」那条 `设计宽 ≈ 世界宽 ÷ 2` 红。**
            return MenuCheck.NodeRectPx(t, MenuCheck.RectCenterMode.LabelNode,
                                        out x1, out y1, out x2, out y2, designSize: true);
        }
        static string TextOf(Transform t)
        {
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            return lb != null ? lb.Text : null;
        }

        /// <summary>筛选栏里一行小标题 **渲出来的左沿**（画布 px · 左上原点 · y 向下）。
        /// 🔴 量的东西：**TMP 自己渲出来那块 `textBounds`**（`LabelRenderedPx`；
        /// 🆕 **2026-10-16 · A796′ 换口** —— 换口前读的是 `Label.WorldW` 那份**字段缓存** `_tmpW`）反推的左缘 ——
        /// **不是**节点位置、更**不是**「对齐枚举 == Left」（那种断言是同义反复：把渲染那一句删掉照样绿）。
        /// ⛔ 期望值由**调用方**给（取自原版读数），本函数只负责量。
        /// 量不出来（那行小标题不在 / 宽度是垃圾）⇒ 返回 **−9999** ⇒ 断言必红，**不静默放过**
        /// （宽度上下界那道守卫与 `Label.HasMeasuredWidth` 同一条：TMP 在未激活 / 空串时给的是天文数字；
        ///  ⚠️ 本条**保留**那道守卫，⛔ 别因为换了口就把它删掉）。</summary>
        static float TitleLeftPx(Transform panel, string title)
        {
            var t = FindChild(panel, "Title " + title);
            var lb = t != null ? t.GetComponentInChildren<Label>() : null;
            if (lb == null) return -9999f;
            float w = LabelRenderedPx(lb).x;      // 🆕 A796′：走 TMP `textBounds`（旧口 = 缓存 `Label.WorldW`）
            if (!(w > 20f && w < 2000f)) return -9999f;
            return MenuCheck.DesignPx(lb.transform).x - w * 0.5f;
        }

        // ============================================================ 🆕 2026-10-04：软边接线探针
        //
        // 判据 = `Shell/MenuDraw.cs` 的 `ApplySoftEdges`（原版 `RectMask2D.m_Softness` 的几何等效物）：
        //   非 0 时把一块**沿渐隐带的内沿切开**（原节点留含矩形中心的那一格、其余格建**子 quad**，
        //   命名 `…_soft<i><j>`）⇒ 父块与子块那条**共享边**就是带的内沿。
        // 🔴 **为什么这就是「接没接」的判据**：软边 = 0（硬边）时**一个子块都不会有** ⇒ 表空。
        //   于是「表空 = 没接」；而「切线该在哪」是**自己拿原版值 + 原版视口矩形算出来的**
        //   （⛔ 不是从被测实现里读常量）⇒ 改坏实现（删赋值 / 改数值 / 把两轴写反）都会真红。

        /// <summary>一个 `ImageQuad` **渲出来**的像素矩形。⚠️ 只用**这个组件自己**
        /// （不比 `RectOf`：那个会先找 `Label`、还会往子树里钻 —— 量切出来的每一块必须逐块量）。</summary>
        static bool QuadRectOf(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
        {
            // 🆕 2026-10-18（A1003 · 账外第 6 份）：算法收口到 `MenuDraw.QuadRectPx`（保留本名与本形参 ⇒ 调用点 0 改动）。
            // 🔴 改前这一份是**甲式**（裸 `position × 108 ± 原点`）；`MenuDraw` 那份是**乙式**
            //    （先 `PosInDesignSpace` 除回父级缩放）⇒ 父链 `lossyScale == 1` 时逐位相同。
            return MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
        }

        /// <summary>扫 `root` 子树，回传里面**所有软边切线**的位置（同 `RewardsScene.ScanSoftCuts`）。
        /// <param name="vertical">true = 只看**竖切线**（渐隐的是左右，= `m_Softness.x`）；false = 看横切线。</param>
        /// <param name="clip">这一棵子树所在的那个视口（**设计 px**，= 原版 `Viewport` 上那颗 `RectMask2D` 的框；
        /// 传进来的一律是**原版字面量 / 已断言的视口局部量**，⛔ 不从被测实现里读）。
        /// 🔴 **2026-10-11（A232）：落在 `clip` **本轴**两条边上的切线【不算软边切口】** ——
        /// `MenuDraw.ApplySoftEdges` 的切刀位置 = `clip.边 ± softness`，**某个分量 = 0 时那两条刀口
        /// 正好落在框自己的两条边上**（那是**硬裁**边，不是渐隐带的内沿）⇒ 少了这一闸，「数竖切线 = 0」
        /// 那几条的鉴别力就**依赖「实现侧恰好会裁」这个偶然性质**（判据原文 →
        /// `资料/普查产出_1008/波A_A225_三条红.md` 顺手发现 §2；4 份同形副本一起补）。
        /// ⚠️ **它是潜伏闸**：今天 `ApplySoftEdges` 入口先 `ClipVisToClip` 硬裁 ⇒ `vis ⊆ clip` ⇒
        /// `SoftCuts` 的「严格落在 `vis` 内部」**已经**挡掉了这类切口 ⇒ **今天一条读数都不会变**；
        /// 它防的是「实现侧哪天不裁了」那一档（那时它会**静默**把硬裁边当成「软边接上了」）。
        /// ⚠️ 形参**必填**（⛔ 不给默认值）：漏传 = 这道闸静默失效 ⇒ 让编译期拦住。</param></summary>
        static List<float> ScanSoftCuts(Transform root, bool vertical, PxRect clip)
        {
            var cuts = new List<float>();
            if (root == null) return cuts;
            // 🔴 A232 的闸：切刀落在**本轴**两条 `clip` 边上 ⇒ 那是硬裁边、不算软边切口（容差同下面 0.5px）
            bool OnClipEdge(float v) { return vertical
                ? (Mathf.Abs(v - clip.x1) <= 0.5f || Mathf.Abs(v - clip.x2) <= 0.5f)
                : (Mathf.Abs(v - clip.y1) <= 0.5f || Mathf.Abs(v - clip.y2) <= 0.5f); }
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                float hx1, hy1, hx2, hy2;
                if (!QuadRectOf(q, out hx1, out hy1, out hx2, out hy2)) continue;
                for (int i = 0; i < q.transform.childCount; i++)
                {
                    var c = q.transform.GetChild(i).GetComponent<ImageQuad>();
                    // ⚠️ **只认软边切出来的子块**（`ApplySoftEdges` 的命名 `baseName + "_soft" + i + j`）——
                    //    `ImageQuad.CreateNineSlice` 那 9 块是**兄弟**不是父子，但留一道名字闸更保险。
                    if (c == null || c.name.IndexOf("_soft") < 0) continue;
                    float cx1, cy1, cx2, cy2;
                    if (!QuadRectOf(c, out cx1, out cy1, out cx2, out cy2)) continue;
                    if (vertical)
                    {
                        float vR = cx1;                       // 子块在**右** ⇒ 切线 = 子块左沿
                        float vL = cx2;                       // 子块在**左** ⇒ 切线 = 子块右沿
                        if (Mathf.Abs(vR - hx2) < 0.5f) { if (!OnClipEdge(vR)) cuts.Add(vR); }
                        else if (Mathf.Abs(vL - hx1) < 0.5f) { if (!OnClipEdge(vL)) cuts.Add(vL); }
                    }
                    else
                    {
                        float vB = cy1;                       // 子块在**下** ⇒ 切线 = 子块上沿
                        float vT = cy2;                       // 子块在**上** ⇒ 切线 = 子块下沿
                        if (Mathf.Abs(vB - hy2) < 0.5f) { if (!OnClipEdge(vB)) cuts.Add(vB); }
                        else if (Mathf.Abs(vT - hy1) < 0.5f) { if (!OnClipEdge(vT)) cuts.Add(vT); }
                    }
                }
            }
            return cuts;
        }

        /// <summary>切线清单的**逐条**判据：每条都必须落在 `want` 里（±`tol`），且 `want` 每一项**都出现过**。
        /// `what` 里写清每一侧的算式（判据要能在失败信息里一眼看懂）。</summary>
        static void CheckSoftCuts(List<float> cuts, float[] want, float tol, string what)
        {
            var hit = new bool[want.Length];
            CheckTrue(cuts.Count > 0, what + "：**有层被软边切开**（切线实测 "
                + (cuts.Count > 0 ? string.Join("、", cuts.ConvertAll(v => v.ToString("F2")).ToArray()) : "一条都没有")
                + "）—— **空表 = 这条软边没接上**（`ClipSoftness` 留在 0）");
            for (int i = 0; i < cuts.Count; i++)
            {
                int k = -1;
                for (int j = 0; j < want.Length; j++)
                    if (Mathf.Abs(cuts[i] - want[j]) <= tol) { k = j; break; }
                CheckTrue(k >= 0, what + $"：切线 #{i + 1} 在 {cuts[i]:F2} ⇒ 必须是带的内沿"
                    + "（" + string.Join(" / ", System.Array.ConvertAll(want, v => v.ToString("F2"))) + "）");
                if (k >= 0) hit[k] = true;
            }
            for (int j = 0; j < want.Length; j++)
                CheckTrue(hit[j], what + $"：**{want[j]:F2} 这条切线确实出现**（少一条就说明那侧的软边没生效）");
        }

        // ============================================================ 🆕 A327：两态夹具（一条共用 · 四份【函数体】逐字同源）
        //
        // 🔴 **为什么要它**：2026-10-11（W4）把「世界 → 设计」那一族（`MenuDraw.PosInDesignSpace` / 各窗的
        //   `Local`·`Local3` / `CampaignTab.BuildLine` / `ShopWindow.BuildTimeCounter` / `CampaignTab.BuildArmyItems`）
        //   修完之后发现：**`k == 1`（小屏缩放开关出厂关着）时新旧两式逐位相同** ⇒ 那 8 处全是**潜伏缺陷**
        //   —— **今天一条现有断言都不会红**（不是「有断言挡着」，是**还没有断言**）。
        //   ⚠️ **2026-10-11（FX3）收窄一处口径**：那 8 处里**基准恰好就是窗根**的那几处，新旧两式在生产里
        //   **永远**逐位相同（`basis == 窗根` ⇒ 除的是 Holder，恒单位缩放）⇒ 是 **no-op**，不是「潜伏」；
        //   真带牙口的是**非根基准**那一族。展开见本段后面那条订正。
        //   判据 / 逐处清单 / 「该断言什么」→ `资料/普查产出_1011/W4_子3.md` §四·b。
        //
        // 🔴 **夹具形状**（判据给的就是这一条，⛔ 别另设计一套）：
        //   ① 态一 = 开关**关**（出厂态）⇒ 量一次 → `p1`；② 态二 = 开关**开** + **被乘的那一级**乘 M（走**生产那条路**
        //   `TransformScalerBySmallScreenUI`：`SetScale(M)` + `Tick()`，批处理没有帧循环）⇒ 再量同一个对象 → `p2`；
        //   ③ 断 **`p2 == M × p1`**（⛔ **一个我们自己的常量都不读** —— 只读 M）。
        //
        // 🔴 **2026-10-11（FX3）三处订正 —— 上一版夹具【自己把这条恒等式砸了】**（Shell 4 + Collection 4 条红；
        //   判据全文 → `资料/普查产出_1011/DIAG-A_Shell与Collection八条红.md`）：
        //   ① **M 加在【基准的父级】那一级**，⛔ **不是基准自己** —— `PosInDesignSpace` 除的正是
        //      `t.parent.lossyScale`（`Shell/MenuDraw.cs` 的 `t.parent.lossyScale`），而 `p2 == M × p1` **只在「基准的父级就是
        //      那个被乘 M 的根、且那个根在世界原点」时成立**（`Shell/MenuDraw.cs` 自己写着适用范围）。
        //      上一版把 M 加在**窗根自己**身上、又把**窗根**挪到 (2,1.5) ⇒ 恒等式被夹具亲手破坏：
        //      偏差逐条 = `(1−M)×(2,1.5)` = **(−0.400, −0.300)**，与实现无关（日志里 8 条逐条对到小数点后 3 位）。
        //   ② **可观测余量** = 「**基准相对被乘那一级的位移** ≥1 设计单位」（⛔ 不是「离**世界原点**」——
        //      上一版量的就是后者，所以它逼着调用方去挪窗根）。坏式与好式相差 `M(M−1)×|那个位移|` ≈ `0.24 × |位移|`，
        //      容差 **0.02 单位（2.2px）** ⇒ `|位移| ≥ 1` 时偏差 ≥ 0.24 单位 = **26px**，远远超出容差 ⇒ 真会红。
        //      （四个调用点取 (2,1.5) ⇒ 0.6 单位 = **65px**。）
        //   ③ **态二的 `measure` 里必须【重建】**（`Open()` → `Build()` 首句清空子件）—— 不重建时被量的局部位置
        //      是 `k == 1` 那一趟**冻结**下来的值，新旧两式在那时**逐位相同** ⇒ 断言恒真（= 假绿）。
        //      带牙口的判据 → `资料/普查产出_1011/W4_子3.md:91-106`；同族先例 = `Editor/ShopScene.cs` 的 **A294** 那一段（同文件的两态探针）。
        // 🔴 **参数 1 = 被乘 M 的那一级**（本文件与 `ShellScene` 那四个调用点里它是**窗根的父级探针根**；
        //   ⚠️ 另两份副本（`RewardsScene` / `ShopScene`）传的是**窗根自己**、基准是窗根的子件 —— 2026-10-11（A350）四份已同步到**同一口径**；两族各自都对，⛔ 别按「哪一族更对」去改）。
        // ⚠️ **态二会把那一级乘 M 再还原**（`localScale` 放回 1 · 组件销毁 · 开关放回关）—— 直线写法，没有提前 return。
        static void CheckScaleTwo(GameObject scaleRoot, Transform basis, System.Func<Vector3> measure, float m, string what)
        {
            CheckTrue(scaleRoot != null && basis != null && measure != null, "（前提）" + what + "：夹具的件齐了");
            if (scaleRoot == null || basis == null || measure == null) return;
            // 🔴 **`A1390`（2026-10-19 · 第五十五会话 S3）：写盘闸由**本读口自己压**。**
            //   本函数两态都要 `SmallScreenUI.Set(...)`，而它**会落 `PlayerPrefs`**（= 改玩家的真设置）。
            //   原来靠「调用点记得先压」—— 那是**隐式契约**（新调用点一忘就写盘，而且没有任何报错）。
            //   ⇒ 在这里存一份、压成 `true`、收尾放回（`PersistOverride` 只改「写不写盘」、不改生效值）。
            //   形状与 `Editor/BattleScene.cs` 开头压 `AutoZoom` / `SmallScreenUI` 那两段同源。
            bool smWasPersist = SmallScreenUI.PersistOverride;
            SmallScreenUI.PersistOverride = true;
            SmallScreenUI.Set(false);                              // 态一：开关**关**（出厂态）
            Vector3 p1 = measure();
            Vector3 b1 = basis.position;                           // 态一的基准位置 = 它的**设计**位置（k == 1）
            Vector3 w1 = scaleRoot.transform.position;             // 被乘那一级的位置（态一；生产里 = 原点）
            // （前提②·可观测余量）**基准相对被乘那一级的位移** ≥1 设计单位 —— 基准落在那一级的原点上时
            // 「除不除缩放」两式**恒等** ⇒ 断言「什么都不中」也全绿。⛔ 上一版量的是「离**世界原点**」，
            // 逼着调用方去挪窗根、又把恒等式砸了（见本段文件头 ①②）。
            CheckTrue(Mathf.Abs(b1.x - w1.x) > 1f || Mathf.Abs(b1.y - w1.y) > 1f,
                      $"（前提）{what}：**基准相对被乘 M 那一级的位移 ≥1 设计单位**（实测 {b1.x - w1.x:F2},{b1.y - w1.y:F2}）"
                    + " —— 位移≈0 时「除不除缩放」两式恒等 ⇒ 这一条会退化成假绿");
            CheckNear(scaleRoot.transform.localScale.x, 1f, 1e-4f, "（前提）" + what + "：态一那一级没被谁乘过");
            SmallScreenUI.Set(true);                               // 态二：开关**开** + 那一级乘 M
            var sc = scaleRoot.GetComponent<TransformScalerBySmallScreenUI>();
            if (sc == null) sc = scaleRoot.AddComponent<TransformScalerBySmallScreenUI>();
            sc.SetScale(m);
            sc.Tick();                                            // 批处理没有帧循环 ⇒ 手动推一次
            CheckNear(scaleRoot.transform.localScale.x, m, 1e-4f,
                      "（前提）" + what + "：态二那一级 `localScale` = M（真走的生产那条路）");
            // （前提①）**被除的那一级真的被乘了 M** —— `PosInDesignSpace` 除的是 `basis.parent.lossyScale`；
            // 那一级是单位缩放时新旧两式**逐位相同** ⇒ 下面那条 ★ 等于没查。改坏法：M 仍加在 `basis` 自己身上
            // （= 上一版那种塞法）⇒ 这条红。
            CheckNear(basis.parent != null ? basis.parent.lossyScale.x : 1f, m, 1e-3f,
                      "（前提）" + what + "：**基准的【父级】在态二被乘了 M**（`PosInDesignSpace` 除的正是这一级，"
                    + "`Shell/MenuDraw.cs`）—— 父级单位缩放时新旧两式**恒等**，这条断言就等于没查");
            Vector3 p2 = measure();
            CheckNear(p2.x, m * p1.x, 0.02f,
                      $"★ {what}：**态二 == M × 态一**（x：{p2.x:F3} vs {m:F2}×{p1.x:F3}）"
                    + " —— 两态合起来才证明「这一处的落位真的跟着被乘 M 的那一级缩放走」"
                    + "（`k == 1` 时新旧两式逐位相同 ⇒ 只断态一的话，改坏了照样绿）");
            CheckNear(p2.y, m * p1.y, 0.02f, "★ " + what + "：……y 分量同理（只改 x 不改 y 时只有上一条红）");
            Object.DestroyImmediate(sc);                           // 还原
            scaleRoot.transform.localScale = Vector3.one;
            SmallScreenUI.Set(false);
            CheckNear(measure().x, p1.x, 0.02f, "（收尾）" + what + "：那一级放回 1 之后位置也回到态一那一份");
            SmallScreenUI.PersistOverride = smWasPersist;           // `A1390`：写盘闸放回进场那一档
        }

        static void Shoot(string file, bool allowBlank = false)
            => MenuCheck.Shoot(_sink, ShotDir, file, allowBlank, guardBlank: true, meanBrightness: MeanBrightness);

        static float MeanBrightness(Texture2D t)
        {
            if (t == null) return 0f;
            var px = t.GetPixels32();
            if (px.Length == 0) return 0f;
            long sum = 0;
            for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b;
            return sum / 3f / ((px.Length + 6) / 7);
        }

        // ============================================================ 建场景

        static CollectionWindow Build(out Transform root)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);

            // 🔴 **2026-10-11（A351）就地订正（铁律 5）**：这里原来**手抄了第三份**「三颗 Holder +
            //    `AddComponent<WindowsManager>()`」—— 那套手抄在**批处理（编辑模式）**下有一处硬伤：
            //    · `WindowsManager` **没有 `[ExecuteAlways]`**（`Shell/WindowsManager.cs` 里**只有** `WindowHolder` 那颗**有**）
            //      ⇒ `Awake` 不跑 ⇒ **`Instance` 恒 null**（`Instance` 只在 `Awake` 里赋，
            //      **批处理下那句从不执行**）。判据（**四条独立记录**，全是踩过的坑）：
            //      `Shell/WindowButton.cs` 的 `WindowButton` 类注（⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs`）· `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 · `Shell/PointerLayer.cs` 的 `WindowButton`
            //      · `资料/已知的坑.md:704`（「编辑模式下 `Awake/OnEnable`/`Update` **只对带 `[ExecuteAlways]`
            //      的脚本**才跑」）。
            //    · ⇒ **任何走 `WindowsManager.EnsureHost()` 的开窗路径都会【再建一台】**（它在 `Instance == null`
            //      时不查「场景里是不是已经有一台」、直接再建一套管理器 + 锚点）
            //      ⇒ 窗落进**第二台**，而 `wm` 是第一台 ⇒ `wm.openWindows` 里没有它、读
            //      `WindowsManager.Instance` 的代码（如 `Shell/DailyData.cs` 里读 `WindowsManager.Instance` 的那一处）**静默**拿到另一台。
            //      2026-10-11 的那 8 条红（`RewardsScene` 十一条里的 #1 及其级联）就是**这一处形状**造成的，
            //      判据全文 → `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1；A351 = 剩下这几处一起收口。
            //    **最小改法 = 走公共件**：`EnsureHost` 里那句 `Instance = wm` 就是为此加的；
            //    而 `EnsureHost()` 的文档注释白纸黑字写着「壳（`ShellRuntime`）与「单独打开某个界面场景
            //    按 Play」**都走它**，两处各建一次 = 迟早不一致（CLAUDE.md §三）」。
            //    ⚠️ 与改前的场景层级**一致**：`EnsureHost()` 建的 Holder 名 / placement 与手抄那份**逐字相同**
            //      （同一个 `MakeHolder` 形状：`1 - Below Upper Bar Holder` / `2 - Canvas Holder Above upper bar` /
            //      `3 - PopUp Holder`，三颗都齐、都在场景根），且 `root == null` ⇒ 同样没有父。
            //    ⚠️ 顺带把 `PointerLayer` 的创建时机提前到 `Build()` 那一刻（`EnsureHost` 第一句就是
            //      `PointerLayer.Ensure(root)`）—— **已核：无可观测差异**（两边都是无父的
            //      `new GameObject("Pointer Layer")`；`RegisterScroll` 读的是惰性 getter
            //      ⇒ 登记表内容一字不变。同 `RewardsScene` 那条，见 `资料/普查产出_1011/FX4_Rewards十一条红修复.md` §六·6）。
            //    **改坏法（如实说 —— 今天【照不出来】，它是一笔【去掉地雷】的改动，⛔ 不是「修好了一条会红的断言」）**：
            //      把这一句换回手抄的 `AddComponent<WindowsManager>()` ⇒ `Instance` 又变回 null；而**本自检今天没有**
            //      走 `WindowsManager.EnsureHost()` / `OpenByRef()` 的开窗入口（现场全部是直调 `win.Manager.OpenWindow(...)`）
            //      ⇒ **改坏它，本文件一条断言都不会红**。它的判别力在【将来】：`WindowsManager.OpenByRef()` 的**第一句**
            //      就是 `EnsureHost()` —— 谁在这几扇窗里接一条走它的入口（`BattleLogTab` / `LeaderboardRow` 那一族就是
            //      这么接的），第一次跑就会**另建一台管理器 + 第二套锚点**、窗落进第二台 ⇒ 现象与判据 →
            //      `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1（`RewardsScene` 那 8 条红就是同一个形状）。
            var wm = WindowsManager.EnsureHost();      // 它自己建 "Window Anchors" + 三颗 Holder + 管理器，并**登记 `Instance`**

            // 🔴 **`A1834`（2026-10-20）**：清一次探针读账 —— 下面那条断言读的是「**建这一扇窗时**
            //   文字重排探针在什么位置跑的」，别让上一次建窗（或别的宿主）的读数混进来。
            CollectionWindow.TextRelayoutProbe.Reset();

            var win = CollectionWindow.Create(wm);       // ← `Create` 末尾会 `Build()`（四页全建）
            wm.OpenWindow(win);
            root = win.transform;
            return win;
        }

        /// <summary>⚠️ **2026-10-11（A351）起 `Build()` 不再调它** —— 那三颗 Holder 现在由
        /// `WindowsManager.EnsureHost()` 建（同一个形状、名字与 placement 逐字相同，见 `Build()` 里那段订正）。
        /// **它留着不删**：这是「单独打开某个界面场景」那条路的**形状存档**（同形手抄全仓原有 4 处，A351 全收口）
        /// —— 留着比删掉更能让下一个会话看出「原来长什么样」。⛔ 新代码别调它。
        /// ⚠️ 它**不是** `WindowsManager` 里那份同名私有件（那份在 `Shell/WindowsManager.cs` 里是 `static` 私有、复用不了）
        /// —— 这正是当年四处各抄一份的来由。</summary>
        static void MakeHolder(Transform parent, string name, WindowsPlacement p)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            var h = t.gameObject.AddComponent<WindowHolder>();
            h.placement = p;
            h.RegisterNow();
        }

        // ============================================================ 自检

        // ==================================================================
        //  🆕 2026-10-17（`A1212` 块 2/3/4/6/8/11）：**「实参 = 原版四格」**的断言。
        //
        //  判据 = 原版 MB 的四个字段原文（`m_fontSizeMin` / `m_fontSizeMax` / `m_fontSizeBase` /
        //  `m_TextWrappingMode`）—— 逐值与 `资料/普查产出_第十一会话/RA1212_切块表.md` 那几块的
        //  「原版四格」列相同（下标 = 块号 + `#` + R5/R6 的原始号）。
        //  ⛔ 期望值**不许**从被测助手/常量读回来（自证）。
        //  ⚠️ **块 5（`MissionsTab`）/ 块 10（`DailyStreakPopup`）/ 块 12（`RewardWindow`）不在本宿主的树上**
        //    （它们只在 `ShellScene` 里建）⇒ 本文件**没有**它们的断言，如实登记在 T07 报告里，
        //    ⛔ 不写「查不到就跳过」的假绿。
        // ==================================================================

        /// <summary>按名字**往深处**找一个节点（`MenuCheck.FindChild` 只认直接子件）。</summary>
        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>`A1212` 的公用断言之「一颗字的四格 + 折行档」。</summary>
        static void CheckA1212(Transform root, string name, string origNode,
                               float mn, float mx, float bs, int wrap,
                               float boxW = -1f, float boxH = -1f)
        {
            var t = FindDeep(root, name);
            var lb = t != null ? t.GetComponent<Label>() : null;
            CheckTrue(lb != null, $"（前提）`A1212` `{name}` 建出来了（原版 `{origNode}`）");
            if (lb == null) return;
            string tag = $"`{name}`（原版 `{origNode}`）";
            CheckTrue(lb.AutoSizing, $"★ A1212 {tag}：**`m_enableAutoSizing = 1`**（自适应开着）");
            // 实例口（`FontSizeToPxNow` + `ActualFace`）才与写进去的那一路同源 —— 静态口在拉丁面/画布档下不同源。
            CheckNear(lb.FontSizeToPxNow(lb.FontSizeMin, lb.ActualFace), mn, 0.05f,
                      $"★ A1212 {tag}：下界 = 原版 `m_fontSizeMin` {mn}px");
            CheckNear(lb.FontSizeToPxNow(lb.FontSizeMax, lb.ActualFace), mx, 0.05f,
                      $"★ A1212 {tag}：上界 = 原版 `m_fontSizeMax` {mx}px");
            CheckNear(lb.FontSizeToPxNow(lb.FontSizeBase, lb.ActualFace), bs, 0.05f,
                      $"★ A1212 {tag}：二分起点 = 原版 `m_fontSizeBase` {bs}px"
                    + "（🧨 只开自适应、不给 base ⇒ 这一格红）");
            Check(lb.WrappingMode, wrap,
                  $"★ A1212 {tag}：折行档 = 原版 `m_TextWrappingMode` {wrap}"
                + "（`SetAutoFitBox` 内部会无条件开 `Normal(1)` ⇒ 这一格抓「接完忘了还原」）");
            var tmp = t.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp == null) { CheckTrue(false, $"（前提）A1212 {tag}：走 TMP 后端"); return; }
            float k = lb.IsUgui ? 1f : (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);
            if (boxW > 0f)
                CheckNear(Mathf.Abs(tmp.rectTransform.sizeDelta.x) * k, boxW, 0.05f,
                          $"★ A1212/A1226 {tag}：框宽 = 原版有效框 {boxW}px");
            if (boxH > 0f)
                CheckNear(Mathf.Abs(tmp.rectTransform.sizeDelta.y) * k, boxH, 0.05f,
                          $"★ A1212/A1226 {tag}：框高 = {boxH}px");
        }

        public static void Run()
        {
            // 🔴 **2026-10-19（`A1534` 尾巴 · 铁律 11）**：批处理自检**不出声** —— `WFSoundPlayer.Enabled = false`
            //   时那个开关自己的契约是「**只记账不发声**：不建对象、不出声，但 `Played` 照样涨」
            //   ⇒「音效调度算对了没有」那一族断言**照旧有效**，而这一趟**不白建一堆 `AudioSource`、不白播**。
            //   先例 = `WarpforgeArena1/Editor/AnimFXCheck.cs`（同一个开关、同一个 `finally` 还原）。
            //   ⚠️ 它是**全局静态开关** ⇒ 必须在 `finally` 里还原；`RunCore` 本体保持原样（只换了名字）。
            bool prevSoundEnabled = WarpforgeVFX.WFSoundPlayer.Enabled;
            WarpforgeVFX.WFSoundPlayer.Enabled = false;
            try { RunCore(); }
            finally { WarpforgeVFX.WFSoundPlayer.Enabled = prevSoundEnabled; }
        }

        /// <summary>`Run()` 的**本体**（🆕 `A1534` 尾巴：`Run` 只负责「关音效 → 调它 → 还原」这一圈）。
        /// ⚠️ 入口仍是 `Run`（`-executeMethod CollectionScene.Run` 一个字没变）。</summary>
        static void RunCore()
        {
            _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
            Directory.CreateDirectory(ShotDir);

            // 自检**不碰玩家的真存档**：`DeckStore.OverridePath` 指到临时文件，并**先造 14 套卡组**
            //（14 套 = 3 行 > 视口 2.4 行 ⇒ 列表**滚得动**，才验得到滚动那条）
            DeckStore.OverridePath = TestDeckFile;
            if (File.Exists(TestDeckFile)) File.Delete(TestDeckFile);
            CollectionData.ResetForTest();
            {
                var lib = DeckLibrary.Load();
                for (int i = 0; i < 14; i++) lib.Create("测试卡组 " + (i + 1));
                lib.Save();
            }
            CollectionData.ResetForTest();
            int fixtureCards = 0;      // 夹具那副的卡数（下面填；断言放在「开始」横幅之后，别抢在标题前面打印）
            // 🔴 给第 1 套塞**真卡** —— `Deck info Popup` 的 `Deck List` 要有东西可画
            //   （`DeckLibrary.Create` 只建空壳，卡组里 0 张卡 ⇒ 第一版那条「画了 N 行」量到 0）
            // 🆕 **2026-10-05 改成【一副合法的 30 张卡组】**（原来只塞督军 + 3 张）。
            //   为什么：`Practice Deck` 那颗钮的 `interactable = DeckUtility.ValidateDeck(deck, …)`
            //   （原版 `DeckInfoControls__Initialize:201-207`；我们这一侧**同一份判据** = `RuleEngine.DeckRules.Validate`）
            //   只在**卡数正好 30** 时为真。3 张 ⇒ `DeckError.TooFewCards` ⇒ 那颗钮被闸门挡掉
            //   ⇒ 本文件 A10 尾巴那一整段（`dp.CurrentState` 变 `Closed` / `DeckSelectionPopup.LastOpened`
            //   / 隐藏卡那一支）**全是假红**（点了什么都不发生）。
            //   ⚠️ 「正好 30」的出处：原版 `GameStaticData.deckSize`，我们这一侧住在
            //   `GameplayVariables.Classic.deckSize`（判据入口 `DeckRules.CardCount(false)`）。
            //   写法与 `Editor/MainMenuScene.cs`（A65④① 的**阳性对照**那一副）**逐字一致**：
            //   同阵营 + 一张一名（⇒ 不碰⑦同名上限）+ 只取 `unit`（⇒ 不碰⑥督军混入、⑥b 效果卡）。
            {
                var lib = DeckLibrary.Load();
                if (lib.Decks.Count > 0)
                {
                    var pool = CardDatabase.Load();
                    var d0 = lib.Decks[0];
                    d0.CardIds.Clear();
                    // 阵营取卡池里 unit 最多的那个（实测 `RuleEngine/Resources/cards_engine.json`：Ultramarines **69** 张）。
                    // ⚠️ 这里是**写死的**、不现算「哪个阵营够 30」—— 那种现算等于把判据搬进自检。
                    // 真凑不满 30 张时下面那条 `Check` 会**红**（不静默变绿）。
                    const string LegalFaction = "Ultramarines";
                    foreach (var c in pool)
                        if (c.Type == "hero" && RuleEngine.DeckRules.SameFaction(c.Faction, LegalFaction))
                        { d0.WarlordId = c.Id; break; }
                    foreach (var c in pool)
                    {
                        if (d0.CardIds.Count >= 30) break;
                        if (c.Type != "unit") continue;
                        if (!RuleEngine.DeckRules.SameFaction(c.Faction, LegalFaction)) continue;
                        if (d0.CardIds.Contains(c.Id)) continue;   // 一张一名 ⇒ 不碰同名上限
                        d0.CardIds.Add(c.Id);
                    }
                    fixtureCards = d0.CardIds.Count;
                    lib.Save();
                }
            }
            CollectionData.ResetForTest();
            CollectionWindow.ResetStylesForTest();      // 异画那批卡的筛选状态（静态缓存）
            CardProgress.ResetForTest();                // 卡片详情窗的拥有数/等级（单机口径，静态缓存）
            Debug.Log(P + "=== 「收藏线」自检 开始 ===");

            // 🔴 **2026-10-05 新增**：夹具那一副（第 1 套）必须是**合法**的 30 张卡组。
            //   它是本文件 A10 尾巴整段的**前提**，不是可选项 ⇒ 必须钉住（缺了它那一段的
            //   「点 `Practice Deck` ⇒ 关自己 + 开选卡组窗」会**静默**变成「点了什么都不发生」）。
            //   期望值 **30** 出自原版 `GameStaticData.deckSize`（不是抄上面那个循环的上限）。
            Check(fixtureCards, 30,
                  "自检夹具：第 1 套 = **Ultramarines 督军 + 30 张同阵营不同名 unit**"
                  + "（原版 `GameStaticData.deckSize = 30`）—— A10 尾巴那一整段的阳性前提");

            var win = Build(out var root);

            // ---------------- 🆕 2026-10-20（`A1834`）：文字重排探针**排在它要观察的那一步之前**吗 ----------------
            //
            // 判据（原账）：`Shell/CollectionWindow.cs` 的 `TimeOneTextRelayout` 排在 `RebuildCardsCells`
            // **之后**（字形在建卡时就烘完、文本也没再变）⇒ 它量到的 `0.0 ms` 是**必然结果**，
            // 日志却拿它当「字体现烘那条假设不成立」的证据
            // （`资料/普查产出_第二十八会话/A1814_A1813_诊断.md` §A1813 ③）。
            // 修法 = 把 **before 快照**挪到变化之前（`BuildWatch.TextProbeBegin`）＋ 建完之后算 Δ（`TextProbeEnd`）。
            // 本条从**两个互相独立的维**断「位置对不对」：
            //   ① **源码次序**（静态，**灭自证**）：位置是源码里的事实，⛔ 不是被测量对象自己报的数 ——
            //      把 begin 挪回 `RebuildCardsCells` 之后 ⇒ 红；
            //   ② **运行时读数**（刚建完这扇窗）：`CardsAtBegin` / `BeginsWithCards` 必须为 **0**，before/after 成对。
            Section("A1834：文字重排探针的位置（静态源码次序）");
            {
                // 🔴 **2026-10-20（W58-UIFIX-A · 铁律 5 就地订正）**：这里原来还挂着 **5 条运行时读数**
                //   （`Begins >= 1` / `CardsAtBegin` / `BeginsWithCards` / 成对 / `UnclosedBegins`）——
                //   ⛔ 已搬到**第一次切到 Cards 页之后**（本文件「切页」那一节之后，`A1834（运行时读数）`）。
                //   **原因**：T09（2026-10-17）起四页**懒建** —— `CollectionWindow.Build()` 只建第 0 页（Deck），
                //   Cards 页要第一次 `tabButtons.Click(1)` 才 `Setup()`（`Shell/CollectionTabPage.cs:44-80`）；
                //   而这里在 `Create` 之后**立刻**读，那时 Cards 页还没建 ⇒ `Begins = 0` **恒红**
                //   （判据 → `普查产出_1020/W58_诊断_界面五宿主.md` §五 C1；日志侧证 = A1834 之前只有
                //   `BuildDeckList` 栈、没有 `BuildCardsPage` 栈）。
                //   ⚠️ 静态源码那 3 条**不依赖建页** ⇒ 留在原处（它们也在这里就地守着「别把 begin 挪到建卡之后」）。
                //   ⛔ 不要为了那条去在 `Create` 里强制建 Cards 页（那会推翻 T09 的首开性能修复）。
                // ① 静态：源码里那三句的**先后**（⛔ 不读任何我们自己算出来的量）
                const string cwPath = "Assets/CardPresentation/Shell/CollectionWindow.cs";
                string cwSrc = File.Exists(cwPath) ? File.ReadAllText(cwPath) : "";
                CheckTrue(cwSrc.Length > 0,
                          $"（前提·不静默）A1834：读得到 `{cwPath}` —— 读不到 ⇒ 下面三条**没跑**，不是绿");
                if (cwSrc.Length > 0)
                {
                    int iBegin = cwSrc.IndexOf("BuildWatch.TextProbeBegin(", System.StringComparison.Ordinal);
                    int iEnd = cwSrc.IndexOf("BuildWatch.TextProbeEnd(", System.StringComparison.Ordinal);
                    // ⚠️ 锚点带行首 12 个空格 —— 页面 Setup 里那一句；`:1040` 的
                    //   `OnChanged = () => RebuildCardsCells(holder);`（lambda）**不该**被当成变化点。
                    int iRebuild = cwSrc.IndexOf("\n            RebuildCardsCells(holder);", System.StringComparison.Ordinal);
                    CheckTrue(iBegin >= 0 && iRebuild >= 0 && iEnd >= 0,
                              $"★ A1834 · 三句都在（begin `{iBegin}` · RebuildCardsCells `{iRebuild}` · end `{iEnd}`）");
                    CheckTrue(iBegin >= 0 && iRebuild > iBegin,
                              "★ A1834 · **`TextProbeBegin` 排在 `RebuildCardsCells(holder);` 之前**（= 变化之前）"
                            + "｜🧨 挪回建卡之后 ⇒ 本条红（探针又会量到恒 0）");
                    CheckTrue(iEnd > iRebuild,
                              "★ A1834 · **`TextProbeEnd` 排在 `RebuildCardsCells(holder);` 之后**（Δ 才算得到）");
                }
            }

            // ---------------- 窗口参数（原版 MB `-6401214277658680619`）----------------
            Section("窗口参数（正本 §一；`Collection Menu Variant` 根 MB 原文）");
            Check(win.type, WindowType.Fullscreen, "`type` = **0 Fullscreen**（原文）");
            Check(win.placement, WindowsPlacement.Canvas,
                  "`windowsPlacement` = **5 Canvas**（⚠️ **商店是 10 World** —— 逐窗不同，别互推）");
            Check(win.closeOnEsc, true, "`closeOnESC` = **1**（⚠️ **奖励窗是 0**）");
            CheckNear(win.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（原文）");

            // ---------------- 外壳（与奖励窗/商店**同一个壳**）----------------
            Section("外壳：`Content Area` / 左栏 / 四页签（正本 §二）");
            var area = FindChild(root, "Content Area");
            CheckAt(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
            CheckRectPx(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");

            // ---------------- `Content Area/Shadow (1)`（🆕 2026-10-23 · A13 · 界面线第四批）----------------
            //  判据 = **解包资源逐窗现读**（⛔ 不是我们的常量）：
            //    `python -I -X utf8 工具/menu_rect.py bundle_menus_assets_all "Collection Menu Variant" --depth 3`
            //      ⇒ `Content Area/Shadow (1)  330.37  70.94  379.81  1080.00  INACT`（宽 49.43）
            //    `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 3 --no-sprite`
            //      ⇒ 组件 `Image,LayoutElement` · 色 `(0,0,0,.475)` · sprite `40k_main_tab_shadow` 51×4 Simple
            //        （与左栏那颗 `Tab Buttons/Shadow` **同一张图、同一个色**）。
            //  ⚠️ **竖向照旧从 70.94 起**（不跟本窗 `BarTop = 158.64` 走 —— 实测如此；
            //     算式与出处 → `Shell/MainMenuSubmenuWindow.Shadow1W` 的 doc · 精确宽 **49.432861328125**）。
            //  ⚠️ 这一件**出厂关着** ⇒ 量它必须带 `true`（`RectOf` / `CheckRectPx` 那一路只找激活链上的块）。
            //  🧨 改坏法：删掉 `MainMenuSubmenuWindow.BuildShell` 末尾那三行 ⇒ 第 1 条红；
            //             去掉 `SetActive(false)` ⇒ 第 2 条红（半透明压暗件，开出来会把右边一片压黑）；
            //             把 `Shadow1R` 跟着 `BarTop` 改成 158.64 ⇒ 第 3 条红。
            Section("`Content Area/Shadow (1)`（原版也有、**出厂 INACT**）");
            {
                var s1 = FindChild(area, "Shadow (1)");
                CheckTrue(s1 != null, "★ `Content Area/Shadow (1)` **建出来了**（原版有、我们原来整件不建）");
                CheckTrue(s1 != null && !s1.gameObject.activeSelf, "★ …而且**出厂关着**（原版 `m_IsActive = 0`）");
                CheckAt(s1, 330.374878f, 379.807739f, 70.94f, 1080f,
                        "★ `Shadow (1)` 在 **[330.3749,70.94]–[379.8077,1080.00]**（竖向**不**跟 `BarTop` 跑）");
                var s1rt = s1 as RectTransform;
                CheckNear(s1rt != null ? s1rt.rect.width : -1f, LayoutSpace.Px(MainMenuSubmenuWindow.Shadow1W), 0.001f,
                          "★ `Shadow (1)` 宽 = **49.4329**（⛔ 与左栏那颗 47.64 的 `Tab Buttons/Shadow` **不是同一件**）");
                var s1quads = s1 != null ? s1.GetComponentsInChildren<ImageQuad>(true) : new ImageQuad[0];
                Check(s1quads.Length, 1, "★ `Shadow (1)` 底下**恰好 1 块** `ImageQuad`（原版只挂一个 `Image`）");
                if (s1quads.Length > 0 && s1quads[0] != null && s1quads[0].Texture != null)
                {
                    CheckTrue(s1quads[0].Texture.name == MainMenuSubmenuWindow.ArtBarShadow,
                              $"★ …贴图 = **`{MainMenuSubmenuWindow.ArtBarShadow}`**（与左栏那颗同源；"
                            + $"现读「{s1quads[0].Texture.name}」）");
                    var s1t = s1quads[0].Tint;
                    CheckTrue(Mathf.Abs(s1t.r) < 0.002f && Mathf.Abs(s1t.g) < 0.002f && Mathf.Abs(s1t.b) < 0.002f
                              && Mathf.Abs(s1t.a - 0.4745098f) < 0.002f,
                              "★ …色 = **(0,0,0,0.4745)**（原版 `Image.m_Color` 实读）"
                            + $"（实得 {s1t.r:F4},{s1t.g:F4},{s1t.b:F4},{s1t.a:F4}）");
                }
                else
                {
                    CheckTrue(false, "★ `Shadow (1)` 底下那块 `ImageQuad`（或它的贴图）取不到"
                                   + " ⇒ 上面「贴图」「色」两条**等于没查**（如实红，不假装）");
                }
            }

            var bar = FindChild(root, "Tab Buttons");
            // 🔴 **2026-10-23（W62 · 底板）就地订正（铁律 5）**：本条原来断的是 **70.94..1080**（= `Content Area` 顶边）
            //    —— 那是**照着我们的实现**断的（自证：实现写 `ContentT`、断言也写 `ContentT`）。
            //    原版 `Collection Menu Variant` 的 `Tab Buttons` 是 **158.64..1080**（165 × **921.36**）；
            //    同族另三扇（奖励 / 商店 / 社交）才是 70.94..1080（逐窗现读 → `Shell/MainMenuSubmenuWindow.BarTop` 的 doc）。
            //    我们原来比原版**高 88 px**（底板渐变 1009.06 vs 921.36）。
            //    🧨 改坏法：`CollectionWindow.BarTop` 去掉覆写 ⇒ 退回 70.94 ⇒ 本条红。
            CheckAt(bar, 167.17f, 332.17f, 158.64f, 1080f,
                    "`Tab Buttons`（左栏 · 原版 **158.64..1080** —— ⛔ 不是 `Content Area` 的 70.94）");
            // ⚠️ **别在这里加 `CheckRectPx(bar, …)`** —— 它量的是子树里**第一个** `ImageQuad`，
            //    而 `Tab Buttons` 的第一个是 `Background/Band 0`（只是整块按色标切出来的一段）。
            //    「底板多高」那把尺在下面那条**三条带高之和**里（样板 = `Editor/RewardsScene.cs` 同一组三条）。

            // ---- 左栏底板：`40k_main_tab_background` + 原版 `Gradient2`（4 色标 ⇒ 3 条横带）----
            //  判据 = `工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 3 --no-sprite`
            //    ⇒ `Tab Buttons | 167.2 158.6 332.2 1080.0 | 165.00 921.36 | Image,…,Gradient2`。
            //  ⚠️ 三条带的**高之和**才是「底板多高」这把尺（单条带只是整块按色标切出来的一段）；
            //     样板 = `Editor/RewardsScene.cs` 的同一组三条（那边断 1009.06，本窗断 921.36）。
            if (bar != null)
            {
                var barBg = FindChild(bar, "Background");
                CheckTrue(barBg != null, "`Tab Buttons/Background`（左栏底板）建了");
                var barBands = barBg != null ? barBg.GetComponentsInChildren<ImageQuad>() : new ImageQuad[0];
                Check(barBands.Length, 3,
                      "左栏底图 = **3 条横带**（原版 `40k_main_tab_background` + `Gradient2` 4 色标）");
                float bandSum = 0f, bandW = 0f;
                for (int i = 0; i < barBands.Length; i++)
                    if (barBands[i] != null)
                    {
                        bandSum += barBands[i].WorldH * 108f;
                        bandW = Mathf.Max(bandW, barBands[i].WorldW * 108f);
                    }
                CheckNear(bandSum, 921.36f, 2.0f,
                          "★ W62·底板：左栏底图 3 条带**高之和** = 原版 **921.36**"
                        + "（158.64..1080）｜🧨 底板从 `ContentT` 起 ⇒ 1009.06 ⇒ 本条红");
                CheckNear(bandW, 165f, 2.0f, "左栏底图带**宽** = 165");
            }

            var keys = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("CollectionTabButton_")) keys.Add(t);
            // ⚠️ **本条是弱判据** —— `GetComponentsInChildren(true)` **含 inactive** ⇒ 某颗键被
            //    `TabButtons.Initialize` 关掉时，这里**照样数到 4**（「全绿而画面上少一个键」的来源）。
            //    ⇒ 「四颗**真在画面上**」那四条在下面 `Section("四页签：…")` 里逐颗另断
            //      （2026-10-11 · `A1498` 同根因补断言）。
            Check(keys.Count, 4, $"左栏**四个键**（实测 {keys.Count}；⚠️ **含 inactive** —— 只证「建出来了」，不证「看得见」）");

            // 🔴 左栏第一个键的顶边 = **188.64**（条从 158.64 起 + VLG padTop 30）
            //    —— 这是本窗与奖励窗/商店**唯一的外壳差别**（那两个是 70.94 + 120 = 190.94）
            if (keys.Count >= 4)
            {
                var want = LayoutSpace.RectCenter(167.17f, 188.64f, 332.17f, 368.64f);
                float d = Vector3.Distance(keys[0].position, want);
                CheckTrue(d <= 0.01f,
                          $"**第一个键的矩形** = 167.17,188.64→332.17,368.64（原版；差 {d * 108f:F2}px）"
                          + " —— 本条是「`BarTop` 覆写成 158.64 + `BarPadTop` = 原版 VLG `padTop 30`」的判据");
            }

            // ============================================================ 左栏 `Shadow`（W62 · 界面线）
            //  **病灶**（真机截图 `_tmp_view/shots/2026-10-22_第六十二会话/02_collection.png` 与
            //    裁图 `45_subnav_crop.png`）：主左栏与子导航列之间有一条**竖向浅白带**
            //    （实测设计 px x≈167..232 · y≈368..1079）。原版实拍同位置是**暗红/黑**（裁图 `46_ref_subnav.png`）。
            //  **根因**：`40k_main_tab_shadow` 这张图是**纯白 + alpha 渐变**（51×4、左 255 → 右 3），
            //    原版靠那颗 `Image.m_Color = (0,0,0,0.475)` 把它变成黑阴影；我们建它时**没给 tint**（= 白）。
            //  **判据**（原版 prefab 现读，⛔ 不是从实现抄）：
            //    `工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 5`
            //    ⇒ `Shadow | 167.2 158.6 214.8 1080.0 | Image | 40k_main_tab_shadow 51×4 | Simple (0,0,0,0.475)`。
            //  ⚠️ **本件在【原版里有】** ⇒ 处置是「照原版画」，⛔ 不是「删掉」（裁图 `46_ref_subnav.png`
            //    同一位置是**一条更暗的竖带**，只是不像白色那么刺眼）。
            Section("左栏 `Shadow`（W62：原版存在 ⇒ 照原版画成黑，⛔ 别删）");
            {
                var shadow = FindChild(bar, "Shadow");
                CheckTrue(shadow != null,
                          "★ W62①：`Content Area/Tab Buttons/Shadow` **存在**"
                        + "（原版这一件有 —— 处置是照它画，不是删）");
                if (shadow != null)
                {
                    CheckArt(shadow, "40k_main_tab_shadow",
                             "★ W62②：Shadow 的贴图 = 原版 `40k_main_tab_shadow`");
                    CheckTint(shadow, new Color(0f, 0f, 0f, 0.475f),
                              "★ W62③：Shadow 的色 = 原版 prefab 的 **Simple (0,0,0,0.475)**"
                            + "｜🧨 不给 tint（= 白）⇒ 屏上就是那条**浅白带** ⇒ 本条红");
                    CheckRectPx(shadow, 167.17f, 167.17f + 47.64f, 158.64f, 1080f,
                                "★ W62④：Shadow 的矩形 = 原版 167.17,158.64→214.81,1080（47.64×921.36）");
                    CheckAt(shadow, 167.17f, 167.17f + 47.64f, 158.64f, 1080f,
                            "★ W62④′：Shadow 落在原版矩形中心");
                }
            }

            // ---------------- 四页签：**卡面文案不是页节点名** ----------------
            Section("四页签：文案 / 切页（正本 §二；A1 表 `:17-22`）");
            // 🔴 **2026-10-11（`A1498`）就地改口径**：四键文案**从写死的英文改成 `Loc.T(键)`** ——
            //    键名 = 原版 `Localize.mTerm` 原文，**实现那一侧的出处 = `Shell/CollectionWindow.Buttons[i].Term`**。
            //    ⚠️ 不改这条 ⇒ 本批一跑必红 4 条，而且**红的是【断言过期】、不是实现错**（实现那边已随语档出中文）。
            // 🔴 **2026-10-11（`A1498` 同根因 · 本批）就地再改一次（铁律 5 续写）**：上一句原文写的是
            //    「**唯一出处** = `CollectionWindow.Buttons[i].Term`」—— 那是**实现那一侧**的出处；
            //    **断言这一侧必须把四颗键名【写死】**（下面这张 `tabTerms`）。**理由**：两边都从 `Buttons[i].Term`
            //    读 ⇒ **检测器与被测实现同一个口** ⇒ 把实现表里那条 `term:` 删掉时**两边一起变绿**（自证）。
            //    ⛔ **别把 `tabTerms` 改回 `CollectionWindow.Buttons[i].Term`。**
            //    四个串与原表逐位相同，只是**独立写出**（原版 `TabButtonLabel ▸ Localize.mTerm` 原文）。
            var tabTerms = new[]
            {
                "MainMenu/MainButtons/ButtonLabel/Deck",
                "MenuShop/ShopItemType/Cards",
                "MenuShop/ShopItemType/Cosmetics",
                "MenuShop/AlternateArtStyle",
            };
            var wantLabels = new string[4];
            for (int i = 0; i < 4; i++)
            {
                string term = tabTerms[i];
                // 🧨 **灭自证（原版字面量 vs 实现那张表）**：**期望值 = 上面写死的串**，被测的是实现表。
                //    ⛔ 这不是「读实现当期望值」—— 方向正好相反。**为什么非要有它**：**英文档下**
                //    `Loc.T(键)` 与 `spec.Label` 是**同一个串** ⇒ 下面那条文案断言**咬不住**「实现不再走词条」
                //    （把 `Buttons[i].Term` 删掉，屏上改画 `Label`，文案一字不变）⇒ 只有本条会红。
                //    🔴 **2026-10-11（`A1517`）补**：上面那个「只在中文档下咬得住」的缺口**已经补上了** ——
                //    **本文件收尾处**（`int total = …` 之前）有一段**语档无关**的真鉴别式：它只在那
                //    一小段里换语档 + `win.Build()` 重建，断「**四颗键的串跟着语档变**」+ 两趟分别
                //    等于中/英列。⇒ ⛔ 别在跑批前钉全局语档（实测会把 §空态/§Import/§Create 那一批
                //    按现行语档取期望值的断言一起弄成**假红**）。
                string gotTerm = i < CollectionWindow.Buttons.Length ? CollectionWindow.Buttons[i].Term : "<实现表短了>";
                CheckTrue(string.Equals(gotTerm, term, System.StringComparison.Ordinal),
                          $"★（A1498）左栏第 {i + 1} 键的**词条键名** = `{term}`"
                        + $"（原版 `Localize.mTerm` 原文；实现表 `CollectionWindow.Buttons[{i}].Term` 实测 `{gotTerm}`）");
                CheckTrue(Loc.HasEntry(term),
                          $"（前提）左栏第 {i + 1} 键的词条 `{term}` 在表里（⛔ 不在 ⇒ 实现与断言两边一起退化成键名）");
                wantLabels[i] = Loc.T(term).ToUpperInvariant();
            }
            // 🔴🔴 **2026-10-11（`A1498` 同根因 · 本批的主要交付）**：四颗键**逐颗**先断「**真在画面上**」。
            //    **为什么单开一档**：本文件上面那句 `Check(keys.Count, 4, …)`（以及 `FindChild` 自己）走的是
            //    `GetComponentsInChildren<Transform>(true)` —— **含 inactive** ⇒ 第 4 颗（`…_3`）被
            //    `TabButtons.Initialize` 关掉时**照样找得到、照样数到 4** ⇒ **全绿而画面上少一个键**。
            //    这正是「16k+ 断言全绿、实拍里只剩 3 个键」的来源（**凡断「该显示」的，都要同时断 `activeSelf`**）。
            //    **判据**（原版本窗 `TabButtons.tabButtonPrefab` = `m_PathID 0` ⇒ 末项**不是**母版、四颗全是真页签）
            //    → `Shell/MainMenuSubmenuWindow.cs` 的 `BuildBar` doc（⛔ 不在本文件再抄第二份，铁律 6）。
            //    🧨 **鉴别式**：把 `Shell/CollectionWindow.cs` 的 `BuildShell(…, lastIsMaster: false)` 那个实参
            //    删掉（= 回旧写法、末项当母版）⇒ **`…_3` 那条立刻红**（`activeSelf = false`）。
            //    ⚠️ **摆回基线**：本档只读**构建期**的 `SetActive`，与「当前选哪一页 / 高亮在哪」无关
            //    （`SetHighlight` 切的是 `Highlight` 子件，不是键本身）⇒ 本档紧跟建窗跑、进来时页签就是出厂态
            //    （`CollectionWindow.Build` 末尾 `tabButtons.Click(0)`）；后面那些切页小节的残留**不会漏到这里**。
            //    ⛔ **别退化成「数一数有几颗是激活的」** —— 那是弱判据（本仓踩过），要**逐颗按名字**钉。
            for (int i = 0; i < 4; i++)
            {
                // ⛔ **按【名字】取，不按 `keys[i]` 的遍历次序** —— 名字才能把「第几颗」钉死。
                var kb = FindChild(bar, "CollectionTabButton_" + i);
                CheckTrue(kb != null, $"（前提）左栏第 {i + 1} 键 `CollectionTabButton_{i}` 建出来了");
                CheckTrue(kb != null && kb.gameObject.activeSelf,
                          $"★ 左栏第 {i + 1} 键 `CollectionTabButton_{i}` **真在画面上**（`activeSelf = true`）"
                        + " —— 原版本窗 `TabButtons.tabButtonPrefab` 是 **null** ⇒ 末项**不是**母版、⛔ 别被关掉"
                        + "（`FindChild` / `GetComponentsInChildren(true)` **含 inactive** ⇒ 只数个数挡不住这个缺陷）");
                // ⚠️ 键上的字是 `Text`；**原版 `m_fontStyle = UpperCase`**（基类已 `ToUpperInvariant`）
                var lab = FindChild(kb, "Text");
                CheckText(TextOf(lab), wantLabels[i],
                          $"左栏第 {i + 1} 键的文案 = **{wantLabels[i]}**"
                          + (i == 2 ? "（⚠️ 页节点叫 `Cardback Tab`，**卡面印的是 Cosmetics**）" : "")
                          + (i == 3 ? "（⚠️ 页节点叫 `Alternate Art Tab`，**卡面印的是 Styles**）" : ""));
            }
            // 🆕 **2026-10-08（波 C3 · A212 主表 #31 验收）：左栏键文案的【渲染】断言**（四窗这一族原来一条都没有）。
            //   判据 = 原版四窗左栏键文案的 `m_TextWrappingMode` **一律 `0`（`NoWrap`）**—— 逐窗现读的四窗表
            //   只写一处：`Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton`（此处不抄第二份，铁律 6）。
            //   ⛔ **为什么必须量渲染、不能只比字号**：`SetAutoFitBox` 只把 `fontSizeMin/Max` 交出去，
            //   **装不装得下由 TMP 算** ⇒ 字号对而字冲出去，自检照样全绿（`AutoFitBox` 那条教训，2026-09-22 踩过）。
            //   框宽 **155** = 原版 `Tab Buttons/*/Label` 的 `sz=(155,37.86)`
            //   （⛔ 不读 `BuildTabButton` 的 `labW` —— 那也是被测实现里的数，读了就是自证）。
            //   **改坏法**：删掉 `MainMenuSubmenuWindow.BuildTabButton` 末句 `txt.SetWrapping(false)` ⇒
            //   `SetAutoFitBox` 开出来的 `Normal` 留着 ⇒ `折行=` 那条立刻红（0 → 1）。
            //   ⚠️ 本窗四颗键在**英文档**下是 `DECK`/`CARDS`/`COSMETICS`/`STYLES`（第 1 键原版 `m_text` 是**单数**
            //   `Deck`，2026-10-17 现读 prefab；中文档 = 卡组/张牌/美容品/风格）—— **都不带空格**
            //   ⇒ 「就一行」那条在这里的红法要 `auto` 缩不动才轮到（真正当场能红的是 `折行=` 那条）。
            {
                int tabN = 0;
                for (int i = 0; i < 4 && i < keys.Count; i++)
                {
                    var klb = FindChild(keys[i], "Text") != null
                        ? FindChild(keys[i], "Text").GetComponentInChildren<Label>() : null;
                    CheckTrue(klb != null, $"（左栏渲染断言 · 前提）第 {i + 1} 键的文案 `Label` 取得到");
                    if (klb == null) continue;
                    tabN++;
                    CheckTrue(klb.WrappingMode >= 0,
                              $"（左栏渲染断言 · 前提）第 {i + 1} 键 `{klb.Text}` 走的是 **TMP 后端**"
                            + "（`-1` = 点阵后端 ⇒ 下面两条渲染断言不成立，如实红、不假装）");
                    Check(klb.WrappingMode, 0, $"★ 第 {i + 1} 键 `{klb.Text}`：**`折行=0`**（原版四窗左栏键一律 0）");
                    Check(klb.LineCount, 1, $"★ …而且渲出来**就一行**（`Normal` 会把装不下的键名折行）");
                    // 🔴 **2026-10-13（A750）换口**：量法 `Label.WorldW`（= 缓存 `_tmpW`）→ **TMP 自己渲出来那块网格**
                    //    —— 与 `Editor/MainMenuScene.cs` 那几处（A709/A714/A715/A718）**同一条口径**；本条 = 这一族的
                    //    **第六处**（第四/五处 = `Editor/RewardsScene.cs` · `Editor/ShopScene.cs`，同一批改完）。
                    //    **为什么这是灭自证**：`Label.WorldW` 那份缓存**只有 `RefreshBounds()` 写**（`Battle/Label.cs`）——
                    //    也就是**被测实现自己**；而 `SetCharSpacing`（`CardPresentation/Battle/Label.cs`）· `SetFontSize`（`CardPresentation/Battle/Label.cs`）这一族
                    //    **只重排 mesh、不刷新缓存** ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**、这条照样绿。
                    //    新口读的是 TMP 自己的 `textBounds`，**不在实现那条链上**。
                    //    期望值 `155f` / 容差 `+ 0.5f` / 文案全文**一位未动** —— 变的只有「从哪个口读那个数」与缩进。
                    // ⚠️ **量不到 ⇒ 必须显式红**：`TmpRenderedRect` 失败时四个 out **全 0** ⇒ 让下面那句拿到 0 的话，
                    //    `0 ≤ 155.5` 会**假绿**。本条的判据句**没有** `> 0f` 那一半 ⇒ **不能**用 `-1f` 哨兵
                    //    （`-1 ≤ 155.5` 也恒真）—— 只能像 A715/A718 那样把原句包进 `else {}` + 补一条显式红。
                    // ⚠️ 量的是 **`klb.transform`**（上面那句 `FindChild(keys[i], "Text").GetComponentInChildren<Label>()`
                    //    **一字未动**；TMP 是 `Label` 的子件 —— `Battle/Label.cs` 的 `NewText` `TmpFont.NewText(transform, …)`）。
                    //    ⛔ 别改成 `TmpRenderedRect(keys[i], …)`：那会捞 `keys[i]` 子树里**第一颗 TMP（含 inactive）**，
                    //    与「只找激活」的 `Label` **未必是同一颗** ⇒ 会把「节点不在（红）」与「量到了别一颗（绿）」混成一档。
                    // **改坏法（只咬旧口）**：在 `Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton` 里那句 `SetWrapping(false)` 那句
                    //    `if (txt != null) txt.SetWrapping(false);` **之后**插一句 `if (txt != null) txt.SetCharSpacing(5f);`
                    //    ——（那句话是四窗左栏键刷**最后一次**缓存的地方：`SetWrapping` → 模式真的变了 → `ForceRelayout`
                    //    → `RefreshBounds()`，`Battle/Label.cs` 的 `RefreshBounds()`）⇒ 网格重排了、**缓存不动** ⇒ 两个口读到的数
                    //    **必然不同**。⚠️ 三件套里的第三件（新口红 / 旧口绿）**本地证不出来**，见下一条如实标。
                    // ⚠️ **如实标**：这四颗键都开着 **autosize**（`BuildTabButton` 的 `SetAutoFitBox`，
                    //    `enableAutoSizing = true`）⇒ TMP 重排时会把字号缩回去、渲出来的宽**仍 ≤ 框宽**
                    //    ⇒ 上面那个改坏法**不一定**把绿翻红（「两个口读到的数不一样」才是换口的全部意义）。
                    //    实测（`_tmp_view/collection.log` 那一次；⚠️ 那是**换口之前**的跑，四颗读的都是**旧口**）：
                    //    `DECKS` 87.3 · `CARDS` 89.0 · `COSMETICS` **153.5** · `STYLES` 96.5
                    //    ⚠️ **2026-10-17 补记（A1643 / A1498）**：第 1 键早已是 `Deck`（英文档 `DECK`），
                    //    且四颗都接了原版的 `字距 −3.5` + `Midline` ⇒ **上面那四个宽是历史读数**，
                    //    ⛔ 别拿它们当期望值（本段的判据只有 `≤ 155 + 0.5`）。
                    //    ⇒ 🔴 第 3 键 `COSMETICS` 余量只有 **2.0px**（十二颗键里最紧的一颗）——
                    //    **首跑重点看它**：红了先量实得值判「实现没对齐」还是「量法没生效（全 0 / 天文数字）」，
                    //    ⛔ **别动期望值 `155f`**。
                    {
                        float rx1, ry1, rx2, ry2;
                        if (!TmpRenderedRect(klb.transform, out rx1, out ry1, out rx2, out ry2))
                        {
                            CheckTrue(false, $"★ 第 {i + 1} 键 `{klb.Text}`（前提）这一颗 `Label` 底下没有 TMP 网格"
                                           + " ⇒ 「渲出来的宽」量不到（⛔ 不是实现把字冲出去了）");
                        }
                        else
                        {
                            float wTabPx = rx2 - rx1;
                            CheckTrue(wTabPx <= 155f + 0.5f,
                                      $"★ …而且**渲出来的宽 {wTabPx:F1} ≤ 框宽 155**"
                                    + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
                        }
                    }
                }
                Check(tabN, 4, "四颗键的文案都量到了（少于 4 ⇒ 上面那几条等于没查）");
            }
            var tabsRoot = FindChild(root, "Tabs");
            CheckTrue(FindChild(tabsRoot, "Select Deck Tab") != null, "页节点 `Select Deck Tab` 在（**不叫 `Deck Tab`**）");
            // 🔴 **2026-10-12（A288）新增**：第 2 页的节点名是 **`CardsTab`** —— 我们原来建的是
            //   `"Card Collection Tab"`（**把脚本类名 `CardCollectionTab` 当成节点名**了）。
            //   原版实据：`bundle_menus_assets_all/GameObject/CardsTab.json` 的 `m_Name = "CardsTab"`。
            //   ⚠️ **这一条是「按名字取」才有的检出能力**：`FindChild` 是 `GetComponentsInChildren` + `name ==`
            //   ⇒ 名字不对就是 `null` ⇒ 红（下面 `:2680` 那条在切页路径上再钉一次）。
            CheckTrue(FindChild(tabsRoot, "CardsTab") != null,
                      "页节点 **`CardsTab`** 在（⚠️ **不叫 `Card Collection Tab`** —— 那是脚本类名 `CardCollectionTab`，"
                    + "节点名只有这一页与前三个不同名；判据 = `…/GameObject/CardsTab.json` 的 `m_Name`）");
            CheckTrue(FindChild(tabsRoot, "Cardback Tab") != null, "页节点 `Cardback Tab` 在");
            CheckTrue(FindChild(tabsRoot, "Alternate Art Tab") != null, "页节点 `Alternate Art Tab` 在");
            // 🆕 **2026-10-09（A264）**：`Tabs` 底下还有一层 **`Shared`**（原版**只有收藏窗**有这一层），
            //   关闭钮「Back」连它下面的 `Button Text` 都装在这一层里面。
            //   🔴 **判据就是【层级】本身 ⇒ 只能按【路径】取**：`FindChild` 是 `GetComponentsInChildren` + `name ==`
            //      （**递归按名字找**），父链读错的那一版（`Close Button` 直接挂窗口根）**照样能找得到**
            //      ⇒ 拿它写这条等于**分不出「挪进 `Shared`」与「留在窗口根」两种状态**（弱断言）。
            //   🔴 **期望值全是原版字面量**（⛔ 不读 `CollectionWindow.CloseBtnL` 那些实现常量 —— 那是自证）：
            //     · 父链（**逐级现读 `m_Father`**，⛔ 不是 `menu_dump` 的缩进；A264 立项时那条「`Shared` 是
            //       `Content Area` 的**兄弟**」就是这么读错的）：
            //       `Collection Menu Variant` → `Content Area` → `Tabs` → `Shared` → `Close Button` → `Button Text`
            //     · `Shared` 那颗 RT 是**拉伸锚**（`a=(0,0)-(1,1)` · `sd=(0,0)` · `ap=(0,0)`）
            //       ⇒ 矩形 = 父件 `Tabs` 的矩形 = **167.17,70.94 → 1920.01,1080（1752.83 × 1009.06）**
            //     · 它的 GO（`bundle_menus_assets_all/GameObject/Shared.json`）`m_Component` **只有 1 项**（= 那颗 RT）
            //       ⇒ **纯容器节点**（没有 `ImageQuad`）⇒ 只量位置，⛔ 别用 `CheckRectPx`（那个要 `ImageQuad`）
            //     · `Close Button` 的原版矩形 = **192.17,83.44 → 342.17,143.44**（150×60）
            //   **改坏法**：① 不建 `Shared`、把 `Close Button` 挂回窗口根（A264 之前那一版）⇒ 下面三条路径**全取不到** ⇒ 红；
            //             ② 建了 `Shared` 但 `Close Button` 仍留在窗口根 ⇒ 第 2 条取不到 ⇒ 红。
            {
                var shared = root != null ? root.Find("Content Area/Tabs/Shared") : null;
                CheckTrue(shared != null,
                          "★ `Shared` 在 **`Tabs` 的直接子件**上（按路径 `Content Area/Tabs/Shared` 取得 —— "
                          + "`Transform.Find` 逐段只找直接子件；原版父链 = `Collection Menu Variant` → `Content Area` → `Tabs` → `Shared`）");
                var shClose = shared != null ? shared.Find("Close Button") : null;
                CheckTrue(shClose != null,
                          "★ `Close Button` 装在 **`Shared` 之下**（原版那颗 `Shared` 的 `m_Children` **只有它一个**）");
                CheckTrue(shClose != null && shClose.Find("Button Text") != null,
                          "★ …而 `Button Text` 又在 **`Close Button` 之下**（原版那条六级链的最末一节）");
                // `Shared` = **纯容器**（原版那颗 GO 上只挂着 RT 一个组件，没有 `Image` / 没有 `RectMask2D`）
                //   ⇒ 只量位置。「1752.83×1009.06」这条边由**拉伸锚 + 父件同矩形**推出来（我们不建图 ⇒ 量不到宽高）。
                CheckAt(shared, 167.17f, 1920.01f, 70.94f, 1080f,
                        "`Shared` 摆在原版那个矩形里（= 父件 `Tabs` 的矩形 167.17,70.94→1920.01,1080 · **1752.83×1009.06**）");
                // 🔴 本件最该防的回归：**换父链把世界矩形一起带偏了** —— `MenuWindowBase.Node/Rect` 走的是
                //   `Local(parent, …)`（世界 − 父的世界位置）⇒ 理论上逐位不变，这里把它钉住。
                CheckAt(shClose, 192.17f, 342.17f, 83.44f, 143.44f,
                        "★ `Close Button` **挪进 `Shared` 之后世界矩形没动**（原版 192.17,83.44→342.17,143.44 = 150×60）");
            }
            Check(win.CurrentTab, WindowTabType.CollectionDecks, "开窗默认落在第 1 页（Decks）");

            // ============================================================ 🆕 **2026-10-17（`A1642`）**
            //   Deck 页的**容器层级**（原版逐级现读；⛔ 期望值全是**原版字面量**，不读本窗任何常量）
            //   原版树（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Select Deck Tab" --depth 4`）：
            //     `Select Deck Tab`(167.2,70.9→1920,1080)
            //       ├ `Decks Tab`(同矩形)
            //       │  └ `Collection Display`(同矩形)
            //       │     ├ `Deck Scroll View`(330.9,155.9→1920,1080)
            //       │     │  └ `Empty Collection Warning`(165.9,70.9→1970,1080)
            //       │     └ `Deck Filters`(0.1,155.9→335.6,1080)
            //       └ **`Header`(167.2,70.9→1920.0,155.9)** ← **最后一个孩子**
            //   🔴 **每一格钉一样东西**（三档改坏法各红各的）：
            //     · 层级那五条用 **`Transform.Find`（只找直接子件）** 取 ⇒ 少建一层 / 挂错父 ⇒ 取不到 ⇒ 红
            //       （⚠️ 别的格用的 `FindChild` 是**递归**的 ⇒ 光靠它分不出「挂 `page`」与「挂 `Collection Display`」）；
            //     · `Header` 的矩形 = 中心（`CheckAt`）+ `sizeDelta` 宽高（两轴独立）；
            //     · `Header` 是最后一个孩子 = `GetSiblingIndex() == childCount − 1`。
            Section("Deck 页：容器层级（A1642 · 原版树形）");
            {
                var page0 = win.PageRoot(0);
                var decksTab = page0 != null ? page0.Find("Decks Tab") : null;
                CheckTrue(decksTab != null,
                          "★ A1642：`Decks Tab` 是 `Select Deck Tab` 的**直接孩子**（原版 `SelectDecksTab.deckCollectionTab` 就挂它）");
                var disp = decksTab != null ? decksTab.Find("Collection Display") : null;
                CheckTrue(disp != null,
                          "★ A1642：`Collection Display` 是 `Decks Tab` 的**直接孩子**（原版 `DeckCollectionTab.display` 指它）");
                var deckSv = disp != null ? disp.Find("Deck Scroll View") : null;
                CheckTrue(deckSv != null,
                          "★ A1642：`Deck Scroll View` 是 `Collection Display` 的**直接孩子**"
                        + "（我们原来直接挂 `page` ⇒ 本格红；🧨 另外 `page.Find(\"Deck Scroll View\")` 那三处也因此取不到 ⇒ 已改 `FindDeep`）");
                var deckFlt = disp != null ? disp.Find("Deck Filters") : null;
                CheckTrue(deckFlt != null,
                          "★ A1642：`Deck Filters` 是 `Collection Display` 的**直接孩子**（原版 0.1,155.9→335.6,1080）");
                var deckEw = deckSv != null ? deckSv.Find("Empty Collection Warning") : null;
                CheckTrue(deckEw != null,
                          "★ A1642：`Empty Collection Warning` 在 **`Deck Scroll View` 之下**（原版父链逐级现读；我们原来挂 `page`）"
                        + "｜🧨 退回挂 `page` ⇒ **只本格红**（`FindChild` 是递归的 ⇒ 别处按名字量它的那些格仍然绿）"
                        + "｜⚠️ 它现在是 `Deck Scroll View` 的常驻孩子 ⇒ `RebuildDeckCells` 那个「留 Viewport、其余全删」的循环已同步放过它");
                var hdr = page0 != null ? page0.Find("Header") : null;
                CheckTrue(hdr != null,
                          "★ A1642：`Header` 是 `Select Deck Tab` 的**直接孩子**（我们原来**根本没有这一层** —— 页头件直接建在 `page` 上）");
                if (hdr != null && page0 != null)
                {
                    CheckAt(hdr, 167.2f, 1920.0f, 70.9f, 155.9f,
                            "`Header` 摆在原版矩形里（**167.2,70.9 → 1920.0,155.9** = 1752.83 × 85.00）");
                    var hrt = hdr as RectTransform;
                    CheckNear(hrt != null ? hrt.sizeDelta.x * 108f : -1f, 1752.83f, 0.5f,
                              "★ A1642：`Header` 的**宽 = 1752.83**（原版字面量 = 1920.0 − 167.2）");
                    CheckNear(hrt != null ? hrt.sizeDelta.y * 108f : -1f, 85.0f, 0.5f,
                              "★ A1642：`Header` 的**高 = 85.00**（原版字面量 = 155.9 − 70.9）");
                    Check(hdr.GetSiblingIndex(), page0.childCount - 1,
                          "★ A1642：`Header` 是 `Select Deck Tab` 的**【最后一个】孩子**"
                        + "（原版 `m_Children` 序 = `Decks Tab` → `Select Game Mode Deck Tab` → `Header`；"
                        + "🆕 2026-10-22（W64）中间那一项**已建**，见下面「选择游戏模式」那一节）"
                        + "｜🧨 删掉 `BuildDeckList` 末尾那句 `SetAsLastSibling()` ⇒ 它停在**第一个** ⇒ 本格红");
                }
            }

            // ============================================================ 🆕 **2026-10-17（`A1644` 定位后收口）**
            //   原位是「遍历 Deck 页 ImageQuad 打日志」的诊断探针 —— **它定位不到本件**（只遍历 `PageRoot(0)`，
            //   而真正的件在**外壳**里；筛条件 `x1∈[330,333]` 与真值 `x1=−762.5` 也不符）⇒ 删除探针、换成断言。
            //   定位结论（静态闭合，D5）：那块暗渐变 = 外壳左竖导航的投影
            //   `Navigation Panel/Panel Shadow`（队列 **2910**、α 0.4667），**不是本窗的件**。
            //   原版兄弟序里 `Navigation Panel` 在窗口 Holder **之前** ⇒ 窗画在它上面；
            //   我们把窗口底图压到 2000（为躲 `CardView` 的 3000）⇒ 反被这条影子盖住。
            //   修法 = 底图队列 **2940**（∈ (2910, 3000)）。本断言咬住这条区间。
            {
                var bgNode = win.transform.Find("Content Area/Background");
                CheckTrue(bgNode != null, "`Content Area/Background` 底图节点存在");
                if (bgNode != null)
                {
                    int qn = 0, bad = 0;
                    foreach (var q in bgNode.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (q == null) continue;
                        qn++;
                        int rq = q.RenderQueue;
                        if (rq <= 2910 || rq >= 3000) bad++;
                    }
                    CheckTrue(qn > 0, $"底图下有 {qn} 块 `ImageQuad`（⛔ 一颗都没有 = 底图没建）");
                    // 🧨 改回 2000（或任何 ≤2910）⇒ 红；改到 ≥3000 ⇒ 卡池会被底图盖住。
                    Check(bad, 0, "★ A1644：底图每一块的渲染队列都 ∈ (2910, 3000)"
                                + "（> 外壳 `Navigation Panel/Panel Shadow` 的 2910、< `CardView` 的 3000）");
                }
            }

            // ============================================================ 🆕 **2026-10-22（W64 · 卡组双模式）**
            //   「选择游戏模式」子页（原版 `Select Game Mode Deck Tab`）。
            //   判据 = `资料/普查产出_1022/W64_卡组双模式_原版规格.md`（下文 §x = 那一份的节号）＋
            //          `…/W64_卡组双模式_资产落点.md`（「落点 §x」）。
            //   ⛔ 期望值一律**原版字面量**（prefab 字段 / 参考图 / 官方词条）—— 不读本窗常量（否则自证）。
            //   ⚠️ 本段跑完把状态**还原**（滚动偏移、夹具里临时改过的 `GameMode`、`PendingNewDeck` / `PendingReturn`），
            //      ⛔ 别把后面的断言带进「筛选着」的状态。
            Section("Deck 页：「选择游戏模式」页（W64 · 505.93×802.67 两张卡 · 原版预制体）");
            {
                var page0m = win.PageRoot(0);
                var decksTabNode = page0m != null ? page0m.Find("Decks Tab") : null;
                var modePage = page0m != null ? page0m.Find("Select Game Mode Deck Tab") : null;
                CheckTrue(modePage != null,
                          "★ W64：`Select Game Mode Deck Tab` 是 `Select Deck Tab` 的直接孩子（原版第二子页，§1·2）");
                Check(modePage != null ? modePage.GetSiblingIndex() : -1, 1,
                      "★ …且排在**第 2 位**（原版 `m_Children` 序 = `Decks Tab` → 模式页 → `Header`，§1·2）"
                    + "｜🧨 删掉 `BuildDeckList` 里那句 `SetSiblingIndex(1)` ⇒ 它停在 `Header` 之后 ⇒ 本格红");

                // ---- ① 进 Deck 页**先出模式页**（§5·1：事件数 ≥2 那一支；离线等价物）----
                CheckTrue(win.DeckModePageShown,
                          "★ ① 进 Deck 页**先显示模式页**（原版 `ChooseTabToOpen`：`IOwnDeckPlayEvent` ≥2 ⇒ 先出这一页）"
                        + "｜🧨 把 `BuildDeckList` 末尾那句 `SetDeckModeChrome(_deckModeFilter < 0)` 换成 `(false)` ⇒ 本格红");
                CheckTrue(decksTabNode != null && !decksTabNode.gameObject.activeSelf,
                          "★ …而卡组列表 `Decks Tab` **整颗关着**（§5·3 `ToggleSelectGameModeDeck` 第一句 `CloseTab`）");

                // ---- ② 标题：rect / 字号 / 颜色（§2·2）----
                var ttlNode = modePage != null ? modePage.Find("Title Header") : null;
                CheckAt(ttlNode, 426.07f, 1824.73f, 155.94f, 243.50f,
                        "`Title Header` 摆在原版矩形（426.07,155.94 → 1824.73,243.50，§1·3）");
                // 🔴 **2026-10-22（W64 修）：`Label` 的节点是裸 `Transform`（不是 `RectTransform`）**
                //   ⇒ `as RectTransform` 恒 null（首轮实测读到 **-1.00**）。框尺寸只能从 **TMP 自己的
                //   `rectTransform`** 读（`SetWrapWidth` / `SetAutoFitBox` 写的就是它）。
                var ttlTmp = ttlNode != null ? ttlNode.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
                CheckNear(ttlTmp != null ? ttlTmp.rectTransform.sizeDelta.x * 108f : -1f, 1398.65f, 0.5f,
                          "★ 标题宽 = **1398.65**（原版字面量，§1·3；⚠️ 不是按锚点算的，是手填值 §八·12）");
                CheckNear(ttlTmp != null ? ttlTmp.rectTransform.sizeDelta.y * 108f : -1f, 87.56f, 0.5f,
                          "★ 标题高 = **87.56**");
                var ttlLb = ttlNode != null ? ttlNode.GetComponent<Label>() : null;
                CheckTrue(ttlLb != null, "（前提）标题是一颗 `Label`（TMP 后端）");
                if (ttlLb != null)
                {
                    CheckText(ttlLb.Text, Loc.T("MenuDeck/SelectDeckGameMode"),
                              "★ 文案 = 官方词条 **`MenuDeck/SelectDeckGameMode`**（中「选择游戏模式」/ 英 `SELECT GAME MODE`）"
                            + "｜🧨 照 prefab 的占位串 `SELECT GAME MODE DECK` 写死 ⇒ 本格红");
                    CheckNear(ttlLb.color.r * 255f, 254f, 1f, "★ 标题 R = **254**（`#FE9900` = preset `Pragati Regular Orange`，§2·2）");
                    CheckNear(ttlLb.color.g * 255f, 153f, 1f, "★ 标题 G = **153**");
                    CheckNear(ttlLb.color.b * 255f, 0f, 1f,
                              "★ 标题 B = **0**（🧨 照 TMP 自己的白色 `m_fontColor` 写 ⇒ 三条颜色全红）");
                }
                // 四格 = `m_enableAutoSizing 1` / `min 18` / `max 72` / `base 36`（§4·1）＋ 折行 `1`
                CheckA1212(modePage, "Title Header", "Select Game Mode Deck Tab ▸ Title Header",
                           18f, 72f, 36f, 1, 1398.65f, 87.56f);

                // ---- ③ Scroll View / 两张卡（§1·3 / §3·4）----
                var sv = modePage != null ? modePage.Find("Scroll View") : null;
                CheckAt(sv, 330.79f, 1920.01f, 260.90f, 1063.20f,
                        "`Scroll View` 摆在原版矩形（1589.21 × 802.30，§1·3）");
                var vpN = sv != null ? sv.Find("Viewport") : null;
                CheckTrue(vpN != null && vpN.GetComponent<ViewportClip>() != null,
                          "★ `Viewport` 挂的是 **`RectMask2D` 的等价物**（`ViewportClip`）—— 原版这一颗**没有 Image**、"
                        + "不是 Mask+sprite（§1·5 / §4）");
                var contentN = vpN != null ? vpN.Find("Content") : null;
                Check(contentN != null ? contentN.childCount : -1, 2,
                      "★ ③ `Content` 底下**两张**模式卡（原版张数 = liveops 事件数，§3·1 ⇒ 离线等价固定 2）");
                if (contentN != null && contentN.childCount >= 2)
                {
                    var c0 = contentN.GetChild(0);
                    var c1 = contentN.GetChild(1);
                    CheckText(c0.name, "Game mode deck selector", "卡节点名照原版 `Game mode deck selector`");
                    var r0 = c0 as RectTransform;
                    var r1 = c1 as RectTransform;
                    CheckNear(r0 != null ? r0.sizeDelta.x * 108f : -1f, 505.9268f, 0.4f,
                              "★ 卡宽 = **505.9268**（`Game mode deck selector` prefab 原值，§3·2）");
                    CheckNear(r0 != null ? r0.sizeDelta.y * 108f : -1f, 802.6741f, 0.4f, "★ 卡高 = **802.6741**");
                    CheckNear(r1 != null ? r1.sizeDelta.x * 108f : -1f, 505.9268f, 0.4f, "右卡宽 = 505.9268");
                    CheckNear(r1 != null ? r1.sizeDelta.y * 108f : -1f, 802.6741f, 0.4f, "右卡高 = 802.6741");
                    float cx0 = MenuCheck.DesignPx(c0).x, cx1 = MenuCheck.DesignPx(c1).x;
                    CheckNear(cx0, 594.4732f + 505.9268f * 0.5f, 0.6f,
                              "左卡中心 x = **847.44**（左沿 594.47 + 卡宽/2，§3·4）");
                    CheckNear(cx1, 1150.40f + 505.9268f * 0.5f, 0.6f,
                              "右卡中心 x = **1403.36**（右卡左沿 1150.40，§3·4）");
                    CheckNear(cx1 - cx0, 505.9268f + 50f, 0.5f,
                              "★ HLG `m_Spacing` = **50**（两卡中心距 = 卡宽 505.93 + 50；§3·4）"
                            + "｜🧨 改成 0 ⇒ 两卡贴在一起、本格红");
                    CheckNear((cx0 - 505.9268f * 0.5f + cx1 + 505.9268f * 0.5f) * 0.5f, 1125.40f, 0.6f,
                              "★ 两卡**整体居中**在视口里（内容宽 1061.86 = 2×505.93+50；§3·4）"
                            + "｜🧨 从 Content 左边起排 ⇒ 中心变 941.15 ⇒ 本格红");
                    CheckNear(MenuCheck.DesignPx(c0).y, 662.05f, 0.6f,
                              "卡中心 y = **662.05**（Content 802 高的中腰：261.05 + 401，§1·3）");

                    // ---- ④ `Border`：九宫 13 / ppuMul 5 / fillCenter 0 / 灰 0.33962（§3·3）----
                    var bd = c0.Find("Border");
                    CheckTrue(bd != null, "左卡上有 `Border`");
                    if (bd != null)
                    {
                        CheckArt(bd, "40k_square_border", "★ `Border` 的图 = **`40k_square_border`**（§3·3）");
                        var bq = bd.GetComponentsInChildren<ImageQuad>(true);
                        Check(bq.Length, 8,
                              "★ `m_FillCenter = 0` ⇒ 只画 **8 块**（中间那块不画）"
                            + "｜🧨 改 `fillCenter: true` ⇒ 9 块（那张图中心不透明 = 整卡变灰板）");
                        float minW = 1e9f, minH = 1e9f;
                        foreach (var q in bq)
                        {
                            if (q == null) continue;
                            minW = Mathf.Min(minW, q.WorldW * 108f);
                            minH = Mathf.Min(minH, q.WorldH * 108f);
                        }
                        CheckNear(minW, 2.6f, 0.3f,
                                  "★ 角块宽 = **2.6px** = `m_Border` 13 ÷ `m_PixelsPerUnitMultiplier` **5**（§3·3）"
                                + "｜🧨 把 ppuMul 当**乘**（13×5 = 65）⇒ 一圈 65px 灰边，本格红（主菜单 2026-10-11 踩过）");
                        CheckNear(minH, 2.6f, 0.3f, "★ 角块高 = 2.6px");
                        CheckTint(bd, new Color(0.33962262f, 0.33962262f, 0.33962262f, 1f),
                                  "★ 边框色 = **0.33962262** 灰（§3·3）");
                    }

                    // ---- 卡图 / 暗带 / 卡名（落点 §1·1 · §4·2 · §3·3）----
                    var art0 = c0.Find("Background Image");
                    CheckArt(art0, "40k_main_GameMode_Skirmish",
                             "★ 左卡卡图 = **`40k_main_GameMode_Skirmish`**（小冲突；落点 §1·1 与参考图逐像素一致）");
                    CheckArt(c1.Find("Background Image"), "Container_Image_Ranked",
                             "★ 右卡卡图 = **`Container_Image_Ranked`**（经典；⚠️ 我们文件名是下划线）");
                    var aq = art0 != null ? art0.GetComponent<ImageQuad>() : null;
                    if (aq != null)
                    {
                        CheckNear(aq.WorldW * 108f, 505.9268f, 1f,
                                  "卡图**铺满卡面**（803 方图被卡根的 `RectMask2D` 裁到卡宽，§3·2）");
                        CheckNear(aq.UvRect.width, 505.9268f / 803.084f, 0.01f,
                                  "★ 卡图 UV 窗宽 = 卡宽 ÷ 803.08 方图 = **0.630**（= 取中间那条竖带；我们没有"
                                + "像素级掩码 ⇒ 用 UV 裁等价复现，落点 §六）");
                        CheckNear(aq.UvRect.x, (1f - aq.UvRect.width) * 0.5f, 0.01f,
                                  "…且**居中**（u 两边各切 ≈0.185）｜🧨 拉满 (0,0,1,1) 会把 1024² 压扁成 505×802 ⇒ 本格红");
                    }
                    var bandN = c0.Find("TextDarkening");
                    CheckTrue(bandN != null, "左卡上有 `TextDarkening`");
                    if (bandN != null)
                    {
                        CheckTint(bandN, new Color(0f, 0f, 0f, 0.6117647f),
                                  "★ 暗带 = 纯黑 **a = 0.6117647**（§3·3）");
                        var bandQ = bandN.GetComponentInChildren<ImageQuad>(true);
                        CheckNear(bandQ != null ? bandQ.WorldH * 108f : -1f, 105.8657f, 0.5f,
                                  "★ 暗带高 = **105.8657**（贴底，§3·2）");
                    }
                    // `Event Title`：fs 58.8 · auto 18–72 · base 36 · 折行 1 · 框高 55.708（§3·3 / 落点 §4·2）
                    CheckA1212(c0, "Event Title", "Game mode deck selector ▸ TextDarkening ▸ Event Title",
                               18f, 72f, 36f, 1, -1f, 55.708f);
                    var etN = FindDeep(c0, "Event Title");
                    var etLb = etN != null ? etN.GetComponent<Label>() : null;
                    CheckTrue(etLb != null, "（前提）卡名是一颗 `Label`");
                    if (etLb != null)
                    {
                        CheckText(etLb.Text, Loc.T("MainMenu/GameMode/Skirmish"),
                                  "★ 左卡卡名 = 官方词条 `MainMenu/GameMode/Skirmish`（小冲突）");
                        CheckNear(etLb.color.r * 255f, 255f, 1f,
                                  "★ 卡名是**白字**（preset `Pragati Thick Outline White`，落点 §4·2）");
                    }
                    var et1Lb = c1 != null && FindDeep(c1, "Event Title") != null
                                    ? FindDeep(c1, "Event Title").GetComponent<Label>() : null;
                    CheckText(et1Lb != null ? et1Lb.Text : null, Loc.T("MainMenu/GameMode/Classic"),
                              "★ 右卡卡名 = `MainMenu/GameMode/Classic`（经典）");

                    // ---- ⑦ 显隐矩阵（§5·3）：模式页时 **Import 在**、Create/Unlock/Filter Toggle/Filters **不在** ----
                    var hdrM = page0m != null ? page0m.Find("Header") : null;
                    var ctrlM = hdrM != null ? hdrM.Find("Control Buttons") : null;
                    var createM = ctrlM != null ? ctrlM.Find("Create") : null;
                    var importM = ctrlM != null ? ctrlM.Find("Import") : null;
                    var unlockM = ctrlM != null ? ctrlM.Find("Unlock") : null;
                    var fltTglM = hdrM != null ? hdrM.Find("Filter Toggle") : null;
                    var fltGrpM = hdrM != null ? hdrM.Find("Filters") : null;
                    CheckTrue(importM != null && importM.gameObject.activeSelf,
                              "★ ⑦ 模式页上 **`Import` 显示着**（§5·3；参考图右上角「导入卡组」就是它）"
                            + "｜🧨 把它一起关掉 ⇒ 本格红（§七 订正过的那一格）");
                    CheckTrue(createM != null && !createM.gameObject.activeSelf, "★ ⑦ …而 `Create` 藏着（§5·3）");
                    CheckTrue(unlockM != null && !unlockM.gameObject.activeSelf, "★ ⑦ …`Unlock` 藏着");
                    CheckTrue(fltTglM != null && !fltTglM.gameObject.activeSelf, "★ ⑦ …页头 `Filter Toggle` 藏着");
                    CheckTrue(fltGrpM != null && !fltGrpM.gameObject.activeSelf, "★ ⑦ …`Filters` 筛选格藏着");
                    CheckTrue(hdrM != null && hdrM.gameObject.activeSelf, "…`Header` 本身**不动**（仍显示）");
                    CheckTrue(hdrM != null && hdrM.Find("Separator Line") != null
                              && hdrM.Find("Separator Line").gameObject.activeSelf,
                              "…`Separator Line` 也**不动**（§5·3 最后一行）");
                }

                // ---- ⑤ 点模式卡 ⇒ 列表按 `GameMode` 筛（§5·5）；用夹具里**临时改过的一副**做正例 ----
                float savedOffM = win.DeckScroll != null ? win.DeckScroll.Offset : 0f;
                if (win.DeckScroll != null) win.DeckScroll.SetOffset(0f);
                int nDecks = CollectionData.DeckCount();
                int flipIdx = nDecks - 1;
                int flipSaved = flipIdx >= 0 ? CollectionData.Raw(flipIdx).GameMode : 0;
                string flipName = flipIdx >= 0 ? CollectionData.DeckAt(flipIdx).Name : null;
                var keepNewM = CollectionData.PendingNewDeck;
                var keepRetM = CollectionData.PendingReturn;
                try
                {
                    if (flipIdx >= 0) CollectionData.Raw(flipIdx).GameMode = (int)GameMode.Skirmish;

                    win.ChooseDeckMode((int)GameMode.Skirmish);
                    Check(win.DeckModeFilter, (int)GameMode.Skirmish,
                          "★ ⑤ 点「小冲突」⇒ `_deckModeFilter` = **13**（原版 `PlayModes.Skirmish`）");
                    CheckTrue(!win.DeckModePageShown, "★ …模式页关掉");
                    CheckTrue(decksTabNode != null && decksTabNode.gameObject.activeSelf, "★ …卡组列表打开");
                    Check(win.DeckCellCount, 1,
                          "★ ⑤ 列表**只剩这一模式**（夹具里只有 1 套 `GameMode=13`）"
                        + "｜🧨 把 `FilteredDeckIndices` 里那句模式筛删掉 ⇒ 变成 " + nDecks + " ⇒ 本格红");
                    CheckText(win.DeckCellCount == 1 ? TextOf(FindChild(win.DeckCells[0], "Deck Name")) : null,
                              flipName, "★ …剩下的正是那一套（`" + flipName + "`）");

                    win.ChooseDeckMode((int)GameMode.Classic);
                    Check(win.DeckModeFilter, (int)GameMode.Classic,
                          "★ ⑤ 点「经典」⇒ `_deckModeFilter` = **0**（原版 `PlayModes.Classic`）");
                    Check(win.DeckCellCount, nDecks - 1,
                          "★ …列表只剩经典的 " + (nDecks - 1) + " 套（那一套小冲突被筛掉）");

                    // 列表态下页头件要**回来**（§5·3 第二列）
                    var ctrlM2 = page0m != null ? page0m.Find("Header") : null;
                    var createM2 = ctrlM2 != null ? ctrlM2.Find("Control Buttons") : null;
                    CheckTrue(createM2 != null && createM2.Find("Create") != null
                              && createM2.Find("Create").gameObject.activeSelf, "★ 回列表 ⇒ `Create` 回来了");
                    CheckTrue(ctrlM2 != null && ctrlM2.Find("Filter Toggle") != null
                              && ctrlM2.Find("Filter Toggle").gameObject.activeSelf,
                              "★ …`Filter Toggle` 回来了");
                    CheckTrue(ctrlM2 != null && ctrlM2.Find("Import") == null
                              && createM2 != null && createM2.Find("Import") != null
                              && createM2.Find("Import").gameObject.activeSelf,
                              "★ …`Import` 一直显示（两态都在）");

                    // ---- ⑥ 新建卡组**继承当前模式**（§5·5·6：`deck.gameMode = (PlayModes)currentEvent`）----
                    win.ChooseDeckMode((int)GameMode.Skirmish);
                    int beforeM = CollectionData.DeckCount();
                    win.CreateDeck();
                    var pend = CollectionData.PendingNewDeck;
                    CheckTrue(pend != null, "（前提）`CreateDeck` 交出了一份 `PendingNewDeck`");
                    Check(pend != null ? pend.GameMode : -99, (int)GameMode.Skirmish,
                          "★ ⑥ 新建的卡组**继承当前模式 13**（原版 `SelectDecksTab.CreateDeck` 打 `currentEvent` 的模式）"
                        + "｜🧨 退回 `CollectionData.CreateDeck()` 不传模式 ⇒ 恒 0 ⇒ 本格红");
                    Check(CollectionData.DeckCount(), beforeM,
                          "★ …且**不入库**（原版 `GetEmptyDeck` → 直接开编辑窗；入库在编辑器 `Done` 那一刻，§5·5·6）");

                    // 反例：还没选模式（`-1`）时退回经典 —— 与上面成对（灭「恒 13」那半边）
                    win.ClearDeckModeFilter();
                    Check(win.DeckModeFilter, -1, "★ ⑥ 反例：`ClearDeckModeFilter` ⇒ 不筛（`-1` = 原版 `currentEvent == null`）");
                    win.CreateDeck();
                    Check(CollectionData.PendingNewDeck != null ? CollectionData.PendingNewDeck.GameMode : -99,
                          (int)GameMode.Classic,
                          "★ ⑥ 反例：没选模式时新建 ⇒ 经典 0（`-1` 的兜底；🧨 把兜底写成 13 ⇒ 本格红）");
                }
                finally
                {
                    if (flipIdx >= 0) CollectionData.Raw(flipIdx).GameMode = flipSaved;   // 还原夹具
                    win.ClearDeckModeFilter();                                            // 还原成「不筛」
                    CollectionData.PendingNewDeck = keepNewM;
                    CollectionData.PendingReturn = keepRetM;
                }
                Check(win.DeckCellCount, nDecks,
                      "★ 还原：清掉模式筛后列表回到全部 " + nDecks + " 套（本段后面的断言不吃这段的状态）");
                // ⚠️ 滚动偏移**最后**还原（先还原它会把「视口外的格不建」那一条带回来 ⇒ 上面那格的读数就跟着偏移走）
                if (win.DeckScroll != null) win.DeckScroll.SetOffset(savedOffM);

                // 🔴 **本段之后的所有「卡组列表」断言都要求 `Decks Tab` 是开着的** ——
                //   原版里列表**只能在点过一张模式卡之后**出现（`ChooseTabToOpen` 的两条分支，§5·1）
                //   ⇒ 收尾显式保证一次（幂等：第 ⑤ 步已经打开过，这里不会改任何可见状态）。
                win.ChooseDeckMode((int)GameMode.Classic);
                win.ClearDeckModeFilter();
                CheckTrue(!win.DeckModePageShown && decksTabNode != null && decksTabNode.gameObject.activeSelf,
                          "（收尾）列表态：模式页关、`Decks Tab` 开 —— 下面那些列表断言的前提");
            }

            // ============================================================ 🆕 **2026-10-22（W64 返回链 · D2）**
            //   「选过模式之后回不去模式页」：改前 `ShowDeckModePage()` / `ClearDeckModeFilter()` **零生产调用点**
            //   （只有自检调），而收藏窗被 `Hide` 之后**复用同一实例**（`_deckModeFilter` 留着）
            //   ⇒ 选过一次模式，这扇窗就永远只筛那个模式。
            //   判据（第一权威 · 逐句读反编译，⛔ 不是猜）：
            //     · `CollectionScreen__BackButtonClick.c` —— 按**窗口页签对象的类型**分派：是 `SelectDecksTab`
            //       ⇒ 交给 `SelectDecksTab.BackButtonClicked`；否则落 vtable `+0x1e8`（关窗/上一层）。
            //     · `SelectDecksTab__BackButtonClicked.c` —— `if (this[+0x60] != this[+0x58])`
            //       （子页签 ≠ 模式页）⇒ `this[+0x60] = null` + `ChooseTabToOpen(0)`（= 回模式页）；否则关窗。
            //   🔴 本段用**真 Back 钮**（窗根 `CloseHit` 那颗 `WindowButton`）驱动，⛔ 不直调 `ShowDeckModePage()`
            //   —— 直调只能证明「那个方法能跑」，证明不了「钮接上了」（本段要防的正是那个缺口）。
            Section("Deck 页：Back 钮的返回链（W64 · 原版 `SelectDecksTab.BackButtonClicked`）");
            {
                // 直接子件：`Shell/CollectionWindow.cs` 里是 `AddHit(transform, "CloseHit", …)`
                // （⚠️ 别用递归的 `FindChild` —— 后面几节开着的弹窗里也有同名的 `CloseHit`）。
                var backHit = win.transform.Find("CloseHit");
                var backWb = backHit != null ? backHit.GetComponent<WindowButton>() : null;
                CheckTrue(backWb != null,
                          "（前提）收藏窗那颗 Back 钮（窗根 `CloseHit` · `WindowButton`）取得到"
                        + " —— 取不到 ⇒ 下面几条**没跑**，不是绿");
                if (backWb != null)
                {
                    var pgB = win.PageRoot(0);
                    var modePgB = pgB != null ? pgB.Find("Select Game Mode Deck Tab") : null;
                    var decksNdB = pgB != null ? pgB.Find("Decks Tab") : null;
                    var modeCardsB = modePgB != null ? modePgB.Find("Scroll View/Viewport/Content") : null;

                    float savedOffB = win.DeckScroll != null ? win.DeckScroll.Offset : 0f;
                    if (win.DeckScroll != null) win.DeckScroll.SetOffset(0f);
                    int nAllB = CollectionData.DeckCount();
                    int lastB = nAllB - 1;
                    int lastSavedB = lastB >= 0 ? CollectionData.Raw(lastB).GameMode : 0;
                    try
                    {
                        // 夹具：把最后一套临时改成「小冲突」（同一套手法见上面 W64 第 ⑤ 步；退出时还原）
                        if (lastB >= 0) CollectionData.Raw(lastB).GameMode = (int)GameMode.Skirmish;

                        // ---- ① 先选一个模式 ⇒ 列表态 ----
                        win.ChooseDeckMode((int)GameMode.Skirmish);
                        Check(win.DeckModeFilter, (int)GameMode.Skirmish, "★ W64-B ① 选「小冲突」⇒ 列表筛 13");
                        Check(win.DeckCellCount, 1, "★ W64-B ① …只剩那一套（夹具里只有它 `GameMode=13`）");
                        // 谓词（唯一判据）在这一档必须为真：原版 ① 页签是 `SelectDecksTab` ② 子页签 ≠ 模式页
                        CheckTrue(win.ShouldReturnToDeckModePage(),
                                  "★ W64-B ① 列表态 ⇒ `ShouldReturnToDeckModePage()` **为真**");

                        // ---- ② 按**真 Back 钮** ⇒ 回模式页（原版第一支）----
                        backWb.Click();
                        Check(win.DeckModeFilter, -1,
                              "★ W64-B ② Back ⇒ 模式筛清空（`-1` = 原版那句 `this[+0x60] = null` 的可观测等价物）"
                            + "｜🧨 把 `OnBackClicked()` 里的 `ShowDeckModePage()` 删掉（或整段改回 `Close()`）⇒ 本格红");
                        // 🔴 **灭自证**：只断「模式页可见」挡不住「两半都开着」⇒ 同时断列表**关着**；
                        //   也⛔ 不能把「窗被关掉」当成通过 ⇒ 第三条单独钉「窗还开着」。
                        CheckTrue(modePgB != null && modePgB.gameObject.activeSelf,
                                  "★ W64-B ② …**模式页可见**（原版 `ChooseTabToOpen(0)` ⇒ `ToggleSelectGameModeDeck`）"
                                + "｜🧨 `CloseHit` 的 onClick 改回 `() => Close()` ⇒ 窗被关、本格红");
                        CheckTrue(decksNdB != null && !decksNdB.gameObject.activeSelf,
                                  "★ W64-B ② …而卡组列表 `Decks Tab` **关着**（§5·3 显隐矩阵）：两半**互斥**");
                        CheckTrue(win.gameObject.activeSelf,
                                  "★ W64-B ② …窗本身**还开着**（这一支是「回上一层子页」，⛔ 不是关窗）");

                        // ---- ③ 反例：已经在模式页 ⇒ 谓词为假（Back 该落「关窗」那一支）----
                        //   ⛔ 这里**不真点**：点了就关窗，本文件后面的断言会全跑在一扇关着的窗上。
                        CheckTrue(!win.ShouldReturnToDeckModePage(),
                                  "★ W64-B ③ 反例：模式页上 ⇒ `ShouldReturnToDeckModePage()` **为假**"
                                + "（原版 `this[+0x60] == this[+0x58]` ⇒ 落 vtable `+0x1e8`）"
                                + "｜🧨 把谓词写成恒真 ⇒ 本格红（那样模式页上按 Back 永远关不了窗）");

                        // ---- ④ 回模式页之后再点**另一张**模式卡 ⇒ 列表按**新模式**筛 ----
                        //   这一条灭的是「模式页只是亮着、其实点不动」那种假修法。
                        var c1B = modeCardsB != null && modeCardsB.childCount >= 2 ? modeCardsB.GetChild(1) : null;
                        var c1Wb = c1B != null && c1B.Find("Hit") != null
                                       ? c1B.Find("Hit").GetComponent<WindowButton>() : null;
                        CheckTrue(c1Wb != null, "（前提）右卡（经典）的命中区 `Hit` 取得到");
                        if (c1Wb != null) c1Wb.Click();      // 右卡 = `MainMenu/GameMode/Classic` ⇒ mode 0
                        Check(win.DeckModeFilter, (int)GameMode.Classic,
                              "★ W64-B ④ 回模式页后再点「经典」⇒ 筛 **0**（`ChooseDeckMode` 仍是唯一入口）");
                        Check(win.DeckCellCount, nAllB - 1,
                              "★ W64-B ④ …列表按**新模式**筛：经典 " + (nAllB - 1) + " 套（那套小冲突被筛掉）"
                            + "｜🧨 若模式页回来后命中区还压着 / `ChooseDeckMode` 只改状态不重筛 ⇒ 本格红");
                        CheckTrue(modePgB != null && !modePgB.gameObject.activeSelf,
                                  "★ W64-B ④ …模式页又关掉（回到列表态）");
                    }
                    finally
                    {
                        if (lastB >= 0) CollectionData.Raw(lastB).GameMode = lastSavedB;   // 还原夹具
                        win.ClearDeckModeFilter();
                    }
                    Check(win.DeckCellCount, nAllB, "★ W64-B（收尾）还原：清掉模式筛 ⇒ 列表回到全部 " + nAllB + " 套");
                    // ⚠️ 滚动偏移**最后**还原（与上面 W64 段同一条纪律：先还原它会把「视口外的格不建」带回来）
                    if (win.DeckScroll != null) win.DeckScroll.SetOffset(savedOffB);
                }
            }

            // ============================================================ 🆕 **2026-10-25（执行代理 `R17`）**
            //   Deck 页「卡组拖拽重排」（`R13` 施工）的 **6 条断言**。
            //   判据（⛔ **不重查**，逐条出处都是 R13 已核过的反汇编/资产）：
            //     `资料/完全复刻/报告/R13_收藏拖拽重排_第2轮.md` §三（施工序）+ §四（建议的断言与改坏法）：
            //       · `CollectionDisplay.ReorderCollectionByHierarchy` RVA `0x15ED470`（Remove + Insert，下标取**移动前**）
            //       · `DeckCollectionDisplay.SaveDeckOrder` RVA `0x1806E0000`（两张表等长才存）+ `OnDisable` RVA `0x1806DFFB0`
            //       · `CollectionDisplay.SetCell` RVA `0x1815EEB64`（`allowItemDrag` ∧ 两表等长 ⇒ `Toggle(true)`）
            //   🔴 **本轮不跑它**（用户 2026-10-25 定「暂停自检」）⇒ 这群是**只写不落跑**；
            //      每条都带「🧨 改坏法」，且 §⑥ 把「哪一句删掉会让哪一条红」列清（灭自证）。
            Section("Deck 页：卡组拖拽重排（R13 施工 · R17 断言）");
            {
                float savedOffD = win.DeckScroll != null ? win.DeckScroll.Offset : 0f;
                if (win.DeckScroll != null) win.DeckScroll.SetOffset(0f);
                win.ClearDeckFilters();
                win.ClearDeckModeFilter();
                win.RebuildDeckListNow();
                int nBaseD = CollectionData.DeckCount();
                var keepOrderD = new List<string>(nBaseD);
                for (int i = 0; i < nBaseD; i++)
                {
                    var d = CollectionData.Raw(i);
                    keepOrderD.Add(d != null ? (d.Name ?? "") : "");
                }
                var keepPopD = CollectionWindow.LastOpened;      // 拖拽**不该**开 `Deck info Popup`
                int flipD = nBaseD - 1;
                int flipModeSaved = flipD >= 0 ? CollectionData.Raw(flipD).GameMode : 0;
                try
                {
                    // ---- ① 无筛选 ⇒ **每格一条**拖拽登记（原版 `SetCell` 的 `Toggle(true)`）----
                    //   🧨 改坏法：把 `AttachDeckDrag` 里那句 `PointerLayer.RegisterDrag(...)` 删掉
                    //      ⇒ 本格从「= 格数」变成 0（拖拽根本不会发生，松手什么都不做）。
                    PointerLayer.ClearDrags();                       // 先清干净（别的夹子可能留着死条目）
                    win.RebuildDeckListNow();                        // 再建一趟 ⇒ 表里只剩本页的登记
                    CheckTrue(win.DeckCellCount > 0,
                              $"（前提）Deck 页建出了 {win.DeckCellCount} 格（视口外的格不建 ⇒ 这个数 ≤ {nBaseD}）");
                    Check(PointerLayer.DragCountForTest, win.DeckCellCount,
                          "★ ① Deck 页：**每一格都登记成可拖件**（无名字/阵营筛选时）"
                        + "｜🧨 删掉 `AttachDeckDrag` 里的 `PointerLayer.RegisterDrag(...)` ⇒ 实得 0");

                    // ---- ② 有「名字 / 阵营」筛选 ⇒ **一格都不可拖**（原版两张表长度不等 ⇒ `Toggle(false)`）----
                    //   🧨 改坏法：`DeckDragAllowed()` 改成恒 `true` ⇒ 本格从 0 变成 > 0。
                    string facD = CollectionData.DeckAt(0).Faction;
                    CheckTrue(!string.IsNullOrEmpty(facD), "（前提）第 1 套卡组推得出阵营（筛选用）");
                    win.ToggleDeckFacFilter(facD);
                    CheckTrue(win.DeckCellCount < nBaseD,
                              $"（前提·本条的判别力）筛「{facD}」之后**格子数真的少了**"
                            + $"（{nBaseD} → {win.DeckCellCount}）—— 否则两张表等长，这条断不出东西");
                    Check(PointerLayer.DragCountForTest, 0,
                          "★ ② 有「名字/阵营」筛选 ⇒ **拖拽全关**（原版 `collection.Count != filteredCollection.Count`）"
                        + "｜🧨 `DeckDragAllowed()` 末尾改成恒 `true` ⇒ 实得 > 0");
                    win.ClearDeckFilters();
                    win.RebuildDeckListNow();
                    Check(PointerLayer.DragCountForTest, win.DeckCellCount,
                          "★ ② …清掉筛选 ⇒ 登记**全回来**（与 ① 同值；两态对比 ⇒ 写死恒开/恒关都过不了）");

                    // ---- ③ **模式筛不算筛选** ⇒ 照样能拖（R13 §一·1 那条自查）----
                    //   🧨 改坏法：把 `DeckDragAllowed` 的比较基准换回 `CollectionData.DeckCount()`（= 全库）
                    //      ⇒ 模式筛之后两边不等 ⇒ 实得 0、本格红（= 「默认流程里拖拽永远关着」那个原缺陷）。
                    if (flipD >= 0) CollectionData.Raw(flipD).GameMode = (int)GameMode.Skirmish;
                    win.ChooseDeckMode((int)GameMode.Classic);
                    CheckTrue(win.DeckCellCount > 0 && win.DeckCellCount < nBaseD,
                              $"（前提·本条的判别力）模式筛之后格子 **{win.DeckCellCount}** 个、全库 {nBaseD} 套"
                            + " ⇒ 两张表**数不同**，能分辨「模式筛算不算筛选」");
                    Check(PointerLayer.DragCountForTest, win.DeckCellCount,
                          "★ ③ 模式筛**不算**「有筛选」⇒ 格子照样能拖（基准 = `BaseDeckIndices()`，只过模式筛）"
                        + "｜🧨 基准换回 `CollectionData.DeckCount()` ⇒ 实得 0");
                    win.ClearDeckModeFilter();
                    win.RebuildDeckListNow();

                    // ---- ④ 重排：内存序 + **从盘上重读一份**（证明真落盘，不是只改内存）----
                    //   🧨 改坏法：把 `CollectionData.ReorderDecks` 改成 `return true`（不动库）⇒ 前两条红；
                    //      把 `DeckLibrary.UpdateOrder` 尾句 `return Save()` 改成 `return true` ⇒ 只有「盘上」那两条红。
                    if (nBaseD >= 2)
                    {
                        string d0n = CollectionData.DeckAt(0).Name, d1n = CollectionData.DeckAt(1).Name;
                        CheckTrue(win.DeckMoveForTest(0, 1), "（前提）`DeckMoveForTest(0,1)` 报「写进去了」");
                        CheckText(CollectionData.DeckAt(0).Name, d1n,
                                  "★ ④ 拖第 1 格到第 2 格 ⇒ **内存序**：第 1 格变成原第 2 套");
                        CheckText(CollectionData.DeckAt(1).Name, d0n, "★ ④ …第 2 格变成原第 1 套（Remove + Insert，不是交换）");
                        // ⚠️ 必须是**新读一份**（`DeckLibrary.Load()` 每次 new），⛔ 别读 `CollectionData` 的缓存（那是自证）
                        var reloadedD = DeckLibrary.Load();
                        CheckText(reloadedD != null && reloadedD.Decks.Count > 1 ? reloadedD.Decks[0].Name : null, d1n,
                                  "★ ④ **盘上**第 1 套也是原第 2 套 ⇒ 真的落盘了（原版 `DeckInventory.UpdateOrder`）"
                                + "｜🧨 `UpdateOrder` 尾句改成 `return true`（不存盘）⇒ 本格红");
                        CheckText(reloadedD != null && reloadedD.Decks.Count > 1 ? reloadedD.Decks[1].Name : null, d0n,
                                  "★ ④ …盘上第 2 套 = 原第 1 套（盘上两份一起看，挡「只换了第一条」）");
                        CheckTrue(win.DeckMoveForTest(1, 0), "（还原）`DeckMoveForTest(1,0)` 报成功");
                        CheckText(CollectionData.DeckAt(0).Name, d0n, "★ ④ 还原：第 1 格回到原第 1 套");
                    }
                    else CheckTrue(false, "（前提）夹具至少要有 2 套卡组才验得了重排");

                    // ---- ⑤ 真落点链（`DeckDragDrop`：最近格 + 落盘 + 重建）+ **拖完不开窗** ----
                    //   🧨 改坏法：删掉 `DeckDragDrop` 里 `if (wasLive && to >= 0 && to != from) MoveDeckInOrder(from, to);`
                    //      ⇒ 格子跟手、松手顺序不变 ⇒ 前两条红；
                    //      在 `DeckDragDrop` 末尾补一句 `SelectDeck(to)` ⇒ 最后那条「不开窗」红。
                    win.RebuildDeckListNow();
                    string e0n = CollectionData.DeckAt(0).Name, e1n = CollectionData.DeckAt(1).Name;
                    CheckTrue(win.DeckDragDropForTest(0, 1),
                              "★ ⑤ `DeckDragDropForTest(0,1)` 跑完了（起拖 → 摆到目标格中心 → 真落点 + 重建）");
                    CheckText(CollectionData.DeckAt(0).Name, e1n,
                              "★ ⑤ 落点算法（`DeckDropTargetPos` 取最近格）把第 1 格放到第 2 格的位置上");
                    CheckText(CollectionData.DeckAt(1).Name, e0n, "★ ⑤ …原第 1 套挪到第 2 格");
                    CheckTrue(CollectionWindow.LastOpened == keepPopD,
                              "★ ⑤ 拖动后的松手**不该开 `Deck info Popup`**（原版 `OnEndDragEvent` 走的是重排，不是 `Select`）"
                            + "｜🧨 `DeckDragDrop` 末尾补一句 `SelectDeck(to)` ⇒ 本格红");
                    win.DeckMoveForTest(1, 0);                 // 还原
                    CheckText(CollectionData.DeckAt(0).Name, e0n, "★ ⑤ 还原：第 1 格回到原第 1 套");

                    // ---- ⑥ 灭自证（结构上不可能两边一起满足）----
                    //   上面 ②③ 两态对比 + ④「内存 / 盘上」两份对照已经互锁；这里再钉一条**次序**：
                    //   「清掉筛选之后格子数回到全库」且「登记数 = 格子数」—— 若有人把闸写成恒开
                    //   （② 会红）或恒关（① 会红），这两条**不可能同时绿**。
                    win.RebuildDeckListNow();
                    Check(nBaseD, CollectionData.DeckCount(),
                          "★ ⑥ 灭自证：清掉全部筛选后**库仍是 " + nBaseD + " 套**（重排/筛选没偷偷丢掉卡组）");
                    Check(PointerLayer.DragCountForTest, win.DeckCellCount,
                          "★ ⑥ 灭自证：闸写恒关 ⇒ ①红；写恒开 ⇒ ②红 ⇒ 这一对**不可能同时绿**");
                }
                finally
                {
                    if (flipD >= 0) CollectionData.Raw(flipD).GameMode = flipModeSaved;   // 还原夹具
                    win.ClearDeckModeFilter();
                    win.ClearDeckFilters();
                    // 🔴 顺序**无条件**还原（⛔ 不靠「再拖回去」—— ④ 半路失败就再也回不来了），
                    //   而且走的是**生产那条口**（`CollectionData.ReorderDecks`）。
                    CollectionData.ReorderDecks(keepOrderD);
                    win.RebuildDeckListNow();
                    // ⚠️ 滚动偏移**最后**还原（同 W64 段那条纪律：先还原它会把「视口外的格不建」带回来）
                    if (win.DeckScroll != null) win.DeckScroll.SetOffset(savedOffD);
                }
                CheckText(CollectionData.DeckAt(0).Name, keepOrderD.Count > 0 ? keepOrderD[0] : null,
                          "★（收尾）卡组顺序回到进场前那一份（后面的段子拿第 1 套当「30 张那副」用）");
            }

            // ---------------- Deck 页：视口 / 格 / 6 列 ----------------
            Section("Deck 页：卡组列表的几何（正本 §三；A2 实测）");
            var listHolder = FindChild(tabsRoot, "Deck Scroll View");
            CheckAt(listHolder, 330.9f, 1920f, 155.9f, 1080f, "`Deck Scroll View`（视口）");
            int n = CollectionData.DeckCount();
            CheckTrue(n > 0, $"卡组列表里有 {n} 套卡组（自检用的临时存档）");

            var cells = win.DeckCells;
            CheckTrue(cells.Count > 0, $"画出了 {cells.Count} 格（视口外的不建 = `RectMask2D` 那套裁切）");
            if (cells.Count > 0)
            {
                var c0 = cells[0];
                // 🔴 **2026-10-12 就地订正（铁律 5）：第 1 格中心 x 从 `453.4` 改成 `512.95`。**
                //   原文那 453.4 是按「视口左边 330.9 + padL 10 + 225/2」算的 —— **那个 `padL=10` 是死值**
                //   （`Content` 的 `GridLayoutGroup` `m_Enabled=0`），真正落位的是 `RecyclableScrollRect`
                //   的**整体居中**：`offset = ((W − (spacingX+cellW)*segments) + spacingX) * 0.5`
                //   （`PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c` 逐句）
                //   = `(1589.1 − 1470 + 20)/2` = **69.55** ⇒ 第 1 格左沿 **400.45**、中心 **512.95**。
                //   旁证（用户实拍 `桌面/warpforge参考图/收藏—卡组的参考图.png`）：把实拍按卡框宽 221.4
                //   归一后量到的第 1 格左沿 = **400.4 ± 3**（旧那套给 340.9，差 59.6px）。
                CheckNear(MenuCheck.DesignPx(c0).x, 512.95f, 0.6f,
                          "第 1 格中心 x = **512.95**（视口左边 330.9 + RSR 整体居中 69.55 + 225/2）");
                CheckNear(MenuCheck.DesignPx(c0).y, 338.15f, 0.6f,
                          "第 1 格中心 y = **338.15**（视口顶 155.9 + 364.5/2）");
                var frame = FindChild(c0, "Frame");
                CheckNear(Wpx(frame), 246f * 0.9f, 2f, "格内卡框宽 = **221.4**（原版 246 × 显示比例 0.9）");
                CheckNear(Hpx(frame), 368f * 0.9f, 2f, "格内卡框高 = **331.2**（原版 368 × 0.9）");
                CheckArt(frame, "40K_bt_deck", "卡框的图 = `40K_bt_deck`（原版）");
                CheckTrue(!string.IsNullOrEmpty(TextOf(FindChild(c0, "Deck Name"))), "格上有卡组名");
                // 🔴 **2026-10-12**：本行原来写 `340.9 + 5×245 + 225 = 1790.9`（`340.9` = 视口沿 + 死的 padL 10）
                //   ⇒ 与上一格同一条订正。居中后的第 6 格右边界 = `400.45 + 5×245 + 225` = **1850.45**。
                // 6 列：第 6 格右边界 = 400.45 + 5×245 + 225 = 1850.45 ≤ 1920
                if (cells.Count >= 6)
                    CheckNear(MenuCheck.DesignPx(cells[5]).x + DeckCellWpx() / 2f, 1850.45f, 0.6f,
                              "第 **6** 格右边界 = **1850.45** ≤ 1920（**这是「6 列」的判据** —— 7 列要 1972.95）");
            }

            // ============================================================ 🆕 **2026-10-12（本会话 · 用户实拍逐屏比）**
            //   —— 「收藏 → 卡组」这一页的两条**新版面判据**（改前都是偏离，见各自的注释）：
            //   ① 网格的**列方向原点 = 整体居中**（不是 `padL 10`）；② 页头 `Filters` 文案**左对齐**（不是居中）。
            {
                // ① 网格居中量：判据 = 反编译 `VerticalRecyclingSystem.CreateCellPool` 那条
                //    `offset = ((W − (spacingX+cellW)*segments) + spacingX) * 0.5` ⇒ (1589.1−1470+20)/2 = 69.55。
                //    ⛔ 别拿 `Content` 那颗 `GridLayoutGroup.m_Padding.left = 10` 来算（组件 `m_Enabled=0` = 死值）。
                CheckNear(CollectionWindow.DeckGridOffsetX, 69.55f, 0.1f,
                          "★ 卡组网格的列方向原点 = **69.55**（RSR 整体居中；不是 padL 10）");
                var dcell0 = FindChild(win.PageRoot(0), "CollectionDeck_0");
                if (dcell0 != null)
                    CheckNear(MenuCheck.DesignPx(dcell0).x - 225f / 2f, 400.45f, 0.6f,
                              "★ 第 1 格**左沿 = 400.45**（视口左 330.9 + 69.55；实拍实测 400.4±3，"
                              + "旧的 `padL 10` 给 340.9 = 差 59.6px）");

                // ② 页头 `Filters` 文案左对齐：判据①（prefab，四页逐颗现读）`Filter Toggle ▸ label`
                //    的 `m_HorizontalAlignment = Left`（`menu_dump.py … "<页节点>" --depth 4` 印作 `对齐=Left/Middle`）；
                //    判据②（用户实拍 `收藏—卡组的参考图.png`）「过滤器」三字的**左缘 = 443.5**（框左沿 437.2 +
                //    fs42 的左留白 ~6px），而我们**居中**那一版左缘在 **462.3**（= 框心 512.2 − 文字宽一半）。
                //    ⇒ 期望 443.5 ± 8：居中那一版差 **18.8** ⇒ 必红（不是容差内的量）。
                var fltLbl = FindChild(win.PageRoot(0), "Filters Label");
                float fx1, fy1, fx2, fy2;
                if (fltLbl == null || !TmpRenderedRect(fltLbl, out fx1, out fy1, out fx2, out fy2))
                    CheckTrue(false, "（前提·不静默）Deck 页 `Filters Label` 与它渲出来的矩形量得到 —— 上面那条的前提");
                else
                    CheckNear(fx1, 443.5f, 8f,
                              "★ 页头 `Filters` 文案**左缘 = 443.5**（原版 `label` 是 `Left` 对齐；"
                              + "我们原来是居中 ⇒ 462.3）");

                // 🆕 **2026-10-17（`A1212` 块 3 #7）**：`Filters Label` 的四格 + 框。
                //   判据 = 原版 `… ▸ Header ▸ Filter Toggle ▸ label`（四页逐值相同）：
                //   `m_fontSizeMin/Max/Base = 10/42/32` · `m_TextWrappingMode = 0` · 有效框 150 × 50。
                CheckA1212(win.PageRoot(0), "Filters Label", "…/Header ▸ Filter Toggle ▸ label",
                           10f, 42f, 32f, 0, boxW: 150f, boxH: -1f);
            }

            // 出处：`menu_rect.py bundle_menus_assets_all "Deck Filters" --depth 6`（直读）。
            {
                var fh = FindChild(win.PageRoot(0), "FiltersHit");
                var fw = fh != null ? fh.GetComponent<WindowButton>() : null;
                CheckTrue(fw != null, "`Filters` 圆钮**有命中区**了（原来没有 ⇒ 玩家点它没反应）");
                var dflt = FindChild(win.PageRoot(0), "Deck Filters");
                CheckTrue(dflt != null, "左抽屉 `Deck Filters` 建了（原版这一页有抽屉）");
                if (dflt != null)
                {
                    // 🔴 **2026-10-04 订正（X3 审查的 R3）：这条位置断言原来是在「收着」的时候量的**，
                    //   A11 之后「收起」= 整栏**真的滑出去** —— 原版 `SetupFilters` 就是这么干的：
                    //   记下 `originalAnchorPosition` 之后立刻 `anchoredPosition = (hiddenPosition.x, …)`
                    //   （`资料/卡组编辑界面_查证_0920.md:433-434` 的 VA 反汇编）⇒ **关着时它已经不在原位**。
                    //   改法：**两态各量一条**（关着 = 真的滑出屏幕 · 开着 = 停在原版矩形），⛔ 不是把断言改软。
                    CheckNear(CollectionWindow.DeckFltView.W, 335.50f, 0.6f, "抽屉宽 **335.50**（原版）");
                    // 🆕 2026-10-05（A101）升格：这条**不再是我们挑的** —— 原版 `Deck Filters` 的 `m_IsActive`
                    //   实读 = **`T`**（`资料/普查产出_1005/块8_卡组窗断言与异画页查证.md` 的实读表 Deck 那一行），
                    //   起手停在哪一头由 `SetupFilters` 定（`0x1815F0740` 收尾 = `anchoredPosition =
                    //   (hiddenPosition.x, originalAnchorPosition.y)`）⇒ **起手收起**，与 Cards 页**同一套**。
                    //   🔴 `act = T` **只等于「节点启用」**，⛔ 别读成「出厂展开」（`资料/已知的坑.md` 2026-10-05 那节）。
                    CheckTrue(!dflt.gameObject.activeSelf,
                              "起手收起（**照原版**：`SetupFilters` 把抽屉摆到 `hiddenPosition` 那一头）");
                    CheckNear(win.DrawerSlide(3), 0f, 0.001f,
                              "🆕 A11：Deck 页这一列也走**同一套滑动**，收起 = 进度 **0**（不只在原位隐身）");
                    float dCx0 = MenuCheck.DesignPx(dflt).x;
                    CheckNear(dCx0, 167.81f - 385f, 1.5f,
                              "…收起 = 按**原版行程 −385px** 滑出去（`hiddenPosition.x − originalAnchorPosition.x`"
                              + " = −550 −(−165)；原位中心 167.81 → −217.19）—— 有洞的实现在这里就红");
                    CheckTrue(dCx0 + CollectionWindow.DeckFltView.W * 0.5f < 0f,
                              "★ …而且整栏**真的在屏幕左外**（右缘 "
                              + (dCx0 + CollectionWindow.DeckFltView.W * 0.5f).ToString("F1")
                              + " < 0）—— 这才叫「收起」");
                    // ★ **R4 的判据**：关着时那一列的命中区必须**已经是关的**。
                    //   ⚠️ 量的是 `WindowButton.enabled` 的**真值**、不是我们自己那个布尔（那个是自证）——
                    //   真命中路 `PointerLayer.CollectHits` 只挑 `isActiveAndEnabled`（`Shell/PointerLayer.cs` 的 `isActiveAndEnabled`）。
                    //   原来 Deck 这一份**不走** `RebuildFilterRows`（格子自己 `AddHit` 建）⇒ 少了那次 `force: true`，
                    //   建出来的按钮停在默认 `enabled=true`，Play 里第一次滑出来的 0.3 秒真鼠标点得到
                    //   （被 R1 的 108 倍位移掩盖着，R1 修完才现形）。
                    var dwbs = dflt.GetComponentsInChildren<WindowButton>(true);
                    int dOn0 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn0++;
                    CheckTrue(dwbs.Length > 0 && dOn0 == 0,
                              $"★ 起手收起 ⇒ 抽屉里 **{dwbs.Length}** 个命中区**全部 `enabled=false`**"
                              + $"（实测还开着 **{dOn0}** 个 —— 这一条是「第一次滑出来的 0.3 秒点得到」的判据）");
                    if (fw != null) fw.Click();
                    CheckTrue(dflt.gameObject.activeSelf, "点 `Filters` ⇒ 抽屉打开（原版这颗钮开的就是它）");
                    CheckNear(win.DrawerSlide(3), 1f, 0.001f, "🆕 A11：打开 = 进度 **1**（停在原位）");
                    // 「停在原版矩形」这一条原来在关着的时候量（见上面那段订正）⇒ 挪到**开着**的时候
                    CheckAt(dflt, 0.06f, 335.56f, 155.94f, 1080f,
                            "…打开后**停在原版矩形**中心（A11 起这条只能在开着时量）");
                    int dOn1 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn1++;
                    CheckTrue(dwbs.Length > 0 && dOn1 == dwbs.Length,
                              $"★ …而到位后 **{dwbs.Length}** 个命中区**全回来**（实测 {dOn1} 个）—— "
                              + "与上面那条正好两态对比：写成恒开/恒关都过不了这两条");
                    var a0 = FindChild(dflt, "Cell_fac_" + CampaignData.Armies[0]);
                    CheckTrue(a0 != null,
                              $"`Army Filter` 里 **{CampaignData.Armies.Length}** 个阵营格建了"
                              + $"（第 1 个 = {CampaignData.Armies[0]}）");
                    // 正例：筛第 1 套卡组的阵营 ⇒ 只剩它那几套；反例：筛一个没有卡组的阵营 ⇒ 空 + 提示亮
                    string fac0 = CollectionData.DeckAt(0).Faction;
                    CheckTrue(!string.IsNullOrEmpty(fac0), "第 1 套卡组推得出阵营（筛选用）");
                    int nAll = win.DeckCellCount;
                    win.ToggleDeckFacFilter(fac0);
                    Check(win.DeckFacFilter, fac0, $"点阵营格 ⇒ 筛选条件 = {fac0}");
                    CheckTrue(win.DeckCellCount > 0 && win.DeckCellCount <= nAll,
                              $"筛「{fac0}」⇒ 卡组格 {nAll} → {win.DeckCellCount}（真筛得动）");
                    string other = "";
                    foreach (var a in CampaignData.Armies) if (a != fac0) { other = a; break; }
                    win.ToggleDeckFacFilter(fac0);          // 先取消
                    Check(win.DeckFacFilter, "", "再点同一格 ⇒ 取消（与 Cards 页 `$fac:` 同一条手感）");
                    win.ToggleDeckFacFilter(other);
                    Check(win.DeckCellCount, 0, $"筛一个**没有卡组**的阵营（{other}）⇒ 一格都不剩");
                    CheckTrue(DeckEmptyShown(win), "……而且 `Empty Collection Warning` **亮起来**（原版判据：过滤后为空）");
                    win.ClearDeckFilters();
                    Check(win.DeckFacFilter, "", "`Clear filters` ⇒ 筛选清掉");
                    Check(win.DeckCellCount, nAll, $"……卡组格回到 {nAll}");
                    CheckTrue(!DeckEmptyShown(win), "……而且那条提示关回去");
                    if (fw != null) fw.Click();
                    CheckTrue(!dflt.gameObject.activeSelf, "再点 `Filters` ⇒ 抽屉收起");
                    CheckNear(win.DrawerSlide(3), 0f, 0.001f, "…而且进度回到 **0**（A11：收 = 滑出去）");
                    int dOn2 = 0; foreach (var b in dwbs) if (b != null && b.enabled) dOn2++;
                    CheckTrue(dwbs.Length > 0 && dOn2 == 0,
                              $"…收起到位 ⇒ 命中区**又关回去**（实测还开着 {dOn2} 个）—— "
                              + "三态（收起 / 打开 / 再收起）走的是同一份实现");
                }
            }

            // `Empty Collection Warning`（**Deck 页那一份**）—— 原版**四个页各有一份、矩形各不相同**，
            // 逐页的实读值写在 `CollectionWindow.BuildDeckList` 那条注释里。
            {
                var ew = FindChild(win.PageRoot(0), "Empty Collection Warning");
                CheckTrue(ew != null, "Deck 页有 `Empty Collection Warning`（原版四页各一份）");
                if (ew != null)
                {
                    CheckAt(ew, 165.88f, 1970.01f, 70.94f, 1080f, "Deck 页 `Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf,
                              "有卡组 ⇒ 不显示（判据 = `CollectionData.DeckCount() <= 0`）");
                }
            }

            // 🆕 **2026-10-18（第三轮整改 · 审查 P4）：四个页的空态那行字 —— 全部走词条** ----
            //   原版四份 `Empty Collection Warning/Warning` 各挂一条 `MenuCollection/*No*Found`
            //   （本审查先认树、再按 pid 亲读 `mTerm` + TMP 的 `m_text`）：
            //     Deck 页 `MenuCollection/NoDecksFound` · Cards/异画页 `MenuCollection/NoCardsFound` ·
            //     卡背页 `MenuCollection/NoCardbackFound`（卡背抽屉那一份在 `DeckRuntime`，另有断言）。
            //   ⚠️ 这里只断**字**：四页的 `act=F` 出厂态与显隐判据由上面那几条 / `BuildDeckList` 管。
            {
                var eKeys = new[] { "MenuCollection/NoDecksFound", "MenuCollection/NoCardsFound",
                                    "MenuCollection/NoCardbackFound", "MenuCollection/NoCardsFound" };
                var ePage = new[] { "Deck 页(0)", "Cards 页(1)", "卡背页(2)", "异画页(3)" };
                for (int pi = 0; pi < eKeys.Length; pi++)
                {
                    CheckTrue(Loc.HasEntry(eKeys[pi]),
                              $"（前提）词条 `{eKeys[pi]}` 在表里（⛔ 不在 ⇒ {ePage[pi]} 那条两边一起退化成键名 = 假绿）");
                    var ewN = FindChild(win.PageRoot(pi), "Empty Collection Warning");
                    var wlb = ewN != null ? FindChild(ewN, "Warning") : null;
                    CheckTrue(wlb != null, $"{ePage[pi]} 的 `Empty Collection Warning/Warning` 节点在");
                    CheckText(wlb != null ? TextOf(wlb) : null, Loc.T(eKeys[pi]),
                              $"{ePage[pi]} 空态那行字 = `Loc.T(\"{eKeys[pi]}\")`（随语档；"
                            + "原文 = 原版那颗 `Warning` 的 TMP `m_text`）");
                }
            }

            // ---------------- 交互：点一格 / 点 Create / 滚动 ----------------
            Section("交互（批处理直调，与真点同一条 `WindowButton.onClick`）");
            int before = CollectionData.CurrentIndex();
            int pick = (before + 1) % Mathf.Max(1, CollectionData.DeckCount());
            if (cells.Count > pick)
            {
                var hit = FindChild(cells[pick], "Hit");
                var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
                CheckTrue(wb != null && wb.onClick != null, "格子上的点击区挂到了 `WindowButton`");
                if (wb != null) wb.Click();
                Check(CollectionData.CurrentIndex(), pick, "点第 " + (pick + 1) + " 格 ⇒ 当前卡组换成它");
                // 🔴 **2026-09-23 换路**：原来这里是「**再点一下同一格** = 进编辑」—— 那是 `Deck info Popup`
                //    还没建时的**顶替路**（当时就出声了）。现在那扇窗建好了 ⇒ 改验**原版那条路**：
                //    点格 ⇒ 开窗 ⇒ 窗里点 `Edit Deck` ⇒ 才交接下标。
                var pop = CollectionWindow.LastOpened;
                CheckTrue(pop != null, "点一格卡组 ⇒ **开出了 `Deck info Popup`**（原版那条路）");
                Check(pop != null ? pop.DeckIndex : -1, pick, "弹窗收到的是**这一格**的下标");
                CheckTrue(pop == null || pop.CurrentState == WindowState.Open, "弹窗进了 `Open` 态");
                CollectionData.PendingEditDeck = -1;
                var eBtn = pop != null ? pop.Btn("Edit Deck") : null;
                var eWb = eBtn != null ? eBtn.GetComponent<WindowButton>() : null;
                CheckTrue(eWb != null, "窗里 `Edit Deck` 有点击区");
                if (eWb != null) eWb.Click();
                Check(CollectionData.PendingEditDeck, pick,
                      "窗里点 `Edit Deck` ⇒ 交接给 `DeckRuntime` 的下标 = " + pick
                      + "（「从收藏进编辑」的判据；真机上这一步会 `LoadScene(\"DeckEditor\")`）");
                if (pop != null) pop.Close();
            }
            var createHit = FindChild(tabsRoot, "CreateHit");
            var createBtn = createHit != null ? createHit.GetComponent<WindowButton>() : null;
            CheckTrue(createBtn != null, "`Create` 钮有点击区");
            if (createBtn != null)
            {
                // 🔴 **2026-10-12（`A1697`）改判：`Create` 不再入卡组库** —— 照原版
                //   `SelectDecksTab__CreateDeck.c:19,26,28`（`GetEmptyDeck` → 直接开编辑窗，**全链零入库**）。
                //   用户可见：点「新建卡组」**不再在列表里多出一格「卡背 + 新卡组」**，而是直接进编辑器；
                //   没选战将就退出 ⇒ **什么都不剩**。
                int n0 = CollectionData.DeckCount();
                createBtn.Click();
                Check(CollectionData.DeckCount(), n0,
                      $"点 `Create` ⇒ 卡组库**不变**（仍是 {n0}）—— 建组那一刻**不入库**"
                    + "（原版 `SelectDecksTab__CreateDeck` 全链零入库；入库唯一入口是编辑器的 `Done` 过校验之后）");
                CheckTrue(CollectionData.PendingNewDeck != null,
                          "……但**交接给编辑器的那一份**在（`PendingNewDeck != null`）—— 没它编辑器就开不出新卡组");
                Check(CollectionData.PendingEditDeck, -1,
                      "……且两个交接口**互斥**（`PendingEditDeck == -1`，⛔ 留着旧下标会跳进**别的**卡组）");
                CollectionData.TakePendingNewDeck();     // 取走（先取后清）⇒ 别漏到后面几节
            }
            // 🔴 **实拍抓的一条**：`Clear filters` 原来摆在 1218.6（**我们推错的**）⇒ 它和 `Import` 压掉 77.6px。
            //    真值 = A2 §161 的**实测落点 612.2**（容器宽 0 + 子件 `m_IgnoreLayout=1`）。
            {
                var cf = FindChild(tabsRoot, "Clear filters Text");
                var il = FindChild(tabsRoot, "Import Text");
                var lcf = cf != null ? cf.GetComponent<Label>() : null;
                var lil = il != null ? il.GetComponent<Label>() : null;
                if (lcf != null && lil != null)
                {
                    // 🆕 **2026-10-16（A796′）换口**：半宽（`WorldW × 54`）不再读**字段缓存** `Label.WorldW`（`_tmpW`），
                    //    改走 `LabelRenderedPx`（TMP `textBounds` 那条口）—— 中心仍是**节点位置**（只换「从哪个口读那个数」）。
                    float cfR = MenuCheck.DesignPx(lcf.transform).x + LabelRenderedPx(lcf).x * 0.5f;
                    float iL = MenuCheck.DesignPx(lil.transform).x - LabelRenderedPx(lil).x * 0.5f;
                    CheckTrue(cfR < iL,
                              $"`Clear filters`（右缘 **{cfR:F0}**）与 `Import Deck`（左缘 **{iL:F0}**）**不叠**"
                              + " —— 判据是 A2 §161 的实测落点 612.2，**别自己按容器推**");
                }
                // 🔴 **2026-10-18（A891 的续）改「随语档」**：`Import` / `Create` 两颗钮的字从本批起走词条
                //   （`Shell/CollectionWindow.cs` 那两处 `Loc.T(...)`）⇒ 宿主跑在**出厂语言 = 中文**时，
                //   原来写死的 `"Import Deck"` / `"Create Deck"` 必红。原文本身没丢：两条词条的**英文列**
                //   = 原版 prefab 那颗 TMP 的 `m_text` 原文（本批按 pid 亲读）—— 见 `Core/Loc.cs` 的注释。
                CheckTrue(Loc.HasEntry("MenuDeck/MenuButtons/ImportDeck")
                          && Loc.HasEntry("MenuDeck/MenuButtons/CreateDeck"),
                          "（前提）两条 `MenuDeck/MenuButtons/*` 词条在表里（⛔ 不在 ⇒ 下面两条两边一起退化成键名）");
                CheckText(TextOf(FindChild(tabsRoot, "Import Text")), Loc.T("MenuDeck/MenuButtons/ImportDeck"),
                          "`Import` 钮文案（键 `MenuDeck/MenuButtons/ImportDeck`，原版 TMP 原文 `Import Deck`；随语档）");
                CheckText(TextOf(FindChild(tabsRoot, "Create Text")), Loc.T("MenuDeck/MenuButtons/CreateDeck"),
                          "`Create` 钮文案（键 `MenuDeck/MenuButtons/CreateDeck`，原版 TMP 原文 `Create Deck`；随语档）");

                // 🆕 **2026-10-17（`A1212` 块 3 #8/#9/#10/#13）**：这四颗的四格 + 框。
                //   判据 = 原版 `… ▸ Header ▸ Clear Filter Button/Button Text`（`10/42/12/0`、有效框 221.44）·
                //   `… ▸ Control Buttons ▸ {Create,Import} ▸ Button Text`（`10/42/12/0`、有效框 220.81）·
                //   `… ▸ Deck Filters ▸ Filters ▸ Army Filter ▸ Title`（`10/36/32/1`、有效框 335.31）。
                CheckA1212(tabsRoot, "Clear filters Text", "…/Header ▸ Clear Filter Button ▸ Button Text",
                           10f, 42f, 12f, 0, boxW: 221.44f);          // A1226 表 B #8（框宽已改）
                CheckA1212(tabsRoot, "Create Text", "…/Control Buttons ▸ Create ▸ Button Text",
                           10f, 42f, 12f, 0, boxW: 220.81f);          // #9
                CheckA1212(tabsRoot, "Import Text", "…/Control Buttons ▸ Import ▸ Button Text",
                           10f, 42f, 12f, 0, boxW: 220.81f);          // #10
                // `Title` 这颗在**整棵 Tabs** 里有同名件（几个页头都叫 `Title`）⇒ 在 **Deck 页**
                //   （`PageRoot(0)`，`Deck Filters ▸ Filters ▸ Army Filter` 那一棵）里逐级找，
                //   ⛔ 不用 `FindDeep(tabsRoot, …)` 取「第一个同名」（会量错对象）。
                {
                    var armyF = FindDeep(FindDeep(win.PageRoot(0), "Deck Filters"), "Army Filter");
                    CheckTrue(armyF != null, "（前提）`Army Filter` 那一格在（`Title` 要在它下面找）");
                    if (armyF != null)
                        CheckA1212(armyF, "Title", "…/Deck Filters ▸ Filters ▸ Army Filter ▸ Title",
                                   10f, 36f, 32f, 1, boxW: 335.31f);   // #13（原版 fs36 / 框宽 335.31）
                }
            }
            // 🔴 **2026-10-05（A89）补：`Control Buttons` 的三颗视觉序** —— 原版那个 HLG 是
            //    **`m_ReverseArrangement = 1`**（spacing 25 · `pad.right` 14 · align `MiddleRight` · `expandW=1`）
            //    ⇒ **树序 `[Create, Import, Unlock]` 倒排** ⇒ 视觉左→右 = `Unlock` **1180.00** →
            //    `Import` **1391.00** → `Create` **1661.01**（组矩形 1179.99,80.94 → 1920.01,140.94）。
            //    判据 = uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`；跑后矩形 =
            //    `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 20 --md`。
            //    ⚠️ 这与 `Shell/CollectionWindow.cs` 的 `CreateX = 1661` / `ImportX = 1391` **逐位吻合**
            //    （实现本来是对的，那段注释写成了镜像，已同批订正）。
            //    🔴 量的是**节点的世界 x**（`CreateHit`/`ImportHit`），不是常量 ——
            //       断常量 = 同义反复；镜像摆法会把这一条连同上面那条 `Clear filters` 不叠一起打红。
            //    ⚠️ `MenuDraw.Hit` 的**节点摆在父原点、quad 在矩形中心**（`MenuDraw.cs` `Hit` 的注释）
            //       ⇒ 必须量 `GetComponentInChildren<ImageQuad>()`。
            {
                var chq = FindChild(tabsRoot, "CreateHit");
                var ihq = FindChild(tabsRoot, "ImportHit");
                var cq = chq != null ? chq.GetComponentInChildren<ImageQuad>() : null;
                var iq = ihq != null ? ihq.GetComponentInChildren<ImageQuad>() : null;
                float ccx = cq != null ? MenuCheck.DesignPx(cq.transform).x : -1f;
                float icx = iq != null ? MenuCheck.DesignPx(iq.transform).x : -1f;
                CheckNear(icx, 1513.50f, 1f, "`Import` 中心 x = **1513.50**（原版左缘 1391.00 + 245/2）");
                CheckNear(ccx, 1783.50f, 1f, "`Create` 中心 x = **1783.50**（原版左缘 1661.01 + 245/2）");
                CheckNear(ccx - icx, 270f, 1f,
                          "★ `Create` 在 `Import` 的**右边** 270px（= 245 + spacing 25）—— "
                          + "照「树序 = 视觉序」镜像摆 ⇒ 这条变 **−270**、红（判据：`Control Buttons` 的 `reverse=1`）");
                // 原版这一组的**最左**是 `Unlock`（1180.00→1366.01）—— 它的 GO `m_IsActive = 1` 但**三个可见
                // 组件全 `m_Enabled = 0`**（`Image` / `Button Outline` / `Text` 的 TMP）⇒ 原版也画不出东西。
                // ⚠️ 与「顺序错」是**两条账**，别拿上面几条断言去替它。
                // 🆕 2026-10-06（A98）：那条账已结 —— `Control Buttons` 组节点 + `Unlock` 占位都建了，见下面那一块。
            }

            // 🆕 **2026-10-06（A98）：`Control Buttons` 组节点 + `Unlock` 占位**（原来两颗钮**直接挂在页节点上**、
            //    这一层没建 ⇒ 见 `项目任务.md` §三 A98 行）。判据（**逐字段直读** prefab `bundle_menus_assets_all`）：
            //    · 组 rect **1180.00,80.94 → 1920.01,140.94**（740.01×60）；组上挂
            //      `HorizontalLayoutGroup`（reverse=1 · spacing 25 · align MiddleRight · pad.right 14 · expandW 1）
            //      + `ContentSizeFitter(m_HorizontalFit = 1 = MinSize)`
            //    · `Unlock` 组内**最左**：**1180.00 → 1366.01**（宽 186.01），三个可见组件**全 `m_Enabled = 0`**
            //      ⇒ **原版也画不出东西**（是**组件级**禁用、不是 GO 级）
            //    🔴 量的是**节点的世界位置**（`MenuDraw.Node` 把节点摆在它那个矩形的中心）+ **挂载关系**，
            //       ⛔ 不是量我们的常量（断常量 = 同义反复）。
            {
                var cbg = FindChild(tabsRoot, "Control Buttons");
                CheckTrue(cbg != null,
                          "★ `Control Buttons` **组节点**建了（原版 `Select Deck Tab>Header/Control Buttons`）");
                if (cbg != null)
                {
                    CheckNear(MenuCheck.DesignPx(cbg).x, 1550.01f, 1f,
                              "★ 组节点中心 x = **1550.01**（原版组矩形 1180.00→1920.01；宽 740.01"
                              + " = 186.01+245+245+25+25+**14**(pad.right)）");
                    CheckNear(MenuCheck.DesignPx(cbg).y, 110.94f, 1f, "★ …中心 y = **110.94**（原版 80.94→140.94）");
                    var bNew = FindChild(cbg, "Create");
                    var bImp = FindChild(cbg, "Import");
                    CheckTrue(bNew != null && bImp != null,
                              "`Create` / `Import` 两颗钮在**组节点底下**（原版 `Control Buttons>{Create,Import}`）");
                    var uUn = FindChild(cbg, "Unlock");
                    CheckTrue(uUn != null,
                              "★ 组里那颗 `Unlock` **占位**建了（原版树里有它；它占的 186.01px 正是"
                              + " `Import` 落在 1391.01 的原因）");
                    if (bNew != null && bImp != null && uUn != null)
                    {
                        // 树序照原版（⛔ 与视觉序**相反** —— `reverse=1`；视觉左→右 = Unlock → Import → Create）
                        CheckTrue(bNew.GetSiblingIndex() < bImp.GetSiblingIndex()
                                  && bImp.GetSiblingIndex() < uUn.GetSiblingIndex(),
                                  "★ 组内**树序** = 原版 `[Create, Import, Unlock]`（`reverse=1` ⇒ 视觉序**反之**："
                                  + "最左是 `Unlock`）");
                        CheckNear(MenuCheck.DesignPx(uUn).x, 1273.01f, 1f,
                                  "★ `Unlock` 中心 x = **1273.01**（原版 1180.00→1366.01，宽 186.01）");
                        CheckNear(MenuCheck.DesignPx(uUn).y, 110.94f, 1f, "…中心 y = **110.94**（与另两颗同一行）");
                        // 🆕 **2026-10-06（A125③）：`Unlock` 的**两个子件**（原来一条断言都没有 —— 摆错不会红）**。
                        //    判据 = **逐字段直读** prefab（`bundle_menus_assets_all`；我另跑了一遍 RT 索引复核，
                        //    与 `Shell/CollectionWindow.cs` 的 `BuildUnlockPlaceholder` 注释同一份数据）：
                        //      · `Button Outline`（GO pid `-8849062637360586027`）拉伸锚 (0,0)→(1,1) ·
                        //        `m_SizeDelta (-1, 0)` · `m_AnchoredPosition (0,0)` ⇒ 左右各内缩 0.50、上下不缩
                        //        ⇒ **相对 `Unlock` 左上角 (0.50,0)→(185.51,60)**；
                        //      · `Text`（GO pid `-220370555535629611`）同锚 · `m_SizeDelta (-16.72, -17.19)`
                        //        ⇒ **相对 `Unlock` 左上角 (8.36,8.59)→(177.65,51.41)**（左右各内缩 8.36、
                        //        上下各内缩 8.59）；
                        //      · 两者**四边都是对称内缩** ⇒ 与 `Unlock` **同心** ⇒ 中心 = `Unlock` 的中心。
                        //    🔴 期望值写**字面量**（`Unlock` 中心 = 1180.00 + 186.01/2 = **1273.01** ·
                        //       y = 80.94 + 60/2 = **110.94**）—— ⛔ **别拿 `CollectionWindow` 的常量再算一遍**
                        //       （那是同义反复：常量改了这条照样绿）。
                        //    ⚠️ **这条量得到的只有中心**：`MenuDraw.Node` 建的是**普通 `Transform`**（没有
                        //       `RectTransform`）⇒ 节点上**不带宽高** ⇒ `185.51` / `177.65` / `60` / `42.82`
                        //       这几个尺寸**本条钉不住**（要钉住得让节点带尺寸语义 = 改公共件 `MenuDraw.Node`，
                        //       不在本件白名单）⇒ 报告里如实记着，别当成「尺寸已钉住」。
                        CheckTrue(uUn.childCount == 2,
                                  $"★ `Unlock` 底下**正好两个**子件（原版子树就 `Button Outline` + `Text`；"
                                  + $"实测 {uUn.childCount} 个）");
                        var uBo = FindChild(uUn, "Button Outline");
                        var uTx = FindChild(uUn, "Text");
                        if (uBo == null)
                            CheckTrue(false, "★ `Unlock` 的子件 `Button Outline` 在（原版树里有它）"
                                             + " —— 不在 ⇒ 下面两条等于没验");
                        else
                        {
                            CheckNear(MenuCheck.DesignPx(uBo).x, 1273.01f, 1f,
                                      "★ `Button Outline` 中心 x = **1273.01**（原版相对 `Unlock`"
                                      + " (0.50,0)→(185.51,60)：左右各内缩 0.50 ⇒ 与 `Unlock` 同心）");
                            CheckNear(MenuCheck.DesignPx(uBo).y, 110.94f, 1f,
                                      "★ `Button Outline` 中心 y = **110.94**（原版上下**不**内缩 ⇒ 与 `Unlock` 同中心）");
                        }
                        if (uTx == null)
                            CheckTrue(false, "★ `Unlock` 的子件 `Text` 在（原版树里有它，文案「Debug Unlock」）"
                                             + " —— 不在 ⇒ 下面两条等于没验");
                        else
                        {
                            CheckNear(MenuCheck.DesignPx(uTx).x, 1273.01f, 1f,
                                      "★ `Text` 中心 x = **1273.01**（原版相对 `Unlock` (8.36,8.59)→(177.65,51.41)："
                                      + "左右各内缩 8.36 ⇒ 与 `Unlock` 同心）");
                            CheckNear(MenuCheck.DesignPx(uTx).y, 110.94f, 1f,
                                      "★ `Text` 中心 y = **110.94**（上下各内缩 8.59 ⇒ 与 `Unlock` 同心）");
                        }
                        if (uBo != null && uTx != null)
                            CheckTrue(uBo.GetSiblingIndex() < uTx.GetSiblingIndex(),
                                      "★ `Unlock` 两个子件的**树序** = 原版 `[Button Outline, Text]`"
                                      + "（判据 = 原版 `RectTransform` 的 `m_Children` 逐位实读，不是照名字猜）");
                        var bImpQ = bImp.GetComponent<ImageQuad>();
                        // 🔴 **2026-10-06（A125③）**：这里原来是 **`if` 没有 `else`** —— 拿不到 quad 时
                        //    下面那条**一条都不跑、section 照样全绿**（**A52-F10** 那一族的形状：
                        //    弱断言分不出两种状态）。**今天它不可能 null**（`Shell/MenuDraw.cs` 的 `Rect`
                        //    转调 `ImageQuad.Create`，quad 组件加在**节点自己**身上，`Import` 那颗既是
                        //    节点也是 quad）⇒ 不是当下假绿，但形状要堵住：改成**前提断言 + 早退**，
                        //    条件不成立当场红、并说清「下面那条等于没验」。
                        if (bImpQ == null)
                            CheckTrue(false, "★ `Import` 那颗钮的**图片本体**（`ImageQuad`）拿得到"
                                             + " —— 拿不到就量不了它相对 `Unlock` 的位置，下面那条等于没验");
                        else
                            CheckNear(MenuCheck.DesignPx(bImpQ.transform).x - MenuCheck.DesignPx(uUn).x, 240.5f, 1f,
                                      "★ `Unlock` 中心 →`Import` 中心 = **240.5**（= 186.01/2 + spacing **25** + 245/2）"
                                      + " —— 原版那颗占位**参与排布**，位置摆错这条就红");
                        int uq = uUn.GetComponentsInChildren<ImageQuad>(true).Length;
                        int ul = uUn.GetComponentsInChildren<Label>(true).Length;
                        CheckTrue(uq == 0 && ul == 0,
                                  $"★ `Unlock` 底下**一个图、一个字都没有**（实测 quad **{uq}** · label **{ul}**）—— "
                                  + "原版那三个可见组件**全 `m_Enabled = 0`**（自己的 `Image` · 子 `Button Outline` 的"
                                  + " `Image` · 子 `Text` 的 TMP「Debug Unlock」）⇒ **原版也画不出东西**；"
                                  + "给它加图/加字 = 与原版可见内容不符");
                        var bnL = FindChild(cbg, "Create Text");
                        var biL = FindChild(cbg, "Import Text");
                        CheckTrue(bnL != null && bnL.IsChildOf(cbg) && biL != null && biL.IsChildOf(cbg),
                                  "两颗钮的文案在**组节点子树**里（原版 = `Create>Button Text` / `Import>Button Text`）");
                    }
                }
            }
            var impHit = FindChild(tabsRoot, "ImportHit");
            CheckTrue(impHit != null && impHit.GetComponent<WindowButton>() != null,
                      "`Import` 钮有点击区（点了**如实说没实现**，不静默）");
            // ---- 🆕 2026-10-17（N29）：滚轮灵敏度 = 原版那台 `ScrollRect` 的 `m_ScrollSensitivity` ----
            //   判据 = `资料/普查产出_1017/UI/D_N29_滚动灵敏度逐视图.md`（原版 prefab 逐台实读）。
            //   ⛔ 别断 `MenuScroll.DefaultNotchPx` —— 那个 48 是「没现读」的兜底，断它 = 自证。
            {
                void Sw(string who, MenuScroll sc, float want)
                {
                    CheckTrue(sc != null, $"（前提·不静默）{who} 的滚动区在");
                    if (sc != null)
                        CheckNear(sc.NotchPx, want, 0.001f,
                                  $"{who}：滚轮灵敏度 = 原版 `m_ScrollSensitivity` **{want}** px/格"
                                  + $"（实测 {sc.NotchPx}）");
                }
                Sw("收藏窗·卡牌页", win.CardsScroll, 100f);
                Sw("收藏窗·卡背页", win.CosmoScroll, 100f);
                Sw("收藏窗·异画页", win.StyleScroll, 100f);
                Sw("收藏窗·卡组页", win.DeckScroll, 100f);
                Sw("收藏窗·筛选项", win.FilterScroll, 50f);
            }

            if (win.DeckScroll != null)
            {
                float max = win.DeckScroll.MaxOffset;
                CheckTrue(max >= 0f, $"卡组列表的滚动极值 = {max:F0}px（`MenuScroll` 纵向；格数少时可以为 0）");
                if (max > 0f)
                {
                    win.DeckScroll.ScrollBy(max);
                    var last = CollectionData.DeckCount() - 1;
                    var lastCell = FindChild(tabsRoot, "CollectionDeck_" + last);
                    CheckTrue(lastCell != null
                              && MenuCheck.DesignPx(lastCell).y + DeckCellHpx() / 2f <= 1080.5f,
                              "**滚到底 ⇒ 最后一格完整落进视口**（这是「够不着」那个缺口的判据）"
                              + "【实测：max=" + max.ToString("F1") + " · 末格=" + last + " · 取到=" + (lastCell != null)
                              + " · 末格中心 y=" + (lastCell != null ? MenuCheck.DesignPx(lastCell).y.ToString("F1") : "-")
                              + " · 格高=" + DeckCellHpx().ToString("F1")
                              + " · 底边=" + (lastCell != null ? (MenuCheck.DesignPx(lastCell).y + DeckCellHpx() / 2f).ToString("F1") : "-")
                              + "】");
                    win.DeckScroll.ScrollBy(-max);
                }
            }

            // ---- 🆕 2026-10-03（A12 收口）：**滚动视口的裁切也管点击区** —— 真界面回归 ----
            //   判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid` 就是一句
            //   `RectangleContainsScreenPoint` ⇒ 框外的点判不中任何东西）。判据正本 = 本工程唯一一份
            //   `MenuDraw.ClipRect`（它的注释里有 UGUI 源码出处）；这一段量的是**真界面**、不是临时节点。
            //   ⚠️ 这一段量的是**节点的位置**（= 交集块的中心），不是宽高。
            //      🔴 **2026-10-04 订正（A26）**：原来这里写着「`MenuDraw.DeckCell` 的 `Hit` 是**裸节点**（不带 quad）
            //      ⇒ 它进不了 `PointerLayer` 的命中表」—— **那条已经不成立**：裸节点已经补上了透明 quad
            //      （见 `MenuDraw.DeckCell` 的注释与新加的「真命中路」那一段断言）。
            {
                var ds = win.DeckScroll;
                if (ds != null)
                {
                    float savedD = ds.Offset;
                    ds.SetOffset(0f);                       // 起手态：第 3 行压在视口下边上
                    Transform edge = null; float edgeCy = 0f;
                    for (int i = 0; i < win.DeckCells.Count; i++)
                    {
                        var c = win.DeckCells[i];
                        if (c == null) continue;
                        float cy = MenuCheck.DesignPx(c).y;
                        if (cy + DeckCellHpx() * 0.5f > CollectionWindow.DeckViewport.y2 + 0.5f)
                        { edge = c; edgeCy = cy; break; }
                    }
                    CheckTrue(edge != null, "起手有一格**压在视口下边上**（否则这一条等于没验）");
                    if (edge != null)
                    {
                        float ecx = MenuCheck.DesignPx(edge).x;
                        var full = new PxRect(ecx - DeckCellWpx() * 0.5f, edgeCy - DeckCellHpx() * 0.5f,
                                              ecx + DeckCellWpx() * 0.5f, edgeCy + DeckCellHpx() * 0.5f);
                        PxRect vis;
                        bool inView = MenuDraw.ClipRect(full, CollectionWindow.DeckViewport, out vis);
                        CheckTrue(inView && !MenuDraw.SameRect(vis, full),
                                  "现算：这一格与视口的**交集比整格小**（= 真被裁了，下面才有可验的）");
                        var eHit = FindChild(edge, "Hit");
                        CheckTrue(eHit != null, "压在边上的那一格**建了** `Hit`（部分越界 ⇒ 建、但位置被截）");
                        if (eHit != null)
                        {
                            CheckNear(MenuCheck.DesignPx(eHit).x, vis.CX, 0.5f,
                                      "命中区中心 x = **露出来那块的中心**（横向没裁 ⇒ 与格中心同）");
                            CheckNear(MenuCheck.DesignPx(eHit).y, vis.CY, 0.5f,
                                      "命中区中心 y = **露出来那块的中心**（**不是**整格中心）");
                            CheckNear(Mathf.Abs(MenuCheck.DesignPx(eHit).y - edgeCy), (full.H - vis.H) * 0.5f, 0.5f,
                                      "…与整格中心的差 = 被裁掉那半截的一半（"
                                      + $"{Mathf.Abs(MenuCheck.DesignPx(eHit).y - edgeCy):F1} ≈ {(full.H - vis.H) * 0.5f:F1}）");
                        }
                    }
                    ds.SetOffset(savedD);
                }
            }

            // ---- 🆕 2026-10-04（A26）：**卡组格真鼠标点得到**（走真命中路，**不是**直调 `wb.Click()`）----
            //   判据 = `PointerLayer.CollectHits` 取的是「按钮下**第一个 `ImageQuad`**」
            //   （`Shell/PointerLayer.cs`）。`MenuDraw.DeckCell` 的 `Hit` 当年是 `Node()` 建的**裸节点**
            //   ⇒ 命中表里**恒没有它** ⇒ 真鼠标点不动；而 `WindowButton.onClick` 在着、自检直调 `wb.Click()`
            //   也过 ⇒ **只有这条路能验出来**（这正是 `真Play待验清单` A 鼠标那条的意义）。
            //   ⚠️ 这一段是**会红的**：把 `DeckCell` 改回裸节点它立刻红（不是自证 —— 期望值取自
            //      `PointerLayer` 那条与实现无关的路）。
            {
                var pl = PointerLayer.Instance;
                CheckTrue(pl != null, "`PointerLayer` 在（真鼠标 → 界面**唯一**那条路）");
                if (pl != null && win.DeckScroll != null)
                {
                    float savedD = win.DeckScroll.Offset;
                    win.DeckScroll.SetOffset(0f);               // 起手态：行 0 完整在视口里
                    var dc = win.DeckCells;                     // ⚠️ `SetOffset` 会重建 ⇒ 重建之后再取表
                    CheckTrue(dc.Count > 0, "有卡组格可点（否则这一段等于没验）");
                    if (dc.Count > 0)
                    {
                        var c0 = dc[0];
                        var h0 = FindChild(c0, "Hit");
                        CheckTrue(h0 != null, "第 1 格有 `Hit` 节点");
                        var hq = h0 != null ? h0.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(hq != null,
                                  "★ 命中区节点下**真的挂着 `ImageQuad`** —— `PointerLayer` 只认它"
                                  + "（裸节点 = 进不了命中表 = 真鼠标点不动）");
                        if (hq != null)
                            Check(hq.RenderQueue, CollectionWindow.QPageRow,
                                  "命中 quad 的渲染队列 = `QPageRow`（与 `DeckCell` 的入参同档）");
                        var b0 = pl.ButtonAt(MenuCheck.PointerFramePx(c0).x, MenuCheck.PointerFramePx(c0).y);   // 指针帧（喂 PointerLayer，W57 §1·2 第三帧）
                        CheckTrue(b0 != null && b0.transform == h0,
                                  "★ **真命中路**（`PointerLayer.ButtonAt`）在格中心拿到的是**这一格**"
                                  + "（拿到 " + (b0 == null ? "**null** = 真鼠标点不动" : "`" + b0.name + "`") + "）");
                    }

                    // 压在视口下边上的那一格：**露出来那块的中心**也点得到，且命中 quad 的高**跟着裁切走**
                    Transform edge = null; float edgeCy = 0f;
                    for (int i = 0; i < win.DeckCells.Count; i++)
                    {
                        var c = win.DeckCells[i];
                        if (c == null) continue;
                        float cy = MenuCheck.DesignPx(c).y;
                        if (cy + DeckCellHpx() * 0.5f > CollectionWindow.DeckViewport.y2 + 0.5f)
                        { edge = c; edgeCy = cy; break; }
                    }
                    CheckTrue(edge != null, "起手有一格**压在视口下边上**（否则下面那条等于没验）");
                    if (edge != null)
                    {
                        float ecx = MenuCheck.DesignPx(edge).x;
                        var full = new PxRect(ecx - DeckCellWpx() * 0.5f, edgeCy - DeckCellHpx() * 0.5f,
                                              ecx + DeckCellWpx() * 0.5f, edgeCy + DeckCellHpx() * 0.5f);
                        PxRect vis;
                        bool partClip = MenuDraw.ClipRect(full, CollectionWindow.DeckViewport, out vis)
                                        && !MenuDraw.SameRect(vis, full);
                        // 🔴 **A52-F10 修（2026-10-06）**：这一段原来是个**没有 `else` 的 `if`** ——
                        //   条件不成立（那一格**整块**落在视口外 ⇒ `ClipRect` 返回 false）时，下面那两条 ★
                        //   **一条都不跑**，而 section 照样全绿 ⇒ 「弱断言分不出两种状态」那一族。
                        //   改成**前提断言 + `if`**：条件不成立时当场红，并说清「下面两条等于没验」。
                        //   （判据同上面 A12 那一段的 `现算：交集比整格小`；写法照 `Editor/ShellScene.cs` 的 R7 探针。）
                        CheckTrue(partClip,
                                  $"★ 前提：压边那一格必须**部分**越界（`ClipRect` 为真 **且** 交集 ≠ 整格；"
                                  + $"实测 `vis`=( {vis.x1:F1},{vis.y1:F1} )→( {vis.x2:F1},{vis.y2:F1} )"
                                  + $" vs 整格 ( {full.x1:F1},{full.y1:F1} )→( {full.x2:F1},{full.y2:F1} )）"
                                  + " —— 不成立则下面两条 ★ 等于没验（A52-F10：原来这个 `if` 没有 `else`，整段静默空转）");
                        if (partClip)
                        {
                            var eh = FindChild(edge, "Hit");
                            var ehq = eh != null ? eh.GetComponentInChildren<ImageQuad>() : null;
                            CheckNear(ehq != null ? ehq.WorldH * 108f : -1f, vis.H, 1.5f,
                                      "★ 压在边上的那一格：命中 quad 的高 = **露出来那块**的高"
                                      + "（`Hit` 跟着 `RectMask2D` 的射线那一面裁）");
                            var beP = MenuCheck.PointerFramePxFromDesign(new Vector2(vis.CX, vis.CY), edge);   // 设计帧 → 指针帧（喂 PointerLayer；`vis` 来自模式设计 px 的 `ClipRect`）
                            var be = pl.ButtonAt(beP.x, beP.y);
                            CheckTrue(be != null && be.transform == eh,
                                      "★ …而**露出来那块的中心**（不是整格中心）点得到它"
                                      + "（拿到 " + (be == null ? "**null**" : "`" + be.name + "`") + "）");
                        }
                    }
                    win.DeckScroll.SetOffset(savedD);
                }
            }

            // ---------------- `Deck info Popup`（A1 §2 逐节点表）----------------
            Section("`Deck info Popup`：版面（A1 §2）");
            {
                var pop = win.OpenDeckInfo(0);
                CheckTrue(pop != null, "开得出来");
                if (pop != null)
                {
                    var pr = pop.transform;
                    CheckHoverSwap(pop.transform, "Deck info Popup");   // 🆕 A17：三颗钮 + 关闭钮的圆底
                    Check(pop.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(pop.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                    Check(pop.closeOnEsc, true, "`closeOnESC` = **1**（⚠️ 提示窗是 0）");
                    CheckAt(FindChild(pr, "Generic Window Red Background Big"),
                            134.50f, 1839.50f, 82f, 1032f, "红底 `UI_Deck_Information_Back`");
                    CheckAt(FindChild(pr, "Info Panel"), 659f, 1799f, 218.10f, 868.10f, "`Info Panel`");
                    var dn = CollectionData.DeckAt(0);
                    CheckText(TextOf(FindChild(pr, "Deck Name")), dn.Name, "`Deck Name` = 被点的那套卡组的名字");
                    var wlc = CollectionData.Warlord(0);
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：空战将位那一支的实现从波 1b 起走词条
                    //   （`Shell/DeckInfoPopup.cs` 的 `wl != null ? CardText.Name(wl) : Loc.T("MenuDeck/Error/NoWarlord")`
                    //   —— ⚠️ 2026-10-18（P1）就地订正：这一句原来写 `:846`、且那一半写的是 `wl.Name`，
                    //   本轮督军名改走本地化后两处都变了，行号换成现读的 `CardPresentation/Shell/DeckInfoPopup.cs`）
                    //   ⇒ 原来写死的 `"未选督军"` 是**两代前**的字面量（表里 ZH 列已换成「还没有选战将」，
                    //   见 `Core/Loc.cs` 的 `Loc.T(键)`）⇒ 一旦走到那一支，**两语档都会红**。期望值换成 `Loc.T(键)`。
                    //   ⚠️ 如实记：本件夹具里 `Warlord(0)` 非空 ⇒ 那一支**今天走不到**，本条是为将来兜底。
                    // 🔴 **2026-10-18（P1 · 报告第 5 条）改期望**：这一栏现在印 **`CardText.Name(wlc)`**（本地化卡名），
                    //   不再是英文 `wlc.Name` —— 判据 = 原版运行期喂的是 `RawCardScript.GetLocalizedCardName(督军卡)`
                    //   （`资料/普查产出_1018/R_DeckInfoPopup_对账.md`「Warlord Name」行；实现 →
                    //   `Shell/DeckInfoPopup.cs` 的 `wl != null ? CardText.Name(wl) : …`）。
                    //   ⚠️ `CardText.Name(CardDef)` 里面带 `Zh` 那道语档闸 ⇒ **中文档印中文、英文档仍印英文**
                    //   （两档都对，⛔ 不是把断言放宽成「随便什么串都过」：期望值仍逐字相等比）。
                    CheckText(TextOf(FindChild(pr, "Warlord Name")),
                              wlc != null ? CardText.Name(wlc) : Loc.T("MenuDeck/Error/NoWarlord"),
                              "`Warlord Name` = 该卡组的督军名（**本地化卡名** = `CardText.Name(CardDef)`）"
                            + "（空战将位那一支随语档 = `Loc.T(\"MenuDeck/Error/NoWarlord\")`）");
                    CheckTrue(pop.Btn("Practice Deck") != null && pop.Btn("Edit Deck") != null
                              && pop.Btn("Select Deck") != null, "`Buttons` 三个钮**都建了**（在不在 ≠ 露不露，见下）");
                    // 🆕 2026-10-05（A65② / A82）：`Edit Deck` 那颗的**变灰**落到哪一层。
                    // 判据：原版 `DeckInfoControls__Initialize.c:79-83` 无条件调
                    //   `EverguildButton__SoftDisable(editButton, !CanImportDeck(popup, context.Deck))`，
                    //   而那颗钮的 `colorTintGreyOnDisable = 1`（真包实读 MB `-8697463422759302744`）
                    //   ⇒ `SwitchMaterial` → **图形件材质换成 `Everguild/UI/Greyscale`**（不是改颜色、不是调 alpha）。
                    // 我们这一侧 = `WindowButton.SetSoftDisabled`（`Shell/WindowButton.cs`；⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs`），实现只有一份。
                    // 🔴 **要核的是「可见的那张底图」**：这颗钮的 `WindowButton` 挂在**透明命中区** `Btn_Edit Deck`
                    //    上，可见的底是**兄弟节点** `Bg Edit Deck`（原版 `Selectable.m_TargetGraphic` 就指向它）
                    //    ⇒ 只灰命中区 = 一个像素都不变（静默）。
                    //    🔴 **2026-10-18 就地订正（铁律 5）**：本行原来写「`CanImportDeck` 恒 `false` ⇒ 这颗钮**应当**
                    //       一直是灰的」—— **不成立**：原版参照图（`选择卡组出现的界面参考.png`）里它与 `练习` **同为橙金**
                    //       ⇒ `CanImportDeck` 在原版实况里是 `true`（两道闸的归位见 `Shell/DeckInfoPopup.cs` 的同名方法）。
                    //       ⇒ 这条断言**反过来**：可见底图必须**不是**灰化材质（读回 `Greyscale` 就是改回去了）。
                    {
                        var bgEdit = FindChild(FindChild(pr, "Buttons"), "Bg Edit Deck");
                        var bgq = bgEdit != null ? bgEdit.GetComponent<ImageQuad>() : null;
                        var bmr = bgq != null ? bgq.GetComponent<MeshRenderer>() : null;
                        CheckText(bmr != null && bmr.sharedMaterial != null && bmr.sharedMaterial.shader != null
                                  ? bmr.sharedMaterial.shader.name : "<没有材质>",
                                  "Sprites/Default",
                                  "`Edit Deck` 的**可见底图**保持出厂材质（**不是** `Everguild/UI/Greyscale`）"
                                + " —— 🔴 **2026-10-18 改判**：`CanImportDeck` 恒 `true`（原版参照图里这颗与 `练习` 同为橙金），"
                                + "`SetSoftDisabled(false)` ⇒ `RefreshGray` 不换材质。"
                                + "**改坏法**：把 `CanImportDeck` 改回 `false`（或让 `SoftDisabled` 恒 true）⇒ 这里读回 `Greyscale`、红");
                        // ⚠️ **上面这一条只核「换没换 shader」（弱断言）** —— 它分不出「换了材质、但队列掉了」。
                        //    🔴 **2026-10-05（A85）：换材质不许把显式分层抹掉。** 变灰走 `ImageQuad.SetMaterial`，
                        //    而 `new Material(Everguild/UI/Greyscale)` 的队列是 shader 自带的
                        //    **`Transparent(3000)`**（`工具/dump_shader.py` 实读它的 SubShader 标签），
                        //    本窗红底在 **`QDI = 3120`** ⇒ 不补回去这几颗钮会**掉到红底之下**
                        //    （画面上「按钮没了」，而上面那条**照样全绿** —— 「弱断言分不出两种状态」）。
                        //    ✅ 修法两层：① `Battle/ImageQuad.cs` 的 `SetMaterial` **保留调用前的旧队列**（通用）；
                        //       ② `Shell/DeckInfoPopup.cs` 的 `ReassertButtonQueues()` 在状态翻转**之后**钉回绝对值。
                        // 🔴 **量与【红底】的相对档位**（原版语义：按钮画在窗底图之上），⛔ **不是跟我们自己的常量比**。
                        //    ⚠️ 覆盖 **8 颗钮里变灰会碰到的每一张 quad**（可见底 + 命中区）—— 只量可见那张的话，
                        //    「命中区掉档」这条静默错就漏了（它全透明，肉眼与截图都看不出来）。
                        var plateNode = FindChild(pr, "Generic Window Red Background Big");
                        var plateQ = plateNode != null ? plateNode.GetComponentInChildren<ImageQuad>(true) : null;
                        if (plateQ == null)
                            CheckTrue(false, "红底 `UI_Deck_Information_Back` 的 quad 没建出来 ⇒ 这条队列不变量判不了");
                        else
                        {
                            string[] qkeys = { "Btn:Practice Deck", "Btn:Edit Deck", "Btn:Select Deck",
                                               "Opt:Switch Deck Info", "Opt:Duplicate", "Opt:Share",
                                               "Opt:Share On Chat", "Opt:Delete" };
                            int nq = 0, badq = 0;
                            var badqList = new System.Text.StringBuilder();
                            foreach (var k in qkeys)
                            {
                                // `Btn:`/`Opt:` 两个前缀 = `Shell/DeckInfoPopup.cs` 登记 `_wbs` 时用的同一套键
                                var node = k.StartsWith("Opt:") ? pop.Opt(k.Substring(4)) : pop.Btn(k.Substring(4));
                                var wb = node != null ? node.GetComponent<WindowButton>() : null;
                                if (wb == null)
                                { badq++; badqList.Append("「").Append(k).Append("」没建出 `WindowButton`；"); continue; }
                                // 变灰碰到的就是这两组：`target`（可见底）+ 子树（全局唯一的 `Hit` 命中区）
                                // —— 那是 `Shell/WindowButton.cs` 的 `GrayTargets()` 的**前两组**，别只量一半。
                                // ⚠️ **2026-10-13 更正（铁律 5）**：本行原来写「与 `PromptPopup.GrayTargets()`
                                // **同一套**分组」—— 措辞不成立：现读是**四组去重**（`_nine` + `target` + 本钮子树
                                // + `TintOn` 那几颗；第三组 `A1719` 加、第四组 `A1725` 加）。
                                // ✅ 本窗这 8 颗钮的可见底走 `Img`（**单张 quad**、无 `_nine`）且**没有一处调
                                // `TintOn`** ⇒ **这两组就是全部**，下面 `qs` 的收法**不用改**
                                //（与 `Shell/DeckInfoPopup.ReassertButtonQueues()` 同一条口径）。
                                var qs = new List<ImageQuad>();
                                if (wb.target != null) qs.Add(wb.target);
                                foreach (var q in wb.GetComponentsInChildren<ImageQuad>(true))
                                    if (q != null && q != wb.target) qs.Add(q);
                                foreach (var q in qs)
                                {
                                    nq++;
                                    if (q.RenderQueue <= plateQ.RenderQueue)
                                    {
                                        badq++;
                                        badqList.Append("「").Append(k).Append("」的 `").Append(q.name)
                                                .Append("` 队列 ").Append(q.RenderQueue)
                                                .Append(" ≤ 红底 ").Append(plateQ.RenderQueue).Append("；");
                                    }
                                }
                            }
                            CheckTrue(nq >= 8, $"量到了 8 颗钮的 quad 共 **{nq}** 张（`target` + 命中区，至少各一张）");
                            // 🔴 **这条断言红的准确路径**（说清楚，别写成半真不假的）：
                            //    A85 之后**有两道**同时在保这个不变量 —— ① `ImageQuad.SetMaterial` 保留旧队列（通用）
                            //    ② 本窗 `ReassertButtonQueues()` 在状态翻转后钉回绝对值。**两道都去掉**才红
                            //    （= 修之前那个状态：变灰后那两张 quad 读出来是 3000 ≤ 红底 3120）。
                            //    ⚠️ 只去掉其中一道仍然绿 —— 这不是「断言没用」，而是**两道保险**，
                            //    所以别把「删一处它不红」当成断言失灵（要证它带电，就把两处一起去掉）。
                            CheckTrue(badq == 0,
                                      $"8 颗钮的**每一张** quad 都画在红底之上（核了 {nq} 张，红底队列 = {plateQ.RenderQueue}）"
                                      + "：⛔ 换灰材质（`Everguild/UI/Greyscale` 自带 3000）不许把显式队列抹掉 —— "
                                      + "（把 `ImageQuad.SetMaterial` 的 `if (q >= 0) m.renderQueue = q;` **与**本窗的"
                                      + " `ReassertButtonQueues()` 两道**一起**去掉，这一条就红）：" + badqList);
                        }
                    }
                    // 🆕 2026-10-04（A31）：**这三颗按 state 显隐** —— 收藏窗这条 = 原版 `DeckCollectionTab.OnItemSelected`
                    //   = **state 0**（判据 → `Shell/DeckInfoPopup.cs` 文件头那张表）。state 0 下：
                    //   `Edit Deck` 露（`state < 2`）· `Practice Deck` 露（`isPlayerDeck && state ∈ {0,2}`）·
                    //   `Select Deck` **不露** —— 原版判据是 `context.SelectButton != null`，而**6 个调用点全传 null**
                    //   （逐处实读 `DeckInfoContext__ctor` 的第 3 个实参）⇒ 本 build 里这颗钮从不出现。
                    CheckTrue(pop.IsItemShown("Btn:Edit Deck"), "state 0 ⇒ `Edit Deck` **露着**（原版 `state < 2`）");
                    CheckTrue(pop.IsItemShown("Btn:Practice Deck"), "state 0 ⇒ `Practice Deck` **露着**（`isPlayerDeck && state∈{0,2}`）");
                    CheckTrue(!pop.IsItemShown("Btn:Select Deck"),
                              "state 0 ⇒ `Select Deck` **不露**（原版 `context.SelectButton != null`，而 6 个调用点全传 null）");
                    var pb = pop.Btn("Practice Deck");
                    // 🔴 **2026-10-18（第四轮 P1）就地订正（铁律 5）**：期望值 `887.45`（= 三格全露时的中间那格）
                    //   —— **不成立**。原版那三颗在一个 `HorizontalLayoutGroup(spacing 36 · MiddleRight)` 里，
                    //   而 uGUI **只排 active 子件**；本夹具是 `state 0` 且 `SelectButtonProvided = false`
                    //   ⇒ `Select Deck` 藏 ⇒ **只剩两颗，整体右移**：容器内容盒 `669.3..1770.7`（宽 1101.4），
                    //   内容宽 `2×324.5 + 36 = 685` ⇒ 起点 = `669.3 + (1101.4−685) = 1085.70`
                    //   ⇒ `Practice Deck` 中心 = `1085.70 + 162.25 = **1247.95**`（真机参照图：练习从 0.57 屏宽起）。
                    //   实现 = `Shell/DeckInfoPopup.LayoutButtonRow()`（按可见集合重排，判据写在那个方法上）。
                    CheckNear(pb != null ? MenuCheck.DesignPx(pb).x : -1f, 1247.95f, 1f,
                              "`Practice Deck` 中心 x = **1247.95**（`Select Deck` 藏着 ⇒ 布局组只排两颗 ⇒ "
                            + "右对齐到 1770.70：1085.70…1410.20）");
                    var sb = pop.Btn("Select Deck");
                    // ⚠️ 量的是**关着的**那颗（`Transform.Find` 找得到关着的节点）—— `LayoutButtonRow()` 把
                    //    **藏起来的复位到基线**（原三格里的第三格）⇒ 它停在 `1446.20…1770.70`、中心仍是 1608.45。
                    //    🔴 别把这个数当成「布局排出来的」：它是**基线**，不看它在画面上有没有出现。
                    CheckNear(sb != null ? MenuCheck.DesignPx(sb).x : -1f, 1608.45f, 1f,
                              "`Select Deck`（藏着）复位在**基线** = 三格里的第三格 ⇒ 中心 x = **1608.45**");
                    CheckTrue(pop.Opt("Delete") != null && pop.Opt("Switch Deck Info") != null
                              && pop.Opt("Share") != null && pop.Opt("Share On Chat") != null
                              && pop.Opt("Duplicate") != null, "`Deck Options` 五个圆钮都在");
                    // 🆕 2026-10-04（A31）：这一扇是 **state 0**（原版 `DeckCollectionTab.OnItemSelected`）
                    //   ⇒ 四颗 `state == 0 && isPlayerDeck` 的**都该露**（两态对比的另一半 → 下面那节 A31）
                    CheckTrue(pop.IsItemShown("Opt:Share") && pop.IsItemShown("Opt:Share On Chat")
                              && pop.IsItemShown("Opt:Delete") && pop.IsItemShown("Opt:Duplicate"),
                              "state 0 ⇒ `Share`/`Share On Chat`/`Delete`/`Duplicate` **四颗都露着**");
                    var od = pop.Opt("Delete");
                    var os2 = pop.Opt("Switch Deck Info");
                    // 🔴 **2026-10-05 就地订正（铁律 5）**：**原文**（2026-10-03「A10」）写的是
                    //    「原版左→右 = **树序**：`Switch Deck Info` → `Duplicate` → `Share` → `Share On Chat`
                    //    → `Delete`……那句『`reverse=1` 把 GO 顺序倒过来』**实测不成立**」——
                    //    **那次订正本身是错的**。**错因**：读数出自**还不建模 `m_ReverseArrangement`** 的
                    //    `工具/menu_dump.py`（输出的是「正序 + 模板位」= **镜像读数**）⇒ A10 把一处**本来正确**的
                    //    实现（`Delete` 最左）改成了错的，并把这句话也抄进了这两条 `CheckNear`。
                    //    **实况**：uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`
                    //    （`startIndex = reverse ? Count−1 : 0` / `increment = reverse ? −1 : 1`）
                    //    ⇒ `reverse=1` 时**树序最后一个（`Delete`）落在最左**；跑后真值（左→右）
                    //    = `Delete` 1333.33 · `Share On Chat` 1427.31 · `Share` 1521.28 · `Duplicate` 1615.26
                    //    · `Switch Deck Info` 1709.23（步进 **93.976**）。算式与出处 →
                    //    `Shell/DeckInfoPopup.cs` 第 7) 节注释；重取命令见该处。
                    // 🔴 **下面三条断的是【建出来的世界 x 的序】、不是「`opts[]` 数组怎么写的」** ——
                    //    断数组顺序是同义反复（期望值会跟着实现走）；断世界 x 才能「改回镜像就红」。
                    var oNames = new[] { "Switch Deck Info", "Duplicate", "Share", "Share On Chat", "Delete" };
                    var oPx = new float[oNames.Length];
                    for (int oi = 0; oi < oNames.Length; oi++)
                    {
                        var ot = pop.Opt(oNames[oi]);
                        oPx[oi] = ot != null ? MenuCheck.DesignPx(ot).x : float.NaN;
                    }
                    // ① 逐颗严格递减（树序第 i 颗必须比第 i+1 颗**靠右** —— 这就是「视觉序 = 树序倒排」）
                    bool oReversed = true;
                    string oTrace = "";
                    for (int oi = 0; oi + 1 < oPx.Length; oi++)
                    {
                        if (!(oPx[oi] > oPx[oi + 1] + 1f)) oReversed = false;
                        oTrace += (oi > 0 ? " > " : "") + oNames[oi] + " " + oPx[oi].ToString("F2");
                    }
                    oTrace += " > " + oNames[oNames.Length - 1] + " " + oPx[oNames.Length - 1].ToString("F2");
                    CheckTrue(oReversed,
                              "★ `Deck Options` **视觉序 = 树序倒排**（原版 `m_ReverseArrangement = 1`）—— "
                              + "树序从左往右必须**逐颗更靠左**；实测：" + oTrace
                              + "（**改成镜像 ⇒ 这条红**）");
                    // ② 身位：最左 = `Delete` 中心 **1370.52**（= 组左沿 1263.74 + 69.59 + 74.386/2）；最右 = `Switch Deck Info`
                    CheckNear(od != null ? MenuCheck.DesignPx(od).x : -1f, 1370.52f, 1f,
                              "**最左**那颗是 `Delete`（中心 1370.52 = 1333.33 + 74.386/2；旧镜像读数 1746.43 是它【最右】的位置）");
                    CheckNear(os2 != null ? MenuCheck.DesignPx(os2).x : -1f, 1746.43f, 1f,
                              "**最右**那颗是 `Switch Deck Info`（中心 1746.43；旧镜像读数 1648.89 作废）");
                    // ③ 步进 = **93.976**（原版 `childSize 143.976 + spacing(−50)`）⇒ 首尾差 = 4 × 93.976 = 375.90
                    CheckNear(oPx[0] - oPx[4], 375.90f, 1f,
                              "…首尾差 = **4 × 93.976 = 375.90**（≠ 4 × (74.386 − 50) = 97.54 —— 旧模型会差 278.36）");

                    // ============ 🆕 2026-10-11（A299）：五颗圆钮的【射线那一面】= 子件矩形外扩 20 ============
                    //   与 A180（关窗钮）**完全同源**，判据也是同一套解包实读（2026-10-11 沿 `m_Children` 整棵核过）：
                    //     `Deck info Popup > Deck Options` 的五个子件 —— 每颗**自己的 `Image`** 都是
                    //     `m_RaycastTarget = 0` · pad 全 0；吃射线的是它的**两个子件** `Background` / `Icon`
                    //     （**都 `m_RaycastTarget = 1` · `m_RaycastPadding = (−20,−20,−20,−20)`**，负 = 外扩）。
                    //     子件矩形（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Options" --depth 3`）
                    //     = **`56.86 × 58.13`**（按钮自身 `74.386 × 75.605`）⇒ 外扩 20 = **96.86 × 98.13**。
                    //   ⛔ 期望值写**原版 dump 的子件矩形外扩 20 后**的字面量（dump 印到 1 位小数），
                    //     ⛔ **不写** `DeckInfoPopup.OptPad` / `OptW` / `OptIconW` —— 那是被测实现**传进去的实参**，
                    //     拿它当期望 = 同式自证（改实现它跟着绿）。
                    //   ⚠️ 节点与 quad 都量（同 A180）：`CheckAtPx` 管**节点**（抓「节点停在容器 (0,0)」那类错），
                    //     四条边 + 宽高管**画出来那张 quad**。
                    {
                        // 五颗各自的「子件矩形外扩 20」（按**树序**给，与上面 `oNames` 同一个集合 —— 铁律：
                        //   断言必须与【被测的那件事】用同一个集合）。x 逐颗不同、y 五颗同值：
                        //   `oX1[i] = 子件左沿 − 20` · `oX2[i] = 子件右沿 + 20`（子件左/右沿 = 上表 dump 的 1717.4/1774.2 …）
                        float[] oX1 = { 1697.4f, 1603.4f, 1509.4f, 1415.5f, 1321.5f };
                        float[] oX2 = { 1794.2f, 1700.3f, 1606.3f, 1512.3f, 1418.3f };
                        const float oTY = 118.2f, oBY = 216.3f;               // = 子件 138.2 / 196.3 各外扩 20
                        for (int oi = 0; oi < oNames.Length; oi++)
                        {
                            var hNode = pop.Opt(oNames[oi]);
                            // ① **节点**位置（2D、**z 不进比较** —— 那颗节点的 z 是按 `HitZFront` 有意挪过的，
                            //    A180 那次就是被 `CheckAt` 的「含 z」口径坑成恒红，见 `CheckAtPx` 的注释）。
                            //    🔴 **2026-10-11（A308）容差 1.5 → 0.3**：修完之后残差 ≤ 0.02px，
                            //      而「子件矩形退回居中」的偏差是 0.61/0.76 ⇒ 这一档正好夹在中间（见 ② 那条注释）。
                            //    改坏法：节点没摆（停在容器 (0,0) ≈ 画布中心 (960,540)）⇒ 差几百 px ⇒ 红；
                            //      子件矩形退回居中 ⇒ 差 0.61/0.76 ⇒ 也红（改前 1.5 那一档够不着）。
                            CheckAtPx(hNode, (oX1[oi] + oX2[oi]) * 0.5f, (oTY + oBY) * 0.5f, 0.3f,
                                      $"`Opt_{oNames[oi]}` 命中区**节点**在原版**射线区**中心（子件矩形外扩 20 后复算）");
                            var oQ = hNode != null ? hNode.GetComponentInChildren<ImageQuad>() : null;
                            if (oQ == null)
                            {
                                CheckTrue(false, $"`Opt_{oNames[oi]}` 命中区下面**没有 `ImageQuad`**"
                                                 + "（`PointerLayer` 的命中候选靠它 ⇒ 这颗等于点不动）");
                                continue;
                            }
                            float oW = oQ.WorldW * 108f, oH = oQ.WorldH * 108f;
                            float oCX = MenuCheck.DesignPx(oQ.transform).x, oCY = MenuCheck.DesignPx(oQ.transform).y;
                            // ② 四条边 + 宽高（绝对值 · **±0.3px** —— 容差是 2026-10-11 A308 从 ±1.5 收下来的，
                            //    理由见下面「为什么能收」那段）。
                            //    改坏法（逐条都会红）：去掉外扩 ⇒ 实测退回按钮矩形 `x1,130.20 → x1+74.386,205.805`
                            //    ⇒ 左沿差 +11.8 / 右沿 −10.6 / 上沿 +12.0 / 下沿 −10.5（全部 > 0.3）；
                            //    把 pad 加在**按钮矩形**上 ⇒ 每边多 ≈8.76（宽高变 114.386×115.605）；
                            //    **把子件矩形退回「居中」**（`(OptW−OptIconW)/2`）⇒ 左/上沿差 +0.61/+0.76、
                            //    右/下沿同量反向 ⇒ **左沿那条必然红**（判别力正是本件加的）；
                            //    整颗搬走 ⇒ 直接红。
                            // 🔴 **为什么能从 ±1.5 收到 ±0.3**：① 修完之后我们与这四个字面量的残差
                            //    **≤ 0.045px**（逐颗复算 `1333.33 + (4−i)×93.976 + 8.151 − 20` ⇒ 1697.385 /
                            //    1603.409 / 1509.433 / 1415.457 / 1321.481 vs 字面量 1697.4 / … / 1321.5）；
                            //    ② 字面量本身是原版 dump 的 1 位小数（±0.05）；③ 而「居中」那半边的偏差是
                            //    **0.61(x) / 0.76(y)** ⇒ 0.3 落在「残差 0.05」与「缺陷 0.61」之间。
                            //    ⚠️ **改前用 1.5 是「够不着缺陷」**（这条一直是绿的）—— 那正是 A308 能被漏掉的原因。
                            CheckNear(oCX - oW * 0.5f, oX1[oi], 0.3f,
                                      $"`Opt_{oNames[oi]}` 命中区**左沿** = **{oX1[oi]:F1}**（= 原版子件左沿 {oX1[oi] + 20f:F1} 外扩 20）");
                            CheckNear(oCX + oW * 0.5f, oX2[oi], 0.3f,
                                      $"…**右沿** = **{oX2[oi]:F1}**（= 原版子件右沿 {oX2[oi] - 20f:F1} 外扩 20）");
                            CheckNear(oCY - oH * 0.5f, oTY, 0.3f, "…**上沿** = **118.2**（= 原版子件上沿 138.2 外扩 20）");
                            CheckNear(oCY + oH * 0.5f, oBY, 0.3f, "…**下沿** = **216.3**（= 原版子件下沿 196.3 外扩 20）");
                            CheckNear(oW, 96.86f, 1.5f, "…**宽 = 96.86**（= 子件宽 56.86 + 2×20 外扩）");
                            CheckNear(oH, 98.13f, 1.5f, "…**高 = 98.13**（= 子件高 58.13 + 2×20 外扩）");
                            // ③ 🔴 **相对断言**（A180 那两条的同一手法）—— 外扩是加在**子件矩形**上的：
                            //    拿**同一颗自己**画出来的 `Face` 层当尺子（不读任何常量、也不看绝对矩形）。
                            //    ⛔ 为什么非要有这一组：绝对量只要有一份**抄错的文档**，实现与期望就会**一起偏**
                            //    （铁律：补相对断言）。
                            //    改坏法（两条差得很开，一眼能分辨）：去掉外扩 ⇒ 相对量 **0**（≠20）；
                            //    pad 加在**按钮矩形**上 ⇒ 相对量 **28.76 / 29.37**（≠20 / ≠20.63）—— 四条都红。
                            var oFace = FindChild(hNode.parent, "Face " + oNames[oi]);
                            var fQ = oFace != null ? oFace.GetComponentInChildren<ImageQuad>() : null;
                            if (fQ == null)
                                CheckTrue(false, $"`Face {oNames[oi]}` 那一层不在 ⇒ 下面四条「外扩量 = 20/边」没法量");
                            else
                            {
                                float fW = fQ.WorldW * 108f, fH = fQ.WorldH * 108f;
                                float fCX = MenuCheck.DesignPx(fQ.transform).x, fCY = MenuCheck.DesignPx(fQ.transform).y;
                                CheckNear((fCX - fW * 0.5f) - (oCX - oW * 0.5f), 20f, 0.5f,
                                          "★ A299：命中区**左外扩 = 20**（相对同颗的 `Face` 层）");
                                CheckNear((oCX + oW * 0.5f) - (fCX + fW * 0.5f), 20f, 0.5f,
                                          "★ …**右外扩 = 20**（同上）");
                                // 上下：`Face` 层是**等比内接**过的（原版子件那颗 `Image` 的 `m_PreserveAspect = 1`；
                                //   贴图 `40k_general_bt_yellow` 实测 **71×71 正方形**，而子件矩形是 56.86×58.12
                                //   ⇒ 内接缩的是【高】、x 不动）⇒ 它的上下缘比**子件矩形**各内缩
                                //   `(58.13 − 脸层高)/2`。原版子件矩形高写**字面量 58.13**（⛔ 不读 `OptIconH`），
                                //   于是相对量 = `20 + 那半个内缩量`（方图那一档 = 20.63）。
                                //   ⚠️ 这样写**不把「脸层是不是内接过」也一起钉死**——本条的题面只有「外扩 20/边」。
                                float fInset = (58.13f - fH) * 0.5f;
                                CheckNear((fCY - fH * 0.5f) - (oCY - oH * 0.5f), 20f + fInset, 0.5f,
                                          $"★ …**上外扩 = 20**（+{fInset:F2} = `Face` 层等比内接的半个内缩量）");
                                CheckNear((oCY + oH * 0.5f) - (fCY + fH * 0.5f), 20f + fInset, 0.5f,
                                          "★ …**下外扩 = 20**（同上）");
                            }
                            // ②b 🔴 **2026-10-11（A308）：原版子件矩形**不居中于按钮矩形****（真偏离 · 亚像素）
                            //    判据 = **逐字段实读**（`bundle_menus_assets_all`；五颗按钮的 `Background`/`Icon`
                            //    **十个节点逐值相同**，铁律 5·c 逐颗核过）：`m_AnchorMin (0.114, 0.12429)` ·
                            //    `m_AnchorMax (0.8733663, 0.88814)` · `m_AnchoredPosition (−0.14, 0.29)` ·
                            //    `m_SizeDelta 0.3774` · pivot `(0.5, 0.5)`（按钮自身 74.386×75.605）
                            //    ⇒ 左内缩 **8.151** / 右 **9.371** ⇒ **中心比按钮中心偏 −0.610**；
                            //       上内缩 **7.978** / 下 **9.498** ⇒ **偏 −0.760**（y 向下）。
                            //    尺子 = **同一颗自己的 `Bg` 层**（九宫底图 `UI_Button_Round_background` 是方图、
                            //    `m_PreserveAspect` 只缩高 ⇒ 它的**左右缘 = 按钮矩形的左右缘**、中心 = 按钮中心）。
                            //    ⛔ 不读 `DeckInfoPopup.OptInL` 那组常量（= 被测实现传进去的实参 ⇒ 同式自证）。
                            //    改坏法：`fx1/fy1` 退回「居中」（`(OptW−OptIconW)/2` · `(OptH−OptIconH)/2`）
                            //      ⇒ 两个相对量都变 **0** ⇒ 两条都红（`|0 − (−0.610)| = 0.610 > 0.3`）；
                            //      只改一半（例如只把 x 改回居中）⇒ **恰好红一半** —— 能分辨是哪半边漏了。
                            var oBg = FindChild(hNode.parent, "Bg " + oNames[oi]);
                            var bQ = oBg != null ? oBg.GetComponentInChildren<ImageQuad>() : null;
                            if (bQ == null)
                                CheckTrue(false, $"`Bg {oNames[oi]}` 那一层不在 ⇒ 「子件矩形不居中」两条没法量");
                            else
                            {
                                float bCX = MenuCheck.DesignPx(bQ.transform).x, bCY = MenuCheck.DesignPx(bQ.transform).y;
                                CheckNear(oCX - bCX, -0.610f, 0.3f,
                                          $"★ A308：`{oNames[oi]}` 的子件矩形中心 **比按钮中心偏左 0.610**"
                                          + "（原版左内缩 8.151 / 右 9.371，⛔ 不是「居中」的 8.763）");
                                CheckNear(oCY - bCY, -0.760f, 0.3f,
                                          $"★ …`{oNames[oi]}` 的子件矩形中心 **比按钮中心偏上 0.760**"
                                          + "（原版上内缩 7.978 / 下 9.498，⛔ 不是「居中」的 8.7425）");
                            }
                        }
                        // ④ 🔴 **深度 / 胜负**（A180 那条的同一手法，但**方向相反**）：外扩之后**相邻两颗的
                        //    命中区重叠**（**原版如此**：`96.865 − 93.976` ≈ **2.888px**；我们 2.884 ——
                        //    ⚠️ **2026-10-11（A308）就地订正**：原来把这 0.004px 归给「子件矩形那 0.6px 的亚像素差」，
                        //    那个**位置**差 A308 已收掉；剩下的是 `OptIconW` 的四舍五入（56.86 vs 解出的 56.8636）
                        //    ⇒ `96.86 − 93.976` vs `96.8636 − 93.976`）⇒ 那条带里谁赢**只能靠兄弟序**。
                        //    原版判据 = uGUI「**深度大者先**」（`Library/PackageCache/…/EventSystem/EventSystem.cs:239-240`
                        //    `return rhs.depth.CompareTo(lhs.depth)`；`Graphic.depth` = `canvasRenderer.absoluteDepth`
                        //    = 层级遍历序）⇒ **树序靠后的那颗赢** = 视觉上**靠左**的那一颗（`Delete` 是 `m_Children`
                        //    最后一个、`m_ReverseArrangement=1` ⇒ 它在最左）。
                        //    我们这侧 = **队列 → z**（`Shell/PointerLayer.cs` 的 `HitButton`：同队列比 z、
                        //    **越小越靠前**）⇒ 树序靠后的必须 z **更小**。
                        //    ⛔ 期望值不写常量：这里比的是**两颗 quad 的 z 相对大小**（题面即断言）。
                        //    改坏法：删掉 `Shell/DeckInfoPopup.cs` 第 7) 节那两句 z ⇒ 五个 z 全相等 ⇒ 红
                        //    （那就退回「看 `FindObjectsByType` 返回序」的运气判）。
                        float[] zOf = new float[oNames.Length];
                        for (int zi = 0; zi < oNames.Length; zi++)
                        {
                            var zT = pop.Opt(oNames[zi]);
                            var zQ = zT != null ? zT.GetComponentInChildren<ImageQuad>() : null;
                            zOf[zi] = zQ != null ? zQ.transform.position.z : float.NaN;
                        }
                        bool zOk = true;
                        string zTrace = "";
                        for (int zi = 0; zi + 1 < zOf.Length; zi++)
                        {
                            if (!(zOf[zi] > zOf[zi + 1])) zOk = false;      // 树序靠后 ⇒ z 更小 ⇒ 更靠前
                            zTrace += (zi > 0 ? " > " : "") + oNames[zi] + " " + zOf[zi].ToString("F4");
                        }
                        zTrace += " > " + oNames[oNames.Length - 1] + " " + zOf[zOf.Length - 1].ToString("F4");
                        CheckTrue(zOk,
                                  "★ A299：`Deck Options` 五颗的**深度序 = 原版兄弟序**（树序靠后的 z **更小** = 更靠前）"
                                  + " —— 原版 uGUI 是「深度大者先」，`Delete` 的树序最后 ⇒ 重叠带里它赢；实测 z 序：" + zTrace
                                  + "（删掉 `Shell/DeckInfoPopup.cs` 第 7) 节那两句 z ⇒ 五颗全同 z ⇒ 这条红）");
                        // ⑤ 跨组：原版窗根 `m_Children` 序 = `… Deck Options(5) → Info Panel(6) → Close(7)`
                        //    ⇒ 关窗钮比**整组**五颗都深 ⇒ 我们这五颗**一律排在关窗钮之后（z 更大）**。
                        //    改坏法：把这五颗也改成「往前挪」（负 z）⇒ 红，并且 A180 那条「关窗钮更深」跟着红。
                        var oCl = FindChild(pr, "CloseHit");
                        var clq = oCl != null ? oCl.GetComponentInChildren<ImageQuad>() : null;
                        if (clq != null)
                        {
                            bool zBehind = true;
                            string zBTrace = "";
                            for (int zi = 0; zi < zOf.Length; zi++)
                            {
                                if (!(zOf[zi] > clq.transform.position.z)) zBehind = false;
                                zBTrace += (zi > 0 ? ", " : "") + oNames[zi] + " " + zOf[zi].ToString("F4");
                            }
                            CheckTrue(zBehind,
                                      $"★ A299：五颗**全在关窗钮之后**（z 更大；关窗钮 z {clq.transform.position.z:F4}）—— "
                                      + "原版窗根最后一个子件是关窗钮 ⇒ 它最深；实测：" + zBTrace
                                      + "（把这五颗改成「往前挪」= 负 z ⇒ 这条红，A180 那条也跟着红）");
                        }
                        // ⑥ 🔴 **真命中路**（判别力最强的一条）：在那条 2.888px 重叠带里真问一次「谁赢」。
                        //    取点 (1417.2, 168.0)：**同时**落在 `Delete` 与 `Share On Chat` 的命中区里
                        //    （原版带 x∈[1415.46, 1418.35]；我们带 A308 之后 = **x∈[1415.46, 1418.34]**
                        //      —— ⚠️ **2026-10-11（A308）就地订正**：这里原来写「我们带 [1416.07, 1418.95]」，
                        //      那是子件矩形还没按实读内缩（左 8.151）画之前的值；取点 1417.2 在新旧两条带里
                        //      都是交集中心 ⇒ **不必改那个字面量**；
                        //      y=168.0 是五颗的按钮中心 = 两条带都含）。原版那里点下去是 **`Delete`**（树序最后一个）。
                        //    ⚠️ 这条同时兜住「z 打平 ⇒ 看 `FindObjectsByType` 返回序（**不保证**）」那种运气判。
                        //    改坏法：删 z 那两句 / 把胜负方向搞反 / 把 `Delete` 的命中区摆错 ⇒ 拿到别的钮或 null ⇒ 红。
                        var plA299 = PointerLayer.Instance;
                        if (plA299 != null)
                        {
                            // 🆕 2026-10-20（A1350 第二批）：这两个**字面量是设计 px**（原版 prefab 的坐标）
                            //   ⇒ 喂 `PointerLayer` 前要走「设计帧 → 指针帧」那一跳（单位缩放下逐位不变；
                            //   父链带缩放时直接把设计 px 喂进去会点到别处）。`owner` 取那一点所属的那颗钮。
                            var delT = pop.Opt("Delete");
                            var shareT = pop.Opt("Share On Chat");
                            var oWinP = MenuCheck.PointerFramePxFromDesign(new Vector2(1417.2f, 168.0f),
                                            delT != null ? delT : clq.transform);
                            var oWin = plA299.ButtonAt(oWinP.x, oWinP.y);
                            CheckTrue(oWin != null && oWin.name == "Opt_Delete",
                                      "★ A299：重叠带里**靠左那颗（`Delete`）赢** —— 原版靠兄弟序（它是 `m_Children` "
                                      + "最后一个 = 最深），我们靠 z；实得「"
                                      + (oWin == null ? "**null**（这一点上谁都没中）" : oWin.name) + "」");
                            // 反向对照：另一颗的**中心**（远离重叠带，1464.5 = `Share On Chat` 按钮中心 x）
                            // 必须还是它自己 —— 防「赢家恒为某一颗」这种假绿。
                            var oWin2P = MenuCheck.PointerFramePxFromDesign(new Vector2(1464.5f, 168.0f),
                                             shareT != null ? shareT : clq.transform);
                            var oWin2 = plA299.ButtonAt(oWin2P.x, oWin2P.y);
                            CheckTrue(oWin2 != null && oWin2.name == "Opt_Share On Chat",
                                      "★ …反向对照：远离重叠带的 `Share On Chat` **中心**拿到的还是它自己（实得「"
                                      + (oWin2 == null ? "**null**" : oWin2.name) + "」）");
                        }
                    }

                    Check(DeckInfoPopup.ListCols, 3,
                          "卡列表列数 = **3** = floor((1140 − 15 − 15 + 11) ÷ 371)（照 GridLayoutGroup 那套算）");
                    CheckTrue(pop.Rows.Count > 0, $"卡组内容画了 {pop.Rows.Count} 行");
                    // 🔴 **2026-10-19（自检断言线 S2 · `A1545`）**：上面 `ListCols` 是**常量公式**（= 自证）
                    //    ⇒ 这里从**实际摆出来的行**量列数（每列一个不同的 x 中心），并断最后一列右缘不越 `Content`。
                    //    加一张防御卡（31 行 ⇒ ⌈31/10⌉ = 4 列）时**先在这里红**，而不是只留下「第 4 列被裁」的观感。
                    {
                        var colXs = new List<float>();
                        foreach (var rr in pop.Rows)
                        {
                            float cx = MenuCheck.DesignPx(rr).x;
                            bool seen = false;
                            foreach (var x in colXs) if (Mathf.Abs(x - cx) < 0.5f) { seen = true; break; }
                            if (!seen) colXs.Add(cx);
                        }
                        int wantCols = Mathf.CeilToInt(pop.Rows.Count / (float)DeckInfoPopup.ListRowCap);
                        Check(colXs.Count, wantCols,
                              $"★ A1545：**实际列数** = ⌈{pop.Rows.Count} 行 ÷ 每列 {DeckInfoPopup.ListRowCap}⌉ = **{wantCols}**"
                            + $"（从行心的不同 x 量出来 {colXs.Count} 列）"
                            + "｜🧨 夹具再加一张卡（31 行 ⇒ 4 列）⇒ 这条先红（不是只留下「第 4 列被裁」的观感）");
                        float maxRight = float.MinValue;
                        foreach (var rr in pop.Rows)
                            maxRight = Mathf.Max(maxRight, MenuCheck.DesignPx(rr).x + DeckInfoPopup.RowW * 0.5f);
                        CheckTrue(maxRight <= DeckInfoPopup.PanelR + 0.5f,
                                  $"★ A1545：最后一列右缘 **{maxRight:F1}** ≤ `Content` 右缘 {DeckInfoPopup.PanelR:F0}"
                                + "（超了就是「第 4 列溢出 / 被裁」那一档）"
                                + "｜🧨 把列宽 / 间距改宽 ⇒ 红");
                    }
                    var r0 = pop.Rows.Count > 0 ? pop.Rows[0] : null;
                    CheckNear(r0 != null ? MenuCheck.DesignPx(r0).x : -1f, 854f, 1f,
                              "第 1 行中心 x = **854**（Info Panel 左 659 + pad 15 + 180）");
                    // 🔴 **2026-10-12（`A1531⑥` 连带 · 本会话按铁律 5 就地改期望值）**：这一条原来是 **`247.10`**
                    //   （= `218.10 + 58/2`）—— 那一档**漏了 `Content` 的 `m_Padding.m_Top`**。
                    //   原版字段实读（`工具/_probe_deckinfo.py bundle_menus_assets_all "Deck List"` 亲读）
                    //   = `m_Padding = {L15, R0, **T15**, B0}`；uGUI 那一档判据 = `LayoutGroup.GetStartOffset`
                    //   （本机 `LayoutGroup.cs:193-202`）+ `GridLayoutGroup.cs:350-351`
                    //   —— 本组 `m_ChildAlignment = 0(UpperLeft)` ⇒ 起点 = `Content` 左上 + `(15, 15)`。
                    //   旁证：同族 `Shell/PracticeModePopup.cs` 的 `DlPadL = 22 / DlPadT = 4` **两侧都加了**。
                    //   ⇒ 第一格顶边 = `218.10 + 15 = 233.1`、行心 = `233.1 + 29` = **262.10**（原来 247.1、整块**偏上 15px**）。
                    //   ⚠️ **x 那一侧本来就加了 15**（`RowL = PanelL + RowPadL`）—— **只有 y 漏了**，
                    //      所以上面那条 x = 854 **一个字不用改**。
                    //   🧨 **改坏法**：把 `Shell/DeckInfoPopup.cs` 的 `RowT` 改回 `PanelT`（= 漏 `m_Top`）⇒ 本条红。
                    CheckNear(r0 != null ? MenuCheck.DesignPx(r0).y : -1f, 262.10f, 1f,
                              "第 1 行中心 y = **262.10**（`218.10` + 原版 `m_Padding.m_Top` **15** + `58/2`）"
                            + "｜🧨 `RowT` 改回 `PanelT`（漏掉 `m_Top` 那半截）⇒ 本条红（整块偏上 15px）");

                    // ============================================================ 🆕 **2026-10-12（`A1531③①④` 的判别式）**
                    //   两条**靠上面 x/宽/高断不出来**的：**逐列填充** 与 **排序**。
                    //   判据一律读 `Shell/DeckInfoPopup.cs` 的 `BuildList`（⛔ 不读它的常量当期望值）：
                    //     · **填充**：原版 `Content` 的 `GridLayoutGroup` 是 **`m_StartAxis = 1(Vertical)`**
                    //       + `m_Constraint = 0(Flexible)` ⇒ uGUI `GridLayoutGroup.cs:350-351` =
                    //       `positionX = i / cellCountY` · `positionY = i % cellCountY`，`cellCountY` = 一列的行容量 **10**
                    //       ⇒ **先往下填满一列（10 行）再换下一列**。
                    //     · **排序**：原版 `DeckListDrawer__Reorder.c:33-40` = `OrderBy(键1).ThenBy(键2)`，
                    //       键1 = `card+0x78` = **费用** · 键2 = `GetLocalizedCardName` = **本地化卡名**。
                    {
                        const int k10 = 10;                       // 一列 10 行（`Shell/DeckInfoPopup.cs` 的 `ListRowCap`）
                        // ③-a **逐列填充**：下标 10 = **第 2 列第 1 格**
                        //   —— 期望 x = 第 1 行的 x + (格宽 360 + 间距 11) = `854 + 371` = **1225**；
                        //     期望 y = 与第 1 行**同高**（= 262.10）。
                        //   🔴 **判别力在 y 上**：`Horizontal` 那一档（`i % 3, i / 3`）算出来的 x **也是 1225**，
                        //     但 y = `262.10 + 3×(58+4.5)` = **449.60** ⇒ **只断 x 抓不到**，必须连 y 一起断。
                        CheckTrue(pop.Rows.Count > k10,
                                  $"（前提）行数 > {k10}（实测 {pop.Rows.Count}）—— 否则下面那条空转");
                        if (pop.Rows.Count > k10)
                        {
                            var r10 = pop.Rows[k10];
                            CheckNear(r10 != null ? MenuCheck.DesignPx(r10).x : -1f, 1225f, 1f,
                                      "★ A1531③ 第 11 行（下标 10）落**第 2 列**：中心 x = **1225**（第 1 行 854 + 格宽 360 + 间距 11）");
                            CheckNear(r10 != null ? MenuCheck.DesignPx(r10).y : -1f, 262.10f, 1f,
                                      "★ …而且落在**那一列最上面那一格**（y = 262.10 = 与第 1 行同高）"
                                    + " —— 原版 `m_StartAxis = Vertical` ⇒ **逐列**"
                                    + "｜🧨 把 `int c = i / rowCap, rr = i % rowCap` 改回 `i % cols, i / cols`（逐行）"
                                    + "⇒ **x 仍是 1225**、**y 变 449.60** ⇒ 本条红（只断 x 的那一版抓不到）");
                        }

                        // ③-b **排序**：逐行费用**单调不减**
                        //   前提① = 夹具那副的**原始次序本来就不是费用序**（否则「排没排」根本断不出来）
                        //     —— 2026-10-12 现算（`RuleEngine/Resources/cards_engine.json` 逐张过一遍夹具那段挑选）：
                        //        30 张 unit 的原始费用序 = `6,6,5,4,5,8,2,7,6,5,9,2,6,4,6,7,5,6,3,7,8,2,3,4,4,6,1,3,6,1`，
                        //        **逆序对 237 个** ⇒ 判别力成立。
                        //   前提② = 防御卡被原版**钉在第 0 格**（`Reorder.c:135`）⇒ 它那一行**不属于排序段**，
                        //     断单调时要**跳过**（夹具那副今天没有防御卡，两个分支都写全是为了换数据不静默）。
                        var deck0 = CollectionData.Raw(0);
                        var rawCosts = new List<int>();
                        if (deck0 != null)
                            foreach (var id in deck0.CardIds)
                            {
                                var cd = CollectionData.Card(id);
                                if (cd != null) rawCosts.Add(cd.Cost);
                            }
                        bool rawInv = false;
                        for (int a = 0; a < rawCosts.Count && !rawInv; a++)
                            for (int b = a + 1; b < rawCosts.Count; b++)
                                if (rawCosts[a] > rawCosts[b]) { rawInv = true; break; }
                        CheckTrue(rawInv,
                                  "（前提·本条的判别力）夹具那副牌的**原始次序不是费用序** —— 是的话「排没排」断不出来"
                                + $"（实测原始费用序 {rawCosts.Count} 张）");

                        var defC = deck0 != null && !string.IsNullOrEmpty(deck0.DefensiveId)
                                 ? CollectionData.Card(deck0.DefensiveId) : null;
                        bool defPinned = defC != null && DeckInfoPopup.IsDefensiveRow(defC);
                        // 夹具那副**有没有**防御卡决定排序段从第几行起：
                        //   有 ⇒ 原版把它钉在第 0 格（`Reorder.c:135` 的 `SetSiblingIndex`），**那一行不属于排序段**；
                        //   没有 ⇒ 排序段 = 全部行（今天就是这一支，两个分支都写全 = 换数据不静默）。
                        int sortFrom = defPinned ? 1 : 0;

                        var gotCost = new List<int>();
                        var gotName = new List<string>();
                        int parseFail = 0;
                        for (int i = sortFrom; i < pop.Rows.Count; i++)
                        {
                            var ct = FindChild(pop.Rows[i], "Cost Text");
                            string cs = ct != null ? TextOf(ct) : null;
                            int v;
                            if (!int.TryParse(cs, out v)) { parseFail++; v = -1; }
                            gotCost.Add(v);
                            gotName.Add(TextOf(FindChild(pop.Rows[i], "Name")));
                        }
                        Check(parseFail, 0,
                              $"（前提）排序段 {gotCost.Count} 行的 `Cost Text` 都读得出整数（读不出 {parseFail} 行）");
                        int badOrder = 0; string firstBad = null;
                        for (int i = 1; i < gotCost.Count; i++)
                            if (gotCost[i] < gotCost[i - 1])
                            {
                                badOrder++;
                                if (firstBad == null) firstBad = $"{i}: {gotCost[i - 1]}→{gotCost[i]}";
                            }
                        Check(badOrder, 0,
                              "★ A1531① **逐行费用单调不减**（原版 `Reorder.c:33-40` 的键1 = `card+0x78` = 费用；"
                            + "实测 " + string.Join(",", gotCost.ConvertAll(x => x.ToString()).ToArray()) + "）"
                            + "｜🧨 删掉 `Shell/DeckInfoPopup.cs` 的 `cards.Sort(...)` ⇒ 回到原始次序"
                            + "（**逆序对 237 个**）⇒ 本条红"
                            + (firstBad == null ? "" : "｜首个逆序 " + firstBad));

                        // ③-c **行名 = 本地化卡名**（原版键2 = `GetLocalizedCardName`）：
                        //   期望值**从卡表现算**（`CardText.Name(cd.Name, cd.NameZh)`，与左栏 `DeckRuntime` 逐字同句），
                        //   ⛔ 不读实现传进去的那个串。**按多重集合比**（两边各自排序后逐位比）⇒ 与行序无关、
                        //   也就**没有把排序规则抄第二份**（铁律「两处写同一条规则 = 迟早不一致」）。
                        //   🔴 **鉴别力依赖跑批语档 = 中文档**（`CardText.Name` 里有 `Zh` 那道闸 —— 英文档下
                        //      「印英文名」与「走本地化」是同一个串）⇒ 本机出厂默认中文，今天有鉴别力；
                        //      ⚠️ 这与账 `A1517` 是**同一族限制**，本批**不**在断言里钉语档（那会弄出假红）。
                        var wantNames = new List<string>();
                        if (deck0 != null)
                            foreach (var id in deck0.CardIds)
                            {
                                var cd = CollectionData.Card(id);
                                if (cd != null) wantNames.Add(CardText.Name(cd.Name, cd.NameZh));
                            }
                        if (defPinned) wantNames.Add(CardText.Name(defC.Name, defC.NameZh));
                        gotName.Sort(); wantNames.Sort();
                        Check(gotName.Count, wantNames.Count,
                              $"★ A1531④ 排序段画了 {wantNames.Count} 行（= 卡组张数）");
                        int nameBad = 0; string nameDiag = "";
                        for (int i = 0; i < gotName.Count && i < wantNames.Count; i++)
                            if (!string.Equals(gotName[i], wantNames[i], System.StringComparison.Ordinal))
                            {
                                nameBad++;
                                if (nameDiag.Length < 160)
                                    nameDiag += $" 实得「{gotName[i]}」≠ 期望「{wantNames[i]}」;";
                            }
                        Check(nameBad, 0,
                              "★ A1531④ **行名逐字 = 本地化卡名**（`CardText.Name(Name, NameZh)`，与左栏 `DeckRuntime` 同句）"
                            + "｜🧨 把 `Shell/DeckInfoPopup.cs` 那一句改回 `card.Name`（英文）⇒ 顺序会错、本条红"
                            + "（⚠️ 鉴别力靠「跑批语档 = 中文」那一档，见上面那条注）" + nameDiag);
                    }

                    // ---------------- 🆕 2026-10-03（§三第29条 A10）补的四件 + 两处订正 ----------------
                    //   逐值 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`
                    //   🔴 两个**新抓到的真缺陷**：`Deck Details` 整块原来用的是**布局组跑之前的模板位**、
                    //      `Deck Options` 顺序反了（上面那条已改）。这里把订正后的值钉住。
                    Section("`Deck info Popup` A10：游戏模式图标 / `Deck Info` 抽屉 / 两个抽屉切换 / 两处订正");
                    {
                        // ① `Deck Details` 跑后真值（原来那套是模板位 ⇒ Army Icon 画到容器外）
                        // 🔴 **2026-10-18（`Game Mode Icon` 改成两态）**：`Game Mode Icon` 现在是「有图才画」，
                        //   **隐藏态**下原版 HLG 把后面几颗**整体左移 108.87**（= 图标 100 + 斜线 8.87）
                        //   ⇒ `Army Icon` 767.9/867.9 → **659.03/759.03**、两行名的左沿 872.9 → **764.03**。
                        //   判据 → `资料/普查产出_1018/R_DeckInfoPopup_对账.md`（`Game Mode Icon` / `Army Icon` 两行）；
                        //   实现 → `Shell/DeckInfoPopup.cs` 的 `showGmIcon` / `ddShift`。
                        //   ⚠️ 夹具 `CollectionData.DeckAt(0)` 是 **classic(0)** ⇒ 本节先断**隐藏态**；
                        //   有图标那一态在下面 **②′** 原地重建再断一次 —— **两态都断**，⛔ 别只断一边。
                        CheckAt(FindChild(pr, "Army Icon"), 659.03f, 759.03f, 106.7f, 216.7f,
                                "`Army Icon`（**隐藏态** · 2026-10-18：`Game Mode Icon` 按原版机制隐藏"
                              + "（判据 → `R_DeckInfoPopup_对账.md`）；位置随之左移 108.87 ⇒ 767.9/867.9 → 659.03/759.03）");
                        // 2026-10-18：`Game Mode Separator` 的显隐**随** `Game Mode Icon`（原版 `act` 联动；
                        //   我们这侧 `Build()` 里那个 `if (showGmIcon)` 把它整颗包住）⇒ classic 下**整颗不建**
                        //   （⛔ 不是「建了但关着」——判据同 `R_DeckInfoPopup_对账.md` 的 Separator 行）。
                        CheckTrue(FindChild(pr, "Game Mode Separator") == null,
                                  "`Game Mode Separator` 在 classic（无图标）下**整颗不建**"
                                + "（2026-10-18：`Game Mode Icon` 按原版机制隐藏（判据 → `R_DeckInfoPopup_对账.md`）；位置随之左移 108.87）");
                        // ⚠️ `Deck Name` / `Warlord Name` 是 **`Align.Left`** ⇒ `AlignLeftOn` **会把 Label 的节点挪走**
                        //    ⇒ 拿节点中心去比 `CheckAt` 会差 100+px（第一版就是这么红的）。
                        //    **量它渲出来的左缘**（同 `已知的 MainMenuScene` 那两句的写法）。
                        {
                            var dnT = FindChild(pr, "Deck Name");
                            var dnl = dnT != null ? dnT.GetComponentInChildren<Label>() : null;
                            // 🆕 **2026-10-16（A796′）换口**：半宽（`WorldW × 54`）→ `LabelRenderedPx`（TMP `textBounds`）；
                            //    中心仍是**节点位置**（`AlignLeft` 把 Label 节点挪走那一档没动）。
                            // 🆕 **2026-10-18**：左缘 872.9 → **764.03**（隐藏态左移 108.87；判据同上）。
                            // 🔴 先断「量得到 `Label`」—— 原来那个 `if (dnl != null)` 没有 `else`
                            //    ⇒ 取不到时整条**静默空转**（同 A52-F10 那个坑）。
                            CheckTrue(dnl != null, "（前提）`Deck Name` 底下量得到 `Label`（量不到 ⇒ 下面那条等于没验）");
                            if (dnl != null)
                                CheckNear(MenuCheck.DesignPx(dnl.transform).x - LabelRenderedPx(dnl).x * 0.5f, 764.03f, 2f,
                                          "`Deck Name` 左缘 = **764.03**（隐藏态；2026-10-18：`Game Mode Icon` 按原版机制隐藏"
                                        + "（判据 → `R_DeckInfoPopup_对账.md`）；位置随之左移 108.87 ⇒ 872.9 → 764.03）");
                            var wnT = FindChild(pr, "Warlord Name");
                            var wnl = wnT != null ? wnT.GetComponentInChildren<Label>() : null;
                            CheckTrue(wnl != null, "（前提）`Warlord Name` 底下量得到 `Label`（同上）");
                            if (wnl != null)
                                CheckNear(MenuCheck.DesignPx(wnl.transform).x - LabelRenderedPx(wnl).x * 0.5f, 764.03f, 2f,
                                          "`Warlord Name` 左缘 = **764.03**（隐藏态 · 同 `Deck Name`）");
                        }
                        // ② `Game Mode Icon`（A10 补的第一件）—— **classic 态：不建**
                        //   原版 `sprite = 0`、运行期 `GameModesIconCollection.GetIcon(deck.GameMode)`，
                        //   无匹配项返回 0 ⇒ 整颗 `SetActive(false)`；我们这侧 `showGmIcon = GameMode == Skirmish`。
                        //   2026-10-18：`Game Mode Icon` 按原版机制隐藏（判据 → `R_DeckInfoPopup_对账.md`）；
                        //   位置随之左移 108.87。
                        CheckTrue(FindChild(pr, "Game Mode Icon") == null,
                                  "`Game Mode Icon` 在 **classic(0)** 下**不建**"
                                + "（2026-10-18：`Game Mode Icon` 按原版机制隐藏（判据 → `R_DeckInfoPopup_对账.md`）；位置随之左移 108.87）");

                        // ②′ **另一态 = skirmish(13)**：有图标（`40k_gamemode_icon_skirmish`）、位置回到 767.9 / 872.9。
                        //   做法照本文件既有写法（`Practice Deck` 那一段的 `flipSaved/flipIdx`：**只改内存、验完还原**），
                        //   重建走 `pop.Open()`（= `Build()`，与生产 `WindowsManager.OpenWindow → TryOpen → Open`
                        //   同一条；先例 → `Editor/MainMenuScene.cs` 的「原地重建」）。
                        //   ⚠️ 断完**必须**改回原值再重建一次 —— 下面几小节（两个抽屉 / 立绘 / 队列）用的还是这棵树。
                        //   ⚠️ 不走 `win.OpenDeckInfo(0)` 另开一扇：那会把这一扇 `ToBackground()`（`SetActive(false)`），
                        //      后面那些 `GetComponentInChildren<Label>()`（默认跳过关着的）就全量不到了 ⇒ 会变成静默空转。
                        {
                            var raw0 = CollectionData.Raw(0);
                            CheckTrue(raw0 != null, "（前提）第 1 套卡组取得到（②′ 要临时改它的 `GameMode`）");
                            if (raw0 != null)
                            {
                                int gmSaved = raw0.GameMode;
                                raw0.GameMode = (int)GameMode.Skirmish;
                                pop.Open();                       // 原地重建（只改内存，没写盘）
                                var sr = pop.transform;
                                var sgmi = FindChild(sr, "Game Mode Icon");
                                var sgq = sgmi != null ? sgmi.GetComponentInChildren<ImageQuad>() : null;
                                CheckTrue(sgq != null && sgq.Texture != null
                                          && sgq.Texture.name == "40k_gamemode_icon_skirmish",
                                          "②′ `Game Mode Icon`（**skirmish 态**）= `40k_gamemode_icon_skirmish`"
                                        + "（实得 " + (sgq == null || sgq.Texture == null
                                                        ? "**没有图 / 没建出来**" : "`" + sgq.Texture.name + "`") + "）");
                                CheckAt(sgmi, 659.0f, 759.0f, 106.7f, 216.7f,
                                        "②′ `Game Mode Icon` 的矩形（有图标态 = 跑后真值 659.0→759.0）");
                                CheckAt(FindChild(sr, "Game Mode Separator"), 759.0f, 767.9f, 106.7f, 216.7f,
                                        "②′ `Game Mode Separator` **建出来了**（显隐随 `Game Mode Icon`）且位置 = 跑后真值");
                                CheckAt(FindChild(sr, "Army Icon"), 767.9f, 867.9f, 106.7f, 216.7f,
                                        "②′ `Army Icon`（**有图标态**：不左移，仍是 767.9→867.9）");
                                var sdnT = FindChild(sr, "Deck Name");
                                var sdnl = sdnT != null ? sdnT.GetComponentInChildren<Label>() : null;
                                CheckTrue(sdnl != null, "（前提）②′ `Deck Name` 底下量得到 `Label`");
                                if (sdnl != null)
                                    CheckNear(MenuCheck.DesignPx(sdnl.transform).x - LabelRenderedPx(sdnl).x * 0.5f, 872.9f, 2f,
                                              "②′ `Deck Name` 左缘 = **872.9**（有图标态不左移）");
                                var swnT = FindChild(sr, "Warlord Name");
                                var swnl = swnT != null ? swnT.GetComponentInChildren<Label>() : null;
                                CheckTrue(swnl != null, "（前提）②′ `Warlord Name` 底下量得到 `Label`");
                                if (swnl != null)
                                    CheckNear(MenuCheck.DesignPx(swnl.transform).x - LabelRenderedPx(swnl).x * 0.5f, 872.9f, 2f,
                                              "②′ `Warlord Name` 左缘 = **872.9**（同上）");
                                raw0.GameMode = gmSaved;          // 还原（夹具是 classic）
                                pop.Open();                       // 把树建回 classic 那一版给后面几小节用
                            }
                        }
                        // ③ 两个抽屉：出厂只有 `Deck List` 开
                        var dlist = FindChild(pr, "Deck List");
                        var dinfo = FindChild(pr, "Deck Info");
                        CheckTrue(dlist != null && dlist.gameObject.activeSelf, "`Deck List` 出厂开着");
                        CheckTrue(dinfo != null && !dinfo.gameObject.activeSelf,
                                  "`Deck Info` **建成但关着**（原版出厂 `INACT`）");
                        CheckTrue(!pop.InfoDrawerShown, "自检读数：现在展示的是 `Deck List`");
                        // ④ `Switch Deck Info` ⇒ 真切（两个互斥）
                        var swHit = pop.Opt("Switch Deck Info");
                        var swWb = swHit != null ? swHit.GetComponent<WindowButton>() : null;
                        CheckTrue(swWb != null, "`Switch Deck Info` 有点击区");
                        if (swWb != null)
                        {
                            swWb.ClickForTest();
                            CheckTrue(pop.InfoDrawerShown, "点它 ⇒ 切到 `Deck Info`");
                            CheckTrue(!FindChild(pr, "Deck List").gameObject.activeSelf
                                      && FindChild(pr, "Deck Info").gameObject.activeSelf,
                                      "两个抽屉**互斥**（`Deck List` 关了、`Deck Info` 开了）");
                            // ⑤ `Deck Info` 里的三件：标题 / 费用曲线 9 行 / 卡背
                            var di = FindChild(pr, "Deck Info");
                            CheckText(TextOf(FindChild(di, "Deck Information Cost/balance text")), Loc.T("Demo/CardCostBalance"),
                                      "费用那条标题（⚠️ 原版 prefab 里是**葡语占位** `Cartas / Coste` ⇒ 这行文案是我们挑的）");
                            Check(pop.CostRowCounts.Count, 9, "费用曲线 **9 行**（费用 0..8 —— 原版序列化就是 9 行）");
                            int sum = 0; for (int k = 0; k < pop.CostRowCounts.Count; k++) sum += pop.CostRowCounts[k];
                            CheckTrue(sum > 0, $"…曲线里**真的有张数**（合计 {sum} 张；这副的卡都算进去了）");
                            var cbk = FindChild(di, "Cardback");
                            var cbq = cbk != null ? cbk.GetComponentInChildren<ImageQuad>() : null;
                            // ⚠️ 卡背图取不到是**已知缺口**（`Art/cards/back_*.png` 只有 4 个阵营 ⇒
                            //    多数阵营**没有「默认卡背」**，正本 §七 ③ 记着）⇒ 这条只断
                            //    「**有图就必须画出来**」；取不到时 `DeckInfoPopup` 会**出声**（日志里那条
                            //    「卡背取不到（`` / 阵营 X）⇒ 那一层不画」），**不是静默**。
                            var cbInfo = CollectionData.DeckAt(0);
                            var cbTex = CardArt.DeckCardback(cbInfo.CardbackId, cbInfo.Faction);
                            CheckTrue(cbTex == null || (cbq != null && cbq.Texture != null),
                                      "`Cardback`：**有图就必须画出来**（实测图 "
                                      + (cbTex != null ? "有" : "**没有**（已知缺口，已出声）") + "）");
                            CheckTrue(FindChild(di, "Lore Text") == null,
                                      "`Lore Text` **不建**（原版出厂 `act=N` + 我们引擎没有 lore 字段）");
                            swWb.ClickForTest();
                            CheckTrue(!pop.InfoDrawerShown, "再点 ⇒ 切回 `Deck List`");
                        }
                        // ⑥ 督军立绘可点（原版 `EverguildButton` ⇒ 开卡详情窗）
                        var wh = FindChild(pr, "WarlordHit");
                        CheckTrue(wh != null && wh.GetComponent<WindowButton>() != null,
                                  "`Warlord Image` 上有点击区（原版那层就是 `EverguildButton`）");
                        // 🆕 **2026-10-07（波 4 件① A142②）：立绘命中区的四条边。**
                        //   判据 = 原版 prefab `Deck info Popup > Warlord Image` 那颗 `Image` 的
                        //   **`m_RaycastPadding`（UGUI 分量序 **L, B, R, T**）** ：
                        //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-7131536541767857752.json`
                        //   = `(246.8, 84.44, 338.6, 132.38)`（`m_GameObject` 指回
                        //   `GameObject/Warlord Image_-6735770576364533336.json`）。
                        //   那颗的 rect（同目录 `RectTransform/RectTransform_8411164374367242664.json`：
                        //   anchor (0.5,0.5) · `m_AnchoredPosition = (-514.981, -534.0)` ·
                        //   `m_SizeDelta = 1107.994²` · `m_Pivot = (0.5, 0.0)`）= `-108.978,-33.994 → 999.016,1074.0`
                        //   ⇒ **命中区 = 四边各往里缩**（正分量 = 缩；符号判据见实现侧 `WarlordHit` 那段）：
                        //     x1 = −108.978 + 246.80 = **137.82** · y1 = −33.994 + 132.38 = **98.39**
                        //     x2 =  999.016 − 338.60 = **660.42** · y2 = 1074.000 − 84.44 = **989.56**
                        //   ⛔ 这四个数写**原版 prefab 复算出来的字面量**，⛔ **不写** `DeckInfoPopup.WarlordPad` /
                        //      `WarlordL..B` —— 那是被测实现传进去的实参（同式自证：改实现它照样绿）。
                        //   ⚠️ **与立绘 `Img` 的 `keepAspect` 无关** —— 命中区只由 `WarlordPad` 决定；
                        //      「渲染矩形拉满 1108²」是**另一条**（下面那段），两条分开、别混在一句里。
                        const float WnL = 137.82f, WnT = 98.39f, WnR = 660.42f, WnB = 989.56f;
                        var wq = wh != null ? wh.GetComponentInChildren<ImageQuad>() : null;
                        if (wq == null)
                            CheckTrue(false, "`WarlordHit` 下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 立绘等于点不动）");
                        else
                        {
                            float wqW = wq.WorldW * 108f, wqH = wq.WorldH * 108f;
                            float wqX = MenuCheck.DesignPx(wq.transform).x;
                            float wqY = MenuCheck.DesignPx(wq.transform).y;
                            CheckNear(wqX - wqW * 0.5f, WnL, 1.5f,
                                      "立绘命中区**左沿** = **137.82**（原版 `m_RaycastPadding` 复算 · 1108² 里那 246.8 的缩）");
                            CheckNear(wqY - wqH * 0.5f, WnT, 1.5f, "…**上沿** = **98.39**");
                            CheckNear(wqX + wqW * 0.5f, WnR, 1.5f, "…**右沿** = **660.42**");
                            CheckNear(wqY + wqH * 0.5f, WnB, 1.5f, "…**下沿** = **989.56**");
                        }
                        // 🆕 **2026-10-07（波 4 件① A142①）：立绘【渲染】矩形 = 原版那样拉满 1108²。**
                        //   判据 = 原版那颗 `Image` 的 **`m_PreserveAspect = 0`** + `m_Type = 0`(Simple)
                        //   + rect `1107.994²`（同一份 JSON，见上）⇒ **拉伸画满**，不是等比内接。
                        //   我们原来传 `keepAspect: true` ⇒ 按贴图自己的比例缩、左右留边
                        //   （督军那族贴图是 671×1024 ⇒ 宽只剩 ≈ 1108×671/1024 ≈ **726**）。
                        //   🔴 这是那处改动的**唯一自动化尺子**（批处理能量几何、量不到「像不像」——
                        //     观感差仍归 `资料/真Play待验清单.md`）。
                        var wlImg = FindChild(pr, "Warlord Image");
                        var wlQ = wlImg != null ? wlImg.GetComponentInChildren<ImageQuad>() : null;
                        if (wlQ == null)
                            CheckTrue(false, "`Warlord Image` 那一层没建出来（督军立绘取不到 ⇒ 实现里已经出声）");
                        else
                        {
                            // 🔴 **2026-10-18（督军方形立绘族落地后）**：原版这颗喂的是 **2048² 方形**
                            //   `UI_Deck_Warlord_<名>` ⇒ `m_PreserveAspect = 0` 拉满 1108²；
                            //   但快照之后新增的 16 位督军**没有**这张图，`CardArt.DeckHero` 会退回
                            //   卡面插图（671×1024 竖构图）并按**非方形图等比内接**（那一档是兜底、不是原版）。
                            //   ⇒ 判据分两档：方形图仍必须拉满 1108²（挡「方形图误用 keepAspect」）；
                            //   非方形兜底则必须**等比**（高度 1108、宽 = 1108×texW/texH，挡「把竖图拉方」）。
                            var tex = wlQ.Texture;
                            bool square = tex != null && Mathf.Abs(tex.width - tex.height) <= 1;
                            float wpx = wlQ.WorldW * 108f, hpx = wlQ.WorldH * 108f;
                            if (square)
                            {
                                CheckNear(wpx, 1108f, 2f,
                                          "方形立绘**渲染宽度** = **1108**（原版 `m_PreserveAspect = 0` 拉满；"
                                        + "退回 `keepAspect: true` 时这里 ≈ 726 ⇒ 这条红）");
                                CheckNear(hpx, 1108f, 2f, "方形立绘**渲染高度** = **1108**（同上）");
                            }
                            else
                            {
                                float wantW = tex != null && tex.height > 0 ? 1108f * tex.width / tex.height : 726f;
                                CheckNear(hpx, 1108f, 2f, "非方形兜底立绘**渲染高度** = **1108**（等比内接）");
                                CheckNear(wpx, wantW, 3f,
                                          "非方形兜底立绘**渲染宽度** = **等比**（1108×texW/texH；"
                                        + "⛔ 不是 1108 拉满 —— 那会把竖图拉方）");
                            }
                        }
                        if (wh != null)
                        {
                            wh.GetComponent<WindowButton>().ClickForTest();
                            // ⚠️ 用 `DeckInfoPopup.LastWarlordDetail`，**不是** `CardDetailPopup.LastOpened`
                            //    （后者只在 `RebuildKeepingState()` 里赋值 ⇒ 这条路拿到的恒是 null）
                            var wd = DeckInfoPopup.LastWarlordDetail;
                            CheckTrue(wd != null && wlc != null && wd.Card != null && wd.Card.Name == wlc.Name,
                                      "点它 ⇒ 开督军的**卡片详情窗**（「" + (wlc != null ? wlc.Name : "?") + "」）");
                            if (wd != null) wd.Close();
                        }
                        // ⑦ `Share` / `Share On Chat` —— 🔴 **两件不同的事**（⛔ 不是「两个入口一个动作」）。
                        //   · `Share` = **写系统剪贴板 + 弹一条提示**。原版 `DeckInfoPopup__ShareDeck.c` 三句：
                        //     ① `MakeDeckString()` 造串 ② `UnityEngine.GUIUtility.set_systemCopyBuffer(串)`
                        //     **写系统剪贴板** ③ `UIMessageController.ShowMessage(…)` 弹一条消息。
                        //     写剪贴板那份实现收在**全库唯一一处** = `Deck/DeckRuntime.CopyDeckToClipboard`
                        //     （`CLAUDE.md` §三：两处写同一条规则 = 迟早不一致）。
                        //   · `Share On Chat` = 原版**开一扇两键面板（群组 / 全局）→ 发一条聊天消息**、
                        //     **不写剪贴板**（`DeckInfoPopup__ShareDeckOnChat.c`）。
                        //     🔴 **2026-10-12（账 `A1045`）就地改口（铁律 5）**：本行原来写「那扇面板**还没建**
                        //     ⇒ 本路如实出声（`NotBuilt` 的 `LogWarning` + 一句人话弹窗）」—— **已过期**：
                        //     面板**建完了**（`Shell/GenericOptionsPanel.cs`）且 `Shell/DeckInfoPopup.cs` 的
                        //     `ShareOnChat()` 现在**真把它开出来**；`NotBuilt` 与那句弹窗**都没有了**，
                        //     剩下的唯一缺口 = 原版 `ChatGlobalManager.ShareDeck(串, 频道)`（本仓没有聊天服务器）
                        //     ⇒ 点两颗钮出声「**面板在、发送口不在**」。判据与六条断言 → 下面 `A1051` 那一节。
                        //   🔴 **2026-10-18（`A1051`）就地订正的结论仍在**（细节不再抄第二份）：本行**原文**写
                        //     「给卡组串（**原版走平台/服务端**）」是**过期结论** —— 原版既不走上服务端、也没做
                        //     平台分享，调的就是**本地** API `GUIUtility.systemCopyBuffer`
                        //     （判据 = `decomp_full/DeckInfoPopup__ShareDeck.c` 第 ② 句；正本 =
                        //      `Shell/DeckInfoPopup.cs` 的 `ShareDeck` 头注 + `Deck/DeckRuntime.cs`）。
                        //   ⛔ **别把这里读成「给卡组串方便玩家自己复制」**：那句兜底**已于 2026-10-12 取消**
                        //     （原版没有它）；`Share` 走剪贴板、成功与否有断言钉着，见下面 A1051 那一节。
                        var shHit = pop.Opt("Share");
                        CheckTrue(shHit != null && shHit.GetComponent<WindowButton>() != null, "`Share` 有点击区");
                        Debug.Log(P + "   卡组串自检：Share 调 `Deck/DeckRuntime.CopyDeckToClipboard`"
                                    + "（写剪贴板那份实现**全库只此一处**）");
                    }
                    Shoot("05_收藏_DeckInfo弹窗.png");
                    var popCloseHit = FindChild(pr, "CloseHit");
                    var popCloseWb = popCloseHit != null ? popCloseHit.GetComponent<WindowButton>() : null;
                    CheckTrue(popCloseWb != null, "关闭圆钮有点击区");
                    // 🆕 **2026-10-07（波 4 件① A126）：这颗带按钮脸的关窗钮，原来一条断言都没有。**
                    //   三条各补一样（① 矩形四边 · ② 档位 · ③ 脸图绑定），逐条说判据：
                    //
                    //  ① **命中区四条边** —— 🔴 **2026-10-11（A180）本条已按原版的【射线那一面】改写**。
                    //    原版那颗按钮**自己的 `Image` 不吃射线**（`m_RaycastTarget = 0`，pad 全 0），
                    //    吃射线的是**两个子件** `Background` / `Icon`（`m_RaycastTarget = 1`）——
                    //    而这两颗都带 **`m_RaycastPadding = (−20,−20,−20,−20)`**（**负 = 外扩**）。
                    //    ⇒ 期望 = **子件 rect 外扩 20**，**不是**按钮那颗 `RectTransform` 的矩形
                    //      （旧断言写的就是按钮矩形 `1782.81/63.20/1857.19/138.80` ⇒ 比原版**每边小 ≈20px** = A180 本身）。
                    //    判据（解包实读）：
                    //      · 按钮自身 Image `MonoBehaviour_3635446896823339432.json` = `m_RaycastTarget 0`；
                    //      · 两个子件 `MonoBehaviour_-583606675015890520.json`（`Background`）/
                    //        `…_7496897533368830376.json`（`Icon`）= `m_RaycastTarget 1` · pad `(−20,…)`；
                    //      · 子件 rect（`menu_dump … "Deck info Popup" --depth 4`）= **`1791.0, 71.2 → 1847.8, 129.3`**
                    //        ⇒ 外扩 20 = **`1771.0, 51.2 → 1867.8, 149.3`（96.8 × 98.1）**。
                    //    复算 —— ✅ **2026-10-12（A329）订正**：原文写「我们那两层画在
                    //      `CloseL + (CloseR−CloseL−OptIconW)/2 = 1791.573` ⇒ `1771.573, 51.18 → 1868.433, 149.30`
                    //      ⇒ 与原版差 **0.573px**（那条亚像素差早已记账）」。那是 **A329 之前**的状态：
                    //      A329 把那颗的 `fx1` 改成 `CloseL + OptInL = 1790.961`（与 `Deck Options` 五颗同一套
                    //      内缩，判据 → `Shell/DeckInfoPopup.cs` 第 8) 节与 `OptInL` 那段）⇒
                    //      现在 = **`1770.961, 51.18 → 1867.821, 149.30`**，四边与原版差 **≤0.039px**
                    //      （中心 0.009 / 0.010）⇒ 下面**五条容差同批从 1.5 收到 0.3**（0.039 的残差 vs 0.612 的缺陷）。
                    //    ⛔ 期望值写**原版复算出来的字面量**，⛔ **不写** `DeckInfoPopup.ClosePad` / `CloseL/T/R/B`
                    //     —— 那是被测实现**传进去的实参**，拿它当期望 = 同式自证（改实现它跟着绿）。
                    //    ⚠️ 节点与 quad 都量：`CheckAtPx` 管**节点**（2026-09-23 那个「节点全停在容器 (0,0)、
                    //        只有里面的 quad 画对了」的坑就靠它抓），四条边管**画出来那张**。
                    //    🔴 **2026-10-11（F6）这一条改走【像素空间 2D】**（原来是 `CheckAt` —— 3D、**含 z**）。
                    //       A180 给这颗节点显式前移了 z（`Shell/DeckInfoPopup.cs` 的 `HitZFront = 0.01`
                    //       = **1.08px**，落在第 8) 节末的 `localPosition -= (0,0,HitZFront)`；那把尺子**是对的**
                    //       —— 就是下面 ①-b 那条「比邻居更深」的相对断言），而 `CheckAt` 的容差**恰好就是**
                    //       0.01 世界单位 = **1.08px** ⇒ **光 z 这一项一个人就吃满**；再加 x 的 0.603px
                    //       （= 上面那句 0.573px 亚像素差的同一条）⇒ `sqrt(0.603² + 1.08²)` = **1.24px**
                    //       ⇒ **恒红**（`_tmp_view/collection.log:3970`）。**那是口径错，不是缺陷。**
                    //       ⛔ 不把 `CheckAt` 改软（全文件 24 处共用，本轮不动）—— **这一处**改成与下面
                    //       四条边**同一个口径**：`LayoutSpace.ToPixel` 比中心 · **z 不进比较**。
                    //       判别力不变：它唯一的增量是抓「节点停在容器 (0,0)」那种**差几百 px** 的错。
                    //       改坏法：把 `closeHitNode` 的 x/y 写成 0（节点落到世界原点 = 画布中心 (960,540)）
                    //       ⇒ 差 **(−859.40, +439.75) px** ⇒ 红。
                    //       ✅ **2026-10-12（A329）订正容忍度**：F6 当时给 1.5px，理由是「**0.603px 的亚像素差**
                    //         与 1.08px 的（有意）z 前移都不该被算成位置错」—— A329 把那个 0.603px 收掉之后
                    //         （残差 0.009px），**这条理由不存在了** ⇒ 与同族 A308 对齐收成 **0.3px**
                    //         （z 本来就不进这个比较，不受影响）。
                    const float ChL = 1771.0f, ChT = 51.2f, ChR = 1867.8f, ChB = 149.3f;
                    const float ChCx = (ChL + ChR) * 0.5f, ChCy = (ChT + ChB) * 0.5f;   // = 1819.40 / 100.25
                    //  🔴 **2026-10-12（A329）：容差 1.5 → 0.3**（上面那句「容差 1.5px 的取法」是 F6 当时的口径，
                    //    当时的理由是「0.603px 的亚像素差…」—— A329 把那一处收掉之后这条理由**不存在了**）：
                    //    现在实测残差 = **中心 (0.009, 0.010) · 四边 (0.039, 0.020, 0.021, 0.000)**，
                    //    而**缺陷那一档**是 0.612px（x 侧两条边 0.573 / 0.633）⇒ 0.3 正夹在中间，
                    //    与同族 A308 那两条（`|Δ| ≤ 0.3`，`本文件`/`本文件`）**同一个口径**。
                    //    改坏法（A329 那一行退回「居中」`(r.W − OptIconW) * 0.5f`）⇒ **左/右两条边红**
                    //      （0.573 / 0.633 > 0.3），上/下两条仍绿（y 侧本来就没这个毛病，见 `DeckInfoPopup` 第 8) 节）。
                    CheckAtPx(popCloseHit, ChCx, ChCy, 0.3f,
                              "关闭钮命中区**节点**在原版**射线区**中心（子件 rect 外扩 20 后复算；节点没摆对就红）");
                    var chQ = popCloseHit != null ? popCloseHit.GetComponentInChildren<ImageQuad>() : null;
                    if (chQ == null)
                        CheckTrue(false, "关闭钮命中区下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 这颗等于点不动）");
                    else
                    {
                        float chW = chQ.WorldW * 108f, chH2 = chQ.WorldH * 108f;
                        float chCx = MenuCheck.DesignPx(chQ.transform).x;
                        float chCy = MenuCheck.DesignPx(chQ.transform).y;
                        CheckNear(chCx - chW * 0.5f, ChL, 0.3f, "关闭钮命中区**左沿** = **1771.0**（子件 1791.0 外扩 20）");
                        CheckNear(chCy - chH2 * 0.5f, ChT, 0.3f, "…**上沿** = **51.2**");
                        CheckNear(chCx + chW * 0.5f, ChR, 0.3f, "…**右沿** = **1867.8**");
                        CheckNear(chCy + chH2 * 0.5f, ChB, 0.3f, "…**下沿** = **149.3**");
                        // 🔴 **2026-10-11（A180）两条【相对】断言** —— 上面四条只钉「摆在哪」，
                        //   这两条钉「**大小**」：外扩是加在**子件矩形**（56.86×58.12）上的，
                        //   ⛔ 不是加在按钮矩形（74.386×75.605）上。三种错法各红一条：
                        //     · 去掉外扩（pad 写 `Vector4.zero`）⇒ 74.386 / 75.605 ⇒ 两条都红（= A180 原状）；
                        //     · 把 pad 加在**按钮矩形**上 ⇒ 114.386 / 115.605 ⇒ 两条都红（**每边多 ≈9px**，
                        //       那 9px = `(74.386 − 56.86)/2` 那道内缩，也是「看着像对了」的那一档）。
                        CheckNear(chW, 96.86f, 1.5f,
                                  "★ A180：命中区**宽 = 原版 96.86**（= 子件宽 56.86 + 2×20 外扩）");
                        CheckNear(chH2, 98.13f, 1.5f,
                                  "★ A180：命中区**高 = 原版 98.13**（= 子件高 58.12 + 2×20 外扩）");
                        //  ② **档位 = `QDIHit` 3129**（⛔ 写字面量、不写常量名）：常量与实现同源，
                        //     只改常量那种写法会跟着一起动、照样绿（= 丙-3 报告 §四那笔「自证残余」的修法）。
                        //     语义：它必须**压在压暗层 `QDI`(3120) 之上**，否则「点窗外关窗」那颗会先吃到命中。
                        //     🔴 **2026-10-18 就地订正（铁律 5）**：这里原来钉 `3123`（= 四档梯子时的 `QDIHit`）；
                        //        同批 `DeckInfoPopup` 已把队列重排成**十档**（压暗 3120 / 红底 3121 / 立绘 3122 /
                        //        面板 3123 / 行 3124 / 行图 3125 / 行边 3126 / 费用圈 3127 / 文字 3128 / 命中 3129，
                        //        见 `Shell/DeckInfoPopup.cs`）⇒ 命中区现在是 **3129**；
                        //        「命中区 > 压暗层 `QDI`(3120)」这一条**语义不变**。
                        Check(chQ.RenderQueue, 3129,
                              "关闭钮命中区的档 = `QDIHit` **3129**（字面量钉死；同档的还有 8 颗钮的命中区与 "
                              + "`WarlordHit` —— 它们都要在压暗层的 3120 之上）");
                    }
                    //  ①-b 🔴 **2026-10-11（A180）外扩带来的那条重叠带 —— 谁赢必须与原版同向**。
                    //    外扩之后关窗钮与邻居 `Switch Deck Info Button` 的命中区重叠 ≈12×19px
                    //    （原版 ≈24×30px —— 它邻居那颗**也是**子件外扩，实测同一套模型）。
                    //    原版靠**深度**定胜负：解包实读窗根 `Deck info Popup` 的 `m_Children` 顺序 =
                    //    `Menu Dark Background(0) → Generic Window Red Background Big(1) → Warlord Image(2) →
                    //     Deck Details(3) → Buttons(4) → Deck Options(5) → Info Panel(6) → Generic Close Button Orange(7)`
                    //    ⇒ **关窗钮是最后一个子件、最深** ⇒ 那条带里点下去**应当关窗**。
                    //    我们这一侧的「谁压谁」= **队列 → z**（`Shell/PointerLayer.cs` 的 `HitButton`：
                    //    同队列再比 z、**越小越靠前**），而本窗 9 颗命中区**全在 3129、z 又全相等** ⇒ 打平，
                    //    胜负落在 `FindObjectsByType` 的返回序上（**不可靠**）。
                    //    ⇒ 本窗 `Build()` 第 8) 节把那颗节点的 z 显式往前挪一点点（相对量）。
                    //    ⛔ 期望值不写那个常量：这里比的是**两颗 quad 的 z 相对大小**（题面即断言）。
                    var optHitT = FindChild(pr, "Opt_Switch Deck Info");
                    var optQ = optHitT != null ? optHitT.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(optQ != null,
                              "（前提）邻居 `Switch Deck Info` 那颗命中区在（下面那条深度对照靠它）");
                    if (chQ != null && optQ != null)
                    {
                        float zc = chQ.transform.position.z, zo = optQ.transform.position.z;
                        CheckTrue(zc < zo,
                                  $"★ A180：关窗钮的命中区**比邻居更深**（z {zc:F4} **<** {zo:F4}）"
                                  + " —— 原版那颗是窗根**最后一个**子件（`m_Children` 实测序 `… Deck Options(5) → "
                                  + "Info Panel(6) → Close(7)`），那条重叠带里点下去**应当关窗**；"
                                  + "删掉 `Shell/DeckInfoPopup.cs` 第 8) 节那两句 z 前移 ⇒ 两个 z 相等 ⇒ 红"
                                  + "（并退回「看 `FindObjectsByType` 返回序」的运气判 —— 同队列同 z 时先到者赢）");
                    }
                    //  ③ **脸图绑定** —— 判据 = 原版那颗 `Selectable`：
                    //     `m_Transition = 2`(SpriteSwap) · `m_TargetGraphic` → pid `-583606675015890520`
                    //     = 子件 **`Background`**（`m_Sprite` 解出来 **`40k_general_bt_yellow`**，`m_RaycastTarget = 1`）·
                    //     `m_SpriteState.m_HighlightedSprite` = **`40k_general_bt_yellow_hover`** ·
                    //     `m_PressedSprite` = `40k_general_bt_yellow_pressed`
                    //     （出处 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3635446896823339432.json`；
                    //      sprite 名由 `menu_dump.py` 的包内 sprite 索引解出，同一行还印出 `Icon` = `40k_general_bt_yellow_close`）。
                    //     ⇒ 换图那一层**必须是「按钮脸」那张**（`Close Face` = `40k_general_bt_yellow`），
                    //       ⛔ 不是圆底 `UI_Button_Round_background`、⛔ 不是 `Close Icon`、⛔ 不是那颗全透明命中区。
                    //     ⚠️ 与 ① 的**命中区无关**（那是 `Hit` 建的另一颗）—— 两条分开写，别混。
                    CheckTrue(popCloseWb != null && popCloseWb.target != null && popCloseWb.target.Texture != null
                              && popCloseWb.target.Texture.name == "40k_general_bt_yellow"
                              && popCloseWb.target.gameObject.name == "Close Face",
                              "关闭钮换图那一层 = 带按钮脸的 `Close Face`（图 **`40k_general_bt_yellow`**；原版 "
                              + "`m_TargetGraphic` 指的就是子件 `Background`）——实得「"
                              + (popCloseWb == null || popCloseWb.target == null || popCloseWb.target.Texture == null
                                 ? "<没绑>" : popCloseWb.target.gameObject.name + " / " + popCloseWb.target.Texture.name)
                              + "」");
                    // `art` 实参那一半：`WindowButton.Bind` 只存纹理、**不存那个字符串** ⇒ 从它算出来的
                    // 高亮图名字反推（`40k_general_bt_yellow` 的表外后备 = `…_hover`）。
                    // ⚠️ 悬停**真的换得动 / 离开真的还原**由本节开头那条 `CheckHoverSwap(pop.transform, …)` 管
                    //    （`AuditHoverSwap` 逐颗走过，含这一颗）—— 这里只核「绑的是哪张图」，别重复。
                    CheckText(popCloseWb != null && popCloseWb.HoverTexForTest != null
                              ? popCloseWb.HoverTexForTest.name : "<null>",
                              "40k_general_bt_yellow_hover",
                              "换图的 `art` 实参 = **`40k_general_bt_yellow`**（原版 `m_SpriteState.m_HighlightedSprite`）"
                              + " —— 把这个实参拿掉时这里取到 `<null>` ⇒ 红");
                    CheckTrue(popCloseWb != null && popCloseWb.onClick != null,
                              "关闭钮的 `onClick` 挂着（「有点击区」≠「点了有反应」；下面那条真点一次再断窗的状态）");
                    if (popCloseWb != null) popCloseWb.Click();
                    Check(pop.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗进 `Closed` 态");
                }
            }

            // ============================================================ 🆕 2026-10-18（账 `A1051`）
            //  `Share` / `Share On Chat` —— **四条断言**（分享收口之后这一块**一条自动化尺子都没有**）。
            //
            //  原版判据（逐句实读 `d:/2/tools/decomp_full/DeckInfoPopup__ShareDeck.c`）：
            //   ① `MakeDeckString()` 造串 ② `UnityEngine.GUIUtility.set_systemCopyBuffer(串)`
            //   **写系统剪贴板** ③ `UIMessageController.ShowMessage(…)` 弹一条消息（原版是 toast，
            //   我们退成模态弹窗 ⇒ 账 `A1050`）。⇒「点一下 ⇒ 系统剪贴板里出现这一副的卡组串」是**原版行为**。
            //
            //  ⚠️ **本段自开一扇窗、末尾关掉并清场**（照本文件 `PracticeModePopup.LastOpened = null` 那一段的
            //    做法：自开的窗自己收，不留状态给后段）。**不借上面那一节的 `pop`** —— 这两下各会开一扇模态
            //    提示窗（`WindowsManager.ShowPopUp`），借它会把后段的截图与断言挡掉。
            Section("A1051：`Share` 真写剪贴板 / `Share On Chat` 不写（四条）");
            {
                var sPop = win.OpenDeckInfo(0);
                CheckTrue(sPop != null, "（前提）自开一扇 `Deck info Popup`");
                if (sPop != null)
                {
                    var rawShare = CollectionData.Raw(0);
                    CheckTrue(rawShare != null, "（前提）被点 `Share` 的那一副（下标 0）取得到");

                    var sharHitA = sPop.Opt("Share");
                    var sharWbA = sharHitA != null ? sharHitA.GetComponent<WindowButton>() : null;
                    CheckTrue(sharWbA != null, "（前提）`Share` 那颗钮取得到（(1)(2) 都靠它）");

                    // 期望值 = **这一副**照原版格式导出来的那一串。
                    string wantShare = rawShare != null ? DeckLibrary.ExportString(rawShare) : "";
                    CheckTrue(!string.IsNullOrEmpty(wantShare),
                              "（前提）这一副**导得出**卡组串（空串 ⇒ (1)(2) 等于空转）");

                    // ⚠️ **哨兵必须先置**：不置的话「实现根本没写」与「剪贴板里正好还留着上一次的内容」
                    //    分不开（本工程那条系统性毛病「弱断言分不出两种状态」）。哨兵**不是**合法卡组串。
                    const string ShareSentinel = "A1051-SENTINEL-NOT-A-DECK";

                    // 🔴 **批处理下 `GUIUtility.systemCopyBuffer` 能不能用【没查实】**（`Deck/DeckRuntime.cs`
                    //    的 `CopyDeckToClipboard` 末段就如实记着这一条 —— 红线不许跑 Unity 去核）⇒ 下面几条
                    //    一律走这三个小口，**每个都包一层**：异常**当场红**，⛔ 不让一个未捕获的异常把整个
                    //    `Run()` 掐掉（那会让本节之后**几百条断言一条都不跑**，症状还看着像「本批全崩」）。
                    //    ⚠️ 这三个口**只给自检用**，⛔ 不许替产品代码挡异常（真人点 `Share` 走生产那一句）。
                    bool ClipSet(string s)
                    {
                        try { GUIUtility.systemCopyBuffer = s; return true; }
                        catch (System.Exception e)
                        { Debug.LogError(P + "`systemCopyBuffer` 写入抛异常：" + e.Message); return false; }
                    }
                    string ClipGet()
                    {
                        try { return GUIUtility.systemCopyBuffer; }
                        catch (System.Exception e)
                        { Debug.LogError(P + "`systemCopyBuffer` 读取抛异常：" + e.Message); return "<读剪贴板抛异常>"; }
                    }
                    void ClipClick(WindowButton wb)
                    {
                        try { wb.ClickForTest(); }
                        catch (System.Exception e) { Debug.LogError(P + "点那颗钮抛异常：" + e.Message); }
                    }

                    // ⚠️ **前置**：这个 API 在批处理下**写得进、读得回**吗？不能 ⇒ 当场红
                    //    （那时下面几条对剪贴板的断言既可能假绿、也可能假红 —— 验不了就**说出来**，
                    //     红线「不许静默失败」；⛔ 也不许把这一条放宽成「跳过」）。
                    bool clipOk = ClipSet(ShareSentinel) && ClipGet() == ShareSentinel;
                    CheckTrue(clipOk, "（前提）批处理下 `GUIUtility.systemCopyBuffer` **写得进、读得回**"
                                    + "（置哨兵 → 读回 = 哨兵）—— ⛔ 不能的话下面几条剪贴板断言**等于没验**，所以这里先红");

                    // ---------------------------------------------------------------- (1) 真写剪贴板
                    if (sharWbA != null && !string.IsNullOrEmpty(wantShare) && clipOk)
                    {
                        ClipSet(ShareSentinel);
                        ClipClick(sharWbA);
                        string gotShare = ClipGet();
                        CheckTrue(gotShare != ShareSentinel,
                                  "★ A1051(1)：点 `Share` ⇒ 系统剪贴板**被写过**（哨兵已被顶掉）"
                                + " —— 🧨 **改坏法**：把 `Deck/DeckRuntime.CopyDeckToClipboard` 改成"
                                + "「算完 `return s` 但**不赋值**」⇒ 这一条红（哨兵原样留着）");
                        CheckText(gotShare, wantShare,
                                  "★ A1051(1)：……而且写进去的就是**这一副的卡组串**"
                                + "（原版 `DeckInfoPopup__ShareDeck.c:16` 就这一句 `set_systemCopyBuffer`）");
                    }

                    // ---------------------------------------------------------------- (2) 灭自证
                    // ⛔ **只断 (1) 不够**：(1) 的期望值 `wantShare` 是**被测实现自己调的**那个
                    //    `DeckLibrary.ExportString` ⇒ 谁把 `ExportString` 改坏（例：漏写督军 / 丢掉模式），
                    //    **期望值与实测值一起动、照样全绿**（= 本项目那条「断言自证 / 同义反复」）。
                    // ⇒ 这里把期望值**按原版格式从卡组数据现场重算**（`CardDeck__Serialize.c` 的形状：
                    //    `名字(: 转义) [':' 防御卡] [':' 督军] (':' 每张卡)* ';' 模式`，再 Base64），
                    //    **一个字节都不来自 `ExportString`** ⇒ 结构上不可能「实现与检测器一起改回去还绿」
                    //    （两边没有共用任何函数）。这一点就是本条与 (1) 合起来才算「灭自证」的原因。
                    if (sharWbA != null && !string.IsNullOrEmpty(wantShare) && clipOk)
                    {
                        string plainShare = null;
                        try
                        {
                            plainShare = System.Text.Encoding.UTF8.GetString(
                                             System.Convert.FromBase64String(ClipGet()));
                        }
                        catch { plainShare = null; }        // 不是合法 Base64 ⇒ 下面那条如实红
                        var sbShare = new System.Text.StringBuilder();
                        sbShare.Append((rawShare.Name ?? "").Replace(":", DeckLibrary.ColonEscape));
                        if (!string.IsNullOrEmpty(rawShare.DefensiveId))
                            sbShare.Append(':').Append(rawShare.DefensiveId);
                        if (!string.IsNullOrEmpty(rawShare.WarlordId))
                            sbShare.Append(':').Append(rawShare.WarlordId);
                        foreach (var cidA in rawShare.CardIds ?? new List<string>())
                            if (!string.IsNullOrEmpty(cidA)) sbShare.Append(':').Append(cidA);
                        sbShare.Append(';').Append(rawShare.GameMode);
                        CheckTrue(plainShare != null && plainShare == sbShare.ToString(),
                                  "★ A1051(2) 灭自证：剪贴板那串按**原版格式**解出来 = 名字 / 防御卡 / 督军 / 全部卡 / 模式"
                                + "（期望值由**卡组数据 + 原版格式**现场重算，⛔ 不调 `ExportString`）"
                                + " —— 🧨 改坏法：把 `ExportString` 里那句 `WarlordId` 摘掉 ⇒ (1) 两边一起动仍绿、"
                                + "**这一条必红**"
                                + (plainShare == null ? "（实得：**不是合法 Base64**）" : ""));
                    }

                    // ---------------------------------------------------------------- (3) `Share On Chat` 真开面板
                    //  🔴 **2026-10-12（账 `A1045`）就地改写（铁律 5）**：本条原来断的是「那扇面板**还没建** ⇒
                    //    坐实一条 `[DeckInfo] … 还没实现` 的告警」。面板**早已建完**（`Shell/GenericOptionsPanel.cs`
                    //    的 8 节点对账表 + `Editor/ShopScene.cs` 那一节），本轮**真把它开出来了** ⇒ 那一句告警
                    //    **已经不存在**（判据 = 原版 `DeckInfoPopup__ShareDeckOnChat.c` 末跳
                    //    `WindowsManager.OpenWindow(panel, ctx)`）⇒ 旧期望**必须改**，⛔ **不是放宽**：
                    //    新期望比旧的多两条（面板真开出来 + 下面 (5) 那一节逐格核内容）。
                    //  ⚠️ 旧断言那半「抓到 `还没实现` 的告警」的**判别力没有被丢掉**：改由 (5)④ 那三条
                    //    （两颗钮各自的目标 + 灭自证）接管 —— 哪一半不对都红。
                    var socHitA = sPop.Opt("Share On Chat");
                    var socWbA = socHitA != null ? socHitA.GetComponent<WindowButton>() : null;
                    CheckTrue(socWbA != null, "（前提）`Share On Chat` 那颗钮取得到");
                    CheckTrue(GenericOptionsPanel.LastOpened == null,
                              "（前提）本节之前**没有任何一扇 `Generic Options Panel` 被开过**"
                            + " —— 否则下面 `LastOpened` 分不清「这一下开出来的」与「上一段剩下的」");
                    if (socWbA != null)
                    {
                        if (clipOk) ClipSet(ShareSentinel);
                        ClipClick(socWbA);
                        if (clipOk)
                            CheckText(ClipGet(), ShareSentinel,
                                      "★ A1051(3)：点 `Share On Chat` ⇒ 剪贴板**一个字节都没动**"
                                    + "（原版那条发的是聊天消息、⛔ 不写剪贴板）"
                                    + " —— 🧨 改坏法：把 `Shell/DeckInfoPopup.ShareDeck` 里那句"
                                    + " `if (key == \"Share On Chat\") { ShareOnChat(); return; }` 删掉 ⇒ 这一条红");
                        CheckTrue(GenericOptionsPanel.LastOpened != null,
                                  "★ A1045①：点 `Share On Chat` ⇒ **真开出了那扇 `Generic Options Panel`**"
                                + "（原版 `ShareDeckOnChat.c` 末跳 `WindowsManager.OpenWindow(panel, ctx)`）"
                                + " —— 🧨 改坏法：把 `ShareOnChat()` 退回 `NotBuilt(…)` ⇒ 这一条红");

                        // ==================== 🆕 2026-10-12（账 `A1045`）：那扇面板逐格核 ====================
                        //  判据（第一权威 = 全量反编译，逐句实读）：
                        //   · `DeckInfoPopup__ShareDeckOnChat.c`：三道门 → 两颗 `Button_(键, onClick,
                        //     色覆盖 = null, interactable, **AutoClose = 1**)` → `Context(buttons, Title,
                        //     AnchorPosition, **LocalizeText = 1**)` → `OpenWindow`；**兄弟序 = 先建群组、后建全局**
                        //   · `DeckInfoPopup___ShareDeckOnChat_b__30_1.c`（第 0 颗 / 群组）
                        //     ⇒ `HandleShare(this, AlliancesManager.CurrentGroupId, 1)`
                        //   · `DeckInfoPopup___ShareDeckOnChat_b__30_2.c`（第 1 颗 / 全局）
                        //     ⇒ `HandleShare(this, <常量频道串>, 0)`
                        //   · `DeckInfoPopup___ShareDeckOnChat_g__HandleShare_30_0.c`（⚠️ **三下划线**）= 四跳
                        //  ⛔ **期望值一律【原版字面量】或【原版公式现算】**，⛔ 不读实现里的常量/口径（自证）。
                        Section("A1045：`Share On Chat` 那扇两键面板（面板在、发送口不在）");
                        {
                            var gopA = GenericOptionsPanel.LastOpened;
                            var gb = gopA != null ? gopA.Ctx.Buttons : null;
                            var zbt = gopA != null ? gopA.Ctx : default(GenericOptionsPanel.Context);
                            bool gbOk = gb != null && gb.Length == 2;
                            CheckTrue(gbOk, "（前提）面板开出来了、而且里面正好 **2 颗**钮"
                                          + "（破了的话 A1045①③④⑤⑥ 整批跳过 —— ⛔ 不拿 0 颗去索引，"
                                          + "那会抛异常把本节之后几百条断言一起掐掉）");
                            if (gbOk)
                            {
                                // 期望值的**三根键** = 原版**字符串常量表**实读（不是自拟）：
                                // `d:/2/tools/il2cpp_out/stringliteral.json` 的
                                // `MenuDeck/Share/{ShareOnAlliance,ShareOnGlobal,Title}`（地址
                                // `0x42D08E0 / 0x42D09E0 / 0x42D0AE0`，与上面那条 `ExportSuccesful`(`0x42D07E0`) 连号）。
                                const string zTitle = "MenuDeck/Share/Title";
                                const string zAlli = "MenuDeck/Share/ShareOnAlliance";
                                const string zGlob = "MenuDeck/Share/ShareOnGlobal";
                                CheckTrue(Loc.HasEntry(zTitle) && Loc.HasEntry(zAlli) && Loc.HasEntry(zGlob),
                                          "（前提）三条词条都在表里 —— ⛔ 缺了 ⇒ 下面几条两边一起退化成键名 = 假绿");

                                // ---- ① 内容 / 兄弟序 / 标题 / 锚点档 ----
                                Check(gopA.BuiltButtons, 2, "★ A1045①：面板里 **2 颗**钮（原版两颗 `Button___ctor`）");
                                Check(zbt.Title, Loc.T(zTitle),
                                      "★ A1045①：标题 = `Loc.T(\"MenuDeck/Share/Title\")`"
                                    + "（原版 `Context___ctor` 第 3 个实参 `DAT_1842d0ae0`）"
                                    + " —— 🧨 改坏法：换一个键名 ⇒ 与这条字面量不等 ⇒ 红");
                                Check(gb[0].Text, Loc.T(zAlli),
                                      "★ A1045①：**第 0 颗 = 群组**（`ShareOnAlliance`）—— 原版**先建**它"
                                    + "（`ShareDeckOnChat.c` 的 `plVar6[4]`，键 `0x42D08E0`）"
                                    + " —— 🧨 改坏法：把数组里两颗**对调** ⇒ 这一条与下一条一起红");
                                Check(gb[1].Text, Loc.T(zGlob),
                                      "★ A1045①：**第 1 颗 = 全局**（`ShareOnGlobal`，键 `0x42D09E0`）");
                                CheckTrue(zbt.LocalizeText,
                                          "★ A1045①：`LocalizeText = true`（原版 `Context___ctor` 那个字面量 `1`）"
                                        + " ⇒ `Apply()` 里那条「远端 I2 取不到」**照旧出声**（⛔ 没被压掉）");
                                CheckAt(FindChild(gopA.transform, "Name"), 782.50f, 1137.50f, 646.70f, 683.30f,
                                        "★ A1045①：面板在**无锚点**那一支（`Name` 顶 = 636.70 + 10；636.70 = prefab 出厂"
                                      + " `anchoredPosition.y = −96.7` 那一档）—— 原版收的是 `Transform`，"
                                      + "**那个 `Transform` 谁给的本地查不到 ⇒ 这一格判据是空的**，"
                                      + "我们取 prefab 出厂值（如实标注，⛔ 不是查漏）");

                                // ---- ② 两颗的 `AutoClose` 与回调（「面板在、发送口不在」那两条出声靠它）----
                                CheckTrue(gb[0].AutoClose && gb[1].AutoClose,
                                          "★ A1045②：两颗 `AutoClose` **都是 `true`**"
                                        + "（原版两颗 `Button___ctor` 的第 6 个实参**都是字面量 1**）");
                                CheckTrue(gb[0].OnClick != null && gb[1].OnClick != null,
                                          "★ A1045②：两颗**都挂了回调**（原版那颗 `UnityEvent` 由调用方挂）"
                                        + " —— 🧨 改坏法：把两个 `OnClick = …` 删掉 ⇒ 这一条红"
                                        + "（且 ④ 那两条也一起红）");

                                // 两个小口（都只走**真点**，⛔ 不读实现的内部状态）：
                                //  · `ReopenPanel` = 拨门 + 重开面板（`sPop` 的树可能被重建过 ⇒ 每次现取那颗钮）
                                //  · `ClickPanelButton` = 点面板里第 `idx` 颗，返回那一下抓到的**全部日志**
                                void ReopenPanel(bool inG, bool banG, bool banA)
                                {
                                    sPop.SetChatState(inG, banG, banA);
                                    var h = sPop.Opt("Share On Chat");
                                    var w = h != null ? h.GetComponent<WindowButton>() : null;
                                    if (w == null)
                                    { Debug.LogError(P + "重开面板时取不到 `Share On Chat` 那颗钮"); return; }
                                    ClipClick(w);
                                }
                                List<string> ClickPanelButton(int idx)
                                {
                                    var got = new List<string>();
                                    var gNow = GenericOptionsPanel.LastOpened;
                                    var bn = gNow != null
                                             ? FindChild(FindChild(gNow.transform, "Buttons"), "Button " + idx) : null;
                                    var hb = bn != null ? FindChild(bn, "Hit") : null;
                                    var wb = hb != null ? hb.GetComponent<WindowButton>() : null;
                                    if (wb == null) { got.Add("<取不到面板第 " + idx + " 颗钮的命中区>"); return got; }
                                    Application.LogCallback hC = (c, st, ty) => { if (c != null) got.Add(c); };
                                    Application.logMessageReceived += hC;
                                    try { wb.ClickForTest(); }
                                    catch (System.Exception e)
                                    { Debug.LogError(P + "点面板第 " + idx + " 颗抛异常：" + e.Message); }
                                    Application.logMessageReceived -= hC;
                                    return got;
                                }
                                string FirstWith(List<string> ls, string a, string b)
                                {
                                    foreach (var lw in ls) if (lw.Contains(a) && lw.Contains(b)) return lw;
                                    return null;
                                }

                                // ---- ③ 三道门 → `Interactable`（**期望值按原版公式现算**）----
                                //  原版两个 `interactable` 表达式（`ShareDeckOnChat.c`）：
                                //    群组颗 = `AlliancesManager.IsInGroup && !IsBannedFromChat(ChatRoom.Alliance)`
                                //    全局颗 = `!IsBannedFromChat(ChatRoom.Global)`
                                //  ⚠️ **四种状态都拨一遍**（铁律 5·c「一个值 ≠ 全部情况」）：只断出厂那一种的话，
                                //    把 `&&` 写成 `||`、或漏掉某个 `!` 都可能蒙过去。
                                //  ⛔ 门的口 = `DeckInfoPopup.SetChatState`（本仓没有 `AlliancesManager`/服务器
                                //    ⇒ **显式状态口**，出厂 `false/false/false`，理由 → 那边的 doc）。
                                var gates = new[]
                                {
                                    new { In = false, BanG = false, BanA = false },
                                    new { In = true,  BanG = false, BanA = false },
                                    new { In = false, BanG = true,  BanA = false },
                                    new { In = true,  BanG = true,  BanA = true  },
                                };
                                for (int gi = 0; gi < gates.Length; gi++)
                                {
                                    var gs = gates[gi];
                                    string tr = "inGroup=" + gs.In + " / banned(Global)=" + gs.BanG
                                              + " / banned(Alliance)=" + gs.BanA;
                                    ReopenPanel(gs.In, gs.BanG, gs.BanA);      // `Show` 会按新的一颗 `Context` 重建
                                    var gNow = GenericOptionsPanel.LastOpened;
                                    var nb = gNow != null ? gNow.Ctx.Buttons : null;
                                    bool nbOk = nb != null && nb.Length == 2;
                                    CheckTrue(nbOk, "（前提）第 " + gi + " 档（" + tr + "）拨完读得到 2 颗钮");
                                    if (!nbOk) continue;
                                    bool wantAlli = gs.In && !gs.BanA;         // ⛔ 原版公式现算，不读实现
                                    bool wantGlob = !gs.BanG;
                                    CheckTrue(nb[0].Interactable == wantAlli,
                                              "★ A1045③：" + tr + " ⇒ **群组**颗 `Interactable = " + wantAlli + "`"
                                            + "（原版 = `IsInGroup && !IsBannedFromChat(Alliance)`）"
                                            + " —— 🧨 改坏法：`&&` 写成 `||`、或漏掉 `!` ⇒ 本档必红");
                                    CheckTrue(nb[1].Interactable == wantGlob,
                                              "★ A1045③：" + tr + " ⇒ **全局**颗 `Interactable = " + wantGlob + "`"
                                            + "（原版 = `!IsBannedFromChat(Global)`）");
                                }

                                // ---- ④ 灭自证：两颗钮**各自**的目标 ----
                                //  🔴 **结构上不可能同时满足**：把两个 `OnClick` **对调** ⇒ 第 0 颗报到 `Global`、
                                //    第 1 颗报到 `Alliance` ⇒ 下面两条互为反例、**必红**；两颗**都留空** ⇒
                                //    一条「发送口不在」都抓不到 ⇒ **也必红**（`OnButton` 那时走「没有挂回调」那一支）。
                                //  ⚠️ 量法 = **真点那两颗**（⛔ 不读 `OnClick.Target`）—— 读数只能证明
                                //    「两个委托不是同一个」，证明不了「哪个是哪一颗」。
                                ReopenPanel(true, false, false);            // 先只放开「群组」那道门
                                var wAlli = FirstWith(ClickPanelButton(0), "[DeckInfo]", "发送口不在");
                                ReopenPanel(false, false, false);           // 上一颗 `AutoClose` 把面板关了 ⇒ 再开一次
                                var wGlob = FirstWith(ClickPanelButton(1), "[DeckInfo]", "发送口不在");
                                CheckTrue(wGlob != null && wGlob.Contains("ChatRoom.Global")
                                          && !wGlob.Contains("ChatRoom.Alliance"),
                                          "★ A1045④ 灭自证：**第 1 颗**（全局）报的目标 = `ChatRoom.Global`"
                                        + "（原版 `…_b__30_2.c` 传的字面量 `0`）"
                                        + " —— 🧨 改坏法：把两颗的 `OnClick = () => ShareDeckToChat(…)` **对调**"
                                        + " ⇒ 这一条与下一条**一起红**");
                                CheckTrue(wAlli != null && wAlli.Contains("ChatRoom.Alliance")
                                          && !wAlli.Contains("ChatRoom.Global"),
                                          "★ A1045④ 灭自证：**第 0 颗**（群组）报的目标 = `ChatRoom.Alliance`"
                                        + "（原版 `…_b__30_1.c` 传的字面量 `1`）");
                                CheckTrue(wAlli != null && wGlob != null && wAlli != wGlob,
                                          "★ A1045④：那两条出声**不是同一条**（两颗绑成同一个回调了？）");

                                // ---- ⑤ 点完**面板自己关了**（原版两颗 `AutoClose = 1`）+ 剪贴板仍未动 ----
                                var gClosed = GenericOptionsPanel.LastOpened;
                                CheckTrue(gClosed != null && !gClosed.gameObject.activeSelf,
                                          "★ A1045⑤：点完一颗之后面板**自己关了**（原版两颗 `AutoClose = 1`"
                                        + " = `SetButtons` 给每颗多挂的那个 `AddListener(Close)`）"
                                        + " —— 🧨 改坏法：把两颗的 `AutoClose = true` 删掉 ⇒ 这一条红");
                                if (clipOk)
                                    CheckText(ClipGet(), ShareSentinel,
                                              "★ A1045⑤：那两下**也没写剪贴板**（原版这两条进的是聊天频道）");

                                // ---- ⑥ 反例：**门关着**的那一颗 —— 点了**什么都不发生**（面板不关、没发送那一跳）----
                                //  🔴 `Interactable = false` 的**三半**：
                                //    · **视觉①** = 底图 tint 换 `m_DisabledColor`（`RebuildButtons` 里那一句）；
                                //    · **视觉②** = 🆕 **A1726**：`wb.Interactable = false` ⇒ 九宫底 **画出来的那几块全灰**（⚠️ 这一档尺寸下只有 6 块，见 `:2909` 那条断言的注释）
                                //      （材质换原版 `Everguild/UI/Greyscale` —— 同 `EverguildButton.DoStateTransition(Disabled)`）；
                                //    · **行为** = **两道闸**：第一道 `WindowButton.Click()` 头一句
                                //      （= 原版 `Selectable.OnPointerClick` 头一句 `!IsInteractable() ⇒ return`，
                                //      **连派发都没有**，并出声 `[Button] … 点了不生效`）；兜底第二道 = `OnButton` 里
                                //      动作照进来、立刻返回 + 出声（口径同 `Shell/DeckInfoPopup.cs` 的 `Blocked`）。
                                //  ⚠️ **2026-10-13（A1726）就地订正（铁律 5）**：本条原来断的是
                                //    「抓到 `[OptionsPanel] … Interactable = false`」（= **第二道闸**那条出声）——
                                //    第一道闸接上之后，那一下在 `WindowButton.Click()` 里就结束了、**第二道闸走不到**
                                //    ⇒ 那条会**假红**。**改的是「哪一道闸出声」这个前提**，⛔ **不是把断言改松**：
                                //    声源换成**第一道闸**那条（`[Button] … 点了不生效`），并**另加两条**——
                                //    「那颗钮自己的 `WindowButton.Interactable` **真的是 `false`**」+
                                //    「九宫底**画出来的那几块**全灰（⚠️ 6 块，见 `本文件` 注释）（视觉态）」。三条合起来 = 灭自证的那两条并存
                                //    （只断「动作不生效」的话，把 `Interactable` 全设 true 也可能照样绿）。
                                ReopenPanel(false, false, false);           // 「群组」那道门关着
                                var lsOff = ClickPanelButton(0);
                                var wOffSend = FirstWith(lsOff, "[DeckInfo]", "发送口不在");
                                CheckTrue(wOffSend == null,
                                          "★ A1045⑥：门关着的那一颗点了 ⇒ **没有那一条『发送口不在』**"
                                        + "（原版那一刻不派发 ⇒ 那颗回调根本不跑）"
                                        + " —— 🧨 改坏法：把 `OnButton` 里 `if (!b.Interactable) … return;`"
                                        + " **与** `RebuildButtons` 里 `wb.Interactable = arr[i].Interactable;`"
                                        + " **两道一起**删掉 ⇒ 这一条红");
                                // 出声那条 = **第一道闸**（`WindowButton.Click()` 头一句），⛔ 不是第二道
                                var wOffLog = FirstWith(lsOff, "[Button]", "点了不生效");
                                CheckTrue(wOffLog != null,
                                          "★ A1045⑥：……而且**如实出声**（抓到 `[Button] … 点了不生效`"
                                        + " —— 第一道闸 = `WindowButton.Click()` 头一句，正是原版那一句话）"
                                        + " —— 红线：⛔ 不许静默失败"
                                        + (wOffLog == null ? "（实得 0 条；那一下的日志共 " + lsOff.Count + " 条）" : ""));
                                // 🔴 **A1726 的真判据（两半并存）**：① 那颗钮自己那一位布尔**真的**是 `false`
                                //    （⛔ 上面那条只证明「有出声」—— 第二道闸也会出声，分不出是哪一道拦的）；
                                //    ② **看得出来被禁用**（九宫底**画出来的那几块**全换灰化 shader（⚠️ 6 块，见 `本文件` 注释））。
                                var gOffNow = GenericOptionsPanel.LastOpened;
                                var bnOff = gOffNow != null
                                            ? FindChild(FindChild(gOffNow.transform, "Buttons"), "Button 0") : null;
                                var hbOff = bnOff != null ? FindChild(bnOff, "Hit") : null;
                                var wbOff = hbOff != null ? hbOff.GetComponent<WindowButton>() : null;
                                CheckTrue(wbOff != null && !wbOff.Interactable,
                                          "★ A1045⑥（A1726）：门关着那颗的 `WindowButton.Interactable` **真的是 `false`**"
                                        + "（= 第一道闸接上了）"
                                        + " —— 🧨 改坏法：把 `RebuildButtons` 里 `wb.Interactable = arr[i].Interactable;`"
                                        + " 删掉 ⇒ 这一条红（而上面那两条「没发送口」照旧绿 —— 那正是「只断后者不够」）");
                                int nNineOff = 0, nGreyOff = 0;
                                // 九宫底那一颗 = `bn` 的直接子件里**图形件最多**的那一个（它就是 `MenuDraw.Nine` 建的 `Image`）
                                // ⛔ **不按名字猜**「Image」—— 按名字找会在将来改了命名时**静默量错一颗**（量到 `Hit` 那颗透明件）。
                                if (bnOff != null)
                                {
                                    Transform nineOff = null; int bestOff = 0;
                                    foreach (Transform chOff in bnOff)
                                    {
                                        int cntOff = chOff.GetComponentsInChildren<ImageQuad>(true).Length;
                                        if (cntOff > bestOff) { bestOff = cntOff; nineOff = chOff; }
                                    }
                                    if (nineOff != null)
                                        foreach (var qOff in nineOff.GetComponentsInChildren<ImageQuad>(true))
                                        {
                                            if (qOff == null) continue;
                                            nNineOff++;
                                            var mrOff = qOff.GetComponent<MeshRenderer>();
                                            var mOff = mrOff != null ? mrOff.sharedMaterial : null;
                                            if (mOff != null && mOff.shader != null
                                                && mOff.shader.name == "Everguild/UI/Greyscale") nGreyOff++;
                                        }
                                }
                                // 🔴 **2026-10-14 就地改判据（铁律 5 · 收口自检 α）**：原来写的是 `nNineOff >= 9`
                                //   —— **那句前提不成立**：这一颗钮高只有 **60 px**、而九宫角块
                                //   `borderOutPx=(111,32,111,32)` ⇒ **32+32 = 64 > 60** ⇒ 原版 uGUI
                                //   （`Image.GetAdjustedBorders` 逐轴缩 + `GenerateSlicedSprite` 跳零尺寸格）
                                //   把该轴角块缩到 30、**中段高 = 0 ⇒ 中行 3 块根本不会被画出来**（横向 111+111 ≪ 357.3 ⇒ 中列还在）
                                //   ⇒ **只有 6 块**。这是**原版行为**、不是缺陷（旁证：日志里 `[ImageQuad] border 比图还大` 50 条）。
                                //   ⇒ 判据改成「**灰组 == 亮组 且 ≥ 6**」（亮组那边没写死 9、是个不变量）。
                                //   🧨 鉴别力没丢：只灰 1 块那种坏法仍然红（`nGreyOff(1) == nNineOff(6)` 为假）。
                                CheckTrue(nNineOff >= 6 && nGreyOff == nNineOff,
                                          "★ A1045⑥（A1726）：门关着那颗**看得出来被禁用** —— 九宫底**画出来的那 "
                                        + nNineOff + " 块**全部**换成原版灰化 shader `Everguild/UI/Greyscale`"
                                        + "（实得灰了 " + nGreyOff + " 块；判据 = 原版"
                                        + " `EverguildButton.DoStateTransition(Disabled)`；"
                                        + "⚠️ 这一档尺寸下原版只画得出 6 块，见上面那条注释）"
                                        + " —— 🧨 改坏法：把 `RebuildButtons` 里 `wb.Interactable = …` 删掉 ⇒ 这一条红"
                                        + "（= A1719 之前那种「只灰 1/6、看着像没禁用」）");
                                var gStill = GenericOptionsPanel.LastOpened;
                                CheckTrue(gStill != null && gStill.gameObject.activeSelf,
                                          "★ A1045⑥：……而且面板**没被关掉**（原版没派发 ⇒ 也就没有 `Close` 那一下）");

                                // ---- 收尾：面板关掉、三道门拨回出厂（⛔ 别留一扇开着的窗给后段）----
                                if (GenericOptionsPanel.LastOpened != null) GenericOptionsPanel.LastOpened.Close();
                                sPop.SetChatState(false, false, false);
                            }
                        }
                    }

                    // ---------------------------------------------------------------- (4) 结构式：只有一处实现
                    // 判据 = `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」。
                    //   (1) 证「点一下就真写了」、(4) 证「这一扇窗自己**没有**第二个写点」
                    //   ⇒ 两句合起来才能说「写的那一下必然走了 `DeckRuntime.CopyDeckToClipboard`」。
                    // ⛔ **不写死行号**（行号会漂，本项目明令）—— 读源码文本、按【行首标记】判：
                    //    先跳过 `//` 开头的行（该文件里 5 处 `systemCopyBuffer` **全在注释里** ——
                    //    讲的就是「原版那一句」+「A1040 的订正留档」），再找**孤立的 `=`**
                    //    （⛔ 不把 `==` / `!=` / `<=` / `>=` / `+=` 算成赋值）。
                    // 🧨 **改坏法**：在 `Shell/DeckInfoPopup.ShareDeck` 里就地抄一份
                    //    `GUIUtility.systemCopyBuffer = DeckLibrary.ExportString(deck)` ⇒ 这一条红。
                    // 🧨 **另一头的改坏法**（这条为什么必须配上 (1)）：把 (1) 那两下删掉、只留本条 ⇒
                    //    源码里照样没有赋值、本条照样绿 ⇒ **弱断言**。两条是一对，⛔ 别只留一条。
                    {
                        string srcDipA = System.IO.Path.Combine(Application.dataPath,
                                                                "CardPresentation/Shell/DeckInfoPopup.cs");
                        if (!System.IO.File.Exists(srcDipA))
                        {
                            CheckTrue(false, $"★ A1051(4)：读得到 `Shell/DeckInfoPopup.cs`（路径 {srcDipA}）"
                                           + " —— 读不到时这一条**等于没验**，所以当场红（⛔ 不静默放过）");
                        }
                        else
                        {
                            var hitsA4 = new List<string>();
                            var linesA4 = System.IO.File.ReadAllLines(srcDipA);
                            for (int ia = 0; ia < linesA4.Length; ia++)
                            {
                                string ta = linesA4[ia].TrimStart();
                                if (ta.StartsWith("//")) continue;                    // 注释不算（含 `///`）
                                int pa = ta.IndexOf("systemCopyBuffer", System.StringComparison.Ordinal);
                                if (pa < 0) continue;
                                string tailA = ta.Substring(pa + "systemCopyBuffer".Length);
                                bool assignA = false;
                                for (int ka = 0; ka < tailA.Length; ka++)
                                {
                                    if (tailA[ka] != '=') continue;
                                    char pv = ka > 0 ? tailA[ka - 1] : ' ';
                                    char nx = ka + 1 < tailA.Length ? tailA[ka + 1] : ' ';
                                    if (pv == '=' || pv == '!' || pv == '<' || pv == '>' || pv == '+') continue;
                                    if (nx == '=') continue;
                                    assignA = true; break;
                                }
                                if (assignA)
                                    hitsA4.Add("行" + (ia + 1) + ":" + ta.Substring(0, Mathf.Min(60, ta.Length)));
                            }
                            CheckTrue(hitsA4.Count == 0,
                                      "★ A1051(4)：`Shell/DeckInfoPopup.cs` 源码里**没有** `systemCopyBuffer` 的赋值"
                                    + " —— 写剪贴板那份实现**全库唯一** = `Deck/DeckRuntime.CopyDeckToClipboard`"
                                    + "（两处写同一条规则 = 迟早不一致）；⛔ 断言里不写行号（读文本扫）"
                                    + (hitsA4.Count > 0 ? "（命中：" + string.Join(" | ", hitsA4.ToArray()) + "）" : ""));
                        }
                    }

                    // ⚠️ **收尾顺序不能反**：先清模态提示窗（它会让 `ShowPreviousWindow` 把 `sPop` 提回前台），
                    //    再 `Close()` —— 反过来的话 `sPop` 会被那一跳**重新打开**，留一扇没人管的窗给后段。
                    MainMenuScene.CloseModalPopups();
                    sPop.Close();
                }
            }

            // ============================================================ 🆕 2026-10-13（账 `A1725`）
            // **「变色认得 `_tqExtra`、变灰不认」** —— `Shell/MainMenuRuntime.cs` 那两处（5 个导航钮 + 3 张模式卡）
            // 把**可见的**那颗图形（挂在**兄弟节点**下）用 `WindowButton.TintOn(...)` 接进色偏，
            // 而 `GrayTargets()` 改前只收「`_nine` + `target` + 本钮子树」⇒ 置灰时那颗可见件**纹丝不动**
            // —— 与 `A1719`（九宫格只灰 1/9）是**同一族**：灰 / 亮两条路认的不是同一组图形件。
            // 判据 = 原版 `EverguildButtonHelper.GetGraphicsInChildren(gameObject, includeInactive: 1)`
            //   ⇒ 「按钮这一组图形件**全部**」，而 `m_TargetGraphic`（= 我们 `TintOn` 的等价物）就在那一组里
            //   （判据 → `Shell/WindowButton.cs` 头部那段 + `TintOn` / `GrayTargets()` 的注释）。
            // ⚠️ **今天零影响**（那两处都**没有灰站点**，见 `MainMenuRuntime.cs` 现读）⇒ 这是**潜伏**缺口，
            //   所以造一枚探针钉住它，而不是去改那两处（它们本来就没要求变灰）。
            Section("A1725：`TintOn` 那一族置灰（变色认得它、变灰也要认得）");
            {
                var tRoot = new GameObject("A1725 TintGrayProbe");
                var tBtn = new GameObject("Btn").transform;
                tBtn.SetParent(tRoot.transform, false);
                // ① 可见件那一颗 —— **命中区的兄弟**（与 `MainMenuRuntime` 那两处同形）
                var tVis = new GameObject("Visible");
                tVis.transform.SetParent(tBtn, false);
                var tVisQ = ImageQuad.Create(tVis.transform, CardArt.Solid(), Vector3.zero,
                                             LayoutSpace.Px(40f), new Vector2(0.5f, 0.5f), "VisibleQuad");
                // ② 对照件：**既不 `TintOn`、也不在命中区子树里**（证明灰的是【那一族】，不是「顺手灰光兄弟」）
                var tCtl = new GameObject("Control");
                tCtl.transform.SetParent(tBtn, false);
                var tCtlQ = ImageQuad.Create(tCtl.transform, CardArt.Solid(), Vector3.zero,
                                             LayoutSpace.Px(40f), new Vector2(0.5f, 0.5f), "ControlQuad");
                // ③ 透明命中区（`WindowButton` 挂在它身上 —— 子树里只有这一颗）
                var tHit = new GameObject("Hit");
                tHit.transform.SetParent(tBtn, false);
                var tHitQ = ImageQuad.Create(tHit.transform, CardArt.Solid(), Vector3.zero,
                                             LayoutSpace.Px(40f), new Vector2(0.5f, 0.5f), "HitQuad");
                var tWb = tHit.AddComponent<WindowButton>();
                if (tVisQ != null) tWb.TintOn(tVisQ);        // ← 被测的那一跳（第四组）

                // 原版那张灰 shader 的**内部名**（字面量，⛔ 不读我们的常量 —— 否则是自证）
                const string tGrey = "Everguild/UI/Greyscale";
                string tMatOf(ImageQuad q)
                {
                    if (q == null) return "<没有那颗 quad>";
                    var mr = q.GetComponent<MeshRenderer>();
                    var m = mr != null ? mr.sharedMaterial : null;
                    return m != null && m.shader != null ? m.shader.name : "<没有材质>";
                }

                CheckTrue(tVisQ != null && tCtlQ != null && tHitQ != null, "（前提）探针三颗 quad 都建出来了");
                CheckTrue(WindowButton.GrayShaderAvailable,
                          "（前提）原版灰化 shader 取得到 —— 取不到时变灰本来就**静默失效**（`MissingGrayArt` 那条路），"
                        + "下面几条断的就不是 A1725 这件事了");
                if (tVisQ != null && tCtlQ != null && tHitQ != null && WindowButton.GrayShaderAvailable)
                {
                    var tVisMat0 = tVisQ.GetComponent<MeshRenderer>().sharedMaterial;
                    CheckTrue(tMatOf(tVisQ) != tGrey, "（前提）出厂**不是**灰材质（实得 `" + tMatOf(tVisQ) + "`）");
                    CheckTrue(!tWb.GrayedForTest, "（前提）出厂这一颗**没灰**");

                    // ---- ① 门关上 ⇒ `TintOn` 那一族**也要**跟着灰 ----
                    //   ⚠️ 量的是 `TintOn` 传进去的**那一颗 quad 自己的材质**（可见件在兄弟节点下、不在命中区子树里）。
                    tWb.Interactable = false;
                    CheckTrue(tWb.GrayedForTest, "★ A1725：`Interactable = false` ⇒ 这一颗**自称灰了**（`GrayedForTest`）");
                    CheckTrue(tMatOf(tVisQ) == tGrey,
                              "★ A1725：**`TintOn` 指定的那一族【也】变灰了**（可见件在兄弟节点下、不在本钮子树里）"
                            + " —— 🧨 改坏法：把 `Shell/WindowButton.cs` 的 `GrayTargets()` 里 `_tqExtra` 那一组删掉"
                            + " ⇒ **这一条红**（那颗可见件**一个像素都不变**，实得 `" + tMatOf(tVisQ) + "`）");
                    var tVisMat1 = tVisQ.GetComponent<MeshRenderer>().sharedMaterial;
                    CheckTrue(tVisMat1 != tVisMat0,
                              "★ A1725：置灰换的是**另一份材质对象**（⛔ 不是把原材质改了个名 —— 观感真的变了）");
                    CheckTrue(tMatOf(tHitQ) == tGrey,
                              "★ A1725 对照：命中区**子树**那一颗照旧灰（第一组没被这条改坏，实得 `" + tMatOf(tHitQ) + "`）");
                    CheckTrue(tMatOf(tCtlQ) != tGrey,
                              "★ A1725 对照：**既不 `TintOn`、也不在子树里**的那一颗**没被灰**"
                            + "（证明灰的**正是**那一族，⛔ 不是「顺手把兄弟节点下的东西全灰光」；实得 `" + tMatOf(tCtlQ) + "`）");

                    // ---- ② 灭自证：还原那一路必须**逐件拿回原来那一份材质对象** ----
                    //   只断「灰了」的话，把 `_tqExtra` 那一组删掉、再顺手让 `GrayedForTest` 恒真也未必露馅
                    //   ⇒ 这里断的是**引用**（同一个材质对象），与 ① 那两条**结构上不可能同时满足**：
                    //     · 压根没灰 ⇒ ① 的前两条红；· 灰完没还回去 / 还了另一份 ⇒ 下面这两条红。
                    tWb.Interactable = true;
                    CheckTrue(!tWb.GrayedForTest, "★ A1725：置回可交互 ⇒ 退出灰化");
                    CheckTrue(tMatOf(tVisQ) != tGrey,
                              "★ A1725：…`TintOn` 那一族也退出灰化（实得 `" + tMatOf(tVisQ) + "`）");
                    var tVisMat2 = tVisQ.GetComponent<MeshRenderer>().sharedMaterial;
                    CheckTrue(tVisMat2 == tVisMat0,
                              "★ A1725 灭自证：还原后那一颗拿回的是**原来那一份材质对象**（引用相等）");
                    // 第二遍：再灰一次 —— 证明这一组是**每次现算**的（`GrayTargets()` 不缓存），
                    // 不是「第一次碰巧对了、后面全错」。
                    tWb.Interactable = false;
                    int tAgain = 0;
                    if (tMatOf(tVisQ) == tGrey) tAgain++;
                    if (tMatOf(tHitQ) == tGrey) tAgain++;
                    tWb.Interactable = true;
                    Check(tAgain, 2, "★ A1725：**再来一轮**仍然对（`TintOn` 那一族 + 子树那颗都灰）—— 这一组是每次现算的");
                }
                Object.DestroyImmediate(tRoot);
            }

            // ============================================================ 🆕 2026-10-18（账 `A1035`）
            //  文案**改长了**之后的溢出闸 —— 三处空态 / 占位句：
            //   · `Shell/DeckSelectionPopup.cs` 的 `Empty Collection Warning/Warning`
            //     键 `MenuCollection/NoDecksFound`（中文列「没有可选的卡组」**7 字 → 11 字**
            //     「没有符合当前筛选的卡组」）
            //   · `Shell/DeckInfoPopup.cs` 的 `Warlord Name` 与 `Shell/PracticeModePopup.cs` 的 `Warlord Name`
            //     键 `MenuDeck/Error/NoWarlord`（中文列「未选战将」**4 字 → 6 字**「还没有选战将」）
            //
            //  🔴 **断的是「渲染宽度 ≤ 框宽」，⛔ 不是「字号对不对」** —— 本项目 2026-09-22 那条教训：
            //    字号对而溢出，自检照样全绿（`AutoFitBox` 那一族）。
            //  ⚠️ 三处**都没接 autosize**（现读核过）：`Shell/DeckSelectionPopup.cs` 那颗是
            //    `MenuDraw.Text(_emptyNode, SvRect, …, 36f, QDsText, SvRect.W)` —— 传了 `wrapPx`、**没传**
            //    `autoMinPx`（缺省 0 ⇒ `MenuDraw.TextCore` 那道 `if (autoMinPx > 0f && fontPx > autoMinPx)`
            //    为假 ⇒ **不调 `SetAutoFitBox`**）；另两处走各自文件的 `Txt(...)` 私有 helper，那两个 helper
            //    **直接 `Label.Create`**、连 `autoMinPx` 这个形参都不存在 ⇒ 更不可能自适应。
            //    ⇒ 这三段字**只能靠框本身放得下**，超了就画到框外（原版那几颗正是固定框 / `NoWrap`）。
            //  ⚠️ **量法 = `LabelRenderedPx`**（TMP 自己那块 `textBounds` 的**活值**，本文件 A750/A796′ 那份），
            //    ⛔ 不是 `Label.WorldW`（那是 `RefreshBounds()` 写的**字段缓存** —— 与被测实现同一个口，自证）。
            //  ⚠️ **期望值（框宽）在断言处按原版矩形的字面量现场写出来**，⛔ 不引实现里的常量名
            //    （同式自证：改实现它跟着绿）。
            Section("A1035：三处空态/占位句 —— 渲染宽度 ≤ 框宽（文案改长之后的溢出闸）");
            {
                // 取**中文列**那一份 —— A1035 改长的**就是它**；而 `Loc.T` 按当前语档取，
                // 自检可能跑在任一语档下（`Loc.Default = Chinese`，但玩家改过就未必）⇒ 这里显式取中文列。
                // ⚠️ 临时切、finally 切回；`PersistOverride = true` 挡住写盘（⛔ 自检不许动玩家的真设置）。
                // ⛔ 别用 `??` 兜底：`Loc.T` 缺键**返回的是键名本身**（不是 null），要判缺键请先 `Loc.HasEntry`。
                string ZhCol(string key)
                {
                    var back = Loc.Current; bool po = Loc.PersistOverride;
                    Loc.PersistOverride = true;
                    string v;
                    try { Loc.SetLanguage(AvailableLanguages.Chinese); v = Loc.T(key); }
                    finally { Loc.SetLanguage(back); Loc.PersistOverride = po; }
                    return v;
                }
                // 把**两列都量一遍**、取宽的那一个（只量当前语档 = 半边绿；中文列正是改长的那一列）。
                // ⚠️ 量法 = `LabelRenderedPx`（TMP 自己那块 `textBounds` 的**活值**）。
                // ⚠️ 返回 0 = 两列都取不到字 ⇒ 调用方**必须显式红**（⛔ 不许 `0 ≤ 框宽` 假绿）。
                float WidestOf(Label lb, string key, out string widestText)
                {
                    float w = 0f; widestText = null;
                    string zh = ZhCol(key), cur = Loc.T(key);
                    for (int iv = 0; iv < 2; iv++)
                    {
                        string s = iv == 0 ? zh : cur;
                        if (string.IsNullOrEmpty(s)) continue;
                        lb.SetText(s);
                        float px = LabelRenderedPx(lb).x;
                        if (px > w) { w = px; widestText = s; }
                    }
                    return w;
                }

                // ---------------------------------------------------------------- ① 选卡组窗的空态
                // 框 = 原版那颗 `Empty Collection Warning` 的矩形 `194.50,208.63 → 1759.50,986.69`
                // （出处 = 本文件 `DeckSelectionPopup` 那一节的 `CheckAt(ewn, 194.5f, 1759.5f, 208.6f, 986.7f, …)`）。
                const float EmptyWarnBoxW = 1565f;          // = 1759.50 − 194.50
                {
                    CheckTrue(Loc.HasEntry("MenuCollection/NoDecksFound"),
                              "（前提）`MenuCollection/NoDecksFound` 在语言表里"
                            + "（缺键时 `Loc.T` 返回的是**键名本身** —— 那时下面量的是键名，等于没验）");
                    // 造「筛选之后一副都不剩」那一态：拿一个**没有任何卡组在用**的模式号去筛。
                    const int NoDeckMode = 9999;
                    for (int iq = 0; iq < CollectionData.DeckCount(); iq++)
                        CheckTrue(CollectionData.DeckAt(iq).GameMode != NoDeckMode,
                                  "（前提）没有哪一副卡组用模式 " + NoDeckMode + "（下面那个筛子才筛得空）");
                    var esel = DeckSelectionPopup.Create(win.Manager, null, NoDeckMode);
                    CheckTrue(esel != null, "（前提）自开一扇 `Deck Selection Popup`（模式筛到空）");
                    if (esel != null)
                    {
                        esel.TryOpen(null);
                        esel.SwitchTab(true);                  // 「我的卡组」页才吃模式筛（预组页不筛）
                        // 🆕 **2026-10-19（第五十六会话 · `N46` 收尾）**：选卡组弹窗的**滚动灵敏度**。
                        //   期望值 = 原版 `Deck Selection Popup/…/Deck Display/Collection Display/Deck Scroll View`
                        //   （带页签那份同值）那台 `ScrollRect.m_ScrollSensitivity` = **100**；
                        //   判据 → `资料/普查产出_1017/UI/D_N29_滚动灵敏度逐视图.md` §「选卡组弹窗」两行。
                        //   ⛔ 不读 `MenuScroll.DefaultNotchPx`（自证）。
                        //   ⚠️ 这条以前一直没补，是因为 `DeckSelectionPopup` 的生产宿主不是本自检文件 ——
                        //      但本文件**现场开了一扇**（上面那句 `Create`）⇒ 就在这里咬住它。
                        CheckNear(esel.Scroll != null ? esel.Scroll.NotchPx : -1f, 100f, 0.001f,
                                  "选卡组弹窗滚动灵敏度 = **100**（原版 `Deck Selection Popup/…/Deck Scroll View`）");
                        Check(esel.ShownCount, 0, "（前提）这一页被筛空了 ⇒ 空态那句话才真上屏（非空时它是空串）");
                        var ewnA = FindChild(esel.transform, "Empty Collection Warning");
                        var ewtA = ewnA != null ? FindChild(ewnA, "Warning") : null;   // ⛔ 不写 "…/Warning"：`FindChild` 不认斜杠路径
                        var elbA = ewtA != null ? ewtA.GetComponentInChildren<Label>(true) : null;
                        CheckTrue(elbA != null, "（前提）空态那行字的 `Label` 取得到（原版节点名 `Warning`）");
                        if (elbA != null)
                        {
                            CheckTrue(!string.IsNullOrEmpty(elbA.Text),
                                      "（前提）空态那件**真写着字**（⛔ 不是空串 —— 空串时 TMP 的 `textBounds` 是哨兵 4.29e9）");
                            string widestA;
                            float ewPx = WidestOf(elbA, "MenuCollection/NoDecksFound", out widestA);
                            CheckTrue(ewPx > 0.1f && ewPx <= EmptyWarnBoxW + 1f,
                                      $"★ A1035①：`MenuCollection/NoDecksFound` **渲出来 {ewPx:F1}px ≤ 框宽 {EmptyWarnBoxW:F1}px**"
                                    + "（框 = 原版 `Empty Collection Warning` 的矩形 194.50→1759.50；"
                                    + "最宽那一档 = 「" + (widestA ?? "<取不到>") + "」）"
                                    + " —— 🧨 改坏法：把词条中文列再改长到放不下 ⇒ 这条红"
                                    + "（⚠️ 这一颗接了 `wrapPx`，所以它只抓「一整段连不成行」那一档；另两处连折行都没有，更硬）");
                        }
                        PointerLayer.UnregisterOwnedBy(esel.gameObject);   // 自开的窗自己把滚动登记撤掉（A867 那条不变量）
                        esel.Close();
                        Object.DestroyImmediate(esel.gameObject);
                    }
                }

                // ---------------------------------------------------------------- ② 卡组信息窗的空战将位
                // 框 = 原版 `Warlord Name` 的**跑后**矩形 `872.9,166.7 → 1359.9,216.7`
                // （出处 = 本文件那一节的 `CheckNear(… , 872.9f, 2f, "`Warlord Name` 左缘 = 872.9")`
                //   —— 原版 hAlign=Left ⇒ 它是从 872.9 起、右沿 1359.9）。
                const float DiWlBoxW = 487f;                // = 1359.9 − 872.9
                {
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/NoWarlord"),
                              "（前提）`MenuDeck/Error/NoWarlord` 在语言表里");
                    var dwPop = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.View);
                    CheckTrue(dwPop != null, "（前提）自建一扇 `Deck Info Popup`");
                    if (dwPop != null)
                    {
                        dwPop.TryOpen(null);
                        var dwnT = FindChild(dwPop.transform, "Warlord Name");
                        var dwnL = dwnT != null ? dwnT.GetComponentInChildren<Label>(true) : null;
                        CheckTrue(dwnL != null, "（前提）`Warlord Name` 那颗 `Label` 取得到");
                        if (dwnL != null)
                        {
                            // ⚠️ 本夹具里 `Warlord(0)` 非空 ⇒ 走不到「空战将位」那一支 ⇒ **自己把它造出来**
                            //    （`Label.SetText` 是 public 的，且这一扇窗是本段自建的、改完就销毁）。
                            string widestD;
                            float dwPx = WidestOf(dwnL, "MenuDeck/Error/NoWarlord", out widestD);
                            CheckTrue(dwPx > 0.1f && dwPx <= DiWlBoxW + 1f,
                                      $"★ A1035②：`MenuDeck/Error/NoWarlord` **渲出来 {dwPx:F1}px ≤ 框宽 {DiWlBoxW:F1}px**"
                                    + "（框 = 原版 `Warlord Name` 跑后矩形 872.9→1359.9；那一行**不折行、不自适应**；"
                                    + "最宽那一档 = 「" + (widestD ?? "<取不到>") + "」）"
                                    + " —— 🧨 改坏法：把词条中文列再改长到放不下 ⇒ 这条红");
                        }
                        dwPop.Close();
                        Object.DestroyImmediate(dwPop.gameObject);
                    }
                }

                // ---------------------------------------------------------------- ③ 练习窗的空战将位
                // 框 = `Shell/PracticeModePopup.cs` 给 `Warlord Name` 的那个**固定框**
                // `1373.78,269.62 → 1726.64,303.62`（= 该文件的 `WnL/WnT/WnR/WnB` 四个字面量，
                // 下面按 1726.64 − 1373.78 现算 ⇒ ⛔ 不引实现里的常量名）。
                // ⚠️ **如实记**：原版那颗**宽 0 + `ContentSizeFitter` 撑开**、「右沿取 `Deck Name` 的 `DnR`」
                // 🔶 自己设计（非原版）：练习窗战将名框宽（右沿取 Deck Name 的 `DnR`） · 原版那颗宽 0 + ContentSizeFitter 撑开，右沿取谁是我们的版面约束（原版判据拿不到） · TODO(谁定的)｜原注：**是我们挑的**（见 `Shell/PracticeModePopup.cs` `BuildDeckInfoHead` 的头注）
                //    ⇒ 这一条的「框宽」**不是原版数字**，是**我们自己的版面约束**；
                //    它证明的是「这段字放得进我们给它的那一格」，⛔ 不是「与原版一致」。
                const float PrWlBoxW = 352.86f;             // = 1726.64 − 1373.78
                {
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/NoWarlord"),
                              "（前提）`MenuDeck/Error/NoWarlord` 在语言表里（与 ② 同一条键）");
                    var pwPop = PracticeModePopup.Create(win.Manager);
                    CheckTrue(pwPop != null, "（前提）自建一扇 `Practice Mode Menu`");
                    if (pwPop != null)
                    {
                        pwPop.TryOpen(null);
                        var pwnT = FindChild(pwPop.transform, "Warlord Name");
                        var pwnL = pwnT != null ? pwnT.GetComponentInChildren<Label>(true) : null;
                        CheckTrue(pwnL != null, "（前提）`Warlord Name` 那颗 `Label` 取得到");
                        if (pwnL != null)
                        {
                            // 同上：自己造出「空战将位」那一支（本夹具的卡组有督军）。
                            string widestP;
                            float pwPx = WidestOf(pwnL, "MenuDeck/Error/NoWarlord", out widestP);
                            CheckTrue(pwPx > 0.1f && pwPx <= PrWlBoxW + 1f,
                                      $"★ A1035③：`MenuDeck/Error/NoWarlord` **渲出来 {pwPx:F1}px ≤ 框宽 {PrWlBoxW:F1}px**"
                                    + "（框 = 本窗给 `Warlord Name` 的固定框 1373.78→1726.64；那一行**不折行、不自适应**；"
                                    + "最宽那一档 = 「" + (widestP ?? "<取不到>") + "」）"
                                    + " —— 🧨 改坏法：把词条中文列再改长到放不下 ⇒ 这条红");
                        }
                        PointerLayer.UnregisterOwnedBy(pwPop.gameObject);
                        pwPop.Close();
                        Object.DestroyImmediate(pwPop.gameObject);
                    }
                }
            }

            // ---------------- 🆕 2026-10-04（§三第29条 A31）：8 颗钮**按 state 显隐** ----------------
            //   判据（唯一出处）= `DeckInfoControls__Initialize.c` 的 SetActive :60-112 + 「offset → 节点名」
            //   对照表（**在 `Shell/DeckInfoPopup.cs` 文件头**）。逐颗：
            //     `Edit Deck` ← `state < 2` · `Select Deck` ← `context.SelectButton != null`
            //     · `Share`/`Share On Chat`/`Delete`/`Duplicate` ← `state == 0 && isPlayerDeck`（**四颗同一条**）
            //     · `Practice Deck` ← `isPlayerDeck && state ∈ {0,2}` · `Switch Deck Info` ← **常显**
            //   `isPlayerDeck` = 原版 **`InventoryManager.HasItem(context.Deck)`**（`DeckInfoPopup__Open.c:43-48`）
            //     —— 我们三处调用点拿的都是玩家自己的卡组 ⇒ 恒 true。
            //   🔴 **真红法**（2026-10-04 逐条推演订正过 —— X3 审查的 **R6**：原来说「任何一行写成恒真/恒假
            //     都必有一条红」**不成立**）：下面 ①～③ 那三条**只覆盖 state 0 与 state 2**，而
            //       · `Practice Deck` 这一行**两态都该露** ⇒ 写成恒 `true` 全绿；
            //       · `isPlayerDeck`（那个 `mine`）默认 true、**没有任何断言把它置 false** ⇒ 丢掉这个合取项全绿。
            //     ⇒ ④ / ⑤ 两条就是给它们补「该藏」的那一态（判据里有、我们的调用点没有，
            //       但字段是 public、自检造得出）。**改坏法**：把 `mine` 换成恒 `true` ⇒ ④ 红；
            //       把 `Practice Deck` 那行的 `state ∈ {0,2}` 换成恒 `true` ⇒ ⑤ 红；
            //       把 `state < 2` 换成 `state < 3` / `<= 1` ⇒ ⑤ 那条「`Edit Deck` 仍露」红。
            Section("`Deck info Popup`：8 颗钮**按 state 显隐**（A31）");
            {
                var mi0 = win.Manager;
                // ① state 2（View）= 原版 `DeckGeneralInfoDemo.CardInDeckInfoButtonOnClick` 那一态
                //    （= 我们的 `PracticeModePopup.ShowDeckContent` **该给**的那一态 —— 见文件头「仍欠」①）
                var v2 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.View);
                mi0.OpenWindow(v2);
                CheckTrue(v2 != null && v2.State == DeckInfoPopup.DeckInfoState.View, "开出一扇 `state = 2`（View）的");
                // 🆕 **2026-10-09（A229）**：`Shell/DeckInfoPopup.cs` 的 `Txt` helper 补了
                //   `basis == parent` 守卫（与同文件 `Nine` 同一条口径、同一句话）—— 这里造 **`basis != parent`**
                //   **真调一次**，断「**会出声**」（✅ 本仓有现成的「出声」断言范式：`Application.logMessageReceived`
                //   计数，先例 `Editor/RewardsScene.cs` 的 `hWarn` 那一段）。
                //   判据 = 本文件自己的约定（`DeckInfoPopup.Local3` 的注释 + `Nine` 的守卫）：坐标一律页面绝对 px、
                //   `basis` 与 `parent` 给同一个节点；不等 ⇒ 世界位置 = `RectCenter + (parent.position − basis.position)`。
                //   🔴 **改坏法**：删掉 `Txt` 开头那句 `if (!ReferenceEquals(basis, parent)) Debug.LogWarning(…)`
                //   ⇒ 反例读出 **0** 条警告 ⇒ 红（正例照样 0 ⇒ 这一条断的是**守卫本身**，不是「有没有警告」）。
                if (v2 != null)                                   // ⚠️ 窗都没开出来时，上面那条已经红了 —— 这里别 NRE
                {
                    var probeT = new GameObject("A229_TxtProbe", typeof(RectTransform)).transform;
                    probeT.SetParent(v2.transform, false);
                    probeT.localPosition = new Vector3(0.5f, 0.5f, 0f);
                    int wSame = 0, wDiff = 0; string lastW = null;
                    Application.LogCallback hSame = (c, s, t) =>
                    { if (t == LogType.Warning && c != null && c.Contains("`Txt` 的 `basis`")) wSame++; };
                    Application.logMessageReceived += hSame;
                    var lbSame = v2.TxtBasisProbeForTest(probeT, probeT);          // 正例：同一个 ⇒ 不许出声
                    Application.logMessageReceived -= hSame;
                    Application.LogCallback hDiff = (c, s, t) =>
                    { if (t == LogType.Warning && c != null && c.Contains("`Txt` 的 `basis`")) { wDiff++; lastW = c; } };
                    Application.logMessageReceived += hDiff;
                    var lbDiff = v2.TxtBasisProbeForTest(probeT, v2.transform);    // 反例：不同 ⇒ 必须出声
                    Application.logMessageReceived -= hDiff;
                    CheckTrue(lbSame != null && lbDiff != null, "A229：两次探针都**真调到了** `Txt`（都建出了标签）");
                    Check(wSame, 0, "A229 正例：`basis == parent` ⇒ **不出声**（守卫不误报）");
                    Check(wDiff, 1, "★ A229 反例：`basis != parent` ⇒ **出一条警告**"
                                    + "（读 0 = 守卫被删掉了）" + (lastW != null ? "；实得：" + lastW : ""));
                    Object.DestroyImmediate(probeT.gameObject);
                }
                // 🆕 A83②（A81 的尾巴）：压暗层（「点窗外关窗」）那条不变量 —— 档 = **压暗层自己那一档**
                //   `QDI`(3120)，**严格低于**本窗内容命中区档 `QDIHit`(3129)；并核「这节点确实是
                //   公共件 `MenuDraw.ShadeHit` 建的」。期望值全是本窗自己的**原版档常量**（⛔ 不从被测实现里读）。
                //   ⚠️ 它与本窗那颗**带按钮脸的** `CloseHit`（`Shell/DeckInfoPopup.cs` 的
                //   `Hit(root, root, "CloseHit", …)`，**现 `CardPresentation/Shell/DeckInfoPopup.cs`**）**不是一件事**（两颗都要有）；
                //   ✅ **2026-10-07（波 4 件① A126）行号订正**：这里原来写的是旧号 `780`、并说源文件那两处注释
                //   「引的还是旧行号（已漂，本件没动它）」—— **三处都订正了**：源文件那两处注释
                //   （现在在 `本文件` 那一块 / `本文件` 那一块）与**本行**一律改成实读出来的 `本文件`。
                //   错因：那颗节点被同批别的改动推下去过两次（旧号 `701` → `780` → 现 `847`），
                //   而引用它的注释**没跟着走**。⛔ 下次再引行号，先 `grep -n '"CloseHit"'` 实读一遍
                //   （⚠️ **本件自己又把它推下去了一次** —— 波 4 件① 在它上面加了注释块 ⇒ 822 → 847）。
                //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档（它由窗口自己那句
                //      `Solid(root, root, …, "Menu Dark Background")` 建，**另一处代码 + 另一个对象**）。
                MenuDraw.CheckShadeRule(CheckTrue, "卡组信息窗", v2.ShadeHit,
                                        v2.transform.Find("Menu Dark Background"), DeckInfoPopup.QDIHit);
                CheckTrue(!v2.IsItemShown("Btn:Edit Deck"), "state 2 ⇒ `Edit Deck` **藏起来**（原版 `state < 2`）");
                CheckTrue(!v2.IsItemShown("Opt:Share") && !v2.IsItemShown("Opt:Share On Chat")
                          && !v2.IsItemShown("Opt:Delete") && !v2.IsItemShown("Opt:Duplicate"),
                          "state 2 ⇒ **四颗同一条**（`Share` / `Share On Chat` / `Delete` / `Duplicate`）全藏");
                CheckTrue(v2.IsItemShown("Btn:Practice Deck"),
                          "state 2 ⇒ `Practice Deck` **仍露**（原版 `isPlayerDeck && state ∈ {0,2}`）");
                CheckTrue(v2.IsItemShown("Opt:Switch Deck Info"), "`Switch Deck Info` **常显**（原版那件没看到 SetActive）");
                CheckTrue(v2.Btn("Edit Deck") != null && v2.Opt("Delete") != null,
                          "…藏起来的那几颗**节点还在**（是 `SetActive(false)`，不是没建 —— 位置/命中区都还按原版摆着）");
                // ② **同一条路的两态**：只把 `State` 改回 0 再摆一次 ⇒ 那五颗必须回来
                v2.State = DeckInfoPopup.DeckInfoState.Edit;
                v2.ApplyStateVisibility();
                CheckTrue(v2.IsItemShown("Btn:Edit Deck") && v2.IsItemShown("Opt:Delete")
                          && v2.IsItemShown("Opt:Duplicate") && v2.IsItemShown("Opt:Share")
                          && v2.IsItemShown("Opt:Share On Chat"),
                          "把 `State` 改回 **0** ⇒ `Edit Deck` 与 4 颗圆钮**都回来了**"
                          + "（同一份实现的两态对比 —— 写成恒真/恒假这里就红）");
                // ③ `Select Deck`：**给了 `context.SelectButton` 才露**（原版 6 个调用点全传 null ⇒ 我们默认恒藏）
                var v3 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Edit, true);
                mi0.OpenWindow(v3);
                CheckTrue(v3.IsItemShown("Btn:Select Deck"),
                          "给了 `SelectButton` ⇒ `Select Deck` **露**（原版 `context.SelectButton != null`）");
                CheckTrue(!v2.IsItemShown("Btn:Select Deck"),
                          "…而没给的那一扇**不露**（两条一起才是判据；只断一条分不出「恒露」和「按判据露」）");
                // ④ 🔴 **`isPlayerDeck` 那一半**（2026-10-04 补，X3 审查的 **R6**）：原版四颗圆钮与
                //    `Practice Deck` 都**与**了 `isPlayerDeck`（= `InventoryManager.HasItem(context.Deck)`，
                //    `DeckInfoPopup__Open.c:43-48`）。我们的三处调用点恒 true ⇒ 上面 ①～③ **抓不到**
                //    「把这个合取项丢掉」。这里造一扇 `IsPlayerDeck = false` 的（字段是 public，
                //    ⚠️ 与铁律 5·c 同理：**一个值 ≠ 全部情况**）。
                var v4 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Edit);
                v4.IsPlayerDeck = false;                  // Create 之后、OpenWindow（会跑 Build→ApplyStateVisibility）之前
                mi0.OpenWindow(v4);
                CheckTrue(!v4.IsItemShown("Opt:Share") && !v4.IsItemShown("Opt:Share On Chat")
                          && !v4.IsItemShown("Opt:Delete") && !v4.IsItemShown("Opt:Duplicate"),
                          "`isPlayerDeck = false` ⇒ 四颗圆钮**全藏**（原版 `state == 0 && isPlayerDeck` 的**后半条**）"
                          + " —— 把 `mine` 写成恒 `true` 只有这一条会红");
                CheckTrue(!v4.IsItemShown("Btn:Practice Deck"),
                          "…而且 `Practice Deck` **也藏**（原版 `isPlayerDeck && state ∈ {0,2}`）");
                CheckTrue(v4.IsItemShown("Btn:Edit Deck"),
                          "…但 `Edit Deck` **照旧露**（原版只看 `state < 2`，与 `isPlayerDeck` 无关 —— 别把它也乘进去）");
                // ⑤ 🔴 **`Practice Deck` 那一行不能恒 true**（R6 的反例①）：state 0 与 state 2 **两态都该露**
                //    ⇒ 上面 ①～③ 一条都抓不到它。state 1（Import）是判据里「该藏」的那一态
                //    （原版 `DeckInfoControls__Initialize.c:84-97` 有这条；我们**没有**这个调用点，
                //     但字段是 public ⇒ 自检造得出来）。
                var v5 = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.Import);
                mi0.OpenWindow(v5);
                CheckTrue(!v5.IsItemShown("Btn:Practice Deck"),
                          "state 1（Import）⇒ `Practice Deck` **藏**（原版 `state ∈ {0,2}`）"
                          + " —— 只看 state 0/2 的话这一行写成恒 `true` 也全绿");
                CheckTrue(v5.IsItemShown("Btn:Edit Deck"),
                          "…而 `Edit Deck` **仍露**（`state < 2`）—— 与 state 2 正好两态对比"
                          + "（判据写成 `state < 3` 或 `<= 1` 都在这一条上红）");
                CheckTrue(!v5.IsItemShown("Opt:Delete") && !v5.IsItemShown("Opt:Share"),
                          "…四颗圆钮藏（`state == 0` 那一半）");
                CheckTrue(v5.IsItemShown("Opt:Switch Deck Info"),
                          "…`Switch Deck Info` **常显**（三个 state 都一样，原版那件没看到 SetActive）");
                // ⚠️ **R13（本轮没修，改点在 `Shell/DeckInfoPopup.cs` 的 `IsItemShown`）**：`IsItemShown` 读的是
                //    **该节点的 `activeSelf`**、父链不参与 ⇒ 上面这些断言的前提是「窗开着」。
                //    谁要是关着窗口来断 A31，会**恒绿**（那是本工程第 N 次「弱断言分不出两态」）。
                v5.Close();
                v4.Close();
                // 🔴 **2026-10-04（A66）补的两句**：上面两扇关了，**这两扇一直没关** —— 它们会一路开着
                //   走到收工（层 3129 的 `Warlord Image` 命中区比屏还大，**顶掉后面所有的真命中路**）。
                //   实据：`_tmp_view/collection.log:6055` 那条「A11 前置：收掉 **2** 扇还开着的
                //   `Deck info Popup`」—— 全场只有这两扇从头到尾没有 `Close()`，正好 2。
                v3.Close();
                v2.Close();

                // 🆕 **2026-10-15（A796）**：压暗层「**点了会不会关**」那条 —— 走公共口
                //   `MenuDraw.CheckShadeClickRule`（唯一一份 → `Shell/MenuDraw.cs` 的 `CheckShadeClickRule`）；
                //   逐站点表 / 与账上 24 的对账 → `资料/普查产出_1015/W7_A796调用点.md`。
                //   🔴 **本口会把窗【真的关掉】** ⇒ 必须排在**本窗其它断言之后**（这里就是本窗的收尾，
                //      上面那句 `v2.Close()` 因此变成幂等收尾；同族翻车留档 → `Editor/RewardsScene.cs` 的 `Run` 里「探针跑在关闭的窗上」那一段
                //      「探针跑在关着的窗上」⇒ ⛔ 别把本块往上挪）。
                //   上面已经关过一次 ⇒ 这里先开回来：**无参** `TryOpen()` 不碰 `Data`（`Closed` 支会重建
                //      内容）⇒ 下面那颗 `ShadeHit` 是**现取**的，⛔ 别缓存成局部变量。
                CheckTrue(v2.TryOpen(), "（A796 现场）把卡组信息窗开回来 —— 下面那条要在**开着**的窗上点");
                MenuDraw.CheckShadeClickRule(CheckTrue, "卡组信息窗", v2.transform, v2.ShadeHit,
                                             () => v2.CurrentState);

                // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                //   期望矩形 = **原版 prefab** `Deck info Popup > Generic Window Red Background Big`
                //   那颗 `Image` 的 rect（`134.50, 82 → 1839.50, 1032`，2026-10-06 `rayscan` 实读）；⛔ 不写
                //   `Shell/DeckInfoPopup.cs` 里那个同名常量（那是被测实现**传进去的实参**）。
                //   ⚠️ 这一段**另开一扇**（上面那几扇的压暗层**同档** 3120 ⇒ (5,5) 上谁吃到由枚举顺序定，
                //   拿它们当现场会让「点面板外」那一步变成抛硬币）。
                {
                    var va = DeckInfoPopup.Create(mi0, 0, DeckInfoPopup.DeckInfoState.View);
                    mi0.OpenWindow(va);
                    CheckTrue(va != null && va.CurrentState == WindowState.Open, "（A94 现场）又开出一扇 `Deck info Popup`");
                    if (va != null)
                        CheckAbsorbRule("卡组信息窗", va.transform, "AbsorbHit",
                                        134.50f, 82f, 1839.50f, 1032f,
                                        DeckInfoPopup.QDI, DeckInfoPopup.QDIHit, () => va.CurrentState);
                }
            }

            // ---------------- 🆕 2026-10-03（A10 的尾巴）：`Practice Deck` ⇒ 挑对手 ⇒ 进练习赛 ----------------
            //   判据（唯一一处）= 原版 `Everguild.MatchMakerManager.StartMatch(PlayModes, PlayerBattleData,
            //   CardDeck **playerDeck**, CardDeck **enemyDeck**, …)` 的**形参名**
            //   （签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/MatchMakerManager.cs:136`）：
            //     · 点 `Practice Deck` 的那一副 = `DeckInfoPopup.context.Deck` ⇒ **playerDeck（我的）**
            //     · 之后在 `DeckSelectionPopup` 里选的那一副 = 回调形参 ⇒ **enemyDeck（对手）**
            //     · 开打前 `GameStaticData.CheckHiddenCardsInDeck(我的那副)` ⇒ 有隐藏卡就弹提示、**不开打**
            //   ⚠️ 本工程 2026-10-03 之前把这两副写反了（`项目任务.md` §三第29条 A10 的提要）—— 已就地订正。
            //   🔴 **真红法（先说清把哪一行改坏它会红）**：
            //     · `DeckInfoPopup.SelectPracticeOpponentDeck` 里 `modeFilter: info.GameMode` 改成 `null` ⇒ 「按模式筛」红
            //     · `DeckInfoPopup.StartPracticeMatch` 里把「我的」与「对手」换个来源（例如 `mine := 选中的那副`、
            //       `OpponentDeck := 被点的那副`）⇒ 下面「我 / 对手」两条同时红
            //     · `PracticeModePopup.StartBotBattle` 里 `SetPendingOpponentDeck(OpponentDeck)` 那行删掉 ⇒ 「放进通道」红
            //     · `PracticeModePopup.HasHiddenCards` 里 `ForceHiddenCardsDeck` 那一支删掉 ⇒ 隐藏卡那两条红
            //     · `PracticeModePopup.StartPracticeMatch` 里去掉 `w.StartBattle()` ⇒ 「选定即开打」那两条红
            Section("`Practice Deck`：这一副 = 我的 · 挑**对手** · 选定即开打（A10 尾巴）");
            {
                int curSaved = CollectionData.CurrentIndex();
                var mi = CollectionData.DeckAt(0);                    // 被点的那一副（自检给第 1 套塞了**合法的 30 张** —— 见 `Run()` 开头那个夹具块）
                // 🔴 让「按模式筛」这一条**真有鉴别力**：14 套测试卡组默认**全是经典(0)** ⇒
                //    不区分模式的话「筛了」与「没筛」结果一样（筛了也红不了 = 等于没查）。
                //    把**最后一副**改成遭遇(13)，验完还原（只改内存，`DeckStore` 指向临时文件）。
                int flipIdx = CollectionData.DeckCount() - 1;
                int flipSaved = flipIdx > 0 ? CollectionData.Raw(flipIdx).GameMode : 0;
                if (flipIdx > 0) CollectionData.Raw(flipIdx).GameMode = (int)GameMode.Skirmish;
                DeckSelectionPopup.LastOpened = null;
                DeckInfoPopup.LastOpponentSelection = null;
                PracticeModePopup.LastOpened = null;
                PracticeModePopup.ClearPendingOpponentDeck();

                var dp = win.OpenDeckInfo(0);
                var dpb = dp != null ? dp.Btn("Practice Deck") : null;
                var dpw = dpb != null ? dpb.GetComponent<WindowButton>() : null;
                CheckTrue(dpw != null, "`Practice Deck` 有点击区");
                if (dpw != null)
                {
                    dpw.ClickForTest();
                    CheckTrue(dp != null && dp.CurrentState == WindowState.Closed,
                              "点它 ⇒ **把自己关掉**（原版开完那扇窗就 `Close` 自己）");
                    var sel = DeckSelectionPopup.LastOpened;
                    CheckTrue(sel != null && sel == DeckInfoPopup.LastOpponentSelection,
                              "…并开**选卡组窗**（原版 `SelectPracticeOpponentDeck` 开的就是这扇，**不另建**）");
                    Check(sel != null ? sel.ModeFilter : null, (int?)mi.GameMode,
                          "选卡组窗按**这一副的模式**筛（原版 lambda：候选.GameMode == `context.Deck.GameMode`）");
                    CheckTrue(sel != null && !sel.OwnDecks, "起手落在**预组卡组**页（`DeckSelectionTabController.Start`）—— 对手多半是预组");
                    if (sel != null)
                    {
                        // ============================================================ 🆕 2026-10-17（D6）
                        // **本窗标题中文化** —— 判据 = **原版 prefab 上的词条**：
                        //   `Deck Selection Popup with Tabs` 的 `Instructions 2` 节点上挂着 `Localize`，
                        //   `mTerm = MenuDeck/Tip/SelectDeckAgainst`
                        //   （实读 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_4152179270747796863.json`；
                        //    它的 GameObject = `bundle_menus_assets_all/GameObject/Instructions 2.json`）。
                        //   · 中文 = **官方 zh-CN 表「选择一个卡组进行对战」**（2026-10-14 换；原值是我们自译的「选择卡组」）；
                        //     ⚠️ 用户 1017 实拍那颗（`资料/原版参照图/用户实拍_1017/更换卡组的参考.png` 右上角）印的是
                        //     「选择卡组」四个字 —— **与官方表不同**，差异已报主对话（本轮照官方表，见报告「5. 顺手发现」）。
                        //   · 英文 = 我们原来写死的那句 `Select deck`。
                        // ⛔ **版面一个字都不改**：照 prefab（那颗节点 1236.12→1770.24、中心 1503.18、居中）
                        //   —— 实拍那版右端贴分隔线，与 prefab 有漂移，**不照它**。
                        {
                            const string TitleTerm = "MenuDeck/Tip/SelectDeckAgainst";
                            CheckTrue(DeckSelectionPopup.TitleTerm == TitleTerm,
                                      "标题的词条键 = 原版 `Instructions 2` 上的 `mTerm`"
                                    + $"（`{TitleTerm}`）—— ⛔ 不是自拟的键");

                            // ---- ① 词条层：两列各钉一次（★官方 zh-CN 的中文 + 我们原来那句英文）----
                            var langBack = Loc.Current;
                            Loc.PersistOverride = true;                 // 自检不许动玩家的真设置
                            try
                            {
                                Loc.SetLanguage(AvailableLanguages.Chinese);
                                // 🔴 2026-10-14（第二十七会话）就地改期望值：MenuDeck/Tip/SelectDeckAgainst 原来是我们自译的「选择卡组」，官方 zh-CN = 「选择一个卡组进行对战」
                                CheckText(Loc.T(TitleTerm), "选择一个卡组进行对战",
                                          "★ 中文列 = 官方 zh-CN 表的值（⚠️ 用户 1017 实拍那颗是「选择卡组」，两者不同）");
                                Loc.SetLanguage(AvailableLanguages.English);
                                CheckText(Loc.T(TitleTerm), "Select a deck to play against",
                                          "★ 英文列 = 官方 EN（🔴 **2026-10-14（`W4`/`A1803`）就地改期望值**："
                                        + "原先这里是**我们写死的** `Select deck`，官方 `MenuDeck/Tip/SelectDeckAgainst`"
                                        + " 的 EN 是 `Select a deck to play against`）");
                            }
                            finally { Loc.RestoreForTest(langBack); Loc.PersistOverride = false; }

                            // ---- ② 实况：窗上那颗标题的字 = **词条按语言取出来的那一串** ----
                            //   ⛔ 不拿我们传进去的字面量当期望值（那是同义反复）——
                            //   期望值**从 `Loc` 现取**，比的是「窗上那颗跟不跟词条走」。
                            //   🧨 改坏法：把 `Build()` 里那句改回 `MenuDraw.Text(..., "Select deck", ...)`
                            //   ⇒ **在中文档下这一条立刻红**（英文档下它会假绿 —— 见报告里如实记的这条限度）。
                            var ins2 = FindChild(sel.transform, "Instructions 2");
                            var ins2Lb = ins2 != null ? ins2.GetComponentInChildren<Label>() : null;
                            CheckTrue(ins2Lb != null,
                                      "（前提）标题那一格取得到（原版节点名 `Instructions 2`）"
                                    + " —— 取不到的话下面那条恒真");
                            if (ins2Lb != null)
                                CheckText(ins2Lb.Text, Loc.T(TitleTerm),
                                          "★ 标题 = `Loc.T(\"MenuDeck/Tip/SelectDeckAgainst\")`"
                                        + "（⛔ 不是写死的英文）");
                        }

                        // ============================================================ 🆕 2026-10-17（D11 + D12）
                        // 判据一律**原版 prefab 的字段/字面量**（⛔ 不写我们 `DeckSelectionPopup` 里的常量 ——
                        // 那些是被测实现传进去的实参，写它们 = 自证）：
                        //  ① **D11 空态**：原版只有 `Deck Scroll View/Empty Collection Warning` **一件**
                        //     （`DeckCollectionDisplay.emptyWarning` 指向它），出厂 `act=F`；
                        //     rect = **194.5,208.6 → 1759.5,986.7**（= 整个视口那一格），
                        //     子件一颗 TMP 名叫 `Warning`：**fs 36.0 · base 36.0 · 无 auto · 折行=1 · 色(1,1,1,1)**。
                        //     实读命令：`python 工具/menu_dump.py bundle_menus_assets_all "Deck Selection Popup with Tabs" --depth 6`
                        //  ② **D12 页签**：原版两颗 `EverguildToggle` 的序列化字段 —— **两颗共用同一对**
                        //     `onSprite = 40K_tab_button` / `offSprite = 40K_tab_button_overwindow`、
                        //     `onColor = (1,0.6308285,0,1)` / `offColor = (1,0.5442529,0,1)`；
                        //     🔴 **两个开关都开着**：`changeSpriteOnValueChange = 1` · `colorTintOnValueChange = 1`
                        //     （逐个 MB 现读：`MonoBehaviour_907016068568626559`（`m_IsOn=1`）/ `…_-4477857345849663325`（`m_IsOn=0`）；
                        //      七个同名件的字段逐字相同）⇒ 换图与染色**两条都真的生效**。
                        //     落地 = `decomp_full/EverguildToggle__RefreshVisuals.c` → `ToggleSprite` / `ToggleTint`
                        //     （按**自己那颗** `m_IsOn` 取图/取色）；两颗 `Button Text` 的 TMP 都是 `(1,1,1,1)`。
                        // 🔴 **判别式**：两张底图必须是**按状态换的** —— 起手一套、切页之后**互换**。
                        //   原来那版把两张图写死在各颗身上 ⇒ 切页后这一组**必红**（本轮改成按 `on` 取）。
                        // ⚠️ **每次都要现取节点**：`SwitchTab` → `RebuildAll()` 会把整窗子件销毁重建，
                        //   缓存的 `Transform` 立刻变成「已销毁」（Unity 的 `== null` 认得出）⇒ 用旧引用会**假红**。
                        {
                            System.Func<string, Transform> tq = n => FindChild(sel.transform, n);
                            CheckTrue(!sel.OwnDecks, "（前提）起手在预组页 ⇒ 下面「哪颗是选中的」不是猜的");
                            var tPre0 = tq("Generic Tab UI Button");      // 原版出厂 `m_IsOn=1` 的那颗（= 预组页签）
                            var tOwn0 = tq("Generic Tab UI Button 1");    // 出厂 `m_IsOn=0` 的那颗（= 我的卡组页签）
                            CheckTrue(tPre0 != null && tOwn0 != null,
                                      "两个页签节点都在（原版两颗的名字：`Generic Tab UI Button` / `…1`）");
                            // ---- D12 · 起手（预组页）----
                            CheckArt(tPre0, "40K_tab_button",
                                     "★ 预组页签（`m_IsOn=1`）底图 = 原版 `onSprite`");
                            CheckArt(tOwn0, "40K_tab_button_overwindow",
                                     "★ 我的卡组页签（`m_IsOn=0`）底图 = 原版 `offSprite`");
                            CheckTint(tPre0, new Color(1f, 0.6308285f, 0f, 1f),
                                      "★ 选中那颗的染色 = 原版 `EverguildToggle.onColor` (1,0.6308285,0,1)");
                            CheckTint(tOwn0, new Color(1f, 0.5442529f, 0f, 1f),
                                      "★ 未选中那颗的染色 = 原版 `offColor` (1,0.5442529,0,1)");
                            // 文字**两态都白**（原版两颗 `Button Text` 的 TMP 都是 `(1,1,1,1)`，
                            // 而且标签上没有 `EverguildButtonMaterialModifier` ⇒ `ToggleTint` 染不到它）。
                            // 🔴 **判别力来自「两颗必须一样」**：改前那版是「选中白 / 未选中 (0.6,0.6,0.6)」
                            //    ⇒ 只要两颗**不相等**就红（⛔ 不靠「比我传进去的那个字面量」自证）。
                            var tLbPre = tq("Tab Text Pre") != null ? tq("Tab Text Pre").GetComponentInChildren<Label>() : null;
                            var tLbOwn = tq("Tab Text Own") != null ? tq("Tab Text Own").GetComponentInChildren<Label>() : null;
                            CheckTrue(tLbPre != null && tLbOwn != null,
                                      "（前提）两颗页签的文字都取得到 —— 否则下面那两条会退化成恒真");
                            if (tLbPre != null && tLbOwn != null)
                            {
                                var cPre = tLbPre.color; var cOwn = tLbOwn.color;
                                CheckTrue(Mathf.Abs(cOwn.r - cPre.r) < 2f / 255f
                                          && Mathf.Abs(cOwn.g - cPre.g) < 2f / 255f
                                          && Mathf.Abs(cOwn.b - cPre.b) < 2f / 255f,
                                          "★ 选中 / 未选中两颗的文字**颜色相同**（原版两颗 `Button Text` 都是 (1,1,1,1)）"
                                        + $"—— 实测选中 ({cPre.r:F3},{cPre.g:F3},{cPre.b:F3}) / 未选中 ({cOwn.r:F3},{cOwn.g:F3},{cOwn.b:F3})；"
                                        + "「一白一灰」就是改前那套自创写法");
                                CheckTrue(Mathf.Abs(cPre.r - 1f) < 2f / 255f && Mathf.Abs(cPre.g - 1f) < 2f / 255f
                                          && Mathf.Abs(cPre.b - 1f) < 2f / 255f,
                                          $"…而且那个共同值 = **白**（原版 `(1,1,1,1)`）—— 实测 ({cPre.r:F3},{cPre.g:F3},{cPre.b:F3})");
                            }
                            // ---- D11 · 空态那一件（**预组页**非空 ⇒ 它必须**关着**，且**只有它一件**）----
                            // ⚠️ 这一段排在切页**之前**（还在预组页）：预组页的列表来自 `PrebuiltDecks.Tab`，
                            //    「非空」是**已知前提**（那条表在仓库里）；「我的卡组」页则可能被模式筛空 ⇒
                            //    在那儿断「非空」会变成一条没意义的红。
                            CheckTrue(sel.ShownCount > 0, "（前提）预组页列表非空 ⇒ 下面那条不是空转");
                            var ewn = tq("Empty Collection Warning");
                            CheckTrue(ewn != null,
                                      "空态那一件叫 **`Empty Collection Warning`**（原版 `DeckCollectionDisplay.emptyWarning` 那个节点名）");
                            CheckTrue(ewn != null && !ewn.gameObject.activeSelf,
                                      "★ 列表非空 ⇒ 空态那一件**关着**");
                            CheckAt(ewn, 194.5f, 1759.5f, 208.6f, 986.7f,
                                    "★ 空态那一件 = 原版 rect **194.5,208.6 → 1759.5,986.7**（整格视口，⛔ 不是我们原来那个 (SvL,SvT+120)-(SvR,SvT+190) 小框）");
                            var ewt = ewn != null ? FindChild(ewn, "Warning") : null;
                            CheckTrue(ewt != null, "…它下面那颗 TMP 叫 `Warning`（原版就叫这个）");
                            var ewl = ewt != null ? ewt.GetComponentInChildren<Label>() : null;
                            if (ewl != null)
                                CheckNear(ewl.FontPxNow, 36f, 0.6f,
                                          "★ 空态那行字 **fs = 36**（原版 `Warning` 的 `m_fontSize` = 36.0，**无 auto**）");
                            CheckTrue(tq("Scope Note") == null,
                                      "★ 我们自己加的那行 `Scope Note` **已删**（原版空态只有 `Empty Collection Warning` 一件）"
                                    + " —— 改坏法：把 `Build()` 第 7b 步那两行 `MenuDraw.Text` 加回来 ⇒ 本条红");
                            // ---- D12 · 切页 ⇒ 两图**互换**（判别式那一刀）----
                            sel.SwitchTab(true);
                            CheckTrue(sel.OwnDecks, "（前提）切到「我的卡组」页了");
                            CheckArt(tq("Generic Tab UI Button"), "40K_tab_button_overwindow",
                                     "★ 切页后**预组那颗**变成 `offSprite` …");
                            CheckArt(tq("Generic Tab UI Button 1"), "40K_tab_button",
                                     "★ …而**我的卡组那颗**变成 `onSprite` ⇒ 底图**按状态换**"
                                   + "（改坏法：像原来那样把两张图写死在各自身上 ⇒ 这两条同时红）");
                            CheckTint(tq("Generic Tab UI Button"), new Color(1f, 0.5442529f, 0f, 1f),
                                      "★ 预组那颗染色也跟着换成 `offColor`");
                            CheckTint(tq("Generic Tab UI Button 1"), new Color(1f, 0.6308285f, 0f, 1f),
                                      "★ 我的卡组那颗染色换成 `onColor`");
                            sel.SwitchTab(false);              // 还原成预组页（下一句自己会再切过去验模式筛选）
                        }

                        sel.SwitchTab(true);                          // 换到「我的卡组」页才看得见模式筛选
                        // ---- 🆕 2026-10-18（A867）：切页 = **整窗重建** ⇒ 滚动登记表**一条都不涨** ----
                        //   先例（逐字同形）→ `Editor/RewardsScene.cs` 的 A510 那一条；
                        //   现场 / 改坏法 → `资料/普查产出_1018/S1_A840与A867.md` §2.2 / §4.1。
                        //   🔴 本窗这条路**不靠重开窗就够得着**（`SwitchTab` → `RebuildAll()` → `Build()`，
                        //   而页签的命中区就绑在 `SwitchTab` 上）⇒ 它是那 4 扇里**最容易复现**的一扇。
                        //   ⚠️ 先切一次**取基线** —— 那一次会顺手把待清的死条目吸掉（`RegisterScroll` 自带
                        //   `PruneScrolls`），基线之后那次才只反映「本窗重建涨不涨」。
                        sel.SwitchTab(false);                         // ← 吸基线
                        int nScrollDs = PointerLayer.ScrollCountForTest;
                        CheckTrue(nScrollDs > 0,
                                  $"（前提）滚动登记表非空（基线 {nScrollDs} 条）—— ⛔ 为 0 ⇒ 下面那条比的是 0 vs 0，"
                                + "等于没验（`PointerLayer.Instance == null` 时 `ScrollCountForTest` 恒 0）");
                        sel.SwitchTab(true);                          // ← 真·重建一次（顺带把页签还原成「我的卡组」）
                        Check(PointerLayer.ScrollCountForTest, nScrollDs,
                              $"★★ A867：切一次页签（= 整窗重建一次）⇒ 滚动登记表**一条都不涨**"
                            + $"（{nScrollDs} → {PointerLayer.ScrollCountForTest}）"
                            + "｜**改坏法**：删掉 `Shell/DeckSelectionPopup.cs` 那句 "
                            + "`PointerLayer.UnregisterOwnedBy(gameObject);` ⇒ 每切一次**净涨 1 条**"
                            + "（`Build()` 只清**子件**、窗根不死，而 `MenuScroll` 是**普通 C# 类** ⇒ "
                            + "`PruneScrolls` 那两道判断恒假），而且旧条目**还能被滚轮命中**"
                            + "（`OnChanged` 指向已销毁的节点）⇒ 本条红");
                        int wantOwn = 0;
                        for (int i = 0; i < CollectionData.DeckCount(); i++)
                            if (CollectionData.DeckAt(i).GameMode == mi.GameMode) wantOwn++;
                        Check(sel.ShownCount, wantOwn,
                              $"「我的卡组」页列出 **{wantOwn}** 副同模式的（筛多 = 没传 `ModeFilter`；筛少 = 筛错档）");
                        CheckTrue(wantOwn < CollectionData.DeckCount(),
                                  "…而且这一条**真有鉴别力**：库里还有 "
                                  + (CollectionData.DeckCount() - wantOwn) + " 副别的模式的（上面刚把最后一副改成遭遇）被筛掉了");

                        // 🆕 2026-10-04（A26）：**第二扇窗**（选卡组窗）的卡格也要**真鼠标**点得到 ——
                        //    它与收藏窗共用 `MenuDraw.DeckCell`（同一处裸节点）⇒ 一起修好的，这里也钉一条。
                        //    判据 = `PointerLayer.CollectHits`（`Shell/PointerLayer.cs` 的 `CollectHits`），不是 `wb.Click()`。
                        CheckTrue(sel.Cells.Count > 0, "选卡组窗画出了格子（否则下面那条等于没验）");
                        if (sel.Cells.Count > 0)
                        {
                            var sc0 = sel.Cells[0];
                            var sh0 = FindChild(sc0, "Hit");
                            var shq = sh0 != null ? sh0.GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(shq != null,
                                      "★ 选卡组窗的格子：命中区节点下**真的挂着 `ImageQuad`**（A26）");
                            var pl2 = PointerLayer.Instance;
                            if (shq != null && pl2 != null)
                            {
                                var sb = pl2.ButtonAt(MenuCheck.PointerFramePx(sc0).x, MenuCheck.PointerFramePx(sc0).y);   // 指针帧（喂 PointerLayer）
                                CheckTrue(sb != null && sb.transform == sh0,
                                          "★ …而**真命中路**在格中心拿到的就是这一格"
                                          + "（拿到 " + (sb == null ? "**null** = 真鼠标点不动" : "`" + sb.name + "`") + "）");
                            }

                            // 🆕 **2026-10-11（A198③）**：这一格改走 `MenuDraw.DeckCell(GameWindow, …)` 之后，
                            //   **「裁哪一块」由【本窗的 `Clip`】说了算**（= 原版模型：mask 挂在**视口节点**上），
                            //   调用点不再自己把 `SvRect` 当 `clip` 传。
                            //   ⚠️ **2026-10-14 更正（铁律 5 / A786）**：上面这半句**已过期** —— A435 乙块（A21）
                            //   把 `DeckSelectionPopup` 那一对「`Clip = SvRect;` → 循环 → `Clip = prevClip;`」
                            //   **整对删掉**了，裁切状态迁到了 `Deck Scroll View/Viewport` 那颗 `ViewportClip` 上
                            //   （`Shell/DeckSelectionPopup.cs`）⇒ 「本窗的 `Clip`」今天**恒 `null`**，
                            //   说了算的是**那颗节点**（`DeckCell(win, …)` 内部沿 `parent` 解析，同一条路）。
                            //   ⇒ 钉两条：
                            //    ① **反向对照**：完全落在视口里的那一格**不许被裁**（裁多了 = 命中区比格还小 ⇒
                            //       四角点不到；也把「`Clip` 取错矩形」这一类错误抓出来）；
                            //    ② 压在视口边上的那一格：**命中区 = 格 ∩ 窗 `Clip`**（整格高 ⇒ 压根没吃到窗口那一份）。
                            //   ⚠️ 期望值用 `CellW/CellH`（**原版 prefab 的格尺寸 225×364.5**，同族已有多条断言钉着它）
                            //     —— 这里比的是**「有没有被多裁」与「交集算没算」**，不是格子摆在哪。
                            //   ⛔ 别去读 `sel.Clip` 来断这件事 —— 要断的是**裁切的结果**，不是那个字段的瞬时值。
                            //     ⚠️ **2026-10-14 更正（铁律 5 / A786）**：原来这里给的理由是「`RebuildCells`
                            //     是「临时设 → 循环 → 还原」（还原后是 null）」—— **那个理由已经过期**：
                            //     A435 乙块（A21）已把 `DeckSelectionPopup` 里那对
                            //     「`Clip = SvRect;` → 循环 → `Clip = prevClip;`」**整对删掉**
                            //     （`Shell/DeckSelectionPopup.cs` 的更正留档）⇒ 那个 `Clip` 现在**恒 `null`**
                            //     （不是「还原之后才是 null」），读它照样恒红。
                            //     ✅ **结论一个字没改**（照样别读它、照样恒红），变的只是理由。
                            //   ⚠️ 反向对照那一格是**现找**的（要求它**整格**在视口里 —— 视口 778.06 > 格高 364.5
                            //     ⇒ 起手**必然**有）：⛔ 别拿 `Cells[0]` 顶替（滚动位置一变它就可能压边 ⇒ 假红）。
                            Transform inC = null;
                            for (int i = 0; i < sel.Cells.Count; i++)
                            {
                                var c = sel.Cells[i];
                                if (c == null) continue;
                                float cy = MenuCheck.DesignPx(c).y;
                                if (cy - DeckSelectionPopup.CellH * 0.5f >= DeckSelectionPopup.SvRect.y1 - 0.5f
                                    && cy + DeckSelectionPopup.CellH * 0.5f <= DeckSelectionPopup.SvRect.y2 + 0.5f)
                                { inC = c; break; }
                            }
                            if (inC == null)
                                CheckTrue(false, "（前提）选卡组窗里至少有一格**整格**落在视口里"
                                                 + "（视口 778.06 高于格高 364.5 ⇒ 必然有；一格都没有 = 前面那套建格就错了）");
                            else
                            {
                                var inH = FindChild(inC, "Hit");
                                var inQ = inH != null ? inH.GetComponentInChildren<ImageQuad>() : null;
                                if (inQ == null)
                                    CheckTrue(false, "（前提）整格那一格的 `Hit` 下面有 `ImageQuad`");
                                else
                                {
                                    CheckNear(inQ.WorldW * 108f, DeckSelectionPopup.CellW, 1.5f,
                                              "★ A198③ 反向对照：整格都在视口里的那格，命中区**宽 = 整格宽 225**"
                                              + "（裁过头 / `Clip` 取错矩形 ⇒ 这条红）");
                                    CheckNear(inQ.WorldH * 108f, DeckSelectionPopup.CellH, 1.5f,
                                              "★ …**高 = 整格高 364.5**（同上）");
                                }
                            }
                            Transform edge2 = null; float e2cy = 0f;
                            for (int i = 0; i < sel.Cells.Count; i++)
                            {
                                var c = sel.Cells[i];
                                if (c == null) continue;
                                float cy = MenuCheck.DesignPx(c).y;
                                if (cy + DeckSelectionPopup.CellH * 0.5f > DeckSelectionPopup.SvRect.y2 + 0.5f)
                                { edge2 = c; e2cy = cy; break; }
                            }
                            if (edge2 == null)
                            {
                                // ⚠️ 只有「这一页的格**不超过两整行**」时才允许找不到压边的格
                                //   （两整行 = `2×Cols` 格，行高 364.5 × 2 = 729 < 视口 778.06 ⇒ 整行都进得去）。
                                //   超过两整行 ⇒ **必然**有一格压在视口下沿上 ⇒ 找不到就是真缺陷（红）。
                                //   这一页的夹具 = 13 副 ⇒ 3 行（6+6+1），第 3 行那 1 格只露 49px ⇒ 一定找得到。
                                CheckTrue(sel.Cells.Count <= DeckSelectionPopup.Cols * 2,
                                          "（前提）选卡组窗这一页的格 ≤ 两整行 ⇒ 可以没有压边的格"
                                          + $"（实得 {sel.Cells.Count} 格 / 每行 {DeckSelectionPopup.Cols}）"
                                          + " —— 超过两整行却一格都找不到压边的，说明那套裁切没生效");
                                Debug.Log(P + $"  （本页 {sel.Cells.Count} 格 ≤ 两整行 ⇒ 这次没有压边的格，"
                                            + "下面那条 ★★ 本次不计入 —— 同一条机制在收藏窗卡组页那条里验到）");
                            }
                            else
                            {
                                float e2cx = MenuCheck.DesignPx(edge2).x;
                                var full2 = new PxRect(e2cx - DeckSelectionPopup.CellW * 0.5f,
                                                       e2cy - DeckSelectionPopup.CellH * 0.5f,
                                                       e2cx + DeckSelectionPopup.CellW * 0.5f,
                                                       e2cy + DeckSelectionPopup.CellH * 0.5f);
                                PxRect vis2;
                                bool part2 = MenuDraw.ClipRect(full2, DeckSelectionPopup.SvRect, out vis2)
                                             && !MenuDraw.SameRect(vis2, full2);
                                CheckTrue(part2,
                                          $"★ 前提：压边那一格必须**部分**越界（实测 `vis` = ( {vis2.x1:F1},{vis2.y1:F1} )→"
                                          + $"( {vis2.x2:F1},{vis2.y2:F1} ) vs 整格 ( {full2.x1:F1},{full2.y1:F1} )→"
                                          + $"( {full2.x2:F1},{full2.y2:F1} )）—— 不成立则下一条 ★ 等于没验");
                                if (part2)
                                {
                                    var e2h = FindChild(edge2, "Hit");
                                    var e2q = e2h != null ? e2h.GetComponentInChildren<ImageQuad>() : null;
                                    CheckNear(e2q != null ? e2q.WorldH * 108f : -1f, vis2.H, 1.5f,
                                              "★★ A198③：压边那一格的命中区**高 = 格 ∩ 窗 `Clip`** 的高"
                                              + "（`clip` 没吃到窗口那一份 ⇒ 退回整格高 ⇒ 红）");
                                }
                            }
                        }
                    }
                    // 挑一副**不是我自己**的当对手（期望值由数据算，不写死名字）
                    int foeIdx = -1;
                    for (int i = 1; i < CollectionData.DeckCount(); i++)
                        if (CollectionData.DeckAt(i).GameMode == mi.GameMode) { foeIdx = i; break; }
                    CheckTrue(foeIdx > 0, "测试库里另有一副同模式的（没有它就没法验「谁是对手」）");
                    if (sel != null && foeIdx > 0)
                    {
                        var foe = CollectionData.Raw(foeIdx);
                        sel.Pick(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = false,
                            Info = CollectionData.DeckAt(foeIdx),
                            OwnIndex = foeIdx,
                            PrebuiltDeck = null,
                        });
                        Check(sel.CurrentState, WindowState.Closed, "选完 ⇒ 选卡组窗自己关（原版 `Select` 的两步：关窗 + 回调）");
                        var prac = PracticeModePopup.LastOpened;
                        CheckTrue(prac != null, "回调 ⇒ **开练习窗并立刻开打**（原版 `StartPracticeMatch` 选完就 `StartMatch`）");
                        if (prac != null)
                        {
                            // 🆕 **2026-10-09（A229）**：`Shell/PracticeModePopup.cs` 里**带 `basis`** 的那两个
                            //   helper（`Nine` 早就有守卫、`Hit`/`HitOn` 没有）—— 探针造 `basis != parent`
                            //   **真调一次 `HitOn`**，断「**会出声**」（范式同上面 `Txt` 那一段）。
                            //   ⚠️ **如实标注**：A229 原文写的是「`PracticeModePopup` 的 **`Txt`** 缺守卫」，
                            //   而**现读该文件的 `Txt`（`CardPresentation/Shell/PracticeModePopup.cs` 那一行）根本没有 `basis` 参数**
                            //   （签名 = `(Transform parent, string text, …)`、落位用 `Local3(parent, …)`
                            //   ⇒ 结构上不可能不等）⇒ 那句判据在本文件**指向不存在的东西**；
                            //   同一形状的真身 = `HitOn`（位置 `Local3(basis, …)`、树父 `parent`）。
                            //   🔴 **改坏法**：删掉 `HitOn` 开头那句守卫 ⇒ 反例读出 0 条警告 ⇒ 红。
                            {
                                var probeH = new GameObject("A229_HitProbe", typeof(RectTransform)).transform;
                                probeH.SetParent(prac.transform, false);
                                probeH.localPosition = new Vector3(0.5f, 0.5f, 0f);
                                int hSame = 0, hDiff = 0; string lastH = null;
                                Application.LogCallback gSame = (c, s, t) =>
                                { if (t == LogType.Warning && c != null && c.Contains("`HitOn` 的 `basis`")) hSame++; };
                                Application.logMessageReceived += gSame;
                                var nSame = prac.HitBasisProbeForTest(probeH, probeH);          // 正例
                                Application.logMessageReceived -= gSame;
                                Application.LogCallback gDiff = (c, s, t) =>
                                { if (t == LogType.Warning && c != null && c.Contains("`HitOn` 的 `basis`"))
                                  { hDiff++; lastH = c; } };
                                Application.logMessageReceived += gDiff;
                                var nDiff = prac.HitBasisProbeForTest(probeH, prac.transform);  // 反例
                                Application.logMessageReceived -= gDiff;
                                CheckTrue(nSame != null && nDiff != null,
                                          "A229：两次探针都**真调到了** `HitOn`（都建出了命中区）");
                                Check(hSame, 0, "A229 正例：`basis == parent` ⇒ **不出声**");
                                Check(hDiff, 1, "★ A229 反例：`basis != parent` ⇒ **出一条警告**"
                                                + "（读 0 = 守卫被删掉了）" + (lastH != null ? "；实得：" + lastH : ""));
                                Object.DestroyImmediate(probeH.gameObject);
                            }
                            Check(prac.DeckIndex, 0,
                                  "**我** = 被点 `Practice Deck` 的那一副（原版 `playerDeck` = `DeckInfoPopup.context.Deck`）");
                            CheckTrue(prac.OpponentDeck != null && prac.OpponentDeck.Name == foe.Name,
                                      "**对手** = 刚在窗里选中的那一副（原版 `enemyDeck` = 回调回来那副）");
                            CheckTrue(prac.OpponentDeck != null && prac.OpponentDeck.Name != mi.Name,
                                      "两副**不是同一副** —— 放反了这条就红（这两副名字本来就不同）");
                            CheckTrue(prac.SearchingMatch, "选定 ⇒ 立刻进「等对手」那 12 秒（原版 `ShowPopUp(等待窗)` → `StartMatch`）");
                            CheckTrue(!prac.StartedBattle, "12 秒还没到 ⇒ 不抢跑");
                            prac.TickSearch(12f);
                            CheckTrue(prac.StartedBattle, "等满 12 秒 ⇒ **真开打**（批处理只记账；真机上这一步 `LoadScene(\"Battle\")`）");
                            Check(CollectionData.CurrentIndex(), prac.DeckIndex,
                                  "开战前把**我那一副**交给 `DeckLibrary`（`BattleDriver.PickSavedDeck` 读的就是它）");
                            var pend = PracticeModePopup.PendingOpponent;
                            CheckTrue(pend != null && pend.Name == foe.Name,
                                      "开战时把**对手那副**放进「本局对手」通道 —— `BattleDriver.BeginFromDeckLibrary` 该读的就是它");
                            CheckTrue(pend != null && pend.Name != mi.Name, "…通道里**不是**我自己那副");
                            var took = PracticeModePopup.TakePendingOpponentDeck();
                            CheckTrue(took != null && took.Name == foe.Name,
                                      "`TakePendingOpponentDeck()` 拿得到 —— 开局那条路读的就是它");
                            CheckTrue(PracticeModePopup.TakePendingOpponentDeck() == null,
                                      "**读一次就清**（下一局不会带着上一局的对手）");
                            prac.Close();       // 清理：别盖住后面的截图
                        }
                    }
                    // ② **隐藏卡**前置检查（原版 `GameStaticData.CheckHiddenCardsInDeck`）：注入 ⇒ 弹提示、**不开打**。
                    //   ⚠️ 为什么用注入：原版判据是 `PlayerItem.IsHidden()`，而这个 build 里 `RawCardScript` 没覆写它、
                    //      我们的卡数据也没有「隐藏」字段 ⇒ 不注入的话这条分支**永远走不到**（= 等于没查）。
                    // 🔴 **2026-10-12（A416）改清场法**：`Manager.ShowPopUp` 的宿主已收编成 `PopUpGameWindow`
                    //    （原版 `MessagePopupWindow`）⇒ 按 `PromptPopup` 这个【类型】清场**再也清不到它**
                    //    （会静默变成空做，模态窗留着盖住后面的截图与断言）。
                    // 🔴 **2026-10-14（A512）第三次改：口径统一到共用那一份** —— A416 当时改成的是「按
                    //    `WindowsManager.popUpWindow` 这个**字段**关一扇」（类型无关 ⇒ 收编前后都绿），
                    //    但那个字段**某一刻装的可能正是被测的那扇窗**（判据与踩过的红逐条写在
                    //    `Editor/MainMenuScene.cs` 的 `CloseModalPopups()` 文档注释里）⇒ 本工程口径 =
                    //    **按【类型】清、两类模态宿主（`PopUpGameWindow` / `PromptPopup`）一起覆盖**，
                    //    而且那一份**只有一个定义**（⛔ 别在这儿再抄一份；`Editor/RewardsScene.cs` 同族那处
                    //    也已改调它）。⚠️ 覆盖范围如实记着：它只清那两类**模态提示窗** —— `DeckInfoPopup` /
                    //    `CampaignRewardWindow` 这类别的 `Popup` 窗**不在**清场范围内（它们各有自己的收尾，
                    //    本段下面那句 `dp3.Close()` 就是一例）。
                    MainMenuScene.CloseModalPopups();
                    var dp3 = win.OpenDeckInfo(0);
                    PracticeModePopup.LastOpened = null;
                    PracticeModePopup.ClearPendingOpponentDeck();
                    PracticeModePopup.ForceHiddenCardsDeck = CollectionData.Raw(0);   // = 「我这一副」那个对象
                    var d3 = dp3 != null ? dp3.Btn("Practice Deck") : null;
                    var d3w = d3 != null ? d3.GetComponent<WindowButton>() : null;
                    if (d3w != null) d3w.ClickForTest();
                    var sel3 = DeckSelectionPopup.LastOpened;
                    if (sel3 != null && foeIdx > 0)
                        sel3.Pick(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = false,
                            Info = CollectionData.DeckAt(foeIdx),
                            OwnIndex = foeIdx,
                            PrebuiltDeck = null,
                        });
                    CheckTrue(PracticeModePopup.LastOpened == null,
                              "我自己那副带**隐藏卡** ⇒ **不开练习赛**（原版 `CheckHiddenCardsInDeck` 那一支：弹提示、不 `StartMatch`）");
                    CheckTrue(PracticeModePopup.PendingOpponent == null, "…连「本局对手」通道都不该被写上");
                    // 🔴 **2026-10-12（A416）改取窗法**：宿主已收编成 `PopUpGameWindow`，按 `PromptPopup`
                    //    这个【类型】找窗会**一路找不到**（`hp` 恒 null ⇒ 下一条断言退化成「一个提示窗都没有」
                    //    的空断）。改成读 `WindowsManager` 那个**字段**（`popUpWindow`，类型无关 ——
                    //    `ShowPopUp` 两条实现都写它）。正文节点名两边同名（`MessageText`，见
                    //    `PopUpGameWindow.Build()`）⇒ 读法一个字都不用改。
                    // 🔴 **2026-10-14（A512）补一句显式区分（⛔ 只加这一句）**：这一处读那个字段是【探针】
                    //    （看那一刻装着谁、拿它断文案），**不是清场入口** —— 清场一律走 `CloseModalPopups()`
                    //    （按【类型】清，见本段上面那一处）。两处**语义不同**，别读成「同一个写法两种说法」。
                    var wmFixD = win.Manager != null ? win.Manager : WindowsManager.Instance;
                    GameWindow hp = wmFixD != null ? wmFixD.popUpWindow : null;
                    var hpTxt = hp != null ? TextOf(FindChild(hp.transform, "MessageText")) : null;
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：提示正文从波 1b 起走词条
                    //   （`Shell/DeckInfoPopup.cs` 的 `Loc.T("MenuDeck/Error/HiddenCards") + hiddenWhy`）
                    //   ⇒ 原来写死的子串「隐藏卡」**只在中文列里出现**（英文列 = `This deck contains hidden cards…`）
                    //   ⇒ 宿主一跑英文档这条必红。期望值换成该词条的**整句**（比原来的子串**更强**，且两语档都成立）。
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/HiddenCards"),
                              "（前提）词条 `MenuDeck/Error/HiddenCards` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                    CheckTrue(hpTxt != null && hpTxt.Contains(Loc.T("MenuDeck/Error/HiddenCards")),
                              "…并且**弹出提示说清原因**（不许静默；**随语档**，键 `MenuDeck/Error/HiddenCards`）"
                            + "—— 实测文案「" + (hpTxt ?? "<没有提示窗>") + "」");
                    PracticeModePopup.ForceHiddenCardsDeck = null;
                    if (hp != null) hp.Close();
                    // 🔴 **2026-10-04（A66）**：这一扇（`dp3`）走到这里必须**显式收掉**。
                    //   它的「开完自己关」那条路在 `DeckInfoPopup.SelectPracticeOpponentDeck():667` 里，
                    //   但**只在点击真的发生了**（`d3w != null`）时才走得到 ⇒ 补一句兜底，
                    //   别让一扇层 3129 的模态一路盖到收工（那会顶掉后面所有人的真命中路）。
                    if (dp3 != null) dp3.Close();
                    // ③ 预组也能当对手（原版那条链默认就落在预组页）—— 判据只一份：`PracticeModePopup.PlayerDeckOf`
                    if (PrebuiltDecks.Available && PrebuiltDecks.Tab.Count > 0)
                    {
                        var pk = PrebuiltDecks.Tab[0];
                        var pd = PracticeModePopup.PlayerDeckOf(new DeckSelectionPopup.DeckPick
                        {
                            Prebuilt = true,
                            Info = DeckSelectionPopup.InfoOf(pk),
                            OwnIndex = -1,
                            PrebuiltDeck = pk,
                        });
                        CheckTrue(pd != null && pd.WarlordId == pk.heroId,
                                  "选预组当对手 ⇒ 搓出来的 `PlayerDeck` 督军 = 那一副的督军（" + pk.deckId + " → " + pk.heroId + "）");
                    }
                    else Debug.LogWarning(P + "   预组数据读不到 ⇒ ③ 那一条跳过了（**不是通过**）");
                }
                if (flipIdx > 0) CollectionData.Raw(flipIdx).GameMode = flipSaved;   // 还原上面动过的那一副
                CollectionData.Select(curSaved);      // 上面开战那一步会改「当前卡组」⇒ 还原
            }

            // ---------------- 🆕 2026-10-06（A132）：练习窗 `Army Selector` 的底图 + 它的吸收层 ----------------
            //   原版 `Practice Mode Menu > Deck Selector > Army Selector > Background`
            //   （`Image` · **`UI_Background faction buttons`** 54×420 · **Sliced** border (2,201,2,202) ·
            //    **`m_RaycastTarget = 1`**；矩形 = **69.42,182.18→246.54,880.17** = `Army Selector` 自己那一格）。
            //   🔴 **A94 相 1 §四·1 当时判「这一块不接」**（理由：我们那一列**没画**那颗底图）——
            //     「没画」是**真话**，但本轮把**两件一起补了**：底图 + 吸收层。
            //     只补底图会多出一块「看着是块底、点了却把窗关掉」的**死区**（图不吃射线 ⇒ 穿到压暗层）。
            //   ⛔ 期望值一律写 **prefab 字面量**（⛔ 不写 `PracticeModePopup.ArmL…` —— 那是被测实现**传进去的实参**，
            //     同式自证）；只有「档」那两个常量照旧取本窗的原版档常量（⚠️ 那是 `CheckAbsorbRule` 的入参来源，
            //     与压暗层那条 `MenuDraw.CheckShadeRule` **不是同一套判据** —— 后者 2026-10-07 起改成量场景真值）。
            //   🔴 **真红法**：把 `BuildArmySelector` 里那两行删掉 ⇒ ①（底图节点不在）红；把 `Nine(...)` 那一行
            //     留着、只删 `Absorb` 那一行 ⇒ `CheckAbsorbRule` 的①②③④⑤ 一起红。
            Section("练习窗：`Army Selector` 的底图 + 它的吸收层（A132）");
            {
                var mgrA = win.Manager;
                PracticeModePopup.LastOpened = null;
                var pw = PracticeModePopup.Create(mgrA);          // 本段专用的一扇（不借上面那扇 ——
                mgrA.OpenWindow(pw);                              //   那时它正 `SearchingMatch`，搜索窗盖着它）
                CheckTrue(pw != null && pw.CurrentState == WindowState.Open, "开出一扇练习窗（本段末尾关掉）");
                if (pw != null)
                {
                    // ① 底图**建出来了**（A132 的本体就是「视觉缺漏」⇒ 先断它，别只断吸收层）
                    var bg = FindChild(FindChild(pw.transform, "Army Selector"), "Background");
                    CheckTrue(bg != null,
                              "`Army Selector/Background` **建出来了**（原版那颗底图，A132 之前我们一颗都没有）");
                    var bgq = bg != null ? bg.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(bgq != null && bgq.Texture != null
                              && bgq.Texture.name == "UI_Background_faction_buttons",
                              "★ 画的是**原版那张** `UI_Background faction buttons`（实得 `"
                              + (bgq != null && bgq.Texture != null ? bgq.Texture.name : "<没取到>") + "`）");
                    if (bg != null)
                    {
                        // 🆕 **2026-10-19（`#86` · 账 `A1346`）**：**「前提·父链缩放」**（本批**第 10 处**）。
                        //   同形先例 = `Editor/MainMenuScene.cs` 的 `CheckParentScaleUnit`（那一族 9 处；
                        //   ⚠️ **本文件原来一条都没有** —— 别去 `CollectionScene.cs` 里找同形的那一族）。
                        // 🔴 为什么它是**前提**：下面那四沿走的是**乙式**
                        //   （`MenuDraw.UnionQuadRectPx` → `PosInDesignSpace`，**先把父级缩放除回去**）；
                        //   `A1330` 收口**之前**是**甲式**（裸 `position` × 108）。
                        //   **两式只在「父链单位缩放」时逐位相同**（差 ≤2.5e-4 px）⇒「这一处该量到什么」
                        //   依赖这条前提。⚠️ 它红了说明两式在这一处本来就不等价 ⇒ 要改的是**期望值的口径**
                        //   （按**乙式** = 设计矩形重算），⛔ **不是**把实现改回甲式。
                        // ⚠️ 本处已有 `CheckScaleTwo`（`本文件`，**写侧**两态夹具）—— ⛔ **两者不许合并**：
                        //   那一条的期望方向是 `p2 == M × p1`（写侧），本族要的是**读侧** `p2 == p1`
                        //   （判据 → 本文件 `CheckScaleTwo` 的文件头 / `Shell/MenuDraw.cs`）。
                        CheckNear(bg.lossyScale.x, 1f, 1e-3f,
                                  "（前提·父链缩放）`Army Selector/Background` 的 `lossyScale.x` = 1"
                                + "（= 该处的**乙式**读口（`MenuDraw.UnionQuadRectPx`，先除回父级缩放）与"
                                + "**甲式**（裸 `position`×108）逐位相同那一档；⚠️ 条红了该改的是**期望值口径**，"
                                + "⛔ 不是把实现改回甲式）");
                        // 🔴 **`A1392`（2026-10-19 · 第五十五会话 S3）：中心也走【乙式】** ——
                        //   这一行原来是甲式（裸 `bg.position` → `LayoutSpace.ToPixel`，即 `×108`），
                        //   而同段四沿已经走乙式（`MenuDraw.UnionQuadRectPx` → `PosInDesignSpace`，**先除父级缩放**）
                        //   ⇒ 父链一旦真带缩放，**中心与四沿分家**（今天 aspect + 父链都被钉死 ⇒ 潜伏）。
                        //   判据 = `A1350` 那条「矩形读口只留一份」：中心/四沿**必须同一个口**。
                        //   ⚠️ 父链单位缩放下两式逐位相同（差 ≤2.5e-4 px）⇒ 这条不改变今天的读数。
                        var pc = MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(bg));
                        CheckNear(pc.x, 157.98f, 1.5f,
                                  "★ 底图中心 x = **69.42..246.54 的中点**（= `Army Selector` 自己那一格）");
                        CheckNear(pc.y, 531.175f, 1.5f, "…中心 y = **182.18..880.17 的中点**");
                        // 🔴 **四沿也要断**：⚠️ 上面那条「中心 y」**分不出**「照父件画」和「照 `Viewport` 画」——
                        //    两格**中点恰好同号**（`Viewport` 上下各探出 33.11 ⇒ (149.07+913.28)/2 = 531.175
                        //    = (182.18+880.17)/2）⇒ 真判据在**上下沿**（各差 33.11）。
                        //    九宫格是 9 颗子块**正好铺满**目标矩形 ⇒ 量子块的**并集**就是底图那四沿。
                        float ux1 = float.MaxValue, uy1 = float.MaxValue;
                        float ux2 = float.MinValue, uy2 = float.MinValue;
                        // 🆕 **2026-10-10（A1330）**：四沿并集收口到 `MenuDraw.UnionQuadRectPx`
                        //    （九宫格 9 块**无缝铺满**目标矩形 ⇒ 量子块的**并集**就是底图那四沿）。
                        // 🔴 **两项原样保留**：**激活闸 = `QuadGate.None`（完全不过滤）** ·
                        //    **`searchInactive = false`** —— 原来用的就是 `GetComponentsInChildren<ImageQuad>()`
                        //    **缺省**那一档（只找激活链上的块；传 `true` 会把关掉的块也算进来）。
                        // ⚠️ 改前是**甲式**（`LayoutSpace.ToPixel` + 硬写 `108f`）；`MenuDraw` 那份是**乙式**
                        //    （位置项先 `PosInDesignSpace` 除回父级缩放、再走 `PixelOfDesign` = A298/A1004）
                        //    ⇒ 本宿主父链单位缩放下两式差 ≤2.5e-4 px（同本文件 `QuadRectOf` `本文件`
                        //    那条 A1003 先例）；父链真带缩放时**乙式才是对的**。
                        // ⚠️ 量不到时四沿**留在哨兵值**（原来是 `MaxValue/MinValue`）—— 与原来逐个累加**同契约**。
                        if (MenuDraw.UnionQuadRectPx(bg, MenuDraw.QuadGate.None, false,
                                out float ubx1, out float uby1, out float ubx2, out float uby2))
                        { ux1 = ubx1; uy1 = uby1; ux2 = ubx2; uy2 = uby2; }
                        CheckNear(ux1, 69.42f, 1.5f, "★ 底图**左沿** = 原版那一格");
                        CheckNear(uy1, 182.18f, 1.5f,
                                  "★ 底图**上沿** = **182.18**（⚠️ **不是** `Viewport` 的 149.07 —— 差 33.11，"
                                  + "这一条才分得出「照父件画」和「照视口画」）");
                        CheckNear(ux2, 246.54f, 1.5f, "★ 底图**右沿** = 原版那一格");
                        CheckNear(uy2, 880.17f, 1.5f, "★ 底图**下沿** = **880.17**（同上，不是 913.28）");
                    }
                    CheckAbsorbRule("练习窗（阵营纵列底图）", pw.transform, "Army Selector/AbsorbHitArmy",
                                    69.42f, 182.18f, 246.54f, 880.17f,
                                    PracticeModePopup.QPr, PracticeModePopup.QPrHit, () => pw.CurrentState);
                }
                if (pw != null) pw.Close();                       // 别让它盖住后面那些真命中路
            }

            // ============================================================ §A92 节点类型（2026-10-07 新增）
            //
            // 🔴 **判据 = 原版自己的节点类型**（不是我们的常量）：下面这 5 个名字在
            //    `bundle_menus_assets_all` 里**逐个实读过组件** —— `Deck info` · `Background Info` ·
            //    `General container` · `Army Selector` · `Viewport` · `Filters` · `Decks Scroll view` · `Content`
            //    **全是 `RectTransform`**（该包 16768 个 `GameObject` 里 **16510 是 `RectTransform`**，
            //    剩下 258 个裸 `Transform` 全是卡框 3D 子锚与粒子件 —— **一个菜单容器都没有**）。
            //    ⇒ `PracticeModePopup.New` 原来建的是**裸 `Transform`**（连 `rect` 都没有，宽高无从验收）——
            //    现在必须是 `RectTransform`。
            // ⚠️ **反面那一半**（原版**就是**裸 `Transform` 的件不许被顺手改齐）在 `RewardsScene` 的 §A92 那节，
            //    那边是**成对**断的（`Particle System nebula` **没有** / 上面两级宿主**有**）。
            // ⚠️ 本段自开一扇窗（照上面 A132 那段的做法），**末尾关掉并清 `LastOpened`** —— 不留状态给后段。
            Section("§A92 节点类型：`PracticeModePopup.New` 建的空节点都是 `RectTransform`");
            {
                var mgrB = win.Manager;
                PracticeModePopup.LastOpened = null;
                var pwB = PracticeModePopup.Create(mgrB);
                mgrB.OpenWindow(pwB);
                CheckTrue(pwB != null, "开出一扇练习窗（本段末尾关掉）");
                if (pwB != null)
                {
                    var a92sel = FindChild(pwB.transform, "Army Selector");
                    CheckTrue(a92sel != null && a92sel.GetComponent<RectTransform>() != null,
                              "`Army Selector` 是 **`RectTransform`**（原版那一件是 `ScrollRect` 的宿主）");
                    var a92vp = a92sel != null ? FindChild(a92sel, "Viewport") : null;
                    CheckTrue(a92vp != null && a92vp.GetComponent<RectTransform>() != null,
                              "`Army Selector/Viewport` 是 **`RectTransform`**（原版挂 `RectMask2D` 的那一件）");
                    var a92fil = a92vp != null ? FindChild(a92vp, "Filters") : null;
                    CheckTrue(a92fil != null && a92fil.GetComponent<RectTransform>() != null,
                              "`…/Viewport/Filters` 是 **`RectTransform`**（原版 `GridLayoutGroup` 的格容器）");
                    var a92di = FindChild(pwB.transform, "Deck info");
                    CheckTrue(a92di != null && a92di.GetComponent<RectTransform>() != null,
                              "`Deck info` 是 **`RectTransform`**（右半那块面板的根）");
                    var a92gc = a92di != null ? FindChild(a92di, "General container") : null;
                    CheckTrue(a92gc != null && a92gc.GetComponent<RectTransform>() != null,
                              "`Deck info/General container` 是 **`RectTransform`**（原版出厂可见那个抽屉）");
                    // 命中区那一族（`PracticeModePopup.HitOn` 是本窗自己的工厂）—— 原版这一层 = 按钮自己的 `RectTransform`
                    var a92th = FindChild(pwB.transform, "ToggleHit");
                    CheckTrue(a92th != null && a92th.GetComponent<RectTransform>() != null,
                              "`ToggleHit`（`HitOn` 那条路建的透明命中区）是 **`RectTransform`**"
                              + "（原来建的是裸 `Transform`；判据同 `MenuDraw.Hit`）");
                }
                if (pwB != null) pwB.Close();
                PracticeModePopup.LastOpened = null;
            }

            // ---------------- `Import Deck Popup`（A1 §4）----------------
            Section("`Import Deck Popup`：版面 + **导入闭环**（A1 §4）");
            {
                // ============================================================ ⑨ 占位符**走词条**（两语档各断一个字面量）
                // 🔴 **2026-10-17（F2 · 修 (α)：这条断言原来把期望值写死成英文）** —— 本批 B11/A884 已经把
                //   占位符改成走词条（`Shell/ImportDeckPopup.cs` 的 `PlaceholderTerm` =
                //   `MenuDeck/HUD/EnterText`，`Core/Loc.cs` 中文列 = 「输入文本...」（官方 zh-CN 表，2026-10-14 换）），而本宿主跑在
                //   **出厂语言 = 中文**（`Core/Loc.cs` 的 `Default`）⇒ 写死 `"Enter text..."` 必红。
                //   正本写法 → `Editor/ShellScene.cs` 的 B11/A884 那一节（那边明写「⛔ 写死 `"Enter text..."`
                //   或写死中文，都过不了这一条」）。
                // 🔴 **为什么这一节另开两扇临时窗、而且必须排在本块那扇 `imp` 之前**：
                //   占位符那行字是 `ImportDeckPopup.Build() → RefreshInputText()` 里
                //   `Label.Create(holder, empty ? Loc.T(PlaceholderTerm) : _text, …)` **当场建出来的**
                //   ⇒ **换语言不会让已经建好的那扇重读词条**（`Loc` 不发事件，见那颗常量的 doc）
                //   ⇒ 要看另一档就得**重开一扇**；而 `WindowsManager.OpenWindow` 的弹窗那一支会把
                //   **当前窗压到背景**（`currentWindow.ToBackground()`）⇒ 若排在本块那扇之后，
                //   后面那串「失败**不关窗**」（`Check(imp.CurrentState, WindowState.Open, …)`）就会红在夹具上。
                var langImp = Loc.Current;
                Loc.RestoreForTest(AvailableLanguages.Chinese);      // 自检口：只改内存，⛔ 不写 `PlayerPrefs`
                var wZh = win.OpenImportPopup();
                string phZh = wZh != null ? TextOf(FindChild(wZh.transform, "Input Text")) : null;
                string msgZh = wZh != null ? TextOf(FindChild(wZh.transform, "Main Search message")) : null;
                string okZh = wZh != null ? TextOf(FindChild(wZh.transform, "Confirm Text")) : null;
                if (wZh != null) wZh.Close();
                Loc.RestoreForTest(AvailableLanguages.English);
                var wEn = win.OpenImportPopup();
                string phEn = wEn != null ? TextOf(FindChild(wEn.transform, "Input Text")) : null;
                string msgEn = wEn != null ? TextOf(FindChild(wEn.transform, "Main Search message")) : null;
                string okEn = wEn != null ? TextOf(FindChild(wEn.transform, "Confirm Text")) : null;
                if (wEn != null) wEn.Close();
                Loc.RestoreForTest(langImp);                         // 收尾：语言放回原值
                CheckTrue(wZh != null && wEn != null, "（前提）两语档各开出了一扇 `Import Deck Popup`"
                          + "（取不到 ⇒ 下面几条恒红，⛔ 不静默）");
                // 🔴 2026-10-14（第二十七会话）就地改期望值：MenuDeck/HUD/EnterText 原来是我们自译的「输入文字...」，官方 zh-CN = 「输入文本...」
                Check(phZh, "输入文本...", "★ 空输入时显示**占位符**：**中文档** = `Loc.T(\"MenuDeck/HUD/EnterText\")`"
                    + " 的**中文列**（2026-10-14 官方 zh-CN 表 = 「输入文本...」，原「输入文字...」）");
                Check(phEn, "Enter text...", "★ …**英文档** = 同一条词条的**英文列**"
                    + "（= 原版那颗 TMP 的 `m_text` 原文逐字符照抄，含末尾那三个点）");
                CheckTrue(!string.IsNullOrEmpty(phZh) && phZh != phEn,
                          "★ 判别式：两语档下那句字**必须不一样** —— ⛔ 写死 `\"Enter text...\"`（改前那样）"
                        + "或写死中文，都过不了这一条");
                // 🆕 **2026-10-17（A891）：提示行 / `Confirm` 钮 也走词条了**（同一扇窗的另两颗，判据同占位符那一条）——
                //   `Main Search message` 的词条 = `MenuDeck/Share/PasteDeck`、`Confirm Text` 的词条 =
                //   `MainMenu/General/Confirm`（⛔ 别按行号找那两条老断言，它们在本节后半，已改成**随语档**的期望值）。
                // 🔴 2026-10-14（第二十七会话）就地改期望值：MenuDeck/Share/PasteDeck 原来是我们自译的「粘贴你的卡组」，官方 zh-CN = 「粘贴您的卡组」
                Check(msgZh, "粘贴您的卡组", "★ **提示行（标题）**：**中文档** = `Loc.T(\"MenuDeck/Share/PasteDeck\")` 的中文列"
                    + "（2026-10-14 官方 zh-CN 表 = 「粘贴您的卡组」，原「粘贴你的卡组」）");
                Check(msgEn, "Paste your deck", "★ …**英文档** = 同一条词条的英文列"
                    + "（= 原版 `Main Search message` 那颗 TMP 的 `m_text` 原文）");
                CheckTrue(!string.IsNullOrEmpty(msgZh) && msgZh != msgEn,
                          "★ 判别式：**标题**两语档必须不一样 —— ⛔ 写死 `\"Paste your deck\"`（改前那样）过不了");
                Check(okZh, "确认", "★ **`Confirm` 钮**：**中文档** = `Loc.T(\"MainMenu/General/Confirm\")` 的中文列"
                    + "（⚠️ 是我们译的 —— 源 `数据/本地化/i18n/zh_CN.csv:83`）");
                Check(okEn, "Confirm", "★ …**英文档** = 同一条词条的英文列"
                    + "（= 原版 `Button Text` 那颗 TMP 的 `m_text` 原文；那颗挂着 `Localize`）");
                CheckTrue(!string.IsNullOrEmpty(okZh) && okZh != okEn,
                          "★ 判别式：**`Confirm`** 两语档必须不一样 —— ⛔ 写死 `\"Confirm\"`（改前那样）过不了");

                var imp = win.OpenImportPopup();
                CheckTrue(imp != null, "开得出来");
                if (imp != null)
                {
                    var iroot = imp.transform;
                    // 🆕 A17：`Confirm`（九宫底 `40K_button`）与绿色关闭钮逐个悬停验一遍
                    CheckHoverSwap(imp.transform, "Import Deck Popup");
                    Check(imp.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(imp.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                    CheckAt(FindChild(iroot, "Generic Popup Background"), 560f, 1360f, 234.07f, 685.93f,
                            "`Window`（560,234.07 → 1360,685.93）");
                    // 🔴 **2026-10-17（A891）**：期望值**按当前语档取** —— 原来写死 `"Paste your deck"`，
                    //   而提示行 B11 那批改走词条、宿主又跑中文档 ⇒ 那条**必红**。⛔ 不是删掉，是换成随语档的字面量；
                    //   两语档的**固定值**在 ⑨ 那一段各断一条（那一对才是不自证的口径）。
                    CheckText(TextOf(FindChild(iroot, "Main Search message")),
                              // 🔴 2026-10-14（第二十七会话）就地改期望值：MenuDeck/Share/PasteDeck 原来是我们自译的「粘贴你的卡组」，官方 zh-CN = 「粘贴您的卡组」
                              Loc.Current == AvailableLanguages.English ? "Paste your deck" : "粘贴您的卡组",
                              "提示行文案（随当前语档）");
                    var ph = FindChild(iroot, "Input Text");
                    // ⚠️ 占位符那条断言**已挪到本块最前面**（两语档各断一个字面量 + 判别式）——
                    //   见上面 ⑨ 那一段：这里原来是 `CheckText(TextOf(ph), "Enter text...", …)`，
                    //   而宿主跑在中文档 ⇒ **期望值过期**（B11/A884 起占位符走词条）。⛔ 别在这里补第二条
                    //   （同一条规则两处写 = 迟早不一致；`ph` 本身还被下面那条 y 落位断言用着 ⇒ 保留）。
                    // 🔴 实拍抓的：占位符第一版**跑到输入框上面去了**（`basis` 给了 root 而 parent 是 `Window`）
                    var phLb = ph != null ? ph.GetComponent<Label>() : null;
                    CheckTrue(phLb != null && Mathf.Abs(MenuCheck.DesignPx(ph.transform).y - 441.03f) <= 1.5f,
                              $"占位符**落在输入框里**（中心 y 实测 {(ph != null ? MenuCheck.DesignPx(ph.transform).y : -1f):F1}"
                              + "，原版 `Text Area` 377→505.06 ⇒ 中心 **441.03**）");
                    // 同上（A891）：`Confirm` 钮的字也走词条了（原版 `Button Text` 挂着 `Localize`）⇒ 期望值随语档。
                    CheckText(TextOf(FindChild(iroot, "Confirm Text")),
                              Loc.Current == AvailableLanguages.English ? "Confirm" : "确认",
                              "确认钮文案（随当前语档）");
                    Shoot("06_收藏_ImportDeck弹窗.png");   // ⚠️ **趁窗开着拍**（第一版拍在导入成功之后 ⇒ 窗已经关了）
                    // ⚠️ 别拿九宫格的**根**量宽 —— `GetComponentInChildren<ImageQuad>()` 取到的是**角块**
                    //    （第一版量出 190.76 = 一个角）。量**点击区那个单 quad**（= 整个按钮矩形）。
                    CheckNear(imp.OkHit != null ? Wpx(imp.OkHit) : -1f, 478.343f, 2f,
                              "确认钮宽 = **478.343**（原版；VLG 只有一个钮 ⇒ 在容器里居中）");
                    var okBtn = imp.OkHit != null ? imp.OkHit.GetComponent<WindowButton>() : null;
                    CheckTrue(okBtn != null, "`Confirm` 有点击区");
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：这两句从波 1b 起走词条
                    //   （`Shell/CollectionData.ImportDeck` → `Loc.T("MenuDeck/Error/ImportEmpty")` /
                    //   `Loc.T("MenuDeck/Error/ImportBadString")`，见 `Core/Loc.cs` 的 `ImportBadString`）。
                    //   原来写死的那两句中文**只是中文列的副本** ⇒ 宿主一切英文档这两条必红（**单语档断言**）。
                    //   期望值改成 `Loc.T(键)` ⇒ 两语档都成立（中文档下取到的字与改前**逐字相同**，值不变）。
                    CheckTrue(Loc.HasEntry("MenuDeck/Error/ImportEmpty")
                              && Loc.HasEntry("MenuDeck/Error/ImportBadString"),
                              "（前提）词条 `MenuDeck/Error/Import{Empty,BadString}` 都在表里"
                            + "（⛔ 不在 ⇒ 下面两条两边一起退化成**键名** = 假绿）");
                    // ① 空串
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, Loc.T("MenuDeck/Error/ImportEmpty"),
                              "空串 ⇒ 给**人话**（**与卡组编辑那边逐字一致** —— 判据只有 `CollectionData.ImportDeck` 一份；"
                            + "期望值随语档 = `Loc.T(\"MenuDeck/Error/ImportEmpty\")`）");
                    Check(imp.CurrentState, WindowState.Open, "失败**不关窗**");
                    // ② 乱串
                    imp.SetTextForTest("这不是一条卡组串");
                    if (okBtn != null) okBtn.Click();
                    CheckText(imp.ErrorText, Loc.T("MenuDeck/Error/ImportBadString"),
                              "乱串 ⇒ 另一句人话（期望值随语档 = `Loc.T(\"MenuDeck/Error/ImportBadString\")`）");
                    // ③ 真串（拿卡组 1 导出的串再导回来）
                    int n0 = CollectionData.DeckCount();
                    var src = DeckLibrary.ExportString(DeckLibrary.Load().Decks[0]);
                    imp.SetTextForTest(src);
                    if (okBtn != null) okBtn.Click();
                    Check(CollectionData.DeckCount(), n0 + 1, "合法串 ⇒ 卡组数 +1（**导入真的接上了**）");
                    Check(imp.CurrentState, WindowState.Closed, "成功 ⇒ 窗关上");
                    // ④ 点背景也关（原版 `backgroundCloseButton`）
                    var imp2 = win.OpenImportPopup();
                    var shade = imp2 != null && imp2.ShadeHit != null ? imp2.ShadeHit.GetComponent<WindowButton>() : null;
                    CheckTrue(shade != null, "背景有点击区（原版 `backgroundCloseButton`）");
                    // 🆕 2026-10-04（**A25⑥ 的断言模板**）：压暗层的命中区**档**必须落在压暗层自己那一档、
                    //   且**严格低于**本窗内容命中区档 —— 同档时 `ImageQuad` 的世界 z 恒 0，谁吃到退化成
                    //   「枚举顺序」，症状是**点不动的钮看着像正常工作**（A27 那批用真缺陷买来的）。
                    //   期望值 = 本窗自己的两个**原版档常量**（`QImp` / `QImpHit`），⛔ 不从被测实现里读。
                    if (imp2 != null)
                    {
                        // 🔴 A77⑬③⑥：判据收口到唯一那份 `MenuDraw.CheckShadeRule`，且期望值改成
                        //   **量**本窗那块视觉压暗层（`ImportDeckPopup` 里它叫 `Background`，
                        //   见 `Solid(root, 960f, 540f, …, "Background")`）的 quad 档。
                        MenuDraw.CheckShadeRule(CheckTrue, "导入卡组窗", imp2.ShadeHit,
                                                imp2.transform.Find("Background"), ImportDeckPopup.QImpHit);
                    }
                    if (shade != null) shade.Click();
                    Check(imp2 != null ? imp2.CurrentState : WindowState.Open, WindowState.Closed, "点背景 ⇒ 关窗");

                    // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据同上，→ `本文件` 那一段）。
                    //   ⚠️ 上面那一句是**本窗自己的**「点背景 ⇒ 关」；本块补的是**公共口**那一版
                    //   （`ShadeHit` 属于本窗 + 不是吸收层 + **点之前必须是 `Open`** 三条一起断）。
                    //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后；上面已经关过一次 ⇒ 先开回来。
                    CheckTrue(imp2 != null && imp2.TryOpen(),
                              "（A796 现场）把导入卡组窗开回来 —— 下面那条要在**开着**的窗上点");
                    if (imp2 != null)
                        MenuDraw.CheckShadeClickRule(CheckTrue, "导入卡组窗", imp2.transform, imp2.ShadeHit,
                                                     () => imp2.CurrentState);

                    // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                    //   期望矩形 = **原版 prefab** `Import Deck Popup > Window > Generic Popup Background`
                    //   那颗 `Image` 的 rect（`560, 234.07 → 1360, 685.93`）；⛔ 不写 `ImportDeckPopup.WinL…`
                    //   —— 那是被测实现**传进去的实参**（同式自证）。⚠️ 上面那扇 `imp2` 已经关了 ⇒ **另开一扇**。
                    var imp3 = win.OpenImportPopup();
                    CheckTrue(imp3 != null && imp3.CurrentState == WindowState.Open,
                              "（A94 现场）又开出一扇 `Import Deck Popup`");
                    if (imp3 != null)
                    {
                        // 🆕 **2026-10-17（`A1212` 块 8 #42/#43/#44）**：本窗那 4 处的四格。
                        //   判据 = `RA1212_切块表.md` 块 8（原始读数 `R6_包装层战斗与其余.md` §2）。
                        //   ⚠️ #45（`RebuildErrorLine` 那个**第二出生入口**的 `Error msg`）本夹具走不到
                        //      （它只在错误态才重建）⇒ 如实登记为「未断」；#43（`Build` 那颗）**断得到**。
                        CheckA1212(imp3.transform, "Main Search message", "Import Deck Popup ▸ Main Search message",
                                   4f, 50f, 36f, 1);
                        CheckA1212(imp3.transform, "Confirm Text", "Import Deck Popup ▸ Window ▸ Buttons ▸ … ▸ Button Text",
                                   12f, 45f, 12f, 0);
                        // `Error msg` 在 `Build` 里**无条件**建（只是空串 / 不可见）⇒ 这一个出生入口量得到；
                        // 第二个入口 `RebuildErrorLine`（#45）重造同名节点、四格逐值相同 —— 这一条量的是
                        // **Build 那一颗**（第二个入口只在错误态才走，本夹具不开那一态，如实登记）。
                        CheckA1212(imp3.transform, "Error msg", "Import Deck Popup ▸ Window ▸ Error msg",
                                   4f, 28f, 36f, 0);
                        CheckAbsorbRule("导入卡组窗", imp3.transform, "AbsorbHit",
                                        560f, 234.07f, 1360f, 685.93f,
                                        ImportDeckPopup.QImp, ImportDeckPopup.QImpHit, () => imp3.CurrentState);
                        // 🆕 2026-10-09（`A1125`）：关窗钮的**命中区 + 换图层**四连断 ——
                        //   🔴 **2026-10-18（`A1149` 第一半 · 第九会话 P7）就地更正**：上面原来写
                        //   「脸 `Close Icon` 也挂**窗根**（⛔ 不套按钮节点）」——**已经不是了**：
                        //   `Shell/ImportDeckPopup.cs` 现补建了原版那颗 `Generic Close Button Green` 节点，
                        //   图标 `Close Icon` 挂在**它**下面（`CloseHit` 仍在窗根）。
                        //   两条 `FindChild` 都是**递归**的 ⇒ 断言逐字不变仍绿。
                        A1125Close("ImportDeckPopup", imp3.transform, null, "CloseHit",
                                   "Close Icon", "40k_bt_close", "Close Icon", 96.37f, 94.50f);
                        // 🆕 **2026-10-18（`A1149` 第二半）**：圆底盘**实绘矩形**两连断。绿族框 **75×75 正方**
                        //   ⇒ 第二格恒真（那类窗没有等比不变量可丢）；第一格钉「用的是原版根那一格 **75**，
                        //   ⛔ 不是子件框 56.37×54.50」。
                        //   🔴 **2026-10-18（`A1149` 第一半 · 第九会话 P7）就地更正**：上面原来写「本窗**没有**
                        //   原版那颗 `Generic Close Button Green` 节点 … 圆底盘现读挂在子件 `Close Bg` 上」
                        //   ——**两件都已归真**（节点补建 + 圆底盘画在它身上）⇒ `btnName` 由 `null` 改成
                        //   **`"Generic Close Button Green"`**、第 8 实参（子件名 `"Close Bg"`）**已删**。
                        A1125CloseBase("ImportDeckPopup", imp3.transform, "Generic Close Button Green",
                                       "UI_Button_Round_background", 75.00f, 75.00f, 75.00f);
                    }
                }
            }

            // ---------------- 切页（`visualTypes` 必须整表替换） ----------------
            Section("切页（正本 §二；`visualTypes` 被本窗整表替换）");
            bool cardsPageOpened = false;                   // A1834（运行时读数）的前提：Cards 页被真正点开过
            if (win.tabButtons != null)
            {
                win.tabButtons.Click(1);
                cardsPageOpened = true;                     // A1834 运行时读数（下面那节）的前提
                Check(win.CurrentTab, WindowTabType.CollectionCards, "点第 2 键 ⇒ 切到 **Cards** 页");
                var p2 = FindChild(tabsRoot, "CardsTab");
                CheckTrue(p2 != null && p2.gameObject.activeSelf,
                          "**`CardsTab`** 开着（A288：页节点名 = `CardsTab`，⛔ 不是 `Card Collection Tab`）");
                CheckTrue(!FindChild(tabsRoot, "Select Deck Tab").gameObject.activeSelf, "`Select Deck Tab` 关着");
                win.tabButtons.Click(0);
                Check(win.CurrentTab, WindowTabType.CollectionDecks, "点回第 1 键 ⇒ 回 Decks 页");
                // 🔴 选中态：**只亮当前那个键**（第一版漏了刷高亮 —— 截图里 DECKS 页却亮着 CARDS）
                var hi = new List<Transform>();
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Highlight" && t.parent != null && t.parent.name.StartsWith("CollectionTabButton_"))
                        hi.Add(t);
                Check(hi.Count, 4, $"左栏四个键的选中底图都建了（实测 {hi.Count}）");
                int on = 0;
                foreach (var h in hi) if (h.gameObject.activeSelf) on++;
                Check(on, 1, $"**恰好一个键亮着**（实测 {on} 个）—— 「切页要跟着刷高亮」的判据");
                if (hi.Count == 4)
                    CheckTrue(hi[0].gameObject.activeSelf && !hi[1].gameObject.activeSelf,
                              "亮的是**第 1 键**（当前页 = Decks）");
            }

            // ---------------- 🆕 2026-10-20（A1834 · 运行时读数）：文字重排探针在懒建之后读 ----------------
            // 🔴 **W58-UIFIX-A 就地订正（铁律 5）**：这 5 条原来紧跟在 `CollectionWindow.Create` 之后
            //   （旧行号 `:1008-1021`），而 T09（2026-10-17）起四页**懒建** —— `Build()` 只建第 0 页（Deck），
            //   Cards 页要**第一次** `tabButtons.Click(1)`（上面「切页」那一节）才 `Setup()`
            //   （`Shell/CollectionTabPage.cs:44-80`）⇒ 原来读到的 `Begins` 恒 0。
            //   判据 → `普查产出_1020/W58_诊断_界面五宿主.md` §五 C1。
            //   ⛔ 不要为了这条去在 `Create` 里强制建 Cards 页（那会推翻 T09 的首开性能修复）。
            //   ⚠️ 静态源码次序那 3 条仍留在原处（`A1834 …（静态源码次序）` 那一节，不依赖建页）。
            Section("A1834：文字重排探针的位置（运行时读数 —— 在 Cards 页懒建之后读）");
            {
                CheckTrue(cardsPageOpened,
                          "（前提·不静默）A1834：Cards 页被点开过（懒建那一次在上一节）—— "
                        + "没点开 ⇒ 下面 5 条**没跑**，不是绿");
                if (cardsPageOpened)
                {
                    CheckTrue(CollectionWindow.TextRelayoutProbe.Begins >= 1,
                              "★ A1834 · 探针**接上了**（本次建 Cards 页至少一次 `TextProbeBegin`）—— 删掉那句调用 ⇒ 本条红");
                    if (CollectionWindow.TextRelayoutProbe.Begins >= 1)
                    {
                        Check(CollectionWindow.TextRelayoutProbe.CardsAtBegin, 0,
                              "★ A1834 · 快照那一刻**一张卡格都还没建**（`CardsAtBegin` = 0）= 「变化之前」");
                        Check(CollectionWindow.TextRelayoutProbe.BeginsWithCards, 0,
                              "★ A1834 · **没有任何一次** begin 落在建卡之后（`BeginsWithCards` = 0）"
                            + "｜🧨 把 `BuildWatch.TextProbeBegin(...)` 挪回 `RebuildCardsCells(holder);` 之后 ⇒ 变 18 ⇒ 红");
                        Check(CollectionWindow.TextRelayoutProbe.Ends, CollectionWindow.TextRelayoutProbe.Begins,
                              "★ A1834 · before/after **成对**（每个 `TextProbeBegin` 都配了 `TextProbeEnd`）");
                        Check(CollectionWindow.TextRelayoutProbe.UnclosedBegins, 0,
                              "★ A1834 · 没有「上一个 begin 还没 end 就又 begin」");
                    }
                }
            }

            // ---------------- Cards 页：卡池网格（正本 §四；A3 实测）----------------
            Section("Cards 页：卡池网格（6 列 · 262.5×384 · **贴左但整体居中**）");
            win.tabButtons.Click(1);                       // 切到 Cards
            // 🔴 **2026-10-22（W64 修）：从 `Tabs` 递归找 `"Scroll View"` 会**先命中新模式页那一颗**
            //   （`Tabs ▸ Select Deck Tab ▸ Select Game Mode Deck Tab ▸ Scroll View` 排在 `CardsTab` 之前）
            //   —— 首轮实测中心差 **44.15px**（= 模式页视口中腰 662.05 − 卡池 617.90）。按**页**取（同下面
            //   卡背页 `FindChild(cpage, …)` / 异画页 `FindChild(spage, …)` 的写法，重名一律走路径/页根）。
            var cardsHolder = FindChild(win.PageRoot(1), "Scroll View");
            CheckAt(cardsHolder, 330.2f, 1919.9f, 155.9f, 1079.9f,
                    "`Scroll View`（卡池视口；⚠️ 与 Deck 页的视口**差 0.7/0.1**，两份实测都记着）");
            // 🔴 **2026-10-18（卡池排序与显隐 · 收口补丁）**：这条原来写 `CardsVisibleCount > 1000`。
            //   卡池 **1127 → 1005**（剔掉 122 张 `show=0`）之后那条硬阈值**只剩 5 张余量** —— 下次数据一变就假红。
            //   ⇒ 改成**两列现算**：期望值 = `Pool` 里 `showInCollectionOptions != 0` 的张数
            //   （收藏卡牌页的显示判据，判据 = `CardCollectionTab.__c___GetCollection_b__8_0.c:19-30`；
            //    与 `CollectionWindow.CardsState.ShowInCollectionOnly` 是同一条链）。⛔ 别再写成硬数字。
            int wantCardsShown = 0;
            foreach (var pc in CollectionWindow.CardsState.Pool) if (pc.ShowInCollectionOptions != 0) wantCardsShown++;
            Check(win.CardsVisibleCount, wantCardsShown,
                  $"卡池读到 {win.CardsVisibleCount} 张 = **两列现算**（`Pool` 里 `showInCollectionOptions != 0` 共 {wantCardsShown} 张）");
            CheckTrue(win.CardsCells.Count > 0, $"画出了 {win.CardsCells.Count} 格（视口外的不建 = `RectMask2D` 那套）");
            if (win.CardsCells.Count > 0)
            {
                var cc0 = win.CardsCells[0];
                CheckNear(MenuCheck.DesignPx(cc0).x, 468.8f, 1.5f,
                          "第 1 格中心 x = **468.8**（贴左但整体居中：330.2 + 7.35 + 262.5/2）");
                // 🔴 **2026-10-26 就地订正（铁律 5）**：这条的 `W11`（2026-10-18）口径**已被 2026-10-25 裁定推翻**。
                //   判据（prefab 现读，`menu_dump.py bundle_menus_assets_all "Collection Card" --depth 4`）：
                //   `CardUI/2DCard` 在 **350×512** 原型里是 **y 6.2 → 505.8**（上下各 6.2 ⇒ **整格里居中**），
                //   张数条 `/Counter` = y 461→511 **画在卡下缘之上**（原版长相，不是「卡不压条」）。
                //   ⇒ 卡体**位置框 = 整格**（`CardsCardRect(cell) == cell`）、按原版公式缩放后在整格里居中
                //     ⇒ `win.CardsCells` 存的那颗 `CardView` transform 中心 = **格心 347.9**。
                //   ⛔ 别再改回 328.764（`W11` 那版「格心 − 张数条/2」，已推翻；判据 → `Shell/CollectionWindow.cs` 的 `CardsCardRect` doc · `Editor/HandAssertE8.cs` §D）。
                CheckNear(MenuCheck.DesignPx(cc0).y, 347.9f, 1.5f,
                          "第 1 格**卡块**中心 y = **347.9**（= 格心；原版 `2DCard` 在整格里居中）");
            }
            // 🆕 **2026-10-24（`E6`）**：卡池小卡的**格子几何**逐条钉给原版
            //   （格/间距/列数/居中偏移 = 原版 RSR 序列化字段 + 反编译算法；卡体宽/高占比 = 原版 prefab
            //    `CardUI/2DCard` 的 313.91/350 与 499.70/512；卡底压条 33.62px = 原版长相）。
            //   🔴 **2026-10-26**：`E6` 原来那三条 `W11` 口径（345.728 / 不压条 / 实拍 81.7%）已按 2026-10-25 裁定就地订正（判据见 `Editor/PoolCardGridAssertE6.cs` 文件头）。
            //   🔴 顺带把 `A2` §Q6 那条错判据挡在门外：它引的 225×364.5 / spacing 20 是**卡组列表**的格
            //   （那两份 MB 的 `PrototypeCell` = `Collection Deck[ With Highlight]`），不是卡池。
            //   判据与逐条出处 → `Editor/PoolCardGridAssertE6.cs` 文件头 + `资料/普查产出_1024c/E6_收藏卡池格子几何.md`。
            PoolCardGridAssertE6.Run(win, _sink);
            if (win.CardsScroll != null && win.CardsScroll.MaxOffset > 0f)
            {
                win.CardsScroll.ScrollBy(win.CardsScroll.MaxOffset);
                int lastIdx = win.CardsVisibleCount - 1;
                var lc = FindChild(tabsRoot, "CollectionCard_" + lastIdx);
                CheckTrue(lc != null && MenuCheck.DesignPx(lc).y + 192f <= 1080.5f,
                          $"**滚到底 ⇒ 第 {lastIdx + 1} 张（最后一张）完整落进视口**");
                win.CardsScroll.ScrollBy(-win.CardsScroll.MaxOffset);
            }

            // ================================================================
            // 🔴🆕 **2026-10-18（卡池排序与天赋卡显隐）**：收藏卡牌页的两条原版判据
            //   判据（现读 `d:/2/tools/decomp_full/`）：
            //   · **显示** = `CardCollectionTab.__c___GetCollection_b__8_0.c:19-30` 的
            //     `Where(showInCollectionOptions != Hide(0))`（另有 `!IsHidden()` 与「非异画」两道 ——
            //     `IsHidden` 的配置在服务端、本地没有 ⇒ 不做；异画那一道我们卡池里只有本体条目 ⇒ 无对象）；
            //   · **排序** = `CardCollectionTab__GetCollection.c:69,84,99,114,129` 的 5 个稳定 `OrderBy`
            //     ⇒ 实际主序 **阵营 → 战将 → `inventoryOptions` → 费用 → 名**。
            //   本节全部**从两列原值现算**，⛔ 不盯我们自己的常量。
            Section("★ 收藏卡牌页：显示判据（`showInCollectionOptions`）+ 排序（阵营 → 战将 → inventory → 费用 → 名）");
            {
                var cs = CollectionWindow.CardsState;
                var vis = cs.VisibleCards();

                // ① `show == 0` 的卡数 = 0
                int show0 = 0; string firstShow0 = null;
                foreach (var c in vis)
                    if (c.ShowInCollectionOptions == 0) { show0++; if (firstShow0 == null) firstShow0 = c.Name; }
                Check(show0, 0, "★ ① 收藏卡牌页里 `showInCollectionOptions == 0` 的卡数 = **0**"
                              + $"（92 张天赋卡 + 30 张 `Z-NC` 效果卡共 122 张都不该出现"
                              + (firstShow0 == null ? "" : $"；反例 `{firstShow0}`") + "）");
                int wantShown = 0;
                foreach (var c in cs.Pool) if (c.ShowInCollectionOptions != 0) wantShown++;
                Check(vis.Count, wantShown, $"★ ① 卡数 = 两列现算（`show != 0` 共 {wantShown} 张）"
                                          + "｜🧨 把 `ShowInCollectionOnly` 关掉 ⇒ 回到全池张数、本条红");

                // ② 天赋卡数 = 0（真值 = `talent_cards.json` 的全部 `talents` id —— 96 宿主 / 92 张去重；
                //    它们 `show=0`，所以 ① 过了 ② 必然过，但 ② 是**按真值表逐 id 查**的独立读数）
                var ta = Resources.Load<TextAsset>(RuleEngine.TalentCardData.ResourcePath);
                CheckTrue(ta != null, "（前提）`Resources/talent_cards.json` 载到了（否则下面 ② 是空转）");
                if (ta != null)
                {
                    var dto = JsonUtility.FromJson<RuleEngine.TalentCardData.FileDto>(ta.text);
                    var tal = new HashSet<string>();
                    if (dto != null && dto.entries != null)
                        foreach (var e in dto.entries)
                            if (e != null && e.talents != null)
                                foreach (var t in e.talents) if (!string.IsNullOrEmpty(t)) tal.Add(t);
                    CheckTrue(tal.Count > 0, $"（前提）天赋真值表里读到 {tal.Count} 个去重天赋 id");
                    int talShown = 0; string firstTal = null;
                    foreach (var c in vis)
                        if (tal.Contains(c.Id)) { talShown++; if (firstTal == null) firstTal = c.Name; }
                    Check(talShown, 0, $"★ ② 收藏卡牌页里**天赋卡 = 0 张**（真值表 {tal.Count} 个去重 id 逐张查）"
                                     + (firstTal == null ? "" : $"；反例 `{firstTal}`"));
                }

                // ③ 排序：首卡 = 第一个阵营（`cardArmy` 10 = Ultramarines）的**战将**
                CheckTrue(vis.Count > 0, "（前提）收藏卡牌页非空");
                if (vis.Count > 0)
                {
                    Check(vis[0].Type, "hero", "★ ③ 排序首卡是**战将**（原版 `cardType != Hero` 那把键；"
                                              + "🧨 不排序 ⇒ 首卡是卡表原序第一张、不是战将）");
                    Check(vis[0].Faction, DeckEditorState.ArmyOrder[0],
                          "★ ③ ……而且属于**第一个阵营**（`cardArmy` 10 = Ultramarines，`DeckEditorState.ArmyOrder[0]`）");
                }

                // ③b `inventoryOptions` 那一层：同阵营、同「战将档」的每一段里必须**非减**
                //   （现读卡表这条的判别力 = 防御卡 `inv=5` 必须整段排在 `inv=0` 之后，
                //     🧨 去掉那一层 ⇒ 防御卡按费用混进 inv=0 那段 ⇒ 本条红）
                int invBreaks = 0, segStart = 0;
                for (int i = 1; i <= vis.Count; i++)
                {
                    bool brk = i == vis.Count || vis[i].Faction != vis[segStart].Faction
                            || (vis[i].Type == "hero") != (vis[segStart].Type == "hero");
                    if (!brk) continue;
                    int lastInv = int.MinValue;
                    for (int j = segStart; j < i; j++)
                    {
                        if (vis[j].InventoryOptions < lastInv) invBreaks++;
                        lastInv = vis[j].InventoryOptions;
                    }
                    segStart = i;
                }
                Check(invBreaks, 0, "★ ③b 「同阵营 + 同战将档」每一段里 `inventoryOptions` **非减**"
                                  + "（原版第 3 个 `OrderBy`，**只有收藏窗有这一层**）");

                // ④ 异画页 / 卡背页**不受**这条显示闸影响（它们各自有自己的数据源与状态对象）
                Check(CollectionWindow.StylesState.Pool.Count, CollectionWindow.AltArtCards.Length,
                      "（前提）异画那份池子 = 异画表那 7 张（不受卡牌显示闸影响）");
                Check(win.StyleVisibleCount, CollectionWindow.AltArtCards.Length,
                      $"★ ④ 异画页照旧显示那 {CollectionWindow.AltArtCards.Length} 张"
                    + "（🧨 把显示闸套到 `StylesState` 上 ⇒ 异画卡若 `show=0` 会被一起剔掉 ⇒ 本条红）");
                CheckTrue(!CollectionWindow.StylesState.ShowInCollectionOnly
                          && !CollectionWindow.StylesState.SortByInventoryOptions,
                          "★ ④ 异画页那份 `DeckEditorState` **没被**套上卡牌显示闸 / 收藏排序（各走各的）");
                CheckTrue(!CollectionWindow.CosmoState.ShowInCollectionOnly
                          && !CollectionWindow.CosmoState.SortByInventoryOptions,
                          "★ ④ 卡背页那份状态同样没被套（卡背本来就不走卡池）");
                // 🔴 **2026-10-18（收口补丁）**：这条原来也是硬阈值 `Pool.Count > 1000` —— 改成
                //   **卡表去重 id 现算**（卡池 1127→1005 后硬阈值余量只剩 5 张；而 `Pool` 是**未过滤**的本体，
                //   它的期望值应恒等于卡表的去重 id 数）。⛔ 别再写死数字。
                int wantPoolAll = 0;
                {
                    var seenPool = new HashSet<string>();
                    foreach (var pc in CardDatabase.Load())
                        if (pc != null && !string.IsNullOrEmpty(pc.Id) && seenPool.Add(pc.Id)) wantPoolAll++;
                }
                Check(cs.Pool.Count, wantPoolAll,
                      $"★ ④ 卡池本体**没被改小**（`Pool` = {cs.Pool.Count} 张 = 卡表去重 id 现算 —— "
                    + "过滤只发生在 `VisibleCards`，⛔ 不是从 `Pool` 里删卡）");
            }

            // ================================================================
            // 🆕 **2026-10-18（T09 / `A1628`）：滚动不再整屏重建** —— 池化的**行为**断言。
            //   原来 `CardsScroll.OnChanged` 的阈值只有 **0.01 px**（`MenuScroll.SetOffset`），而回调体
            //   `RebuildCardsCells` 首句把视口下**所有孩子销毁**、逐格重新 `CardView.Create`
            //   ⇒ 拖一下 = 整屏重建（≈2.2 s/格 → 一屏 18 张 ≈ 40 s）。
            //   判据与实测 → `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md`
            //   改坏法（灭自证）：把池命中支改回「`DestroySafe` + `CardView.Create`」⇒ ①② 当场红（18 张）。
            Section("T09：收藏窗滚动池化（滚一格不再整屏 `DestroySafe` + `CardView.Create`）");
            if (win.CardsScroll != null && win.CardsCells.Count > 0)
            {
                win.CardsScroll.SetOffset(0f);
                var beforeCell = win.CardsCells[0];
                int beforeCount = win.CardsCells.Count;
                // ① 小滚（100 < 一格高 384）⇒ 可见集合不变 ⇒ **一张都不该新建**
                win.CardsScroll.ScrollBy(100f);
                CheckTrue(win.CardsCellCreatedLastRefresh == 0,
                          $"★ 滚 100px（< 一格 384）⇒ 新建 `CardView` **{win.CardsCellCreatedLastRefresh}** 张（期望 **0**）"
                          + " —— 整屏重建那条旧路会在这里建 " + beforeCount + " 张 ⇒ 本条当场红");
                // ② 同一格还是**同一个实例**（池 = 搬实例；「重建后长得一样」不算）
                CheckTrue(beforeCell != null && win.CardsCells.Count > 0
                          && object.ReferenceEquals(win.CardsCells[0], beforeCell),
                          "★ …滚过之后第 1 格仍是**同一个 `CardView` 实例**（`object.ReferenceEquals`）"
                          + " —— 池化 = 搬实例，不是「销毁后重建出一张长得一样的」");
                // ③ 跨一整行（再 +400 > 384）⇒ **只补新露出的那一行**（≤ 6 张），⛔ 不是整屏 18
                win.CardsScroll.ScrollBy(400f);
                int created = win.CardsCellCreatedLastRefresh;
                CheckTrue(created > 0 && created <= CollectionWindow.CardsCols,
                          $"★ 再滚 400px（跨一行）⇒ 只新建 **{created}** 张（期望 1…{CollectionWindow.CardsCols} = "
                          + "只补新滚进来的那一行）—— 整屏重建会是 " + beforeCount + " 张");
                win.CardsScroll.SetOffset(0f);      // 还原（后面几段都按滚动量 0 量落位）
            }

            // ---------------- 🆕 2026-10-17（B10）：卡位**底下那条「张数」**（原版 `Collection Card/Content/Counter`）----------------
            // 判据逐条现读（原始出处写在 `CollectionWindow` 那组 `CardsCnt*` 常量上）：
            //   · 框：`m_AnchorMin = (0.2857143, 0.002)` · `m_AnchorMax = (0.7142857, 0.09966714)` · `pivot (0.5,0)` · `sd (0,0)`
            //     ⇒ 相对 262.5×384 的格 = **x 75…187.5（宽 112.5）· 距格底 0.768…38.272（高 37.504）**
            //   · 底图 `40K_main_deck_card counter`（工程里那张 PNG 实测 **116×36** · Simple · **`m_PreserveAspect = 1`**）
            //     ⇒ 内接进那个框后**按宽顶满 ⇒ 实绘 112.5×34.91**（高那一轴留白 2.59）
            //   · 文案：原版 `"x{0}"` 填**原始拥有数**；**我们印 `min(拥有, 卡组上限)`**（见 `CardsCounterText` 的注释）
            Section("Cards 页：卡位底下那条「张数」（原版 `Collection Card/Content/Counter`）");
            {
                // 量落位之前**先把滚动量归零**（上面两句 `ScrollBy` 之后不保证逐位回到 0；
                // 紧跟着的 A12 那一段也是先 `SetOffset(0)`）—— 归零会经 `OnChanged` 重建格子。
                float savedOff = win.CardsScroll != null ? win.CardsScroll.Offset : 0f;
                if (win.CardsScroll != null) win.CardsScroll.SetOffset(0f);
                int built = win.CardsCells.Count;      // 起手 = 18（3 行 × 6 列，`RectMask2D` 之外的不建）
                int i0 = -1;
                for (int i = 0; i < built; i++) if (win.CardsCellDef(i) != null) { i0 = i; break; }
                CheckTrue(i0 >= 0, $"（前提）视口里至少建成一格（实测 {built} 格）—— 否则这一段等于没验");
                var cell0 = CollectionWindow.CardsCellRect(i0 >= 0 ? i0 : 0);
                var cnt0 = FindChild(tabsRoot, "Counter " + (i0 >= 0 ? i0 : 0));
                CheckTrue(cnt0 != null, "★ 第 " + ((i0 >= 0 ? i0 : 0) + 1) + " 格底下那条「张数」**建出来了**"
                                        + "（原来一个 per-card 计数都没有）");
                if (cnt0 != null)
                {
                    CheckArt(cnt0, "40K_main_deck_card_counter",
                             "…底图 = 原版那条 `40K_main_deck_card counter`（`m_Type=0 Simple`）");
                    var q = cnt0.GetComponentInChildren<ImageQuad>();
                    CheckTrue(q != null, "…底图那个 `ImageQuad` 找得到（下面两条才有意义）");
                    if (q != null)
                    {
                        float bw = q.WorldW * 108f, bh = q.WorldH * 108f;
                        CheckNear(bw, 112.5f, 1.5f,
                                  "★ 底图**渲出来 112.5 宽** = 格宽 262.5 × (0.7142857 − 0.2857143)");
                        CheckNear(bh, 34.914f, 1.6f,
                                  "★ 底图**渲出来 34.91 高**（116×36 的图 + `m_PreserveAspect=1` 内接进 112.5×37.504）"
                                  + " —— **判别式**：把 `Rect(…, keepAspect: true)` 那个 `true` 抽掉"
                                  + " ⇒ 拉满 **37.50**（差 2.59 > 容差）⇒ 本条红");
                    }
                    // 框心：x = (75+187.5)/2 = 131.25（格左偏）；y = (345.728+383.232)/2 = 364.48（格顶偏下沿）
                    CheckAtPx(cnt0, cell0.x1 + 131.25f, cell0.y1 + 364.48f, 1.5f,
                              "★ 那条「张数」落在**格子下沿**（框心 = 格左 +131.25 · 格顶 +364.48；"
                              + "⛔ 不是格心 130.0/192.0 —— 摆错那两轴差几十 px）");
                    var t0 = FindChild(cnt0, "Text (TMP)");
                    CheckTrue(t0 != null, "…那行字是它的子件 `Text (TMP)`（原版 `/Counter/Text (TMP)`）");
                }

                // 每一格都按**它自己那张卡**的卡组上限比（**督军 1** / 传说 1 / 其余 2 —— 三档）——
                // ⚠️ 期望值**不取 `CardsCounterText`**（那会自证），这里按「卡型 + 稀有度」**独立写死三档**。
                // 🔴 **2026-10-17（F2 · 修 (α)：这条原来把「我们不建视口外那条」当成了违约）** ——
                //   `built` 是**建出来的格**（18 = 3 行 × 6 列），而视口 `155.9 → 1079.9` 只有 924 高
                //   ⇒ 第 3 行（i = 12…17）**格底 1307.9** 整行在视口外；那条「张数」的框长在**格底**
                //   （`CollectionWindow.CardsCounterRect`：距格底 0.768…38.272 ⇒ **1269.6…1307.1**）
                //   ⇒ **整条落在视口外** ⇒ `BuildCardsCounter` 里
                //   `var q = Rect(parent, CardsCounterSprite, bar, "Counter " + i, …); if (q == null) return;`
                //   —— `MenuDraw.Rect` 里 `if (!ClipRect(…)) return null;` 会**连节点一起不建**，
                //   而该函数自己的注释写着「② **整条落在视口外** …那是**正常路径**」。
                //   ⇒ 原来那条把「空的 6 条」当成违约（实测 6 条不符 = 正好第 3 行）。
                //   **现在的口径**：对**没有计数节点**的格子跳过，另立**真不变式** ——
                //   「缺的那些【恰好】是矩形落在视口外的那些」（那才是实现真正承诺的东西）。
                //   ⚠️ 原版是「建了但被 `RectMask2D` 遮住」——**两者语义不同**，⛔ 别用「把 `MenuDraw.Rect`
                //   的裁切去掉」来凑绿（那是另一条：视口外的不建 = 省下几十个 quad + 点击区也消失）。
                int bad = 0, lg = 0, nm = 0, skipped = 0, outOfView = 0, missingInView = 0;
                var vpCnt = CollectionWindow.CardsViewport;
                for (int i = 0; i < built; i++)
                {
                    var d = win.CardsCellDef(i);
                    if (d == null) continue;
                    // 这一格那条「张数」**当前**的画布矩形（滚动量 = 0 ⇒ `Shift` 是恒等；判据同
                    // `RebuildCardsCells` 里 `var r = CardsScroll.Shift(content);` 那一跳）。
                    var cellR = win.CardsScroll != null
                        ? win.CardsScroll.Shift(CollectionWindow.CardsCellRect(i))
                        : CollectionWindow.CardsCellRect(i);
                    PxRect visCnt;
                    bool barInView = MenuDraw.ClipRect(CollectionWindow.CardsCounterRect(cellR), vpCnt, out visCnt);
                    var node = FindChild(tabsRoot, "Counter " + i);
                    if (node == null)                       // 没建 ⇒ 只可能是「整条在视口外」（下面那条钉它）
                    {
                        skipped++;
                        if (barInView) missingInView++; else outOfView++;
                        continue;
                    }
                    // 🔴 **2026-10-17（A934）：期望值也要带【卡型】那一档** —— 原版 `GetMaxCopiesInDeck` 把
                    //   `if (cardType == 10) return 1;` 排在**稀有度判断之前**
                    //   （`d:/2/tools/decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`）
                    //   ⇒ **非传说督军也是 `x1`**（我们池子里 28 张：epic 15 / rare 13）。
                    //   ⚠️ 期望值仍然**不取 `CardsCounterText`**（那会自证）—— 按「督军 / 传说 / 其余」**三档**独立写死。
                    bool isWl = d.Type == "hero";
                    string want = (isWl || d.Rarity == "legendary") ? "x1" : "x2";
                    string got = TextOf(FindChild(node, "Text (TMP)"));
                    if (got != want)
                    {
                        bad++;
                        if (bad <= 3)
                            CheckText(got, want, $"第 {i + 1} 格（{d.Rarity}{(isWl ? " · 督军" : "")}）那条「张数」");
                    }
                    if (want == "x1") lg++; else nm++;
                }
                Check(missingInView, 0, $"「张数」**没建出来**的那些格，**一格都不是**落在视口**里**的"
                              + $"（实测视口内缺 {missingInView} 格 —— ⛔ 那不是裁切，是真缺口）");
                Check(skipped, outOfView, $"…而且缺的那 {skipped} 条「张数」与「框落在视口外」的那些格"
                              + $"**一一对应**（实测落在视口外 {outOfView} 格）—— 判据 = `BuildCardsCounter` 里"
                              + "那句 `if (q == null) return;`（整条在视口外 ⇒ 连节点都不建，那是正常路径）");
                CheckTrue(outOfView >= 1, $"…这条**不是空转**：本夹具真有 {outOfView} 格在视口外（第 3 行整行）"
                              + " —— 少了它上面那条等于没验");
                Check(bad, 0, $"（共 {built} 格，其中 {skipped} 格那条在本滚动量下**不建**）已建的 {lg + nm} 格里"
                              + $"**每一条**「张数」都 = 它那张卡的卡组上限（**督军/传说 `x1`** / 其余 `x2`；"
                              + $"实测 {bad} 条不符）—— 覆盖 上限 1 的 {lg} 格 / 上限 2 的 {nm} 格");
                if (i0 >= 0)
                {
                    var d0 = win.CardsCellDef(i0);
                    int own0 = CardProgress.Owned(d0.Id, d0.Rarity);
                    CheckTrue(TextOf(FindChild(cnt0, "Text (TMP)")) != "x" + own0,
                              "★ **判别式**：印的**不是原始拥有数**（`CardProgress.Owned` 是「给足」口径 = " + own0
                              + " ⇒ 照原版那条式子会印 `x" + own0 + "`；谁把 `Mathf.Min` 删掉「按公式复原」⇒ 本条红）");
                }

                // ================================================================
                //  🔴 **2026-10-17（A934）判别式**：**非传说督军**那条「张数」印 `x1`，而**同稀有度的非督军**仍是 `x2`。
                //  判据 = 原版 `GetMaxCopiesInDeck` 的 `if (cardType == 10) return 1;` **排在稀有度判断之前**
                //  （`d:/2/tools/decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`）
                //  ⇒ 我们池子里 **28 位督军不是传说**（epic 15 / rare 13），只看稀有度会把他们印成 `x2`（A934 改前正是）。
                //  ⚠️ **两条必须【同时】成立**才叫改对：只断督军 `x1` ⇒ 「一律印 x1」那种写法也能过；
                //     只断非督军 `x2` ⇒ 退回按稀有度也能过。两张卡**稀有度相同** ⇒ 这才挡得住「按稀有度一刀切」。
                //  ⛔ 两处期望值都是**字面量**，不取 `CardsCounterText`（那会自证）。
                {
                    // 卡池快照取一次（`CardsCellDef(i)` 每次都会重建整张列表 ⇒ 逐张取会白跑 1000+ 次）
                    var visAll = CollectionWindow.CardsState.VisibleCards();
                    // 挑一对**同稀有度**的「非传说督军 / 非督军」，**优先挑同一行同一屏里的那两张**
                    //   —— 那样滚一行就能两条都落进视口，屏幕口径也能各量一次。
                    int kHero = -1, kNorm = -1;
                    for (int i = 0; i < visAll.Count && kHero < 0; i++)
                    {
                        var d = visAll[i];
                        if (d.Type != "hero" || d.Rarity == "legendary") continue;
                        for (int j = 0; j < visAll.Count; j++)
                        {
                            var e = visAll[j];
                            if (e.Type == "hero" || e.Rarity != d.Rarity) continue;
                            if (j / CollectionWindow.CardsCols != i / CollectionWindow.CardsCols) continue;   // 同一行
                            kHero = i; kNorm = j; break;
                        }
                    }
                    string pairRar = kHero >= 0 ? visAll[kHero].Rarity : null;
                    CheckTrue(kHero >= 0 && kNorm >= 0,
                              "（前提）卡池里找得到**同一行、同稀有度**的「非传说督军」与「非督军」各一张"
                            + $"（督军下标 {kHero} · 非督军下标 {kNorm}）—— 少了它，下面四条等于没验");
                    // ① 函数口径（与滚到哪儿无关）：成对断，两条一起才挡得住「一刀切」
                    if (kHero >= 0)
                        CheckText(CollectionWindow.CardsCounterText(visAll[kHero]), "x1",
                                  $"★ **判别式 · 督军恒 `x1`**：非传说督军 {visAll[kHero].Name}"
                                + $"（{pairRar} · 第 {kHero + 1} 张）⇒ `x1`"
                                + "（🧨 改坏法：`CardsCounterText` 退回 `DeckCap(def.Rarity)` ⇒ 本条回 `x2`）");
                    if (kNorm >= 0)
                        CheckText(CollectionWindow.CardsCounterText(visAll[kNorm]), "x2",
                                  $"★ **判别式 · 同稀有度的非督军仍是 `x2`**：{visAll[kNorm].Name}"
                                + $"（同为 {pairRar} · 第 {kNorm + 1} 张）⇒ `x2`"
                                + " —— 与上一条**成对**：谁把式子写成「按稀有度一刀切」，这两条必有一条红");
                    // ② 屏幕上真印出来的字**也**量一次（A934 说的就是「印 x2」）
                    var csWl = win.CardsScroll;
                    if (kHero >= 0 && kNorm >= 0 && csWl != null)
                    {
                        float offBefore = csWl.Offset;
                        // 把督军那一格顶到视口上沿 ⇒ 整行落进视口（那条「张数」长在格底 ⇒ 一定建得出来；
                        // 视口高 924 > 2×384 ⇒ 这一行与下一行都是整行可见的）
                        csWl.SetOffset(CollectionWindow.CardsCellRect(kHero).y1 - CollectionWindow.CardsViewport.y1);
                        var nH = FindChild(tabsRoot, "Counter " + kHero);
                        var nN = FindChild(tabsRoot, "Counter " + kNorm);
                        CheckTrue(nH != null && nN != null,
                                  $"…滚到第 {kHero + 1} 格那一行后，**同一行的两条**「张数」都建出来了"
                                + "（⭐那条与跟它成对的那条各一颗）—— 下两条才有意义");
                        if (nH != null)
                            CheckText(TextOf(FindChild(nH, "Text (TMP)")), "x1",
                                      $"★ 非传说督军那一格**印出来的就是 `x1`**（`Counter {kHero}` 的 `Text (TMP)`）");
                        if (nN != null)
                            CheckText(TextOf(FindChild(nN, "Text (TMP)")), "x2",
                                      $"★ …而**同一行里**同为 {pairRar} 的非督军那一格仍是 `x2`（`Counter {kNorm}`）");
                        csWl.SetOffset(offBefore);      // 还原（紧跟着那句 `SetOffset(savedOff)` 再兜一次）
                    }
                }
                if (win.CardsScroll != null) win.CardsScroll.SetOffset(savedOff);   // 还原滚动量
            }

            // ============================================================ 🆕 2026-10-25（`C` 域 · 迁移第二步）
            //   卡牌拥有数的**正本 = `Inventories.Cards`**，落盘 = `InventoryStore`（`WarpforgeInventory/cards.json`）。
            //   🧨 改坏法：把 `AddOwned` 里那句 `InventoryStore.Save` 删掉 ⇒ 下面「重读之后还在」当场红。
            //   ✅ 灭自证：重读前**先清背包** ⇒ 想读到 +1 之后的值，唯一的路就是盘上那份。
            Section("卡牌拥有数落盘（`WarpforgeInventory/cards.json`）：写 → 清背包 → 重读 → 还在");
            {
                string prevDir2 = InventoryStore.OverrideDir;
                InventoryStore.OverrideDir = "d:/4/_tmp_view/cards_inv_selftest";
                InventoryStore.Delete(CardProgress.InventoryName);
                CardDef probe = null;
                foreach (var c in CollectionWindow.CardsState.Pool) { if (c != null && !string.IsNullOrEmpty(c.Id)) { probe = c; break; } }
                CheckTrue(probe != null, "（前提）卡池里至少有一张卡可以当探针");
                if (probe != null)
                {
                    int before2 = CardProgress.Owned(probe.Id, probe.Rarity);
                    CardProgress.AddOwned(probe.Id, probe.Rarity, 1);
                    Check(CardProgress.Owned(probe.Id, probe.Rarity), before2 + 1, "（前提）`AddOwned(+1)` ⇒ 拥有数 +1");
                    CheckTrue(InventoryStore.LastStorePath != null
                              && System.IO.File.Exists(InventoryStore.StorePath(CardProgress.InventoryName)),
                              "★ `AddOwned` **真的写盘**了（`InventoryStore.LastStorePath` 非 null + 文件在）");
                    Inventories.Cards.ClearForTest();          // 灭自证：先清内存
                    // 🔴 **2026-10-26（C1）**：**播种在前、读盘在后** —— 与生产 `CollectionWindow.CardsState` 同一条顺序（本次一并订正）。`RestoreAmounts` 的契约是「**已在的改、不在的跳过**」⇒ 空背包读盘 = **什么都不读**（原来那句「先读盘再播种」正是拥有数**重启清零**的根因）；`SeedCardsAllUnlocked` 幂等（只补缺条目）⇒ 先播种不会把盘上的值盖掉。
                    Inventories.SeedCardsAllUnlocked(CollectionWindow.CardsState.Pool, c => CardProgress.Owned(c.Id, c.Rarity));
                    InventoryStore.Load(CardProgress.InventoryName, Inventories.Cards);
                    var back = Inventories.Cards.Get(probe);
                    CheckTrue(back != null && back.Amount == before2 + 1,
                              $"★ **重读之后拥有数还在**（{before2 + 1}）—— = 重启不清零");
                    // 收尾：回滚 + 删档 + 关闸
                    CardProgress.AddOwned(probe.Id, probe.Rarity, -1);
                    InventoryStore.Delete(CardProgress.InventoryName);
                    InventoryStore.OverrideDir = prevDir2;
                    Inventories.Cards.ClearForTest();
                    Inventories.SeedCardsAllUnlocked(CollectionWindow.CardsState.Pool,
                        c => CardProgress.Owned(c.Id, c.Rarity));
                    Check(CardProgress.Owned(probe.Id, probe.Rarity), before2, "（收尾：拥有数推回原值）");
                }
            }

            // ============================================================ 🆕 2026-10-25（`C` 域 · 迁移第一步）
            //   收藏的拥有数现在**两套并存**：旧的 `CardProgress.Owned`（`Shell/CardDetailPopup.cs`，自带存档）
            //   与新的背包 `Inventories.Cards`（框架层）。**这一步先让两套读口逐值相等**，
            //   全绿之后才删旧写口（见 `报告/C_CardProgress归并_第1轮.md`）。
            //   🧨 改坏法：把播种口径从 `CardProgress.Owned` 改回默认的 `CopyLimit` ⇒ 下面第一条当场红。
            Section("拥有数：`CardProgress.Owned` 与背包 `Inventories.Cards` **逐值相等**（迁移第一步）");
            {
                var poolAll = CollectionWindow.CardsState.Pool;
                int nCard = 0, nBad = 0; string firstBad = null;
                foreach (var d in poolAll)
                {
                    if (d == null || string.IsNullOrEmpty(d.Id)) continue;
                    var info = Inventories.Cards.Get(d);
                    if (info == null) continue;      // 播种在 `CardsState` 的 getter 里，这里不该缺
                    nCard++;
                    int oldV = CardProgress.Owned(d.Id, d.Rarity);
                    if (info.Amount != oldV)
                    {
                        nBad++;
                        if (firstBad == null) firstBad = d.Id + "：背包 " + info.Amount + " ≠ CardProgress " + oldV;
                    }
                }
                CheckTrue(nCard > 0, "（前提）背包里至少播种了一张卡（播种点 = `CollectionWindow.CardsState` 的 getter）");
                Check(nBad, 0, $"★ 两套拥有数**逐值相等**（核了 {nCard} 张）"
                             + (nBad == 0 ? "" : "｜第一位不一致：" + firstBad));
            }

            // ============================================================ 🆕 2026-10-25（`C` 域 · 调度台裁定）
            //   卡体缩放 = **原版公式**（`CollectionCard__OnRectTransformDimensionsChange`：
            //   `cardUI.localScale = sizeDelta.y × initialCardScale(150) / initialHeight(512)`）
            //   ⇒ 384 × 150 / 512 ÷ 108 = **1.0417**；prefab 实测 `CardUI/2DCard` = 313.91×**499.70**（350×512 原型）
            //   ⇒ 高 **97.6%**、宽 **89.7%**（与公式逐位吻合）。
            //   ⚠️ **重叠是原版长相**：卡体 `y 6.2→505.8`、张数条 `y 461→511`（同一 512 帧）⇒ 叠 44.8px（折 384 = 33.6px）。
            //   🧨 改坏法：退回 `(格高−张数条高)/…` ⇒ ① 变 90% ⇒ 红；把缩放的分子换成 `(384−37.5)` ⇒ 同上。
            Section("Cards 页：卡体缩放 = 原版公式 + 张数条与 prefab 逐值（`W11` 裁定后）");
            {
                CardView cv0 = null;
                for (int ci = 0; ci < win.CardsCells.Count && cv0 == null; ci++)
                    if (win.CardsCells[ci] != null) cv0 = win.CardsCells[ci].GetComponent<CardView>();
                CheckTrue(cv0 != null, "（前提）卡池里至少建出一张 `CardView`");
                if (cv0 != null)
                {
                    float wantScale = CollectionWindow.CardsCellH * 150f / 512f / 108f;
                    CheckNear(cv0.BaseScale, wantScale, 1e-3f,
                              $"★ 卡体缩放 = **原版公式** `384×150/512 ÷ 108` = **{wantScale:F4}**"
                            + "（⛔ 不是 `(格高−张数条高)/…` 那一版 = 0.9609）");
                    float cardH = CardView.Height * cv0.BaseScale * 108f;
                    CheckNear(cardH / CollectionWindow.CardsCellH, 499.70f / 512f, 5e-3f,
                              $"★ 卡体高 ÷ 格高 = **97.6%**（prefab `2DCard` 499.70 / 512；实测 {cardH:F1}px）"
                            + " —— **断言的是「等于公式值」，⛔ 不是「不重叠」**（重叠 33.6px 是原版长相）");
                }
                // 张数条：prefab `Content/Counter` 的 `Image.m_Color = (1,1,1,1)`、画在卡**之上**
                {
                    var bandNode = FindChild(win.PageRoot(1), "Counter 0");
                    var bandQuad = bandNode != null ? bandNode.GetComponentInChildren<ImageQuad>(true) : null;
                    CheckTrue(bandQuad != null, "（前提）`Counter 0` 那颗 quad 找得到");
                    if (bandQuad != null)
                    {
                        var t = bandQuad.Tint;
                        CheckTrue(Mathf.Abs(t.r - 1f) < 0.01f && Mathf.Abs(t.g - 1f) < 0.01f
                                  && Mathf.Abs(t.b - 1f) < 0.01f && Mathf.Abs(t.a - 1f) < 0.01f,
                                  $"★ 张数条 tint = **白 (1,1,1,1)**（prefab `Image.m_Color` 逐值；实测 ({t.r},{t.g},{t.b},{t.a})）");
                        CheckTrue(bandQuad.RenderQueue >= CollectionWindow.QPageRow,
                                  $"★ 张数条渲染队列 {bandQuad.RenderQueue} ≥ `QPageRow`({CollectionWindow.QPageRow}) ⇒ **画在卡之上**"
                                + "｜🔴 **判据（2026-10-25 补查）**：原版卡体**不是** SpriteRenderer —— `Card2DController` 的字段全是 "
                                + "**`UnityEngine.UI.Image`**（`frame` / `frameHighlightAndShadow` / `cardBackImage` / `cardBackShadow`）"
                                + "⇒ 与 `Counter` 同一张 uGUI canvas ⇒ 先后由**兄弟序 + 材质队列**定；prefab 兄弟序 = "
                                + "`Counter`(0) → `CardUI`(1) ⇒ **原版是卡体盖在条上**。"
                                + "✅ **2026-10-25 已验证：我们这条 = 视觉等价，不是偏离** —— "
                                + "采样卡框贴图（`Resources/Art/cards/frame_*.png`，1024²）在**底部 10%**（512 帧的 y 461..511）的 alpha："
                                + "**不透明率 26~35%（平均 α 0.27~0.35）= 卡框下边框本身是实心的**（对照：卡面中部只有 5~9%）"
                                + "⇒ **若原版真是「卡体盖在条上」，`xN` 会被那条实心边框盖掉、根本看不见**；"
                                + "而实拍里 `xN` 是看得见的 ⇒ **原版的有效先后必然是「条在卡之上」**（真实机制可能是卡体那些 Image 的**材质队列更高**，"
                                + "不是兄弟序）⇒ **我们这条队列与原版的可见结果一致**");
                    }
                }
            }

            // 万能卡计数条的 `Army Icon`（原版 `WIldcard Display` 的第三个孩子，A3 §5·3）
            {
                var ai = FindChild(win.PageRoot(1), "Army Icon");
                CheckTrue(ai != null, "★ 计数条左边那颗 `Army Icon` 建出来了（原来**没有**它 —— 那正是「按阵营」那一半）");
                if (ai != null)
                {
                    CheckAt(ai, 1470f, 1550f, 70.94f, 155.94f, "`Army Icon` 落在 **1470,70.94 → 1550,155.94**（80×85）");
                    CheckArt(ai, "40k_DeckSelection_icon_FactionUM",
                             "…喂的是 **Ultramarines** 那一面旗 —— 判据 = `CardCollectionTab__Setup.c:44-48` 在"
                             + " `Setup()` 里就 `WildcardDisplay.Initialize(wildcards, 10)`（`10` = `CardArmy.Ultramarines`；"
                             + "`WildcardDisplay__Initialize.c:9-10` 只见 `currentArmy` 0 ≠ 10 ⇒ 一定跑下去换图）"
                             + " ⇒ **不是 prefab 那个 `…FactionBlackLegion` 死值**（2026-10-25 K3 订正）");
                }
            }

            // ============================================================ 🆕 2026-10-25（`C` 收藏域 · K3 P2-⑦ 落地后的断言）
            //   原版：`CardCollectionDisplay.OnListDrag`（列表滚动）→ `CheckFocusedArmy()` →
            //     `RectExtensions.OverlapsAny(Reference Card Pointer 矩形, 在屏的格们, out 命中格)` →
            //     `WildcardDisplay.Initialize(命中格的阵营)`；命中序 = **按枚举序第一个交叉**（两份反编译一致）。
            //   参考矩形（prefab 实读，`menu_dump.py … "CardsTab" --depth 8`）= **402.3,236.4 → 596.1,281.4**。
            //   ⚠️ 本段**自己按几何算期望值**（格矩形 + 滚动量 + 参考矩形常量），**不调** `CheckFocusedArmy`
            //     —— 那是自证；这里算出来再和窗口状态比。
            //   🧨 改坏法 ① 把徽记换回「指针悬停触发」⇒ 本段两处读数都会停在初值 Ultramarines（滚到底那格若也是
            //      Ultramarines 才可能假绿 ⇒ 第二处特意滚到**末尾**，末格阵营与首格不同）；② 命中序改成「取最后一格」⇒ 边界那一格对不上。
            Section("Cards 页：万能卡徽记 = 列表**滚动**后按固定参考矩形命中（原版 CheckFocusedArmy）");
            {
                int HitIdx(CollectionWindow w)
                {
                    if (w == null || w.CardsScroll == null) return -1;
                    var lc = CollectionWindow.CardsState.VisibleCards();
                    for (int i = 0; i < lc.Count; i++)
                    {
                        var r = w.CardsScroll.Shift(CollectionWindow.CardsCellRect(i));
                        // 原版 `RectExtensions.Overlaps` 是**严格不等**的 AABB（贴边不算相交）
                        if (!(r.x1 < CollectionWindow.WcRefR) || !(CollectionWindow.WcRefL < r.x2)) continue;
                        if (!(r.y1 < CollectionWindow.WcRefB) || !(CollectionWindow.WcRefT < r.y2)) continue;
                        return i;
                    }
                    return -1;
                }

                float csOff0 = win.CardsScroll != null ? win.CardsScroll.Offset : -1f;
                int h0 = HitIdx(win);
                CheckTrue(h0 >= 0, "（前提）滚动到顶时，参考矩形至少压住一格（402.3,236.4→596.1,281.4 落在第一行）");
                if (h0 >= 0)
                {
                    var lc0 = CollectionWindow.CardsState.VisibleCards();
                    Check(win.WildcardArmyForTest, ArmyIcons.Id(lc0[h0].Faction),
                          $"★ 一开页徽记 = **参考矩形命中的那一格**的阵营（命中第 {h0} 格「{lc0[h0].Name}」⇒ "
                        + $"army={ArmyIcons.Id(lc0[h0].Faction)}）；判据 = `CardCollectionDisplay__OnListDrag.c` → "
                        + "`CheckFocusedArmy`（**不是指针悬停**）");
                    CheckTrue(win.WildcardArmyForTest != 0,
                              "★ 徽记**不是 0/未设**（0 = 从来没算过 ⇒ 上面那条会比 `ArmyIcons.Id(null)` = 0 而假绿）");
                }

                // 滚到底再算一次：末行的阵营与首行不同 ⇒ 这条能抓住「徽记不跟滚动」
                if (win.CardsScroll != null
                    && CollectionWindow.CardsState.VisibleCards().Count > CollectionWindow.CardsCols)
                {
                    win.CardsScroll.SetOffset(win.CardsScroll.MaxOffset);
                    int h1 = HitIdx(win);
                    CheckTrue(h1 >= 0, "（前提）滚到底之后参考矩形仍压住一格");
                    if (h1 >= 0)
                    {
                        var lc1 = CollectionWindow.CardsState.VisibleCards();
                        Check(win.WildcardArmyForTest, ArmyIcons.Id(lc1[h1].Faction),
                              $"★ 滚到底后徽记 = **滚完之后**参考矩形命中的那一格（命中第 {h1} 格「{lc1[h1].Name}」）"
                            + "｜🧨 徽记若停在悬停/初值 ⇒ 红");
                        var lc0 = CollectionWindow.CardsState.VisibleCards();
                        CheckTrue(h0 < 0 || lc0[h0].Faction != lc1[h1].Faction,
                                  "★ 这一滚的两格阵营**确实不同**（否则上一条分不清「跟滚动」与「一直没变」）");
                    }
                    win.CardsScroll.SetOffset(csOff0);       // 还原
                }
            }

            // ============================================================ 🆕 2026-10-19（第五十七会话 · 树形尾巴）
            //   Cards 页 `Header Filters` 之下补的**两层容器**。判据 = **原版 prefab 现读**
            //   （`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "CardsTab" --depth 7`，2026-10-19）：
            //     · `CardsTab ▸ Header Filters ▸ WIldcard Display` = **1470.0,70.9 → 1870.0,155.9**（400×85）
            //       —— 孩子序 = `Background` → `Counters` → `Army Icon`；
            //     · `WIldcard Display ▸ Counters` = **1555.0,91.4 → 1870.0,135.4**（315×44）。
            //   🔴 取法一律 `Transform.Find`（**只找直接子件**）—— 换回递归的 `FindChild` 就分不出
            //     「挂在 `WIldcard Display` 下」与「直接挂在页节点上」，而后者正是补容器**之前的形态**（弱断言）。
            //   🆕 **2026-10-19（续）**：原版 `Counters` 底下那**四组** `Common/Rare/Epic/Legendary`（每组
            //     `Icon` + `Counter`）已按原版建出 ⇒ 本段也逐组断父链 / 兄弟序 / rect（判据见段内注释）。
            //   🧨 改坏法：把 `Shell/CollectionWindow.cs` 的 `BuildCardsPage` 里那两颗 `Node(… , "WIldcard Display"/"Counters", …)`
            //     或四组 `Node(wcCounters, wcGroup[i], …)` 删掉、件挂回上一层 ⇒ 本段对应格全红。
            Section("Cards 页：`Header Filters ▸ WIldcard Display ▸ Counters` 两层容器（父链 + 直接子件 + 兄弟序）");
            {
                var cp1 = win.PageRoot(1);
                var hdr1 = cp1 != null ? cp1.Find("Header Filters") : null;
                CheckTrue(hdr1 != null, "（前提）Cards 页那颗页头 `Header Filters` 是页节点的直接孩子");
                var wd = hdr1 != null ? hdr1.Find("WIldcard Display") : null;
                CheckTrue(wd != null,
                          "★ `Header Filters ▸ WIldcard Display` 是**直接孩子**（原版 1470,70.9→1870,155.9 · 400×85）"
                        + "｜🧨 退回「四件直接建在页节点上」⇒ 本格红");
                if (wd != null)
                {
                    CheckAt(wd, 1470f, 1870f, 70.9f, 155.9f, "`WIldcard Display` 落在原版矩形（1470,70.9 → 1870,155.9）");
                    var wrt = wd as RectTransform;
                    CheckNear(wrt != null ? wrt.sizeDelta.x * 108f : -1f, 400f, 0.5f,
                              "`WIldcard Display` 宽 = 原版 **400px**");
                    CheckNear(wrt != null ? wrt.sizeDelta.y * 108f : -1f, 85f, 0.5f,
                              "`WIldcard Display` 高 = 原版 **85px**");
                    var wBg = wd.Find("Background");
                    var wCnt = wd.Find("Counters");
                    var wArmy = wd.Find("Army Icon");
                    CheckTrue(wBg != null && wCnt != null && wArmy != null,
                              "★ `WIldcard Display` 的三个**直接孩子**都挂在这一层（`Background` / `Counters` / `Army Icon`；"
                            + "底图名按原版 = **`Background`**，⛔ 不是我们以前的 `Wildcard Bg`）"
                            + "｜🧨 任一颗挂回页节点 ⇒ 本格红");
                    if (wBg != null && wCnt != null && wArmy != null)
                        CheckTrue(wBg.GetSiblingIndex() < wCnt.GetSiblingIndex()
                                  && wCnt.GetSiblingIndex() < wArmy.GetSiblingIndex(),
                                  "★ 兄弟序 = **Background → Counters → Army Icon**（原版 `WildcardDisplay.m_Children`；实测 "
                                + $"{wBg.GetSiblingIndex()}/{wCnt.GetSiblingIndex()}/{wArmy.GetSiblingIndex()}）");
                    if (wCnt != null)
                    {
                        CheckAt(wCnt, 1555f, 1870f, 91.4f, 135.4f,
                                "`Counters` 落在原版矩形（1555,91.4 → 1870,135.4 · 315×44）");
                        // 🆕 **2026-10-19（续 · 原版现读）**：`Counters ▸ {Common,Rare,Epic,Legendary} ▸ {Icon, Counter}`。
                        //   判据 = `menu_dump.py bundle_menus_assets_all "CardsTab" --depth 7 --no-layout` 现读：
                        //     Common 1565→1630 · Rare 1640→1705 · Epic 1715→1780 · Legendary 1790→1855（65×44，y 91.4→135.4）；
                        //     每组孩子 = `Icon`（x→x+30）· `Counter`（x+30→x+71，⚠️ 溢出组框 6px —— 原版如此）。
                        string[] grpNames = { "Common", "Rare", "Epic", "Legendary" };
                        for (int i = 0; i < 4; i++)
                        {
                            float gx = 1565f + 75f * i;
                            var g = wCnt.Find(grpNames[i]);
                            CheckTrue(g != null,
                                      $"★ `Counters ▸ {grpNames[i]}` 是**直接孩子**（原版四组之一；🧨 退回 8 颗平铺 ⇒ 本格红）");
                            if (g == null) continue;
                            CheckAt(g, gx, gx + 65f, 91.4f, 135.4f,
                                    $"`{grpNames[i]}` 落在原版矩形（{gx},91.4 → {gx + 65f},135.4 · 65×44）");
                            var grt = g as RectTransform;
                            CheckNear(grt != null ? grt.sizeDelta.x * 108f : -1f, 65f, 0.5f,
                                      $"`{grpNames[i]}` 宽 = 原版 **65px**");
                            CheckNear(grt != null ? grt.sizeDelta.y * 108f : -1f, 44f, 0.5f,
                                      $"`{grpNames[i]}` 高 = 原版 **44px**");
                            var ic = g.Find("Icon");
                            var ct = g.Find("Counter");
                            CheckTrue(ic != null && ct != null,
                                      $"★ `{grpNames[i]}` 的两个**直接孩子** = `Icon` / `Counter`（原版名字逐字）");
                            if (ic != null && ct != null)
                            {
                                CheckTrue(ic.GetSiblingIndex() < ct.GetSiblingIndex(),
                                          $"`{grpNames[i]}` 兄弟序 = Icon({ic.GetSiblingIndex()}) → "
                                        + $"Counter({ct.GetSiblingIndex()})（原版 `m_Children`）");
                                CheckAt(ic, gx, gx + 30f, 91.4f, 135.4f,
                                        $"`{grpNames[i]} ▸ Icon` 落在原版 30×44 框");
                                CheckAt(ct, gx + 30f, gx + 71f, 91.4f, 135.4f,
                                        $"`{grpNames[i]} ▸ Counter` 落在原版 41×44 框");
                            }
                        }
                        var ga = wCnt.Find("Common");
                        var gb = wCnt.Find("Rare");
                        var gc = wCnt.Find("Epic");
                        var gd = wCnt.Find("Legendary");
                        if (ga != null && gb != null && gc != null && gd != null)
                            CheckTrue(ga.GetSiblingIndex() < gb.GetSiblingIndex()
                                      && gb.GetSiblingIndex() < gc.GetSiblingIndex()
                                      && gc.GetSiblingIndex() < gd.GetSiblingIndex(),
                                      "★ `Counters` 四组兄弟序 = **Common → Rare → Epic → Legendary**（原版；实测 "
                                    + $"{ga.GetSiblingIndex()}/{gb.GetSiblingIndex()}/{gc.GetSiblingIndex()}/{gd.GetSiblingIndex()}）");
                    }
                }
            }

            // 🆕 2026-10-03（A12 收口）：Cards 页 —— 压在视口下边上的那一格，**命中区 == 露出来的那部分**
            //   （这一页的 `Hit` 走 `AddHit` ⇒ **有 quad** ⇒ 能直接量渲染矩形；判据同 `MenuDraw.ClipRect`）
            if (win.CardsScroll != null)
            {
                var cs = win.CardsScroll;
                float savedC = cs.Offset;
                cs.SetOffset(0f);
                int edgeIdx = -1;
                for (int i = 0; i < win.CardsVisibleCount; i++)
                {
                    var rr = CollectionWindow.CardsCellRect(i);
                    if (rr.y1 < CollectionWindow.CardsViewport.y2 - 0.5f
                        && rr.y2 > CollectionWindow.CardsViewport.y2 + 0.5f) { edgeIdx = i; break; }
                }
                CheckTrue(edgeIdx >= 0, "起手有一格**压在卡池视口下边上**（否则这一条等于没验）");
                if (edgeIdx >= 0)
                {
                    // 视口 330.2,155.9 → 1919.9,1079.9（本文件上面 `CheckAt(cardsHolder, …)` 已钉住）
                    var vp = CollectionWindow.CardsViewport;
                    var onScreen = cs.Shift(CollectionWindow.CardsCellRect(edgeIdx));
                    PxRect vis;
                    bool inView = MenuDraw.ClipRect(onScreen, vp, out vis);
                    var h = FindChild(tabsRoot, "CardHit_" + edgeIdx);
                    float x1, y1, x2, y2;
                    CheckTrue(h != null && RectOf(h, out x1, out y1, out x2, out y2),
                              $"第 {edgeIdx + 1} 格（压边那一格）的 `Hit` 建了、渲染矩形量得到");
                    if (h != null && RectOf(h, out x1, out y1, out x2, out y2))
                    {
                        CheckTrue(inView, "现算：这一格与视口**有交集**");
                        CheckNear(y2, vp.y2, 0.5f, "命中区的**下边缘 = 视口下边 1079.9**（被裁在那儿）");
                        CheckNear(y2 - y1, vis.H, 1.0f, $"命中区高 = **露出来的那部分**（{vis.H:F1}px）");
                        CheckTrue(y2 - y1 < CollectionWindow.CardsCellH - 1f,
                                  $"…而且确实**比整格矮**（{y2 - y1:F1} < {CollectionWindow.CardsCellH}）");
                        CheckNear(x2 - x1, CollectionWindow.CardsCellW, 1f, "…横向没裁 ⇒ 宽仍是格的宽 262.5");
                    }
                    // 对照：完全落在视口里的那一格 ⇒ 命中区是**整格**
                    var h0 = FindChild(tabsRoot, "CardHit_0");
                    float ax1, ay1, ax2, ay2;
                    if (h0 != null && RectOf(h0, out ax1, out ay1, out ax2, out ay2))
                    {
                        CheckNear(ax2 - ax1, CollectionWindow.CardsCellW, 1f, "对照：视口里的那一格 ⇒ 命中区**整格宽**");
                        CheckNear(ay2 - ay1, CollectionWindow.CardsCellH, 1f, "对照：…**整格高**（没被裁）");
                    }
                }
                cs.SetOffset(savedC);
            }

            // ═══════════ 🆕 2026-10-08（A181）：卡池的卡**真的被视口裁住了** ═══════════
            //  判据 = 原版 `CardsTab/Collection Display/Scroll View/Viewport` 上那颗 `RectMask2D`
            //  （`m_Softness = (0,0)` · `m_Padding = (0,0,0,0)` ⇒ **硬边、不内缩**；实读命令与四条 Viewport 的值
            //   见 `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`）。
            //  🔴 **为什么必须量渲染几何**：原来漏掉正是因为断言只量「节点在不在 / 矩形对不对」——
            //   卡是 `CardView` 的自建网格，节点位置全对而**画出来的层越界**（原来滚 192px 时第一排卡
            //   一路顶到 155.9−192 = −36.1，压在页头那条空带上）。这里逐顶点把网格换算成画布像素来量。
            //  **改坏法**：抽掉 `CollectionWindow.RebuildCardsCells` 里 `v.SetClipFromTree();` 那一句
            //  （或 `CardView.CropLayer`）⇒ 被切那一排的层会顶到视口上沿之上 ⇒ 这一组**立刻红**。
            //  🔴 **2026-10-13（A435 辛 · A774）就地订正（铁律 5）**：这一行原来写的是「抽掉
            //   `v.SetPose(…, CardsViewport)` 的第 4 个实参」—— 迁移后**那个实参已经不存在**了
            //   （裁切边界改走 `CardView.SetClipFromTree()` 沿父链解析那颗 `ViewportClip`）。
            if (win.CardsScroll != null)
            {
                var cs = win.CardsScroll;
                float savedC = cs.Offset;
                cs.SetOffset(192f);              // 上移 192 ⇒ 起手第一排（155.9..539.9）被视口上沿切掉 192px
                var vp = CollectionWindow.CardsViewport;
                int cutIdx = -1;
                for (int i = 0; i < CollectionWindow.CardsCols; i++)
                {
                    var rr = cs.Shift(CollectionWindow.CardsCellRect(i));
                    if (rr.y1 < vp.y1 - 0.5f && rr.y2 > vp.y1 + 8f) { cutIdx = i; break; }
                }
                CheckTrue(cutIdx >= 0, "（前提）滚 192px 后第一排有一张卡**压在视口上沿上**");
                var cutCard = cutIdx >= 0 ? FindChild(tabsRoot, "CollectionCard_" + cutIdx) : null;
                CheckTrue(cutCard != null, $"被上沿切到的那张卡（`CollectionCard_{cutIdx}`）建出来了");
                float cx1, cy1, cx2, cy2;
                int cverts;
                CheckTrue(RenderExtentPx(cutCard, out cx1, out cy1, out cx2, out cy2, out cverts) && cverts >= 20,
                          $"被切的那张卡**量得到渲染几何**（实测 {cverts} 个顶点；量到 0 就是这一条没验）");
                CheckTrue(cy1 >= vp.y1 - 0.5f && cy2 <= vp.y2 + 0.5f && cx1 >= vp.x1 - 0.5f && cx2 <= vp.x2 + 0.5f,
                          $"★ 卡上**没有一层画到视口外**（渲染范围 {cx1:F1},{cy1:F1} → {cx2:F1},{cy2:F1}；"
                          + $"视口 {vp.x1:F1},{vp.y1:F1} → {vp.x2:F1},{vp.y2:F1}）");
                CheckTrue(cy1 <= vp.y1 + 0.5f,
                          $"★ 而且它**真的被裁在视口上沿**（最高顶点 {cy1:F1} ≈ {vp.y1:F1}）"
                          + " —— 没有被裁的话它会一直顶到 " + $"{vp.y1 - 192f:F0}（= 页头那条空带上）");
                float tx1, ty1, tx2, ty2;
                int tverts;
                if (TextExtentPx(cutCard, out tx1, out ty1, out tx2, out ty2, out tverts) && tverts > 0)
                    CheckTrue(ty1 >= vp.y1 - 0.5f && ty2 <= vp.y2 + 0.5f && tx1 >= vp.x1 - 0.5f && tx2 <= vp.x2 + 0.5f,
                              $"★ 卡上的**文字也没有一个字画到视口外**（字的范围 {tx1:F1},{ty1:F1} → {tx2:F1},{ty2:F1}；"
                              + $"{tverts} 个顶点）");
                else
                    CheckTrue(false, "★ 卡上的文字**量不到网格** ⇒ 这一条没验（⛔ 别当通过；TMP 网格没建起来要先修那件事）");

                // ═══════ 🆕 2026-10-11（A250）：卡上那段字**真的走了「夹顶点」那条路** ═══════
                //  为什么单开这一条：上面那两条读的是 `textInfo`（TMP 的**模型**）与「启用的 `MeshRenderer` 网格」，
                //  它们都**分辨不出**「那一刀只改了模型、没上传到会被画出来的网格」—— 而 A250 把
                //  `CardView.ClipTextMesh` 收口成**转调** `MenuDraw.ClipTmpMesh`，那份公共件里**正好有**
                //  `MeshFilter.sharedMesh` 守卫（拿不到就不上传、数进 `TextClipUploadSkipped`）⇒ 这条契约要钉住。
                //  🔴 **用哪张卡**：卡面上那几段字（卡名 / 效果 / 兵种 / 数值）**全在卡高 58% 以下**
                //  （`UnitNameAt.y = 0.5821`、`RaceText` 0.8682 —— 单位「从卡顶算」，见 `CardView.PlaceAt`）
                //  ⇒ 滚 192px 那种「切上沿」（切掉上半张）**一段字都碰不到**。要验字就得用**压在下沿上**的那张
                //  （卡片下半截在视口外 ⇒ 卡上每一段字都整块越界）。期望值（**两重独立来源**）：
                //  ① `vp` = 原版 `Viewport` 的字面量；② 「整块越界 ⇒ 四角被夹到同一条边」是**几何事实**
                //  ⇒ 夹完应当有顶点**恰好贴在 `vp.y2`**（不夹的话它们停在卡自己那一带、整段在视口外）。
                //  **改坏法**：① 删掉 `CardView.ClipTextMesh` 里那句转调（文字不裁）⇒ 第 3 条红；
                //    ② 把公共件改成「只写 `textInfo`、不上传」（删 `UpdateVertexData`）⇒ 读上传网格时还是原始
                //      位置 ⇒ 第 3 条红（第 1、2 条读模型，看不出来 —— 这正是这条断言值钱的地方）；
                //    ③ 抽掉 `RebuildCardsCells` 里 `v.SetClipFromTree();` 那一句 ⇒ 整卡不裁 ⇒ 红
                //       （⚠️ **2026-10-13（A435 辛 · A774）就地订正**：原文指的「第 4 个实参」已随迁移消失）。
                //  🆕 **2026-10-11（F3）：本条原来是把 `tm.vertices`【整条数组】扫一遍 ⇒ 假红**
                //    —— 那份数组里夹着零面积的**非字形占位槽**（空格那种不可见的字占的 4 个零槽；
                //    见 `GlyphVertsOf` 里那两条判据）。现在按 `characterInfo[i].vertexIndex` **只数字形四角**，
                //    读的仍是**真上传的那一份**；上面三条改坏法逐条复核过、**都还成立**（不裁时字形整段在视口外）。
                {
                    // ⚠️ **本块自己把滚动拉回 0**（下面那句 `cs.SetOffset(0f)` 是同一件事 ⇒ 它成了空操作）：
                    //    切「下沿」这张卡只在**不滚**时才有（`CardsViewport` 924px = 2.4 行 ⇒ 第 3 行只露出
                    //    上面一截）；滚 192 时第 3 行露出 348px ⇒ 卡上那几段字**刚好又回到视口里**了，
                    //    那一条就成了「没验」（`qEdge` 恒 0）。
                    cs.SetOffset(0f);
                    int bIdx = -1;
                    for (int i = 0; i < CollectionWindow.CardsCols * 3; i++)
                    {
                        var rr = cs.Shift(CollectionWindow.CardsCellRect(i));
                        if (rr.y1 < vp.y2 - 8f && rr.y2 > vp.y2 + 0.5f) { bIdx = i; break; }
                    }
                    CheckTrue(bIdx >= 0, "（前提）起手就有一张卡**压在下沿上**（格子下半截在视口外）");
                    var botCard = bIdx >= 0 ? FindChild(tabsRoot, "CollectionCard_" + bIdx) : null;
                    CheckTrue(botCard != null, $"压在下沿的那张卡（`CollectionCard_{bIdx}`）建出来了");
                    float qy1 = float.MaxValue, qy2 = float.MinValue;
                    int qn = 0, qEdge = 0, qOut = 0;
                    if (botCard != null)
                        foreach (var tmp in botCard.GetComponentsInChildren<TMPro.TextMeshPro>(true))
                        {
                            if (tmp == null) continue;
                            var tmf = tmp.GetComponent<MeshFilter>();
                            var tm = tmf != null ? tmf.sharedMesh : null;   // ⛔ 不是 `tmp.textInfo`
                            if (tm == null) continue;
                            var tvs = tm.vertices;
                            if (tvs == null) continue;
                            //  🔴 **2026-10-11（F3）：只数「字形」的四角**（`characterInfo[i].vertexIndex`），
                            //     ⛔ 别再扫整条 `tvs` —— 那份数组里**还夹着非字形的退化占位槽**（零面积、画不出来）：
                            //     TMP 对**不可见的字**（空格 / 超出 `maxVisibleCharacters` 的字）把四角全写成
                            //     `Vector3.zero`（`TextMeshPro.cs:4536-4541`）**却照旧占 4 个槽**
                            //     （`:4550-4552 FillCharacterVertexBuffers(i)` 照跑），尾巴上还有一段零槽。
                            //     换算成像素后它们停在**这一段字自己的原点**上，而 `ClipTmpMesh` 只认 `isVisible`
                            //     的字 ⇒ **压根不夹它们** ⇒ 卡压在下沿时它们留在视口外，整条一扫就成了
                            //     「字画到视口外」（**当天实红：期望 [0] 实得 [12] = 3 槽 × 4 点，一个字形都没有**）。
                            //     ⚠️ 改法**不是**退回 `textInfo`：读的仍旧是**真上传的那一份**，只是按字取下标。
                            //     ⚠️ 详细判据（含「为什么不能整条扫」「为什么只收 `materialReferenceIndex == 0`」）
                            //     → 本文件 `GlyphVertsOf`。
                            //  🔴 **2026-10-20（`A989②`）· 已查清的【覆盖面缺口，不是产品缺陷】**：
                            //     本处只取**这颗 TMP 自己**的 `MeshFilter.sharedMesh`（= 0 号槽那份上传网格）
                            //     ⇒ `GlyphVertsOf` 会把 `<sprite name=…>` 那些字（**非 0 号槽**）跳过
                            //     ⇒ 这条断言盯的是「**不含图标**的那部分字**」有没有画到视口外，
                            //     **图标那枚字形没被它看着**。裁切本身是好的（`MenuDraw.ClipTmpMesh`
                            //     按每个字自己的 `materialReferenceIndex` 取槽，`TextMeshPro.UpdateVertexData`
                            //     把**每个槽**都上传到 `m_subTextObjects[i].mesh`）⇒ **画面不会出错**。
                            //     ⚠️ **不能**照 `Editor/CardFaceProbe.cs` 那条改成
                            //     `chr[i].materialReferenceIndex` 取 `textInfo.meshInfo[slot]` —— 那是读**模型**，
                            //     而本块的立身之本正是「读**真上传的那一份**」（上面三条改坏法里第②条
                            //     就是「只写 `textInfo`、不上传」）。要补覆盖面只能**另找那份上传网格**
                            //     （子件 `m_subTextObjects[slot]` 的那个 `MeshRenderer`）。判据不足 ⇒ 本轮只记录。
                            var gv = GlyphVertsOf(tmp, tvs.Length);
                            for (int i = 0; i < gv.Count; i++)
                            {
                                var p = LayoutSpace.ToPixel(tmp.transform.TransformPoint(tvs[gv[i]]));   // 甲式保留：顶点量法（W57 §1·4 第 4 条）
                                qn++;
                                qy1 = Mathf.Min(qy1, p.y); qy2 = Mathf.Max(qy2, p.y);
                                if (Mathf.Abs(p.y - vp.y1) <= 0.5f || Mathf.Abs(p.y - vp.y2) <= 0.5f) qEdge++;
                                if (p.x < vp.x1 - 0.5f || p.x > vp.x2 + 0.5f
                                    || p.y < vp.y1 - 0.5f || p.y > vp.y2 + 0.5f) qOut++;
                            }
                        }
                    CheckTrue(qn > 0, $"（前提）那张卡上的 TMP 量得到**上传的网格里的字形顶点**（{qn} 个角）"
                                      + " —— 量到 0 就是这一条没验（⛔ 别当通过）");
                    Check(qOut, 0, "★ 卡上没有一个**字的顶点**画到视口外（读 `MeshFilter.sharedMesh`："
                                  + "**真上传的那一份**、按 `characterInfo[i].vertexIndex` 只数字形，⛔ 不是 `textInfo`）");
                    CheckTrue(qEdge >= 4,
                              $"★ 而且**真有字被夹在视口边上**（贴边 `{vp.y1:F2}` / `{vp.y2:F2}` 的顶点 {qEdge} 个；"
                              + $"整卡字的 y 范围 {qy1:F1}..{qy2:F1}）—— 那几段字全在卡高 58% 以下、"
                              + "而这张卡只露出上面一小截 ⇒ **整块越界** ⇒ 夹完四角落在同一条边上；"
                              + "**不裁的话它们整段留在视口外**");
                }

                // **起手（不滚）也有**：第一排卡的 `SDF 影` 比卡本体大一圈（4.4281 vs 3.3313 卡单位、
                // 画出来 510.4px 见方）⇒ 影子**上沿本来就压在视口上沿之上 ≈62px**
                // （卡中心 347.9 + `ShadowY` 1.45 − 影子半高 255.2 ≈ 94.2 < 155.9）——
                // 这一截原来一直画在页头那条空带上。
                cs.SetOffset(0f);
                var top0 = FindChild(tabsRoot, "CollectionCard_0");
                float zx1, zy1, zx2, zy2;
                int zverts;
                CheckTrue(RenderExtentPx(top0, out zx1, out zy1, out zx2, out zy2, out zverts) && zverts >= 20,
                          $"起手第一张卡量得到渲染几何（{zverts} 个顶点）");
                CheckTrue(zy1 >= vp.y1 - 0.5f && zy1 <= vp.y1 + 2.5f,
                          $"★ 起手那张卡的**影子也被裁在视口上沿**（最高顶点 {zy1:F1}，视口上沿 {vp.y1:F1}；"
                          + "没裁的话是 ≈94.2 —— 那 62px 就画在页头上了）");

                // 对照：**没被上沿切到的那一排**（第 2 排）⇒ **纵向不许被裁**（只在越界时裁，不是一律压扁）
                var fullCard = FindChild(tabsRoot, "CollectionCard_" + CollectionWindow.CardsCols);
                float fx1, fy1, fx2, fy2;
                int fverts;
                CheckTrue(RenderExtentPx(fullCard, out fx1, out fy1, out fx2, out fy2, out fverts) && fverts >= 20,
                          $"对照：第 2 排那张卡量得到渲染几何（{fverts} 个顶点）");
                CheckTrue(fy1 > vp.y1 + 100f,
                          $"对照：没压边的那一排**纵向没被裁**（最高顶点 {fy1:F1}，比视口上沿 {vp.y1:F1} 低 "
                          + $"{fy1 - vp.y1:F0}px —— 被一起裁掉的话它会停在 {vp.y1:F1}）");
                CheckTrue(zy1 <= vp.y1 + 2.5f && fy1 - zy1 > 100f,
                          $"★ 两张卡的**最高顶点差 {fy1 - zy1:F0}px** ⇒ 裁切只落在压边的那一张上"
                          + $"（压边那张停在 {zy1:F1} = 视口上沿）");
                // ⚠️ 横向则**每张卡都被裁**：`SDF 影` 比卡宽一圈（510.4 vs 241），第一列那张的左沿会伸到
                //    213.6 < 视口左沿 330.2 ⇒ 那 116.6px 原来画在视口左边之外（抽屉收起时看得见）。
                CheckTrue(fx1 >= vp.x1 - 0.5f,
                          $"★ 第 2 排那张卡的**左沿也被裁在视口左沿上**（{fx1:F1} ≥ {vp.x1:F1}）"
                          + " —— 影子的横向溢出（到 213.6）在抽屉收起时本来是看得见的");
                cs.SetOffset(savedC);
            }

            // ═══════════ 🆕 2026-10-13（A435 辛 · 第 7 种载体的收口：A773 + A774 + A775）═══════════
            //  背景：A435 阶段 2 把裁切状态从「窗级三兄弟」搬到了**视口节点**上，但还有一族没迁 ——
            //  「**调用点自己把 clip 吃掉**」（不转调 `MenuDraw.*` 的解析口），普查实锤 20 组，
            //  剩下的两组就是本段（`资料/普查产出_1013/WA766_同形口普查.md` §二 A-1/A-2）。
            //  🔴 **为什么一条计数器都不能当判据**：这一族的口**根本不调 `ViewportClip.Resolve`** ⇒
            //   `NodeShadowedByParam` / `NodeResolutions` 对它**恒不变**（普查 §五·3 已证）——
            //   只能量行为：**渲出来的几何**（网格顶点 / TMP 顶点）与**真落点**。
            //  ⚠️ 本段所有期望值都不写死成「我们自己的常量」：要么用**节点自己那份状态**现算，
            //     要么**先量对照组**再拿它当边界（见 A773 那一段）—— 两态 + 改坏法都写在每条断言里。
            Section("第 7 种载体收口（A773 抽屉文字 · A774 卡跟随视口节点 · A775 `MenuScroll.Place`）");

            // ---------- A774：卡池的卡 —— 裁切边界**沿父链解析**（不再由宿主算一个常量矩形）----------
            //  判据 = `ViewportClip.Resolve` 的契约（形参 `null` ⇒ 沿父链找最近的 `ViewportClip`）。
            //  量的东西 = **真卡池那张卡**（`RebuildCardsCells` 建的那一批）的渲染几何：
            //  把节点 `padding` 的 Top 拨成 60、再按生产那两行的形状重摆一次 ⇒ 卡的**最高顶点**必须
            //  跟着下移 60（= 边界真从节点上取，而不是宿主那个常量矩形）。
            //  改坏法：① 调用点退回 `v.SetPose(…, CardsViewport)`（显式矩形赢）⇒ 最高顶点停在 155.9 ⇒ 红；
            //    ② 删掉调用点那句 `v.SetClipFromTree()` ⇒ 整卡不裁 ⇒ 最高顶点跑到 ≈94.2（影子那 62px）⇒ 红；
            //    ③ 把 `CardView.ApplyClip` 里 `_clipFromTree` 那一支删掉 ⇒ 没有「重裁时重新解析」⇒ 态② 红。
            if (win.CardsScroll != null)
            {
                var cs774 = win.CardsScroll;
                var vp774 = CollectionWindow.CardsViewport;
                var page774 = win.PageRoot(1);
                var holder774 = FindChild(page774, "Scroll View");
                var vpNode774 = holder774 != null ? holder774.Find("Viewport") : null;
                var vc774 = vpNode774 != null ? vpNode774.GetComponent<ViewportClip>() : null;
                var card774 = FindChild(page774, "CollectionCard_0");
                var cv774 = card774 != null ? card774.GetComponent<CardView>() : null;
                CheckTrue(vc774 != null && cv774 != null,
                          "（A774 前提）卡池那颗 `ViewportClip`（`Scroll View/Viewport`）与第 1 张卡的 `CardView` 都取得到");
                if (vc774 != null && cv774 != null)
                {
                    CheckTrue(cv774.Clipped, "（A774 前提）起手这张卡**是裁着的**（调用点那句 `SetClipFromTree()` 解析到了节点）");
                    float savedOff774 = cs774.Offset;
                    var savedPad774 = vc774.padding;
                    var savedPos774 = card774.localPosition;
                    cs774.SetOffset(0f);                                       // 与 A181 那一段同一个起手（第一排压在上沿）
                    var r774 = cs774.Shift(CollectionWindow.CardsCellRect(0));
                    float sc774 = CollectionWindow.CardsCellH / (CardView.Height * 108f);
                    // 🔴 **2026-10-24（F3）就地修（铁律 5）**：`Local(...)` 的第一个实参**必须是卡自己的父节点**，
                    //   以前这里写死 `vpNode774`（`Scroll View/Viewport`）—— **A14 之后不成立**：A14 给每格加了
                    //   一层容器 `Content <i>`（卡体 + 张数条都挂进去，判据 → `A14_界面第五批.md` §三），
                    //   卡不再是 `Viewport` 的直接孩子。拿 `Viewport` 当父去算局部坐标 = **把容器那一段偏移
                    //   当成了卡的偏移**：`Content 0` 的 `localPosition` ≈ (−656.3, −270)，同一个数被算两遍
                    //   ⇒ 卡被推到视口**左外侧 ≈1312 px** ⇒ `CardView.CropLayer` 的 ① 分支（整层在视口外）
                    //   把**每一层 `enabled = false`** ⇒ `RenderExtentPx` 量到 **0 个顶点**。
                    //   （本段那 4 条红就是这么来的：**不是裁切被 A14/A18 弄坏了**，而是探针没跟上结构。）
                    //   ⚠️ 别改回写死 `vpNode774`：本段断的是「**边界跟节点**」，而「卡挂在哪一级」是 A14 定下的
                    //   生产结构 —— 两件事各归各，探针只负责**按生产结构摆位**再量真几何。
                    var cellT774 = card774.parent != null ? card774.parent : vpNode774;
                    CheckTrue(card774.parent != null && cellT774.name.StartsWith("Content "),
                              $"（A774 前提）第 1 张卡挂在**格容器** `Content <i>` 里（实测「{cellT774.name}」）"
                            + " —— A14 起卡不再直挂 `Viewport`；父节点取错 ⇒ 卡会被摆到视口外、量到 0 个顶点（假红）");
                    var pos774 = MainMenuSubmenuWindow.Local(cellT774, r774.x1, r774.y1, r774.x2, r774.y2);
                    cv774.SetPose(pos774, 0f, sc774);                          // ← 生产那两行的形状（父 = 容器）

                    float a1, b1, c1, d1; int n1;
                    CheckTrue(RenderExtentPx(card774, out a1, out b1, out c1, out d1, out n1) && n1 >= 20,
                              $"（A774 前提）第 1 张卡量得到渲染几何（{n1} 个顶点；量到 0 这条就没验）");
                    CheckTrue(b1 >= vp774.y1 - 0.5f && b1 <= vp774.y1 + 2.5f,
                              $"★ A774 态①：`padding = 0` 时卡的裁切边界 = **节点框**（最高顶点 {b1:F1} ≈ 视口上沿 "
                              + $"{vp774.y1:F1}；⛔ 没裁的话影子会顶到 ≈94.2）");

                    vc774.padding = new Vector4(0f, 0f, 0f, 60f);              // Top 内缩 60（= 原版 `m_Padding.w`）
                    cv774.SetPose(pos774, 0f, sc774);                          // 同一次摆位 ⇒ 触发重裁
                    float a2, b2, c2, d2; int n2;
                    CheckTrue(RenderExtentPx(card774, out a2, out b2, out c2, out d2, out n2) && n2 >= 20,
                              $"★ A774 态②：**只改节点**的 `padding`、重摆一次 ⇒ 卡跟着下移（量得到几何 {n2} 个顶点）");
                    CheckTrue(b2 >= vp774.y1 + 59.5f,
                              $"★ …边界跟着 `padding` 走到 **{vp774.y1 + 60f:F1}**（最高顶点 {b2:F1}）—— "
                              + $"停在 {vp774.y1:F1} 就是「边界还在宿主那个常量矩形上」（= 第二状态源）");
                    CheckTrue(Mathf.Abs(b2 - b1) > 50f,
                              $"★ 两态的最高顶点差 **{Mathf.Abs(b2 - b1):F0}px** ⇒ 这条真能分出『跟节点』与『跟旧矩形』");

                    vc774.padding = savedPad774;                               // 还原节点
                    cv774.SetPose(pos774, 0f, sc774);
                    float a3, b3, c3, d3; int n3;
                    // 🔴 **2026-10-24（F3）**：原来这条写成 `if (RenderExtentPx(...)) CheckTrue(...)` ——
                    //   **量不到几何就整条静默跳过**（既不红也不出声）。2026-10-24 那次 4 红就是这样漏掉的：
                    //   态② 量到 0 个顶点，态③ 本该跟着红，结果**默默消失**（2211 条里少一条）。
                    //   ⇒ 把「量得到几何」并进**同一条**断言（不新增条数、也不降低强度）：
                    //   `RenderExtentPx` 的 out 形参在入口就赋了值（`float.MaxValue` / `n3 = 0`）⇒ `&&` 短路后
                    //   消息里读到的数不会未定义。⛔ 别改回 `if`。
                    CheckTrue(RenderExtentPx(card774, out a3, out b3, out c3, out d3, out n3) && n3 >= 20
                              && b3 >= vp774.y1 - 0.5f && b3 <= vp774.y1 + 2.5f,
                              $"★ A774 态③：还原 `padding` ⇒ 边界**回到**节点框 {vp774.y1:F1}（{b3:F1}；{n3} 个顶点；"
                              + "量到 0 个顶点也算红，⛔ 不许静默跳过）"
                              + " —— 读数随状态动、不是恒值（这一条挡「态② 那个 60 是碰巧量的」）");
                    cs774.SetOffset(savedOff774);
                    cv774.SetPose(savedPos774, 0f, sc774);                     // 还原（下一段还会重建整页）
                }
            }

            // ---------- A773：`ItemDrawer` 那个 `Price Display`（`TextCentered`）吃**视口节点** ----------
            //  病灶：`ItemDrawer.cs` 那句 `if (st.Clip.HasValue) MenuDraw.ClipText(lb, st.Clip.Value, …)`
            //  在**生产上恒假**（`ItemDrawerStyle.Clip` 缺省 `null`、三处派生写入已在甲块删掉）
            //  ⇒ 这两段字（`SetEphemeral` 的时长串 / `SetConverted` 的数量数字）**一个顶点都不裁**。
            //  本段**不碰任何窗**：自建探针（与 `Editor/RewardsScene.cs` 的 A302 探针同一个形状 —— 无父 `GameObject`），
            //  两态 = ① 探针上挂一颗 `ViewportClip` ② **不挂**（对照组）。
            //  🔴 **期望值不写死**：先量对照组的落点，再把节点框沿放在**它自己的中心** —— 这样
            //     「压边」这件事与 `Label.WorldW` 量出来是多少无关（⛔ 不拿我们自己的常量当尺子）。
            //  ⚠️ 前提与既有 px 断言同一条：批处理是 16:9（`VisibleWidth == DesignWidth`）。
            //  改坏法：把那句改回 `if (st.Clip.HasValue)` ⇒ 态① 的 `1234` 立刻和对照组一样越界 ⇒ 两条红。
            {
                var a773Box = new PxRect(1700f, 100f, 2600f, 1000f);           // 900×900 ⇒ 条高 90 ⇒ 字 ≈54px
                var a773St = ItemDrawerStyle.Default(10, 11, 12);              // ⚠️ `Clip` 保持缺省 `null`（= 生产那一档）
                a773St.ConvertedQuantity = 1234;                               // 画 `1234`（比单个数字宽，好压边）

                // 对照组：**不挂节点** ⇒ 这两段字一个顶点都不该被裁（= 迁移前/病灶那一态）
                var fx0 = new GameObject("A773 探针（无节点）").transform;
                ItemDrawer.SetConverted(fx0, a773Box, a773St);
                var drw0 = FindChild(fx0, ItemDrawer.NodeConvertedDrawer);
                // 🆕 **2026-10-17（`A1212` 块 11 #69）**：`Quantity` 的四格 —— 原版
                //   `… Drawer ▸ Content/Label/Quantity` = **`12 / 75 / 36 / 0`**（设计帧 712×1080）。
                //   本夹具的抽屉高 900 ⇒ 该族刻度 `k = box.H / DecorRefH = 900/1080 = 0.8333`
                //   （`ItemDrawer` 的 `ClippedText` 三格**乘 `k`** 是**同族已接的那一站**（`CardPresentation/Shell/ItemDrawer.cs`）的写法，
                //   文档写明「调用方按该族刻度换算」）⇒ 期望 = `12k / 75k / 36k`，⛔ 不是拿 12/75/36 直接比。
                //   ⚠️ `Army Name`（#67，同四格但折行 = 1）这一条**没断**：它要求 `fac != null`（阵营图取到才建），
                //      本夹具的样式没带阵营 ⇒ 如实登记为「本夹具不建这一颗」（⛔ 不写假绿）。
                {
                    float k = a773Box.H / ItemDrawer.DecorRefH;
                    var qtyLb = FindDeep(drw0, ItemDrawer.NodeQuantity);
                    CheckTrue(qtyLb != null, "（前提）`A1212` 块 11：`Converted Drawer ▸ … ▸ Quantity` 建出来了");
                    if (qtyLb != null)
                        CheckA1212(drw0, ItemDrawer.NodeQuantity, "… Drawer ▸ Content/Label/Quantity",
                                   12f * k, 75f * k, 36f * k, 0);
                }
                var own0 = FindChild(drw0, ItemDrawer.NodeAlreadyOwned);
                var num0 = FindChild(FindChild(drw0, ItemDrawer.NodePriceDisplay), ItemDrawer.NodePriceText);
                float ox1 = 0f, oy1 = 0f, ox2 = 0f, oy2 = 0f, nx1 = 0f, ny1 = 0f, nx2 = 0f, ny2 = 0f;
                int ov0 = 0, nv0 = 0;
                bool okOwn0 = own0 != null && TextExtentPx(own0, out ox1, out oy1, out ox2, out oy2, out ov0) && ov0 > 0;
                bool okNum0 = num0 != null && TextExtentPx(num0, out nx1, out ny1, out nx2, out ny2, out nv0) && nv0 > 0;
                CheckTrue(okNum0, "（A773 前提）对照组：`Converted Drawer/Price Display/text` 那段数字建出来了、顶点量得到");
                CheckTrue(okOwn0, "（A773 前提）对照组：`Converted Drawer/AlreadyOwned` 那段说明字也建出来了");

                if (okNum0 && okOwn0)
                {
                    float a773Edge = (nx1 + nx2) * 0.5f;                       // 框沿 = 数字那段字的**实际中心**
                    var a773Vp = new PxRect(0f, 300f, a773Edge, 1080f);         // 只切 x 的右沿（两条带都在 y 内）
                    var fx1s = new GameObject("A773 探针（有节点）").transform;
                    // 🔴 **2026-10-14（#55/#56 · α）**：抽屉必须建在**视口节点【之下】**。
                    //   原来 `SetConverted` 的实参是 `fx1s` ⇒ `Converted Drawer` 与 `Viewport` 成了**兄弟**；
                    //   而 `ViewportClip.Resolve` / `FindAbove` **只沿父链往上走** ⇒ 兄弟上那颗节点**一次都没命中**
                    //   （自检里那两态实得值**逐位相同** ＝ 裁切一次都没发生）。
                    //   ⇒ 拿 `Hang` 的**返回节点**当父。（`MenuDraw.Node` 按**绝对设计矩形**落位 ⇒ 抽屉的位置不变，
                    //   变的只有父链 —— 这正是这两条要断的东西。）
                    //   判据 → `资料/普查产出_1013/D1013_诊断_块2_Shell与Collection.md` §16/§17。
                    var a773Vc = ViewportClip.Hang(fx1s, "Viewport", a773Vp, Vector4.zero, Vector2Int.zero);
                    ItemDrawer.SetConverted(a773Vc.transform, a773Box, a773St);
                    var drw1 = FindChild(fx1s, ItemDrawer.NodeConvertedDrawer);
                    var own1 = FindChild(drw1, ItemDrawer.NodeAlreadyOwned);
                    var num1 = FindChild(FindChild(drw1, ItemDrawer.NodePriceDisplay), ItemDrawer.NodePriceText);
                    float px1 = 0f, py1 = 0f, px2 = 0f, py2 = 0f, qx1 = 0f, qy1 = 0f, qx2 = 0f, qy2 = 0f;
                    int pv1 = 0, qv1 = 0;
                    bool okOwn1 = own1 != null && TextExtentPx(own1, out qx1, out qy1, out qx2, out qy2, out qv1) && qv1 > 0;
                    bool okNum1 = num1 != null && TextExtentPx(num1, out px1, out py1, out px2, out py2, out pv1) && pv1 > 0;
                    CheckTrue(okNum1 && okOwn1, "（A773 前提）有节点那一态：两段字都建出来了（粗筛 `VisibleAbove` 与框有交集）");

                    if (okNum1 && okOwn1)
                    {
                        // 态②（对照组）：**没有节点 ⇒ 不裁** —— 两段字都必须越过框沿（否则态① 那两条是空话）
                        CheckTrue(nx2 > a773Edge + 8f,
                                  $"★ A773 态②（对照）：**不挂节点**时数字越过框沿 {nx2:F1} > {a773Edge:F1}（框沿 = 它自己的中心）"
                                  + " —— 这条是态① 那两条的**前提**：几何本来就压在外面");
                        CheckTrue(ox2 > a773Edge + 8f,
                                  $"★ A773 态②（对照）：`Already Owned` 那段同样越过框沿（{ox2:F1} > {a773Edge:F1}）"
                                  + " —— 两条路都压边，后面才谈得上「行为一致」");

                        // 态①（有节点）：两段字都必须**被夹在框沿上**（≥ 贴边 = 真被截过，不是碰巧没到）
                        CheckTrue(px2 <= a773Edge + 0.5f,
                                  $"★ A773 态①：挂了 `ViewportClip` ⇒ **数量数字被截在框内**（最右顶点 {px2:F1} ≤ {a773Edge:F1}；"
                                  + $"⛔ 病灶态下它 = {nx2:F1}，压在视口边上整段画出去）");
                        CheckTrue(px2 >= a773Edge - 0.5f,
                                  $"★ …而且**确实被夹在框沿上**（{px2:F1} ≥ {a773Edge - 0.5f:F1}）—— "
                                  + "少了这一条，一条「本来就落在框内」的短文字也能过（假绿）");
                        CheckTrue(qx2 <= a773Edge + 0.5f && qx2 >= a773Edge - 0.5f,
                                  $"★ A773 态①：同一格里 `Already Owned` 那条路（`ClippedText`，甲块已改）**也夹在同一条框沿**"
                                  + $"（{qx2:F1} ≈ {a773Edge:F1}）⇒ 同一个函数里两条路**行为一致**");
                    }
                    Object.DestroyImmediate(fx1s.gameObject);
                }
                Object.DestroyImmediate(fx0.gameObject);
            }

            // ---------- A775：`MenuScroll.Place` 的框来源 = `ClipNode`（与 `Intersects` 同一档）----------
            //  ⚠️ **全仓零调用点**（`grep '\.Place('` 在 `Assets/` 下零命中、`git log -S` 也查不到 ⇒
            //  按「**没查到**」记）⇒ 只能**直调**验它。两态 = `ClipNode = null`（回落 `Viewport`）/ 接了节点。
            //  改坏法：把 `Place` 改回直读 `Viewport` ⇒ 态② 的 `x2` 变 1000 ⇒ 红。
            //  ⚠️ 本段与 `MenuScroll.cs` 的注释成对 —— 那里写了「为什么死代码也要补齐」。
            {
                var vpA775 = new PxRect(0f, 0f, 1000f, 500f);                  // 旧的 `Viewport` 那一份
                var vpB775 = new PxRect(0f, 0f, 600f, 500f);                   // 节点上那一份（更窄）
                var scA775 = new MenuScroll(vpA775, 0f, 2000f);
                var scB775 = new MenuScroll(vpA775, 0f, 2000f);
                var fx775 = new GameObject("A775 探针").transform;
                var vc775 = ViewportClip.Hang(fx775, "Viewport", vpB775, Vector4.zero, Vector2Int.zero);
                scB775.ClipNode = vc775;
                PxRect onA, onB; Rect uvA, uvB;
                bool okA = scA775.Place(new PxRect(0f, 100f, 2000f, 400f), out onA, out uvA);
                bool okB = scB775.Place(new PxRect(0f, 100f, 2000f, 400f), out onB, out uvB);
                CheckTrue(okA && okB, "（A775 前提）两态都算得出来（`Place` 返回 true）");
                CheckNear(onA.x2, 1000f, 0.5f,
                          "★ A775 态①：**没接节点** ⇒ 夹到 `Viewport.x2 = 1000`（= 迁移前的行为，逐位不变）");
                CheckNear(onB.x2, 600f, 0.5f,
                          "★ A775 态②：接了 `ClipNode` ⇒ 夹到**节点框** x2 = 600 —— ⛔ 不是 1000（那说明它还在直读 `Viewport`）");
                CheckNear(uvB.width, 0.3f, 0.005f,
                          "★ …而且 `uv` 跟着同一段比例截（0.3 = 600/2000）—— 只缩几何不截 uv 会把图压扁");
                CheckNear(onB.W, 600f, 0.5f, "★ …夹完的矩形宽 = 露出来的那一段");
                Object.DestroyImmediate(fx775.gameObject);
            }

            // ---------- ★ C1-B08：滚轮**没有阈值**（原版 uGUI `ScrollRect.OnScroll` 任何非零 delta 都位移）----------
            //  判据 → `资料/普查产出_1023e/C1_战斗线判据_布置与层级.md` 的 **C1-B08**
            //         + 本地真 uGUI 源码 `Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/ScrollRect.cs:665-676`
            //           （`position += delta × m_ScrollSensitivity`；零 delta 只经 `data.IsScrolling()` 影响
            //            `m_Scrolling`，**不产生位移**）。
            //  ⚠️ 这道自造闸原来有**两道**：`MenuScroll.Wheel`（下游）与 `PointerLayer.WheelAt(Vector2)`（上游）。
            //     只删下游 = 没改（上游先丢）。两条都断，改哪一条都红。
            //  🧨 改坏法：`MenuScroll.Wheel` 的 `if (dy == 0f) return;` 写回 `Mathf.Abs(dy) < 1f` ⇒ ①红；
            //            `PointerLayer.WheelAt` 的 `if (f == 0f) return false;` 写回 `Mathf.Abs(f) < 1f` ⇒ ③红。
            //  ⚠️ ② / ②b 钉的是**行为契约**（零 delta 不许动），不是某一行：今天 `Apply` 里那条
            //     `Mathf.Abs(v - Offset) < 0.01f` 的浮点噪声闸也会兜住它 ⇒ 就算把 `Wheel` 的
            //     `dy == 0` 早退删掉，这两条也仍绿。留着是防「有人把 `Wheel` 改成无条件 `Apply`
            //     并把噪声闸一起拿掉」那一版（那会让一次零 delta 的滚轮事件也起一次重建）。
            {
                // ① / ② 直接打 `MenuScroll.Wheel`：半格必须位移、零 delta 必须不动。
                var scB08 = new MenuScroll(new PxRect(0f, 0f, 1000f, 500f), 0f, 3000f, notchPx: 120f);
                int firedB08 = 0;
                scB08.OnChanged = () => firedB08++;
                scB08.Wheel(-0.5f);                       // 半格（本机一格 = ±1，见 `ScrollUnitsPerNotch`）
                CheckNear(scB08.Offset, 60f, 0.01f,
                          "★ C1-B08 ① **半格滚轮就位移**：dy=−0.5 × 120 px/格 ⇒ **60 px**（原版没有任何阈值）"
                        + $"｜实测 {scB08.Offset:F2}；🧨 写回 `Mathf.Abs(dy) < 1f` ⇒ 0 ⇒ 红");
                CheckTrue(firedB08 == 1, $"★ ①b …而且 `OnChanged` 真通知了一次（实测 {firedB08}）");
                float keepB08 = scB08.Offset;
                firedB08 = 0;
                scB08.Wheel(0f);
                CheckNear(scB08.Offset, keepB08, 1e-4f,
                          $"★ C1-B08 ② **零 delta 不位移**（实测 {scB08.Offset:F4} vs {keepB08:F4}）");
                CheckTrue(firedB08 == 0, $"★ ②b …也不通知 `OnChanged`（实测 {firedB08}）");

                // ③ 走**真入口** `PointerLayer.WheelAt(Vector2)`（`Update` 里取 `Mouse.current.scroll` 那一个）
                //    —— 上游那道闸也删了。`Owner` 挂在**无父**的空节点上 ⇒ `ScrollScale` = (1,1)
                //    ⇒ `DesignToPtrPx` 是恒等 ⇒ 指针 px 就是设计 px（视口 0..1000 × 0..500，取中心 (500,250)）。
                var goB08 = new GameObject("C1-B08 滚动探针");
                var scHit = new MenuScroll(new PxRect(0f, 0f, 1000f, 500f), 0f, 3000f, notchPx: 120f)
                            { Owner = goB08 };
                PointerLayer.RegisterScroll(scHit);
                var plB08 = PointerLayer.Instance;
                bool hitB08 = plB08 != null && plB08.WheelAt(500f, 250f, new Vector2(0f, -0.5f));
                CheckTrue(hitB08, "（前提）`PointerLayer.WheelAt` 在视口中心命中这个滚动区"
                            + "（不命中 ⇒ ③ 会在假绿上过；先看 `InputBlocked` / `RegisterScroll`）");
                CheckNear(scHit.Offset, 60f, 0.01f,
                          "★ C1-B08 ③ **真入口**（`PointerLayer.WheelAt(Vector2)`）传半格 ⇒ 也位移 60 px"
                        + $"｜实测 {scHit.Offset:F2}；🧨 把上游 `if (f == 0f)` 写回 `Mathf.Abs(f) < 1f` ⇒ 0 ⇒ 红");
                PointerLayer.UnregisterOwnedBy(goB08);
                Object.DestroyImmediate(goB08);
            }

            // 🆕 2026-10-08（A212）：`Shared/Close Button` 那颗钮的字（**"Back"**）——原版 **`折行=0`**
            //  （实读：`Button Text … 'Back' 字号=40.0 auto[10.0~40.0] 对齐=Center/Capline 折行=0`）。
            //  改坏法：删掉 `Shell/CollectionWindow.cs` 那颗钮后面的 `SetWrapping(false)` ⇒ 退回
            //  `SetAutoFitBox` 开出来的 `1` ⇒ 红。
            {
                var closeBtn = FindChild(root, "Close Button");
                var bk = closeBtn != null ? FindChild(closeBtn, "Button Text") : null;
                var bl = bk != null ? bk.GetComponent<Label>() : null;
                CheckTrue(bl != null, "`Shared/Close Button/Button Text` 在（A22② 那颗 Back 钮）");
                Check(bl != null ? bl.WrappingMode : -1, 0,
                      "★ `Back` 那颗钮的字 **`折行=0`**（原版实读，别让 `SetAutoFitBox` 开的折行留着）");
                // 🔴 **2026-10-18（第三轮整改 · 审查 P4）**：这颗钮的字从第三轮起**走词条**
                //   （`Shell/CollectionWindow` 的 `Loc.T("MainMenu/MainButtons/ButtonLabel/Back")`，
                //   键 = 原版那颗 `Button Text` 的 `mTerm` 原文）⇒ 顺手把它的**文案**也断上
                //   （原来这一节只断折行/框宽，一个字都没断 ⇒ 「字有没有接上词条」看不见）。
                CheckTrue(Loc.HasEntry("MainMenu/MainButtons/ButtonLabel/Back"),
                          "（前提）词条 `MainMenu/MainButtons/ButtonLabel/Back` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                Check(bl != null ? bl.Text : null, Loc.T("MainMenu/MainButtons/ButtonLabel/Back"),
                      "★ `Back` 那颗钮的**文案** = `Loc.T(\"MainMenu/MainButtons/ButtonLabel/Back\")`（随语档；"
                      + "原版 TMP 原文 `Back` / 中文「返回」`zh_CN.csv:5`）");
                // 🆕 **2026-10-09（A265）**：那颗 `Button Text` 用的是**它自己的矩形 132.86×48.24**，
                //   **不是**整颗钮的 150×60。中心只差 0.25px ⇒ `CheckAt` 那一档**分辨不出**这两态，
                //   所以必须**单独断框宽/框高**（`SetAutoFitBox` 写进 `sizeDelta` 的就是这两数）。
                //   判据 = 原版 dump 字面量（`python 工具/menu_dump.py bundle_menus_assets_all
                //   "Collection Menu Variant" --depth 12` 第 255 行：
                //   `Button Text  200.5, 89.3 → 333.4, 137.5   132.86   48.24`）。
                //   ⚠️ 读的是**场景里那个 TMP 的 `rectTransform.sizeDelta`**（= `SetWrapWidth` 真写进去的值），
                //      **不是**传进 `SetAutoFitBox` 的实参、也不是代码里那两个常量。
                //   改坏法：`Shell/CollectionWindow.cs` 里把 `bt` 换回 `cr`（整颗钮 150×60）
                //   ⇒ 下面两条读 150.00 / 60.00 ⇒ **红**。
                //   ⚠️ 「132.86×48.24 是序列化的、不是 ARF 算的」已查实：那颗节点上的
                //      `AspectRatioFitter` 是 `m_Enabled=0`（判据与算式 → `CollectionWindow.BackTxtL` 注释）。
                {
                    var tmp = bl != null ? bl.GetComponentInChildren<TMPro.TextMeshPro>() : null;
                    CheckNear(tmp != null ? tmp.rectTransform.sizeDelta.x * 108f : -1f, 132.86f, 0.5f,
                              "★ `Back` 的**文本框宽 = 132.86**（= 333.4 − 200.5；原版那颗 `Button Text` 自己的矩形，"
                              + "⛔ 不是整颗钮的 150）");
                    CheckNear(tmp != null ? tmp.rectTransform.sizeDelta.y * 108f : -1f, 48.24f, 0.5f,
                              "★ …框高 = 48.24（= 137.5 − 89.3；同一颗节点的另一半）");
                    CheckAt(bk, 200.5f, 333.4f, 89.3f, 137.5f,
                            "`Button Text` 那颗字**在它自己的矩形中心**（原版 200.5,89.3→333.4,137.5）");
                }
            }
            // 筛选：**复用卡组编辑那套 `DeckEditorState.Filter`**（别写第二套）
            {
                int all = win.CardsVisibleCount;
                var f = DeckFilter.None; f.Rarity = "legendary";
                CollectionWindow.CardsState.SetFilter(f);
                win.RebuildCardsPage();
                CheckTrue(win.CardsVisibleCount > 0 && win.CardsVisibleCount < all,
                          $"筛 `legendary` ⇒ 可见卡 **{all} → {win.CardsVisibleCount}**（筛选真的接上了）");
                CollectionWindow.CardsState.SetFilter(DeckFilter.None);
                win.RebuildCardsPage();
                Check(win.CardsVisibleCount, all, "清空筛选 ⇒ 卡数回到原来的数");
            }

            // ---------------- Cards 页：**完整筛选面板**（正本 §五·1；A3 + `menu_rect.py` 实读）----------------
            Section("Cards 页：筛选面板（7 行 · 面板内坐标逐条比原版）");
            var fltPanel = FindChild(tabsRoot, "Card Filters");
            CheckTrue(fltPanel != null, "面板节点 `Card Filters` 建了");
            if (fltPanel != null)
            {
                // 原版**起手是收起的**（整栏滑到 `hiddenPosition=(-550,0)`，行程 −385px）
                // ⇒ 我们也是「收起 = 滑出去 + 滑完 SetActive(false)」（🆕 A11 起；原来只有后面那半截）
                CheckTrue(!fltPanel.gameObject.activeSelf, "**起手收起**（原版 `hiddenPosition = (-550, 0)`）");
                win.ToggleFilters();
                CheckTrue(fltPanel.gameObject.activeSelf, "`Filters` 圆钮 ⇒ 面板打开");

                CheckAt(fltPanel, 0.25f, 335.56f, 155.9f, 1079.99f,
                        "面板矩形 = **0.25,155.9 → 335.56,1080**（原版 x/w；高按**屏幕可见**的 924.1 裁）");
                CheckNear(FilterPanelModel.ContentHFor(CollectionWindow.CardsState), 1389.02f, 0.1f,
                          "内容高 = **1389.02** = 79.02+50+50 + **Army 550**（13 格 3 列 = 5 行）+ 280+230+150"
                          + "（⚠️ 2026-09-28 前写的是 989.02 —— 那是把 Army 行当成 150 算的，见 `FilterPanelModel.ArmyRowH`）");
                var fscr = win.FilterScroll;
                CheckTrue(fscr != null, "面板挂了滚动区（原版 `Scroll View` sens **50**、`Viewport` + `Mask showGraphic=0`）");
                if (fscr != null)
                    CheckNear(fscr.MaxOffset, 1389.02f - 924.1f, 0.6f,
                              "可滚量 = **约 464.9**（1389.02 − 924.1）—— **「Cost / Type 够得着」的判据**");

                // 🔴 「一个值 ≠ 全部情况」（铁律 5·c）：**选项表是从 MB 实读的**，不是按枚举直觉编
                //    （`Card*Filter.options` 的 `alternativeText`）：Army 13 / Rarity 5 / Cost **8** / Type 3
                //    + `Owned only` / `Upgradable only` 两格（原版 `filters[6]` 里的前两个）
                Check(win.FilterCellCount, 31,
                      "格子总数 = **31** = Owned + Upgradable + Army **13** + Rarity **5** + Cost **8** + Type **3**"
                      + "（⚠️ Cost 是**区间档**不是每费一格；⚠️ Type **没有防御卡那一档**）");

                // Army 第 1 格：Content 从行内 y+50 起、pad L14、cell 100×100
                //   ⚠️ 阵营名别写死 —— 按 `CardsState.Factions()`（**排序过**）的第 1 个取
                var facs = CollectionWindow.CardsState.Factions();
                CheckTrue(facs.Count > 0, $"卡池里有 {facs.Count} 个阵营（Army 行按它铺格）");
                var a0 = facs.Count > 0 ? FindChild(fltPanel, "Cell_fac_" + facs[0]) : null;
                CheckTrue(a0 != null,
                          "Army 那一格在（按**名字**找，不按序号 —— 视口外的格不建）；"
                          + "第 1 格 = 「" + (facs.Count > 0 ? facs[0] : "?") + "」");
                if (a0 != null)
                {
                    CheckNear(MenuCheck.DesignPx(a0).x, 64.25f, 0.6f, "Army 第 1 格中心 x = **64.25**（0.25+14+50）");
                    CheckNear(MenuCheck.DesignPx(a0).y, 434.92f, 0.6f,
                              "Army 第 1 格中心 y = **434.92**（155.9+179.02+50+50）");
                    CheckArt(a0, DeckRuntime.FactionIcon(facs[0]), "Army 格的图 = 该阵营图标（原版运行时赋）");
                    // 🆕 2026-10-05（A32③）：Army 行**关着时**的 off 色 = `(0.5,0.5,0.5,1)`（出厂无阵营筛选 ⇒ 全是关的）
                    CheckTrue(string.IsNullOrEmpty(CollectionWindow.CardsState.Filter.Faction),
                              "（前提）没有阵营筛选 ⇒ Army 格是【关】的（下面断的是 off 色）");
                    CheckTint(FindChild(a0, "Background"), new Color(0.5f, 0.5f, 0.5f, 1f),
                              "Army 格 off 色 = 原版 `offColor (0.5,0.5,0.5,1)`（不是共用的 0.349）");
                }
                // Rarity 第 1 格：Content 从行内 y+65 起；图**比格小**（格 100²、图 50² 居中）
                var r0 = FindChild(fltPanel, "Cell_rar_common");
                CheckTrue(r0 != null, "Rarity 的 Common 格在");
                if (r0 != null)
                {
                    CheckNear(MenuCheck.DesignPx(r0).y, 999.92f, 0.6f,
                              "Rarity 第 1 格中心 y = **999.92**（155.9 + **729.02** + 65 + 50；729.02 = Army 行 550 之后）");
                    CheckArt(r0, "1_40k_cardframe_rarity_common", "Rarity Common 的图 = `1_40k_cardframe_rarity_common`");
                    var bg = FindChild(r0, "Background");
                    CheckNear(Wpx(bg), 50f, 2f, "Rarity 格里的图宽 = **50**（格 100 ⇒ 图只有一半，原版如此）");
                    // 🔴 实拍又抓一条：标签像**贴着面板左边界、被切掉**。
                    //    做法同 CLAUDE.md 的「量渲染真值」——**量 Label 自己的宽度算左边缘**，别只看节点位置。
                    //    ⚠️ 第一版这条断言**恒真**（宽度量出来 0 ⇒ 左边缘 = 右边界）⇒ 现在**同时要求宽度合理**。
                    //    ⚠️ 第二版又抓出 `Legendary` 105.9px > 原版格宽 100 ⇒ 补上原版的 `auto(10-27)`
                    //      （`SetAutoFitBox`），再断「缩完之后落进格子里」。
                    var lab0 = FindChild(r0, "Label");
                    var lb0 = lab0 != null ? lab0.GetComponent<Label>() : null;
                    float lw = LabelRenderedPx(lb0).x;   // 🆕 A796′ 换口：TMP `textBounds`（旧口 = 缓存 `Label.WorldW`；null ⇒ 0）
                    float lleft = lab0 != null ? MenuCheck.DesignPx(lab0).x - lw * 0.5f : -999f;
                    CheckTrue(lb0 != null && lw > 20f,
                              $"Rarity 格的标签**量得出宽度**（实测 {lw:F1}px）—— 量到 0 就是「TMP 在非激活对象上量不出尺寸」");
                    CheckTrue(lb0 != null && lw <= 100.5f,
                              $"标签**缩进原版的 100px 格宽里**（实测 {lw:F1}px ≤ 100）"
                              + " —— 判据是原版那几处标着 `auto(10-27)`；不开自适应会冲出格子");
                    CheckTrue(lb0 != null && lb0.FontPxNow >= 9.5f && lb0.FontPxNow <= 27.5f,
                              $"标签字号落在原版 `auto(10-27)` 区间里（实测 {lb0?.FontPxNow:F1}px）");
                    // 🆕 **2026-10-12（A333 · F1 §三 #3）：上限那一格是【27】，不是标称的 23.2** ——
                    //   判据 = 原版 `Collection Menu Variant` 两页各一颗的 `m_fontSizeMax = 27`
                    //   （普查逐颗实读 → `资料/普查产出_1011/V7_A305_A304_普查.md`；本件亲跑
                    //    `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12 --md`
                    //    的 `auto[10.0~27.0]` 一节）。本工程原来把**调用方那一档（`fontPx 23.2`）**当上限。
                    //   🔴 口径（F1 §三 末）：**反射直读 TMP 的真字段**（`Label.FontSizeMax`，不是我们的账本），
                    //      期望值 **27 写字面量**（原版资产字段）——
                    //      ⛔ **不许读 `FilterPanelModel.RarityFontAutoMax` 来比**（那是被测实现里的常量 = 自证）。
                    //   ⚠️ `FontSizeMax` 是 **TMP 的 `fontSize` 单位**（不是 px）⇒ 经 `Label.FontSizeToPx` 折回；
                    //      `_tmp == null`（点阵兜底）时它恒 0 ⇒ 折出 0 ⇒ **这条会红，不会静默绿**。
                    float rarMaxPx = lb0 != null ? Label.FontSizeToPx(lb0.FontSizeMax) : -1f;
                    CheckNear(rarMaxPx, 27f, 0.35f,
                              $"★ Rarity 族 `Label` 的**上限** = **原版 `m_fontSizeMax` 27.0 px**（实得 {rarMaxPx:F2}；"
                            + "标称那一档是 23.2）"
                            + "。改坏法：把 `Core/FilterPanelModel.cs` 的 `LabelAutoMax = RarityFontAutoMax` 删掉"
                            + " ⇒ 上限退回标称 23.2 ⇒ 红");
                    CheckTrue(Mathf.Abs(rarMaxPx - 23.2f) > 0.5f,
                              $"…而且它**不是**标称那一档 23.2（实得 {rarMaxPx:F2}）—— 这条证明上面那个 27 分得出两种状态");
                    // 🆕 2026-10-08（A212）：**折行**要按这一族自己的原版值 —— 稀有度族 = `0`
                    //  （`SetAutoFitBox` 会**无条件**把 `m_TextWrappingMode` 开成 `1` ⇒ 不显式还原就是「碰巧错」）。
                    //  ⛔ 别按 `LabelCenter` 之类反推（那会把三族一起漏掉）；判据 = 原 prefab 那一格自己的字段。
                    Check(lb0 != null ? lb0.WrappingMode : -1, 0,
                          "★ Rarity 族的标签 **`折行=0`**（原版实读；删掉 `TextAligned` 里那句 `SetWrapping` ⇒ 退回 1 ⇒ 红）");
                    CheckTrue(lb0 != null && lleft >= 0f && lleft + lw <= 335.6f,
                              $"Rarity 格的标签**落在面板内**（左边缘 {lleft:F1}px · 文字宽 {lw:F1}px · "
                              + $"原版右对齐到格子右边 {114.25f:F1}px）");
                    // 🆕 2026-10-05（A32③）：Rarity 那一行**关着时**的 off 色 = `(0.5,0.5,0.5,`**0.749**`)`
                    //   —— 原来是**一份共用值 0.349**（Army/Rarity 两行都偏深，而且丢了 alpha）。
                    CheckTrue(string.IsNullOrEmpty(CollectionWindow.CardsState.Filter.Rarity),
                              "（前提）没有稀有度筛选 ⇒ Rarity 格是【关】的");
                    CheckTint(FindChild(r0, "Background"), new Color(0.5f, 0.5f, 0.5f, 0.749f),
                              "Rarity 格 off 色 = 原版 `offColor (0.5,0.5,0.5,0.749)`（原来是共用的 0.349）");
                }

                // ============================================================ 🆕 2026-10-05（A32①③④）
                //  开关行那三件事：**状态换图** · **逐行 off 色** · **标签左对齐**。
                //  判据 = 原 prefab 的字段（`EverguildToggle.onSprite/offSprite` · `colorTintOnValueChange` ·
                //  `offColor` · `Label.m_HorizontalAlignment`）——复读命令见 `Core/FilterPanelModel` 的
                //  `OffTintFaction/Rarity/CostType` 与 `Build()` 里那两处长注释。
                {
                    // ① 状态换图（这扇窗原来**恒画 on 那张**：镜像模型时漏了 `IconOff` ⇒ 开/关只差一点色偏）
                    CheckArt(FindChild(fltPanel, "Cell_owned"), "40_main_bt_toggle_on",
                             "`Owned only`（出厂**开**）画的是 `40_main_bt_toggle_on`");
                    CheckArt(FindChild(fltPanel, "Cell_upgradable"), "40_main_bt_toggle_off",
                             "`Upgradable only`（出厂**关**）画的是 `40_main_bt_toggle_off` —— "
                             + "🔴 A32① 收的就是这一条（原来两扇窗都恒画 on 图）");
                    win.ApplyCardFilter("$owned");                       // 关掉它
                    CheckTrue(!CollectionWindow.CardsState.Filter.Owned, "（前提）`Owned only` 切到关");
                    CheckArt(FindChild(fltPanel, "Cell_owned"), "40_main_bt_toggle_off",
                             "关掉 ⇒ 换成 `40_main_bt_toggle_off`（原版 `changeSpriteOnValueChange = 1`）");
                    win.ApplyCardFilter("$owned");                       // 切回来
                    CheckArt(FindChild(fltPanel, "Cell_owned"), "40_main_bt_toggle_on",
                             "再切回来 ⇒ 又变回 on 那张（判据可逆，不是单向的）");

                    // ② 开关那一类**不吃 tint**（原版 `colorTintOnValueChange = 0`）⇒ 两个态都是**白**。
                    //    ⚠️ 这条是「原来那套色偏」的对照：改回 0.349 就红。
                    CheckTint(FindChild(FindChild(fltPanel, "Cell_upgradable"), "Background"), Color.white,
                              "开关格（`IconOff != null`）**不打 off 色偏**（原版 `colorTintOnValueChange = 0`"
                              + " ⇒ 开/关**只靠换图**区分）");

                    // ③ 标签**左对齐**（原版 `m_HorizontalAlignment = 1`）——挂在**文字左缘**上量，
                    //    不是看节点位置（判据同上面 Rarity 那条：`center − 渲出来的宽/2`；
                    //    🆕 **2026-10-16（A796′）换口**：那个「宽」改走 `LabelRenderedPx`（TMP `textBounds`），
                    //    不再读字段缓存 `Label.WorldW` —— 换口当天逐位同值，公式本身一个字没变）。
                    //    期望值 25.25 来自**原版 `Label` 的 rect 左缘**（`25.25,234.96→234.96,284.96`）。
                    var olab = FindChild(FindChild(fltPanel, "Cell_owned"), "Label");
                    var olb = olab != null ? olab.GetComponent<Label>() : null;
                    float owl = LabelRenderedPx(olb).x;   // 🆕 A796′ 换口（旧口 = 缓存 `Label.WorldW`；null ⇒ 0）
                    float oleft = olab != null ? MenuCheck.DesignPx(olab).x - owl * 0.5f : -999f;
                    CheckTrue(olb != null && owl > 20f, $"`Owned only` 的标签量得出宽度（实测 {owl:F1}px）");
                    CheckNear(oleft, 25.25f, 1.0f,
                              "`Owned only` 的标签**左对齐**在面板内 x = **25.25**（原版 `Label` rect 的左缘）"
                              + " —— 居中的话左缘会落在 ~95px（`LabelCenter` 改回 `true` 就红）");
                    // 🆕 2026-10-08（A212）：开关族（`Owned`/`Upgradable`）原版 **`折行=0`**。
                    Check(olb != null ? olb.WrappingMode : -1, 0,
                          "★ `Owned only` 的标签 **`折行=0`**（原版实读；`SetAutoFitBox` 会把它开成 1 ⇒ 必须显式还原）");
                }
                // Cost 第 1 格 / Type 那一行：**2026-09-28 起都在视口外** ——
                //   Army 行的高度现在按**它自己的内容**算（13 格 · 3 格/行 = 5 行 = 550），Rarity 及以下整排往下挪
                //   ⇒ **不滚到底，Cost / Type 的格子根本不建**（原版 `RectMask2D` 那套裁切 + 我们的视口剔除）。
                //   判据与两处证据（A3 的 150 vs 卡组编辑那棵树的 `332x0`）→ `FilterPanelModel.ArmyRowH` 的注释。
                if (fscr != null)
                {
                    CheckTrue(FindChild(fltPanel, "Cell_cost_1") == null && FindChild(fltPanel, "Cell_type_hero") == null,
                              "**不滚时 Cost / Type 的格子不建**（都在视口下方）");
                    fscr.ScrollBy(fscr.MaxOffset);                       // 滚到底
                    var c0 = FindChild(fltPanel, "Cell_cost_1");
                    CheckTrue(c0 != null, "滚到底 ⇒ Cost 的 `1-` 格建出来了");
                    if (c0 != null)
                    {
                        CheckNear(MenuCheck.DesignPx(c0).x, 47.75f, 0.6f, "Cost 第 1 格中心 x = **47.75**（0.25+15+32.5）");
                        // 面板内 y = 609.02+65+32.5（旧值，见 `ArmyRowH`）→ 现在 = 1009.02+65+32.5，再减可滚量 464.92
                        CheckNear(MenuCheck.DesignPx(c0).y, 155.9f + 1009.02f + 65f + 32.5f - 464.92f, 0.8f,
                                  "Cost 第 1 格中心 y（滚到底后）= **797.5**");
                        CheckNear(Wpx(c0), 65f, 2f, "Cost 格 = **65×65**（原版 `cell 65×65`）");
                        CheckArt(c0, "Card_Frame_Cost_Icon", "Cost 格的图 = `Card_Frame_Cost_Icon`");
                        // 🆕 2026-10-05（A32③）：Cost / Type 两行的 off 色 = `(0.349,0.341,0.341,1)`
                        //   ⚠️ **三个通道不是一个数**（0.349/0.341）—— 写成 `(0.349,0.349,0.349)` 也会红，
                        //     这正是「照抄 prefab 字面量」与「随手抹一个灰」的区别。
                        CheckTrue(CollectionWindow.CardsState.Filter.Cost == DeckEditorState.AnyCost,
                                  "（前提）没有费用筛选 ⇒ Cost 格是【关】的");
                        CheckTint(FindChild(c0, "Background"), new Color(0.349f, 0.341f, 0.341f, 1f),
                                  "Cost 格 off 色 = 原版 `offColor (0.349,0.341,0.341,1)`");
                        // 🆕 2026-10-08（A212）：**费用桶是四族里【唯一】折行的那一族**（原版 `折行=1`）
                        //  ⇒ 三族设 0 的时候别把它一起设成 0（这条与 Rarity/Owned 那两条互为对照）。
                        var cvLab = FindChild(c0, "Label");
                        var cvLb = cvLab != null ? cvLab.GetComponent<Label>() : null;
                        Check(cvLb != null ? cvLb.WrappingMode : -1, 1,
                              "★ Cost 族的标签 **`折行=1`**（原版实读；三族一起设成 0 就红）");
                    }
                    var th = FindChild(fltPanel, "Cell_type_hero");
                    CheckTrue(th != null && MenuCheck.DesignPx(th).y + 50f <= 1080.5f,
                              "滚到底 ⇒ Type 那 3 格**完整落进视口**");
                    CheckArt(th, "40k_menu_search_icon_warlord", "Type 第 1 格（Warlord）的图 = `40k_menu_search_icon_warlord`");
                    CheckTrue(string.IsNullOrEmpty(CollectionWindow.CardsState.Filter.Type),
                              "（前提）没有类型筛选 ⇒ Type 格是【关】的");
                    CheckTint(FindChild(th, "Background"), new Color(0.349f, 0.341f, 0.341f, 1f),
                              "Type 格 off 色 = 原版 `offColor (0.349,0.341,0.341,1)`");
                    CheckTrue(FindChild(fltPanel, "Cell_type_unit") != null
                              && FindChild(fltPanel, "Cell_type_tactic") != null,
                              "Type 另两格 `Troops` / `Stratagem` 也在（图 = `..._troop` / `..._stratagem`）");
                    fscr.ScrollBy(-fscr.MaxOffset);                  // ⚠️ 量完**滚回顶部** —— 下面几条断言
                    //    （四个小标题、搜索框）量的都是**顶部**那些件，不滚回去它们根本不建
                }

                // 四行的小标题（原版 `Title` TMP · **fs32 · hAlign=Left/Middle**）
                //   🔴 2026-09-23 **实拍补的缺口**：第一版只建了格子、**四个标题一个都没建**，
                //      96 条断言全绿 —— 因为它们不是「摆错位」而是「根本不在」，而当时没有盯这一条的断言。
                //   🔴 **2026-10-05（A93②）**：判据那句 `hAlign=Center` 是**读错了** —— 原版是 `Left/Middle`
                //      （判据 = `Core/FilterPanelModel.cs` 的 `TitleFontPx` 那段，两扇窗逐行实读）。
                //      对齐单独断（下面那四条量**左沿**），不只是「在不在」。
                // 🔴 **2026-10-18（A891 的续 · 续做 A）：四行小标题全走词条**（键 = 原版那颗 `Localize.mTerm`
                //   的原文，判据见 `Core/FilterPanelModel.cs` 的 `TitleTerm`）⇒ 期望值随语档。
                //   🔴🔴 **第三轮整改（审查 P1）就地订正（铁律 5）**：这一段原来写「`Energy Cost` 那一行
                //   原版**没有** `Localize`（全库 3 颗）⇒ 它恒英文」—— **那是错的**。错因 = **「全库」只扫了
                //   `bundle_menus_assets_all` 一个包**。复跑全部 80 个 bundle：`m_text == "Energy Cost"` 的 TMP
                //   **17 颗 / 15 个 bundle**，**每颗都挂 `Localize.mTerm = "Battle/Tips/EnergyCost"`**
                //   ⇒ 它是正经词条，已接上（本文件下面那条断言就是正面判据）。
                //   ⚠️ 上面那句「键在表里」是**必须的**：键不在 ⇒ `Loc.T` 返回键名本身、渲染那一侧同样返回
                //   键名 ⇒ 只比相等会**假绿**（两边一起退化）。
                CheckTrue(Loc.HasEntry("MenuDeck/Filters/Army") && Loc.HasEntry("MenuDeck/HUD/Rarity")
                          && Loc.HasEntry("MenuDeck/Filters/Type") && Loc.HasEntry("Battle/Tips/EnergyCost"),
                          "（前提）四行小标题词条都在 `Loc` 表里（⛔ 有一条不在 ⇒ 下面四条两边一起退化成键名 = 假绿）");
                CheckText(TextOf(FindChild(fltPanel, "Title Army")), Loc.T("MenuDeck/Filters/Army"),
                          "小标题 `Army` 在（原版 `Title` TMP fs32；键 `MenuDeck/Filters/Army`，随语档；顶部视野内）");
                CheckText(TextOf(FindChild(fltPanel, "Title Rarity")), Loc.T("MenuDeck/HUD/Rarity"),
                          "小标题 `Rarity` 在（键 `MenuDeck/HUD/Rarity`，随语档；顶部视野内）");
                // 🔴 **左沿断言（4 条，2026-10-05 A93②）** —— 期望值是**原版的读数**，不是我们的常量：
                //   原版面板内左沿 = Army 行 **0** · 其余三行 **25**（`Collection Menu Variant` 的 Cards 页
                //   `Title` 实读 `0.3 / 25.3`、面板原点 `0.3`；卡组编辑那棵同族 `2.2 / 27.2`、原点 `2.2`）
                //   ⇒ 画布绝对 x = 面板原点 **0.25** + 那个数 = **0.25 / 25.25**（**写死字面量**，
                //   ⛔ 不拿 `CollectionWindow.FltL` + 模型常量去算 = 那是自证）。
                //   量的东西：**TMP 自己渲出来那块 `textBounds`**（`TitleLeftPx` → `LabelRenderedPx`）反推的**渲染左缘** ——
                //   🆕 **2026-10-16（A796′）换口**：换口前读的是 `Label.WorldW` 那份**字段缓存** `_tmpW`
                //   （两法同源 ⇒ 换口当天逐位同值；期望值与容差**一字未动**）。
                //   ⛔ 不是节点位置、更不是「对齐枚举 == Left」（那是同义反复，改坏实现照样绿）。
                //   改坏会红：把 `CollectionWindow.TitleRow` 里那句 `MenuDraw.AlignLeft` 删掉
                //   （或 `Title.Left` 退回 false）⇒ 左沿落在矩形**中心**附近（Army ~129 / 其余 ~135）。
                CheckNear(TitleLeftPx(fltPanel, "Army"), 0.25f, 1f,
                          "小标题 `Army` 的**渲染左沿** = 面板内 **0**（原版 `Title` `m_HorizontalAlignment=1`"
                          + " ⇒ 左对齐；居中画的话会落在 ~129）；量不出来时这里给 −9999");
                CheckNear(TitleLeftPx(fltPanel, "Rarity"), 25.25f, 1f,
                          "小标题 `Rarity` 的**渲染左沿** = 面板内 **25**（原版那三行都从 x=25 起；居中会落在 ~135）");
                // ⚠️ 2026-09-28：`Energy Cost` / `Type` 两个标题落在 Army 行（550 高）之后 ⇒ **要滚下去才建**
                if (fscr != null)
                {
                    fscr.ScrollBy(fscr.MaxOffset);
                    // 🔴 2026-10-18（A891 的续 · 续做 A）：`Energy Cost` / `Type` 都走词条（随语档）。
                    //   ⚠️ **第三轮整改（审查 P1）**：`Energy Cost` 原来这条**把「原版无词条」钉成了检验标准**
                    //   —— 是最坏的一档（下一个人要接它时会先撞红）。现改为正面判据。
                    CheckText(TextOf(FindChild(fltPanel, "Title Energy Cost")), Loc.T("Battle/Tips/EnergyCost"),
                              "小标题 `Energy Cost` 在（键 `Battle/Tips/EnergyCost`，随语档；滚到底之后才够得着）");
                    CheckText(TextOf(FindChild(fltPanel, "Title Type")), Loc.T("MenuDeck/Filters/Type"),
                              "小标题 `Type` 在（键 `MenuDeck/Filters/Type`，随语档；滚到底之后才够得着）");
                    // 同样量左沿（**滚到底之后**才够得着 ⇒ 这两条必须在 `ScrollBy(-MaxOffset)` 之前）
                    CheckNear(TitleLeftPx(fltPanel, "Energy Cost"), 25.25f, 1f,
                              "小标题 `Energy Cost` 的**渲染左沿** = 面板内 **25**（滚到底时量的）");
                    CheckNear(TitleLeftPx(fltPanel, "Type"), 25.25f, 1f,
                              "小标题 `Type` 的**渲染左沿** = 面板内 **25**（滚到底时量的）");
                    fscr.ScrollBy(-fscr.MaxOffset);
                }

                // ---- 筛选**真的接上了**（每一条都拿 `DeckEditorState` 的结果数对照）----
                int all = win.CardsVisibleCount;
                System.Action<string> clickCell = key =>
                {
                    var cell = FindChild(tabsRoot, key);
                    var hit = cell != null ? FindChild(cell, "Hit") : null;
                    var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
                    if (wb != null) wb.Click();
                };
                clickCell("Cell_rar_legendary");
                var vis = CollectionWindow.CardsState.VisibleCards();
                CheckTrue(vis.Count > 0 && vis.Count < all, $"点 `Legendary` ⇒ 卡池 {all} → {vis.Count}");
                bool allLeg = true;
                foreach (var c in vis) if (c.Rarity != "legendary") { allLeg = false; break; }
                CheckTrue(allLeg, "筛出来的**每一张**都是 `legendary`");

                win.ClearCardFilters();
                Check(win.CardsVisibleCount, all, "`Clear filters` ⇒ 卡数回到 " + all);

                // ⚠️ Cost / Type 的格子**滚到底才建**（见上一段那条）⇒ 点它们之前先滚下去
                if (fscr != null) fscr.ScrollBy(fscr.MaxOffset);
                clickCell("Cell_cost_8");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allGe8 = vis.Count > 0;
                foreach (var c in vis) if (c.Cost < 8) { allGe8 = false; break; }
                CheckTrue(allGe8, $"点 `8+` ⇒ {vis.Count} 张**全部 cost ≥ 8**"
                                  + "（原版 Cost 是**区间档**：`1-`/2…7/`8+`，不是每费一格）");
                win.ClearCardFilters();

                clickCell("Cell_cost_1");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allLe1 = vis.Count > 0;
                foreach (var c in vis) if (c.Cost > 1) { allLe1 = false; break; }
                CheckTrue(allLe1, $"点 `1-` ⇒ {vis.Count} 张**全部 cost ≤ 1**（下界那档的效果）");
                win.ClearCardFilters();

                clickCell("Cell_type_hero");
                vis = CollectionWindow.CardsState.VisibleCards();
                bool allHero = vis.Count > 0;
                foreach (var c in vis) if (c.Type != "hero") { allHero = false; break; }
                CheckTrue(allHero, $"点 `Warlord` ⇒ {vis.Count} 张**全部是督军**（= `CardTypeOptions.Hero`）");
                win.ClearCardFilters();
                if (fscr != null) fscr.ScrollBy(-fscr.MaxOffset);        // 回顶部（搜索框在上面）

                // 搜索框：**外壳自己没有键盘**（`PointerLayer` 原来明写「键盘没实现」）⇒ 2026-09-23 补上
                var pl = PointerLayer.Instance;
                CheckTrue(pl != null, "`PointerLayer` 在（键盘走它）");
                if (pl != null)
                {
                    clickCell("Name Filter");
                    CheckTrue(pl.TextEditing, "点搜索框 ⇒ **进入文本编辑**");
                    foreach (var ch in "impe") pl.TypeChar(ch);
                    CheckText(pl.TextBuffer, "impe", "逐字输入 `impe` ⇒ 缓冲对得上");
                    pl.EndText(true);
                    vis = CollectionWindow.CardsState.VisibleCards();
                    CheckTrue(vis.Count > 0 && vis.Count < all,
                              $"卡名筛 `impe` ⇒ 卡池 {all} → {vis.Count}（**外壳的键盘真的接上了**）");
                    win.ClearCardFilters();
                    Check(win.CardsVisibleCount, all, "再清空 ⇒ 回到 " + all);
                    CheckText(CollectionWindow.CardsState.Filter.Name ?? "", "", "清空把卡名也一起清了");
                    // 🔴 2026-09-23 实拍抓的：**搜索框停在 `impe_` 没回到占位符**（断言当时一条都没报）
                    //    ⇒ 补这一条盯**画面上的字**（`Label.Text`），别只盯状态
                    var nf = FindChild(FindChild(fltPanel, "Name Filter"), "Input Text");
                    // 🔴 **2026-10-18（A891 的续）改「随语档」**：占位符从本批起走词条
                    //   （`Core/FilterPanelModel.InputPlaceholder` → `Loc.T("MenuDeck/HUD/SearchFilter")`）
                    //   ⇒ 宿主跑中文档时写死的 `"Search"` 必红。原文（`Search`）留在 `Loc` 的英文列里
                    //   （= 原版 `Placeholder` 那颗 TMP 的 `m_text` 原文）。
                    CheckTrue(Loc.HasEntry("MenuDeck/HUD/SearchFilter"),
                              "（前提）词条 `MenuDeck/HUD/SearchFilter` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
                    CheckText(TextOf(nf), Loc.T("MenuDeck/HUD/SearchFilter"),
                              "清空后搜索框回到占位符（键 `MenuDeck/HUD/SearchFilter`，随语档；"
                              + "**实拍抓出来的那条**：光看状态量不到）");
                }

                // 🔴 层序：筛选栏**必须盖在卡池之上**（原版兄弟序 `Collection Display` 在前、`Card Filters` 在后）
                var fbg = FindChild(fltPanel, "Panel");
                var fq = fbg != null ? fbg.GetComponentInChildren<ImageQuad>() : null;
                if (fq != null && win.CardsCells.Count > 0)
                {
                    var mrs = win.CardsCells[0].GetComponentsInChildren<MeshRenderer>(true);
                    int maxQ = int.MinValue;
                    foreach (var mr in mrs)
                    {
                        if (mr.sharedMaterial == null) continue;
                        maxQ = Mathf.Max(maxQ, mr.sharedMaterial.renderQueue);
                    }
                    CheckTrue(maxQ >= 0 && fq.RenderQueue > maxQ,
                              $"筛选栏底板队列 **{fq.RenderQueue}** > 卡池最高层 **{maxQ}**"
                              + " —— 「面板被卡池盖住」那类 bug 的判据（矩形断言量不到它）");
                }
                Shoot("03_收藏_Cards_筛选栏.png");
                win.ToggleFilters();
                CheckTrue(!fltPanel.gameObject.activeSelf, "再点一次 `Filters` ⇒ 面板收起");
            }

            // ---------------- 🆕 2026-10-04（§三第29条 A11）：筛选栏的**滑入/滑出** ----------------
            //   判据（原文 → `资料/待办判据_卡面卡池与双语.md` §四 那条操作链 + `卡组编辑界面_查证_0920.md:434`）：
            //     `Filter Toggle` → `CollectionDisplay.OnEnable → ToggleFilters(bool)`
            //     → `CollectionFilterController.Toggle(bool,bool)` → `DOTween.Kill` +
            //       **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`：收起 x = `hiddenPosition.x` = **−550**、
            //       展开 x = `originalAnchorPosition.x` = **−165**（**两个都是父系里的 `anchoredPosition`**）
            //       ⇒ **行程 = −385px**（🆕 2026-10-04 订正，X3 审查的 R5 —— 原来我们按 −550 走，多 43%）；
            //       ⚠️ **只动 x**（`anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）。
            //   ⚠️ 批处理**没有帧循环**（CLAUDE.md §二）⇒ `Update` 一次都不跑、`Toggle*` **直接到位**；
            //      动画本身由**确定性口**复验（`TickDrawers` / `SetDrawerProgressForTest`）——
            //      两条路走的是**同一个** `ApplyDrawerSlide`（不是「自检走一份、跑起来走另一份」）。
            //   🔴 **真红法（先说清把哪一行改坏它会红）** —— 2026-10-04 逐条推演订正过
            //     （X3 审查的 R12：原来 5 条里有 2 条说错）：
            //     · `ApplyDrawerSlide` 里 `lp.x += LayoutSpace.Px(FltHiddenDx) * (1 − p.Slide)` 那句**删掉**
            //       ⇒ 只红**两条**：「挪了半个行程」与「还差 10%」—— 而「回到原位」那条**照样绿**
            //       （它量的是「回到了 `BasePos`」，删掉那句它**恒**在 `BasePos`）。
            //     · 那句里把 **`LayoutSpace.Px(...)` 去掉**（= 同步点实跑抓到的那条 **R1**：px 裸进世界单位）
            //       ⇒ 上面那两条红（275→29700、55→5940）**而且**「★ 这一格此刻还在屏内」也红
            //       （实测量到 −5876 —— 那一条正是「点不到」的前提，前提塌了「点不到」就没意义）。
            //     · 行程改回 −550（行程偏 43%）⇒ 上面两条也红（它们把**轨迹**钉住了，不只是「回没回原位」）。
            //     · `StepDrawer` 里 `dt / FltAnimTime` 改成 `dt` ⇒ 「推进 0.15s ⇒ 进度 0.5」红。
            //     · `SetDrawerInteractive` 里 `WindowButton.enabled` 那一行删掉 ⇒ 红的是 **②「点不到」**
            //       （那一行是**唯一**关这些命中区的地方 —— 建的时候那次 `force: true` 也走它；
            //        位置断言照样全绿，正是「画面看着对、真鼠标还点得到」那类静默 bug）；
            //       而 ③「推到 1 **又点得到**」会**照样绿**（按钮从没被关过）—— 说清是哪一条才有用。
            //     · 「撤登记滚动区」那半份删掉 ⇒ ②的「滚轮也不该被这一列吃掉」红。
            //     · `DrawerSettled` 改成恒 `true` ⇒ 「不参与命中/滚轮」「起点不算到位」「不再吃命中/滚轮」三条红。
            //     · `ToggleFilters` 不调 `StartDrawerSlide`（只翻 `Open`）⇒ 「点一次 ⇒ 进度 1」与
            //       「到位了 ⇒ 命中/滚轮生效」红。
            //     · `BuildDeckFilterDrawer` 收尾那次 `force: true` 退回 `force: false`（= X3 审查的 **R4**）
            //       ⇒ Deck 页那三条数 `WindowButton.enabled` 的断言红（第 1 条最直接）。
            Section("筛选栏的滑入/滑出：位移 + 0.3 秒 + 位移期间命中/滚轮失效（A11）");
            {
                var pl3 = PointerLayer.Instance;
                CheckTrue(pl3 != null, "`PointerLayer` 在（下面两条要拿它问「真鼠标点不点得到」）");
                // ⚠️ **前置：把前几段留下的弹窗收干净** —— ② 那两条走的是**真命中路**（`ButtonAt`），
                //    而 `Deck info Popup` 的 `Warlord Image` 命中区盖着 x∈[−109, 999]、y∈[−34, 1074]（比屏还大），
                //    层又是 `QDIHit = 3129` > 筛选格的 `QFltHit = 3043` ⇒ 留着它，
                //    「点不到 / 点得到」两条量到的都是**它**顶掉的结果（不是筛选格的真值）。
                // 🔴 **2026-10-04（A66）：这笔债已经还清** —— 原来一路开到收工的是 **2 扇**
                //    （`_tmp_view/collection.log:6055`「收掉 2 扇」）：A31 那一段建的四扇里 **`v2`/`v3` 没关**
                //    （上面已补），加上 `Practice Deck` 那一段的 `dp3`（也补了兜底）。
                //    ⇒ 这一段从「只打日志」升成**断言**：走到这里还开着 = **新开的一笔债**，当场红，
                //      别再被下面这次清扫静默盖住（收紧的判据，不是放松）。
                int closedLeftovers = 0;
                foreach (var lw in Object.FindObjectsByType<DeckInfoPopup>(FindObjectsSortMode.None))
                    if (lw != null && lw.CurrentState != WindowState.Closed) { lw.Close(); closedLeftovers++; }
                Check(closedLeftovers, 0,
                      "★ A11 前置：这一路跑完，场上**没有**还开着的 `Deck info Popup`（A66 —— 有 = 又漏关了一扇"
                      + (closedLeftovers == 0 ? "）" : $"：实测 {closedLeftovers} 扇，它层 3129、"
                         + "`Warlord Image` 命中区比屏还大，会顶掉下面两条真命中路）"));
                // 起点：上面那一段刚把它收回去
                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "起点进度 = **0**（整栏停在 `hiddenPosition` 那一头）");
                CheckTrue(!win.DrawerSettled(0), "…不参与命中/滚轮");

                win.ToggleFilters();                                   // 开（批处理里直接到位）
                CheckNear(win.DrawerSlide(0), 1f, 0.001f, "点一次 ⇒ 进度 **1**（原位）");
                CheckTrue(win.DrawerSettled(0), "…到位了 ⇒ 命中/滚轮生效");
                float baseCx = MenuCheck.DesignPx(fltPanel).x;

                // ① 时间推进：0.15 秒 = 半个 0.3 ⇒ 进度正好 0.5（这一步**只有真按 `animationTime` 走**才成立）
                win.SetDrawerProgressForTest(0, 0f);
                CheckTrue(fltPanel.gameObject.activeSelf,
                          "钉在**滑出来的起点**（进度 0 · 目标 1）⇒ 节点**还活着** —— 原版也是先 `SetActive(true)` 再动 tween，"
                          + "不然滑出来的那 0.3 秒根本看不见东西（**滑完**那一下才关，见本段末尾）");
                CheckTrue(!win.DrawerSettled(0), "…起点当然不算到位 ⇒ 命中/滚轮仍失效");
                win.TickDrawers(0.15f);
                CheckNear(win.DrawerSlide(0), 0.5f, 0.01f, "推进 **0.15 秒**（`animationTime` = 0.3）⇒ 进度 **0.5**");
                CheckTrue(fltPanel.gameObject.activeSelf, "…滑动途中整栏**活着**（要看得见它在滑）");
                float midCx = MenuCheck.DesignPx(fltPanel).x;
                CheckNear(baseCx - midCx, 385f * 0.5f, 1f,
                          "…而且**真的挪了半个行程**（原位 → 左移 **192.5px** = 385 × 0.5，"
                          + "行程 = 原版 `−550 −(−165)`）");

                // ② 位移没停稳 ⇒ 命中区与滚轮都失效（**这一条量的是「真鼠标路」，不是我们自己那个布尔**）
                win.SetDrawerProgressForTest(0, 0.9f);
                CheckNear(baseCx - MenuCheck.DesignPx(fltPanel).x, 385f * 0.1f, 1f,
                          "进度 0.9 ⇒ 离原位还差 **10%**（左移 **38.5px**；位移 = `行程 × (1 − 进度)`、只动 x）");
                var rc = FindChild(fltPanel, "Cell_rar_common");
                var rcq = rc != null ? rc.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(rcq != null, "拿得到一格（否则下面两条等于没验）");
                if (rcq != null)
                {
                    float cx = MenuCheck.PointerFramePx(rcq.transform).x, cy = MenuCheck.PointerFramePx(rcq.transform).y;   // 指针帧（下面喂 ButtonAt / ScrollUnder）
                    // ★ 前置：这一格**现在还在屏内** —— 否则「点不到」是因为它滑出屏外、不是命中失效（等于没验）
                    CheckTrue(cx > 0f && cx < 1920f && cy > 0f && cy < 1080f,
                              $"★ 这一格此刻**还在屏内**（{cx:F0},{cy:F0}）—— 这条是下面「点不到」那个断言的前提");
                    var wbMid = pl3 != null ? pl3.ButtonAt(cx, cy) : null;
                    CheckTrue(wbMid == null,
                              "★ 位移没停稳 ⇒ **真命中路点不到它**（`PointerLayer.CollectHits` 只挑 "
                              + "`WindowButton.isActiveAndEnabled`，`Shell/PointerLayer.cs` 的 `WindowButton`）—— 拿到 "
                              + (wbMid == null ? "**null** ✓" : "`" + wbMid.name + "` = **还点得到**"));
                    var sMid = pl3 != null ? pl3.ScrollUnder(cx, cy) : null;
                    CheckTrue(sMid != win.FilterScroll,
                              "★ …滚轮也不该被这一列吃掉（`HitScroll` 那条 `Owner.activeInHierarchy` 的语义；"
                              + "拿到 " + (sMid == null ? "**null** ✓" : "`" + sMid.GetType().Name + "`"));
                }

                // ③ 推到目标 ⇒ 回原位、命中恢复（**同一格、同一条真命中路** —— 与 ② 正好两态）
                win.TickDrawers(0.2f);
                CheckNear(win.DrawerSlide(0), 1f, 0.001f, "再推 0.2 秒 ⇒ 收尾到 **1**（`MoveTowards` 夹住）");
                CheckNear(MenuCheck.DesignPx(fltPanel).x, baseCx, 0.5f, "…回到原位（位移是加在 `BasePos` 上的）");
                CheckTrue(win.DrawerSettled(0), "…到位 ⇒ 命中/滚轮**恢复**");
                if (rcq != null)
                {
                    var wbBack = pl3 != null
                        ? pl3.ButtonAt(MenuCheck.PointerFramePx(rcq.transform).x, MenuCheck.PointerFramePx(rcq.transform).y) : null;   // 指针帧
                    CheckTrue(wbBack != null && rc != null && wbBack.transform == FindChild(rc, "Hit"),
                              "★ 同一格现在**又点得到了**（拿到 " + (wbBack == null ? "**null**" : "`" + wbBack.name + "`") + "）");
                }

                // 收尾：还它一个「收着」的状态（后面那些断言/截图按这个来）
                win.ToggleFilters();
                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "再点一次 ⇒ 进度回 **0**（整栏滑出去）");
                CheckTrue(!fltPanel.gameObject.activeSelf, "…滑完就整块关掉（省渲染；原版那套也配 `SetActive`）");
                CheckTrue(!win.DrawerSettled(0), "…并且不再吃命中/滚轮");
                // 「收起」的语义 = **整栏真的在屏幕左外**（不是停在原位隐身）——
                //   判据要的是这一条，不是某个定点（原版那个 −550 是**父系里的绝对锚点值**；
                //   我们的**行程** = `hiddenPosition.x − originalAnchorPosition.x` = −385px，见 `ApplyDrawerSlide`）
                CheckTrue(MenuCheck.DesignPx(fltPanel).x + CollectionWindow.FltView.W * 0.5f < 0f,
                          "…而且此刻**整栏都在屏幕左外**（右缘 "
                          + (MenuCheck.DesignPx(fltPanel).x + CollectionWindow.FltView.W * 0.5f).ToString("F1")
                          + " < 0）—— 这才叫「滑出去」"
                          + "（行程 −385 时右缘 = −49.4；⚠️ 行程若再缩小到 < 335 这条就红）");
            }

            // ---------------- 🆕 2026-10-05（§三第29条 **A76①**）：**起滑那一下必须把命中区按下去** ----------------
            //   判据（原文 → `资料/已知的坑.md`「同一个洞会在下一个新入口重开」· `待办判据_阶段二与联机.md` §A25⑥）：
            //   `StartDrawerSlide` 的 **Play 那一支**原来调 `ApplyDrawerSlide(p, p.Slide)`（`force` 取默认 `false`）
            //   ⇒ **收起态起滑**时算出来的 `on=false` 与 `p.Interactive` 的现值**相等**（false）⇒
            //   撞上 `SetDrawerInteractive` 里「没变就不动」那条短路 ⇒ **整栏的 `WindowButton` 一下都不按**。
            //   只要哪个新入口建完漏了收尾那次 `force: true`（新按钮的出厂默认就是 `enabled=true`），
            //   症状就是「**滑入那 0.3 秒里真鼠标点得到**」——画面在滑、命中区却是活的（X3 的 R4 形态）。
            //   ⚠️ **批处理里 `Toggle*` 走的是「直接到位」那一支** ⇒ Play 这一支**一处断言都覆盖不到**
            //     （这正是 2026-10-04 那次「改成它就完了 ⇒ 没有断言能覆盖」的顾虑所在）⇒ 本段用**故障注入**补齐：
            //     ① 先把按钮按成脏态（= 模拟一个漏了 `force` 的新入口）② 再走**真正的起滑口**（`playLike: true`）。
            //   🔴 **真红法**（逐条推演过，别抄成「全红」）：把 `StartDrawerSlide(p, playLike)` 里那句
            //     `ApplyDrawerSlide(p, p.Slide, true)` 退回 `force: false` ⇒ 红的是 **②与④两条**
            //     （起滑那一下按钮**一个都不会被关**）；**③ 照样绿** —— 它量的是「推到 1 之后」，
            //     那时 `on` 变成 `true`、与 `Interactive` 的 `false` 不相等 ⇒ 本来就会真按一遍。
            //     把 `SetDrawerInteractive` 里 `wbs[i].enabled = on;` 删掉 ⇒ ②③④**全红**（那行是**唯一**
            //     按命中区的地方，建的那次 `force` 也走它）—— 说清是哪几条才有用。
            //   ⚠️ 与上面那三条**不是重复**：那三条量的是「建完之后**静止**时是关的」（`Toggle*` 那一支），
            //     这一段量的是「**起滑那一刻**即使状态被弄脏也会被按下去」——两条各管一个入口。
            Section("抽屉起滑口：**脏态起滑 ⇒ 命中区当场全关**（A76① · 只覆盖 Play 那一支）");
            {
                CheckTrue(!win.DeckFiltersOpen && !win.DrawerSettled(3),
                          "前置：Deck 抽屉此刻是**收起态**（起滑前 `Slide`/`SlideTarget` 都还是 0）");
                // ① 故障注入：把这一栏的命中区按回出厂默认（`enabled=true`），而 `Interactive` 仍是 false
                int stale3 = win.MakeDrawerHitsStaleForTest(3);
                var dfNode = FindChild(win.PageRoot(0), "Deck Filters");
                var dfWbs = dfNode != null
                    ? dfNode.GetComponentsInChildren<WindowButton>(true) : System.Array.Empty<WindowButton>();
                CheckTrue(stale3 > 0 && dfWbs.Length == stale3,
                          $"★ ① 注入的按钮数 = 真读到的 {dfWbs.Length} 个（`MakeDrawerHitsStaleForTest` 报 {stale3}）"
                          + " —— 数不上的话下面两条等于没验");
                int onA = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onA++;
                CheckTrue(dfWbs.Length > 0 && onA == dfWbs.Length,
                          $"★ ① 注入后 **{dfWbs.Length}** 个命中区**全开着**（实测 {onA}）—— 这就是「新入口漏了 `force`」的样子；"
                          + "而 `DrawerSettled(3)` 仍是 false ⇒ 脏态 = 「按钮开着 + 标志是关的」");
                // ② 真正的起滑口（Play 那一支；`Toggle*` 在批处理里永远走不到这里）
                win.StartDrawerSlideForTest(3, true, true);
                CheckNear(win.DrawerSlide(3), 0f, 0.001f,
                          "② 起滑这一刻进度**还是 0**（动画刚要开始 —— 下面那条量的是「滑入期间」而不是「到位后」）");
                CheckTrue(!win.DrawerSettled(3), "② …所以按定义还没到位");
                int onB = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onB++;
                CheckTrue(dfWbs.Length > 0 && onB == 0,
                          $"★ ② 起滑那一下 ⇒ 命中区**当场全关**（实测还开着 **{onB}** 个）—— "
                          + "这一条红了就说明 `StartDrawerSlide` 的 Play 分支把 `force` 丢了（A76①）");
                // ③ 对照组：推完动画 ⇒ 全回来（证明 ② 不是「一刀切关死」）
                win.TickDrawers(0.3f);
                CheckNear(win.DrawerSlide(3), 1f, 0.001f, "③ 推 0.3 秒（原版 `animationTime`）⇒ 进度 **1**（到位）");
                CheckTrue(win.DrawerSettled(3), "③ …到位了 ⇒ 命中/滚轮生效");
                int onC = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onC++;
                CheckTrue(dfWbs.Length > 0 && onC == dfWbs.Length,
                          $"★ ③ …而且 **{dfWbs.Length}** 个命中区**全回来**（实测 {onC}）"
                          + " —— ②③ 两态正好相反：写成恒关（或恒开）都过不了这两条");
                // ④ 起滑口是**共用**的（`StartDrawerSlide` 只有一份）—— Cards 那一栏同样得按下去
                //   ⚠️ 自己按页找节点（`PageRoot(1)` = Cards 页）—— `Card Filters` 这个名字**Styles 页也有一份**
                //     （同一个 builder 建的），别用「全树第一个」去找（那可能落到另一页的份上）。
                int stale0 = win.MakeDrawerHitsStaleForTest(0);
                var cfNode = FindChild(win.PageRoot(1), "Card Filters");
                var cfWbs = cfNode != null
                    ? cfNode.GetComponentsInChildren<WindowButton>(true) : System.Array.Empty<WindowButton>();
                CheckTrue(stale0 > 0 && cfWbs.Length == stale0,
                          $"★ ④ Card Filters 那一栏也注入成脏态（真读到 {cfWbs.Length} 个命中区，注入报了 {stale0} 个）");
                win.StartDrawerSlideForTest(0, true, true);
                int onD = 0; foreach (var b in cfWbs) if (b != null && b.enabled) onD++;
                CheckTrue(cfWbs.Length > 0 && onD == 0,
                          $"★ ④ 同一份实现 ⇒ 它起滑时也一样**全关**（实测还开着 {onD} 个）");
                // 还它一个「收着」的状态（后面那些断言/截图按这个来）
                win.ToggleFilters();                       // Cards 抽屉：Open 翻回 false ⇒ 批处理那一支直接到位
                win.ToggleDeckFilters();                   // Deck 抽屉：同上
                CheckTrue(!win.FiltersOpen && !win.DrawerSettled(0), "自检收尾：Cards 抽屉收回**收起**态");
                CheckTrue(!win.DeckFiltersOpen && !win.DrawerSettled(3), "自检收尾：Deck 抽屉收回**收起**态");
                int onE = 0; foreach (var b in dfWbs) if (b != null && b.enabled) onE++;
                CheckTrue(dfWbs.Length > 0 && onE == 0, "自检收尾：Deck 那一栏的命中区**又关回去**了");
            }

            // 🔴 层序：**窗口底图必须在页内容之下** —— `CardView` 的各层都落在默认队列 **3000**
            //    （全工程只有 SDF 那层显式设过 3000），而基类给 `Background` 的是 **3005**
            //    ⇒ 实拍抓到过：**整片卡池被底图盖住、画面全空，而所有矩形断言全绿**。
            //    判据只有这一条（量队列，量不到「谁盖谁」）。
            {
                var bgN = FindChild(root, "Background");
                var bq = bgN != null ? bgN.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(bq != null && bq.RenderQueue < 3000,
                          $"窗口底图的队列 = **{bq?.RenderQueue}**，必须 **< 3000**（`CardView` 的层都在 3000）"
                          + " —— 「底图盖住卡池」那个 bug 的判据");
                if (bq != null && win.CardsCells.Count > 0)
                {
                    // ⚠️ **`CardView` 的层不是 `ImageQuad`**（那套是卡面渲染器，走**自定义 mesh + MeshRenderer**）
                    //    ⇒ 队列要量**材质**的 `renderQueue`（渲染真值），量 `ImageQuad` 会得到空数组。
                    var mrs = win.CardsCells[0].GetComponentsInChildren<MeshRenderer>(true);
                    int minQ = int.MaxValue, cnt = 0; string qlist = "";
                    foreach (var mr in mrs)
                    {
                        if (mr.sharedMaterial == null) continue;      // **没材质的渲染器不画东西**，不算层
                        cnt++; minQ = Mathf.Min(minQ, mr.sharedMaterial.renderQueue);
                        qlist += mr.sharedMaterial.renderQueue + ",";
                    }
                    CheckTrue(cnt > 0 && minQ > bq.RenderQueue,
                              $"卡池的格**每一层**都在底图之上（底图 {bq.RenderQueue} · 格内最低队列 {minQ}；"
                              + $"{cnt} 层，实测 [{qlist}]）");
                }
            }

            // ---- `Empty Collection Warning`（§三 第 15 条 **第 50 行**；2026-09-24 补建）----
            // 判据 = 原版 `CollectionDisplay.RefreshCollection`：**过滤后为空** ⇒ `SetActive(true)`
            //        （反汇编 `工具/disasm_va.py` 读 `0x1815ECC5B`）。
            // 坐标 = **135.22,70.94 → 1970.01,1080** —— **比父还宽、左右都溢出**，原版就这样，别"修正"。
            {
                // ⚠️ **必须限定在 Cards 页里找** —— 四个页**各有一份**同名 `Empty Collection Warning`
                //    （原版就这样，矩形逐页不同，见 `CollectionWindow.BuildDeckList` 那条注释）。
                //    用 `FindChild(tabsRoot, …)` 会先撞上 **Deck 页**那一份（第一版就栽在这儿，差 15.33px）。
                var ew = FindChild(win.PageRoot(1), "Empty Collection Warning");
                CheckTrue(ew != null, "Cards 页有 `Empty Collection Warning` 这一件（原版**四页各一份**）");
                if (ew != null)
                {
                    CheckAt(ew, 135.22f, 1970.01f, 70.94f, 1080f, "`Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf, "没筛选 ⇒ **不显示**（原版出厂 `act=F`）");
                    // 🔴 **正例与反例必须成对** —— 只验「不显示」的话，「永远不显示」也能过
                    var f = DeckFilter.None;
                    f.Name = "zzz_本地没有这张卡_zzz";
                    win.UiSetCardFilter(f);
                    CheckTrue(ew.gameObject.activeSelf,
                              "筛到一个**空结果** ⇒ **显示**（原版判据 `filteredCollection.Count <= 0`）");
                    win.ClearCardFilters();
                    CheckTrue(!ew.gameObject.activeSelf, "`Clear filters` ⇒ 又关回去（判据跟着数据走）");
                }
            }

            // 两张截图：当前还停在 Cards 页 ⇒ 先拍它，再切回 Decks 拍第一张
            Shoot("02_收藏_Cards.png");
            win.tabButtons.Click(0);
            Shoot("01_收藏_Decks.png");

            // ---------------- Cosmetics 页：233 张卡背（正本 §五；A4 §二 + §2·1）----------------
            Section("Cosmetics 页：卡背网格（列数**算出来的** · 250×405 · 233 张）");
            win.tabButtons.Click(2);
            Check(win.CurrentTab, WindowTabType.CollectionCosmetics, "点第 3 键 ⇒ 切到 **Cosmetics** 页");
            CheckTrue(win.PageRoot(2) != null, "页节点 `Cardback Tab` 在（**卡面文案是 `COSMETICS`**）");
            Check(CollectionWindow.CosmoTotal, 233,
                  "卡背读数 = **233**（`Resources/Art/cardbacks/`；全在**工程外**导进来的，见正本 §七 ③）");
            // 🔴 列数是**算出来的**：A4 §2·1 原来写「`_segments=5` 是定值」⇒ **已就地更正**
            //    （`_controlSegmentSize=1` ⇒ `ConfigureColumnNumber` 按宽度覆盖 `_segments`，证据同 A3 §3·5）
            // 🔴 **2026-09-24 整套订正**：本页原来那组坐标（`168.27,85→1920,1080`、7 列）是
            //    **在 `Content Area` 的局部系里**算的 ⇒ 整页偏 (167.17, 70.94)、列数**多算一列**。
            //    真值（从 `Collection Menu Variant` 根一路走下来）见 `CollectionWindow.CosmoView` 的注释；
            //    根因是 `menu_rect.py` 父链那个坑（`资料/已知的坑.md`）。
            Check(CollectionWindow.CosmoCols, 6,
                  "列数 = **6** = floor(1584.56 ÷ 250)（⚠️ **不是** `_segments=5` —— 那个是死值；"
                  + "也**不是**上一版算的 7 —— 那是拿偏了 167.17px 的视口宽算出来的）");
            var cpage = win.PageRoot(2);
            var cbh = cpage != null ? FindChild(cpage, "Scroll View") : null;
            CheckAt(cbh, 335.44f, 1920.01f, 155.94f, 1080f, "`Scroll View`（卡背视口 **1584.56 × 924.06**）");
            CheckTrue(win.CosmoCells.Count > 0, $"画出了 {win.CosmoCells.Count} 格（视口外的不建 = 那套裁切）");
            if (win.CosmoCells.Count > 0)
            {
                var k0 = win.CosmoCells[0];
                // 内容**整体居中**：pad = (1584.56 − 6×250) / 2 = **42.285** ⇒ 首格中心 335.44+42.285+125
                CheckNear(MenuCheck.DesignPx(k0).x, 502.73f, 0.6f, "第 1 格中心 x = **502.73**（335.44 + pad 42.285 + 250/2）");
                CheckNear(MenuCheck.DesignPx(k0).y, 358.44f, 0.6f, "第 1 格中心 y = **358.44**（155.94 + 405/2）");
                var art = FindChild(k0, "Cardback");
                var qArt0 = art != null ? art.GetComponentInChildren<ImageQuad>() : null;
                var tex0 = qArt0 != null ? qArt0.Texture as Texture2D : null;
                // 🔴 **2026-10-18（A994③）就地订正（铁律 5）**：这两条原来写 **250 / 405**（=「铺满格」）——
                //    **错了**：原版那颗 `Cardback` 的 `Image` 带 **`m_PreserveAspect = 1`**
                //    （A4 §2·1 那行「Simple **preserveAspect**」）⇒ 实绘 **≠ 拉满**。
                // 🔴 **2026-10-19（B2）就地订正（铁律 5）**：期望值原来用 `InsetFit`（= 按**贴图自己**的比例内接）
                //    —— 那只是 uGUI 的**半套**。原版是**两段**：① 按 sprite 的 **`m_Rect`（707×1020）定框**；
                //    ② 贴图再按 **`textureRect/m_Rect` 缩** ⇒ **宽也会缩**（⛔ 不是「宽恒 250」）。
                //    算法/判据全文 → `Core/CardbackFace.cs`；期望值现算走 `CardbackDrawnPx`
                //    （⛔ 不调被测实现 `CardbackFace.Fit` / `PreserveAspectSize` —— 那是自证）。
                CheckTrue(tex0 != null, "（前提）第 1 格的卡背有贴图（`Texture` 取不到 ⇒ 下面两条无从算起）");
                if (tex0 != null)
                {
                    var fit0 = CardbackDrawnPx(tex0, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH);
                    CheckNear(Wpx(art), fit0.x, 0.6f,
                              $"格里的卡背宽 = **{fit0.x:F2}**（= 框宽 250 × `textureRect`/`m_Rect` 的**宽那一半**；"
                            + $"贴图 {tex0.width}×{tex0.height} ⇒ 贴图宽/707 = {tex0.width / 707f:F4}。"
                            + $"⛔ 「宽恒 250」是 B2 之前那一版（只做了第一段））");
                    CheckNear(Hpx(art), fit0.y, 0.6f,
                              $"格里的卡背高 = **{fit0.y:F2}**（= (250×1020/707) × 贴图高/1020；"
                            + "⛔ 不是 **405** —— 405 是**框**高、也不是「仅按贴图比例内接」那一档）");
                    // 🧨 **改坏法**：把 `RebuildCosmoCells` 里卡背那一跳的 `keepAspect` 改回 `false`
                    //    （或退回直调 `ImageQuad.Create` + `SetAspect(250/405)`）⇒ 上面两条立刻红（高变回 405）。
                    // 🆕 **2026-10-19（B2）**：画心**不居中** —— 原版按 sprite 的 `padding` 偏（uGUI 第二段的另一半）。
                    //   偏移 = `((padL−padR)/2, (padB−padT)/2) × (框宽/707)`；uGUI 的 y **向上** ⇒ 本仓画布
                    //   （y 向下）要**反号**。期望值走 `CardbackOffsetPx`（吃原版 `padding` 数据、独立复述算式）。
                    float odx, ody;
                    CheckTrue(CardbackOffsetPx(tex0, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH,
                                               out odx, out ody),
                              $"（前提）`{tex0.name}` 在 `Resources/Cardbacks.json` 里有 `padL/padR/padB/padT` 四列"
                            + "（表是旧版就没有 ⇒ 下面两条**空转**；跑 `python d:/4/Unity/工具/gen_cardbacks.py` 重生成）");
                    if (CardbackOffsetPx(tex0, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH, out odx, out ody))
                    {
                        // 第 1 格**整块在视口里**（上面 Wpx/Hpx 两条已经依赖这一点）⇒ 渲染中心 == 框中心 + 偏移
                        CheckNear(MenuCheck.DesignPx(qArt0.transform).x - MenuCheck.DesignPx(k0).x, odx, 0.6f,
                                  $"★ 卡背格里的画心**不居中**：横偏移 = **{odx:F2}**px"
                                + "（= (padL−padR)/2 × 250/707 ⇒ 没做这一半时恒为 **0**）");
                        CheckNear(MenuCheck.DesignPx(qArt0.transform).y - MenuCheck.DesignPx(k0).y, ody, 0.6f,
                                  $"★ ……纵偏移 = **{ody:F2}**px（= −(padB−padT)/2 × 250/707 —— **反号那一处**："
                                + "uGUI 的 y 向上、本仓画布 y 向下；没做这一半时恒为 **0**）");
                    }
                }
                CheckTrue(art != null && art.GetComponentInChildren<ImageQuad>() != null
                          && art.GetComponentInChildren<ImageQuad>().Texture != null,
                          "卡背**真的有贴图**（不是空图 —— 导没导错就看这一条）");

                // ---- 🆕 2026-09-26：卡背底下那层 **SDF**（原版 `Cardback Shadow SDF`）----
                // 逐值出处：`资料/普查产出_0923/A4_装饰页与驱动链.md:91` ——
                //   rect **-42.5,-70.87,337.5,550.8**（格式是 `x,y,w,h`，y 向下相对格左上）·
                //   锚点 (-0.17,-0.185)-(1.18,1.175) + sizeDelta (0,0)（拉伸）· `act=T` ·
                //   组件 = Image + `UIImageMaterialColorChanger`（换色即悬停高亮）·
                //   🔴 **`m_PreserveAspect = 1`**（同表那列写着「Simple preserveAspect」—— `A994③` 起照它做）。
                // 🔴 下面那条框的两维 + 底边（相对格上沿）= 上面那串字面量的直接换算，**独立于被测实现**：
                const float SdfBoxW = 337.5f, SdfBoxH = 550.8f, SdfBoxBottomRel = 479.925f;
                var sdf = FindChild(k0, "Cardback Shadow SDF");
                CheckTrue(sdf != null, "卡背格里有 **`Cardback Shadow SDF`** 那一层（原版两层的底那层）");
                if (sdf != null)
                {
                    // 🔴 **2026-10-08 就地订正（铁律 5）—— 这两条原来写 337.5 / 550.8，现在红在 479.93。**
                    //    根因**不是新缺陷**，是**旧断言拿「布局矩形」当「渲染矩形」量**：
                    //    · `k0` 是**第一排**那一格（上沿 = 视口上沿 155.94），而 SDF 比格大一圈、**上溢 70.875px**；
                    //    · 原版 `Cardback Display/Scroll View/Viewport` 上挂着 `RectMask2D`
                    //      （`m_Softness=(0,0)` · `m_Padding=(0,0,0,0)`，实读见
                    //      `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`）⇒ **那 70.875px 在原版里本来就画不出来**；
                    //    · A181（`Shell/CollectionWindow.cs` 的 `RebuildCosmoCells`）把这一层改走
                    //      `MenuDraw.Rect(…, CosmoView, …)` 之后，`WorldW/H` 量到的就是**截过的那一段**
                    //      （`MenuDraw.Rect` 把 quad 建在 `ClipRect` 求交后的矩形上）：
                    //      `CosmoView.y1` 155.94 → 格上沿 + 479.925 = 635.865 ⇒ 高 **479.925**（= 原来报的 479.93）。
                    //    ⇒ **实现是对的（更贴原版），错的是期望值**；⛔ 别为了这两条把 `CosmoView` 实参去掉
                    //      （那正是 A181 修掉的真偏离）。修法 = **两件事分开、各有一条能红的断言**：
                    //      ① 这一格断「**真被视口上沿截住**」；② 「布局矩形 = 337.5 × 550.8」改到
                    //      **整块落在视口里**的那一格上量（见下面那组「对照」）。
                    {
                        var sq0 = sdf.GetComponentInChildren<ImageQuad>();
                        var stx0 = sq0 != null ? sq0.Texture as Texture2D : null;
                        // 🔴 **2026-10-18（A994③）**：SDF 那层也带 `m_PreserveAspect = 1`
                        //    ⇒ 先把 337.5×550.8 的**框**按贴图比例内接（实测 `_SDF` 全部 100×130
                        //    ⇒ 337.5×438.75、上下各内缩 `pad`），**再**与视口求交。见 `RebuildCosmoCells`。
                        float sSdf = stx0 != null ? stx0.width / (float)stx0.height : 130f / 100f;
                        var sdfFit0 = InsetFit(sSdf, SdfBoxW, SdfBoxH);
                        float sdfPad0 = (SdfBoxH - sdfFit0.y) * 0.5f;      // 上下各内缩（= 56.025）
                        float sdfBottom0 = SdfBoxBottomRel - sdfPad0;      // 相对格上沿的**底边**
                        float sh0 = sq0 != null ? sq0.WorldH * 108f : -1f;
                        float st0 = sq0 != null ? MenuCheck.DesignPx(sq0.transform).y - sh0 * 0.5f : -1f;
                        // 第一排那一格的**格上沿 == 视口上沿** ⇒ 内接后整体仍 `-14.85` 上溢（< 0）
                        // ⇒ 露出来的就是 `sdfBottom0` 那一段（= 423.9；原来是拉满时的 479.925）。
                        CheckNear(sh0, sdfBottom0, 1.5f,
                                  $"★ 第一排那一格的 SDF **上溢被视口截住**：渲染高 = **{sdfBottom0:F3}**"
                                + $"（= 格上沿 + {sdfBottom0:F3}；原版框高 550.8 里「上溢 70.875 + 内接内缩 {sdfPad0:F3}」"
                                + "那两段都画不出来 —— 前一段是 `RectMask2D`，后一段是 `m_PreserveAspect = 1`）");
                        CheckNear(st0, CollectionWindow.CosmoView.y1, 0.8f,
                                  "…而且它**正好停在视口上沿 155.94**（= 原版 `RectMask2D` 那条边，不是「整层被关掉」）");
                    }
                    var qi = sdf.GetComponentInChildren<ImageQuad>();
                    CheckTrue(qi != null && qi.Texture != null
                              && qi.Texture.name.EndsWith("_sdf"),
                              "SDF 层贴的是**这张卡背自己的 `_SDF` 掩码**"
                            + "（`CosmeticItemCardback.GetCardBackSprites()` 成对返回；100×130.5 是距离场的本意）");
                    CheckTrue(qi != null && qi.GetComponent<MeshRenderer>().sharedMaterial != null
                              && qi.GetComponent<MeshRenderer>().sharedMaterial.shader != null
                              && qi.GetComponent<MeshRenderer>().sharedMaterial.shader.name
                                 == "Everguild/FX/Card Highlight And Shadow",
                              "SDF 层用的是**原版 shader** `Everguild/FX/Card Highlight And Shadow`"
                            + "（材质值照原版 `Card Backs SDF`，不是 shader 默认值 —— 否则会多一圈白框）");
                    // 🔴 **SDF 必须在卡背底下**：两层给的是**两个渲染队列**（同一个队列里谁盖谁不可控，踩过三次）
                    var qArt = art != null ? art.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(qi != null && qArt != null && qi.RenderQueue < qArt.RenderQueue,
                              $"SDF 的渲染队列**低于**卡背（{CollectionWindow.QPageSdf} < {CollectionWindow.QPageRow}）"
                            + "—— 同队列排不出稳定次序");
                }
                // ---- 对照（🆕 2026-10-08）：**整块落在视口里**的那一格 ⇒「渲染矩形 == 内接矩形」----
                //  上面那一格（`k0`）只能证明「上溢被截住」，量不到内接矩形本身 ⇒ 判据要挪到一格**四边都不越界**的：
                //  第 2 排第 2 列（`CosmoCells[CosmoCols + 1]`，`CosmoCellRect` 是**行优先**、视口外的格不建）：
                //    · SDF **框**相对格左上 = `−42.5, −70.875 → +295, +479.925`（= 宽 337.5 / 高 550.8）；
                //      🔴 `A994③` 起框内还要**按贴图比例内接**：337.5×438.75、上下各内缩 56.025
                //      ⇒ 实绘相对格左上 = `−42.5, −14.85 → +295, +423.9`；
                //    · 该格上沿 = `CosmoView.y1` 155.94 + 格高 405 = **560.94** ⇒ SDF 上沿 **546.09** > 视口上沿 ✓；
                //      左沿 = 335.44 + `CosmoPadX` 42.285 + 250 = **627.725** ⇒ SDF 左沿 **585.225** > 视口左沿 335.44 ✓；
                //      下沿 560.94 + 423.9 = **984.84** < 1080 ✓ · 右沿 627.725 + 295 = **922.725** < 1920.01 ✓。
                //  **改坏法**：把 `RebuildCosmoCells` 里那句 `sr` 的 337.5 / 550.8 改错 ⇒ 红；
                //   **把 SDF 那一跳的 `keepAspect` 改回 `false`（= 拉满）⇒ 高变回 550.8 ⇒ 红**。
                {
                    var sdfFull = win.CosmoCells.Count > CollectionWindow.CosmoCols + 1
                        ? FindChild(win.CosmoCells[CollectionWindow.CosmoCols + 1], "Cardback Shadow SDF") : null;
                    CheckTrue(sdfFull != null,
                              "（对照 · 前提）第 2 排那一格的 `Cardback Shadow SDF` 也在（它整块落在视口里 ⇒ 量得到实绘矩形）");
                    if (sdfFull != null)
                    {
                        var qSf = sdfFull.GetComponentInChildren<ImageQuad>();
                        var tSf = qSf != null ? qSf.Texture as Texture2D : null;
                        CheckTrue(tSf != null, "（对照 · 前提）那一格的 SDF 贴图取得到（100×130.5 那批）");
                        var fitSf = InsetFit(tSf != null ? tSf.width / (float)tSf.height : 130f / 100f,
                                             SdfBoxW, SdfBoxH);
                        CheckNear(Wpx(sdfFull), fitSf.x, 1.5f,
                                  $"★ 对照（整块不越界的格）：SDF 层宽 = **{fitSf.x:F2}**（= 框宽 337.5 —— 贴图比框「宽」"
                                + "⇒ 宽定、宽**不变**；比 250 的卡背大一圈 —— 露出来的就是落地感）");
                        CheckNear(Hpx(sdfFull), fitSf.y, 1.5f,
                                  $"★ 对照（整块不越界的格）：SDF 层高 = **{fitSf.y:F2}**（⛔ **不是 550.8** —— 那是**框**高；"
                                + "`m_PreserveAspect = 1` ⇒ 高缩成 `337.5 ÷ 比例`。"
                                + "🔴 **反证**：拉满时 x 缩放 337.5/100 = **3.375** 而 y 缩放 550.8/130 = **4.237**（**非等比**、"
                                + "距离场被竖向拉长）；内接后两轴**都是 3.375**）");
                    }
                }
                // ---- 🔴 2026-10-18（A994③）：**逐格**验「两层都没被拉伸」（原版两颗 `Image` 都带 `m_PreserveAspect = 1`）----
                //  判据 = **渲染宽高比 == 贴图自己的宽高比**（uGUI `Image` 的直接后果；
                //  「图没被拉伸」这件事与「我们怎么摆框」无关 ⇒ 它比「量宽/高」更靠近原版语义）。
                //  ⚠️ **2026-10-19（B2）复核**：两段式之后渲染比例 = **`textureRect` 的比例**，而 `textureRect`
                //     与「我们那张裁片 PNG」的比例实测最大只差 **1.42e-4**（233 张全量现读）⇒ 这条的 **2e-3 容差照旧成立**，
                //     **一个字不用改**（它是「没被拉伸」的看门人，与「该多大」是两件事）。
                //  ⛔ **不写死格号**：233 张比例跨 **0.6188~0.7652**，每格偏离量都不同 —— 写死一格就是赌假绿。
                //  做法 = ① 全量扫（可量的格逐格比比例）② 按**偏离量**挑「两段式 ≠ 老口径」差得**最远**的那一张做精确断言。
                //  被视口切过的格（滚动到边上那几格）渲染矩形本来就是「裁剩那块」的比 ⇒ 跳过（归 A181 那组管）。
                {
                    var vpA = CollectionWindow.CosmoView;
                    int cellsOk = 0, stretched = 0, worstI = -1; float worstDev = -1f;
                    foreach (var cn in win.CosmoCells)
                    {
                        if (cn == null || !cn.name.StartsWith("CollectionCosmetic_")) continue;
                        int ci;
                        if (!int.TryParse(cn.name.Substring("CollectionCosmetic_".Length), out ci)) continue;
                        var ca = FindChild(cn, "Cardback");
                        var cq2 = ca != null ? ca.GetComponentInChildren<ImageQuad>() : null;
                        var ct = cq2 != null ? cq2.Texture as Texture2D : null;
                        if (ct == null || ct.height <= 0) continue;
                        var lr = win.CosmoScroll != null
                            ? win.CosmoScroll.Shift(CollectionWindow.CosmoCellRect(ci))
                            : CollectionWindow.CosmoCellRect(ci);
                        if (lr.x1 < vpA.x1 - 0.5f || lr.x2 > vpA.x2 + 0.5f
                            || lr.y1 < vpA.y1 - 0.5f || lr.y2 > vpA.y2 + 0.5f) continue;   // 被视口切过 ⇒ 跳过
                        cellsOk++;
                        float sc = ct.width / (float)ct.height;
                        float rr = (cq2.WorldW * 108f) / Mathf.Max(1e-6f, cq2.WorldH * 108f);
                        // 🔴 **这一条对旧写法（一律 `SetAspect(250/405)` = 0.61728）必红** ——
                        //    本仓 233 张的比例**全都 > 0.61728**（最小 0.6188 ≥ 0.61728 + 0.0015），一张都对不上。
                        if (Mathf.Abs(rr - sc) > 2e-3f) stretched++;
                        // 🔴 **2026-10-19（B2）改挑法**：原来按「实绘高离框高 405 多远」挑 —— 那是**老口径**
                        //    （`250 ÷ 贴图比例`）的度量。两段式之后「高」的极值不再贴着 405
                        //    （最大 = `250 × 最大 texRectH / 707` = **360.68**），挑出来那一格**未必**是
                        //    「新老两态差得最远」的 ⇒ 下面的精确断言会**看着绿其实空转**。
                        //    改成按**宽**挑：两段式的画心宽 = `250 × texRectW/707`，而老口径/拉伸**恒 250**
                        //    ⇒ **贴图越窄、两态差得越远**（`texRectW` 用资产自己的 `ct.width` 顶，差 ≤0.5px）。
                        float dv = 250f - 250f * ct.width / 707f;
                        if (dv > worstDev) { worstDev = dv; worstI = ci; }
                    }
                    CheckTrue(cellsOk >= 6,
                              $"（前提）有 {cellsOk} 格**整块落在视口里**、可以量实绘矩形（⛔ < 6 格则下面两条等于没验）");
                    Check(stretched, 0,
                          $"★ **卡背格逐格比「渲染宽高比 vs 贴图自己的宽高比」**（可量 {cellsOk} 格）"
                        + " —— 一格不等就是**拉伸**（原版 `Cardback` 那颗 `Image` 带 `m_PreserveAspect = 1`）");
                    CheckTrue(worstI >= 0 && worstDev > 5f,
                              $"（前提）挑得到一格「两段式 ≠ 老口径（宽恒 250）」差 > 5px 的探针 —— 实得 {worstDev:F2}px"
                            + "（可量那几格里最小的贴图宽 < 692 ⇒ 否则下面两条**空转**，⛔ 别放它过去）");
                    if (worstI >= 0)
                    {
                        var wcell = FindChild(cpage, "CollectionCosmetic_" + worstI);
                        var wart = wcell != null ? FindChild(wcell, "Cardback") : null;
                        var wq = wart != null ? wart.GetComponentInChildren<ImageQuad>() : null;
                        var wt = wq != null ? wq.Texture as Texture2D : null;
                        CheckTrue(wt != null, "（前提）按偏离量挑出来的那一格贴图取得到");
                        if (wt != null)
                        {
                            var fitW = CardbackDrawnPx(wt, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH);
                            CheckNear(Wpx(wart), fitW.x, 0.6f,
                                      $"★ 两态差得最远的那一格（`{wt.name}` · 第 {worstI + 1} 张 · 贴图 {wt.width}×{wt.height}）"
                                    + $"画出来的**宽** = **{fitW.x:F2}**（= 250 × {wt.width}/707；"
                                    + $"**老口径 / 拉满时恒是 250**，差 {250f - fitW.x:F2}px）");
                            CheckNear(Hpx(wart), fitW.y, 0.6f,
                                      $"★ ……**高** = **{fitW.y:F2}**（= (250×1020/707) × {wt.height}/1020；"
                                    + $"**拉满时是 {CollectionWindow.CosmoCellH}**，差 "
                                    + $"{CollectionWindow.CosmoCellH - fitW.y:F2}px）");
                            // 🔴 **2026-10-19（B2）补第二条灭自证**：老口径（只做第一段 / 拉伸）时
                            //    **宽恒 == 250**⇒ 这一条与上面那条宽断言**结构上不可能同时绿**。
                            CheckTrue(Wpx(wart) < CollectionWindow.CosmoCellW - 5f,
                                      $"★ 灭自证：画心宽 {Wpx(wart):F2} **严格小于**框宽 {CollectionWindow.CosmoCellW}"
                                    + $"（实得 {Wpx(wart):F2}，差 {CollectionWindow.CosmoCellW - Wpx(wart):F2}px）"
                                    + " —— 老口径/拉满时它**恒等于**框宽，两条不可能同时绿");
                            // 🧨 **灭自证**：只要退回「拉满」（`keepAspect: false` / `SetAspect(250/405)`），
                            //    「画出来的高」就**恒 == `CosmoCellH`** ⇒ 这一条与上一条**不可能同时绿**
                            //    （挡的是「把实现和期望值一起改回旧写法」：期望值是**从贴图资产现算**的，改不回去）。
                            CheckTrue(Hpx(wart) < CollectionWindow.CosmoCellH - 10f,
                                      $"★ 灭自证：「两段式后的高」**严格小于框高 {CollectionWindow.CosmoCellH}**"
                                    + $"（实得 {Hpx(wart):F2}，差 {CollectionWindow.CosmoCellH - Hpx(wart):F2}px）"
                                    + " —— 拉满时它**恒等于**框高，两条不可能同时绿");
                        }
                    }
                }
            }
            // ============================================================ 🆕 2026-10-17（界面线 T18/T25）
            // 点一张卡背 ⇒ **`Item Display Window`**（T18 建的那扇窗）。
            // 🔴 **判据**：`CardbackCollectionTab__OnItemSelected.c:17-24` = `ItemDisplayWindowContext(item,1)` →
            //   `WindowsManager.OpenWindow<ItemDisplayWindow>`（不猜）。
            // 🔴 **期望值全部独立算**：标题 / 描述 / lore / 图 都从 `Resources/*.json` **原样读**（`CbRawFile` /
            //   `OfficialFile`），⛔ 不调被测实现 `ItemDisplayWindow.ForCardback` 的口（那是自证）。
            Section("T18/T25：点一张卡背 ⇒ `Item Display Window`（大图 + 信息面板 + 风味条）");
            {
                var cell0 = win.CosmoCells.Count > 0 ? win.CosmoCells[0] : null;
                CheckTrue(cell0 != null, "（前提）卡背页画出了至少一格");
                if (cell0 != null)
                {
                    var art0 = FindChild(cell0, "Cardback");
                    var qArt0 = art0 != null ? art0.GetComponentInChildren<ImageQuad>() : null;
                    var tex0 = qArt0 != null ? qArt0.Texture as Texture2D : null;
                    CheckTrue(tex0 != null, "（前提）第 1 格的卡背贴图取得到（要先知道点的是**哪一张**）");
                    if (tex0 != null)
                    {
                        var cbTa = Resources.Load<TextAsset>("Cardbacks");
                        var cbF = cbTa != null ? JsonUtility.FromJson<CbRawFile>(cbTa.text) : null;
                        CbRawItem row = null;
                        if (cbF != null && cbF.items != null)
                            foreach (var it in cbF.items)
                                if (it != null && it.name == tex0.name) { row = it; break; }
                        CheckTrue(row != null, $"（前提）`{tex0.name}` 在 `Resources/Cardbacks.json` 里");
                        if (row != null)
                        {
                            int ci0 = 0;
                            if (cell0.name.StartsWith("CollectionCosmetic_"))
                                int.TryParse(cell0.name.Substring("CollectionCosmetic_".Length), out ci0);
                            // 格矩形走**当前滚动后的那一份**（同上面「整块在视口里」那组：`CosmoScroll.Shift`）
                            var cellR = win.CosmoScroll != null
                                ? win.CosmoScroll.Shift(CollectionWindow.CosmoCellRect(ci0))
                                : CollectionWindow.CosmoCellRect(ci0);
                            var pl = PointerLayer.Instance;
                            CheckTrue(pl != null, "（前提）指针层在（`WindowsManager.EnsureHost` 建的）");
                            // 先问一句「这一点上到底是谁吃到了」—— 若前几段漏关了别的窗（层更高），
                            // 下面那条「窗开出来了」会红在**错误的原因**上 ⇒ 这一句把原因钉住。
                            var hit0 = FindChild(cell0, "Hit");
                            // 🆕 2026-10-20（A1350 第二批）：`cellR` 是**设计 px**（`CosmoCellRect` + `MenuScroll.Shift`
                            //   都活在设计帧）⇒ 喂指针前换到指针帧（单位缩放下逐位不变）。`owner` = 那一格自己。
                            var cellP = MenuCheck.PointerFramePxFromDesign(
                                new Vector2((cellR.x1 + cellR.x2) * 0.5f, (cellR.y1 + cellR.y2) * 0.5f), cell0);
                            var at0 = pl != null ? pl.ButtonAt(cellP.x, cellP.y) : null;
                            CheckTrue(hit0 != null && at0 != null && at0.gameObject == hit0.gameObject,
                                      "★ 卡背格中心吃到的是**它自己的命中区**（没被别的窗顶掉）");
                            bool clicked = pl != null && pl.ClickAt(cellP.x, cellP.y);
                            CheckTrue(clicked, "★ 点卡背格：`PointerLayer.ClickAt` 命中并派发了 onClick（真路径）");

                            var idw = ItemDisplayWindow.LastOpened;
                            CheckTrue(idw != null && idw.CurrentState == WindowState.Open,
                                      "★ 点一张卡背 ⇒ 开出 **`Item Display Window`**（原版 `CardbackCollectionTab.OnItemSelected`；"
                                    + "不再是 `NotifyNotBuilt`）");
                            if (idw != null && idw.CurrentState == WindowState.Open)
                            {
                                // ---- 窗参（MB `MonoBehaviour_-3926313906056799325.json` 实读）----
                                Check(idw.type, WindowType.Popup, "窗参 `type` = **1**（Popup，MB 实读）");
                                Check(idw.placement, WindowsPlacement.Popup, "窗参 `windowsPlacement` = **15**（Popup）");
                                CheckTrue(idw.closeOnEsc, "窗参 `closeOnESC` = **1**");
                                CheckNear(idw.extraScaleSmallScreen, 1f, 1e-6f, "窗参 `extraScaleSmallScreen` = **1.0**");
                                // 🆕 **2026-10-19（`P2-7`）**：原版 `Card Display/FlavourTextBG/LoreText` 的
                                //   `m_lineSpacing = 5`（prefab 实读）。🧨 改坏法：删掉 `ItemDisplayWindow`
                                //   里 `MenuDraw.SetLineSpacing(_lore, 5f)` 那一句 ⇒ 这条红（TMP 默认 0）。
                                {
                                    var lt = idw.LoreLabel != null
                                           ? idw.LoreLabel.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
                                    CheckTrue(lt != null, "（前提）`LoreText` 建出来了");
                                    if (lt != null)
                                        CheckNear(lt.lineSpacing, 5f, 1e-4f,
                                                  "★ `LoreText` 行距 = 原版 `m_lineSpacing` **5**");
                                }

                                // ---- 标题 = 官方名 `Cardbacks/<SO 名>`（独立读 official_names.json）----
                                string expTitle = null;
                                var onTa = Resources.Load<TextAsset>("official_names");
                                var onF = onTa != null ? JsonUtility.FromJson<OfficialFile>(onTa.text) : null;
                                if (onF != null && onF.entries != null)
                                    foreach (var e in onF.entries)
                                        if (e != null && e.k == "Cardbacks/" + row.so)
                                        {
                                            expTitle = Loc.Current == AvailableLanguages.Chinese
                                                ? (string.IsNullOrEmpty(e.zh) ? e.en : e.zh)
                                                : (string.IsNullOrEmpty(e.en) ? e.zh : e.en);
                                            break;
                                        }
                                CheckTrue(expTitle != null,
                                          $"（前提）`Cardbacks/{row.so}` 在官方数据面词条表里（这张卡背有官方名）");
                                if (expTitle != null)
                                    CheckText(TextOf(FindChild(idw.transform, "Title")), expTitle,
                                              $"★ `Title` = 官方名（键 `Cardbacks/{row.so}`；期望值由自检**独立读 json** 算出）");

                                // ---- Description = `itemDescriptionKey`（卡背 = `Item_Description/CardbackGeneric`）----
                                CheckTrue(Loc.HasEntry("Item_Description/CardbackGeneric"),
                                          "（前提）`Item_Description/CardbackGeneric` 在词条表里");
                                CheckText(TextOf(FindChild(idw.transform, "Description")),
                                          Loc.T("Item_Description/CardbackGeneric"),
                                          "★ `Description` = 原版 `itemDescriptionKey` 那一格"
                                        + "（`ItemInformationPanel.Initialize` 的 `descriptionText ← I2(item+0x18)`）");

                                // ---- Type 留空（原版 = I2(options.TypeString)，那份配置资产本地不存在）----
                                CheckText(TextOf(FindChild(idw.transform, "Type")), "",
                                          "`Type` **留空**（原版 = I2(`ItemDrawerOptions.TypeString`)，而 `ItemDrawerConfig`"
                                        + " 资产在解包树里不存在 ⇒ 判据不足，实现出声、⛔ 不编）");

                                // ---- Lore = `Cosmetic_Lore/<uniqueId>`；没有 ⇒ 整块 FlavourTextBG 关 ----
                                var expLore = Loc.LoreByUniqueId(row.uniqueId);
                                var fb0 = FindChild(idw.transform, "FlavourTextBG");
                                CheckTrue(fb0 != null, "（前提）`Card Display/FlavourTextBG` 在");
                                if (fb0 != null)
                                {
                                    if (!string.IsNullOrEmpty(expLore))
                                    {
                                        CheckText(TextOf(FindChild(fb0, "LoreText")), expLore,
                                                  $"★ `LoreText` = `Cosmetic_Lore/{row.uniqueId}`（期望值独立取）");
                                        CheckTrue(fb0.gameObject.activeSelf, "有 lore ⇒ 整块 `FlavourTextBG` **亮着**");
                                    }
                                    else
                                        CheckTrue(!fb0.gameObject.activeSelf,
                                                  "没有 lore ⇒ 整块 `FlavourTextBG` **关着**"
                                                + "（原版 `ItemDisplayWindow__Open.c` 的 `SetActive(!IsNullOrWhiteSpace)`）");
                                }

                                // ---- 图 = **点的那一张**卡背 ----
                                var holder0 = FindChild(idw.transform, "Item Holder");
                                var himg0 = holder0 != null ? FindChild(holder0, "Image") : null;
                                var hq0 = himg0 != null ? himg0.GetComponentInChildren<ImageQuad>() : null;
                                CheckTrue(hq0 != null && hq0.Texture != null && hq0.Texture.name == tex0.name,
                                          "★ `Item Holder` 里画的是 **`" + tex0.name + "`**（点的那张，不是别的）");

                                // ---- 点暗底 ⇒ 关窗（原版靠 `ItemDisplayWindow.OnEnable` 把 Close 挂到
                                //      `BackgroundCloseButton.onClick`；prefab 里 `window` PPtr 是 0）----
                                // 🆕 2026-10-20（A1350 第二批）：(5,5) 是**设计 px**（屏幕左上角那一档）⇒ 换到指针帧。
                                //   `owner` = 压暗层那颗命中区（它是窗根的直接子件 ⇒ 乘的是窗根那一级缩放）。
                                var shadeT = FindChild(idw.transform, "BackgroundHit");
                                var shadeP = MenuCheck.PointerFramePxFromDesign(new Vector2(5f, 5f),
                                                 shadeT != null ? shadeT : idw.transform);
                                CheckTrue(pl.ClickAt(shadeP.x, shadeP.y), "★ 点暗底（设计 (5,5) → 指针帧）：`PointerLayer.ClickAt` 命中并派发");
                                Check(idw.CurrentState, WindowState.Closed,
                                      "★ 点暗底 ⇒ **窗关了**（`BackgroundHit` = 原版那条「点窗外关窗」）");
                            }
                        }
                    }
                }
            }
            // 页头（A3 那条「每页自己的实例值」：本页 35/33，**异画页是 42**）
            // 🔴 **2026-10-18（第三轮整改 · 审查 P4）改「随语档」**：本页页头标题从第三轮起走词条
            //   （`Shell/CollectionWindow.BuildCosmeticsPage` 的 `Loc.T("MenuCollection/Label/Cosmetics")`，
            //   键 = 原版那颗 `label` 的 `mTerm` 原文）⇒ 宿主跑中文档时写死的 `"Your cosmetics collection"` 必红。
            CheckTrue(Loc.HasEntry("MenuCollection/Label/Cosmetics"),
                      "（前提）词条 `MenuCollection/Label/Cosmetics` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
            CheckText(TextOf(FindChild(cpage, "Header Label")), Loc.T("MenuCollection/Label/Cosmetics"),
                      "页头标题 = `Loc.T(\"MenuCollection/Label/Cosmetics\")`（随语档；"
                      + "原版 TMP 原文 `Your cosmetics collection` · 原版 fs38）");
            // 🔴 **2026-10-18（A891 的续 · 续做 B）改「随语档」**：页头那颗字从本批起走词条
            //   （`Shell/CollectionWindow.BuildFilterHeader` 的 `Loc.T("MenuDeck/Filters/Filters")` —— 与
            //   卡组编辑窗页头那颗**同一条键**）⇒ 宿主跑中文档时写死的 `"Filters"` 必红。
            //   原文 = 原版那颗 TMP 的 `m_text`（`Filters`），留在 `Loc` 的英文列里（中文列 = 原版实拍「过滤器」）。
            CheckTrue(Loc.HasEntry("MenuDeck/Filters/Filters"),
                      "（前提）词条 `MenuDeck/Filters/Filters` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
            CheckText(TextOf(FindChild(cpage, "Filters Label")), Loc.T("MenuDeck/Filters/Filters"),
                      "页头 `Filters` 文案在（键 `MenuDeck/Filters/Filters`，随语档；"
                      + "原版 **fs35** auto 10-35 —— ⚠️ 异画页同名的是 42）");
            // 🔴 **2026-10-18（A891 的续）改「随语档」**：页头那颗字从本批起走词条
            //   （`Shell/CollectionWindow.BuildFilterHeader` 的 `Loc.T("MenuDeck/Filters/ClearFilters")`）
            //   ⇒ 宿主跑中文档时写死的 `"Clear filters"` 必红。原文 = 原版那颗 TMP 的 `m_text`（`Clear filters`），
            //   留在 `Loc` 的英文列里。⛔ 下面找底图那句（节点名 `"Clear filters"`）**不动**。
            CheckTrue(Loc.HasEntry("MenuDeck/Filters/ClearFilters"),
                      "（前提）词条 `MenuDeck/Filters/ClearFilters` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名）");
            CheckText(TextOf(FindChild(cpage, "Clear filters Text")), Loc.T("MenuDeck/Filters/ClearFilters"),
                      "页头 `Clear filters` 文案在（键 `MenuDeck/Filters/ClearFilters`，随语档；"
                      + "原版 **fs33** auto 10-33）");
            // ============================================================ 🆕 **2026-10-11（A1496）：
            //   `Clear filters` 的【显隐】** —— 原版空筛选时这颗钮**不在画面上**。
            //   判据：`CollectionFilterController` 的 `hiddenPosition = (−550, 0)`（卡组编辑窗那一份实例逐字段实读），
            //   隐藏方式是**平移**；用户实拍参考图那两帧上它也确实不在。✅ **用户 2026-10-11 点名要做**。
            //   ⚠️ **我们走 `SetActive`、不走原版那条平移**（同卡组编辑窗口径）：我们的命中是**静态 `PxRect`**、
            //   不跟着节点走 ⇒ 平移了但不关闸 = 「藏起来却还点得到」= 静默失败（本仓红线）。
            //   🔴 **这里是【Cards 页】**（`PageRoot(1)` + `UiSetCardFilter`）；`cpage` 是**卡背页**，
            //   它的抽屉只有 Army/Owned 两行、没有 `UiSetCosmoFilter` 这个口 ⇒ 不拿它断。
            {
                var cardPage = win.PageRoot(1);
                var cfQ = FindChild(cardPage, "Clear filters");
                var cfT = FindChild(cardPage, "Clear filters Text");
                var cfH = FindChild(cardPage, "ClearFiltersHit");
                CheckTrue(cfQ != null && cfT != null && cfH != null,
                          "（前提）Cards 页 `Clear filters` **三件**（底图 / 字 / 命中区）都建了");
                // ① 出厂态（一个筛选都没设）⇒ 三件**全不激活**（`FindChild` 收未激活件 ⇒ 取得到、只是关着）
                CheckTrue(!cfQ.gameObject.activeSelf && !cfT.gameObject.activeSelf && !cfH.gameObject.activeSelf,
                          "★ A1496 空筛选 ⇒ `Clear filters` **底图 / 字 / 命中区三件全关**（原版是平移到 −550）");
                // ② **灭自证**：只断「都关着」的话，把两边**一起改成永远关**也全绿
                //    ⇒ 必须再断一次「设了筛选之后三件一起开」。
                var f1 = DeckFilter.None; f1.Faction = "saimhann";
                win.UiSetCardFilter(f1);
                CheckTrue(cfQ.gameObject.activeSelf && cfT.gameObject.activeSelf && cfH.gameObject.activeSelf,
                          "★ A1496 设了筛选 ⇒ **三件一起打开**（灭自证：两边永远关 / 两边永远开都会红一条）");
                // ③ 摆回基线（本仓规矩：每一格先摆回基线，别把状态漏给后面那些断言）
                win.ClearCardFilters();
                CheckTrue(!cfQ.gameObject.activeSelf && !cfH.gameObject.activeSelf,
                          "★ A1496 清空筛选 ⇒ **又关回去**（判据跟着数据走，不是一次性摆的）");
            }
            // ============================================================ 🆕 **2026-10-11（A1503 ①）：
            //   卡组格右下那颗 `Game Mode Icon`（**绿色双箭头** / 经典的红枪）**
            // 判据（原版 prefab 实读）：`Game Mode Icon` 84.5×85.75 @作者系 (170.5, 273.7)，
            //   `enabled = (icon != null)`；遭遇 = `40k_gamemode_icon_skirmish` · 经典 = `40k_gamemode_icon_classic`。
            // 🔴 **它原来整层没建** —— `MenuDraw.DeckCell` 那一支早写好了，只是 `CollectionWindow.BuildDeckCell`
            //   **从来没传 `gameMode`**（形参默认 null ⇒ 静默不画）。所以这条断言断的正是「**接上了没有**」：
            //   逐格按 `GameModeIconFile` 反查「该有图的格子是不是真有那颗节点」。
            // ⚠️ 「至少有一套该有图」是**前提**（否则整条空转 = 假绿）；本轮实测那批测试卡组里有模式非 0 的。
            {
                var deckPage = win.PageRoot(0);
                int built = 0, want = 0, miss = 0;
                for (int i = 0; i < CollectionData.DeckCount(); i++)
                {
                    // ⚠️ **只在【建出来了的那几格】上断** —— `RebuildDeckCells` 有一句
                    //   `if (DeckScroll != null && !DeckScroll.Intersects(r)) continue;`（视口外的格子根本不建），
                    //   拿没建的格子去数「缺图」会数出一串假账（第一版就这么错的：16 该有 / 4 报缺）。
                    var cell = FindChild(deckPage, "CollectionDeck_" + i);
                    if (cell == null) continue;
                    built++;
                    var di = CollectionData.DeckAt(i);   // ⚠️ `DeckInfo` 是 **struct** ⇒ 判不了 null
                    if (MenuDraw.GameModeIconFile(di.GameMode) == null) continue;
                    // ⚠️ **视口把下半截裁掉的那几格不参与** —— 我们引擎的裁切是「框外的层**不建节点**」
                    //    （`Rect(..., clipR)` 直接跳过）。
                    //    🔑 探针用 **`Deck Name`**（作者系 y **344.2**，比模式图标那排 y 273.7 **更低**）
                    //      —— 名字画得出来 ⇒ 上面那排图标一定在框内（**严格一档的闸**）。
                    //    ⚠️ 第一版没写闸：第 12–15 号（第 3 行、只露一半）被算成「缺图」= **4 条假账**；
                    //      第二版拿 `Faction` 当闸 ⇒ 只剩 1 格（测试卡组大多没有督军 ⇒ 左下那颗本来就不建），
                    //      **闸太松 = 断言快空转** ⇒ 换成名字这一档。
                    if (FindChild(cell, "Deck Name") == null) continue;
                    want++;
                    if (FindChild(cell, "Game Mode Icon") == null) miss++;
                }
                CheckTrue(want > 0,
                          "（前提）**屏幕上建出来的**格子里至少有一格该有图标（⛔ 0 格 ⇒ 下一条空转 = 假绿）");
                if (miss > 0)
                {
                    var sb = new System.Text.StringBuilder("[Collection]    【诊断】缺 `Game Mode Icon` 的格子：");
                    for (int i = 0; i < CollectionData.DeckCount(); i++)
                    {
                        var cell = FindChild(deckPage, "CollectionDeck_" + i);
                        if (cell == null) continue;
                        var di = CollectionData.DeckAt(i);
                        if (MenuDraw.GameModeIconFile(di.GameMode) == null) continue;
                        if (FindChild(cell, "Deck Name") == null) continue;     // 与上面同一道闸
                        if (FindChild(cell, "Game Mode Icon") != null) continue;
                        sb.Append(" #").Append(i).Append("(mode=").Append(di.GameMode)
                          .Append(",art=").Append(MenuDraw.GameModeIconFile(di.GameMode))
                          .Append(",active=").Append(cell.gameObject.activeSelf).Append(")");
                    }
                    Debug.Log(sb.ToString());
                }
                Check(miss, 0, "★ A1503① 每一格卡组右下都有 `Game Mode Icon`（原版 `enabled = icon != null`；"
                               + "本轮实测：建出来 " + built + " 格、其中该有图标的 " + want + " 格）");
            }
            // ============================================================ 🆕 **2026-10-11（A1503 ②③⑤⑥）**
            //   ② `Deck Name` 框高 39.69（原来 35.8）+ auto[8~32] + 字距 −2
            //   ③ 卡背画在 `Frame` **之前**（原版子件序 = `Deck Image` → `Frame`；我们原来是反的）
            //   ⑤ 军种格的**排列顺序** = 原版 option 序号（原来按字母序）
            //   ⑥ Deck 页**行距** = 364.5（原版 `RSR._spacingY = 0`；我们原来拿横向的 20 当行距 = 384.5）
            {
                var dpg = win.PageRoot(0);
                var c0 = FindChild(dpg, "CollectionDeck_0");
                if (c0 == null) { CheckTrue(false, "（前提）第 0 格卡组建出来了 —— 下面四条的前提"); }
                else
                {
                    // 🆕 **2026-10-19（`N70` P0-5）**：卡组格悬停 = **整格 `localScale` 1.0 ↔ 1.05**
                    //   （原版 `CollectionDeck.HoverHighlight`，常数 `DAT_1834b3254 = 1.05`、**瞬切无补间**）。
                    //   🧨 改坏法：删掉 `MenuDraw.DeckCell` 里那两句 `onEnter/onExit` ⇒ 下面两条红。
                    {
                        var dHitN = FindChild(c0, "Hit");
                        var dWb = dHitN != null ? dHitN.GetComponent<WindowButton>() : null;
                        CheckTrue(dWb != null && dWb.onEnter != null && dWb.onExit != null,
                                  "★ 卡组格接了悬停/离开（原版 `CollectionDeck.HoverHighlight`）");
                        if (dWb != null && dWb.onEnter != null && dWb.onExit != null)
                        {
                            dWb.onEnter();
                            CheckNear(c0.localScale.x, 1.05f, 1e-4f,
                                      "★ 悬停 ⇒ 整格 **×1.05**（原版 `localScale × DAT_1834b3254`）");
                            dWb.onExit();
                            CheckNear(c0.localScale.x, 1f, 1e-4f, "★ 离开 ⇒ 回到 **1.0**");
                        }
                    }
                    // 🔴 **2026-10-11（A1507）改了节点名**：卡格主图现在是 **`Deck Image`**（督军立绘），
                    //   只有「取不到督军立绘」才退回 `CardBack` ⇒ 两个名字都要认（缺一就漏检）。
                    var cb = FindChild(c0, "Deck Image") ?? FindChild(c0, "CardBack");
                    var fr = FindChild(c0, "Frame");
                    int bi = cb != null ? cb.GetSiblingIndex() : -1;
                    int fi = fr != null ? fr.GetSiblingIndex() : -1;
                    CheckTrue(fi >= 0 && (bi < 0 || bi < fi),
                              "★ A1503③ 卡背/立绘画在 `Frame` **之前**（原版子件序 `Deck Image` → `Frame`；"
                              + "实得 主图=" + (cb != null ? cb.name : "<无>") + " 兄弟序=" + bi + " / Frame=" + fi + "）");
                    // ★ A1507：卡格主图必须是**督军的立绘**（原版 `CollectionDeck.Config` 的
                    //   `deckHero.cardImageReference`），不是卡背。判据 = 节点的贴图名含督军卡 id 的立绘。
                    var di0 = CollectionData.DeckAt(0);
                    var wantTex = CardArt.Portrait(di0.WarlordId);
                    var gotQ = cb != null ? cb.GetComponentInChildren<ImageQuad>() : null;
                    if (wantTex != null && gotQ != null)
                        CheckTrue(gotQ.Texture != null && gotQ.Texture.name == wantTex.name,
                                  "★ A1507 卡格主图 = **督军立绘** `" + wantTex.name + "`（原版 `deckHero.cardImageReference`；"
                                  + "实得 " + (gotQ.Texture != null ? gotQ.Texture.name : "<null>") + "）");
                    else CheckTrue(false, "（前提）第 0 格取得到督军立绘与主图 quad —— 上面那条的前提"
                                          + "（warlord=" + di0.WarlordId + "）");
                    var nt = FindChild(c0, "Deck Name");
                    var nl2 = nt != null ? nt.GetComponent<Label>() : null;
                    CheckTrue(nl2 != null, "（前提）`Deck Name` 那颗 `Label` 取得到");
                    // ⚠️ **量的口径**：`Label.WorldH` 是 **TMP【渲染】出来的高**（`_tmpH`），**不是框高**
                    //   —— 第一版拿它比「框高 39.69×0.9」当场红了（实测 43.66）。**框高在这套里量不到**，
                    //   所以这一条断**常量本身**：期望值 **39.69** = 原版 prefab 的读数（`MenuDraw.DcNameH` 的注释）。
                    CheckNear(MenuDraw.DcNameH, 39.69f, 0.01f,
                              "★ A1503② `Deck Name` 框高 = **39.69**（原版 prefab 实读；我们原来写 35.8）");
                    if (nl2 != null)
                        Debug.Log(P + $"  【量】`Deck Name` 的 TMP 渲染高 = {nl2.WorldH * 108f:F2}px（**仅记录**，不作断言）");
                }
                // ⑥ 行距：同一列、上下相邻两格（列数 6 ⇒ 下标差 6）的 y 差 = 364.5
                var r0 = FindChild(dpg, "CollectionDeck_0");
                var r1 = FindChild(dpg, "CollectionDeck_6");
                if (r0 != null && r1 != null)
                    CheckNear(Mathf.Abs(MenuCheck.DesignPx(r0).y - MenuCheck.DesignPx(r1).y), 364.5f, 1.5f,
                              "★ A1503⑥ Deck 页**行距 = 364.5**（原版 `RSR._spacingY = 0`；我们原来拿横向那个 20 当行距 ⇒ 每行多 20）");
                else CheckTrue(false, "（前提）第 0 格与第 6 格（下一行）都在视口内 —— 上面那条的前提");
                // ⑤ 军种格顺序：按原版 option 序号**单调**
                var facs5 = CollectionWindow.CardsState.Factions();
                int lastK = -1; bool mono = true;
                foreach (var f5 in facs5)
                {
                    int k5 = System.Array.IndexOf(DeckEditorState.ArmyOrder, f5);
                    if (k5 < 0) k5 = int.MaxValue;
                    if (k5 < lastK) mono = false;
                    lastK = k5;
                }
                CheckTrue(mono && facs5.Count >= 2,
                          "★ A1503⑤ 军种格顺序 = **原版 option 序号**（`DeckEditorState.ArmyOrder`）且单调 —— "
                          + "本轮卡池里 " + facs5.Count + " 个阵营：" + string.Join(",", facs5.ToArray()));
            }
            // ============================================================ 🆕 **2026-10-11（第十七会话 · `A1525⑤`）：
            //   卡组格那两枚徽章（`Faction Icon` 左下圆徽 / `Game Mode Icon` 右下模式徽）的**渲染队列夹层** + 可见性**
            // 🔴 **2026-10-11（`A1530②`）**：节点名 `Faction` → **`Faction Icon`**（原版 prefab 原样名；
            //   同族的 `Game Mode Icon` / `DificultyLevel` 都照抄了原版拼写，只有这一颗被改短）。
            //   ⇒ 本文件按名字取件的这一处跟着改（建节点处 = `Shell/MenuDraw.cs` `DeckCell`）。
            // 判据（两条，全文 → `Shell/MenuDraw.cs` `DeckCell` 的 `A1525⑤` 那一段）：
            //   ① **原版 prefab 的子件序**（`menu_dump.py bundle_menus_assets_all -2024334647938937812 --depth 4`
            //      `Deck` 下**底→顶**）= `Highlight` → `Deck Image(mask)` → `Deck Image` → `Frame` →
            //      `Deck Name` → **`Faction Icon`** → **`Game Mode Icon`** → `DificultyLevel`
            //      ⇒ 两枚徽章排在 `Frame` **之后** = 画在它**之上**；
            //   ② **两张用户实拍**（`收藏—卡组的参考图.png` 4 格 · `更换卡组的参考.png` 12 格）里
            //      每一格底部**都看得见**这两枚徽章（压在金框与名牌之上）。
            // 🔴 **为什么必须有断言**：改前两枚徽章与 `Frame` **共用队列 `q`** —— 而 `ImageQuad` 全是透明队列、
            //   同队列内按「到相机的 3D 距离」排序（CLAUDE.md §三）⇒ **大而居中的卡框盖住了靠边角的小徽章**
            //   （改前实拍里红枪徽**只剩左上一个月牙**）。⚠️ 这一族**眼睛看不出来**（节点在、图对、队列不对）
            //   ⇒ 只能量队列，量不到「看得见」。
            Section("A1525⑤：卡组格两枚徽章的**队列夹层** + 可见性（「件建了、画面上却没有」那一族）");
            {
                // 先钉**这条梯子本身**：期望值 = 现读 `Shell/CollectionWindow.cs` 那一行常量
                //   （`QPagePanel/QPageSdf/QPageRow/QPageText/QPageOverlay` = 3030/3031/3032/3033/3034）。
                //   🧨 谁把整条梯子抬/降一档（徽章与 `Frame` 一起漂）⇒ 下面那三条红（夹层那条抓不住）。
                Check(CollectionWindow.QPageOverlay, 3034,
                      "梯子顶档 `QPageOverlay` = **3034**（现读 `Shell/CollectionWindow.cs` 那行常量）");
                Check(CollectionWindow.QPageText, 3033, "…`QPageText` = **3033**");
                Check(CollectionWindow.QPageRow, 3032, "…`QPageRow` = **3032**");
                CheckTrue(CollectionWindow.QPageOverlay > CollectionWindow.QPageText
                          && CollectionWindow.QPageText > CollectionWindow.QPageRow,
                          "★ 梯子**单调**：`QPageOverlay` > `QPageText` > `QPageRow` —— 两枚徽章落在"
                        + "「**名字之上**」那一档（= 原版子件序里它们在 `Deck Name` 之后）");

                var dpg5 = win.PageRoot(0);
                int built5 = 0, fac5 = 0, mode5 = 0, both5 = 0, want5 = 0, miss5 = 0;
                int facOff5 = 0, modeOff5 = 0, facQ5 = 0, modeQ5 = 0, layer5 = 0;
                string diag5 = "";
                for (int i = 0; i < CollectionData.DeckCount(); i++)
                {
                    var cell5 = FindChild(dpg5, "CollectionDeck_" + i);
                    if (cell5 == null) continue;       // 视口外的格子根本不建（`DeckScroll.Intersects`）
                    built5++;
                    var ft5 = FindChild(cell5, "Faction Icon");
                    var mt5 = FindChild(cell5, "Game Mode Icon");
                    if (ft5 != null)
                    {
                        fac5++;
                        if (!ft5.gameObject.activeSelf) facOff5++;   // ⚠️ `FindChild` **含未激活件** ⇒ 只断「找得到」测不出「关着」
                        var qq5 = ft5.GetComponent<ImageQuad>();
                        if (qq5 == null || qq5.RenderQueue != CollectionWindow.QPageOverlay) facQ5++;
                    }
                    if (mt5 != null)
                    {
                        mode5++;
                        if (!mt5.gameObject.activeSelf) modeOff5++;
                        var qq5 = mt5.GetComponent<ImageQuad>();
                        if (qq5 == null || qq5.RenderQueue != CollectionWindow.QPageOverlay) modeQ5++;
                    }
                    // 「该有阵营徽」的**存在性**：数据侧该有 + **图确实在**（不在则那一层本来就不建：`Rect` 遇 null 直接 return）
                    //   + **底部没被视口切掉**（同一道闸，见上面 `A1503①` 那段：探针用 `Deck Name` ——
                    //   它在作者系 y **344.2**、比徽章那排的底 **359.4** 更靠上 ⇒ 名字画得出来 ⇒ 徽章一定在框内）。
                    var di5 = CollectionData.DeckAt(i);
                    if (!string.IsNullOrEmpty(di5.Faction) && FindChild(cell5, "Deck Name") != null
                        && CardArt.MenuUi(DeckRuntime.FactionIcon(di5.Faction)) != null)
                    {
                        want5++;
                        if (ft5 == null) miss5++;
                    }
                    if (ft5 == null || mt5 == null) continue;
                    both5++;
                    var fq5 = ft5.GetComponent<ImageQuad>();
                    var mq5 = mt5.GetComponent<ImageQuad>();
                    var frt5 = FindChild(cell5, "Frame");
                    var frq5 = frt5 != null ? frt5.GetComponent<ImageQuad>() : null;
                    if (frq5 == null || fq5 == null || mq5 == null
                        || !(frq5.RenderQueue < fq5.RenderQueue && frq5.RenderQueue < mq5.RenderQueue))
                    {
                        layer5++;
                        if (diag5.Length < 200)
                            diag5 += " #" + i + "(Frame=" + (frq5 != null ? frq5.RenderQueue.ToString() : "<无quad>")
                                   + " 徽=" + (fq5 != null ? fq5.RenderQueue.ToString() : "<无quad>") + "/"
                                   + (mq5 != null ? mq5.RenderQueue.ToString() : "<无quad>") + ")";
                    }
                }
                CheckTrue(built5 > 0, "（前提）Deck 页建出了 " + built5 + " 格 —— 否则下面几条全空转（假绿）");
                CheckTrue(both5 > 0,
                          "（前提）至少有一格**两枚徽章都建出来了**（本轮实测：建 " + built5 + " 格 · `Faction Icon` "
                        + fac5 + " 格 · `Game Mode Icon` " + mode5 + " 格 · 两枚齐 " + both5 + " 格）"
                        + " —— ⛔ 0 格 ⇒ 下面的夹层断言等于没验");
                CheckTrue(want5 > 0,
                          "（前提）有 " + want5 + " 格**数据侧该有阵营徽**（`Faction` 非空 + 图在 + 底部没被切）"
                        + " —— ⛔ 0 格 ⇒ 下面那条存在性断言空转");
                Check(layer5, 0,
                      "★★ A1525⑤ `Frame` 的渲染队列**严格小于**两枚徽章的（夹层 —— 判据 = 原版子件序 + 两张实拍）"
                    + "｜🧨 改坏法：把 `Shell/MenuDraw.cs` `DeckCell` 里两枚徽章那两个 `qOverlay` 改回 `q`"
                    + "（= 与 `Frame` 同队列 ⇒ 被卡框压住，改前那版）⇒ 本条必红" + diag5);
                Check(facQ5, 0, "★ …两枚徽章的队列都 **== `CollectionWindow.QPageOverlay`**（阵营徽那一半）");
                Check(modeQ5, 0, "★ …模式徽那一半");
                Check(facOff5, 0,
                      "★ A1525⑤ `Faction Icon` 徽章**逐格 `activeSelf == true`**（⚠️ `FindChild` 含未激活件 ⇒ "
                    + "只断「找得到」测不出「关着」；「建了却看不见」这一族本仓栽过 4 次）");
                Check(modeOff5, 0, "★ …`Game Mode Icon` 同理");
                Check(miss5, 0,
                      "★ A1525⑤ 该有阵营徽的 " + want5 + " 格**都真有那一颗**｜"
                    + "🧨 改坏法：把那句 `if (!string.IsNullOrEmpty(info.Faction))` 去掉（整层不画）⇒ 本条红");
            }
            // ============================================================ 🆕 **2026-10-11（第十七会话 · `A1525⑥`）：
            //   收藏窗的卡组格**不该有金框**（`Highlight Rounded Square`）**
            // 判据（机器码级两关，全文 → `Shell/CollectionWindow.cs` `BuildDeckCell` 那一大段）：
            //   ① 唯一的调用点 = `CollectionItem<T>.Config` 那句
            //      **`if (useSelectedHighlight) SelectedHighlight(isSelected)`**
            //      （`script.json` 的 `CollectionItem<object>$$Config` @ RVA **`0x15F1B10`**）；
            //   ② 两个变体的这个字段不一样：**`Collection Deck`（收藏窗用的那份）= 0** ·
            //      `Collection Deck With Highlight`（弹窗那份）= 1 ⇒ **本页永远不调 `SelectedHighlight`**。
            //   旁证：两张用户实拍（`收藏—卡组的参考图.png` 4 格 · `更换卡组的参考.png` 12 格）**没有一格是金的**。
            Section("A1525⑥：收藏窗卡组格**没有**金框（原版这一页本来就不画）");
            {
                var dpg6 = win.PageRoot(0);
                int built6 = 0, hl6 = 0;
                string diag6 = "";
                for (int i = 0; i < CollectionData.DeckCount(); i++)
                {
                    var cell6 = FindChild(dpg6, "CollectionDeck_" + i);
                    if (cell6 == null) continue;
                    built6++;
                    // ⚠️ `FindChild` **含未激活件** —— 这一次要的正是「**压根没有**」：关着的金框也算有。
                    if (FindChild(cell6, "Highlight Rounded Square") != null)
                    { hl6++; if (diag6.Length < 120) diag6 += " #" + i; }
                }
                CheckTrue(built6 > 0, "（前提）Deck 页建出了 " + built6 + " 格");
                Check(hl6, 0,
                      "★ A1525⑥ 收藏窗**逐格**都没有 `Highlight Rounded Square`｜"
                    + "🧨 改坏法：把 `Shell/CollectionWindow.cs` `BuildDeckCell` 那个实参从 `false` 改回 "
                    + "`i == CollectionData.CurrentIndex()` ⇒ 本条必红" + diag6);
                int cur6 = CollectionData.CurrentIndex();
                var ccell6 = FindChild(dpg6, "CollectionDeck_" + cur6);
                if (ccell6 != null)
                    CheckTrue(FindChild(ccell6, "Highlight Rounded Square") == null,
                              "★★ …**当前那一格也没有**（# " + cur6 + " = `CollectionData.CurrentIndex()`）—— "
                            + "上面那条逐格断言的**判别式**：改回 `i == CurrentIndex()` 时它抓的就是这一格"
                            + "（那一格若恰在视口外，逐格那份量不到）");
                else
                    Debug.LogWarning(P + "   【限额·**不是通过**】当前卡组 #" + cur6
                                       + " 那一格没建出来（视口外）⇒ 「当前那一格也没有金框」本次没验到；"
                                       + "上面那条**逐格**断言照样成立。");
            }
            // ============================================================ 🆕 **灭自证对**（与上面那条**结构上不可能同时满足**）：
            //   选卡组弹窗那一份变体（`Collection Deck With Highlight`，`useSelectedHighlight = 1`）
            //   选中那一格**必须真画金框**。
            // ⚠️ 弹窗那份的 `sel` 是**真值**（`Shell/DeckSelectionPopup.cs` 的 `pick.OwnIndex == DeckIndex`）
            //   ⇒ **别顺手把它也改成 `false`**（那是另一棵树）。本段自开一扇窗，**末尾关掉并清场**
            //   （照本文件 `A1035①` 那一节的收尾三句）。
            {
                var psel = DeckSelectionPopup.Create(win.Manager);
                CheckTrue(psel != null, "（前提）自开一扇 `Deck Selection Popup`（验「弹窗那一份**有**金框」）");
                if (psel != null)
                {
                    psel.TryOpen(null);
                    psel.SwitchTab(true);        // 「我的卡组」页（`SwitchTab` 会把 `DeckIndex` 清成 −1）
                    var shown6 = psel.Shown();
                    CheckTrue(shown6.Count > 0,
                              "（前提）「我的卡组」页列出 " + shown6.Count + " 副 —— 否则下面那条空转（假绿）");
                    if (shown6.Count > 0)
                    {
                        int pk6 = shown6[0].OwnIndex;
                        psel.DeckIndex = pk6;    // 把「选中」显式落在**确实在这一页里**的那一副上
                        psel.RebuildListNow();
                        var sc6 = FindChild(psel.transform, "DeckSel_" + pk6);
                        CheckTrue(sc6 != null, "（前提）弹窗里 `DeckSel_" + pk6 + "` 建出来了");
                        var hl6b = sc6 != null ? FindChild(sc6, "Highlight Rounded Square") : null;
                        CheckTrue(hl6b != null && hl6b.gameObject.activeSelf,
                                  "★★ 灭自证对：**弹窗**里选中的那一格**有** `Highlight Rounded Square` 且**开着**"
                                + "（原版 `Collection Deck With Highlight` 的 `useSelectedHighlight = 1`；"
                                + "⚠️ `activeSelf` 一起断 —— `FindChild` 含未激活件）—— "
                                + "与上面「收藏窗逐格没有金框」**结构上不可能同时满足**");
                        if (hl6b != null)
                            CheckTint(hl6b, new Color(1f, 0.773f, 0f, 1f),
                                      "★★ …而且那颗的染色 = 原版 `Highlight Rounded Square` 的 **(1, 0.773, 0)**（金）");
                    }
                    DeckSelectionPopup.LastOpened = null;             // 不留状态给后段
                    PointerLayer.UnregisterOwnedBy(psel.gameObject);  // 自开的窗自己把滚动登记撤掉（A867 那条不变量）
                    psel.Close();
                    Object.DestroyImmediate(psel.gameObject);
                }
            }
            // 🔴 **实拍抓的一条**：第一版照抄了 dump 的 `1405`（= **布局组跑之前的模板位**，
            //    父容器 `Header/Filters` 是 425..1385 的 VLG）⇒ **它和右对齐到 1821 的标题叠在一起**。
            //    ⇒ 现在断「按钮在容器内」**并且**「标题与按钮不叠」—— 这类错**矩形断言量不到**，
            //      得**量文字自己的宽度**算边缘（同筛选栏标签那两条）。
            // 🔴 2026-09-24：**x 也从 425 改成 612.2** —— 与 Deck/Cards/Styles 三页**同一个值**
            //    （四页共用同一条 `Header`；理由见 `CollectionWindow.BuildFilterHeader` 的注释）。
            {
                var bq = FindChild(cpage, "Clear filters");
                CheckTrue(bq != null && Mathf.Abs(MenuCheck.DesignPx(bq).x - 737.17f) <= 1f,
                          "`Clear filters` 底图中心 x = **737.17**（= 612.17 + 250/2 —— 与另外三页同值）"
                          + " —— **别照抄 dump 里的 1405 / 1488.59**（那是布局组跑之前的模板位）");
                var ttlT = FindChild(cpage, "Header Label");
                var clrT = FindChild(cpage, "Clear filters Text");
                var lt = ttlT != null ? ttlT.GetComponent<Label>() : null;
                var lc2 = clrT != null ? clrT.GetComponent<Label>() : null;
                if (lt != null && lc2 != null)
                {
                    // 🔴 **2026-10-17（R2 现核 + 末轮复跑）**：这一节跑的时候当前页可能不是 Cosmetics ⇒ 页节点
                    //   `SetActive(false)`；而且**没有筛选时 `Clear filters` 那一颗本身也是关着的**
                    //   （`RefreshClearFilter` 按「筛选空不空」开关）⇒ 两颗 TMP 都量不到（哨兵 4.29e9）。
                    //   ⇒ 量之前把「页 + 按钮 + 文字」这条链临时点亮、量完逐级还原（⛔ 不改产品显隐）。
                    bool pageWas = cpage != null && cpage.gameObject.activeSelf;
                    var cfParent = clrT != null ? clrT.parent : null;
                    bool cfPWas = cfParent != null && cfParent.gameObject.activeSelf;
                    bool cfTWas = clrT != null && clrT.gameObject.activeSelf;
                    if (cpage != null && !pageWas) cpage.gameObject.SetActive(true);
                    if (cfParent != null && !cfPWas) cfParent.gameObject.SetActive(true);
                    if (clrT != null && !cfTWas) clrT.gameObject.SetActive(true);
                    lt.ForceRelayout(); lc2.ForceRelayout();
                    // 🆕 **2026-10-16（A796′）换口**：半宽（`WorldW × 54`）→ `LabelRenderedPx`（TMP `textBounds`）；
                    //    中心仍是**节点位置**（只换「从哪个口读那个数」）。
                    float tLeft = MenuCheck.DesignPx(lt.transform).x - LabelRenderedPx(lt).x * 0.5f;
                    float cRight = MenuCheck.DesignPx(lc2.transform).x + LabelRenderedPx(lc2).x * 0.5f;
                    CheckTrue(tLeft > cRight,
                              $"标题（左缘 **{tLeft:F0}**，右对齐到 1821）与 `Clear filters`（右缘 **{cRight:F0}**）**不叠**");
                    if (cfParent != null && !cfPWas) cfParent.gameObject.SetActive(false);
                    if (clrT != null && !cfTWas) clrT.gameObject.SetActive(false);
                    if (cpage != null && !pageWas) cpage.gameObject.SetActive(false);
                }
            }
            // 左抽屉：**出厂 act=F**，靠 `Filter Toggle` 开合
            CheckTrue(!win.CosmoFiltersOpen, "筛选抽屉 `Cosmetic FIlter` **起手是收起的**（实证 act=F）");
            var cHit = FindChild(cpage, "FiltersHit");
            var cBtn = cHit != null ? cHit.GetComponent<WindowButton>() : null;
            CheckTrue(cBtn != null, "`Filter Toggle` 有点击区");
            if (cBtn != null)
            {
                cBtn.Click();
                CheckTrue(win.CosmoFiltersOpen, "点 `Filters` ⇒ 抽屉打开");
                cBtn.Click();
                CheckTrue(!win.CosmoFiltersOpen, "再点 ⇒ 收起");
            }
            // ---------------- 🆕 2026-10-03（§三 第 29 条 A11）：卡背页的筛选抽屉（**两行都建了**）----------------
            //   判据 = `FilterPanelModel.BuildCosmetics`（与卡组编辑那扇窗**同一份模型** ——
            //   🔴 **2026-10-10 订正（A271）**：**不再逐字相同**了。那颗 `'Owned only'` 的**自适应下界**：
            //   **卡组编辑窗显式传 26**（原版 `auto[26~32]`，A247）· **本窗走缺省 18**（原版 `auto[18~32]`，本来就对））
            //        + `CardbackTable.NamesFor`（阵营 → 卡背，**判据只有一份**）。
            //   原来那句「A4 只给了容器 rect、没给格子尺寸 ⇒ 没建」**早就不成立**（2026-09-28 在卡组编辑实测补全）。
            Section("卡背页筛选抽屉：Army 13 格 + Owned（**行序 Army 在前**，与卡牌那套相反）");
            if (cBtn != null)
            {
                cBtn.Click();                       // 再开一次（上一组开合把它关回去了）
                CheckTrue(win.CosmoFiltersOpen, "抽屉开着才量得到格");
                var drw = FindChild(cpage, "Cosmetic FIlter");
                Check(win.CosmoFilterCellCount, 14,
                      "格子总数 = **14** = Army **13** + `Owned` **1**"
                      + "（⚠️ 这一页**没有**搜索框 / 稀有度 / 费用 / 类型 —— 原版那棵树里就没有）");

                var cf = CollectionWindow.CosmoState.Factions();
                CheckTrue(cf.Count > 0, $"阵营表有 {cf.Count} 个（按它铺 Army 格）");
                var ca0 = cf.Count > 0 ? FindChild(drw, "Cell_fac_" + cf[0]) : null;
                CheckTrue(ca0 != null, "Army 第 1 格在（按**名字**找，不按序号）");
                if (ca0 != null)
                {
                    // 面板内 (pad L14, 15+50) ⇒ `Abs` 加 (0.25,155.9) ⇒ 中心 (64.25, 270.9)
                    CheckNear(MenuCheck.DesignPx(ca0).x, 64.25f, 0.6f, "Army 第 1 格中心 x = **64.25**（0.25+14+50）");
                    CheckNear(MenuCheck.DesignPx(ca0).y, 270.9f, 0.6f,
                              "Army 第 1 格中心 y = **270.9**（155.9+15+50+50）"
                              + " —— 这一页 **Army 在前**（卡牌那套是 Owned/Upgradable 在前）");
                    CheckArt(ca0, DeckRuntime.FactionIcon(cf[0]), "Army 格的图 = 该阵营图标（原版运行期赋）");
                }
                // `Owned` 行：`CosmoOwnedTop(13)` = 15 + 550 + 12.81 = 577.81 ⇒ 绝对行顶 733.71、中心 758.71
                var co = FindChild(drw, "Cell_owned");
                CheckTrue(co != null, "`Owned` 那一格在");
                if (co != null)
                {
                    CheckNear(MenuCheck.DesignPx(co).y, 758.71f, 0.6f,
                              "`Owned` 行中心 y = **758.71**（155.9 + 577.81 + 25 —— Army 行 550 一高，它跟着往下走）");
                    // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：这颗字从波 1b（P1）起走词条
                    //   （`Core/FilterPanelModel.cs` 的 `BuildCosmetics` 那颗 `Owned` 格，键 = `MenuDeck/Filters/ShowOwnedOnly`）
                    //   ⇒ 原来写死的 `"Owned only"` 只是**英文列的副本**，
                    //   宿主跑中文档时它印的是中文列 ⇒ **本条必红（这就是本轮那条红）**。
                    //   期望值改成 `Loc.T(键)` ⇒ 两语档都成立。
                    //   ⚠️ **2026-10-18 收口（铁律 5）**：本段原来引的是实现里那两个符号名
                    //     （`TermOr(OwnedOnlyTerm, "Owned only")`）—— 那道**取词闸门已删除**（键全在表里，
                    //     闸门是静默兜底）⇒ 实现现在是**裸 `Loc.T(OwnedOnlyTerm)`**，本段改成不引那个已不存在的符号。
                    //     ⚠️ 断言本身**一个字没动**（`Loc.T(键)` 那句两版都成立 —— 键在表 ⇒ 闸门与裸调用同值）。
                    //   ⚠️ 上面那条 `HasEntry` **仍然不是装饰**：键若不在表里，`Loc.T` 回的是**键名本身**
                    //      ⇒ 实现印键名、期望也是键名，这条会**假绿**（看不出「词条丢了」）—— 它挡的是这个。
                    CheckTrue(Loc.HasEntry("MenuDeck/Filters/ShowOwnedOnly"),
                              "（前提）词条 `MenuDeck/Filters/ShowOwnedOnly` 在表里"
                            + "（⛔ 不在 ⇒ 下一条红得看不出原因）");
                    CheckText(TextOf(FindChild(co, "Label")), Loc.T("MenuDeck/Filters/ShowOwnedOnly"),
                              "`Owned` 的标签文案（**随语档** —— 键 `MenuDeck/Filters/ShowOwnedOnly`，"
                            + "英文列 = 原版那颗 TMP 的 `m_text` `Owned only`）");
                    // 🆕 **2026-10-09（A270）**：这颗 `'Owned only'` 的**自适应窗口**（TMP 真字段）。
                    //   🔴 **为什么单开一条**：今天**全仓没有一条断言量得到本窗这一颗** ——
                    //     本页走 `FilterPanelModel.BuildCosmetics` 的**缺省形参** `labelAutoMin`
                    //     （缺省 = 共用常量 `ToggleFontAutoMin` = 18），而**卡组编辑窗显式传 26**
                    //     （原版 `auto[26~32]`，A247）⇒ 谁动那个缺省值、或动那个共用常量，
                    //     **本窗会静默变歪而自检照样绿**。判据 = 原版卡背页那颗的实读值：
                    //     **字号 32 · `auto[18~32]` · 折行 0**（A270）—— **18 本来就是对的**。
                    //   ⚠️ 读的是 `Label.FontSizeMin/Max`（**TMP 里那两个真字段**，经工程唯一那份
                    //     `Label.FontSizeToPx` 折成画布 px），⛔ **不是** `cell.LabelAutoMin` 那个实参（那是自证）。
                    //   **改坏法**：把 `Core/FilterPanelModel.BuildCosmetics` 的缺省形参改成别的值（例：26）
                    //     ⇒ 下界读成 26 ≠ 18 ⇒ 下面那条红。
                    {
                        var olb = FindChild(co, "Label") != null
                                ? FindChild(co, "Label").GetComponent<Label>() : null;
                        CheckTrue(olb != null && olb.CanRenderChinese,
                                  "（前提）`Owned only` 那颗走的是**真 TMP** —— 点阵后端没有「自适应」这回事"
                                + "（`FontSizeMin/Max` 恒 0），不前置下面两条会假绿");
                        if (olb != null && olb.CanRenderChinese)
                        {
                            CheckNear(Label.FontSizeToPx(olb.FontSizeMin), 18f, 0.5f,
                                      "★ `Owned only` 的**自适应下界** = 原版 `m_fontSizeMin` **18px**"
                                    + "（本页走缺省形参 —— 卡组编辑窗那颗是 26，两窗**不再逐字相同**，A270/A271）");
                            CheckNear(Label.FontSizeToPx(olb.FontSizeMax), 32f, 0.5f,
                                      "★ …**上界** = 原版 `m_fontSizeMax` **32px**（= 原版那颗的 `m_fontSize`）");
                        }
                    }
                    CheckNear(Wpx(FindChild(co, "Background")), 70.59f, 1.5f,
                              "开关底图宽 = **70.59** = 0.3×335.31 − 30（原版那条锚点式子）");
                }

                // ---- **真的筛得动**（重画会重建格 ⇒ 每次点完要**重新找**那个节点）----
                System.Action<string> clickCell = key =>
                {
                    var c = FindChild(FindChild(cpage, "Cosmetic FIlter"),
                                      "Cell_" + key.Replace("$", "").Replace(":", "_"));
                    var h = c != null ? FindChild(c, "Hit") : null;
                    var b = h != null ? h.GetComponent<WindowButton>() : null;
                    CheckTrue(b != null, "格子 `" + key + "` 的点击区在");
                    if (b != null) b.ClickForTest();
                };
                int allN = CollectionWindow.CosmoTotal;
                if (cf.Count > 0)
                {
                    int wantFac = CardbackTable.NamesFor(cf[0], CardArt.CosmeticNames()).Length;
                    clickCell("$fac:" + cf[0]);
                    Check(CollectionWindow.FilteredCosmoNames().Length, wantFac,
                          $"点「{cf[0]}」⇒ 筛出 **{wantFac}** 张（判据 = `CardbackTable.NamesFor` 那一份）");
                    CheckTrue(wantFac > 0 && wantFac < allN,
                              $"…比全部 {allN} 张少 ⇒ **这是真筛**（不是摆设；`Army` 那半本来一直是空的）");
                    // 卡背格也跟着重画了（格数只能是**变少**）
                    CheckTrue(win.CosmoCells.Count > 0 && win.CosmoCells.Count <= wantFac,
                              $"卡背格重画了（{win.CosmoCells.Count} 格 ≤ {wantFac} 张）");
                    clickCell("$fac:" + cf[0]);        // 再点一次 = 取消（`FilterPanelModel.Click` 的语义）
                    Check(CollectionWindow.FilteredCosmoNames().Length, allN, "再点一次 ⇒ 取消阵营筛选，回到全部");

                // 🆕 **2026-10-25（多选筛选 · 点击路径）**：连点两个阵营 ⇒ **两个都选中、结果 = 并集**
                //   判据 = `判据/共用件_过滤模型_B升A.md` 第 10/11 条（`OnFilterChange` 收集 `IsOn` 成集合、`Filter` 取并集）。
                //   🧨 改坏法：把 `FilterPanelModel.Click` 的阵营支改回「单击覆盖」⇒ 集合只剩 1 ⇒ 第一条红。
                if (cf.Count >= 2)
                {
                    var wantSet = new HashSet<string>();
                    foreach (var nm in CardbackTable.NamesFor(cf[0], CardArt.CosmeticNames())) wantSet.Add(nm);
                    foreach (var nm in CardbackTable.NamesFor(cf[1], CardArt.CosmeticNames())) wantSet.Add(nm);
                    clickCell("$fac:" + cf[0]);
                    clickCell("$fac:" + cf[1]);
                    Check(CollectionWindow.CosmoState.Filter.Factions != null
                              ? CollectionWindow.CosmoState.Filter.Factions.Count : -1, 2,
                          "★ 连点两个阵营 ⇒ **集合里两个都在**（同类多选）");
                    Check(CollectionWindow.FilteredCosmoNames().Length, wantSet.Count,
                          $"★ 多选的结果 = **并集**（{cf[0]} ∪ {cf[1]} = {wantSet.Count} 张）");
                    CheckTrue(wantSet.Count > CardbackTable.NamesFor(cf[0], CardArt.CosmeticNames()).Length,
                              "…而且比只选第一个**多**（挡「并集写成了交集/只剩后一个」）");
                    clickCell("$fac:" + cf[0]);
                    clickCell("$fac:" + cf[1]);       // 各自再点一次 ⇒ 集合清空
                    CheckTrue(CollectionWindow.CosmoState.Filter.Factions == null
                              || CollectionWindow.CosmoState.Filter.Factions.Count == 0,
                              "★ 再各点一次 ⇒ **集合清空**");
                    Check(CollectionWindow.FilteredCosmoNames().Length, allN, "…结果回到全部");
                }
                }
                // `Owned only`：单机全解锁 ⇒ **切得动但不改变结果**（如实标的差异，不是静默失效）
                bool ownedBefore = CollectionWindow.CosmoState.Filter.Owned;
                clickCell("$owned");
                Check(CollectionWindow.CosmoState.Filter.Owned, !ownedBefore, "`Owned only` 那个开关切得动");
                Check(CollectionWindow.FilteredCosmoNames().Length, allN,
                      "…但**结果不变**（单机全解锁 —— 与卡组编辑那扇窗同一条如实标注）");
                clickCell("$owned");
                // `Clear filters`：回到全部
                if (cf.Count > 0) clickCell("$fac:" + cf[0]);
                win.ClearCosmoFilters();
                Check(CollectionWindow.FilteredCosmoNames().Length, allN, "`Clear filters` ⇒ 回到全部 233 张");
                CheckTrue(CollectionWindow.CosmoState.Filter.Faction == null
                          || CollectionWindow.CosmoState.Filter.Faction.Length == 0, "…阵营条件真的清掉了");

                Shoot("05_收藏_卡背筛选抽屉.png");
                cBtn.Click();                       // 收回去（下一张实拍要的是收起态）
                CheckTrue(!win.CosmoFiltersOpen, "量完收回去");
            }

            if (win.CosmoScroll != null && win.CosmoScroll.MaxOffset > 0f)
            {
                win.CosmoScroll.ScrollBy(win.CosmoScroll.MaxOffset);
                int last = CollectionWindow.CosmoTotal - 1;
                var lc = FindChild(cpage, "CollectionCosmetic_" + last);
                CheckTrue(lc != null && MenuCheck.DesignPx(lc).y + 405f * 0.5f <= 1080.5f,
                          $"**滚到底 ⇒ 第 {last + 1} 张（最后一张卡背）完整落进视口**");
                win.CosmoScroll.ScrollBy(-win.CosmoScroll.MaxOffset);
            }

            // ═══════════ 🆕 2026-10-08（A181）：卡背格的两层也被视口裁住 ═══════════
            //  判据 = 原版 `Cardback Display/Scroll View/Viewport` 上那颗 `RectMask2D`
            //  （`m_Softness = (0,0)` · `m_Padding = (0,0,0,0)`）。
            //  🔴 这一页最容易露馅：格里的 **`Cardback Shadow SDF` 比格大一圈**
            //  （左 −42.5 / 右 +45 / 🔴 `A994③` 起上下按贴图比例内接 ⇒ **上 −14.85 / 下 +18.9**，
            //   原来是拉满时的 上 −70.875 / 下 +74.925；见 `RebuildCosmoCells` 那段逐值出处）⇒ 压边那几格
            //  光靠「格与视口求交」拦不住它。原来那两行是直调 `ImageQuad.Create` ⇒ **整块画出去**。
            //  **改坏法**：把 `MenuDraw.Rect(…, CosmoView, …)` 的 `CosmoView` 实参去掉 ⇒ 越界 quad 立刻出现 ⇒ 红。
            if (win.CosmoScroll != null)
            {
                var csc = win.CosmoScroll;
                float savedCO = csc.Offset;
                csc.SetOffset(192f);            // 上移 192 ⇒ 第一排格（155.94..560.94）被视口上沿切掉 192px
                var vpC = CollectionWindow.CosmoView;
                int cidx = -1;
                for (int i = 0; i < CollectionWindow.CosmoCols; i++)
                {
                    var rr = csc.Shift(CollectionWindow.CosmoCellRect(i));
                    if (rr.y1 < vpC.y1 - 0.5f && rr.y2 > vpC.y1 + 8f) { cidx = i; break; }
                }
                CheckTrue(cidx >= 0, "（前提）卡背页滚 192px 后第一排有一格压在视口上沿上");
                var ccell = cidx >= 0 ? FindChild(cpage, "CollectionCosmetic_" + cidx) : null;
                CheckTrue(ccell != null, $"被切的那一格（`CollectionCosmetic_{cidx}`）建出来了");
                if (ccell != null)
                {
                    int quads = 0, qover = 0;
                    foreach (var q in ccell.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (q == null || !q.gameObject.activeInHierarchy) continue;
                        float w = q.WorldW * 108f, h = q.WorldH * 108f;
                        float cx = MenuCheck.DesignPx(q.transform).x, cy = MenuCheck.DesignPx(q.transform).y;
                        float x1 = cx - w * 0.5f, x2 = cx + w * 0.5f, y1 = cy - h * 0.5f, y2 = cy + h * 0.5f;
                        quads++;
                        if (y1 < vpC.y1 - 0.5f || y2 > vpC.y2 + 0.5f
                            || x1 < vpC.x1 - 0.5f || x2 > vpC.x2 + 0.5f)
                        {
                            qover++;
                            Debug.LogError(P + $"     ↳ `{q.name}` 的渲染矩形 {x1:F1},{y1:F1} → {x2:F1},{y2:F1} 越出视口");
                        }
                    }
                    CheckTrue(quads >= 2, $"那一格**两层都量得到**（实测 {quads} 个 quad = SDF + 卡背本体）");
                    Check(qover, 0, "★ 卡背格的两层都**落在视口内**（越界 quad 0 个 —— SDF 比格大一圈，最容易露）");
                }
                // 对照：没被切的那一排（第 2 排）⇒ 仍是**内接矩形本身**（没被视口一起压扁）
                //  🔴 **2026-10-18（A994③）**：期望值从「整格高 405」改成**按该格贴图现算的内接高**；
                //    405 是**框**高（= 拉满），而原版那颗 `Image` 带 `m_PreserveAspect = 1`。
                //  🔴 **2026-10-19（B2）**：现算那一步从 `InsetFit`（只做第一段）换成 **`CardbackDrawnPx`**
                //    （两段式：先按 `m_Rect` 定框、再按 `textureRect/m_Rect` 缩）—— 口径/判据 → `Core/CardbackFace.cs`。
                var cfull = FindChild(cpage, "CollectionCosmetic_" + CollectionWindow.CosmoCols);
                if (cfull != null)
                {
                    var cq = FindChild(cfull, "Cardback");
                    var cqq = cq != null ? cq.GetComponentInChildren<ImageQuad>() : null;
                    var ctq = cqq != null ? cqq.Texture as Texture2D : null;
                    var fitF = ctq != null
                        ? CardbackDrawnPx(ctq, CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH)
                        : new Vector2(CollectionWindow.CosmoCellW, CollectionWindow.CosmoCellH);
                    CheckNear(cqq != null ? cqq.WorldH * 108f : -1f, fitF.y, 1.5f,
                              $"对照：没被切的那一排卡背 = **本格自己两段式后的高 {fitF.y:F2}**（实测 "
                            + $"{(cqq != null ? cqq.WorldH * 108f : -1f):F1}）—— 只在越界时裁、不是一律压扁；"
                            + $"也**不是框高 {CollectionWindow.CosmoCellH}**（两段式之后高只可能 ≤ 360.68）");
                }
                csc.SetOffset(savedCO);
            }
            Shoot("04_收藏_Cosmetics.png");

            // `Empty Collection Warning`（**Cosmetics 页那一份**；矩形与别页又不同）
            {
                var ew = FindChild(win.PageRoot(2), "Empty Collection Warning");
                CheckTrue(ew != null, "Cosmetics 页有 `Empty Collection Warning`");
                if (ew != null)
                {
                    CheckAt(ew, 170.44f, 1970.00f, 70.94f, 1080f, "Cosmetics 页 `Empty Collection Warning` 的位置");
                    CheckTrue(!ew.gameObject.activeSelf,
                              $"233 张卡背 ⇒ 不显示（🆕 2026-10-03：抽屉两行齐了 ⇒ 这条判据**真能触发**；"
                              + "13 个阵营各 9~20 张 ⇒ 正常筛不空）");
                }
            }

            // ============================================================ 卡片详情窗（2026-09-24）
            // 出处：`资料/阶段二_卡片详情窗_原版规格.md`（逐节点表 + 三块面板 + 计数条）。
            Section("卡片详情窗（点一张卡开 · 原版菜单版 `CardDisplayWindow`）");
            win.tabButtons.Click(1);                       // 回 Cards 页
            {
                var cp = win.PageRoot(1);
                var hit0 = cp != null ? FindChild(cp, "CardHit_0") : null;
                var hb0 = hit0 != null ? hit0.GetComponent<WindowButton>() : null;
                CheckTrue(hb0 != null, "卡池第 1 格有点击区");
                // 🆕 **2026-10-19（`N70` P0-5）**：卡牌格悬停 = 原版 `Card2DController.ToggleHighlight`（alpha 0.2s）。
                //   我们落在 **卡面那层 `Card Highlight And Shadow`** 的 `_Outline` 上
                //   （`CardView.SetHoverHighlight` → `SetOutline`；⛔ **不是**战斗的 `_rim` —— 那是另一层，
                //    判据见 `SetHoverHighlight` 的注释）。
                //   🧨 改坏法：删掉 `AttachCardHover` 那两句 ⇒ `onEnter` 为空 ⇒ 第一条红；
                //      把 `SetHoverHighlight` 写回 `_rim`（或删掉 `SetOutline` 那一句）⇒ 后面整组红。
                if (hb0 != null)
                {
                    CheckTrue(hb0.onEnter != null && hb0.onExit != null,
                              "★ 卡格接了悬停/离开（原版 `Card2DController.ToggleHighlight(bool)`）");
                    var cv0 = cp != null ? FindChild(cp, "CollectionCard_0") : null;
                    var view0 = cv0 != null ? cv0.GetComponent<CardView>() : null;
                    CheckTrue(view0 != null, "（前提）第 1 格是 `CardView`（悬停那条判据的载体）");
                    // 🆕 **2026-10-23（`C3`）**：悬停光**四项参数**照原版 `Collection Card` 那一份
                    //   （判据 = `MENUS` 的 `CollectionCard.highlightColor` + `Card Highlight And Shadow` 的
                    //   RectTransform + 材质 `Card Frame SDF`；核查报告 §三）。
                    if (view0 != null && hb0.onEnter != null && hb0.onExit != null)
                    {
                        var prevMode = CardTween.Mode;
                        CardTween.Mode = DG.Tweening.UpdateType.Manual;   // 0.2s 那条要手动推（批处理无帧循环）
                        hb0.onEnter();
                        // ① 颜色 = #0080FF（原版 `CollectionCard.highlightColor` 逐位实读）
                        var mc = view0.OutlineTargetForTest;
                        CheckTrue(mc.a > 0f,
                                  "★ 悬停 ⇒ 描边目标 **alpha > 0**（原版 `ToggleHighlight(true)` 把 alpha 补到 1）");
                        CheckNear(mc.r, 0f, 0.004f, $"★ 菜单悬停色 **r = 0**（原版 `highlightColor.r`；实测 {mc.r:F4}）");
                        CheckNear(mc.g, 0.5026951f, 0.004f, $"★ … **g = 0.5027**（= `#0080FF` 的 0x80；实测 {mc.g:F4}）");
                        CheckNear(mc.b, 1f, 0.004f, $"★ … **b = 1**（实测 {mc.b:F4}）—— 原来这里是**白 (1,1,1)**");
                        // ② alpha 目标 = **1.0**（原版 `ToggleHighlight` 直接写 1；⛔ 不乘 `RimAlphaScale 0.447`）
                        CheckNear(mc.a, 1f, 0.004f,
                                  $"★ alpha 目标 = **1.0**、**不乘** `RimAlphaScale`（原版菜单档 `ToggleHighlight` 写的就是 1）"
                                + $"—— 实测 {mc.a:F4}；若变成 {CardHighlight.RimAlphaScale:F3} 说明走错了层（战斗那圈）");
                        // ③ 尺寸 = 原版 4.4281²（= 那层 quad 的**设计**尺寸；原来是卡的 1.09×1.06 = 2.281×3.531）
                        var ms = view0.OutlineQuadSize;
                        CheckNear(ms.x, 4.4281f, 0.02f, $"★ 悬停光 quad 宽 = **4.4281**（原版 `m_SizeDelta.x`；实测 {ms.x:F4}）");
                        CheckNear(ms.y, 4.4281f, 0.02f, $"★ … 高 = **4.4281**（实测 {ms.y:F4}；原来只有卡的 1.09×1.06）");
                        // 🧨 **灭自证（2026-10-24 · `P2`）**：`OutlineQuadSize` 量的必须是**设计网格**，不是渲染包围盒。
                        //   背景：收藏页的卡是**带视口裁切**的（`CardView.CropLayer` 会把软光/影那层的 `sharedMesh`
                        //   换成「按视口裁过的副本」）⇒ 量 `mr.bounds` 会量成「视口里还剩多少」：
                        //   实测第一排那张 = **3.5495 × 3.8923**（上沿被裁 61.7px = 0.535 卡单位 ⇒ 高 4.4281−0.535）。
                        //   ⇒ 这里**故意再裁一刀**（40×40 px、只留卡心那一块）：
                        //     · 量设计网格 ⇒ 仍是 4.4281²（下面两条绿）；
                        //     · 写回 `mr.bounds` ⇒ 只剩 40 / 115.3 ≈ 0.35 ⇒ **这两条立刻红**。
                        {
                            // 卡心 → 设计 px：用 `ToDesignPixel`（= `ApplyClip` 里 `FromPixel` 的**逆**，
                            // ⛔ 不是 `ToPixel` 那一份 108 px/单位；两者只在 16:9 重合）。
                            var cpix = LayoutSpace.ToDesignPixel(view0.transform.position);
                            view0.SetClip(new PxRect(cpix.x - 20f, cpix.y - 20f, cpix.x + 20f, cpix.y + 20f));
                            CheckTrue(view0.Clipped, "（前提）那一刀真的落下去了（卡被裁成 40×40 px）");
                            var qc = view0.OutlineQuadSize;
                            CheckNear(qc.x, 4.4281f, 0.02f,
                                      $"★ **裁成 40×40 px 之后** quad 宽仍是 **4.4281**（实测 {qc.x:F4}）"
                                    + " —— 量的是**设计网格**；量 `mr.bounds` 的话这里只剩 ≈0.35");
                            CheckNear(qc.y, 4.4281f, 0.02f,
                                      $"★ … 高仍是 **4.4281**（实测 {qc.y:F4}）—— `CropLayer` 换掉了网格，但设计尺寸不跟着变");
                            view0.SetClipFromTree();      // 还原成收藏页的生产口径（沿父链解析视口节点）
                            CheckTrue(view0.Clipped, "（还原）裁切回到视口节点那一档（卡仍是裁着的）");
                        }
                        // ④ 材质档 = `Card Frame SDF`（与战斗 `Card board Frame SDF` **只差这三项**）
                        CheckNear(view0.OutlineFloatForTest("_Inner_Edge"), 0.333f, 0.004f,
                                  $"★ 材质 `_Inner_Edge` = **0.333**（原版 `Card Frame SDF`；实测 {view0.OutlineFloatForTest("_Inner_Edge"):F4}）");
                        CheckNear(view0.OutlineFloatForTest("_Inner_Fallof"), 0.224f, 0.004f,
                                  $"★ … `_Inner_Fallof` = **0.224**（实测 {view0.OutlineFloatForTest("_Inner_Fallof"):F4}）");
                        CheckNear(view0.OutlineFloatForTest("_SpriteAlphaEdge_Inner"), 0.043f, 0.004f,
                                  $"★ … `_SpriteAlphaEdge_Inner` = **0.043**（实测 {view0.OutlineFloatForTest("_SpriteAlphaEdge_Inner"):F4}）");
                        // ⑤ **真落到材质上**（0.2 s 线性）：补间推完 ⇒ `_Outline` = #0080FF α1
                        CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);
                        var land = view0.OutlineColor;
                        CheckNear(land.g, 0.5026951f, 0.01f, $"★ 推完 0.2s ⇒ 材质 `_Outline.g` = **0.5027**（实测 {land.g:F4}）");
                        CheckNear(land.a, 1f, 0.01f, $"★ … α = **1.0**（实测 {land.a:F4}）");
                        hb0.onExit();
                        CheckTrue(view0.OutlineTargetForTest.a <= 1e-4f,
                                  "★ 离开 ⇒ 目标 **alpha = 0**（原版 `ToggleHighlight(false)`）");
                        CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);
                        CheckTrue(view0.OutlineColor.a <= 0.02f,
                                  $"★ 离开推完 ⇒ 材质 α = **0**（实测 {view0.OutlineColor.a:F4}）");
                        // 🔴 **反向断言（灭自证）**：菜单悬停**不许碰战斗那层状态环**（`_rim`）。
                        //   判据 = 环的尺寸仍是 1.09×1.06 的 2.2810×3.5312、且目标 alpha 恒 0（从没被点亮）。
                        //   ⇒ 把悬停写回 `_rim`（旧实现）或把菜单尺寸/材质套到环上，这一组立刻红。
                        var rs = view0.RimSize;
                        CheckNear(rs.x, 1.09f * CardView.Width, 0.01f,
                                  $"★ 反向：战斗状态环尺寸仍是 **1.09 × 2.0927 = 2.2810**（实测 {rs.x:F4}）—— 悬停没碰它");
                        CheckNear(rs.y, 1.06f * CardView.Height, 0.01f,
                                  $"★ 反向：… 高 **1.06 × 3.3313 = 3.5312**（实测 {rs.y:F4}）");
                        CheckTrue(view0.RimTargetForTest.a <= 1e-4f && !view0.RimVisible,
                                  "★ 反向：悬停**从头到尾没点亮战斗状态环**（α 目标恒 0、环恒关）—— 两层是物理分开的");
                        // 同一页里**另一格**（没悬停过）也不受影响（防止把悬停做成全局/静态）
                        var cv1 = FindChild(cp, "CollectionCard_1");
                        var view1 = cv1 != null ? cv1.GetComponent<CardView>() : null;
                        CheckTrue(view1 != null, "（前提）第 2 格也是 `CardView`（反向断言要用一张没悬停过的卡）");
                        if (view1 != null)
                            CheckTrue(view1.OutlineTargetForTest.a <= 1e-4f,
                                      "★ 反向：没悬停过的那一格描边目标 α = **0**（悬停只作用在被指的那一张）");
                        // 🆕 **2026-10-26（`G1` · 补 `X2` 查出的自检缺口）**：`CollectionWindow.SetHoverSuppressedForTest`
                        //   **全仓 0 调用者** ⇒「指针按住 / 拖拽中 ⇒ 悬停被抑制」这条**从来没被断言覆盖过**。
                        //   判据 = 原版 `CollectionItem.OnPointerEnter` 第一句 `if (eventData.pointerDrag != null) return;`；
                        //   我们的等价物 = `CollectionWindow.HoverSuppressed`（`AttachCardHover` 的 `onEnter` 里那道闸）。
                        //   🔴 **两半都要断**：只断「抑制」那半 ⇒ 把实现改成「永远不亮」也照样绿（= 自证）。
                        //   🧨 改坏法：删掉 `AttachCardHover` 的 `&& !HoverSuppressed` ⇒ 第一条红；
                        //     删掉 `onEnter` 里那句 `v.SetHoverHighlight(true)` ⇒ 第二条红。
                        CollectionWindow.SetHoverSuppressedForTest(true);
                        hb0.onEnter();
                        CheckTrue(view0.OutlineTargetForTest.a <= 1e-4f,
                                  "★ 抑制档：指针按住期间 `onEnter` ⇒ 描边目标 α 仍 **0**（原版 `pointerDrag != null` 直接 return）");
                        hb0.onExit();
                        CollectionWindow.SetHoverSuppressedForTest(false);
                        hb0.onEnter();
                        CheckTrue(view0.OutlineTargetForTest.a > 0f,
                                  $"★ 放开抑制 ⇒ 同一个 `onEnter` **点亮**（α = {view0.OutlineTargetForTest.a:F4}）"
                                + " —— 灭自证：实现改成「永远不亮」必红");
                        hb0.onExit();                                       // 收尾还原（否则后面的断言跑在假状态上）
                        CheckTrue(view0.OutlineTargetForTest.a <= 1e-4f, "★ （还原）收尾后描边目标 α 回到 **0**");
                        CardTween.Mode = prevMode;
                    }
                }
                // 🆕 **2026-10-23（`C3`）选中 punch 的包络**（原版 `CollectionCard.SelectedHighlight(true)`）：
                //   判据 = `CollectionCard__SelectedHighlight.c` + `.rdata` 实读（punch = −Vector3.one × **0.05**、
                //   时长 **0.1**、vibrato **10**、elasticity **1.0**；向量怎么解出来的见 `PunchCardCell` 那段）。
                //   用**独立探针**跑（⛔ 不拿窗里的卡试：批处理要 `CardTween.Advance` 手动推进，
                //   推窗里那张会连带把详情窗的开窗补间一起推掉 ⇒ 后面那些断言的前提就变了）。
                //   🧨 改坏法：删掉 `DOPunchScale` 那一句 ⇒ 第二条红（途中缩放不动）；删掉 `OnComplete` 复位 ⇒ 第三条红。
                {
                    var prevMode = CardTween.Mode;
                    CardTween.Mode = DG.Tweening.UpdateType.Manual;
                    var punchGo = new GameObject("CardPunchProbe");
                    var pt = punchGo.transform;
                    pt.localScale = Vector3.one;
                    int punchBefore = CollectionWindow.CardPunchCountForTest;
                    CollectionWindow.PunchCardForTest(pt);
                    CheckTrue(CollectionWindow.CardPunchCountForTest == punchBefore + 1,
                              "★ 点卡格 ⇒ 起了**一次**选中 punch（原版 `CollectionCard.SelectedHighlight(true)`）");
                    // ⚠️ 取样点必须避开**零点**：punch 的包络是 `sin(t/duration × π × vibrato)`，
                    //   而原版 vibrato = **10** ⇒ `t = 0.1k × duration` 全是零交叉（0.05 s 正好是零点！
                    //   第一次写的就是 0.05，取样必得 1.0 = 假红）。取 **0.025 s = 0.25 × duration** ⇒ sin(2.5π) = **+1**（第一个峰）。
                    CardTween.Advance(0.025f);
                    CheckTrue(Mathf.Abs(pt.localScale.x - 1f) > 0.005f,
                              $"★ punch **途中真的在缩放**（t=0.025s 第一个峰时 scale.x = {pt.localScale.x:F4}；原版幅度 ±0.05）"
                            + "—— 幅度为 0 / 没建补间 都会是 1");
                    CardTween.Advance(0.2f);           // 推到头
                    CheckNear(pt.localScale.x, 1f, 0.002f,
                              $"★ punch 收尾 ⇒ 缩放**复位到 1**（实测 {pt.localScale.x:F4}）"
                            + "—— 不复位就会在那一格留一个非 1 的缩放（静默，且只在被点过的那一格看得见）");
                    Object.DestroyImmediate(punchGo);
                    CardTween.Mode = prevMode;
                }
                if (hb0 != null)
                {
                    int punchCnt = CollectionWindow.CardPunchCountForTest;
                    hb0.Click();
                    CheckTrue(CollectionWindow.CardPunchCountForTest == punchCnt + 1,
                              "★ 真点卡格这一下**也走同一条 punch**（`CardHit_0.onClick` 里调的不是另一份实现）");
                    // 🆕 **2026-10-24（A14）**：punch **打哪一级** —— 原版 `CollectionCard.SelectedHighlight(true)`
                    //   打的是 `CollectionCard.canvas`(CanvasGroup，`+0x60`) 的 transform，那个 GameObject
                    //   **名字就叫 `Content`**（prefab `Collection Card/Content`），底下是
                    //   **`Counter`（张数条）+ `CardUI`（卡体）** ⇒ **整格一起弹**（判据 / PPtr 落点见
                    //   `CollectionWindow.PunchCardCell` 那段）。我们对应的那一级 = 每格 `Content <i>` 容器。
                    //   🧨 改坏法：把 `RebuildCardsCells` / `RebuildStyleCells` 的点击回调改回
                    //   `PunchCardCell(v.transform)` ⇒ 第 1、2 条红（打到了卡根）；把张数条挪到 `Content` 外面
                    //   ⇒ 第 3 条红；把 punch 抬到视口/窗根 ⇒ 第 4 条红。
                    var punchT = CollectionWindow.LastPunchTargetForTest;
                    var cardRoot0 = cp != null ? FindChild(cp, "CollectionCard_0") : null;
                    CheckTrue(punchT != null && cardRoot0 != null,
                              "（前提）拿得到 punch 作用节点 + 第 1 格的卡根");
                    if (punchT != null && cardRoot0 != null)
                    {
                        CheckTrue(punchT.name == "Content 0",
                                  $"★ punch 打在**格容器**上（实测 `{punchT.name}`；原版 = `Collection Card/Content`）");
                        CheckTrue(punchT != cardRoot0,
                                  "★ …**不是** `CardView` 的根（原版卡体是 `Content` 的孩子 `CardUI`；⛔ 别打岔到卡根）");
                        CheckTrue(FindChild(punchT, "Counter 0") != null,
                                  "★ …那一级底下**同时挂着张数条 `Counter 0`**（原版 `Content` = `{Counter, CardUI}` ⇒ 整格一起弹）");
                        CheckTrue(punchT.parent != null && punchT.parent.name == "Viewport",
                                  "★ …它是**格内**那一级（父 = `Viewport`），⛔ 不是视口 / 窗根");
                    }
                    // 🆕 **2026-10-24（A18）入场动画**（原版 `CollectionCard.AnimateIn`；判据 / 四个参数
                    //   → `CollectionWindow` 的「入场动画」那一节头部：`0.1` 步长 · `0.7` 时长 · `alpha 1` ·
                    //   `From(y + 200)`，全部从 `GameAssembly.dll` `.rdata` 现读）。三件事分开验：
                    //   ① 结构：格容器 `Content <i>` 上挂着 `CanvasGroup`（原版 `CollectionCard.canvas`，`+0x60`）
                    //   ② `CellFade` 真能淡到**三套后端**（卡面 / 张数条底 / 张数条字 ——
                    //      `CanvasGroup` 只乘得到 `CanvasRenderer`，我们那两套是世界空间 `MeshRenderer`）
                    //   ③ 补间本身：起手 = `alpha 0` + `y+200`、`Duration = 延迟 + 0.7`、走完回终值
                    //   🧨 改坏法：删掉容器上那颗 `CanvasGroup` ⇒ ① 红；`CellFade.SetAlpha` 里删掉
                    //   `card.SetAlpha` / `bars[].SetTint` / `labels[].SetColor` 任一条 ⇒ ② 对应那条红；
                    //   把 `CardAnimateRise` 改成 0、或把 `order` 写死 0 ⇒ ③ 第一 / 第四条红。
                    if (punchT != null)
                    {
                        var cg0 = punchT.GetComponent<CanvasGroup>();
                        CheckTrue(cg0 != null,
                                  "★ 入场动画 · 格容器 `Content 0` 上挂着 `CanvasGroup`"
                                + "（原版 `CollectionCard.canvas`，字段 `+0x60` —— `AnimateIn` 的 `set_alpha`/`DOFade` 打的就是它）");
                        var view0b = cardRoot0 != null ? cardRoot0.GetComponent<CardView>() : null;
                        var fade0 = view0b != null ? CollectionWindow.MakeCellFadeForTest(punchT, view0b) : null;
                        CheckTrue(fade0 != null, "（前提）拿得到第 1 格的 `CellFade`（下面四条都靠它）");
                        if (fade0 != null && view0b != null)
                        {
                            fade0.SetAlpha(0.5f);
                            CheckNear(view0b.Alpha, 0.5f, 0.001f,
                                      "★ 淡入作用到**卡面**（`CardView.SetAlpha`；`CanvasGroup` 传不到自建网格）");
                            CheckTrue(cg0 != null && Mathf.Abs(cg0.alpha - 0.5f) < 0.001f,
                                      "★ …以及原版那颗 `CanvasGroup`（`Content` 上）");
                            var ctr0 = FindChild(punchT, "Counter 0");
                            var bar0 = ctr0 != null ? ctr0.GetComponent<ImageQuad>() : null;
                            CheckTrue(bar0 != null && Mathf.Abs(bar0.Tint.a - 0.5f) < 0.001f,
                                      "★ …以及**张数条底图**（世界空间 `ImageQuad`，`CanvasGroup` 管不到）");
                            var lb0 = ctr0 != null ? ctr0.GetComponentInChildren<Label>(true) : null;
                            CheckTrue(lb0 != null && Mathf.Abs(lb0.color.a - 0.5f) < 0.001f,
                                      "★ …以及**张数条那行字**（`Label` 走 TMP 顶点色，不吃材质 `_Color`）");
                            fade0.SetAlpha(1f);
                            CheckNear(view0b.Alpha, 1f, 0.001f,
                                      "…复位到 1（本段不改窗的真状态：后面所有截图 / 版面断言都靠它）");
                        }
                    }
                    // ③ 补间本身 —— **独立探针**（同上面 punch 那条的做法：批处理要 `CardTween.Advance`
                    //   手动推；⛔ 别拿窗里真格子推，推完会改它的位置/透明度，后面版面断言全乱）。
                    {
                        var prevMode = CardTween.Mode;
                        CardTween.Mode = DG.Tweening.UpdateType.Manual;
                        var probeGo = new GameObject("CardAnimateInProbe", typeof(RectTransform));
                        var t0 = probeGo.transform;
                        const float endY = -123f;          // 终值取个非 0 的数：「+200 没生效」也能被判出来
                        t0.localPosition = new Vector3(0f, endY, 0f);
                        probeGo.AddComponent<CanvasGroup>();
                        var fd = CollectionWindow.MakeCellFadeForTest(t0, null);
                        CheckTrue(fd != null, "（前提）探针拿到了 `CellFade`");
                        var tw0 = CollectionWindow.BuildCardAnimateInTween(t0, fd, 0);
                        CheckTrue(tw0 != null, "（前提）建出了入场补间");
                        if (tw0 != null && fd != null)
                        {
                            float dur0 = DG.Tweening.TweenExtensions.Duration(tw0);   // ⚠️ 先记下来：走完之后 DOTween 会把这条回收，`Duration()` 不可靠
                            CheckNear(t0.localPosition.y, endY + CollectionWindow.CardAnimateRise, 0.01f,
                                      "★ 起手位置 = **终值 y + 200**（原版 `DOLocalMoveY(y)` `.From(y + 200)`；"
                                    + "`0x1834B3258` 实读 = 200）—— 与终值不同");
                            CheckTrue(Mathf.Abs(fd.Alpha) < 0.001f,
                                      "★ 起手 alpha = **0**（原版 `CanvasGroup.set_alpha(0)`）—— 与终值 1 不同");
                            CheckNear(dur0, CollectionWindow.CardAnimateDuration, 0.001f,
                                      "★ order 0 ⇒ 总时长 = **0.7 s**（没有延迟 + 0.7 那条补间）");
                            CardTween.Advance(0.35f);
                            CheckTrue(fd.Alpha > 0.05f && fd.Alpha < 0.95f,
                                      $"★ 半程真的在淡（t = 0.35 s 时 alpha = {fd.Alpha:F3}）");
                            CardTween.Advance(1.0f);
                            CheckNear(fd.Alpha, 1f, 0.001f,
                                      $"★ 动画走完 ⇒ alpha 回到终值 1（实测 {fd.Alpha:F4}）");
                            CheckNear(t0.localPosition.y, endY, 0.01f,
                                      $"★ …位置也回到终值（实测 {t0.localPosition.y:F2}）");

                            // ④ **格子顺序决定延迟**：order 2 比 order 0 多等 2 × 0.1 s
                            var t2Go = new GameObject("CardAnimateInProbe2", typeof(RectTransform));
                            var t2 = t2Go.transform;
                            t2.localPosition = new Vector3(0f, endY, 0f);
                            var fd2 = CollectionWindow.MakeCellFadeForTest(t2, null);
                            var tw2 = CollectionWindow.BuildCardAnimateInTween(t2, fd2, 2);
                            if (tw2 != null)
                            {
                                float dur2 = DG.Tweening.TweenExtensions.Duration(tw2);
                                CheckNear(dur2 - dur0, 2f * CollectionWindow.CardAnimateDelayStep, 0.001f,
                                          "★ 第 3 格比第 1 格**多等 2 × 0.1 s**"
                                        + "（原版 `AppendInterval(GetSiblingIndex() * 0.1)`）");
                                CardTween.Advance(0.15f);      // 还在 0.2 s 的延迟里
                                CheckTrue(fd2 != null && fd2.Alpha < 0.001f,
                                          "★ …而且它**真的在等**（t = 0.15 s 时 alpha 还是 0）");
                                CardTween.Advance(1.0f);       // 推完 —— ⛔ 别留一条活的 Manual 补间给后面的段
                                // 🔴 **2026-10-24（`H2` · `E4` 顺手发现 1）**：下面这一句在**编辑器批处理里是静默 no-op**
                                //   —— `TweenExtensions.Kill` 的 IL 头三条就是 `if (!DOTween.initialized) return;`
                                //   （判据 `E4_补间掐杀与F4判别式.md` 一·1；`!Application.isPlaying` ⇒ `initialized` 永远 false）。
                                //   **不影响断言**：① 补间在上面那 `Advance(1.0f)` 里已走到终点（0.15 + 1.0 = 1.15 s > 0.9 s
                                //   总时长）⇒ `autoKill` 已把它 despawn（`tw2` 只是个池化壳，⛔ 别拿它判身份）；
                                //   ② 紧接着 `DestroyImmediate(t2Go)` 把目标整个拆掉；③ 本段之后没有任何断言读 `tw2` 状态。
                                //   ⛔ 别改成 `DOTween.Kill(t2)` —— 这条 `Sequence` 的 `target` 是 **null**
                                //   （`CardTween.Use` 只 `SetLink`、不 `SetTarget`），而 `DOTween.Kill(targetOrId)` 只匹配
                                //   顶层补间的 `id`/`target`、**不看嵌套子补间** ⇒ 那样写照样 no-op，还更容易被误读成「有牙」。
                                DG.Tweening.TweenExtensions.Kill(tw2);
                            }
                            Object.DestroyImmediate(t2Go);
                        }
                        Object.DestroyImmediate(probeGo);
                        CardTween.Mode = prevMode;
                    }
                    // ⑤ **批处理不播**（`CollectionWindow.PlayCardAnimateIn` 里那道 `Application.isPlaying` 闸）：
                    //   没有帧循环 ⇒ 播了整页卡会停在 `alpha 0 / y+200`。⛔ 删那道闸 ⇒ 本页卡变半透明、位置也错。
                    Check(CollectionWindow.CardAnimateInCountForTest, 0,
                          "★ 批处理（`Application.isPlaying == false`）⇒ **一次入场动画都不起**"
                        + "（起了 = 没有帧循环推进 ⇒ 卡停在 alpha 0）");
                    var cell0View = win.CardsCells.Count > 0 ? win.CardsCells[0].GetComponent<CardView>() : null;
                    CheckTrue(cell0View != null && Mathf.Abs(cell0View.Alpha - 1f) < 0.001f,
                              "★ …因此第 1 格的卡是**实心**的（`Alpha == 1`，不是停在 0）");
                    var cd = CollectionWindow.LastCardDetail;
                    CheckTrue(cd != null, "点第 1 格 ⇒ **开出了卡片详情窗**");
                    if (cd != null)
                    {
                        var disp = FindChild(cd.transform, "Card Display");
                        CheckAt(disp, 584f, 1336f, 106f, 974f, "`Card Display`（752 × 868）");
                        // 卡本体尺寸：量**渲出来的**网格包围盒（原版 = 523.25 × 832.75）
                        // 🔴 **只能量「卡框」那一层** —— 整个 `2DCard` 的包围盒**必然**比卡大一圈，
                        //    而那两个来源**都跟「立绘越出」无关**：
                        //    ① 「软光/影」那层（`Card Highlight And Shadow`）**原版就是 4.4281² 的方图**
                        //       ⇒ **1106.9px**（= 4.4281 × 250 px/卡单位）。
                        //       ⚠️ **2026-10-10 订正（铁律 5）**：原来这里把 1106.9 记成
                        //       「**立绘抠图的溢出**撑大、是『角色越出卡框』那一层的功劳」——
                        //       那是**量错了对象**：`_artFront` 那块板最大也只有 ≈847px
                        //       （立绘 fit 进 561.25×814.25 的框矩形、再乘 `ArtCoverMargin` 1.04）；
                        //       1106.9 **恰好等于**那层方图的尺寸，来源就是它。
                        //    ② 立绘底层 `_art` 本身**按设计**就比框矩形大 4%（`ArtCoverMargin`），
                        //       而且它故意画在框**之下**（由框的 alpha 剪出来）⇒ 也不该拿整棵子量。
                        //    ⇒ 这条「只找 `frame_` 那一层」**不是绕行、是唯一能用的量法**（⛔ 别改回「直量整棵子」：
                        //      实测会给 1106.9 而不是 814.25，当场误报）。
                        //    卡框那层的贴图名以 **`frame_`** 开头（`CardArt.Frame(faction, rarity)` 给的，
                        //    落在 `Art/cards/frame_<阵营>[_strat]_tier<N>`）；
                        //    🔴 **别用 `_tier` 去找** —— SDF 那层叫 `<阵营>_tier<N>`（在 `Art/card_sdf/`），
                        //    也带 `_tier`，而且它就是上面①那张 1106.9px 的方图（实测踩过）。
                        var mrs = disp != null ? disp.GetComponentsInChildren<MeshRenderer>(true) : null;
                        if (mrs != null && mrs.Length > 0)
                        {
                            Bounds? frameB = null;
                            foreach (var mr in mrs)
                            {
                                var tx = mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null;
                                if (tx != null && tx.name.StartsWith("frame_")) { frameB = mr.bounds; break; }
                            }
                            CheckTrue(frameB.HasValue, $"找到了卡框那一层（{mrs.Length} 层网格里贴图名以 `frame_` 开头的那个）");
                            if (frameB.HasValue)
                            {
                                // 🔴 **2026-09-27 用户拍板：卡框改成【按固定矩形画】**（原来是「裁到 bbox 再等比内接」——
                                //    **那个设计是错的**）。硬证据：原版 `2DCard/CardFrame` 那个 Image 的
                                //    **`m_PreserveAspect = 0`** ⇒ 原版就是把框贴图**拉伸**进这个固定矩形的。
                                //    改之前：四档画形不同（bbox w/h：tier1/2 = 0.672、tier3/4 = 0.733，而矩形是 0.6893）
                                //    ⇒ 一律用 tier4 会让每张卡的框**矮 5.7%**（768.2）；改完**四档都是 814.25**（= 矩形本身）。
                                CheckNear(frameB.Value.size.y * 108f, 814.25f, 6f,
                                          $"**卡框渲出来的高 = {frameB.Value.size.y * 108f:F1}px**（原版 `CardFrame` = 561.25 × **814.25**；"
                                        + "按固定矩形画 ⇒ **与卡框档无关**）");
                                // ⚠️ **宽对不上，别拿它当判据**：我量到 535.19px、原版 `CardFrame` 是 561.25 —— 差 ~26px。
                                //    **还没查清**（疑 `CardView` 那层按贴图自身宽高比画、而原版 `CardFrame` 的 rect
                                //    比贴图比例宽）。已记进 `项目任务.md` §三 第 15 条，**不在本轮擅自改卡面**。
                                Debug.Log(P + $"   · 卡框宽实测 {frameB.Value.size.x * 108f:F1}px（原版 `CardFrame` 561.25 —— 差 "
                                          + $"{(frameB.Value.size.x * 108f - 561.25f):F1}px，**还没查清**，见 §三 第 15 条）");
                            }
                            CheckTrue(mrs.Length > 3, $"卡面上画了 {mrs.Length} 层网格（不是空卡位）");
                        }
                        // 三块面板的标题 —— 🔴 **2026-10-12 用户裁定：「请你恢复」**（口径 12 天内改了两次）
                        //   ⚠️ **旧口径留痕（铁律 5）**：本节原来断的是这两块**都不建**（用户 2026-09-27
                        //     原话「直接全部卡都是最高级别的卡框，这样就不用升级了。也不需要合成卡牌了。」）
                        //     —— **那一条已被 2026-10-12 的裁定取代**（原话：「……还是恢复？**请你恢复**。
                        //     我说的是不需要升级卡牌系统，**默认最高级就是不能点击升级卡牌按钮**」）。
                        //   ⇒ 现在断「**两块都在**」；「升级」那颗钮的**可交互度**由下面「三块面板的动作」
                        //     那一节（`本文件` 附近）单独钉 —— 两件事分开验。
                        //   🔴 **2026-10-18 再改判（铁律 5 · 就地订正上面「原版文案」那句）**：用户当天把
                        //     「升级」那块**一颗钮改成『降级 / 升级』两颗**、标题也改成他指定的字面量
                        //     「将此张牌降级或升级」（判据 → `资料/普查产出_1018/W8_副本升级面板_按用户口径.md`）
                        //     ⇒ 标题**不再是原版文案**（⛔ 别再断 `StartsWith("Upgrade this card")`）。
                        // 🔴 **2026-10-18 用户口径（判据 → `资料/普查产出_1018/W14_详情窗按钮与副本文案.md` §二·3）**：
                        //   这一格的标题已从**写死的英文**改成走**官方本地化键** `MenuDeck/Craft/CraftDescription`
                        //   （zh 列 = **创建这张牌的副本** · en 列 = `Create a copy of this card`）⇒ **随语档**。
                        //   ⛔ 别再写死英文 `"Create a copy of this card"` —— 出厂语言 = 中文时必红。
                        //   ⚠️ 中 / 英两档用**字面量**（⛔ 不从 `Loc.T(键)` 反读，那是自证）；
                        //      其余 10 语档才回落 `Loc.T(键)`（同一个键、同一张官方 13 语表）。
                        string wantCraftTitle = Loc.Current == AvailableLanguages.Chinese ? "创建这张牌的副本"
                                              : Loc.Current == AvailableLanguages.English ? "Create a copy of this card"
                                              : Loc.T("MenuDeck/Craft/CraftDescription");
                        CheckTrue(cd.TitleOf("Crafting") == wantCraftTitle,
                                  $"★「创建副本」那一块**在**且标题 = 官方词条 `MenuDeck/Craft/CraftDescription`"
                                + $"（实测「{cd.TitleOf("Crafting")}」，期望「{wantCraftTitle}」）"
                                + " —— 用户 2026-10-18 口径：两句英文改中文（W14 §二·3）");
                        var upTitleTxt = cd.TitleOf("Upgrade");
                        // 2026-10-18：用户拍板改法（判据 → `W8_副本升级面板_按用户口径.md`）：
                        //   标题从原版文案 `Upgrade this card\nto level N` 改成**用户指定的字面量**
                        //   「将此张牌降级或升级」——用户原话：「上面的文字『将此张牌升级至等级x』
                        //   改为『将此张牌降级或升级』」。一颗钮改两颗之后，本来也没有「升到第 N 级」这个说法了。
                        //   ⛔ 别断回 `StartsWith("Upgrade this card")`，也别把这句往 `official_loc` 里塞键。
                        CheckText(upTitleTxt, "将此张牌降级或升级",
                                  $"★「升级」那一块**在**且标题 = 用户 2026-10-18 指定的字面量（实测「{upTitleTxt}」）"
                                + " —— 判据 → `资料/普查产出_1018/W8_副本升级面板_按用户口径.md`");
                        CheckText(cd.TitleOf("AltArt"), "Alternate art",
                                  "异画面板标题 —— ⚠️ 原版这一格印的是**升级文案**（复制粘贴 bug），**我们不抄那个 bug**（出声）");
                        // 计数条：格式 = `x{min(拥有,卡组上限)}` + `"/ "` + `{拥有−该数}`
                        var cnt = cd.Counter;
                        CheckTrue(cnt != null, "`Card Counter` 在");
                        if (cnt != null)
                        {
                            var t1 = FindChild(cnt, "Counter");
                            string s1 = TextOf(t1);
                            CheckTrue(s1.StartsWith("x"),
                                      $"计数条左数 = **{s1}**（原版格式串 `\"x{{0}}\"`，= min(拥有, 卡组上限)）");
                            CheckText(TextOf(FindChild(cnt, "Slash")), "/ ",
                                      "中间那个 `/ ` **是写死的**（原版代码不改它）");
                            var t2 = FindChild(cnt, "Duplicates text");
                            CheckTrue(t2 != null && int.TryParse(TextOf(t2), out _),
                                      $"右数 = **{TextOf(t2)}**（= 多余副本数；> 0 ⇒ 走 `Duplicate Counter` 那一支）");
                        }
                        // ============================================================ 🔴 2026-10-13（A404）
                        // 计数条那三颗 TMP 的**自适应**（原版三颗都带 `auto`）。判据 → `资料/阶段二_卡片详情窗_原版规格.md:90-92`
                        // （§三 表）+ 逐颗现读原版 MB（`bundle_scenes_scenes_mainmenuwarpforge`）：
                        //   `…/Counters/Counter`      `fs52.6`(auto **25–52.6**) sd(**73.283**,70.586) 折行 **0**（MB 1892）
                        //   `…/Counters/Slash`        `fs57`  (auto **25–57**)   sd(**28.02**,70.586)  折行 **0**（MB 1848）
                        //   `…/Slash/Duplicates text` `fs52.6`(auto **18–52.6**) sd(**44.09**,50)      折行 **0**（MB 1823）
                        //   三颗都是 `m_enableAutoSizing = 1` + `m_TextWrappingMode = 0`。
                        // 🔴 期望值**全是原版值**（⛔ 不写我们那一侧传进去的实参）：容器宽 = 原版 `m_SizeDelta.x`、
                        //   min/max = 原版 `m_fontSizeMin/Max` 的 **px 口径**（`Label.FontSizeToPx`）、折行档 = 原版 0。
                        // 🔴 **为什么必须断「渲染宽/高 ≤ 框」**（`CLAUDE.md` §二 `AutoFitBox` 那条教训）：
                        //   只比字号的话，「字号字段对、字却溢出框」照样全绿；而这三颗**正是靠自适应收缩**的
                        //   （`Duplicates text` 的框高 **50 < 字号 52.6**，正本 §三 末那条注）。量的东西 =
                        //   **TMP 自己渲出来那块 `textBounds`**（🆕 **2026-10-16（A796′）换口** → `LabelRenderedPx`；
                        //   换口前是 `Label.WorldW/WorldH` = 字段缓存 `_tmpW/_tmpH`，两法同源 ⇒ 当天逐位同值）——
                        //   ⛔ 不是节点位置、⛔ 更不是「我们传了多少」。
                        // **改坏法**：① 三处 `wrapPx` 改回 `0f` ⇒ 自适应那一段整段不执行（它写在 `if (wrapPx > 0f)` 里）
                        //   ⇒ `AutoSizing` 恒 false、`fontSizeMin/Max` 停在 TMP 出厂 `0/0`、TMP 容器宽停在出厂值 ⇒ 红；
                        //   ② `autoMinPx` 传错（如 25 → 10）⇒ `FontSizeMin` 那条红；③ 删掉 `SetWrapping(false)` ⇒ 折行档回 1 ⇒ 红。
                        // ⚠️ **如实说清判别力**（实测过这三颗的文案）：咬住 ① 的是 `AutoSizing` / `fontSizeMin/Max` /
                        //   **TMP 容器宽**那 **12 条**（字段级、唯一判别式）。宽/高那两条**对 ① 不一定有判别力** ——
                        //   本夹具的文案是 `x1`（战将 `DeckCap(rarity, type) = 1`，2026-10-19 批次 D 订正；
                        //   改前记的是 `x2` —— 那时夹具拿到的是非战将）/ `/ ` / **一位数**（`spares = 9`：
                        //   `NeedCopies[4] − NeedCopies[1] = 9 − 0`，见 `CardDetailPopup.NeedCopies`），
                        //   按 52.6 / 57px **不缩**大概也塞得进 73.28 / 28.02 / 44.09 这三个框
                        //   （⚠️ 那三个字的墨宽**没在实机上量过**，这一句是估的）⇒ 那两条是**另一族**
                        //   （「字号对而溢出」）的守卫，**不是 A404 的判别式**（同 `Editor/RewardsScene.cs` 里记过的同一件事）。
                        {
                            var fitCases = new[]
                            {
                                // 节点名 · 原版容器宽 `sd.x` · 原版容器高 `sd.y` · 原版 auto 的 min/max（px）
                                new { Name = "Counter",         W = 73.283f, H = 70.586f, Min = 25f, Max = 52.6f },
                                new { Name = "Slash",           W = 28.02f,  H = 70.586f, Min = 25f, Max = 57f   },
                                new { Name = "Duplicates text", W = 44.09f,  H = 50f,     Min = 18f, Max = 52.6f },
                            };
                            foreach (var fc in fitCases)
                            {
                                var ft = FindChild(cnt, fc.Name);
                                var flb = ft != null ? ft.GetComponentInChildren<Label>() : null;
                                CheckTrue(flb != null, $"`{fc.Name}` 上找得到 `Label`（A404 要量它的自适应）");
                                if (flb == null) continue;
                                var ftmp = ft.GetComponentInChildren<TMPro.TextMeshPro>(true);
                                CheckTrue(flb.AutoSizing,
                                          $"★ `{fc.Name}` **真的开了自适应**（原版 `m_enableAutoSizing = 1`）"
                                        + " —— 改坏法：`CardDetailPopup` 那三处 `wrapPx` 传回 `0f` ⇒ 这一段永不执行、本行红");
                                // 🔴 **2026-10-18（`N55`）**：口径由静态 Cjk 口改成按 `ActualFace` ——
                                //   `CardDetailPopup` 这些字段是 `LatinCardBody`（`Shell/CardDetailPopup.cs` 的 `LatinCardBody`）；
                                //   `EmPerFontSize` 自本日起按面分两支（CJK 0.0948 / 拉丁 0.1）⇒ 静态口恒差 5.2%。
                                CheckNear(flb.FontSizeToPxNow(flb.FontSizeMin, flb.ActualFace), fc.Min, 0.05f,
                                          $"`{fc.Name}` 自适应下限 = 原版 `m_fontSizeMin` **{fc.Min:F1}px**");
                                CheckNear(flb.FontSizeToPxNow(flb.FontSizeMax, flb.ActualFace), fc.Max, 0.05f,
                                          $"`{fc.Name}` 自适应上限 = 原版 `m_fontSizeMax` **{fc.Max:F1}px**"
                                        + "（= 原版 `m_fontSize`；⛔ 不是我们那一侧的字号常量）");
                                Check(flb.WrappingMode, 0,
                                      $"`{fc.Name}` 折行档 = **原版 `m_TextWrappingMode = 0`**（`NoWrap`）"
                                    + " —— 改坏法：删掉紧跟 `MenuDraw.Text` 的那句 `SetWrapping(false)` ⇒ 回 1");
                                CheckNear(ftmp != null ? ftmp.rectTransform.sizeDelta.x : -1f, LayoutSpace.Px(fc.W),
                                          2e-4f, $"`{fc.Name}` 的 **TMP 容器宽 = 原版 `m_SizeDelta.x`**（{fc.W:F3}px）"
                                               + " —— 改坏法：`wrapPx` 传 `0f` ⇒ 容器停在出厂值");
                                // 🆕 **2026-10-16（A796′）换口**：宽/高改走 `LabelRenderedPx`（TMP `textBounds`）
                                //    —— ⛔ 不再读**字段缓存** `Label.WorldW/H`（就是 `AutoFitBox` 教训里那个口）。
                                var flbSz = LabelRenderedPx(flb);
                                float rw = flbSz.x, rh = flbSz.y;
                                CheckTrue(rw <= fc.W + 1.5f && rh <= fc.H + 1.5f,
                                          $"`{fc.Name}` **渲出来 {rw:F1}×{rh:F1}px ≤ 原版框 {fc.W:F2}×{fc.H:F2}**"
                                        + "（容差 1.5px = TMP 二分收敛粒度 `TextMeshPro.cs:4139`）"
                                        + " —— 只比字号会漏掉「字号对而溢出」那一族（`AutoFitBox` 教训）");
                            }
                        }
                        // ============================================================ 🔴 2026-10-13（A574 + A575）
                        // 计数条那三颗 TMP 的 **`m_fontSizeBase`**（自适应二分的起点）+ **水平对齐档**。
                        // 判据 = **逐颗现读原版 MB**（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/`）：
                        //   `…/Counters/Counter`      pid `1892`  `m_fontSizeBase = 36.0`  `m_HorizontalAlignment = 4`(**Right**)
                        //   `…/Counters/Slash`        pid `1848`  `m_fontSizeBase = 36.0`  `m_HorizontalAlignment = 2`(Center)
                        //   `…/Slash/Duplicates text` pid `1823`  `m_fontSizeBase = 35.0`  `m_HorizontalAlignment = 1`(**Left**)
                        //   （枚举 → `Runtime/TMP/TMP_Text.cs:74-77`：`Left=1, Center=2, Right=4, Justified=8, Flush=0x10`
                        //    ⇒ `4` 是 **`Right`**、⛔ **不是** `Flush`；正本 `:90` 原写 `4(Flush)`，已被调度台就地订正）
                        // 三颗的**框**（= 我们传进去那个矩形，与正本 §三 的 `rect` 同源）→ `资料/阶段二_卡片详情窗_原版规格.md:90-92`：
                        //   `Counter` 869.41→**942.69** · `Slash` 942.69→**970.71** · `Duplicates text` **970.71**→1014.80
                        //   （三格**首尾相接**：原版的 `Right`/`Center`/`Left` 正好让 `x2` 贴 `/`、`9` 从 `/` 后起 ⇒ 读起来是连续的 `x2/ 9`）
                        // 🔴 期望值**全是原版字面量**（⛔ 不读 `CardDetailPopup.CcBaseX1` / `CcX1R` —— 那是同义反复）。
                        // **改坏法**（四条，各红在不同的一组上）：
                        //   ① 三处 `autoBasePx` 实参去掉（回到不传第 10 参）⇒ base 落到标称档 52.6/57/52.6 ⇒ 那三条红；
                        //   ② 去掉 `MenuDraw.AlignRight(lbC, rC)` ⇒ 第 1 颗退回出厂档 `Center`，整块**右缘**落到
                        //      「rect 心 906.05 + 块宽/2」（= 差 `36.64 − 块宽/2`）⇒ 红；
                        //   ③ 去掉 `MenuDraw.AlignLeft(lbX2, rX2)` ⇒ 第 3 颗**左缘**落到「rect 心 992.755 − 块宽/2」⇒ 红；
                        //   ④ 把 `Slash` 也接上 `Align*` ⇒ 第 2 颗的**块心**离开 956.70（挪半个块宽）⇒ 红。
                        // ⚠️ **如实说清判别力**：②③④ 咬住的是「**整块被挪了半个块宽**」——它们对 **A575** 有判别力，
                        //   对 **A574（base）没有判别力**：base 只改二分起点、收敛结果不变 ⇒ 落位一动不动。
                        //   A574 的判别式是下面 `FontSizeBase` 那三条。两笔各断各的，⛔ 别互相顶。
                        // ⚠️ 判别力还依赖「**渲染块比容器窄**」（否则 `Center` 与 `Right` 的落位重合）：本夹具三颗
                        //   的块宽都远小于容器宽（`x2` < 73.28 · `/ ` < 28.02 · 一位数 < 44.09）⇒ 差值 = 半个块宽。
                        {
                            // 节点名 · 原版 `m_fontSizeBase`(px) · 原版对齐档 · 对齐后的**锚点**该落在哪(原版 rect 的边/心)
                            var alignCases = new[]
                            {
                                new { Name = "Counter",         BasePx = 36f, Mode = "Right",  Want = 942.69f,  What = "右缘" },
                                new { Name = "Slash",           BasePx = 36f, Mode = "Center", Want = 956.70f,  What = "块心" },
                                new { Name = "Duplicates text", BasePx = 35f, Mode = "Left",   Want = 970.71f,  What = "左缘" },
                            };
                            foreach (var ac in alignCases)
                            {
                                var at = FindChild(cnt, ac.Name);
                                var alb = at != null ? at.GetComponentInChildren<Label>() : null;
                                CheckTrue(alb != null, $"`{ac.Name}` 上找得到 `Label`（A574/A575 要量 base 与落位）");
                                if (alb == null) continue;
                                // ---- A574：`m_fontSizeBase`（自适应起点；`Label.FontSizeBase` 反射读 TMP 那个 protected 字段）
                                // 🔴 **2026-10-18（`N55`）**：口径按 `ActualFace`（本件字段 = `LatinCardBody`，理由同上）
                                CheckNear(alb.FontSizeToPxNow(alb.FontSizeBase, alb.ActualFace), ac.BasePx, 0.5f,
                                          $"`{ac.Name}` 的自适应 **base = 原版 `m_fontSizeBase` {ac.BasePx:F0}px**"
                                        + " —— 改坏法：`CardDetailPopup` 那三处不传第 10 参 ⇒ 落到标称档 52.6/57/52.6");
                                // ---- A575：整块的落位。块宽是**渲出来的**真测量（TMP 自己那块 `textBounds`
                                //     —— 🆕 **2026-10-16（A796′）换口**：改走 `LabelRenderedPx`，⛔ 不再读字段缓存 `Label.WorldW`），
                                //     ⛔ 不是回读我们传进去的实参；`MenuCheck.DesignPx` 与 `TitleLeftPx` 同一个设计空间口径。
                                float wpx = LabelRenderedPx(alb).x;
                                float cx = MenuCheck.DesignPx(alb.transform).x;
                                CheckTrue(wpx > 5f,
                                          $"（前提）`{ac.Name}` 的渲染块宽 {wpx:F2}px > 5 —— 量不出宽度时下面那条会**退化**"
                                        + "（`MenuDraw.Align*` 自己也有 `HasMeasuredWidth` 早退，这条防的是「两边一起变成 0」）");
                                float anchor = ac.Mode == "Right" ? cx + wpx * 0.5f
                                             : ac.Mode == "Left"  ? cx - wpx * 0.5f
                                             : cx;
                                CheckNear(anchor, ac.Want, 0.5f,
                                          $"`{ac.Name}` 的 **{ac.What} = {ac.Want:F2}px**（原版 rect + 原版对齐档 `{ac.Mode}`；"
                                        + $"量的是渲染块，块宽 {wpx:F1}px）"
                                        + " —— 改坏法：不调 `MenuDraw.Align*` ⇒ 退回出厂档 `Center`，整块挪半个块宽");
                            }
                        }
                        // ============================================================ 🔴 2026-10-13（A691）
                        // 计数条的**两支是两棵不同的树**（`spares > 0` / `spares == 0`）—— 本笔之前我们只把
                        // `Duplicate Counter` **改了个名**、几何/字号/结构全部沿用（= 两棵树当成一棵）。
                        // 判据 ①（分支）原版反编译 `decomp_full/CardCounterDisplay__Initialize.c`：
                        //   `iVar1 = 拥有` · `iVar2 = min(拥有, 卡组上限)` · `spares = iVar1 − iVar2`
                        //   `spares > 0` ⇒ `singleCounterContent.SetActive(0)` + `duplicateCounterContent.SetActive(1)`
                        //                  + `cardCounter = "x{min}"` + `duplicateCardCounter = "{spares}"`
                        //   `spares == 0`⇒ **反过来**，而且**只填 `singleCardCounter`**（`SetSingleCounter.c` 同款写法）
                        // 判据 ②（几何/字号各是各的）本笔**现读原版 prefab**（`bundle_scenes_scenes_mainmenuwarpforge`）：
                        //   `Card Counter` RT `1596` anchors(.5,.5) pos **(0,−329.5)** sd **(269.857, 79.37)**
                        //   ├ `Duplicate Counter` RT `1420` anchors(.5,.5) pos (0,0) sd **(239.991, 70.586)**
                        //   │   ├ `Background` RT `1140` stretch sd(0,0) · `m_PreserveAspect=1` · rot z 180°
                        //   │   └ `Counters` RT `1215` pos(−3.30002,−6.9) → `Counter` RT `1351` sd(73.283,70.586)
                        //   │       TMP `1892` fs **52.6** auto[25~52.6] base 36 `H=4`；另有 `Slash` / `Duplicates text`
                        //   │       / `Duplicate image` 三件
                        //   └ `Single Counter` RT `1531` anchors(.5,.5) pos (0,0) sd **(170, 50)** · GO `m_IsActive = False`
                        //       ├ `Background` RT `1513` stretch sd(0,0) · **同一个 sprite**（PathID `3969383734418133180`）· rot z 180°
                        //       └ `Counter` RT `1151` stretch sd(0,0) · TMP `1893`（= `singleCardCounter`，字段偏移 0x40）
                        //           fs **35** · auto[**25~35**] · base **36** · `m_HorizontalAlignment = 2`(Center) · 折行 0
                        //           ⇒ **没有** `Slash` / `Duplicates text` / `Duplicate image`
                        //        （⇒ 绝对矩形 `875, 844.50 → 1045, 894.50`）
                        // 🔴 期望值**从原版五元组现算**（`UguiRect.Child` = 本工程唯一那份锚点算法），⛔ **不读**
                        //    `CardDetailPopup.CcSg*` / `CcBg*` 那一族常量（那是自证）。
                        // 🔴 **两态都走**：本夹具默认 `spares = 9 > 0`（`CardProgress.Owned` 的「给足」口径）
                        //    ⇒ 先断 dup 支；再把拥有数压到 = 卡组上限（⇒ `spares == 0`）重建、断 single 支；
                        //    最后**原样还原**（数量 + 重建）—— 不给后面的断言与 `Shoot` 留个怪状态。
                        // **改坏法**（各红在不同的一条上）：
                        //   ① 回到「只改名、沿用它那套几何/字号」⇒「框 170×50」那条 + fs 那三条红；
                        //   ② 两支都建（去掉 `if (!dup)` 的早返回）⇒ 两条「另一支不在」红；
                        //   ③ single 支漏掉 `SetWrapping(false)` ⇒ 折行档那条红；
                        //   ④ 把 dup 那颗的 `AlignRight` 抄给 single ⇒「块心 = 960」那条红；
                        //   ⑤ `wrapPx` 传 `0f` ⇒ 自适应那四条（`AutoSizing` / min / max / TMP 容器宽）红。
                        // ⚠️ **如实说清判别力**：两支的 `m_AnchoredPosition` 都是 **(0,0)** ⇒ **两支矩形心逐位同值**
                        //    ⇒ `CheckAt` 那两条**分不出是哪一支**（它们只抓「节点摆到别处去了」）；真正分辨两支的是
                        //    **尺寸**（239.991×70.586 vs 170×50）与**结构**（那三件在不在）。
                        // ⚠️ **本段没覆盖的一档**：原版那颗 `Counter` 的 **`m_VerticalAlignment = 1024 (Bottom)`**
                        //    （现读 MB `1893`；dup 那三颗都是 `512 (Middle)`）—— 我们全工程按框内居中画 ⇒ **不在此断言**
                        //    （做不了的原因 → `WM3_SingleCounter.md` §七·1：要动 `Battle/Label.cs`，不在本笔白名单里）。
                        {
                            // 原版五元组 → 绝对矩形（父 = 全屏 `Card Options Panel` 0,0 → 1920,1080）
                            var rCC = UguiRect.Child(new PxRect(0f, 0f, 1920f, 1080f),
                                                     new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), UguiRect.P50c,
                                                     new Vector2(0f, -329.5f), new Vector2(269.857f, 79.37f));
                            var rDup2 = UguiRect.Child(rCC, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                       UguiRect.P50c, Vector2.zero, new Vector2(239.991f, 70.586f));
                            var rSg2 = UguiRect.Child(rCC, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                      UguiRect.P50c, Vector2.zero, new Vector2(170f, 50f));
                            // 「空节点 + `PxRect`」这一套把矩形记在 `sizeDelta` 上（`MenuDraw.ApplyPxRect` → `SetPxSize`）
                            Vector2 SizePx(Transform t)
                            {
                                var rt = t != null ? t.GetComponent<RectTransform>() : null;
                                return rt != null ? new Vector2(rt.sizeDelta.x * 108f, rt.sizeDelta.y * 108f)
                                                  : new Vector2(-1f, -1f);
                            }

                            var cnt2 = cd.Counter;
                            CheckTrue(cnt2 != null, "`Card Counter` 在（两支共同的父）");
                            // ---------- ① dup 支（本夹具当前状态：`spares = 9 > 0`）----------
                            var dupNode = FindChild(cnt2, "Duplicate Counter");
                            CheckTrue(dupNode != null,
                                      "★ `spares > 0` ⇒ **`Duplicate Counter` 那一支**（原版 `duplicateCounterContent.SetActive(1)`）");
                            if (dupNode != null)
                            {
                                var dupSize = SizePx(dupNode);
                                CheckNear(dupSize.x, rDup2.W, 1.5f,
                                          $"★ `Duplicate Counter` 框宽 = 原版 `m_SizeDelta.x` **239.991px**（{rDup2.W:F3}）");
                                CheckNear(dupSize.y, rDup2.H, 1.5f,
                                          $"…框高 = 原版 **70.586px**（{rDup2.H:F3}）");
                            }
                            CheckTrue(FindChild(cnt2, "Single Counter") == null,
                                      "★ …而且 `Single Counter` **不该在**（原版 `singleCounterContent.SetActive(0)`）"
                                    + " —— 改坏法：去掉 `if (!dup)` 的早返回、两支都建 ⇒ 这条红");
                            // ---------- ② 把 `spares` 压到 0 ⇒ 换另一支 ----------
                            // 🔴 **2026-10-19（批次 D · 红 #7）：上限必须带卡型** —— 本批排序后 `CardHit_0` =
                            //   战将 `UM47 Uriel Ventris`（`type=hero, rarity=rare`）；原版 `GetMaxCopiesInDeck` 对
                            //   督军返回 **1**（`decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`
                            //   ⇒ `DeckRules.CopyLimit` 同一档），详情窗也是按 `DeckCap(rarity, type)` 印 **x1**。
                            //   旧夹具用不带卡型的那一档（rare ⇒ 2）⇒ 期望 `x2`、实测 `x1`。
                            //   ⚠️ 改成带卡型后 `sgDelta` 随之自洽：`AddOwned` 落到 1 ⇒ `Spares` 仍 = 0（见 `Spares` 的 `min(o,cap)`）。
                            int sgCap = CardProgress.DeckCap(cd.Card.Rarity, cd.Card.Type);
                            int sgOwned = CardProgress.Owned(cd.Card.Id, cd.Card.Rarity);
                            int sgDelta = sgCap - sgOwned;               // ⇒ 拥有 = 卡组上限 ⇒ `spares == 0`
                            CardProgress.AddOwned(cd.Card.Id, cd.Card.Rarity, sgDelta);
                            Check(CardProgress.Spares(cd.Card.Id, cd.Card.Rarity), 0,
                                  $"（前提）拥有数 {sgOwned} → {sgCap} ⇒ `spares == 0`（原版换支的那个条件）");
                            cd.ShowCard(cd.Card);                        // 计数条按当前 `CardProgress` 重建
                            var cnt3 = cd.Counter;
                            var sgNode = FindChild(cnt3, "Single Counter");
                            CheckTrue(sgNode != null,
                                      "★ `spares == 0` ⇒ **`Single Counter` 那一支**（原版 `singleCounterContent.SetActive(1)`）");
                            CheckTrue(FindChild(cnt3, "Duplicate Counter") == null,
                                      "★ …而且 `Duplicate Counter` **不在了**（原版把那一支 `SetActive(0)`）");
                            if (sgNode != null)
                            {
                                var sgSize = SizePx(sgNode);
                                CheckNear(sgSize.x, rSg2.W, 1.5f,
                                          "★ `Single Counter` 框宽 = 原版 `m_SizeDelta.x` **170px**"
                                        + " —— ⛔ 不是 `Duplicate Counter` 那套 239.991（改坏法：两支共用一套框 ⇒ 本条红）");
                                CheckNear(sgSize.y, rSg2.H, 1.5f, "…框高 = 原版 **50px**");
                                CheckAt(sgNode, rSg2.x1, rSg2.x2, rSg2.y1, rSg2.y2,
                                        "`Single Counter` 落在原版矩形心 (960, 869.5) 上"
                                      + "（⚠️ 两支的 `m_AnchoredPosition` 都是 (0,0) ⇒ 本条**不是**分支判别式）");
                            }
                            // 那三件只长在 dup 那棵树上
                            CheckTrue(FindChild(cnt3, "Slash") == null, "…`Slash` **不在**（原版 `Single Counter` 子树里没有它）");
                            CheckTrue(FindChild(cnt3, "Duplicates text") == null, "…`Duplicates text` **不在**");
                            CheckTrue(FindChild(cnt3, "Duplicate image") == null, "…`Duplicate image` **不在**");
                            // 底图：两支**同一张 sprite**（原版两个 `Background` 的 `m_Sprite.m_PathID` 是同一个数）
                            var sgBg = FindChild(cnt3, "Background");
                            CheckArt(sgBg, "40K_main_deck_card_counter",
                                     "`Single Counter/Background` 还是那张底图（原版 `m_PreserveAspect = 1`）");
                            var sgQ = sgBg != null ? sgBg.GetComponentInChildren<ImageQuad>() : null;
                            if (sgQ != null)
                            {
                                float sgW = sgQ.WorldW * 108f, sgH = sgQ.WorldH * 108f;
                                CheckTrue(sgW > 5f && sgH > 5f,
                                          $"（前提）底图**真量得到**（{sgW:F1}×{sgH:F1}px）—— 否则下面那条会退化成恒真");
                                CheckTrue(sgW <= rSg2.W + 1.5f && sgH <= rSg2.H + 1.5f,
                                          $"底色**渲出来 {sgW:F1}×{sgH:F1}px ≤ 原版框 170×50**"
                                        + "（`m_PreserveAspect = 1` ⇒ 按图自身比例内接；若误用 dup 那套框会渲成 227.4×70.6 ⇒ 本条红）");
                            }
                            // 那颗 `Counter`（原版 = `singleCardCounter` 字段，0x40）
                            var sgT = FindChild(sgNode, "Counter");
                            CheckTrue(sgT != null, "`Single Counter/Counter` 在");
                            if (sgT != null)
                            {
                                CheckText(TextOf(sgT), "x" + sgCap,
                                          $"…文案 = **x{sgCap}**（原版格式串 `\"x{{0}}\"` 填 `min(拥有, 卡组上限)`；"
                                        + "⛔ 这一支**不印**多余副本数 —— 那个 `Duplicates text` 只长在 dup 那一支）");
                                var sgLb = sgT.GetComponentInChildren<Label>();
                                CheckTrue(sgLb != null, "…上找得到 `Label`（下面要量它的自适应与落位）");
                                if (sgLb != null)
                                {
                                    var sgTmp = sgT.GetComponentInChildren<TMPro.TextMeshPro>(true);
                                    CheckTrue(sgLb.AutoSizing,
                                              "★ …**真的开了自适应**（原版 MB `1893` 的 `m_enableAutoSizing = 1`）"
                                            + " —— 改坏法：`wrapPx` 传 `0f` ⇒ 自适应那一段整段不执行、本条红");
                                    // 🔴 **2026-10-18（`N55`）**：口径按 `ActualFace`（同族理由见 `fitCases` 之前那条）
                                    CheckNear(sgLb.FontSizeToPxNow(sgLb.FontSizeMax, sgLb.ActualFace), 35f, 0.05f,
                                              "★ …自适应上限 = 原版 `m_fontSizeMax` **35px**（= `m_fontSize`；⛔ 不是 dup 那颗的 52.6）");
                                    CheckNear(sgLb.FontSizeToPxNow(sgLb.FontSizeMin, sgLb.ActualFace), 25f, 0.05f,
                                              "…自适应下限 = 原版 `m_fontSizeMin` **25px**（⛔ 不是 `Duplicates text` 那个 18）");
                                    CheckNear(sgLb.FontSizeToPxNow(sgLb.FontSizeBase, sgLb.ActualFace), 36f, 0.5f,
                                              "…自适应 base = 原版 `m_fontSizeBase` **36px**"
                                            + " —— 改坏法：不传第 10 参 ⇒ base 落到字号本身（35）⇒ 本条红");
                                    Check(sgLb.WrappingMode, 0,
                                          "…折行档 = 原版 `m_TextWrappingMode = 0`（`NoWrap`）"
                                        + " —— 改坏法：删掉紧跟 `MenuDraw.Text` 的那句 `SetWrapping(false)` ⇒ 回 1");
                                    CheckNear(sgTmp != null ? sgTmp.rectTransform.sizeDelta.x : -1f,
                                              LayoutSpace.Px(170f), 2e-4f,
                                              "…**TMP 容器宽 = 原版 170px**（那颗 RT 是 stretch sd(0,0) ⇒ 容器 = **解析后的父宽**；"
                                            + "⛔ 不是 `m_SizeDelta.x = 0`，也不是 dup 那三颗的 73.283/28.02/44.09）");
                                    // 🆕 **2026-10-16（A796′）换口**：宽/高改走 `LabelRenderedPx`（TMP `textBounds`）
                                    //    —— ⛔ 不再读**字段缓存** `Label.WorldW/H`。
                                    var sgLbSz = LabelRenderedPx(sgLb);
                                    float sgLbW = sgLbSz.x, sgLbH = sgLbSz.y;
                                    CheckTrue(sgLbW > 5f && sgLbH > 5f,
                                              $"（前提）那颗字**真量得到**（{sgLbW:F1}×{sgLbH:F1}px）—— 量不出时下面那条会退化");
                                    CheckTrue(sgLbW <= rSg2.W + 1.5f && sgLbH <= rSg2.H + 1.5f,
                                              $"…**渲出来 {sgLbW:F1}×{sgLbH:F1}px ≤ 原版框 170×50**"
                                            + "（容差 1.5px = TMP 二分收敛粒度；只比字号会漏掉「字号对而溢出」那一族）");
                                    // 落位：原版这颗 `m_HorizontalAlignment = 2 (Center)` = 我们的出厂档 ⇒ **不调 `Align*`**
                                    float sgLbCx = MenuCheck.DesignPx(sgLb.transform).x;
                                    CheckNear(sgLbCx, rSg2.CX, 0.5f,
                                              $"★ …**块心 = {rSg2.CX:F0}**（原版 `Center` = 出厂档）—— 改坏法："
                                            + "把上面 dup 那颗的 `MenuDraw.AlignRight` 抄过来 ⇒ 块心右移半个块宽");
                                }
                            }
                            // ---------- ③ 原样还原（数量 + 重建）----------
                            CardProgress.AddOwned(cd.Card.Id, cd.Card.Rarity, -sgDelta);
                            Check(CardProgress.Owned(cd.Card.Id, cd.Card.Rarity), sgOwned,
                                  $"…拥有数还原成 {sgOwned}（后面的断言与 `Shoot` 都按原状态走）");
                            cd.ShowCard(cd.Card);
                            var cnt4 = cd.Counter;
                            CheckTrue(FindChild(cnt4, "Duplicate Counter") != null && FindChild(cnt4, "Single Counter") == null,
                                      "★ 还原后又**切回 `Duplicate Counter`**（两支来回都走得通 —— 不是只搭了个壳）");
                        }
                        // 三块面板的动作：🔴 **2026-10-24 主对话裁定（铁律 5 就地订正）**
                        //   （判据 → `资料/任务线/特效线/任务总表.md`「锻造按钮的完整链路（请求 + 特效）」·
                        //    `普查产出_1023e/A31_特效接线.md` §2·1 / §5·2 · 反编译 `CraftingPanel__TryCraftingInternal.c:51`）
                        //   · 「创建副本」= **真发请求 + 同一拍播特效**：
                        //      请求 = `DoCraft`（扣 1 张同稀有度通配符 + `owned +1` + 落盘；**不动货币** ——
                        //             单机资源固定 2000）；特效 = 原版 `craftVFXs[]` 的 **按钮 + 前台卡** 两件。
                        //      🔴 时机照原版：「**发出合成请求之后、同一次调用里**就 `DoVFX()`」——
                        //         ⛔ **不合成却播特效 = 假反馈** ⇒ 失败（通配符 0）那一支**一件都不许播**（下面有反向条）。
                        //   · 「升级 / 降级」= **两颗并列钮**：置灰**只看等级上下限**（1 级灰降级、4 级灰升级），
                        //     点一下等级 ±1 ⇒ 卡框换档（判据 = 「卡框档 = 等级」那条链）。
                        //   ⚠️ **旧口径已作废**（2026-10-12「Craft 只出声」· 2026-10-18「点击但不起作用 /
                        //     恒可交互 / 存档不动」· 同日早些时候 W5「Upgrade 可交互度 = `CanUpgrade`」）
                        //     —— ⛔ 别再断回那一组。
                        string craftSavePath = System.IO.Path.Combine(Application.persistentDataPath, CardProgress.SaveFileName);
                        byte[] craftSave0 = System.IO.File.Exists(craftSavePath)
                                          ? System.IO.File.ReadAllBytes(craftSavePath) : null;
                        // ---- 四档**逐档**走（铁律 5·c：一个值 ≠ 全部情况）----
                        //   每一档都要断「名字选对了 + 两件都播了 + 落点/量纲对 + 没通配符不许播」——
                        //   只测 common 挡不住「rarity 映射串档」（那正是原版 `craftVFXs[]` 最易抄错的地方）。
                        //   四档 × 两件 = **原版那 8 件 `Wildcard_Use_*` 全被走到**。
                        // 期望值：原版 `MainMenu Warpforge.unity` 的 `craftVFXs[]` 两列（**字面量**，
                        //   ⛔ 不读 `WarpforgeUiEffect` 那张表 = 自证；判据 → `A31_特效接线.md` §2·1 第 4 条）。
                        //   rarity 5 那一行是原版原值（Button 复用 4 / Card 回落 1），我们四档才建面板 ⇒ 走不到。
                        string[] expBtnFx = { null,
                            "Wildcard_Use_Button 1 Common", "Wildcard_Use_Button 2 Rare",
                            "Wildcard_Use_Button 3 Epic",   "Wildcard_Use_Button 4 Legendary" };
                        string[] expCardFx = { null,
                            "Wildcard_Use_Card 1 Common", "Wildcard_Use_Card 2 Rare",
                            "Wildcard_Use_Card 3 Epic",   "Wildcard_Use_Card 4 Legendary" };
                        string[] craftRarities = { "common", "rare", "epic", "legendary" };
                        var craftCases = new List<CardDef>();
                        for (int ci = 0; ci < craftRarities.Length; ci++)
                        {
                            var poolC = CollectionWindow.CardsState.Pool;
                            for (int pi = 0; pi < poolC.Count; pi++)
                                if (poolC[pi].Rarity == craftRarities[ci]) { craftCases.Add(poolC[pi]); break; }
                        }
                        Check(craftCases.Count, craftRarities.Length,
                              $"（前提）卡池里四档**各有一张**卡可拿来试合成（实得 {craftCases.Count} 档）"
                            + " —— 少一档 = 上面那 8 件里有一对没被走到，⛔ 不许静默跳过");
                        var craftCard0 = cd.Card;             // 进块前那张（收尾要换回来，后面的断言按它走）
                        for (int ci = 0; ci < craftCases.Count && ci < craftRarities.Length; ci++)
                        {
                            var ccard = craftCases[ci];
                            string craftRarity = ccard.Rarity;
                            int rIdx = ci + 1;                // 与 `craftRarities` 同序：1=common … 4=legendary
                            cd.ShowCard(ccard);               // 换到这一档那张（⇒ 面板 / 按钮按它重建）
                            int owned0 = CardProgress.Owned(ccard.Id, craftRarity);
                            // 🔴 **2026-10-26（C1）**：通配符库存键 = **`army|rarity`**（2026-10-26 `F7` 起，判据 = 原版 `CheckForCardCrafting` 的谓词 `w.cardArmy == card.cardArmy && w.cardRarity == card.Rarity`）⇒ 夹具**必须**走两维口；单维 `Wildcards(rarity)` 是「不分阵营」的**兼容旧桶**、生产链根本不读它（读它 = 摆了个生产看不见的库存 ⇒ 一条断言也验不到）。
                            int wc0 = CardProgress.Wildcards(ccard.Faction, craftRarity);
                            var ch = cd.CraftHit; var chb = ch != null ? ch.GetComponent<WindowButton>() : null;
                            // （前提·不静默）四档**必须**都有那颗钮（原版 `CheckButtons`：有价就 `SetActive(1)`）
                            CheckTrue(chb != null,
                                      $"（前提·不静默）`{craftRarity}` 那张卡（{ccard.Name}）⇒ `CraftHit` **必须在**"
                                    + "（原版 `CraftingPanel__CheckButtons.c`：有价 ⇒ 面板 `SetActive(1)`）");
                            if (chb == null) continue;
                            // 原版 `CraftingPanel__CheckButtons.c`：`interactable = CheckForCardCrafting(...)` —— **有同稀有度通配符才可点**（⚠️ 2026-10-18 那版「恒可交互」已作废）。
                            CheckTrue(chb.Interactable == (wc0 > 0),
                                      $"`{craftRarity}`：`Craft` 那颗钮的可交互度 = **有通配符**（现有 {wc0} 张 ⇒ 期望 "
                                    + $"{(wc0 > 0 ? "可点" : "灰")}）—— 判据 = 原版 `CheckButtons` 的 "
                                    + "`set_interactable(CheckForCardCrafting())`");
                            var craftBtnNode = FindChild(cd.transform, "Craft Bg");
                            var frontView = cd.SlotView(0);
                            CheckTrue(craftBtnNode != null && frontView != null,
                                      $"`{craftRarity}`：（前提）两个落点都在：`Craft Bg` 节点 + 前台卡 `SlotViews[0]`"
                                    + "（原版 `DoVFX` 就是 Instantiate 在 `craftButton.position` / `card.position`）");
                            Vector3 btnPos = craftBtnNode != null ? craftBtnNode.position : Vector3.zero;
                            Vector3 cardPos = frontView != null ? frontView.transform.position : btnPos;
                            // 基线快照（用**差集**取新建的那两件，⛔ 不靠下标猜顺序）
                            var fxBefore = new HashSet<WarpforgeVFX.WarpforgeEffectPlayer>(
                                               WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers);
                            int fx0 = WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount;
                            chb.Click();
                            // ---- ① 请求真的发生了（原版 `CardServices.CraftCard`：扣 1 张 + owned+1）----
                            Check(CardProgress.Owned(ccard.Id, craftRarity), owned0 + 1,
                                  $"★ `{craftRarity}`：点 `Craft` ⇒ 拥有数 **{owned0} → {owned0 + 1}**"
                                + "（原版 `CraftCard` 受理后 owned+1）—— 没加/加多了都红");
                            Check(CardProgress.Wildcards(ccard.Faction, craftRarity), wc0 - 1,
                                  $"★ `{craftRarity}`：…而且**扣了 1 张**通配符（{wc0} → {wc0 - 1}）"
                                + " —— 原版 `This will consume a wildcard`；⛔ 不扣 = 白送");
                            // ---- ② 同一拍播了**两件**，名字照原版那两列 ----
                            var fxAdded = new List<WarpforgeVFX.WarpforgeEffectPlayer>();
                            foreach (var fp in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
                                if (fp != null && !fxBefore.Contains(fp)) fxAdded.Add(fp);
                            Check(fxAdded.Count, 2,
                                  $"★ `{craftRarity}`：点一下 ⇒ **两件 UI 特效**（按钮 + 前台卡；原版 `DoVFX` 两个 Instantiate）"
                                + $" —— 实得 {fxAdded.Count} 件（活着的特效 {fx0} → {WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount}）");
                            if (fxAdded.Count == 2)
                            {
                                var fxBtn = fxAdded[0]; var fxCard = fxAdded[1];   // 顺序 = `PlayCraftVfx` 先按钮、后卡
                                CheckText(fxBtn.EffectName, expBtnFx[rIdx],
                                          $"★ `{craftRarity}`：按钮那件 = **原版 `craftVFXs[{rIdx}].ParticleButton`**"
                                        + $" = 「{expBtnFx[rIdx]}」—— 映射串档/名字错都红");
                                CheckText(fxCard.EffectName, expCardFx[rIdx],
                                          $"★ `{craftRarity}`：卡那件 = **原版 `craftVFXs[{rIdx}].ParticleCard`**"
                                        + $" = 「{expCardFx[rIdx]}」");
                                // 落点：原版取**目标节点当时的世界位置**（`DoVFX` 两个 Instantiate 的第 2 实参）
                                CheckNear(Vector3.Distance(fxBtn.transform.position, btnPos), 0f, 0.02f,
                                          $"★ `{craftRarity}`：按钮那件落在 **`Craft Bg` 的世界位置**"
                                        + $"（距 {Vector3.Distance(fxBtn.transform.position, btnPos) * 108f:F1} 设计 px）");
                                CheckNear(Vector3.Distance(fxCard.transform.position, cardPos), 0f, 0.02f,
                                          $"★ `{craftRarity}`：卡那件落在 **前台那张卡的世界位置**（同上：原版 `card.position`）");
                                // 量纲：原版 `SetParent` 保**世界缩放** ⇒ 实例世界缩放 = prefab 根缩放 × **1/108**
                                //   （壳可见高 10 世界单位 / 1080 设计 px；判据 = `WarpforgeUiEffect` 文件头 ③）。
                                //   prefab 根缩放（`A31` §七·2 现读）：`Card 1` = **1**、`Card 2/3/4` = **1.2**、
                                //   四个 Button = **1**。
                                float px = CardPresentation.LayoutSpace.Px(1f);
                                float expCardScale = (rIdx == 1 ? 1f : 1.2f) * px;
                                CheckNear(fxBtn.transform.lossyScale.x, 1f * px, 2e-4f,
                                          $"★ `{craftRarity}`：按钮那件的**世界缩放 = 1 × 1/108**"
                                        + "（⛔ 不是 1 —— 漏乘 `PixelScale` 会大 108 倍）");
                                CheckNear(fxCard.transform.lossyScale.x, expCardScale, 2e-4f,
                                          $"★ `{craftRarity}`：卡那件的**世界缩放 = {(rIdx == 1 ? "1" : "1.2")} × 1/108**"
                                        + "（`Card 2/3/4` 的 prefab 根缩放就是 **1.2**，⛔ 别「统一」成 1）");
                                // 收尾：这两件 Kill 掉（batch 里 `DestroyNow` 走 `DestroyImmediate`），活着的数回基线
                                // 🔴 **2026-10-24（`H2` · `E4` 顺手发现 1 复核）**：这是 `WarpforgeEffectPlayer.Kill()`
                                //   —— **我们自己的方法**，批处理里**真生效**（`DestroyNow` → `Active.Remove` +
                                //   `DestroyImmediate`，见 `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:385/457`）。
                                //   ⛔ 它**不是** DOTween 的 `TweenExtensions.Kill` —— 那个在编辑器批处理里是
                                //   **静默 no-op**（`initialized == false` 直接 return，判据 `E4` 一·1）。
                                //   ⇒ 下面那条「回到基线」的断言**不是空验**，别按「批处理里 Kill 都空转」一刀切。
                                fxBtn.Kill(); fxCard.Kill();
                                Check(WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount, fx0,
                                      $"…`{craftRarity}`：两件 Kill 之后活着的特效数回到基线 {fx0}");
                            }
                            // ---- ③ 反向（非自证）：**没有通配符 ⇒ 请求不发、特效一件都不许播** ----
                            CardProgress.SetWildcardsForTest(CardProgress.WildKey(ccard.Faction, craftRarity), 0);
                            cd.ShowCard(ccard);              // 重建 ⇒ 钮按库存变灰（原版 `CheckButtons`）
                            var zeroBtn = cd.CraftHit != null ? cd.CraftHit.GetComponent<WindowButton>() : null;
                            CheckTrue(zeroBtn != null && !zeroBtn.Interactable,
                                      $"`{craftRarity}`：通配符 **0 张** ⇒ `Craft` 钮**不可交互**（原版 `CheckButtons`）"
                                    + " —— 改坏法：把 `BuildCrafting` 的 `Interactable` 写回恒 `true` ⇒ 本条红");
                            int ownedZero = CardProgress.Owned(ccard.Id, craftRarity);
                            int fxZero = WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount;
                            // 绕过灰钮**直调请求口** —— 方法自己必须拒绝（⛔ 不能只靠 UI 的灰挡着）
                            cd.OnCraftClicked();
                            Check(CardProgress.Owned(ccard.Id, craftRarity), ownedZero,
                                  $"★ `{craftRarity}`：通配符 0 时直调 `OnCraftClicked` ⇒ 拥有数**不变**（仍是 {ownedZero}）"
                                + " —— 拒绝要在**方法里**，不能只靠那颗灰钮");
                            Check(WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount, fxZero,
                                  $"★ `{craftRarity}`：…而且**一件特效都没播**（不是 1 件、不是 2 件）"
                                + " —— ⛔ 不合成却播特效 = 假反馈（原版失败走 `OnFail_22_1`、**不调 `DoVFX`**）");
                            // ---- ④ 还原这一档（后面的断言与 `Shoot` 都按原状态走）----
                            CardProgress.SetWildcardsForTest(CardProgress.WildKey(ccard.Faction, craftRarity), wc0);
                            CardProgress.AddOwned(ccard.Id, craftRarity,
                                                  owned0 - CardProgress.Owned(ccard.Id, craftRarity));
                            cd.ShowCard(ccard);
                            Check(CardProgress.Owned(ccard.Id, craftRarity), owned0,
                                  $"（收尾）`{craftRarity}` 拥有数还原成 {owned0}");
                            Check(CardProgress.Wildcards(ccard.Faction, craftRarity), wc0,
                                  $"（收尾）`{craftRarity}` 通配符还原成 {wc0} 张");
                        }
                        // ============================================================ ⑥ 🆕 2026-10-24（A6）
                        // 原版那**另一支**（`CraftingPanel__TryCrafting.c:26-49`，逐句）：
                        //   if (card.Rarity != 4) goto normal;                      // 4 = Legendary
                        //   if (InventoryManager.GetOwnedCount(card) != 1) goto normal;
                        //   WindowsManager.ShowPopUp(正文, localizeTexts:1, closeOnEsc:0,
                        //                            [Cancel(OnPress=null), OK(OnPress=<TryCrafting>b__21_0)])
                        //   return;                                                 // ← 不合成、不播
                        //   normal: TryCraftingInternal();                          // = 我们的 `CraftNow`
                        // 三句文案的键（`stringliteral.json` 地址表实读，反编译里那三个 `DAT_` 逐个对上地址）：
                        //   正文 `DAT_1842d24e0` @0x42D24E0 = "MenuShop/ExtraLegendaryWarning"
                        //   左钮 `DAT_1842be120` @0x42BE120 = "MainMenu/General/Cancel"
                        //   右钮 `DAT_1842be718` @0x42BE718 = "MainMenu/General/OK"
                        // 🔴 期望值一律**写死字面量**（⛔ 不读 `CardDetailPopup` 里那几个常量 = 非自证）。
                        // 灭自证：删掉那道闸 ⇒ ②「窗出现 / 卡没变 / 特效 0」三条一起红；
                        //         把闸放宽成「传奇就弹」（不看 owned==1）⇒ ⑤ 那条红。
                        {
                            Section("A6：传奇 且 `owned == 1` ⇒ 先弹确认窗（`CraftingPanel__TryCrafting.c:26-49`）");
                            CardDef lg = null;
                            var poolL = CollectionWindow.CardsState.Pool;
                            for (int pi = 0; pi < poolL.Count; pi++)
                                if (poolL[pi].Rarity == "legendary") { lg = poolL[pi]; break; }
                            CheckTrue(lg != null,
                                      "（前提）卡池里有一张 **legendary** 卡可试确认窗 —— 没有 ⇒ 下面整组无对象（不静默跳过）");
                            if (lg != null)
                            {
                                string lr = lg.Rarity;
                                int ownedL0 = CardProgress.Owned(lg.Id, lr);
                                int wcL0 = CardProgress.Wildcards(lg.Faction, lr);   // 🔴 两维桶（`army|rarity`，见上段 C1 注）
                                // 夹具：owned **正好 1**（原版条件是 `== 1`）、通配符 3 张（够扣两次）
                                CardProgress.AddOwned(lg.Id, lr, 1 - CardProgress.Owned(lg.Id, lr));
                                CardProgress.SetWildcardsForTest(CardProgress.WildKey(lg.Faction, lr), 3);
                                Check(CardProgress.Owned(lg.Id, lr), 1, "（夹具）传奇那张卡的 owned 摆成 **1**");

                                // ---- ② 点「创建副本」⇒ 只弹窗：卡不变、通配符不扣、特效 0 件 ----
                                cd.ShowCard(lg);
                                int fxBase = WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount;
                                var fxPre = new HashSet<WarpforgeVFX.WarpforgeEffectPlayer>(
                                                WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers);
                                var hitL = cd.CraftHit != null ? cd.CraftHit.GetComponent<WindowButton>() : null;
                                CheckTrue(hitL != null,
                                          "（前提）传奇那张卡 ⇒ `CraftHit` 在（原版 `CheckButtons`：有价就 `SetActive(1)`）");
                                if (hitL != null)
                                {
                                    hitL.Click();
                                    var conf = cd.CraftConfirmPopup;
                                    CheckTrue(conf != null && conf.gameObject.activeSelf,
                                              "★ 传奇 && owned==1：点「创建副本」⇒ **先弹确认窗**"
                                            + "（原版 `TryCrafting` 那一支：`ShowPopUp` 后 `return`）—— "
                                            + "改坏法：把 `OnCraftClicked` 的闸删掉 ⇒ 窗不出现、本条红");
                                    Check(CardProgress.Owned(lg.Id, lr), 1,
                                          "★ …而且**卡没变**（owned 仍 1）—— 确认之前不许合成"
                                        + "（改坏法：闸里顺手调了 `CraftNow()` ⇒ 本条红）");
                                    Check(CardProgress.Wildcards(lg.Faction, lr), 3,
                                          "★ …通配符**一张都没扣**（仍 3）—— 确认之前不许扣");
                                    Check(WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount, fxBase,
                                          "★ …而且**一件特效都没播**（原版那一支 `return` 在 `DoVFX` 之前）"
                                        + " —— 改坏法：先播后弹 / 弹窗那一刻也播 ⇒ 本条红");
                                    if (conf != null)
                                    {
                                        // 窗 = 原版 `popupWindowTwoButtons` 那一版；三句文案的键照地址表写死
                                        CheckTrue(conf.TwoButtons,
                                                  "★ 确认窗 = 原版 **2 按钮版**（`MessagePopupWindow2Buttons`：Cancel + OK）");
                                        Check(conf.PrefabName, "MessagePopupWindow2Buttons",
                                              "★ …建的就是那一版 prefab（原版 `popupWindowTwoButtons`）");
                                        Check(conf.MessageKey, "MenuShop/ExtraLegendaryWarning",
                                              "★ 正文键 = `MenuShop/ExtraLegendaryWarning`（`DAT_1842d24e0`）");
                                        Check(conf.PrimaryKey, "MainMenu/General/Cancel",
                                              "★ **左钮**键 = `MainMenu/General/Cancel`（`DAT_1842be120` = 原版 `primaryButton`）");
                                        Check(conf.SecondaryKey, "MainMenu/General/OK",
                                              "★ **右钮**键 = `MainMenu/General/OK`（`DAT_1842be718` = 原版 `secondButton`）");
                                        // 画出来的字 = **官方本地化整句**（中/英两列都照抄进这里；
                                        // ⛔ 不拿 `Loc.T` 反查 —— 那与实现的取值口同源 = 自证）
                                        bool zhC = Loc.Current == AvailableLanguages.Chinese;
                                        CheckText(conf.MessageShown,
                                                  zhC
                                                  ? "请记住，您的卡组中只能使用每张传奇牌的一份副本。您仍然可以创建此张牌的更多副本，但它们只能用于升级该张牌。您想要继续吗？"
                                                  : "You can only use one copy of each legendary card in your deck. You can buy or craft more copies of this card, but they are only usable to level up the card. Do you wish to continue?",
                                                  "★ 正文画出来的是**官方本地化**那整句（不是键名）—— 中/英两列都写死在这里，"
                                                + "改词条 / 改 `Term` 链都红");
                                        CheckText(conf.PrimaryShown, zhC ? "取消" : "Cancel",
                                                  "★ 左钮画出来的 = **取消**（官方 `MainMenu/General/Cancel`）");
                                        CheckText(conf.SecondaryShown, zhC ? "好的" : "OK",
                                                  "★ 右钮画出来的 = **好的**（官方 `MainMenu/General/OK`）");

                                        // ---- ③ 确认（右钮 `ButtonRight` = OK）⇒ owned+1、扣 1 张、播 2 件 ----
                                        ClickPopUpButton(conf, "ButtonRight");
                                        Check(CardProgress.Owned(lg.Id, lr), 2,
                                              "★ 点 **OK** ⇒ owned **1 → 2**（原版 `TryCraftingInternal` 受理后 owned+1）");
                                        Check(CardProgress.Wildcards(lg.Faction, lr), 2,
                                              "★ …而且**扣了 1 张**通配符（3 → 2）");
                                        var add = new List<WarpforgeVFX.WarpforgeEffectPlayer>();
                                        foreach (var fp in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
                                            if (fp != null && !fxPre.Contains(fp)) add.Add(fp);
                                        Check(add.Count, 2,
                                              "★ …而且**同一拍播了两件** UI 特效（原版 `DoVFX` 两个 Instantiate）"
                                            + $" —— 实得 {add.Count} 件");
                                        if (add.Count == 2)
                                        {
                                            CheckText(add[0].EffectName, "Wildcard_Use_Button 4 Legendary",
                                                      "★ 按钮那件 = 原版 `craftVFXs[4].ParticleButton`"
                                                    + "（rarity 4 = Legendary，⛔ 不读我们那张表 = 非自证）");
                                            CheckText(add[1].EffectName, "Wildcard_Use_Card 4 Legendary",
                                                      "★ 卡那件 = 原版 `craftVFXs[4].ParticleCard`");
                                        }
                                        CheckTrue(conf == null || !conf.gameObject.activeSelf,
                                                  "★ …而且确认窗**自己关掉了**（原版 `ConfigurePopUp` 给每颗钮都挂了 "
                                                + "`PopUpWindowButtonPressed`）");
                                        // 收尾：别留活的特效给后面。⚠️ `WarpforgeEffectPlayer.Kill()` 是**自有实现**
                                        // （批处理里真生效：`Active.Remove` + `DestroyImmediate`），⛔ 不是 DOTween 的
                                        // `TweenExtensions.Kill`（那个在批处理里是静默 no-op —— 判据 `E4` 一·1）。
                                        // ⇒ 紧跟的「回到基线」断言不是空验。
                                        foreach (var fp in add) if (fp != null) fp.Kill();
                                        Check(WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount, fxBase,
                                              "…两件 Kill 之后活着的特效数回到基线");
                                    }

                                    // ---- ④ 取消（左钮 `ButtonLeft` = Cancel）⇒ 一切不变、窗消失 ----
                                    CardProgress.AddOwned(lg.Id, lr, 1 - CardProgress.Owned(lg.Id, lr));  // 摆回 1
                                    cd.ShowCard(lg);
                                    var hitL2 = cd.CraftHit != null ? cd.CraftHit.GetComponent<WindowButton>() : null;
                                    CheckTrue(hitL2 != null, "（前提·取消那支）`CraftHit` 还在");
                                    if (hitL2 != null)
                                    {
                                        int fxB2 = WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount;
                                        int wcB2 = CardProgress.Wildcards(lg.Faction, lr);
                                        hitL2.Click();
                                        var conf2 = cd.CraftConfirmPopup;
                                        CheckTrue(conf2 != null && conf2.gameObject.activeSelf,
                                                  "（前提·取消）owned 已摆回 1 ⇒ 确认窗**又弹出来了**");
                                        if (conf2 != null)
                                        {
                                            ClickPopUpButton(conf2, "ButtonLeft");
                                            Check(CardProgress.Owned(lg.Id, lr), 1,
                                                  "★ 点 **Cancel** ⇒ owned **不变**（仍 1）—— ⛔ 取消不许合成");
                                            Check(CardProgress.Wildcards(lg.Faction, lr), wcB2,
                                                  "★ …通配符**一张都没扣**");
                                            Check(WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount, fxB2,
                                                  "★ …而且**一件特效都没播**（⛔ 不合成却播 = 假反馈）");
                                            CheckTrue(conf2 == null || !conf2.gameObject.activeSelf,
                                                      "★ …确认窗**关掉了**（Cancel 那颗也得关 —— 原版靠 "
                                                    + "`PopUpWindowButtonPressed`，我们写进回调）");
                                            CheckTrue(cd.CraftConfirmPopup == null,
                                                      "★ …而且实现把它记成「已关」（`CraftConfirmPopup == null`）");
                                        }
                                    }

                                    // ---- A6-⑤ 反方向（灭自证）：传奇但 owned **≠ 1** ⇒ 不弹窗、直接合成 ----
                                    //   原版条件是 `GetOwnedCount != 1 ⇒ goto normal` ⇒ owned==2 必须**直接**走
                                    //   `TryCraftingInternal`。改坏法：闸写成 `owned <= 1` / 「传奇就弹」⇒ 本条红。
                                    CardProgress.AddOwned(lg.Id, lr, 2 - CardProgress.Owned(lg.Id, lr));  // owned = 2
                                    cd.ShowCard(lg);
                                    var hitL3 = cd.CraftHit != null ? cd.CraftHit.GetComponent<WindowButton>() : null;
                                    CheckTrue(hitL3 != null, "（前提·owned≠1 那支）`CraftHit` 还在");
                                    if (hitL3 != null)
                                    {
                                        int fxB3 = WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount;
                                        var fxPre3 = new HashSet<WarpforgeVFX.WarpforgeEffectPlayer>(
                                                         WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers);
                                        hitL3.Click();
                                        CheckTrue(cd.CraftConfirmPopup == null,
                                                  "★ 传奇但 owned **== 2** ⇒ **不弹确认窗**"
                                                + "（原版 `GetOwnedCount(card) != 1 ⇒ goto normal`）"
                                                + " —— 改坏法：闸写成 `owned <= 1` / 传奇就弹 ⇒ 本条红");
                                        Check(CardProgress.Owned(lg.Id, lr), 3,
                                              "★ …而且**直接合成**（owned 2 → 3），闸不该拦这一支");
                                        Check(WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount, fxB3 + 2,
                                              "★ …而且直接播了 **2 件**特效（⛔ 拦住这一支 = 玩家点不动钮）");
                                        // 收尾：**先收集再 Kill** —— `ActivePlayers` 是**活表**（`Kill()` 里 `Active.Remove`）
                                        // ⇒ 边遍历边 Kill 会抛 `InvalidOperationException`。
                                        var kill3 = new List<WarpforgeVFX.WarpforgeEffectPlayer>();
                                        foreach (var fp in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
                                            if (fp != null && !fxPre3.Contains(fp)) kill3.Add(fp);
                                        // 收尾（**纯清理**，这一段后面没有断言依赖它死没死）：
                                        // `WarpforgeEffectPlayer.Kill()` 是自有实现、批处理里真生效；
                                        // ⛔ 不是 DOTween 的 `TweenExtensions.Kill`（那个在批处理里是静默 no-op）。
                                        foreach (var fp in kill3) fp.Kill();
                                    }
                                }
                                // ---- 收尾：库存 / 窗 全部还原（后面的升级断言与 `Shoot` 都按进块前的状态走）----
                                if (cd.CraftConfirmPopup != null) cd.HideCraftConfirm();
                                CardProgress.SetWildcardsForTest(CardProgress.WildKey(lg.Faction, lr), wcL0);
                                CardProgress.AddOwned(lg.Id, lr, ownedL0 - CardProgress.Owned(lg.Id, lr));
                                cd.ShowCard(lg);
                                Check(CardProgress.Owned(lg.Id, lr), ownedL0,
                                      $"（收尾）legendary 那张的拥有数还原成 {ownedL0}");
                                Check(CardProgress.Wildcards(lg.Faction, lr), wcL0,
                                      $"（收尾）legendary 通配符还原成 {wcL0} 张");
                                CheckTrue(cd.CraftConfirmPopup == null, "（收尾）没有留下开着的确认窗");
                            }
                        }
                        if (craftCard0 != null) cd.ShowCard(craftCard0);   // 换回进块前那张
                        // ---- ⑤ 自检绝不许污染玩家真存档（`_persist = false`）----
                        byte[] craftSave1 = System.IO.File.Exists(craftSavePath)
                                          ? System.IO.File.ReadAllBytes(craftSavePath) : null;
                        bool saveSame = craftSave0 == null ? craftSave1 == null
                                                          : (craftSave1 != null && craftSave0.Length == craftSave1.Length);
                        if (saveSame && craftSave0 != null)
                            for (int bi = 0; bi < craftSave0.Length; bi++)
                                if (craftSave0[bi] != craftSave1[bi]) { saveSame = false; break; }
                        CheckTrue(saveSame,
                                  $"★ 上面这一整轮（四档各合成一次 + 传奇确认窗那三下：合成 / 取消 / owned≠1 直合成）"
                                + $"**玩家存档一个字节都没动**（`{CardProgress.SaveFileName}`："
                                + (craftSave0 == null ? "点前不存在" : $"点前 {craftSave0.Length} 字节")
                                + " → "
                                + (craftSave1 == null ? "点后不存在" : $"点后 {craftSave1.Length} 字节")
                                + "）—— 自检里 `_persist = false`（`ResetForTest`）⇒ `Save()` 是空操作；"
                                + "⛔ 别让自检改写开发机上的玩家存档");
                        int lv0 = CardProgress.Level(cd.Card.Id);
                        bool canUp = CardProgress.CanUpgrade(cd.Card.Id, cd.Card.Rarity);
                        // ============================================================ 🆕 2026-10-18：「降级」钮
                        // 用户拍板改法（判据 → `资料/普查产出_1018/W8_副本升级面板_按用户口径.md` §二）：
                        //   一颗钮改**两颗并列**（『降级』`DowngradeHit` / 『升级』`UpgradeHit`）；
                        //   **等级 = 1 时「降级」不可交互、> 1 时可交互**，点一下等级 −1 ⇒ 卡框降一档
                        //   （`DoDowngrade`，与「升级」完全对称）。
                        // ⚠️ 这里**用 `SetLevel` 摆出两档再逐档断**，不依赖夹具进来时的等级
                        //   （铁律 5·c：一种情况 ≠ 全部；且两档都要真断到）。
                        // ⚠️ `RebuildKeepingState()` / `ShowCard()` 会**整棵重建** ⇒ 每次重建后都要重新取
                        //   `DowngradeHit` / `SlotView(0)`，⛔ 不能缓存重建前的 Transform。
                        {
                            int lvSave = CardProgress.Level(cd.Card.Id);
                            CheckTrue(cd.DowngradeHit != null,
                                      "★「降级」那颗钮**存在**（`CardDetailPopup.DowngradeHit`）"
                                    + " —— 用户 2026-10-18：一颗钮改两颗并列");
                            // ① 等级 1 ⇒ 钮灰（用户口径：最低级**不提示**，只把钮灰掉）
                            CardProgress.SetLevel(cd.Card.Id, 1);
                            cd.ShowCard(cd.Card);
                            var d1 = cd.DowngradeHit != null ? cd.DowngradeHit.GetComponent<WindowButton>() : null;
                            CheckTrue(d1 != null && !d1.Interactable,
                                      "★ 等级 **1** ⇒「降级」钮**不可交互**"
                                    + " —— 判据 = `BuildUpgrade` 的 `downWb.Interactable = !minned`（用户 2026-10-18 口径）");
                            // ② 等级 2 ⇒ 钮可点；点一下 ⇒ 回到 1，且卡框回到 `_tier1`
                            CardProgress.SetLevel(cd.Card.Id, 2);
                            cd.ShowCard(cd.Card);
                            var d2 = cd.DowngradeHit != null ? cd.DowngradeHit.GetComponent<WindowButton>() : null;
                            CheckTrue(d2 != null && d2.Interactable,
                                      "★ 等级 **2** ⇒「降级」钮**可交互**（等级 > 1 就不再灰）");
                            if (d2 != null)
                            {
                                var dvBefore = cd.SlotView(0);
                                string dtierBefore = dvBefore != null && dvBefore.FrameTexture != null
                                                   ? dvBefore.FrameTexture.name : null;
                                d2.Click();
                                Check(CardProgress.Level(cd.Card.Id), 1,
                                      "★ 点 `Downgrade` ⇒ 等级 **2 → 1**（`DoDowngrade` 的 `SetLevel(before − 1)`）");
                                var dvAfter = cd.SlotView(0);
                                string dtierAfter = dvAfter != null && dvAfter.FrameTexture != null
                                                  ? dvAfter.FrameTexture.name : null;
                                CheckTrue(dtierAfter != null && dtierAfter.Contains("_tier1"),
                                          $"★ …而且**卡框降到第 1 档**（`{dtierBefore}` → `{dtierAfter}`）"
                                        + " —— 判据与「升级」同一条链（`CardView.Build` 里读 `CardProgress.Level(d.artId)`；"
                                        + "⛔ 别在 `DoDowngrade` 里另算一份卡框档）");
                            }
                            // ③ 收尾：等级还原成进这一块之前的值（后面的断言与 `Shoot` 都按原状态走）
                            CardProgress.SetLevel(cd.Card.Id, lvSave);
                            cd.ShowCard(cd.Card);
                            Check(CardProgress.Level(cd.Card.Id), lvSave,
                                  $"（收尾）等级还原成 {lvSave}（后面的断言与 `Shoot` 都按原状态走）");
                        }
                        // ⚠️ 上面那一块重建过整棵树 ⇒ `UpgradeHit` 必须**现在**取（下面的 `uhb` 是新树上的）
                        var uh = cd.UpgradeHit; var uhb = uh != null ? uh.GetComponent<WindowButton>() : null;
                        if (uhb != null)
                        {
                            // 🔴 2026-10-18：可交互度**只看等级上限**（`lv == MaxLevel` ⇒ 灰），
                            //   判据 = `BuildUpgrade` 的 `upWb.Interactable = !maxed`（用户点名的就是等级那一档）。
                            //   ⚠️ 「多余副本够不够」仍由 `DoUpgrade()` 自己把着（不够就出声、不升级）——
                            //   只是**不再拿它决定钮灰不灰**。⛔ 别改回 `uhb.Interactable == canUp`。
                            bool belowMax = lv0 < CardProgress.MaxLevel;
                            CheckTrue(uhb.Interactable == belowMax,
                                      $"`Upgrade` 那颗钮的可交互度 = 「等级 < {CardProgress.MaxLevel}」（{belowMax}；等级 {lv0}）"
                                    + " —— 用户 2026-10-18：两颗钮并列，置灰只看等级上下限（⛔ 不是 `CanUpgrade`）");
                            var viewBefore = cd.SlotView(0);
                            string tierBefore = viewBefore != null && viewBefore.FrameTexture != null
                                              ? viewBefore.FrameTexture.name : null;
                            uhb.Click();
                            if (canUp)
                            {
                                Check(CardProgress.Level(cd.Card.Id), lv0 + 1,
                                      $"★ 点 `Upgrade` ⇒ 等级 **{lv0} → {lv0 + 1}**（`DoUpgrade` 的 `SetLevel(before + 1)`）");
                                var viewAfter = cd.SlotView(0);
                                string tierAfter = viewAfter != null && viewAfter.FrameTexture != null
                                                 ? viewAfter.FrameTexture.name : null;
                                CheckTrue(tierAfter != null && tierAfter.Contains("_tier" + (lv0 + 1)),
                                          $"★ …而且**卡框换到第 {lv0 + 1} 档**（`{tierBefore}` → `{tierAfter}`）"
                                        + " —— 判据 = 原版 `UpgradePanel.ChangeFrameOnLevelUp` → "
                                        + "`CardFramesSO.GetClanFrame(army, tier, cardType)`（等级就是卡框档）");
                            }
                            else if (belowMax)
                            {
                                // 到了这里 = 等级没满但**多余副本不够**（`DoUpgrade` 的第二道闸）——
                                // 钮是可点的（置灰只看等级），点下去方法自己拒绝 ⇒ 等级不变。
                                Check(CardProgress.Level(cd.Card.Id), lv0,
                                      $"点了 `Upgrade` ⇒ 等级**不变**（仍是 {lv0}）—— 等级没满（钮可点），"
                                    + $"但 `DoUpgrade` 的 `CanUpgrade` 闸拦下了（多余副本 "
                                    + $"{CardProgress.Spares(cd.Card.Id, cd.Card.Rarity)} < 需要 "
                                    + $"{CardProgress.NeedCopies[lv0 + 1]}）");
                            }
                            else
                            {
                                Check(CardProgress.Level(cd.Card.Id), lv0,
                                      $"点了 `Upgrade` ⇒ 等级**不变**（仍是 {lv0}）—— 已满级 ⇒ 钮灰着、`Click()` 被挡");
                            }
                        }
                        // ============================================================ 🆕 2026-10-18：`Content` / 满级提示
                        // 用户拍板改法（判据 → `资料/普查产出_1018/W8_副本升级面板_按用户口径.md` §三·2）：
                        //   ① `Content` **常显**（满级也常显 —— 否则满级后「降级」钮跟着消失，
                        //      用户要的「降级」在最高档直接失效）；
                        //   ② `No Upgrade Warning` **仍只在满级露**（用户口径：「保留满级提示那一支」）。
                        //   ⚠️ 原版那两句 `content.SetActive(!maxed)` / `w.SetActive(maxed)` 的互斥口径
                        //      **只在「只有一颗升级钮」时成立** ⇒ 这里按用户改法断，别照原版改回去。
                        {
                            int lvBak = CardProgress.Level(cd.Card.Id);
                            // 先摆到**未满级**那一档再断（`lvBak` 可能正好是满级 —— 铁律 5·c：别只测一种情况）
                            CardProgress.SetLevel(cd.Card.Id, 1);
                            cd.ShowCard(cd.Card);
                            // ⚠️ `ShowCard` 整棵重建 ⇒ `Find` 必须在重建**之后**（旧 Transform 已销毁）
                            var upPanel = cd.transform.Find("Card Options Panel/Panel/Upgrade Panel");
                            CheckTrue(upPanel != null, "（前提）找得到 `Card Options Panel/Panel/Upgrade Panel`");
                            var contentNow = upPanel != null ? upPanel.Find("Content") : null;
                            CheckTrue(contentNow != null && contentNow.gameObject.activeSelf,
                                      "★ 等级 1（未满级）⇒ `Upgrade Panel/Content` **常显**");
                            var warnNow = cd.NoUpgradeWarn;
                            CheckTrue(warnNow != null && !warnNow.gameObject.activeSelf,
                                      "…此时 `No Upgrade Warning` **不露**（只在满级露）");
                            // 满级：`Content` 仍显 + 警告露出 + 「升级」钮灰
                            CardProgress.SetLevel(cd.Card.Id, CardProgress.MaxLevel);
                            cd.ShowCard(cd.Card);
                            var contentMax = cd.transform.Find("Card Options Panel/Panel/Upgrade Panel/Content");
                            CheckTrue(contentMax != null && contentMax.gameObject.activeSelf,
                                      $"★ **满级（{CardProgress.MaxLevel}）**时 `Content` 仍**常显**"
                                    + " —— ⛔ 别照原版「满级 ⇒ 整组关」改回去（那样「降级」在最高档失效）");
                            var warnMax = cd.NoUpgradeWarn;
                            CheckTrue(warnMax != null && warnMax.gameObject.activeSelf,
                                      "★ **满级** ⇒ `No Upgrade Warning` **露出来**（用户口径：保留满级提示那一支）");
                            var upMax = cd.UpgradeHit != null ? cd.UpgradeHit.GetComponent<WindowButton>() : null;
                            CheckTrue(upMax != null && !upMax.Interactable,
                                      $"★ 满级（{CardProgress.MaxLevel}）⇒「升级」钮**不可交互**（可交互度 = 等级上下限）");
                            CardProgress.SetLevel(cd.Card.Id, lvBak);
                            cd.ShowCard(cd.Card);
                            Check(CardProgress.Level(cd.Card.Id), lvBak,
                                  $"（收尾）等级还原成 {lvBak}（后面的断言按原状态走）");
                        }
                        // `Show Card Text` 切效果文字条；语音钮「没有就出声」
                        var ehit = cd.EyeHit; var ehb = ehit != null ? ehit.GetComponent<WindowButton>() : null;
                        if (ehb != null && cd.LoreVisible)
                        {
                            ehb.Click();
                            CheckTrue(!cd.LoreVisible, "点 `Show Card Text` ⇒ 效果文字条**藏起来**");
                            ehb.Click();
                            CheckTrue(cd.LoreVisible, "再点 ⇒ 显示回来");
                        }
                        // ================================================================
                        //  🆕 2026-09-27：「相关卡」那一块（**1 主卡 + 8 相关卡 = 9 格** · 扇形 · 换位）
                        //  判据 → 正本 **§8·7**（扇形真值 = 原版那条 legacy clip `Card Display Open`）
                        //  与 **§9·6**（换位：三个闸 · 0.25s 六条 tween · `SetSiblingIndex` 两两互换 ·
                        //  收尾重设 lore/语音）。用户口径 → `项目任务.md` §三 第 19 条。
                        //  ⚠️ **槽 0–4 是原版真值，槽 5–8 是我们外推的**（原版只有 5 格）—— 下面分开钉。
                        // ================================================================
                        {
                            // 量「卡框」那一层的渲染包围盒 —— ⚠️ **不能量整棵子**：整个 `2DCard` 的
                            // 包围盒必然被「软光/影」那张 **4.4281²（= 1106.9px）**方图撑大
                            // （2026-10-10 订正：那不是「立绘越出」带来的，见上面那条注释）
                            Bounds? FB(CardView v)
                            {
                                if (v == null) return null;
                                foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
                                {
                                    var tx = mr.sharedMaterial != null ? mr.sharedMaterial.mainTexture : null;
                                    if (tx != null && tx.name.StartsWith("frame_")) return mr.bounds;
                                }
                                return null;
                            }

                            CardDef poc = null, sgDef = null, moa = null;
                            foreach (var c in CardDatabase.Load())
                            {
                                if (c.Name == "Path of Command") poc = c;
                                if (c.Name == "Storm Guardian") sgDef = c;
                                if (c.Name == "Master of Arcana") moa = c;
                            }
                            CheckTrue(poc != null && sgDef != null,
                                      "（前提）卡池里有 `Path of Command` 与 `Storm Guardian`（用户举的那个例子）");
                            if (poc != null && sgDef != null)
                            {
                                cd.ShowCard(poc);      // 复用同一扇窗换一张卡（原版 `ShowCard` 就是这个意思）
                                CheckTrue(cd.SlotCount >= 2,
                                          $"`Path of Command` ⇒ 卡片那一叠 **{cd.SlotCount} 格**（主卡 + 相关卡）");
                                int sg = cd.SlotIndexOf(sgDef.Id);
                                CheckTrue(sg > 0,
                                          $"★ **`Storm Guardian` 就在这一叠里（第 {sg} 格）** —— "
                                        + "用户 2026-09-26 举的例子：卡面写 `Deploy a Storm Guardian`，"
                                        + "总不能让玩家不知道那张是什么");

                                // ① 前台那张 = 位姿槽 0：**屏心 (960,480) · 转角 0**（原版 clip 末帧）
                                var s0 = cd.SlotView(0);
                                var b0 = FB(s0);
                                CheckTrue(b0.HasValue, "位姿槽 0（前台）画出来了");
                                // 🔴 **位置断言要用「节点位置」，不能用渲染包围盒的中心**：
                                //    卡框那层（561.25×814.25）与卡本体（523.25×832.75）**本来就不同心**（差 ≈7.5px），
                                //    而且**旋转过的卡 AABB 还会再涨** —— 第一版就是这么误报的（实测 487.5 / 494.0）。
                                //    尺寸才用渲染盒（下面平台那条 `frame_` 高度就是渲染真值）。
                                if (b0.HasValue)
                                {
                                    var c0 = MenuCheck.DesignPx(s0.transform);
                                    CheckNear(c0.x, 960f, 2f, "前台那张**中心 x = 960**（原版 clip 末帧 `anchoredPosition.x = 0`）");
                                    CheckNear(c0.y, 480f, 2f, "…**中心 y = 480**（原版 `anchoredPosition.y = 60` ⇒ 屏幕 y = 540−60）");
                                }

                                // ② 槽 1（第一张相关卡）：**原版真值 (−121,53) · 2.510° · scale 232.954**
                                var s1 = cd.SlotView(1);
                                if (s1 != null && s0 != null)
                                {
                                    var c1 = MenuCheck.DesignPx(s1.transform);
                                    CheckNear(c1.x, 960f - 121f, 2.5f,
                                              "槽 1 中心 x = **839**（原版 clip：`anchoredPosition.x = −121` ⇒ 扇形**朝左开**）");
                                    CheckNear(c1.y, 480f + 7f, 2.5f,
                                              "槽 1 中心 y = **487**（原版 `y = 53` ⇒ 屏幕 y = 540−53，比前台**低 7**）");
                                    CheckNear(s1.transform.eulerAngles.z, 2.510f, 0.05f,
                                              "槽 1 转角 = **2.510°**（原版 clip 末帧的 z 旋转）");
                                    // 🔴 缩放比**别用渲染包围盒比** —— 旋转过的 AABB 会被撑大（实测给 0.96，真值 0.932）
                                    CheckNear(s1.transform.localScale.x / s0.transform.localScale.x, 232.954f / 250f, 0.005f,
                                              "槽 1 缩放 ÷ 前台缩放 = **232.954 / 250**（原版 clip 的 `localScale`）");
                                }

                                // ③ **外推的那几格**（槽 5–8）：只钉走向（更靠左 / 更斜 / 更小）——
                                //    原版没有第 6 格可比，⚠️ 这一段是**我们挑的**（正本 §8·7）。
                                //    ⚠️ 真实卡池里相关卡通常只有 1–2 张（全池 128 处点名摊在 100 来张卡上）
                                //    🔴 **2026-10-18 之后·第六会话就地订正（铁律 5）**：本行原写「**129** 处」
                                //    —— 那是 2026-10-18 之前的口径（`S13` 把 `UM84 Chaplain Cassius` 的
                                //    `[Talent]:` 改成裸写 `Talent:` 之后，规则②第一次真生效 ⇒ 少 1 处）。
                                //    真值 = **128**，判据 = `RuleEngine/Editor/RuleEngineTest.cs` 的
                                //    `Check(mentions, 128, …)`（那里 2026-10-18 已就地订正过，本行没跟着改）。
                                //    ⚠️ 中文档另有一份**不同**的数（**132** 处 / 同样 67 张）—— 见
                                //    `资料/普查产出_第六会话/WRelated_相关卡中文索引.md`。
                                //    ⇒ **换一张相关卡够多的**再走这段：`Master of Arcana` 的天赋是
                                //    `Choose an Ultramarines Psychic Power…`（**池子 4 张**）⇒ 至少 5 格。
                                {
                                    CheckTrue(moa != null, "（前提）卡池里有 `Master of Arcana`");
                                    if (moa != null)
                                    {
                                        // 🔴 **2026-10-12（A437）换卡靠的是 `ShowCard` 里那句显式 `Build()`** ——
                                        //   A217② 把 `GameWindow.TryOpen` 按原版改成按 `CurrentState` 分三档之后，
                                        //   「同窗再开 ⇒ 重建内容」**没有了**（`Open` 那一支一个字段都不写就 return）
                                        //   ⇒ 这一句是**同窗换卡**唯一的重建入口。判据 = 原版 `CardDisplayWindow.ShowCard`
                                        //   复用同一个窗（`WindowsManager.OpenWindow(this)`）+ 窗自己那份刷新
                                        //   ⇒ 我们的等价物 = **窗实例不变 + 内容按新卡重建**（下面两条分开断）。
                                        var wBefore = cd.gameObject;      // 换卡前那一扇（下面断它**不换**）
                                        cd.ShowCard(moa);
                                        CheckTrue(cd.gameObject == wBefore,
                                                  "★ A437：`ShowCard(A)` 紧跟 `ShowCard(B)` ⇒ **窗对象不变**（复用同一扇；"
                                                + "原版 `WindowsManager.OpenWindow(this)` 就是这个意思）"
                                                + " —— 改坏法：把 `ShowCard` 改成「销毁旧窗 + 建一扇新的」⇒ 这条红");
                                        CheckTrue(cd.FrontDef != null && cd.FrontDef.Id == moa.Id,
                                                  "★ A437 …而且前台那张**真的换成 `Master of Arcana` 了**（现在的 `FrontDef` = "
                                                + (cd.FrontDef != null ? "`" + cd.FrontDef.Name + "`" : "(null)") + "）"
                                                + " —— 🔴 **这条最锋利**：它专打「以为复用会自动重建」的那个人。"
                                                + "改坏法：删掉 `Shell/CardDetailPopup.cs` 的 `ShowCard` 里那句显式 `Build()`"
                                                + "（「关过再开」那一支仍会因 `Closed` 支重建、**红不出来**）"
                                                + "⇒ **同窗换卡这一支**的 `FrontDef` 还是上一张 ⇒ 红");
                                        // ⚠️ 下面那条 `inPool`（相关卡有没有列出来）**单独不能当换卡的证据**：
                                        //   `Path of Command` 自己也是 Ultramarines 的卡，池子可能与它重叠
                                        //   ⇒ 换卡这件事由上面那条 `FrontDef` 断，`inPool` 管的是另一件事（相关卡来源②）。
                                        CheckTrue(cd.SlotCount >= 5,
                                                  $"`Master of Arcana`（天赋是个 **4 张的池子**）⇒ 卡片那一叠 {cd.SlotCount} 格");
                                        int inPool = 0;
                                        foreach (var c in CardDatabase.Load())
                                            if (c.Faction == "Ultramarines" && c.Subtype == "Psychic Power"
                                                && cd.SlotIndexOf(c.Id) > 0) inPool++;
                                        CheckTrue(inPool >= 3,
                                                  $"★ **池子里的卡真列进相关卡了**（{inPool} 张）—— "
                                                + "相关卡来源②：天赋是个池子 ⇒ 池里那几张跟出来（判据 → 正本 §九）");
                                        int pairs = 0;
                                        for (int i = 2; i < cd.SlotCount; i++)
                                        {
                                            var sa = cd.SlotView(i - 1); var sb = cd.SlotView(i);
                                            if (sa == null || sb == null) continue;
                                            pairs++;
                                            CheckTrue(MenuCheck.DesignPx(sb.transform).x
                                                       < MenuCheck.DesignPx(sa.transform).x - 10f,
                                                      $"槽 {i} 比槽 {i - 1} **更靠左**（扇形继续张开）");
                                            CheckTrue(sb.transform.localScale.x < sa.transform.localScale.x,
                                                      $"槽 {i} 比槽 {i - 1} **更小**（原版就是越远越小）");
                                            CheckTrue(sb.transform.eulerAngles.z > sa.transform.eulerAngles.z,
                                                      $"槽 {i} 比槽 {i - 1} **更斜**（原版就是越远越斜）");
                                        }
                                        CheckTrue(pairs >= 3, $"比得出至少 3 对相邻卡位（实得 {pairs} 对）");
                                        // 🔴 分层：**每格一个独立队列、越靠前台号越大** ——
                                        //    不然「谁盖谁」只剩「到相机的距离」在排，而那是**不可控**的
                                        //    （实测第一版：后面那张的**卡名画到了前面那张的立绘之上**）。
                                        {
                                            int prevQ = int.MaxValue; bool mono = true;
                                            for (int i = 0; i < cd.SlotCount; i++)
                                            {
                                                var sv = cd.SlotView(i);
                                                if (sv == null) continue;
                                                int q = -1;
                                                foreach (var mr in sv.GetComponentsInChildren<MeshRenderer>(true))
                                                    if (mr.sharedMaterial != null) { q = mr.sharedMaterial.renderQueue; break; }
                                                if (q >= prevQ) mono = false;
                                                prevQ = q;
                                            }
                                            CheckTrue(mono,
                                                      "★ **每格的渲染队列逐格递减**（前台最高）—— 分层靠队列，不靠距离"
                                                    + "（`资料/已知的坑.md`：同队列的两层谁盖谁不可控）");
                                        }
                                        cd.ShowCard(poc);        // 换回来 —— 下面第 ④ 段要在 `Path of Command` 这叠上验换位
                                    }
                                }

                                // ④ 换位：点相关卡 ⇒ 它和前台**两两互换**（原版 `ChangeCardPosition`）
                                if (sg > 0)
                                {
                                    var before0 = cd.FrontDef;
                                    var hit1 = FindChild(cd.transform, "CardHit " + sg);
                                    var hb1 = hit1 != null ? hit1.GetComponent<WindowButton>() : null;
                                    CheckTrue(hb1 != null, $"第 {sg} 格有点击区（原版 `AddCardsListeners`：5 个卡位各挂一个）");
                                    var posFront = cd.SlotView(0).transform.position;
                                    var posOther = cd.SlotView(sg).transform.position;
                                    // 🔴 批处理没有帧循环 ⇒ 补间要**手动推进**（同 `BattleScene` 那几处）
                                    CardTween.Mode = DG.Tweening.UpdateType.Manual;
                                    if (hb1 != null) hb1.Click();
                                    CheckTrue(cd.IsSwapping, "点了相关卡 ⇒ 换位在播（原版 `swappingCards` 闸置上）");
                                    // 播完之后再点一次 ⇒ **该被闸①挡掉**（原版：上一次没播完什么都不做）
                                    if (hb1 != null) hb1.Click();
                                    CheckTrue(cd.FrontDef == before0,
                                              "★ 换位播到一半再点 ⇒ **什么都不做**（原版闸① `swappingCards`）");
                                    // 🆕 **2026-10-24（`A22` §4.1 · 非自证）**：换位半程的**进度判别式**。
                                    //   判据 = DOTween 的**两套默认值**（IL 实读 → `A22_Sequence缓动审计.md` §1.1）：
                                    //     · `Sequence::Setup` 把序列自己的 `easeType` 硬编码成 **`Linear`**（不弯）；
                                    //     · 子补间 `Tweener::Setup` 取 `defaultEaseType`（本工程 = **6 OutQuad**）。
                                    //   原版 `CardDisplayWindow__ChangeCardPosition.c` **全文没有 `SetEase`** ⇒
                                    //   半程（t = 0.5）位移 = **OutQuad(0.5) = 0.75**。
                                    //   ⚠️ **2026-10-24（`F1` · `R1` P2-3 就地订正）**：本条的「改坏法」原写
                                    //   「给这个 Sequence 挂 `OutQuad` ⇒ 0.9375」—— **已不可达**：`A11` 已把本窗换位
                                    //   改成**单条** `CardView.TweenPose(…, OutQuad)`（`CardDetailPopup.SwapToFront`），
                                    //   没有 Sequence 可挂了。现在**两条**都能真退回、都能让本条红：
                                    //     ① **可达**：把 `SwapToFront` 那两条 `TweenPose` 的 ease 改成 `Linear`
                                    //       （`CardDetailPopup.cs` 里的 `Ease.OutQuad` → `Ease.Linear`）⇒ 半程 = **0.5**；
                                    //     ② **需先改回旧形态**：把位姿补间换回 Sequence（`DOMove + DOLocalRotate +
                                    //       DOScale` 三条 `Join`、各自默认 OutQuad）**并**给该 Sequence 挂 `OutQuad`
                                    //       ⇒ 0.75 再弯一次 = **0.9375**（二次弯曲）。
                                    //   两条路径的期望读数与 0.75 相差 ≥ 0.25 / 0.1875 ⇒ 远超本条的 ±0.02。
                                    //   ⚠️ 期望值来自 DOTween 的缓动定义，⛔ **不是**从 `CardTween.ToPose` 里读的（那会自证）。
                                    CardTween.Advance(CardFan.SwapTime * 0.5f);
                                    float swapSpan = Vector3.Distance(posFront, posOther);
                                    float swapProg = swapSpan > 1e-5f
                                        ? Vector3.Distance(cd.SlotView(sg).transform.position, posOther) / swapSpan
                                        : -1f;
                                    CheckNear(swapProg, 0.75f, 0.02f,
                                              $"★ 换位**半程（t = {CardFan.SwapTime * 0.5f:F3}s）位移进度 = 0.75**（实得 {swapProg:F4}）"
                                            + " —— 单条补间 `OutQuad`（原版不写 `SetEase` 的那条口径）；"
                                            + "改回 `Linear` 会变成 **0.5**，换回 Sequence 再挂 `OutQuad` 会变成 **0.9375**");
                                    CardTween.Advance(0.3f);          // 0.25s 那条 tween 走完（半程已推 0.125s）
                                    CheckTrue(!cd.IsSwapping, "0.25s 之后换位收尾（开闸）");
                                    CheckTrue(cd.FrontDef != null && cd.FrontDef.Id == sgDef.Id,
                                              $"★ **被点那张换到了前台**（现在是「{cd.FrontDef.Name}」）—— 原版「点谁就把谁换到前面」");
                                    CheckNear(Vector3.Distance(cd.SlotView(0).transform.position, posFront), 0f, 0.01f,
                                              "★ 被点那张现在站在**原来的前台位**（两两互换，不是「把谁提到最前」）");
                                    CheckNear(Vector3.Distance(cd.SlotView(sg).transform.position, posOther), 0f, 0.01f,
                                              "★ 原来那张前台让到了**被点卡的槽位**（同上）");
                                    // 闸②：点前台自己 ⇒ 什么都不做（也别让这一下落到遮罩上把窗关掉）
                                    int hitFront = 0;
                                    var hf = FindChild(cd.transform, "CardHit " + hitFront);
                                    var hfb = hf != null ? hf.GetComponent<WindowButton>() : null;
                                    CheckTrue(hfb != null, "**前台那格也有点击区**（不然点它会落到遮罩上**把窗关掉**）");
                                    if (hfb != null) hfb.Click();
                                    CheckTrue(cd.CurrentState != WindowState.Closed,
                                              "★ 点前台那张 ⇒ **窗不关、也不换位**（原版闸②）");
                                    CheckTrue(cd.FrontDef.Id == sgDef.Id, "…而且前台还是刚换上去那张（没被点回去）");
                                    // 拍照前换回**格子最多**的那张（5 格）—— 截图是拿来**看扇形**的
                                    if (moa != null) cd.ShowCard(moa);
                                }
                            }
                        }
                        // ⑤ **着色 / 分层** —— 2026-09-28 按用户给的实拍（《点击卡片查看详情的参考.png》）
                        //    订正的两条（判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §十·2 / §十·5）
                        {
                            var s0 = cd.SlotView(0);
                            var sN = cd.SlotView(cd.SlotCount - 1);
                            CheckTrue(s0 != null, "（前提）前台那张在");
                            CheckNear(s0 != null ? s0.Tint.r : -1f, 1f, 0.01f,
                                      "★ 前台那张**不压暗**（原版前台色 = (1,1,1,1)）");
                            CheckTrue(cd.SlotCount < 2 || sN.Tint.r < 0.99f,
                                      "★ 相关卡**压暗**（原版 `cardInBackGroundColorTint` = 0.65）");
                            if (cd.SlotCount >= 2)
                                CheckNear(sN.Tint.r, 0.65f, 0.01f, "…而且是 **0.65**，不是我们随手取的");
                            // 🔴 遮罩必须在**卡格之下**：原来卡格 3009–3017、遮罩 3110 ⇒ **整叠卡被压暗一半**
                            var shadeNode = FindChild(cd.transform, "Menu Dark Background");
                            var shadeQuad = shadeNode != null ? shadeNode.GetComponentInChildren<ImageQuad>() : null;
                            int shadeQ = shadeQuad != null ? shadeQuad.RenderQueue : -1;
                            int cardQ = CardQueue(s0);
                            CheckTrue(shadeQ >= 0, $"（前提）遮罩在（队列 {shadeQ}）");
                            // 🆕 A47：压暗层命中区那条不变量 —— 档 = `QShade`(3105)（**压暗层自己那一档**），
                            //   严格低于内容命中区最低档 `QCdHit`(3118)。改前是 `QCdHit − 1` = 3117
                            //   （= `QCdText`，**文字那一档** ⇒ 落在别的层上）。
                            //   🔴 A77⑬③：期望值改用**上面已经量到的那颗视觉压暗层 quad**（`shadeNode`）——
                            //      本行**不再传 `CardDetailPopup.QShade`**（那与被测实参同源 = 同义反复）。
                            MenuDraw.CheckShadeRule(CheckTrue, "卡片详情窗", cd.ShadeHit, shadeNode,
                                                    CardDetailPopup.QCdHit);
                            var clHit = FindChild(cd.transform, "BackgroundHit");
                            CheckTrue(clHit != null && clHit.GetComponent<WindowButton>() != null,
                                      "…而且那块命中区带 `WindowButton`（`PointerLayer` 靠它派发点击）");
                            CheckTrue(cardQ > shadeQ,
                                      $"★ 卡格队列（{cardQ}）**高于遮罩**（{shadeQ}）—— 卡画在压暗层之上"
                                      + "（原版那棵树里 `Menu Dark Background` 是第一个孩子）");
                            // 风味底图：**按阵营**选图（2026-09-28 刚导进工程的那 13 张）
                            var loreBgNode = FindChild(cd.transform, "FlavourTextBG");
                            var loreImg = loreBgNode != null ? FindChild(loreBgNode, "Image") : null;
                            var loreQuad = loreImg != null ? loreImg.GetComponent<ImageQuad>() : null;
                            CheckTrue(loreQuad != null,
                                      "★ 风味底图建出来了（原版 `FlavourTextSO.GetClanFlavorBackground` 按阵营选）");
                            var frontDef = cd.FrontDef;
                            if (loreQuad != null && frontDef != null)
                                CheckTrue(loreQuad.Texture != null
                                          && loreQuad.Texture.name == "flavourbg_" + frontDef.Faction.ToLowerInvariant(),
                                          $"★ …而且取的是**这个阵营**那张：`{loreQuad.Texture.name}`"
                                          + $"（卡是 {frontDef.Faction}）—— 判据是原版那张 army→资产 表");
                        }
                        // ================================================================
                        //  🆕 A156（2026-10-07）：**点击区（`CardHit 0`）的双轴比例 + 偏置**
                        //  判据 = 解包原件字段（第一权威 · 现读现核）：
                        //    · 菜单树 `bundle_scenes_scenes_mainmenuwarpforge/RectTransform/RectTransform_1220.json`
                        //    · 战斗树 `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2801.json`
                        //    两颗 `UI Collider` **逐字段相同**：
                        //      `m_AnchorMin(0,0)` · `m_AnchorMax(1,1)`（**拉伸锚**）· `m_Pivot(0.5,0.5)`
                        //      · `m_AnchoredPosition(0, −0.02)` · `m_SizeDelta(−0.2, −0.44)`
                        //    父件 `2DCard`（`RectTransform_1323` / `RectTransform_2876`，两棵同为
                        //      `anchor(0.5,0.5)` · `ap(0,0)` · **`m_SizeDelta = 2.0927 × 3.3313`** 卡单位）
                        //    ⇒ 点击区 = (2.0927−0.2) × (3.3313−0.44) = **1.8927 × 2.8913** 卡单位，
                        //      中心比卡心**低 0.02 卡单位**（= 前台 5px）
                        //    ⇒ 前台（scale 250 · 卡心 (960,480) · 卡体 523.175 × 832.825）：
                        //      **四沿 = 723.412 / 123.587 / 1196.588 / 846.413**
                        //      = 左右各缩 **25**（= 0.2 卡单位 × 250）· **上缩 60 · 下缩 50**
                        //   ⛔ 期望值写**原版 prefab 复算出来的字面量**，⛔ **不写** `CardFan.HitRatioX/Y`
                        //      —— 那是被测实现传进去的实参（同式自证：改实现它照样绿）。
                        //  ⚠️ 改前是「**一个 0.9043 双轴同用 + 居中**」⇒ 上沿 103.438 / 下沿 856.562
                        //      （y 轴多吃上 20.149 / 下 10.149）—— 那两条窄带**在原版是会关窗的**。
                        {
                            var hitq0 = FindChild(cd.transform, "CardHit 0");
                            var hq0 = hitq0 != null ? hitq0.GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(hq0 != null, "`CardHit 0`（前台那格的点击区）在");
                            if (hq0 != null)
                            {
                                float hx1, hy1, hx2, hy2;
                                QuadRectOf(hq0, out hx1, out hy1, out hx2, out hy2);
                                CheckNear(hx1, 723.412f, 1.5f,
                                          "★ 点击区**左沿** = **723.412**（原版 `UI Collider` `sd(−0.2,−0.44)` 复算"
                                        + " —— 左缩 25px = 0.2 卡单位 × 250）");
                                CheckNear(hy1, 123.587f, 1.5f,
                                          "★ …**上沿** = **123.587** ⇒ **上缩 60**（⚠️ 不是 50 —— 中心被"
                                        + " `m_AnchoredPosition.y = −0.02` 拉低了 5px；改前量到 **103.438**）");
                                CheckNear(hx2, 1196.588f, 1.5f, "…**右沿** = **1196.588**（右缩 25）");
                                CheckNear(hy2, 846.413f, 1.5f,
                                          "…**下沿** = **846.413** ⇒ **下缩 50**（改前量到 **856.562**）");
                            }
                        }
                        cd.PlayVoice();     // 有就播、没有就出声 —— 两种都接受（判据是它**不静默**）
                        Shoot("08_收藏_卡片详情窗.png");
                        var sh = cd.ShadeHit; var shb = sh != null ? sh.GetComponent<WindowButton>() : null;
                        if (shb != null) shb.Click();
                        Check(cd.CurrentState, WindowState.Closed, "点遮罩 ⇒ 窗关上（**原版全树没有关闭钮**，就这一条路 + ESC）");

                        // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据 → `:2298` 那一段）。
                        //   ⚠️ 上面那句点的是**本窗自己的** `ShadeHit`；本块补公共口那一版（多断三条结构/前提）。
                        //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后（本块下面紧接着就是 `Styles 页` 那一节，
                        //      与 `cd` 无关）；上面已经关过一次 ⇒ 先开回来。
                        CheckTrue(cd.TryOpen(), "（A796 现场）把卡片详情窗开回来 —— 下面那条要在**开着**的窗上点");
                        MenuDraw.CheckShadeClickRule(CheckTrue, "卡片详情窗", cd.transform, cd.ShadeHit,
                                                     () => cd.CurrentState);

                        // ================================================================
                        //  🆕 2026-10-17（D17 = 账上 A856）：**没有异画 ⇒ 整块 `SetActive(false)`**
                        //  判据（第一权威 · 本轮现读）= `d:/2/tools/decomp_full/AlternateArtPanel__Initialize.c:22-38`：
                        //    · `RawCardScript.HasAlternativeArtStyles(card) == false` ⇒ 填两个 0 之后
                        //      **`UnityEngine.GameObject.SetActive(gameObject, 0)`** —— **整块关**
                        //      （⛔ 不是「画成 `0 of 0` / `No alternate art`」，那正是我们改前的样子）；
                        //    · 有异画风格那一支才是 `SetActive(…, 1)`（`:35-38`）。
                        //  ⚠️ **两向都要断**：只断「没有 ⇒ 关」，一个「恒关」的实现会假绿；
                        //    只断「有 ⇒ 开」，一个「恒开」的实现会假绿 ⇒ 两条合起来才关得住。
                        //  ⚠️ 判据是**数据**（这张卡在不在异画表里），⛔ 不是「贴图加载出来没有」——
                        //    全文与理由 → `CardDetailPopup.HasAltArtStyle` 的注释。
                        //  ⚠️ 本块会把窗**换两张卡再关掉**：排在**本窗所有断言之后**（下面只剩 `Styles 页` 那一节）。
                        {
                            var plainDef = cd.Card;      // 当前这张 —— 上面那条刚断过它的异画标题是 `Alternate art`（= 没有异画）
                            string altId = null;
                            bool plainOk = plainDef != null;
                            foreach (var a in CollectionWindow.AltArtCards)
                            {
                                if (CollectionData.Card(a.CardId) == null) continue;   // 本地卡池里没有的跳过
                                if (plainDef != null && a.CardId == plainDef.Id) plainOk = false;  // 撞上了 ⇒ 这一向换不了对象
                                if (altId == null) altId = a.CardId;
                            }
                            CheckTrue(altId != null,
                                      "（前提）异画表里至少有一张卡在本地卡池里 —— 否则下面两条都成空转");
                            CheckTrue(plainOk,
                                      "（前提）当前这张卡**不在异画表里** ⇒ 下面「没有异画」那一向不是空转");
                            // ① 有异画 ⇒ **开着**
                            if (altId != null)
                            {
                                cd.ShowCard(CollectionData.Card(altId));
                                var apOn = FindChild(cd.transform, "Alternate Art Panel");
                                CheckTrue(apOn != null && apOn.gameObject.activeSelf,
                                          $"★ 有异画的卡（`{altId}`）⇒ 异画面板**开着**（原版 `SetActive(…, 1)`）");
                            }
                            // ② 没有异画 ⇒ **整块关**（节点还在树上，只是 `activeSelf == false`）
                            if (plainOk)
                            {
                                cd.ShowCard(plainDef);
                                var apOff = FindChild(cd.transform, "Alternate Art Panel");
                                CheckTrue(apOff != null,
                                          "「没有异画」时**节点仍在树上**（原版是 `SetActive(false)`，⛔ **不是不建**）");
                                CheckTrue(apOff != null && !apOff.gameObject.activeSelf,
                                          "★ 没有异画的卡 ⇒ 异画面板**整块 `SetActive(false)`**"
                                        + "（原版 `AlternateArtPanel__Initialize.c:27-33`）"
                                        + " —— 改坏法：把 `BuildAltArt` 末尾那句 `p.gameObject.SetActive(has)` 删掉 ⇒ 本条红");
                            }
                            cd.Close();     // 收尾：本块换过两张卡 ⇒ 交还一个**关着**的窗（同本块进来时的状态）
                        }
                    }
                }
            }

            // ============================================================ Styles 页（异画，2026-09-24）
            //
            // 几何出处：`CollectionWindow.StyleView` 那段注释（**从 `Collection Menu Variant` 根走下来实读的**）。
            // 判据一律是**原版数**，不是我们自己的常量（否则是自证）。
            Section("Styles 页：换风格条 + 异画网格（6 列 · 262.5×384 · 本地 7 张 / **1 档**）");
            win.tabButtons.Click(3);
            Check(win.CurrentTab, WindowTabType.CollectionStyles, "点第 4 键 ⇒ 切到 **Styles** 页");
            CheckTrue(win.PageRoot(3) != null, "页节点 `Alternate Art Tab` 在（**卡面文案是 `STYLES`**）");
            Check(CollectionWindow.AltArtCards.Length, 7,
                  "异画读数 = **7**（本地只有这 7 张督军异画；图片文件名两套写法 `AA_HB_…` / `…_v2`，**但那不是两种风格**）");
            Check(CollectionWindow.AltStyles.Length, 1,
                  "风格数 = **1**（原版 `AlternateArtSyle` 只有 `Default`/`HammerAndBolter`，而轮播走 "
                  + "`GetAlternateArtStyleOnly` = `FindAll(style != 0)` ⇒ 只剩 `HammerAndBolter`；"
                  + "本地 7 张卡的 `style` 现读全是 **5**。判据 → `CollectionWindow.StyleLogoName` 头注）");
            Check(CollectionWindow.StyleCols, 6,
                  "列数 = **6** = floor(1589.78 ÷ 262.5)（⚠️ 不是 `_segments=4` —— 那是死值）");
            var spage = win.PageRoot(3);
            var svp = spage != null ? FindChild(spage, "Scroll View") : null;
            CheckAt(svp, 330.22f, 1920.01f, 287.67f, 1080f, "`Scroll View`（异画视口 **1589.78 × 792.33**）");
            // 两个换风格圆钮：**按位置**（脚本字段名反着：`leftStyleButton` 挂的是右边那颗）
            var aL = spage != null ? FindChild(spage, "Select Art Button Left") : null;
            var aR = spage != null ? FindChild(spage, "Select Art Button Right") : null;
            CheckNear(aL != null ? MenuCheck.DesignPx(aL).x : -1f, 720.595f, 1f,
                      "`Select Art Button Left` 中心 x = **720.60**（683.40 + 74.39/2）");
            CheckNear(aR != null ? MenuCheck.DesignPx(aR).x : -1f, 1357.59f, 1f,
                      "`Select Art Button Right` 中心 x = **1357.59**（1320.40 + 74.39/2）");
            CheckTrue(aL != null && aR != null && Mathf.Abs(MenuCheck.DesignPx(aL).y - 228.47f) <= 1f,
                      "两个圆钮中心 y = **228.47**（190.67 + 75.61/2）");
            // 🆕 **2026-10-18（`A1149` 第一半 · 第九会话 P6）**：这两颗**换风格圆钮**的**圆底盘实绘矩形**
            //   —— 同族那个「圆底盘长在子件上」的偏离（判据 = 原版 `/Header/Select Art Button {L,R}` 自己带
            //   `Image[UI_Button_Round_background]` · Simple · `preserveAspect` · `sizeDelta 74.386×75.605`，
            //   → `资料/普查产出_0923/A4_装饰页与驱动链.md:129`）**已归真到根节点自己身上**
            //   （`Shell/CollectionWindow.cs`）⇒ 第 8 实参不传。期望值 = 正方贴图 237×237 等比内接 ⇒ **74.39×74.39**。
            //   ⚠️ 只断左钮：两颗走**同一个** `BuildStyleArrow`（⇔ 同一份代码），右侧那颗的矩形是同一支算出来的。
            A1125CloseBase("CollectionWindow/Select Art Button Left", spage,
                           "Select Art Button Left", "UI_Button_Round_background",
                           74.39f, 74.39f, 75.61f);
            // 🔴 **箭头那一层必须比黄底高一级队列** —— 两层摆在同一个矩形上，同队列时「谁盖谁不可控」
            //    （2026-09-24 实拍：箭头**整个没出现**、只看得见黄底，而矩形断言全绿）。
            //    判据照坑表那条：**比 `RenderQueue`，不比 z**。
            {
                var lIcon = aL != null ? aL.Find("Icon") : null;
                var lBg = aL != null ? aL.Find("Background") : null;
                var iq = lIcon != null ? lIcon.GetComponentInChildren<ImageQuad>() : null;
                var bq2 = lBg != null ? lBg.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(iq != null && iq.Texture != null, "左箭钮的箭头图**真的有贴图**（`40k_general_bt_arrow`）");
                CheckTrue(iq != null && bq2 != null && iq.RenderQueue > bq2.RenderQueue,
                          $"箭头那层队列 **{(iq != null ? iq.RenderQueue : -1)}** > 黄底那层 **{(bq2 != null ? bq2.RenderQueue : -1)}**"
                          + "（同队列时箭头会被盖掉 —— 实测过一次）");
            }
            // `Art Style Logo` 那一格：**原版是 `Image`**（`AlternateArtCardCollectionTab.styleImage`
            //   ← `AlternateArtStylesUtilities.GetStyleIcon(style)`）⇒ 现在**照原版画 logo 图**。
            //   🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`）**：原来这里断的是「那一格的字 == `Hammer and Bolter`」
            //   以及「字的渲染宽度 ≤ 512」—— 那两条**随「我们画字」一起作废**；原来那句「原版 `sprite=0`、
            //   风格图 SO **本地没有**」也**不成立**（SO 在本地内置文件里、它那两张图现在都在工程里）。
            //   映射与判据 → `CollectionWindow.StyleLogoName` 的头注（SO 序列化体 + 远端 catalog + 束内 `m_Container` 三路互证）。
            var sLogo = FindChild(spage, "Art Style Logo");
            var sLogoIq = sLogo != null ? sLogo.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(sLogoIq != null && sLogoIq.Texture != null,
                      "`Art Style Logo` 那格**真画出了图**（原版那格是 `Image`，⛔ 不是字）—— "
                      + "载不到图时实现会**退回画字并出声**，所以这一条同时盯着那个兜底有没有被静默触发");
            CheckText(sLogoIq != null && sLogoIq.Texture != null ? sLogoIq.Texture.name : "(null)",
                      "Cell_Shaded_Alternative_Art_Style",
                      "那一格的图 = **`Cell_Shaded_Alternative_Art_Style`**（= 原版 `HammerAndBolter` 档的 logo，"
                      + "图上印的是 `CEL SHADED`）");
            // 🔴 **量画出来那块**（⛔ 不是量字）：那一格 512×128、图也是 512×128 ⇒ 画出来就该是 512×128。
            //    量法走同文件 `QuadRectOf`（= `MenuDraw.QuadRectPx`，**独立于实现**的渲染四边形）。
            {
                float qx1 = 0f, qy1 = 0f, qx2 = 0f, qy2 = 0f;   // ⚠️ 必须给初值：`gotQ && …` 短路 ⇒ 编译器证不出已赋值（CS0165，已踩过）
                bool gotQ = sLogoIq != null && QuadRectOf(sLogoIq, out qx1, out qy1, out qx2, out qy2);
                CheckTrue(gotQ && Mathf.Abs((qx2 - qx1) - 512f) <= 1.5f && Mathf.Abs((qy2 - qy1) - 128f) <= 1.5f,
                          $"`Art Style Logo` 画出来 **{(gotQ ? qx2 - qx1 : 0f):F1}×{(gotQ ? qy2 - qy1 : 0f):F1}**"
                          + " = 原版那一格 **512×128**（图与框同比例 ⇒ 等比不缩、也不溢出到右箭钮上）");
            }
            // 🔴 **量渲染宽度**（不是比字号）：那一格是 **512×128**，56px 的 `Hammer and Bolter`
            //    实测宽 ≈1270px ⇒ **会压到右箭钮上**（第一版实拍一眼可见）。判据照 `AutoFitBox` 那条教训。
            // 🔴 **2026-10-16（A830 换口）**：那个数**不再从实现里读**。原来读 `win.StyleLogoWidthPx`，
            //    而它是 `Shell/CollectionWindow.cs` 的一个属性、读的是 `Label.WorldW`
            //    = **字段缓存** `_tmpW/_tmpH`（只有 `RefreshBounds()` 写 —— 也就是**被测实现自己**；
            //    `SetFontSize` / `SetCharSpacing` 这一族只重排 mesh、**不刷缓存** ⇒ 谁在末次刷缓存之后
            //    重排一次，旧口照旧报旧值）⇒ **尺子长在被测实现身上**（A796′ 同族「更外层还在的口」，RO 漏列）。
            //    ✅ 现在**自检自己量**：TMP 自己渲出来那块 `textBounds`（`LabelRenderedPx`，与同文件
            //    `RectOf` / `TitleLeftPx`、`Editor/ShopScene.cs` / `RewardsScene.cs` **同一条口**）。
            //    ⚠️ 两条量法**同源**（`RefreshBounds` 读的就是它）⇒ **换口当天逐位同值**，
            //    期望值 512 与容差 1px **一个都不用动**；变的只是「以后重排看得见」+「尺子不再来自实现」。
            //    ⚠️ 顺手把「量不到」这一档补红：旧写法在 `_styleLogo == null` 时返回 `0f`，而 `0 ≤ 513` 恒真
            //    ⇒ 那是一处**假绿**（`AutoFitBox` 教训的同族：字没了照样绿）。⛔ 别把 `sLogoLb != null`
            //    那一项当成多余 —— 少了它，下面这条在「节点整个不在」时反而变绿。
            //    改坏法（🔴 **2026-10-09（A1173）就地订正（铁律 5）** —— 原句写的是「`SetAutoFitBox` 那两个
            //    宽/高参（`CollectionWindow.BuildStylesPage` 里那句）传 `0`」；**那句已不在**：
            //    A1173 把 autosize 收进 `MainMenuSubmenuWindow.Text` 的四个形参）：
            //    **把漏斗里那两步（`SetWrapWidth` + `SetAutoFitBox`）删掉**，或把调用点的
            //    `wrapPx` / `autoMinPx` 传 `0` ⇒ 自适应不生效、`Hammer and Bolter` 按 56px 渲出来 ≈1270px ⇒ 这条当场红。
            // 🔴 **2026-10-14（`A1808`）删掉一条**：原来这里还有「那行字的**渲染宽度 ≤ 512**」——
            //    它量的是**风格名文字**，而那一格现在画的是 **logo 图** ⇒ 已由上面那两条（图在不在、图是不是那一张、
            //    画出来是不是 512×128）替代。
            //    ⚠️ 不是把「文字那一档」的判据一起丢了：下面那条 **`A1173` 形参契约探针自己建两个 `Label`**
            //    （`A1173 Probe Fit` / `NoFit`）验的就是同一件事，`wrapPx` 那一档仍被它盯着。
            // 网格：7 张里当前风格 6 张 ⇒ 2 行；首格中心
            Check(win.StyleVisibleCount, 7,
                  "当前风格（**只有 `HammerAndBolter` 这一档**）下可见 **7** 张异画"
                  + "（本地就这 7 张、全是同一档 —— 判据 → `CollectionWindow.StyleLogoName` 头注）");
            // ================================================================
            //  🆕 **2026-10-09（A1173）**：`MainMenuSubmenuWindow.Text` 那四个 autosize 形参的**契约判别式**。
            //  ⛔ **别删这一对** —— 它是「形参真的接上了」唯一**独立**的判据：屏上那 11 处收口点只看
            //  「渲染宽度 ≤ 框宽」**分不出**那个效果是**形参给的**还是**调用点自己补的**
            //  （两套写法都满足它 ⇒ 同义反复）。
            //  ⚠️ 用**真节点那一格**（`Art Style Logo` 的矩形/父件）当探针位，量法走 `LabelRenderedPx`
            //  （TMP 自己的 `textBounds`，⛔ 不是 `Label.WorldW/H` 那口缓存 —— 见该助手头注）。
            //  ⚠️ 两条探针**建完立刻 `DestroyImmediate`**（批处理下没有帧循环，`Destroy` 不生效）
            //  ⇒ 树与后续断言一位不动。
            if (spage != null)
            {
                float bw = CollectionWindow.StyleLogoR - CollectionWindow.StyleLogoL;
                var p1 = win.Text(spage, "Hammer and Bolter",
                                  CollectionWindow.StyleLogoL, CollectionWindow.StyleLogoR,
                                  CollectionWindow.StyleBarT, CollectionWindow.StyleBarB,
                                  5, Color.white, "A1173 Probe Fit", 56f,
                                  wrapPx: bw, autoMinPx: 18f, autoMaxPx: 56f);
                float w1 = LabelRenderedPx(p1).x;
                CheckTrue(p1 != null && w1 > 5f && w1 <= bw + 1f,
                          $"★ A1173 形参契约：`Text(wrapPx: {bw:F0}, autoMinPx: 18)` ⇒ 那行字**渲出来 {w1:F0}px ≤ {bw:F0}**"
                          + "（删掉漏斗里 `wrapPx > 0` 那两步 ⇒ 按 56px 渲 ≈1270px ⇒ 本条红）");
                // 🔴 **判别式那一半**：**只传 `autoMinPx`、不传 `wrapPx` = 死实参**（判据 = `MenuDraw.TextCore`
                //   那三条：`wrapPx > 0` ∧ `autoMinPx > 0` ∧ `fontPx > autoMinPx` 全真才调 `SetAutoFitBox`）。
                //   少了这一条，上面那条在「把判据放宽成只看 `autoMinPx`」时**照样绿**（= 让一个死实参蒙混过关）。
                //   ⚠️ 期望值写成**与 p1 的比值**（不写 ≈1270 那个数）—— 那两串字一样、字号一样
                //   ⇒ 比值只由「缩没缩」决定，与字体度量无关。
                var p2 = win.Text(spage, "Hammer and Bolter",
                                  CollectionWindow.StyleLogoL, CollectionWindow.StyleLogoR,
                                  CollectionWindow.StyleBarT, CollectionWindow.StyleBarB,
                                  5, Color.white, "A1173 Probe NoFit", 56f,
                                  autoMinPx: 18f, autoMaxPx: 56f);
                float w2 = LabelRenderedPx(p2).x;
                // 🔴 **2026-10-09（第十会话 · `D2` 诊断 · α）**：这条判别式原来写的是 **`w2 > w1 * 2f`**，
                //   而那个期望值**是推算的**（`P1` 自己在注释里写着「不缩时 ≈1270、**未实跑**」）——
                //   实测 **569 vs 509 = 1.118×**：因为那串字**本来就短**（字号 56 时自然宽只有 ~569px，
                //   比框 512 宽不了多少）⇒ 比值式**不成立**。
                //   ⇒ 换成**两条都不依赖字体度量**的判别式（合起来仍挡得住「只传 `autoMinPx` 的死实参」）：
                //     ① **字段级契约**：传了 `wrapPx` 的那条**必须开** autosize、没传的**必须不开**；
                //     ② **几何**：开的那条**缩进框里**、没开的那条**溢出框外**（实测 509 ≤ 512 < 569）。
                float boxW = CollectionWindow.StyleLogoR - CollectionWindow.StyleLogoL;
                CheckTrue(p1 != null && p2 != null && p1.AutoSizing && !p2.AutoSizing,
                          "★ A1173 判别式（契约）：**只有传了 `wrapPx` 的那条才开 autosize**"
                        + $"（`AutoSizing`：p1 = {(p1 != null ? p1.AutoSizing.ToString() : "null")} · "
                        + $"p2 = {(p2 != null ? p2.AutoSizing.ToString() : "null")}）"
                        + " —— 「只传 `autoMinPx`、不传 `wrapPx`」是**死实参**，这条就是挡它的");
                CheckTrue(w1 > 5f && w1 <= boxW + 1.5f && w2 > boxW + 1.5f,
                          $"★ A1173 判别式（几何）：**传了 `wrapPx` 的缩进框里、不传的溢出框外**"
                        + $"（框 {boxW:F1}px：p1 {w1:F0}px ≤ 框 · p2 {w2:F0}px > 框）"
                        + " —— ⚠️ **⛔ 别改回比值**：实测「不缩」只有 569px（那串字本来就短），"
                        + "原来那句「必须 `> 2×`」是**推算错**");
                if (p1 != null) Object.DestroyImmediate(p1.gameObject);
                if (p2 != null) Object.DestroyImmediate(p2.gameObject);
            }
            Check(win.StyleCells.Count, 7, $"画出了 {win.StyleCells.Count} 格（视口外的不建 = 那套裁切；7 张 = 2 行全在视口内）");
            if (win.StyleCells.Count > 0)
            {
                var s0 = win.StyleCells[0];
                // 内容**整体居中**：pad = (1589.78 − 6×262.5) / 2 = **7.39** ⇒ 首格中心 330.22+7.39+131.25
                CheckNear(MenuCheck.DesignPx(s0).x, 468.86f, 0.7f, "第 1 格中心 x = **468.86**（330.22 + pad 7.39 + 262.5/2）");
                CheckNear(MenuCheck.DesignPx(s0).y, 479.67f, 0.7f, "第 1 格中心 y = **479.67**（287.67 + 384/2）");
                // 🔴 判「卡面用的是**异画**立绘」—— 扫这一格**所有** `MeshRenderer` 的材质贴图名字。
                //    ⚠️ **别只看第一个**：第一版取「第一个有贴图的」拿到的是**卡框**
                //    （`astramilitarum_tier3`），断言因此误报。`CardView` 的层走的是**自定义 mesh**
                //    （不是 `ImageQuad`）⇒ 只能按 `MeshRenderer.sharedMaterial.mainTexture` 判。
                int meshCount = 0; string altTex = null;
                foreach (var mr in s0.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.sharedMaterial == null || mr.sharedMaterial.mainTexture == null) continue;
                    meshCount++;
                    if (mr.sharedMaterial.mainTexture.name.StartsWith("alt_") && altTex == null)
                        altTex = mr.sharedMaterial.mainTexture.name;
                }
                CheckTrue(meshCount > 0, $"第 1 格里画出了 `CardView` 的 {meshCount} 层网格");
                CheckTrue(altTex != null,
                          $"格里的立绘贴图 = **{altTex ?? "(一个 alt_* 都没有)"}**（**必须出现 `alt_*`** —— "
                          + "异画页画的就是它；一个都没有就说明 `CardData.artOverride` 没接上、退回了普通立绘）");
            }
            // 🆕 **2026-10-24（A18）**：**异画页的 punch 作用节点** —— A14 只给卡池页补了那几条，
            //   异画页的点击回调上一轮也一起改成了 `PunchCardCell(cellT)`，但当时**没有用例**钉住它
            //   （A14 报告 §四·2 自己列成「没做完」）。原版两页的格子是同一个 `CollectionCard`
            //   ⇒ `SelectedHighlight` 打的是同一级 = `Collection Card/Content`（判据 → `PunchCardCell` 那段）。
            //   🧨 改坏法：把异画页那处回调改回 `PunchCardCell(v.transform)` ⇒ 第 2、3 条红；
            //   把 punch 抬到视口/窗根 ⇒ 第 4 条红；把容器改成复用上一趟那个（不新建）⇒ 第 5 条红。
            if (win.StyleCells.Count > 0 && win.StyleCells[0] != null)
            {
                var sCell0 = win.StyleCells[0];                       // = `CardView` 根（`CollectionAltArt_<卡 id>`）
                var sContent0 = sCell0.parent;                        // = 本趟的 `Content <i>` 容器
                string sId = sCell0.name.StartsWith("CollectionAltArt_")
                           ? sCell0.name.Substring("CollectionAltArt_".Length) : null;
                var sHit0 = (sContent0 != null && sContent0.parent != null && sId != null)
                          ? FindChild(sContent0.parent, "AltArtHit_" + sId) : null;
                var sBtn = sHit0 != null ? sHit0.GetComponent<WindowButton>() : null;
                CheckTrue(sBtn != null,
                          $"（前提）异画第 1 格有点击区 `AltArtHit_<卡 id>`（与 `Content` **同级** —— 原版射线件在上一层）");
                if (sBtn != null)
                {
                    int altPunch = CollectionWindow.CardPunchCountForTest;
                    sBtn.Click();
                    CheckTrue(CollectionWindow.CardPunchCountForTest == altPunch + 1,
                              "★ 真点异画格 ⇒ 起**一次**选中 punch（与卡池格**同一条实现** `PunchCardCell`）");
                    var pt2 = CollectionWindow.LastPunchTargetForTest;
                    CheckTrue(pt2 != null && sContent0 != null, "（前提）拿得到 punch 作用节点 + 该格容器");
                    if (pt2 != null && sContent0 != null)
                    {
                        CheckTrue(pt2.name.StartsWith("Content "),
                                  $"★ 异画页 punch 也打在**格容器**上（实测 `{pt2.name}`；原版 = `Collection Card/Content`）");
                        CheckTrue(pt2 != sCell0,
                                  "★ …**不是** `CardView` 的根（`CollectionAltArt_*`）—— A14 之前打岔的就是这一级");
                        CheckTrue(pt2.parent != null && pt2.parent.name == "Viewport",
                                  "★ …它是**格内**那一级（父 = `Viewport`），⛔ 不是视口 / 窗根");
                        CheckTrue(object.ReferenceEquals(pt2, sContent0),
                                  "★ …而且就是**这一格**的容器（⛔ 不是别格 / 上一趟重建残留的那个）");
                    }
                    // 点格子会顺带开卡片详情窗（`OpenCardDetail`）—— 本段自己收尾，别给后面的段留一扇开着的窗
                    if (CollectionWindow.LastCardDetail != null) CollectionWindow.LastCardDetail.Close();
                }
            }
            // 🆕 **2026-10-18（T09 / `A1813`）：异画页也池化了** —— 本页**滚不动**
            //   （7 张 = 2 行 × 384 = **768 < 视口 792.33**，`MaxOffset` 恒 0），所以直接调
            //   **滚动回调那一趟的同一个函数**（`RebuildStyleCellsForTest`）验「池命中」。
            //   改坏法：把 `RebuildStyleCells` 首句「只清每趟重建的件（`AltArtHit_*` + `Content *` 容器，
            //   先把池里的 `CardView` 摘出来）」改回「清 `parent` 下全部孩子」（并走 `CardView.Create`）⇒ ①② 当场红（7 张）。
            if (win.StyleCells.Count > 0)
            {
                int styleBuiltFirst = win.StyleCellCreatedLastRefresh;
                Check(styleBuiltFirst, win.StyleCells.Count,
                      $"（前提）异画页**起手那一趟确实新建了** {win.StyleCells.Count} 张 —— 计数器本身是活的"
                    + "（⛔ 少了这条，下面「新建 0」就是 0 vs 0 的假绿）");
                var stBefore = win.StyleCells[0];
                win.RebuildStyleCellsForTest();
                CheckTrue(win.StyleCellCreatedLastRefresh == 0,
                          $"★ 重画异画页一趟 ⇒ 新建 `CardView` **{win.StyleCellCreatedLastRefresh}** 张（期望 **0**）"
                        + " —— 旧路（`DestroySafe` 整屏 + `Create`）会重建 " + styleBuiltFirst + " 张 ⇒ 本条当场红");
                CheckTrue(win.StyleCells.Count > 0 && object.ReferenceEquals(win.StyleCells[0], stBefore),
                          "★ …第 1 格还是**同一个 `CardView` 实例**（`object.ReferenceEquals`）—— 池化 = 搬实例");
                Check(win.StyleCells.Count, win.StyleVisibleCount,
                      $"重画后仍是 {win.StyleVisibleCount} 格（池化没把格子数弄丢、也没裂成两份）");
            }
            // 左抽屉：**起手收起**。
            // ⚠️ **2026-10-05 更正（铁律 5）**：原文写「出厂展开（实证 act=T —— ⚠️ 与 Cosmetics 页相反）」
            //   并断 `StyleFiltersOpen == true` —— **字段读数（`act=T`）对、推论错**：
            //   `act=T` 只等于「节点启用」，**推不出**「抽屉停在哪一头」（同 prefab 的卡组编辑窗那份
            //   `Card Filters` 也是 `act=T`，而它早已独立证实起手收起）。
            //   判据（VA 反汇编 —— `decomp_full` 里这几个**泛型方法体确实没有**）：
            //     `CollectionTab<object>$$Setup`（`0x1815F3C00`）尾调用 `display.Initialize(GetCollection())`
            //     → `CollectionDisplay<object>$$Initialize`（`0x1815EC0A0`）挂 `filterToggle.onValueChanged`
            //       并调 `filters.SetupFilters()`（`0x1815EC278`）
            //     → `SetupFilters`（`0x1815F0740`）收尾 `anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`
            //   ⇒ **每个页签一建出来就停在「收起」那一头**，之后没有任何一处起手把它打开
            //   （唯一的开启者 = 页头那颗 toggle 的 `onValueChanged`）。旁证：四颗 `Filter Toggle`
            //   的 `m_IsOn` 全是 0、出厂画的是 `offSprite`。
            //   逐字段实读 = `资料/普查产出_1005/块8_卡组窗断言与异画页查证.md` 件 B。
            // 🔴 两条**能区分两种状态**的断言（⛔ 不是同义反复、也不是一条恒假）：
            //    ① 断**节点真收着**那一头 —— 量的是**可见性**（`activeSelf`，= `ApplyDrawerSlide` 里
            //       那句「滑出去了才关」的结果）、**位置**进度 `DrawerSlide`、**命中区** `DrawerSettled`，
            //       ⛔ **不是**那个逻辑态 bool 自己；
            //    ② 真去**点一次页头那颗钮**（`FiltersHit` 上的 `WindowButton.Click()`，与 `PointerLayer`
            //       派发的是**同一个** `Click()`）⇒ **才开**。
            //   少了 ②，「起手收起」可以被「一直收着、点了也不开」蒙过去；少了 ①，② 也证明不了起手态。
            var styleDrawer = spage != null ? FindChild(spage, "Card Filters") : null;
            CheckTrue(styleDrawer != null, "（前提）异画页左抽屉节点 `Card Filters` 建出来了 —— 下面几条都靠它");
            CheckTrue(!win.StyleFiltersOpen, "异画页左抽屉**逻辑态起手收起**（`FilterPanel.Open == false`）");
            CheckTrue(styleDrawer != null && !styleDrawer.gameObject.activeSelf,
                      "…而且**节点真的收着**（`activeSelf == false`）—— 起手整栏在屏幕左外（`hiddenPosition.x`）");
            CheckNear(win.DrawerSlide(1), 0f, 0.001f,
                      "…位置进度 = **0**（0 = 已滑出到 `hiddenPosition`；1 = 回到原位）");
            CheckTrue(!win.DrawerSettled(1), "…没到位 ⇒ 这一栏的命中区/滚轮都不生效");
            var styleFltHit = spage != null ? FindChild(spage, "FiltersHit") : null;
            var styleFltBtn = styleFltHit != null ? styleFltHit.GetComponent<WindowButton>() : null;
            CheckTrue(styleFltBtn != null, "页头那颗 `Filter Toggle` 的命中区 `FiltersHit` 在（点它才开）");
            if (styleFltBtn != null)
            {
                styleFltBtn.Click();        // 🔴 **真点一次**（不是直调 `win.ToggleStyleFilters()`）
                CheckTrue(win.StyleFiltersOpen && styleDrawer != null && styleDrawer.gameObject.activeSelf,
                          "点一下页头 ⇒ 抽屉**才开**：逻辑态 true **且**节点 `activeSelf == true`");
                CheckNear(win.DrawerSlide(1), 1f, 0.001f,
                          "…而且真滑回原位：进度 **1**（批处理里 `Toggle*` 直接到位，见 `StartDrawerSlide`）");
                CheckTrue(win.DrawerSettled(1), "…到位 ⇒ 命中区/滚轮恢复");

                // ---------------- 🆕 2026-10-11（A248 · A249）：异画页抽屉的**字号那一档**（与卡牌页不同）
                //
                // 🔴 **「一个值 ≠ 全部情况」（铁律 5·c）**：同一个 `FilterPanelModel`、**同一套矩形**，
                //   而**异画页**是**另一套字号**。判据 = **现读的 dump 原始行**（可复跑）：
                //   `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 22 --no-sprite`
                //     · 异画页 `Name FIlter/…/Text Area/{Placeholder,Text}`   = **35 · `auto[10~35]`**
                //     · 异画页 `Owned Toggle/Label` 与 `Upgradable only/Label` = **36 · `auto[10~36]`**
                //     · 异画页四个 `Title`（Army/Rarity/Energy Cost/Type）     = **36**（**无 `auto[…]`** ⇒ 不开自适应）
                //   对照（同一份 dump）：**卡牌页**那一份 = `30 · auto[18~30]` / `32 · auto[18~32]` / `32`。
                //   稀有度 / 费用 / 类型三族**两页相同**（23.2 auto[10~27] / 45 auto[25~45]）⇒ 不走这一档。
                //
                // ⛔ **期望值一律写上面的原版字面量**，⛔ 不从 `FilterPanelModel.*Styles` 常量读
                //    （那是被测实现**传进去的实参** ⇒ 同式自证）；读的是 **TMP 自己那两个真字段**
                //    （`fontSizeMin/Max`，经工程唯一那份 `Label.FontSizeToPx` 折回画布 px —— 与 A247/A270 同一口径）。
                // 🔴 **改坏哪里它会红**：把 `BuildFilterPanel(…, styles: true)` 那个实参删掉 / `_flt.Styles` 忘了往
                //    下传 ⇒ 异画页那 6 条全红（退回 30/18、32/18、32）；把**共用常量**改成 35/36 ⇒ 下面的
                //    **卡牌页对照组**红（且卡牌页会一起被改歪）。
                {
                    var sNF = FindChild(styleDrawer, "Name Filter");
                    var sInT = sNF != null ? FindChild(sNF, "Input Text") : null;
                    var sInLb = sInT != null ? sInT.GetComponent<Label>() : null;
                    CheckTrue(sInLb != null, "（前提）异画页搜索框的 `Input Text` 建出来了");
                    if (sInLb != null)
                    {
                        CheckNear(Label.FontSizeToPx(sInLb.FontSizeMax), 35f, 0.6f,
                                  "★ A248：异画页搜索框字号 = 原版 **35**（卡牌页是 30）");
                        CheckNear(Label.FontSizeToPx(sInLb.FontSizeMin), 10f, 0.6f,
                                  "…自适应下界 = 原版 **10**（卡牌页是 18）—— 只改上界不改下界 ⇒ 这条红");
                        // 🔴 **A249** 与字号无关、**两页同档**：`Text` 那一半原版就是 `折行=3`
                        //    （`Placeholder` 是 0，我们这颗 `Label` 兼作两者 ⇒ 取 `3`，同 `DeckEditInputWrap` 那条先例）。
                        //    改坏法：删掉 `TextAligned` 里那句 `SetWrappingMode` ⇒ 退回 `SetAutoFitBox` 开的 1 ⇒ 红。
                        Check(sInLb.WrappingMode, 3,
                              "★ A249：异画页搜索框 **`折行=3`**（`PreserveWhitespaceNoWrap`，TMP 给单行输入框那一档）"
                              + "—— `SetAutoFitBox` 会无条件开成 1（`Normal`）⇒ 不显式设就退回 1");
                    }
                    var sOwT = FindChild(styleDrawer, "Cell_owned");
                    var sOwL = sOwT != null ? FindChild(sOwT, "Label") : null;
                    var sOwLb = sOwL != null ? sOwL.GetComponent<Label>() : null;
                    CheckTrue(sOwLb != null, "（前提）异画页 `Owned only` 那一格的标签在");
                    if (sOwLb != null)
                    {
                        CheckNear(Label.FontSizeToPx(sOwLb.FontSizeMax), 36f, 0.6f,
                                  "★ A248：异画页 `Owned only` 字号 = 原版 **36**（卡牌页 32）");
                        CheckNear(Label.FontSizeToPx(sOwLb.FontSizeMin), 10f, 0.6f,
                                  "…自适应下界 = 原版 **10**（卡牌页 18）");
                        Check(sOwLb.WrappingMode, 0,
                              "…而折行**两页同档**（原版 `'Owned only' 折行=0`）—— 按页分的是**字号**、不是折行；"
                              + "把 `LabelWrap` 也按页分 ⇒ 这条红");
                    }
                    string[] sTitleNames = { "Title Army", "Title Rarity", "Title Energy Cost", "Title Type" };
                    int sTitleSeen = 0;
                    for (int ti = 0; ti < sTitleNames.Length; ti++)
                    {
                        var tt = FindChild(styleDrawer, sTitleNames[ti]);
                        var tlb = tt != null ? tt.GetComponent<Label>() : null;
                        if (tlb == null)
                        {
                            // ⚠️ **正常**：四行是按视口建的（`TitleRow` 先 `Intersects` 再建）——
                            //   `Type` 那一行在 y 1155.9 而视口到 1079.99 ⇒ 这一档**本来就该没有节点**。
                            //   ⛔ 别把这条写成 `CheckTrue(false,…)`（那会把「按视口建」这件对的事判成红）。
                            Debug.Log(P + $"  （异画页小标题 `{sTitleNames[ti]}` 这次在视口外 ⇒ 不建节点，跳过）");
                            continue;
                        }
                        sTitleSeen++;
                        CheckNear(tlb.FontPxNow, 36f, 0.6f,
                                  "★ A248：异画页 `" + sTitleNames[ti] + "` 字号 = 原版 **36**（卡牌页 32）"
                                  + "（原版这四行**不开自适应**、也没有 `auto[…]` ⇒ 这一档落在标称字号上）");
                    }
                    CheckTrue(sTitleSeen >= 3,
                              $"★ A248：异画页真的建出了 **≥3** 行小标题（实得 {sTitleSeen}）——"
                              + "只建出 1~2 行 ⇒ 上面那一组等于没验");

                    // ---- 🧪 **对照组：卡牌页必须还是原版那一套**（30 / 18 · 32 / 18 · 32）----
                    //   ⛔ 这是「按页分参数」这条裁定的**另一半**：只把异画页改对、顺手把共用常量也改掉 ⇒ 卡牌页被改歪。
                    //   🔴 **2026-10-14（#57–#59 · γ · 夹具侧）就地订正（铁律 5）**：这里原来写
                    //     「卡牌页抽屉此刻是**收着**的，但节点都在」—— **那个前提已经不成立**：
                    //     B1/A781 之后「整块落在视口外的文字**不建**」（`GameWindow.Text` / `MenuWindowBase.Text`
                    //     头一句就是 `MenuDraw.Visible`），而本页筛选列**最后一次重建**在本文件上面
                    //     「`Empty Collection Warning`」那一节里那次 `ClearCardFilters()`
                    //     （⚠️ 原来这里写 `:3858` / `:3802` —— **行号已漂、当场改成锚点**，铁律 5）——
                    //     那一刻抽屉是**收着**的（同一节上面几行刚关）、整栏滑在屏幕左外
                    //     ⇒ 那三件 `Label`（`Input Text` / `Cell_owned/Label` / `Title Army`）**压根没建出来**
                    //     （容器节点照建 ⇒ 上面那条「`Card Filters` 节点在」仍然绿，是个假前提）。
                    //     🔴 **2026-10-16 订正（A835 · 铁律 5）：这一句只对「A811 根治之前」成立** ——
                    //     根治之后框改取 `ViewportClip.BaseRect`（宿主写进去的设计矩形）⇒ 收起期间那次
                    //     重建**照样把这三件建出来**（详见下面 ② 那两段订正）。⚠️ ①② 两步夹具照旧留着
                    //     （① 让 TMP 在**活着的那一页**上量、② 把抽屉摆到**实现真正工作的那一态**），
                    //     但「这三件在不在」**今天不再是那条修法的验收口**。
                    //   ⇒ 夹具先摆到**实现真正工作的那一态**再查（**三条断言一个字没改**）：
                    //     ① 切到卡牌页 —— 这三条读的是 TMP 自己的字段，要在**活着的那一页**上量（同异画页那段）；
                    //     ② 真点页头那颗钮，把抽屉**展开到位**（与上面异画页那条 `styleFltBtn.Click();`
                    //        同一套；⚠️ 原来这里写 `:4981` —— 行号已漂，改成锚点）；
                    //   🔴 **2026-10-16 订正（铁律 5）**：这里原来还有 ③「**在原地重排一次**」（理由是
                    //     「展开这一下**不重建** ⇒ 收起时重建过的那一版不会自己回来」）—— **那句话从
                    //     2026-10-16 起不成立**：A811 的**最小修法**当天已落进 `Shell/CollectionWindow.cs`
                    //     的 `ApplyDrawerSlide` 尾「④」（滑到展开位那一拍自动补一次 `RebuildFilterRows`）
                    //     ⇒ ③ 删掉、⛔ 只留「点展开」那一步。
                    //   🔴 **2026-10-16 再订正（A835 · 铁律 5）：末句「把 ④ 删掉 ⇒ 三条当场全红
                    //     （收起态建的那一版里这三件**真的不在**）」已经不成立** —— 那是**根治之前**的
                    //     实况。根治之后收起态建的那一版里这三件**是在的**（见下面 ② 的订正）⇒
                    //     下面三条「（前提）…在」**删掉 ④ 也不会红**（它们只查在不在、不查落点）。
                    //     ✅ **2026-10-16 当天已补（A843）**：下面「（前提）…在」那三条**不再**是 ④ 的
                    //     验收口；盯 ④ 的是**本节末尾**新加的那一段「**A843 · 落点**」——
                    //     它把抽屉摆到「收起态重建过」那一态再展开，断 `Cell_owned` 必须回到 **167.905**
                    //     （删掉 ④ ⇒ 实得 552.905 ⇒ 必红）。⛔ 别再按「三条在不在」下结论。
                    //   ⚠️ A811 的另一半（「视口外的件到底该不该建」= 那道闸）**不是待办** ——
                    //     A798 已裁定「只裁不建在画面上**等价**」：判据见下面那一大段。
                    //
                    // 🔴 **2026-10-16（A811 现核 · 只读，未跑；铁律 5·b）—— 这条账要按【两条】分开记：**
                    //   ① **「闸本身」那一半已经有裁定、不是待办**：同一道「整块在框外 ⇒ 不建」的闸，
                    //      2026-10-14（A798）也加进了 `MenuDraw.Text` / `TextBox` 的**第一句** ——
                    //      就是那两句 `if (!Visible(r, _st.RenderClip)) return null;`
                    //      （⚠️ 原来这里写 `Shell/MenuDraw.cs` / `:1673`，**行号已漂**；
                    //        2026-10-16 现读 = **`CardPresentation/Shell/MenuDraw.cs`（`Text`）/ `:1705`（`TextBox`）**，按语句认别按行号认），
                    //      而那段注释自己写着裁定的理由：
                    //      「**「只裁不建」在画面上等价**（整块在框外 ⇒ `ClipText` 今天也会把它夹成零面积、
                    //     画不出像素），收益是与 `Rect`/`Nine`/`Tiled`/`Hit` 同一条判据」⇒
                    //      A811 行里那条「改成只裁不删」的路**已被否掉**（判据 = `MenuDraw.Text` 的那段）。
                    //   ② **重建那一半 —— 机制已换（🔴 2026-10-16 A835 订正 · 铁律 5）**。
                    //      **原文（留痕，未跑）**：「仍然开着的是「重建时机」那一半（这才是上面那三条红的
                    //      **真因**，而且**可见**）。读实现得到的机制 = **抽屉的行程比整块面板还宽**：
                    //        · 收起时 `ApplyDrawerSlide` 把面板整块挪 **−385px**（`FltHiddenDx`），
                    //          而本页那整块面板只有 **335.31px** 宽、视口 `FltView` = x∈[0.25, 335.56]
                    //          （`FltL`/`FltW`）⇒ **面板＋视口整条滑到屏左外**；
                    //        · 那一刻看框的那两路**读的都是节点当下位置** —— 文字那一路 `ViewportClip.ClipPx`
                    //          （`Shell/ViewportClip.cs` 的 `PosInDesignSpace(transform)`）、
                    //          容器那一路 `_fltScroll.Intersects` → `MenuScroll.ClipNode`（A465 起也转节点、
                    //          `Shell/MenuScroll.cs`）⇒ **两条路都拿到「屏左外那一条」**；
                    //          而被比较的矩形是**页面绝对设计矩形**（`MenuDraw.Local` 按基准位算出来的，
                    //          `Shell/CollectionWindow.cs` 的 `Abs(...)` / `_fltScroll.Shift(...)`）⇒ 恒不相交。
                    //        ⇒ **收起态下发生的任何一次重建**（`Empty Collection Warning` 那一节的
                    //          `ClearCardFilters()` / `RefreshCardsAfterFilter` / `RebuildFilterRowsNow`
                    //          的任一入口）**把所有带闸的件
                    //          全判成「框外」**：三件 `Label`、`Cell_*`、`Name Filter`、底图…**一律不建**；
                    //          只有**不带闸**的裸 `Node`（面板 `Card Filters` 自己）照建
                    //          —— 这正是「（前提）卡牌页的 `Card Filters` 节点在」只是**假前提**的原因。」
                    //      ⚠️ **上面这一整段引文只对「A811 根治之前」成立**（根治落地后那两条路**不再**
                    //      拿到「屏左外那一条」）。**✅ 根治之后的状态**：框的中心改取
                    //      `ViewportClip.BaseRect`（`Hang` / `MenuDraw.ApplyPxRect` 写进去的那份设计矩形），
                    //      **不再跟实时 `localPosition` 走** ⇒ 框与被比的矩形**同一帧** ⇒ 收起期间那次重建
                    //      **不再把任何件判成框外**（静态推读、**未跑** → `资料/普查产出_1016/W11_A811根治.md` §③·C · §⑤）。
                    //      🔴 **今天还开着的那点差 = 落点**：`MenuDraw.Local` 用的是父件**当下**位置
                    //      ⇒ 收起期间建的那一版会**跟着面板滑回来**、到位那一刻偏 **+385px** ⇒ ④ 补的就是这一拍
                    //      （更彻底那条 = **A834**：让 `MenuDraw.Local` 的帧也取记录矩形 —— 另开一趟）。
                    //        ⚠️ **下面这条「括注不成立」的现读结论仍然成立**（两路确实都读**同一颗节点框**，
                    //          只是那颗框现在取的是记录矩形、不再跟着面板滑）：
                    //          `Block6_CollectionScene五条.md` §二·1 的括注写「容器 `Cell_*` / `Name Filter`
                    //           照建」—— **2026-10-16 按代码现读核实：那条括注不成立**：
                    //           `Cell_*` 那一路在 `if (!cosmo && !_fltScroll.Intersects(r)) continue;` **之后**
                    //           才 `Node(parent, "Cell_" + …)`；`Name Filter` 更直接 —— `BuildNameRow` 第二句
                    //           就是 `if (!_fltScroll.Intersects(r)) return;`。
                    //        · ⚠️ **这三行从 2026-10-16 起失效（铁律 5，留痕）** —— 原文是：「而**展开抽屉
                    //          这一下不重建**（`ToggleFiltersNow` / `ApplyDrawerSlide` 只挪位置 + 开关节点）
                    //          ⇒ 收起时漏掉的那一版**不会自己回来** —— 这就是**可见后果**（按上面那条链，
                    //          打开抽屉那一刻整列是空的，直到滚一下 / 点一下筛选触发重排）」。
                    //          **当天已按下面的「最小一条」修掉** —— 展开到位那一拍现在**会**补一次重建。
                    //      ⇒ 修法两条：
                    //        · **最小一条 —— ✅ 已做**（2026-10-16，落进 `Shell/CollectionWindow.cs` 的
                    //          `ApplyDrawerSlide` 尾「④」）：抽屉**滑到展开位**那一拍补一次
                    //          `RebuildFilterRows(p)`，把「收起期间建的那一版」**重排到位**（偏 +385px）。
                    //          落点**没选** `ToggleFiltersNow` —— 那一处在 Play 下跑在起滑之后的一瞬间、
                    //          面板还停在收起位。
                    //        · **根治 —— ✅ 2026-10-16 已落**（A811 根治：跨 `Shell/ViewportClip.cs` 的
                    //          `BaseRect` + `Shell/MenuDraw.ApplyPxRect` 那一处穿透）：框的中心 = **宿主写进
                    //          节点的设计矩形** ⇒ 框与被比的矩形**同一帧**，**不再跟实时 `localPosition` 走**。
                    //          逐处改动 / 判据 → `资料/普查产出_1016/W11_A811根治.md`。
                    //          ⏭ **同族仍开着的那一件 = A834**：`MenuDraw.Local` 的帧仍是父件**当下**位置
                    //          ⇒ 收起期间建的那批件在 0.3 秒滑入期间偏 0→+385px（到位被 ④ 拉回）。
                    //      🔴 **验收断言 —— ⚠️ 2026-10-16 订正（A835 · 铁律 5）**：原文（留痕）写的是
                    //      「**去掉**原来那句 `win.ClearCardFilters()` 重排，只「点一下展开」之后那三件字
                    //      **应当在** —— 删掉 ④ ⇒ 三条红（这条断言的电就在这儿）」。**根治落地后那句不成立**：
                    //      收起期间建的那一版现在**真的**含这三件 ⇒ 删掉 ④ 它们**照样在**、三条**照旧绿**
                    //      （那三条只查**在不在**、不查**落在哪**）⇒ **④ 今天没有断言盯着**。
                    //      ✅ **2026-10-16 当天已补（A843）**：本节末尾那段「**A843 · 落点**」就是新定的
                    //      验收口（量**落点**：`Cell_owned` 展开到位后必须回到 **167.905**；删掉 ④ ⇒
                    //      实得 **552.905** ⇒ 红）。⚠️ 那一整段**未跑**（本件没跑 Unity）—— 首跑若红，
                    //      先看它自己的前提那三条，⛔ 别直接改期望值。原文列的两条路里**选了前面那条**。
                    //      ✅ **2026-10-16 现核（A843 · 铁律 5 订正）**：`Shell/CollectionWindow.cs` 里那两处同源注
                    //      **已经改好了**（用的就是上面这一套「**A843 · 落点段 = 验收口**」的说法）；
                    //      原文（留痕）写的是「**仍未改**、那文件不在本件白名单、留给调度台」—— 那是**收口之前**的态。
                    win.tabButtons.Click(1);
                    Check(win.CurrentTab, WindowTabType.CollectionCards,
                          "（夹具态）先切到卡牌页 —— 下面三条要在**活着的那一页**上量");
                    var cFltHit = win.PageRoot(1) != null ? FindChild(win.PageRoot(1), "FiltersHit") : null;
                    var cFltBtn = cFltHit != null ? cFltHit.GetComponent<WindowButton>() : null;
                    CheckTrue(cFltBtn != null,
                              "（前提）卡牌页页头那颗 `Filter Toggle` 的命中区 `FiltersHit` 在（三条前提靠它开抽屉）");
                    // 🔴 **灭自证用的探针**（说明见下面那条断言）：**点之前**抓一张活着的卡池卡
                    //   （`CardsCells` 里存的是**卡的根节点**）当探针。
                    //   ⚠️ 此刻卡池滚在顶部（上一节那次清筛选把 `CardsScroll` 拉回 0）、且没有筛选条件
                    //   （`Empty Collection Warning` 那条刚验完「不显示」）⇒ 一定有建出来的格。
                    var probeCard = win.CardsCells.Count > 0 ? win.CardsCells[0] : null;
                    CheckTrue(probeCard != null,
                              "（前提）卡池里抓得到一张活卡当「灭自证」的探针 —— 抓不到那条就等于空转");
                    if (cFltBtn != null)
                    {
                        cFltBtn.Click();            // 🔴 **真点一次**（不是直调 `win.ToggleFilters()`）
                        CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                  "…点一下 ⇒ 卡牌页抽屉**展开到位**（⚠️ 抽屉页号 **0** = Cards；异画页那一份才是 1）");
                        // 🔴 **只「点一下展开」、⛔ 不再补重排**（2026-10-16 起 —— A811 最小修法
                        //   落进 `ApplyDrawerSlide` ④）：展开到位那一拍**自动**补一次重建。
                        //   ⚠️ **2026-10-16 订正（A835 · 铁律 5）**：原文这里写「这一步就是 A811 修法的
                        //   **验收点** ⇒ 那三个节点自己回来」，而「**改坏法**：把 ④ 那个 `if`（或它里面那次
                        //   `RebuildFilterRows(p)`）删掉 ⇒ 下面三条「（前提）…在」**当场全红**」——
                        //   **那两句从 A811 根治落地起不成立**（收起期间那一版现在**真的**含这三件，
                        //   见上面 ② 的订正）⇒ 删掉 ④ 它们**照样在**、三条**照旧绿**。
                        //   ⚠️ **2026-10-16 订正（A843 · 铁律 5）**：上面这句原文（留痕）写的是「④ 今天**没有断言
                        //   盯着** …… 这一条**要主对话裁**」—— **两半都已过期**：A843 当天就把验收口定在
                        //   本节末尾那段「**A843 · 落点**」= **验收口**（= 下面紧接的那三行，详见它自己）⇒ ⛔ 不再「待裁」。
                        //   ✅ **2026-10-16 当天已补（A843）**：本节末尾那段「**A843 · 落点**」= 新验收口
                        //   （量落点：`Cell_owned` 展开到位后必须回到 **167.905**；删掉 ④ ⇒ 实得 **552.905** ⇒ 红）。
                        //   ⚠️ 它**另起一段夹具**（自己收起 → 收起态重建 → 再展开），不复用本处这颗 `cFltBtn` 的态 ——
                        //   上面那条 `probeCard` 灭自证（卡池一张都没被重建）管的是**本处这一次点击**，两者不冲突。
                        //   · ⚠️ **本条不再单配「重建之后抽屉仍停在展开那一头」那条断言**：④ 与它收尾那次
                        //     `ApplyDrawerSlide(…, force: true)` 都在 `Click()` 里**同步**跑完 ⇒ 上面那条
                        //     （`DrawerSettled(0)`）读到的**就是重建之后的态**，再断一遍 = 同一条读两次（空转）。
                        //     （原来那句断言的**存在理由**是它夹在一次真重排后面 —— 重排没了，理由也没了。）
                        // 🔴 **灭自证**（铁律：只断「新写法对」不够）：上面三条光看结果，**分不出**
                        //   「修法挣来的」与「有人把这句删掉的 `win.ClearCardFilters()` 又补回来」——
                        //   所以再钉一条**只可能被那条补偿路弄红**的：`ClearFiltersNow` 会走
                        //   `RefreshCardsAfterFilter`（`CardsScroll.SetOffset(0)` + **整批**重建卡池
                        //   `RebuildCardsCells`，先销毁再重建）⇒ 拿一张**点之前**抓的活卡当探针，
                        //   点完之后它**必须还活着**。
                        CheckTrue(probeCard != null,
                                  "★ **灭自证**：点展开**只**走了抽屉那条路 —— 卡池那批 `CardView` 一张都没被"
                                  + "重建（`ClearCardFilters()` 那条补偿路会把它们**整批销毁重建** ⇒ 这条红，"
                                  + "上面三条的绿就不是修法挣来的了）");
                    }
                    var cDrawer = win.PageRoot(1) != null ? FindChild(win.PageRoot(1), "Card Filters") : null;
                    CheckTrue(cDrawer != null, "（前提）卡牌页的 `Card Filters` 节点在（对照组靠它）");
                    if (cDrawer != null)
                    {
                        var cNF = FindChild(cDrawer, "Name Filter");
                        var cInT = cNF != null ? FindChild(cNF, "Input Text") : null;
                        var cInLb = cInT != null ? cInT.GetComponent<Label>() : null;
                        if (cInLb != null)
                        {
                            CheckNear(Label.FontSizeToPx(cInLb.FontSizeMax), 30f, 0.6f,
                                      "★ **对照**：卡牌页搜索框仍是原版 **30**（把共用常量改成 35 ⇒ 这条红）");
                            CheckNear(Label.FontSizeToPx(cInLb.FontSizeMin), 18f, 0.6f,
                                      "★ …下界仍是原版 **18**");
                            Check(cInLb.WrappingMode, 3,
                                  "★ **对照**：卡牌页搜索框**也是** `折行=3`（原版两页同档 —— 这条钉「折行没被按页分」）");
                        }
                        else CheckTrue(false, "（前提）卡牌页搜索框的 `Input Text` 在");
                        var cOwT = FindChild(cDrawer, "Cell_owned");
                        var cOwL = cOwT != null ? FindChild(cOwT, "Label") : null;
                        var cOwLb = cOwL != null ? cOwL.GetComponent<Label>() : null;
                        if (cOwLb != null)
                        {
                            CheckNear(Label.FontSizeToPx(cOwLb.FontSizeMax), 32f, 0.6f,
                                      "★ **对照**：卡牌页 `Owned only` 仍是原版 **32**（改成 36 ⇒ 这条红）");
                            CheckNear(Label.FontSizeToPx(cOwLb.FontSizeMin), 18f, 0.6f,
                                      "★ …下界仍是原版 **18**");
                        }
                        else CheckTrue(false, "（前提）卡牌页 `Owned only` 那一格的标签在");
                        var cTt = FindChild(cDrawer, "Title Army");
                        var cTlb = cTt != null ? cTt.GetComponent<Label>() : null;
                        if (cTlb != null)
                            CheckNear(cTlb.FontPxNow, 32f, 0.6f,
                                      "★ **对照**：卡牌页小标题仍是原版 **32**（改成 36 ⇒ 这条红）");
                        else CheckTrue(false, "（前提）卡牌页小标题 `Title Army` 在");
                    }

                    // ============================================================ 🆕 2026-10-16（A843）
                    // **抽屉展开后的【落点】** —— ④（`Shell/CollectionWindow.cs` 的 `ApplyDrawerSlide` 尾）
                    // 今天**唯一**被人盯着的地方。
                    // 🔴 欠账原文：上面那三条「（前提）…在」**只查在不在**，而 A811 根治之后
                    //    「收起期间那次重建」照样把这三件建得出来 ⇒ **删掉 ④ 也全绿** ⇒ ④ 等于
                    //    没有断言盯着（判据 → `资料/普查产出_1016/W18_FltCell与文档漂移.md` §④·2）。
                    //
                    // 🔴 ④ 到底在补什么（现读实现）：`MenuDraw.Local(parent, r)` =
                    //    `RectCenter(r) − PosInDesignSpace(parent)` —— 减的是**父件当下**的位置 ⇒
                    //    面板停在【收起位】（左移 385px）时建的那一版，局部坐标里**多算了 385px**：
                    //    它出生那一刻看着是对的（跟着面板一起偏），**面板滑回原位之后整列偏 +385px**。
                    //    ④ = 「`Slide` 刚跨到 1 那一拍补一次 `RebuildFilterRows`」⇒ 把这一版**重排到位**。
                    //    ⚠️ 起手那次建**不欠**（`RowsBuiltOffBase = HasBasePos && Slide < 1`；起手
                    //    `HasBasePos == false`）⇒ 只有「滑出去之后、又在收起态重建过」这一条路欠。
                    //
                    // 🔴 夹具三步（全部走**公开口**，⛔ 不读任何私有状态）：
                    //    ① **基准**必须取自「④ 碰不到」的那一态 —— 抽屉**开着**（`Slide == 1`）时调
                    //       `ClearCardFilters()` ⇒ `RebuildFilterRowsNow` 里 `Slide < 1` 为假 ⇒
                    //       这一版**按基准位建**。⛔ **别拿「刚点开之后的那一版」当基准**：那一版
                    //       **正是 ④ 挣来的**（删掉 ④ 它自己就偏了）⇒ 拿它当基准 = 自证。
                    //    ② 收起（点页头那颗钮）⇒ 收起态**再** `ClearCardFilters()` 一次 ⇒ 这一版
                    //       就是**照收起位那帧建的**（= 欠的那一版）。
                    //    ③ 再展开 ⇒ ④ 必须把它**重排到位**。
                    //    🔴 ③ 走的是**帧路**（`StartDrawerSlideForTest` + `TickDrawers` →
                    //       `ApplyDrawerSlide`），**不是**点那颗钮 —— 见下面「灭自证」那条。
                    //
                    // 🔴 期望值一律写**原版字面量**（同 `:3455` 那条口径，⛔ 不读 `CollectionWindow.FltL/FltW`）：
                    //    · `Cell_owned` 那一行**横跨整个抽屉宽**（`FilterPanelModel.ToggleRowRects` 的
                    //      `row = 0…w`、`w = FltW`）⇒ 它的中心 x = 面板矩形 0.25…335.56 的中点 = **167.905**
                    //      （面板矩形那两条字面量 = 上面那条 `CheckAt(fltPanel, 0.25f, 335.56f, …)`）；
                    //    · `Title Army` 的渲染左沿 = **0.25**（同 `本文件` 那条）。
                    //    ⚠️ 前提：抽屉滚动量此刻是 **0**（下面所有落点值都是「未滚动」那一帧的读数）。
                    if (cDrawer != null && cFltBtn != null)
                    {
                        var cFscr = win.FilterScroll;
                        CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                  "（前提 · A843）进这一段时卡牌页抽屉是**展开到位**的（基准版要在这一态上建）");
                        CheckTrue(cFscr != null && Mathf.Abs(cFscr.Offset) < 0.01f,
                                  "（前提 · A843）抽屉滚动量 = **0**（下面落点值都是未滚动那一帧的读数；实得 "
                                  + (cFscr != null ? cFscr.Offset.ToString("F2") : "null") + "）");
                        // ① 基准版：**开着**重建一次（`Slide == 1` ⇒ ④ 碰不到这一版）
                        win.ClearCardFilters();
                        var cBase = FindChild(cDrawer, "Cell_owned");
                        CheckTrue(cBase != null, "（前提 · A843）落点探针 `Cell_owned` 在（那一行横跨整个抽屉宽）");
                        if (cBase != null)
                        {
                            CheckNear(MenuCheck.DesignPx(cBase).x, 167.905f, 1f,
                                      "★ **A843 · 基准落点**：`Cell_owned` 中心 x = **167.905**（原版那一行横跨"
                                      + "抽屉全宽 0.25…335.56 ⇒ 取中点）—— 这一版是【抽屉开着】时建的"
                                      + "（`Slide == 1` ⇒ `RowsBuiltOffBase` 为假）⇒ **④ 碰不到它**"
                                      + "（能当基准的原因就在这儿；⛔ 别拿「刚点开那一版」当基准）");
                            // ② 收起 ⇒ 收起态再重建一次（= 欠的那一版）
                            cFltBtn.Click();
                            CheckNear(win.DrawerSlide(0), 0f, 0.001f, "…收起：进度 **0**（整栏滑出去）");
                            var cRide = FindChild(cDrawer, "Cell_owned");
                            CheckTrue(cRide != null && cRide == cBase,
                                      "…而且**没重建**（还是同一颗节点）—— 它只是跟着面板一起滑走了");
                            CheckNear(cRide != null ? MenuCheck.DesignPx(cRide).x : -9999f, 167.905f - 385f, 1f,
                                      "…这一颗**跟着面板左移 385px**（167.905 − 385 = **−217.095**）——"
                                      + "「基准版按基准位建」+ 这一条 = 这杆子真被 `ApplyDrawerSlide` 挪过"
                                      + "（`HasBasePos == true` ⇒ 下面那一版才可能「欠」）");
                            win.ClearCardFilters();          // 收起态重建 ⇒ 照收起位那帧建
                            var cOff = FindChild(cDrawer, "Cell_owned");
                            CheckTrue(cOff != null && cOff != cBase,
                                      "★ **前提（欠版已建）**：收起态这一次重建**真的换了节点**"
                                      + "（`RebuildFilterRowsNow` 先销毁再重建）—— 不换 = 夹具空转、下面两条等于没验");
                            if (cOff != null)
                            {
                                float offCx = MenuCheck.DesignPx(cOff).x;
                                CheckTrue(Mathf.Abs(offCx - 167.905f) < 1f
                                          || Mathf.Abs(offCx - (167.905f - 385f)) < 1f,
                                          "（前提 · A843）这一版是**照某一帧建出来**的：实得 **" + offCx.ToString("F1") + "**——"
                                          + "**167.905** = 照**收起位**那帧建（今天这条实现：局部坐标里多算 385px）；"
                                          + "**−217.095** = 照**基准位**那帧建（`MenuDraw.Local` 改成取记录矩形"
                                          + "—— A834 那条彻底修法落地后就是这一支）；两个都不是 ⇒ 面板停在半路"
                                          + "（夹具没摆到收起位）");
                                // 🔴 **2026-10-18（A834 · 铁律 5 订正）**：这条**析取保留原样**（它是**夹具前提**，
                                //   ⛔ 不是 A834 的验收口 —— 写成析取正是为了「A834 落地那天不假红」，
                                //   判据 → `资料/普查产出_1016/W24_落点断言与量法收口.md:41`）。
                                //   ⚠️ **但今天恒落 `−217.095` 那一支**：A834 走的是**候选甲**（`RebuildFilterRowsNow`
                                //   建之前调 `DrawerHome(_flt)`），不是这一行注释里原来设想的那条「`MenuDraw.Local`
                                //   改成取记录矩形」。A834 自己的验收口在**下一段**（那三条 + 判别式）。
                                // ③ 帧路展开：`StartDrawerSlide` + `TickDrawers → ApplyDrawerSlide`
                                //    （与 Play 跑起来那条**同源**；⛔ 这里**故意不点那颗钮**）
                                win.StartDrawerSlideForTest(0, true, playLike: true);
                                win.TickDrawers(0.3f);
                                CheckNear(win.DrawerSlide(0), 1f, 0.001f,
                                          "…`TickDrawers(0.3)`（原版 `animationTime`）⇒ 进度 **1**（到位）");
                                var cAfter = FindChild(cDrawer, "Cell_owned");
                                CheckNear(cAfter != null ? MenuCheck.DesignPx(cAfter).x : -9999f, 167.905f, 1f,
                                          "★★ **A843 · 落点**：展开到位后 `Cell_owned` 必须**重排回 167.905**。"
                                          + "🔴 **改坏法：删掉 `RebuildFilterRowsNow` 里那句 `DrawerHome(_flt)`"
                                          + "（A834，2026-10-18 落地的修法）⇒ 这一条当场红**"
                                          + "（欠的那一版跟着面板滑回来 ⇒ 实得 **552.905** = 167.905 + 385）。"
                                          + "⚠️ **2026-10-18（铁律 5 订正）**：原文写的改坏法是「删掉 `ApplyDrawerSlide` 尾那个 ④」"
                                          + "—— 那一笔**从 A834 落地当天起不再成立**：`DrawerHome` 已经让这一版建在基准帧上，"
                                          + "④ 今天只是把同一件事**再做一遍**（冗余），删它这条照样绿。");
                                CheckNear(TitleLeftPx(cDrawer, "Army"), 0.25f, 1f,
                                          "★★ 同一拍、**另一条建法**的件也一样：小标题 `Army` 的**渲染左沿**回到 "
                                          + "**0.25**（面板内 0；期望值 = `本文件` 那条）"
                                          + "—— 改坏法同上（删 `DrawerHome(_flt)` ⇒ 落在 **385.25**）");
                                // 🔴 **灭自证（挡「终点和检测器一起改回去」）**：只断「落点对」不够 ——
                                //   删掉 ④ 之后，**在夹具里补一条补偿路**（例如「点开之后再
                                //   `ClearCardFilters()` 重排一次」= 原来那个 ③ 夹具，或者把补偿塞进
                                //   `ToggleFiltersNow`）照样能把落点摆对 ⇒ 那一条就变成「验夹具」了。
                                //   上面 ③ **故意走帧路**（`StartDrawerSlideForTest` + `TickDrawers`）：
                                //   那一路只经过 `ApplyDrawerSlide`，**`Toggle*` / 夹具里的补偿一律碰不到**
                                //   ⇒ **要让这条绿，修法必须落在帧路里**（`ApplyDrawerSlide` —— ④ 就在那儿；
                                //   同族的 `TickDrawers` 也算帧路）；⛔ 往 `ToggleFiltersNow` 里补一次重建、
                                //   或在夹具里补一句 `ClearCardFilters()` 都**不管用**（它们只能骗过下面 ④ 那条）。
                                //   ⚠️ 如实说清它**挡不住**什么：`RebuildFilterRowsNow` 自己（或者
                                //   `MenuDraw.Local`）如果改成「永远按基准帧算」，这条**照样绿**——
                                //   那**是对的**。🔴 **2026-10-18（A834 · 铁律 5）**：那个状态**今天已经到了**
                                //   （`RebuildFilterRowsNow` 建之前调 `DrawerHome(_flt)`）⇒ ④ 已退化成空转、
                                //   本条仍成立（「落点对」这一半照样要它绿）。
                                //
                                // ④ 另一条路（批量里 `Toggle*` 直接到位）也过一遍 —— 与 ③ 是**同一个** ④，
                                //   但入口不同（`ToggleFiltersNow → StartDrawerSlide(playLike: false)`）。
                                cFltBtn.Click();                  // 收起
                                CheckNear(win.DrawerSlide(0), 0f, 0.001f, "…再收起（批量那条入口）：进度 **0**");
                                win.ClearCardFilters();           // 收起态重建 ⇒ 又一版欠的
                                var cOff2 = FindChild(cDrawer, "Cell_owned");
                                CheckTrue(cOff2 != null && cOff2 != cAfter,
                                          "…收起态那一版**又换了一颗**（欠版 2 就位）");
                                cFltBtn.Click();                  // 展开（批处理里 `Toggle*` 直接到位）
                                CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                          "…点一下 ⇒ 展开到位（`Toggle*` 那一支）");
                                var cAfter2 = FindChild(cDrawer, "Cell_owned");
                                CheckNear(cAfter2 != null ? MenuCheck.DesignPx(cAfter2).x : -9999f, 167.905f, 1f,
                                          "★ **A843 · 落点（`Toggle*` 那条入口）**：同上，必须回到 **167.905** ——"
                                          + "删掉 ④ ⇒ 红（实得 **552.905**）；这一条与上面那条**各管一个入口**"
                                          + "（⚠️ 它**不是**灭自证那一条：这条入口在 `Toggle*` 里，补偿路碰得到它）");
                            }
                        }
                    }

                    // ============================================================ 🆕 2026-10-18（A834）
                    // **过渡帧的落点** —— 上面那些（含 A843 那 16 条）**全部在滑动结束之后量**，
                    // 而 A834 治的正是**过渡帧**：面板停在收起位时建的那一版，局部坐标里多烘了 385px
                    // ⇒ 0.3 秒滑入期间整列**与别的件不在同一帧上**（偏 0→+385px），只有 ④ 在到位那一拍扳回来。
                    //
                    // 🔴 **修法（= `Shell/CollectionWindow.cs` 的 `DrawerHome`，调度台裁定「甲」）**：
                    //    `RebuildFilterRowsNow` **建之前**先把面板摆回基准位 ⇒ 每一版**本来就是基准帧的坐标**
                    //    ⇒ 整列跟着面板**刚性**平移、途中每一帧都对。
                    //    ⚠️ 它**同时**治好两个读口（`MenuDraw.Local` **与** `Label.ParentXInDesignSpace`）
                    //    —— 这两个口读的是**同一份「父件当下位置」**，只修一个 = 「格子跟抽屉走、字不跟」。
                    //
                    // 🔴 **期望值一律写【原版字面量】**（与上面 A843 那一段同一条口径）：
                    //    · `167.905` = `Cell_owned` 那一行的中心 x（横跨抽屉全宽 0.25…335.56 ⇒ 取中点）；
                    //    · `0.25`    = `Title Army` 的渲染左沿（面板内 0 + 面板原点 0.25）；
                    //    · `385`     = 抽屉行程（原版 `hiddenPosition.x −550` 与 `originalAnchorPosition.x`
                    //                  那一对，`FltHiddenDx`）—— 三个都是**原版读数**，⛔ 不读 `FltL/FltW/FltHiddenDx`。
                    //    · t=0.5 那一帧 ⇒ 两位移各 **−192.5px** ⇒ 读数 = `167.905−192.5 = **−24.595**` ·
                    //      `0.25−192.5 = **−192.25**`。
                    if (cDrawer != null && cFltBtn != null)
                    {
                        // 归一状态：把抽屉推到展开位（帧路，0.3 秒走完；与下面 ③ 同一条口）
                        win.StartDrawerSlideForTest(0, true, playLike: true);
                        win.TickDrawers(0.3f);
                        CheckTrue(win.FiltersOpen && win.DrawerSettled(0),
                                  "（前提 · A834）进这一段时卡牌页抽屉已展开到位");
                        // ① **基准**那一版（抽屉开着时建）—— 两条读数各取一个零点
                        win.ClearCardFilters();
                        var a8Base = FindChild(cDrawer, "Cell_owned");
                        float a8Cell0 = a8Base != null ? MenuCheck.DesignPx(a8Base).x : -9999f;
                        float a8Army0 = TitleLeftPx(cDrawer, "Army");
                        CheckNear(a8Cell0, 167.905f, 1f,
                                  "（前提 · A834）基准版 `Cell_owned` 中心 x = **167.905**（开着建的那一版）");
                        CheckNear(a8Army0, 0.25f, 1f,
                                  "（前提 · A834）基准版 `Title Army` 渲染左沿 = **0.25**");
                        // ② 收起 ⇒ **收起态重建**（= 那版要跟着面板滑回来的件）
                        cFltBtn.Click();
                        CheckNear(win.DrawerSlide(0), 0f, 0.001f, "…收起：进度 **0**");
                        win.ClearCardFilters();
                        var a8Ride = FindChild(cDrawer, "Cell_owned");
                        CheckTrue(a8Ride != null && a8Ride != a8Base,
                                  "★ **前提（A834 的欠版已就位）**：收起态这一次重建**真的换了节点**"
                                  + "（`RebuildFilterRowsNow` 先销毁再重建）—— 不换 = 夹具空转、下面三条等于没验");
                        CheckNear(a8Ride != null ? MenuCheck.DesignPx(a8Ride).x : -9999f, 167.905f - 385f, 1f,
                                  "★★ **A834 · 建的是【基准帧】**：收起态这一版建完（面板被摆回收起位）⇒ 中心 x = "
                                  + "**−217.095**（= 167.905 − 385）。"
                                  + "🔴 **改坏法：删掉 `RebuildFilterRowsNow` 里那句 `DrawerHome(_flt)`** ⇒ 局部坐标多烘 "
                                  + "385px ⇒ 实得 **167.905**（= A834 落地前那一支）");
                        // ③ 帧路推到**半路**（0.15s / 原版 `animationTime` 0.3 = 进度 0.5）
                        win.StartDrawerSlideForTest(0, true, playLike: true);
                        win.TickDrawers(0.15f);
                        CheckNear(win.DrawerSlide(0), 0.5f, 0.001f,
                                  "（前提 · A834）`TickDrawers(0.15)` ⇒ 进度 **0.5**（`animationTime` 0.3 的一半）");
                        var a8Mid = FindChild(cDrawer, "Cell_owned");
                        float a8Cell1 = a8Mid != null ? MenuCheck.DesignPx(a8Mid).x : -9999f;
                        float a8Army1 = TitleLeftPx(cDrawer, "Army");
                        CheckTrue(a8Mid != null && a8Mid == a8Ride,
                                  "★ **A834 · 过渡帧里一次都不重建**：滑到一半时还是收起态那一版"
                                  + "（`RebuildFilterRowsNow` 只在**建的那一刻**摆位、之后整列跟着面板刚性平移）"
                                  + "—— 途中任何一次重建都会让下面两条变成「验夹具」");
                        CheckNear(a8Cell1, -24.595f, 1f,
                                  "★★ **A834 · 过渡帧落点**：0.3 秒滑入到一半（t=0.5）时 `Cell_owned` 中心 x = "
                                  + "**−24.595**（= 167.905 − 385×0.5，整列跟着面板**刚性**平移）。"
                                  + "🔴 **改坏法：删掉 `DrawerHome(_flt)`** ⇒ 收起态那一版局部坐标多烘 385px ⇒ "
                                  + "实得 **360.405** = 167.905 + 385×0.5");
                        CheckNear(a8Army1, -192.25f, 1f,
                                  "★★ **同一个病的【另一个读口】**：同一帧里 `Title Army` 的渲染左沿 = **−192.25**"
                                  + "（= 0.25 − 385×0.5）。这一颗走的是 `Label.AlignLeftOn` → "
                                  + "`Label.ParentXInDesignSpace()`（`Battle/Label.cs`），与格子的 `MenuDraw.Local` "
                                  + "**读同一份「父件当下位置」** —— 只修一个 ⇒ 字与格子**撕裂**（差 385px）");
                        // 🔴 **判别式（挡「只修一半」/「两边一起改回去」）**：
                        //   上面两条**各按字面量**钉了一个读数；这一条**不读任何常量**，只比**两个实测量各挪了多少**
                        //   —— 两条读口同帧 ⇒ 位移**必须相等**（都 −192.5）。
                        //   它挡的是：把 `MenuDraw.Local` 那条路改成「按记录矩形算」、却漏了 `Label` 那条
                        //   （那时两个差 = −192.5 与 **+192.5** ⇒ 红）；也挡「把期望值改成实测量」这种改法
                        //   （差值那一条不受期望值影响）。
                        CheckNear(a8Cell1 - a8Cell0, a8Army1 - a8Army0, 0.5f,
                                  "★★ **判别式（A834）**：同一帧里**两条读口各挪了多少必须相等**"
                                  + $"（实得 格子 {(a8Cell1 - a8Cell0):F2}px · 字 {(a8Army1 - a8Army0):F2}px）");
                        // 收尾：把抽屉推回**展开**（下面那段收尾要靠「点一下」把它关掉）
                        win.StartDrawerSlideForTest(0, true, playLike: true);
                        win.TickDrawers(0.3f);
                        CheckNear(win.DrawerSlide(0), 1f, 0.001f,
                                  "…收尾：推回展开（0.3 秒走完）—— 紧接着段收尾那句「点一下」要靠它关得掉");
                    }
                    // 🔴 **2026-10-14（#57–#59 · γ · 夹具侧收尾）**：本节自己开自己关 —— 卡牌页抽屉收回去、
                    //   页签切回**异画页**（紧接着那一条还要点异画页那颗钮、把它的抽屉也关掉）。
                    //   ⛔ 两句都不能省：抽屉留着张开 ⇒ 给后面的段留状态；页签留在 Cards ⇒ 异画页那一段收不了尾。
                    if (cFltBtn != null) cFltBtn.Click();
                    win.tabButtons.Click(3);
                    Check(win.CurrentTab, WindowTabType.CollectionStyles, "（夹具态复原）页签切回异画页");
                }
                styleFltBtn.Click();        // 关回去 —— 本节自己开自己关，不给后面的段留状态
                CheckTrue(!win.StyleFiltersOpen && styleDrawer != null && !styleDrawer.gameObject.activeSelf,
                          "再点一下 ⇒ 收回**收起**态（本节收尾 = 与进本节时同一个状态）");
            }
            Check(win.StyleVisibleCount, 7, "抽屉开合都不影响卡数（起手没有筛选条件）");
            // 换风格：**原版只有 1 档** ⇒ 两颗箭钮**点了也不动**。
            //   🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`/`A1830`）**：原来这里断的是「点右箭 ⇒ 下标 1、
            //   切到 `v2`、只剩 1 张、再点回绕」—— 那一套建立在**我们自己按图片文件名拆出来的第二档**上，
            //   而那第二档**原版没有**（`AlternateArtSyle` 只有 `Default`/`HammerAndBolter`；
            //   `GetAlternateArtStyleOnly` = `FindAll(style != 0)` ⇒ 轮播只剩 `HammerAndBolter`；
            //   本地 7 张卡的 `style` 现读全是 **5**。判据 → `CollectionWindow.StyleLogoName` 头注）。
            //   ⇒ 改成断「**只有 1 档、箭钮是死键**」—— 那才是原版的行为。
            var rhit = FindChild(spage, "ArrowHit Right");
            var rbtn = rhit != null ? rhit.GetComponent<WindowButton>() : null;
            CheckTrue(rbtn != null, "右箭钮有点击区");
            var lhit = spage != null ? FindChild(spage, "ArrowHit Left") : null;
            var lbtn = lhit != null ? lhit.GetComponent<WindowButton>() : null;
            CheckTrue(lbtn != null, "左箭钮有点击区");
            Check(CollectionWindow.AltStyles.Length, 1,
                  "风格**只有 1 档**（`AltStyles` 现读长度 —— 原版 `FindAll(style != 0)` 之后只剩 `HammerAndBolter`）");
            if (rbtn != null && lbtn != null)
            {
                rbtn.Click();
                Check(win.StyleIndex, 0, "点右箭 ⇒ 下标**仍是 0**（只有 1 档；⛔ 既不是回绕也不是报错）");
                Check(win.StyleVisibleCount, 7, "点数不受影响（还是那 7 张）");
                // 🔴 **2026-10-17（桶 C）**：夹具**每次点击后重取按钮** —— 不假设点击不重建页面
                //   （原版 `ConfigureStyle` 只刷新、不重建，但夹具不能把这条当前提；否则实现一旦重建
                //   第二下就点在已销毁对象上，报出来的是 `WindowButton.PlayClickSound` 的 NRE）。
                lhit = spage != null ? FindChild(spage, "ArrowHit Left") : null;
                lbtn = lhit != null ? lhit.GetComponent<WindowButton>() : null;
                CheckTrue(lbtn != null, "点右箭之后左箭钮仍在（⛔ 不是一个已销毁的引用）");
                if (lbtn != null) lbtn.Click();
                Check(win.StyleIndex, 0, "点左箭 ⇒ 下标**仍是 0**");
            }
            // 筛选**真的筛得动**（复用同一套 `DeckEditorState`：筛 `legendary` 只剩传奇那几张）
            {
                int altBefore = win.StyleVisibleCount;
                win.ApplyStyleFilter("$rar:legendary");
                CheckTrue(win.StyleVisibleCount < altBefore,
                          $"筛 `Legendary` ⇒ 异画从 {altBefore} 张降到 **{win.StyleVisibleCount}** 张（真筛得动）");
                win.ClearStyleFilters();
                Check(win.StyleVisibleCount, altBefore, $"`Clear filters` ⇒ 回到 {altBefore} 张");
            }
            Shoot("07_收藏_Styles.png");
            win.tabButtons.Click(0);
            Debug.Log(P + "   " + win.Dump());
            SaveScene();

            // ---------------- 页头那颗 `Filter Toggle`：四页各一颗，按本页抽屉的逻辑态换图（A93①）----------------
            // 判据（本轮**现读**原版，两处独立）：
            //   ① `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12`
            //      —— 四页页头**各有一颗** `Filter Toggle`（`Image,EverguildToggle,EverguildButtonMaterialModifier`
            //      · `40k_menu_bt` · `isOn=0` · `onSprite=4570862220269996290` · `offSprite=4472012397149938974`），
            //      含 **Deck 页**（`Select Deck Tab>Header>Filter Toggle` = 367.2,88.4 **50×50**，
            //      与另外三页同值）—— 我们原来给 Deck 页单写的那条页头**连这颗 quad 都没建**。
            //   ② 两个 pid 解名（`d:/4/_tmp_view/sprite_pids_ALL.json`，由 UnityPy 扫真包 `o.path_id` 建的）：
            //      `4570862220269996290 → 40k_menu_bt_pressed` · `4472012397149938974 → 40k_menu_bt`。
            // ⚠️ 断言量的是**渲出来的图名**（`CheckArt` 读 `ImageQuad.Texture.name`），
            //    ⛔ 不是「有没有调换图那个方法」——后者是同义反复。
            // 🔴 改坏会红的样子：把 `CollectionWindow.RefreshFilterToggle` 里 `p.Open ? …pressed : …` 那一句
            //    写死成 `40k_menu_bt`（或三条开合路径里那句 `RefreshFilterToggles()` 删掉）⇒ 每页的
            //    「展开 ⇒ 按下图」那条红；把 Deck 页退回原来那条自写页头 ⇒ 第 0 页两条**都**红（节点不在）。
            Section("页头 `Filter Toggle`：**四页各一颗**，展开 ⇒ `40k_menu_bt_pressed` / 收起 ⇒ `40k_menu_bt`（A93①）");
            {
                var tog = new System.Action[]
                {
                    win.ToggleDeckFilters, win.ToggleFilters, win.ToggleCosmoFilters, win.ToggleStyleFilters,
                };
                var nowOpen = new System.Func<bool>[]
                {
                    () => win.DeckFiltersOpen, () => win.FiltersOpen,
                    () => win.CosmoFiltersOpen, () => win.StyleFiltersOpen,
                };
                string[] pgTag = { "Deck（`Select Deck Tab`）", "Cards", "Cosmetics", "Styles" };

                // 先把四页各自的开合态记下来（本段结束时**原样还回去**，别把状态漏给后面的段）
                var wasOpen = new bool[4];
                for (int p = 0; p < 4; p++) wasOpen[p] = nowOpen[p]();

                for (int p = 0; p < 4; p++)
                {
                    // 🔴 **必须先把这一页切过去**：`CheckArt` 走的是 `GetComponentInChildren<ImageQuad>()`
                    //   （**不含未激活**），而四个页节点只有当前那一页是 `activeSelf` 的
                    //   ⇒ 不切页的话另外三页会「取不到 quad」而**假红**（报的是「实测 null」）。
                    //   这也正是原版的语义：每页页头那颗粒只在自己那一页上看得见。
                    win.tabButtons.Click(p);
                    CheckTrue(win.PageRoot(p) != null && win.PageRoot(p).gameObject.activeSelf,
                              $"（前提）第 {p} 页（{pgTag[p]}）已经切过去了");
                    // 归一：先确保这一页是【收起】的（⚠️ **2026-10-05 更正（铁律 5）**：原文写
                    // 「Deck/Cards/Cosmetics 起手本来就关；Styles 出厂展开」—— 后半句是把 `act=T`
                    // 读成了结论；**四页起手全是收着的**，判据见 Styles 段那一段更正）
                    // 这一句留着**不是**为 Styles 那半句：它是本段「进任何一页都从收起态开量」的前置，
                    // 对将来任何一页改成起手展开照样成立。
                    if (nowOpen[p]()) tog[p]();
                    CheckTrue(!nowOpen[p](), $"（前提）第 {p} 页（{pgTag[p]}）的抽屉此刻是**收起**的");
                    CheckArt(FindChild(win.PageRoot(p), "Filters Button"), "40k_menu_bt",
                             $"第 {p} 页（{pgTag[p]}）页头那颗 `40k_menu_bt` **建出来了**，收起态 = 常态图"
                             + "（原版 `EverguildToggle.offSprite`；Deck 页原来**连这颗都没建**，会红在这里）");
                    tog[p]();                                     // 开
                    CheckTrue(nowOpen[p](), $"（前提）第 {p} 页点一下 ⇒ 抽屉开着");
                    CheckArt(FindChild(win.PageRoot(p), "Filters Button"), "40k_menu_bt_pressed",
                             $"第 {p} 页（{pgTag[p]}）抽屉**开着** ⇒ 换成 `40k_menu_bt_pressed`"
                             + "（原版 `onSprite`；原来开着也恒画 `40k_menu_bt`）");
                    tog[p]();                                     // 关
                    CheckArt(FindChild(win.PageRoot(p), "Filters Button"), "40k_menu_bt",
                             $"第 {p} 页再点一下 ⇒ 换回 `40k_menu_bt`（判据**可逆**，不是单向的）");
                }
                win.tabButtons.Click(0);                          // 页签还原（本节进来时就在 Deck 页）
                // 还原：谁进来时是开的，就把它开回去
                for (int p = 0; p < 4; p++)
                {
                    if (wasOpen[p] != nowOpen[p]()) tog[p]();
                    CheckTrue(wasOpen[p] == nowOpen[p](),
                              $"（收尾）第 {p} 页（{pgTag[p]}）的开合态还原成进来时的样子（{wasOpen[p]}）");
                }
            }

            // ============================================================ 🆕 2026-10-19（第五十七会话 · `N52` 收尾）
            //   **四页页头的容器树 + 兄弟序**。判据 = 本轮 `menu_dump.py bundle_menus_assets_all "<页>" --depth 3` **现读**：
            //     · `Select Deck Tab ▸ Header`             孩子序 = Filter Toggle → **Control Buttons** → Separator Line → Filters
            //     · `CardsTab ▸ Header Filters`            孩子序 = Filter Toggle → Separator Line → Filters（→ `WIldcard Display`，另一笔账）
            //     · `Cardback Tab ▸ Header`                孩子序 = Filter Toggle → Separator Line → **label** → Filters
            //     · `Alternate Art Tab ▸ Header Filters`   孩子序 = Filter Toggle → Separator Line → Filters
            //   ⇒ N52① = `Filters`（原版 `GridLayoutGroup` 容器）要在；N52② = `Filter Toggle` 容器要在；
            //     N52③ = 兄弟序按上表。（①② 是 2026-10-17 落的，本轮只补断言；③ 里卡背页那颗 `label` 的父链 2026-10-19 收口。）
            //   🔴 四条都用 **`Transform.Find`（只找直接子件）** 取 —— 换回递归的 `FindChild` 就分不出
            //     「挂在 `Header` 下」与「挂在页节点上」，而后者正是这两条尾巴原来的形态（弱断言）。
            //   ⚠️ 放在本节（A93 四页循环**之后**）：四页此时都已由测试自己按正常顺序建好 ——
            //     本段**不**提前触发 `PageRoot(1..3)` 的懒建，免得改掉后续夹具的建页次序。
            Section("N52：收藏窗四页页头容器树（Header / Filter Toggle / Filters）与兄弟序");
            {
                var hdrNames = new[] { "Header", "Header Filters", "Header", "Header Filters" };
                for (int p = 0; p < 4; p++)
                {
                    var pr = win.PageRoot(p);
                    var hdr = pr != null ? pr.Find(hdrNames[p]) : null;
                    CheckTrue(hdr != null,
                              $"N52：第 {p} 页页头容器 **`{hdrNames[p]}`** 是页节点的**直接孩子**"
                            + "（原版四页逐颗现读：Deck/Cardback = `Header` · Cards/Alternate Art = `Header Filters`）");
                    if (hdr == null) continue;
                    var ft = hdr.Find("Filter Toggle");
                    CheckTrue(ft != null,
                              $"N52②：`{hdrNames[p]} ▸ Filter Toggle` 是**直接孩子**（原版 50×50 容器；"
                            + "🧨 退回「直接挂页节点」那一版 ⇒ 本格红）");
                    var sep = hdr.Find("Separator Line");
                    CheckTrue(sep != null, $"N52③：`{hdrNames[p]} ▸ Separator Line` 是**直接孩子**");
                    var grp = hdr.Find("Filters");
                    CheckTrue(grp != null,
                              $"N52①：`{hdrNames[p]} ▸ Filters`（原版 `GridLayoutGroup` 容器）是**直接孩子**"
                            + "｜🧨 退回「不建这一层」⇒ 本格红");
                    if (grp != null)
                        CheckTrue(grp.Find("Clear filters") != null,
                                  $"N52①：`{hdrNames[p]} ▸ Filters ▸ Clear filters`（原版 `Clear Filter Button`）"
                                + "挂在容器**之下**（原版 `Filters` 的唯一孩子）");
                    if (ft != null && sep != null && grp != null)
                        CheckTrue(ft.GetSiblingIndex() < sep.GetSiblingIndex()
                                  && sep.GetSiblingIndex() < grp.GetSiblingIndex(),
                                  $"N52③：`{hdrNames[p]}` 的兄弟序 = "
                                + $"Filter Toggle({ft.GetSiblingIndex()}) → Separator Line({sep.GetSiblingIndex()})"
                                + $" → Filters({grp.GetSiblingIndex()})（原版现读；⛔ 不许倒排）");
                }

                // Deck 页多一层 `Control Buttons` —— 原版**夹在 `Filter Toggle` 与 `Separator Line` 之间**。
                var dpr = win.PageRoot(0);
                var dh = dpr != null ? dpr.Find("Header") : null;
                var dft = dh != null ? dh.Find("Filter Toggle") : null;
                var dctrl = dh != null ? dh.Find("Control Buttons") : null;
                var dsep = dh != null ? dh.Find("Separator Line") : null;
                var dgrp = dh != null ? dh.Find("Filters") : null;
                CheckTrue(dft != null && dctrl != null && dsep != null && dgrp != null,
                          "N52③（前提·不静默）：Deck 页 `Header` 的四个孩子都取得到"
                        + "（Filter Toggle / Control Buttons / Separator Line / Filters）");
                if (dft != null && dctrl != null && dsep != null && dgrp != null)
                    CheckTrue(dft.GetSiblingIndex() == 0 && dctrl.GetSiblingIndex() == 1
                              && dsep.GetSiblingIndex() == 2 && dgrp.GetSiblingIndex() == 3,
                              "N52③：Deck 页 `Header` 的兄弟序 = **Filter Toggle(0) → Control Buttons(1) → "
                            + "Separator Line(2) → Filters(3)**（原版 `m_Children` 现读；实测 "
                            + $"{dft.GetSiblingIndex()}/{dctrl.GetSiblingIndex()}/{dsep.GetSiblingIndex()}/{dgrp.GetSiblingIndex()}）"
                            + "｜🧨 删掉 `BuildDeckHeader` 里 `ctrl.SetSiblingIndex(1)` ⇒ Control Buttons 停在第 3 位 ⇒ 本格红");

                // 卡背页那颗标题：原版在 `Cardback Tab ▸ Header ▸ label`（2026-10-19 同批收口父链与兄弟序）
                var cpr = win.PageRoot(2);
                var ch = cpr != null ? cpr.Find("Header") : null;
                var cLabel = ch != null ? ch.Find("Header Label") : null;
                CheckTrue(cLabel != null,
                          "N52③（同族尾巴）：卡背页标题 `Header Label` 在 **`Header` 之下**"
                        + "（原版 `Cardback Tab ▸ Header ▸ label`）｜🧨 挂回页节点 ⇒ 本格红");
                if (cLabel != null)
                    CheckTrue(cLabel.GetSiblingIndex() == 2,
                              "N52③（同族尾巴）：卡背页 `Header` 兄弟序 = Filter Toggle → Separator Line → "
                            + $"**label**({cLabel.GetSiblingIndex()}) → Filters（原版现读）");
            }

            // ---- 卡片详情窗 · 「创建副本」/「升级」两块面板 —— 🔴 2026-10-12 起【恢复】----
            // 🔴 用户 2026-10-12 裁定（逐字）：「「创建副本 / 升级」两块面板：维持你 9-27 的停用，还是恢复？**请你恢复**。
            //    我说的是不需要升级卡牌系统，**默认最高级就是不能点击升级卡牌按钮**。」
            //   ⇒ ① 两块面板的 UI **照原版恢复**；② **「升级」那颗钮 = 显示但不可交互**（默认最高级 ⇒ 不是删面板，是钮点不动）；
            //     ③ 「创建副本」那颗钮的**行为未定** ⇒ 本批**只出声**（`OnCraftClicked`），⛔ 不静默；
            //        **判据与两条待裁候选 → 账 `A1699`**。
            //   ⚠️ **旧口径留痕（铁律 5）**：本节原来断的是「两块面板**都不建**」（2026-09-27 用户拍板
            //     「直接全部卡都是最高级别的卡框…也不需要合成卡牌了」）—— **那一条已被 2026-10-12 的裁定取代**。
            //   🔴 **2026-10-18 再改判（就地订正上面 ②③ 两条）**：用户当天先拍板「照参考图布置两块面板」+
            //     「**通过升级按钮进行不同等级的卡框切换**」（判据 → `资料/普查产出_1018/W5_升级与副本_落地.md`），
            //     **同日第二轮又改**（判据 → `资料/普查产出_1018/W8_副本升级面板_按用户口径.md`）：
            //     「创建副本」= **点击但不起作用**（钮恒可点、点了什么都不变）；
            //     「升级 / 降级」= **两颗并列钮**，置灰只看等级上下限。
            //   🔴 **2026-10-24 再改判（铁律 5）**：「创建副本」= **真发请求 + 同一拍播特效**
            //     （判据 → `资料/任务线/特效线/任务总表.md`「锻造按钮的完整链路」· `A31_特效接线.md` §2·1）——
            //     `OnCraftClicked` 不再是空实现，钮的可交互度回到原版 `CheckButtons`（**有通配符才可点**）。
            //     ⇒ 本节只断「面板 / 两颗钮建没建（外观）」；**行为**由上面那一节逐条钉
            //     （请求 = owned+1 / 通配符−1；特效 = 按钮 + 前台卡两件；无通配符 ⇒ 一件都不播）。
            Section("卡片详情窗 · 「创建副本」/「升级」两块面板已恢复（行为：2026-10-24 起 = 请求 + 特效）");
            // 🔴 **2026-10-18（`N116`）**：本段会 5 次 `win.OpenCardDetail` —— 顺手把「开一扇留一具」那笔债
            //   断掉：前后各数一次**含未激活**的 `CardDetailPopup`（默认 `FindObjectsByType` 是
            //   `FindObjectsInactive.Exclude`，数不到关着的那扇 ⇒ 必须显式 `Include`）。
            //   修法 = `Shell/CollectionWindow.OpenCardDetail` 新建前 `Close()` + `DestroySafe` 上一扇。
            int cdWinsBefore = Object.FindObjectsByType<CardDetailPopup>(
                                   FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            // 🔴 **2026-10-19（批次 D · 红 #8）**：进本段时**可能已经有一扇**本文件开的详情窗
            //   （上一节点 `CardHit_0` 开的那扇 `Uriel Ventris` 还留着）⇒ 第一次 `OpenCardDetail`
            //   会先把它销毁再新建，**净增 0**；旧的 `cdWinsBefore + 1` 只在「起手为空」时成立。
            bool hadChainWin = CollectionWindow.LastCardDetail != null;
            int cdWinsAfterFirst = -1;                 // 第一次真正开成之后数一次（见下面第二条断言）
            foreach (var want in new[] { "common", "rare", "epic", "legendary", "special" })
            {
                CardDef cd = null;
                var pool = CollectionWindow.CardsState.Pool;
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i].Rarity == want) { cd = pool[i]; break; }
                if (cd == null) { Check(true, false, $"卡池里找不到稀有度 `{want}` 的卡"); continue; }
                var dw = win.OpenCardDetail(cd);
                if (dw == null) { Check(true, false, $"打不开卡片详情窗（{cd.Name}）"); continue; }
                // 第一次开成之后立刻数一次 → 作为「后面 4 次不再涨」的基线（见本节末尾那条断言）
                if (cdWinsAfterFirst < 0)
                    cdWinsAfterFirst = Object.FindObjectsByType<CardDetailPopup>(
                                           FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                // 「创建副本」那一块：**可合成的稀有度才建**（`special` 空价 ⇒ 整块不建；判据 = 原版
                //   `CraftingPanel__CheckButtons.c`：空价 ⇒ `SetActive(0)`，**不是变灰**）
                bool craftable = want != "special";
                // 🔴 **2026-10-18 用户口径（判据 → `资料/普查产出_1018/W14_详情窗按钮与副本文案.md` §二）**：
                //   用户原话「创建副本按钮**去掉数字，只留『创建副本』四个字**」⇒ 钮上原来那两件**不再建**：
                //   ① `Craft Text`（原版那颗「1」）· ② `Craft Icon`（原版 `WC icon` 通配符小图）；
                //   换成一个 `Craft Label`（文本 = 字面量 **创建副本**，36px）。
                //   ⚠️ **旧断言 `(Craft Icon 存在) == craftable` 已过期** —— 该节点现在**哪一档都不建**，
                //      所以改成「三件逐件断」：图标/数字恒无 · 四个字可合成档才有、且文本逐字相等。
                //      ⛔ 别改回「按稀有度建图标」。
                CheckTrue(FindChild(dw.transform, "Craft Icon") == null,
                          $"★ `{want}`：`Craft Icon` **哪一档都不建**（用户 2026-10-18：钮上只留四个字）"
                          + $"—— 拿 `{cd.Name}` 试的");
                CheckTrue(FindChild(dw.transform, "Craft Text") == null,
                          $"★ `{want}`：`Craft Text`（原版那颗「1」）**也不建**（同一条口径：去掉数字）"
                          + $"—— 拿 `{cd.Name}` 试的");
                var craftLb = FindChild(dw.transform, "Craft Label");
                if (craftable)
                {
                    CheckTrue(craftLb != null,
                              $"★ `{want}`：`Craft Label` **建出来了**（顶替原来的 `Craft Text` + `Craft Icon`）"
                              + $"—— 拿 `{cd.Name}` 试的");
                    CheckText(TextOf(craftLb), "创建副本",
                              $"★ `{want}`：`Craft Label` 的文本 = **创建副本**"
                            + "（用户 2026-10-18 指定的字面量，**不是**原版词条 —— 原版那颗钮上没有字）");
                }
                else
                {
                    // `special` 空价 ⇒ 整块不建（原版 `SetActive(0)`）⇒ 新加的 `Craft Label` 也不许在
                    CheckTrue(craftLb == null,
                              $"★ `{want}`：整块不建 ⇒ `Craft Label` 也**不许建**"
                            + $"（判据 = 原版 `CraftingPanel__CheckButtons.c` 空价 `SetActive(0)`）—— 拿 `{cd.Name}` 试的");
                }
                // 「升级」那一块：**一律建**（用户 2026-10-12：面板要回来）
                CheckTrue(FindChild(dw.transform, "Upgrade Title") != null,
                          $"★ `{want}`：`Upgrade Title` **建出来了**（面板不许再被停掉）—— 拿 `{cd.Name}` 试的");
                // 🔴 **2026-10-18 第二轮（判据 → `资料/普查产出_1018/W8_副本升级面板_按用户口径.md`）**：
                //   升级钮的可交互度**只看等级上限**（`lv == MaxLevel` ⇒ 灰），实现落点 =
                //   `CardDetailPopup.BuildUpgrade` 的 `upWb.Interactable = !maxed`。
                //   ⚠️ 同日早些时候那一版「可交互度 = `CanUpgrade`」**已作废**（⛔ 别断回来）；
                //   更早的「恒不可交互（默认最高级）」也在 2026-10-18 作废。
                var upb1 = dw.UpgradeHit != null ? dw.UpgradeHit.GetComponent<WindowButton>() : null;
                int lv1 = CardProgress.Level(cd.Id);
                bool belowMax1 = lv1 < CardProgress.MaxLevel;
                CheckTrue(upb1 != null && upb1.Interactable == belowMax1,
                          $"★ `{want}`：`Upgrade` 那颗钮的可交互度 = 「等级 < {CardProgress.MaxLevel}」（{belowMax1}；"
                          + $"等级 {lv1}、多余副本 {CardProgress.Spares(cd.Id, cd.Rarity)}）"
                          + $"—— 拿 `{cd.Name}` 试的");
                // 同一处：用户 2026-10-18 把一颗钮改成两颗 ⇒「降级」那颗**必须也在**（缺了就是改法没落地）
                CheckTrue(dw.DowngradeHit != null,
                          $"★ `{want}`：「降级」那颗钮**在**（`CardDetailPopup.DowngradeHit`）—— 拿 `{cd.Name}` 试的");
                // 🆕 A17：卡片详情窗的两颗圆钮（语音 / 显示卡面文字）
                if (want == "common") CheckHoverSwap(dw.transform, "卡片详情窗");
                dw.Close();
            }
            // 🔴 **2026-10-18（`N116`）**：本文件开的详情窗**最多存活 1 扇** —— 原版关窗只是不显示、
            //   实例由下一次 `CloseWindowCO` 的 `Destroy` 收掉；我们把它提前到「下一次新建之前」。
            //   🔴 **2026-10-19（批次 D · 红 #8）期望值订正**：起手若**已经有一扇**本文件开的窗（上一节点的
            //   `CardHit_0` 详情窗），第一次 `OpenCardDetail` 会先销毁它再新建 ⇒ **净增 0**，旧的 `+1` 只在
            //   「起手为空」时成立。⚠️ 日志实测 =「实得 2 扇；本段开之前 2 扇」= **净增 0**
            //   （`collection.log:52664`）—— 不是简报里说的「多 2 扇」。
            {
                int cdWinsAfter = Object.FindObjectsByType<CardDetailPopup>(
                                      FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                int cdWinsExpect = cdWinsBefore + (hadChainWin ? 0 : 1);
                Check(cdWinsAfter, cdWinsExpect,
                      "★ N116：本段开了 **5** 次卡片详情窗 ⇒ "
                    + (hadChainWin
                        ? "相对起手**不增**（起手已有一扇，第一次 open 先销毁它）"
                        : "场上只多 **1** 扇（最后那扇）")
                    + $"（实得 {cdWinsAfter} 扇；本段开之前 {cdWinsBefore} 扇" + (hadChainWin ? "、起手有本文件的窗" : "") + "）"
                    + " —— 判据 = 原版 `CloseWindowCO` 的收尾是 Hide → List.Remove → **Destroy**"
                    + "（`decomp_full/WindowsManager__CloseWindowCO.c`）；我们那条链的 Destroy 落在"
                    + " `CollectionWindow.OpenCardDetail` 新建之前。改坏法：删掉那一段 ⇒ 每开一次 +1");
                // 更强的一条（不看起手）：第 1 次开成之后的扇数 = 5 次之后 —— 咬住「每次新建前销毁上一扇」。
                // 删掉 N116 那段时：起手为空 ⇒ 第一次 1 扇、末尾 5 扇 ⇒ 这条红。
                if (cdWinsAfterFirst >= 0)
                    Check(cdWinsAfter, cdWinsAfterFirst,
                          $"★ N116（更强）：第 1 次开成之后 = **{cdWinsAfterFirst}** 扇，后面 4 次**不再涨**"
                        + $"（实得 {cdWinsAfter}）—— 这条不看起手，删掉「新建前销毁上一扇」⇒ 本条必红");
                else
                    Check(true, false, "N116：一次详情窗都没开成 ⇒ 这条判不了（上面已有逐次红，⛔ 不静默跳过）");
            }

            // 🆕 A17：本窗的换图按钮（四页共用的 `Clear filters` / `Import` / `Create` / 换风格箭头 / 关闭钮「Back」）
            CheckHoverSwap(win.transform, "收藏窗");
            CheckNoMissingSwapArt("收藏窗这条链");

            // ============================================================ 🆕 2026-10-04：软边接线（三处）
            //
            // 🔴 **这一节量的是「接线」，不是机制** —— 机制（按带的内沿切开 + 逐角 alpha 斜坡）的逐条判据
            //    在 `Editor/RewardsScene.cs` §三·b4-d-2/d-4（对的是原版剖面手算值）。这里补的是另一半：
            //    **原版逐处不同的 `m_Softness` 真的被喂进那几处 `Clip` 了吗**（铁律 5·c：四处四个值）。
            // 判据（逐条实读，全量表 `d:/4/_tmp_view/q1_rm2d.txt`）：
            //   · 档案窗 `Avatar Tab/Item Display Panel/Scroll Rect` = **(0,50)**（:221-222）
            //   · 档案窗 `Title Tab/Item Display Panel/Scroll Rect` = **(0,50)**（:265-266）
            //   · 商店三页 `…/Packs Scroll View/Viewport` = **(0,25)**（`:177-178` / `:295-296` / `:59-60`）
            // 期望的**切线位置**全部由「原版值 + 原版视口矩形」现算（⛔ 不从被测实现里读常量）：
            //   带的内沿 = 视口该边的坐标 ± `m_Softness` 的那个分量。
            // ⚠️ 这两扇窗是**现场建的**（`PlayerProfileWindow` 是 Popup、`ShopWindow` 是 Fullscreen）
            //    —— 放在 `Run` 的**最后**，免得动到前面那些断言的现场。
            Section("软边接线（原版 `RectMask2D.m_Softness`）：档案窗 Avatar / Title 两页 + 商店");
            {
                // ---------------- ① 档案窗 `Avatar Tab`：(0,50) ----------------
                var pp = PlayerProfileWindow.Create(win.Manager);
                win.Manager.OpenWindow(pp);
                CheckTrue(pp.CurrentState == WindowState.Open, "`Player Profile Window` 开起来了（下面量它的两个页）");
                pp.tabButtons.Click(1);
                Check(pp.CurrentTab, WindowTabType.ProfileAvatar, "点第 2 个键 ⇒ 切到 `Avatar` 页");

                var avPage = FindChild(pp.transform, "Avatar Tab");
                var avGrid = FindChild(avPage, "Item Drawer");
                CheckTrue(avGrid != null, "`Avatar Tab/Item Display Panel/Scroll Rect/Item Drawer` 在");
                // 视口 = `AvatarTab` 的 `Scroll Rect`：654.16,210.69 → 1680.12,855.46（原版实测）
                // ⇒ 带内沿：上 210.69 + 50 = **260.69**、下 855.46 − 50 = **805.46**
                // 第 1 行的格（y 250.70..430.70）与第 3 行的格（710.70..890.70）各压在一条带上 ⇒ 两条都该出现
                CheckSoftCuts(ScanSoftCuts(avGrid, false, new PxRect(654.16f, 210.69f, 1680.12f, 855.46f)), new[] { 260.69f, 805.46f }, 0.6f,
                              "`Avatar Tab` 的格子（原版 `m_Softness = (0,50)`）");
                Check(ScanSoftCuts(avGrid, true, new PxRect(654.16f, 210.69f, 1680.12f, 855.46f)).Count, 0,
                      "`Avatar Tab` **一条竖切线都没有** —— 这一处只渐变上下（`(50,0)` 那种写反的实现这里会冒横竖两种）");

                // ---------------- ② 档案窗 `Title Tab`：(0,50)（同一个窗口的另一页）----------------
                pp.tabButtons.Click(2);
                Check(pp.CurrentTab, WindowTabType.ProfileTitle, "点第 3 个键 ⇒ 切到 `Title` 页");
                // ⚠️ 这一页的格子里**没有吃 `Clip` 的图件**：底板走 `ProfilePage.Solid`，而那个口子**不收 `Clip`**
                //    （`PlayerProfileWindow.cs` 的 `Solid` —— 与 `Rect/Nine/Text` 不同，**这是一条真缺口**，已写进报告）。
                //    ⇒ 这一处的可观测面是**压在带里的文字**：`MenuDraw.ClipText` 对带内顶点按同一剖面削 alpha。
                var ttGrid = FindChild(FindChild(pp.transform, "Title Tab"), "Item Drawer");
                // 🔴 **2026-10-23 订正（铁律 5）**：格子里的文字子件从 `Name` 改名为 **`Title`** ——
                //   代理 P1 按原版模板 `Title Drawer Horizontal Variant` 重画了格子
                //   （`Content > Background(石板) + Title(黑字) + Image/Image Top(阵营徽记)`；
                //    原版模板里的 `Label`/`Name`/`Army` 三件 **m_IsActive=0 ⇒ 我们不再建**）。
                //   旧断言按 `Name` 找 ⇒ 恒 null ⇒ 本条会假红。
                var ttLb = FindChild(FindChild(ttGrid, "TitleDrawer_9"), "Title");
                var ttTmp = ttLb != null ? ttLb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
                CheckTrue(ttTmp != null,
                          "第 4 行第 1 格（`TitleDrawer_9`：y 790.70..920.70，**压着视口底 855.46**）的名字有 TMP 网格");
                if (ttTmp != null)
                {
                    // 🔴 **2026-10-25（`X5`）：读 `textInfo` 之前必须先补一次「重排」** —— 判据链（逐行可核）：
                    //   ① 本页是**未激活**时建的：`Shell/PlayerProfileWindow.cs:497-499` 只把 `DefaultTabIndex`
                    //      那一页 `SetActive(true)`，六页的 `Setup()` 在循环**之后**才跑 ⇒ 每颗 `Title`
                    //      建的时候父链是关着的（`Shell/TitleTab.cs:384-386`）。
                    //   ② ⇒ `Label.SetWrapWidth` 只把「生成」记成待办、不生成（`Battle/Label.cs:641-649`）。
                    //   ③ 补做那一刀（`Battle/Label.cs:673-681`，激活时/`RefreshBounds` 尾句）走的是
                    //      `TmpFont.GenerateLayout` = **单跑一遍** `GetTextInfo`（`Core/TmpFont.cs:572-576`）。
                    //   ④ 本格是**自适应**（auto[15~90]/基准 36）⇒ 单跑一遍命中「超框 ⇒ 缩字号 ⇒ 在 Phase II
                    //      之前 early-return」（`TextMeshPro.cs:4138-4155`）⇒ `m_textInfo.characterCount` 仍是 **0**
                    //      （它是 Phase II 才赋值的；本工程那条已查清的判据 = `Battle/Label.cs:702-710` 的 P7 注释）。
                    //   ⑤ **批处理没有帧循环**（`CLAUDE.md` §三）⇒ TMP 自己那条收敛路那一刻还没跑
                    //      ⇒ 直接读 `textInfo` 读到的是空模型（今天这 3 红就是这么来的，`0` 顶点）。
                    //   `Label.ForceRelayout()` 的**已激活**那一档 = `SetFontSize(现值) → ForceMeshUpdate()`
                    //   （`Battle/Label.cs:924`，值相同 ⇒ setter 早退、只剩那次 `ForceMeshUpdate`）⇒ 走
                    //   `OnPreRenderObject` 的 `while (m_IsAutoSizePointSizeSet == false) GenerateTextMesh();`
                    //   （`TextMeshPro.cs:2171-2175`）= **完整自适应收敛**（和 ③ 的单跑一遍不是一回事）。
                    //   ⛔ 别改成 `TmpFont.GenerateLayout`（= ③ 那一次单跑一遍、不收敛 ⇒ 在这里会**重现**
                    //      同一个空模型）。`ForceRelayout()` 比裸 `ForceMeshUpdate()` 多的只有尾巴那句
                    //      `RefreshBounds()`（把 `_tmpW/_tmpH` 落回字段、并把欠下的裁切补上）。
                    //   ⚠️ 它**只**把「模型/网格」补成当前真值，**不改**渐隐带那套判据（期望值仍是原版剖面
                    //      `255 × (855.46 − y) ÷ 50`）⇒ 不是自证。
                    var ttLabel = ttLb != null ? ttLb.GetComponentInChildren<Label>(true) : null;
                    CheckTrue(ttLabel != null,
                              "（前提·不静默）`Title` 上挂着 `Label` —— 上面那次重排要走它的公开口"
                            + "（拿不到 ⇒ 这一步没跑，下面那些红不是绿）");
                    if (ttLabel != null) ttLabel.ForceRelayout();

                    var ti = ttTmp.textInfo;
                    int nV = 0, nBand = 0, nBad = 0, nBelow = 0, nBadBelow = 0;
                    float minA = 255f, worst = 0f;
                    if (ti != null && ti.characterInfo != null && ti.meshInfo != null)
                    {
                        int cn = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
                        for (int ci = 0; ci < cn; ci++)
                        {
                            var ch = ti.characterInfo[ci];
                            if (!ch.isVisible) continue;
                            int mi = ch.materialReferenceIndex;
                            if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                            var mesh = ti.meshInfo[mi];
                            if (mesh.vertices == null || mesh.colors32 == null) continue;
                            for (int k = 0; k < 4; k++)
                            {
                                int v = ch.vertexIndex + k;
                                if (v < 0 || v >= mesh.vertices.Length || v >= mesh.colors32.Length) continue;
                                float y = LayoutSpace.ToPixel(ttTmp.transform.TransformPoint(mesh.vertices[v])).y;   // 甲式保留：顶点量法（W57 §1·4 第 4 条）
                                int a = mesh.colors32[v].a;
                                nV++;
                                minA = Mathf.Min(minA, a);
                                if (y > 805.46f)          // 带内：alpha 必须 = 255 × (视口底 855.46 − y) ÷ 50
                                {
                                    nBand++;
                                    float want = Mathf.Clamp01((855.46f - y) / 50f) * 255f;
                                    float err = Mathf.Abs(a - want);
                                    worst = Mathf.Max(worst, err);
                                    if (err > 3f) nBad++;
                                }
                                else                      // 带外：**一个顶点都不该被动**（硬裁那半句照旧）
                                {
                                    nBelow++;
                                    if (a < 250) nBadBelow++;
                                }
                            }
                        }
                    }
                    // 🔴 **2026-10-25（`X5`）：前提拆成三条各自独立**（原来那一条「有 TMP + 排得出字 + 有顶点」是
                    //   **复合前提**：空集时它只红一条，而 `nBad==0` / `nBadBelow==0` 这两条**空集恒真 = 假绿**
                    //   —— 今天 3 红那一跑里正是这两条假绿，log `:161624` / `:161638`）。现在：
                    //   `characterCount` 红 = **版面没排出来**（模型层）；`nV` 红 = **排出来了但网格没上传**（另一档）；
                    //   两条分开 ⇒ 出红即自证卡在哪一档，⛔ 不再靠猜。
                    int nChars = ti != null ? ti.characterCount : -1;
                    CheckTrue(nChars > 0, $"模型把这一格的文字**排出来了**（`characterCount = {nChars}`）"
                                        + " —— **0 = 版面没排出来**（不是渐隐带/网格的问题；上面那段重排注释是判据）");
                    CheckTrue(nV > 0, $"量得到这一格的文字网格（{nV} 个顶点）"
                                    + $" —— `characterCount = {nChars}` 有字却 0 顶点 = **另一种**病（网格没上传）");
                    CheckTrue(nBand > 0, $"**有 {nBand} 个顶点落在渐隐带里**（y > 805.46）—— 0 个 = 软边没接上"
                                       + "（顶点还会被硬裁夹到 855.46，但 alpha 一个都不动）");
                    CheckTrue(minA < 250f, $"带内的字**确实被削了 alpha**（最小 {minA:F0} < 250）—— 只夹顶点不削 alpha 是硬边");
                    if (nV > 0)
                    {
                        Check(nBad, 0, $"带内每个顶点的 alpha = **255 × (855.46 − y) ÷ 50**（原版剖面；最差差 {worst:F2}）");
                        CheckTrue(nBadBelow == 0, $"带外（y ≤ 805.46）的顶点 {nBelow} 个**一个都没被动**"
                                                + "（软边只改带内；动的那些就是写错了带的位置）");
                    }
                    else
                    {   // ⛔ 空集时这两条**没有证据价值**（原来是恒真 = 假绿）⇒ 走「判不了」出声，⛔ 不静默算绿
                        Check(true, false, "（判不了）带内 alpha 剖面：上面 `nV = 0` ⇒ 一条顶点都没量到，本条**不构成证据**（⛔ 不是绿）");
                        Check(true, false, "（判不了）带外顶点一个都没被动：同上，`nV = 0` ⇒ 空集，本条**不构成证据**（⛔ 不是绿）");
                    }
                }

                // ---------------- 🆕 2026-10-18（A840）：两页视口**各记过自己的设计矩形** ----------------
                //   记录点 = `Shell/AvatarTab.cs` 的 `vc.CaptureNow();` / `Shell/TitleTab.cs` 的 `vc.CaptureNow();` 各补的那一句。
                //   口径 → `Shell/ViewportClip.cs` 文件头 §①；现场 → `资料/普查产出_1018/S1_A840与A867.md` §1/§2.1。
                //   🔴 **为什么非断不可**：`MenuDraw.ApplyPxRect` 往这个节点写矩形**发生在组件存在之前**
                //   （那一句的穿透只找**已有**的 `ViewportClip`）⇒ ⛔ 少了 `CaptureNow()` 这两颗**逐位回落到
                //   旧写法**（实时反推 + `LiveDerivations`）：本页整块面板被挪过之后，框与被比矩形就**不在同一帧**
                //   （A811 那个病灶）。⇒ **逐页各一条**，⛔ 不写成「至少有一页对」那种弱断言。
                //   期望值 = **原版 prefab 字面量**（两页逐位同矩形 654.16,210.69 → 1680.12,855.46，
                //   就是上面那几条 `ScanSoftCuts` 用的那一份）；⛔ 不读 `AvatarTab.VpL` / `TitleTab.VpL`
                //   —— 那是**被测实现传进 `Node(...)` 的实参**（拿它当期望值 = 自证）。
                void A840Check(string who, Transform tabRoot, string path, PxRect want)
                {
                    var vn = tabRoot != null ? tabRoot.Find(path) : null;
                    var vv = vn != null ? vn.GetComponent<ViewportClip>() : null;
                    CheckTrue(vv != null,
                              $"（前提·不静默）{who}：`{path}` 那颗节点上挂着 `ViewportClip`"
                            + " —— ⛔ 拿不到 ⇒ 下面那两条**没跑**，不是绿");
                    if (vv == null) return;
                    CheckTrue(vv.HasBaseRect,
                              $"★ A840：{who} 这颗视口**记过设计矩形**（`ViewportClip.HasBaseRect`）"
                            + " —— 记录点 = `AddComponent<ViewportClip>()` 之后那句 `vc.CaptureNow();`"
                            + "｜**改坏法**：删掉那一句 ⇒ 本条红（该颗逐位回落到实时反推 + `LiveDerivations`）");
                    var b = vv.BaseRect;
                    CheckTrue(Mathf.Abs(b.x1 - want.x1) <= 0.05f && Mathf.Abs(b.y1 - want.y1) <= 0.05f
                              && Mathf.Abs(b.x2 - want.x2) <= 0.05f && Mathf.Abs(b.y2 - want.y2) <= 0.05f,
                              $"★★ A840：{who} 记下的矩形 = **宿主写进这个节点的那份设计矩形**"
                            + $"（实测 {b.x1:F3},{b.y1:F3} → {b.x2:F3},{b.y2:F3}；"
                            + $"期望 = 原版 prefab 的 {want.x1:F2},{want.y1:F2} → {want.x2:F2},{want.y2:F2}）"
                            + "｜**改坏法**：把 `CaptureNow()` 挪到节点被改过之后再调（= 记成**错帧**）⇒ 红"
                            + "（`HasBaseRect` 仍 true ⇒ 上面那条抓不住它，只有本条抓得住）");
                }
                A840Check("档案窗 `Avatar Tab`（`Shell/AvatarTab.cs`）",
                          FindChild(pp.transform, "Avatar Tab"), "Item Display Panel/Scroll Rect",
                          new PxRect(654.16f, 210.69f, 1680.12f, 855.46f));
                A840Check("档案窗 `Title Tab`（`Shell/TitleTab.cs`）",
                          FindChild(pp.transform, "Title Tab"), "Item Display Panel/Scroll Rect",
                          new PxRect(654.16f, 210.69f, 1680.12f, 855.46f));

                pp.Close();
                Check(pp.CurrentState, WindowState.Closed, "量完把档案窗关掉（别影响后面的现场）");

                // ---------------- 🆕 2026-10-05：聊天窗 `ChatPanel` ----------------
                //   （§三第29条 **A38 顺手发现①** = 消息行的**滚动区** + **A77⑧** = 压暗层命中区走公共件）
                //   🔴 原版参数是**现读的**（`bundle_mainmenualwaysloaded_assets_all` 的 `Chat Tab` 那棵树）：
                //     `Chat Tab` 那个 GO 上**没有 `ScrollRect`** —— 它是 OSA 虚拟列表
                //     （`ChatContentView : OSA<BaseParamsWithPrefab, ChatEntryView>`，参数在 `_Params` 里：
                //      `_ContentPadding = (25,25,5,5)` · `_ContentSpacing = 10` · `_DefaultItemSize = 60` ·
                //      `_ScrollSensivity = 20` …）。⛔ 所以**不能照抄** `BattleLogPopup` 那组 `Clamped/1.0`
                //     —— 铁律 5·c。下面每一条期望值都从**原档那几个字段**算出来，不是回读实现。
                Section("聊天窗：消息行的**滚动区**（A38 顺手发现①）+ 压暗层命中区（A77⑧）");
                {
                    SocialData.ChatMessages.Clear();                  // 本地没有服务器 ⇒ 这一页出厂就是空的
                    var chat = ChatPanel.Create(win.Manager);
                    win.Manager.OpenWindow(chat);
                    chat.tabButtons.Click(0);
                    Check(chat.CurrentState, WindowState.Open, "聊天窗开得起来（`ChatPanel.Create` + `OpenWindow`）");
                    Check(chat.type, WindowType.Popup, "`type` = **1 Popup**（与社交窗相反）");
                    CheckTrue(ChatPanel.LastOpened == chat, "`ChatPanel.LastOpened` 指到它（自检口）");

                    // 🔴 **2026-10-18（第七轮）：输入框占位符走词条** —— 键 = 原版那颗 `Placeholder` 的
                    //   `Localize.mTerm` 原文 `MainMenu/Chat/TypeMessage`（1 颗；父链 =
                    //   `Placeholder < Text Area < InputField (TMP) < Enter Text < Chat < Holder < ChatPanel`
                    //   ⇒ 节点名与路径都和我们这一颗一致）。
                    //   ⚠️ 我们的树上**没有 `Text Area` 那一层**（`Text(...)` 直接把字挂在 `InputField (TMP)` 下）
                    //   ⇒ 路径少一节，别照抄原版那条。
                    {
                        var ci = FindChild(FindChild(FindChild(FindChild(chat.transform, "Holder"), "Chat"), "Enter Text"),
                                           "InputField (TMP)");
                        var cph = ci != null ? FindChild(ci, "Placeholder") : null;
                        CheckTrue(Loc.HasEntry("MainMenu/Chat/TypeMessage"),
                                  "（前提）词条 `MainMenu/Chat/TypeMessage` 在表里（⛔ 不在 ⇒ 下一条两边一起退化成键名 = 假绿）");
                        CheckText(TextOf(cph), Loc.T("MainMenu/Chat/TypeMessage"),
                                  "★ 聊天窗输入框占位 = `Loc.T(\"MainMenu/Chat/TypeMessage\")`（随语档；"
                                + "原版 TMP 原文 `Type message` / 中文「输入消息」`zh_CN.csv:183`）");
                    }

                    // ① 压暗层命中区（A77⑧）：档 = 压暗层自己那一档（`QPanel` = 3300）、且严格低于内容档
                    //    （`QHit` = 3308）；并且**确实是公共件 `MenuDraw.ShadeHit` 建的**（改回本窗自己那份
                    //    `MenuDraw.Hit` 就红 —— 两条路没有任何可见行为差异，只有这一句分得出来）。
                    //    🔴 A77⑬③：期望值改成**量**本窗那块**视觉**压暗层 —— 它的节点名是 `CloseBackground`
                    //      （原版语义同族、名字不同；那颗 quad 在它子件 `Image` 上，见 `Shell/ChatPanel.cs`），
                    //      所以这条走 `Holder/CloseBackground` 路径，而**不**按 `Menu Dark Background` 找。
                    MenuDraw.CheckShadeRule(CheckTrue, "聊天窗", FindChild(FindChild(chat.transform, "CloseBackground"), "CloseHit"),
                                            chat.transform.Find("Holder/CloseBackground"), ChatPanel.QHit);

                    // ② 滚动区（A38 顺手发现①）
                    var ctab = chat.tabs.Count > 0 ? chat.tabs[0] as ChatTab : null;
                    CheckTrue(ctab != null, "第 0 页就是 `ChatTab`（Global）");
                    var csc = ctab != null ? ctab.RowsScroll : null;
                    CheckTrue(csc != null,
                              "★ 这一页**有滚动区了**（`MenuScroll`）—— 补之前 `grep MenuScroll Shell/ChatPanel.cs`"
                              + " **零命中**：消息行既不滚也不裁，**超一屏直接画到框外**、第一屏之外的行永远看不到");
                    if (csc != null && ctab != null)
                    {
                        // 视口 = 原版 `Chat Tab/Viewport`（同一棵树里那一段就是 `Tabs` 给的矩形：
                        // 613.88,161 → 1813.88,911 ⇒ 高 750）。期望值是**原档矩形**，不是回读实现。
                        CheckNear(csc.Viewport.x1, 613.88f, 0.6f, "滚动视口 = 原版 `Chat Tab/Viewport` 左沿 **613.88**");
                        CheckNear(csc.Viewport.y1, 161f, 0.6f, "…上沿 **161**");
                        CheckNear(csc.Viewport.H, 750f, 0.6f, "…高 **750**（911 − 161）");
                        CheckTrue(csc.Vertical, "是**纵向**滚动（原版 `_Orientation = 0`）");

                        // 空表：内容高也要写（写成视口顶）—— 不写就是「上一次的脏值留在区里」（同 BattleLogPopup 那条）
                        CheckNear(ctab.ContentBottom, 161f, 0.6f,
                                  "空表（0 条）也写 `ContentX2` = 视口顶 —— 这一句是「空表不清脏值」那类静默 bug 的判据");
                        CheckTrue(csc.ClampHi <= 0.01f, "…而且此刻**滚不动**（内容比视口短 ⇒ 上下界都收到 0）");

                        // 喂 20 条（60px 行高 + 10px 行距 ⇒ 内容 1400 > 视口 750 ⇒ 真能滚）
                        for (int i = 0; i < 20; i++)
                            SocialData.ChatMessages.Add(new SocialData.ChatMessage
                            {
                                Channel = "Global", Sender = "Probe" + i, Time = "0d 0h",
                                Text = "msg-" + i.ToString("00"), Mine = false, Height = 0f,
                                AvatarArt = ProfileData.AvatarArt,
                            });
                        chat.RefreshMessages();

                        var ccon = ctab.transform.Find("Viewport/Content");
                        CheckTrue(ccon != null, "`Chat Tab/Viewport/Content` 在（消息行挂它下面）");
                        Transform RowOf(string txt)
                        {
                            if (ccon == null) return null;
                            foreach (var t in ccon.GetComponentsInChildren<Transform>(true))
                                if (t.name == "ChatMessageRow" && TextOf(FindChild(t, "Message")) == txt) return t;
                            return null;
                        }

                        // 内容高 = **原版 OSA 的算式**：padT 5 + 20×60 + 19×10 + padB 5 = **1400**
                        // ⇒ `ClampHi` = 1400 − 750 = **650**（这两个数是从 `_Params` 那四个字段算出来的）
                        CheckNear(ctab.ContentBottom, 161f + 1400f, 1f,
                                  "★ 20 条 ⇒ 内容底 = 视口顶 + **1400**（原版 `_ContentPadding`/`_ContentSpacing`/`_DefaultItemSize`）");
                        CheckNear(csc.ClampHi, 650f, 1f, "★ …所以**能滚 650px**（1400 − 750）—— 滚不动的话这条直接红");
                        Check(ctab.BuiltRows, 11,
                              "偏移 0 ⇒ 建出 **11** 行（视口 750 / 每行占 70 ⇒ 161..911 里正好 11 行；"
                              + "整行在视口外的**连节点都不建**）");
                        var r0 = RowOf("msg-00");
                        CheckTrue(r0 != null, "偏移 0 ⇒ **第 1 行**建出来了");
                        if (r0 != null)
                            CheckNear(MenuCheck.DesignPx(r0).y, 196f, 0.6f,
                                      "…它的中心 = 视口顶 161 + `_ContentPadding.top` 5 + 半行 30 = **196**");
                        CheckTrue(RowOf("msg-19") == null,
                                  "★ 偏移 0 ⇒ **最后一行根本没建**（它整行在 y 1496..1556，视口底下；"
                                  + "补之前是「画到框外」，现在是「不建」——两种都不该出现在画面上）");

                        // 滚到最下（`SetOffset` 就是滚轮/拖拽那条路公用的那一个口）
                        float y10 = RowOf("msg-10") != null ? MenuCheck.DesignPx(RowOf("msg-10")).y : float.NaN;
                        csc.SetOffset(csc.ClampHi);
                        CheckNear(csc.Offset, 650f, 0.01f, "滚到最下 ⇒ 偏移 = `ClampHi` = **650**");
                        var last = RowOf("msg-19");
                        CheckTrue(last != null,
                                  "★★ 滚到最下 ⇒ **最后一行建出来了**（「超一屏的内容滚得到」的判据 —— 只断「行数」"
                                  + "分不出这两态：两种偏移下都是 11 行）");
                        if (last != null)
                        {
                            float ly = MenuCheck.DesignPx(last).y;
                            CheckTrue(ly >= csc.Viewport.y1 && ly <= csc.Viewport.y2,
                                      $"…而且它**落在视口里**（中心 y = {ly:F1}，视口 {csc.Viewport.y1:F1}..{csc.Viewport.y2:F1}）");
                        }
                        CheckTrue(RowOf("msg-00") == null,
                                  "★★ …而**第一行滚出视口 ⇒ 不建了**（与上面那条合起来 = 「建的是哪几行」随偏移变）");
                        var r10b = RowOf("msg-10");
                        CheckTrue(r10b != null, "…中间那行（msg-10）两种偏移下都在视口里（下面拿它量位移）");
                        if (r10b != null && !float.IsNaN(y10))
                            CheckNear(y10 - MenuCheck.DesignPx(r10b).y, 650f, 0.6f,
                                      "★ 同一行**真的换了位置**：内容上移的像素数 == 偏移（650）");

                        // ★ 「**裁**」那半份也要有判据（改之前那一页是「既不滚也不裁」，行直接画到框外）。
                        //   最后那一行的头像命中区：它自己的矩形 = (545.87,841)→(691.90,1004.14)
                        //   （左 68px、下 93px 都在视口外）⇒ 实建出来那块应当**被截到视口**：
                        //   **(613.88,841)→(691.90,911)**。期望值全是几何算出来的（原档行内偏移 + 视口矩形），
                        //   ⛔ 不是回读实现；`clip` 那一格要是丢回 `null`，下沿会变回 1004.14 ⇒ 这两条红。
                        var hitLast = last != null ? FindChild(last, "Hit") : null;
                        float hx1, hy1, hx2, hy2;
                        if (hitLast != null && RectOf(hitLast, out hx1, out hy1, out hx2, out hy2))
                        {
                            CheckNear(hx1, csc.Viewport.x1, 0.8f,
                                      "★ 头像命中区**左沿被裁到视口左缘 613.88**（不裁的话它在 545.87）");
                            CheckNear(hy2, csc.Viewport.y2, 0.8f,
                                      "★ 下沿被裁到**视口底 911**（不裁的话它在 1004.14）");
                            CheckNear(hy1, 841f, 0.8f, "…上沿**没被裁**（841，本来就落在视口里）");
                        }
                        else CheckTrue(false, "拿不到最后一行头像命中区的矩形 —— 这一条等于没验（滚出视口/被裁没了？）");

                        var cpl = PointerLayer.Instance;
                        // 🆕 2026-10-20（A1350 第二批）：(1200,500) 是**设计 px**（视口中央）；
                        //   `ScrollUnder` 内部把它和 `DesignToPtrPx(Viewport, Owner.lossyScale)` 比 ⇒ 先换到指针帧。
                        //   `owner` = `Viewport` 节点（`ScrollScale` 取的正是它父级 `_root` 那一级 `lossyScale`）。
                        var vpNode = ctab.transform.Find("Viewport");
                        var underP = MenuCheck.PointerFramePxFromDesign(new Vector2(1200f, 500f),
                                         vpNode != null ? vpNode : ccon);
                        var under = cpl != null ? cpl.ScrollUnder(underP.x, underP.y) : null;
                        CheckTrue(under == csc,
                                  "★ 视口中央那一点，**滚轮落到的就是这一区**（`SocialPage.RegisterScroll` 那条路）——"
                                  + "拿到 " + (under == null ? "**null**（没登记）" : "`" + under.GetType().Name + "`"));
                    }

                    // 收尾：清数据 + 关窗（它的压暗层是整屏的，留着会顶掉后面那些真命中路）
                    SocialData.ChatMessages.Clear();
                    chat.RefreshMessages();
                    if (ctab != null) Check(ctab.BuiltRows, 0, "清掉消息 ⇒ 0 行（收尾）");

                    // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                    //   期望矩形 = **原版 prefab** `ChatPanel > Holder > Chat > ChatBackground`
                    //   那颗 `Image` 的 rect（`563.88, 146 → 1863.88, 1076`）；⛔ 不写 `ChatPanel.ChatBgR`
                    //   —— 那是被测实现**传进去的实参**（同式自证）。
                    CheckAbsorbRule("聊天窗", chat.transform, "AbsorbHit",
                                    563.88f, 146f, 1863.88f, 1076f,
                                    ChatPanel.QPanel, ChatPanel.QHit, () => chat.CurrentState);
                    // ⚠️ 上面那一组**结尾就把窗关掉了**（「点面板外 ⇒ 关」那一步）⇒ 这里重开一次，
                    //   下面那条「收尾：聊天窗关掉」才是**真**在断 `Close()`（不开回来它就恒绿了）。
                    //   `ChatPanel.Open()` = `Build()` 重建（不依赖 `Data`）⇒ 重开是安全的。
                    CheckTrue(chat.TryOpen(null), "（A94 收尾）把聊天窗**开回来** —— 下面那条才不是空断");
                    chat.Close();
                    Check(chat.CurrentState, WindowState.Closed, "收尾：聊天窗关掉");

                    // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据 → `:2298` 那一段）。
                    //   ⚠️ 本窗那颗命中区的节点名是 **`CloseBackground/CloseHit`**（同族三扇里只有它不叫
                    //      `BackgroundHit`）⇒ 名字照上面 `CheckShadeRule` 那一行现成的实参抄，⛔ 别统一写
                    //      `BackgroundHit`（`Editor/MainMenuScene.cs` 的 `BackgroundHit` 记着那次误判）。
                    //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后（下面紧接着是 `CardPresentation/Editor/MainMenuScene.cs` 的 ShopWindow 那一段，
                    //      与 `chat` 无关）；上面已经关过一次 ⇒ 先开回来。
                    CheckTrue(chat.TryOpen(), "（A796 现场）把聊天窗开回来 —— 下面那条要在**开着**的窗上点");
                    MenuDraw.CheckShadeClickRule(CheckTrue, "聊天窗",
                                                 chat.transform,
                                                 FindChild(FindChild(chat.transform, "CloseBackground"), "CloseHit"),
                                                 () => chat.CurrentState);
                }

                // ---------------- ③ 商店：三页的 `Packs Scroll View` 都是 (0,25) ----------------
                // ⚠️ **ShopWindow 是 Fullscreen** ⇒ `OpenWindow` 会**把关着的当前主窗关掉**
                //    （`WindowsManager.OpenWindow` 那条原版判定）—— 所以这一段放在**最后**。
                var shop = ShopWindow.Create(win.Manager);
                win.Manager.OpenWindow(shop);
                shop.tabButtons.Click(0);
                var shopPage = FindChild(shop.transform, ShopData.Pages[0].Prefab);
                // ⚠️ 本文件没有 `FindPath`（`FindChild` 是**按名字**找的、不认识 `A/B/C`）⇒ 用 `Transform.Find`
                var shopContent = shopPage != null ? shopPage.Find("Packs Scroll View/Viewport/Content") : null;
                CheckTrue(shopContent != null, "商店 `Card Shop Tab/Packs Scroll View/Viewport/Content` 在");
                // 视口 = 329.76,127.62 → 1920.00,1080.00（原版实测）
                // ⇒ 带内沿：上 127.62 + 25 = **152.62**、下 1080.00 − 25 = **1055.00**
                // 第 1 行格底（133.62..610.62）与第 2 行格底（608.62..1080.00，硬裁到视口底）各压一条 ⇒ 两条都该出现
                CheckSoftCuts(ScanSoftCuts(shopContent, false, ShopTabPage.ScrollView), new[] { 152.62f, 1055.00f }, 0.6f,
                              "商店 `Packs Scroll View` 的格子（原版 `m_Softness = (0,25)`）");
                Check(ScanSoftCuts(shopContent, true, ShopTabPage.ScrollView).Count, 0,
                      "商店这一处**一条竖切线都没有** —— `m_Softness = (0,25)` 只渐变上下");

                // 🆕 **2026-10-17（`A1212` 块 6 #15/#16/#17）**：商店页头那三颗的四格。
                //   判据 = 原版 `ShopWindow ▸ … ▸ {RefreshText, Time, Available Counter}`：
                //   `10/30/25/0` · `15/30/25/0` · `16/31.5/39/0`（`Available Counter` 的 `fontPx` 已是 **39**、
                //   闸 `39 > 16` 真过 —— 账里那句「传 16f、被闸挡住」**已过期**，本批现读订正）。
                CheckA1212(shop.transform, "RefreshText", "ShopWindow ▸ … ▸ RefreshText",
                           10f, 30f, 25f, 0);
                CheckA1212(shop.transform, "Time", "ShopWindow ▸ … ▸ Time",
                           15f, 30f, 25f, 0);
                CheckA1212(shop.transform, "Available Counter", "ShopWindow ▸ … ▸ Available Counter",
                           16f, 31.5f, 39f, 0);
                shop.Close();
            }

            // ================= 🆕 2026-10-11（A327 · A306② + A306④）：两态夹具 =================
            //   判据 / 断言什么 / 为什么这个形状能照出它 → `资料/普查产出_1011/W4_子3.md` §四·b（两行）。
            //   🔴 **2026-10-11（FX3）订正一处说法（铁律 5）**：原来这两行写「两处都是**潜伏缺陷**」—— **不准确**。
            //   `basis == 窗根` ⇒ `PosInDesignSpace` 除的是窗根的**父级** = Holder（恒单位缩放，
            //   `Shell/WindowsManager.cs` 的 `MakeHolder`）、而 `AttachToAnchor` 把窗根钉在 `localPosition = 0`
            //   （同一文件里那句 `SetParent(anchor, false)` + `localPosition = zero`）⇒ 新旧两式在**该调用形状下永远逐位相同**（⛔ 不是「今天观测不到」）
            //   ⇒ A306①②③④ 那四处码的改动在生产里**是 no-op**（留着只因口径更对 / 防御性）。
            //   ⛔ **别再说成「修好了一个带电的潜伏缺陷」**；真带电的是**非根基准**那一族
            //   （`Shell/PracticeModePopup.cs`）。
            //   ⚠️ 本夹具（M 加在**窗根的父级** + 态二**重建**）照的正是**那一族**的形状（判据 → 文件头 ①②③）。
            // 🔴 **`A1391`（2026-10-19 · 第五十五会话 S3）**：进场先记下写盘闸的原值，收尾按它放回 ——
            //   ⛔ 不许收尾硬写 `false`（那是把「进场档」当成常量；`Run` 里若另有谁把它设成 `true`，
            //   这里就替别人把闸抬了）。判据 = 账 `A1391`。
            bool ssPersistWas = SmallScreenUI.PersistOverride;
            SmallScreenUI.PersistOverride = true;      // ⛔ 自检不许动玩家的真设置
            Section("A327 · A306②：`DeckInfoPopup.Local3` 的落位（关 = 逐值不变 / 开 = 设计点 × M）");
            {
                var dipA = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.View);
                CheckTrue(dipA != null, "（前提）`Deck info Popup` 建出来了");
                if (dipA != null)
                {
                    // 🔴 **2026-10-11（FX3）换夹具姿势**（原来是把**窗根**挪到 (2,1.5) ⇒ 那条恒等式必红，见文件头 ①②）：
                    //   M 加在**窗根的父级**那一颗探针根上；窗根留在它**下面**、只给它一个非零位移 (2,1.5)。
                    //   ⇒ 基准（= 窗根）相对被乘那一级的位移 = (2,1.5)（|·| = 2.5 ≥ 1）：坏式与好式相差
                    //     `M(M−1)×2.5` = **0.6 世界单位 = 65px**（容差 0.02 = 2.2px）⇒ 真会红。
                    var probeB = new GameObject("A327② probe root");          // ← 这一颗才是「被乘 M 的那一级」
                    dipA.transform.SetParent(probeB.transform, false);        // 窗根留在被乘那一级**下面**……
                    dipA.transform.localPosition = new Vector3(2f, 1.5f, 0f); // ……并在它下面有一个非零位移
                    CheckTrue(dipA.TryOpen(null), // 态一（开关关）建一遍 ⇒ p1 == 设计点
                              "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                    // 🆕 **2026-10-17（`A1212` 块 4）**：本窗那 7 处的四格 + 框。
                    //   判据 = 原版 `Deck info Popup` 三份 MB 逐颗现读（pid 见表内注释）：
                    //   `Deck Name 10/45/42/0` · `Warlord Name 10/40/38/0` ·
                    //   三颗钮 `Button Text 10/40/12/0`（有效框 **294.41 × 76.70**，`A1226` 表 B #37）·
                    //   `Cost Text 18/50/32/1`（有效框宽 **48.819**，`N34②`）·
                    //   `Deck Information Cost/balance text 10/44/22.3/1`。
                    //   ⚠️ 行里的 `Name 2/38/27.69/0` 未断 —— 它只在**有卡的行走**里建，本夹具的行由
                    //      `CurInfo` 决定，逐张数不固定；`Count 2/32/36/0` 同理（`n > 1` 才建）⇒ 两条如实登记。
                    CheckA1212(dipA.transform, "Deck Name", "Deck Details ▸ Deck Details ▸ Deck Name",
                               10f, 45f, 42f, 0);
                    CheckA1212(dipA.transform, "Warlord Name", "Deck Details ▸ Warlord Name",
                               10f, 40f, 38f, 0);
                    CheckA1212(dipA.transform, "Text Practice Deck", "Buttons ▸ Practice Deck ▸ Button Text",
                               10f, 40f, 12f, 0, boxW: 294.41f, boxH: 76.70f);
                    CheckA1212(dipA.transform, "Text Edit Deck", "Buttons ▸ Edit Deck ▸ Button Text",
                               10f, 40f, 12f, 0, boxW: 294.41f, boxH: 76.70f);
                    CheckA1212(dipA.transform, "Text Select Deck", "Buttons ▸ Select Deck ▸ Button Text",
                               10f, 40f, 12f, 0, boxW: 294.41f, boxH: 76.70f);
                    //   🔴 **2026-10-18 就地订正（铁律 5）**：`Cost` 的框高 **47.88 → 49.66**（实得 49.66）。
                    //      原版链现核（`bundle_menus_assets_all`）：`Deck List/Content` 的
                    //      `GridLayoutGroup.cellSize.y = **58**` ⇒ 行高 58 ⇒ `Background` 高 58（RT
                    //      `6569929413234690472`：anchors y 0..1 · sd.y 0）⇒ `Cost Image` 高 **58 + 6 = 64**
                    //      （RT `4517749637251500456`：anchors y 0..1 · `m_SizeDelta.y = 6`）⇒ `Cost` TMP 高
                    //      `0.776 × 64 = **49.664**`（RT `-3142486381871265368`：anchors y `0.112 → 0.888` · sd.y ≈ 0）。
                    //      **47.88 的来源 = `RM_原版TMP框尺寸.md` 表 B 的 55.7 行高那一支**
                    //      （= `Deck Editing Menu` 的 `DeckRuntime.cs` `RowCostS = 61.7 = 55.7 + 6`）
                    //      —— **张冠李戴**：本 popup 的行高是 58，不是 55.7。
                    CheckA1212(dipA.transform, "Cost Text", "Deck List ▸ … ▸ Cost Image ▸ Cost",
                               18f, 50f, 32f, 1, boxW: 48.819f, boxH: 49.66f);
                    CheckA1212(dipA.transform, "Deck Information Cost/balance text",
                               "Deck Info ▸ Deck Information Cost/balance text", 10f, 44f, 22.3f, 1);
                    var btnsA = FindChild(dipA.transform, "Buttons");
                    CheckTrue(btnsA != null, "（前提）`Buttons` 在（那一层就是 `Local3(root, …)` 摆的）");
                    if (btnsA != null)
                        CheckScaleTwo(probeB, dipA.transform,
                                      () =>
                                      {
                                          // 🔴 **2026-10-12（A437）态二那次必须【先 `Close()` 再开】** ——
                                          //   A217② 把 `GameWindow.TryOpen` 按原版改成按 `CurrentState` 分三档之后，
                                          //   **同窗再开（`Open` 支）会早退、不重建** ⇒ 光再调一次 `TryOpen` 拿到的还是
                                          //   态一那颗 `Buttons`（旧节点）⇒ 下面那条「真的重建了」的前提先红、★ 退化成假绿。
                                          //   `Close()` 走的是**生产那条链**（`Close` → `NotifyClosed` → `state = Closed`）
                                          //   ⇒ 紧接着的 `TryOpen` 落回 `Closed` 支、重建照旧。⛔ **别改成直调 `Open()`**。
                                          dipA.Close();               // ← A437：把 state 送回 `Closed`
                                          CheckTrue(dipA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）
                                                    "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                                          var n = FindChild(dipA.transform, "Buttons");
                                          // （前提③）**真的重建了**：拿到的是**新**节点 ⇒ 被量的局部位置是在
                                          // `k ≠ 1` 那一趟**重算**出来的，不是态一冻结的那份。改坏法：`measure`
                                          // 改回纯读（`() => btnsA.position`）⇒ 这条红（而 ★ 会退化成假绿）。
                                          CheckTrue(n != null && n != btnsA,
                                                    "（前提）…：态二的 measure **真的重建了** `Buttons`"
                                                  + "（`Build()` 首句清空子件 ⇒ 拿到的是新节点）");
                                          if (n != null) btnsA = n;
                                          return n != null ? n.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                                      },
                                      1.2f,
                                      "A306② `DeckInfoPopup.Local3`（`Buttons` 那一层的落位）"
                                    + " —— 改坏法：`DeckInfoPopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 偏"
                                    + " `M(M−1)×|基准相对被乘那一级的位移 (2,1.5)|` = 0.24×2.5 = **0.6 单位 = 65px**"
                                    + "（容差 0.02 = 2.2px）⇒ 红；⛔ 别把窗根挪走换绿（那是假绿）");
                    Object.DestroyImmediate(dipA.gameObject);
                    Object.DestroyImmediate(probeB);
                }
                SmallScreenUI.Set(false);
            }

            // 🔴 **2026-10-18 就地改判（铁律 5 · 覆盖 2026-10-17「`A1226` 表 B #40 · 布局组驱动 ⇒ 不可比」那段口径）**：
            //   那一版写「我们传 **44 × 58**（= 右栏父格）、⛔ 不为凑 22.87 改几何」—— **已过期**：
            //   `Shell/DeckInfoPopup.cs` 的 P1 批（2026-10-18）已把 `Count` 的框改成**宽 22.87、右沿 x+351.60**。
            //   本笔（新账 `A2133`）再把**高**从「整行 58」改成原版锚跨度：**0.86 × 58 = 49.88**。
            //   · **宽 22.87** = 原版 `LayoutElement.m_MinWidth`（MB `-8067466461408032344`）——
            //     ⚠️ 同 GO 的 `Count` RT（`-327133057421637208`）**`m_SizeDelta = (0,0)`** ⇒ 框宽由
            //     `Text fill` 的 `HorizontalLayoutGroup` 撑开 ⇒ **22.87 是下限、不是运行时框宽**。
            //     🔴 诊断另估「真布局宽 ≈ 83.16px」= **推导值、没实机量过** ⇒ 本笔**不据此改宽度**
            //     （已记进报告的「没查清」一节）。
            //   · **高 49.88** = 同一颗 RT 的 anchors y **0.07 → 0.93**（跨度 **0.86**）× 父行高 **58**
            //     （`m_SizeDelta.y = 0`）；`Count` 是 `Text fill` 的布局子件
            //     （`childControlHeight = 1 · childForceExpandHeight = 1`）⇒ 高 = 父内高 49.88。
            //     ⚠️ 改前我们传的是**整行 58**（断言也钉 58 ⇒ **假绿**）；节点仍居行心
            //     （锚跨度中心 = 0.5），实现同步传 `boxH: 49.88f`。
            //   夹具：库里的第 1 套本来「一张一名」⇒ 没有 `Count`（`n > 1` 才建）⇒ **临时多塞一张同 id**
            //   造那一行，断完即弃（不写盘、不改库里的对象 —— `Raw(0)` 返回的是副本的克隆）。
            {
                var a12Base = CollectionData.Raw(0);
                CheckTrue(a12Base != null && a12Base.CardIds.Count > 0,
                          "（前提）夹具卡组第 1 套有卡（要借它造一张重复 id）");
                if (a12Base != null && a12Base.CardIds.Count > 0)
                {
                    var a12Dup = a12Base.Clone();
                    a12Dup.CardIds.Add(a12Base.CardIds[0]);          // 同一 id ×2 ⇒ 该行建 `Count`
                    var a12Pop = DeckInfoPopup.CreateForPicked(win.Manager, -1, a12Dup,
                                                               CollectionData.DeckAt(0),
                                                               DeckInfoPopup.DeckInfoState.View,
                                                               true, "Select deck", null);
                    win.Manager.OpenWindow(a12Pop);
                    CheckTrue(a12Pop != null && a12Pop.CurrentState == WindowState.Open,
                              "（前提）重复 id 那张卡组详情窗开起来了（`Count` 只在 n > 1 时建）");
                    if (a12Pop != null)
                    {
                        CheckA1212(a12Pop.transform, "Count",
                                   "Deck Selector Card Info button ▸ Content/Text fill ▸ Count",
                                   2f, 32f, 36f, 0, boxW: 22.87f, boxH: 49.88f);
                        a12Pop.Close();
                        Object.DestroyImmediate(a12Pop.gameObject);
                    }
                }
            }

            Section("A327 · A306④：`PracticeModePopup.Local3` 的落位（关 = 逐值不变 / 开 = 设计点 × M）");
            {
                var pmpA = PracticeModePopup.Create(win.Manager);
                CheckTrue(pmpA != null, "（前提）`Practice Mode Menu` 建出来了");
                if (pmpA != null)
                {
                    // 同 A306②：M 加在**窗根的父级**探针根上，窗根留在它下面、只给它一个 (2,1.5) 的位移。
                    // ⚠️ 本窗 `extraScaleSmallScreen = 1.07`（原版实证）：态二那次 `TryOpen` 会照原版那段
                    //   往**窗根**上挂一颗 `TransformScalerBySmallScreenUI`（`menuScale = 1.07`）—— 但
                    //   批处理**没有帧循环**、产品路径只在 `LateUpdate` 里乘，本夹具也不推它 ⇒ 量到的仍是 1.0 那档。
                    var probeD = new GameObject("A327④ probe root");           // ← 被乘 M 的那一级
                    pmpA.transform.SetParent(probeD.transform, false);
                    pmpA.transform.localPosition = new Vector3(2f, 1.5f, 0f);
                    CheckTrue(pmpA.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                    // 🆕 **2026-10-17（`A1212` 块 2）**：本窗那 8 处的四格（判据 = `RA1212_切块表.md` 块 2，
                    //   原始读数出处 `R5_包装层菜单族.md` §2·H；⛔ 不读 `PracticeModePopup` 的常量）。
                    CheckA1212(pmpA.transform, "Selected Army Title", "Deck Selector ▸ Deck Buttons ▸ Selected Army Title",
                               1f, 36f, 23f, 1);
                    // ⚠️ `Deck Name` 在本窗有**两颗**（`Deck info` 那颗 + 卡组行那颗）⇒ 先从 `Deck info`
                    //   容器定位，⛔ 不用 `FindDeep(pmpA.transform, "Deck Name")` 拿「第一个同名」。
                    {
                        var infoB = FindDeep(pmpA.transform, "Deck info");
                        CheckTrue(infoB != null, "（前提）`Practice Mode Menu ▸ Deck info` 容器在");
                        if (infoB != null)
                        {
                            CheckA1212(infoB, "Deck Name", "Deck info ▸ Deck Name", 1f, 36f, 22.68f, 1);
                            CheckA1212(infoB, "Warlord Name", "Deck info ▸ Warlord Name", 1f, 35f, 24.3f, 1);
                        }
                    }
                    CheckA1212(pmpA.transform, "Deck Information Cost/balance text", "General container ▸ Cost/balance text",
                               1f, 34f, 22.3f, 1);
                    // 🔴 **2026-10-17（`A1226` 表 B #50）· 框已收口（第四十六会话）**：
                    //   原版 `… ▸ Generic Simplified UI Button ▸ Button Text` 的有效框 = **236.77 × 61.68**
                    //   （锚 `(0.0355112, 0.099)→(0.961262, 0.903)` × 父钮 `254.538 × 83.321` + `sd (1.135, 0)`；
                    //   `menu_dump --no-layout --no-ancestor-scale` 实读）；高 = ARF 宽控高 `236.77 / 3.83864 = 61.68`。
                    //   我们原来传**整颗钮** `254.54 × 83.33`（+17.77 / +21.65）⇒ `Shell/PracticeModePopup.cs` 的
                    //   `Txt` 新增 `boxW/boxH` 并传了原版值。下面这条框断言 = 判别式（⛔ 不读实现常量）。
                    CheckA1212(pmpA.transform, "Change Deck Text", "General container ▸ … ▸ Button Text",
                               10f, 45f, 12f, 0, boxW: 236.77f, boxH: 61.68f);
                    CheckA1212(pmpA.transform, "Back Text", "Back button ▸ Text", 1f, 45f, 31.9f, 1);
                    CheckA1212(pmpA.transform, "tooltip", "Deck Selector ▸ Deck Buttons ▸ tooltip",
                               1f, 38f, 23.2f, 1);
                    CheckA1212(pmpA.transform, "Battle Text", "DeckSelectionContinueButton ▸ Text",
                               18f, 45f, 36f, 1);
                    var selA = FindChild(pmpA.transform, "Army Selector");
                    CheckTrue(selA != null, "（前提）`Army Selector` 在（它是 `Local3(root, …)` 摆的）");
                    if (selA != null)
                        CheckScaleTwo(probeD, pmpA.transform,
                                      () =>
                                      {
                                          // 🔴 **2026-10-12（A437）**：态二必须先 `Close()` —— 理由与 A306② 那一处逐字相同
                                          //   （A217② 之后同窗再开走 `Open` 支、**早退不重建**；`Close()` 走生产链把 state
                                          //   送回 `Closed` ⇒ `TryOpen` 落回 `Closed` 支、重建照旧）。⛔ 别改成直调 `Open()`。
                                          pmpA.Close();               // ← A437：把 state 送回 `Closed`
                                          CheckTrue(pmpA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）
                                                    "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                                          var n = FindChild(pmpA.transform, "Army Selector");
                                          // （前提③）重建真的发生了 —— 改坏法：`measure` 改回纯读 ⇒ 这条红。
                                          CheckTrue(n != null && n != selA,
                                                    "（前提）…：态二的 measure **真的重建了** `Army Selector`"
                                                  + "（`Build()` 首句清空子件 ⇒ 拿到的是新节点）");
                                          if (n != null) selA = n;
                                          return n != null ? n.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                                      },
                                      1.2f,
                                      "A306④ `PracticeModePopup.Local3`（`Army Selector` 那一层的落位）"
                                    + " —— 改坏法：`PracticeModePopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 偏"
                                    + " `M(M−1)×|基准相对被乘那一级的位移 (2,1.5)|` = 0.24×2.5 = **0.6 单位 = 65px**"
                                    + "（容差 0.02 = 2.2px）⇒ 红；⛔ 别把窗根挪走换绿（那是假绿）");
                    Object.DestroyImmediate(pmpA.gameObject);
                    Object.DestroyImmediate(probeD);
                }
                SmallScreenUI.Set(false);
                SmallScreenUI.PersistOverride = ssPersistWas;   // `A1391`：放回**进场档**（⛔ 不再硬写 false）
                CheckTrue(!SmallScreenUI.Enabled, "（收尾）A327：自检跑完把开关放回**出厂值 关**");
            }

            // ======== 🆕 2026-10-13（A503）：`CollectionData` 的四个口在「写盘失败」时说得出话 ========
            //  判据 = **本项目红线**「⛔ 不许静默失败」（CLAUDE.md §三）+ `DeckLibrary` 侧已经补好的出口
            //  （A398：`Save()` 把原因写进 `LastError`、`SaveOrWarn` 出声、`Rename`/`Delete` 是 `return Save();`）。
            //  🔴 缺陷本来是：`Shell/CollectionData.cs` 那四个口**既不读返回值、也不读 `LastError`**，还各多调一次
            //  `Lib.Save()` ⇒ 写盘失败时玩家看到的是「成功」，而盘上一点没变。
            //  ⚠️ 这一段**必须留在 `Run()` 最后**：它会把 `OverridePath` 拐到坏路径、还会往内存里多塞几套卡组 ——
            //     前面那些断言（卡组数 / 列表格 / 开战链）全依赖 `Run()` 开头那个夹具（14 套 + 第 1 套合法 30 张）。
            Section("A503：`CollectionData` 四个口 —— **写盘失败不许说成「成功」**");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");

                // ---- ① 控制组：**好路径**下四个口都回真值（缺了它，一个「恒报失败」的实现照样绿）----
                string okCreate = CollectionData.CreateDeck();
                CheckTrue(!string.IsNullOrEmpty(okCreate),
                          $"（控制组）好路径 `CreateDeck` 回**名字**（「{okCreate}」）—— 恒返空串的实现 ⇒ 这条红");
                string okCopy = CollectionData.DuplicateDeck(0);
                CheckTrue(!string.IsNullOrEmpty(okCopy), $"（控制组）……`DuplicateDeck` 回名字（「{okCopy}」）");
                string okWhy;
                string goodExport = DeckLibrary.ExportString(CollectionData.Raw(0));   // 🆕 A1697：坏路径那一节要靠它把库垫起来
                string okImp = CollectionData.ImportDeck(goodExport, out okWhy);
                CheckTrue(!string.IsNullOrEmpty(okImp) && string.IsNullOrEmpty(okWhy),
                          $"（控制组）……`ImportDeck` 回名字（「{okImp}」）且 `why` 空（实得「{okWhy}」）");
                int c0 = CollectionData.DeckCount();
                CheckTrue(CollectionData.DeleteDeck(c0 - 1) && CollectionData.LastDeleteError.Length == 0,
                          "（控制组）……`DeleteDeck` 回 **true**，且 `LastDeleteError` 是**空串**（成功不报错）");

                // ---- ② 探针：**写不进去的路径**（`File.WriteAllText` 抛 `DirectoryNotFoundException`）----
                DeckStore.OverridePath = Path.Combine(probeDir, "__wf_a503_no_such_dir__", "x.json");
                CheckTrue(!DeckLibrary.Load().Save(),
                          "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                CollectionData.ResetForTest();     // 丢掉缓存 ⇒ 重新 `Load()`（坏路径读不到 ⇒ 空库，**不抛异常**）
                Check(CollectionData.DeckCount(), 0, "（前提）坏路径下起手是**空库**（「读不到」不是「读失败」）");

                // 🔴 **2026-10-12（`A1697`）改判**：`CreateDeck` 走原版那条路（**只造、不入库、不落盘**）
                //   ⇒ 它**不再属于「会写盘的那几个口」**，原来那两条断言（回空串 / 内存里建了 1 套）**整条作废**。
                //   新口径已由上面那一节（点 `Create`）钉住：库不变 + `PendingNewDeck` 有值。
                //   本节改用 `ImportDeck` 把库垫起来 —— 它**仍入库、仍会写盘** ⇒ 正好当「写盘失败」的探针。
                string why0;
                string badCreate = CollectionData.ImportDeck(goodExport, out why0);
                CheckTrue(string.IsNullOrEmpty(badCreate),
                          "★ A503：写盘失败 ⇒ `ImportDeck` 回**空串**（本条现在是本节的起点）");
                Check(CollectionData.DeckCount(), 1,
                      "★ A503：……而它**真的建在内存里**了（1 套）—— 本条说的「失败」专指**没落盘**（内存建了、盘上没有）");

                string badCopy = CollectionData.DuplicateDeck(0);
                CheckTrue(string.IsNullOrEmpty(badCopy),
                          "★ A503：写盘失败 ⇒ `DuplicateDeck` 回**空串**（改坏法：删掉它那个 `Lib.LastError` 支 ⇒ 红）");

                string why;
                string badImp = CollectionData.ImportDeck(DeckLibrary.ExportString(CollectionData.Raw(0)), out why);
                CheckTrue(string.IsNullOrEmpty(badImp),
                          "★ A503：写盘失败 ⇒ `ImportDeck` 回**空串**（改坏法：删掉它那个 `Lib.LastError` 支 ⇒ 红）");
                // 🔴 **2026-10-18（波 2a · W2）改「随语档」**：`why` 那两句从波 1b 起走词条
                //   （`Shell/CollectionData.ImportDeck` → `Loc.T("MenuDeck/Error/ImportEmpty")` /
                //   `Loc.T("MenuDeck/Error/ImportBadString")`）⇒ 原来写死的那两句中文**只是中文列的副本**，
                //   宿主一切英文档这条就**假绿**（第三句与英文的短键本来也不等）。
                //   期望值换成 `Loc.T(键)` —— 负判据照样成立（两语档下第三句都与那两个短键不同）。
                CheckTrue(Loc.HasEntry("MenuDeck/Error/ImportEmpty")
                          && Loc.HasEntry("MenuDeck/Error/ImportBadString"),
                          "（前提）词条 `MenuDeck/Error/Import{Empty,BadString}` 都在表里（⛔ 不在 ⇒ 下一条恒真）");
                CheckTrue(!string.IsNullOrEmpty(why)
                          && why != Loc.T("MenuDeck/Error/ImportEmpty")
                          && why != Loc.T("MenuDeck/Error/ImportBadString"),
                          "★ A503：……而且 `why` 给的是**第三种人话**（串本身没问题，是**没写进存档**；实测「"
                        + why + "」）—— 改坏法：把 `why = \"卡组串读出来了…\"` 那一行删掉 ⇒ 这条红"
                        + "（两个负判据**随语档**，键 `MenuDeck/Error/Import{Empty,BadString}`）");

                // ---- ③ 「没删」vs「删了没落盘」：**分开报**（A503 验收第 2 条）----
                int n1 = CollectionData.DeckCount();          // = 3：上面建 1 + 复制 1 + 导入 1
                bool delRange = CollectionData.DeleteDeck(99);
                string e1 = CollectionData.LastDeleteError;
                CheckTrue(!delRange, "★ A503：下标越界 ⇒ 回 false");
                Check(CollectionData.DeckCount(), n1, "★ A503：……而且**一套都没少**（「没删」这一种）");
                bool delSave = CollectionData.DeleteDeck(0);
                string e2 = CollectionData.LastDeleteError;
                CheckTrue(!delSave,
                          "★ A503：删了、但**没落盘** ⇒ 同样回 false（`DeckLibrary.Delete` 就是 `return Save();`）");
                Check(CollectionData.DeckCount(), n1 - 1, "★ A503：……但内存里**确实少了一套**（「删了没落盘」这一种）");
                CheckTrue(e1.Length > 0 && e2.Length > 0 && e1 != e2,
                          "★ A503：**两种失败报的是两句不同的话**（改坏法：把 `CollectionData.DeleteDeck` 里那两支"
                        + "合成一句「删不了」⇒ 这条红 —— 那正是原来的缺陷）"
                        + "\n              ①（没删）「" + e1 + "」\n              ②（删了没落盘）「" + e2 + "」");
                CheckTrue(e1.Contains("没删") && e2.Contains("没写进存档"),
                          "★ A503：……而且各自点明是**哪一种**（① 含「没删」· ② 含「没写进存档」）");

                // ---- ④ 收尾：路径还回去（⛔ 绝不碰玩家的真存档）+ 丢掉这一段弄脏的缓存 ----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");
            }

            // ======== 🆕 2026-10-13（A600 · A601）：`Select` 的出口 + `DeckInfoPopup` 删/复制的失败文案 ========
            //  **判据** = **本项目红线**「⛔ 不许静默失败」+ **A503 在 `CollectionData` 上刚定下的那套出口形状**
            //    （`bool` 返回值 + `LastXxxError` + `Debug.LogWarning` —— 见 `Shell/CollectionData.cs` 的 `DeleteDeck`；
            //     ⛔ 本件**没另立一套**）。⚠️ 原版**没有对应物**：原版卡组存在**服务器**上
            //    （`CardDeck.syncedToServer` / `deckId`，见 `RuleEngine/Data/DeckLibrary.cs` 文件头）⇒ 本地这条链无原版判据。
            //
            //  **A600 的缺陷原文**：`CollectionData.Select(i)` 原来是 `Lib.Select(i); Lib.Save();` —— 返回值与
            //    `LastError` **都不读**；而 `BattleDriver.PickSavedDeck` 是**从磁盘重读**的（`CollectionData` 自己
            //    在 `Select` 里写着这条因果）⇒ 写盘失败时「你选的那一套」**静默失效**、**战斗会拿磁盘上那套旧的**，
            //    而七个调用点（`DeckInfoPopup` / `CollectionWindow` / `PracticeModePopup` / `LiveOpsEventWindow`
            //    ×2 / `MainMenuScene` / `CollectionScene`）还紧接着打「已选中」。
            //  **A601**：`Shell/DeckInfoPopup.cs` 那三处（A548 刚改完的文案）当时**只有类型检查兜着**、一条断言都没有；
            //    要咬的是「**删失败 / 复制失败各自的两支，文案分得清是哪一支**」（⛔ 弱断言分不出两种状态）。
            //
            //  ⚠️ 这一段**必须留在 `Run()` 最后**（同 A503 那节的理由）：它会把 `OverridePath` 拐走、还会增删内存里的卡组。
            var grabbed = new List<string>();

            // 点一颗钮、抓住这一下产生的**全部**日志（判据范式 = 本文件 `本文件` 的 A229 那一段 +
            //   `Editor/BattleScene.cs` 的 `Debug.Log`：批处理里 `Debug.Log` 照样走 `Application.logMessageReceived`）。
            void ClickHit(Transform hit)
            {
                grabbed.Clear();
                Application.LogCallback h = (c, s, t) => { if (c != null) grabbed.Add(c); };
                Application.logMessageReceived += h;
                if (hit != null) { var wb = hit.GetComponentInChildren<WindowButton>(true); if (wb != null) wb.ClickForTest(); }   // 🔴 **2026-10-26（C1）**：`GetComponent` → **`GetComponentInChildren`** —— `PopUpGameWindow.MakeButton` 把 `WindowButton` 挂在节点的**子件 `Image`** 上（`MenuDraw.Node("ButtonLeft")` → `MenuDraw.Rect(…, "Image")` ← 才挂 `WindowButton`）⇒ 只查自己那一层永远取不到，**确认窗左钮点了等于没点**（K3 的「删确认窗 → 点左钮」那一整段 9 条红就是这么来的）。
                Application.logMessageReceived -= h;
            }
            int HitCount(string contains) { int k = 0; foreach (var l in grabbed) if (l.Contains(contains)) k++; return k; }
            string FirstHit(string contains) { foreach (var l in grabbed) if (l.Contains(contains)) return l; return null; }

            // ---------------- ① A600：`CollectionData.Select` 的出口（控制组 + 两个探针 + 调用点） ----------------
            Section("A600：`CollectionData.Select` 的落盘结果**有人看了**（出口 + 调用点）");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");

                int n0 = CollectionData.DeckCount();
                CheckTrue(n0 >= 2, $"（前提）库里 **≥ 2** 套（实得 {n0}）—— 下面要挑一个「不是现在选中的」下标");

                // ---- ①-a 控制组（好路径）：选中**成功**那一支 ----
                //   缺了它，一个「恒返 false」的实现照样能把下面两条探针全骗绿。
                int tgt = CollectionData.CurrentIndex() == 0 ? n0 - 1 : 0;
                bool okSel = CollectionData.Select(tgt);
                CheckTrue(okSel, $"（控制组）好路径 `Select({tgt})` 回 **true**"
                               + "（改坏法：把 `Select` 写成恒返 false ⇒ 这条红）");
                CheckTrue(CollectionData.LastSelectError.Length == 0,
                          "（控制组）……而且 `LastSelectError` 是**空串**（成功不报错）—— "
                        + "改坏法：成功那一支也写错误 ⇒ 这条红");
                Check(CollectionData.CurrentIndex(), tgt, "（控制组）……而且当前选中那一套**真的是它**");

                // ---- ①-b 探针一：**下标越界**（`DeckLibrary.Select` 早退、**根本不碰盘** ⇒ 就在好路径上量）----
                int keepCur = CollectionData.CurrentIndex();
                bool badRange = CollectionData.Select(n0 + 5);
                string eRange = CollectionData.LastSelectError;
                CheckTrue(!badRange, "★ A600：下标越界 ⇒ 回 **false**"
                                   + "（改坏法：删掉 `Select` 里那句 `Lib.CurrentIndex != i` 判断 ⇒ 这条红）");
                CheckTrue(eRange.Length > 0 && eRange.Contains("没选中"),
                          "★ A600：……而且报的是「**没选中**」那一支（实得「" + eRange + "」）");
                Check(CollectionData.CurrentIndex(), keepCur, "★ A600：……而且**当前选中一动没动**（越界那一支是早退）");

                // ---- ①-c 探针二：**写盘失败**（父目录不存在 ⇒ `File.WriteAllText` 抛 `DirectoryNotFoundException`）----
                string probePath = Path.Combine(probeDir, "__wf_a600_no_such_dir__", "x.json");
                DeckStore.OverridePath = probePath;
                var probeLib = DeckLibrary.Load();
                CheckTrue(!probeLib.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                string expectSaveErr = probeLib.LastError;
                CheckTrue(!string.IsNullOrEmpty(expectSaveErr),
                          "（前提）写不进去时**带了原因**（原因空 ⇒ 下面「报的是真原因」那条会**空串恒真**）");

                int saveTgt = CollectionData.CurrentIndex() == 0 ? 1 : 0;
                bool badSave = CollectionData.Select(saveTgt);
                string eSave = CollectionData.LastSelectError;
                CheckTrue(!badSave, "★ A600：写盘失败 ⇒ 回 **false**"
                                  + "（改坏法：退回裸 `Lib.Save();`、不读返回值 ⇒ 这条红 —— **那正是原来的缺陷**）");
                CheckTrue(eSave.Contains("没写进存档"),
                          "★ A600：……而且说的是「**没写进存档**」那一支（实得「" + eSave + "」）");
                CheckTrue(eSave.Contains(expectSaveErr),
                          "★ A600：……而且报的是**真原因**（`DeckStore.SaveAll` 那条 `catch` 的**运行时原话**"
                        + "「" + expectSaveErr + "」—— ⛔ 不是我们写死的一句文案）");
                Check(CollectionData.CurrentIndex(), saveTgt,
                      "★ A600：……而内存里**真的换了**那一套 —— 本条说的「失败」专指**没落盘**"
                    + "（内存改了、盘上没改），与 A503 那一节**同一条语义锚**；"
                    + "改坏法：把失败做成「回滚内存里的 `_current`」⇒ 这条红");
                CheckTrue(eRange != eSave, "★ A600：两种失败报的是**两句不同的话**（合成一句「选不了」⇒ 这条红）"
                                         + "\n              ①（没选中）「" + eRange + "」"
                                         + "\n              ②（没落盘）「" + eSave + "」");

                // ---- ①-d 调用点：`DeckInfoPopup` 的 `Select Deck` **真的读了返回值** ----
                //   ⛔ 不是自证：正例与反例**用同一份实现**、只翻「路径」与「`DeckIndex`」两个输入；
                //     反例要求日志里出现 `LastSelectError` 的**原话**（运行时值，不是我们源码里的常量）。
                DeckStore.OverridePath = keepPath;          // 先把路径还回去（正例要在**好路径**上跑）
                var pSelOk = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.Edit, true);
                win.Manager.OpenWindow(pSelOk);
                // 🆕 **2026-10-13（A646）**：上面那个 `ClickHit` 抓的是**文案**、把**级别**丢了 ⇒ 这一节另配一份
                //   **带级别**的抓取，而且**好路径与失败路径两态都抓** —— 只抓一边分不出「抓取器根本不认级别」
                //   与「级别真的对」（弱断言）。判据 = 「失败走警告级」是 A610/A611 之后本工程统一的口径
                //   （`Shell/DeckInfoPopup.cs` 里 `Blocked()` / 取不到图 / 没登记过这颗钮 一律 `LogWarning`）
                //   ⇒ 期望值是**级别枚举名**（`LogType.Log` / `LogType.Warning`），不是我们自己的常量。
                var okLv = new List<string>();
                Application.LogCallback hOkLv = (c, st2, t) => { if (c != null && c.Contains("[DeckInfo] 已选中「")) okLv.Add(t.ToString()); };
                Application.logMessageReceived += hOkLv;
                ClickHit(pSelOk.Btn("Select Deck"));
                Application.logMessageReceived -= hOkLv;
                CheckTrue(HitCount("[DeckInfo] 已选中「") == 1 && HitCount("[DeckInfo] 选中卡组失败：") == 0,
                          "（控制组）好路径点 `Select Deck` ⇒ 「已选中『…』」**且没有**失败那一句");

                DeckStore.OverridePath = probePath;
                var pSelBad = DeckInfoPopup.Create(win.Manager, 0, DeckInfoPopup.DeckInfoState.Edit, true);
                win.Manager.OpenWindow(pSelBad);
                if (pSelBad != null) pSelBad.DeckIndex = n0 + 7;   // ⚠️ 开完窗**之后**才改（建窗/接线跑的是有效下标那一次）
                var selLv = new List<string>();
                Application.LogCallback hSelLv = (c, st2, t) => { if (c != null && c.Contains("[DeckInfo] 选中卡组失败：")) selLv.Add(t.ToString()); };
                Application.logMessageReceived += hSelLv;
                ClickHit(pSelBad != null ? pSelBad.Btn("Select Deck") : null);
                Application.logMessageReceived -= hSelLv;
                Check(HitCount("[DeckInfo] 选中卡组失败："), 1,
                      "★ A600：调用点失败时**出声**（改坏法：退回 `CollectionData.Select(DeckIndex); "
                    + "Debug.Log(\"已选中…\")` ⇒ 这条与下一条**一起红**）");
                string selLog = FirstHit("[DeckInfo] 选中卡组失败：");
                CheckTrue(selLog != null && CollectionData.LastSelectError.Length > 0
                          && selLog.Contains(CollectionData.LastSelectError),
                          "★ A600：……而且打的是 `CollectionData.LastSelectError` 的**原话**"
                        + "（⛔ 不是另写一句 —— 两处写同一条规则 = 迟早不一致）\n              实得：「" + selLog + "」");
                Check(HitCount("[DeckInfo] 已选中「"), 0,
                      "★ A600：……而**不许**再说「已选中」（两句互斥 —— 弱断言分不出这两种状态）");

                // ---- ①-e 收尾 ----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");

                // ---- ①-f 🆕 **2026-10-13（A646）**：**日志的级别**（原来这一句是 `Debug.Log`） ----
                //   ⚠️ 放在**收尾之后**：它只读上面抓下来的 `okLv` / `selLv` 两个表，**不碰任何状态**
                //     （⛔ 别把抓取那两段挪到这儿来 —— 那必须在点钮那一刻挂着）。
                //   A646 的缺陷原文：A611 之后 `Shell/DeckInfoPopup.cs` 里只剩这一处失败走**普通级** ——
                //   它当年用 `Log` 的理由是「与 A548 那两句（删/复制失败）**同通道**」，而 **A610 已把那两句
                //   统一成 `LogWarning`** ⇒ 那条理由失效。⛔ 两条期望值都不是我们自己的常量（`LogType` 枚举）。
                //   **改坏法**：把 `Shell/DeckInfoPopup.cs` 那句改回 `Debug.Log` ⇒ ② 的实得变成「Log」⇒ 红。
                //   ① 是**对照档**：它同时证明抓取器**认得出级别**（否则 ② 是恒绿的假观测）。
                CheckTrue(okLv.Count == 1 && okLv[0] == "Log",
                          "★★ A646（对照档）：**成功**那一句仍是 `Log` 级（⛔ A646 只动失败那一句）"
                        + " —— 实得「" + (okLv.Count == 1 ? okLv[0] : "(没抓到 / 抓到 " + okLv.Count + " 条)") + "」"
                        + "；这一档同时证明抓取器**认得出级别**（不是「恒 Warning」那种假绿）");
                CheckTrue(selLv.Count == 1 && selLv[0] == "Warning",
                          "★★ A646：`Select Deck` **失败走警告级**（`Debug.LogWarning`）"
                        + " —— 实得「" + (selLv.Count == 1 ? selLv[0] : "(没抓到 / 抓到 " + selLv.Count + " 条)") + "」"
                        + "；改回 `Debug.Log` ⇒ 实得「Log」⇒ 红");
            }

            // ---------------- ② A601：`DeckInfoPopup` 的删 / 复制失败 —— 两支各自说得清是哪一支 ----------------
            Section("A601（A548）：`DeckInfoPopup` 的删 / 复制失败 —— **两支各自说得清是哪一支**");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");
                int n0 = CollectionData.DeckCount();
                CheckTrue(n0 >= 2 && n0 < DeckInfoPopup.MaxCustomDecks,
                          $"（前提）库里 **2 ≤ 套数 < 上限 114**（实得 {n0}）—— `Delete` / `Duplicate` 两颗钮的 "
                        + "`interactable` 才为真（原版 `1 < 卡组数` / `卡组数 < totalCustomDecks`）");

                // ---- ②-a 控制组（好路径）：两支**成功**的日志 ----
                //   缺了它，一个「恒报失败」的实现照样能把下面四条探针全骗绿。
                {
                    // 🔴 **2026-10-12（`A1697`）**：`CreateDeck` 现在**只造不入库** ⇒ 拿不到「库里那一套」当靶子；
                    //   改用 `DuplicateDeck(0)` —— 那个口**仍入库**。本节要验的是「靶子那扇窗的两颗钮可点」，
                    //   与「靶子怎么造出来的」无关。
                    string tmp = CollectionData.DuplicateDeck(0);
                    int ti = CollectionData.IndexOf(tmp);
                    CheckTrue(ti >= 0, $"（控制组）先复制一套当靶子（「{tmp}」@{ti}）");

                    var pOk = DeckInfoPopup.Create(win.Manager, ti);
                    win.Manager.OpenWindow(pOk);
                    CheckTrue(pOk.DeleteInteractable && pOk.DuplicateInteractable,
                              "（控制组）靶子那扇窗的 `Delete` / `Duplicate` **都可点**"
                            + "（原版 `1 < 卡组数` / `卡组数 < totalCustomDecks`）");

                    int beforeDup = CollectionData.DeckCount();
                    ClickHit(pOk.Opt("Duplicate"));
                    CheckTrue(HitCount("[DeckInfo] 已复制成「") == 1 && HitCount("[DeckInfo] 复制失败：") == 0,
                              "（控制组）好路径点 `Duplicate` ⇒ 「已复制成『…』」**且没有**「复制失败」"
                            + "（改坏法：把那一支写成恒报失败 ⇒ 这条红）");
                    Check(CollectionData.DeckCount(), beforeDup + 1, "（控制组）……而且库里**真多了一套**");

                    int di = CollectionData.DeckCount() - 1;          // = 刚复制出来那套
                    var pDel = DeckInfoPopup.Create(win.Manager, di);
                    win.Manager.OpenWindow(pDel);
                    int beforeDel = CollectionData.DeckCount();
                    ClickDeleteAndConfirm(pDel, ClickHit);      // 🆕 K3：先弹确认窗、再点左钮（原版就是两拍）
                    CheckTrue(HitCount("[DeckInfo] 已删除该卡组") == 1 && HitCount("[DeckInfo] 删卡组失败：") == 0,
                              "（控制组）好路径点 `Delete` ⇒ 「已删除该卡组」**且没有**「删卡组失败」"
                            + "（改坏法：把那一支写成恒报失败 ⇒ 这条红）");
                    Check(CollectionData.DeckCount(), beforeDel - 1, "（控制组）……而且库里**真少了一套**");
                }

                // ---- ②-b 探针（**写不进去的路径**）：同一颗钮的**两支失败**各打一句、且两句不同 ----
                //  判据 = `Shell/DeckInfoPopup.cs` 的 `OnOption`：删失败**直接打** `LastDeleteError`
                //   （它自己分成「没删」与「删了没落盘」两种）；复制失败按**调用前后 `DeckCount()` 变没变**分两支
                //   （与 `CollectionData.DeleteDeck` 分开它那两种 `false` 用的是**同一条**判据 ⇒ 不是新造的一套）。
                DeckStore.OverridePath = Path.Combine(probeDir, "__wf_a601_no_such_dir__", "x.json");
                var probeLib2 = DeckLibrary.Load();
                CheckTrue(!probeLib2.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                string expectSaveErr2 = probeLib2.LastError;
                CheckTrue(!string.IsNullOrEmpty(expectSaveErr2),
                          "（前提）写不进去时**带了原因**（原因空 ⇒ 下面「带上了真原因」那条会**空串恒真**）");

                // ②-b-1 删除 · 「**没删**（下标越界）」那一支
                int nBefore = CollectionData.DeckCount();
                var d1 = DeckInfoPopup.Create(win.Manager, 0);     // ⚠️ 先用**有效下标**开窗（建窗/接线要读到一套真的）
                win.Manager.OpenWindow(d1);
                if (d1 != null) d1.DeckIndex = n0 + 7;              // 开完再改成越界 —— 模拟「下标已失效」那一态
                ClickDeleteAndConfirm(d1, ClickHit);                          // 🆕 K3：确认窗 → 左钮（越界 ⇒ 失败支）
                string del1 = FirstHit("[DeckInfo] 删卡组失败：");
                CheckTrue(del1 != null, "★ A601：删失败时**出声**（`[DeckInfo] 删卡组失败：…`）");
                CheckTrue(del1 != null && del1.Contains("没删") && !del1.Contains("没写进存档"),
                          "★ A601（删除 · 越界支）：文案点明是「**没删**」、且**不许**说「没写进存档」"
                        + "\n              实得：「" + del1 + "」");
                Check(CollectionData.DeckCount(), nBefore, "★ A601：……而且**一套都没少**（越界那一支是早退）");

                // ②-b-2 删除 · 「**删了，但没写进存档**」那一支
                var d2 = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(d2);
                ClickDeleteAndConfirm(d2, ClickHit);                          // 🆕 K3：确认窗 → 左钮（没落盘支）
                string del2 = FirstHit("[DeckInfo] 删卡组失败：");
                CheckTrue(del2 != null && del2.Contains("没写进存档") && !del2.Contains("越界"),
                          "★ A601（删除 · 没落盘支）：文案点明是「**没写进存档**」、且**不许**说「越界」"
                        + "\n              实得：「" + del2 + "」");
                CheckTrue(del2 != null && del2.Contains(expectSaveErr2),
                          "★ A601：……而且**带上了真原因**（`DeckStore.SaveAll` 那条 `catch` 的**运行时原话**"
                        + "「" + expectSaveErr2 + "」—— 改坏法：把 `LastDeleteError` 换成写死的文案 ⇒ 这条红）");
                Check(CollectionData.DeckCount(), nBefore, "★ A601：……而内存里**回插了一套**（`DeletedNotSaved` ⇒ 走的是原版 `InventoryManager__Add` 那条退回，重启不会多也不会少；两种失败仍由上面两句文案 + 下面 `del1 != del2` 分得开）");
                CheckTrue(del1 != del2,
                          "★ A601：**删失败的两支报的是两句不同的话**"
                        + "（改坏法：退回旧那句「删不了（`DeckLibrary.Delete` 的规矩：只剩一套时不许删 / 下标越界）」"
                        + "⇒ 这条红 —— 那一句**在两种情况下都打**，正是原来的缺陷）");

                // ②-b-3 复制 · 「**没复制**（下标越界）」那一支
                int cBefore = CollectionData.DeckCount();
                var u1 = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(u1);
                if (u1 != null) u1.DeckIndex = n0 + 7;
                ClickHit(u1 != null ? u1.Opt("Duplicate") : null);
                string dup1 = FirstHit("[DeckInfo] 复制失败：");
                CheckTrue(dup1 != null, "★ A601：复制失败时**出声**（`[DeckInfo] 复制失败：…`）");
                CheckTrue(dup1 != null && dup1.Contains("没复制") && !dup1.Contains("没写进存档"),
                          "★ A601（复制 · 越界支）：文案点明「**没复制**」、且**不许**说「没写进存档」"
                        + "\n              实得：「" + dup1 + "」");
                Check(CollectionData.DeckCount(), cBefore, "★ A601：……而且库里**一套没多**");

                // ②-b-4 复制 · 「**复制出来了，但没写进存档**」那一支
                var u2 = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(u2);
                ClickHit(u2 != null ? u2.Opt("Duplicate") : null);
                string dup2 = FirstHit("[DeckInfo] 复制失败：");
                CheckTrue(dup2 != null && dup2.Contains("没写进存档") && !dup2.Contains("越界"),
                          "★ A601（复制 · 没落盘支）：文案点明「**没写进存档**」、且**不许**说「越界」"
                        + "\n              实得：「" + dup2 + "」");
                Check(CollectionData.DeckCount(), cBefore + 1, "★ A601：……而内存里**确实多了一套**");
                CheckTrue(dup1 != dup2,
                          "★ A601：**复制失败的两支报的是两句不同的话**"
                        + "（改坏法：退回旧那句「复制失败（`DeckLibrary.Duplicate` 返回空）」—— 那一句把原因"
                        + "**一律归给「返回空」**，两种情况下都打 ⇒ 这条红 —— 正是原来的缺陷）");

                // ---- ②-c 收尾 ----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");
            }

            // ======== 🆕 2026-10-13（A610 · A611）：`DuplicateDeck` 的出口 + 失败日志**是警告级** ========
            //  **判据**：① `资料/普查产出_1013/WSmall1_Deck两尾巴.md` §五·5（A611 的原始出处：A548 当时
            //    `CollectionData.Lib` 是私有的 ⇒ 「复制失败」那一支只能拿**「调用前后 `DeckCount()` 变没变」**
            //    这个**间接判据**去分两种成因 —— 那等于把同一条规则**写了第二份**）·
            //    ② `资料/普查产出_1013/WSmall3_选中断言与两条尾巴.md` §六·2（A610）/ §六·3（「更干净的做法」）·
            //    ③ **成品范本 = `Shell/CollectionData.cs` 现读的 `LastDeleteError`**（A503 定的形状：
            //    开头清 → 按**观测状态**分两支 → `LastXxxError` + 一条 `Debug.LogWarning`）。
            //  **A611 改了**：`CollectionData.DuplicateDeck` 补 `LastDuplicateError`；调用点
            //    （`Shell/DeckInfoPopup.cs` 的 `OnOption`）**直接打它的原话**，`DeckCount()` 那个间接判据**已删**。
            //  **A610 改了**：`OnOption` 里那三支失败（删失败 1 + 复制失败 2，A611 后并成 2 句）从 `Debug.Log`
            //    **统一成 `Debug.LogWarning`**（失败本来就是警告级：`CollectionData` 自己的失败 / `Blocked()` /
            //    `WindowButton.Click`（`Shell/WindowButton.cs`；⚠️ 2026-10-19 订正：原写 `PromptPopup.Click` —— `PromptPopup` 类没有 `Click`）全是 `LogWarning`）。⛔ **成功那两句不动**（仍是 `Debug.Log`）。
            //  ⚠️ **两件都**没有原版判据：原版卡组存在**服务器**上（`CardDeck.syncedToServer`，见
            //    `RuleEngine/Data/DeckLibrary.cs` 文件头）⇒ 本地这条链没有原版对应物；判据 = 本项目红线
            //    「⛔ 不许静默失败」+ A503 已经定下的那套口径。⛔ 本件没另立一套。
            //
            //  ⚠️ **这一段必须留在 `Run()` 最后**（同 A503/A600 那两节的理由）：它会拐走 `OverridePath`、
            //    还会增删内存里的卡组。⚠️ **不碰夹具那条存档本身** —— 控制组把它**复制一份**到
            //    `_wf_a611_probe.json`（同一目录），所有**成功**的写盘都落在副本上。
            Section("A610 + A611：`DuplicateDeck` 的出口（失败两支分得清）+ 失败日志**是警告级**");
            {
                string keepPath = DeckStore.OverridePath;
                string probeDir = Path.GetDirectoryName(keepPath);
                CheckTrue(!string.IsNullOrEmpty(probeDir) && Directory.Exists(probeDir),
                          $"（前提）探针要用的目录存在（{probeDir}）—— 不存在 ⇒ 下面全是假绿");
                CheckTrue(!string.IsNullOrEmpty(keepPath) && File.Exists(keepPath),
                          $"（前提）夹具那条存档在（{keepPath}）—— 控制组要把它**复制一份**，⛔ 不碰它本身");

                // 日志**连级别一起**抓（本节 A610 那几条要断的就是级别）。
                //   ⚠️ 与上面 A600/A601 两节那份 `ClickHit`（只抓正文）**并存**：那一份不在本件白名单内，
                //      ⛔ 一行都不动；两份同时挂着不影响（`ClickHit` 自己加、自己摘）。
                var lvMsg = new List<string>();
                var lvLevel = new List<LogType>();
                Application.LogCallback hLv = (c, s, t) => { if (c != null) { lvMsg.Add(c); lvLevel.Add(t); } };
                Application.logMessageReceived += hLv;
                // 只数**这一次点击之后**新产生的那几条（`mark` = 点之前的条数）
                int LvCount(int mark, string contains, LogType ty)
                {
                    int k = 0;
                    for (int i = mark; i < lvMsg.Count; i++)
                        if (lvLevel[i] == ty && lvMsg[i].Contains(contains)) k++;
                    return k;
                }
                string LvFirst(int mark, string contains)
                {
                    for (int i = mark; i < lvMsg.Count; i++) if (lvMsg[i].Contains(contains)) return lvMsg[i];
                    return null;
                }

                // ---- ① 控制组（好路径）：**直接调出口** —— 挡住「恒报失败 / 恒回空串」那种坏实现 ----
                string goodPath = Path.Combine(probeDir, "_wf_a611_probe.json");
                File.Copy(keepPath, goodPath, true);        // 夹具的**副本**：成功的写盘全落它身上
                DeckStore.OverridePath = goodPath;
                CollectionData.ResetForTest();              // 从副本重读（内容与夹具一致）

                int n0 = CollectionData.DeckCount();
                CheckTrue(n0 >= 1 && n0 < DeckInfoPopup.MaxCustomDecks,
                          $"（前提）副本里 **1 ≤ 套数 < 上限 114**（实得 {n0}）—— `Duplicate` 那颗钮的 "
                        + "`interactable`（原版 `卡组数 < totalCustomDecks`）要为真，否则下面点了不生效");

                int c0 = CollectionData.DeckCount();
                string nmOk = CollectionData.DuplicateDeck(0);
                CheckTrue(!string.IsNullOrEmpty(nmOk),
                          "（控制组）好路径 `DuplicateDeck(0)` 回**新卡组名**（「" + nmOk + "」）—— "
                        + "改坏法：把成功那一支写成恒回空串 ⇒ 这条红");
                CheckTrue(CollectionData.LastDuplicateError.Length == 0,
                          "（控制组）……而且 `LastDuplicateError` 是**空串**（成功不报错）—— "
                        + "改坏法：成功那一支也写错误 ⇒ 这条红");
                Check(CollectionData.DeckCount(), c0 + 1, "（控制组）……而且库里**真多了一套**");
                CheckTrue(File.Exists(goodPath),
                          "（控制组）……而且**真写进了那个探针文件**（" + goodPath + "）—— "
                        + "这条钉住「好路径确实写得进去」，否则上面几条可能是假绿");

                // ---- ② 控制组（好路径）：**走调用点** —— 「成功」那一句**仍然是 `Log`**（A610 只动失败那几支）----
                var pOk = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pOk);
                CheckTrue(pOk != null && pOk.DuplicateInteractable,
                          "（前提）那扇窗的 `Duplicate` **可点**（原版 `卡组数 < totalCustomDecks`；"
                        + "不可点的话下面这一下会被 `Blocked` 拦掉 = 什么都不发生）");
                int c1 = CollectionData.DeckCount();
                int m1 = lvMsg.Count;
                ClickHit(pOk.Opt("Duplicate"));
                CheckTrue(LvCount(m1, "[DeckInfo] 已复制成「", LogType.Log) == 1
                          && LvCount(m1, "[DeckInfo] 复制失败：", LogType.Warning) == 0,
                          "（控制组）好路径点 `Duplicate` ⇒ 「已复制成『…』」**恰好一条、且级别仍是 `Log`**"
                        + "（⛔ A610 只把**失败**改成警告级：把成功那句也改成 `LogWarning` ⇒ 这条红）"
                        + "，且**没有**「复制失败」（那一支写成恒报失败 ⇒ 这条红）");
                Check(CollectionData.DeckCount(), c1 + 1, "（控制组）……而且库里**真多了一套**");

                // ---- ③ A610：**删失败**那一支的级别（越界那一支不碰盘 ⇒ 在好路径上量，零副作用）----
                var pDel = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pDel);
                if (pDel != null) pDel.DeckIndex = CollectionData.DeckCount() + 7;   // 开完再改 = 模拟「下标已失效」
                int c2 = CollectionData.DeckCount();
                int m2 = lvMsg.Count;
                ClickDeleteAndConfirm(pDel, ClickHit);                        // 🆕 K3：确认窗 → 左钮（量「删失败」那一句的级别）
                CheckTrue(LvCount(m2, "[DeckInfo] 删卡组失败：", LogType.Warning) == 1,
                          "★ A610：删失败那一句**是警告级**（`Debug.LogWarning`）—— "
                        + "改坏法：退回 `Debug.Log`（A548 当时的写法）⇒ 这条红");
                Check(LvCount(m2, "[DeckInfo] 删卡组失败：", LogType.Log), 0,
                      "★ A610：……而且**不是**普通级（同一句话的级别两种状态互斥，弱断言分不出）");
                Check(CollectionData.DeckCount(), c2, "★ A610：……而越界那一支**一套都没少**（早退，根本没碰盘）");

                // ---- ④ 探针一：**下标越界**（`DeckLibrary.Duplicate` 早退、根本不碰盘 ⇒ 也在好路径上量）----
                int c3 = CollectionData.DeckCount();
                int bad = c3 + 7;
                string rBad = CollectionData.DuplicateDeck(bad);
                string eRange = CollectionData.LastDuplicateError;
                CheckTrue(rBad.Length == 0,
                          "★ A611：下标越界 ⇒ `DuplicateDeck` 回**空串**"
                        + "（🔴 A611 **之前**也是回空串 —— 这条是**前提**、不是新增的能力："
                        + "原来缺的是「空串是哪一种」）");
                CheckTrue(eRange.Length > 0,
                          "（前提）……而且 `LastDuplicateError` 里**真有人话**"
                        + "（空串会让下面那条 `Contains` **恒真** ⇒ 假绿）");
                CheckTrue(eRange.Contains("没复制") && !eRange.Contains("没写进存档"),
                          "★ A611（越界支）：出口说的是「**没复制**」、且**不许**说「没写进存档」"
                        + "（两支合成一句 ⇒ 这条红 —— 那正是原来的缺陷）\n              实得：「" + eRange + "」");
                Check(CollectionData.DeckCount(), c3, "★ A611：……而且库里**一套没多**（越界那一支是早退）");

                var pBad = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pBad);
                if (pBad != null) pBad.DeckIndex = bad;
                int m3 = lvMsg.Count;
                ClickHit(pBad != null ? pBad.Opt("Duplicate") : null);
                CheckTrue(LvCount(m3, "[DeckInfo] 复制失败：", LogType.Warning) == 1,
                          "★ A610 + A611：走调用点、复制失败 ⇒ **恰好一条警告级**的「复制失败」"
                        + "（改坏法：退回 `Debug.Log` ⇒ 这条红）");
                string dupLog1 = LvFirst(m3, "[DeckInfo] 复制失败：");
                CheckTrue(dupLog1 != null && CollectionData.LastDuplicateError.Length > 0
                          && dupLog1.Contains(CollectionData.LastDuplicateError),
                          "★ A611：……而且打的是 `CollectionData.LastDuplicateError` 的**原话**"
                        + "（⛔ 不是在这里拿「`DeckCount()` 变没变」再判一遍 —— A548 当时就是那么写的，"
                        + "那一版**不含**出口的原话 ⇒ 这条红）\n              实得：「" + dupLog1 + "」");
                Check(LvCount(m3, "[DeckInfo] 已复制成「", LogType.Log), 0,
                      "★ A611：……而**不许**再说「已复制成」（两句互斥 —— 弱断言分不出这两种状态）");
                Check(CollectionData.DeckCount(), c3, "★ A611：……走调用点也一样，库里**一套没多**");

                // ---- ⑤ 探针二：**写盘失败**（父目录不存在 ⇒ `File.WriteAllText` 抛 `DirectoryNotFoundException`）----
                //   ⚠️ **不调** `CollectionData.ResetForTest()`：那会按新路径**读成空库**
                //      （`DeckStore.LoadAll` 见 `File.Exists(Path)` 为假 ⇒ 回空表）—— 内存里那份库**要留着**
                //      （它才是「内存里已经多了一套」的观测面）。落盘用的是**调用那一刻**的 `DeckStore.OverridePath`。
                DeckStore.OverridePath = Path.Combine(probeDir, "__wf_a611_no_such_dir__", "x.json");
                var probeLib = DeckLibrary.Load();
                CheckTrue(!probeLib.Save(), "（前提）这条路径**确实写不进去**（写得进去 ⇒ 下面几条全是假绿）");
                string expectSaveErr = probeLib.LastError;
                CheckTrue(!string.IsNullOrEmpty(expectSaveErr),
                          "（前提）写不进去时**带了原因**（原因空 ⇒ 下面「报的是真原因」那两条会**空串恒真**）");

                int c4 = CollectionData.DeckCount();
                string rSave = CollectionData.DuplicateDeck(0);
                string eSave = CollectionData.LastDuplicateError;
                CheckTrue(rSave.Length == 0, "★ A611：写盘失败 ⇒ `DuplicateDeck` 也回**空串**（两种成因同一个返回值）");
                CheckTrue(eSave.Contains("没写进存档") && !eSave.Contains("越界"),
                          "★ A611（没落盘支）：出口说的是「**没写进存档**」、且**不许**说「越界」"
                        + "\n              实得：「" + eSave + "」");
                CheckTrue(eSave.Contains(expectSaveErr),
                          "★ A611：……而且报的是**真原因**（`DeckStore.SaveAll` 那条 `catch` 的**运行时原话**"
                        + "「" + expectSaveErr + "」—— ⛔ 不是我们写死的一句文案）");
                Check(CollectionData.DeckCount(), c4 + 1,
                      "★ A611：……而内存里**真的多了一套** —— 本条说的「失败」专指**没落盘**"
                    + "（内存改了、盘上没改），与 A503 那一节**同一条语义锚**；"
                    + "改坏法：把失败做成「回滚掉那一套」⇒ 这条红");
                CheckTrue(eRange != eSave,
                          "★ A611：两种失败报的是**两句不同的话**（合成一句「复制不了」⇒ 这条红）"
                        + "\n              ①（没复制）「" + eRange + "」"
                        + "\n              ②（没落盘）「" + eSave + "」");

                var pSave = DeckInfoPopup.Create(win.Manager, 0);
                win.Manager.OpenWindow(pSave);
                int c5 = CollectionData.DeckCount();
                int m4 = lvMsg.Count;
                ClickHit(pSave != null ? pSave.Opt("Duplicate") : null);
                CheckTrue(LvCount(m4, "[DeckInfo] 复制失败：", LogType.Warning) == 1,
                          "★ A610 + A611：走调用点、**没落盘**那一支 ⇒ **恰好一条警告级**的「复制失败」"
                        + "（改坏法：退回 `Debug.Log`，或退回 A548 那条按 `DeckCount()` 分支的写法 ⇒ 这条红）");
                string dupLog2 = LvFirst(m4, "[DeckInfo] 复制失败：");
                CheckTrue(dupLog2 != null && CollectionData.LastDuplicateError.Length > 0
                          && dupLog2.Contains(CollectionData.LastDuplicateError),
                          "★ A611：……而且打的是出口的**原话**（⛔ A548 那一版这里写的是"
                        + "「原因见上面那条 `[CollectionData]` 警告」—— **不含**出口的原话）"
                        + "\n              实得：「" + dupLog2 + "」");
                CheckTrue(dupLog2 != null && dupLog2.Contains(expectSaveErr),
                          "★ A611：……**带上了运行时真原因**（「" + expectSaveErr + "」）—— "
                        + "改坏法：把 `LastDuplicateError` 换成写死的一句文案 ⇒ 这条红");
                CheckTrue(dupLog2 != null && !dupLog2.Contains("越界"),
                          "★ A611：……且**不许**说「越界」（两支互斥）");
                Check(CollectionData.DeckCount(), c5 + 1, "★ A611：……而内存里**确实多了一套**（两种失败因此分得开）");

                // ---- ⑥ 收尾：路径还回去 + 把探针副本删掉（⛔ 夹具那条存档**一个字节都没被碰过**）----
                DeckStore.OverridePath = keepPath;
                CollectionData.ResetForTest();
                Application.logMessageReceived -= hLv;
                CheckText(DeckStore.OverridePath, keepPath, "（收尾）`OverridePath` 还回夹具那条（玩家真存档没被碰过）");
                if (File.Exists(goodPath)) File.Delete(goodPath);
                CheckTrue(!File.Exists(goodPath), "（收尾）控制组那份**副本**删掉了（" + goodPath + "）");
            }

            // ======== 🆕 2026-10-13（A435 阶段 2 · 乙）：裁切状态迁到【视口节点】上 + A465 ========
            //   覆盖本块在收藏线的全部站点：A15（卡池）· A16（卡背）· A17（异画）· A18（左栏筛选栏）
            //   · A19（卡组列表）· B2（筛选栏标题行那记「重新裁一刀」）· A465（`MenuScroll.Intersects` 吃节点）。
            //   契约 → `Shell/ViewportClip.cs` 文件头；逐站点的「设/还原」对照 → `资料/普查产出_1013/A435_迁移表.md`。
            //   🔴 **两个变异源分开下毒**（谁红就知道是哪一档坏）：
            //     ① 挪**节点**的 `localPosition` ⇒ 只动「框在哪」，不动 `Viewport` 字段（A465 的灭自证条）；
            //     ② 收**节点**的 `sizeDelta` ⇒ 只动「框多大」，看**渲出来的真几何**（`ImageQuad.WorldW/H`）。
            //   ⛔ 全段一条计数器都不读（`NodeResolutions` / `NodeShadowedByParam` 都不碰）。
            Section("A435 阶段 2 · 乙：五个视口节点 + A465（构建循环吃节点）");
            {
                win.tabButtons.Click(0);        // 先切到 Deck 页（上面的小节把页签留在别处；节点引用一律在这个之后取）
                // ---- ① 四页各自的 `Viewport` 节点：在不在 / 参数 / **框** ----
                //   框的期望值 = **原版视口矩形字面量**（与上面各节同一批数，这里独立重写一遍：
                //   ⛔ 不回读 `CollectionWindow.*View` 那几个常量 —— 那是被测实现自己的那一份）。
                int[] pgIdx = { 0, 1, 2, 3 };
                string[] pgName = { "Deck", "Cards", "Cosmetics", "Styles" };
                // 🔴 **2026-10-17（`A1642`）**：Deck 页那条路径**多了两层容器**（`Decks Tab ▸ Collection Display`）
                //   —— 原版层级（`menu_dump.py … "Select Deck Tab" --depth 4` 现读）。这里写**全路径**
                //   （`Transform.Find` 逐段只找直接子件）⇒ 顺带把「容器层级」也钉在这一条上。
                string[] pgPath = { "Decks Tab/Collection Display/Deck Scroll View/Viewport", "Scroll View/Viewport",
                                    "Scroll View/Viewport", "Scroll View/Viewport" };
                float[] pgL = { 330.9f, 330.2f, 335.44f, 330.22f };
                float[] pgT = { 155.9f, 155.9f, 155.94f, 287.67f };
                float[] pgR = { 1920f, 1919.9f, 1920.01f, 1920.01f };
                float[] pgB = { 1080f, 1079.9f, 1080f, 1080f };
                for (int k = 0; k < 4; k++)
                {
                    var pgRootK = win.PageRoot(pgIdx[k]);
                    var vpK = pgRootK != null ? pgRootK.Find(pgPath[k]) : null;
                    CheckTrue(vpK != null, $"（前提）`{pgName[k]}` 页的视口节点 `{pgPath[k]}` 在");
                    if (vpK == null) continue;
                    var vcK = vpK.GetComponent<ViewportClip>();
                    CheckTrue(vcK != null,
                              $"★ A435·乙：`{pgName[k]}` 页那颗 `Viewport` 上挂着 `ViewportClip`"
                            + "（= 原版 `Viewport` 上那个 `RectMask2D`）—— 迁移前这里是**裸节点** + 各 `Rebuild*Cells`"
                            + " 里一对「把本窗 `Clip` 设成页视口 → 画 → 还原」（**那一对已整对删掉**）"
                            + "。**改坏法**：把那句 `ViewportClip.Hang(...)` 换回 `Node(...)` ⇒ 本条红");
                    if (vcK == null) continue;
                    Check(vcK.padding, Vector4.zero,
                          $"★ A435·乙：`{pgName[k]}` 页节点 `padding` = (0,0,0,0)（原版那颗 `RectMask2D.m_Padding` 实读）");
                    Check(vcK.softness, Vector2Int.zero,
                          $"★ A435·乙：`{pgName[k]}` 页节点 `softness` = (0,0)（原版 `m_Softness` 实读）");
                    var bxK = vcK.ClipPx;
                    CheckTrue(bxK.HasValue, $"（前提）`{pgName[k]}` 页那颗节点给得出框（它是 `RectTransform` 且有 `sizeDelta`）");
                    if (bxK.HasValue)
                    {
                        CheckNear(bxK.Value.x1, pgL[k], 0.5f, $"★ A435·乙：`{pgName[k]}` 页节点框**左沿** = 原版视口左沿 {pgL[k]}");
                        CheckNear(bxK.Value.y1, pgT[k], 0.5f, $"★ A435·乙：……**上沿** {pgT[k]}");
                        CheckNear(bxK.Value.x2, pgR[k], 0.5f, $"★ A435·乙：……**右沿** {pgR[k]}");
                        CheckNear(bxK.Value.y2, pgB[k], 0.5f, $"★ A435·乙：……**下沿** {pgB[k]}");
                    }
                }

                // ---- ② A465 的**灭自证**条：框的来源是【节点】，不是那个非空 `Viewport` 字段 ----
                //   🔴 为什么这条能灭自证：老的错写法（块 4 建议的 `VisibleAbove(Owner.transform, onScreen,
                //   Viewport)`，或干脆不改）读的都是 `MenuScroll.Viewport` —— 那是一份**与节点无关**的常数，
                //   把节点搬走它**纹丝不动** ⇒ 下面这一条必红。
                {
                    var pgD435 = win.PageRoot(0);
                    // 🆕 2026-10-17（`A1642`）：`Deck Scroll View` 在两层容器之下 ⇒ 递归取（`FindChild` 是递归的）
                    var dHolder435 = FindChild(pgD435, "Deck Scroll View");
                    var vpD = dHolder435 != null ? dHolder435.Find("Viewport") : null;
                    var scD = win.DeckScroll;
                    CheckTrue(vpD != null && scD != null, "（前提）卡组页的视口节点 + 滚动区都在");
                    if (vpD != null && scD != null)
                    {
                        var inVp = new PxRect(500f, 400f, 700f, 500f);         // 原视口里的一小块
                        CheckTrue(scD.Intersects(inVp), "（前提）这一小块**落在原视口里** ⇒ 正常态判「可见」");
                        // 🆕 **2026-10-16（A811 根治 · 主对话裁定的补丁）**：毒药从「挪**实时 transform**」改成
                        //   「挪**节点那个框**」—— 根治之后 `ClipPx` 的中心取的是**宿主写进节点的设计矩形**
                        //   （`ViewportClip.BaseRect`）、**不再跟实时 `localPosition` 走** ⇒ 老毒药下错了地方
                        //   （命题仍成立，只是毒在了「框不再取用的那个量」上，会变成假红）。
                        //   改完语义**更强**：毒的是框**真正取用的那个量**。
                        //   出处 → `资料/普查产出_1016/W11_A811根治.md` §③·B。
                        var vpcD = vpD.GetComponent<ViewportClip>();
                        var keepRect = vpcD != null && vpcD.HasBaseRect ? vpcD.BaseRect : (PxRect?)null;
                        CheckTrue(vpcD != null && keepRect.HasValue,
                                  "（前提）那颗视口**有框记录**（`ViewportClip.BaseRect`）—— 根治后毒药的落点");
                        // 🔴 **2026-10-17 订正**：这条毒药**只挪中心** —— 根治后的 `ClipPx` 是「**中心取 `BaseRect`、
                        //    尺寸仍取活节点自己的 `rect`**」（`Shell/ViewportClip.cs`）⇒ 上面那对尺寸
                        //    **根本不参与**：毒后中心 = (1125.45, 202.95)、半尺寸仍取活的 Deck 视口 (794.55, 462.05)
                        //    ⇒ 毒后框 y = [−259.1, 665.0] **完整罩住** `inVp`=(500,400,700,500) ⇒「不相交」前提假、假红。
                        //    改法 = 把**中心**推到 |cy − [400,500]| > 462.05 之外（⛔ 别只调矩形高度，尺寸本来就不参与）。
                        //    判据 → `资料/普查产出_1016/诊断_收口7条红.md` §7。
                        if (vpcD != null) vpcD.SetBaseRect(new PxRect(330.9f, 155.9f, 1920f, -1600f));   // 与 `inVp` 不相交（中心 y ≈ −722）
                        CheckTrue(!scD.Intersects(inVp),
                                  "★ A465：把**节点那个框**搬走之后，落在 `MenuScroll.Viewport` 里那一块"
                                + "**必须判不可见** —— 这条钉的是「`Intersects` 的框来自**节点**」。"
                                + "**改坏法**：把 `MenuScroll.Intersects` 写回 `MenuDraw.Visible(onScreen, Viewport)`"
                                + "（2026-10-13 之前的老写法）⇒ 立刻红");
                        if (vpcD != null && keepRect.HasValue) vpcD.SetBaseRect(keepRect.Value);
                        CheckTrue(scD.Intersects(inVp),
                                  "（还原）把框放回去 ⇒ 又判可见 —— 这一条同时钉住「上一条不是因为别的原因红的」");
                    }
                }

                // ---- ③ 节点态真的驱动【裁切】：收小节点 ⇒ 重建 ⇒ **渲出来的真几何**跟着变 ----
                //   判据全部独立算：带子高 250（我下的毒）· 卡框上沿 = 格顶 + `DcFrameY(17) × K(0.9)`。
                {
                    var holderD = FindChild(win.PageRoot(0), "Deck Scroll View");     // 🆕 A1642：递归取（两层容器之下）
                    var vpD2 = holderD != null ? holderD.Find("Viewport") : null;
                    CheckTrue(vpD2 != null && holderD != null, "（前提）卡组页的节点 + `Deck Scroll View` 都在");
                    if (vpD2 != null && holderD != null)
                    {
                        win.ClearDeckFilters();                    // 前面的小节可能留着筛选 ⇒ 先回到「全列」
                        win.DeckScroll.SetOffset(0f);              // 前面的小节可能滚过 ⇒ 回到顶端
                        win.RebuildDeckCells(holderD);
                        int nDk435 = win.DeckCellCount;
                        CheckTrue(nDk435 > CollectionWindow.DeckCols,
                                  $"（前提）正常态建出 {nDk435} 格 > {CollectionWindow.DeckCols}（6）"
                                + " ⇒ 下面那条「收成一条带」才有鉴别力（≤6 格时第 1 行本来就装得下全部）");
                        var c0 = win.DeckCells.Count > 0 ? win.DeckCells[0] : null;
                        CheckNear(Hpx(FindChild(c0, "Frame")), 368f * 0.9f, 2f,
                                  "（前提）正常态：第 1 格的卡框高 = **331.2**（原版 368 × 显示比例 0.9）—— 没被裁");

                        // 下毒：把**节点框**收成「视口顶端 250px 的一条带」（`MenuScroll.Viewport` 一个字节没动）
                        MenuDraw.ApplyPxRect(vpD2, vpD2.parent,
                                             new PxRect(330.9f, 155.9f, 1920f, 155.9f + 250f));
                        win.RebuildDeckCells(holderD);             // 走生产那条重建路（`DeckScroll.OnChanged` 也指它）
                        Check(win.DeckCellCount, Mathf.Min(CollectionWindow.DeckCols, nDk435),
                              $"★ A435·乙（A19）：把节点框收成顶端 250px 之后**只剩第 1 行**被建"
                            + $"（正常态 {nDk435} 格）—— 改坏法：把 `DeckScroll.ClipNode = deckVp;` 那一句删掉"
                            + "（退回只看 `MenuScroll.Viewport`）或把 `Hang(...)` 换回 `Node(...)` ⇒ 本条红");
                        var c0p = win.DeckCells.Count > 0 ? win.DeckCells[0] : null;
                        CheckNear(Hpx(FindChild(c0p, "Frame")), 250f - 17f * 0.9f, 2.5f,
                                  "★ A435·乙（A19）：第 1 格的卡框被**节点那条带**裁到 **234.7** 高"
                                + "（= 250 − `DcFrameY` 17 × `K` 0.9；正常态 331.2）—— 量的是**渲出来的真值**"
                                + "（`ImageQuad.WorldH × 108`，⛔ 不是计数器）。**改坏法**：节点不生效 ⇒ 回到不裁 ⇒ 331.2 ⇒ 红");

                        MenuDraw.ApplyPxRect(vpD2, vpD2.parent, CollectionWindow.DeckViewport);   // 还原
                        win.RebuildDeckCells(holderD);
                        Check(win.DeckCellCount, nDk435, "（还原）格数回到正常态");
                        var c0r = win.DeckCells.Count > 0 ? win.DeckCells[0] : null;
                        CheckNear(Hpx(FindChild(c0r, "Frame")), 368f * 0.9f, 2f,
                                  "（还原）第 1 格的卡框高回到 **331.2** —— 毒解干净了（上一条不是残留状态蒙对的）");
                    }
                }

                // ---- ④ 「两态」之二：**显式实参**仍然赢（`Resolve` 第 1 支，逐字未改）----
                //   探针挂在**带节点的子树里**、显式给一个「与节点框部分重叠」的框 ⇒ 结果必须按显式那个算。
                //   ⚠️ 这一条会让 `ViewportClip.NodeShadowedByParam` **+1**（设计如此：它是「显式传参盖住节点」
                //   的探测器、**不是缺陷计数**），而那条「== 0」的断言只落在 `RewardsScene.Run`（A489 的时点纪律）
                //   —— 本场景不断它，且每次 `*Scene.Run` 是**独立进程**，不会串到别人那儿。
                {
                    var pgD435c = win.PageRoot(0);
                    // 🆕 2026-10-17（`A1642`）：同上 —— 视口在两层容器之下，先递归取宿主再取直接子件
                    var dHolder435c = FindChild(pgD435c, "Deck Scroll View");
                    var vpD3 = dHolder435c != null ? dHolder435c.Find("Viewport") : null;
                    CheckTrue(vpD3 != null, "（前提）拿得到一颗带 `ViewportClip` 的节点当代父件");
                    if (vpD3 != null)
                    {
                        var expClip = new PxRect(200f, 50f, 900f, 700f);          // 与节点框（左 330.9）**部分重叠**
                        var want = new PxRect(330.9f, 200f, 900f, 700f);          // 独立算：R ∩ 显式框
                        var probe = MenuDraw.Rect(vpD3, CardArt.Solid(),
                                                  new PxRect(330.9f, 200f, 1200f, 800f), "A435b probe", 3000,
                                                  null, false, expClip, default(Vector2));
                        CheckTrue(probe != null, "（前提）探针件建出来了（`CardArt.Solid()` 取得到）");
                        if (probe != null)
                        {
                            CheckNear(probe.WorldW * 108f, want.W, 2f,
                                      "★ A435·乙（`Resolve` 第 1 支）：**显式 `clip` 形参仍逐字优先**"
                                    + "（宽 = 900 − 330.9 = **569.1**；⛔ 节点框那一档会给 **869.1** = 1200 − 330.9）"
                                    + " —— 这一档管着全壳「不传 `null` 时的老行为」；**改坏法**：把 `Resolve` 的优先级"
                                    + "翻成「节点优先」⇒ 立刻红");
                            CheckNear(probe.WorldH * 108f, want.H, 2f,
                                      "★ A435·乙（`Resolve` 第 1 支）：……高 = 700 − 200 = 500（同样按显式那个框算）");
                            Object.DestroyImmediate(probe.gameObject);
                        }
                    }
                }
            }

            // ============================================================ 🆕 2026-10-09（`A1025`）本批键全 `Loc.HasEntry` + 两语档取真文案
            // 判据（坑表 #18）：**「原版有词条」≠「我们表里有键」** —— 键不在 ⇒ `Loc.T` 返回**键名本身**、界面上就印键名。
            // 🔴 **与 `A1057(f)`（`Editor/NetSelfTest.cs` 那张**表级**扫描）不是同一条、方向相反**：那条是 **表 → 表**
            //   （表自身健康），本条是 **代码 → 表**（**本批代码引的键**有没有落进表）—— 表级扫描永远看不见后者。
            // 🔴 **Net/ 那批已有等价物**（`Editor/NetSelfTest.cs` 的 `TestNetTermBilingual`）⇒ 本笔不重复覆盖它。
            // 数组 = 本批生产文件里出现的**活键字面量** ∩ `Core/Loc.cs` 的表键（超集无害、且更严）。
            // 本宿主的数组 = 上面 `DeckScene` 那批 ∪ `Shell/{DeckInfoPopup,DeckSelectionPopup,ImportDeckPopup,CollectionData,PracticeModePopup}.cs`。
            // 🔴 **`MenuDeck/Share/ExportSuccesful` 【不在】本数组**：它在代码里被引（`Shell/DeckInfoPopup.cs` 的 `ExportSuccesful`）、表里没有 ——
            //   但走的是 `ErrorMessageBanner.ShowMessage` 的 `fallback` 支（**界面不印键名、不是缺陷**），正主是 `A1050`。
            // 🧨 改坏法：① `Loc.cs` 删掉本批任一条键 ⇒ ①红；② 某条**英文列**填中文/全角空格 ⇒ ③红；
            //   ③ **中文列**清空 ⇒ ②红；④ 值改成键名本身 ⇒ ②红；⑤ 表删掉一半 ⇒ ④红（`EntryCount` 掉到基线之下）；
            //   ⑥「把实现与期望一起改回写死中文 **并** 把表里那条删掉」⇒ ①红（数组里那条键仍在、`HasEntry` 假）—— 这正是本数组存在的唯一理由。
            {
                string[] a1025Keys =
                {
                    "Battle/Tips/EnergyCost", "Card_Race/Warlord", "Card_Rarity/Common",
                    "Card_Rarity/Epic", "Card_Rarity/Legendary", "Card_Rarity/Rare",
                    "Card_Rarity/Special", "MainMenu/General/Confirm", "MainMenu/General/OK",
                    "MainMenu/MainButtons/ButtonLabel/Back", "MenuCollection/NoCardsFound", "MenuCollection/NoDecksFound",
                    "MenuDeck/Button/Random", "MenuDeck/CantImportDeck", "MenuDeck/DefaultDeckName",
                    "MenuDeck/DemoDeckName", "MenuDeck/Error/CantStartNoWarlord", "MenuDeck/Error/HiddenCards",
                    "MenuDeck/Error/ImportBadString", "MenuDeck/Error/ImportEmpty", "MenuDeck/Error/ImportNotPersisted",
                    "MenuDeck/Error/InvalidDeck", "MenuDeck/Error/NoUsablePrebuilt", "MenuDeck/Error/NoWarlord",
                    "MenuDeck/Error/PrebuiltMissing", "MenuDeck/Error/SaveFailed", "MenuDeck/Filters/Army",
                    "MenuDeck/Filters/ClearFilters", "MenuDeck/Filters/Filters", "MenuDeck/Filters/ShowOwnedOnly",
                    "MenuDeck/Filters/ShowUpgradableOnly", "MenuDeck/Filters/Type", "MenuDeck/HUD/DeckDescription/DeckInfo",
                    "MenuDeck/HUD/DeckDescription/Minions", "MenuDeck/HUD/DeckDescription/Spells", "MenuDeck/HUD/DiscardChanges",
                    "MenuDeck/HUD/DragCardsTip", "MenuDeck/HUD/EditDeckName", "MenuDeck/HUD/EnterText",
                    "MenuDeck/HUD/NoArmySelected", "MenuDeck/HUD/Rarity", "MenuDeck/HUD/SearchFilter",
                    "MenuDeck/MenuButtons/Done", "MenuDeck/NewDeckName", "MenuDeck/Share/PasteDeck",
                    "MenuDeck/Tip/SelectDeckAgainst", "MenuShop/ShopItemType/Cards", "MenuShop/ShopItemType/Cosmetics",
                    "Settings/Online/MatchCancelFailed", "Settings/Online/MatchCancelled",
                };
                foreach (var k in a1025Keys)
                    CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");
                // ② 两语档各取一次、都非空且 ≠ 键名（只断中文档 = 半边绿）
                var a1025LangWas = Loc.Current;
                foreach (var lang in new[] { AvailableLanguages.Chinese, AvailableLanguages.English })
                {
                    Loc.RestoreForTest(lang);
                    int bad = 0; string firstBad = null;
                    foreach (var k in a1025Keys)
                    {
                        string v = Loc.T(k);
                        if (string.IsNullOrEmpty(v) || v == k) { bad++; if (firstBad == null) firstBad = k; }
                    }
                    CheckTrue(bad == 0, $"★（A1025）`{lang}` 档下 {a1025Keys.Length} 条**全部取到真文案**（缺 {bad} 条"
                              + (firstBad == null ? "" : $"，第一条 `{firstBad}`") + "）"
                              + " —— 取不到时会印**键名本身**，那就是静默失败");
                }
                Loc.RestoreForTest(a1025LangWas);      // ⛔ 只改内存、不写 `PlayerPrefs`（不是 `SetLanguage`）
                // ③ 灭自证 C1：英文列不许含汉字（`Loc.HasCjk` 的区间含 `0x3000-0x303F` 与 `0xFF00-0xFFEF`）
                {
                    int cjk = 0; string firstCjk = null;
                    foreach (var k in a1025Keys)
                    {
                        string en = Loc.EnOf(k);
                        if (!string.IsNullOrEmpty(en) && Loc.HasCjk(en)) { cjk++; if (firstCjk == null) firstCjk = k; }
                    }
                    CheckTrue(cjk == 0, $"★（A1025）本批 {a1025Keys.Length} 条的**英文列无 CJK**（坏 {cjk} 条"
                                      + (firstCjk == null ? "" : $"，第一条 `{firstCjk}`") + "）");
                }
                // ④ 灭自证 D（表基线）：开工前实测 `EntryCount == 429`
                CheckTrue(Loc.EntryCount >= 429,
                          $"★（A1025）表基线：`Loc.EntryCount` = {Loc.EntryCount} ≥ **429**（开工前实测）"
                        + "｜🧨 把键删掉、断言也一起删 ⇒ 这条红");
            }

            // ============================================================ 🆕 **2026-10-11（`A1517`）：
            //   左栏四颗键文案断言的【真鉴别式】—— **语档无关**的那一种。**
            //
            // 🔴 **要治的病**：上面那条 `CheckText(TextOf(键上那颗 Text), wantLabels[i])` 里的
            //   `wantLabels[i] = Loc.T(term).ToUpperInvariant()` **按当前语档取列**，而本宿主
            //   **全程不设语档**（`Loc.Current` 第一次读时从 `PlayerPrefs["Language"]` 载入）——
            //   那四个键的**英文列逐字等于 `TabBtnSpec.Label`（= 原版 `m_text`）** ⇒ 本机若被设成
            //   English，把实现改回「直接画 `spec.Label`」**四条照样全绿**（屏上字一模一样、看不出区别）。
            //   今天跑批有鉴别力，只是因为出厂默认是 Chinese（`Loc.Default`）。
            //
            // ⛔ **不是**「在跑批前钉一次中文档」：本文件实测过 —— 一旦把语档钉成中文并**保持**，
            //   那些同样「按 `Loc.T` 现取期望值」的断言（`本文件` 空态那行 / `本文件` `Import Deck` /
            //   `本文件` `Create Deck` / `本文件` `Warlord Name` …）**会一起红**（期望值按钉住的档取，
            //   而屏上的字是建窗那一刻那一档画上去的）⇒ 那是**假红**，⛔ 别走那条路。
            //
            // ✅ **本段的做法 = 断「切语档之后那颗字的串**跟着变**」**：
            //   · 语档**只在这一小段里**改，走内存态 `Loc.RestoreForTest`（⛔ 一个字节都不写
            //     `PlayerPrefs`），收尾**还回本机原语档**；
            //   · 每次改完把窗**原地重建**一次（`CollectionWindow.Build()` 是 `public`，它首句
            //     `BuildShell` → `DestroyChildren(root)` —— 本来就是「清空再建」的形状）；
            //   · ⇒ 结论与**跑批机器是什么语言**无关。
            // 🔴 **为什么必须重建**：字是**建窗时**画的（`Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton`
            //   里那句 `Loc.T(spec.Term)`），换语档**不会**改已经画上去的字（本窗没有 `RefreshTexts`）
            //   ⇒ 不重建就量不出「跟着变」。
            // 🧨 **改坏法**：把 `Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton` 里那句 `Loc.T(spec.Term)`
            //   改回 `spec.Label` ⇒ **中文档那一趟**印的仍是英文 ⇒ 「两趟一样」⇒ 对应那条立刻红。
            // ⚠️ 本段排在**全文件收尾**（下面只剩 `total` 与收尾日志）：重建会把这扇窗的孩子整棵换掉，
            //   ⛔ 别把它往前挪 —— 前面每一节都还攥着节点引用（`keys` 那张表就是）。
            {
                var p4LangWas = Loc.Current;
                var p4Zh = new string[4];
                var p4En = new string[4];
                // 前提（本条自己的闸）：四颗键**中英两列都在、而且不同** —— 两列相同时这一问没有鉴别力
                string p4Bad = null;
                for (int i = 0; i < 4; i++)
                {
                    string a = Loc.ZhOf(tabTerms[i]), b = Loc.EnOf(tabTerms[i]);
                    if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b)
                        p4Bad = (p4Bad == null ? "" : p4Bad + " ") + tabTerms[i];
                }
                CheckTrue(p4Bad == null,
                          "★（A1517 前提）左栏四颗键的**中文列与英文列都存在且不同**"
                        + "（⛔ 两列一样 ⇒ 下面「串跟着语档变」没有鉴别力；坏：" + (p4Bad ?? "无") + "）");

                for (int pass = 0; pass < 2; pass++)
                {
                    Loc.RestoreForTest(pass == 0 ? AvailableLanguages.Chinese : AvailableLanguages.English);
                    win.Build();                     // 原地重建：字按**当前**语档重画（⛔ 不是改已画出的字）
                    var bar7 = FindChild(win.transform, "Tab Buttons");
                    var dst = pass == 0 ? p4Zh : p4En;
                    for (int i = 0; i < 4; i++)
                    {
                        var kb7 = FindChild(bar7, "CollectionTabButton_" + i);
                        dst[i] = TextOf(FindChild(kb7, "Text"));
                    }
                }
                Loc.RestoreForTest(p4LangWas);       // ⛔ 只改内存、不写 `PlayerPrefs`
                win.Build();                         // 收尾：把窗建回**本机原语档**那一版

                for (int i = 0; i < 4; i++)
                {
                    CheckTrue(p4Zh[i] != null && p4En[i] != null && p4Zh[i] != p4En[i],
                              $"★ A1517 真鉴别式：左栏第 {i + 1} 键 `CollectionTabButton_{i}` 的串"
                            + $"**跟着语档变** —— 中文档印「{(p4Zh[i] ?? "<取不到>")}」、"
                            + $"英文档印「{(p4En[i] ?? "<取不到>")}」"
                            + $"（⛔ 两趟一样 ⇒ 那颗字**不是从 `Loc.T` 来的**）｜键 `{tabTerms[i]}`"
                            + "｜🧨 改坏法：`Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton` 里那句"
                            + " `Loc.T(spec.Term)` 改回 `spec.Label` ⇒ 本条红"
                            + "（**语档无关**，不再依赖跑批机器是中文档）");
                    CheckText(p4Zh[i], Loc.ZhOf(tabTerms[i]).ToUpperInvariant(),
                              $"…而且**中文档那一趟**印的就是那条词条的**中文列**（大写那一档 —— "
                            + "原版 `m_fontStyle = UpperCase` 已由基类 `ToUpperInvariant` 实现）");
                    CheckText(p4En[i], Loc.EnOf(tabTerms[i]).ToUpperInvariant(),
                              "…**英文档那一趟**印的是**英文列**（同上）");
                }
            }

            // ---------------- 🆕 2026-10-13（`A1132` 第 3 步）：入口「改开窗」那半 ----------------
            //
            // 🔴 **本节怎么把「入口 → 开窗那一跳」验到**（口径 → `Shell/CollectionWindow.RouteDeckEntry` 的 doc）：
            //    批处理里那三处入口**故意**停在 `Application.isBatchMode` 那道闸上（**有意保留的兜底**：
            //    直起 `-wfscene DeckEditor` 也走它；而且 `DeckRuntime.Build` 开局会把交接值读掉并清掉
            //    ⇒ 把那道闸挪到开窗之后，本文件上面「点 `Edit Deck` ⇒ 交接的下标」当场观测不到）
            //    ⇒ 生产的**单参**调用形式**永远走不到开窗那一支**。
            //    ⇒ 落点判定被抽成一个**可传参的纯函数** `RouteDeckEntry(batch, hasShell)`，
            //    自检直接把那两个真值喂进去（`hasShell: true, batch: false`）⇒ **走的就是生产那个 `switch` 本体**。
            //    ⛔ **缝只喂判定值**：开窗那一跳拿的仍是**现场的** `WindowsManager.Instance` ⇒ **伪造不出一个壳**
            //    （传 `hasShell:true` 而真没壳时 `OpenFrom(null)` 会出声并返回 null）。
            Section("A1132 第 3 步：进编辑 = **开窗不切场景**（`RouteDeckEntry` 表 + 入口那一跳）");
            {
                // ---- ① 那张表本身（三格全断：少了哪一格都说明判定被改过）----
                Check(CollectionWindow.RouteDeckEntry(batch: false, hasShell: true).ToString(),
                      CollectionWindow.DeckEntryRoute.OpenWindow.ToString(),
                      "★ A1132 第 3 步：**有壳 + 非批处理 ⇒ 开窗**（第 3 步要换成的那个落点）");
                Check(CollectionWindow.RouteDeckEntry(batch: true, hasShell: true).ToString(),
                      CollectionWindow.DeckEntryRoute.BatchSkip.ToString(),
                      "★ …**批处理 ⇒ 既不切场景也不开窗**（有意保留的兜底；这条红 = 有人把闸改坏了）");
                Check(CollectionWindow.RouteDeckEntry(batch: false, hasShell: false).ToString(),
                      CollectionWindow.DeckEntryRoute.LoadScene.ToString(),
                      "★ …**没壳（直起 `-wfscene DeckEditor`）⇒ 切场景兜底**"
                    + "（判据 = `Shell/PlayerBoot.LoadSceneMatching` 按名字找场景）");

                var wm3 = WindowsManager.Instance;
                CheckTrue(wm3 != null, "（前提）有壳 —— 「入口 → 开窗」那一跳要先有这个壳");
                if (wm3 != null)
                {
                    System.Func<int> countDeckWin = () =>
                    {
                        int n = 0;
                        foreach (var w in wm3.openWindows) if (w is DeckEditingWindow) n++;
                        return n;
                    };

                    string sceneBefore3 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    // ⚠️ 三个交接口 + 回程意图都存/还：入口会写它们，而 `DeckRuntime.Build` 开局会把交接口**读掉并清掉**。
                    int keepPend3 = CollectionData.PendingEditDeck;
                    var keepNew3 = CollectionData.PendingNewDeck;
                    var keepRet3 = CollectionData.PendingReturn;
                    DeckEditingWindow deW3 = null;
                    try
                    {
                        // ---- ② 灭自证（**缝没漏到生产**）----
                        //   🔴 判据 = 「生产调用形式（单参）实际做出来的事」**必须等于**「拿**现场两个真值**喂给判定口
                        //   算出来的那一格」。批处理 ⇒ 那一格是 `BatchSkip` ⇒ 一扇窗都不许多出来。
                        //   若有人把「开窗」接到生产上（或把闸删了），本条当场红。
                        int nWinBefore = countDeckWin();
                        CollectionWindow.GoEdit(0);                    // ← **生产调用形式**（单参重载，读现场）
                        Check(countDeckWin(), nWinBefore,
                              "★ A1132 第 3 步（**灭自证**）：生产调用形式 `GoEdit(i)` 在批处理里**没有**多出任何"
                            + "卡组编辑窗 —— 判定口拿现场值算出来的那一格是 "
                            + CollectionWindow.RouteDeckEntry(Application.isBatchMode, WindowsManager.Instance != null)
                            + "（**缝只能用来测**：⛔ 不许在生产路径上被绕过）");
                        Check(CollectionData.PendingEditDeck, 0,
                              "★ ……而交接口照旧留着（`PendingEditDeck == 0`）—— **批处理那一支与改前逐位一致**"
                            + "（本文件上面那一整条链靠的就是它）");

                        // ---- ③ 缝：`hasShell:true, batch:false` ⇒ **入口**走生产那个 `switch` 的开窗支 ----
                        CollectionWindow.GoEdit(0, hasShell: true, batch: false);
                        foreach (var w in wm3.openWindows)
                            if (w is DeckEditingWindow && w.CurrentState == WindowState.Open) deW3 = (DeckEditingWindow)w;

                        CheckTrue(deW3 != null && deW3.gameObject.activeSelf,
                                  "★ …**入口那一跳真开出了窗**、物体活的（⛔ 不是「没开出来也算过」）");
                        CheckTrue(deW3 != null && deW3.IsOpen() && ReferenceEquals(wm3.TopWindow, deW3),
                                  "★ …而且是 `WindowsManager` 认的**顶窗**（`IsOpen()` 与 `TopWindow` 两条并存 —— "
                                + "只断一条的话，「state 说开着、物体却不活」那一档过不了另一条）");

                        // ---- 🆕 顺带把「第 5 步（遮让）到底还要不要做」的**读数**当场量出来（见本批报告 ③）----
                        //   D10 §四 说「收藏窗仍会盖住本窗 ⇒ 第 5 步的 `Hide()` 不能省」，而本窗是 `Fullscreen`
                        //   ⇒ `OpenWindow` 的全屏支第一句 `if (openWindows.Count > 0) HideAllWindows();`
                        //   （`Shell/WindowsManager.cs`）**应当已经**把收藏窗 `Hide()` 掉了。
                        //   ⚠️ **先断前提、再断事实**：前提不成立（收藏窗本来就已经 `Closed` / 不在表里）时
                        //     那一句 `HideAllWindows()` 本来就不会跑到它身上 ⇒ 那**不是缺陷**，出声说明、⛔ 不硬断成红。
                        {
                            bool winWasOpen = wm3.openWindows.Contains(win) && win.CurrentState != WindowState.Closed;
                            if (!winWasOpen)
                                Debug.Log(P + "  ⚠️ A1132「第 5 步该不该做」的读数**没量到**：这一刻收藏窗不在"
                                          + " `openWindows` 里（或已经是 `Closed`）⇒ 全屏支那句 `HideAllWindows()`"
                                          + " 本来就不会跑到它身上（判据 `Shell/WindowsManager.cs` 的 predicate）");
                            else
                                CheckTrue(!win.gameObject.activeSelf,
                                          "★ A1132【第 5 步该不该做】的**实测读数**：开编辑窗那一刻，收藏窗"
                                        + "**已经被全屏支的 `HideAllWindows()` 藏了**（`activeSelf == false`）"
                                        + " ⇒ D10 §四「收藏窗仍会盖住本窗」**不成立**；第 5 步若只做「把上一扇 `Hide()`」，"
                                        + "在**这条来路上是多余的**（⚠️ 前提 = 那一刻 `openWindows` 里真有、且还开着的收藏窗；"
                                        + "换一条「没有别的窗开着」的来路时那一跳不跑）");
                        }

                        Check(CollectionData.PendingEditDeck, -1,
                              "★ …**交接链路端到端走通了**：入口写下 `PendingEditDeck = 0`（上一条断的），"
                            + "窗里 `DeckRuntime.Build` 开局把它**读掉并清掉** ⇒ 此刻是 −1"
                            + "（这条红 = 窗开了、但**没接上交接**，等于开了一扇别的卡组）");
                        // 🔴 **2026-10-18（收口补丁 · 铁律 5）**：这一条原来写 `PoolCount > 1000` 的硬阈值。
                        //   `PoolCount` 是**未过滤**的卡池本体（收藏页那 122 张显示闸不参与）⇒ 期望值 =
                        //   **卡表去重 id 现算**；硬阈值在「1127 → 1005」之后只剩 127 的余量，数据一动就会假红。
                        //   ⛔ 别再写死数字。
                        int wantPoolN = 0;
                        {
                            var seenP = new HashSet<string>();
                            foreach (var pc in CardDatabase.Load())
                                if (pc != null && !string.IsNullOrEmpty(pc.Id) && seenP.Add(pc.Id)) wantPoolN++;
                        }
                        CheckTrue(deW3 != null && deW3.Runtime != null && deW3.Runtime.State != null
                                  && deW3.Runtime.State.PoolCount == wantPoolN,
                                  $"★ …而且编辑器内容**真建在窗里**（`DeckRuntime` 在 + 卡池张数 = 卡表去重 id 现算 {wantPoolN}）"
                                + " —— 只断「窗活着」的话，一个空窗也能过"
                                + $"（实测 {deW3?.Runtime?.State?.PoolCount}）");
                        CheckTrue(deW3 != null && deW3.Runtime != null && deW3.Runtime.HostWindow == deW3,
                                  "★ …**而且第 2 步那道豁免的宿主登记也生效了**（`DeckRuntime.HostWindow` = 这一扇）"
                                + " —— 第 2 / 3 步是**同一批**交件，本条就是把它们钉在一起的那一句");
                        CheckTrue(deW3 != null && deW3.Runtime != null
                                  && !deW3.Runtime.UiWindowShield && deW3.Runtime.UiEscEatenByHost,
                                  "★ …**两个谓词方向相反、且都对着**（指针放行 `!UiWindowShield` / ESC 由宿主窗吃"
                                + " `UiEscEatenByHost`）—— 少了任一半，「窗一开点不动」或「同一下 ESC 被处理两遍」"
                                + "就会有一个**悄悄地**回来（⛔ 两个谓词合成一个的话本条必红）");
                        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, sceneBefore3,
                              "★ **场景没换**（第 3 步要替掉的就是那三处 `LoadScene(\"DeckEditor\")`；"
                            + "批处理下这条就是「它没走切场景那条路」的判据）");

                        // ---- ④ 第二条入口（`GoNewDeck`）走同一张表：交接的是**未入库**那一份 ----
                        //   ⚠️ 上一扇要**关掉之后就销毁**（不然它会留在场景里、被本文件末尾那句 `SaveScene()`
                        //     一起存进自检场景 —— `CollectionCheck.unity` 是自检产物，但也别往里堆垃圾）。
                        if (deW3 != null) { deW3.Close(); UnityEngine.Object.DestroyImmediate(deW3.gameObject); deW3 = null; }
                        CollectionData.PendingNewDeck = CollectionData.Raw(0);   // 生产里由 `CollectionData.CreateDeck()` 填
                        int nWin2 = countDeckWin();
                        CollectionWindow.GoNewDeck(hasShell: true, batch: false);
                        CheckTrue(CollectionData.PendingNewDeck == null,
                                  "★ A1132 第 3 步：`GoNewDeck` 那条入口**同样走开窗那一支** —— "
                                + "窗里 `Build` 开头那句 `TakePendingNewDeck()` 跑过了（`PendingNewDeck == null`）");
                        Check(countDeckWin(), nWin2 + 1,
                              "★ ……而且**只多了一扇**卡组编辑窗（少一扇 = 没开出来；多 = 开重了）");
                        foreach (var w in wm3.openWindows)
                            if (w is DeckEditingWindow && w.CurrentState == WindowState.Open) deW3 = (DeckEditingWindow)w;
                    }
                    finally
                    {
                        if (deW3 != null) { deW3.Close(); UnityEngine.Object.DestroyImmediate(deW3.gameObject); }
                        CollectionData.PendingEditDeck = keepPend3;
                        CollectionData.PendingNewDeck = keepNew3;
                        CollectionData.PendingReturn = keepRet3;
                    }
                }
            }

            // ---------------- 🆕 2026-10-14（`A1714`）：关的这扇就是 `baseMenu` ⇒ 只 `Hide()` 就 `return` ----------------
            //
            // 判据 = `d:/2/tools/decomp_full/WindowsManager__CloseWindowCO.c` **逐句亲读**（那段证据写在本仓
            // `Shell/WindowsManager.cs` 的 `GameWindow.Close()` 里）：`op_Equality(param_2, *(manager + 0x48))`
            // 为**真**那一支 = 虚调 Slot 9 `Hide()` + **`return`**（既不 `List.Remove`、也不 `ShowPreviousWindow`）；
            // 为**假**那一支才是完整记账。
            // 🔴 **灭自证 = 两条并存、结构上不可能同时满足**（同一句 `Close()`，只因「是不是 `baseMenu`」而落两样）：
            //   · **关 `baseMenu` 自己** ⇒ 只 `Hide()`：`Closed` + 物体不活 **且 仍在册**（不 `Remove`）、
            //     `TopWindow` **一个字没动**；→ 缺这一支时它会走完整记账、被 `ShowPreviousWindow` **重新开回来**
            //     ⇒ 本半第一条必红（那正是 `A1709` 的根因那条链）。
            //   · **关一扇不是 `baseMenu` 的窗**（且它在列表尾）⇒ 走完整记账：它**离册** + 底下那扇 `baseMenu`
            //     被**开回来** ⇒ 把这一支写成「一律只 `Hide()`」时本半必红。
            Section("A1714：`baseMenu` 那一支（只 `Hide()` 就 `return`）—— 与「普通窗」成对");
            {
                var wmV = WindowsManager.Instance;
                CheckTrue(wmV != null, "（前提）壳在 —— 本节要读 `openWindows` / `TopWindow` / `BaseMenu`");
                if (wmV != null)
                {
                    var keepBase = wmV.BaseMenu;
                    var pA = MakeProbeWindow("A1714 probe base");     // 当 `baseMenu` 用
                    var pB = MakeProbeWindow("A1714 probe other");    // 当「普通窗」用
                    try
                    {
                        wmV.SetBaseMenu(pA);          // = 原版 `MainMenuWindow.Initialize` 那两句的前一句（顺带清表）
                        wmV.OpenWindow(pA);
                        CheckTrue(pA.gameObject.activeSelf && pA.CurrentState == WindowState.Open
                                  && wmV.openWindows.Contains(pA),
                                  "（前提）`baseMenu` 探针开好了：`Open` + 活 + 在册");

                        // ---- 半 ①：关的就是 `baseMenu` ⇒ **只 `Hide()`**（不摘表、不动 `TopWindow`）----
                        pA.Close();
                        CheckTrue(!pA.gameObject.activeSelf,
                                  "★ A1714：关 `baseMenu` 自己 ⇒ 它**被藏了**（`activeSelf == false`）"
                                + " —— 判据 = 原版那一支只虚调 Slot 9 `Hide()`（`GameWindow__Hide.c` = `state=Closed`"
                                + " + `SetActive(false)`）。⚠️ **缺这一支时**它会走完整记账、被 `ShowPreviousWindow`"
                                + " 重新 `OpenWindow` 开回来 ⇒ 本条红");
                        Check(pA.CurrentState, WindowState.Closed, "★ ……而且 `state` 写成了 `Closed`（= `Hide()` 的第一句）");
                        CheckTrue(wmV.openWindows.Contains(pA),
                                  "★ ……**但它【仍在册】**（原版那一支**不 `List.Remove`**）—— 改坏法：让它也走"
                                + " `NotifyClosed` ⇒ 本条红（那会把 `baseMenu` 从表里摘掉）");
                        CheckTrue(ReferenceEquals(wmV.TopWindow, pA),
                                  "★ ……而且 `TopWindow` **一个字没动**（原版那一支**不 `ShowPreviousWindow`**、也不改 "
                                + "`currentWindow`）—— 这一条与上面那条一起，才咬得住「只 `Hide()` 就 `return`」");

                        // ---- 半 ②：关一扇**不是** `baseMenu` 的窗 ⇒ 完整记账（它离册 + `baseMenu` 被带回来）----
                        wmV.OpenWindow(pB);           // 全屏支会 `HideAllWindows()`（此刻表里只有已 `Closed` 的 `pA` ⇒ 无事）
                        CheckTrue(pB.gameObject.activeSelf && pB.CurrentState == WindowState.Open,
                                  "（前提）普通窗探针开好了（此刻它是顶窗）");
                        pB.Close();
                        CheckTrue(!wmV.openWindows.Contains(pB),
                                  "★ A1714（**对照半**）：关一扇**不是 `baseMenu`** 的窗 ⇒ 它**离册**"
                                + "（= 完整记账那条 `List.Remove`）—— 与半 ① 那句「仍在册」**结构上不可能同时满足**"
                                + "（把 A1714 那一支写成「一律只 `Hide()`」⇒ 本半红）");
                        CheckTrue(pA.gameObject.activeSelf && pA.CurrentState == WindowState.Open,
                                  "★ ……而且底下那扇 `baseMenu` 被 `ShowPreviousWindow` **开回来了**（`Open` + 活）"
                                + " —— 这一跳正是 `A1709` 那条链在**普通窗**上的正确形态（⛔ 别把它一起砍了）");
                    }
                    finally
                    {
                        if (pA != null) UnityEngine.Object.DestroyImmediate(pA.gameObject);
                        if (pB != null) UnityEngine.Object.DestroyImmediate(pB.gameObject);
                        wmV.SetBaseMenu(keepBase);    // 复原：本宿主原来有没有 `baseMenu` 都还回去（顺带清表 + 清顶窗）
                    }
                }
            }

            // ---------------- 🆕 2026-10-20（`A1350` 第二批 · `W58-PTRPORT`）：设计帧 → 指针帧换算口 ----------------
            //   判据 / 恒等式 → `Editor/MenuCheck.cs` 的 `PointerFramePxFromDesign`；
            //   本宿主的调用点（`vis` / 设计 px 字面量 / `cellR` / 暗底 (5,5)）已全部走它。
            Section("A1350 第二批：设计帧 → 指针帧（`PointerFramePxFromDesign`）两态探针");
            CheckPointerFrameFromDesign("`PointerFramePxFromDesign`");

            // ---------------- 🆕 2026-10-21（`T02`）：尺寸帧（`NodeRectPx(designSize: true)`）两态 ----------------
            //   判据 → `Editor/MenuCheck.cs` 的 `NodeRectPx` `designSize` 那一支（四份覆盖块的归一口）；
            //   夹具与量法唯一一份 = `MenuCheck.DesignSizeTwoStateProbe`（本宿主只交**自己的** `RectOf`）。
            //   ⛔ 期望值**不是**今天量到的宽：它是「父链 ×2」这条夹具的不变量（设计宽 = 世界宽 ÷ 2）。
            Section("T02：`RectOf` 的尺寸帧（`designSize: true`）两态探针");
            {
                float dw, ww;
                CheckTrue(MenuCheck.DesignSizeTwoStateProbe(RectOf, out dw, out ww),
                          "（前提）T02：尺寸帧探针量得到（`RectOf` 返回 `false` ⇒ 下面两条不许静默跳过）");
                CheckTrue(dw > 1f, $"（前提）T02：设计宽 > 1px（{dw:F2} —— 0 的话下面那条恒等）");
                CheckNear(dw, ww * 0.5f, 0.5f,
                          $"★★ T02：父链 ×2 时 `RectOf` 报的**是设计宽**（本宿主 {dw:F2} vs 世界 {ww:F2} ÷ 2；容差 0.5px）"
                        + "｜🧨 把本文件 `RectOf` 的 `designSize: true` 改回缺省（或删掉 `MenuCheck` 里那一支）"
                        + " ⇒ 读到 `2 × 设计宽` ⇒ **本条红**；两帧一起改回同一帧 ⇒ 本条照样红");
            }

            // ============================================================ 🆕 2026-10-27（`B9_W36SHINEA`）
            //   账 `收藏:348`：`WIldcard Display ▸ Background` 的 **Shiny 扫光**（原版 `WildcardDisplay.OnEnable`
            //   → `shinyEffect.Play(resetTime:true)`）。判据 / 参数 / 「9 块共用一份材质」的理由 →
            //   `资料/完全复刻/报告/B9_W16SHINE_扫光统一.md` §三·1 ＋ `Resources/Shaders/WildcardShine.shader` 文件头。
            //   🔴 三道**前提各写一条硬断言**（shader 取不到 / 层没建出来 / 材质没挂上，**当场红**）
            //      —— ⛔ 不写 `if (前提) Check(结论)`（那条形状会让前提不成立时静默跳过，铁律：自检里最贵的坑）。
            //   🧨 灭自证：① 「9 块共用**同一个**材质对象」+「shader 名对」并排 —— 退回「每块各一份」红、
            //      退回 `Sprites/Default`（根本没挂）红；② `_ShineRot`/`_ShineFrame` 的期望值一半来自
            //      **原版参数**（120° ⇒ `(cos,sin)=(-0.5,0.8660)`）、一半来自**本段独立算的世界并集**
            //      （⛔ 不回读 `WildcardShineLayer.FrameBounds` 那个被测口）；③ `Neutral ⇒ 不播` 与
            //      「非 Neutral ⇒ 播」**两条并排** —— 把闸写成恒关/恒开都只能对一半。
            Section("B9 `收藏:348`：万能卡 `Background` 的 Shiny 扫光（参数 / 一份材质 / 播放进度 / Neutral 闸）");
            {
                // ---- 前提 ①：我们自己的 shader 取得到（只有 `Resources/` 下的才随包）----
                var shineShader = Shader.Find("CardPresentation/WildcardShine");
                CheckTrue(shineShader != null,
                          "（前提·不静默）`Shader.Find(\"CardPresentation/WildcardShine\")` 取得到"
                        + " —— 它不在 `Resources/` 下时 player 包里返回 null ⇒ 扫光**静默不出现**"
                        + "（`WildcardShineLayer.Attach` 会打一条 warning）");
                // ---- 前提 ②：层建出来了（`-1` = 没建；⛔ 不能当成「没播」）----
                CheckTrue(win.WildcardShineRate >= 0f,
                          $"（前提·不静默）扫光层建出来了（`WildcardShineRate` 现读 {win.WildcardShineRate:F3}；"
                        + "-1 = `WildcardShineLayer.AttachNine` 返回了 null）");

                // 那颗 `Background` 按**直接子件**取（同本文件上面那节 `WIldcard Display` 的取法，⛔ 不用递归的 FindChild：
                //   整页里叫 `Background` 的件不止一颗，递归会拿错）
                var hdrB = win.PageRoot(1) != null ? win.PageRoot(1).Find("Header Filters") : null;
                var wdB = hdrB != null ? hdrB.Find("WIldcard Display") : null;
                var bgRootT = wdB != null ? wdB.Find("Background") : null;
                CheckTrue(bgRootT != null, "（前提）`Header Filters ▸ WIldcard Display ▸ Background` 取得到");
                var bgQuads = bgRootT != null ? bgRootT.GetComponentsInChildren<ImageQuad>(true) : null;
                Check(bgQuads != null ? bgQuads.Length : 0, 9,
                      "★ 那颗 `Background` 是**九宫格 9 块**（= 原版「一颗 Image、一张 9 格网格」的等价物；"
                    + "🧨 改成单块/改成 `MenuDraw.Rect` 拉伸 ⇒ 红）");

                // ---- ③ 材质的同一性 + 身份 + 参数（灭自证的第一半）----
                Material m0 = null; int sameMat = 0, wrongShader = 0, noMr = 0;
                if (bgQuads != null)
                    for (int i = 0; i < bgQuads.Length; i++)
                    {
                        var q = bgQuads[i];
                        var mr = q != null ? q.GetComponent<MeshRenderer>() : null;
                        var mm = mr != null ? mr.sharedMaterial : null;
                        if (mm == null) { noMr++; continue; }
                        if (m0 == null) m0 = mm;
                        if (ReferenceEquals(mm, m0)) sameMat++;
                        if (mm.shader == null || mm.shader.name != "CardPresentation/WildcardShine") wrongShader++;
                    }
                Check(noMr, 0, "（前提）9 块都拿得到材质（`MeshRenderer.sharedMaterial` 不为 null）");
                Check(sameMat, 9, "★ 9 块 quad **共用同一个材质对象** —— 掩码按**顶点世界位置**算 ⇒ 一份就连续"
                                 + "（🧨 退回上一版「每块各算一份 uv 仿射 / 各自一份材质」⇒ 本格红）");
                Check(wrongShader, 0, "★ 9 块的材质 shader 名**全是** `CardPresentation/WildcardShine`"
                                     + "（🧨 没挂上 / 退回 `Sprites/Default` ⇒ 本格红）");
                CheckTrue(m0 != null, "（前提）拿到那份扫光材质");
                if (m0 != null)
                {
                    // ---- ④ 参数（原版现读值，⛔ 不是我们挑的）----
                    CheckTrue(m0.HasProperty("_TransitionWidth"),
                              "（前提）材质有 `_TransitionWidth`（shader 编不过时 `HasProperty` 会是 false）");
                    if (m0.HasProperty("_TransitionWidth"))
                    {
                        CheckNear(m0.GetFloat("_TransitionWidth"), 0.25f, 1e-5f, "★ 带宽 = **0.25**（原版 `m_TransitionWidth`）");
                        CheckNear(m0.GetFloat("_TransitionSoftness"), 0.5f, 1e-5f, "★ 软边 = **0.5**（原版 `m_TransitionSoftness`）");
                        CheckNear(m0.GetFloat("_TransitionColorFilter"), 6f, 1e-5f, "★ 颜色滤镜 = **6**（MultiplyAdditive）");
                        var sc = m0.GetColor("_TransitionColor");
                        CheckNear(sc.r, 1f, 1e-3f, "★ 带色 = **白**（收藏窗这颗是白的；🧨 套成 Deck 那颗 (3.4822,…) ⇒ 红）");
                        CheckNear(sc.g, 1f, 1e-3f, "……g 也是 1");
                        CheckNear(sc.b, 1f, 1e-3f, "……b 也是 1");
                    }
                    // ---- ⑤ 框 / 角度：一半原版参数、一半本段独立算的世界并集 ----
                    CheckTrue(m0.HasProperty("_ShineFrame"),
                              "（前提）材质有 `_ShineFrame`（掩码按世界位置算的那两个 uniform 之一）");
                    if (m0.HasProperty("_ShineFrame") && bgQuads != null)
                    {
                        // 独立算 9 块的世界并集（用 `MeshFilter.sharedMesh.bounds` 逐角变换 —— 与渲染器无关、确定性；
                        //   ⛔ 不回读 `WildcardShineLayer.FrameBounds`，那是被测实现自己的答案）
                        bool any = false; Vector3 bmin = Vector3.zero, bmax = Vector3.zero;
                        for (int i = 0; i < bgQuads.Length; i++)
                        {
                            var q = bgQuads[i];
                            if (q == null) continue;
                            var mf = q.GetComponent<MeshFilter>();
                            if (mf == null || mf.sharedMesh == null) continue;
                            var lb = mf.sharedMesh.bounds;
                            for (int c = 0; c < 4; c++)
                            {
                                var p = q.transform.TransformPoint(new Vector3(
                                    (c & 1) == 0 ? lb.min.x : lb.max.x,
                                    (c & 2) == 0 ? lb.min.y : lb.max.y, lb.center.z));
                                if (!any) { bmin = p; bmax = p; any = true; }
                                else { bmin = Vector3.Min(bmin, p); bmax = Vector3.Max(bmax, p); }
                            }
                        }
                        CheckTrue(any, "（前提）9 块里有 `MeshFilter + sharedMesh`（不然并集算不出来）");
                        if (any)
                        {
                            var ctr = (bmin + bmax) * 0.5f; var size = bmax - bmin;
                            var fr = m0.GetVector("_ShineFrame");
                            CheckNear(fr.x, ctr.x, 0.02f, $"★ `_ShineFrame.xy` = 那 9 块的**世界中心** x（实得 {fr.x:F4} vs 独立算 {ctr.x:F4}）"
                                                        + "｜🧨 `Reframe()` 没被调用 ⇒ 读到默认 (0,0) ⇒ 红");
                            CheckNear(fr.y, ctr.y, 0.02f, $"…… 中心 y（{fr.y:F4} vs {ctr.y:F4}）");
                            CheckNear(fr.z, size.x, 0.02f, $"★ `_ShineFrame.zw` = 世界**宽/高**（{fr.z:F4}×{fr.w:F4} vs 独立算 {size.x:F4}×{size.y:F4}）");
                            CheckNear(fr.w, size.y, 0.02f, "…… 高");
                            var rot = m0.GetVector("_ShineRot");
                            CheckNear(rot.x, -0.5f, 2e-3f, "★ `_ShineRot.x` = cos(**120°**) = **−0.5**（原版 `m_TransitionRotation = 120`）");
                            CheckNear(rot.y, 0.8660254f, 2e-3f, "★ `_ShineRot.y` = sin(**120°**) = **0.8660**");
                            CheckNear(rot.z, 1f / Mathf.Max(size.x, size.y), 2e-3f,
                                      "★ `_ShineRot.z` = **1/max(宽,高)**（🔶 自己设计那一档归一；判据见 `.shader` 文件头 ④）");
                        }
                    }
                }

                // ---- ⑥ 播放进度：建页那一拍 = 原版 `OnEnable` 的 `Play(resetTime:true)` ⇒ 从 0 起 ----
                bool pOn0 = win.WildcardShinePlayingForTest;
                CheckNear(win.WildcardShineRate, 0f, 1e-4f,
                          $"★ 建页那一拍 rate = **0**（= 原版 `OnEnable` 的 `Play(resetTime:true)` 把时间清零；现读 {win.WildcardShineRate:F4}）"
                        + "｜🧨 把 `BuildWildcardShine` 里那句 `Play(true)` 删掉 ⇒ rate 停在初值 1 ⇒ 红");
                CheckTrue(pOn0, "★ …而且**在播**（`Playing` = true；`Rate` 单独看分不出「没播」与「播完了」，两条必须并排）");
                win.StepWildcardShineForTest(0.5f);
                CheckNear(win.WildcardShineRate, 0.5f / 0.75f, 2e-3f,
                          $"★ 推进 0.5 s ⇒ rate ≈ **0.6667**（0.5 ÷ 0.75；实得 {win.WildcardShineRate:F4}）"
                        + "｜🧨 时长套成 1.0 s（Deck 那颗的）⇒ 读到 0.5 ⇒ 红");
                win.StepWildcardShineForTest(0.25f);
                CheckNear(win.WildcardShineRate, 1f, 1e-4f, "★ 再推进 0.25 s（累计 0.75 s）⇒ rate = **1**（扫完）");
                CheckTrue(!win.WildcardShinePlayingForTest,
                          "★ …**而且自己停了**（`m_WrapMode 0 (Once)`；🧨 把它接成 Loop ⇒ 仍在播 ⇒ 红）");

                // ---- ⑦ Neutral 闸（原版 `if (currentArmy != CardArmy.Neutral)`）----
                int armyWas = win.WildcardArmyForTest;
                win.RebuildCardsPageWithArmyForTest(ArmyIcons.Neutral);
                // （前提）断的是 `BuildWildcardShine` **那一刻**的阵营快照 —— ⛔ 不是重建**之后**的 `_wcArmy`：
                //   重建链尾句 `CheckFocusedArmy()` 会按 `WcRef` 命中的第一格把它重算回去（原版行为），
                //   读晚了只能读到那个重算值（判据 → `报告/D3_deck_rewards_collection.md` ②）。
                Check(win.WildcardArmyAtShineBuildForTest, ArmyIcons.Neutral,
                      "（前提）`BuildWildcardShine` **那一刻**读到的 `_wcArmy` 真的是 Neutral(0) —— 这是「下面那道闸真的被跑到」的直接证据"
                    + $"（重建**之后**的 `_wcArmy` 现读 {win.WildcardArmyForTest} = `CheckFocusedArmy` 按命中的第一格重算的结果，原版行为）"
                    + $"；快照实得 {win.WildcardArmyAtShineBuildForTest}（-2147483648 = `BuildWildcardShine` 一次都没跑到）"
                    + "｜🧨 删掉 `BuildWildcardShine` 里那句快照 ⇒ 读到哨兵 ⇒ 红；"
                    + "把 `BuildWildcardShine` 挪到 `RebuildCardsCells` **之后**（`_wcArmy` 已被重算）⇒ 红；"
                    + "⛔ 别改回读 `WildcardArmyForTest`（那是在断重建**之后**的值，恒不是 0 ⇒ 假红）");
                CheckTrue(!win.WildcardShinePlayingForTest,
                          "★ **Neutral ⇒ 不播**（原版 `WildcardDisplay.OnEnable` 的 `currentArmy != Neutral` 闸）"
                        + "｜🧨 把 `BuildWildcardShine` 里那道闸删掉 ⇒ 本条红");
                CheckNear(win.WildcardShineRate, 1f, 1e-4f, "★ ……而且 rate 停在 **1**（不亮）");
                win.StepWildcardShineForTest(0.2f);
                CheckNear(win.WildcardShineRate, 1f, 1e-4f,
                          "★ ……**推进 0.2 s 也不动**（真没在播；🧨 若只是在「播完」那一侧，rate 会变成 0.267 ⇒ 红）");
                // 反证：闸不是恒关（换回一个有脸的阵营 ⇒ 又播）
                win.RebuildCardsPageWithArmyForTest(armyWas);
                CheckTrue(win.WildcardArmyForTest == armyWas, "（收尾）阵营换回去了");
                CheckTrue(win.WildcardShinePlayingForTest,
                          "★ **非 Neutral ⇒ 又播了**（闸不是恒关；与上一条并排 ⇒ 恒开/恒关都只能对一半）");
                CheckNear(win.WildcardShineRate, 0f, 1e-4f, "（收尾）重开又从 **0** 起");
            }

            B97W6Assert.Run(win); _sink.Pass += B97W6Assert.Pass; _sink.Fail += B97W6Assert.Fail;   // 🆕 B97_W6：收藏窗卡池 ban 徽标（196.875px / 0.8×0.2 字框 / 默认关）；判据 → 判据/B97_藏卡ban徽标_施工单.md
            int total = _sink.Pass + _sink.Fail;
            if (_sink.Fail == 0) Debug.Log(P + $"=== 结束：{_sink.Pass}/{total} 全过 ✅ ===");
            else
            {
                var sb = new System.Text.StringBuilder(P + $"=== 结束：{_sink.Pass}/{total} 通过，**{_sink.Fail} 条失败** ❌ ===");
                // 🔴 **2026-10-11（A350 · 调度台裁定）**：这一串是失败表的【重列】（`Check` 里已经逐条打过）
                //   ⇒ 行首标记 = `失败重列：`，⛔ 不再是 `✗`（原来是 `✗` 时日志里 `✗` 行数 = 失败数 ×2）。
                foreach (var f in _sink.Failures) sb.Append("\n").Append(P).Append("   失败重列：").Append(f);
                Debug.LogError(sb.ToString());
            }
            if (Application.isBatchMode) EditorApplication.Exit(_sink.Fail == 0 ? 0 : 1);
        }

        /// <summary>🆕 **2026-10-20（`A1350` 第二批 · `W58-PTRPORT`）**：`MenuCheck.PointerFramePxFromDesign`
        /// （**设计帧 → 指针帧**的唯一换算口）的两态探针。
        ///
        /// <para>① **单位缩放**：新旧口**逐位相同** —— 新口 = `PointerFramePxFromDesign(DesignPx(t), t)`、
        /// 旧口 = `PointerFramePx(t)`（两个口各自独立，⛔ 不回读被测的换算算式）。
        /// ② **父链 ×2**：新口 = `C + (设计点 − C) × 2`（`C` = 画布中心 `(960,540)`）——
        /// 期望值用**建夹具时给的四个字面量**独立算；并且 `ButtonAt` 在**新口**上命得中探针、
        /// 在**未换算的设计点**上**命不中** ⇒ **结构上挡住「不乘 / 乘错基准」**。</para>
        ///
        /// <para>🔴 **灭自证**：`960 + (1300 − 960) × 2 = 1640` 是**夹具几何的字面量**，不是实现算出来的。
        /// 把实现改回「原样返回设计点」（= 旧行为）⇒ 读到 1300（差 340px）、`ButtonAt(1300,420)` 也落到
        /// 命中区 x∈[1440,1840] 之外 ⇒ 两条同时红。**把实现与断言一起改回旧写法 = 不可能同时绿**。</para>
        ///
        /// <para>⚠️ 父链带 2× 的节点必须落在**它自己父级的原点**（同 `CheckQuadPxRectUnderScaledParent`
        /// 的硬约束；判据 = `Shell/MenuDraw.cs` 的 `PosInDesignSpace` 适用范围）。</para></summary>
        static void CheckPointerFrameFromDesign(string what)
        {
            const float X1 = 1200f, Y1 = 320f, X2 = 1400f, Y2 = 520f;          // 设计矩形（离画布中心 ≥1 设计单位）
            const float Cx = (X1 + X2) * 0.5f, Cy = (Y1 + Y2) * 0.5f;          // 1300, 420
            const float Kcx = LayoutSpace.DesignPxW * 0.5f;                      // 960（ToDesignPixel 的不动点）
            const float Kcy = LayoutSpace.DesignPxH * 0.5f;                      // 540
            var tex = CardArt.Solid();
            CheckTrue(tex != null, $"（前提）{what}：纯色图取到了");
            if (tex == null) return;

            // ---------------- ① 单位缩放：新旧口逐位相同 ----------------
            {
                var host = new GameObject("A1350 PtrPort Unit Host");           // 世界原点 · 单位缩放
                try
                {
                    var node = MenuDraw.Node(host.transform, "A1350 PtrPort Unit Node", new PxRect(X1, Y1, X2, Y2));
                    CheckTrue(node != null, $"（前提）{what}：单位缩放探针节点建出来了");
                    if (node != null)
                    {
                        var d = MenuCheck.DesignPx(node);
                        var oldP = MenuCheck.PointerFramePx(node);
                        var newP = MenuCheck.PointerFramePxFromDesign(d, node);
                        CheckNear(d.x, Cx, 0.01f, $"（前提）{what}：单位缩放下设计点 x = {Cx}（实得 {d.x:F4}）");
                        CheckNear(d.y, Cy, 0.01f, $"（前提）{what}：……设计点 y = {Cy}（实得 {d.y:F4}）");
                        CheckTrue(newP.x == oldP.x && newP.y == oldP.y,
                                  $"★★ {what}：**单位缩放 ⇒ 换算口是恒等** —— 新旧口**逐位相同**"
                                + $"（新 ({newP.x:F6},{newP.y:F6}) vs 旧 ({oldP.x:F6},{oldP.y:F6})）"
                                + "｜🧨 若把「乘父链缩放」写成无条件乘：这里会差浮点尾数 ⇒ 红");
                    }
                }
                finally { Object.DestroyImmediate(host); }
            }

            // ---------------- ② 父链 ×2：新口 = C + (设计点 − C) × 2；ButtonAt 命中正确 ----------------
            {
                var host = new GameObject("A1350 PtrPort Probe Host");          // 世界原点 · 单位缩放（⛔ 见函数头）
                var mid = new GameObject("A1350 PtrPort Probe Scaled");
                mid.transform.SetParent(host.transform, false);                 // 局部位置进 0（被乘的那一级落在父级原点）
                mid.transform.localScale = new Vector3(2f, 2f, 1f);
                try
                {
                    var q = MenuDraw.Rect(mid.transform, tex, new PxRect(X1, Y1, X2, Y2),
                                          "A1350 PtrPort Quad", 4000);          // 队列压过真界面（保证命中路拿到它）
                    CheckTrue(q != null, $"（前提）{what}：父链 ×2 的探针 quad 建出来了");
                    if (q != null)
                    {
                        q.gameObject.AddComponent<WindowButton>();              // 只为让 `PointerLayer.CollectHits` 收它
                        CheckNear(q.transform.parent != null ? q.transform.parent.lossyScale.x : -1f, 2f, 1e-4f,
                                  $"（前提）{what}：**父链真的带 2×**（它是 1 的话下面全部恒等、等于没查）");
                        var d = MenuCheck.DesignPx(q.transform);
                        CheckNear(d.x, Cx, 0.01f, $"（前提）{what}：父链 ×2 下设计点仍是 {Cx}（实得 {d.x:F4}）");
                        CheckNear(d.y, Cy, 0.01f, $"（前提）{what}：……设计点仍是 {Cy}（实得 {d.y:F4}）");

                        var p = MenuCheck.PointerFramePxFromDesign(d, q.transform);
                        float wantX = Kcx + (Cx - Kcx) * 2f;                    // 960 + 340×2 = 1640
                        float wantY = Kcy + (Cy - Kcy) * 2f;                    // 540 + (420−540)×2 = 300
                        CheckNear(p.x, wantX, 0.01f,
                                  $"★★ {what}：父链 ×2 ⇒ 新口 x = **画布中心 960 + (设计 1300 − 960) × 2 = {wantX}**"
                                + $"（实得 {p.x:F4}）｜🧨 不乘（原样返回设计点）⇒ 得 1300 ⇒ 红");
                        CheckNear(p.y, wantY, 0.01f,
                                  $"★★ {what}：……y = **540 + (420 − 540) × 2 = {wantY}**（实得 {p.y:F4}）");
                        CheckNear(p.x, MenuCheck.PointerFramePx(q.transform).x, 0.01f,
                                  $"★★ {what}：新口 x 与独立的 `PointerFramePx(q)` **同帧**（两个口互证）");
                        CheckNear(p.y, MenuCheck.PointerFramePx(q.transform).y, 0.01f,
                                  $"★★ {what}：……y 同帧");

                        var pl = PointerLayer.Instance;
                        CheckTrue(pl != null, $"（前提）{what}：`PointerLayer` 在（真命中路要它）");
                        if (pl != null)
                        {
                            var hitNew = pl.ButtonAt(p.x, p.y);
                            CheckTrue(hitNew != null && hitNew.transform == q.transform,
                                      $"★★ {what}：**在【指针帧】的点上命得中探针 quad**"
                                    + "（拿到 " + (hitNew == null ? "**null**" : "`" + hitNew.name + "`") + "）"
                                    + "｜🧨 把新口改回「原样返回设计点」⇒ 这一点落到命中区外 ⇒ 红");
                            var hitOld = pl.ButtonAt(d.x, d.y);
                            CheckTrue(hitOld == null || hitOld.transform != q.transform,
                                      $"★★ {what}：**未换算的设计点命不中它**（灭自证的另一半 —— 防「在哪儿都命中」）"
                                    + "（拿到 " + (hitOld == null ? "**null**" : "`" + hitOld.name + "`") + "）");
                        }
                    }
                }
                finally { Object.DestroyImmediate(host); }
            }
        }

        static float DeckCellWpx() { return CollectionWindow.DeckCellW; }
        static float DeckCellHpx() { return CollectionWindow.DeckCellH; }

        /// <summary>🆕 **2026-10-14（`A1714`）那两节用的探针窗**（照 `Editor/ShellScene.cs` 的 A1709 探针写法，
        /// 它本来也是这么造的：`new GameObject` → `AddComponent&lt;GameWindow&gt;` → 赋两个必填参数 → `AttachToAnchor`）。
        /// ⚠️ `type` / `placement` **必须显式赋**：本仓那两枚哨兵会让 `AttachToAnchor` **出声**
        /// （`[Win] … 没有显式赋值 …` 两条 `LogError`，见 A166 / A672）—— 探针不该给日志添脏。</summary>
        static GameWindow MakeProbeWindow(string name)
        {
            var go = new GameObject(name);
            var w = go.AddComponent<GameWindow>();
            w.type = WindowType.Fullscreen;               // 与常驻主菜单那一扇同档（`MainMenuWindow` 实读 = 0）
            w.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(w);
            return w;
        }

        static void SaveScene()
        {
            // 收藏窗是**挂在壳锚点上的窗口**，不该有自己的场景 ⇒ 这里只存一张自检场景供人工看
            var path = "Assets/CardPresentation/Scenes/CollectionCheck.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            SceneStubAudit.Run(path);   // 🆕 A1506：存盘后自证「不许有 !u!115 存根」（干净时零输出）
            Debug.Log(P + $"  自检场景 → {path}");
        }
}
