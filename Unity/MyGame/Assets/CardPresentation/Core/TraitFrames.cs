// TraitFrames.cs — 单位身上那套「状态框」（trait frame）
//
// 原版判据（2026-09-29 逐句读过全量反编译；正本 → `资料/待办判据_战场与战斗视图.md` **Q8 第 3 条**）：
//   · **两本字典挂在 `BattleCardUI` 上**：`TraitFrameEffectsPlay`（偏移 `+0x2d0`）与
//     `TraitFrameEffectsTrigger`（`+0x2d8`），值 = 实例化出来的那个 GameObject。
//   · **分工按 `CardTrait.EffectTriggerType`**：
//       `TraitFrameEffectsPlay`    ← **OnPlay(2)**   （`BattleCardUI__DisplayStatusAnim.c:120-126`）
//       `TraitFrameEffectsTrigger` ← **Trigger(3) 与 TriggerOnPlay(6)**（`…__DisplayTriggerAnim.c:71`）
//   · `DisplayStatusAnim(trait)`：框实例化在**卡的 `effectsAnchor`** 下
//     （`BattleManager.GetAnimTransform` case 0 返回 `BattleCardUI.effectsAnchor`，`:59-63`；
//      `CreateAnim` 里 `Instantiate(prefab, anchor.position, prefab.localRotation, anchor)`、
//      **缩放随 prefab 自己**）。播完的回调把 GO **写进字典** + `localPosition = Vector3.zero`。
//     已在字典里 ⇒ 只归位 + `SetActive(true)`，**不重播**。
//   · `DisplayTriggerAnim(trait)`：**只有字典里没有才播**；回调**只对 `TriggerOnPlay(6)`** 写进字典
//     （`Trigger(3)` 是纯放一次、不进表）。
//   · `RemoveStatusAnim(trait)`：两本字典各自「有就 `Exit()`（没 `AnimFXController` 就 `Destroy`）+ 移除」。
//   · `CleanStatusAnims(clearAll)`：清掉「卡上**已经不再持有**的 trait」的框（`clearAll` 时全清）；
//     另外**无条件**销毁 `codexFrame` 与 `activeQuestFrame`（**这两个不在两本字典里**）。
//   · 存活时间：框 prefab 上带 `AnimFXController`，`destroyTime > 0 && !preventDestroy` 时自己 `Destroy`。
//
// 🔴 **「哪张框配哪个 prefab」在原版本地【说不清】**（`BattleAnims.json` 里
//    `codexActivePrefab` / `stealthPrefab` / `reflectedPrefab` / `summonSickness` 全是空引用 `{FileID:0,PathID:0}`，
//    `CardTrait` 那份 SO 本体全库 0 命中）⇒ 下面那张绑定表是**按名字硬绑的、是我们的口径**，
//    不是从原版证出来的（每一个都标了它的来路）。⚠️ **不许把这张表当成「原版就是这样」**。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;
using WarpforgeVFX;

namespace CardPresentation
{
    /// <summary>一张卡身上的「状态框」那一套。**每张卡视图一个**（`CardView` 建体时挂上）。</summary>
    public class TraitFrames : MonoBehaviour
    {
        // ==================================================================
        //  绑定表（trait → 我们特效库里的框 prefab 名）—— **我们的口径**，见文件头
        // ==================================================================
        //  来路分三种，逐条标：
        //   · 「同名直取」= 原版那件 prefab 的名字与我们要挂的那个 trait 语义一致、且**已在效果库里**；
        //   · 「待接」= 原版那件在解包资源里有（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/`），
        //     但**没进我们的效果库**（效果库只导出「被特效引用」的 prefab，而这几件只被 `CardAnim` 引用）
        //     ⇒ 要另做一次导入，**记着别忘**（`项目任务.md` §三 第 5 条那两条待办里的第一条）；
        //   · 「判据空」= 原版自己那几项引用就是空的（见文件头最后一段）⇒ 按铁律 11 第 ① 种**结案**。

