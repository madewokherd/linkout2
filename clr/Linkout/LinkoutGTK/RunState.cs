using System;
namespace LinkoutGTK
{
	public enum RunState
	{
		Nothing, // no file is open
		Stopped, // we would normally be advancing frames but the game is paused
		Running, // we're running the game engine
		Playing // playing back frames of a replay
	}
}

