using InfiniteModuleEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static ZTools.ZCommon;
using InfiniteModuleEditor;
using InfiniteRuntimeTagViewer.Halo.TagObjects;
using Microsoft.Win32;
using Newtonsoft.Json;
using static ZTools.ZCommon;
using System.IO;
using System.Diagnostics;

namespace ZTools
{
    public class MaterialExport
    {
        class Material
        {
            public string materialID = "";
            public string materialName = "";

            public float real = 0;
            public Vector3 vector = new();

            public string regionName = "";
            public string materialStyleID = "";
            public string materialStyleName = "";
            public List<MaterialStyle>? materialStyles = null;
        }

        class MaterialStyle
        {
            public string name = "";
            public string grimeType = "";
            public float emissiveAmount = 0;
            public float grimeAmount = 0;
            public float scratchAmount = 0;
            public string paletteID = "";
            public string paletteName = "";
            public List<MaterialPalette>? paletteSwatches = null;
        }

        class MaterialPalette
        {
            public string name = "";
            public string color = "";
            public string groupName = "";
            public string roughnessOverride = "";
            public float emissiveIntensity = 0;
            public float emissiveAmount = 0;
            public string swatchID = "";
            public string swatchName = "";
            public MaterialSwatch? swatchData = null;
        }

        class MaterialSwatch
        {
            public Vector2 colorAndRoughnessTextureTransform = new(0, 0);
            public Vector2 normalTextureTransform = new(0, 0);
            public string color_gradient_map_id = "";
            public string color_gradient_map = "";
            public float roughness_white = 0;
            public float roughness_black = 0;
            public string normal_detail_map_id = "";
            public string normal_detail_map = "";
            public float metallic = 0;
            public float ior = 0;
            public float albedo_tint_spec = 0;
            public float sss_strenght = 0;
            public Vector3 scratch_color = new(0, 0, 0);
            public float scratch_brightness = 0;
            public float scratch_roughness = 0;
            public float scratch_metallic = 0;
            public float scratch_ior = 0;
            public float scratch_albedo_tint_spec = 0;
            public float sss_intensity = 0;
            public ColorVariant? color_variant = null;
        }

        class ColorVariant
        {
            public string name = "";
            public Vector3 gradient_top_color = new(0, 0, 0);
            public Vector3 gradient_mid_color = new(0, 0, 0);
            public Vector3 gradient_bottom_color = new(0, 0, 0);
        }

        private static MemoryStream tStream = new();
        private static FileStream? mStream;
        private static Module mod = new();
        private static int curDataBlockInd = 1;

        public static string? ExportMaterial(TagInfo tag, FileStream moduleStream, Module module)
        {
            // Set Values
            
            mStream = moduleStream;
            mod = module;

            // Attempt to open tag.
            ModuleFile? mf = TryOpenTag(tag.TagPath);
            if (mf == null) return null;

            // Create a Material Class
            Material mat = new();
            mat.materialID = tag.TagID;
            mat.materialName = tag.TagPath.Split(".").First().Replace("__chore\\", string.Empty).Replace("gen__\\", string.Empty);
            GetMaterialData(TagLayouts.Tags(tagGroups[tag.TagPath.Split(".").Last()]), mf, 0, obj: mat);

            // Return restult
            return JsonConvert.SerializeObject(mat, Formatting.Indented).ToString();
        }

        private static ModuleFile? TryOpenTag(string tagPath)
        {
            curDataBlockInd = 1;
            ModuleFile? mf = null;
            tStream = ModuleEditor.GetTag(mod, mStream, tagPath);
            mod.ModuleFiles.TryGetValue(mod.ModuleFiles.Keys.ToList().Find(x => x.Contains(tagPath)), out mf);

            if (mf != null)
            {
                mf.Tag = ModuleEditor.ReadTag(tStream, tagPath.Replace("\0", String.Empty), mf);
                mf.Tag.Name = tagPath;
                mf.Tag.ShortName = tagPath.Split("\\").Last();
            }
            
            return mf;
        }

