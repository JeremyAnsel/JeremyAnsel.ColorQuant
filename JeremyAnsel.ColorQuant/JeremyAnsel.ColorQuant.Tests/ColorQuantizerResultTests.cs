// <copyright file="ColorQuantizerResultTests.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

namespace JeremyAnsel.ColorQuant.Tests
{
    using System;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ColorQuantizerResult"/>.
    /// </summary>
    public class ColorQuantizerResultTests
    {
        /// <summary>
        /// Tests the constructor's parameters.
        /// </summary>
        [Fact]
        public void NewResultParameters()
        {
            Assert.Throws<ArgumentOutOfRangeException>("size", () => new ColorQuantizerResult(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("colorCount", () => new ColorQuantizerResult(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("colorCount", () => new ColorQuantizerResult(1, 257));
        }

        /// <summary>
        /// Tests a new result.
        /// </summary>
        [Fact]
        public void NewResult()
        {
            var result = new ColorQuantizerResult(1, 1);

            Assert.Single(result.Bytes);
            Assert.Equal(4, result.Palette.Length);
        }
    }
}
