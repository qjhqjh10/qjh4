// AllianceEventScoreInfo.cs — A103：原版 prefab **`Alliance Event Score Info`**（根组件类 `AllianceScoreBar`）
//
// ============================ 出处（判据一律现读）============================
//   python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Event Score Info" \
//          --depth 14 --relative --md          # **26 个节点**（根 + 25）
//   · 根组件字段（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AllianceScoreBar.cs`）：
//       `progressBar`(Slider) · `scoringLines`(AllianceScoreBarLine[])
//   · 子件类 `AllianceScoreBarLine`：`score`(TMP) · `chestReward`(Image) · `chestOpen`(Sprite)
//   · 方法体：`AllianceScoreBar__{Initialize,SetProgressBar,SetScoringMilestones}.c` ·
//             `AllianceScoreBarLine__{Initialize,ShowChestOpen}.c`
//   · 五档箱子的「开箱图」= 逐颗读 `Alliance Score Bar Line Level N.json` 的 MB：
//       `chestOpen` = `40k_Crate_TierN_<材质>_open`（N=1 Iron / 2 Copper / 3 Silver / 4 Gold / 5 Warp）
//
// ⚠️ **这是「面板的一部分」不是窗**：根组件是 `MonoBehaviour`（`AllianceScoreBar`），
//    **不是 `GameWindow`** ⇒ **没有 `type` / `placement` / `closeOnESC` 可填、也不注册进 `WindowsManager`**。
//    它同时以 **standalone prefab** 与 **`Alliance Event Score Panel` 的嵌套实例**两处出现
//    （两处的框逐位相同 ⇒ 由 `AllianceEventScorePanel` 在本件 `Build()` 之上加一个原点偏移复用）。
//
// ============================ 状态 → 参数 ============================
// | 件 | prefab 出厂（现读） | 运行期由谁改 | 我们的处置 |
// |---|---|---|---|
// | 5 行 `Score` | `'1256'` | `AllianceScoreBarLine.Initialize(int)` = `score.SetText(n.ToString())` | 出厂照 prefab 原文；`SetMilestones` 是第二态 |
// | 5 颗 `Chest` | `40k_Crate_TierN_<材质>`（闭箱） | `ShowChestOpen()` = `Image.sprite = chestOpen` | 出厂闭箱；`SetMilestones` 在「已达那一档」时换开箱图 |
// | `Progress Bar`(Slider) | `Fill` 现读 **0.00×4.16**（进度 0） | `Slider.maxValue = 末档 · normalizedValue = InterpolateMilestones(...)` | 出厂画 0；`SetProgress(v)` 是第二态 |
// | `Score Levels` 的 VLG | **`reverse=1`** | —— | 子件按**树序倒排**：`Level 5` 在最上、`Level 1` 在最下 |
//
// 🔴 **`reverse=1` 的实据**：现读 `Level 1` 中心 y = **266.285**、`Level 5` = **18.695**（y 向下）
//    ⇒ 树序第一颗**在最下**。判据 = uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`。
// ⛔ 别按「树序第一 = 最上」摆 —— 那会把整条记分条上下镜像，而只断「有几行」的断言**看不出来**。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 prefab `Alliance Event Score Info`（`AllianceScoreBar`）：竖进度条 + 5 档里程碑。</summary>
    public class AllianceEventScoreInfo : MonoBehaviour
    {
        // 分层（渲染队列，⛔ 不是 z）—— 本件占 **3975–3981**
        //  ⚠️ 与 `EnergySinglePlayerOnlyEventWindow`（3935–3945）**不重叠**；⛔ 别挪进别人的段。
        public const int QBar = 3975;    // 进度条底 / 箱子 / 骷髅
        public const int QBar1 = 3976;   // 压在底上的那一层（进度填充）
        public const int QText = 3977;

        // ============================================================ 几何（现读 `--relative`，根在 0,0）
        const float RootW = 273.92f, RootH = 357.88f;
        const float ProgCx = 136.96f, ProgCy = 179.125f, ProgVisW = 63.16f, ProgVisH = 344.55f;  // × scl 0.8696
        const float BgCx = 136.96f, BgCy = 179.125f, BgW = 36.32f, BgH = 396.24f;                 // 已含两层 scale
        const float FillAreaCx = 136.69f, FillAreaCy = 178.14f, FillAreaW = 26.75f, FillAreaH = 357.89f;
        // 现读 `Fill` = **0.00 × 4.16**、框 123.32,352.93→123.32,357.09（y 向下 ⇒ 352.93 **上沿**、
        // 357.09 下沿）。塌的是**宽**（同 Energy 那一格：`Slider` 按 `anchorMax.x = fillAmount` 缩）⇒ 向右长。
        const float FillX1 = 123.32f, FillTop0 = 352.93f, FillBarH = 4.16f;
        static readonly PxRect LevelsR = new PxRect(89.17f, -12.25f, 184.75f, 297.23f);
        const float LvlCx = 136.96f, LvlW = 282.55f, LvlH = 57.81f;
        const float LvlCy0 = 266.285f, LvlStep = 61.8975f;   // = 现读 5 档中心等距（61.8975 逐位可验）       // `Level 1` 在最下
        // ⚠️ 这三组 x 取的是**这一份 standalone prefab 自己的帧**（现读：`Chest` 160.55→247.56 等）；
        //    `Alliance Event Score Panel` 里那一份**整体偏 (54.84,144.36)** —— 由调用方传 `at` 平移，
        //    ⛔ 别把嵌套那一份的 x 抄到这里来（2026-10-13 对账脚本抓出过一次）。
        const float ChestCx = 204.055f, ChestCy = 266.285f, ChestW = 75.66f, ChestH = 67.49f;   // 视觉框 ×0.8696
        const float SkullCx = 92.605f,  SkullCy = 266.285f, SkullW = 48.69f, SkullH = 43.43f;
        const float ScoreCx = 14.75f,   ScoreCy = 266.285f, ScoreW = 96.54f, ScoreH = 41.56f;

        const string ArtMiniBar = "MiniBar_01";                        // 59×648 · Simple
        const string ArtSkull   = "40K_missions_icon_Daily_skulls";    // 222×198 · Simple
        static readonly string[] CrateArt =
        { "40k_Crate_Tier1_Iron", "40k_Crate_Tier2_Copper", "40k_Crate_Tier3_Silver",
          "40k_Crate_Tier4_Gold", "40k_Crate_Tier5_Warp" };
        /// <summary>五档箱子的**开箱图**（逐颗读 MB 的 `chestOpen` 字段得到；⛔ 不是猜的）。
        /// ⚠️ **本仓 `Resources/` 里一张都没有**（源在 `bundle_boosterpacks_assets_all/Sprite/`，
        /// 切好的 PNG 在 `d:/2/Warpforge_tools/data/ui_extract/boosterpacks_assets_all/Sprite/`）⇒ 记账 + 出声。</summary>
        static readonly string[] CrateOpenArt =
        { "40k_Crate_Tier1_Iron_open", "40k_Crate_Tier2_Copper_open", "40k_Crate_Tier3_Silver_open",
          "40k_Crate_Tier4_Gold_open", "40k_Crate_Tier5_Warp_open" };
        static readonly Color FillTint = new Color(1f, 0.228f, 0.0142f, 1f);   // 实读

        // ============================================================ 建出来的东西
        readonly List<Transform> _levels = new List<Transform>();
        readonly List<ImageQuad> _chests = new List<ImageQuad>();
        readonly List<Label> _scores = new List<Label>();
        readonly List<PxRect> _scoreRects = new List<PxRect>();
        readonly List<string> _missArt = new List<string>();
        Transform _root, _fill;
        float _progress;

        public List<Transform> LevelRows { get { return _levels; } }
        public Transform Root { get { return _root; } }
        public List<string> MissingArt { get { return _missArt; } }
        public float Progress { get { return _progress; } }

        // ============================================================ 建
        /// <summary>在 <paramref name="parent"/> 下建出这一棵，根框 = <paramref name="at"/>。
        /// ⚠️ `at` 的宽高**必须是 273.92 × 357.88**（= 原版根框）—— 同族的调用点给的都是这个尺寸，
        /// 里面所有子件坐标都按「根在 (0,0)」的现读值再加 `at` 的原点算出来的。</summary>
        public static AllianceEventScoreInfo Create(Transform parent, PxRect at, string name = "Alliance Event Score Info")
        {
            var go = new GameObject(name);
            var c = go.AddComponent<AllianceEventScoreInfo>();
            c._root = go.transform;
            go.transform.SetParent(parent, false);
            MenuDraw.ApplyPxRect(go.transform, parent, at);   // 根自己也带矩形（原版根框 273.92×357.88）
            c.Build(at);
            return c;
        }

        void Build(PxRect at)
        {
            float dx = at.x1, dy = at.y1;
            _missArt.Clear();
            _levels.Clear(); _chests.Clear(); _scores.Clear(); _scoreRects.Clear();

            // ---- `Progress Bar`（Slider）：原版这一颗自带 scl **0.8696** ⇒ 容器取**视觉框** ----
            var pr = MenuDraw.Node(_root, "Progress Bar", Rect(dx + ProgCx, dy + ProgCy, ProgVisW, ProgVisH));
            MenuDraw.Rect(pr, Art(ArtMiniBar), Rect(dx + BgCx, dy + BgCy, BgW, BgH), "Background", QBar);
            // `Fill Area`（原版这一颗只有 `RectTransform`、没有组件）+ 它的孩子 `Fill`
            var fa = MenuDraw.Node(pr, "Fill Area", Rect(dx + FillAreaCx, dy + FillAreaCy, FillAreaW, FillAreaH));
            _fill = MenuDraw.Node(fa, "Fill", Rect(dx + FillX1, dy + FillTop0 + FillBarH * 0.5f, 0f, FillBarH));
            DrawFill(0f, dx, dy);

            // ---- `Score Levels`（VLG **reverse=1** · spacing 0 · pad 0 · align MiddleCenter · expandH=1）----
            var levels = MenuDraw.Node(_root, "Score Levels",
                                       new PxRect(dx + LevelsR.x1, dy + LevelsR.y1, dx + LevelsR.x2, dy + LevelsR.y2));
            for (int i = 0; i < 5; i++)
            {
                // i = 树序下标（0 起 = `Level 1`）⇒ 它排在**最下**那一档；往上走一步 −LvlStep
                float cy = LvlCy0 - i * LvlStep;
                var row = MenuDraw.Node(levels, "Alliance Score Bar Line Level " + (i + 1),
                                        Rect(dx + LvlCx, dy + cy, LvlW, LvlH));
                _levels.Add(row);
                float rdy = -i * LvlStep;
                // `Chest`（自带 scl 0.8696 ⇒ 视觉框）—— 出厂画**闭箱图**（MB `chestReward`）
                _chests.Add(MenuDraw.Rect(row, Art(CrateArt[i]),
                                          Rect(dx + ChestCx, dy + ChestCy + rdy, ChestW, ChestH), "Chest", QBar));
                // `Skull`（`40K_missions_icon_Daily skulls` 222×198）
                MenuDraw.Rect(row, Art(ArtSkull), Rect(dx + SkullCx, dy + SkullCy + rdy, SkullW, SkullH),
                              "Skull", QBar);
                // `Score`：原版 hAlign = **Right**（现读 `对齐=Right/Midline · 折行=1`）
                var sr = Rect(dx + ScoreCx, dy + ScoreCy + rdy, ScoreW, ScoreH);
                var sc = MenuDraw.Text(row, sr, "1256", Color.white, "Score", 48f, QText);
                MenuDraw.AlignRight(sc, sr);
                _scores.Add(sc);
                _scoreRects.Add(sr);
            }
            _at = at;
        }

        /// <summary>本件当前的落点（`SetMilestones` 换字之后要重排对齐 —— 见那边的注释）。</summary>
        PxRect _at;

        // ============================================================ 状态 → 参数（两态）
        /// <summary>进度条那一格（原版 `Slider.normalizedValue`）。`v = 0` = 出厂那一档。
        /// ⚠️ 增长方向 = **向右**（判据：原版 `Fill` 现读 **0.00 宽 × 4.16 高** ⇒ 塌的是**宽**，
        /// 而 uGUI 的 `Slider` 是拿 `anchorMax.x = fillAmount` 缩那一格的）；`m_Direction` 的枚举值
        /// **没有逐字段读**（见报告 §九·8）。</summary>
        public void SetProgress(float v)
        {
            v = Mathf.Clamp01(v);
            // 🔴 **必须带本件自己的落点**（`_at`）—— 这一份是**可被嵌到别处**的子 prefab
            //    （`Alliance Event Score Panel` 里那一份偏 (54.84,144.36)）⇒ 写死 (0,0)
            //    会把填充条画到面板左上角去，而且**只在调用 `SetProgress` 之后**才现形（静默）。
            DrawFill(v, _at.x1, _at.y1);
        }

        void DrawFill(float v, float dx, float dy)
        {
            if (_fill == null) return;
            MenuDraw.ClearChildren(_fill);
            _progress = v;
            float w = FillAreaW * v;
            if (w <= 0.01f) return;      // 出厂：原版就是 0 宽 ⇒ **不建 quad**（不是「建了看不见」）
            // ⚠️ `Rect(cx,cy,w,h)` 的 `cy` 是**中心** ⇒ 要加半个条高（首版 `Build` 加对了、这里漏了 ⇒ 差 2.08px）
            MenuDraw.Rect(_fill, CardArt.Solid(),
                          Rect(dx + FillX1, dy + FillTop0 + FillBarH * 0.5f, w, FillBarH), "FillBar", QBar1, FillTint);
        }

        /// <summary>喂分数 + 阈值（原版 `AllianceScoreBar.Initialize(currentScore, scoringThresholds)`）：
        /// ① 五行的 `Score` = `thresholds[i].ToString()`（`AllianceScoreBarLine.Initialize(int)` 逐句可读）；
        /// ② **已达阈值的那些档换开箱图**（`ShowChestOpen()` = `Image.sprite = chestOpen`）；
        /// ③ 进度 = `currentScore / 末档`（原版 `Slider.maxValue = 末档 · normalizedValue = InterpolateMilestones`，
        ///    那个函数要在档位之间做**插值**——本地没有它的字面量，这里用**线性归一**并如实标）。
        /// <para>⚠️ **两处如实标**：开箱图本仓没有 ⇒ 换图会记账 + 出声；插值算法是**我们用的线性归一**，
        /// 不是原版 `SupportMethods.InterpolateMilestones`（后者的方法体只读到「在相邻两档之间取值」）。</para></summary>
        public void SetMilestones(int currentScore, int[] thresholds)
        {
            if (thresholds == null || thresholds.Length < 5)
            {
                Debug.LogWarning("[Alliance] `SetMilestones` 至少要 5 个阈值（原版 `scoringLines` 是 5 颗）；"
                               + "这次传了 " + (thresholds == null ? 0 : thresholds.Length) + " 个 ⇒ **不画**（出声）");
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                if (_scores[i] != null)
                {
                    _scores[i].SetText(thresholds[i].ToString());
                    // 🔴 **换完字要重排对齐**：`Label` 那一段是按**当时的 `WorldW`** 反推整块位置的
                    //    （`Battle/Label.cs` 的 `AlignRightOn`）⇒ 字变宽/变窄之后不重排，右沿就跑掉了。
                    //    ⛔ 这不是「顺手多做一步」：原版那三格是静态字段、不存在这个次序问题。
                    MenuDraw.AlignRight(_scores[i], _scoreRects[i]);
                }
                if (currentScore >= thresholds[i]) SwapChestOpen(i);
            }
            SetProgress(thresholds[4] > 0 ? (float)currentScore / thresholds[4] : 0f);
        }

        void SwapChestOpen(int i)
        {
            var tex = Art(CrateOpenArt[i]);
            if (_chests[i] == null) return;
            if (tex == null)
            {
                Debug.Log("[Alliance] 第 " + (i + 1) + " 档**已达**（要换开箱图 `" + CrateOpenArt[i] + "`），"
                        + "但**本仓 `Resources/` 里没有这张图** ⇒ 保持闭箱图（⛔ 不拿别的图顶上）。"
                        + "源在 `bundle_boosterpacks_assets_all/Sprite/`（切好的 PNG 在 `ui_extract/…`）。");
                return;
            }
            _chests[i].SetTexture(tex);
        }

        /// <summary>`Score` 那 5 行的文字（自检读口）。</summary>
        public List<Label> ScoreLabels { get { return _scores; } }
        /// <summary>5 颗箱子（自检读口）。</summary>
        public List<ImageQuad> Chests { get { return _chests; } }

        // ============================================================ 助手
        static PxRect Rect(float cx, float cy, float w, float h)
            => new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);

        /// <summary>取图 + 记账（`MenuDraw.Rect` 取不到图会**静默 return null** ⇒ 整层消失）。</summary>
        public Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !_missArt.Contains(name)) _missArt.Add(name);
            return t;
        }
    }
}
