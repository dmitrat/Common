using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OutWit.Common.MVVM.WPF.Utils
{
    /// <summary>
    /// Utility methods for WPF bindings, dependency properties, and visual tree traversal.
    /// For automatic DependencyProperty generation, use [StyledProperty] attribute with the source generator.
    /// </summary>
    public static class BindingUtils
    {
        #region RoutedEvent Registration

        /// <summary>
        /// Registers a routed event.
        /// </summary>
        public static RoutedEvent Register<TControl>(string name, RoutingStrategy strategy = RoutingStrategy.Direct)
            where TControl : DependencyObject
        {
            return EventManager.RegisterRoutedEvent(name, strategy, typeof(RoutedEventHandler), typeof(TControl));
        }

        #endregion

        #region Binding Updates

        /// <summary>
        /// Updates all bindings in all windows of the application.
        /// </summary>
        public static void UpdateBinding(this Application application)
        {
            foreach (Window window in application.Windows)
            {
                window.UpdateBinding();
            }
        }

        /// <summary>
        /// Updates bindings for data triggers in a style.
        /// </summary>
        public static void UpdateBinding(this Style me)
        {
            if (me == null || me.Triggers.Count == 0)
                return;

            foreach (var trigger in me.Triggers.OfType<DataTrigger>())
            {
                // Trigger update logic
            }
        }

        /// <summary>
        /// Updates all bindings in the visual tree starting from the specified element.
        /// </summary>
        public static void UpdateBinding(this DependencyObject me)
        {
            foreach (DependencyObject element in me.AggregateVisualDescendents())
            {
                LocalValueEnumerator localValueEnumerator = element.GetLocalValueEnumerator();
                while (localValueEnumerator.MoveNext())
                {
                    BindingExpressionBase? bindingExpression = BindingOperations.GetBindingExpressionBase(element, localValueEnumerator.Current.Property);
                    bindingExpression?.UpdateTarget();
                }
            }
        }

        #endregion

        #region Visual Tree Traversal

        private static IEnumerable<DependencyObject> AggregateVisualDescendents(this DependencyObject me)
        {
            var list = new List<DependencyObject>();
            me.AggregateVisualDescendents(list);
            return list;
        }

        private static void AggregateVisualDescendents(this DependencyObject me, List<DependencyObject> descendents)
        {
            descendents.Add(me);
            me.AggregateVisualItems(descendents);
            me.AggregateVisualChildren(descendents);
        }

        private static void AggregateVisualItems(this DependencyObject me, List<DependencyObject> descendents)
        {
            if (!(me is ItemsControl itemsControl))
                return;

            foreach (var dependencyObject in itemsControl.Items.OfType<DependencyObject>())
                dependencyObject.AggregateVisualDescendents(descendents);
        }

        private static void AggregateVisualChildren(this DependencyObject me, List<DependencyObject> descendents)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(me); i++)
                VisualTreeHelper.GetChild(me, i).AggregateVisualDescendents(descendents);
        }

        /// <summary>
        /// Finds the first child of the specified type in the visual tree.
        /// </summary>
        public static TChild? FindFirstChildOf<TChild>(this DependencyObject me)
            where TChild : DependencyObject
        {
            return FindFirstChildOf<TChild>(me, x => true);
        }

        /// <summary>
        /// Finds the first child of the specified type that matches the predicate.
        /// </summary>
        public static TChild? FindFirstChildOf<TChild>(this DependencyObject me, Func<TChild, bool> predicate)
            where TChild : DependencyObject
        {
            if (me == null)
                return null;

            var childrenCount = VisualTreeHelper.GetChildrenCount(me);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(me, i);

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
        public static IReadOnlyList<TChild> FindAllChildrenOf<TChild>(this DependencyObject me)
            where TChild : DependencyObject
        {
            return me.FindAllChildrenOf<TChild>(x => true);
        }

        /// <summary>
        /// Finds all children of the specified type that match the predicate.
        /// </summary>
        public static IReadOnlyList<TChild> FindAllChildrenOf<TChild>(this DependencyObject me, Func<TChild, bool> predicate)
            where TChild : DependencyObject
        {
            var children = new List<TChild>();
            me.FindAllChildrenOf<TChild>(predicate, children);
            return children;
        }

        private static void FindAllChildrenOf<TChild>(this DependencyObject me, Func<TChild, bool> predicate, List<TChild> children)
            where TChild : DependencyObject
        {
            if (me == null)
                return;

            var childrenCount = VisualTreeHelper.GetChildrenCount(me);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(me, i);

                if (child is TChild typed && predicate(typed))
                    children.Add(typed);
                else
                    child.FindAllChildrenOf(predicate, children);
            }
        }

        /// <summary>
        /// Finds the first parent of the specified type in the visual tree.
        /// </summary>
        public static TChild? FindFirstParentOf<TChild>(this DependencyObject me)
            where TChild : DependencyObject
        {
            return FindFirstParentOf<TChild>(me, x => true);
        }

        /// <summary>
        /// Finds the first parent of the specified type that matches the predicate.
        /// </summary>
        public static TChild? FindFirstParentOf<TChild>(this DependencyObject me, Func<TChild, bool> predicate)
            where TChild : DependencyObject
        {
            if (me == null)
                return null;

            var parent = VisualTreeHelper.GetParent(me);

            if (parent is TChild typed && predicate(typed))
                return typed;

            var foundChild = FindFirstParentOf(parent, predicate);
            if (foundChild != null)
                return foundChild;

            return null;
        }

        #endregion

        #region Layout Updates

        /// <summary>
        /// Forces a complete layout update of the element and all its children.
        /// </summary>
        public static void ForceUpdate(this FrameworkElement me, double width, double height)
        {
            if (me == null)
                return;

            me.UpdateDefaultStyle();
            me.ApplyTemplate();
            me.UpdateLayout();

            var childrenCount = VisualTreeHelper.GetChildrenCount(me);

            for (int i = 0; i < childrenCount; i++)
                (VisualTreeHelper.GetChild(me, i) as FrameworkElement)?.ForceUpdate(width, height);
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
