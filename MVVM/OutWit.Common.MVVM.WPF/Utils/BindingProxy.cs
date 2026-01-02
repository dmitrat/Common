using System;
using System.Windows;

namespace OutWit.Common.MVVM.WPF.Utils
{
    /// <summary>
    /// Freezable proxy for accessing DataContext in binding scenarios where direct access is not possible.
    /// Useful for accessing parent DataContext from within DataGrid columns or other templated controls.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// <Window.Resources>
    ///     <local:BindingProxy x:Key="Proxy" Data="{Binding}" />
    /// </Window.Resources>
    /// 
    /// <DataGrid>
    ///     <DataGrid.Columns>
    ///         <DataGridTemplateColumn>
    ///             <DataGridTemplateColumn.CellTemplate>
    ///                 <DataTemplate>
    ///                     <Button Command="{Binding Data.DeleteCommand, Source={StaticResource Proxy}}" />
    ///                 </DataTemplate>
    ///             </DataGridTemplateColumn.CellTemplate>
    ///         </DataGrid.Columns>
    ///     </DataGrid>
    /// </DataGrid>
    /// ]]>
    /// </example>
    public class BindingProxy : Freezable
    {
        #region Dependency Properties

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(object),
                typeof(BindingProxy),
                new PropertyMetadata(null));

        #endregion

        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the data to be proxied. Typically bound to the parent DataContext.
        /// </summary>
        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        #endregion
    }
}
