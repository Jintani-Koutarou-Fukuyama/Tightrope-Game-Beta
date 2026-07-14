# Tightrope Game Bata — AI作業・ルート制実装方針

最終調査日: 2026-07-14  
対象Unity: Unity 6 (`6000.0.23f1`)  
対象ゲームシーン: `Assets/Scenes/SampleScene.unity`

> [!IMPORTANT]
> この文書は、今後ルート制を安全に実装するための調査記録・作業方針である。今回、ゲーム機能、既存スクリプト、Scene、Prefabには変更を加えていない。実装は必ず1機能ずつ、最小差分で行うこと。

## 1. プロジェクト概要

本作は、プレイヤーがビル間のロープを渡り、バランス操作やイベントを乗り越えてゴールを目指すUnity 6プロジェクトである。

Build Settingsには次の4シーンが有効登録されている。

1. `Assets/Scenes/TitleScene.unity`
2. `Assets/Scenes/SampleScene.unity`
3. `Assets/Scenes/GameOverScene.unity`
4. `Assets/Scenes/ClearScene.unity`

`SampleScene`が現在のゲーム本編である。`ClearScene`の評価表示、`GameOverScene`への遷移、ダメージ回数の管理は既存処理として成立しているため、ルート制の初期実装では変更しない。

ローカルの実在フォルダ名は、リポジトリ側が `Tightrope-Game-Beta`、Unityプロジェクト側が `Tightrope Game Bata` である。依頼時の `Tightrope-Game-Bata` とはリポジトリ名の綴りが異なるため、次回作業時も実在パスを確認すること。

## 2. 旧仕様

旧仕様は、1本のロープを進みながら、複数のイベントフラグを通過し、イベントをランダムに発生させる構成である。

現状でも旧ランダムイベント処理は残っており、`EventManager.Start()`が次のワールド座標へ3個のイベントフラグを生成する。

- `(-0.7, 3.0, -4.0)`
- `(-3.0, 3.0, -4.0)`
- `(-6.5, 3.0, -4.0)`

生成されるPrefabは `Assets/EventFrag.prefab` で、名前も `EventFrag` である。アタッチされているクラス名は `EventFlag` である。PlayerタグのColliderが入ると `EventManager.RandomEvent()`を呼び、そのフラグ自身を破棄する。

`RandomEvent()`の候補は地震、ヘリ、ポーズ、スナイパーである。現在の`SampleScene`では4候補がすべて有効になっている。

## 3. 新仕様

新仕様では、マップ上に複数のロープを配置し、共通ルートの後で左右どちらかを選び、選択したルートだけを進んでゴールへ向かう。

概念上の進行は次のとおり。

```text
Start
  └─ 共通1本目のロープ
       └─ 分岐地点で停止
            ├─ 左を選択 ─ 左2本目 ─ 左3本目 ─ 左Goal
            └─ 右を選択 ─ 右2本目 ─ 右3本目 ─ 右Goal
```

添付された俯瞰図では、黒い四角が建物、黄色い線がロープ、`S`が開始地点、上下2個の`G`が左右各ルートのゴールを表している。図は設計イメージであり、現時点の`SampleScene`にはこの複数ルート構成はまだ配置されていない。

## 4. 今回の分岐ルート仕様

将来実装する確定仕様は次のとおり。

1. プレイヤーはStartから共通の1本目を渡る。
2. 1本目の後、2本目へ進む前の分岐地点で完全に停止する。
3. 分岐待機中だけ左キーまたは右キーを受け付ける。
4. キーを押した瞬間に選択を確定し、同じ分岐で再選択させない。
5. 左選択なら左ルート、右選択なら右ルートの2本目へ進む。
6. 屋上などロープ間に隙間がある場合、現在ロープの終端から次ロープの開始Transformへ瞬間移動してよい。
7. 選択ルートの3本目を渡り、そのルートのGoalへ到達する。

分岐入力は押し続け判定ではなく、1回だけの押下判定にする。ルート状態が「分岐待機中」でないフレームでは左右入力をルート選択に使わない。

### 入力競合の重要事項

`BalanceManager`は通常操作に左矢印・右矢印を使用している。ポーズイベントも矢印キーを使用する。分岐にも同じキーを使うため、次回実装前に次を決める必要がある。

