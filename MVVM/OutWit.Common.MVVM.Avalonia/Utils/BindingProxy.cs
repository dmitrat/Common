using System;
using Avalonia;
using Avalonia.Data;

namespace OutWit.Common.MVVM.Avalonia.Utils
{
    /// <summary>
    /// Proxy for accessing DataContext in binding scenarios where direct access is not possible.
    /// This is the Avalonia equivalent of WPF's BindingProxy using Freezable.
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
    public class BindingProxy : AvaloniaObject
    {
        #region Styled Properties

        public static readonly StyledProperty<object?> DataProperty =
            AvaloniaProperty.Register<BindingProxy, object?>(
                nameof(Data),
                defaultBindingMode: BindingMode.OneWay);

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
