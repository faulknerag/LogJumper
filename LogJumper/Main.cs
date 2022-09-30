using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.FileIO;


namespace Kbg.NppPluginNET
{
    class Main
    {
        internal const string PluginName = "LogJumper";
        static string iniFilePath = null; //C:\Users\<user>\AppData\Roaming\Notepad++\plugins\config
        static frmMyDlg frmMyDlg = null;
        static int toolBarButtonID = -1;
        static int idMyDlg;

        //Icons
        static Bitmap tbBmp = LogJumper.Properties.Resources.icon_light;
        //static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = LogJumper.Properties.Resources.star_bmp;
        static Icon tbIcon = null;

        //Keep track of which unit of time to jump 
        public enum JumpUnits
        {
            h, m, s
        }

        //Index for quicker timestamp searches
        static Dictionary<DateTime, int> timestampIndex = new Dictionary<DateTime, int>();
        static bool indexDirty = true;

        //Prevent the plugin from taking too long reading every single line
        public static int maxLinesToRead = 10000;

        //RegEx for date format 10-Jun-21 18:19:42
        public static string defaultRegex = @"^[CEWID] ([a-zA-Z0-9]{1,3}[-/.][a-zA-Z0-9]{1,3}[-/.]\d{1,4} \d{1,2}:\d{1,2}:\d{1,2})";
        public static Regex regex = new Regex(defaultRegex, RegexOptions.IgnoreCase);

        //Scintilla instance
        static IntPtr currentScint = PluginBase.GetCurrentScintilla();
        static ScintillaGateway scintillaGateway = new ScintillaGateway(currentScint);

        //For CSV log files
        public static bool csvLog = false;
        public static char csvDelim = ',';
        public static int csvColumn = 0;

        //Standard direction has timestamps in chronological order going down the log
        public static bool standardLogDirection = true; 

        public static void OnNotification(ScNotification notification)
        {
            // if (notification.Header.Code == (uint)SciMsg.SCNxxx)
            
            //Event is fired when the user switches documents
            if (notification.Header.Code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
            {
                //Update scintilla for this new open page
                currentScint = PluginBase.GetCurrentScintilla();
                scintillaGateway = new ScintillaGateway(currentScint);
                
                //clear timestamp index 
                timestampIndex.Clear();

                //Trigger index to be rebuilt on next navigation click
                indexDirty = true;
            }
        }

        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            //If key is present, get user-defined regex from ini
            StringBuilder temp = new StringBuilder(255);
            if (Win32.GetPrivateProfileString("Startup", "TimestampRegEx", "", temp, 255, iniFilePath) > 0) { regex = new Regex(temp.ToString()); }
            
            //PluginBase.SetCommand(0, "MyMenuCommand", myMenuFunction, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(0, "Log Jumper Navigation Window", ShowLogJumperDialog); 
            //PluginBase.SetCommand(2, "GetCurrentTimestamp", JumpTime);
            toolBarButtonID = 0;
            idMyDlg = 0;
        }

        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[toolBarButtonID]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        internal static void PluginCleanUp()
        {
            Win32.WritePrivateProfileString("Startup", "TimestampRegEx", regex.ToString(), iniFilePath);
        }