- 分岐待機中はバランスゲージを止めるか。
- 分岐確定の1入力をバランス操作にも反映させてよいか。
- 分岐中にイベントを必ず停止・禁止するか。

必要であれば、`BalanceManager`内部を変更せず、既存公開APIの `PauseNormalBalanceGauge()` / `ResumeNormalBalanceGauge()` を利用する案をチーム担当者と確認する。ただし初回のルート実装で勝手に連携を追加しない。

## 5. ゴール・評価仕様

左ルートのGoal、右ルートのGoalのどちらに到達しても、既存の同じクリア処理へ接続する。

```text
各ルートの最終Goal
  → PlayerGameFeedbackController.LoadClearScene()
  → ClearResultDataへ評価とミス回数を保存
  → ClearSceneをロード
  → ClearResultManagerが結果を表示
```

評価仕様は変更禁止。

| ダメージ／ミス回数 | 結果 |
|---:|---|
| 0〜1回 | S評価 |
| 2回 | A評価 |
| 3〜4回 | B評価 |
| 5回 | GameOver |

現行コードでも、`PlayerGameFeedbackController.CalculateClearRank()`が0〜1回をS、2回をA、それ以外をBとしている。5回に到達した時点では `AddDamage()` が `GameOverScene`へ遷移し、`LoadClearScene()`も5回以上ならクリア遷移を拒否する。したがって、実動作は上表と一致する。

### 現在のGoal到達方法

`GoalPoint`にTriggerやGoal専用スクリプトは付いていない。`SuitMan`の `TightropeAutoGoalMover` が距離を監視し、到達時の `onGoalReached` UnityEventから、`GameManager`オブジェクト上の `PlayerGameFeedbackController.LoadClearScene()`を呼ぶ。

現在の `GoalPoint` はルート直下のTransformで、`RopePath`使用時にはその3D座標自体へ移動するのではなく、`GoalPoint.position`を直線`RopePath`へ投影した距離まで進む。このため、将来の斜めロープや複数ロープでは各終端Transformを正しくロープ上へ配置し、暗黙の投影に依存しないこと。

## 6. 今回はイベント関連を触らない方針

イベント関連はチームメンバー担当であり、今回もルート制の初回実装時も原則として変更しない。

- `EventManager`を変更しない。
- `EventFlag`を変更しない。
- `EventFrag.prefab`を変更しない。
- 地震、ヘリ、ポーズ、スナイパー各イベントを変更しない。
- 選ばれなかったルートのイベントを発生させない仕組みは将来の別工程とする。
- ルート制の最初の実装にはイベント連動を入れない。
- 古い固定座標のEventFlagが新ルートと偶然重ならないかだけ確認する。

注意: イベントを一時停止する目的で、`EventManager`の地震・ヘリ・ポーズ・スナイパーをすべてFalseにする方法は使わない。`RandomEvent()`は無効候補を引くと再帰的に再抽選するため、全候補無効の状態でフラグに触れると処理が終わらない危険がある。イベントの試験停止方法は担当者と合意してから決める。

## 7. 現状調査結果

### 7.1 SampleScene内の主要配置・命名

| Scene上の名前 | 現状 |
|---|---|
| `Rope` | 1個。多数のRopePartsを子に持ち、`Rope`コンポーネントが付く物理ロープ。 |
| `StartPoint` | `Rope`の子Transform。現在の直線RopePathの開始参照。 |
| `EndPoint` | `Rope`の子Transform。現在の直線RopePathの終了参照。 |
| `RopePath` | `Rope`とは別のルートオブジェクト。`RopePath`コンポーネントから上記Start/Endを参照。 |
| `GoalPoint` | ルート直下のTransformのみ。ColliderやGoal用MonoBehaviourはない。 |
| `SuitMan` | Playerタグ。Animator、Trigger Collider、Rigidbody、`TightropeAutoGoalMover`などを持つ。 |
| `GameManager` | GameObject名。`GameManager`クラスは存在せず、実体は `PlayerGameFeedbackController`。 |
| `BalanceManager` | バランスゲージ、各イベント用モード、ダメージUnityEventを管理。 |
| `EventManager` | ランダムイベント管理。実行時に3個の`EventFrag`を生成。 |
| `SniperEventManager` | `EventManager`の子。移動停止、Balance、複数カメラなどとの結合が強い。 |
| `Flags` | Scene内にあるイベント系Prefabインスタンス名。汎用`EventFlag`とは別物なので混同しない。 |
| `Main Camera` | `ThirdPersonCameraFollow`とCinemachine Brainを持つ。 |
| `CameraManager` | Prefabインスタンス。`CameraSwhich`とCameraShake関連を持つ。 |

