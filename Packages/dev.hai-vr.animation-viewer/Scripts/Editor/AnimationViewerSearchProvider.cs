#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace Hai.AnimationViewer.Scripts.Editor
{
    static class AnimationViewerSearchProvider
    {
        private const string AnimExtensionWithDot = ".anim";

        // See https://docs.unity3d.com/2022.3/Documentation/Manual/api-register-provider.html
        // See https://docs.unity3d.com/ScriptReference/Search.SearchProvider.html
        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider("animation_viewer", "Animation Viewer")
            {
                filterId = "anim:",
                priority = 99999,
                fetchItems = (context, items, provider) => FetchItems(context, provider),
                toObject = (item, type) => AssetDatabase.LoadMainAssetAtPath(item.id),
                fetchThumbnail = FetchThumbnail,
                trackSelection = TrackSelection,
                startDrag = StartDrag
            };
        }
        
        private static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
#if UNITY_2022_1_OR_NEWER
            if (context.empty)
                yield break;
#else
            if (string.IsNullOrEmpty(context.searchText))
                yield break;
#endif

            foreach (var guid in AssetDatabase.FindAssets($"t:animationclip {context.searchQuery}"))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var cleanName = ExtractCleanName(assetPath);
                yield return provider.CreateItem(context, assetPath, cleanName, null, null, null);
            }
        }

        private static string ExtractCleanName(string assetPath)
        {
            if (!assetPath.EndsWith(AnimExtensionWithDot)) return assetPath;
            
            var lastIndex = assetPath.LastIndexOf("/", StringComparison.Ordinal);
            if (lastIndex != -1)
            {
                var startIndex = lastIndex + 1;
                return assetPath.Substring(startIndex, assetPath.Length - AnimExtensionWithDot.Length - startIndex);
            }
            return assetPath;
        }

        private static Texture2D FetchThumbnail(SearchItem searchItem, SearchContext searchContext)
        {
            var assetPath = searchItem.id;
            if (!assetPath.EndsWith(AnimExtensionWithDot)) return null;

            var texture = AnimationViewerEditorWindow.InternalGetRenderQueue().RequireRender(assetPath);
            AnimationViewerEditorWindow.QueueRerender();
            
            return texture;
        }

        // Direct copy of sample code at:
        // https://docs.unity3d.com/ScriptReference/Search.SearchProvider.html
        private static void StartDrag(SearchItem item, SearchContext context)
        {
            if (context.selection.Count > 1)
            {
                var selectedObjects = context.selection.Select(i => AssetDatabase.LoadMainAssetAtPath(i.id));
                var paths = context.selection.Select(i => i.id).ToArray();
                StartDrag(selectedObjects.ToArray(), paths, item.GetLabel(context, true));
            }
            else
                StartDrag(new[] { AssetDatabase.LoadMainAssetAtPath(item.id) }, new[] { item.id }, item.GetLabel(context, true));
        }

        // Direct copy of sample code at:
        // https://docs.unity3d.com/ScriptReference/Search.SearchProvider.html
        private static void StartDrag(UnityEngine.Object[] objects, string[] paths, string label = null)
        {
            if (paths == null || paths.Length == 0)
                return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = objects;
            DragAndDrop.paths = paths;
            DragAndDrop.StartDrag(label);
        }

        private static void TrackSelection(SearchItem searchItem, SearchContext searchContext)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(searchItem.id));
        }
    }
}
#endif