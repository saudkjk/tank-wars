using System;
using System.Xml;
using World;
using TankWars;
using System.Net.Sockets;
using NetworkUtil;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GameController;
using System.Text;

// The controller part of the server for the TankWars game
// As part of PS9 for CS3500
// Authors: Daniel Nelson and Saoud Aldowaish
// Date: 4/22/2021

namespace Server
{
    /// <summary>
    /// The Controller part of MVC for the server. Holds open a network connection for 
    ///  clients to connect to, and after the handshake has been fulfilled, repeatedly sends
    ///  world info to each client
    /// Loads the layout of the world from settings.xml in the Resources folder
    /// Tries to update the world every frame, as defined by MSPerFrame in the settings
    /// </summary>
    class ServerController
    {
        private Map map;
        private TcpListener listener;
        private Dictionary<int, SocketState> clients; // a Dictionary for all clients connected to the server
        private Dictionary<int, ControlCommand> latestCommands; // a Dictionary for latest command from a giving client

        //various delays loaded from settings.xml
        private int frameDelay;
        private int shotDelay;
        private int respawnDelay;
        private int projectileSpeed = 25;
        private string powerupMode = "default";

        //various parameters controlling how many and how often powerups show up
        private int powerupCooldown = 0;
        private int powerupMaxDelay = 1650;
        private int powerupMaxCount = 2;
        private int powerupCurrentCount = 0;

        //various parameters for the Tank speed boost powerup mode
        private int tankPowerupMultiplier = 3;
        private int tankPowerupLengthOfTime = 300;

        /// <summary>
        /// The entry point for the program. Starts a new server and keeps the console from closing.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ServerController sc = new ServerController();
            Console.Read();
        }

        /// <summary>
        /// Default constructor. Reads the settings file, instantiates the needed objects,
        /// and starts the two loops for accepting new clients and updating the world.
        /// </summary>
        public ServerController()
        {
            //read in settings.xml
            LoadSettings();

            clients = new Dictionary<int, SocketState>();
            latestCommands = new Dictionary<int, ControlCommand>();
            //start new connection event loop
            listener = Networking.StartServer(OnNewConnection, 11000);
            Console.WriteLine("Accepting clients...");

            //start update sending event loop
            RunUpdateLoop();
        }

