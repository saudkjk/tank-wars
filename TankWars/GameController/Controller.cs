using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TankWars;
using World;

// This is the game controller for the tank wars game as part of PS8 cs3500
// Date: 4/8/2021 
// Authors: Daniel Nelson and Saoud Aldowaish

namespace GameController
{
    /// <summary>
    /// The Control part of MVC. Handles logic for helping the View and Model (Map) interact
    /// </summary>
    public class Controller
    {
        // The server that is providing the game
        private SocketState server;
        //The Model part of MVC
        private Map map;
        // the Tank object that this client controls
        private Tank playerTank;
        // A constant representation of what the client is trying to do
        private ControlCommand command;

        // a linkedlist that keeps track of the player's movements
        private LinkedList<string> moves;

        // For when there is a problem connecting
        public delegate void ErrorOccurredHandler(string errorMessage);
        public event ErrorOccurredHandler ErrorHappened;

        // For when there is new information for the View to draw
        public delegate void ServerUpdateHandler();
        public event ServerUpdateHandler UpdateArrived;

        // For when there is a new Beam object
        public delegate void BeamFiredHandler(Beam b);
        public event BeamFiredHandler BeamFired;

        // For when a Tank dies
        public delegate void TankDiedHandler(Tank t);
        public event TankDiedHandler TankDied;

        /// <summary>
        /// Controller constructor
        /// </summary>
        public Controller()
        {
            map = new Map();
            moves = new LinkedList<string>();
        }

        #region Network connection methods

        /// <summary>
        /// connects the player to the server by using the Networking method ConnectToServer
        /// </summary>
        /// <param name="addr">The hostname or IP address of the server</param>
        /// <param name="playerName">The player's name</param>
        public void Connect(string addr, string playerName)
        {
            playerTank = new Tank(playerName);
            command = new ControlCommand();
            Networking.ConnectToServer(OnConnect, addr, 11000);
        }

        /// <summary>
        /// The callback method for when a connection to the server has been established
        ///   Starts the handshake by sending the player name, then preparing to receive 
        ///   the player ID and world size
        /// </summary>
        /// <param name="state">The SocketState for the server connection</param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // If we couldn't connect, fire the event so that the View can 
                //   show a dialog
                ErrorHappened(state.ErrorMessage);
                return;
            }
            server = state;

            // send player name
            Networking.Send(state.TheSocket, playerTank.Name + '\n');

