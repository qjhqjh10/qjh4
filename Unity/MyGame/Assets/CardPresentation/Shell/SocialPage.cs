// SocialPage.cs — 社交窗每一页的公共底座（原版 `WindowTabBase<SocialMenuWindow>`）。
// 🔴 **2026-10-13 从 `SocialWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**——
//    它是 `AlliancesTab` / `FriendsTab` 的基类，`AddComponent<派生>()` 时 Unity 要把整条链都解析成脚本资产）。
//    出处与纪律见 `SocialWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>社交窗一页的公共底座。原版是 `WindowTabBase&lt;SocialMenuWindow&gt;`。
    /// 画图一律走宿主窗那套（`Win.Rect` / `Win.Art`）—— **画图层只有 `MenuDraw` 一份**，别在这再写一份。</summary>
    public abstract class SocialPage : WindowTabBase
    {
        /// <summary>宿主窗（`BuildTabContents` 建完就赋）。</summary>
        public SocialWindow Win;

        /// <summary>本页在 `Tabs` 表里的下标（0 = Alliances · 1 = Friends）—— 决定队列档。</summary>
        protected abstract int PageIndex { get; }

        /// <summary>本页的队列起档（两页各占 10 个号，互不重叠）。⚠️ **public 是给 `SocialView` 用的**。</summary>
        public int Q { get { return SocialWindow.QPageBase + PageIndex * 10; } }

        protected Transform Root { get { return transform; } }

        // 🔴 **2026-10-14（A753 = A744 的「全删」）：本类原来那个 `Clip` 字段（连同 `ClipNow` / `SetClip`
        //   这一对读写口）【已整体删掉】** —— 本类里那四处转发（`Rect` / `Nine` / `Text` / `Hit` / `Cosmetic`）
        //   现在一律传 `clip = null`，裁切由**父链上那颗 `ViewportClip` 节点**说了算
        //   （三段优先级见 `Shell/ViewportClip.Resolve`；⛔ 别再给本类补回「显式覆盖」那一档）。
        //
        //   📌 **三处视口的原版真值（判据，⛔ 别删）**（普查 §A·1；⇐ **那三颗节点就是照这三个矩形建的**）：
        //   ① `Open Alliances>Viewport` = 360.99,337.29→1874.90,1079.77（`Shell/AlliancesTab.cs` 的 `OpenViewportR`）；
        //   ② `Friends Container>Viewport` = 332.15,314.80→1875.80,1080.06（`Shell/FriendsTab.cs` 的 `ContainerR`）；
        //   ③ `MemberList>Scroll View>Viewport` = 369.67,493.63→1880.67,1080.05（`Shell/AllianceMemberTab.cs` 的 `MemberScroll.Viewport`）。
        //   （另：奖杯那一格 `TrophiesWindow>Scroll Rect` 的视口 = `AllianceMemberTab.TrophyScrollR`。）
        //   ⚠️ 三处**软边**：只有奖杯那一格非 0（`(0,50)`）—— 现在写在节点上（`TrophyClipSoftness`）。

        /// <summary>取图（走宿主窗那一个入口；取不到会记进 `MissingArt`）。
        /// ⚠️ `SocialView`（子视图）也要用 ⇒ **public**。</summary>
        public Texture2D ArtOf(string name) { return Win != null ? Win.Art(name) : null; }

        protected static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
        protected Transform Node(string name, PxRect r) { return MenuDraw.Node(Root, name, r); }

        /// <summary>按矩形摆一张图（`art == null` = 纯色块）。取不到图 ⇒ 返回 null 并**记进宿主的 `MissingArt`**。
        /// ⚠️ **public 是给 `SocialView` 用的**（子视图没有自己的 `SocialPage` 基类）。</summary>
        public ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                              Color? tint = null, bool keepAspect = false)
        {
            var tex = art == null ? CardArt.Solid() : Win.Art(art);
            // 🔴 **2026-10-14（A753）**：尾参原来是本页的 `Clip`（显式覆盖那一档）—— 字段已整体删掉
            //    ⇒ 走缺省 `clip = null`：由**父链上那颗 `ViewportClip`** 说了算。
            return MenuDraw.Rect(parent, tex, r, name, Q + qOff, tint, keepAspect);
        }

        /// <summary>九宫格（原版 `Image.Type = Sliced`）。`border` 按**贴图原始像素**给（L,B,R,T）。
        /// 🔴 **2026-10-14（A753）**：从前这里把本页的 `Clip` 当**尾参**传下去（2026-10-03 A25① 才补上 ——
        /// 当时 `Rect` / `Text` / `Hit` / `Cosmetic` 都传了、**唯独九宫格这一路漏了**，图会画到视口外）。
        /// 那个字段已整体删掉 ⇒ **现在不传 `clip`**（走 `MenuDraw.Nine` 的缺省 `null`），裁切由
        /// **父链上那颗 `ViewportClip`** 说了算 —— 与 `Rect` / `Text` / `Hit` / `Cosmetic` 同一条路。
        /// ⚠️ 这一路（与那四路）都是「**整块**在框外 ⇒ 连节点一起不建」，⛔ 不是逐像素裁。</summary>
        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                               Color? tint = null, bool fillCenter = true)
        {
            var tex = Win.Art(art);
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border, tex.width, tex.height, Q + qOff, tint, fillCenter, name);
        }

        /// <summary>限宽换行 + 可选自适应字号（原版 `m_TextWrappingMode=1` + autosize）。
        /// 🔴 别用 `SetFontSize(px/108)` —— 那会大 2.7 倍；这条路走 `SetGlyphHeight`。
        /// <para>🆕 **2026-10-03（A25④）求交只剩一份**：这一处原来**自己判了一遍横轴**
        /// （`r.x2 &lt;= Clip.x1 || r.x1 &gt;= Clip.x2` —— 那句里的 `Clip` 就是本页那个**已于 A753 删掉的**
        /// 显式覆盖字段）⇒ 与 `Rect` / `Nine` / `Hit` / `Cosmetic` 那四路
        /// **不是同一条判据**（那四路都转调 `MenuDraw.ClipRect`）。在**横向**滚动区里两种写法等价，
        /// 而社交这三处视口**全是纵向**的 ⇒ 纵向越界的文字照样画到框外，而且**是静默的**
        /// （断言量「节点在不在」，量不到「画多出去了」）—— 这正是把三处滚动接上之后会**真的**现形的缺陷。
        /// ⇒ 收口成 `MenuDraw.ClipRect`（它**转调** `MenuDraw.Visible` —— **全工程唯一一份**求交），
        /// 与 `MenuWindowBase.Text` 同一口径。⚠️ 2026-10-07 更正（铁律 5 / A12①）：原文只写到 `ClipRect`
        /// 为止；收口后唯一一份是 `Visible`，`ClipRect` = 它的「顺带夹出可见矩形」版。
        /// ⚠️ **2026-10-14（A753）**：那个「显式覆盖」的 `Clip` 字段已整体删掉 ⇒ 本口现在**只**走
        /// 「父链节点」那一支（`clip` 实参恒传 `null`）；没有节点时与旧行为一字不变
        /// （`ViewportClip.Resolve` 第 3 支返回 `null` ⇒ `ClipRect` 第一句就是 `return true`）。
        /// ⚠️ 仍然是「**整块**在框外就不建」（文字没法截 uv；部分越界的字按原样画）—— 这条缺口在
        /// `MenuWindowBase.Clip` 的注释里记着（`项目任务.md` §三 第 29 条 A9），本处**同一条口径**、不是新缺口。</para>
        /// <para>🔴 **2026-10-08（A213）新增 `wrap`**：本行**恒折行**是错的 —— `MenuDraw.TextBox` 第一句就是
        /// `SetWrapWidth(框宽)`，而那个**无条件**把 `m_TextWrappingMode` 设成 `Normal(=1)`
        /// （`Core/TmpFont.cs` 的 `SetWrapWidth`）⇒ 凡走这里的件**一律折行**，可**原版逐件不同**。
        /// 判例（A213 点名的那一处）：`Shell/AlliancesTab.cs` 建盟页 `Price Display Button > Price Display > text`
        /// （出厂文本 `'1000'`）原版实读 = **`折行=0 · auto[13.46~40] · Center/Capline`**
        /// （`python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`）
        /// ⇒ 我们原来**真偏离**。
        /// 📋 **`AlliancesTab` 那 15 处逐条核过的原版 `折行`**（同一条 dump 的 `折行=` 列）：
        /// **0** = `Alliance Header Buttons/Tab buttons/Generic Tab UI Button {Search,Create}/Button Text`
        /// （`Join`/`Create`，fs60）· `List View/Search Field/Text Area/Placeholder`（`Search`，fs50）·
        /// 行尾 `Join`/`Reject` 的 `Button Text`（fs36.65/44）· `Price Display Button/…/Price Display/text`；
        /// **1** = `Invitations/Title` · `Open Alliances/Title` · 行里的 `Title`/`Region`/`Members Header`/
        /// `Member Count`/`Ranking Header`/`Ranking Value` · `Create Alliance Text` ·
        /// `Name input title` / `Desc input title` / `Select Language` / `Select Privacy`。
        /// ⇒ 只有那 **4** 处改传 `wrap: false`。
        /// <para>🔴 **2026-10-11（A317 · 已落地）：`autoMinPx` 与 `wrap` 的缺省值【删掉、形参必填】。**
        /// 判据 = `资料/普查产出_1010/调度台_口径裁定_1011.md` §A258 · `V4a_壳与共用件口径.md` §Q4：
        /// V4a **亲跑 `menu_dump`** 量过三族原版（`Profile` 112/44 · `Social` **56/44** · `Collection` 26/28）
        /// —— **取值本来就是混的** ⇒「统一到哪一套」是**伪问题**（缺省值**不是原版概念**，原版只有
        /// **逐个节点**的真值）；而缺省值今天**真在载荷**（不传就跟着它走）⇒ 去掉它才能**倒逼逐处现读**。
        /// ⛔ **别改成某一边的缺省值**（往任一边统一都当场回归另一批站点）。
        /// ⚠️ C# 要求**必填形参排在所有可选形参【之前】**（`CS1737`）⇒ `wrap` 必填就得连它前面的
        /// `autoMinPx` 一起定（前例 = 同族的 `CollectionWindow.TextAligned` **三个形参全去掉缺省**，
        /// 但那口只有两个调用点）。</para>
        /// <para>✅ **`alignLeft` 的缺省【已删 · 形参必填】—— 2026-10-12（A323 · 收尾半落地）**：
        /// 判据与理由同 `wrap`（缺省值**不是原版概念**，原版只有**逐个节点**的真值）⇒ 去掉它才能**倒逼逐处现读**。
        /// 全仓 **13 处**阻塞点各补了一句实参（**零行为变化**：缺省本来就是 `true`，而那 13 处原版**全是 `Left`**）：
        /// · `Shell/AlliancesTab.cs` **9 处** = `Placeholder`（`List View/Search Field/Text Area`）·
        ///   `Invitations/Title` · `Open Alliances/Title` · 行 `Title` · 行 `Region` · 行 `Ranking Value` ·
        ///   `Create Alliance Text` · `Field` 的标题（`Name`/`Desc` 两处共用）· `Dropdown` 的标题
        ///   （`Language`/`Privacy` 两处共用）；
        /// · `Shell/FriendsTab.cs` **4 处** = `Placeholder` · `Search Player` · `Friends Title` · 行 `Friend name`。
        /// 📌 逐处真值 = **本批现读** `python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant"
        /// --depth 16 --md` 的 `对齐=` 列（`AlliancesTab` 9 处 = `Left/Midline`×4 + `Left/Middle`×5 ——
        /// ⚠️ 两个标题各被 `Name`/`Desc`、`Language`/`Privacy` **共用**，**两两都是 `Left`**）；
        /// `Friend name` 那处在**独立根**上（`menu_dump … "Friend Info Item" --depth 6 --md` ⇒ `Left/Midline`），
        /// 同批 `普查产出_1011/W5_A307_A255_A258.md` §五·4 已核过；这一族的逐处真值**不再另抄一份**。
        /// <para>⚠️ **上一轮（C1）的记账 —— 保留痕迹**：A323 立项时以为**唯一的阻塞点是 `AllianceMemberTab` 那 13 处**；
        /// A319 把**那 13 处逐条现读补全**（真值表 → `Shell/AllianceMemberTab.cs` 的 `Toggle` 上方），
        /// 但**真删缺省才露出真阻塞点** —— C1 实跑类型检查（`TMPDIR=/tmp/wf_c1 bash d:/4/Unity/工具/typecheck.sh`）
        /// 后 `CS7036` 一共 **13 条**，**全部**落在 `AlliancesTab.cs` / `FriendsTab.cs` 这两个**当时白名单外**的文件上。</para>
        /// <para>⚠️ 上一批（A258 · W5）已把「还没显式声明 `wrap`」的 **21 处逐处按原版实读补齐**
        /// （`AlliancesTab` **11** · `FriendsTab` **3** · `AllianceMemberTab` **7**）⇒ 那三个文件
        /// **加了缺省值之后一个字都不用再动**；先前那几批已经传 `false` 的 **11 处**也一个字没动。
        /// 📋 真值出处 = `资料/普查产出_1008/波C3_A212其余_A213_A214.md` §A213 表 A/B。
        /// ⚠️ `wrap: true` = `MenuDraw.TextBox` 里那句 `SetWrapWidth` 的既有效果（`Normal`）
        /// ⇒ 与原来的缺省逐字等价。</para>
        /// </para>
        /// <para>🔴 **本批（A317）唯一的调用点改动 = 一处「探针」**：
        /// `Editor/MainMenuScene.cs` 的 `A25①` `Clip` 探针（原来是 7 实参 ⇒ `CS7036`）补成 9 实参。
        /// （本文件里还有一条同形的探针，但那一条**写在注释里**、不是真调用点。）
        /// ⛔ **别在这里再补一个「转调专用」的重载来绕开必填** —— 那是把唯一的口子藏起来。</para>
        /// <para>⚠️ 同族的 `CollectionWindow.TextAligned`（2026-10-08 已去掉缺省、档位还换成了原版
        /// `m_TextWrappingMode` 的 **`int`** 原文）与 `PlayerProfileWindow.ProfilePage.Text`
        /// （🆕 **2026-10-12（A323）`alignLeft` 也已去掉缺省** —— 它的 4 处阻塞点
        /// `AchievementsMenu`(3) + `BattleLogTab`(1) 本批进了白名单、各补了 `alignLeft: false`）
        /// ⇒ **本口是这一族里最后一个去掉 `alignLeft` 缺省的**（2026-10-12 · A323 收尾半）
        /// ⇒ **三处 `wrap` 的类型仍不统一**（一处 `int`、两处 `bool`）
        /// —— 这一条按调度台口径**如实留着**，不自己拍。</para>
        /// </para>
        /// <para>⚠️ 本口只表达 0 / 1 两档；第三档 `3` 由调用点自己在 `Text(...)` 之后
        /// <see cref="Label.SetWrappingMode"/>（先例 = `Deck/DeckRuntime.cs` 的搜索框）。</para>
        /// <para>🔴 **2026-10-16（A822）：本口【不】自己调 `MenuDraw.ClipText` —— 这是有意的，别补。**
        /// 全文件 `grep ClipText` = **0 命中**（本注释**写进来之前**跑的；今天再跑会命中本注释自身）
        /// —— R2 的 A799 全量表顺带查出，见
        /// `资料/普查产出_1015/R2_A799全量表.md` §4·2：账的「16 组包装器」里只写了 `SocialWindow.Text`，
        /// 没往下解）。**「部分越界的那半边」由下面 `MenuDraw.TextBox` 的末尾那一句裁掉**
        /// （A781 · 2026-10-13：`if (_st.RenderClip.HasValue) ClipText(lb, clip, clipSoftness);`，
        /// 见 `Shell/MenuDraw.cs` 的 `TextBox`）—— 本口恒传 `clip = null` ⇒ 走**父链上那颗 `ViewportClip`**。
        /// ⛔ **别在这儿再补一句 `ClipText`**：那就成了「同一颗字裁两刀」（= 另一条账 **A821** 记的那 7 个包装器）
        /// —— 两刀**同框幂等、画面无差异**这一点照旧。
        /// ⚠️ **2026-10-16 就地订正（铁律 5）**：这一句后面原来接着写「但会把 `TextClipUnavailable` /
        /// `TextClipUploadSkipped` 两个诊断计数**数两遍** —— 而 `MenuDraw.TextCore` 那个内层正是为了躲这个
        /// 才拆出来的」。**「数两遍」那半已过期**：A821（2026-10-15）已把口径从「次数」改成「**唯一标签**数」，
        /// 同一颗标签**只记一次**（`Battle/Label.cs` 的 `Label.TakeClipFailMark`；两处调用点 =
        /// `MenuDraw` 里那两处 `TakeClipFailMark` 调用点）⇒ **计数不再虚高**
        /// （口径原文 → `资料/已知的坑.md` 的 2026-10-15 §1）。⚠️ `TextCore` 那个内层**照旧拆着** ——
        /// 它免掉的是那一刀**本身**（白做一次 + 被重排冲掉），**不只是计数**
        /// （同口径 → `Shell/MenuDraw.cs` 的 `TextCore` 头 `:1674-1676`）。
        /// 🔴 **它真的带电**：三个使用者（`AllianceMemberTab` / `AlliancesTab` / `FriendsTab`）**各挂一颗**
        /// `ViewportClip`（A435 阶段 2），本口恒传 `clip = null` ⇒ 框由父链那颗节点解析（不是「没人管」）。
        /// 本口第一句 `ClipRectAbove` 那是**另一件事**：**整块**在框外 ⇒ 连节点一起不建
        /// （`MenuDraw.Visible` 那一份求交；A25④ 收口 · A798 把同一道闸也加进了 `MenuDraw.Text`/`TextBox`）。</para>
        /// <para>⚠️ **A822（2026-10-15 查出）**：上面这条「顺带被 `MenuDraw.TextBox` 裁上」的链路
        /// **没有任何断言钉着** —— `Editor/MainMenuScene.cs` 的 A25① 探针只验了 `Rect`（跨边被截）、
        /// **整块**在外的 `Text`（不建）、`Hit`（跨边夹到视口沿）；**缺的是**
        /// 「`SocialPage.Text` 建出来的、**部分越界**的那一段字，它的 mesh 被截到视口沿」这一条。
        /// 断言宿主 `Editor/MainMenuScene.cs` 不在写手白名单里 ⇒ 由调度台另派（判据 = 上面的 A781 那一句）。</para>
        /// <para>🔴 **2026-10-12（A406）：追加尾参 `autoMaxPx` / `autoBasePx`** —— 语义、量纲、缺省行为
        /// **与 `MenuDraw.TextBox` 的同名形参逐字相同**（判据与全量说明 → `Shell/MenuDraw.cs` 的 `Text` 头）：
        /// 原版那一颗的 `m_fontSizeMax` / `m_fontSizeBase`（**画布 px**）；**都 `&lt;= 0` ⇒ 旧行为**
        /// （上限 = `fontPx`、base = 调用方那一档）。逐站实读值只填在 `Shell/AllianceMemberTab.cs`
        /// （A406 那一批的白名单内）。</para>
        /// <para>🔴 **2026-10-13（A414）就地订正 —— 原来这一句写的是「⇒ 本文件与 `AlliancesTab` /
        /// `FriendsTab` 的既有调用点**一个都不用改**」，**那是错的**（铁律 5：两份说法打架比没有更糟）。
        /// 真相：那句话只对「**加形参本身**不会让调用点编不过」成立（缺省 `&lt;= 0` ⇒ 旧行为，**编译上**
        /// 一个都不用改）；它**不等于**「那 19 处**没有真值可填**」。A414 逐处现读原版后确认：
        /// **`AlliancesTab` 15 处 + `FriendsTab` 4 处**里 **17 处**都有**不等于**旧行为的真值
        /// （`m_fontSizeMax` ≠ 我们传的 `fontPx`，或 `m_fontSizeBase` ≠ 调用方那一档），**已全部填上**。
        /// 逐处表（含 2 处「不适用」）→ `资料/普查产出_1013/W403_A414_对齐与字号.md` §四。</para>
        /// </summary>
        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                          int qOff, float autoMinPx, bool wrap, bool alignLeft,
                          float autoMaxPx = 0f, float autoBasePx = 0f)
        {
            // 🔴 求交那一份 = `MenuDraw.Visible`（本行走它的夹取版 `ClipRect`；别在这儿再写一遍 `Max/Min`）。
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）→ 🆕 2026-10-14（A753 · A744 的「全删」）**：走 **`ClipRectAbove`**
            //   （节点态那一版）——
            //   三处生产站点的 `SetClip(...)` **已整对删除**、那个「显式覆盖」字段也**已整体删掉**
            //   ⇒ 这里恒传 `null`：框由**父链上那颗 `ViewportClip`** 给。沿用裸 `ClipRect` 的话
            //   这条守卫会**永远放行**（静默：压在视口外的整段文字照建，而画面「看着没问题」）。
            if (!MenuDraw.ClipRectAbove(parent, r, null, out _)) return null;
            // 🔴 **2026-10-16（A799 · 生产 9/9）：这一句【会新裁】= 目的，⛔ 别「修」。**
            //   本口自己不调 `MenuDraw.ClipText`（A822 的有意选择，理由不在这儿抄第二份 —— 看文档头）
            //   ⇒ `clip` 恒 `null` ⇒ A781 起 `MenuDraw.TextBox` 末句沿父链解析到**使用者那一棵**的节点：
            //   `AllianceMemberTab` 的 `v.Text(row, …)`（`:1381` / `:1400` / `:1402` / `:1458` 四颗，
            //   `row` 在 `mcontent`（该文件 `:1137`）之下、VC = 该文件 `:1125` 那颗 `ViewportClip`；
            //   `AlliancesTab` / `FriendsTab` 同形）⇒ 压在视口边上的那一段字第一次被夹到视口沿。
            //   🔴 这一处是 **R2 新查出的第 9 处**（账的「16 组包装器」里只写了本口、没往下解父链，
            //   见 R2 §一② 与 §4·2）——「整族漏了」的意思：A781 顺带把它补上了，**这是好事**。
            //   ✅ 为什么可以：原版 `RectMask2D` 对文字与图片一视同仁（同页 `Rect` / `Hit` 早在裁）⇒ 更贴原版；
            //   下面两句都带 `lb != null` 守卫（A798 起「整块在框外 ⇒ 返回 `null`」）。
            //   ⚠️「这条链没有断言钉着」= **A822** 那条账（宿主 `Editor/MainMenuScene.cs` 不在本件白名单）；
            //   A799 这半边只要求「把『会新裁』记在站点上」。判据全文 → R2 §一① #9 / §4·2。
            var lb = MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, Q + qOff, autoMaxPx, autoBasePx);
            // 🔴 **2026-10-08（A213）**：`wrap: false` ⇒ 按原版把模式显式落成 `0`。
            //    ⚠️ 必须在 `TextBox`（里面已跑过 `SetWrapWidth` / `SetAutoFitBox`）**之后**、`AlignLeft` **之前**：
            //    `SetWrapping` 会重排并挪 TMP 子节点（`ForceRelayout`，A205），对齐要落在它之后。
            if (lb != null && !wrap) lb.SetWrapping(false);
            if (lb != null && alignLeft) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>一个**透明点击区** + `WindowButton`（`PointerLayer` 扫的就是它）。
        /// 🆕 **2026-10-03**：这一路也吃裁切 —— 视口外的命中区**不建**、压在视口边上的**截到视口内**
        /// （判据 = 原版 `RectMask2D` 的射线那一面，见 `MenuDraw.Hit` / `ClipRect`；本页的 `Rect`
        /// 早就带了、**只有这里漏了** ⇒ 补上才是同一套）。
        /// 🔴 **2026-10-14（A753 · A744 的「全删」）**：尾参那个「本页 `Clip`」已**整体删掉** ⇒ 本路与
        /// `Rect` / `Nine` / `Text` / `Cosmetic` 一样走缺省 `clip = null`：由**父链上那颗 `ViewportClip`**
        /// 说了算（`MenuDraw.Hit` 内部自己 `Resolve`，命中那一路要的是「裸框 + `pad`」）。
        /// <para>🔴 **2026-10-16（A822）**：本口与 `Text` **同一族** —— 本文件零 `ClipText`（有意的，理由写在
        /// `Text` 的头里，⛔ 别抄第二份）。命中那半边的裁法是 `R ∩ (V − pad)`（`MenuDraw.Hit` 自己算，
        /// 与文字那半边的 `ClipText` 不是同一条路），而且**它已经有断言**（`Editor/MainMenuScene.cs` 的
        /// `edgeHit` 那条：「跨在视口边上的命中区 quad 也被截到同一条边」）；**文字那半边没有**
        /// （= `A822` 要补的那一条，判据与落点见 `Text` 的头）。</para></summary>
        public Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick,
                             ImageQuad target = null, string art = null,
                             string hoverArt = null, string pressedArt = null, string clickCue = null)
        { return MenuDraw.Hit(parent, name, r, Q + qOff, onClick, target, art, hoverArt, pressedArt,
                              clickCue: clickCue); }

        /// <summary>点了**还没接**的东西 —— 一律出声（红线：不许静默失败）。</summary>
        public static void Say(string what)
        { Debug.Log("[Social] " + what); }

        /// <summary>🆕 2026-10-03（A25④）：把社交这三处的滚动区登记给指针层（滚轮才会找到它）——
        /// **三处都走这一份**（收口，别在页里各写一遍 `PointerLayer.RegisterScroll`）。
        /// 🔴 **不静默**：指针层还不在场景里时 `PointerLayer.RegisterScroll` 是**空转**（登记表都没建）⇒
        /// 滚轮永远落不到这一格上，而画面看着完全正常（正是本批要治的那类缺陷）。
        /// 正常路径上它一定在：`PointerLayer` 与窗口管理器**同生共死**
        /// （`WindowsManager.Awake` / `EnsureHost` 都调 `PointerLayer.Ensure`）。
        /// 🔴 **2026-10-04（A35⑧）补第二条判据：`Owner` 也要判** —— `PointerLayer.PruneScrolls`
        /// （`PointerLayer.PruneScrolls`）把 `Owner == null` 当**死的**条目删掉（那条注释写着
        /// 「`s.Owner == null` 有两个来源：**指向的对象被销毁**（Unity 的假 null）与**从没设过**」）
        /// ⇒ 忘设 `Owner` 的滚动区**下一次登记时会被悄悄删掉**，而这条路原来一声不响
        /// （画面照样正常，只是滚轮永远落不上去）。**出声，但仍然登记**（不改行为）。</summary>
        public static void RegisterScroll(MenuScroll s)
        {
            if (s == null) return;
            if (PointerLayer.Instance == null)
            {
                Debug.LogWarning("[Social] 指针层还不在场景里 ⇒ 滚动区**没登记上**（滚轮不会落到它上面）；"
                               + "先走 `WindowsManager.EnsureHost()` 再开窗。");
                return;
            }
            if (s.Owner == null)
                Debug.LogWarning("[Social] 登记滚动区时 `Owner` 是空的 ⇒ 它会被 `PointerLayer.PruneScrolls` "
                               + "当**死条目**删掉（滚轮不会落到它上面）。登记之前要先 "
                               + "`s.Owner = 宿主 GameObject`（本页三处 + `BattleLogPopup` 都设了）。");
            PointerLayer.RegisterScroll(s);
        }
    }
}
