# Slot Machine Game

## Game Overview

This project is a simple slot machine game made in Unity. The main idea was to make something fun, easy to understand, and visually clean without making the code too messy.

The player can pull a lever, spin the reels, and wait for the result. Each reel has its own symbols, and the game picks symbols using different weights so some symbols appear more often than others. I also added a simple setup where the reel animations feel smooth and stop in a staggered order instead of all at once.

I kept the project focused on a small but solid gameplay loop:

- choose a slot machine setup
- pull the lever
- reels spin and stop
- results appear on the screen

It is not a huge casino game, but it has a simple wallet, betting, and payout flow. It can still grow later with more win rules, UI updates, and game modes.

## Unity Setup

The settings are split into two assets so game rules and visual assets stay separate:

- `SlotMachineConfig` holds symbols, reel settings, starting money, and bet limits.
- `SlotMachinePresentationConfig` holds the popup, bet option, and notification prefabs, plus sound clips, looping background music, and display colors.

The `SlotGameController` is the only place that needs the `SlotMachineConfig`. It passes those settings to its reels, so each reel does not need another copy of the same config.

To connect the wallet and UI in a scene:

1. Create a presentation asset from **Create > Slot Machine > Presentation**.
2. Add the popup, bet option, and notification prefabs to that asset. Each bet option prefab needs a TMP text and `UIPointerHandler`; it does not need a Unity Button.
3. Add `SlotMachineGameManager` to the scene.
4. Assign its `Game` field to the scene's `SlotGameController`, its `Presentation` field to the presentation asset, and its `Wallet Text` field to the money TMP text.
5. Add the sound clips and background music to the presentation asset. Background music loops while the game manager is enabled; its volume can be changed in the same asset.

Bet choices come from the minimum, maximum, and step values in `SlotMachineConfig`. All reels must show the same symbol to win. The payout uses that symbol's multiplier times the bet.

## Instructions to Run WebGL Build

To run this project as a WebGL build, follow the steps below.

1. Open the project in Unity.
2. Make sure the platform is set to WebGL.
   - Go to File > Build Settings
   - Select WebGL
   - Click Switch Platform
3. If Unity asks to install WebGL support, install it first.
4. After switching platform, click Build.
5. Choose a folder where you want the build output to be saved.
6. Unity will generate the WebGL files in that folder.
7. Open the generated index.html in a browser, or host the folder using a local web server if needed.

If you want to test it locally in a browser, I usually recommend hosting the build folder through a simple local server instead of opening the HTML file directly. Sometimes browser security can block things when opening files from disk.

A simple option is to run a local server from the build folder with Python or any web server tool you already use.

## Bonus Features

This project already includes a few small things that make it feel a bit more complete:

- weighted symbol chances so some symbols appear more often than others
- reel stagger timing so the reels do not all stop at the exact same moment
- a wallet that spends the chosen bet and adds winning payouts
- win and low-money messages, plus a small wallet animation
- clicking anywhere opens the bet menu at the pointer, and the menu stays inside the screen safe area
- the lever is an animation only; choosing a bet starts the lever and reel spin
- sound clips and UI prefabs grouped in a separate presentation config
- gameplay settings in one config, shared with all reels
- the lever moves after the player chooses a bet; clicking the screen opens the bet menu
- smooth reel animation and symbol pool reuse

There are no advanced pay lines or large bonus rounds yet, but the project already has a base for adding them later.

## My Thought Process / Approach

I wanted to build this in a way that felt easy to work with and easy to expand. I did not want to make the game too complicated at the start, because that usually creates messy code and makes it hard to fix problems later.

The main idea was to separate the game into small parts:

- one config holds the slot-machine rules, while another holds UI and sound assets
- each reel handles its own animation
- the game controller manages the reels and spin flow
- the game manager handles the wallet, bets, and result messages
- the lever handles player input
- pointer logic handles UI clicks

This makes the code easier to read and easier to change later. I also wanted the slot machine to feel more natural, so I added things like staggered stopping and weighted symbol selection instead of making everything random in a rough way.

I also tried to keep the structure clean enough that if I wanted to add more features later, I would not need to rewrite the whole project. That was the main goal for me: a simple slot machine that works well, is readable, and can grow without becoming a mess.

If I had to describe my approach in one sentence, it would be: build the core loop first, keep the code organized, and add polish only where it improves the game experience.