        /// <summary>
        /// Loads game settings from settings.xml in the Resources folder.
        /// Settings include : {UniverseSize, MSPerFrame, FramesPerShot, RespawnRate, PowerupMode, Walls}
        /// </summary>
        private void LoadSettings()
        {
            map = new Map();
            try
            {
                using (XmlReader reader = XmlReader.Create("..\\..\\..\\..\\Resources\\settings.xml"))
                {
                    reader.Read();
                    if (reader.Name == "GameSettings")
                    {
                        while (reader.Read())
                        {
                            if (reader.IsStartElement())
                            {
                                switch (reader.Name /*XML type name*/)
                                {
                                    case "UniverseSize":
                                        map.Size = reader.ReadElementContentAsInt();
                                        break;
                                    case "MSPerFrame":
                                        frameDelay = reader.ReadElementContentAsInt();
                                        break;
                                    case "FramesPerShot":
                                        shotDelay = reader.ReadElementContentAsInt();
                                        break;
                                    case "RespawnRate":
                                        respawnDelay = reader.ReadElementContentAsInt();
                                        break;
                                    case "PowerupMode":
                                        powerupMode = reader.ReadElementContentAsString();
                                        break;
                                    case "Wall":
                                        reader.ReadToDescendant("p1");
                                        reader.ReadToDescendant("x");
                                        int x1 = reader.ReadElementContentAsInt();
                                        int y1 = reader.ReadElementContentAsInt();
                                        reader.ReadEndElement();

                                        reader.ReadToFollowing("p2");
                                        reader.ReadToDescendant("x");
                                        int x2 = reader.ReadElementContentAsInt();
                                        int y2 = reader.ReadElementContentAsInt();
                                        reader.ReadEndElement();

                                        map.AddWall(new Wall(Wall.NextID, new Vector2D(x1, y1), new Vector2D(x2, y2)));
                                        break;
                                }
                            }
                        }
                    }
                    else
                        throw new Exception("settings.xml does not appear to be the right format");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("There was a problem reading settings.xml");
                throw e;
            }

        }

        /// <summary>
        /// Networking Callback method for a new connection. Sets OnNewClient as 
        ///  the next callback method and waits for the new client to send the playername
        /// </summary>
        /// <param name="newClient">The SocketState representing the new connection</param>
        private void OnNewConnection(SocketState newClient)
        {
            if (newClient.ErrorOccurred)
            {
                Console.WriteLine("There was an error connecting a client: " + newClient.ErrorMessage);
                return;
            }

            newClient.OnNetworkAction = OnNewClient;
            Networking.GetData(newClient);
        }

        /// <summary>
        /// Networking callback for when a new client sends the playername.
        /// Completes the handshake by sending the new player id, the world size,
        /// and the walls. Sets the next callback to OnReceiveCommand and
        /// adds the client to the collection that updates are sent to.
        /// </summary>
        /// <param name="client">The SocketState representing the new connection</param>
        private void OnNewClient(SocketState client)
        {
            if (client.ErrorOccurred)
            {
                Console.WriteLine("There was an error connecting a client: " + client.ErrorMessage);
                return;
            }

            //receive player name
            string data = client.GetData();
            string playerName = data.Substring(0, data.IndexOf('\n'));
            client.RemoveData(0, playerName.Length + 1);

            //make a new tank
            Tank t = new Tank(playerName);
            t.ID = (int)client.ID;
            t.Joined = true;

            Console.WriteLine("Accepted Client " + t.ID);

            //send player id and world size
            Networking.Send(client.TheSocket, t.ID + "\n" + map.Size + "\n");

            //send walls
            StringBuilder sb = new StringBuilder();
            foreach (Wall w in map.Walls())
            {
                sb.Append(JsonConvert.SerializeObject(w) + "\n");
            }
            Networking.Send(client.TheSocket, sb.ToString());

            client.OnNetworkAction = OnReceiveCommand;

            //start sending world updates
            lock (clients)
            {
                clients.Add((int)client.ID, client);
            }

            // add this new tank to the map
            lock (map)
            {
                map.UpdateTank(t);
            }

            // continue the event loop by receiving commands from this client
            Networking.GetData(client);
        }

        /// <summary>
        /// Networking callback for a client sending a command.
        /// Deserializes the command and adds it to the buffer to be processed
        ///   by the main update thread, then continues the event loop
        /// </summary>
        /// <param name="client">The SocketState representing the client that sent the command</param>
        private void OnReceiveCommand(SocketState client)
        {
            if (client.ErrorOccurred)
            {
                lock (clients)
                {
                    if (clients.Remove((int)client.ID))
                        Console.WriteLine("client " + client.ID + " was disconnected");
                }
                return;
            }

            string data = client.GetData();
            string[] parts = Regex.Split(data, @"(?<=[\n])");

            foreach (string p in parts)
            {
                //if it's an empty string or is missing the ending '\n', ignore it
                if (p.Length == 0)
                    continue;
                if (p[p.Length - 1] != '\n')
                    break;

                ControlCommand cc = JsonConvert.DeserializeObject<ControlCommand>(p);
                lock (latestCommands)
                {
                    latestCommands[(int)client.ID] = cc;
                }

                // set the special boolean for a beam firing command to make sure it doesn't get missed
                if (cc.Fire == "alt" && map.Tanks()[(int)client.ID].PowerupCount > 0)
                {
                    lock (map)
                    {
                        map.Tanks()[(int)client.ID].BeamFired = true;
                    }
                }
                client.RemoveData(0, p.Length);
            }
            Networking.GetData(client);
        }

        /// <summary>
        /// Runs the infinite loop for the main update thread.
        /// Updates the world, sends the updated world to the clients,
        /// and checks for objects that need to be removed.
        /// </summary>
        private void RunUpdateLoop()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (true)
            {
                while (watch.ElapsedMilliseconds < frameDelay)
                {/* do nothing */}
                watch.Restart();
                UpdateWorld();
                SendUpdates();
                RemoveDeadObjects();
            }
        }

