// BattleAutoDrive.cs — 「构建后 player 验证」用的**自动打一局**驱动（两种路子）
//
// ── 为什么有它 ──────────────────────────────────────────────────────────
// `项目任务.md` 顶部待办表第 1 行的验收是「在 player 里**打一局对战**」，
// 而 player 里**没有任何输入驱动**（原版有 `SceneJumpShot` 的 `drive`，我们没有）。
// 加上 `-wfdrive`（或 `-wfhuman`）之后，player 会自己把一局打完 ⇒ 「发牌 → 部署 → 攻击 →
// 阵亡 → 回合流转 → 结算/开门视频」整条链**在真包里**都会被跑到、被拍到、被 `Player.log` 记下来。
//
// ── 两种路子（`-wfdrive` / `-wfhuman`）──────────────────────────────────
// | | `-wfdrive`（默认） | `-wfhuman` |
// |---|---|---|
// | 怎么走 | 反复调 `BattleDriver.SimulateAiTurn()` = `SimpleAI.PlayTurn` + 换边 | **我的回合走 UI 那条路**：`SimulatePlay` / `SimulateOpenCommand`+`SimulateCommand`+`SimulateResolve`（**真·攻击方式选择器**）/ `SimulateUseAbility` / `SimulateEndTurn`；对手的回合仍用 `SimulateAiTurn` |
// | 覆盖面 | 引擎 + 渲染整条链 | 上面那些 **UI 入口**（选择器、技能面板、结束回合、换牌面板） |
// | 风险 | 最低 | 高一些（UI 有前提条件，任何一步不合法就**如实记下来**并结束该回合） |
//
// 🔴 **两条路子都走不到的**（**别把它们读成「已验」**）：
//   · **拖拽手势本身** —— `CardInteraction` 的拖拽吃的是真实指针输入，
//     `SimulatePointerAt` 只喂选择器/准星，**不驱动拖拽**；
//   · 卡组编辑场景的按钮（那个场景**根本没有交互组件**，见 `DeckScene.cs`）。
//
// ── 用法 ────────────────────────────────────────────────────────────────
//   WarpforgePlayer.exe -wfscene Battle -wfdrive  -wfshot <目录>/x.png -wfquit 240
//   WarpforgePlayer.exe -wfscene Battle -wfhuman  -wfshot <目录>/x.png -wfquit 240
//   截图落在 `-wfshot` 的同目录下，文件名 `auto_<序号>_<阶段>.png`；
//   没给 `-wfshot` 就不截图，只写日志。跑完**自己退**（`-wfquit` 只是保险丝）。
using System.Collections;
using System.IO;
using CardPresentation;    // ⚠️ `BattleDriver` / `AttackKind` 在这个命名空间里（漏了 = error CS0246）
using RuleEngine;          // `SimpleAI` / `RuleCodes` / `BattleContext`
using UnityEngine;
using UnityEngine.InputSystem;            // 拖拽：注入**真实指针**（`QueueStateEvent`）
using UnityEngine.InputSystem.LowLevel;   // `MouseState`

public class BattleAutoDrive : MonoBehaviour
{
    /// <summary>每一步之间等多久（秒）—— 留时间给画面，也留时间给截图。</summary>
    public float stepGap = 1.0f;

    /// <summary>保险丝：最多驱多少步。跑不完就**如实报**，别静默停在那儿。</summary>
    public int maxSteps = 200;

    /// <summary>我的回合里最多做几个动作（防「什么都做不了却一直转」）。</summary>
    public int maxActionsPerTurn = 30;

    /// <summary>截图目录（`PlayerBoot` 从 `-wfshot` 的目录取；空 = 不截图）。</summary>
    public string shotDir;

    /// <summary>true = 我的回合走**人类那条路**（面板 / 选择器 / 结束回合按钮），见文件头那张表。</summary>
    public bool humanStyle;

    /// <summary>我的回合里要不要做一次**真拖拽**（注入真实指针，见 `DragOnce`）。
    /// 只做一次就够验了 —— 每回合都拖会让整局慢下来。</summary>
    public bool dragOnce = true;

    BattleDriver _drv;
    int _shot;
    bool _running;
    bool _dragDone;

