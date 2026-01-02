using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace OutWit.Common.MVVM.Avalonia.Utils
{
    /// <summary>
    /// Utility methods for Avalonia bindings, styled properties, and visual tree traversal.
    /// For automatic StyledProperty generation, use [StyledProperty] attribute with the source generator.
    /// </summary>
    public static class BindingUtils
    {
        #region Visual Tree Traversal

        /// <summary>
        /// Finds the first child of the specified type in the visual tree.
        /// </summary>
        public static TChild? FindFirstChildOf<TChild>(this Visual me)
            where TChild : Visual
        {
            return FindFirstChildOf<TChild>(me, x => true);
        }

        /// <summary>
        /// Finds the first child of the specified type that matches the predicate.
        /// </summary>
        public static TChild? FindFirstChildOf<TChild>(this Visual me, Func<TChild, bool> predicate)
            where TChild : Visual
        {
            if (me == null)
                return null;

            var children = me.GetVisualChildren();

            foreach (var child in children)
            {
                if (child is TChild typed && predicate(typed))
                    return typed;

                var foundChild = FindFirstChildOf(child, predicate);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }

        /// <summary>
        /// Finds all children of the specified type in the visual tree.
        /// </summary>
        public static IReadOnlyList<TChild> FindAllChildrenOf<TChild>(this Visual me)
            where TChild : Visual
        {
            return me.FindAllChildrenOf<TChild>(x => true);
        }

        /// <summary>
        /// Finds all children of the specified type that match the predicate.
        /// </summary>
        public static IReadOnlyList<TChild> FindAllChildrenOf<TChild>(this Visual me, Func<TChild, bool> predicate)
            where TChild : Visual
        {
            var children = new List<TChild>();
            me.FindAllChildrenOf<TChild>(predicate, children);
            return children;
        }

        private static void FindAllChildrenOf<TChild>(this Visual me, Func<TChild, bool> predicate, List<TChild> children)
            where TChild : Visual
        {
            if (me == null)
                return;

            var visualChildren = me.GetVisualChildren();

            foreach (var child in visualChildren)
            {
                if (child is TChild typed && predicate(typed))
                    children.Add(typed);
                else
                    child.FindAllChildrenOf(predicate, children);
            }
        }

        /// <summary>
        /// Finds the first parent of the specified type in the visual tree.
        /// </summary>
        public static TParent? FindFirstParentOf<TParent>(this Visual me)
            where TParent : Visual
        {
            return FindFirstParentOf<TParent>(me, x => true);
        }

        /// <summary>
        /// Finds the first parent of the specified type that matches the predicate.
        /// </summary>
        public static TParent? FindFirstParentOf<TParent>(this Visual me, Func<TParent, bool> predicate)
            where TParent : Visual
        {
            if (me == null)
                return null;

            var parent = me.GetVisualParent();

            if (parent is TParent typed && predicate(typed))
                return typed;

            if (parent is Visual visualParent)
                return FindFirstParentOf(visualParent, predicate);

            return null;
        }

        #endregion

        #region ItemsControl Helpers

        /// <summary>
        /// Finds all visual descendants of an ItemsControl, including items.
        /// </summary>
        public static IEnumerable<Visual> GetAllVisualDescendants(this Visual me)
        {
            var list = new List<Visual>();
            me.AggregateVisualDescendants(list);
            return list;
        }

        private static void AggregateVisualDescendants(this Visual me, List<Visual> descendants)
        {
            descendants.Add(me);

            if (me is ItemsControl itemsControl)
            {
                foreach (var item in itemsControl.Items)
                {
                    if (item is Visual visualItem)
                        visualItem.AggregateVisualDescendants(descendants);
                }
            }

            foreach (var child in me.GetVisualChildren())
            {
                child.AggregateVisualDescendants(descendants);
            }
        }

        #endregion

        #region Reflection Helpers

        /// <summary>
        /// Gets the value of a property or field using reflection.
        /// </summary>
        public static object? GetValue(this object me, string memberPath)
        {
            try
            {
                if (string.IsNullOrEmpty(memberPath))
                    return me;

                var type = me.GetType();
                var property = type.GetProperty(memberPath);
                if (property != null)
                    return property.GetValue(me);

                var field = type.GetField(memberPath);
                return field?.GetValue(me);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}
