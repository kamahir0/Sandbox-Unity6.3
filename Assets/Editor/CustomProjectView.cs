#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CustomProjectView
{
    // =========================================================================
    #region Data Model
    // =========================================================================

    public enum NodeType
    {
        Group,      // 仮想フォルダ (CustomExplorer: group)
        AssetRef,   // アセットへの参照 (CustomExplorer: file-ref)
        FolderRef,  // フォルダへのリンク・自動同期 (CustomExplorer: folder-ref)
    }

    [Serializable]
    public class CustomProjectNode
    {
        public string Id;
        public string Label;
        public NodeType Type;
        public string Guid;          // AssetRef: Asset GUID
        public string LinkedPath;    // FolderRef: 実フォルダパス (Assets相対)
        public bool IsExpanded;
        public List<CustomProjectNode> Children = new List<CustomProjectNode>();

        // --- ファクトリ ---
        public static CustomProjectNode CreateGroup(string label)
            => new CustomProjectNode { Id = GenerateId(), Label = label, Type = NodeType.Group, IsExpanded = true };

        public static CustomProjectNode CreateAssetRef(string guid, string label)
            => new CustomProjectNode { Id = GenerateId(), Label = label, Type = NodeType.AssetRef, Guid = guid };

        public static CustomProjectNode CreateFolderRef(string assetRelPath)
            => new CustomProjectNode
            {
                Id = GenerateId(),
                Label = Path.GetFileName(assetRelPath.TrimEnd('/', '\\')),
                Type = NodeType.FolderRef,
                LinkedPath = assetRelPath,
                IsExpanded = false,
            };

        private static string GenerateId()
            => System.Guid.NewGuid().ToString("N").Substring(0, 12);

        // --- ヘルパ ---
        public bool IsGroupLike => Type == NodeType.Group || Type == NodeType.FolderRef;

        /// <summary>実際のファイルシステムパスを返す。なければ null。</summary>
        public string ResolveAssetPath()
        {
            if (Type == NodeType.AssetRef && !string.IsNullOrEmpty(Guid))
                return AssetDatabase.GUIDToAssetPath(Guid);
            if (Type == NodeType.FolderRef && !string.IsNullOrEmpty(LinkedPath))
                return LinkedPath;
            return null;
        }
    }

    [Serializable]
    internal class SerializableModel
    {
        public List<CustomProjectNode> Roots = new List<CustomProjectNode>();
    }

    #endregion

    // =========================================================================
    #region Tree Model  (CRUD / 永続化 / ソート / 検索)
    // =========================================================================

    internal class CustomProjectTreeModel
    {
        private const string PrefKeyPrefix = "CustomProjectView_";
        private SerializableModel _model = new SerializableModel();
        public List<CustomProjectNode> Roots => _model.Roots;
        public bool IsEmpty => _model.Roots.Count == 0;

        private string PrefKey
            => PrefKeyPrefix + Application.dataPath.GetHashCode().ToString();

        // --- 永続化 ---
        public void Load()
        {
            var json = EditorPrefs.GetString(PrefKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { _model = JsonUtility.FromJson<SerializableModel>(json); }
                catch { _model = new SerializableModel(); }
            }
            SyncAllFolderRefs();
        }

        public void Save()
        {
            SortNodes(_model.Roots);
            var json = JsonUtility.ToJson(_model, false);
            EditorPrefs.SetString(PrefKey, json);
        }

        // --- CRUD ---
        public void AddGroup(string label, CustomProjectNode parent = null)
        {
            var node = CustomProjectNode.CreateGroup(label);
            AppendToParent(node, parent);
            Save();
        }

        public void AddAssetRef(string guid, CustomProjectNode parent = null)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return;
            var label = Path.GetFileName(assetPath);
            // 重複チェック
            if (FindNodeByGuid(guid, _model.Roots) != null) return;
            var node = CustomProjectNode.CreateAssetRef(guid, label);
            AppendToParent(node, parent);
            Save();
        }

        public void AddFolderRef(string assetRelPath, CustomProjectNode parent = null)
        {
            var node = CustomProjectNode.CreateFolderRef(assetRelPath);
            AppendToParent(node, parent);
            SyncFolderRef(node);
            Save();
        }

        public void ConvertToGroup(CustomProjectNode node)
        {
            if (node.Type != NodeType.FolderRef) return;
            node.Type = NodeType.Group;
            node.LinkedPath = null;
            Save();
        }

        public void Rename(CustomProjectNode node, string newLabel)
        {
            node.Label = newLabel;
            Save();
        }

        public bool Remove(CustomProjectNode node)
        {
            var result = RemoveRecursive(_model.Roots, node);
            if (result) Save();
            return result;
        }

        private bool RemoveRecursive(List<CustomProjectNode> nodes, CustomProjectNode target)
        {
            var idx = nodes.FindIndex(n => n.Id == target.Id);
            if (idx >= 0) { nodes.RemoveAt(idx); return true; }
            return nodes.Any(n => n.Children != null && RemoveRecursive(n.Children, target));
        }

        public bool MoveNode(CustomProjectNode source, CustomProjectNode targetParent)
        {
            if (source == targetParent) return false;
            if (IsDescendant(source, targetParent)) return false;
            if (IsChildOfFolderRef(source, _model.Roots)) return false;

            if (!RemoveRecursive(_model.Roots, source)) return false;
            AppendToParent(source, targetParent);
            Save();
            return true;
        }

        private bool IsDescendant(CustomProjectNode ancestor, CustomProjectNode node)
        {
            if (node == null) return false;
            return ancestor.Children != null &&
                   ancestor.Children.Any(c => c == node || IsDescendant(c, node));
        }

        // --- FolderRef 自動同期 ---
        public void SyncAllFolderRefs()
        {
            SyncFolderRefsRecursive(_model.Roots);
        }

        private void SyncFolderRefsRecursive(List<CustomProjectNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (n.Type == NodeType.FolderRef) SyncFolderRef(n);
                if (n.Children != null) SyncFolderRefsRecursive(n.Children);
            }
        }

        public void SyncFolderRef(CustomProjectNode node)
        {
            if (node.Type != NodeType.FolderRef || string.IsNullOrEmpty(node.LinkedPath)) return;
            var absPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", node.LinkedPath));
            if (!Directory.Exists(absPath)) { node.Children = new List<CustomProjectNode>(); return; }

            node.Children = new List<CustomProjectNode>();
            ScanDirectory(absPath, node.LinkedPath, node);
        }

        private void ScanDirectory(string absPath, string relBase, CustomProjectNode parent)
        {
            // フォルダを先に
            foreach (var dir in Directory.GetDirectories(absPath).OrderBy(d => d))
            {
                var name = Path.GetFileName(dir);
                if (ShouldExclude(name)) continue;
                var relPath = relBase + "/" + name;
                var child = CustomProjectNode.CreateGroup(name);
                child.IsExpanded = false;
                parent.Children.Add(child);
                ScanDirectory(dir, relPath, child);
            }
            // ファイル
            foreach (var file in Directory.GetFiles(absPath).OrderBy(f => f))
            {
                var name = Path.GetFileName(file);
                if (ShouldExclude(name)) continue;
                var relPath = (relBase + "/" + name).Replace("\\", "/");
                // Assets/... 形式に正規化
                var assetPath = relPath.StartsWith("Assets/") ? relPath : relPath;
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid)) continue;
                parent.Children.Add(CustomProjectNode.CreateAssetRef(guid, name));
            }
        }

        private static bool ShouldExclude(string name)
        {
            if (name.EndsWith(".meta")) return true;
            if (name.StartsWith(".")) return true;
            if (name.EndsWith("~")) return true;
            if (name == "Temp" || name == "Library" || name == "obj") return true;
            return false;
        }

        // --- インデックス ---
        public CustomProjectNode FindNodeByGuid(string guid, List<CustomProjectNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (n.Type == NodeType.AssetRef && n.Guid == guid) return n;
                if (n.Children != null)
                {
                    var found = FindNodeByGuid(guid, n.Children);
                    if (found != null) return found;
                }
            }
            return null;
        }

        public CustomProjectNode FindNodeById(string id, List<CustomProjectNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (n.Id == id) return n;
                if (n.Children != null)
                {
                    var found = FindNodeById(id, n.Children);
                    if (found != null) return found;
                }
            }
            return null;
        }

        public CustomProjectNode GetParent(CustomProjectNode target, List<CustomProjectNode> nodes = null)
        {
            nodes = nodes ?? _model.Roots;
            foreach (var n in nodes)
            {
                if (n.Children != null && n.Children.Any(c => c.Id == target.Id)) return n;
                if (n.Children != null)
                {
                    var found = GetParent(target, n.Children);
                    if (found != null) return found;
                }
            }
            return null;
        }

        public bool IsChildOfFolderRef(CustomProjectNode node, List<CustomProjectNode> roots)
        {
            var parent = GetParent(node, roots);
            while (parent != null)
            {
                if (parent.Type == NodeType.FolderRef) return true;
                parent = GetParent(parent, roots);
            }
            return false;
        }

        // --- ファイル追従 ---
        public void HandleAssetMoved(string oldPath, string newPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(newPath);
            if (string.IsNullOrEmpty(guid)) return;
            var node = FindNodeByGuid(guid, _model.Roots);
            if (node == null) return;
            if (!IsChildOfFolderRef(node, _model.Roots))
            {
                node.Label = Path.GetFileName(newPath);
            }
            Save();
        }

        public void HandleAssetDeleted(string deletedPath)
        {
            // GUID が無効になったノードを削除
            var toRemove = CollectInvalidAssetRefs(_model.Roots);
            foreach (var n in toRemove) RemoveRecursive(_model.Roots, n);
            if (toRemove.Count > 0) Save();
        }

        private List<CustomProjectNode> CollectInvalidAssetRefs(List<CustomProjectNode> nodes)
        {
            var result = new List<CustomProjectNode>();
            foreach (var n in nodes)
            {
                if (n.Type == NodeType.AssetRef && !string.IsNullOrEmpty(n.Guid))
                {
                    var path = AssetDatabase.GUIDToAssetPath(n.Guid);
                    if (string.IsNullOrEmpty(path)) result.Add(n);
                }
                if (n.Children != null) result.AddRange(CollectInvalidAssetRefs(n.Children));
            }
            return result;
        }

        // --- ソート (VSCode CustomExplorer 準拠) ---
        private void SortNodes(List<CustomProjectNode> nodes)
        {
            // グループ優先 → フォルダRef → アセットRef → 名前順
            nodes.Sort((a, b) =>
            {
                int pa = GetPriority(a), pb = GetPriority(b);
                if (pa != pb) return pa - pb;
                return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
            });
            foreach (var n in nodes)
                if (n.Children != null && n.Children.Count > 0)
                    SortNodes(n.Children);
        }

        private int GetPriority(CustomProjectNode n)
        {
            if (n.Type == NodeType.Group) return 0;
            if (n.Type == NodeType.FolderRef) return 1;
            return 2;
        }

        private void AppendToParent(CustomProjectNode node, CustomProjectNode parent)
        {
            if (parent != null && parent.IsGroupLike)
            {
                if (parent.Children == null) parent.Children = new List<CustomProjectNode>();
                parent.Children.Add(node);
                parent.IsExpanded = true;
            }
            else
            {
                _model.Roots.Add(node);
            }
        }

        // --- 検索 ---
        public List<CustomProjectNode> Search(string query, List<CustomProjectNode> nodes = null)
        {
            nodes = nodes ?? _model.Roots;
            var result = new List<CustomProjectNode>();
            foreach (var n in nodes)
            {
                if (n.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(n);
                if (n.Children != null)
                    result.AddRange(Search(query, n.Children));
            }
            return result;
        }

        // --- 展開状態の保存 ---
        public void SetExpanded(CustomProjectNode node, bool expanded)
        {
            node.IsExpanded = expanded;
            Save();
        }
    }

    #endregion

    // =========================================================================
    #region Asset Postprocessor
    // =========================================================================

    internal class CustomProjectAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var window = EditorWindow.HasOpenInstances<CustomProjectViewWindow>()
                ? EditorWindow.GetWindow<CustomProjectViewWindow>(false, null, false)
                : null;
            if (window == null) return;

            bool changed = false;
            for (int i = 0; i < movedAssets.Length; i++)
            {
                window.Model.HandleAssetMoved(movedFromAssetPaths[i], movedAssets[i]);
                changed = true;
            }
            foreach (var deleted in deletedAssets)
            {
                window.Model.HandleAssetDeleted(deleted);
                changed = true;
            }
            // FolderRef の自動同期
            window.Model.SyncAllFolderRefs();
            if (changed || importedAssets.Length > 0)
                window.RequestRefresh();
        }
    }

    #endregion

    // =========================================================================
    #region TreeView Item
    // =========================================================================

    internal class CustomProjectViewItem : TreeViewItem
    {
        public CustomProjectNode Node;
        public string AssetPath;
        public bool IsMissing;

        public CustomProjectViewItem(int id, int depth, string displayName, CustomProjectNode node)
            : base(id, depth, displayName)
        {
            Node = node;

            // アセットパス解決 & アイコン設定 (旧 CustomExplorer 方式)
            AssetPath = node.ResolveAssetPath();

            switch (node.Type)
            {
                case NodeType.AssetRef:
                    IsMissing = string.IsNullOrEmpty(AssetPath);
                    if (IsMissing)
                    {
                        this.displayName += " (Missing)";
                        icon = EditorGUIUtility.IconContent("console.erroricon.sml").image as Texture2D;
                    }
                    else
                    {
                        icon = AssetDatabase.GetCachedIcon(AssetPath) as Texture2D;
                    }
                    break;

                case NodeType.FolderRef:
                    IsMissing = !string.IsNullOrEmpty(node.LinkedPath)
                        && !Directory.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", node.LinkedPath)));
                    icon = (EditorGUIUtility.FindTexture("FolderFavorite Icon")
                        ?? EditorGUIUtility.FindTexture("Folder Icon")) as Texture2D;
                    break;

                case NodeType.Group:
                    IsMissing = false;
                    icon = (node.IsExpanded
                        ? EditorGUIUtility.FindTexture("FolderOpened Icon")
                        : EditorGUIUtility.FindTexture("Folder Icon")) as Texture2D;
                    break;
            }
        }
    }

    #endregion

    // =========================================================================
    #region TreeView
    // =========================================================================

    internal class CustomProjectTreeView : TreeView
    {
        private CustomProjectTreeModel _model;
        private CustomProjectViewWindow _window;
        private string _searchQuery = "";

        // インライン操作ボタン幅
        private const float ButtonW = 18f;
        private const float ButtonSpacing = 1f;

        // アイテムID ↔ ノード対応
        private Dictionary<int, CustomProjectNode> _idToNode = new Dictionary<int, CustomProjectNode>();
        private int _nextId = 1;

        // リネーム中フラグ
        private int _renamingId = -1;

        public CustomProjectTreeView(TreeViewState state, CustomProjectTreeModel model, CustomProjectViewWindow window)
            : base(state)
        {
            _model = model;
            _window = window;
            showAlternatingRowBackgrounds = false;
            showBorder = true;
            rowHeight = EditorGUIUtility.singleLineHeight + 2f;
        }

        public void SetSearch(string query)
        {
            _searchQuery = query;
            Reload();
        }

        // --- TreeView 構築 ---
        protected override TreeViewItem BuildRoot()
        {
            _idToNode.Clear();
            _nextId = 1;
            var root = new TreeViewItem(-1, -1, "root");

            if (string.IsNullOrEmpty(_searchQuery))
            {
                BuildFromNodes(_model.Roots, root);
            }
            else
            {
                // 検索モード: フラットリストで表示
                var hits = _model.Search(_searchQuery);
                foreach (var n in hits)
                {
                    var item = CreateItem(n, 0);
                    root.AddChild(item);
                }
            }

            if (!root.hasChildren)
                root.AddChild(new TreeViewItem(0, 0, ""));

            SetupDepthsFromParentsAndChildren(root);

            // 展開状態を復元
            if (string.IsNullOrEmpty(_searchQuery))
                ApplyExpandedState(_model.Roots);

            return root;
        }

        private void BuildFromNodes(List<CustomProjectNode> nodes, TreeViewItem parent)
        {
            foreach (var node in nodes)
            {
                var item = CreateItem(node, parent.depth + 1);
                parent.AddChild(item);
                if (node.IsGroupLike && node.Children != null && node.Children.Count > 0)
                    BuildFromNodes(node.Children, item);
            }
        }

        private CustomProjectViewItem CreateItem(CustomProjectNode node, int depth)
        {
            int id = _nextId++;
            _idToNode[id] = node;
            return new CustomProjectViewItem(id, depth, node.Label, node);
        }

        private void ApplyExpandedState(List<CustomProjectNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (!n.IsGroupLike) continue;
                var item = FindItem(GetIdForNode(n), rootItem);
                if (item == null) continue;
                if (n.IsExpanded) SetExpanded(item.id, true);
                if (n.Children != null) ApplyExpandedState(n.Children);
            }
        }

        private int GetIdForNode(CustomProjectNode node)
        {
            foreach (var kv in _idToNode)
                if (kv.Value.Id == node.Id) return kv.Key;
            return -1;
        }

        public CustomProjectNode GetNodeForId(int id)
        {
            _idToNode.TryGetValue(id, out var node);
            return node;
        }

        // --- 行描画 (旧 CustomExplorer 方式: base.RowGUI + サフィックス + インラインボタン) ---
        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as CustomProjectViewItem;
            if (item == null || item.Node == null)
            {
                // 空表示
                if (_model.IsEmpty && string.IsNullOrEmpty(_searchQuery))
                    DrawEmptyLabel(args.rowRect);
                return;
            }

            var node = item.Node;
            var rowRect = args.rowRect;

            // Missing 時は赤テキスト (旧 CustomExplorer 方式)
            Color oldColor = GUI.color;
            if (item.IsMissing) GUI.color = Color.red;

            // Unity 標準 TreeView 描画 (フォールドアウト三角形 + アイコン + ラベル)
            base.RowGUI(args);

            GUI.color = oldColor;

            // サフィックス: 親ディレクトリ名をラベル右横にインライン表示 (旧 CustomExplorer 方式)
            if (Event.current.type == EventType.Repaint)
            {
                DrawPathSuffix(args, item, node);
            }

            // インラインボタン描画 (新 CustomProjectView 方式: hover/選択時)
            if (args.selected || IsHovered(rowRect))
            {
                float btnAreaWidth = CalcButtonAreaWidth(node);
                var btnRect = new Rect(rowRect.xMax - btnAreaWidth, rowRect.y, btnAreaWidth, rowRect.height);
                DrawInlineButtons(btnRect, node, item.id);
            }
        }

        private void DrawPathSuffix(RowGUIArgs args, CustomProjectViewItem item, CustomProjectNode node)
        {
            string parentDir = null;

            if (node.Type == NodeType.FolderRef && !string.IsNullOrEmpty(node.LinkedPath) && !item.IsMissing)
            {
                parentDir = Path.GetFileName(Path.GetDirectoryName(node.LinkedPath.TrimEnd('/', '\\')));
            }
            else if (node.Type == NodeType.AssetRef && !item.IsMissing && !string.IsNullOrEmpty(item.AssetPath))
            {
                parentDir = Path.GetFileName(Path.GetDirectoryName(item.AssetPath));
            }

            if (string.IsNullOrEmpty(parentDir)) return;

            var labelStyle = new GUIStyle(EditorStyles.label);
            Vector2 size = labelStyle.CalcSize(new GUIContent(item.displayName));
            float indent = GetContentIndent(item);
            float iconWidth = 18f;
            float xOffset = indent + iconWidth + size.x;

            var suffixRect = new Rect(args.rowRect.x + xOffset, args.rowRect.y, args.rowRect.width - xOffset, args.rowRect.height);
            labelStyle.normal.textColor = args.selected ? new Color(0.8f, 0.8f, 0.8f, 0.8f) : Color.gray;
            GUI.Label(suffixRect, $" (/{parentDir})", labelStyle);
        }

        private bool IsHovered(Rect rect)
        {
            return rect.Contains(Event.current.mousePosition);
        }

        private void DrawEmptyLabel(Rect rect)
        {
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            GUI.Label(rect, "まだ項目がありません。\"+ Group\" でグループを追加してください。", style);
        }

        private float CalcButtonAreaWidth(CustomProjectNode node)
        {
            int count = 0;
            if (node.Type == NodeType.Group)
                count = 4; // [+Group][▶Expand][◀Collapse][×Remove]
            else if (node.Type == NodeType.FolderRef)
                count = 3; // [▶][◀][×]
            else
                count = 1; // [×]
            return count * (ButtonW + ButtonSpacing) + 4f;
        }

        private void DrawInlineButtons(Rect rect, CustomProjectNode node, int itemId)
        {
            float x = rect.x + 2f;
            float y = rect.y + (rect.height - ButtonW) / 2f;

            GUIStyle btnStyle = EditorStyles.iconButton;

            if (node.Type == NodeType.Group)
            {
                // [+ サブグループ]
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(EditorGUIUtility.FindTexture("CreateAddNew") ?? EditorGUIUtility.IconContent("Toolbar Plus").image,
                    "サブグループを追加"), btnStyle))
                {
                    AddSubGroup(node);
                }
                x += ButtonW + ButtonSpacing;

                // [▶ 展開]
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(EditorGUIUtility.IconContent("d_SceneViewOrtho").image, "再帰的に展開"), btnStyle))
                {
                    ExpandRecursive(itemId, true);
                }
                x += ButtonW + ButtonSpacing;

                // [◀ 折りたたむ]
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(EditorGUIUtility.FindTexture("d_winbtn_win_min") ?? EditorGUIUtility.IconContent("d_winbtn_win_min").image, "再帰的に折りたたむ"), btnStyle))
                {
                    ExpandRecursive(itemId, false);
                }
                x += ButtonW + ButtonSpacing;
            }
            else if (node.Type == NodeType.FolderRef)
            {
                // [▶ 展開]
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(EditorGUIUtility.IconContent("d_SceneViewOrtho").image, "再帰的に展開"), btnStyle))
                {
                    ExpandRecursive(itemId, true);
                }
                x += ButtonW + ButtonSpacing;

                // [◀ 折りたたむ]
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(EditorGUIUtility.IconContent("d_winbtn_win_min").image, "再帰的に折りたたむ"), btnStyle))
                {
                    ExpandRecursive(itemId, false);
                }
                x += ButtonW + ButtonSpacing;
            }

            // [× 削除] — 全種別
            if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                new GUIContent(EditorGUIUtility.FindTexture("winbtn_win_close") ?? EditorGUIUtility.IconContent("d_winbtn_win_close").image, "取り除く"), btnStyle))
            {
                if (EditorUtility.DisplayDialog("確認", $"\"{node.Label}\" をリストから取り除きますか？\n（実ファイルは削除されません）", "取り除く", "キャンセル"))
                {
                    _model.Remove(node);
                    Reload();
                }
            }
        }

        // --- 展開 / 折りたたみ ---
        private void ExpandRecursive(int rootItemId, bool expand)
        {
            var item = FindItem(rootItemId, rootItem);
            if (item == null) return;
            SetExpandedRecursive(item, expand);
            _window.RequestRefresh();
        }

        private void SetExpandedRecursive(TreeViewItem item, bool expand)
        {
            SetExpanded(item.id, expand);
            var node = GetNodeForId(item.id);
            if (node != null) node.IsExpanded = expand;
            if (item.children != null)
                foreach (var child in item.children)
                    SetExpandedRecursive(child, expand);
        }

        public void ExpandAll(bool expand)
        {
            if (rootItem?.children == null) return;
            foreach (var child in rootItem.children)
                SetExpandedRecursive(child, expand);
            _model.Save();
            Reload();
        }

        // --- コンテキストメニュー ---
        protected override void ContextClickedItem(int id)
        {
            var node = GetNodeForId(id);
            if (node == null) return;
            ShowContextMenu(node, id);
        }

        private void ShowContextMenu(CustomProjectNode node, int itemId)
        {
            var menu = new GenericMenu();

            if (node.Type == NodeType.Group)
            {
                menu.AddItem(new GUIContent("サブグループを追加"), false, () => AddSubGroup(node));
                menu.AddItem(new GUIContent("項目を追加..."), false, () => AddAssetToGroup(node));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("グループ名の変更"), false, () =>
                {
                    _renamingId = itemId;
                    BeginRename(FindItem(itemId, rootItem));
                });
                menu.AddItem(new GUIContent("取り除く"), false, () => RemoveWithConfirm(node));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("再帰的に展開"), false, () => ExpandRecursive(itemId, true));
                menu.AddItem(new GUIContent("再帰的に折りたたむ"), false, () => ExpandRecursive(itemId, false));
            }
            else if (node.Type == NodeType.FolderRef)
            {
                menu.AddItem(new GUIContent("グループに変換"), false, () =>
                {
                    _model.ConvertToGroup(node);
                    Reload();
                });
                menu.AddItem(new GUIContent("OS で表示"), false, () => RevealInFinder(node));
                menu.AddItem(new GUIContent("パスのコピー"), false, () => CopyPath(node, false));
                menu.AddItem(new GUIContent("相対パスのコピー"), false, () => CopyPath(node, true));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("取り除く"), false, () => RemoveWithConfirm(node));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("再帰的に展開"), false, () => ExpandRecursive(itemId, true));
                menu.AddItem(new GUIContent("再帰的に折りたたむ"), false, () => ExpandRecursive(itemId, false));
            }
            else if (node.Type == NodeType.AssetRef)
            {
                var assetPath = node.ResolveAssetPath();
                menu.AddItem(new GUIContent("開く"), false, () => OpenAsset(node));
                menu.AddItem(new GUIContent("OS で表示"), false, () => RevealInFinder(node));
                menu.AddItem(new GUIContent("パスのコピー"), false, () => CopyPath(node, false));
                menu.AddItem(new GUIContent("相対パスのコピー"), false, () => CopyPath(node, true));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("取り除く（リストのみ）"), false, () => RemoveWithConfirm(node));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Project で選択"), false, () =>
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                });

                // アセットが存在する場合のみ Create/Delete メニュー
                if (!string.IsNullOrEmpty(assetPath))
                {
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("名前の変更"), false, () =>
                    {
                        _renamingId = itemId;
                        BeginRename(FindItem(itemId, rootItem));
                    });
                    menu.AddItem(new GUIContent("削除（実ファイル）"), false, () =>
                    {
                        if (EditorUtility.DisplayDialog("確認", $"\"{node.Label}\" を削除しますか？\nこの操作は取り消せません。", "削除", "キャンセル"))
                        {
                            AssetDatabase.DeleteAsset(assetPath);
                        }
                    });
                }
            }

            menu.ShowAsContext();
        }

        // --- リネーム ---
        protected override bool CanRename(TreeViewItem item)
        {
            var node = GetNodeForId(item.id);
            return node != null; // 全種別でリネーム可
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            _renamingId = -1;
            if (!args.acceptedRename) return;
            var node = GetNodeForId(args.itemID);
            if (node == null) return;

            if (node.Type == NodeType.Group)
            {
                _model.Rename(node, args.newName);
                Reload();
            }
            else if (node.Type == NodeType.AssetRef)
            {
                // 実ファイルのリネーム
                var assetPath = node.ResolveAssetPath();
                if (string.IsNullOrEmpty(assetPath)) return;
                var dir = Path.GetDirectoryName(assetPath);
                var ext = Path.GetExtension(assetPath);
                var newName = args.newName.EndsWith(ext) ? args.newName : args.newName + ext;
                var newPath = Path.Combine(dir, newName).Replace("\\", "/");
                var err = AssetDatabase.RenameAsset(assetPath, Path.GetFileNameWithoutExtension(newName));
                if (!string.IsNullOrEmpty(err))
                    EditorUtility.DisplayDialog("エラー", err, "OK");
                else
                    Reload();
            }
        }

        // --- ダブルクリック ---
        protected override void DoubleClickedItem(int id)
        {
            var node = GetNodeForId(id);
            if (node == null) return;
            OpenAsset(node);
        }

        // --- 選択変更 → Unity Selection 連動 ---
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0) return;
            var node = GetNodeForId(selectedIds[0]);
            if (node == null) return;
            if (node.Type == NodeType.AssetRef && !string.IsNullOrEmpty(node.Guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(node.Guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (obj != null) Selection.activeObject = obj;
                }
            }
        }

        // --- 展開状態変更の保存 ---
        protected override void ExpandedStateChanged()
        {
            // 展開状態を全ノードに反映
            SyncExpandedStateToModel(_model.Roots);
            _model.Save();
        }

        private void SyncExpandedStateToModel(List<CustomProjectNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (!n.IsGroupLike) continue;
                var id = GetIdForNode(n);
                if (id >= 0) n.IsExpanded = IsExpanded(id);
                if (n.Children != null) SyncExpandedStateToModel(n.Children);
            }
        }

        // --- D&D ---
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            foreach (var id in args.draggedItemIDs)
            {
                var node = GetNodeForId(id);
                if (node != null && _model.IsChildOfFolderRef(node, _model.Roots))
                    return false; // FolderRef 配下は移動不可
            }
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            var nodes = args.draggedItemIDs
                .Select(id => GetNodeForId(id))
                .Where(n => n != null)
                .ToList();

            DragAndDrop.SetGenericData("CustomProjectViewNodes", nodes);

            // Unity の Asset D&D にも対応
            var unityObjects = nodes
                .Where(n => n.Type == NodeType.AssetRef && !string.IsNullOrEmpty(n.Guid))
                .Select(n => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(n.Guid)))
                .Where(o => o != null)
                .ToArray();
            if (unityObjects.Length > 0)
                DragAndDrop.objectReferences = unityObjects;

            DragAndDrop.StartDrag("CustomProjectView");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // 内部 D&D
            var internalNodes = DragAndDrop.GetGenericData("CustomProjectViewNodes") as List<CustomProjectNode>;
            if (internalNodes != null)
            {
                if (args.performDrop)
                {
                    CustomProjectNode targetParent = null;
                    if (args.parentItem is CustomProjectViewItem parentViewItem)
                        targetParent = parentViewItem.Node;

                    foreach (var source in internalNodes)
                        _model.MoveNode(source, targetParent);
                    Reload();
                }
                return DragAndDropVisualMode.Move;
            }

            // 外部（Unity ProjectWindow）からの D&D
            if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            {
                // FolderRef / FolderRef 配下へのドロップ禁止
                if (args.parentItem is CustomProjectViewItem pItem &&
                    (pItem.Node.Type == NodeType.FolderRef || _model.IsChildOfFolderRef(pItem.Node, _model.Roots)))
                    return DragAndDropVisualMode.Rejected;

                if (args.performDrop)
                {
                    CustomProjectNode targetParent = null;
                    if (args.parentItem is CustomProjectViewItem parentViewItem)
                        targetParent = parentViewItem.Node;

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        var path = AssetDatabase.GetAssetPath(obj);
                        if (string.IsNullOrEmpty(path)) continue;

                        if (AssetDatabase.IsValidFolder(path))
                            _model.AddFolderRef(path, targetParent);
                        else
                            _model.AddAssetRef(AssetDatabase.AssetPathToGUID(path), targetParent);
                    }
                    Reload();
                }
                return DragAndDropVisualMode.Copy;
            }

            return DragAndDropVisualMode.None;
        }

        // --- アクション ヘルパ ---
        private void AddSubGroup(CustomProjectNode parent)
        {
            _model.AddGroup("New Group", parent);
            Reload();
            // 新規グループを即リネームモードに
            var newNode = parent.Children?.LastOrDefault();
            if (newNode != null)
            {
                int id = GetIdForNode(newNode);
                if (id >= 0)
                {
                    SetSelection(new[] { id });
                    BeginRename(FindItem(id, rootItem));
                }
            }
        }

        private void AddAssetToGroup(CustomProjectNode parent)
        {
            var path = EditorUtility.OpenFilePanel("項目を追加", "Assets", "");
            if (string.IsNullOrEmpty(path)) return;
            // 絶対パス → アセットパスに変換
            var dataPath = Application.dataPath;
            if (path.StartsWith(dataPath))
                path = "Assets" + path.Substring(dataPath.Length);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("エラー", "Assets フォルダ外のファイルは追加できません。", "OK");
                return;
            }
            _model.AddAssetRef(guid, parent);
            Reload();
        }

        private void RemoveWithConfirm(CustomProjectNode node)
        {
            if (EditorUtility.DisplayDialog("確認",
                $"\"{node.Label}\" をリストから取り除きますか？\n（実ファイルは削除されません）",
                "取り除く", "キャンセル"))
            {
                _model.Remove(node);
                Reload();
            }
        }

        private void OpenAsset(CustomProjectNode node)
        {
            if (node.Type == NodeType.AssetRef && !string.IsNullOrEmpty(node.Guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(node.Guid);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null) AssetDatabase.OpenAsset(obj);
            }
        }

        private void RevealInFinder(CustomProjectNode node)
        {
            var path = node.ResolveAssetPath();
            if (string.IsNullOrEmpty(path)) return;
            var absPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            EditorUtility.RevealInFinder(absPath);
        }

        private void CopyPath(CustomProjectNode node, bool relative)
        {
            var path = node.ResolveAssetPath();
            if (string.IsNullOrEmpty(path)) return;
            if (!relative)
                path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            GUIUtility.systemCopyBuffer = path;
        }

        private void AddSubGroup(CustomProjectNode parent, bool dummy) { } // overload guard

        // Unity Selection → ツリー自動選択
        public void SyncSelectionFromUnity()
        {
            var obj = Selection.activeObject;
            if (obj == null) return;
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) return;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var node = _model.FindNodeByGuid(guid, _model.Roots);
            if (node == null) return;
            var id = GetIdForNode(node);
            if (id < 0) return;
            SetSelection(new[] { id }, TreeViewSelectionOptions.RevealAndFrame);
        }
    }

    #endregion

    // =========================================================================
    #region EditorWindow
    // =========================================================================

    public class CustomProjectViewWindow : EditorWindow
    {
        [SerializeField] private TreeViewState _treeViewState;
        [SerializeField] private string _searchQuery = "";

        private CustomProjectTreeView _treeView;
        private SearchField _searchField;
        private bool _needsRefresh = false;

        internal CustomProjectTreeModel Model { get; private set; }

        [MenuItem("Window/Custom Project View")]
        public static void Open()
        {
            var window = GetWindow<CustomProjectViewWindow>();
            window.titleContent = new GUIContent("Custom Project", EditorGUIUtility.FindTexture("Project") ?? EditorGUIUtility.FindTexture("d_Project"));
            window.Show();
        }

        private void OnEnable()
        {
            if (_treeViewState == null)
                _treeViewState = new TreeViewState();

            Model = new CustomProjectTreeModel();
            Model.Load();

            _treeView = new CustomProjectTreeView(_treeViewState, Model, this);
            _treeView.Reload();

            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;

            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (_treeView != null)
                _treeView.SyncSelectionFromUnity();
            Repaint();
        }

        public void RequestRefresh()
        {
            _needsRefresh = true;
            Repaint();
        }

        private void OnGUI()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                _treeView.Reload();
            }

            DrawToolbar();
            DrawSearchBar();
            DrawTreeView();
        }

        // ツールバー (タイトル・グローバルボタン)
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // ワークスペース名
            var workspaceName = !string.IsNullOrEmpty(Application.productName)
                ? Application.productName
                : "Custom Project";
            GUILayout.Label(workspaceName, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));

            GUILayout.FlexibleSpace();

            // [+ Group] ルートにグループを追加
            if (GUILayout.Button(new GUIContent("+ Group", "グループをルートに追加"),
                EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                AddRootGroup();
            }

            // [項目を追加]
            if (GUILayout.Button(new GUIContent(
                EditorGUIUtility.FindTexture("d_Toolbar Plus") ?? EditorGUIUtility.FindTexture("Toolbar Plus"),
                "項目を追加"),
                EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                AddAssetToRoot();
            }

            // [すべて展開]
            if (GUILayout.Button(new GUIContent(
                EditorGUIUtility.FindTexture("UnityEditor.SceneHierarchyWindow") ?? EditorGUIUtility.FindTexture("d_SceneViewOrtho"),
                "すべて展開"),
                EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                _treeView.ExpandAll(true);
            }

            // [すべて折りたたむ]
            if (GUILayout.Button(new GUIContent(
                EditorGUIUtility.FindTexture("d_winbtn_win_min"),
                "すべて折りたたむ"),
                EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                _treeView.ExpandAll(false);
            }

            EditorGUILayout.EndHorizontal();
        }

        // 検索バー
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var newQuery = _searchField.OnToolbarGUI(_searchQuery);
            if (newQuery != _searchQuery)
            {
                _searchQuery = newQuery;
                _treeView.SetSearch(_searchQuery);
            }
            EditorGUILayout.EndHorizontal();
        }

        // ツリービュー本体
        private void DrawTreeView()
        {
            var rect = GUILayoutUtility.GetRect(0, position.height, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // D&D受け入れ: ビュー外からのドロップも可能にする
            HandleExternalDrop(rect);

            _treeView.OnGUI(rect);
        }

        // ウィンドウ外からの D&D ハンドリング (ツリーの空白エリアへのドロップ)
        private void HandleExternalDrop(Rect rect)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;

            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (AssetDatabase.IsValidFolder(path))
                        Model.AddFolderRef(path);
                    else
                        Model.AddAssetRef(AssetDatabase.AssetPathToGUID(path));
                }
                _treeView.Reload();
            }
            evt.Use();
        }

        // --- アクション ---
        private void AddRootGroup()
        {
            // 入力ダイアログ代わりにポップアップを使用
            PopupNameDialog.Show("グループを追加", "グループ名を入力してください", "New Group", name =>
            {
                Model.AddGroup(name);
                _treeView.Reload();
            });
        }

        private void AddAssetToRoot()
        {
            var path = EditorUtility.OpenFilePanel("項目を追加", "Assets", "");
            if (string.IsNullOrEmpty(path)) return;
            var dataPath = Application.dataPath;
            if (path.StartsWith(dataPath))
                path = "Assets" + path.Substring(dataPath.Length);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("エラー", "Assets フォルダ外のファイルは追加できません。", "OK");
                return;
            }
            Model.AddAssetRef(guid);
            _treeView.Reload();
        }
    }

    #endregion

    // =========================================================================
    #region Popup Name Dialog
    // =========================================================================

    /// <summary>
    /// グループ名入力用の軽量ポップアップウィンドウ。
    /// EditorInputDialog の代替（依存なし）。
    /// </summary>
    internal class PopupNameDialog : EditorWindow
    {
        private string _title;
        private string _message;
        private string _value;
        private Action<string> _onConfirm;
        private bool _focused = false;

        public static void Show(string title, string message, string defaultValue, Action<string> onConfirm)
        {
            var win = CreateInstance<PopupNameDialog>();
            win._title = title;
            win._message = message;
            win._value = defaultValue;
            win._onConfirm = onConfirm;
            win.titleContent = new GUIContent(title);
            win.minSize = new Vector2(300, 90);
            win.maxSize = new Vector2(300, 90);
            win.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(_message);
            EditorGUILayout.Space(4);

            GUI.SetNextControlName("NameField");
            _value = EditorGUILayout.TextField(_value);

            if (!_focused)
            {
                EditorGUI.FocusTextInControl("NameField");
                _focused = true;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("キャンセル", GUILayout.Width(80)))
                Close();

            GUI.enabled = !string.IsNullOrWhiteSpace(_value);
            if (GUILayout.Button("追加", GUILayout.Width(80)) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                _onConfirm?.Invoke(_value.Trim());
                Close();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }
    }

    #endregion
}
#endif