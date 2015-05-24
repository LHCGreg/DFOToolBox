npk2gif is a command line tool for creating animated GIFs with transparent backgrounds from images in the .NPK files. The .NET Framework 4.5 is required to run npk2gif. There's a good chance you already have it installed. If you get an error box when you try to run it, you can get .NET 4.5 at https://www.microsoft.com/en-us/download/details.aspx?id=30653.

Run

npk2gif -h

to get help.

Example usage:

npk2gif -npk C:\Neople\DFO\ImagePacks2\sprite_monster_impossible_bakal.NPK -img ashcore.img -frames 1-6 -delay 160 -o bakal_walk.gif



npk2gif is open source and is licensed under the Apache License 2.0 (license.txt). You can find the source code at https://github.com/LHCGreg/DFOToolBox.

npk2gif uses the following software:

GraphicsMagick.NET (https://graphicsmagick.codeplex.com/license), licensed under the Apache License 2.0 (graphicsmagick_net_license.txt).

GraphicsMagick (http://www.graphicsmagick.org/index.html), licensed under various licenses (graphicsmagick_licenses.txt).

SharpZipLib (http://icsharpcode.github.io/SharpZipLib/), licensed under the GPL with GNU Classpath exception, permitting distribution of linked binaries without providing source code (sharpziplib_license.txt).

NDesk.Options is copyright NDesk.org and is licensed under the MIT/X11 license (ndesk_options_license.txt).



Thanks to Fiel for the initial reverse engineering work.