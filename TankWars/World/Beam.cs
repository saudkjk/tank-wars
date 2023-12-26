using Newtonsoft.Json;
using TankWars;

//Represents a Beam in the world
// As part of PS8 for CS3500
// Date: 4/9/2021
// Authors: Daniel Nelson and Saoud Aldowaish

// Update 4/22/2021: Add NextID property and default constructor

namespace World
{
    /// <summary>
    /// Represents a beam with an ID, origin, direction, 
    ///  and the ID of the tank that fired it
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Beam
    {
        [JsonProperty(PropertyName = "beam")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "org")]
        public Vector2D Origin { get; private set; }

        [JsonProperty(PropertyName = "dir")]
        public Vector2D Orientation { get; private set; }

        [JsonProperty(PropertyName = "owner")]
        public int Owner;

        public static int NextID
        {
            get
            {
                return p_id++;
            }
        }

        private static int p_id = 0;
        public Beam(int id, Vector2D dir, Vector2D loc, int owner)
        {
            ID = id;
            Orientation = dir;
            Origin = loc;
            Owner = owner;

        }
    }
}
