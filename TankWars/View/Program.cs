using System;
using System.Windows.Forms;
using GameController;

// This is the point of entry for the tank wars game 
//  as part of ps8 cs3500
// Date: 4/8/2021 
// Authors: Daniel Nelson and Saoud Aldowaish 

namespace View
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Controller ctrl = new Controller();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            View form = new View(ctrl);
            Application.Run(form);
        }
    }
}
