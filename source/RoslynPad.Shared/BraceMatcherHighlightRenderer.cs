// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace JustyBase.Editor;

public class BraceMatcherHighlightRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private readonly CommonBrush _backgroundBrush;
    public BraceMatchingResult? LeftOfPosition { get; private set; }
    public BraceMatchingResult? RightOfPosition { get; private set; }

    public const string BracketHighlight = "Bracket highlight";

    public BraceMatcherHighlightRenderer(TextView textView)
    {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));

        _textView.BackgroundRenderers.Add(this);

        var brush = new SolidColorBrush(Color.FromArgb(150, 190, 255, 190));
        if (brush != null)
        {
            _backgroundBrush = brush;
        }
        else
        {
            _backgroundBrush = Brushes.Transparent;
        }
    }

    public void SetHighlight(BraceMatchingResult? leftOfPosition, BraceMatchingResult? rightOfPosition)
    {
        if (LeftOfPosition != leftOfPosition || RightOfPosition != rightOfPosition)
        {
            LeftOfPosition = leftOfPosition;
            RightOfPosition = rightOfPosition;
            _textView.InvalidateLayer(Layer);
        }
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (LeftOfPosition == null && RightOfPosition == null)
            return;

        var builder = new BackgroundGeometryBuilder
        {
            CornerRadius = 1,
#if !AVALONIA
            AlignToWholePixels = true
#endif
        };

        if (RightOfPosition != null)
        {
            builder.AddSegment(textView, new TextSegment { StartOffset = RightOfPosition.Value.LeftPosition/*LeftSpan.Start*/, Length = 1/*RightOfPosition.Value.LeftSpan.Length*/ });
            builder.CloseFigure();
            builder.AddSegment(textView, new TextSegment { StartOffset = RightOfPosition.Value.RightPosition/*RightSpan.Start*/, Length = 1/*RightOfPosition.Value.RightSpan.Length*/ });
            builder.CloseFigure();
        }

        if (LeftOfPosition != null)
        {
            builder.AddSegment(textView, new TextSegment { StartOffset = LeftOfPosition.Value.LeftPosition/*LeftSpan.Start*/, Length = 1 /*LeftOfPosition.Value.LeftSpan.Length*/ });
            builder.CloseFigure();
            builder.AddSegment(textView, new TextSegment { StartOffset = LeftOfPosition.Value.RightPosition/*RightSpan.Start*/, Length = 1/*LeftOfPosition.Value.RightSpan.Length*/ });
            builder.CloseFigure();
        }

        var geometry = builder.CreateGeometry();
        if (geometry != null)
        {
            drawingContext.DrawGeometry(_backgroundBrush, null, geometry);
        }
    }
}
