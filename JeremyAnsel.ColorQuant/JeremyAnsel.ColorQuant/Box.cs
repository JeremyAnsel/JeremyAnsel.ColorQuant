// <copyright file="Box.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

namespace JeremyAnsel.ColorQuant
{
    /// <summary>
    /// A box color cube.
    /// </summary>
    internal sealed class Box
    {
        /// <summary>
        /// The index bits.
        /// </summary>
        private const int IndexBits = 7;

        /// <summary>
        /// Gets or sets the min red value, exclusive.
        /// </summary>
        public int R0;

        /// <summary>
        /// Gets or sets the max red value, inclusive.
        /// </summary>
        public int R1;

        /// <summary>
        /// Gets or sets the min green value, exclusive.
        /// </summary>
        public int G0;

        /// <summary>
        /// Gets or sets the max green value, inclusive.
        /// </summary>
        public int G1;

        /// <summary>
        /// Gets or sets the min blue value, exclusive.
        /// </summary>
        public int B0;

        /// <summary>
        /// Gets or sets the max blue value, inclusive.
        /// </summary>
        public int B1;

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        public int Volume;

        public int Index111;
        public int Index110;
        public int Index101;
        public int Index100;
        public int Index011;
        public int Index010;
        public int Index001;
        public int Index000;

        public void Update()
        {
            Volume = (R1 - R0) * (G1 - G0) * (B1 - B0);

            int r1 = (R1 << (IndexBits * 2)) + (R1 << (IndexBits + 1)) + R1;
            int r0 = (R0 << (IndexBits * 2)) + (R0 << (IndexBits + 1)) + R0;
            int g1 = (G1 << IndexBits) + G1;
            int g0 = (G0 << IndexBits) + G0;
            int b1 = B1;
            int b0 = B0;

            Index111 = r1 + g1 + b1;
            Index110 = r1 + g1 + b0;
            Index101 = r1 + g0 + b1;
            Index100 = r1 + g0 + b0;
            Index011 = r0 + g1 + b1;
            Index010 = r0 + g1 + b0;
            Index001 = r0 + g0 + b1;
            Index000 = r0 + g0 + b0;
        }
    }
}
