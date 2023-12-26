using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using World;
using MapPanel;

// This is the mapPanel that draws objects for the tank wars game
//  as part of ps8 cs3500
// Date: 4/8/2021 
// Authors: Daniel Nelson and Saoud Aldowaish 

// Update 4/22/2021: renamed some variables

namespace View
{
    public partial class MapPanel : Panel
    {
        private Map map;
        private List<Image> tankImages; // a list of images the holds the different colors of tanks
        private List<Image> turretImages; // a list of images the holds the different colors of turrets
        private List<Image> ProjectilesImages; // a list of images the holds the different colors of projectiles
        private HashSet<BeamAnimation> beamAnimations; // a hashset of the beam animations
        private HashSet<TankExplosionAnimation> explosionAnimations; // a hashset of the explosion animations
        private Image backgroundImage;
        private Image wallImage;

        // A delegate for DrawObjectWithTransform
        // Methods matching this delegate can draw whatever they want using e  
        public delegate void ObjectDrawer(object o, PaintEventArgs e);

        /// <summary>
        ///  the mapPanel constructor
        /// </summary>
        /// <param name="map">The Map model</param>
        public MapPanel(Map map)
        {
            DoubleBuffered = true;
            this.map = map;

            beamAnimations = new HashSet<BeamAnimation>();
            explosionAnimations = new HashSet<TankExplosionAnimation>();

            tankImages = new List<Image>();
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\BlueTank.png"));
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\DarkTank.png"));
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\GreenTank.png"));
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\LightGreenTank.png"));
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\OrangeTank.png"));
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\PurpleTank.png"));
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\RedTank.png"));
            tankImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\YellowTank.png"));

            turretImages = new List<Image>();
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\BlueTurret.png"));
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\DarkTurret.png"));
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\GreenTurret.png"));
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\LightGreenTurret.png"));
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\OrangeTurret.png"));
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\PurpleTurret.png"));
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\RedTurret.png"));
            turretImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\YellowTurret.png"));

            ProjectilesImages = new List<Image>();
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-blue.png"));
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-grey.png"));
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-green.png"));
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-white.png"));
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-brown.png"));
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-violet.png"));
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-red.png"));
            ProjectilesImages.Add(Image.FromFile("..\\..\\..\\Resources\\Images\\shot-yellow.png"));

            backgroundImage = Image.FromFile("..\\..\\..\\Resources\\Images\\Background.png");
            wallImage = Image.FromFile("..\\..\\..\\Resources\\Images\\WallSprite.png");
        }

