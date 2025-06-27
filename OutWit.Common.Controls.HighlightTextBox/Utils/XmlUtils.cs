using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace OutWit.Common.Controls.HighlightTextBox.Utils
{
    public static class XmlUtils
    {
        public static string ReadString(this XElement me, string element)
        {
            return me.Element(element)?.Value.Trim();
        }

        public static IReadOnlyList<string> ReadStringList(this XElement me, string element)
        {
            var words = new List<string>();

            var text = me.ReadString(element);

            if (string.IsNullOrWhiteSpace(text))
                return words;

            foreach (var word in Regex.Split(text, "\\s+"))
            {
                if (!string.IsNullOrWhiteSpace(word))
                    words.Add(word.Trim());
            }

            return words;

        }

        public static bool ReadBoolean(this XElement me, string element)
        {
            var value = me.Element(element)?.Value.Trim();

            return bool.TryParse(value, out bool result) && result;
        }

        public static Brush ReadBrush(this XElement me, string element)
        {
            var value = me.Element(element)?.Value.Trim();

            if (string.IsNullOrEmpty(value))
                return null;

            try
            {
                return new BrushConverter().ConvertFrom(value) as Brush;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static FontWeight? ReadFontWeight(this XElement me, string element)
        {
            var value = me.Element(element)?.Value.Trim();

            if (string.IsNullOrEmpty(value))
                return null;

            try
            {
                if (new FontWeightConverter().ConvertFrom(value) is FontWeight fontWeight)
                    return fontWeight;

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static FontStyle? ReadFontStyle(this XElement me, string element)
        {
            var value = me.Element(element)?.Value.Trim();

            if (string.IsNullOrEmpty(value))
                return null;

            try
            {
                if (new FontStyleConverter().ConvertFrom(value) is FontStyle fontStyle)
                    return fontStyle;

                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