        private static object GetMaterialData(Dictionary<long, TagLayouts.C> tagLayout, ModuleFile mf, long address, bool save = false, object? obj = null)
        {
            byte[] data = mf.Tag.TagData;
            Dictionary<string, ColorVariant> variants = new();

            foreach (KeyValuePair<long, TagLayouts.C> entry in tagLayout)
            {
                try
                {
                    // Set the entries data address
                    entry.Value.MemoryAddress = address + entry.Key;

                    // Set Name and Type
                    string name = "";
                    string type = "";
                    byte[] rawValue = GetDataFromByteArray((int)entry.Value.S, entry.Value.MemoryAddress, data);
                    if (entry.Value.N != null) name = entry.Value.N;
                    if (entry.Value.T != null) type = entry.Value.T;

                    if (mf.Tag.Name.EndsWith("material") && save && obj != null)
                    {
                        Material mat = (Material)obj;
                        switch (name)
                        {
                            case "real":
                                mat.real = BitConverter.ToSingle(rawValue);
                                break;
                            case "vector":
                                mat.vector = new()
                                {
                                    X = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 0),
                                    Y = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 4),
                                    Z = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 8)
                                };
                                break;
                            case "region name":
                                mat.regionName = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                break;
                            case "material style":
                                mat.materialStyleID = ReverseHexString(BitConverter.ToUInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 8, data)).ToString("X"));
                                mat.materialStyleName= IDToTagName(mat.materialStyleID).Split(".").First().Replace("__chore\\", string.Empty).Replace("gen__\\", string.Empty);
                                TagInfo styleTag = GetTagInfo(mat.materialStyleID);
                                ModuleFile? styleFile = TryOpenTag(styleTag.TagPath);
                                
