# Slot Machine Game

## Game overview

This is a slot machine game made with Unity. The player clicks on the screen to open the bet menu, chooses an amount, and watches the reels spin. The bet is taken from the wallet before the spin. If every reel stops on the same symbol, the player receives a payout.

The game uses Unity UI and TextMesh Pro. It does not use Unity Button components for the bet menu. `UIPointerHandler` reads the clicks instead.

![Slot Machine Gameplay](Gameplay.gif)

## Project expectations

| Expectation     | Current game                                                                                                                     |
| --------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| Winning logic   | All assigned reels must stop on the same symbol. The game needs at least two reels.                                              |
| Smooth reels    | Reels speed up, spin, slow down, and stop one after another.                                                                     |
| Clear symbols   | Each reel uses the same symbol size and keeps its symbols inside the reel view. The final result is shown in the center row.     |
| Random outcomes | The game uses Unity's `Random` value and each symbol's weight to choose results. A bigger weight gives a symbol a bigger chance. |
| Payouts         | A matching result pays the bet multiplied by that symbol's payout multiplier.                                                    |
| Bonus features  | There are no special bonus rounds yet. A symbol can be added in the config, but its special behavior needs new game code.        |

This RNG is suitable for a local game. It is not a secure or audited system for real-money gambling.

## Open and play the project

1. Open this project folder with Unity Hub.
2. Use the Unity version written in `ProjectSettings/ProjectVersion.txt`.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press **Play**.

The sample scene already has the main game objects and asset references. If you make a new scene, follow the setup steps below.

## How to play

1. Start the game in Unity by pressing **Play**, or open the WebGL build in your browser.
2. Check your money in the wallet text at the top-right of the screen.
3. Right-click anywhere on the game screen to open the bet menu.
4. Choose one of the bet amounts in the menu. The amount you choose is taken from your wallet.
5. The lever moves and the reels start spinning. Wait for all reels to stop.
6. If the center symbol on every reel is the same, you win. The payout is added to your wallet and a win message appears.
7. If the center symbols do not all match, you do not win and the bet is not returned.
8. When the spin is finished, right-click again to open the bet menu and play another round.

If your wallet has less money than the bet you chose, the game shows a “Not enough money” message and does not start the spin. The bet menu stays open so you can try a smaller amount.

## How the scripts work together

The C# scripts are in `Assets/Scripts` and use two namespaces:

- `SlotMachine.Core` contains the game rules, spin controller, and wallet manager.
- `SlotMachine.UI` contains the reels, popups, notifications, and pointer input.

The files are also grouped into `Core` and `UI` folders. A script's Unity GUID is kept in its `.meta` file. Do not delete or replace that file when moving a script, or Unity may lose links to the script in scenes and assets.

| Script                                 | What it does                                                                                       |
| -------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `Core/SlotMachineGameManager.cs`       | Opens the bet menu, checks bets, updates the wallet, shows win or loss messages, and plays sounds. |
| `Core/Lever.cs`                        | Shows the lever animation when a spin starts. The lever itself is not clicked.                     |
| `Core/SlotGameController.cs`           | Chooses one result for each assigned reel and manages the spin start and finish.                   |
| `UI/SlotReel.cs`                       | Builds the symbols for one reel, moves them, and shows that reel's final result.                   |
| `Core/SlotMachineConfig.cs`            | Stores the symbols, reel movement settings, and wallet and bet settings.                           |
| `UI/SlotMachinePresentationConfig.cs`  | Stores the UI prefabs, sounds, music, colors, and message time.                                    |
| `UI/BettingPopupController.cs`         | Builds the bet choices, reacts to pointer actions, and keeps the popup inside the screen.          |
| `UI/FloatingNotificationController.cs` | Shows and animates short messages such as a win or a low wallet balance.                           |
| `UI/ScreenSpaceUIUtility.cs`           | Fits UI inside the safe screen area and keeps it there when it moves.                              |
| `UI/UIPointerHandler.cs`               | Sends pointer events to scripts that need to react to clicks, presses, or hover.                   |

Gameplay scripts use the `SlotMachine.Core` namespace and are in the `Core` folder. Reel and screen scripts use the `SlotMachine.UI` namespace and are in the `UI` folder. The two config assets keep gameplay rules separate from UI assets.

## Set up a scene

### 1. Add the main UI parts

For a new scene, create a Canvas and make sure it has a **Graphic Raycaster**. Add an **EventSystem** to the scene too. The EventSystem lets Unity send pointer actions to UI objects.

Add a TextMesh Pro text object for the wallet. Put it somewhere visible, such as the top-right corner.

### 2. Set up the reels

Create one UI object for each reel. Each reel object needs a `RectTransform`, a `RectMask2D`, and the `SlotReel` component. The `SlotReel` script adds a `RectMask2D` if one is missing. Arrange the reel objects side by side and size them to fit your machine art.

Add a `SlotGameController` to a scene object. In its Inspector:

1. Assign the `SlotMachineConfig` asset to **Config**.
2. Add every reel object to the **Reels** list.
3. Assign the lever object to **Lever** if the scene has one. The lever reference is optional.

