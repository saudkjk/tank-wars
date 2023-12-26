using Newtonsoft.Json;
using TankWars;

// A class representing a command that is sent to the game server
// As part of PS8 for CS3500
// Authors: Saoud Aldowaish and Daniel Nelson
// 4/8/2021

namespace GameController
{
    /// <summary>
    /// Represents the actions a client is trying to take that it can request of a server
    /// Namely being the direction of movement, what is being fired by the turret,
    ///  and the direction of the turret
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
   public class ControlCommand
    {
        // The direction the player's tank is trying to move in. 
        // Valid values are: "none", "up", "down", "left", and "right"
        [JsonProperty(PropertyName = "moving")]
        public string Moving { get; set; }

        // The kind of projectile the player's tank is trying to fire
        // Valid values are: "none", "main", and "alt"
        [JsonProperty(PropertyName = "fire")]
        public string Fire { get; set; }

        // The direction the turret is trying to face, as a 
        //   normalized Vector2D
        [JsonProperty(PropertyName = "tdir")]
        public Vector2D Direction { get; set; }

        /// <summary>
        /// A default constructor that sets Moving and Fire to "none",
        ///   and Direction to straight up
        /// </summary>
        public ControlCommand()
        {
            Moving = "none";
            Fire = "none";
            Direction = new Vector2D(0, -1);
        }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="moving">The requested direction to move</param>
        /// <param name="fire">The requested kind of projectile</param>
        /// <param name="direction">The requested direction of the turret</param>
        public ControlCommand(string moving, string fire, Vector2D direction)
        {
            Moving = moving;
            Fire = fire;
            Direction = direction;
        }



    }
}
