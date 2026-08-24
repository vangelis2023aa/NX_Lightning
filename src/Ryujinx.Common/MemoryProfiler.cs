using System;
using System.Threading;

namespace Ryujinx.Common
{
    /// <summary>
    /// Memory profiler for tracking memory usage in MeloNX.
    /// Enable by setting MemoryProfiler.IsEnabled = true.
    /// </summary>
    public static class MemoryProfiler
    {
        private static readonly object _lock = new object();
        private static bool _isEnabled;

        // Managed memory (.NET heap)
        private static long _managedCurrent;
        private static long _managedPeak;

        // Guest memory (committed Switch RAM)
        private static long _guestCurrent;
        private static long _guestPeak;
        private static long _guestPageCount; // number of committed 4KB pages

        // JIT memory (executable code)
        private static long _jitCurrent;
        private static long _jitPeak;
        private static long _jitBlockCount; // number of JIT code blocks

        // GPU textures
        private static long _gpuTexturesCurrent;
        private static long _gpuTexturesPeak;
        private static long _gpuTextureCount;

        // GPU buffers
        private static long _gpuBuffersCurrent;
        private static long _gpuBuffersPeak;
        private static long _gpuBufferCount;

        // Shaders (compiled shader programs)
        private static long _shadersCurrent;
        private static long _shadersPeak;
        private static long _shaderCount;

        // Pipelines (pipeline state objects)
        private static long _pipelinesCurrent;
        private static long _pipelinesPeak;
        private static long _pipelineCount;

        // Pipeline layouts (VkPipelineLayout)
        private static long _pipelineLayoutsCurrent;
        private static long _pipelineLayoutsPeak;
        private static long _pipelineLayoutCount;

        // Descriptor set layouts (VkDescriptorSetLayout)
        private static long _descriptorSetLayoutsCurrent;
        private static long _descriptorSetLayoutsPeak;
        private static long _descriptorSetLayoutCount;

        // Staging buffers (subset of GPU buffers, but tracked separately for clarity)
        private static long _stagingCurrent;
        private static long _stagingPeak;
        private static long _stagingBufferCount;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public static long ManagedCurrent => _managedCurrent;
        public static long ManagedPeak => _managedPeak;
        public static long GuestCurrent => _guestCurrent;
        public static long GuestPeak => _guestPeak;
        public static long GuestPageCount => _guestPageCount;
        public static long JITCurrent => _jitCurrent;
        public static long JITPeak => _jitPeak;
        public static long JITBlockCount => _jitBlockCount;
        public static long GPUTexturesCurrent => _gpuTexturesCurrent;
        public static long GPUTexturesPeak => _gpuTexturesPeak;
        public static long GPUTextureCount => _gpuTextureCount;
        public static long GPUBuffersCurrent => _gpuBuffersCurrent;
        public static long GPUBuffersPeak => _gpuBuffersPeak;
        public static long GPUBufferCount => _gpuBufferCount;
        public static long ShadersCurrent => _shadersCurrent;
        public static long ShadersPeak => _shadersPeak;
        public static long ShaderCount => _shaderCount;
        public static long PipelinesCurrent => _pipelinesCurrent;
        public static long PipelinesPeak => _pipelinesPeak;
        public static long PipelineCount => _pipelineCount;
        public static long PipelineLayoutsCurrent => _pipelineLayoutsCurrent;
        public static long PipelineLayoutsPeak => _pipelineLayoutsPeak;
        public static long PipelineLayoutCount => _pipelineLayoutCount;
        public static long DescriptorSetLayoutsCurrent => _descriptorSetLayoutsCurrent;
        public static long DescriptorSetLayoutsPeak => _descriptorSetLayoutsPeak;
        public static long DescriptorSetLayoutCount => _descriptorSetLayoutCount;
        public static long StagingCurrent => _stagingCurrent;
        public static long StagingPeak => _stagingPeak;
        public static long StagingBufferCount => _stagingBufferCount;

