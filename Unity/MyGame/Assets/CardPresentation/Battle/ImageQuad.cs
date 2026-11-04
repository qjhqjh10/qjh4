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
        /// （判据 = `Core/LayoutSpace.cs` 的 `VisibleHeight` 那条：「可见高度固定 10 个世界单位 = 1080 px」
        ///  ⇒ `LayoutSpace.Px()` 就是 `px ÷ 108`）。写成 `100` 会让**每一条九宫格/平铺整体大 8%**，
        /// 实测：左栏 `Highlight` 的角块 `30 ÷ 0.92 = 32.61` 画布像素，渲出来是 **35.22**（= 32.61 × 1.08）
        /// —— `Editor/RewardsScene.cs` 的「§三·b4-b」那条断言量到了、且当时是红的。
        ///
        /// ⚠️ **不是「和 `Label` 同口径」**（原来这么写是错的）：`Battle/Label.cs` 的 `PixelsPerUnit` 另有一份**私有**的
        /// `PixelsPerUnit = 100f`，管的是**点阵后端**的字块尺寸与 HUD 的整数 scale 档，**与本类这个常量无关**
        /// （改这里不会动它）。本常量**只**被下面 `CreateNineSlice` / `CreateTiled` 两处用来把 px 折成世界尺寸。
        ///
        /// 📌 值写成 `LayoutSpace` 的推导式（而不是字面量 `108f`）—— 让「换算只有一处」是**结构性**的：
        /// `DesignPxH / DesignHeight` 一变，这里跟着变，不会再各写一个数（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</summary>
        public const float PixelsPerUnit = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;

        public Vector2 anchor = new Vector2(0.5f, 0.5f);

        MeshRenderer _mr;
        MeshFilter _mf;
        [SerializeField] Texture _tex;
        [SerializeField] float _worldH = 1f;
        [SerializeField] float _aspect = 1f;
        // 🆕 **2026-10-17（第四十八会话 · `N53②` SkewedImage）**：斜切量（`skewVector` 原样）。
        //   原版 `SkewedImage.OnPopulateMesh` 的换算（decomp 实读）：
        //   `x' = x + y * skewX` · `y' = y + x * skewY`（顶点在**图自己的局部空间**、以中心为原点）。
        //   `_skewOffX/_skewOffY` 只给**九宫子块**用：块心在父件里的偏移要一起进算式，
        //   否则每块各绕自己中心切 ⇒ 块与块之间错开。
        [SerializeField] float _skewX, _skewY, _skewOffX, _skewOffY;

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

        /// <summary>🔴 **2026-10-17（第四十八会话 · 桶 B）：克隆出来的图块必须重绑 `_mf/_mr`。**
        /// <para>它们和 `Label` 的 `_tmp/_mr` 是同一个病：**运行时字段、没有序列化** ⇒
        /// `Instantiate` 一个含 `ImageQuad` 的预制体时，子节点克隆了、引用却是 null。</para>
        /// <para>后果：`SetTexture` / `SetRenderQueue` 静默空转；一旦走到 `SetWorldHeight`
        /// （`ApplyMeasuredHeight` 那条链）⇒ `RebuildMesh` 在 `_mf.sharedMesh` 上 NRE。</para>
        /// <para>⚠️ `_tex/_worldH/_aspect` 已标 `[SerializeField]` ⇒ 克隆体保留原几何，
        /// 重绑后 `RebuildMesh` 得到的是**同一块尺寸**（不是出厂 1×1）。</para></summary>
        public void RebindAfterClone()
        {
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mf == null)
            {
                Debug.LogError("[ImageQuad] 克隆后重绑失败：找不到 `MeshFilter` ⇒ 这一块图会是死的（⛔ 不静默）。");
                return;
            }
            RebuildMesh();
        }

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
        /// `SetAspect(...)`** 把显示比例拉回来（现读有 6 处这么补：`Shell/WindowButton.cs`（⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs`）的
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
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.mainTexture = t != null ? t : (prev != null ? prev : MissingQuadTexture(gameObject.name));   // 🆕 `B118-W14`：运行期换图这一口原来**没有判空** ⇒ 绑 null 时 Unity 用内置白图 = 一块不透明白块（`Create` 那一档有判空、`MenuDraw.Rect` 也有，只有这里漏）
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
        /// 实测受害链 = `WindowButton.RefreshGray`（`Shell/WindowButton.cs`；⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs`）→ 这里
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
            _mr.sharedMaterial = m; SyncShineUv1();   // `B-326`：换成 `WildcardShine` 时补 `uv1`（同物理行追加，行数不变；其余材质这句立刻返回，网格一个字节不变）
        }

        // ============================================================ 去饱和通道（`_GreyScale`）
        //
        // 🆕 **2026-10-24（`W6` · `G1`）**。卡组编辑卡池那条「张数条」的底板要吃原版那套**去饱和**
        // （判据 → `d:/4/Unity/资料/普查产出_1024d/G1_卡组编辑变灰判据.md` §1 / §2·B：
        //  `CollectionCard.counter.greyscale` 的 `ToggleGreyScale(canAdd ^ 1, 0.25f)` **只灰 `Counter` 那一张图**，
        //  旁边的数字是独立子件、**不灰**）。
        //
        // 🔴 **⛔ 别用 `SetTint` 表达「灰」** —— 材质色是**乘法**（同 `CardView.SetHighlight` 的 doc：
        //    `(0.55,0.55,0.58)` 出来是「更暗的红卡」，不是原版那种**降饱和**）。原版 `_GreyScale` 的算式
        //    = `lerp(rgb, dot(rgb,(0.299,0.587,0.114)), g)`：只降饱和、**不变暗、不动 alpha**。
        //
        // 🔴 **惰性换材质，⛔ 不是把 `Create` 那一句全局换掉**：本类建的 quad 全工程几百个，
        //   只有**真被要求灰**的那几个才会从 `Sprites/Default` 换到 `CardPresentation/CardSprite`
        //   （后者 = 前者 + `_GreyScale`；默认 0 = 逐像素一致，判据 → `Resources/Shaders/CardSprite.shader` 头部）。
        //   ⇒ **没被灰过的 quad 一个字节都不变** —— 这条影响面不跑 Unity 也说得清（`Create` 那句没动）。

        /// <summary>`_GreyScale` 的属性 id（原版 `UIImageGreyscaleController.GREYSCALE_SHADER_PROPERTY` 那个名）。</summary>
        static readonly int GreyProp = Shader.PropertyToID("_GreyScale");
        static bool _greyShaderWarned;

        float _grey;
        MaterialPropertyBlock _greyMpb;

        /// <summary>当前去饱和度：**0 = 原色 / 1 = 全灰**（原版 `_GreyScale` 的语义）。
        /// <para>⚠️ 读的是**渲染器上真正生效**的那个值（`MaterialPropertyBlock` 优先，没设过再读材质）；
        /// 材质根本没有这个属性 ⇒ 返回 **`-1`**（与「0 = 不灰」区分开 —— 自检要能认出「这条通道压根没接上」，
        /// ⛔ 别让 `-1` 混进「不灰」里变成假绿，所以调用方要断 `>= 0`）。</para></summary>
        public float Grey
        {
            get
            {
                if (_mr == null) return -1f;
                var m = _mr.sharedMaterial;
                if (m == null || !m.HasProperty(GreyProp)) return -1f;
                var mpb = new MaterialPropertyBlock();
                _mr.GetPropertyBlock(mpb);
                return mpb.isEmpty ? m.GetFloat(GreyProp) : mpb.GetFloat(GreyProp);
            }
        }

        /// <summary>设置去饱和度（`0..1`，`1` = 全灰）。与 `CardView.SetGrey` 同一口径（同一算式、同一属性名）。
        /// <para>⚠️ **不做补间**：原版 `ToggleGreyScale(…, 0.25f)` 那条 0.25 s 的曲线，其常量**没有读二进制核实**
        /// （`G1` §6.3 明确标着「未查清」，连反向补间的符号都没解）⇒ 这里**只做瞬切**，⛔ 不猜一个数。</para></summary>
        public void SetGrey(float g)
        {
            _grey = Mathf.Clamp01(g);
            if (_mr != null)
            {
                EnsureGreyCapable();
                var m = _mr.sharedMaterial;
                if (m != null && m.HasProperty(GreyProp))
                {
                    if (_greyMpb == null) _greyMpb = new MaterialPropertyBlock();
                    _mr.GetPropertyBlock(_greyMpb);
                    _greyMpb.SetFloat(GreyProp, _grey);
                    _mr.SetPropertyBlock(_greyMpb);
                }
                // 找不到 shader 时 `EnsureGreyCapable` 已经喊过（不静默）—— 这里不再重复喊。
            }
            // 🆕 软边切出来的子块是**独立 quad**（同 `SetTint` 那条理由）⇒ 灰度也要跟着走，
            //    否则「边带那一条还是彩的」（静默）。没登记过子块的宿主这里是一次空循环。
            for (int i = 0; i < _softKids.Count; i++)
                if (_softKids[i] != null) _softKids[i].SetGrey(_grey);
        }

        /// <summary>材质不是 `_GreyScale` 那一支时**换一次**（贴图 / 染色 / 显式渲染队列都带过去）。
        /// ⚠️ 旧材质**不销毁** —— 它可能是调用方经 `SetMaterial` 传进来的共享材质（见那条 doc），销毁会连带别人。
        /// 每个「真被要求灰」的 quad 最多多留一份，数量有界。</summary>
        void EnsureGreyCapable()
        {
            var old = _mr.sharedMaterial;
            if (old != null && old.HasProperty(GreyProp)) return;
            var sh = Shader.Find("CardPresentation/CardSprite");
            if (sh == null)
            {
                if (!_greyShaderWarned)
                {
                    _greyShaderWarned = true;
                    Debug.LogWarning("[ImageQuad] 找不到 `CardPresentation/CardSprite` —— 去饱和**不会生效**"
                                   + "（`Resources/Shaders/CardSprite.shader` 在不在？）；材质保持原样。");
                }
                return;
            }
            var m = new Material(sh);
            // 🔴 显式分好的渲染队列必须带过去 —— 见 `SetMaterial` 那条 doc（掉队 = 那块图沉到自己底图之下，静默）。
            int q = old != null ? old.renderQueue : -1;
            m.mainTexture = _tex;
            if (q >= 0) m.renderQueue = q;
            if (old != null && old.HasProperty("_Color")) m.color = old.color;
            _mr.sharedMaterial = m;
        }

        public void SetTint(Color c)
        {
            ApplyTintColor(c);              // 🆕 `B-190`：开了顶点色通道的 quad 改走 `Mesh.colors`（见类尾那一段）
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
            get { return CurrentTintColor(); }   // 🆕 `B-190`：顶点色通道开着时读那一份（否则逐字等于原来那句）
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

        /// <summary>斜切（原版 `SkewedImage.skewVector = (x, y)`）。
        /// 换算逐句照 `SkewedImage__OnPopulateMesh.c`：`x' = x + y·skewX` · `y' = y + x·skewY`
        /// （顶点在**图自己的局部空间**、中心为原点）。
        /// <para>`offsetX/offsetY` 只给**九宫子块**用 —— 块心在父件里的偏移要一起进算式，
        /// 否则每块各绕自己中心切、块与块之间会错开。</para></summary>
        public void SetShear(float skewX, float skewY, float offsetX = 0f, float offsetY = 0f)
        {
            _skewX = skewX; _skewY = skewY; _skewOffX = offsetX; _skewOffY = offsetY;
            RebuildMesh();
            NotifySoftEdgeChanged();
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
            if (_nineOn) { RebuildNineSliceMesh(); return; } float w = WorldW, h = WorldH, f = Mathf.Clamp01(_fillAmount); float x0 = -anchor.x * w, y0 = -anchor.y * h;   // 🔴 `B-247`：九宫格态走另一支（**同物理行追加**，行数不变；`_nineOn` 默认 false ⇒ 既有几百个 quad 逐字不变）。为什么非要有这一句 → 类尾那一段注释
            Rect uv = _uv; Color[] cols = _cornerColors;
            // 🆕 2026-10-30（B102_AUD05）`Image.Type = Filled`（Horizontal · `m_FillOrigin=1` 自右向左）的替身 = 逐句照 uGUI `Image.GenerateFilledSprite`：
            //   只留矩形与 uv 的**右段**（`v.x = v.z − (v.z−v.x)·amount` · `tx0 = tx1 − span·amount`）；`amount < 0.001` 时**一个顶点都不生成**（原版那一支直接 return）；`f == 1`（出厂）⇒ 整段不进、与旧代码逐字相同。
            if (f < 1f)
            {
                if (f < 0.001f) { _mf.sharedMesh = new Mesh { name = "ImageQuad" }; return; }
                float t = 1f - f, x1 = x0 + w;
                uv = new Rect(uv.xMin + uv.width * t, uv.yMin, uv.width * f, uv.height); x0 = x1 - w * f; w = x1 - x0;
                if (cols != null) cols = new[] { Color.Lerp(cols[0], cols[1], t), cols[1], cols[2], Color.Lerp(cols[3], cols[2], t) };
            }
            var m = new Mesh { name = "ImageQuad" };
            var verts = new[] { new Vector3(x0, y0, 0f), new Vector3(x0 + w, y0, 0f), new Vector3(x0 + w, y0 + h, 0f), new Vector3(x0, y0 + h, 0f) };
            // `SkewedImage`：x' = x + (y + 父系偏移y) * skewX · y' = y + (x + 父系偏移x) * skewY
            if (_skewX != 0f || _skewY != 0f)
                for (int i = 0; i < verts.Length; i++)
                {
                    float x = verts[i].x, y = verts[i].y;
                    verts[i].x = x + (y + _skewOffY) * _skewX;
                    verts[i].y = y + (x + _skewOffX) * _skewY;
                }
            m.vertices = verts;
            // UV 默认是整张图；九宫格/平铺靠 `SetUvRect` 只取一块（`fill < 1` 时这里已经是右段）
            m.uv = new[] { new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax) };
            // 顶点色：默认白（= 不染色）。`Sprites/Default` 会 `tex × 顶点色`，所以四角给了不同颜色就是渐变。
            m.colors = CornerColorsWithTint(cols ?? WhiteVerts);   // 🆕 `B-190`：通道没开 ⇒ 原样返回（零行为变化）
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _mf.sharedMesh = m; SyncShineUv1();   // `B-326`：`WildcardShine` 的 quad 才写 `uv1`（同物理行追加，行数不变）
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
        /// —— UV 切分按**图里的真实边宽**（`m_Border` = 13px / 64² 图），而**画出来的角块 = `m_Border ÷ ppuMul`**
        /// （uGUI `Image.cs:1157`：`multipliedPixelsPerUnit = pixelsPerUnit / pixelsPerUnitMultiplier`
        /// ⇒ **乘数在【分母】**，不是「×5」）。⚠️ 本条原来写成「13 × 5 = 65px」**方向反了**
        /// —— 2026-10-19 就地订正（`A1939`，判据 → `资料/普查产出_第二十九会话/A1933_文字底板九宫格.md` §七·①；
        /// 旁证 = `Shell/MenuDraw.cs:1599-1601` 那处**渲图核过**的「角块 = `m_Border ÷ ppuMul`」）。
        /// 只传一个量的话：13 当绘制尺寸 = **角块比原版大**、而 65 又会让 UV 越界（`65/64 > 1` ⇒ 报警退化成单块）。</param>
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

            // ✅ 已复刻（2026-11-04 · B142-E17）：算式抽到类尾 `NineSliceLayout`，与 `ResliceNineSlice` **共用同一份**
            //   （换 sprite 那一刻要按新图的 border 重切，见类尾那一段判据）。
            float[] xs, ys, us, vs;
            NineSliceLayout(name, texW, texH, borderPx, borderOutPx ?? borderPx, worldW, worldH,
                            out xs, out ys, out us, out vs);

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

        // ============================================================ 🆕 2026-10-30（`B102_AUD05`）：`Image.Type = Filled`
        //
        // **为什么要**：原版 `ChatPreview/Container` 是 `Image.Type = Filled`（真包 prefab 实读：
        //   `m_Type=3` · `m_FillMethod=0`(Horizontal) · `m_FillOrigin=1`(Right) · `m_FillClockwise=1`(Horizontal 不用它)），
        //   `ChatPreview__OnBeforeInitialization.c` 出厂 `UnityEngine_UI_Image__set_fillAmount(…, 0)`，
        //   `ChatPreview__Show.c` 再 `DOFillAmount(container, 1, 0.4).From(0).SetDelay(0.2)`
        //   （常量 `0x1834b2bb8=1` · `0x1834b3158=0.4` · `0x1834b2bb0=0.2`，`read_literal.py` 现读）。
        //   我们过去在 `Shell/MainMenuRuntime.ShowChatPreview` 里**如实出声说做不到** —— 现在补上。
        //
        // **怎么做的（纯 C# 侧，⛔ 没动 shader）**：`RebuildMesh` 按 uGUI `Image.GenerateFilledSprite` 的
        //   Horizontal 分支裁**网格 + uv**（只留右段；`< 0.001` 一个顶点都不生成）—— 顶点少画 = 像素上看不见，
        //   与原版「`Image` 只生半张网格」是同一个机制，且不需要重编 shader（见 CLAUDE.md「别为了它去改 shader」）。
        //
        // **如实标的差异**（铁律 3 / 18）：
        //   ① 原版 `fillAmount` 是 `Image` 的**序列化字段**、动画系统可驱动；这里是**运行时字段、不序列化**
        //      ⇒ 预制体克隆体 / 域重载之后回到 **1（= 不裁）**，由调用方自己重设。**默认 1 = 出厂不裁**，
        //      所以全工程既有的几百个 quad **一个字节都不变**（`Create` 那条路没改）。
        //   ② 只做 **Horizontal + origin Right** —— 全库只有 `ChatPreview/Container` 这一处 Filled，
        //      其余的 FillMethod/origin 没有判据 ⇒ ⛔ 不编通吃档（没判据的部分不发明）。
        //   ③ 软边子块（`MenuDraw.ApplySoftEdges` 切出来的独立 quad）**不跟着裁** —— 两套几何没有共用的判据；
        //      真被这么用时喊一条警告（⛔ 不静默失败）。
        //   ④ 命中测试（`Contains`）**仍是整框** —— 与 uGUI 一致（`Image` 的 raycast 按 rect 走，
        //      `alphaHitTestMinimumThreshold` 默认 0 ⇒ 不看 fill）。

        float _fillAmount = 1f;
        static bool _fillSoftWarned;

        /// <summary>填充量 `0..1`（原版 `Image.fillAmount` 的 Horizontal 档）。**1 = 不裁（出厂）**；
        /// `0` ⇒ 整块不画（与 uGUI `GenerateFilledSprite` 的 `m_FillAmount < 0.001f` 那一支同义）。</summary>
        public float FillAmount { get { return _fillAmount; } }

        /// <summary>设置填充量（**几何会重建**）。只服务原版 `Image.Type = Filled` 的那几块
        /// —— `ChatPreview/Container` 的 `m_FillMethod=0` / `m_FillOrigin=1`（自右向左填）。
        /// ⚠️ 期望值不要读 `FillAmount` 证明自己的行为（自证）—— 判据 = 原版 prefab 字段。</summary>
        public void SetFillAmount(float amount)
        {
            float a = Mathf.Clamp01(amount);
            if (Mathf.Approximately(a, _fillAmount)) return;
            _fillAmount = a;
            if (a < 1f && _softKids.Count > 0 && !_fillSoftWarned)
            {
                _fillSoftWarned = true;
                Debug.LogWarning("[ImageQuad] 软边子块**不跟随** fillAmount 裁剪（两套几何没有共用的判据）"
                               + " —— 这一块的边带会停在整框上（⛔ 不静默）。");
            }
            RebuildMesh();
            NotifySoftEdgeChanged();
        }

        // ============================================================ `B-190`：顶点色染色通道（追加在类尾）
        //
        // **为什么要这条通道**：原版 uGUI 的悬停/按下色偏走 `CanvasRenderer.SetColor` —— 那是**顶点色**
        //   （`EverguildButton.m_Transition = ColorTint` → `Graphic.CrossFadeColor`），**不是**材质 `_Color`。
        //   我们 `SetTint` 一直写的是 `sharedMaterial.color`（=`_Color`）—— 对 `Sprites/Default` 那类
        //   「`tex × _Color × 顶点色`」的 shader 两者等效，但对**没有 `_Color`** 的 shader 就整层落空。
        //
        // **判据（原版实读，2026-11-01）**：材质 `UI Orange Button To Purple` 的 shader
        //   `Shader Graphs/Sprite HUE Color change` ——
        //   · 属性表 `_Brightness _Contrast _Hue_Offset _MainTex _Saturation`，**没有 `_Color`**
        //     （`WarpforgeVFX/Runtime/OriginalShaderBlendTable.cs:422`）；
        //   · VS 的输入签名里有 `COLOR 0 xyzw`，体里 `mul o2.xyzw, cb0[130], v4` 把它原样转给 PS 的 `v2`；
        //   · PS 末句 `mul o0.xyzw, r0.xyzw, v2.xyzw` —— **最终乘顶点色**
        //     （`工具/disasm_dxbc.py "Sprite HUE Color change" --stage vs|ps` 现读）。
        //   ⇒ 原版「根 Image 有色偏」与「材质没有 `_Color`」并不矛盾：色偏就是**顶点色**这一路。
        //
        // **影响面（为什么既有调用点一个字节都不变）**：`_vertexTint` 默认 `false`
        //   ⇒ `SetTint` / `Tint` / `RebuildMesh` 三处走的分支与改动前**逐字相同**
        //   （`CornerColorsWithTint` 在通道关闭时原样返回入参、不分配）。只有调用方**显式**
        //   `SetVertexTintChannel(true)` / `SetVertexTint(c)` 过的 quad 才换路。
        //   ⚠️ 与 `_Color` **不会双重相乘**：这条通道只写顶点色、**不写 `_Color`**；
        //     目标 shader 本来就没有 `_Color`，所以顶点色在整个管线里只被乘一次。
        //
        // ⚠️ **如实标的差异**：这是「按原版机制补齐」，不是自设计 —— 原版本来就乘顶点色。
        //   唯一我们自己定的是「怎么在 `ImageQuad` 里表达它」（opt-in 开关而不是全局改 `SetTint`），
        //   理由：`ImageQuad` 是全工程共用件，全局改会把所有「shader 没有 `_Color`」的站点**静默**换路。

        bool _vertexTint;
        Color _vertexTintColor = Color.white;

        /// <summary>这条 quad 现在是不是走**顶点色**染色（而不是材质 `_Color`）。自检/诊断用。</summary>
        public bool UsesVertexTint { get { return _vertexTint; } }

        /// <summary>开关「顶点色染色」通道。判据与影响面 → 本节头部注释。
        /// <para>打开后 `SetTint` / `Tint` 都改走 `Mesh.colors`（**不再写 `_Color`**，于是没有 `_Color`
        /// 的 shader 也不会再报「Material doesn't have a color property」）；关掉时回到原来那条路。</para>
        /// <para>⚠️ 打开时那一份颜色默认是**白**（= 不偏色），调用方随后照常用 `SetTint` 推色即可。</para></summary>
        public void SetVertexTintChannel(bool on)
        {
            if (_vertexTint == on) return;
            _vertexTint = on;
            if (_mf != null) RebuildMesh();     // 把 `_vertexTintColor` 重新乘进四角顶点色
            // 软边切出来的子块是**独立 quad**、共用同一支材质（`SetTint` 会逐块转过去，见那一处）——
            // 只给宿主开、不给子块开，子块就会继续去写不存在的 `_Color`（静默报错路）。没登记过子块时是空循环。
            for (int i = 0; i < _softKids.Count; i++)
                if (_softKids[i] != null) _softKids[i].SetVertexTintChannel(on);
        }

        /// <summary>直接设一次顶点色（**顺带打开通道**）。等价于 `SetVertexTintChannel(true)` + `SetTint(c)`。</summary>
        public void SetVertexTint(Color c)
        {
            _vertexTint = true;
            ApplyTintColor(c);
        }

        /// <summary>`SetTint` 的实现体（**逐字保留**原来那一句作为 `_vertexTint == false` 的分支）。
        /// ⚠️ 顶点色这一支要 `RebuildMesh`：四角顶点色是在建网格时写进 `Mesh.colors` 的。</summary>
        void ApplyTintColor(Color c)
        {
            if (_vertexTint)
            {
                _vertexTintColor = c; UploadVertexTint();
                // 子块可能是**之后**才被 `MenuDraw.ReapplySoftEdges` 重切出来的（那一刻还没有通道）——
                // 每次推色顺手补齐，避免子块回落到写不存在的 `_Color`。没登记过子块时是空循环。
                for (int i = 0; i < _softKids.Count; i++)
                    if (_softKids[i] != null) _softKids[i].SetVertexTintChannel(true);
                return;
            }
            if (_mr != null && _mr.sharedMaterial != null) _mr.sharedMaterial.color = c;
        }

        /// <summary>把当前顶点色写进网格（**只改 `Mesh.colors`，不重建几何**）。
        /// <para>🔴 为什么不直接 `RebuildMesh()`：`WindowButton.Update` 在 0.1 s 补间里**每帧**调一次
        /// `SetTint` ⇒ 每次重建整块网格（新建 `Mesh` + 顶点/uv/三角数组）会在悬停那几帧里反复分配。
        /// 几何没变、只有颜色变 ⇒ 走 `Mesh.colors` 这一条就够（uGUI 的 `SetColor` 也是只动顶点色）。</para>
        /// <para>⚠️ `fillAmount &lt; 1` 那一档的四角色要先按填充量插值（见 `RebuildMesh`），这里不重算
        /// ⇒ 退回 `RebuildMesh()`（只有 `ChatPreview/Container` 用填充，且它不开这条通道）。</para></summary>
        void UploadVertexTint()
        {
            if (_mf == null) return;
            var mesh = _mf.sharedMesh;
            if (mesh == null || mesh.vertexCount != 4 || _fillAmount < 1f) { RebuildMesh(); return; }
            mesh.colors = CornerColorsWithTint(_cornerColors ?? WhiteVerts);
        }

        /// <summary>`Tint` 的实现体（同上：通道关闭时逐字等于原来那句）。</summary>
        Color CurrentTintColor()
        {
            if (_vertexTint) return _vertexTintColor;
            return (_mr != null && _mr.sharedMaterial != null) ? _mr.sharedMaterial.color : Color.white;
        }

        /// <summary>把 `_vertexTintColor` **乘进**四角顶点色（`RebuildMesh` 用）。
        /// 🔴 通道关闭、或颜色本来就是白 ⇒ **原样返回入参**（同一个数组、零分配）——
        /// 这正是「既有几百个 quad 一个字节都不变」的结构性保证。</summary>
        Color[] CornerColorsWithTint(Color[] cornerColors)
        {
            if (!_vertexTint || _vertexTintColor == Color.white) return cornerColors;
            var dst = new Color[4];
            for (int i = 0; i < 4; i++)
                dst[i] = cornerColors != null ? cornerColors[i] * _vertexTintColor : _vertexTintColor;
            return dst;
        }

        // ============================================================ `B-247`：`Image.Type = Sliced` 的原版九宫格（**追加在类尾**）
        //
        // **为什么要它**：原版 `Level Up Effect` / `Level Up Effect Down` 两颗 `Image` 是 `m_Type = 1`（Sliced，九宫格），
        //   而本链过去是「整张图拉伸的单个 quad」⇒ 128² 的八边形环被按 2.80 × 3.81 的比例**整体拉开**（角与边一起被拉）。
        //
        // **为什么是「一个网格里 9 块」而不是 `CreateNineSlice` 的「9 个子 quad」**（关键，⛔ 别改成后者）：
        //   用这两件的 shader（`WarpforgeVFX/UI/UI Border Highlight`）把**极坐标原点**放在网格 `uv0.zw` 里
        //   ——原版 `UIBorderGlow.ModifyMesh` 给**整张 Image 的每个顶点写同一个值**，我们由 `UIViewportUvWriter` 写。
        //   9 个子 quad ⇒ 每个子件各写自己 `transform.position` ⇒ **9 个不同的圆心**，画面会碎成 9 块径向片段；
        //   而且 `AnimateUIMaterialProperty` / `UIBorderGlow` 都是**按单个 `MeshRenderer`** 挂的，9 个子件只挂得上其中一块。
        //   uGUI 自己也是**一个 `Image` 生成一张含 9×4 个顶点的网格** ⇒ 单网格才是原版形状。
        //
        // **uv 怎么给**（判据 = 原版 uGUI `Image.GenerateSlicedSprite`，
        //   `com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs:1130-1200` 现读）：每一块拿 **sprite 自己的那一段 uv**
        //   （`GetInnerUV` = `m_Border / m_Rect` 归一化），⛔ **不是**把整张图压进每一块。
        //   ⇒ shader 里的 `uv2 = puls*(uv−0.5)+0.5 + nz` 于是**在各自的分段 uv 上**做脉动/噪声位移 —— 这正是**原版行为**
        //   （原版同样是 9 块各带自己的 uv 子矩形进同一个 shader），⛔ 不是我们引入的观感变化。
        //
        // **几何**（判据 = 原版 `Image.GetAdjustedBorders`，同文件 `:1479-1509`）：边框在**矩形局部单位**里四边同值
        //   `m_Border / multipliedPixelsPerUnit`（`multipliedPixelsPerUnit = pixelsPerUnit × m_PixelsPerUnitMultiplier`，
        //   `pixelsPerUnit = sprite.pixelsPerUnit / canvas.referencePixelsPerUnit`）；只有「两边之和 > 该轴边长」时才按轴等比缩。
        //   本方法**直接吃世界单位**的边框（调用方把原版那两个比值折好再传），不在这里做 px→world 的换算。
        //
        // ⚠️ **`_uv`（`SetUvRect`）/ `_fillAmount` / 斜切 / 软边子块都不参与九宫格** —— 这四个今天在这两件上都没有消费者；
        //   真被同时用到会**出声一次**（`_nineWarnedMixed`），⛔ 不静默。
        //
        // 🔴 **持久性（为什么 `RebuildMesh` 第一句要有那个 dispatch）**：`CardDetailPopup` 的升级演出把这两件当
        //   `FadeImage`（`CardDisplayLevelUp.FadeImage.quad`，绑在 `CardDetailPopup.cs:3012-3013`），走
        //   `quad.SetTint(...)` → `ApplyTintColor` → `UploadVertexTint` → 顶点数 ≠ 4 ⇒ `RebuildMesh()`
        //   ⇒ 没有那句 dispatch，**九宫格会在动画第一帧被静默换回「整张拉伸」**。
        //
        // 判据出处（全部现读 2026-11-01）：
        //   `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`
        //   · Image **2545**（GameObject 338 `Level Up Effect`）：`m_Type 1` · `m_PixelsPerUnitMultiplier 0.5`
        //   · Image **2057**（GameObject 404 `Level Up Effect Down`）：`m_Type 1` · `m_PixelsPerUnitMultiplier 1.11`
        //   · sprite `OctagonUI Filled SDF` / `OctagonUI Border SDF`：`m_Border (52,52,52,52)` · `m_Rect 128×128` · `m_PixelsToUnits 100`
        //   · 同场景 CanvasScaler：`m_ReferencePixelsPerUnit 100` ⇒ `pixelsPerUnit = 100/100 = 1`
        //     ⇒ `multipliedPixelsPerUnit = 1 × ppuMul` ⇒ 边框(矩形单位) = `52 / ppuMul`
        //   · 两颗 Image 的 RectTransform `m_LocalScale = 0.004545454`（= 1/220）⇒ 矩形单位 → 世界单位 ×0.004545454

        Vector4 _nineBorder;        // (左, 下, 右, 上) —— **世界单位**（= 本 quad 的局部单位）
        Vector4 _nineUvBorder;      // (左, 下, 右, 上) —— **归一化**（0..1，= `m_Border / m_Rect`）
        bool _nineOn, _nineFillCenter = true;
        static bool _nineWarnedMixed;

        /// <summary>这块 quad 现在是九宫格几何吗（自检用；`false` = 出厂的单 quad）。</summary>
        public bool IsNineSlice { get { return _nineOn; } }
        /// <summary>九宫格的边框（世界单位，(左,下,右,上)）。⚠️ **这只当「配置」读** ——
        /// ⛔ 不要拿它证明几何对（那是自证）；几何要读网格顶点。</summary>
        public Vector4 NineSliceBorderWorld { get { return _nineBorder; } }
        /// <summary>九宫格的 uv 分段（归一化，(左,下,右,上)）。</summary>
        public Vector4 NineSliceUvBorder { get { return _nineUvBorder; } }

        /// <summary>把这块 quad 切成原版 `Image.Type = Sliced` 的九宫格（**一个网格里 9 块**）。
        /// 两个边框参数都是 **(左,下,右,上)**：<paramref name="borderWorld"/> 世界单位（会被 `GetAdjustedBorders`
        /// 的同款规则按轴夹取）· <paramref name="uvBorder"/> 归一化 0..1。</summary>
        public void SetNineSlice(Vector4 borderWorld, Vector4 uvBorder, bool fillCenter = true)
        {
            _nineBorder = borderWorld;
            _nineUvBorder = uvBorder;
            _nineFillCenter = fillCenter;
            _nineOn = true;
            RebuildMesh();          // ← 走类尾那一段说的 dispatch，几何 + 顶点色一起重建
            NotifySoftEdgeChanged();
        }

        /// <summary>退回单 quad（自检的**反证**要用：同一个对象上关掉九宫格，顶点数必须回到 4）。</summary>
        public void ClearNineSlice()
        {
            if (!_nineOn) return;
            _nineOn = false;
            RebuildMesh();
            NotifySoftEdgeChanged();
        }

        /// <summary>九宫格网格：9 块 × 4 顶点。顶点序与三角序**照抄 uGUI `Image.AddQuad`**
        /// （BL·TL·TR·BR / `(0,1,2)(2,3,0)`）—— 与 `RebuildMesh` 单 quad 那套 `(0,2,1)(0,3,2)` 同向（顺时针 = 正面）。
        /// ⚠️ 只在 `_nineOn` 为真时被 `RebuildMesh` 调。</summary>
        void RebuildNineSliceMesh()
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mf == null) { Debug.LogError("[ImageQuad] 九宫格重建失败：找不到 `MeshFilter`（⛔ 不静默）"); return; }

            float w = WorldW, h = WorldH;
            if (w <= 0f || h <= 0f || _tex == null) return;

            if (_fillAmount < 1f || _skewX != 0f || _skewY != 0f || _softKids.Count > 0
                || _uv.x != 0f || _uv.y != 0f || _uv.width != 1f || _uv.height != 1f)
            {
                if (!_nineWarnedMixed)
                {
                    _nineWarnedMixed = true;
                    Debug.LogWarning("[ImageQuad] 九宫格与 `fillAmount` / 斜切 / 软边子块 / `SetUvRect` **不同时支持** —— "
                                   + "这一块只按九宫格画，其余几项被忽略（⛔ 不静默）。见 `SetNineSlice` 头部注释。");
                }
            }

            float l = Mathf.Max(0f, _nineBorder.x), b = Mathf.Max(0f, _nineBorder.y);
            float r = Mathf.Max(0f, _nineBorder.z), t = Mathf.Max(0f, _nineBorder.w);
            // uGUI `GetAdjustedBorders`：**逐轴**判「两边之和 > 边长」才等比缩（⛔ 不是两轴共用一个比例）
            float scX = (l + r) > w ? w / Mathf.Max(1e-4f, l + r) : 1f;
            float scY = (b + t) > h ? h / Mathf.Max(1e-4f, b + t) : 1f;
            l *= scX; r *= scX; b *= scY; t *= scY;
            float wm = Mathf.Max(0f, w - l - r), hm = Mathf.Max(0f, h - b - t);

            float x0 = -anchor.x * w, y0 = -anchor.y * h;
            float[] xs = { x0, x0 + l, x0 + l + wm, x0 + w };
            float[] ys = { y0, y0 + b, y0 + b + hm, y0 + h };

            float uL = Mathf.Clamp01(_nineUvBorder.x), vB = Mathf.Clamp01(_nineUvBorder.y);
            float uR = 1f - Mathf.Clamp01(_nineUvBorder.z), vT = 1f - Mathf.Clamp01(_nineUvBorder.w);
            if (uR < uL) uR = uL;       // 边框比图还宽 ⇒ 中段取同一列纹素（与 `CreateNineSlice` / uGUI 同形）
            if (vT < vB) vT = vB;
            float[] us = { 0f, uL, uR, 1f };
            float[] vs = { 0f, vB, vT, 1f };

            var cols = CornerColorsWithTint(_cornerColors ?? WhiteVerts);
            var verts = new List<Vector3>(36);
            var uvs = new List<Vector2>(36);
            var colors = new List<Color>(36);
            var tris = new List<int>(54);

            for (int xi = 0; xi < 3; xi++)
            {
                for (int yi = 0; yi < 3; yi++)
                {
                    if (!_nineFillCenter && xi == 1 && yi == 1) continue;   // 原版 `m_FillCenter = 0` ⇒ 中间那块不画
                    float bx0 = xs[xi], bx1 = xs[xi + 1], by0 = ys[yi], by1 = ys[yi + 1];
                    if (bx1 - bx0 <= 0f || by1 - by0 <= 0f) continue;       // uGUI：零/负尺寸的格子直接跳过
                    int st = verts.Count;
                    NinePushVert(verts, uvs, colors, cols, x0, y0, w, h, bx0, by0, us[xi], vs[yi]);         // BL
                    NinePushVert(verts, uvs, colors, cols, x0, y0, w, h, bx0, by1, us[xi], vs[yi + 1]);     // TL
                    NinePushVert(verts, uvs, colors, cols, x0, y0, w, h, bx1, by1, us[xi + 1], vs[yi + 1]); // TR
                    NinePushVert(verts, uvs, colors, cols, x0, y0, w, h, bx1, by0, us[xi + 1], vs[yi]);     // BR
                    tris.Add(st); tris.Add(st + 1); tris.Add(st + 2);
                    tris.Add(st + 2); tris.Add(st + 3); tris.Add(st);
                }
            }

            var m = new Mesh { name = "ImageQuadNine" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetColors(colors);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            _mf.sharedMesh = m; SyncShineUv1();   // `B-326`：九宫格同样按**整块 sprite 矩形**写 `uv1`（同物理行追加，行数不变）
        }

        /// <summary>九宫格的一个顶点（`RebuildNineSliceMesh` 用）。顶点色按**整块 quad 的归一化位置**做四角双线性插值
        /// —— 只有一个格子时它与 `RebuildMesh` 的四角顶点色逐值相同。</summary>
        static void NinePushVert(List<Vector3> v, List<Vector2> uv, List<Color> c, Color[] cols,
                                 float x0, float y0, float w, float h, float x, float y, float u, float vv)
        {
            v.Add(new Vector3(x, y, 0f));
            uv.Add(new Vector2(u, vv));
            float nx = w > 1e-6f ? (x - x0) / w : 0f;
            float ny = h > 1e-6f ? (y - y0) / h : 0f;
            c.Add(NineVertColor(cols, nx, ny));
        }

        /// <summary>四角顶点色（顺序 BL·BR·TR·TL）按归一化位置 (nx, ny) 双线性插值。</summary>
        static Color NineVertColor(Color[] cols, float nx, float ny)
        {
            if (cols == null || cols.Length < 4) return Color.white;
            return Color.Lerp(Color.Lerp(cols[0], cols[1], nx), Color.Lerp(cols[3], cols[2], nx), ny);
        }

        // ============================================================ `B-326`：给 `WildcardShine` 的 quad 补 `uv1`（**追加在类尾**）
        //
        // **为什么**：`Resources/Shaders/WildcardShine.shader` 拿 `TEXCOORD1`（= `Mesh.uv2`，即 uv **通道 1**）当
        //   「sprite 的 uv 矩形」做四边盒测，且 `uv1.x` 同时是 Shadow 支的总闸 `_287`（判据 = `b16.hlsl:122-128`：
        //   `uv1 = (xMin − 2·影 − 4·镜, yMin, xMax, yMax)`，剥掉偏移后 `uv1.x < 0` ⇒ 影子）。
        //   原版那份数据由 `UIEffectBase.ModifyMesh` → `GraphicProxy.s_OnModifyVertex` 写入
        //   （`vt.uv1 = GetBounds(这批顶点的 uv)`，判据 → `报告/B116_W11_WildcardShine剩余三块.md` §一），
        //   **只有挂 `UIEffect` 的图形才有**；我们等价的挂载者 = 被 `WildcardShineLayer` 换上那颗材质的 quad。
        //   ⇒ 本类只给**材质是 `CardPresentation/WildcardShine`** 的 quad 写这个通道；其余 quad 的网格一个字节不变
        //     （`UsesShineShader` 为假时 `SyncShineUv1` 第一句就 return —— 连 `mesh.uv` 都不读）。
        //
        // **写什么**：整批顶点的 uv **包围矩形**，四个分量 = `(xMin, yMin, xMax, yMax)`，**每个顶点写同一组值**
        //   —— 与 `GraphicProxy` 的 `GetBounds` 同语义（⛔ 不是「每个顶点写自己的 uv」）。
        //   · 单 quad ⇒ 就是它那一块 uv 矩形（`SetUvRect` / `SetFillAmount` 之后的那一段，与真实网格同源）；
        //   · 九宫格**单网格**（`ImageQuadNine`）⇒ 包围矩形 = 整块 sprite 矩形，⛔ 不是每段自己的；
        //   · 九宫格走 `CreateNineSlice` 的**9 个子 quad** 时，每个子 quad 在我们的模型里各自是一张图形
        //     （各自带自己的 uv 子矩形）⇒ 各自写自己那一段，与「`GetBounds(该图形这批顶点)`」同一条规则。
        //
        // **为什么不影响现有画面**（这是「变了也没关系」的证明，不是「没变」的证明）：盒测（`b16.hlsl:144-163`）
        //   只在**网格被外扩**（描边/阴影/边缘）时才裁得掉东西；本链**从不外扩** ⇒ 所有顶点都落在自己的 uv 矩形内
        //   ⇒ 四条盒测恒 1。`uv1.x ≥ 0`（归一化 uv 的 xMin 不会是负数）⇒ `_287` 恒 0 ⇒ **影子支仍不进**。
        //   这与 `B116_W11` 那条「无数据兜底 = 单位矩形」的结果**逐项相同**，只是数据来源从兜底换成了真值。
        //
        // 🔶 自己设计（非原版）：**「按 shader 名判定要不要写 uv1」这一跳** —— 原版的结构事实是「这个图形挂没挂
        //   `UIEffect`」，我们这条链上没有那个组件；等价物 = 「渲染器上是不是 `WildcardShine` 这颗材质」
        //   （全工程唯一会读 `uv1` 的 shader，`grep "uv1" Resources/Shaders/` 可复核）。
        //   为什么必须自定：`ImageQuad` 是共用件（几百个实例），没有「谁会读 uv1」的注册口可用；
        //   按 shader 名判定是唯一不必改所有调用点的办法。· 调度台 2026-11-02 裁定（`B116_W17`）
        const string ShineShaderName = "CardPresentation/WildcardShine";

        /// <summary>这块 quad 现在的材质是不是 `WildcardShine`（= 唯一会读 `uv1` 的 shader）。
        /// ⚠️ 读的是**渲染器上真正生效**的材质（`sharedMaterial`），与 `SetMaterial` 同口径。</summary>
        public bool UsesShineShader
        {
            get
            {
                if (_mr == null || _mr.sharedMaterial == null) return false;
                var sh = _mr.sharedMaterial.shader;
                return sh != null && sh.name == ShineShaderName;
            }
        }

        /// <summary>网格现在有几个 `uv1`（`TEXCOORD1`）顶点。**自检读口**：`0` = 这条通道没接上
        /// （`WildcardShine` 就会走「无数据兜底 = 单位矩形」）；接上后应等于 `Mesh.vertexCount`
        /// （单 quad = 4 / 九宫格单网格 = 4×格数 / `fillAmount < 0.001` 时 = 0，那本来就一个顶点都没有）。</summary>
        public int Uv1VertexCount
        {
            get
            {
                if (_mf == null || _mf.sharedMesh == null) return 0;
                var mesh = _mf.sharedMesh;
                return mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1) ? mesh.vertexCount : 0;
            }
        }

        /// <summary>把当前网格的 `uv1` 刷成「这批顶点的 uv 包围矩形」（判据与影响面 → 上面那一段）。
        /// **只对 `WildcardShine` 的 quad 生效** —— 其余 quad 在这里第一句就返回，网格一个字节不变。</summary>
        void SyncShineUv1()
        {
            if (!UsesShineShader) return;
            if (_mf == null || _mf.sharedMesh == null) return;
            var mesh = _mf.sharedMesh;
            int n = mesh.vertexCount;
            if (n <= 0) return;                        // `fillAmount < 0.001` 那一档：网格本来就没有顶点
            var uv = mesh.uv;
            if (uv == null || uv.Length != n) return;  // 没有 uv0 ⇒ 无判据；不写（`uv1` 保持「无数据」，shader 走兜底）
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                var p = uv[i];
                if (p.x < xMin) xMin = p.x;
                if (p.y < yMin) yMin = p.y;
                if (p.x > xMax) xMax = p.x;
                if (p.y > yMax) yMax = p.y;
            }
            if (!(xMax > xMin && yMax > yMin)) return;  // 退化矩形 ⇒ 留「无数据」（⛔ 别写成一个 mask 恒 0 的框，那会把整块抹黑）
            var v4 = new List<Vector4>(n);
            var one = new Vector4(xMin, yMin, xMax, yMax);
            for (int i = 0; i < n; i++) v4.Add(one);
            mesh.SetUVs(1, v4);
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-11-02（`B118-W14` · 判据 `判据/B118_J6_卡牌表示层矩阵.md` §3）
        //     **`SetTexture(null)` 不许静默变白** —— 这是运行期换图那口唯一的兜底
        // ══════════════════════════════════════════════════════════════════════════════════════
        //  规则（两条，按「有没有上一张图」分）：
        //   · **有上一张**（`prev != null`）⇒ **留着上一张**。壳侧的「清空」调用点
        //     （`Shell/EventBadge.SetNotificationData` · `Shell/DraftModeWindow`）都是
        //     `SetTexture(null)` **紧跟一句 `SetActive(false)`** ⇒ 保留旧图既不画白、也不会有残影；
        //     而 `_tex` 仍写 null（`Texture` 读口如实反映「已清空」）。
        //   · **一张都没有**（`prev == null`）⇒ 出声一次 + 一块紫灰占位图（`MissingQuadTexture`）。
        //  🔶 自己设计（非原版）：缺图时的占位色 · 为什么必须自定：原版是 uGUI `Image.sprite = null`
        //     （**不画任何像素**，不会变白）；我们世界空间 quad 绑 null 时 Unity 给的是**内置白图**
        //     ⇒ 语义不同，必须自己补一档。调度台 2026-11-02 裁定。
        // ══════════════════════════════════════════════════════════════════════════════════════

        static readonly Dictionary<string, Texture2D> _missingQuadTex = new Dictionary<string, Texture2D>();
        static readonly HashSet<string> _missingQuadWarned = new HashSet<string>();

        /// <summary>自检用：清掉「缺件记账 + 占位图缓存」⇒ 让「第一次出声」可复现。⚠️ 只给 Editor 自检。</summary>
        public static void ResetMissingDiagnosticsForTest()
        {
            _missingQuadWarned.Clear(); _missingQuadTex.Clear();
        }

        /// <summary>「这张 quad 一张图都没有」时的占位图（按 quad 名缓存 + 出声一次）。</summary>
        public static Texture2D MissingQuadTexture(string quadName)
        {
            string key = string.IsNullOrEmpty(quadName) ? "<unnamed>" : quadName;
            Texture2D t;
            if (_missingQuadTex.TryGetValue(key, out t) && t != null) return t;
            const int n = 4;
            t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.name = "ImageQuadMISSING_" + key;
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0.55f, 0.15f, 0.45f, 1f);   // 紫灰：明显不是白
            t.SetPixels(px); t.Apply(false, false);
            _missingQuadTex[key] = t;
            if (_missingQuadWarned.Add(key))
                Debug.LogWarning($"[ImageQuad] `{key}` 被 `SetTexture(null)` 且**没有上一张图**可留 ⇒ 用占位色顶替"
                               + "（Unity 对 null 贴图会绑内置白图 = 一块**不透明白块**；判据 → "
                               + "`判据/B118_J6_卡牌表示层矩阵.md` §3）");
            return t;
        }

        // ============================================================ ✅ 已复刻（2026-11-04 · B142-E17 · `B141-BINDNINE-BORDER`）
        //
        //  判据（第一权威 = 本机 uGUI 包源码 `Library/PackageCache/com.unity.ugui@*/Runtime/UGUI/UI/Core/Image.cs`）：
        //   · `:279-295` `sprite` setter：`m_Sprite != value` ⇒ `SetAllDirty()`（换 sprite = **重建整张网格**）；
        //   · `:1130 / :1157` `GenerateSlicedSprite`：`adjustedBorders = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect)`
        //     —— 那个 `border` 是**新 sprite 的 `Sprite.border`**（逐次现读）⇒ 换图后必须按**新图**重切；
        //   · `:756` `multipliedPixelsPerUnit => pixelsPerUnit * m_PixelsPerUnitMultiplier`
        //     ⇒ 画出来的边 = `m_Border ÷ ppuMul`（**乘数在分母**）；
        //   · `:1479-1500` `GetAdjustedBorders`：某轴「两条角块之和 > 矩形边长」时**逐轴**按比例缩。
        //
        //  我们这颗九宫格是「根 + 9 张 quad」，建件那一刻按**常态图**的 border 切好；换到 border 不同的那一档
        //  （实测三族：`40K_button_square` 36/28/37/29 ↔ `_hover` 32/27/34/28 ↔ `_pressed` 28/27/27/25 ·
        //   `40K_tab_button` 183/0/128/29 ↔ `…_overwindow` 188/0/99/30）时**必须重切**，
        //  否则 uv 分界与角块宽一起错（静默：画面只是「差一点」，没有任何报错）。

        /// <summary>九宫格切分算式 —— `CreateNineSlice`（建件那一刻）与 <see cref="ResliceNineSlice"/>（换 sprite 那一刻）
        /// **共用这一份**（⛔ 别在两处各写一遍：CLAUDE.md §三「两处写同一条规则 = 迟早不一致」）。
        /// <para>`borderPx` = **贴图 px** 的边宽（uGUI `Sprite.border`，决定 uv 分界）；`borderOutPx` = **画出来**的
        /// 角块 px（= `m_Border ÷ m_PixelsPerUnitMultiplier`，见 `Image.cs:756/1157`）。
        /// 输出 = 三段坐标（世界单位，以根为原点）+ 三段 uv；`GetAdjustedBorders` 的逐轴夹也在本方法里做
        /// ⇒ 两个入口拿到的是**同一份夹过的值**。</para></summary>
        static void NineSliceLayout(string name, float texW, float texH, Vector4 borderPx, Vector4 borderOutPx,
                                    float worldW, float worldH,
                                    out float[] xs, out float[] ys, out float[] us, out float[] vs)
        {
            float l = borderPx.x, b = borderPx.y, r = borderPx.z, t = borderPx.w;
            // 归一化 uv 分界
            float uL = l / texW, uR = 1f - r / texW, vB = b / texH, vT = 1f - t / texH;
            // 🔴 **2026-10-04 修（真 bug：误报 + 中段被吃掉）**。
            //   旧写法 `if (uR <= uL || vT <= vB)` 把「**端帽正好铺满整张图**」这种**合法**形状也当成了越界：
            //   判据 = 原版 uGUI `Image.GetAdjustedBorders`（关键那一句就是 `adjustedRect.size[axis] < combinedBorders`）——
            //   它只在 **`border.x + border.z > rect.width`**（两边边宽之和 **>** 矩形宽）时才按比例缩，
            //   **`uR == uL`（= 边宽之和 == 贴图宽）是合法形状**，中段宽就是 0（`GenerateSlicedSprite`
            //   对「宽 ≤ 0 的那一格」是 `continue` 跳过，见 `Image.GenerateSlicedSprite`）。
            //   实测受害例：`WF_Special offer_Value` 324×87 · `m_Border = (162,0,162,0)`（L+R **正好** = 324）
            //   ⇒ `uL == uR == 0.5` ⇒ 旧代码每次都打一条「border 比图还大，退回单块」的**假警告**
            //   （`BoosterPackOpenWindow` 每个卡位一枚 `New Card Badge`、`OfferContainer` 19 个变体各一枚 —— 实测 21 次），而它根本没有退回单块。
            //   ⇒ 只有**真正的越界**（`uR < uL` / `vT < vB`）才出声。
            //   另：真的越界时把分界**夹成不反向**（下面一行）——旧代码会拿着一个反过来的 uv 去切图
            //   （画出来是垃圾且**静默**）；夹完之后退化成「中段取同一列纹素」，与原版同形。
            if (uR < uL || vT < vB)
                Debug.LogWarning($"[ImageQuad] {name}: border 比图还大（{l}+{r} > {texW} 或 {b}+{t} > {texH}）—— "
                               + "`m_Border` 与贴图尺寸不自洽，中段退回同一列纹素");
            uR = Mathf.Max(uR, uL); vT = Mathf.Max(vT, vB);

            // 目标里三段的长（角块**不缩放**，按 108 px = 1 世界单位）
            float ol = borderOutPx.x, ob = borderOutPx.y, orr = borderOutPx.z, ot = borderOutPx.w;
            float wl = ol / PixelsPerUnit, wr = orr / PixelsPerUnit;
            float hb = ob / PixelsPerUnit, ht = ot / PixelsPerUnit;
            // ⚠️ **目标比「两边角加起来」还小时，按比例把角缩下来** —— 原版 uGUI 也是这么退化的
            //    （`GetAdjustedBorders`：把角块压扁），不这么做的话角块会**互相重叠**画出去。
            // 🔴 **2026-10-04：按【轴】缩，不是两轴共用一个比例**（判据同上 `GetAdjustedBorders` ——
            //    它是 `for (axis = 0; axis <= 1; axis++)` **逐轴**判 `rect.size[axis] < border[axis]+border[axis+2]`）。
            //    旧写法 `sc = min(1, W/(wl+wr), H/(hb+ht))` 两轴共用：**一个轴挤了，另一个轴的角块也跟着缩**。
            //    实测受害例：提示条用 `40k_popup`（边 169/160）塞进 1323×90 —— 只有**竖**着挤，
            //    原版横边的两个端帽仍是 **169px**（中段 985），旧写法把它们缩成 **47.5px**（中段 1228）。
            float scX = (wl + wr) > worldW ? worldW / Mathf.Max(1e-4f, wl + wr) : 1f;
            float scY = (hb + ht) > worldH ? worldH / Mathf.Max(1e-4f, hb + ht) : 1f;
            wl *= scX; wr *= scX; hb *= scY; ht *= scY;
            float wm = worldW - wl - wr, hm = worldH - hb - ht;
            if (wm < 0f) { wm = 0f; }
            if (hm < 0f) { hm = 0f; }

            float x0 = -worldW * 0.5f, y0 = -worldH * 0.5f;
            xs = new[] { x0, x0 + wl, x0 + wl + wm, x0 + worldW };
            ys = new[] { y0, y0 + hb, y0 + hb + hm, y0 + worldH };
            us = new[] { 0f, uL, uR, 1f };
            vs = new[] { 0f, vB, vT, 1f };
        }

        /// <summary>✅ 已复刻（2026-11-04 · B142-E17 · 判据见上一段类注释，`Image.cs:279-295 / :1157 / :756 / :1479-1500`）：
        /// 按**新 sprite 的 border** 把一棵**已经建好**的九宫树**原地**重切（= uGUI `SetAllDirty → GenerateSlicedSprite` 的等价动作）。
        /// <para>块名沿用 `CreateNineSlice` 的 `<root.name>_<i><j>`；
        /// 新算法用不到的旧块 **`SetActive(false)`、⛔ 不删节点**（与 `MenuDraw.ClipNineChildren` 同一条纪律，
        /// 自检按名字仍找得到、`PointerLayer` 自动跳过未激活件）；新出现的块用 `Create` 补建，
        /// 并拷第一颗兄弟的 `RenderQueue` / `Tint`（`MenuDraw.Nine` 给 9 块上的是同一个值）。</para>
        /// <para>🔴 **选「原地改」而不是「销毁重建」**：子块身份不变 ⇒ 调用方（`WindowButton.target`）的引用、
        /// 染色表 `_tq/_tqBase`、灰材质 `_graySaved/_grayMats/_graySavedQ`、`RenderQueue` **全部不用动**；
        /// 销毁重建会让 `RefreshGray` 的按下标缓存错位、`target` 变成已销毁对象（且**静默**）。</para>
        /// <para>⚠️ 只改**几何与 uv**（贴图由调用方在同一跳换）；`worldW/H` 是**换之前**那棵树的外框
        /// （九宫格的框不随 sprite 变，uGUI 也是按 `rect` 夹 border）。</para>
        /// <para>⚠️ **本方法会补建缺失的块**（新 border 让某格从 0 变成正）：新块是拿本次 `tex` 建的、队列/基色拷兄弟，
        /// 但**不在 `WindowButton._nine` 的建件快照里** ⇒ 「建件时缺中心块、换 border 后又要它」的站点本方案不覆盖
        /// （今天两个生产站点 9 块齐全；`fillCenter:false` 的站点永远不补中心块）。</para>
        /// <returns>true = 真的重切了；false = 参数不成立（根/贴图为空、贴图或框尺寸为 0）⇒ **没有动任何东西**。</returns></summary>
        public static bool ResliceNineSlice(GameObject root, Texture tex, Vector4 borderPx, float ppuMul,
                                            float worldW, float worldH, bool fillCenter)
        {
            if (root == null || tex == null || tex.width <= 0 || tex.height <= 0) return false;
            if (worldW <= 0f || worldH <= 0f) return false;
            if (ppuMul <= 0f) ppuMul = 1f;

            float[] xs, ys, us, vs;
            NineSliceLayout(root.name, tex.width, tex.height, borderPx, borderPx / ppuMul,
                            worldW, worldH, out xs, out ys, out us, out vs);

            // 模板（队列 / 基色）取第一颗**直属**子块 —— `MenuDraw.Nine` 对 9 块上的是同一个值。
            var kids = root.GetComponentsInChildren<ImageQuad>(true);
            int tplQ = 0; Color tplTint = Color.white; bool hasTpl = false;
            for (int i = 0; i < kids.Length; i++)
            {
                var k = kids[i];
                if (k == null || k.transform.parent != root.transform) continue;
                tplQ = k.RenderQueue; tplTint = k.Tint; hasTpl = true; break;
            }

            var used = new HashSet<Transform>();
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    if (!fillCenter && i == 1 && j == 1) continue;      // 原版 `m_FillCenter = 0` ⇒ 中间那块不画
                    float w = xs[i + 1] - xs[i], h = ys[j + 1] - ys[j];
                    if (w <= 0f || h <= 0f) continue;                   // 与原版 `GenerateSlicedSprite` 同形：宽/高 ≤ 0 的格子跳过
                    string cn = root.name + "_" + i + j;
                    Vector3 pos = new Vector3((xs[i] + xs[i + 1]) * 0.5f, (ys[j] + ys[j + 1]) * 0.5f, 0f);
                    var tr = root.transform.Find(cn);
                    var cell = tr != null ? tr.GetComponent<ImageQuad>() : null;
                    if (cell == null)
                    {
                        cell = Create(root.transform, tex, pos, h, new Vector2(0.5f, 0.5f), cn);
                        if (cell == null) continue;
                        if (hasTpl) { cell.SetRenderQueue(tplQ); cell.SetTint(tplTint); }
                    }
                    else
                    {
                        pos.z = cell.transform.localPosition.z;         // z 不动（同队列里还靠它排序）
                        cell.transform.localPosition = pos;
                        if (!cell.gameObject.activeSelf) cell.gameObject.SetActive(true);
                        cell.SetWorldHeight(h);
                    }
                    cell.SetAspect(w / h);
                    cell.SetUvRect(new Rect(us[i], vs[j], us[i + 1] - us[i], vs[j + 1] - vs[j]));
                    used.Add(cell.transform);
                }

            // 新算法用不到的旧块 ⇒ 关掉、⛔ 不删（裁切那一族也要能把它们再打开）
            for (int i = 0; i < kids.Length; i++)
            {
                var k = kids[i];
                if (k == null || k.transform.parent != root.transform) continue;
                if (!used.Contains(k.transform) && k.gameObject.activeSelf) k.gameObject.SetActive(false);
            }
            return true;
        }
    }
}
