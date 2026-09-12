// BoardLayout.cs — 战场的 9 个格位（督军居中 + 两侧各 4）
//
// 和手牌一样用归一化坐标，换分辨率/宽高比都成立。
// 格位间距/缩放搬自原版实测（下面的常量），**敌方那个实例要镜像**（原版就是镜像的）。
//
// ⚠️ 格数 **9** 是硬决定，权威来源三处一致：
//    · 规则书 :30 ——「玩家棋盘 9 个空格，督军永远占据中央格，两侧各 4 格共可部署 8 张部队卡」
//    · `rule_core.gd:44` —— BOARD_SIZE 9 / WARLORD_SLOT 4
//    · battlearena1 的 `MinionArea` —— slotsPerSide=4
//    之前这里是 7 格，那是被推翻过的误读（rule_core.gd:42 有修正记录）。
//    规则引擎那边对应 `RuleEngine.BoardSpec`，**两边必须一致**。
//
// ---- 间距/缩放是量出来的，不是拍的 ----
//   出处：`d:/warpforge/scripts/battle.gd` 的场卡尺寸体系（8-27 审查定案，对着原版
//   冻结帧量的），原版 1920×1080 下：
//     · 场卡      137.2 × 218.4 px   （2DCard 2.0927 宽 × desiredScale 0.36 × k 182.14）
//     · 相邻中心距 149.3 px          （MinionSeparation 0.82 × 182.14）
//     · 玩家行中心 y = 708，敌方行中心 y = 466
//   换到我这套（可见高 10 单位 = 1080 px，可见宽 17.78）：
//     placedScale 0.876 → 卡宽 1.271 世界 = 137.2 px；spacing 0.0778 → 1.382 世界 = 149.3 px
//   ⚠️ 原版敌方那一行是**投影收窄**的（131.9 px 步进、卡也更小），2D 里不适用 ——
//      两边同尺寸、只镜像**顺序**。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public class BoardLayout : MonoBehaviour
    {
        public const int SlotCount = 9;

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
        public float placedScale = 0.876f;

        public bool IsWarlord(int slot) { return slot == WarlordSlot; }

        // ---- 格位底片（给玩家看「能往哪儿放」）----
        // 底片由这里创建而不是场景里摆 —— 它得跟着 LayoutSpace 的分辨率缩放走，
        // 而且拖拽时要整体变色，放在一处最省事。
        readonly List<Renderer> _markers = new List<Renderer>();
        Transform _band;

        // 有战场背景图之后，底片要**很轻**：原版平时不画格子，只在拖拽时亮起来提示落点。
        // 所以 idle 只是一层「淡阴影」（让两行棋盘看得出是两块区域），拖拽时才明显。
        static readonly Color MarkerIdle = new Color(0.04f, 0.05f, 0.08f, 0.20f);
        static readonly Color MarkerActive = new Color(0.35f, 0.85f, 0.45f, 0.45f);  // 拖拽中：亮绿
        static readonly Color BandColor = new Color(0.04f, 0.06f, 0.10f, 0.30f);     // 槽带：半透明压暗

        public void EnsureMarkers()
        {
            if (_markers.Count == 0) BuildMarkers();
            PlaceMarkers();
        }

        void BuildMarkers()
        {
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
                r.sharedMaterial = new Material(baseMat) { color = MarkerIdle };
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
                r.transform.localPosition = SlotPosition(i) + new Vector3(0f, 0f, 0.05f);
                r.transform.localScale = new Vector3(CardView.Width * s, CardView.Height * 1.05f * s, 1f);
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

        /// <summary>拖拽中把格位亮起来 —— 同时告诉玩家「这局有哪些位置能放」</summary>
        public void SetDragHighlight(bool on)
        {
            foreach (var r in _markers)
                if (r != null && r.sharedMaterial != null)
                    r.sharedMaterial.color = on ? MarkerActive : MarkerIdle;
        }

        /// <summary>某个格位被占了就单独标暗（原型阶段用不到，留给规则引擎接进来后调）</summary>
        public void MarkOccupied(int slot, bool occupied)
        {
            if (slot < 0 || slot >= _markers.Count) return;
            var r = _markers[slot];
            if (r != null && r.sharedMaterial != null)
                r.sharedMaterial.color = occupied ? new Color(0.32f, 0.20f, 0.20f, 1f) : MarkerIdle;
        }

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
            var n = LayoutSpace.ToNormalized(worldPos);
            slot = -1;

            // ⚠️ **纵向必须先判**。只比 x 的话，「拖到战场上方的空白处」只要 x 恰好落在某格附近
            //    就会被算成合法落点 —— 实测踩到过：非法落点用例丢在 (0.12, 0.88)，
            //    而槽 0 的 nx 是 0.148，|0.12-0.148|=0.028 < 0.055 就判合法了，
            //    牌直接落到战场上（本该回弹）。7 格时槽 0 在 0.236 恰好躲过，改成 9 格才暴露。
            if (Mathf.Abs(n.y - lineY) > snapToleranceY) return false;

            float best = float.MaxValue;
            for (int i = 0; i < SlotCount; i++)
            {
                float d = Mathf.Abs(n.x - NxOf(i));
                if (d < best) { best = d; slot = i; }
            }
            return best <= snapTolerance;
        }

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
