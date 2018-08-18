﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.ComponentModel;

namespace SASR
{
    public class ResizeAdorner : Adorner
    {
        const double THUMB_SIZE = 10;
        const double MINIMAL_SIZE = 20;
        const double MOVE_OFFSET = 20;

        //9 thumbs
        /*                        moveAndRotateThumb
         *                              *
         *                              *
         * topLeftThumb*************topMiddleThumb**************topRightThumb        
         *      *                                                    *
         *      *                                                    *
         *      *                                                    *
         * middleLeftThumb                                     middleRightThumb
         *      *                                                    *
         *      *                                                    *
         *      *                                                    * 
         * bottomLeftThumb*********bottomMiddleThumb**************bottomRightThumb
         * 
         * */
        Thumb middleLeftThumb, middleRightThumb;

        //Rectangle thumbRectangle;


        VisualCollection visualCollection;

        public ResizeAdorner(UIElement adorned) : base(adorned)
        {
            visualCollection = new VisualCollection(this);

            //visualCollection.Add(thumbRectangle = GetResizeRectangle());

            //visualCollection.Add(topLeftThumb = GetResizeThumb(Cursors.SizeNWSE, HorizontalAlignment.Left, VerticalAlignment.Top));
            visualCollection.Add(middleLeftThumb = GetResizeThumb(Cursors.SizeWE, HorizontalAlignment.Left, VerticalAlignment.Center));
            //visualCollection.Add(bottomLeftThumb = GetResizeThumb(Cursors.SizeNESW, HorizontalAlignment.Left, VerticalAlignment.Bottom));

            //visualCollection.Add(topRightThumb = GetResizeThumb(Cursors.SizeNESW, HorizontalAlignment.Right, VerticalAlignment.Top));
            visualCollection.Add(middleRightThumb = GetResizeThumb(Cursors.SizeWE, HorizontalAlignment.Right, VerticalAlignment.Center));
            //visualCollection.Add(bottomRightThumb = GetResizeThumb(Cursors.SizeNWSE, HorizontalAlignment.Right, VerticalAlignment.Bottom));

            //visualCollection.Add(topMiddleThumb = GetResizeThumb(Cursors.SizeNS, HorizontalAlignment.Center, VerticalAlignment.Top));
            //visualCollection.Add(bottomMiddleThumb = GetResizeThumb(Cursors.SizeNS, HorizontalAlignment.Center, VerticalAlignment.Bottom));

            //visualCollection.Add(moveThumb = GetMoveThumb());


        }
        private Rectangle GetResizeRectangle()
        {
            var rectangle = new Rectangle()
            {
                Width = AdornedElement.RenderSize.Width,
                Height = AdornedElement.RenderSize.Height,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Gold,
                StrokeThickness = (double)1
            };
            return rectangle;
        }

        private Thumb GetResizeThumb(Cursor cur, HorizontalAlignment horizontal, VerticalAlignment vertical)
        {
            var thumb = new Thumb()
            {
                //Background = Brushes.Red,
                Width = 10,
                Height = 50,
                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical,
                Cursor = cur,
                Template = new ControlTemplate(typeof(Thumb))
                {
                    VisualTree = GetThumbTemple(new SolidColorBrush(Colors.Gold))
                }
            };
            thumb.DragDelta += (s, e) =>
            {
                var element = AdornedElement as FrameworkElement;

                if (element == null)
                    return;

                this.ElementResize(element);

                switch (thumb.VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        if (element.Height + e.VerticalChange > MINIMAL_SIZE)
                        {
                            element.Height += e.VerticalChange;
                            //thumbRectangle.Height += e.VerticalChange;
                        }
                        break;

                    //case VerticalAlignment.Center:
                    //    if ()
                    //    {

                    //    }
                    //    break;
                    case VerticalAlignment.Top:                       
                        if (element.Height - e.VerticalChange > MINIMAL_SIZE)
                        {
                            element.Height -= e.VerticalChange;
                            //thumbRectangle.Height -= e.VerticalChange;

                            Canvas.SetTop(element, Canvas.GetTop(element) + e.VerticalChange);
                        }
                        break;
                }
                switch (thumb.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        if (element.Width - e.HorizontalChange > MINIMAL_SIZE)
                        {
                            element.Width -= e.HorizontalChange;
                            //thumbRectangle.Width -= e.HorizontalChange;
                            Canvas.SetLeft(element, Canvas.GetLeft(element) + e.HorizontalChange);
                        }
                        break;
                    case HorizontalAlignment.Right:
                        if (element.Width + e.HorizontalChange > MINIMAL_SIZE)
                        {
                            element.Width += e.HorizontalChange;
                            //thumbRectangle.Width += e.HorizontalChange;
                        }
                        break;
                }

                e.Handled = true;
            };
            return thumb;
        }

