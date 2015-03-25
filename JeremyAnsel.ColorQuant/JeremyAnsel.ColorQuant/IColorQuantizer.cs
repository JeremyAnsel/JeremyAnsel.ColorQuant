// <copyright file="IColorQuantizer.cs" company="Jérémy Ansel">
// Copyright (c) Jérémy Ansel 2014
// </copyright>

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
