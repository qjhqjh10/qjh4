// WfSlider.cs — 原版音量滑块（Unity `UI.Slider`）的替身
//
// 为什么自己写：我们的 UI 是**世界空间**的（`ImageQuad` + `Label`），没有 uGUI 那套
// `Canvas` / `Slider` 组件。原版那三根是标准 uGUI `Slider`，这里按**它的实测字段**复刻。
//
// 🔴 逐字段出处（`资料/普查产出_0918/第18行_UI三小条_规格.md` §② + 2026-09-19 逐级解父链复核）：
//   · 根尺寸 **561.08 × 14.00 px**（anchorMin(0.10,0.33)/anchorMax(0.90,0.45)，被容器撑出来）
//   · 子物体三件，**绘制序 Background → Fill → Handle**：
//       Background  铺满 561.08×14.00，sprite **`Volume_bar_inactive`**，`Image.Type = Sliced`，border **(184,0,184,0)**
//       Fill        左对齐、宽 = value × 轨道宽，sprite **`Volume_bar_active`**，Sliced，border **(30,0,30,0)**
//       Handle      中心 x 随 value 在 `[−280.54, +270.54]` 上滑动（= 轨道两端各让 0 / 10 px，
//                   即原版 `Handle Slide Area` 的 551.08 宽），sprite **`Volume_button`**（110×110）
//   · Slider 字段：`m_Direction = LeftToRight` · `Min 0` / `Max 1` · `WholeNumbers = 0` · `Value = 1.0`
//     · `m_TargetGraphic` = Handle 的 Image（**不是**轨道 —— 点轨道不改值，只有拖手柄才算）
//
// ⚠️ **两处我们挑的（如实标着）**：
//   1. **手柄的绘制尺寸**：原版 Image 是 `preserveAspect = 1` + 110×110 正方形贴图，塞进 46.811×22.406 的
//      非方框 —— Unity 的 `preserveAspect` 取**能装下**的那个比例（短边 22.406）⇒ 实际画成 22.406 的正方形。
//      我们照这个画。**没有逐帧跟原版比对过**。
//   2. **手柄的纵向位置**：原版 RT 里读到的 y 是 −7.00（贴根底），看起来是编辑器残留；
//      我们按**竖直居中**画。规格文档里也没给定论。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一根原版音量滑块。**不是 MonoBehaviour** —— 它只是三张图 + 一个值。</summary>
    public class WfSlider
    {
        public const float TrackW = 561.08f, TrackH = 14f;      // px
        public const float HandlePx = 22.406f;                  // 见文件头「我们挑的 1」
        public const float HandleSpritePx = 110f;
        public const float TravelRightPx = 10f;                 // 手柄滑动区右端比轨道少 10 px（原版 551.08 vs 561.08）
        const float U = 108f;                                   // px → 世界单位（和 `SettingsPanel` 同一套）

        /// <summary>本件两层的**渲染队列**。原来一次都没显式设过 = `Sprites/Default` 的默认档（**3000**，
        /// 同 `SettingsPanel.QPanel` / `WaitBanner.BattleQChrome`）。**图层序靠 z**（bg 0 → fill −0.001
        /// → handle −0.002，越负越靠前），队列只要不比别人高就行 ⇒ 显式传这个默认档、行为不变。</summary>
        const int QSlider = 3000;

        /// <summary>`MenuDraw.Nine` 要的**画布 px 矩形**。🔴 **只用到它的宽高**：
        /// 九宫格那九块是按**根节点的局部原点**铺的（尺寸从 `−尺寸/2` 起算）⇒ 根节点摆在哪由调用方
        /// 紧接着那行 `localPosition` 定，这里的**中心值只是占位**（取画布中心，好读而已）。
        /// ⛔ 别把中心改成「世界坐标反推的画布 px」—— 那会让这一层在非 16:9 下跟着可见宽挪走。</summary>
        static PxRect TrackRectPx(float trackPx)
        {
            float cx = LayoutSpace.DesignPxW * 0.5f, cy = LayoutSpace.DesignPxH * 0.5f;
            return new PxRect(cx - trackPx * 0.5f, cy - TrackH * 0.5f, cx + trackPx * 0.5f, cy + TrackH * 0.5f);
        }

        public string SliderName;
        public float Min = 0f, Max = 1f;
        public float Value = 1f;
        public Action<float> OnChanged;

        GameObject _root;
        GameObject _fillRoot;      // 九宫格的根（填条要整体缩放/移动）
        ImageQuad _bg, _handle;
        float _w, _h;

        public bool Visible { get { return _root != null && _root.activeSelf; } }

        /// <param name="trackW">轨道宽（px）。⚠️ **2026-09-26 加**：设置窗的音频页那三根取的是
        /// **该页自己的容器宽**（684.19，`资料/联机P2P_设计与交接.md` §3·5），
        /// 和战斗内那根的 561.08 不是同一个数。**默认值 = 老值 ⇒ 战斗那条路一字未改。**</param>
        public static WfSlider Create(Transform parent, string name, Vector3 center, float value,
                                      Action<float> onChanged, float trackW = TrackW)
        {
            var s = new WfSlider();
            s.SliderName = name;
            s.OnChanged = onChanged;
            s.Value = value;

            s._w = trackW / U; s._h = TrackH / U;

            s._root = new GameObject("slider_" + name);
            s._root.transform.SetParent(parent, false);
            s._root.transform.localPosition = center;

            var bgTex = CardArt.Ui("Volume_bar_inactive");
            var fillTex = CardArt.Ui("Volume_bar_active");
            var handleTex = CardArt.Ui("Volume_button");
            if (bgTex == null || fillTex == null || handleTex == null)
            {
                // 不许静默失败：图不在 = 滑块画不出来，而玩家只会看到「这里什么都没有」
                Debug.LogWarning($"[WfSlider] 音量滑块的图缺了："
                    + $"bar_inactive={(bgTex != null)} bar_active={(fillTex != null)} button={(handleTex != null)}"
                    + "（跑 `工具/` 那套取图脚本把三张图放进 Resources/Art/ui/）");
            }
            else
            {
                // 🔴 **2026-10-04（A50③）：两层都收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //    `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，拿不到 `clip`）。
                //    与旧代码**逐项等价**，三样都别改：
                //    ① **尺寸** = 轨道 `trackW × TrackH`（px）：`MenuDraw.Nine` 内部按 `LayoutSpace.Px`
                //       折世界尺寸，与本件那个 `U = 108f` 是**同一个换算**；
                //    ② **落位** = 每层紧跟的 `localPosition` 显式给回 `(0, 0, z)`（旧代码传的 center 就是它）
                //       —— 九块是按**根节点的局部原点**铺的（尺寸从 `−尺寸/2` 起算），与矩形中心值无关，
                //       ⛔ **别删那两行**：删了这一层会跟着 `MenuDraw.Local` 的映射跑（非 16:9 会挪位）；
                //    ③ **渲染队列 = 3000**（`QSlider`）：本件原来一次都没显式设过队列（= `Sprites/Default`
                //       的默认档），而 `MenuDraw.Nine` **会写**队列 ⇒ 显式传同一个数，否则等于换一次排序。
                // ① Background：Sliced，铺满
                var bg = MenuDraw.Nine(s._root.transform, bgTex, TrackRectPx(trackW),
                                       new Vector4(184f, 0f, 184f, 0f), bgTex.width, bgTex.height,
                                       QSlider, name: "slider_bg");
                if (bg != null) bg.transform.localPosition = new Vector3(0f, 0f, 0f);
                // ② Fill：Sliced，左对齐，宽随值
                // ⚠️ **实现上用的是「整体横向缩放」而不是「按目标宽重建九宫格」** ——
                //    代价是左右那 30 px 的端帽会跟着缩（拖到很小的时候端帽变窄）。
                //    我们挑的：重建九宫格要每次 drop_value 都销毁/新建 9 个 quad，拖动时太吵；
                //    而这条填充条只有横端帽（border 上下都是 0），缩放的观感差别很小。**未逐帧比对过**。
                s._fillRoot = MenuDraw.Nine(s._root.transform, fillTex, TrackRectPx(trackW),
                                            new Vector4(30f, 0f, 30f, 0f), fillTex.width, fillTex.height,
                                            QSlider, name: "slider_fill");
                if (s._fillRoot != null) s._fillRoot.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                // ③ Handle：正方形
                s._handle = ImageQuad.Create(s._root.transform, handleTex, new Vector3(0f, 0f, -0.002f),
                                             HandlePx / U, new Vector2(0.5f, 0.5f), "slider_handle");
            }
            s.Layout();
            return s;
        }

        /// <summary>按当前 `Value` 摆 Fill 与 Handle。</summary>
        void Layout()
        {
            float t = Mathf.InverseLerp(Min, Max, Mathf.Clamp(Value, Min, Max));
            if (_fillRoot != null)
            {
                float w = Mathf.Max(0.0001f, t * _w);
                _fillRoot.transform.localScale = new Vector3(w / _w, 1f, 1f);
                _fillRoot.transform.localPosition = new Vector3(-_w * 0.5f + w * 0.5f, 0f, -0.001f);
            }
            if (_handle != null)
            {
                float x = -_w * 0.5f + t * (_w - TravelRightPx / U);
                _handle.transform.localPosition = new Vector3(x, 0f, -0.002f);
            }
        }

        /// <summary>世界坐标是不是落在这根滑块的**轨道**上（点轨道 = 直接跳值，原版 `m_TargetGraphic` 是手柄，
        /// 但单机点轨道不改值会很难用；这里点轨道也认 —— **这一条我们挑的**）。</summary>
        public bool Contains(Vector3 world)
        {
            if (_root == null) return false;
            var l = _root.transform.InverseTransformPoint(world);
            // 纵向给手柄留出高度（手柄比轨道高，点在**手柄上**也该算命中）
            float halfH = Mathf.Max(_h, HandlePx / U) * 0.5f;
            return Mathf.Abs(l.x) <= _w * 0.5f && Mathf.Abs(l.y) <= halfH;
        }

        /// <summary>按指针的世界坐标取值。返回**值变了没有**。</summary>
        public bool SetFromPointer(Vector3 world)
        {
            if (_root == null) return false;
            var l = _root.transform.InverseTransformPoint(world);
            float t = Mathf.Clamp01((l.x + _w * 0.5f) / (_w - TravelRightPx / U));
            float v = Mathf.Lerp(Min, Max, t);
            if (Mathf.Abs(v - Value) < 1e-4f) return false;
            Value = v;
            Layout();
            if (OnChanged != null) OnChanged(Value);
            return true;
        }

        public void SetValue(float v, bool fire)
        {
            v = Mathf.Clamp(v, Min, Max);
            bool changed = Mathf.Abs(v - Value) > 1e-4f;
            Value = v;
            Layout();
            if (fire && changed && OnChanged != null) OnChanged(Value);
        }

        public void SetVisible(bool on) { if (_root != null) _root.SetActive(on); }

        // ---- 自检用 ----
        /// <summary>三张图都取到了没有（缺图 = 玩家什么都看不见）。</summary>
        public bool HasArt
        {
            get
            {
                bool bg = _fillRoot != null && _fillRoot.transform.childCount > 0;
                return bg && _handle != null && _handle.Texture != null;
            }
        }
        public Vector3 WorldPos { get { return _root != null ? _root.transform.position : Vector3.zero; } }
        public Vector3 HandleWorldPos { get { return _handle != null ? _handle.transform.position : WorldPos; } }
        /// <summary>轨道左端 / 右端的世界坐标（自检拿它喂指针，验「点最左 = 0、点最右 = 1」）。</summary>
        public Vector3 LeftWorld { get { return _root.transform.TransformPoint(new Vector3(-_w * 0.5f, 0f, 0f)); } }
        public Vector3 RightWorld { get { return _root.transform.TransformPoint(new Vector3(_w * 0.5f, 0f, 0f)); } }
    }
}
