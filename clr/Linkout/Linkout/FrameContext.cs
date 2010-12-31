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

