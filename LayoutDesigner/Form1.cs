using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace LayoutDesigner
{

    public partial class Form1 : Form
    {
        private BoxButton selected_button;
        private static List<BoxButton> current_buttons = new List<BoxButton>();
        private const int num_buttons = 23;
        private String[] button_names = new string[num_buttons] {"BR1", "BR2", "TR1", "TR2", "TB", "BR3", "TR3", "BR4", "TR4", "DL", "DR", "DU", "DD", "LSL", "LSR", "LSU", "LSD", "RSL", "RSR", "RSU", "RSD", "TILT1", "TILT2"};
        // these are the default config that I personally use so there might be a need to change this up
        private String[] og_button_values = new string[num_buttons] { "A", "B", "X", "Y", "START", "L", "R", "ZL", "ZR", "L100", "R100", "U100", "D100", "HATL", "HATR", "HATU", "HATD", "CSL", "CSR", "CSU", "CSD", "HALF", "MIRROR" };
        private String[] button_values = new string[num_buttons] { "A", "B", "X", "Y", "START", "L", "R", "ZL", "ZR", "L100", "R100", "U100", "D100", "HATL", "HATR", "HATU", "HATD", "CSL", "CSR", "CSU", "CSD", "HALF", "MIRROR" };
        //-400 from x
        // -100 from y
        private Point[] button_locs = new Point[num_buttons] {
            new Point(565, 250), new Point(665, 218), new Point(586, 143), new Point(687, 102), new Point(450, 10),
            new Point(775, 238), new Point(792, 120), new Point(874, 278), new Point(900, 166), //hasnt been changed knowing that we subtracted 200 from the below line
            new Point(120, 179), new Point(318, 235), new Point(245, 108), new Point(220, 207), // subtracting 100 from x (this is the directional buttons)
            new Point(353, 539), new Point(415, 435), new Point(309, 435), new Point(458, 539),
            new Point(597, 539), new Point(746, 435), new Point(640, 435), new Point(703, 539),
            new Point(33, 263), new Point(652, 639)
        };
        private IDictionary lookup_table = new Dictionary<string, string>();
        private bool waiting_for_press = false;
        private bool right_clicked_menu = false;
        private bool can_run_background = true;

        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;

        // nothing here
        private void get_usb_devices()
        {
            bool found = false;
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                string description = (string)device.GetPropertyValue("Description");
                string device_id = (string)device.GetPropertyValue("DeviceID");
                if (description == "HID-compliant game controller" && device_id.Contains("HID\\VID"))
                {
                    found = true;
                    backgroundWorker1.ReportProgress(2);
                }
                Console.WriteLine("DEVICE ID: " + (string)device.GetPropertyValue("DeviceID"));
                Console.WriteLine("PNPDeviceID: " + (string)device.GetPropertyValue("PNPDeviceID"));
                Console.WriteLine("Description: " + (string)device.GetPropertyValue("Description"));
            }
            collection.Dispose();
            if (found == false)
            {
                backgroundWorker1.ReportProgress(1);
            }
        }

        // only reason we will keep this around is to handle the unplugging of a device / updating the status when things move
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            switch (m.Msg)
            {
                case WM_DEVICECHANGE:
                    Console.WriteLine("DEVICE change");
                    if (!backgroundWorker1.IsBusy)
                    {
                        backgroundWorker1.RunWorkerAsync();
                    }

                    switch ((int)m.WParam)
                    {
                        case DBT_DEVICEARRIVAL:
                            //DEV_BROADCAST_HDR pHdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(m.lParam, typeof(DEV_BROADCAST_HDR));
                            Console.WriteLine("DEVICE arrival");
                            int devType = Marshal.ReadInt32(m.LParam, 4);
                            Console.WriteLine("DEV TYPE: " + devType);
                            Console.WriteLine(m.LParam);
                            Console.WriteLine(m.WParam);
                            break;
                        case DBT_DEVICEREMOVECOMPLETE:
                            Console.WriteLine("DEVICE move complete");
                            break;
                    }
                    break;
            }
        }

        private void remove_buttons()
        {
            while(current_buttons.Count > 0)
            {
                BoxButton a = current_buttons[0];
                current_buttons.RemoveAt(0);
                tabPage1.Controls.Remove(a);
            }
        }
        private void build_default_buttons()
        {
            remove_buttons();
            for (var i = 0; i < num_buttons; i++)
            {
                BoxButton temp = new BoxButton(button_values[i], button_locs[i], i);
                temp.MouseDown += new MouseEventHandler(this.boxaddProfileButton_MouseDown);

                tabPage1.Controls.Add(temp);
                current_buttons.Add(temp);
            }
        }

        public Form1()
        {
            InitializeComponent();
            get_usb_devices();
            build_default_buttons();
            lookup_table.Add("X", "ReportData->Button |= SWITCH_X;");
            lookup_table.Add("Y", "ReportData->Button |= SWITCH_Y;");
            lookup_table.Add("R", "ReportData->Button |= SWITCH_R;");
            lookup_table.Add("L", "ReportData->Button |= SWITCH_L;");
            lookup_table.Add("ZR", "ReportData->Button |= SWITCH_ZR;");
            lookup_table.Add("ZL", "ReportData->Button |= SWITCH_ZL;");
            lookup_table.Add("A", "ReportData->Button |= SWITCH_A;");
            lookup_table.Add("B", "ReportData->Button |= SWITCH_B;");
            lookup_table.Add("START", "ReportData->Button |= SWITCH_PLUS;");
            lookup_table.Add("MINUS", "ReportData->Button |= SWITCH_MINUS;");
            lookup_table.Add("CSR", "ReportData->RX = 255;ReportData->RY = 128;");
            lookup_table.Add("CSL", "ReportData->RX = 0;ReportData->RY = 128;");
            lookup_table.Add("CSU", "ReportData->RX = 128;ReportData->RY = 0;");
            lookup_table.Add("CSD", "ReportData->RX = 128;ReportData->RY = 255;");
            lookup_table.Add("R100", "ReportData->LX = 255;ReportData->LY = 128;direction_pressed = true;");
            lookup_table.Add("L100", "ReportData->LX = 0;ReportData->LY = 128;direction_pressed = true;");
            lookup_table.Add("SODC_L100", "if(direction_pressed) { ReportData->LX = 128; ReportData->LY = 128; } else { ReportData->LX = 0;ReportData->LY = 128;direction_pressed = true;}");
            lookup_table.Add("U100", "if(direction_pressed){ReportData->LY = 0;}else{ReportData->LX = 128;ReportData->LY = 0;}");
            lookup_table.Add("D100", "if(direction_pressed){ReportData->LY = 255;}else{ReportData->LX = 128;ReportData->LY = 255;}");
            lookup_table.Add("MIRROR", "if(direction_pressed) {if(ReportData->LX == 0) {ReportData->LX = 255;} else {ReportData->LX = 0;}} else {mirror_pressed = true;}");
            lookup_table.Add("HALF", "if(direction_pressed) {if(ReportData->LX == 0) {ReportData->LX = 60;} else {ReportData->LX = 196;}}if(ReportData->LY == 255) {ReportData->LY = 192;} else if(ReportData->LY == 0) {ReportData->LY = 60;}");
            lookup_table.Add("HATU", "ReportData->HAT = HAT_TOP;");
            lookup_table.Add("HATD", "ReportData->HAT = HAT_BOTTOM;");
            lookup_table.Add("HATL", "ReportData->HAT = HAT_LEFT;");
            lookup_table.Add("HATR", "ReportData->HAT = HAT_RIGHT;");
        }

        private void generate_config()
        {
            textBox1.Text = "";
            for(var i=0;i<num_buttons;i++)
            {
                if(current_buttons[i].AltCommand != "")
                {
                    textBox1.Text += button_names[i] + ", " + button_values[i] + "|" + current_buttons[i].AltCommand + Environment.NewLine;
                } else
                {
                    textBox1.Text += button_names[i] + ", " + button_values[i] + Environment.NewLine;
                }
            }
        }
        // TODO: move to a class start

        // XXX: this will break with the multiple commands on the button
        private string generate_joystick()
        {
            string og_template = File.ReadAllText("template.c");
            foreach (string line in File.ReadLines("config.dbox"))
            {
                string[] button_command = line.Split(',');
                string button = "//"+button_command[0];
                string command = Regex.Replace(button_command[1], @"\s+", "");
                string alt_command = "";
                string replace_code_string = "";//will be filled out depending if the command contains an alt / just a norm
                if(command.Contains('|'))
                {
                    string[] commands = command.Split('|');
                    string double_command = "if(mirror_pressed) { //ALT } else { //NORM }";
                    command = commands[0];
                    alt_command = commands[1];
                    double_command = double_command.Replace("//ALT", lookup_table[alt_command] as string);
                    double_command = double_command.Replace("//NORM", lookup_table[command] as string);
                    Console.WriteLine("alt command is present");
                    Console.WriteLine(double_command);
                    replace_code_string = double_command;
                } else
                {
                    replace_code_string = lookup_table[command] as string;
                }
                // COMO: need to work out the way we'd replace the code for a multiple command in this section
                if (lookup_table.Contains(command))
                {
                    og_template = og_template.Replace(button, replace_code_string);
                } else
                {
                    Console.WriteLine("skipping "+command);
                }
            }
            File.WriteAllText("Joystick.c", og_template);
            return og_template;
        }
        // TODO: END ABOVE

        private void Form1_Load(object sender, EventArgs e)
        {
            this.generate_config();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void dadToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        // create a new tab
        private void addProfileButton_Click(object sender, EventArgs e)
        {
            TabPage tpOld = tabControl1.SelectedTab;

            TabPage tpNew = new TabPage();
            tpNew.Text = "Profile 2";
            foreach (Control c in tpOld.Controls)
            {
                Control cNew = (Control)Activator.CreateInstance(c.GetType());

                PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(c);

                foreach (PropertyDescriptor entry in pdc)
                {
                    object val = entry.GetValue(c);
                    entry.SetValue(cNew, val);
                }

                // add control to new TabPage
                tpNew.Controls.Add(cNew);
            }

            tabControl1.TabPages.Add(tpNew);
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        // Button click handler for BoxButton
        private void boxaddProfileButton_MouseDown(object sender, MouseEventArgs e)
        {
            selected_button = sender as BoxButton;
            // Bold the currently selected option from the dropdown menu
            // XXX: kinda ghetto solve to bold the current selection in the menu
            // not sure of a better way to take care of it but for now its working and could be moved into a function
            for (var i = 0; i < buttonSwapMenu.Items.Count; i++)
            {
                if (buttonSwapMenu.Items[i].Text == selected_button.NormalCommand || buttonSwapMenu.Items[i].Text == selected_button.AltCommand)
                {
                    ToolStripItem a = buttonSwapMenu.Items[i];
                    a.Font = new Font(a.Font, a.Font.Style | FontStyle.Bold); 
                }
                else
                {
                    ToolStripItem a = buttonSwapMenu.Items[i];
                    a.Font = new Font(a.Font, FontStyle.Regular);
                }
            }

            if (e.Button == MouseButtons.Left)
            {
                right_clicked_menu = false;
                buttonSwapMenu.Show(Cursor.Position);
                // experimental drawing stuff here
                selected_button.BackColor = SystemColors.ButtonHighlight;
            }
            if (e.Button == MouseButtons.Right)
            {
                //do something
                right_clicked_menu = true;
                buttonSwapMenu.Show(Cursor.Position);
            }
        }

        private void buttonSwapMenu_Closing(object sender, CancelEventArgs e)
        {
            // experimental reset the button
            selected_button.BackColor = SystemColors.ControlDark;
        }

        private void buttonSwapMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string new_text = e.ClickedItem.Text;
            if(new_text == "Remove Alt Button")
            {
                selected_button.AltCommand = "";
                set_text_for_button();
                this.generate_config();
            } else
            {
                if (right_clicked_menu)
                {
                    selected_button.AltCommand = new_text;
                    set_text_for_button();
                    this.generate_config();
                    // TODO: not setting the button value because it will break the generate_config function
                    // TODO: want to add additional information
                    // TODO: need to regenerate the config here
                }
                else
                {
                    selected_button.NormalCommand = new_text;
                    // want to find what the text was before selection
                    button_values[selected_button.AssignedValue] = new_text;
                    //selected_button.Text = new_text;
                    set_text_for_button();
                    this.generate_config();
                }
            }
        }
        private void set_text_for_button()
        {
            selected_button.Text = selected_button.NormalCommand;
            if(selected_button.AltCommand != "")
            {
                selected_button.Text += " | " + selected_button.AltCommand;
            }
        }

        private void resetToDefaultButton_Click(object sender, EventArgs e)
        {
            build_default_buttons();
        }

        private void generateAndLoadButton_Click(object sender, EventArgs e)
        {
            File.WriteAllText("config.dbox", textBox1.Text);
            string joystick_c = generate_joystick();
            // Create a 'WebRequest' object with the specified url. 
            string data = joystick_c;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            //WebRequest request = WebRequest.Create("http://localhost:4567/make-it");
            WebRequest request = WebRequest.Create("http://143.110.136.163/make-it");
            
            //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = "application/json";
            request.Method = "POST";

            // Send the 'WebRequest' and wait for response.

            using (Stream requestBody = request.GetRequestStream())
            {
                requestBody.Write(dataBytes, 0, dataBytes.Length);
            }
            WebResponse myWebResponse = request.GetResponse();
            Stream stream = myWebResponse.GetResponseStream();
            string response_text = "";
            using (StreamReader reader = new StreamReader(stream))
            {
                response_text = reader.ReadToEnd();
            }
            //Console.WriteLine(response_text);
            myWebResponse.Close();
            File.WriteAllText("Joystick.hex", response_text);

            try
            {
                using (Process p = new Process())
                {
                    Process ps = new Process();
                    ps.StartInfo.FileName = "teensy_loader_cli.exe";
                    ps.StartInfo.Arguments = "-w -v -mmcu=at90usb1286 Joystick.hex";
                    ps.StartInfo.UseShellExecute = false;
                    ps.StartInfo.CreateNoWindow = true;
                    ps.StartInfo.RedirectStandardOutput = true;
                    ps.OutputDataReceived += (s, args) => 
                    {
                        if(!String.IsNullOrEmpty(args.Data))
                        {
                            if (args.Data != null && args.Data.Contains("Waiting for Teensy device"))
                            {
                                Console.WriteLine("FOUND");
                                waiting_for_press = true;
                                generateAndLoadButton.BeginInvoke(new MethodInvoker(() => {
                                    label1.Text = "Open and Press Button Inside";
                                    label1.ForeColor = System.Drawing.Color.Red;
                                    generateAndLoadButton.Text = "Open and Press";
                                    generateAndLoadButton.BackColor = Color.Red;
                                }));
                            }
                            if (args.Data.Contains("Booting"))
                            {
                                Console.WriteLine("WROTE TO BOX");
                                ps.Close();
                                generateAndLoadButton.BeginInvoke(new MethodInvoker(() => {
                                    generateAndLoadButton.Text = "Generate Config";
                                    generateAndLoadButton.BackColor = Color.Transparent;
                                }));
                            }
                        }
                    };
                    ps.Start();
                    ps.BeginOutputReadLine();
                }
            }
            catch
            {
                Console.WriteLine("Failed to start exe");
            }
        }

        private void exportLayoutButton_Click(object sender, EventArgs e)
        {
            string joystick_c = generate_joystick();
            FlexibleMessageBox.Show(joystick_c);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("DO WORK");
            get_usb_devices();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch(e.ProgressPercentage)
            {
                case (1):
                    label1.ForeColor = System.Drawing.Color.Red;
                    label1.Text = "No Device Detected";
                    break;
                case (2):
                    label1.Text = "Device Detected (Caja Grande)";
                    label1.ForeColor = System.Drawing.Color.Green;
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Save the config to an exportable file.. open the file dialog to save somewhere on disk");
        }
    }
}
