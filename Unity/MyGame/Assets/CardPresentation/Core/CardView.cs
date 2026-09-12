// CardView.cs — 一张卡的视觉载体
//
// **分层**，和原版一样：
//
//     ┌─ info  ── 数值 / 名字 / 关键词（我们画的，透明底）        z 最小 = 最前
//     ├─ frame ── 卡框（原版 PNG，`Resources/Art/cards/frame_<阵营>.png`）
//     └─ art   ── 美术窗口里的画（现在是占位图，换成你自己的立绘）
//
// 没有卡框 PNG 时**退回单层程序生成的黑卡**（`CardArt.Available == false`）——
// 所以删掉 `Resources/Art/` 整个目录，游戏照样跑，只是回到占位美术。
//
// 数值位置是**照原版卡框量的**（见下面 `xxxAt` 常量）：近战/远程在左下、生命在右下、
// 费用在右上。别挪 —— 手牌是「左卡压右卡」，露出来的正好是每张牌的右半边。
//
// 文字分两条路（2026-09-12 起）：
//   · **卡名 / 关键词 → TextMeshPro**（世界空间那版），中文才画得出来。和原版同构 ——
//     原版卡预制体上挂的就是 `NameTextUnit` / `DescTextUnit` 这些 TMP 组件，不是烘进贴图的。
//   · **数值 / 费用 → 仍是自写 5×7 点阵**（`TextCanvas`）。就几个数字，点阵够用，
//     而且不依赖字体资产。**拿不到 TMP 字体资产时卡名也退回点阵**（只画得出 ASCII 英文）。
// 见 `Core/TmpFont.cs`（字体从哪来）和 `Core/CardText.cs`（中文文案表）。
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一张卡要显示的东西。由 `BattleDriver` 从规则引擎的卡定义转过来。</summary>
    public struct CardData
    {
        public string id;
        public string title;
        public int cost;
        public int melee;        // 近战攻击力
        public int ranged;       // 远程攻击力（0 = 没有）
        public int health;
        public int armor;        // 护甲（原版卡面右侧那面盾。0 = 没有）
        public string keywords;  // 已格式化的关键词串，直接画在卡面上
        public bool isUnit;      // 单位还是战术（战术不上战场）
        public Color frame;      // 阵营色 —— 占位卡面的边框、费用框用它
        public string faction;   // 阵营 key，用来找 `Art/cards/frame_<faction>[_tierN].png`
        /// <summary>稀有度（`common` / `rare` / `epic` / `legendary` / `special`）——
        /// **卡框按稀有度分四档**（原版就是），空的按 common 处理。见 `CardArt.Frame`。</summary>
        public string rarity;

        public static CardData Simple(string title, int cost, int melee, int health)
        {
            return new CardData
            {
                id = title, title = title, cost = cost,
                melee = melee, ranged = 0, health = health, armor = 0,
                keywords = "", isUnit = true, frame = new Color(0.55f, 0.55f, 0.62f),
                faction = null,
            };
        }

        /// <summary>布局自检（`CardBaseDemo`）用的假卡 —— 跟真数据无关，只填满版面。
        /// `id` 借几张**有原版立绘**的卡名，这样演示截图里能看到真立绘。</summary>
        public static CardData Placeholder(int i)
        {
            var colors = new[]
            {
                new Color(0.78f, 0.24f, 0.22f),   // 红
                new Color(0.22f, 0.45f, 0.78f),   // 蓝
                new Color(0.28f, 0.60f, 0.32f),   // 绿
                new Color(0.72f, 0.62f, 0.20f),   // 金
                new Color(0.52f, 0.30f, 0.66f),   // 紫
            };
            string[] ids = { "Scavenger", "Wave Rider", "Ironclad", "Siren", "War Drake", "Leviathan" };
            return new CardData
            {
                id = ids[i % ids.Length],
                title = "CARD " + (i + 1),
                cost = (i % 7) + 1,
                melee = (i * 3) % 9 + 1,
                ranged = (i % 4 == 0) ? 2 : 0,
                health = (i * 5) % 9 + 1,
                keywords = (i % 3 == 0) ? "VANGUARD" : "",
                isUnit = true,
                frame = colors[i % colors.Length],
                faction = (i % 2 == 0) ? "Ember" : "Tide",
            };
        }
    }

    /// <summary>
    /// 卡面左下那两颗攻击数值格 —— 「这个单位能被选中」的底光点亮哪一颗。
    /// （原版 `Base Attack Counters` 下就两个 `Highlight`：近战一个、远程一个，**没有技能那一档**。）
    /// </summary>
    public enum TargetGem { None, Melee, Ranged }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CardView : MonoBehaviour
    {
        // 设计尺寸（世界单位）—— 卡牌是 1 : 1.4 的竖卡
        // ⚠️ **卡本体**尺寸，出处：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` 的
        //    `2DCard (GO 1075, RT 2876, size 2.0927×3.3313)` —— **卡单位**（不是 px）。
        //    为什么用它而不是 `CardFrame` 的 2.2452×3.2572：后者的 rect 虽然更宽，但**它左右各约 18%
        //    是透明留白**（框的金属实宽只有 ~1.33–1.42 卡单位），而且**铺到卡片左右边缘的是立绘**。
        //    「卡本体」这个值有两个独立来源交叉验证 ——
        //      · `2.0927 × 0.36 × 182.14 = 137.2 px`、`3.3313 × 0.36 × 182.14 = 218.4 px`
        //        （k=182.14 px/单位，见 `审查更正清单_0827.md:136`）—— 正是战场上量到的卡尺寸；
        //      · 同上 ×0.73 → 手牌 165×263 px。
        //    （2026-09-12 改：原来是我们自己挑的 1.45×2.03，比例 0.714，比原版的 0.628 宽 13.7%，
        //      表现为「卡又矮又胖」，连带着所有卡面元素的位置全偏。）
        public const float Width = 2.0927f;
        public const float Height = 3.3313f;

        // 贴图尺寸（像素）。比例 256:371 = 0.690，**跟着原版卡框外框走**（实测 647:936 = 0.691），
        // 这样卡框铺满整块 quad 时不会被拉变形
        // 贴图缓冲的宽高比要**跟着卡本体走**（256 / 0.6282 = 407）——
        // 原来是 256×371（0.690，按卡框比例配的），画到卡本体上会被横向压扁。
        const int FaceW = 256, FaceH = 407;

        // ---- 原版卡框上各元素的位置（在「卡外框」归一化坐标里量的，见资料/原版复刻_场景与美术.md）----
        // x 从左边 0 到右边 1，y 从**顶部** 0 到底部 1
        // ⚠️ **下面这 7 个位置全部来自原版 JSON**（`子代理读报_2dcard_0827.md` A2 表），不是量的截图。
        //    换算：卡本体 W=2.0927、H=3.3313，中心为原点，
        //          x01 = 0.5 + px/W，y01 = 0.5 − py/H（y01 从**顶部**数）。
        //    原版那 7 个 container 的 (x,y)（卡单位）：
        //      Cost (0.788,+0.666)  Armour (0.899,−0.991)  Melee (−0.836,−1.19)
        //      Range (−0.595,−1.40) Health (0.74,−1.36)    Rarity (0,−1.458)
        //    文字块 `Name and description` 1.3×0.68 @(0,−0.7745) → 纵向 [0.631, 0.834]，
        //    卡名取它上缘、效果文字取中段（原版子节点的 pos 只有零点几 px、等于没偏移，
        //    实际排版靠运行时布局，所以只能取这个块的上下缘 —— **这两条是我们挑的**）。
        static readonly Vector2 CostAt   = new Vector2(0.8766f, 0.3001f);   // 右上的费用宝石（原版在卡上部 30%）
        static readonly Vector2 NameAt   = new Vector2(0.5000f, 0.6450f);   // 名字块上缘
        static readonly Vector2 KwAt     = new Vector2(0.5000f, 0.7450f);   // 效果文字（块中段）
        static readonly Vector2 MeleeAt  = new Vector2(0.1005f, 0.8572f);   // 左下红圆
        static readonly Vector2 RangedAt = new Vector2(0.2157f, 0.9203f);   // 左下偏右的紫圆
        static readonly Vector2 HealthAt = new Vector2(0.8536f, 0.9083f);   // 右下绿
        static readonly Vector2 ArmourAt = new Vector2(0.9296f, 0.7975f);   // 右侧盾牌（原来**根本没画**）

        // ---- 卡上各层在原版里的**真实尺寸**（单位是「卡单位」，卡本体 = 2.0927 × 3.3313）----
        // 出处：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` A2 全树规格表。
        // ⚠️ **它们不是同一块矩形**：卡框比卡本体**宽**（2.2452 vs 2.0927）、比卡本体**矮**
        //    （3.2572 vs 3.3313），而且还偏下 0.03；Front 则比卡本体高。
        //    所以每一层都要按**自己的**矩形画 —— 把各自的 bbox 拉满整张卡是错的
        //    （我们原来就这么干：卡框被横向压扁 7.3%，框上的宝石跟着内移，数值就落到宝石外面了）。
        // ⚠️ **2026-09-12 更正**：原来这里写「框比卡宽所以两侧塔楼探出去」——**不成立**。
        //    卡框 PNG 的**不透明实宽只有 ~608–647 / 1024 px（≈1.33–1.42 卡单位）**，比卡本体还窄；
        //    rect 左右那 ~18% 是**透明留白**。**真正铺到卡片左右边缘的是立绘**，不是卡框。
        const float CardUnitW = 2.0927f, CardUnitH = 3.3313f;
        const float FrameUnitW = 2.2452f, FrameUnitH = 3.2572f, FrameUnitY = -0.03f;

        // 占位卡面（没有原版卡框时）的**插图位**——和 `FaceTexture` 里那个 ③ 号框是**同一处**，
        // 归一化是 x[0.06,0.94] y[0.20,0.58]（y 从顶算）。⚠️ 改一处要两处一起改。
        // 换算到卡单位（卡本体 2.0927×3.3313 居中）：宽 0.88×卡宽、高 0.38×卡高、中心 y=+0.11×卡高。
        const float WellW = 0.88f * CardUnitW;
        const float WellH = 0.38f * CardUnitH;
        const float WellY = 0.11f * CardUnitH;

        // 卡面**底部中央那颗稀有度宝石**（原版 `Rarity` 节点，A2 表：0.4 × 0.4 @(0,−1.458) 卡单位）
        const float RarityGemW = 0.4f, RarityGemH = 0.4f, RarityGemY = -1.458f;

        // ---- 立绘：原版怎么装的，我们怎么跟 ----
        // 原版的立绘矩形（预制体 JSON `RectTransform_2364194910465924032`，战斗预制体）：
        //   2.7484 × 2.7484 @ (0, −0.065)，Image `m_PreserveAspect=0`
        //   —— **比卡本体大**，是**故意**要外溢的；由卡框上的 SoftMask（`MaskArea`=立绘的 RT、
        //   `FlipAlphaMask=1`）拿**卡框的 alpha** 把它切掉。
        //   出处：`子代理读报_2dcard_0827.md:310`「卡图 2.7484² 外溢…外溢部分被框贴图 Alpha 掩掉」
        //   + `CardImage_-4679357769477678144.json` / `CardFrame_7138976253573503936.json` 的组件字段。
        // 立绘图本身：纹理 1024²、sprite `textureRect = 670.5 × 1024`（宽高比 0.6548，卡本体 0.628，
        //   卡框矩形 0.689）—— 出处 `Sprite/SM_UM_inf_Aggressor Sergeant.json` 的 `m_RD.textureRect`
        //   （⚠️ 这个字段**旧解包没有**，见 `资料/资源使用手册.md` 顶部那条更正）。
        //
        // ⚠️ **2026-09-12 改**：原来这里有一组 `WinMinX/MaxX/MinY/MaxY` 常量 ——「对卡框 alpha 做膨胀 +
        //    flood fill 量出闭合透明区」当成立绘的裁剪框。那个区是**拱形**，拿它当**矩形**框用有两个后果：
        //    立绘的直角**顶出拱形**（症状就是「卡框没包住插图」），而且窗口以外根本不画立绘。
        //    现在的做法（见 `ArtMesh`）：**立绘铺满卡框那块矩形** + 卡框画在上面 ——
        //    卡框自己就是那个遮罩（不透明处盖住立绘、透空处露出立绘），和原版等价、少一层 mask。

        /// <summary>TMP 文字层的 z。比 `_info`(-0.02) 再靠前一点 —— **z 越小离相机越近**</summary>
        const float TextZ = -0.03f;

        public CardData Data { get; private set; }

        /// <summary>自检用：卡框那一层现在用的是哪张贴图。
        /// **截图看不出「稀有度分档对不对」**（四档都是同一个形状的框），只能这么断言。</summary>
        public Texture2D FrameTexture
        {
            get
            {
                return (_frame != null && _frame.sharedMaterial != null)
                     ? _frame.sharedMaterial.mainTexture as Texture2D : null;
            }
        }

        /// <summary>自检用：立绘那一层现在用的是哪张贴图（同上，截图看不出用的是不是真插图）。</summary>
        public Texture2D ArtLayerTexture
        {
            get
            {
                return (_art != null && _art.sharedMaterial != null)
                     ? _art.sharedMaterial.mainTexture as Texture2D : null;
            }
        }

        /// <summary>自检用：底部那颗稀有度宝石用的是哪张图（颜色差别小，截图不好断言）。</summary>
        public Texture2D GemTexture
        {
            get
            {
                return (_gem != null && _gem.sharedMaterial != null)
                     ? _gem.sharedMaterial.mainTexture as Texture2D : null;
            }
        }

        MeshRenderer _face;      // 没有卡框时的整张卡面
        MeshRenderer _frame;     // 原版卡框
        MeshRenderer _art;       // 立绘占位
        MeshRenderer _gem;       // 卡面底部那颗稀有度宝石（原版 `Rarity`）
        MeshRenderer _info;      // 数值层
        MeshRenderer _rim;       // 状态描边
        TextMeshPro _title;      // 卡名（TMP。没有字体资产时为 null，字烘在 _info 里）
        TextMeshPro _keywords;   // 关键词（同上）
        static Material _quadMat;

        readonly List<MeshRenderer> _layers = new List<MeshRenderer>();

        /// <summary>造一张卡。parent 传手牌容器或战场容器。</summary>
        public static CardView Create(Transform parent, CardData d, string name = null)
        {
            var go = new GameObject(name ?? d.title);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<CardView>();
            v.Build(d);
            return v;
        }

        /// <summary>换卡面（同一张卡换数据时用，比如手牌换牌）。</summary>
        public void SetData(CardData d)
        {
            Data = d;
            if (_face != null) _face.sharedMaterial = FaceMaterial(d);
            if (_info != null)
            {
                _info.sharedMaterial.mainTexture = InfoTexture(d);
                if (_art != null) _art.sharedMaterial.mainTexture = ArtTexture(d);
            }

            // TMP 那两层跟数据走（名字/关键词可能整条换掉）。
            // ⚠️ **原地更新，不要销毁重建** —— 这条路在「战场每刷新一次」时都会走
            //    （`BattleDriver.cs:600`，掉血/疲劳都要反映到卡面），而 Play 模式下
            //    `Destroy` 要等帧末，重建的话**那一帧新旧两份字会叠在一起**。
            BuildTextLayers(d);
        }

        void Build(CardData d)
        {
            Data = d;

            var mf = GetComponent<MeshFilter>();
            mf.sharedMesh = Quad();

            CardArt.Load();
            // 战术卡用**另一套框**（原版 troop / stratagem 分开）—— `isUnit=false` 就是战术卡
            var frameTex = CardArt.Frame(d.faction, d.rarity, !d.isUnit);

            if (frameTex != null)
            {
                // ---- 三层：立绘（后）→ 卡框（中）→ 数值（前）----
                // 根节点自带的那个 MeshRenderer 用不上 —— 不关掉它会拿默认材质渲出一块品红
                GetComponent<MeshRenderer>().enabled = false;
                var artTex = ArtTexture(d);
                _art = AddLayer("art", frameTex, 0.03f, artTex,
                                ArtMesh(artTex, FrameUnitW, FrameUnitH, FrameUnitY));
                _frame = AddLayer("frame", frameTex, 0f, null);
                _frame.GetComponent<MeshFilter>().sharedMesh = FrameMesh(frameTex);   // UV 裁到卡外框
                _info = AddLayer("info", frameTex, -0.02f, InfoTexture(d));
                // 稀有度宝石：叠在卡框那颗**暗色凹槽**上（原版 `Rarity` 节点）。
                // 卡框分档改的是框的形制，**光靠它看不出稀有度** —— 颜色在这颗宝石上。
                // 图取不到就不画（不静默失败：卡框和数值照常）。
                var gemTex = RarityGem(d.rarity);
                if (gemTex != null) _gem = AddLayer("gem", gemTex, -0.01f, gemTex, GemMesh());
            }
            else
            {
                // ---- 没有原版卡框：退回单层占位卡面 ----
                // ⚠️ 这条路**以前不画立绘**（原版插图白导进来了）—— 9 个没导卡框的阵营
                //    在卡组编辑器里就是一片空白。现在补上：立绘画在**插图位**，
                //    占位卡面那块是透明的（见 `FaceTexture` ③），立绘正好从那里露出来。
                _face = GetComponent<MeshRenderer>();
                _face.sharedMaterial = FaceMaterial(d);
                _layers.Add(_face);
                var wellArt = ArtTexture(d);
                _art = AddLayer("art", null, 0.03f, wellArt, ArtMesh(wellArt, WellW, WellH, WellY));
            }

            // 状态描边：比卡面大一圈、贴在后面（z 大一点 = 更远），默认关掉
            var rimGo = new GameObject("rim");
            rimGo.transform.SetParent(transform, false);
            rimGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            rimGo.transform.localScale = new Vector3(1.09f, 1.06f, 1f);
            rimGo.AddComponent<MeshFilter>().sharedMesh = Quad();
            _rim = rimGo.AddComponent<MeshRenderer>();
            _rim.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = Color.clear };
            _rim.enabled = false;

            // 卡名/关键词走 TMP（拿不到字体资产时它自己会跳过，字仍旧烘在 _info 里）
            BuildTextLayers(d);
        }

        /// <summary>
        /// 卡名 / 关键词挂 TMP。**和原版同构** —— 原版卡预制体上就是 `NameTextUnit` /
        /// `DescTextUnit` 这些 TMP 组件，运行时由 `CardTextsController.SetTexts()` 灌字，
        /// 不是烘在卡面贴图里的。走这条路中文才画得出来。
        /// 拿不到字体资产（`TmpFont.Available == false`）就**不挂**，
        /// 由 `InfoTexture` 里那两条 `if` 用点阵字库顶上（只画得出 ASCII）。
        /// </summary>
        void BuildTextLayers(CardData d)
        {
            if (!TmpFont.Available) return;

            // ① 卡名：字号来自原版实测（见 `TitleFontSize`），太长再回缩进版面
            _title = Fill(_title, "title", d.title, NameAt, 0.86f * Width, TitleFontSize, Ink, false);

            // ② 关键词：名字下面，超宽折行
            _keywords = Fill(_keywords, "keywords", d.keywords, KwAt, 0.80f * Width, KeywordFontSize, InkDim, true);
        }

        /// <summary>
        /// 建或**原地更新**一层 TMP 文字，返回它（文字为空时返回 null 并把旧的清掉）。
        /// 原地更新是必须的 —— 这条路径每次战场刷新都会走，见 `SetData` 的注释。
        /// </summary>
        TextMeshPro Fill(TextMeshPro t, string name, string text, Vector2 at, float maxWidth,
                         float fontSize, Color32 baseColor, bool wrap)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (t != null)
                {
                    t.text = "";                     // 先清字再销毁：Play 模式下 Destroy 要等帧末，
                    TmpFont.Kill(t.gameObject);      // 不清的话那一帧还留着上一张卡的名字
                }
                return null;
            }

            if (t == null) t = TmpFont.NewText(transform, name, "", fontSize, baseColor);

            t.text = text;
            t.fontSize = fontSize;                   // `FitToWidth` 会改字号，每次得先重置回基准
            t.color = (Color)baseColor * _tint;      // 高亮/置灰的状态色要跟着走，不然刷新一次就白了
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

            if (wrap) TmpFont.SetWrapWidth(t, maxWidth);
            else FitToWidth(t, maxWidth);

            PlaceAt(t, at, TextZ);
            return t;
        }

        // 字号（= 一个 em 有多高）对卡高的比值。**出处是原版，不是我们挑的**：
        //   原版卡预制体 `08_预制体特效/战斗预制体/` 上的 `NameTextUnit`：
        //     `m_fontSize = 19`、`m_LocalScale = (0.01, 0.01, 0.01)`  → em = 19 × 0.01 = 0.19 世界单位
        //     卡面高 `2DCard.m_SizeDelta.y = 3.3313`（自它往上到根 `CardPrefab` 的 scale 全是 1）
        //   → 0.19 ÷ 3.3313 = **5.70%**
        //   （场景那份 `battlearena1` 里链上多一层 `CardUI` scale 150，对分子分母同时生效、约掉。）
        // ⚠️ 这是**字号（em）**对卡高，不是文字框对卡高 —— 文字框是 29.7308 × 0.01 = 0.2973，
        //    比值 8.92%，那是留给卡名的**版面空间**，不是字实际多大。
        // ⚠️ 原版这句开着 `m_enableAutoSizing`（`m_fontSizeMax` = 19）—— 名字**长**的时候会自动缩，
        //    所以 5.70% 是**上限**。我们这边没做自动缩放，只有 `FitToWidth` 的固定回缩。
        const float TitleEmOfCard = 0.0570f;

        /// <summary>卡名的字号。汉字约占 1 em，所以「em 高度」直接喂给 `FontSizeForGlyphHeight`</summary>
        public static float TitleFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(Height * TitleEmOfCard); }
        }

        /// <summary>
        /// 关键词的字号。⚠️ **这条没有原版出处**：原版对应的是 `DescTextUnit`
        /// （`m_fontSize = 23.55`、`m_fontSizeMax = 24`、同样开自动缩放），
        /// 那个 23.55 是**请求值不是渲染值** —— 描述一长就自动缩下去，
        /// **从解包资源里读不出它实际多大**。所以这里沿用原来点阵字的大小
        /// （7 像素 × 缩放 2 = 卡高的 3.75%），**属于「我们挑的」，不是原版的做法**。
        /// </summary>
        public static float KeywordFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(7f * 2f / FaceH * Height); }
        }

        /// <summary>
        /// 把整块文字**居中**摆到卡面归一化坐标 (x 从左、y 从**顶部**) 上。
        /// 用 `textBounds` 反推而不是「把原点放上去」：TMP 的原点在**第一行的基线**上，
        /// 折成两行之后整块会往下长、盖住底下的宝石（而且行数还不固定）。
        /// </summary>
        static void PlaceAt(TextMeshPro t, Vector2 at, float z)
        {
            if (t == null) return;
            t.ForceMeshUpdate();                 // 批处理没有帧循环，尺寸得手动推出来
            var center = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, z);
            t.rectTransform.localPosition = center - (Vector3)t.textBounds.center;
        }

        /// <summary>太宽就把字号整体回缩到装得下（原来点阵那条路也是「缩号到放得下」）</summary>
        static void FitToWidth(TextMeshPro t, float maxWidth)
        {
            if (t == null) return;
            t.ForceMeshUpdate();
            float w = t.textBounds.size.x;
            if (w <= maxWidth || w <= 0f) return;
            t.fontSize *= maxWidth / w;
            t.ForceMeshUpdate();
        }

        MeshRenderer AddLayer(string name, Texture2D basis, float z, Texture2D tex, Mesh mesh = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh != null ? mesh : Quad();
            var mr = go.AddComponent<MeshRenderer>();
            // ⚠️ 每层一份**新材质**：直接用共享材质再改 mainTexture 会让所有卡共用同一张贴图（踩过）
            mr.sharedMaterial = new Material(BaseMaterial()) { mainTexture = tex != null ? tex : basis };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _layers.Add(mr);
            return mr;
        }

        /// <summary>`Sprites/Default`：吃 alpha + 有 `_Color` 可以着色（模板，用的时候要 `new Material`）</summary>
        static Material BaseMaterial()
        {
            if (_quadMat == null) _quadMat = new Material(Shader.Find("Sprites/Default"));
            return _quadMat;
        }

        /// <summary>当前的状态色。`SetData` 原地重建 TMP 文字时要把它乘回去</summary>
        Color _tint = Color.white;

        /// <summary>卡的底色（高亮态改它）。1 = 原色，0.55 = 置灰不可打出</summary>
        public void SetTint(Color c)
        {
            _tint = c;
            foreach (var r in _layers)
                if (r != null && r.sharedMaterial != null) r.sharedMaterial.color = c;

            // TMP 的字**不吃材质的 `_Color`**（它用顶点色），得单独乘一遍 ——
            // 不乘的话卡都置灰了、卡名还是亮的，状态读不出来
            if (_title != null) _title.color = (Color)Ink * c;
            if (_keywords != null) _keywords.color = (Color)InkDim * c;
        }

        /// <summary>当前状态色（状态机在 CardInteraction 那边，这里只负责显示）</summary>
        public CardHighlightState State { get; private set; }

        /// <summary>状态描边（卡后面那圈）。
        ///
        /// 为什么不用「整卡着色」表达状态：`material.color` 是**乘法** ——
        /// 红卡染「置灰」出来还是暗红，状态根本读不出来（试过）。
        /// 描边是独立一层，不受卡面配色影响，也是真实卡牌游戏的通行做法。</summary>
        public void SetHighlight(CardHighlightState s)
        {
            State = s;
            SetTint(CardHighlight.ColorOf(s));
            if (_rim != null)
            {
                bool show = s != CardHighlightState.Normal;
                _rim.enabled = show;
                if (show)
                {
                    var c = CardHighlight.ColorOf(s);
                    c.a = 0.95f;
                    _rim.sharedMaterial.color = c;
                }
            }
        }

        static readonly Color TintNormal = Color.white;
        public void ResetTint() { SetTint(TintNormal); }

        // ==================================================================
        //  合法目标的底光（原版 `Highlight` / `Highlight ranged`）
        // ==================================================================

        /// <summary>底光直径 ÷ 卡宽。算式见 `SetTargetGem`</summary>
        const float GlowOfCard = 1.13f / 2.09f;      // ≈ 0.54

        /// <summary>底光的 z：夹在卡框（0）和数值层（-0.02）之间 —— 即**画在数字后面**</summary>
        const float GlowZ = -0.01f;

        ImageQuad _glowMelee, _glowRanged;
        bool _glowTried;

        /// <summary>
        /// 点亮某颗攻击数值格的底光 —— 「这个单位**能被选中**」。
        ///
        /// 出处：原版卡预制体 `Base Attack Counters/{Melee,Range} Attack Container/` 下各挂一个
        /// `Highlight`（图 `40K_melee_glow`，红）/ `Highlight ranged`（图 `40K_ranged_glow`，紫），
        /// 3D 版 `HighlightAttackType.highlightObject` 指向它们。
        /// ⚠️ **锚点是钉死的**（四条独立证据）：它在**卡体那颗攻击数值格上**，与数字同点同心、
        ///    `SortingOrder` 比数字小（所以画在数字**后面**）、旋转 identity 且等比缩放。
        ///    **不是**单位脚下的地板（同 prefab 里真贴地的东西必须翻 -90°X），也**不是** 2D 印刷卡面
        ///    那颗（那条链是 RectTransform + CanvasRenderer，而且运行时 32 个 UI 版容器
        ///    `highlightObject` 全是 0 —— 根本没接光圈）。
        ///
        /// 尺寸：sprite 128 px ÷ PPU 100 = 1.28（原版卡体空间）× `m_LocalScale` 0.655 = **0.84**；
        /// 点亮瞬间再 × `scaleModifierOnHighLight` **1.35** = 1.13。卡体宽 `Card 3D` = 2.09
        /// → **1.13 / 2.09 = 54% 卡宽**。
        ///
        /// ⚠️ **技能没有对应的那一档** —— 原版就两个 `Highlight`（近战/远程），所以放技能时不点亮底光，
        ///    靠准星和弧线的金色来表达。
        ///
        /// 没有图（删掉美术目录）时什么都不做 —— 和 `CardArt.Available` 一个路子。
        /// </summary>
        public void SetTargetGem(TargetGem gem)
        {
            CurrentGem = gem;
            if (gem != TargetGem.None) EnsureGlow();
            if (_glowMelee != null) _glowMelee.gameObject.SetActive(gem == TargetGem.Melee);
            if (_glowRanged != null) _glowRanged.gameObject.SetActive(gem == TargetGem.Ranged);
        }

        /// <summary>现在点亮的是哪颗数值格（自检断言用 —— 截图上看不出「点亮的是不是该亮的那颗」）</summary>
        public TargetGem CurrentGem { get; private set; }

        void EnsureGlow()
        {
            if (_glowTried) return;
            _glowTried = true;
            float dia = Width * GlowOfCard;
            _glowMelee = MakeGlow("40K_melee_glow", MeleeAt, dia, "glow_melee");
            _glowRanged = MakeGlow("40K_ranged_glow", RangedAt, dia, "glow_ranged");
        }

        ImageQuad MakeGlow(string artName, Vector2 at, float dia, string name)
        {
            var q = ImageQuad.Create(transform, CardArt.Ui(artName), Vector3.zero, dia,
                                     new Vector2(0.5f, 0.5f), name);
            if (q == null) return null;
            // `at` 是卡面归一化坐标（x 从左、y 从**顶部**），换成卡的局部坐标
            q.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, GlowZ);
            q.gameObject.SetActive(false);
            return q;
        }

        /// <summary>世界坐标在不在这张卡上。**用卡自己的局部坐标判** —— 扇形里每张卡都带旋转，
        /// 拿屏幕空间的包围盒判会错。</summary>
        public bool Contains(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= Width * 0.5f && Mathf.Abs(l.y) <= Height * 0.5f;
        }

        /// <summary>摆位：位置(世界) + 绕 Z 的倾角 + 缩放。手牌扇形/战场落位都调它。</summary>
        public void SetPose(Vector3 pos, float rotZ, float scale)
        {
            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            transform.localScale = Vector3.one * scale;
        }

        // ==================================================================
        //  卡框的 UV 裁剪
        // ==================================================================

        // 原版卡框 PNG 是 1024×1024，**卡本体只占中间一块**，四周是透明留白。
        // 直接铺满 quad 的话卡会缩成一小块还偏 —— 所以要量出不透明包围盒，
        // 把 UV 裁到那一块。量一次按贴图缓存。
        static readonly Dictionary<Texture2D, Rect> _uvCache = new Dictionary<Texture2D, Rect>();
        static readonly Dictionary<Texture2D, Mesh> _meshCache = new Dictionary<Texture2D, Mesh>();

        /// <summary>卡外框在贴图里的 UV 矩形 (uMin, vMin, uMax, vMax)。量不出来的话退回整张贴图。</summary>
        public static Rect FrameUv(Texture2D tex)
        {
            Rect r;
            if (_uvCache.TryGetValue(tex, out r)) return r;

            int W = tex.width, H = tex.height;
            Color32[] px = null;
            try { px = tex.GetPixels32(); }
            catch { /* 贴图没开 Read/Write —— 见 ArtBaker.ApplyImportSettings */ }

            if (px == null)
            {
                r = new Rect(0f, 0f, 1f, 1f);
            }
            else
            {
                // ⚠️ `GetPixels32` 的 row 0 在**图的下边**，而 Unity 的 UV v=0 也在下边 ——
                //    两边一致，所以这里不用翻，直接按数组坐标算 UV。
                int x0 = W, x1 = -1, y0 = H, y1 = -1;
                for (int y = 0; y < H; y++)
                {
                    int row = y * W;
                    for (int x = 0; x < W; x++)
                    {
                        if (px[row + x].a <= 8) continue;
                        if (x < x0) x0 = x;
                        if (x > x1) x1 = x;
                        if (y < y0) y0 = y;
                        if (y > y1) y1 = y;
                    }
                }
                r = (x1 < x0 || y1 < y0)
                  ? new Rect(0f, 0f, 1f, 1f)
                  : new Rect((x0 + 0.5f) / W, (y0 + 0.5f) / H,
                             (x1 - x0 + 1) / (float)W, (y1 - y0 + 1) / (float)H);
            }

            _uvCache[tex] = r;
            return r;
        }

        static readonly Dictionary<int, Mesh> _artMeshCache = new Dictionary<int, Mesh>();

        /// <summary>
        /// 立绘那一层的网格：**铺满指定的那块矩形**，UV 走「cover」——
        /// 立绘按自己的比例放大到刚好盖住，多出来的部分**靠 UV 裁掉，不拉伸**。
        /// 缓存按「立绘宽高比 ×1000 + 矩形档位」做键。
        ///
        /// 有原版卡框时传**卡框那块矩形**（`FrameUnitW/H/Y`）：原版立绘是**故意画得比卡还大**、
        /// 由卡框的 alpha 遮罩切掉的（`子代理读报_2dcard_0827.md:310`）。卡框自己是拱形的窗口，
        /// 拿那个窗口当**矩形**裁剪框会让立绘的直角顶出拱形（2026-09-12 之前就是这么错的）。
        /// 改成铺满卡框之后，**卡框自己就是那个遮罩**（不透明处盖住立绘、透空处露出立绘），
        /// 等价、且少一层 mask。
        ///
        /// 没有原版卡框时传**占位卡面的插图位**（`WellW/H/Y`），那条路以前**根本不画立绘**。
        ///
        /// 实测数据：插图 sprite 是 **671×1024**（宽高比 0.6553），卡框矩形是 **0.689** ——
        /// cover 会纵向裁掉约 5%，横向刚好铺满。出处见 `CardUnitW` 那一段注释。
        /// </summary>
        static Mesh ArtMesh(Texture2D art, float rectW, float rectH, float rectY)
        {
            int key = art != null && art.height > 0
                    ? Mathf.RoundToInt(art.width / (float)art.height * 1000f) : 0;
            int slot = Mathf.Abs(rectW - FrameUnitW) < 0.001f ? 0 : 1;   // 0=卡框矩形 1=占位插图位
            int cacheKey = key * 10 + slot;
            Mesh cached;
            if (_artMeshCache.TryGetValue(cacheKey, out cached) && cached != null) return cached;

            float s = Width / CardUnitW;                       // 卡单位 → 世界
            float w = rectW * s, h = rectH * s;
            float cx = 0f, cy = rectY * s;

            float uw = 1f, uh = 1f;
            if (key > 0)
            {
                float artAspect = key / 1000f, rectAspect = rectW / rectH;
                if (artAspect > rectAspect) uw = rectAspect / artAspect;   // 立绘更宽 → 横向裁掉两边
                else                        uh = artAspect / rectAspect;   // 立绘更高 → 纵向裁掉上下
            }
            float u0 = (1f - uw) * 0.5f, v0 = (1f - uh) * 0.5f;

            var m = new Mesh { name = "CardArtQuad" };
            m.vertices = new[]
            {
                new Vector3(cx - w * 0.5f, cy - h * 0.5f, 0f), new Vector3(cx + w * 0.5f, cy - h * 0.5f, 0f),
                new Vector3(cx + w * 0.5f, cy + h * 0.5f, 0f), new Vector3(cx - w * 0.5f, cy + h * 0.5f, 0f),
            };
            m.uv = new[]
            {
                new Vector2(u0, v0), new Vector2(u0 + uw, v0),
                new Vector2(u0 + uw, v0 + uh), new Vector2(u0, v0 + uh),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _artMeshCache[key] = m;
            return m;
        }

        /// <summary>
        /// 卡面**底部中央那颗菱形宝石**的网格（原版 `Rarity` 节点）。
        /// 出处：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` A2 表
        /// —— `Rarity` **0.4 × 0.4 @ (0, −1.458)**（卡单位）。
        ///
        /// ⚠️ **它和「卡框按稀有度分档」是两件事**：卡框分档改的是**框的形制**（tier1 素 → tier4 华丽），
        ///    而卡框纹理上那颗菱形是个**空的暗色凹槽**（四档都一样）；稀有度还要靠**另外叠一张彩色宝石**
        ///    才看得出来（common 浅蓝 / rare 绿 / epic 紫 / legendary 金 / special 橙红）。
        ///    对照图：`资料/留档_排查证据/卡面组装_0912/gems.png`（上排原版卡面的宝石 / 下排我们四档框的凹槽）。
        /// </summary>
        static Mesh GemMesh()
        {
            if (_gemMesh != null) return _gemMesh;
            float s = Width / CardUnitW;
            float w = RarityGemW * s * 0.5f, h = RarityGemH * s * 0.5f;
            float cy = RarityGemY * s;
            _gemMesh = new Mesh { name = "CardRarityGemQuad" };
            _gemMesh.vertices = new[]
            {
                new Vector3(-w, -h + cy, 0f), new Vector3(w, -h + cy, 0f),
                new Vector3(w, h + cy, 0f),   new Vector3(-w, h + cy, 0f),
            };
            _gemMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            _gemMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _gemMesh.RecalculateBounds();
            return _gemMesh;
        }
        static Mesh _gemMesh;

        /// <summary>稀有度宝石的贴图名（图在 `Resources/Art/ui_deck/`）。
        /// 取不到就返回 null —— 那颗宝石不画，卡面其余部分照常。</summary>
        static Texture2D RarityGem(string rarity)
        {
            string r;
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "rare":      r = "rare";      break;
                case "epic":      r = "epic";      break;
                case "legendary": r = "legendary"; break;
                case "special":   r = "special";   break;
                default:          r = "common";    break;   // common / 空 / 不认识
            }
            string idx = r == "rare" ? "2" : r == "epic" ? "3" : r == "legendary" ? "4" : r == "special" ? "5" : "1";
            return CardArt.DeckUi(idx + "_40k_cardframe_rarity_" + r);
        }

        static Mesh FrameMesh(Texture2D tex)
        {
            Mesh m;
            if (_meshCache.TryGetValue(tex, out m)) return m;

            var uv = FrameUv(tex);
            float s = Width / CardUnitW;                       // 卡单位 → 世界

            // ⚠️ 用**卡框自己的**尺寸（2.2452×3.2572 @y−0.03 卡单位），不是卡本体的。
            // ⚠️ **按 sprite 自身的比例 fit 进去，不拉伸**（2026-09-12 改）：
            //    卡框 PNG 的金属 bbox 是 608×936（宽高比 0.6496），而 rect 是 0.689 ——
            //    直接拉满会把框横向撑宽 6%。fit 出来是 **2.1159 × 3.2572**，宽度正好落在
            //    卡本体（2.0927）那条线上，和原版卡面量出来的「金属基本顶到卡边」一致
            //    （原版卡面实测金属横跨卡宽的 0～98%）。
            //    出处：预制体 JSON `RectTransform_-3192049446263720900` 给了 rect 2.2452×3.2572，
            //    但 sprite 带左右透明留白，所以**画的时候要按 sprite 的实宽**。
            float spriteW = Mathf.Max(1f, uv.width * tex.width);
            float spriteH = Mathf.Max(1f, uv.height * tex.height);
            float fit = Mathf.Min(FrameUnitW / spriteW, FrameUnitH / spriteH);
            float w = spriteW * fit * s * 0.5f, h = spriteH * fit * s * 0.5f, oy = FrameUnitY * s;
            m = new Mesh { name = "CardFrameQuad" };
            m.vertices = new[]
            {
                new Vector3(-w, -h + oy, 0f), new Vector3(w, -h + oy, 0f),
                new Vector3(w, h + oy, 0f),   new Vector3(-w, h + oy, 0f),
            };
            m.uv = new[]
            {
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin),
                new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _meshCache[tex] = m;
            return m;
        }

        // ==================================================================
        //  程序生成的卡面：整张卡（没有原版卡框时）/ 数值层（有卡框时）
        // ==================================================================

        static readonly Color32 Ink       = new Color32(240, 240, 245, 255);   // 主文字：白
        static readonly Color32 InkDim    = new Color32(180, 182, 195, 255);   // 次文字：灰
        static readonly Color32 InkMelee  = new Color32(255, 226, 150, 255);   // 近战：暖黄（红宝石上）
        static readonly Color32 InkRanged = new Color32(205, 175, 255, 255);   // 远程：淡紫（紫宝石上）
        static readonly Color32 InkHealth = new Color32(190, 255, 200, 255);   // 生命：淡绿（绿宝石上）
        // 护甲：冷白（原版那面盾是深灰钢色，白字最清楚。**这是我们挑的颜色**，原版数值色是纯白）
        static readonly Color32 InkArmour = new Color32(228, 238, 248, 255);
        static readonly Color32 InkCost   = new Color32(255, 255, 255, 255);   // 费用：白（蓝宝石上）
        static readonly Color32 InkCostGem= new Color32(38, 132, 214, 255);    // 费用宝石底：原版就是蓝的
        static readonly Color32 BgCard    = new Color32(10, 10, 14, 255);      // 占位卡面：近黑
        static readonly Color32 BgWell    = new Color32(20, 20, 26, 255);      // 占位卡面：插图位
        static readonly Color32 BgArt     = new Color32(26, 26, 34, 255);      // 立绘占位底色

        static Material FaceMaterial(CardData d)
        {
            // `Sprites/Default`：吃 alpha（圆角透明处才不会被画成黑块）+ 有 `_Color` 可以着色
            //（Unlit/Texture 两样都不行 —— 高亮/置灰全靠这个 _Color）
            if (_quadMat == null) _quadMat = new Material(Shader.Find("Sprites/Default"));
            var m = new Material(_quadMat);
            m.mainTexture = FaceTexture(d);
            return m;
        }

        // ⚠️ 缓存键必须带**所有画进贴图的字段** —— 只按阵营色做键的话，
        //    同色不同数值的卡会共用同一张脸（踩过）。
        static readonly Dictionary<string, Texture2D> FaceCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> InfoCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> ArtCache  = new Dictionary<string, Texture2D>();

        static string CacheKey(CardData d)
        {
            return $"{ColorUtility.ToHtmlStringRGBA(d.frame)}|{d.title}|{d.cost}|{d.melee}|{d.ranged}|{d.health}|{d.armor}|{d.keywords}|{d.isUnit}";
        }

        /// <summary>数值层（透明底）：费用 + 卡名 + 关键词 + 三个数值</summary>
        static Texture2D InfoTexture(CardData d)
        {
            string key = "info|" + CacheKey(d);
            Texture2D cached;
            if (InfoCache.TryGetValue(key, out cached) && cached != null) return cached;

            var px = Blank(FaceW, FaceH);

            // ① 费用：右上角徽记的位置，画一个**蓝色六边形**（原版的费用宝石就是蓝的）+ 白数字
            if (d.cost >= 0)
            {
                // 徽记本身花纹很花，所以要用**不透明的宝石**盖住它 —— 半透明/阵营色都读不出来
                var rim = new Color32(14, 14, 18, 255);
                // 半径：原版费用格是 0.4×0.4 卡单位 → 0.2/2.0927 = 0.0956 卡宽
                FillHexagon(px, FaceW, FaceH, CostAt.x * FaceW, CostAt.y * FaceH, 0.0956f * FaceW, rim);
                FillHexagon(px, FaceW, FaceH, CostAt.x * FaceW, CostAt.y * FaceH, 0.0779f * FaceW, InkCostGem);
                DrawCenteredAt(px, CostAt, d.cost.ToString(), 4, InkCost);
            }

            // ①b 文字区底下垫一层**柔和压暗** —— 原版卡面的卡名/效果文字是直接压在立绘上的
            //     （`Name and description` 那个块没有自己的底图），立绘亮的时候字就看不清。
            //     ⚠️ **这层压暗是我们加的**，不是原版的节点 —— 原版靠的是立绘本身在那一带偏暗。
            //     范围取文字块 `1.3×0.68 @(0,−0.7745)` 换算后的那一条（y01 0.60~0.85），
            //     两端用渐变淡出，免得在卡面上留一条硬边。
            DrawTextScrim(px, FaceW, FaceH, 0.60f, 0.85f, 0.08f, 0.92f, 120);

            // ② 卡名：立绘中段，过长自动缩号
            //    ⚠️ **有 TMP 字体资产时这里不画** —— 卡名由 `BuildTextLayers` 挂的
            //       `TextMeshPro` 出（中文只有那条路画得出来）。两条路不能同时画，会重影。
            if (!TmpFont.Available && !string.IsNullOrEmpty(d.title))
            {
                int scale = 4;
                int maxW = (int)(0.86f * FaceW);
                while (scale > 1 && TextCanvas.Measure(d.title, scale) > maxW) scale--;
                int h = TextCanvas.LineHeight(scale);
                TextCanvas.DrawCentered(px, FaceW, FaceH, d.title, scale, NameAt.x,
                                        Mathf.RoundToInt(NameAt.y * FaceH - h * 0.5f), Ink);
            }

            // ③ 关键词：名字下面折行（同上，有 TMP 就交给 TMP）
            if (!TmpFont.Available && !string.IsNullOrEmpty(d.keywords))
            {
                int h = TextCanvas.LineHeight(2);
                TextCanvas.DrawWrapped(px, FaceW, FaceH, d.keywords, 2,
                                       Mathf.RoundToInt(0.10f * FaceW),
                                       Mathf.RoundToInt(KwAt.y * FaceH - h * 0.5f),
                                       Mathf.RoundToInt(0.80f * FaceW), 3, InkDim);
            }

            // ④ 四个数值：画在卡框的宝石上（左下红/紫、右侧盾、右下绿）
            DrawStatAt(px, MeleeAt, d.melee, InkMelee);
            if (d.ranged > 0) DrawStatAt(px, RangedAt, d.ranged, InkRanged);
            if (d.armor > 0) DrawStatAt(px, ArmourAt, d.armor, InkArmour);   // 原版这面盾一直画（0 就不画）
            DrawStatAt(px, HealthAt, d.health, InkHealth);

            return BakeInfo(key, px);
        }

        static Texture2D BakeInfo(string key, Color32[] px)
        {
            var tex = new Texture2D(FaceW, FaceH, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            InfoCache[key] = tex;
            return tex;
        }

        /// <summary>立绘：优先用原版立绘（`Art/cards/art_<卡名>.png`），没有再退回程序生成的占位图。
        /// 换自己的美术就是把同名文件换掉。</summary>
        static Texture2D ArtTexture(CardData d)
        {
            var real = CardArt.Portrait(d.id);
            if (real != null) return real;

            string key = "art|" + ColorUtility.ToHtmlStringRGBA(d.frame) + "|" + d.title;
            Texture2D cached;
            if (ArtCache.TryGetValue(key, out cached) && cached != null) return cached;

            // ⚠️ **别在中间放大字**：卡名就画在这块中间（原版也是），
            //    大字会和名字打架（踩过）。占位图只要「看得出这里该有画」就够。
            const int W = 128, H = 179;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)(H - 1);
                // 上亮下暗的斜渐变 + 一点阵营色，像张「待填的画」
                float k = 0.20f + 0.35f * t;
                var c = new Color32((byte)(BgArt.r * k + d.frame.r * 90 * (1f - t)),
                                    (byte)(BgArt.g * k + d.frame.g * 90 * (1f - t)),
                                    (byte)(BgArt.b * k + d.frame.b * 90 * (1f - t)), 255);
                for (int x = 0; x < W; x++) px[y * W + x] = c;
            }
            // 对角斜线，明确是「占位」
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (((x + y) / 9) % 2 == 0 && (y % 3) == 0)
                        px[y * W + x] = new Color32((byte)(d.frame.r * 70 + 18),
                                                    (byte)(d.frame.g * 70 + 18),
                                                    (byte)(d.frame.b * 70 + 18), 255);

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            ArtCache[key] = tex;
            return tex;
        }

        static Color32[] Blank(int W, int H)
        {
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            return px;
        }

        /// <summary>把一个数字居中画在 (x01, y01) 那个点上</summary>
        static void DrawCenteredAt(Color32[] px, Vector2 at, string s, int scale, Color32 c)
        {
            int w = TextCanvas.Measure(s, scale), h = TextCanvas.LineHeight(scale);
            TextCanvas.Draw(px, FaceW, FaceH, s, scale,
                            Mathf.RoundToInt(at.x * FaceW - w * 0.5f),
                            Mathf.RoundToInt(at.y * FaceH - h * 0.5f), c);
        }

        /// <summary>文字区底下的柔和压暗（竖渐变 + 左右淡出）。见 `InfoTexture` 里的注释。</summary>
        static void DrawTextScrim(Color32[] px, int W, int H, float y0, float y1, float x0, float x1, int maxA)
        {
            int py0 = Mathf.Clamp(Mathf.RoundToInt(y0 * H), 0, H - 1);
            int py1 = Mathf.Clamp(Mathf.RoundToInt(y1 * H), 0, H - 1);
            int px0 = Mathf.Clamp(Mathf.RoundToInt(x0 * W), 0, W - 1);
            int px1 = Mathf.Clamp(Mathf.RoundToInt(x1 * W), 0, W - 1);
            int span = Mathf.Max(1, py1 - py0);
            for (int y = py0; y <= py1; y++)
            {
                // 纵向：中间最重、上下淡出
                float t = (y - py0) / (float)span;
                float vy = Mathf.Sin(t * Mathf.PI);
                int row = (H - 1 - y) * W;        // 数组 row 0 在下边（和本文件其它画法一致）
                for (int x = px0; x <= px1; x++)
                {
                    float u = (x - px0) / (float)Mathf.Max(1, px1 - px0);
                    float vx = Mathf.Sin(u * Mathf.PI);
                    int a = Mathf.RoundToInt(maxA * vy * vx);
                    if (a <= 0) continue;
                    int i = row + x;
                    // 直接盖上去（`_info` 在卡框之上，所以这是「压暗」不是「混色」）
                    px[i] = new Color32(0, 0, 0, (byte)Mathf.Min(255, px[i].a + a));
                }
            }
        }

        static void DrawStatAt(Color32[] px, Vector2 at, int value, Color32 c)
        {
            DrawCenteredAt(px, at, Mathf.Max(0, value).ToString(), 4, c);
        }

        /// <summary>尖朝左右的正六边形（费用底）。
        /// ⚠️ `cy` 是**从顶部**数的像素行（和本文件其它画法一致）——
        ///    画布数组本身 row 0 在底部，所以这里要先翻一次，别直接拿它当行号（踩过）</summary>
        static void FillHexagon(Color32[] px, int W, int H, float cx, float cyFromTop, float r, Color32 c)
        {
            float cy = (H - 1) - cyFromTop;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + r));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx) / r, dy = Mathf.Abs(y + 0.5f - cy) / r;
                    if (dy <= 1f && dx <= 1f - 0.5f * dy) px[y * W + x] = c;
                }
        }

        // ---- 占位卡面（没有原版卡框时用）：整张黑卡 ----
        static Texture2D FaceTexture(CardData d)
        {
            string key = CacheKey(d);
            Texture2D cached;
            if (FaceCache.TryGetValue(key, out cached) && cached != null) return cached;

            var px = Blank(FaceW, FaceH);

            // ① 圆角黑底
            FillRoundedRect(px, FaceW, FaceH, 0.015f, 0.02f, BgCard);

            // ② 阵营色边框（细，不喧宾夺主）
            var edge = (Color32)d.frame; edge.a = 255;
            StrokeRoundedRect(px, FaceW, FaceH, 0.015f, 0.02f, 3, edge);

            // ③ 插图位：**抠成透明**（不是填底色）—— 立绘那一层就画在这个位置（`ArtMesh(.., WellW..)`），
            //    卡面在 z=0、立绘在 z=0.03（更远），所以这里透明才能让立绘露出来。
            //    ⚠️ 立绘缺了也不会开天窗：`ArtTexture()` 会退回程序生成的占位画。
            ClearRect(px, FaceW, FaceH, 0.06f, 0.20f, 0.94f, 0.58f);
            StrokeRect(px, FaceW, FaceH, 0.06f, 0.20f, 0.94f, 0.58f, 1, new Color32(48, 48, 58, 255));

            // ④⑤⑥⑦ 复用数值层那套画法（位置和原版卡框一致）
            var info = InfoTexture(d);
            var infoPx = info.GetPixels32();
            for (int i = 0; i < px.Length && i < infoPx.Length; i++)
            {
                var s = infoPx[i];
                if (s.a > 0) px[i] = s;
            }

            var tex = new Texture2D(FaceW, FaceH, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            FaceCache[key] = tex;
            return tex;
        }

        // ---- 画布基本图元 ----
        // ⚠️ 坐标都是归一化的，**y 从顶部往下**（和 TextCanvas 一致）。
        //    画布数组本身是 row 0 在底部，所以这里统一翻一次，调用处就不用操心。

        static int RowFromTop(int H, float y01Top)
        {
            return H - 1 - Mathf.RoundToInt(y01Top * H);
        }

        /// <summary>把一块矩形**抠成透明**（alpha=0）—— 占位卡面的插图位用它，
        /// 好让后面那层立绘透出来（见 `Build` 里没有卡框那条分支）。</summary>
        static void ClearRect(Color32[] px, int W, int H, float x0, float y0, float x1, float y1)
        {
            FillRect(px, W, H, x0, y0, x1, y1, new Color32(0, 0, 0, 0));
        }

        static void FillRect(Color32[] px, int W, int H, float x0, float y0, float x1, float y1, Color32 c)
        {
            int ix0 = Mathf.RoundToInt(x0 * W), ix1 = Mathf.RoundToInt(x1 * W);
            int rowTop = RowFromTop(H, y0), rowBot = RowFromTop(H, y1);
            for (int y = rowBot; y <= rowTop; y++)
            {
                if (y < 0 || y >= H) continue;
                for (int x = ix0; x < ix1; x++)
                {
                    if (x < 0 || x >= W) continue;
                    px[y * W + x] = c;
                }
            }
        }

        static void StrokeRect(Color32[] px, int W, int H, float x0, float y0, float x1, float y1,
                               int thickness, Color32 c)
        {
            int ix0 = Mathf.RoundToInt(x0 * W), ix1 = Mathf.RoundToInt(x1 * W);
            int rowTop = RowFromTop(H, y0), rowBot = RowFromTop(H, y1);
            for (int t = 0; t < thickness; t++)
            {
                for (int x = ix0; x < ix1; x++)
                {
                    Plot(px, W, H, x, rowTop - t, c);
                    Plot(px, W, H, x, rowBot + t, c);
                }
                for (int y = rowBot; y <= rowTop; y++)
                {
                    Plot(px, W, H, ix0 + t, y, c);
                    Plot(px, W, H, ix1 - 1 - t, y, c);
                }
            }
        }

        static void Plot(Color32[] px, int W, int H, int x, int y, Color32 c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            px[y * W + x] = c;
        }

        /// <summary>归一化坐标是否落在「内缩 margin、圆角 radius」的圆角矩形里</summary>
        static bool InRoundedRect(float nx, float ny, float margin, float radius)
        {
            float x0 = margin, x1 = 1f - margin, y0 = margin, y1 = 1f - margin;
            if (nx < x0 || nx > x1 || ny < y0 || ny > y1) return false;
            float cx = Mathf.Clamp(nx, x0 + radius, x1 - radius);
            float cy = Mathf.Clamp(ny, y0 + radius, y1 - radius);
            float dx = nx - cx, dy = ny - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        static void FillRoundedRect(Color32[] px, int W, int H, float margin, float radius, Color32 c)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (InRoundedRect((x + 0.5f) / W, (y + 0.5f) / H, margin, radius))
                        px[y * W + x] = c;
        }

        static void StrokeRoundedRect(Color32[] px, int W, int H, float margin, float radius,
                                      int thickness, Color32 c)
        {
            float innerMargin = margin + (float)thickness / Mathf.Min(W, H);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float nx = (x + 0.5f) / W, ny = (y + 0.5f) / H;
                    if (InRoundedRect(nx, ny, margin, radius) && !InRoundedRect(nx, ny, innerMargin, radius))
                        px[y * W + x] = c;
                }
        }

        static Mesh _quad;
        static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var m = new Mesh { name = "CardQuad" };
            float w = Width * 0.5f, h = Height * 0.5f;
            m.vertices = new[]
            {
                new Vector3(-w, -h, 0f), new Vector3(w, -h, 0f),
                new Vector3(w, h, 0f),   new Vector3(-w, h, 0f),
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _quad = m;
            return m;
        }
    }
}
