using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Represents a program composed of one or more shader stages (for graphics shaders),
    /// or a single shader (for compute shaders).
    /// </summary>
    class CachedShaderProgram : IDisposable
    {
        /// <summary>
        /// Host shader program object.
        /// </summary>
        public IProgram HostProgram { get; }

        /// <summary>
        /// Optional vertex shader converted to compute.
        /// </summary>
        public ShaderAsCompute VertexAsCompute { get; }

        /// <summary>
        /// Optional geometry shader converted to compute.
        /// </summary>
        public ShaderAsCompute GeometryAsCompute { get; }

        /// <summary>
        /// GPU state used to create this version of the shader.
        /// </summary>
        public ShaderSpecializationState SpecializationState { get; }

        /// <summary>
        /// Compiled shader for each shader stage.
        /// </summary>
        public CachedShaderStage[] Shaders { get; }

        /// <summary>
        /// Cached shader bindings, ready for placing into the bindings manager.
        /// </summary>
        public CachedShaderBindings Bindings { get; }

        /// <summary>
        /// Creates a new instance of the shader bundle.
        /// </summary>
        /// <param name="hostProgram">Host program with all the shader stages</param>
        /// <param name="specializationState">GPU state used to create this version of the shader</param>
        /// <param name="shaders">Shaders</param>
        public CachedShaderProgram(IProgram hostProgram, ShaderSpecializationState specializationState, params CachedShaderStage[] shaders)
        {
            HostProgram = hostProgram;
            SpecializationState = specializationState;
            Shaders = shaders;

            SpecializationState.Prepare(shaders);
            Bindings = new CachedShaderBindings(shaders.Length == 1, shaders);

            // Track shader memory allocation
            if (MemoryProfiler.IsEnabled)
            {
                // Estimate shader memory usage based on binary size
                long estimatedSize = 0;
                foreach (var shader in shaders)
                {
                    // This is a rough estimate - in reality we'd need to query the actual binary size
                    estimatedSize += 8192; // 8KB per shader stage as a placeholder
                }
                if (HostProgram != null)
                {
                    // Add estimated size for the program object itself
                    estimatedSize += 4096; // 4KB for program object
                }
                MemoryProfiler.AddShaderMemory(estimatedSize);
            }
        }

        public CachedShaderProgram(
            IProgram hostProgram,
            ShaderAsCompute vertexAsCompute,
            ShaderAsCompute geometryAsCompute,
            ShaderSpecializationState specializationState,
            CachedShaderStage[] shaders) : this(hostProgram, specializationState, shaders)
        {
            VertexAsCompute = vertexAsCompute;
            GeometryAsCompute = geometryAsCompute;
        }

        /// <summary>
        /// Dispose of the host shader resources.
        /// </summary>
        public void Dispose()
        {
            HostProgram.Dispose();
            VertexAsCompute?.HostProgram.Dispose();
            GeometryAsCompute?.HostProgram.Dispose();

            // Track shader memory deallocation
            if (MemoryProfiler.IsEnabled)
            {
                // Estimate shader memory usage based on binary size (same as in constructor)
                long estimatedSize = 0;
                foreach (var shader in Shaders)
                {
                    // This is a rough estimate - in reality we'd need to query the actual binary size
                    estimatedSize += 8192; // 8KB per shader stage as a placeholder
                }
                if (HostProgram != null)
                {
                    // Add estimated size for the program object itself
                    estimatedSize += 4096; // 4KB for program object
                }
                MemoryProfiler.RemoveShaderMemory(estimatedSize);
            }
        }
    }
}
