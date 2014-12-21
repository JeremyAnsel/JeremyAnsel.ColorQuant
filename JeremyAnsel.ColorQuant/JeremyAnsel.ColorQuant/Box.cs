// <copyright file="Box.cs" company="Jérémy Ansel">
// Copyright (c) Jérémy Ansel 2014
// </copyright>

namespace JeremyAnsel.ColorQuant
{
    /// <summary>
    /// A box.
    /// </summary>
    internal sealed class Box
    {
        /// <summary>
        /// Gets or sets the min red value, exclusive.
        /// </summary>
        public int R0 { get; set; }

        /// <summary>
        /// Gets or sets the max red value, inclusive.
        /// </summary>
        public int R1 { get; set; }

        /// <summary>
        /// Gets or sets the min green value, exclusive.
        /// </summary>
        public int G0 { get; set; }

        /// <summary>
        /// Gets or sets the max green value, inclusive.
        /// </summary>
        public int G1 { get; set; }

        /// <summary>
        /// Gets or sets the min blue value, exclusive.
        /// </summary>
        public int B0 { get; set; }

        /// <summary>
        /// Gets or sets the max green value, inclusive.
        /// </summary>
        public int B1 { get; set; }

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        public int Volume { get; set; }
    }
}
