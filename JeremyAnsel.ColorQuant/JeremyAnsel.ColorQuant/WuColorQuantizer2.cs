// <copyright file="WuColorQuantizer2.cs" company="Jérémy Ansel">
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
    /// A Wu's color quantizer.
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
    public sealed class WuColorQuantizer : IColorQuantizer
    {
        /// <summary>
        /// The maximum color count for the quantize.
        /// </summary>
        public const int MaxColors = 256;

        /// <summary>
        /// The index bits.
        /// </summary>
        private const int IndexBits = 7;

        /// <summary>
        /// The index count.
        /// </summary>
        private const int IndexCount = (1 << IndexBits) + 1;

        /// <summary>
        /// The table length.
        /// </summary>
        private const int TableLength = IndexCount * IndexCount * IndexCount;

        private readonly Vector256<long>[] vm = new Vector256<long>[TableLength];

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
        /// <param name="image">The image (XRGB).</param>
        /// <returns>The result.</returns>
        public ColorQuantizerResult Quantize(byte[] image)
        {
            return this.Quantize(image, 256);
        }

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB).</param>
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

            Box[] cube;
            this.BuildCube(out cube, ref colorCount);

            return this.GenerateResult(image, colorCount, cube);
        }

        /// <summary>
        /// Gets an index.
        /// </summary>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        /// <returns>The index.</returns>
        private static int GetIndex(int r, int g, int b)
        {
            return (r << (IndexBits * 2)) + (r << (IndexBits + 1)) + (g << IndexBits) + r + g + b;
        }

        /// <summary>
        /// Computes sum over a box of any given statistic.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static Vector256<long> Volume(Box cube, Vector256<long>[] moment)
        {
            return moment[cube.Index111]
               - moment[cube.Index110]
               - moment[cube.Index101]
               + moment[cube.Index100]
               - moment[cube.Index011]
               + moment[cube.Index010]
               + moment[cube.Index001]
               - moment[cube.Index000];
        }

        /// <summary>
        /// Computes part of Volume(cube, moment) that doesn't depend on r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static Vector256<long> Bottom(Box cube, int direction, Vector256<long>[] moment)
        {
            switch (direction)
            {
                // Red
                case 2:
                    return -moment[cube.Index011]
                        + moment[cube.Index010]
                        + moment[cube.Index001]
                        - moment[cube.Index000];

                // Green
                case 1:
                    return -moment[cube.Index101]
                        + moment[cube.Index100]
                        + moment[cube.Index001]
                        - moment[cube.Index000];

                // Blue
                case 0:
                    return -moment[cube.Index110]
                        + moment[cube.Index100]
                        + moment[cube.Index010]
                        - moment[cube.Index000];

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
        private static Vector256<long> Top(Box cube, int direction, int position, Vector256<long>[] moment)
        {
            switch (direction)
            {
                // Red
                case 2:
                    return moment[GetIndex(position, cube.G1, cube.B1)]
                       - moment[GetIndex(position, cube.G1, cube.B0)]
                       - moment[GetIndex(position, cube.G0, cube.B1)]
                       + moment[GetIndex(position, cube.G0, cube.B0)];

                // Green
                case 1:
                    return moment[GetIndex(cube.R1, position, cube.B1)]
                       - moment[GetIndex(cube.R1, position, cube.B0)]
                       - moment[GetIndex(cube.R0, position, cube.B1)]
                       + moment[GetIndex(cube.R0, position, cube.B0)];

                // Blue
                case 0:
                    return moment[GetIndex(cube.R1, cube.G1, position)]
                       - moment[GetIndex(cube.R1, cube.G0, position)]
                       - moment[GetIndex(cube.R0, cube.G1, position)]
                       + moment[GetIndex(cube.R0, cube.G0, position)];

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// Clears the tables.
        /// </summary>
        private void Clear()
        {
            this.vm.AsSpan().Clear();
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
                int r = image[i + 2];
                int g = image[i + 1];
                int b = image[i];

                int inr = r >> (8 - IndexBits);
                int ing = g >> (8 - IndexBits);
                int inb = b >> (8 - IndexBits);

                int ind = GetIndex(inr + 1, ing + 1, inb + 1);

                this.vm[ind] += Vector256.Create(r, g, b, 1);
                this.m2[ind] += (r * r) + (g * g) + (b * b);
            }
        }

        /// <summary>
        /// Converts the histogram into moments so that we can rapidly calculate
        /// the sums of the above quantities over any desired box.
        /// </summary>
        private void Get3DMoments()
        {
            Span<Vector256<long>> area = stackalloc Vector256<long>[IndexCount];
            Span<double> area2 = stackalloc double[IndexCount];

            for (int r = 1; r < IndexCount; r++)
            {
                area.Clear();
                area2.Clear();

                for (int g = 1; g < IndexCount; g++)
                {
                    Vector256<long> line = Vector256<long>.Zero;
                    double line2 = 0;

                    for (int b = 1; b < IndexCount; b++)
                    {
                        int ind1 = GetIndex(r, g, b);

                        line += this.vm[ind1];
                        line2 += this.m2[ind1];

                        area[b] += line;
                        area2[b] += line2;

                        int ind2 = ind1 - GetIndex(1, 0, 0);

                        this.vm[ind1] = this.vm[ind2] + area[b];
                        this.m2[ind1] = this.m2[ind2] + area2[b];
                    }
                }
            }
        }

        /// <summary>
        /// Computes the weighted variance of a box.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <returns>The result.</returns>
        private double Variance(Box cube)
        {
            Vector256<long> d = Volume(cube, this.vm);

            double xx =
                this.m2[cube.Index111]
                - this.m2[cube.Index110]
                - this.m2[cube.Index101]
                + this.m2[cube.Index100]
                - this.m2[cube.Index011]
                + this.m2[cube.Index010]
                + this.m2[cube.Index001]
                - this.m2[cube.Index000];

            Vector256<double> dv = Vector256.Create((double)d[0], (double)d[1], (double)d[2], 0.0);
            return xx - (Vector256.Dot(dv, dv) / d[3]);
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
        /// <param name="whole">The whole color and weight.</param>
        /// <returns>The result.</returns>
        private double Maximize(Box cube, int direction, int first, int last, out int cut, in Vector256<long> whole)
        {
            Vector256<long> bases = Bottom(cube, direction, this.vm);

            double max = 0.0;
            cut = -1;

            for (int i = first; i < last; i++)
            {
                Vector256<long> halfs1 = bases + Top(cube, direction, i, this.vm);

                if (halfs1[3] == 0)
                {
                    continue;
                }

                Vector256<long> halfs2 = whole - halfs1;

                if (halfs2[3] == 0)
                {
                    continue;
                }

                Vector256<double> dv1 = Vector256.Create((double)halfs1[0], (double)halfs1[1], (double)halfs1[2], 0.0);
                double temp = Vector256.Dot(dv1, dv1) / halfs1[3];

                Vector256<double> dv2 = Vector256.Create((double)halfs2[0], (double)halfs2[1], (double)halfs2[2], 0.0);
                temp += Vector256.Dot(dv2, dv2) / halfs2[3];

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
        private bool Cut(Box set1, Box set2)
        {
            Vector256<long> whole = Volume(set1, this.vm);

            int cutr;
            int cutg;
            int cutb;

            double maxr = this.Maximize(set1, 2, set1.R0 + 1, set1.R1, out cutr, whole);
            double maxg = this.Maximize(set1, 1, set1.G0 + 1, set1.G1, out cutg, whole);
            double maxb = this.Maximize(set1, 0, set1.B0 + 1, set1.B1, out cutb, whole);

            int dir;

            if ((maxr >= maxg) && (maxr >= maxb))
            {
                dir = 2;

                if (cutr < 0)
                {
                    return false;
                }
            }
            else if ((maxg >= maxr) && (maxg >= maxb))
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

            switch (dir)
            {
                // Red
                case 2:
                    set2.R0 = set1.R1 = cutr;
                    set2.G0 = set1.G0;
                    set2.B0 = set1.B0;
                    break;

                // Green
                case 1:
                    set2.G0 = set1.G1 = cutg;
                    set2.R0 = set1.R0;
                    set2.B0 = set1.B0;
                    break;

                // Blue
                case 0:
                    set2.B0 = set1.B1 = cutb;
                    set2.R0 = set1.R0;
                    set2.G0 = set1.G0;
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
        private void Mark(Box cube, byte label)
        {
            for (int r = cube.R0 + 1; r <= cube.R1; r++)
            {
                for (int g = cube.G0 + 1; g <= cube.G1; g++)
                {
                    for (int b = cube.B0 + 1; b <= cube.B1; b++)
                    {
                        this.tag[GetIndex(r, g, b)] = label;
                    }
                }
            }
        }

        /// <summary>
        /// Builds the cube.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="colorCount">The color count.</param>
        private void BuildCube(out Box[] cube, ref int colorCount)
        {
            cube = new Box[colorCount];
            Span<double> vv = stackalloc double[colorCount];

            for (int i = 0; i < colorCount; i++)
            {
                cube[i] = new Box();
            }

            cube[0].R0 = cube[0].G0 = cube[0].B0 = 0;
            cube[0].R1 = cube[0].G1 = cube[0].B1 = IndexCount - 1;
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
        private ColorQuantizerResult GenerateResult(byte[] image, int colorCount, Box[] cube)
        {
            var quantizedImage = new ColorQuantizerResult(image.Length / 4, colorCount);

            for (int k = 0; k < colorCount; k++)
            {
                this.Mark(cube[k], (byte)k);

                Vector256<long> weight = Volume(cube[k], this.vm);
                double w = weight[3];

                if (w != 0)
                {
                    quantizedImage.Palette[(k * 4) + 3] = 0xff;
                    quantizedImage.Palette[(k * 4) + 2] = (byte)(weight[0] / w);
                    quantizedImage.Palette[(k * 4) + 1] = (byte)(weight[1] / w);
                    quantizedImage.Palette[k * 4] = (byte)(weight[2] / w);
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
                int r = image[(i * 4) + 2] >> (8 - IndexBits);
                int g = image[(i * 4) + 1] >> (8 - IndexBits);
                int b = image[i * 4] >> (8 - IndexBits);

                int ind = GetIndex(r + 1, g + 1, b + 1);

                quantizedImage.Bytes[i] = this.tag[ind];
            }

            return quantizedImage;
        }
    }
}
#endif
