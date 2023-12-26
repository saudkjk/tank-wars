using Newtonsoft.Json;
using System;
using TankWars;

// Represents a wall in the game world
//  as part of PS8 for CS3500
// Date: 4/8/2021
// Authors: Daniel Nelson and Saoud Aldowaish

// Update 4/22/2021: Add NextID and size

namespace World
{
    /// <summary>
    /// Represents a wall in the game world with
    /// Two end points that are expected to have either
    /// the same x or y value and an ID
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        public static int NextID
        {
            get
            {
                return p_id++;
            }
        }

        private static int p_id = 0;

        [JsonProperty(PropertyName = "wall")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "p1")]
        public Vector2D P1 { get; private set; }

        [JsonProperty(PropertyName = "p2")]
        public Vector2D P2 { get; private set; }

        public static readonly int size = 50;

        /// <summary>
        /// Default constructor required for JSON to work properly
        /// </summary>
        public Wall()
        { }

        /// <summary>
        /// Constructor that sets the ID and endpoints of the wall
        /// </summary>
        /// <param name="id">The ID of the wall</param>
        /// <param name="p1">One endpoint of the wall</param>
        /// <param name="p2">the other endpoint</param>
        public Wall(int id, Vector2D p1, Vector2D p2)
        {
            ID = id;
            P1 = p1;
            P2 = p2;
        }

        /// <summary>
        /// Returns two points representing the top left and bottom right corners
        ///  of the wall with index/ID i, for collision purposes
        /// </summary>
        /// <param name="i">The index of the Wall</param>
        /// <param name="p1">The top left of the bounding box</param>
        /// <param name="p2">The bottom right of the bounding box</param>
        /// <param name="buffer"> extra space to add to the area of the box on each side</param>
        public void GetBoundingBox(out Vector2D p1, out Vector2D p2, int buffer)
        {
            if (this.P1.GetY() < this.P2.GetY() || this.P1.GetX() < this.P2.GetX()) // if this.P1 is the top or left
            {
                p1 = new Vector2D(this.P1.GetX() - Wall.size / 2 - buffer, this.P1.GetY() - Wall.size / 2 - buffer);
                p2 = new Vector2D(this.P2.GetX() + Wall.size / 2 + buffer, this.P2.GetY() + Wall.size / 2 + buffer);
            }
            else // this.P1 is the bottom or right
            {
                p2 = new Vector2D(this.P1.GetX() + Wall.size / 2 + buffer, this.P1.GetY() + Wall.size / 2 + buffer);
                p1 = new Vector2D(this.P2.GetX() - Wall.size / 2 - buffer, this.P2.GetY() - Wall.size / 2 - buffer);
            }
        }
    }
}
