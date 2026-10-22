OLD PROJECT! I might pick this up again t some time, but for now this has been abandoned.


[![banterbrain buddy intro video](https://img.youtube.com/vi/3U-PzTrTVyo/0.jpg)](https://www.youtube.com/watch?v=3U-PzTrTVyo)


### BanterBrain Buddy v1.0.11 bugfix release

**This program requires a valid OpenAI ChatGPT API key to operate _or_ a local Ollama installation.** 

Please report all bugs to the [Discord](https://discord.gg/2EEhKpEMMT) or here on [Github](https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues). 

### Requisites to run
This release depends on .NET Runtime Desktop 8. After installation and trying to run BBB you will be asked to download and install this from the Microsoft website if you do not have it installed yet.

You can also download Runtime Desktop here: [Windows Runtime Desktop 8.0.4](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.4-windows-x64-installer)

### KNOWN ISSUES
- Plugging in/out audio devices while BBB runs can have an unforseen effect on your input/output and throw errors
- No ability to check for Twitch followers
- ElevenLabs API check (preloading voices) can sometimes timeout after 15 seconds. This makes the first time you use it, probably quite a bit slower when editing persona's, but that's only until it works. This cannot really be fixed on my side.

### RELEASE V1.0.11
Fixed:
- Full codebase bug-hunt: startup no longer crashes or hangs on corrupt/missing settings or a first run (fresh install previously froze forever in the constructor)
- Twitch event handlers (cheer, channel point redemption, resub) can no longer spin forever or crash the app when a message is empty or a persona is missing; resubs without a message are acknowledged again
- Three persona dropdowns (subscription/cheering/channel point) saved to the wrong setting, now each stores its own persona
- Access tokens are no longer written to log files
- Chat word filter no longer matches everything when the filter file contains empty entries; missing/locked filter and language files are handled gracefully
- TTS reliability: SSML escaping for Azure/Native (LLM text with & or < broke synthesis), ElevenLabs UI deadlock and audio device leaks fixed, unplugged audio devices fall back to the system default
- OBS websource listener stops cleanly instead of crashing the app; unanswered HTTP requests are now closed
- Empty API keys for OpenAI/Groq show a clear error instead of an SDK crash
- Settings form: no more crashes on empty voice lists, hand-edited personas with missing fields, or a not-yet-existing config folder; hotkey, word filter and custom language forms close safely

### RELEASE V1.0.8 RELEASE
New:
- Completely new settings format so you can now blame me, not Windows if your settings disappear. _The old settings are not yet deleted so reverting to 1.0.7 if you have serious issues will be a fallback_
- Ability to add notable viewers where you can personalize the LLM response for that viewer
- Ability to use the newer local Windows TTS voices
- Button to forcibly reset the current chat history with the LLM (chatgpt/ollama) without restarting BBB

Fixed:
- Ollama restarting a conversation every time you send a message. Now it has a conversation history
- gifted subs fixed see https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/86
- Authorization to Twitch issue fixed
- Timeout on OpenAI API key check to prevent hanging at startup

### RELEASE V1.0.7 RELEASE
New:
- Allow the ElevenLabs 2.5 Turbo API at 50% the cost of the default one https://elevenlabs.io/blog/introducing-turbo-v2-5
- You can now filter bad words in Twitch messages and triggers! If a bad word is found, the message is completely ignored by BBB

<img width="424" alt="image" src="https://github.com/user-attachments/assets/2a66757c-a8d4-4b78-befc-cecad38add54">

### RELEASE V1.0.6 RELEASE
Fixed:
- Not saving Streamer name on exit https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/74
- Not saving LLM Language setting on exit https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/75
- Authorization to twitch failing due to ClientID not being correct in settings file  https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/78

New:
- Allowing fully custom Twitch and intermediary LLM messages.
 **Note:** There is no protection to what you enter or remove there! Be careful!

### RELEASE V1.0.5 RELEASE
- Now supports OpenAI's ChatGPT 4 Omni Mini model. 

<img width="496" alt="image" src="https://github.com/user-attachments/assets/11b0facd-24dc-4d89-bfb0-b5e768803eb5">


### RELEASE V1.0.4 RELEASE
Fixed:
- Check for Windows Native STT recognizer installed or not. Feedback when not https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/73
- Subscriber and Founder badge both recognized as issubscriber in new library. https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/71
- Crash on first install fix

New:
- You can now also select German and Spanish Twitch and LLM intermediary messages

### RELEASE V1.0.3 RELEASE
New:
- shows if there is a version update in the bottom left
- Started working on multilingual Twitch messages (not yet finished)
- Recording button turns red when recording. Main textfield also shows when stopoing recording.
- Small text in hotkey setting to mention the difference between the hotkey (hold to record) and the button (toggle)

Fixed:
- Twitch channel command should not hang on subscriber chat commands

Not yet fixed:
- Native STT non English

Known issue:
- Viewers using their "First" badge while being subsribers are not correctly identified as subscribers

### RELEASE V1.0.2 RELEASE
New:
- Spanish added to OpenAI/Whisper TTS voices
- ElevenLabs now defaults to the higher quality and multilingual v2 API speech model

Note: this does not fix the English used in some prompts that are embedded in BBB, which can generate some response oddities from the used LLM. This will be fixed in a later release.

### RELEASE V1.0.1 RELEASE
New:
- Talking rate for TTS Voices that support it  https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/62
- Talking volume for TTS Voices https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/53
- Talking pitch for TTS Voices that support it
- Version control check at startup

#### Supported TTS Voices features:
**Azure Cognitive Services:** pitch, volume, rate
**Windows native:** pitch, rate, volume
**OpenAI Whisper:** volume, rate
**ElevenLabs:** volume

### RELEASE V1.0.0 RELEASE
Fixed:
- Some small logic errors for Elevenlabs fixed when loading twice in personas
- Some small logic errors fixed when switching personas and saving from dialog window

### RELEASE V0.1.8 BETA
Fixed: 
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/57 now uses a broadcaster account to verify channel issues while a bot account can be used for talking in chat. The bot account is optional.
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/55 now throws an error when there is an audio device selected that's active but unusable with Azure Speech
- https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/58 logfile is now being created in appdata, The same directory where some application settings files are being written to. Also some more error handling incase there's issues writing.

### RELEASE V0.1.7 BETA

Fixed:
- Streamer listbox STT was not cleared when coming back from settings https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/59
- Chat command cooldown timer for Twitch started at the beginning, not at the end of TTS https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/54

Need feedback:
 - Lower delay between notification sound and saying response in TTS https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/61
 - Log file not being created, added extra notification and testing https://github.com/WhiskerWeirdo/BanterBrain-Buddy/issues/58

### RELEASE V0.1.6 BETA
Thank you max aka Dadflaps#1337 for your bug reports and feedback

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

### RELEASE V0.1.4 BETA
- Enabling Ollama in the settings screen while its not running now handled gracefully instead of crashing. Testing Ollama now also handles issues more gracefully instead of crashing.
- 
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
- local based LLM's like Ollama take significant resources; expect to need at least 8 GB memory for a basic model and a solid GPU if you want fast responses