    /// <summary>诊断用：`WF_MANUAL_TWEEN=1` 时补间改成**手动推进**（= 编辑器自检那条路
    /// `CardTween.Mode = Manual` + `CardTween.Advance`）。
    /// 用途：判定「**为什么编辑器一条 DOTween 报错都没有**」—— 见 `PlayerBoot` 里那段注释。</summary>
    float _beatAt;

    void Update()
    {
        if (CardTween.Mode == DG.Tweening.UpdateType.Manual)
            CardTween.Advance(Time.deltaTime);

        // 🆕 **心跳**（2026-09-17 加）—— 跑批时用来分辨两种「日志不动」：
        //    · **心跳还在跳**、别的日志不动 ⇒ 协程在等（逻辑停住了，界面/流程的问题）
        //    · **心跳也停了** ⇒ 主线程被卡住出不了帧 —— 这时连 `PlayerBoot` 的 `-wfquit`
        //      保险丝都不会响（它是每帧轮询的），整个进程只能靠外部 kill。
        //    实测遇到过一次：`-wfhuman` 在换牌之后日志断掉、进程活活挂了 20 分钟没退。
        //    ⇒ **没有这条心跳，「卡死」和「没话说」在日志上长得一模一样。**
        if (!_running) return;
        _beatAt += Time.unscaledDeltaTime;
        if (_beatAt >= 5f)
        {
            _beatAt = 0f;
            Debug.Log($"[AutoDrive·心跳] 主线程活着（换牌中={_drv != null && _drv.InMulligan}）");
        }
    }

    public void Begin()
    {
        if (_running) return;
        _running = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // ---- 1. 等战斗场景就绪 -------------------------------------------------
        // 不是 Battle 场景的话这里永远等不到 —— 所以给 60 s 上限并**报出来**（不静默挂着）
        for (float t = 0f; t < 60f; t += Time.unscaledDeltaTime)
        {
            _drv = Object.FindFirstObjectByType<BattleDriver>();
            if (_drv != null && _drv.Ctx != null) break;
            yield return null;
        }
        if (_drv == null || _drv.Ctx == null)
        {
            Debug.LogError($"[AutoDrive] 60 s 内没等到 BattleDriver/BattleContext —— " +
                           $"这场没驱起来（`-wfdrive`/`-wfhuman` 只对 Battle 场景有意义）");
            yield break;
        }
        Debug.Log($"[AutoDrive] 接上了：我是 P{_drv.MyIndex + 1}，换牌中={_drv.InMulligan}"
                  + $"，路子={(humanStyle ? "人类（走 UI 入口）" : "AI 代打")}");
        DumpSpace();
        yield return Shot("01_接上");

        // ---- 2. 换牌 ----------------------------------------------------------
        yield return Mulligan();
        // ⚠️ **等发牌补间跑完再拍**（`CardFeel.DealIn` 0.55 s + 手牌重排 0.18 s）。
        //    第一版拍完就拍，结果 `auto_02` 里「手牌 5」只画出了 3 张 ——
        //    **看着像「真包里少了两张牌」，其实是我的截图太早**（尺子的假象，本工程第 N 次）。
        yield return new WaitForSeconds(2.0f);
        DumpSpace();
        yield return Shot("02_换牌之后");

        // ---- 3. 一步步驱到结束 -----------------------------------------------
        int n = 0;
        while (!_drv.Ctx.IsOver && n < maxSteps)
        {
            n++;
            int turnBefore = _drv.Ctx.Turn;
            string who = _drv.Ctx.Active == _drv.MyIndex ? "我" : "对手";

            // ⚠️ **`yield return` 不能写在带 `catch` 的 `try` 里**（`error CS1626`）——
            //    所以「会抛异常的引擎调用」一律走不 yield 的普通方法，协程这层只负责推进。
            if (humanStyle && _drv.Ctx.Active == _drv.MyIndex)
            {
                yield return MyTurnByUi();
            }
            else
            {
                bool ok = true;
                try { _drv.SimulateAiTurn(); }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AutoDrive] 第 {n} 步抛异常，停下：{e}");
                    ok = false;
                }
                if (!ok) yield break;
            }