        /// <summary>
        /// This method performs a translation and rotation to drawn an object in the world.
        /// </summary>
        /// <param name="e">PaintEventArgs to access the graphics (for drawing)</param>
        /// <param name="o">The object to draw</param>
        /// <param name="worldX">The X coordinate of the object in world space</param>
        /// <param name="worldY">The Y coordinate of the object in world space</param>
        /// <param name="angle">The orientation of the objec, measured in degrees clockwise from "up"</param>
        /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
        private void DrawObjectWithTransform(PaintEventArgs e, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
        {
            // "push" the current transform
            System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

            e.Graphics.TranslateTransform((int)worldX, (int)worldY);
            e.Graphics.RotateTransform((float)angle);
            drawer(o, e);

            // "pop" the transform
            e.Graphics.Transform = oldMatrix;
        }

        /// <summary>
        /// This method is invoked when the DrawingPanel needs to be re-drawn 
        ///   and it draw all the objects in the world and the animations
        /// </summary>
        /// <param name="e">The PaintEventArgs for drawing</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            int viewSize = Size.Width;

            // make sure we have the map size and the player's tank before we start drawing anything
            if (map.Size != 0 && map.Tanks().TryGetValue(map.playerTankID, out Tank playerTank))
            {
                lock (map)
                {
                    double playerX = playerTank.Location.GetX(); //(the player's world-space X coordinate)
                    double playerY = playerTank.Location.GetY(); ; //(the player's world-space Y coordinate)

                    // center the view on the player's tank
                    e.Graphics.TranslateTransform((float)-playerX + (viewSize / 2), (float)-playerY + (viewSize / 2));

                    // draw the background
                    Rectangle r = new Rectangle(-(map.Size / 2), -(map.Size / 2), map.Size, map.Size);
                    e.Graphics.DrawImage(backgroundImage, r);

                    // draw the walls
                    DrawWalls(e);

                    // Draw the tanks
                    foreach (Tank tank in map.Tanks().Values)
                    {
                        if (tank.hitPoints != 0) // make sure the tank is alive before we draw it
                        {
                            DrawObjectWithTransform(e, tank, tank.Location.GetX(), tank.Location.GetY(), tank.Angle(), TankDrawer);
                            DrawObjectWithTransform(e, tank, tank.Location.GetX(), tank.Location.GetY(), 0, TankAccessoriesDrawer);
                            DrawObjectWithTransform(e, tank, tank.Location.GetX(), tank.Location.GetY(), tank.Aiming.ToAngle(), TurretDrawer);
                        }
                    }

                    // Draw the powerups
                    foreach (Powerup pow in map.Powerups().Values)
                        DrawObjectWithTransform(e, pow, pow.Location.GetX(), pow.Location.GetY(), 0, PowerupDrawer);

                    // Draw the projectiles
                    foreach (Projectile p in map.Projectiles().Values)
                        DrawObjectWithTransform(e, p, p.Location.GetX(), p.Location.GetY(), p.Orientation.ToAngle(), ProjectileDrawer);

                    // Draw beams
                    HashSet<BeamAnimation> removalSet = new HashSet<BeamAnimation>();
                    foreach (BeamAnimation b in beamAnimations)
                    {
                        DrawObjectWithTransform(e, b, b.Origin.GetX(), b.Origin.GetY(), b.Orientation.ToAngle(), b.BeamDrawer);
                        if (b.Expired())
                            removalSet.Add(b);
                    }
                    foreach (BeamAnimation b in removalSet)
                        beamAnimations.Remove(b);

                    // Draw explosions
                    HashSet<TankExplosionAnimation> toRemove = new HashSet<TankExplosionAnimation>();
                    foreach (TankExplosionAnimation tankEA in explosionAnimations)
                    {
                        DrawObjectWithTransform(e, tankEA, tankEA.Origin.GetX(), tankEA.Origin.GetY(), 0, tankEA.TankExplosionDrawer);
                        if (tankEA.Expired())
                            toRemove.Add(tankEA);
                    }
                    foreach (TankExplosionAnimation tankEA in toRemove)
                        explosionAnimations.Remove(tankEA);
                }
            }
            // Do anything that Panel (from which we inherit) needs to do
            base.OnPaint(e);
        }

        /// <summary>
        ///  A private method that gets the walls x and y so we know where to draw them
        /// </summary>
        /// <param name="e">The PaintEventArgs for drawing</param>
        private void DrawWalls(PaintEventArgs e)
        {
            foreach (Wall wall in map.Walls())
            {
                //vertical wall
                if (wall.P1.GetX() == wall.P2.GetX())
                {
                    double start = wall.P1.GetY();
                    double end = wall.P2.GetY();
                    double x = wall.P1.GetX();

                    //ensure start is on the left
                    if (start > end)
                    {
                        double temp = start;
                        start = end;
                        end = temp;
                    }
                    // repeatedly draw a wall sprite for the length of the wall 
                    while (start <= end)
                    {
                        DrawObjectWithTransform(e, wall, x, start, 0, WallDrawer);
                        start += 50;
                    }
                }
                //horizontal wall
                else
                {
                    double start = wall.P1.GetX();
                    double end = wall.P2.GetX();
                    double y = wall.P1.GetY();

                    //ensure start is on the top
                    if (start > end)
                    {
                        double temp = start;
                        start = end;
                        end = temp;
                    }
                    // repeatedly draw a wall sprite for the length of the wall
                    while (start <= end)
                    {
                        DrawObjectWithTransform(e, wall, start, y, 0, WallDrawer);
                        start += 50;
                    }
                }
            }
        }

