using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Utils
{
    public static class AnimationExtensions
    {

        public static EventTrigger GetTrigger<T, TResult>(this T me, RoutedEvent routedEvent, DependencyProperty valueProperty, Expression<Func<T, TResult>> bindingStart, Expression<Func<T, TResult>> bindingEnd, double seconds)
        {
            var animation = new DoubleAnimation { Duration = new Duration(TimeSpan.FromSeconds(seconds)) };
            BindingOperations.SetBinding(animation, DoubleAnimation.FromProperty, me.CreateBinding(bindingStart, BindingMode.OneWay));
            BindingOperations.SetBinding(animation, DoubleAnimation.ToProperty, me.CreateBinding(bindingEnd, BindingMode.OneWay));

            Storyboard.SetTargetProperty(animation, new PropertyPath(valueProperty));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            var begin = new BeginStoryboard { Storyboard = storyboard };

            var trigger = new EventTrigger(routedEvent);
            trigger.Actions.Add(begin);

            return trigger;
        }


    }
}