Scene上には汎用イベントフラグの `EventFrag` / `EventFlag` は直接配置されていない。`EventManager.Start()`が実行時に生成する。

### 7.2 現在のプレイヤー移動・自動移動

#### `Assets/Player/anim/TightropeAutoGoalMover.cs`

現在`SuitMan`で実際に有効な自動移動担当。

- `moveOnStart = true`で開始時に自動移動する。
- `SampleScene`上の速度は `0.1`。
- `RopePath`が設定されている場合、直線Path上の距離を `Mathf.MoveTowards` で更新する。
- `RopePath`が未設定の場合、`GoalPoint`へ `Vector3.MoveTowards` で直接移動する。
- `PlayerStoping`がTrueの間、イベント用に移動を止める。
- `StartMoving()` / `StopMoving()`も公開されている。
- Goal到達時に `onGoalReached` を1回呼ぶ。
- 現在はGoalやRopePathを実行中に差し替える公開APIを持たないため、そのままでは複数セグメントの順次移動を管理できない。

重要な既存依存:

- `Helicopter`が `PlayerStoping` をTrue/Falseにする。
- `PosingEvent`が `PlayerStoping` をTrue/Falseにする。
- `SniperEventManager`が `PlayerStoping` をTrue/Falseにする。
- `onGoalReached`が既存クリア処理へ接続されている。

このクラスの名前・型・`PlayerStoping`の意味を突然変えると、イベント担当者の処理を壊す。

#### `Assets/Player/anim/TightropePlayerMover.cs`

W/Sで`RopePath`上を前後移動する手動移動スクリプト。Transform位置を直接更新する。現在の`SampleScene`にはアタッチされていない。

#### `Assets/Player/anim/RopePositionLock.cs`

`LateUpdate()`でプレイヤー位置を`RopePath`へ投影し、ロープ中心へ固定するスクリプト。現在の`SampleScene`にはアタッチされていない。

#### `Assets/Player/anim/RopePath.cs`

開始Transformと終了Transformの間を1本の直線として扱う。開始・終了が未設定なら子Renderer/ColliderのBoundsから軸と端点を推定する。曲線・分岐・複数セグメントの管理機能はない。

#### `Assets/Player/anim/ThirdPersonCameraFollow.cs`

`Main Camera`から`SuitMan`を追従する。ルート方向が変わると、プレイヤー回転に応じて追従オフセットも回る設定になっている。分岐実装時は見え方の確認だけ行い、初回は変更しない。

### 7.3 名前による自動検索の注意

`TightropeAutoGoalMover`、`TightropePlayerMover`、`RopePositionLock`は、参照が未設定の場合に `GameObject.Find("Rope")` から `RopePath`を取得しようとする。

しかし現状では、`RopePath`コンポーネントは `Rope`ではなく、別GameObjectの `RopePath`に付いている。現在動作しているのはInspector参照が設定済みだからである。複数ロープ化すると同名検索はさらに危険になる。

今後は次を守る。

- `Rope`や`GoalPoint`を複数同名で置いて `GameObject.Find` に頼らない。
- 各ルート・各セグメントの参照をInspectorから明示的に設定する。
- 参照切れ時の自動検索はフォールバックと考え、ルート選択ロジックの基盤にしない。

### 7.4 Goal・Clear・GameOver関連

