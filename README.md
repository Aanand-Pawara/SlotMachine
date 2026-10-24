# Slot Machine Game

## Game Overview

This project is a simple slot machine game made in Unity. The main idea was to make something fun, easy to understand, and visually clean without making the code too messy.

The player can pull a lever, spin the reels, and wait for the result. Each reel has its own symbols, and the game picks symbols using different weights so some symbols appear more often than others. I also added a simple setup where the reel animations feel smooth and stop in a staggered order instead of all at once.

I kept the project focused on a small but solid gameplay loop:

- choose a slot machine setup
- pull the lever
- reels spin and stop
- results appear on the screen

It is not a huge casino game, but it is a clean base that can grow later with win checks, sound effects, score systems, UI upgrades, and more game modes.

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
- sound references for lever pull, reel spin, stop, win, and jackpot
- config-based setup, so the game values can be changed without rewriting the logic
- click support on the lever through a pointer handler
- smooth reel animation and symbol pool reuse

There are no huge bonus systems like a full money system, advanced win lines, or complex GUI yet, but the project already has a good base for those features later.

## My Thought Process / Approach

I wanted to build this in a way that felt easy to work with and easy to expand. I did not want to make the game too complicated at the start, because that usually creates messy code and makes it hard to fix problems later.

The main idea was to separate the game into small parts:

- configuration holds the settings
- each reel handles its own animation
- the controller manages the overall spin flow
- the lever handles player input
- pointer logic handles UI clicks

This makes the code easier to read and easier to change later. I also wanted the slot machine to feel more natural, so I added things like staggered stopping and weighted symbol selection instead of making everything random in a rough way.

I also tried to keep the structure clean enough that if I wanted to add more features later, I would not need to rewrite the whole project. That was the main goal for me: a simple slot machine that works well, is readable, and can grow without becoming a mess.

If I had to describe my approach in one sentence, it would be: build the core loop first, keep the code organized, and add polish only where it improves the game experience.
