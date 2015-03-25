// <copyright file="WuColorQuantizer.cs" company="Jérémy Ansel">
// Copyright (c) 2014-2015 Jérémy Ansel
// </copyright>
// <license>
// Licensed under the MIT license. See LICENSE.txt
// </license>

namespace JeremyAnsel.ColorQuant
{
    using System;
    using System.Diagnostics.CodeAnalysis;

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
        /// The index bits.
        /// </summary>
        private const int IndexBits = 7;

        /// <summary>
        /// The index count.
        /// </summary>
        private const int IndexCount = (1 << WuColorQuantizer.IndexBits) + 1;

        /// <summary>
        /// The table length.
        /// </summary>
        private const int TableLength = WuColorQuantizer.IndexCount * WuColorQuantizer.IndexCount * WuColorQuantizer.IndexCount;

        /// <summary>
        /// Moment of <c>P(c)</c>.
        /// </summary>
        private long[] vwt;

        /// <summary>
        /// Moment of <c>r*P(c)</c>.
        /// </summary>
        private long[] vmr;

        /// <summary>
        /// Moment of <c>g*P(c)</c>.
        /// </summary>
        private long[] vmg;

        /// <summary>
        /// Moment of <c>b*P(c)</c>.
        /// </summary>
        private long[] vmb;

        /// <summary>
        /// Moment of <c>c^2*P(c)</c>.
        /// </summary>
        private double[] m2;

        /// <summary>
        /// Color space tag.
        /// </summary>
        private byte[] tag;

