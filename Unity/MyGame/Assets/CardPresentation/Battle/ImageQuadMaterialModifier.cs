// ImageQuadMaterialModifier.cs — 原版 `EverguildButtonMaterialModifier`（`IMaterialModifier`）在
//   【`ImageQuad` 载体】上的等价口（账 `B-225` · 2026-11-01 · 第一百一十三会话 `X2`）。
//
// ============================ 原版是什么（先纠正一条错判） ============================
//  原版那颗组件是 `Forge Menu Reward Button/Generic UI Button/Highlight` 的**第 5 个组件**
//  （`RectTransform / CanvasRenderer / Image / LayoutElement / EverguildButtonMaterialModifier`；
//   2026-11-01 现读 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`，另见
//   `Shell/ForgeTab.cs` 的 `BuildGenericHighlight` 注释块）。
//
//  🔴 **它不是「序列化零字段 ⇒ 无视觉作用」的死件 —— 那样判是错的**：
//  · `GetModifiedMaterial(baseMaterial)`：当 `ToogleGreyScale == true` 时返回
//    `EverguildButtonHelper.disabledMaterial`（= 一份**静态缓存**的
//    `new Material(Shader.Find("Everguild/UI/Greyscale"))`），否则原样返回 `baseMaterial`。
//  · `ToogleGreyScale` 的**写点**只有一个：
//    `EverguildButtonHelper.DoMaterialRefresh(list, interactable)` 的末句
//    `*(byte *)(modifier + 0x20) = interactable ^ 1;`
//    （`d:/2/tools/decomp_full/EverguildButtonHelper__DoMaterialRefresh.c` 实际反汇编）。
//    ⚠️ **ILSpy 版 `cpp2il/out_cpp2il_dev_ilspy/EverguildButtonHelper.cs:100-135` 把这一句丢了**
//    （正是「ILSpy 会丢 `= true` 的写点」那一族）⇒ ⛔ **别拿 ILSpy 版判「这个字段没人写」**。
//  · 调用链：`EverguildButton.SoftDisable(true)` / `EverguildButton.SwitchMaterial(interactable)`
//    → `EverguildButtonHelper.GetGraphicsInChildren(gameObject, 1)`（子树里**每一个** `Graphic` 都补一颗
//    这个组件）→ `DoMaterialRefresh(...)`。两条都还有前置闸 `colorTintGreyOnDisable`。
//  ⇒ 原版语义一句话：**按钮变灰（不可交互 / soft-disable）时，这颗 `Highlight` 也跟着换灰材质**。
//
// ============================ 为什么不能【直接搬】 ============================
//  `IMaterialModifier` 只在 uGUI 的 `MaskableGraphic.GetModifiedMaterial` 链上被调用；
//  我们的可见件是 `ImageQuad`（`MeshRenderer` + 自建网格，**不是 `Graphic`**）
//  ⇒ 直接 `AddComponent` 一个实现 `IMaterialModifier` 的类上去**永远没人调它** = 死件（本账原话）。
//
// ============================ 我们这边的既有载体，以及本件补的那一格 ============================
//  · 本工程的等价机制**已经有了**：`Shell/WindowButton.RefreshGray()` —— 置灰时把「该按钮那一组图形件」
//    的材质换成 `Everguild/UI/Greyscale`。shader 名、逐像素算式、`_GreyScale` 默认值、混合状态
//    全都写在 `WindowButton` 头部那段长注释里，⛔ **别在这儿抄第二份**。
//  · 但它覆盖的是 `GrayTargets()` = `GetComponentsInChildren<ImageQuad>(true)`（本钮子树）
//    `+ target + _nine + TintOn`。而原版那颗 `Highlight` 挂在 `Generic UI Button` 的 **`Image`** 上
//    （我们 `ImageQuad.Create` 恒把渲染件建在**子对象**里，见 `Battle/ImageQuad.cs:124-125`），
//    而那颗按钮的 `WindowButton` 挂在**兄弟**命中区 `ClaimHit` 上
//    ⇒ **`Highlight` 那颗 quad 不在任何一棵被扫的子树里**，按钮灰了它不灰。
//  ⇒ 本组件就是补这一格：给「不在按钮子树里、但原版挂了 `MaterialModifier`」的节点一个**显式开关**，
//    换 / 还材质，语义与 `WindowButton.RefreshGray` **同一条**（同一张 shader、同样「只带贴图与颜色」、
//    同样「取不到 shader 就出声、⛔ 不拿别的灰顶替」）。
//
// ============================ 用法 ============================
//  `ImageQuadMaterialModifier.Attach(quad)` 建/取组件，之后 `SetGreyScale(true / false)`。
//  ⚠️ **别把它扩散成第二套灰化机制**：按钮自己子树内的件一律仍走 `WindowButton`；
//     本件只服务「挂在兄弟/父级、扫不到」的那几颗。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`EverguildButtonMaterialModifier` 在 `ImageQuad` 上的等价口：一个显式灰化开关
    /// （语义 / shader / 取不到时的行为都照 `Shell/WindowButton.RefreshGray` 那一条，见文件头）。</summary>
    [DisallowMultipleComponent]
    public sealed class ImageQuadMaterialModifier : MonoBehaviour
    {
        /// <summary>原版那张灰化 shader 的内部名（判据 → `Shell/WindowButton.GrayShaderName` 的同一条注释；
        /// ⛔ 不许用 `Shader.Find` —— 构建里找不到，要走随包 shader bundle）。</summary>
        public const string GrayShaderName = "Everguild/UI/Greyscale";

        ImageQuad _quad;
        /// <summary>换灰之前那份材质（退出时**原样还回去**，不是重建一份）。</summary>
        Material _saved;
        /// <summary>我们为这颗 quad 建的那一份灰材质（一份 quad 一份：我们的贴图/颜色存在材质上，
        /// 一份共享材质装不下多张贴图 —— 同 `WindowButton` 那段「一处如实标注的偏离」）。</summary>
        Material _gray;

        /// <summary>现在灰着吗（原版那一个布尔的可读版）。</summary>
        public bool GreyScale { get; private set; }

        /// <summary>本件服务的渲染件（`Attach` 时传入；`null` = 还没接上）。</summary>
        public ImageQuad Quad { get { return _quad; } }

        /// <summary>这颗 quad **当下**的材质（自检用：判「换没换成功 / 还回去没有」）。</summary>
        public Material CurrentMaterial
        {
            get { var mr = Renderer; return mr != null ? mr.sharedMaterial : null; }
        }

        MeshRenderer Renderer { get { return _quad != null ? _quad.GetComponent<MeshRenderer>() : null; } }

        /// <summary>建 / 取一颗 quad 上的本组件（幂等）。<paramref name="quad"/> 为 `null` ⇒ 出声 + 返回 `null`。</summary>
        public static ImageQuadMaterialModifier Attach(ImageQuad quad)
        {
            if (quad == null)
            {
                Debug.LogWarning("[ImageQuadMaterialModifier] `Attach(null)` —— 没有渲染件可挂，本件不建（不静默）。");
                return null;
            }
            var m = quad.gameObject.GetComponent<ImageQuadMaterialModifier>();
            if (m == null) m = quad.gameObject.AddComponent<ImageQuadMaterialModifier>();
            m._quad = quad;
            return m;
        }

        /// <summary>灰化开关。**幂等**（同态重复调用不重建材质）；取不到 shader ⇒ 保持原样 + 出声
        /// （⛔ 不拿别的灰顶替 —— 铁律 3：那就成了「我们挑的观感」冒充原版）。</summary>
        public void SetGreyScale(bool on)
        {
            if (_quad == null)
            {
                Debug.LogWarning("[ImageQuadMaterialModifier] `" + name + "` 还没 `Attach` 过 `ImageQuad` ⇒ "
                               + "`SetGreyScale(" + on + ")` 什么都不做（不静默）。");
                return;
            }
            var mr = Renderer;
            if (mr == null)
            {
                Debug.LogWarning("[ImageQuadMaterialModifier] `" + name + "` 拿不到 `MeshRenderer`（渲染件没了？）"
                               + " ⇒ `SetGreyScale` 什么都不做（不静默）。");
                return;
            }
            if (on)
            {
                if (GreyScale) return;
                if (_gray == null)
                {
                    Shader sh;
                    if (!WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(GrayShaderName, out sh) || sh == null)
                    {
                        // 红线：不许静默失败，也不拿别的灰顶替（同 `WindowButton.RefreshGray`）。
                        if (!WindowButton.MissingGrayArt.Contains(name)) WindowButton.MissingGrayArt.Add(name);
                        Debug.LogWarning("[ImageQuadMaterialModifier] 取不到原版灰化 shader `" + GrayShaderName
                                       + "`（随包 shader bundle 在不在？见 `资料/特效还原_进度与交接.md` §三）"
                                       + " ⇒ `" + name + "` **不变灰**（⛔ 不拿别的灰顶替）。");
                        return;
                    }
                    _gray = new Material(sh);
                    _gray.name = "UI Greyscale (" + name + ")";
                }
                _saved = mr.sharedMaterial;          // 换之前那一份（还原时原样还回去）
                _gray.color = _quad.Tint;            // 原版那份「一个属性都没写」⇒ 只带颜色；贴图由 SetMaterial 带
                _quad.SetMaterial(_gray);
                GreyScale = true;
            }
            else
            {
                if (!GreyScale) return;
                if (_saved != null) _quad.SetMaterial(_saved);
                _saved = null;
                GreyScale = false;
            }
        }
    }
}
