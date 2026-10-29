// ChallengeList.cs —— 原版 `ChallengeList : IdentifiableSO`（一组挑战的命名清单）
//
// ============================ 这是什么、判据在哪 ============================
// 账 `日常_奖励:524` 的附属件（`MissionsConfig.challengeLists` 的元素类型）。判据：
//   · 字段表 = `d:/2/tools/il2cpp_out/dump.cs:58761` TypeDefIndex 1390
//       `[SerializeField] private string id;                                   // 0x18`
//       `[SerializeField] private List<LoadableReference<MissionChallenge>> challenges; // 0x20`
//       `public List<LoadableReference<MissionChallenge>> Challenges { get; }`
//       `public override string GetID();                 // Slot 5`
//   · ILSpy = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChallengeList.cs`（`GetID() =&gt; id`）
//   · 同族 `ChallengeListLocator : AssetProvider<ChallengeList>`（TypeDefIndex 1391）**不建**：
//     全量反编译里没有任何调用点读它，而本机 0 份 `ChallengeList` 资产（`MissionsConfig` 三源否定）。
//
// ⚠️ **一处如实差异**：原版基类 `IdentifiableSO` 是 **ScriptableObject**（`GetID()` 是 `override`）；
//    本工程没有 `IdentifiableSO` 基类（全仓 0 命中）⇒ 这里落成**普通类** + 同名 `GetID()`。
//    ⇒ 它**不是**可 `CreateAssetMenu` 的资产；本机也没有任何 `.asset` 实例（与 `MissionsConfig` 同一处否定）。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>原版 `ChallengeList`（`dump.cs:58761`，两个字段逐个抄）。
    /// <para>⚠️ 为什么是 `public` 而不是原版那种「`IdentifiableSO` 派生」：见文件头那条差异。
    /// 必须是 `public` —— 断言在 `Assembly-CSharp-Editor` 里，`internal` 过不去。</para></summary>
    public class ChallengeList
    {
        /// <summary>原版 `private string id; // 0x18`。</summary>
        public string id;

        /// <summary>原版 `private List&lt;LoadableReference&lt;MissionChallenge&gt;&gt; challenges; // 0x20`。</summary>
        public List<LoadableRef<MissionChallenge>> challenges;

        /// <summary>原版 `public List&lt;LoadableReference&lt;MissionChallenge&gt;&gt; Challenges =&gt; challenges;`。</summary>
        public List<LoadableRef<MissionChallenge>> Challenges { get { return challenges; } }

        /// <summary>原版 `public override string GetID()`（Slot 5，体 = `return this.id;`）。</summary>
        public string GetID() { return id; }
    }
}
