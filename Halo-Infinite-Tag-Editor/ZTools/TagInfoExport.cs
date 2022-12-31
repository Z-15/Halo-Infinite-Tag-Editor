using InfiniteModuleEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ZTools.ZCommon;

namespace ZTools
{
    public class TagInfoExport
    {
        private static int curDataBlockInd = 1;

        public static Dictionary<string, TagInfo> ExtractTagInfo(List<string> modulePaths)
        {
            Dictionary<string, TagInfo> result = new();
            int i = 0;
            foreach (string path in modulePaths)
            {
                FileStream mStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                ModuleFile mFile = new ModuleFile();
                Module m = ModuleEditor.ReadModule(mStream);
                
                foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
                {
                    TagInfo tI = new();
                    tI.TagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));
                    tI.AssetID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.AssetId));
                    tI.TagPath = mf.Key.Replace("\0", String.Empty);
                    tI.ModulePath = path.Split("deploy\\").Last();

                    if (result.ContainsKey(tI.TagID))
                        result.Add(tI.TagID + ": Duplicate ID at " + i.ToString(), tI);
                    else
                        result.Add(tI.TagID, tI);

                    i++;
                }
            }
            return result;
        }
    }
}