| スクリプト | 役割 |
|---|---|
| `TightropeAutoGoalMover.cs` | Goalへの到達判定と`onGoalReached`通知。 |
| `PlayerGameFeedbackController.cs` | ダメージ回数、GameOver遷移、ランク計算、ClearScene遷移。 |
| `ClearResultData.cs` | ランクとミス回数をシーン間で一時保持。 |
| `ClearResultManager.cs` | ClearSceneでS/A/B結果を表示。 |
| `ClearSceneButtonController.cs` | リトライ・タイトル遷移とリトライ時リセット。 |
| `GameOverManager.cs` | GameOverSceneからTitleSceneへ戻る入力。 |

左右のGoalから別々の評価ロジックを作らない。最終到達だけを同じ `PlayerGameFeedbackController.LoadClearScene()`へ集約する。

### 7.5 EventManager・イベントフラグ概要

| スクリプト／Prefab | 役割・注意点 |
|---|---|
| `Assets/Script/Event/EventManager.cs` | EventFragの生成とイベント抽選。固定座標・単一路線前提が残る。 |
| `Assets/Script/Event/EventFlag.cs` | PlayerのTrigger侵入でランダムイベントを呼び、自身を破棄。 |
| `Assets/EventFrag.prefab` | 汎用EventFlagの実体。ファイル名とクラス名の綴りが異なる。 |
| `Assets/Flags.prefab` | ヘリ等のイベント構成を含む別Prefab。汎用EventFragではない。 |
| `Earthquake.cs` | `Rope.GetRandomRopePart(30, 60)`で現在指定された物理ロープを揺らす。 |
| `Helicopter.cs` | 自動移動停止、カメラ、演出等を管理。 |
| `PosingEvent.cs` | 自動移動停止、Balance、カメラ、矢印入力等を管理。 |
| `SniperEventManager.cs` | 自動移動停止、Balanceのモード、複数カメラ・演出を横断管理。 |

旧ランダムイベントは現在も有効である。選択ルートの概念はなく、EventFlagの固定ワールド座標だけで発火する。新ルートにイベントを対応させる作業は、イベント担当者と設計を合わせた後の別工程にする。

地震イベントは単一の`Rope`と、その中の少なくとも60付近までのRopePartsを暗黙に前提としている。複数ロープ化後に「現在渡っているロープ」を選ぶ仕組みが必要になる可能性はあるが、今回は変更しない。

### 7.6 BalanceManager概要

`BalanceManager`は通常の横ゲージ、イベント用縦ゲージ、スナイパー防御、マトリックス回避など複数モードを持つ大きな既存クラスである。

- 通常の左右操作は左矢印・右矢印。
- ミス時の`onDamage(int)`は `GameManager`上の `PlayerGameFeedbackController.AddDamage(int)`へ接続済み。
- 成功・失敗音も `PlayerGameFeedbackController`へ接続済み。
- スナイパー関連スクリプトから多数の公開APIが呼ばれる。

ルート分岐のために内部ロジックやキー設定を直接書き換えない。必要な連携が生じた場合も、まず担当者に確認し、既存公開APIで足りるかを検討する。

### 7.7 GameManager・PlayerGameFeedbackController概要

`SampleScene`の `GameManager`はGameObject名であり、`GameManager.cs`や`GameManager`クラスは見つからない。アタッチされているのは `PlayerGameFeedbackController`である。

このコンポーネントは次を一括管理する。

- ダメージ回数
- 5回到達時のGameOverScene遷移
- S/A/Bランク計算
- ClearResultDataへの保存
- ClearScene遷移
- 成功音・カウント音・ミス音
- プレイヤーの赤点滅

ルート制からは最終Goal時の既存公開メソッドだけを呼び、評価ロジックを複製・変更しない。

### 7.8 CameraManager系概要

`CameraManager` Prefabには `CameraSwhich` があり、Man、Dino、EventDino、Posing、UnderRope等のCinemachine Camera優先度を切り替える。`SniperEventManager`も別途Cinemachine Cameraの状態を保存・復元する。

さらに`Main Camera`には `ThirdPersonCameraFollow`とCinemachine Brainが同居している。カメラ制御は複数箇所にまたがるため、ルート制初回ではカメラ演出や優先度を変更しない。

### 7.9 現在の主要スクリプト一覧

