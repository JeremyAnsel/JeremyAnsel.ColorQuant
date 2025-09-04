// <copyright file="AlphaBox.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

namespace JeremyAnsel.ColorQuant
{
    /// <summary>
    /// A box color cube with alpha.
    /// </summary>
    internal sealed class AlphaBox
    {
        /// <summary>
        /// The index bits.
        /// </summary>
        private const int IndexBits = 6;

        /// <summary>
        /// The index alpha bits.
        /// </summary>
        private const int IndexAlphaBits = 4;

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
        /// Gets or sets the min alpha value, exclusive.
        /// </summary>
        public int A0;

        /// <summary>
        /// Gets or sets the max alpha value, inclusive.
        /// </summary>
        public int A1;

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        public int Volume;

        public int Index1111;
        public int Index1110;
        public int Index1101;
        public int Index1100;
        public int Index1011;
        public int Index1010;
        public int Index1001;
        public int Index1000;
        public int Index0111;
        public int Index0110;
        public int Index0101;
        public int Index0100;
        public int Index0011;
        public int Index0010;
        public int Index0001;
        public int Index0000;

        public void Update()
        {
            Volume = (R1 - R0) * (G1 - G0) * (B1 - B0) * (A1 - A0);

            int r1 = (R1 << ((IndexBits * 2) + IndexAlphaBits)) + (R1 << (IndexBits + IndexAlphaBits + 1)) + (R1 << (IndexBits * 2)) + (R1 << (IndexBits + 1)) + (R1 << IndexAlphaBits) + R1;
            int r0 = (R0 << ((IndexBits * 2) + IndexAlphaBits)) + (R0 << (IndexBits + IndexAlphaBits + 1)) + (R0 << (IndexBits * 2)) + (R0 << (IndexBits + 1)) + (R0 << IndexAlphaBits) + R0;
            int g1 = (G1 << (IndexBits + IndexAlphaBits)) + (G1 << IndexBits) + (G1 << IndexAlphaBits) + G1;
            int g0 = (G0 << (IndexBits + IndexAlphaBits)) + (G0 << IndexBits) + (G0 << IndexAlphaBits) + G0;
            int b1 = (B1 << IndexAlphaBits) + B1;
            int b0 = (B0 << IndexAlphaBits) + B0;
            int a1 = A1;
            int a0 = A0;

            Index1111 = r1 + g1 + b1 + a1;
            Index1110 = r1 + g1 + b1 + a0;
            Index1101 = r1 + g1 + b0 + a1;
            Index1100 = r1 + g1 + b0 + a0;
            Index1011 = r1 + g0 + b1 + a1;
            Index1010 = r1 + g0 + b1 + a0;
            Index1001 = r1 + g0 + b0 + a1;
            Index1000 = r1 + g0 + b0 + a0;
            Index0111 = r0 + g1 + b1 + a1;
            Index0110 = r0 + g1 + b1 + a0;
            Index0101 = r0 + g1 + b0 + a1;
            Index0100 = r0 + g1 + b0 + a0;
            Index0011 = r0 + g0 + b1 + a1;
            Index0010 = r0 + g0 + b1 + a0;
            Index0001 = r0 + g0 + b0 + a1;
            Index0000 = r0 + g0 + b0 + a0;
        }
    }
}
