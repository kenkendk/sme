using System;
using System.Linq;

namespace SME
{
	/// <summary>
	/// Interface for requesting a fixed-length delay-assigned array
	/// </summary>
	public interface IFixedArray<T>
	{
		/// <summary>
		/// Gets or sets the <see cref="T:SME.IFixedArray`1"/> at the specified index.
		/// </summary>
		/// <param name="index">The index to use.</param>
		T this[int index] { get; set; }

		/// <summary>
		/// Gets the length of the array
		/// </summary>
		int Length { get; }
	}

	/// <summary>
	/// Helper interface to access methods in an untyped manner
	/// </summary>
	internal interface IFixedArrayInteraction
	{
		void Propagate();
		void Forward();
	}

	/// <summary>
	/// Implemnetation of an array with a fixed length
	/// </summary>
	internal class FixedArray<T> : IFixedArray<T>, IFixedArrayInteraction
	{
		private T[] m_stage;
		private bool[] m_written; 
		private bool[] m_staged;
		private bool[] m_initialized;
		private T[] m_read;
		private T[] m_write;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SME.FixedArray`1"/> class.
		/// </summary>
		/// <param name="size">Size.</param>
		public FixedArray(int size)
		{
			m_stage = new T[size];
			m_write = new T[size];
			m_read = new T[size];
			m_written = new bool[size];
			m_staged = new bool[size];
			m_initialized = new bool[size];
		}

		/// <summary>
		/// Propagates all writen signals into the read side
		/// </summary>
		public virtual void Propagate()
		{
			Forward();

			Array.Copy(m_write, m_read, m_write.Length);
			Array.Clear(m_written, 0, m_written.Length);
		}

		/// <summary>
		/// Forwards all staged values to the write area
		/// </summary>
		public virtual void Forward()
		{
			for (var i = 0; i < m_written.Length; i++)
			{
				if (m_written[i] && m_staged[i])
					throw new Exception(string.Format("Attempted to perform conflicting write to index {0}", i));
				
				m_initialized[i] |= m_written[i];
				m_written[i] |= m_staged[i];
			}

			Array.Copy(m_stage, m_write, m_write.Length);
			Array.Clear(m_staged, 0, m_staged.Length);
		}

		/// <summary>
		/// Gets or sets the <see cref="T:SME.FixedArray`1"/> at the specified index.
		/// </summary>
		/// <param name="index">The index to use.</param>
		public T this[int index]
		{
			get 
			{
				if (!m_initialized[index])
					throw new ReadViolationException($"Attempted to read index {index} before it has been written");
				return m_read[index];
			}
			set 
			{
				m_staged[index] = true;
				m_stage[index] = value; 
			}
		}

		/// <summary>
		/// Gets the length of the array
		/// </summary>
		public int Length
		{
			get { return m_stage.Length; }
		}
	}
}
