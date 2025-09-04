// <copyright file="WuAlphaColorQuantizer2.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2019 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

#if NET8_0_OR_GREATER
namespace JeremyAnsel.ColorQuant
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Intrinsics;

    /// <summary>
    /// A Wu's color quantizer with alpha channel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Based on C Implementation of Xiaolin Wu's Color Quantizer (v. 2)
    /// (see Graphics Gems volume II, pages 126-133)
    /// (<see href="http://www.ece.mcmaster.ca/~xwu/cq.c"/>).
    /// </para>
    /// <para>
    /// Algorithm: Greedy orthogonal bipartition of RGB space for variance
    /// minimization aided by inclusion-exclusion tricks.
    /// For speed no nearest neighbor search is done. Slightly
    /// better performance can be expected by more sophisticated
    /// but more expensive versions.
    /// </para>
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Wu", Justification = "Reviewed")]
    public sealed class WuAlphaColorQuantizer : IColorQuantizer
    {
        /// <summary>
        /// The maximum color count for the quantize.
        /// </summary>
        public const int MaxColors = 256;

        /// <summary>
        /// The index bits.
        /// </summary>
        private const int IndexBits = 6;

        /// <summary>
        /// The index alpha bits.
        /// </summary>
        private const int IndexAlphaBits = 4;

        /// <summary>
        /// The index count.
        /// </summary>
        private const int IndexCount = (1 << IndexBits) + 1;

        /// <summary>
        /// The index alpha count.
        /// </summary>
        private const int IndexAlphaCount = (1 << IndexAlphaBits) + 1;

        /// <summary>
        /// The table length.
        /// </summary>
        private const int TableLength = IndexCount * IndexCount * IndexCount * IndexAlphaCount;

        /// <summary>
        /// Moment of <c>P(c)</c>.
        /// </summary>
        private readonly long[] vwt = new long[TableLength];

        private readonly Vector256<long>[] vmc = new Vector256<long>[TableLength];

        /// <summary>
        /// Moment of <c>c^2*P(c)</c>.
        /// </summary>
        private readonly double[] m2 = new double[TableLength];

        /// <summary>
        /// Color space tag.
        /// </summary>
        private readonly byte[] tag = new byte[TableLength];

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (ARGB).</param>
        /// <returns>The result.</returns>
        public ColorQuantizerResult Quantize(byte[] image)
        {
            return this.Quantize(image, 256);
        }

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (ARGB).</param>
        /// <param name="colorCount">The color count.</param>
        /// <returns>The result.</returns>
        public ColorQuantizerResult Quantize(byte[] image, int colorCount)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            if (colorCount < 1 || colorCount > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(colorCount));
            }

            this.Clear();

            this.Build3DHistogram(image);
            this.Get3DMoments();

            AlphaBox[] cube;
            this.BuildCube(out cube, ref colorCount);

            return this.GenerateResult(image, colorCount, cube);
        }

        /// <summary>
        /// Gets an index.
        /// </summary>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        /// <param name="a">The alpha value.</param>
        /// <returns>The index.</returns>
        private static int GetIndex(int r, int g, int b, int a)
        {
            return (r << ((IndexBits * 2) + IndexAlphaBits))
                + (r << (IndexBits + IndexAlphaBits + 1))
                + (g << (IndexBits + IndexAlphaBits))
                + (r << (IndexBits * 2))
                + (r << (IndexBits + 1))
                + (g << IndexBits)
                + ((r + g + b) << IndexAlphaBits)
                + r + g + b + a;
        }

        /// <summary>
        /// Computes sum over a box of any given statistic.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static long Volume(AlphaBox cube, long[] moment)
        {
            return moment[cube.Index1111]
                - moment[cube.Index1110]
                - moment[cube.Index1101]
                + moment[cube.Index1100]
                - moment[cube.Index1011]
                + moment[cube.Index1010]
                + moment[cube.Index1001]
                - moment[cube.Index1000]
                - moment[cube.Index0111]
                + moment[cube.Index0110]
                + moment[cube.Index0101]
                - moment[cube.Index0100]
                + moment[cube.Index0011]
                - moment[cube.Index0010]
                - moment[cube.Index0001]
                + moment[cube.Index0000];
        }

        /// <summary>
        /// Computes sum over a box of any given statistic.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static Vector256<long> Volume(AlphaBox cube, Vector256<long>[] moment)
        {
            return moment[cube.Index1111]
                - moment[cube.Index1110]
                - moment[cube.Index1101]
                + moment[cube.Index1100]
                - moment[cube.Index1011]
                + moment[cube.Index1010]
                + moment[cube.Index1001]
                - moment[cube.Index1000]
                - moment[cube.Index0111]
                + moment[cube.Index0110]
                + moment[cube.Index0101]
                - moment[cube.Index0100]
                + moment[cube.Index0011]
                - moment[cube.Index0010]
                - moment[cube.Index0001]
                + moment[cube.Index0000];
        }

        /// <summary>
        /// Computes part of Volume(cube, moment) that doesn't depend on r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static long Bottom(AlphaBox cube, int direction, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 3:
                    return -moment[cube.Index0111]
                        + moment[cube.Index0110]
                        + moment[cube.Index0101]
                        - moment[cube.Index0100]
                        + moment[cube.Index0011]
                        - moment[cube.Index0010]
                        - moment[cube.Index0001]
                        + moment[cube.Index0000];

                // Green
                case 2:
                    return -moment[cube.Index1011]
                        + moment[cube.Index1010]
                        + moment[cube.Index1001]
                        - moment[cube.Index1000]
                        + moment[cube.Index0011]
                        - moment[cube.Index0010]
                        - moment[cube.Index0001]
                        + moment[cube.Index0000];

                // Blue
                case 1:
                    return -moment[cube.Index1101]
                        + moment[cube.Index1100]
                        + moment[cube.Index1001]
                        - moment[cube.Index1000]
                        + moment[cube.Index0101]
                        - moment[cube.Index0100]
                        - moment[cube.Index0001]
                        + moment[cube.Index0000];

                // Alpha
                case 0:
                    return -moment[cube.Index1110]
                        + moment[cube.Index1100]
                        + moment[cube.Index1010]
                        - moment[cube.Index1000]
                        + moment[cube.Index0110]
                        - moment[cube.Index0100]
                        - moment[cube.Index0010]
                        + moment[cube.Index0000];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// Computes part of Volume(cube, moment) that doesn't depend on r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static Vector256<long> Bottom(AlphaBox cube, int direction, Vector256<long>[] moment)
        {
            switch (direction)
            {
                // Red
                case 3:
                    return -moment[cube.Index0111]
                        + moment[cube.Index0110]
                        + moment[cube.Index0101]
                        - moment[cube.Index0100]
                        + moment[cube.Index0011]
                        - moment[cube.Index0010]
                        - moment[cube.Index0001]
                        + moment[cube.Index0000];

                // Green
                case 2:
                    return -moment[cube.Index1011]
                        + moment[cube.Index1010]
                        + moment[cube.Index1001]
                        - moment[cube.Index1000]
                        + moment[cube.Index0011]
                        - moment[cube.Index0010]
                        - moment[cube.Index0001]
                        + moment[cube.Index0000];

                // Blue
                case 1:
                    return -moment[cube.Index1101]
                        + moment[cube.Index1100]
                        + moment[cube.Index1001]
                        - moment[cube.Index1000]
                        + moment[cube.Index0101]
                        - moment[cube.Index0100]
                        - moment[cube.Index0001]
                        + moment[cube.Index0000];

                // Alpha
                case 0:
                    return -moment[cube.Index1110]
                        + moment[cube.Index1100]
                        + moment[cube.Index1010]
                        - moment[cube.Index1000]
                        + moment[cube.Index0110]
                        - moment[cube.Index0100]
                        - moment[cube.Index0010]
                        + moment[cube.Index0000];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// Computes remainder of Volume(cube, moment), substituting position for r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="position">The position.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static long Top(AlphaBox cube, int direction, int position, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 3:
                    return moment[GetIndex(position, cube.G1, cube.B1, cube.A1)]
                        - moment[GetIndex(position, cube.G1, cube.B1, cube.A0)]
                        - moment[GetIndex(position, cube.G1, cube.B0, cube.A1)]
                        + moment[GetIndex(position, cube.G1, cube.B0, cube.A0)]
                        - moment[GetIndex(position, cube.G0, cube.B1, cube.A1)]
                        + moment[GetIndex(position, cube.G0, cube.B1, cube.A0)]
                        + moment[GetIndex(position, cube.G0, cube.B0, cube.A1)]
                        - moment[GetIndex(position, cube.G0, cube.B0, cube.A0)];

                // Green
                case 2:
                    return moment[GetIndex(cube.R1, position, cube.B1, cube.A1)]
                        - moment[GetIndex(cube.R1, position, cube.B1, cube.A0)]
                        - moment[GetIndex(cube.R1, position, cube.B0, cube.A1)]
                        + moment[GetIndex(cube.R1, position, cube.B0, cube.A0)]
                        - moment[GetIndex(cube.R0, position, cube.B1, cube.A1)]
                        + moment[GetIndex(cube.R0, position, cube.B1, cube.A0)]
                        + moment[GetIndex(cube.R0, position, cube.B0, cube.A1)]
                        - moment[GetIndex(cube.R0, position, cube.B0, cube.A0)];

                // Blue
                case 1:
                    return moment[GetIndex(cube.R1, cube.G1, position, cube.A1)]
                        - moment[GetIndex(cube.R1, cube.G1, position, cube.A0)]
                        - moment[GetIndex(cube.R1, cube.G0, position, cube.A1)]
                        + moment[GetIndex(cube.R1, cube.G0, position, cube.A0)]
                        - moment[GetIndex(cube.R0, cube.G1, position, cube.A1)]
                        + moment[GetIndex(cube.R0, cube.G1, position, cube.A0)]
                        + moment[GetIndex(cube.R0, cube.G0, position, cube.A1)]
                        - moment[GetIndex(cube.R0, cube.G0, position, cube.A0)];

                // Alpha
                case 0:
                    return moment[GetIndex(cube.R1, cube.G1, cube.B1, position)]
                        - moment[GetIndex(cube.R1, cube.G1, cube.B0, position)]
                        - moment[GetIndex(cube.R1, cube.G0, cube.B1, position)]
                        + moment[GetIndex(cube.R1, cube.G0, cube.B0, position)]
                        - moment[GetIndex(cube.R0, cube.G1, cube.B1, position)]
                        + moment[GetIndex(cube.R0, cube.G1, cube.B0, position)]
                        + moment[GetIndex(cube.R0, cube.G0, cube.B1, position)]
                        - moment[GetIndex(cube.R0, cube.G0, cube.B0, position)];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// Computes remainder of Volume(cube, moment), substituting position for r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="position">The position.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static Vector256<long> Top(AlphaBox cube, int direction, int position, Vector256<long>[] moment)
        {
            switch (direction)
            {
                // Red
                case 3:
                    return moment[GetIndex(position, cube.G1, cube.B1, cube.A1)]
                        - moment[GetIndex(position, cube.G1, cube.B1, cube.A0)]
                        - moment[GetIndex(position, cube.G1, cube.B0, cube.A1)]
                        + moment[GetIndex(position, cube.G1, cube.B0, cube.A0)]
                        - moment[GetIndex(position, cube.G0, cube.B1, cube.A1)]
                        + moment[GetIndex(position, cube.G0, cube.B1, cube.A0)]
                        + moment[GetIndex(position, cube.G0, cube.B0, cube.A1)]
                        - moment[GetIndex(position, cube.G0, cube.B0, cube.A0)];

                // Green
                case 2:
                    return moment[GetIndex(cube.R1, position, cube.B1, cube.A1)]
                        - moment[GetIndex(cube.R1, position, cube.B1, cube.A0)]
                        - moment[GetIndex(cube.R1, position, cube.B0, cube.A1)]
                        + moment[GetIndex(cube.R1, position, cube.B0, cube.A0)]
                        - moment[GetIndex(cube.R0, position, cube.B1, cube.A1)]
                        + moment[GetIndex(cube.R0, position, cube.B1, cube.A0)]
                        + moment[GetIndex(cube.R0, position, cube.B0, cube.A1)]
                        - moment[GetIndex(cube.R0, position, cube.B0, cube.A0)];

                // Blue
                case 1:
                    return moment[GetIndex(cube.R1, cube.G1, position, cube.A1)]
                        - moment[GetIndex(cube.R1, cube.G1, position, cube.A0)]
                        - moment[GetIndex(cube.R1, cube.G0, position, cube.A1)]
                        + moment[GetIndex(cube.R1, cube.G0, position, cube.A0)]
                        - moment[GetIndex(cube.R0, cube.G1, position, cube.A1)]
                        + moment[GetIndex(cube.R0, cube.G1, position, cube.A0)]
                        + moment[GetIndex(cube.R0, cube.G0, position, cube.A1)]
                        - moment[GetIndex(cube.R0, cube.G0, position, cube.A0)];

                // Alpha
                case 0:
                    return moment[GetIndex(cube.R1, cube.G1, cube.B1, position)]
                        - moment[GetIndex(cube.R1, cube.G1, cube.B0, position)]
                        - moment[GetIndex(cube.R1, cube.G0, cube.B1, position)]
                        + moment[GetIndex(cube.R1, cube.G0, cube.B0, position)]
                        - moment[GetIndex(cube.R0, cube.G1, cube.B1, position)]
                        + moment[GetIndex(cube.R0, cube.G1, cube.B0, position)]
                        + moment[GetIndex(cube.R0, cube.G0, cube.B1, position)]
                        - moment[GetIndex(cube.R0, cube.G0, cube.B0, position)];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// Clears the tables.
        /// </summary>
        private void Clear()
        {
            this.vwt.AsSpan().Clear();
            this.vmc.AsSpan().Clear();
            this.m2.AsSpan().Clear();
            this.tag.AsSpan().Clear();
        }

        /// <summary>
        /// Builds a 3-D color histogram of <c>counts, r/g/b, c^2</c>.
        /// </summary>
        /// <param name="image">The image.</param>
        private void Build3DHistogram(byte[] image)
        {
            for (int i = 0; i < image.Length; i += 4)
            {
                int a = image[i + 3];
                int r = image[i + 2];
                int g = image[i + 1];
                int b = image[i];

                int inr = r >> (8 - IndexBits);
                int ing = g >> (8 - IndexBits);
                int inb = b >> (8 - IndexBits);
                int ina = a >> (8 - IndexAlphaBits);

                int ind = GetIndex(inr + 1, ing + 1, inb + 1, ina + 1);

                this.vwt[ind]++;
                this.vmc[ind] += Vector256.Create(r, g, b, a);
                this.m2[ind] += (r * r) + (g * g) + (b * b) + (a * a);
            }
        }

        /// <summary>
        /// Converts the histogram into moments so that we can rapidly calculate
        /// the sums of the above quantities over any desired box.
        /// </summary>
        private void Get3DMoments()
        {
            Span<long> volume = stackalloc long[IndexCount * IndexAlphaCount];
            Span<Vector256<long>> volumeC = stackalloc Vector256<long>[IndexCount * IndexAlphaCount];
            Span<double> volume2 = stackalloc double[IndexCount * IndexAlphaCount];

            Span<long> area = stackalloc long[IndexAlphaCount];
            Span<Vector256<long>> areaC = stackalloc Vector256<long>[IndexAlphaCount];
            Span<double> area2 = stackalloc double[IndexAlphaCount];

            for (int r = 1; r < IndexCount; r++)
            {
                volume.Clear();
                volumeC.Clear();
                volume2.Clear();

                for (int g = 1; g < IndexCount; g++)
                {
                    area.Clear();
                    areaC.Clear();
                    area2.Clear();

                    for (int b = 1; b < IndexCount; b++)
                    {
                        long line = 0;
                        Vector256<long> lineC = Vector256<long>.Zero;
                        double line2 = 0;

                        for (int a = 1; a < IndexAlphaCount; a++)
                        {
                            int ind1 = GetIndex(r, g, b, a);

                            line += this.vwt[ind1];
                            lineC += this.vmc[ind1];
                            line2 += this.m2[ind1];

                            area[a] += line;
                            areaC[a] += lineC;
                            area2[a] += line2;

                            int inv = (b * IndexAlphaCount) + a;

                            volume[inv] += area[a];
                            volumeC[inv] += areaC[a];
                            volume2[inv] += area2[a];

                            int ind2 = ind1 - GetIndex(1, 0, 0, 0);

                            this.vwt[ind1] = this.vwt[ind2] + volume[inv];
                            this.vmc[ind1] = this.vmc[ind2] + volumeC[inv];
                            this.m2[ind1] = this.m2[ind2] + volume2[inv];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Computes the weighted variance of a box.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <returns>The result.</returns>
        private double Variance(AlphaBox cube)
        {
            Vector256<long> d = Volume(cube, this.vmc);

            double xx =
                this.m2[cube.Index1111]
                - this.m2[cube.Index1110]
                - this.m2[cube.Index1101]
                + this.m2[cube.Index1100]
                - this.m2[cube.Index1011]
                + this.m2[cube.Index1010]
                + this.m2[cube.Index1001]
                - this.m2[cube.Index1000]
                - this.m2[cube.Index0111]
                + this.m2[cube.Index0110]
                + this.m2[cube.Index0101]
                - this.m2[cube.Index0100]
                + this.m2[cube.Index0011]
                - this.m2[cube.Index0010]
                - this.m2[cube.Index0001]
                + this.m2[cube.Index0000];

            Vector256<double> dv = Vector256.Create((double)d[0], (double)d[1], (double)d[2], (double)d[3]);
            return xx - (Vector256.Dot(dv, dv) / Volume(cube, this.vwt));
        }

        /// <summary>
        /// We want to minimize the sum of the variances of two sub-boxes.
        /// The sum(c^2) terms can be ignored since their sum over both sub-boxes
        /// is the same (the sum for the whole box) no matter where we split.
        /// The remaining terms have a minus sign in the variance formula,
        /// so we drop the minus sign and maximize the sum of the two terms.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="first">The first position.</param>
        /// <param name="last">The last position.</param>
        /// <param name="cut">The cutting point.</param>
        /// <param name="wholeC">The whole color.</param>
        /// <param name="wholeW">The whole weight.</param>
        /// <returns>The result.</returns>
        private double Maximize(AlphaBox cube, int direction, int first, int last, out int cut, in Vector256<long> wholeC, long wholeW)
        {
            Vector256<long> baseC = Bottom(cube, direction, this.vmc);
            long baseW = Bottom(cube, direction, this.vwt);

            double max = 0.0;
            cut = -1;

            for (int i = first; i < last; i++)
            {
                long halfW1 = baseW + Top(cube, direction, i, this.vwt);

                if (halfW1 == 0)
                {
                    continue;
                }

                long halfW2 = wholeW - halfW1;

                if (halfW2 == 0)
                {
                    continue;
                }

                Vector256<long> halfC1 = baseC + Top(cube, direction, i, this.vmc);
                Vector256<double> dv1 = Vector256.Create((double)halfC1[0], (double)halfC1[1], (double)halfC1[2], (double)halfC1[3]);
                double temp = Vector256.Dot(dv1, dv1) / halfW1;

                Vector256<long> halfC2 = wholeC - halfC1;
                Vector256<double> dv2 = Vector256.Create((double)halfC2[0], (double)halfC2[1], (double)halfC2[2], (double)halfC2[3]);
                temp += Vector256.Dot(dv2, dv2) / halfW2;

                if (temp > max)
                {
                    max = temp;
                    cut = i;
                }
            }

            return max;
        }

        /// <summary>
        /// Cuts a box.
        /// </summary>
        /// <param name="set1">The first set.</param>
        /// <param name="set2">The second set.</param>
        /// <returns>Returns a value indicating whether the box has been split.</returns>
        private bool Cut(AlphaBox set1, AlphaBox set2)
        {
            Vector256<long> wholeC = Volume(set1, this.vmc);
            long wholeW = Volume(set1, this.vwt);

            int cutr;
            int cutg;
            int cutb;
            int cuta;

            double maxr = this.Maximize(set1, 3, set1.R0 + 1, set1.R1, out cutr, wholeC, wholeW);
            double maxg = this.Maximize(set1, 2, set1.G0 + 1, set1.G1, out cutg, wholeC, wholeW);
            double maxb = this.Maximize(set1, 1, set1.B0 + 1, set1.B1, out cutb, wholeC, wholeW);
            double maxa = this.Maximize(set1, 0, set1.A0 + 1, set1.A1, out cuta, wholeC, wholeW);

            int dir;

            if ((maxr >= maxg) && (maxr >= maxb) && (maxr >= maxa))
            {
                dir = 3;

                if (cutr < 0)
                {
                    return false;
                }
            }
            else if ((maxg >= maxr) && (maxg >= maxb) && (maxg >= maxa))
            {
                dir = 2;
            }
            else if ((maxb >= maxr) && (maxb >= maxg) && (maxb >= maxa))
            {
                dir = 1;
            }
            else
            {
                dir = 0;
            }

            set2.R1 = set1.R1;
            set2.G1 = set1.G1;
            set2.B1 = set1.B1;
            set2.A1 = set1.A1;

            switch (dir)
            {
                // Red
                case 3:
                    set2.R0 = set1.R1 = cutr;
                    set2.G0 = set1.G0;
                    set2.B0 = set1.B0;
                    set2.A0 = set1.A0;
                    break;

                // Green
                case 2:
                    set2.G0 = set1.G1 = cutg;
                    set2.R0 = set1.R0;
                    set2.B0 = set1.B0;
                    set2.A0 = set1.A0;
                    break;

                // Blue
                case 1:
                    set2.B0 = set1.B1 = cutb;
                    set2.R0 = set1.R0;
                    set2.G0 = set1.G0;
                    set2.A0 = set1.A0;
                    break;

                // Alpha
                case 0:
                    set2.A0 = set1.A1 = cuta;
                    set2.R0 = set1.R0;
                    set2.G0 = set1.G0;
                    set2.B0 = set1.B0;
                    break;
            }

            set1.Update();
            set2.Update();

            return true;
        }

        /// <summary>
        /// Marks a color space tag.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="label">A label.</param>
        private void Mark(AlphaBox cube, byte label)
        {
            for (int r = cube.R0 + 1; r <= cube.R1; r++)
            {
                for (int g = cube.G0 + 1; g <= cube.G1; g++)
                {
                    for (int b = cube.B0 + 1; b <= cube.B1; b++)
                    {
                        for (int a = cube.A0 + 1; a <= cube.A1; a++)
                        {
                            this.tag[GetIndex(r, g, b, a)] = label;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Builds the cube.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="colorCount">The color count.</param>
        private void BuildCube(out AlphaBox[] cube, ref int colorCount)
        {
            cube = new AlphaBox[colorCount];
            Span<double> vv = stackalloc double[colorCount];

            for (int i = 0; i < colorCount; i++)
            {
                cube[i] = new AlphaBox();
            }

            cube[0].R0 = cube[0].G0 = cube[0].B0 = cube[0].A0 = 0;
            cube[0].R1 = cube[0].G1 = cube[0].B1 = IndexCount - 1;
            cube[0].A1 = IndexAlphaCount - 1;
            cube[0].Update();

            int next = 0;

            for (int i = 1; i < colorCount; i++)
            {
                if (this.Cut(cube[next], cube[i]))
                {
                    vv[next] = cube[next].Volume > 1 ? this.Variance(cube[next]) : 0.0;
                    vv[i] = cube[i].Volume > 1 ? this.Variance(cube[i]) : 0.0;
                }
                else
                {
                    vv[next] = 0.0;
                    i--;
                }

                next = 0;

                double temp = vv[0];
                for (int k = 1; k <= i; k++)
                {
                    if (vv[k] > temp)
                    {
                        temp = vv[k];
                        next = k;
                    }
                }

                if (temp <= 0.0)
                {
                    colorCount = i + 1;
                    break;
                }
            }
        }

        /// <summary>
        /// Generates the quantized result.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="colorCount">The color count.</param>
        /// <param name="cube">The cube.</param>
        /// <returns>The result.</returns>
        private ColorQuantizerResult GenerateResult(byte[] image, int colorCount, AlphaBox[] cube)
        {
            var quantizedImage = new ColorQuantizerResult(image.Length / 4, colorCount);

            for (int k = 0; k < colorCount; k++)
            {
                this.Mark(cube[k], (byte)k);

                double weight = Volume(cube[k], this.vwt);

                if (weight != 0)
                {
                    Vector256<long> color = Volume(cube[k], this.vmc);
                    quantizedImage.Palette[(k * 4) + 3] = (byte)(color[3] / weight);
                    quantizedImage.Palette[(k * 4) + 2] = (byte)(color[0] / weight);
                    quantizedImage.Palette[(k * 4) + 1] = (byte)(color[1] / weight);
                    quantizedImage.Palette[k * 4] = (byte)(color[2] / weight);
                }
                else
                {
                    quantizedImage.Palette[(k * 4) + 3] = 0xff;
                    quantizedImage.Palette[(k * 4) + 2] = 0;
                    quantizedImage.Palette[(k * 4) + 1] = 0;
                    quantizedImage.Palette[k * 4] = 0;
                }
            }

            for (int i = 0; i < image.Length / 4; i++)
            {
                int a = image[(i * 4) + 3] >> (8 - IndexAlphaBits);
                int r = image[(i * 4) + 2] >> (8 - IndexBits);
                int g = image[(i * 4) + 1] >> (8 - IndexBits);
                int b = image[i * 4] >> (8 - IndexBits);

                int ind = GetIndex(r + 1, g + 1, b + 1, a + 1);

                quantizedImage.Bytes[i] = this.tag[ind];
            }

            return quantizedImage;
        }
    }
}
#endif
