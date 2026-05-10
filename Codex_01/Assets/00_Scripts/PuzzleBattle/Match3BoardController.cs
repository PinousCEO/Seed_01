using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleBattle
{
    public sealed class Match3BoardController : MonoBehaviour
    {
        public sealed class AttackResult
        {
            private readonly Dictionary<OrbVisualDefinition, int> _orbCounts = new Dictionary<OrbVisualDefinition, int>();

            public AttackResult(List<MatchResult> matches)
            {
                Matches = matches;
                int totalOrbsCleared = 0;
                int totalCombos = 0;
                int totalCascades = 0;

                for (int i = 0; i < matches.Count; i++)
                {
                    MatchResult match = matches[i];
                    totalOrbsCleared += match.Size;
                    totalCombos = Mathf.Max(totalCombos, match.ComboIndex);
                    totalCascades = Mathf.Max(totalCascades, match.CascadeIndex);

                    if (match.Definition == null)
                    {
                        continue;
                    }

                    if (_orbCounts.TryGetValue(match.Definition, out int existingCount))
                    {
                        _orbCounts[match.Definition] = existingCount + match.Size;
                    }
                    else
                    {
                        _orbCounts.Add(match.Definition, match.Size);
                    }
                }

                TotalOrbsCleared = totalOrbsCleared;
                TotalCombos = totalCombos;
                TotalCascades = totalCascades;
            }

            public IReadOnlyList<MatchResult> Matches { get; }
            public int TotalOrbsCleared { get; }
            public int TotalCombos { get; }
            public int TotalCascades { get; }

            public int GetClearedCount(OrbVisualDefinition definition)
            {
                if (definition == null)
                {
                    return 0;
                }

                return _orbCounts.TryGetValue(definition, out int count) ? count : 0;
            }

            public int GetClearedCount(string orbId)
            {
                if (string.IsNullOrWhiteSpace(orbId))
                {
                    return 0;
                }

                foreach (KeyValuePair<OrbVisualDefinition, int> pair in _orbCounts)
                {
                    if (pair.Key != null && pair.Key.OrbId == orbId)
                    {
                        return pair.Value;
                    }
                }

                return 0;
            }
        }

        public sealed class MatchResult
        {
            public MatchResult(OrbVisualDefinition definition, List<Vector2Int> cells, int cascadeIndex, int comboIndex, Color displayColor)
            {
                Definition = definition;
                Cells = cells;
                CascadeIndex = cascadeIndex;
                ComboIndex = comboIndex;
                DisplayColor = displayColor;
            }

            public OrbVisualDefinition Definition { get; }
            public List<Vector2Int> Cells { get; }
            public int CascadeIndex { get; }
            public int ComboIndex { get; }
            public Color DisplayColor { get; }
            public int Size => Cells.Count;
        }

        private sealed class BoardPiece
        {
            public OrbVisualDefinition Definition;
            public BoardPieceView View;
        }

        public event System.Action<IReadOnlyList<MatchResult>> MatchesCleared;
        public event System.Action<AttackResult> AttackResolved;
        public event System.Action<AttackResult> TurnResolved;

        private PuzzleBattleBoardProfile _profile;
        private BoardPiece[,] _pieces;
        private Rect _region;
        private Camera _camera;
        private SpriteRenderer _backdrop;
        private SpriteRenderer _frame;
        private readonly List<SpriteRenderer> _gridLines = new List<SpriteRenderer>();
        private float _cellSize;
        private Vector2 _origin;
        private bool _inputEnabled;
        private bool _resolving;
        private bool _pointerPressed;
        private bool _dragging;
        private bool _movedDuringDrag;
        private Vector2Int _pressedCell;
        private Vector2Int _dragCurrentCell;
        private Vector2Int? _clickSelectedCell;
        private Vector3 _pointerDownWorld;
        private BoardPiece _draggedPiece;

        private int Columns => _profile.Columns;
        private int Rows => _profile.Rows;
        private OrbMotionProfile MotionProfile => _profile.MotionProfile;
        private OrbVisualDefinition[] OrbDefinitions => _profile.OrbDefinitions;

        public void Configure(PuzzleBattleBoardProfile profile, Rect region, Camera worldCamera)
        {
            _profile = profile;
            _region = region;
            _camera = worldCamera;
            _inputEnabled = true;

            EnsureBoardVisuals();
            UpdateLayout();

            if (_pieces == null)
            {
                BuildBoard();
            }
            else
            {
                RepositionPieces();
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            if (!enabled)
            {
                ClearClickSelection();
            }
        }

        private void Update()
        {
            if (!_inputEnabled || _resolving || _camera == null)
            {
                return;
            }

            Vector3 pointerWorld = _camera.ScreenToWorldPoint(Input.mousePosition);
            pointerWorld.z = 0f;

            if (Input.GetMouseButtonDown(0))
            {
                HandlePointerDown(pointerWorld);
            }

            if (_pointerPressed && !_dragging && Input.GetMouseButton(0))
            {
                float dragThreshold = _cellSize * 0.18f;

                if (Vector2.Distance(pointerWorld, _pointerDownWorld) >= dragThreshold)
                {
                    StartDrag();
                }
            }

            if (_dragging && Input.GetMouseButton(0))
            {
                UpdateDrag(pointerWorld);
            }

            if (Input.GetMouseButtonUp(0))
            {
                HandlePointerUp(pointerWorld);
            }
        }

        private void HandlePointerDown(Vector3 worldPosition)
        {
            if (!TryWorldToCell(worldPosition, out Vector2Int cell))
            {
                _pointerPressed = false;
                return;
            }

            _pointerPressed = true;
            _pressedCell = cell;
            _pointerDownWorld = worldPosition;
        }

        private void HandlePointerUp(Vector3 worldPosition)
        {
            if (_dragging)
            {
                FinishDrag();
            }
            else if (_pointerPressed && TryWorldToCell(worldPosition, out Vector2Int cell) && cell == _pressedCell)
            {
                HandleClickCell(cell);
            }
            else
            {
                ClearClickSelection();
            }

            _pointerPressed = false;
        }

        private void StartDrag()
        {
            _dragging = true;
            _movedDuringDrag = false;
            _dragCurrentCell = _pressedCell;
            _draggedPiece = _pieces[_pressedCell.x, _pressedCell.y];

            if (_clickSelectedCell.HasValue && _clickSelectedCell.Value != _pressedCell)
            {
                ClearClickSelection();
            }

            if (_draggedPiece != null)
            {
                _draggedPiece.View.SetSelected(true);
                _draggedPiece.View.SetSortingOrder(60);
            }
        }

        private void UpdateDrag(Vector3 worldPosition)
        {
            if (!TryWorldToCell(worldPosition, out Vector2Int hoveredCell))
            {
                return;
            }

            int guard = 0;

            while (_dragCurrentCell != hoveredCell && guard < 8)
            {
                Vector2Int step = ComputeStepTowards(_dragCurrentCell, hoveredCell, worldPosition);
                Vector2Int nextCell = _dragCurrentCell + step;

                if (!IsInside(nextCell))
                {
                    break;
                }

                SwapForDrag(_dragCurrentCell, nextCell);
                _dragCurrentCell = nextCell;
                _movedDuringDrag = true;
                guard++;
            }
        }

        private void FinishDrag()
        {
            _dragging = false;

            if (_draggedPiece != null)
            {
                _draggedPiece.View.SetSelected(false);
                _draggedPiece.View.SetSortingOrder(20);
                _draggedPiece.View.AnimateTo(CellToWorld(_dragCurrentCell), MotionProfile.SwapDuration);
            }

            _draggedPiece = null;

            if (_movedDuringDrag)
            {
                ClearClickSelection();
                StartCoroutine(ResolveBoardRoutine(MotionProfile.SwapDuration + 0.02f));
            }
        }

        private void HandleClickCell(Vector2Int cell)
        {
            if (_clickSelectedCell.HasValue)
            {
                Vector2Int selectedCell = _clickSelectedCell.Value;

                if (selectedCell == cell)
                {
                    ClearClickSelection();
                    return;
                }

                if (AreAdjacent(selectedCell, cell))
                {
                    ClearClickSelection();
                    StartCoroutine(SwapAndResolveRoutine(selectedCell, cell));
                    return;
                }

                ClearClickSelection();
            }

            _clickSelectedCell = cell;
            SetCellSelected(cell, true);
        }

        private IEnumerator SwapAndResolveRoutine(Vector2Int first, Vector2Int second)
        {
            _inputEnabled = false;
            SwapWithAnimation(first, second, MotionProfile.SwapDuration);
            yield return new WaitForSeconds(MotionProfile.SwapDuration + 0.02f);
            yield return ResolveBoardRoutine();
        }

        private IEnumerator ResolveBoardRoutine(float initialDelay = 0f)
        {
            _resolving = true;
            _inputEnabled = false;
            int cascadeIndex = 1;
            List<MatchResult> resolvedMatches = new List<MatchResult>();

            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            while (true)
            {
                List<MatchResult> matches = FindMatches(cascadeIndex);

                if (matches.Count == 0)
                {
                    break;
                }

                resolvedMatches.AddRange(matches);
                yield return ClearMatchesRoutine(matches);
                yield return CollapseBoardRoutine();
                cascadeIndex++;
            }

            _resolving = false;
            _inputEnabled = true;

            AttackResult attack = new AttackResult(resolvedMatches);

            if (resolvedMatches.Count > 0)
            {
                AttackResolved?.Invoke(attack);
            }

            TurnResolved?.Invoke(attack);
        }

        private IEnumerator ClearMatchesRoutine(List<MatchResult> matches)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                MatchResult match = matches[i];

                for (int cellIndex = 0; cellIndex < match.Cells.Count; cellIndex++)
                {
                    Vector2Int cell = match.Cells[cellIndex];
                    BoardPiece piece = _pieces[cell.x, cell.y];

                    if (piece != null)
                    {
                        piece.View.AnimatePop();
                    }
                }
            }

            MatchesCleared?.Invoke(matches);
            yield return new WaitForSeconds(MotionProfile.PopDuration + 0.02f);

            for (int i = 0; i < matches.Count; i++)
            {
                for (int cellIndex = 0; cellIndex < matches[i].Cells.Count; cellIndex++)
                {
                    Vector2Int cell = matches[i].Cells[cellIndex];
                    BoardPiece piece = _pieces[cell.x, cell.y];

                    if (piece != null)
                    {
                        Destroy(piece.View.gameObject);
                        _pieces[cell.x, cell.y] = null;
                    }
                }
            }
        }

        private IEnumerator CollapseBoardRoutine()
        {
            float longestDuration = 0f;

            for (int x = 0; x < Columns; x++)
            {
                int writeRow = 0;

                for (int readRow = 0; readRow < Rows; readRow++)
                {
                    BoardPiece piece = _pieces[x, readRow];

                    if (piece == null)
                    {
                        continue;
                    }

                    if (writeRow != readRow)
                    {
                        _pieces[x, writeRow] = piece;
                        _pieces[x, readRow] = null;
                        float duration = Mathf.Abs(readRow - writeRow) * MotionProfile.FallDurationPerCell;
                        duration = Mathf.Max(duration, MotionProfile.SwapDuration);
                        piece.View.AnimateTo(CellToWorld(new Vector2Int(x, writeRow)), duration);
                        longestDuration = Mathf.Max(longestDuration, duration);
                    }

                    writeRow++;
                }

                int spawnOffset = 0;

                for (int row = writeRow; row < Rows; row++)
                {
                    BoardPiece piece = CreatePiece(x, row, GetRandomDefinition(), true, spawnOffset + 1);
                    float duration = (Rows - row + spawnOffset) * MotionProfile.FallDurationPerCell;
                    duration = Mathf.Max(duration, MotionProfile.SwapDuration);
                    piece.View.AnimateTo(CellToWorld(new Vector2Int(x, row)), duration);
                    longestDuration = Mathf.Max(longestDuration, duration);
                    spawnOffset++;
                }
            }

            if (longestDuration > 0f)
            {
                yield return new WaitForSeconds(longestDuration + 0.02f);
            }
        }

        private List<MatchResult> FindMatches(int cascadeIndex)
        {
            bool[,] matched = new bool[Columns, Rows];

            for (int y = 0; y < Rows; y++)
            {
                int x = 0;

                while (x < Columns)
                {
                    BoardPiece piece = _pieces[x, y];

                    if (piece == null)
                    {
                        x++;
                        continue;
                    }

                    int runLength = 1;

                    while (x + runLength < Columns &&
                           _pieces[x + runLength, y] != null &&
                           _pieces[x + runLength, y].Definition == piece.Definition)
                    {
                        runLength++;
                    }

                    if (runLength >= _profile.MinimumMatchLength)
                    {
                        for (int offset = 0; offset < runLength; offset++)
                        {
                            matched[x + offset, y] = true;
                        }
                    }

                    x += runLength;
                }
            }

            for (int x = 0; x < Columns; x++)
            {
                int y = 0;

                while (y < Rows)
                {
                    BoardPiece piece = _pieces[x, y];

                    if (piece == null)
                    {
                        y++;
                        continue;
                    }

                    int runLength = 1;

                    while (y + runLength < Rows &&
                           _pieces[x, y + runLength] != null &&
                           _pieces[x, y + runLength].Definition == piece.Definition)
                    {
                        runLength++;
                    }

                    if (runLength >= _profile.MinimumMatchLength)
                    {
                        for (int offset = 0; offset < runLength; offset++)
                        {
                            matched[x, y + offset] = true;
                        }
                    }

                    y += runLength;
                }
            }

            List<MatchResult> results = new List<MatchResult>();
            bool[,] visited = new bool[Columns, Rows];
            int comboIndex = 1;

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (!matched[x, y] || visited[x, y] || _pieces[x, y] == null)
                    {
                        continue;
                    }

                    OrbVisualDefinition definition = _pieces[x, y].Definition;
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    List<Vector2Int> cells = new List<Vector2Int>();
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        Vector2Int current = queue.Dequeue();
                        cells.Add(current);

                        for (int dir = 0; dir < 4; dir++)
                        {
                            Vector2Int next = current + DirectionAt(dir);

                            if (!IsInside(next) || visited[next.x, next.y] || !matched[next.x, next.y] || _pieces[next.x, next.y] == null)
                            {
                                continue;
                            }

                            if (_pieces[next.x, next.y].Definition != definition)
                            {
                                continue;
                            }

                            visited[next.x, next.y] = true;
                            queue.Enqueue(next);
                        }
                    }

                    Color displayColor = _pieces[x, y].View != null ? _pieces[x, y].View.DisplayColor : definition.Tint;
                    results.Add(new MatchResult(definition, cells, cascadeIndex, comboIndex, displayColor));
                    comboIndex++;
                }
            }

            return results;
        }

        private void BuildBoard()
        {
            _pieces = new BoardPiece[Columns, Rows];

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    CreatePiece(x, y, GetInitialDefinition(x, y), false, 0);
                }
            }
        }

        private BoardPiece CreatePiece(int x, int y, OrbVisualDefinition definition, bool spawnAbove, int spawnOffset)
        {
            BoardPieceView prefab = _profile.GetOrbPrefab(definition);
            BoardPieceView view;

            if (prefab != null)
            {
                view = Instantiate(prefab, transform);
            }
            else
            {
                GameObject pieceObject = new GameObject();
                pieceObject.transform.SetParent(transform, false);
                view = pieceObject.AddComponent<BoardPieceView>();
            }

            string orbId = definition != null ? definition.OrbId : "orb";
            view.name = $"Piece_{orbId}_{x}_{y}";
            view.Initialize(definition, MotionProfile, _cellSize * 0.92f);

            Vector3 targetPosition = CellToWorld(new Vector2Int(x, y));
            Vector3 startPosition = targetPosition;

            if (spawnAbove)
            {
                startPosition += Vector3.up * _cellSize * spawnOffset;
            }

            view.SnapTo(startPosition);
            view.SetSortingOrder(20);

            BoardPiece piece = new BoardPiece
            {
                Definition = definition,
                View = view
            };

            _pieces[x, y] = piece;
            return piece;
        }

        private OrbVisualDefinition GetInitialDefinition(int x, int y)
        {
            if (!_profile.AvoidStartingMatches)
            {
                return GetRandomDefinition();
            }

            List<OrbVisualDefinition> candidates = new List<OrbVisualDefinition>(OrbDefinitions);

            if (x >= 2 &&
                _pieces[x - 1, y] != null &&
                _pieces[x - 2, y] != null &&
                _pieces[x - 1, y].Definition == _pieces[x - 2, y].Definition)
            {
                candidates.Remove(_pieces[x - 1, y].Definition);
            }

            if (y >= 2 &&
                _pieces[x, y - 1] != null &&
                _pieces[x, y - 2] != null &&
                _pieces[x, y - 1].Definition == _pieces[x, y - 2].Definition)
            {
                candidates.Remove(_pieces[x, y - 1].Definition);
            }

            if (candidates.Count == 0)
            {
                candidates.AddRange(OrbDefinitions);
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private OrbVisualDefinition GetRandomDefinition()
        {
            return OrbDefinitions[Random.Range(0, OrbDefinitions.Length)];
        }

        private void SwapForDrag(Vector2Int first, Vector2Int second)
        {
            BoardPiece firstPiece = _pieces[first.x, first.y];
            BoardPiece secondPiece = _pieces[second.x, second.y];
            _pieces[first.x, first.y] = secondPiece;
            _pieces[second.x, second.y] = firstPiece;

            if (secondPiece != null)
            {
                secondPiece.View.AnimateTo(CellToWorld(first), MotionProfile.SwapDuration);
            }

            if (firstPiece != null)
            {
                firstPiece.View.AnimateTo(CellToWorld(second), MotionProfile.SwapDuration);
            }
        }

        private void SwapWithAnimation(Vector2Int first, Vector2Int second, float duration)
        {
            BoardPiece firstPiece = _pieces[first.x, first.y];
            BoardPiece secondPiece = _pieces[second.x, second.y];
            _pieces[first.x, first.y] = secondPiece;
            _pieces[second.x, second.y] = firstPiece;

            if (firstPiece != null)
            {
                firstPiece.View.AnimateTo(CellToWorld(second), duration);
            }

            if (secondPiece != null)
            {
                secondPiece.View.AnimateTo(CellToWorld(first), duration);
            }
        }

        private void EnsureBoardVisuals()
        {
            if (_backdrop == null)
            {
                GameObject backdropObject = new GameObject("BoardBackdrop");
                backdropObject.transform.SetParent(transform, false);
                _backdrop = backdropObject.AddComponent<SpriteRenderer>();
                _backdrop.sprite = ProceduralSpriteLibrary.GetSquareSprite();
                _backdrop.color = new Color(0.1f, 0.11f, 0.18f, 0.96f);
                _backdrop.sortingOrder = 1;
            }

            if (_frame == null)
            {
                GameObject frameObject = new GameObject("BoardFrame");
                frameObject.transform.SetParent(transform, false);
                _frame = frameObject.AddComponent<SpriteRenderer>();
                _frame.sprite = ProceduralSpriteLibrary.GetSquareSprite();
                _frame.color = new Color(0.95f, 0.96f, 1f, 0.2f);
                _frame.sortingOrder = 2;
            }

            int requiredLines = Columns + Rows + 2;

            while (_gridLines.Count < requiredLines)
            {
                GameObject lineObject = new GameObject($"GridLine_{_gridLines.Count}");
                lineObject.transform.SetParent(transform, false);
                SpriteRenderer lineRenderer = lineObject.AddComponent<SpriteRenderer>();
                lineRenderer.sprite = ProceduralSpriteLibrary.GetSquareSprite();
                lineRenderer.color = new Color(1f, 1f, 1f, 0.08f);
                lineRenderer.sortingOrder = 3;
                _gridLines.Add(lineRenderer);
            }
        }

        private void UpdateLayout()
        {
            float usableWidth = _region.width - (_profile.BoardInset * 2f);
            float usableHeight = _region.height - (_profile.BoardInset * 2f);
            _cellSize = Mathf.Min(usableWidth / Columns, usableHeight / Rows);

            Vector2 boardSize = new Vector2(Columns * _cellSize, Rows * _cellSize);
            Vector2 boardCenter = new Vector2(_region.center.x, _region.center.y);
            _origin = boardCenter - (boardSize * 0.5f) + (Vector2.one * (_cellSize * 0.5f));

            _backdrop.transform.position = boardCenter;
            _backdrop.transform.localScale = new Vector3(boardSize.x + 0.32f, boardSize.y + 0.32f, 1f);

            _frame.transform.position = boardCenter;
            _frame.transform.localScale = new Vector3(boardSize.x + 0.06f, boardSize.y + 0.06f, 1f);

            int lineIndex = 0;
            float left = _origin.x - (_cellSize * 0.5f);
            float bottom = _origin.y - (_cellSize * 0.5f);
            float right = left + (Columns * _cellSize);
            float top = bottom + (Rows * _cellSize);

            for (int x = 0; x <= Columns; x++)
            {
                SpriteRenderer line = _gridLines[lineIndex++];
                line.transform.position = new Vector3(left + (x * _cellSize), (top + bottom) * 0.5f, 0f);
                line.transform.localScale = new Vector3(0.03f, Rows * _cellSize, 1f);
            }

            for (int y = 0; y <= Rows; y++)
            {
                SpriteRenderer line = _gridLines[lineIndex++];
                line.transform.position = new Vector3((left + right) * 0.5f, bottom + (y * _cellSize), 0f);
                line.transform.localScale = new Vector3(Columns * _cellSize, 0.03f, 1f);
            }

            for (int i = lineIndex; i < _gridLines.Count; i++)
            {
                _gridLines[i].transform.localScale = Vector3.zero;
            }
        }

        private void RepositionPieces()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    BoardPiece piece = _pieces[x, y];

                    if (piece != null)
                    {
                        piece.View.SetBaseScale(_cellSize * 0.92f);
                        piece.View.SnapTo(CellToWorld(new Vector2Int(x, y)));
                    }
                }
            }
        }

        private void SetCellSelected(Vector2Int cell, bool selected)
        {
            BoardPiece piece = _pieces[cell.x, cell.y];

            if (piece != null)
            {
                piece.View.SetSelected(selected);
            }
        }

        private void ClearClickSelection()
        {
            if (_clickSelectedCell.HasValue)
            {
                SetCellSelected(_clickSelectedCell.Value, false);
                _clickSelectedCell = null;
            }
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(_origin.x + (cell.x * _cellSize), _origin.y + (cell.y * _cellSize), 0f);
        }

        private bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            float left = _origin.x - (_cellSize * 0.5f);
            float bottom = _origin.y - (_cellSize * 0.5f);
            int x = Mathf.FloorToInt((worldPosition.x - left) / _cellSize);
            int y = Mathf.FloorToInt((worldPosition.y - bottom) / _cellSize);
            cell = new Vector2Int(x, y);
            return IsInside(cell);
        }

        private bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;
        }

        private static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        private Vector2Int ComputeStepTowards(Vector2Int from, Vector2Int to, Vector3 worldPosition)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;

            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                return new Vector2Int(dx > 0 ? 1 : -1, 0);
            }

            if (Mathf.Abs(dy) > Mathf.Abs(dx))
            {
                return new Vector2Int(0, dy > 0 ? 1 : -1);
            }

            Vector3 currentWorld = CellToWorld(from);
            Vector2 delta = worldPosition - currentWorld;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return new Vector2Int(dx >= 0 ? 1 : -1, 0);
            }

            return new Vector2Int(0, dy >= 0 ? 1 : -1);
        }

        private static Vector2Int DirectionAt(int index)
        {
            switch (index)
            {
                case 0:
                    return Vector2Int.up;
                case 1:
                    return Vector2Int.right;
                case 2:
                    return Vector2Int.down;
                default:
                    return Vector2Int.left;
            }
        }
    }
}