        /// <summary>
        ///  Acts as a drawing delegate for DrawObjectWithTransform that draws the tanks
        /// </summary>
        /// <param name="o">The Tank object</param>
        /// <param name="e">The PaintEventArgs for drawing</param>
        private void TankDrawer(object o, PaintEventArgs e)
        {
            Tank tank = o as Tank;
            int tankSize = 60;
            Rectangle r = new Rectangle(-(tankSize / 2), -(tankSize / 2), tankSize, tankSize);

            Image tankImage = tankImages[tank.ID % 8];
            e.Graphics.DrawImage(tankImage, r);

        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform that 
        ///   draws health bars and the players names and scores.
        /// </summary>
        /// <param name="o">The Tank object</param>
        /// <param name="e">The PaintEventArgs for drawing</param>
        private void TankAccessoriesDrawer(object o, PaintEventArgs e)
        {
            Tank tank = o as Tank;
            int tankSize = 60;

            // getting the percentage of the player's tank health to know 
            //  what color to draw and the how much health the player's have left
            double healthPercent = (double)tank.hitPoints / (double)Tank.MaxHP;
            Rectangle r = new Rectangle(-(tankSize / 2), (-tankSize / 2) - 15, (int)(tankSize * healthPercent), 5);

            using (SolidBrush redBrush = new SolidBrush(Color.Red))
            using (SolidBrush yellowBrush = new SolidBrush(Color.Yellow))
            using (SolidBrush greenBrush = new SolidBrush(Color.Green))
            {
                if (healthPercent < 0.34)
                    e.Graphics.FillRectangle(redBrush, r);
                else if (healthPercent < 0.67)
                    e.Graphics.FillRectangle(yellowBrush, r);
                else
                    e.Graphics.FillRectangle(greenBrush, r);
            }

            r.Y *= -1; // switch to drawing below instead of above the tank
            Font f = new Font(Font.FontFamily, 12);
            SizeF sizef = e.Graphics.MeasureString(tank.Name + ": " + tank.Score, f);
            PointF point = new PointF(-(sizef.Width / 2), tankSize / 2 + 2); //center the player name under the tank

            // draw the player's name and score
            using (SolidBrush blackBrush = new SolidBrush(Color.White))
                e.Graphics.DrawString(tank.Name + ": " + tank.Score, f, blackBrush, point);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform that draws turrets
        /// </summary>
        /// <param name="o">The Tank object</param>
        /// <param name="e">The PaintEventArgs for drawing</param>
        private void TurretDrawer(object o, PaintEventArgs e)
        {
            Tank tank = o as Tank;
            int turretSize = 50;
            Rectangle r = new Rectangle(-(turretSize / 2), -(turretSize / 2), turretSize, turretSize);

            Image turretImage = turretImages[tank.ID % 8];
            e.Graphics.DrawImage(turretImage, r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform that draws walls
        /// </summary>
        /// <param name="o">Unused</param>
        /// <param name="e">The PaintEventArgs for drawing</param>
        private void WallDrawer(object o, PaintEventArgs e)
        {
            int wallSize = 50;
            Rectangle r = new Rectangle(-(wallSize / 2), -(wallSize / 2), wallSize, wallSize);

            e.Graphics.DrawImage(wallImage, r);
        }

        /// <summary>
        ///  Acts as a drawing delegate for DrawObjectWithTransform that draws projectiles
        /// </summary>
        /// <param name="o">The Projectile object</param>
        /// <param name="e">The PaintEventArgs for drawing</param>
        private void ProjectileDrawer(object o, PaintEventArgs e)
        {
            Projectile projectile = o as Projectile;
            int ProjectileSize = 30;
            Rectangle r = new Rectangle(-(ProjectileSize / 2), -(ProjectileSize / 2), ProjectileSize, ProjectileSize);

            Image projectileImage = ProjectilesImages[projectile.Owner % 8];
            e.Graphics.DrawImage(projectileImage, r);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform that draws powerups
        /// </summary>
        /// <param name="o">The Powerup object</param>
        /// <param name="e">The PaintEventArgs for drawing</param>
        private void PowerupDrawer(object o, PaintEventArgs e)
        {
            Powerup p = o as Powerup;

            int size = 20;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.SolidBrush redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
            using (System.Drawing.SolidBrush yellowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow))
            {
                Rectangle r = new Rectangle(-(size / 2), -(size / 2), size, size);

                e.Graphics.FillEllipse(redBrush, r);

                r.X += 5;
                r.Y += 5;
                r.Width -= 10;
                r.Height -= 10;

                e.Graphics.FillEllipse(yellowBrush, r);
            }

        }

        /// <summary>
        ///  a method that creates a new BeamAnimation and adds it to beamAnimations
        /// </summary>
        /// <param name="b">The beam that was just fired</param>
        public void AddBeamAnimation(Beam b)
        {
            BeamAnimation ba = new BeamAnimation(b.Origin, b.Orientation);
            this.Invoke(new MethodInvoker(() => { beamAnimations.Add(ba); }));
        }

        /// <summary>
        /// a method that creates a new TankExplosionAnimation and adds it to explosionAnimations
        /// </summary>
        /// <param name="tank">The Tank that just died</param>        
        public void AddTankExplosionAnimation(Tank tank)
        {
            TankExplosionAnimation exp = new TankExplosionAnimation(tank.Location);
            this.Invoke(new MethodInvoker(() => { explosionAnimations.Add(exp); }));
        }

    }
}
