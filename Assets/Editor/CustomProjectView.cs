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
    public enum ProjectNodeKind
    {
        Group,
        Folder,
        Asset,
    }

    public enum ProjectNodeSource
    {
        Manual,
        FolderRefRoot,
        FolderRefSynced,
    }

    [Serializable]
    public sealed class CustomProjectNode
    {
        public string Id;
        public string Label;
        public ProjectNodeKind Kind;
        public ProjectNodeSource Source;
        public string AssetGuid;
        public string AssetPath;
        public bool IsExpanded = true;
        public List<CustomProjectNode> Children = new List<CustomProjectNode>();

        public bool IsContainer => Kind != ProjectNodeKind.Asset;
        public bool IsManualGroup => Kind == ProjectNodeKind.Group && Source == ProjectNodeSource.Manual;
        public bool IsFolderRefRoot => Kind == ProjectNodeKind.Folder && Source == ProjectNodeSource.FolderRefRoot;
        public bool IsSynced => Source == ProjectNodeSource.FolderRefSynced;
        public bool CanAddChildren => IsManualGroup;
        public bool CanRenameInTree => IsManualGroup;
        public bool CanRemoveFromList => Source != ProjectNodeSource.FolderRefSynced;
        public bool CanMoveInTree => Source != ProjectNodeSource.FolderRefSynced;
        public bool CanDeleteOnDisk => Kind == ProjectNodeKind.Asset && Source == ProjectNodeSource.FolderRefSynced;
        public bool CanOpenAsset => Kind == ProjectNodeKind.Asset;
        public bool CanCopyPath => !string.IsNullOrEmpty(ResolveAssetPath());
        public bool CanRevealInFinder => !string.IsNullOrEmpty(ResolveAssetPath());

        public string ResolveAssetPath()
        {
            if (!string.IsNullOrEmpty(AssetPath))
                return AssetPath.Replace("\\", "/");

            if (!string.IsNullOrEmpty(AssetGuid))
                return AssetDatabase.GUIDToAssetPath(AssetGuid);

            return null;
        }

        public static CustomProjectNode CreateManualGroup(string label)
        {
            return new CustomProjectNode
            {
                Id = $"manual-group:{Guid.NewGuid():N}",
                Label = string.IsNullOrWhiteSpace(label) ? "New Group" : label.Trim(),
                Kind = ProjectNodeKind.Group,
                Source = ProjectNodeSource.Manual,
                IsExpanded = true,
            };
        }

        public static CustomProjectNode CreateManualAssetRef(string guid, string label)
        {
            return new CustomProjectNode
            {
                Id = $"manual-asset:{Guid.NewGuid():N}",
                Label = label,
                Kind = ProjectNodeKind.Asset,
                Source = ProjectNodeSource.Manual,
                AssetGuid = guid,
                IsExpanded = false,
            };
        }

        public static CustomProjectNode CreateFolderRefRoot(string assetPath)
        {
            var normalized = NormalizeAssetPath(assetPath);
            return new CustomProjectNode
            {
                Id = $"folderref-root:{normalized}",
                Label = Path.GetFileName(normalized.TrimEnd('/', '\\')),
                Kind = ProjectNodeKind.Folder,
                Source = ProjectNodeSource.FolderRefRoot,
                AssetGuid = AssetDatabase.AssetPathToGUID(normalized),
                AssetPath = normalized,
                IsExpanded = false,
            };
        }

        public static CustomProjectNode CreateSyncedFolder(string assetPath)
        {
            var normalized = NormalizeAssetPath(assetPath);
            return new CustomProjectNode
            {
                Id = $"folderref-sync-folder:{normalized}",
                Label = Path.GetFileName(normalized.TrimEnd('/', '\\')),
                Kind = ProjectNodeKind.Folder,
                Source = ProjectNodeSource.FolderRefSynced,
                AssetPath = normalized,
                IsExpanded = false,
            };
        }

        public static CustomProjectNode CreateSyncedAsset(string assetPath, string guid)
        {
            var normalized = NormalizeAssetPath(assetPath);
            return new CustomProjectNode
            {
                Id = $"folderref-sync-asset:{normalized}",
                Label = Path.GetFileName(normalized),
                Kind = ProjectNodeKind.Asset,
                Source = ProjectNodeSource.FolderRefSynced,
                AssetGuid = guid,
                AssetPath = normalized,
                IsExpanded = false,
            };
        }

        public static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace("\\", "/").TrimEnd('/');
        }
    }

    [Serializable]
    internal sealed class SerializableModel
    {
        public int Version = 2;
        public List<CustomProjectNode> Roots = new List<CustomProjectNode>();
    }

    internal static class CustomProjectViewIcons
    {
        private const string MissingAssetIconName = "console.erroricon.sml";
        private const string FolderFavoriteIconName = "FolderFavorite Icon";
        private const string FolderClosedIconName = "Folder Icon";
        private const string FolderOpenedIconName = "FolderOpened Icon";
        private const string AddGroupIconName = "CreateAddNew";
        private const string ToolbarPlusIconName = "Toolbar Plus";
        private const string ExpandIconName = "CollabCreate Icon";
        private const string CollapseIconName = "CollabDeleted Icon";
        private const string CollapseLegacyIconName = "CollabDeleted Icon";
        private const string SyncIconName = "d_Linked";
        private const string SyncLegacyIconName = "Linked";
        private const string TrashIconName = "TreeEditor.Trash";
        private const string ProjectIconName = "Project";

        public static Texture2D MissingAsset => GetTexture(MissingAssetIconName);
        public static Texture2D FolderRefRoot => GetTexture(FolderFavoriteIconName, FolderClosedIconName);
        public static Texture2D FolderClosed => GetTexture(FolderClosedIconName);
        public static Texture2D FolderOpened => GetTexture(FolderOpenedIconName, FolderClosedIconName);
        public static Texture2D AddGroup => GetTexture(AddGroupIconName, ToolbarPlusIconName);
        public static Texture2D Expand => GetTexture(ExpandIconName);
        public static Texture2D Collapse => GetTexture(CollapseIconName, CollapseLegacyIconName);
        public static Texture2D Sync => GetTexture(SyncIconName, SyncLegacyIconName);
        public static Texture2D Remove => GetTexture(TrashIconName);
        public static Texture2D Project => GetTexture(ProjectIconName);

        private static Texture2D GetTexture(params string[] iconNameList)
        {
            foreach (var iconName in iconNameList)
            {
                if (string.IsNullOrEmpty(iconName))
                    continue;

                var texture = EditorGUIUtility.FindTexture(iconName) as Texture2D;
                if (texture != null)
                    return texture;

                var contentImage = EditorGUIUtility.IconContent(iconName).image as Texture2D;
                if (contentImage != null)
                    return contentImage;
            }

            return null;
        }
    }

    internal sealed class CustomProjectTreeModel
    {
        private const string PrefKeyPrefix = "CustomProjectView_";
        private SerializableModel _model = new SerializableModel();

        public List<CustomProjectNode> Roots => _model.Roots;
        public bool IsEmpty => _model.Roots.Count == 0;

        private string PrefKey => PrefKeyPrefix + Application.dataPath.GetHashCode();

        public void Load()
        {
            _model = new SerializableModel();

            var json = EditorPrefs.GetString(PrefKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    _model = JsonUtility.FromJson<SerializableModel>(json) ?? new SerializableModel();
                }
                catch
                {
                    _model = new SerializableModel();
                }
            }

            if (_model.Roots == null)
                _model.Roots = new List<CustomProjectNode>();

            SanitizeTree(_model.Roots);
            SyncAllFolderRefs();
        }

        public void Save()
        {
            SortNodes(_model.Roots);
            var snapshot = new SerializableModel
            {
                Version = 2,
                Roots = ClonePersistentNodes(_model.Roots),
            };

            var json = JsonUtility.ToJson(snapshot, false);
            EditorPrefs.SetString(PrefKey, json);
        }

        public CustomProjectNode AddGroup(string label, CustomProjectNode parent = null)
        {
            if (!CanAcceptChild(parent))
                return null;

            var node = CustomProjectNode.CreateManualGroup(label);
            AppendToParent(node, parent);
            Save();
            return node;
        }

        public CustomProjectNode AddAssetRef(string guid, CustomProjectNode parent = null)
        {
            if (!CanAcceptChild(parent))
                return null;

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                return null;

            if (HasManualAssetRef(guid))
                return null;

            var node = CustomProjectNode.CreateManualAssetRef(guid, Path.GetFileName(assetPath));
            AppendToParent(node, parent);
            Save();
            return node;
        }

        public CustomProjectNode AddFolderRef(string assetPath, CustomProjectNode parent = null)
        {
            if (!CanAcceptChild(parent))
                return null;

            var normalized = CustomProjectNode.NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(normalized) || !AssetDatabase.IsValidFolder(normalized))
                return null;

            if (HasFolderRef(normalized))
                return null;

            var node = CustomProjectNode.CreateFolderRefRoot(normalized);
            AppendToParent(node, parent);
            SyncFolderRef(node);
            Save();
            return node;
        }

        public void RenameManualGroup(CustomProjectNode node, string newLabel)
        {
            if (node == null || !node.CanRenameInTree)
                return;

            node.Label = string.IsNullOrWhiteSpace(newLabel) ? node.Label : newLabel.Trim();
            Save();
        }

        public bool Remove(CustomProjectNode node)
        {
            if (node == null || !node.CanRemoveFromList)
                return false;

            var removed = RemoveRecursive(_model.Roots, node.Id);
            if (removed)
                Save();
            return removed;
        }

        public bool MoveNode(CustomProjectNode source, CustomProjectNode targetParent)
        {
            if (source == null || !source.CanMoveInTree)
                return false;

            if (!CanAcceptChild(targetParent))
                return false;

            if (targetParent != null && IsDescendant(source, targetParent))
                return false;

            if (!RemoveRecursive(_model.Roots, source.Id))
                return false;

            AppendToParent(source, targetParent);
            Save();
            return true;
        }

        public void ConvertFolderRefToSnapshotGroup(CustomProjectNode folderRefRoot)
        {
            if (folderRefRoot == null || !folderRefRoot.IsFolderRefRoot)
                return;

            var replacement = SnapshotAsManualGroup(folderRefRoot);
            if (replacement == null)
                return;

            ReplaceNode(folderRefRoot.Id, replacement, _model.Roots);
            Save();
        }

        public void SyncAllFolderRefs()
        {
            SyncFolderRefsRecursive(_model.Roots);
        }

        public void HandleAssetMoved(string oldPath, string newPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(newPath);
            if (string.IsNullOrEmpty(guid))
                return;

            HandleAssetMovedRecursive(_model.Roots, guid, newPath);
            SyncFolderRefsForPaths(new[] { oldPath, newPath });
            Save();
        }

        public void HandleAssetDeleted(string deletedPath)
        {
            var removedAny = RemoveMissingManualAssetRefs(_model.Roots);
            var syncedAny = SyncFolderRefsForPaths(new[] { deletedPath });
            if (removedAny || syncedAny)
                Save();
        }

        public bool HandleAssetsImported(IEnumerable<string> importedPaths)
        {
            if (!SyncFolderRefsForPaths(importedPaths))
                return false;

            Save();
            return true;
        }

        public void SetExpanded(CustomProjectNode node, bool expanded)
        {
            if (node == null || !node.IsContainer)
                return;

            node.IsExpanded = expanded;
        }

        public CustomProjectNode FindNodeById(string id)
        {
            return FindNodeByIdRecursive(id, _model.Roots);
        }

        public CustomProjectNode FindManualAssetRefByGuid(string guid)
        {
            return FindManualAssetRefByGuidRecursive(guid, _model.Roots);
        }

        public CustomProjectNode FindNodeByAssetPath(string assetPath)
        {
            var normalized = CustomProjectNode.NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(normalized))
                return null;

            return FindNodeByAssetPathRecursive(normalized, _model.Roots);
        }

        public List<CustomProjectNode> Search(string query)
        {
            var result = new List<CustomProjectNode>();
            SearchRecursive(_model.Roots, query ?? string.Empty, result);
            return result;
        }

        private void SanitizeTree(List<CustomProjectNode> nodes)
        {
            if (nodes == null)
                return;

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var node = nodes[i];
                if (node == null)
                {
                    nodes.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    node.Id = $"manual-legacy:{Guid.NewGuid():N}";
                }

                if (string.IsNullOrWhiteSpace(node.Label))
                {
                    node.Label = node.Kind == ProjectNodeKind.Asset ? "Missing Asset" : "Group";
                }

                if (node.Children == null)
                    node.Children = new List<CustomProjectNode>();

                if (node.Source == ProjectNodeSource.FolderRefSynced)
                {
                    nodes.RemoveAt(i);
                    continue;
                }

                if (node.IsFolderRefRoot)
                {
                    node.AssetPath = CustomProjectNode.NormalizeAssetPath(node.ResolveAssetPath());
                    node.AssetGuid = string.IsNullOrEmpty(node.AssetGuid) && !string.IsNullOrEmpty(node.AssetPath)
                        ? AssetDatabase.AssetPathToGUID(node.AssetPath)
                        : node.AssetGuid;
                    node.Children.Clear();
                }
                else
                {
                    SanitizeTree(node.Children);
                }
            }
        }

        private List<CustomProjectNode> ClonePersistentNodes(List<CustomProjectNode> nodes)
        {
            var result = new List<CustomProjectNode>();
            if (nodes == null)
                return result;

            foreach (var node in nodes)
            {
                var clone = ClonePersistentNode(node);
                if (clone != null)
                    result.Add(clone);
            }

            return result;
        }

        private CustomProjectNode ClonePersistentNode(CustomProjectNode node)
        {
            if (node == null || node.Source == ProjectNodeSource.FolderRefSynced)
                return null;

            var clone = new CustomProjectNode
            {
                Id = node.Id,
                Label = node.Label,
                Kind = node.Kind,
                Source = node.Source,
                AssetGuid = node.AssetGuid,
                AssetPath = node.AssetPath,
                IsExpanded = node.IsExpanded,
                Children = new List<CustomProjectNode>(),
            };

            if (node.IsManualGroup)
            {
                clone.Children = ClonePersistentNodes(node.Children);
            }

            return clone;
        }

        private void AppendToParent(CustomProjectNode node, CustomProjectNode parent)
        {
            if (parent == null)
            {
                _model.Roots.Add(node);
                return;
            }

            if (parent.Children == null)
                parent.Children = new List<CustomProjectNode>();

            parent.Children.Add(node);
            parent.IsExpanded = true;
        }

        private bool CanAcceptChild(CustomProjectNode parent)
        {
            return parent == null || parent.CanAddChildren;
        }

        private bool HasManualAssetRef(string guid)
        {
            return FindManualAssetRefByGuid(guid) != null;
        }

        private bool HasFolderRef(string assetPath)
        {
            var normalized = CustomProjectNode.NormalizeAssetPath(assetPath);
            return EnumerateNodes(_model.Roots).Any(n => n.IsFolderRefRoot && CustomProjectNode.NormalizeAssetPath(n.ResolveAssetPath()) == normalized);
        }

        private bool RemoveRecursive(List<CustomProjectNode> nodes, string id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == id)
                {
                    nodes.RemoveAt(i);
                    return true;
                }

                if (RemoveRecursive(nodes[i].Children, id))
                    return true;
            }

            return false;
        }

        private bool ReplaceNode(string oldId, CustomProjectNode replacement, List<CustomProjectNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == oldId)
                {
                    nodes[i] = replacement;
                    return true;
                }

                if (ReplaceNode(oldId, replacement, nodes[i].Children))
                    return true;
            }

            return false;
        }

        private bool IsDescendant(CustomProjectNode ancestor, CustomProjectNode candidate)
        {
            if (ancestor == null || candidate == null)
                return false;

            return ancestor.Children.Any(child => child.Id == candidate.Id || IsDescendant(child, candidate));
        }

        private void SyncFolderRefsRecursive(List<CustomProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsFolderRefRoot)
                    SyncFolderRef(node);

                if (node.Children != null && node.Children.Count > 0)
                    SyncFolderRefsRecursive(node.Children);
            }
        }

        private bool SyncFolderRefsForPaths(IEnumerable<string> assetPaths)
        {
            if (assetPaths == null)
                return false;

            var normalizedPathList = assetPaths
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(CustomProjectNode.NormalizeAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedPathList.Count == 0)
                return false;

            var folderRefRootList = EnumerateNodes(_model.Roots)
                .Where(node => node.IsFolderRefRoot)
                .ToList();

            bool syncedAny = false;

            foreach (var folderRefRoot in folderRefRootList)
            {
                var folderPath = CustomProjectNode.NormalizeAssetPath(folderRefRoot.ResolveAssetPath());
                if (string.IsNullOrEmpty(folderPath))
                    folderPath = CustomProjectNode.NormalizeAssetPath(folderRefRoot.AssetPath);

                if (string.IsNullOrEmpty(folderPath))
                    continue;

                if (!normalizedPathList.Any(path => IsSameOrChildPath(path, folderPath)))
                    continue;

                SyncFolderRef(folderRefRoot);
                syncedAny = true;
            }

            return syncedAny;
        }

        public void SyncFolderRef(CustomProjectNode folderRefRoot)
        {
            if (folderRefRoot == null || !folderRefRoot.IsFolderRefRoot)
                return;

            var folderPath = folderRefRoot.ResolveAssetPath();
            folderPath = CustomProjectNode.NormalizeAssetPath(folderPath);
            folderRefRoot.AssetPath = folderPath;

            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                folderRefRoot.Children = new List<CustomProjectNode>();
                return;
            }

            folderRefRoot.AssetGuid = AssetDatabase.AssetPathToGUID(folderPath);
            folderRefRoot.Label = Path.GetFileName(folderPath.TrimEnd('/', '\\'));
            folderRefRoot.Children = BuildSyncedChildren(folderPath);
        }

        private List<CustomProjectNode> BuildSyncedChildren(string folderPath)
        {
            var absPath = ToAbsolutePath(folderPath);
            var result = new List<CustomProjectNode>();

            if (!Directory.Exists(absPath))
                return result;

            foreach (var dir in Directory.GetDirectories(absPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(dir);
                if (ShouldExclude(name))
                    continue;

                var childPath = CustomProjectNode.NormalizeAssetPath(Path.Combine(folderPath, name));
                var folderNode = CustomProjectNode.CreateSyncedFolder(childPath);
                folderNode.Children = BuildSyncedChildren(childPath);
                result.Add(folderNode);
            }

            foreach (var file in Directory.GetFiles(absPath).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(file);
                if (ShouldExclude(name))
                    continue;

                var childPath = CustomProjectNode.NormalizeAssetPath(Path.Combine(folderPath, name));
                var guid = AssetDatabase.AssetPathToGUID(childPath);
                if (string.IsNullOrEmpty(guid))
                    continue;

                result.Add(CustomProjectNode.CreateSyncedAsset(childPath, guid));
            }

            return result;
        }

        private void HandleAssetMovedRecursive(List<CustomProjectNode> nodes, string guid, string newPath)
        {
            foreach (var node in nodes)
            {
                if (node.Source == ProjectNodeSource.Manual && node.Kind == ProjectNodeKind.Asset && node.AssetGuid == guid)
                {
                    node.Label = Path.GetFileName(newPath);
                }
                else if (node.IsFolderRefRoot && node.AssetGuid == guid)
                {
                    node.AssetPath = CustomProjectNode.NormalizeAssetPath(newPath);
                    node.Label = Path.GetFileName(newPath.TrimEnd('/', '\\'));
                }

                if (node.Children != null && node.Children.Count > 0)
                    HandleAssetMovedRecursive(node.Children, guid, newPath);
            }
        }

        private bool RemoveMissingManualAssetRefs(List<CustomProjectNode> nodes)
        {
            bool removedAny = false;

            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var node = nodes[i];
                if (node.Source == ProjectNodeSource.Manual && node.Kind == ProjectNodeKind.Asset)
                {
                    var path = node.ResolveAssetPath();
                    if (string.IsNullOrEmpty(path))
                    {
                        nodes.RemoveAt(i);
                        removedAny = true;
                        continue;
                    }
                }

                removedAny |= RemoveMissingManualAssetRefs(node.Children);
            }

            return removedAny;
        }

        private CustomProjectNode SnapshotAsManualGroup(CustomProjectNode folderRefRoot)
        {
            var group = CustomProjectNode.CreateManualGroup(folderRefRoot.Label);
            group.IsExpanded = folderRefRoot.IsExpanded;

            foreach (var child in folderRefRoot.Children)
            {
                var snapshot = SnapshotChildRecursive(child);
                if (snapshot != null)
                    group.Children.Add(snapshot);
            }

            return group;
        }

        private CustomProjectNode SnapshotChildRecursive(CustomProjectNode node)
        {
            if (node == null)
                return null;

            if (node.Kind == ProjectNodeKind.Asset)
            {
                var path = node.ResolveAssetPath();
                var guid = !string.IsNullOrEmpty(node.AssetGuid)
                    ? node.AssetGuid
                    : AssetDatabase.AssetPathToGUID(path);

                if (string.IsNullOrEmpty(guid))
                    return null;

                return CustomProjectNode.CreateManualAssetRef(guid, node.Label);
            }

            var group = CustomProjectNode.CreateManualGroup(node.Label);
            group.IsExpanded = node.IsExpanded;
            foreach (var child in node.Children)
            {
                var snapshot = SnapshotChildRecursive(child);
                if (snapshot != null)
                    group.Children.Add(snapshot);
            }

            return group;
        }

        private CustomProjectNode FindNodeByIdRecursive(string id, List<CustomProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id)
                    return node;

                var found = FindNodeByIdRecursive(id, node.Children);
                if (found != null)
                    return found;
            }

            return null;
        }

        private CustomProjectNode FindManualAssetRefByGuidRecursive(string guid, List<CustomProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Source == ProjectNodeSource.Manual && node.Kind == ProjectNodeKind.Asset && node.AssetGuid == guid)
                    return node;

                var found = FindManualAssetRefByGuidRecursive(guid, node.Children);
                if (found != null)
                    return found;
            }

            return null;
        }

        private CustomProjectNode FindNodeByAssetPathRecursive(string assetPath, List<CustomProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                var resolved = CustomProjectNode.NormalizeAssetPath(node.ResolveAssetPath());
                if (!string.IsNullOrEmpty(resolved) && resolved == assetPath)
                    return node;

                var found = FindNodeByAssetPathRecursive(assetPath, node.Children);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void SearchRecursive(List<CustomProjectNode> nodes, string query, List<CustomProjectNode> result)
        {
            foreach (var node in nodes)
            {
                if (node.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(node);

                SearchRecursive(node.Children, query, result);
            }
        }

        private IEnumerable<CustomProjectNode> EnumerateNodes(List<CustomProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var child in EnumerateNodes(node.Children))
                    yield return child;
            }
        }

        private void SortNodes(List<CustomProjectNode> nodes)
        {
            nodes.Sort((a, b) =>
            {
                var pa = GetSortPriority(a);
                var pb = GetSortPriority(b);
                if (pa != pb)
                    return pa.CompareTo(pb);

                return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
            });

            foreach (var node in nodes)
            {
                if (node.Source != ProjectNodeSource.FolderRefSynced && node.Children != null && node.Children.Count > 0)
                    SortNodes(node.Children);
            }
        }

        private int GetSortPriority(CustomProjectNode node)
        {
            if (node.IsManualGroup)
                return 0;
            if (node.IsFolderRefRoot)
                return 1;
            if (node.Kind == ProjectNodeKind.Folder)
                return 2;
            return 3;
        }

        private static bool ShouldExclude(string name)
        {
            if (string.IsNullOrEmpty(name))
                return true;
            if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                return true;
            if (name.StartsWith(".", StringComparison.Ordinal))
                return true;
            if (name.EndsWith("~", StringComparison.Ordinal))
                return true;
            if (name == "Temp" || name == "Library" || name == "obj")
                return true;
            return false;
        }

        private static bool IsSameOrChildPath(string path, string rootPath)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(rootPath))
                return false;

            if (string.Equals(path, rootPath, StringComparison.OrdinalIgnoreCase))
                return true;

            return path.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }

    internal sealed class CustomProjectAssetPostprocessor : AssetPostprocessor
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

            if (window == null || window.Model == null)
                return;

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

            if (importedAssets.Length > 0)
            {
                changed |= window.Model.HandleAssetsImported(importedAssets);
            }

            if (changed)
                window.RequestRefresh();
        }
    }

    internal sealed class CustomProjectViewItem : TreeViewItem<int>
    {
        public CustomProjectNode Node;
        public string AssetPath;
        public bool IsMissing;

        public CustomProjectViewItem(int id, int depth, CustomProjectNode node)
            : base(id, depth, node?.Label ?? string.Empty)
        {
            Node = node;
            AssetPath = node?.ResolveAssetPath();
            IsMissing = false;
            icon = ResolveIcon(node, AssetPath, ref IsMissing);

            if (IsMissing && node != null)
                displayName = node.Label + " (Missing)";
        }

        private static Texture2D ResolveIcon(CustomProjectNode node, string assetPath, ref bool isMissing)
        {
            if (node == null)
                return null;

            if (node.Kind == ProjectNodeKind.Asset)
            {
                if (string.IsNullOrEmpty(assetPath))
                {
                    isMissing = true;
                    return CustomProjectViewIcons.MissingAsset;
                }

                return AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
            }

            if (node.IsFolderRefRoot)
            {
                isMissing = string.IsNullOrEmpty(assetPath) || !AssetDatabase.IsValidFolder(assetPath);
                return CustomProjectViewIcons.FolderRefRoot;
            }

            if (node.Kind == ProjectNodeKind.Folder)
            {
                isMissing = string.IsNullOrEmpty(assetPath) || !AssetDatabase.IsValidFolder(assetPath);
                return node.IsExpanded
                    ? CustomProjectViewIcons.FolderOpened
                    : CustomProjectViewIcons.FolderClosed;
            }

            return node.IsExpanded
                ? CustomProjectViewIcons.FolderOpened
                : CustomProjectViewIcons.FolderClosed;
        }
    }

    internal sealed class CustomProjectTreeView : TreeView<int>
    {
        private readonly CustomProjectTreeModel _model;
        private readonly CustomProjectViewWindow _window;

        private readonly Dictionary<int, CustomProjectNode> _idToNode = new Dictionary<int, CustomProjectNode>();
        private readonly Dictionary<int, Rect> _idToFoldoutRect = new Dictionary<int, Rect>();
        private string _searchQuery = string.Empty;
        private int _nextId = 1;
        private int _selectionSyncFrame = -1;
        private int _contextRenameItemId = -1;
        private int _pendingToggleItemId = -1;
        private Vector2 _pendingToggleMouseDownPosition;
        private float _lastVisibleRowBottom;
        private bool _restoringExpandedState;

        private const float ButtonW = 18f;
        private const float ButtonSpacing = 1f;
        private const float ToggleDragThreshold = 4f;
        private const float IconWidth = 16f;
        private const float IconTextSpacing = 2f;

        public CustomProjectTreeView(TreeViewState<int> state, CustomProjectTreeModel model, CustomProjectViewWindow window)
            : base(state)
        {
            _model = model;
            _window = window;
            showBorder = true;
            showAlternatingRowBackgrounds = false;
            rowHeight = EditorGUIUtility.singleLineHeight + 2f;
        }

        public void SetSearch(string query)
        {
            var previousIds = GetSelection()
                .Select(GetNodeForId)
                .Where(n => n != null)
                .Select(n => n.Id)
                .ToList();

            _searchQuery = query ?? string.Empty;
            Reload();

            if (previousIds.Count == 0)
                return;

            var restored = previousIds
                .Select(id => _model.FindNodeById(id))
                .Where(n => n != null)
                .Select(GetIdForNode)
                .Where(id => id >= 0)
                .ToList();

            if (restored.Count > 0)
                SetSelection(restored, TreeViewSelectionOptions.RevealAndFrame);
        }

        public void ClearSelectionAndPing()
        {
            SetSelection(new List<int>(), TreeViewSelectionOptions.FireSelectionChanged);
        }

        public bool HasSelection()
        {
            return GetSelection().Count > 0;
        }

        public void BeginFrame(Rect treeRect)
        {
            _lastVisibleRowBottom = treeRect.yMin;
        }

        public bool TryClearSelectionFromEmptySpace(Rect treeRect)
        {
            var evt = Event.current;
            if (evt == null || evt.type != EventType.MouseDown || evt.button != 0)
                return false;

            if (!treeRect.Contains(evt.mousePosition))
                return false;

            if (evt.mousePosition.y <= _lastVisibleRowBottom)
                return false;

            if (GetSelection().Count == 0)
                return false;

            ClearSelectionAndPing();
            evt.Use();
            return true;
        }

        protected override TreeViewItem<int> BuildRoot()
        {
            _idToNode.Clear();
            _idToFoldoutRect.Clear();
            _nextId = 1;

            var root = new TreeViewItem<int>(-1, -1, "root");

            if (string.IsNullOrEmpty(_searchQuery))
            {
                BuildTree(_model.Roots, root);
            }
            else
            {
                foreach (var node in _model.Search(_searchQuery))
                    root.AddChild(CreateItem(node, 0));
            }

            if (!root.hasChildren)
                root.AddChild(new TreeViewItem<int>(0, 0, string.Empty));

            SetupDepthsFromParentsAndChildren(root);

            if (string.IsNullOrEmpty(_searchQuery))
                ApplyExpandedState(_model.Roots);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as CustomProjectViewItem;
            if (item == null || item.Node == null)
            {
                if (_model.IsEmpty && string.IsNullOrEmpty(_searchQuery))
                    GUI.Label(args.rowRect, "まだ項目がありません。\"+ Group\" でグループを追加してください。", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var oldColor = GUI.color;
            if (item.IsMissing)
                GUI.color = Color.red;

            _lastVisibleRowBottom = Mathf.Max(_lastVisibleRowBottom, args.rowRect.yMax);
            _idToFoldoutRect[item.id] = CreateFoldoutRect(args.rowRect, item);
            HandlePendingToggleInteraction(args, item);
            var isRenamingItem = _contextRenameItemId == item.id;
            if (isRenamingItem)
            {
                base.RowGUI(args);
            }
            else
            {
                DrawRowContent(args, item);
            }

            GUI.color = oldColor;

            if (Event.current.type == EventType.Repaint)
                DrawPathSuffix(args, item);

            if (args.selected || args.rowRect.Contains(Event.current.mousePosition))
            {
                var width = CalcButtonAreaWidth(item.Node);
                if (width > 0f)
                {
                    var rect = new Rect(args.rowRect.xMax - width, args.rowRect.y, width, args.rowRect.height);
                    DrawInlineButtons(rect, item.Node, item.id);
                }
            }
        }

        protected override void ContextClickedItem(int id)
        {
            var selectedIds = GetSelection();
            if (selectedIds.Count > 1)
            {
                var nodes = selectedIds.Select(GetNodeForId).Where(n => n != null && n.CanRemoveFromList).ToList();
                if (nodes.Count > 0)
                    ShowMultiSelectionContextMenu(nodes);
                return;
            }

            var node = GetNodeForId(id);
            if (node != null)
                ShowContextMenu(node, id);
        }

        protected override void SingleClickedItem(int id)
        {
        }

        protected override bool CanRename(TreeViewItem<int> item)
        {
            var node = GetNodeForId(item.id);
            return node != null && node.CanRenameInTree && item.id == _contextRenameItemId;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            _contextRenameItemId = -1;

            if (!args.acceptedRename)
                return;

            var node = GetNodeForId(args.itemID);
            if (node == null || !node.CanRenameInTree)
                return;

            _model.RenameManualGroup(node, args.newName);
            Reload();
        }

        protected override void DoubleClickedItem(int id)
        {
            _pendingToggleItemId = -1;

            var node = GetNodeForId(id);
            if (node == null)
                return;

            if (node.CanOpenAsset)
                OpenAsset(node);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (!_window.AutoSyncSelection || selectedIds == null || selectedIds.Count == 0)
                return;

            var node = GetNodeForId(selectedIds[0]);
            var assetPath = node?.ResolveAssetPath();
            if (string.IsNullOrEmpty(assetPath))
                return;

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj == null)
                return;

            _selectionSyncFrame = Time.frameCount;
            Selection.activeObject = obj;
        }

        protected override void ExpandedStateChanged()
        {
            if (_restoringExpandedState)
                return;

            SyncExpandedState(_model.Roots);
            _model.Save();
        }

        protected override bool CanMultiSelect(TreeViewItem<int> item) => true;

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return args.draggedItemIDs
                .Select(GetNodeForId)
                .Where(n => n != null)
                .All(n => n.CanMoveInTree);
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var nodes = args.draggedItemIDs
                .Select(GetNodeForId)
                .Where(n => n != null)
                .ToList();

            DragAndDrop.SetGenericData("CustomProjectViewNodes", nodes);

            var objects = nodes
                .Select(n => n.ResolveAssetPath())
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p))
                .Where(o => o != null)
                .ToArray();

            if (objects.Length > 0)
                DragAndDrop.objectReferences = objects;

            DragAndDrop.StartDrag("CustomProjectView");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var targetParent = (args.parentItem as CustomProjectViewItem)?.Node;
            if (targetParent != null && !targetParent.CanAddChildren)
                return DragAndDropVisualMode.Rejected;

            var internalNodes = DragAndDrop.GetGenericData("CustomProjectViewNodes") as List<CustomProjectNode>;
            if (internalNodes != null)
            {
                if (args.performDrop)
                {
                    foreach (var source in internalNodes)
                        _model.MoveNode(source, targetParent);
                    Reload();
                }
                return DragAndDropVisualMode.Move;
            }

            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
                return DragAndDropVisualMode.None;

            if (args.performDrop)
            {
                foreach (var obj in DragAndDrop.objectReferences)
                    AddObjectReference(obj, targetParent);
                Reload();
            }

            return DragAndDropVisualMode.Copy;
        }

        protected override void KeyEvent()
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            if (Event.current.keyCode != KeyCode.Delete && Event.current.keyCode != KeyCode.Backspace)
                return;

            var nodes = GetSelection()
                .Select(GetNodeForId)
                .Where(n => n != null && n.CanRemoveFromList)
                .ToList();

            if (nodes.Count == 0)
                return;

            Event.current.Use();

            if (nodes.Count == 1)
                RemoveWithConfirm(nodes[0]);
            else
                RemoveMultipleWithConfirm(nodes);
        }

        public void ExpandAll(bool expand)
        {
            if (rootItem?.children == null)
                return;

            foreach (var child in rootItem.children)
                SetExpandedRecursive(child, expand);

            _model.Save();
            Reload();
        }

        public void SyncSelectionFromUnity()
        {
            if (Mathf.Abs(Time.frameCount - _selectionSyncFrame) <= 1)
                return;

            var obj = Selection.activeObject;
            if (obj == null)
                return;

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath))
                return;

            var best = _model.FindNodeByAssetPath(assetPath);
            if (best == null)
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                best = _model.FindManualAssetRefByGuid(guid);
            }

            if (best == null)
                return;

            var id = GetIdForNode(best);
            if (id >= 0)
                SetSelection(new[] { id }, TreeViewSelectionOptions.RevealAndFrame);
        }

        private void BuildTree(List<CustomProjectNode> nodes, TreeViewItem<int> parent)
        {
            foreach (var node in nodes)
            {
                var item = CreateItem(node, parent.depth + 1);
                parent.AddChild(item);
                if (node.IsContainer && node.Children != null && node.Children.Count > 0)
                    BuildTree(node.Children, item);
            }
        }

        private CustomProjectViewItem CreateItem(CustomProjectNode node, int depth)
        {
            var id = _nextId++;
            _idToNode[id] = node;
            return new CustomProjectViewItem(id, depth, node);
        }

        private void ApplyExpandedState(List<CustomProjectNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            _restoringExpandedState = true;
            try
            {
                ApplyExpandedStateRecursive(nodes);
            }
            finally
            {
                _restoringExpandedState = false;
            }
        }

        private void ApplyExpandedStateRecursive(List<CustomProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsContainer)
                {
                    var id = GetIdForNode(node);
                    if (id >= 0 && node.IsExpanded)
                        SetExpanded(id, true);
                }

                if (node.Children != null && node.Children.Count > 0)
                    ApplyExpandedStateRecursive(node.Children);
            }
        }

        private int GetIdForNode(CustomProjectNode node)
        {
            foreach (var kv in _idToNode)
            {
                if (kv.Value.Id == node.Id)
                    return kv.Key;
            }
            return -1;
        }

        private CustomProjectNode GetNodeForId(int id)
        {
            _idToNode.TryGetValue(id, out var node);
            return node;
        }

        private void HandlePendingToggleInteraction(RowGUIArgs args, CustomProjectViewItem item)
        {
            var evt = Event.current;
            if (evt == null || evt.button != 0)
                return;

            if (evt.type == EventType.MouseDown)
            {
                if (!args.rowRect.Contains(evt.mousePosition))
                    return;

                if (TryToggleOnFastRepeatClick(args, item))
                    return;

                if (!CanPreparePendingToggle(args, item))
                {
                    _pendingToggleItemId = -1;
                    return;
                }

                _pendingToggleItemId = item.id;
                _pendingToggleMouseDownPosition = evt.mousePosition;
                return;
            }

            if (_pendingToggleItemId != item.id)
                return;

            if (evt.type == EventType.MouseDrag)
            {
                if ((evt.mousePosition - _pendingToggleMouseDownPosition).sqrMagnitude > ToggleDragThreshold * ToggleDragThreshold)
                    _pendingToggleItemId = -1;
                return;
            }

            if (evt.type != EventType.MouseUp)
                return;

            _pendingToggleItemId = -1;

            if (!CanCommitPendingToggle(args, item))
                return;

            ToggleExpandedState(item.id, item.Node);
        }

        private bool TryToggleOnFastRepeatClick(RowGUIArgs args, CustomProjectViewItem item)
        {
            var evt = Event.current;
            if (evt == null || evt.clickCount < 2)
                return false;

            if (!CanPreparePendingToggle(args, item))
                return false;

            _pendingToggleItemId = -1;
            ToggleExpandedState(item.id, item.Node);
            return true;
        }

        private bool CanPreparePendingToggle(RowGUIArgs args, CustomProjectViewItem item)
        {
            if (item.Node == null || !item.Node.IsContainer)
                return false;

            if (!string.IsNullOrEmpty(_searchQuery))
                return false;

            var evt = Event.current;
            if (evt == null)
                return false;

            if (evt.shift || evt.control || evt.command)
                return false;

            if (!args.selected)
                return false;

            var selectedIds = GetSelection();
            if (selectedIds.Count != 1 || selectedIds[0] != item.id)
                return false;

            if (IsPointerOnFoldout(item.id) || IsPointerOnInlineButtons(args.rowRect, item.Node))
                return false;

            return true;
        }

        private bool CanCommitPendingToggle(RowGUIArgs args, CustomProjectViewItem item)
        {
            if (item.Node == null || !item.Node.IsContainer)
                return false;

            if (!args.rowRect.Contains(Event.current.mousePosition))
                return false;

            if (IsPointerOnFoldout(item.id) || IsPointerOnInlineButtons(args.rowRect, item.Node))
                return false;

            return true;
        }

        private Rect CreateFoldoutRect(Rect rowRect, TreeViewItem<int> item)
        {
            var foldoutX = rowRect.x + GetFoldoutIndent(item);
            var contentX = rowRect.x + GetContentIndent(item);
            var foldoutWidth = Mathf.Max(14f, contentX - foldoutX);
            return new Rect(foldoutX, rowRect.y, foldoutWidth, rowRect.height);
        }

        private bool IsPointerOnFoldout(int itemId)
        {
            var evt = Event.current;
            if (evt == null)
                return false;

            if (!_idToFoldoutRect.TryGetValue(itemId, out var foldoutRect))
                return false;

            return foldoutRect.Contains(evt.mousePosition);
        }

        private bool IsPointerOnInlineButtons(Rect rowRect, CustomProjectNode node)
        {
            var evt = Event.current;
            if (evt == null)
                return false;

            var width = CalcButtonAreaWidth(node);
            if (width <= 0f)
                return false;

            var buttonRect = new Rect(rowRect.xMax - width, rowRect.y, width, rowRect.height);
            return buttonRect.Contains(evt.mousePosition);
        }

        private void ToggleExpandedState(int itemId, CustomProjectNode node)
        {
            SetExpandedState(itemId, node, !IsExpanded(itemId));
        }

        private void DrawPathSuffix(RowGUIArgs args, CustomProjectViewItem item)
        {
            var node = item.Node;
            if (node == null)
                return;

            string parentDir = null;
            var assetPath = item.AssetPath;
            if (!string.IsNullOrEmpty(assetPath) && (node.Source == ProjectNodeSource.Manual || node.IsFolderRefRoot))
            {
                var dir = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(dir))
                    parentDir = Path.GetFileName(dir.TrimEnd('/'));
            }

            if (string.IsNullOrEmpty(parentDir))
                return;

            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.MiddleLeft;
            var size = labelStyle.CalcSize(new GUIContent(item.displayName));
            var labelRect = GetCenteredLabelRect(args, item);
            var xOffset = labelRect.x - args.rowRect.x + size.x;
            var rect = new Rect(args.rowRect.x + xOffset, args.rowRect.y, args.rowRect.xMax - (args.rowRect.x + xOffset), args.rowRect.height);
            labelStyle.normal.textColor = args.selected ? new Color(0.8f, 0.8f, 0.8f, 0.8f) : Color.gray;
            GUI.Label(rect, $" (/{parentDir})", labelStyle);
        }

        private void DrawRowContent(RowGUIArgs args, CustomProjectViewItem item)
        {
            DrawFoldout(args, item);
            DrawIcon(args, item);
            DrawCenteredLabel(args, item);
        }

        private void DrawFoldout(RowGUIArgs args, CustomProjectViewItem item)
        {
            if (!item.Node.IsContainer)
                return;

            var foldoutRect = _idToFoldoutRect[item.id];
            var expanded = IsExpanded(item.id);
            var newExpanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, false);
            if (newExpanded != expanded)
                SetExpandedState(item.id, item.Node, newExpanded);
        }

        private void DrawIcon(RowGUIArgs args, CustomProjectViewItem item)
        {
            if (item.icon == null)
                return;

            var iconRect = GetIconRect(args, item);
            GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit, true);
        }

        private void DrawCenteredLabel(RowGUIArgs args, CustomProjectViewItem item)
        {
            var labelRect = GetCenteredLabelRect(args, item);
            if (labelRect.width <= 0f)
                return;

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };

            if (item.IsMissing)
            {
                labelStyle.normal.textColor = args.selected
                    ? new Color(1f, 0.85f, 0.85f)
                    : new Color(0.85f, 0.2f, 0.2f);
            }
            else if (args.selected)
            {
                labelStyle.normal.textColor = Color.white;
            }

            GUI.Label(labelRect, item.displayName, labelStyle);
        }

        private Rect GetCenteredLabelRect(RowGUIArgs args, CustomProjectViewItem item)
        {
            var x = args.rowRect.x + GetContentIndent(item) + IconWidth + IconTextSpacing;
            var width = args.rowRect.xMax - x - CalcButtonAreaWidth(item.Node) - 6f;
            if (width <= 0f)
                return Rect.zero;

            return new Rect(x, args.rowRect.y, width, args.rowRect.height);
        }

        private Rect GetIconRect(RowGUIArgs args, CustomProjectViewItem item)
        {
            var x = args.rowRect.x + GetContentIndent(item);
            var y = args.rowRect.y + (args.rowRect.height - IconWidth) * 0.5f;
            return new Rect(x, y, IconWidth, IconWidth);
        }

        private void SetExpandedState(int itemId, CustomProjectNode node, bool expanded)
        {
            _model.SetExpanded(node, expanded);

            _restoringExpandedState = true;
            try
            {
                SetExpanded(itemId, expanded);
            }
            finally
            {
                _restoringExpandedState = false;
            }

            _model.Save();
        }

        private float CalcButtonAreaWidth(CustomProjectNode node)
        {
            var count = 0;

            if (node.IsManualGroup)
                count = 4;
            else if (node.IsFolderRefRoot || (node.Kind == ProjectNodeKind.Folder && node.IsSynced))
                count = 2 + (node.CanRemoveFromList ? 1 : 0);
            else if (node.CanRemoveFromList)
                count = 1;

            if (count == 0)
                return 0f;

            return count * (ButtonW + ButtonSpacing) + 4f;
        }

        private void DrawInlineButtons(Rect rect, CustomProjectNode node, int itemId)
        {
            float x = rect.x + 2f;
            float y = rect.y + (rect.height - ButtonW) * 0.5f;
            var style = EditorStyles.iconButton;

            if (node.IsManualGroup)
            {
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(CustomProjectViewIcons.AddGroup, "サブグループを追加"), style))
                {
                    AddSubGroup(node);
                }
                x += ButtonW + ButtonSpacing;

                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(CustomProjectViewIcons.Expand, "再帰的に展開"), style))
                {
                    ExpandRecursive(itemId, true);
                }
                x += ButtonW + ButtonSpacing;

                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(CustomProjectViewIcons.Collapse, "再帰的に折りたたむ"), style))
                {
                    ExpandRecursive(itemId, false);
                }
                x += ButtonW + ButtonSpacing;
            }
            else if (node.IsFolderRefRoot || (node.Kind == ProjectNodeKind.Folder && node.IsSynced))
            {
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(CustomProjectViewIcons.Expand, "再帰的に展開"), style))
                {
                    ExpandRecursive(itemId, true);
                }
                x += ButtonW + ButtonSpacing;

                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(CustomProjectViewIcons.Collapse, "再帰的に折りたたむ"), style))
                {
                    ExpandRecursive(itemId, false);
                }
                x += ButtonW + ButtonSpacing;
            }

            if (node.CanRemoveFromList)
            {
                if (GUI.Button(new Rect(x, y, ButtonW, ButtonW),
                    new GUIContent(CustomProjectViewIcons.Remove, "取り除く"), style))
                {
                    RemoveWithConfirm(node);
                }
            }
        }

        private void ShowMultiSelectionContextMenu(List<CustomProjectNode> nodes)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent($"選択した {nodes.Count} 項目を取り除く"), false, () => RemoveMultipleWithConfirm(nodes));
            menu.ShowAsContext();
        }

        private void ShowContextMenu(CustomProjectNode node, int itemId)
        {
            var menu = new GenericMenu();

            if (node.IsManualGroup)
            {
                menu.AddItem(new GUIContent("サブグループを追加"), false, () => AddSubGroup(node));
                menu.AddItem(new GUIContent("項目を追加..."), false, () => AddAssetToGroup(node));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("グループ名を変更"), false, () => BeginContextRename(itemId));
                menu.AddItem(new GUIContent("取り除く"), false, () => RemoveWithConfirm(node));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("再帰的に展開"), false, () => ExpandRecursive(itemId, true));
                menu.AddItem(new GUIContent("再帰的に折りたたむ"), false, () => ExpandRecursive(itemId, false));
                menu.ShowAsContext();
                return;
            }

            if (node.IsFolderRefRoot)
            {
                AddPathActions(menu, node);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("グループに変換"), false, () =>
                {
                    _model.ConvertFolderRefToSnapshotGroup(node);
                    Reload();
                });
                menu.AddItem(new GUIContent("取り除く"), false, () => RemoveWithConfirm(node));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("再帰的に展開"), false, () => ExpandRecursive(itemId, true));
                menu.AddItem(new GUIContent("再帰的に折りたたむ"), false, () => ExpandRecursive(itemId, false));
                menu.ShowAsContext();
                return;
            }

            if (node.Kind == ProjectNodeKind.Folder && node.IsSynced)
            {
                AddPathActions(menu, node);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("再帰的に展開"), false, () => ExpandRecursive(itemId, true));
                menu.AddItem(new GUIContent("再帰的に折りたたむ"), false, () => ExpandRecursive(itemId, false));
                menu.ShowAsContext();
                return;
            }

            if (node.Kind == ProjectNodeKind.Asset)
            {
                menu.AddItem(new GUIContent("開く"), false, () => OpenAsset(node));
                AddPathActions(menu, node);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Project で選択"), false, () => SelectInProject(node));

                if (node.CanDeleteOnDisk)
                {
                    menu.AddSeparator(string.Empty);
                    menu.AddItem(new GUIContent("削除"), false, () => DeleteAssetOnDisk(node));
                }

                if (node.CanRemoveFromList)
                {
                    menu.AddSeparator(string.Empty);
                    menu.AddItem(new GUIContent("取り除く"), false, () => RemoveWithConfirm(node));
                }

                menu.ShowAsContext();
            }
        }

        private void BeginContextRename(int itemId)
        {
            var item = FindItem(itemId, rootItem);
            if (item == null)
                return;

            var node = GetNodeForId(itemId);
            if (node == null || !node.CanRenameInTree)
                return;

            _contextRenameItemId = itemId;
            SetSelection(new[] { itemId }, TreeViewSelectionOptions.RevealAndFrame);
            BeginRename(item);
        }

        private void AddPathActions(GenericMenu menu, CustomProjectNode node)
        {
            if (node.CanRevealInFinder)
                menu.AddItem(new GUIContent("OS で表示"), false, () => RevealInFinder(node));
            else
                menu.AddDisabledItem(new GUIContent("OS で表示"));

            if (node.CanCopyPath)
            {
                menu.AddItem(new GUIContent("パスのコピー"), false, () => CopyPath(node, false));
                menu.AddItem(new GUIContent("相対パスのコピー"), false, () => CopyPath(node, true));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("パスのコピー"));
                menu.AddDisabledItem(new GUIContent("相対パスのコピー"));
            }
        }

        private void ExpandRecursive(int rootItemId, bool expand)
        {
            var item = FindItem(rootItemId, rootItem);
            if (item == null)
                return;

            SetExpandedRecursive(item, expand);
            _model.Save();
            _window.RequestRefresh();
        }

        private void SetExpandedRecursive(TreeViewItem<int> item, bool expand)
        {
            SetExpanded(item.id, expand);
            var node = GetNodeForId(item.id);
            if (node != null && node.IsContainer)
                node.IsExpanded = expand;

            if (item.children == null)
                return;

            foreach (var child in item.children)
                SetExpandedRecursive(child, expand);
        }

        private void SyncExpandedState(List<CustomProjectNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsContainer)
                {
                    var id = GetIdForNode(node);
                    if (id >= 0)
                        node.IsExpanded = IsExpanded(id);
                }

                SyncExpandedState(node.Children);
            }
        }

        private void AddSubGroup(CustomProjectNode parent)
        {
            parent.IsExpanded = true;
            var parentId = GetIdForNode(parent);
            if (parentId >= 0)
                SetExpanded(parentId, true);

            var newNode = _model.AddGroup("New Group", parent);
            Reload();

            if (newNode == null)
                return;

            var id = GetIdForNode(newNode);
            if (id >= 0)
                SetSelection(new[] { id });
        }

        private void AddAssetToGroup(CustomProjectNode parent)
        {
            var path = EditorUtility.OpenFilePanel("項目を追加", "Assets", string.Empty);
            if (string.IsNullOrEmpty(path))
                return;

            if (path.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("エラー", "Assets フォルダ外のファイルは追加できません。", "OK");
                return;
            }

            _model.AddAssetRef(guid, parent);
            Reload();
        }

        private void AddObjectReference(UnityEngine.Object obj, CustomProjectNode parent)
        {
            if (obj == null)
                return;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return;

            if (AssetDatabase.IsValidFolder(path))
                _model.AddFolderRef(path, parent);
            else
                _model.AddAssetRef(AssetDatabase.AssetPathToGUID(path), parent);
        }

        private void RemoveWithConfirm(CustomProjectNode node)
        {
            if (!node.CanRemoveFromList)
                return;

            if (!EditorUtility.DisplayDialog("確認", $"\"{node.Label}\" をリストから取り除きますか？\n（実ファイルは削除されません）", "取り除く", "キャンセル"))
                return;

            _model.Remove(node);
            Reload();
        }

        private void RemoveMultipleWithConfirm(List<CustomProjectNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return;

            var bullets = string.Join("\n", nodes.Take(5).Select(n => $"  • {n.Label}"));
            if (nodes.Count > 5)
                bullets += $"\n  … 他 {nodes.Count - 5} 件";

            if (!EditorUtility.DisplayDialog("確認",
                $"{nodes.Count} 件の項目をリストから取り除きますか？\n（実ファイルは削除されません）\n\n{bullets}",
                "取り除く",
                "キャンセル"))
            {
                return;
            }

            foreach (var node in nodes)
                _model.Remove(node);
            Reload();
        }

        private void OpenAsset(CustomProjectNode node)
        {
            if (!node.CanOpenAsset)
                return;

            var path = node.ResolveAssetPath();
            if (string.IsNullOrEmpty(path))
                return;

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
                AssetDatabase.OpenAsset(obj);
        }

        private void SelectInProject(CustomProjectNode node)
        {
            var path = node.ResolveAssetPath();
            if (string.IsNullOrEmpty(path))
                return;

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null)
                return;

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private void RevealInFinder(CustomProjectNode node)
        {
            var path = node.ResolveAssetPath();
            if (string.IsNullOrEmpty(path))
                return;

            var abs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            EditorUtility.RevealInFinder(abs);
        }

        private void CopyPath(CustomProjectNode node, bool relative)
        {
            var path = node.ResolveAssetPath();
            if (string.IsNullOrEmpty(path))
                return;

            GUIUtility.systemCopyBuffer = relative
                ? path
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        private void DeleteAssetOnDisk(CustomProjectNode node)
        {
            var path = node.ResolveAssetPath();
            if (string.IsNullOrEmpty(path))
                return;

            if (!EditorUtility.DisplayDialog("確認", $"\"{node.Label}\" を削除しますか？\nこの操作は取り消せません。", "削除", "キャンセル"))
                return;

            AssetDatabase.DeleteAsset(path);
        }
    }

    public sealed class CustomProjectViewWindow : EditorWindow
    {
        private static readonly Type SceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

        [SerializeField] private TreeViewState<int> _treeViewState;
        [SerializeField] private string _searchQuery = string.Empty;
        [SerializeField] private bool _autoSyncSelection;

        private CustomProjectTreeView _treeView;
        private SearchField _searchField;
        private bool _needsRefresh;
        private Rect _treeViewRect;

        internal CustomProjectTreeModel Model { get; private set; }
        internal bool AutoSyncSelection => _autoSyncSelection;

        [MenuItem("Window/Custom Project View")]
        public static void Open()
        {
            var window = GetWindow<CustomProjectViewWindow>();
            window.titleContent = new GUIContent("Project Custom", CustomProjectViewIcons.Project);
            window.Show();
        }

        private void OnEnable()
        {
            if (_treeViewState == null)
                _treeViewState = new TreeViewState<int>();

            Model = new CustomProjectTreeModel();
            Model.Load();

            _searchField = _searchField ?? new SearchField();
            _treeView = new CustomProjectTreeView(_treeViewState, Model, this);
            _searchField.downOrUpArrowKeyPressed -= _treeView.SetFocusAndEnsureSelectedItem;
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            _treeView.Reload();

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (ShouldClearSelectionFromHierarchySelection())
            {
                _treeView?.ClearSelectionAndPing();
                Repaint();
                return;
            }

            if (!_autoSyncSelection)
                return;

            _treeView?.SyncSelectionFromUnity();
            Repaint();
        }

        public void RequestRefresh()
        {
            _needsRefresh = true;
            Repaint();
        }

        private static bool IsHierarchyWindow(EditorWindow window)
        {
            if (window == null)
                return false;

            if (SceneHierarchyWindowType != null)
                return SceneHierarchyWindowType.IsInstanceOfType(window);

            return string.Equals(window.GetType().Name, "SceneHierarchyWindow", StringComparison.Ordinal);
        }

        private bool ShouldClearSelectionFromHierarchySelection()
        {
            if (_treeView == null || !_treeView.HasSelection())
                return false;

            var activeWindow = EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
            if (!IsHierarchyWindow(activeWindow))
                return false;

            var selectedObject = Selection.activeObject;
            if (selectedObject == null)
                return true;

            if (EditorUtility.IsPersistent(selectedObject))
                return false;

            return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selectedObject));
        }

        private void OnGUI()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                _treeView?.Reload();
            }

            DrawToolbar();
            DrawSearchBar();
            DrawTreeView();

            if (Event.current.type == EventType.MouseDown
                && _treeViewRect.width > 0
                && !_treeViewRect.Contains(Event.current.mousePosition))
            {
                _treeView?.ClearSelectionAndPing();
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(new GUIContent(" Group", CustomProjectViewIcons.AddGroup, "グループをルートに追加"), EditorStyles.toolbarButton, GUILayout.Width(78)))
                AddRootGroup();

            GUILayout.FlexibleSpace();

            _autoSyncSelection = GUILayout.Toggle(_autoSyncSelection,
                new GUIContent(" Sync", CustomProjectViewIcons.Sync, "選択時に Project ビューと同期"),
                EditorStyles.toolbarButton,
                GUILayout.Width(70));

            if (GUILayout.Button(new GUIContent(
                " Expand",
                CustomProjectViewIcons.Expand,
                "すべて展開"),
                EditorStyles.toolbarButton,
                GUILayout.Width(84)))
            {
                _treeView?.ExpandAll(true);
            }

            if (GUILayout.Button(new GUIContent(
                " Collapse",
                CustomProjectViewIcons.Collapse,
                "すべて折りたたむ"),
                EditorStyles.toolbarButton,
                GUILayout.Width(90)))
            {
                _treeView?.ExpandAll(false);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            _searchField = _searchField ?? new SearchField();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            try
            {
                var newQuery = _searchField.OnToolbarGUI(_searchQuery);
                if (newQuery != _searchQuery)
                {
                    _searchQuery = newQuery;
                    _treeView?.SetSearch(_searchQuery);
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTreeView()
        {
            _treeViewRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            _treeView?.BeginFrame(_treeViewRect);
            _treeView?.OnGUI(_treeViewRect);
            HandleExternalDrop(_treeViewRect);
            _treeView?.TryClearSelectionFromEmptySpace(_treeViewRect);
        }

        private void HandleExternalDrop(Rect rect)
        {
            var evt = Event.current;
            if (evt.type == EventType.Used)
                return;
            if (!rect.Contains(evt.mousePosition))
                return;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
                return;
            if (DragAndDrop.GetGenericData("CustomProjectViewNodes") is List<CustomProjectNode>)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (AssetDatabase.IsValidFolder(path))
                        Model.AddFolderRef(path);
                    else
                        Model.AddAssetRef(AssetDatabase.AssetPathToGUID(path));
                }
                _treeView?.Reload();
            }

            evt.Use();
        }

        private void AddRootGroup()
        {
            PopupNameDialog.Show("グループを追加", "グループ名を入力してください", "New Group", name =>
            {
                Model.AddGroup(name);
                _treeView?.Reload();
            });
        }

    }

    internal sealed class PopupNameDialog : EditorWindow
    {
        private string _title;
        private string _message;
        private string _value;
        private Action<string> _onConfirm;
        private bool _focused;

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
            {
                Close();
                return;
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_value);
            if (GUILayout.Button("追加", GUILayout.Width(80))
                || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                _onConfirm?.Invoke(_value.Trim());
                Close();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

