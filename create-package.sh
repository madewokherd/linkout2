#!/bin/sh

(cd clr/Linkout && xbuild /p:Configuration=Release) || exit 1

# FIXME: This is needed to include /platform:x86 because of a bug in xbuild
(cd clr/Linkout/LinkoutGTK && gmcs /platform:x86 /noconfig /debug- /optimize- /out:obj/Release/LinkoutGTK.exe /resource:obj/Release/LinkoutGTK.gtk-gui.gui.stetic,gui.stetic gtk-gui/generated.cs MainWindow.cs Main.cs AssemblyInfo.cs Utils.cs gtk-gui/LinkoutGTK.MainWindow.cs RunState.cs /target:winexe /reference:/usr/lib/mono/2.0/System.dll /reference:/usr/lib/cli/gtk-sharp-2.0/gtk-sharp.dll /reference:/usr/lib/cli/gdk-sharp-2.0/gdk-sharp.dll /reference:/usr/lib/cli/glib-sharp-2.0/glib-sharp.dll /reference:/usr/lib/cli/glade-sharp-2.0/glade-sharp.dll /reference:/usr/lib/cli/pango-sharp-2.0/pango-sharp.dll /reference:/usr/lib/cli/atk-sharp-2.0/atk-sharp.dll /reference:/usr/lib/mono/2.0/Mono.Posix.dll /reference:/usr/lib/cli/gtk-dotnet-2.0/gtk-dotnet.dll /reference:/usr/lib/mono/2.0/System.Drawing.dll /reference:/home/meh/source/linkout2/clr/Linkout/Linkout/bin/Release//Linkout.dll /reference:/home/meh/source/linkout2/clr/Linkout/LinkoutDrawing/bin/Release//LinkoutDrawing.dll /reference:/usr/lib/mono/2.0/mscorlib.dll /warn:4) || exit 1

rm -rfv bin
mkdir bin

cp clr/Linkout/*/bin/Release/* bin

# FIXME: This is needed to include /platform:x86 because of a bug in xbuild
cp clr/Linkout/LinkoutGTK/obj/Release/LinkoutGTK.exe bin

(git describe 2>/dev/null || git rev-parse HEAD) > REVISION

rm -f Linkout.zip
7z a Linkout.zip bin lgpl-2.1.txt specs tests prototypes REVISION