        /// <summary>
        /// Initializes a new instance of the <see cref="WuColorQuantizer"/> class.
        /// </summary>
        public WuColorQuantizer()
        {
            this.vwt = new long[WuColorQuantizer.TableLength];
            this.vmr = new long[WuColorQuantizer.TableLength];
            this.vmg = new long[WuColorQuantizer.TableLength];
            this.vmb = new long[WuColorQuantizer.TableLength];
            this.m2 = new double[WuColorQuantizer.TableLength];

            this.tag = new byte[WuColorQuantizer.TableLength];
        }

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
                throw new ArgumentNullException("image");
            }

            if (colorCount < 1 || colorCount > 256)
            {
                throw new ArgumentOutOfRangeException("colorCount");
            }

            this.Clear();

            this.Hist3d(image);
            this.M3d();

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
        private static int Ind(int r, int g, int b)
        {
            return (r << (WuColorQuantizer.IndexBits * 2)) + (r << (WuColorQuantizer.IndexBits + 1)) + (g << WuColorQuantizer.IndexBits) + r + g + b;
        }

        /// <summary>
        /// Computes sum over a box of any given statistic.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static double Volume(Box cube, long[] moment)
        {
            return moment[WuColorQuantizer.Ind(cube.R1, cube.G1, cube.B1)]
               - moment[WuColorQuantizer.Ind(cube.R1, cube.G1, cube.B0)]
               - moment[WuColorQuantizer.Ind(cube.R1, cube.G0, cube.B1)]
               + moment[WuColorQuantizer.Ind(cube.R1, cube.G0, cube.B0)]
               - moment[WuColorQuantizer.Ind(cube.R0, cube.G1, cube.B1)]
               + moment[WuColorQuantizer.Ind(cube.R0, cube.G1, cube.B0)]
               + moment[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B1)]
               - moment[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B0)];
        }

        /// <summary>
        /// Computes part of Volume(cube, moment) that doesn't depend on r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static long Bottom(Box cube, int direction, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 2:
                    return -moment[WuColorQuantizer.Ind(cube.R0, cube.G1, cube.B1)]
                        + moment[WuColorQuantizer.Ind(cube.R0, cube.G1, cube.B0)]
                        + moment[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B1)]
                        - moment[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B0)];

                // Green
                case 1:
                    return -moment[WuColorQuantizer.Ind(cube.R1, cube.G0, cube.B1)]
                        + moment[WuColorQuantizer.Ind(cube.R1, cube.G0, cube.B0)]
                        + moment[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B1)]
                        - moment[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B0)];

                // Blue
                case 0:
                    return -moment[WuColorQuantizer.Ind(cube.R1, cube.G1, cube.B0)]
                        + moment[WuColorQuantizer.Ind(cube.R1, cube.G0, cube.B0)]
                        + moment[WuColorQuantizer.Ind(cube.R0, cube.G1, cube.B0)]
                        - moment[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B0)];

                default:
                    throw new ArgumentOutOfRangeException("direction");
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
        private static long Top(Box cube, int direction, int position, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 2:
                    return moment[WuColorQuantizer.Ind(position, cube.G1, cube.B1)]
                       - moment[WuColorQuantizer.Ind(position, cube.G1, cube.B0)]
                       - moment[WuColorQuantizer.Ind(position, cube.G0, cube.B1)]
                       + moment[WuColorQuantizer.Ind(position, cube.G0, cube.B0)];

                // Green
                case 1:
                    return moment[WuColorQuantizer.Ind(cube.R1, position, cube.B1)]
                       - moment[WuColorQuantizer.Ind(cube.R1, position, cube.B0)]
                       - moment[WuColorQuantizer.Ind(cube.R0, position, cube.B1)]
                       + moment[WuColorQuantizer.Ind(cube.R0, position, cube.B0)];

                // Blue
                case 0:
                    return moment[WuColorQuantizer.Ind(cube.R1, cube.G1, position)]
                       - moment[WuColorQuantizer.Ind(cube.R1, cube.G0, position)]
                       - moment[WuColorQuantizer.Ind(cube.R0, cube.G1, position)]
                       + moment[WuColorQuantizer.Ind(cube.R0, cube.G0, position)];

                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        /// <summary>
        /// Clears the tables.
        /// </summary>
        private void Clear()
        {
            Array.Clear(this.vwt, 0, WuColorQuantizer.TableLength);
            Array.Clear(this.vmr, 0, WuColorQuantizer.TableLength);
            Array.Clear(this.vmg, 0, WuColorQuantizer.TableLength);
            Array.Clear(this.vmb, 0, WuColorQuantizer.TableLength);
            Array.Clear(this.m2, 0, WuColorQuantizer.TableLength);

            Array.Clear(this.tag, 0, WuColorQuantizer.TableLength);
        }

        /// <summary>
        /// Builds a 3-D color histogram of <c>counts, r/g/b, c^2</c>.
        /// </summary>
        /// <param name="image">The image.</param>
        private void Hist3d(byte[] image)
        {
            for (int i = 0; i < image.Length; i += 4)
            {
                int r = image[i + 2];
                int g = image[i + 1];
                int b = image[i];

                int inr = r >> (8 - WuColorQuantizer.IndexBits);
                int ing = g >> (8 - WuColorQuantizer.IndexBits);
                int inb = b >> (8 - WuColorQuantizer.IndexBits);

                int ind = WuColorQuantizer.Ind(inr + 1, ing + 1, inb + 1);

                this.vwt[ind]++;
                this.vmr[ind] += r;
                this.vmg[ind] += g;
                this.vmb[ind] += b;
                this.m2[ind] += (r * r) + (g * g) + (b * b);
            }
        }

        /// <summary>
        /// Converts the histogram into moments so that we can rapidly calculate
        /// the sums of the above quantities over any desired box.
        /// </summary>
        private void M3d()
        {
            long[] area = new long[WuColorQuantizer.IndexCount];
            long[] area_r = new long[WuColorQuantizer.IndexCount];
            long[] area_g = new long[WuColorQuantizer.IndexCount];
            long[] area_b = new long[WuColorQuantizer.IndexCount];
            double[] area2 = new double[WuColorQuantizer.IndexCount];

            for (int r = 1; r < WuColorQuantizer.IndexCount; r++)
            {
                Array.Clear(area, 0, WuColorQuantizer.IndexCount);
                Array.Clear(area_r, 0, WuColorQuantizer.IndexCount);
                Array.Clear(area_g, 0, WuColorQuantizer.IndexCount);
                Array.Clear(area_b, 0, WuColorQuantizer.IndexCount);
                Array.Clear(area2, 0, WuColorQuantizer.IndexCount);

                for (int g = 1; g < WuColorQuantizer.IndexCount; g++)
                {
                    long line = 0;
                    long line_r = 0;
                    long line_g = 0;
                    long line_b = 0;
                    double line2 = 0;

                    for (int b = 1; b < WuColorQuantizer.IndexCount; b++)
                    {
                        int ind1 = WuColorQuantizer.Ind(r, g, b);

                        line += this.vwt[ind1];
                        line_r += this.vmr[ind1];
                        line_g += this.vmg[ind1];
                        line_b += this.vmb[ind1];
                        line2 += this.m2[ind1];

                        area[b] += line;
                        area_r[b] += line_r;
                        area_g[b] += line_g;
                        area_b[b] += line_b;
                        area2[b] += line2;

                        int ind2 = ind1 - WuColorQuantizer.Ind(1, 0, 0);

                        this.vwt[ind1] = this.vwt[ind2] + area[b];
                        this.vmr[ind1] = this.vmr[ind2] + area_r[b];
                        this.vmg[ind1] = this.vmg[ind2] + area_g[b];
                        this.vmb[ind1] = this.vmb[ind2] + area_b[b];
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
        private double Var(Box cube)
        {
            double dr = WuColorQuantizer.Volume(cube, this.vmr);
            double dg = WuColorQuantizer.Volume(cube, this.vmg);
            double db = WuColorQuantizer.Volume(cube, this.vmb);

            double xx = this.m2[WuColorQuantizer.Ind(cube.R1, cube.G1, cube.B1)]
             - this.m2[WuColorQuantizer.Ind(cube.R1, cube.G1, cube.B0)]
             - this.m2[WuColorQuantizer.Ind(cube.R1, cube.G0, cube.B1)]
             + this.m2[WuColorQuantizer.Ind(cube.R1, cube.G0, cube.B0)]
             - this.m2[WuColorQuantizer.Ind(cube.R0, cube.G1, cube.B1)]
             + this.m2[WuColorQuantizer.Ind(cube.R0, cube.G1, cube.B0)]
             + this.m2[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B1)]
             - this.m2[WuColorQuantizer.Ind(cube.R0, cube.G0, cube.B0)];

            return xx - (((dr * dr) + (dg * dg) + (db * db)) / WuColorQuantizer.Volume(cube, this.vwt));
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
        /// <param name="whole_r">The whole red.</param>
        /// <param name="whole_g">The whole green.</param>
        /// <param name="whole_b">The whole blue.</param>
        /// <param name="whole_w">The whole weight.</param>
        /// <returns>The result.</returns>
        private double Maximize(Box cube, int direction, int first, int last, out int cut, double whole_r, double whole_g, double whole_b, double whole_w)
        {
            long base_r = WuColorQuantizer.Bottom(cube, direction, this.vmr);
            long base_g = WuColorQuantizer.Bottom(cube, direction, this.vmg);
            long base_b = WuColorQuantizer.Bottom(cube, direction, this.vmb);
            long base_w = WuColorQuantizer.Bottom(cube, direction, this.vwt);

            double max = 0.0;
            cut = -1;

            for (int i = first; i < last; i++)
            {
                double half_r = base_r + WuColorQuantizer.Top(cube, direction, i, this.vmr);
                double half_g = base_g + WuColorQuantizer.Top(cube, direction, i, this.vmg);
                double half_b = base_b + WuColorQuantizer.Top(cube, direction, i, this.vmb);
                double half_w = base_w + WuColorQuantizer.Top(cube, direction, i, this.vwt);

                double temp;

                if (half_w == 0)
                {
                    continue;
                }
                else
                {
                    temp = ((half_r * half_r) + (half_g * half_g) + (half_b * half_b)) / half_w;
                }

                half_r = whole_r - half_r;
                half_g = whole_g - half_g;
                half_b = whole_b - half_b;
                half_w = whole_w - half_w;

                if (half_w == 0)
                {
                    continue;
                }
                else
                {
                    temp += ((half_r * half_r) + (half_g * half_g) + (half_b * half_b)) / half_w;
                }

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
            double whole_r = WuColorQuantizer.Volume(set1, this.vmr);
            double whole_g = WuColorQuantizer.Volume(set1, this.vmg);
            double whole_b = WuColorQuantizer.Volume(set1, this.vmb);
            double whole_w = WuColorQuantizer.Volume(set1, this.vwt);

            int cutr;
            int cutg;
            int cutb;

            double maxr = this.Maximize(set1, 2, set1.R0 + 1, set1.R1, out cutr, whole_r, whole_g, whole_b, whole_w);
            double maxg = this.Maximize(set1, 1, set1.G0 + 1, set1.G1, out cutg, whole_r, whole_g, whole_b, whole_w);
            double maxb = this.Maximize(set1, 0, set1.B0 + 1, set1.B1, out cutb, whole_r, whole_g, whole_b, whole_w);

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

            set1.Volume = (set1.R1 - set1.R0) * (set1.G1 - set1.G0) * (set1.B1 - set1.B0);
            set2.Volume = (set2.R1 - set2.R0) * (set2.G1 - set2.G0) * (set2.B1 - set2.B0);

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
                        this.tag[WuColorQuantizer.Ind(r, g, b)] = label;
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
            double[] vv = new double[colorCount];

            for (int i = 0; i < colorCount; i++)
            {
                cube[i] = new Box();
            }

            cube[0].R0 = cube[0].G0 = cube[0].B0 = 0;
            cube[0].R1 = cube[0].G1 = cube[0].B1 = WuColorQuantizer.IndexCount - 1;

            int next = 0;

            for (int i = 1; i < colorCount; i++)
            {
                if (this.Cut(cube[next], cube[i]))
                {
                    vv[next] = cube[next].Volume > 1 ? this.Var(cube[next]) : 0.0;
                    vv[i] = cube[i].Volume > 1 ? this.Var(cube[i]) : 0.0;
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

                double weight = WuColorQuantizer.Volume(cube[k], this.vwt);

                if (weight != 0)
                {
                    quantizedImage.Palette[(k * 4) + 3] = 0xff;
                    quantizedImage.Palette[(k * 4) + 2] = (byte)(WuColorQuantizer.Volume(cube[k], this.vmr) / weight);
                    quantizedImage.Palette[(k * 4) + 1] = (byte)(WuColorQuantizer.Volume(cube[k], this.vmg) / weight);
                    quantizedImage.Palette[k * 4] = (byte)(WuColorQuantizer.Volume(cube[k], this.vmb) / weight);
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
                int r = image[(i * 4) + 2] >> (8 - WuColorQuantizer.IndexBits);
                int g = image[(i * 4) + 1] >> (8 - WuColorQuantizer.IndexBits);
                int b = image[i * 4] >> (8 - WuColorQuantizer.IndexBits);

                int ind = WuColorQuantizer.Ind(r + 1, g + 1, b + 1);

                quantizedImage.Bytes[i] = this.tag[ind];
            }

            return quantizedImage;
        }
    }
}
