// BoardLayout.cs — 战场的 9 个格位（督军居中 + 两侧各 4）
//
// 和手牌一样用归一化坐标，换分辨率/宽高比都成立。
// 格位间距/缩放搬自原版实测（下面的常量），**敌方那个实例要镜像**（原版就是镜像的）。
//
// ⚠️ 格数 **9** 是硬决定，**三处来源指向同一个数**（⚠️ 2026-10-18 更正：原来写「**权威来源**
//    三处一致」—— 那个「权威」用错了；这三处里**只有第二处是判据**）：
//    · ⚠️ 规则书 :30 ——「玩家棋盘 9 个空格，督军永远占据中央格，两侧各 4 格共可部署 8 张部队卡」
//      —— **粉丝实体版规则书，只算旁证**（非官方）
//    · ✅ **解包资源字段**（判据②）—— battlearena1 的 `MinionArea` `slotsPerSide=4`
//    · ⚠️ `rule_core.gd:44` —— BOARD_SIZE 9 / WARLORD_SLOT 4
//      —— **我们自己的上一版 Godot 复刻，只算旁证**（不是原版语义判据）
//    之前这里是 7 格，那是被推翻过的误读（推翻它的判据 = 上面那条 `slotsPerSide=4`；
//    `rule_core.gd:42` 的修正记录是我们的旁证）。
//    规则引擎那边对应 `RuleEngine.BoardSpec`，**两边必须一致**。
//    🔴 判据顺序 = ① 原版全量反编译 `D:/2/tools/decomp_full/` → ② 解包资源字段 → ③ 成品卡图卡面文字。
//
// ---- 间距/缩放是量出来的，不是拍的 ----
//   出处：`d:/warpforge/scripts/battle.gd` 的场卡尺寸体系（8-27 审查定案，对着原版
//   冻结帧量的），原版 1920×1080 下：
//     · 场卡      137.2 × 218.4 px   （2DCard 2.0927 宽 × desiredScale 0.36 × k 182.14）
//     · 相邻中心距 149.3 px          （MinionSeparation 0.82 × 182.14）
//     · 玩家行中心 y = 708，敌方行中心 y = 466
//   换到我这套（可见高 10 单位 = 1080 px，可见宽 17.78）：
//     placedScale **0.607** → 卡宽 1.271 世界 = 137.2 px；spacing 0.0778 → 1.382 世界 = 149.3 px
//   🔴 **2026-09-17 更正**：`placedScale` 的默认值原来是 **0.876**，与它同一行的这段注释**直接打架**
//      （注释写「0.876 → 卡宽 1.271 世界」，可 2.0927 × 0.876 = **1.833** 世界 = 197.9 px）。
//      **产品侧没受影响**：`BattleScene` 一直显式赋 `BoardScale = 137.2/(2.0927×108) = 0.607`，
//      所以 Battle 场景里一直是对的。**中招的是「取默认值」的那条路** —— `CardBaseDemo` 自建的那块板
//      没赋过这个字段 ⇒ 它的场卡**大了 44%**，而它正是「四档分辨率」的版面验收图。
//      一直没人发现，因为**它当时一个断言都没有**（2026-09-17 补断言时当场抓到）。
//      ⇒ 现在默认值改成 `DefaultPlacedScale`（= 0.607），且**与 `BattleScene` 共用这一个判据**。
//   ⚠️ 原版敌方那一行是**投影收窄**的（131.9 px 步进、卡也更小），**2D 里**不适用 ——
//      这边同尺寸、只镜像**顺序**。
//   🔴 **2026-09-20：真 3D 那条路不吃这一套了** —— 场上的卡搬进透视层之后，落点/缩放
//      逐值照原版（**玩家敌各一份**：步距 0.82/1.53、`desiredScale` 0.36/0.69），
//      判据在 `Board/ArenaSlots.cs`。本文件这份（屏幕空间）**只剩命中判定与底片**在用。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public class BoardLayout : MonoBehaviour
    {
        public const int SlotCount = 9;

        /// <summary>原版场卡宽度（px @1920×1080）—— 出处见文件头（`2.0927 × 0.36 × 182.14`）。
        /// 🔴 **判据只此一处**：落位缩放与 `spacing` 都由它推出来，`BattleScene` 也引用这里，别再各写一份。</summary>
        public const float OriginalCardWidthPx = 137.2f;
        /// <summary>原版相邻格中心距（px @1920×1080）= `MinionSeparation 0.82 × 182.14`</summary>
        public const float OriginalSlotPitchPx = 149.3f;

        /// <summary>落位后的缩放 = 原版卡宽 ÷ (`2DCard` 宽 2.0927 × 我们**每单位像素数** 108)。
        /// = `137.2 / (2.0927 × 108)` = **0.607**（1080p 下卡宽 1.271 世界 = 137.2 px）。</summary>
        public const float DefaultPlacedScale = OriginalCardWidthPx / (CardView.Width * 108f);

        /// <summary>督军槽 —— 正中间那格（原版也是中间）</summary>
        public const int WarlordSlot = 4;

        [Tooltip("格位基准线的 y（归一化）—— 默认值 = 对战里玩家那行（BattleScene 会自己设）")]
        public float lineY = 0.3444f;

        [Tooltip("相邻格位的间距（归一化宽度）—— 原版实测 149.3 px / 1920")]
        public float spacing = 0.0778f;

        [Tooltip("镜像：敌方那份要把槽号左右翻过来（原版敌方 leftSlotPosNormal 就是 +x）")]
        public bool mirror = false;

        [Tooltip("落位吸附的容差（归一化**宽度**）—— 松手点离格位多近算落上。"
               + "必须 < 半间距（0.0778/2），否则相邻两格的判定区会互相咬")]
        public float snapTolerance = 0.036f;

        [Tooltip("纵向容差（归一化**高度**）—— 和上面那个不是同一个物理尺度（可见宽 17.78 / 高 10），"
               + "所以单独给。要小于「相邻两行之间的距离」和「手牌最上沿到本行」两处，"
               + "否则会把牌吸到别的行上")]
        public float snapToleranceY = 0.09f;

        [Tooltip("落上去的卡缩放 —— 原版实测场卡 137.2 px / 卡设计宽 1.45×108")]
        public float placedScale = DefaultPlacedScale;

        public bool IsWarlord(int slot) { return slot == WarlordSlot; }

        // ---- 格位底片（给玩家看「能往哪儿放」）----
        // 底片由这里创建而不是场景里摆 —— 它得跟着 LayoutSpace 的分辨率缩放走，
        // 而且拖拽时要整体变色，放在一处最省事。
        readonly List<Renderer> _markers = new List<Renderer>();
        Transform _band;

        // 有战场背景图之后，底片要**很轻**：原版平时不画格子，只在拖拽时亮起来提示落点。
        // 所以 idle 只是一层「淡阴影」（让两行棋盘看得出是两块区域），拖拽时才明显。
        //
        // 🔴 **2026-09-19：战场变成真 3D 之后，这两层平时**一律不画**（alpha 0）。**
        //    原版的格位分界来自 **3D 地板本身**，静帧上根本没有我们这套「槽带 + 九宫底片」——
        //    `资料/战场还原度_差距清单_0917.md` §一 把它列为「我们挑的」第 2 条（🔴 整屏观感来源之一）。
        //    现在地板是真的了 ⇒ 我们这套叠上去的压暗层就是**多余的自设计**，去掉。
        //    **拖拽高亮保留**（原版也有落点提示，见 `Minion Position Highlight` 那三件套）。
        static readonly Color MarkerIdle = new Color(0f, 0f, 0f, 0f);                // 平时不画
        /// <summary>**落点指示**那一格的颜色 —— 照原版那枚 `Glow_674` 的 `m_Color`
        /// = **(0.40732, 0.78302, 0.14405)**（`SpriteRenderer_1752`，`bundle_scenes_scenes_battlearena1`）。
        /// ⚠️ **α = 0.45 是我们挑的**：原版那枚 `SpriteRenderer` 的 `m_Sprite` **13 个场景全是 null**
        /// （`SetCardShadow` 方法体里也没有一次 `set_sprite`）⇒ 它自己画不出东西、**没有可照抄的透明度**。
        /// 出处与结论 → `资料/待办判据_战场与战斗视图.md` §8b。</summary>
        static readonly Color DropSlotColor = new Color(0.40732f, 0.78302f, 0.14405f, 0.45f);
        /// <summary>落点指示相对**卡**的尺寸比 —— 做成**跟卡一样大**（原来的 1.05 是高了一点点，照抄）。
        /// 🔴 2026-10-01 走过的弯路（别重犯）：先按原版那枚 `Shadow` 的 **3.2×3.2**（比卡宽 1.53×）放大、
        /// 又把落点从卡心挪到「脚底」，**都还是看不见** —— 肉眼看图 + 像素探针确认：
        /// 那枚方块被**拖拽中的卡**整块压住（卡周围 380×340 里只剩 **706 个绿像素**）。
        /// ⇒ 真正的解法是**渲染队列**：见 <see cref="MarkerQ"/>。</summary>
        public const float DropMarkW = 1f, DropMarkH = 1.05f;

        /// <summary>落点指示的**渲染队列** —— 🔴 **必须大于卡的 3000**（原版材质的 `m_CustomRenderQueue = 3000`，
        /// 见 `CardView.cs` 里那几处 `renderQueue = 3000`）。透明队列里同队列是**按到相机的距离**排的，而拖拽中的卡被抬高了
        /// (`dragLiftZ`) ⇒ 它离相机更近、永远后画 ⇒ 同队列的指示**一定被它盖住**。
        /// 取 **3100**：在卡之上、在战斗日志面板（4000）与各式窗口（3600+）之下。
        /// ⚠️ 这条是**我们挑的**（原版那枚影子是画在卡**下面**的，但它的图在这个 build 里是空的、
        /// 没有可照抄的观感）—— 判据与两条弯路 → `资料/已知的坑.md` 那条「断言验得到亮没亮、
        /// 验不到看不看得见」。</summary>
        public const int MarkerQ = 3100;
        static readonly Color BandColor  = new Color(0f, 0f, 0f, 0f);                // 槽带：同样不画

        public void EnsureMarkers()
        {
            if (_markers.Count == 0) BuildMarkers();
            // 🔴 **2026-10-01：这两层留在默认层（= HUD 相机画）**，**别挂 `ArenaSlots.ArenaLayer`**。
            //   试过挂那一层（以为「在战场空间里就该归战场相机」）—— 结果它被**战场相机**投影到
            //   **左边 600 px 外**（实测：根活动、层对、cullingMask 也含它，就是位置错），
            //   因为它的位置是**按 HUD 平面的映射**算的（见 `PlaceMarkers` 里那段）。
            //   A/B 实拍（`2d_落点指示_单独亮.png`）看得很清楚：它出现在 hover 弹窗右边那条。
            PlaceMarkers();
        }

        void BuildMarkers()
        {
            // 🔴 **2026-10-11（A218）判「不改」**（这一层**故意**保持**裸 `Transform`**，⛔ 别补 `RectTransform`）：
            //    判据 = 原版棋盘那一族挂点**全是裸 `Transform`** —— `bundle_scenes_scenes_battlearena1` 实读：
            //    `Board Center`（go_pid 289）· `MinionArea`(339/344) · `LeftMinionArea` · `RightMinionArea` ·
            //    `HandArea`(318/898) —— 一个 `RectTransform` 都没有（它们活在世界空间里，靠 `localPos`/`scale` 定位）。
            //    本节点下面挂的是 `GameObject.CreatePrimitive(Quad)` 这类**世界空间的 3D 片子**（见下一段）
            //    ⇒ 没有矩形语义，写 `sizeDelta` 只会造一个**没有判据的数**（铁律 3）。
            var root = new GameObject("Slots");
            root.transform.SetParent(transform, false);
            var baseMat = new Material(Shader.Find("Sprites/Default"));

            // 「槽带」：整行 9 格背后一条深色底 —— 没有它的话两行卡会漂在背景上，
            // 看不出「这两行是棋盘」。原版是 3D 地板自带分界，2D 得自己画一条。
            var band = GameObject.CreatePrimitive(PrimitiveType.Quad);
            band.name = "Band";
            band.transform.SetParent(root.transform, false);
            var br = band.GetComponent<MeshRenderer>();
            br.sharedMaterial = new Material(baseMat) { color = BandColor };
            UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());
            _band = band.transform;

            for (int i = 0; i < SlotCount; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = $"Slot_{i}{(i == WarlordSlot ? "_warlord" : "")}";
                q.transform.SetParent(root.transform, false);
                var r = q.GetComponent<MeshRenderer>();
                // ⚠️ 每格一份材质：共用的话 `MarkOccupied(i)` 会把 9 格一起变色（踩过）
                // 🔴 **必须给一张白图**：`Sprites/Default` 是**采样 `_MainTex` × 顶点色 × `_Color`** 的，
                //   `_MainTex` 为空时采出来是**黑的** ⇒ 压在暗场上**什么也看不见**（而 `isVisible` 照样是 `True`、
                //   位置/层/队列/深度**全对**）。2026-10-01 打了七轮就是栽在这 —— 详见 `资料/已知的坑.md`。
                r.sharedMaterial = new Material(baseMat) { color = MarkerIdle };
                r.sharedMaterial.mainTexture = Texture2D.whiteTexture;
                // 🔴 **每格一份材质 ⇒ 改队列是安全的**（同 `DeckRuntime` 那条分层注释）。
                //    不给这一档的话，亮起来的那一格会被**拖拽中的卡**整块盖住（2026-10-01 实测）。
                r.sharedMaterial.renderQueue = MarkerQ;
                UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
                _markers.Add(r);
            }
        }

        /// <summary>
        /// 按**当前**可见宽度重算底片和槽带的位置/尺寸。
        /// ⚠️ 必须能重复调用：`LayoutSpace.VisibleWidth` 跟着宽高比走，
        ///    切分辨率后不重算的话底片还停在上一档的位置上（批处理自检里就是 4:3 建、16:9 拍）。
        /// </summary>
        void PlaceMarkers()
        {
            float s = placedScale * LayoutSpace.Scale;
            for (int i = 0; i < _markers.Count; i++)
            {
                var r = _markers[i];
                if (r == null) continue;
                // 🔴 **2026-10-01：3D 时位置要按「战场相机投屏 → HUD 平面」算，不能用 `DropTargetWorld`。**
                //   `DropTargetWorld` 走的是 `LayoutSpace.ScreenToWorld`（**相机反投影**），而这两层是
                //   **HUD 相机**画的（默认层）⇒ 拿战场相机的反投影去摆 HUD 上的东西，实测**偏左 600 px**
                //   （A/B 实拍 `2d_落点指示_单独亮.png`：它出现在 hover 弹窗右边那条，而不是落点上）。
                //   正确做法 = **`LayoutSpace.FromPixel`**（`ToPixel` 的逆，HUD 平面那把尺子）：
                //   先把格子的卡心用**战场相机**投到屏幕，再按 HUD 的映射换算回 HUD 平面。
                //   ⚠️ **不能改 `DropTargetWorld` 本身** —— 它是拖拽的落点判据（自检夹具也用）。
                Vector3 markerPos = DropTargetWorld(i);
                if (use3D && boardCam != null)
                {
                    var msp = boardCam.WorldToScreenPoint(
                        ArenaSlots.CardCenter(i, mirror, ArenaSlots.CardScale(mirror)));
                    if (msp.z > 0f)
                        markerPos = transform.InverseTransformPoint(LayoutSpace.FromPixel(msp.x, msp.y));
                }
                r.transform.localPosition = markerPos + new Vector3(0f, 0f, 0.05f);
                // 🔴 **2026-09-20：真 3D 时底片的位置与尺寸都要按「投影」来。**
                //    底片平时不可见（alpha 0），但**拖拽时是亮的**（alpha 0.45）——
                //    卡搬进 3D 之后不跟着走的话，玩家看到的高亮格会停在**上面 150 px 的老位置**，
                //    于是「亮着的那格」和「牌真的会落到的格」指的不是同一处。
                //    尺寸也不能用 `placedScale`：那是在**正交平面**上量的尺子，
                //    透视相机下场卡的屏幕尺寸随行不同（我方 137.2 px / 敌 124.5 px）。
                if (use3D && boardCam != null)
                {
                    float sc = ArenaSlots.CardScale(mirror);
                    var c = ArenaSlots.CardCenter(i, mirror, sc);
                    float halfW = CardView.CardUnitW * sc * 0.5f;
                    float pxW = Mathf.Abs(boardCam.WorldToScreenPoint(c + Vector3.right * halfW).x
                                        - boardCam.WorldToScreenPoint(c - Vector3.right * halfW).x);
                    float pxH = Mathf.Abs(boardCam.WorldToScreenPoint(c + Vector3.up * halfW).y
                                        - boardCam.WorldToScreenPoint(c - Vector3.up * halfW).y);
                    float pxPerUnit = boardCam.pixelWidth / Mathf.Max(0.001f, LayoutSpace.VisibleWidth);
                    r.transform.localScale = new Vector3(pxW * DropMarkW / pxPerUnit,
                                                         pxH * DropMarkH / pxPerUnit, 1f);
                }
                else
                {
                    r.transform.localScale = new Vector3(CardView.Width * DropMarkW * s,
                                                         CardView.Height * DropMarkH * s, 1f);
                }
            }

            if (_band != null)
            {
                float w = Mathf.Abs(SlotPosition(SlotCount - 1).x - SlotPosition(0).x)
                        + CardView.Width * s * 1.12f;
                float h = CardView.Height * s * 1.12f;
                _band.localPosition = LayoutSpace.ToWorld(0.5f, lineY) + new Vector3(0f, 0f, 0.06f);
                _band.localScale = new Vector3(w, h, 1f);
            }
        }

        /// <summary>**落点指示**：点亮「它会落的那一格」（`slot &lt; 0` = 全灭），
        /// 并把那一格摆到 `hudWorld`（**玩家指针**在 HUD 平面上的位置）。
        ///
        /// 🔴 **2026-10-01 换掉了原来的 `SetDragHighlight(bool)`** —— 那个是把九格**一起**染绿，
        /// 只回答「哪些格能放」、**不回答「它会落在哪一格」**，而原版没有这种「全亮」：
        /// 它给的是**一格** —— `MinionManager.ReassembleMinionsWhilePlayingUnit` 里那句
        /// `SetCardShadow(GetLeft/RightSlotPos(落点), active:1, rotate:1)`，**那枚影子落在它真正会去的那一格**
        /// （判据 → `资料/待办判据_战场与战斗视图.md` §8b）。颜色改用原版 `Glow_674` 的绿（见上）。
        /// ⚠️ 原版那一步是 `SetActive` **硬切、没有补间**（`MinionManager__SetCardShadow.c` 方法体亲读）⇒ 硬切。
        ///
        /// 🔴🔴 **位置为什么取【被拖那张卡自己的 transform + 它的层】**（2026-10-01 打了六轮才收敛，别推翻）：
        ///   试过**四套**摆法，**每套都错**（每张都拍了实拍 + 像素探针看的）：
        ///   ① `DropTargetWorld`（= `LayoutSpace.ScreenToWorld` 的相机反投影）⇒ 偏左 **600 px**；
        ///   ② `ArenaSlots.CardCenter`（3D 点 + 挂 `ArenaLayer`）⇒ 被战场相机投到别处；
        ///   ③ `ArenaSlots.RootPosition`（名字像「卡根」，其实是「我们卡心」）；
        ///   ④ 「指针的世界坐标 + 站 HUD 平面」⇒ **整块绿盖到了左边的 hover 弹窗上**
        ///      （`2c_落点指示_拖拽中.png`：30963 个绿像素全在弹窗那块）。
        ///   **根因**：卡是 **3D 空间**里的东西（战场相机画），而这枚底片是 `CreatePrimitive` 的 HUD 平面
        ///   物件（默认层）⇒ 任何「屏幕↔世界」换算都**跨了两台相机**，怎么算都对不上。
        ///   ⇒ **不换算了**：调用方把**那张卡自己的 `transform.position`** 和**它的层**递进来 ——
        ///   「同空间 + 同层」⇒ 两台相机里画的必然是同一台、位置必然是同一个点 ✓
        ///   （画序用 <see cref="MarkerQ"/>：比卡的 3000 大 ⇒ 压在卡上面）。
        /// </summary>
        public void AttachDropSlotTo(int slot, Transform host)
        {
            SetDropSlot(slot);
            if (slot < 0 || slot >= _markers.Count) return;
            var r = _markers[slot];
            if (r == null || host == null) return;
            // 🔴 **挂成「被拖那张卡」的子物体** —— 见上面那段（四套坐标换算全错之后的结论）。
            //   卡自己的子物体（比如它的 blob 阴影）**一定跟着卡画**（实拍里看得见）⇒ 这是唯一的保证。
            //   ⚠️ 还回去之前 `PlaceMarkers` 的 `localPosition` 语义会变 ⇒ `SetDropSlot(-1)` 里复位。
            if (_borrowed == null) { _borrowed = r.transform; _borrowedHome = r.transform.parent; }
            r.transform.SetParent(host, false);
            // ⚠️ z 要**足够靠前**：卡自己内部也是按 z 叠层的（卡体 / 卡框 / 立绘 / 文字各自一层），
            //    给 −0.05 可能还压在它前面几层之后 ⇒ 推到 −1.0（远在卡所有层之前）。
            r.transform.localPosition = new Vector3(0f, 0f, -1.0f);
            // 卡本体在它自己的局部系里就是 `CardView.Width × Height` ⇒ 照这个摆正好盖住卡
            r.transform.localScale = new Vector3(CardView.Width, CardView.Height, 1f);
            r.gameObject.layer = host.gameObject.layer;
            _dropAt = host.position;
        }
        Transform _borrowed, _borrowedHome;

        /// <summary>自检用：最近一次 `SetDropSlot(slot, hudWorld)` 摆到的那个世界点（没摆过 = `Vector3.zero`）。</summary>
        public Vector3 DropSlotAt { get { return _dropAt; } }
        Vector3 _dropAt;

        /// <summary>**落点指示**：只点亮「它会落的那一格」（`slot &lt; 0` = 全灭），位置沿用底片自己的排布。
        /// ⚠️ 3D 时的位置**不准**（见上面那条注释）⇒ 拖拽那条路请用带 `hudWorld` 的那个重载。</summary>
        public void SetDropSlot(int slot)
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                var r = _markers[i];
                if (r == null || r.sharedMaterial == null) continue;
                r.sharedMaterial.color = (i == slot) ? DropSlotColor : MarkerIdle;
            }
            // 全灭 = 收摊：把借出去的那一格**还回 `Slots` 根** —— 不还的话它的 `localPosition`
            // 是相对**那张卡**的（`PlaceMarkers` 下次摆位会把它扔到别处）。
            if (slot < 0 && _borrowed != null)
            {
                _borrowed.SetParent(_borrowedHome, false);
                _borrowed = null; _borrowedHome = null;
            }
        }

        /// <summary>某个格位被占了就单独标暗（原型阶段用不到，留给规则引擎接进来后调）。
        /// ⚠️ **全仓目前没有调用者**；落点指示走 `SetDropSlot`（两者都写同一批材质色，别同时用）。</summary>
        public void MarkOccupied(int slot, bool occupied)
        {
            if (slot < 0 || slot >= _markers.Count) return;
            var r = _markers[slot];
            if (r != null && r.sharedMaterial != null)
                r.sharedMaterial.color = occupied ? new Color(0.32f, 0.20f, 0.20f, 1f) : MarkerIdle;
        }

        /// <summary>自检用：第 `i` 格底片**当前的颜色**（盯落点指示用）。</summary>
        public Color SlotMarkerColor(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return (r != null && r.sharedMaterial != null) ? r.sharedMaterial.color : new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>自检用：现在**亮着几格**（判据 = α &gt; 0.001）。落点指示同时只该亮一格。</summary>
        public int LitSlotCount
        {
            get { int n = 0; for (int i = 0; i < _markers.Count; i++) if (SlotMarkerColor(i).a > 0.001f) n++; return n; }
        }

        /// <summary>自检用：亮着的那一格（没有 = −1）。同时亮多格时返回**最小**的那个行号。</summary>
        public int LitSlot
        {
            get { for (int i = 0; i < _markers.Count; i++) if (SlotMarkerColor(i).a > 0.001f) return i; return -1; }
        }

        /// <summary>自检用：那一格的 MeshRenderer（诊断「相机到底画没画到它」用 —— `isVisible` 是 Unity 自己的旗）。</summary>
        public Renderer SlotMarkerRenderer(int i)
        {
            return (i >= 0 && i < _markers.Count) ? _markers[i] : null;
        }

        /// <summary>自检用：某一格那块底片的**渲染队列** —— 盯「它得画在被拖的卡之后」（见 `MarkerQ`）。</summary>
        public int SlotMarkerQueue(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return (r != null && r.sharedMaterial != null) ? r.sharedMaterial.renderQueue : -1;
        }

        /// <summary>自检用：某一格那块底片所在的**层** —— 盯「相机画不画得到它」
        /// （3D 时必须 = `ArenaSlots.ArenaLayer`，见 `EnsureMarkers`）。</summary>
        public int SlotMarkerLayer(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return r != null ? r.gameObject.layer : -1;
        }

        /// <summary>自检用：某一格那块底片的**世界位置**（诊断「它到底落在哪儿」用）。</summary>
        public Vector3 SlotMarkerWorldPos(int i)
        {
            var r = (i >= 0 && i < _markers.Count) ? _markers[i] : null;
            return r != null ? r.transform.position : Vector3.zero;
        }

        /// <summary>自检用：那一层底片的根（`Slots`）**活动着没有**。</summary>

        public Vector3 SlotPosition(int slot)
        {
            return LayoutSpace.ToWorld(NxOf(slot), lineY);
        }

        public float NxOf(int slot)
        {
            float d = (slot - WarlordSlot) * spacing;
            return 0.5f + (mirror ? -d : d);       // 镜像：敌方槽 0 跑到画面右边
        }

        /// <summary>把世界坐标解析成格位号。离任何格位都太远就返回 false（不合法落点）。</summary>
        public bool TryResolveSlot(Vector3 worldPos, out int slot)
        {
            // 🔴 **2026-09-20：真 3D 那条路单独走一套判据。**
            //    场上的卡站在地面上、由**透视相机**画，屏幕位置和下面那套 2D 行心
            //    **不是一个地方**（实测我方行差 ≈150 px）—— 不改的话「看着落在卡上、实际判到别处」。
            //    拖拽那一路传进来的是**正交平面**里的世界坐标（整个拖拽都活在正交平面里），
            //    所以先把它换回**屏幕点**，再交给 3D 判据（`ArenaSlots.TryResolveSlot`）。
            if (use3D && boardCam != null)
            {
                var n = LayoutSpace.ToNormalized(worldPos);
                var sp = new Vector2(n.x * boardCam.pixelWidth, n.y * boardCam.pixelHeight);
                return ArenaSlots.TryResolveSlot(boardCam, sp, mirror, out slot);
            }

            var nn = LayoutSpace.ToNormalized(worldPos);
            slot = -1;

            // ⚠️ **纵向必须先判**。只比 x 的话，「拖到战场上方的空白处」只要 x 恰好落在某格附近
            //    就会被算成合法落点 —— 实测踩到过：非法落点用例丢在 (0.12, 0.88)，
            //    而槽 0 的 nx 是 0.148，|0.12-0.148|=0.028 < 0.055 就判合法了，
            //    牌直接落到战场上（本该回弹）。7 格时槽 0 在 0.236 恰好躲过，改成 9 格才暴露。
            if (Mathf.Abs(nn.y - lineY) > snapToleranceY) return false;

            float best = float.MaxValue;
            for (int i = 0; i < SlotCount; i++)
            {
                float d = Mathf.Abs(nn.x - NxOf(i));
                if (d < best) { best = d; slot = i; }
            }
            return best <= snapTolerance;
        }

        /// <summary>**真 3D 那条路要用**：这一格的卡**现在画在屏幕上的哪一点**（返回**本物体局部坐标**，
        /// 和 `SlotPosition` 同一个约定 —— 拖拽/回弹/自动对局那几路全都拿它当 `SlotPosition` 用，
        /// 换函数名就够了）。
        /// 没有 3D 战场时就是老那套 `SlotPosition`。判据只有这一处，别在调用点各写一份。</summary>
        public Vector3 DropTargetWorld(int slot)
        {
            if (use3D && boardCam != null)
            {
                float sc = ArenaSlots.CardScale(mirror);
                var sp = boardCam.WorldToScreenPoint(ArenaSlots.CardCenter(slot, mirror, sc));
                if (sp.z > 0f)
                    return transform.InverseTransformPoint(LayoutSpace.ScreenToWorld(new Vector2(sp.x, sp.y)));
            }
            return SlotPosition(slot);
        }

        /// <summary>这一格上的卡是不是**由透视相机画**（真 3D）。`BattleScene` 建场时置。
        /// ⚠️ 与 `BattleDriver.use3DBoard` 是同一个开关的两个落点，必须一起设。</summary>
        public bool use3D;
        /// <summary>画 3D 战场那台透视相机（`use3D` 时必须有）。</summary>
        public Camera boardCam;

        /// <summary>
        /// 这一格能不能放。**这里只表达「表现层觉得能放」** —— 真值由规则引擎说了算
        /// （`RuleCore.CanPlayCard`，含费用和格位占用）。接上驱动层之后这个方法就退化成查询缓存。
        /// </summary>
        public bool IsSlotFree(int slot) { return slot >= 0 && slot < SlotCount; }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            for (int i = 0; i < SlotCount; i++)
            {
                var p = SlotPosition(i);
                Gizmos.DrawWireCube(p, new Vector3(1.2f, 1.8f, 0.02f));
            }
        }
    }
}