| 分類 | 主要スクリプト |
|---|---|
| 自動移動 | `TightropeAutoGoalMover.cs` |
| 手動ロープ移動候補 | `TightropePlayerMover.cs`, `RopePositionLock.cs`, `RopePath.cs` |
| 物理ロープ | `Rope.cs`, `RopeParts.cs` |
| バランス | `BalanceManager.cs` |
| ゲーム結果 | `PlayerGameFeedbackController.cs`, `ClearResultData.cs`, `ClearResultManager.cs` |
| Scene遷移 | `TitleManager.cs`, `GameOverManager.cs`, `ClearSceneButtonController.cs` |
| ランダムイベント | `EventManager.cs`, `EventFlag.cs` |
| 主なイベント | `Earthquake.cs`, `Helicopter.cs`, `PosingEvent.cs`, `SniperEventManager.cs` |
| カメラ | `CameraSwiich.cs`内の`CameraSwhich`, `CameraShake.cs`, `ThirdPersonCameraFollow.cs` |

## 8. 今後のルート制実装方針

### 8.1 最初はTransform間移動

最初の実装は、各ロープの開始Transformと終了Transformの間を座標で移動する方式にする。物理ロープの形状追従、足元判定、曲線追従は初回に入れない。

各区間は最低限、次の参照を持つ。

- 区間開始Transform
- 区間終了Transform
- 区間終了後に次開始地点へ瞬間移動する必要があるか
- 次開始Transform

ルート定義とTransform更新を同じ巨大クラスへ詰め込まない。

### 8.2 ルート管理と移動処理を分離する

責務は概念上、次の2つへ分ける。

1. **ルート進行管理**: 共通1本目、分岐待機、左右選択、選択ルートの2本目・3本目、Goal到達を管理する。
2. **1区間の移動**: 指定された開始・終了間を移動し、停止・再開・区間完了を通知する。

最初から汎用グラフ、ScriptableObject群、複雑なノードエディタなどの大規模フレームワークは作らない。最小限の状態と、左右2ルートに必要な参照だけで始める。

ルート進行管理側は `transform.position` を直接更新せず、1区間の移動担当へ開始・停止を依頼する。これにより後から移動担当だけを、チームメンバーの「ロープを直接歩くシステム」へ差し替えやすくする。

### 8.3 既存TightropeAutoGoalMoverとの共存

`TightropeAutoGoalMover`はイベント側3系統から具体型で参照され、`PlayerStoping`で停止されている。このため、最初から削除・改名・全面置換しない。

安全性の高い候補は、次回実装時にチームと確認してどちらかを選ぶ。

- 既存公開挙動を維持したまま、1区間の開始Transform・終了Transformを設定できる最小APIだけを追加する。
- 新しいTransform間移動担当を追加しつつ、既存イベントから見た停止・再開契約を維持する小さな橋渡しを用意する。

どちらの場合も、既存の `onGoalReached`、`PlayerStoping`、イベント側Inspector参照を壊さない。実装前に、未統合の直接歩行システムのAPI・Prefab・担当者方針を確認する。

### 8.4 推奨するScene命名

同名検索を避けるため、例として次のように一意な名前を使う。最終名はチームで決定する。

```text
RoutePoints
├─ CommonRope1Start
├─ CommonRope1End
├─ BranchPoint
├─ LeftRope2Start
├─ LeftRope2End
├─ LeftRope3Start
├─ LeftRope3End
├─ LeftGoal
├─ RightRope2Start
├─ RightRope2End
├─ RightRope3Start
├─ RightRope3End
└─ RightGoal
```

自動検索ではなく、Inspectorの明示参照を使用する。左右Goalの名前は別でも、到達後に呼ぶ既存クリア処理は同じにする。

### 8.5 後からロープ直接歩行へ差し替える方針

将来の直接歩行システムに必要になる可能性が高い情報は次のとおり。

- 現在選択中のルート
- 現在のロープ／区間
- その区間の開始・終了
- 移動中、停止中、分岐待機中の状態
- 区間完了通知
- 屋上間テレポート通知
- Goal到達通知

これらをルート進行側が保持し、ロープ上の具体的な位置計算、接線方向、足元補正、アニメーション制御は移動担当側へ閉じ込める。`RopePath`は現在1本の直線専用なので、将来システムが曲線や物理RopePartsを使う場合は、その実装をルート進行側へ漏らさない。