        internal static void JumpTime(JumpUnits jumpUnit, int jumpAmount)
        {
            //If the index is dirty, rebuild the index on the first navigation click
            if (indexDirty || timestampIndex.Count == 0) { 
                indexTimestamps();
                indexDirty = false;
            }

            //Line where the cursor currently is (0-indexed)
            int curLineNum = scintillaGateway.GetCurrentLineNumber();
            
            DateTime targetTime = new DateTime();
            bool found = false;
            int linesRead = 0;

            //Get the timestamp corresponding to the current line. In multi-line message formats, the timestamp may be on a previous line
            for (int lineNum = curLineNum; lineNum >= 0; lineNum--)
            { 
                //Stop searching if there's a timestamp on this line
                if (getTimestampFromLineNumber(lineNum, regex, ref targetTime)) {
                    found = true;
                    break; 
                }

                if (linesRead++ >= maxLinesToRead)
                {
                    MessageBox.Show($"No matching timestamp found within {maxLinesToRead} lines. Terminating operation.");
                    return;
                }
            }

            if (!found)
            {
                MessageBox.Show("Unable to identify the timestamp for the current log line: no timestamp found on or before the current line.");
                return;
            }
            
            //Adjust time based on jump unit
            switch (jumpUnit)
            {
                case JumpUnits.h:
                    targetTime = targetTime.AddHours(jumpAmount);
                    break;
                case JumpUnits.m:
                    targetTime = targetTime.AddMinutes(jumpAmount);
                    break;
                case JumpUnits.s:
                    targetTime = targetTime.AddSeconds(jumpAmount);
                    break;
            }

            int searchStart = 0;
            bool foundMatch = false;

            bool findPrevIndexThenTraverseDown = false;

            //Determine search pattern
            if ((standardLogDirection && jumpAmount > 0) || 
                (!standardLogDirection && jumpAmount < 0))
            {
                findPrevIndexThenTraverseDown = true;
            }

            //Determine the start point for the search from the timestamp index
            for (int i = 0; i < timestampIndex.Count; i++) 
            {
                DateTime indexedTime = timestampIndex.ElementAt(i).Key;

                if ((standardLogDirection && indexedTime >= targetTime) ||
                    (!standardLogDirection && indexedTime <= targetTime))
                {
                    if (findPrevIndexThenTraverseDown)
                    {
                        //Looking for the index timestamp that's higher in the log than the target 
                        if (i == 0)
                        {
                            //First indexed timestamp is the match, use it  
                            searchStart = timestampIndex.ElementAt(i).Value;
                        }
                        else
                        {
                            //Use the previous index time because that will be the higher timestamp before the target time
                            searchStart = timestampIndex.ElementAt(i - 1).Value;
                        }
                    }
                    else
                    {
                        searchStart = timestampIndex.ElementAt(i).Value;
                    }
                    foundMatch = true;
                    break;
                }
            }

            //Special case when the targettime is after the last indexed time
            if (!foundMatch)
            {
                if (findPrevIndexThenTraverseDown)
                {
                    //Searching down the log from the index location, so start from the last indexed time
                    searchStart = timestampIndex.Last().Value;
                }
                else
                {
                    //Searching up the log from the index location, so start from the current line
                    searchStart = curLineNum;
                }
            }

            int lineCount = scintillaGateway.GetLineCount();
            foundMatch = false;
            linesRead = 0;

            //Loop through lines after/before the current line looking for the target timestamp 
            for (int testLineNum = searchStart; testLineNum < lineCount && testLineNum >= 0;)
            {
                if (linesRead++ >= maxLinesToRead)
                {
                    MessageBox.Show($"No matching timestamp found within {maxLinesToRead} lines. Terminating operation.");
                    return;
                }

                DateTime temp = new DateTime();
                if (getTimestampFromLineNumber(testLineNum, regex, ref temp)) { 

                    //Check if this is the correct line to jump to
                    if ((jumpAmount > 0 && temp >= targetTime) || 
                        (jumpAmount < 0 && temp <= targetTime))
                    {
                        //Move to line 
                        scintillaGateway.GotoLine(testLineNum);
                        foundMatch = true;

                        //Number of lines currently visible on the screen
                        var linesOnScreen = scintillaGateway.LinesOnScreen() - 2;

                        //Start and end locations to center testLineNum on screen
                        var start = scintillaGateway.PositionFromLine(testLineNum - (linesOnScreen / 2));
                        var end = scintillaGateway.PositionFromLine(testLineNum + (linesOnScreen / 2));
                        
                        //Scroll to the matching line, putting it in the center of the screen
                        if (findPrevIndexThenTraverseDown)
                        {
                            //If the bottom of the scroll is past the end of the document, set the end to the last line 
                            if (end < 0) { end = scintillaGateway.PositionFromLine(scintillaGateway.GetLineCount()); }
                            
                            scintillaGateway.ScrollRange(end, start);
                        }
                        else
                        {
                            //If the top of the scroll is before the beginning of the document, set the beginning to the first line 
                            if (start < 0) { start = 0; }

                            scintillaGateway.ScrollRange(start, end);
                        }

                        break;
                    }
                }

                //Perform increment/decrement here so the same loop can be used in both directions
                testLineNum = testLineNum + (findPrevIndexThenTraverseDown ? 1 : -1);
            }

            if (!foundMatch)
            {
                if (findPrevIndexThenTraverseDown)
                {
                    //MessageBox.Show($"Target timestamp ({targetTime}) not found after current line.");
                    scintillaGateway.GotoLine(scintillaGateway.GetLineCount());
                } 
                else
                {
                    //MessageBox.Show($"Target timestamp ({targetTime}) not found before current line.");
                    scintillaGateway.GotoLine(0);
                }
            }
        }

