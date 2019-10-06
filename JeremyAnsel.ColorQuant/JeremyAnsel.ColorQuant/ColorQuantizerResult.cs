// <copyright file="ColorQuantizerResult.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

namespace JeremyAnsel.ColorQuant
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A result of color quantization.
    /// </summary>
    public sealed class ColorQuantizerResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorQuantizerResult"/> class.
        /// </summary>
        /// <param name="size">The size of the result.</param>
        /// <param name="colorCount">The color count.</param>
        public ColorQuantizerResult(int size, int colorCount)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (colorCount < 1 || colorCount > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(colorCount));
            }

            this.Palette = new byte[colorCount * 4];
            this.Bytes = new byte[size];
        }

        /// <summary>
        /// Gets the palette (XRGB or ARGB).
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
