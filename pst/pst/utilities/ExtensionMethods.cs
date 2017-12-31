﻿using pst.encodables.ndb;
using pst.encodables.ndb.btree;
using pst.impl;
using pst.interfaces;
using System;
using System.Collections.Generic;

namespace pst.utilities
{
    static class ExtensionMethods
    {
        public static BinaryData Encode<T>(this T[] array, IEncoder<T> typeEncoder)
        {
            var generator = BinaryDataGenerator.New();

            foreach (var element in array)
            {
                generator.Append(element, typeEncoder);
            }

            return generator.GetData();
        }

        public static T[][] Slice<T>(this T[] array, int itemSize, int maximumSliceSize)
        {
            return array.Slice(new FuncBasedExtractor<T, int>(i => itemSize), maximumSliceSize);
        }

        public static T[][] Slice<T>(this T[] array, IExtractor<T, int> itemSizeExtractor, int maximumSliceSize)
        {
            var slices = new List<T[]>();

            var currentSlice = new List<T>();
            var sizeOfCurrentSlice = 0;

            foreach (var item in array)
            {
                var itemSize = itemSizeExtractor.Extract(item);

                if (sizeOfCurrentSlice + itemSize > maximumSliceSize)
                {
                    slices.Add(currentSlice.ToArray());

                    currentSlice = new List<T>();
                    sizeOfCurrentSlice = 0;
                }

                currentSlice.Add(item);
            }

            return slices.ToArray();
        }

        public static T[][] Slice<T>(this T[] array, int numberOfItemsInSlice)
        {
            var slices = new List<T[]>();

            var numberOfWholeSlices = array.Length / numberOfItemsInSlice;

            for (var i = 0; i < numberOfWholeSlices; i += numberOfItemsInSlice)
            {
                var slice = new T[numberOfItemsInSlice];

                Array.Copy(array, i * numberOfItemsInSlice, slice, 0, numberOfItemsInSlice);

                slices.Add(slice);
            }

            var remainingItemsInOriginalArray = array.Length % numberOfItemsInSlice;

            if (remainingItemsInOriginalArray > 0)
            {
                var slice = new T[remainingItemsInOriginalArray];

                Array.Copy(array, numberOfWholeSlices * numberOfItemsInSlice, slice, 0, remainingItemsInOriginalArray);

                slices.Add(slice);
            }

            return slices.ToArray();
        }

        public static int GetBlockSize(this LBBTEntry entry)
        {
            return Utilities.GetTotalExternalDataBlockSize(entry.ByteCountOfRawDataInReferencedBlockExcludingTrailerAndAlignmentPadding);
        }

        public static byte[] ToBytes(this int[] bits)
        {
            var bytes = new byte[bits.Length / 8];

            for (var i = 0; i < bits.Length; i += 8)
            {
                var @byte = new byte[8];

                Array.Copy(bits, i, @byte, 0, 8);

                bytes[i / 8] =
                    Convert.ToByte(
                        @byte[0] &
                        (@byte[1] << 1) &
                        (@byte[2] << 2) &
                        (@byte[3] << 3) &
                        (@byte[4] << 4) &
                        (@byte[5] << 5) &
                        (@byte[6] << 6) &
                        (@byte[7] << 7));
            }

            return bytes;
        }

        public static int[] ToBits(this byte[] value)
        {
            var bits = new List<int>();

            foreach (var b in value)
            {
                bits.Add(b & 0x01);
                bits.Add((b & 0x02) >> 1);
                bits.Add((b & 0x04) >> 2);
                bits.Add((b & 0x08) >> 3);
                bits.Add((b & 0x10) >> 4);
                bits.Add((b & 0x20) >> 5);
                bits.Add((b & 0x40) >> 6);
                bits.Add((b & 0x80) >> 7);
            }

            return bits.ToArray();
        }

        public static Root SetFileEOF(this Root root, long value)
        {
            return
                new Root(
                    root.Reserved,
                    value,
                    root.AMapLast,
                    root.AMapFree,
                    root.PMapFree,
                    root.NBTRootPage,
                    root.NBTRootPage,
                    root.AMapValid,
                    root.BReserved,
                    root.WReserved);
        }

        public static Root SetLastAMapOffset(this Root root, long value)
        {
            return
                new Root(
                    root.Reserved,
                    root.FileEOF,
                    value,
                    root.AMapFree,
                    root.PMapFree,
                    root.NBTRootPage,
                    root.NBTRootPage,
                    root.AMapValid,
                    root.BReserved,
                    root.WReserved);
        }

        public static Root SetFreeSpaceInAllAMaps(this Root root, long value)
        {
            return
                new Root(
                    root.Reserved,
                    root.FileEOF,
                    root.AMapLast,
                    value,
                    root.PMapFree,
                    root.NBTRootPage,
                    root.NBTRootPage,
                    root.AMapValid,
                    root.BReserved,
                    root.WReserved);
        }

        public static Header SetRoot(this Header header, Root value)
        {
            return
                new Header(
                    header.Magic,
                    header.CRCPartial,
                    header.MagicClient,
                    header.Version,
                    header.ClientVersion,
                    header.PlatformCreate,
                    header.PlatformAccess,
                    header.Reserved1,
                    header.Reserved2,
                    header.UnusedPadding,
                    header.NextPageBID,
                    header.Unique,
                    header.NIDs,
                    header.Unused,
                    value,
                    header.AlignmentBytes,
                    header.FMap,
                    header.FPMap,
                    header.Sentinel,
                    header.CryptMethod,
                    header.Reserved,
                    header.NextBID,
                    header.CRCFull,
                    header.RGBReserved2,
                    header.BReserved,
                    header.RGBReserved3);
        }

        public static Header IncrementBIDIndexForDataBlocks(this Header header)
        {
            return
                new Header(
                    header.Magic,
                    header.CRCPartial,
                    header.MagicClient,
                    header.Version,
                    header.ClientVersion,
                    header.PlatformCreate,
                    header.PlatformAccess,
                    header.Reserved1,
                    header.Reserved2,
                    header.UnusedPadding,
                    header.NextPageBID,
                    header.Unique,
                    header.NIDs,
                    header.Unused,
                    header.Root,
                    header.AlignmentBytes,
                    header.FMap,
                    header.FPMap,
                    header.Sentinel,
                    header.CryptMethod,
                    header.Reserved,
                    header.NextBID + 1,
                    header.CRCFull,
                    header.RGBReserved2,
                    header.BReserved,
                    header.RGBReserved3);
        }

        public static T[] DecodeMultipleItems<T>(this IDecoder<T> decoder, int numberOfItems, int itemSize, BinaryData data)
        {
            var entries = new List<T>();

            for (var i = 0; i < numberOfItems; i++)
            {
                var item = data.Take(i * itemSize, itemSize);

                entries.Add(decoder.Decode(item));
            }

            return entries.ToArray();
        }
    }
}
