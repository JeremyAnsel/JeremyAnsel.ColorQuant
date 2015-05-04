// <copyright file="WuAlphaColorQuantizerTests.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2015 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

namespace JeremyAnsel.ColorQuant.Tests
{
    using System;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="WuAlphaColorQuantizer"/>.
    /// </summary>
    public class WuAlphaColorQuantizerTests : IClassFixture<WuAlphaColorQuantizer>
    {
        /// <summary>
        /// The alpha color quantizer.
        /// </summary>
        private WuAlphaColorQuantizer quantizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="WuAlphaColorQuantizerTests"/> class.
        /// </summary>
        /// <param name="quantizer">The alpha color quantizer.</param>
        public WuAlphaColorQuantizerTests(WuAlphaColorQuantizer quantizer)
        {
            this.quantizer = quantizer;
        }

        /// <summary>
        /// Tests the constructor's parameters.
        /// </summary>
        [Fact]
        public void QuantizeParameters()
        {
            var image = new byte[0];

            Assert.Throws<ArgumentNullException>("image", () => this.quantizer.Quantize(null));
            Assert.Throws<ArgumentNullException>("image", () => this.quantizer.Quantize(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>("colorCount", () => this.quantizer.Quantize(image, 0));
            Assert.Throws<ArgumentOutOfRangeException>("colorCount", () => this.quantizer.Quantize(image, 257));
        }

        /// <summary>
        /// Tests a single opaque pixel.
        /// </summary>
        [Fact]
        public void SinglePixelOpaque()
        {
            byte[] image = { 0, 0, 0, 255 };

            var result = this.quantizer.Quantize(image);

            Assert.Equal(4, result.Palette.Length);
            Assert.Equal(1, result.Bytes.Length);

            Assert.Equal(new byte[] { 0, 0, 0, 255 }, result.Palette);
            Assert.Equal(new byte[] { 0 }, result.Bytes);
        }

        /// <summary>
        /// Tests a single transparent pixel.
        /// </summary>
        [Fact]
        public void SinglePixelTransparent()
        {
            byte[] image = { 0, 0, 0, 0 };

            var result = this.quantizer.Quantize(image);

            Assert.Equal(4, result.Palette.Length);
            Assert.Equal(1, result.Bytes.Length);
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, result.Palette);
            Assert.Equal(new byte[] { 0 }, result.Bytes);
        }

        /// <summary>
        /// Tests a gray scale.
        /// </summary>
        [Fact]
        public void GrayScale()
        {
            byte[] image = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)i;
                image[i * 4] = c;
                image[(i * 4) + 1] = c;
                image[(i * 4) + 2] = c;
                image[(i * 4) + 3] = 128;
            }

