// Tooltip.cs — 悬停信息层（原版 `EverguildTooltipManager` + `EverguildTooltipItem`）
//
// 原版怎么做（**逐条来自全量反编译**，出处 `d:/2/tools/decomp_full/`）：
//   · 面板是**运行时 instantiate** 的（场景里 `TooltipManager` 只有一个空节点、下面没有子节点）
//   · 显示时机：触发器 `OnPointerEnter` **立刻**显示、`OnPointerExit` **立刻**隐藏（**没有延迟**）
//   · 位置 = **触发器自己的 `transform.position`**（**不跟随鼠标**）+ `defaultOffset` + `data.Offset`
//   · `rt.pivot = GetPivotPosition(TooltipAnchor)`（9 项表，未知值回落 (0.5,0.5) 并打 warning）
//   · 动画只有 **alpha 淡入 0.3s / 淡出 0.3s**（`ANIMATION_TIME = 0.3f`，DOTween 默认 OutQuad），
//     **没有缩放、没有位移**
//   · **没有屏幕边缘翻面/夹取**（全量反编译里零处用到 `Screen.width/height/safeArea`）
//
// 面板本体（**本地就有**，2026-09-20 查到）：
//   `assets_full/bundle_duplicateassetisolation_assets_all/GameObject/BasicToolTip.json`
//   （脚本类 `EverguildTooltipItem`，同名还有 4 个变体：`Battle Tooltip Variant` / `Trait Tooltip` /
//     `Updated Tooltip Variant` / `… - ranked`；子物体 `Background`/`Title`/`Text (TMP)`/`Content`/`Line`/`Mask`）
//   · 底图 = **`Smooth background square`**（32×32、**`m_Border=(12,2,12,12)`** ⇒ 九宫格）
//   · 正文 TMP 实测：**字号 28**（自动缩到 10）、**纯白**、HAlign=2(Center)；
//     **基础 tooltip 只有正文、没有标题**（带标题的是 `EverguildTooltipWithTitle` / Trait 版）
//   ⚠️ **`Tips/HealthTip` 那串不是预制体名、是 I2 本地化 key**（在触发器的 `text` 字段里）；
//      `tooltipPrefab` 字段**全部指向同一个** tooltip 预制体（`assets_full/…/MonoBehaviour_-4253307515847882232.json:17`）。
//
// 🔴 **我们查不到的（如实标，别当原版）**：
//   ① **文案**：原版走本地化 term（`Tips/MeleeAttackTip` 这类），而 **I2 的词条表在远端 CCD**，
//      本地只有 key、**一个 value 都没有**（全量扫过：`I2Languages` 零命中、`assets_full` 的
//      `*.csv/*.tsv` 零命中）。⇒ 见 `TipText`，**那是我们照规则书写的**，不是原版文案。
//   ② 面板的**圆角/内边距/九宫格拉伸目标尺寸**：prefab 的 RectTransform 是 UI 布局算的，
//      我们这套是世界空间 quad ⇒ **内边距与最小宽高是我们挑的**。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>悬停信息层。全局一个，按需建。</summary>
    public class Tooltip : MonoBehaviour
    {
        static Tooltip _inst;
        /// <summary>当前显示的那条（自检用）。没显示时是 null。</summary>
        public static string ShownBody { get; private set; }
        /// <summary>正在显示吗（**淡出期间也算显示**，和原版一致）。</summary>
        public static bool Visible { get { return _inst != null && _inst._target > 0f; } }
        public static int ShowCount { get; private set; }      // 自检用：来过几条
        /// <summary>面板中心的世界坐标（自检用来验 pivot 表）。没显示时是 zero。</summary>
        public static Vector3 PanelCenter
        {
            get { return (_inst != null && _inst._bgRoot != null) ? _inst._bgRoot.transform.localPosition : Vector3.zero; }
        }
        /// <summary>面板尺寸（px，1920 设计空间）—— 自检用。</summary>
        public static Vector2 PanelSizePx
        {
            get { return _inst != null ? new Vector2(_inst._w, _inst._h) : Vector2.zero; }
        }

        /// <summary>渲染队列：压在 HUD / 卡面 / 卡组编辑那些层之上</summary>
        const int QTip = 3200, QTipShadow = 3199, QTipText = 3201;
        /// <summary>原版 `EverguildTooltipItem.ANIMATION_TIME = 0.3f`（桩 `.cs:10`，.rdata 实测 0.3f）</summary>
        const float FadeTime = 0.3f;
        /// <summary>1 世界单位 = 108 px（和 `LayoutSpace` 同一口径）</summary>
        const float PxPerUnit = 1080f / 10f;
        /// <summary>正文的字号：原版 TMP 是 **28 pt**，跨工程只能对**渲染高度**
        /// （28 pt ⇒ 大写高 0.72 em ≈ 20.2 px ≈ 0.1867 世界单位）。同 `OvertimeSplashText` 那条教训：
        /// **原版的 `m_fontSize` 不能照抄**，它按原版画布算。</summary>
        const float BodyCapWorld = 0.72f * 28f / PxPerUnit;

        // 面板内边距与下限 —— ⚠️ **我们挑的**（原版是 UI 布局算的，见文件头 ②）
        const float PadX = 20f, PadY = 14f, MinW = 120f, MaxW = 640f, LineH = 30f;

        // 底图（原版资产，九宫格）
        const string BgSprite = "Smooth_background_square";
        const float BgTexW = 32f, BgTexH = 32f;
        static readonly Vector4 BgBorder = new Vector4(12f, 2f, 12f, 12f);   // m_Border = (12,2,12,12)

        GameObject _bgRoot;
        readonly List<ImageQuad> _bgParts = new List<ImageQuad>();
        Label _body;
        float _alpha, _target;
        Vector3 _pos;                       // 世界坐标（已含 offset）
        float _w = MinW, _h = 60f;

        static Tooltip Ensure()
        {
            if (_inst != null) return _inst;
            var go = new GameObject("TooltipLayer");
            _inst = go.AddComponent<Tooltip>();
            _inst.Build();
            return _inst;
        }

        void Build()
        {
            _body = Label.Create(transform, "", Vector3.zero, 2, new Color(1f, 1f, 1f, 0f),
                                 new Vector2(0.5f, 0.5f), "tip_body");
            _body?.SetRenderQueue(QTipText);
            _body?.SetCapHeight(BodyCapWorld);
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (_inst != this) return;
            if (Mathf.Approximately(_alpha, _target))
            {
                if (_target <= 0f && gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            // `DOTween` 的 `DOFade` 默认 OutQuad；批处理下没有补间驱动 ⇒ 自己按时间推
            _alpha = Mathf.MoveTowards(_alpha, _target, Time.unscaledDeltaTime / FadeTime);
            ApplyAlpha(_alpha);
        }

        void ApplyAlpha(float a)
        {
            foreach (var q in _bgParts) if (q != null) q.SetTint(new Color(0f, 0f, 0f, 0.9f * a));
            if (_body != null) _body.SetColor(new Color(1f, 1f, 1f, a));
        }

        /// <summary>显示一条。`worldPos` 是**触发器自己的位置**（不是鼠标），`anchor` = 原版 `AnchorPositions`。
        /// `anchor` 表：0/5 居中 · 10 左中 · 15 右中 · 17 上中 · 20 左上 · 25 右上 · 27 下中 · 30 左下 · 55 右下。</summary>
        public static void Show(string body, Vector3 worldPos, int anchor = 0, Vector3 offset = default(Vector3))
        {
            if (string.IsNullOrEmpty(body)) { Hide(); return; }
            var t = Ensure();
            t._pos = worldPos + offset;
            ShownBody = body;
            ShowCount++;
            // 🔴 **顺序不能反**：`Layout()` 里要 `ForceMeshUpdate()` 量文字尺寸，
            //    而 TMP **在对象没激活时量不出尺寸**（实测：`tmp=True` 但 `textBounds` 是垃圾，
            //    `tmpW` 变成 4.29e9 ⇒ 面板 640px 宽、字一个都看不见）。
            //    所以：**先激活、再排版**。
            t.gameObject.SetActive(true);
            t.Layout(body);
            t.Pivot(anchor);
            t._target = 1f;
        }

        public static void Hide()
        {
            if (_inst == null) return;
            _inst._target = 0f;
            ShownBody = null;
        }

        /// <summary>自检用：立刻结束淡入淡出（批处理里没有帧循环推 `Update`）。</summary>
        public static void FinishFade()
        {
            if (_inst == null) return;
            _inst._alpha = _inst._target;
            _inst.ApplyAlpha(_inst._alpha);
            if (_inst._target <= 0f) _inst.gameObject.SetActive(false);
        }

        /// <summary>自检用：把面板与文字的当前状态摊开（排「面板在、字不在」这种问题）。</summary>
        public static string DebugDump()
        {
            if (_inst == null) return "(没有实例)";
            var l = _inst._body;
            if (l == null) return "(没有 body)";
            int q = -1;
            if (l.GetComponent<TMPro.TMP_Text>() != null)
                q = l.GetComponent<TMPro.TMP_Text>().fontMaterial != null
                    ? l.GetComponent<TMPro.TMP_Text>().fontMaterial.renderQueue : -2;
            return $"text=\"{l.Text}\" len={l.Text?.Length ?? -1} W={l.WorldW:F3} H={l.WorldH:F3} "
                 + $"cap={l.CapHeightWorld:F4} size={l.transform.localPosition} parentActive={l.transform.parent.gameObject.activeSelf} "
                 + $"selfActive={l.gameObject.activeSelf} queue={q} alpha={_inst._alpha:F2} target={_inst._target:F2} "
                 + $"bgParts={_inst._bgParts.Count} bgPos={PanelCenter} panelPx={PanelSizePx} | {l.DumpSizes()}";
        }

        /// <summary>原版 `EverguildTooltipItem.GetPivotPosition`（9 项表 + 未知值回落）。</summary>
        void Pivot(int anchor)
        {
            Vector2 p;
            switch (anchor)
            {
                case 0: case 5:  p = new Vector2(0.5f, 0.5f); break;   // None / MiddleCenter
                case 10:         p = new Vector2(0f, 0.5f);   break;   // MiddleLeft
                case 15:         p = new Vector2(1f, 0.5f);   break;   // MiddleRight
                case 17:         p = new Vector2(0.5f, 1f);   break;   // TopCenter
                case 20:         p = new Vector2(0f, 1f);     break;   // TopLeft
                case 25:         p = new Vector2(1f, 1f);     break;   // TopRight
                case 27:         p = new Vector2(0.5f, 0f);   break;   // BottomCenter
                case 30:         p = new Vector2(0f, 0f);     break;   // BottomLeft
                case 55:         p = new Vector2(1f, 0f);     break;   // BottomRight
                default:
                    Debug.LogWarning($"[Tooltip] anchor {anchor} not implemented —— 回落 (0.5, 0.5)");
                    p = new Vector2(0.5f, 0.5f); break;
            }
            // 锚点 = pivot 那个点落在 `_pos` 上 ⇒ 面板中心 = pos + (0.5 − pivot) × 尺寸
            float cx = _pos.x + (0.5f - p.x) * _w / PxPerUnit;
            float cy = _pos.y + (0.5f - p.y) * _h / PxPerUnit;
            if (_bgRoot != null) _bgRoot.transform.localPosition = new Vector3(cx, cy, 0f);
            if (_body != null) _body.transform.localPosition = new Vector3(cx, cy, 0f);
        }

        void Layout(string body)
        {
            if (_body == null) return;
            // ⚠️ 我们的 `Label` **不自动换行**（TMP 只是一个 text 组件，宽度是算出来的）
            //    ⇒ 太长会顶到 `MaxW` 上限、字跑到面板外面去。这里按**字符数**手动折行
            //    （CJK 都是等宽的，按字宽算比按像素稳）。**这是我们的做法**，原版靠 UI 布局。
            string wrapped = Wrap(body, MaxCharsPerLine);
            _body.SetText(wrapped);
            float bw = _body.WorldW * PxPerUnit;
            _w = Mathf.Clamp(bw + PadX * 2f, MinW, MaxW);
            int lines = Mathf.Max(1, wrapped.Split('\n').Length);
            _h = lines * LineH + PadY * 2f;
            BuildBg();
        }

        /// <summary>按**显示宽度**折行：CJK/全角算 1 个宽单位，ASCII 算 0.5。</summary>
        const float MaxCharsPerLine = 22f;

        static string Wrap(string body, float maxUnits)
        {
            if (string.IsNullOrEmpty(body)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var raw in body.Split('\n'))
            {
                float line = 0f;
                foreach (char c in raw)
                {
                    float u = c < 0x2E80 ? 0.5f : 1f;
                    if (line + u > maxUnits) { sb.Append('\n'); line = 0f; }
                    sb.Append(c); line += u;
                }
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>九宫格底**尺寸一变就得重建**（九块的位置依赖目标尺寸）。
        /// 原版是一张 `Image (Sliced)`，Unity 自己重排；我们这套是世界空间 quad。</summary>
        void BuildBg()
        {
            if (_bgRoot != null) DestroySafe(_bgRoot);
            _bgParts.Clear();
            var tex = CardArt.DeckUi(BgSprite);
            if (tex == null) { Debug.LogWarning($"[Tooltip] 面板底图 `{BgSprite}` 取不到，只显示文字"); return; }
            _bgRoot = ImageQuad.CreateNineSlice(transform, tex, BgBorder, BgTexW, BgTexH, Vector3.zero,
                                                _w / PxPerUnit, _h / PxPerUnit, "tip_bg");
            foreach (var q in _bgRoot.GetComponentsInChildren<ImageQuad>())
            {
                q.SetRenderQueue(QTip);
                _bgParts.Add(q);
            }
            ApplyAlpha(_alpha);
        }

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }

    /// <summary>
    /// tooltip 的**文案**。
    ///
    /// 🔴 **这是我们写的，不是原版文案** —— 原版走 I2 本地化 key（`Tips/MeleeAttackTip` 那一族），
    ///    而**词条表在远端 CCD**：本地只有 key、一个 value 都没有（`assets_full` 全量扫过：
    ///    `I2Languages` 零命中、`*.csv/*.tsv` 零命中、`LanguageSourceData` 的序列化字段零命中）。
    /// ⇒ 这里的每一句都**照规则书**写（行号见各条注释），**不是**照原版 tooltip 抄的。
    /// **拿到原版词条表之后，把这张表整个换掉即可**（接口不变）。
    /// </summary>
    public static class TipText
    {
        /// <summary>卡面四个数值 + 费用。原版挂点 = `Health/Range Attack/Melee Attack/Cost Container`
        /// （`assets_full/bundle_staticgeneralassets_assets_all/GameObject/` 那几个）。</summary>
        public static string Melee  { get { return "近战攻击力（规则书 :78）"; } }
        public static string Ranged { get { return "远程攻击力（规则书 :78）"; } }
        public static string Armour { get { return "受任何来源的伤害都减这么多，最低减到 1（:167）"; } }
        public static string Health { get { return "扣完护甲后扣生命，归零进弃牌堆（:147）"; } }
        public static string Cost   { get { return "打出去要花的能量（规则书 :78）"; } }

        /// <summary>HUD 计数。</summary>
        public static string Energy      { get { return "每回合恢复，用来打出手牌"; } }
        public static string Skulls      { get { return "本局拿到的战功骷髅数"; } }
        public static string QuestPoints { get { return "暗黑天使的任务点进度（0/3）"; } }
        public static string Faith       { get { return "战斗修女的阵营资源"; } }
        public static string SpiritStone { get { return "灵族的阵营资源；在场也算单位（1 血），点击收集"; } }
    }
}