        /// <summary>
        /// Adds to the current guest memory usage.
        /// </summary>
        public static void AddGuestMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _guestCurrent += bytes;
                if (_guestCurrent > _guestPeak) _guestPeak = _guestCurrent;
                _guestPageCount += bytes / 0x1000; // 4KB pages
            }
        }

        /// <summary>
        /// Removes from the current guest memory usage.
        /// </summary>
        public static void RemoveGuestMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _guestCurrent -= bytes;
                _guestPageCount -= bytes / 0x1000;
            }
        }

        /// <summary>
        /// Adds to the current JIT memory usage.
        /// </summary>
        public static void AddJITMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _jitCurrent += bytes;
                if (_jitCurrent > _jitPeak) _jitPeak = _jitCurrent;
                _jitBlockCount++;
            }
        }

        /// <summary>
        /// Removes from the current JIT memory usage.
        /// </summary>
        public static void RemoveJITMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _jitCurrent -= bytes;
                _jitBlockCount--;
            }
        }

        /// <summary>
        /// Adds to the current GPU texture memory usage.
        /// </summary>
        public static void AddGPUTextureMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _gpuTexturesCurrent += bytes;
                if (_gpuTexturesCurrent > _gpuTexturesPeak) _gpuTexturesPeak = _gpuTexturesCurrent;
                _gpuTextureCount++;
            }
        }

        /// <summary>
        /// Removes from the current GPU texture memory usage.
        /// </summary>
        public static void RemoveGPUTextureMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _gpuTexturesCurrent -= bytes;
                _gpuTextureCount--;
            }
        }

        /// <summary>
        /// Adds to the current GPU buffer memory usage.
        /// </summary>
        public static void AddGPUBufferMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _gpuBuffersCurrent += bytes;
                if (_gpuBuffersCurrent > _gpuBuffersPeak) _gpuBuffersPeak = _gpuBuffersCurrent;
                _gpuBufferCount++;
            }
        }

        /// <summary>
        /// Removes from the current GPU buffer memory usage.
        /// </summary>
        public static void RemoveGPUBufferMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _gpuBuffersCurrent -= bytes;
                _gpuBufferCount--;
            }
        }

        /// <summary>
        /// Adds to the current shader memory usage.
        /// </summary>
        public static void AddShaderMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _shadersCurrent += bytes;
                if (_shadersCurrent > _shadersPeak) _shadersPeak = _shadersCurrent;
                _shaderCount++;
            }
        }

        /// <summary>
        /// Removes from the current shader memory usage.
        /// </summary>
        public static void RemoveShaderMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _shadersCurrent -= bytes;
                _shaderCount--;
            }
        }

        /// <summary>
        /// Adds to the current pipeline memory usage.
        /// </summary>
        public static void AddPipelineMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _pipelinesCurrent += bytes;
                if (_pipelinesCurrent > _pipelinesPeak) _pipelinesPeak = _pipelinesCurrent;
                _pipelineCount++;
            }
        }

        /// <summary>
        /// Removes from the current pipeline memory usage.
        /// </summary>
        public static void RemovePipelineMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _pipelinesCurrent -= bytes;
                _pipelineCount--;
            }
        }

        /// <summary>
        /// Adds to the current pipeline layout memory usage.
        /// </summary>
        public static void AddPipelineLayoutMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _pipelineLayoutsCurrent += bytes;
                if (_pipelineLayoutsCurrent > _pipelineLayoutsPeak) _pipelineLayoutsPeak = _pipelineLayoutsCurrent;
                _pipelineLayoutCount++;
            }
        }

        /// <summary>
        /// Removes from the current pipeline layout memory usage.
        /// </summary>
        public static void RemovePipelineLayoutMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _pipelineLayoutsCurrent -= bytes;
                _pipelineLayoutCount--;
            }
        }

        /// <summary>
        /// Adds to the current descriptor set layout memory usage.
        /// </summary>
        public static void AddDescriptorSetLayoutMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _descriptorSetLayoutsCurrent += bytes;
                if (_descriptorSetLayoutsCurrent > _descriptorSetLayoutsPeak) _descriptorSetLayoutsPeak = _descriptorSetLayoutsCurrent;
                _descriptorSetLayoutCount++;
            }
        }

        /// <summary>
        /// Removes from the current descriptor set layout memory usage.
        /// </summary>
        public static void RemoveDescriptorSetLayoutMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _descriptorSetLayoutsCurrent -= bytes;
                _descriptorSetLayoutCount--;
            }
        }

        /// <summary>
        /// Adds to the current staging buffer memory usage.
        /// </summary>
        public static void AddStagingMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _stagingCurrent += bytes;
                if (_stagingCurrent > _stagingPeak) _stagingPeak = _stagingCurrent;
                _stagingBufferCount++;
            }
        }

        /// <summary>
        /// Removes from the current staging buffer memory usage.
        /// </summary>
        public static void RemoveStagingMemory(long bytes)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                _stagingCurrent -= bytes;
                _stagingBufferCount--;
            }
        }

        /// <summary>
        /// Takes a memory snapshot and prints it to the console.
        /// Updates managed memory from GC and includes all tracked categories.
        /// </summary>
        public static void TakeSnapshot()
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                // Update managed memory from GC (without forcing a collection)
                _managedCurrent = GC.GetTotalMemory(false);
                if (_managedCurrent > _managedPeak) _managedPeak = _managedCurrent;

                var totalTracked = _managedCurrent + _guestCurrent + _jitCurrent + _gpuTexturesCurrent + _gpuBuffersCurrent + _shadersCurrent + _pipelinesCurrent + _stagingCurrent;

                Console.WriteLine("MEMORY SNAPSHOT");
                Console.WriteLine("==============");
                Console.WriteLine($"Managed: {FormatBytes(_managedCurrent)} (peak {FormatBytes(_managedPeak)})");
                Console.WriteLine($"Guest: {FormatBytes(_guestCurrent)} (peak {FormatBytes(_guestPeak)})");
                Console.WriteLine($"JIT: {FormatBytes(_jitCurrent)} (peak {FormatBytes(_jitPeak)})");
                Console.WriteLine($"GPU Textures: {FormatBytes(_gpuTexturesCurrent)} (peak {FormatBytes(_gpuTexturesPeak)})");
                Console.WriteLine($"GPU Buffers: {FormatBytes(_gpuBuffersCurrent)} (peak {FormatBytes(_gpuBuffersPeak)})");
                Console.WriteLine($"Shaders: {FormatBytes(_shadersCurrent)} (peak {FormatBytes(_shadersPeak)})");
                Console.WriteLine($"Pipelines: {FormatBytes(_pipelinesCurrent)} (peak {FormatBytes(_pipelinesPeak)})");
                Console.WriteLine($"Staging: {FormatBytes(_stagingCurrent)} (peak {FormatBytes(_stagingPeak)})");
                Console.WriteLine($"Total tracked: {FormatBytes(totalTracked)}");
                Console.WriteLine();
                Console.WriteLine("CACHE DETAILS:");
                Console.WriteLine($"  Guest Pages: {_guestPageCount} (4KB pages)");
                Console.WriteLine($"  JIT Blocks: {_jitBlockCount}");
                Console.WriteLine($"  GPU Textures: {_gpuTextureCount}");
                Console.WriteLine($"  GPU Buffers: {_gpuBufferCount}");
                Console.WriteLine($"  Shaders: {_shaderCount}");
                Console.WriteLine($"  Pipelines: {_pipelineCount}");
                Console.WriteLine($"  Staging Buffers: {_stagingBufferCount}");
                Console.WriteLine();
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 0x1000000000) // 1GB
                return $"{bytes / 4294967296.0:F2} GB";
            if (bytes >= 0x100000) // 1MB
                return $"{bytes / 1048576.0:F2} MB";
            if (bytes >= 0x400) // 1KB
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        // Periodic snapshot timer
        private static System.Timers.Timer _snapshotTimer;

        public static void StartPeriodicSnapshots(int intervalSeconds)
        {
            if (_snapshotTimer != null) return;
            _snapshotTimer = new System.Timers.Timer(intervalSeconds * 1000);
            _snapshotTimer.Elapsed += (s, e) => TakeSnapshot();
            _snapshotTimer.AutoReset = true;
            _snapshotTimer.Start();
        }

        public static void StopPeriodicSnapshots()
        {
            if (_snapshotTimer == null) return;
            _snapshotTimer.Stop();
            _snapshotTimer.Dispose();
            _snapshotTimer = null;
        }
    }
}