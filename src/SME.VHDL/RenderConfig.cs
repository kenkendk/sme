using System;
using System.Linq;

namespace SME.VHDL
{
    /// <summary>
    /// Attribute for marking devices with the vendor name.
    /// </summary>
    public class VendorAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the vendor.
        /// </summary>
        /// <value>The vendor.</value>
        public FPGAVendor Vendor { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.VHDL.VendorAttribute"/> class.
        /// </summary>
        /// <param name="vendor">The vendor to set.</param>
        public VendorAttribute(FPGAVendor vendor) { this.Vendor = vendor; }
    }

    /// <summary>
    /// Helper to mark device as Xilinx based.
    /// </summary>
    public class VendorXilinxAttribute : VendorAttribute
    {
        public VendorXilinxAttribute() : base(FPGAVendor.Xilinx) { }
    }
    /// <summary>
    /// Helper to mark device as Altera based.
    /// </summary>
    public class VendorAlteraAttribute : VendorAttribute
    {
        public VendorAlteraAttribute() : base(FPGAVendor.Altera) { }
    }
    /// <summary>
    /// Helper to mark device as Simulation based.
    /// </summary>
    public class VendorSimulationAttribute : VendorAttribute
    {
        public VendorSimulationAttribute() : base(FPGAVendor.Simulation) { }
    }

    /// <summary>
    /// The known vendors.
    /// </summary>
    public enum FPGAVendor
    {
        Xilinx,
        Altera,
        Simulation
    }

    /// <summary>
    /// The known FPGA devices.
    /// </summary>
    public enum FPGADevice
    {
        [VendorSimulation]
        SimulationOnly,

        [VendorXilinx]
        Zynq7000,

        [VendorXilinx]
        ZedBoard,

        [VendorXilinx]
        PynqZ1,

        [VendorXilinx]
        XC7A200T,

        [VendorAltera]
        Arria10
    }

    /// <summary>
    /// The strategy used to render components.
    /// </summary>
    public enum ComponentRendererStrategy
    {
        /// <summary>
        /// No special rendering is used, the SME code is transpiled as-is.
        /// </summary>
        Direct,
        /// <summary>
        /// The components are generated with a pattern that is known to be inferrable by the VHDL compilers.
        /// </summary>
        Inferred,
        /// <summary>
        /// The components are generated as native implementations for the target device.
        /// </summary>
        Native
    }

    /// <summary>
    /// The configuration for the VHDL render.
    /// </summary>
    public class RenderConfig
    {
        /// <summary>
        /// Enable this to use the VHDL 2008 features if the VHDL compiler supports it.
        /// </summary>
        public bool SUPPORTS_VHDL_2008 { get; private set; } = false;

        /// <summary>
        /// Activates explicit selection of the IEEE_1164 concatenation operator.
        /// </summary>
        public bool USE_EXPLICIT_CONCATENATION_OPERATOR { get; private set; } = true;

        /// <summary>
        /// This makes the array lengths use explicit lengths instead of &quot;(x - 1)&quot;.
        /// </summary>
        public bool USE_EXPLICIT_LITERAL_ARRAY_LENGTH { get; private set; } = true;

        /// <summary>
        /// This avoids emitting code with SLL and SRL, and uses shift_left() and shift_right() instead.
        /// </summary>
        public bool AVOID_SLL_AND_SRL { get; private set; } = true;

        /// <summary>
        /// The device vendor.
        /// </summary>
        public FPGAVendor DEVICE_VENDOR => typeof(FPGADevice)
            .GetMember(TARGET_DEVICE.ToString())
            .First()
            .GetCustomAttributes(typeof(VendorAttribute), false)
            .OfType<VendorAttribute>()
            .First()
            .Vendor;

        /// <summary>
        /// The target device.
        /// </summary>
        /// <value>The target device.</value>
        public FPGADevice TARGET_DEVICE { get; private set; } = FPGADevice.Zynq7000;

        /// <summary>
        /// The rendering strategy.
        /// </summary>
        public ComponentRendererStrategy COMPONENT_RENDERER_STRATEGY { get; private set; } = ComponentRendererStrategy.Inferred;
    }
}
