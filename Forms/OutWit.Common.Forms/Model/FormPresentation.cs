namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// How the one who declared the form would like it drawn.
    /// </summary>
    /// <remarks>
    /// A preference and not an instruction. A renderer with nothing for <see cref="Segmented"/>
    /// draws a dropdown and is not wrong; a renderer that ignores <see cref="FormFieldKind"/> is.
    /// </remarks>
    public enum FormPresentation : byte
    {
        /// <summary>Whatever the renderer thinks best for the kind and the number of options.</summary>
        Auto = 0,

        /// <summary>All the options at once, side by side.</summary>
        Inline = 1,

        /// <summary>One option showing, the rest behind it.</summary>
        Dropdown = 2,

        /// <summary>A joined row of options, one of them pressed.</summary>
        Segmented = 3,

        /// <summary>A switch rather than a box to tick.</summary>
        Toggle = 4
    }
}
