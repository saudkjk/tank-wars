using Newtonsoft.Json;
using System;
using TankWars;

// Represents a Tank in the game world
//  as part of PS8 for CS3500
// Date: 4/8/2021
// Authors: Daniel Nelson and Saoud Aldowaish

// Update 4/22/2021: add lots more properties to better represent
//  the current state of the Tank

namespace World
{
    /// <summary>
    /// A class that represents a Tank in the world
    ///  with an ID, player name, location, direction, turret direction,
    ///   health points, score, and booleans for if this Tank died, disconnected,
    ///   or just joined
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Tank
    {
        public static readonly int MaxHP = 3;
        public static readonly int tankSize = 60;
        private static int p_nextId = 0;

        public static int NextID
        {
            get
            {
                return p_nextId++;
            }
        }

        public string Fire { get;  set; }

        public bool IsMoving { get; set; }

        public int FireCooldown { get; set; }

        [JsonProperty(PropertyName = "tank")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }    

        [JsonProperty(PropertyName = "loc")]
        public Vector2D Location { get;  set; }


        private Vector2D p_orientation;

        [JsonProperty(PropertyName = "bdir")]
        public Vector2D Orientation
        {
            get { return p_orientation; }
            set
            {
                p_orientation = value;
                p_orientation.Normalize();
            }
        }

        [JsonProperty(PropertyName = "tdir")]
        public Vector2D Aiming { get;  set; }

        [JsonProperty(PropertyName = "hp")]
        public int hitPoints { get;  set; }

        [JsonProperty(PropertyName = "score")]
        public int Score { get; set; }

        [JsonProperty(PropertyName = "died")]
        public bool Died { get;  set; }

        [JsonProperty(PropertyName = "dc")]
        public bool Disconnected { get;  set; }

        [JsonProperty(PropertyName = "join")]
        public bool Joined { get; set; }

        public int SpawnCooldown { get; set; }
        public int PowerupCount { get; set; }
        public int PowerupTimeLeft { get; set; }
        public bool BeamFired { get; set; }
        public int Speed { get; set; }

        

        /// <summary>
        /// Default constructor that sets the turret direction to up
        ///  and disconnected to false
        /// </summary>
        public Tank()
        {
            Aiming = new Vector2D(0, -1);
            Disconnected = false;
            Speed = 5;
        }

        /// <summary>
        /// Constructor that sets the player name property
        /// </summary>
        /// <param name="playerName">The player name for this Tank</param>
        public Tank(string playerName)
        {
            Location = new Vector2D(50, 50);
            Orientation = new Vector2D(0, 1);
            hitPoints = 3;
            Aiming = new Vector2D(0, -1);
            Name = playerName;
            Disconnected = false;
            IsMoving = false;
            Speed = 5;
        }


        /// <summary>
        /// Returns the direction this Tank is facing as an angle,
        ///  clockwise from up
        /// </summary>
        /// <returns></returns>
        public double Angle()
        {
            return Orientation.ToAngle();
        }

        /// <summary>
        /// Returns two points representing the top left and bottom right corners
        ///  of a tank for collision purposes
        /// </summary>
        /// <param name="i"> index </param>
        /// <param name="p1">The top left of the bounding box</param>
        /// <param name="p2">The bottom right of the bounding box</param>
        /// <param name="size"> extra space</param>
        public void GetBoundingBox(out Vector2D p1, out Vector2D p2)
        {
            // set p1 to the top left corner, and p2 to the bottom right
            p1 = new Vector2D(this.Location.GetX() - Tank.tankSize / 2, this.Location.GetY() - Tank.tankSize / 2);
            p2 = new Vector2D(this.Location.GetX() + Tank.tankSize / 2, this.Location.GetY() + Tank.tankSize / 2);
        }

        /// <summary>
        /// Finds a new, random location for the Tank t 
        ///  that doesn't collide with any walls.
        ///  (for when a tank joins the game or respawns)
        /// </summary>
        /// <param name="t">tank</param>
        public void FindNewLocationNoCollisions(Map map)
        {
            Random rand = new Random();
            this.Location = new Vector2D(rand.Next(-map.Size / 2, map.Size / 2), rand.Next(-map.Size / 2, map.Size / 2));
            int i = 0;

            while (i < map.Walls().Count)
            {
                map.Walls()[i].GetBoundingBox(out Vector2D p1, out Vector2D p2, Tank.tankSize / 2);

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

        /// <summary>
        /// Checks if Tank t is about to go off the edge of the world.
        ///  If it is, AND there is not a wall on the opposite side of the world,
        ///  moves the Tank there. Otherwise sets the Tank's location back to oldLocation
        /// </summary>
        /// <param name="t">The Tank</param>
        /// <param name="oldLocation">The Tank's previous location as a Vector2D</param>
        public void CheckCollisionsOnEdge(Map map, Vector2D oldLocation)
        {
            // left
            if (this.Location.GetX() - this.Speed < -map.Size / 2)
                this.Location = new Vector2D(-this.Location.GetX() - this.Speed, this.Location.GetY());
            // right
            else if (this.Location.GetX() + this.Speed > map.Size / 2)
                this.Location = new Vector2D(-this.Location.GetX() + this.Speed, this.Location.GetY());
            // top
            if (this.Location.GetY() - this.Speed < -map.Size / 2)
                this.Location = new Vector2D(this.Location.GetX(), -this.Location.GetY() - this.Speed);
            // bottom
            else if (this.Location.GetY() + this.Speed > map.Size / 2)
                this.Location = new Vector2D(this.Location.GetX(), -this.Location.GetY() + this.Speed);

            // if we moved the tank, make sure the new location we set it to doesn't collide with a wall
            if (oldLocation != this.Location)
            {
                if (CheckCollisionsOnWalls(map))
                    this.Location = oldLocation;
            }
        }

        /// <summary>
        /// Returns true if the Tank is colliding with any walls, false otherwise
        /// </summary>
        /// <param name="t">The Tank</param>
        public bool CheckCollisionsOnWalls(Map map)
        {
            bool colliding = false;
            int i = 0;
            while (!colliding && i < map.Walls().Count)
            {
                map.Walls()[i].GetBoundingBox(out Vector2D p1, out Vector2D p2, Tank.tankSize / 2);

                if (Map.CollidePointAndRectangle(this.Location, p1, p2))
                    colliding = true;

                i++;
            }
            return colliding;
        }
    }

    
}
