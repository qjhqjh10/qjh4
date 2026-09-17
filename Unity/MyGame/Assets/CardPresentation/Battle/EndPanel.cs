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
// 奖杯图：原版那张是 `40k_UI_icon_ranked_Skirmish`（dump 里 `Trophy` 节点挂的 sprite）。
//
// 🔴 **2026-09-17 用户拍板：奖励那一行我们不做** —— 奖励（红水晶数量）与评分**都在服务器**，
//    单机版用不上 ⇒ **只留骷髅那行**（`SkullsHolder` + 三个骷髅），
//    `RewardsHolder` / `HolderRating` / `Trophy` / `RatingText` **一个都不建**。
//    上面 dump 里那两行尺寸**保留当参照**（将来真要做，照它摆就行），代码里不再生成。
//    （历史留痕：那段「奖杯图一度静默不画」的更正随奖励行一起去掉了；原版那张
//      `40k_UI_icon_ranked_Skirmish` 仍在 `Resources/Art/ui_deck/` 里，将来要做直接取。）
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    public class EndPanel : MonoBehaviour
    {
        /// <summary>原版界面按 1920×1080 设计；这套布局可见高 10 单位 → 108 px/单位</summary>
        public const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;

        /// <summary>像素坐标（左上原点）→ 世界坐标。z 越小越靠近相机。
        /// ⚠️ 加上开门视频之后面板分了**三层**，三个 z 的含义见 `BattleDoors`。</summary>
        public static Vector3 Pos(float px, float py, float z)
        {
            return new Vector3((px - 960f) / PxPerUnit, (540f - py) / PxPerUnit, z);
        }

        /// <summary>内容层（标题 / 副标题 / 骷髅 / 提示）—— 盖在开门视频**上面**那一层。</summary>
        static Vector3 Content(float px, float py)
        {
            return Pos(px, py, BattleDoors.ZContent);
        }

        static float U(float px) { return px / PxPerUnit; }

        /// <summary>面板是不是正显示着（自检用）。</summary>
        public bool Visible { get; private set; }

        /// <summary>这次显示点亮了几个骷髅（自检用）。</summary>
        public int ShownSkulls { get; private set; }

        /// <summary>结果文字（自检用）：`胜利` / `失败` / `平局`。没显示时是空串。</summary>
        public string ResultText { get; private set; }
        /// <summary>副标题那行（`N 回合   敌方督军最低生命 X` / 投降时是另一种写法）。自检读它。</summary>
        public string SubText { get { return _sub != null ? _sub.Text : null; } }

        /// <summary>开门视频那层（自检用）。</summary>
        public BattleDoors Doors { get { return _doors; } }

        /// <summary>内容层（标题 / 副标题 / 骷髅 / 提示）是不是露着的（自检用）。</summary>
        public bool ContentVisible { get { return _content != null && _content.gameObject.activeSelf; } }

        /// <summary>结果文字（标题）是不是露着的（自检用）。**放开门视频时应该是 false** —— 字在视频里。</summary>
        public bool TitleVisible { get { return _title != null && _title.gameObject.activeSelf; } }

        Transform _root, _content;
        BattleDoors _doors;
        ImageQuad _dim;
        Label _title, _sub;
        readonly ImageQuad[] _skulls = new ImageQuad[DeckRules.SkullThresholds.Length];
        ImageQuad _skullPlate;

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
            // ⚠️ **压暗必须比卡靠前**（z 越小越前）。原来建在 z=0 —— 和场上卡同层、
            //    而**手牌在 z 0~0.24（比它还远）**，于是手牌整排**没被压暗、从面板底下亮着**透出来
            //    （2026-09-12 看结算截图发现的；注释里早就担心「卡会透出来」，但 z 给错了）。
            //    `BattleDoors.ZDim` = −0.50：比开门视频(−0.55)和内容(−0.60)**都靠后**，
            //    但比任何一张卡都靠前。
            _dim = ImageQuad.Create(_root, SolidTex(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                    new Vector3(0f, 0f, BattleDoors.ZDim),
                                    LayoutSpace.DesignHeight * 1.05f, new Vector2(0.5f, 0.5f), "dim");
            if (_dim != null) _dim.SetTint(new Color(0f, 0f, 0f, 0.97f));

            // 内容层：标题 / 副标题 / 骷髅 / 提示都挂这下面。
            // 开门视频在播的时候**整层藏起来**（原版那段视频自己就带 VICTORY/DEFEAT/DRAW 字样，
            // 见抽帧 `资料/战斗规格/战斗重建_0827/video_check_0828/frames/Victory_1_8.png`）——
            // 不藏的话我们的标题会叠在视频的字上，同一个词出现两遍。
            var contentGo = new GameObject("content");
            contentGo.transform.SetParent(_root, false);
            _content = contentGo.transform;

            // 结果：原版这块的文字是运行时填的，dump 里 `EndBattlePanel` 子树下没有结果文字节点
            //（只有 Video Image / AllRewardsHolder 那几块）→ **字号是我们挑的**。
            //
            // ⚠️ **纵向位置改过一轮（2026-09-12，加开门视频那轮）**：
            //    第一版把这一整块放在**上方**（标题 260 起），理由是「避开棋盘」——
            //    当时压暗还没铺满（`ImageQuad` 宽高比那个坑），棋盘会透出来。
            //    现在压暗铺满了，而且**原版的位置是知道的**：dump 里 `AllRewardsHolder`（奖励块的锚）
            //    在 **anchoredPosition (0, -260)** → 屏幕坐标 y = 540+260 = **800**，即**中心偏下**。
            //    加上开门视频之后更必须下移：徽章正片占着屏幕中央（抽帧见
            //    `资料/战斗规格/战斗重建_0827/video_check_0828/frames/Victory_1_8.png`），
            //    留在上方会正好糊在徽章上。
            //    所以标题/副标题/骷髅统一挪到 **y ≥ 730**，贴着原版那个锚点带。
            _title = Label.Create(_content, "", Content(960f, 260f), 6, Color.white, new Vector2(0.5f, 0.5f), "end_title");
            _sub = Label.Create(_content, "", Content(960f, 730f), 2, new Color(0.8f, 0.8f, 0.85f),
                                new Vector2(0.5f, 0.5f), "end_sub");

            // 骷髅行：底板 648.1×52.4，三个 64.3×71.2 的骷髅（尺寸是原版 dump 的）
            // ⚠️ 纵向位置按 `AllRewardsHolder` 的锚点 (0,-260) 排（见上面那段）
            _skullPlate = ImageQuad.Create(_content, CardArt.Ui("40k_main_bt_nametag"), Content(960f, 790f),
                                           U(52.4f), new Vector2(0.5f, 0.5f), "skull_plate");
            float step = 64.3f + 12f;
            for (int i = 0; i < _skulls.Length; i++)
            {
                float x = 960f + (i - (_skulls.Length - 1) * 0.5f) * step;
                _skulls[i] = ImageQuad.Create(_content, CardArt.Ui("40k_battle_Win_Skull"), Content(x, 790f),
                                              U(71.2f), new Vector2(0.5f, 0.5f), "skull_" + i);
            }

            // 奖励行（原版 `RewardsHolder` 495.2×49.0 + `HolderRating` 187×45 / Trophy 60.1×60 / RatingText）
            // 🔴 **2026-09-17 用户拍板：不建** —— 奖励与评分都在服务器，单机版不做（见文件头）。只留上面的骷髅行。

            // 结束语下面的操作提示
            Label.Create(_content, "按 R 再来一局", Content(960f, 960f), 2, new Color(0.7f, 0.7f, 0.75f),
                         new Vector2(0.5f, 0.5f), "end_hint");

            // 开门视频（原版 `EndBattleDoors`）。资产不在时它自己退回「没视频」。
            _doors = BattleDoors.Create(_root);
        }

        /// <summary>
        /// 显示结算。
        /// </summary>
        /// <param name="winner">`RuleCore` 那套：0=没结束，1/2=玩家序号+1，3=平局。</param>
        /// <param name="myIndex">我是几号玩家（0 基）。</param>
        /// <param name="minFoeWarlordHealth">这局里**敌方督军降到过的最低生命** —— 骷髅数由它算
        /// （规则书:36，判据只在 `DeckRules.SkullsFor` 一处）。</param>
        /// <param name="rounds">打了几个回合。</param>
        /// <param name="forfeitedBy">谁投降的（`BattleContext.ForfeitedBy`，`-1` = 正常打完）。
        /// 投降和「督军倒下」是**两种结局**（原版 `BattleResult.Forfeit`），副标题得说清是哪种。</param>
        public void Show(int winner, int myIndex, int minFoeWarlordHealth, int rounds, int forfeitedBy = -1)
        {
            Visible = true;
            ShownSkulls = DeckRules.SkullsFor(minFoeWarlordHealth);
            ResultText = winner == 3 ? "平局" : (winner == myIndex + 1 ? "胜利" : "失败");

            // ⚠️ **先激活再写文字** —— 反过来的话 `Label.SetText` 在未激活的物体上跑，
            //    字形网格没重建，标题/副标题/评分全是空白（截图才发现）
            SetVisible(true);

            string r = ResultText;
            _title.SetText(r == "胜利" ? CardText.Phrase("VICTORY") : (r == "失败" ? CardText.Phrase("DEFEAT") : r));
            _sub.SetText(forfeitedBy >= 0
                         ? $"{rounds} 回合   " + (forfeitedBy == myIndex ? "我方投降" : "对方投降")
                         : $"{rounds} 回合   敌方督军最低生命 {minFoeWarlordHealth}");

            for (int i = 0; i < _skulls.Length; i++)
            {
                // ⚠️ 「未点亮 = 半透明」是我们挑的（见文件头注释 2）
                if (_skulls[i] != null)
                    _skulls[i].SetTint(i < ShownSkulls ? Color.white : new Color(1f, 1f, 1f, 0.18f));
            }

            // ---- 开门（原版 `EndBattleDoors`）----
            // 原版 `SetupDoor` 返回 `VideoClip.length + 一个常量`，调用方拿它等 —— 我们照这个约定，
            // 片长由 `BattleDoors` 拿着，`Advance` 推够就停。
            BattleDoors.Result doorRes = ResultText == "胜利" ? BattleDoors.Result.Victory
                                       : ResultText == "失败" ? BattleDoors.Result.Defeat
                                       : BattleDoors.Result.Draw;
            float len = _doors == null ? 0f : _doors.SetupDoor(doorRes);
            bool video = len > 0f;
            if (video) _doors.Play();

            // 奖励跟视频**同时**出。判据不是猜的：`EndBattleDoors.SetupDoor` 里
            // **自己就调了 `EndBattleDoors__ShowRewards(...)`**（反编译 `decomp_out/EndBattleDoors__SetupDoor.c:105`，
            // 前面先是一串把奖励物件 SetActive(false) 的调用），入场靠
            // `DG_Tweening_TweenSettingsExtensions__SetDelay(..., _DAT_1834b2bbc, ...)`（同文件 :194）——
            // 那个常量正好也是 `SetupDoor` 加在 `clip.length` 上的那个。
            // **所以不是「播完才揭晓」**（第一版我做成了播完才揭晓，那是错的，已改）。
            SetContentVisible(true);

            // 但**结果文字要藏**：这段视频自己带着 VICTORY / DEFEAT / DRAW 字样
            //（抽帧 `资料/战斗规格/战斗重建_0827/video_check_0828/frames/Victory_1_8.png`），
            // 而且原版 `EndBattlePanel` 子树里**根本没有结果文字节点** —— 结果字就是那段视频本身。
            // 不藏的话同一个词在屏幕上出现两遍。
            SetTitleVisible(!video);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 推时间。批处理里由 `BattleScene.Step` 手动推（那里没有帧循环）；真机由 `BattleDriver.Update` 每帧推。
        /// **时长按片长自己算，不依赖解码器出没出帧** —— 时序因此是可断言的。
        /// 播完之后 `BattleDoors` 会把解码器 `Pause` 住（**最后一帧留着当底图，不销毁**）。
        /// </summary>
        public void Advance(float dt)
        {
            if (!Visible || _doors == null || !_doors.Playing) return;
            _doors.Advance(dt);
        }

        void SetContentVisible(bool v)
        {
            if (_content != null) _content.gameObject.SetActive(v);
        }

        void SetTitleVisible(bool v)
        {
            if (_title != null) _title.gameObject.SetActive(v);
        }

        /// <summary>自检用：**奖励行应当一个都不建**（用户 2026-09-17 定：单机不做奖励与评分，只留骷髅）。
        /// 按**节点名**数（字段已经删了，数不到才说明真去干净了）。</summary>
        public int RewardRowPieces
        {
            get
            {
                if (_root == null) return 0;
                int n = 0;
                foreach (var t in _root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "reward_plate" || t.name == "trophy" || t.name == "rating") n++;
                return n;
            }
        }

        public void Hide()
        {
            Visible = false;
            ShownSkulls = 0;
            ResultText = "";
            if (_doors != null) _doors.Stop();
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
