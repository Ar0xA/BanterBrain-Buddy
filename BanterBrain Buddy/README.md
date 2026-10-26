### v1.0.0 release of BanterBrain Buddy.
Download: https://github.com/WhiskerWeirdo/BanterBrain-Buddy/releases/tag/0.1.4-beta

**This application requires a valid API key (OpenAI ChatGPT, Groq, Google AI Gemini) to operate _or_ a local Ollama installation.** 

Please report all bugs to the discord or here on github. 

### Requisites to run
This release depends on .NET Runtime Desktop 8. After installation and trying to run BBB you will be asked to download and install this from the Microsoft website if you do not have it installed yet.

You can also download Runtime Desktop here: [Windows Runtime Desktop 8.0.4](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.4-windows-x64-installer)

### KNOWN ISSUES
- Plugging in/out audio devices while BBB runs can have an unforseen effect on your input/output and throw errors
- No ability to check for Twitch followers
- ElevenLabs API check (preloading voices) can sometimes timeout after 15 seconds. This makes the first time you use it, probably quite a bit slower when editing persona's, but that's only until it works. This cannot really be fixed on my side.

### RELEASE V1.0.0
fixed:
- small bugfix for elevenlabs Persona error.

### RELEASE V0.1.6 BETA
Thank you @max aka Dadflaps#1337 for your bug reports and feedback

fixed:
- Save dialog when you edit the default persona should not popup now all the time

not fixed:
- Added additional logging to eventsub subscription errors.

### RELEASE V0.1.5 BETA
Thank you @max aka Dadflaps#1337 for your bug reports and feedback

fixed: 
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/50 by adding a small delay and a check for ratelimits
- (hopefully) Azure voice quality should be improved

Added:
- Ability to post the chat command cooldown being over in Twitch chat.
### RELEASE V0.1.4 BETA
Thank you @max aka Dadflaps#1337 for your bug reports and feedback

fixed:
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/45 fixed; due to logic error whenn the authorized API key and monitoring channel were different people
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/46 fixed; in combination with autostart checked, after the settings window Twitch would disconnect
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/49 fixed; username of broadcaster was not used in streamer/local STT recordings
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/47 fixed; setting was not saved when opening Settings panel.

Improvement:
- Switching windows now saves settings more often to prevent setting-data loss
- Main window now calls "Persona"  "Streamer Persona" to make it more obvious that's only used for the Speech-To-Text/streamer.

 
### RELEASE V0.1.3 BETA
- Fixed https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/47
- added more times to save settings to make it less likely to miss something when switching screens
- Renamed main window STT and Persona to show that its specifically for the streamer

### RELEASE V0.1.3 BETA
- Enabling Ollama in the settings screen while its not running now handled gracefully instead of crashing. Testing Ollama now also handles issues more gracefully instead of crashing.

### RELEASE V0.1.2 BETA
- Elevenlabs speed optimization. It only needs to load the voices when you are busy editing persona's that use Elevenlabs and not in other events.

### RELEASE V0.1.1 BETA
- Fixes and validations for invalid or empty API keys so that the program does not crash. It fails gracefully and tells you the key is invalid.

### RELEASE V0.1.0 BETA
- many fixes for text input issues, like saving on the correct time or not allowing empty fields
- Twitch OAUTH fix to allow configurable redirect if default port is occupied
- Elevenlabs optimizations
- Fixes to PTT button
- Fixes to Hotkeys
- Added timestamps to Main text window 
- many code hygiene fixes
- Ollama roletext fix & now Ollama supports and remembers previous content
- Optimization of API verifications 
- added more logging to help debugging
- Added a HELP setting that opens the logfile directory
- New Elevenlabs library

## License
This project uses the GNU General Public License v3.0 as you can read [here](./LICENSE.txt). The TLDR is:

>You may copy, distribute and modify the software as long as you track changes/dates in source files. Any modifications to or software including (via compiler) GPL-licensed code must also be made available under the GPL along with build & install instructions.

## About
BanterBrain Buddy is a Windows .Net based Speech-To-Text to LLM to Text-To-Speech program for general entertainment or as a streaming companion.

The goal is to provide local PC or streaming entertainment by talking to an AI and hearing the responses back, based on a role you can set for the AI. The goa is to support both local only-resources or API services.
For the streaming side of things, integration with Twitch to respond to chat commands and stream events is planned.

Example: https://www.youtube.com/watch?v=TawapT1WEEo

## Social
Come talk or chat at https://discord.banterbrain.tv

## Build instructions
[todo]
tldr: use Visual studio 2022 with .net 8. 

## Resources for local and API
- for most API-services you need a paid account using a creditcard! (OpenAI, Azure, Google, Deepgram, etc.)
- local based LLM's like GPT4All take significant resources; expect to need at least 8 GB memory for a basic model and a solid GPU if you want fast responses

## Roadmap


### Short term roadmap
- make beta stable