## 9. 触ると危険な既存処理

### 原則として触らない

- `Assets/Script/Event/EventManager.cs`
- `Assets/Script/Event/EventFlag.cs`
- `Assets/EventFrag.prefab`
- `Assets/Flags.prefab`
- `Assets/Script/Event/`以下のイベント実装
- `Assets/Script/BalanceManager.cs`
- `Assets/Script/PlayerGameFeedbackController.cs`
- `Assets/Script/ClearScene/`以下の評価・表示処理
- `Assets/Script/Camera/`以下
- `Assets/Camera/CameraManager.prefab`
- `Main Camera`および各Cinemachine Cameraの設定
- PlayerのAnimator、Rig、Collider、Rigidbody、既存イベント参照

### ルート制で触る可能性は高いが、特に慎重に扱う

- `Assets/Player/anim/TightropeAutoGoalMover.cs`
- `Assets/Player/anim/RopePath.cs`
- `Assets/Scenes/SampleScene.unity`
- `SuitMan`の `TightropeAutoGoalMover` Inspector設定
- `Rope`、`StartPoint`、`EndPoint`、`GoalPoint`のHierarchyと参照

Scene変更はテキスト上の差分が大きくなりやすい。Unity Editorで必要箇所だけを変更し、Prefab override、削除オブジェクト、参照GUIDの意図しない変化がないか必ず確認する。

### 複数ロープ化で後から影響しそうな箇所

- `Earthquake.cs`が単一の`Rope`参照とRopePartsの範囲30〜60を前提にしている。
- 移動系3スクリプトのフォールバックが、名前`Rope`の単一オブジェクトを前提にしている。
- `EventManager`が旧ロープ上の固定ワールド座標へEventFragを生成する。
- カメラ追従がプレイヤー回転依存で、ルート方向変更時に見え方が変わる。
- `BalanceManager`と分岐入力が左右矢印を共有する。
- `Helicopter`、`PosingEvent`、`SniperEventManager`が具体的な`TightropeAutoGoalMover`を参照する。
- `UnderTheRope`などAnimator StateMachineBehaviourがプレイヤーTransformを直接動かす場面がある。

## 10. 今後の安全な実装ステップ

以下を一度に行わず、各ステップごとに動作確認・差分確認を行う。

0. **Gitの基準状態を確定する。** 調査時点ではリポジトリに既存の `.gitignore` 削除と、Unityプロジェクトフォルダ全体の未追跡状態が見えている。チーム作業を始める前に、正しいリポジトリ構成と基準コミットを担当者へ確認する。今回これらは変更・修正していない。
1. **ルート座標だけを確定する。** 共通1本目、BranchPoint、左右2本目・3本目、左右GoalのTransform位置・向きを決める。
2. **1区間のTransform移動だけを実装する。** 既存1本のロープで開始・終了・停止・区間完了を検証する。まだ分岐を入れない。
3. **共通1本目の完了で停止する。** 分岐入力はまだ入れず、確実に停止できることだけを検証する。
4. **左右選択だけを追加する。** 分岐待機中の1回押下で選択を確定し、二重入力を防ぐ。Balance入力との競合も確認する。
5. **左ルートだけを接続する。** 2本目、必要なテレポート、3本目、左Goalまで確認する。
6. **右ルートだけを接続する。** 左と同じ処理を複製せず、参照データだけを変えて右Goalまで確認する。
7. **両Goalを既存クリア処理へ接続する。** `LoadClearScene()`を再利用し、評価コードは変更しない。
8. **既存回帰確認を行う。** 0〜1=S、2=A、3〜4=B、5=GameOverを確認する。
9. **イベント担当者とルート連動を設計する。** 選択ルートだけのEventFlagを有効にする方法を別タスクで決める。
10. **直接歩行システムへ差し替える。** ルート進行は維持し、1区間の移動担当だけを交換する。

## 11. Codexが作業するときの注意点

