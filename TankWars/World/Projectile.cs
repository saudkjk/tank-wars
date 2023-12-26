using Newtonsoft.Json;
using TankWars;

// Represents a Projectile that was fired by a Tank
// in the game world
//  as part of PS8 for CS3500
// Date: 4/8/2021
// Authors: Daniel Nelson and Saoud Aldowaish

// Update 4/22/2021: Add NextID and give more properties public get/set

namespace World
{
    /// <summary>
    /// A class for representing a Projectile that was fired by a tank
    ///  with an ID, location, direction, and boolean for if it exists still
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Projectile
    {
        [JsonProperty(PropertyName = "proj")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "loc")]
        public Vector2D Location { get;  set; }

        [JsonProperty(PropertyName = "dir")]
        public Vector2D Orientation { get; private set; }

        [JsonProperty(PropertyName = "died")]
        public bool Died { get; set; }

        [JsonProperty(PropertyName = "owner")]
        public int Owner { get; private set; }

        public static int NextID
        {
            get
            {
                return p_id++;
            }
        }

        private static int p_id = 0;

        public Projectile (int id ,Vector2D dir , Vector2D loc , int owner)
        {
            ID = id;
            Orientation = dir;
            Location = loc;
            Owner = owner;

        }

        /// <summary>
        /// True if this particle hit something, false otherwise
        /// </summary>
        /// <returns></returns>
        public bool IsDead()
        {
            return Died;
        }

        /// <summary>
        /// Checks that the Projectiel p is not colliding with any Tanks.
        ///  If it is, sets the Projectile as dead and removes one health from
        ///  the Tank it hit. If the Tank now has 0 health, adds to the score
        ///  of the owner of the Projectile.
        /// </summary>
        /// <param name="p">projectile</param>
        public void CheckCollisionsOnTanks(Map map)
        {
            foreach (Tank t in map.Tanks().Values)
            {
                if (this.IsDead())
                    break; // don't check anymore tanks if the projectile is dead
                if (t.hitPoints == 0)
                    continue; // don't try to hit a tank that's already dead

                t.GetBoundingBox(out Vector2D p1, out Vector2D p2);

                // make sure a tank can't shoot itself
                if (this.Owner != t.ID && Map.CollidePointAndRectangle(this.Location, p1, p2))
                {
                    this.Died = true;
                    t.hitPoints--;
                    if (t.hitPoints == 0)
                        map.Tanks()[this.Owner].Score++;
                }
            }
        }

        /// <summary>
        /// Checks that the Projectile p is not off the edge of the world. 
        ///  If it is, sets the Projectile as dead so that it gets removed
        /// </summary>
        /// <param name="p">projectile</param>
        public void CheckCollisionsOnEdges(Map map)
        {
            if (this.Location.GetX() < -map.Size / 2 || this.Location.GetX() > map.Size / 2)
                this.Died = true;
            if (this.Location.GetY() < -map.Size / 2 || this.Location.GetY() > map.Size / 2)
                this.Died = true;
        }

        /// <summary>
        /// Checks that the Projectile p is not colliding with any Walls.
        ///  If it is, sets the Projectile as dead so that it is removed
        /// </summary>
        /// <param name="p">projectile</param>
        public void CheckCollisionsOnWalls(Map map)
        {
            int i = 0;
            while (!this.IsDead() && i < map.Walls().Count)
            {
                map.Walls()[i].GetBoundingBox(out Vector2D p1, out Vector2D p2, 0);
                if (Map.CollidePointAndRectangle(this.Location, p1, p2))
                    this.Died = true;
                i++;
            }
        }
    }
}
