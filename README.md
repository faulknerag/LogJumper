# LogJumper
LogJumper is a Notepad++ extension for quickly navigating timestamped log and CSV files.

LogJumper adds a navigation window with buttons to quickly jump forward and backward in time from the current cursor line. Jumps can be made in common time increments—1/6/12 hours or 1/10/30 minutes or seconds:
![image](https://user-images.githubusercontent.com/20804273/193426634-d153d59b-5033-44db-a449-cffe5753d046.png)

The navigation window can be opened either by clicking the plugin's icon in the toolbar, or by clicking Plugins > LogJumper > *Log Jumper Navigation Window*:  
![image](https://user-images.githubusercontent.com/20804273/193427228-0688a1c2-4f41-40d9-8b5c-2bc438f48315.png)

# Settings  
Press the 'Settings' button to configure the plugin:
![image](https://user-images.githubusercontent.com/20804273/193427043-62ae03c9-1d35-41fa-b519-6bea905f43a8.png)

Enter a valid expression in the 'Timestamp RegEx' field to identify the log file's timestamps. Press 'Save and Verify' to persist and use the new expression. 

If the log file is CSV formatted, select the 'CSV Log Format' checkbox and enter the CSV Delimiter character and Column number which contains the timestamp field. 

Set the 'Log Reads In Reverse Chronological Order' checkbox if the latest timestamps occur at the beginning of the file and the earliest timestamps occur at the end of the file. 

# Build and Installation Instructions
1. Download and build the repository. Use the same bitness for the build as your Notepad++ installation. 
2. Confirm the build successfully created the plugin directory, or manually create it:
    * For 32-bit Notepad++: C:\Program Files (x86)\Notepad++\plugins\LogJumper
    * For 64-bit Notepad++: C:\Program Files\Notepad++\plugins\LogJumper
3. If necessary, copy the build dll (LogJumper.dll) to the Notepad++ extension folder created in step 2. 
4. Start or restart Notepad++

# Attributions
This plugin uses the NppPlugin .NET package by kbilsted:

https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net Licensed under GPL v3
