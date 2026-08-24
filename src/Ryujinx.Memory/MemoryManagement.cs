using System;
using Ryujinx.Common;

namespace Ryujinx.Memory
{
    public static class MemoryManagement
    {
        public static nint Allocate(ulong size, bool forJit)
        {
            nint ptr;
            if (OperatingSystem.IsWindows())
            {
                ptr = MemoryManagementWindows.Allocate((nint)size);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                ptr = MemoryManagementUnix.Allocate(size, forJit);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (MemoryProfiler.IsEnabled)
            {
                if (forJit)
                {
                    MemoryProfiler.AddJITMemory((long)size);
                }
                else
                {
                    MemoryProfiler.AddGuestMemory((long)size);
                }
            }

            return ptr;
        }

        public static nint Reserve(ulong size, bool forJit, bool viewCompatible)
        {
            if (OperatingSystem.IsWindows())
            {
                return MemoryManagementWindows.Reserve((nint)size, viewCompatible);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                return MemoryManagementUnix.Reserve(size, forJit);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void Commit(nint address, ulong size, bool forJit)
        {
            if (OperatingSystem.IsWindows())
            {
                MemoryManagementWindows.Commit(address, (nint)size);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                MemoryManagementUnix.Commit(address, size, forJit);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            // Track memory commitment
            if (MemoryProfiler.IsEnabled)
            {
                if (forJit)
                {
                    MemoryProfiler.AddJITMemory((long)size);
                }
                else
                {
                    MemoryProfiler.AddGuestMemory((long)size);
                }
            }
        }

        public static void Decommit(nint address, ulong size)
        {
            if (OperatingSystem.IsWindows())
            {
                MemoryManagementWindows.Decommit(address, (nint)size);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                MemoryManagementUnix.Decommit(address, size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            // Track memory decommitment
            if (MemoryProfiler.IsEnabled)
            {
                // Note: We don't have forJit here, so we can't distinguish between JIT and guest memory
                // This is a limitation of the current Decommit signature
                // In practice, we might need to track this elsewhere or modify the signature
                // For now, we'll assume it's guest memory (most common case)
                MemoryProfiler.RemoveGuestMemory((long)size);
            }
        }

        public static void MapView(nint sharedMemory, ulong srcOffset, nint address, ulong size, MemoryBlock owner)
        {
            if (OperatingSystem.IsWindows())
            {
                MemoryManagementWindows.MapView(sharedMemory, srcOffset, address, (nint)size, owner);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                MemoryManagementUnix.MapView(sharedMemory, srcOffset, address, size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void UnmapView(nint sharedMemory, nint address, ulong size, MemoryBlock owner)
        {
            if (OperatingSystem.IsWindows())
            {
                MemoryManagementWindows.UnmapView(sharedMemory, address, (nint)size, owner);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                MemoryManagementUnix.UnmapView(address, size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void Reprotect(nint address, ulong size, MemoryPermission permission, bool forView, bool throwOnFail)
        {
            bool result;

            if (OperatingSystem.IsWindows())
            {
                result = MemoryManagementWindows.Reprotect(address, (nint)size, permission, forView);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                result = MemoryManagementUnix.Reprotect(address, size, permission);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            if (!result && throwOnFail)
            {
                throw new MemoryProtectionException(permission);
            }
        }

        public static bool Free(nint address, ulong size)
        {
            if (OperatingSystem.IsWindows())
            {
                return MemoryManagementWindows.Free(address, (nint)size);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                return MemoryManagementUnix.Free(address);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static nint CreateSharedMemory(ulong size, bool reserve)
        {
            if (OperatingSystem.IsWindows())
            {
                return MemoryManagementWindows.CreateSharedMemory((nint)size, reserve);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                return MemoryManagementUnix.CreateSharedMemory(size, reserve);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void DestroySharedMemory(nint handle)
        {
            if (OperatingSystem.IsWindows())
            {
                MemoryManagementWindows.DestroySharedMemory(handle);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                MemoryManagementUnix.DestroySharedMemory(handle);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static nint MapSharedMemory(nint handle, ulong size)
        {
            if (OperatingSystem.IsWindows())
            {
                return MemoryManagementWindows.MapSharedMemory(handle);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                return MemoryManagementUnix.MapSharedMemory(handle, size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void UnmapSharedMemory(nint address, ulong size)
        {
            if (OperatingSystem.IsWindows())
            {
                MemoryManagementWindows.UnmapSharedMemory(address);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
            {
                MemoryManagementUnix.UnmapSharedMemory(address, size);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
