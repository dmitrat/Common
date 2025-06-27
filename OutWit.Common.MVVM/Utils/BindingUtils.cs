using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OutWit.Common.MVVM.Utils
{
	public static class BindingUtils
	{
		public static RoutedEvent Register<TControl>(string name, RoutingStrategy strategy = RoutingStrategy.Direct)
			where TControl : DependencyObject
		{
			return EventManager.RegisterRoutedEvent(name, strategy, typeof(RoutedEventHandler), typeof(TControl));
		}


		public static DependencyProperty Register<TControl, TValue>(string name, TValue defaultValue = default(TValue))
			where TControl : DependencyObject
		{
			return DependencyProperty.Register(name, typeof(TValue), typeof(TControl),
				new PropertyMetadata(defaultValue));
		}
        public static DependencyProperty Attach<TControl, TValue>(string name, TValue defaultValue = default(TValue))
            where TControl : DependencyObject
        {
            return DependencyProperty.RegisterAttached(name, typeof(TValue), typeof(TControl),
                new PropertyMetadata(defaultValue));
        }


        public static DependencyProperty Register<TControl, TValue>(string name, FrameworkPropertyMetadataOptions options, TValue defaultValue = default(TValue))
            where TControl : DependencyObject
		{
            return DependencyProperty.Register(name, typeof(TValue), typeof(TControl),
                new FrameworkPropertyMetadata(defaultValue, options));
        } public static DependencyProperty Attach<TControl, TValue>(string name, FrameworkPropertyMetadataOptions options, TValue defaultValue = default(TValue))
            where TControl : DependencyObject
        {
            return DependencyProperty.RegisterAttached(name, typeof(TValue), typeof(TControl),
                new FrameworkPropertyMetadata(defaultValue, options));
        }


        public static DependencyProperty Register<TControl, TValue>(string name, TValue defaultValue, PropertyChangedCallback callback)
			where TControl : DependencyObject
		{
			return DependencyProperty.Register(name, typeof(TValue), typeof(TControl),
				new PropertyMetadata(defaultValue, callback));
		}
        public static DependencyProperty Attach<TControl, TValue>(string name, TValue defaultValue, PropertyChangedCallback callback)
            where TControl : DependencyObject
        {
            return DependencyProperty.RegisterAttached(name, typeof(TValue), typeof(TControl),
                new PropertyMetadata(defaultValue, callback));
        }


        public static DependencyProperty Register<TControl, TValue>(string name, FrameworkPropertyMetadataOptions options, TValue defaultValue, PropertyChangedCallback callback)
            where TControl : DependencyObject
		{
            return DependencyProperty.Register(name, typeof(TValue), typeof(TControl),
                new FrameworkPropertyMetadata(defaultValue, options, callback));
        }
        public static DependencyProperty Attach<TControl, TValue>(string name, FrameworkPropertyMetadataOptions options, TValue defaultValue, PropertyChangedCallback callback)
            where TControl : DependencyObject
        {
            return DependencyProperty.RegisterAttached(name, typeof(TValue), typeof(TControl),
                new FrameworkPropertyMetadata(defaultValue, options, callback));
        }


        public static DependencyProperty Register<TControl, TValue>(string name, PropertyChangedCallback callback)
			where TControl : DependencyObject
		{
			return DependencyProperty.Register(name, typeof(TValue), typeof(TControl),
				new PropertyMetadata(default(TValue), callback));
		}
        public static DependencyProperty Attach<TControl, TValue>(string name, PropertyChangedCallback callback)
            where TControl : DependencyObject
        {
            return DependencyProperty.RegisterAttached(name, typeof(TValue), typeof(TControl),
                new PropertyMetadata(default(TValue), callback));
        }


        public static DependencyProperty Register<TControl, TValue>(string name, FrameworkPropertyMetadataOptions options, PropertyChangedCallback callback)
            where TControl : DependencyObject
		{
            return DependencyProperty.Register(name, typeof(TValue), typeof(TControl),
                new FrameworkPropertyMetadata(default(TValue), options, callback));
        }
        public static DependencyProperty Attach<TControl, TValue>(string name, FrameworkPropertyMetadataOptions options, PropertyChangedCallback callback)
            where TControl : DependencyObject
        {
            return DependencyProperty.RegisterAttached(name, typeof(TValue), typeof(TControl),
                new FrameworkPropertyMetadata(default(TValue), options, callback));
        }


        public static void UpdateBinding(this Application application)
		{
			foreach (Window window in application.Windows)
			{
				window.UpdateBinding();
			}
		}

		public static void UpdateBinding(this Style me)
		{
			if(me == null || me.Triggers.Count == 0)
				return;

			foreach (var trigger in me.Triggers.OfType<DataTrigger>())
			{
				//foreach (var binding in BindingOperations.GetSourceUpdatingBindings(trigger))
				//{
				//    binding.UpdateTarget();
				//}
				
				//trigger.UpdateBinding();
			}

		}


		public static void UpdateBinding(this DependencyObject me)
        {
            foreach (DependencyObject element in me.AggregateVisualDescendents())
			{
				LocalValueEnumerator localValueEnumerator = element.GetLocalValueEnumerator();
				while (localValueEnumerator.MoveNext())
				{
					BindingExpressionBase bindingExpression = BindingOperations.GetBindingExpressionBase(element, localValueEnumerator.Current.Property);
					bindingExpression?.UpdateTarget();
				}
			}
		}

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

        //private static void AggregatePopups(this DependencyObject me, List<DependencyObject> descendents)
        //{
        //    if (!(me is PopupBox))
        //        return;

        //    foreach (var dependencyObject in itemsControl.Items.OfType<DependencyObject>())
        //        dependencyObject.AggregateVisualDescendents(descendents);
        //}

		private static void AggregateVisualChildren(this DependencyObject me, List<DependencyObject> descendents)
        {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(me); i++)
                VisualTreeHelper.GetChild(me, i).AggregateVisualDescendents(descendents);
        }

        public static TChild FindFirstChildOf<TChild>(this DependencyObject me)
            where TChild : DependencyObject
        {
            return FindFirstChildOf<TChild>(me, x => true);
        }

        public static TChild FindFirstChildOf<TChild>(this DependencyObject me, Func<TChild, bool> predicate)
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

        public static IReadOnlyList<TChild> FindAllChildrenOf<TChild>(this DependencyObject me)
            where TChild : DependencyObject
        {
            return me.FindAllChildrenOf<TChild>(x => true);
        }

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


        public static void ForceUpdate(this FrameworkElement me, double width, double height)
        {
            if (me == null)
                return;

            //me.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //me.Arrange(new Rect(new Size(width, height)));
            me.UpdateDefaultStyle();
            me.ApplyTemplate();
            me.UpdateLayout();


            var childrenCount = VisualTreeHelper.GetChildrenCount(me);

            for (int i = 0; i < childrenCount; i++)
                (VisualTreeHelper.GetChild(me, i) as FrameworkElement)?.ForceUpdate(width, height);

        }


        public static TChild FindFirstParentOf<TChild>(this DependencyObject me)
            where TChild : DependencyObject
        {
            return FindFirstParentOf<TChild>(me, x => true);
        }

        public static TChild FindFirstParentOf<TChild>(this DependencyObject me, Func<TChild, bool> predicate)
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


        public static object GetValue(this object me, string memberPath)
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
            catch (Exception e)
            {
                return null;
            }
        }
	}
}