            Debug.Log($"[AutoDrive] {n,3}. {who} 的回合（{turnBefore} → {_drv.Ctx.Turn}）"
                      + $"　督军血 P1={Hp(0)} P2={Hp(1)}");

            if (n == 6) { DumpSpace(); yield return Shot("03_打了六步"); }
            yield return new WaitForSeconds(stepGap);
        }

        // ---- 4. 收尾：如实报 ---------------------------------------------------
        if (_drv.Ctx.IsOver)
        {
            Debug.Log($"[AutoDrive] ✅ 对局结束：共 {n} 步 · 结果 Winner={_drv.Ctx.Winner}"
                      + $"（1=P1 胜 · 2=P2 胜 · 3=平局）· 我是 P{_drv.MyIndex + 1}");
            // 结算面板 / 开门视频要几秒才铺开，等一会儿再拍
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime) yield return null;
            yield return Shot("04_结算");
        }
        else
        {
            Debug.LogWarning($"[AutoDrive] ⚠️ 到保险丝（{maxSteps} 步）还没结束 —— **如实报，别当跑完了**");
            yield return Shot("04_保险丝");
        }

        // ---- 5. 打完了就自己退 ------------------------------------------------
        // 不停在这儿等 `-wfquit`：那一局十来步、二十来秒就打完了，而 `-wfquit 240` 是**保险丝**。
        // ⚠️ 想打完还留着看，就别给 `-wfdrive` —— 这是跑批工具，不是产品逻辑。
        Debug.Log("[AutoDrive] 收工，退出 player");
        Application.Quit(0);
    }

    // ── 换牌 ────────────────────────────────────────────────────────────────
    IEnumerator Mulligan()
    {
        if (!_drv.InMulligan) yield break;

        if (humanStyle)
        {
            // 🔴 **走面板那颗「换」钮**（`SimulateMulliganToggle` 就是它的入口），
            //    顺便验一下「点了再点 = 取消」——那是 AI 那条路完全走不到的。
            _drv.SimulateMulliganToggle(0);
            _drv.SimulateMulliganToggle(0);
            bool ok = _drv.SimulateMulliganToggle(1);
            Debug.Log($"[AutoDrive] 换牌：点了第 1 张（再点取消）、第 2 张（保留标记={ok}）");
            yield return new WaitForSeconds(0.4f);
        }

        for (float t = 0f; _drv.InMulligan && t < 10f; t += Time.unscaledDeltaTime)
        {
            if (_drv.SimulateMulliganDone()) Debug.Log("[AutoDrive] 换牌：完成");
            yield return null;
        }

        // 🔴 **放弃也要出声**（2026-09-17 加）—— 原来这里等不到就**一个字都不留**，
        //    下一个会话只会看到「日志停在换牌那一步」，**分不清是卡死了还是跳过去了**。
        //    （而且换牌没完成的话，`interaction.enabled` 还锁着、后面的回合全线受影响。）
        if (_drv.InMulligan)
            Debug.LogError("[AutoDrive] ⚠️ 10 s 内换牌没完成、面板还开着 —— 如实报，别当换过了"
                           + "（`interaction` 这时是关着的，后面所有步骤都会受影响）");
    }

    // ── 我的回合：走 UI 那条路 ───────────────────────────────────────────────
    //
    // ⚠️ **分成两层是有原因的**：`yield return` **不能出现在带 `catch` 的 `try` 里**
    //    （`error CS1626: Cannot yield a value in the body of a try block with a catch clause`）。
    //    所以「挑一个动作并执行」放在普通方法 `NextUiAction()` 里（那儿可以放心 try/catch），
    //    协程这层只负责「等一下、再挑下一个」。
    IEnumerator MyTurnByUi()
    {
        var ctx = _drv.Ctx;
        int acted = 0;

        // 🔴 **真拖拽**：只做一次（`dragOnce`）—— 这一段是 `-wfdrive`（AI 代打）和
        //    「UI 入口」两条路**都覆盖不到**的：按下 → 抬起 → 拖动 → 松手 → 判落点 → 落位。
        if (dragOnce && !_dragDone)
        {
            _dragDone = true;
            yield return DragOnce();
        }

        while (!ctx.IsOver && ctx.Active == _drv.MyIndex && acted < maxActionsPerTurn)
        {
            string msg; float wait;
            if (!NextUiAction(out msg, out wait)) break;
            acted++;
            Debug.Log($"[AutoDrive·UI] {msg}");
            yield return new WaitForSeconds(wait);
        }

        if (acted >= maxActionsPerTurn)
            Debug.LogWarning($"[AutoDrive·UI] 本回合动作到上限（{maxActionsPerTurn}）就收手 —— 别在这儿空转");

        Debug.Log($"[AutoDrive·UI] 本回合做了 {acted} 个动作 → 结束回合");
        try { _drv.SimulateEndTurn(); }
        catch (System.Exception e) { Debug.LogError($"[AutoDrive·UI] 结束回合抛异常：{e}"); }
    }

    /// <summary>挑一个能做的动作并执行。`false` = 三样都做不了（外层去结束回合）。
    /// **这里不 yield**，所以可以放心 try/catch —— 见 `MyTurnByUi` 头上那条注释。</summary>
    bool NextUiAction(out string msg, out float wait)
    {
        msg = null; wait = 0.35f;
        var ctx = _drv.Ctx;
        try
        {
            // ① 出牌（走 UI 的出牌入口；⚠️ 跳过拖拽手势 —— 那个模拟不了，见文件头）
            int handIdx, slot;
            if (SimpleAI.NextPlay(ctx, out handIdx, out slot))
            {
                int code = _drv.SimulatePlay(handIdx, slot);
                if (code == RuleCodes.OK) { msg = $"出牌：手牌 #{handIdx} → 槽 {slot}"; return true; }
                Debug.LogWarning($"[AutoDrive·UI] 出牌被拒（码 {code}）：手牌 #{handIdx} → 槽 {slot}");
            }

            // ② 攻击 —— **真·攻击方式选择器**（开面板 → 点近战/远程 → 点目标）
            int atkSlot, targetP, targetSlot; bool ranged;
            if (SimpleAI.NextAttack(ctx, out atkSlot, out targetP, out targetSlot, out ranged)
                && _drv.SimulateOpenCommand(atkSlot))
            {
                // ⚠️ 近战还是远程**只有 `SimpleAI.UseRanged` 一处判据**，别在这儿另写一份
                var kind = ranged ? AttackKind.Ranged : AttackKind.Melee;
                if (_drv.HasCommand(kind))
                {
                    _drv.SimulateCommand(kind);
                    int code = _drv.SimulateResolve(targetSlot);
                    msg = $"攻击：槽 {atkSlot} 用 {kind} 打槽 {targetSlot}（码 {code}）";
                    wait = 0.45f;
                    return true;
                }
                Debug.LogWarning($"[AutoDrive·UI] 选择器上没有可点的「{kind}」（槽 {atkSlot}）");
                _drv.SimulateDeselect();      // 别把面板留在屏幕上
                wait = 0.2f;
            }

            // ③ 技能（走面板那条路）
            int abSlot, abTarget;
            if (SimpleAI.NextAbility(ctx, out abSlot, out abTarget))
            {
                int code = _drv.SimulateUseAbility(abSlot, abTarget);
                if (code == RuleCodes.OK) { msg = $"技能：槽 {abSlot} → 目标 {abTarget}"; wait = 0.45f; return true; }
                Debug.LogWarning($"[AutoDrive·UI] 技能被拒（码 {code}）：槽 {abSlot} → {abTarget}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AutoDrive·UI] 动作抛异常，本回合收手：{e}");
        }
        return false;
    }

    // ── 诊断：把「布局尺子」的当前状态打出来 ─────────────────────────────────
    //
    // 🔴 **为什么要有它**：player 里出现过「手牌挤在右下角、编辑器里是底部居中扇形」。
    //    手牌坐标全走 `LayoutSpace.VisibleWidth`，而它取决于 `LayoutSpace.Cam`；
    //    光看代码推不出差别（`cam` 是序列化的、`DesignAspect` 也是 16:9），
    //    ⇒ **量出来**（本工程的规矩：读代码只能确认你想到的那一种情况，量能发现想不到的）。
    void DumpSpace()
    {
        try
        {
            var cam = LayoutSpace.Cam;
            Debug.Log($"[AutoDrive·诊断] {LayoutSpace.Describe()}"
                      + $" · Screen={Screen.width}x{Screen.height}({Screen.currentResolution.width}x{Screen.currentResolution.height})"
                      + $" · VisibleWidth={LayoutSpace.VisibleWidth:F3} · Scale={LayoutSpace.Scale:F3}"
                      + $" · Cam={(cam != null ? cam.name + " aspect=" + cam.aspect.ToString("F3") : "<空>")}");

            var hand = Object.FindFirstObjectByType<HandLayout>();
            if (hand != null)
            {
                var hc = hand.GetComponentsInChildren<CardView>(true);
                Debug.Log($"[AutoDrive·诊断] HandLayout 下面 {hc.Length} 张");
            }

            // 🔴 **把场上所有卡连同父节点倒出来** —— 光看 `HandLayout` 会得出「0 张」这种没用的结论
            //    （换牌阶段卡是挂在换牌面板下面的）。要判「位置错没错」，得知道**它挂在谁下面**。
            var all = Object.FindObjectsByType<CardView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new System.Text.StringBuilder();
            foreach (var c in all)
            {
                var par = c.transform.parent;
                sb.Append($"\n      {c.name,-28} 父={(par != null ? par.name : "<无>"),-22}"
                        + $" 世界=({c.transform.position.x,6:F2},{c.transform.position.y,6:F2})"
                        + $" 缩放={c.transform.localScale.x:F3} 激活={c.gameObject.activeInHierarchy}");
            }
            Debug.Log($"[AutoDrive·诊断] 场上 CardView 共 {all.Length} 个：{sb}");
        }
        catch (System.Exception e) { Debug.LogError($"[AutoDrive·诊断] 抛异常：{e}"); }
    }

    // ── 真拖拽（注入真实指针）───────────────────────────────────────────────
    //
    /// <summary>**真拖拽**：注入真实指针事件（`InputSystem.QueueStateEvent`），走 `CardInteraction`
    /// 那条**和鼠标完全一样**的路 —— 按下 → 移动 → 松手 → 判落点 → 落位。
    ///
    /// **为什么非注入不可**：`CardInteraction` 读的是 `Mouse.current`（真输入设备），
    /// 而 `BattleDriver.SimulatePointerAt` **只喂选择器/准星、不驱动拖拽**（看过实现）。
    /// `QueueStateEvent` 是官方给「运行时往输入管线里塞状态」用的口子 ⇒ 走的还是**同一条管线**，
    /// 不是另写一份判据。
    ///
    /// 🔴 **判据不静默**：拖之前记手牌数 / 场上单位数，拖完再看一次 —— **没变化就如实报失败**。
    /// </summary>
    IEnumerator DragOnce()
    {
        var ctx = _drv.Ctx;
        int handIdx, slot;
        if (!SimpleAI.NextPlay(ctx, out handIdx, out slot))
        {
            Debug.LogWarning("[AutoDrive·拖拽] 这一手 `SimpleAI.NextPlay` 挑不出牌，跳过拖拽用例");
            yield break;
        }
        // 🔴 **用驱动自己的手牌表取卡，别按屏幕 x 猜** ——
        //    `CanDropAtSlot` 是拿 `HandIndexOf(card)` **反查手牌下标**再问引擎的
        //    （`RuleCore.CanPlayCard`，含费用）。猜错一张就会拖到**付不起的牌**上 ⇒ 判「落点不合法」⇒ 回弹，
        //    而日志上看着一切正常（指针位置对、`正在拖=True`、卡也到位了）。
        //    第一版就是按「名字前缀 `Hand_` + 按 x 排序」猜的，**卡在了这一条**。
        var card = _drv.HandViewAt(handIdx);
        var board = Object.FindFirstObjectByType<BoardLayout>();
        if (card == null || board == null || LayoutSpace.Cam == null)
        {
            Debug.LogWarning($"[AutoDrive·拖拽] 缺东西（卡={card != null} board={board != null} "
                           + $"cam={LayoutSpace.Cam != null}），跳过拖拽用例");
            yield break;
        }

        int handBefore = ctx.Players[_drv.MyIndex].Hand.Count;
        int unitsBefore = _drv.MyUnits.Count;
        Vector3 from = card.transform.position;
        Vector3 to = board.transform.TransformPoint(board.SlotPosition(slot));
        Debug.Log($"[AutoDrive·拖拽] 拿手牌第 {handIdx} 张（{card.name}）→ 槽 {slot}"
                  + $"，世界 ({from.x:F2},{from.y:F2}) → ({to.x:F2},{to.y:F2})");

        InjectMouseAt(from, true);             // 按下（落在卡上）
        yield return null;
        yield return null;

        // 🔴 **先判「注入到底有没有到」** —— 不然「拖不动」分不清是输入没进去还是拖拽本身坏，
        //    而那两件事的修法完全不同（这条是本工程反复强调的「先确认尺子」）。
        var inter = Object.FindFirstObjectByType<CardInteraction>();
        if (Mouse.current == null)
            Debug.LogError("[AutoDrive·拖拽] ❌ `Mouse.current` 是空的 —— 注入根本进不去，"
                         + "先查 player 的 Active Input Handling / InputSystem 设置");
        else
        {
            var mp = Mouse.current.position.ReadValue();
            var want = LayoutSpace.Cam.WorldToScreenPoint(new Vector3(from.x, from.y, 0f));
            Debug.Log($"[AutoDrive·拖拽] 按下后：InputSystem 读到指针 ({mp.x:F1},{mp.y:F1})，"
                      + $"期望 ≈({want.x:F1},{want.y:F1})"
                      + $"，交互层={(inter != null)}，正在拖={(inter != null && inter.IsDragging)}");
        }

        for (int i = 1; i <= 6; i++)           // 拖过去（分几步，让 hover / 邻牌让位都跑到）
        {
            InjectMouseAt(Vector3.Lerp(from, to, i / 6f));
            yield return null;
        }

        // 🔴 **闭环拖到位** —— 别用「世界→屏幕」开环算。
        //    实测开环会差 **~0.78 世界单位**（84 px）：我给的屏幕坐标和
        //    `LayoutSpace.ScreenToWorld` 那个往返换算**不严丝合缝**，而卡最终停在
        //    `指针 + _grabOffset`，差一点就被 `ResolveDrop` 判「落点不合法」⇒ 回弹。
        //    闭环（按**卡的残差**反过来修指针）不依赖那个换算，人也正是这么拖的：看着卡，挪鼠标，直到它到位。
        Vector3 pointer = to;
        int frames = 0;
        bool diverged = false;
        for (; frames < 80; frames++)
        {
            var cur = card.transform.position;            // ⚠️ 判的是**卡**在哪，不是指针在哪
            float ex = to.x - cur.x, ey = to.y - cur.y;
            if (ex * ex + ey * ey < 0.02f) break;         // 0.14 世界单位以内就算到了

            // 🔴 **阻尼**（2026-09-17 修）：原来是把**全部残差**加到指针上。
            //    可卡是**滞后**跟指针的（`UpdateDrag` 每帧只走 `1-exp(-20·dt)` ≈ 28%），
            //    于是指针一路冲到可见区外（实测 (-16.18, 4.73)，可见区才 13.33×10）——
            //    **卡反而更追不上**，80 帧一次都没收敛（残差 4.45）。
            //    取 0.6：递推式的特征值平方 = 1-k < 1，**任何帧率下都收敛**。
            pointer += new Vector3(ex, ey, 0f) * 0.6f;

            // 发散保护：指针跑出可见区就**停手并报出来**，别继续喂野值
            //（喂下去只会让卡被拽飞，然后在别处表现成「落点不合法」—— 查了两轮才找到这里）
            if (Mathf.Abs(pointer.x) > LayoutSpace.VisibleWidth
                || Mathf.Abs(pointer.y) > LayoutSpace.VisibleHeight)
            { diverged = true; break; }

            InjectMouseAt(pointer);
            yield return null;
        }
        if (diverged)
            Debug.LogError($"[AutoDrive·拖拽] 指针跑到可见区外（({pointer.x:F2},{pointer.y:F2})，"
                         + $"可见区 {LayoutSpace.VisibleWidth:F2}×{LayoutSpace.VisibleHeight:F2}）—— "
                         + "收敛发散了，如实报。**这一拖的结果不可信，别当成「落点不合法」。**");
        var fin = card.transform.position;
        Debug.Log($"[AutoDrive·拖拽] 卡到位：卡在 ({fin.x:F2},{fin.y:F2})，目标 ({to.x:F2},{to.y:F2})，"
                  + $"残差 {Mathf.Sqrt((to.x-fin.x)*(to.x-fin.x)+(to.y-fin.y)*(to.y-fin.y)):F3}，用了 {frames} 帧");
        // 🔴 **松手前把那几道闸逐条拆开量** ——
        //    光看「落点不合法」分不清是**命中不了格位**、**引擎说不能打**、还是**这格本回合摆过牌**。
        //    （这几条的修法完全不同；本工程反复强调的「先确认尺子」。）
        // 🔴 **2026-09-17 修**：这里原来**手抄了一份判据**（自己调 `TryResolveSlot` + `CanDropAtSlot`），
        //    结果探针报「全绿」而 `Release` 判「不合法」—— 因为真值走的是 `ResolveDrop`，
        //    还有**第四道 `_placed`**。探针与真值各写一份 ⇔ 必然分叉（本工程的老毛病）。
        //    现在两边都读 `CardInteraction.DropReject`（判据正本），`ExplainDrop` 只是它的报话层。
        if (inter != null)
            Debug.Log($"[AutoDrive·拖拽] 松手前拆解：{inter.ExplainDrop(card.transform.position, card)}"
                      + $" · 当前行动方={ctx.Active} · 我是={_drv.MyIndex}");

        InjectMouseAt(pointer, false);         // 松手（落在格位上）
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.6f); // 等落位补间

        int handAfter = ctx.Players[_drv.MyIndex].Hand.Count;
        int unitsAfter = _drv.MyUnits.Count;
        if (handAfter < handBefore || unitsAfter > unitsBefore)
            Debug.Log($"[AutoDrive·拖拽] ✅ **真拖拽成功**：手牌 {handBefore}→{handAfter}，"
                      + $"场上单位 {unitsBefore}→{unitsAfter}");
        else
            Debug.LogError($"[AutoDrive·拖拽] ❌ 拖完**什么都没发生**（手牌 {handBefore}→{handAfter}，"
                         + $"单位 {unitsBefore}→{unitsAfter}）—— **如实报**，别当验过了");
        yield return Shot("05_拖拽之后");
    }

    /// <summary>把指针喂到某个世界坐标（位置 + 左键按没按）。</summary>
    /// `down` 从 false→true 的那一帧，`Mouse.current.leftButton.wasPressedThisFrame` 就是 true，
    /// 所以「按下」要**先塞一帧按下**再继续塞着不放；「松开」就是塞一帧不按。</summary>
    static void InjectMouseAt(Vector3 world, bool down = true)
    {
        var cam = LayoutSpace.Cam;
        if (cam == null || Mouse.current == null) return;
        var sp = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
        var st = new MouseState { position = new Vector2(sp.x, sp.y) };
        if (down) st.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(Mouse.current, st);
    }

    int Hp(int player)
    {
        var p = _drv.Ctx.Players[player];
        var w = p != null ? p.Warlord : null;
        return w != null ? w.Health : -1;
    }

    IEnumerator Shot(string what)
    {
        if (string.IsNullOrEmpty(shotDir)) yield break;
        try
        {
            Directory.CreateDirectory(shotDir);
            var path = $"{shotDir}/auto_{++_shot:00}_{what}.png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[AutoDrive] 截图 → {path}");
        }
        catch (System.Exception e) { Debug.LogError($"[AutoDrive] 截图失败：{e.Message}"); }
        // `CaptureScreenshot` 在帧末才写盘 —— 多等几帧，否则文件是空的
        for (int i = 0; i < 5; i++) yield return new WaitForEndOfFrame();
    }
}
