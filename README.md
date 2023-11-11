# Rain World Custom Arena DJ
A Rain World mod that allows you to play your own music in Arena mode. Go to the Remix config to see and change the folder where the mod will load custom music from.

It's best that the volume of your music doesn't exceed -8 dB, so that the song doesn't sound too loud compared to the Rain World music.

## Building
This project will reference assemblies from the Rain World install directory. I've set some
fields in the .csproj file which I believe will copy the required references instead of referencing
them directly. As such, you must set an environment variable "RainWorldDir" to the Rain World install directory before
building.

PowerShell (Windows):
```powershell
# if installed through Steam
$env:RainWorldDir = "C:\Program Files (x86)\Steam\steamapps\common\Rain World"
```
Bash (Linux):
```bash
# i don't have steam on linux i just searched this up
export RainWorldDir="~/.steam/root/steamapps/common/Rain World"
```

Then to build the project, run these commands:
```bash
# install cake build tool for this repository
# (only needs to be run once)
dotnet tool restore

# build the project
dotnet cake
```
Once executed, there will be a folder called `out` containing the mod contents,
and a copy of `out` will be put in the Rain World mod folder.

Running `dotnet cake --target=Build` instead will not put the mod into the Rain World mod folder.