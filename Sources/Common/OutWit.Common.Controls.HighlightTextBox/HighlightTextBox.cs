using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OutWit.Common.Controls.HighlightTextBox.Highlighters;
using OutWit.Common.Controls.HighlightTextBox.Interfaces;
using OutWit.Common.Controls.HighlightTextBox.Utils;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.MVVM.Aspects;

namespace OutWit.Common.Controls.HighlightTextBox
{
    [TemplatePart(Name = nameof(HighlightTextBox.RENDER_CANVAS_PART), Type = typeof(DrawingControl))]
    [TemplatePart(Name = nameof(HighlightTextBox.LINE_NUMBERS_CANVAS_PART), Type = typeof(DrawingControl))]
    [TemplatePart(Name = nameof(HighlightTextBox.CONTENT_HOST_PART), Type = typeof(ScrollViewer))]
    public class HighlightTextBox : TextBox
    {
        #region Constants

        private const string RENDER_CANVAS_PART = "PART_RenderCanvas";
        private const string LINE_NUMBERS_CANVAS_PART = "PART_LineNumbersCanvas";
        private const string CONTENT_HOST_PART = "PART_ContentHost";

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty IsLineNumbersMarginVisibleProperty = BindingUtils.Register<HighlightTextBox, bool>(nameof(IsLineNumbersMarginVisible));

        public static readonly DependencyProperty LineHeightProperty = BindingUtils.Register<HighlightTextBox, double>(nameof(LineHeight), OnLineHeightChanged);

        public static readonly DependencyProperty MaxLineCountInBlockProperty = BindingUtils.Register<HighlightTextBox, int>(nameof(MaxLineCountInBlock), OnMaxLineCountInBlockChanged);

        public static readonly DependencyProperty HighlighterKeyProperty = BindingUtils.Register<HighlightTextBox, string>(nameof(HighlighterKey), OnHighlighterKeyChanged);

        public static readonly DependencyProperty PositionProperty = BindingUtils.Register<HighlightTextBox, int>(nameof(Position), OnPositionChanged);
        
        public static readonly DependencyProperty DefaultTextBrushProperty = BindingUtils.Register<HighlightTextBox, Brush>(nameof(DefaultTextBrush));

        #endregion

        #region Constructors

