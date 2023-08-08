using System;

namespace ClothoSharedItems.Common
{
    [Serializable]
    public struct RIValue
    {
        public double Real;
        public double Imag;

        public RIValue(double real, double imag)
        {
            this.Real = real; this.Imag = imag;
        }

        public double Abs()
        {
            return Math.Sqrt(this.Square());
        }

        public double Square()
        {
            return (Real * Real + Imag * Imag);
        }

        public RIValue Conjugate()
        {
            return new RIValue(Real, -Imag);
        }

        public static RIValue operator +(RIValue a, RIValue b)
        {
            return new RIValue(a.Real + b.Real, a.Imag + b.Imag);
        }

        public static RIValue operator +(RIValue a, double b)
        {
            return new RIValue(a.Real + b, a.Imag);
        }

        public static RIValue operator +(double a, RIValue b)
        {
            return new RIValue(a + b.Real, b.Imag);
        }

        public static RIValue operator -(RIValue a, RIValue b)
        {
            return new RIValue(a.Real - b.Real, a.Imag - b.Imag);
        }

        public static RIValue operator -(RIValue a, double b)
        {
            return new RIValue(a.Real - b, a.Imag);
        }

        public static RIValue operator -(double a, RIValue b)
        {
            return new RIValue(a - b.Real, -b.Imag);
        }

        public static RIValue operator *(RIValue a, RIValue b)
        {
            return new RIValue(a.Real * b.Real - a.Imag * b.Imag, a.Real * b.Imag + a.Imag * b.Real);
        }

        public static RIValue operator *(RIValue a, double b)
        {
            return new RIValue(a.Real * b, a.Imag * b);
        }

        public static RIValue operator *(double a, RIValue b)
        {
            return new RIValue(a * b.Real, a * b.Imag);
        }

        public static RIValue operator /(RIValue a, RIValue b)
        {
            double squareValue = b.Square();
            return new RIValue((a.Real * b.Real + a.Imag * b.Imag) / squareValue, (a.Imag * b.Real - a.Real * b.Imag) / squareValue);
        }

        public static RIValue operator /(RIValue a, double b)
        {
            return new RIValue(a.Real / b, a.Imag / b);
        }

        public static RIValue operator /(double a, RIValue b)
        {
            double squareValue = b.Square();
            return new RIValue(a * b.Real / squareValue, -a * b.Imag / squareValue);
        }

        public static implicit operator RIValue(double a)
        {
            return new RIValue(a, 0.0);
        }

        private static RIValue Exp(RIValue z)
        {
            double e = Math.Exp(z.Real);
            double r = Math.Cos(z.Imag);
            double i = Math.Sin(z.Imag);

            return new RIValue(e * r, e * i);
        }

        public static RIValue[] RecursiveFFT(RIValue[] a)
        {
            int n = a.Length;
            int n2 = n / 2;

            if (n == 1) return a;

            RIValue z = new RIValue(0.0, 2.0 * Math.PI / n);

            RIValue omega = new RIValue(1.0, 0.0);
            RIValue omegaN = new RIValue(Math.Cos(z.Imag), Math.Sin(z.Imag));

            RIValue[] a0 = new RIValue[n2];
            RIValue[] a1 = new RIValue[n2];
            RIValue[] y0 = new RIValue[n2];
            RIValue[] y1 = new RIValue[n2];
            RIValue[] y = new RIValue[n];

            for (int i = 0; i < n2; i++)
            {
                a0[i] = a[2 * i];
                a1[i] = a[2 * i + 1];
            }

            y0 = RecursiveFFT(a0);
            y1 = RecursiveFFT(a1);

            for (int k = 0; k < n2; k++)
            {
                y[k] = y0[k] + (y1[k] * omega);
                y[k + n2] = y0[k] - (y1[k] * omega);
                omega = omega * omegaN;
            }

            return y;
        }

        public static RIValue[] RecursiveFFT(RIValue[] a, int nby2N)
        {
            int n = nby2N;
            int n2 = nby2N / 2;

            RIValue z = new RIValue(0.0, 2.0 * Math.PI / n);

            RIValue omega = new RIValue(1.0, 0.0);
            RIValue omegaN = new RIValue(Math.Cos(z.Imag), Math.Sin(z.Imag));

            RIValue[] a0 = new RIValue[n2];
            RIValue[] a1 = new RIValue[n2];
            RIValue[] y0 = new RIValue[n2];
            RIValue[] y1 = new RIValue[n2];
            RIValue[] y = new RIValue[n];

            var it = 0;
            for (int i = 0; i < n2; i++)
            {
                if (it < a.Length)
                {
                    a0[i] = a[it++];
                    if (it < a.Length)
                    {
                        a1[i] = a[it++];
                    }
                    else break;
                }
                else break;
            }

            y0 = RecursiveFFT(a0);
            y1 = RecursiveFFT(a1);

            for (int k = 0; k < n2; k++)
            {
                y[k] = y0[k] + (y1[k] * omega);
                y[k + n2] = y0[k] - (y1[k] * omega);
                omega = omega * omegaN;
            }

            return y;
        }
    }
}