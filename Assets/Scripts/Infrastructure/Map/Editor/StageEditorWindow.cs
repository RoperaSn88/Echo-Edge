#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Domain.Battle;
using EchoEdge.Domain.Map;
using EchoEdge.Domain.Phase;
using EchoEdge.Infra.Battle;

namespace EchoEdge.Infra.Map
{
    /// <summary>
    /// ステージ（ウェーブ）構築用 CSV を割り当てて、盤面を byte の2次元配列として確認・編集するツール。
    /// 上段は盤面（0: 空 / 1: 壁 / 2: 斜面の壁 / 3: 敵 / 4: ワープホール / 5: リエイムパッド）、下段は選んだマスの詳細。
    /// 盤面の上の「←」「→」で、エネミーフェーズごとのユニットの移動をシミュレーションできる（バフは考慮しない）。
    /// </summary>
    public class StageEditorWindow : EditorWindow
    {
        /// <summary>
        /// 盤面の1マスの種類。値は盤面に表示する byte の値
        /// </summary>
        private enum CellKind : byte
        {
            Empty = 0,
            Wall = 1,
            SlopeWall = 2,
            Enemy = 3,
            WarpHole = 4,
            ReAimPad = 5,
        }

        /// <summary>
        /// CSV の1行分の配置（編集用）
        /// </summary>
        [Serializable]
        private class Placement
        {
            public StageObjectKind kind;
            public int h;
            public int w;
            public EnemyKinds enemyKind;
            public int level;
            public int angle;
            public int pairId;
            public List<EnemyActionKinds> actions = new();
        }

        private const int MaxTurn = 99;

        /// <summary>
        /// 逆算モードで目標にできる最大ターン（初期位置の候補を総当たりで探すため、重くならない範囲にする）
        /// </summary>
        private const int MaxReverseTurn = 10;

        /// <summary>
        /// 逆算で試す初期位置の組み合わせの上限
        /// </summary>
        private const int MaxSolveCombinations = 3000;
        private const float CellSize = 44f;
        private const float AxisLabelSize = 22f;
        private const string DefaultConditionLine = "0, 0";
        private const string DefaultHeaderLine = "ObjectKind,Height,Width,EnemyKind,Level,Actions";

        /// <summary>
        /// 行動リスト列のヘッダー名（元のヘッダーに無いとき、行動リストを保存するなら末尾に足す）
        /// </summary>
        private const string ActionsHeader = "Actions";

        // StageClearConditionType の値の順
        private static readonly string[] ClearConditionLabels = { "0: 全滅", "1: 耐久", "2: 連続撃破", "3: ボス撃破" };

        private static readonly string[] CellKindLabels = { "0: 空", "1: 通常の壁", "2: スロープの壁", "3: 敵", "4: ワープホール", "5: リエイムパッド" };

        private static readonly string[] ActionLabels = { "攻撃 (Attack)", "スキル (Skill)", "なにもしない (None)" };

        private static readonly Dictionary<EnemyKinds, string> EnemyNames = new()
        {
            { EnemyKinds.Skya, "スカイア" },
            { EnemyKinds.Builder, "ビルダー" },
            { EnemyKinds.Booster, "ブースター" },
            { EnemyKinds.Brute, "ブルート" },
            { EnemyKinds.Enar, "エナー" },
            { EnemyKinds.BigEnar, "でかエナー" },
            { EnemyKinds.Seeker, "シーカー" },
        };

        private static readonly EnemyKinds[] SelectableEnemyKinds =
            ((EnemyKinds[])Enum.GetValues(typeof(EnemyKinds))).Where(k => k != EnemyKinds.Invalid).ToArray();

        private static readonly Color EmptyColor = new(0.22f, 0.22f, 0.22f);
        private static readonly Color WallColor = new(0.62f, 0.62f, 0.66f);
        private static readonly Color SlopeWallColor = new(0.24f, 0.55f, 0.62f);
        private static readonly Color EnemyColor = new(0.75f, 0.30f, 0.28f);
        private static readonly Color WarpHoleColor = new(0.55f, 0.32f, 0.85f);
        private static readonly Color ReAimPadColor = new(0.18f, 0.62f, 0.55f);
        private static readonly Color SelectedColor = new(1f, 0.85f, 0.2f);
        private static readonly Color MovedFromColor = new(0.75f, 0.30f, 0.28f, 0.35f);
        private static readonly Color MismatchColor = new(1f, 0.5f, 0.1f);

        [SerializeField]
        private TextAsset _csv;

        private string _conditionLine = DefaultConditionLine;
        private string _headerLine = DefaultHeaderLine;
        private StageClearConditionData _clearCondition;
        private List<Placement> _placements = new();
        private int _skippedRowCount;
        private bool _isDirty;

        private Dictionary<int, string[]> _enemyRows = new();

        private int _turn;

        // シミュレーション結果（[ターン][盤面上のユニット番号] = 位置）。配置を編集したら作り直す
        private List<Placement> _simulatedUnits;
        private List<(int h, int w)[]> _turnPositions;

        private (int h, int w)? _selectedCell;
        private Placement _selectedPlacement;

        // 逆算モード：今のターンでいてほしい位置（目標）を置くと、そこへ来るように初期位置を求める
        private bool _reverseMode;
        private readonly Dictionary<Placement, (int h, int w)> _targets = new();
        private bool _fixActionsOnSolve = true;
        private string _solveMessage;
        private MessageType _solveMessageType;

        private Vector2 _scrollPosition;
        private GUIStyle _cellValueStyle;
        private GUIStyle _cellDetailStyle;
        private GUIStyle _axisStyle;
        private GUIStyle _turnStyle;

        [MenuItem("Tools/Stage/Stage Editor")]
        private static void Open()
        {
            GetWindow<StageEditorWindow>("Stage Editor");
        }

        private void OnEnable()
        {
            LoadEnemyRows();
            if (_csv != null) LoadCsv(_csv);
        }

        private void OnGUI()
        {
            CreateStylesIfNeeded();

            var newCsv = (TextAsset)EditorGUILayout.ObjectField("ステージCSV", _csv, typeof(TextAsset), false);
            if (newCsv != _csv)
            {
                if (!_isDirty || EditorUtility.DisplayDialog("Stage Editor", "保存していない変更があります。破棄して別の CSV を開きますか？", "破棄して開く", "キャンセル"))
                {
                    _csv = newCsv;
                    LoadCsv(_csv);
                }
            }

            if (_csv == null)
            {
                EditorGUILayout.HelpBox("確認・編集するステージ（ウェーブ）の CSV を割り当ててください。", MessageType.Info);
                return;
            }

            DrawClearCondition();
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawToolbar();
            EditorGUILayout.Space();

            DrawTurnControls();
            DrawGrid();
            DrawOffBoardUnits();

            EditorGUILayout.Space();
            DrawInspector();

            EditorGUILayout.EndScrollView();
        }