            //recieve player ID and world size
            server.OnNetworkAction = ReceiveOpeningData;
            Networking.GetData(state);
        }

        /// <summary>
        /// The callback method for when the server sends the player ID and world size.
        ///   Receives that data then prepares to recieve data about the walls
        /// </summary>
        /// <param name="state">The SocketState for the server connection</param>
        private void ReceiveOpeningData(SocketState state)
        {
            string data = state.GetData();

            //get player id and remove it from the buffer
            string id = data.Substring(0, data.IndexOf('\n'));
            state.RemoveData(0, id.Length + 1);
            playerTank.ID = Int32.Parse(id);
            map.playerTankID = playerTank.ID;

            data = state.GetData();

            //get the world size and remove it from the buffer
            string worldSize = data.Substring(0, data.IndexOf('\n'));
            state.RemoveData(0, worldSize.Length + 1);
            map.Size = Int32.Parse(worldSize);


            // continue the event loop
            state.OnNetworkAction = ReceiveWallData;
            Networking.GetData(state);
        }

        /// <summary>
        /// The callback method for when the server sends wall data.
        ///   Parses data as walls until it recieves something that isn't a wall
        ///   Then starts the event loop to receive data about the rest of the map
        /// </summary>
        /// <param name="state">The SocketState for the server connection</param>
        private void ReceiveWallData(SocketState state)
        {
            string totalData = state.GetData();
            // parse the data we received on '\n' characters
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            foreach (string p in parts)
            {
                // if it's an empty string or is missing the ending '\n', ignore it
                if (p.Length == 0)
                    continue;
                if (p[p.Length - 1] != '\n')
                    break;

                JObject obj = JObject.Parse(p);
                //if it's not a wall, end this loop
                if (!obj.ContainsKey("wall"))
                    break;

                // add the wall to the map and remove it from the data we received
                map.AddWall(JsonConvert.DeserializeObject<Wall>(p));
                state.RemoveData(0, p.Length);
            }

            //continue the event loop
            state.OnNetworkAction = ReceiveData;
            Networking.GetData(state);
        }

        /// <summary>
        ///  The callback method for when the server send data about the map.
        ///   Processes this 'frame' of data then sends a control command to the server
        ///   and continues the event loop
        /// </summary>
        /// <param name="state">The SocketState for the server connection</param>
        private void ReceiveData(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                ErrorHappened(state.ErrorMessage);
                return;
            }

            //process data from state
            ProcessData(state);

            // send movements controls to the server
            SendControlCommand();

            // tell the View to redraw
            if (UpdateArrived != null)
                UpdateArrived();

            //contine event loop
            Networking.GetData(state);
        }

        /// <summary>
        /// Decodes all the JSON objects currently in the buffer of the SocketState
        ///   (split with '\n' characters) and sends them to the Map
        /// </summary>
        /// <param name="state">The SocketState for the server connection</param>
        private void ProcessData(SocketState state)
        {
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            //loop until we have processed all messages
            foreach (string p in parts)
            {
                //ignore empty strings
                if (p.Length == 0)
                    continue;
                //ignore the last string if it's missing the terminating '\n'
                if (p[p.Length - 1] != '\n')
                    break;

                JObject obj = JObject.Parse(p);
                if (obj.ContainsKey("tank"))
                {
                    Tank t = JsonConvert.DeserializeObject<Tank>(p);
                    map.UpdateTank(t);
                    if (t.Died)
                        TankDied(t); // adds a new tank explosion animation
                }
                else if (obj.ContainsKey("proj"))
                {
                    map.UpdateProjectile(JsonConvert.DeserializeObject<Projectile>(p));
                }
                else if (obj.ContainsKey("beam"))
                {
                    BeamFired(JsonConvert.DeserializeObject<Beam>(p));
                }
                else if (obj.ContainsKey("power"))
                {
                    map.UpdatePowerup(JsonConvert.DeserializeObject<Powerup>(p));
                }

                //remove from the SocketState's buffer
                state.RemoveData(0, p.Length);
            }
        }

        /// <summary>
        /// Sends the current ControlCommand to the server.
        /// </summary>
        private void SendControlCommand()
        {
            if (moves.Count < 1) // if no keys are pressed right now
                command.Moving = "none";
            else
                command.Moving = moves.First.Value;

            Networking.Send(server.TheSocket, JsonConvert.SerializeObject(command) + "\n");

            // forces the player to release the mouse button to send a new beam
            if (command.Fire == "alt")
                command.Fire = "none";
        }

        #endregion

        #region Player command Handlers

        /// <summary>
        /// Handler for when the player wants to fire
        /// </summary>
        /// <param name="fire">A string representing the kind of projectile: "none", "main", or "alt"</param>
        public void HandleFireRequest(string fire)
        {
            command.Fire = fire;
        }

        /// <summary>
        /// Handler for when the player stops firing
        /// </summary>
        public void CancelFireRequest()
        {
            command.Fire = "none";
        }

        /// <summary>
        /// Handler for the direction of the player's turret
        /// </summary>
        /// <param name="direction">A normalized Vector2D representing the direction from the player tank to the mouse</param>
        public void HandleFireDirectionRequest(Vector2D direction)
        {
            // Have to check for null because this method can be called right away,
            //   as soon as the client starts
            if (command != null)
                command.Direction = direction;
        }

        /// <summary>
        /// Handler for the direction of the player's movement
        /// </summary>
        /// <param name="dir">String for the direction of movement: "none", "up", "down", "left", or "right"</param>
        public void HandleMoveRequest(string dir)
        {
            // As long as they just pressed the direction, add it to the front of the list
            if (!moves.Contains(dir))
                moves.AddFirst(dir);
        }

        /// <summary>
        /// Handler for when the player stops moving
        /// </summary>
        /// <param name="dir"></param>
        public void CancelMoveRequest(string dir)
        {
            moves.Remove(dir);
        }

        #endregion

        /// <summary>
        /// Returns the map representing the world
        /// </summary>
        public Map GetMap()
        {
            return map;
        }

        /// <summary>
        /// Close the connection to the server
        /// </summary>
        public void Close()
        {
            server?.TheSocket.Close();
        }

    }
}
