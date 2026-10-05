// ImageQuad.cs — 世界空间的一张图（HUD 用）
//
// 和 `Label` 是一对：Label 画字，这里贴图。没有 uGUI —— 卡牌/粒子全是世界空间的，
// HUD 也走同一套坐标，省得在 Canvas 和世界坐标之间来回换算。
//
// 原版复刻用的图从 `CardArt.Ui(...)` 取（`Resources/Art/ui/`）；没有图时 `Create` 返回 null，
// 调用处要判空 —— 这样删掉美术目录 HUD 也不会炸，只是变成纯文字。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public class ImageQuad : MonoBehaviour
    {
        /// <summary>**画布像素 → 世界单位**的换算：1 px = 1/108 世界单位。
        ///
        /// 🔴 **2026-10-03 订正（原来写的是 `100f`）**：这里的 px 一律指**画布像素**（1920×1080 设计像素，
        /// 左上原点 —— `menu_dump` / 场景 JSON 里那些数），而本工程 **1 世界单位 = 108 画布像素**
        /// （判据 = `Core/LayoutSpace.cs:110` 那条：「可见高度固定 10 个世界单位 = 1080 px」
        ///  ⇒ `LayoutSpace.Px()` 就是 `px ÷ 108`）。写成 `100` 会让**每一条九宫格/平铺整体大 8%**，
        /// 实测：左栏 `Highlight` 的角块 `30 ÷ 0.92 = 32.61` 画布像素，渲出来是 **35.22**（= 32.61 × 1.08）
        /// —— `Editor/RewardsScene.cs` 的「§三·b4-b」那条断言量到了、且当时是红的。
        ///
        /// ⚠️ **不是「和 `Label` 同口径」**（原来这么写是错的）：`Battle/Label.cs:22` 另有一份**私有**的
        /// `PixelsPerUnit = 100f`，管的是**点阵后端**的字块尺寸与 HUD 的整数 scale 档，**与本类这个常量无关**
        /// （改这里不会动它）。本常量**只**被下面 `CreateNineSlice` / `CreateTiled` 两处用来把 px 折成世界尺寸。
        ///
        /// 📌 值写成 `LayoutSpace` 的推导式（而不是字面量 `108f`）—— 让「换算只有一处」是**结构性**的：
        /// `DesignPxH / DesignHeight` 一变，这里跟着变，不会再各写一个数（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</summary>
        public const float PixelsPerUnit = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;

        public Vector2 anchor = new Vector2(0.5f, 0.5f);

        MeshRenderer _mr;
        MeshFilter _mf;
        Texture _tex;
        float _worldH = 1f;
        float _aspect = 1f;

        // ============================================================ 软边子块（`MenuDraw.ApplySoftEdges`）
        //
        // 🆕 **2026-10-04（A38③）**。软边的做法是「按渐隐带的内沿把这个 quad **切开**、每块逐顶点 alpha 斜坡」
        //（几何等效，判据与代价见 `MenuDraw.ApplySoftEdges`）—— 切出来的每一块是**独立 quad**，
        // 于是有两个只有软边宿主才有的缺口（Q1 审查顺手发现的那条）：
        //   · 父件之后被 `SetTint` ⇒ 子块停在建它那一刻的颜色（**静默**：只有边带那一条颜色不对）；
        //   · 父件之后被 `SetAspect`/`SetWorldHeight` ⇒ 子块的几何停在旧框上（**静默**：带的位置错）。
        // ⇒ 给宿主两个口：`SoftEdgeRegister`（登记子块，`SetTint` 跟着刷）·
        //   `SoftEdgeRebuild`（几何一变就重切，回调由 `MenuDraw` 给）。
        // 🔴 **影响面**：没登记过子块的 quad **一个字节都不变**（`_softKids` 空 + `_softRebuild == null`
        //   ⇒ `SetTint` 里一次空循环、`SetAspect/SetWorldHeight` 里一次 null 判断）。
        //   全工程 `SetTint/SetAspect/SetWorldHeight` 共 **184 处调用** —— **口径（复核者可原样复现）**：
        //   `grep -rn "\.SetTint(\|\.SetAspect(\|\.SetWorldHeight(" --include=*.cs d:/4/Unity/MyGame/Assets | wc -l`，**按行计**、
        //   **含 `ImageQuad.cs` 自身那 3 处**（`:196`/`:399`/`:428` —— 它们正是下面这两个分支自己的实现
        //   ⇒ **除自身 = 181 行**；另：184 行里有 2 行是**注释里提到**这个写法，真调用行 = 182）。
        //   ⚠️ **这个数逐波在漂**（`2026-10-04` W6 审查的读数是 187 行 ⇒ 除自身 184，**上一版注释抄的就是它**；
        //   `2026-10-07` 复核实测 184 行）—— **引用前自己再数一遍，别当常量**。
        //   其中**只有**走过 `MenuDraw.Rect/Nine/Tiled` 且 `clipSoftness ≠ 0` 的那几处会进这两个分支。

        readonly List<ImageQuad> _softKids = new List<ImageQuad>();
        System.Action _softRebuild;
        bool _softBusy;

        /// <summary>「几何变了 ⇒ 重切软边」的回调（由 `MenuDraw` 挂；不挂 = 不重切 = 原来那套行为）。</summary>
        public System.Action SoftEdgeRebuild { get { return _softRebuild; } set { _softRebuild = value; } }
        /// <summary>登记一块软边子块（`SetTint` 时跟着刷）。</summary>
        public void SoftEdgeRegister(ImageQuad child)
        {
            if (child == null || child == this || _softKids.Contains(child)) return;
            _softKids.Add(child);
        }
        /// <summary>现在登记着几块软边子块（**自检用**：断「这条路真的带着电」）。</summary>
        public int SoftEdgeKidCount { get { return _softKids.Count; } }
        // ⚠️ **2026-10-04 删掉了 `SoftEdgeForget(child)`**（F11：全工程 0 个调用点 = 死代码）。
        //    它当年的用途是「子块被**单独**销毁时把登记摘掉」—— 但那条路**根本不存在**：
        //    全工程只有 `MenuDraw.ReapplySoftEdges` 一处会销毁子块，它走的是 `SoftEdgeClear()`（清空整张表）。
        //    就算哪天真有悬挂条目也**不会出事**：`_softKids` 里存的是 `ImageQuad`（`UnityEngine.Object`），
        //    被销毁之后 `!= null` **假**（Unity 的 fake-null）⇒ `SetTint` / `SoftEdgeClear` 的 `if (k == null)`
        //    已经兜住了（唯一的代价是那张表会慢慢长，十来个上限）。
        //    ⇒ 将来真要单块销毁，**重新加回这个方法**（登记/注销成对），别只删不摘。

        /// <summary>销毁全部软边子块并清空登记（`MenuDraw.ReapplySoftEdges` 重切之前叫它）。
        /// ⚠️ 销毁一律走 `Destroy` / `DestroyImmediate` 那条**批处理规矩**（没有帧循环 ⇒ `Destroy` 不生效，
        /// 见 `MenuDraw.ClearChildren` —— 这里与它同一条）。</summary>
        public void SoftEdgeClear()
        {
            for (int i = 0; i < _softKids.Count; i++)
            {
                var k = _softKids[i];
                if (k == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying) { Object.DestroyImmediate(k.gameObject); continue; }
#endif
                Object.Destroy(k.gameObject);
            }
            _softKids.Clear();
        }

        /// <summary>几何变了的通知口。🔴 **必须防重入**：重切本身要摆父件（`PlaceCell` → `SetAspect`/`SetWorldHeight`）
        /// ⇒ 不防就是无限递归。</summary>
        void NotifySoftEdgeChanged()
        {
            if (_softBusy || _softRebuild == null) return;
            _softBusy = true;
            try { _softRebuild(); } finally { _softBusy = false; }
        }

        public float WorldH { get { return _worldH; } }
        public float WorldW { get { return _worldH * _aspect; } }

        /// <summary>建一张图。`worldHeight` 是**屏幕上的高度**（世界单位），宽度按原图比例走。
        /// 贴图为空则返回 null（调用处判空）。
        /// ⚠️ `tex` 收 `Texture` 而不是 `Texture2D` —— 结算视频那层要贴 `RenderTexture`。</summary>
        public static ImageQuad Create(Transform parent, Texture tex, Vector3 pos, float worldHeight,
                                       Vector2 anchor, string name = null)
        {
            if (tex == null) return null;

            var go = new GameObject(name ?? ("img_" + tex.name));
            go.transform.SetParent(parent, false);

            var q = go.AddComponent<ImageQuad>();
            q.anchor = anchor;
            q._tex = tex;
            q._aspect = tex.height > 0 ? tex.width / (float)tex.height : 1f;
            q._worldH = worldHeight;

            q._mf = go.AddComponent<MeshFilter>();
            q._mr = go.AddComponent<MeshRenderer>();
            q._mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            q._mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q._mr.receiveShadows = false;
            q.SetTexture(tex);
            q.RebuildMesh();
            go.transform.localPosition = pos;
            return q;
        }

        public Texture Texture { get { return _tex; } }

        /// <summary>把贴图**内接**进一个框，返回内接后的**高**（世界单位）—— 这就是原版
        /// `Image.m_PreserveAspect` 的语义：**等比放进框、居中**（宽由 `WorldW` 自然得出）。
        ///
        /// 🔴 **为什么非要有这个入口**：`Create` 只吃「高」，宽一律 = 高 × 贴图比例 ⇒ **可以超出框**。
        ///   牌堆底板就是这个坑：原版那一格 `preserveAspect`、框 **230×229.85**（方），
        ///   而图 `UI_Deck_Background` 是 **364×346**（横）⇒ 原版**按宽定**、实绘 **230×218.63**；
        ///   我们原来只给高 230 ⇒ 实绘 **241.96×230**（**宽出框 11.96px、高出 11.4px = +5.2%**）。
        ///
        /// 判据 = uGUI `Image.GetDrawingDimensions(preserveAspect)`：**图比框宽 ⇒ 按宽定；否则按高定**。
        /// 与菜单侧那条**同一条算法**（`MenuDraw.Rect:68-73` / `MenuWindowBase.Rect` 只是先内缩矩形再建）。</summary>
        public static float FitHeight(float boxW, float boxH, float sprAspect)
        {
            if (boxW <= 0f || boxH <= 0f || sprAspect <= 0f) return boxH;
            return sprAspect > (boxW / boxH) ? boxW / sprAspect : boxH;
        }

        /// <summary>换图。🔴 **软边宿主换图时，切出来的子块跟着换** —— 判据与两种情形（同尺寸 / 不同尺寸）
        /// → 下面那一段注释（2026-10-11 · A286）。
        /// <para>🔴 **2026-10-11（A292）：本重载【会】按新贴图改 `_aspect`**（= 从写下那天起的行为，
        /// 逐字保留）—— 于是 `WorldW = WorldH × _aspect` 跟着变，**每个调用点都得自己补一句
        /// `SetAspect(...)`** 把显示比例拉回来（现读有 6 处这么补：`Shell/PromptPopup.cs` 的
        /// `WindowButton.SetOn` · `Battle/BattleLogPanel` · `Shell/AlliancesTab` · `Deck/DeckRuntime`
        /// 两处 · `Battle/BattleDriver` —— **那 6 处本件一个字都没动**）。
        /// ⛔ **新代码别再用这个单参口**：想说清意图就走
        /// <see cref="SetTexture(Texture,bool)"/>（`keepAspect: true` = 只换图、比例不动）。</para></summary>
        public void SetTexture(Texture t) { SetTexture(t, false); }

        /// <summary>🆕 **2026-10-11（A292）**：换图 + **显式**说明「要不要按新贴图改比例」。
        ///
        /// <para>🔴 **为什么必须有它**：`_aspect` 被 `SetTexture` 静默冲成「新贴图自己的比例」这件事，
        /// 今天全靠**纪律**兜 —— 6 个调用点各自紧跟一句 `SetAspect(...)`，**漏一处就是那个 quad 的显示
        /// 比例被静默改掉**（`_aspect` 直接乘进 `WorldW`，画面变宽/变窄而没有任何报错；
        /// 同族先例：`Battle/BattleLogPanel` 的注释里记着实测 748→690.1）。⇒ 把「改比例」变成**显式**动作。</para>
        ///
        /// <para>`keepAspect == false` ⇒ **与单参那个重载逐字相同**（= 今天的行为，⛔ 老调用点一个字节都没变）；
        /// `keepAspect == true` ⇒ **只换贴图，`_aspect`（以及 `WorldW`/`WorldH`）一个字节都不碰** ——
        /// 连网格都不重建（几何没变），软边子块照旧只换图（它们走 `SetTextureOnly`，本来就是「不动比例」那一档）。</para>
        ///
        /// <para>⚠️ **`true` 这一档今天**（2026-10-11）**生产上还没有调用点** —— 它服务的是「换上去那张图
        /// 与当前显示比例**本来就该一致**」的那些新站点（`WindowButton.Bind` 记的 `_targetAspect` 是
        /// **该 quad 的矩形**比例，见 A286 那条订正）。既有 6 处**按简报保持原样**（能不改就不改）。</para>
        ///
        /// <para>⚠️ **软边那两件事与 `keepAspect` 无关、两条路都做**（它们管的是「子块跟着换图」与
        /// 「像素尺寸变了 ⇒ 重切几何 + uv」，与 `_aspect` 是两回事）：`keepAspect: true` 时宿主矩形没变，
        /// 那一次重切是**恒等**的（A286 的注释：`ReapplySoftEdges` 在宿主矩形不变时映射为恒等、逐字段相同）。</para>
        ///
        /// <para>⚠️ **没碰 `SetTextureOnly`（私有，服务软边子块）**：它是「**只**换贴图」的最小步，
        /// 与本重载的 `true` 档**差在「要不要转给子块 / 挂不挂重切」** —— 合成一个会让子块自己再转一次
        /// （`SetTexture` 那一段会遍历 `_softKids`）。</para></summary>
        public void SetTexture(Texture t, bool keepAspect)
        {
            var prev = _tex;
            bool sizeChanged = !SameTexSize(prev, t);
            _tex = t;
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.mainTexture = t;
            // 🆕 **2026-10-11（A286）：先把图转给登记过的软边子块** —— 照 `SetTint` 的既有形状
            //   （子块是**独立 quad**，不跟着刷就会停在旧图上：**静默**，只有边带那一条不对）。
            //   🔴 **必须走 `SetTextureOnly`（只换图、⛔ 不动 `_aspect`）** —— 子块是**细条**，
            //   它的 `_aspect` 是**切出来那一格**的比例（`MenuDraw.PlaceCell` 里
            //   `SetAspect(cell.W / cell.H)`），而 `SetTexture` 会把 `_aspect` 冲成「贴图自己的」
            //   ⇒ `WorldW = _worldH × _aspect`，细条被拉成整条宽（`BindNine` 那条路上实测过同一个病）。
            //   ⛔ **顺序**：这一步必须在下面的重切**之前** —— 重切（`SoftEdgeClear`）会把 `_softKids` 清空，
            //   先换图再重切时那些新子块本来就带着新图（它们在 `ApplySoftEdges` 里读 `q.Texture`）。
            for (int i = 0; i < _softKids.Count; i++)
            {
                var k = _softKids[i];
                if (k != null) k.SetTextureOnly(t);
            }
            // 🔴 **2026-10-11（A292）**：`keepAspect` 只管这一句 —— **唯一**的差异点。
            //    `false` 时逐字等于旧代码；`true` 时比例（以及 `WorldW`/`WorldH`/网格）一个字节都不动。
            if (!keepAspect && t != null && t.height > 0) _aspect = t.width / (float)t.height;
            // 🆕 **2026-10-11（A286）· 两种情形里的第二种**：常态图与悬停图**不同尺寸**时，再走一次重切
            //   （同尺寸那一档**不**走 —— 那是绝大多数情况：本机能对上对子的 28 对里 26 对同尺寸 ⇒
            //    只换贴图 = 零分配、零对象身份变化）。
            //   🔴 **这一档的确切作用（如实写，别读成「不重切就画错」）**：`SetTexture` 自己**从不重建宿主网格**，
            //   而子块的位置/尺寸/uv 是从**宿主矩形**算出来的（`MenuDraw.PlaceCell`）⇒ 宿主矩形没变时，
            //   子块的几何本来就是对的 —— 所以这一次重切在**今天的调用链上多半是冗余的**
            //   （`WindowButton.SetOn` 紧跟的那句 `SetAspect(_targetAspect)`，在「贴图比例 ≠ 目标比例」时
            //    自己就会触发同一条链、把子块按新图重建一遍）。留着它的两条理由：
            //   ① 裁定（A286）要的就是「不同尺寸 ⇒ 子块要**重切几何 + uv**」；
            //   ② 让「**`SetTexture` 单独被调用**」那条路也自洽 —— 今天有五处就是这么用的
            //      （`Shell/AlliancesTab` · `Battle/BattleLogPanel` · `Deck/DeckRuntime` 那几处「换图 + 自己拉比例」），
            //      宿主比例被换过之后，**之后任何一次几何重算**都会按新比例来 ⇒ 子块必须已经在同一份几何上。
            //   ⚠️ **它是幂等的**：宿主矩形没变时 `ReapplySoftEdges` 那个映射是恒等，重切出来的块与原来的
            //      逐字段相同（只是新对象）⇒ 不改画面。
            //   ⚠️ **没挂重切回调的宿主**（`SoftEdgeRebuild == null`）：这一句是**空操作**（`NotifySoftEdgeChanged`
            //   第一句就返回），子块仍已换到新图 ⇒ 不会退化成「什么都没做」。
            if (sizeChanged) NotifySoftEdgeChanged();
        }

        /// <summary>**只换贴图**：`_tex` + 材质上的 `mainTexture`，⛔ **一个字节都不碰 `_aspect`**。
        /// 谁要用：`SetTexture` 往软边子块转图那一路（子块是细条，比例是切出来那一格的，见上面的注释）——
        /// 别的调用点都用 `SetTexture`（那个**会**按贴图更新 `_aspect`，是 `MenuDraw.Rect` 一族依赖的
        /// 「换图要把比例拉回来」那条纪律的输入，见 `WindowButton.SetOn`）。
        /// 🆕 **2026-10-11（A292）**：「**只**换图、不动比例」这个语义现在也有**公开口**了 =
        /// `SetTexture(t, keepAspect: true)`（它多做的只有「转给软边子块 + 按需重切」两件）。
        /// 本函数仍是**子块那一路的最小步**，⛔ 别删、也别把 `SetTexture` 改成转调它。
        /// ⚠️ 不递归：子块自己不会有子块（`MenuDraw.ReapplySoftEdges` 只销毁/重建一层）。</summary>
        void SetTextureOnly(Texture t)
        {
            _tex = t;
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.mainTexture = t;
        }

        /// <summary>两张图**像素尺寸是不是一样**（`Texture.width/height`）。
        /// `null` 参与比较：两张都 `null` 才算「没变」（Unity 的 `==` 重载对已销毁对象返回 null ⇒ 同时兜住那一档）。</summary>
        static bool SameTexSize(Texture a, Texture b)
        {
            if (a == null || b == null) return a == b;
            return a.width == b.width && a.height == b.height;
        }

        /// <summary>换掉材质（结算视频要自建的「左右拼 alpha」合成 shader，
        /// 默认的 `Sprites/Default` 不会拆左右半）。**贴图会跟着带过去**，不然换完是空白。
        ///
        /// 🔴 **2026-10-05（A85）：换材质要【保留】调用前那份显式分好的 `renderQueue`。**
        /// 为什么非有这一条：分层**只靠渲染队列**（见上面 `SetRenderQueue` 的注释 —— 透明队列按
        /// 「到相机的 3D 距离」排序，铺满屏的图会互相盖错），而 `new Material(shader)` 是**没有显式队列**的：
        /// 读出来就是 SubShader 自带那一个，而我们用到的几张（`Sprites/Default` ·
        /// `Everguild/UI/Greyscale` · `CardPresentation/Video Split Alpha`）**全是 `QUEUE: Transparent` = 3000**
        /// ⇒ 一个 `SetRenderQueue(3120)` 过的 quad 一换材质就**掉到自己的底图之下**（画面上「按钮没了」）。
        /// 🔴 **最毒的是它不报错**：`WindowButton.AuditGrayLook` 只核 **shader 名**、不核队列 ⇒ **断言全绿、按钮没了**。
        /// 实测受害链 = `WindowButton.RefreshGray`（`Shell/PromptPopup.cs`）→ 这里
        /// （`Shell/DeckInfoPopup.cs` 那 8 颗 · `Shell/MissionsTab.cs` 那 6 颗 `Collect`）。
        ///
        /// ⚠️ **`q &lt; 0` 才不写**（`_mr` 上本来就没材质时读不出「旧队列」，那就保持新材质自己的）。
        /// 判据：`Material.renderQueue` 对**没显式设过队列**的材质返回的是 **SubShader 标签**那一个
        /// ⇒ 这条对「新旧都是 3000」的调用是**恒等变换**，不会把谁的档改掉。
        /// 全工程 **8 个 `SetMaterial(` 调用点**逐处核过（A85）：新材质清一色 3000、老材质要么本来 3000、
        /// 要么正是被这条救回来的显式队列 —— **没有一处会因此改坏**（`CardbackSdfMaterialBase()` 那份
        /// 自己也把 `renderQueue` 设成 3000，与旧材质同值）。</summary>
        public void SetMaterial(Material m)
        {
            if (_mr == null || m == null) return;
            // ⚠️ 旧队列必须在**换之前**读 —— 换完 `_mr.sharedMaterial` 已经是新的了
            int q = _mr.sharedMaterial != null ? _mr.sharedMaterial.renderQueue : -1;
            m.mainTexture = _tex;
            if (q >= 0) m.renderQueue = q;
            _mr.sharedMaterial = m;
        }

        public void SetTint(Color c)
        {
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.color = c;
            // 🆕 **2026-10-04（A38③）**：软边切出来的子块**跟着刷** —— 它们是独立的 quad
            //（同一张贴图、只有顶点色不同），不跟着就会出现「边带那一条颜色不对」（静默）。
            // ⚠️ **只有登记过软边子块的宿主会进这个循环**（子块自己不会再有子块 ⇒ 不会递归）。
            for (int i = 0; i < _softKids.Count; i++)
                if (_softKids[i] != null) _softKids[i].SetTint(c);
        }

        /// <summary>当前染色（含 alpha）。**自检用它验「半透明底板没被画成实心」** ——
        /// 一张 α0.694 的板子画成 α1 在截图上很容易看漏（尤其底下本来就有图案的时候）。</summary>
        public Color Tint
        {
            get { return (_mr != null && _mr.sharedMaterial != null) ? _mr.sharedMaterial.color : Color.white; }
        }

        /// <summary>
        /// 改**渲染队列**。给「要压住一切」的面板用（日志面板、结算面板那种）。
        ///
        /// 为什么需要它：`Sprites/Default` 和粒子都在**透明队列 3000**，同队列下谁压谁由
        /// **距离排序**决定 —— 而粒子系统的排序用的是它自己的**包围盒中心**，粒子一散开包围盒就变大，
        /// 排序结果和肉眼看到的对不上（2026-09-13 实测：把面板一路推到 z = −4，烟照样穿在面板上面）。
        /// **4000 = Overlay：脱离排序，最后画。**
        /// </summary>
        public void SetRenderQueue(int q)
        {
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.renderQueue = q;
        }

        /// <summary>当前渲染队列（**分层靠它，不靠 z** —— 见 `DeckRuntime` 那条注释：
        /// 透明队列按「到相机的 3D 距离」排序，铺满屏的图会互相盖错）。</summary>
        public int RenderQueue
        {
            get { return (_mr != null && _mr.sharedMaterial != null) ? _mr.sharedMaterial.renderQueue : 0; }
        }

        /// <summary>强制宽高比，**盖掉从贴图推出来的那个**。
        /// 结算视频要用：RT 是左右拼的 3840×1080（比例 3.56），显示区却是 1920×1080。</summary>
        public void SetAspect(float aspect)
        {
            if (aspect <= 0f || Mathf.Approximately(_aspect, aspect)) return;
            _aspect = aspect;
            RebuildMesh();
            NotifySoftEdgeChanged();      // 🆕 A38③：几何变了 ⇒ 软边要重切（没挂软边的 quad 不受影响）
        }

        public void SetWorldHeight(float h)        {
            if (Mathf.Approximately(h, _worldH)) return;
            _worldH = h;
            RebuildMesh();
            NotifySoftEdgeChanged();      // 🆕 A38③：同上
        }

        /// <summary>重新贴到某个归一化锚点（切分辨率要调）。**保留 z** —— HUD 靠它分层</summary>
        public void SetAnchorPosition(float x01, float y01)
        {
            var p = LayoutSpace.ToWorld(x01, y01);
            p.z = transform.localPosition.z;
            transform.localPosition = p;
        }

        void RebuildMesh()
        {
            float w = WorldW, h = WorldH;
            float x0 = -anchor.x * w, y0 = -anchor.y * h;

            var m = new Mesh { name = "ImageQuad" };
            m.vertices = new[]
            {
                new Vector3(x0, y0, 0f), new Vector3(x0 + w, y0, 0f),
                new Vector3(x0 + w, y0 + h, 0f), new Vector3(x0, y0 + h, 0f),
            };
            // UV 默认是整张图；九宫格/平铺靠 `SetUvRect` 只取一块
            m.uv = new[]
            {
                new Vector2(_uv.xMin, _uv.yMin), new Vector2(_uv.xMax, _uv.yMin),
                new Vector2(_uv.xMax, _uv.yMax), new Vector2(_uv.xMin, _uv.yMax),
            };
            // 顶点色：默认白（= 不染色）。`Sprites/Default` 会 `tex × 顶点色`，所以四角给了不同颜色就是渐变。
            m.colors = _cornerColors ?? WhiteVerts;
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _mf.sharedMesh = m;
        }

        static readonly Color[] WhiteVerts = { Color.white, Color.white, Color.white, Color.white };
        Color[] _cornerColors;

        /// <summary>
        /// **四角顶点色**（顺序 = 左下 · 右下 · 右上 · 左上，与网格顶点同序）。
        /// 原版好几块「底板」的渐变就是靠这个，**不是靠 `Image.m_Color`**：
        /// `Navigation Panel/Background`（8×8 的 `White Square`，`m_Color` 是**白的**）
        /// 之所以是一块**深酒红板**，是因为**同一个 GameObject 上还挂着一个四角顶点色组件**
        /// （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1941.json`，
        /// TL `(0.066,0.027,0.003)` · TR `(0.189,0.005,0)` · BR `(0.226,0.001,0.001)` · BL `(0.019,0.007,0.007)`）。
        /// ⇒ 只补 `m_Color` **补不出那块板**（2026-09-22 实测：渲染出来是一整块白）。
        /// </summary>
        public void SetCornerColors(Color bl, Color br, Color tr, Color tl)
        {
            _cornerColors = new[] { bl, br, tr, tl };
            RebuildMesh();
        }

        /// <summary>当前的四角顶点色（顺序 = 左下 · 右下 · 右上 · 左上）；**没设过时是 `null`**（= 四角全白）。
        /// 🆕 2026-10-04 加：给**软边遮罩**用 —— 它要在「已有的顶点色」上再乘一道 alpha 斜坡，
        /// 而 `Tint` 是**材质色**、量不到顶点色（`SetCornerColors` 一直只写不读）。
        /// ⚠️ 返回的是内部数组本身（**别改它**）—— 自检也只读它。</summary>
        public Color[] CornerColors { get { return _cornerColors; } }

        Rect _uv = new Rect(0f, 0f, 1f, 1f);

        /// <summary>只用贴图的一块（**归一化** uv 矩形）。九宫格/平铺的子块用它。
        /// ⚠️ 贴图的导入设置若是 Clamp，uv 超出 [0,1] 会拉边而不是重复 ——
        ///    所以**平铺走「多块 quad」**（`CreateTiled`），不靠 uv 越界。</summary>
        public void SetUvRect(Rect r) { _uv = r; RebuildMesh(); }

        /// <summary>当前那张 uv 矩形（**只读**）。给自检用 —— 镜像就是「宽为负」，
        /// 断言要量**真值**、不能只量「调过 SetUvRect」（判据 → `SearchingOpponentWindow.ShowOpponent`）。</summary>
        public Rect UvRect { get { return _uv; } }

        // ==================================================================
        //  两种原版 `Image.Type` 的替身（`Sliced` / `Tiled`）
        //  —— 为什么要有：原版弹窗底板是 `40k_popup`（**九宫格**：`m_Border=(169,160,169,160)`、
        //     `m_Rect=359×336`，中间只剩 21×16），我们过去**因为「没有九宫格」把它改成了自建实底**
        //     （`WaitBanner` / `SettingsPanel` 两处都这么写的）。这里补上，就能照原版画。
        // ==================================================================

        /// <summary>原版 `Image.Type = Sliced`：按 sprite 的 border 切九块。
        /// `borderPx` = (左, 下, 右, 上)（Unity `m_Border` 的 x/y/z/w，**贴图 px**）；
        /// `texW/texH` = 整张图的 px 尺寸；`worldW/H` = 目标世界尺寸。
        /// 角块保持原 px 尺寸，边与心拉伸。
        /// 🔴 **「目标太小怎么办」的判据 = 原版 uGUI `Image.GetAdjustedBorders`**（**逐轴**：只在
        /// `border.x + border.z > rect.width` 时才把那两边的角块按 `rect.width ÷ (bL+bR)` 缩）——
        /// 2026-10-04 之前这里两轴共用一个比例、且会为「端帽铺满整张图」的**合法**形状误报（见下面的注释）。</summary>
        /// <param name="borderOutPx">**绘制时的角块像素长**（不传 = 与 `borderPx` 相同）。
        /// 🔴 为什么要分成两个量：原版的 `Image` 有 **`m_PixelsPerUnitMultiplier`**（`40k_square_border` 是 **5**）
        /// —— UV 切分按**图里的真实边宽**（`m_Border` = 13px / 64² 图），而**画出来的角块 = 13 × 5 = 65px**。
        /// 只传一个量的话，13 会让角块只有 13px（太细）、65 又会让 UV 越界（`65/64 > 1` ⇒ 报警退化成单块）。</param>
        /// <param name="fillCenter">**画不画中间那块**。原版的 `Image.m_FillCenter = 0`（`40k_square_border` 就是这样）
        /// ⇒ 中间**留空**，只画那 8 块边角。</param>
        public static GameObject CreateNineSlice(Transform parent, Texture tex, Vector4 borderPx,
                                                 float texW, float texH, Vector3 center,
                                                 float worldW, float worldH, string name,
                                                 Vector4? borderOutPx = null, bool fillCenter = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;
            if (tex == null) { Debug.LogWarning($"[ImageQuad] {name}: 贴图为空，九宫格没建"); return root; }

            float l = borderPx.x, b = borderPx.y, r = borderPx.z, t = borderPx.w;
            // 归一化 uv 分界
            float uL = l / texW, uR = 1f - r / texW, vB = b / texH, vT = 1f - t / texH;
            // 🔴 **2026-10-04 修（真 bug：误报 + 中段被吃掉）**。
            //   旧写法 `if (uR <= uL || vT <= vB)` 把「**端帽正好铺满整张图**」这种**合法**形状也当成了越界：
            //   判据 = 原版 uGUI `Image.GetAdjustedBorders`（`Runtime/UGUI/UI/Core/Image.cs:1479-1506`（关键那一条在 `:1501`：`adjustedRect.size[axis] < combinedBorders`））——
            //   它只在 **`border.x + border.z > rect.width`**（两边边宽之和 **>** 矩形宽）时才按比例缩，
            //   **`uR == uL`（= 边宽之和 == 贴图宽）是合法形状**，中段宽就是 0（`GenerateSlicedSprite`
            //   对「宽 ≤ 0 的那一格」是 `continue` 跳过，`Image.cs:1194-1195`）。
            //   实测受害例：`WF_Special offer_Value` 324×87 · `m_Border = (162,0,162,0)`（L+R **正好** = 324）
            //   ⇒ `uL == uR == 0.5` ⇒ 旧代码每次都打一条「border 比图还大，退回单块」的**假警告**
            //   （`BoosterPackOpenWindow` 每个卡位一枚 `New Card Badge`、`OfferContainer` 19 个变体各一枚 —— 任务书记的实测是 **21 次**），而它根本没有退回单块。
            //   ⇒ 只有**真正的越界**（`uR < uL` / `vT < vB`）才出声。
            //   另：真的越界时把分界**夹成不反向**（下面两行）——旧代码会拿着一个反过来的 uv 去切图
            //   （画出来是垃圾且**静默**）；夹完之后退化成「中段取同一列纹素」，与原版同形。
            if (uR < uL || vT < vB)
                Debug.LogWarning($"[ImageQuad] {name}: border 比图还大（{l}+{r} > {texW} 或 {b}+{t} > {texH}）—— "
                               + "`m_Border` 与贴图尺寸不自洽，中段退回同一列纹素");
            uR = Mathf.Max(uR, uL); vT = Mathf.Max(vT, vB);

            // 目标里三段的长（角块**不缩放**，按 108 px = 1 世界单位）
            float ol = borderOutPx.HasValue ? borderOutPx.Value.x : l;
            float ob = borderOutPx.HasValue ? borderOutPx.Value.y : b;
            float orr = borderOutPx.HasValue ? borderOutPx.Value.z : r;
            float ot = borderOutPx.HasValue ? borderOutPx.Value.w : t;
            float wl = ol / PixelsPerUnit, wr = orr / PixelsPerUnit;
            float hb = ob / PixelsPerUnit, ht = ot / PixelsPerUnit;
            // ⚠️ **目标比「两边角加起来」还小时，按比例把角缩下来** —— 原版 uGUI 也是这么退化的
            //    （`GetAdjustedBorders`：把角块压扁），不这么做的话角块会**互相重叠**画出去。
            // 🔴 **2026-10-04：按【轴】缩，不是两轴共用一个比例**（判据同上 `GetAdjustedBorders` ——
            //    它是 `for (axis = 0; axis <= 1; axis++)` **逐轴**判 `rect.size[axis] < border[axis]+border[axis+2]`）。
            //    旧写法 `sc = min(1, W/(wl+wr), H/(hb+ht))` 两轴共用：**一个轴挤了，另一个轴的角块也跟着缩**。
            //    实测受害例：提示条用 `40k_popup`（边 169/160）塞进 1323×90 —— 只有**竖**着挤，
            //    原版横边的两个端帽仍是 **169px**（中段 985），旧写法把它们缩成 **47.5px**（中段 1228）。
            //    ⚠️ 受影响的是「某一轴被挤」的那些件（横竖两轴都够的、或被挤的那个轴本来就该缩的，值不变）；
            //    块数不变（被挤那一轴的中段该归零还是归零）。已记在交接报告里。
            float scX = (wl + wr) > worldW ? worldW / Mathf.Max(1e-4f, wl + wr) : 1f;
            float scY = (hb + ht) > worldH ? worldH / Mathf.Max(1e-4f, hb + ht) : 1f;
            wl *= scX; wr *= scX; hb *= scY; ht *= scY;
            float wm = worldW - wl - wr, hm = worldH - hb - ht;
            if (wm < 0f) { wm = 0f; }
            if (hm < 0f) { hm = 0f; }

            float x0 = -worldW * 0.5f, y0 = -worldH * 0.5f;
            float[] xs = { x0, x0 + wl, x0 + wl + wm, x0 + worldW };
            float[] ys = { y0, y0 + hb, y0 + hb + hm, y0 + worldH };
            float[] us = { 0f, uL, uR, 1f };
            float[] vs = { 0f, vB, vT, 1f };

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    if (!fillCenter && i == 1 && j == 1) continue;   // 原版 `m_FillCenter = 0` ⇒ 中间那块不画
                    float w = xs[i + 1] - xs[i], h = ys[j + 1] - ys[j];
                    if (w <= 0f || h <= 0f) continue;
                    var q = Create(root.transform, tex,
                                   new Vector3((xs[i] + xs[i + 1]) * 0.5f, (ys[j] + ys[j + 1]) * 0.5f, 0f),
                                   h, new Vector2(0.5f, 0.5f), $"{name}_{i}{j}");
                    if (q == null) continue;
                    q.SetAspect(w / h);
                    q.SetUvRect(new Rect(us[i], vs[j], us[i + 1] - us[i], vs[j + 1] - vs[j]));
                }
            return root;
        }

        /// <summary>原版 `Image.Type = Tiled`：按**贴图原始尺寸**重复铺（不是拉伸）。
        /// 用「多块 quad」实现（不依赖贴图的 wrapMode）。</summary>
        public static GameObject CreateTiled(Transform parent, Texture tex, float tilePxW, float tilePxH,
                                             Vector3 center, float worldW, float worldH, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;
            if (tex == null) { Debug.LogWarning($"[ImageQuad] {name}: 贴图为空，平铺没建"); return root; }

            float tw = tilePxW / PixelsPerUnit, th = tilePxH / PixelsPerUnit;
            int nx = Mathf.Max(1, Mathf.CeilToInt(worldW / tw));
            int ny = Mathf.Max(1, Mathf.CeilToInt(worldH / th));
            float x0 = -worldW * 0.5f, y0 = -worldH * 0.5f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                {
                    float px = x0 + i * tw, py = y0 + j * th;
                    float w = Mathf.Min(tw, x0 + worldW - px), h = Mathf.Min(th, y0 + worldH - py);
                    if (w <= 0f || h <= 0f) continue;
                    var q = Create(root.transform, tex, new Vector3(px + w * 0.5f, py + h * 0.5f, 0f),
                                   h, new Vector2(0.5f, 0.5f), $"{name}_{i}{j}");
                    if (q == null) continue;
                    q.SetAspect(w / h);
                    // 最后一块只画一部分 ⇒ uv 也跟着截（否则会被压扁）
                    q.SetUvRect(new Rect(0f, 0f, w / tw, h / th));
                }
            return root;
        }

        /// <summary>世界坐标是不是点在这张图上（按钮命中测试用）</summary>
        public bool Contains(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            return l.x >= -anchor.x * WorldW && l.x <= (1 - anchor.x) * WorldW
                && l.y >= -anchor.y * WorldH && l.y <= (1 - anchor.y) * WorldH;
        }
    }
}