        #region 読み込み・保存

        private void LoadEnemyRows()
        {
            var enemyCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(EnemyStatusLoader.CsvPath);
            _enemyRows = enemyCsv != null ? EnemyStatusLoader.ParseRows(enemyCsv.text) : new Dictionary<int, string[]>();
        }

        private void LoadCsv(TextAsset csv)
        {
            _placements = new List<Placement>();
            _conditionLine = DefaultConditionLine;
            _headerLine = DefaultHeaderLine;
            _clearCondition = null;
            _skippedRowCount = 0;
            _isDirty = false;
            _turn = 0;
            _selectedCell = null;
            _selectedPlacement = null;
            ExitReverseMode();
            InvalidateSimulation();

            if (csv == null) return;

            var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 1) _conditionLine = lines[0];
            if (lines.Length >= 2) _headerLine = lines[1];

            _clearCondition = StageLayoutLoader.GetClearCondition(csv);

            foreach (var data in StageLayoutLoader.GetPlacements(csv, MapManager.MapHeight, MapManager.MapWidth))
            {
                _placements.Add(new Placement
                {
                    kind = data.objectKind,
                    h = data.height,
                    w = data.width,
                    enemyKind = data.enemyKind,
                    level = data.level,
                    angle = StageLayoutLoader.GetSlopeAngle(data.slopeDirection),
                    pairId = data.pairId,
                    actions = new List<EnemyActionKinds>(data.actions),
                });
            }

            _skippedRowCount = Math.Max(0, lines.Length - 2 - _placements.Count);
        }

        private void SaveCsv()
        {
            var path = AssetDatabase.GetAssetPath(_csv);
            if (string.IsNullOrEmpty(path)) return;

            var newLine = _csv.text.Contains("\r\n") ? "\r\n" : "\n";
            var builder = new StringBuilder();
            builder.Append(_conditionLine).Append(newLine);
            var headerLine = _headerLine;
            if (_placements.Any(p => p.kind == StageObjectKind.Unit && p.actions.Count > 0) &&
                !headerLine.Contains(ActionsHeader, StringComparison.OrdinalIgnoreCase))
            {
                headerLine = headerLine.TrimEnd().TrimEnd(',') + "," + ActionsHeader;
            }
            builder.Append(headerLine).Append(newLine);

            foreach (var placement in _placements)
            {
                switch (placement.kind)
                {
                    case StageObjectKind.Wall:
                        builder.Append($"Wall, {placement.h}, {placement.w}");
                        break;
                    case StageObjectKind.SlopeWall:
                        builder.Append($"SlopeWall, {placement.h}, {placement.w}, {placement.angle}");
                        break;
                    case StageObjectKind.WarpHole:
                        builder.Append($"WarpHole, {placement.h}, {placement.w}, {placement.pairId}");
                        break;
                    case StageObjectKind.ReAimPad:
                        builder.Append($"ReAimPad, {placement.h}, {placement.w}");
                        break;
                    case StageObjectKind.Unit:
                        builder.Append($"Unit, {placement.h}, {placement.w}, {placement.enemyKind}");
                        if (placement.actions.Count > 0)
                        {
                            // 行動リストは6列目なので、レベル 0 でもレベル列を書く
                            builder.Append($", {placement.level}, {StageLayoutLoader.FormatActions(placement.actions)}");
                        }
                        else if (placement.level > 0)
                        {
                            builder.Append($", {placement.level}");
                        }
                        break;
                    default:
                        continue;
                }
                builder.Append(newLine);
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            _csv = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            _headerLine = headerLine;
            _isDirty = false;
            _skippedRowCount = 0;
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_isDirty))
                {
                    if (GUILayout.Button(_isDirty ? "CSVに保存 *" : "CSVに保存", GUILayout.Width(120)))
                    {
                        SaveCsv();
                    }

                    if (GUILayout.Button("変更を破棄", GUILayout.Width(120)))
                    {
                        LoadCsv(_csv);
                    }
                }