        /// <summary>
        /// Updates the model of the world, according to the physics and
        /// the commands from the clients that are in the buffer.
        /// </summary>
        private void UpdateWorld()
        {
            lock (map)
            {
                lock (latestCommands)
                {
                    // process the single latest command from each client
                    foreach (int key in latestCommands.Keys)
                    {
                        Tank t = map.Tanks()[key];

                        ControlCommand cc = latestCommands[key];
                        switch (cc.Moving)
                        {
                            case "up":
                                t.Orientation = new Vector2D(0, -1);
                                t.IsMoving = true;
                                break;
                            case "down":
                                t.Orientation = new Vector2D(0, 1);
                                t.IsMoving = true;
                                break;
                            case "left":
                                t.Orientation = new Vector2D(-1, 0);
                                t.IsMoving = true;
                                break;
                            case "right":
                                t.Orientation = new Vector2D(1, 0);
                                t.IsMoving = true;
                                break;
                            default:
                                t.IsMoving = false;
                                break;
                        }

                        t.Aiming = cc.Direction;
                        t.Fire = cc.Fire;
                    }
                }

                // updating tank position, shooting, powerup access, and disconnection status
                foreach (Tank t in map.Tanks().Values)
                {
                    // brand new tank
                    if (t.Joined)
                    {
                        t.Joined = false;
                        t.FindNewLocationNoCollisions(map);
                    }

                    // respawning tanks
                    else if (t.hitPoints == 0)
                    {
                        if (!t.Died && t.SpawnCooldown <= 0) // Died in the previous frame
                        {
                            t.Died = true;
                            t.SpawnCooldown = respawnDelay;
                            continue;
                        }
                        else if (t.SpawnCooldown > 1)// cooldown is counting down
                        {
                            t.Died = false;
                            t.SpawnCooldown--;
                            continue;
                        }
                        else if (!t.Died && t.SpawnCooldown <= 1) // time to respawn
                        {
                            t.SpawnCooldown--;
                            t.hitPoints = Tank.MaxHP;
                            if (t.PowerupTimeLeft > 0)
                            {
                                t.PowerupTimeLeft = 0;
                                t.Speed /= tankPowerupMultiplier;
                            }
                            t.FindNewLocationNoCollisions(map);
                        }
                    }

                    // tanks colliding with walls
                    else if (t.IsMoving)
                    {
                        Vector2D oldLoc = t.Location;
                        t.Location = t.Location + t.Orientation * t.Speed;

                        // save the previous location, and restore it if there is a collision                       
                        bool colliding = t.CheckCollisionsOnWalls(map);
                        if (colliding)
                            t.Location = oldLoc;
                        else
                            t.CheckCollisionsOnEdge(map, oldLoc);
                    }

                    // letting this tank fire a main projectile
                    if (!t.Died && t.Fire == "main" && t.FireCooldown < 0)
                    {
                        map.UpdateProjectile(new Projectile(Projectile.NextID, t.Aiming, t.Location, t.ID));
                        t.FireCooldown = shotDelay;
                    }
                    // letting this tank fire a beam
                    else if (!t.Died && t.PowerupCount > 0 && t.BeamFired)
                    {
                        t.BeamFired = false;
                        t.PowerupCount--;
                        Beam beam = new Beam(Beam.NextID, t.Aiming, t.Location, t.ID);
                        map.Beams().Add(beam.ID, beam);

                        foreach (Tank t2 in map.Tanks().Values)
                        {
                            // don't try to kill a tank that's already dead
                            if (t2.hitPoints == 0)
                                continue;

                            if (Map.Intersects(t.Location, t.Aiming, t2.Location, Tank.tankSize / 2))
                            {
                                t2.hitPoints = 0;
                                t.Score++;
                            }
                        }
                    }
                    t.FireCooldown--;

                    // apply the countdown if we are in tank speed boost mode
                    if (powerupMode == "tankSpeedBoost")
                    {
                        t.PowerupTimeLeft--;
                        if (t.PowerupTimeLeft == 0)
                            t.Speed /= tankPowerupMultiplier;
                    }

                    // if the connection no longer exists for a tank,
                    //  set the various flags and remove it's command entry
                    if (!clients.ContainsKey(t.ID))
                    {
                        lock (latestCommands)
                        {
                            latestCommands.Remove(t.ID);
                        }
                        t.Disconnected = true;
                        t.Died = true;
                        t.hitPoints = 0;
                    }
                }

                // projectiles colliding with tanks and walls
                foreach (Projectile p in map.Projectiles().Values)
                {
                    p.Location = p.Location + p.Orientation * projectileSpeed;

                    // if a projectile collides with a Tank, Wall or an Edge we mark it as dead
                    p.CheckCollisionsOnTanks(map);
                    p.CheckCollisionsOnWalls(map);
                    p.CheckCollisionsOnEdges(map);
                }

                // spawning powerups - spawn a new powerup if there isn't the max amount
                if (powerupCurrentCount < powerupMaxCount)
                {
                    if (powerupCooldown < 0)
                    {
                        Powerup p = new Powerup(Powerup.NextID);
                        powerupCurrentCount++;
                        p.FindNewLocationNoCollisions(map);
                        map.UpdatePowerup(p);
                        Random rand = new Random();
                        powerupCooldown = rand.Next(0, powerupMaxDelay);
                    }
                    else
                    {
                        powerupCooldown--;
                    }
                }

                // powerups colliding with tanks
                foreach (Powerup p in map.Powerups().Values)
                {
                    // if a powerup collides with a Tank we mark it as dead and gives the powerup to the Tank
                    CheckCollisionsPowerupOnTanks(p);
                }
            }
        }

