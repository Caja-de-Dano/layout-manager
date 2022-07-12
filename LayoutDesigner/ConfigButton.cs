using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayoutDesigner
{
    class ConfigButton
    {
        private string buttonName;
        private string originalValue; // used for resetting to default
        private string currentValue;
        private Point location;

        public ConfigButton(string name, string value, Point location)
        {
            this.buttonName = name;
            this.originalValue = value;
            this.location = location;
        }
    }
}