            byte[] expectedImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)((i & ~3) + 1);
                expectedImage[i * 4] = c;
                expectedImage[(i * 4) + 1] = c;
                expectedImage[(i * 4) + 2] = c;
                expectedImage[(i * 4) + 3] = 128;
            }

            var result = this.quantizer.Quantize(image);

            Assert.Equal(4 * 64, result.Palette.Length);
            Assert.Equal(256, result.Bytes.Length);

            byte[] resultImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte pal = result.Bytes[i];

                resultImage[i * 4] = result.Palette[pal * 4];
                resultImage[(i * 4) + 1] = result.Palette[(pal * 4) + 1];
                resultImage[(i * 4) + 2] = result.Palette[(pal * 4) + 2];
                resultImage[(i * 4) + 3] = result.Palette[(pal * 4) + 3];
            }

            Assert.Equal(expectedImage, resultImage);
        }

        /// <summary>
        /// Tests a blue scale.
        /// </summary>
        [Fact]
        public void BlueScale()
        {
            byte[] image = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)i;
                image[i * 4] = c;
                image[(i * 4) + 1] = 0;
                image[(i * 4) + 2] = 0;
                image[(i * 4) + 3] = 128;
            }

            byte[] expectedImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)((i & ~3) + 1);
                expectedImage[i * 4] = c;
                expectedImage[(i * 4) + 1] = 0;
                expectedImage[(i * 4) + 2] = 0;
                expectedImage[(i * 4) + 3] = 128;
            }

            var result = this.quantizer.Quantize(image);

            Assert.Equal(4 * 64, result.Palette.Length);
            Assert.Equal(256, result.Bytes.Length);

            byte[] resultImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte pal = result.Bytes[i];

                resultImage[i * 4] = result.Palette[pal * 4];
                resultImage[(i * 4) + 1] = result.Palette[(pal * 4) + 1];
                resultImage[(i * 4) + 2] = result.Palette[(pal * 4) + 2];
                resultImage[(i * 4) + 3] = result.Palette[(pal * 4) + 3];
            }

            Assert.Equal(expectedImage, resultImage);
        }

        /// <summary>
        /// Tests a green scale.
        /// </summary>
        [Fact]
        public void GreenScale()
        {
            byte[] image = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)i;
                image[i * 4] = 0;
                image[(i * 4) + 1] = c;
                image[(i * 4) + 2] = 0;
                image[(i * 4) + 3] = 128;
            }

            byte[] expectedImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)((i & ~3) + 1);
                expectedImage[i * 4] = 0;
                expectedImage[(i * 4) + 1] = c;
                expectedImage[(i * 4) + 2] = 0;
                expectedImage[(i * 4) + 3] = 128;
            }

            var result = this.quantizer.Quantize(image);

            Assert.Equal(4 * 64, result.Palette.Length);
            Assert.Equal(256, result.Bytes.Length);

            byte[] resultImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte pal = result.Bytes[i];

                resultImage[i * 4] = result.Palette[pal * 4];
                resultImage[(i * 4) + 1] = result.Palette[(pal * 4) + 1];
                resultImage[(i * 4) + 2] = result.Palette[(pal * 4) + 2];
                resultImage[(i * 4) + 3] = result.Palette[(pal * 4) + 3];
            }

            Assert.Equal(expectedImage, resultImage);
        }

        /// <summary>
        /// Tests a red scale.
        /// </summary>
        [Fact]
        public void RedScale()
        {
            byte[] image = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)i;
                image[i * 4] = 0;
                image[(i * 4) + 1] = 0;
                image[(i * 4) + 2] = c;
                image[(i * 4) + 3] = 128;
            }

            byte[] expectedImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)((i & ~3) + 1);
                expectedImage[i * 4] = 0;
                expectedImage[(i * 4) + 1] = 0;
                expectedImage[(i * 4) + 2] = c;
                expectedImage[(i * 4) + 3] = 128;
            }

            var result = this.quantizer.Quantize(image);

            Assert.Equal(4 * 64, result.Palette.Length);
            Assert.Equal(256, result.Bytes.Length);

            byte[] resultImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte pal = result.Bytes[i];

                resultImage[i * 4] = result.Palette[pal * 4];
                resultImage[(i * 4) + 1] = result.Palette[(pal * 4) + 1];
                resultImage[(i * 4) + 2] = result.Palette[(pal * 4) + 2];
                resultImage[(i * 4) + 3] = result.Palette[(pal * 4) + 3];
            }

            Assert.Equal(expectedImage, resultImage);
        }

        /// <summary>
        /// Tests an alpha scale.
        /// </summary>
        [Fact]
        public void AlphaScale()
        {
            byte[] image = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)i;
                image[i * 4] = 0;
                image[(i * 4) + 1] = 0;
                image[(i * 4) + 2] = 0;
                image[(i * 4) + 3] = c;
            }

            byte[] expectedImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte c = (byte)((i & ~31) + 15);
                expectedImage[i * 4] = 0;
                expectedImage[(i * 4) + 1] = 0;
                expectedImage[(i * 4) + 2] = 0;
                expectedImage[(i * 4) + 3] = c;
            }

            var result = this.quantizer.Quantize(image);

            Assert.Equal(4 * 8, result.Palette.Length);
            Assert.Equal(256, result.Bytes.Length);

            byte[] resultImage = new byte[4 * 256];

            for (int i = 0; i < 256; i++)
            {
                byte pal = result.Bytes[i];

                resultImage[i * 4] = result.Palette[pal * 4];
                resultImage[(i * 4) + 1] = result.Palette[(pal * 4) + 1];
                resultImage[(i * 4) + 2] = result.Palette[(pal * 4) + 2];
                resultImage[(i * 4) + 3] = result.Palette[(pal * 4) + 3];
            }

            Assert.Equal(expectedImage, resultImage);
        }
    }
}