        /// <summary>按 **OnPlay(2)** 归口的那本（原版 `TraitFrameEffectsPlay`）。
        /// 触发时机 = **这个 trait 被加到这张卡上**（原版挂在 `CardScript.AddEffect` 那几个分支上）。</summary>
        static readonly Dictionary<string, string> PlayFrames = new Dictionary<string, string>
        {
            // 「同名直取」——两件都在效果库里（`效果库报告.tsv` 有行）
            { KeywordTable.Stealth, "StealthEffect" },
            { KeywordTable.Codex,   "CodexEffect"  },
            // 「走地址表」——原版 `ExecuteSwarm` 写的是 `swarmFrame`，而那一族的 CardAnim 叫
            // `SwarmTraitFromCode`；`数据/索引/anim_address_map.json` 解出它指向 prefab
            // **`Swarm_Trigger_OnTarget`**（那张表由 `工具/gen_anim_address_map.py` 从各 bundle 的
            // `m_Container` 建出来）。⚠️ 「那个 CardAnim 就是 `swarmFrame` 的源」这一步**是推断**，如实标。
            { KeywordTable.Swarm,   "Swarm_Trigger_OnTarget" },
        };

        /// <summary>按 **Trigger(3) / TriggerOnPlay(6)** 归口的那本（原版 `TraitFrameEffectsTrigger`）。
        /// 触发时机 = 那个关键词**真的触发了**（我们这边 = `EvtKind.Trigger`，`Keyword` 字段带规范键）。</summary>
        static readonly Dictionary<string, string> TriggerFrames = new Dictionary<string, string>
        {
            // 「同名直取」
            { "oath", "Oath_Effect" },
        };

        /// <summary>原版有、我们**暂时挂不上**的那几个（如实记：不是「不做」，是缺一件前置）。
        /// 键 = 原版那个具名字段（`…Frame 0xNNN`），值 = 缺什么。</summary>
        public static readonly string[] MissingFrames =
        {
            "vanguardFrame（原版 prefab `Vanguard Frame Animated VAT` 在解包资源里有、**没进效果库**）",
            "bloodThirstFrame（prefab `BloodThirstEffect` 在库里，但**我们引擎里没有 `bloodthirst` 这个关键词**）",
            "summonSicknessFrame / stealthFrame / reflectedFrame（原版 `BattleAnims.json` 那几项是**空引用**）",
            "perfectionFrame / courageFrame（原版桩里**没有写方** ⇒ 判据空）",
            "codexFrame / activeQuestFrame（原版不在两本字典里、无条件销毁；我们按同一语义处理 —— 见 `Clean`）",
        };

        /// <summary>查一个 trait 该用哪张框（`null` = 没有绑定）。**判据只此一处**。</summary>
        public static string PrefabFor(string trait, bool trigger)
        {
            var t = Norm(trait);
            if (t.Length == 0) return null;
            var table = trigger ? TriggerFrames : PlayFrames;
            string s;
            return table.TryGetValue(t, out s) ? s : null;
        }

        // ==================================================================
        //  两本字典（原版 `TraitFrameEffectsPlay` / `TraitFrameEffectsTrigger`）
        // ==================================================================

        readonly Dictionary<string, WarpforgeEffectPlayer> _play =
            new Dictionary<string, WarpforgeEffectPlayer>();
        readonly Dictionary<string, WarpforgeEffectPlayer> _trigger =
            new Dictionary<string, WarpforgeEffectPlayer>();

        CardView _card;
        Transform _anchor;

        /// <summary>框挂在哪个节点下 —— 原版是 `BattleCardUI.effectsAnchor(+0x1A0)`。
        /// ⚠️ **那个 anchor 的局部位置我们没读出来**（如实记）⇒ 我们的 `effectsAnchor` 建在
        /// **卡根原点**（卡中心），框的世界位置就取它。</summary>
        public Transform Anchor { get { return _anchor; } }

