using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TankWars;

// This is the two animation classes BeamAnimation and TankExplosionAnimation 
//    for the tank wars game as part of PS8 cs3500
// Date: 4/8/2021 
// Authors: Daniel Nelson and Saoud Aldowaish 

namespace MapPanel
{

    /// <summary>
    /// A class representing the animation of a beam that was fired
    /// </summary>
    class BeamAnimation
    {
        // Represents the point where the beam starts
        public Vector2D Origin { get; private set; }
        // Represents the direction the beam points
        public Vector2D Orientation { get; private set; }
        // The number of frames left in this animation
        private int frame;

        /// <summary>
        /// the BeamAnimation constructor
        /// </summary>
        /// <param name="pos">The position the beam starts at</param>
        /// <param name="dir">The direction the beam points</param>
        public BeamAnimation(Vector2D pos, Vector2D dir)
        {
            Origin = pos;
            Orientation = dir;

            //By default this animation lasts 30 frames
            frame = 30;
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform that draws beams
        /// </summary>
        /// <param name="o">Unused</param>
        /// <param name="e">The PaintEventArgs for drawing</param>
        public void BeamDrawer(object o, PaintEventArgs e)
        {
            int vOffset = -25;

            if (frame > 0)
            {
                //create a path of the circle at the origin
                GraphicsPath path = new GraphicsPath();
                Rectangle r = new Rectangle();

                if (frame > 25) // first 5 frames, start small and grow larger
                {
                    double percent = (30 - frame) / 5.0; // the decimal percent of the way through this animation
                    //scale the size by the percent
                    int size = (int)(40 * percent);

                    if (percent > 0)
                        r = new Rectangle(-(size / 2), -(size / 2) + vOffset, size, size);
                    else
                    {
                        frame--;
                        return; //if size == 0 don't try to draw
                    }

                    path.AddEllipse(r);

                    //Make a brush using the path
                    using (PathGradientBrush brush = new PathGradientBrush(path))
                    {
                        //set the color of the center to red
                        brush.CenterColor = Color.FromArgb(255, Color.Red);

                        //set the color along the entire boundary to clear
                        Color[] colors = { Color.FromArgb(0) };
                        brush.SurroundColors = colors;

                        //draw the circle at the origin
                        e.Graphics.FillEllipse(brush, r);
                    }

                    //draw the line straight up, since it's already been transformed
                    using (Pen brush1 = new Pen(Color.Red))
                        e.Graphics.DrawLine(brush1, 0, vOffset, 0, -2000);
                }
                else if (frame > 5)// the rest of the animation, slowly fade away
                {
                    double percent = frame / 30.0;
                    int size = 40;

                    r = new Rectangle(-(size / 2), -(size / 2) + vOffset, size, size);

                    path.AddEllipse(r);

                    //Make a brush using the path
                    using (PathGradientBrush brush = new PathGradientBrush(path))
                    {
                        //set the color of the center to red, scaling the alpha by percent
                        brush.CenterColor = Color.FromArgb((int)(255 * percent), Color.Red);

                        //set the color along the entire boundary to clear
                        Color[] colors = { Color.FromArgb(0) };
                        brush.SurroundColors = colors;

                        e.Graphics.FillEllipse(brush, r);
                    }

                    //draw the line, scaling the alpha by percent
                    using (Pen brush1 = new Pen(Color.FromArgb((int)(255 * percent), Color.Red)))
                        e.Graphics.DrawLine(brush1, 0, vOffset, 0, -2000);
                }
                frame--;
            }
        }

        /// <summary>
        /// checks if the frames are zero
        /// </summary>
        /// <returns></returns>
        public bool Expired()
        {
            return frame <= 0;
        }

    }

    /// <summary>
    /// A class representing the animation of a tank that just died
    /// </summary>
    class TankExplosionAnimation
    {
        private static readonly int frameLife = 60; // number of frames the animation should last

        //unlike beams, explosions don't have a direction
        public Vector2D Origin { get; private set; }
        //how many frames this animation has left
        private int frame;
        private HashSet<Particle> dots;

        /// <summary>
        /// TankExplosionAnimation constructor
        /// </summary>
        /// <param name="pos">The point where the explosion starts</param>
        public TankExplosionAnimation(Vector2D pos)
        {
            Origin = pos;
            frame = frameLife;
            dots = new HashSet<Particle>();
            for (int i = 0; i < 15; i++)
                dots.Add(new Particle(i));
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform that draws explosions
        /// </summary>
        /// <param name="o">Unused</param>
        /// <param name="e">The PaintEventArgs used for drawing</param>
        public void TankExplosionDrawer(object o, PaintEventArgs e)
        {
            //update the location of each particle
            foreach (Particle p in dots)
            {
                p.Pos = p.Pos + p.Dir;
            }

            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                // drawing the explosion Particles
                foreach (Particle p in dots)
                {
                    e.Graphics.FillEllipse(brush, (float)p.Pos.GetX(), (float)p.Pos.GetY(), 5, 5);
                }
            }
            frame--;
        }

        /// <summary>
        /// checks if the frames are zero
        /// </summary>
        /// <returns></returns>
        public bool Expired()
        {
            return frame <= 0;
        }

        /// <summary>
        /// a private class for the explosion Particles
        /// </summary>
        protected class Particle
        {
            public Vector2D Dir;
            public Vector2D Pos;

            // creating the explosion Particles and adding them to dirs
            // Use set directions because Random was just returning the same number for some reason
            private static List<Vector2D> dirs = new List<Vector2D>() { new Vector2D(-0.1, 1.5),
                new Vector2D(0.2,-1.4), new Vector2D(0.3, 1.3), new Vector2D(-0.4, -1.2),
                new Vector2D(0.5, -1.1), new Vector2D(0.6, 1), new Vector2D(0.7, -.9),
                new Vector2D(-0.8,- .8), new Vector2D(0.9, .7), new Vector2D(-1,.6),
                new Vector2D(1.1, -.5), new Vector2D(-1.2,- .4), new Vector2D(1.3,- .3),
                new Vector2D(-1.4, .2), new Vector2D(1.5,.1)
            };

            /// <summary>
            /// the Particle constructor
            /// </summary>
            /// <param name="index">The index from directions</param>
            public Particle(int index)
            {
                Pos = new Vector2D(0, 0);
                Dir = dirs[index];
            }
        }
    }
}
