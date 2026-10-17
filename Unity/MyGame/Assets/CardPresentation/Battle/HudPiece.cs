// HudPiece.cs — 战斗 HUD 里一个「件」的**适配层**（`T09` 整层换 uGUI 的过渡件）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【它解决什么】T09 要的把那 ~52 处 `ImageQuad` 件换成 uGUI 件，但那些件在**别处**被用了 ~140 次
//   （`.Texture` 32 · `.gameObject` 32 · `.transform` 25 · `.WorldW/H` 28 · `.SetAspect` 8 · `.SetTint` 7 ·
//     `.Contains` 3 · `.Tint` 2 · `.SetTexture` 2 · `.SetAnchorPosition` 1）。
//   如果直接把这 39 个字段的类型从 `ImageQuad` 改成 `RectTransform`，那 140 处**每处都要改**，
//   而每一处都是**看不见的改动**（本工程那条血规：只改半边在 16:9 下逐位等价、全绿，坏只在别的宽高比上现形）。
//   ⇒ 用这一层**把差异挡住**：字段类型 `ImageQuad` → `HudPiece`，**140 处用法一行不用改**。
//
// 🔴 **一条铁律（本件存在的全部意义）**：一个 `HudPiece` **只能是两种之一** ——
//   **要么**旧的 `ImageQuad`（世界空间 quad），**要么**uGUI 的 `RectTransform + Image`。
//   ⛔ **绝不允许「两边都建」**（那就是同一件画两遍、而且两边会漂不同步）。
//   本件每个写口都先判 `IsUgui` 再走对应那一支，且**没有**任何一处会同时改两边。
//
// ⚠️ **它是【临时】的**：T09 第 7 步（收尾）要把旧那一支整个删掉、本件随之退场。
//   ⛔ 别在它上面长新功能（它不是抽象基类，是**迁移脚手架**）。
//
// 【判据：几个读回口在 uGUI 那一侧怎么算】
//   · `WorldW/WorldH` —— 🔑 **用 `RectTransform.GetWorldCorners()` 直接量世界尺寸**，
//     ⛔ **不是** `sizeDelta × 某个系数`：canvas 从「设计 px」到「世界单位」的换算随分辨率变
//     （1 单位 = `10/屏高` 世界单位），写死系数就是在**重犯 A2092 那个「两套尺」的错**。
//   · `Contains(world)` —— 同样用 `GetWorldCorners()` 得到的**世界四边形**判内点。
//     这样它与旧 quad 那条路**量的是同一件事**（画出来的那个矩形），且非 16:9 下也成立。
//   · `Texture` —— uGUI 那一侧取 `Image.sprite.texture`。
//   · 🆕 `SizePx`（**2026-10-16 `EW11` 补**）—— **真设计 px**（1920×1080 那一套）：
//     quad 支 = `WorldW/H × 108` · uGUI 支 = **转调 `HudCanvas.SizePx(Rect)`**（= `sizeDelta`）。
//     🔴 **凡「读『画出来多大』」一律走它** —— `WorldW/H` 是**世界单位**，拿它 `× 108` 在非 16:9 下
//     只有真设计 px 的 **0.75 倍**（批处理实测 640×480；判据见 `WorldW` 那两段 + `SetWorldHeight` 的 doc，
//     先例 = `HudCanvas.SizePx` / `BattleLogPanel.FrameSizePx`）。详见它自己的 doc（含**点锚**那条边界）。
//
// 🔴🔴 **写回口（`NudgeWorld` / `SetWorldHeight`）那两个 `108` —— 2026-10-16（`EW2`）现核后的口径，⛔ 别按
//   「硬编码的世界→画布系数」把它换成实测系数**（批处理下 x 108 / y 144，见 `T09` §七·8）：
//   **它俩不是「世界单位 → 画布单位」的换算，是 `LayoutSpace.Px()` 的【逆】** —— 现存三个调用点
//   （`BattleDriver.cs:15850/15851/16304`）**都先套了 `Px()`**，`× 108` 只是把那个 ÷108 还原成**设计 px**。
//   换成 144 ⇒ 每一处偏移/尺寸乘成 **1.333 倍**（回合灯会离开牌堆底板、卡背高会超原版 `m_SizeDelta` 33%）。
//   完整判据与「真要动就必须成对动」的清单 → 那两个口的 doc。
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>战斗 HUD 里的一个件：**旧 `ImageQuad` 世界空间 quad** 或 **uGUI `RectTransform + Image`**，
    /// 二选一。见文件头（含那条「⛔ 不许两边都建」的铁律）。</summary>
    public sealed class HudPiece
    {
        readonly ImageQuad _quad;          // 旧路（非 null ⇒ 走它）
        readonly RectTransform _rt;        // 新路
        readonly Image _img;

        HudPiece(ImageQuad q, RectTransform rt, Image img) { _quad = q; _rt = rt; _img = img; }

        /// <summary>这一件是不是已经迁到 uGUI 了（自检/诊断用；**判据只此一处**）。</summary>
        public bool IsUgui { get { return _quad == null; } }

        /// <summary>旧路那一支（没迁的件才有；没迁时才非 null）。</summary>
        public ImageQuad Quad { get { return _quad; } }
        /// <summary>新路那一支（迁过的件才有）。</summary>
        public RectTransform Rect { get { return _rt; } }

        // ── 建件（**只有 `HudCanvas` 与 `BattleDriver` 那 4 个建件助手调**）────────────
        /// <summary>包一个旧 quad（没迁的件走这条）。</summary>
        public static HudPiece Of(ImageQuad q) { return q == null ? null : new HudPiece(q, null, null); }
        /// <summary>包一个 uGUI 件（迁过的件走这条）。</summary>
        public static HudPiece Of(RectTransform rt, Image img)
        {
            return rt == null ? null : new HudPiece(null, rt, img);
        }

        // ── 那 140 处用法用到的口（逐条两种实现）──────────────────────────────────
        public GameObject gameObject { get { return _quad != null ? _quad.gameObject : _rt.gameObject; } }
        public Transform transform { get { return _quad != null ? _quad.transform : (Transform)_rt; } }

        /// <summary>这个件的**对象名**。⚠️ `HudPiece` **不是 `UnityEngine.Object`** ⇒ 那些
        /// `q.name` / `.gameObject` 的用法得由本件补出来（转发给底下那个真正的对象）。</summary>
        public string name
        {
            get { var g = gameObject; return g != null ? g.name : null; }
            set { var g = gameObject; if (g != null) g.name = value; }
        }

        /// <summary>当前贴图（诊断/自检用）。取不到 ⇒ `null`（⛔ 不返回假贴图）。
        /// ⚠️ 返回类型是 `Texture`（不是 `Texture2D`）—— 跟 `ImageQuad.Texture` 一致即可（用法只有 `.name`）。</summary>
        public Texture Texture
        {
            get
            {
                if (_quad != null) return _quad.Texture;
                var sp = _img != null ? _img.sprite : null;
                return sp != null ? sp.texture : null;
            }
        }

        /// <summary>画出来的**世界宽**。判据见文件头（uGUI 侧用 `GetWorldCorners`）。</summary>
        public float WorldW
        {
            get
            {
                if (_quad != null) return _quad.WorldW;
                var c = Corners(); return Mathf.Abs(c[3].x - c[0].x);
            }
        }

        /// <summary>画出来的**世界高**。判据同 <see cref="WorldW"/>。</summary>
        public float WorldH
        {
            get
            {
                if (_quad != null) return _quad.WorldH;
                var c = Corners(); return Mathf.Abs(c[1].y - c[0].y);
            }
        }

        /// <summary>这个件画出来的尺寸（**真设计 px**，即 **1920×1080** 那一套）—— ⛔ **不是世界单位**。
        ///
        /// <para>🔴 **2026-10-16（`A1302` · `EW11`）为什么要有它**：<see cref="WorldW"/> /
        /// <see cref="WorldH"/>（本类，**世界单位**）与 `Label.WorldW/WorldH`（**px**）**同名不同量纲**
        /// —— 本会话那个「**0.75 倍**」的总源头就是「**拿世界尺寸去折 px**」。对**已迁 uGUI** 的件，
        /// `WorldH × 108f` 在非 16:9 下**只有真设计 px 的 0.75 倍**（交到画布上的 `sizeDelta` 会被
        /// `CanvasScaler` 的 `scaleFactor` 缩一道）；判据 → `WorldW` 那两段 doc +
        /// <see cref="SetWorldHeight"/> 的 doc + `T09` §七·8（**批处理实测就是 640×480**，
        /// `Core/LayoutSpace.cs:174` 有自证句）。
        /// ⇒ **凡「自检要读『画出来多大』」一律走本口**（口径同 `HudCanvas.SizePx` /
        /// `BattleLogPanel.FrameSizePx` / `MultiCardDisplay.ContinueBarDrawnPx` —— 全仓已经三条先例）。</para>
        ///
        /// <para>两种模式**各读各的**（⛔ 别用一个去凑另一个）：
        /// · quad 支 ⇒ `WorldW/H × 108` —— 那一族的 quad 就是按 `LayoutSpace.Px()`（÷108）的尺建的，
        ///   所以 `× 108` 是它的逆，**原值逐个位不变**；`108` 是**设计常数**
        ///   （= `LayoutSpace.DesignPxH / DesignHeight`），⛔ **不是**随屏变的实测系数。
        /// · uGUI 支 ⇒ **转调 `HudCanvas.SizePx(Rect)`**（= `sizeDelta`）—— ⛔ **不在这里另写一份**：
        ///   `HudCanvas` 文件头那条推导（**一个 canvas 单位 = 一个设计 px**，`MakeAnchored` / `MakeImage`
        ///   就是把设计 px 原样写进 `sizeDelta` 的）**判据只留那一处**，也是本会话其余三条先例
        ///   （`BattleLogPanel.FrameSizePx` / `MultiCardDisplay.ContinueBarDrawnPx` / `HudCanvas.SizePx`）的口。</para>
        ///
        /// <para>⚠️ **一个现核过的边界（2026-10-16 `EW11`）——「今天安全、别照抄」**：
        /// uGUI 支读的是 `sizeDelta`，那个量**只在点锚（`anchorMin == anchorMax`）下等于绝对尺寸**；
        /// 拉伸锚（`anchorMin != anchorMax`）时它是「相对父矩形」的增量，`FillParent` 那一路
        /// 还会把它写成 **(0, 0)**。现核：本仓**确实有四个**把 `HudPiece` 拉成拉伸锚的调用点 ——
        /// `BattleLogPanel.cs:652`（`_shade`）· `EndPanel.cs:218`（`_dim`）·
        /// `MulliganPanel.cs:634`（`p._shade`）· `SettingsPanel.cs:768`（`_shade`）
        /// ⇒ **那四件调本口会读到 (0,0)**。
        /// ✅ **今天不踩**：本口此刻只有三个消费者，拿的全是**点锚件**
        /// （`BattleDriver.HudExtraDrawnPx` / `CameraResetButtonDrawnPx` / `AllianceButtonDrawnPx`，
        /// 底层都是 `HudAbs` → `HudImageTex` → `HudCanvas.MakeAnchored`，点锚）。
        /// ⛔ **将来要拿拉伸锚的件调本口**：先把锚点设回去，或改读 `Rect.rect.size`（那才是拉伸后的真尺寸）
        /// —— **别直接 `q.SizePx`**（会静默给 0）。</para></summary>
        public Vector2 SizePx
        {
            get
            {
                if (_quad != null) return new Vector2(_quad.WorldW, _quad.WorldH) * 108f;
                return HudCanvas.SizePx(_rt);
            }
        }

        /// <summary>当前染色（自检用）。</summary>
        public Color Tint { get { return _quad != null ? _quad.Tint : (_img != null ? _img.color : Color.white); } }

        /// <summary>换贴图。⚠️ uGUI 那一侧要**重建 `Sprite`**（`Image` 要的是 `Sprite`、不是 `Texture2D`）。</summary>
        public void SetTexture(Texture2D tex)
        {
            if (_quad != null) { _quad.SetTexture(tex); return; }
            if (_img == null || tex == null) return;
            _img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                        new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>染色。</summary>
        public void SetTint(Color c)
        {
            if (_quad != null) { _quad.SetTint(c); return; }
            if (_img != null) _img.color = c;
        }

        /// <summary>**盖掉贴图比例**。语义 = 「宽 = 高 × aspect」（与 `ImageQuad.SetAspect` 逐字一致）——
        /// ⚠️ uGUI 那一侧**只改 `sizeDelta.x`、高度不动**（`Image.preserveAspect` 是另一回事，见
        /// `HudCanvas.MakeImage` 里那条「原版 `PA=0` ⇒ 拉满」）。</summary>
        public void SetAspect(float aspect)
        {
            if (_quad != null) { _quad.SetAspect(aspect); return; }
            if (_rt == null || aspect <= 0f) return;
            _rt.sizeDelta = new Vector2(_rt.sizeDelta.y * aspect, _rt.sizeDelta.y);
        }

        /// <summary>旧那套的「往后挪一点」（`localPosition.z += dz`）—— 修 z 的人一律走它。
        /// ⚠️ uGUI 下 z 没有意义，它改的是**兄弟序**（见 `HudCanvas.Reorder`）。</summary>
        public void NudgeZ(float dz)
        {
            if (_quad != null) { _quad.transform.localPosition += new Vector3(0f, 0f, dz); return; }
            if (_rt != null) HudCanvas.Reorder(_rt, HudCanvas.OrderZOf(_rt) + dz);
        }

        /// <summary>这个件占的 **z 档**（诊断/自检读回）。uGUI ⇒ 它在画布里的**排位 z**（⛔ 不是 Unity 的 z）。
        /// 认不出 ⇒ `NaN`。</summary>
        public float OrderZ
        {
            get { return _quad != null ? _quad.transform.localPosition.z : HudCanvas.OrderZOf(_rt); }
        }

        /// <summary>平移这个件（**世界单位**的增量）。两种模式各用各的口：
        /// quad ⇒ `transform.localPosition`；uGUI ⇒ `anchoredPosition`（+y 向上，与世界 +y 同向）。
        /// ⚠️ 别在调用点自己写 `transform.localPosition` —— uGUI 那一侧的单位与原点都不是那一套。
        ///
        /// <para>🔴🔴 **单位契约（2026-10-16 · `EW2` 现核 —— 读了再改）**：传进来的数**一律是
        /// 「设计 px ÷ 108」**（= `LayoutSpace.Px(px)` 的像 —— `BattleDriver.cs:1377`
        /// `static float Px(float px) { return px / 108f; }`）；三个现存调用点
        /// （`BattleDriver.cs:15850/15851` 回合灯 · `:16304` 牌堆卡背）**都先套了 `Px()`**，
        /// 所以 uGUI 支这里 `× 108` 是它的**逆**：两个分支交出的是**同一个量**（原版那个**设计 px**）——
        /// quad 支的 `localPosition` = 设计 px/108 **世界单位**，uGUI 支的 `anchoredPosition` = **设计 px ·
        /// 画布单位**。这与「原版 HUD 是画布件、位置/尺寸都印在 `m_SizeDelta`/`anchoredPosition` 里」逐位一致
        /// （口径 → `T09` §五·补⑧ 那张表：已迁族「位置 = 锚点 · 尺寸 = **设计 px**」）。</para>
        ///
        /// <para>⚠️ **为什么⛔ 不能换成「按轴实测的画布单位/世界单位」**（批处理下 x 108 / y 144，
        /// 判据 → `T09` §七·8）：那会把**每一处**偏移乘成 **1.333 倍**。实测量：回合灯的
        /// `MyLightDxPx = 83.46` 是从**原版 rect** 推出来的设计 px（`BattleDriver.cs:1363-1376`），
        /// 按 144 折会落到 **111.28 px** ⇒ 灯离开牌堆底板 33%；而它必须与牌堆底板（同一族、同一把尺）
        /// 严丝合缝。同一批代理在 `T09` §七·8 记的结论正是「**画面其实是对的**」——
        /// 偏 0.75 的是**拿世界尺寸去折 px 的【读回】**（`q.WorldH × 108f`），⛔ 不是这里。</para>
        ///
        /// <para>🔴 **真要动本口，必须【成对】动**（半迁移比不迁更坏）：① `HudImageTex` 的
        /// `hPx = worldHeight * 108f`（`BattleDriver.cs`，同一个 `Px()` 契约）② 上面那三个调用点
        /// ③ 本口两个分支的语义。荐的路子是把口改成**显式 px**（`NudgePx(dxPx, dyPx)`：quad 支自己 `Px()`、
        /// uGUI 支原样吃 px），`108` 就只剩 `LayoutSpace` 一处 —— ⛔ 但那是**白名单外**的活，
        /// 本件只记判据、**不擅自长新口**（本件是迁移脚手架，见文件头）。</para>
        ///
        /// <para>✅ **16:9 逐位不变**：`scaleFactor == 屏高/1080`（宽高比 ≥ 16:9）时两支画出来**逐位相同**
        /// （那时实测系数也正好是 108，`LayoutSpace.HudTextScale == 1`）；只有比 16:9 **窄**的档
        /// （批处理 640×480 就是）才相差 `1/HudTextScale`（uGUI 支 = quad 支 × 0.75）——
        /// 那是**原版画布件在窄屏本来就按 `scaleFactor` 缩**这件事（`LayoutSpace.HudTextScale` 的 doc
        /// 把这条推导写全了），⛔ 两边谁都不该去凑另一个。</para></summary>
        public void NudgeWorld(float dxWorld, float dyWorld)
        {
            if (_quad != null)
            {
                _quad.transform.localPosition += new Vector3(dxWorld, dyWorld, 0f);
                return;
            }
            // ⚠️ 这个系数 = `LayoutSpace.Px()` 的逆（设计 px / 世界单位，两轴同值 108）——
            //    ⛔ **不是**「世界单位 → 画布单位」的实测系数（那个随屏变、两轴还可能不同）。
            //    判据与「换成实测系数会把件放大 1.333 倍」的实测例 → 上面那段 doc。
            if (_rt != null)
            {
                float k = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
                _rt.anchoredPosition += new Vector2(dxWorld * k, dyWorld * k);
            }
        }

        /// <summary>这个件贴着的**归一化锚点**（x 从左、y 从**下**算）—— 自检读回用。
        /// ⚠️ 与 <see cref="SetAnchorPosition"/> 成对。⛔ **别用 `transform.localPosition` 代替**：
        /// uGUI 那一侧的单位是「设计 px」、且相对的是 canvas，与归一化锚点不是一回事。
        ///
        /// <para>🔴🔴 **2026-10-17（`BSC62` · W2）：uGUI 支原来【只回 `anchorMin`】、把 `anchoredPosition`
        /// 丢了 —— 已修。** 病灶：<see cref="NudgeWorld"/> 是**增量**（`_rt.anchoredPosition += …`），
        /// 而本口只读回锚点那一半 ⇒ **凡是偏移过的件（牌堆回合灯 / 手牌数底板 / 牌堆卡背）读回来都少了那个偏移**。
        /// 「牌堆回合灯」那条断言（`Editor/BattleScene.cs:3588-3590`，期望 `0.94451 / 0.08778`）就是它的直接受害者：
        /// 实读 `0.90104 / 0.10648`（= 纯 `anchorMin`），差 `+0.04347 / −0.0187`
        /// —— 换成 px 正是 `PlaceDeckLights` 喂进 `NudgeWorld` 的 `(83.46, −20.2)`，一分不差。</para>
        ///
        /// <para>🔑 **单位契约（逐跳写清，口径见 <see cref="NudgeWorld"/> / <see cref="SetAnchorPosition"/> 的 doc）**：
        /// · <see cref="SetAnchorPosition"/> 写的是**归一化**（`anchorMin = anchorMax = (x01,y01)`、`ap = 0`）；
        /// · <see cref="NudgeWorld"/> 写的是 `ap += dxWorld·k`，`k = 108 = DesignPxH/DesignHeight`
        ///   （= `LayoutSpace.Px()` 的**逆**）；而调用点喂进来的 `dxWorld` 已经是 `Px(px)`（设计 px÷108）
        ///   ⇒ `ap` 累加的**就是设计 px**（px）；
        /// · ⇒ 本口把「设计 px 的偏移」折回**归一化** = **除以设计框**（`DesignPxW` / `DesignPxH` = 1920 / 1080）。
        /// quad 支（`ToNormalized(localPosition)`）**同样是设计框归一化**（`ToNormalized` 是 `ToWorld` 的逆、
        /// 与分辨率无关）⇒ 两支量的是**同一件事**。</para>
        ///
        /// <para>🔴 **「锚点参考矩形」取【设计框 1920×1080】，⛔ 不是父件（canvas）的实 rect。**
        /// 为什么单写这一句（这一条错了会**静默偏**、16:9 下还看不出来）：
        /// 根 canvas 挂的是 `CanvasScaler(1920×1080, **Expand**)`（见 `HudCanvas.Ensure`）⇒ canvas 的实 rect
        /// = `屏px ÷ scaleFactor`，**只在 16:9 下 = 1920×1080**；比 16:9 **窄**时它**变高**
        /// （批处理 640×480 → `scaleFactor = min(640/1920, 480/1080) = 1/3` ⇒ canvas = **1920×1440**）。
        /// 若拿父 rect 当分母：y 就变成除以 1440 ⇒ 回合灯读回 `≈0.0925`（≠ 期望 `0.08778`，超 0.002 容差），
        /// 而且读数会**随宽高比变**，与「写侧写的是设计 px」以及 quad 支**都对不上**。
        /// ⇒ 设计框才是本 HUD 坐标系**约定的那块矩形**（`Px()` / `FromPixel` / `SetAnchorPosition` 全按它建），
        /// 归一化也相对它 ⇒ 两轴各除 (`DesignPxW`, `DesignPxH`)。</para>
        ///
        /// <para>⚠️ **依赖（别当成无关）**：<see cref="NudgeWorld"/> 是**增量**，本口读的是「当前 `ap`」⇒
        /// 只在「**上一次绝对复位（`SetAnchorPosition`）之后、被 nudge 了 n 次**」这个前提下才等于
        /// 「偏移后的设计框归一化位置」。本口**不依赖**「写侧不累加」、也**不依赖**「复位一定跑」——
        /// 它忠实反映当前 `ap`；累加 / 漏复位是**写侧**的事（另有一件活在排队，⛔ 不在本件）。</para>
        ///
        /// <para>⚠️ **点锚前提**：本式假设 `anchorMin == anchorMax`（点锚 + `ap` 相对那个锚）。
        /// 拉伸锚（`anchorMin != anchorMax`）下 `ap` 的语义不是这个 —— 今天本口没有拉伸锚的消费者
        /// （那四件 `_shade` / `_dim` 覆盖层都没被读过）。</para></summary>
        public Vector2 Anchor01()
        {
            if (_quad != null) return LayoutSpace.ToNormalized(_quad.transform.localPosition);
            if (_rt == null) return new Vector2(-1f, -1f);
            // `ap` 是【设计 px】（见上面推导）⇒ 除以设计框即归一化。两轴各除（⛔ 别拿 108 去折 —— y 那个 108 只是
            // `Px()` 的系数，这里要的是「相对整框的比例」，框高就是 1080）。
            return new Vector2(_rt.anchorMin.x + _rt.anchoredPosition.x / LayoutSpace.DesignPxW,
                               _rt.anchorMin.y + _rt.anchoredPosition.y / LayoutSpace.DesignPxH);
        }

        /// <summary>设**画心高**（世界单位），**保持当前宽高比**（⛔ 不动比例 —— 调用点接着会自己调
        /// `SetAspect` 把宽定下来，见 `FitDeckPile` 那三步）。判据同 <see cref="SetAspect"/>：
        /// 设计 px = 世界单位 × 108。
        ///
        /// <para>🔴 **单位契约与那个 `108` 的来路 = 与 <see cref="NudgeWorld"/> 同一条**（那段 doc 有完整判据，
        /// ⛔ 别只看这一处）**：传进来的数**一律是「设计 px ÷ 108」**（`BattleDriver.cs:1377` 的 `Px()` 的像），
        /// uGUI 支乘回去得到的就是**原版设计 px** —— 也就是 `sizeDelta` 该有的数
        /// （`FitDeckPile` 的 `SetWorldHeight(Px(h))` 里那个 `h` 正是原版卡背节点高，见 `BattleDriver.cs:16301`）。
        /// ⛔ **换成实测系数（批处理 y 轴 144）⇒ 卡背高变成原版 `m_SizeDelta` 的 1.333 倍**，
        /// 而且会与同一族的牌堆底板（`HudImageTex` 用同一把 108 尺建的）错开。</para>
        ///
        /// <para>⚠️ **世界档那一条不受影响**：`_quad != null` 时原样转发 `ImageQuad.SetWorldHeight`（世界单位），
        /// 那一支的量纲本来就对；只有 uGUI 支在「世界单位」这个名字下走的其实是**设计 px** 那把尺。</para>
        ///
        /// <para>✅ **16:9 逐位不变** · **读回要读 `sizeDelta`**（`HudCanvas.SizePx`）：`q.WorldH * 108f` 在窄屏是
        /// 设计 px 的 0.75 倍（`T09` §七·8），⛔ 别拿它当「这个口写错了」的证据。</para></summary>
        public void SetWorldHeight(float worldH)
        {
            if (_quad != null) { _quad.SetWorldHeight(worldH); return; }
            if (_rt == null || worldH <= 0f) return;
            float h = worldH * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);   // = `LayoutSpace.Px()` 的逆（见 doc）
            float a = _rt.sizeDelta.y > 0f ? _rt.sizeDelta.x / _rt.sizeDelta.y : 1f;
            _rt.sizeDelta = new Vector2(h * a, h);
        }

        /// <summary>当前**材质**（自检比「牌堆那张 SDF 用的是原版 shader」时读它）。
        /// 两种模式各取各的：quad ⇒ 它自己的 `MeshRenderer.sharedMaterial`；uGUI ⇒ `Image.material`。</summary>
        public Material Material
        {
            get
            {
                if (_quad != null)
                {
                    var mr = _quad.GetComponent<MeshRenderer>();
                    return mr != null ? mr.sharedMaterial : null;
                }
                return _img != null ? _img.material : null;
            }
        }

        /// <summary>换材质（牌堆那张 SDF 用的）。⚠️ uGUI 那一侧 = `Image.material`。</summary>
        public void SetMaterial(Material m)
        {
            if (_quad != null) { _quad.SetMaterial(m); return; }
            if (_img != null) _img.material = m;
        }

        /// <summary>贴到某个**归一化锚点**（x 从左、y 从**下**算 —— 与 `LayoutSpace` 同口径）。</summary>
        public void SetAnchorPosition(float x01, float y01)
        {
            if (_quad != null) { _quad.SetAnchorPosition(x01, y01); return; }
            if (_rt == null) return;
            _rt.anchorMin = _rt.anchorMax = new Vector2(x01, y01);
            _rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>这个件的**世界四边形**是不是含住了 `world` 点（命中判定，**与旧 quad 量的是同一件事**）。</summary>
        public bool Contains(Vector3 world)
        {
            if (_quad != null) return _quad.Contains(world);
            var c = Corners();
            // 用四边形判内点（canvas 的件是轴对齐的矩形；这里按「两个三角」判，斜面也能覆盖）
            return InTri(world, c[0], c[1], c[2]) || InTri(world, c[0], c[2], c[3]);
        }

        /// <summary>`GetWorldCorners` 的四个角（顺序 = Unity 的约定：左下 → 左上 → 右上 → 右下）。</summary>
        Vector3[] Corners()
        {
            var c = new Vector3[4];
            _rt.GetWorldCorners(c);
            return c;
        }

        static bool InTri(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // 只判 xy 平面（HUD 是正对相机的平面）——与 `ImageQuad.Contains` 同口径
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool neg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool pos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(neg && pos);
        }

        static float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
