/*
 * Copyright 2010, 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Linkout;
using Linkout.Lisp;

namespace LinkoutDrawing
{
	public class Drawing
	{
		public Drawing ()
		{
		}
		
		public virtual void DrawBackground(Graphics graphics, Frame frame, RectangleF game_region)
		{
			Brush black_fill = new SolidBrush(Color.Black);
			graphics.FillRectangle(black_fill, game_region);
		}

		public virtual void DrawObject(Graphics graphics, Frame frame, GameObject obj, RectangleF game_region)
		{
			if (obj.GetType() == typeof(Box))
			{
				Rectangle box_location;
				Brush box_fill;
				
				try
				{
					box_location = new Rectangle((int)(obj.getattr(new StringAtom("x")).get_fixedpoint() >> 16),
					                             (int)(obj.getattr(new StringAtom("y")).get_fixedpoint() >> 16),
					                             (int)(obj.getattr(new StringAtom("width")).get_fixedpoint() >> 16),
					                             (int)(obj.getattr(new StringAtom("height")).get_fixedpoint() >> 16));
				}
				catch (NotSupportedException)
				{
					// Attribute missing or not a number
					return;
				}
				
				if (box_location.IsEmpty)
					return;
				
				box_fill = new SolidBrush(Color.White);
				
				graphics.FillRectangle(box_fill, box_location);
			}
		}
		
		public virtual void DrawFrame(Graphics graphics, Frame frame, RectangleF game_region)
		{
			DrawBackground(graphics, frame, game_region);
			
			foreach (GameObject obj in frame)
			{
				DrawObject(graphics, frame, obj, game_region);
			}
		}
		
		public virtual void DrawFrame(Graphics graphics, Frame frame, RectangleF game_region, RectangleF output_region)
		{
			GraphicsState prev_state;
			prev_state = graphics.Save();
			
			try
			{
				Matrix transform_matrix;
				PointF[] output_region_points = new PointF[3];
				
				output_region_points[0] = new PointF(output_region.Left, output_region.Top);
				output_region_points[1] = new PointF(output_region.Right, output_region.Top);
				output_region_points[2] = new PointF(output_region.Left, output_region.Bottom);
				
				transform_matrix = new Matrix(game_region, output_region_points);
				
				graphics.MultiplyTransform(transform_matrix);
				
				graphics.IntersectClip(game_region);
				
				DrawFrame(graphics, frame, game_region);
			}
			finally
			{
				graphics.Restore(prev_state);
			}
		}
	}
}