        /// <summary>给一张卡视图挂上这一套（已经挂过就返回原来那个）。</summary>
        public static TraitFrames Attach(CardView card)
        {
            if (card == null) return null;
            var f = card.GetComponent<TraitFrames>();
            if (f != null) return f;
            f = card.gameObject.AddComponent<TraitFrames>();
            f._card = card;
            var go = new GameObject("effectsAnchor");
            go.transform.SetParent(card.transform, false);
            go.transform.localPosition = Vector3.zero;      // ⚠️ 我们挑的，见 `Anchor` 的注释
            f._anchor = go.transform;
            return f;
        }

        // ---- 断言用 ----
        public int PlayCount { get { return _play.Count; } }
        public int TriggerCount { get { return _trigger.Count; } }
        public bool HasPlayFrame(string trait) { return _play.ContainsKey(Norm(trait)); }
        public bool HasTriggerFrame(string trait) { return _trigger.ContainsKey(Norm(trait)); }
        /// <summary>自检用：某一本里现在挂着哪些 trait。</summary>
        public List<string> Traits(bool trigger)
        {
            var keys = trigger ? _trigger.Keys : _play.Keys;
            return new List<string>(keys);
        }

        static string Norm(string s) { return string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant(); }

        // ==================================================================
        //  四个方法（名字与原版一一对应）
        // ==================================================================

        /// <summary>原版有框、我们**挂不上**的那几个 trait（`MissingFrames` 的键那一侧，用来出声）。
        /// 其余没绑定的 trait 原版也没有框 ⇒ 静默是对的。</summary>
        static readonly HashSet<string> MissingTraits = new HashSet<string>
        { "vanguard", "bloodthirst", "summonsickness", "reflected", "perfection", "courage" };
        static readonly HashSet<string> _warnedMissing = new HashSet<string>();

        /// <summary>把「这一轮卡上持有的关键词」报进来 —— **我们对差出「新加上的 trait」的入口**。
        /// 原版不做对差（它挂在 `CardScript.AddEffect` 那几个分支上）；我们引擎不发「关键词被授予」事件，
        /// 所以在这里按集合对差（`RefreshAll` 就在动作结算之后跑，时机与原版一致）。
        /// 返回这一轮**新挂上的框**数（断言用）。</summary>
        public int SyncKeywords(IEnumerable<string> current)
        {
            int added = 0;
            var now = new HashSet<string>();
            if (current != null)
                foreach (var k in current)
                {
                    now.Add(k);
                    if (_prev.Contains(k)) continue;
                    if (DisplayStatus(k)) added++;
                }
            _prev.Clear();
            foreach (var k in now) _prev.Add(k);
            Clean(now, false);                  // 不再持有的 ⇒ 清掉（原版 `CleanStatusAnims(false)` 那条谓词）
            return added;
        }

        readonly HashSet<string> _prev = new HashSet<string>();

        /// <summary>原版 `BattleCardUI.DisplayStatusAnim(trait)` —— **trait 被加到这张卡上**时演。
        /// 已在表里 ⇒ 只归位 + 打开，**不重播**（原版 `:157-196`）。</summary>
        public bool DisplayStatus(string trait)
        {
            var key = Norm(trait);
            if (key.Length == 0) return false;

            WarpforgeEffectPlayer old;
            if (_play.TryGetValue(key, out old))
            {
                if (old != null)
                {
                    old.transform.localPosition = Vector3.zero;
                    old.gameObject.SetActive(true);
                }
                return false;                       // 「已有 ⇒ 不重播」也是原版的行为，不是失败
            }

            var name = PrefabFor(key, false);
            if (name == null)
            {
                if (MissingTraits.Contains(key) && _warnedMissing.Add(key))
                    Debug.LogWarning($"[TraitFrames] 「{key}」在原版有状态框，但我们**挂不上**："
                                   + string.Join(" · ", MissingFrames)
                                   + " —— 如实出声，不静默（判据 → `资料/待办判据_战场与战斗视图.md` Q8 第 3 条）");
                return false;
            }
            var p = Play(name);
            if (p == null) return false;
            _play[key] = p;
            return true;
        }

