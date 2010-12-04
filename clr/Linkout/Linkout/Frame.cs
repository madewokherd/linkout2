using System;
using System.Collections.Generic;
using Linkout.Lisp;
namespace Linkout
{
	public class Frame : Interpreter
	{
		public Frame ()
		{
			priv_committed = false;
			priv_next_id = 0;
			priv_frame_number = 0;
			objects = new SortedDictionary<long, GameObject>();
		}

		private bool priv_committed;

		private long priv_next_id;
		private int priv_frame_number;
		
		private SortedDictionary<long, GameObject> objects;
		
		public void add_object(GameObject obj)
		{
			if (obj.id < priv_next_id)
			{
				obj.id = priv_next_id;
			}
			priv_next_id = obj.id + 1;
			
			objects[obj.id] = obj;
		}
	}
}

