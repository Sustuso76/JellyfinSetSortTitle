# JellyfinSetSortTitle

A C# console for editing Jellyfin database data by modifying the "ForcedSortName" and "SortName" fields in the "TypedBaseItems" table.
This utility is very useful for correctly sorting the Jellyfin comic book library.
# Quick Start

#Install 
```c#
https://github.com/Sustuso76/JellyfinSetSortTitle.git
```
Build and Publish for your target (Windows, Linux ....)

# Usage

```c#
dotnet JellyfinSetSortTitle.dll -s <Jellyfin database path> <Comics path> <Comics prefix ForcedSortName> <Start number>
```
example 
```c#
dotnet JellyfinSetSortTitle -s /var/lib/jellyfin/data/library.db /media/comic/Spiderman Spiderman 001
```
# Requirements
The Jellyfin instance must be shut down before running the command.
The files must already be added to the Jellyfin library.
The files must contain the prefix specified in the "<Comics prefix ForcedSortName>" parameter and the number specified in the "<Start number>" parameter in their names.
E.g., Spiderman 001.cbr

## Stop Jellyfin service (Example)
```c#
service jellyfin stop
```

## Switch to jellyfin user (Example)
```c#
su jellyfin
```
Than execute
