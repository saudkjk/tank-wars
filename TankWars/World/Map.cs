using System;
using System.Collections.Generic;
using TankWars;

// Represents the map of the game and holds all other model objects
//  As part of PS8 for CS3500
// Date: 4/8/2021
// Authors: Daniel Nelson and Saoud Aldowaish

// Update 4/22/2021: Add getter for Beams

namespace World
{
    /// <summary>
    /// Container for all the objects in the world
    /// </summary>
    public class Map
    {
        // the pixel size of the world
        public int Size { get; set; }

        // Collections for each type of object
        private List<Wall> walls;
        private Dictionary<int, Tank> tanks;
        private Dictionary<int, Projectile> projectiles;
        private Dictionary<int, Powerup> powerups;
        private Dictionary<int, Beam> beams;

        public int playerTankID { get; set; }

        /// <summary>
        /// Instantiates the collections and sets the world size to 0
        /// </summary>
        public Map()
        {
            Size = 0;
            walls = new List<Wall>();
            tanks = new Dictionary<int, Tank>();
            projectiles = new Dictionary<int, Projectile>();
            beams = new Dictionary<int, Beam>();
            powerups = new Dictionary<int, Powerup>();
        }

        /// <summary>
        /// Returns the collection of Tanks
        /// </summary>
        public Dictionary<int, Tank> Tanks()
        {
            return tanks;
        }

        /// <summary>
        /// Returns the collection of Projectiles
        /// </summary>
        public Dictionary<int, Projectile> Projectiles()
        {
            return projectiles;
        }

        /// <summary>
        /// Returns the collection of Powerups
        /// </summary>
        public Dictionary<int, Powerup> Powerups()
        {
            return powerups;
        }

        public Dictionary<int, Beam> Beams()
        {
            return beams;
        }

        /// <summary>
        /// Returns the collection of Walls
        /// </summary>
        public List<Wall> Walls()
        {
            return walls;
        }

        /// <summary>
        /// If the Tank already is in the collection, replaces the old one.
        /// Otherwise, adds it to the collection
        /// </summary>
        /// <param name="t">The Tank to update</param>
        public void UpdateTank(Tank t)
        {
            lock (this)
            {
                if (tanks.ContainsKey(t.ID))
                    tanks[t.ID] = t;
                else
                    tanks.Add(t.ID, t);

                if (t.Disconnected)
                    tanks.Remove(t.ID);
            }
        }

        /// <summary>
        /// If the Projectile already exists, replace the old one.
        /// Otherwise, add it to the collection
        /// </summary>
        /// <param name="p">The Projectile to update</param>
        public void UpdateProjectile(Projectile p)
        {
            lock (this)
            {
                if (projectiles.ContainsKey(p.ID))
                    projectiles[p.ID] = p;
                else
                    projectiles.Add(p.ID, p);

                if (p.IsDead())
                    projectiles.Remove(p.ID);
            }
        }

        /// <summary>
        /// Add the new Beam to the collection
        /// </summary>
        /// <param name="b">The Beam to add</param>
        public void AddBeam(Beam b)
        {
            lock (this)
            {
                beams.Add(b.ID, b);
            }
        }

        /// <summary>
        /// If the Powerup already exists, replace the old one.
        /// Otherwise add it to the collection
        /// </summary>
        /// <param name="p"></param>
        public void UpdatePowerup(Powerup p)
        {
            lock (this)
            {
                if (powerups.ContainsKey(p.ID))
                    powerups[p.ID] = p;
                else
                    powerups.Add(p.ID, p);

                if (p.IsDead())
                    powerups.Remove(p.ID);
            }
        }

        /// <summary>
        /// Add a wall to the map
        /// </summary>
        /// <param name="w">The new Wall</param>
        public void AddWall(Wall w)
        {
            walls.Add(w);
        }

        /// <summary>
        /// Checks if the point loc coollides with the rectangle described by p1 and p2
        /// </summary>
        /// <param name="loc">A point to check</param>
        /// <param name="p1">The top left of the bounding box</param>
        /// <param name="p2">The bottom right of the bounding box</param>
        /// <returns></returns>
        public static bool CollidePointAndRectangle(Vector2D loc, Vector2D p1, Vector2D p2)
        {
            if (p1.GetX() <= loc.GetX() && p2.GetX() >= loc.GetX()) // inside horizontally
                if (p1.GetY() <= loc.GetY() && p2.GetY() >= loc.GetY()) // inside vertically
                    return true;

            return false;
        }

        /// <summary>
        /// Determines if a ray interescts a circle
        /// </summary>
        /// <param name="rayOrig">The origin of the ray</param>
        /// <param name="rayDir">The direction of the ray</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="r">The radius of the circle</param>
        /// <returns></returns>
        public static bool Intersects(Vector2D rayOrig, Vector2D rayDir, Vector2D center, double r)
        {
            // ray-circle intersection test
            // P: hit point
            // ray: P = O + tV
            // circle: (P-C)dot(P-C)-r^2 = 0
            // substituting to solve for t gives a quadratic equation:
            // a = VdotV
            // b = 2(O-C)dotV
            // c = (O-C)dot(O-C)-r^2
            // if the discriminant is negative, miss (no solution for P)
            // otherwise, if both roots are positive, hit

            double a = rayDir.Dot(rayDir);
            double b = ((rayOrig - center) * 2.0).Dot(rayDir);
            double c = (rayOrig - center).Dot(rayOrig - center) - r * r;

            // discriminant
            double disc = b * b - 4.0 * a * c;

            if (disc < 0.0)
                return false;

            // find the signs of the roots
            // technically we should also divide by 2a
            // but all we care about is the sign, not the magnitude
            double root1 = -b + Math.Sqrt(disc);
            double root2 = -b - Math.Sqrt(disc);

            return (root1 > 0.0 && root2 > 0.0);
        }
    }
}
