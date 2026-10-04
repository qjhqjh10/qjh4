// PromptPopup.cs — 通用提示窗（「暂无服务器」等的宿主）
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §七。**类名照原版**：`PromptPopup : GameWindow`（MB `-8455344173513121409`），
// prefab 根 `GenericPromptWindow`（`RT/-2582785123254017665`，`bundle_menus_assets_all`）。
//
// 🔴 **2026-09-23 换版**：这个文件以前叫 `PopUpGameWindow`（**我们自建**的版面，注释写着
//   「原版那两个 popup prefab 本地没有」）—— **那条已经不成立了**：`GenericPromptWindow`
//   在 `bundle_menus_assets_all` 里参数齐全（21 个节点），现在照它重做（铁律 5：就地改掉错记录）。
//
// ---- 照原版的部分（逐条有出处）----
//   `type=1(Popup)` · `windowsPlacement=15` · `closeOnESC=0` · `extraScaleSmallScreen=1.0`
//   根 `sz=(1919 × 1079)` · `Menu Dark Background` `sz=(4574.6 × 2572.36)` 无 sprite、色 **(0,0,0,0.7725)**
//   `Window` `sz=(900 × 0)`（**宽是定的、高由 VLG+CSF 算**）
//   `Generic Popup Background` = `40k_popup` **Sliced**，九宫格 **(169,160,169,160)**、359×336
//   `Mask` `sz=(−20.268, −19.245)` · `Background fill` = `40k_popup_texture` **Tiled**、`ppuMultiplier=2.0`（⇒ 128/2 = **64 一格**）
//   `MessageText` fs=**50** 居中 · **最小高 100**；`Buttons` **最小高 110** · HLG **spacing 40** · **LowerCenter**
//   `CancelButton` / `OkButton` = `40K_button`（489×107）色 **(0.3686,0.8941,0.5874,1)** · `Simple + PreserveAspect` · `sz=(0,80)`
//   `Button Text` fs=**50**（autosize 12→50）居中
//
// ---- 🔴 三处**我们挑的**（原版查不到，按铁律 3 标出来，不许冒充原版）----
//   ① **面板高**：原版是 `VerticalLayoutGroup`（align=4 · sp=0 · ctlH=1）+ `ContentSizeFitter(V=PreferredSize)`
//      在**运行时**算出来的，序列化里只有 `sz=(900, 0)`。我们的引擎没有布局系统 ⇒
//      按「`MessageText` 的实际渲染高（下限 100）+ `Buttons` 的 110」自己算（**同一套语义，不是抄的值**）。
//   ② **只有 Ok 时**：原版 prefab **只有 2 按钮版**，运行时会不会藏 `Cancel` 静态查不到
//      （`STUB:PromptPopup.cs` 是空体签名桩）⇒ 我们**藏 Cancel、把 Ok 居中**，并在日志里说明。
//   ③ **`inputs` / `Error Message` 不建**：它们在 prefab 里出厂 `active=1`，但显然由脚本按状态开关
//      （`PromptPopup` 的字段表里有 `inputHolder` / `inputPrefab` / `errorMessage`）——
//      一个「暂无服务器」提示里出现密码输入框与 `INVALID PASSWORD ERROR` 是不可能的。
//      我们的提示窗没有输入/校验这两种流程 ⇒ **按「脚本字段引用的节点＝代码控制」处理**，不建。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`PromptPopup : GameWindow` —— 原版唯一的通用提示窗。也是边界③「点了如实提示」的唯一宿主。</summary>
    public class PromptPopup : GameWindow
    {
        // ---- 出处：正本 §七 的节点表 ----
        public const float ShadeW = 4574.6f, ShadeH = 2572.36f;
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.7725f);
        public const float PanelW = 900f;
        /// <summary>`Generic Popup Background` 的 `sz=(100,100)`（`a=(0,0)-(1,1)`）⇒ 四周各外扩 **50**。</summary>
        public const float BgPad = 50f;
        /// <summary>`Mask` 的 `sz=(−20.268,−19.245)` ⇒ 相对背景四边各内缩一半。</summary>
        public const float MaskInsetX = 20.268f, MaskInsetY = 19.245f;
        public const float MsgMinH = 100f, BtnRowH = 110f, BtnH = 80f, BtnGap = 40f;
        public const float MsgFontPx = 50f, BtnFontPx = 50f;
        public static readonly Color BtnColor = new Color(0.3686f, 0.8941f, 0.5874f, 1f);

        public const string ArtPopup = "40k_popup";              // 359×336 · 九宫格 (169,160,169,160)
        public const string ArtPopupFill = "40k_popup_texture";  // 128×128 · Tiled · ppuMultiplier 2 ⇒ 64 一格
        public const string ArtButton = "40K_button";            // 489×107
        public const float PopupTexW = 359f, PopupTexH = 336f;
        public const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>`40k_popup_texture` 的平铺格（画布 px）= 128 ÷ `ppuMultiplier 2.0`。</summary>
        public const float FillTilePx = 64f;

        // 🔴 渲染队列**必须高于所有「页」**。原来写的是 3018…3023、理由写「奖励窗最高 3014」——
        //    **2026-09-23 更正（铁律 5）：那条前提过期了**。第 3 层那几页的底板用到
        //    `ForgeTab` 3027 / `CampaignTab` 3064 ⇒ 3018 的弹窗**画在页底板下面**，
        //    在战役页/锻造页上弹一个提示会被页盖住，而**矩形断言量不到**（同族于「量矩形量不到被盖住」）。
        //    ⇒ 整段抬到 **3140+**（高于 `CampaignRewardWindow` 3110…3123；`Tooltip` 3199+ 仍在最上）。
        //    ⚠️ 新增任何一页都要回头看这个数 —— `ShellScene` 里有一条断言钉住「弹窗 > 所有已建页」。
        public const int QShade = 3140, QPanel = 3141, QFill = 3142, QContent = 3143, QText = 3144;

        string _text, _okText, _cancelText;
        System.Action _onOk, _onCancel;

        /// <summary>面板最后算出来的高（自检用）。</summary>
        public float PanelH { get; private set; }
        public bool TwoButtons { get; private set; }

        public static PromptPopup Create(WindowsManager mgr, string text, string okText, System.Action onOk,
                                         string cancelText, System.Action onCancel)
        {
            var go = new GameObject("GenericPromptWindow");
            var win = go.AddComponent<PromptPopup>();
            win.type = WindowType.Popup;                  // 实证 type=1
            win.placement = WindowsPlacement.Popup;       // 实证 windowsPlacement=15
            win.closeOnEsc = false;                       // 实证 closeOnESC=0
            win.extraScaleSmallScreen = 1f;               // 实证 1.0
            win._text = text;
            win._okText = okText;
            win._cancelText = cancelText;
            win._onOk = onOk; win._onCancel = onCancel;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);

            float cx = 960f, cy = 540f;                       // 屏幕中心（画布像素）

            // 1) 压暗整屏：`Menu Dark Background` —— **无 sprite**，UGUI 会回落到 `Graphic.OnPopulateMesh`
            //    画一块纯色矩形（同一个套路见 `RewardsWindow` 的背景与卡片 header）。
            Solid(root, cx, cy, ShadeW, ShadeH, ShadeColor, QShade, "Menu Dark Background");

            // 2) `MessageText`：先按 900 宽限制换行画出来（**量出它的真实高度**），再定面板高。
            TwoButtons = !string.IsNullOrEmpty(_cancelText);
            float msgW = PanelW - 40f;                        // ⚠️ 我们挑的左右留白（原版由 VLG 的 padding 决定，是 0）
            var msgLb = Label.Create(root, _text ?? "", new Vector3(0f, 0f, 0f), 6, Color.white,
                                     new Vector2(0.5f, 0.5f), "MessageText");
            float msgH = MsgMinH;
            if (msgLb != null)
            {
                msgLb.SetRenderQueue(QText);
                msgLb.SetGlyphHeight(LayoutSpace.Px(MsgFontPx));
                msgLb.SetWrapWidth(LayoutSpace.Px(msgW));
                msgLb.RefreshBounds();
                msgH = Mathf.Max(MsgMinH, msgLb.WorldH * 108f);
            }

            PanelH = msgH + BtnRowH;
            float px1 = cx - PanelW * 0.5f, px2 = cx + PanelW * 0.5f;
            float py1 = cy - PanelH * 0.5f, py2 = cy + PanelH * 0.5f;

            // 3) 背景：`Generic Popup Background`（九宫格 Sliced，外扩 50）
            var bgGo = Node(root, "Generic Popup Background", cx, cy, PanelW + BgPad * 2f, PanelH + BgPad * 2f);
            var bgTex = CardArt.MenuUi(ArtPopup);
            if (bgTex != null)
            {
                // 🔴 2026-10-05 修（真 bug：**第 7/8 实参写反**）：`CreateNineSlice` 第 7/8 形参是
                //   `worldW` / `worldH`（`Battle/ImageQuad.cs:330-334`），这里原来把 `Px(PanelH+…)` 喂给了
                //   `worldW`、`Px(PanelW+…)` 喂给了 `worldH` ⇒ 底板按「**面板高+100 宽 × 1000 高**」建
                //   （最小高时 = **310 × 1000**，应为 **1000 × 310**）。
                //   判据 = 全工程其余 `CreateNineSlice` 直调点（`grep -rn "ImageQuad.CreateNineSlice("` 实测；
                //   ⚠️ 条数随并发写手会漂、别当定值）**一律「宽在前、高在后」**
                //   （共用件 `Shell/MenuDraw.Nine` 里那一句是最典型的一处）；`Editor/RewardsScene.cs` 的探针更是
                //   **同一张 `40k_popup`**（同 border 169/160）塞进 `1000×90` 建过 —— 本条是孤例。
                //
                // 🔴 **2026-10-06（A50③）：这一处收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //   `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，**拿不到 `clip` / `clipSoftness`**）。
                //   与旧代码**逐项等价**（三样都别改，判据同 `Battle/WfSlider.cs:112-124` 那条第一处收口）：
                //    ① **矩形** = 面板四周各外扩 `BgPad`(50)：`px1−50 → px2+50` / `py1−50 → py2+50`，
                //       就是上面 `bgGo` 那个 `PanelW+100 × PanelH+100`（`MenuDraw.Nine` 内部按 `LayoutSpace.Px`
                //       折世界尺寸，与旧代码那两个 `LayoutSpace.Px(…)` 是同一个换算）；
                //    ② **落位** = `Local(bgGo, …)` 的局部位移 —— `bgGo` 的中心**正是**这个矩形的中心
                //       ⇒ 位移恒 `Vector3.zero`（与旧代码显式传的那个零向量同值）；⚠️ 别把 `bgGo` 挪走；
                //    ③ **队列 = `QPanel`**（旧代码建完逐个子块设的就是这一档，`MenuDraw.Nine` 会替我们设）；
                //       tint 仍不传（旧代码也没传）。
                // ⚠️ **别再给子块 `SetAspect`**：`CreateNineSlice` 建每一块时已按
                //   **真九宫格**把该块自己的长宽比算好（`ImageQuad.cs:397` 的 `q.SetAspect(w / h)`）。
                //   原来这里还把**面板的**长宽比套给每一块（`SetAspect((PanelW+100)/(PanelH+100))`）⇒
                //   九块各自被拉成面板的形状、互相重叠/留缝，**并集永远不等于面板矩形**
                //   （算式：`PanelH`=300 时并集 = **1231 宽** × 400 高，应为 **1000** × 400）—— 2026-10-05 已删。
                //   全工程所有 `CreateNineSlice` 调用点（含 `MenuDraw.Nine`）对子块**只设 tint / 队列**：
                //   `grep -A 10 "ImageQuad.CreateNineSlice(" | grep SetAspect` 实测**一处都没有**
                //   （唯一命中的是本条注释自己）；`Editor/ShellScene.cs` 的 ⑤b 段有「九块并集 = 面板矩形」的断言盯住它。
                var bgRect = new PxRect(px1 - BgPad, py1 - BgPad, px2 + BgPad, py2 + BgPad);
                MenuDraw.Nine(bgGo, bgTex, bgRect, new Vector4(169f, 160f, 169f, 160f), PopupTexW, PopupTexH,
                              QPanel, null, true, "Nine");
            }
            else Debug.LogWarning($"[Prompt] 取不到 `{ArtPopup}`（面板底板没画）—— 导入器：`工具/import_original_art.py`");

            // 4) `Mask` → `Background fill`（Tiled，64 一格）。⚠️ 我们的引擎没有 Mask ⇒ 直接按 Mask 的矩形铺，
            //    不裁子件（这里子件只有它自己，等价）。
            // 🔴 **2026-10-04（A25⑤）**：原来在这里直调 `ImageQuad.CreateTiled` —— 那是**绕开公共件**的
            //    第四条路（`MenuDraw.Tiled` 才带 `clip` / `clipSoftness`），已收口。
            //    ⚠️ 摆位/尺寸/队列逐项等价：`MenuDraw.Tiled` 把根摆在 `Local(parent, r)`，
            //    而这里的 `fillGo` 本来就是按 `fillRect` 建的空节点 ⇒ 局部位移恒为 0（与 `Vector3.zero` 同）。
            var fillRect = new PxRect(px1 - BgPad + MaskInsetX * 0.5f, py1 - BgPad + MaskInsetY * 0.5f,
                                      px2 + BgPad - MaskInsetX * 0.5f, py2 + BgPad - MaskInsetY * 0.5f);
            var fillGo = Node(root, "Background fill", fillRect);
            var fillTex = CardArt.MenuUi(ArtPopupFill);
            if (fillTex != null)
            {
                MenuDraw.Tiled(fillGo, fillTex, fillRect, FillTilePx, QFill, "Tiles");
            }
            else Debug.LogWarning($"[Prompt] 取不到 `{ArtPopupFill}`（面板填充没铺）—— 导入器：`工具/import_original_art.py`");

            // 5) 文案：摆在**上半区**（`MessageText` 是 VLG 的第一个内容件，占 msgH 高；下面才是 110 的按钮行）
            if (msgLb != null)
            {
                float mTop = py1, mBot = py1 + msgH;
                msgLb.transform.localPosition = Local(root, cx - msgW * 0.5f, mTop, cx + msgW * 0.5f, mBot);
            }

            // 6) 按钮：`Buttons` 行 110 高、**LowerCenter**（align=7）⇒ 按钮贴行底；HLG spacing 40、**等分**
            float rowBot = py2;
            float btnY2 = rowBot, btnY1 = rowBot - BtnH;      // LowerCenter ⇒ 底对齐
            if (TwoButtons)
            {
                float halfW = (PanelW - BtnGap) * 0.5f;
                MakeButton(root, px1, px1 + halfW, btnY1, btnY2, _cancelText, false);
                MakeButton(root, px1 + halfW + BtnGap, px2, btnY1, btnY2, _okText, true);
            }
            else
            {
                // ⚠️ **我们挑的**（原版只有 2 按钮版）：只给 Ok 时把它摆中间，宽度按 AspectRatioFitter 的比例 5.1406
                //    （`Button Text` 上的 ARF：`mode=1` WidthControlsHeight、`ratio=5.1405730`）
                float w = Mathf.Min(PanelW, BtnH * 5.1405730f);
                MakeButton(root, cx - w * 0.5f, cx + w * 0.5f, btnY1, btnY2, _okText, true);
                Debug.Log("[Prompt] 只给了 Ok ⇒ **藏掉 Cancel、Ok 居中**（⚠️ 我们挑的：原版 prefab 只有 2 按钮版，" +
                          "运行时是否隐藏 Cancel 静态查不到，见 `资料/日常_原版规格.md` §七）");
            }
        }

        void MakeButton(Transform root, float x1, float x2, float y1, float y2, string text, bool isOk)
        {
            var go = Node(root, isOk ? "OkButton" : "CancelButton", new PxRect(x1, y1, x2, y2));
            // `40K_button` · `Simple + PreserveAspect` ⇒ 等比放进框（框 430×80 vs 源图 489×107 ⇒ 宽受限）
            var tex = CardArt.MenuUi(ArtButton);
            if (tex != null)
            {
                float w = x2 - x1, h = y2 - y1, aspect = BtnTexW / BtnTexH;
                if (w / h > aspect) w = h * aspect; else h = w / aspect;
                float mx = (x1 + x2) * 0.5f, my = (y1 + y2) * 0.5f;
                var q = ImageQuad.Create(go, tex, Local(root, mx - w * 0.5f, my - h * 0.5f, mx + w * 0.5f, my + h * 0.5f),
                                         LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), "Image");
                if (q != null)
                {
                    q.SetAspect(aspect);
                    q.SetTint(BtnColor);
                    q.SetRenderQueue(QContent);
                    var hit = q.gameObject.AddComponent<WindowButton>();
                    hit.onClick = () => Choose(isOk);
                    // 🆕 2026-10-03 A17：原版 `GenericPromptWindow>Buttons>{Cancel,Ok}Button` 是 SpriteSwap
                    //（实测 `trans=2` · HL = `40K_button_hover` · P = `40K_button_pressed`；
                    //  普查那 5 块表**漏了这扇窗**，是接线时按 `HasHit` 反查出来的）
                    hit.BindSelf(ArtButton);
                }
            }
            else Debug.LogWarning($"[Prompt] 取不到 `{ArtButton}`（按钮底没画）");

            // `Button Text` `sz=(−26,0)` fs=50 居中白
            var lb = Label.Create(go, text ?? "", Vector3.zero, 6, Color.white, new Vector2(0.5f, 0.5f), "Button Text");
            if (lb != null)
            {
                lb.SetRenderQueue(QText);
                lb.SetGlyphHeight(LayoutSpace.Px(BtnFontPx));
                lb.SetWrapWidth(LayoutSpace.Px((x2 - x1) - 26f));
                lb.transform.localPosition = Local(root, x1, y1, x2, y2);
            }
        }

        void Choose(bool ok)
        {
            var a = ok ? _onOk : _onCancel;
            Close();
            a?.Invoke();     // ⚠️ 先关再回调（照原版 `HidePopUp` 在 `OnPress` 之后）—— 回调里可能再开窗
        }

        // ---------------------------------------------------------- 画图小工具（与 MissionsTab 同一套换算）

        static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - (parent != null ? parent.position : Vector3.zero);

        static Transform Node(Transform root, string name, PxRect r)
            => Node(root, name, r.CX, r.CY, r.W, r.H);

        static Transform Node(Transform root, string name, float cx, float cy, float w, float h)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = Local(root, cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
            return t;
        }

        static ImageQuad Solid(Transform root, float cx, float cy, float w, float h, Color color, int q, string name)
        {
            var quad = ImageQuad.Create(root, CardArt.Solid(),
                                        Local(root, cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect(w / h);
            quad.SetTint(color);
            quad.SetRenderQueue(q);
            return quad;
        }
    }

    /// <summary>弹窗按钮的点击接收（原版 `GameWindowButton` 的最小等价物）。
    ///
    /// 🔴 **这个类为什么住在本文件里（2026-10-05 A65② 定案）**：它最早的长相就是「`PromptPopup` 那两颗钮的
    /// 点击接收」，类注释也是那么写的；**A65② 期间评估过把它拆成 `Shell/WindowButton.cs`，结论是不拆** ——
    /// ① 全工程 **40 个文件**引用它（`grep -rl` 实测，含本文件），而 **0 处**是**序列化引用**
    ///    （本工程所有 `WindowButton` 都是 `AddComponent<WindowButton>()` 运行时挂的；
    ///    `guid` 扫描实测：`PromptPopup.cs` 的 guid 只在 `CardPresentation/Scenes/CollectionCheck.unity`
    ///    里出现过一次，且那一处是 `Assembly-CSharp::CardPresentation.PromptPopup`，**不是 `WindowButton`**）；
    /// ② 拆出去要**新建一个 `.cs` + 新 `guid`**，而 Unity 的 `.meta` 只能由编辑器生成 —— 这一批里
    ///    **没人能跑 Unity 验证**，等于拿一次没人验过的搬迁换一点观感上的整洁；
    /// ③ 搬迁会留下一小段「类不在原处、新文件还没被 Unity 导入」的窗口，同批**并行的别的写手**
    ///    做秒级类型检查时会撞上一串假错（铁律 13·3 第 2 条那类假错）。
    /// ⇒ **留在原处**；将来真要拆，单独开一件、并在能跑 Unity 的那一轮做。</summary>
    public class WindowButton : MonoBehaviour
    {
        public System.Action onClick;

        // ============================================================ 🆕 2026-10-03 悬停 / 按下（原版 UGUI 的按钮态）
        //
        // 🔴 **判据 = UGUI `Selectable.DoStateTransition` + 原版 prefab 里的实测值**（不是我们挑的）：
        //   · 原版 `bundle_menus_assets_all` 里 **1276 个按钮组件**逐个数过：`m_Transition` = 2(SpriteSwap) **630** ·
        //     1(ColorTint) **505** · 0(None) **141**。
        //     ⚠️ **2026-10-04 更正（铁律 5）**：下面这句原来写的是「`m_Colors` 全部是 UGUI 默认那组」——
        //     **不成立**：实测那 **1276 颗里 150 颗不是**（同一份数据独立复算过两遍）。
        //     分布：`EverguildToggle` 一族 **51** 颗 HL = 绿 `(0.292,0.953,0.682)` · **36** 颗浅蓝
        //     `(0.882,0.976,1)` · **19** 颗青 `(0.609,0.943,1)` · **13** 颗 `(0.6887,…)`（卡组行的两颗就在其中）· 其余零散。
        //     按 prefab 根分组最大的几族：`Give Feedback Popup menu` **41** · `Player Profile Window` **16** ·
        //     `Main Menu Settings Window` **14** · `Social Submenu Variant` 6 · `Collection Menu Variant` 5 …
        //     ⇒ **默认值只是「没被改过的那一批」**：`HighlightK` / `PressedK` 这两个常量**不能当全库判据**，
        //     要拿它比对必须**先逐颗读 `m_Colors`**（这就是本类只做「统一色偏兜底」时留下的那笔账）。
        //     卡组编辑窗里占了 2 颗：`Deck Selector Defensive Card Slot` 与 `Deck Selector Card Info button`
        //     的 HL = `(0.6887,0.6887,0.6887,1)` —— 那两颗的悬停**没走本类**，走 `DeckRuntime.RowHoverK`。
        //     ⚠️ 出处分界：**这一句不在** `按钮悬停图_普查.md` 里（那份没提 `m_Colors`）——
        //     数据出处 = `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md` §四②。
        //     默认那一组本身是：`m_HighlightedColor = 0.9607843` · `m_PressedColor = 0.7843137` ·
        //     `m_FadeDuration = 0.1` · `m_ColorMultiplier = 1.0`。
        //   · UGUI 的 `ColorTint` = `targetGraphic.CrossFadeColor(tintColor, fadeDuration)` —— 它是
        //     **乘**在顶点色上（`canvasRenderer.SetColor`），所以「悬停 = 原色 × 0.9608」。
        //   · `SpriteSwap` 那 630 个原版**换的是同一件 graphic 的图**（`m_SpriteState.m_HighlightedSprite`，
        //     **630/630 全非空**）⇒ 那一档要换图才叫对。**本版只做了统一的色偏兜底**，
        //     逐个换图的活**已记账**（判据 = 每个按钮的 `m_SpriteState`）——
        //     ⚠️ **这是一处如实标注的偏离**（见 `项目任务.md` §三 第 29 条 A15）。
        //   · `m_Transition = 0` 的 141 个原版**悬停什么都不变** ⇒ 那些地方我们这一层也不该变色，
        //     但**本地判不出「我们的哪一颗对应原版哪一颗」** ⇒ 留作上面那条账的一部分。
        //
        // ⚠️ **批处理下没有帧循环**（`Update` 不跑）⇒ 自检要看色偏就直调 `Enter/Exit/SetInstant`：
        //    `PointerLayer.HoverAt(...)` 在非 Play 时**直接落到目标色**（照 `ImageQuad` 那一族的规矩）。

        /// <summary>指针进入 / 离开时各调一次（原版就是 UGUI 的 `IPointerEnterHandler` / `IPointerExitHandler`）。</summary>
        public System.Action onEnter, onExit;

        /// <summary>原版 `m_Colors.m_HighlightedColor`（**全库同一个值**）。</summary>
        public const float HighlightK = 0.9607843f;
        /// <summary>原版 `m_Colors.m_PressedColor`。</summary>
        public const float PressedK = 0.7843137f;
        /// <summary>原版 `m_Colors.m_FadeDuration`（秒）。</summary>
        public const float FadeSeconds = 0.1f;

        /// <summary>关掉这一颗的色偏（原版 `m_Transition = 0` 的那 141 颗用得上）。</summary>
        public bool tintOnHover = true;

        /// <summary>🆕 **2026-10-06（A94）「吸收层」专用标志** —— 这一颗**不是按钮**，是
        /// 「窗内面板吃掉这一下」的等价物（原版面板那颗 `Image` 的 `m_RaycastTarget = 1`，
        /// 而它的父链上没有任何 `IPointerClickHandler` ⇒ 那一下**什么都不做**）。
        ///
        /// <para>为真时 <see cref="Enter"/> / <see cref="Exit"/> / <see cref="Press"/> / <see cref="Release"/>
        /// / <see cref="Click"/> **全部直接返回**，于是：</para>
        /// <list type="bullet">
        /// <item>**零视觉副作用** —— 不上悬停色偏、不换图、不进 `Pressed` 态（原版面板没有 `Selectable`、
        /// 没有任何悬停/按下变化）；</item>
        /// <item>**零告警** —— `Click` 不派发（也就不会撞上「没有绑动作」那条给「**忘了绑**」用的真告警）、
        /// `Press` 也不会报「按下无图可换」（`Bind` 从不被调用、两张图都是 null）。</item>
        /// </list>
        ///
        /// <para>🔴 **它也不进键盘导航**（原版面板不是 `Selectable` ⇒ 方向键永远不该停在它上面）——
        /// 四条遍历一起跳过：`PointerLayer` 的 `SelectFirst` / `FindInDirection` / `ButtonCountForTest`
        /// 与 `Select`（按在它身上 = 原版 `DeselectIfSelectionChanged` 把选中清成 null）。</para>
        ///
        /// <para>由 `MenuDraw.Absorb` 建出来时置位 —— ⛔ **别在别处用它**。</para></summary>
        public bool absorbOnly;

        // ============================================================ 🆕 2026-10-03 A17：悬停 / 按下【换图】
        //
        // 原版一颗 `Selectable` 只有**一种** transition（UGUI `DoStateTransition`）：
        //   `ColorTint` 的 505 颗 = 变暗（上面那套色偏就是它）· `SpriteSwap` 的 **630 颗 = 换图**。
        // ⇒ **换图那一档不该再叠色偏**（`Bind` 会自动把 `tintOnHover` 关掉，不然两种行为同时上）。
        // 判据正本 = `资料/普查产出_1003/按钮悬停图_普查.md`（12 条命名规律 + 5 块逐颗表）。

        /// <summary>要换图的那一层 —— 原版 `Selectable.m_TargetGraphic`。
        /// ⚠️ **它常常不是按钮自己那层**：原版 `Generic Close Button Orange` 指子件 `Background`、
        /// `WebShop Button` 指子件 `Button Image` ⇒ 由调用方传**真正画着常态图的那个 `ImageQuad`**。</summary>
        public ImageQuad target;
        Texture _normalTex, _hoverTex, _pressedTex;
        /// <summary>绑定时那张 quad 的宽高比 —— **换图后要拉回来**（见 `SwapTo` 的注释）。</summary>
        float _targetAspect;
        /// <summary>🆕 2026-10-05：本颗的「按下无图可换」告警**只说一次**（同 `TipHovers` 那种一次性出声）。</summary>
        bool _pressedSilent;

        /// <summary>常态图 → 高亮图。表里没有的走 `<常态图>_hover` 后备（普查 §一：绝大多数是这个规律）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> HoverNames =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "40K_settings_button",            "40K_settings_button_selected" },   // ⚠️ 不是 `_hover`
            { "40K_dropdown_field_closed",      "40K_dropdown_field_opened" },      // ⚠️ 不是 `_hover`
            { "UI_Button_Menu_Back",            "UI_Button_Menu_Back_Hover" },      // ⚠️ 大写 H
            { "UI_Button_Organe_Square_Normal", "UI_Button_Organe_Square_Hover" },  // ⚠️ 大写 H
        };

        /// <summary>常态图 → 按下图（**只有这几张存在**，其余退回高亮图；取不到按下图**不算缺图**）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> PressedNames =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "40K_button",            "40K_button_pressed" },
            { "40k_general_bt_yellow", "40k_general_bt_yellow_pressed" },
            { "40k_bt_close",          "40k_bt_close_pressed" },
            { "40k_menu_bt",           "40k_menu_bt_pressed" },
            { "UI_Button_Mulligan",    "UI_Button_Mulligan_Pressed" },    // ⚠️ 大写 P
            { "UI_Button_Menu_Back",   "UI_Button_Menu_Back_Pressed" },   // ⚠️ 大写 P
        };

        /// <summary>**取不到的悬停图**（红线：不许静默失败）—— 每扇窗的自检断它为空。</summary>
        public static readonly System.Collections.Generic.List<string> MissingSwapArt =
            new System.Collections.Generic.List<string>();

        /// <summary>🆕 **2026-10-05（A50②）：取不到的按下图** —— `MissingSwapArt` 的**镜像记录**。
        /// 🔴 **为什么不能像 hover 那样被断成空**：原版那一档来自 `m_SpriteState.m_PressedSprite`，
        /// 实测 `bundle_menus_assets_all` 里 **1276 个带 `m_SpriteState` 的 `Selectable`**
        /// （复现：`grep -rl m_SpriteState MonoBehaviour/ | wc -l`）：
        /// `m_Transition = 2`(SpriteSwap) **630 颗 —— 悬停图与按下图 630/630 都非空**；
        /// `= 1`(ColorTint) 495 颗 · `= 0`(None) 134 颗 —— **两张都是空**（另有 10 / 7 颗带图但那一档不吃）。
        /// **全 1276 颗逐颗核过：悬停图空 ⇔ 按下图空（0 处不一致）** ⇒ **我们取不到按下图时退回高亮图**
        /// （`Press()` 里 `_pressedTex ?? _hoverTex`）**这一档多数是合法的**（本类只按常态图名推名字）。
        /// ⇒ 这份表**只出声、不当缺点断**（要断它得先有「我们这一颗 → 原版哪一颗」的映射 = A15 那笔账）。
        /// **真缺口只有一种**：连高亮图也没有 ⇒ 按下**画面什么都不变**（`Press()` 里那条告警，不许静默）。</summary>
        public static readonly System.Collections.Generic.List<string> MissingPressedArt =
            new System.Collections.Generic.List<string>();

        static string HoverNameFor(string art)
            => string.IsNullOrEmpty(art) ? null
             : (HoverNames.TryGetValue(art, out var h) ? h : art + "_hover");
        static string PressedNameFor(string art)
            => string.IsNullOrEmpty(art) ? null
             : (PressedNames.TryGetValue(art, out var p) ? p : art + "_pressed");

        /// <summary>把这一颗接到「悬停换图」上。`art` = **常态图的图名**（查表/后备用），
        /// `hoverArt` / `pressedArt` = **逐颗显式覆盖** —— 同一张常态图在不同按钮上配不同高亮图时必须用，
        /// 例：卡组四圆钮常态图都是 `UI_Button_Round_background`，高亮分别是 `back` / `deck_change` / `eye` / `back`。
        /// 🔴 **取不到高亮图就记进 `MissingSwapArt`**（自检会红），**不静默画成没反应**。</summary>
        public void Bind(ImageQuad t, string art, string hoverArt = null, string pressedArt = null)
        {
            target = t;
            if (t == null) return;                 // 没画出来的件没有图可换（调用点负责如实说明）
            _normalTex = t.Texture;
            _targetAspect = t.WorldH > 0f ? t.WorldW / t.WorldH : 0f;
            string hn = hoverArt ?? HoverNameFor(art);
            string pn = pressedArt ?? PressedNameFor(art);
            _hoverTex = CardArt.MenuUi(hn);
            _pressedTex = CardArt.MenuUi(pn);
            if (_hoverTex == null && !MissingSwapArt.Contains(art + " → " + hn))
                MissingSwapArt.Add(art + " → " + hn);
            // 🆕 2026-10-05（A50②）：**按下图的镜像记录** —— 原来只有 hover 那一条，按下图缺了**永远静默**
            //   （`MissingPressedArt` 那张表全工程 0 命中）。口径与「能不能断言」见那张表的注释。
            if (_pressedTex == null && !MissingPressedArt.Contains(art + " → " + pn))
                MissingPressedArt.Add(art + " → " + pn);
            // 原版一颗只有一种 transition ⇒ 换图那一档不再叠色偏
            tintOnHover = false;
        }

        void SwapTo(Texture tex)
        {
            if (tex == null) return;
            SetOn(target, tex);
            if (_nine != null) for (int i = 0; i < _nine.Length; i++) SetOn(_nine[i], tex);
        }

        void SetOn(ImageQuad q, Texture tex)
        {
            if (q == null) return;
            q.SetTexture(tex);
            // 🔴 **换完图必须把宽高比拉回来** —— `ImageQuad.SetTexture` 会把 `_aspect` 冲成**贴图自己的**比值，
            //    而我们这套 quad 的矩形是**按原版矩形定的**（`MenuDraw.Rect` 里那次 `SetAspect`）。
            //    2026-10-03 实测：不拉回来 ⇒ 那颗 300px 的钮 `WorldW` 变成 **329**（`SettingsScene.CheckRectS` 抓到的；
            //    **屏幕形状没变、只有逻辑宽度被改掉**，肉眼看不出来）。
            //    同族先例（同一句纪律，两处早就写了）：`BattleLogPanel:532` · `AlliancesTab:189`。
            if (_targetAspect > 0f) q.SetAspect(_targetAspect);
        }

        /// <summary>自检用：把一棵树里**所有接了换图的**按钮逐个悬停一遍，返回「没换 / 没还原」的描述
        /// （空串 = 全过），`n` = 查了几颗。⚠️ 批处理**没有帧循环** ⇒ 这里直调 `Enter/Exit`
        /// —— 它们正是 `PointerLayer.HoverAt` 会调的那两个（`onEnter/onExit` 目前**无人挂**，
        /// 所以直调没有副作用；将来谁挂了钩子，这条要改成走 `PointerLayer.HoverAt`）。</summary>
        public static string AuditHoverSwap(Transform root, out int n)
        {
            n = 0;
            if (root == null) return "";
            var bad = new System.Text.StringBuilder();
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
            {
                if (wb == null || wb._hoverTex == null || wb.target == null) continue;
                n++;
                var before = wb.target.Texture;
                wb.Enter();
                if (wb.target.Texture != wb._hoverTex)
                    bad.Append("「").Append(wb.name).Append("」悬停**没换图**；");
                wb.Exit();
                if (wb.target.Texture != before)
                    bad.Append("「").Append(wb.name).Append("」离开**没还原**；");
            }
            return bad.ToString();
        }

        /// <summary>🆕 **2026-10-05（A50②）**：把一棵树里**能按下换图的**按钮逐个**按下 → 松开**一遍
        /// （`AuditHoverSwap` 的镜像）—— 返回「按下没换图 / 松开没还原」的描述（空串 = 全过），
        /// `n` = 查了几颗（**一张图都换不出的那几颗不计入** —— 它们在 `Press()` 里另出声）。
        /// 判据 = 原版 `m_SpriteState.m_PressedSprite`（`m_Transition = 2` 的 **630 颗 630/630 全非空**，
        /// 见 `MissingPressedArt` 的注释）；**没有按下图时退回高亮图**（`Press()` 里那行 `??`）。
        /// ⚠️ **批处理没有帧循环** ⇒ 直调 `Press/Release`（就是 `PointerLayer` 在按下/抬起时调的那两个；
        /// **`onClick` 不在它们身上派发** —— 那要「按下与抬起落在同一件上」由 `PointerLayer` 判，
        /// 所以这里按一圈**不会触发任何购买/开窗**）。
        /// ⚠️ 跑完这颗按钮回到**未按下**（`Release` 按 `Hovered` 还原成常态图）。
        /// ⚠️ 与 `AuditHoverSwap` 同一条口径：比的是**调用前那张图** ⇒ 顺手能抓到
        /// 「底色被别人换掉却没 `SetNormalTex`」（松开时还原成旧图）。</summary>
        public static string AuditPressedSwap(Transform root, out int n)
        {
            n = 0;
            if (root == null) return "";
            var bad = new System.Text.StringBuilder();
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
            {
                if (wb == null || wb.target == null) continue;
                var want = wb._pressedTex != null ? wb._pressedTex : wb._hoverTex;
                if (want == null) continue;                  // 两张都没有 = `Press()` 那一档（那边出声）
                n++;
                var before = wb.target.Texture;
                wb.Press();
                if (wb.target.Texture != want)
                    bad.Append("「").Append(wb.name).Append("」按下**没换图**；");
                wb.Release();
                if (wb.target.Texture != before)
                    bad.Append("「").Append(wb.name).Append("」松开**没还原**；");
            }
            return bad.ToString();
        }

        ImageQuad[] _nine;   // 九宫格那种「一颗按钮由 9 张小 quad 拼出来」的情形（见 BindNine）

        /// <summary>底色**由别人换掉之后**（例：设置窗页签选中态走 `SetTexture`）要调一次，
        /// 否则悬停退出时会把这一层恢复成**绑定时那一刻**的旧图。</summary>
        public void SetNormalTex(Texture t) { _normalTex = t; }

        /// <summary>九宫格按钮（`Image.Type = Sliced`）的换图 —— 原版换的是**同一个 `Image` 的 sprite**，
        /// 我们这边它被切成了 9 张小 quad ⇒ **每一张都要换**（只换中心那格 = 边框不跟着亮）。
        /// `40K_button` 与 `40K_button_hover` 实测**同尺寸 489×107**（2026-10-03 核对），uv 切分不变、只换纹理。</summary>
        public void BindNine(GameObject nine, string art, string hoverArt = null, string pressedArt = null)
        {
            if (nine == null) return;
            var qs = nine.GetComponentsInChildren<ImageQuad>();
            if (qs == null || qs.Length == 0) return;
            _nine = qs;
            Bind(qs[0], art, hoverArt, pressedArt);   // 常态图取第一张（九张同源）
        }

        /// <summary>便捷：**按钮自己就是那张图**时（`quad.gameObject.AddComponent&lt;WindowButton&gt;()` 那种写法）
        /// 直接绑自己身上那张，不必再传引用。</summary>
        public void BindSelf(string art, string hoverArt = null, string pressedArt = null)
            => Bind(GetComponent<ImageQuad>(), art, hoverArt, pressedArt);

        /// <summary>指针正压在这一颗上吗。</summary>
        public bool Hovered { get; private set; }
        /// <summary>左键正压在这一颗上吗（原版 `SelectionState.Pressed`）。</summary>
        public bool Pressed { get; private set; }

        ImageQuad[] _tq;
        Color[] _tqBase;
        float _k = 1f, _kTarget = 1f;

        /// <summary>指针进来（`PointerLayer` 唯一派发口）。</summary>
        public void Enter()
        {
            if (absorbOnly) return;         // 吸收层：原版面板没有任何悬停变化（见 `absorbOnly`）
            if (Hovered) return;
            Hovered = true;
            if (onEnter != null) onEnter();
            SetTarget(Hovered && Pressed ? PressedK : HighlightK);
            SwapTo(Pressed && _pressedTex != null ? _pressedTex : _hoverTex);
        }

        /// <summary>指针离开。</summary>
        public void Exit()
        {
            if (absorbOnly) return;         // 吸收层（与 `Enter` 配对）
            if (!Hovered) return;
            Hovered = false;
            if (onExit != null) onExit();
            SetTarget(Pressed ? PressedK : 1f);
            SwapTo(_normalTex);
        }

        /// <summary>左键按下（**不派发 `onClick`** —— 那只在「按下与抬起同一件」时才发生）。</summary>
        public void Press()
        {
            if (absorbOnly) return;         // 吸收层：进 `Pressed` 态会让整块面板变暗（原版没有这一档）
            Pressed = true;
            SetTarget(PressedK);
            // 🆕 2026-10-05（A50②）：**按下连一张图都换不动**时出声一次 —— `SwapTo(null)` 是**静默早退**，
            //   原来这一档一点痕迹都不留（`MissingSwapArt` 只记 hover 那一路）。红线：不许静默失败。
            if (_pressedTex == null && _hoverTex == null && !_pressedSilent)
            {
                _pressedSilent = true;      // 每颗只说一次（这颗每次按都会走到这里）
                Debug.LogWarning("[WindowButton] `" + name + "`：按下**一张图都换不出来**（常态图 `"
                                 + (_normalTex != null ? _normalTex.name : "<空>")
                                 + "` 的按下图/高亮图都取不到）⇒ 这一档**静默无效**；"
                                 + "原版那一档 = `m_SpriteState.m_PressedSprite`（`trans=2` 的 630 颗**全非空**）");
            }
            SwapTo(_pressedTex ?? _hoverTex);
            if (onDown != null) onDown();
        }

        /// <summary>左键抬起（不论抬在哪 —— 原版 `Pressed` 态在这一刻结束）。</summary>
        public void Release()
        {
            if (absorbOnly) return;         // 吸收层（与 `Press` 配对：那一边没进态，这一边也别退态）
            Pressed = false;
            SetTarget(Hovered ? HighlightK : 1f);
            SwapTo(Hovered ? _hoverTex : _normalTex);
            if (onUp != null) onUp();
        }

        public System.Action onDown, onUp;

        void SetTarget(float k)
        {
            if (!tintOnHover) return;
            _kTarget = k;
            if (!Application.isPlaying) { _k = k; ApplyTint(); }
        }

        /// <summary>色偏补间（照原版 `m_FadeDuration = 0.1s`）。
        /// ⚠️ 只在 Play 里跑得到（批处理下 `Update` 不执行）—— 自检看的是**目标色**，见类注释。</summary>
        void Update()
        {
            if (_k == _kTarget) return;
            _k = Mathf.MoveTowards(_k, _kTarget,
                                   Mathf.Max(0.0001f, Mathf.Abs(_kTarget - _k)) *
                                   Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.0001f, FadeSeconds)));
            if (Mathf.Abs(_k - _kTarget) < 0.001f) _k = _kTarget;
            ApplyTint();
        }

        /// <summary>这一颗名下**要跟着变色的 quad**。默认 = 自己这棵子树里的全部
        /// （直接挂在大图上的按钮就是这一档；`MenuDraw.Hit` 那种「另建一个透明命中区」的
        /// 子树里只有那个**透明** quad ⇒ 变色看不见 = 与原版 `m_Transition = 0` 等效）。</summary>
        void Collect()
        {
            if (_tq != null) return;
            _tq = GetComponentsInChildren<ImageQuad>(true);
            _tqBase = new Color[_tq.Length];
            for (int i = 0; i < _tq.Length; i++) _tqBase[i] = _tq[i] != null ? _tq[i].Tint : Color.white;
        }

        void ApplyTint()
        {
            Collect();
            for (int i = 0; i < _tq.Length; i++)
            {
                if (_tq[i] == null) continue;
                var b = _tqBase[i];
                _tq[i].SetTint(new Color(b.r * _k, b.g * _k, b.b * _k, b.a));
            }
        }

        /// <summary>自检用：当前色偏系数（1 = 原色 · 0.9608 = 悬停 · 0.7843 = 按下）。</summary>
        public float TintKForTest { get { return _k; } }

        /// <summary>自检用：**当前贴在 `target` 上的图**（换图那一档靠它验「悬停后确实换了 / 离开换回来了」）。</summary>
        public Texture CurrentTexForTest { get { return target != null ? target.Texture : null; } }
        /// <summary>自检用：绑定的常态图 / 高亮图 / 按下图（没接换图的三者都是 null）。</summary>
        public Texture NormalTexForTest { get { return _normalTex; } }
        public Texture HoverTexForTest { get { return _hoverTex; } }
        public Texture PressedTexForTest { get { return _pressedTex; } }

        // 🔴 **没有「登记表」**（2026-09-23 撤掉）：原来想用 `OnEnable/OnDisable` 维护一张静态表让指针层扫，
        //    但**自检跑在编辑模式**，而编辑模式下这两个回调**只对 `[ExecuteAlways]` 的脚本**才跑
        //    ⇒ 自检里表恒为空（实测「场景里 13 个 `WindowButton`、登记表 0 个」）。
        //    现在 `PointerLayer` **在真有输入事件时**才 `FindObjectsByType<WindowButton>()` 扫一遍
        //    （事件很少，代价可忽略），**不依赖任何生命周期回调**。

        // ============================================================ 🆕 2026-10-05 A65②：`disabled / soft-disable` 的【变灰】观感
        //
        // 🔴 **原版两条路落到同一个东西上 —— 把图形件的材质换成 `Everguild/UI/Greyscale`**：
        //   · `EverguildButton.SoftDisable(true)`（`DF:EverguildButton__SoftDisable.c`）：
        //     `softDisabled = true` → `SetToStateActiveOrDisabled(1, …)` → `SwitchMaterial` →
        //     `EverguildButtonHelper.DoMaterialRefresh(list, /*grey=*/false)` → 每颗
        //     `EverguildButtonMaterialModifier.ToogleGreyScale = true` → `GetModifiedMaterial` 返回
        //     `EverguildButtonHelper.get_DisabledMaterial()`（= **一份静态缓存**的 `new Material(Shader.Find(<Greyscale>))`）。
        //     ⚠️ **它【不】改 `interactable`** ⇒ 钮只是**变灰，照样点得动**（这就是 A65② 的题目）。
        //   · `Selectable.interactable = false`（`DoStateTransition(4 /*Disabled*/)`）走的是**同一条**：
        //     `DF:EverguildButton__DoStateTransition.c:93-104` 算出 `bVar4 = state != 4 && !softDisabled`
        //     再调 `SetToStateActiveOrDisabled(param_1, bVar4)` ⇒ 同一次 `DoMaterialRefresh`。
        //   · 🔴 **两条都还要一个前置**：`EverguildButton.colorTintGreyOnDisable`（字段 `0x17A`）为真才会真的灰
        //     （`SoftDisable` / `SwitchMaterial` 的第一句都是 `if (*(char *)(param_1 + 0x17a) != '\0')`）。
        //     **本工程用到变灰的那两颗，真包实读全是 1**：`Edit Deck`（`bundle_menus_assets_all` GO `Edit Deck` 上的
        //     EverguildButton MB `-8697463422759302744`，`grey=1 trans=2 custom=0`）· 12 颗 `Collect`
        //     （如每日行 MB `-4755074315463813377`，`grey=1 trans=2 custom=0`）。
        //
        // 🔴 **灰成什么样（逐像素）**：sprite 属性表 = `_MainTex` / `_Color` / `_GreyScale`（默认 **1.0**），
        //   Blend = `One / OneMinusSrcAlpha`、zWrite = 0（`OriginalShaderBlendTable` 也收了这一条：
        //   `{ "Everguild/UI/Greyscale", new[] { 1, 10, 0 } }`）；算式（DXBC 反汇编）
        //   `out.rgb = lerp(col, dot(col, (0.30,0.59,0.11)), _GreyScale)`，原版**一个属性都没写** ⇒ 纯亮度灰。
        //   ⚠️ **白色灰化后还是白的**：`dot((1,1,1),(0.30,0.59,0.11)) == 1.0`。
        //   ⇒ 我们**只换 `ImageQuad`**、**不动 `Label`**：我们这些钮的 `Button Text` 全是 `Color.white`，
        //     换与不换是同一个像素。**这一条是「照算式等价的省略」，不是漏做** —— 若将来某颗钮的文案不是白的，
        //     这里就得补（`Label.SetColor` 到 `grey(c)` 即可，仍是同一算式）。
        //
        // ⚠️ **一处如实标注的偏离（被迫）**：原版是**一份**静态 `disabledMaterial` 全按钮共用
        //   （uGUI 的贴图与颜色由 `CanvasRenderer` 逐件给，材质只管 shader 与混合）；
        //   我们的贴图/颜色**存在材质上**（`ImageQuad.SetTexture` 写 `sharedMaterial.mainTexture`、
        //   `SetTint` 写 `sharedMaterial.color`）⇒ **一份共享材质装不下多张贴图** ⇒ **每个 quad 一份**。
        //   shader、`_GreyScale`（不写 = 用默认 1.0）、混合状态**与原版逐项相同**，差的只是「几份材质对象」。

        /// <summary>原版那张灰化 shader 的**内部名**（出处：`EverguildButtonStateFollower` 的
        /// `private const string EVERGUILD_UI_GREYSCALE = "Everguild/UI/Greyscale"`；真包实读同一个串）。
        /// ⛔ **不许用 `Shader.Find`** —— 它在**包起来的构建里**找不到这张 shader（本工程的既定教训），
        /// 要走随包的 `wf_shaders.bundle`（`tools/gen_shader_blend.py` 就是从那个包里读出它的混合状态的）。</summary>
        public const string GrayShaderName = "Everguild/UI/Greyscale";

        /// <summary>**取不到 shader 的按钮名**（红线：不许静默失败）—— 每扇窗的自检断它为空。
        /// 取不到时**保持原样（不变灰）**，⛔ **不拿别的灰顶替**（铁律 3：那就变成「我们挑的观感」冒充原版）。</summary>
        public static readonly System.Collections.Generic.List<string> MissingGrayArt =
            new System.Collections.Generic.List<string>();

        static bool _saidNoGrayShader;

        /// <summary>`Everguild/UI/Greyscale` 这张原版 shader 现在**取不取得到**（自检用）。
        /// 取不到 ⇒ 所有变灰都会**静默失效**（只剩一条警告），所以这一条值得单独断。</summary>
        public static bool GrayShaderAvailable
        {
            get
            {
                UnityEngine.Shader sh;
                return WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(GrayShaderName, out sh) && sh != null;
            }
        }

        /// <summary>原版 `EverguildButton.softDisabled`（`0x198`）：**只变灰，照样点得动**。</summary>
        public bool SoftDisabled { get; private set; }

        bool _interactable = true;
        /// <summary>原版 `Selectable.interactable` 的最小等价物。置假 ⇒ ① 变灰（同 `EverguildButton`
        /// 在 `DoStateTransition(Disabled)` 里做的那一下）② **`Click()` 直接返回**（原版
        /// `Selectable.OnPointerClick` 头一句 `if (!IsActive() || !IsInteractable()) return;`）。
        /// ⚠️ **悬停那半边本地没有判据**（`Selectable.OnPointerEnter` 在 UGUI 包里，不在 `d:/2/tools/decomp_full/`）
        /// ⇒ 我们**保持现状**（不可交互的钮**照旧**吃悬停换图），**如实标注、不编**。</summary>
        public bool Interactable
        {
            get { return _interactable; }
            set { _interactable = value; RefreshGray(); }
        }

        /// <summary>原版 `EverguildButton.SoftDisable(bool)`。</summary>
        public void SetSoftDisabled(bool on) { SoftDisabled = on; RefreshGray(); }

        /// <summary>该灰了吗（原版那两条路在 `DoStateTransition` 里合成的那一个布尔：
        /// `bVar4 = state != 4 && !softDisabled`，取反即「灰」）。</summary>
        bool WantGray { get { return SoftDisabled || !_interactable; } }

        /// <summary>自检用：现在**真的**灰着吗（不是「想灰」—— 取不到 shader 时这里仍是 `false`）。</summary>
        public bool GrayedForTest { get; private set; }
        /// <summary>自检用：这一颗要一起变灰的 quad 数（`target` + 子树，去重）。</summary>
        public int GrayQuadCountForTest { get { return GrayTargets().Length; } }

        ImageQuad[] _graySavedQ;    // 上次变灰时的那一组（顺序与 `_graySaved` / `_grayMats` 对齐）
        Material[] _graySaved;      // 换灰之前每颗 quad 的材质（退出时**原样还回去**）
        Material[] _grayMats;       // 我们为每颗 quad 建的那一份

        /// <summary>要一起变灰的图形件。原版是 `GetComponentsInChildren&lt;Graphic&gt;(gameObject, includeInactive: 1)`
        /// （`DF:EverguildButtonHelper__GetGraphicsInChildren.c:33` 实读 `(gameObject, 1, …)`）——
        /// 我们这棵树里能画的东西只有 `ImageQuad`，**加上 `target`**：原版那颗 `Selectable.m_TargetGraphic`
        /// 本来就是按钮自己的图形件，在我们这里它常常是**兄弟**（`DeckInfoPopup.Hit` 那种「另建一个透明命中区、
        /// 真图在隔壁」的摆法）⇒ 只按子树灰会**一颗像素都不变**。⚠️ 取不到的件静默跳过（`Label` 见上面那段说明）。
        /// 🔴 **不缓存**（每次现算）：`Bind` 可能在挂上 `WindowButton` 之后再改 `target`，
        ///    缓存下来就会「按旧的一组灰、按旧的一组还原」（静默错一组），而这里总共只在状态切换时被调到。</summary>
        ImageQuad[] GrayTargets()
        {
            var kids = GetComponentsInChildren<ImageQuad>(true);
            var list = new System.Collections.Generic.List<ImageQuad>(kids.Length + 1);
            if (target != null) list.Add(target);
            for (int i = 0; i < kids.Length; i++)
                if (kids[i] != null && kids[i] != target) list.Add(kids[i]);
            return list.ToArray();
        }

        /// <summary>按 `WantGray` 摆材质。**只有这一份实现** —— `SoftDisabled` 与 `Interactable` 都走它。</summary>
        void RefreshGray()
        {
            bool want = WantGray;
            if (want == GrayedForTest) return;          // 没变就别重做（材质对象很贵）
            var qs = GrayTargets();
            if (want)
            {
                // 🔴 一组变了（长度不等）就重做那两张表 —— 只认 `_grayMats == null` 会在
                //    「先灰过、`Bind` 之后又灰」时**按下标越界**（自检里就是一条难查的 NRE）。
                if (_grayMats == null || _grayMats.Length != qs.Length)
                {
                    UnityEngine.Shader sh;
                    if (!WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(GrayShaderName, out sh) || sh == null)
                    {
                        // 红线：**不许静默失败**，也**不拿别的灰顶替**（那样就成了「我们挑的观感」冒充原版）。
                        if (!_saidNoGrayShader)
                        {
                            _saidNoGrayShader = true;
                            Debug.LogWarning("[Button] 取不到原版灰化 shader `" + GrayShaderName + "`"
                                             + "（随包 shader bundle 在不在？见 `资料/特效还原_进度与交接.md` §三）"
                                             + " ⇒ **这颗钮不变灰**（⛔ 不拿别的灰顶替 —— 铁律 3）。"
                                             + "`SoftDisable` / `interactable=false` 的**行为**照常生效。");
                        }
                        if (!MissingGrayArt.Contains(name)) MissingGrayArt.Add(name);
                        return;
                    }
                    _graySaved = new Material[qs.Length];
                    _grayMats = new Material[qs.Length];
                    for (int i = 0; i < qs.Length; i++)
                    {
                        if (qs[i] == null) continue;
                        var mr = qs[i].GetComponent<MeshRenderer>();
                        _graySaved[i] = mr != null ? mr.sharedMaterial : null;
                        // 原版那一份**一个属性都没写** ⇒ 我们也只带过去贴图与颜色（shader 自带 `_GreyScale = 1.0`）。
                        var m = new Material(sh);
                        m.name = "UI Greyscale (" + qs[i].name + ")";
                        m.color = qs[i].Tint;
                        _grayMats[i] = m;
                    }
                }
                for (int i = 0; i < qs.Length; i++)
                {
                    if (qs[i] == null || _grayMats[i] == null) continue;
                    // 🔴 每次应用前把颜色对齐到这一颗**当下**的 tint（`SetMaterial` 只带贴图、不带颜色）；
                    //    否则「同一颗按钮先灰过、换了一批 quad 又灰」会把上一颗的颜色带过来（静默偏色）。
                    _grayMats[i].color = qs[i].Tint;
                    qs[i].SetMaterial(_grayMats[i]);     // 它会把 quad 当前的贴图带过去
                }
                _graySavedQ = qs;                        // 🔴 记下**这一组**，退出时按同一组还回去
                GrayedForTest = true;
            }
            else
            {
                if (_graySaved == null) return;
                var back = _graySavedQ ?? qs;            // 按变灰那一刻那一组还原（不是按现在这一组）
                for (int i = 0; i < back.Length && i < _graySaved.Length; i++)
                {
                    if (back[i] == null || _graySaved[i] == null) continue;
                    back[i].SetMaterial(_graySaved[i]);
                }
                GrayedForTest = false;
            }
            Debug.Log("[Button] `" + name + "` " + (GrayedForTest ? "**变灰**" : "**还原**")
                      + "（原版 `EverguildButton.SoftDisable` / `interactable=false` 都落到"
                      + "「子树图形件的材质换成 `" + GrayShaderName + "`」这一下，见 `WindowButton` 头部那段）"
                      + "；`softDisabled = " + SoftDisabled + "` · `interactable = " + _interactable + "`");
        }

        /// <summary>自检用：把一棵树里**每一颗说自己是灰的**按钮逐个核一遍 —— 返回空串 = 全过，`n` = 查了几颗。
        /// 🔴 核的是**原版那张 shader 的名字**（`Everguild/UI/Greyscale`，出处见类头部），**不是我们自己的常量**
        /// ⇒ 不是自证；`GrayedForTest == true` 而材质没换 ⇒ 报红。
        /// ⚠️ 只查「自称灰了」的那些 —— 「该灰的没灰」（取不到 shader 那条路）由 `MissingGrayArt` 兜。</summary>
        public static string AuditGrayLook(Transform root, out int n)
        {
            n = 0;
            if (root == null) return "";
            var bad = new System.Text.StringBuilder();
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
            {
                if (wb == null || !wb.GrayedForTest) continue;
                n++;
                var qs = wb.GrayTargets();
                for (int i = 0; i < qs.Length; i++)
                {
                    if (qs[i] == null) continue;
                    var mr = qs[i].GetComponent<MeshRenderer>();
                    var m = mr != null ? mr.sharedMaterial : null;
                    if (m == null || m.shader == null || m.shader.name != GrayShaderName)
                    {
                        bad.Append("「").Append(wb.name).Append("」的 `").Append(qs[i].name)
                           .Append("` 自称灰了，材质却是 `")
                           .Append(m != null && m.shader != null ? m.shader.name : "<没有材质>").Append("`；");
                        break;
                    }
                }
            }
            return bad.ToString();
        }

        /// <summary>点一下 —— **`PointerLayer` 唯一的派发口**。
        /// 🔴 `interactable == false` 时**直接返回**（原版 `Selectable.OnPointerClick` 的第一句），
        /// 并且**出声**：原版那一刻玩家看得见「钮是灰的」，我们若连日志都不打，就成了静默失败。</summary>
        public void Click()
        {
            // 🔴 **吸收层（`MenuDraw.Absorb` 建的）在这里就结束**：这一下**被吃掉、什么都不做**
            //    （原版：面板那颗 `Image` 是射线落点、父链上没有点击处理器）。
            //    ⚠️ 必须挡在下面那条 `_interactable` 告警**之前** —— 否则每次点面板都误报一次。
            if (absorbOnly) return;
            if (!_interactable)
            {
                Debug.LogWarning("[Button] `" + name + "` **点了不生效** —— 原版这颗钮 `interactable = false`"
                                 + "（`Selectable.OnPointerClick` 头一句就返回）⇒ 连派发都没有。"
                                 + "⚠️ 悬停那半边我们**没有判据**（见 `Interactable` 的注释）⇒ 照旧吃悬停。");
                return;
            }
            if (onClick != null) onClick();
        }

        /// <summary>🔴 **老式的 `OnMouseUpAsButton`：在这个工程里一次也不会派发** ——
        /// ① 它要求同一物体上有 `Collider`，而本工程**零处**加过 collider；
        /// ② `ProjectSettings.asset:932 activeInputHandler = 1`（只用新 Input System）⇒ 老式 `OnMouseXxx` 不派发。
        /// 见 `项目任务.md` §三 第 15 条 **第 23 条**。**留着只为「原版是 UGUI 按钮」这条线索可查，
        /// 别再往它上面挂新逻辑**（新逻辑挂 `onClick`，由 `PointerLayer` 派发）。</summary>
        void OnMouseUpAsButton() { Click(); }

        /// <summary>自检用：批处理里没有输入事件，直调这条路（**和真点走同一个 `Click()`**）。</summary>
        public void ClickForTest() { Click(); }
    }
}
