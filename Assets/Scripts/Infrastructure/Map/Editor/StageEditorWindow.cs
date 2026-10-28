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
    /// 上段は盤面（0: 空 / 1: 壁 / 2: 斜面の壁 / 3: 敵）、下段は選んだマスの詳細。
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
        }

        private const int MaxTurn = 99;
        private const float CellSize = 44f;
        private const float AxisLabelSize = 22f;
        private const string DefaultConditionLine = "0, 0";
        private const string DefaultHeaderLine = "ObjectKind,Height,Width,EnemyKind,Level";

        private static readonly string[] CellKindLabels = { "0: 空", "1: 通常の壁", "2: スロープの壁", "3: 敵" };

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
        private static readonly Color SelectedColor = new(1f, 0.85f, 0.2f);
        private static readonly Color MovedFromColor = new(0.75f, 0.30f, 0.28f, 0.35f);

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

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawToolbar();
            DrawClearCondition();
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
            builder.Append(_headerLine).Append(newLine);

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
                    case StageObjectKind.Unit:
                        builder.Append($"Unit, {placement.h}, {placement.w}, {placement.enemyKind}");
                        if (placement.level > 0) builder.Append($", {placement.level}");
                        break;
                    default:
                        continue;
                }
                builder.Append(newLine);
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            _csv = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
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

            var text = _clearCondition.conditionType switch
            {
                StageClearConditionType.DefeatAllEnemies => "全滅",
                StageClearConditionType.Endurance => $"耐久（{_clearCondition.conditionValue} ターン）",
                StageClearConditionType.DefeatInARow => $"連続撃破（{_clearCondition.conditionValue} 体）",
                StageClearConditionType.DefeatBoss => $"ボス撃破（{GetEnemyName((EnemyKinds)_clearCondition.conditionValue)}）",
                _ => _clearCondition.conditionType.ToString(),
            };
            EditorGUILayout.LabelField("クリア条件（1行目）", text);
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

            var walls = new bool[MapManager.MapHeight, MapManager.MapWidth];
            foreach (var placement in _placements)
            {
                if (placement.kind is StageObjectKind.Wall or StageObjectKind.SlopeWall && IsInBounds(placement.h, placement.w))
                {
                    walls[placement.h, placement.w] = true;
                }
            }

            var units = _simulatedUnits
                .Select(p =>
                {
                    var status = GetStatus(p.enemyKind, 0);
                    return status == null
                        ? new StageMoveSimulator.Unit(1, 0)
                        : new StageMoveSimulator.Unit((int)status.Size, status.Move);
                })
                .ToList();

            while (_turnPositions.Count <= turn)
            {
                _turnPositions.Add(StageMoveSimulator.NextPhase(walls, units, _turnPositions[^1]));
            }
        }

        /// <summary>
        /// 現在のターンにおける、配置の位置（ユニットはシミュレーション後の位置）
        /// </summary>
        private (int h, int w) GetPosition(Placement placement)
        {
            if (_turn > 0 && placement.kind == StageObjectKind.Unit && _simulatedUnits != null)
            {
                var index = _simulatedUnits.IndexOf(placement);
                if (index >= 0) return _turnPositions[_turn][index];
            }
            return (placement.h, placement.w);
        }

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
                using (new EditorGUI.DisabledScope(_turn <= 0))
                {
                    if (GUILayout.Button("←", GUILayout.Width(40))) _turn--;
                }

                GUILayout.Label($"ターン:{_turn}", _turnStyle, GUILayout.Width(90));

                using (new EditorGUI.DisabledScope(_turn >= MaxTurn))
                {
                    if (GUILayout.Button("→", GUILayout.Width(40))) _turn++;
                }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.HelpBox(
                "ターン N は、エネミーフェーズを N 回終えたあとの配置です。移動だけを再現し、バフ・ランダムな特殊行動（飛行、壁生成、召喚、吸収など）・撃破は考慮しません。編集はターン:0 で行えます。",
                MessageType.None);
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
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var local = e.mousePosition - new Vector2(area.x + AxisLabelSize, area.y + AxisLabelSize);
                var w = Mathf.FloorToInt(local.x / CellSize);
                var row = Mathf.FloorToInt(local.y / CellSize);
                if (local.x >= 0 && local.y >= 0 && w < width && row < height)
                {
                    var h = height - 1 - row;
                    _selectedCell = (h, w);
                    _selectedPlacement = owners[h, w];
                    GUI.FocusControl(null);
                    e.Use();
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
                _ => EmptyColor,
            });

            // 選んでいるユニットの初期位置を薄く示す（シミュレーション中にどこから来たか分かるように）
            if (_turn > 0 && _selectedPlacement != null && _selectedPlacement.kind == StageObjectKind.Unit &&
                _selectedPlacement.h == h && _selectedPlacement.w == w && owner != _selectedPlacement)
            {
                EditorGUI.DrawRect(inner, MovedFromColor);
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

                // マスの種類はターン0の盤面に対して変える
                if (!isOffBoard && _selectedCell != null && _turn == 0)
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

        private void DrawEnemyFields(Placement placement)
        {
            var labels = SelectableEnemyKinds.Select(k => $"{GetEnemyName(k)} ({k})").ToArray();
            var index = Array.IndexOf(SelectableEnemyKinds, placement.enemyKind);
            using (new EditorGUI.DisabledScope(_turn != 0))
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
                EditorGUILayout.TextField("サイズ", status.Size == EnemySize.Large ? "2x2" : "1x1");
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
                _ => CellKind.Enemy,
            };
        }

        /// <summary>
        /// ターン0の盤面で、指定マスを占有している配置を返す（盤面外のユニットは除く）
        /// </summary>
        private Placement FindInitialOwner(int h, int w, Placement ignore = null)
        {
            foreach (var placement in _placements)
            {
                if (placement == ignore) continue;
                if (placement.kind == StageObjectKind.Unit && placement.enemyKind.IsOffBoard()) continue;

                var size = placement.kind == StageObjectKind.Unit ? GetSize(placement.enemyKind) : 1;
                if (h >= placement.h && h < placement.h + size && w >= placement.w && w < placement.w + size)
                {
                    return placement;
                }
            }
            return null;
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
            if (existing != null) _placements.Remove(existing);

            Placement created = kind switch
            {
                CellKind.Wall => new Placement { kind = StageObjectKind.Wall, h = h, w = w, enemyKind = EnemyKinds.Invalid },
                CellKind.SlopeWall => new Placement { kind = StageObjectKind.SlopeWall, h = h, w = w, enemyKind = EnemyKinds.Invalid, angle = StageLayoutLoader.SlopeAngles[0] },
                CellKind.Enemy => new Placement { kind = StageObjectKind.Unit, h = h, w = w, enemyKind = EnemyKinds.Skya },
                _ => null,
            };

            if (created != null) _placements.Insert(insertIndex, created);
            _selectedPlacement = created;
            MarkDirty();
        }

        private void TryChangeEnemyKind(Placement placement, EnemyKinds enemyKind)
        {
            if (!enemyKind.IsOffBoard() && !CanPlace(placement.h, placement.w, GetSize(enemyKind), placement))
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
                _selectedCell = (placement.h, placement.w);
            }

            MarkDirty();
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
            return status != null ? (int)status.Size : 1;
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
