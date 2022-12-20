using Halo_Infinite_Tag_Editor.InfiniteRuntimeTagViewer.Controls;
using HavokScriptToolsCommon;
using InfiniteModuleEditor;
using InfiniteRuntimeTagViewer;
using InfiniteRuntimeTagViewer.Halo;
using InfiniteRuntimeTagViewer.Halo.TagObjects;
using InfiniteRuntimeTagViewer.Interface.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using TagBlock = InfiniteRuntimeTagViewer.Interface.Controls.TagBlock;

namespace Halo_Infinite_Tag_Editor
{
    public partial class MainWindow : Window
    {
        // -HALO INFINITE TAG EDITOR-
        // 
        // I spent a great deal of time learning how Krevil's Infinite Module Editor (IME) works,
        // as well as working on Gamergottens IRTV. So, I decided to attempt to combine the two. IRTVs,
        // structures and tag reading is pretty much perfected, while IME can open and view all tags within
        // modules. So, I decied to combine the two to make this. Credits for the two repositories are below.
        // I will try to credit everything I didn't personally write as to not upset those who spent their 
        // time building some pretty awesome programs.
        //
        // Also, credit to soupstream and their Havok Script Disassembler. I decided to add it in here so we
        // no long have to extract tags with havok scripts to read them. Credit will be given on the tab as well.
        //
        // Krevil's Infinite Module Editor: https://github.com/Krevil/InfiniteModuleEditor
        // Gamergotten's Infinite Runtime Tag Viewer: https://github.com/Gamergotten/Infinite-runtime-tagviewer
        // Krevil's fork of Crauzer's OodleSharp: https://github.com/Krevil/OodleSharp
        // Soupstream's Havok-Script-Tools: https://github.com/soupstream/havok-script-tools
        //
        // Everything used from other projects will be placed in their own seperate folders, unless it's impossible
        // or more difficult to do so.

        // Used to reference in other classes.
        public static MainWindow? instance;

        public MainWindow()
        {
            InitializeComponent();

            // Set instance to this window.
            instance = this;

            // Don't know how settings work yet, this will be updated in the future.
            deployPath = AppSettings.Default.DeployPath;

            // Tries to find module files in the given path.
            if (FindModuleFiles())
            {
                CreateModuleTree(null, baseFolder);
            }
            else
            {
                StatusOut("No module files were found in given path...");
            }

            // Read and store info from the text files in the Files directory.
            InhaleTagGroup();
            InhaleTagNames();
        }

