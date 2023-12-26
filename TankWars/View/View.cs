using GameController;
using System;
using System.Drawing;
using System.Windows.Forms;
using TankWars;
using World;

// This is the window for the tank wars game 
//  as part of ps8 cs3500
// Date: 4/8/2021
// Authors: Daniel Nelson and Saoud Aldowaish 

namespace View
{
    public partial class View : Form
    {
        private Controller ctrl;
        private Map map;

        // add window components
        MapPanel mapPanel;
        Button connectButton;
        Label nameLabel;
        TextBox nameText;
        Label serverLabel;
        TextBox serverText;

        private const int viewSize = 900;
        private const int menuSize = 40;

        /// <summary>
        /// the view constructor 
        /// </summary>
        /// <param name="controller"></param>
        public View(Controller controller)
        {
            InitializeComponent();
            ctrl = controller;
            ctrl.ErrorHappened += ErrorMessage;
            ctrl.UpdateArrived += OnFrame;
            map = ctrl.GetMap();

            //Set the size of the form
            ClientSize = new Size(viewSize, viewSize + menuSize);

            // Place and add the server label
            serverLabel = new Label();
            serverLabel.Text = "Server:";
            serverLabel.Location = new Point(5, 10);
            serverLabel.Size = new Size(40, 15);
            this.Controls.Add(serverLabel);

            // Place and add the server textbox
            serverText = new TextBox();
            serverText.Text = "localhost";
            serverText.Location = new Point(50, 5);
            serverText.Size = new Size(70, 15);
            this.Controls.Add(serverText);

            // Place and add the name label
            nameLabel = new Label();
            nameLabel.Text = "Name:";
            nameLabel.Location = new Point(125, 10);
            nameLabel.Size = new Size(40, 15);
            this.Controls.Add(nameLabel);

            // Place and add the name textbox
            nameText = new TextBox();
            nameText.Text = "player";
            nameText.Location = new Point(180, 5);
            nameText.Size = new Size(70, 15);
            this.Controls.Add(nameText);

            // Place and add the button
            connectButton = new Button();
            connectButton.Location = new Point(255, 5);
            connectButton.Size = new Size(70, 20);
            connectButton.Text = "Connect";
            connectButton.Click += ConnectClick;
            connectButton.TabStop = false; // make sure the button does not stay highlighted after connecting
            this.Controls.Add(connectButton);

            // Place and add the map panel
            mapPanel = new MapPanel(map);
            mapPanel.Location = new Point(0, menuSize);
            mapPanel.Size = new Size(viewSize, viewSize);
            mapPanel.BackColor = Color.Black;
            this.Controls.Add(mapPanel);

            // Set up key and mouse handlers
            this.KeyDown += HandleKeyDown;
            this.KeyUp += HandleKeyUp;
            mapPanel.MouseDown += HandleMouseDown;
            mapPanel.MouseUp += HandleMouseUp;
            mapPanel.MouseMove += HandleMouseDirection;

            // adding animation handlers
            ctrl.BeamFired += mapPanel.AddBeamAnimation;
            ctrl.TankDied += mapPanel.AddTankExplosionAnimation;
        }

        /// <summary>
        /// Click handler for the connect button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectClick(object sender, EventArgs e)
        {
            HandleConnect();
        }

        /// <summary>
        /// handler for the connection process that checks if the player's name is 
        ///  longer than 16 characters and, if it's valid, starts the connection process
        /// </summary>
        private void HandleConnect()
        {
            if (nameText.Text.Length > 16)
            {
                MessageBox.Show("Player name cannot exceed 16 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.Invoke(new MethodInvoker(() => DisableControls()));
            ctrl.Connect(serverText.Text, nameText.Text);
        }

        /// <summary>
        /// Handler for the controller's UpdateArrived event
        /// </summary>
        private void OnFrame()
        {
            try
            {
                // Invalidate this form and all its children
                // This will cause the form to redraw as soon as it can
                MethodInvoker invalidator = new MethodInvoker(() => this.Invalidate(true));
                this.Invoke(invalidator);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// an event handler for when we receive an error 
        ///  from the controller that displays a message box 
        ///  with an option to reconnect
        /// </summary>
        /// <param name="errorMessage">The string to display in the pop up</param>
        private void ErrorMessage(string errorMessage)
        {
            DialogResult result = MessageBox.Show("There was a problem connecting to the server: " + errorMessage, "Error", MessageBoxButtons.RetryCancel);
            if (result == DialogResult.Retry)
            {
                HandleConnect();
            }
            else
            {
                //re-enable to button and text boxes if they click cancel
                this.Invoke(new MethodInvoker(() => EnableControls()));
            }
        }

        /// <summary>
        /// enables the button and text box controls
        /// </summary>
        private void EnableControls()
        {
            connectButton.Enabled = true;
            serverText.Enabled = true;
            nameText.Enabled = true;
        }

        /// <summary>
        /// Disable the button and text box input controls
        /// </summary>
        private void DisableControls()
        {
            connectButton.Enabled = false;
            serverText.Enabled = false;
            nameText.Enabled = false;
        }

        /// <summary>
        /// Key down handler for when a key is pressed. 
        /// Alerts the controller of what direction was pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Application.Exit();
            else if (e.KeyCode == Keys.W)
                ctrl.HandleMoveRequest("up");
            else if (e.KeyCode == Keys.A)
                ctrl.HandleMoveRequest("left");
            else if (e.KeyCode == Keys.S)
                ctrl.HandleMoveRequest("down");
            else if (e.KeyCode == Keys.D)
                ctrl.HandleMoveRequest("right");

            // Prevent other key handlers from running
            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        /// <summary>
        /// Handler for when a key is released. 
        /// Alerts the controller of what key was released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
                ctrl.CancelMoveRequest("up");
            else if (e.KeyCode == Keys.A)
                ctrl.CancelMoveRequest("left");
            else if (e.KeyCode == Keys.S)
                ctrl.CancelMoveRequest("down");
            else if (e.KeyCode == Keys.D)
                ctrl.CancelMoveRequest("right");
        }

        /// <summary>
        /// Handler for when a mouse button is pressed.
        /// Alerts the controller of which projectile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ctrl.HandleFireRequest("main");
            if (e.Button == MouseButtons.Right)
                ctrl.HandleFireRequest("alt");
        }

        /// <summary>
        /// Handler for when a mouse button is released.
        /// Alerts the controller of it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                ctrl.CancelFireRequest();
        }

        /// <summary>
        /// Handler for the direction of the mouse. 
        /// Computes the vector from the player's tank 
        ///  to the mouse and sends it to the controller.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseDirection(object sender, MouseEventArgs e)
        {
            Vector2D mouseLoc = new Vector2D(e.X, e.Y);
            Vector2D TankLoc = new Vector2D(450, 450);
            Vector2D direction = mouseLoc - TankLoc;
            direction.Normalize();
            ctrl.HandleFireDirectionRequest(direction);
        }
    }
}
