using Newtonsoft.Json;
using System;
using TankWars;

//Represents a Powerup in the world
// as part of PS8 for CS3500
// Date: 4/8/2021
// Authors: Daniel Nelson and Saoud Aldowaish

// Update 4/22/2021: Add NextID and give all properties public get/set

namespace World
{
    /// <summary>
    /// A class for representing a Powerup in the game,
    ///  with an ID, location, and boolean for when it's picked up
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup
    {
        private static int p_NextId = 0;

        public static int NextID
        {
            get
            {
                return p_NextId++;
            }
        }

        [JsonProperty(PropertyName = "power")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "loc")]
        public Vector2D Location { get; set; }

        [JsonProperty(PropertyName = "died")]
        public bool Died { get; set; }

        public Powerup()
        {}

        public Powerup(int id)
        {
            ID = id;
        }

        /// <summary>
        /// True if this Powerup was picked up, false otherwise
        /// </summary>
        public bool IsDead()
        {
            return Died;
        }

        /// <summary>
        /// Finds a new, random location for the Powerup p
        ///  that doesn't collide with any walls.
        ///  (for when a new powerup spawns)
        /// </summary>
        /// <param name="p"> powerup</param>
        public void FindNewLocationNoCollisions(Map map)
        {
            Random rand = new Random();
            this.Location = new Vector2D(rand.Next(-map.Size / 2, map.Size / 2), rand.Next(-map.Size / 2, map.Size / 2));
            int i = 0;

            while (i < map.Walls().Count)
            {
                map.Walls()[i].GetBoundingBox(out Vector2D p1, out Vector2D p2, 10);

                //if this new loc collides with a wall, get a different new loc and start checking walls over again
                if (Map.CollidePointAndRectangle(this.Location, p1, p2))
                {
                    this.Location = new Vector2D(rand.Next(-map.Size / 2, map.Size / 2), rand.Next(-map.Size / 2, map.Size / 2));
                    i = 0;
                }
                else
                    i++;
            }
        }
    }
}