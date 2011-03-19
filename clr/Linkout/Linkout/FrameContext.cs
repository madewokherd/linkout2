/*
 * Copyright 2010, 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Linkout is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *  
 * You should have received a copy of the GNU General Public License
 * along with Linkout.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using Linkout.Lisp;
namespace Linkout
{
	public class FrameContext : Context
	{
		public FrameContext ()
		{
		}
		
		public Frame frame;

		public override void CopyToContext(Context result)
		{
			FrameContext frame_result = (FrameContext)result;

			base.CopyToContext(result);
			frame_result.frame = frame;
		}
		
		public override Context Copy()
		{
			Context result = new FrameContext();
			
			CopyToContext(result);
			
			return result;
		}
	}
}

