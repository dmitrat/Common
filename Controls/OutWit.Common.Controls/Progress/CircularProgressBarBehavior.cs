using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using AspectInjector.Broker;
using MaterialDesignThemes.Wpf;
using Microsoft.Xaml.Behaviors;
using OutWit.Common.Controls.Prompts;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Progress
{
    public class CircularProgressBarBehavior : Behavior<ProgressBar>
    {
        public static readonly DependencyProperty StrokeThicknessProperty = BindingUtils.Register<CircularProgressBarBehavior, double>(nameof(StrokeThickness), 3d);
        
        protected override void OnAttached()
        {
            base.OnAttached();
        }

        [Bindable]
        public double StrokeThickness { get; set; }
    }
}