1. 実装は必ず1機能ずつ行う。
2. 一度に大きく作り替えない。
3. 作業前に `AGENTS.md`、Git状態、対象Scene、Prefab overrideを確認する。
4. 既存ファイルを丸ごと上書きしない。
5. ユーザーやチームメンバーの未コミット変更を戻さない。
6. `EventManager`、イベントフラグ、イベントスクリプトを勝手に変更しない。
7. `BalanceManager`、`GameManager`相当の`PlayerGameFeedbackController`、`CameraManager`系、プレイヤー制御系を慎重に扱う。
8. 最初のルート実装ではイベント連動を入れない。
9. 最初の実装対象はプレイヤー移動と分岐選択だけにする。
10. ルート管理と移動処理を分離し、後から直接歩行へ差し替えられるようにする。
11. 左右どちらのGoalでも既存の同じクリア処理を使う。
12. 評価閾値、GameOver閾値、ClearSceneの結果処理を変更しない。
13. 複数の`Rope`や`GoalPoint`を名前検索で解決しない。
14. 実装ごとに、変更対象ファイル、変更理由、Inspectorで必要な設定、確認手順を報告する。
15. Sceneを保存した場合は、意図しないオブジェクト・参照・Prefab overrideの変更がないか差分確認する。

## 12. 「1機能ずつ、最小差分で実装する」方針

このプロジェクトは、移動、イベント、バランス、カメラ、結果処理が互いに参照している。全面置換は小さな仕様変更でも広範囲を壊す可能性がある。

ルート制は次の単位に分割し、1回の依頼で原則1単位だけ扱う。

- 1区間のTransform移動
- 区間完了通知
- 分岐地点での停止
- 左右入力の1回確定
- 左ルートの連結
- 右ルートの連結
- 両Goalから既存Clearへの接続
- イベント連動（後工程）
- 直接歩行への差し替え（後工程）

各単位の完了条件を先に決め、確認後に次へ進む。共通化は左右両方で実際に重複が確認できた範囲だけに留め、将来予測だけで大きな抽象化を追加しない。

## 今回実装しなかったこと

- 新しいルート移動処理
- 分岐選択処理
- イベント発生処理
- イベントフラグ変更
- `EventManager`の変更
- クリア処理の変更
- バランスゲージ改良
- ロープ直接歩行システムの実装
- UI追加
- カメラ演出追加
- 体感メーター
- 落下アニメーション
- 警告音

## 次回の実装依頼前に必要な情報

- 実装対象Sceneは既存`SampleScene`か、検証用の複製Sceneか。
- 添付図をUnityワールドへ落とした各開始・終了・Branch・Goalの正確な座標と向き。
- 既存物理ロープを複製して使うか、仮のTransformだけで先に試すか。
- 1本目開始時に自動移動するか、開始入力を待つか。
- 全区間の速度は同じか、区間別か。
- テレポート時のプレイヤー位置、向き、高さ補正。
- BranchPointでBalanceゲージを止めるか。
- 左右矢印を旧Input APIで読むか、Input SystemのActionを使うか。
- ルート試験中の旧EventManagerをどう扱うか。イベント担当者の合意が必要。
- チームメンバーの「ロープを直接歩くシステム」のスクリプト、Prefab、公開API、担当者の統合方針。
- `TightropeAutoGoalMover`へ最小APIを追加してよいか、新規移動担当を追加すべきか。
- 変更を許可するファイルの明示。特に`SampleScene.unity`とPlayerのPrefab override。
- 調査時に未追跡だったUnityプロジェクトを、どのGit状態を基準として扱うか。
## 初心者向け説明・コメント方針

このプロジェクトでは、Unity初心者でも後から設定・確認できるように、実装時は以下を守る。

- 新しく作るスクリプトには、クラスの役割を日本語コメントで簡単に書く。
- Inspectorに表示される[SerializeField]には、何を入れる項目なのか分かるコメントを書く。
- publicメソッドには、UnityEventやButtonから呼ぶ用途がある場合、その使い方をコメントで書く。
- 複雑な処理には、なぜその処理が必要なのかを短くコメントする。
- ただし、すべての行にコメントを書く必要はない。初心者が迷いやすい部分だけ説明する。
- 作業報告では、コード説明だけでなく、Unity側でどのGameObjectを選び、Inspectorに何を設定するかも必ず説明する。
- 可能であれば、テスト手順も「1. どのSceneを開く」「2. どのGameObjectを選ぶ」「3. Playして何を見る」の形で書く。