The assigned **Reels** list decides which reels spin. To add another reel, make another reel UI object and add it to this list. The list is the only place that decides how many reels spin. You do not need to edit the C# code. Make sure the machine art has room for the extra reel.

### 3. Create the gameplay config

The sample config is `Assets/Scripts/SlotMachineConfig.asset`. Its script is `Assets/Scripts/Core/SlotMachineConfig.cs`. To make another config asset, use **Assets > Create > Slot Machine > Configuration**.

In the config Inspector:

- **Symbols**: Add one entry for every symbol. Give each symbol a unique ID, a name, an image, a weight above zero, and a payout multiplier greater than 1 if a win should add money beyond the original bet.
- **Reel settings**: Set the visible rows, symbol height, speed, spin time, and stop delay. The symbol height should fit the reel view.
- **Betting**: Set the starting wallet, smallest bet, largest bet, and the gap between bet choices.

To add a symbol, add an entry to **Symbols**, give it a new ID, and assign its image and values. The reels use this list when they show symbols and choose spin results. You do not need to edit the C# code to add a symbol.

### 4. Set up the bet menu and messages

The project includes these prefabs:

- `Assets/Prefab/BettingOptions.prefab` is the bet menu.
- `Assets/Prefab/BetOption.prefab` is one bet choice inside the menu.
- `Assets/Prefab/FloatingOptions.prefab` is a short message, such as a win or loss.

The bet menu prefab uses `BettingPopupController`. Its **Popup Root**, **Popup Group**, and **Option Container** need to point to the correct objects inside the prefab. The title text is optional.

Each bet option prefab needs TextMesh Pro text and `UIPointerHandler`. A Unity Button component is not needed. Make sure the option's UI graphic can receive raycasts.

The message prefab needs TextMesh Pro text. The notification script can find its text, image, and Canvas Group in its children, or you can assign them in the Inspector.

### 5. Set up sounds, music, and the wallet

The sample presentation config is `Assets/Scripts/SlotMachinePresentation.asset`. To make another one, use **Assets > Create > Slot Machine > Presentation**.

Assign these fields on the presentation asset:

- **Betting Popup Prefab**, **Bet Option Prefab**, and **Notification Prefab**: the prefabs described above.
- **Lever Pull**, **Reel Spin**, **Button Click**, and **Button Hover**: the sounds for those actions.
- **Popup Open**, **Lose**, **Win**, and **Jackpot**: the matching menu or result sounds.
- **Reel Stop**: this field is available, but the current game scripts do not play it yet.
- **Background Music**: the music clip. The game loops this clip while the game manager is enabled.
- **Background Music Volume**: music volume from 0 to 1.
- **Currency Symbol**, colors, and **Notification Duration**: how the wallet and messages look.

Add `SlotMachineGameManager` to a scene object. Assign its **Game** to the `SlotGameController`, **Presentation** to the presentation asset, and **Wallet Text** to the wallet TMP text.

Sound clips are optional. If a sound field is empty, that sound will not play. The project has example audio files in `Assets/Sounds`; choose and assign the clips you want in the Inspector.

## How wins and bets work

The bet choices are made from the minimum, maximum, and step values in the config. The chosen bet is removed from the wallet before the reels spin.

At least two reels must be assigned, and every reel's center symbol must match for a win. A spin result is picked separately for each reel, using the symbol weights in the config. The payout is:

`bet amount x matching symbol payout multiplier`

The payout is added to the wallet after the reels stop. A different symbol on any reel means there is no payout.

## Adding more content

The project is set up so common changes can be made in Unity without editing the scripts:

- **Add reels**: Create more UI reel objects and add them to the `SlotGameController` **Reels** list.
- **Add symbols**: Add more entries to the **Symbols** list in `SlotMachineConfig`.
- **Change bet values**: Edit the betting settings in `SlotMachineConfig`.
- **Change reel speed and timing**: Edit the reel settings in `SlotMachineConfig`.
- **Change UI art, sounds, and music**: Assign different prefabs and clips in `SlotMachinePresentationConfig`.

Adding more reels or symbols does not require changing the spin code. Add the reel object to the controller list, or add the symbol to the config list. This lets the game grow through the Unity Inspector.

The current win rule is intentionally simple: all reels must match. A new kind of payout, such as a line win, a scatter symbol, or a bonus round, needs new game rules in code. Adding a symbol to the config does not add special behavior by itself.

## Build for WebGL

1. Open **File > Build Profiles** (or **File > Build Settings**, depending on the Unity version).
2. Select **WebGL** and choose **Switch Platform** if Unity asks.
3. Make sure `Assets/Scenes/SampleScene.unity` is included in the build scenes.
4. Press **Build** and choose an output folder.
5. To test the build, serve the output folder from a local web server. Opening `index.html` directly may not work because of browser security rules.

If WebGL is not available in the platform list, install WebGL Build Support for the project's Unity version using Unity Hub.

## My approach

I wanted the game rules and the screen parts to be easy to find. The config keeps the symbol and bet settings together. Another config keeps the prefabs, sounds, and colors together. The controller handles the reels, and the game manager handles the wallet and bet menu.

This also makes it easier for me to add more symbols or reels in the Inspector without changing the spin code. If I add a new type of win later, I will need to add that rule in code, but I can still use the same symbol and presentation setup.
