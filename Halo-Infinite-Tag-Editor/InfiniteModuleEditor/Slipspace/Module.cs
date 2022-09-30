using System;
using System.Collections.Generic;
using System.Text;

namespace InfiniteModuleEditor
{
    public class Module
    {
        public string Head { get; set; }
        public int Version { get; set; }
        public long ModuleId { get; set; }
        public int FileCount { get; set; }
        public int ManifestCount { get; set; }
        public int ResourceIndex { get; set; }
        public int StringsSize { get; set; }
        public int ResourceCount { get; set; }
        public int BlockCount { get; set; }

        public int StringTableOffset { get; set; }
        public int ResourceListOffset { get; set; }
        public int BlockListOffset { get; set; }
        public long FileDataOffset { get; set; }

        public Dictionary<int, string> Strings = new Dictionary<int, string>();
        public Dictionary<string, ModuleFile> ModuleFiles = new Dictionary<string, ModuleFile>();
    }
}
