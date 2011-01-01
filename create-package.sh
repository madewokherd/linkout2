#!/bin/sh

(cd clr/Linkout && xbuild /p:Configuration=Release) || exit 1

rm -rfv bin
mkdir bin

cp clr/Linkout/*/bin/Release/* bin

rm -f Linkout.zip
7z a Linkout.zip bin lgpl-2.1.txt specs tests

