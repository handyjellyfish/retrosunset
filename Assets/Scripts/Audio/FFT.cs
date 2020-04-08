using System;
using System.Collections.Generic;
using System.Numerics;

namespace HandyJellyfish.Audio
{
    public class FFT
    {
        readonly int Length;
        Dictionary<(int N, int j), (double sin, double cos)> SinCosLookup;

        public FFT(int length)
        {
            // TODO : Check length is pow 2
            Length = length;
            SinCosLookup = new Dictionary<(int N, int j), (double sin, double cos)>();

            for (var N = 2; N <= Length; N <<= 1)
            {
                for (var j = 0; j < N / 2; j++)
                {
                    var val = -2 * Math.PI * (j / (double)N);
                    SinCosLookup[(N, j)] = (Math.Sin(val), Math.Cos(val));
                }
            }
        }

        public void Transform(Complex[] buffer)
        {
            if (buffer.Length != Length)
                throw new InvalidOperationException("Buffer length must equal " + Length);

            var bits = (int)Math.Log(buffer.Length, 2);

            for (var i = 1; i < Length; i++)
            {
                var swapPos = BitReverse(i, bits);
                
                if (swapPos <= i)
                    continue;

                var temp = buffer[i];
                buffer[i] = buffer[swapPos];
                buffer[swapPos] = temp;
            }

            for (var N = 2; N <= buffer.Length; N <<= 1)
            {
                for (var i = 0; i < buffer.Length; i+= N)
                {
                    var halfN = N / 2;

                    for (var j = 0; j < halfN; j++)
                    {
                        var eIx = i + j;
                        var oIx = i + j + halfN;
                        
                        var even = buffer[eIx];
                        var odd = buffer[oIx];
                        
                        var (sin, cos) = SinCosLookup[(N, j)];
                        var signal = new Complex(cos, sin) * odd;

                        buffer[eIx] = even + signal;
                        buffer[oIx] = even - signal; 
                    }
                }
            }
        }

        private static int BitReverse(int number, int bits)
        {
            var reversed = number;
            var count = bits - 1;

            number >>= 1;
            while (number > 0)
            {
                reversed = (reversed << 1) | (number & 1);
                count--;
                number >>= 1; 
            }

            return reversed << count & ((1 << bits) - 1);
        }
    }
}