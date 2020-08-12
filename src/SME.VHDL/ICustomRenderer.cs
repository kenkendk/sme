using System;

namespace SME.VHDL
{
    /// <summary>
    /// Interface for a fully custom renderer process.
    /// </summary>
    public interface IFullCustomRenderer
    {
        /// <summary>
        /// The method used to render the process.
        /// </summary>
        /// <returns>The rendered process.</returns>
        /// <param name="rsp">The renderstate for the process.</param>
        string RenderProcess(RenderStateProcess rsp);
    }

    /// <summary>
    /// Interface for a partially custom renderer process.
    /// </summary>
    public interface ICustomRenderer
    {
        /// <summary>
        /// Method to output the signal region of the component.
        /// </summary>
        /// <returns>The signal region.</returns>
        /// <param name="indentation">The indentation level to use.</param>
        /// <param name="renderer">The renderer building the output</param>
        string IncludeRegion(RenderStateProcess renderer, int indentation);

        /// <summary>
        /// Method to output the body of the component.
        /// </summary>
        /// <returns>The process region.</returns>
        /// <param name="indentation">The indentation level to use.</param>
        /// <param name="renderer">The renderer building the output.</param>
        string BodyRegion(RenderStateProcess renderer, int indentation);
    }
}
