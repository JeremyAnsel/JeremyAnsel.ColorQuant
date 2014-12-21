// <copyright file="QuantizedResult.cs" company="Jérémy Ansel">
// Copyright (c) Jérémy Ansel 2014
// </copyright>

namespace JeremyAnsel.ColorQuant
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A quantized result.
    /// </summary>
    public sealed class QuantizedResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuantizedResult"/> class.
        /// </summary>
        /// <param name="size">The size of the result.</param>
        /// <param name="colorCount">The color count.</param>
        public QuantizedResult(int size, int colorCount)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException("size");
            }

            if (colorCount < 1 || colorCount > 256)
            {
                throw new ArgumentOutOfRangeException("colorCount");
            }

            this.Palette = new byte[colorCount * 4];
            this.Bytes = new byte[size];
        }

        /// <summary>
        /// Gets the palette (XRGB).
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed")]
        public byte[] Palette { get; private set; }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed")]
        public byte[] Bytes { get; private set; }
    }
}
