﻿using System;
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
        // these are the default config that I personally use so there might be a need to change this up
        private CodeGenerator masterGenerator = new CodeGenerator();
        private CodeGenerator currentGenerator;
        private bool waitingForPress = false;
        private bool rightClickedMenu = false;
        private bool can_run_background = true;

        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;
        private List<CodeGenerator> profileGenerators = new List<CodeGenerator>();

        // BOX Descriptions
        // DOUBLE STICK BOX LATEST
        // DEVICE ID: HID\VID_0F0D&PID_0092\6&17DE8C14&0&0000
        // HAWK BOX
        // DEVICE ID: HID\VID_0F0D&PID_0092\6&22F43165&2&0000
        // NEW NEW BOX
        // DEVICE ID: HID\VID_0F0D&PID_0092\6&22F43165&2&0000
        private void getUsbDevices()
        {
            bool found = false;
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                collection = searcher.Get();
            ManagementClass processClass = new ManagementClass("Win32_Process");

            foreach (var device in collection)
            {
                string description = (string)device.GetPropertyValue("Description");
                string deviceId = (string)device.GetPropertyValue("DeviceID");
                if (description == "HID-compliant game controller" && deviceId.Contains("HID\\VID"))
                {
                    Console.WriteLine("DEVICE ID: " + (string)device.GetPropertyValue("DeviceID"));
                    Console.WriteLine("PNPDeviceID: " + (string)device.GetPropertyValue("PNPDeviceID"));
                    Console.WriteLine("Manufacturer: " + (string)device.GetPropertyValue("Manufacturer"));

                    found = true;
                    if (deviceId.Contains("17DE8C1"))
                    {
                        backgroundWorker1.ReportProgress(3);
                    }
                    else
                    {
                        backgroundWorker1.ReportProgress(2);
                    }

                }
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

        public Form1()
        {
            InitializeComponent();
            radioButton1.Select();
            getUsbDevices();
            currentGenerator = masterGenerator;
            masterGenerator.CreateButtons(this.boxButtonMouseDownHandler, tabPage1);
            masterGenerator.commandTextbox = textBox1;
            profileGenerators.Add(masterGenerator);
            profileComboBox.SelectedIndex = 0;
            this.Icon = Properties.Resources.gunstock_icon;
            this.Text = "Caja de Dano Layout Manager";
        }

        private void generateConfig()
        {
            currentGenerator.MakeConfig();
        }

        private string generateCode()
        {
            string originalTempalte = File.ReadAllText("template.c");
            if(profileGenerators.Count == 1)
            {
                foreach(string line in profileGenerators[0].ExportConfig().Split(Environment.NewLine.ToCharArray()))
                {
                    if(line.Contains(","))
                    {
                        Console.WriteLine("LINE:" + line);
                        string[] buttonCommand = line.Split(',');
                        string button = "//" + buttonCommand[0];
                        string command = Regex.Replace(buttonCommand[1], @"\s+", "");

                        Console.WriteLine("BUTTON COMMAND: " + button);
                        Console.WriteLine("BUTTON CCCCOMMAND: " + command);
                        string altCommand = "";
                        string replaceCodeString = "";//will be filled out depending if the command contains an alt / just a norm
                        if (command.Contains('|'))
                        {
                            string[] commands = command.Split('|');
                            string doubleCommand = "if(mirror_pressed) { //ALT } else { //NORM }";
                            command = commands[0];
                            altCommand = commands[1];
                            doubleCommand = doubleCommand.Replace("//ALT", currentGenerator.FetchCommand(altCommand));
                            doubleCommand = doubleCommand.Replace("//NORM", currentGenerator.FetchCommand(command));
                            Console.WriteLine("alt command is present");
                            Console.WriteLine(doubleCommand);
                            replaceCodeString = doubleCommand;
                        }
                        else
                        {
                            // TODO: actually check for SODC radio here
                            if(command == "L100")
                            {
                                replaceCodeString = currentGenerator.FetchCommand("SODC_L100");
                            }
                            else
                            {
                                replaceCodeString = currentGenerator.FetchCommand(command);
                            }
                        }
                        // COMO: need to work out the way we'd replace the code for a multiple command in this section
                        if (currentGenerator.ValidKey(command))
                        {
                            originalTempalte = originalTempalte.Replace(button, replaceCodeString);
                        }
                        else
                        {
                            Console.WriteLine("skipping " + command);
                        }

                    }
                }
                originalTempalte.Replace("//STARTUP", "ReportData->Button |= SWITCH_L | SWITCH_R;");
                File.WriteAllText("Joystick.c", originalTempalte);
                return originalTempalte;
            } else
            {
                List<string> rawCommandList = new List<string>();

                List<string> commandList = new List<string>();
                List<string> buttonList = new List<string>();
                foreach (CodeGenerator cg in profileGenerators)
                {
                    foreach (string line in cg.ExportConfig().Split(Environment.NewLine.ToCharArray()))
                    {
                        if (line.Contains(","))
                        {
                            string[] buttonCommand = line.Split(',');
                            string button = "//" + buttonCommand[0];
                            string command = Regex.Replace(buttonCommand[1], @"\s+", "");

                            Console.WriteLine("bb COMMAND: " + button);
                            Console.WriteLine("bb CCCCOMMAND: " + command);
                            string altCommand = "";
                            string replaceCodeString = "";//will be filled out depending if the command contains an alt / just a norm
                            if (command.Contains('|'))
                            {
                                string[] commands = command.Split('|');
                                string doubleCommand = "if(mirror_pressed) { //ALT } else { //NORM }";
                                command = commands[0];
                                altCommand = commands[1];
                                doubleCommand = doubleCommand.Replace("//ALT", currentGenerator.FetchCommand(altCommand));
                                doubleCommand = doubleCommand.Replace("//NORM", currentGenerator.FetchCommand(command));
                                Console.WriteLine("alt command is present");
                                Console.WriteLine(doubleCommand);
                                replaceCodeString = doubleCommand;
                            }
                            else
                            {
                                replaceCodeString = currentGenerator.FetchCommand(command);
                            }


                            if(buttonList.Contains(button))
                            {
                                Console.WriteLine("ALREADY IN THERE");
                                int a = buttonList.IndexOf(button);
                                string og = rawCommandList[a];
                                rawCommandList[a] = og + "$$$" + replaceCodeString;
                            }
                            else
                            {
                                Console.WriteLine("ADDING IN FRESH: " + button);
                                commandList.Add(command);
                                buttonList.Add(button);
                                rawCommandList.Add(replaceCodeString);
                            }

                        }
                    }
                    Console.WriteLine("PROFILE PROCESSING");
                }
                int counter = 0;
                foreach(string b in buttonList)
                {
                    Console.WriteLine("BIG thing:" + b);
                    string key_check = b.Substring(2);
                    string s = commandList[counter];


                    // want to do the replacing right here
                    if (currentGenerator.ValidKey(s))
                    {
                        Console.WriteLine("VALID KEY");
                        string button = "//" + s;
                        string combined_command = rawCommandList[counter];
                        string[] command_combo = Regex.Split(combined_command, @"\$\$\$");

                        string replacement_string = "if(profile_a_active){ " + command_combo[1] + "} else {" + command_combo[0] + "}";
                        originalTempalte = originalTempalte.Replace(b, replacement_string);
                    }
                    else
                    {
                        Console.WriteLine("skipping " + s);
                    }
                    counter += 1;

                }
                foreach (string a in rawCommandList)
                {
                    Console.WriteLine("LINE: " + a);
                }
                // TODO: we should handle for different cases of profile counts but for now lets just do for 2 profiles
                originalTempalte = originalTempalte.Replace("//STARTUP", "if (buf_button & (1<<0)) { profile_a_active = true; } else { ReportData->Button |= SWITCH_L | SWITCH_R; state=USING; }");

            }
            return originalTempalte;
        }
        // TODO: move to a class start

        // XXX: this will break with the multiple commands on the button
        private string generate_joystick()
        {
            string originalTempalte = File.ReadAllText("template.c");
            foreach (string line in File.ReadLines("config.dbox"))
            {
                string[] buttonCommand = line.Split(',');
                string button = "//"+buttonCommand[0];
                string command = Regex.Replace(buttonCommand[1], @"\s+", "");
                string altCommand = "";
                string replaceCodeString = "";//will be filled out depending if the command contains an alt / just a norm
                if(command.Contains('|'))
                {
                    string[] commands = command.Split('|');
                    string doubleCommand = "if(mirror_pressed) { //ALT } else { //NORM }";
                    command = commands[0];
                    altCommand = commands[1];
                    doubleCommand = doubleCommand.Replace("//ALT", currentGenerator.FetchCommand(altCommand));
                    doubleCommand = doubleCommand.Replace("//NORM", currentGenerator.FetchCommand(command));
                    Console.WriteLine("alt command is present");
                    Console.WriteLine(doubleCommand);
                    replaceCodeString = doubleCommand;
                } else
                {
                    replaceCodeString = currentGenerator.FetchCommand(command);
                }
                // COMO: need to work out the way we'd replace the code for a multiple command in this section
                if (currentGenerator.ValidKey(command))
                {
                    originalTempalte = originalTempalte.Replace(button, replaceCodeString);
                } else
                {
                    Console.WriteLine("skipping "+command);
                }
            }
            File.WriteAllText("Joystick.c", originalTempalte);
            return originalTempalte;
        }
        // TODO: END ABOVE

        private void Form1_Load(object sender, EventArgs e)
        {
            this.generateConfig();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void dadToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        // Button click handler for BoxButton
        private void boxButtonMouseDownHandler(object sender, MouseEventArgs e)
        {
            currentGenerator.SetSelectedButton(sender);
            // Bold the currently selected option from the dropdown menu
            // XXX: kinda ghetto solve to bold the current selection in the menu
            // not sure of a better way to take care of it but for now its working and could be moved into a function
            for (var i = 0; i < buttonSwapMenu.Items.Count; i++)
            {
                if (currentGenerator.CheckTextMatch(buttonSwapMenu.Items[i].Text))
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
                rightClickedMenu = false;
                buttonSwapMenu.Show(Cursor.Position);
                // experimental drawing stuff here
                currentGenerator.ChangeButtonColor(SystemColors.ButtonHighlight);
            }
            if (e.Button == MouseButtons.Right)
            {
                //do something
                rightClickedMenu = true;
                buttonSwapMenu.Show(Cursor.Position);
            }
        }

        private void buttonSwapMenu_Closing(object sender, CancelEventArgs e)
        {
            // experimental reset the button
            currentGenerator.ChangeButtonColor(SystemColors.ControlDark);
        }

        private void buttonSwapMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string newText = e.ClickedItem.Text;
            if(newText == "Remove Alt Button")
            {
                currentGenerator.SetAltCommand("");
                currentGenerator.SetButtonText();
                this.generateConfig();
            } else
            {
                if (rightClickedMenu)
                {
                    currentGenerator.SetAltCommand(newText);
                    currentGenerator.SetButtonText();
                    this.generateConfig();
                    // TODO: not setting the button value because it will break the generateConfig function
                    // TODO: want to add additional information
                    // TODO: need to regenerate the config here
                }
                else
                {
                    currentGenerator.SetNormCommand(newText);
                    // want to find what the text was before selection
                    currentGenerator.SetButtonValue(newText);
                    currentGenerator.SetButtonText();
                    this.generateConfig();
                }
            }
        }

        private void resetToDefaultButton_Click(object sender, EventArgs e)
        {
            currentGenerator.CreateButtons(this.boxButtonMouseDownHandler, tabPage1);
        }

        private void generateAndLoadButton_Click(object sender, EventArgs e)
        {
            // OLD WAY
            //File.WriteAllText("config.dbox", textBox1.Text);
            //string joystickCode = generate_joystick();
            string joystickCode = generateCode();

            // Create a 'WebRequest' object with the specified url.
            string data = joystickCode;
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            WebRequest request = WebRequest.Create(Environment.GetEnvironmentVariable("WEB_HOST") + "/make-it");

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
            string responseText = "";
            using (StreamReader reader = new StreamReader(stream))
            {
                responseText = reader.ReadToEnd();
            }
            //Console.WriteLine(responseText);
            myWebResponse.Close();
            File.WriteAllText("Joystick.hex", responseText);

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
                                waitingForPress = true;
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
            //string joystickCode = generate_joystick();
            string joystickCode = generateCode();

            FlexibleMessageBox.Show(joystickCode);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("DO WORK");
            getUsbDevices();
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
                case (3):
                    label1.Text = "Device Detected (Palanaca 2)";
                    label1.ForeColor = System.Drawing.Color.Green;
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Caja Config (*.caja)|*.caja";
            dialog.FilterIndex = 1;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(textBox1.Text);
                FileStream fs = new FileStream(dialog.FileName, FileMode.OpenOrCreate, FileAccess.Write);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Caja Config (*.caja)|*.caja";
            dialog.FilterIndex = 1;
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = "";
                //Read the contents of the file into a stream
                var fileStream = dialog.OpenFile();
                var fileContent = string.Empty;

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string s = string.Empty;
                    while((s = reader.ReadLine()) != null) {
                        // still need to split
                        textBox1.Text += s + System.Environment.NewLine;
                        string[] commands = s.Trim().Split(',');
                        string a = commands[0];
                        string b = commands[1].Trim();
                        Console.WriteLine("command a:" + a + " command b: " + b);
                        currentGenerator.SetButtonFromConfig(a, b);
                    }
                }
                //this.generateConfig();
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl t = (TabControl)sender;
            Console.WriteLine("SELECTED: " + t.SelectedIndex);
            currentGenerator = profileGenerators[t.SelectedIndex];
        }

        private void tabControl1_TabIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine("TAB INDEX CHANGED");
        }

        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            Console.WriteLine("TABCONTROL MOUSE CLICK");

        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            Console.WriteLine("TABCONTROL SELECTED");
        }

        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            Console.WriteLine("TABCONTROL SELECTING");
        }

        private void profileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox a = (ComboBox)sender;
            if(a.SelectedItem.ToString() == "Add Profile")
            {
                a.Items.Insert(a.Items.Count - 1, "Profile "+ a.Items.Count);
                Console.WriteLine("SHOULD ADD A PROFILE");
                CodeGenerator t = new CodeGenerator();
                t.commandTextbox = textBox1;
                profileGenerators.Add(t);
                Console.WriteLine("PROFILE: " + profileGenerators.Count);
                a.SelectedIndex = a.Items.Count-2;
                t.CreateButtons(this.boxButtonMouseDownHandler, tabPage1);
            } else if(a.SelectedItem.ToString().Contains("Profile "))
            {
                Console.WriteLine(a.SelectedIndex);
                currentGenerator = profileGenerators[a.SelectedIndex];
                Console.WriteLine("Profile was selected");
                currentGenerator.LoadButtons(this.boxButtonMouseDownHandler, tabPage1);
                currentGenerator.MakeConfig();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