        static HighlightTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HighlightTextBox),
                new FrameworkPropertyMetadata(typeof(HighlightTextBox)));
        }

        public HighlightTextBox()
        {
            InitDefaults();
            InitEvents();
        }

        #endregion

        #region Inititalization

        private void InitDefaults()
        {
            MaxLineCountInBlock = 100;
            LineHeight = FontSize * 1.3;
            TotalLineCount = 1;
            Blocks = new List<HighlightTextBoxInner>();
        }

        private void InitEvents()
        {
            HighlighterManager.Instance.HighlighterAdded += OnHighlighterAdded;

            this.Loaded += OnHighlightTextBoxLoaded;
            this.SizeChanged += OnHighlightTextBoxSizeChanged;
            this.TextChanged += OnHighlightTextBoxTextChanged;
            this.SelectionChanged += OnSelectionChanged;
        }

        #endregion

        #region Functions


        private void UpdateBlocks() 
        {
			if (Blocks.Count == 0)
				return;

			// While something is visible after last block...
			while (!Blocks.Last().IsLast && Blocks.Last().GetPosition().Y + BlockHeight - VerticalOffset < ActualHeight)
            {
				int firstLineIndex = Blocks.Last().LineEndIndex + 1;
				int lastLineIndex = firstLineIndex + MaxLineCountInBlock - 1;
				lastLineIndex = lastLineIndex <= TotalLineCount - 1 ? lastLineIndex : TotalLineCount - 1;

				int fisrCharIndex = Blocks.Last().CharEndIndex + 1;
				int lastCharIndex = Text.GetLastCharIndexFromLineIndex(lastLineIndex); // to be optimized (forward search)

				if (lastCharIndex <= fisrCharIndex) 
                {
                    Blocks.Last().IsLast = true;
					return;
				}

				var block = new HighlightTextBoxInner(
					fisrCharIndex,
					lastCharIndex,
                    Blocks.Last().LineEndIndex + 1,
					lastLineIndex,
					LineHeight);

				block.RawText = block.GetSubString(Text);
				block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);
                Blocks.Add(block);
				FormatBlock(block);
			}
		}

		private void InvalidateBlocks(int changeOffset) 
        {
			HighlightTextBoxInner blockChanged = null;
			for (int i = 0; i < Blocks.Count; i++)
            {
				if (Blocks[i].CharStartIndex <= changeOffset && changeOffset <= Blocks[i].CharEndIndex + 1)
                {
					blockChanged = Blocks[i];
					break;
				}
			}

			if (blockChanged == null && changeOffset > 0)
				blockChanged = Blocks.Last();

			int fvline = blockChanged != null ? blockChanged.LineStartIndex : 0;
			int lvline = GetIndexOfLastVisibleLine();
			int fvchar = blockChanged != null ? blockChanged.CharStartIndex : 0;
			int lvchar = Text.GetLastCharIndexFromLineIndex(lvline);

			if (blockChanged != null)
                Blocks.RemoveRange(Blocks.IndexOf(blockChanged), Blocks.Count - Blocks.IndexOf(blockChanged));

			int localLineCount = 1;
			int charStart = fvchar;
			int lineStart = fvline;
			for (int i = fvchar; i < Text.Length; i++)
            {
				if (Text[i] == '\n') 
                {
					localLineCount += 1;
				}
				if (i == Text.Length - 1) 
                {
					string blockText = Text.Substring(charStart);

					var block = new HighlightTextBoxInner(
						charStart,
						i, lineStart,
						lineStart + blockText.GetLineCount() - 1,
						LineHeight);

					block.RawText = block.GetSubString(Text);
					block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);
					block.IsLast = true;

					foreach (var b in Blocks)
						if (b.LineStartIndex == block.LineStartIndex)
							throw new Exception();

                    Blocks.Add(block);
					FormatBlock(block);
					break;
				}
				if (localLineCount > MaxLineCountInBlock) 
                {
					var block = new HighlightTextBoxInner(
						charStart,
						i,
						lineStart,
						lineStart + MaxLineCountInBlock - 1,
						LineHeight);

					block.RawText = block.GetSubString(Text);
					block.LineNumbers = GetFormattedLineNumbers(block.LineStartIndex, block.LineEndIndex);

					foreach (var b in Blocks)
						if (b.LineStartIndex == block.LineStartIndex)
							throw new Exception();

                    Blocks.Add(block);
					FormatBlock(block);

					charStart = i + 1;
					lineStart += MaxLineCountInBlock;
					localLineCount = 1;

					if (i > lvchar)
						break;
				}
			}
		}

		private void DrawBlocks()
        {
			if (!IsLoaded || RenderCanvas == null || LineNumbersCanvas == null)
				return;

            using var textContext = RenderCanvas.GetContext();
            using var lineNumbersContext = LineNumbersCanvas.GetContext();

			foreach (var block in Blocks)
            {
                var position = block.GetPosition();
                var top = position.Y - VerticalOffset;
                var bottom = top + BlockHeight;

                if (!(top < ActualHeight) || !(bottom > 0)) 
                    continue;

                try
                {
                    textContext.DrawText(block.FormattedText, new Point(2 - HorizontalOffset, position.Y - VerticalOffset));

                    if (IsLineNumbersMarginVisible)
                    {
                        LineNumbersCanvas.Width = GetFormattedTextWidth($"{TotalLineCount:0000}") + 5;
                        lineNumbersContext.DrawText(block.LineNumbers, new Point(LineNumbersCanvas.ActualWidth, 1 + position.Y - VerticalOffset));
                    }
                } 
                catch 
                {
                    // Don't know why this exception is raised sometimes.
                    // Reproduce steps:
                    // - Sets a valid syntax highlighter on the box.
                    // - Copy a large chunk of code in the clipboard.
                    // - Paste it using ctrl+v and keep these buttons pressed.
                }
            }

		}

        private void CheckLineHeight()
        {
            UpdateBlockHeight();

            TextBlock.SetLineStackingStrategy(this, LineStackingStrategy.BlockLineHeight);
            TextBlock.SetLineHeight(this, LineHeight);
        }

        private void UpdateBlockHeight()
        {
            BlockHeight = MaxLineCountInBlock * LineHeight;
        }

        private void UpdateTotalLineCount() 
        {
            TotalLineCount = Text.GetLineCount();
        }

        /// <summary>
        /// Returns the index of the first visible text line.
        /// </summary>
        public int GetIndexOfFirstVisibleLine() 
        {
            int guessedLine = (int)(VerticalOffset / LineHeight);
            return guessedLine > TotalLineCount ? TotalLineCount : guessedLine;
        }

        /// <summary>
        /// Returns the index of the last visible text line.
        /// </summary>
        public int GetIndexOfLastVisibleLine() 
        {
            double height = VerticalOffset + ViewportHeight;
            int guessedLine = (int)(height / LineHeight);
            return guessedLine > TotalLineCount - 1 ? TotalLineCount - 1 : guessedLine;
        }


        /// <summary>
        /// Formats and Highlights the text of a block.
        /// </summary>
        private void FormatBlock(HighlightTextBoxInner currentBlock) 
        {
            if (Highlighter == null) 
                return;

            currentBlock.FormattedText = GetFormattedText(currentBlock.RawText, DefaultTextBrush ?? Brushes.Black);

            ThreadPool.QueueUserWorkItem(p =>
            {
                Highlighter.Highlight(currentBlock.FormattedText, -1);
            });
        }

        /// <summary>
        /// Returns the width of a text once formatted.
        /// </summary>
        private double GetFormattedTextWidth(string text)
        {
            return GetFormattedText(text, DefaultTextBrush ?? Brushes.Black).Width;
        }

        /// <summary>
        /// Returns a formatted text object from the given string
        /// </summary>
        private FormattedText GetFormattedText(string text, Brush brush)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, brush, 1)
            {
                Trimming = TextTrimming.None, LineHeight = LineHeight
            };
        }

        /// <summary>
        /// Returns a string containing a list of numbers separated with newlines.
        /// </summary>
        private FormattedText GetFormattedLineNumbers(int firstIndex, int lastIndex)
        {
            string text = "";

            for (int i = firstIndex + 1; i <= lastIndex + 1; i++)
                text += $"{i}\n";

            text = text.Trim();

            var formattedText = GetFormattedText(text, new SolidColorBrush(Color.FromRgb(0x21, 0xA1, 0xD8)));

            formattedText.TextAlignment = TextAlignment.Right;

            return formattedText;
        }

        private void UpdateCaretIndex()
        {
            if(Position == CaretIndex)
                return;

            CaretIndex = Position;
            
            var lineIndex = GetLineIndexFromCharacterIndex(CaretIndex);
            ScrollToLine(lineIndex);
        }

        private void ResetHighlighter()
        {
            if(!string.IsNullOrEmpty(HighlighterKey))
                Highlighter = HighlighterManager.Instance.Find(HighlighterKey);
        }

        #endregion

        #region Event Handlers

        protected override void OnRender(DrawingContext drawingContext)
        {
            DrawBlocks();
            base.OnRender(drawingContext);
        }
        
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
                UpdateBlocks();

            InvalidateVisual();
        }

        private void OnHighlightTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            RenderCanvas = (DrawingControl)Template.FindName(RENDER_CANVAS_PART, this);
            LineNumbersCanvas = (DrawingControl)Template.FindName(LINE_NUMBERS_CANVAS_PART, this);
            ScrollViewer = (ScrollViewer)Template.FindName(CONTENT_HOST_PART, this);

            LineNumbersCanvas.Width = GetFormattedTextWidth($"{TotalLineCount:0000}") + 5;

            ScrollViewer.ScrollChanged += OnScrollChanged;

            InvalidateBlocks(0);
            InvalidateVisual();
        }

        private void OnHighlightTextBoxSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged == false)
                return;

            UpdateBlocks();
            InvalidateVisual();
        }

        private void OnHighlightTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotalLineCount();
            InvalidateBlocks(e.Changes.First().Offset);
            InvalidateVisual();
        }

        private void OnHighlighterAdded(Highlighter highlighter)
        {
            ResetHighlighter();
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            Position = CaretIndex;
        }

        private static void OnLineHeightChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var editor = (HighlightTextBox)source;
            editor.CheckLineHeight();
            
        }

        private static void OnMaxLineCountInBlockChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var editor = (HighlightTextBox)source;
            editor.UpdateBlockHeight();
        }

        private static void OnHighlighterKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (HighlightTextBox) d;
            editor.ResetHighlighter();
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (HighlightTextBox) d;
            editor.UpdateCaretIndex();
        }

        #endregion

        #region Properties

        public IHighlighter Highlighter { get; set; }

        private DrawingControl RenderCanvas { get; set; }

        private DrawingControl LineNumbersCanvas  { get; set; }

        private ScrollViewer ScrollViewer  { get; set; }

        private double BlockHeight { get; set; }

        private int TotalLineCount { get; set; }

        private List<HighlightTextBoxInner> Blocks { get; set; }
        #endregion

        #region Bindable Properties

        [Bindable]
        public bool IsLineNumbersMarginVisible { get; set; }

        [Bindable]
        public double LineHeight { get; private set; }

        [Bindable]
        public int MaxLineCountInBlock { get; private set; }

        [Bindable]
        public string HighlighterKey { get; set; }

        [Bindable]
        public int Position { get; set; }

        [Bindable]
        public Brush DefaultTextBrush { get; set; }

        #endregion
    }
}