        #region Window Controls
        // Anything that controls how the UI that doesn't fit in another group.
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
            RestoreButton.Visibility = Visibility.Visible;
            MaximizeButton.Visibility = Visibility.Collapsed;
        }

        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
            RestoreButton.Visibility = Visibility.Collapsed;
            MaximizeButton.Visibility = Visibility.Visible;
        }

        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void Move_Window(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void StatusOut(string message)
        {
            // Invoking dispatcher allows this to be called from other threads.
            Dispatcher.Invoke(new Action(() =>
            {
                StatusBlock.Text = message;
            }), DispatcherPriority.Background);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        #endregion

        #region Module List
        private string deployPath = "";
        private Folder baseFolder = new Folder();
        public List<string> modulePaths = new List<string>();
        
        public class Folder
        {
            public string fullPath = "";
            public string folderName = "";
            public List<Folder> subfolders = new List<Folder>();
            public List<string> files = new List<string>();
            public static List<string> filePaths = new List<string>();
            public static List<string> folderPaths = new List<string>();

            public void Search()
            {
                try
                {
                    foreach (string file in Directory.GetFiles(fullPath))
                    {
                        if (file.EndsWith("module") && !filePaths.Contains(file))
                        {
                            instance.modulePaths.Add(file);
                            files.Add(file.Split('\\').Last());
                            filePaths.Add(file);
                        }
                    }

                    foreach (string folder in Directory.GetDirectories(fullPath))
                    {
                        folderPaths.Add(folder);
                        Folder subfolder = new Folder();
                        subfolder.fullPath = folder;
                        subfolder.folderName = folder.Split('\\').Last();
                        subfolder.Search();
                        subfolders.Add(subfolder);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            public int GetCount()
            {
                return filePaths.Count;
            }
        }

        private void SetPathClick(object sender, RoutedEventArgs e)
        {
            deployPath = DeployPathBox.Text;

            baseFolder = new Folder();
            ModuleTree.Items.Clear();

            if (FindModuleFiles())
            {
                AppSettings.Default.DeployPath = deployPath;
                CreateModuleTree(null, baseFolder);
            }
            else
            {
                ModuleTree.Items.Clear();
                StatusOut("No module files were found in given path...");
            }

            GC.Collect();
        }

        private bool FindModuleFiles()
        {
            if (Directory.Exists(deployPath))
            {
                DeployPathBox.Text = deployPath;
                baseFolder.fullPath = deployPath;
                baseFolder.folderName = deployPath.Split('\\').Last();
                baseFolder.Search();

                if (baseFolder.GetCount() > 0)
                    return true;
                else
                    return false;
            }
            else
            {
                StatusOut("Unable to locate module files...");
                return false;
            }
        }

        private void CreateModuleTree(TreeViewItem? item, Folder currentFolder)
        {
            foreach (Folder folder in currentFolder.subfolders)
            {
                TreeViewItem tvFolder = new TreeViewItem();
                tvFolder.Header = folder.folderName;

                if (item == null)
                {
                    if (folder.files.Count > 0)
                    {
                        foreach (string file in folder.files)
                        {
                            TreeViewItem tvFile = new TreeViewItem();
                            tvFile.Header = file;
                            tvFile.Tag = folder.fullPath + "\\" + file;
                            tvFile.Selected += ModuleSelected;
                            ModuleTree.Items.Add(tvFile);
                        }
                    }
                    ModuleTree.Items.Add(tvFolder);
                }
                else
                {
                    if (folder.files.Count > 0)
                    {
                        foreach (string file in folder.files)
                        {
                            TreeViewItem tvFile = new TreeViewItem();
                            
                            tvFile.Header = file;
                            tvFile.Tag = folder.fullPath + "\\" + file;
                            tvFile.Selected += ModuleSelected;
                            tvFolder.Items.Add(tvFile);
                        }
                    }
                    item.Items.Add(tvFolder);
                }

                if (folder.subfolders.Count > 0)
                    CreateModuleTree(tvFolder, folder);
            }
        }
        #endregion

        #region Tag List
        public class TagFolder
        {
            public string folderName = "";
            public SortedList<string, TagData>? tags = new SortedList<string, TagData>();
            public TreeViewItem? folder;
        }

        public class TagData
        {
            public string Header = "";
            public string Tag = "";
            public string Group = "";
        }

        private SortedList<string, TagFolder> tagFolders = new SortedList<string, TagFolder>();
        private FileStream? moduleStream;
        private Module? module;
        private string modulePath = "";
        private bool moduleOpen = false;

        private void ModuleSelected(object sender, RoutedEventArgs e)
        {
            ResetTagTree();
            TreeViewItem tv = (TreeViewItem)sender;
            LoadModule((string)tv.Tag);
        }

        private void OpenModuleClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Module File (*.module)|*.module";

            if (ofd.ShowDialog() == true)
            {
                ResetTagTree();

                LoadModule(ofd.FileName);
            }
        }

        private void CloseModuleClick(object sender, RoutedEventArgs e)
        {
            if (moduleOpen)
            {
                ResetTagTree();
                StatusOut("Module closed...");
            }
            else
                StatusOut("No module is loaded...");

        }

        private void BackupModuleClick(object sender, RoutedEventArgs e)
        {
            if (moduleOpen)
            {
                try
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "Module File (*.module)|*.module";
                    sfd.FileName = "BACKUP_" + modulePath.Split("\\").Last();

                    if (sfd.ShowDialog() == true)
                    {
                        File.Copy(modulePath, sfd.FileName, true);

                    }
                    StatusOut("Backup successful: " + sfd.FileName);
                }
                catch
                {
                    StatusOut("Backup failed!");
                }
            }
            else
                StatusOut("A module must be opened to backup...");

        }

        private void SearchTagClick(object sender, RoutedEventArgs e)
        {
            int foundCount = 0;
            foreach (TagFolder folder in tagFolders.Values)
            {
                bool foundInFolder = false;

                foreach (TreeViewItem tag in folder.folder.Items)
                {
                    if (SearchBox.Text == "" || SearchBox.Text == " ")
                    {
                        foundCount++;
                        foundInFolder = true;
                        tag.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (((string)tag.Header).Contains(SearchBox.Text))
                        {
                            foundCount++;
                            foundInFolder = true;
                            tag.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            tag.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                if (!foundInFolder)
                    folder.folder.Visibility = Visibility.Collapsed;
                else
                    folder.folder.Visibility = Visibility.Visible;
            }
            StatusOut("Found " + foundCount + " matches for: \"" + SearchBox.Text + "\"");
        }

        private void ResetTagTree()
        {
            ResetTagViewer();

            if (moduleOpen || module != null)
            {
                fileStreamOpen = false;
                moduleOpen = false;

                if (moduleStream != null)
                    moduleStream.Close();

                moduleStream = null;
                module = null;

                foreach (TagFolder tf in tagFolders.Values)
                {
                    TagsTree.Items.Remove(tf.folder);
                    tf.folder = null;
                    tf.tags = null;
                }
                tagFolders.Clear();
                tagFolders = new SortedList<string, TagFolder>();
            }
            
            GC.Collect();
        }

        private async void LoadModule(string path)
        {
            try
            {
                StatusOut("Loading module...");
                List<string> folders = new();
                Task loadModule = new Task(() =>
                {
                    moduleStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    module = ModuleEditor.ReadModule(moduleStream);
                    
                    fileStreamOpen = true;
                    modulePath = path;

                    foreach (string tag in module.ModuleFiles.Keys)
                    {
                        string[] pathSplit = tag.Split("\\");
                        string head = tag;

                        if (pathSplit.Count() > 2)
                        {
                            List<string> head1 = tag.Split("\\").TakeLast(2).ToList();
                            head = head1[0] + "\\" + head1[1];
                        }

                        string group = tag.Split(".").Last();

                        TagData newTag = new()
                        {
                            Header = head,
                            Tag = tag,
                            Group = group
                        };

                        if (!tagFolders.ContainsKey(group))
                        {
                            TagFolder newFolder = new();
                            newFolder.folderName = group;
                            newFolder.tags.Add(tag, newTag);
                            tagFolders.Add(group, newFolder);

                        }
                        else
                        {
                            TagFolder tf = tagFolders[group];
                            if (!tf.tags.ContainsKey(head))
                                tf.tags.Add(tag, newTag);
                        }
                    }
                });
                loadModule.Start();
                await loadModule;
                loadModule.Dispose();

                foreach (TagFolder tf in tagFolders.Values)
                {
                    TreeViewItem tvFolder = new();
                    tvFolder.Header = tf.folderName;
                    tf.folder = tvFolder;

                    foreach (TagData tag in tf.tags.Values)
                    {
                        TreeViewItem tvTag = new TreeViewItem();
                        tvTag.Header = tag.Header;
                        tvTag.Tag = tag;
                        tvTag.ToolTip = tag.Tag;
                        tvTag.Selected += TagSelected;
                        tvFolder.Items.Add(tvTag);
                    }
                    TagsTree.Items.Add(tvFolder);
                }

                moduleOpen = true;

                StatusOut("Opened module: " + path);
            }
            catch
            {
                ResetTagTree();
                StatusOut("Failed to open module!");
            }
        }
        #endregion

        #region Tag Viewer Events
        private bool tagOpen = false;
        private bool fileStreamOpen = false;
        private bool tagOpenedFromModule = false;
        private string tagFileName = "";
        private string curTagID = "";
        private MemoryStream? tagStream;
        private FileStream? tagFileStream;
        public static ModuleFile? moduleFile;

        private void TagSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem tv = (TreeViewItem)sender;

            if (tagOpen)
            {
                ResetTagViewer();
            }

            if (fileStreamOpen)
            {
                TagData tagData = (TagData)tv.Tag;
                tagFileName = tagData.Tag.Replace("\0", String.Empty);
                tagStream = ModuleEditor.GetTag(module, moduleStream, tagFileName);
                moduleFile = new ModuleFile();
                module.ModuleFiles.TryGetValue(module.ModuleFiles.Keys.ToList().Find(x => x.Contains(tagFileName)), out moduleFile);
                moduleFile.Tag = ModuleEditor.ReadTag(tagStream, tagFileName.Replace("\0", String.Empty), moduleFile);
                moduleFile.Tag.Name = tagFileName;
                moduleFile.Tag.ShortName = tagFileName.Split("\\").Last();

                tagOpen = true;
                tagOpenedFromModule = true;

                BuildTagViewer();

                ReadScript(tagStream.ToArray());

                ModuleBlock.Text = modulePath.Split("\\").Last();
                TagNameBlock.Text = tagFileName.Split("\\").Last();
                TagIDBlock.Text = moduleFile.Tag.TagData[8].ToString("X2") + moduleFile.Tag.TagData[9].ToString("X2") + moduleFile.Tag.TagData[10].ToString("X2") + moduleFile.Tag.TagData[11].ToString("X2");
                DataOffsetBlock.Text = moduleFile.Tag.Header.HeaderSize.ToString();
            }
        }

        private void ReadScript(byte[] data)
        {
            try
            {
                string search = "1B4C7561510E";
                int index = 0;
                bool found = false;
                for (int i = 0; i < data.Length - 7; i++)
                {
                    string testBytes = Convert.ToHexString(data[i..(i + 6)]);

                    if (testBytes == search)
                    {
                        index = i;
                        found = true;
                        break;
                    }
                }

                // Doing it this way so any tag with a lua script is read.
                if (found)
                {
                    byte[] lua = data[index..data.Length];
                    var disassembler = new HksDisassembler(lua);
                    luaView.Text = disassembler.Disassemble();
                }
            }
            catch(Exception ex)
            {
                StatusOut("Error reading lua script: " + ex.Message);
            }
        }

        private void SaveScript(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new();
            sfd.Filter = "Lua Script (*.lua)|*.lua";
            sfd.FileName = TagNameBlock.Text.Split(".").First() + ".lua";
            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, luaView.Text);
            }
        }

        private void TagViewerSearchClick(object sender, RoutedEventArgs e)
        {
            string searchTerm = TagViewerSearchBox.Text;
            int foundCount = 0;

            foreach (KeyValuePair<string, Control> control in tagViewerControls)
            {
                if (searchTerm == "" || searchTerm == " ")
                {
                    control.Value.Visibility = Visibility.Visible;
                }
                else
                {
                    TagValueData tvd = tagValueData[control.Key];
                    if (tvd.Name.ToLower().Contains(searchTerm))
                        control.Value.Visibility = Visibility.Visible;
                    else
                        control.Value.Visibility = Visibility.Collapsed;
                }
                
            }
            StatusOut("Found " + foundCount + " matches for: \"" + SearchBox.Text + "\"");
        }

        private void OpenTagClick(object sender, RoutedEventArgs e)
        {
            if (tagOpen)
            {
                ResetTagViewer();
            }

            if (moduleOpen)
            {
                ResetTagTree();
            }

            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == true)
            {
                ReadScript(File.ReadAllBytes(ofd.FileName));

                tagFileStream = new FileStream(ofd.FileName, FileMode.Open);
                moduleFile = new ModuleFile();
                moduleFile.Tag = ModuleEditor.ReadTag(tagFileStream, ofd.SafeFileName);
                moduleFile.Tag.Name = ofd.FileName;
                tagFileName = ofd.FileName;
                moduleFile.Tag.ShortName = tagFileName.Split("\\").Last();
                
                tagOpen = true;
                tagOpenedFromModule = false;
                curTagID = moduleFile.Tag.TagData[8].ToString("X2") + moduleFile.Tag.TagData[9].ToString("X2") + moduleFile.Tag.TagData[10].ToString("X2") + moduleFile.Tag.TagData[11].ToString("X2");

                ModuleBlock.Text = modulePath.Split("\\").Last();
                TagNameBlock.Text = tagFileName.Split("\\").Last();
                TagIDBlock.Text = curTagID;
                DataOffsetBlock.Text = moduleFile.Tag.Header.HeaderSize.ToString();

                BuildTagViewer();

                StatusOut("Tag opened from file: " + ofd.FileName);
            }
        }

        private void SaveTagClick(object sender, RoutedEventArgs e)
        {
            if (tagOpenedFromModule)
            {
                if (ModuleEditor.WriteTag(moduleFile, tagStream, moduleStream))
                {
                    StatusOut("Tag successfully saved to module!");
                }
                else
                {
                    StatusOut("Error saving tag to module!");
                }
            }
            else
            {
                StatusOut("Saving tag file not yet supported!");
            }
        }

        private void ImportTagClick(object sender, RoutedEventArgs e)
        {
            if (tagOpen)
            {
                OpenFileDialog ofd = new OpenFileDialog();

                if (ofd.ShowDialog() == true)
                {
                    FileStream fs = new FileStream(ofd.FileName, FileMode.Open);
                    byte[] data = new byte[fs.Length];
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Read(data, 0, (int)fs.Length);

                    tagStream.Seek(0, SeekOrigin.Begin);
                    tagStream.Write(data, 0, data.Length);

                    StatusOut("Tag imported: " + ofd.FileName);
                }
            }
            else
            {
                StatusOut("A tag needs to be open to overwrite...");
            }
            
        }

        private void ExportTagClick(object sender, RoutedEventArgs e)
        {
            if (tagOpen)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = tagFileName.Split("\\").Last();

                if (sfd.ShowDialog() == true)
                {
                    FileStream outputStream = new FileStream(sfd.FileName, FileMode.Create);
                    tagStream.Seek(0, SeekOrigin.Begin);
                    tagStream.CopyTo(outputStream);
                    outputStream.Close();
                    StatusOut("Tag exported: " + sfd.FileName);
                }
            }
            else
            {
                StatusOut("A tag needs to be open to export...");
            }
        }

        private void CloseTagClick(object sender, RoutedEventArgs e)
        {
            if (tagOpen)
            {
                ResetTagViewer();
                StatusOut("Tag closed...");
            }
            else
            {
                StatusOut("A tag isn't open...");
            }
        }

        private void ResetTagViewer()
        {
            if (tagOpen)
            {
                if (tagOpenedFromModule)
                {
                    tagStream.Close();
                }
                else
                {
                    tagFileStream.Close();
                }
                
                tagOpen = false;
                tagOpenedFromModule = false;
                ModuleBlock.Text = "";
                TagNameBlock.Text = "";
                TagIDBlock.Text = "";
                DataOffsetBlock.Text = "";
                TagViewer.Children.Clear();
                tagViewerControls.Clear();
                tagViewerControls = new();
                tagValueData.Clear();
                tagValueData = new();
                curDataBlockInd = 1;
                tagStream = null;
                tagFileStream = null;
                moduleFile = null;
                luaView.Clear();
            }
        }
        
        private void DumpTagClick(object sender, RoutedEventArgs e)
        {
            // Setup
            string curTagGroup = moduleFile.Tag.ShortName.Split(".")[1];
            curDataBlockInd = 1;
            IRTV_TagStruct tagStruct = new()
            {
                Datnum = "",
                ObjectId = curTagID,
                TagGroup = tagGroups[curTagGroup],
                TagData = 0,
                TagTypeDesc = "",
                TagFullName = moduleFile.Tag.Name,
                TagFile = moduleFile.Tag.ShortName,
                unloaded = false
            };
            Dictionary<long, TagLayouts.C> tagDefinitions = TagLayouts.Tags(tagStruct.TagGroup);

            // Initialize json lines
            List<string> json = new();

            // Header
            json.Add("{");
            json.Add("  \"TagName\": \"" + tagFileName + "\",");
            json.Add("  \"Data\": {");

            // Run through every control
            foreach (string line in GetJsonData(tagDefinitions, 0, 0, curTagID + ":", 4))
            {
                json.Add(line);
            }
                
            // End
            json.Add("  }");
            json.Add("}");

            // Save File
            SaveFileDialog sfd = new();
            sfd.Filter = "Json (*.json)|*.json";
            sfd.FileName = TagNameBlock.Text.Split(".").First();
            if (sfd.ShowDialog() == true)
            {
                File.WriteAllLines(sfd.FileName, json);
            }
        }

        private List<string> GetJsonData(Dictionary<long, TagLayouts.C> tagDefinitions, long address, long startingTagOffset, string offsetChain,int indentCount)
        {
            KeyValuePair<long, TagLayouts.C> prevEntry = new();
            List<string> result = new();
            string indent = "";
            int current = 0;

            for (int i = 0; i < indentCount; i++)
                indent += " ";

            foreach (KeyValuePair<long, TagLayouts.C> entry in tagDefinitions)
            {
                entry.Value.MemoryAddress = address + entry.Key;
                entry.Value.AbsoluteTagOffset = offsetChain + "," + (entry.Key + startingTagOffset);
                
                string name = "";
                if (entry.Value.N != null)
                    name = entry.Value.N;

                if (!name.Contains("generated_pad"))
                {
                    try
                    {
                        if (entry.Value.T == "Comment")
                        {
                            if (prevEntry.Value != null)
                            {
                                if (entry.Value.T != prevEntry.Value.T && entry.Value.N != prevEntry.Value.N)
                                {
                                    string line = indent + "\"" + entry.Value.T + "\": \"" + entry.Value.N + "\"";

                                    if (current < tagDefinitions.Count - 1)
                                        result.Add(line + ",");
                                    else
                                        result.Add(line);
                                }
                            }
                        }
                        else if (entry.Value.T == "Byte")
                        {
                            string value = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)[0].ToString();
                            string line = indent + "\"" + entry.Value.N + "\": " + value;

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "2Byte")
                        {
                            string value = BitConverter.ToInt16(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)).ToString();
                            string line = indent + "\"" + entry.Value.N + "\": " + value;

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "4Byte")
                        {
                            string value = BitConverter.ToInt32(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)).ToString();
                            string line = indent + "\"" + entry.Value.N + "\": " + value;

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "Float")
                        {
                            string value = BitConverter.ToSingle(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)).ToString("0.000");
                            string line = indent + "\"" + entry.Value.N + "\": " + value;

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "Pointer")
                        {
                            string value = "0x" + BitConverter.ToInt64(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)).ToString("X");
                            string line = indent + "\"" + entry.Value.N + "\": \"" + value + "\"";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "mmr3Hash")
                        {
                            string value = BitConverter.ToUInt32(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)).ToString("XXXXXXXX");
                            string line = indent + "\"" + entry.Value.N + "\": \"" + value + "\"";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "String")
                        {
                            string value = Encoding.UTF8.GetString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)).Split('\0').First();
                            string line = indent + "\"" + entry.Value.N + "\": \"" + value + "\"";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "EnumGroup")
                        {
                            TagLayouts.EnumGroup fg3 = entry.Value as TagLayouts.EnumGroup;

                            result.Add(indent + "\"" + fg3.N + "\": {");
                            // Get Value
                            int enumValueRaw = 0;
                            if (entry.Value.S == 1)
                            {
                                result.Add(indent + "  \"Type\": \"enum8\",");
                                enumValueRaw = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)[0];
                            }
                            else if (entry.Value.S == 2)
                            {
                                result.Add(indent + "  \"Type\": \"enum16\",");
                                enumValueRaw = BitConverter.ToInt16(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress));
                            }
                            else if (entry.Value.S == 4)
                            {
                                result.Add(indent + "  \"Type\": \"enum32\",");
                                enumValueRaw = BitConverter.ToInt32(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress));
                            }
                            result.Add(indent + "  \"Value\": " + enumValueRaw + ",");
                            if (fg3.STR.ContainsKey(enumValueRaw))
                                result.Add(indent + "  \"ValueName\": \"" + fg3.STR[enumValueRaw] + "\"");
                            else
                                result.Add(indent + "  \"ValueName\": \"Error\"");

                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "FlagGroup")
                        {
                            TagLayouts.FlagGroup? fg = entry.Value as TagLayouts.FlagGroup;
                            result.Add(indent + "\"" + fg.N + "\": {");

                            int enumValueRaw = 0;
                            if (entry.Value.S == 1)
                            {
                                result.Add(indent + "  \"Type\": \"flags8\",");
                                enumValueRaw = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress)[0];
                            }
                            else if (entry.Value.S == 2)
                            {
                                result.Add(indent + "  \"Type\": \"flags16\",");
                                enumValueRaw = BitConverter.ToInt16(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress));
                            }
                            else if (entry.Value.S == 4)
                            {
                                result.Add(indent + "  \"Type\": \"flags32\",");
                                enumValueRaw = BitConverter.ToInt32(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress));
                            }

                            result.Add(indent + "  \"Value\": " + enumValueRaw + ",");

                            result.Add(indent + "  \"Flags\": {");

                            Dictionary<string, bool> values = GetFlagsFromBits(fg.A, fg.MB, GetDataFromFile(fg.A, entry.Value.MemoryAddress), fg.STR);
                            int i = 1;
                            foreach (KeyValuePair<string, bool> kvp in values)
                            {
                                if (i != values.Count)
                                    result.Add(indent + "    \"" + kvp.Key + "\": \"" + kvp.Value + "\",");
                                else
                                    result.Add(indent + "    \"" + kvp.Key + "\": \"" + kvp.Value + "\"");
                                i++;
                            }
                            result.Add(indent + "  }");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "BoundsFloat")
                        {
                            string value1 = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 0).ToString("0.000");
                            string value2 = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 4).ToString("0.000");

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"Min\": " + value1 + ",");
                            result.Add(indent + "  \"Max\": " + value2 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "Bounds2Byte")
                        {
                            string value1 = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 0).ToString();
                            string value2 = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 2).ToString();

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"Min\": " + value1 + ",");
                            result.Add(indent + "  \"Max\": " + value2 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "2DPoint_Float")
                        {
                            string value1 = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 0).ToString("0.000");
                            string value2 = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 4).ToString("0.000");

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"X\": " + value1 + ",");
                            result.Add(indent + "  \"Y\": " + value2 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "2DPoint_2Byte")
                        {
                            string value1 = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 0).ToString();
                            string value2 = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 2).ToString();

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"X\": " + value1 + ",");
                            result.Add(indent + "  \"Y\": " + value2 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "3DPoint")
                        {
                            string value1 = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 0).ToString("0.000");
                            string value2 = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 4).ToString("0.000");
                            string value3 = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 8).ToString("0.000");

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"X\": " + value1 + ",");
                            result.Add(indent + "  \"Y\": " + value2 + ",");
                            result.Add(indent + "  \"Z\": " + value3 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "Quanternion")
                        {
                            string value1 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0).ToString("0.000");
                            string value2 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4).ToString("0.000");
                            string value3 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8).ToString("0.000");
                            string value4 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12).ToString("0.000");

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"W\": " + value1 + ",");
                            result.Add(indent + "  \"X\": " + value2 + ",");
                            result.Add(indent + "  \"Y\": " + value3 + ",");
                            result.Add(indent + "  \"Z\": " + value4 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "3DPlane")
                        {
                            string value1 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0).ToString("0.000");
                            string value2 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4).ToString("0.000");
                            string value3 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8).ToString("0.000");
                            string value4 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12).ToString("0.000");

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"X\": " + value1 + ",");
                            result.Add(indent + "  \"Y\": " + value2 + ",");
                            result.Add(indent + "  \"Z\": " + value3 + ",");
                            result.Add(indent + "  \"P\": " + value4 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "RGB")
                        {
                            byte r_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 0) * 255);
                            byte g_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 4) * 255);
                            byte b_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 8) * 255);
                            string hex_color = r_hex.ToString("X2") + g_hex.ToString("X2") + b_hex.ToString("X2");

                            string value1 = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 0).ToString("0.000");
                            string value2 = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 4).ToString("0.000");
                            string value3 = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 8).ToString("0.000");

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"Hex\": \"#" + hex_color + "\",");
                            result.Add(indent + "  \"R\": " + value1 + ",");
                            result.Add(indent + "  \"G\": " + value2 + ",");
                            result.Add(indent + "  \"B\": " + value3 + "");
                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "ARGB")
                        {
                            byte a_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0) * 255);
                            byte r_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4) * 255);
                            byte g_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8) * 255);
                            byte b_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12) * 255);
                            string hex_color = a_hex.ToString("X2") + r_hex.ToString("X2") + g_hex.ToString("X2") + b_hex.ToString("X2");

                            string value1 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0).ToString("0.000");
                            string value2 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4).ToString("0.000");
                            string value3 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8).ToString("0.000");
                            string value4 = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12).ToString("0.000");

                            result.Add(indent + "\"" + entry.Value.N + "\": {");
                            result.Add(indent + "  \"Hex\": \"#" + hex_color + "\",");
                            result.Add(indent + "  \"A\": " + value1 + ",");
                            result.Add(indent + "  \"R\": " + value2 + ",");
                            result.Add(indent + "  \"G\": " + value3 + ",");
                            result.Add(indent + "  \"B\": " + value4 + "");

                            string line = indent + "}";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else if (entry.Value.T == "TagRef")
                        {
                            string tagId = Convert.ToHexString(GetDataFromFile(4, entry.Value.MemoryAddress + 8));
                            string tagName = IDToTagName(tagId);
                            byte[] tagGroupData = GetDataFromFile(4, entry.Value.MemoryAddress + 20);
                            string tagGroup = "Null";
                            if (Convert.ToHexString(tagGroupData) != "FFFFFFFF")
                                tagGroup = ReverseString(Encoding.UTF8.GetString(tagGroupData));

                            if (tagId == "FFFFFFFF" && tagGroup == "Null")
                            {
                                string line = indent + "\"" + entry.Value.N + "\": \"Null\"";
                                if (current < tagDefinitions.Count - 1)
                                    result.Add(line + ",");
                                else
                                    result.Add(line);
                            }
                            else
                            {
                                result.Add(indent + "\"" + entry.Value.N + "\": {");
                                result.Add(indent + "  \"TagGroup\": \"" + tagGroup + "\",");
                                result.Add(indent + "  \"TagID\": \"" + tagId + "\",");
                                result.Add(indent + "  \"TagName\": \"" + tagName + "\"");

                                string line = indent + "}";
                                if (current < tagDefinitions.Count - 1)
                                    result.Add(line + ",");
                                else
                                    result.Add(line);
                            }
                        }
                        else if (entry.Value.T == "Tagblock")
                        {
                            int blockIndex = 0;
                            int childCount = BitConverter.ToInt32(GetDataFromFile(20, entry.Value.MemoryAddress), 16);

                            if (childCount > 0 && childCount < 100000)
                            {
                                result.Add(indent + "\"" + entry.Value.N + "\": [");

                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;

                                for (int i = 0; i < childCount; i++)
                                {
                                    long newAddress = (long)moduleFile.Tag.DataBlockArray[blockIndex].Offset - (long)moduleFile.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);
                                    result.Add(indent + "  {");
                                    result.Add(indent + "    \"Index\": " + i);
                                    foreach (string line in GetJsonData(entry.Value.B, newAddress, newAddress + entry.Value.S * i, entry.Value.AbsoluteTagOffset, indentCount + 4))
                                    {
                                        result.Add(line);
                                    }
                                    if (i == childCount - 1)
                                        result.Add(indent + "  }");
                                    else
                                        result.Add(indent + "  },");
                                }

                                string newLine = indent + "]";
                                if (current < tagDefinitions.Count - 1)
                                    result.Add(newLine + ",");
                                else
                                    result.Add(newLine);
                            }
                            else
                            {
                                string line = indent + "\"" + entry.Value.N + "\": []";

                                if (current < tagDefinitions.Count - 1)
                                    result.Add(line + ",");
                                else
                                    result.Add(line);
                            }
                        }
                        else if (entry.Value.T == "FUNCTION")
                        {
                            int blockIndex = 0;
                            int childCount = BitConverter.ToInt32(GetDataFromFile(20, entry.Value.MemoryAddress), 16);

                            childCount = BitConverter.ToInt32(GetDataFromFile(4, entry.Value.MemoryAddress + 20));
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;
                            }

                            string line = indent + "\"" + entry.Value.N + "\": []";

                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                        else
                        {
                            string line = indent + "\"Type not supported\": " + "\"" + entry.Value.T + "\"";
                            if (current < tagDefinitions.Count - 1)
                                result.Add(line + ",");
                            else
                                result.Add(line);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                
                prevEntry = entry;
                current++;
            }
            return result;
        }

        private Dictionary<string, bool> GetFlagsFromBits(int amountOfBytes, int maxBit, byte[] data, Dictionary<int, string>? descriptions = null)
        {
            Dictionary<string, bool> values = new();

            if (maxBit == 0)
            {
                maxBit = maxBit = amountOfBytes * 8;
            }

            int maxAmountOfBytes = Math.Clamp((int)Math.Ceiling((double)maxBit / 8), 0, amountOfBytes);
            int bitsLeft = maxBit - 1; // -1 to start at 

            for (int @byte = 0; @byte < maxAmountOfBytes; @byte++)
            {
                if (bitsLeft < 0)
                {
                    continue;
                }

                int amountOfBits = @byte * 8 > maxBit ? ((@byte * 8) - maxBit) : 8;
                byte flags_value = (byte)data[@byte];

                for (int bit = 0; bit < amountOfBits; bit++)
                {
                    int currentBitIndex = (@byte * 8) + bit;
                    if (bitsLeft < 0)
                    {
                        continue;
                    }

                    int _byte = @byte, _bit = bit;

                    if (descriptions != null && descriptions.ContainsKey(currentBitIndex))
                    {
                        bool value = flags_value.GetBit(bit);
                        string description = descriptions[(@byte * 8) + bit];
                        values.Add(description, value);
                    }
                    bitsLeft--;
                }
            }

            return values;
        }
        #endregion

        #region Tag Viewer Controls
        public class TagValueData
        {
            public string Name = "";
            public string? Type;
            public string? ControlType;
            public long Offset = 0;
            public string? OffsetChain;
            public byte[]? Value;
            public long? Size;
            public int DataBlockIndex = 0;
            public int ChildCount = 0;
        }

        private Dictionary<string, TagValueData> tagValueData = new();
        private Dictionary<string, Control> tagViewerControls = new();
        private int curDataBlockInd = 1;

        private async void BuildTagViewer()
        {
            try
            {
                string curTagGroup = moduleFile.Tag.ShortName.Split(".")[1];
                curDataBlockInd = 1;

                if (tagGroups.ContainsKey(curTagGroup.Trim()))
                {
                    StatusOut("Retrieving tag data...");

                    IRTV_TagStruct tagStruct = new()
                    {
                        Datnum = "",
                        ObjectId = curTagID,
                        TagGroup = tagGroups[curTagGroup],
                        TagData = 0,
                        TagTypeDesc = "",
                        TagFullName = moduleFile.Tag.Name,
                        TagFile = moduleFile.Tag.ShortName,
                        unloaded = false
                    };

                    Dictionary<long, TagLayouts.C> tagDefinitions = TagLayouts.Tags(tagStruct.TagGroup);
                    Task loadTag = new Task(() =>
                    {
                        GetTagValueData(tagDefinitions, 0, 0, curTagID + ":");
                    });

                    loadTag.Start();
                    await loadTag;
                    loadTag.Dispose();
                    
                    StatusOut("Building tag viewer...");
                    CreateTagControls(tagStruct, 0, tagDefinitions, tagStruct.TagData, TagViewer, curTagID + ":", null, false);
                    StatusOut("Opened tag from module: " + tagFileName.Split("\\").Last());

                    GC.Collect();
                }
                else
                {
                    StatusOut("Tag group name missing...");
                }
            }
            catch (Exception ex)
            {
                StatusOut("Error loading tag: " + ex.Message);
            }
            
        }

        private void GetTagValueData(Dictionary<long, TagLayouts.C> tagDefinitions, long address, long startingTagOffset, string offsetChain)
        {
            foreach (KeyValuePair<long, TagLayouts.C> entry in tagDefinitions)
            {
                entry.Value.MemoryAddress = address + entry.Key;
                entry.Value.AbsoluteTagOffset = offsetChain + "," + (entry.Key + startingTagOffset);
                try
                {
                    string name = "";
                    if (entry.Value.N != null)
                        name = entry.Value.N;
                    TagValueData curTagData = new()
                    {
                        Name = name,
                        ControlType = entry.Value.T,
                        Type = entry.Value.T,
                        OffsetChain = entry.Value.AbsoluteTagOffset,
                        Offset = entry.Value.MemoryAddress,
                        Value = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress),
                        Size = (int)entry.Value.S,
                        ChildCount = 0,
                        DataBlockIndex = 0
                    };

                    if (entry.Value.T == "Tagblock" || entry.Value.T == "FUNCTION")
                    {
                        int blockIndex = 0;
                        int childCount = BitConverter.ToInt32(GetDataFromFile(20, entry.Value.MemoryAddress), 16); ;

                        if (curTagData.Type == "Tagblock" && childCount < 100000)
                        {
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;
                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;

                                for (int i = 0; i < childCount; i++)
                                {
                                    long newAddress = (long)moduleFile.Tag.DataBlockArray[curTagData.DataBlockIndex].Offset - (long)moduleFile.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);
                                    GetTagValueData(entry.Value.B, newAddress , newAddress + entry.Value.S * i, curTagData.OffsetChain);
                                }
                            }
                        }
                        else if (curTagData.Type == "FUNCTION")
                        {
                            curTagData.ChildCount = childCount;
                            childCount = BitConverter.ToInt32(GetDataFromFile(4, entry.Value.MemoryAddress + 20));
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;

                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;
                            }
                            
                        }
                    }

                    tagValueData.Add(curTagData.OffsetChain, curTagData);
                }
                catch (Exception ex)
                {
                    StatusOut("Error loading getting tag data: " + ex.Message);
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void CreateTagControls(IRTV_TagStruct tagStruct, long startingTagOffset, Dictionary<long, TagLayouts.C> tagDefinitions, long address, StackPanel parentpanel, string offsetChain, TagBlock? tb, bool isTagBlock)
        {
            KeyValuePair<long, TagLayouts.C> prevEntry = new();

            foreach (KeyValuePair<long, TagLayouts.C> entry in tagDefinitions)
            {
                entry.Value.MemoryAddress = address + entry.Key;
                entry.Value.AbsoluteTagOffset = offsetChain + "," + (entry.Key + startingTagOffset);

                TagValueData tvd = new();

                if (tagValueData.ContainsKey(entry.Value.AbsoluteTagOffset))
                {
                    tvd = tagValueData[entry.Value.AbsoluteTagOffset];

                    try
                    {
                        if (!tvd.Name.ToLower().Contains("generated_pad"))
                        {
                            if (tvd.ControlType == "Comment")
                            {
                                if (prevEntry.Value != null && prevEntry.Value.N != null && tvd.Name.ToLower().Trim() != "function")
                                {
                                    string prevName = prevEntry.Value.N;
                                    string prevType = prevEntry.Value.T;
                                    string curName = tvd.Name;

                                    prevName = (prevName.ToLower()).Replace(" ", "");
                                    curName = (curName.ToLower()).Replace(" ", "");

                                    CommentBlock? vb0 = new();
                                    vb0.Tag = "";
                                    vb0.comment.Text = tvd.Name;

                                    if (prevType == "Comment")
                                    {
                                        if (prevName != curName)
                                        {
                                            parentpanel.Children.Add(vb0);
                                            tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb0);
                                        }
                                            
                                    }
                                    else
                                    {
                                        parentpanel.Children.Add(vb0);
                                        tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb0);
                                    }
                                }
                                else
                                {
                                    CommentBlock? vb0 = new();
                                    vb0.comment.Text = tvd.Name;
                                    parentpanel.Children.Add(vb0);
                                    tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb0);
                                }
                            }
                            else if (tvd.ControlType == "Byte")
                            {
                                TagValueBlock? vb = new();
                                vb.value_name.Text = tvd.Name;
                                vb.value.Text = tvd.Value[0].ToString();
                                vb.value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.value.TextChanged += VBTextChange;
                                vb.value.ToolTip = "Type: " + tvd.Type;
                                parentpanel.Children.Add(vb);

                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "2Byte")
                            {
                                TagValueBlock? vb = new();
                                vb.value_name.Text = tvd.Name;
                                vb.value.Text = BitConverter.ToInt16(tvd.Value).ToString();
                                vb.value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.value.TextChanged += VBTextChange;
                                vb.value.ToolTip = "Type: " + tvd.Type;
                                parentpanel.Children.Add(vb);

                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "4Byte")
                            {
                                TagValueBlock? vb = new();
                                vb.value_name.Text = tvd.Name;
                                vb.value.Text = BitConverter.ToInt32(tvd.Value).ToString();
                                vb.value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.value.TextChanged += VBTextChange;
                                vb.value.ToolTip = "Type: " + tvd.Type;
                                parentpanel.Children.Add(vb);

                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "Float")
                            {
                                TagValueBlock? vb = new();
                                vb.value_name.Text = tvd.Name;
                                vb.value.Text = BitConverter.ToSingle(tvd.Value).ToString();
                                vb.value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.value.TextChanged += VBTextChange;
                                vb.value.ToolTip = "Type: " + tvd.Type;
                                parentpanel.Children.Add(vb);

                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "Pointer")
                            {
                                TagValueBlock? vb = new();
                                vb.value_name.Text = tvd.Name;
                                vb.value.Text = Convert.ToHexString(tvd.Value);
                                vb.value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.value.ToolTip = "Type: " + tvd.Type;
                                vb.value.IsReadOnly = true;
                                parentpanel.Children.Add(vb);

                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "mmr3Hash")
                            {
                                TagValueBlock? vb = new();
                                vb.value_name.Text = tvd.Name;
                                vb.value.Text = Convert.ToHexString(tvd.Value);
                                vb.value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.value.TextChanged += VBTextChange;
                                vb.value.ToolTip = "Type: " + tvd.Type;
                                parentpanel.Children.Add(vb);

                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "String")
                            {
                                TagValueBlock? vb = new();
                                vb.value_name.Text = tvd.Name;
                                vb.value.Text = Encoding.UTF8.GetString(tvd.Value);
                                vb.value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.value.TextChanged += VBTextChange;
                                vb.value.ToolTip = "Type: " + tvd.Type;
                                parentpanel.Children.Add(vb);

                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "EnumGroup")
                            {
                                if (!(entry.Value is TagLayouts.EnumGroup))
                                    continue;

                                TagLayouts.EnumGroup? fg3 = entry.Value as TagLayouts.EnumGroup;
                                EnumBlock eb = new EnumBlock();
                                eb.enums.Tag = entry.Value.AbsoluteTagOffset;
                                eb.enums.SelectionChanged += EnumBlockChanged;

                                foreach (KeyValuePair<int, string> gvsdahb in fg3.STR)
                                {
                                    ComboBoxItem cbi = new() { Content = gvsdahb.Value };
                                    eb.enums.Items.Add(cbi);
                                }

                                if (fg3.A == 1)
                                {
                                    int test_this = (int)tvd.Value[0];
                                    if (eb.enums.Items.Count >= test_this)
                                    {
                                        eb.enums.SelectedIndex = test_this;
                                    }
                                }
                                else if (fg3.A == 2)
                                {
                                    int test_this = BitConverter.ToInt16(tvd.Value);
                                    if (eb.enums.Items.Count >= test_this)
                                    {
                                        eb.enums.SelectedIndex = test_this;
                                    }
                                }
                                else if (fg3.A == 4)
                                {
                                    int test_this = (int)BitConverter.ToInt32(tvd.Value);
                                    if (eb.enums.Items.Count >= test_this)
                                    {
                                        eb.enums.SelectedIndex = test_this;
                                    }
                                }
                                eb.value_name.Text = fg3.N;
                                
                                parentpanel.Children.Add(eb);
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, eb);
                            }
                            else if (tvd.ControlType == "Flags")
                            {
                                byte flags_value = (byte)tvd.Value[0];

                                TagsFlags? vb = new();
                                vb.Tag = entry.Value.AbsoluteTagOffset;
                                vb.flag1.IsChecked = flags_value.GetBit(0);
                                vb.flag2.IsChecked = flags_value.GetBit(1);
                                vb.flag3.IsChecked = flags_value.GetBit(2);
                                vb.flag4.IsChecked = flags_value.GetBit(3);
                                vb.flag5.IsChecked = flags_value.GetBit(4);
                                vb.flag6.IsChecked = flags_value.GetBit(5);
                                vb.flag7.IsChecked = flags_value.GetBit(6);
                                vb.flag8.IsChecked = flags_value.GetBit(7);
                                vb.flag1.Click += FlagGroupChange;
                                vb.flag2.Click += FlagGroupChange;
                                vb.flag3.Click += FlagGroupChange;
                                vb.flag4.Click += FlagGroupChange;
                                vb.flag5.Click += FlagGroupChange;
                                vb.flag6.Click += FlagGroupChange;
                                vb.flag7.Click += FlagGroupChange;
                                vb.flag8.Click += FlagGroupChange;
                                parentpanel.Children.Add(vb);
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                            }
                            else if (tvd.ControlType == "FlagGroup")
                            {
                                if (!(entry.Value is TagLayouts.FlagGroup))
                                    continue;

                                TagLayouts.FlagGroup? fg = entry.Value as TagLayouts.FlagGroup;
                                TagFlagsGroup? vb = new();
                                vb.Tag = entry.Value.AbsoluteTagOffset;
                                vb.flag_name.Text = fg.N;
                                vb.generateBitsFromFile(fg.A, fg.MB, GetDataFromFile(fg.A, entry.Value.MemoryAddress), fg.STR);
                                vb.FlagToggled += FlagGroupChange;
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "BoundsFloat")
                            {
                                tvd.Type = "Float";

                                TagTwoBlock? vb = new(30);
                                vb.f_name.Text = tvd.Name;
                                vb.f_label1.Text = "Min:";
                                vb.f_label2.Text = "Max:";
                                vb.f_value1.Text = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 0).ToString();
                                vb.f_value2.Text = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 4).ToString();
                                vb.f_value1.TextChanged += VBTextChange;
                                vb.f_value2.TextChanged += VBTextChange;
                                vb.f_value1.ToolTip = "Type: " + tvd.Type;
                                vb.f_value2.ToolTip = "Type: " + tvd.Type;
                                vb.f_value1.Tag = entry.Value.AbsoluteTagOffset;
                                vb.f_value2.Tag = entry.Value.AbsoluteTagOffset + "-4";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);

                            }
                            else if (tvd.ControlType == "Bounds2Byte")
                            {
                                tvd.Type = "2Byte";

                                TagTwoBlock? vb = new(30);
                                vb.f_name.Text = tvd.Name;
                                vb.f_label1.Text = "Min:";
                                vb.f_label2.Text = "Max:";
                                vb.f_value1.Text = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 0).ToString();
                                vb.f_value2.Text = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 2).ToString();
                                vb.f_value1.TextChanged += VBTextChange;
                                vb.f_value2.TextChanged += VBTextChange;
                                vb.f_value1.ToolTip = "Type: " + tvd.Type;
                                vb.f_value2.ToolTip = "Type: " + tvd.Type;
                                vb.f_value1.Tag = entry.Value.AbsoluteTagOffset;
                                vb.f_value2.Tag = entry.Value.AbsoluteTagOffset + "-2";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "2DPoint_Float")
                            {
                                tvd.Type = "Float";

                                TagTwoBlock? vb = new(13);
                                vb.f_name.Text = entry.Value.N;
                                vb.f_label1.Text = "X:";
                                vb.f_label2.Text = "Y:";
                                vb.f_value1.Text = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 0).ToString();
                                vb.f_value2.Text = BitConverter.ToSingle(GetDataFromFile(8, entry.Value.MemoryAddress), 4).ToString();
                                vb.f_value1.TextChanged += VBTextChange;
                                vb.f_value2.TextChanged += VBTextChange;
                                vb.f_value1.ToolTip = "Type: " + tvd.Type;
                                vb.f_value2.ToolTip = "Type: " + tvd.Type;
                                vb.f_value1.Tag = entry.Value.AbsoluteTagOffset;
                                vb.f_value2.Tag = entry.Value.AbsoluteTagOffset + "-4";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "2DPoint_2Byte")
                            {
                                tvd.Type = "2Byte";

                                TagTwoBlock? vb = new(13);
                                vb.f_name.Text = entry.Value.N;
                                vb.f_label1.Text = "X:";
                                vb.f_label2.Text = "Y:";
                                vb.f_value1.Text = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 0).ToString();
                                vb.f_value2.Text = BitConverter.ToInt16(GetDataFromFile(4, entry.Value.MemoryAddress), 2).ToString();
                                vb.f_value1.TextChanged += VBTextChange;
                                vb.f_value2.TextChanged += VBTextChange;
                                vb.f_value1.ToolTip = "Type: " + tvd.Type;
                                vb.f_value2.ToolTip = "Type: " + tvd.Type;
                                vb.f_value1.Tag = entry.Value.AbsoluteTagOffset;
                                vb.f_value2.Tag = entry.Value.AbsoluteTagOffset + "-2";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "3DPoint")
                            {
                                tvd.Type = "Float";

                                TagThreeBlock? vb = new(13);
                                vb.f_name.Text = entry.Value.N;
                                vb.f_label1.Text = "X:";
                                vb.f_label2.Text = "Y:";
                                vb.f_label3.Text = "Z:";
                                vb.f_value1.Text = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 0).ToString();
                                vb.f_value2.Text = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 4).ToString();
                                vb.f_value3.Text = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 8).ToString();
                                vb.f_value1.TextChanged += VBTextChange;
                                vb.f_value2.TextChanged += VBTextChange;
                                vb.f_value3.TextChanged += VBTextChange;
                                vb.f_value1.ToolTip = "Type: " + tvd.Type;
                                vb.f_value2.ToolTip = "Type: " + tvd.Type;
                                vb.f_value3.ToolTip = "Type: " + tvd.Type;
                                vb.f_value1.Tag = entry.Value.AbsoluteTagOffset;
                                vb.f_value2.Tag = entry.Value.AbsoluteTagOffset + "-4";
                                vb.f_value3.Tag = entry.Value.AbsoluteTagOffset + "-8";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "Quanternion")
                            {
                                tvd.Type = "Float";

                                TagFourBlock? vb = new(13);
                                vb.f_name.Text = entry.Value.N;
                                vb.f_label1.Text = "W:";
                                vb.f_label2.Text = "X:";
                                vb.f_label3.Text = "Y:";
                                vb.f_label4.Text = "Z:";
                                vb.f_value1.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0).ToString();
                                vb.f_value2.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4).ToString();
                                vb.f_value3.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8).ToString();
                                vb.f_value4.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12).ToString();
                                vb.f_value1.TextChanged += VBTextChange;
                                vb.f_value2.TextChanged += VBTextChange;
                                vb.f_value3.TextChanged += VBTextChange;
                                vb.f_value4.TextChanged += VBTextChange;
                                vb.f_value1.ToolTip = "Type: " + tvd.Type;
                                vb.f_value2.ToolTip = "Type: " + tvd.Type;
                                vb.f_value3.ToolTip = "Type: " + tvd.Type;
                                vb.f_value4.ToolTip = "Type: " + tvd.Type;
                                vb.f_value1.Tag = entry.Value.AbsoluteTagOffset;
                                vb.f_value2.Tag = entry.Value.AbsoluteTagOffset + "-4";
                                vb.f_value3.Tag = entry.Value.AbsoluteTagOffset + "-8";
                                vb.f_value4.Tag = entry.Value.AbsoluteTagOffset + "-12";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "3DPlane")
                            {
                                tvd.Type = "Float";

                                TagFourBlock? vb = new TagFourBlock(13);
                                vb.f_name.Text = entry.Value.N;
                                vb.f_label1.Text = "X:";
                                vb.f_label2.Text = "Y:";
                                vb.f_label3.Text = "Z:";
                                vb.f_label4.Text = "P:";
                                vb.f_value1.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0).ToString();
                                vb.f_value2.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4).ToString();
                                vb.f_value3.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8).ToString();
                                vb.f_value4.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12).ToString();
                                vb.f_value1.TextChanged += VBTextChange;
                                vb.f_value2.TextChanged += VBTextChange;
                                vb.f_value3.TextChanged += VBTextChange;
                                vb.f_value4.TextChanged += VBTextChange;
                                vb.f_value1.ToolTip = "Type: " + tvd.Type;
                                vb.f_value2.ToolTip = "Type: " + tvd.Type;
                                vb.f_value3.ToolTip = "Type: " + tvd.Type;
                                vb.f_value4.ToolTip = "Type: " + tvd.Type;
                                vb.f_value1.Tag = entry.Value.AbsoluteTagOffset;
                                vb.f_value2.Tag = entry.Value.AbsoluteTagOffset + "-4";
                                vb.f_value3.Tag = entry.Value.AbsoluteTagOffset + "-8";
                                vb.f_value4.Tag = entry.Value.AbsoluteTagOffset + "-12";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "RGB")
                            {
                                tvd.Type = "Float";

                                byte r_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 0) * 255);
                                byte g_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 4) * 255);
                                byte b_hex = (byte)(BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 8) * 255);
                                string hex_color = r_hex.ToString("X2") + g_hex.ToString("X2") + b_hex.ToString("X2");

                                TagRGBBlock? vb = new();
                                vb.rgb_name.Text = entry.Value.N;
                                vb.color_hash.Text = "#" + hex_color;
                                vb.r_value.Text = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 0).ToString();
                                vb.g_value.Text = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 4).ToString();
                                vb.b_value.Text = BitConverter.ToSingle(GetDataFromFile(12, entry.Value.MemoryAddress), 8).ToString();
                                vb.rgb_colorpicker.SelectedColor = Color.FromRgb(r_hex, g_hex, b_hex);
                                vb.r_value.TextChanged += VBTextChange;
                                vb.g_value.TextChanged += VBTextChange;
                                vb.b_value.TextChanged += VBTextChange;
                                vb.r_value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.g_value.Tag = entry.Value.AbsoluteTagOffset + "-4";
                                vb.b_value.Tag = entry.Value.AbsoluteTagOffset + "-8";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "ARGB")
                            {
                                tvd.Type = "Float";

                                byte a_hex2 = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0) * 255);
                                byte r_hex2 = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4) * 255);
                                byte g_hex2 = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8) * 255);
                                byte b_hex2 = (byte)(BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12) * 255);

                                TagARGBBlock? vb = new();
                                vb.rgb_name.Text = entry.Value.N;
                                vb.argb_colorpicker.SelectedColor = Color.FromArgb(a_hex2, r_hex2, g_hex2, b_hex2);
                                vb.a_value.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 0).ToString();
                                vb.r_value.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 4).ToString();
                                vb.g_value.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 8).ToString();
                                vb.b_value.Text = BitConverter.ToSingle(GetDataFromFile(16, entry.Value.MemoryAddress), 12).ToString();
                                vb.a_value.TextChanged += VBTextChange;
                                vb.r_value.TextChanged += VBTextChange;
                                vb.g_value.TextChanged += VBTextChange;
                                vb.b_value.TextChanged += VBTextChange;
                                vb.a_value.Tag = entry.Value.AbsoluteTagOffset;
                                vb.r_value.Tag = entry.Value.AbsoluteTagOffset + "-4";
                                vb.g_value.Tag = entry.Value.AbsoluteTagOffset + "-8";
                                vb.b_value.Tag = entry.Value.AbsoluteTagOffset + "-12";
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);
                            }
                            else if (tvd.ControlType == "TagRef")
                            {
                                try
                                {
                                    byte[] data = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress);
                                    string tagId = Convert.ToHexString(GetDataFromFile(4, entry.Value.MemoryAddress + 8));
                                    string tagName = IDToTagName(tagId);
                                    byte[] tagGroupData = GetDataFromFile(4, entry.Value.MemoryAddress + 20);
                                    string tagGroup = "Null";

                                    TagInfo ti = GetTagInfo(tagId);
                                    string toolTip = "Tag ID: " + ti.TagID + "; Asset ID: " + ti.AssetID;
                                    if (Convert.ToHexString(tagGroupData) != "FFFFFFFF")
                                        tagGroup = ReverseString(Encoding.UTF8.GetString(tagGroupData));

                                    TagRefBlockFile? vb = new();
                                    vb.ToolTip = toolTip;
                                    vb.value_name.Text = entry.Value.N;
                                    vb.tag_button.Content = tagName;
                                    vb.taggroup.Text = tagGroup;
                                    vb.taggroup.IsReadOnly = true;
                                    tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                    parentpanel.Children.Add(vb);
                                }
                                catch
                                {
                                    break;
                                }
                            }
                            else if (tvd.ControlType == "Tagblock")
                            {
                                int dataOffset = 0;
                                long stringAddress = BitConverter.ToInt64(GetDataFromFile(20, entry.Value.MemoryAddress), 8);
                                int childCount = BitConverter.ToInt32(GetDataFromFile(20, entry.Value.MemoryAddress), 16);
                                string childrenCount = childCount.ToString();
                                string our_name = "";
                                if (entry.Value.N != null)
                                    our_name = entry.Value.N;

                                long tempBlockAdd = 0;

                                if (tvd.DataBlockIndex != 0)
                                    tempBlockAdd = (long)moduleFile.Tag.DataBlockArray[tvd.DataBlockIndex].Offset - (long)moduleFile.Tag.DataBlockArray[0].Offset;

                                TagBlock? vb = new(tagStruct);
                                vb.tagblock_title.Text = our_name;
                                vb.BlockAddress = tempBlockAdd;
                                vb.tagblock_address.Text = "0x" + vb.BlockAddress.ToString("X");
                                vb.tagblock_address.IsReadOnly = true;
                                vb.Children = entry;
                                vb.tagblock_count.Text = childrenCount;
                                vb.tagblock_count.IsReadOnly = true;
                                vb.stored_num_on_index = 0;
                                vb.size = (int)entry.Value.S;

                                if (childCount > 0)
                                {
                                    vb.dataBlockInd = tvd.DataBlockIndex;
                                }
                                tagViewerControls.Add(entry.Value.AbsoluteTagOffset, vb);
                                parentpanel.Children.Add(vb);

                                if (childCount > 2000000)
                                {
                                    return;
                                }

                                if (entry.Value.B != null)
                                {
                                    List<string> indexBoxSource = new List<string>();
                                    for (int y = 0; y < childCount; y++)
                                    {
                                        indexBoxSource.Add(y.ToString());
                                    }
                                    vb.indexbox.ItemsSource = indexBoxSource;

                                    if (childCount > 0)
                                    {
                                        vb.indexbox.SelectedIndex = -1;
                                    }
                                    else
                                    {
                                        vb.Expand_Collapse_Button.IsEnabled = false;
                                        vb.Expand_Collapse_Button.Content = "";
                                        vb.indexbox.IsEnabled = false;
                                    }
                                }
                                else
                                {
                                    vb.Expand_Collapse_Button.IsEnabled = false;
                                    vb.Expand_Collapse_Button.Content = "";
                                    vb.indexbox.IsEnabled = false;

                                }
                            }

                            if (isTagBlock && tb != null)
                                tb.controlKeys.Add(entry.Value.AbsoluteTagOffset);

                            prevEntry = entry;
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusOut(ex.Message);
                    }
                }
            }
        }

        public void BuildTagBlock(IRTV_TagStruct tagStruct, KeyValuePair<long, TagLayouts.C> entry, TagBlock tagBlock, string absolute_address_chain)
        {
            tagBlock.dockpanel.Children.Clear();

            foreach (string key in tagBlock.controlKeys)
            {
                if (tagViewerControls.ContainsKey(key))
                    tagViewerControls.Remove(key);
            }

            tagBlock.controlKeys.Clear();

            if (entry.Value.B != null && tagBlock.dataBlockInd > 0)
            {
                try
                {
                    long newAddress = (long)moduleFile.Tag.DataBlockArray[tagBlock.dataBlockInd].Offset - (long)moduleFile.Tag.DataBlockArray[0].Offset + (entry.Value.S * tagBlock.stored_num_on_index);
                    long startingAddress = tagBlock.size * tagBlock.stored_num_on_index + newAddress;

                    CreateTagControls(tagStruct, startingAddress, entry.Value.B, newAddress, tagBlock.dockpanel, absolute_address_chain, tagBlock, true);
                }
                catch
                {
                    TextBox tb = new TextBox { Text = "this tagblock is fucked uwu" };
                    tagBlock.dockpanel.Children.Add(tb);
                }
            }
        }

        private byte[] GetDataFromFile(int size, long offset, ModuleFile? mf = null)
        {
            if (mf == null)
                mf = moduleFile;

            byte[] data = mf.Tag.TagData.Skip((int)offset).Take(((int)offset + size) - (int)offset).ToArray();
            return data;
        }

        public string IDToTagName(string value)
        {
            if (inhaledTags.ContainsKey(value))
            {
                return inhaledTags[value].Path;
            }
            else
            {
                return "ObjectID: " + value;
            }
        }

        public static string ReverseString(string myStr)
        {
            char[] myArr = myStr.ToCharArray();
            Array.Reverse(myArr);
            return new string(myArr);
        }

        private TagInfo GetTagInfo(string tagID)
        {
            TagInfo result = new TagInfo();

            if (inhaledTags.ContainsKey(tagID))
                result = inhaledTags[tagID];
            else
            {
                result.TagID = tagID;
                result.AssetID = "Unknown";
                result.Path = "Unknown";
            }    

            return result;
        }
        #endregion

        #region Tag and Module Writing
        private void VBTextChange(object sender, TextChangedEventArgs e)
        {
            try
            {
                TextBox tb = (TextBox)sender;

                int writeOffset = 0;
                string dataKey = (string)tb.Tag;
                if (dataKey.Contains("-"))
                {
                    writeOffset = Convert.ToInt32(dataKey.Split("-")[1]);
                    dataKey = dataKey.Split("-")[0];
                }

                TagValueData tvd = tagValueData[dataKey];
                long totalOffset = Convert.ToInt64(tvd.Offset) + moduleFile.Tag.Header.HeaderSize;
                byte[] value = new byte[0];

                if (tvd.Type == "Byte")
                {
                    value = new byte[] { Convert.ToByte(tb.Text) };
                    tvd.Value = value;
                }
                else if (tvd.Type == "2Byte")
                {
                    value = BitConverter.GetBytes(Convert.ToInt16(tb.Text));
                    tvd.Value = value;
                }
                else if (tvd.Type == "4Byte")
                {
                    value = BitConverter.GetBytes(Convert.ToInt32(tb.Text));
                    tvd.Value = value;
                }
                else if (tvd.Type == "Float")
                {
                    value = BitConverter.GetBytes(Convert.ToSingle(tb.Text));
                    tvd.Value = value;
                }
                else if (tvd.Type == "mmr3Hash")
                {
                    if (tb.Text.Length == 8)
                    {
                        value = Convert.FromHexString(tb.Text);
                        tvd.Value = value;
                    }
                }
                else if (tvd.Type == "String")
                {
                    value = Encoding.ASCII.GetBytes(tb.Text);
                    tvd.Value = value;
                }
                
                if (value.Length > 0)
                {
                    if (tagStream != null)
                    {
                        tagStream.Position = totalOffset + writeOffset;
                        tagStream.Write(value);
                        Debug.WriteLine("Change at: " + totalOffset + " New Value: " + tb.Text + " Value Type: " + tvd.Type);
                    }
                    else if (tagFileStream != null)
                    {
                        tagFileStream.Position = totalOffset + writeOffset;
                        tagFileStream.Write(value);
                        Debug.WriteLine("Change at: " + totalOffset + " New Value: " + tb.Text + " Value Type: " + tvd.Type);
                    }
                }
            }
            catch
            {
                StatusOut("Error setting new value");
            }
        }

        public void FlagGroupChange(object sender, RoutedEventArgs e)
        {
            try
            {
                TagFlagsGroup tfg = (TagFlagsGroup)sender;
                TagValueData tvd = tagValueData[(string)tfg.Tag];

                long totalOffset = Convert.ToInt64(tvd.Offset) + moduleFile.Tag.Header.HeaderSize;
                byte[] value = tfg.data;
                tvd.Value = value;

                if (tagStream != null)
                {
                    tagStream.Position = totalOffset;
                    tagStream.Write(value);
                    Debug.WriteLine("Change at: " + totalOffset + " New Value: " + Convert.ToHexString(value) + " Value Type: " + tvd.Type);
                }
                else if (tagFileStream != null)
                {
                    tagFileStream.Position = totalOffset;
                    tagFileStream.Write(value);
                    Debug.WriteLine("Change at: " + totalOffset + " New Value: " + Convert.ToHexString(value) + " Value Type: " + tvd.Type);
                }
            }
            catch
            {
                StatusOut("Error setting new value");
            }
        }

        private void EnumBlockChanged(object sender, RoutedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            TagValueData tvd = tagValueData[(string)cb.Tag];
            byte[] data = BitConverter.GetBytes(cb.SelectedIndex);
            byte[] value = new byte[0];

            if (tvd.Size != null)
            {
                value = new byte[(int)tvd.Size];

                if (tvd.Size == 1)
                {
                    value[0] = data[0];
                }
                else if (tvd.Size == 2)
                {
                    value[0] = data[0];
                    value[1] = data[1];
                }
                else if (tvd.Size == 4)
                {
                    value[0] = data[0];
                    value[1] = data[1];
                    value[2] = data[2];
                    value[3] = data[3];
                }
            }
                

            long totalOffset = Convert.ToInt64(tvd.Offset) + moduleFile.Tag.Header.HeaderSize;
            tvd.Value = value;
            
            if (tagStream != null)
            {
                tagStream.Position = totalOffset;
                tagStream.Write(value);
                Debug.WriteLine("Change at: " + totalOffset + " New Value: " + Convert.ToHexString(value) + " Value Type: " + tvd.Type + " Size" + tvd.Size);
            }
            else if (tagFileStream != null)
            {
                tagFileStream.Position = totalOffset;
                tagFileStream.Write(value);
                Debug.WriteLine("Change at: " + totalOffset + " New Value: " + Convert.ToHexString(value) + " Value Type: " + tvd.Type + " Size" + tvd.Size);
            }
        }

        #endregion

        #region Dictionaries
        public Dictionary<string, string> tagGroups = new Dictionary<string, string>();
        public Dictionary<string, TagInfo> inhaledTags = new Dictionary<string, TagInfo>();

        public class TagInfo
        {
            public string Path = "";
            public string TagID = "";
            public string AssetID = "";
            public string Module = "";
        }

        private void InhaleTagGroup()
        {
            foreach(string line in File.ReadAllLines(@".\Files\tagGroups.txt"))
            {
                if (line.Contains(":"))
                {
                    string tagGroup = line.Split(":")[1];
                    string tagGroupShort = line.Split(":")[0];

                    if (!tagGroups.ContainsKey(tagGroup))
                    {
                        tagGroups.Add(tagGroup.Trim(), tagGroupShort.Trim());
                    }
                }
            }
        }

        public void InhaleTagNames()
        {
            string filename = Directory.GetCurrentDirectory() + @"\files\tagnames.txt";
            IEnumerable<string>? lines = System.IO.File.ReadLines(filename);
            foreach (string? line in lines)
            {
                string[] hexString = line.Split(" : ");
                if (!inhaledTags.ContainsKey(hexString[0]))
                {
                    TagInfo ti = new();
                    ti.TagID = hexString[0];
                    ti.AssetID = hexString[1];
                    ti.Path = hexString[2];
                    ti.Module = hexString[3];
                    inhaledTags.Add(hexString[0], ti);
                }
            }
            Debug.WriteLine("");
        }
        #endregion

        #region Tag References
        // Notes:
        // 
        // There are two parts to tag references.
        // The tag reference index:
        //      1. Bytes [0-3] - Tag Type
        //      2. Bytes [4-7] - Tag path start in tag path index
        //      3. Bytes [8-15] - Asset ID
        //      4. Bytes [16-19] - Tag ID
        //      5. Bytes [20-24] - 0xFF
        //
        // The Tag Ref Itself:
        //      1. Bytes [0-7] - 0xBC
        //      2. Bytes [8-11] - Tag ID
        //      3. Bytes [12-19] - Asset ID
        //      4. Bytes [20-23] - Tag Type
        //      5. Bytes [24-27] - 0xBC
        //
        // Asset ID can be found when loading in a module, however it needs to be output to the tag ID file.
        // I'm going to make a new dump and tag list loading system to account for this.
        // 
        // Something really weird with all of this, it doesn't work all the time for some reason.
        // Sometimes you can change the tag reference, sometimes you can't. I'm not sure why.
        #endregion

        #region Tools
        private async void DumpTagInfoClick(object sender, RoutedEventArgs e)
        {
            List<string> tagDumpInfos = new List<string>();

            try
            {
                StatusOut("Dumping tag info...");

                foreach (string path in modulePaths)
                {
                    FileStream mStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    ModuleFile mFile = new ModuleFile();

                    Task dumpTags = new Task(() =>
                    {
                        Module m = ModuleEditor.ReadModule(mStream);

                        foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
                        {
                            string TagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));
                            string AssetID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.AssetId));
                            string TagPath = mf.Key.Replace("\0", String.Empty);
                            string ModulePath = path.Split("deploy\\").Last();
                            tagDumpInfos.Add(TagID + " : " + AssetID + " : " + TagPath + " : " + ModulePath);
                        }
                    });
                    dumpTags.Start();
                    await dumpTags;
                    dumpTags.Dispose();
                }

                tagDumpInfos.Sort();
                SaveFileDialog sfd = new();
                sfd.Filter = "Text File (*.txt)|*.txt";
                sfd.FileName = "tagnames.txt";
                if (sfd.ShowDialog() == true)
                {
                    File.WriteAllLines(sfd.FileName, tagDumpInfos.ToArray());
                }

                tagDumpInfos.Clear();
                StatusOut("Tag info dumped!");
            }
            catch
            {
                StatusOut("Failed to dump tag info!");
            }
        }

        private async void DumpTagInfoIRTVClick(object sender, RoutedEventArgs e)
        {
            List<string> tagDumpInfos = new List<string>();

            try
            {
                StatusOut("Dumping tag info...");

                foreach (string path in modulePaths)
                {
                    FileStream mStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    ModuleFile mFile = new ModuleFile();

                    Task dumpTags = new Task(() =>
                    {
                        Module m = ModuleEditor.ReadModule(mStream);

                        foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
                        {
                            string TagID = mf.Value.FileEntry.GlobalTagId.ToString();
                            //string TagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));
                            string AssetID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.AssetId));
                            string TagPath = mf.Key.Replace("\0", String.Empty);
                            string ModulePath = path.Split("deploy\\").Last();

                            if (TagPath.EndsWith(".forgeobjectdata"))
                                tagDumpInfos.Add(TagID + " : " + TagPath);
                        }
                    });
                    dumpTags.Start();
                    await dumpTags;
                    dumpTags.Dispose();
                }

                tagDumpInfos.Sort();
                SaveFileDialog sfd = new();
                sfd.Filter = "Text File (*.txt)|*.txt";
                sfd.FileName = "tagnames.txt";
                if (sfd.ShowDialog() == true)
                {
                    File.WriteAllLines(sfd.FileName, tagDumpInfos.ToArray());
                }

                tagDumpInfos.Clear();
                StatusOut("Tag info dumped!");
            }
            catch
            {
                StatusOut("Failed to dump tag info!");
            }
        }

        private async void DumpTagInfoIRMEClick(object sender, RoutedEventArgs e)
        {
            List<string> tagDumpInfos = new List<string>();

            try
            {
                StatusOut("Dumping tag info...");

                foreach (string path in modulePaths)
                {
                    FileStream mStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    ModuleFile mFile = new ModuleFile();

                    Task dumpTags = new Task(() =>
                    {
                        Module m = ModuleEditor.ReadModule(mStream);

                        foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
                        {
                            string TagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));
                            string AssetID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.AssetId));
                            string TagPath = mf.Key.Replace("\0", String.Empty);
                            string ModulePath = path.Split("deploy\\").Last();
                            if (TagPath.EndsWith("model"))
                                tagDumpInfos.Add(TagID + " : " + TagPath);
                        }
                    });
                    dumpTags.Start();
                    await dumpTags;
                    dumpTags.Dispose();
                }

                tagDumpInfos.Sort();
                SaveFileDialog sfd = new();
                sfd.Filter = "Text File (*.txt)|*.txt";
                sfd.FileName = "tagnames.txt";
                if (sfd.ShowDialog() == true)
                {
                    File.WriteAllLines(sfd.FileName, tagDumpInfos.ToArray());
                }

                tagDumpInfos.Clear();
                StatusOut("Tag info dumped!");
            }
            catch
            {
                StatusOut("Failed to dump tag info!");
            }
        }

        public class HashTagInfo
        {
            public string TagID = "";
            public Dictionary<long, string> ReferenceLocations = new();
        }

        public class HashInfo
        {
            public string HashID = "";
            public string HashName = "";
            public Dictionary<string, HashTagInfo> Tags = new();
        }

        private Dictionary<string, HashInfo> foundHashes = new();
        private Dictionary<string, string> hashNames = new();
       
        private async void DumpHashesClick(object sender, RoutedEventArgs e)
        {
            StatusOut("Gathering hashes...");
            bool done = false;
            int modulesDone = 0;
            Task dumpHashes = new Task(() =>
            {
                foreach (string line in File.ReadLines(@".\Files\all_trimmed.txt"))
                {
                    string trim = line.Trim();
                    if (trim.Contains(":"))
                        if (!hashNames.ContainsKey(trim.Split(":").First()))
                            hashNames.Add(trim.Split(":").First(), trim.Split(":").Last());
                }

                foreach (string line in File.ReadLines(@".\Files\nocaps_3chars.txt"))
                {
                    string trim = line.Trim();
                    if (trim.Contains(":"))
                        if (!hashNames.ContainsKey(trim.Split(":").First()))
                            hashNames.Add(trim.Split(":").First(), trim.Split(":").Last());
                }

                foreach (string path in modulePaths)
                {
                    Debug.WriteLine(modulesDone + " Current Module: " + path);
                    FileStream mStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    Module m = ModuleEditor.ReadModule(mStream);
                    ResetTagTree();
                    foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
                    {
                        try
                        {
                            string TagPath = mf.Key.Replace("\0", String.Empty);

                            if (!TagPath.EndsWith(".model_animation_graph") && !TagPath.EndsWith(".physics_model") && !TagPath.EndsWith(".decal_system") && !TagPath.EndsWith(".generator_system") && !TagPath.EndsWith(".composer_spawning_pattern"))
                            {
                                MemoryStream tStream = new();
                                tStream = ModuleEditor.GetTag(m, mStream, TagPath);
                                mf.Value.Tag = ModuleEditor.ReadTag(tStream, TagPath, mf.Value);
                                mf.Value.Tag.Name = TagPath;
                                mf.Value.Tag.ShortName = TagPath.Split("\\").Last();

                                string curTagGroup = mf.Key.Replace("\0", String.Empty).Split(".").Last();
                                string tagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));

                                if (tagGroups.ContainsKey(curTagGroup.Trim()))
                                {
                                    Dictionary<long, TagLayouts.C> tagDefinitions = TagLayouts.Tags(tagGroups[curTagGroup]);
                                    GetTagHashes(tagDefinitions, 0, 0, tagID + ":", mf.Value, path);
                                }

                                tStream.Close();
                                curDataBlockInd = 1;
                            } 
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Dump Hashes Task: " + ex.Message);
                        }
                    }
                    mStream.Close();
                    modulesDone++;
                }       

                done = true;
            });

            Task dumpCheck = new Task(() =>
            {
                while (!done)
                {
                    Thread.Sleep(1000);
                    StatusOut("Hashes Found: " + foundHashes.Count());
                }
            });

            dumpHashes.Start();
            dumpCheck.Start();
            await dumpHashes;
            
            StatusOut("Writing to file...");
            CreateHashFile();
            StatusOut("Hashes dumped!");
        }

        private void GetTagHashes(Dictionary<long, TagLayouts.C> tagDefinitions, long address, long startingTagOffset, string offsetChain, ModuleFile mf, string modulePath)
        {
            try
            {
                foreach (KeyValuePair<long, TagLayouts.C> entry in tagDefinitions)
                {
                    entry.Value.MemoryAddress = address + entry.Key;
                    entry.Value.AbsoluteTagOffset = offsetChain + "," + (entry.Key + startingTagOffset);
                    string name = "";
                    if (entry.Value.N != null)
                        name = entry.Value.N;

                    if (entry.Value.T == "Tagblock" || entry.Value.T == "FUNCTION")
                    {
                        TagValueData curTagData = new()
                        {
                            Name = name,
                            ControlType = entry.Value.T,
                            Type = entry.Value.T,
                            OffsetChain = entry.Value.AbsoluteTagOffset,
                            Offset = entry.Value.MemoryAddress,
                            Value = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf),
                            Size = (int)entry.Value.S,
                            ChildCount = 0,
                            DataBlockIndex = 0
                        };

                        int blockIndex = 0;
                        int childCount = BitConverter.ToInt32(GetDataFromFile(20, entry.Value.MemoryAddress, mf), 16);

                        if (curTagData.Type == "Tagblock" && childCount < 10000)
                        {
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;
                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;

                                for (int i = 0; i < childCount; i++)
                                {
                                    long newAddress = (long)mf.Tag.DataBlockArray[curTagData.DataBlockIndex].Offset - (long)mf.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);
                                    GetTagHashes(entry.Value.B, newAddress, newAddress + entry.Value.S * i, curTagData.OffsetChain, mf, modulePath);
                                }
                            }
                        }
                        else if (curTagData.Type == "FUNCTION")
                        {
                            curTagData.ChildCount = childCount;
                            childCount = BitConverter.ToInt32(GetDataFromFile(4, entry.Value.MemoryAddress + 20, mf));
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;

                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;
                            }
                        }
                    }

                    if (entry.Value.T == "mmr3Hash")
                    {
                        string hash = Convert.ToHexString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf));

                        if (hash != "BCBCBCBC" && hash != "00000000" && !String.IsNullOrWhiteSpace(hash) && !String.IsNullOrEmpty(hash))
                        {
                            string tagID = mf.FileEntry.GlobalTagId.ToString("X");
                            if (!foundHashes.ContainsKey(hash))
                            {
                                HashInfo hashInfo = new();
                                hashInfo.HashID = hash;

                                if (hashNames.ContainsKey(hash))
                                    hashInfo.HashName = hashNames[hash];
                                else
                                    hashInfo.HashName = "Unknown";

                                HashTagInfo hashTagInfo = new();
                                hashTagInfo.TagID = tagID;
                                if (!hashTagInfo.ReferenceLocations.ContainsKey(entry.Value.MemoryAddress))
                                    hashTagInfo.ReferenceLocations.Add(entry.Value.MemoryAddress, name);

                                hashInfo.Tags.Add(hashTagInfo.TagID, hashTagInfo);
                                foundHashes.Add(hash, hashInfo);
                            }
                            else
                            {
                                HashInfo hashInfo = foundHashes[hash];

                                if (!hashInfo.Tags.ContainsKey(tagID))
                                {
                                    HashTagInfo hashTagInfo = new();
                                    hashTagInfo.TagID = tagID;
                                    if (!hashTagInfo.ReferenceLocations.ContainsKey(entry.Value.MemoryAddress))
                                        hashTagInfo.ReferenceLocations.Add(entry.Value.MemoryAddress, name);
                                    hashInfo.Tags.Add(hashTagInfo.TagID, hashTagInfo);
                                }
                                else
                                {
                                    HashTagInfo hashTagInfo = hashInfo.Tags[tagID];
                                    if (!hashTagInfo.ReferenceLocations.ContainsKey(entry.Value.MemoryAddress))
                                        hashTagInfo.ReferenceLocations.Add(entry.Value.MemoryAddress, name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Get Tag Hashes: " + ex.Message);
            }
        }

        private void CreateHashFile()
        {
            List<string> lines = new();

            foreach (HashInfo hashInfo in foundHashes.Values)
            {
                string outLine = "";
                outLine = "Hash-" +hashInfo.HashID + "~Name-" + hashInfo.HashName + "~Tags{";
                int i = 0;
                foreach (HashTagInfo hashTagInfo in hashInfo.Tags.Values)
                {
                    outLine += hashTagInfo.TagID + "[";
                    int j = 0;
                    foreach (KeyValuePair<long, string> kvp in hashTagInfo.ReferenceLocations)
                    {
                        outLine += kvp.Value + ":" + kvp.Key.ToString("X");
                        if (j < hashTagInfo.ReferenceLocations.Count - 1)
                            outLine += ",";

                        j++;
                    }
                    outLine += "]";

                    if (i < hashInfo.Tags.Count - 1)
                        outLine += ",";

                    i++;
                }
                outLine += "}";

                lines.Add(outLine);
            }

            File.WriteAllLines(@".\Files\mmr3Hashes.txt", lines);
        }

        #endregion

        #region Forge Data
        public class ForgeData
        {
            public string TagID = "";
            public string TagName = "";
            public int TagIDInt = 0;
            public string Folder = "";
            public string SubFolder = "";
            public int EntryInd = 0;
            public string EntryDesc = "";
            public string Description = "";
            public string Title = "";
        }

        private Dictionary<string, ForgeData> forgeDump = new();
        private Dictionary<int, string> entryTags = new();
        private Dictionary<int, string> catagoryIDs = new();
        private Dictionary<int, string> parentCatagoryIDs = new();
        private Dictionary<string, string> parentCategories = new();
        private Dictionary<int, string> categoryDescIDs = new();
        private Dictionary<string, string> categoryDescs = new();
        private Dictionary<int, string> categoryTitles = new();
        private Dictionary<string, string> categoryTitleIDs = new();
        private int entryCount = 0;
        private int categoryCount = 0;

        private async void DumpForgeData(object sender, RoutedEventArgs e)
        {
            StatusOut("Gathering forge data...");
            forgeDump.Clear();

            Dictionary<string, string> forgeObjectNames = new();
            foreach (string line in File.ReadAllLines(@".\Files\nameList.txt"))
            {
                string ID = line.Split(" = ").Last().Replace(",", string.Empty).Trim();
                string name = line.Split(" = ").First().Trim();
                if (!forgeObjectNames.ContainsKey(ID))
                    forgeObjectNames.Add(ID, name);
            }
            string deploy = @"X:\Games\Halo Infinite - Big Forge\deploy";
            string objectDataPath = deploy + @"\any\globals\forge\forge_objects-rtx-new.module";
            string objectManifestPath = deploy + @"\any\globals\levels-rtx-new.module";

            FileStream mStream = new FileStream(objectDataPath, FileMode.Open, FileAccess.Read);
            Module m = ModuleEditor.ReadModule(mStream);

            foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
            {
                try
                {
                    string TagPath = mf.Key.Replace("\0", String.Empty);

                    if (TagPath.EndsWith(".forgeobjectdata"))
                    {
                        MemoryStream tStream = new();
                        tStream = ModuleEditor.GetTag(m, mStream, TagPath);
                        string tagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));

                        ForgeData fd = new();
                        fd.TagID = tagID;
                        fd.TagName = TagPath.Split("\\").Last().Split(".").First();
                        fd.TagIDInt = mf.Value.FileEntry.GlobalTagId;
                        forgeDump.Add(fd.TagID, fd);

                        tStream.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Forge Dump Error: " + ex.Message);
                }
            }
            mStream.Close();
            m = new();

            mStream = new FileStream(objectManifestPath, FileMode.Open, FileAccess.Read);
            m = ModuleEditor.ReadModule(mStream);

            foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
            {
                string TagPath = mf.Key.Replace("\0", String.Empty);

                if (TagPath.EndsWith(".forgeobjectmanifest"))
                {
                    MemoryStream tStream = new();
                    tStream = ModuleEditor.GetTag(m, mStream, TagPath);
                    mf.Value.Tag = ModuleEditor.ReadTag(tStream, TagPath, mf.Value);
                    mf.Value.Tag.Name = TagPath;
                    mf.Value.Tag.ShortName = TagPath.Split("\\").Last();

                    string tagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));
                    string curTagGroup = mf.Key.Replace("\0", String.Empty).Split(".").Last();

                    if (tagGroups.ContainsKey(curTagGroup.Trim()))
                    {
                        Dictionary<long, TagLayouts.C> tagDefinitions = TagLayouts.Tags(tagGroups[curTagGroup]);
                        
                        entryCount = 0;
                        categoryCount = 0;                        
                        GetForgeInfo(tagDefinitions, 0, 0, tagID + ":", mf.Value, objectManifestPath, "");
                    }
                }
            }

            mStream.Close();
            m = new();

            mStream = new FileStream(objectManifestPath, FileMode.Open, FileAccess.Read);
            m = ModuleEditor.ReadModule(mStream);
            curDataBlockInd = 1;
            foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
            {
                string TagPath = mf.Key.Replace("\0", String.Empty);

                if (TagPath.EndsWith(".forgeobjectmanifest"))
                {
                    MemoryStream tStream = new();
                    tStream = ModuleEditor.GetTag(m, mStream, TagPath);
                    mf.Value.Tag = ModuleEditor.ReadTag(tStream, TagPath, mf.Value);
                    mf.Value.Tag.Name = TagPath;
                    mf.Value.Tag.ShortName = TagPath.Split("\\").Last();

                    string tagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));
                    string curTagGroup = mf.Key.Replace("\0", String.Empty).Split(".").Last();

                    if (tagGroups.ContainsKey(curTagGroup.Trim()))
                    {
                        Dictionary<long, TagLayouts.C> tagDefinitions = TagLayouts.Tags(tagGroups[curTagGroup]);

                        entryCount = 0;
                        categoryCount = 0;
                        GetForgeInfo2(tagDefinitions, 0, 0, tagID + ":", mf.Value, objectManifestPath, "");
                    }
                }
            }

            foreach (KeyValuePair<int, string> kvp in catagoryIDs)
            {
                string parentCategory = parentCatagoryIDs[kvp.Key];
                string categoryDesc = categoryDescIDs[kvp.Key];
                string categoryTitle = categoryTitles[kvp.Key];
                parentCategories.Add(kvp.Value, parentCategory);
                categoryDescs.Add(kvp.Value, categoryDesc);
                categoryTitleIDs.Add(kvp.Value, categoryTitle);
            }

            List<string> lines = new();
            foreach (ForgeData fd in forgeDump.Values)
            {
                try
                {
                    fd.Folder = parentCategories[fd.SubFolder];
                    fd.EntryDesc = categoryDescs[fd.SubFolder];
                    fd.Title = categoryTitleIDs[fd.SubFolder];

                    string folder = fd.Folder;
                    if (FolderNames.ContainsKey(fd.Folder))
                    {
                        folder = FolderNames[fd.Folder];
                    }

                    string subfolder = fd.SubFolder;
                    if (FolderNames.ContainsKey(fd.SubFolder))
                    {
                        subfolder = FolderNames[fd.SubFolder];
                    }

                    string newLine = folder + ":" + fd.Folder + ":" + subfolder + ":" + fd.SubFolder + ":" + fd.TagName + ":" + fd.TagIDInt + ":" + fd.Description + ":" + fd.EntryDesc + ":" + fd.Title + ":" + fd.EntryInd;
                    lines.Add(newLine);
                }
                catch
                {

                }
                
            }
            lines.Sort();
            SaveFileDialog sfd = new();
            sfd.Filter = "Text File (*.txt)|*.txt";
            sfd.FileName = "ForgeObjects.txt";
            if (sfd.ShowDialog() == true)
            {
                File.WriteAllLines(sfd.FileName, lines.ToArray());
            }

        }
        private void GetForgeInfo(Dictionary<long, TagLayouts.C> tagDefinitions, long address, long startingTagOffset, string offsetChain, ModuleFile mf, string modulePath, string tagBlockName)
        {
            try
            {
                foreach (KeyValuePair<long, TagLayouts.C> entry in tagDefinitions)
                {
                    entry.Value.MemoryAddress = address + entry.Key;
                    entry.Value.AbsoluteTagOffset = offsetChain + "," + (entry.Key + startingTagOffset);
                    string name = "";
                    if (entry.Value.N != null)
                        name = entry.Value.N;

                    if (entry.Value.T == "Tagblock" || entry.Value.T == "FUNCTION")
                    {
                        TagValueData curTagData = new()
                        {
                            Name = name,
                            ControlType = entry.Value.T,
                            Type = entry.Value.T,
                            OffsetChain = entry.Value.AbsoluteTagOffset,
                            Offset = entry.Value.MemoryAddress,
                            Value = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf),
                            Size = (int)entry.Value.S,
                            ChildCount = 0,
                            DataBlockIndex = 0
                        };

                        int blockIndex = 0;
                        int childCount = BitConverter.ToInt32(GetDataFromFile(20, entry.Value.MemoryAddress, mf), 16);

                        if (curTagData.Type == "Tagblock" && childCount < 10000)
                        {
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;
                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;

                                for (int i = 0; i < childCount; i++)
                                {
                                    long newAddress = (long)mf.Tag.DataBlockArray[curTagData.DataBlockIndex].Offset - (long)mf.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);
                                    GetForgeInfo(entry.Value.B, newAddress, newAddress + entry.Value.S * i, curTagData.OffsetChain, mf, modulePath, entry.Value.N);
                                }
                            }
                        }
                        else if (curTagData.Type == "FUNCTION")
                        {
                            curTagData.ChildCount = childCount;
                            childCount = BitConverter.ToInt32(GetDataFromFile(4, entry.Value.MemoryAddress + 20, mf));
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;

                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;
                            }
                        }
                    }

                    if (entry.Value.N == "Title")
                    {
                        string categoryTitle = Convert.ToHexString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf));
                        categoryTitles.Add(categoryCount, categoryTitle);
                    }

                    if (entry.Value.N == "Description" && tagBlockName == "Category Entries")
                    {
                        string categoryDesc = Convert.ToHexString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf));
                        categoryDescIDs.Add(categoryCount, categoryDesc);
                    }

                    if (entry.Value.N == "Category ID")
                    {
                        string categoryID = Convert.ToHexString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf));
                        catagoryIDs.Add(categoryCount, categoryID);
                        
                    }
                    
                    if (entry.Value.N == "Parent Category ID")
                    {
                        string categoryID = Convert.ToHexString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf));
                        parentCatagoryIDs.Add(categoryCount, categoryID);
                        categoryCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Get Tag Hashes: " + ex.Message);
            }
        }
        private void GetForgeInfo2(Dictionary<long, TagLayouts.C> tagDefinitions, long address, long startingTagOffset, string offsetChain, ModuleFile mf, string modulePath, string tagBlockName)
        {
            try
            {
                foreach (KeyValuePair<long, TagLayouts.C> entry in tagDefinitions)
                {
                    entry.Value.MemoryAddress = address + entry.Key;
                    entry.Value.AbsoluteTagOffset = offsetChain + "," + (entry.Key + startingTagOffset);
                    string name = "";
                    if (entry.Value.N != null)
                        name = entry.Value.N;

                    if (entry.Value.T == "Tagblock" || entry.Value.T == "FUNCTION")
                    {
                        TagValueData curTagData = new()
                        {
                            Name = name,
                            ControlType = entry.Value.T,
                            Type = entry.Value.T,
                            OffsetChain = entry.Value.AbsoluteTagOffset,
                            Offset = entry.Value.MemoryAddress,
                            Value = GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf),
                            Size = (int)entry.Value.S,
                            ChildCount = 0,
                            DataBlockIndex = 0
                        };

                        int blockIndex = 0;
                        int childCount = BitConverter.ToInt32(GetDataFromFile(20, entry.Value.MemoryAddress, mf), 16);

                        if (curTagData.Type == "Tagblock" && childCount < 10000)
                        {
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;
                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;

                                for (int i = 0; i < childCount; i++)
                                {
                                    long newAddress = (long)mf.Tag.DataBlockArray[curTagData.DataBlockIndex].Offset - (long)mf.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);
                                    GetForgeInfo2(entry.Value.B, newAddress, newAddress + entry.Value.S * i, curTagData.OffsetChain, mf, modulePath, entry.Value.N);
                                }
                            }
                        }
                        else if (curTagData.Type == "FUNCTION")
                        {
                            curTagData.ChildCount = childCount;
                            childCount = BitConverter.ToInt32(GetDataFromFile(4, entry.Value.MemoryAddress + 20, mf));
                            if (childCount > 0)
                            {
                                blockIndex = curDataBlockInd;
                                curDataBlockInd++;

                                curTagData.ChildCount = childCount;
                                curTagData.DataBlockIndex = blockIndex;
                            }
                        }
                    }

                    if (entry.Value.N == "Forge Object")
                    {
                        string entryTag = Convert.ToHexString(GetDataFromFile(4, entry.Value.MemoryAddress + 8, mf));

                        if (entryTag == "BCBA3880")
                        {
                            string test = "test";
                        }

                        if (forgeDump.ContainsKey(entryTag))
                        {
                            ForgeData fd = new();
                            fd = (ForgeData)forgeDump[entryTag];
                            fd.EntryInd = entryCount;
                        }

                        entryTags.Add(entryCount, entryTag);
                        entryCount++;
                    }
                    
                    if (entry.Value.N == "Description" && tagBlockName == "Forge Object Entries")
                    {
                        string desc = Convert.ToHexString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf));
                        if (forgeDump.ContainsKey(entryTags[entryCount - 1]))
                        {
                            ForgeData fd = forgeDump[entryTags[entryCount - 1]];
                            fd.Description = desc;
                        }
                        
                    }
                    
                    if (entry.Value.N == "Keyword" && tagBlockName == "Object Metadata")
                    {
                        string subFolder = Convert.ToHexString(GetDataFromFile((int)entry.Value.S, entry.Value.MemoryAddress, mf));
                        
                        if (forgeDump.ContainsKey(entryTags[entryCount - 1]))
                        {
                            ForgeData fd = forgeDump[entryTags[entryCount - 1]];
                            fd.SubFolder = subFolder;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("One: " + ex.Message);
            }
        }

        private Dictionary<string, string> FolderNames = new()
        {
            // ROOT FOLDERS
            { "3ACEAA9D", "recents" },
            { "751DF3FA", "prefabs" },
            { "EA3223EA", "accents" },
            { "9D24B701", "biomes" },
            { "957003CB", "blockers" },
            { "7416759B", "decals" },
            { "D45632B1", "fx" },
            { "58EAD6EF", "gameplay" },
            { "98D3445F", "halo design set" },
            { "F1CC100D", "lights" },
            { "EF548B2A", "primitives" },
            { "805A8ACF", "props" },
            { "AB5C78A5", "structures" },
            { "920A82B9", "Z_Null" },
            { "45E9333E", "Z_Unused" },
            { "F773D4F6", "kits" },
            // ACCENTS
            { "FCB0B577", "antennas" },
            { "D3E15E7E", "antennas mp" },
            { "051C51D3", "arena" },
            { "C83A0371", "barrels" },
            { "ACD5A535", "barrels mp" },
            { "BA860FA1", "bazaar" },
            { "A62122D8", "bazaar mp" },
            { "600A76CA", "bodies" },
            { "94C44886", "city props" },
            { "4A174B06", "city props mp" },
            { "48DF3BB5", "cover" },
            { "3C5C221E", "cover mp" },
            { "B8E84C1E", "crates" },
            { "4215C711", "crates mp" },
            { "094B5F3D", "destructibles" },
            { "2730E2EF", "destructibles mp" },
            { "374F00F1", "ducts" },
            { "F32489F1", "fences" },
            { "6C5E87BF", "fences mp" },
            { "B62245A9", "forerunner" },
            { "E908822C", "forerunner mp" },
            { "228BE1A4", "garbage" },
            { "9E83989D", "garbage mp" },
            { "77B82060", "glass" },
            { "56BA23BA", "missiles" },
            { "74D2A795", "missiles mp" },
            { "D7477F6A", "panels" },
            { "BDD4227F", "pipes" },
            { "69769AD1", "railings" },
            { "D1CD6834", "railings mp" },
            { "127F0160", "rubble" },
            { "300A7AB4", "sandbags" },
            { "A7DAD52E", "sandbags mp" },
            { "17864DE1", "signs" },
            { "4588194B", "supports" },
            { "070BAEA1", "tools mp" },
            { "935B215C", "unsc" },
            { "67927F10", "unsc mp" },
            { "FFC5EE8E", "vehicles" },
            { "53833E8C", "vehicles mp" },
            { "78F032BC", "wires" },
            { "2C664138", "wires mp" },
            { "06DFFE4B", "workstations" },
            { "F3F5375C", "workstations mp" },
            // BIOMES
            { "59D2C375", "bushes" },
            { "F7174DF2", "flora" },
            { "7E640351", "rocks - alpine" },
            { "F7D02A88", "rocks - burnt forest" },
            { "D16E4F08", "rocks - desert" },
            { "637D015B", "rocks - glacier" },
            { "ACE14020", "rocks - misc" },
            { "ADC73C23", "rocks - space" },
            { "32E488A5", "rocks - tidal" },
            { "A40F97C9", "rocks - wetlands" },
            { "3B167CF9", "stumps" },
            { "94C6B4EB", "terrain" },
            { "E857DAFE", "trees" },
            { "84A89220", "trees - logs" },
            { "5D98AB89", "trees - roots" },
            // BLOCKERS
            { "48FD8875", "one way blockers" },
            { "57FD7FFC", "player blockers" },
            { "0CAD6ADA", "projectile blockers" },
            { "94A72FF7", "team blockers" },
            { "DD740B6E", "vehicle blockers" },
            // DECALS
            { "68B715EC", "building signage" },
            { "3F6697AB", "letters" },
            { "945FB30E", "numbered symbols" },
            { "8FE9E9A6", "numbers" },
            { "1EFE9CB5", "unsc" },
            // FX
            { "67FE6E8C", "ambient life" },
            { "9F2A6BE7", "atmospherics" },
            { "04758E7F", "energy" },
            { "D96067BD", "explosions" },
            { "E84E2354", "fire" },
            { "05B5A5C8", "general" },
            { "9B475E78", "holograms" },
            { "04FEBB43", "liquids" },
            { "94CD949E", "smoke" },
            { "F24EA653", "sparks" },
            // GAMEPLAY
            { "3BD142A2", "audio" },
            { "0EA3DB6D", "equipment" },
            { "35328CB8", "game modes" },
            { "16A987BE", "launchers / lifts" },
            { "2EE94D70", "match flow" },
            { "82B4C1BA", "nav mesh" },
            { "466AD096", "player spawning" },
            { "D623245D", "sandbox" },
            { "A26A4C1A", "scripting" },
            { "9DAB4EF5", "teleporters" },
            { "D9026BD9", "vehicles" },
            { "0C5B1EFE", "volumes" },
            { "214E07D8", "weapon spawners" },
            { "AE54AEC7", "weapons" },
            // HALO DESIGN SET
            { "007F8C7D", "columns" },
            { "85FADED6", "columns mp" },
            { "6CB2C702", "cover" },
            { "931DAC45", "cover mp" },
            { "037F0DCC", "crates" },
            { "9E43E1BF", "crates mp" },
            { "4A50D3DE", "doorways" },
            { "891CD51E", "doorways mp" },
            { "B7D08EFA", "floors" },
            { "C3487ED6", "floors mp" },
            { "0F051460", "railings" },
            { "C7E7D7BE", "railings mp" },
            { "5A3E0F31", "ramps" },
            { "BD2FF5EF", "ramps mp" },
            { "51CB6FCF", "scale objects" },
            { "2E62784F", "walls" },
            { "8E06BFE6", "walls mp" },
            // LIGHTS
            { "4FA9B4B8", "forerunner light" },
            { "ED571C3C", "forerunner light mp" },
            { "545838D9", "forerunner no light" },
            { "DD37E7E2", "forerunner no light mp" },
            { "9C3D0A89", "generic light objects" },
            { "E019CC37", "unsc light" },
            { "6A72C8B2", "unsc light mp" },
            { "289FE80D", "unsc no light" },
            { "91964832", "unsc no light mp" },
            // PRIMITIVES
            { "F3228393", "blocks" },
            { "9A215954", "cones" },
            { "BD977D51", "cylinders" },
            { "0F2B2029", "pyramids" },
            { "B5A1024C", "rings" },
            { "15A30F2F", "spheres" },
            { "F6B1AB74", "trapezoids" },
            { "2C6FA031", "triangles" },
            // PROPS
            { "B33BA26D", "sports" },
            { "85274C4A", "summertime" },
            { "53087C47", "toys" },
            // STRUCTURES
            { "59E9E765", "beams" },
            { "6E0FC805", "bridges" },
            { "29DAE20F", "bridges mp" },
            { "13944678", "columns" },
            { "3D53832E", "cover" },
            { "0813C7E9", "doors" },
            { "FB42AFE8", "doors mp" },
            { "8C890378", "floors" },
            { "5589B3AD", "slopes" },
            { "4DE4BA8D", "walls" },
        };
        #endregion

        #region Hash Searching

        private void HashSearchClick(object sender, RoutedEventArgs e)
        {
            StatusOut("Searching...");
            ResetHashSearch();
            string search = hashSeachBox.Text.Trim();
            if (search.Length == 8)
            {
                string foundLine = "";
                int curLine = 1;
                foreach (string line in File.ReadAllLines(@".\Files\mmr3Hashes.txt"))
                {
                    if (line.Contains(search))
                    {
                        foundLine = line;
                        break;
                    }
                    curLine++;
                }

                if (foundLine.Length > 1)
                {
                    StatusOut("Hash found at line: " + curLine);
                    string hash = foundLine.Split("~")[0].Split("-")[1];
                    string hashName = foundLine.Split("~")[1].Split("-")[1];
                    string TagList = foundLine.Split("~")[2].Replace("Tags{","").Replace("}", "");
                    string[] Tags = TagList.Split("],");

                    HashBox.Text = hash;
                    HashNameBox.Text = hashName;
                    TagCountBox.Text = Tags.Count().ToString();

                    foreach (string tag in Tags)
                    {
                        string tagID = ReverseHexString(tag.Split("[").First());
                        string tagName = "Object ID: " + tagID;
                        if (inhaledTags.ContainsKey(tagID))
                            tagName = inhaledTags[tagID].Path.Split("\\").Last();

                        TreeViewItem item = new TreeViewItem();
                        item.Header = tagName;
                        item.Selected += HashTagSelected;
                        item.Tag = tag;
                        HashTree.Items.Add(item);
                    }
                }
            }
            else
            {
                StatusOut("Incorrect hash format!");
            }
            
        }

        private void HashTagSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            string tagID = ReverseHexString(((string)item.Tag).Split("[").First());
            string tagName = "Object ID: " + tagID;
            string module = "";
            if (inhaledTags.ContainsKey(tagID))
            {
                tagName = inhaledTags[tagID].Path.Split("\\").Last();
                module = inhaledTags[tagID].Module;
            }

            TagIDBox.Text = tagID;
            TagNameBox.Text = tagName;
            ModuleNameBox.Text = module;

            string referenceList = ((string)item.Tag).Split("[").Last().Replace("]","");
            string[] references = referenceList.Split(",");

            ReferencePanel.Children.Clear();
            foreach (string reference in references)
            {
                HashReference hr = new();
                hr.ValueName.Text = reference.Split(":")[0];
                hr.ValueOffset.Text = reference.Split(":")[1];
                ReferencePanel.Children.Add(hr);
            }
        }

        private string ReverseHexString(string hexString)
        {
            string result = hexString;

            if (hexString.Length == 8)
            {
                byte[] bArray = Convert.FromHexString(result);
                byte[] newArray = new byte[4];
                newArray[0] = bArray[3];
                newArray[1] = bArray[2];
                newArray[2] = bArray[1];
                newArray[3] = bArray[0];

                result = Convert.ToHexString(newArray);
            }

            return result;
        }

        private void ResetHashSearch()
        {
            HashBox.Text = "";
            HashNameBox.Text = "";
            TagCountBox.Text = "";
            HashTree.Items.Clear();
            TagIDBox.Text = "";
            TagNameBox.Text = "";
            ModuleNameBox.Text = "";
            ReferencePanel.Children.Clear();
        }
        #endregion
    }
}
