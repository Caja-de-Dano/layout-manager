using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LayoutDesigner
{
    public class BoxButton : Button
    {
        public int AssignedValue;
        public string NormalCommand; // the main functionality for the button
        public string AltCommand = ""; // the not mandatory alt commands that would be triggered when holding the MOD button

        private void DefaultButtonLayout()
        {
            this.FlatStyle = 0; //0-flat,1-popup,2-standard,3-system
            this.FlatAppearance.BorderSize = 0;
            this.Size = new Size(100, 100);
            this.BackColor = SystemColors.ControlDark;
        }
        public BoxButton()
        {
            DefaultButtonLayout();
            this.AssignedValue = -1;
        }
        public BoxButton(String text, Point location, int assignedValue)
        {
            DefaultButtonLayout();
            this.AssignedValue = assignedValue;
            this.Text = text;
            this.Location = location;
            // XXX: unknown why we do this other than the note that was left before..
            // OLD NOTE: we set this here so that altCommands can be set
            this.NormalCommand = text;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            GraphicsPath graphics = new GraphicsPath();
            graphics.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            this.Region = new System.Drawing.Region(graphics);
            base.OnPaint(pevent);
        }
    }
}
