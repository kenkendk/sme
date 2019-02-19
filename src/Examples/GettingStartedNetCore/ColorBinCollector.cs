using System;
using SME;

namespace GettingStarted
{
	/// <summary>
	/// The bin counter process
	/// </summary>
	public class ColorBinCollector : SimpleProcess
	{
		/// <summary>
		/// The bus that we read input pixels from
		/// </summary>
        [InputBus]
        private readonly ImageInputLine m_input;

		/// <summary>
		/// The bus that we write results to
		/// </summary>
		[OutputBus]
		public readonly BinCountOutput Output = Scope.CreateBus<BinCountOutput>();

		/// <summary>
		/// The threshold when a pixel is deemed high intensity
		/// </summary>
		const uint HighThreshold = 200;
		/// <summary>
		/// The threshold when a pixel is deemed medium intensity
		/// </summary>
		const uint MediumThreshold = 100;

        /// <summary>
        /// The current number of low intensity pixels
        /// </summary>
        private uint m_low;
        /// <summary>
        /// The current number of medium intensity pixels
        /// </summary>
        private uint m_med;
		/// <summary>
		/// The current number of high intensity pixels
		/// </summary>
		private uint m_high;

		/// <summary>
		/// Constructs a new bin counter process
		/// </summary>
		/// <param name="input">The camera input bus</param>
		public ColorBinCollector(ImageInputLine input)
		{
			// The constructor is not translated into hardware,
			// so it is possible to have dynamic and initialization
			// When the simulation "run" method is called,
			// the values of all variables are captured and used for 
			// initialization
            m_input = input ?? throw new ArgumentNullException(nameof(input));
		}

		/// <summary>
		/// The method invoked when all inputs are ready.
		/// The method is only invoked once pr. clock cycle
		/// </summary>
		protected override void OnTick()
		{
			// If the input pixel is valid, increment the relevant counter
			if (m_input.IsValid)
			{
				//R=0.299, G=0.587, B=0.114
				var color = ((m_input.R * 299u) + (m_input.G * 587u) + (m_input.B * 114u)) / 1000u;
				if (color > HighThreshold)
					m_high++;
				else if (color > MediumThreshold)
					m_med++;
				else
					m_low++;
			}

			// Check if this is the last pixel
            var done = m_input.IsValid && m_input.LastPixel;

			// Send the output
            Output.Low = m_low;
			Output.Medium = m_med;
			Output.High = m_high;
			Output.IsValid = done;
            
			// Make sure we reset if this was the last pixel
			if (done)
                m_low = m_med = m_high = 0;
        }
	}
}
