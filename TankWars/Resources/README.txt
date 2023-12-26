------------------------------
-------TANK WARS--------------
------------------------------

A game for CS3500

Coded by Daniel Nelson and Saoud Aldowaish

--BASIC MECHANICS--

Tank wars is game where players can join a server and battle each other. 
There are two weapons: a primary weapon for all tanks that deals a third of a tank's health 
and cannot go through tanks or walls, fired by a left mouse click. Also, there is 
a special beam that you can use (after getting a powerup that spawns randomly in the world),
which can go through all walls and players and destroy all tanks instantly, fired with 
a right mouse click. To move you use the WASD keys: ‘W’ to go up, ‘A’ to go left, ‘S’ to 
go down, and ‘D’ to go right.

We also added special protection that forces the player to let go of the right mouse button in order to fire
another beam, preventing the game from firing two beams one frame after another and wasting a player's powerup

--KEY HANDLERS--

For handling the player movements we used a LinkedList to keep track of the pressed keys. When a key is pressed
the View alerts the Controller and if the pressed key is not in the LinkedList we add it to the front of the list and
when a key is released we try to remove it from the list. This allows us to keep track of multiple keys pressed from
the player and smoothly transition between movements prioritizing the most recently pressed key. 

--ANIMATIONS--

For our beam animation a red fading circle is drawn at the end of the turret and a straight red line 
that is drawn across the world. The quickly fade in, then slowly fade out in half a second total. For our tank explosions 
animation we tried generating 15 small explosion particles randomly using C#'s Random class, but for 
some weird reason we couldn't figure out, Random was always returning the same pair of numbers, causing the particles to 
all be drawn on top of each other. To fix this issue we decided to always use the same set of numbers we came up with,
and after a couple of versions we decided to use an explosion animation that sends particles in the
shape of a square starting from the middle of the tank. 


--CONNECTION DETAIL--

Another detail that we added was disabling the connect button and text fields while the client is connecting. We 
thought that it makes more sense for the button and fields to be disabled while we are trying to connect which 
makes the client looks better.The also prevents the user from starting multiple connection attempts and causing 
network errors or other bugs.

--SETTINGS--

Upon starting the server, it will attempt to read various game settings from \Resources\settings.xml
Settings include: {UniverseSize, MSPerFrame, FramesPerShot, RespawnRate, PowerupMode, Wall}

UniverseSize: The length of one side of the game world. (The game world is square)
MSPerFrame: Milliseconds per frame. The server will attempt to keep updates running at this rate.
	Default/suggested is 17 ms, which is about 60 fps
FramesPerShot: The number of frames between when a tank is allowed to fire a standard projectile
	(Beams in default mode have no delay)
RespawnRate: The number of frames after a tank dies before it will automatically respawn in a random location
PowerupMode (Optional): The effect that a powerup should have on a tank when picked up (See EXTRA GAME MODES)
Wall (Optional): The physical walls that should appear in the game world, defined by a <p1> and <p2>
	that each have an <x> and <y>, which are the coordinates of each end of the wall.

--EXTRA GAME MODES--

To choose another game mode you can change the PowerupMode in the settings.xml. 
The powerup options are:

default: The original game mode from the instructions, where each powerup a tank picks up lets it fire a beam that instantly
	kills every tank in its path and goes through walls.
tankSpeedBoost: Upon picking up a powerup, a tank's speed will be 3x faster for 5 seconds. Effect does not stack upon 
	picking up multiple powerups. Effect is reset if the tank dies

--SERVER OPERATION DESCRIPTION--

Upon loading the console application that is the server, it will attempt to automatically load the settings,
start a TcpListener to start accepting new clients on a new Thread, and start the updating loop on the main thread.
The update loop continually updates the world, sends the new world to each client, then removes dead objects
from the map. This is done in that order to make sure that each client recieves a single frame of the 'dead'
representation of an object. 

Anytime a new client connects, a new thread is started for that client to continually accept commands that it sends.
Commands are stored in a Dictionary, and only the command that is registered/processed is the one that was sent last 
before the world starting processing (instead of sending) updates (except 'alt' fire commands. Those have their own 
bool inside the Tank class to make sure they are registered, since they are only sent in a single frame).

