# Source lines counter for C# projects

A simple tool to get number of [LOCs](http://en.wikipedia.org/wiki/Source_lines_of_code) for Visual Studio solutions and projects.

Specify *.csproj or *.sln file to process and `/l` or `/s` option to calculate lines of code or total source files size. `/ls` will display both values separated with semicolon.

	lcnt /l MyProject.csproj
	lcnt /s MySolution.sln

Calculated data could be appended to the log file.

	lcnt /ls MySolution.sln loc-stat.log