        /// <summary>原版 `BattleCardUI.DisplayTriggerAnim(trait)` —— 那个关键词**真的触发了**时演。
        /// **只有表里没有才播**（原版 `:88-91`）。</summary>
        public bool DisplayTrigger(string trait)
        {
            var key = Norm(trait);
            if (key.Length == 0) return false;
            if (_trigger.ContainsKey(key)) return false;      // 已有 ⇒ 不重播

            var name = PrefabFor(key, true);
            if (name == null) return false;
            var p = Play(name);
            if (p == null) return false;
            _trigger[key] = p;
            return true;
        }

        /// <summary>原版 `BattleCardUI.RemoveStatusAnim(trait)` —— 两本表各自「有就 `Exit()` + 移除」。</summary>
        public void Remove(string trait)
        {
            var key = Norm(trait);
            Drop(_play, key);
            Drop(_trigger, key);
        }

        /// <summary>原版 `BattleCardUI.CleanStatusAnims(clearAll)` —— 清掉「卡上**已经不再持有**的 trait」
        /// 的框（`clearAll` = 全清，卡离场时走这条）。逐条判据见文件头。
        /// ⚠️ 原版还会**无条件**销毁 `codexFrame` / `activeQuestFrame`（那两个不在两本字典里）——
        /// 我们这两个走的就是字典那条路（`codex` 在 `PlayFrames` 里），语义等价。</summary>
        public void Clean(ICollection<string> stillHas, bool clearAll)
        {
            CleanOne(_play, stillHas, clearAll);
            CleanOne(_trigger, stillHas, clearAll);
        }

        void CleanOne(Dictionary<string, WarpforgeEffectPlayer> dict,
                      ICollection<string> stillHas, bool clearAll)
        {
            if (dict.Count == 0) return;
            var drop = new List<string>();
            foreach (var kv in dict)
                if (clearAll || stillHas == null || !stillHas.Contains(kv.Key)) drop.Add(kv.Key);
            for (int i = 0; i < drop.Count; i++) Drop(dict, drop[i]);
        }

        static void Drop(Dictionary<string, WarpforgeEffectPlayer> dict, string key)
        {
            WarpforgeEffectPlayer p;
            if (!dict.TryGetValue(key, out p)) return;
            dict.Remove(key);
            Exit(p);
        }

        /// <summary>原版 `RemoveStatusAnim` / `CleanStatusAnims` 的收尾：有 `AnimFXController` 就 `Exit()`，
        /// 否则 `Destroy()`。我们这边对应 `WarpforgeEffectPlayer.Exit()` / `Kill()`。</summary>
        static void Exit(WarpforgeEffectPlayer p)
        {
            if (p == null) return;
            if (p.IsPlaying) p.Exit();
            else p.Kill();
        }

        /// <summary>建一张框（原版 = `Instantiate(prefab, effectsAnchor.position, prefab.localRotation, effectsAnchor)`
        /// + **缩放随 prefab**）。</summary>
        WarpforgeEffectPlayer Play(string prefabName)
        {
            if (_anchor == null) return null;
            // `scale: 1` = 不额外缩放（`Play` 里那个 scale 是**乘数**，乘在 prefab 作者的缩放之上）
            var p = WarpforgeEffectPlayer.Play(prefabName, _anchor, Vector3.zero, 1f);
            if (p == null)
                Debug.LogWarning($"[TraitFrames] 效果库里没有 `{prefabName}` ⇒ 这张状态框挂不上"
                               + "（是不是重导过 prefab 没重建效果库？Tools > Warpforge > 生成效果库）");
            return p;
        }
    }
}
