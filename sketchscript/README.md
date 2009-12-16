SketchScript
============
An animation and visualization playground for Ruby

Left column: the "canvas"
-------------------------
The canvas is where you make art. It is an [actual canvas](http://msdn.microsoft.com/en-us/library/system.windows.controls.canvas\(VS.100\).aspx) object, so things can be added to it, mouse-clicks captured, etc.

Right column: the "editor"
--------------------------
The editor lets you manage the code running your animations. The Interactive tab lets you actually run the entire buffer by pressing Ctrl-C. You can run less than that by selecting the code you want to run and also pressing Ctrl-C.
When the code runs, the result will be shown in the output window below the editor. Also, the History tab shows you a record of all the code that has run in your session, so you easily recreate the state.
Also, the editor and output window are all resizable, so if you can't see a part of the UI, try dragging things around.

Examples
========
A bunch of examples are in the "features" directory.

TODO
====
- Replace editor box with ReplLib or HawkCodeBox for syntax coloring
- Python support (maybe a %<language> command?)
- Need a real repl along with the smart editor
- Get running in Silverlight as a replacement for DLRConsole
