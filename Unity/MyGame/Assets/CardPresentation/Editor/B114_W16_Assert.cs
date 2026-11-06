// B114_W16_Assert.cs — 第一百一十四会话 · 写手 `W-16`（2026-11-01）· 账 `B-249` 的断言
//
// ============================ 它盯的是什么 ============================
//  原版 `Avatar Drawer Shop Variant`（= `PlayerAvatar` 在 `DrawerOverride.Shop`(20) 那一档的 prefab）与基础档是
//  **同一个类 `AvatarDrawer`、两套 prefab**。我们原先一律按基础档几何画 ⇒ 这一档**整套**是欠账。
//  本件断的是「Shop 档的那 4 处几何 + 1 处显隐」，⛔ 不断「某个方法被调过」。
//
//  【判据（现行 build 现读）】`工具/menu_dump.py bundle_menus_assets_all "Avatar Drawer Shop Variant" --depth 4 --relative --no-sprite --no-ancestor-scale`
//    · 根 712.8×1080（ARF 0.66）· `Content` 712×1080（**与基础档逐值相同**）
//    · `Background` GameObject **`m_IsActive = 0`** ⇒ 原版这一档没有底板
//    · `avatar`（`image` 槽）锚 -0.475..1.52 × -0.175..1.35（相对 `avatarBG`）⇒ 设计 px (-337.8,-161.46)-(1082.64,777.33)
//    · `Army` 锚 (0,0.26)-(1,0.35) ⇒ 设计 px (0.4,702)-(712.4,799.2) · **`m_IsActive = 1`** · 白 · 字号 90
//    · `Label` 锚 (0,0.14)-(1,0.25) + sizeDelta.x 4 ⇒ 设计 px (-1.6,810)-(714.4,928.8)
//      `HorizontalLayoutGroup` pad 25 ⇒ `Label/Name` 设计 px (23.4,810)-(689.4,928.8)
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · S4（结论）断 `Army.activeSelf == true`：**删掉** `Dispatch` 那条 Shop 分流（或让 `ShopAvatarVariant` 永远返
//    `false`）⇒ 落回基础档 ⇒ `ItemDrawerAvatar.cs` 那句 `SetActive(false)` 把节点关掉 ⇒ **红**。
//  · S5/S6/S9（几何）：把 Shop 实现改回基础档那批常量 ⇒ 三条一起红。
//  · S7（不建 `Background`）：让 Shop 支仍调 `DrawerBackground` ⇒ 红。
//  · **D 组 = 结构上不可能同时满足的那一半**：同一个夹具、同一个 `cardArmy`、同一个 `ItemDrawerStyle`，
//    `DrawerOverride.Default` 那一档 **`Background` 在 + `Army` 关**，Shop 那一档 **`Background` 不在 + `Army` 开**
//    ⇒ 两档的 (底板在否, 阵营名开否) 是 (1,0) vs (0,1)，**互为反相**。谁把两档画成同一套，D2 必红。
//    （D 组同时锁住 `B-242` 的既有结论，⛔ 不是把 W-11 那条改回去。）
//
// ============================ 怎么接（⚠️ 由调度台统一追加，写手不碰宿主）============================
//  唯一入口 = `B114_W16_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行（`B114_W12_Assert...` 之后、
//  `Debug.Log(P + $"=== 合计` 之前）**同一物理行、第一个 `//` 之前**追加：
//      B114_W16_Assert.Run(); _sink.Pass += B114_W16_Assert.Pass; _sink.Fail += B114_W16_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · `ShopData.cs` 现在**没有** `PlayerAvatar` 商品 ⇒ 生产暂未触发；本件直接调 `ItemDrawer.Draw(..., Shop, ...)`
//    把这一档建出来（与 `ShopTabPage.cs:565` 生产那条调用同形）。
//  · 本件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-16`：账 `B-249` —— `Shop` 档 Avatar 变体（`Avatar Drawer Shop Variant`）的几何 + 阵营名显隐。</summary>
public static class B114_W16_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W16] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>抽屉根 = 712.8×1080（`AspectRatioFitter` 0.66）⇒ 夹具直接给这个框（`FrameOf` 的 `K = 1`）。</summary>
    static readonly PxRect BoxBase = new PxRect(0f, 0f, 712.8f, 1080f);
    /// <summary>头像图（`Resources/Art/avatars/`，与 `B114_C` / `B114_W11` 同一张 —— 那张是现成可取的）。</summary>
    const string ArtAvatar = "Avatar_AM_Attilan Rough Rider";
    /// <summary>夹具阵营 = `Ultramarines`(10)（`ArmyIcons.LocalizedName` 会给出非空串）。</summary>
    const int Army = 10;

    // ---- 期望值 = **独立的 prefab 常量**（⛔ 不读 `ItemDrawer` 里那批 Shop 常量，否则就是拿实现证明实现）----
    /// <summary>`Shop` 档 `avatar`（`image` 槽）：锚 -0.475..1.52 × -0.175..1.35（相对 `avatarBG` (0.4,54)-(712.4,669.6)）。</summary>
    static readonly PxRect ImgRect = new PxRect(-337.8f, -161.46f, 1082.64f, 777.33f);
    /// <summary>`Shop` 档 `Army`：锚 (0,0.26)-(1,0.35) of `Content` 712×1080。</summary>
    static readonly PxRect ArmyRect = new PxRect(0.4f, 702f, 712.4f, 799.2f);
    /// <summary>`Shop` 档 `Label`：锚 (0,0.14)-(1,0.25) + sizeDelta.x 4（左右各外扩 2）。</summary>
    static readonly PxRect LabelRect = new PxRect(-1.6f, 810f, 714.4f, 928.8f);
    /// <summary>`Shop` 档 `Label/Name`：`Label` 左右各收 `HorizontalLayoutGroup` 的 pad 25。</summary>
    static readonly PxRect NameRect = new PxRect(23.4f, 810f, 689.4f, 928.8f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W16（B-249）：`Shop` 档 Avatar 变体（`Avatar Drawer Shop Variant`）");
        SectionShop();
        SectionDefaultControl();
        Debug.Log(_sink.P + "B114W16 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ Shop 档
    static void SectionShop()
    {
        var tex = CardArt.Cosmetics(ArtAvatar);
        Tr(tex != null, "S0 **前提**：头像图 `" + ArtAvatar + "` 取得到（`CardArt.Cosmetics`）"
                      + "｜🧨 删掉 `Resources/Art/avatars/" + ArtAvatar + ".png` ⇒ 红"
                      + "（⛔ 不是 `if (图) Check(...)` 静默跳过 —— 这一条本身就是硬前提）");
        string armyName = ArmyIcons.LocalizedName(Army);
        Tr(!string.IsNullOrEmpty(armyName),
           "S0b **前提**：`ArmyIcons.LocalizedName(" + Army + ")` 非空（实得「" + (armyName ?? "<空>") + "」）"
         + "｜🧨 空串 ⇒ 阵营名压根建不出节点 ⇒ 下面「显示着」是空转");
        if (tex == null || string.IsNullOrEmpty(armyName)) return;

        var root = new GameObject("B114W16_AVATAR_SHOP");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);   // `QuantityPx 34 / NamePx 26` 都非 0 ⇒ 文字层**开着**
            var spec = new ItemSpec
            {
                Id = "B114W16_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic,
                Art = ArtAvatar, CardArmy = Army, Label = "B114W16",
            };
            res = ItemDrawer.Draw(root.transform, BoxBase, spec, 1, DrawerOverride.Shop, st);

            Tr(res.Node != null, "S1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerAvatar,
              "S1b **前提**：`ov = Shop` 仍路由到 `AvatarDrawer`（`ItemTypeSets` 的 Shop(20) 列写的就是它 —— 同一个类、两套 prefab）");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "S2 **前提**：`Content` 建出来了（这一档的 `Content` 与基础档逐值相同：712×1080）");

            // ---- 阵营名：这一档原版**真显示** ----
            var army = FindDeep(res.Node, ItemDrawer.NodeArmy);
            Tr(army != null, "S3 **前提**：`Army` 节点**建了**（prefab 里这颗存在且 `m_IsActive: 1`）");
            Tr(army != null && army.gameObject.activeSelf,
               "S4 ★★ **结论**：`Shop` 档的 `Army` **`activeSelf == true`** —— 与原版 `m_IsActive: 1` 同义"
             + "｜🧨 删掉 `Dispatch` 那条 `ov == Shop` 分流（落回基础档）⇒ `ItemDrawerAvatar.cs` 的 `SetActive(false)` 会把它关掉 ⇒ 红");
            if (army != null)
            {
                // ⚠️ 世界档 `Label` 挂在**纯 `Transform`** 上（TMP 在它的子节点，`Label.Create` 建的是
                //    `new GameObject(goName)`）⇒ 这里**没有 `RectTransform` 可量尺寸**，只量「框心」
                //    （= `MenuDraw.Local` 写进去的那个矩形的中心，与 `ApplyPxRect` 同一条换算）。
                CenterIs(NodeCenterPx(army), ArmyRect, 0.6f,
                       "S5 ★★ `Army` 框心 = (356.4,750.6)（= prefab 锚 (0,0.26)-(1,0.35) of 712×1080 的矩形中心）"
                     + "｜🧨 用回通用可见档那格 `DrArmyY1/Y2`（702..810 ⇒ 中心 756）或用基础档那格（中心 236.1）⇒ 红");

                // 🔴 2026-11-01 就地订正（铁律 5 · `B114_R9` 诊断 #2）：原来读 `TextMeshProUGUI` —— 但这条抽屉是
                //   **世界档**（`MenuDraw.cs:2052` → `Label.Create(ugui:false)` → `TmpFont.cs:414 NewText` → `TextMeshPro`）
                //   ⇒ 结构上取不到，该红是**断言写错**、不是文本错。读口改与 S5c 同一条 = `Label`（`Label.Text` / `Label.color`）。
                var lb = army.GetComponent<Label>();
                Tr(lb != null && lb.Text == armyName,
                   "S5b `Army` 的文本 = `ArmyIcons.LocalizedName(" + Army + ")` =「" + armyName + "」"
                 + "（实得「" + (lb != null ? lb.Text : "<没有 Label>") + "」）");
                // 🔴 字号**只能断 `fontSizeMax` 折算回 px**（`Label.FontSizeToPxNow` = 那条「跟原版 `m_fontSize` 比」的官方口）：
                //    `_tmp.fontSize` 在开了自适应之后是**二分出来的收敛值**，⛔ 不是作者设的那个数（判据 → `Label.FontSizeMax` 的 doc）。
                float armyMaxPx = FontPxMax(army);
                Tr(Mathf.Abs(armyMaxPx - 90f) < 0.5f,
                   "S5c `Army` 的字号上限 = **90 px**（这一档的 prefab `m_fontSize`/`m_fontSizeMax` 现读；基础档 / 通用可见档都是 75）"
                 + "（实得 " + armyMaxPx.ToString("F2") + " px）｜🧨 照基础档给 75 ⇒ 红");
                // 🔴 同上（诊断 #3）：⛔ 不再用 `new Color(-1,…)` 当「读不到」的兜底 —— 那只会把一个**读口错**
                //   伪装成「墨色错」。`lb == null` 就走失败分支并把原因印出来。
                Tr(lb != null && Mathf.Abs(lb.color.r - 1f) < 0.01f && Mathf.Abs(lb.color.g - 1f) < 0.01f
                   && Mathf.Abs(lb.color.b - 1f) < 0.01f && Mathf.Abs(lb.color.a - 1f) < 0.01f,
                   "S5d `Army` 的墨色 = **白 (1,1,1,1)**（这一档 prefab 的 `m_fontColor`；通用可见档那批是 "
                 + "(0.689,0.689,0.689,0.851) —— 照那份 ⇒ 红）"
                 + "（实得 " + (lb != null ? lb.color.ToString() : "<没有 Label>") + "）");
            }

            // ---- 主图：这一档最大的一处分叉 ----
            var img = FindDeep(res.Node, ItemDrawer.NodeImage);
            Tr(img != null, "S6a **前提**：主图节点建出来了");
            if (img != null)
                RectIs(QuadRectOf(img), FitBox(tex, ImgRect), 0.6f,
                       "S6 ★★ 主图框 = Fit(过扫 (-337.8,-161.46)-(1082.64,777.33)) —— 1420.44×938.79 那一格"
                     + "｜🧨 用回基础档 `AvatarImageFrame`（(-65,65.6)-(795.2,796.1)）⇒ 红");

            // ---- 底板：这一档**没有** ----
            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null,
               "S7 ★ `Shop` 档**不建** `Background`（`Avatar Drawer Shop Variant` 的 `Background` GameObject 是 `m_IsActive: 0`；"
             + "`ItemDrawer<T>.Draw` 全程不碰 `background` 那格 ⇒ 原版屏幕上没有那张底板）"
             + "｜🧨 让 Shop 支仍调 `DrawerBackground` ⇒ 红");

            // ---- 名字行：换框 + 换字号 ----
            var label = FindDeep(res.Node, "Label");
            Tr(label != null, "S8a `Label` 建出来了");
            if (label != null)
                RectIs(NodeRectOf(label), LabelRect, 0.6f,
                       "S8b `Label` 框 = (-1.6,810)-(714.4,928.8)（prefab 锚 (0,0.14)-(1,0.25) + sizeDelta.x 4）"
                     + "｜🧨 用回基础档那格 (36,810)-(676.8,918) ⇒ 红");
            var name = FindDeep(res.Node, ItemDrawer.NodeName);
            Tr(name != null, "S8c `Label/Name` 建出来了");
            if (name != null)
            {
                // ⚠️ `Name` 与 `Label` **同心**（(23.4+689.4)/2 = (61+651.8)/2 = 356.4，y 也同）⇒ 框心断不出差别；
                //    而世界档 `Label` 又没有 `RectTransform` 可量宽（见 S5 那条注释）。
                //    ⇒ 这一格用**字号**当判别量（基础档 75 / 这一档 90），宽度那半由 `Label` 那一格（S8b）+ 生产代码
                //    里「`Name` = `Label` 左右各收 pad 25」这一句共同锁住（`NameRect` 常量仍写在文件头，供复核）。
                float nameMaxPx = FontPxMax(name);
                Tr(Mathf.Abs(nameMaxPx - 90f) < 0.5f,
                   "S9 ★ `Name` 的字号上限 = **90 px**（这一档的 prefab `m_fontSize` 现读；基础档是 75；"
                 + "`Name` 的期望框 = (" + NameRect.x1 + "," + NameRect.y1 + ")-(" + NameRect.x2 + "," + NameRect.y2 + ")）"
                 + "（实得 " + nameMaxPx.ToString("F2") + " px）｜🧨 用回 `DrawerNameQuantity` 那套（75）⇒ 红");
            }

            // ---- 数量：`quantity` 槽 null（与基础档同）----
            Tr(FindDeep(res.Node, ItemDrawer.NodeQuantity) == null,
               "S10 `Quantity` 不建（这一档 `quantity` 槽 = null，与基础档同 ⇒ 永不画数量）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ 灭自证：两档互为反相
    static void SectionDefaultControl()
    {
        MenuCheck.Section(_sink, "D 灭自证：同一个夹具下，`Default` 档与 `Shop` 档的（底板在否, 阵营名开否）互为反相");

        var tex = CardArt.Cosmetics(ArtAvatar);
        if (tex == null) { Tr(false, "D0 **前提**：头像图取得到（与 S0 同一张 —— 这里必须在，否则整段是空转）"); return; }

        var root = new GameObject("B114W16_AVATAR_DEFAULT");
        ItemDrawResult res = default(ItemDrawResult);
        var rootShop = new GameObject("B114W16_AVATAR_SHOP2");
        ItemDrawResult resShop = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);        // 与 S 组**同一套** style
            var spec = new ItemSpec
            {
                Id = "B114W16_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic,
                Art = ArtAvatar, CardArmy = Army, Label = "B114W16",
            };
            res = ItemDrawer.Draw(root.transform, BoxBase, spec, 1, DrawerOverride.Default, st);
            resShop = ItemDrawer.Draw(rootShop.transform, BoxBase, spec, 1, DrawerOverride.Shop, st);

            Tr(res.Node != null && resShop.Node != null, "D1 **前提**：两档的根都建出来了");
            if (res.Node == null || resShop.Node == null) return;

            C(res.Drawer, ItemDrawer.DrawerAvatar, "D1b **前提**：`ov = Default` 路由到同一个抽屉类（两档只差 prefab）");

            var baseArmy = FindDeep(res.Node, ItemDrawer.NodeArmy);
            var shopArmy = FindDeep(resShop.Node, ItemDrawer.NodeArmy);
            bool baseBg = FindDeep(res.Node, ItemDrawer.NodeBackground) != null;
            bool shopBg = FindDeep(resShop.Node, ItemDrawer.NodeBackground) != null;
            bool baseArmyActive = baseArmy != null && baseArmy.gameObject.activeSelf;
            bool shopArmyActive = shopArmy != null && shopArmy.gameObject.activeSelf;

            Tr(baseArmy != null && !baseArmy.gameObject.activeSelf,
               "D2 ★ 基础档：`Army` 建了但 **`activeSelf == false`**（`Avatar Drawer` 的 `m_IsActive: 0` —— 即 `B-242` 的结论）"
             + "｜🧨 把 `ItemDrawerAvatar.cs` 那句 `SetActive(false)` 删掉 ⇒ 红");
            Tr(baseBg,
               "D3 ★ 基础档：**有** `Background`（`Avatar Drawer` 的 `Background` GameObject 是 `m_IsActive: 1`）");

            bool inv = (baseBg && !shopBg) && (!baseArmyActive && shopArmyActive);
            Tr(inv,
               "D4 ★★ **灭自证**：两档的（底板在否, 阵营名开否）= 基础档 **(有, 关)** vs `Shop` **(无, 开)** —— **互为反相**"
             + "｜🧨 谁把两档画成同一套（Shop 也画底板 / 或 Shop 的阵营名也关掉）⇒ 这条与 S4/S7 一起红"
             + "（实得：基础 (" + baseBg + "," + baseArmyActive + ") · Shop (" + shopBg + "," + shopArmyActive + ")）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            if (resShop.View != null) resShop.View.Release();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(rootShop);
        }
    }

    // ============================================================ 量具
    /// <summary>按名字递归找（`Transform.Find` 只找**直接子件** —— `CLAUDE.md` §三 卡根那层 `2DCard` 的教训）。</summary>
    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var t = FindDeep(root.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }

    /// <summary>主图那类件是 `ImageQuad` ⇒ 走全仓唯一那份 `MenuDraw.QuadRectPx`（设计 px · 左上原点 · y 向下）。</summary>
    static PxRect QuadRectOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>(true) : null;
        return q != null ? MenuDraw.QuadRectPx(q) : new PxRect();
    }

    /// <summary>文字 / 纯容器那颗自己的矩形（画布 px · 左上原点 · y 向下）。
    /// 尺寸项取 `rect.rect`（拉伸锚点下 `sizeDelta` 表达的是「相对父框的内缩量」）；
    /// 位置项走 `PixelOfDesign(PosInDesignSpace(t))`（= `MenuDraw.Local` 的逆）—— 与 `Editor/C5_Assert.cs` 的
    /// `NodeRect` 同一条量法（⛔ 不复用第三份公式）。</summary>
    static PxRect NodeRectOf(Transform t)
    {
        var rt = t as RectTransform;
        if (rt == null) return new PxRect();
        var r = rt.rect;
        Vector2 c = MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(rt));
        const float K = 108f;                        // = `LayoutSpace.DesignPxH / DesignHeight`
        float hw = r.width * 0.5f * K, hh = r.height * 0.5f * K;
        return new PxRect(c.x - hw, c.y - hh, c.x + hw, c.y + hh);
    }

    /// <summary>一个节点的**框心**（画布 px · 左上原点 · y 向下）。
    /// 位置项走 `PixelOfDesign(PosInDesignSpace(t))` —— 与 <see cref="NodeRectOf"/> / `Editor/C5_Assert.cs` 同一条换算；
    /// 世界档 `Label` 挂在**纯 `Transform`** 上（没有 `RectTransform`）⇒ 那一族只能量框心。
    static Vector2 NodeCenterPx(Transform t)
    {
        return t != null ? MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(t)) : new Vector2(-99999f, -99999f);
    }

    /// <summary>框心比对（<paramref name="want"/> 只用来取中心）。</summary>
    static void CenterIs(Vector2 got, PxRect want, float tol, string msg)
    {
        bool ok = Mathf.Abs(got.x - want.CX) <= tol && Mathf.Abs(got.y - want.CY) <= tol;
        Tr(ok, msg + "｜实得 (" + got.x.ToString("F2") + "," + got.y.ToString("F2") + ")");
    }

    /// <summary>一颗文字节点的**字号上限折算成画布 px**（`Label.FontSizeToPxNow` = 官方那条「跟原版 `m_fontSize` 比」的口；
    /// ⛔ 别读 `_tmp.fontSize` —— 开了自适应之后那是二分收敛值，不是作者设的数）。
    /// 🔴 **必须带 `ActualFace`**（`B150` #9）：2026-11-05 `MenuDraw.Text` 的缺省字体面已是 `LatinCardBody`，
    /// 而单实参重载恒按 `Face.Cjk` 折算 ⇒ 拉丁面标签**恒差 0.853 倍**（`Battle/Label.cs:1059-1066` 那条 doc 点名的坑）。
    /// 返回 **−1** = 这个节点上没有 `Label`（或点阵后端）。</summary>
    static float FontPxMax(Transform t)
    {
        var lb = t != null ? t.GetComponent<Label>() : null;
        return lb != null ? lb.FontSizeToPxNow(lb.FontSizeMax, lb.ActualFace) : -1f;
    }

    /// <summary>`MenuDraw.Rect(..., keepAspect: true)` 之后**画出来**的矩形（= uGUI `PreserveSpriteAspectRatio`）。
    /// 直接用生产代码那条 `CardbackFace.Fit` ⇒ 期望值只差「喂进去的框」，而那个框是**独立的 prefab 常量**。</summary>
    static PxRect FitBox(Texture2D tex, PxRect box)
    {
        float dw, dh, dx, dyUp;
        if (tex == null || !CardbackFace.Fit(tex, box.W, box.H, out dw, out dh, out dx, out dyUp)) return box;
        float cx = box.CX + dx, cy = box.CY - dyUp;     // `Fit` 的 dy 是**向上为正**；本仓 `PxRect` 是 y 向下
        return new PxRect(cx - dw * 0.5f, cy - dh * 0.5f, cx + dw * 0.5f, cy + dh * 0.5f);
    }

    static void RectIs(PxRect got, PxRect want, float tol, string msg)
    {
        bool ok = Mathf.Abs(got.x1 - want.x1) <= tol && Mathf.Abs(got.y1 - want.y1) <= tol
               && Mathf.Abs(got.x2 - want.x2) <= tol && Mathf.Abs(got.y2 - want.y2) <= tol;
        Tr(ok, msg + "｜实得 (" + got.x1.ToString("F2") + "," + got.y1.ToString("F2") + ")-("
             + got.x2.ToString("F2") + "," + got.y2.ToString("F2") + ") " + got.W.ToString("F2") + "×"
             + got.H.ToString("F2"));
    }
}
