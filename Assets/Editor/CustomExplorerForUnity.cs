#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CustomExplorerForUnity
{
    // =========================================================================
    // 1. Data Models (CustomExplorerData)
    // =========================================================================
    public enum NodeType
    {
        Group,
        AssetRef,
        FolderRef
    }

    [Serializable]
    public class FlatNode
    {
        public string Id;
        public string ParentId;
        public string DisplayName;
        public NodeType Type;
        public string AssetGuid;
        public bool IsExpanded;
        public int Order;
    }

    [Serializable]
    public class CustomExplorerSaveData
    {
        public List<FlatNode> Nodes = new List<FlatNode>();
    }

    public class CustomExplorerNode
    {
        public string Id;
        public string DisplayName;
        public NodeType Type;
        public string AssetGuid;

        public CustomExplorerNode Parent;
        public List<CustomExplorerNode> Children = new List<CustomExplorerNode>();

        public bool IsExpanded;
        public bool IsVirtual;

        public CustomExplorerNode()
        {
            Id = Guid.NewGuid().ToString();
        }

        public int GetInstanceID()
        {
            return GetDeterministicHashCode(Id);
        }

        private static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;
                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1) break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }
                return hash1 + (hash2 * 1566083941);
            }
        }
    }

    public static class DataManager
    {
        private const string FilePath = "UserSettings/CustomExplorerData.json";
        public static CustomExplorerNode Root { get; private set; }

        public static void Load()
        {
            Root = new CustomExplorerNode { Id = "ROOT", DisplayName = "Root", Type = NodeType.Group };

            if (!File.Exists(FilePath)) return;

            try
            {
                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<CustomExplorerSaveData>(json);
                if (data == null || data.Nodes == null) return;

                var nodeDict = new Dictionary<string, CustomExplorerNode>();
                var parentMap = new Dictionary<string, string>();

                foreach (var flat in data.Nodes.OrderBy(n => n.Order))
                {
                    var node = new CustomExplorerNode
                    {
                        Id = flat.Id,
                        DisplayName = flat.DisplayName,
                        Type = flat.Type,
                        AssetGuid = flat.AssetGuid,
                        IsExpanded = flat.IsExpanded,
                        IsVirtual = false
                    };
                    nodeDict[node.Id] = node;
                    parentMap[node.Id] = flat.ParentId;
                }

                foreach (var kvp in nodeDict)
                {
                    string parentId = parentMap[kvp.Key];
                    if (string.IsNullOrEmpty(parentId) || !nodeDict.ContainsKey(parentId))
                    {
                        AddChild(Root, kvp.Value);
                    }
                    else
                    {
                        AddChild(nodeDict[parentId], kvp.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomExplorer] Failed to load data: {e.Message}");
            }
        }

        public static void Save()
        {
            var data = new CustomExplorerSaveData();
            FlattenTree(Root, string.Empty, data.Nodes);

            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }

        private static void FlattenTree(CustomExplorerNode node, string parentId, List<FlatNode> list)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (child.IsVirtual) continue;

                list.Add(new FlatNode
                {
                    Id = child.Id,
                    ParentId = parentId,
                    DisplayName = child.DisplayName,
                    Type = child.Type,
                    AssetGuid = child.AssetGuid,
                    IsExpanded = child.IsExpanded,
                    Order = i
                });

                FlattenTree(child, child.Id, list);
            }
        }

        public static void AddChild(CustomExplorerNode parent, CustomExplorerNode child)
        {
            child.Parent = parent;
            parent.Children.Add(child);
        }

        public static void RemoveNode(CustomExplorerNode node)
        {
            if (node.Parent != null)
            {
                node.Parent.Children.Remove(node);
                node.Parent = null;
            }
        }

        public static List<CustomExplorerNode> FindNodesByAssetGuid(string guid)
        {
            var results = new List<CustomExplorerNode>();
            FindNodesRecursive(Root, guid, results);
            return results;
        }

        private static void FindNodesRecursive(CustomExplorerNode node, string guid, List<CustomExplorerNode> results)
        {
            if (!node.IsVirtual && node.AssetGuid == guid)
            {
                results.Add(node);
            }
            foreach (var child in node.Children)
            {
                FindNodesRecursive(child, guid, results);
            }
        }
    }

    // =========================================================================
    // 2. TreeView Implementation (CustomExplorerTreeView)
    // =========================================================================
    public class CustomExplorerTreeViewItem : TreeViewItem
    {
        public CustomExplorerNode Node { get; }
        public string AssetPath { get; }
        public bool IsMissing { get; }

        public CustomExplorerTreeViewItem(CustomExplorerNode node, int depth)
            : base(node.GetInstanceID(), depth, node.DisplayName)
        {
            Node = node;

            if (node.Type != NodeType.Group && !string.IsNullOrEmpty(node.AssetGuid))
            {
                AssetPath = AssetDatabase.GUIDToAssetPath(node.AssetGuid);
                IsMissing = string.IsNullOrEmpty(AssetPath);

                if (IsMissing)
                {
                    displayName += " (Missing)";
                    icon = EditorGUIUtility.IconContent("console.erroricon.sml").image as Texture2D;
                }
                else
                {
                    icon = AssetDatabase.GetCachedIcon(AssetPath) as Texture2D;
                }
            }
            else
            {
                icon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            }
        }
    }

    public class CustomExplorerTreeView : TreeView
    {
        public CustomExplorerTreeView(TreeViewState state) : base(state)
        {
            showAlternatingRowBackgrounds = false;
            showBorder = true;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = GetRows() ?? new List<TreeViewItem>();
            rows.Clear();

            if (DataManager.Root != null)
            {
                foreach (var child in DataManager.Root.Children)
                {
                    AddNodeRecursive(child, root, rows, 0);
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return rows;
        }

        private void AddNodeRecursive(CustomExplorerNode node, TreeViewItem parentItem, IList<TreeViewItem> rows, int depth)
        {
            var item = new CustomExplorerTreeViewItem(node, depth);
            parentItem.AddChild(item);
            rows.Add(item);

            node.IsExpanded = IsExpanded(item.id);

            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                {
                    AddNodeRecursive(child, item, rows, depth + 1);
                }

                if (node.Type == NodeType.FolderRef && !item.IsMissing)
                {
                    AddVirtualChildrenRecursive(item.AssetPath, node, item, rows, depth + 1);
                }
            }
            else
            {
                if (node.Children.Count > 0 || (node.Type == NodeType.FolderRef && !item.IsMissing))
                {
                    item.children = CreateChildListForCollapsedParent();
                }
            }
        }

        private void AddVirtualChildrenRecursive(string folderPath, CustomExplorerNode parentNode, TreeViewItem parentItem, IList<TreeViewItem> rows, int depth)
        {
            if (!AssetDatabase.IsValidFolder(folderPath)) return;

            var entries = Directory.GetFileSystemEntries(folderPath)
                .Where(e => !e.EndsWith(".meta"))
                .Select(e => e.Replace('\\', '/'))
                .OrderBy(e => !AssetDatabase.IsValidFolder(e))
                .ThenBy(e => Path.GetFileName(e))
                .ToList();

            foreach (var entryPath in entries)
            {
                string guid = AssetDatabase.AssetPathToGUID(entryPath);
                if (string.IsNullOrEmpty(guid)) continue;

                bool isFolder = AssetDatabase.IsValidFolder(entryPath);
                string fileName = Path.GetFileName(entryPath);

                var virtualNode = new CustomExplorerNode
                {
                    Id = guid + "_virtual_" + parentNode.Id,
                    DisplayName = fileName,
                    Type = isFolder ? NodeType.FolderRef : NodeType.AssetRef,
                    AssetGuid = guid,
                    Parent = parentNode,
                    IsVirtual = true
                };

                var item = new CustomExplorerTreeViewItem(virtualNode, depth);
                parentItem.AddChild(item);
                rows.Add(item);

                if (isFolder)
                {
                    if (IsExpanded(item.id))
                    {
                        AddVirtualChildrenRecursive(entryPath, virtualNode, item, rows, depth + 1);
                    }
                    else
                    {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (CustomExplorerTreeViewItem)args.item;

            Color oldColor = GUI.color;
            if (item.IsMissing) GUI.color = Color.red;

            base.RowGUI(args);

            if (Event.current.type == EventType.Repaint && item.Node.Type == NodeType.FolderRef && !item.Node.IsVirtual && !item.IsMissing)
            {
                string parentDir = Path.GetFileName(Path.GetDirectoryName(item.AssetPath));
                if (!string.IsNullOrEmpty(parentDir))
                {
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                    Vector2 size = labelStyle.CalcSize(new GUIContent(item.displayName));

                    float indent = GetContentIndent(item);
                    float extraOffset = 18f; // Icon width (16) + padding (2)
                    float xOffset = indent + extraOffset + size.x;

                    Rect suffixRect = new Rect(args.rowRect.x + xOffset, args.rowRect.y, args.rowRect.width - xOffset, args.rowRect.height);

                    labelStyle.normal.textColor = args.selected ? new Color(0.8f, 0.8f, 0.8f, 0.8f) : Color.gray;
                    GUI.Label(suffixRect, $" (/{parentDir})", labelStyle);
                }
            }

            GUI.color = oldColor;
        }

        protected override void SingleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as CustomExplorerTreeViewItem;
            if (item != null && !string.IsNullOrEmpty(item.AssetPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.AssetPath);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as CustomExplorerTreeViewItem;
            if (item == null) return;

            if (item.Node.Type == NodeType.AssetRef && !item.IsMissing)
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.AssetPath);
                if (obj != null) AssetDatabase.OpenAsset(obj);
            }
            else
            {
                SetExpanded(id, !IsExpanded(id));
            }
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as CustomExplorerTreeViewItem;
            if (item != null)
            {
                SetSelection(new[] { id });
                ShowContextMenu(item);
            }
        }

        protected override void ContextClicked()
        {
            SetSelection(new int[0]);
            ShowContextMenu(null);
        }

        public void CreateNewGroup()
        {
            CustomExplorerNode targetParent = DataManager.Root;

            var selection = GetSelection();
            if (selection.Count > 0)
            {
                var item = FindItem(selection[0], rootItem) as CustomExplorerTreeViewItem;
                if (item != null && item.Node != null && !item.Node.IsVirtual)
                {
                    if (item.Node.Type == NodeType.Group)
                    {
                        targetParent = item.Node;
                    }
                    else if (item.Node.Parent != null)
                    {
                        targetParent = item.Node.Parent;
                    }
                }
            }

            var newGroup = new CustomExplorerNode { DisplayName = "New Group", Type = NodeType.Group };
            DataManager.AddChild(targetParent, newGroup);
            DataManager.Save();
            Reload();
            SetExpanded(targetParent.GetInstanceID(), true);

            EditorApplication.delayCall += () =>
            {
                var newItem = FindItem(newGroup.GetInstanceID(), rootItem) as CustomExplorerTreeViewItem;
                if (newItem != null)
                {
                    SetSelection(new[] { newItem.id });
                    BeginRename(newItem);
                }
            };
        }

        public void RemoveSelectedNodes()
        {
            var selection = GetSelection();
            if (selection.Count == 0) return;

            bool changed = false;
            foreach (var id in selection)
            {
                var item = FindItem(id, rootItem) as CustomExplorerTreeViewItem;
                if (item != null && !item.Node.IsVirtual)
                {
                    DataManager.RemoveNode(item.Node);
                    changed = true;
                }
            }

            if (changed)
            {
                DataManager.Save();
                Reload();
                SetSelection(new int[0]);
            }
        }

        public void RenameSelectedItem()
        {
            var selection = GetSelection();
            if (selection.Count == 1)
            {
                var item = FindItem(selection[0], rootItem) as CustomExplorerTreeViewItem;
                if (item != null && CanRename(item))
                {
                    BeginRename(item);
                }
            }
        }

        private void ShowContextMenu(CustomExplorerTreeViewItem item)
        {
            var menu = new GenericMenu();
            var node = item?.Node;

            menu.AddItem(new GUIContent("Add New Group"), false, () => CreateNewGroup());

            if (HasSelection())
            {
                menu.AddSeparator("");

                var selection = GetSelection();
                bool canRename = selection.Count == 1 && node != null && !node.IsVirtual && node.Type == NodeType.Group;

                if (canRename)
                {
                    menu.AddItem(new GUIContent("Rename"), false, () => RenameSelectedItem());
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Rename"));
                }

                bool canRemove = selection.Any(id =>
                {
                    var i = FindItem(id, rootItem) as CustomExplorerTreeViewItem;
                    return i != null && !i.Node.IsVirtual;
                });

                if (canRemove)
                {
                    menu.AddItem(new GUIContent("Remove"), false, () => RemoveSelectedNodes());
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Remove"));
                }
            }

            if (node != null && !string.IsNullOrEmpty(item.AssetPath) && !item.IsMissing)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Open"), false, () => DoubleClickedItem(item.id));
                menu.AddItem(new GUIContent("Reveal in Finder"), false, () => EditorUtility.RevealInFinder(item.AssetPath));
            }

            menu.AddSeparator("");
            if (node != null && (node.Type == NodeType.Group || node.Type == NodeType.FolderRef))
            {
                menu.AddItem(new GUIContent("Expand Recursively"), false, () => SetExpandedRecursively(item.id, true));
                menu.AddItem(new GUIContent("Collapse Recursively"), false, () => SetExpandedRecursively(item.id, false));
                menu.AddSeparator("");
            }
            menu.AddItem(new GUIContent("Expand All"), false, () => ExpandAll());
            menu.AddItem(new GUIContent("Collapse All"), false, () => CollapseAll());

            menu.ShowAsContext();
        }

        private void SetExpandedRecursively(int id, bool expanded)
        {
            var item = FindItem(id, rootItem) as CustomExplorerTreeViewItem;
            if (item != null && item.Node != null)
            {
                SetNodeExpandedRecursively(item.Node, expanded);
                DataManager.Save();
                Reload();
            }
        }

        private void SetNodeExpandedRecursively(CustomExplorerNode node, bool expanded)
        {
            SetExpanded(node.GetInstanceID(), expanded);
            if (!node.IsVirtual)
            {
                node.IsExpanded = expanded;
            }

            foreach (var child in node.Children)
            {
                SetNodeExpandedRecursively(child, expanded);
            }
        }

        protected override bool CanRename(TreeViewItem item)
        {
            var exItem = item as CustomExplorerTreeViewItem;
            return exItem != null && !exItem.Node.IsVirtual && exItem.Node.Type == NodeType.Group;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.acceptedRename && !string.IsNullOrWhiteSpace(args.newName))
            {
                var item = FindItem(args.itemID, rootItem) as CustomExplorerTreeViewItem;
                if (item != null)
                {
                    item.Node.DisplayName = args.newName;
                    DataManager.Save();
                    Reload();
                }
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var draggedNodes = new List<CustomExplorerNode>();
            var paths = new List<string>();
            var objRefs = new List<UnityEngine.Object>();

            foreach (var id in args.draggedItemIDs)
            {
                var item = FindItem(id, rootItem) as CustomExplorerTreeViewItem;
                if (item != null)
                {
                    draggedNodes.Add(item.Node);
                    if (!string.IsNullOrEmpty(item.AssetPath) && !item.IsMissing)
                    {
                        paths.Add(item.AssetPath);
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.AssetPath);
                        if (obj != null) objRefs.Add(obj);
                    }
                }
            }

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("CustomExplorerNodes", draggedNodes);
            DragAndDrop.paths = paths.ToArray();
            DragAndDrop.objectReferences = objRefs.ToArray();
            DragAndDrop.StartDrag("CustomExplorerDrag");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var targetItem = args.parentItem as CustomExplorerTreeViewItem;
            CustomExplorerNode targetNode = targetItem?.Node ?? DataManager.Root;

            if (targetNode.IsVirtual || targetNode.Type != NodeType.Group)
            {
                return DragAndDropVisualMode.Rejected;
            }

            if (args.performDrop)
            {
                var draggedNodes = DragAndDrop.GetGenericData("CustomExplorerNodes") as List<CustomExplorerNode>;

                if (draggedNodes != null && draggedNodes.Count > 0)
                {
                    foreach (var node in draggedNodes)
                    {
                        if (node.IsVirtual) continue;
                        if (IsDescendant(targetNode, node)) continue;

                        DataManager.RemoveNode(node);
                        targetNode.Children.Insert(args.insertAtIndex >= 0 ? args.insertAtIndex : targetNode.Children.Count, node);
                        node.Parent = targetNode;
                    }
                }
                else if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    foreach (var path in DragAndDrop.paths)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);
                        if (string.IsNullOrEmpty(guid)) continue;

                        bool isFolder = AssetDatabase.IsValidFolder(path);
                        var newNode = new CustomExplorerNode
                        {
                            DisplayName = Path.GetFileName(path),
                            Type = isFolder ? NodeType.FolderRef : NodeType.AssetRef,
                            AssetGuid = guid
                        };

                        targetNode.Children.Insert(args.insertAtIndex >= 0 ? args.insertAtIndex : targetNode.Children.Count, newNode);
                        newNode.Parent = targetNode;
                    }
                }

                if (targetNode != DataManager.Root) SetExpanded(targetItem.id, true);

                DataManager.Save();
                Reload();
            }

            return DragAndDropVisualMode.Move;
        }

        private bool IsDescendant(CustomExplorerNode node, CustomExplorerNode potentialAncestor)
        {
            var current = node;
            while (current != null)
            {
                if (current == potentialAncestor) return true;
                current = current.Parent;
            }
            return false;
        }

        protected override void ExpandedStateChanged()
        {
            base.ExpandedStateChanged();
            SyncExpandedState(DataManager.Root);
            DataManager.Save();
        }

        private void SyncExpandedState(CustomExplorerNode node)
        {
            if (node != DataManager.Root && !node.IsVirtual)
            {
                node.IsExpanded = IsExpanded(node.GetInstanceID());
            }
            foreach (var child in node.Children)
            {
                SyncExpandedState(child);
            }
        }
    }

    // =========================================================================
    // 3. Editor Window (CustomExplorerWindow)
    // =========================================================================
    public class CustomExplorerWindow : EditorWindow
    {
        private CustomExplorerTreeView _treeView;
        private TreeViewState _treeViewState;
        private SearchField _searchField;

        [MenuItem("Window/General/Custom Explorer")]
        public static void ShowWindow()
        {
            var window = GetWindow<CustomExplorerWindow>("Custom Explorer");
            window.Show();
        }

        private void OnEnable()
        {
            DataManager.Load();

            if (_treeViewState == null) _treeViewState = new TreeViewState();

            _treeView = new CustomExplorerTreeView(_treeViewState);
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;

            CustomExplorerAssetPostprocessor.OnAssetsChanged += OnAssetsModified;
        }

        private void OnDisable()
        {
            CustomExplorerAssetPostprocessor.OnAssetsChanged -= OnAssetsModified;
        }

        private void OnAssetsModified()
        {
            DataManager.Load();
            _treeView?.Reload();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            HandleKeyboardEvents();
            DrawTreeView();
        }

        private void HandleKeyboardEvents()
        {
            Event e = Event.current;

            if (e.type == EventType.ValidateCommand && (e.commandName == "Delete" || e.commandName == "SoftDelete"))
            {
                e.Use();
            }
            else if (e.type == EventType.ExecuteCommand && (e.commandName == "Delete" || e.commandName == "SoftDelete"))
            {
                _treeView.RemoveSelectedNodes();
                e.Use();
            }
            else if (e.type == EventType.ValidateCommand && e.commandName == "Rename")
            {
                e.Use();
            }
            else if (e.type == EventType.ExecuteCommand && e.commandName == "Rename")
            {
                _treeView.RenameSelectedItem();
                e.Use();
            }
            else if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                {
                    _treeView.RemoveSelectedNodes();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.F2)
                {
                    _treeView.RenameSelectedItem();
                    e.Use();
                }
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Create Group", EditorStyles.toolbarButton))
            {
                _treeView.CreateNewGroup();
            }

            GUILayout.FlexibleSpace();

            _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);

            GUILayout.EndHorizontal();
        }

        private void DrawTreeView()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 10000, 0, 10000);
            _treeView.OnGUI(rect);
        }
    }

    // =========================================================================
    // 4. Asset Postprocessor (CustomExplorerAssetPostprocessor)
    // =========================================================================
    public class CustomExplorerAssetPostprocessor : AssetPostprocessor
    {
        public static event Action OnAssetsChanged;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool hasChanges = false;

            if (DataManager.Root == null)
            {
                DataManager.Load();
            }

            foreach (var deletedPath in deletedAssets)
            {
                bool nodesRemoved = RemoveInvalidNodes(DataManager.Root);
                if (nodesRemoved) hasChanges = true;
            }

            foreach (var movedPath in movedAssets)
            {
                string guid = AssetDatabase.AssetPathToGUID(movedPath);
                if (!string.IsNullOrEmpty(guid))
                {
                    var matchingNodes = DataManager.FindNodesByAssetGuid(guid);
                    foreach (var node in matchingNodes)
                    {
                        node.DisplayName = Path.GetFileName(movedPath);
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                DataManager.Save();
                OnAssetsChanged?.Invoke();
            }
        }

        private static bool RemoveInvalidNodes(CustomExplorerNode node)
        {
            bool removed = false;

            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                var child = node.Children[i];
                if (child.Type != NodeType.Group && !string.IsNullOrEmpty(child.AssetGuid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(child.AssetGuid);
                    if (string.IsNullOrEmpty(path))
                    {
                        DataManager.RemoveNode(child);
                        removed = true;
                        continue;
                    }
                }

                if (RemoveInvalidNodes(child))
                {
                    removed = true;
                }
            }

            return removed;
        }
    }
}
#endif