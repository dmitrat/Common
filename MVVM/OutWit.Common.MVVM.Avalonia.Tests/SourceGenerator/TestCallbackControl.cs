using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.Avalonia.Tests.SourceGenerator
{
    /// <summary>
    /// One control per callback shape the generator claims to discover. Avalonia has no
    /// changed-callback parameter on Register, so the generator has to subscribe to the
    /// property's Changed observable — and for a long time it only emitted a comment saying
    /// somebody ought to. These properties are the proof that it does.
    /// </summary>
    public partial class TestCallbackControl : Control
    {
        #region Functions

        public void Reset()
        {
            TextChanges.Clear();
            ActiveChanges.Clear();
            RenamedChanges.Clear();
            NumberChanges.Clear();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Instance callback with typed arguments — the shape the documentation shows.
        /// </summary>
        private void OnTextChanged(AvaloniaPropertyChangedEventArgs<string> e)
        {
            TextChanges.Add($"{e.OldValue.GetValueOrDefault()}->{e.NewValue.GetValueOrDefault()}");
        }

        /// <summary>
        /// Instance callback with the untyped arguments, on a direct property.
        /// </summary>
        private void OnIsActiveChanged(AvaloniaPropertyChangedEventArgs e)
        {
            ActiveChanges.Add((bool)e.NewValue!);
        }

        /// <summary>
        /// Static callback taking the object that changed.
        /// </summary>
        private static void OnNumberChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e)
        {
            ((TestCallbackControl)sender).NumberChanges.Add((int)e.NewValue!);
        }

        /// <summary>
        /// Named by the attribute rather than by convention.
        /// </summary>
        private void HandleRenamed(AvaloniaPropertyChangedEventArgs<string> e)
        {
            RenamedChanges.Add(e.NewValue.GetValueOrDefault() ?? string.Empty);
        }

        #endregion

        #region Properties

        [StyledProperty(DefaultValue = "")]
        public string Text { get; set; } = default!;

        [StyledProperty(DefaultValue = 0)]
        public int Number { get; set; }

        [StyledProperty(DefaultValue = "", OnChanged = nameof(HandleRenamed))]
        public string Renamed { get; set; } = default!;

        [DirectProperty(DefaultValue = false)]
        public bool IsActive { get; set; }

        /// <summary>
        /// No callback at all: the generated field must stay a plain initializer.
        /// </summary>
        [StyledProperty(DefaultValue = "quiet")]
        public string Untouched { get; set; } = default!;

        public List<string> TextChanges { get; } = new();

        public List<bool> ActiveChanges { get; } = new();

        public List<string> RenamedChanges { get; } = new();

        public List<int> NumberChanges { get; } = new();

        #endregion
    }
}