        //Builds an index of up to ~1000 timestamps in the file, evenly spaced by line count
        internal static void indexTimestamps()
        {
            int lineCount = scintillaGateway.GetLineCount();
            int stepSize = 1000;

            //If more than 20 million lines (~2GB), set the step size such that there won't be more thank 20k steps
            if (lineCount > 2E7)
            {
                stepSize = lineCount / 20000;
            }
            
            //Number of loops ranges from 1 to 20k depending on the number of lines
            for (int lineNum = 0; lineNum <= lineCount; lineNum += stepSize)
            {
                DateTime temp = new DateTime();
                int lookAheadLine = lineNum;

                bool matchFound = true;
                //Check for a match on this line. If not found, keep moving forward until one is found
                while (!getTimestampFromLineNumber(lookAheadLine, regex, ref temp))
                {
                    lookAheadLine++;

                    //Don't look ahead past the current step
                    if (lookAheadLine >= lineNum + stepSize) {
                        matchFound = false;
                        break; 
                    }
                }

                if (matchFound && !timestampIndex.Keys.Contains(temp)) { timestampIndex.Add(temp, lookAheadLine); }
            }
        }

        //Looks for a timestamp-like string on the specified line number and parses to a DateTime. Returns a boolean indicating whether a match was found. 
        internal static bool getTimestampFromLineNumber(int lineNum, Regex matchExpression, ref DateTime outTimestamp)
        {
            string lineText = scintillaGateway.GetLine(lineNum);

            //If reading a CSV log, split on the delimiter and take the correct offset
            if (csvLog)
            {
                StringReader sr = new StringReader(lineText);
                using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(sr))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(csvDelim.ToString());

                    try
                    {
                        string[] fields = parser.ReadFields();

                        if (null != fields && fields.Length > csvColumn)
                        {
                            lineText = fields[csvColumn];
                        }
                    }
                    catch (Exception ex)
                    {
                        //Do nothing to avoid spamming the user with errors
                    }
                }
                sr.Close();
            }

            if (matchExpression.IsMatch(lineText))
            {
                string timestamp;

                try
                {
                    //Found a match, extract the timestamp text
                    timestamp = matchExpression.Matches(lineText)[0].Groups[1].Value;

                    outTimestamp = DateTime.Parse(timestamp);
                    return true;
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"Unable to parse string to timestamp: {timestamp}. Exception: {ex.Message}");
                    return false;
                }
            } 
            else
            {
                return false;
            }
        }

        internal static void myMenuFunction()
        {
            MessageBox.Show("Hello N++!");
        }

        internal static void ShowLogJumperDialog()
        {
            if (frmMyDlg == null)
            {
                frmMyDlg = new frmMyDlg();

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = frmMyDlg.Handle;
                _nppTbData.pszName = "Log Jumper";
                _nppTbData.dlgID = idMyDlg;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_BOTTOM | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);

                //Set background color of the form to match the background color of the first line in the file
                Colour sciBackground = scintillaGateway.StyleGetBack(scintillaGateway.GetStyleAt(0));
                Colour sciForeground = scintillaGateway.StyleGetFore(scintillaGateway.GetStyleAt(0));
                
                Color backColor = Color.FromArgb(sciBackground.Red, sciBackground.Green, sciBackground.Blue);
                Color foreColor  = Color.FromArgb(sciForeground.Red, sciForeground.Green, sciForeground.Blue);

                frmMyDlg.BackColor = backColor;
                frmMyDlg.ForeColor = foreColor;

                SetFormColors(frmMyDlg, foreColor);

                //Build the timestamp index for more efficient timestamp searching
                indexTimestamps();
            }
            else
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_DMMSHOW, 0, frmMyDlg.Handle);
            }
        }

        private static void SetFormColors(Control parent, Color color)
        {
            foreach (Control c in parent.Controls)
            {                
                if (c is LinkLabel)
                {
                    (c as LinkLabel).LinkColor = color;
                }
                else if (c is Button)
                {
                    (c as Button).FlatAppearance.BorderColor = color;
                }
                else
                {
                    //Recursively call to get all children
                    SetFormColors(c, color);
                }
            }
        }
    }
}