        /// <summary>
        /// Send the current version of the Model to each client,
        /// and check that the client is still connected and removes disconnected clients
        /// </summary>
        private void SendUpdates()
        {

            lock (clients)
            {
                HashSet<SocketState> disconnected = new HashSet<SocketState>();
                foreach (SocketState client in clients.Values)
                {
                    // use a stringbuilder to minimize overhead from calls to Networking.Send
                    StringBuilder sb = new StringBuilder();
                    lock (map)
                    {
                        foreach (Tank t in map.Tanks().Values)
                            sb.Append(JsonConvert.SerializeObject(t) + "\n");

                        foreach (Projectile p in map.Projectiles().Values)
                            sb.Append(JsonConvert.SerializeObject(p) + "\n");

                        foreach (Powerup po in map.Powerups().Values)
                            sb.Append(JsonConvert.SerializeObject(po) + "\n");

                        foreach (Beam b in map.Beams().Values)
                            sb.Append(JsonConvert.SerializeObject(b) + "\n");

                        if (!Networking.Send(client.TheSocket, sb.ToString()))
                            disconnected.Add(client);
                    }
                }
                // remove disconnected clients from the clients dictionary
                foreach (SocketState client in disconnected)
                {
                    if (clients.Remove((int)client.ID))
                        Console.WriteLine("client " + client.ID + " was disconnected");
                }
            }

        }

        /// <summary>
        /// Check each collection of projectiles, tanks, powerups, and beams
        ///  and removes anything that is dead (or disconnected for tanks)
        /// </summary>
        private void RemoveDeadObjects()
        {
            lock (map)
            {
                List<Projectile> deadProjectiles = new List<Projectile>();
                foreach (Projectile p in map.Projectiles().Values)
                    if (p.IsDead())
                        deadProjectiles.Add(p);
                foreach (Projectile p in deadProjectiles)
                    map.Projectiles().Remove(p.ID);

                List<Tank> deadTanks = new List<Tank>();
                foreach (Tank t in map.Tanks().Values)
                    if (t.Disconnected)
                        deadTanks.Add(t);
                foreach (Tank t in deadTanks)
                    map.Tanks().Remove(t.ID);

                List<Powerup> deadPowerup = new List<Powerup>();
                foreach (Powerup p in map.Powerups().Values)
                    if (p.Died)
                        deadPowerup.Add(p);
                foreach (Powerup p in deadPowerup)
                    map.Powerups().Remove(p.ID);

                // beams only last one frame, so always remove all of them
                map.Beams().Clear();
            }
        }

        /// <summary>
        /// checks if any tank passes through the Powerup p if yes it 
        ///  may gives the tank a different powerup depanding on the server settings. 
        ///  
        ///  Option 1 the default: the tank can right click to fire a beam that 
        ///  kills all tanks on its way. 
        ///  Option 2 tank speed boost: does something. 
        ///  
        /// note: this may belong in Powerup, but uses so many properties that already
        /// belong to this class, and I'm not sure where those should be moved to,
        /// and makes me not as sure this method should be moved.
        /// </summary>
        /// <param name="p">The Projectile to check against</param>
        private void CheckCollisionsPowerupOnTanks(Powerup p)
        {
            foreach (Tank t in map.Tanks().Values)
            {
                t.GetBoundingBox(out Vector2D p1, out Vector2D p2);

                if (Map.CollidePointAndRectangle(p.Location, p1, p2))
                {
                    if (p.Died == false)
                    {
                        switch (powerupMode)
                        {
                            case "tankSpeedBoost":
                                if (t.PowerupTimeLeft <= 0) // don't try to boost the tank speed if it already boosted
                                    t.Speed *= tankPowerupMultiplier;
                                t.PowerupTimeLeft = tankPowerupLengthOfTime;
                                p.Died = true;
                                powerupCurrentCount--;
                                break;
                            case "default":
                            default:
                                p.Died = true;
                                powerupCurrentCount--;
                                t.PowerupCount++;
                                break;
                        }
                    }
                }
            }
        }
    }
}