        private void ElementResize(FrameworkElement frameworkElement)
        {
            if (Double.IsNaN(frameworkElement.Width))
            {
                frameworkElement.Width = frameworkElement.RenderSize.Width;
            }

            if (Double.IsNaN(frameworkElement.Height))
                frameworkElement.Height = frameworkElement.RenderSize.Height;
        }

        // get Thumb Temple
        private FrameworkElementFactory GetThumbTemple(Brush back)
        {
            back.Opacity = 0.6d;
            //var fef = new FrameworkElementFactory(typeof(Ellipse));
            //fef.SetValue(Ellipse.FillProperty, back);
            //fef.SetValue(Ellipse.StrokeProperty, Brushes.DarkGreen);
            //fef.SetValue(Ellipse.StrokeThicknessProperty, (double)1);
            var fef = new FrameworkElementFactory(typeof(Rectangle));
            fef.SetValue(Rectangle.FillProperty, back);
            //fef.SetValue(Line.StrokeProperty, back);
            //fef.SetValue(Line.StrokeThicknessProperty, (double)15);
            fef.SetValue(Rectangle.HeightProperty, (double)50);
            fef.SetValue(Rectangle.WidthProperty,(double)15);
            return fef;
        }

        private Thumb GetMoveThumb()
        {
            var thumb = new Thumb()
            {
                Width = 10,
                Height = 50,
                //Cursor = wpfDecorator.CursorHelper.CreateCursor(@"..\..\wpfAdorner\旋转.png", 8, 8),
                Template = new ControlTemplate(typeof(Thumb))
                {
                    VisualTree = GetThumbTemple(GetMoveEllipseBack())
                }
            };
            thumb.DragDelta += (s, e) =>
            {
                FrameworkElement element = AdornedElement as FrameworkElement;
                if (element == null)
                    return;

                Canvas.SetLeft(element, Canvas.GetLeft(element) + e.HorizontalChange);
                Canvas.SetTop(element, Canvas.GetTop(element) + e.VerticalChange);
            };
            return thumb;
        }

        private Brush GetMoveEllipseBack()
        {
            string lan = "M841.142857 570.514286c0 168.228571-153.6 336.457143-329.142857 336.457143s-329.142857-153.6-329.142857-336.457143c0-182.857143 153.6-336.457143 329.142857-336.457143v117.028571l277.942857-168.228571L512 0v117.028571c-241.371429 0-438.857143 197.485714-438.857143 453.485715S270.628571 1024 512 1024s438.857143-168.228571 438.857143-453.485714h-109.714286z m0 0";
            var converter = TypeDescriptor.GetConverter(typeof(Geometry));
            var geometry = (Geometry)converter.ConvertFrom(lan);
            TileBrush bsh = new DrawingBrush(new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Black, 2), geometry));
            bsh.Stretch = Stretch.Fill;
            return bsh;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size sz = new Size(10, 50);

            //topLeftThumb.Arrange(new Rect(new Point(-offset, -offset), sz));
            //topMiddleThumb.Arrange(new Rect(new Point(AdornedElement.RenderSize.Width / 2 - THUMB_SIZE / 2, -offset), sz));
            //topRightThumb.Arrange(new Rect(new Point(AdornedElement.RenderSize.Width - offset, -offset), sz));

            //bottomLeftThumb.Arrange(new Rect(new Point(-offset, AdornedElement.RenderSize.Height - offset), sz));
            //bottomMiddleThumb.Arrange(new Rect(new Point(AdornedElement.RenderSize.Width / 2 - THUMB_SIZE / 2, AdornedElement.RenderSize.Height - offset), sz));
            //bottomRightThumb.Arrange(new Rect(new Point(AdornedElement.RenderSize.Width - offset, AdornedElement.RenderSize.Height - offset), sz));

            middleLeftThumb.Arrange(new Rect(new Point(0,0), sz));
            middleRightThumb.Arrange(new Rect(new Point(AdornedElement.RenderSize.Width-10,0), sz));

           // moveThumb.Arrange(new Rect(new Point(AdornedElement.RenderSize.Width / 2 - THUMB_SIZE / 2, -MOVE_OFFSET), sz));

            //thumbRectangle.Arrange(new Rect(new Point(-offset, -offset), new Size(Width = AdornedElement.RenderSize.Width + THUMB_SIZE, Height = AdornedElement.RenderSize.Height + THUMB_SIZE)));

            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return visualCollection[index];
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return visualCollection.Count;
            }
        }

    }
}