                if (GUILayout.Button("EnemyInfo.csv を再読み込み", GUILayout.Width(180)))
                {
                    LoadEnemyRows();
                    InvalidateSimulation();
                }
            }

            if (_skippedRowCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"読み込めなかった行が {_skippedRowCount} 行あります（詳細は Console の警告）。保存するとその行は消えます。",
                    MessageType.Warning);
            }
        }

        private void DrawClearCondition()
        {
            if (_clearCondition == null) return;

            EditorGUILayout.LabelField("クリア条件（1行目）", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 描画時の補正（範囲外の値の丸めなど）だけでは保存対象にしないよう、入力があったときだけ反映する
                EditorGUI.BeginChangeCheck();
                var type = (StageClearConditionType)EditorGUILayout.Popup(
                    "種類", (int)_clearCondition.conditionType, ClearConditionLabels);
                var value = _clearCondition.conditionValue;

                switch (type)
                {
                    case StageClearConditionType.Endurance:
                        value = Mathf.Max(1, EditorGUILayout.IntField("耐久ターン数", value));
                        break;
                    case StageClearConditionType.DefeatInARow:
                        value = Mathf.Max(1, EditorGUILayout.IntField("連続撃破数", value));
                        EditorGUILayout.HelpBox("連続撃破はゲーム側が未実装のため、全滅として扱われます。", MessageType.Warning);
                        break;
                    case StageClearConditionType.DefeatBoss:
                        value = DrawBossField(value);
                        break;
                    default:
                        value = 0;
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _clearCondition.conditionType = type;
                    _clearCondition.conditionValue = value;
                    _conditionLine = $"{(int)type}, {value}";
                    _isDirty = true;
                }
            }
        }

        /// <summary>
        /// ボス撃破条件のボス（敵ID）を選ぶ。倒せない敵（『シーカー』など）は選べない
        /// </summary>
        private int DrawBossField(int enemyId)
        {
            var kinds = SelectableEnemyKinds.Where(k => k.IsDefeatTarget()).ToArray();
            var labels = kinds.Select(k => $"{GetEnemyName(k)} (ID {(int)k})").ToArray();
            var index = Array.IndexOf(kinds, (EnemyKinds)enemyId);
            var newIndex = EditorGUILayout.Popup("ボス", Math.Max(0, index), labels);
            var boss = kinds[newIndex];

            if (!_placements.Any(p => p.kind == StageObjectKind.Unit && p.enemyKind == boss))
            {
                EditorGUILayout.HelpBox($"盤面に{GetEnemyName(boss)}が配置されていません。", MessageType.Warning);
            }

            return (int)boss;
        }

        #endregion

        #region シミュレーション

        private void InvalidateSimulation()
        {
            _simulatedUnits = null;
            _turnPositions = null;
        }

        private void MarkDirty()
        {
            _isDirty = true;
            InvalidateSimulation();
        }

        private void EnsureSimulated(int turn)
        {
            if (_turnPositions == null)
            {
                _simulatedUnits = _placements
                    .Where(p => p.kind == StageObjectKind.Unit && !p.enemyKind.IsOffBoard())
                    .ToList();
                _turnPositions = new List<(int h, int w)[]>
                {
                    _simulatedUnits.Select(p => (p.h, p.w)).ToArray()
                };
            }

            if (_turnPositions.Count > turn) return;

            var walls = BuildWalls();
            var units = _simulatedUnits.Select(ToSimUnit).ToList();

            while (_turnPositions.Count <= turn)
            {
                _turnPositions.Add(StageMoveSimulator.NextPhase(walls, units, _turnPositions[^1]));
            }
        }

        private bool[,] BuildWalls()
        {
            var walls = new bool[MapManager.MapHeight, MapManager.MapWidth];
            foreach (var placement in _placements)
            {
                if (placement.kind is StageObjectKind.Wall or StageObjectKind.SlopeWall && IsInBounds(placement.h, placement.w))
                {
                    walls[placement.h, placement.w] = true;
                }
            }
            return walls;
        }

        private StageMoveSimulator.Unit ToSimUnit(Placement placement)
        {
            var status = GetStatus(placement.enemyKind, 0);
            return status == null
                ? new StageMoveSimulator.Unit(1, 0)
                : new StageMoveSimulator.Unit(status.Size.FootprintSpan(), status.Move);
        }

        /// <summary>
        /// 現在のターンにおける、配置の位置（ユニットはシミュレーション後の位置。逆算モードでは目標の位置）
        /// </summary>
        private (int h, int w) GetPosition(Placement placement)
        {
            if (_reverseMode && _targets.TryGetValue(placement, out var target)) return target;
            return GetSimulatedPosition(placement);
        }

        /// <summary>
        /// 現在のターンにおける、シミュレーション後の位置
        /// </summary>
        private (int h, int w) GetSimulatedPosition(Placement placement)
        {
            if (_turn > 0 && placement.kind == StageObjectKind.Unit)
            {
                EnsureSimulated(_turn);
                var index = _simulatedUnits.IndexOf(placement);
                if (index >= 0) return _turnPositions[_turn][index];
            }
            return (placement.h, placement.w);
        }

        #region 逆算

        private void EnterReverseMode()
        {
            if (_turn <= 0) _turn = 2;
            _turn = Mathf.Clamp(_turn, 1, MaxReverseTurn);
            EnsureSimulated(_turn);

            // 今のシミュレーション結果をそのまま目標にする（入った時点では配置は変わらない）
            _targets.Clear();
            for (var i = 0; i < _simulatedUnits.Count; i++)
            {
                _targets[_simulatedUnits[i]] = _turnPositions[_turn][i];
            }

            _reverseMode = true;
            _solveMessage = null;
        }

        private void ExitReverseMode()
        {
            _reverseMode = false;
            _targets.Clear();
            _solveMessage = null;
        }

        /// <summary>
        /// 目標が置かれたユニットが、ターン _turn に目標の位置へ来るように初期位置を求めて反映する。
        /// ユニットごとに「単独で動かしたら目標に来る初期位置」を候補にし、候補の組み合わせを
        /// 実際の盤面（ユニット同士のぶつかりを含む）でシミュレーションして、全員が目標に来るものを選ぶ。
        /// </summary>
        private void SolveReverse()
        {
            var turn = _turn;
            var walls = BuildWalls();
            var units = _placements
                .Where(p => p.kind == StageObjectKind.Unit && !p.enemyKind.IsOffBoard())
                .ToList();
            var simUnits = units.Select(ToSimUnit).ToList();
            var count = units.Count;

            if (_fixActionsOnSolve)
            {
                foreach (var unit in units.Where(u => _targets.ContainsKey(u)))
                {
                    while (unit.actions.Count < turn) unit.actions.Add(EnemyActionKinds.Attack);
                }
            }

            // ユニットごとの初期位置の候補
            var candidates = new List<(int h, int w)>[count];
            var hasTarget = new bool[count];
            var unreachable = new List<Placement>();
            for (var i = 0; i < count; i++)
            {
                if (!_targets.TryGetValue(units[i], out var target))
                {
                    // 目標の無いユニットは今の初期位置のまま
                    candidates[i] = new List<(int h, int w)> { (units[i].h, units[i].w) };
                    continue;
                }

                hasTarget[i] = true;
                candidates[i] = FindStartCandidates(walls, simUnits[i], target, turn);
                if (candidates[i].Count == 0)
                {
                    unreachable.Add(units[i]);
                    candidates[i] = new List<(int h, int w)> { (units[i].h, units[i].w) };
                }
            }

            // 候補の少ないユニットから決めていく
            var order = Enumerable.Range(0, count).OrderBy(i => candidates[i].Count).ToArray();
            var chosen = new (int h, int w)[count];
            var occupied = new bool[MapManager.MapHeight, MapManager.MapWidth];
            (int h, int w)[] best = null;
            var bestKey = -1;
            var bestHasTie = false;
            var bestMatched = 0;
            var targetCount = hasTarget.Count(t => t);
            var tried = 0;
            var finished = false;

            void Evaluate()
            {
                tried++;
                IReadOnlyList<(int h, int w)> positions = chosen;
                var hasTie = false;
                for (var t = 0; t < turn; t++)
                {
                    positions = StageMoveSimulator.NextPhase(walls, simUnits, positions, out var tie);
                    hasTie |= tie;
                }

                var matched = 0;
                for (var i = 0; i < count; i++)
                {
                    if (hasTarget[i] && positions[i] == _targets[units[i]]) matched++;
                }

                // 目標に来る数が多いほど、同じなら同点（ランダム）が無いほど良い
                var key = matched * 2 + (hasTie ? 0 : 1);
                if (key > bestKey)
                {
                    bestKey = key;
                    best = chosen.ToArray();
                    bestHasTie = hasTie;
                    bestMatched = matched;
                }
                if (matched == targetCount && !hasTie) finished = true;
            }

            void Search(int depth)
            {
                if (finished || tried >= MaxSolveCombinations) return;
                if (depth == count)
                {
                    Evaluate();
                    return;
                }

                var i = order[depth];
                var span = simUnits[i].Size;
                foreach (var candidate in candidates[i])
                {
                    if (!IsFootprintFree(occupied, candidate, span)) continue;
                    SetFootprint(occupied, candidate, span, true);
                    chosen[i] = candidate;
                    Search(depth + 1);
                    SetFootprint(occupied, candidate, span, false);
                    if (finished || tried >= MaxSolveCombinations) return;
                }
            }

            Search(0);

            if (best == null)
            {
                _solveMessage = "初期位置が重ならない組み合わせが見つかりませんでした。目標の置き方を変えてください。";
                _solveMessageType = MessageType.Error;
                return;
            }

            for (var i = 0; i < count; i++)
            {
                units[i].h = best[i].h;
                units[i].w = best[i].w;
            }
            _isDirty = true;
            InvalidateSimulation();

            if (bestMatched == targetCount && unreachable.Count == 0)
            {
                _solveMessage = bestHasTie
                    ? $"全員がターン:{turn} に目標の位置へ来ます。ただし途中で移動先が同点になる場面があり、実際のゲームではランダムでずれることがあります（オレンジ枠は無し）。"
                    : $"全員がターン:{turn} に目標の位置へ来ます。";
                _solveMessageType = bestHasTie ? MessageType.Warning : MessageType.Info;
                return;
            }

            var builder = new StringBuilder($"ターン:{turn} に目標へ来ないユニットがあります（オレンジ枠）。");
            foreach (var unit in unreachable)
            {
                var target = _targets[unit];
                builder.Append($"\n・{GetEnemyName(unit.enemyKind)}（目標 縦 {target.h}, 横 {target.w}）：どこから出しても届きません。目標をもっと左にするか、ターンを減らしてください。");
            }
            if (unreachable.Count < targetCount - bestMatched)
            {
                builder.Append("\n・ユニット同士がぶつかって、全員をそろえられる初期位置が見つかりませんでした。");
            }
            _solveMessage = builder.ToString();
            _solveMessageType = MessageType.Warning;
        }

        /// <summary>
        /// そのユニットだけを盤面に置いて turn 回動かしたとき、target に来る初期位置の一覧。
        /// 同点（ランダム）が無いもの → 右にあるもの（毎フェーズしっかり動いて見える）→ 目標と縦が近いものの順。
        /// </summary>
        private static List<(int h, int w)> FindStartCandidates(bool[,] walls, StageMoveSimulator.Unit unit, (int h, int w) target, int turn)
        {
            var found = new List<((int h, int w) start, bool hasTie)>();
            var single = new[] { unit };
            for (var h = 0; h < MapManager.MapHeight; h++)
            {
                for (var w = 0; w < MapManager.MapWidth; w++)
                {
                    if (!IsFootprintWithoutWall(walls, h, w, unit.Size)) continue;

                    IReadOnlyList<(int h, int w)> positions = new[] { (h, w) };
                    var hasTie = false;
                    for (var t = 0; t < turn; t++)
                    {
                        positions = StageMoveSimulator.NextPhase(walls, single, positions, out var tie);
                        hasTie |= tie;
                    }

                    if (positions[0] == target) found.Add(((h, w), hasTie));
                }
            }

            return found
                .OrderBy(c => c.hasTie)
                .ThenByDescending(c => c.start.w)
                .ThenBy(c => Math.Abs(c.start.h - target.h))
                .Select(c => c.start)
                .ToList();
        }

        private static bool IsFootprintWithoutWall(bool[,] walls, int h, int w, int span)
        {
            for (var dh = 0; dh < span; dh++)
            {
                for (var dw = 0; dw < span; dw++)
                {
                    if (!IsInBounds(h + dh, w + dw) || walls[h + dh, w + dw]) return false;
                }
            }
            return true;
        }

        private static bool IsFootprintFree(bool[,] occupied, (int h, int w) anchor, int span)
        {
            for (var dh = 0; dh < span; dh++)
            {
                for (var dw = 0; dw < span; dw++)
                {
                    var h = anchor.h + dh;
                    var w = anchor.w + dw;
                    if (!IsInBounds(h, w) || occupied[h, w]) return false;
                }
            }
            return true;
        }

        private static void SetFootprint(bool[,] occupied, (int h, int w) anchor, int span, bool value)
        {
            for (var dh = 0; dh < span; dh++)
            {
                for (var dw = 0; dw < span; dw++)
                {
                    if (IsInBounds(anchor.h + dh, anchor.w + dw)) occupied[anchor.h + dh, anchor.w + dw] = value;
                }
            }
        }

        /// <summary>
        /// 逆算モードで、目標の位置に来ていないユニットか
        /// </summary>
        private bool IsMismatched(Placement placement)
        {
            return _reverseMode && _targets.TryGetValue(placement, out var target) && GetSimulatedPosition(placement) != target;
        }

        #endregion

        /// <summary>
        /// 現在のターンの盤面（byte の2次元配列）と、各マスにいる配置を作る
        /// </summary>
        private (byte[,] cells, Placement[,] owners) BuildBoard()
        {
            EnsureSimulated(_turn);

            var cells = new byte[MapManager.MapHeight, MapManager.MapWidth];
            var owners = new Placement[MapManager.MapHeight, MapManager.MapWidth];

            foreach (var placement in _placements)
            {
                CellKind kind;
                int size = 1;
                switch (placement.kind)
                {
                    case StageObjectKind.Wall:
                        kind = CellKind.Wall;
                        break;
                    case StageObjectKind.SlopeWall:
                        kind = CellKind.SlopeWall;
                        break;
                    case StageObjectKind.WarpHole:
                        kind = CellKind.WarpHole;
                        break;
                    case StageObjectKind.ReAimPad:
                        kind = CellKind.ReAimPad;
                        break;
                    case StageObjectKind.Unit when !placement.enemyKind.IsOffBoard():
                        kind = CellKind.Enemy;
                        size = GetSize(placement.enemyKind);
                        break;
                    default:
                        continue;
                }

                var (h, w) = GetPosition(placement);
                for (var dh = 0; dh < size; dh++)
                {
                    for (var dw = 0; dw < size; dw++)
                    {
                        if (!IsInBounds(h + dh, w + dw)) continue;
                        cells[h + dh, w + dw] = (byte)kind;
                        owners[h + dh, w + dw] = placement;
                    }
                }
            }

            return (cells, owners);
        }

        #endregion

        #region 盤面

        private void DrawTurnControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var minTurn = _reverseMode ? 1 : 0;
                var maxTurn = _reverseMode ? MaxReverseTurn : MaxTurn;
                var newTurn = _turn;
                using (new EditorGUI.DisabledScope(_turn <= minTurn))
                {
                    if (GUILayout.Button("←", GUILayout.Width(40))) newTurn--;
                }

                GUILayout.Label($"ターン:{_turn}", _turnStyle, GUILayout.Width(90));

                using (new EditorGUI.DisabledScope(_turn >= maxTurn))
                {
                    if (GUILayout.Button("→", GUILayout.Width(40))) newTurn++;
                }
                GUILayout.FlexibleSpace();

                if (newTurn != _turn)
                {
                    _turn = newTurn;
                    // 逆算モードでは、目標はそのままで「このターンに来るように」初期位置を求め直す
                    if (_reverseMode) SolveReverse();
                }
            }

            EditorGUILayout.HelpBox(
                $"ターン N は、エネミーフェーズを N 回終えたあとの配置です（{_turn + 1} ターン目のプレイヤーフェーズ）。移動だけを再現し、バフ・ランダムな特殊行動（飛行、壁生成、召喚、吸収など）・撃破は考慮しません。編集はターン:0 か逆算モードで行えます。",
                MessageType.None);

            DrawReversePanel();
        }

        private void DrawReversePanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var newMode = EditorGUILayout.ToggleLeft(
                    new GUIContent("逆算モード（このターンの並びを置くと、初期位置を自動で求める）"), _reverseMode, EditorStyles.boldLabel);
                if (newMode != _reverseMode)
                {
                    if (newMode) EnterReverseMode();
                    else ExitReverseMode();
                    GUI.FocusControl(null);
                }

                if (!_reverseMode) return;

                _fixActionsOnSolve = EditorGUILayout.ToggleLeft(
                    new GUIContent($"行動リストの先頭 {_turn} 回を攻撃にする",
                        "スキル（飛行・壁生成など）で動きがずれないように、逆算するときに行動リストの足りない分を「攻撃」で埋めます。"),
                    _fixActionsOnSolve);

                EditorGUILayout.LabelField(
                    "盤面には「このターンにいてほしい位置」を表示しています。マスの種類を「敵」にして置く・選んだ敵を右クリックで移動・←→で目標ターンを変えると、初期位置を求め直します。選んだ敵の初期位置は薄い赤で表示されます。",
                    EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("初期位置を求め直す", GUILayout.Width(140)))
                    {
                        SolveReverse();
                    }
                    if (GUILayout.Button("ターン:0 で確認する", GUILayout.Width(140)))
                    {
                        ExitReverseMode();
                        _turn = 0;
                    }
                }

                if (!string.IsNullOrEmpty(_solveMessage))
                {
                    EditorGUILayout.HelpBox(_solveMessage, _solveMessageType);
                }
            }
        }

        private void DrawGrid()
        {
            var (cells, owners) = BuildBoard();
            var height = MapManager.MapHeight;
            var width = MapManager.MapWidth;

            var totalWidth = AxisLabelSize + CellSize * width;
            var totalHeight = AxisLabelSize + CellSize * height;
            var area = GUILayoutUtility.GetRect(totalWidth, totalHeight, GUILayout.ExpandWidth(false));

            // 横（w）の目盛り
            for (var w = 0; w < width; w++)
            {
                var rect = new Rect(area.x + AxisLabelSize + w * CellSize, area.y, CellSize, AxisLabelSize);
                GUI.Label(rect, w.ToString(), _axisStyle);
            }

            // 縦（h）は上が奥（+Z）なので、h の大きい行を上に描く
            for (var row = 0; row < height; row++)
            {
                var h = height - 1 - row;
                var labelRect = new Rect(area.x, area.y + AxisLabelSize + row * CellSize, AxisLabelSize, CellSize);
                GUI.Label(labelRect, h.ToString(), _axisStyle);

                for (var w = 0; w < width; w++)
                {
                    var rect = new Rect(area.x + AxisLabelSize + w * CellSize, area.y + AxisLabelSize + row * CellSize, CellSize, CellSize);
                    DrawCell(rect, h, w, (CellKind)cells[h, w], owners[h, w]);
                }
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button is 0 or 1)
            {
                var local = e.mousePosition - new Vector2(area.x + AxisLabelSize, area.y + AxisLabelSize);
                var w = Mathf.FloorToInt(local.x / CellSize);
                var row = Mathf.FloorToInt(local.y / CellSize);
                if (local.x >= 0 && local.y >= 0 && w < width && row < height)
                {
                    var h = height - 1 - row;
                    if (e.button == 0)
                    {
                        _selectedCell = (h, w);
                        _selectedPlacement = owners[h, w];
                        GUI.FocusControl(null);
                        e.Use();
                    }
                    else if (_reverseMode && _selectedPlacement != null && _targets.ContainsKey(_selectedPlacement))
                    {
                        // 逆算モード：選んでいる敵の目標を右クリックしたマスへ動かす
                        TryMoveTarget(_selectedPlacement, h, w);
                        e.Use();
                    }
                }
            }
        }

        private void DrawCell(Rect rect, int h, int w, CellKind kind, Placement owner)
        {
            var inner = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            EditorGUI.DrawRect(inner, kind switch
            {
                CellKind.Wall => WallColor,
                CellKind.SlopeWall => SlopeWallColor,
                CellKind.Enemy => EnemyColor,
                CellKind.WarpHole => WarpHoleColor,
                CellKind.ReAimPad => ReAimPadColor,
                _ => EmptyColor,
            });

            // 選んでいるユニットの初期位置を薄く示す（シミュレーション中にどこから来たか分かるように）
            if (_turn > 0 && _selectedPlacement != null && _selectedPlacement.kind == StageObjectKind.Unit &&
                _selectedPlacement.h == h && _selectedPlacement.w == w && owner != _selectedPlacement)
            {
                EditorGUI.DrawRect(inner, MovedFromColor);
            }

            if (kind == CellKind.Enemy && owner != null && IsMismatched(owner))
            {
                DrawOutline(rect, MismatchColor, 3f);
            }

            var isSelected = _selectedPlacement != null ? owner == _selectedPlacement : _selectedCell == (h, w);
            if (isSelected)
            {
                DrawOutline(rect, SelectedColor, 2f);
            }

            var valueRect = new Rect(inner.x, inner.y + 1, inner.width, inner.height * 0.4f);
            GUI.Label(valueRect, ((byte)kind).ToString(), _cellValueStyle);

            var detail = kind switch
            {
                CellKind.SlopeWall when owner != null => $"{owner.angle}°",
                CellKind.WarpHole when owner != null => $"ペア{owner.pairId}",
                CellKind.Enemy when owner != null && GetPosition(owner) == (h, w) => GetShortEnemyLabel(owner),
                _ => string.Empty,
            };
            if (detail.Length > 0)
            {
                var detailRect = new Rect(inner.x, inner.y + inner.height * 0.4f, inner.width, inner.height * 0.6f);
                GUI.Label(detailRect, detail, _cellDetailStyle);
            }
        }

        private void DrawOffBoardUnits()
        {
            var offBoard = _placements.Where(p => p.kind == StageObjectKind.Unit && p.enemyKind.IsOffBoard()).ToList();
            if (offBoard.Count == 0) return;

            EditorGUILayout.LabelField("盤面外のユニット（マスを占有しない）", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var placement in offBoard)
                {
                    var label = $"{GetEnemyName(placement.enemyKind)} ({placement.h}, {placement.w})";
                    var isSelected = _selectedPlacement == placement;
                    if (GUILayout.Toggle(isSelected, label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)) && !isSelected)
                    {
                        _selectedPlacement = placement;
                        _selectedCell = null;
                        GUI.FocusControl(null);
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        #endregion

        #region マスの詳細

        private void DrawInspector()
        {
            EditorGUILayout.LabelField("マスの詳細", EditorStyles.boldLabel);

            if (_selectedPlacement != null && !_placements.Contains(_selectedPlacement))
            {
                _selectedPlacement = null;
            }

            if (_selectedPlacement == null && _selectedCell == null)
            {
                EditorGUILayout.HelpBox("盤面のマスをクリックすると、そのマスの壁・ユニットの詳細を確認できます。", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var isOffBoard = _selectedPlacement != null && _selectedPlacement.kind == StageObjectKind.Unit &&
                                 _selectedPlacement.enemyKind.IsOffBoard();

                if (isOffBoard)
                {
                    EditorGUILayout.LabelField("位置", $"盤面外（CSV の座標 縦 {_selectedPlacement.h}, 横 {_selectedPlacement.w}）");
                }
                else if (_reverseMode && _selectedPlacement != null && _targets.TryGetValue(_selectedPlacement, out var selectedTarget))
                {
                    DrawTargetFields(_selectedPlacement, selectedTarget);
                }
                else if (_selectedPlacement != null && _selectedPlacement.kind == StageObjectKind.Unit)
                {
                    var (h, w) = GetPosition(_selectedPlacement);
                    EditorGUILayout.LabelField("位置", _turn > 0
                        ? $"縦 {h}, 横 {w}（初期位置 縦 {_selectedPlacement.h}, 横 {_selectedPlacement.w}）"
                        : $"縦 {h}, 横 {w}");
                }
                else if (_selectedCell is { } cell)
                {
                    EditorGUILayout.LabelField("位置", $"縦 {cell.h}, 横 {cell.w}");
                }

                // マスの種類はターン0の盤面（逆算モードでは目標の盤面）に対して変える
                if (!isOffBoard && _selectedCell != null && (_turn == 0 || _reverseMode))
                {
                    DrawCellKindField();
                }

                var target = _selectedPlacement;
                if (target == null)
                {
                    return;
                }

                switch (target.kind)
                {
                    case StageObjectKind.Wall:
                        EditorGUILayout.HelpBox("情報なし", MessageType.None);
                        break;
                    case StageObjectKind.SlopeWall:
                        DrawSlopeWallFields(target);
                        break;
                    case StageObjectKind.WarpHole:
                        DrawWarpHoleFields(target);
                        break;
                    case StageObjectKind.ReAimPad:
                        EditorGUILayout.HelpBox("一閃が触れるとパッドの中心で止まり、そこから向きを選び直せます（残りの反射回数はそのまま）。", MessageType.None);
                        break;
                    case StageObjectKind.Unit:
                        DrawEnemyFields(target);
                        break;
                }

                if (isOffBoard && _turn == 0 && GUILayout.Button("この配置を削除", GUILayout.Width(120)))
                {
                    _placements.Remove(target);
                    _selectedPlacement = null;
                    MarkDirty();
                }
            }
        }

        /// <summary>
        /// 逆算モードで選んだ敵の、目標の位置と求めた初期位置
        /// </summary>
        private void DrawTargetFields(Placement placement, (int h, int w) target)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel($"ターン:{_turn} の目標");
                EditorGUIUtility.labelWidth = 24;
                var newH = EditorGUILayout.DelayedIntField("縦", target.h);
                var newW = EditorGUILayout.DelayedIntField("横", target.w);
                EditorGUIUtility.labelWidth = 0;
                if ((newH, newW) != target)
                {
                    TryMoveTarget(placement, newH, newW);
                }
            }

            EditorGUILayout.LabelField("初期位置（ターン:0）", $"縦 {placement.h}, 横 {placement.w}");

            if (IsMismatched(placement))
            {
                var (h, w) = GetSimulatedPosition(placement);
                EditorGUILayout.HelpBox($"この初期位置だと、ターン:{_turn} には 縦 {h}, 横 {w} に来ます（目標に届いていません）。", MessageType.Warning);
            }
        }

        private void DrawCellKindField()
        {
            var (h, w) = _selectedCell.Value;
            var current = GetCellKindAt(h, w);

            var selected = (CellKind)EditorGUILayout.Popup("種類", (int)current, CellKindLabels);
            if (selected != current)
            {
                ChangeCellKind(h, w, selected);
            }
        }

        private void DrawSlopeWallFields(Placement placement)
        {
            var labels = StageLayoutLoader.SlopeAngles.Select(GetAngleLabel).ToArray();
            var index = Array.IndexOf(StageLayoutLoader.SlopeAngles, placement.angle);
            var newIndex = EditorGUILayout.Popup("角度", Math.Max(0, index), labels);
            if (newIndex != index)
            {
                placement.angle = StageLayoutLoader.SlopeAngles[newIndex];
                MarkDirty();
            }
        }

        private void DrawWarpHoleFields(Placement placement)
        {
            var newPairId = Mathf.Max(0, EditorGUILayout.IntField(
                new GUIContent("ペア番号", "同じペア番号のワープホール2つが対になります。一閃が片方に入ると、もう片方から同じ向きで出ます"),
                placement.pairId));
            if (newPairId != placement.pairId)
            {
                placement.pairId = newPairId;
                MarkDirty();
            }

            var partners = _placements.Where(p => p != placement && p.kind == StageObjectKind.WarpHole && p.pairId == placement.pairId).ToList();
            if (partners.Count == 1)
            {
                EditorGUILayout.LabelField("対のワープホール", $"縦 {partners[0].h}, 横 {partners[0].w}");
            }
            else
            {
                EditorGUILayout.HelpBox(
                    partners.Count == 0
                        ? $"ペア番号 {placement.pairId} のワープホールがもう1つありません。対が無いワープホールは何も起きません。"
                        : $"ペア番号 {placement.pairId} のワープホールが {partners.Count + 1} 個あります。同じ番号はちょうど2つにしてください（このペアは使えません）。",
                    MessageType.Warning);
            }
        }

        private void DrawEnemyFields(Placement placement)
        {
            var labels = SelectableEnemyKinds.Select(k => $"{GetEnemyName(k)} ({k})").ToArray();
            var index = Array.IndexOf(SelectableEnemyKinds, placement.enemyKind);
            using (new EditorGUI.DisabledScope(_turn != 0 && !_reverseMode))
            {
                var newIndex = EditorGUILayout.Popup("敵", Math.Max(0, index), labels);
                if (newIndex != index)
                {
                    TryChangeEnemyKind(placement, SelectableEnemyKinds[newIndex]);
                }
            }

            var newLevel = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("レベル", "0 は補正なし（EnemyInfo.csv の値そのまま）"), placement.level));
            if (newLevel != placement.level)
            {
                placement.level = newLevel;
                MarkDirty();
            }

            DrawActionList(placement);

            var status = GetStatus(placement.enemyKind, placement.level);
            if (status == null)
            {
                EditorGUILayout.HelpBox($"EnemyInfo.csv に ID {(int)placement.enemyKind} の行が見つかりません。", MessageType.Warning);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("HP", status.MaxHP);
                EditorGUILayout.IntField("攻撃", status.Attack);
                EditorGUILayout.IntField("防御", status.Defend);
                EditorGUILayout.IntField("移動", status.Move);
                EditorGUILayout.TextField("行動順", status.MovePattern == MovePattern.Before ? "移動の前に行動" : "移動の後に行動");
                EditorGUILayout.IntField("エナジー", status.Energy);
                EditorGUILayout.IntField("経験値", status.Experience);
                EditorGUILayout.TextField("サイズ", status.Size == EnemySize.Large ? "見た目2倍（1x1マス）" : "1x1");
            }
        }

        /// <summary>
        /// 敵の行動リスト（先頭から順に行い、使い切ったら通常のランダム行動）の編集欄
        /// </summary>
        private void DrawActionList(Placement placement)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(new GUIContent("行動リスト",
                "エネミーフェーズごとに先頭から1つずつ行います。使い切ったら通常（ランダム）の行動に戻ります。"), EditorStyles.boldLabel);

            if (placement.actions.Count == 0)
            {
                EditorGUILayout.LabelField("（なし：最初から通常のランダム行動）", EditorStyles.miniLabel);
            }

            var removeIndex = -1;
            var swapIndex = -1;
            for (var i = 0; i < placement.actions.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // ターン:N のときは、次のエネミーフェーズ（N+1回目）で行う行動に印を付ける
                    var mark = i == _turn ? "▶" : " ";
                    EditorGUILayout.LabelField($"{mark} {i + 1}", GUILayout.Width(36));

                    var current = (int)placement.actions[i];
                    var selected = EditorGUILayout.Popup(current, ActionLabels);
                    if (selected != current)
                    {
                        placement.actions[i] = (EnemyActionKinds)selected;
                        MarkDirty();
                    }

                    using (new EditorGUI.DisabledScope(i == 0))
                    {
                        if (GUILayout.Button("↑", GUILayout.Width(24))) swapIndex = i - 1;
                    }
                    using (new EditorGUI.DisabledScope(i == placement.actions.Count - 1))
                    {
                        if (GUILayout.Button("↓", GUILayout.Width(24))) swapIndex = i;
                    }
                    if (GUILayout.Button("×", GUILayout.Width(24))) removeIndex = i;
                }
            }

            if (swapIndex >= 0)
            {
                (placement.actions[swapIndex], placement.actions[swapIndex + 1]) = (placement.actions[swapIndex + 1], placement.actions[swapIndex]);
                MarkDirty();
            }
            else if (removeIndex >= 0)
            {
                placement.actions.RemoveAt(removeIndex);
                MarkDirty();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("＋ 攻撃", GUILayout.Width(80)))
                {
                    placement.actions.Add(EnemyActionKinds.Attack);
                    MarkDirty();
                }
                if (GUILayout.Button("＋ スキル", GUILayout.Width(80)))
                {
                    placement.actions.Add(EnemyActionKinds.Skill);
                    MarkDirty();
                }
                if (GUILayout.Button("＋ なにもしない", GUILayout.Width(100)))
                {
                    placement.actions.Add(EnemyActionKinds.None);
                    MarkDirty();
                }
                using (new EditorGUI.DisabledScope(placement.actions.Count == 0))
                {
                    if (GUILayout.Button("すべて削除", GUILayout.Width(80)))
                    {
                        placement.actions.Clear();
                        MarkDirty();
                    }
                }
            }

            var note = GetActionListNote(placement);
            if (note != null)
            {
                EditorGUILayout.HelpBox(note, MessageType.Info);
            }
        }

        /// <summary>
        /// 行動リストを指定したときの、敵ごとの注意書き
        /// </summary>
        private static string GetActionListNote(Placement placement)
        {
            if (placement.actions.Count == 0) return null;

            var hasAttack = placement.actions.Contains(EnemyActionKinds.Attack);
            var hasSkill = placement.actions.Contains(EnemyActionKinds.Skill);
            switch (placement.enemyKind)
            {
                case EnemyKinds.Seeker when hasAttack:
                    return "シーカーは攻撃を持たないため、「攻撃」のフェーズは何もしません。";
                case EnemyKinds.Skya when hasSkill:
                    return "スカイアの「スキル」は飛び上がりです。次のフェーズのビームは行動リストを消費せずに行います。";
                case EnemyKinds.Enar when hasSkill:
                    return "エナーの「スキル」は、吸収できる仲間がいないときは何もしません。";
                case EnemyKinds.BigEnar when hasSkill:
                    return "でかエナーの「スキル」は、HP がコスト以下のときは何もしません。";
                default:
                    return null;
            }
        }

        #endregion

        #region 編集

        private CellKind GetCellKindAt(int h, int w)
        {
            var owner = FindInitialOwner(h, w);
            if (owner == null) return CellKind.Empty;
            return owner.kind switch
            {
                StageObjectKind.Wall => CellKind.Wall,
                StageObjectKind.SlopeWall => CellKind.SlopeWall,
                StageObjectKind.WarpHole => CellKind.WarpHole,
                StageObjectKind.ReAimPad => CellKind.ReAimPad,
                _ => CellKind.Enemy,
            };
        }

        /// <summary>
        /// 編集対象の盤面で、指定マスを占有している配置を返す（盤面外のユニットは除く）。
        /// 編集対象はターン0の盤面、逆算モードでは目標の盤面。
        /// </summary>
        private Placement FindInitialOwner(int h, int w, Placement ignore = null)
        {
            foreach (var placement in _placements)
            {
                if (placement == ignore) continue;
                if (placement.kind == StageObjectKind.Unit && placement.enemyKind.IsOffBoard()) continue;

                var (ph, pw) = GetEditPosition(placement);
                var size = placement.kind == StageObjectKind.Unit ? GetSize(placement.enemyKind) : 1;
                if (h >= ph && h < ph + size && w >= pw && w < pw + size)
                {
                    return placement;
                }
            }
            return null;
        }

        /// <summary>
        /// 編集対象の盤面での位置（ターン0の位置。逆算モードでは目標の位置）
        /// </summary>
        private (int h, int w) GetEditPosition(Placement placement)
        {
            return _reverseMode && _targets.TryGetValue(placement, out var target) ? target : (placement.h, placement.w);
        }

        /// <summary>
        /// 逆算モードで、敵の目標を動かして初期位置を求め直す
        /// </summary>
        private void TryMoveTarget(Placement placement, int h, int w)
        {
            if (!CanPlace(h, w, GetSize(placement.enemyKind), placement))
            {
                ShowNotification(new GUIContent("そのマスには置けません"));
                return;
            }

            _targets[placement] = (h, w);
            _selectedCell = (h, w);
            GUI.FocusControl(null);
            MarkDirty();
            SolveReverse();
            Repaint();
        }

        private bool CanPlace(int h, int w, int size, Placement ignore)
        {
            for (var dh = 0; dh < size; dh++)
            {
                for (var dw = 0; dw < size; dw++)
                {
                    if (!IsInBounds(h + dh, w + dw)) return false;
                    if (FindInitialOwner(h + dh, w + dw, ignore) != null) return false;
                }
            }
            return true;
        }

        private void ChangeCellKind(int h, int w, CellKind kind)
        {
            var existing = FindInitialOwner(h, w);
            var insertIndex = existing != null ? _placements.IndexOf(existing) : _placements.Count;
            if (existing != null)
            {
                _placements.Remove(existing);
                _targets.Remove(existing);
            }

            Placement created = kind switch
            {
                CellKind.Wall => new Placement { kind = StageObjectKind.Wall, h = h, w = w, enemyKind = EnemyKinds.Invalid },
                CellKind.SlopeWall => new Placement { kind = StageObjectKind.SlopeWall, h = h, w = w, enemyKind = EnemyKinds.Invalid, angle = StageLayoutLoader.SlopeAngles[0] },
                CellKind.Enemy => new Placement { kind = StageObjectKind.Unit, h = h, w = w, enemyKind = EnemyKinds.Skya },
                CellKind.ReAimPad => new Placement { kind = StageObjectKind.ReAimPad, h = h, w = w, enemyKind = EnemyKinds.Invalid },
                CellKind.WarpHole => new Placement { kind = StageObjectKind.WarpHole, h = h, w = w, enemyKind = EnemyKinds.Invalid, pairId = SuggestWarpHolePairId() },
                _ => null,
            };

            if (created != null) _placements.Insert(insertIndex, created);
            _selectedPlacement = created;
            MarkDirty();

            if (_reverseMode)
            {
                // 置いたマスはこのターンの目標。初期位置は逆算で決める
                if (created is { kind: StageObjectKind.Unit }) _targets[created] = (h, w);
                SolveReverse();
            }
        }

        /// <summary>
        /// 新しく置くワープホールのペア番号。対の片方しか無い番号があればそれ、無ければ未使用の最小の番号
        /// </summary>
        private int SuggestWarpHolePairId()
        {
            var counts = _placements.Where(p => p.kind == StageObjectKind.WarpHole)
                .GroupBy(p => p.pairId)
                .ToDictionary(g => g.Key, g => g.Count());
            foreach (var pair in counts.OrderBy(p => p.Key))
            {
                if (pair.Value == 1) return pair.Key;
            }

            var id = 0;
            while (counts.ContainsKey(id)) id++;
            return id;
        }

        private void TryChangeEnemyKind(Placement placement, EnemyKinds enemyKind)
        {
            var (editH, editW) = GetEditPosition(placement);
            if (!enemyKind.IsOffBoard() && !CanPlace(editH, editW, GetSize(enemyKind), placement))
            {
                ShowNotification(new GUIContent($"{GetEnemyName(enemyKind)} を置くマスが空いていません"));
                return;
            }

            var wasOffBoard = placement.enemyKind.IsOffBoard();
            placement.enemyKind = enemyKind;

            // 盤面外 ⇔ 盤面上が切り替わったら、選択も合わせる
            if (enemyKind.IsOffBoard() && !wasOffBoard)
            {
                _selectedCell = null;
            }
            else if (!enemyKind.IsOffBoard() && wasOffBoard)
            {
                _selectedCell = (editH, editW);
            }

            MarkDirty();

            if (_reverseMode)
            {
                // 盤面外の敵は動かないので目標を持たない。盤面に戻したら今の位置を目標にする
                if (enemyKind.IsOffBoard()) _targets.Remove(placement);
                else if (wasOffBoard) _targets[placement] = (editH, editW);
                SolveReverse();
            }
        }

        #endregion

        #region ユーティリティ

        private BattleStatus GetStatus(EnemyKinds enemyKind, int level)
        {
            if (!_enemyRows.TryGetValue((int)enemyKind, out var cols)) return null;
            try
            {
                return EnemyStatusLoader.ParseStatus(cols, level);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private int GetSize(EnemyKinds enemyKind)
        {
            var status = GetStatus(enemyKind, 0);
            return status != null ? status.Size.FootprintSpan() : 1;
        }

        private static bool IsInBounds(int h, int w)
        {
            return h >= 0 && h < MapManager.MapHeight && w >= 0 && w < MapManager.MapWidth;
        }

        private static string GetEnemyName(EnemyKinds enemyKind)
        {
            return EnemyNames.TryGetValue(enemyKind, out var name) ? name : enemyKind.ToString();
        }

        private static string GetShortEnemyLabel(Placement placement)
        {
            var name = GetEnemyName(placement.enemyKind);
            if (name.Length > 4) name = name.Substring(0, 4);
            return placement.level > 0 ? $"{name}\nLv{placement.level}" : name;
        }

        private static string GetAngleLabel(int angle)
        {
            return angle switch
            {
                45 => "45°（斜面が右上）",
                135 => "135°（斜面が左上）",
                -135 => "-135°（斜面が左下）",
                -45 => "-45°（斜面が右下）",
                _ => $"{angle}°",
            };
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void CreateStylesIfNeeded()
        {
            if (_cellValueStyle != null) return;

            _cellValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white },
            };
            _cellDetailStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 9,
                wordWrap = false,
                normal = { textColor = Color.white },
            };
            _axisStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };
            _turnStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
            };
        }

        #endregion
    }
}
#endif
