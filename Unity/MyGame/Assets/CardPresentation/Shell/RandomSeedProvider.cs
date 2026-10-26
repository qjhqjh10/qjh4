// RandomSeedProvider.cs —— 原版 `RandomSeedProvider` 的逐句复刻（「受控种子」工具类）
//
// ============================================================ 判据（第一权威 = 反编译方法体）
//  · 🥇 `d:/2/tools/decomp_full/RandomSeedProvider__{InitSeed,GetRandom,GetSeedState,SetSeedState,ResetSeed,GenerateRandomSeed}.c`
//    + `RandomSeedProvider.ControlledSeedState__{ctor,GetRandom,Reset}.c` + `RandomSeedProvider__.cctor.c`（方法体全在）
//  · 🥈 交叉：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RandomSeedProvider.cs`
//  · 字段/枚举值：`d:/2/tools/il2cpp_out/dump.cs:101069-101107`
//      `SeedOptions{ Random = 0, Fixed = 5, Offset = 10 }` ·
//      `ControlledSeedState{ startValue @0x10, currentRoll @0x14 }` ·
//      `static readonly Dictionary<string, ControlledSeedState> seedStates`（`.cctor` = `new Dictionary<,>()`）
//
// ============================================================ 原版调用方（现读）
//  · **唯一**：`Everguild.LiveOps/DraftEventBase.cs` 的 `GetRandomWarlords()`（:2353-2932）里三处 ——
//    `:2560` = `InitSeed(seedID, SeedOptions.Fixed, cardDeck.deckHero)`；
//    `:2610` / `:2858` = `GetRandom(seedID, 0, 候选数)`（每取一张就从候选池里移除 ⇒ **一条顺序流**）。
//    （🥈 产物里 `seedID` 那条字符串**丢了**，读成 `(string)0` —— 本地解不出来，⛔ 别猜一个名字）；
//  · 我们自己的轮抽选将（`Shell/DraftModeWindow.RollWarlords`）**没有**接这一族 —— 它用逐位 `System.Random`
//    （文件头写明「依赖关系照 `RollRound` 那一条」）。机制差异：原版是**一条顺序流**（seed = 选中卡组的 deckHero），
//    我们是**逐位独立种子** ⇒ 同 seed 下两侧选出的战将不同（形状/可复现性一致）。要不要接回来 = 需先有探针对拍。
//  · `GenerateRandomSeed()` 在**产物里 0 个调用点** —— ⚠️ 反编译只覆盖约 29% 的方法体
//    ⇒ **不能**据此判「原版是死代码」（`资料/完全审查与完全复刻_方案.md` §2.4 ①）。
//
// ============================================================ 为什么这里用 `UnityEngine.Random`
//  原版这整套就长在 `UnityEngine.Random` 上：`GetRandom` = **存全局 state → `InitState(startValue + currentRoll)`
//  → `Range` → 还原 state → `currentRoll++`**（`ControlledSeedState__GetRandom.c:14-22` 逐句）。
//  等价复刻 = 必须用同一个 RNG（同 seed ⇒ 同数列）。工程红线「对局必须可复现、引擎里只用 `System.Random`」
//  管的是 **`RuleEngine` 的对局结算**；本类是 **Shell 侧**（轮抽 UI），与原版同 RNG 才有复刻意义 ——
//  先例：`Battle/CardChoicePanel.cs:695`（「随机选是 UI 侧的 `UnityEngine.Random`，原版就是」）。
//  ⚠️ 与 `Shell/DraftPacks.cs` 的关系：那边**已有**自己的一套 `System.Random` 抽包口径（文件头写明「具体抽中的包
//  与原版不同、复刻的是形状与确定性」）。本类**不改那条路**，两条口并存、各自有出处；将来若要「与原版逐包一致」，
//  改的是 `DraftPacks.RollRound`（把 `InitSeed/GetRandom` 换进来），不是这里。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RandomSeedProvider`（`dump.cs:101104`，`MonoBehaviour`）—— 但**原版一个实例字段都没有**，
    /// 全部静态 ⇒ 我们落成 `static` 类（无 `MonoBehaviour`，⛔ 别挂节点）。</summary>
    public static class RandomSeedProvider
    {
        /// <summary>原版 `RandomSeedProvider.SeedOptions`（`dump.cs:101069`，值 0 / 5 / 10 —— **不是 0/1/2**）。</summary>
        public enum SeedOptions
        {
            /// <summary>`startValue = (int)DateTime.Now.Ticks`（每次都不一样）。</summary>
            Random = 0,
            /// <summary>`startValue = seedInt`（定值 ⇒ 同 seed 同数列）。</summary>
            Fixed = 5,
            /// <summary>`startValue = (int)(DateTime.Now.Ticks + seedInt)`（时间 + 偏移）。</summary>
            Offset = 10,
        }

        /// <summary>原版 `RandomSeedProvider.ControlledSeedState`（`dump.cs:101080`，`[Serializable]`）。
        /// 两个字段：`startValue @+0x10` · `currentRoll @+0x14`。</summary>
        [Serializable]
        public class ControlledSeedState
        {
            // 原版两个都是 private（`dump.cs:101082-101086`），没有访问器 ⇒ 我们也不给 setter，
            // 只给**只读**口（自检/演练要看它，见下面 `StartValue` / `CurrentRoll`）。
            int _startValue;      // 原版 `startValue` @0x10
            int _currentRoll;     // 原版 `currentRoll` @0x14

            /// <summary>判据：`RandomSeedProvider.ControlledSeedState__.ctor.c:14-34` 三分支
            /// （`0` = `(int)DateTime.Now.Ticks` · `5` = `seedInt` · `10` = `(int)(Ticks + seedInt)`）。
            /// ⚠️ `currentRoll` 原版不显式赋 ⇒ 默认 0（`Reset()` 也是写 0）。</summary>
            public ControlledSeedState(SeedOptions seedOptions, int seedInt)
            {
                switch (seedOptions)
                {
                    case SeedOptions.Random:
                        _startValue = unchecked((int)DateTime.Now.Ticks);
                        break;
                    case SeedOptions.Fixed:
                        _startValue = seedInt;
                        break;
                    case SeedOptions.Offset:
                        _startValue = unchecked((int)(DateTime.Now.Ticks + seedInt));
                        break;
                    default:
                        // 原版：三分支都不命中 ⇒ 一个字段都不写（`startValue` 保持 0）。
                        // 出声（本项目红线：不许静默），但**不**改行为。
                        Debug.LogWarning("[RandomSeed] `ControlledSeedState` 收到不认识的 SeedOptions=" + (int)seedOptions
                                       + "（原版只有 0 / 5 / 10）⇒ startValue 保持 0（照原版那条落空分支）");
                        break;
                }
            }

            /// <summary>判据：`RandomSeedProvider.ControlledSeedState__GetRandom.c:14-22`，
            /// 逐句 = 存全局 `Random.state` → `InitState(startValue + currentRoll)` → `Random.Range(min,max)`
            /// → 还 `Random.state` → `currentRoll++`。
            /// ⚠️ 原版 `Range(int,int)` 是 **[min, max)** 左闭右开（Unity 语义）。</summary>
            public int GetRandom(int min, int max)
            {
                var saved = UnityEngine.Random.state;
                UnityEngine.Random.InitState(unchecked(_startValue + _currentRoll));
                int result = UnityEngine.Random.Range(min, max);
                UnityEngine.Random.state = saved;
                _currentRoll = unchecked(_currentRoll + 1);
                return result;
            }

            /// <summary>同上一份，`Range(float,float)` 是 **[min, max]**（Unity 的 float 重载与 int 重载**区间不同**，
            /// 照原版各走各的：`.c:14-22` 两档逐句同形、只差 `Range` 的重载）。</summary>
            public float GetRandom(float min, float max)
            {
                var saved = UnityEngine.Random.state;
                UnityEngine.Random.InitState(unchecked(_startValue + _currentRoll));
                float result = UnityEngine.Random.Range(min, max);
                UnityEngine.Random.state = saved;
                _currentRoll = unchecked(_currentRoll + 1);
                return result;
            }

            /// <summary>判据：`RandomSeedProvider.ControlledSeedState__Reset.c:5` —— 全篇一句 `currentRoll = 0`。
            /// ⚠️ **不重掷 `startValue`** ⇒ 重置后**同一串数原样重来**（这是原版的设计：可回放）。</summary>
            public void Reset() { _currentRoll = 0; }

            /// <summary>我们加的**只读**口（原版 private、无访问器；只为自检/演练能看，⛔ 不参与行为）。</summary>
            public int StartValue { get { return _startValue; } }

            /// <summary>我们加的**只读**口（同上）。</summary>
            public int CurrentRoll { get { return _currentRoll; } }
        }

        // ============================================================ 静态那一半

        /// <summary>原版 `seedStates`（`.cctor` = `new Dictionary<string, ControlledSeedState>()`）。</summary>
        static readonly Dictionary<string, ControlledSeedState> seedStates
            = new Dictionary<string, ControlledSeedState>();

        /// <summary>判据：`RandomSeedProvider__InitSeed.c:18-64` —— 建一个新 state（`startValue` 按 SeedOptions），
        /// 然后 **`seedStates[seedID] = state`**（原版 `TryGetValue` 后 `Add` / `set_Item` 两支，净效果 = 覆盖写）。
        /// ⚠️ 原版先 `new ControlledSeedState(Random, seedInt)` 再按 SeedOptions **重写** `startValue`
        /// （`:23-43`）⇒ 净效果 = 直接按 SeedOptions 构造；**两处 `DateTime.Now` 各调一次**，
        /// `Offset` 档以**第二次**的时刻为准 ⇒ 我们只构造一次，差一两个 tick，无观测差异。</summary>
        public static ControlledSeedState InitSeed(string seedID, SeedOptions seedOptions = SeedOptions.Random,
                                                  int seedInt = 0)
        {
            var state = new ControlledSeedState(seedOptions, seedInt);
            seedStates[seedID] = state;
            return state;
        }

        /// <summary>判据：`RandomSeedProvider__GetRandom.c`（int 重载）—— 字典里没有 ⇒
        /// **`CustomDebug.Log("Seed ID not found. Creating new seed")`**（原文照抄）+ `InitSeed(seedID)`（默认 Random,0），
        /// 再走 `state.GetRandom(min,max)`。</summary>
        public static int GetRandom(string seedID, int min, int max)
        {
            var st = StateFor(seedID);
            return st.GetRandom(min, max);
        }

        /// <summary>同上一份（float 重载，判据同一份 `.c`：`RandomSeedProvider__GetRandom` 的第二个重载）。</summary>
        public static float GetRandom(string seedID, float min, float max)
        {
            var st = StateFor(seedID);
            return st.GetRandom(min, max);
        }

        /// <summary>两个 `GetRandom(seedID,…)` 共用的「取 state、缺了就建」那一段
        /// （原版两份各写一遍，逐句相同 ⇒ 我们合成一处，行为不变）。</summary>
        static ControlledSeedState StateFor(string seedID)
        {
            ControlledSeedState st;
            if (!seedStates.TryGetValue(seedID, out st))
            {
                Debug.Log("Seed ID not found. Creating new seed");   // 原版文案，逐字
                st = InitSeed(seedID);
            }
            return st;
        }

        /// <summary>判据：`RandomSeedProvider__GetSeedState.c:18-23` —— `TryGetValue` 后把
        /// **`(state, found)`** 一起交回（原版返回 `ValueTuple&lt;ControlledSeedState,bool&gt;`）。</summary>
        public static (ControlledSeedState state, bool found) GetSeedState(string seedID)
        {
            ControlledSeedState st;
            bool found = seedStates.TryGetValue(seedID, out st);
            return (st, found);
        }

        /// <summary>判据：`RandomSeedProvider__SetSeedState.c:17-37` —— `seedStates[seedID] = state`
        /// （原版 `Add` / `set_Item` 两支，净效果 = 覆盖写）。</summary>
        public static void SetSeedState(string seedID, ControlledSeedState seedState)
        {
            seedStates[seedID] = seedState;
        }

        /// <summary>判据：`RandomSeedProvider__ResetSeed.c:17-25` —— `TryGetValue` 落空 ⇒ **返回 false（什么都不做）**；
        /// 命中 ⇒ **`state.currentRoll = 0`** + 返回 true（**不重掷 `startValue`**）。</summary>
        public static bool ResetSeed(string seedID)
        {
            ControlledSeedState st;
            if (!seedStates.TryGetValue(seedID, out st)) return false;
            if (st == null) return false;            // 原版这一支是 NRE；我们表里不会有 null（不静默：见下）
            st.Reset();
            return true;
        }

        /// <summary>判据：`RandomSeedProvider__GenerateRandomSeed.c:15-24` ——
        /// `Random.Range(0,1000) + (int)DateTime.Now.Ticks + deviceUniqueIdentifier.GetHashCode()`（`int` 截断相加）。
        /// 🔴 **这一发【不】存/还原全局 `Random.state`**（原版就没有）⇒ 它会**消耗全局随机流**，照抄。
        /// 🔴 三个输入里两个是「时间 + 设备」⇒ **它天生不可复现**（不是「按 seed 可回放」的那一族）。</summary>
        public static int GenerateRandomSeed()
        {
            var device = SystemInfo.deviceUniqueIdentifier;
            if (device == null)
            {
                // 原版这一支是 `FUN_1803f47a0()` = NullReferenceException（`.c:25-27` 的「不返回」）。
                // 照它的语义（抛），但先出声说清是哪一格（本项目红线：不许静默失败）。
                Debug.LogError("[RandomSeed] `SystemInfo.deviceUniqueIdentifier` 为 null —— "
                             + "原版 `GenerateRandomSeed` 这一支会抛 NullReferenceException（照原版语义）");
                throw new NullReferenceException("RandomSeedProvider.GenerateRandomSeed: deviceUniqueIdentifier == null");
            }
            int hash = device.GetHashCode();
            int ticks = unchecked((int)DateTime.Now.Ticks);
            int roll = UnityEngine.Random.Range(0, 1000);     // ← 不存/不还原全局 state（原版如此）
            return unchecked(roll + ticks + hash);
        }

        // ============================================================ 自检口（我们加的）

        /// <summary>我们加的**只读**口：表里现在挂了几个 seed（自检读「`ResetSeed` 没删键」那种事）。</summary>
        public static int SeedCount { get { return seedStates.Count; } }

        /// <summary>自检用：清空 seed 表（⛔ 只给自检，别在生产路径调）。</summary>
        public static void ClearForTest() { seedStates.Clear(); }
    }
}
