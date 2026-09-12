// EndPanel.cs — 对局结算界面（原版 `EndBattlePanel`）
//
// ---- 尺寸出处 ----
// 全部来自运行时 UI dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`
// 里 `BattleHud/Canvas/BattleDoors/Canvas/EndBattlePanel` 那棵子树（实测 anchoredPosition / sizeDelta）：
//   Background        3840×2160   （两倍屏，铺满带余量）
//   Video Image       1920×1080  @ (-6, 64)
//   AllRewardsHolder  100×100    @ (0, -260)
//   SkullsHolder      648.1×52.4   图 `40k_main_bt_nametag`
//   skull1/2/3        64.3×71.2    图 `40k_battle_Win Skull`
//   RewardsHolder     495.2×49.0   图 `40k_main_bt_nametag`
//   RewardHolder      140×50 → DrawerHolder 65×50 @ (5,0) → Quantity 文字 0×45
//   HolderRating      187×45 → Trophy 60.1×60（图 `40k_UI_icon_ranked_Skirmish`）、
//                              RatingText 65.3×46.9、ArmyIcon 64.8×60（dump 里 activeSelf=False）
//
// ⚠️ **两处是我们排的，不是原版**（dump 里拿不到，别当原版抄）：
//   1. **纵向叠放顺序**：dump 里 `SkullsHolder` 和 `RewardsHolder` 的 anchoredPosition **都是 (0,0)** ——
//      说明位置是运行时 LayoutGroup 算出来的，静态 dump 只有建之前的初值。这里改成上下两行排。
//   2. **未点亮的骷髅用半透明表示**：原版那三张 `skull1..3` 是同一个 sprite，
//      亮/灭怎么表现（是不是有第二张图、还是靠缩放/着色）dump 里看不出来。
//
// ⚠️ 奖杯图：dump 里是 `40k_UI_icon_ranked_Skirmish`，**那张图我们的图集里没有**，
//    用 `WF_UI_Trophy_Gold` 顶（**这是替代，不是原版**）。
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    public class EndPanel : MonoBehaviour
    {
        /// <summary>原版界面按 1920×1080 设计；这套布局可见高 10 单位 → 108 px/单位</summary>
        const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;

        /// <summary>整块面板放到 HUD **前面**（相机在 z=-20 朝 +z 看，z 越小越近）。
        /// 不这样的话面板和 HUD 同层，谁压谁看渲染顺序 —— 这条已经踩过两次。</summary>
        const float Z = -0.5f;

        static Vector3 Pos(float px, float py)
        {
            return new Vector3((px - 960f) / PxPerUnit, (540f - py) / PxPerUnit, Z);
        }

        static float U(float px) { return px / PxPerUnit; }

        /// <summary>面板是不是正显示着（自检用）。</summary>
        public bool Visible { get; private set; }

        /// <summary>这次显示点亮了几个骷髅（自检用）。</summary>
        public int ShownSkulls { get; private set; }

        /// <summary>结果文字（自检用）：`胜利` / `失败` / `平局`。没显示时是空串。</summary>
        public string ResultText { get; private set; }

        Transform _root;
        ImageQuad _dim;
        Label _title, _sub, _rating;
        readonly ImageQuad[] _skulls = new ImageQuad[DeckRules.SkullThresholds.Length];
        ImageQuad _skullPlate, _rewardPlate, _trophy;

        public static EndPanel Create(Transform parent)
        {
            var go = new GameObject("EndPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<EndPanel>();
            p.Build();
            p.Hide();
            return p;
        }

        void Build()
        {
            _root = transform;

            // 压暗背景：dump 里 `Background` 是一张 **3840×2160（两倍屏）的整屏图** ——
            // 也就是说原版的结算界面**把战场整块盖住**。我们没有那张图，用**重压暗**替代
            // （**这是替代，不是原版**）。压暗不够重的话，棋盘上的卡会从面板内容里透出来。
            // ⚠️ `ImageQuad` 的宽度是**从贴图宽高比推出来**的，不是给两个尺寸。
            //    第一版用 4×4 的方图 → 压暗是个 12×12 的方块，屏幕左右两边没盖住（截图才发现）。
            //    所以贴图得按「当前可见区」的宽高比造。
            _dim = ImageQuad.Create(_root, SolidTex(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                    Vector3.zero,
                                    LayoutSpace.DesignHeight * 1.05f, new Vector2(0.5f, 0.5f), "dim");
            if (_dim != null) _dim.SetTint(new Color(0f, 0f, 0f, 0.97f));

            // 结果：原版这块的文字是运行时填的，dump 里 `EndBattlePanel` 子树下没有结果文字节点
            //（只有 Video Image / AllRewardsHolder 那几块）→ **字号和位置是我们挑的**
            //
            // ⚠️ 位置要避开棋盘：玩家行 y=708、对手行 y=466（`BattleScene` 里那两个常量），
            //    第一版把骷髅/奖杯放在 700/780，正好压在玩家那行卡上（截图才发现）。
            //    这里统一收到 **y<400 的上方空白区**。
            _title = Label.Create(_root, "", Pos(960f, 260f), 6, Color.white, new Vector2(0.5f, 0.5f), "end_title");
            _sub = Label.Create(_root, "", Pos(960f, 330f), 2, new Color(0.8f, 0.8f, 0.85f),
                                new Vector2(0.5f, 0.5f), "end_sub");

            // 骷髅行：底板 648.1×52.4，三个 64.3×71.2 的骷髅
            // ⚠️ 纵向位置是我们排的（见文件头注释 1）
            _skullPlate = ImageQuad.Create(_root, CardArt.Ui("40k_main_bt_nametag"), Pos(960f, 430f),
                                           U(52.4f), new Vector2(0.5f, 0.5f), "skull_plate");
            float step = 64.3f + 12f;
            for (int i = 0; i < _skulls.Length; i++)
            {
                float x = 960f + (i - (_skulls.Length - 1) * 0.5f) * step;
                _skulls[i] = ImageQuad.Create(_root, CardArt.Ui("40k_battle_Win_Skull"), Pos(x, 430f),
                                              U(71.2f), new Vector2(0.5f, 0.5f), "skull_" + i);
            }

            // 奖励行：底板 495.2×49.0 + 奖杯 60.1×60 + 评分文字
            _rewardPlate = ImageQuad.Create(_root, CardArt.Ui("40k_main_bt_nametag"), Pos(960f, 500f),
                                            U(49.0f), new Vector2(0.5f, 0.5f), "reward_plate");
            _trophy = ImageQuad.Create(_root, CardArt.DeckUi("WF_UI_Trophy_Gold"), Pos(860f, 500f),
                                       U(60.1f), new Vector2(0.5f, 0.5f), "trophy");
            _rating = Label.Create(_root, "", Pos(1030f, 500f), 2, new Color(0.95f, 0.85f, 0.5f),
                                   new Vector2(0.5f, 0.5f), "rating");

            // 结束语下面的操作提示
            Label.Create(_root, "按 R 再来一局", Pos(960f, 960f), 2, new Color(0.7f, 0.7f, 0.75f),
                         new Vector2(0.5f, 0.5f), "end_hint");
        }

        /// <summary>
        /// 显示结算。
        /// </summary>
        /// <param name="winner">`RuleCore` 那套：0=没结束，1/2=玩家序号+1，3=平局。</param>
        /// <param name="myIndex">我是几号玩家（0 基）。</param>
        /// <param name="minFoeWarlordHealth">这局里**敌方督军降到过的最低生命** —— 骷髅数由它算
        /// （规则书:36，判据只在 `DeckRules.SkullsFor` 一处）。</param>
        /// <param name="rounds">打了几个回合。</param>
        public void Show(int winner, int myIndex, int minFoeWarlordHealth, int rounds)
        {
            Visible = true;
            ShownSkulls = DeckRules.SkullsFor(minFoeWarlordHealth);
            ResultText = winner == 3 ? "平局" : (winner == myIndex + 1 ? "胜利" : "失败");

            // ⚠️ **先激活再写文字** —— 反过来的话 `Label.SetText` 在未激活的物体上跑，
            //    字形网格没重建，标题/副标题/评分全是空白（截图才发现）
            SetVisible(true);

            string r = ResultText;
            _title.SetText(r == "胜利" ? CardText.Phrase("VICTORY") : (r == "失败" ? CardText.Phrase("DEFEAT") : r));
            _sub.SetText($"{rounds} 回合   敌方督军最低生命 {minFoeWarlordHealth}");
            _rating.SetText($"{ShownSkulls} / {_skulls.Length}");

            for (int i = 0; i < _skulls.Length; i++)
            {
                // ⚠️ 「未点亮 = 半透明」是我们挑的（见文件头注释 2）
                if (_skulls[i] != null)
                    _skulls[i].SetTint(i < ShownSkulls ? Color.white : new Color(1f, 1f, 1f, 0.18f));
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Visible = false;
            ShownSkulls = 0;
            ResultText = "";
            SetVisible(false);
        }

        void SetVisible(bool v)
        {
            foreach (var t in _root.GetComponentsInChildren<Transform>(true))
                t.gameObject.SetActive(v);
            // 根自己开着（否则 SetActive(false) 之后没法再打开），只有子物体跟着切
            gameObject.SetActive(true);
        }

        /// <summary>铺满用的纯白贴图（`ImageQuad` 要一张图才肯建）。宽高比按传入的来，
        /// 因为图的宽度是从**贴图宽高比**推的 —— 见 `Build()` 里的注释。</summary>
        static Texture2D _solid;
        static float _solidAspect;
        static Texture2D SolidTex(float aspect)
        {
            if (_solid != null && Mathf.Abs(_solidAspect - aspect) < 0.01f) return _solid;
            int h = 32;
            int w = Mathf.Max(4, Mathf.RoundToInt(h * aspect));
            _solid = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            _solid.SetPixels(px);
            _solid.Apply();
            _solidAspect = aspect;
            return _solid;
        }
    }
}
