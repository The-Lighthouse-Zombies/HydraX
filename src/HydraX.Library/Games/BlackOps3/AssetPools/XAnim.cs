﻿using PhilLibX.IO;
using PhilLibX.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace HydraX.Library
{
    partial class BlackOps3
    {
        /// <summary>
        /// Black Ops 3 Raw File Logic
        /// </summary>
        private class XAnim : IAssetPool
        {
            #region AssetStructures
            /// <summary>
            /// XAnim Vector Types
            /// </summary>
            private enum TranslationVectorType : byte
            {
                UShortVec = 0,
                ByteVec = 1,
            }

            /// <summary>
            /// XAnim Part Type
            /// </summary>
            private enum PartType : int
            {
                NoQuat                = 0x0,
                HalfQuat              = 0x1,
                FullQuat              = 0x2,
                HalfQuatNoSize        = 0x3,
                FullQuatNoSize        = 0x4,
                SmallTranslation      = 0x5,
                FullTranslation       = 0x6,
                FullTranslationNoSize = 0x7,
                NoTranslation         = 0x8,
                AllParts              = 0x9,
            }

            /// <summary>
            /// XAnim Notify Info Structure
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 8)]
            private struct XAnimNotifyInfo
            {
                public int Type;
                public float Time;
                public int Param1;
                public int Param2;
            }

            /// <summary>
            /// XAnim Notifications Structure
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 8)]
            private struct XAnimNotifies
            {
                public long NotifyInfoPointer;
                public byte Count;
            }

            /// <summary>
            /// Raw File Asset Structure
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 8)]
            private struct XAnimAsset
            {
                public long NamePointer;
                public int RandomDataByteCount;
                public int DataShortCount;
                public int ExtraChannelDataCount;
                public int DataByteCount;
                public int DataIntCount;
                public int RandomDataIntCount;
                public ushort FrameCount;
                public ushort BoneCount;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
                public byte[] Flags;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
                public ushort[] BoneCounts; // Above is total, this is across different part types, index using the Parts enum
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
                public byte[] Flags2;
                public int RandomDataShortCount;
                public int IndexCount;
                public float FrameRate;
                public float Frequency;
                public float PrimedLength;
                public float LoopEntryTime;
                public int IKPitchLayerCount;
                public int IKPitchBoneCount;
                public long NamesPointer;
                public long DataBytePointer;
                public long DataShortPointer;
                public long DataIntPointer;
                public long RandomDataShortPointer;
                public long RandomDataBytePointer;
                public long RandomDataIntPointer;
                public long ExtraChannelDataPointer;
                public long IndicesPointer;
                public long IKPitchLayersPointer;
                public long IKPitchBonesPointer;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
                public XAnimNotifies[] Notetracks; // Note, Startup, Shutdown
                public long DeltaPartsPointer;

                /// <summary>
                /// Converts pointers to -1 to indicate use
                /// </summary>
                public void ConvertPointers()
                {
                    if (NamePointer > 0) NamePointer                         = 0x4C554C38304335;
                    if (NamesPointer > 0) NamesPointer                       = 0x4C554C38304335;
                    if (DataBytePointer > 0) DataBytePointer                 = 0x4C554C38304335;
                    if (DataShortPointer > 0) DataShortPointer               = 0x4C554C38304335;
                    if (DataIntPointer > 0) DataIntPointer                   = 0x4C554C38304335;
                    if (RandomDataShortPointer > 0) RandomDataShortPointer   = 0x4C554C38304335;
                    if (RandomDataBytePointer > 0) RandomDataBytePointer     = 0x4C554C38304335;
                    if (RandomDataIntPointer > 0) RandomDataIntPointer       = 0x4C554C38304335;
                    if (ExtraChannelDataPointer > 0) ExtraChannelDataPointer = 0x4C554C38304335;
                    if (IndicesPointer > 0) IndicesPointer                   = 0x4C554C38304335;
                    if (IKPitchLayersPointer > 0) IKPitchLayersPointer       = 0x4C554C38304335;
                    if (IKPitchLayersPointer > 0) IKPitchLayersPointer       = 0x4C554C38304335;
                    if (DeltaPartsPointer > 0) DeltaPartsPointer             = 0x4C554C38304335;

                    for(int i = 0; i < Notetracks.Length; i++)
                        if (Notetracks[i].NotifyInfoPointer > 0)
                            Notetracks[i].NotifyInfoPointer = 0x4C554C38304335;
                }
            }

            /// <summary>
            /// XAnim Delta Part Structure
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 8)]
            private struct XAnimDeltaPart
            {
                public long TranslationsPointer;
                public long Quaternions2DPointer;
                public long QuaternionsPointer;

                /// <summary>
                /// Converts pointers to -1 to indicate use
                /// </summary>
                public void ConvertPointers()
                {
                    if (TranslationsPointer > 0) TranslationsPointer = 0x4C554C38304335;
                    if (Quaternions2DPointer > 0) Quaternions2DPointer = 0x4C554C38304335;
                    if (QuaternionsPointer > 0) QuaternionsPointer = 0x4C554C38304335;
                }
            }

            /// <summary>
            /// XAnim Delta Part Translations Structure
            /// </summary>
            [StructLayout(LayoutKind.Explicit)]
            private struct XAnimDeltaPartTrans
            {
                [FieldOffset(0)]
                public ushort Size;
                [FieldOffset(2)]
                public byte SmallTrans;
                [FieldOffset(8)]
                public Vector3 Frame0;
                [FieldOffset(8)]
                public Vector3 Min;
                [FieldOffset(20)]
                public Vector3 Max;
                [FieldOffset(32)]
                public long FramesPointer;
            }

            /// <summary>
            /// XAnim Types
            /// </summary>
            public string[] XAnimTypes =
            {
                "absolute",
                "relative",
                "delta",
                "mp_torso",
                "mp_legs",
                "mp_fullbody",
                "additive",
                "delta3d",
            };
            #endregion

            /// <summary>
            /// Size of each asset
            /// </summary>
            public int AssetSize { get; set; }

            /// <summary>
            /// Gets or Sets the number of Assets 
            /// </summary>
            public int AssetCount { get; set; }

            /// <summary>
            /// Gets or Sets the Start Address
            /// </summary>
            public long StartAddress { get; set; }

            /// <summary>
            /// Gets or Sets the End Address
            /// </summary>
            public long EndAddress { get { return StartAddress + (AssetCount * AssetSize); } set => throw new NotImplementedException(); }

            /// <summary>
            /// Gets the Name of this Pool
            /// </summary>
            public string Name => "xanim";

            /// <summary>
            /// Gets the Setting Group for this Pool
            /// </summary>
            public string SettingGroup => "Misc";

            /// <summary>
            /// Gets the Index of this Pool
            /// </summary>
            public int Index => (int)AssetPool.xanim;

            /// <summary>
            /// Loads Assets from this Asset Pool
            /// </summary>
            public List<GameAsset> Load(HydraInstance instance)
            {

                var results = new List<GameAsset>();

                // Not complete
                return results;


                var poolInfo = instance.Reader.ReadStruct<AssetPoolInfo>(instance.Game.BaseAddress + instance.Game.AssetPoolsAddresses[instance.Game.ProcessIndex] + (Index * 0x20));

                StartAddress = poolInfo.PoolPointer;
                AssetSize = poolInfo.AssetSize;
                AssetCount = poolInfo.PoolSize;

                var voidXAnim = new XAnimAsset();

                for(int i = 0; i < AssetCount; i++)
                {
                    var header = instance.Reader.ReadStruct<XAnimAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;

                    var name = instance.Reader.ReadNullTerminatedString(header.NamePointer);

                    if (header.DataBytePointer        != voidXAnim.DataBytePointer &&
                        header.DataShortPointer       != voidXAnim.DataShortPointer &&
                        header.DataIntPointer         != voidXAnim.DataIntPointer &&
                        header.RandomDataBytePointer  != voidXAnim.RandomDataBytePointer &&
                        header.RandomDataShortPointer != voidXAnim.RandomDataShortPointer &&
                        header.RandomDataIntPointer   != voidXAnim.RandomDataBytePointer)
                    {
                        results.Add(new GameAsset()
                        {
                            Name = name,
                            HeaderAddress = StartAddress + (i * AssetSize),
                            AssetPool = this,
                            Type = Name,
                            Information = string.Format("Bones: {0} Frames: {1} Type: {2}", header.BoneCount, header.FrameCount, XAnimTypes[header.Flags2[0]])
                        });
                    }
                    else if(name == "void")
                    {
                        voidXAnim = header;
                        continue;
                    }


                }

                return results;
            }

            /// <summary>
            /// Exports the given asset from this pool
            /// </summary>
            public HydraStatus Export(GameAsset asset, HydraInstance instance)
            {
                var xanimAsset = instance.Reader.ReadStruct<XAnimAsset>(asset.HeaderAddress);

                if (asset.Name != instance.Reader.ReadNullTerminatedString(xanimAsset.NamePointer))
                    return HydraStatus.MemoryChanged;

                Console.WriteLine(xanimAsset.DeltaPartsPointer);

                string path = Path.Combine("exported_files", instance.Game.Name, "share", "raw", "xanim", asset.Name + ".xanim_raw");
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    var fileHeader = instance.Reader.ReadStruct<XAnimAsset>(asset.HeaderAddress);
                    fileHeader.ConvertPointers();

                    writer.WriteStruct(fileHeader);

                    writer.WriteNullTerminatedString(instance.Reader.ReadNullTerminatedString(xanimAsset.NamePointer));

                    for (int i = 0; i < xanimAsset.BoneCount; i++)
                        writer.WriteNullTerminatedString(instance.Game.GetString(instance.Reader.ReadInt32(xanimAsset.NamesPointer + i * 4), instance));

                    writer.Write(instance.Reader.ReadBytes(xanimAsset.DataBytePointer,          xanimAsset.DataByteCount));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.DataShortPointer,         xanimAsset.DataShortCount * 2));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.DataIntPointer,           xanimAsset.DataIntCount * 4));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.RandomDataBytePointer,    xanimAsset.RandomDataByteCount));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.RandomDataShortPointer,   xanimAsset.RandomDataShortCount * 2));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.RandomDataIntPointer,     xanimAsset.RandomDataIntCount * 4));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.ExtraChannelDataPointer,  xanimAsset.ExtraChannelDataCount));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.IKPitchLayersPointer,     xanimAsset.IKPitchLayerCount * 8));
                    writer.Write(instance.Reader.ReadBytes(xanimAsset.IKPitchBonesPointer,      xanimAsset.IKPitchBoneCount * 28));

                    for (int i = 0; i < xanimAsset.Notetracks.Length; i++)
                    {
                        var notifyInfo = instance.Reader.ReadArray<XAnimNotifyInfo>(xanimAsset.Notetracks[i].NotifyInfoPointer, xanimAsset.Notetracks[i].Count);

                        foreach (var notify in notifyInfo)
                        {
                            writer.Write(notify.Time);
                            writer.WriteNullTerminatedString(instance.Game.GetString(notify.Type, instance));
                            writer.WriteNullTerminatedString(instance.Game.GetString(notify.Param1, instance));
                            writer.WriteNullTerminatedString(instance.Game.GetString(notify.Param2, instance));
                        }
                    }

                    // Delta data requires a bit more work
                    if(xanimAsset.DeltaPartsPointer > 0)
                    {
                        var xanimDeltaParts = instance.Reader.ReadStruct<XAnimDeltaPart>(xanimAsset.DeltaPartsPointer);
                        var xanimDeltaPartsFile = instance.Reader.ReadStruct<XAnimDeltaPart>(xanimAsset.DeltaPartsPointer);
                        xanimDeltaPartsFile.ConvertPointers();

                        writer.WriteStruct(xanimDeltaPartsFile);

                        if(xanimDeltaParts.TranslationsPointer > 0)
                        {
                            var translationCount = instance.Reader.ReadUInt16(xanimDeltaParts.TranslationsPointer) + 1;
                            var byteTranslations = instance.Reader.ReadUInt16(xanimDeltaParts.TranslationsPointer + 2);

                            writer.Write(translationCount);
                            writer.Write(byteTranslations);

                            if (translationCount == 1)
                            {
                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.TranslationsPointer + 8, 12));
                            }
                            else
                            {
                                var indexBufferSize = xanimAsset.FrameCount >= 0x100 ? translationCount * 2 : translationCount;
                                var frameBufferSize = byteTranslations == 0 ? translationCount * 6 : translationCount * 3;

                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.TranslationsPointer + 8, 12));
                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.TranslationsPointer + 20, 12));
                                writer.Write(instance.Reader.ReadBytes(instance.Reader.ReadInt64(xanimDeltaParts.TranslationsPointer + 32), frameBufferSize));
                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.TranslationsPointer + 40, indexBufferSize));
                            }
                        }

                        if (xanimDeltaParts.Quaternions2DPointer > 0)
                        {
                            var rotationCount = instance.Reader.ReadUInt16(xanimDeltaParts.Quaternions2DPointer) + 1;

                            writer.Write(rotationCount);

                            if (rotationCount == 1)
                            {
                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.Quaternions2DPointer + 8, 4));
                            }
                            else
                            {
                                var indexBufferSize = xanimAsset.FrameCount >= 0x100 ? rotationCount * 2 : rotationCount;

                                writer.Write(instance.Reader.ReadBytes(instance.Reader.ReadInt64(xanimDeltaParts.Quaternions2DPointer + 8), rotationCount * 4));
                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.Quaternions2DPointer + 16, indexBufferSize));
                            }
                        }

                        if (xanimDeltaParts.QuaternionsPointer > 0)
                        {
                            var rotationCount = instance.Reader.ReadUInt16(xanimDeltaParts.QuaternionsPointer) + 1;

                            writer.Write(rotationCount);

                            if (rotationCount == 1)
                            {
                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.QuaternionsPointer + 8, 8));
                            }
                            else
                            {
                                var indexBufferSize = xanimAsset.FrameCount >= 0x100 ? rotationCount * 2 : rotationCount;

                                writer.Write(instance.Reader.ReadBytes(instance.Reader.ReadInt64(xanimDeltaParts.QuaternionsPointer + 8), rotationCount * 8));
                                writer.Write(instance.Reader.ReadBytes(xanimDeltaParts.QuaternionsPointer + 16, indexBufferSize));
                            }
                        }
                    }
                }

                return HydraStatus.Success;
            }

            /// <summary>
            /// Checks if the given asset is a null slot
            /// </summary>
            public bool IsNullAsset(GameAsset asset)
            {
                return IsNullAsset(asset.NameLocation);
            }

            /// <summary>
            /// Checks if the given asset is a null slot
            /// </summary>
            public bool IsNullAsset(long nameAddress)
            {
                return nameAddress >= StartAddress && nameAddress <= EndAddress || nameAddress == 0;
            }
        }
    }
}