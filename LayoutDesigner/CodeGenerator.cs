using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LayoutDesigner
{
    public class CodeGenerator
    {
        public const int numButtons = 23;
        public TextBox commandTextbox;
        public TabPage mainTab;
        public IDictionary lookupTable = new Dictionary<string, string>();
        public BoxButton selectedButton;

        private String[] buttonNames = new string[numButtons] { "BR1", "BR2", "TR1", "TR2", "TB", "BR3", "TR3", "BR4", "TR4", "DL", "DR", "DU", "DD", "LSL", "LSR", "LSU", "LSD", "RSL", "RSR", "RSU", "RSD", "TILT1", "TILT2" };
        // these are the default config that I personally use so there might be a need to change this up
        private String[] defaultButtonValues = new string[numButtons] { "A", "B", "X", "Y", "START", "R", "L", "ZR", "ZL", "L100", "R100", "U100", "D100", "HATL", "HATR", "HATU", "HATD", "CSL", "CSR", "CSU", "CSD", "HALF", "MIRROR" };
        private String[] buttonValues = new string[numButtons] { "A", "B", "X", "Y", "START", "R", "L", "ZR", "ZL", "L100", "R100", "U100", "D100", "HATL", "HATR", "HATU", "HATD", "CSL", "CSR", "CSU", "CSD", "HALF", "MIRROR" };
        //-400 from x
        // -100 from y
        private Point[] buttonPos = new Point[numButtons] {
            new Point(565, 250), new Point(665, 218), new Point(586, 143), new Point(687, 102), new Point(450, 10),
            new Point(775, 238), new Point(792, 120), new Point(874, 278), new Point(900, 166), //hasnt been changed knowing that we subtracted 200 from the below line
            new Point(120, 179), new Point(318, 235), new Point(245, 108), new Point(220, 207), // subtracting 100 from x (this is the directional buttons)
            new Point(353, 539), new Point(415, 435), new Point(309, 435), new Point(458, 539),
            new Point(597, 539), new Point(746, 435), new Point(640, 435), new Point(703, 539),
            new Point(33, 263), new Point(652, 639)
        };

        private ConfigButton[] buttons;
        private static List<BoxButton> currentButtons = new List<BoxButton>();
        //private String[] buttonNames;
        private String[] originalButtonValues;

        public CodeGenerator()
        {
            AddKeysToTable();
        }

        public string FetchCommand(string lookupKey)
        {
            return this.lookupTable[lookupKey] as string;
        }

        public bool ValidKey(string lookupKey)
        {
            return this.lookupTable.Contains(lookupKey);
        }

        // NOTE: automatically resets the buttons
        public void CreateButtons(Action<object, MouseEventArgs> mouseHandler, TabPage tabPage)
        {
            // remove buttons here
            while(currentButtons.Count > 0)
            {
                BoxButton a = currentButtons[0];
                currentButtons.RemoveAt(0);
                tabPage.Controls.Remove(a);
            }
            for (var i = 0; i < numButtons; i++)
            {
                buttonValues[i] = defaultButtonValues[i];
                BoxButton temp = new BoxButton(defaultButtonValues[i], buttonPos[i], i);
                temp.MouseDown += new MouseEventHandler(mouseHandler);

                tabPage.Controls.Add(temp);
                currentButtons.Add(temp);
            }
            mainTab = tabPage;
        }

        public void LoadButtons(Action<object, MouseEventArgs> mouseHandler, TabPage tabPage)
        {
            // remove buttons here
            while (currentButtons.Count > 0)
            {
                BoxButton a = currentButtons[0];
                currentButtons.RemoveAt(0);
                tabPage.Controls.Remove(a);
            }
            for (var i = 0; i < numButtons; i++)
            {
                BoxButton temp = new BoxButton(buttonValues[i], buttonPos[i], i);
                temp.MouseDown += new MouseEventHandler(mouseHandler);

                tabPage.Controls.Add(temp);
                currentButtons.Add(temp);
            }
            mainTab = tabPage;

        }

        public void MakeConfig()
        {
            commandTextbox.Text = "";
            for (var i = 0; i < numButtons; i++)
            {
                if (currentButtons[i].AltCommand != "")
                {
                    commandTextbox.Text += buttonNames[i] + ", " + buttonValues[i] + "|" + currentButtons[i].AltCommand + Environment.NewLine;
                }
                else
                {
                    commandTextbox.Text += buttonNames[i] + ", " + buttonValues[i] + Environment.NewLine;
                }
            }
        }

        public string ExportConfig()
        {
            string configText = "";
            for (var i = 0; i < numButtons; i++)
            {
                if (currentButtons[i].AltCommand != "")
                {
                    configText += buttonNames[i] + ", " + buttonValues[i] + "|" + currentButtons[i].AltCommand + Environment.NewLine;
                }
                else
                {
                    configText += buttonNames[i] + ", " + buttonValues[i] + Environment.NewLine;
                }
            }
            return configText;
        }

        public void SetSelectedButton(object selected)
        {
            this.selectedButton = selected as BoxButton;
        }

        public bool CheckTextMatch(string text)
        {
            return (text == selectedButton.NormalCommand || text == selectedButton.AltCommand);
        }

        public void ChangeButtonColor(Color selected)
        {
            selectedButton.BackColor = selected;
        }

        public void SetAltCommand(string cmd)
        {
            selectedButton.AltCommand = cmd;
        }

        public void SetNormCommand(string cmd)
        {
            selectedButton.NormalCommand = cmd;
        }

        public void SetButtonValue(string text)
        {
            buttonValues[selectedButton.AssignedValue] = text;
        }

        public void SetButtonFromConfig(string buttonName, string buttonValue)
        {
            // lookup from buttonNames to get the right index
            var idx = buttonNames.ToList().IndexOf(buttonName);
            Console.WriteLine("INDEX FOUND: " + idx.ToString());
            if (idx != -1)
            {
                //buttonValues[idx] = buttonValue;
                //currentButtons[idx].NormalCommand = buttonValue;
                selectedButton = currentButtons[idx];
                SetNormCommand(buttonValue);
                SetButtonValue(buttonValue);
                SetButtonText();
            }
        }

        public void SetButtonText()
        {
            selectedButton.Text = selectedButton.NormalCommand;
            if(selectedButton.AltCommand != "")
            {
                selectedButton.Text += " | " + selectedButton.AltCommand;
            }
        }

        private void AddButtons()
        {
            for(var i = 0; i < numButtons; i++)
            {
                ConfigButton temp = new ConfigButton(buttonNames[i], buttonValues[i], buttonPos[i]);
                buttons.Append(temp);
            }
        }

        private void AddKeysToTable()
        {
            this.lookupTable.Add("X", "ReportData->Button |= SWITCH_X;");
            this.lookupTable.Add("Y", "ReportData->Button |= SWITCH_Y;");
            this.lookupTable.Add("R", "ReportData->Button |= SWITCH_R;");
            this.lookupTable.Add("L", "ReportData->Button |= SWITCH_L;");
            this.lookupTable.Add("ZR", "ReportData->Button |= SWITCH_ZR;");
            this.lookupTable.Add("ZL", "ReportData->Button |= SWITCH_ZL;");
            this.lookupTable.Add("A", "ReportData->Button |= SWITCH_A;");
            this.lookupTable.Add("B", "ReportData->Button |= SWITCH_B;");
            this.lookupTable.Add("START", "ReportData->Button |= SWITCH_PLUS;");
            this.lookupTable.Add("MINUS", "ReportData->Button |= SWITCH_MINUS;");
            this.lookupTable.Add("CSR", "ReportData->RX = 255;ReportData->RY = 128;");
            this.lookupTable.Add("CSL", "ReportData->RX = 0;ReportData->RY = 128;");
            this.lookupTable.Add("CSU", "ReportData->RX = 128;ReportData->RY = 0;");
            this.lookupTable.Add("CSD", "ReportData->RX = 128;ReportData->RY = 255;");
            this.lookupTable.Add("R100", "ReportData->LX = 255;ReportData->LY = 128;direction_pressed = true;");
            this.lookupTable.Add("L100", "ReportData->LX = 0;ReportData->LY = 128;direction_pressed = true;");
            this.lookupTable.Add("SODC_L100", "if(direction_pressed) { ReportData->LX = 128; ReportData->LY = 128; } else { ReportData->LX = 0;ReportData->LY = 128;direction_pressed = true;}");
            this.lookupTable.Add("U100", "if(direction_pressed){ReportData->LY = 0;}else{ReportData->LX = 128;ReportData->LY = 0;}");
            this.lookupTable.Add("D100", "if(direction_pressed){ReportData->LY = 255;}else{ReportData->LX = 128;ReportData->LY = 255;}");
            this.lookupTable.Add("MIRROR", "mirror_pressed = true;");
            this.lookupTable.Add("HALF", "if(direction_pressed) {if(ReportData->LX == 0) {ReportData->LX = 60;} else {ReportData->LX = 196;}}if(ReportData->LY == 255) {ReportData->LY = 192;} else if(ReportData->LY == 0) {ReportData->LY = 60;}");
            this.lookupTable.Add("SOCD_HALF", "if(last_dir != -1) {  if(ReportData->LX == 0) {    ReportData->LX = 60;  } else {    ReportData->LX = 196;  }}if(ReportData->LY == 255) {  ReportData->LY = 192;} else if(ReportData->LY == 0) {  ReportData->LY = 60;}");
            this.lookupTable.Add("HATU", "ReportData->HAT = HAT_TOP;");
            this.lookupTable.Add("HATD", "ReportData->HAT = HAT_BOTTOM;");
            this.lookupTable.Add("HATL", "ReportData->HAT = HAT_LEFT;");
            this.lookupTable.Add("HATR", "ReportData->HAT = HAT_RIGHT;");
            this.lookupTable.Add("HOME", "ReportData->Button |= SWITCH_HOME;");
            this.lookupTable.Add("CAPTURE", "ReportData->Button |= SWITCH_CAPTURE;");
            this.lookupTable.Add("LEFTSTICKPRESS", "ReportData->Button |= SWITCH_LCLICK;");
            this.lookupTable.Add("RIGHTSTICKPRESS", "ReportData->Button |= SWITCH_RCLICK;");
            this.lookupTable.Add("TRAININGTOGGLE", "ReportData->Button |= SWITCH_L | SWITCH_X; ReportData->HAT = HAT_LEFT;");
            this.lookupTable.Add("RESETTRAINING", "ReportData->Button |= SWITCH_L | SWITCH_R | SWITCH_A;");
            this.lookupTable.Add("SAVE_STATE", "ReportData->HAT = HAT_BOTTOM; ReportData->Button |= SWITCH_R;");
            this.lookupTable.Add("LOAD_STATE", "ReportData->HAT = HAT_TOP; ReportData->Button |= SWITCH_R;");
            this.lookupTable.Add("UPB", "ReportData->LY = 0; ReportData->Button |= SWITCH_B;");
            /// melee things
            this.lookupTable.Add("MELEE_R100", "ReportData->LX = 255; ReportData->LY = 128; direction_pressed = true;");
            this.lookupTable.Add("MELEE_L100", "ReportData->LX = 0; ReportData->LY = 128;direction_pressed = true;");
            this.lookupTable.Add("MELEE_U100", "if(direction_pressed){ReportData->LY = 48;}else{ReportData->LX = 128;ReportData->LY = 48;}");
            this.lookupTable.Add("MELEE_D100", "if(direction_pressed){ReportData->LY = 208;}else{ReportData->LX = 128;ReportData->LY = 208;}");
            this.lookupTable.Add("MELEE_HALF", "if(direction_pressed) {if(ReportData->LX == 0) {ReportData->LX = 70;} else {ReportData->LX = 130;}}if(ReportData->LY > 128) {ReportData->LY = 150;} else if(ReportData->LY < 128) {ReportData->LY = 76;}");
            // firefox angle things
            this.lookupTable.Add("STEEP_TILT", "if(ReportData->LX > 128) {    if(ReportData->LY > 128) {        ReportData->LX = 230;    } else if(ReportData->LY < 128) {        ReportData->LX = 230;        ReportData->LY = 75;    }} else if(ReportData->LX < 128) {    if(ReportData->LY < 128) {        ReportData->LY = 75;    }}");
        }
    }
}
