# HOMP - Highly Optimized Music Player  

![A screenshot of HOMP's playlists page](Assets/Screenshots/homp_playlists.png)  

HOMP is a simple lightweight music player.  
Some of its features include:
- Support for global shortcuts without media keys
- Lyrics support and built-in lyrics editor
- Easy to use user interface
- Automatic album and artist organization
- Ultra fast search for each supported song attribute
- Miniplayer window with playback controls  

## Global shortcuts  

HOMP was originally created with the goal of creating a music player completely controllable by non-media keys shortcuts that was lightweight enough to be used while doing other heavy tasks.  
The supported shortcuts are:  
- `CTRL-ALT-SHIFT-P`: Play/Pause  
- `CTRL-ALT-SHIFT-N`: Next song in collection  
- `CTRL-ALT-SHIFT-B`: Previous song in collection  
- `CTRL-ALT-SHIFT-0`: Go back to the beginning of the current song  
- `CTRL-ALT-SHIFT-R`: Toggle Repeat  
- `CTRL-ALT-SHIFT-S`: Toggle Shuffle  
- `CTRL-ALT-SHIFT-UP`: Volume up by 2% (the amount will be customizable)  
- `CTRL-ALT-SHIFT-DOWN`: Volume down by 2% (the amount will be customizable)  

These shortcuts work even with HOMP running in the background. All shortcut keys are customizable (only the actual activator key, modifiers are fixed to `CTRL-ALT-SHIFT`).  

## Quirks  

HOMP has some quirks in how it works compared to other music players.  

**1. Shuffle and Repeat**  
Shuffle works on the current song collection (all songs, album, playlist...) and the collection is always on loop.  
This means that while shuffle is turned on, all songs in the collection get played at least once, without repetitions, but the collection itself is looped, meaning that once every song has been played, it starts over from the beginning again.  
Turning off shuffle just makes the playback stop once the current song ends.  

Repeat on the other hand only affects single songs. This makes the single song loop, even when played from a collection. You can have both Shuffle and Repeat turned on, but repeat has priority over shuffle.  

**2. Determining the current collection**  
The current collection indicates to HOMP which album/playlist/artist to use as source for Shuffle/Next song.  
HOMP follows a single, easy rule to determine which collection has to be used: the current collection is the one where playback is started from.  
This means that:  
- Playing a song from 'All Songs' -> All loaded songs as source  
- Playing a song from 'Playlists' (or playing a playlist directly) -> That playlist as source  
- Playing a song from 'Albums' (or playing an album directly) -> That album as source  
- Playing a song from 'Artists' (or playing an artist directly) -> That artist as source  

A special case is the 'Search results' page, where HOMP respects the 'Use search results as collection source' option.  
If this option is active and playback is started from a song in the search results, then all of the songs in the search results are used as source.  

**3. Play as next song**  
HOMP does not support manual queue management and the only option to force a song to get played after the current one is to right-click on it and click 'Play as next song'.  
This only plays that single song and does not change the collection source.  
This means that when the song that was forced to be played ends, HOMP returns to standard behaviour and resumes playing songs from the collection that was playing before.  

## Running HOMP  

HOMP uses WPF and .NET 8.0.  
This makes HOMP a Windows only program and there are no plans to port it to other platforms.  

The project is divided into 2 sub-projects:  
- `CustomMediaPlayerUltimate`: this is the main project, the actual HOMP project  
- `WPFHotkeys`: this is a legacy auxiliary project, which is not needed anymore but is kept for backup reasons.  

The project has a Visual Studio Solution File that you can use to build and edit the project using Visual Studio 2022.  

You can also run `dotnet build` or `dotnet run` in CustomMediaPlayerUltimate/CustomMediaPlayerUltimate.
