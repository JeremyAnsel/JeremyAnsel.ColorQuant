// <copyright file="IColorQuantizer.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

namespace JeremyAnsel.ColorQuant
{
    /// <summary>
    /// Defines a color quantizer.
    /// </summary>
    public interface IColorQuantizer
    {
        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB or ARGB).</param>
        /// <returns>The result.</returns>
        ColorQuantizerResult Quantize(byte[] image);

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB or ARGB).</param>
        /// <param name="colorCount">The color count.</param>
        /// <returns>The result.</returns>
        ColorQuantizerResult Quantize(byte[] image, int colorCount);
    }
}
