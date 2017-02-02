// rex706's Quick Memory Scanner
// Revision 3.2

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SAM
{
    class AobMemScan
    {
        #region x32

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int size, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        protected static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

        [StructLayout(LayoutKind.Sequential)]
        protected struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private static List<MEMORY_BASIC_INFORMATION> MemReg { get; set; }

        #endregion

        #region x64

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, ulong size, long lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, long size, long lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        protected static extern long VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION_64 lpBuffer, long dwLength);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        [StructLayout(LayoutKind.Sequential)]
        protected struct MEMORY_BASIC_INFORMATION_64
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public ulong AllocationProtect;
            public ulong RegionSize;
            public ulong State;
            public ulong Protect;
            public ulong Type;
        }

        private static List<MEMORY_BASIC_INFORMATION_64> MemReg64 { get; set; }

        #endregion

        private static List<IntPtr> addrList;
        private static List<byte[]> patterns;
        private static List<string> masks;

        private static bool isWow64 = false;
        private static bool usingMemList = false;
        private static bool orderedMemList = false;
        private static bool regionedMemList = false;

        private static int memRegDiffLimit = -1;

        #region Constructors

        /// <summary>
        /// Search for a list of patterns without a mask. Returns a List of pointers to found addresses.
        /// Scan resets to the beginning of the current memory region after each address is found.
        /// </summary>
        public List<IntPtr> RegionedListScan(Process process, List<byte[]> patterns)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            regionedMemList = true;
            AobMemScan.patterns = patterns;
            masks = new List<string>();

            // Generate mask for each pattern.
            for (int i = 0; i < patterns.Count; i++)
            {
                masks.Add(new string('x', patterns[i].Length));
            }

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            regionedMemList = false;
            return addrList;
        }

        /// <summary>
        /// Search for a list of patterns with an accompanying list of masks. Returns a List of pointers to found addresses. 
        /// Scan resets to the beginning of the current memory region after each address is found.
        /// </summary>
        public List<IntPtr> RegionedListScan(Process process, List<byte[]> patterns, List<string> masks)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            AobMemScan.patterns = patterns;
            AobMemScan.masks = masks;

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            regionedMemList = false;
            return addrList;
        }

        /// <summary>
        /// Search for a list of patterns with a memory region difference limit. Returns a List of pointers to found addresses.
        /// Scan resets to the beginning of the current memory region after each address is found.
        /// </summary>
        public List<IntPtr> RegionedListScan(Process process, List<byte[]> patterns, int limit)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            regionedMemList = true;
            AobMemScan.patterns = patterns;
            masks = new List<string>();
            memRegDiffLimit = limit;

            // Generate mask for each pattern.
            for (int i = 0; i < patterns.Count; i++)
            {
                masks.Add(new string('x', patterns[i].Length));
            }

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            regionedMemList = false;
            memRegDiffLimit = -1;
            return addrList;
        }

        /// <summary>
        /// Search for an ordered list of patterns with an accompanying list of masks and a memory region difference limit.
        /// Returns a List of pointers to found addresses.
        /// Scan resets to the beginning of the current memory region after each address is found.
        /// </summary> 
        public List<IntPtr> RegionedListScan(Process process, List<byte[]> patterns, List<string> masks, int limit)
        {
            // Initialize global variables.
            memRegDiffLimit = limit;
            addrList = new List<IntPtr>();
            regionedMemList = true;
            AobMemScan.patterns = patterns;
            AobMemScan.masks = masks;

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            regionedMemList = false;
            memRegDiffLimit = -1;
            return addrList;
        }

        /// <summary>
        /// Search for an ordered list of patterns without a mask.
        /// Returns a List of pointers to found addresses.
        /// Continues search from the location of found address.
        /// </summary>
        public List<IntPtr> OrderedListScan(Process process, List<byte[]> patterns)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            orderedMemList = true;
            AobMemScan.patterns = patterns;
            masks = new List<string>();

            // Generate mask for each pattern.
            for(int i = 0; i < patterns.Count; i++)
            {
                masks.Add(new string('x', patterns[i].Length));
            }

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            orderedMemList = false;
            return addrList;
        }

        /// <summary>
        /// Search for an ordered list of patterns with an accompanying list of masks.
        /// Returns a List of pointers to found addresses. 
        /// Continues search from the location of found address.
        /// </summary>
        public List<IntPtr> OrderedListScan(Process process, List<byte[]> patterns, List<string> masks)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            orderedMemList = true;
            AobMemScan.patterns = patterns;
            AobMemScan.masks = masks;

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            orderedMemList = false;
            return addrList;
        }

        /// <summary>
        /// Search for an ordered list of patterns with a memory region difference limit.
        /// Returns a List of pointers to found addresses.
        /// Continues search from the location of found address.
        /// </summary>
        public List<IntPtr> OrderedListScan(Process process, List<byte[]> patterns, int limit)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            orderedMemList = true;
            AobMemScan.patterns = patterns;
            masks = new List<string>();
            memRegDiffLimit = limit;

            // Generate mask for each pattern.
            for (int i = 0; i < patterns.Count; i++)
            {
                masks.Add(new string('x', patterns[i].Length));
            }

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            orderedMemList = false;
            memRegDiffLimit = -1;
            return addrList;
        }

        /// <summary>
        /// Search for an ordered list of patterns with an accompanying list of masks and a memory region difference limit.
        /// Returns a List of pointers to found addresses.
        /// Continues search from the location of found address.
        /// </summary> 
        public List<IntPtr> OrderedListScan(Process process, List<byte[]> patterns, List<string> masks, int limit)
        {
            // Initialize global variables.
            memRegDiffLimit = limit;
            addrList = new List<IntPtr>();
            orderedMemList = true;
            AobMemScan.patterns = patterns;
            AobMemScan.masks = masks;

            AobScanner(process, patterns[0], masks[0]);

            // Reset and return.
            orderedMemList = false;
            memRegDiffLimit = -1;
            return addrList;
        }

        /// <summary>
        /// Search for a single byte pattern and returns a List of pointers to all found addresses.
        /// </summary>
        public List<IntPtr> ListScan(Process process, byte[] pattern)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            usingMemList = true;
            string mask = new string('x', pattern.Length);

            AobScanner(process, pattern, mask);

            // Reset and return.
            usingMemList = false;
            return addrList;
        }

        /// <summary>
        /// Search for a single byte pattern with a mask and returns a List of pointers to all found addresses.
        /// </summary>
        public List<IntPtr> ListScan(Process process, byte[] pattern, string mask)
        {
            // Initialize global variables.
            addrList = new List<IntPtr>();
            usingMemList = true;

            AobScanner(process, pattern, mask);

            // Reset and return.
            usingMemList = false;
            return addrList;
        }

        /// <summary>
        /// Search for a byte pattern and returns first found address.
        /// </summary>
        public IntPtr Scan(Process process, byte[] pattern)
        {
            string mask = new string('x', pattern.Length);

            return AobScanner(process, pattern, mask);
        }

        /// <summary>
        /// Search for a byte pattern with a mask and returns first found address.
        /// </summary>
        public IntPtr Scan(Process process, byte[] pattern, string mask)
        {
            return AobScanner(process, pattern, mask);
        }

        #endregion

        /// <summary>
        /// Initiate scanning procedure.
        /// </summary>
        private IntPtr AobScanner(Process process, byte[] pattern, string mask)
        {
            // Check process bit structure.
            // isWow64 = IsWow64Process(process.Handle, out isWow64);

            // Generate MemReg.
            MemReg64 = new List<MEMORY_BASIC_INFORMATION_64>();
            MemReg = new List<MEMORY_BASIC_INFORMATION>();
            MemInfo(process.Handle);

            // Initialize variables.
            int prevFoundMemReg = -1;
            int currentPattern = 0;
            int regCount = 0;
            int pool = 0;

            if (isWow64)
                regCount = MemReg64.Count;
            else
                regCount = MemReg.Count;

            // Scan each memory region until we find what we are looking for. 
            for (int currentMemReg = 0; currentMemReg < regCount; currentMemReg++)
            {
                // If a reg limit was set, check bounds.
                if (memRegDiffLimit >= 0 && prevFoundMemReg >= 0 && currentMemReg - prevFoundMemReg > memRegDiffLimit)
                {
                    return IntPtr.Zero;
                }

                byte[] buff;

                // Load buffer with current region's memory.
                if (isWow64)
                {
                    buff = new byte[MemReg64[currentMemReg].RegionSize];
                    ReadProcessMemory(process.Handle, MemReg64[currentMemReg].BaseAddress, buff, MemReg64[currentMemReg].RegionSize, 0);
                }
                else
                {
                    buff = new byte[MemReg[currentMemReg].RegionSize];
                    ReadProcessMemory(process.Handle, MemReg[currentMemReg].BaseAddress, buff, MemReg[currentMemReg].RegionSize, 0);
                }
                
                // Convert string mask to character array.
                char[] char_mask = mask.ToCharArray();

                // Scan buffer for pattern.
                IntPtr result = ScanBuff(buff, pattern, char_mask, pool);

                // If something was found, save or return pointer to its location.
                if (result != IntPtr.Zero)
                {
                    IntPtr address;

                    if (isWow64)
                        address = new IntPtr(MemReg64[currentMemReg].BaseAddress.ToInt64() + result.ToInt64());
                    else
                        address = new IntPtr(MemReg[currentMemReg].BaseAddress.ToInt32() + result.ToInt32());

                    // If using any type of address list. 
                    if (usingMemList || orderedMemList || regionedMemList)
                    {
                        // Add memory address to the list. 
                        addrList.Add(address);

                        // If using special list.
                        if (orderedMemList || regionedMemList)
                        {
                            // If region limit was set, update found memory region.
                            if (memRegDiffLimit >= 0)
                                prevFoundMemReg = currentMemReg;

                            // Increment current pattern. 
                            currentPattern++;

                            // If we found all patterns to search for, return.
                            if (currentPattern == patterns.Count)
                                return address;

                            // Set up next pattern.
                            pattern = patterns[currentPattern];
                            mask = masks[currentPattern];
                        }

                        // Reset current memory region.
                        currentMemReg--;

                        // Update pool to continue from the location scan left off. 
                        if (usingMemList || orderedMemList)
                            pool = result.ToInt32() + 1;
                    }
                    // Return pointer to found address.
                    else
                    {
                        return address;
                    }
                }
            }
            
            // Nothing was found, return zero. 
            return IntPtr.Zero;
        }

        /// <summary>
        /// Create MemInfo struct from given process handle. 
        /// </summary>
        private void MemInfo(IntPtr pHandle)
        {
            IntPtr addy = new IntPtr();

            while (true)
            {
                if (isWow64)
                {
                    MEMORY_BASIC_INFORMATION_64 MemInfo64 = new MEMORY_BASIC_INFORMATION_64();

                    long memDump64 = VirtualQueryEx(pHandle, addy, out MemInfo64, Marshal.SizeOf(MemInfo64));

                    if (memDump64 == 0) break;

                    if ((MemInfo64.State & 0x1000) != 0 && (MemInfo64.Protect & 0x100) == 0)
                        MemReg64.Add(MemInfo64);

                    addy = new IntPtr(MemInfo64.BaseAddress.ToInt64() + (long)MemInfo64.RegionSize);
                }
                else
                {
                    MEMORY_BASIC_INFORMATION MemInfo = new MEMORY_BASIC_INFORMATION();

                    int memDump = VirtualQueryEx(pHandle, addy, out MemInfo, Marshal.SizeOf(MemInfo));

                    if (memDump == 0) break;

                    if ((MemInfo.State & 0x1000) != 0 && (MemInfo.Protect & 0x100) == 0)
                        MemReg.Add(MemInfo);

                    addy = new IntPtr(MemInfo.BaseAddress.ToInt32() + (int)MemInfo.RegionSize);
                }
            }
        }

        /// <summary>
        /// Scan current region's memory dump buffer for given pattern starting from pool.
        /// </summary>
        private IntPtr ScanBuff(byte[] searchIn, byte[] pattern, char[] mask, int pool)
        {
            int[] sBytes = new int[256];
            int end = pattern.Length - 1;

            for (int i = 0; i < 256; i++)
                sBytes[i] = pattern.Length;

            for (int i = 0; i < end; i++)
                sBytes[pattern[i]] = end - i;

            while (pool <= searchIn.Length - pattern.Length)
            {
                for (int i = end; (searchIn[pool + i] == pattern[i] || mask[i] == '?'); i--)
                    if (i == 0)
                        return new IntPtr(pool);

                pool += sBytes[searchIn[pool + end]];
            }

            // If nothing was found, return zero.
            return IntPtr.Zero;
        }
    }
}