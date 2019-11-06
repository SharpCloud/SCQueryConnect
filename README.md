# SCQueryConnect

Simple windows UI to help test SQLBatch (https://github/SharpCloud/SQLBatch).

Allows user to connect to a database, run q simple query, update a selected story and produce the SQLBatch and associated config files.

## Building Query Connect

Query Connect can be built from within Visual Studio, or from the command line.
A simple batch file `build.bat` is included in the project root; MSBuild needs
to be in your environment path in order for it to work correctly. Running it
will use MSBuild to build the project, and then copy the output into a newly
created `_build_output` folder in the project root.