                                if (styleFile != null)
                                {
                                    int temp = curDataBlockInd;
                                    curDataBlockInd = 1;
                                    mat.materialStyles = (List<MaterialStyle>)GetMaterialData(TagLayouts.Tags(tagGroups[styleTag.TagPath.Split(".").Last()]), styleFile, 0);
                                    curDataBlockInd = temp;
                                }
                                break;
                        }
                    }
                    else if (mf.Tag.Name.EndsWith("materialstyles") && save && obj != null)
                    {
                        MaterialStyle style = (MaterialStyle)obj;

                        switch (name)
                        {
                            case "name":
                                style.name = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                break;
                            case "palette":
                                style.paletteID = ReverseHexString(BitConverter.ToUInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 8, data)).ToString("X"));
                                style.paletteName = IDToTagName(style.paletteID).Split(".").First().Replace("__chore\\", string.Empty).Replace("gen__\\", string.Empty);
                                TagInfo palTag = GetTagInfo(style.paletteID);
                                ModuleFile? palFile = TryOpenTag(palTag.TagPath);

                                if (palFile != null)
                                {
                                    int temp = curDataBlockInd;
                                    curDataBlockInd = 1;
                                    style.paletteSwatches = (List<MaterialPalette>)GetMaterialData(TagLayouts.Tags(tagGroups[palTag.TagPath.Split(".").Last()]), palFile, 0);
                                    curDataBlockInd = temp;
                                }
                                break;
                            case "emissive_amount":
                                style.emissiveAmount = BitConverter.ToSingle(rawValue);
                                break;
                            case "scratch_amount":
                                style.scratchAmount = BitConverter.ToSingle(rawValue);
                                break;
                            case "grime_type":
                                style.grimeType = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                break;
                            case "grime_amount":
                                style.grimeAmount = BitConverter.ToSingle(rawValue);
                                break;
                        }
                    }
                    else if (mf.Tag.Name.EndsWith("materialpalette") && save && obj != null)
                    {
                        MaterialPalette pal = (MaterialPalette)obj;
                        if (entry.Value.T == "EnumGroup")
                        {
                            TagLayouts.EnumGroup fg3 = entry.Value as TagLayouts.EnumGroup;
                            int enumValueRaw = 0;
                            if (fg3.N == "roughnessOverride")
                            {
                                if (entry.Value.S == 1)
                                {
                                    enumValueRaw = rawValue[0];
                                }
                                else if (entry.Value.S == 2)
                                {
                                    enumValueRaw = BitConverter.ToInt16(rawValue);
                                }
                                else if (entry.Value.S == 4)
                                {
                                    enumValueRaw = BitConverter.ToInt32(rawValue);
                                }

                                if (fg3.STR.ContainsKey(enumValueRaw))
                                    pal.roughnessOverride = fg3.STR[enumValueRaw];
                                else
                                    pal.roughnessOverride = "Error";
                            }
                        }
                        else
                        {
                            switch (name)
                            {
                                case "name":
                                    pal.name = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                                case "swatch":
                                    pal.swatchID = ReverseHexString(BitConverter.ToUInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 8, data)).ToString("X"));
                                    pal.swatchName = IDToTagName(pal.swatchID);
                                    TagInfo swaTag = GetTagInfo(pal.swatchID);
                                    ModuleFile? swaFile = TryOpenTag(swaTag.TagPath);

                                    if (swaFile != null)
                                    {
                                        MaterialSwatch ms = new();
                                        int temp = curDataBlockInd;
                                        curDataBlockInd = 1;
                                        variants = (Dictionary<string, ColorVariant>)GetMaterialData(TagLayouts.Tags(tagGroups[swaTag.TagPath.Split(".").Last()]), swaFile, 0, obj: ms);
                                        pal.swatchData = ms;
                                        curDataBlockInd = temp;
                                    }
                                    break;
                                case "color":
                                    pal.color = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                                case "emissiveIntensity":
                                    pal.emissiveIntensity = BitConverter.ToSingle(rawValue);
                                    break;
                                case "emissiveAmount":
                                    pal.emissiveAmount = BitConverter.ToSingle(rawValue);
                                    break;
                                case "groupName":
                                    pal.groupName = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                            }
                        }
                    }
                    else if (mf.Tag.Name.EndsWith("materialswatch"))
                    {
                        if (obj != null)
                        {
                            if (save)
                            {
                                ColorVariant cVar = (ColorVariant)obj;
                                switch (name.ToLower())
                                {
                                    case "name":
                                        cVar.name = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                        break;
                                    case "gradient_top_color":
                                        cVar.gradient_top_color.X = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 0);
                                        cVar.gradient_top_color.Y = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 4);
                                        cVar.gradient_top_color.Z = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 8);
                                        break;
                                    case "gradient_mid_color":
                                        cVar.gradient_mid_color.X = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 0);
                                        cVar.gradient_mid_color.Y = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 4);
                                        cVar.gradient_mid_color.Z = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 8);
                                        break;
                                    case "gradient_bottom_color":
                                        cVar.gradient_bottom_color.X = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 0);
                                        cVar.gradient_bottom_color.Y = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 4);
                                        cVar.gradient_bottom_color.Z = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 8);
                                        break;
                                }
                            }
                            else
                            {
                                MaterialSwatch swatch = (MaterialSwatch)obj;
                                switch (name.ToLower())
                                {
                                    case "colorandroughnesstexturetransform": // Bound Float
                                        swatch.colorAndRoughnessTextureTransform.X = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 0);
                                        swatch.colorAndRoughnessTextureTransform.Y = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 4);
                                        break;
                                    case "normaltexturetransform": // Bound Float
                                        swatch.normalTextureTransform.X = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 0);
                                        swatch.normalTextureTransform.Y = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 4);
                                        break;
                                    case "color_gradient_map": // Tag Ref
                                        swatch.color_gradient_map_id = ReverseHexString(BitConverter.ToUInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 8, data)).ToString("X"));
                                        swatch.color_gradient_map = IDToTagName(swatch.color_gradient_map_id);
                                        break;
                                    case "roughness_white": // Float
                                        swatch.roughness_white = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "roughness_black": // Float
                                        swatch.roughness_black = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "normal_detail_map": // Tag Ref
                                        swatch.normal_detail_map_id = ReverseHexString(BitConverter.ToUInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 8, data)).ToString("X"));
                                        swatch.normal_detail_map = IDToTagName(swatch.normal_detail_map_id);
                                        break;
                                    case "metallic": // Float
                                        swatch.metallic = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "ior": // Float
                                        swatch.ior = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "albedo_tint_spec": // Float
                                        swatch.albedo_tint_spec = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "sss_strength": // Float
                                        swatch.sss_strenght = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "scratch_color": // RGB
                                        swatch.scratch_color.X = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 0);
                                        swatch.scratch_color.Y = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 4);
                                        swatch.scratch_color.Z = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 8);
                                        break;
                                    case "scratch_brightness": // Float
                                        swatch.scratch_brightness = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "scratch_roughness": // Float
                                        swatch.scratch_roughness = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "scratch_metallic": // Float
                                        swatch.scratch_metallic = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "scratch_ior": // Float
                                        swatch.scratch_ior = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "scratch_albedo_tint_spec": // Float
                                        swatch.scratch_albedo_tint_spec = BitConverter.ToSingle(rawValue);
                                        break;
                                    case "sss_intensity": // Float
                                        swatch.sss_intensity = BitConverter.ToSingle(rawValue);
                                        break;
                                }
                            }
                        }
                    }

                    // Handle tagblocks
                    switch (type)
                    {
                        case "Tagblock":
                            int childCount = BitConverter.ToInt32(GetDataFromByteArray(20, entry.Value.MemoryAddress, data), 16);
                            int curBlock = 0;
                            if (childCount > 0 && childCount < 100000 && entry.Value.N != "material constants")
                            {
                                curBlock = curDataBlockInd;
                                curDataBlockInd++;
                                List<MaterialStyle> styleData = new();
                                List<MaterialPalette> paletteData = new();
                                Dictionary<string, ColorVariant> colorVariants = new Dictionary<string, ColorVariant>();
                                for (int i = 0; i < childCount; i++)
                                {
                                    long newAddress = (long)mf.Tag.DataBlockArray[curBlock].Offset - (long)mf.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);

                                    if (entry.Value.B != null)
                                    {
                                        if (mf.Tag.Name.EndsWith("material"))
                                        {
                                            if (name == "material parameters" && i == 1)
                                                GetMaterialData(entry.Value.B, mf, newAddress, true, obj);
                                            else if (name == "style info")
                                                GetMaterialData(entry.Value.B, mf, newAddress, true, obj);
                                            else
                                                GetMaterialData(entry.Value.B, mf, newAddress);
                                        }
                                        else if (mf.Tag.Name.EndsWith("materialstyles") && name == "style")
                                        {
                                            MaterialStyle style = new MaterialStyle();
                                            GetMaterialData(entry.Value.B, mf, newAddress, true, style);
                                            styleData.Add(style);
                                        }
                                        else if (mf.Tag.Name.EndsWith("materialpalette") && name == "swatches")
                                        {
                                            MaterialPalette pal = new MaterialPalette();
                                            GetMaterialData(entry.Value.B, mf, newAddress, true, pal);
                                            paletteData.Add(pal);
                                        }
                                        else if (mf.Tag.Name.EndsWith("materialswatch") && name == "color_variants")
                                        {
                                            ColorVariant cVar = new();
                                            GetMaterialData(entry.Value.B, mf, newAddress, true, cVar);
                                            colorVariants.Add(cVar.name, cVar);
                                        }
                                        else
                                        {
                                            GetMaterialData(entry.Value.B, mf, newAddress);
                                        }
                                    }
                                }
                                if (mf.Tag.Name.EndsWith("materialstyles") && name == "style")
                                    return styleData;
                                else if (mf.Tag.Name.EndsWith("materialpalette") && name == "swatches")
                                    return paletteData;
                                if (mf.Tag.Name.EndsWith("materialswatch") && !save)
                                    return colorVariants;
                            }
                            break;
                        case "FUNCTION":
                            int funcSize = BitConverter.ToInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 20, data));
                            if (funcSize > 0)
                            {
                                curDataBlockInd++;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            if (mf.Tag.Name.EndsWith("materialpalette") && obj != null)
            {
                MaterialPalette pal = (MaterialPalette)obj;

                if (pal.color != "" && variants.ContainsKey(pal.color))
                    pal.swatchData.color_variant = variants[pal.color];
            }
            return -1;
        }
    }
}
